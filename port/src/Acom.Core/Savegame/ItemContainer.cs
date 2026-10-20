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
// Ported from: src/Savegame/ItemContainer.h, src/Savegame/ItemContainer.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents the items contained by a certain entity,
/// like base stores, craft equipment, etc.
/// Handles all necessary item management tasks.
/// </summary>
/// <remarks>
/// C++ keys a <c>std::map</c> by rule pointer (iteration order = pointer order, effectively unspecified);
/// here a <see cref="Dictionary{TKey, TValue}"/> keyed by rule reference (insertion order).
/// The C++ <c>addItem(const std::string&amp;)</c> overload is deleted and therefore not ported.
/// </remarks>
public class ItemContainer
{
	private readonly Dictionary<RuleItem, int> _qty = [];

	/// <summary>Initializes an item container with no contents.</summary>
	public ItemContainer()
	{
	}

	/// <summary>Loads the item container from YAML.</summary>
	public void Load(YamlNodeReader reader, Mod mod) => Load(reader, name => mod.GetItem(name));

	/// <summary>Loads the item container from YAML, looking item rules up with <paramref name="getItem"/> (C++ <c>mod->getItem</c>).</summary>
	internal void Load(YamlNodeReader reader, Func<string, RuleItem> getItem)
	{
		if (!reader || !reader.IsMap)
			return;
		_qty.Clear();
		foreach (var item in reader.Children())
		{
			string name = item.ReadKey<string>();
			var type = getItem(name);
			if (type != null)
			{
				_qty[type] = item.ReadVal<int>();
			}
			else
			{
				Logger.Error($"Failed to load item {name}");
			}
		}
	}

	/// <summary>Saves the item container to YAML.</summary>
	public void Save(YamlNodeWriter writer)
	{
		writer.SetAsMap();
		// item containers are sorted alphabetically in the yaml mapping
		var sortedItems = new List<(string first, int second)>(_qty.Count);
		foreach (var pair in _qty)
			sortedItems.Add((pair.Key.Type, pair.Value));
		sortedItems.Sort((a, b) =>
		{
			int c = string.CompareOrdinal(a.first, b.first);
			return c != 0 ? c : a.second.CompareTo(b.second);
		});
		foreach (var pair in sortedItems)
			writer.Write(pair.first, pair.second);
	}

	/// <summary>Adds an item amount to the container.</summary>
	public void AddItem(RuleItem item, int qty = 1)
	{
		if (item != null)
		{
			_qty.TryGetValue(item, out int current);
			_qty[item] = current + qty;
		}
	}

	/// <summary>Removes an item amount from the container.</summary>
	public void RemoveItem(string id, int qty = 1)
	{
		if (Mod.IsEmptyRuleName(id))
		{
			return;
		}
		RuleItem found = null;
		foreach (var pair in _qty)
		{
			if (pair.Key.Type == id)
			{
				found = pair.Key;
				break;
			}
		}
		if (found == null)
		{
			return;
		}
		RemoveItem(found, qty);
	}

	/// <summary>Removes an item amount from the container.</summary>
	public void RemoveItem(RuleItem item, int qty = 1)
	{
		if (item != null)
		{
			if (!_qty.TryGetValue(item, out int current))
			{
				return;
			}

			if (qty < current)
			{
				_qty[item] = current - qty;
			}
			else
			{
				_qty.Remove(item);
			}
		}
	}

	/// <summary>Returns the quantity of an item in the container.</summary>
	public int GetItem(string id)
	{
		if (Mod.IsEmptyRuleName(id))
		{
			return 0;
		}

		foreach (var pair in _qty)
		{
			if (pair.Key.Type == id)
				return pair.Value;
		}
		return 0;
	}

	/// <summary>Returns the quantity of an item in the container.</summary>
	public int GetItem(RuleItem item)
	{
		if (item != null && _qty.TryGetValue(item, out int qty))
		{
			return qty;
		}
		return 0;
	}

	/// <summary>Returns the total quantity of the items in the container.</summary>
	public int TotalQuantity
	{
		get
		{
			int total = 0;
			foreach (var pair in _qty)
			{
				total += pair.Value;
			}
			return total;
		}
	}

	/// <summary>Returns the total size of the items in the container.</summary>
	public double TotalSize
	{
		get
		{
			double total = 0;
			foreach (var pair in _qty)
			{
				total += pair.Key.Size * pair.Value;
			}
			return total;
		}
	}

	/// <summary>Check if have any item.</summary>
	public bool Empty() => _qty.Count == 0;

	/// <summary>Clear all content.</summary>
	public void Clear() => _qty.Clear();

	/// <summary>Returns all the items currently contained within.</summary>
	public Dictionary<RuleItem, int> Contents => _qty;
}
