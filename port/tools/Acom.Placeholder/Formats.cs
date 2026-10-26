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

using System.Buffers.Binary;

namespace Acom.Placeholder;

/// <summary>A colour with 6-bit channels (0..63) as stored in the original palette files.</summary>
public readonly record struct Rgb6(byte R, byte G, byte B);

/// <summary>A paletted image (one byte per pixel, 0 = transparent).</summary>
public sealed class Image
{
	public Image(int width, int height)
	{
		Width = width;
		Height = height;
		Pixels = new byte[width * height];
	}

	public int Width { get; }
	public int Height { get; }
	public byte[] Pixels { get; }

	public byte this[int x, int y]
	{
		get => Pixels[y * Width + x];
		set => Pixels[y * Width + x] = value;
	}

	/// <summary>Sets a pixel if it is inside the image.</summary>
	public void Plot(int x, int y, byte color)
	{
		if ((uint)x < (uint)Width && (uint)y < (uint)Height)
			Pixels[y * Width + x] = color;
	}
}

/// <summary>Writers for the binary formats the engine loads (see the loaders in Acom.Core and src/).</summary>
public static class Formats
{
	/// <summary>Size of one palette in PALETTES.DAT: 256 colours plus 6 unused bytes (Palette::palOffset).</summary>
	public const int PaletteRecord = 768 + 6;

	/// <summary>Size of one MCD record (MapDataSet::loadData).</summary>
	public const int McdRecord = 62;

	/// <summary>Size of one RMP node (BattlescapeGenerator::loadRMP).</summary>
	public const int RmpRecord = 24;

	/// <summary>Writes palettes with 6-bit channels; each one padded to <see cref="PaletteRecord"/> when requested.</summary>
	public static byte[] Palettes(IReadOnlyList<Rgb6[]> palettes, bool padded)
	{
		ArgumentNullException.ThrowIfNull(palettes);
		using var ms = new MemoryStream();
		foreach (var pal in palettes)
		{
			foreach (var c in pal)
			{
				ms.WriteByte(c.R);
				ms.WriteByte(c.G);
				ms.WriteByte(c.B);
			}
			if (padded)
				ms.Write(new byte[PaletteRecord - pal.Length * 3]);
		}
		return ms.ToArray();
	}

	/// <summary>Uncompressed frames one after another (SCR images and DAT sprite sheets).</summary>
	public static byte[] Raw(IReadOnlyList<Image> frames)
	{
		ArgumentNullException.ThrowIfNull(frames);
		using var ms = new MemoryStream();
		foreach (var f in frames)
			ms.Write(f.Pixels);
		return ms.ToArray();
	}

	/// <summary>
	/// PCK sprite set with its TAB offsets (SurfaceSet::loadPck): per frame the number of leading transparent rows,
	/// then pixels, 254 n = n transparent pixels, 255 = end of frame. Pixel values 254 and 255 cannot be stored.
	/// The TAB uses 16-bit offsets like UFO when they fit, otherwise 32-bit offsets (first entry 0, like TFTD).
	/// </summary>
	public static (byte[] Pck, byte[] Tab) Pck(IReadOnlyList<Image> frames)
	{
		ArgumentNullException.ThrowIfNull(frames);
		using var pck = new MemoryStream();
		var offsets = new List<long>();
		foreach (var f in frames)
		{
			offsets.Add(pck.Length);
			int top = 0;
			while (top < f.Height && top < 253 && RowEmpty(f, top))
				++top;
			pck.WriteByte((byte)top);
			int end = f.Pixels.Length;
			while (end > top * f.Width && f.Pixels[end - 1] == 0)
				--end;
			for (int i = top * f.Width; i < end;)
			{
				byte p = f.Pixels[i];
				if (p == 0)
				{
					int n = 0;
					while (i < end && f.Pixels[i] == 0 && n < 255)
					{
						++n;
						++i;
					}
					pck.WriteByte(254);
					pck.WriteByte((byte)n);
				}
				else
				{
					if (p >= 254)
						throw new InvalidDataException("PCK frames cannot use colours 254 and 255");
					pck.WriteByte(p);
					++i;
				}
			}
			pck.WriteByte(255);
		}
		bool small = frames.Count > 1 && pck.Length <= ushort.MaxValue;
		var tab = new byte[offsets.Count * (small ? 2 : 4)];
		for (int i = 0; i < offsets.Count; ++i)
		{
			if (small)
				BinaryPrimitives.WriteUInt16LittleEndian(tab.AsSpan(i * 2), (ushort)offsets[i]);
			else
				BinaryPrimitives.WriteUInt32LittleEndian(tab.AsSpan(i * 4), (uint)offsets[i]);
		}
		return (pck.ToArray(), tab);
	}

	private static bool RowEmpty(Image f, int y)
	{
		for (int x = 0; x < f.Width; ++x)
			if (f[x, y] != 0)
				return false;
		return true;
	}

	/// <summary>
	/// SPK image (Surface::loadSpk): 16-bit commands, FFFF n = 2n transparent pixels, FFFE n = 2n literal pixels, FFFD = end.
	/// </summary>
	public static byte[] Spk(Image image)
	{
		ArgumentNullException.ThrowIfNull(image);
		var px = image.Pixels;
		int pairs = px.Length / 2;
		using var ms = new MemoryStream();
		void Word(int v)
		{
			ms.WriteByte((byte)(v & 0xFF));
			ms.WriteByte((byte)(v >> 8));
		}
		bool Clear(int pair) => px[pair * 2] == 0 && px[pair * 2 + 1] == 0;
		for (int i = 0; i < pairs;)
		{
			int start = i;
			bool clear = Clear(i);
			while (i < pairs && Clear(i) == clear && i - start < 0xFFF0)
				++i;
			Word(clear ? 0xFFFF : 0xFFFE);
			Word(i - start);
			if (!clear)
				ms.Write(px, start * 2, (i - start) * 2);
		}
		Word(0xFFFD);
		return ms.ToArray();
	}

