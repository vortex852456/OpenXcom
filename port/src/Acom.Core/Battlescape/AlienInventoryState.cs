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
// Ported from: src/Battlescape/AlienInventoryState.h, src/Battlescape/AlienInventoryState.cpp
#nullable disable

using System.Globalization;
using System.Text;
using static Acom.BattleActionType;
using static Acom.BattleType;
using static Acom.UnitFaction;

namespace Acom;

/// <summary>
/// Screen which displays alien's inventory.
/// </summary>
public class AlienInventoryState : State
{
	private readonly Surface _bg, _soldier;
	private readonly BattlescapeButton _btnArmor;
	private readonly Text _txtName;
	private readonly Text _txtFatalWounds;
	private readonly Text _txtLeftHand, _txtRightHand;
	private readonly AlienInventory _inv;
	private bool _disposed;

	/// <summary>(dx, dy) of the 8 surrounding tiles, indexed by direction.</summary>
	private static readonly int[,] SurroundingTilePositions = {
		{0, -1}, // north (-y direction)
		{1, -1}, // northeast
		{1, 0}, // east (+ x direction)
		{1, 1}, // southeast
		{0, 1}, // south (+y direction)
		{-1, 1}, // southwest
		{-1, 0}, // west (-x direction)
		{-1, -1} }; // northwest

