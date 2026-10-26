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
// Ported from: src/Battlescape/BattlescapeState.cpp (icon buttons)
#nullable disable

using static Acom.BattleActionType;
using static Acom.BattleType;
using static Acom.UnitFaction;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

public partial class BattlescapeState
{
	/// <summary>Moves the selected unit up.</summary>
	public void BtnUnitUpClick(Action action)
	{
		if (PlayableUnitSelected() && _save.Pathfinding.ValidateUpDown(_save.SelectedUnit, _save.SelectedUnit.Position, Pathfinding.DIR_UP))
		{
			_battleGame.CancelAllActions();
			_battleGame.MoveUpDown(_save.SelectedUnit, Pathfinding.DIR_UP);
		}
	}

	/// <summary>Moves the selected unit down.</summary>
	public void BtnUnitDownClick(Action action)
	{
		if (PlayableUnitSelected() && _save.Pathfinding.ValidateUpDown(_save.SelectedUnit, _save.SelectedUnit.Position, Pathfinding.DIR_DOWN))
		{
			_battleGame.CancelAllActions();
			_battleGame.MoveUpDown(_save.SelectedUnit, Pathfinding.DIR_DOWN);
		}
	}

	/// <summary>Shows the next map layer.</summary>
	public void BtnMapUpClick(Action action)
	{
		if (_save.Side == FACTION_PLAYER || _save.DebugMode)
			_map.Camera.Up();
	}

	/// <summary>Shows the previous map layer.</summary>
	public void BtnMapDownClick(Action action)
	{
		if (_save.Side == FACTION_PLAYER || _save.DebugMode)
			_map.Camera.Down();
	}

	/// <summary>Shows the minimap.</summary>
	public void BtnShowMapClick(Action action)
	{
		//MiniMapState
		if (AllowButtons())
			_game.PushState(new MiniMapState(_map.Camera, _save));
	}

	/// <summary>Updates the kneel button for the unit.</summary>
	private void ToggleKneelButton(BattleUnit unit)
	{
		if (_btnKneel.TFTDMode)
		{
			_btnKneel.Toggle(unit != null && unit.IsKneeled);
		}
		else
		{
			_game.Mod.GetSurfaceSet("KneelButton").GetFrame((unit != null && unit.IsKneeled) ? 1 : 0).BlitNShade(_btnKneel, 0, 0);
		}
	}

	/// <summary>Toggles the current unit's kneel/standup status.</summary>
	public void BtnKneelClick(Action action)
	{
		if (AllowButtons())
		{
			BattleUnit bu = _save.SelectedUnit;
			if (bu != null)
			{
				_battleGame.Kneel(bu);
				ToggleKneelButton(bu);

				// update any path preview when unit kneels
				if (_battleGame.Pathfinding.IsPathPreviewed)
				{
					_battleGame.Pathfinding.RefreshPath();
				}
			}
		}
	}

	/// <summary>Goes to the soldier info screen.</summary>
	public void BtnInventoryClick(Action action)
	{
		if (_save.IsPreview)
		{
			return;
		}
		if (PlayableUnitSelected()
			&& (_save.SelectedUnit.HasInventory || _save.DebugMode))
		{
			_battleGame.CancelAllActions();
			_game.PushState(new InventoryState(true, this, null));
		}
	}

	/// <summary>Centers on the currently selected soldier.</summary>
	public void BtnCenterClick(Action action)
	{
		if (PlayableUnitSelected())
		{
			_map.Camera.CenterOnPosition(_save.SelectedUnit.Position);
			_map.RefreshSelectorPosition();
		}
	}

	/// <summary>Selects the next soldier.</summary>
	public void BtnNextSoldierClick(Action action)
	{
		if (_game.IsRightClick(action, true))
		{
			BtnPrevSoldierClick(action);
			return;
		}

		if (AllowButtons())
		{
			SelectNextPlayerUnit(true, false);
			_map.RefreshSelectorPosition();
		}
	}

	/// <summary>Disables reselection of the current soldier and selects the next soldier.</summary>
	public void BtnNextStopLClick(Action action)
	{
		if (AllowButtons())
		{
			// vanilla: next by ID + don't reselect
			_save.UndoUnit = _save.SelectedUnit;
			SelectNextPlayerUnit(true, true);
			_map.RefreshSelectorPosition();
		}
	}

