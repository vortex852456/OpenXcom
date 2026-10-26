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
// Ported from: src/Battlescape/SkillMenuState.h, src/Battlescape/SkillMenuState.cpp
#nullable disable

using static Acom.BattleActionType;

namespace Acom;

/// <summary>
/// Window that allows the player
/// to select a soldier skill.
/// </summary>
public class SkillMenuState : ActionMenuState
{
	/// <summary>Initializes all the elements in the Skill Menu window.</summary>
	/// <param name="action">Pointer to the action.</param>
	/// <param name="x">Position on the x-axis.</param>
	/// <param name="y">position on the y-axis.</param>
	public SkillMenuState(BattleAction action, int x, int y) : base(action)
	{
		ArgumentNullException.ThrowIfNull(action);
		// Attention: back up the current _action members
		BattleActionType currentActionType = _action.type;
		BattleItem currentWeapon = _action.weapon;
		RuleSkill currentSkill = _action.skillRules;

		_screen = false;

		// Set palette
		_game.SavedGame.SavedBattle.SetPaletteByDepth(this);

		for (int i = 0; i < _actionMenu.Length; ++i)
		{
			_actionMenu[i] = new ActionMenuItem(i, _game, x, y);
			Add(_actionMenu[i]);
			_actionMenu[i].Visible = false;
			_actionMenu[i].OnMouseClick(BtnActionMenuItemClick);
		}

		// Build up the popup menu
		int id = 0;

		List<SDLKey> hotkeys =
		[
			Options.keyBattleActionItem5,
			Options.keyBattleActionItem4,
			Options.keyBattleActionItem3,
			Options.keyBattleActionItem2,
			Options.keyBattleActionItem1,
		];
		Soldier soldier = _action.actor.GeoscapeSoldier;
		foreach (RuleSkill skill in soldier.Rules.Skills)
		{
			if (hotkeys.Count != 0
				&& soldier.HasAllRequiredBonusesForSkill(skill)
				&& (skill.Cost.Time > 0 || skill.Cost.Mana > 0)
				&& (!skill.IsPsiRequired || _action.actor.BaseStats.psiSkill > 0))
			{
				// Attention: we are modifying _action here
				_action.skillRules = skill;
				_action.type = skill.TargetMode;

				// Attention: we are modifying _action->weapon inside!
				ChooseWeaponForSkill(_action, skill);

				// Attention: here the modified values are consumed
				AddItem(skill, ref id, hotkeys[^1]);

				hotkeys.RemoveAt(hotkeys.Count - 1);
			}
		}

		// Attention: restore the original values
		_action.type = currentActionType;
		_action.weapon = currentWeapon;
		_action.skillRules = currentSkill;
	}

	/// <summary>Adds a new menu item for an action.</summary>
	/// <param name="skill">The skill.</param>
	/// <param name="id">The new item ID.</param>
	/// <param name="key">Hotkey of the item.</param>
	private void AddItem(RuleSkill skill, ref int id, SDLKey key)
	{
		BattleActionType ba = skill.TargetMode;

		string s1 = "", s2 = "";
		RuleItemUseCost cost = _action.actor.GetActionTUs(ba, _action.skillRules);

		if (_action.weapon != null)
		{
			int acc = BattleUnit.GetFiringAccuracy(BattleActionAttack.GetBeforeShoot(ba, _action.actor, _action.weapon, _action.skillRules), _game.Mod);
			if (ba == BA_THROW || ba == BA_AIMEDSHOT || ba == BA_SNAPSHOT || ba == BA_AUTOSHOT || ba == BA_LAUNCH || ba == BA_HIT)
				s1 = Tr("STR_ACCURACY_SHORT").Arg(Unicode.FormatPercentage(acc));
		}

		if (cost.Time > 0)
		{
			s2 = Tr("STR_TIME_UNITS_SHORT").Arg(cost.Time);
		}
		else if (cost.Mana > 0)
		{
			s2 = Tr("STR_MANA_SHORT").Arg(cost.Mana);
		}

		_actionMenu[id].SetAction(ba, Tr(skill.Type), s1, s2, cost.Time);
		_actionMenu[id].Skill = skill;
		_actionMenu[id].Visible = true;

		if (key != SDLKey.SDLK_UNKNOWN)
		{
			_actionMenu[id].OnKeyboardPress(BtnActionMenuItemClick, key);
		}
		id++;
	}

