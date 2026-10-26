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
// Ported from: src/Battlescape/BriefingState.h, src/Battlescape/BriefingState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Briefing screen which displays info
/// about a Crash Site mission.
/// </summary>
public class BriefingState : State
{
	private readonly TextButton _btnOk;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtTarget, _txtCraft, _txtBriefing;
	private string _cutsceneId = "", _musicId = "";
	private readonly bool _infoOnly;
	private readonly bool _disableCutsceneAndMusic;

	/// <summary>Initializes all the elements in the Briefing screen.</summary>
	/// <param name="craft">Pointer to the craft in the mission.</param>
	/// <param name="base">Pointer to the base in the mission.</param>
	/// <param name="infoOnly">Only show static info, when briefing is re-opened during the battle.</param>
	/// <param name="customBriefing">Pointer to a custom briefing (used for Reinforcements notification).</param>
	public BriefingState(Craft craft = null, Base @base = null, bool infoOnly = false, BriefingData customBriefing = null)
	{
		_infoOnly = infoOnly;
		_disableCutsceneAndMusic = false;

		Options.baseXResolution = Options.baseXGeoscape;
		Options.baseYResolution = Options.baseYGeoscape;
		_game.Screen.ResetDisplay(false);

		_screen = true;
		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnOk = new TextButton(120, 18, 100, 164);
		_txtTitle = new Text(300, 32, 16, 24);
		_txtTarget = new Text(300, 17, 16, 40);
		_txtCraft = new Text(300, 17, 16, 56);
		_txtBriefing = new Text(274, 94, 16, 72);

		SavedBattleGame battleSave = _game.SavedGame.SavedBattle;

		string mission = battleSave.MissionType;
		AlienDeployment deployment = _game.Mod.GetDeployment(mission);
		if (mission == "STR_BASE_DEFENSE")
		{
			AlienDeployment customDeployment = _game.Mod.GetDeployment(battleSave.AlienCustomDeploy);
			if (customDeployment != null && !string.IsNullOrEmpty(customDeployment.BriefingData.desc))
			{
				deployment = customDeployment;
			}
		}
		else
		{
			if (deployment == null && craft != null)
			{
				Ufo ufo = craft.Destination as Ufo;
				if (ufo != null) // landing site or crash site.
				{
					string ufoMissionName = ufo.Rules.Type;
					if (!string.IsNullOrEmpty(battleSave.AlienCustomMission))
					{
						// fake underwater UFO
						ufoMissionName = battleSave.AlienCustomMission;
					}
					deployment = _game.Mod.GetDeployment(ufoMissionName);
				}
			}
		}

		string title = mission;
		string desc = title + "_BRIEFING";
		if (deployment == null && customBriefing == null) // none defined - should never happen, but better safe than sorry i guess.
		{
			SetStandardPalette("PAL_GEOSCAPE", 0);
			_musicId = "GMDEFEND";
			_window.SetBackground(_game.Mod.GetSurface("BACK16.SCR"));
		}
		else
		{
			BriefingData data = customBriefing != null ? customBriefing.Clone() : deployment.BriefingData;
			SetStandardPalette("PAL_GEOSCAPE", data.palette);
			_window.SetBackground(_game.Mod.GetSurface(data.background));
			_txtCraft.Y = 56 + data.textOffset;
			_txtBriefing.Y = 72 + data.textOffset;
			_txtTarget.Visible = data.showTarget;
			_txtCraft.Visible = data.showCraft;
			_cutsceneId = data.cutscene;
			_musicId = data.music;
			if (!string.IsNullOrEmpty(data.title))
			{
				title = data.title;
			}
			if (!string.IsNullOrEmpty(data.desc))
			{
				desc = data.desc;
			}
		}
		_disableCutsceneAndMusic = _infoOnly && customBriefing == null;

		Add(_window, "window", "briefing");
		Add(_btnOk, "button", "briefing");
		Add(_txtTitle, "text", "briefing");
		Add(_txtTarget, "text", "briefing");
		Add(_txtCraft, "text", "briefing");
		Add(_txtBriefing, "text", "briefing");

		CenterAllSurfaces();

		// Set up objects
		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTarget.SetBig();
		_txtCraft.SetBig();

		if (!_infoOnly)
		{
			string s;
			if (craft != null)
			{
				Target destination = craft.Destination;
				if (destination != null)
				{
					s = destination.GetName(_game.Language);
					battleSave.MissionTarget = s;
				}

				s = Tr("STR_CRAFT_").Arg(craft.GetName(_game.Language));
				battleSave.MissionCraftOrBase = s;
			}
			else if (@base != null)
			{
				s = Tr("STR_BASE_UC_").Arg(@base.GetName(_game.Language));
				battleSave.MissionCraftOrBase = s;
			}

			// random operation names
			if (craft != null || @base != null)
			{
				if (_game.Mod.OperationNamesFirst.Count != 0)
				{
					int pickFirst = Rng.Seedless(0, _game.Mod.OperationNamesFirst.Count - 1);
					s = _game.Mod.OperationNamesFirst[pickFirst];
					if (_game.Mod.OperationNamesLast.Count != 0)
					{
						int pickLast = Rng.Seedless(0, _game.Mod.OperationNamesLast.Count - 1);
						s += " " + _game.Mod.OperationNamesLast[pickLast];
					}
					battleSave.MissionTarget = s;
				}
			}
		}

		if (_game.Mod.OperationNamesFirst.Count != 0)
			_txtTarget.SetText(Tr("STR_OPERATION_UC").Arg(battleSave.MissionTarget));
		else
			_txtTarget.SetText(battleSave.MissionTarget);

		_txtCraft.SetText(battleSave.MissionCraftOrBase);

		_txtTitle.SetText(Tr(title));

		bool isPreview = battleSave.IsPreview;
		if (isPreview)
		{
			if (battleSave.CraftForPreview != null)
			{
				if (battleSave.CraftForPreview.Id == RuleCraft.DUMMY_CRAFT_ID)
				{
					// we're using the same alienDeployment for the real craft preview and for the dummy craft preview,
					// but we want to have different briefing texts
					desc = desc + "_DUMMY";
				}
			}
			else
			{
				// base preview
				desc = desc + "_PREVIEW";
			}
		}
		_txtBriefing.SetWordWrap(true);
		_txtBriefing.SetText(Tr(desc));

		if (_infoOnly) return;

		if (!isPreview && @base != null && mission == "STR_BASE_DEFENSE")
		{
			AlienMission am = @base.RetaliationMission;

			// And make sure the base is unmarked (but only for vanilla retaliations, not for instant retaliations)
			if (am != null)
			{
				@base.SetRetaliationTarget(false);
			}

			if (am != null && am.Rules.IsMultiUfoRetaliation)
			{
				// Remember that more UFOs may be coming
				am.SetMultiUfoRetaliationInProgress(true);
			}
		}
	}

