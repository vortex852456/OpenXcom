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
// Ported from: src/Battlescape/BattlescapeMessage.h, src/Battlescape/BattlescapeMessage.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Generic window used to display messages
/// over the Battlescape map.
/// </summary>
public class BattlescapeMessage : Surface
{
	private const int HORIZONTAL_OFFSET = 200;
	private const int VERTICAL_OFFSET = 20;

	private readonly Window _window;
	private readonly Text _text, _txtThinking;
	private readonly ProgressBar _progressBar;

	/// <summary>
	/// Sets up a blank Battlescape message with the specified size and position.
	/// </summary>
	public BattlescapeMessage(int width, int height, int x = 0, int y = 0) : base(width, height, x, y)
	{
		_window = new Window(null, width, height, x, y, WindowPopup.POPUP_NONE);
		_window.SetColor((byte)(Acom.Palette.BlockOffset(0) - 1));
		_window.SetHighContrast(true);

		_text = new Text(width, height, x, y);
		_text.SetColor((byte)(Acom.Palette.BlockOffset(0) - 1));
		_text.Align = TextHAlign.ALIGN_CENTER;
		_text.VerticalAlign = TextVAlign.ALIGN_MIDDLE;
		_text.SetHighContrast(true);

		_txtThinking = new Text(102, 9, HORIZONTAL_OFFSET, VERTICAL_OFFSET - 10);
		_txtThinking.SetColor((byte)(Acom.Palette.BlockOffset(0) - 1));
		_txtThinking.Align = TextHAlign.ALIGN_CENTER;
		_txtThinking.SetHighContrast(true);

		_progressBar = new ProgressBar(102, 5, HORIZONTAL_OFFSET, VERTICAL_OFFSET);

		if (Options.oxceDisableThinkingProgressBar)
		{
			_txtThinking.Visible = false;
			_progressBar.Visible = false;
		}
	}

	/// <summary>Changes the position of the surface in the X axis.</summary>
	public override int X
	{
		set
		{
			base.X = value;
			_window.X = value;
			_text.X = value;
			_txtThinking.X = value + HORIZONTAL_OFFSET;
			_progressBar.X = value + HORIZONTAL_OFFSET;
		}
	}

	/// <summary>Changes the position of the surface in the Y axis.</summary>
	public override int Y
	{
		set
		{
			base.Y = value;
			_window.Y = value;
			_text.Y = value;
			_txtThinking.Y = value + VERTICAL_OFFSET - 10;
			_progressBar.Y = value + VERTICAL_OFFSET;
		}
	}

	/// <summary>Changes the message background.</summary>
	public void SetBackground(Surface background)
	{
		_window.SetBackground(background);
	}

	/// <summary>Changes the message text.</summary>
	public void SetText(string message, string message2)
	{
		_text.SetText(message);
		_txtThinking.SetText(message2);
	}

	/// <summary>Changes the progress bar value.</summary>
	public void SetProgressValue(int progress)
	{
		_progressBar.Value = progress;
	}

	/// <summary>Changes the various resources needed for text rendering.</summary>
	public override void InitText(Font big, Font small, Language lang)
	{
		_text.InitText(big, small, lang);
		_text.SetBig();
		_txtThinking.InitText(big, small, lang);
	}

	/// <summary>Replaces a certain amount of colors in the surface's palette.</summary>
	public override void SetPalette(ReadOnlySpan<Rgba> colors, int firstcolor = 0, int ncolors = 256)
	{
		base.SetPalette(colors, firstcolor, ncolors);
		_window.SetPalette(colors, firstcolor, ncolors);
		_text.SetPalette(colors, firstcolor, ncolors);
		_txtThinking.SetPalette(colors, firstcolor, ncolors);
		_progressBar.SetPalette(colors, firstcolor, ncolors);
	}

	/// <summary>Blits the warning message.</summary>
	public override void Blit(Surface surface)
	{
		base.Blit(surface);
		_window.Blit(surface);
		_text.Blit(surface);
		_txtThinking.Blit(surface);
		_progressBar.Blit(surface);
	}

	/// <summary>Special handling for setting the height of the battlescape message.</summary>
	public override void SetHeight(int height)
	{
		base.SetHeight(height);
		_window.SetHeight(height);
		_text.SetHeight(height);
	}

	/// <summary>Sets the text color of the battlescape message.</summary>
	public void SetTextColor(byte color)
	{
		_text.SetColor(color);
		_txtThinking.SetColor(color);
	}

	/// <summary>Sets the colors of the progress bar.</summary>
	public void SetProgressBarColor(byte color, byte borderColor)
	{
		_progressBar.SetColor(color);
		_progressBar.SetBorderColor(borderColor);
	}
}
