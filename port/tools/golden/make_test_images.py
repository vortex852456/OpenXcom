#!/usr/bin/env python3
# Writes small synthetic PNG/GIF files that cover the image loader paths (bit depths, interlacing,
# filters, transparency rules, GIF LZW). Only own content; no external libraries.
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
    gif(p('gif_as.png'), 13, 7, [gif_image(13, 7, pix16)], colors=ramp(16), transparent=3)


if __name__ == '__main__':
    main(sys.argv[1])
