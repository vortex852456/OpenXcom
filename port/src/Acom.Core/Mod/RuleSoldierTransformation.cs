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
// Ported from: src/Mod/RuleSoldierTransformation.h, src/Mod/RuleSoldierTransformation.cpp
#nullable disable


namespace Acom;

/// <summary>
/// Ruleset data structure for the information to transform a soldier (necromancy, cloning, ascending!).
/// </summary>
public class RuleSoldierTransformation
{
	private readonly string _name;
	private readonly List<string> _requires = [], _requiredPreviousTransformations = [], _forbiddenPreviousTransformations = [];
	private RuleBaseFacilityFunctions _requiresBaseFunc;
	private string _producedItem = "";
	private string _producedSoldierType = "", _producedSoldierArmor = "";
	private bool _keepSoldierArmor, _createsClone, _needsCorpseRecovered, _allowsDeadSoldiers, _allowsLiveSoldiers, _allowsWoundedSoldiers;
	private readonly List<string> _allowedSoldierTypes = [];
	private readonly SortedDictionary<string, int> _requiredItems = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, int> _requiredCommendations = new(StringComparer.Ordinal);
	private int _listOrder, _cost, _transferTime, _recoveryTime;
	private int _minRank;
	private bool _includeBonusesForMinStats, _includeBonusesForMaxStats;
	private UnitStats _requiredMinStats, _requiredMaxStats, _flatOverallStatChange, _percentOverallStatChange, _percentGainedStatChange;
	private UnitStats _flatMin, _flatMax, _percentMin, _percentMax, _percentGainedMin, _percentGainedMax;
	private bool _showMinMax;
	private UnitStats _rerollStats;
	private bool _lowerBoundAtMinStats, _upperBoundAtMaxStats, _upperBoundAtStatCaps;
	private int _upperBoundType;
	private readonly List<string> _removeTransformations = [];
	private bool _reset;
	private bool _resetRank;
	private string _soldierBonusType = "";
	private readonly WeightedOptions _events;

	/// <summary>Constructor for a soldier transformation project.</summary>
	/// <param name="name">The unique project name id.</param>
	/// <param name="listOrder">The list weight for this transformation project.</param>
	public RuleSoldierTransformation(string name, int listOrder)
	{
		_name = name;
		_keepSoldierArmor = false;
		_createsClone = false;
		_needsCorpseRecovered = true;
		_allowsDeadSoldiers = false;
		_allowsLiveSoldiers = false;
		_allowsWoundedSoldiers = false;
		_listOrder = listOrder;
		_cost = 0;
		_transferTime = 0;
		_recoveryTime = 0;
		_minRank = 0;
		_includeBonusesForMinStats = false;
		_includeBonusesForMaxStats = false;
		_showMinMax = false;
		_lowerBoundAtMinStats = true;
		_upperBoundAtMaxStats = false;
		_upperBoundAtStatCaps = false;
		_upperBoundType = 0;
		_reset = false;
		_resetRank = false;
		_events = new WeightedOptions();
		_requiredMaxStats = UnitStats.Scalar(9999); // set all default max stats to 9999
	}

