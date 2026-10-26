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
// Ported from: src/Battlescape/BattlescapeState.cpp (soldier info, hand items, indicators)
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
	private static readonly int[] s_pulsate = [0, 1, 2, 3, 4, 3, 2, 1];

	/// <summary>
	/// Draw hand item with ammo number.
	/// </summary>
	private void DrawItem(BattleItem item, Surface hand, List<NumberText> ammoText, List<NumberText> medikitText, NumberText twoHandedText, bool drawReactionIndicator, bool drawNoReactionIndicator)
	{
		hand.Clear();
		for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
		{
			ammoText[slot].Visible = false;
		}
		for (int slot = 0; slot < RuleItem.MedikitSlots; ++slot)
		{
			medikitText[slot].Visible = false;
		}
		twoHandedText.Visible = false;
		if (item != null)
		{
			RuleItem rule = item.Rules;
			rule.DrawHandSprite(_game.Mod.GetSurfaceSet("BIGOBS.PCK"), hand, item, _save, _save.AnimFrame);
			for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
			{
				if (item.IsAmmoVisibleForSlot(slot))
				{
					BattleItem ammo = item.GetAmmoForSlot(slot);
					if (ammo == null)
					{
						ammoText[slot].Visible = true;
						ammoText[slot].Value = 0;
					}
					else
					{
						ammoText[slot].Visible = true;
						ammoText[slot].Value = (uint)ammo.AmmoQuantity;
					}
				}
			}
			twoHandedText.Visible = rule.IsTwoHanded;
			twoHandedText.SetColor(rule.IsBlockingBothHands ? _twoHandedRed : _twoHandedGreen);
			if (rule.BattleType == BT_MEDIKIT)
			{
				medikitText[0].Visible = true;
				medikitText[0].Value = (uint)item.PainKillerQuantity;
				medikitText[1].Visible = true;
				medikitText[1].Value = (uint)item.StimulantQuantity;
				medikitText[2].Visible = true;
				medikitText[2].Value = (uint)item.HealQuantity;
			}

			// primed grenade indicator (animated)
			if (item.FuseTimer >= 0)
			{
				Surface tempSurface = _game.Mod.GetSurfaceSet("SCANG.DAT").GetFrame(6);
				int x = (RuleInventory.HAND_W - rule.InventoryWidth) * RuleInventory.SLOT_W / 2;
				int y = (RuleInventory.HAND_H - rule.InventoryHeight) * RuleInventory.SLOT_H / 2;
				tempSurface.BlitNShade(hand, x, y, s_pulsate[_save.AnimFrame % 8], false, item.IsFuseEnabled ? 0 : 32);
			}
		}
		if (drawReactionIndicator)
		{
			Surface reactionIndicator = _game.Mod.GetSurface("reactionIndicator", false);
			if (reactionIndicator != null)
			{
				reactionIndicator.BlitNShade(hand, 0, 0);
			}
			else
			{
				Surface tempSurface = _game.Mod.GetSurfaceSet("SCANG.DAT").GetFrame(0);
				tempSurface.BlitNShade(hand, 28, 0);
			}
		}
		if (drawNoReactionIndicator)
		{
			Surface noReactionIndicator = _game.Mod.GetSurface("noReactionIndicator", false);
			if (noReactionIndicator != null)
			{
				noReactionIndicator.BlitNShade(hand, 0, 0);
			}
			else
			{
				Surface tempSurface = _game.Mod.GetSurfaceSet("SCANG.DAT").GetFrame(6); // red dot
				tempSurface.BlitNShade(hand, 28, 0);
			}
		}
	}

	/// <summary>
	/// Draw both hands sprites.
	/// </summary>
	private void DrawHandsItems()
	{
		BattleUnit battleUnit = _battleGame.PlayableUnitSelected() ? _save.SelectedUnit : null;
		bool left = false;
		bool right = false;
		bool left2 = false;
		bool right2 = false;
		BattleItem leftHandItem = null;
		BattleItem rightHandItem = null;
		if (battleUnit != null)
		{
			left = battleUnit.IsLeftHandPreferredForReactions;
			right = battleUnit.IsRightHandPreferredForReactions;
			left2 = battleUnit.IsLeftHandDisabledForReactions;
			right2 = battleUnit.IsRightHandDisabledForReactions;
			leftHandItem = battleUnit.LeftHandWeapon;
			rightHandItem = battleUnit.RightHandWeapon;
			if (leftHandItem == null || rightHandItem == null)
			{
				// even if both hands are empty, draw the special item just in one hand
				BattleItem emptyHandItem = null;
				foreach (BattleType type in s_emptyHandTypes)
				{
					emptyHandItem = battleUnit.GetSpecialWeapon(type);
					if (emptyHandItem != null && emptyHandItem.Rules.ShowSpecialInEmptyHand() && emptyHandItem.Rules.IsSpecialUsingEmptyHand)
					{
						break;
					}
					emptyHandItem = null;
				}
				if (leftHandItem != null)
					rightHandItem = emptyHandItem;
				else
					leftHandItem = emptyHandItem;
			}
		}
		DrawItem(leftHandItem, _btnLeftHandItem, _numAmmoLeft, _numMedikitLeft, _numTwoHandedIndicatorLeft, left, left2);
		DrawItem(rightHandItem, _btnRightHandItem, _numAmmoRight, _numMedikitRight, _numTwoHandedIndicatorRight, right, right2);
	}

	/// <summary>Shows an indicator button for the unit (helper for the repeated C++ block).</summary>
	private void ShowVisibleUnit(ref int j, BattleUnit bu, string tooltip)
	{
		_btnVisibleUnit[j].Tooltip = tooltip;
		_btnVisibleUnit[j].Visible = true;
		_numVisibleUnit[j].Visible = true;
		_visibleUnit[j] = bu;
		++j;
	}

	/// <summary>
	/// Updates a soldier's name/rank/tu/energy/health/morale.
	/// </summary>
	public void UpdateSoldierInfo(bool checkFOV = true)
	{
		BattleUnit battleUnit = _save.SelectedUnit;

		for (int i = 0; i < VISIBLE_MAX; ++i)
		{
			_btnVisibleUnit[i].Visible = false;
			_numVisibleUnit[i].Visible = false;
			_visibleUnit[i] = null;
		}

		bool playableUnit = _battleGame.PlayableUnitSelected();
		_rank.Visible = playableUnit;
		_rankTiny.Visible = playableUnit;
		_numTimeUnits.Visible = playableUnit;
		_barTimeUnits.Visible = playableUnit;
		_numEnergy.Visible = playableUnit;
		_barEnergy.Visible = playableUnit;
		_numHealth.Visible = playableUnit;
		_barHealth.Visible = playableUnit;
		_numMorale.Visible = playableUnit;
		_barMorale.Visible = playableUnit;
		if (_manaBarVisible)
		{
			_barMana.Visible = playableUnit;
		}
		_btnLeftHandItem.Visible = playableUnit;
		_btnRightHandItem.Visible = playableUnit;

		DrawHandsItems();

		if (!playableUnit)
		{
			_txtName.SetText("");
			ResetUiButton();
			ToggleKneelButton(null);
			return;
		}

		_txtName.SetText(battleUnit.GetName(_game.Language, false));
		Soldier soldier = battleUnit.GeoscapeSoldier;
		if (soldier != null)
		{
			if (soldier.HasCallsign && !_save.IsNameDisplay)
			{
				_txtName.SetText(soldier.GetCallsign());
			}
			// presence of custom background determines what should happen
			Surface customBg = _game.Mod.GetSurface("AvatarBackground", false);
			if (customBg == null)
			{
				// show rank (vanilla behaviour)
				SurfaceSet texture = _game.Mod.GetSurfaceSet("SMOKE.PCK");
				Surface frame = texture.GetFrame(soldier.RankSpriteBattlescape);
				frame?.BlitNShade(_rank, 0, 0);
			}
			else
			{
				// show tiny rank (modded)
				SurfaceSet texture = _game.Mod.GetSurfaceSet("TinyRanks");
				Surface spr = texture.GetFrame(soldier.RankSpriteTiny);
				spr?.BlitNShade(_rankTiny, 0, 0);

				// use custom background (modded)
				customBg.BlitNShade(_rank, 0, 0);

				// show avatar
				Armor customArmor = soldier.Armor;
				if (!string.IsNullOrEmpty(soldier.Rules.ArmorForAvatar))
				{
					customArmor = _game.Mod.GetArmor(soldier.Rules.ArmorForAvatar);
				}
				if (customArmor.HasLayersDefinition)
				{
					foreach (string layer in soldier.GetArmorLayers(customArmor))
					{
						Surface surf = _game.Mod.GetSurface(layer, true);
						BlitAvatar(surf, soldier);
					}
				}
				else
				{
					string look = soldier.Armor.SpriteInventory;
					if (!string.IsNullOrEmpty(soldier.Rules.ArmorForAvatar))
					{
						look = _game.Mod.GetArmor(soldier.Rules.ArmorForAvatar).SpriteInventory;
					}
					string gender = soldier.Gender == SoldierGender.GENDER_MALE ? "M" : "F";
					Surface surf = null;

					for (int i = 0; i <= RuleSoldier.LookVariantBits; ++i)
					{
						int lookIndex = (int)soldier.Look + (soldier.LookVariant & (RuleSoldier.LookVariantMask >> i)) * 4;
						surf = _game.Mod.GetSurface(look + gender + lookIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".SPK", false);
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

					// crop
					BlitAvatar(surf, soldier);
				}
			}
		}
		else
		{
			_rank.Clear();
			_rankTiny.Clear();
		}
		_numTimeUnits.Value = (uint)battleUnit.TimeUnits;
		_barTimeUnits.Max = battleUnit.BaseStats.tu;
		_barTimeUnits.Value = battleUnit.TimeUnits;
		_numEnergy.Value = (uint)battleUnit.Energy;
		_barEnergy.Max = battleUnit.BaseStats.stamina;
		_barEnergy.Value = battleUnit.Energy;
		_numHealth.Value = (uint)battleUnit.Health;
		_barHealth.Max = battleUnit.BaseStats.health;
		_barHealth.Value = battleUnit.Health;
		_barHealth.Value2 = battleUnit.Stunlevel;
		_numMorale.Value = (uint)battleUnit.Morale;
		_barMorale.Max = 100;
		_barMorale.Value = battleUnit.Morale;
		if (_manaBarVisible)
		{
			_barMana.Max = battleUnit.BaseStats.mana;
			_barMana.Value = battleUnit.Mana;
		}

		ToggleKneelButton(battleUnit);

		if (checkFOV)
		{
			_save.TileEngine.CalculateFOV(_save.SelectedUnit);
		}

		// go through all units visible to the selected soldier (or other unit, e.g. mind-controlled enemy)
		int j = 0;
		foreach (BattleUnit bu in battleUnit.VisibleUnits)
		{
			if (j >= VISIBLE_MAX) break; // loop finished
			ShowVisibleUnit(ref j, bu, _txtVisibleUnitTooltip[j]);
		}

		// remember where red indicators turn green
		_numberOfDirectlyVisibleUnits = j;

		// go through all units on the map
		foreach (BattleUnit bu in _save.Units)
		{
			if (j >= VISIBLE_MAX) break; // loop finished
			// check if they are hostile and visible (by any friendly unit)
			if (bu.OriginalFaction == FACTION_HOSTILE && !bu.IsOut && bu.Visible)
			{
				bool alreadyShown = false;
				// check if they are not already shown (e.g. because we see them directly)
				foreach (BattleUnit bu2 in battleUnit.VisibleUnits)
				{
					if (bu.Id == bu2.Id)
					{
						alreadyShown = true;
					}
				}
				if (!alreadyShown)
				{
					ShowVisibleUnit(ref j, bu, _txtVisibleUnitTooltip[j]);
				}
			}
		}

		// remember where green indicators turn blue
		_numberOfEnemiesTotal = j;

		{
			// go through all wounded units under player's control (incl. unconscious)
			foreach (BattleUnit bu in _save.Units)
			{
				if (j >= VISIBLE_MAX) break; // loop finished
				if (bu.Faction == FACTION_PLAYER && bu.Status != STATUS_DEAD && !bu.IsIgnored && bu.IndicatorsAreEnabled())
				{
					if (bu.FatalWounds > 0 || (Options.oxceShowBurningAsWounded && bu.Fire > 0))
					{
						ShowVisibleUnit(ref j, bu, _txtVisibleUnitTooltip[VISIBLE_MAX]);
					}
				}
			}
		}

		// remember where blue indicators turn purple
		_numberOfEnemiesTotalPlusWounded = j;

		{
			// first show all stunned allies with negative health regen (usually caused by high stun level)
			foreach (BattleUnit bu in _save.Units)
			{
				if (j >= VISIBLE_MAX) break; // loop finished
				if (bu.OriginalFaction == FACTION_PLAYER && bu.Status == STATUS_UNCONSCIOUS && bu.HasNegativeHealthRegen && bu.IndicatorsAreEnabled())
				{
					ShowVisibleUnit(ref j, bu, _txtVisibleUnitTooltip[VISIBLE_MAX + 1]);
				}
			}

			// then show all standing units under player's control with high stun level
			foreach (BattleUnit bu in _save.Units)
			{
				if (j >= VISIBLE_MAX) break; // loop finished
				if (bu.Faction == FACTION_PLAYER && !(bu.IsOut) && bu.Health > 0 && bu.IndicatorsAreEnabled())
				{
					if (bu.Stunlevel * 100 / bu.Health >= 75)
					{
						ShowVisibleUnit(ref j, bu, _txtVisibleUnitTooltip[VISIBLE_MAX + 1]);
					}
				}
			}
		}

		UpdateUiButton(battleUnit);
	}

	/// <summary>Blits the 26x23 avatar part of <paramref name="surf"/> onto the rank surface.</summary>
	private void BlitAvatar(Surface surf, Soldier soldier)
	{
		SurfaceCrop crop = surf.GetCrop();
		crop.Crop.x = (short)soldier.Rules.AvatarOffsetX;
		crop.Crop.y = (short)soldier.Rules.AvatarOffsetY;
		crop.Crop.w = 26;
		crop.Crop.h = 23;

		crop.Blit(_rank);
	}

	/// <summary>Updates the special action buttons (special weapon, skills, psi) for the unit.</summary>
	public void UpdateUiButton(BattleUnit battleUnit)
	{
		ArgumentNullException.ThrowIfNull(battleUnit);
		BattleItem psiWeapon = battleUnit.GetSpecialWeapon(BT_PSIAMP);

		BattleType type = BT_NONE;
		BattleItem specialWeapon = battleUnit.GetSpecialIconWeapon(ref type); // updates type!
		bool hasSpecialWeapon = specialWeapon != null && type != BT_NONE && type != BT_AMMO && type != BT_GRENADE && type != BT_PROXIMITYGRENADE && type != BT_FLARE && type != BT_CORPSE;

		// if we have psi amp with icon then it will show one button only, but if we have two psi amps and one with icon is second (this is important) then we will show both buttons.
		bool hasPsiWeapon = psiWeapon != null && psiWeapon != specialWeapon;

		bool hasSkills = battleUnit.GeoscapeSoldier != null && battleUnit.SkillMenuCheck();

		ResetUiButton();

		int offset = 0;
		void Show(BattlescapeButton btn, int spriteIndex)
		{
			if (offset < SPECIAL_BUTTONS_MAX)
			{
				_game.Mod.GetSurfaceSet("SPICONS.DAT").GetFrame(spriteIndex).BlitNShade(btn, 0, 0);
				btn.Visible = true;
				btn.X = _posSpecialActions[offset];
				++offset;
			}
		}

		if (hasSpecialWeapon)
		{
			Show(_btnSpecial, specialWeapon.Rules.SpecialIconSprite);
		}
		if (hasSkills)
		{
			Show(_btnSkills, battleUnit.GeoscapeSoldier.Rules.SkillIconSprite);
		}
		if (hasPsiWeapon)
		{
			bool canUsePsiWeapon = (psiWeapon.Rules.CostPanic.Time > 0) || (psiWeapon.Rules.CostUse.Time > 0);
			if (canUsePsiWeapon)
			{
				Show(_btnPsi, 1);
			}
		}
	}

	/// <summary>Hides the special action buttons.</summary>
	public void ResetUiButton()
	{
		BattlescapeButton[] btns = [
			_btnPsi,
			_btnSkills,
			_btnSpecial,
		];

		foreach (BattlescapeButton btn in btns)
		{
			btn.Visible = false;
			btn.X = _posSpecialActions[0];
		}
	}

	/// <summary>
	/// Shifts the red colors of the visible unit buttons backgrounds.
	/// </summary>
	private void BlinkVisibleUnitButtons()
	{
		for (int i = 0; i < VISIBLE_MAX; ++i)
		{
			if (_btnVisibleUnit[i].Visible == true)
			{
				_btnVisibleUnit[i].DrawRect(0, 0, 15, 12, 15);
				int bgColor = i < _numberOfDirectlyVisibleUnits ? s_blinkColor : i < _numberOfEnemiesTotal ? _indicatorGreen : i < _numberOfEnemiesTotalPlusWounded ? _indicatorBlue : _indicatorPurple;
				_btnVisibleUnit[i].DrawRect(1, 1, 13, 10, (byte)bgColor);
			}
		}

		if (s_blinkColor == 44) s_blinkDelta = -2;
		if (s_blinkColor == 32) s_blinkDelta = 1;

		s_blinkColor += s_blinkDelta;
	}

	/// <summary>
	/// Shifts the colors of the health bar when unit has fatal wounds.
	/// </summary>
	private void BlinkHealthBar()
	{
		s_healthStep = (byte)(1 - s_healthStep); // 1, 0, 1, 0, ...
		BattleUnit bu = _save.SelectedUnit;
		if (s_healthStep == 0 || bu == null || !_barHealth.Visible) return;

		if (++s_healthColor > HEALTH_MAX_COLOR) s_healthColor = HEALTH_MAX_COLOR - 3;

		for (int i = 0; i < (int)BODYPART_MAX; i++)
		{
			if (bu.GetFatalWound((UnitBodyPart)i) > 0)
			{
				_barHealth.SetColor((byte)(_barHealthColor + s_healthColor));
				return;
			}
		}
		if (_barHealth.Color != _barHealthColor) // avoid redrawing if we don't have to
			_barHealth.SetColor(_barHealthColor);
	}

	/// <summary>
	/// Animates map objects on the map, also smoke,fire, ...
	/// </summary>
	public void Animate()
	{
		_map.Animate(!_battleGame.IsBusy);

		BlinkVisibleUnitButtons();
		BlinkHealthBar();

		if (_map.Projectile == null)
		{
			DrawHandsItems();
		}
	}

	/// <summary>
	/// Handles the battle game state.
	/// </summary>
	public void HandleState()
	{
		_battleGame.HandleState();
	}

	/// <summary>
	/// Sets the timer interval for think() calls of the state.
	/// </summary>
	/// <param name="interval">An interval in ms.</param>
	public void SetStateInterval(uint interval)
	{
		_gameTimer.SetInterval(interval);
	}

	/// <summary>Gets pointer to the game. Some states need this info.</summary>
	public Game Game => _game;

	/// <summary>Gets pointer to the map. Some states need this info.</summary>
	public Map Map => _map;

	/// <summary>
	/// Shows a debug message in the topleft corner.
	/// </summary>
	public void Debug(string message)
	{
		if (_save.DebugMode)
		{
			_txtDebug.SetText(message);
		}
	}

	/// <summary>
	/// Shows a bug hunt message in the topleft corner.
	/// </summary>
	public void BugHuntMessage()
	{
		if (_save.BughuntMode)
		{
			_txtDebug.SetText(Tr("STR_BUG_HUNT_ACTIVATED"));
		}
	}

	/// <summary>Shows a warning message.</summary>
	public void Warning(string message)
	{
		_warning.ShowMessage(Tr(message));
	}

	/// <summary>Shows a warning message without automatic translation.</summary>
	public void WarningRaw(string message)
	{
		_warning.ShowMessage(message);
	}

	/// <summary>Shows a warning message without automatic translation (longer).</summary>
	public void WarningLongRaw(string message)
	{
		_warning.ShowMessage(message, 8);
	}

	/// <summary>
	/// Gets melee damage preview.
	/// </summary>
	/// <param name="actor">Selected unit.</param>
	/// <param name="weapon">Weapon to use for calculation.</param>
	public string GetMeleeDamagePreview(BattleUnit actor, BattleItem weapon)
	{
		if (weapon == null)
			return "";

		bool discovered = false;
		if (_game.SavedGame.MonthsPassed == -1)
		{
			discovered = true; // new battle mode
		}
		else
		{
			ArticleDefinition article = _game.Mod.GetUfopaediaArticle(weapon.Rules.Type, false);
			if (article != null && Ufopaedia.IsArticleAvailable(_game.SavedGame, article))
			{
				discovered = true; // pedia article unlocked
			}
		}

		var ss = new StringBuilder();
		if (discovered)
		{
			int totalDamage = 0;
			RuleDamageType dmgType;
			BattleActionAttack attack = BattleActionAttack.GetBeforeShoot(BattleActionType.BA_HIT, actor, weapon);
			if (weapon.Rules.BattleType == BT_MELEE)
			{
				totalDamage += weapon.Rules.GetPowerBonus(attack);
				dmgType = weapon.Rules.DamageType;
			}
			else
			{
				totalDamage += weapon.Rules.GetMeleeBonus(attack);
				dmgType = weapon.Rules.MeleeType;
			}

			ss.Append((string)Tr(weapon.Rules.Type));
			ss.Append('\n');
			ss.Append(dmgType.GetRandomDamage(totalDamage, 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
			ss.Append('-');
			ss.Append(dmgType.GetRandomDamage(totalDamage, 2).ToString(System.Globalization.CultureInfo.InvariantCulture));
			if (dmgType.RandomType == ItemDamageRandomType.DRT_UFO_WITH_TWO_DICE)
				ss.Append('*');
		}
		else
		{
			ss.Append((string)Tr(weapon.Rules.Type));
			ss.Append("\n?-?");
		}

		return ss.ToString();
	}

	/// <summary>
	/// Adds a new popup window to the queue
	/// (this prevents popups from overlapping).
	/// </summary>
	public void Popup(State state)
	{
		_popups.Add(state);
	}

	/// <summary>Shows the launch button.</summary>
	public void ShowLaunchButton(bool show)
	{
		_btnLaunch.Visible = show;
	}

	/// <summary>Clears mouse-scrolling state (isMouseScrolling).</summary>
	public void ClearMouseScrollingState()
	{
		_isMouseScrolling = false;
	}

	/// <summary>Returns a pointer to the battlegame, in case we need its functions.</summary>
	public BattlescapeGame BattleGame => _battleGame;

	/// <summary>Handler for the mouse moving over the icons, disabling the tile selection cube.</summary>
	public void MouseInIcons(Action action)
	{
		_mouseOverIcons = true;
	}

	/// <summary>Handler for the mouse going out of the icons, enabling the tile selection cube.</summary>
	public void MouseOutIcons(Action action)
	{
		_mouseOverIcons = false;
	}

	/// <summary>Checks if the mouse is over the icons.</summary>
	public bool MouseOverIcons => _mouseOverIcons;

	/// <summary>
	/// Determines whether the player is allowed to press buttons.
	/// Buttons are disabled in the middle of a shot, during the alien turn,
	/// and while a player's units are panicking.
	/// The save button is an exception as we want to still be able to save if something
	/// goes wrong during the alien turn, and submit the save file for dissection.
	/// </summary>
	/// <param name="allowSaving">True, if the help button was clicked.</param>
	public bool AllowButtons(bool allowSaving = false)
	{
		return ((allowSaving || _save.Side == FACTION_PLAYER || _save.DebugMode)
			&& (_battleGame.PanicHandled || _firstInit)
			&& (allowSaving || !_battleGame.IsBusy || _firstInit)
			&& (_map.Projectile == null));
	}

	/// <summary>Autosave the game the next time the battlescape is displayed.</summary>
	public void Autosave(int currentTurn)
	{
		_autosave = currentTurn;
	}

	/// <summary>Is busy?</summary>
	public bool IsBusy => (_map.CursorType == CursorType.CT_NONE || _battleGame.IsBusy);
}
