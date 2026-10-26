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
// Ported from: src/Menu/ConfirmLoadState.h, src/Menu/ConfirmLoadState.cpp
#nullable disable
#pragma warning disable CA1822 // UI handlers stay instance members like in C++ (State._game is static here)

namespace Acom;

/// <summary>Confirms loading a save with missing content.</summary>
public class ConfirmLoadState : State
{
	private OptionsOrigin _origin;
	private string _fileName;
	private TextButton _btnYes, _btnNo;
	private Window _window;
	private Text _txtText;

	/// <summary>Initializes all the elements in the Confirm Load screen.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	/// <param name="fileName">Name of the save file without extension.</param>
	public ConfirmLoadState(OptionsOrigin origin, string fileName)
	{
		_origin = origin;
		_fileName = fileName;
		_screen = false;

		// Create objects
		_window = new Window(this, 216, 100, 52, 50, WindowPopup.POPUP_BOTH);
		_btnYes = new TextButton(50, 20, 70, 120);
		_btnNo = new TextButton(50, 20, 200, 120);
		_txtText = new Text(204, 58, 58, 60);

		// Set palette
		SetInterface("saveMenus", false, _game.SavedGame?.SavedBattle);

		Add(_window, "confirmLoad", "saveMenus");
		Add(_btnYes, "confirmLoad", "saveMenus");
		Add(_btnNo, "confirmLoad", "saveMenus");
		Add(_txtText, "confirmLoad", "saveMenus");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "saveMenus");

		_btnYes.Text = Tr("STR_YES");
		_btnYes.OnMouseClick(BtnYesClick);
		_btnYes.OnKeyboardPress(BtnYesClick, Options.keyOk);

		_btnNo.Text = Tr("STR_NO");
		_btnNo.OnMouseClick(BtnNoClick);
		_btnNo.OnKeyboardPress(BtnNoClick, Options.keyCancel);

		_txtText.Align = TextHAlign.ALIGN_CENTER;
		_txtText.SetBig();
		_txtText.SetWordWrap(true);
		_txtText.SetText(Tr("STR_MISSING_CONTENT_PROMPT"));

		if (_origin == OptionsOrigin.OPT_BATTLESCAPE)
		{
			ApplyBattlescapeTheme("saveMenus");
		}
	}

	/// <summary>Proceed to load the save.</summary>
	public void BtnYesClick(Action action)
	{
		_game.PopState();
		_game.PushState(new LoadGameState(_origin, _fileName, _palette));
	}

	/// <summary>Abort loading and return to save list.</summary>
	public void BtnNoClick(Action action)
	{
		_game.PopState();
	}
}
