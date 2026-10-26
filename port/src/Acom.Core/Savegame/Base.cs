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
// Ported from: src/Savegame/Base.h, src/Savegame/Base.cpp
#nullable disable

using System.Collections;
using System.Globalization;

namespace Acom;

public enum BasePlacementErrors
{
	/// <summary>0: ok</summary>
	BPE_None = 0,
	/// <summary>1: not connected to lift or on top of another facility (standard OXC behavior)</summary>
	BPE_NotConnected = 1,
	/// <summary>2: trying to upgrade over existing facility, but it's in use</summary>
	BPE_Used = 2,
	/// <summary>3: trying to upgrade over existing facility, but it's already being upgraded</summary>
	BPE_Upgrading = 3,
	/// <summary>4: trying to upgrade over existing facility, but size/placement mismatch</summary>
	BPE_UpgradeSizeMismatch = 4,
	/// <summary>5: trying to upgrade over existing facility, but ruleset of new facility requires a specific existing facility</summary>
	BPE_UpgradeRequireSpecific = 5,
	/// <summary>6: trying to upgrade over existing facility, but ruleset disallows it</summary>
	BPE_UpgradeDisallowed = 6,
	/// <summary>7: trying to upgrade over existing facility, but all buildings next to it are under construction and build queue is off</summary>
	BPE_Queue = 7,
	/// <summary>8: trying to build a facility, which is forbidden by other existing facilities in the base</summary>
	BPE_ForbiddenByOther = 8,
	/// <summary>9: trying to build a facility, which would forbid (i.e. would be in conflict with) other existing facilities in the base</summary>
	BPE_ForbiddenByThis = 9,
	/// <summary>10: not enough storage space</summary>
	BPE_Used_Stores = 10,
	/// <summary>11: not enough living quarters</summary>
	BPE_Used_Quarters = 11,
	/// <summary>12: not enough laboratories</summary>
	BPE_Used_Laboratories = 12,
	/// <summary>13: not enough workshops</summary>
	BPE_Used_Workshops = 13,
	/// <summary>14: not enough hangars</summary>
	BPE_Used_Hangars = 14,
	/// <summary>15: not enough psi labs</summary>
	BPE_Used_PsiLabs = 15,
	/// <summary>16: not enough gyms</summary>
	BPE_Used_Gyms = 16,
	/// <summary>17: not enough alien containment</summary>
	BPE_Used_AlienContainment = 17,
	/// <summary>18: trying to build a facility (from scratch) that can only be built as an upgrade of another facility</summary>
	BPE_UpgradeOnly = 18,
}

/// <summary>Summary of all recovery rates provided by a base.</summary>
public class BaseSumDailyRecovery
{
	/// <summary>Amount of mana recovery or loss provided by the base.</summary>
	public int ManaRecovery;
	/// <summary>Amount of health recovery provided by the base.</summary>
	public int HealthRecovery;
	/// <summary>Sum of the amount of additional wounds healed in this base due to sick bay facilities (as percentage of max HP per soldier).</summary>
	public float SickBayRelativeBonus;
	/// <summary>Sum of the amount of additional wounds healed in this base due to sick bay facilities (in absolute number).</summary>
	public float SickBayAbsoluteBonus;
}

/// <summary>
/// Represents a player base on the globe.
/// Bases can contain facilities, personnel, crafts and equipment.
/// </summary>
/// <remarks>
/// C++ iterators into the facility/craft vectors are indices here
/// (<see cref="GetDisconnectedFacilities"/>, <see cref="DestroyFacility"/>, <see cref="RemoveCraft"/>).
/// <c>RuleBaseFacilityFunctions</c> (<c>std::bitset&lt;128&gt;</c>) is a value type; getters return copies like C++.
/// </remarks>
public class Base : Target
{
	private const int BASE_SIZE = 6;

	private readonly Mod _mod;
	private readonly List<BaseFacility> _facilities = [];
	private readonly List<Soldier> _soldiers = [];
	private readonly List<Craft> _crafts = [];
	private readonly List<Transfer> _transfers = [];
	private readonly ItemContainer _items;
	private int _scientists, _engineers;
	private readonly List<ResearchProject> _research = [];
	private readonly List<Production> _productions = [];
	private bool _inBattlescape;
	private bool _retaliationTarget;
	private AlienMission _retaliationMission;
	private bool _fakeUnderwater;
	private readonly List<Vehicle> _vehicles = [];
	private readonly List<Vehicle> _vehiclesFromBase = [];
	private readonly List<BaseFacility> _defenses = [];
	private readonly Dictionary<RuleBaseFacility, int> _destroyedFacilitiesCache = [];
	private RuleBaseFacilityFunctions _provideBaseFunc;
	private RuleBaseFacilityFunctions _forbiddenBaseFunc;
	private Texture _globeTexture;

	/// <summary>Initializes an empty base.</summary>
	/// <param name="mod">Pointer to mod.</param>
	public Base(Mod mod) : base()
	{
		_mod = mod;
		_scientists = 0;
		_engineers = 0;
		_inBattlescape = false;
		_retaliationTarget = false;
		_retaliationMission = null;
		_fakeUnderwater = false;
		_items = new ItemContainer();
	}

	/// <summary>Loads the base from YAML.</summary>
	/// <param name="reader">YAML node.</param>
	/// <param name="save">Pointer to saved game.</param>
	/// <param name="newGame">Is this the first base of a new game?</param>
	/// <param name="newBattleGame">Is this the base of a skirmish game?</param>
	public void Load(YamlNodeReader reader, SavedGame save, bool newGame, bool newBattleGame = false)
	{
		base.Load(reader);
		if (!newGame || !Options.customInitialBase || newBattleGame)
		{
			foreach (var facilityReader in reader["facilities"].Children())
			{
				string type = facilityReader["type"].ReadVal<string>();
				if (_mod.GetBaseFacility(type) != null)
				{
					var f = new BaseFacility(_mod.GetBaseFacility(type), this);
					f.Load(facilityReader);
					_facilities.Add(f);
				}
				else
				{
					Logger.Error($"Failed to load facility {type}");
				}
			}
		}
		foreach (var craftReader in reader["crafts"].Children())
		{
			string type = craftReader["type"].ReadVal<string>();
			if (_mod.GetCraft(type) != null)
			{
				var c = new Craft(_mod.GetCraft(type), this);
				c.Load(craftReader, _mod, save);
				_crafts.Add(c);
			}
			else
			{
				Logger.Error($"Failed to load craft {type}");
			}
		}
		foreach (var soldierReader in reader["soldiers"].Children())
		{
			string type = soldierReader["type"].ReadVal(_mod.SoldiersList[0]);
			if (_mod.GetSoldier(type) != null)
			{
				var s = new Soldier(_mod.GetSoldier(type), null, 0 /*nationality*/);
				s.Load(soldierReader, _mod, save);
				s.SetCraft(null);
				var craftIdReader = soldierReader["craft"];
				if (craftIdReader)
				{
					var craftId = Craft.LoadId(craftIdReader);
					foreach (var xcraft in _crafts)
					{
						if (xcraft.UniqueId == craftId)
						{
							s.SetCraft(xcraft);
							break;
						}
					}
				}
				_soldiers.Add(s);
			}
			else
			{
				Logger.Error($"Failed to load soldier {type}");
			}
		}

		_items.Load(reader["items"], _mod);

		reader.TryRead("scientists", ref _scientists);
		reader.TryRead("engineers", ref _engineers);
		reader.TryRead("inBattlescape", ref _inBattlescape);

		foreach (var transfersReader in reader["transfers"].Children())
		{
			int hours = transfersReader["hours"].ReadVal<int>();
			var t = new Transfer(hours);
			if (t.Load(transfersReader, this, _mod, save))
			{
				_transfers.Add(t);
			}
		}
		foreach (var researchReader in reader["research"].Children())
		{
			string research = researchReader["project"].ReadVal<string>();
			if (_mod.GetResearch(research) != null)
			{
				var r = new ResearchProject(_mod.GetResearch(research));
				r.Load(researchReader);
				_research.Add(r);
			}
			else
			{
				_scientists += researchReader["assigned"].ReadVal(0);
				Logger.Error($"Failed to load research {research}");
			}
		}
		foreach (var productionReader in reader["productions"].Children())
		{
			string item = productionReader["item"].ReadVal<string>();
			if (_mod.GetManufacture(item) != null)
			{
				var p = new Production(_mod.GetManufacture(item), 0);
				p.Load(productionReader);
				_productions.Add(p);
			}
			else
			{
				_engineers += productionReader["assigned"].ReadVal(0);
				Logger.Error($"Failed to load manufacture {item}");
			}
		}

		reader.TryRead("retaliationTarget", ref _retaliationTarget);
		var missionIdReader = reader["retaliationMissionUniqueId"];
		if (missionIdReader)
		{
			int missionId = missionIdReader.ReadVal<int>();
			foreach (var am in save.AlienMissions)
			{
				if (am.Id == missionId)
				{
					_retaliationMission = am;
					break;
				}
			}
		}
		reader.TryRead("fakeUnderwater", ref _fakeUnderwater);

		_ = IsOverlappingOrOverflowing; // don't crash, just report in the log file...
	}

