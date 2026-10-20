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
// Ported from: src/Mod/RuleStartingCondition.h, src/Mod/RuleStartingCondition.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents a specific Starting Condition.
/// </summary>
public class RuleStartingCondition
{
	private readonly string _type;
	private readonly SortedDictionary<string, SortedDictionary<string, int>> _defaultArmor = new(StringComparer.Ordinal);
	private readonly List<string> _allowedArmors = [], _forbiddenArmors = [];
	private List<string> _forbiddenArmorsInNextStageName = [];
	private readonly List<Armor> _forbiddenArmorsInNextStage = [];
	private readonly List<string> _allowedVehicles = [], _forbiddenVehicles = [];
	private readonly List<string> _allowedItems = [], _forbiddenItems = [];
	private readonly List<string> _allowedItemCategories = [], _forbiddenItemCategories = [];
	private readonly List<string> _allowedCraft = [], _forbiddenCraft = [];
	private readonly List<string> _allowedSoldierTypes = [], _forbiddenSoldierTypes = [];
	private readonly SortedDictionary<string, int> _requiredItems = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, string> _craftTransformationsName = new(StringComparer.Ordinal);
	private readonly Dictionary<RuleCraft, RuleCraft> _craftTransformations = [];
	private bool _destroyRequiredItems;
	private bool _requireCommanderOnboard;

	/// <summary>Creates a blank ruleset for a certain type of Starting Condition.</summary>
	/// <param name="type">String defining the type.</param>
	public RuleStartingCondition(string type)
	{
		_type = type;
		_destroyRequiredItems = false;
		_requireCommanderOnboard = false;
	}

