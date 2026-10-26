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
// Ported from: src/Mod/RuleInventory.h, src/Mod/RuleInventory.cpp
// Port status: partial (missing: script hooks)
#nullable disable

namespace Acom;

/// <summary>One slot of an inventory section, in slot units.</summary>
public class RuleSlot
{
	public int x;
	public int y;

	/// <summary>YAML: sequence [x, y] (C++ read overload, deserialization only).</summary>
	public static bool YamlRead(YamlNodeReader reader, ref RuleSlot val)
	{
		val ??= new RuleSlot();
		val.x = reader[0].ReadVal<int>();
		val.y = reader[1].ReadVal<int>();
		return true;
	}
}

public enum InventoryType : uint
{
	INV_SLOT = 0,
	INV_HAND = 1,
	INV_GROUND = 2,
}

/// <summary>
/// Represents a specific section of the inventory,
/// containing information like available slots and screen position.
/// </summary>
public partial class RuleInventory
{
	public const int SLOT_W = 16;
	public const int SLOT_H = 16;
	public const int HAND_W = 2;
	public const int HAND_H = 3;
	public const int PAPERDOLL_W = 40;
	public const int PAPERDOLL_H = 70;
	public const int PAPERDOLL_X = 60;
	public const int PAPERDOLL_Y = 65;

	/// <summary>Name of class used in script.</summary>
	public const string ScriptName = "RuleInventory";

	private readonly string _id;
	private int _x, _y;
	private InventoryType _type;
	private List<RuleSlot> _slots = [];
	private SortedDictionary<string, int> _costs = new(StringComparer.Ordinal);
	private int _listOrder;
	private int _hand;

	/// <summary>Creates a blank ruleset for a certain type of inventory section.</summary>
	/// <param name="id">String defining the id.</param>
	/// <param name="listOrder">The list weight for this inventory.</param>
	public RuleInventory(string id, int listOrder)
	{
		_id = id;
		_x = 0;
		_y = 0;
		_type = InventoryType.INV_SLOT;
		_listOrder = listOrder;
		_hand = 0;
	}