	/// <summary>Finishes loading the base (more specifically all craft in the base) from YAML.</summary>
	/// <param name="reader">YAML node.</param>
	/// <param name="save">Pointer to saved game.</param>
	public void FinishLoading(YamlNodeReader reader, SavedGame save)
	{
		foreach (var craftsReader in reader["crafts"].Children())
		{
			int id = craftsReader["id"].ReadVal<int>();
			string type = craftsReader["type"].ReadVal<string>();
			if (_mod.GetCraft(type) != null)
			{
				Craft craft = null;
				foreach (var xcraft in _crafts)
				{
					if (xcraft.Id == id && xcraft.Rules.Type == type)
					{
						craft = xcraft;
						break;
					}
				}
				if (craft != null)
				{
					craft.FinishLoading(craftsReader, save);
				}
			}
			else
			{
				Logger.Error($"Failed to load craft {type}");
			}
		}
		CalculateServices(save);
	}

	/// <summary>Pre-calculates base services provided by region and country.</summary>
	public void CalculateServices(SavedGame save)
	{
		foreach (var country in save.Countries)
		{
			if (country.Rules.InsideCountry(_lon, _lat))
			{
				_provideBaseFunc |= country.Rules.ProvidedBaseFunc;
				_forbiddenBaseFunc |= country.Rules.ForbiddenBaseFunc;
				break;
			}
		}
		foreach (var region in save.Regions)
		{
			if (region.Rules.InsideRegion(_lon, _lat))
			{
				_provideBaseFunc |= region.Rules.ProvidedBaseFunc;
				_forbiddenBaseFunc |= region.Rules.ForbiddenBaseFunc;
				break;
			}
		}
	}

	/// <summary>Tests whether the base facilities are within the base boundaries and not overlapping.</summary>
	/// <returns>True if the base has a problem.</returns>
	public bool IsOverlappingOrOverflowing
	{
		get
		{
			bool result = false;
			var grid = new BaseFacility[BASE_SIZE, BASE_SIZE];

			foreach (var fac in _facilities)
			{
				var rules = fac.Rules;
				int facilityX = fac.X;
				int facilityY = fac.Y;
				int facilitySizeX = rules.SizeX;
				int facilitySizeY = rules.SizeY;

				if (facilityX < 0 || facilityY < 0 || facilityX + (facilitySizeX - 1) >= BASE_SIZE || facilityY + (facilitySizeY - 1) >= BASE_SIZE)
				{
					Logger.Error(string.Create(CultureInfo.InvariantCulture,
						$"Facility {rules.Type} at [{facilityX}, {facilityY}] (size [{facilitySizeX}, {facilitySizeY}]) is outside of base boundaries."));
					result = true;
					continue;
				}

				for (int x = facilityX; x < facilityX + facilitySizeX; ++x)
				{
					for (int y = facilityY; y < facilityY + facilitySizeY; ++y)
					{
						if (grid[x, y] != null)
						{
							Logger.Error(string.Create(CultureInfo.InvariantCulture,
								$"Facility {rules.Type} at [{facilityX}, {facilityY}] (size [{facilitySizeX}, {facilitySizeY}]) overlaps with {grid[x, y].Rules.Type} at [{x}, {y}] (size [{grid[x, y].Rules.SizeX}, {grid[x, y].Rules.SizeY})"));
							result = true;
						}
						grid[x, y] = fac;
					}
				}
			}

			return result;
		}
	}

	/// <summary>Saves the base to YAML.</summary>
	public override void Save(YamlNodeWriter writer)
	{
		writer.SetAsMap();
		base.Save(writer);
		writer.Write("facilities", _facilities,
			(vectorWriter, f) => f.Save(vectorWriter.Write()));
		writer.Write("soldiers", _soldiers,
			(vectorWriter, s) => s.Save(vectorWriter.Write(), _mod.ScriptGlobal));
		writer.Write("crafts", _crafts,
			(vectorWriter, c) => c.Save(vectorWriter.Write(), _mod.ScriptGlobal));
		_items.Save(writer["items"]);
		writer.Write("scientists", _scientists);
		writer.Write("engineers", _engineers);
		if (_inBattlescape)
			writer.Write("inBattlescape", _inBattlescape);
		writer.Write("transfers", _transfers,
			(vectorWriter, t) => t.Save(vectorWriter.Write(), this, _mod));
		writer.Write("research", _research,
			(vectorWriter, r) => r.Save(vectorWriter.Write()));
		writer.Write("productions", _productions,
			(vectorWriter, p) => p.Save(vectorWriter.Write()));
		if (_retaliationTarget)
			writer.Write("retaliationTarget", _retaliationTarget);
		if (_retaliationMission != null)
			writer.Write("retaliationMissionUniqueId", _retaliationMission.Id);
		if (_fakeUnderwater)
			writer.Write("fakeUnderwater", _fakeUnderwater);
	}

	/// <summary>Returns the base's unique type used for savegame purposes.</summary>
	public override string Type => "STR_BASE";

	/// <summary>Returns the custom name for the base.</summary>
	/// <param name="lang">Language to get strings from (unused).</param>
	public override string GetName(Language lang) => _name;

	/// <summary>Returns the globe marker for the base.</summary>
	/// <returns>Marker sprite, -1 if none.</returns>
	public override int Marker
	{
		get
		{
			// Cheap hack to hide bases when they haven't been placed yet
			if (AreSame(_lon, 0.0) && AreSame(_lat, 0.0))
				return -1;
			return 0;
		}
	}

	/// <summary>Returns the list of facilities in the base.</summary>
	public List<BaseFacility> Facilities => _facilities;

	/// <summary>Returns the list of soldiers in the base.</summary>
	public List<Soldier> Soldiers => _soldiers;

	/// <summary>Pre-calculates soldier stats with various bonuses.</summary>
	public void PrepareSoldierStatsWithBonuses()
	{
		foreach (var soldier in _soldiers)
		{
			soldier.PrepareStatsWithBonuses(_mod);
		}
	}

	/// <summary>Gets the base's crafts.</summary>
	public List<Craft> Crafts => _crafts;

	/// <summary>Gets the base's transfers.</summary>
	public List<Transfer> Transfers => _transfers;

	/// <summary>Gets the base's items.</summary>
	public ItemContainer StorageItems => _items;

	/// <summary>The amount of scientists currently in the base.</summary>
	public int Scientists
	{
		get => _scientists;
		set => _scientists = value;
	}

	/// <summary>The amount of engineers currently in the base.</summary>
	public int Engineers
	{
		get => _engineers;
		set => _engineers = value;
	}

	/// <summary>
	/// Returns if a certain target is covered by the base's
	/// radar range, taking in account the range and chance.
	/// </summary>
	/// <param name="target">Pointer to target to compare.</param>
	/// <param name="save">Saved game.</param>
	/// <param name="alreadyTracked">Was ufo already detected, <c>true</c> mean we track it without probability.</param>
	/// <returns>0 - not detected, 1 - detected by conventional radar, 2 - detected by hyper-wave decoder.</returns>
	public UfoDetection Detect(Ufo target, SavedGame save, bool alreadyTracked)
	{
		int distance = Craft.XcomDistance(GetDistance(target));
		bool hyperwave = false;
		int hyperwave_chance = 0;
		int radar_chance = 0;
		int hyperwave_max_range = 0;
		int radar_max_range = 0;

		foreach (var fac in _facilities)
		{
			if (fac.BuildTime != 0)
			{
				continue;
			}
			if (fac.Rules.RadarRange >= distance)
			{
				int radarChance = fac.Rules.RadarChance;
				if (fac.Rules.IsHyperwave)
				{
					if (radarChance == 100 || Rng.Percent(radarChance))
					{
						hyperwave = true;
					}
					hyperwave_chance += radarChance;
				}
				else
				{
					radar_chance += radarChance;
				}
			}
			if (fac.Rules.IsHyperwave)
			{
				hyperwave_max_range = Math.Max(hyperwave_max_range, fac.Rules.RadarRange);
			}
			else
			{
				radar_max_range = Math.Max(radar_max_range, fac.Rules.RadarRange);
			}
		}

		int detectionChance = 0;
		UfoDetection detectionType = UfoDetection.DETECTION_NONE;

		if (alreadyTracked)
		{
			if (hyperwave || hyperwave_chance > 0)
			{
				detectionType = hyperwave ? UfoDetection.DETECTION_HYPERWAVE : UfoDetection.DETECTION_RADAR;
				detectionChance = 100;
			}
			else if (radar_chance > 0)
			{
				detectionType = UfoDetection.DETECTION_RADAR;
				detectionChance = 100;
			}
		}
		else
		{
			if (hyperwave)
			{
				detectionType = UfoDetection.DETECTION_HYPERWAVE;
				detectionChance = 100;
			}
			else if (radar_chance > 0)
			{
				detectionType = UfoDetection.DETECTION_RADAR;
				detectionChance = radar_chance * (100 + target.Visibility) / 100;
			}
		}

		var args = new ScriptOutputArgs((int)detectionType, detectionChance);

		ModScript.Execute(target.Rules.GetScript(ModScript.DetectUfoFromBase), args, target, save, distance, alreadyTracked, radar_chance, radar_max_range, hyperwave_chance, hyperwave_max_range);

		return Rng.Percent(args.GetSecond()) ? (UfoDetection)args.GetFirst() : UfoDetection.DETECTION_NONE;
	}

