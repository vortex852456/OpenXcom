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
// Ported from: src/Mod/RuleEvent.h, src/Mod/RuleEvent.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents a custom Geoscape event.
/// Events are spawned using Event Script ruleset.
/// </summary>
public class RuleEvent
{
	private readonly string _name;
	private string _description = "", _background, _music = "", _cutscene = "";
	private List<string> _regionList = [];
	private bool _alignBottom;
	private bool _city;
	private int _points, _funds;
	private string _spawnedCraftType = "";
	private int _spawnedPersons;
	private string _spawnedPersonType = "", _spawnedPersonName = "";
	private string _spawnedSoldier = "";
	private SortedDictionary<string, int> _everyMultiItemList = new(StringComparer.Ordinal);
	private List<string> _everyItemList = [], _randomItemList = [];
	private List<SortedDictionary<string, int>> _randomMultiItemList = [];
	private readonly WeightedOptions _weightedItemList = new();
	private List<string> _researchNames = [];
	private readonly List<RuleResearch> _research = [];
	private List<string> _adhocMissionScriptTags = [];
	private string _interruptResearch = "";
	private int _timer, _timerRandom;
	private bool _invert;
	private SortedDictionary<string, int> _everyMultiSoldierList = new(StringComparer.Ordinal);
	private List<SortedDictionary<string, int>> _randomMultiSoldierList = [];

	/// <summary>Creates a blank RuleEvent.</summary>
	public RuleEvent(string name)
	{
		_name = name;
		_background = "BACK13.SCR";
		_alignBottom = false;
		_city = false;
		_points = 0;
		_funds = 0;
		_spawnedPersons = 0;
		_timer = 30;
		_timerRandom = 0;
		_invert = false;
	}