	/// <summary>Disables reselection of the current soldier and selects the next soldier (by distance).</summary>
	public void BtnNextStopMClick(Action action)
	{
		if (AllowButtons())
		{
			// OXCE: next by distance + don't reselect
			_save.UndoUnit = _save.SelectedUnit;
			SelectNextPlayerUnit(true, true, false, true, true);
			_map.RefreshSelectorPosition();
		}
	}

	/// <summary>Selects the previous soldier (last marked as don't reselect).</summary>
	public void BtnNextStopRClick(Action action)
	{
		if (AllowButtons())
		{
			// OXCE: previous unit (last marked as don't reselect)
			BattleUnit candidate = _save.UndoUnit;
			if (candidate != null && candidate.IsSelectable(_save.Side, false, false))
			{
				candidate.AllowReselect();
				_save.SelectedUnit = candidate;
				_save.UndoUnit = null;

				UpdateSoldierInfo();
				if (candidate != null && !_game.IsShiftPressed(true)) _map.Camera.CenterOnPosition(candidate.Position);
				_battleGame.CancelAllActions();
				_battleGame.CurrentAction.actor = candidate;
				_battleGame.SetupCursor();

				_map.RefreshSelectorPosition();
			}
		}
	}

	/// <summary>Selects previous soldier.</summary>
	public void BtnPrevSoldierClick(Action action)
	{
		if (AllowButtons())
		{
			SelectPreviousPlayerUnit(true);
			_map.RefreshSelectorPosition();
		}
	}

	/// <summary>
	/// Selects the next soldier.
	/// </summary>
	/// <param name="checkReselect">When true, don't select a unit that has been previously flagged.</param>
	/// <param name="setReselect">When true, flag the current unit first.</param>
	/// <param name="checkInventory">When true, don't select a unit that has no inventory.</param>
	/// <param name="checkFOV">Recalculate the field of view of the new unit.</param>
	/// <param name="byDistance">Select the nearest unit instead of the next by ID.</param>
	public void SelectNextPlayerUnit(bool checkReselect = false, bool setReselect = false, bool checkInventory = false, bool checkFOV = true, bool byDistance = false)
	{
		if (AllowButtons())
		{
			BattleUnit unit = byDistance
				? _save.SelectNextPlayerUnitByDistance(checkReselect, setReselect, checkInventory)
				: _save.SelectNextPlayerUnit(checkReselect, setReselect, checkInventory);
			UpdateSoldierInfo(checkFOV);
			if (unit != null && !_game.IsShiftPressed(true)) _map.Camera.CenterOnPosition(unit.Position);
			_battleGame.CancelAllActions();
			_battleGame.CurrentAction.actor = unit;
			_battleGame.SetupCursor();
		}
	}

	/// <summary>
	/// Selects the previous soldier.
	/// </summary>
	/// <param name="checkReselect">When true, don't select a unit that has been previously flagged.</param>
	/// <param name="setReselect">When true, flag the current unit first.</param>
	/// <param name="checkInventory">When true, don't select a unit that has no inventory.</param>
	public void SelectPreviousPlayerUnit(bool checkReselect = false, bool setReselect = false, bool checkInventory = false)
	{
		if (AllowButtons())
		{
			BattleUnit unit = _save.SelectPreviousPlayerUnit(checkReselect, setReselect, checkInventory);
			UpdateSoldierInfo();
			if (unit != null && !_game.IsShiftPressed(true)) _map.Camera.CenterOnPosition(unit.Position);
			_battleGame.CancelAllActions();
			_battleGame.CurrentAction.actor = unit;
			_battleGame.SetupCursor();
		}
	}

	/// <summary>Shows/hides all map layers.</summary>
	public void BtnShowLayersClick(Action action)
	{
		if (Options.oxceLinks)
		{
			_game.PushState(new ExtendedBattlescapeLinksState(this, _save));
		}
		else
		{
			BtnShowLayersClickOrig(null);
		}
	}

	/// <summary>Shows/hides all map layers (original behaviour).</summary>
	public void BtnShowLayersClickOrig(Action action)
	{
		_numLayers.Value = (uint)_map.Camera.ToggleShowAllLayers();
	}

	/// <summary>Opens the Ufopaedia.</summary>
	public void BtnUfopaediaClick(Action action)
	{
		if (AllowButtons())
		{
			Ufopaedia.Open(_game);
		}
	}

	/// <summary>Shows options.</summary>
	public void BtnHelpClick(Action action)
	{
		if (_save.IsPreview)
		{
			// Notes for future explorers:
			// 1. saving makes no sense
			// 2. loading could be enabled, but needs changes in the Game's _states management; make sure you know what you're doing!
			return;
		}

		if (AllowButtons(true))
			_game.PushState(new PauseState(OptionsOrigin.OPT_BATTLESCAPE));
	}

