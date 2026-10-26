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
// Ported from: src/Menu/ListLoadState.h, src/Menu/ListLoadState.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Load Game screen for listing info on available
/// saved games and loading them.
/// </summary>
public class ListLoadState : ListGamesState
{
	private TextButton _btnOld;

	/// <summary>Initializes all the elements in the Load Game screen.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	public ListLoadState(OptionsOrigin origin) : base(origin, 0, true)
	{
		// Create objects
		_btnOld = new TextButton(80, 16, 60, 172);
		_btnCancel.X = 180;

		Add(_btnOld, "button", "saveMenus");

		// Set up objects
		_txtTitle.SetText(Tr("STR_SELECT_GAME_TO_LOAD"));

		_btnOld.Text = Tr("STR_ORIGINAL_XCOM");
		_btnOld.OnMouseClick(BtnOldClick);

		CenterAllSurfaces();
	}

	/// <summary>Switches to Original X-Com saves.</summary>
	public void BtnOldClick(Action action)
	{
		_game.PushState(new ListLoadOriginalState(_origin));
	}

	/// <summary>Loads the selected save.</summary>
	public override void LstSavesPress(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		base.LstSavesPress(action);
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT && !_btnDelete.Pressed)
		{
			LoadSave((int)_lstSaves.SelectedRow);
		}
	}

	/// <summary>Checks the save's mods and loads it (asking first if content is missing).</summary>
	public void LoadSave(int list_idx)
	{
		bool confirm = false;
		SaveInfo saveInfo = _saves[list_idx];
		foreach (var modName in saveInfo.mods)
		{
			string name = SavedGame.SanitizeModName(modName);
			if (!Options.mods.Exists(m => m.Id == name && m.Active))
			{
				confirm = true;
				break;
			}
		}
		if (confirm)
		{
			_game.PushState(new ConfirmLoadState(_origin, saveInfo.fileName));
		}
		else
		{
			_game.PushState(new LoadGameState(_origin, saveInfo.fileName, _palette));
		}
	}

	/// <summary>Refreshes the saves list; on the first start with "load last save" loads it directly.</summary>
	public override void Init()
	{
		base.Init();
		if (_origin == OptionsOrigin.OPT_MENU && Options.GetLoadLastSave())
		{
			// make it so that this fires only once
			Options.ExpendLoadLastSave();

			// if the save was explicitly specified, load that one and skip all checks and confirmations
			if (!string.IsNullOrEmpty(Options.GetLoadThisSave()))
			{
				// hide the ui
				ToggleScreen();
				HideAll();

				_game.PushState(new LoadGameState(_origin, Options.GetLoadThisSave(), _palette));
				return;
			}

			// find the absolutely latest save game including quick and autos
			long timestamp = 0;
			int idx = -1, i = 0;
			foreach (var save in _saves)
			{
				if (save.timestamp > timestamp)
				{
					idx = i;
					timestamp = save.timestamp;
				}
				++i;
			}
			if (idx != -1)
			{
				// hide the ui
				ToggleScreen();
				HideAll();
				LoadSave(idx);
			}
		}
	}
}
