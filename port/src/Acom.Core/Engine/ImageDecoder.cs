/*
 * Copyright 2010-2026 OpenXcom Developers.
 * C# port: Copyright 2026 ACOM contributors.
 *
 * This file is part of ACOM, a C# port of OpenXcom Extended.
 *
 * ACOM is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * ACOM is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Acom.  If not, see <http://www.gnu.org/licenses/>.
 */
// Replaces lodepng (src/lodepng.cpp) and the SDL_image 1.2 loaders (IMG_png.c, IMG_gif.c, IMG_bmp.c, IMG_lbm.c) used by Surface::loadImage.
// Only what yields an 8-bit surface is decoded; true colour images are reported as such (null).

using System.Buffers.Binary;
using System.IO.Compression;

namespace Acom;

/// <summary>8-bit palette image as read from a file.</summary>
internal sealed class IndexedImage(int width, int height, Rgba[] palette, int colorKey = 0)
{
	public int Width { get; } = width;

	public int Height { get; } = height;

	/// <summary>One palette index per pixel, row by row.</summary>
	public byte[] Pixels { get; } = new byte[width * height];

	/// <summary>The colours of the file's palette (lodepng takes alpha from tRNS, SDL_image path: 255).</summary>
	public Rgba[] Palette { get; } = palette;

	/// <summary>Transparent index declared by the file (SDL colour key); 0 if none.</summary>
	public int ColorKey { get; } = colorKey;
}

/// <summary>
/// Reads palette images without converting them to true colour.
/// <see cref="DecodeLodePng"/> behaves like lodepng with <c>color_convert = 0</c>,
/// <see cref="DecodeSdl"/> like <c>IMG_Load</c> of SDL_image 1.2 (BMP, GIF, LBM, PNG via libpng).
/// </summary>
internal static class ImageDecoder
{
	/// <summary>Decodes a PNG like lodepng does for <c>Surface::loadImage</c>.</summary>
	/// <returns>The image, or null if it does not have 8 bits per pixel.</returns>
	/// <exception cref="InvalidDataException">The file is no valid PNG.</exception>
	public static IndexedImage? DecodeLodePng(byte[] data)
	{
		var png = PngFile.Read(data);
		if (png.BitDepth != 8 || (png.ColorType != PngFile.Palette && png.ColorType != PngFile.Grey))
			return null;
		var image = new IndexedImage(png.Width, png.Height, png.Plte ?? []);
		png.ReadSamples(image.Pixels);
		return image;
	}

	/// <summary>Decodes an image like SDL_image's <c>IMG_Load</c>.</summary>
	/// <returns>The image, or null if SDL_image would return a true colour surface.</returns>
	/// <exception cref="InvalidDataException">Unknown format or broken file.</exception>
	public static IndexedImage? DecodeSdl(byte[] data)
	{
		ArgumentNullException.ThrowIfNull(data);
		if (data.Length >= 4 && data[0] == 0x89 && data[1] == 'P' && data[2] == 'N' && data[3] == 'G')
			return DecodeSdlPng(data);
		if (data.Length >= 2 && data[0] == 'B' && data[1] == 'M')
			return BmpReader.Read(data);
		if (data.Length >= 6 && "GIF"u8.SequenceEqual(data.AsSpan(0, 3)) && ("87a"u8.SequenceEqual(data.AsSpan(3, 3)) || "89a"u8.SequenceEqual(data.AsSpan(3, 3))))
			return new GifReader(data).Read();
		if (data.Length >= 12 && "FORM"u8.SequenceEqual(data.AsSpan(0, 4)) && ("PBM "u8.SequenceEqual(data.AsSpan(8, 4)) || "ILBM"u8.SequenceEqual(data.AsSpan(8, 4))))
			return LbmReader.Read(data);
		throw new InvalidDataException("Unsupported image format");
	}

	/// <summary>IMG_png.c: packing, 16-bit stripping and grey expansion, single transparent index as colour key.</summary>
	private static IndexedImage? DecodeSdlPng(byte[] data)
	{
		var png = PngFile.Read(data);
		IndexedImage image;
		if (png.ColorType == PngFile.Palette)
		{
			if (png.Plte == null)
				throw new InvalidDataException("Missing PLTE before IDAT");
			int ckey = 0;
			if (png.Trns != null)
			{
				// all tRNS entries must be opaque except one, otherwise libpng expands to RGBA
				int t = -1, i;
				for (i = 0; i < png.Trns.Length; ++i)
				{
					if (png.Trns[i] == 0)
					{
						if (t >= 0)
							break;
						t = i;
					}
					else if (png.Trns[i] != 255)
					{
						break;
					}
				}
				if (i != png.Trns.Length)
					return null;
				ckey = Math.Max(t, 0);
			}
			image = new IndexedImage(png.Width, png.Height, [.. png.Plte.Select(c => c with { A = 255 })], ckey);
			png.ReadSamples(image.Pixels);
		}
		else if (png.ColorType == PngFile.Grey && !png.HasGreyKey)
		{
			// grey ramp palette (with a tRNS key libpng adds an alpha channel instead)
			var ramp = new Rgba[256];
			for (int i = 0; i < 256; ++i)
				ramp[i] = new Rgba((byte)i, (byte)i, (byte)i);
			image = new IndexedImage(png.Width, png.Height, ramp);
			png.ReadSamples(image.Pixels);
			int mul = png.BitDepth < 8 ? 255 / ((1 << png.BitDepth) - 1) : 1;
			if (mul != 1)
			{
				for (int i = 0; i < image.Pixels.Length; ++i)
					image.Pixels[i] = (byte)(image.Pixels[i] * mul);
			}
		}
		else
		{
			return null;
		}
		return image;
	}

