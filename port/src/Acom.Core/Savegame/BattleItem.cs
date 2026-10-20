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
// Ported from: src/Savegame/BattleItem.h, src/Savegame/BattleItem.cpp
// Port status: partial (missing: script hooks)
#nullable disable

namespace Acom;

/// <summary>
/// Represents a single item in the battlescape.
/// Contains battle-related info about an item like the position, ammo quantity, ...
/// </summary>
/// <seealso cref="RuleItem"/>
public class BattleItem
{
	public const string ScriptName = "BattleItem";

	private readonly int _id;
	private RuleItem _rules;
	private BattleUnit _owner, _previousOwner;
	private BattleUnit _unit;
	private Tile _tile;
	private int _inventoryMoveCostPercent;
	private RuleInventory _inventorySlot;
	private int _inventoryX, _inventoryY;
	private readonly BattleItem[] _ammoItem = new BattleItem[RuleItem.AmmoSlotMax];
	private readonly bool[] _ammoVisibility = new bool[RuleItem.AmmoSlotMax];
	private int _fuseTimer, _ammoQuantity;
	private int _painKiller, _heal, _stimulant;
	private bool _XCOMProperty, _droppedOnAlienTurn, _isAmmo, _isWeaponWithAmmo, _fuseEnabled;
	private readonly RuleItemAction _confAimedOrLaunch;
	private readonly RuleItemAction _confSnap;
	private readonly RuleItemAction _confAuto;
	private readonly RuleItemAction _confMelee;

	/// <summary>Initializes a item of the specified type.</summary>
	/// <param name="rules">Pointer to ruleset.</param>
	/// <param name="id">The id of the item; incremented (C++ passes the item id counter as <c>int*</c>).</param>
	public BattleItem(RuleItem rules, ref int id)
	{
		_id = id;
		_rules = rules;
		_fuseTimer = -1;
		id++;
		if (_rules != null)
		{
			_inventoryMoveCostPercent = _rules.InventoryMoveCostPercent;
			_confMelee = _rules.ConfigMelee;
			AmmoQuantity = _rules.ClipSize;
			if (_rules.BattleType == BattleType.BT_MEDIKIT)
			{
				HealQuantity = _rules.HealQuantity;
				PainKillerQuantity = _rules.PainKillerQuantity;
				StimulantQuantity = _rules.StimulantQuantity;
			}
			// weapon does not need ammo, ammo item points to weapon
			else if (_rules.BattleType == BattleType.BT_FIREARM || _rules.BattleType == BattleType.BT_MELEE)
			{
				_confAimedOrLaunch = _rules.ConfigAimed;
				_confAuto = _rules.ConfigAuto;
				_confSnap = _rules.ConfigSnap;
				bool showSelfAmmo = _rules.ClipSize > 0;
				for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
				{
					bool used = false;
					used |= (_confAimedOrLaunch != null && _confAimedOrLaunch.ammoSlot == slot);
					used |= (_confAuto != null && _confAuto.ammoSlot == slot);
					used |= (_confSnap != null && _confSnap.ammoSlot == slot);
					used |= (_confMelee != null && _confMelee.ammoSlot == slot);
					if (_rules.GetCompatibleAmmoForSlot(slot).Count == 0)
					{
						if (used && showSelfAmmo)
						{
							_ammoVisibility[slot] = true;
							showSelfAmmo = false;
						}
						_ammoItem[slot] = this;
					}
					else
					{
						_ammoVisibility[slot] = used;
						_isWeaponWithAmmo = true;
					}
				}
			}
		}
	}

	/// <summary>Loads the item from a YAML file.</summary>
	/// <remarks>C++ also takes the script globals (<c>ScriptGlobal*</c>); script values are not ported.</remarks>
	public void Load(YamlNodeReader reader, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		if (reader["inventoryMoveCost"] is { IsValid: true } cost)
			_inventoryMoveCostPercent = cost["basePercent"].ReadVal(_inventoryMoveCostPercent);
		string slot = reader["inventoryslot"].ReadVal("NULL");
		if (slot != "NULL")
		{
			_inventorySlot = mod.GetInventory(slot);
			if (_inventorySlot == null)
				_inventorySlot = mod.InventoryGround;
		}
		reader.TryRead("inventoryX", ref _inventoryX);
		reader.TryRead("inventoryY", ref _inventoryY);
		reader.TryRead("ammoqty", ref _ammoQuantity);
		reader.TryRead("painKiller", ref _painKiller);
		reader.TryRead("heal", ref _heal);
		reader.TryRead("stimulant", ref _stimulant);
		//reader.tryRead("fuseTimer", _fuseTimer);
		if (reader["fuseTimer"] is { IsValid: true } fuseTimer) // needed for compatibility with OXC
			FuseTimer = fuseTimer.ReadVal<int>();
		reader.TryRead("fuseEnabed", ref _fuseEnabled);
		reader.TryRead("droppedOnAlienTurn", ref _droppedOnAlienTurn);
		reader.TryRead("XCOMProperty", ref _XCOMProperty);
	}