	/// <summary>Returns the amount of soldiers contained in the base without any assignments.</summary>
	/// <param name="checkCombatReadiness">does what it says on the tin.</param>
	/// <param name="includeWounded">even wounded soldiers can fight in the base defense, if they have enough HP (user option).</param>
	public int GetAvailableSoldiers(bool checkCombatReadiness = false, bool includeWounded = false)
	{
		int total = 0;
		foreach (var soldier in _soldiers)
		{
			if (!checkCombatReadiness && soldier.Craft == null)
			{
				total++;
			}
			else if (checkCombatReadiness && ((soldier.Craft != null && soldier.Craft.Status != "STR_OUT") ||
				(soldier.Craft == null && (soldier.HasFullHealth || (includeWounded && soldier.CanDefendBase())))))
			{
				total++;
			}
		}
		return total;
	}

	/// <summary>Returns the total amount of soldiers contained in the base.</summary>
	public int TotalSoldiers
	{
		get
		{
			int total = _soldiers.Count;
			foreach (var transfer in _transfers)
			{
				if (transfer.Type == TransferType.TRANSFER_SOLDIER)
				{
					total += transfer.Quantity;
				}
			}
			return total;
		}
	}

	/// <summary>Returns the amount of scientists contained in the base without any assignments.</summary>
	public int AvailableScientists => Scientists;

	/// <summary>Returns the total amount of scientists contained in the base.</summary>
	public int TotalScientists
	{
		get
		{
			int total = _scientists;
			foreach (var transfer in _transfers)
			{
				if (transfer.Type == TransferType.TRANSFER_SCIENTIST)
				{
					total += transfer.Quantity;
				}
			}
			foreach (var proj in _research)
			{
				total += proj.Assigned;
			}
			return total;
		}
	}

	/// <summary>Returns the amount of engineers contained in the base without any assignments.</summary>
	public int AvailableEngineers => Engineers;

	/// <summary>Returns the total amount of engineers contained in the base.</summary>
	public int TotalEngineers
	{
		get
		{
			int total = _engineers;
			foreach (var transfer in _transfers)
			{
				if (transfer.Type == TransferType.TRANSFER_ENGINEER)
				{
					total += transfer.Quantity;
				}
			}
			foreach (var prod in _productions)
			{
				total += prod.AssignedEngineers;
			}
			return total;
		}
	}

	/// <summary>Returns the total cost of other staff &amp; inventory contained in the base.</summary>
	/// <returns>Total cost.</returns>
	public int GetTotalOtherStaffAndInventoryCost(ref int staffCount, ref int inventoryCount)
	{
		staffCount = 0;
		inventoryCount = 0;
		int totalCost = 0;

		foreach (var transfer in _transfers)
		{
			if (transfer.Type == TransferType.TRANSFER_ITEM)
			{
				var ruleItem = transfer.Items;
				if (ruleItem.MonthlySalary != 0)
				{
					staffCount += transfer.Quantity;
					totalCost += ruleItem.MonthlySalary * transfer.Quantity;
				}
				if (ruleItem.MonthlyMaintenance != 0)
				{
					inventoryCount += transfer.Quantity;
					totalCost += ruleItem.MonthlyMaintenance * transfer.Quantity;
				}
			}
			else if (transfer.Type == TransferType.TRANSFER_SOLDIER)
			{
				var ruleItem = transfer.Soldier.Armor.StoreItem;
				if (ruleItem != null && ruleItem.MonthlySalary != 0)
				{
					staffCount += 1;
					totalCost += ruleItem.MonthlySalary;
				}
				if (ruleItem != null && ruleItem.MonthlyMaintenance != 0)
				{
					inventoryCount += 1;
					totalCost += ruleItem.MonthlyMaintenance;
				}
			}
		}
		foreach (var storeItem in _items.Contents)
		{
			var ruleItem = storeItem.Key;
			if (ruleItem.MonthlySalary != 0)
			{
				staffCount += storeItem.Value;
				totalCost += ruleItem.MonthlySalary * storeItem.Value;
			}
			if (ruleItem.MonthlyMaintenance != 0)
			{
				inventoryCount += storeItem.Value;
				totalCost += ruleItem.MonthlyMaintenance * storeItem.Value;
			}
		}
		foreach (var xcraft in _crafts)
		{
			foreach (var craftItem in xcraft.Items.Contents)
			{
				var ruleItem = craftItem.Key;
				if (ruleItem.MonthlySalary != 0)
				{
					staffCount += craftItem.Value;
					totalCost += ruleItem.MonthlySalary * craftItem.Value;
				}
				if (ruleItem.MonthlyMaintenance != 0)
				{
					inventoryCount += craftItem.Value;
					totalCost += ruleItem.MonthlyMaintenance * craftItem.Value;
				}
			}
			foreach (var vehicle in xcraft.Vehicles)
			{
				var ruleItem = vehicle.Rules;
				if (ruleItem.MonthlySalary != 0)
				{
					staffCount += 1;
					totalCost += ruleItem.MonthlySalary;
				}
				if (ruleItem.MonthlyMaintenance != 0)
				{
					inventoryCount += 1;
					totalCost += ruleItem.MonthlyMaintenance;
				}
			}
		}
		foreach (var soldier in _soldiers)
		{
			var ruleItem = soldier.Armor.StoreItem;
			if (ruleItem != null && ruleItem.MonthlySalary != 0)
			{
				staffCount += 1;
				totalCost += ruleItem.MonthlySalary;
			}
			if (ruleItem != null && ruleItem.MonthlyMaintenance != 0)
			{
				inventoryCount += 1;
				totalCost += ruleItem.MonthlyMaintenance;
			}
		}

		return totalCost;
	}

	/// <summary>Returns the amount of living quarters used up by personnel in the base.</summary>
	public int UsedQuarters
	{
		get
		{
			int total = TotalSoldiers + TotalScientists + TotalEngineers;
			foreach (var prod in _productions)
			{
				if (prod.Rules.SpawnedPersonType != "")
				{
					// reserve one living space for each production project (even if it's on hold)
					total += 1;
				}
			}
			return total;
		}
	}

	/// <summary>Returns the total amount of living quarters available in the base.</summary>
	public int AvailableQuarters
	{
		get
		{
			int total = 0;
			foreach (var fac in _facilities)
			{
				if (fac.BuildTime == 0)
				{
					total += fac.Rules.Personnel;
				}
			}
			return total;
		}
	}

	/// <summary>Returns the amount of stores used up by equipment in the base, and equipment about to arrive.</summary>
	/// <returns>Storage space.</returns>
	public double GetUsedStores(bool excludeNormalItems = false)
	{
		double total = excludeNormalItems ? 0.0 : _items.TotalSize;
		foreach (var xcraft in _crafts)
		{
			total += xcraft.TotalItemStorageSize;
		}
		foreach (var transfer in _transfers)
		{
			if (transfer.Type == TransferType.TRANSFER_ITEM)
			{
				total += transfer.Quantity * transfer.Items.Size;
			}
			else if (transfer.Type == TransferType.TRANSFER_CRAFT)
			{
				total += transfer.Craft.TotalItemStorageSize;
			}
		}
		return total;
	}

	/// <summary>
	/// Checks if the base's stores are overfull.
	/// Supplying an offset will add/subtract to the used capacity before performing the check.
	/// A positive offset simulates adding items to the stores, whereas a negative offset
	/// can be used to check whether sufficient items have been removed to stop the stores overflowing.
	/// </summary>
	/// <param name="offset">Adjusts the used capacity.</param>
	/// <returns>True if the base's stores are over their limit.</returns>
	public bool StoresOverfull(double offset = 0.0)
	{
		int capacity = AvailableStores * 100;
		double used = (GetUsedStores() + offset) * 100;
		return (int)used > capacity;
	}

	/// <summary>Checks if the base's stores are so full that even craft equipment and incoming transfers can't fit.</summary>
	public bool StoresOverfullCritical()
	{
		int capacity = AvailableStores * 100;
		double total = GetUsedStores(true);
		int used = (int)(total * 100);
		return used > capacity;
	}

	/// <summary>Returns the total amount of stores available in the base.</summary>
	public int AvailableStores
	{
		get
		{
			int total = 0;
			foreach (var fac in _facilities)
			{
				if (fac.BuildTime == 0)
				{
					total += fac.Rules.Storage;
				}
			}
			return total;
		}
	}

	/// <summary>Returns the amount of laboratories used up by research projects in the base.</summary>
	public int UsedLaboratories
	{
		get
		{
			int usedLabSpace = 0;
			foreach (var proj in _research)
			{
				usedLabSpace += proj.Assigned;
			}
			return usedLabSpace;
		}
	}

	/// <summary>Returns the total amount of laboratories available in the base.</summary>
	public int AvailableLaboratories
	{
		get
		{
			int total = 0;
			foreach (var fac in _facilities)
			{
				if (fac.BuildTime == 0)
				{
					total += fac.Rules.Laboratories;
				}
			}
			return total;
		}
	}