	/// <summary>Initializes all the elements in the AlienInventory screen.</summary>
	/// <param name="unit">Pointer to the alien unit.</param>
	public AlienInventoryState(BattleUnit unit)
	{
		ArgumentNullException.ThrowIfNull(unit);
		if (Options.maximizeInfoScreens)
		{
			Options.baseXResolution = Screen.ORIGINAL_WIDTH;
			Options.baseYResolution = Screen.ORIGINAL_HEIGHT;
			_game.Screen.ResetDisplay(false);
		}

		// Create objects
		_bg = new Surface(320, 200, 0, 0);
		int offsetX = _game.Mod.AlienInventoryOffsetX;
		_soldier = new Surface(320 - offsetX, 200, offsetX, 0);
		_txtName = new Text(308, 17, 6, 6);
		_txtFatalWounds = new Text(100, 48, 6, 32);
		_txtLeftHand = new Text(308, 17, 6, 160);
		_txtRightHand = new Text(308, 17, 6, 180);
		_btnArmor = new BattlescapeButton(40, 70, 140, 65);
		_inv = new AlienInventory(_game, 320, 200, 0, 0);

		// Set palette
		SetStandardPalette("PAL_BATTLESCAPE");

		Add(_bg);
		Add(_soldier);
		Add(_txtName, "textName", "inventory", _bg);
		Add(_txtFatalWounds, "textName", "inventory", _bg);
		Add(_txtLeftHand, "textName", "inventory", _bg);
		Add(_txtRightHand, "textName", "inventory", _bg);
		Add(_btnArmor, "buttonOK", "inventory", _bg);
		Add(_inv);

		CenterAllSurfaces();

		// Set up objects
		Surface tmp = _game.Mod.GetSurface("AlienInventory2", false);
		if (tmp == null || unit.GeoscapeSoldier == null)
		{
			tmp = _game.Mod.GetSurface("AlienInventory", false);
		}
		if (tmp != null)
		{
			tmp.BlitNShade(_bg, 0, 0);
		}

		_txtName.SetBig();
		_txtName.SetHighContrast(true);
		_txtName.Align = TextHAlign.ALIGN_CENTER;

		if (Options.oxceDisableAlienInventory)
		{
			_txtName.SetHeight(_txtName.Height * 9);
			_txtName.SetWordWrap(true);
			_txtName.SetText(Tr("STR_THIS_FEATURE_IS_DISABLED_5"));
			_soldier.Visible = false;
			_txtLeftHand.Visible = false;
			_txtRightHand.Visible = false;
			_btnArmor.OnKeyboardPress(BtnOkClick, Options.keyCancel);
			_inv.Visible = false;
			return;
		}

		if (unit.OriginalFaction == FACTION_NEUTRAL)
		{
			_txtName.SetText(Tr(unit.Type));
		}
		else
		{
			if (unit.UnitRules != null)
			{
				if (unit.UnitRules.GetShowFullNameInAlienInventory(_game.Mod))
				{
					// e.g. Sectoid Leader
					_txtName.SetText(unit.GetName(_game.Language));
				}
				else
				{
					// e.g. Sectoid
					_txtName.SetText(Tr(unit.UnitRules.Race));
				}
			}
			else
			{
				// Soldier names
				_txtName.SetText(unit.GetName(_game.Language));
			}
		}

		// Click on name to open Tech Tree Viewer for this unit type
		_txtName.Tooltip = unit.Type;
		_txtName.OnMouseClick(TxtNameClick);
		_txtFatalWounds.SetHighContrast(true);

		_txtLeftHand.SetBig();
		_txtLeftHand.SetHighContrast(true);
		_txtLeftHand.Align = TextHAlign.ALIGN_CENTER;
		_txtLeftHand.Visible = false;

		_txtRightHand.SetBig();
		_txtRightHand.SetHighContrast(true);
		_txtRightHand.Align = TextHAlign.ALIGN_CENTER;
		_txtRightHand.Visible = false;

		_btnArmor.OnKeyboardPress(BtnToggleClick, SDLKey.SDLK_F1);
		_btnArmor.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnArmor.OnMouseClick(BtnArmorClickMiddle, SDLMouse.SDL_BUTTON_MIDDLE);

		_soldier.Clear();

		Soldier s = unit.GeoscapeSoldier;
		if (s != null)
		{
			if (s.Armor.HasLayersDefinition)
			{
				foreach (string layer in s.GetArmorLayers())
				{
					Surface surf = _game.Mod.GetSurface(layer, true);
					surf.BlitNShade(_soldier, 0, 0);
				}
			}
			else
			{
				string look = s.Armor.SpriteInventory;
				string gender = s.Gender == SoldierGender.GENDER_MALE ? "M" : "F";
				Surface surf = null;

				for (int i = 0; i <= RuleSoldier.LookVariantBits; ++i)
				{
					string name = look + gender + ((int)s.Look + (s.LookVariant & (RuleSoldier.LookVariantMask >> i)) * 4).ToString(CultureInfo.InvariantCulture) + ".SPK";
					surf = _game.Mod.GetSurface(name, false);
					if (surf != null)
					{
						break;
					}
				}
				if (surf == null)
				{
					surf = _game.Mod.GetSurface(look + ".SPK", false);
				}
				if (surf == null)
				{
					surf = _game.Mod.GetSurface(look, true);
				}
				surf.BlitNShade(_soldier, 0, 0);
			}
		}
		else
		{
			Surface armorSurface = _game.Mod.GetSurface(unit.Armor.SpriteInventory, false);
			if (armorSurface == null)
			{
				armorSurface = _game.Mod.GetSurface(unit.Armor.SpriteInventory + ".SPK", false);
			}
			if (armorSurface == null)
			{
				armorSurface = _game.Mod.GetSurface(unit.Armor.SpriteInventory + "M0.SPK", false);
			}
			if (armorSurface != null)
			{
				armorSurface.BlitNShade(_soldier, 0, 0);
			}
		}

		_inv.SelectedUnit = unit;
		_inv.Draw();

		// Bleeding indicator
		tmp = _game.Mod.GetSurface("BigWoundIndicator", false);
		if (tmp != null && unit.FatalWounds > 0 && unit.IndicatorsAreEnabled())
		{
			tmp.BlitNShade(_soldier, 32, 32);

			Element element = _game.Mod.GetInterface("inventory").GetElementOptional("textName");
			if (element != null && element.custom == 0)
			{
				// fatally wounded body parts
				var ss = new StringBuilder();
				for (int i = 0; i < (int)UnitBodyPart.BODYPART_MAX; ++i)
				{
					if (unit.GetFatalWound((UnitBodyPart)i) != 0)
					{
						ss.Append((string)_game.Language.GetString(BattleUnit.PARTS_STRING[i]));
						ss.Append('\n');
					}
				}
				_txtFatalWounds.SetText(ss.ToString());
			}
		}

		// Burning indicator
		tmp = _game.Mod.GetSurface("BigBurnIndicator", false);
		if (tmp != null && unit.Fire > 0 && unit.IndicatorsAreEnabled())
		{
			tmp.BlitNShade(_soldier, 112, 32);
		}

		// --------------------- DEBUG INDICATORS ---------------------
		if (!Options.debug)
			return;

		BattleType[] typesToCheck = [BT_MELEE, BT_FIREARM];
		BattleItem weaponL = unit.LeftHandWeapon;
		if (weaponL == null)
		{
			foreach (BattleType type in typesToCheck)
			{
				weaponL = unit.GetSpecialWeapon(type);
				if (weaponL != null && weaponL.Rules.IsSpecialUsingEmptyHand)
				{
					break;
				}
				weaponL = null;
			}
		}
		if (weaponL != null)
		{
			if (weaponL.Rules.BattleType == BT_FIREARM)
			{
				CalculateRangedWeapon(unit, weaponL, _txtLeftHand);
			}
			else if (weaponL.Rules.BattleType == BT_MELEE)
			{
				CalculateMeleeWeapon(unit, weaponL, _txtLeftHand);
			}
		}

		BattleItem weaponR = unit.RightHandWeapon;
		if (weaponR == null)
		{
			foreach (BattleType type in typesToCheck)
			{
				weaponR = unit.GetSpecialWeapon(type);
				if (weaponR != null && weaponR.Rules.IsSpecialUsingEmptyHand)
				{
					break;
				}
				weaponR = null;
			}
		}
		if (weaponR != null)
		{
			if (weaponR.Rules.BattleType == BT_FIREARM)
			{
				CalculateRangedWeapon(unit, weaponR, _txtRightHand);
			}
			else if (weaponR.Rules.BattleType == BT_MELEE)
			{
				CalculateMeleeWeapon(unit, weaponR, _txtRightHand);
			}
		}
	}