	/// <summary>Loads the event definition from YAML.</summary>
	public void Load(YamlNodeReader node)
	{
		var reader = node.UseIndex();
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent);
		}

		reader.TryRead("description", ref _description);
		reader.TryRead("alignBottom", ref _alignBottom);
		reader.TryRead("background", ref _background);
		reader.TryRead("music", ref _music);
		reader.TryRead("cutscene", ref _cutscene);
		reader.TryRead("regionList", ref _regionList);
		reader.TryRead("city", ref _city);
		reader.TryRead("points", ref _points);
		reader.TryRead("funds", ref _funds);
		reader.TryRead("spawnedCraftType", ref _spawnedCraftType);
		reader.TryRead("spawnedPersons", ref _spawnedPersons);
		reader.TryRead("spawnedPersonType", ref _spawnedPersonType);
		reader.TryRead("spawnedPersonName", ref _spawnedPersonName);
		if (reader["spawnedSoldier"])
		{
			_spawnedSoldier = reader["spawnedSoldier"].EmitDescendants(new YamlRootNodeReader(_spawnedSoldier, "(spawned soldier template)").ToBase());
		}
		reader.TryRead("everyMultiItemList", ref _everyMultiItemList);
		reader.TryRead("everyItemList", ref _everyItemList);
		reader.TryRead("randomItemList", ref _randomItemList);
		reader.TryRead("randomMultiItemList", ref _randomMultiItemList);
		if (reader["weightedItemList"])
		{
			_weightedItemList.Load(reader["weightedItemList"]);
		}
		reader.TryRead("researchList", ref _researchNames);
		reader.TryRead("adhocMissionScriptTags", ref _adhocMissionScriptTags);
		reader.TryRead("interruptResearch", ref _interruptResearch);
		reader.TryRead("timer", ref _timer);
		reader.TryRead("timerRandom", ref _timerRandom);
		reader.TryRead("invert", ref _invert);
		reader.TryRead("everyMultiSoldierList", ref _everyMultiSoldierList);
		reader.TryRead("randomMultiSoldierList", ref _randomMultiSoldierList);
	}

	/// <summary>Cross link with other rules.</summary>
	public void AfterLoad(Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		// mod->linkRule(_research, _researchNames)
		foreach (var n in _researchNames)
		{
			_research.Add(mod.GetResearch(n, true));
		}
		_researchNames = [];
	}

	/// <summary>Gets the event's name.</summary>
	public string Name => _name;

	/// <summary>Gets the event's description.</summary>
	public string Description => _description;

	/// <summary>Should the description be bottom aligned?</summary>
	public bool AlignBottom() => _alignBottom;

	/// <summary>Gets the event's background sprite name.</summary>
	public string Background => _background;

	/// <summary>Gets the event's music.</summary>
	public string Music => _music;

	/// <summary>Gets the cutscene to play when the event dialog is closed.</summary>
	public string Cutscene => _cutscene;

	/// <summary>Gets a list of regions where this event can occur.</summary>
	public List<string> RegionList => _regionList;

	/// <summary>Is this event city specific?</summary>
	public bool IsCitySpecific => _city;

	/// <summary>Gets the amount of score points awarded when this event pops up.</summary>
	public int Points => _points;

	/// <summary>Gets the amount of funds awarded when this event pops up.</summary>
	public int Funds => _funds;

	/// <summary>Gets the spawned craft type.</summary>
	public string SpawnedCraftType => _spawnedCraftType;

	/// <summary>Gets the number of spawned persons.</summary>
	public int SpawnedPersons => _spawnedPersons;

	/// <summary>Gets the spawned person type.</summary>
	public string SpawnedPersonType => _spawnedPersonType;

	/// <summary>Gets the custom name of the spawned person.</summary>
	public string SpawnedPersonName => _spawnedPersonName;

	/// <summary>Gets the spawned soldier template (YAML text).</summary>
	public string SpawnedSoldierTemplate => _spawnedSoldier;

	/// <summary>Gets a list of items; they are all transferred to HQ stores when this event pops up.</summary>
	public SortedDictionary<string, int> EveryMultiItemList => _everyMultiItemList;

	/// <summary>Gets a list of items; they are all transferred to HQ stores when this event pops up.</summary>
	public List<string> EveryItemList => _everyItemList;

	/// <summary>Gets a list of items; one of them is randomly selected and transferred to HQ stores when this event pops up.</summary>
	public List<string> RandomItemList => _randomItemList;

	/// <summary>Gets a list of lists of items; one of them is randomly selected and transferred to HQ stores when this event pops up.</summary>
	public List<SortedDictionary<string, int>> RandomMultiItemList => _randomMultiItemList;

	/// <summary>Gets a list of items; one of them is randomly selected (considering weights) and transferred to HQ stores when this event pops up.</summary>
	public WeightedOptions WeightedItemList => _weightedItemList;

	/// <summary>Gets a list of research projects; one of them will be randomly discovered when this event pops up.</summary>
	public List<RuleResearch> ResearchList => _research;

	/// <summary>Gets a list of adhoc script tags; used for adhoc alien mission generation.</summary>
	public List<string> AdhocMissionScriptTags => _adhocMissionScriptTags;

	/// <summary>Gets the research project that will interrupt/terminate an already generated (but not yet popped up) event.</summary>
	public string InterruptResearch => _interruptResearch;

	/// <summary>Gets the timer of delay for this event, for it occurring after being spawned with eventScripts ruleset.</summary>
	public int Timer => _timer;

	/// <summary>Gets value for calculation of random part of delay for this event.</summary>
	public int TimerRandom => _timerRandom;

	/// <summary>Should the event remove items instead of adding them?</summary>
	public bool Invert => _invert;

	/// <summary>Gets a list of soldiers; they are all transferred to HQ when this event pops up.</summary>
	public SortedDictionary<string, int> EveryMultiSoldierList => _everyMultiSoldierList;

	/// <summary>Gets a list of lists of soldiers; one of them is randomly selected and transferred to HQ when this event pops up.</summary>
	public List<SortedDictionary<string, int>> RandomMultiSoldierList => _randomMultiSoldierList;
}
