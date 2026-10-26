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
// Ported from: src/Battlescape/ActionMenuState.h, src/Battlescape/ActionMenuState.cpp
#nullable disable

using static Acom.BattleActionType;
using static Acom.BattleFuseType;
using static Acom.BattleMediKitAction;
using static Acom.BattleMediKitType;
using static Acom.BattleType;
using static Acom.UnitBodyPart;
using static Acom.UnitFaction;

namespace Acom;

/// <summary>
/// Window that allows the player
/// to select a battlescape action.
/// </summary>
public class ActionMenuState : State
{
	protected BattleAction _action;
	protected ActionMenuItem[] _actionMenu = new ActionMenuItem[6];

	/// <summary>
	/// Default constructor, used by SkillMenuState.
	/// </summary>
	public ActionMenuState(BattleAction action)
	{
		_action = action;
	}

	/// <summary>
	/// Initializes all the elements in the Action Menu window.
	/// </summary>
	/// <param name="action">Pointer to the action.</param>
	/// <param name="x">Position on the x-axis.</param>
	/// <param name="y">position on the y-axis.</param>
	public ActionMenuState(BattleAction action, int x, int y)
	{
		_action = action;
		_screen = false;

		// Set palette
		_game.SavedGame.SavedBattle.SetPaletteByDepth(this);

		for (int i = 0; i < 6; ++i)
		{
			_actionMenu[i] = new ActionMenuItem(i, _game, x, y);
			Add(_actionMenu[i]);
			_actionMenu[i].Visible = false;
			_actionMenu[i].OnMouseClick(BtnActionMenuItemClick);
		}

		// Build up the popup menu
		int id = 0;
		RuleItem weapon = _action.weapon.Rules;

		// throwing (if not a fixed weapon)
		if (!weapon.IsFixed && weapon.CostThrow.Time > 0)
		{
			AddItem(BA_THROW, "STR_THROW", ref id, Options.keyBattleActionItem5);
		}

		if (weapon.IsPsiRequired && _action.actor.BaseStats.psiSkill <= 0)
		{
			return;
		}

		if (weapon.IsManaRequired && _action.actor.OriginalFaction == FACTION_PLAYER)
		{
			if (!_game.Mod.IsManaFeatureEnabled || !_game.SavedGame.IsManaUnlocked(_game.Mod))
			{
				return;
			}
		}

		// priming
		if (weapon.FuseTimerType != BFT_NONE)
		{
			bool normalWeapon = weapon.BattleType != BT_GRENADE && weapon.BattleType != BT_FLARE && weapon.BattleType != BT_PROXIMITYGRENADE;
			if (_action.weapon.FuseTimer == -1)
			{
				if (weapon.CostPrime.Time > 0)
				{
					AddItem(BA_PRIME, weapon.PrimeActionName, ref id, normalWeapon ? SDLKey.SDLK_UNKNOWN : Options.keyBattleActionItem1);
				}
			}
			else
			{
				if (weapon.CostUnprime.Time > 0 && !string.IsNullOrEmpty(weapon.UnprimeActionName))
				{
					AddItem(BA_UNPRIME, weapon.UnprimeActionName, ref id, normalWeapon ? SDLKey.SDLK_UNKNOWN : Options.keyBattleActionItem2);
				}
			}
		}

		if (weapon.BattleType == BT_FIREARM)
		{
			bool isLauncher = _action.weapon.CurrentWaypoints != 0;
			int slotLauncher = _action.weapon.GetActionConf(BA_LAUNCH).ammoSlot;
			int slotSnap = _action.weapon.GetActionConf(BA_SNAPSHOT).ammoSlot;
			int slotAuto = _action.weapon.GetActionConf(BA_AUTOSHOT).ammoSlot;

			if ((!isLauncher || slotLauncher != slotAuto) && weapon.CostAuto.Time > 0)
			{
				AddItem(BA_AUTOSHOT, weapon.ConfigAuto.name, ref id, Options.keyBattleActionItem3);
			}

			if ((!isLauncher || slotLauncher != slotSnap) && weapon.CostSnap.Time > 0)
			{
				AddItem(BA_SNAPSHOT, weapon.ConfigSnap.name, ref id, Options.keyBattleActionItem2);
			}

			if (isLauncher)
			{
				AddItem(BA_LAUNCH, "STR_LAUNCH_MISSILE", ref id, Options.keyBattleActionItem1);
			}
			else if (weapon.CostAimed.Time > 0)
			{
				AddItem(BA_AIMEDSHOT, weapon.ConfigAimed.name, ref id, Options.keyBattleActionItem1);
			}
		}

		if (weapon.CostMelee.Time > 0)
		{
			string name = weapon.ConfigMelee.name;
			if (string.IsNullOrEmpty(name))
			{
				// stun rod
				if (weapon.BattleType == BT_MELEE && weapon.DamageType.ResistType == ItemDamageType.DT_STUN)
				{
					name = "STR_STUN";
				}
				else
				// melee weapon
				{
					name = "STR_HIT_MELEE";
				}
			}
			AddItem(BA_HIT, name, ref id, Options.keyBattleActionItem4);
		}

		// special items
		if (weapon.BattleType == BT_MEDIKIT)
		{
			AddItem(BA_USE, weapon.MedikitActionName, ref id, Options.keyBattleActionItem1);
		}
		else if (weapon.BattleType == BT_SCANNER)
		{
			AddItem(BA_USE, string.IsNullOrEmpty(weapon.PsiAttackName) ? "STR_USE_SCANNER" : weapon.PsiAttackName, ref id, Options.keyBattleActionItem1);
		}
		else if (weapon.BattleType == BT_PSIAMP)
		{
			if (weapon.CostMind.Time > 0)
			{
				AddItem(BA_MINDCONTROL, "STR_MIND_CONTROL", ref id, Options.keyBattleActionItem3);
			}
			if (weapon.CostPanic.Time > 0)
			{
				AddItem(BA_PANIC, "STR_PANIC_UNIT", ref id, Options.keyBattleActionItem2);
			}
			if (weapon.CostUse.Time > 0)
			{
				AddItem(BA_USE, weapon.PsiAttackName, ref id, Options.keyBattleActionItem1);
			}
		}
		else if (weapon.BattleType == BT_MINDPROBE)
		{
			AddItem(BA_USE, string.IsNullOrEmpty(weapon.PsiAttackName) ? "STR_USE_MIND_PROBE" : weapon.PsiAttackName, ref id, Options.keyBattleActionItem1);
		}
	}