	/// <summary>Saves the item to a YAML file.</summary>
	/// <remarks>C++ also takes the script globals (<c>ScriptGlobal*</c>); script values are not ported.</remarks>
	public void Save(YamlNodeWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.SetAsMap();
		writer.Write("id", _id);
		writer.Write("type", _rules.Type);
		if (_owner != null)
			writer.Write("owner", _owner.Id);
		if (_previousOwner != null)
			writer.Write("previousOwner", _previousOwner.Id);
		if (_unit != null)
			writer.Write("unit", _unit.Id);
		if (_inventoryMoveCostPercent != _rules.InventoryMoveCostPercent)
		{
			var mcWriter = writer["inventoryMoveCost"];
			mcWriter.SetAsMap();
			mcWriter.Write("basePercent", _inventoryMoveCostPercent);
		}
		if (_inventorySlot != null)
		{
			writer.Write("inventoryslot", _inventorySlot.Id);
			if (_inventorySlot.Type == InventoryType.INV_SLOT) // only for slot items this matter, for hands and ground it can be `0` for both
			{
				writer.Write("inventoryX", _inventoryX);
				writer.Write("inventoryY", _inventoryY);
			}
		}
		if (_tile != null)
			writer.Write("position", _tile.Position);
		if (_ammoQuantity != 0 || _rules.IsAmmoRechargeable) // To consider: maybe it would be better to just always write also zero?
			writer.Write("ammoqty", _ammoQuantity);
		if (_ammoItem[0] != null)
			writer.Write("ammoItem", _ammoItem[0].Id);
		// Collections::untilLastIf: all slots up to the last one that is set
		int countLimit = 0;
		for (int i = 0; i < _ammoItem.Length; ++i)
		{
			if (_ammoItem[i] != null)
				countLimit = i + 1;
		}
		if (countLimit > 0)
		{
			var ammoSlotWriter = writer["ammoItemSlots"];
			ammoSlotWriter.SetAsSeq();
			ammoSlotWriter.SetFlowStyle();
			for (int i = 0; i < countLimit; ++i)
			{
				ammoSlotWriter.Write(_ammoItem[i] != null ? _ammoItem[i].Id : -1);
			}
		}
		if (_rules != null && _rules.BattleType == BattleType.BT_MEDIKIT)
		{
			writer.Write("painKiller", _painKiller);
			writer.Write("heal", _heal);
			writer.Write("stimulant", _stimulant);
		}
		if (_fuseTimer != -1)
			writer.Write("fuseTimer", _fuseTimer);
		if (_fuseEnabled)
			writer.Write("fuseEnabed", _fuseEnabled);
		if (_droppedOnAlienTurn)
			writer.Write("droppedOnAlienTurn", _droppedOnAlienTurn);
		if (_XCOMProperty)
			writer.Write("XCOMProperty", _XCOMProperty);
	}

	/// <summary>Gets the ruleset for the item's type.</summary>
	public RuleItem Rules => _rules;

	/// <summary>Gets/sets the quantity of ammo in this item (255 for unlimited clips).</summary>
	public int AmmoQuantity
	{
		get
		{
			if (_rules.ClipSize == -1)
			{
				return 255;
			}
			return _ammoQuantity;
		}
		set => _ammoQuantity = value;
	}

	/// <summary>
	/// Gets/sets the turns until detonation. -1 = unprimed grenade. -2 = mapblock grenade to explode before battle.
	/// Turns are player/alien turns, not game turns.
	/// </summary>
	public int FuseTimer
	{
		get => _fuseTimer;
		set
		{
			var @event = _rules.FuseTriggerEvent;
			_fuseTimer = value;
			if (_fuseTimer >= 0)
			{
				if (@event.throwTrigger || @event.proximityTrigger)
				{
					_fuseEnabled = false;
				}
				else if (@event.defaultBehavior)
				{
					_fuseEnabled = true;
				}
				else
				{
					_fuseEnabled = false;
				}
			}
			else
			{
				_fuseEnabled = false;
			}
		}
	}

