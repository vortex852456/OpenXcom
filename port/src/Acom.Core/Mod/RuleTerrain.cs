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
// Ported from: src/Mod/RuleTerrain.h, src/Mod/RuleTerrain.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents a specific type of Battlescape Terrain.
/// - the names of the objectsets needed in this specific terrain.
/// - the mapblocks that can be used to build this terrain.
/// </summary>
/// <seealso href="http://www.ufopaedia.org/index.php?title=TERRAIN"/>
public class RuleTerrain
{
	private List<MapDataSet> _mapDataSets = [];
	private readonly List<MapBlock> _mapBlocks = [];
	private readonly string _name;
	private string _mapScript;
	private List<string> _mapScripts = [];
	private string _enviroEffects = "";
	private readonly List<string> _civilianTypes = [], _music = [];
	private int _minDepth, _maxDepth, _ambience;
	private double _ambientVolume;
	private readonly List<int> _ambienceRandom = [];
	private int _minAmbienceRandomDelay, _maxAmbienceRandomDelay;
	private int _lastCraftSkinIndex;

	/// <summary>RuleTerrain construction.</summary>
	public RuleTerrain(string name)
	{
		_name = name;
		_mapScript = "DEFAULT";
		_minDepth = 0;
		_maxDepth = 0;
		_ambience = -1;
		_ambientVolume = 0.5;
		_minAmbienceRandomDelay = 20;
		_maxAmbienceRandomDelay = 60;
		_lastCraftSkinIndex = 0;
		_civilianTypes.Add("MALE_CIVILIAN");
		_civilianTypes.Add("FEMALE_CIVILIAN");
	}

