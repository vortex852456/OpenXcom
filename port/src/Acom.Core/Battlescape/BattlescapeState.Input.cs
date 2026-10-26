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
// Ported from: src/Battlescape/BattlescapeState.cpp (event handling, battle end, tooltips, resize)
#nullable disable

using System.Text;
using static Acom.BattleType;
using static Acom.UnitBodyPart;
using static Acom.UnitFaction;
using static Acom.UnitStatus;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

public partial class BattlescapeState
{
	/// <summary>
	/// Takes care of any events from the core game engine.
	/// </summary>
	public override void Handle(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (!_firstInit)
		{
			if (_game.Cursor.Visible || ((action.Details.type == SDLEventType.SDL_MOUSEBUTTONDOWN || action.Details.type == SDLEventType.SDL_MOUSEBUTTONUP) && _game.IsRightClick(action)))
			{
				base.Handle(action);

				if (Options.touchEnabled == false && _isMouseScrolling && !Options.battleDragScrollInvert)
				{
					_map.SetSelectorPosition((int)((_cursorPosition.x - _game.Screen.CursorLeftBlackBand) / action.XScale), (int)((_cursorPosition.y - _game.Screen.CursorTopBlackBand) / action.YScale));
				}

				if (Options.oxceThumbButtons && action.Details.type == SDLEventType.SDL_MOUSEBUTTONDOWN)
				{
					if (action.Details.button.button == SDLMouse.SDL_BUTTON_X1)
					{
						BtnNextSoldierClick(action);
					}
					else if (action.Details.button.button == SDLMouse.SDL_BUTTON_X2)
					{
						BtnPrevSoldierClick(action);
					}
				}

				if (action.Details.type == SDLEventType.SDL_KEYDOWN)
				{
					HandleKey(action);
				}
			}
		}
	}