	/// <summary>Loads the Starting Conditions from YAML.</summary>
	/// <param name="node">YAML node.</param>
	/// <param name="mod">Mod for the starting condition.</param>
	public void Load(YamlNodeReader node, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		var reader = node.UseIndex();
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent, mod);
		}

		mod.LoadUnorderedNamesToNamesToInt(_type, _defaultArmor, reader["defaultArmor"]);
		mod.LoadUnorderedNames(_type, _allowedArmors, reader["allowedArmors"]);
		mod.LoadUnorderedNames(_type, _forbiddenArmors, reader["forbiddenArmors"]);
		mod.LoadUnorderedNames(_type, _forbiddenArmorsInNextStageName, reader["forbiddenArmorsInNextStage"]);
		mod.LoadUnorderedNames(_type, _allowedVehicles, reader["allowedVehicles"]);
		mod.LoadUnorderedNames(_type, _forbiddenVehicles, reader["forbiddenVehicles"]);
		mod.LoadUnorderedNames(_type, _allowedItems, reader["allowedItems"]);
		mod.LoadUnorderedNames(_type, _forbiddenItems, reader["forbiddenItems"]);
		mod.LoadUnorderedNames(_type, _allowedItemCategories, reader["allowedItemCategories"]);
		mod.LoadUnorderedNames(_type, _forbiddenItemCategories, reader["forbiddenItemCategories"]);
		mod.LoadUnorderedNames(_type, _allowedCraft, reader["allowedCraft"]);
		mod.LoadUnorderedNames(_type, _forbiddenCraft, reader["forbiddenCraft"]);
		mod.LoadUnorderedNames(_type, _allowedSoldierTypes, reader["allowedSoldierTypes"]);
		mod.LoadUnorderedNames(_type, _forbiddenSoldierTypes, reader["forbiddenSoldierTypes"]);

		mod.LoadUnorderedNamesToInt(_type, _requiredItems, reader["requiredItems"]);
		mod.LoadUnorderedNamesToNames(_type, _craftTransformationsName, reader["craftTransformations"]);
		reader.TryRead("destroyRequiredItems", ref _destroyRequiredItems);
		reader.TryRead("requireCommanderOnboard", ref _requireCommanderOnboard);

		if (reader["environmentalConditions"] || reader["paletteTransformations"] || reader["armorTransformations"]
			|| reader["mapBackgroundColor"] || reader["inventoryShockIndicator"] || reader["mapShockIndicator"])
		{
			Logger.Error($"There are invalid/obsolete attributes in starting condition {_type}. Please review the ruleset.");
		}
	}

	/// <summary>Cross link with other rules.</summary>
	public void AfterLoad(Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		// mod->linkRule(_forbiddenArmorsInNextStage, _forbiddenArmorsInNextStageName)
		foreach (var name in _forbiddenArmorsInNextStageName)
		{
			_forbiddenArmorsInNextStage.Add(mod.GetArmor(name, true));
		}
		_forbiddenArmorsInNextStageName = [];

		foreach (var pair in _craftTransformationsName)
		{
			var src = mod.GetCraft(pair.Key, true);
			var dest = mod.GetCraft(pair.Value, true);
			_craftTransformations[src] = dest;
		}

		//remove not needed data
		_craftTransformationsName.Clear();
	}

	/// <summary>Gets the Starting Conditions's type.</summary>
	public string Type => _type;

	/// <summary>Gets the allowed armor types.</summary>
	public List<string> AllowedArmors => _allowedArmors;

	/// <summary>Gets the forbidden armor types.</summary>
	public List<string> ForbiddenArmors => _forbiddenArmors;

	/// <summary>Gets the forbidden armors (in later stages).</summary>
	public List<Armor> ForbiddenArmorsInNextStage => _forbiddenArmorsInNextStage;

	/// <summary>Gets the allowed craft types.</summary>
	public List<string> AllowedCraft => _allowedCraft;

	/// <summary>Gets the forbidden craft types.</summary>
	public List<string> ForbiddenCraft => _forbiddenCraft;

	/// <summary>Gets the allowed soldier types.</summary>
	public List<string> AllowedSoldierTypes => _allowedSoldierTypes;

	/// <summary>Gets the forbidden soldier types.</summary>
	public List<string> ForbiddenSoldierTypes => _forbiddenSoldierTypes;

	/// <summary>Gets the required items.</summary>
	public SortedDictionary<string, int> RequiredItems => _requiredItems;

	/// <summary>Should the required items be destroyed when the mission starts?</summary>
	public bool DestroyRequiredItems => _destroyRequiredItems;

	/// <summary>Does the mission require a commander present onboard?</summary>
	public bool RequiresCommanderOnboard() => _requireCommanderOnboard;

	/// <summary>Checks if the craft type is permitted.</summary>
	/// <param name="craftType">Craft type name.</param>
	/// <returns>True if permitted, false otherwise.</returns>
	public bool IsCraftPermitted(string craftType)
	{
		if (_forbiddenCraft.Count != 0)
		{
			return !_forbiddenCraft.Contains(craftType);
		}
		else if (_allowedCraft.Count != 0)
		{
			return _allowedCraft.Contains(craftType);
		}
		return true;
	}

	/// <summary>Checks if the soldier type is permitted.</summary>
	/// <param name="soldierType">Soldier type name.</param>
	/// <returns>True if permitted, false otherwise.</returns>
	public bool IsSoldierTypePermitted(string soldierType)
	{
		if (_forbiddenSoldierTypes.Count != 0)
		{
			return !_forbiddenSoldierTypes.Contains(soldierType);
		}
		else if (_allowedSoldierTypes.Count != 0)
		{
			return _allowedSoldierTypes.Contains(soldierType);
		}
		return true;
	}

	/// <summary>Gets the replacement armor.</summary>
	/// <param name="soldierType">Soldier type name.</param>
	/// <param name="armorType">Existing/old armor type name.</param>
	/// <returns>Replacement armor type name (or empty string if no replacement is needed).</returns>
	public string GetArmorReplacement(string soldierType, string armorType)
	{
		bool allowed = true;

		if (_forbiddenArmors.Count != 0)
		{
			allowed = !_forbiddenArmors.Contains(armorType);
		}
		else if (_allowedArmors.Count != 0)
		{
			allowed = _allowedArmors.Contains(armorType);
		}

		if (!allowed)
		{
			if (_defaultArmor.TryGetValue(soldierType, out var j))
			{
				var w = new WeightedOptions();
				foreach (var k in j)
				{
					w.Set(k.Key, k.Value);
				}
				string pick = w.Choose();
				return pick == "noChange" ? "" : pick;
			}
		}

		return "";
	}

	/// <summary>Gets the replacement craft.</summary>
	/// <param name="sourceCraft">Existing/old craft type.</param>
	/// <param name="mapScriptCraft">Fallback craft type defined by map script (can be null).</param>
	/// <returns>Replacement craft type (or <paramref name="mapScriptCraft"/> if no replacement is defined).</returns>
	public RuleCraft GetCraftReplacement(RuleCraft sourceCraft, RuleCraft mapScriptCraft)
	{
		if (_craftTransformations.Count != 0 && sourceCraft != null)
		{
			if (_craftTransformations.TryGetValue(sourceCraft, out var i))
			{
				return i;
			}
		}
		return mapScriptCraft;
	}

	/// <summary>Checks if the vehicle type is permitted.</summary>
	/// <param name="vehicleType">Vehicle type name.</param>
	/// <returns>True if permitted, false otherwise.</returns>
	public bool IsVehiclePermitted(string vehicleType)
	{
		if (_forbiddenVehicles.Count != 0)
		{
			return !_forbiddenVehicles.Contains(vehicleType);
		}
		else if (_allowedVehicles.Count != 0)
		{
			return _allowedVehicles.Contains(vehicleType);
		}
		return true;
	}

	/// <summary>Checks if the item type is permitted.</summary>
	/// <param name="itemType">Item type name.</param>
	/// <param name="mod">Mod (for the item and its categories).</param>
	/// <param name="craft">Craft with the equipped ammo (for inherited ammo categories).</param>
	/// <returns>True if permitted, false otherwise.</returns>
	public bool IsItemPermitted(string itemType, Mod mod, Craft craft)
	{
		bool itemCheckSubResult = true; // if both item lists are empty, item is OK
		if (_forbiddenItems.Count != 0)
		{
			if (_forbiddenItems.Contains(itemType))
			{
				return false; // item explicitly forbidden
			}
			else
			{
				itemCheckSubResult = true; // item is OK, unless category rules below say otherwise
			}
		}
		else if (_allowedItems.Count != 0)
		{
			if (_allowedItems.Contains(itemType))
			{
				return true; // item explicitly allowed
			}
			else
			{
				itemCheckSubResult = false; // item is NOT OK, unless category rules below say otherwise
			}
		}

		bool checkForbiddenCategories = _forbiddenItemCategories.Count != 0;
		bool checkAllowedCategories = !checkForbiddenCategories && _allowedItemCategories.Count != 0;

		bool categoryCheckSubResult = true;
		if (checkForbiddenCategories)
			categoryCheckSubResult = true;
		else if (checkAllowedCategories)
			categoryCheckSubResult = false;

		if (checkForbiddenCategories || checkAllowedCategories)
		{
			ArgumentNullException.ThrowIfNull(mod);
			var item = mod.GetItem(itemType);
			if (item != null)
			{
				// primary categories (a copy)
				var itemCategories = new List<string>(item.Categories);
				// secondary categories ("inherited" from equipped ammo)
				if (mod.ShareAmmoCategories && item.BattleType == BattleType.BT_FIREARM)
				{
					foreach (var ammoRule in item.PrimaryCompatibleAmmo)
					{
						if (craft.Items.GetItem(ammoRule) > 0)
						{
							foreach (var cat in ammoRule.Categories)
							{
								itemCategories.Add(cat);
							}
						}
					}
				}
				if (itemCategories.Count != 0)
				{
					// check all categories of the item
					foreach (var catName in itemCategories)
					{
						if (checkForbiddenCategories)
						{
							if (_forbiddenItemCategories.Contains(catName))
							{
								return false; // found a category that is forbidden
							}
						}
						else if (checkAllowedCategories)
						{
							if (_allowedItemCategories.Contains(catName))
							{
								return true; // found a category that is allowed
							}
						}
					}
				}
			}
		}

		return itemCheckSubResult && categoryCheckSubResult;
	}
}
