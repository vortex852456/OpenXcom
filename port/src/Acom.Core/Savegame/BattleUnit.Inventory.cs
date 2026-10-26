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
// Ported from: src/Savegame/BattleUnit.h, src/Savegame/BattleUnit.cpp
#nullable disable

using static Acom.BattleActionType;
using static Acom.BattleType;
using static Acom.InventoryType;
using static Acom.UnitFaction;

namespace Acom;

// Inventory slots, hands, reloading and special (built-in) weapons.
public partial class BattleUnit
{
	/// <summary>Get the list of items in the inventory.</summary>
	public List<BattleItem> Inventory => _inventory;

	/// <summary>Fit item into inventory slot.</summary>
	/// <param name="slot">Slot to fit.</param>
	/// <param name="item">Item to fit.</param>
	/// <returns>True if succeeded, false otherwise.</returns>
	public bool FitItemToInventory(RuleInventory slot, BattleItem item)
	{
		var rule = item.Rules;
		if (rule.CanBePlacedIntoInventorySection(slot) == false)
		{
			return false;
		}
		if (slot.Type == INV_HAND)
		{
			if (!Acom.Inventory.OverlapItems(this, item, slot))
			{
				item.MoveToOwner(this);
				item.Slot = slot;
				return true;
			}
		}
		else if (slot.Type == INV_SLOT)
		{
			foreach (RuleSlot rs in slot.Slots)
			{
				if (!Acom.Inventory.OverlapItems(this, item, slot, rs.x, rs.y) && slot.FitItemInSlot(rule, rs.x, rs.y))
				{
					item.MoveToOwner(this);
					item.Slot = slot;
					item.SlotX = rs.x;
					item.SlotY = rs.y;
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>Adds an item to an XCom soldier (auto-equip).</summary>
	/// <param name="item">Pointer to the Item.</param>
	/// <param name="mod">Pointer to the Mod.</param>
	/// <param name="allowSecondClip">allow the unit to take a second clip or not. (only applies to xcom soldiers, aliens are allowed regardless of this flag)</param>
	/// <param name="allowAutoLoadout">allow auto equip of weapons for solders.</param>
	/// <param name="allowUnloadedWeapons">allow equip of weapons without ammo.</param>
	/// <returns>if the item was placed or not.</returns>
	public bool AddItem(BattleItem item, Mod mod, bool allowSecondClip = false, bool allowAutoLoadout = false, bool allowUnloadedWeapons = false)
	{
		RuleInventory rightHand = mod.InventoryRightHand;
		RuleInventory leftHand = mod.InventoryLeftHand;
		bool placed = false;
		bool loaded = false;
		RuleItem rule = item.Rules;
		int weight = 0;

		bool isStandardPlayerUnit = Faction == FACTION_PLAYER && HasInventory && !IsSummonedPlayerUnit;

		// tanks and aliens don't care about weight or multiple items,
		// their loadouts are defined in the rulesets and more or less set in stone.
		if (isStandardPlayerUnit)
		{
			weight = GetCarriedWeight() + item.TotalWeight;
			// allow all weapons to be loaded by avoiding this check,
			// they'll return false later anyway if the unit has something in his hand.
			if (rule.BattleType != BT_FIREARM && rule.BattleType != BT_MELEE)
			{
				int tally = 0;
				foreach (var bi in Inventory)
				{
					if (rule.Type == bi.Rules.Type)
					{
						if (allowSecondClip && rule.BattleType == BT_AMMO)
						{
							tally++;
							if (tally == 2)
							{
								return false;
							}
						}
						else
						{
							// we already have one, thanks.
							return false;
						}
					}
				}
			}
		}

		// place fixed weapon
		if (rule.IsFixed)
		{
			// either in the default slot provided in the ruleset
			if (rule.DefaultInventorySlot != null)
			{
				RuleInventory defaultSlot = rule.DefaultInventorySlot;
				BattleItem defaultSlotWeapon = GetItem(defaultSlot);
				if (defaultSlotWeapon == null)
				{
					item.MoveToOwner(this);
					item.Slot = defaultSlot;
					item.SlotX = rule.DefaultInventorySlotX;
					item.SlotY = rule.DefaultInventorySlotY;
					placed = true;
					item.XCOMProperty = Faction == FACTION_PLAYER && !IsSummonedPlayerUnit;
					if (item.Rules.TurretType > -1)
					{
						TurretType = item.Rules.TurretType;
					}
				}
			}
			// or in the left/right hand
			if (!placed && (FitItemToInventory(rightHand, item) || FitItemToInventory(leftHand, item)))
			{
				placed = true;
				item.XCOMProperty = Faction == FACTION_PLAYER && !IsSummonedPlayerUnit;
				if (item.Rules.TurretType > -1)
				{
					TurretType = item.Rules.TurretType;
				}
			}
			return placed;
		}

		// we equip item only if we have skill to use it.
		if (BaseStats.psiSkill <= 0 && rule.IsPsiRequired)
		{
			return false;
		}

		if (rule.IsManaRequired && OriginalFaction == FACTION_PLAYER)
		{
			// don't auto-equip items that require mana for now, maybe reconsider in the future
			return false;
		}

		bool keep;
		switch (rule.BattleType)
		{
			case BT_FIREARM:
			case BT_MELEE:
				if (item.HaveAnyAmmo() || Faction != FACTION_PLAYER || !HasInventory || allowUnloadedWeapons)
				{
					loaded = true;
				}

				if (loaded && (GeoscapeSoldier == null || allowAutoLoadout))
				{
					if (BaseStats.strength * 0.66 >= weight) // weight is always considered 0 for aliens
					{
						// C1 - vanilla right-hand main weapon (and OXCE left-hand second main weapon)
						if (FitItemToInventory(rightHand, item))
						{
							placed = true;
						}
						bool allowTwoMainWeapons = (Faction != FACTION_PLAYER) || _armor.AllowTwoMainWeapons;
						if (!placed && allowTwoMainWeapons && FitItemToInventory(leftHand, item))
						{
							placed = true;
						}
					}
				}
				break;
			case BT_AMMO:
				{
					BattleItem rightWeapon = RightHandWeapon;
					BattleItem leftWeapon = LeftHandWeapon;
					// xcom weapons will already be loaded, aliens and tanks, however, get their ammo added afterwards.
					// so let's try to load them here.
					if (rightWeapon != null && (rightWeapon.Rules.IsFixed || Faction != FACTION_PLAYER || allowUnloadedWeapons) &&
						rightWeapon.IsWeaponWithAmmo && rightWeapon.SetAmmoPreMission(item))
					{
						placed = true;
						break;
					}
					if (leftWeapon != null && (leftWeapon.Rules.IsFixed || Faction != FACTION_PLAYER || allowUnloadedWeapons) &&
						leftWeapon.IsWeaponWithAmmo && leftWeapon.SetAmmoPreMission(item))
					{
						placed = true;
						break;
					}
					// don't take ammo for weapons we don't have.
					keep = Faction != FACTION_PLAYER;
					if (rightWeapon != null)
					{
						if (rightWeapon.Rules.GetSlotForAmmo(rule) != -1)
						{
							keep = true;
						}
					}
					if (leftWeapon != null)
					{
						if (leftWeapon.Rules.GetSlotForAmmo(rule) != -1)
						{
							keep = true;
						}
					}
					if (!keep)
					{
						break;
					}
					goto default; // C++ FALLTHROUGH
				}
			default:
				if (rule.BattleType == BT_PSIAMP && Faction == FACTION_HOSTILE)
				{
					// C2 - vanilla left-hand psi-amp for hostiles
					if (FitItemToInventory(rightHand, item) || FitItemToInventory(leftHand, item))
					{
						placed = true;
					}
				}
				else if (GeoscapeSoldier == null || allowAutoLoadout)
				{
					if (BaseStats.strength >= weight) // weight is always considered 0 for aliens
					{
						// D1 - default slot by item
						if (!placed && isStandardPlayerUnit)
						{
							if (item.Rules.DefaultInventorySlot != null)
							{
								RuleInventory slot = item.Rules.DefaultInventorySlot;
								if (slot.Type != INV_GROUND)
								{
									placed = FitItemToInventory(slot, item);
									if (placed)
									{
										break; // leaves the switch, like C++
									}
								}
							}
						}
						// D2 - slot order by item category
						if (!placed && isStandardPlayerUnit)
						{
							var cat = item.Rules.GetFirstCategoryWithInvOrder(mod);
							if (cat != null)
							{
								foreach (var s in cat.InvOrder)
								{
									RuleInventory slot = mod.GetInventory(s);
									if (slot.Type != INV_GROUND)
									{
										placed = FitItemToInventory(slot, item);
										if (placed)
										{
											break;
										}
									}
								}
							}
						}
						// C3 - fallback: vanilla slot order by listOrder
						if (!placed)
						{
							// this is `n*(log(n) + log(n))` code, it could be `n` but we would lose predefined order, as `RuleItem` have them in effective in random order (depending on global memory allocations)
							foreach (var s in mod.InvsList)
							{
								RuleInventory slot = mod.GetInventory(s);
								if (slot.Type == INV_SLOT)
								{
									placed = FitItemToInventory(slot, item);
									if (placed)
									{
										break;
									}
								}
							}
						}
					}
				}
				break;
		}

		item.XCOMProperty = Faction == FACTION_PLAYER && !IsSummonedPlayerUnit;

		return placed;
	}

	/// <summary>Checks if there's an inventory item in the specified inventory position.</summary>
	/// <param name="slot">Inventory slot.</param>
	/// <param name="x">X position in slot.</param>
	/// <param name="y">Y position in slot.</param>
	/// <returns>Item in the slot, or NULL if none.</returns>
	public BattleItem GetItem(RuleInventory slot, int x = 0, int y = 0)
	{
		// Soldier items
		if (slot.Type != INV_GROUND)
		{
			foreach (var bi in _inventory)
			{
				if (bi.Slot == slot && bi.OccupiesSlot(x, y, null))
				{
					return bi;
				}
			}
		}
		// Ground items
		else if (_tile != null)
		{
			foreach (var bi in _tile.Inventory)
			{
				if (bi.OccupiesSlot(x, y, null))
				{
					return bi;
				}
			}
		}
		return null;
	}

	/// <summary>Get the "main hand weapon" from the unit.</summary>
	/// <param name="quickest">Whether to get the quickest weapon, default true</param>
	/// <param name="reactions">Ignore hands disabled for reactions (player units only).</param>
	/// <returns>Pointer to item.</returns>
	public BattleItem GetMainHandWeapon(bool quickest = true, bool reactions = false)
	{
		BattleItem weaponRightHand = RightHandWeapon;
		BattleItem weaponLeftHand = LeftHandWeapon;

		// ignore weapons without ammo (rules out grenades)
		if (weaponRightHand == null || !weaponRightHand.HaveAnyAmmo())
			weaponRightHand = null;
		if (weaponLeftHand == null || !weaponLeftHand.HaveAnyAmmo())
			weaponLeftHand = null;

		// ignore disabled hands/weapons (player units only... to prevent abuse)
		// Note: there is another check later, but this one is still needed, so that also non-main weapons get a chance to be used in case the main weapon is disabled
		if (reactions && _faction == FACTION_PLAYER)
		{
			if (_reactionsDisabledForRightHand)
				weaponRightHand = null;
			if (_reactionsDisabledForLeftHand)
				weaponLeftHand = null;
		}

		// if there is only one weapon, it's easy:
		if (weaponRightHand != null && weaponLeftHand == null)
			return weaponRightHand;
		else if (weaponRightHand == null && weaponLeftHand != null)
			return weaponLeftHand;
		else if (weaponRightHand == null && weaponLeftHand == null)
		{
			// Allow *AI* to use also a special weapon, but only when both hands are empty
			// Only need to check for firearms since melee/psi is handled elsewhere
			BattleItem specialWeapon = GetSpecialWeapon(BT_FIREARM);
			if (specialWeapon != null)
			{
				return specialWeapon;
			}

			return null;
		}

		// otherwise pick the one with the least snapshot TUs
		int tuRightHand = GetActionTUs(BA_SNAPSHOT, weaponRightHand).Time;
		int tuLeftHand = GetActionTUs(BA_SNAPSHOT, weaponLeftHand).Time;
		BattleItem weaponCurrentHand = GetActiveHand(weaponLeftHand, weaponRightHand);
		//prioritize blaster
		if (!quickest && _faction != FACTION_PLAYER)
		{
			if (weaponRightHand.CurrentWaypoints != 0)
			{
				return weaponRightHand;
			}
			if (weaponLeftHand.CurrentWaypoints != 0)
			{
				return weaponLeftHand;
			}
		}
		// if only one weapon has snapshot, pick that one
		if (tuLeftHand <= 0 && tuRightHand > 0)
			return weaponRightHand;
		else if (tuRightHand <= 0 && tuLeftHand > 0)
			return weaponLeftHand;
		// else pick the better one
		else
		{
			if (tuLeftHand >= tuRightHand)
			{
				if (quickest)
				{
					return weaponRightHand;
				}
				else if (_faction == FACTION_PLAYER)
				{
					return weaponCurrentHand;
				}
				else
				{
					return weaponLeftHand;
				}
			}
			else
			{
				if (quickest)
				{
					return weaponLeftHand;
				}
				else if (_faction == FACTION_PLAYER)
				{
					return weaponCurrentHand;
				}
				else
				{
					return weaponRightHand;
				}
			}
		}
	}

	/// <summary>Get a grenade from the belt (used for AI)</summary>
	public BattleItem GetGrenadeFromBelt(SavedBattleGame battle)
	{
		foreach (var bi in _inventory)
		{
			if (bi.Rules.IsGrenadeOrProxy)
			{
				if (battle.Turn >= bi.Rules.GetAIUseDelay(battle.Mod))
				{
					return bi;
				}
			}
		}
		return null;
	}

	/// <summary>Gets the item from right hand.</summary>
	public BattleItem RightHandWeapon
	{
		get
		{
			foreach (var bi in _inventory)
			{
				var slot = bi.Slot;
				if (slot != null && slot.IsRightHand)
				{
					return bi;
				}
			}
			return null;
		}
	}

	/// <summary>Gets the item from left hand.</summary>
	public BattleItem LeftHandWeapon
	{
		get
		{
			foreach (var bi in _inventory)
			{
				var slot = bi.Slot;
				if (slot != null && slot.IsLeftHand)
				{
					return bi;
				}
			}
			return null;
		}
	}

	/// <summary>Set the right hand as main active hand.</summary>
	public void SetActiveRightHand()
	{
		_activeHand = "STR_RIGHT_HAND";
	}

	/// <summary>Set the left hand as main active hand.</summary>
	public void SetActiveLeftHand()
	{
		_activeHand = "STR_LEFT_HAND";
	}

	/// <summary>Choose what weapon was last use by unit.</summary>
	public BattleItem GetActiveHand(BattleItem left, BattleItem right)
	{
		if (_activeHand == "STR_RIGHT_HAND" && right != null) return right;
		if (_activeHand == "STR_LEFT_HAND" && left != null) return left;
		return left ?? right;
	}

	/// <summary>Check if we have ammo and reload if needed (used for AI).</summary>
	/// <returns>Do we have ammo?</returns>
	public bool ReloadAmmo()
	{
		BattleItem[] list =
		[
			RightHandWeapon,
			LeftHandWeapon,
		];

		for (int i = 0; i < 2; ++i)
		{
			BattleItem weapon = list[i];
			if (weapon == null || !weapon.IsWeaponWithAmmo || weapon.HaveAllAmmo())
			{
				continue;
			}

			// we have a non-melee weapon with no ammo and 15 or more TUs - we might need to look for ammo then
			BattleItem ammo = null;
			var ruleWeapon = weapon.Rules;
			var tuCost = TimeUnits + 1;
			var slotAmmo = 0;

			foreach (var bi in Inventory)
			{
				int slot = ruleWeapon.GetSlotForAmmo(bi.Rules);
				if (slot != -1 && weapon.GetAmmoForSlot(slot) == null)
				{
					int tuTemp = (Mod.EXTENDED_ITEM_RELOAD_COST && bi.Slot.Type != INV_HAND) ? bi.GetMoveToCost(weapon.Slot) : 0;
					tuTemp += ruleWeapon.GetTULoad(slot);
					if (tuTemp < tuCost)
					{
						tuCost = tuTemp;
						ammo = bi;
						slotAmmo = slot;
					}
				}
			}

			if (ammo != null && SpendTimeUnits(tuCost))
			{
				weapon.SetAmmoForSlot(slotAmmo, ammo);

				var sound = ammo.Rules.ReloadSound;
				if (sound == Mod.NO_SOUND)
				{
					sound = ruleWeapon.ReloadSound;
				}
				if (sound == Mod.NO_SOUND)
				{
					sound = Mod.ITEM_RELOAD;
				}

				_lastReloadSound = sound;
				return true;
			}
		}
		return false;
	}

	/// <summary>Toggle the right hand as main hand for reactions.</summary>
	public void ToggleRightHandForReactions(bool isCtrl)
	{
		if (isCtrl)
		{
			if (IsRightHandPreferredForReactions)
			{
				_preferredHandForReactions = "";
			}
			_reactionsDisabledForRightHand = !_reactionsDisabledForRightHand;
		}
		else
		{
			if (IsRightHandPreferredForReactions)
			{
				_preferredHandForReactions = "";
			}
			else
			{
				_preferredHandForReactions = "STR_RIGHT_HAND";
			}
			_reactionsDisabledForRightHand = false;
		}
	}

	/// <summary>Toggle the left hand as main hand for reactions.</summary>
	public void ToggleLeftHandForReactions(bool isCtrl)
	{
		if (isCtrl)
		{
			if (IsLeftHandPreferredForReactions)
			{
				_preferredHandForReactions = "";
			}
			_reactionsDisabledForLeftHand = !_reactionsDisabledForLeftHand;
		}
		else
		{
			if (IsLeftHandPreferredForReactions)
			{
				_preferredHandForReactions = "";
			}
			else
			{
				_preferredHandForReactions = "STR_LEFT_HAND";
			}
			_reactionsDisabledForLeftHand = false;
		}
	}

	/// <summary>Is right hand preferred for reactions?</summary>
	public bool IsRightHandPreferredForReactions => _preferredHandForReactions == "STR_RIGHT_HAND";

	/// <summary>Is left hand preferred for reactions?</summary>
	public bool IsLeftHandPreferredForReactions => _preferredHandForReactions == "STR_LEFT_HAND";

	/// <summary>Is right hand disabled for reactions?</summary>
	public bool IsRightHandDisabledForReactions => _reactionsDisabledForRightHand;

	/// <summary>Is left hand disabled for reactions?</summary>
	public bool IsLeftHandDisabledForReactions => _reactionsDisabledForLeftHand;

	/// <summary>Get preferred weapon for reactions, if applicable.</summary>
	public BattleItem WeaponForReactions
	{
		get
		{
			if (_preferredHandForReactions.Length == 0)
				return null;

			BattleItem weapon;
			if (IsRightHandPreferredForReactions)
				weapon = RightHandWeapon;
			else
				weapon = LeftHandWeapon;

			if (weapon == null)
			{
				// find the empty hands weapon using the standard algorithm (i.e. standard order)
				BattleType[] typesToCheck = [BT_MELEE, BT_PSIAMP, BT_FIREARM/*, BT_MEDIKIT, BT_SCANNER, BT_MINDPROBE*/];
				foreach (var type in typesToCheck)
				{
					weapon = GetSpecialWeapon(type);
					if (weapon != null && weapon.Rules.IsSpecialUsingEmptyHand)
					{
						break;
					}
					weapon = null;
				}
				// but only use BT_MELEE and BT_FIREARM (BT_PSIAMP doesn't have BA_HIT nor BA_SNAPSHOT)
				if (weapon != null && weapon.Rules.BattleType == BT_PSIAMP)
				{
					weapon = null;
				}
			}

			if (weapon == null)
				return null;

			if (weapon.Rules.BattleType == BT_MELEE)
			{
				return weapon;
			}
			else
			{
				// ignore weapons without ammo (rules out grenades)
				if (!weapon.HaveAnyAmmo())
					return null;

				int tu = GetActionTUs(BA_SNAPSHOT, weapon).Time;
				if (tu > 0)
					return weapon;
			}

			return null;
		}
	}

	/// <summary>Get the units carried weight in strength units.</summary>
	/// <param name="draggingItem">item to ignore</param>
	/// <returns>weight</returns>
	public int GetCarriedWeight(BattleItem draggingItem = null)
	{
		int weight = _armor.Weight;
		foreach (var bi in _inventory)
		{
			if (bi == draggingItem) continue;
			weight += bi.TotalWeight;
		}
		return Math.Max(0, weight);
	}

	/// <summary>Get any utility weapon we may be carrying, or a built in one.</summary>
	public BattleItem GetUtilityWeapon(BattleType type)
	{
		BattleItem melee = RightHandWeapon;
		if (melee != null && melee.Rules.BattleType == type)
		{
			return melee;
		}
		melee = LeftHandWeapon;
		if (melee != null && melee.Rules.BattleType == type)
		{
			return melee;
		}
		melee = GetSpecialWeapon(type);
		if (melee != null)
		{
			return melee;
		}
		return null;
	}

	/// <summary>Set special weapon that is handled outside inventory.</summary>
	/// <param name="save">The battle (creates the items).</param>
	/// <param name="updateFromSave">The unit was loaded from a save (special weapons are already there).</param>
	public void SetSpecialWeapon(SavedBattleGame save, bool updateFromSave)
	{
		Mod mod = save.Mod;
		int i = 0;

		void addItem(RuleItem item)
		{
			if (item != null && i < SPEC_WEAPON_MAX)
			{
				if (BaseStats.psiSkill <= 0 && item.IsPsiRequired)
				{
					return;
				}

				//TODO: move this check to load of ruleset
				if ((item.BattleType == BT_FIREARM || item.BattleType == BT_MELEE) && item.ClipSize == 0)
				{
					throw new InvalidOperationException("Weapon " + item.Type + " is used as a special built-in weapon on unit " + UnitRules.Type + " but doesn't have it's own ammo - give it a clipSize!");
				}

				// we already have an item of this type, skip it
				foreach (var w in _specWeapon)
				{
					if (w != null && w.Rules == item)
					{
						return;
					}
				}

				_specWeapon[i++] = save.CreateItemForUnitSpecialBuiltin(item, this);
			}
		}

		if (_specWeapon[0] != null && updateFromSave)
		{
			// for backward compatibility, we try add corpse explosion
			addItem(Armor.SelfDestructItem);

			// new saves already contain special built-in weapons, we can stop here
			return;
			// old saves still need the below functionality to work properly
		}

		if (UnitRules != null)
		{
			addItem(mod.GetItem(UnitRules.MeleeWeapon));
		}

		addItem(Armor.SpecialWeapon);

		if (UnitRules != null && OriginalFaction == FACTION_HOSTILE)
		{
			addItem(mod.GetItem(UnitRules.PsiWeapon));
		}
		if (GeoscapeSoldier != null)
		{
			addItem(GeoscapeSoldier.Rules.SpecialWeapon);
		}

		addItem(Armor.SelfDestructItem);
	}

	/// <summary>Add/assign a special weapon loaded from a save.</summary>
	public void AddLoadedSpecialWeapon(BattleItem item)
	{
		for (int i = 0; i < SPEC_WEAPON_MAX; ++i)
		{
			if (_specWeapon[i] == null)
			{
				_specWeapon[i] = item;
				return;
			}
		}
		Logger.Error($"Failed to add special built-in item '{item.Rules.Type}' (id {item.Id}) to unit '{Type}' (id {Id})");
	}

	/// <summary>Remove all special weapons.</summary>
	public void RemoveSpecialWeapons(SavedBattleGame save)
	{
		for (int i = 0; i < SPEC_WEAPON_MAX; ++i)
		{
			var s = _specWeapon[i];
			if (s != null)
			{
				s.Owner = null; // stops being a special weapon, so that `removeItem` can remove it
				save.RemoveItem(s);
				_specWeapon[i] = null;
			}
		}
	}

	/// <summary>Get special weapon by battletype.</summary>
	public BattleItem GetSpecialWeapon(BattleType type)
	{
		for (int i = 0; i < SPEC_WEAPON_MAX; ++i)
		{
			if (_specWeapon[i] == null)
			{
				break;
			}
			if (_specWeapon[i].Rules.BattleType == type)
			{
				return _specWeapon[i];
			}
		}
		return null;
	}

	/// <summary>Get special weapon by name.</summary>
	public BattleItem GetSpecialWeapon(RuleItem weaponRule)
	{
		for (int i = 0; i < SPEC_WEAPON_MAX; ++i)
		{
			if (_specWeapon[i] == null)
			{
				break;
			}
			if (_specWeapon[i].Rules == weaponRule)
			{
				return _specWeapon[i];
			}
		}
		return null;
	}

	/// <summary>Gets the special weapon that uses an icon</summary>
	/// <param name="type">Parameter passed to get back the type of the weapon</param>
	/// <returns>Pointer the the weapon, null if not found</returns>
	public BattleItem GetSpecialIconWeapon(ref BattleType type)
	{
		for (int i = 0; i < SPEC_WEAPON_MAX; ++i)
		{
			if (_specWeapon[i] == null)
			{
				break;
			}

			if (_specWeapon[i].Rules.SpecialIconSprite != -1)
			{
				type = _specWeapon[i].Rules.BattleType;
				return _specWeapon[i];
			}
		}
		return null;
	}
}
