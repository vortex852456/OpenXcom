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
// Ported from: src/Interface/ProgressBar.h, src/Interface/ProgressBar.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Progress bar graphic that represents a certain percentage value.
/// Drawn with a coloured border and partially filled inner content.
/// </summary>
public class ProgressBar : Surface
{
	private byte _color, _borderColor;
	private int _value;

	/// <summary>Sets up a blank ProgressBar with the specified size and position.</summary>
	public ProgressBar(int width, int height, int x = 0, int y = 0) : base(width, height, x, y)
	{
		_color = 32;
		_borderColor = 1;
	}

	/// <summary>Changes the color used to draw the border and contents.</summary>
	public override void SetColor(byte color)
	{
		_color = color;
		_redraw = true;
	}

	/// <summary>Returns the color used to draw the ProgressBar.</summary>
	public byte Color => _color;

	/// <summary>Sets the border color for the ProgressBar.</summary>
	public override void SetBorderColor(byte color) => _borderColor = color;

	/// <summary>The value used to draw the inner contents (negative values become 0).</summary>
	public int Value
	{
		get => _value;
		set
		{
			_value = (value < 0) ? 0 : value;
			_redraw = true;
		}
	}

	/// <summary>Draws the bordered ProgressBar filled according to its value.</summary>
	public override void Draw()
	{
		base.Draw();
		// border
		var square = new SDL_Rect(0, 0, Width, Height);
		if (_borderColor != 0)
			DrawRect(ref square, _borderColor);
		else
			DrawRect(ref square, unchecked((byte)(_color + 4)));

		// transparent inner content
		square.x++;
		square.y++;
		square.w -= 2;
		square.h -= 2;
		DrawRect(ref square, 0);

		// non-transparent inner content
		square.w = unchecked((ushort)(square.w * _value / 100));
		DrawRect(ref square, _color);
	}
}