	/// <summary>
	/// Requests the end of turn. This will add a 0 to the end of the state queue,
	/// so all ongoing actions, like explosions are finished first before really switching turn.
	/// </summary>
	public void BtnEndTurnClick(Action action)
	{
		if (AllowButtons())
		{
			// Temporarily deactivate the touch buttons at the end of the player's turn
			ToggleTouchButtons(true, false);

			// PEBCAK
			_map.Camera.StopKeyScrolling();

			_txtTooltip.SetText("");
			_battleGame.RequestEndTurn(false);
		}
	}

	/// <summary>Aborts the game.</summary>
	public void BtnAbortClick(Action action)
	{
		if (_save.IsPreview)
		{
			if (_save.CraftForPreview == null)
			{
				// base preview
				return;
			}
			// Notes for future explorers:
			// - there are craft, which can have multiple layouts (one is chosen randomly at the start of a battle)
			// - these layouts are not forced to be compatible
			// - thus custom craft deployment for one layout may not be compatible with another layout either
			// - so instead of having multiple custom deployments per craft, I decided to not support it for such craft at all
			// - if you want to add partial or full support for it... make sure you don't forget all the corner cases
			if (_save.CraftForPreview.Rules.BattlescapeTerrainData.MapBlocks.Count > 1)
			{
				return;
			}
		}

		if (AllowButtons())
			_game.PushState(new AbortMissionState(_save, this));
	}

	/// <summary>Shows the selected soldier's info.</summary>
	public void BtnStatsClick(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (PlayableUnitSelected())
		{
			bool scroll = false;
			if (ScrollType.SCROLL_TRIGGER == Options.battleEdgeScroll &&
				SDLEventType.SDL_MOUSEBUTTONUP == action.Details.type && _game.IsLeftClick(action))
			{
				int posX = action.XMouse;
				int posY = action.YMouse;
				if ((posX < (Camera.SCROLL_BORDER * action.XScale) && posX > 0)
					|| (posX > (_map.Width - Camera.SCROLL_BORDER) * action.XScale)
					|| (posY < (Camera.SCROLL_BORDER * action.YScale) && posY > 0)
					|| (posY > (_map.Height - Camera.SCROLL_BORDER) * action.YScale))
					// To avoid handling this event as a click
					// on the stats button when the mouse is on the scroll-border
					scroll = true;
			}
			if (!scroll)
			{
				if (_game.IsRightClick(action))
				{
					_save.SetNameDisplay(!_save.IsNameDisplay);
					UpdateSoldierInfo();
				}
				else
				{
					_battleGame.CancelAllActions();
					Popup(new UnitInfoState(_save.SelectedUnit, this, false, false));
				}
			}
		}
	}

	/// <summary>Special weapon types usable with an empty hand, in the order the C++ code checks them.</summary>
	private static readonly BattleType[] s_emptyHandTypes = [BT_MELEE, BT_PSIAMP, BT_FIREARM, BT_MEDIKIT, BT_SCANNER, BT_MINDPROBE];

	/// <summary>Finds the special weapon used with an empty hand.</summary>
	private static BattleItem FindEmptyHandSpecialWeapon(BattleUnit unit)
	{
		foreach (BattleType type in s_emptyHandTypes)
		{
			BattleItem item = unit.GetSpecialWeapon(type);
			if (item != null && item.Rules.IsSpecialUsingEmptyHand)
			{
				return item;
			}
		}
		return null;
	}

	/// <summary>Shows an action popup menu. When clicked, creates the action.</summary>
	public void BtnLeftHandItemClick(Action action)
	{
		if (PlayableUnitSelected())
		{
			// concession for touch devices:
			// click on the item to cancel action, and don't pop up a menu to select a new one
			// TODO: wrap this in an IFDEF ?
			if (_battleGame.CurrentAction.targeting)
			{
				_battleGame.CancelCurrentAction();
				return;
			}

			_battleGame.CancelCurrentAction();

			_save.SelectedUnit.SetActiveLeftHand();
			_map.Draw();

			bool rightClick = _game.IsRightClick(action, true);
			if (rightClick)
			{
				bool isCtrl = _game.IsCtrlPressed(true);
				_save.SelectedUnit.ToggleLeftHandForReactions(isCtrl);
				return;
			}

			BattleItem leftHandItem = _save.SelectedUnit.LeftHandWeapon;
			if (leftHandItem == null)
			{
				leftHandItem = FindEmptyHandSpecialWeapon(_save.SelectedUnit);
			}
			bool middleClick = _game.IsMiddleClick(action, true);
			HandleItemClick(leftHandItem, middleClick);
		}
	}

