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
// Ported from: src/Interface/Cursor.h, src/Interface/Cursor.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Mouse cursor that replaces the system cursor.
/// Drawn as a shaded triangle-like shape, automatically
/// matches the mouse coordinates.
/// </summary>
public class Cursor : Surface
{
	private byte _color;

	/// <summary>
	/// Sets up a cursor with the specified size and position
	/// and hides the system cursor.
	/// </summary>
	/// <remarks>It's a 9x13 shape, the size parameters are just there for inheritance.</remarks>
	public Cursor(int width, int height, int x = 0, int y = 0) : base(width, height, x, y)
	{
	}

	/// <summary>Automatically updates the cursor position when the mouse moves.</summary>
	/// <param name="action">Pointer to an action.</param>
	public void Handle(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		var ev = action.Details;
		if (ev.type == SDLEventType.SDL_MOUSEMOTION)
		{
			X = (int)Math.Floor((ev.motion.x - action.LeftBlackBand) / action.XScale);
			Y = (int)Math.Floor((ev.motion.y - action.TopBlackBand) / action.YScale);
		}
	}

	/// <summary>Changes the cursor's base color.</summary>
	public override void SetColor(byte color)
	{
		_color = color;
		_redraw = true;
	}

	/// <summary>Returns the cursor's base color.</summary>
	public byte Color => _color;

	/// <summary>Draws a pointer-shaped cursor graphic.</summary>
	public override void Draw()
	{
		base.Draw();
		byte color = _color;
		int x1 = 0, y1 = 0, x2 = Width - 1, y2 = Height - 1;

		Lock();
		for (int i = 0; i < 4; ++i)
		{
			DrawLine(x1, y1, x1, y2, color);
			DrawLine(x1, y1, x2, Width - 1, color);
			x1++;
			y1 += 2;
			y2--;
			x2--;
			color++;
		}
		SetPixel(4, 8, --color);
		Unlock();
	}
}