	/// <summary>Hotkeys of <see cref="Handle"/> (keyboard part of the C++ handle()).</summary>
	private void HandleKey(Action action)
	{
		SDLKey key = action.Details.key.keysym.sym;
		bool ctrlPressed = _game.IsCtrlPressed();
		bool shiftPressed = _game.IsShiftPressed();
		bool altPressed = _game.IsAltPressed();

		// "shift-hotkey" - select without centering
		if (shiftPressed)
		{
			if (key == Options.keyBattleNextUnit)
			{
				BtnNextSoldierClick(action);
			}
			else if (key == Options.keyBattlePrevUnit)
			{
				BtnPrevSoldierClick(action);
			}
		}

		// "ctrl-b" - reopen briefing
		if (key == SDLKey.SDLK_b && ctrlPressed)
		{
			Craft ycraft = null;
			foreach (Base xbase in _game.SavedGame.Bases)
			{
				foreach (Craft xcraft in xbase.Crafts)
				{
					if (xcraft.IsInBattlescape)
					{
						ycraft = xcraft;
						break;
					}
				}
				if (ycraft != null) break;
			}

			_game.PushState(new BriefingState(ycraft, null, true));
		}
		// "ctrl-c" - camera: toggle show single map level
		else if (key == SDLKey.SDLK_c && ctrlPressed)
		{
			_map.Camera.ToggleShowSingleLayer();

			if (_map.Camera.ShowSingleLayer)
				WarningLongRaw(Tr("STR_SINGLE_MAP_LAYER_ACTIVATED"));
			else
				Warning("STR_SINGLE_MAP_LAYER_DEACTIVATED");
		}
		// "ctrl-f" - show fatal wounds
		else if (key == SDLKey.SDLK_f && ctrlPressed)
		{
			if (_save.Side == FACTION_PLAYER)
			{
				BattleUnit bu = _save.SelectedUnit;
				if (bu != null)
				{
					var ss = new StringBuilder();
					ss.Append((string)Tr("STR_FATAL_WOUNDS"));
					ss.Append('\n');
					for (int i = 0; i < (int)BODYPART_MAX; ++i)
					{
						if (bu.GetFatalWound((UnitBodyPart)i) != 0)
						{
							ss.Append('\n');
							ss.Append((string)_game.Language.GetString(BattleUnit.PARTS_STRING[i]));
						}
					}
					_game.PushState(new InfoboxState(ss.ToString()));
				}
			}
		}
		// "ctrl-h" - show hit log
		else if (key == SDLKey.SDLK_h && ctrlPressed)
		{
			if (_save.Side == FACTION_PLAYER)
			{
				if (Options.oxceDisableHitLog)
				{
					_game.PushState(new InfoboxState(Tr("STR_THIS_FEATURE_IS_DISABLED_4")));
				}
				else if (altPressed)
				{
					// turn diary
					_game.PushState(new TurnDiaryState(_save.HitLog));
				}
				else
				{
					// hit log
					string hitLogText = _save.HitLog.GetHitLogText();
					if (!string.IsNullOrEmpty(hitLogText))
						_game.PushState(new InfoboxState(hitLogText));
				}
			}
		}
		// "ctrl-Home" - reset default palettes
		else if (key == SDLKey.SDLK_HOME && ctrlPressed)
		{
			_paletteResetRequested = true;
			Init();
		}
		// "ctrl-End" - toggle between various debug vision/brightness modes
		else if (key == SDLKey.SDLK_END && ctrlPressed)
		{
			_map.ToggleDebugVisionMode();
		}
		// "ctrl-shift-Del" - clear TUs for all allied units
		else if (key == SDLKey.SDLK_DELETE && ctrlPressed && shiftPressed)
		{
			foreach (BattleUnit bu in _save.Units)
			{
				if (bu.Faction == _save.Side && !bu.IsOut)
				{
					bu.ClearTimeUnits();
				}
			}
			UpdateSoldierInfo();
		}
		// "ctrl-s" - switch xcom unit speed to max and back
		else if (key == SDLKey.SDLK_s && ctrlPressed)
		{
			if (_save.Side == FACTION_PLAYER)
			{
				if (Options.battleXcomSpeedOrig >= 1 && Options.battleXcomSpeedOrig <= 40)
				{
					Options.battleXcomSpeed = Options.battleXcomSpeedOrig;
					Options.battleXcomSpeedOrig = -1;
					Warning("STR_QUICK_MODE_DEACTIVATED");
				}
				else
				{
					Options.battleXcomSpeedOrig = Options.battleXcomSpeed;
					Options.battleXcomSpeed = 1;
					WarningLongRaw(Tr("STR_QUICK_MODE_ACTIVATED"));
				}
			}
			else
			{
				if (Options.battleAlienSpeedOrig >= 1 && Options.battleAlienSpeedOrig <= 40)
				{
					Options.battleAlienSpeed = Options.battleAlienSpeedOrig;
					Options.battleAlienSpeedOrig = -1;
					Warning("STR_QUICK_MODE_DEACTIVATED");
				}
				else
				{
					Options.battleAlienSpeedOrig = Options.battleAlienSpeed;
					Options.battleAlienSpeed = 1;
					Warning("STR_QUICK_MODE_ACTIVATED");
				}
			}
		}
		// "ctrl-x" - mute/unmute unit response sounds
		else if (key == SDLKey.SDLK_x && ctrlPressed)
		{
			if (_game.Mod.EnableUnitResponseSounds)
			{
				Options.oxceEnableUnitResponseSounds = !Options.oxceEnableUnitResponseSounds;
			}
		}
		// "ctrl-e" - experience log
		else if (key == SDLKey.SDLK_e && ctrlPressed)
		{
			if (altPressed)
			{
				_game.PushState(new NoExperienceState());
			}
			else if (shiftPressed)
			{
				_game.PushState(new ExperienceOverviewState(this));
			}
			else
			{
				var ss = new StringBuilder();
				ss.Append((string)Tr("STR_NO_EXPERIENCE_YET"));
				ss.Append("\n\n");
				bool first = true;
				foreach (BattleUnit bu in _save.Units)
				{
					if (bu.OriginalFaction == FACTION_PLAYER && !bu.IsOut)
					{
						if (bu.GeoscapeSoldier != null && !bu.HasGainedAnyExperience)
						{
							if (!first) ss.Append(", ");
							if (bu == _save.SelectedUnit)
								ss.Append(Unicode.TOK_COLOR_FLIP).Append(bu.GetName(_game.Language)).Append(Unicode.TOK_COLOR_FLIP);
							else
								ss.Append(bu.GetName(_game.Language));
							first = false;
						}
					}
				}
				_game.PushState(new InfoboxState(ss.ToString()));
			}
		}
		// "alt-c" - custom marker
		else if (key == SDLKey.SDLK_c && altPressed)
		{
			BattleUnit unitUnderTheCursor = GetUnitUnderTheCursor();
			// mark a friendly unit under the cursor
			if (unitUnderTheCursor != null && unitUnderTheCursor.Faction == FACTION_PLAYER && !unitUnderTheCursor.IsOut)
			{
				unitUnderTheCursor.CustomMarker = (unitUnderTheCursor.CustomMarker + 1) % 5; // rotate 4 colors + turned off
			}
		}
		// "ctrl-m" - melee damage preview
		else if (key == SDLKey.SDLK_m && ctrlPressed)
		{
			BattleUnit actor = _save.SelectedUnit;
			if (actor != null)
			{
				BattleItem leftWeapon = actor.LeftHandWeapon;

				BattleItem rightWeapon = actor.RightHandWeapon;

				BattleItem specialWeapon = FindEmptyHandSpecialWeapon(actor);

				BattleType type = BT_NONE;
				BattleItem anotherSpecialWeapon = actor.GetSpecialIconWeapon(ref type);
				if (anotherSpecialWeapon != null && anotherSpecialWeapon == specialWeapon)
				{
					anotherSpecialWeapon = null;
				}

				var ss = new StringBuilder();
				bool first = true;
				if (leftWeapon != null)
				{
					ss.Append(GetMeleeDamagePreview(actor, leftWeapon));
					first = false;
				}
				if (rightWeapon != null)
				{
					if (!first) ss.Append("\n\n");
					ss.Append(GetMeleeDamagePreview(actor, rightWeapon));
					first = false;
				}
				if (specialWeapon != null)
				{
					if (!first) ss.Append("\n\n");
					ss.Append(GetMeleeDamagePreview(actor, specialWeapon));
					first = false;
				}
				if (anotherSpecialWeapon != null)
				{
					if (!first) ss.Append("\n\n");
					ss.Append(GetMeleeDamagePreview(actor, anotherSpecialWeapon));
				}

				_game.PushState(new InfoboxState(ss.ToString()));
			}
		}
		// "ctrl-w" - warp unit
		else if ((_save.DebugMode || _save.IsPreview) && key == SDLKey.SDLK_w && ctrlPressed)
		{
			BattleUnit unit = _save.SelectedUnit;
			if (unit != null)
			{
				Position newPos = default;
				_map.GetSelectorPosition(ref newPos);
				if (_save.TileEngine.IsPositionValidForUnit(ref newPos, unit))
				{
					Debug("Beam me up Scotty");
					_save.Pathfinding.RemovePreview();

					unit.SetTile(_save.GetTile(newPos), _save);
					unit.SetPosition(newPos);

					//free refresh as bonus
					unit.UpdateUnitStats(true, false);
					_save.TileEngine.CalculateLighting(LightLayers.LL_UNITS);
					_battleGame.HandleState();
					UpdateSoldierInfo(true);
				}
			}
		}
		if (Options.debug)
		{
			// "ctrl-d" - enable debug mode
			if (key == SDLKey.SDLK_d && ctrlPressed)
			{
				_save.SetDebugMode();
				Debug("Debug Mode");
			}
			// "ctrl-v" - reset tile visibility
			else if (_save.DebugMode && key == SDLKey.SDLK_v && ctrlPressed)
			{
				Debug("Resetting tile visibility");
				_save.ResetTiles();
			}
			else if (_save.DebugMode && (key == SDLKey.SDLK_k || key == SDLKey.SDLK_j) && ctrlPressed)
			{
				bool stunOnly = (key == SDLKey.SDLK_j);
				BattleUnit unitUnderTheCursor = null;
				if (shiftPressed || altPressed)
				{
					unitUnderTheCursor = GetUnitUnderTheCursor();
				}
				if (shiftPressed)
				{
					// kill (ctrl-shift-k) or stun (ctrl-shift-j) just a single unit (under the cursor)
					if (unitUnderTheCursor != null && !unitUnderTheCursor.IsOut)
					{
						Debug("Bingo!");
						unitUnderTheCursor.Damage(new Position(0, 0, 0), 1000, _game.Mod.GetDamageType(stunOnly ? ItemDamageType.DT_STUN : ItemDamageType.DT_MELEE), _save, new BattleActionAttack());
					}
				}
				else
				{
					if (stunOnly)
					{
						// "ctrl-j" - stun all aliens
						Debug("Deploying Celine Dion album");
					}
					else
					{
						// "ctrl-k" - kill all aliens
						Debug("Influenza bacterium dispersed");
					}
					foreach (BattleUnit bu in _save.Units)
					{
						if (unitUnderTheCursor != null && unitUnderTheCursor == bu)
						{
							// kill (ctrl-alt-k) or stun (ctrl-alt-j) all aliens EXCEPT the one under the cursor
							continue;
						}
						if (bu.OriginalFaction == FACTION_HOSTILE && !bu.IsOut)
						{
							bu.Damage(new Position(0, 0, 0), 1000, _game.Mod.GetDamageType(stunOnly ? ItemDamageType.DT_STUN : ItemDamageType.DT_MELEE), _save, new BattleActionAttack());
						}
					}
				}
				_battleGame.CheckForCasualties(null, new BattleActionAttack(), true, false);
				_battleGame.HandleState();
			}
			else if (_save.DebugMode && (key == SDLKey.SDLK_m || key == SDLKey.SDLK_p) && ctrlPressed && shiftPressed)
			{
				BattleUnit unitUnderTheCursor = GetUnitUnderTheCursor();
				// mind control (ctrl-shift-m) or panic (ctrl-shift-p) just a single unit (under the cursor)
				if (unitUnderTheCursor != null && !unitUnderTheCursor.IsOut)
				{
					if (key == SDLKey.SDLK_p)
					{
						int moraleLoss = unitUnderTheCursor.ReduceByBravery(100);
						if (moraleLoss > 0)
						{
							Debug("Have you paid your taxes yet?");
							unitUnderTheCursor.MoraleChange(-moraleLoss);
							_game.PushState(new InfoboxState(_game.Language.GetString("STR_MORALE_ATTACK_SUCCESSFUL")));
						}
					}
					else
					{
						if (unitUnderTheCursor.Faction != FACTION_PLAYER)
						{
							Debug("My mind to your mind, my thoughts to your thoughts.");
							unitUnderTheCursor.ConvertToFaction(FACTION_PLAYER);
							//unitUnderTheCursor->recoverTimeUnits();
							unitUnderTheCursor.AllowReselect();
							unitUnderTheCursor.AbortTurn(); // resets unit status to STANDING
							_game.PushState(new InfoboxState(_game.Language.GetString("STR_MIND_CONTROL_SUCCESSFUL")));
						}
					}
				}
			}
			// f11 - voxel map dump
			else if (key == SDLKey.SDLK_F11)
			{
				SaveVoxelMap();
			}
			// f9 - ai
			else if (key == SDLKey.SDLK_F9 && Options.traceAI)
			{
				SaveAIMap();
			}
		}
		// quick save and quick load
		if (!_game.SavedGame.IsIronman && !_save.IsPreview)
		{
			if (key == Options.keyQuickSave)
			{
				_game.PushState(new SaveGameState(OptionsOrigin.OPT_BATTLESCAPE, SaveType.SAVE_QUICK, _palette));
			}
			else if (key == Options.keyInstaSave)
			{
				_game.PushState(new SaveGameState(OptionsOrigin.OPT_BATTLESCAPE, SaveType.SAVE_INSTA, _palette));
			}
			else if (key == Options.keyQuickLoad)
			{
				_game.PushState(new LoadGameState(OptionsOrigin.OPT_BATTLESCAPE, SaveType.SAVE_QUICK, _palette));
			}
		}

		// voxel view dump
		if (key == Options.keyBattleVoxelView)
		{
			SaveVoxelView();
		}
	}