	/// <summary>Gets if fuse was triggered.</summary>
	public bool IsFuseEnabled => _fuseEnabled;

	/// <summary>Set fuse trigger.</summary>
	public void SetFuseEnabled(bool enable)
	{
		if (FuseTimer > -1)
		{
			_fuseEnabled = enable;
		}
	}

	/// <summary>Called at end of turn.</summary>
	public void FuseEndTurnUpdate()
	{
		var @event = _rules.FuseTriggerEvent;
		if (_fuseEnabled && FuseTimer > 0)
		{
			if (@event.defaultBehavior)
			{
				if (_rules.FuseTimerType != BattleFuseType.BFT_INSTANT)
				{
					--_fuseTimer;
				}
			}
		}
	}

	/// <summary>Get if item can trigger end of turn effect.</summary>
	/// <returns>True if grenade should explode or other item removed.</returns>
	public bool FuseTimeEvent()
	{
		var @event = _rules.FuseTriggerEvent;
		bool check()
		{
			if (_fuseEnabled && FuseTimer == 0)
			{
				if (@event.defaultBehavior)
				{
					return _rules.FuseTimerType != BattleFuseType.BFT_INSTANT;
				}
			}
			return false;
		}

		if (check())
		{
			if (Rng.Percent(_rules.SpecialChance))
			{
				return true;
			}
			else
			{
				//grenade fail to explode or item to get removed.
				if (_rules.FuseTimerType == BattleFuseType.BFT_SET)
				{
					FuseTimer = 1;
				}
				else
				{
					FuseTimer = -1;
				}
			}
		}
		return false;
	}

	/// <summary>Called when item is throw.</summary>
	public bool FuseThrowEvent()
	{
		var @event = _rules.FuseTriggerEvent;
		bool check()
		{
			if (_fuseEnabled && FuseTimer == 0)
			{
				if (@event.throwExplode)
				{
					return true;
				}
				else if (@event.defaultBehavior)
				{
					return _rules.BattleType == BattleType.BT_GRENADE && (Options.battleInstantGrenade || _rules.FuseTimerType == BattleFuseType.BFT_INSTANT);
				}
			}
			return false;
		}

		if (@event.throwTrigger)
		{
			if (_rules.FuseTimerType == BattleFuseType.BFT_NONE)
			{
				_fuseEnabled = true;
				_fuseTimer = 0;
			}
			else if (_fuseTimer >= 0)
			{
				_fuseEnabled = true;
			}
		}

		if (check())
		{
			return Rng.Percent(_rules.SpecialChance);
		}
		return false;
	}

	/// <summary>Called when item proximity fuse is triggered.</summary>
	public bool FuseProximityEvent()
	{
		var @event = _rules.FuseTriggerEvent;
		bool check()
		{
			if (_fuseEnabled && FuseTimer >= 0)
			{
				if (@event.proximityExplode)
				{
					return true;
				}
				else if (@event.defaultBehavior)
				{
					return _rules.BattleType == BattleType.BT_PROXIMITYGRENADE;
				}
			}
			return false;
		}

		if (@event.proximityTrigger)
		{
			if (_rules.FuseTimerType == BattleFuseType.BFT_NONE)
			{
				_fuseEnabled = true;
				_fuseTimer = 0;
			}
			else if (_fuseTimer >= 0)
			{
				_fuseEnabled = true;
			}
		}

		if (check())
		{
			return Rng.Percent(_rules.SpecialChance);
		}
		return false;
	}

	/// <summary>
	/// Spends a bullet from the ammo in this item.
	/// (Or spends a certain amount of energy from this battery item.)
	/// </summary>
	/// <returns>True if there are bullets left.</returns>
	public bool SpendBullet(int spendPerShot)
	{
		if (_ammoQuantity >= spendPerShot)
			_ammoQuantity -= spendPerShot;

		if (_ammoQuantity == 0)
			return false;
		else
			return true;
	}

