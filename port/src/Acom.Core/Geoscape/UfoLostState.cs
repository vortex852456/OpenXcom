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
// Ported from: src/Geoscape/UfoLostState.h, src/Geoscape/UfoLostState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Notifies the player when a targeted UFO
/// goes outside radar range.
/// </summary>
public class UfoLostState : State
{
	private readonly TextButton _btnOk;
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly string _id;

	/// <summary>
	/// Initializes all the elements in the Ufo Lost window.
	/// </summary>
	/// <param name="id">Name of the UFO.</param>
	public UfoLostState(string id)
	{
		_id = id;
		_screen = false;

		// Create objects
		_window = new Window(this, 192, 104, 32, 48, WindowPopup.POPUP_BOTH);
		_btnOk = new TextButton(60, 12, 98, 112);
		_txtTitle = new Text(160, 32, 48, 72);

		// Set palette
		SetInterface("UFOLost");

		Add(_window, "window", "UFOLost");
		Add(_btnOk, "button", "UFOLost");
		Add(_txtTitle, "text", "UFOLost");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "UFOLost");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		string s = _id;
		s += '\n';
		s += Tr("STR_TRACKING_LOST");
		_txtTitle.SetText(s);
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}
}
