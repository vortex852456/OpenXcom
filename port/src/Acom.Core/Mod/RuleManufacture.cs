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
// Ported from: src/Mod/RuleManufacture.h, src/Mod/RuleManufacture.cpp
#nullable disable

namespace Acom;

public enum ManufacturingFilterType : uint
{
	MANU_FILTER_DEFAULT = 0,
	MANU_FILTER_DEFAULT_SUPPLIES_OK = 1,
	MANU_FILTER_DEFAULT_NO_SUPPLIES = 2,
	MANU_FILTER_FACILITY_REQUIRED = 3,
	MANU_FILTER_HIDDEN = 4,
}

/// <summary>
/// Represents the information needed to manufacture an object.
/// </summary>
/// <remarks>
/// The C++ maps keyed by rule pointers iterate in address order; the dictionaries here keep insertion order.
/// </remarks>
public class RuleManufacture
{
	public const int MANU_STATUS_NEW = 0;
	public const int MANU_STATUS_NORMAL = 1;
	public const int MANU_STATUS_HIDDEN = 2;
	public const int MANU_STATUSES = 3;

	private string _name, _category = "";
	private string _spawnedPersonType = "", _spawnedPersonName = "";
	private string _spawnedSoldier = "";
	private readonly List<string> _requiresName = [];
	private RuleBaseFacilityFunctions _requiresBaseFunc;
	private List<RuleResearch> _requires = [];
	private int _space, _time, _cost;
	private int _points;
	private bool _refund;
	private readonly SortedDictionary<string, int> _requiredItemsNames = new(StringComparer.Ordinal), _producedItemsNames = new(StringComparer.Ordinal);
	private readonly Dictionary<RuleItem, int> _requiredItems = [], _producedItems = [];
	private readonly Dictionary<RuleCraft, int> _requiredCrafts = [];
	private RuleCraft _producedCraft;
	private List<(int first, SortedDictionary<string, int> second)> _randomProducedItemsNames = [];
	private readonly List<(int first, Dictionary<RuleItem, int> second)> _randomProducedItems = [];
	private List<int> _transferTimes = [];
	private int _listOrder;

	/// <summary>Creates a new manufacture.</summary>
	/// <param name="name">The unique manufacture name.</param>
	/// <param name="listOrder">The list weight for this manufacture.</param>
	public RuleManufacture(string name, int listOrder)
	{
		_name = name;
		_space = 0;
		_time = 0;
		_cost = 0;
		_points = 0;
		_refund = false;
		_producedCraft = null;
		_listOrder = listOrder;
		_producedItemsNames[name] = 1;
	}

