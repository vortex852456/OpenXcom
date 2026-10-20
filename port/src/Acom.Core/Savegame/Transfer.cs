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
// Ported from: src/Savegame/Transfer.h, src/Savegame/Transfer.cpp
#nullable disable

namespace Acom;

public enum TransferSortDirection
{
	BY_LIST_ORDER = 0,
	BY_UNIT_SIZE = 1,
	BY_TOTAL_SIZE = 2,
	BY_UNIT_COST = 3,
	BY_TOTAL_COST = 4,
}

public enum TransferType : uint
{
	TRANSFER_ITEM = 0,
	TRANSFER_CRAFT = 1,
	TRANSFER_SOLDIER = 2,
	TRANSFER_SCIENTIST = 3,
	TRANSFER_ENGINEER = 4,
}

/// <summary>One row of the transfer/purchase/sell lists (C++ struct).</summary>
public class TransferRow
{
	public TransferType type;
	public object rule;
	public string name;
	public int cost;
	public int qtySrc;
	public int qtyDst;
	public int amount;
	public int listOrder;
	public double size;
	public double totalSize;
	public long totalCost;
}

/// <summary>
/// Represents an item transfer.
/// Items are placed "in transit" whenever they are
/// purchased or transferred between bases.
/// </summary>
public class Transfer
{
	private int _hours;
	private Soldier _soldier;
	private Craft _craft;
	private RuleItem _itemId;
	private int _itemQty, _scientists, _engineers;
	private bool _delivered;

	/// <summary>Initializes a transfer.</summary>
	/// <param name="hours">Hours in-transit.</param>
	public Transfer(int hours)
	{
		_hours = hours;
		_soldier = null;
		_craft = null;
		_itemQty = 0;
		_scientists = 0;
		_engineers = 0;
		_delivered = false;
	}

	/// <summary>Loads the transfer from YAML.</summary>
	/// <param name="reader">YAML node.</param>
	/// <param name="base">Destination base.</param>
	/// <param name="mod">Game mod.</param>
	/// <param name="save">Pointer to savegame.</param>
	/// <returns>Was the transfer content valid? (C++ also deletes the transfer when not.)</returns>
	public bool Load(YamlNodeReader reader, Base @base, Mod mod, SavedGame save)
	{
		reader.TryRead("hours", ref _hours);

		var soldier = reader["soldier"];
		if (soldier)
		{
			string type = soldier["type"].ReadVal(mod.SoldiersList[0]);
			if (mod.GetSoldier(type) != null)
			{
				_soldier = new Soldier(mod.GetSoldier(type), null, 0 /*nationality*/);
				_soldier.Load(soldier, mod, save);
			}
			else
			{
				Logger.Error($"Failed to load soldier {type}");
				return false;
			}
		}
		var craft = reader["craft"];
		if (craft)
		{
			string type = craft["type"].ReadVal<string>();
			if (mod.GetCraft(type) != null)
			{
				_craft = new Craft(mod.GetCraft(type), @base);
				_craft.Load(craft, mod, null);
			}
			else
			{
				Logger.Error($"Failed to load craft {type}");
				return false;
			}

		}
		var item = reader["itemId"];
		if (item)
		{
			string name = item.ReadVal<string>();
			_itemId = mod.GetItem(name);
			if (_itemId == null)
			{
				Logger.Error($"Failed to load item {name}");
				return false;
			}
		}
		reader.TryRead("itemQty", ref _itemQty);
		reader.TryRead("scientists", ref _scientists);
		reader.TryRead("engineers", ref _engineers);
		reader.TryRead("delivered", ref _delivered);
		return true;
	}