	/// <summary>Loads the terrain from YAML.</summary>
	/// <param name="node">YAML node.</param>
	/// <param name="mod">Mod for the terrain.</param>
	public void Load(YamlNodeReader node, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		var reader = node.UseIndex();
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent, mod);
		}

		bool adding = reader["addOnly"].ReadVal(false);
		var mapDataSets = reader["mapDataSets"];
		if (mapDataSets)
		{
			_mapDataSets.Clear();
			foreach (var mapDataSet in mapDataSets.Children())
			{
				_mapDataSets.Add(mod.GetMapDataSet(mapDataSet.ReadVal<string>()));
			}
		}
		var mapBlocks = reader["mapBlocks"];
		if (mapBlocks)
		{
			if (!adding)
			{
				_mapBlocks.Clear();
			}
			foreach (var mapBlockReader in mapBlocks.Children())
			{
				var mapBlock = new MapBlock(mapBlockReader["name"].ReadVal<string>());
				mapBlock.Load(mapBlockReader);
				_mapBlocks.Add(mapBlock);
			}
		}

		reader.TryRead("enviroEffects", ref _enviroEffects);
		mod.LoadUnorderedNames(_name, _civilianTypes, reader["civilianTypes"]);
		mod.LoadUnorderedNames(_name, _music, reader["music"]);
		if (reader["depth"])
		{
			reader["depth"][0].TryReadVal(ref _minDepth);
			reader["depth"][1].TryReadVal(ref _maxDepth);
		}
		mod.LoadSoundOffset(_name, ref _ambience, reader["ambience"], "BATTLE.CAT");
		reader.TryRead("ambientVolume", ref _ambientVolume);
		mod.LoadSoundOffset(_name, _ambienceRandom, reader["ambienceRandom"], "BATTLE.CAT");
		if (reader["ambienceRandomDelay"])
		{
			reader["ambienceRandomDelay"][0].TryReadVal(ref _minAmbienceRandomDelay);
			reader["ambienceRandomDelay"][1].TryReadVal(ref _maxAmbienceRandomDelay);
		}
		reader.TryRead("script", ref _mapScript);
		reader.TryRead("mapScripts", ref _mapScripts);
	}

	/// <summary>Gets the terrain's name (used for MAP generation).</summary>
	public string Name => _name;

	/// <summary>Returns the enviro effects name for this terrain.</summary>
	public string EnviroEffects => _enviroEffects;

	/// <summary>Gets the array of mapblocks.</summary>
	public List<MapBlock> MapBlocks => _mapBlocks;

	/// <summary>Gets the array of mapdatafiles.</summary>
	public List<MapDataSet> MapDataSets => _mapDataSets;

	/// <summary>Refreshes the terrain's mapdatafiles. Use for craft skins ONLY!</summary>
	public void RefreshMapDataSets(int craftSkinIndex, Mod mod)
	{
		if (_lastCraftSkinIndex == craftSkinIndex)
		{
			return;
		}
		ArgumentNullException.ThrowIfNull(mod); // C++ only touches the mod after the early return

		var newNames = new List<string>();
		foreach (var item in _mapDataSets)
		{
			if (item.Name == "BLANKS")
			{
				newNames.Add(item.Name);
			}
			else if (_lastCraftSkinIndex == 0)
			{
				newNames.Add(item.Name + "_" + craftSkinIndex);
			}
			else
			{
				// std::string::substr(0, npos) keeps the whole name when there is no '_'
				int lastPos = item.Name.LastIndexOf('_');
				string stripped = lastPos < 0 ? item.Name : item.Name[..lastPos];
				if (craftSkinIndex > 0)
				{
					newNames.Add(stripped + "_" + craftSkinIndex);
				}
				else
				{
					newNames.Add(stripped);
				}
			}
		}
		_mapDataSets.Clear();
		foreach (var newName in newNames)
		{
			_mapDataSets.Add(mod.GetMapDataSet(newName));
		}
		_lastCraftSkinIndex = craftSkinIndex;
	}

	/// <summary>Gets a random mapblock within the given constraints.</summary>
	/// <param name="maxSizeX">The maximum X size of the mapblock (10 or 20 or 999 - don't care).</param>
	/// <param name="maxSizeY">The maximum Y size of the mapblock.</param>
	/// <param name="group">Whether this must be a block of a certain group.</param>
	/// <param name="force">Whether to enforce the max size.</param>
	/// <returns>The mapblock, or null if none fits.</returns>
	public MapBlock GetRandomMapBlock(int maxSizeX, int maxSizeY, int group, bool force = true)
	{
		var compliantMapBlocks = new List<MapBlock>();

		foreach (var mapblock in _mapBlocks)
		{
			if ((mapblock.SizeX == maxSizeX ||
				(!force && mapblock.SizeX < maxSizeX)) &&
				(mapblock.SizeY == maxSizeY ||
				(!force && mapblock.SizeY < maxSizeY)) &&
				mapblock.IsInGroup(group))
			{
				compliantMapBlocks.Add(mapblock);
			}
		}

		if (compliantMapBlocks.Count == 0) return null;

		int n = Rng.Generate(0, compliantMapBlocks.Count - 1);

		return compliantMapBlocks[n];
	}

	/// <summary>Gets a mapblock with a given name (or null).</summary>
	public MapBlock GetMapBlock(string name)
	{
		foreach (var mapblock in _mapBlocks)
		{
			if (mapblock.Name == name)
				return mapblock;
		}
		return null;
	}

	/// <summary>Gets a mapdata object.</summary>
	/// <param name="id">The id in the terrain; becomes the id in the map data set.</param>
	/// <param name="mapDataSetID">The id of the map data set (incremented).</param>
	public MapData GetMapData(ref uint id, ref int mapDataSetID)
	{
		MapDataSet mdf = null;
		int iter = 0;
		for (; iter != _mapDataSets.Count; ++iter)
		{
			mdf = _mapDataSets[iter];
			if (id < (uint)mdf.Size)
			{
				break;
			}
			id -= (uint)mdf.Size;
			mapDataSetID++;
		}
		if (iter == _mapDataSets.Count)
		{
			// oops! someone at microprose made an error in the map!
			// set this broken tile reference to BLANKS 0.
			mdf = _mapDataSets[0];
			id = 0;
			mapDataSetID = 0;
		}
		return mdf.GetObject((int)id);
	}

	/// <summary>Gets the list of civilian types to use on this terrain (default MALE_CIVILIAN and FEMALE_CIVILIAN), as a copy.</summary>
	public List<string> CivilianTypes => [.. _civilianTypes];

	/// <summary>Gets the min depth.</summary>
	public int MinDepth => _minDepth;

	/// <summary>Gets the max depth.</summary>
	public int MaxDepth => _maxDepth;

	/// <summary>Gets the ambient sound effect.</summary>
	public int Ambience => _ambience;

	/// <summary>Gets the random ambient sound effects.</summary>
	public List<int> AmbienceRandom => _ambienceRandom;

	/// <summary>Gets the minimum delay for the random ambient sound effect.</summary>
	public int MinAmbienceRandomDelay => _minAmbienceRandomDelay;

	/// <summary>Gets the maximum delay for the random ambient sound effect.</summary>
	public int MaxAmbienceRandomDelay => _maxAmbienceRandomDelay;

	/// <summary>Gets the generation script name (picks one of the map scripts at random if defined).</summary>
	public string RandomMapScript
	{
		get
		{
			if (_mapScripts.Count != 0)
			{
				int pick = Rng.Generate(0, _mapScripts.Count - 1);
				return _mapScripts[pick];
			}
			return _mapScript;
		}
	}

	/// <summary>Does any map script use globe terrain?</summary>
	/// <returns>1 = yes, 0 = no, -1 = no map script found.</returns>
	public int HasTextureBasedScript(Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		int ret = -1;
		// iterate _mapScripts
		foreach (string script in _mapScripts)
		{
			var vec = mod.GetMapScript(script);
			if (vec != null)
			{
				ret = 0;
				if (UsesTextureBasedTerrain(vec))
				{
					return 1;
				}
			}
		}
		// check also _mapScript
		{
			var vec = mod.GetMapScript(_mapScript);
			if (vec != null)
			{
				ret = 0;
				if (UsesTextureBasedTerrain(vec))
				{
					return 1;
				}
			}
		}
		return ret;
	}

	/// <summary>Checks the commands' terrains and vertical level terrains for "globeTerrain"/"baseTerrain".</summary>
	internal static bool UsesTextureBasedTerrain(List<MapScript> commands)
	{
		// iterate map script commands
		foreach (var ms in commands)
		{
			// iterate terrains
			foreach (string terrain in ms.RandomAlternateTerrain)
			{
				if (terrain == "globeTerrain" || terrain == "baseTerrain")
				{
					return true;
				}
			}
			// iterate vertical levels
			foreach (var vlevel in ms.VerticalLevels)
			{
				if (vlevel.levelTerrain == "globeTerrain" || vlevel.levelTerrain == "baseTerrain")
				{
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>Gets the list of music tracks this terrain has to choose from.</summary>
	public List<string> Music => _music;

	/// <summary>Gets the ambient volume.</summary>
	public double AmbientVolume => _ambientVolume;
}
