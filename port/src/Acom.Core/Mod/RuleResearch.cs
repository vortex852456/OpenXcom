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
// Ported from: src/Mod/RuleResearch.h, src/Mod/RuleResearch.cpp
// Port status: partial (missing: script hooks)
#nullable disable

namespace Acom;

/// <summary>
/// Represents one research project.
/// Dependency is the list of RuleResearchs which must be discovered before a RuleResearch became available.
/// Unlocks are used to immediately unlock a RuleResearch (even if not all the dependencies have been researched).
/// </summary>
/// <remarks>
/// Fake ResearchProjects: A RuleResearch is fake one, if its cost is 0. They are used to to create check points in the dependency tree.
/// For example, if we have a Research E which needs either A &amp; B or C &amp; D, we create two fake research projects:
/// F which need A &amp; B and G which need C &amp; D; both F and G can unlock E.
/// </remarks>
public partial class RuleResearch
{
	/// <summary>Name of class used in script.</summary>
	public const string ScriptName = "RuleResearch";

	public const int RESEARCH_STATUS_NEW = 0;
	public const int RESEARCH_STATUS_NORMAL = 1;
	public const int RESEARCH_STATUS_DISABLED = 2;
	public const int RESEARCH_STATUS_HIDDEN = 3;

	private readonly string _name;
	private string _lookupName = "", _cutscene = "", _spawnedItem = "", _spawnedEvent = "";
	private int _spawnedItemCount;
	private readonly List<string> _spawnedItemList = [];
	private readonly List<string> _decreaseCounter = [], _increaseCounter = [];
	private int _cost, _points;
	private readonly List<string> _dependenciesName = [], _unlocksName = [], _disablesName = [], _reenablesName = [], _getOneFreeName = [], _requiresName = [];
	private RuleBaseFacilityFunctions _requiresBaseFunc;
	private List<RuleResearch> _dependencies = [], _unlocks = [], _disables = [], _reenables = [], _getOneFree = [], _requires = [];
	private bool _sequentialGetOneFree;
	private readonly List<(string first, List<string> second)> _getOneFreeProtectedName = [];
	private readonly List<(RuleResearch first, List<RuleResearch> second)> _getOneFreeProtected = [];
	private RuleResearch _lookup;
	private string _neededItemName = "";
	private RuleItem _neededItem;
	private bool _needItem, _destroyItem, _returnsItem, _unlockFinalMission;
	private bool _repeatable;
	private int _listOrder;

	public RuleResearch(string name, int listOrder)
	{
		_name = name;
		_spawnedItemCount = 1;
		_cost = 0;
		_points = 0;
		_sequentialGetOneFree = false;
		_needItem = false;
		_destroyItem = false;
		_returnsItem = false;
		_unlockFinalMission = false;
		_repeatable = false;
		_listOrder = listOrder;
	}