	/// <summary>Cleans up the AlienInventory state (C++ destructor).</summary>
	public override void Destroy()
	{
		if (_disposed)
			return;
		_disposed = true;
		if (Options.maximizeInfoScreens)
		{
			Screen.UpdateScale(Options.battlescapeScale, ref Options.baseXBattlescape, ref Options.baseYBattlescape, true);
			_game.Screen.ResetDisplay(false);
		}
	}

	/// <summary>Applies the melee dodge of the victim to a hit chance.</summary>
	private static int ApplyMeleeDodge(TileEngine tileEngine, BattleUnit unit, BattleUnit victim, int hitChance)
	{
		if (victim != null)
		{
			int arc = tileEngine.GetArcDirection(tileEngine.GetDirectionTo(victim.PositionVexels, unit.PositionVexels), victim.Direction);
			float penalty = 1.0f - arc * victim.Armor.MeleeDodgeBackPenalty / 4.0f;
			if (penalty > 0)
			{
				// C++ int -= float: computed in float, truncated back to int
				hitChance = (int)(hitChance - victim.Armor.GetMeleeDodge(victim) * penalty);
			}
		}
		return hitChance;
	}

	/// <summary>Shows the debug melee hit chance against the faced unit.</summary>
	private void CalculateMeleeWeapon(BattleUnit unit, BattleItem weapon, Text label)
	{
		var ss = new StringBuilder();

		TileEngine tileEngine = _game.SavedGame.SavedBattle.TileEngine;

		// Start by finding the target for the check
		Position tileToCheck = unit.Position;
		int dir = unit.Direction;
		tileToCheck.x = (short)(tileToCheck.x + SurroundingTilePositions[dir, 0]);
		tileToCheck.y = (short)(tileToCheck.y + SurroundingTilePositions[dir, 1]);
		BattleUnit meleeDodgeTarget = null;
		if (_game.SavedGame.SavedBattle.GetTile(tileToCheck) != null) // Make sure the tile is in bounds
		{
			meleeDodgeTarget = _game.SavedGame.SavedBattle.SelectUnit(tileToCheck);
		}

		ss.Append(Tr(weapon.Rules.Type).ToString()).Append(" > ");
		if (meleeDodgeTarget != null)
		{
			BattleActionAttack attack = new BattleActionAttack();
			attack.type = BA_HIT;
			attack.attacker = unit;
			attack.weapon_item = weapon;
			attack.damage_item = weapon;
			attack.skill_rules = null;
			int hitChance = BattleUnit.GetFiringAccuracy(attack, _game.Mod);

			BattleUnit victim = meleeDodgeTarget;
			hitChance = ApplyMeleeDodge(tileEngine, unit, victim, hitChance);
			ss.Append("hitChance ").Append(hitChance.ToString(CultureInfo.InvariantCulture)).Append("% ");
		}
		else
		{
			ss.Append("not facing target ");
		}

		label.SetText(ss.ToString());
	}

