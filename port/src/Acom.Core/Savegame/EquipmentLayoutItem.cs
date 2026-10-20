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
// Ported from: src/Savegame/EquipmentLayoutItem.h, src/Savegame/EquipmentLayoutItem.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents a soldier-equipment layout item which is used
/// on the beginning of the Battlescape.
/// </summary>
public class EquipmentLayoutItem
{
	/// <summary>Value used for save backward and forward compatibility. Represent empty slot.</summary>
	private const string EmptyPlaceHolder = "NONE";

	private RuleItem _itemType;
	private RuleInventory _slot;
	private int _slotX, _slotY;
	private readonly RuleItem[] _ammoItem = new RuleItem[RuleItem.AmmoSlotMax];
	private int _fuseTimer;
	private bool _fixed;

	/// <summary>Initializes a new soldier-equipment layout item from YAML.</summary>
	public EquipmentLayoutItem(YamlNodeReader reader, Mod mod)
	{
		Load(reader, mod);
	}

	/// <summary>Initializes a new soldier-equipment layout item from a battle item.</summary>
	public EquipmentLayoutItem(BattleItem item)
	{
		_itemType = item.Rules;
		_slot = item.Slot;
		_slotX = item.SlotX;
		_slotY = item.SlotY;
		_fuseTimer = item.FuseTimer;
		_fixed = item.Rules.IsFixed;
		for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
		{
			if (item.NeedsAmmoForSlot(slot) && item.GetAmmoForSlot(slot) != null)
			{
				_ammoItem[slot] = item.GetAmmoForSlot(slot).Rules;
			}
			else
			{
				_ammoItem[slot] = null;
			}
		}
	}

	/// <summary>Port addition (tests): creates an entry from its values.</summary>
	internal EquipmentLayoutItem(RuleItem itemType, RuleInventory slot, int slotX, int slotY, RuleItem[] ammoItem, int fuseTimer, bool isFixed)
	{
		_itemType = itemType;
		_slot = slot;
		_slotX = slotX;
		_slotY = slotY;
		for (int i = 0; i < RuleItem.AmmoSlotMax && i < ammoItem.Length; ++i)
			_ammoItem[i] = ammoItem[i];
		_fuseTimer = fuseTimer;
		_fixed = isFixed;
	}

	/// <summary>Returns the item's type which has to be in a slot.</summary>
	public RuleItem ItemType => _itemType;

	/// <summary>Returns the slot to be occupied.</summary>
	public RuleInventory Slot => _slot;

	/// <summary>Returns the position-X in the slot to be occupied.</summary>
	public int SlotX => _slotX;

	/// <summary>Returns the position-Y in the slot to be occupied.</summary>
	public int SlotY => _slotY;

	/// <summary>Returns the ammo has to be loaded into the item.</summary>
	public RuleItem GetAmmoItemForSlot(int i) => _ammoItem[i];

	/// <summary>Returns the turn until explosion of the item. (if it's an activated grenade-type)</summary>
	public int FuseTimer => _fuseTimer;

	/// <summary>Is this a fixed weapon entry?</summary>
	public bool IsFixed => _fixed;

	/// <summary>Loads the soldier-equipment layout item from a YAML file.</summary>
	public void Load(YamlNodeReader reader, Mod mod)
	{
		_itemType = mod.GetItem(reader["itemType"].ReadVal<string>(), true);
		_slot = mod.GetInventory(reader["slot"].ReadVal<string>(), true);
		_slotX = reader["slotX"].ReadVal(0);
		_slotY = reader["slotY"].ReadVal(0);
		var ammoSlots = reader["ammoItemSlots"];
		if (ammoSlots)
		{
			for (int slot = 0; slot < RuleItem.AmmoSlotMax && ammoSlots[slot]; ++slot)
			{
				var s = ammoSlots[slot].ReadVal<string>();
				_ammoItem[slot] = s != EmptyPlaceHolder ? mod.GetItem(s, true) : null;
			}
		}
		else
		{
			var ammo = reader["ammoItem"];
			if (ammo)
			{
				_ammoItem[0] = mod.GetItem(ammo.ReadVal<string>(), true);
			}
		}
		_fuseTimer = reader["fuseTimer"].ReadVal(-1);
		_fixed = reader["fixed"].ReadVal(false);
	}

	/// <summary>Saves the soldier-equipment layout item to a YAML file.</summary>
	public void Save(YamlNodeWriter writer)
	{
		writer.SetAsMap();
		writer.SetFlowStyle();
		writer.Write("itemType", _itemType.Type);
		writer.Write("slot", _slot.Id);
		// only save this info if it's needed, reduce clutter in saves
		if (_slotX != 0)
			writer.Write("slotX", _slotX);
		if (_slotY != 0)
			writer.Write("slotY", _slotY);
		if (_ammoItem[0] != null)
			writer.Write("ammoItem", _ammoItem[0].Type);
		// Collections::untilLastIf: all slots up to the last used one
		int countLimit = 0;
		for (int i = 0; i < _ammoItem.Length; ++i)
		{
			if (_ammoItem[i] != null) countLimit = i + 1;
		}
		YamlNodeWriter ammoSlotWriter = null;
		for (int i = 0; i < countLimit; ++i)
		{
			if (ammoSlotWriter == null)
			{
				ammoSlotWriter = writer["ammoItemSlots"];
				ammoSlotWriter.SetAsSeq();
			}
			ammoSlotWriter.Write(_ammoItem[i] != null ? _ammoItem[i].Type : EmptyPlaceHolder);
		}
		if (_fuseTimer >= 0)
			writer.Write("fuseTimer", _fuseTimer);
		if (_fixed)
			writer.Write("fixed", _fixed);
	}
}