	/// <summary>Loads the research project from YAML.</summary>
	/// <remarks>The C++ <c>parsers</c> argument (script values) is left out with the script hooks.</remarks>
	public void Load(YamlNodeReader node, Mod mod)
	{
		var reader = node.UseIndex();
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent, mod);
		}

		reader.TryRead("lookup", ref _lookupName);
		reader.TryRead("cutscene", ref _cutscene);
		reader.TryRead("spawnedItem", ref _spawnedItem);
		reader.TryRead("spawnedItemCount", ref _spawnedItemCount);
		mod.LoadUnorderedNames(_name, _spawnedItemList, reader["spawnedItemList"]);
		mod.LoadUnorderedNames(_name, _decreaseCounter, reader["decreaseCounter"]);
		mod.LoadUnorderedNames(_name, _increaseCounter, reader["increaseCounter"]);
		reader.TryRead("spawnedEvent", ref _spawnedEvent);
		reader.TryRead("cost", ref _cost);
		reader.TryRead("points", ref _points);
		mod.LoadUnorderedNames(_name, _dependenciesName, reader["dependencies"]);
		mod.LoadUnorderedNames(_name, _unlocksName, reader["unlocks"]);
		mod.LoadUnorderedNames(_name, _disablesName, reader["disables"]);
		mod.LoadUnorderedNames(_name, _reenablesName, reader["reenables"]);
		mod.LoadUnorderedNames(_name, _getOneFreeName, reader["getOneFree"]);
		mod.LoadUnorderedNames(_name, _requiresName, reader["requires"]);
		mod.LoadBaseFunction(_name, ref _requiresBaseFunc, reader["requiresBaseFunc"]);
		reader.TryRead("sequentialGetOneFree", ref _sequentialGetOneFree);
		mod.LoadNamesToNames(_name, _getOneFreeProtectedName, reader["getOneFreeProtected"]);
		mod.LoadNameNull(_name, ref _neededItemName, reader["neededItem"]);
		reader.TryRead("needItem", ref _needItem);
		reader.TryRead("destroyItem", ref _destroyItem);
		reader.TryRead("returnsItem", ref _returnsItem);
		reader.TryRead("unlockFinalMission", ref _unlockFinalMission);
		reader.TryRead("repeatable", ref _repeatable);
		reader.TryRead("listOrder", ref _listOrder);

		_scriptValues.Load(reader, mod.ScriptParsers.GetShared());
	}

	/// <summary>Cross link with other rules.</summary>
	public void AfterLoad(Mod mod)
	{
		// This is necessary, research code assumes it!
		if (_requiresName.Count != 0 && _cost != 0)
		{
			throw new InvalidOperationException("Research topic " + _name + " has requirements, but the cost is not zero. Sorry, this is not allowed!");
		}

		if (_lookupName == _name)
		{
			_lookupName = "";
		}
		mod.LinkRule(ref _lookup, ref _lookupName);

		if (_needItem)
		{
			// FIXME: this would break all mods unfortunately, maybe one day...
			//mod->linkRule(_neededItem, _neededItemName.empty() ? _name : _neededItemName);
			var linkedByName = mod.GetItem(_name, false); // false, because even vanilla ruleset is a mess
			if (string.IsNullOrEmpty(_neededItemName))
			{
				_neededItem = linkedByName;
			}
			else
			{
				_neededItem = mod.GetItem(_neededItemName, true);
			}

			if (_neededItem != null && linkedByName != null && linkedByName != _neededItem)
			{
				throw new LoadRuleException(_name, "Conflict between researched item '" + _name + "' and needed item '" + _neededItemName + "'");
			}
		}

		_dependencies = mod.GetResearch(_dependenciesName);
		_unlocks = mod.GetResearch(_unlocksName);
		_disables = mod.GetResearch(_disablesName);
		_reenables = mod.GetResearch(_reenablesName);
		_getOneFree = mod.GetResearch(_getOneFreeName);
		_requires = mod.GetResearch(_requiresName);

		foreach (var n in _getOneFreeProtectedName)
		{
			var left = mod.GetResearch(n.first, false);
			if (left != null)
			{
				var right = mod.GetResearch(n.second);
				_getOneFreeProtected.Add((left, right));
			}
			else
			{
				throw new InvalidOperationException("Unknown research '" + n.first + "'");
			}
		}

		//remove not needed data
		_dependenciesName.Clear();
		_unlocksName.Clear();
		_disablesName.Clear();
		_reenablesName.Clear();
		_getOneFreeName.Clear();
		_requiresName.Clear();
		_getOneFreeProtectedName.Clear();
	}

	/// <summary>Gets the cost of this ResearchProject.</summary>
	public int Cost => _cost;

	/// <summary>Gets the name of this ResearchProject.</summary>
	public string Name => _name;

	/// <summary>Gets the list of dependencies, i.e. ResearchProjects, that must be discovered before this one.</summary>
	public List<RuleResearch> Dependencies => _dependencies;

	/// <summary>Checks if this ResearchProject gives free topics in sequential order (or random order).</summary>
	public bool SequentialGetOneFree() => _sequentialGetOneFree;

	/// <summary>Gets the Item needed for this research.</summary>
	public RuleItem NeededItem => _neededItem;

	/// <summary>Checks if this ResearchProject needs a corresponding Item to be researched.</summary>
	public bool NeedItem() => _needItem;

	/// <summary>Checks if this ResearchProject consumes the corresponding Item when research completes.</summary>
	public bool DestroyItem() => _destroyItem;

	/// <summary>Checks if this ResearchProject returns the corresponding Item when research completes.</summary>
	public bool ReturnItem() => _returnsItem;

	/// <summary>Checks if this ResearchProject holds the researched item during research.</summary>
	public bool IsHoldingNeededItem => _needItem && (_destroyItem || _returnsItem);

	/// <summary>Check if this ResearchProject is unlocking final mission, it can be only one!</summary>
	public bool UnlockFinalMission() => _unlockFinalMission;

	/// <summary>Check if this ResearchProject is repeatable, i.e. is never marked as discovered.</summary>
	public bool IsRepeatable => _repeatable;

	/// <summary>Gets the list of ResearchProjects unlocked by this research.</summary>
	public List<RuleResearch> Unlocked => _unlocks;

	/// <summary>Gets the list of ResearchProjects disabled by this research.</summary>
	public List<RuleResearch> Disabled => _disables;

	/// <summary>Gets the list of ResearchProjects reenabled by this research.</summary>
	public List<RuleResearch> Reenabled => _reenables;

	/// <summary>Gets the points earned for discovering this ResearchProject.</summary>
	public int Points => _points;

	/// <summary>Gets the list of ResearchProjects granted at random for free by this research.</summary>
	public List<RuleResearch> GetOneFree => _getOneFree;

	/// <summary>Gets the list(s) of ResearchProjects granted at random for free by this research (if a defined prerequisite is met).</summary>
	public List<(RuleResearch first, List<RuleResearch> second)> GetOneFreeProtected => _getOneFreeProtected;

	/// <summary>Gets what article to look up in the ufopedia.</summary>
	public RuleResearch Lookup => _lookup;

	/// <summary>Gets the requirements for this ResearchProject.</summary>
	public List<RuleResearch> Requirements => _requires;

	/// <summary>Gets the base requirements for this ResearchProject.</summary>
	public RuleBaseFacilityFunctions RequireBaseFunc => _requiresBaseFunc;

	/// <summary>Gets the list weight for this research item.</summary>
	public int ListOrder => _listOrder;

	/// <summary>Gets the cutscene to play when this research item is completed.</summary>
	public string Cutscene => _cutscene;

	/// <summary>Gets the item to spawn in the base stores when this topic is researched.</summary>
	public string SpawnedItem => _spawnedItem;

	/// <summary>Gets the number of items to spawn in the base stores when this topic is researched.</summary>
	public int SpawnedItemCount => _spawnedItemCount;

	/// <summary>Gets the list of items to spawn in the base stores when this topic is researched.</summary>
	public List<string> SpawnedItemList => _spawnedItemList;

	/// <summary>Gets the geoscape event to spawn when this topic is researched.</summary>
	public string SpawnedEvent => _spawnedEvent;

	/// <summary>Gets the name of custom counter variables to decrease when this topic is researched.</summary>
	public List<string> DecreaseCounter => _decreaseCounter;

	/// <summary>Gets the name of custom counter variables to increase when this topic is researched.</summary>
	public List<string> IncreaseCounter => _increaseCounter;
}
