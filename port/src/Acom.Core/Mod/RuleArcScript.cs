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
// Ported from: src/Mod/RuleArcScript.h, src/Mod/RuleArcScript.cpp
#nullable disable

namespace Acom;

/// <summary>
/// The (optional) rules for the high-level game progression.
/// Each script element is independent, and the saved game will probe the list of these each month to determine what's going to happen.
/// Arc scripts are executed just before the mission scripts and unlock research topics that can then be used by mission scripts.
/// </summary>
public class RuleArcScript
{
	private readonly string _type;
	private List<string> _sequentialArcs = [];
	private readonly WeightedOptions _randomArcs = new();
	private int _firstMonth, _lastMonth, _executionOdds, _maxArcs, _minDifficulty, _maxDifficulty;
	private int _minScore, _maxScore;
	private long _minFunds, _maxFunds;
	private string _missionVarName = "", _missionMarkerName = "";
	private int _counterMin, _counterMax;
	private SortedDictionary<string, bool> _researchTriggers = new(StringComparer.Ordinal);
	private SortedDictionary<string, bool> _itemTriggers = new(StringComparer.Ordinal);
	private SortedDictionary<string, bool> _facilityTriggers = new(StringComparer.Ordinal);
	private SortedDictionary<string, bool> _soldierTypeTriggers = new(StringComparer.Ordinal);
	private SortedDictionary<string, bool> _xcomBaseInRegionTriggers = new(StringComparer.Ordinal);
	private SortedDictionary<string, bool> _xcomBaseInCountryTriggers = new(StringComparer.Ordinal);
	private SortedDictionary<string, bool> _pactCountryTriggers = new(StringComparer.Ordinal);

	/// <summary>Creates a new arc script.</summary>
	public RuleArcScript(string type)
	{
		_type = type;
		_firstMonth = 0;
		_lastMonth = -1;
		_executionOdds = 100;
		_maxArcs = -1;
		_minDifficulty = 0;
		_maxDifficulty = 4;
		_minScore = int.MinValue;
		_maxScore = int.MaxValue;
		_minFunds = long.MinValue;
		_maxFunds = long.MaxValue;
		// not initialized in C++ (indeterminate unless loaded); same defaults as RuleEventScript
		_counterMin = 0;
		_counterMax = -1;
	}

	/// <summary>Loads an arcScript from YAML.</summary>
	/// <param name="node">The node within the file we're reading.</param>
	public void Load(YamlNodeReader node)
	{
		var reader = node.UseIndex();
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent);
		}

		reader.TryRead("sequentialArcs", ref _sequentialArcs);
		if (reader["randomArcs"])
		{
			_randomArcs.Load(reader["randomArcs"]);
		}
		reader.TryRead("firstMonth", ref _firstMonth);
		reader.TryRead("lastMonth", ref _lastMonth);
		reader.TryRead("executionOdds", ref _executionOdds);
		reader.TryRead("maxArcs", ref _maxArcs);
		reader.TryRead("minDifficulty", ref _minDifficulty);
		reader.TryRead("maxDifficulty", ref _maxDifficulty);
		reader.TryRead("minScore", ref _minScore);
		reader.TryRead("maxScore", ref _maxScore);
		reader.TryRead("minFunds", ref _minFunds);
		reader.TryRead("maxFunds", ref _maxFunds);
		reader.TryRead("missionVarName", ref _missionVarName);
		reader.TryRead("missionMarkerName", ref _missionMarkerName);
		reader.TryRead("counterMin", ref _counterMin);
		reader.TryRead("counterMax", ref _counterMax);
		reader.TryRead("researchTriggers", ref _researchTriggers);
		reader.TryRead("itemTriggers", ref _itemTriggers);
		reader.TryRead("facilityTriggers", ref _facilityTriggers);
		reader.TryRead("soldierTypeTriggers", ref _soldierTypeTriggers);
		reader.TryRead("xcomBaseInRegionTriggers", ref _xcomBaseInRegionTriggers);
		reader.TryRead("xcomBaseInCountryTriggers", ref _xcomBaseInCountryTriggers);
		reader.TryRead("pactCountryTriggers", ref _pactCountryTriggers);
	}

	/// <summary>Gets the name of the script command.</summary>
	public string Type => _type;

	/// <summary>Gets the sequential arcs list.</summary>
	public List<string> SequentialArcs => _sequentialArcs;

	/// <summary>Gets the random arcs weighted list.</summary>
	public WeightedOptions RandomArcs => _randomArcs;

	/// <summary>Gets the first month this command will run.</summary>
	public int FirstMonth => _firstMonth;

	/// <summary>Gets the last month this command will run.</summary>
	public int LastMonth => _lastMonth;

	/// <summary>Gets the odds of this command executing.</summary>
	public int ExecutionOdds => _executionOdds;

	/// <summary>Gets the maximum number of arcs this command can generate (during the whole game).</summary>
	public int MaxArcs => _maxArcs;

	/// <summary>Gets the minimum difficulty for this command to run.</summary>
	public int MinDifficulty => _minDifficulty;

	/// <summary>Gets the maximum difficulty for this command to run.</summary>
	public int MaxDifficulty => _maxDifficulty;

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
}
