#!/usr/bin/env python3
# Writes small synthetic PNG/GIF/BMP/LBM files that cover the image loader paths (bit depths, interlacing,
# filters, transparency rules, GIF LZW, BMP RLE, IFF bit planes and ByteRun1). Only own content; no external libraries.
# Usage: make_test_images.py <output dir>
import os
import struct
import sys
import zlib

ADAM7 = [(0, 0, 8, 8), (4, 0, 8, 8), (0, 4, 4, 8), (2, 0, 4, 4), (0, 2, 2, 4), (1, 0, 2, 2), (0, 1, 1, 2)]


def chunk(kind, body):
    return struct.pack('>I', len(body)) + kind + body + struct.pack('>I', zlib.crc32(kind + body) & 0xFFFFFFFF)


def pack_row(samples, depth):
    if depth == 16:
        return b''.join(struct.pack('>H', s) for s in samples)
    if depth == 8:
        return bytes(samples)
    out, acc, bits = bytearray(), 0, 0
    for s in samples:
        acc = (acc << depth) | s
        bits += depth
        if bits == 8:
            out.append(acc)
            acc, bits = 0, 0
    if bits:
        out.append(acc << (8 - bits))
    return bytes(out)


def paeth(a, b, c):
    p = a + b - c
    pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
    return a if pa <= pb and pa <= pc else b if pb <= pc else c


def filter_row(row, prev, ftype, bpp):
    out = bytearray([ftype])
    for i, x in enumerate(row):
        a = row[i - bpp] if i >= bpp else 0
        b = prev[i]
        c = prev[i - bpp] if i >= bpp else 0
        pred = [0, a, b, (a + b) >> 1, paeth(a, b, c)][ftype]
        out.append((x - pred) & 0xFF)
    return out


def png(path, w, h, pixels, depth=8, ctype=3, plte=None, trns=None, interlace=False, corrupt_idat_crc=False):
    bpp = max(1, depth // 8)
    passes = ADAM7 if interlace else [(0, 0, 1, 1)]
    raw, n = bytearray(), 0
    for x0, y0, dx, dy in passes:
        pw, ph = (w - x0 + dx - 1) // dx, (h - y0 + dy - 1) // dy
        if pw <= 0 or ph <= 0:
            continue
        prev = bytes(len(pack_row([0] * pw, depth)))
        for y in range(ph):
            row = pack_row([pixels[(y0 + y * dy) * w + x0 + x * dx] for x in range(pw)], depth)
            raw += filter_row(row, prev, n % 5, bpp)
            prev = row
            n += 1
    data = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, depth, ctype, 0, 0, 1 if interlace else 0))
    if plte is not None:
        data += chunk(b'PLTE', b''.join(bytes(c) for c in plte))
    if trns is not None:
        data += chunk(b'tRNS', bytes(trns))
    data += chunk(b'tEXt', b'Comment\0ACOM test image')
    idat = chunk(b'IDAT', zlib.compress(bytes(raw), 9))
    if corrupt_idat_crc:
        idat = idat[:-1] + bytes([idat[-1] ^ 0xFF])
    data += idat + chunk(b'IEND', b'')
    with open(path, 'wb') as f:
        f.write(data)


def lzw(pixels, min_size):
    """GIF LZW encoder: grows the code size up to 12 bits and sends a clear code when the table is full."""
    clear, end = 1 << min_size, (1 << min_size) + 1
    codes, size = [], min_size + 1
    table, nxt = {}, end + 1
    codes.append((clear, size))
    cur = None
    for p in pixels:
        if cur is None:
            cur = (p,)
            continue
        key = cur + (p,)
        if key in table:
            cur = key
            continue
        codes.append((table[cur] if len(cur) > 1 else cur[0], size))
        if nxt < 4096:
            table[key] = nxt
            nxt += 1
            if nxt > (1 << size) and size < 12:
                size += 1
        else:
            codes.append((clear, size))
            table, nxt, size = {}, end + 1, min_size + 1
        cur = (p,)
    codes.append((table[cur] if len(cur) > 1 else cur[0], size))
    codes.append((end, size))
    out, acc, bits = bytearray(), 0, 0
    for code, s in codes:
        acc |= code << bits
        bits += s
        while bits >= 8:
            out.append(acc & 0xFF)
            acc >>= 8
            bits -= 8
    if bits:
        out.append(acc)
    blocks = bytearray([min_size])
    for i in range(0, len(out), 255):
        part = out[i:i + 255]
        blocks += bytes([len(part)]) + part
    return bytes(blocks + b'\0')