	/// <summary>Starts the cutscene or the music.</summary>
	public override void Init()
	{
		base.Init();
		if (_disableCutsceneAndMusic) return;

		if (!string.IsNullOrEmpty(_cutsceneId))
		{
			_game.PushState(new CutsceneState(_cutsceneId));

			// don't play the cutscene again when we return to this state
			_cutsceneId = "";
		}
		else
		{
			_game.Mod.PlayMusic(_musicId);
		}
	}

	/// <summary>Closes the window.</summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
		Options.baseXResolution = Options.baseXBattlescape;
		Options.baseYResolution = Options.baseYBattlescape;
		_game.Screen.ResetDisplay(false);
		if (_infoOnly) return;

		BattlescapeState bs = new();
		bs.BattleGame.SpawnFromPrimedItems();
		BattlescapeTally tally = bs.BattleGame.TallyUnits();
		bool isPreview = _game.SavedGame.SavedBattle.IsPreview;
		if (tally.liveAliens > 0 || isPreview)
		{
			_game.PushState(bs);
			_game.SavedGame.SavedBattle.BattleState = bs;
			_game.PushState(new NextTurnState(_game.SavedGame.SavedBattle, bs));
			if (isPreview)
			{
				// skip InventoryState
				_game.SavedGame.SavedBattle.StartFirstTurn();
				return;
			}
			_game.PushState(new InventoryState(false, bs, null));
		}
		else
		{
			Options.baseXResolution = Options.baseXGeoscape;
			Options.baseYResolution = Options.baseYGeoscape;
			_game.Screen.ResetDisplay(false);
			// (C++ deletes bs here)
			_game.PushState(new AliensCrashState());
		}
	}
}
