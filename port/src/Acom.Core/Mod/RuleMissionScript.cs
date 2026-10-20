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
// Ported from: src/Mod/RuleMissionScript.h, src/Mod/RuleMissionScript.cpp
#nullable disable

namespace Acom;

public enum GenerationType : uint
{
	GEN_REGION = 0,
	GEN_MISSION = 1,
	GEN_RACE = 2,
}

/// <summary>
/// The rules for the alien mission progression.
/// Each script element is independent, and the saved game will probe the list of these each month to determine what's going to happen.
/// </summary>
public class RuleMissionScript
{
	private readonly string _type;
	private string _varName = "";
	private int _firstMonth, _lastMonth, _label, _executionOdds, _targetBaseOdds, _minDifficulty, _maxDifficulty, _maxRuns, _avoidRepeats, _delay, _randomDelay;
	private int _minScore, _maxScore;
	private long _minFunds, _maxFunds;
	private string _missionVarName = "", _missionMarkerName = "";
	private int _counterMin, _counterMax;
	private List<int> _conditionals = [];
	private List<string> _adhocMissionScriptTags = [];
	private readonly List<(int first, WeightedOptions second)> _regionWeights = [], _missionWeights = [], _raceWeights = [];
	private SortedDictionary<string, bool> _researchTriggers = new(StringComparer.Ordinal);
	private SortedDictionary<string, bool> _itemTriggers = new(StringComparer.Ordinal);
	private SortedDictionary<string, bool> _facilityTriggers = new(StringComparer.Ordinal);
	private SortedDictionary<string, bool> _soldierTypeTriggers = new(StringComparer.Ordinal);
	private SortedDictionary<string, bool> _xcomBaseInRegionTriggers = new(StringComparer.Ordinal);
	private SortedDictionary<string, bool> _xcomBaseInCountryTriggers = new(StringComparer.Ordinal);
	private SortedDictionary<string, bool> _pactCountryTriggers = new(StringComparer.Ordinal);
	private bool _useTable, _siteType;

	/// <summary>Creates a new mission script.</summary>
	public RuleMissionScript(string type)
	{
		_type = type;
		_firstMonth = 0;
		_lastMonth = -1;
		_label = 0;
		_executionOdds = 100;
		_targetBaseOdds = 0;
		_minDifficulty = 0;
		_maxDifficulty = 4;
		_maxRuns = -1;
		_avoidRepeats = 0;
		_delay = 0;
		_randomDelay = 0;
		_minScore = int.MinValue;
		_maxScore = int.MaxValue;
		_minFunds = long.MinValue;
		_maxFunds = long.MaxValue;
		// not initialized in C++ (indeterminate unless loaded); same defaults as RuleEventScript
		_counterMin = 0;
		_counterMax = -1;
		_useTable = true;
		_siteType = false;
	}

