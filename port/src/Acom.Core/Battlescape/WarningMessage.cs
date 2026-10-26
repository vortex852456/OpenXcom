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
// Ported from: src/Battlescape/WarningMessage.h, src/Battlescape/WarningMessage.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Coloured box with text inside that fades out after it is displayed.
/// Used to display warning/error messages on the Battlescape.
/// </summary>
public class WarningMessage : Surface
{
	private readonly Text _text;
	private readonly Timer _timer;
	private byte _color;
	private int _fade;

	/// <summary>
	/// Sets up a blank warning message with the specified size and position.
	/// </summary>
	public WarningMessage(int width, int height, int x = 0, int y = 0) : base(width, height, x, y)
	{
		_color = 0;
		_fade = 0;
		_text = new Text(width, height, 0, 0);
		_text.SetHighContrast(true);
		_text.Align = TextHAlign.ALIGN_CENTER;
		_text.VerticalAlign = TextVAlign.ALIGN_MIDDLE;
		_text.SetWordWrap(true);

		_timer = new Timer(50);
		_timer.OnTimer((SurfaceHandler)Fade);

		Visible = false;
	}

	/// <summary>Changes the color for the message background.</summary>
	public override void SetColor(byte color)
	{
		_color = color;
	}

	/// <summary>Changes the color for the message text.</summary>
	public void SetTextColor(byte color)
	{
		_text.SetColor(color);
	}

	/// <summary>Changes the various resources needed for text rendering.</summary>
	public override void InitText(Font big, Font small, Language lang)
	{
		_text.InitText(big, small, lang);
	}

	/// <summary>Replaces a certain amount of colors in the surface's palette.</summary>
	public override void SetPalette(ReadOnlySpan<Rgba> colors, int firstcolor = 0, int ncolors = 256)
	{
		base.SetPalette(colors, firstcolor, ncolors);
		_text.SetPalette(colors, firstcolor, ncolors);
	}

	/// <summary>Displays the warning message.</summary>
	/// <param name="msg">Message string.</param>
	/// <param name="time">How long message will be visible.</param>
	public void ShowMessage(string msg, int time = 2)
	{
		_text.SetText(msg);
		_fade = time * 12;
		_redraw = true;
		Visible = true;
		_timer.Start();
	}

	/// <summary>Keeps the animation timers running.</summary>
	public override void Think()
	{
		_timer.Think(null, this);
	}

	/// <summary>Plays the message fade animation.</summary>
	public void Fade()
	{
		_fade--;
		_redraw = true;
		if (_fade == 0)
		{
			Visible = false;
			_timer.Stop();
		}
	}

	/// <summary>Draws the warning message.</summary>
	public override void Draw()
	{
		base.Draw();
		DrawRect(0, 0, (short)Width, (short)Height, (byte)(_color + Math.Clamp(24 - _fade, 0, 12)));
		_text.Blit(this);
	}
}
