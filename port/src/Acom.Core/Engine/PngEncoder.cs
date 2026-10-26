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
// Replaces lodepng::encode (src/lodepng.cpp) as used for screenshots and debug dumps
// (Screen::screenshot, BattlescapeState::saveAIMap/saveVoxelMap/saveVoxelView).

using System.Buffers.Binary;
using System.IO.Compression;

namespace Acom;

/// <summary>
/// Small PNG writer: 8-bit palette images (PLTE, plus tRNS when a colour is not opaque) and 8-bit RGB images.
/// The files decode to the same pixels as the lodepng output of the original; the bytes differ (own filter choice
/// and zlib from .NET), which no reader depends on.
/// </summary>
public static class PngEncoder
{
	private static readonly uint[] s_crcTable = CreateCrcTable();

	/// <summary>Encodes an 8-bit palette image (lodepng with LCT_PALETTE, bit depth 8, no auto conversion).</summary>
	/// <param name="pixels">One palette index per pixel, <paramref name="width"/> per row.</param>
	/// <param name="width">Width in pixels.</param>
	/// <param name="height">Height in pixels.</param>
	/// <param name="palette">The colours (1 to 256); alpha goes into a tRNS chunk if any colour is not opaque.</param>
	public static byte[] EncodePalette(ReadOnlySpan<byte> pixels, int width, int height, ReadOnlySpan<Rgba> palette)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
		ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
		if (palette.Length is < 1 or > 256)
			throw new ArgumentOutOfRangeException(nameof(palette), "a PNG palette has 1 to 256 colours");
		if (pixels.Length < width * height)
			throw new ArgumentException("not enough pixels", nameof(pixels));

		var plte = new byte[palette.Length * 3];
		var trns = new byte[palette.Length];
		int trnsLength = 0;
		for (int i = 0; i < palette.Length; ++i)
		{
			plte[i * 3] = palette[i].R;
			plte[i * 3 + 1] = palette[i].G;
			plte[i * 3 + 2] = palette[i].B;
			trns[i] = palette[i].A;
			if (palette[i].A != 255)
				trnsLength = i + 1;
		}
		// palette images are not filtered (lodepng's default strategy for them too)
		var raw = new byte[(width + 1) * height];
		for (int y = 0; y < height; ++y)
			pixels.Slice(y * width, width).CopyTo(raw.AsSpan(y * (width + 1) + 1));
		return Write(width, height, 3, plte, trns.AsSpan(0, trnsLength), raw);
	}

	/// <summary>Encodes an 8-bit RGB image (lodepng::encode with LCT_RGB).</summary>
	/// <param name="rgb">Three bytes (R, G, B) per pixel, <paramref name="width"/> pixels per row.</param>
	/// <param name="width">Width in pixels.</param>
	/// <param name="height">Height in pixels.</param>
	public static byte[] EncodeRgb(ReadOnlySpan<byte> rgb, int width, int height)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
		ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
		int stride = width * 3;
		if (rgb.Length < stride * height)
			throw new ArgumentException("not enough pixels", nameof(rgb));

		// per row the filter with the smallest sum of absolute values (lodepng's LFS_MINSUM)
		var raw = new byte[(stride + 1) * height];
		var candidate = new byte[stride];
		var best = new byte[stride];
		ReadOnlySpan<byte> empty = new byte[stride];
		for (int y = 0; y < height; ++y)
		{
			var line = rgb.Slice(y * stride, stride);
			var prev = y > 0 ? rgb.Slice((y - 1) * stride, stride) : empty;
			long bestSum = long.MaxValue;
			int bestType = 0;
			for (int type = 0; type < 5; ++type)
			{
				long sum = 0;
				for (int i = 0; i < stride; ++i)
				{
					int a = i >= 3 ? line[i - 3] : 0, b = prev[i], c = i >= 3 ? prev[i - 3] : 0;
					int pred = type switch
					{
						1 => a,
						2 => b,
						3 => (a + b) >> 1,
						4 => Paeth(a, b, c),
						_ => 0,
					};
					byte v = (byte)(line[i] - pred);
					candidate[i] = v;
					sum += type == 0 ? v : (v < 128 ? v : 256 - v);
				}
				if (sum < bestSum)
				{
					bestSum = sum;
					bestType = type;
					candidate.CopyTo(best, 0);
				}
			}
			raw[y * (stride + 1)] = (byte)bestType;
			best.CopyTo(raw, y * (stride + 1) + 1);
		}
		return Write(width, height, 2, null, default, raw);
	}

	/// <summary>
	/// Writes binary data to a file (C++ <c>CrossPlatform::writeFile(filename, std::vector&lt;unsigned char&gt;)</c>), logging failures.
	/// </summary>
	/// <returns>If we did write it.</returns>
	public static bool WriteFile(string filename, byte[] data)
	{
		try
		{
			File.WriteAllBytes(filename, data);
			return true;
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
		{
			Logger.Error($"Failed to write {filename}: {e.Message}");
			return false;
		}
	}

	private static int Paeth(int a, int b, int c)
	{
		int pa = Math.Abs(b - c), pb = Math.Abs(a - c), pc = Math.Abs(a + b - 2 * c);
		return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
	}

	private static byte[] Write(int width, int height, byte colorType, byte[]? plte, ReadOnlySpan<byte> trns, byte[] raw)
	{
		using var file = new MemoryStream();
		file.Write([0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]);
		var ihdr = new byte[13];
		BinaryPrimitives.WriteInt32BigEndian(ihdr, width);
		BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4), height);
		ihdr[8] = 8; // bit depth
		ihdr[9] = colorType;
		// compression, filter and interlace method 0
		WriteChunk(file, "IHDR"u8, ihdr);
		if (plte != null)
			WriteChunk(file, "PLTE"u8, plte);
		if (!trns.IsEmpty)
			WriteChunk(file, "tRNS"u8, trns);
		using (var idat = new MemoryStream())
		{
			using (var zlib = new ZLibStream(idat, CompressionLevel.Optimal, true))
				zlib.Write(raw);
			WriteChunk(file, "IDAT"u8, idat.GetBuffer().AsSpan(0, (int)idat.Length));
		}
		WriteChunk(file, "IEND"u8, []);
		return file.ToArray();
	}

	private static void WriteChunk(Stream file, ReadOnlySpan<byte> type, ReadOnlySpan<byte> body)
	{
		Span<byte> word = stackalloc byte[4];
		BinaryPrimitives.WriteInt32BigEndian(word, body.Length);
		file.Write(word);
		file.Write(type);
		file.Write(body);
		uint crc = 0xFFFFFFFF;
		foreach (byte b in type)
			crc = s_crcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
		foreach (byte b in body)
			crc = s_crcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
		BinaryPrimitives.WriteUInt32BigEndian(word, crc ^ 0xFFFFFFFF);
		file.Write(word);
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
}
