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
// Ported from: src/Menu/ErrorMessageState.h, src/Menu/ErrorMessageState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Generic window used to display error messages.
/// </summary>
public class ErrorMessageState : State
{
	private Window _window;
	private TextButton _btnOk;
	private Text _txtMessage;

	/// <summary>
	/// Initializes all the elements in an error window.
	/// </summary>
	/// <param name="msg">Text string for the message to display.</param>
	/// <param name="palette">Parent state palette.</param>
	/// <param name="color">Color of the UI controls.</param>
	/// <param name="bg">Background image.</param>
	/// <param name="bgColor">Background color (-1 for Battlescape).</param>
	/// <param name="color2">Secondary text color (0 = none).</param>
	public ErrorMessageState(string msg, ReadOnlySpan<Rgba> palette, byte color, string bg, int bgColor, byte color2 = 0)
	{
		Create(msg, palette, color, bg, bgColor, color2);
	}

	/// <summary>
	/// Creates the elements in an error window.
	/// </summary>
	private void Create(string str, ReadOnlySpan<Rgba> palette, byte color, string bg, int bgColor, byte color2)
	{
		_screen = false;

		// Create objects
		_window = new Window(this, 256, 160, 32, 20, WindowPopup.POPUP_BOTH);
		_btnOk = new TextButton(120, 18, 100, 154);
		_txtMessage = new Text(246, 80, 37, 50);

		// Set palette
		SetStatePalette(palette);
		if (bgColor != -1)
			SetStatePalette(_game.Mod.GetPalette("BACKPALS.DAT").GetColors(Acom.Palette.BlockOffset((byte)bgColor)), Acom.Palette.BackPos, 16);

		Add(_window);
		Add(_btnOk);
		Add(_txtMessage);

		CenterAllSurfaces();

		// Set up objects
		_window.SetColor(color);
		_window.SetBackground(_game.Mod.GetSurface(bg));

		_btnOk.SetColor(color);
		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtMessage.SetColor(color);
		if (color2 != 0) // just in case, probably not needed
		{
			_txtMessage.SetSecondaryColor(color2);
		}
		_txtMessage.Align = TextHAlign.ALIGN_CENTER;
		_txtMessage.VerticalAlign = TextVAlign.ALIGN_MIDDLE;
		_txtMessage.SetBig();
		_txtMessage.SetWordWrap(true);
		_txtMessage.SetText(str);

		if (bgColor == -1)
		{
			_window.SetHighContrast(true);
			_btnOk.SetHighContrast(true);
			_txtMessage.SetHighContrast(true);
		}
	}

	/// <summary>
	/// Closes the window.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}
}