	/// <summary>Executes the action corresponding to this action menu item.</summary>
	public override void BtnActionMenuItemClick(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
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
				break;
			}
		}

		if (btnID != -1)
		{
			TileEngine tileEngine = _game.SavedGame.SavedBattle.TileEngine;
			RuleSkill selectedSkill = _actionMenu[btnID].Skill;
			_action.skillRules = selectedSkill;
			_action.type = _actionMenu[btnID].Action;
			ChooseWeaponForSkill(_action, selectedSkill);
			_action.UpdateTU();

			bool continueAction = tileEngine.SkillUse(_action, selectedSkill);

			if (!continueAction || _action.type == BA_NONE)
			{
				_action.targeting = false;
				_action.type = BA_NONE;
				_action.skillRules = null;
				_game.PopState();
				return;
			}

			// check if the skill needs an item, if so check if an item was found
			BattleItem item = _action.weapon;
			if (_action.type != BA_NONE)
			{
				if (item == null)
				{
					_action.result = "STR_SKILL_NEEDS_ITEM";
					if (_action.type == BA_HIT) _action.type = BA_NONE; // OXC merge
					_game.PopState();
					return;
				}
			}

			if (_action.type == BA_THROW && _action.weapon.Rules.BattleType == BattleType.BT_GRENADE)
			{
				// instant grenades
				_action.weapon.FuseTimer = 0;
				_action.weapon.SetFuseEnabled(true);
			}
			else if (_action.type == BA_PRIME || _action.type == BA_UNPRIME)
			{
				// nothing, skill menu only supports instant grenades for now
				_game.PopState();
				return;
			}

			// default handling from here on...
			HandleAction();
		}
	}

	/// <summary>Choose an action weapon based on given parameters.</summary>
	private static void ChooseWeaponForSkill(BattleAction action, RuleSkill skillRules)
	{
		BattleUnit unit = action.actor;
		action.weapon = null;

		if (action.type == BA_NONE)
		{
			return;
		}

		// 1. choose by weapon's name
		if (skillRules.CompatibleWeapons.Count != 0)
		{
			foreach (RuleItem itemRule in skillRules.CompatibleWeapons)
			{
				// check both hands, right first
				if (unit.RightHandWeapon != null && unit.RightHandWeapon.Rules == itemRule)
				{
					action.weapon = unit.RightHandWeapon;
					return;
				}
				else if (unit.LeftHandWeapon != null && unit.LeftHandWeapon.Rules == itemRule)
				{
					action.weapon = unit.LeftHandWeapon;
					return;
				}
				if (!skillRules.CheckHandsOnly())
				{
					// check special weapons
					BattleItem item = unit.GetSpecialWeapon(itemRule);
					if (item != null)
					{
						action.weapon = item;
						return;
					}
					// check inventory
					foreach (BattleItem invItem in unit.Inventory)
					{
						if (invItem.Rules == itemRule)
						{
							action.weapon = invItem;
							return;
						}
					}
				}
			}
		}

		// 2. if not found, try by weapon's battle type
		if (skillRules.CompatibleBattleType != BattleType.BT_NONE)
		{
			// check both hands, right first
			if (unit.RightHandWeapon != null && unit.RightHandWeapon.Rules.BattleType == skillRules.CompatibleBattleType)
			{
				action.weapon = unit.RightHandWeapon;
				return;
			}
			else if (unit.LeftHandWeapon != null && unit.LeftHandWeapon.Rules.BattleType == skillRules.CompatibleBattleType)
			{
				action.weapon = unit.LeftHandWeapon;
				return;
			}
			if (!skillRules.CheckHandsOnly2())
			{
				// check special weapons
				BattleItem item = unit.GetSpecialWeapon(skillRules.CompatibleBattleType);
				if (item != null)
				{
					action.weapon = item;
					return;
				}
				// check inventory
				foreach (BattleItem invItem in unit.Inventory)
				{
					if (invItem.Rules.BattleType == skillRules.CompatibleBattleType)
					{
						action.weapon = invItem;
						return;
					}
				}
			}
		}
	}
}
