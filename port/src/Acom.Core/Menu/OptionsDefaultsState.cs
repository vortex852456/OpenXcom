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
// Ported from: src/Menu/OptionsDefaultsState.h, src/Menu/OptionsDefaultsState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>Confirmation window when restoring the default game options.</summary>
public class OptionsDefaultsState : State
{
	private readonly OptionsOrigin _origin;
	private readonly OptionsBaseState _state;
	private readonly Window _window;
	private readonly TextButton _btnYes, _btnNo;
	private readonly Text _txtTitle;

	/// <summary>Initializes all the elements in the Restore Defaults screen.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	/// <param name="state">Pointer to the base Options state.</param>
	public OptionsDefaultsState(OptionsOrigin origin, OptionsBaseState state)
	{
		_origin = origin;
		_state = state;
		_screen = false;

		// Create objects
		_window = new Window(this, 256, 100, 32, 50, WindowPopup.POPUP_BOTH);
		_btnYes = new TextButton(60, 18, 60, 122);
		_btnNo = new TextButton(60, 18, 200, 122);
		_txtTitle = new Text(246, 32, 37, 70);

		// Set palette
		SetInterface("optionsMenu", false, _game.SavedGame?.SavedBattle);

		Add(_window, "confirmDefaults", "optionsMenu");
		Add(_btnYes, "confirmDefaults", "optionsMenu");
		Add(_btnNo, "confirmDefaults", "optionsMenu");
		Add(_txtTitle, "confirmDefaults", "optionsMenu");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "optionsMenu");

		_btnYes.Text = Tr("STR_YES");
		_btnYes.OnMouseClick(BtnYesClick);
		_btnYes.OnKeyboardPress(BtnYesClick, Options.keyOk);

		_btnNo.Text = Tr("STR_NO");
		_btnNo.OnMouseClick(BtnNoClick);
		_btnNo.OnKeyboardPress(BtnNoClick, Options.keyCancel);

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetBig();
		_txtTitle.SetWordWrap(true);
		_txtTitle.SetText(Tr("STR_RESTORE_DEFAULTS_QUESTION"));

		if (_origin == OptionsOrigin.OPT_BATTLESCAPE)
		{
			ApplyBattlescapeTheme("optionsMenu");
		}
	}

	/// <summary>Restores the default options.</summary>
	public void BtnYesClick(Action action)
	{
		Options.ResetDefault(false);
		Options.Save(true);
		if (_game.IsCtrlPressed())
		{
			// my development/debug defaults
			Options.debug = true;
			Options.language = "en-US";
			Options.musicVolume = 15;
			Options.soundVolume = 15;
			Options.uiVolume = 15;
			Options.keyFps = SDLKey.SDLK_UNKNOWN;
			Options.playIntro = false;
			Options.autosave = false;
			Options.showFundsOnGeoscape = true;
			Options.battleFireSpeed = 16;
			Options.battleXcomSpeed = 1;
			Options.battleNotifyDeath = true;
			Options.battleNewPreviewPath = PathPreview.PATH_ARROW_TU;
			Options.battleUFOExtenderAccuracy = true;
			Options.showMoreStatsInInventoryView = true;
			Options.battleAutoEnd = true;
			Options.disableAutoEquip = true;
			Options.strafe = true;
			Options.skipNextTurnScreen = true;
			Options.alienBleeding = true;
		}
		_game.LoadLanguages();
		_game.PopState();
		_state.BtnOkClick(action);
	}

	/// <summary>Closes the window.</summary>
	public void BtnNoClick(Action action)
	{
		_game.PopState();
	}
}