	/// <summary>Returns the amount of workshops used up by manufacturing projects in the base.</summary>
	public int UsedWorkshops
	{
		get
		{
			int usedWorkShop = 0;
			foreach (var prod in _productions)
			{
				usedWorkShop += prod.AssignedEngineers;

				// don't count the workshop space yet if the production is only queued (for future)
				if (!prod.IsQueuedOnly)
				{
					usedWorkShop += prod.Rules.RequiredSpace;
				}
			}
			return usedWorkShop;
		}
	}

	/// <summary>Returns the total amount of workshops available in the base.</summary>
	public int AvailableWorkshops
	{
		get
		{
			int total = 0;
			foreach (var fac in _facilities)
			{
				if (fac.BuildTime == 0)
				{
					total += fac.Rules.Workshops;
				}
			}
			return total;
		}
	}

	/// <summary>Returns the amount of hangars used up by crafts in the base.</summary>
	public int UsedHangars
	{
		get
		{
			// C++ sums in size_t; the result is converted back to int (same value modulo 2^32)
			int total = _crafts.Count;
			foreach (var transfer in _transfers)
			{
				if (transfer.Type == TransferType.TRANSFER_CRAFT)
				{
					total += transfer.Quantity;
				}
			}
			foreach (var prod in _productions)
			{
				if (prod.Rules.ProducedCraft != null)
				{
					// This should be fixed on the case when prod->getInfiniteAmount() == TRUE
					total += prod.AmountTotal - prod.AmountProduced;
				}
			}
			return total;
		}
	}

	/// <summary>Returns the total amount of hangars available in the base.</summary>
	public int AvailableHangars
	{
		get
		{
			int total = 0;
			foreach (var fac in _facilities)
			{
				if (fac.BuildTime == 0)
				{
					total += fac.Rules.Crafts;
				}
			}
			return total;
		}
	}

	/// <summary>Return laboratories space not used by a ResearchProject.</summary>
	public int FreeLaboratories => AvailableLaboratories - UsedLaboratories;

	/// <summary>Return workshop space not used by a Production.</summary>
	public int FreeWorkshops => AvailableWorkshops - UsedWorkshops;

	/// <summary>Return psi lab space not in use.</summary>
	public int FreePsiLabs => AvailablePsiLabs - UsedPsiLabs;

	/// <summary>Return containment space not in use.</summary>
	public int GetFreeContainment(int prisonType) => GetAvailableContainment(prisonType) - GetUsedContainment(prisonType);

	/// <summary>Returns the amount of scientists currently in use.</summary>
	public int AllocatedScientists
	{
		get
		{
			int total = 0;
			foreach (var proj in _research)
			{
				total += proj.Assigned;
			}
			return total;
		}
	}

	/// <summary>Returns the amount of engineers currently in use.</summary>
	public int AllocatedEngineers
	{
		get
		{
			int total = 0;
			foreach (var prod in _productions)
			{
				total += prod.AssignedEngineers;
			}
			return total;
		}
	}

	/// <summary>Returns the total defense value of all the facilities in the base.</summary>
	public int DefenseValue
	{
		get
		{
			int total = 0;
			foreach (var fac in _facilities)
			{
				if (fac.BuildTime == 0)
				{
					total += fac.Rules.DefenseValue;
				}
			}
			return total;
		}
	}

	/// <summary>Returns the total amount of short range detection facilities in the base.</summary>
	public int ShortRangeDetection
	{
		get
		{
			int total = 0;
			int minRadarRange = _mod.ShortRadarRange;

			if (minRadarRange == 0) return 0;
			foreach (var fac in _facilities)
			{
				if (fac.Rules.RadarRange > 0 && fac.Rules.RadarRange <= minRadarRange && fac.BuildTime == 0)
				{
					total++;
				}
			}
			return total;
		}
	}

	/// <summary>Returns the total amount of long range detection facilities in the base.</summary>
	public int LongRangeDetection
	{
		get
		{
			int total = 0;
			int minRadarRange = _mod.ShortRadarRange;

			foreach (var fac in _facilities)
			{
				if (fac.Rules.RadarRange > minRadarRange && fac.BuildTime == 0)
				{
					total++;
				}
			}
			return total;
		}
	}

	/// <summary>Returns the total amount of craft of a certain type stored in the base.</summary>
	/// <param name="craft">Craft type.</param>
	public int GetCraftCount(RuleCraft craft)
	{
		int total = 0;
		foreach (var transfer in _transfers)
		{
			if (transfer.Type == TransferType.TRANSFER_CRAFT && transfer.Craft.Rules == craft)
			{
				total++;
			}
		}
		foreach (var xcraft in _crafts)
		{
			if (xcraft.Rules == craft)
			{
				total++;
			}
		}
		return total;
	}

	/// <summary>Gets the base's crafts of a certain type (not out on a mission).</summary>
	public int GetCraftCountForProduction(RuleCraft craft)
	{
		int total = 0;
		foreach (var xcraft in _crafts)
		{
			if (xcraft.Rules == craft && xcraft.Status != "STR_OUT")
			{
				total++;
			}
		}
		return total;
	}

	/// <summary>Returns the total amount of monthly costs for maintaining the craft in the base.</summary>
	public int CraftMaintenance
	{
		get
		{
			int total = 0;
			foreach (var transfer in _transfers)
			{
				if (transfer.Type == TransferType.TRANSFER_CRAFT)
				{
					total += transfer.Craft.Rules.RentCost;
				}
			}
			foreach (var xcraft in _crafts)
			{
				total += xcraft.Rules.RentCost;
			}
			return total;
		}
	}

	/// <summary>Returns the total count and total salary of soldiers of a certain type stored in the base.</summary>
	/// <param name="soldier">Soldier type.</param>
	/// <returns>Number of soldiers and their salary.</returns>
	public (int first, int second) GetSoldierCountAndSalary(string soldier)
	{
		int total = 0;
		int totalSalary = 0;
		foreach (var transfer in _transfers)
		{
			if (transfer.Type == TransferType.TRANSFER_SOLDIER && transfer.Soldier.Rules.Type == soldier)
			{
				total++;
				totalSalary += transfer.Soldier.Rules.GetSalaryCost((int)transfer.Soldier.Rank);
			}
		}
		foreach (var xsoldier in _soldiers)
		{
			if (xsoldier.Rules.Type == soldier)
			{
				total++;
				totalSalary += xsoldier.Rules.GetSalaryCost((int)xsoldier.Rank);
			}
		}
		return (total, totalSalary);
	}

	/// <summary>Returns the total amount of monthly costs for maintaining the personnel in the base.</summary>
	public int PersonnelMaintenance
	{
		get
		{
			int total = 0;
			foreach (var transfer in _transfers)
			{
				if (transfer.Type == TransferType.TRANSFER_SOLDIER)
				{
					total += transfer.Soldier.Rules.GetSalaryCost((int)transfer.Soldier.Rank);
				}
			}
			foreach (var soldier in _soldiers)
			{
				total += soldier.Rules.GetSalaryCost((int)soldier.Rank);
			}
			total += TotalEngineers * _mod.EngineerCost;
			total += TotalScientists * _mod.ScientistCost;
			int dummy1 = 0, dummy2 = 0;
			total += GetTotalOtherStaffAndInventoryCost(ref dummy1, ref dummy2); // other staff & inventory
			return total;
		}
	}

	/// <summary>Returns the total amount of monthly costs for maintaining the facilities in the base.</summary>
	public int FacilityMaintenance
	{
		get
		{
			int total = 0;
			foreach (var fac in _facilities)
			{
				if (fac.BuildTime == 0)
				{
					total += fac.Rules.MonthlyCost;
				}
			}
			return total;
		}
	}

	/// <summary>Returns the total amount of all the maintenance monthly costs in the base.</summary>
	public int MonthlyMaintenace => CraftMaintenance + PersonnelMaintenance + FacilityMaintenance;

	/// <summary>Returns the list of all base's ResearchProject.</summary>
	public List<ResearchProject> Research => _research;

	/// <summary>Add A new ResearchProject to Base.</summary>
	/// <param name="arg0">The project to add.</param>
	public void AddResearch(ResearchProject arg0)
	{
		_research.Add(arg0);
	}

	/// <summary>Remove a ResearchProject from base.</summary>
	/// <param name="arg0">the project to remove.</param>
	public void RemoveResearch(ResearchProject arg0)
	{
		var project = arg0;
		_scientists += project.Assigned;
		var ruleResearch = project.Rules;
		if (ruleResearch.IsHoldingNeededItem)
		{
			if (!project.IsFinished)
			{
				StorageItems.AddItem(ruleResearch.NeededItem, 1);
			}
			else
			{
				// handle interrogation
				if (ruleResearch.ReturnItem())
				{
					StorageItems.AddItem(ruleResearch.NeededItem, 1);
				}
				else if (Options.retainCorpses && ruleResearch.DestroyItem())
				{
					var ruleUnit = _mod.GetUnit(ruleResearch.Name, false); // research & item & unit should have same name to work correctly
					if (ruleUnit != null)
					{
						var ruleCorpse = ruleUnit.Armor.CorpseGeoscape;
						if (ruleCorpse != null && ruleCorpse.IsRecoverable && ruleCorpse.IsCorpseRecoverable)
						{
							StorageItems.AddItem(ruleCorpse);
						}
					}
				}
			}
		}

		DeleteIf(_research, 1, r => r == project);
	}

