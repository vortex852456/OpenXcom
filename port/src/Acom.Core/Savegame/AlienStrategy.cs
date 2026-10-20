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
// Ported from: src/Savegame/AlienStrategy.h, src/Savegame/AlienStrategy.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Stores the information about alien strategy.
/// </summary>
public class AlienStrategy
{
	/// <summary>The chances of each region to be targeted for a mission.</summary>
	private readonly WeightedOptions _regionChances = new();
	/// <summary>The chances of each mission type for each region.</summary>
	private readonly SortedDictionary<string, WeightedOptions> _regionMissions = new(StringComparer.Ordinal);
	private SortedDictionary<string, int> _missionRuns = new(StringComparer.Ordinal);
	private SortedDictionary<string, List<(string first, int second)>> _missionLocations = new(StringComparer.Ordinal);

	/// <summary>
	/// Create an AlienStrategy with no values.
	/// Running a game like this will most likely crash.
	/// </summary>
	public AlienStrategy()
	{
		// Empty by design!
	}

	/// <summary>Get starting values from the rules.</summary>
	/// <param name="mod">Pointer to the game mod.</param>
	public void Init(Mod mod)
	{
		foreach (var regionName in mod.RegionsList)
		{
			RuleRegion region = mod.GetRegion(regionName, true);
			_regionChances.Set(regionName, region.Weight);
			var missions = new WeightedOptions(region.AvailableMissions);
			_regionMissions.TryAdd(regionName, missions);
		}
	}

	/// <summary>Loads the data from a YAML file.</summary>
	/// <param name="reader">YAML node.</param>
	/// <param name="mod">The game mod, used to validate the regions.</param>
	public void Load(YamlNodeReader reader, Mod mod)
	{
		_regionMissions.Clear();
		_regionChances.Clear();
		_regionChances.Load(reader["regions"]);

		foreach (var possibleMissionReader in reader["possibleMissions"].Children())
		{
			string region = possibleMissionReader["region"].ReadVal<string>();
			if (mod.GetRegion(region, false) != null)
			{
				var missionOptionsReader = possibleMissionReader["missions"];
				var options = new WeightedOptions();
				options.Load(missionOptionsReader);
				_regionMissions.TryAdd(region, options);
			}
			else
			{
				Logger.Warning($"Corrupted save: Alien strategy contains an invalid region: {region}, skipping...");
			}
		}
		var missionLocations = _missionLocations;
		if (reader.TryRead("missionLocations", ref missionLocations))
			_missionLocations = new(missionLocations, StringComparer.Ordinal);
		var missionRuns = _missionRuns;
		if (reader.TryRead("missionsRun", ref missionRuns))
			_missionRuns = new(missionRuns, StringComparer.Ordinal);
	}

	/// <summary>Saves the alien data to a YAML file.</summary>
	public void Save(YamlNodeWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.SetAsMap();
		_regionChances.Save(writer["regions"]);
		if (_regionMissions.Count != 0)
		{
			var possibleMissionsWriter = writer["possibleMissions"];
			possibleMissionsWriter.SetAsSeq();
			foreach (var pair in _regionMissions)
			{
				var regionWriter = possibleMissionsWriter.Write();
				regionWriter.SetAsMap();
				regionWriter.Write("region", pair.Key);
				pair.Value.Save(regionWriter["missions"]);
			}
		}
		writer.Write("missionLocations", _missionLocations);
		writer.Write("missionsRun", _missionRuns);
	}

	/// <summary>Choose one of the regions for a mission.</summary>
	/// <param name="mod">Pointer to the mod.</param>
	/// <returns>The region id.</returns>
	public string ChooseRandomRegion(Mod mod)
	{
		string chosen = _regionChances.Choose();
		if (chosen.Length == 0)
		{
			// no more missions to choose from: refresh.
			_regionMissions.Clear();
			// re-initialize the list
			Init(mod);
			// now let's try that again.
			chosen = _regionChances.Choose();
		}
		System.Diagnostics.Debug.Assert(chosen.Length != 0);
		return chosen;
	}

	/// <summary>Choose one missions available for <paramref name="region"/>.</summary>
	/// <param name="region">The region id.</param>
	/// <returns>The mission id.</returns>
	public string ChooseRandomMission(string region)
	{
		bool found = _regionMissions.TryGetValue(region, out var missions);
		System.Diagnostics.Debug.Assert(found);
		return missions.Choose();
	}

	/// <summary>Remove <paramref name="mission"/> from the list of possible missions for <paramref name="region"/>.</summary>
	/// <param name="region">The region id.</param>
	/// <param name="mission">The mission id.</param>
	/// <returns>If there are no more regions with missions available.</returns>
	public bool RemoveMission(string region, string mission)
	{
		if (_regionMissions.TryGetValue(region, out var found))
		{
			found.Set(mission, 0);
			if (found.Empty())
			{
				_regionMissions.Remove(region);
				_regionChances.Set(region, 0);
			}
		}
		return _regionMissions.Count == 0;
	}

	/// <summary>Checks the number of missions run labelled as "varName".</summary>
	/// <returns>The number of missions run under the variable name.</returns>
	public int GetMissionsRun(string varName)
	{
		if (_missionRuns.TryGetValue(varName, out int runs))
			return runs;
		return 0;
	}

	/// <summary>Increments the number of missions run labelled as "varName".</summary>
	/// <param name="varName">The variable name that we want to use to keep track of this.</param>
	/// <param name="increment">The value to increment by.</param>
	public void AddMissionRun(string varName, int increment = 1)
	{
		if (string.IsNullOrEmpty(varName))
			return;
		_missionRuns.TryGetValue(varName, out int runs);
		_missionRuns[varName] = runs + increment;
	}

	/// <summary>Adds a mission location to our storage array.</summary>
	/// <param name="varName">The name on the variable under which to store this info.</param>
	/// <param name="regionName">The name of the region we're using.</param>
	/// <param name="zoneNumber">The number of the zone within that region we're using.</param>
	/// <param name="maximum">The maximum size of the list we want to maintain.</param>
	public void AddMissionLocation(string varName, string regionName, int zoneNumber, int maximum)
	{
		if (maximum <= 0) return;
		if (!_missionLocations.TryGetValue(varName, out var locations))
		{
			locations = [];
			_missionLocations.Add(varName, locations);
		}
		locations.Add((regionName, zoneNumber));
		if (locations.Count > maximum)
		{
			// C++ quirk kept: this erases the first entry of the whole map (the alphabetically first
			// variable name), not the oldest location of this variable.
			_missionLocations.Remove(_missionLocations.Keys.First());
		}
	}

	/// <summary>Checks that a given mission location (city or whatever) isn't stored in our list of previously attacked locations.</summary>
	/// <param name="varName">The name of the variable that is storing our data.</param>
	/// <param name="regionName">The name of the region we're looking for.</param>
	/// <param name="zoneNumber">The number in the region that we want to check.</param>
	/// <returns>If the region is valid (meaning it is not in our table).</returns>
	public bool ValidMissionLocation(string varName, string regionName, int zoneNumber)
	{
		if (_missionLocations.TryGetValue(varName, out var locations))
		{
			foreach (var pair in locations)
			{
				if (pair.first == regionName && pair.second == zoneNumber)
					return false;
			}
		}
		return true;
	}

	/// <summary>Checks that a given region appears in our strategy table.</summary>
	/// <param name="regionName">The region we want to check for validity.</param>
	/// <returns>If the region appears in the table or not.</returns>
	public bool ValidMissionRegion(string regionName) => _regionMissions.ContainsKey(regionName);
}