	/// <summary>Shows an action popup menu. When clicked, create the action.</summary>
	public void BtnRightHandItemClick(Action action)
	{
		if (PlayableUnitSelected())
		{
			// concession for touch devices:
			// click on the item to cancel action, and don't pop up a menu to select a new one
			// TODO: wrap this in an IFDEF ?
			if (_battleGame.CurrentAction.targeting)
			{
				_battleGame.CancelCurrentAction();
				return;
			}

			_battleGame.CancelCurrentAction();

			_save.SelectedUnit.SetActiveRightHand();
			_map.Draw();

			bool rightClick = _game.IsRightClick(action, true);
			if (rightClick)
			{
				bool isCtrl = _game.IsCtrlPressed(true);
				_save.SelectedUnit.ToggleRightHandForReactions(isCtrl);
				return;
			}

			BattleItem rightHandItem = _save.SelectedUnit.RightHandWeapon;
			if (rightHandItem == null)
			{
				rightHandItem = FindEmptyHandSpecialWeapon(_save.SelectedUnit);
			}
			bool middleClick = _game.IsMiddleClick(action, true);
			HandleItemClick(rightHandItem, middleClick);
		}
	}

	/// <summary>Centers on the unit corresponding to this button.</summary>
	public void BtnVisibleUnitClick(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		int btnID = -1;

		// got to find out which button was pressed
		for (int i = 0; i < VISIBLE_MAX && btnID == -1; ++i)
		{
			if (action.Sender == _btnVisibleUnit[i])
			{
				btnID = i;
			}
		}

		if (btnID != -1 && _game.IsRightClick(action, true))
		{
			if (AllowButtons())
			{
				BattleUnit targetUnit = _visibleUnit[btnID];
				var sortSpotters = new List<(BattleUnit first, int second)>();
				foreach (BattleUnit unit in _save.Units)
				{
					if (unit.IsSelectable(_save.Side, false, false) && unit.HasVisibleUnit(targetUnit))
					{
						int tuPercent = unit.BaseStats.tu > 0 ? (unit.TimeUnits * 100 / unit.BaseStats.tu) : 0;
						sortSpotters.Add((unit, tuPercent));
					}
				}
				if (sortSpotters.Count != 0)
				{
					// std::stable_sort, descending by TU percent
					sortSpotters = sortSpotters.OrderByDescending(p => p.second).ToList();
					// select the first (= with most TU percent left)
					_battleGame.CancelAllActions();
					Position position = sortSpotters[0].first.Position;
					_battleGame.PrimaryAction(position);
					_map.Camera.CenterOnPosition(position);
				}
			}
		}
		else if (btnID != -1)
		{
			Position position = _visibleUnit[btnID].Position;
			if (position == TileEngine.invalid)
			{
				bool found = false;
				foreach (BattleUnit unit in _save.Units)
				{
					if (!unit.IsOut)
					{
						foreach (BattleItem invItem in unit.Inventory)
						{
							if (invItem.Unit != null && invItem.Unit == _visibleUnit[btnID])
							{
								position = unit.Position; // position of a unit that has the wounded unit in the inventory
								found = true;
								break;
							}
						}
					}
					if (found) break;
				}
			}
			_map.Camera.CenterOnPosition(position);
		}

		action.Details.type = SDLEventType.SDL_NOEVENT; // consume the event
	}

	public void BtnCtrlClick(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (AllowButtons())
		{
			_game.ToggleCtrlPressedFlag();
			_btnCtrl.Toggle(_game.CtrlPressedFlag);
		}

		action.Details.type = SDLEventType.SDL_NOEVENT; // consume the event
	}

	public void BtnAltClick(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (AllowButtons())
		{
			_game.ToggleAltPressedFlag();
			_btnAlt.Toggle(_game.AltPressedFlag);
		}

		action.Details.type = SDLEventType.SDL_NOEVENT; // consume the event
	}

	public void BtnShiftClick(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (AllowButtons())
		{
			_game.ToggleShiftPressedFlag();
			_btnShift.Toggle(_game.ShiftPressedFlag);
		}

		action.Details.type = SDLEventType.SDL_NOEVENT; // consume the event
	}

