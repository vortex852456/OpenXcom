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
// Ported from: src/Mod/MapBlock.h, src/Mod/MapBlock.cpp
#nullable disable

namespace Acom;

public enum MapBlockType
{
	MT_UNDEFINED = -1,
	MT_DEFAULT = 0,
	MT_LANDINGZONE = 1,
	MT_EWROAD = 2,
	MT_NSROAD = 3,
	MT_CROSSING = 4,
}

/// <summary>Items placed at random positions of a map block.</summary>
public class RandomizedItems
{
	public Position position;
	public int amount;
	public int fuseTimerMin;
	public int fuseTimerMax;
	public bool mixed;
	public List<string> itemList;

	public RandomizedItems()
	{
		amount = 1;
		fuseTimerMin = -1;
		fuseTimerMax = -1;
		mixed = false;
		itemList = [];
	}

	/// <summary>YAML deserialization (C++ read(ryml::ConstNodeRef, RandomizedItems*)).</summary>
	public static bool YamlRead(YamlNodeReader reader, ref RandomizedItems val)
	{
		val ??= new RandomizedItems();
		reader.TryRead("position", ref val.position);
		reader.TryRead("amount", ref val.amount);
		reader.TryRead("fuseTimerMin", ref val.fuseTimerMin);
		reader.TryRead("fuseTimerMax", ref val.fuseTimerMax);
		reader.TryRead("mixed", ref val.mixed);
		reader.TryRead("itemList", ref val.itemList);
		return true;
	}
}

/// <summary>Items placed on a map block, extended syntax.</summary>
public class ExtendedItems
{
	public string type;
	public List<Position> pos;
	public int fuseTimerMin;
	public int fuseTimerMax;
	public List<(string first, int second)> ammoDef;

	public ExtendedItems()
	{
		type = "";
		pos = [];
		fuseTimerMin = -1;
		fuseTimerMax = -1;
		ammoDef = [];
	}

	/// <summary>YAML deserialization (C++ read(ryml::ConstNodeRef, ExtendedItems*)).</summary>
	public static bool YamlRead(YamlNodeReader reader, ref ExtendedItems val)
	{
		val ??= new ExtendedItems();
		reader.TryRead("type", ref val.type);
		reader.TryRead("pos", ref val.pos);
		reader.TryRead("fuseTimerMin", ref val.fuseTimerMin);
		reader.TryRead("fuseTimerMax", ref val.fuseTimerMax);
		reader.TryRead("ammoDef", ref val.ammoDef);
		return true;
	}
}

/// <summary>
/// Represents a Terrain Map Block.
/// It contains constant info about this mapblock, like its name, dimensions, attributes...
/// Map blocks are stored in RuleTerrain objects.
/// </summary>
/// <seealso href="http://www.ufopaedia.org/index.php?title=MAPS_Terrain"/>
public class MapBlock
{
	private string _name;
	private int _size_x, _size_y, _size_z;
	private List<int> _groups = [], _revealedFloors = [];
	private List<int> _craftInventoryTile = [];
	private SortedDictionary<string, List<Position>> _items = new(StringComparer.Ordinal);
	private SortedDictionary<string, (int first, int second)> _itemsFuseTimer = new(StringComparer.Ordinal);
	private List<RandomizedItems> _randomizedItems = [];
	private List<ExtendedItems> _extendedItems = [];

	/// <summary>MapBlock construction.</summary>
	public MapBlock(string name)
	{
		_name = name;
		_size_x = 10;
		_size_y = 10;
		_size_z = 4;
		_groups.Add(0);
	}

	/// <summary>Loads the map block from YAML.</summary>
	public void Load(YamlNodeReader reader)
	{
		reader.TryRead("name", ref _name);
		reader.TryRead("width", ref _size_x);
		reader.TryRead("length", ref _size_y);
		reader.TryRead("height", ref _size_z);
		if ((_size_x % 10) != 0 || (_size_y % 10) != 0)
		{
			throw new InvalidOperationException("Error: MapBlock " + _name + ": Size must be divisible by ten");
		}
		if (reader["groups"] is { IsValid: true } groups)
		{
			_groups.Clear();
			if (groups.IsSeq)
			{
				groups.TryReadVal(ref _groups);
			}
			else
			{
				_groups.Add(groups.ReadVal(0));
			}
		}
		if (reader["revealedFloors"] is { IsValid: true } revealedFloors)
		{
			_revealedFloors.Clear();
			if (revealedFloors.IsSeq)
			{
				revealedFloors.TryReadVal(ref _revealedFloors);
			}
			else
			{
				_revealedFloors.Add(revealedFloors.ReadVal(0));
			}
		}
		reader.TryRead("items", ref _items);
		reader.TryRead("fuseTimers", ref _itemsFuseTimer);
		reader.TryRead("randomizedItems", ref _randomizedItems);
		reader.TryRead("extendedItems", ref _extendedItems);
		reader.TryRead("craftInventoryTile", ref _craftInventoryTile);
	}

	/// <summary>Gets the mapblock's name (used for MAP generation).</summary>
	public string Name => _name;

	/// <summary>Gets the mapblock's x size in tiles.</summary>
	public int SizeX => _size_x;

	/// <summary>Gets the mapblock's y size in tiles.</summary>
	public int SizeY => _size_y;

	/// <summary>Gets/sets the mapblock's z size.</summary>
	public int SizeZ { get => _size_z; set => _size_z = value; }

	/// <summary>Returns if this mapblock is from the group specified.</summary>
	public bool IsInGroup(int group) => _groups.Contains(group);

	/// <summary>Gets if this floor should be revealed or not.</summary>
	public bool IsFloorRevealed(int floor) => _revealedFloors.Contains(floor);

	/// <summary>Gets the items and their positioning for any items associated with this block.</summary>
	public SortedDictionary<string, List<Position>> Items => _items;

	/// <summary>Gets the predefined fuse timers for items on this block.</summary>
	public SortedDictionary<string, (int first, int second)> ItemsFuseTimers => _itemsFuseTimer;

	/// <summary>Gets the to-be-randomized items and their positioning for any items associated with this block.</summary>
	public List<RandomizedItems> RandomizedItems => _randomizedItems;

	/// <summary>Gets the layout for any items that belong in this map block. Extended syntax.</summary>
	public List<ExtendedItems> ExtendedItems => _extendedItems;

	/// <summary>Gets the craft inventory tile position.</summary>
	public List<int> CraftInventoryTile => _craftInventoryTile;
}