	/// <summary>Shows the debug close quarters chances against the surrounding units.</summary>
	private void CalculateRangedWeapon(BattleUnit unit, BattleItem weapon, Text label)
	{
		var ss = new StringBuilder();

		TileEngine tileEngine = _game.SavedGame.SavedBattle.TileEngine;

		// Start by finding 'targets' for the check
		var closeQuartersTargetList = new List<BattleUnit>();

		for (int dir = 0; dir < 8; dir++)
		{
			Position tileToCheck = unit.Position;
			tileToCheck.x = (short)(tileToCheck.x + SurroundingTilePositions[dir, 0]);
			tileToCheck.y = (short)(tileToCheck.y + SurroundingTilePositions[dir, 1]);

			if (_game.SavedGame.SavedBattle.GetTile(tileToCheck) != null) // Make sure the tile is in bounds
			{
				BattleUnit closeQuartersTarget = _game.SavedGame.SavedBattle.SelectUnit(tileToCheck);
				// Variable for LOS check
				int checkDirection = tileEngine.GetDirectionTo(tileToCheck, unit.Position);
				if (closeQuartersTarget != null && unit.Faction != closeQuartersTarget.Faction // Unit must exist and not be same faction
					&& closeQuartersTarget.Armor.GetCreatesMeleeThreat() // Unit must be valid defender, 2x2 default false here
					&& closeQuartersTarget.TimeUnits >= _game.Mod.CloseQuartersTuCostGlobal // Unit must have enough TUs
					&& closeQuartersTarget.Energy >= _game.Mod.CloseQuartersEnergyCostGlobal // Unit must have enough Energy
					&& tileEngine.ValidMeleeRange(closeQuartersTarget, unit, checkDirection) // Unit must be able to see the unit attempting to fire
					&& !(unit.Faction == FACTION_PLAYER && closeQuartersTarget.Faction == FACTION_NEUTRAL) // Civilians don't inhibit player
					&& !(unit.Faction == FACTION_NEUTRAL && closeQuartersTarget.Faction == FACTION_PLAYER)) // Player doesn't inhibit civilians
				{
					closeQuartersTargetList.Add(closeQuartersTarget);
				}
			}
		}

		ss.Append(Tr(weapon.Rules.Type).ToString()).Append(" > ");
		if (closeQuartersTargetList.Count != 0)
		{
			foreach (BattleUnit victim in closeQuartersTargetList)
			{
				{
					BattleActionAttack attack = new BattleActionAttack();
					attack.type = BA_CQB;
					attack.attacker = unit;
					attack.weapon_item = weapon;
					attack.damage_item = weapon;
					attack.skill_rules = null;
					int hitChance = BattleUnit.GetFiringAccuracy(attack, _game.Mod);

					hitChance = ApplyMeleeDodge(tileEngine, unit, victim, hitChance);
					ss.Append("cqcChance ").Append(hitChance.ToString(CultureInfo.InvariantCulture)).Append("% ");
				}
			}
		}

		label.SetText(ss.ToString());
	}

#pragma warning disable CA1822 // instance handler in C++ (State._game is static here)
	/// <summary>Returns to the previous screen.</summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}
#pragma warning restore CA1822

	/// <summary>Toggles debug indicators.</summary>
	public void BtnToggleClick(Action action)
	{
		_txtLeftHand.Visible = !_txtLeftHand.Visible;
		_txtRightHand.Visible = !_txtRightHand.Visible;
	}

	/// <summary>Opens Ufopaedia entry for the corresponding armor.</summary>
	public void BtnArmorClickMiddle(Action action)
	{
		BattleUnit unit = _inv.SelectedUnit;
		if (unit != null)
		{
			string articleId = unit.Armor.UfopediaType;
			Ufopaedia.OpenArticle(_game, articleId);
		}
	}

	/// <summary>Opens the Tech Tree Viewer for this unit type.</summary>
	public void TxtNameClick(Action action)
	{
		RuleResearch selectedTopic = _game.Mod.GetResearch(_txtName.Tooltip);

		if (selectedTopic != null)
		{
			_game.PushState(new TechTreeViewerState(selectedTopic, null, null, null));
		}
	}
}