def gif_image(w, h, pixels, colors=None, interlace=False):
    flags = 0x40 if interlace else 0
    table = b''
    if colors is not None:
        bits = max(1, (len(colors) - 1).bit_length())
        flags |= 0x80 | (bits - 1)
        table = b''.join(bytes(c) for c in colors) + bytes(3 * ((1 << bits) - len(colors)))
    rows = list(range(h))
    if interlace:
        rows = list(range(0, h, 8)) + list(range(4, h, 8)) + list(range(2, h, 4)) + list(range(1, h, 2))
    ordered = [pixels[y * w + x] for y in rows for x in range(w)]
    min_size = max(2, (max(pixels)).bit_length())
    return b',' + struct.pack('<HHHHB', 0, 0, w, h, flags) + table + lzw(ordered, min_size)


def gif(path, w, h, images, colors=None, transparent=None, version=b'89a'):
    flags, table = 0, b''
    if colors is not None:
        bits = max(1, (len(colors) - 1).bit_length())
        flags = 0x80 | 0x70 | (bits - 1)
        table = b''.join(bytes(c) for c in colors) + bytes(3 * ((1 << bits) - len(colors)))
    data = b'GIF' + version + struct.pack('<HHBBB', w, h, flags, 0, 0) + table
    data += b'!\xfe' + bytes([9]) + b'ACOM test' + b'\0'  # comment extension
    if transparent is not None:
        data += b'!\xf9\x04' + bytes([1, 0, 0, transparent]) + b'\0'
    for image in images:
        data += image
    data += b';'
    with open(path, 'wb') as f:
        f.write(data)