	/// <summary>Spend one quantity of a healing item use.</summary>
	public void SpendHealingItemUse(BattleMediKitAction mediKitAction)
	{
		if (mediKitAction == BattleMediKitAction.BMA_PAINKILLER)
		{
			PainKillerQuantity = PainKillerQuantity - 1;
		}
		else if (mediKitAction == BattleMediKitAction.BMA_STIMULANT)
		{
			StimulantQuantity = StimulantQuantity - 1;
		}
		else if (mediKitAction == BattleMediKitAction.BMA_HEAL)
		{
			HealQuantity = HealQuantity - 1;
		}
	}

	/// <summary>Check if owner is removed from game.</summary>
	public bool IsOwnerIgnored => _owner != null && _owner.IsIgnored;

	/// <summary>Gets/sets the item's owner. Setting also makes the old owner the previous owner.</summary>
	public BattleUnit Owner
	{
		get => _owner;
		set
		{
			_previousOwner = _owner;
			_owner = value;
		}
	}

	/// <summary>Gets/sets the item's previous owner.</summary>
	public BattleUnit PreviousOwner { get => _previousOwner; set => _previousOwner = value; }

	/// <summary>Removes the item from the previous owner and moves it to the new owner.</summary>
	public void MoveToOwner(BattleUnit owner)
	{
		if (_tile != null)
		{
			_tile.RemoveItem(this);
			_tile = null;
		}
		if (owner != _owner)
		{
			Owner = owner;

			_previousOwner?.Inventory.Remove(this);
			_owner?.Inventory.Add(this);
		}
	}

	/// <summary>Checks if the item is a special built-in weapon (outside of the inventory).</summary>
	public bool IsSpecialWeapon => Owner != null && Slot == null;

	/// <summary>Gets/sets the item's inventory slot.</summary>
	public RuleInventory Slot { get => _inventorySlot; set => _inventorySlot = value; }

	/// <summary>Gets the cost of moving item to given slot.</summary>
	public int GetMoveToCost(RuleInventory slot)
	{
		ArgumentNullException.ThrowIfNull(slot);
		int cost = _inventorySlot.GetCost(slot);
		if (cost == 0)
		{
			// if move was free it stay free, required to prevent paying cost of move only for clicking on item in inventory
			return 0;
		}
		else if (_inventorySlot.Type == InventoryType.INV_HAND && slot.Type == InventoryType.INV_GROUND)
		{
			// this special case has two roles:
			// * right now dropping ammo when reloading only uses default move cost, manually dropping should have same cost.
			// * conceptually you should be able to relese grip and item should fall down, "hard to grab, easy to drop"
			return cost;
		}
		else
		{
			return Math.Max(1, cost * _inventoryMoveCostPercent / 100);
		}
	}

	/// <summary>Gets/sets the item's inventory X position.</summary>
	public int SlotX { get => _inventoryX; set => _inventoryX = value; }

	/// <summary>Gets/sets the item's inventory Y position.</summary>
	public int SlotY { get => _inventoryY; set => _inventoryY = value; }

	/// <summary>Checks if the item is covering certain inventory slot(s).</summary>
	/// <param name="x">Slot X position.</param>
	/// <param name="y">Slot Y position.</param>
	/// <param name="item">Item to check for overlap, or null if none.</param>
	public bool OccupiesSlot(int x, int y, BattleItem item = null)
	{
		if (item == this || _inventorySlot == null)
			return false;
		if (_inventorySlot.Type == InventoryType.INV_HAND)
			return true;
		if (item == null)
		{
			return (x >= _inventoryX && x < _inventoryX + _rules.InventoryWidth &&
					y >= _inventoryY && y < _inventoryY + _rules.InventoryHeight);
		}
		else
		{
			return !(x >= _inventoryX + _rules.InventoryWidth ||
					x + item.Rules.InventoryWidth <= _inventoryX ||
					y >= _inventoryY + _rules.InventoryHeight ||
					y + item.Rules.InventoryHeight <= _inventoryY);
		}
	}

	/// <summary>Gets the item's floor sprite.</summary>
	public Surface GetFloorSprite(SurfaceSet set, SavedBattleGame save, int animFrame, int shade)
	{
		ArgumentNullException.ThrowIfNull(set);
		int i = _rules.FloorSprite;
		if (i != -1)
		{
			Surface surf = set.GetFrame(i);
			//enforce compatibility with basic version
			if (surf == null)
			{
				throw new InvalidOperationException("Image missing in 'FLOOROB.PCK' for item '" + _rules.Type + "'");
			}

			// script hook not ported: ModScript::SelectItemSprite (BODYPART_ITEM_FLOOR) keeps the sprite index
			var newSurf = set.GetFrame(i);
			if (newSurf == null)
			{
				newSurf = surf;
			}
			return newSurf;
		}
		else
		{
			return null;
		}
	}

