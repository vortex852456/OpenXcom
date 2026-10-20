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
// Ported from: src/Interface/Bar.h, src/Interface/Bar.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Bar graphic that represents a certain value.
/// Drawn with a coloured border and partially
/// filled content to contrast two values, typically
/// used for showing base and soldier stats.
/// </summary>
public class Bar : Surface
{
	private byte _color, _color2, _borderColor;
	private double _scale, _max, _value, _value2;
	private bool _secondOnTop;

	/// <summary>Sets up a blank bar with the specified size and position.</summary>
	public Bar(int width, int height, int x = 0, int y = 0) : base(width, height, x, y)
	{
		_secondOnTop = true;
	}

	/// <summary>Changes the color used to draw the border and contents.</summary>
	public override void SetColor(byte color)
	{
		_color = color;
		_redraw = true;
	}

	/// <summary>Returns the color used to draw the bar.</summary>
	public byte Color => _color;

	/// <summary>Changes the color used to draw the second contents.</summary>
	public override void SetSecondaryColor(byte color)
	{
		_color2 = color;
		_redraw = true;
	}

	/// <summary>Returns the second color used to draw the bar.</summary>
	public byte SecondaryColor => _color2;

	/// <summary>The scale factor used to draw the bar values (hides the pixel density <see cref="Surface.Scale"/>).</summary>
	public new double Scale
	{
		get => _scale;
		set
		{
			_scale = value;
			_redraw = true;
		}
	}

	/// <summary>The maximum value used to draw the outer border.</summary>
	public double Max
	{
		get => _max;
		set
		{
			_max = value;
			_redraw = true;
		}
	}

	/// <summary>The value used to draw the inner contents (negative values become 0).</summary>
	public double Value
	{
		get => _value;
		set
		{
			_value = (value < 0.0) ? 0.0 : value;
			_redraw = true;
		}
	}

	/// <summary>The value used to draw the second inner contents (negative values become 0).</summary>
	public double Value2
	{
		get => _value2;
		set
		{
			_value2 = (value < 0.0) ? 0.0 : value;
			_redraw = true;
		}
	}

	/// <summary>Defines whether the second value should be drawn on top. Default this is true.</summary>
	public void SetSecondValueOnTop(bool onTop) => _secondOnTop = onTop;

	/// <summary>Uint16 cast of a double (via int, like x86).</summary>
	private static ushort ToUint16(double d) => unchecked((ushort)(int)d);

	/// <summary>Draws the bordered bar filled according to its values.</summary>
	public override void Draw()
	{
		base.Draw();
		var square = new SDL_Rect();

		square.x = 0;
		square.y = 0;
		square.w = unchecked((ushort)(ToUint16(_scale * _max) + 1));
		square.h = (ushort)Height;

		if (_borderColor != 0)
			DrawRect(square, _borderColor);
		else
			DrawRect(square, unchecked((byte)(_color + 4)));

		square.y++;
		square.w--;
		square.h -= 2;

		DrawRect(square, 0);

		if (_secondOnTop)
		{
			square.w = ToUint16(_scale * _value);
			DrawRect(square, _color);
			square.w = ToUint16(_scale * _value2);
			DrawRect(square, _color2);
		}
		else
		{
			square.w = ToUint16(_scale * _value2);
			DrawRect(square, _color2);
			square.w = ToUint16(_scale * _value);
			DrawRect(square, _color);
		}
	}

	/// <summary>Sets the outline color for the bar.</summary>
	public override void SetBorderColor(byte color) => _borderColor = color;
}