	/// <summary>Add a new Production to the Base.</summary>
	public void AddProduction(Production p)
	{
		_productions.Add(p);
	}

	/// <summary>Remove a Production from the Base.</summary>
	public void RemoveProduction(Production p)
	{
		_engineers += p.AssignedEngineers;

		DeleteIf(_productions, 1, r => r == p);
	}

	/// <summary>Get the list of Base Production's.</summary>
	public List<Production> Productions => _productions;

	/// <summary>Returns the total amount of used Psi Lab Space in the base.</summary>
	public int UsedPsiLabs
	{
		get
		{
			int total = 0;
			foreach (var soldier in _soldiers)
			{
				if (soldier.IsInPsiTraining)
				{
					total++;
				}
			}
			// Only soldiers returning home after being shot down by a HK can ever be in psi training while in transfer
			foreach (var transfer in _transfers)
			{
				if (transfer.Type == TransferType.TRANSFER_SOLDIER)
				{
					if (transfer.Soldier.IsInPsiTraining)
					{
						total++;
					}
				}
			}
			return total;
		}
	}

	/// <summary>Returns the total amount of Psi Lab Space available in the base.</summary>
	public int AvailablePsiLabs
	{
		get
		{
			int total = 0;
			foreach (var fac in _facilities)
			{
				if (fac.BuildTime == 0)
				{
					total += fac.Rules.PsiLaboratories;
				}
			}
			return total;
		}
	}

	/// <summary>Returns the total amount of used training space in the base.</summary>
	public int UsedTraining
	{
		get
		{
			int total = 0;
			foreach (var soldier in _soldiers)
			{
				if (soldier.IsInTraining)
				{
					total++;
				}
			}
			return total;
		}
	}

	/// <summary>Returns the total amount of training space available in the base.</summary>
	public int AvailableTraining
	{
		get
		{
			int total = 0;
			foreach (var fac in _facilities)
			{
				if (fac.BuildTime == 0)
				{
					total += fac.Rules.TrainingFacilities;
				}
			}
			return total;
		}
	}

	/// <summary>Returns training space not in use.</summary>
	public int FreeTrainingSpace => AvailableTraining - UsedTraining;

	/// <summary>Returns the total amount of Containment Space available in the base.</summary>
	public int GetAvailableContainment(int prisonType)
	{
		int total = 0;
		foreach (var fac in _facilities)
		{
			if (fac.BuildTime == 0 && fac.Rules.PrisonType == prisonType)
			{
				total += fac.Rules.Aliens;
			}
		}
		return total;
	}

	/// <summary>Returns the total amount of used Containment Space in the base.</summary>
	public int GetUsedContainment(int prisonType, bool onlyExternal = false)
	{
		int total = 0;
		RuleItem rule;
		foreach (var transfer in _transfers)
		{
			if (transfer.Type == TransferType.TRANSFER_ITEM)
			{
				rule = transfer.Items;
				if (rule.IsAlien && rule.PrisonType == prisonType)
				{
					total += transfer.Quantity;
				}
			}
		}
		foreach (var proj in _research)
		{
			var projRules = proj.Rules;
			if (projRules.IsHoldingNeededItem)
			{
				rule = projRules.NeededItem;
				if (rule != null && rule.IsAlien && rule.PrisonType == prisonType)
				{
					++total;
				}
			}
		}
		if (onlyExternal)
		{
			return total;
		}

		foreach (var pair in _items.Contents)
		{
			rule = pair.Key;
			if (rule.IsAlien && rule.PrisonType == prisonType)
			{
				total += pair.Value;
			}
		}
		return total;
	}

	/// <summary>Changes the base's battlescape status.</summary>
	/// <param name="inbattle">True if it's in battle, False otherwise.</param>
	public void SetInBattlescape(bool inbattle)
	{
		_inBattlescape = inbattle;
	}

	/// <summary>Returns the base's battlescape status.</summary>
	public bool IsInBattlescape => _inBattlescape;

	/// <summary>Mark the base as a valid alien retaliation target.</summary>
	/// <param name="mark">Mark (if <c>true</c>) or unmark (if <c>false</c>) the base.</param>
	public void SetRetaliationTarget(bool mark = true)
	{
		_retaliationTarget = mark;
	}

	/// <summary>Get the base's retaliation status.</summary>
	public bool RetaliationTarget => _retaliationTarget;

	/// <summary>The corresponding alien retaliation mission.</summary>
	public AlienMission RetaliationMission
	{
		get => _retaliationMission;
		set => _retaliationMission = value;
	}

	/// <summary>Mark/unmark this base as a fake underwater base.</summary>
	public void SetFakeUnderwater(bool fakeUnderwater) => _fakeUnderwater = fakeUnderwater;

	/// <summary>Is this a fake underwater base?</summary>
	public bool IsFakeUnderwater => _fakeUnderwater;

	/// <summary>
	/// Calculate the detection chance of this base.
	/// Big bases without mind shields are easier to detect.
	/// </summary>
	/// <remarks>C++ computes in <c>size_t</c>; the same unsigned arithmetic is used here.</remarks>
	public int DetectionChance
	{
		get
		{
			ulong mindShields = 0;
			ulong completedFacilities = 0;
			foreach (var fac in _facilities)
			{
				if (fac.BuildTime == 0)
				{
					completedFacilities += (ulong)(long)(fac.Rules.SizeX * fac.Rules.SizeY);
					if (fac.Rules.IsMindShield && !fac.Disabled)
					{
						mindShields += (ulong)(long)fac.Rules.MindShieldPower;
					}
				}
			}
			return (int)((completedFacilities / 6 + 15) / (mindShields + 1));
		}
	}

	/// <summary>Gets how many Grav Shields the base has.</summary>
	public int GravShields
	{
		get
		{
			int total = 0;
			foreach (var fac in _facilities)
			{
				if (fac.BuildTime == 0 && fac.Rules.IsGravShield)
				{
					++total;
				}
			}
			return total;
		}
	}

	/// <summary>Setup base defenses.</summary>
	public void SetupDefenses(AlienMission am)
	{
		// Note: OBJECTIVE_INSTANT_RETALIATION is intentionally ignored here
		if (am.Rules.Objective == MissionObjective.OBJECTIVE_RETALIATION)
		{
			RetaliationMission = am;
		}

		_defenses.Clear();
		foreach (var fac in _facilities)
		{
			if (fac.BuildTime == 0 && fac.Rules.DefenseValue != 0)
			{
				_defenses.Add(fac);
			}
		}

		_vehicles.Clear();
		_vehiclesFromBase.Clear();

		// add vehicles that are in the crafts of the base, if it's not out
		foreach (var xcraft in _crafts)
		{
			if (xcraft.Status != "STR_OUT")
			{
				foreach (var vehicle in xcraft.Vehicles)
				{
					_vehicles.Add(vehicle);
				}
			}
		}

		// add vehicles left on the base
		// C++ restarts the map iteration after every removal ("iterator is broken"); the scan restarts the same way here.
		bool restart;
		do
		{
			restart = false;
			foreach (var pair in _items.Contents)
			{
				int itemQty = pair.Value;
				var rule = pair.Key;
				if (rule.VehicleUnit == null)
				{
					continue;
				}
				int size = rule.VehicleUnit.Armor.TotalSize;
				if (rule.VehicleClipAmmo == null) // so this vehicle does not need ammo
				{
					for (int j = 0; j < itemQty; ++j)
					{
						var vehicle = new Vehicle(rule, rule.VehicleClipSize, size);
						_vehicles.Add(vehicle);
						_vehiclesFromBase.Add(vehicle);
					}
					_items.RemoveItem(rule, itemQty);
				}
				else // so this vehicle needs ammo
				{
					var ammo = rule.VehicleClipAmmo;
					int ammoPerVehicle = rule.VehicleClipsLoaded;

					int baseQty = _items.GetItem(ammo) / ammoPerVehicle;
					if (baseQty == 0)
					{
						continue;
					}
					int canBeAdded = Math.Min(itemQty, baseQty);
					for (int j = 0; j < canBeAdded; ++j)
					{
						var vehicle = new Vehicle(rule, rule.VehicleClipSize, size);
						_vehicles.Add(vehicle);
						_vehiclesFromBase.Add(vehicle);
						_items.RemoveItem(ammo, ammoPerVehicle);
					}
					_items.RemoveItem(rule, canBeAdded);
				}

				restart = true; // we have to start over because iterator is broken because of the removeItem
				break;
			}
		}
		while (restart);
	}

	/// <summary>Get a list of Defensive Facilities.</summary>
	public List<BaseFacility> Defenses => _defenses;

	/// <summary>Returns the list of vehicles currently equipped in the base.</summary>
	public List<Vehicle> Vehicles => _vehicles;

	/// <summary>Gets the list of recently destroyed base facilities.</summary>
	public Dictionary<RuleBaseFacility, int> DestroyedFacilitiesCache => _destroyedFacilitiesCache;