	/// <summary>
	/// Init function.
	/// </summary>
	public override void Init()
	{
		if (!_actionMenu[0].Visible)
		{
			// Item don't have any actions, close popup.
			_game.PopState();
		}
	}

	/// <summary>
	/// Adds a new menu item for an action.
	/// </summary>
	/// <param name="ba">Action type.</param>
	/// <param name="name">Action description.</param>
	/// <param name="id">The new item ID.</param>
	/// <param name="key">Hotkey of the item.</param>
	protected void AddItem(BattleActionType ba, string name, ref int id, SDLKey key)
	{
		string s1 = "", s2;
		int acc = BattleUnit.GetFiringAccuracy(BattleActionAttack.GetBeforeShoot(ba, _action.actor, _action.weapon), _game.Mod);
		int tu = _action.actor.GetActionTUs(ba, _action.weapon).Time;

		if (ba == BA_THROW || ba == BA_AIMEDSHOT || ba == BA_SNAPSHOT || ba == BA_AUTOSHOT || ba == BA_LAUNCH || ba == BA_HIT)
			s1 = Tr("STR_ACCURACY_SHORT").Arg(Unicode.FormatPercentage(acc));
		s2 = Tr("STR_TIME_UNITS_SHORT").Arg(tu);
		_actionMenu[id].SetAction(ba, Tr(name), s1, s2, tu);
		_actionMenu[id].Visible = true;
		if (key != SDLKey.SDLK_UNKNOWN)
		{
			_actionMenu[id].OnKeyboardPress(BtnActionMenuItemClick, key);
		}
		id++;
	}

	/// <summary>
	/// Closes the window on right-click.
	/// </summary>
	public override void Handle(Action action)
	{
		base.Handle(action);
		if (action.Details.type == SDLEventType.SDL_MOUSEBUTTONDOWN && _game.IsRightClick(action))
		{
			_game.PopState();
		}
		else if (action.Details.type == SDLEventType.SDL_KEYDOWN)
		{
			SDLKey key = action.Details.key.keysym.sym;
			if (key == Options.keyCancel || key == Options.keyBattleUseLeftHand || key == Options.keyBattleUseRightHand)
			{
				if (key != Options.keyBattleActionItem1 &&
					key != Options.keyBattleActionItem2 &&
					key != Options.keyBattleActionItem3 &&
					key != Options.keyBattleActionItem4 &&
					key != Options.keyBattleActionItem5)
				{
					_game.PopState();
				}
			}
		}
	}