	/// <summary>Loads the transformation project from YAML.</summary>
	public void Load(YamlNodeReader node, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		var reader = node.UseIndex();
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent, mod);
		}

		reader.TryRead("listOrder", ref _listOrder);

		mod.LoadUnorderedNames(_name, _requires, reader["requires"]);
		mod.LoadBaseFunction(_name, ref _requiresBaseFunc, reader["requiresBaseFunc"]);
		reader.TryRead("producedItem", ref _producedItem);
		reader.TryRead("producedSoldierType", ref _producedSoldierType);
		reader.TryRead("producedSoldierArmor", ref _producedSoldierArmor);
		reader.TryRead("keepSoldierArmor", ref _keepSoldierArmor);
		reader.TryRead("createsClone", ref _createsClone);
		reader.TryRead("needsCorpseRecovered", ref _needsCorpseRecovered);
		reader.TryRead("allowsDeadSoldiers", ref _allowsDeadSoldiers);
		reader.TryRead("allowsLiveSoldiers", ref _allowsLiveSoldiers);
		reader.TryRead("allowsWoundedSoldiers", ref _allowsWoundedSoldiers);
		mod.LoadUnorderedNames(_name, _allowedSoldierTypes, reader["allowedSoldierTypes"]);
		mod.LoadUnorderedNames(_name, _requiredPreviousTransformations, reader["requiredPreviousTransformations"]);
		mod.LoadUnorderedNames(_name, _forbiddenPreviousTransformations, reader["forbiddenPreviousTransformations"]);
		reader.TryRead("includeBonusesForMinStats", ref _includeBonusesForMinStats);
		reader.TryRead("includeBonusesForMaxStats", ref _includeBonusesForMaxStats);
		reader.TryRead("requiredMinStats", ref _requiredMinStats);
		if (reader["requiredMaxStats"])
		{
			UnitStats tmp = reader["requiredMaxStats"].ReadVal(_requiredMaxStats);
			_requiredMaxStats.Merge(tmp);
		}
		mod.LoadUnorderedNamesToInt(_name, _requiredItems, reader["requiredItems"]);
		mod.LoadUnorderedNamesToInt(_name, _requiredCommendations, reader["requiredCommendations"]);
		reader.TryRead("cost", ref _cost);
		reader.TryRead("transferTime", ref _transferTime);
		reader.TryRead("recoveryTime", ref _recoveryTime);
		reader.TryRead("minRank", ref _minRank);
		reader.TryRead("flatOverallStatChange", ref _flatOverallStatChange);
		reader.TryRead("percentOverallStatChange", ref _percentOverallStatChange);
		reader.TryRead("percentGainedStatChange", ref _percentGainedStatChange);
		reader.TryRead("flatMin", ref _flatMin);
		reader.TryRead("flatMax", ref _flatMax);
		reader.TryRead("percentMin", ref _percentMin);
		reader.TryRead("percentMax", ref _percentMax);
		reader.TryRead("percentGainedMin", ref _percentGainedMin);
		reader.TryRead("percentGainedMax", ref _percentGainedMax);
		reader.TryRead("showMinMax", ref _showMinMax);
		reader.TryRead("rerollStats", ref _rerollStats);
		reader.TryRead("lowerBoundAtMinStats", ref _lowerBoundAtMinStats);
		reader.TryRead("upperBoundAtMaxStats", ref _upperBoundAtMaxStats);
		reader.TryRead("upperBoundAtStatCaps", ref _upperBoundAtStatCaps);
		reader.TryRead("upperBoundType", ref _upperBoundType);

		mod.LoadUnorderedNames(_name, _removeTransformations, reader["removeTransformations"]);
		reader.TryRead("reset", ref _reset);
		reader.TryRead("resetRank", ref _resetRank);
		reader.TryRead("soldierBonusType", ref _soldierBonusType);

		if (reader["events"])
		{
			_events.Load(reader["events"]);
		}
	}

	/// <summary>Gets the unique name id of the project.</summary>
	public string Name { get => _name; }
	/// <summary>Gets the list weight of the project.</summary>
	public int ListOrder { get => _listOrder; }
	/// <summary>Gets the list of research this project requires.</summary>
	public List<string> RequiredResearch { get => _requires; }
	/// <summary>Gets the list of required base functions for this project (a copy, like the C++ bitset by value).</summary>
	public RuleBaseFacilityFunctions RequiredBaseFuncs => _requiresBaseFunc;
	/// <summary>Gets the type of item produced by this project (the soldier stops existing completely and is fully replaced by the item).</summary>
	public string ProducedItem { get => _producedItem; }
	/// <summary>Gets the type of soldier produced by this project.</summary>
	public string ProducedSoldierType { get => _producedSoldierType; }
	/// <summary>Gets the armor that the produced soldier should be wearing.</summary>
	public string ProducedSoldierArmor { get => _producedSoldierArmor; }
	/// <summary>Gets whether or not the soldier should keep their armor after the project.</summary>
	public bool IsKeepingSoldierArmor { get => _keepSoldierArmor; }
	/// <summary>Gets whether or not the project should produce a clone (new id) of the input soldier.</summary>
	public bool IsCreatingClone { get => _createsClone; }
	/// <summary>Gets whether or not the project needs the body of the soldier to have been recovered.</summary>
	public bool NeedsCorpseRecovered() => _needsCorpseRecovered;
	/// <summary>Gets whether or not the project allows input of dead soldiers.</summary>
	public bool IsAllowingDeadSoldiers { get => _allowsDeadSoldiers; }
	/// <summary>Gets whether or not the project allows input of alive soldiers.</summary>
	public bool IsAllowingAliveSoldiers { get => _allowsLiveSoldiers; }
	/// <summary>Gets whether or not the project allows input of wounded soldiers.</summary>
	public bool IsAllowingWoundedSoldiers { get => _allowsWoundedSoldiers; }
	/// <summary>Gets the list of soldier types eligible for this project.</summary>
	public List<string> AllowedSoldierTypes { get => _allowedSoldierTypes; }
	/// <summary>Gets the list of previous soldier transformations a soldier needs for this project.</summary>
	public List<string> RequiredPreviousTransformations { get => _requiredPreviousTransformations; }
	/// <summary>Gets the list of previous soldier transformations that make a soldier ineligible for this project.</summary>
	public List<string> ForbiddenPreviousTransformations { get => _forbiddenPreviousTransformations; }
	/// <summary>Gets whether or not to include soldier bonuses when checking required min stats.</summary>
	public bool IncludeBonusesForMinStats { get => _includeBonusesForMinStats; }
	/// <summary>Gets whether or not to include soldier bonuses when checking required max stats.</summary>
	public bool IncludeBonusesForMaxStats { get => _includeBonusesForMaxStats; }
	/// <summary>Gets the minimum stats a soldier needs to be eligible for this project.</summary>
	public UnitStats RequiredMinStats { get => _requiredMinStats; }
	/// <summary>Gets the maximum stats a soldier can have to be eligible for this project.</summary>
	public UnitStats RequiredMaxStats { get => _requiredMaxStats; }
	/// <summary>Gets the list of items necessary to complete this project.</summary>
	public SortedDictionary<string, int> RequiredItems { get => _requiredItems; }
	/// <summary>Gets the list of commendations necessary to complete this project.</summary>
	public SortedDictionary<string, int> RequiredCommendations { get => _requiredCommendations; }
	/// <summary>Gets the cash cost of the project.</summary>
	public int Cost { get => _cost; }
	/// <summary>Gets how long the transformed soldier should be in transit to the base after completion.</summary>
	public int TransferTime { get => _transferTime; }
	/// <summary>Gets how long the transformed soldier should take to recover after completion.</summary>
	public int RecoveryTime { get => _recoveryTime; }
	/// <summary>Gets the minimum rank a soldier needs to be eligible for this project.</summary>
	public int MinRank { get => _minRank; }
	/// <summary>Gets the flat change to a soldier's overall stats when undergoing this project.</summary>
	public UnitStats FlatOverallStatChange { get => _flatOverallStatChange; }
	/// <summary>Gets the percent change to a soldier's overall stats when undergoing this project.</summary>
	public UnitStats PercentOverallStatChange { get => _percentOverallStatChange; }
	/// <summary>Gets the percent change to a soldier's gained stats when undergoing this project.</summary>
	public UnitStats PercentGainedStatChange { get => _percentGainedStatChange; }
	/// <summary>Gets the min flat change to a soldier's overall stats when undergoing this project.</summary>
	public UnitStats FlatMin { get => _flatMin; }
	/// <summary>Gets the max flat change to a soldier's overall stats when undergoing this project.</summary>
	public UnitStats FlatMax { get => _flatMax; }
	/// <summary>Gets the min percent change to a soldier's overall stats when undergoing this project.</summary>
	public UnitStats PercentMin { get => _percentMin; }
	/// <summary>Gets the max percent change to a soldier's overall stats when undergoing this project.</summary>
	public UnitStats PercentMax { get => _percentMax; }
	/// <summary>Gets the min percent change to a soldier's gained stats when undergoing this project.</summary>
	public UnitStats PercentGainedMin { get => _percentGainedMin; }
	/// <summary>Gets the max percent change to a soldier's gained stats when undergoing this project.</summary>
	public UnitStats PercentGainedMax { get => _percentGainedMax; }
	/// <summary>Gets whether to display min/max changes in two lines; or just one line with randomized changes replaced with a question mark.</summary>
	public bool ShowMinMax { get => _showMinMax; }
	/// <summary>Gets information about which soldier stats should be re-rolled when undergoing this project.</summary>
	public UnitStats RerollStats { get => _rerollStats; }
	/// <summary>Gets whether or not this project should bound stat penalties at the produced RuleSoldier's minStats.</summary>
	public bool HasLowerBoundAtMinStats { get => _lowerBoundAtMinStats; }
	/// <summary>Gets whether or not this project should cap stats at the produced RuleSoldier's maxStats.</summary>
	public bool HasUpperBoundAtMaxStats { get => _upperBoundAtMaxStats; }
	/// <summary>Gets whether or not this project should cap stats at the produced RuleSoldier's statCaps.</summary>
	public bool HasUpperBoundAtStatCaps { get => _upperBoundAtStatCaps; }

	/// <summary>Gets whether to use soft upper bound limit or not.</summary>
	public bool IsSoftLimit(bool isSameSoldierType)
	{
		if (_upperBoundType == 0)
		{
			return isSameSoldierType; // 0 = dynamic
		}
		else if (_upperBoundType == 1)
		{
			return true; // 1 = soft limit
		}
		return false; // 2+ = hard limit
	}

	/// <summary>Gets the list of (potential) previous soldier transformations to remove when undergoing this project.</summary>
	public List<string> RemoveTransformations { get => _removeTransformations; }
	/// <summary>Gets whether or not this project should reset info about all previous transformations and all previously assigned soldier bonuses.</summary>
	public bool Reset { get => _reset; }
	/// <summary>Gets whether or not this project should reset the rank of the destination soldier to rookie.</summary>
	public bool ResetRank { get => _resetRank; }
	/// <summary>Gets the type of soldier bonus assigned by this project.</summary>
	public string SoldierBonusType { get => _soldierBonusType; }
	/// <summary>Gets geoscape event rule name to spawn after soldier transformation.</summary>
	public string ChooseEvent() => _events.Choose();
}