	/// <summary>The unit overlapping the tile under the map selector (repeated C++ block).</summary>
	private BattleUnit GetUnitUnderTheCursor()
	{
		Position newPos = default;
		_map.GetSelectorPosition(ref newPos);
		Tile tile = _save.GetTile(newPos);
		if (tile != null)
		{
			return tile.GetOverlappingUnit(_save);
		}
		return null;
	}

	/// <summary>
	/// Saves a map as used by the AI.
	/// </summary>
	/// <remarks>Not ported: debug PNG dump (SDL_gfx drawing + lodepng), the port has no PNG encoder yet.</remarks>
	public void SaveAIMap()
	{
		BattleUnit unit = _save.SelectedUnit;
		if (unit == null) return;
		Logger.Info("saveAIMap() is not available in this port.");
	}

	/// <summary>
	/// Saves each layer of voxels on the bettlescape as a png.
	/// </summary>
	/// <remarks>Not ported: debug PNG dump, the port has no PNG encoder yet.</remarks>
	public void SaveVoxelMap()
	{
		Logger.Info("saveVoxelMap() is not available in this port.");
	}

	/// <summary>
	/// Saves a first-person voxel view of the battlescape.
	/// </summary>
	/// <remarks>Not ported: debug PNG dump, the port has no PNG encoder yet.</remarks>
	public void SaveVoxelView()
	{
		BattleUnit bu = _save.SelectedUnit;
		if (bu == null) return; //no unit selected
		Logger.Info("saveVoxelView() is not available in this port.");
	}