	/// <summary>
	/// Executes the action corresponding to this action menu item.
	/// </summary>
	public virtual void BtnActionMenuItemClick(Action action)
	{
		_game.SavedGame.SavedBattle.Pathfinding.RemovePreview();

		int btnID = -1;

		if (_game.SavedGame.SavedBattle.IsPreview)
		{
			_action.result = "STR_UNABLE_TO_USE_ALIEN_ARTIFACT_UNTIL_RESEARCHED";
			_game.PopState();
			return;
		}

		// got to find out which button was pressed
		for (int i = 0; i < _actionMenu.Length && btnID == -1; ++i)
		{
			if (action.Sender == _actionMenu[i])
			{
				btnID = i;
			}
		}

		if (btnID != -1)
		{
			_action.type = _actionMenu[btnID].Action;
			_action.skillRules = null;
			_action.UpdateTU();

			HandleAction();
		}
	}

	/// <summary>
	/// Acts on the action instance that has been chosen and set.
	/// </summary>
	protected void HandleAction()
	{
		// reset potential garbage from the previous action
		_action.terrainMeleeTilePart = 0;

		{
			RuleItem weapon = _action.weapon.Rules;
			bool newHitLog = false;
			string actionResult = "STR_UNKNOWN"; // needs a non-empty default/fall-back !

			if (_action.type != BA_THROW &&
				_action.actor.OriginalFaction == FACTION_PLAYER &&
				!_game.SavedGame.IsResearched(weapon.Requirements))
			{
				_action.result = "STR_UNABLE_TO_USE_ALIEN_ARTIFACT_UNTIL_RESEARCHED";
				_game.PopState();
			}
			else if (_action.type != BA_THROW &&
				!_game.SavedGame.SavedBattle.CanUseWeapon(_action.weapon, _action.actor, false, _action.type, ref actionResult))
			{
				_action.result = actionResult;
				_game.PopState();
			}
			else if (_action.type == BA_PRIME)
			{
				BattleFuseType fuseType = weapon.FuseTimerType;
				if (fuseType == BFT_SET)
				{
					_game.PushState(new PrimeGrenadeState(_action, false, null));
				}
				else
				{
					_action.value = weapon.FuseTimerDefault;
					_game.PopState();
				}
			}
			else if (_action.type == BA_UNPRIME)
			{
				_game.PopState();
			}
			else if (_action.type == BA_USE && weapon.BattleType == BT_MEDIKIT)
			{
				BattleUnit targetUnit = null;
				TileEngine tileEngine = _game.SavedGame.SavedBattle.TileEngine;
				foreach (BattleUnit bu in _game.SavedGame.SavedBattle.Units)
				{
					// we can heal a unit that is at the same position, unconscious and healable(=woundable)
					if (bu.Position == _action.actor.Position &&
						bu != _action.actor &&
						bu.Status == UnitStatus.STATUS_UNCONSCIOUS &&
						(bu.IsWoundable || weapon.AllowTargetImmune) &&
						weapon.AllowTargetGround)
					{
						if (bu.IsBigUnit)
						{
							// never EVER apply anything to 2x2 units on the ground
							continue;
						}
						if ((weapon.AllowTargetFriendGround && bu.OriginalFaction == FACTION_PLAYER) ||
							(weapon.AllowTargetNeutralGround && bu.OriginalFaction == FACTION_NEUTRAL) ||
							(weapon.AllowTargetHostileGround && bu.OriginalFaction == FACTION_HOSTILE))
						{
							targetUnit = bu;
							break; // loop finished
						}
					}
				}
				if (targetUnit == null && weapon.AllowTargetStanding)
				{
					if (tileEngine.ValidMeleeRange(
						_action.actor.Position,
						_action.actor.Direction,
						_action.actor,
						null, ref _action.target, false))
					{
						Tile tile = _game.SavedGame.SavedBattle.GetTile(_action.target);
						if (tile != null && tile.Unit != null && (tile.Unit.IsWoundable || weapon.AllowTargetImmune))
						{
							if ((weapon.AllowTargetFriendStanding && tile.Unit.OriginalFaction == FACTION_PLAYER) ||
								(weapon.AllowTargetNeutralStanding && tile.Unit.OriginalFaction == FACTION_NEUTRAL) ||
								(weapon.AllowTargetHostileStanding && tile.Unit.OriginalFaction == FACTION_HOSTILE))
							{
								targetUnit = tile.Unit;
							}
						}
					}
				}
				if (targetUnit == null && weapon.AllowTargetSelf)
				{
					targetUnit = _action.actor;
				}
				if (targetUnit != null)
				{
					_game.PopState();
					BattleMediKitType type = weapon.MediKitType;
					if (type != BMT_NORMAL)
					{
						if ((type == BMT_HEAL && _action.weapon.HealQuantity > 0) ||
							(type == BMT_STIMULANT && _action.weapon.StimulantQuantity > 0) ||
							(type == BMT_PAINKILLER && _action.weapon.PainKillerQuantity > 0))
						{
							if (_action.SpendTU(ref _action.result))
							{
								switch (type)
								{
									case BMT_HEAL:
										if (targetUnit.FatalWounds != 0)
										{
											for (int i = 0; i < (int)BODYPART_MAX; ++i)
											{
												if (targetUnit.GetFatalWound((UnitBodyPart)i) != 0)
												{
													tileEngine.MedikitUse(_action, targetUnit, BMA_HEAL, (UnitBodyPart)i);
													tileEngine.MedikitRemoveIfEmpty(_action);
													break;
												}
											}
										}
										else
										{
											tileEngine.MedikitUse(_action, targetUnit, BMA_HEAL, BODYPART_TORSO);
											tileEngine.MedikitRemoveIfEmpty(_action);
										}
										break;
									case BMT_STIMULANT:
										tileEngine.MedikitUse(_action, targetUnit, BMA_STIMULANT, BODYPART_TORSO);
										tileEngine.MedikitRemoveIfEmpty(_action);
										break;
									case BMT_PAINKILLER:
										tileEngine.MedikitUse(_action, targetUnit, BMA_PAINKILLER, BODYPART_TORSO);
										tileEngine.MedikitRemoveIfEmpty(_action);
										break;
									case BMT_NORMAL:
										break;
								}
							}
						}
						else
						{
							_action.result = "STR_NO_USES_LEFT";
						}
					}
					else
					{
						_game.PushState(new MedikitState(targetUnit, _action, tileEngine));
					}
				}
				else
				{
					_action.result = "STR_THERE_IS_NO_ONE_THERE";
					_game.PopState();
				}
			}
			else if (_action.type == BA_USE && weapon.BattleType == BT_SCANNER)
			{
				// spend TUs first, then show the scanner
				if (_action.SpendTU(ref _action.result))
				{
					_game.PopState();
					_game.PushState(new ScannerState(_action));
				}
				else
				{
					_game.PopState();
				}
			}
			else if (_action.type == BA_LAUNCH)
			{
				// check beforehand if we have enough time units
				if (!_action.HaveTU(ref _action.result))
				{
					//nothing
				}
				else if (_action.weapon.GetAmmoForAction(BA_LAUNCH, ref _action.result) == null)
				{
					//nothing
				}
				else
				{
					_action.targeting = true;
					newHitLog = true;
				}
				_game.PopState();
			}
			else if (_action.type == BA_HIT)
			{
				// check beforehand if we have enough time units
				if (!_action.HaveTU(ref _action.result))
				{
					//nothing
				}
				else if (!_game.SavedGame.SavedBattle.TileEngine.ValidMeleeRange(
					_action.actor.Position,
					_action.actor.Direction,
					_action.actor,
					null, ref _action.target))
				{
					if (!_game.SavedGame.SavedBattle.TileEngine.ValidTerrainMeleeRange(_action))
					{
						_action.result = "STR_THERE_IS_NO_ONE_THERE";
					}
				}
				else
				{
					newHitLog = true;
				}
				_game.PopState();
			}
			else
			{
				_action.targeting = true;
				newHitLog = true;
				_game.PopState();
			}

			// meleeAttackBState won't be available to clear the action type, do it here instead.
			if (_action.type == BA_HIT && !string.IsNullOrEmpty(_action.result))
			{
				_action.type = BA_NONE;
			}

			if (newHitLog)
			{
				_game.SavedGame.SavedBattle.AppendToHitLog(HitLogEntryType.HITLOG_PLAYER_FIRING, FACTION_PLAYER, Tr(weapon.Type));
			}
		}
	}

	/// <summary>
	/// Updates the scale.
	/// </summary>
	public override void Resize(ref int dX, ref int dY)
	{
		base.Recenter(dX, dY * 2);
	}
}