	/// <summary>CAT archive (CatFile): a table of (offset, size) pairs, then the items.</summary>
	public static byte[] Cat(IReadOnlyList<byte[]> items)
	{
		ArgumentNullException.ThrowIfNull(items);
		using var ms = new MemoryStream();
		var header = new byte[items.Count * 8];
		int offset = header.Length;
		for (int i = 0; i < items.Count; ++i)
		{
			BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(i * 8), offset);
			BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(i * 8 + 4), items[i].Length);
			offset += items[i].Length;
		}
		ms.Write(header);
		foreach (var item in items)
			ms.Write(item);
		return ms.ToArray();
	}

	/// <summary>Sound archive item (SoundSet::loadCatByIndex): a name chunk (length byte + text) followed by the sound.</summary>
	public static byte[] CatSoundItem(string name, byte[] sound)
	{
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(sound);
		var item = new byte[1 + name.Length + sound.Length];
		item[0] = (byte)name.Length;
		for (int i = 0; i < name.Length; ++i)
			item[1 + i] = (byte)name[i];
		sound.CopyTo(item, 1 + name.Length);
		return item;
	}

	/// <summary>RIFF WAVE file, PCM 8 bit unsigned mono.</summary>
	public static byte[] Wav(ReadOnlySpan<byte> samples, int sampleRate)
	{
		var wav = new byte[44 + samples.Length];
		var s = wav.AsSpan();
		"RIFF"u8.CopyTo(s);
		BinaryPrimitives.WriteInt32LittleEndian(s[4..], 36 + samples.Length);
		"WAVEfmt "u8.CopyTo(s[8..]);
		BinaryPrimitives.WriteInt32LittleEndian(s[16..], 16);
		BinaryPrimitives.WriteInt16LittleEndian(s[20..], 1); // PCM
		BinaryPrimitives.WriteInt16LittleEndian(s[22..], 1); // mono
		BinaryPrimitives.WriteInt32LittleEndian(s[24..], sampleRate);
		BinaryPrimitives.WriteInt32LittleEndian(s[28..], sampleRate);
		BinaryPrimitives.WriteInt16LittleEndian(s[32..], 1);
		BinaryPrimitives.WriteInt16LittleEndian(s[34..], 8);
		"data"u8.CopyTo(s[36..]);
		BinaryPrimitives.WriteInt32LittleEndian(s[40..], samples.Length);
		samples.CopyTo(s[44..]);
		return wav;
	}

	/// <summary>DOS sound (SoundSet::loadCatByIndex without RIFF header): 5 header bytes, 6-bit samples, 1 trailing byte.</summary>
	public static byte[] DosSound(ReadOnlySpan<byte> samples6)
	{
		var data = new byte[5 + samples6.Length + 1];
		samples6.CopyTo(data.AsSpan(5));
		return data;
	}

	/// <summary>Little-endian 16-bit words (LOFTEMPS.DAT).</summary>
	public static byte[] Words(IReadOnlyList<ushort> words)
	{
		ArgumentNullException.ThrowIfNull(words);
		var data = new byte[words.Count * 2];
		for (int i = 0; i < words.Count; ++i)
			BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(i * 2), words[i]);
		return data;
	}

	/// <summary>
	/// Globe polygon record of WORLD.DAT (RuleGlobe::loadDat): 4 points (longitude, latitude) in 1/8 degree,
	/// the 4th longitude -1 for triangles, then the texture and an unused word.
	/// </summary>
	public static void WorldPolygon(Stream output, ReadOnlySpan<(int Lon, int Lat)> points, int texture)
	{
		ArgumentNullException.ThrowIfNull(output);
		Span<byte> rec = stackalloc byte[20];
		rec.Clear();
		for (int i = 0; i < 4; ++i)
		{
			var (lon, lat) = i < points.Length ? points[i] : (-1, 0);
			BinaryPrimitives.WriteInt16LittleEndian(rec[(i * 4)..], (short)lon);
			BinaryPrimitives.WriteInt16LittleEndian(rec[(i * 4 + 2)..], (short)lat);
		}
		BinaryPrimitives.WriteInt16LittleEndian(rec[16..], (short)texture);
		output.Write(rec);
	}

	/// <summary>MAP block (BattlescapeGenerator::loadMAP): header y, x, z, then 4 bytes per tile, top level first, x fastest.</summary>
	public static byte[] Map(int sizeX, int sizeY, int sizeZ, Func<int, int, int, (byte Floor, byte West, byte North, byte Object)> tile)
	{
		ArgumentNullException.ThrowIfNull(tile);
		var data = new byte[3 + sizeX * sizeY * sizeZ * 4];
		data[0] = (byte)sizeY;
		data[1] = (byte)sizeX;
		data[2] = (byte)sizeZ;
		int o = 3;
		for (int z = sizeZ - 1; z >= 0; --z)
		{
			for (int y = 0; y < sizeY; ++y)
			{
				for (int x = 0; x < sizeX; ++x)
				{
					var t = tile(x, y, z);
					data[o++] = t.Floor;
					data[o++] = t.West;
					data[o++] = t.North;
					data[o++] = t.Object;
				}
			}
		}
		return data;
	}
}