	/// <summary>Loads the manufacture project from YAML.</summary>
	public void Load(YamlNodeReader node, Mod mod)
	{
		var reader = node.UseIndex();
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent, mod);
		}

		reader.TryRead("category", ref _category);
		mod.LoadUnorderedNames(_name, _requiresName, reader["requires"]);
		mod.LoadBaseFunction(_name, ref _requiresBaseFunc, reader["requiresBaseFunc"]);
		reader.TryRead("space", ref _space);
		reader.TryRead("time", ref _time);
		reader.TryRead("cost", ref _cost);
		reader.TryRead("points", ref _points);
		reader.TryRead("refund", ref _refund);
		mod.LoadUnorderedNamesToInt(_name, _requiredItemsNames, reader["requiredItems"]);
		mod.LoadUnorderedNamesToInt(_name, _producedItemsNames, reader["producedItems"]);
		reader.TryRead("randomProducedItems", ref _randomProducedItemsNames);
		reader.TryRead("spawnedPersonType", ref _spawnedPersonType);
		reader.TryRead("spawnedPersonName", ref _spawnedPersonName);
		if (reader["spawnedSoldier"])
		{
			_spawnedSoldier = EmitDescendants(reader["spawnedSoldier"], new YamlRootNodeReader(_spawnedSoldier, "(spawned soldier template)").ToBase());
		}
		reader.TryRead("transferTimes", ref _transferTimes);
		reader.TryRead("listOrder", ref _listOrder);
	}

	/// <summary>Cross link with other rules.</summary>
	public void AfterLoad(Mod mod)
	{
		if (_time <= 0)
		{
			throw new InvalidOperationException("Manufacturing time must be greater than zero.");
		}

		_requires = mod.GetResearch(_requiresName);

		if (_category == "STR_CRAFT")
		{
			if (_producedItemsNames.Count == 0)
			{
				throw new InvalidOperationException("No craft defined for production");
			}
			var item = _producedItemsNames.First();
			if (item.Value != 1)
			{
				throw new InvalidOperationException("Only one craft can be build in production");
			}
			else
			{
				_producedCraft = mod.GetCraft(item.Key, true);
			}
		}
		else
		{
			foreach (var i in _producedItemsNames)
			{
				_producedItems[mod.GetItem(i.Key, true)] = i.Value;
			}
		}

		foreach (var i in _requiredItemsNames)
		{
			var itemRule = mod.GetItem(i.Key, false);
			var craftRule = mod.GetCraft(i.Key, false);
			if (itemRule != null)
			{
				_requiredItems[itemRule] = i.Value;
			}
			else if (craftRule != null)
			{
				_requiredCrafts[craftRule] = i.Value;
			}
			else
			{
				throw new InvalidOperationException("Unknown required item '" + i.Key + "'");
			}
		}

		foreach (var itemSet in _randomProducedItemsNames)
		{
			var tmp = new Dictionary<RuleItem, int>();
			foreach (var i in itemSet.second)
			{
				tmp[mod.GetItem(i.Key, true)] = i.Value;
			}
			_randomProducedItems.Add((itemSet.first, tmp));
		}

		//remove not needed data
		_requiresName.Clear();
		_producedItemsNames.Clear();
		_requiredItemsNames.Clear();
		_randomProducedItemsNames.Clear();
	}

	/// <summary>Change the name and break down the sub-projects into simpler components.</summary>
	public void BreakDown(Mod mod, RuleManufactureShortcut recipe)
	{
		// 0. rename
		_name = recipe.Name;

		// 1. init temp variables
		var tempRequiredItems = new Dictionary<RuleItem, int>(_requiredItems);
		var tempRequires = new Dictionary<RuleResearch, bool>();
		foreach (var r in _requires)
			tempRequires[r] = true;
		RuleBaseFacilityFunctions tempRequiresBaseFunc = _requiresBaseFunc;

		// 2. break down iteratively
		bool doneSomething = false;
		do
		{
			doneSomething = false;
			foreach (var projectName in recipe.BreakDownItems)
			{
				RuleItem itemRule = mod.GetItem(projectName, true);
				RuleManufacture projectRule = mod.GetManufacture(projectName, true);
				tempRequiredItems.TryAdd(itemRule, 0); // std::map::operator[] inserts
				int count = tempRequiredItems[itemRule];
				if (count > 0)
				{
					tempRequiredItems[itemRule] = 0;
					doneSomething = true;

					_space = Math.Max(_space, projectRule.RequiredSpace);
					_time += count * projectRule.ManufactureTime;
					_cost += count * projectRule.ManufactureCost;
					foreach (var ri in projectRule.RequiredItems)
					{
						tempRequiredItems.TryGetValue(ri.Key, out int old);
						tempRequiredItems[ri.Key] = old + count * ri.Value;
					}
					foreach (var r in projectRule.Requirements)
						tempRequires[r] = true;
					tempRequiresBaseFunc |= projectRule.RequireBaseFunc;
				}
			}
		}
		while (doneSomething);

		// 3. update
		{
			_requiredItems.Clear();
			foreach (var item in tempRequiredItems)
				if (item.Value > 0)
					_requiredItems[item.Key] = item.Value;
		}
		if (recipe.BreakDownRequires)
		{
			_requires.Clear();
			foreach (var item in tempRequires)
				if (item.Value)
					_requires.Add(item.Key);
		}
		if (recipe.BreakDownRequiresBaseFunc)
		{
			_requiresBaseFunc = tempRequiresBaseFunc;
		}
	}

	/// <summary>Gets the unique name of the manufacture.</summary>
	public string Name => _name;

	/// <summary>Gets the category shown in the manufacture list.</summary>
	public string Category => _category;

	/// <summary>Gets the list of research required to manufacture this object.</summary>
	public List<RuleResearch> Requirements => _requires;

	/// <summary>Gets the base requirements.</summary>
	public RuleBaseFacilityFunctions RequireBaseFunc => _requiresBaseFunc;

	/// <summary>Gets the required workshop space to start production.</summary>
	public int RequiredSpace => _space;

	/// <summary>Gets the time needed to manufacture one object.</summary>
	public int ManufactureTime => _time;

	/// <summary>Gets the cost of manufacturing one object.</summary>
	public int ManufactureCost => _cost;

	/// <summary>Checks if there's enough funds to manufacture one object.</summary>
	/// <param name="funds">Current funds.</param>
	public bool HaveEnoughMoneyForOneMoreUnit(long funds)
	{
		// either we have enough money, or the production doesn't cost anything
		return funds >= _cost || _cost <= 0;
	}

	/// <summary>Gets the points earned for manufacturing one production object.</summary>
	public int Points => _points;

	/// <summary>Should all resources of a cancelled project be refunded?</summary>
	public bool Refund
	{
		get
		{
			if (Options.oxceAlwaysRefundProductionCosts) return true;
			return _refund;
		}
	}

	/// <summary>Gets the list of items required to manufacture one object.</summary>
	public Dictionary<RuleItem, int> RequiredItems => _requiredItems;

	/// <summary>Gets the list of crafts required to manufacture one object.</summary>
	public Dictionary<RuleCraft, int> RequiredCrafts => _requiredCrafts;

	/// <summary>
	/// Gets the list of items produced by completing "one object" of this project.
	/// By default: it contains only the "name" item with a value of 1.
	/// </summary>
	public Dictionary<RuleItem, int> ProducedItems => _producedItems;

	/// <summary>If this produces a craft, returns its type, otherwise null.</summary>
	public RuleCraft ProducedCraft => _producedCraft;

	/// <summary>Gets the random manufacture rules.</summary>
	public List<(int first, Dictionary<RuleItem, int> second)> RandomProducedItems => _randomProducedItems;

	/// <summary>Gets the "manufactured person", i.e. person spawned when manufacturing project ends.</summary>
	public string SpawnedPersonType => _spawnedPersonType;

	/// <summary>Gets the custom name of the "manufactured person".</summary>
	public string SpawnedPersonName => _spawnedPersonName;

	/// <summary>Gets the spawned soldier template (YAML text, C++ <c>YAML::YamlString</c>).</summary>
	public string SpawnedSoldierTemplate => _spawnedSoldier;

	/// <summary>Is it possible to use auto-sell feature for this manufacturing project?</summary>
	public bool CanAutoSell()
	{
		if (!string.IsNullOrEmpty(_spawnedPersonType))
		{
			// can't sell people
			return false;
		}
		if (_randomProducedItems.Count != 0)
		{
			// sell profit label can't show reasonable info
			return false;
		}
		return true;
	}

	/// <summary>Gets the transfer time info.</summary>
	public List<int> TransferTimes => _transferTimes;

	/// <summary>Gets the list weight for this manufacture item.</summary>
	public int ListOrder => _listOrder;

	/// <summary>
	/// C++ <c>YamlNodeReader::emitDescendants(defaultValuesReader)</c>: serializes the node's children plus
	/// the children of <paramref name="defaultValuesReader"/> whose keys the node does not have.
	/// </summary>
	private static string EmitDescendants(YamlNodeReader reader, YamlNodeReader defaultValuesReader)
	{
		var node = reader.Node;
		if (node == null || (node.Kind != YamlKind.Map && node.Kind != YamlKind.Seq))
			return "";
		var root = YamlEmitter.FromReader(node);
		if (defaultValuesReader.Node is { } defaults)
		{
			foreach (var child in defaults.Children)
			{
				if (!root.Children.Exists(c => c.Key == child.Key))
					root.Children.Add(YamlEmitter.FromReader(child));
			}
		}
		return YamlEmitter.Emit(root);
	}
}