	public void BtnRMBClick(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (AllowButtons())
		{
			_game.ToggleRMBFlag();
			_btnRMB.Toggle(_game.RMBFlag);
			if (_game.RMBFlag && _game.MMBFlag)
			{
				_game.ToggleMMBFlag();
				_btnMMB.Toggle(_game.MMBFlag);
			}
		}

		action.Details.type = SDLEventType.SDL_NOEVENT; // consume the event
	}

	public void BtnMMBClick(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (AllowButtons())
		{
			_game.ToggleMMBFlag();
			_btnMMB.Toggle(_game.MMBFlag);
			if (_game.RMBFlag && _game.MMBFlag)
			{
				_game.ToggleRMBFlag();
				_btnRMB.Toggle(_game.RMBFlag);
			}
		}

		action.Details.type = SDLEventType.SDL_NOEVENT; // consume the event
	}

	/// <summary>Toggles touch buttons.</summary>
	public void BtnTouchButtonsClick(Action action)
	{
		if (AllowButtons())
			ToggleTouchButtons(false, false);
	}

	/// <summary>Shows/hides the touch buttons.</summary>
	public void ToggleTouchButtons(bool deactivate, bool tryToReactivate)
	{
		// Reset touch flags
		_game.ResetTouchButtonFlags();

		// Reset touch buttons
		_btnCtrl.Toggle(_game.CtrlPressedFlag);
		_btnAlt.Toggle(_game.AltPressedFlag);
		_btnShift.Toggle(_game.ShiftPressedFlag);
		_btnRMB.Toggle(_game.RMBFlag);
		_btnMMB.Toggle(_game.MMBFlag);

		if (tryToReactivate)
		{
			_touchButtonsEnabled = Options.oxceBattleTouchButtonsEnabled; // restore
		}
		else if (deactivate)
		{
			Options.oxceBattleTouchButtonsEnabled = _touchButtonsEnabled; // backup
			_touchButtonsEnabled = false;
		}
		else
		{
			_touchButtonsEnabled = !_touchButtonsEnabled;
			Options.oxceBattleTouchButtonsEnabled = _touchButtonsEnabled; // backup
		}

		_btnCtrl.Visible = _touchButtonsEnabled;
		_btnAlt.Visible = _touchButtonsEnabled;
		_btnShift.Visible = _touchButtonsEnabled;
		_btnRMB.Visible = _touchButtonsEnabled;
		_btnMMB.Visible = _touchButtonsEnabled;
	}

	/// <summary>Launches the blaster bomb.</summary>
	public void BtnLaunchClick(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		_battleGame.LaunchAction();
		action.Details.type = SDLEventType.SDL_NOEVENT; // consume the event
	}

	/// <summary>Uses psionics.</summary>
	public void BtnPsiClick(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		_battleGame.PsiButtonAction();
		action.Details.type = SDLEventType.SDL_NOEVENT; // consume the event
	}

	/// <summary>Shows action menu for special weapons.</summary>
	public void BtnSpecialClick(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (PlayableUnitSelected())
		{
			// concession for touch devices:
			// click on the item to cancel action, and don't pop up a menu to select a new one
			// TODO: wrap this in an IFDEF ?
			if (_battleGame.CurrentAction.targeting)
			{
				_battleGame.CancelCurrentAction();
				return;
			}

			_battleGame.CancelCurrentAction();

			BattleType type = BT_NONE;
			BattleItem specialItem = _save.SelectedUnit.GetSpecialIconWeapon(ref type);
			if (specialItem == null)
			{
				// Note: this is a hack to access the soldier skills button via the same hotkey as the special weapon button
				if (_btnSkills.Visible)
				{
					BtnSkillsClick(action);
				}
				return;
			}

			_map.Draw();
			bool middleClick = _game.IsMiddleClick(action, true);
			HandleItemClick(specialItem, middleClick);
		}
		action.Details.type = SDLEventType.SDL_NOEVENT; // consume the event
	}

	/// <summary>Shows action menu for the skills feature.</summary>
	public void BtnSkillsClick(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (PlayableUnitSelected() && !_battleGame.IsBusy)
		{
			Popup(new SkillMenuState(_battleGame.CurrentAction, _icons.X, _icons.Y + 16));
		}
		action.Details.type = SDLEventType.SDL_NOEVENT; // consume the event
	}