	/// <summary>Gets the item's inventory sprite.</summary>
	public Surface GetBigSprite(SurfaceSet set, SavedBattleGame save, int animFrame)
	{
		ArgumentNullException.ThrowIfNull(set);
		int i = _rules.BigSprite;
		if (i != -1)
		{
			Surface surf = set.GetFrame(i);
			//enforce compatibility with basic version
			if (surf == null)
			{
				throw new InvalidOperationException("Image missing in 'BIGOBS.PCK' for item '" + _rules.Type + "'");
			}

			// script hook not ported: ModScript::SelectItemSprite (BODYPART_ITEM_INVENTORY) keeps the sprite index
			var newSurf = set.GetFrame(i);
			if (newSurf == null)
			{
				newSurf = surf;
			}
			return newSurf;
		}
		else
		{
			return null;
		}
	}

	/// <summary>Check if item use any ammo.</summary>
	public bool IsWeaponWithAmmo => _isWeaponWithAmmo;

	/// <summary>
	/// Check if the weapon is loaded with any ammo item(s). IMPORTANT: ammo quantity can also be zero!
	/// Empty clip also counts as a loaded weapon! empty "built-in" clip also counts as a loaded weapon!
	/// </summary>
	public bool HaveAnyAmmo()
	{
		if (!_isWeaponWithAmmo)
		{
			return true;
		}
		if (_rules.BattleType == BattleType.BT_MELEE)
		{
			return GetAmmoForActionConst(BattleActionType.BA_HIT) != null;
		}
		else
		{
			return GetAmmoForActionConst(BattleActionType.BA_AIMEDSHOT) != null ||
				GetAmmoForActionConst(BattleActionType.BA_AUTOSHOT) != null ||
				GetAmmoForActionConst(BattleActionType.BA_SNAPSHOT) != null;
		}
	}