	/// <summary>
	/// Finishes up the current battle, shuts down the battlescape
	/// and presents the debriefing screen for the mission.
	/// </summary>
	/// <param name="abort">Was the mission aborted?</param>
	/// <param name="inExitArea">Number of soldiers in the exit area OR number of survivors when battle finished due to either all aliens or objective being destroyed.</param>
	public void FinishBattle(bool abort, int inExitArea)
	{
		bool isPreview = _save.IsPreview;

		while (!_game.IsState(this))
		{
			_game.PopState();
		}
		_game.Cursor.Visible = true;
		if (_save.AmbientSound != Mod.NO_SOUND)
		{
			_game.Mod.GetSoundByDepth(0, (uint)_save.AmbientSound).StopLoop();
		}

		// reset touch flags
		_game.ResetTouchButtonFlags();

		// dear civilians and summoned player units,
		// please drop all borrowed xcom equipment now, so that we can recover it
		// thank you!
		var itemsToDrop = new List<BattleItem>();
		foreach (BattleUnit unit in _save.Units)
		{
			bool relevantUnitType = unit.OriginalFaction == FACTION_NEUTRAL || unit.IsSummonedPlayerUnit;
			if (relevantUnitType && !unit.IsOut)
			{
				itemsToDrop.Clear();
				foreach (BattleItem item in unit.Inventory)
				{
					if (item.XCOMProperty || item.Unit != null)
					{
						itemsToDrop.Add(item);
					}
				}
				foreach (BattleItem xcomItem in itemsToDrop)
				{
					_save.TileEngine.ItemDrop(unit.Tile, xcomItem, false);
				}
			}
		}

		// let's count summoned player-controlled VIPs before we remove them :)
		_battleGame.TallySummonedVIPs();
		// this removes player-controlled VIPs (not civilian VIPs)
		_battleGame.RemoveSummonedPlayerUnits();

		AlienDeployment ruleDeploy = _game.Mod.GetDeployment(_save.MissionType);
		if (ruleDeploy == null)
		{
			foreach (Ufo ufo in _game.SavedGame.Ufos)
			{
				if (ufo.IsInBattlescape)
				{
					string ufoMissionName = ufo.Rules.Type;
					if (!string.IsNullOrEmpty(_save.AlienCustomMission))
					{
						// fake underwater UFO
						ufoMissionName = _save.AlienCustomMission;
					}
					ruleDeploy = _game.Mod.GetDeployment(ufoMissionName);
					break;
				}
			}
		}
		string nextStage = "";
		if (ruleDeploy != null)
		{
			nextStage = ruleDeploy.NextStage;
		}

		if (!string.IsNullOrEmpty(nextStage) && inExitArea != 0 && !isPreview)
		{
			// if there is a next mission stage + we have people in exit area OR we killed all aliens, load the next stage
			_popups.Clear();
			_save.MissionType = nextStage;
			var bgen = new BattlescapeGenerator(_game);
			bgen.NextStage();
			_game.PopState();
			_game.PushState(new BriefingState(null, null));
		}
		else
		{
			_popups.Clear();
			_animTimer.Stop();
			_gameTimer.Stop();
			_game.PopState();
			if (isPreview)
			{
				// skip Debriefing
				Options.baseXResolution = Options.baseXGeoscape;
				Options.baseYResolution = Options.baseYGeoscape;
				_game.Screen.ResetDisplay(false);

				// Restore the cursor in case something weird happened
				_game.Cursor.Visible = true;

				// delete SavedBattleGame
				_game.SavedGame.SetBattleGame(null);

				// unmark all craft and all bases (current craft would be enough, but better safe than sorry)
				foreach (Base xbase in _game.SavedGame.Bases)
				{
					xbase.SetInBattlescape(false);
					foreach (Craft craft in xbase.Crafts)
					{
						craft.SetInBattlescape(false);
					}
				}

				// reset the music
				_game.Mod.PlayMusic("GMGEO");
				return;
			}
			_game.PushState(new DebriefingState());
			string cutscene = "";
			if (ruleDeploy != null)
			{
				if (abort)
				{
					cutscene = ruleDeploy.AbortCutscene;
				}
				else if (inExitArea == 0)
				{
					cutscene = ruleDeploy.LoseCutscene;
				}
				else
				{
					cutscene = ruleDeploy.WinCutscene;
				}
			}
			if (!string.IsNullOrEmpty(cutscene))
			{
				// if cutscene is "wingame" or "losegame", then the DebriefingState
				// pushed above will get popped without being shown.  otherwise
				// it will get shown after the cutscene.
				_game.PushState(new CutsceneState(cutscene));

				RuleVideo videoRule = _game.Mod.GetVideo(cutscene, true);
				if (videoRule.WinGame)
				{
					_game.SavedGame.Ending = GameEnding.END_WIN;
				}
				else if (videoRule.LoseGame)
				{
					_game.SavedGame.Ending = GameEnding.END_LOSE;
				}
				// Autosave if game is over
				if (_game.SavedGame.Ending != GameEnding.END_NONE && _game.SavedGame.IsIronman)
				{
					_game.PushState(new SaveGameState(OptionsOrigin.OPT_BATTLESCAPE, SaveType.SAVE_IRONMAN, _palette));
				}
			}
		}
	}

