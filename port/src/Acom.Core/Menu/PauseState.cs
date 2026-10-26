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
// Ported from: src/Menu/PauseState.h, src/Menu/PauseState.cpp
// Deviation: the version line names ACOM and the OXCE version it is based on.
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Options window that displays all
/// the options available in the game.
/// </summary>
public class PauseState : State
{
	private readonly OptionsOrigin _origin;
	private readonly TextButton _btnLoad, _btnSave, _btnAbandon, _btnOptions, _btnCancel;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtVersion;

	/// <summary>
	/// Initializes all the elements in the Pause window.
	/// </summary>
	/// <param name="origin">Game section that originated this state.</param>
	public PauseState(OptionsOrigin origin)
	{
		_origin = origin;
		_screen = false;

		int x;
		if (_origin == OptionsOrigin.OPT_GEOSCAPE)
		{
			x = 20;
		}
		else
		{
			x = 52;
		}

		// Create objects
		_window = new Window(this, 216, 160, x, 20, WindowPopup.POPUP_BOTH);
		_btnLoad = new TextButton(180, 18, x + 18, 52);
		_btnSave = new TextButton(180, 18, x + 18, 74);
		_btnAbandon = new TextButton(180, 18, x + 18, 96);
		_btnOptions = new TextButton(180, 18, x + 18, 122);
		_btnCancel = new TextButton(180, 18, x + 18, 150);
		_txtTitle = new Text(206, 17, x + 5, 32);
		_txtVersion = new Text(216, 9, x, 11);

		// Set palette
		SetInterface("pauseMenu", false, _game.SavedGame != null ? _game.SavedGame.SavedBattle : null);

		Add(_window, "window", "pauseMenu");
		Add(_btnLoad, "button", "pauseMenu");
		Add(_btnSave, "button", "pauseMenu");
		Add(_btnAbandon, "button", "pauseMenu");
		Add(_btnOptions, "button", "pauseMenu");
		Add(_btnCancel, "button", "pauseMenu");
		Add(_txtTitle, "text", "pauseMenu");
		Add(_txtVersion, "text", "pauseMenu");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "pauseMenu");

		_btnLoad.Text = Tr("STR_LOAD_GAME");
		_btnLoad.OnMouseClick(BtnLoadClick);

		_btnSave.Text = Tr("STR_SAVE_GAME");
		_btnSave.OnMouseClick(BtnSaveClick);

		_btnAbandon.Text = Tr("STR_ABANDON_GAME");
		_btnAbandon.OnMouseClick(BtnAbandonClick);

		_btnOptions.Text = Tr("STR_GAME_OPTIONS");
		_btnOptions.OnMouseClick(BtnOptionsClick);

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);
		if (origin == OptionsOrigin.OPT_GEOSCAPE)
		{
			_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyGeoOptions);
		}
		else if (origin == OptionsOrigin.OPT_BATTLESCAPE)
		{
			_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyBattleOptions);
			if (_game.SavedGame.SavedBattle.BattleGame.States.Count != 0)
			{
				_btnOptions.Visible = false;
			}
		}

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetBig();
		_txtTitle.SetText(Tr("STR_OPTIONS_UC"));

		_txtVersion.SetText($"ACOM {Version.ACOM_VERSION} (OpenXcom {Version.OPENXCOM_VERSION_SHORT})");
		_txtVersion.Align = TextHAlign.ALIGN_CENTER;

		if (_origin == OptionsOrigin.OPT_BATTLESCAPE)
		{
			ApplyBattlescapeTheme("pauseMenu");
		}

		if (_game.SavedGame.IsIronman)
		{
			_btnLoad.Visible = false;
			_btnSave.Visible = false;
			_btnAbandon.Text = Tr("STR_SAVE_AND_ABANDON_GAME");
		}

		// ENOUGH! No save corruption when trying to save/exit mid-action (e.g. during alien turn)
		if (origin == OptionsOrigin.OPT_BATTLESCAPE)
		{
			bool playerTurn = _game.SavedGame.SavedBattle.Side == UnitFaction.FACTION_PLAYER;
			bool debugMode = _game.SavedGame.SavedBattle.DebugMode;
			bool busy = _game.SavedGame.SavedBattle.BattleGame.States.Count != 0;

			if ((!playerTurn && !debugMode) || busy)
			{
				_btnSave.Visible = false; // non-ironman + ironman

				if (_game.SavedGame.IsIronman)
				{
					_btnAbandon.Visible = false; // ironman only
				}
			}
		}
	}

	/// <summary>
	/// Opens the Load Game screen.
	/// </summary>
	public void BtnLoadClick(Action action)
	{
		_game.PushState(new ListLoadState(_origin));
	}

	/// <summary>
	/// Opens the Save Game screen.
	/// </summary>
	public void BtnSaveClick(Action action)
	{
		_game.PushState(new ListSaveState(_origin));
	}

	/// <summary>
	/// Opens the Game Options screen.
	/// </summary>
	public void BtnOptionsClick(Action action)
	{
		Options.BackupDisplay();
		if (_origin == OptionsOrigin.OPT_GEOSCAPE)
		{
			_game.PushState(new OptionsGeoscapeState(_origin));
		}
		else if (_origin == OptionsOrigin.OPT_BATTLESCAPE)
		{
			_game.PushState(new OptionsBattlescapeState(_origin));
		}
		else
		{
			_game.PushState(new OptionsVideoState(_origin));
		}
	}

	/// <summary>
	/// Opens the Abandon Game window.
	/// </summary>
	public void BtnAbandonClick(Action action)
	{
		_game.PushState(new AbandonGameState(_origin));
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}
}
