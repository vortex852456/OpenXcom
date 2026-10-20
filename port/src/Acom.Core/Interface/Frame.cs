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
// Ported from: src/Interface/Frame.h, src/Interface/Frame.cpp
#nullable disable

namespace Acom;

/// <summary>Fancy frame border thing used for windows and other elements.</summary>
public class Frame : Surface
{
	private byte _color, _bg;
	private int _thickness;
	private bool _contrast;

	/// <summary>Sets up a blank frame with the specified size and position.</summary>
	public Frame(int width, int height, int x = 0, int y = 0) : base(width, height, x, y)
	{
		_thickness = 5;
	}

	/// <summary>Changes the color used to draw the shaded border.</summary>
	public override void SetColor(byte color)
	{
		_color = color;
		_redraw = true;
	}

	/// <summary>Returns the color used to draw the shaded border.</summary>
	public byte Color => _color;

	/// <summary>
	/// Changes the color used to draw the shaded border.
	/// only really to be used in conjunction with the State add()
	/// function as a convenience wrapper to avoid ugly hacks on that end
	/// better to have them here!
	/// </summary>
	public override void SetBorderColor(byte color) => SetColor(color);

	/// <summary>Changes the color used to draw the background.</summary>
	public override void SetSecondaryColor(byte color)
	{
		_bg = color;
		_redraw = true;
	}

	/// <summary>Returns the color used to draw the background.</summary>
	public byte SecondaryColor => _bg;

	/// <summary>Enables/disables high contrast color. Mostly used for Battlescape UI.</summary>
	public override void SetHighContrast(bool contrast)
	{
		_contrast = contrast;
		_redraw = true;
	}

	/// <summary>Changes the thickness of the border to draw.</summary>
	public void SetThickness(int thickness)
	{
		_thickness = thickness;
		_redraw = true;
	}

	/// <summary>
	/// Draws the bordered frame with a graphic background.
	/// The background never moves with the frame, it's
	/// always aligned to the top-left corner of the screen
	/// and cropped to fit the inside area.
	/// </summary>
	public override void Draw()
	{
		base.Draw();
		var square = new SDL_Rect(0, 0, Width, Height);

		int mul = 1;
		if (_contrast)
			mul = 2;

		// _color denotes our middle line color, so we start (half the thickness times the multiplier) steps darker and build up
		byte color = unchecked((byte)(_color + ((1 + _thickness) * mul) / 2));
		// we want the darkest version of this colour to outline any thick borders
		byte darkest = unchecked((byte)(Acom.Palette.BlockOffset((byte)(_color / 16)) + 15));
		for (int i = 0; i < _thickness; ++i)
		{
			if (_thickness > 5 && (i == 0 || i == _thickness - 1))
				DrawRect(ref square, darkest);
			else
				DrawRect(ref square, color);
			if (i < _thickness / 2)
				color = unchecked((byte)(color - 1 * mul));
			else
				color = unchecked((byte)(color + 1 * mul));
			square.x++;
			square.y++;
			if (square.w >= 2)
				square.w -= 2;
			else
				square.w = 1;

			if (square.h >= 2)
				square.h -= 2;
			else
				square.h = 1;
		}
		DrawRect(ref square, _bg);
	}
}