	/// <summary>
	/// Shows a tooltip with extra information (used for medikit-type equipment).
	/// </summary>
	public void TxtTooltipInExtra(Action action, bool leftHand, bool special = false)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (AllowButtons() && Options.battleTooltips)
		{
			// no one selected... do normal tooltip
			if (!PlayableUnitSelected())
			{
				_currentTooltip = action.Sender.Tooltip;
				_txtTooltip.SetText(Tr(_currentTooltip));
				return;
			}

			BattleUnit selectedUnit = _save.SelectedUnit;
			BattleItem weapon;
			if (leftHand)
			{
				weapon = selectedUnit.LeftHandWeapon;
			}
			else if (special)
			{
				BattleType type = BT_NONE;
				weapon = selectedUnit.GetSpecialIconWeapon(ref type);
			}
			else
			{
				weapon = selectedUnit.RightHandWeapon;
			}

			// no weapon selected... do normal tooltip
			if (weapon == null)
			{
				_currentTooltip = action.Sender.Tooltip;
				_txtTooltip.SetText(Tr(_currentTooltip));
				return;
			}

			RuleItem weaponRule = weapon.Rules;

			// find the target unit
			if (weaponRule.BattleType == BT_MEDIKIT)
			{
				BattleUnit targetUnit = null;

				// search for target on the ground
				bool onGround = false;
				foreach (BattleUnit bu in _save.Units)
				{
					if (targetUnit != null) break; // loop finished
					// we can heal a unit that is at the same position, unconscious and healable(=woundable)
					if (bu.Position == selectedUnit.Position && bu != selectedUnit && bu.Status == STATUS_UNCONSCIOUS && (bu.IsWoundable || weaponRule.AllowTargetImmune) && weaponRule.AllowTargetGround)
					{
						if (bu.IsBigUnit)
						{
							// never EVER apply anything to 2x2 units on the ground
							continue;
						}
						if ((weaponRule.AllowTargetFriendGround && bu.OriginalFaction == FACTION_PLAYER) ||
							(weaponRule.AllowTargetNeutralGround && bu.OriginalFaction == FACTION_NEUTRAL) ||
							(weaponRule.AllowTargetHostileGround && bu.OriginalFaction == FACTION_HOSTILE))
						{
							targetUnit = bu;
							onGround = true;
						}
					}
				}

				// search for target in front of the selected unit
				if (targetUnit == null && weaponRule.AllowTargetStanding)
				{
					Position dest = default;
					if (_save.TileEngine.ValidMeleeRange(
						selectedUnit.Position,
						selectedUnit.Direction,
						selectedUnit,
						null, ref dest, false))
					{
						Tile tile = _save.GetTile(dest);
						if (tile != null && tile.Unit != null && (tile.Unit.IsWoundable || weaponRule.AllowTargetImmune))
						{
							if ((weaponRule.AllowTargetFriendStanding && tile.Unit.OriginalFaction == FACTION_PLAYER) ||
								(weaponRule.AllowTargetNeutralStanding && tile.Unit.OriginalFaction == FACTION_NEUTRAL) ||
								(weaponRule.AllowTargetHostileStanding && tile.Unit.OriginalFaction == FACTION_HOSTILE))
							{
								targetUnit = tile.Unit;
							}
						}
					}
				}

				_currentTooltip = action.Sender.Tooltip;
				var tooltipExtra = new StringBuilder();
				tooltipExtra.Append((string)Tr(_currentTooltip));

				// target unit found
				if (targetUnit != null)
				{
					if (targetUnit.OriginalFaction == FACTION_HOSTILE)
					{
						_txtTooltip.SetColor(Acom.Palette.BlockOffset(_medikitRed));
						tooltipExtra.Append((string)Tr("STR_TARGET_ENEMY"));
					}
					else if (targetUnit.OriginalFaction == FACTION_NEUTRAL)
					{
						_txtTooltip.SetColor(Acom.Palette.BlockOffset(_medikitOrange));
						tooltipExtra.Append((string)Tr("STR_TARGET_NEUTRAL"));
					}
					else if (targetUnit.OriginalFaction == FACTION_PLAYER)
					{
						_txtTooltip.SetColor(Acom.Palette.BlockOffset(_medikitGreen));
						tooltipExtra.Append((string)Tr("STR_TARGET_FRIEND"));
					}
					if (onGround) tooltipExtra.Append((string)Tr("STR_TARGET_ON_THE_GROUND"));
					_txtTooltip.SetText(tooltipExtra.ToString());
				}
				else
				{
					// target unit not found => selected unit is the target (if self-heal is possible)
					if (weaponRule.AllowTargetSelf)
					{
						_txtTooltip.SetColor(Acom.Palette.BlockOffset(_medikitBlue));
						tooltipExtra.Append((string)Tr("STR_TARGET_YOURSELF"));
						if (onGround) tooltipExtra.Append((string)Tr("STR_TARGET_ON_THE_GROUND"));
						_txtTooltip.SetText(tooltipExtra.ToString());
					}
					else
					{
						// cannot use the weapon (medikit) on anyone
						_currentTooltip = action.Sender.Tooltip;
						_txtTooltip.SetText(Tr(_currentTooltip));
					}
				}
			}
			else
			{
				// weapon is not of medikit battle type
				_currentTooltip = action.Sender.Tooltip;
				_txtTooltip.SetText(Tr(_currentTooltip));
			}
		}
	}

	/// <summary>Shows a tooltip with extra information (used for medikit-type equipment).</summary>
	public void TxtTooltipInExtraLeftHand(Action action)
	{
		TxtTooltipInExtra(action, true);
	}

	/// <summary>Shows a tooltip with extra information (used for medikit-type equipment).</summary>
	public void TxtTooltipInExtraRightHand(Action action)
	{
		TxtTooltipInExtra(action, false);
	}

	/// <summary>Shows a tooltip with extra information (used for medikit-type equipment).</summary>
	public void TxtTooltipInExtraSpecial(Action action)
	{
		TxtTooltipInExtra(action, false, true);
	}

	/// <summary>Shows a tooltip for the End Turn button.</summary>
	public void TxtTooltipInEndTurn(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (AllowButtons() && Options.battleTooltips)
		{
			_currentTooltip = action.Sender.Tooltip;

			var ss = new StringBuilder();
			ss.Append((string)Tr(_currentTooltip));
			ss.Append(' ');
			ss.Append(_save.Turn.ToString(System.Globalization.CultureInfo.InvariantCulture));
			if (_save.TurnLimit > 0)
			{
				ss.Append('/').Append(_save.TurnLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
			}

			_txtTooltip.SetText(ss.ToString());
		}
	}

	/// <summary>Shows a tooltip for the appropriate button.</summary>
	public void TxtTooltipIn(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (AllowButtons() && Options.battleTooltips)
		{
			_currentTooltip = action.Sender.Tooltip;
			_txtTooltip.SetText(Tr(_currentTooltip));
		}
	}

	/// <summary>Clears the tooltip text.</summary>
	public void TxtTooltipOut(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		// reset color
		_txtTooltip.SetColor(_tooltipDefaultColor);

		if (AllowButtons() && Options.battleTooltips)
		{
			if (_currentTooltip == action.Sender.Tooltip)
			{
				_txtTooltip.SetText("");
			}
		}
	}

	/// <summary>
	/// Updates the scale.
	/// </summary>
	/// <param name="dX">delta of X;</param>
	/// <param name="dY">delta of Y;</param>
	public override void Resize(ref int dX, ref int dY)
	{
		dX = Options.baseXResolution;
		dY = Options.baseYResolution;
		int divisor = 1;
		double pixelRatioY = 1.0;

		if (Options.nonSquarePixelRatio)
		{
			pixelRatioY = 1.2;
		}
		switch ((ScaleType)Options.battlescapeScale)
		{
			case ScaleType.SCALE_SCREEN_DIV_10:
				divisor = 10;
				break;
			case ScaleType.SCALE_SCREEN_DIV_8:
				divisor = 8;
				break;
			case ScaleType.SCALE_SCREEN_DIV_6:
				divisor = 6;
				break;
			case ScaleType.SCALE_SCREEN_DIV_5:
				divisor = 5;
				break;
			case ScaleType.SCALE_SCREEN_DIV_4:
				divisor = 4;
				break;
			case ScaleType.SCALE_SCREEN_DIV_3:
				divisor = 3;
				break;
			case ScaleType.SCALE_SCREEN_DIV_2:
				divisor = 2;
				break;
			case ScaleType.SCALE_SCREEN:
				break;
			default:
				dX = 0;
				dY = 0;
				return;
		}

		Options.baseXResolution = Math.Max(Screen.ORIGINAL_WIDTH, Options.displayWidth / divisor);
		Options.baseYResolution = Math.Max(Screen.ORIGINAL_HEIGHT, (int)(Options.displayHeight / pixelRatioY / divisor));

		dX = Options.baseXResolution - dX;
		dY = Options.baseYResolution - dY;
		_map.SetWidth(Options.baseXResolution);
		_map.SetHeight(Options.baseYResolution);
		_map.Camera.Resize();
		_map.Camera.JumpXY(dX / 2, dY / 2);

		foreach (Surface surf in _surfaces)
		{
			if (surf == _btnCtrl || surf == _btnAlt || surf == _btnShift || surf == _btnRMB || surf == _btnMMB)
			{
				continue;
			}
			if (surf != _map && surf != _btnPsi && surf != _btnLaunch && surf != _btnSpecial && surf != _btnSkills && surf != _txtDebug)
			{
				surf.X = surf.X + dX / 2;
				surf.Y = surf.Y + dY;
			}
			else if (surf != _map && surf != _txtDebug)
			{
				surf.X = surf.X + dX;
			}
		}

		for (int i = 0; i < _posSpecialActions.Length; ++i)
		{
			_posSpecialActions[i] += dX;
		}
	}

	/// <summary>
	/// Move the mouse back to where it started after we finish drag scrolling.
	/// </summary>
	public void StopScrolling(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (Options.battleDragScrollInvert)
		{
			// C++: SDL_WarpMouse(_xBeforeMouseScrolling, _yBeforeMouseScrolling); missing, the host cannot warp the pointer yet.
			action.SetMouseAction(_xBeforeMouseScrolling, _yBeforeMouseScrolling, _map.X, _map.Y);
			_battleGame.SetupCursor();
			if (_battleGame.CurrentAction.actor == null && (_save.Side == FACTION_PLAYER || _save.DebugMode))
			{
				Map.SetCursorType(CursorType.CT_NORMAL);
			}
		}
		else
		{
			// C++: SDL_WarpMouse(_cursorPosition.x, _cursorPosition.y); missing, the host cannot warp the pointer yet.
			action.SetMouseAction(_cursorPosition.x, _cursorPosition.y, _map.X, _map.Y);
			_map.SetSelectorPosition((int)action.AbsoluteXMouse, (int)action.AbsoluteYMouse);
		}
		// reset our "mouse position stored" flag
		_cursorPosition.z = 0;
	}
}
