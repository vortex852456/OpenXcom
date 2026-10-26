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
// Ported from: src/Battlescape/AbortMissionState.h, src/Battlescape/AbortMissionState.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Screen which asks for confirmation to abort mission.
/// </summary>
public class AbortMissionState : State
{
	private readonly Window _window;
	private readonly Text _txtInEntrance, _txtInExit, _txtOutside, _txtAbort;
	private readonly TextButton _btnOk, _btnCancel;
	private readonly SavedBattleGame _battleGame;
	private readonly BattlescapeState _state;
	private readonly int _inEntrance, _inExit, _outside;

	/// <summary>
	/// Initializes all the elements in the Abort Mission window.
	/// </summary>
	/// <param name="battleGame">Pointer to the saved game.</param>
	/// <param name="state">Pointer to the Battlescape state.</param>
	public AbortMissionState(SavedBattleGame battleGame, BattlescapeState state)
	{
		_battleGame = battleGame;
		_state = state;
		_inEntrance = 0;
		_inExit = 0;
		_outside = 0;

		// Create objects
		_screen = false;
		_window = new Window(this, 320, 144, 0, 0);
		_txtInEntrance = new Text(304, 17, 16, 20);
		_txtInExit = new Text(304, 17, 16, 40);
		_txtOutside = new Text(304, 17, 16, 60);
		_txtAbort = new Text(320, 17, 0, 80);
		_btnOk = new TextButton(120, 16, 16, 110);
		_btnCancel = new TextButton(120, 16, 184, 110);

		// Set palette
		_battleGame.SetPaletteByDepth(this);

		Add(_window, "messageWindowBorder", "battlescape");
		Add(_txtInEntrance, "messageWindows", "battlescape");
		Add(_txtInExit, "messageWindows", "battlescape");
		Add(_txtOutside, "messageWindows", "battlescape");
		Add(_txtAbort, "messageWindows", "battlescape");
		Add(_btnOk, "messageWindowButtons", "battlescape");
		Add(_btnCancel, "messageWindowButtons", "battlescape");

		// Check available areas (maybe should be cached somewhere)
		bool exit = false, craft = true;
		AlienDeployment deployment = _game.Mod.GetDeployment(_battleGame.MissionType);
		if (deployment != null)
		{
			exit = !string.IsNullOrEmpty(deployment.NextStage) || deployment.EscapeType == EscapeType.ESCAPE_EXIT || deployment.EscapeType == EscapeType.ESCAPE_EITHER;
			string lastUsedMapScript = _battleGame.LastUsedMapScript;
			if (string.IsNullOrEmpty(lastUsedMapScript))
			{
				lastUsedMapScript = deployment.RandomMapScript; // don't crash on old saves
			}
			List<MapScript> mapScriptEntries = _game.Mod.GetMapScript(lastUsedMapScript);
			if (mapScriptEntries != null)
			{
				craft = false;
				foreach (MapScript mapScriptEntry in mapScriptEntries)
				{
					if (mapScriptEntry.Type == MapScriptCommand.MSC_ADDCRAFT)
					{
						craft = true;
						break;
					}
				}
			}
		}
		if (exit)
		{
			exit = false;
			for (int i = 0; i < _battleGame.MapSizeXYZ; ++i)
			{
				Tile tile = _battleGame.GetTile(i);
				if (tile != null && tile.FloorSpecialTileType == SpecialTileType.END_POINT)
				{
					exit = true;
					break;
				}
			}
		}

		// Calculate values
		BattlescapeTally tally = _battleGame.IsPreview ? _battleGame.TallyUnitsForPreview() : _battleGame.BattleGame.TallyUnits();
		_inEntrance = tally.inEntrance;
		_inExit = tally.inExit;
		_outside = tally.inField;

		if (!exit && _inExit > 0)
		{
			// FIXME: better would be to correctly decide already at the top (how??), but for now this will do...
			exit = true;
		}

		// Set up objects
		_window.SetHighContrast(true);
		_window.SetBackground(_game.Mod.GetSurface("TAC00.SCR"));

		_txtInEntrance.SetBig();
		_txtInEntrance.SetHighContrast(true);
		if (craft)
		{
			_txtInEntrance.SetText(Tr("STR_UNITS_IN_CRAFT", _inEntrance + tally.vipInEntrance));
		}
		else
		{
			_txtInEntrance.SetText(Tr("STR_UNITS_IN_ENTRANCE", _inEntrance + tally.vipInEntrance));
		}

		_txtInExit.SetBig();
		_txtInExit.SetHighContrast(true);
		_txtInExit.SetText(Tr("STR_UNITS_IN_EXIT", _inExit + tally.vipInExit));

		_txtOutside.SetBig();
		_txtOutside.SetHighContrast(true);
		_txtOutside.SetText(Tr("STR_UNITS_OUTSIDE", _outside + tally.vipInField));

		if (_battleGame.MissionType == "STR_BASE_DEFENSE")
		{
			_txtInEntrance.Visible = false;
			_txtInExit.Visible = false;
			_txtOutside.Visible = false;
		}
		else if (!exit || _battleGame.IsPreview)
		{
			_txtInEntrance.Y = 26;
			_txtOutside.Y = 54;
			_txtInExit.Visible = false;
		}

		_txtAbort.SetBig();
		_txtAbort.Align = TextHAlign.ALIGN_CENTER;
		_txtAbort.SetHighContrast(true);
		_txtAbort.SetText(Tr("STR_ABORT_MISSION_QUESTION"));
		if (_battleGame.IsPreview)
		{
			_txtAbort.SetText(Tr("STR_CRAFT_DEPLOYMENT_QUESTION"));
		}

		_btnOk.Text = Tr("STR_OK");
		_btnOk.SetHighContrast(true);
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		if (_battleGame.IsPreview && (_outside > 0 || _inEntrance <= 0))
		{
			_btnOk.Visible = false;
		}

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.SetHighContrast(true);
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyBattleAbort);

		CenterAllSurfaces();
	}

	/// <summary>Confirms mission abort.</summary>
	public void BtnOkClick(Action action)
	{
		if (_battleGame.IsPreview)
		{
			if (_battleGame.CraftForPreview.Id == RuleCraft.DUMMY_CRAFT_ID)
			{
				// dummy craft, generic deployment schema
				_battleGame.SaveDummyCraftDeployment();
			}
			else
			{
				// real craft, real unit deployment
				_battleGame.SaveCustomCraftDeployment();
			}

			_game.PopState();
			return;
		}

		_game.PopState();
		_battleGame.SetAborted(true);
		_state.FinishBattle(true, _inExit);
	}

	/// <summary>Returns to the previous screen.</summary>
#pragma warning disable CA1822 // instance handler as in C++
	public void BtnCancelClick(Action action)
#pragma warning restore CA1822
	{
		_game.PopState();
	}
}
