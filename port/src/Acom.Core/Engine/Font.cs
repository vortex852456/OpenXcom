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
// Ported from: src/Engine/Font.h, src/Engine/Font.cpp

using System.Buffers.Binary;
using System.Text;

namespace Acom.Engine;

/// <summary>One image of a font with its character cell size.</summary>
internal sealed class FontImage(int width, int height, int spacing, Surface surface)
{
	public int Width { get; } = width;

	public int Height { get; } = height;

	public int Spacing { get; } = spacing;

	public Surface Surface { get; } = surface;
}

/// <summary>
/// Takes care of loading and storing each character in a sprite font.
/// Sprite fonts consist of a set of fixed-size characters
/// with the letters ordered in a specific way.
/// </summary>
/// <remarks>
/// Characters are UTF-16 <see cref="char"/>s like the rest of the text code. Characters outside the
/// Basic Multilingual Plane still take their slot in the font image but cannot be looked up.
/// </remarks>
public sealed class Font
{
	/// <summary>Palette of the terminal font: transparent and grey.</summary>
	public static readonly Rgba[] TerminalColors = [new(0, 0, 0, 0), new(185, 185, 185, 255)];

	private readonly List<FontImage> _images = [];
	private readonly Dictionary<char, (int Image, SDL_Rect Rect)> _chars = [];
	private bool _monospace;

	/// <summary>Loads the font from a YAML node; the images come from <c>Language/</c> in the mod file system.</summary>
	public void Load(YamlNodeReader reader) => Load(reader, FileMap.ReadAll);

	/// <summary>Loads the font from a YAML node, reading the image files with <paramref name="readFile"/>.</summary>
	internal void Load(YamlNodeReader reader, Func<string, byte[]> readFile)
	{
		ArgumentNullException.ThrowIfNull(readFile);
		int width = reader["width"].ReadVal(0);
		int height = reader["height"].ReadVal(0);
		int spacing = reader["spacing"].ReadVal(0);
		_monospace = reader["monospace"].ReadVal(_monospace);
		foreach (var imageReader in reader["images"].Children())
		{
			string file = "Language/" + imageReader["file"].ReadVal<string>();
			string chars = imageReader["chars"].ReadVal<string>();
			int imageWidth = imageReader["width"].ReadVal(width), imageHeight = imageReader["height"].ReadVal(height);
			var surface = new Surface(imageWidth, imageHeight);
			surface.LoadImage(readFile(file), file);
			_images.Add(new FontImage(imageWidth, imageHeight, imageReader["spacing"].ReadVal(spacing), surface));
			Init(_images.Count - 1, chars);
		}
	}

	/// <summary>Generates a pre-defined Codepage 437 (MS-DOS terminal) font.</summary>
	public void LoadTerminal()
	{
		_monospace = true;
		// DosFont is a bottom-up 1-bit BMP with a black/white palette; blitting it onto the
		// terminal palette maps black to index 0 and white to index 1, i.e. the bits are the indices.
		var bmp = DosFont.Data;
		int offset = BinaryPrimitives.ReadInt32LittleEndian(bmp[10..]);
		int w = BinaryPrimitives.ReadInt32LittleEndian(bmp[18..]), h = BinaryPrimitives.ReadInt32LittleEndian(bmp[22..]);
		int pitch = (w + 31) / 32 * 4;
		var surface = new Surface(w, h);
		surface.SetPalette(TerminalColors, 0, TerminalColors.Length);
		for (int y = 0; y < h; ++y)
		{
			var row = bmp.Slice(offset + (h - 1 - y) * pitch, pitch);
			for (int x = 0; x < w; ++x)
				surface.SetPixel(x, y, (byte)((row[x >> 3] >> (7 - (x & 7))) & 1));
		}
		_images.Add(new FontImage(9, 16, 0, surface));
		Init(_images.Count - 1, " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~");
	}

	/// <summary>
	/// Calculates the real size and position of each character in
	/// the surface and stores them in SDL_Rect's for future use
	/// by other classes.
	/// </summary>
	/// <param name="index">The index of the surface to use.</param>
	/// <param name="str">A string of characters to map to the surface.</param>
	private void Init(int index, string str)
	{
		var image = _images[index];
		var surface = image.Surface;
		int length = surface.Width / image.Width;
		int i = -1;
		foreach (var rune in str.EnumerateRunes())
		{
			++i;
			int startX = i % length * image.Width;
			int startY = i / length * image.Height;
			SDL_Rect rect;
			if (_monospace)
			{
				rect = new SDL_Rect(startX, startY, image.Width, image.Height);
			}
			else
			{
				int left = -1, right = -1;
				for (int x = startX; x < startX + image.Width; ++x)
				{
					for (int y = startY; y < startY + image.Height && left == -1; ++y)
					{
						if (surface.GetPixel(x, y) != 0)
							left = x;
					}
				}
				for (int x = startX + image.Width - 1; x >= startX; --x)
				{
					for (int y = startY + image.Height; y-- != startY && right == -1;)
					{
						if (surface.GetPixel(x, y) != 0)
							right = x;
					}
				}
				rect = new SDL_Rect(left, startY, right - left + 1, image.Height);
			}
			if (rune.IsBmp)
				_chars[(char)rune.Value] = (index, rect);
		}
	}

	/// <summary>Returns a particular character from the set stored in the font ('?' if unknown).</summary>
	/// <returns>The font's surface with the respective cropping rectangle set up.</returns>
	public SurfaceCrop GetChar(char c)
	{
		if (!_chars.TryGetValue(c, out var f))
			f = _chars['?'];
		var surfaceCrop = _images[f.Image].Surface.GetCrop();
		surfaceCrop.Crop = f.Rect;
		return surfaceCrop;
	}

	/// <summary>Gets the maximum width for any character in the font.</summary>
	public int Width => _images[0].Width;

	/// <summary>Gets the maximum height for any character in the font.</summary>
	public int Height => _images[0].Height;

	/// <summary>
	/// Gets the spacing between any character in the font. This does not refer to character
	/// spacing within the surface, but to the spacing used between successive characters in a line.
	/// </summary>
	public int Spacing => _images[0].Spacing;

	/// <summary>Returns the dimensions of a particular character in the font.</summary>
	/// <returns>Width and Height dimensions (X and Y are set to the same values).</returns>
	public SDL_Rect GetCharSize(char c)
	{
		int w, h;
		if (Unicode.IsPrintable(c))
		{
			if (!_chars.TryGetValue(c, out var f))
				f = _chars['?'];
			var image = _images[f.Image];
			w = f.Rect.w + image.Spacing;
			h = f.Rect.h + image.Spacing;
		}
		else
		{
			if (_monospace)
				w = Width + Spacing;
			else if (c == Unicode.TOK_NBSP)
				w = Width / 4;
			else if (c == '\t')
				w = Width * 3 / 4;
			else
				w = Width / 2;
			h = Height + Spacing;
		}
		// In case anyone mixes them up
		return new SDL_Rect(w, h, w, h);
	}
}