	/// <summary>Loads a missionScript from YAML.</summary>
	/// <param name="node">The node within the file we're reading.</param>
	public void Load(YamlNodeReader node)
	{
		var reader = node.UseIndex();
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent);
		}

		reader.TryRead("varName", ref _varName);
		reader.TryRead("firstMonth", ref _firstMonth);
		reader.TryRead("lastMonth", ref _lastMonth);
		reader.TryRead("label", ref _label);
		reader.TryRead("executionOdds", ref _executionOdds);
		reader.TryRead("targetBaseOdds", ref _targetBaseOdds);
		reader.TryRead("minDifficulty", ref _minDifficulty);
		reader.TryRead("maxDifficulty", ref _maxDifficulty);
		reader.TryRead("maxRuns", ref _maxRuns);
		reader.TryRead("avoidRepeats", ref _avoidRepeats);
		reader.TryRead("startDelay", ref _delay);
		reader.TryRead("randomDelay", ref _randomDelay);
		reader.TryRead("minScore", ref _minScore);
		reader.TryRead("maxScore", ref _maxScore);
		reader.TryRead("minFunds", ref _minFunds);
		reader.TryRead("maxFunds", ref _maxFunds);
		reader.TryRead("missionVarName", ref _missionVarName);
		reader.TryRead("missionMarkerName", ref _missionMarkerName);
		reader.TryRead("counterMin", ref _counterMin);
		reader.TryRead("counterMax", ref _counterMax);
		reader.TryRead("conditionals", ref _conditionals);
		reader.TryRead("adhocMissionScriptTags", ref _adhocMissionScriptTags);
		foreach (var monthWeights in reader["missionWeights"].Children())
		{
			var nw = new WeightedOptions();
			nw.Load(monthWeights);
			_missionWeights.Add((monthWeights.ReadKey(0), nw));
		}
		foreach (var monthWeights in reader["raceWeights"].Children())
		{
			var nw = new WeightedOptions();
			nw.Load(monthWeights);
			_raceWeights.Add((monthWeights.ReadKey(0), nw));
		}
		foreach (var monthWeights in reader["regionWeights"].Children())
		{
			var nw = new WeightedOptions();
			nw.Load(monthWeights);
			_regionWeights.Add((monthWeights.ReadKey(0), nw));
		}

		reader.TryRead("researchTriggers", ref _researchTriggers);
		reader.TryRead("itemTriggers", ref _itemTriggers);
		reader.TryRead("facilityTriggers", ref _facilityTriggers);
		reader.TryRead("soldierTypeTriggers", ref _soldierTypeTriggers);
		reader.TryRead("xcomBaseInRegionTriggers", ref _xcomBaseInRegionTriggers);
		reader.TryRead("xcomBaseInCountryTriggers", ref _xcomBaseInCountryTriggers);
		reader.TryRead("pactCountryTriggers", ref _pactCountryTriggers);

		reader.TryRead("useTable", ref _useTable);
		if (_varName.Length == 0 && (_maxRuns > 0 || _avoidRepeats > 0))
		{
			throw new InvalidOperationException("Error in mission script: " + _type + ": no varName provided for a script with maxRuns or repeatAvoidance.");
		}
	}

	/// <summary>Gets the name of this command.</summary>
	public string Type => _type;

	/// <summary>Gets the name of the variable we want to use to track in the saved game.</summary>
	public string VarName => _varName;

	/// <summary>Gets a complete, unique list of all the mission types this command could possibly generate.</summary>
	public SortedSet<string> AllMissionTypes
	{
		get
		{
			var types = new SortedSet<string>(StringComparer.Ordinal);
			foreach (var (_, second) in _missionWeights)
			{
				var names = second.Names;
				foreach (var name in names)
				{
					types.Add(name);
				}
			}
			return types;
		}
	}

	/// <summary>Gets the list of the regions we have to pick from in the given month.</summary>
	public List<string> GetRegions(int month) => GetNames(_regionWeights, month);

	/// <summary>Gets the list of the possible missions for the given month.</summary>
	public List<string> GetMissionTypes(int month) => GetNames(_missionWeights, month);

	/// <summary>The names of the newest weights not newer than <paramref name="month"/> (the oldest weights as fallback).</summary>
	private static List<string> GetNames(List<(int first, WeightedOptions second)> weights, int month)
	{
		var result = new List<string>();
		int rw = weights.Count - 1; // rbegin (dereferencing it on an empty list is UB in C++, throws here)
		while (month < weights[rw].first)
		{
			--rw;
			if (rw < 0)
			{
				++rw;
				break;
			}
		}
		var names = weights[rw].second.Names;
		foreach (var name in names)
		{
			result.Add(name);
		}
		return result;
	}

	/// <summary>Gets the first month this script should run.</summary>
	public int FirstMonth => _firstMonth;

	/// <summary>Gets the last month this script should run.</summary>
	public int LastMonth => _lastMonth;

	/// <summary>Gets the label this command uses for conditional tracking.</summary>
	public int Label => _label;

	/// <summary>Gets the odds of this command's execution.</summary>
	public int ExecutionOdds => _executionOdds;

	/// <summary>Gets the odds of this command targetting a base.</summary>
	public int TargetBaseOdds => _targetBaseOdds;

	/// <summary>Gets the minimum difficulty for this script to run.</summary>
	public int MinDifficulty => _minDifficulty;

	/// <summary>Gets the maximum difficulty for this command to run.</summary>
	public int MaxDifficulty => _maxDifficulty;

	/// <summary>Gets the maximum runs for scripts tracking our varName.</summary>
	public int MaxRuns => _maxRuns;

	/// <summary>Gets the number of sites to avoid repeating missions against.</summary>
	public int RepeatAvoidance => _avoidRepeats;

	/// <summary>
	/// Gets the fixed (or randomized) delay on spawning the first wave (if any) to override whatever's written in the mission definition.
	/// Note: calls the RNG when a random delay is defined.
	/// </summary>
	public int Delay
	{
		get
		{
			if (_randomDelay == 0)
				return _delay;
			else
				return _delay + Rng.Generate(0, _randomDelay);
		}
	}

	/// <summary>Gets the minimum score (from last month) for this command to run.</summary>
	public int MinScore => _minScore;

	/// <summary>Gets the maximum score (from last month) for this command to run.</summary>
	public int MaxScore => _maxScore;

	/// <summary>Gets the minimum funds (from current month) for this command to run.</summary>
	public long MinFunds => _minFunds;

	/// <summary>Gets the maximum funds (from current month) for this command to run.</summary>
	public long MaxFunds => _maxFunds;

	/// <summary>Gets the name of the mission script tracking variable.</summary>
	public string MissionVarName => _missionVarName;

	/// <summary>Gets the name of the mission marker tracking variable.</summary>
	public string MissionMarkerName => _missionMarkerName;

	/// <summary>Gets the minimum number of missions generated for this command to run.</summary>
	public int CounterMin => _counterMin;

	/// <summary>Gets the maximum number of missions generated for this command to run.</summary>
	public int CounterMax => _counterMax;

	/// <summary>Gets the list of conditions that govern execution of this command.</summary>
	public List<int> Conditionals => _conditionals;

	/// <summary>Gets the list of tags for this command.</summary>
	public List<string> AdhocMissionScriptTags => _adhocMissionScriptTags;

	/// <summary>Does this command use a weighted distribution to pick a race?</summary>
	public bool HasRaceWeights => _raceWeights.Count != 0;

	/// <summary>Does this command use a weighted distribution to pick a mission?</summary>
	public bool HasMissionWeights => _missionWeights.Count != 0;

	/// <summary>Does this command use a weighted distribution to pick a region?</summary>
	public bool HasRegionWeights => _regionWeights.Count != 0;

	/// <summary>Gets the research triggers that may apply to this command.</summary>
	public SortedDictionary<string, bool> ResearchTriggers => _researchTriggers;

	/// <summary>Gets the item triggers that may apply to this command.</summary>
	public SortedDictionary<string, bool> ItemTriggers => _itemTriggers;

	/// <summary>Gets the facility triggers that may apply to this command.</summary>
	public SortedDictionary<string, bool> FacilityTriggers => _facilityTriggers;

	/// <summary>Gets the soldier type triggers that may apply to this command.</summary>
	public SortedDictionary<string, bool> SoldierTypeTriggers => _soldierTypeTriggers;

	/// <summary>Gets the xcom base (in region) triggers that may apply to this command.</summary>
	public SortedDictionary<string, bool> XcomBaseInRegionTriggers => _xcomBaseInRegionTriggers;

	/// <summary>Gets the xcom base (in country) triggers that may apply to this command.</summary>
	public SortedDictionary<string, bool> XcomBaseInCountryTriggers => _xcomBaseInCountryTriggers;

	/// <summary>Gets the country pact triggers that may apply to this command.</summary>
	public SortedDictionary<string, bool> PactCountryTriggers => _pactCountryTriggers;

	/// <summary>
	/// Delete this mission from the table? stops it coming up again in random selection,
	/// but NOT if a missionScript calls it by name.
	/// </summary>
	public bool UseTable => _useTable;

	/// <summary>Gets/sets whether this is a mission site (terror-type) command.</summary>
	public bool SiteType { get => _siteType; set => _siteType = value; }

	/// <summary>Chooses one of the available races, regions, or missions for this command.</summary>
	/// <param name="monthsPassed">The number of months that have passed in the game world.</param>
	/// <param name="type">The type of thing we want to generate, region, mission or race.</param>
	/// <returns>The string id of the thing.</returns>
	public string Generate(int monthsPassed, GenerationType type)
	{
		List<(int first, WeightedOptions second)> weights;
		if (type == GenerationType.GEN_RACE)
			weights = _raceWeights;
		else if (type == GenerationType.GEN_REGION)
			weights = _regionWeights;
		else
			weights = _missionWeights;
		// reverse iteration like the C++ const_reverse_iterator (running past the start is UB there, throws here)
		int rw = weights.Count - 1;
		while (monthsPassed < weights[rw].first)
			--rw;
		return weights[rw].second.Choose();
	}
}