	/// <summary>Damage and/or destroy facilities after a missile impact.</summary>
	/// <param name="ufo">The missile that hit the base.</param>
	public void DamageFacilities(Ufo ufo)
	{
		_destroyedFacilitiesCache.Clear();

		for (int i = 0; i < ufo.Rules.MissilePower;)
		{
			var options = new WeightedOptions();
			int index = 0;
			foreach (var facility in _facilities)
			{
				if (facility.Rules.MissileAttraction > 0 && !facility.Rules.IsLift)
				{
					options.Set(index.ToString(CultureInfo.InvariantCulture), facility.Rules.MissileAttraction);
				}
				++index;
			}
			if (options.Empty())
			{
				// only indestructible stuff remains, stop trying
				break;
			}

			string sel = options.Choose();
			int selected = int.Parse(sel, CultureInfo.InvariantCulture);
			var toBeDamaged = _facilities[selected];

			i += DamageFacility(toBeDamaged);
		}

		// this may cause the base to become disjointed, destroy the disconnected parts
		if (_mod.DestroyedFacility == null)
		{
			DestroyDisconnectedFacilities();
		}
	}

	/// <summary>Damage a given facility.</summary>
	/// <param name="toBeDamaged">The facility to be damaged.</param>
	/// <returns>Missile power spent on this facility.</returns>
	public int DamageFacility(BaseFacility toBeDamaged)
	{
		int result = 0;

		// 1. Create the new "damaged facility" first, so that when we destroy the original facility we don't lose "too much"
		if (toBeDamaged.Rules.DestroyedFacility != null)
		{
			var fac = new BaseFacility(toBeDamaged.Rules.DestroyedFacility, this);
			fac.X = toBeDamaged.X;
			fac.Y = toBeDamaged.Y;
			fac.BuildTime = 0;
			_facilities.Add(fac);

			// move the craft from the original hangar to the damaged hangar
			if (fac.Rules.Crafts > 0)
			{
				fac.CraftForDrawing = toBeDamaged.CraftForDrawing;
				toBeDamaged.CraftForDrawing = null;
			}
		}
		else if (_mod.DestroyedFacility != null)
		{
			for (int x = 0; x < toBeDamaged.Rules.SizeX; ++x)
			{
				for (int y = 0; y < toBeDamaged.Rules.SizeY; ++y)
				{
					var fac = new BaseFacility(_mod.DestroyedFacility, this);
					fac.X = toBeDamaged.X + x;
					fac.Y = toBeDamaged.Y + y;
					fac.BuildTime = 0;
					_facilities.Add(fac);
				}
			}
		}

		// 2. Now destroy the original
		for (int iter = 0; iter < _facilities.Count; ++iter)
		{
			if (_facilities[iter] == toBeDamaged)
			{
				// bigger facilities spend more missile power
				result = toBeDamaged.Rules.SizeX * toBeDamaged.Rules.SizeY;
				DestroyFacility(iter);
				break;
			}
		}

		return result;
	}

	/// <summary>Destroys all disconnected facilities in the base.</summary>
	public void DestroyDisconnectedFacilities()
	{
		var disFacs = GetDisconnectedFacilities(null);
		// back to front, so the remaining indices stay valid (like the C++ reverse iteration)
		for (int outer = disFacs.Count - 1; outer >= 0; --outer)
		{
			DestroyFacility(disFacs[outer]);
		}
	}

	/// <summary>Connection state of one base grid square (C++ <c>std::pair&lt;iterator, bool&gt;</c>).</summary>
	private sealed class FacilityConnState
	{
		public int Facility;
		public bool Connected;
	}

	/// <summary>Gets a sorted list of the facilities (indices into <see cref="Facilities"/>) NOT connected to the Access Lift.</summary>
	/// <param name="remove">Facility to ignore (in case of facility dismantling).</param>
	/// <returns>A sorted list of indices of elements in <see cref="Facilities"/>.</returns>
	public List<int> GetDisconnectedFacilities(BaseFacility remove)
	{
		var result = new List<int>();

		if (remove != null && remove.Rules.IsLift)
		{ // Theoretically this is impossible, but sanity check is good :)
			for (int iter = 0; iter < _facilities.Count; ++iter)
			{
				if (_facilities[iter] != remove) result.Add(iter);
			}
			return result;
		}

		var facilitiesConnStates = new List<FacilityConnState>();
		var grid = new FacilityConnState[BASE_SIZE, BASE_SIZE];
		BaseFacility lift = null;

		// Ok, fill up the grid(+facilitiesConnStates), and search the lift
		for (int iter = 0; iter < _facilities.Count; ++iter)
		{
			var fac = _facilities[iter];
			if (fac != remove)
			{
				if (fac.Rules.IsLift) lift = fac;
				for (int x = 0; x != fac.Rules.SizeX; ++x)
				{
					for (int y = 0; y != fac.Rules.SizeY; ++y)
					{
						var p = new FacilityConnState { Facility = iter, Connected = false };
						facilitiesConnStates.Add(p);
						grid[fac.X + x, fac.Y + y] = p;
					}
				}
			}
		}

		// we're in real trouble if this happens...
		if (lift == null)
		{
			//TODO: something clever.
			return result;
		}

		// Now make the recursion manually using a stack
		var stack = new Stack<(int first, int second)>();
		stack.Push((lift.X, lift.Y));
		while (stack.Count != 0)
		{
			var (x, y) = stack.Pop();
			if (x >= 0 && x < BASE_SIZE && y >= 0 && y < BASE_SIZE && grid[x, y] != null && !grid[x, y].Connected)
			{
				grid[x, y].Connected = true;
				var fac = _facilities[grid[x, y].Facility];
				var neighborLeft = (x - 1 >= 0 && grid[x - 1, y] != null) ? _facilities[grid[x - 1, y].Facility] : null;
				var neighborRight = (x + 1 < BASE_SIZE && grid[x + 1, y] != null) ? _facilities[grid[x + 1, y].Facility] : null;
				var neighborTop = (y - 1 >= 0 && grid[x, y - 1] != null) ? _facilities[grid[x, y - 1].Facility] : null;
				var neighborBottom = (y + 1 < BASE_SIZE && grid[x, y + 1] != null) ? _facilities[grid[x, y + 1].Facility] : null;
				if (fac.IsBuiltOrHadPreviousFacility || (neighborLeft != null && (neighborLeft == fac || neighborLeft.BuildTime > neighborLeft.Rules.BuildTime))) stack.Push((x - 1, y));
				if (fac.IsBuiltOrHadPreviousFacility || (neighborRight != null && (neighborRight == fac || neighborRight.BuildTime > neighborRight.Rules.BuildTime))) stack.Push((x + 1, y));
				if (fac.IsBuiltOrHadPreviousFacility || (neighborTop != null && (neighborTop == fac || neighborTop.BuildTime > neighborTop.Rules.BuildTime))) stack.Push((x, y - 1));
				if (fac.IsBuiltOrHadPreviousFacility || (neighborBottom != null && (neighborBottom == fac || neighborBottom.BuildTime > neighborBottom.Rules.BuildTime))) stack.Push((x, y + 1));
			}
		}

		BaseFacility lastFacility = null;
		foreach (var pair in facilitiesConnStates)
		{
			// Not a connected facility? -> push its index into the list!
			// Oh, and we don't want duplicates (facilities with bigger sizes like hangar)
			if (_facilities[pair.Facility] != lastFacility && !pair.Connected) result.Add(pair.Facility);
			lastFacility = _facilities[pair.Facility];
		}

		return result;
	}