	/// <summary>Check if weapon have all ammo slot filled.</summary>
	public bool HaveAllAmmo()
	{
		foreach (var a in _ammoItem)
		{
			if (a == null)
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>Sets the item's ammo item based on it type.</summary>
	/// <returns>True if item fit to weapon.</returns>
	public bool SetAmmoPreMission(BattleItem item)
	{
		ArgumentNullException.ThrowIfNull(item);
		int slot = _rules.GetSlotForAmmo(item.Rules);
		if (slot >= 0)
		{
			if (_ammoItem[slot] != null)
				return false;

			SetAmmoForSlot(slot, item);
			return true;
		}

		return false;
	}

	/// <summary>Get configuration of action on that item, or null for wrong action type or item.</summary>
	public RuleItemAction GetActionConf(BattleActionType action)
	{
		switch (action)
		{
			case BattleActionType.BA_LAUNCH:
			case BattleActionType.BA_AIMEDSHOT: return _confAimedOrLaunch;
			case BattleActionType.BA_AUTOSHOT: return _confAuto;
			case BattleActionType.BA_SNAPSHOT: return _confSnap;
			case BattleActionType.BA_HIT: return _confMelee;
			default: return null;
		}
	}

	/// <summary>Check if attack shoot in arc.</summary>
	public bool GetArcingShot(BattleActionType action)
	{
		if (_rules.ArcingShot)
		{
			return true;
		}

		var conf = GetActionConf(action);
		if (conf != null && conf.arcing)
		{
			return true;
		}

		return false;
	}

	/// <summary>Determines if this item uses ammo.</summary>
	public bool NeedsAmmoForAction(BattleActionType action)
	{
		var conf = GetActionConf(action);
		if (conf == null || conf.ammoSlot == RuleItem.AmmoSlotSelfUse)
		{
			return false;
		}

		return NeedsAmmoForSlot(conf.ammoSlot);
	}

	/// <summary>
	/// Get ammo used by action: the C++ <c>const</c> overload (used through <c>const BattleItem*</c>).
	/// Unlike <see cref="GetAmmoForAction(BattleActionType)"/> an empty clip is null only if it is not rechargeable,
	/// and the shot cost (<c>spendPerShot</c>) is not checked.
	/// </summary>
	public BattleItem GetAmmoForActionConst(BattleActionType action)
	{
		var conf = GetActionConf(action);
		if (conf == null)
		{
			return null;
		}
		if (conf.ammoSlot == RuleItem.AmmoSlotSelfUse)
		{
			return this;
		}

		var ammo = GetAmmoForSlot(conf.ammoSlot);
		if (ammo != null && ammo.AmmoQuantity == 0 && !ammo.Rules.IsAmmoRechargeable)
		{
			return null;
		}
		return ammo;
	}

	/// <summary>
	/// Get ammo used by action: the C++ non-const overload with its default arguments
	/// (<c>message = nullptr, spendPerShot = nullptr</c>), which C++ picks for every call through a non-const <c>BattleItem*</c>.
	/// </summary>
	/// <seealso cref="GetAmmoForActionConst"/>
	public BattleItem GetAmmoForAction(BattleActionType action)
	{
		string message = null;
		int spendPerShot = 0;
		return GetAmmoForAction(action, ref message, ref spendPerShot);
	}

	/// <summary>Get ammo used by action (C++ non-const overload with a message).</summary>
	/// <param name="action">Battle Action done using this item.</param>
	/// <param name="message">Set to the error message if weapon don't have ammo (else unchanged).</param>
	public BattleItem GetAmmoForAction(BattleActionType action, ref string message)
	{
		int spendPerShot = 0;
		return GetAmmoForAction(action, ref message, ref spendPerShot);
	}

	/// <summary>Get ammo used by action (C++ non-const overload).</summary>
	/// <param name="action">Battle Action done using this item.</param>
	/// <param name="message">Set to the error message if weapon don't have ammo (else unchanged).</param>
	/// <param name="spendPerShot">Set to how much ammo should be spent for one shot (if the action has a config).</param>
	public BattleItem GetAmmoForAction(BattleActionType action, ref string message, ref int spendPerShot)
	{
		var conf = GetActionConf(action);
		if (conf == null)
		{
			return null;
		}
		spendPerShot = conf.spendPerShot;
		if (conf.ammoSlot == RuleItem.AmmoSlotSelfUse)
		{
			return this;
		}

		var ammo = GetAmmoForSlot(conf.ammoSlot);
		if (ammo == null)
		{
			message = "STR_NO_AMMUNITION_LOADED";
			return null;
		}
		if (ammo.AmmoQuantity < conf.spendPerShot)
		{
			message = "STR_NO_ROUNDS_LEFT"; // no rounds left (or not enough energy left in the battery)
			return null;
		}
		return ammo;
	}

	/// <summary>Spend weapon ammo, if depleted remove clip.</summary>
	public void SpendAmmoForAction(BattleActionType action, SavedBattleGame save)
	{
		ArgumentNullException.ThrowIfNull(save);
		if (save.DebugMode || GetActionConf(action).ammoSlot == RuleItem.AmmoSlotSelfUse)
		{
			return;
		}

		int spendPerShot = 1;
		string message = null; // C++ passes nullptr
		var ammo = GetAmmoForAction(action, ref message, ref spendPerShot);
		if (ammo != null)
		{
			if (ammo.Rules.ClipSize > 0 && ammo.SpendBullet(spendPerShot) == false && !ammo.Rules.IsAmmoRechargeable)
			{
				save.RemoveItem(ammo);
				ammo.IsAmmo = false;
				if (ammo != this)
				{
					for (int i = 0; i < _ammoItem.Length; ++i)
					{
						if (_ammoItem[i] == ammo)
						{
							_ammoItem[i] = null;
						}
					}
				}
			}
		}
	}

	/// <summary>Check how many shoots attack can perform.</summary>
	/// <returns>True if still can shoot.</returns>
	public bool HaveNextShotsForAction(BattleActionType action, int shotCount)
	{
		var conf = GetActionConf(action);
		if (conf != null)
		{
			return shotCount < conf.shots;
		}
		return false;
	}

	/// <summary>Determines if the item uses ammo in the slot.</summary>
	public bool NeedsAmmoForSlot(int slot) => (_isWeaponWithAmmo && _ammoItem[slot] != this); // no ammo for this weapon is needed

	/// <summary>Set ammo slot with new ammo.</summary>
	/// <returns>Old item if was set.</returns>
	public BattleItem SetAmmoForSlot(int slot, BattleItem item)
	{
		if (!NeedsAmmoForSlot(slot))
		{
			return null;
		}

		BattleItem oldItem = _ammoItem[slot];
		if (oldItem != null)
		{
			oldItem.IsAmmo = false;
		}
		_ammoItem[slot] = item;
		if (item != null)
		{
			item.MoveToOwner(null);
			item.Slot = null;
			item.IsAmmo = true;
		}
		return oldItem;
	}

	/// <summary>Gets the item's ammo item.</summary>
	public BattleItem GetAmmoForSlot(int slot) => _ammoItem[slot];

	/// <summary>Get ammo count visibility for slot.</summary>
	public bool IsAmmoVisibleForSlot(int slot) => _ammoVisibility[slot];

	/// <summary>Get item weight with ammo weight.</summary>
	public int TotalWeight
	{
		get
		{
			int weight = _rules.Weight;
			foreach (var a in _ammoItem)
			{
				if (a != null && a != this)
				{
					weight += a._rules.Weight;
				}
			}
			return weight;
		}
	}

	/// <summary>Get waypoints count of weapon or from ammo (maximum waypoints count or -1 if unlimited).</summary>
	public int CurrentWaypoints
	{
		get
		{
			int waypoints = _rules.Waypoints;
			var ammo = GetAmmoForActionConst(BattleActionType.BA_LAUNCH);
			if (waypoints == 0 && ammo != null && ammo != this)
			{
				waypoints = ammo._rules.Waypoints;
			}
			return waypoints;
		}
	}

	/// <summary>Gets/sets the item's tile.</summary>
	public Tile Tile { get => _tile; set => _tile = value; }

	/// <summary>Gets the item's unique id.</summary>
	public int Id => _id;

	/// <summary>Gets/sets the corpse's unit.</summary>
	public BattleUnit Unit { get => _unit; set => _unit = value; }

	/// <summary>Gets/sets medikit heal quantity.</summary>
	public int HealQuantity { get => _heal; set => _heal = value; }

	/// <summary>Gets/sets medikit pain killers quantity.</summary>
	public int PainKillerQuantity { get => _painKiller; set => _painKiller = value; }

	/// <summary>Gets/sets medikit stimulant quantity.</summary>
	public int StimulantQuantity { get => _stimulant; set => _stimulant = value; }

	/// <summary>Gets/sets the XCom property flag. This is to determine at debriefing what goes into the base/craft.</summary>
	public bool XCOMProperty { get => _XCOMProperty; set => _XCOMProperty = value; }

	/// <summary>
	/// Gets/sets the "dropped on non-player turn" flag. This is to determine whether or not
	/// aliens should attempt to pick this item up, as items dropped by the player may be "honey traps".
	/// </summary>
	public bool TurnFlag { get => _droppedOnAlienTurn; set => _droppedOnAlienTurn = value; }

	/// <summary>Converts an unconscious body into a dead one.</summary>
	/// <param name="rules">the rules of the corpse item to convert this item into.</param>
	public void ConvertToCorpse(RuleItem rules)
	{
		ArgumentNullException.ThrowIfNull(rules);
		if (_unit != null && _rules.BattleType == BattleType.BT_CORPSE && rules.BattleType == BattleType.BT_CORPSE)
		{
			_rules = rules;
		}
	}

	/// <summary>Check if item can glow in darkness.</summary>
	public bool Glow
	{
		get
		{
			if (_rules.BattleType == BattleType.BT_FLARE)
			{
				return (_rules.FuseTriggerEvent.defaultBehavior && _rules.FuseTimerType == BattleFuseType.BFT_NONE) || (_fuseEnabled && FuseTimer >= 0);
			}
			else
			{
				return false;
			}
		}
	}

	/// <summary>Gets range of glow in tiles.</summary>
	public int GlowRange
	{
		get
		{
			var owner = _unit ?? _previousOwner;
			return _rules.GetPowerBonus(new BattleActionAttack { type = BattleActionType.BA_NONE, attacker = owner, weapon_item = this, damage_item = this });
		}
	}

	/// <summary>Gets update range needed by this item. For simplicity we always update to max range if it can glow.</summary>
	public int VisibilityUpdateRange => _rules.BattleType == BattleType.BT_FLARE ? GlowRange : 1;

	/// <summary>Gets/sets the flag on this item indicating whether or not it is a clip used in a weapon.</summary>
	public bool IsAmmo { get => _isAmmo; set => _isAmmo = value; }
}