def ramp(n):
    return [(i * 255 // max(1, n - 1), (i * 37) % 256, 255 - i * 255 // max(1, n - 1)) for i in range(n)]


def pattern(w, h, colors, seed=1):
    out, x = [], seed
    for i in range(w * h):
        x = (x * 1103515245 + 12345) & 0x7FFFFFFF
        out.append((x >> 16) % colors if i % 3 else (i // w + i % w) % colors)
    return out


def bmp_rle8_row(row):
    out, i = bytearray(), 0
    while i < len(row):
        run = 1
        while i + run < len(row) and row[i + run] == row[i] and run < 255:
            run += 1
        if run >= 2 or len(row) - i < 3:
            out += bytes([run, row[i]])
            i += run
        else:
            n = min(len(row) - i, 255)
            k = 3
            while k < n and not (k + 1 < n and row[i + k] == row[i + k + 1]):
                k += 1
            out += bytes([0, k]) + bytes(row[i:i + k]) + (b'\0' if k & 1 else b'')
            i += k
    return out


def bmp_rle4_row(row):
    out, i = bytearray(), 0
    while i < len(row):
        n = min(len(row) - i, 254)
        if n >= 3 and i % 2 == 0:
            k = n if n % 2 == 0 else n - 1
            k = min(k, 8)
            data = row[i:i + k]
            packed = bytes((data[j] << 4) | (data[j + 1] if j + 1 < len(data) else 0) for j in range(0, len(data), 2))
            out += bytes([0, k]) + packed + (b'\0' if ((k + 1) >> 1) & 1 else b'')
            i += k
        else:
            a = row[i]
            b = row[i + 1] if i + 1 < len(row) else 0
            k = min(len(row) - i, 2)
            out += bytes([k, (a << 4) | b])
            i += k
    return out


def bmp(path, w, h, pixels, bits, colors, rle=False, top_down=False, core=False, delta=False):
    """Windows BMP (BITMAPINFOHEADER or OS/2 core header), palette images, optional RLE8/RLE4."""
    ncol = 1 << bits
    pal = b''
    for c in list(colors) + [(0, 0, 0)] * (ncol - len(colors)):
        pal += bytes([c[2], c[1], c[0]]) + (b'' if core else b'\0')
    rows = [pixels[y * w:(y + 1) * w] for y in range(h)]
    order = rows if top_down else rows[::-1]
    body = bytearray()
    if rle:
        for y, row in enumerate(order):
            if delta and y == 1:
                body += bytes([0, 2, 2, 1])  # skip 2 pixels and one line
                continue
            body += (bmp_rle8_row if bits == 8 else bmp_rle4_row)(row) + b'\0\0'
        body += b'\0\1'
    else:
        for row in order:
            line = pack_row(row, bits)
            body += line + bytes((4 - len(line) % 4) % 4)
    compression = (1 if bits == 8 else 2) if rle else 0
    if core:
        info = struct.pack('<IHHHH', 12, w, h, 1, bits)
    else:
        info = struct.pack('<IiiHHIIiiII', 40, w, -h if top_down else h, 1, bits, compression, len(body), 2835, 2835, 0, 0)
    off = 14 + len(info) + len(pal)
    data = b'BM' + struct.pack('<IHHI', off + len(body), 0, 0, off) + info + pal + bytes(body)
    with open(path, 'wb') as f:
        f.write(data)


def byterun1(line):
    out, i = bytearray(), 0
    while i < len(line):
        run = 1
        while i + run < len(line) and line[i + run] == line[i] and run < 128:
            run += 1
        if run >= 2:
            out += bytes([(257 - run) & 0xFF, line[i]])
            i += run
        else:
            k = 1
            while i + k < len(line) and k < 128 and not (i + k + 1 < len(line) and line[i + k] == line[i + k + 1]):
                k += 1
            out += bytes([k - 1]) + bytes(line[i:i + k])
            i += k
    return out


def iff_chunk(kind, body):
    return kind + struct.pack('>I', len(body)) + body + (b'\0' if len(body) & 1 else b'')


def lbm(path, w, h, pixels, planes, colors, pbm=False, compress=False, mask=0, tcolor=0, camg=None, stencil_bits=None):
    """IFF ILBM (bit planes, optional stencil plane) or PBM (one byte per pixel), optional ByteRun1."""
    bpl = (w + 15) // 16 * 2
    body = bytearray()
    for y in range(h):
        row = pixels[y * w:(y + 1) * w]
        lines = []
        if pbm:
            lines.append(bytes(row) + bytes(bpl * 8 - w))
        else:
            for p in range(planes):
                lines.append(pack_row([(v >> p) & 1 for v in row] + [0] * (bpl * 8 - w), 1))
            if mask & 1:
                lines.append(pack_row([stencil_bits[y * w + x] for x in range(w)] + [0] * (bpl * 8 - w), 1))
        for line in lines:
            body += byterun1(line) if compress else line
    bmhd = struct.pack('>HHhhBBBBHBBhh', w, h, 0, 0, planes, mask, 1 if compress else 0, 0, tcolor, 10, 11, 320, 200)
    form = (b'PBM ' if pbm else b'ILBM') + iff_chunk(b'BMHD', bmhd)
    form += iff_chunk(b'ANNO', b'ACOM test')  # odd size: padding
    if camg is not None:
        form += iff_chunk(b'CAMG', struct.pack('>I', camg))
    form += iff_chunk(b'CMAP', b''.join(bytes(c) for c in colors))
    form += iff_chunk(b'BODY', bytes(body))
    with open(path, 'wb') as f:
        f.write(b'FORM' + struct.pack('>I', len(form)) + form)


def main(out):
    os.makedirs(out, exist_ok=True)
    p = lambda name: os.path.join(out, name)
    pix16 = pattern(13, 7, 16)
    png(p('p8_trns0.png'), 13, 7, pix16, plte=ramp(16), trns=[0])
    png(p('p8_trns5.png'), 13, 7, pix16, plte=ramp(16), trns=[255, 255, 255, 255, 255, 0])
    png(p('p8_notrns16.png'), 13, 7, pix16, plte=ramp(16))
    png(p('p8_full256.png'), 23, 17, pattern(23, 17, 256), plte=ramp(256))
    png(p('p8_multi.png'), 13, 7, pix16, plte=ramp(16), trns=[255, 128, 0, 0, 255])
    png(p('p8_interlaced.png'), 9, 11, pattern(9, 11, 32), plte=ramp(32), trns=[0], interlace=True)
    png(p('p8_tiny_interlaced.png'), 3, 2, pattern(3, 2, 4), plte=ramp(4), interlace=True)
    png(p('p8_badcrc.png'), 13, 7, pix16, plte=ramp(16), corrupt_idat_crc=True)
    png(p('p4_key3.png'), 13, 7, pix16, depth=4, plte=ramp(16), trns=[255, 255, 255, 0])
    png(p('p4_alltrns_opaque.png'), 13, 7, pix16, depth=4, plte=ramp(16), trns=[255, 255])
    png(p('p4_interlaced.png'), 13, 7, pix16, depth=4, plte=ramp(16), interlace=True)
    png(p('p4_translucent.png'), 13, 7, pix16, depth=4, plte=ramp(16), trns=[0, 128])
    png(p('p2.png'), 13, 7, pattern(13, 7, 4), depth=2, plte=ramp(4))
    png(p('p1.png'), 13, 7, pattern(13, 7, 2), depth=1, plte=ramp(2), trns=[0])
    png(p('g8.png'), 13, 7, pattern(13, 7, 256), depth=8, ctype=0)
    png(p('g4.png'), 13, 7, pix16, depth=4, ctype=0)
    png(p('g2_interlaced.png'), 13, 7, pattern(13, 7, 4), depth=2, ctype=0, interlace=True)
    png(p('g16.png'), 13, 7, [v * 256 + 3 for v in pattern(13, 7, 256)], depth=16, ctype=0)
    png(p('g4_trns.png'), 13, 7, pix16, depth=4, ctype=0, trns=[0, 5])
    with open(p('rgb.png'), 'wb') as f:
        raw = b''.join(b'\0' + bytes(3 * 4) for _ in range(4))
        f.write(b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', 4, 4, 8, 2, 0, 0, 0))
                + chunk(b'IDAT', zlib.compress(raw)) + chunk(b'IEND', b''))

    gif(p('gif_trans.gif'), 13, 7, [gif_image(13, 7, pix16)], colors=ramp(16), transparent=7)
    gif(p('gif_interlaced.gif'), 9, 19, [gif_image(9, 19, pattern(9, 19, 8), interlace=True)], colors=ramp(8), version=b'87a')
    gif(p('gif_local.gif'), 10, 6, [gif_image(10, 6, pattern(10, 6, 4), colors=ramp(4)),
                                     gif_image(10, 6, pattern(10, 6, 4, 7), colors=ramp(4))])
    gif(p('gif_big.gif'), 128, 96, [gif_image(128, 96, pattern(128, 96, 256))], colors=ramp(256), transparent=0)
    bmp(p('b8.bmp'), 13, 7, pix16, 8, ramp(16))
    bmp(p('b8_topdown.bmp'), 13, 7, pattern(13, 7, 256), 8, ramp(256), top_down=True)
    bmp(p('b4.bmp'), 13, 7, pix16, 4, ramp(16))
    bmp(p('b1.bmp'), 13, 7, pattern(13, 7, 2), 1, ramp(2))
    bmp(p('b8_core.bmp'), 13, 7, pix16, 8, ramp(16), core=True)
    bmp(p('b8_rle.bmp'), 21, 9, [v // 7 for v in pattern(21, 9, 64)], 8, ramp(16), rle=True)
    bmp(p('b8_rle_delta.bmp'), 21, 9, [v // 7 for v in pattern(21, 9, 64)], 8, ramp(16), rle=True, delta=True)
    bmp(p('b4_rle.bmp'), 21, 9, [v // 4 for v in pattern(21, 9, 64)], 4, ramp(16), rle=True)
    with open(p('b24.bmp'), 'wb') as f:
        f.write(b'BM' + struct.pack('<IHHI', 54 + 16, 0, 0, 54) + struct.pack('<IiiHHIIiiII', 40, 2, 2, 1, 24, 0, 16, 0, 0, 0, 0) + bytes(16))
    with open(p('b8_short.bmp'), 'wb') as f:
        f.write(b'BM not a bitmap')

    lbm(p('ilbm4.lbm'), 13, 7, pix16, 4, ramp(16))
    lbm(p('ilbm8_comp.lbm'), 21, 9, pattern(21, 9, 256), 8, ramp(256), compress=True)
    lbm(p('ilbm1_key.lbm'), 13, 7, pattern(13, 7, 2), 1, ramp(2), mask=2, tcolor=1)
    lbm(p('ilbm4_short_cmap.lbm'), 13, 7, pix16, 4, ramp(5), compress=True)
    lbm(p('ilbm4_stencil.lbm'), 13, 7, pix16, 4, ramp(16), mask=1, stencil_bits=pattern(13, 7, 2, 5))
    lbm(p('ilbm5.lbm'), 13, 7, pattern(13, 7, 32), 5, ramp(32))  # rejected plane count
    lbm(p('pbm.lbm'), 13, 7, pattern(13, 7, 256), 8, ramp(256), pbm=True, compress=True)
    lbm(p('pbm_ehb.lbm'), 13, 7, pattern(13, 7, 64), 6, ramp(32), pbm=True, camg=0x80)
    lbm(p('pbm_key.iff'), 16, 4, pix16[:64], 8, ramp(16), pbm=True, mask=2, tcolor=0)
    gif(p('gif_as.png'), 13, 7, [gif_image(13, 7, pix16)], colors=ramp(16), transparent=3)


if __name__ == '__main__':
    main(sys.argv[1])
