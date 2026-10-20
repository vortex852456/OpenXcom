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
// Ported from: src/Engine/Palette.h, src/Engine/Palette.cpp
// Port status: partial (file access goes through streams until FileMap/CrossPlatform are ported).

using System.Globalization;
using System.Runtime.InteropServices;

namespace Acom.Engine;

/// <summary>One palette entry, layout-compatible with SDL_Color (alpha takes the place of <c>unused</c>).</summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Rgba(byte R, byte G, byte B, byte A = 255);

/// <summary>
/// Container for palettes (sets of 8bpp colors).
/// Provides shortcuts for common tasks to make code more readable.
/// </summary>
public sealed class Palette
{
	/// <summary>Position of the background colours in a palette.</summary>
	public const int BackPos = 224;

	private Rgba[]? _colors;

	/// <summary>Gets the number of colors in the palette.</summary>
	public int ColorCount => _colors?.Length ?? 0;

	/// <summary>Gets the colors of the palette (all of them, starting at index 0).</summary>
	public Rgba[] Colors => _colors ?? throw new InvalidOperationException("Palette is not initialized");

	/// <summary>
	/// Loads a palette in X-Com DAT format (6-bit RGB triplets).
	/// </summary>
	/// <param name="stream">Palette data.</param>
	/// <param name="ncolors">Number of colors to load.</param>
	/// <param name="offset">Byte offset of the palette inside the data.</param>
	public void LoadDat(Stream stream, int ncolors, int offset = 0)
	{
		ArgumentNullException.ThrowIfNull(stream);
		if (_colors != null)
			throw new InvalidOperationException("loadDat can be run only once");
		_colors = new Rgba[ncolors];
		stream.Seek(offset, SeekOrigin.Begin);
		Span<byte> value = stackalloc byte[3];
		for (int i = 0; i < ncolors && stream.ReadAtLeast(value, 3, throwOnEndOfStream: false) == 3; ++i)
		{
			// Correct X-Com colors to RGB colors
			_colors[i] = new Rgba((byte)(value[0] * 4), (byte)(value[1] * 4), (byte)(value[2] * 4));
		}
		_colors[0] = _colors[0] with { A = 0 };
	}

	/// <summary>Initializes an all black palette.</summary>
	public void InitBlack()
	{
		if (_colors != null)
			throw new InvalidOperationException("initBlack can be run only once");
		_colors = new Rgba[256];
		for (int i = 0; i < _colors.Length; ++i)
			_colors[i] = new Rgba(0, 0, 0);
		_colors[0] = _colors[0] with { A = 0 };
	}

	/// <summary>Copies the RGB values of another palette.</summary>
	public void CopyFrom(Palette srcPal)
	{
		ArgumentNullException.ThrowIfNull(srcPal);
		for (int i = 0; i < srcPal.ColorCount; i++)
		{
			var c = srcPal.Colors[i];
			SetColor(i, c.R, c.G, c.B);
		}
	}

	/// <summary>Gets the colors starting at a certain offset.</summary>
	public ReadOnlySpan<Rgba> GetColors(int offset = 0) => Colors.AsSpan(offset);

	/// <summary>Initializes the palette from a set of colors.</summary>
	/// <remarks>
	/// The C++ version nudged duplicate blacks to avoid SDL 1.2 colour matching turning them
	/// transparent. This port blits palette indices directly, so that workaround is not needed.
	/// </remarks>
	public void SetColors(ReadOnlySpan<Rgba> pal, int ncolors)
	{
		if (_colors != null)
			throw new InvalidOperationException("setColors can be run only once");
		_colors = new Rgba[ncolors];
		for (int i = 0; i < ncolors; ++i)
			_colors[i] = new Rgba(pal[i].R, pal[i].G, pal[i].B);
		_colors[0] = _colors[0] with { A = 0 };
	}

	/// <summary>Sets the RGB value of one color (alpha unchanged).</summary>
	public void SetColor(int index, int r, int g, int b)
	{
		Colors[index] = Colors[index] with { R = (byte)r, G = (byte)g, B = (byte)b };
	}

	/// <summary>Same as <see cref="SetColor"/>; kept for parity with the C++ API.</summary>
	public void CopyColor(int index, int r, int g, int b) => SetColor(index, r, g, b);

	/// <summary>Converts a palette color to a 32-bit RGBA value (0xRRGGBBAA).</summary>
	public static uint GetRGBA(ReadOnlySpan<Rgba> pal, byte color)
	{
		var c = pal[color];
		return ((uint)c.R << 24) | ((uint)c.G << 16) | ((uint)c.B << 8) | 0xFFu;
	}

	/// <summary>Position of a palette inside an X-Com palette file (each is a 768-byte chunk plus 6 bytes).</summary>
	public static int PalOffset(int palette) => palette * (768 + 6);

	/// <summary>Position of a color block in a palette (usually 16-color gradients).</summary>
	public static byte BlockOffset(byte block) => (byte)(block * 16);

	/// <summary>Writes the palette as a RIFF PAL file.</summary>
	public void SavePal(Stream output)
	{
		ArgumentNullException.ThrowIfNull(output);
		short count = (short)ColorCount;
		using var w = new BinaryWriter(output, System.Text.Encoding.ASCII, leaveOpen: true);
		w.Write("RIFF"u8);
		w.Write(4 + 4 + 4 + 4 + 2 + 2 + count * 4);
		w.Write("PAL "u8);
		w.Write("data"u8);
		w.Write(count * 4 + 4);
		w.Write((short)0x0300);
		w.Write(count);
		foreach (var c in Colors)
		{
			w.Write(c.R);
			w.Write(c.G);
			w.Write(c.B);
			w.Write((byte)0);
		}
	}

	/// <summary>Writes the palette as a ruleset snippet (customPalettes).</summary>
	public void SavePalMod(TextWriter output, string type, string target)
	{
		ArgumentNullException.ThrowIfNull(output);
		output.Write("customPalettes:\n");
		output.Write($"  - type: {type}\n");
		output.Write($"    target: {target}\n");
		output.Write("    palette:\n");
		for (int i = 0; i < ColorCount; ++i)
		{
			var c = Colors[i];
			output.Write(string.Create(CultureInfo.InvariantCulture, $"      {i}: [{c.R},{c.G},{c.B}]\n"));
		}
	}

	/// <summary>Writes the palette in JASC-PAL text format.</summary>
	public void SavePalJasc(TextWriter output)
	{
		ArgumentNullException.ThrowIfNull(output);
		output.Write("JASC-PAL\n0100\n256\n");
		foreach (var c in Colors)
			output.Write(string.Create(CultureInfo.InvariantCulture, $"{c.R} {c.G} {c.B}\n"));
	}
}