	/// <summary>The chunks of a PNG file, checked like lodepng does.</summary>
	private sealed class PngFile
	{
		public const int Grey = 0, Palette = 3;

		private static readonly int[] Adam7X = [0, 4, 0, 2, 0, 1, 0];
		private static readonly int[] Adam7Y = [0, 0, 4, 0, 2, 0, 1];
		private static readonly int[] Adam7Dx = [8, 8, 4, 4, 2, 2, 1];
		private static readonly int[] Adam7Dy = [8, 8, 8, 4, 4, 2, 2];
		private static readonly uint[] CrcTable = CreateCrcTable();

		private byte[] _data = [];
		private readonly List<Range> _idat = [];

		public int Width { get; private set; }

		public int Height { get; private set; }

		public int BitDepth { get; private set; }

		public int ColorType { get; private set; }

		public bool Interlaced { get; private set; }

		/// <summary>PLTE entries, alpha from tRNS (255 without).</summary>
		public Rgba[]? Plte { get; private set; }

		/// <summary>Raw tRNS entries of a palette image.</summary>
		public byte[]? Trns { get; private set; }

		/// <summary>A greyscale image has a transparent grey value (tRNS).</summary>
		public bool HasGreyKey { get; private set; }

		public static PngFile Read(byte[] data)
		{
			ArgumentNullException.ThrowIfNull(data);
			if (data.Length < 33)
				throw new InvalidDataException("PNG file is smaller than a PNG header");
			if (!data.AsSpan(0, 8).SequenceEqual((ReadOnlySpan<byte>)[137, 80, 78, 71, 13, 10, 26, 10]))
				throw new InvalidDataException("incorrect PNG signature, it's no PNG or corrupted");
			if (!"IHDR"u8.SequenceEqual(data.AsSpan(12, 4)))
				throw new InvalidDataException("first chunk is not the header chunk");
			if (BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(8)) != 13)
				throw new InvalidDataException("header chunk must have a size of 13 bytes");
			var png = new PngFile { _data = data };
			uint w = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(16)), h = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(20));
			if (w == 0 || h == 0)
				throw new InvalidDataException("zero width or height is invalid");
			if ((ulong)w * h > int.MaxValue / 2)
				throw new InvalidDataException("too many pixels, not supported");
			png.Width = (int)w;
			png.Height = (int)h;
			png.BitDepth = data[24];
			png.ColorType = data[25];
			int[]? depths = png.ColorType switch
			{
				Grey => [1, 2, 4, 8, 16],
				Palette => [1, 2, 4, 8],
				2 or 4 or 6 => [8, 16],
				_ => null,
			};
			if (depths == null)
				throw new InvalidDataException("illegal PNG color type or bpp");
			if (!depths.Contains(png.BitDepth))
				throw new InvalidDataException("bit depth not allowed for color type");
			if (data[26] != 0)
				throw new InvalidDataException("unsupported compression method");
			if (data[27] != 0)
				throw new InvalidDataException("unsupported filter method");
			if (data[28] > 1)
				throw new InvalidDataException("unsupported interlace method");
			png.Interlaced = data[28] == 1;
			CheckCrc(data, 8, 13);

			int pos = 33;
			bool iend = false;
			while (!iend)
			{
				if (pos + 12 > data.Length)
					throw new InvalidDataException("chunk length too large, chunk broken off at end of file");
				uint length = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pos));
				if (length > int.MaxValue || pos + 12 + (long)length > data.Length)
					throw new InvalidDataException("chunk length too large, chunk broken off at end of file");
				var type = data.AsSpan(pos + 4, 4);
				var body = data.AsSpan(pos + 8, (int)length);
				// the CRC is checked for the chunks lodepng knows (the content of bKGD, tEXt, ... does not matter here)
				bool known = true;
				if ("IDAT"u8.SequenceEqual(type))
					png._idat.Add(new Range(pos + 8, pos + 8 + (int)length));
				else if ("IEND"u8.SequenceEqual(type))
					iend = true;
				else if ("PLTE"u8.SequenceEqual(type))
					png.ReadPlte(body);
				else if ("tRNS"u8.SequenceEqual(type))
					png.ReadTrns(body);
				else if (!IsAnyOf(type, "bKGD"u8, "tEXt"u8, "zTXt"u8, "iTXt"u8, "tIME"u8, "pHYs"u8))
					known = false;
				if (!known && (type[0] & 32) == 0)
					throw new InvalidDataException("unknown chunk type with critical flag");
				if (known)
					CheckCrc(data, pos, (int)length);
				pos += 12 + (int)length;
			}
			return png;
		}

		private static bool IsAnyOf(ReadOnlySpan<byte> type, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, ReadOnlySpan<byte> c,
			ReadOnlySpan<byte> d, ReadOnlySpan<byte> e, ReadOnlySpan<byte> f)
		{
			return type.SequenceEqual(a) || type.SequenceEqual(b) || type.SequenceEqual(c)
				|| type.SequenceEqual(d) || type.SequenceEqual(e) || type.SequenceEqual(f);
		}

		private void ReadPlte(ReadOnlySpan<byte> body)
		{
			int count = body.Length / 3;
			if (count > 256)
				throw new InvalidDataException("the palette is too big");
			var plte = new Rgba[count];
			for (int i = 0; i < count; ++i)
				plte[i] = new Rgba(body[i * 3], body[i * 3 + 1], body[i * 3 + 2]);
			Plte = plte;
		}

		private void ReadTrns(ReadOnlySpan<byte> body)
		{
			if (ColorType == Palette)
			{
				if (body.Length > (Plte?.Length ?? 0))
					throw new InvalidDataException("more palette alpha values given in tRNS chunk than there are colors in the palette");
				for (int i = 0; i < body.Length; ++i)
					Plte![i] = Plte[i] with { A = body[i] };
				Trns = body.ToArray();
			}
			else if (ColorType == Grey)
			{
				if (body.Length != 2)
					throw new InvalidDataException("tRNS chunk has wrong size for greyscale image");
				HasGreyKey = true;
			}
			else if (ColorType == 2)
			{
				if (body.Length != 6)
					throw new InvalidDataException("tRNS chunk has wrong size for RGB image");
			}
			else
			{
				throw new InvalidDataException("tRNS chunk not allowed for color type");
			}
		}

		private static void CheckCrc(byte[] data, int chunk, int length)
		{
			uint crc = 0xFFFFFFFF;
			foreach (byte b in data.AsSpan(chunk + 4, length + 4))
				crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
			if (~crc != BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(chunk + 8 + length)))
				throw new InvalidDataException("invalid CRC encountered (checking CRC can be disabled)");
		}

		private static uint[] CreateCrcTable()
		{
			var table = new uint[256];
			for (uint n = 0; n < 256; ++n)
			{
				uint c = n;
				for (int k = 0; k < 8; ++k)
					c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
				table[n] = c;
			}
			return table;
		}

		/// <summary>
		/// Decompresses and unfilters the image data and stores one sample per pixel
		/// (the high byte for 16-bit samples). Only valid for single-channel images.
		/// </summary>
		public void ReadSamples(byte[] dest)
		{
			int passes = Interlaced ? 7 : 1;
			var size = new (int W, int H)[passes];
			long predict = 0;
			for (int p = 0; p < passes; ++p)
			{
				size[p] = Interlaced
					? ((Width - Adam7X[p] + Adam7Dx[p] - 1) / Adam7Dx[p], (Height - Adam7Y[p] + Adam7Dy[p] - 1) / Adam7Dy[p])
					: (Width, Height);
				if (size[p].W > 0 && size[p].H > 0)
					predict += (long)size[p].H * (1 + LineBytes(size[p].W));
			}

			var raw = new byte[predict];
			try
			{
				var idat = new byte[_idat.Sum(r => r.GetOffsetAndLength(_data.Length).Length)];
				int offset = 0;
				foreach (var r in _idat)
				{
					_data.AsSpan(r).CopyTo(idat.AsSpan(offset));
					offset += r.GetOffsetAndLength(_data.Length).Length;
				}
				using var z = new ZLibStream(new MemoryStream(idat), CompressionMode.Decompress);
				z.ReadAtLeast(raw, raw.Length, throwOnEndOfStream: true);
				if (z.ReadByte() != -1)
					throw new InvalidDataException("decompressed size doesn't match prediction");
			}
			catch (EndOfStreamException)
			{
				throw new InvalidDataException("decompressed size doesn't match prediction");
			}

			int pos = 0;
			for (int p = 0; p < passes; ++p)
			{
				var (w, h) = size[p];
				if (w == 0 || h == 0)
					continue;
				int lineBytes = LineBytes(w);
				int bpp = Math.Max(1, BitDepth / 8); // byte distance of the filters
				var prev = new byte[lineBytes];
				for (int y = 0; y < h; ++y)
				{
					int filter = raw[pos];
					var line = raw.AsSpan(pos + 1, lineBytes);
					Unfilter(line, prev, filter, bpp);
					for (int x = 0; x < w; ++x)
					{
						int sample;
						if (BitDepth >= 8)
						{
							sample = line[x * (BitDepth / 8)];
						}
						else
						{
							int bit = x * BitDepth;
							sample = (line[bit >> 3] >> (8 - BitDepth - (bit & 7))) & ((1 << BitDepth) - 1);
						}
						int dx = Interlaced ? Adam7X[p] + x * Adam7Dx[p] : x;
						int dy = Interlaced ? Adam7Y[p] + y * Adam7Dy[p] : y;
						dest[dy * Width + dx] = (byte)sample;
					}
					line.CopyTo(prev);
					pos += 1 + lineBytes;
				}
			}
		}

		private int LineBytes(int width) => (int)(((long)width * BitDepth + 7) / 8);

		private static void Unfilter(Span<byte> line, ReadOnlySpan<byte> prev, int filter, int bpp)
		{
			switch (filter)
			{
				case 0:
					break;
				case 1:
					for (int i = bpp; i < line.Length; ++i)
						line[i] += line[i - bpp];
					break;
				case 2:
					for (int i = 0; i < line.Length; ++i)
						line[i] += prev[i];
					break;
				case 3:
					for (int i = 0; i < line.Length; ++i)
						line[i] += (byte)(((i >= bpp ? line[i - bpp] : 0) + prev[i]) >> 1);
					break;
				case 4:
					for (int i = 0; i < line.Length; ++i)
					{
						int a = i >= bpp ? line[i - bpp] : 0, b = prev[i], c = i >= bpp ? prev[i - bpp] : 0;
						int pa = Math.Abs(b - c), pb = Math.Abs(a - c), pc = Math.Abs(a + b - 2 * c);
						line[i] += (byte)(pa <= pb && pa <= pc ? a : pb <= pc ? b : c);
					}
					break;
				default:
					throw new InvalidDataException("illegal PNG filter type");
			}
		}
	}

	/// <summary>Port of the GIF reader of SDL_image 1.2 (IMG_gif.c, based on giftopnm): first image only.</summary>
	private sealed class GifReader(byte[] data)
	{
		private const int MAX_LWZ_BITS = 12;

		private readonly byte[] _data = data;
		private int _pos;
		private int _transparent = -1;

		// GetDataBlock
		private bool _zeroDataBlock;

		// GetCode
		private readonly byte[] _buf = new byte[280];
		private int _curbit, _lastbit, _lastByte;
		private bool _done;

		// LWZReadByte
		private bool _fresh;
		private int _codeSize, _setCodeSize, _maxCode, _maxCodeSize, _firstcode, _oldcode, _clearCode, _endCode;
		private readonly int[,] _table = new int[2, 1 << MAX_LWZ_BITS];
		private readonly int[] _stack = new int[(1 << MAX_LWZ_BITS) * 2];
		private int _sp;

		public IndexedImage Read()
		{
			_pos = 6;
			var screen = ReadBytes(7, "failed to read screen descriptor");
			int screenBits = 2 << (screen[4] & 0x07);
			var globalMap = new Rgba[screenBits];
			if ((screen[4] & 0x80) != 0)
				ReadColorMap(globalMap, "error reading global colormap");
			while (true)
			{
				int c = ReadByte("EOF / read error on image data");
				if (c == ';')
					throw new InvalidDataException("only 0 image found in file");
				if (c == '!')
				{
					DoExtension(ReadByte("EOF / read error on extention function code"));
					continue;
				}
				if (c != ',')
					continue; // not a valid start character
				var desc = ReadBytes(9, "couldn't read left/top/width/height");
				var map = globalMap;
				if ((desc[8] & 0x80) != 0)
				{
					map = new Rgba[1 << ((desc[8] & 0x07) + 1)];
					ReadColorMap(map, "error reading local colormap");
				}
				var image = ReadImage(desc[4] | desc[5] << 8, desc[6] | desc[7] << 8, map, (desc[8] & 0x40) != 0);
				if (image != null)
					return image;
			}
		}

		private int ReadByte(string error)
		{
			if (_pos >= _data.Length)
				throw new InvalidDataException(error);
			return _data[_pos++];
		}

		private ReadOnlySpan<byte> ReadBytes(int count, string error)
		{
			if (_pos + count > _data.Length)
				throw new InvalidDataException(error);
			_pos += count;
			return _data.AsSpan(_pos - count, count);
		}

		private void ReadColorMap(Rgba[] map, string error)
		{
			var rgb = ReadBytes(map.Length * 3, error);
			for (int i = 0; i < map.Length; ++i)
				map[i] = new Rgba(rgb[i * 3], rgb[i * 3 + 1], rgb[i * 3 + 2]);
		}

		private void DoExtension(int label)
		{
			var buf = new byte[256];
			if (label == 0xF9) // Graphic Control Extension
			{
				GetDataBlock(buf);
				if ((buf[0] & 0x1) != 0)
					_transparent = buf[3];
			}
			while (GetDataBlock(buf) != 0)
			{
			}
		}

		/// <summary>Reads a data sub-block; returns its size (0 = terminator).</summary>
		/// <remarks>Deviation: SDL_image loops forever on a truncated extension; ACOM reports an error.</remarks>
		private int GetDataBlock(Span<byte> buf)
		{
			int count = ReadByte("error in getting DataBlock size");
			_zeroDataBlock = count == 0;
			ReadBytes(count, "error in reading DataBlock").CopyTo(buf);
			return count;
		}

		/// <summary>Like <see cref="GetDataBlock"/>, but returns -1 at the end of the file.</summary>
		/// <remarks>Deviation: SDL_image then keeps decoding stale buffer bytes; ACOM ends the image data.</remarks>
		private int TryGetDataBlock(Span<byte> buf)
		{
			if (_pos >= _data.Length || _pos + 1 + _data[_pos] > _data.Length)
			{
				_pos = _data.Length;
				return -1;
			}
			return GetDataBlock(buf);
		}

		private IndexedImage? ReadImage(int width, int height, Rgba[] map, bool interlace)
		{
			int c = ReadByte("EOF / read error on image data");
			if (LWZReadByte(true, c) < 0)
				throw new InvalidDataException("error reading image");
			if (width == 0 || height == 0)
				throw new InvalidDataException("empty image");
			var image = new IndexedImage(width, height, map, _transparent >= 0 ? _transparent : 0);
			int xpos = 0, ypos = 0, pass = 0, v;
			while ((v = LWZReadByte(false, c)) >= 0)
			{
				image.Pixels[xpos + ypos * width] = (byte)v;
				++xpos;
				if (xpos == width)
				{
					xpos = 0;
					if (interlace)
					{
						ypos += pass switch { 0 or 1 => 8, 2 => 4, _ => 2 };
						if (ypos >= height)
						{
							++pass;
							if (pass > 3)
								break;
							ypos = pass switch { 1 => 4, 2 => 2, _ => 1 };
						}
					}
					else
					{
						++ypos;
					}
				}
				if (ypos >= height)
					break;
			}
			return image;
		}

		private int GetCode(int codeSize, bool flag)
		{
			if (flag)
			{
				_curbit = 0;
				_lastbit = 0;
				_lastByte = 2;
				_done = false;
				return 0;
			}
			if (_curbit + codeSize >= _lastbit)
			{
				if (_done)
					return -1;
				_buf[0] = _buf[_lastByte - 2];
				_buf[1] = _buf[_lastByte - 1];
				int count = TryGetDataBlock(_buf.AsSpan(2));
				if (count <= 0)
				{
					_done = true;
					count = Math.Max(count, 0);
				}
				_lastByte = 2 + count;
				_curbit = _curbit - _lastbit + 16;
				_lastbit = (2 + count) * 8;
			}
			int ret = 0;
			for (int i = _curbit, j = 0; j < codeSize; ++i, ++j)
				ret |= ((_buf[i / 8] & (1 << (i % 8))) != 0 ? 1 : 0) << j;
			_curbit += codeSize;
			return ret;
		}

		private void ResetTable()
		{
			int i;
			for (i = 0; i < _clearCode; ++i)
			{
				_table[0, i] = 0;
				_table[1, i] = i;
			}
			for (; i < 1 << MAX_LWZ_BITS; ++i)
				_table[0, i] = _table[1, i] = 0;
			_codeSize = _setCodeSize + 1;
			_maxCodeSize = 2 * _clearCode;
			_maxCode = _clearCode + 2;
			_sp = 0;
		}

		private int LWZReadByte(bool flag, int inputCodeSize)
		{
			if (inputCodeSize > MAX_LWZ_BITS)
				return -1;
			if (flag)
			{
				_setCodeSize = inputCodeSize;
				_clearCode = 1 << _setCodeSize;
				_endCode = _clearCode + 1;
				ResetTable();
				GetCode(0, true);
				_fresh = true;
				return 0;
			}
			if (_fresh)
			{
				_fresh = false;
				do
				{
					_firstcode = _oldcode = GetCode(_codeSize, false);
				}
				while (_firstcode == _clearCode);
				return _firstcode;
			}
			if (_sp > 0)
				return _stack[--_sp];

			int code;
			while ((code = GetCode(_codeSize, false)) >= 0)
			{
				if (code == _clearCode)
				{
					ResetTable();
					_firstcode = _oldcode = GetCode(_codeSize, false);
					return _firstcode;
				}
				if (code == _endCode)
				{
					if (_zeroDataBlock)
						return -2;
					var buf = new byte[256];
					while (TryGetDataBlock(buf) > 0)
					{
					}
					return -2;
				}
				int incode = code;
				if (code >= _maxCode)
				{
					_stack[_sp++] = _firstcode;
					code = _oldcode;
				}
				while (code >= _clearCode)
				{
					if (code >= 1 << MAX_LWZ_BITS || _sp >= _stack.Length - 1)
						return -3; // invalid LWZ data
					_stack[_sp++] = _table[1, code];
					if (code == _table[0, code])
						return -3; // circular table entry
					code = _table[0, code];
				}
				_stack[_sp++] = _firstcode = _table[1, code];
				if ((code = _maxCode) < 1 << MAX_LWZ_BITS)
				{
					_table[0, code] = _oldcode;
					_table[1, code] = _firstcode;
					++_maxCode;
					if (_maxCode >= _maxCodeSize && _maxCodeSize < 1 << MAX_LWZ_BITS)
					{
						_maxCodeSize *= 2;
						++_codeSize;
					}
				}
				_oldcode = incode;
				if (_sp > 0)
					return _stack[--_sp];
			}
			return code;
		}
	}

	/// <summary>
	/// Port of the BMP reader of SDL_image 1.2.12 (IMG_bmp.c LoadBMP_RW, SDL's loader plus RLE4/RLE8):
	/// 1 and 4 bit images are expanded to 8 bits, the palette always has 1 &lt;&lt; bits entries. No colour key.
	/// </summary>
	private static class BmpReader
	{
		private const int BI_RGB = 0, BI_RLE8 = 1, BI_RLE4 = 2;

		public static IndexedImage? Read(byte[] data)
		{
			int pos = 2;
			// The Win32 BMP file header (14 bytes)
			_ = ReadLE32(data, ref pos); // bfSize
			pos += 4; // bfReserved1, bfReserved2
			uint bfOffBits = ReadLE32(data, ref pos);

			// The Win32 BITMAPINFOHEADER struct (40 bytes)
			uint biSize = ReadLE32(data, ref pos);
			int biWidth, biHeight, biBitCount;
			uint biCompression;
			if (biSize == 12)
			{
				biWidth = ReadLE16(data, ref pos);
				biHeight = ReadLE16(data, ref pos);
				pos += 2; // biPlanes
				biBitCount = ReadLE16(data, ref pos);
				biCompression = BI_RGB;
			}
			else
			{
				biWidth = (int)ReadLE32(data, ref pos);
				biHeight = (int)ReadLE32(data, ref pos);
				pos += 2; // biPlanes
				biBitCount = ReadLE16(data, ref pos);
				biCompression = ReadLE32(data, ref pos);
				// biSizeImage, biXPelsPerMeter, biYPelsPerMeter, biClrUsed, biClrImportant are not used
				if (pos + 20 > data.Length)
					throw new InvalidDataException("Error reading from BMP");
			}
			bool topDown = false;
			if (biHeight < 0)
			{
				topDown = true;
				biHeight = -biHeight;
			}

			// Expand 1 and 4 bit bitmaps to 8 bits per pixel
			int expandBMP = 0;
			if (biBitCount is 1 or 4)
			{
				expandBMP = biBitCount;
				biBitCount = 8;
			}
			// Only 8-bit surfaces are of use (Surface::loadImage rejects the others).
			if (biBitCount != 8)
				return null;
			// Security fix of SDL_image (Debian 1.2.12 build, SDL2_image >= 2.0.5); the size limit stands for a failing SDL_CreateRGBSurface
			if (biWidth <= 0 || biHeight == 0)
				throw new InvalidDataException($"BMP file with bad dimensions ({biWidth}x{biHeight})");
			if (biWidth > 0x7FFF || biHeight > 0x7FFF)
				throw new InvalidDataException("Width or height is too large");

			// Create a compatible surface (8 bits: pitch aligned to 4 bytes, palette of 256 black colours)
			int pitch = (biWidth + 3) & ~3;
			var pixels = new byte[pitch * biHeight];
			var palette = new Rgba[256];
			Array.Fill(palette, new Rgba(0, 0, 0));

			// Load the palette: guich: always use 1<<bpp b/c some bitmaps can bring wrong information for colorsUsed
			int biClrUsed = 1 << biBitCount;
			long p = 14 + (long)biSize;
			int entry = biSize == 12 ? 3 : 4;
			for (int i = 0; i < biClrUsed; ++i, p += entry)
			{
				// failed reads keep the zeroed colours
				byte b = p < data.Length ? data[p] : (byte)0;
				byte g = p + 1 < data.Length ? data[p + 1] : (byte)0;
				byte r = p + 2 < data.Length ? data[p + 2] : (byte)0;
				palette[i] = new Rgba(r, g, b);
			}

			// Read the surface pixels. Note that the bmp image is upside down
			if (bfOffBits > data.Length)
				throw new InvalidDataException("Error seeking in datastream");
			pos = (int)bfOffBits;
			if (biCompression is BI_RLE4 or BI_RLE8)
			{
				if (ReadRlePixels(data, ref pos, pixels, pitch, biHeight, biCompression == BI_RLE8))
					throw new InvalidDataException("Error reading from BMP");
			}
			else
			{
				int pad;
				switch (expandBMP)
				{
					case 1:
					{
						int bmpPitch = (biWidth + 7) >> 3;
						pad = (bmpPitch % 4) != 0 ? 4 - (bmpPitch % 4) : 0;
						break;
					}
					case 4:
					{
						int bmpPitch = (biWidth + 1) >> 1;
						pad = (bmpPitch % 4) != 0 ? 4 - (bmpPitch % 4) : 0;
						break;
					}
					default:
						pad = (pitch % 4) != 0 ? 4 - (pitch % 4) : 0;
						break;
				}
				for (int row = 0; row < biHeight; ++row)
				{
					int bits = (topDown ? row : biHeight - 1 - row) * pitch;
					if (expandBMP != 0)
					{
						byte pixel = 0;
						int shift = 8 - expandBMP;
						for (int i = 0; i < biWidth; ++i)
						{
							if (i % (8 / expandBMP) == 0)
							{
								if (pos >= data.Length)
									throw new InvalidDataException("Error reading from BMP");
								pixel = data[pos++];
							}
							pixels[bits + i] = (byte)(pixel >> shift);
							pixel = (byte)(pixel << expandBMP);
						}
					}
					else
					{
						if (pos + pitch > data.Length)
							throw new InvalidDataException("Error reading from datastream");
						data.AsSpan(pos, pitch).CopyTo(pixels.AsSpan(bits, pitch));
						pos += pitch;
					}
					// Skip padding bytes, ugh
					pos += pad;
				}
			}

			var image = new IndexedImage(biWidth, biHeight, palette);
			for (int y = 0; y < biHeight; ++y)
				pixels.AsSpan(y * pitch, biWidth).CopyTo(image.Pixels.AsSpan(y * biWidth));
			return image;
		}

		/// <summary>readRlePixels: decodes RLE4/RLE8 bottom-up into the pitched buffer.</summary>
		/// <returns>True on a read error (like the original).</returns>
		private static bool ReadRlePixels(byte[] data, ref int pos, byte[] pixels, int pitch, int height, bool isRle8)
		{
			int end = height * pitch;
			int bits = end - pitch;
			int ofs = 0;

			void CopyPixel(int bits, ref int ofs, byte x)
			{
				int spot = bits + ofs++;
				if (spot >= 0 && spot < end)
					pixels[spot] = x;
			}

			for (;;)
			{
				if (pos >= data.Length) return true;
				byte ch = data[pos++];
				// encoded mode starts with a run length, and then a byte
				// with two colour indexes to alternate between for the run
				if (ch != 0)
				{
					if (pos >= data.Length) return true;
					byte pixel = data[pos++];
					if (isRle8)
					{
						// 256-color bitmap, compressed
						do
						{
							CopyPixel(bits, ref ofs, pixel);
						}
						while (--ch != 0);
					}
					else
					{
						// 16-color bitmap, compressed
						byte pixel0 = (byte)(pixel >> 4);
						byte pixel1 = (byte)(pixel & 0x0F);
						for (;;)
						{
							CopyPixel(bits, ref ofs, pixel0); // even count, high nibble
							if (--ch == 0) break;
							CopyPixel(bits, ref ofs, pixel1); // odd count, low nibble
							if (--ch == 0) break;
						}
					}
				}
				else
				{
					// A leading zero is an escape; it may signal the end of the bitmap,
					// a cursor move, or some absolute data.
					if (pos >= data.Length) return true;
					ch = data[pos++];
					switch (ch)
					{
						case 0: // end of line
							ofs = 0;
							bits -= pitch; // go to previous
							break;
						case 1: // end of bitmap
							return false; // success!
						case 2: // delta
							if (pos >= data.Length) return true;
							ofs += data[pos++];
							if (pos >= data.Length) return true;
							bits -= data[pos++] * pitch;
							break;
						default: // no compression
						{
							bool needsPad;
							if (isRle8)
							{
								needsPad = (ch & 1) != 0;
								do
								{
									if (pos >= data.Length) return true;
									CopyPixel(bits, ref ofs, data[pos++]);
								}
								while (--ch != 0);
							}
							else
							{
								needsPad = (((ch + 1) >> 1) & 1) != 0; // (ch+1)>>1: bytes size
								for (;;)
								{
									if (pos >= data.Length) return true;
									byte pixel = data[pos++];
									CopyPixel(bits, ref ofs, (byte)(pixel >> 4));
									if (--ch == 0) break;
									CopyPixel(bits, ref ofs, (byte)(pixel & 0x0F));
									if (--ch == 0) break;
								}
							}
							// pad at even boundary
							if (needsPad)
							{
								if (pos >= data.Length) return true;
								pos++;
							}
							break;
						}
					}
				}
			}
		}

		private static uint ReadLE32(byte[] data, ref int pos)
		{
			if (pos + 4 > data.Length)
				throw new InvalidDataException("Error reading from BMP");
			uint v = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(pos));
			pos += 4;
			return v;
		}

		private static ushort ReadLE16(byte[] data, ref int pos)
		{
			if (pos + 2 > data.Length)
				throw new InvalidDataException("Error reading from BMP");
			ushort v = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(pos));
			pos += 2;
			return v;
		}
	}

	/// <summary>
	/// Port of the IFF reader of SDL_image 1.2.12 (IMG_lbm.c): ILBM (interleaved bit planes) and PBM (packed bytes),
	/// ByteRun1 compression, EHB palettes, transparent colour as colour key. The surface width is rounded up to 16
	/// pixels like in the original. 24-bit and HAM pictures give true colour surfaces (null).
	/// </summary>
	private static class LbmReader
	{
		private const int MAXCOLORS = 256;

		public static IndexedImage? Read(byte[] data)
		{
			int pos = 12; // "FORM", size, "PBM "/"ILBM" (checked by IMG_isLBM)
			bool pbm = data[8] == 'P';
			uint nbcolors = 0;
			var colormap = new byte[MAXCOLORS * 3];
			int w = 0, h = 0, planes = 0, mask = 0, tcomp = 0, tcolor = 0;
			bool flagHAM = false, flagEHB = false;

			var id = new byte[4];
			for (;;)
			{
				Read(data, ref pos, id, "error reading IFF chunk");
				uint size = BinaryPrimitives.ReadUInt32BigEndian(Read(data, ref pos, 4, "error reading IFF chunk size"));
				if ("BODY"u8.SequenceEqual(id))
					break;
				uint bytesloaded = 0;

				if ("BMHD"u8.SequenceEqual(id)) // Bitmap header
				{
					var bmhd = Read(data, ref pos, 20, "error reading BMHD chunk");
					bytesloaded = 20;
					w = BinaryPrimitives.ReadUInt16BigEndian(bmhd);
					h = BinaryPrimitives.ReadUInt16BigEndian(bmhd[2..]);
					planes = bmhd[8];
					mask = bmhd[9];
					tcomp = bmhd[10];
					tcolor = BinaryPrimitives.ReadUInt16BigEndian(bmhd[12..]);
				}

				if ("CMAP"u8.SequenceEqual(id)) // palette ( Color Map )
				{
					if (size > colormap.Length)
						throw new InvalidDataException("colormap size is too large");
					Read(data, ref pos, (int)size, "error reading CMAP chunk").CopyTo(colormap);
					bytesloaded = size;
					nbcolors = size / 3;
				}

				if ("CAMG"u8.SequenceEqual(id)) // Amiga ViewMode
				{
					uint viewmodes = BinaryPrimitives.ReadUInt32BigEndian(Read(data, ref pos, 4, "error reading CAMG chunk"));
					bytesloaded = 4;
					if ((viewmodes & 0x0800) != 0)
						flagHAM = true;
					if ((viewmodes & 0x0080) != 0)
						flagEHB = true;
				}

				if ((size & 1) != 0) ++size; // padding !
				size -= bytesloaded;
				// skip the remaining bytes of this chunk
				pos = (int)Math.Min(data.Length, pos + (long)size);
			}

			// compute some usefull values, based on the bitmap header
			int width = (w + 15) & ~15; // Width in pixels modulo 16
			int bytesperline = ((w + 15) / 16) * 2;
			int nbplanes = planes;
			if (pbm) // File format : 'Packed Bitmap'
			{
				bytesperline *= 8;
				nbplanes = 1;
			}
			// Security fix of SDL_image (CVE-2019-12216 and others, in the Debian 1.2.12 build and SDL2_image >= 2.0.5)
			if (nbplanes != 1 && nbplanes != 4 && nbplanes != 8 && nbplanes != 24)
				throw new InvalidDataException("unsupported number of color planes");
			int stencil = mask & 1; // There is a mask ( 'stencil' )

			// Only 8-bit surfaces are of use (Surface::loadImage rejects the others).
			if (planes == 24 || flagHAM)
				return null;

			// Temporary buffer ( used for decompression/deinterleaving )
			var miniBuf = new byte[bytesperline * (nbplanes + stencil)];
			// SDL_CreateRGBSurface: 8 bits, palette of 256 black colours, pitch = width (a multiple of 16)
			var palette = new Rgba[MAXCOLORS];
			Array.Fill(palette, new Rgba(0, 0, 0));
			int ncolors = MAXCOLORS;
			int colorKey = (mask & 2) != 0 ? tcolor : 0; // There is a transparent color

			// Update palette informations
			if (nbcolors > 0)
			{
				int nbrcolorsfinal = 1 << (nbplanes + stencil);
				int p = 0;
				for (int i = 0; i < nbcolors; i++, p += 3)
					palette[i] = new Rgba(colormap[p], colormap[p + 1], colormap[p + 2]);

				// Amiga EHB mode (Extra-Half-Bright): 6 bitplanes mode with a 32 colors palette,
				// the 32 last colors are the same but divided by 2
				// (with the plane count check above this needs a 6-plane ILBM, which is rejected: kept as in the original)
				if ((nbcolors == 32 || flagEHB) && (1 << nbplanes) == 64)
				{
					nbcolors = 64;
					p = 0;
					for (int i = 32; i < 64; i++, p += 3)
						palette[i] = new Rgba((byte)(colormap[p] / 2), (byte)(colormap[p + 1] / 2), (byte)(colormap[p + 2] / 2));
				}

				// If nbcolors < 2^nbplanes, repeat the colormap
				// This happens when pictures have a stencil mask
				if (nbrcolorsfinal > (1 << nbplanes))
					nbrcolorsfinal = 1 << nbplanes;
				// Port: more than 8 planes would write past the 256 colours (undefined behaviour in C)
				nbrcolorsfinal = Math.Min(nbrcolorsfinal, MAXCOLORS);
				for (int i = (int)nbcolors; i < nbrcolorsfinal; i++)
					palette[i] = palette[i % (int)nbcolors];
				if (!pbm)
					ncolors = nbrcolorsfinal;
			}

			var image = new IndexedImage(width, h, palette[..ncolors], colorKey);
			var pixels = image.Pixels;

			// Get the bitmap
			for (int y = 0; y < h; y++)
			{
				// uncompress the datas of each planes
				for (int plane = 0; plane < nbplanes + stencil; plane++)
				{
					int ptr = plane * bytesperline;
					int remainingbytes = bytesperline;
					if (tcomp == 1) // Datas are compressed
					{
						do
						{
							if (pos >= data.Length)
								throw new InvalidDataException("error reading BODY chunk");
							int count = data[pos++];
							if ((count & 0x80) != 0)
							{
								count = (byte)(count ^ 0xFF);
								count += 2; // now it
								if (count > remainingbytes || pos >= data.Length)
									throw new InvalidDataException("error reading BODY chunk");
								miniBuf.AsSpan(ptr, count).Fill(data[pos++]);
							}
							else
							{
								++count;
								if (count > remainingbytes || pos + count > data.Length)
									throw new InvalidDataException("error reading BODY chunk");
								data.AsSpan(pos, count).CopyTo(miniBuf.AsSpan(ptr));
								pos += count;
							}
							ptr += count;
							remainingbytes -= count;
						}
						while (remainingbytes > 0);
					}
					else
					{
						Read(data, ref pos, bytesperline, "error reading BODY chunk").CopyTo(miniBuf.AsSpan(ptr));
					}
				}

				// One line has been read, store it !
				int dst = y * width;
				if (pbm) // File format : 'Packed Bitmap'
				{
					miniBuf.AsSpan(0, width).CopyTo(pixels.AsSpan(dst));
				}
				else // We have to un-interlace the bits !
				{
					int size = (width + 7) / 8;
					for (int i = 0; i < size; i++)
					{
						pixels.AsSpan(dst, 8).Clear();
						for (int plane = 0; plane < nbplanes + stencil; plane++)
						{
							byte color = miniBuf[i + plane * bytesperline];
							byte msk = 0x80;
							for (int j = 0; j < 8; j++)
							{
								// (Uint8)(color & msk) shifted as int, stored as Uint8
								if (plane + j <= 7)
									pixels[dst + j] |= (byte)((color & msk) >> (7 - plane - j));
								else
									pixels[dst + j] |= (byte)((color & msk) << (plane + j - 7));
								msk >>= 1;
							}
						}
						dst += 8;
					}
				}
			}
			return image;
		}

		private static void Read(byte[] data, ref int pos, byte[] dest, string error)
			=> Read(data, ref pos, dest.Length, error).CopyTo(dest);

		private static ReadOnlySpan<byte> Read(byte[] data, ref int pos, int count, string error)
		{
			if (count < 0 || pos + (long)count > data.Length)
				throw new InvalidDataException(error);
			var span = data.AsSpan(pos, count);
			pos += count;
			return span;
		}
	}
}