	/// <summary>Loads the inventory from YAML.</summary>
	public void Load(YamlNodeReader reader)
	{
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent);
		}

		reader.TryRead("x", ref _x);
		reader.TryRead("y", ref _y);
		reader.TryRead("type", ref _type);
		reader.TryRead("slots", ref _slots);
		reader.TryRead("costs", ref _costs);
		reader.TryRead("listOrder", ref _listOrder);
		if (_id == "STR_RIGHT_HAND")
		{
			_hand = 2;
		}
		else if (_id == "STR_LEFT_HAND")
		{
			_hand = 1;
		}
		else
		{
			_hand = 0;
		}
	}

	/// <summary>Gets the language string that names this inventory section. Each section has a unique name.</summary>
	public string Id { get => _id; }

	/// <summary>Gets the X position of the inventory section on the screen, in pixels.</summary>
	public int X { get => _x; }

	/// <summary>Gets the Y position of the inventory section on the screen, in pixels.</summary>
	public int Y { get => _y; }

	/// <summary>
	/// Gets the type of the inventory section.
	/// Slot-based contain a limited number of slots.
	/// Hands only contain one slot but can hold any item.
	/// Ground can hold infinite items but don't attach to soldiers.
	/// </summary>
	public InventoryType Type { get => _type; }

	/// <summary>Gets if this slot is right hand.</summary>
	public bool IsRightHand { get => _hand == 2; }

	/// <summary>Gets if this slot is left hand.</summary>
	public bool IsLeftHand { get => _hand == 1; }

	/// <summary>Gets all the slots in the inventory section.</summary>
	public List<RuleSlot> Slots { get => _slots; }

	/// <summary>Gets the slot located in the specified mouse position.</summary>
	/// <param name="x">Mouse X position. Returns the slot's X position.</param>
	/// <param name="y">Mouse Y position. Returns the slot's Y position.</param>
	/// <returns>True if there's a slot there.</returns>
	public bool CheckSlotInPosition(ref int x, ref int y)
	{
		int mouseX = x, mouseY = y;
		if (_type == InventoryType.INV_HAND)
		{
			for (int xx = 0; xx < HAND_W; ++xx)
			{
				for (int yy = 0; yy < HAND_H; ++yy)
				{
					if (mouseX >= _x + xx * SLOT_W && mouseX < _x + (xx + 1) * SLOT_W &&
						mouseY >= _y + yy * SLOT_H && mouseY < _y + (yy + 1) * SLOT_H)
					{
						x = 0;
						y = 0;
						return true;
					}
				}
			}
		}
		else if (_type == InventoryType.INV_GROUND)
		{
			if (mouseX >= _x && mouseX < Screen.ORIGINAL_WIDTH && mouseY >= _y && mouseY < Screen.ORIGINAL_HEIGHT)
			{
				x = (int)Math.Floor((double)(mouseX - _x) / SLOT_W);
				y = (int)Math.Floor((double)(mouseY - _y) / SLOT_H);
				return true;
			}
		}
		else
		{
			foreach (var coord in _slots)
			{
				if (mouseX >= _x + coord.x * SLOT_W && mouseX < _x + (coord.x + 1) * SLOT_W &&
					mouseY >= _y + coord.y * SLOT_H && mouseY < _y + (coord.y + 1) * SLOT_H)
				{
					x = coord.x;
					y = coord.y;
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>Checks if an item completely fits when placed in a certain slot.</summary>
	/// <param name="item">Item ruleset.</param>
	/// <param name="x">Slot X position.</param>
	/// <param name="y">Slot Y position.</param>
	/// <returns>True if there's a slot there.</returns>
	public bool FitItemInSlot(RuleItem item, int x, int y)
	{
		ArgumentNullException.ThrowIfNull(item);
		return FitItemInSlot(item.InventoryWidth, item.InventoryHeight, x, y);
	}

	/// <summary><see cref="FitItemInSlot(RuleItem, int, int)"/> with the item size.</summary>
	internal bool FitItemInSlot(int inventoryWidth, int inventoryHeight, int x, int y)
	{
		if (_type == InventoryType.INV_HAND)
		{
			return true;
		}
		else if (_type == InventoryType.INV_GROUND)
		{
			int width = (320 - _x) / SLOT_W;
			int height = (200 - _y) / SLOT_H;
			int xOffset = 0;
			while (x >= xOffset + width)
				xOffset += width;
			for (int xx = x; xx < x + inventoryWidth; ++xx)
			{
				for (int yy = y; yy < y + inventoryHeight; ++yy)
				{
					if (!(xx >= xOffset && xx < xOffset + width && yy >= 0 && yy < height))
						return false;
				}
			}
			return true;
		}
		else
		{
			int totalSlots = inventoryWidth * inventoryHeight;
			int foundSlots = 0;
			foreach (var coord in _slots)
			{
				if (foundSlots >= totalSlots)
				{
					break; // loop finished
				}
				if (coord.x >= x && coord.x < x + inventoryWidth &&
					coord.y >= y && coord.y < y + inventoryHeight)
				{
					foundSlots++;
				}
			}
			return (foundSlots == totalSlots);
		}
	}

	/// <summary>Gets the time unit cost to place an item in another section.</summary>
	/// <param name="slot">The new section.</param>
	/// <returns>The time unit cost.</returns>
	public int GetCost(RuleInventory slot)
	{
		ArgumentNullException.ThrowIfNull(slot);
		if (slot == this)
			return 0;
		return _costs[slot.Id]; // C++ dereferences find() without a check
	}

	/// <summary>Gets the list weight for this inventory section.</summary>
	public int ListOrder { get => _listOrder; }
}