	/// <summary>Removes a base module, and deals with the ramifications thereof.</summary>
	/// <param name="facility">Index (C++ iterator) of the facility to destroy and remove.</param>
	public void DestroyFacility(int facility)
	{
		var fac = _facilities[facility];
		if (fac.Rules.Crafts > 0)
		{
			// hangar destruction - destroy crafts and any production of crafts
			// if this will mean there is no hangar to contain it
			if (fac.CraftForDrawing != null)
			{
				// remove all soldiers
				foreach (var s in _soldiers)
				{
					if (s.Craft == fac.CraftForDrawing)
					{
						s.SetCraft(null);
					}
				}

				// remove all items
				while (!fac.CraftForDrawing.Items.Empty())
				{
					var i = fac.CraftForDrawing.Items.Contents.First();
					_items.AddItem(i.Key, i.Value);
					fac.CraftForDrawing.Items.RemoveItem(i.Key, i.Value);
				}
				DeleteIf(_crafts, 1, c => c == fac.CraftForDrawing);
			}
			else
			{
				int remove = -(AvailableHangars - UsedHangars - fac.Rules.Crafts);
				remove = DeleteIf(_productions, remove,
					i =>
					{
						if (i.Rules.ProducedCraft != null)
						{
							_engineers += i.AssignedEngineers;
							return true;
						}
						else
						{
							return false;
						}
					});
				_ = DeleteIf(_transfers, remove, i => i.Type == TransferType.TRANSFER_CRAFT);
			}
		}
		if (fac.Rules.PsiLaboratories > 0)
		{
			// psi lab destruction: remove any soldiers over the maximum allowable from psi training.
			int toRemove = fac.Rules.PsiLaboratories - FreePsiLabs;
			foreach (var soldier in _soldiers)
			{
				if (toRemove <= 0) break; // loop finished
				if (soldier.IsInPsiTraining)
				{
					soldier.SetPsiTraining(false);
					--toRemove;
				}
			}
		}
		if (fac.Rules.TrainingFacilities > 0)
		{
			// gym destruction: remove any soldiers over the maximum allowable from martial training.
			int toRemove = fac.Rules.TrainingFacilities - FreeTrainingSpace;
			foreach (var soldier in _soldiers)
			{
				if (toRemove <= 0) break; // loop finished
				if (soldier.IsInTraining)
				{
					soldier.SetTraining(false);
					--toRemove;
				}
			}
		}
		if (fac.Rules.Laboratories != 0)
		{
			// lab destruction: enforce lab space limits. take scientists off projects until
			// it all evens out. research is not cancelled.
			int toRemove = fac.Rules.Laboratories - FreeLaboratories;
			for (int iter = 0; iter < _research.Count && toRemove > 0;)
			{
				var proj = _research[iter];
				if (proj.Assigned >= toRemove)
				{
					proj.Assigned = proj.Assigned - toRemove;
					_scientists += toRemove;
					break;
				}
				else
				{
					toRemove -= proj.Assigned;
					_scientists += proj.Assigned;
					proj.Assigned = 0;
					++iter;
				}
			}
		}
		if (fac.Rules.Workshops != 0)
		{
			// workshop destruction: similar to lab destruction, but we'll lay off engineers instead
			// in this case, however, production IS cancelled, as it takes up space in the workshop.
			int toRemove = fac.Rules.Workshops - FreeWorkshops;
			DeleteIf(_productions, _productions.Count,
				p =>
				{
					if (toRemove <= 0)
					{
						// skip rest
						return false;
					}
					else if (p.AssignedEngineers > toRemove)
					{
						p.AssignedEngineers = p.AssignedEngineers - toRemove;
						_engineers += toRemove;
						toRemove = 0;
						return false;
					}
					else
					{
						_engineers += p.AssignedEngineers;
						toRemove -= p.AssignedEngineers;
						return true;
					}
				});
		}
		if (fac.Rules.Storage != 0)
		{
			// we won't destroy the items physically AT the base,
			// but any items in transit will end up at the dead letter office.
			if (StoresOverfull(fac.Rules.Storage))
			{
				DeleteIf(_transfers, _transfers.Count, i => i.Type == TransferType.TRANSFER_ITEM);
			}
		}
		if (fac.Rules.Personnel != 0)
		{
			// as above, we won't actually fire people, but we'll block any new ones coming in.
			if ((AvailableQuarters - UsedQuarters) - fac.Rules.Personnel < 0)
			{
				DeleteIf(_transfers, _transfers.Count,
					i => i.Type == TransferType.TRANSFER_ENGINEER || i.Type == TransferType.TRANSFER_SCIENTIST);
			}
		}

		_destroyedFacilitiesCache.TryGetValue(fac.Rules, out int destroyed);
		_destroyedFacilitiesCache[fac.Rules] = destroyed + 1;
		_facilities.RemoveAt(facility);
	}

	/// <summary>Cancels all prisoner interrogations. Cancels all incoming prisoner transfers.</summary>
	public void CleanupPrisons(int prisonType)
	{
		// cancel all interrogations
		DeleteIf(_research, _research.Count,
			project =>
			{
				var projRules = project.Rules;
				if (projRules.IsHoldingNeededItem)
				{
					var rule = projRules.NeededItem;
					if (rule != null && rule.IsAlien && rule.PrisonType == prisonType)
					{
						_scientists += project.Assigned;
						project.Assigned = 0;
						StorageItems.AddItem(rule, 1);
						return true;
					}
				}
				return false;
			});

		// act as if all incoming prisoners arrived already, let player decide what to do with them
		DeleteIf(_transfers, _transfers.Count,
			transfer =>
			{
				if (transfer.Type == TransferType.TRANSFER_ITEM)
				{
					var rule = transfer.Items;
					if (rule.IsAlien && rule.PrisonType == prisonType)
					{
						StorageItems.AddItem(rule, transfer.Quantity);
						return true;
					}
				}
				return false;
			});
	}

	/// <summary>Cleans up the defenses vector and optionally reclaims the tanks and their ammo.</summary>
	/// <param name="reclaimItems">determines whether the HWPs should be returned to storage.</param>
	public void CleanupDefenses(bool reclaimItems)
	{
		_defenses.Clear();

		if (reclaimItems)
		{
			foreach (var vehicle in _vehiclesFromBase)
			{
				var rule = vehicle.Rules;
				_items.AddItem(rule);
				if (rule.VehicleClipAmmo != null)
				{
					_items.AddItem(rule.VehicleClipAmmo, rule.VehicleClipsLoaded);
				}
			}
		}

		_vehicles.Clear();
		_vehiclesFromBase.Clear();
	}

	/// <summary>Space provided by facilities (local struct <c>Av</c> of C++ <c>isAreaInUse</c>).</summary>
	private struct AreaSpace
	{
		public int quarters;
		public int stores;
		public int laboratories;
		public int workshops;
		public int hangars;
		public int psiLaboratories;
		public int training;

		public void Add(RuleBaseFacility rule)
		{
			stores += rule.Storage;
			AddWithoutStores(rule);
		}

		public void AddWithoutStores(RuleBaseFacility rule)
		{
			quarters += rule.Personnel;
			laboratories += rule.Laboratories;
			workshops += rule.Workshops;
			hangars += rule.Crafts;
			psiLaboratories += rule.PsiLaboratories;
			training += rule.TrainingFacilities;
		}
	}

	/// <summary>Check if any facilities in a given area are used.</summary>
	public BasePlacementErrors IsAreaInUse(BaseAreaSubset area, RuleBaseFacility replacement = null)
	{
		var available = new AreaSpace();
		var removed = new AreaSpace();
		var provide = _provideBaseFunc;
		var require = default(RuleBaseFacilityFunctions);
		var forbidden = _forbiddenBaseFunc;
		var future = _provideBaseFunc;
		var missed = default(RuleBaseFacilityFunctions);

		int removedBuildings = 0;
		var removedPrisonType = new int[9];
		int prisonCurr = 0;

		foreach (var bf in _facilities)
		{
			var rule = bf.Rules;
			if ((bool)BaseAreaSubset.Intersection(bf.Placement, area))
			{
				++removedBuildings;

				// removed one, check what we lose
				removed.Add(rule);
				missed |= rule.ProvidedBaseFunc;

				if (rule.Aliens > 0)
				{
					int type = rule.PrisonType;
					if (Array.IndexOf(removedPrisonType, type, 0, prisonCurr) < 0)
					{
						//too many prison types, give up
						if (prisonCurr == removedPrisonType.Length)
						{
							return BasePlacementErrors.BPE_Used_AlienContainment;
						}
						removedPrisonType[prisonCurr] = type;
						++prisonCurr;
					}
				}

				// if we build over a lift better if the new one is a lift too, right now it is a bit buggy and the game can crash if two lifts are defined but in the future it can be useful.
				if (replacement != null && rule.IsLift && !replacement.IsLift)
				{
					return BasePlacementErrors.BPE_Used;
				}
			}
			else
			{
				// sum all old one, not removed
				require |= rule.RequireBaseFunc;
				forbidden |= rule.ForbiddenBaseFunc;
				future |= rule.ProvidedBaseFunc;
				if (bf.BuildTime == 0)
				{
					available.Add(rule);
					provide |= rule.ProvidedBaseFunc;
				}
				else if (bf.IfHadPreviousFacility)
				{
					if (Options.storageLimitsEnforced)
					{
						// with enforced limits we can't allow upgrades that make (temporarily) insufficient storage in a base
						available.AddWithoutStores(rule);
					}
					else
					{
						available.Add(rule);
					}

					// do not give any `provide`, you need to wait until it finishes upgrading
				}
			}
		}

		// sum new one too.
		if (replacement != null)
		{
			if (Options.storageLimitsEnforced)
			{
				// with enforced limits we can't allow upgrades that make (temporarily) insufficient storage in a base
				available.AddWithoutStores(replacement);
			}
			else
			{
				available.Add(replacement);
			}

			// temporarily allow `provide` from a new building
			provide |= replacement.ProvidedBaseFunc;
			require |= replacement.RequireBaseFunc;

			// there is still some other bulding that prevents placing the new one
			if ((forbidden & replacement.ProvidedBaseFunc).Any())
			{
				return BasePlacementErrors.BPE_ForbiddenByOther;
			}

			// check if there are any other buildings that are forbidden by this one
			if ((future & replacement.ForbiddenBaseFunc).Any())
			{
				return BasePlacementErrors.BPE_ForbiddenByThis;
			}
		}

		// if there is any required function that we do not have then it means we are trying to remove something still needed.
		// in case when building was destroyed by aliens attack we can lack some functions,
		// if we do not remove anything now then we can add new building even if we lack some functions.
		if ((~provide & require & missed).Any())
		{
			return BasePlacementErrors.BPE_Used;
		}

		// nothing removed, skip.
		if (removedBuildings == 0)
		{
			return BasePlacementErrors.BPE_None;
		}

		if (prisonCurr != 0)
		{
			var availablePrisonTypes = new int[removedPrisonType.Length];

			void SumAvailablePrisons(RuleBaseFacility rule)
			{
				int prisonSize = rule.Aliens;
				if (prisonSize > 0)
				{
					int type = rule.PrisonType;
					int find = Array.IndexOf(removedPrisonType, type, 0, prisonCurr);
					if (find >= 0)
					{
						availablePrisonTypes[find] += prisonSize;
					}
				}
			}

			// sum all available space
			foreach (var bf in _facilities)
			{
				var rule = bf.Rules;
				if (!(bool)BaseAreaSubset.Intersection(bf.Placement, area))
				{
					SumAvailablePrisons(rule);
				}
			}

			// sum new space too
			if (replacement != null)
			{
				// same as like with storage, only when limits are not enforced you can upgrade full prison
				if (!Options.storageLimitsEnforced)
				{
					SumAvailablePrisons(replacement);
				}
			}

			// check if usage fits space
			for (int i = 0; i < prisonCurr; ++i)
			{
				if (availablePrisonTypes[i] < GetUsedContainment(removedPrisonType[i]))
				{
					return BasePlacementErrors.BPE_Used_AlienContainment;
				}
			}
		}

		// only check space for things that are removed
		if (removed.stores > 0 && available.stores < GetUsedStores())
		{
			return BasePlacementErrors.BPE_Used_Stores;
		}
		else if (removed.quarters > 0 && available.quarters < UsedQuarters)
		{
			return BasePlacementErrors.BPE_Used_Quarters;
		}
		else if (removed.laboratories > 0 && available.laboratories < UsedLaboratories)
		{
			return BasePlacementErrors.BPE_Used_Laboratories;
		}
		else if (removed.workshops > 0 && available.workshops < UsedWorkshops)
		{
			return BasePlacementErrors.BPE_Used_Workshops;
		}
		else if (removed.hangars > 0 && available.hangars < UsedHangars)
		{
			return BasePlacementErrors.BPE_Used_Hangars;
		}
		else if (removed.psiLaboratories > 0 && available.psiLaboratories < UsedPsiLabs)
		{
			return BasePlacementErrors.BPE_Used_PsiLabs;
		}
		else if (removed.training > 0 && available.training < UsedTraining)
		{
			return BasePlacementErrors.BPE_Used_Gyms;
		}

		return BasePlacementErrors.BPE_None;
	}

