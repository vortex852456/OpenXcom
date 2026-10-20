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
// Ported from: src/Mod/MCDPatch.h, src/Mod/MCDPatch.cpp
#nullable disable

namespace Acom;

/// <summary>
/// An MCD data Patch.
/// </summary>
public class MCDPatch
{
	private readonly List<(int first, int second)> _bigWalls = [], _TUWalks = [], _TUFlys = [], _TUSlides = [], _deathTiles = [], _terrainHeight = [],
		_specialTypes = [], _armors = [], _explosives = [], _flammabilities = [], _fuels = [], _HEBlocks = [], _footstepSounds = [], _objectTypes = [];
	private readonly List<(int first, bool second)> _noFloors = [], _stopLOSses = [];
	private readonly List<(int first, List<int> second)> _LOFTS = [];

	/// <summary>Initializes an MCD Patch.</summary>
	public MCDPatch()
	{
	}

	/// <summary>Loads the MCD Patch from YAML.</summary>
	public void Load(YamlNodeReader reader)
	{
		foreach (var d in reader["data"].Children())
		{
			var mcd = d.UseIndex();
			int MCDIndex = (int)mcd["MCDIndex"].ReadVal<uint>(); // size_t in C++
			if (mcd["bigWall"])
			{
				int bigWall = mcd["bigWall"].ReadVal<int>();
				_bigWalls.Add((MCDIndex, bigWall));
			}
			if (mcd["TUWalk"])
			{
				int TUWalk = mcd["TUWalk"].ReadVal<int>();
				_TUWalks.Add((MCDIndex, TUWalk));
			}
			if (mcd["TUFly"])
			{
				int TUFly = mcd["TUFly"].ReadVal<int>();
				_TUFlys.Add((MCDIndex, TUFly));
			}
			if (mcd["TUSlide"])
			{
				int TUSlide = mcd["TUSlide"].ReadVal<int>();
				_TUSlides.Add((MCDIndex, TUSlide));
			}
			if (mcd["deathTile"])
			{
				int deathTile = mcd["deathTile"].ReadVal<int>();
				_deathTiles.Add((MCDIndex, deathTile));
			}
			if (mcd["terrainHeight"])
			{
				int terrainHeight = mcd["terrainHeight"].ReadVal<int>();
				_terrainHeight.Add((MCDIndex, terrainHeight));
			}
			if (mcd["specialType"])
			{
				int specialType = mcd["specialType"].ReadVal<int>();
				_specialTypes.Add((MCDIndex, specialType));
			}
			if (mcd["explosive"])
			{
				int explosive = mcd["explosive"].ReadVal<int>();
				_explosives.Add((MCDIndex, explosive));
			}
			if (mcd["armor"])
			{
				int armor = mcd["armor"].ReadVal<int>();
				_armors.Add((MCDIndex, armor));
			}
			if (mcd["flammability"])
			{
				int flammability = mcd["flammability"].ReadVal<int>();
				_flammabilities.Add((MCDIndex, flammability));
			}
			if (mcd["fuel"])
			{
				int fuel = mcd["fuel"].ReadVal<int>();
				_fuels.Add((MCDIndex, fuel));
			}
			if (mcd["footstepSound"])
			{
				int footstepSound = mcd["footstepSound"].ReadVal<int>();
				_footstepSounds.Add((MCDIndex, footstepSound));
			}
			if (mcd["HEBlock"])
			{
				int HEBlock = mcd["HEBlock"].ReadVal<int>();
				_HEBlocks.Add((MCDIndex, HEBlock));
			}
			if (mcd["noFloor"])
			{
				bool noFloor = mcd["noFloor"].ReadVal<bool>();
				_noFloors.Add((MCDIndex, noFloor));
			}
			if (mcd["LOFTS"])
			{
				var lofts = mcd["LOFTS"].ReadVal<List<int>>();
				_LOFTS.Add((MCDIndex, lofts));
			}
			if (mcd["stopLOS"])
			{
				bool stopLOS = mcd["stopLOS"].ReadVal<bool>();
				_stopLOSses.Add((MCDIndex, stopLOS));
			}
			if (mcd["objectType"])
			{
				int objectType = mcd["objectType"].ReadVal<int>();
				_objectTypes.Add((MCDIndex, objectType));
			}
		}
	}

	/// <summary>Applies an MCD patch to a mapDataSet.</summary>
	/// <param name="dataSet">The MapDataSet we want to modify.</param>
	public void ModifyData(MapDataSet dataSet)
	{
		foreach (var (first, second) in _bigWalls)
		{
			dataSet.GetObject(first).BigWall = second;
		}
		foreach (var (first, second) in _TUWalks)
		{
			dataSet.GetObject(first).SetTUWalk(second);
		}
		foreach (var (first, second) in _TUFlys)
		{
			dataSet.GetObject(first).SetTUFly(second);
		}
		foreach (var (first, second) in _TUSlides)
		{
			dataSet.GetObject(first).SetTUSlide(second);
		}
		foreach (var (first, second) in _deathTiles)
		{
			dataSet.GetObject(first).DieMCD = second;
		}
		foreach (var (first, second) in _terrainHeight)
		{
			dataSet.GetObject(first).TerrainLevel = second;
		}
		foreach (var (first, second) in _specialTypes)
		{
			dataSet.GetObject(first).SetSpecialType(second, dataSet.GetObject(first).ObjectType);
		}
		foreach (var (first, second) in _explosives)
		{
			dataSet.GetObject(first).Explosive = second;
		}
		foreach (var (first, second) in _armors)
		{
			dataSet.GetObject(first).Armor = second;
		}
		foreach (var (first, second) in _flammabilities)
		{
			dataSet.GetObject(first).Flammable = second;
		}
		foreach (var (first, second) in _fuels)
		{
			dataSet.GetObject(first).Fuel = second;
		}
		foreach (var (first, second) in _HEBlocks)
		{
			dataSet.GetObject(first).SetHEBlock(second);
		}
		foreach (var (first, second) in _footstepSounds)
		{
			dataSet.GetObject(first).FootstepSound = second;
		}
		foreach (var (first, second) in _objectTypes)
		{
			dataSet.GetObject(first).ObjectType = (TilePart)second;
		}
		foreach (var (first, second) in _noFloors)
		{
			dataSet.GetObject(first).SetNoFloor(second);
		}
		foreach (var (first, second) in _stopLOSses)
		{
			dataSet.GetObject(first).SetStopLOS(second);
		}
		foreach (var (first, second) in _LOFTS)
		{
			int layer = 0;
			foreach (int loft in second)
			{
				dataSet.GetObject(first).SetLoftID(loft, layer);
				++layer;
			}
		}
	}
}
