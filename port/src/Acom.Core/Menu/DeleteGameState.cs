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
// Ported from: src/Menu/DeleteGameState.h, src/Menu/DeleteGameState.cpp
#nullable disable
#pragma warning disable CA1822 // UI handlers stay instance members like in C++ (State._game is static here)

namespace Acom;

/// <summary>Window used to confirm save game deletion.</summary>
public class DeleteGameState : State
{
	private TextButton _btnNo, _btnYes;
	private Window _window;
	private Text _txtMessage;
	private string _filename;
	private OptionsOrigin _origin;

	/// <summary>Initializes all the elements in the Confirmation screen.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	/// <param name="save">Name of the save file to delete.</param>
	public DeleteGameState(OptionsOrigin origin, string save)
	{
		_origin = origin;
		_filename = Options.GetMasterUserFolder() + save;
		_screen = false;

		// Create objects
		_window = new Window(this, 256, 100, 32, 50, WindowPopup.POPUP_BOTH);
		_btnYes = new TextButton(60, 18, 60, 122);
		_btnNo = new TextButton(60, 18, 200, 122);
		_txtMessage = new Text(246, 32, 37, 70);

		// Set palette
		SetInterface("saveMenus", false, _game.SavedGame?.SavedBattle);

		Add(_window, "confirmDelete", "saveMenus");
		Add(_btnYes, "confirmDelete", "saveMenus");
		Add(_btnNo, "confirmDelete", "saveMenus");
		Add(_txtMessage, "confirmDelete", "saveMenus");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "saveMenus");

		_btnYes.Text = Tr("STR_YES");
		_btnYes.OnMouseClick(BtnYesClick);
		_btnYes.OnKeyboardPress(BtnYesClick, Options.keyOk);

		_btnNo.Text = Tr("STR_NO");
		_btnNo.OnMouseClick(BtnNoClick);
		_btnNo.OnKeyboardPress(BtnNoClick, Options.keyCancel);

		_txtMessage.Align = TextHAlign.ALIGN_CENTER;
		_txtMessage.SetBig();
		_txtMessage.SetWordWrap(true);
		_txtMessage.SetText(Tr("STR_IS_IT_OK_TO_DELETE_THE_SAVED_GAME"));

		if (_origin == OptionsOrigin.OPT_BATTLESCAPE)
		{
			ApplyBattlescapeTheme("saveMenus");
		}
	}

	/// <summary>Cancels the deletion.</summary>
	public void BtnNoClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>Deletes the save file.</summary>
	public void BtnYesClick(Action action)
	{
		_game.PopState();
		if (!CrossPlatform.DeleteFile(_filename))
		{
			string error = Tr("STR_DELETE_UNSUCCESSFUL");
			var errorMessages = _game.Mod.GetInterface("errorMessages");
			if (_origin != OptionsOrigin.OPT_BATTLESCAPE)
				_game.PushState(new ErrorMessageState(error, _palette, unchecked((byte)errorMessages.GetElement("geoscapeColor").color), "BACK01.SCR", errorMessages.GetElement("geoscapePalette").color));
			else
				_game.PushState(new ErrorMessageState(error, _palette, unchecked((byte)errorMessages.GetElement("battlescapeColor").color), "TAC00.SCR", errorMessages.GetElement("battlescapePalette").color));
		}
	}
}
