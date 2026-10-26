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
// Ported from: src/Battlescape/InfoboxOKState.h, src/Battlescape/InfoboxOKState.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Notifies the player about things like soldiers going unconscious or dying from wounds.
/// </summary>
public class InfoboxOKState : State
{
	private readonly Frame _frame;
	private readonly Text _txtTitle;
	private readonly TextButton _btnOk;

	/// <summary>
	/// Initializes all the elements.
	/// </summary>
	/// <param name="msg">Message string.</param>
	public InfoboxOKState(string msg)
	{
		_screen = false;

		// Create objects
		_frame = new Frame(261, 89, 30, 48);
		_btnOk = new TextButton(120, 18, 100, 112);
		_txtTitle = new Text(255, 61, 33, 51);

		// Set palette
		_game.SavedGame.SavedBattle.SetPaletteByDepth(this);

		Add(_frame, "infoBoxOK", "battlescape");
		Add(_btnOk, "infoBoxOKButton", "battlescape");
		Add(_txtTitle, "infoBoxOK", "battlescape");

		CenterAllSurfaces();

		// Set up objects
		_frame.SetThickness(3);
		_frame.SetHighContrast(true);

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnOk.SetHighContrast(true);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.VerticalAlign = TextVAlign.ALIGN_MIDDLE;
		_txtTitle.SetHighContrast(true);
		_txtTitle.SetWordWrap(true);
		_txtTitle.SetText(msg);

		_game.Cursor.Visible = true;
	}

	/// <summary>Returns to the previous screen.</summary>
#pragma warning disable CA1822 // instance handler as in C++
	public void BtnOkClick(Action action)
#pragma warning restore CA1822
	{
		_game.PopState();
	}
}