	/// <summary>Return list of all provided functionality in base.</summary>
	/// <param name="skip">Skip functions provide by this facility.</param>
	public RuleBaseFacilityFunctions GetProvidedBaseFunc(BaseAreaSubset skip)
	{
		var ret = default(RuleBaseFacilityFunctions);

		foreach (var bf in _facilities)
		{
			if ((bool)BaseAreaSubset.Intersection(bf.Placement, skip))
			{
				continue;
			}
			if (bf.BuildTime > 0)
			{
				continue;
			}
			ret |= bf.Rules.ProvidedBaseFunc;
		}

		ret |= _provideBaseFunc;

		return ret;
	}

	/// <summary>Base functionality provided by its region and country (a copy).</summary>
	public RuleBaseFacilityFunctions InherentProvidedBaseFunc => _provideBaseFunc;

	/// <summary>Return list of all required functionality in base.</summary>
	/// <param name="skip">Skip functions require by this facility.</param>
	public RuleBaseFacilityFunctions GetRequireBaseFunc(BaseAreaSubset skip)
	{
		var ret = default(RuleBaseFacilityFunctions);

		foreach (var bf in _facilities)
		{
			if ((bool)BaseAreaSubset.Intersection(bf.Placement, skip))
			{
				continue;
			}
			ret |= bf.Rules.RequireBaseFunc;
		}

		foreach (var proj in _research)
		{
			ret |= proj.Rules.RequireBaseFunc;
		}

		foreach (var prod in _productions)
		{
			ret |= prod.Rules.RequireBaseFunc;
		}

		return ret;
	}

	/// <summary>Return list of all forbidden functionality in base.</summary>
	public RuleBaseFacilityFunctions GetForbiddenBaseFunc(BaseAreaSubset skip)
	{
		var ret = default(RuleBaseFacilityFunctions);

		foreach (var bf in _facilities)
		{
			if ((bool)BaseAreaSubset.Intersection(bf.Placement, skip))
			{
				continue;
			}
			ret |= bf.Rules.ForbiddenBaseFunc;
		}

		ret |= _forbiddenBaseFunc;

		return ret;
	}

	/// <summary>Base functionality forbidden by its region and country (a copy).</summary>
	public RuleBaseFacilityFunctions InherentForbiddenBaseFunc => _forbiddenBaseFunc;

	/// <summary>Return list of all future provided functionality in base.</summary>
	public RuleBaseFacilityFunctions GetFutureBaseFunc(BaseAreaSubset skip)
	{
		var ret = default(RuleBaseFacilityFunctions);

		foreach (var bf in _facilities)
		{
			if ((bool)BaseAreaSubset.Intersection(bf.Placement, skip))
			{
				continue;
			}
			ret |= bf.Rules.ProvidedBaseFunc;
		}

		ret |= _provideBaseFunc;

		return ret;
	}

	/// <summary>Future base functionality provided by its region and country (a copy).</summary>
	public RuleBaseFacilityFunctions InherentFutureBaseFunc => _provideBaseFunc;

	/// <summary>Checks if it is possible to build another facility of a given type.</summary>
	/// <returns>True if limit is reached (i.e. can't build anymore).</returns>
	public bool IsMaxAllowedLimitReached(RuleBaseFacility rule)
	{
		if (rule.MaxAllowedPerBase == 0)
			return false;

		int total = 0;

		foreach (var bf in _facilities)
		{
			if (bf.Rules.Type == rule.Type)
			{
				total++;
			}
		}

		return total >= rule.MaxAllowedPerBase;
	}

	/// <summary>Gets the summary of all recovery rates provided by the base.</summary>
	public BaseSumDailyRecovery SumRecoveryPerDay
	{
		get
		{
			var result = new BaseSumDailyRecovery();

			int manaMinimum = 0;
			int manaMaximum = 0;

			int healthMaxinum = 0;

			foreach (var bf in _facilities)
			{
				if (bf.BuildTime == 0)
				{
					var rule = bf.Rules;

					manaMinimum = Math.Min(manaMinimum, rule.ManaRecoveryPerDay);
					manaMaximum = Math.Max(manaMaximum, rule.ManaRecoveryPerDay);

					healthMaxinum = Math.Max(healthMaxinum, rule.HealthRecoveryPerDay);

					result.SickBayAbsoluteBonus += rule.SickBayAbsoluteBonus;
					result.SickBayRelativeBonus += rule.SickBayRelativeBonus;
				}
			}

			if (manaMaximum > 0)
				result.ManaRecovery = manaMaximum;
			else if (manaMinimum < 0)
				result.ManaRecovery = manaMinimum;

			result.HealthRecovery = healthMaxinum;

			return result;
		}
	}

	/// <summary>Removes the craft and all associations from the base (does not destroy it!).</summary>
	/// <param name="craft">Pointer to craft.</param>
	/// <param name="unload">Unload craft contents before removing.</param>
	/// <returns>Index of the element after the removed craft (C++ iterator returned by erase), or the craft count if not found.</returns>
	public int RemoveCraft(Craft craft, bool unload)
	{
		// Unload craft
		if (unload)
		{
			craft.Unload();
		}

		// Clear hangar
		foreach (var fac in _facilities)
		{
			if (fac.CraftForDrawing == craft)
			{
				fac.CraftForDrawing = null;
				break;
			}
		}

		// Remove craft
		int c;
		for (c = 0; c < _crafts.Count; ++c)
		{
			if (_crafts[c] == craft)
			{
				_crafts.RemoveAt(c);
				return c;
			}
		}
		return c;
	}

	/// <summary>The base's globe texture.</summary>
	public Texture GlobeTexture
	{
		get => _globeTexture;
		set => _globeTexture = value;
	}

	/// <summary>
	/// C++ <c>Collections::deleteIf</c> / <c>removeIf</c> on a vector: removes (in order) up to <paramref name="numberToRemove"/>
	/// elements for which <paramref name="func"/> is true; once the limit is reached <paramref name="func"/> is not called any more.
	/// </summary>
	/// <returns>Number of values left to remove (0 if <paramref name="numberToRemove"/> was not positive).</returns>
	internal static int DeleteIf<T>(List<T> vec, int numberToRemove, Func<T, bool> func)
	{
		if (numberToRemove <= 0)
		{
			return 0;
		}
		int newEnd = 0;
		for (int it = 0; it < vec.Count; ++it)
		{
			var value = vec[it];
			if (numberToRemove > 0 && func(value))
			{
				--numberToRemove;
			}
			else
			{
				vec[newEnd] = value;
				++newEnd;
			}
		}
		vec.RemoveRange(newEnd, vec.Count - newEnd);
		return numberToRemove;
	}
}
