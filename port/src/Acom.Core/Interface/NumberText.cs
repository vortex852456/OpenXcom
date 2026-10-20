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
// Ported from: src/Interface/NumberText.h, src/Interface/NumberText.cpp
#nullable disable

using System.Globalization;

namespace Acom;

/// <summary>
/// Number digits displayed on the screen.
/// Takes a number and displays it using a simple hard-coded font.
/// </summary>
public class NumberText : Surface
{
	/// <summary>Pixels (x, y pairs) of the 3x5 digits 0-9, in the order the C++ constructor sets them.</summary>
	private static readonly int[][] s_digits =
	[
		[0, 0, 1, 0, 2, 0, 0, 1, 0, 2, 0, 3, 2, 1, 2, 2, 2, 3, 0, 4, 1, 4, 2, 4],
		[1, 0, 1, 1, 1, 2, 1, 3, 0, 4, 1, 4, 2, 4, 0, 1],
		[0, 0, 1, 0, 2, 0, 2, 1, 0, 2, 1, 2, 2, 2, 0, 3, 0, 4, 1, 4, 2, 4],
		[0, 0, 1, 0, 2, 0, 2, 1, 2, 2, 2, 3, 0, 2, 1, 2, 0, 4, 1, 4, 2, 4],
		[0, 0, 0, 1, 0, 2, 1, 2, 2, 0, 2, 1, 2, 2, 2, 3, 2, 4],
		[0, 0, 1, 0, 2, 0, 0, 1, 0, 2, 1, 2, 2, 2, 2, 3, 0, 4, 1, 4, 2, 4],
		[0, 0, 1, 0, 2, 0, 0, 1, 0, 2, 1, 2, 2, 2, 0, 3, 2, 3, 0, 4, 1, 4, 2, 4],
		[0, 0, 1, 0, 2, 0, 2, 1, 2, 2, 2, 3, 2, 4],
		[0, 0, 1, 0, 2, 0, 0, 1, 0, 2, 0, 3, 2, 1, 2, 2, 2, 3, 1, 2, 0, 4, 1, 4, 2, 4],
		[0, 0, 1, 0, 2, 0, 0, 1, 0, 2, 2, 1, 2, 2, 2, 3, 1, 2, 0, 4, 1, 4, 2, 4],
	];

	private uint _value;
	private readonly Surface[] _chars = new Surface[10];
	private readonly Surface[] _borderedChars = new Surface[10];
	private bool _bordered;
	private byte _color;

	/// <summary>Sets up a blank number text.</summary>
	/// <param name="width">Width in pixels.</param>
	/// <param name="height">Height in pixels.</param>
	/// <param name="x">X position in pixels.</param>
	/// <param name="y">Y position in pixels.</param>
	public NumberText(int width, int height, int x = 0, int y = 0) : base(width, height, x, y)
	{
		for (int i = 0; i < 10; ++i)
		{
			_chars[i] = new Surface(3, 5);
			_chars[i].Lock();
			var pixels = s_digits[i];
			for (int p = 0; p < pixels.Length; p += 2)
				_chars[i].SetPixel(pixels[p], pixels[p + 1], 1);
			_chars[i].Unlock();
		}

		for (int i = 0; i < 10; ++i)
		{
			_borderedChars[i] = new Surface(5, 7);
			// give it a border
			// this is the "darker" shade that goes in the corners.
			for (int j = 0; j <= 2; j += 2)
			{
				for (int k = 0; k <= 2; k += 2)
					_chars[i].BlitNShade(_borderedChars[i], j, k, 11);
			}
			// this is the "slightly darker" version that goes in four cardinals.
			for (int z = 0; z <= 2; z += 2)
			{
				_chars[i].BlitNShade(_borderedChars[i], z, 1, 8);
				_chars[i].BlitNShade(_borderedChars[i], 1, z, 8);
			}
			// and finally the number itself
			_chars[i].BlitNShade(_borderedChars[i], 1, 1, 0);
		}
	}

	/// <summary>The value used to render the number.</summary>
	public uint Value
	{
		get => _value;
		set
		{
			_value = value;
			_redraw = true;
		}
	}

	/// <summary>Changes the color used to render the number.</summary>
	public override void SetColor(byte color)
	{
		_color = color;
		_redraw = true;
	}

	/// <summary>Returns the color used to render the number.</summary>
	public byte Color => _color;

	/// <summary>Replaces a certain amount of colors in the number text palette.</summary>
	public override void SetPalette(ReadOnlySpan<Rgba> colors, int firstcolor = 0, int ncolors = 256)
	{
		base.SetPalette(colors, firstcolor, ncolors);
		for (int i = 0; i < 10; ++i)
		{
			_chars[i].SetPalette(colors, firstcolor, ncolors);
			_borderedChars[i].SetPalette(colors, firstcolor, ncolors);
		}
	}

	/// <summary>Draws all the digits in the number.</summary>
	public override void Draw()
	{
		base.Draw();
		string s = _value.ToString(CultureInfo.InvariantCulture);
		int x = 0;
		if (!_bordered)
		{
			foreach (char i in s)
			{
				_chars[i - '0'].BlitNShade(this, x, 0);
				x += _chars[i - '0'].Width + 1;
			}
		}
		else
		{
			foreach (char i in s)
			{
				_borderedChars[i - '0'].BlitNShade(this, x, 0);
				x += _chars[i - '0'].Width + 1; // no this isn't a typo, i want to use the same spacing regardless.
			}
		}

		Offset(_color);
	}

	/// <summary>Sets this numbertext to have a border or not.</summary>
	public void SetBordered(bool bordered) => _bordered = bordered;
}