	/// <summary>Presses the sender button like a left click (C++ fake SDL_MOUSEBUTTONDOWN action).</summary>
	private void PressSender(Action action)
	{
		var ev = new SDL_Event { type = SDLEventType.SDL_MOUSEBUTTONDOWN };
		ev.button.button = SDLMouse.SDL_BUTTON_LEFT;
		var a = new Action(ev, 0.0, 0.0, 0, 0);
		action.Sender.MousePress(a, this);
	}

	/// <summary>Reserves time units.</summary>
	public void BtnReserveClick(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (AllowButtons())
		{
			PressSender(action);

			if (_reserve.Value == _btnReserveNone)
				_battleGame.SetTUReserved(BA_NONE);
			else if (_reserve.Value == _btnReserveSnap)
				_battleGame.SetTUReserved(BA_SNAPSHOT);
			else if (_reserve.Value == _btnReserveAimed)
				_battleGame.SetTUReserved(BA_AIMEDSHOT);
			else if (_reserve.Value == _btnReserveAuto)
				_battleGame.SetTUReserved(BA_AUTOSHOT);

			// update any path preview
			if (_battleGame.Pathfinding.IsPathPreviewed)
			{
				_battleGame.Pathfinding.RefreshPath();
			}
		}
	}

	/// <summary>Reloads the weapon in hand.</summary>
	public void BtnReloadClick(Action action)
	{
		if (PlayableUnitSelected() && _save.SelectedUnit.ReloadAmmo())
		{
			_game.Mod.GetSoundByDepth((uint)_save.Depth, (uint)_save.SelectedUnit.ReloadSound).Play(-1, Map.GetSoundAngle(_save.SelectedUnit.Position));
			UpdateSoldierInfo();
		}
	}

	/// <summary>Opens the jukebox.</summary>
	public void BtnSelectMusicTrackClick(Action action)
	{
		if (AllowButtons())
		{
			_game.PushState(new SelectMusicTrackState(SelectMusicTrackOrigin.SMT_BATTLESCAPE));
		}
	}

	/// <summary>Toggles soldier's personal lighting.</summary>
	public void BtnPersonalLightingClick(Action action)
	{
		if (AllowButtons())
			_save.TileEngine.TogglePersonalLighting();
	}

	/// <summary>Toggles night vision (purely cosmetic).</summary>
	public void BtnNightVisionClick(Action action)
	{
		if (AllowButtons())
			_map.ToggleNightVision();
	}

	/// <summary>
	/// Determines whether a playable unit is selected. Normally only player side units can be selected, but in debug mode one can play with aliens too :)
	/// Is used to see if action buttons will work.
	/// </summary>
	public bool PlayableUnitSelected()
	{
		return _save.SelectedUnit != null && AllowButtons();
	}

	/// <summary>Reserves time units for kneeling.</summary>
	public void BtnReserveKneelClick(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (AllowButtons())
		{
			PressSender(action);
			_battleGame.KneelReserved = !_battleGame.KneelReserved;

			_btnReserveKneel.Toggle(_battleGame.KneelReserved);

			// update any path preview
			if (_battleGame.Pathfinding.IsPathPreviewed)
			{
				_battleGame.Pathfinding.RefreshPath();
			}
		}
	}

	/// <summary>Removes all time units.</summary>
	public void BtnZeroTUsClick(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (AllowButtons())
		{
			PressSender(action);
			if (_battleGame.Save.SelectedUnit != null)
			{
				_battleGame.Save.SelectedUnit.ClearTimeUnits();
				UpdateSoldierInfo();
			}
		}
	}

	/// <summary>
	/// Popups a context sensitive list of actions the user can choose from.
	/// Some actions result in a change of gamestate.
	/// </summary>
	/// <param name="item">Item the user clicked on (righthand/lefthand)</param>
	/// <param name="middleClick">was it a middle click?</param>
	private void HandleItemClick(BattleItem item, bool middleClick)
	{
		// make sure there is an item, and the battlescape is in an idle state
		if (item != null && !_battleGame.IsBusy)
		{
			if (middleClick)
			{
				string articleId = item.Rules.UfopediaType;
				Ufopaedia.OpenArticle(_game, articleId);
			}
			else
			{
				_battleGame.CurrentAction.weapon = item;
				Popup(new ActionMenuState(_battleGame.CurrentAction, _icons.X, _icons.Y + 16));
				if (item.Rules.BattleType == BT_FIREARM)
				{
					_battleGame.PlayUnitResponseSound(_battleGame.CurrentAction.actor, 2); // "select weapon" sound
				}
			}
		}
	}
}