	/// <summary>Saves the transfer to YAML.</summary>
	public void Save(YamlNodeWriter writer, Base b, Mod mod)
	{
		writer.SetAsMap();
		writer.Write("hours", _hours);
		if (_soldier != null)
		{
			_soldier.Save(writer["soldier"]);
		}
		else if (_craft != null)
		{
			_craft.Save(writer["craft"]);
		}
		else if (_itemQty != 0)
		{
			writer.Write("itemId", _itemId.Type);
			writer.Write("itemQty", _itemQty);
		}
		else if (_scientists != 0)
		{
			writer.Write("scientists", _scientists);
		}
		else if (_engineers != 0)
		{
			writer.Write("engineers", _engineers);
		}
		if (_delivered)
			writer.Write("delivered", _delivered);
	}

	/// <summary>The soldier being transferred.</summary>
	public Soldier Soldier
	{
		get => _soldier;
		set => _soldier = value;
	}

	/// <summary>The craft being transferred.</summary>
	public Craft Craft
	{
		get => _craft;
		set => _craft = value;
	}

	/// <summary>Returns the items being transferred.</summary>
	public RuleItem Items => _itemId;

	/// <summary>Changes the items being transferred.</summary>
	public void SetItems(RuleItem rule, int qty = 1)
	{
		_itemId = rule;
		_itemQty = qty;
	}

	/// <summary>Changes the scientists being transferred.</summary>
	public void SetScientists(int scientists)
	{
		_scientists = scientists;
	}

	/// <summary>Changes the engineers being transferred.</summary>
	public void SetEngineers(int engineers)
	{
		_engineers = engineers;
	}

	/// <summary>Returns the name of the contents of the transfer.</summary>
	public string GetName(Language lang)
	{
		if (_soldier != null)
		{
			return _soldier.GetName();
		}
		else if (_craft != null)
		{
			return _craft.GetName(lang);
		}
		else if (_scientists != 0)
		{
			return lang.GetString("STR_SCIENTISTS");
		}
		else if (_engineers != 0)
		{
			return lang.GetString("STR_ENGINEERS");
		}
		return lang.GetString(_itemId.Type);
	}

	/// <summary>Returns the time remaining until the transfer arrives at its destination (hours).</summary>
	public int Hours => _hours;

	/// <summary>Returns the quantity of items in the transfer.</summary>
	public int Quantity
	{
		get
		{
			if (_itemQty != 0)
			{
				return _itemQty;
			}
			else if (_scientists != 0)
			{
				return _scientists;
			}
			else if (_engineers != 0)
			{
				return _engineers;
			}
			return 1;
		}
	}

	/// <summary>Returns the type of the contents of the transfer.</summary>
	public TransferType Type
	{
		get
		{
			if (_soldier != null)
			{
				return TransferType.TRANSFER_SOLDIER;
			}
			else if (_craft != null)
			{
				return TransferType.TRANSFER_CRAFT;
			}
			else if (_scientists != 0)
			{
				return TransferType.TRANSFER_SCIENTIST;
			}
			else if (_engineers != 0)
			{
				return TransferType.TRANSFER_ENGINEER;
			}
			return TransferType.TRANSFER_ITEM;
		}
	}

	/// <summary>Advances the transfer and takes care of the delivery once it's arrived.</summary>
	/// <param name="base">Pointer to destination base.</param>
	public void Advance(Base @base)
	{
		_hours--;
		if (_hours <= 0)
		{
			if (_soldier != null)
			{
				@base.Soldiers.Add(_soldier);
			}
			else if (_craft != null)
			{
				@base.Crafts.Add(_craft);
				_craft.SetBase(@base);
				_craft.Checkup();
			}
			else if (_itemQty != 0)
			{
				@base.StorageItems.AddItem(_itemId, _itemQty);
			}
			else if (_scientists != 0)
			{
				@base.Scientists = @base.Scientists + _scientists;
			}
			else if (_engineers != 0)
			{
				@base.Engineers = @base.Engineers + _engineers;
			}
			_delivered = true;
		}
	}

	/// <summary>Whether the transfer has arrived (C++ uses it to decide if the destructor deletes soldier/craft).</summary>
	internal bool Delivered => _delivered;
}
