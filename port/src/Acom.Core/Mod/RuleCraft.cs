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
// Ported from: src/Mod/RuleCraft.h, src/Mod/RuleCraft.cpp
// Port status: partial (missing: script hooks)
#nullable disable

namespace Acom;

/// <summary>
/// Battle statistic of craft type and bonus from craft weapons.
/// </summary>
/// <remarks>
/// A value type in C++. The operators return new objects (so <c>a += b</c> never changes a shared rule object);
/// use the copy constructor where C++ copies it.
/// </remarks>
public class RuleCraftStats
{
	public int fuelMax;
	public int damageMax;
	public int speedMax;
	public int accel;
	public int radarRange;
	public int radarChance;
	public int sightRange;
	public int hitBonus;
	public int avoidBonus;
	public int avoidBonus2;
	public int powerBonus;
	public int armor;
	public int shieldCapacity;
	public int shieldRecharge;
	public int shieldRechargeInGeoscape;
	public int shieldBleedThrough;
	public int soldiers;
	public int vehicles;
	public int maxItems;
	public double maxStorageSpace;

	/// <summary>Default constructor (all zero).</summary>
	public RuleCraftStats()
	{
	}

	/// <summary>Copy (C++ copy constructor).</summary>
	public RuleCraftStats(RuleCraftStats other)
	{
		ArgumentNullException.ThrowIfNull(other);
		CopyFrom(other);
	}

	/// <summary>Assigns all stats of <paramref name="other"/> (C++ copy assignment).</summary>
	public void CopyFrom(RuleCraftStats other)
	{
		ArgumentNullException.ThrowIfNull(other);
		fuelMax = other.fuelMax;
		damageMax = other.damageMax;
		speedMax = other.speedMax;
		accel = other.accel;
		radarRange = other.radarRange;
		radarChance = other.radarChance;
		sightRange = other.sightRange;
		hitBonus = other.hitBonus;
		avoidBonus = other.avoidBonus;
		avoidBonus2 = other.avoidBonus2;
		powerBonus = other.powerBonus;
		armor = other.armor;
		shieldCapacity = other.shieldCapacity;
		shieldRecharge = other.shieldRecharge;
		shieldRechargeInGeoscape = other.shieldRechargeInGeoscape;
		shieldBleedThrough = other.shieldBleedThrough;
		soldiers = other.soldiers;
		vehicles = other.vehicles;
		maxItems = other.maxItems;
		maxStorageSpace = other.maxStorageSpace;
	}

	/// <summary>Adds different stats in place (C++ <c>operator+=</c>).</summary>
	public void Add(RuleCraftStats r)
	{
		ArgumentNullException.ThrowIfNull(r);
		fuelMax += r.fuelMax;
		damageMax += r.damageMax;
		speedMax += r.speedMax;
		accel += r.accel;
		radarRange += r.radarRange;
		radarChance += r.radarChance;
		sightRange += r.sightRange;
		hitBonus += r.hitBonus;
		avoidBonus += r.avoidBonus;
		avoidBonus2 += r.avoidBonus2;
		powerBonus += r.powerBonus;
		armor += r.armor;
		shieldCapacity += r.shieldCapacity;
		shieldRecharge += r.shieldRecharge;
		shieldRechargeInGeoscape += r.shieldRechargeInGeoscape;
		shieldBleedThrough += r.shieldBleedThrough;
		soldiers += r.soldiers;
		vehicles += r.vehicles;
		maxItems += r.maxItems;
		maxStorageSpace += r.maxStorageSpace;
	}

	/// <summary>Subtracts different stats in place (C++ <c>operator-=</c>).</summary>
	public void Subtract(RuleCraftStats r)
	{
		ArgumentNullException.ThrowIfNull(r);
		fuelMax -= r.fuelMax;
		damageMax -= r.damageMax;
		speedMax -= r.speedMax;
		accel -= r.accel;
		radarRange -= r.radarRange;
		radarChance -= r.radarChance;
		sightRange -= r.sightRange;
		hitBonus -= r.hitBonus;
		avoidBonus -= r.avoidBonus;
		avoidBonus2 -= r.avoidBonus2;
		powerBonus -= r.powerBonus;
		armor -= r.armor;
		shieldCapacity -= r.shieldCapacity;
		shieldRecharge -= r.shieldRecharge;
		shieldRechargeInGeoscape -= r.shieldRechargeInGeoscape;
		shieldBleedThrough -= r.shieldBleedThrough;
		soldiers -= r.soldiers;
		vehicles -= r.vehicles;
		maxItems -= r.maxItems;
		maxStorageSpace -= r.maxStorageSpace;
	}

	/// <summary>Sum of two stats as a new object.</summary>
	public static RuleCraftStats operator +(RuleCraftStats l, RuleCraftStats r)
	{
		var s = new RuleCraftStats(l);
		s.Add(r);
		return s;
	}

	/// <summary>Difference of two stats as a new object.</summary>
	public static RuleCraftStats operator -(RuleCraftStats l, RuleCraftStats r)
	{
		var s = new RuleCraftStats(l);
		s.Subtract(r);
		return s;
	}

	/// <summary>Gets negative values of stats.</summary>
	public static RuleCraftStats operator -(RuleCraftStats r)
	{
		var s = new RuleCraftStats();
		s.Subtract(r);
		return s;
	}

	public static RuleCraftStats Negate(RuleCraftStats r) => -r;

	/// <summary>Loads stats from YAML.</summary>
	public void Load(YamlNodeReader reader)
	{
		reader.TryRead("fuelMax", ref fuelMax);
		reader.TryRead("damageMax", ref damageMax);
		reader.TryRead("speedMax", ref speedMax);
		reader.TryRead("accel", ref accel);
		reader.TryRead("radarRange", ref radarRange);
		reader.TryRead("radarChance", ref radarChance);
		reader.TryRead("sightRange", ref sightRange);
		reader.TryRead("hitBonus", ref hitBonus);
		reader.TryRead("avoidBonus", ref avoidBonus);
		reader.TryRead("avoidBonus2", ref avoidBonus2);
		reader.TryRead("powerBonus", ref powerBonus);
		reader.TryRead("armor", ref armor);
		reader.TryRead("shieldCapacity", ref shieldCapacity);
		reader.TryRead("shieldRecharge", ref shieldRecharge);
		reader.TryRead("shieldRechargeInGeoscape", ref shieldRechargeInGeoscape);
		reader.TryRead("shieldBleedThrough", ref shieldBleedThrough);
		reader.TryRead("soldiers", ref soldiers);
		reader.TryRead("vehicles", ref vehicles);
		reader.TryRead("maxItems", ref maxItems);
		reader.TryRead("maxStorageSpace", ref maxStorageSpace);
	}
}

/// <summary>
/// Represents a specific type of craft.
/// Contains constant info about a craft like
/// costs, speed, capacities, consumptions, etc.
/// </summary>
/// <seealso cref="Craft"/>
public partial class RuleCraft
{
	/// <summary>Maximum number of weapon slots on craft.</summary>
	public const int WeaponMax = 4;
	/// <summary>Maximum of different types in one weapon slot.</summary>
	public const int WeaponTypeMax = 8;
	/// <summary>Default craft preview ID.</summary>
	public const string DEFAULT_CRAFT_DEPLOYMENT_PREVIEW = "STR_CRAFT_DEPLOYMENT_PREVIEW";
	/// <summary>Dummy craft ID.</summary>
	public const int DUMMY_CRAFT_ID = -42; // a negative integer
	/// <summary>Name of class used in script.</summary>
	public const string ScriptName = "RuleCraft";

	private readonly string _type;
	private readonly List<string> _requires = [];
	private RuleBaseFacilityFunctions _requiresBuyBaseFunc;
	private string _requiresBuyCountry = "";
	private int _sprite, _marker;
	private readonly List<int> _skinSprites = [];
	private int _weapons, _maxUnitsLimit, _pilots, _maxVehiclesAndLargeSoldiersLimit;
	private int _maxSmallSoldiers, _maxLargeSoldiers, _maxSmallVehicles, _maxLargeVehicles;
	private int _maxSmallUnits, _maxLargeUnits, _maxSoldiers, _maxVehicles;
	private int _monthlyBuyLimit;
	private string _monthlyBuyLimitMessage = "";
	private int _costBuy, _costRent, _costSell;
	private readonly sbyte[,] _weaponTypes = new sbyte[WeaponMax, WeaponTypeMax]; // C++ char
	private RuleItem _refuelItem;
	private string _refuelItemName = "";
	private readonly string[] _weaponStrings = ["", "", "", ""];
	private readonly string[] _fixedWeaponNames = ["", "", "", ""];
	private int _repairRate, _refuelRate, _transferTime, _score;
	private RuleTerrain _battlescapeTerrainData;
	private int _maxSkinIndex;
	private bool _keepCraftAfterFailedMission, _allowLanding, _spacecraft, _notifyWhenRefueled, _autoPatrol, _undetectable, _patrolWithoutFuel;
	private int _missilePower;
	private int _listOrder, _maxAltitude;
	private string _defaultAltitude;
	private List<List<int>> _deployment = [];
	private List<int> _craftInventoryTile = [];
	private readonly List<int> _groups = [];
	private readonly List<int> _allowedSoldierGroups = [];
	private readonly List<int> _allowedArmorGroups = [];
	private SortedDictionary<int, int> _limitArmorGroups = new();
	private bool _onlyOneSoldierGroupAllowed;
	private readonly RuleCraftStats _stats;
	private int _shieldRechargeAtBase;
	private bool _mapVisible, _forceShowInMonthlyCosts;
	private bool _useAllStartTiles;
	private string _customPreview = "";
	private readonly List<int> _selectSound = [], _takeoffSound = [];
	private UnitStats _pilotMinStatsRequired;
	private readonly List<string> _pilotSoldierBonusesRequiredNames = [];
	private readonly List<RuleSoldierBonus> _pilotSoldierBonusesRequired = [];

	/// <summary>Creates a blank ruleset for a certain type of craft.</summary>
	/// <param name="type">String defining the type.</param>
	/// <param name="listOrder">The list weight for this craft.</param>
	public RuleCraft(string type, int listOrder)
	{
		_type = type;
		_sprite = -1;
		_marker = -1;
		_weapons = 0;
		_maxUnitsLimit = -1;
		_pilots = 0;
		_maxVehiclesAndLargeSoldiersLimit = -1;
		_maxSmallSoldiers = -1;
		_maxLargeSoldiers = -1;
		_maxSmallVehicles = -1;
		_maxLargeVehicles = -1;
		_maxSmallUnits = -1;
		_maxLargeUnits = -1;
		_maxSoldiers = -1;
		_maxVehicles = -1;
		_monthlyBuyLimit = 0;
		_costBuy = 0;
		_costRent = 0;
		_costSell = 0;
		_repairRate = 1;
		_refuelRate = 1;
		_transferTime = 24;
		_score = 0;
		_battlescapeTerrainData = null;
		_maxSkinIndex = 0;
		_keepCraftAfterFailedMission = false;
		_allowLanding = true;
		_spacecraft = false;
		_notifyWhenRefueled = false;
		_autoPatrol = false;
		_undetectable = false;
		_patrolWithoutFuel = false;
		_missilePower = 0;
		_listOrder = listOrder;
		_maxAltitude = -1;
		_defaultAltitude = "STR_VERY_LOW";
		_onlyOneSoldierGroupAllowed = false;
		_stats = new RuleCraftStats();
		_shieldRechargeAtBase = 1000;
		_mapVisible = true;
		_forceShowInMonthlyCosts = false;
		_useAllStartTiles = false;

		_stats.radarRange = 672;
		_stats.radarChance = 100;
		_stats.sightRange = 1696;
		_stats.maxItems = 999999;
		_stats.maxStorageSpace = 99999.0;
		_weaponStrings[0] = "STR_WEAPON_ONE";
		_weaponStrings[1] = "STR_WEAPON_TWO";
	}

	/// <summary>Loads the craft from YAML.</summary>
	/// <remarks>The C++ <c>parsers</c> argument (script containers) is left out with the script hooks.</remarks>
	public void Load(YamlNodeReader node, Mod mod)
	{
		var reader = node.UseIndex();
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent, mod);
		}

		//requires
		mod.LoadUnorderedNames(_type, _requires, reader["requires"]);
		mod.LoadBaseFunction(_type, ref _requiresBuyBaseFunc, reader["requiresBuyBaseFunc"]);
		reader.TryRead("requiresBuyCountry", ref _requiresBuyCountry);

		if (reader["sprite"])
		{
			// used in
			// Surface set (baseOffset):
			//   BASEBITS.PCK (33)
			//   INTICON.PCK (11)
			//   INTICON.PCK (0)
			//
			// Final index in surfaceset is `baseOffset + sprite + (sprite > 4 ? modOffset : 0)`
			_sprite = mod.GetOffset(reader["sprite"].ReadVal(_sprite), 4);
		}
		var skinSprites = reader["skinSprites"];
		if (skinSprites)
		{
			_skinSprites.Clear();
			foreach (var skinSprite in skinSprites.Children())
			{
				int tmp = mod.GetOffset(skinSprite.ReadVal<int>(), 4);
				_skinSprites.Add(tmp);
			}
		}
		_stats.Load(reader);
		if (reader["marker"])
		{
			_marker = mod.GetOffset(reader["marker"].ReadVal(_marker), 8);
		}
		reader.TryRead("weapons", ref _weapons);
		reader.TryRead("maxUnitsLimit", ref _maxUnitsLimit);
		reader.TryRead("pilots", ref _pilots);
		reader.TryRead("maxHWPUnitsLimit", ref _maxVehiclesAndLargeSoldiersLimit);
		reader.TryRead("maxSmallSoldiers", ref _maxSmallSoldiers);
		reader.TryRead("maxLargeSoldiers", ref _maxLargeSoldiers);
		reader.TryRead("maxSmallVehicles", ref _maxSmallVehicles);
		reader.TryRead("maxLargeVehicles", ref _maxLargeVehicles);
		reader.TryRead("maxSmallUnits", ref _maxSmallUnits);
		reader.TryRead("maxLargeUnits", ref _maxLargeUnits);
		reader.TryRead("maxSoldiers", ref _maxSoldiers);
		reader.TryRead("maxVehicles", ref _maxVehicles);
		reader.TryRead("monthlyBuyLimit", ref _monthlyBuyLimit);
		reader.TryRead("monthlyBuyLimitMessage", ref _monthlyBuyLimitMessage);
		reader.TryRead("costBuy", ref _costBuy);
		reader.TryRead("costRent", ref _costRent);
		reader.TryRead("costSell", ref _costSell);
		mod.LoadName(_type, ref _refuelItemName, reader["refuelItem"]);
		reader.TryRead("repairRate", ref _repairRate);
		reader.TryRead("refuelRate", ref _refuelRate);
		reader.TryRead("transferTime", ref _transferTime);
		reader.TryRead("score", ref _score);
		var terrain = reader["battlescapeTerrainData"];
		if (terrain)
		{
			var rule = new RuleTerrain(terrain["name"].ReadVal<string>());
			rule.Load(terrain, mod);
			_battlescapeTerrainData = rule;
		}
		reader.TryRead("craftInventoryTile", ref _craftInventoryTile);
		mod.LoadUnorderedInts(_type, _groups, reader["groups"]);
		mod.LoadUnorderedInts(_type, _allowedSoldierGroups, reader["allowedSoldierGroups"]);
		mod.LoadUnorderedInts(_type, _allowedArmorGroups, reader["allowedArmorGroups"]);
		reader.TryRead("limitArmorGroups", ref _limitArmorGroups);
		reader.TryRead("onlyOneSoldierGroupAllowed", ref _onlyOneSoldierGroupAllowed);
		reader.TryRead("maxSkinIndex", ref _maxSkinIndex);
		reader.TryRead("deployment", ref _deployment);
		reader.TryRead("keepCraftAfterFailedMission", ref _keepCraftAfterFailedMission);
		reader.TryRead("allowLanding", ref _allowLanding);
		reader.TryRead("spacecraft", ref _spacecraft);
		reader.TryRead("notifyWhenRefueled", ref _notifyWhenRefueled);
		reader.TryRead("autoPatrol", ref _autoPatrol);
		reader.TryRead("undetectable", ref _undetectable);
		reader.TryRead("patrolWithoutFuel", ref _patrolWithoutFuel);
		reader.TryRead("missilePower", ref _missilePower);
		reader.TryRead("listOrder", ref _listOrder);
		reader.TryRead("maxAltitude", ref _maxAltitude);
		reader.TryRead("defaultAltitude", ref _defaultAltitude);

		var types = reader["weaponTypes"];
		if (types)
		{
			int max = Math.Min(types.ChildrenCount, WeaponMax);
			for (int i = 0; i < max; ++i)
			{
				var type = types[i];
				if (type.HasVal)
				{
					for (int j = 0; j < WeaponTypeMax; ++j)
						_weaponTypes[i, j] = unchecked((sbyte)type.ReadVal<int>());
				}
				else if (type.IsSeq)
				{
					for (int j = 0; j < type.ChildrenCount && j < WeaponTypeMax; ++j)
						_weaponTypes[i, j] = unchecked((sbyte)type[j].ReadVal<int>());
					for (int j = type.ChildrenCount; j < WeaponTypeMax; ++j)
						_weaponTypes[i, j] = _weaponTypes[i, 0];
				}
				else
				{
					throw new InvalidOperationException("Invalid weapon type in craft " + _type + ".");
				}
			}
		}
		var str = reader["weaponStrings"];
		if (str)
		{
			for (int i = 0; i < str.ChildrenCount && i < WeaponMax; ++i)
				_weaponStrings[i] = str[i].ReadVal<string>();
		}
		str = reader["fixedWeapons"];
		if (str)
		{
			for (int i = 0; i < str.ChildrenCount && i < WeaponMax; ++i)
				_fixedWeaponNames[i] = str[i].ReadVal<string>();
		}
		reader.TryRead("shieldRechargedAtBase", ref _shieldRechargeAtBase);
		reader.TryRead("mapVisible", ref _mapVisible);
		reader.TryRead("forceShowInMonthlyCosts", ref _forceShowInMonthlyCosts);
		reader.TryRead("useAllStartTiles", ref _useAllStartTiles);
		reader.TryRead("customPreview", ref _customPreview);

		mod.LoadSoundOffset(_type, _selectSound, reader["selectSound"], "GEO.CAT");
		mod.LoadSoundOffset(_type, _takeoffSound, reader["takeoffSound"], "GEO.CAT");

		reader.TryRead("pilotMinStatsRequired", ref _pilotMinStatsRequired);
		mod.LoadNames(_type, _pilotSoldierBonusesRequiredNames, reader["pilotSoldierBonusesRequired"]);

		LoadScripts(reader, mod.ScriptParsers);
	}

	/// <summary>Cross link with other rules.</summary>
	public void AfterLoad(Mod mod)
	{
		mod.LinkRule(ref _refuelItem, ref _refuelItemName);
		mod.LinkRule(_pilotSoldierBonusesRequired, _pilotSoldierBonusesRequiredNames);

		// No turning soldiers into antimatter
		mod.CheckForSoftError(_stats.soldiers < 0, _type, "Default unit capacity cannot be negative.", SeverityLevel.LOG_ERROR);
		mod.CheckForSoftError(_stats.vehicles < 0, _type, "Default HWP capacity cannot be negative.", SeverityLevel.LOG_ERROR);

		// Backwards-compatibility
		if (_maxUnitsLimit < 0)
		{
			_maxUnitsLimit = _stats.soldiers;
		}
		if (_maxVehiclesAndLargeSoldiersLimit < 0)
		{
			_maxVehiclesAndLargeSoldiersLimit = _stats.vehicles;
		}

		// Sanity checks
		mod.CheckForSoftError(_maxUnitsLimit < _stats.soldiers, _type, "Maximum unit capacity is smaller than the default unit capacity.", SeverityLevel.LOG_ERROR);
		mod.CheckForSoftError(_maxVehiclesAndLargeSoldiersLimit < _stats.vehicles, _type, "Maximum HWP capacity is smaller than the default HWP capacity.", SeverityLevel.LOG_ERROR);
	}

	/// <summary>Gets the language string that names this craft. Each craft type has a unique name.</summary>
	public string Type => _type;

	/// <summary>Gets the list of research required to acquire this craft.</summary>
	public List<string> Requirements => _requires;

	/// <summary>Gets the base functions required to buy craft.</summary>
	public RuleBaseFacilityFunctions RequiresBuyBaseFunc => _requiresBuyBaseFunc;

	/// <summary>Gets the allied country name required to buy this craft.</summary>
	public string RequiresBuyCountry => _requiresBuyCountry;

	/// <summary>Gets the ID of the sprite used to draw the craft in the Basescape and Equip Craft screens.</summary>
	public int GetSprite(int skinIndex)
	{
		if (skinIndex > 0)
		{
			int vectorIndex = skinIndex - 1;
			if (vectorIndex < _skinSprites.Count)
			{
				return _skinSprites[vectorIndex];
			}
		}
		return _sprite;
	}

	public List<int> SkinSpritesRaw => _skinSprites;

	/// <summary>Returns the globe marker for the craft type (-1 if none).</summary>
	public int Marker => _marker;

	/// <summary>Gets the maximum fuel the craft can contain.</summary>
	public int MaxFuel => _stats.fuelMax;

	/// <summary>Gets the maximum damage (damage the craft can take) of the craft.</summary>
	public int MaxDamage => _stats.damageMax;

	/// <summary>Gets the maximum speed of the craft flying around the Geoscape (knots).</summary>
	public int MaxSpeed => _stats.speedMax;

	/// <summary>Gets the acceleration of the craft for taking off / stopping.</summary>
	public int Acceleration => _stats.accel;

	/// <summary>Gets the maximum number of weapons that can be equipped onto the craft.</summary>
	public int Weapons => _weapons;

	/// <summary>Checks if this craft is supported in the New Battle mode (and Preview mode).</summary>
	public bool IsForNewBattle => BattlescapeTerrainData != null && MaxUnitsLimit > 0 && AllowLanding;

	/// <summary>Gets the maximum number of units (soldiers and vehicles, small and large) that the craft can carry.</summary>
	public int MaxUnits => _stats.soldiers;

	/// <summary>Gets the craft's maximum unit capacity *including* any additional weapons module bonuses.</summary>
	public int MaxUnitsLimit => _maxUnitsLimit;

	/// <summary>Gets the number of pilots that the craft requires in order to take off.</summary>
	public int Pilots => _pilots;

	/// <summary>Gets the maximum number of vehicles (and 2x2 soldiers) that the craft can carry.</summary>
	public int MaxVehiclesAndLargeSoldiers => _stats.vehicles;

	/// <summary>Gets the craft's maximum vehicle capacity (incl. 2x2 soldiers) *including* any additional weapons module bonuses.</summary>
	public int MaxVehiclesAndLargeSoldiersLimit => _maxVehiclesAndLargeSoldiersLimit;

	/// <summary>Gets the craft's maximum supported number of small (size=1) soldiers.</summary>
	public int MaxSmallSoldiers => _maxSmallSoldiers;

	/// <summary>Gets the craft's maximum supported number of large (size=2) soldiers.</summary>
	public int MaxLargeSoldiers => _maxLargeSoldiers;

	/// <summary>Gets the craft's maximum supported number of small (size=1) vehicles (HWPs/SWSs).</summary>
	public int MaxSmallVehicles => _maxSmallVehicles;

	/// <summary>Gets the craft's maximum supported number of large (size=2) vehicles (HWPs/SWSs).</summary>
	public int MaxLargeVehicles => _maxLargeVehicles;

	/// <summary>Gets the craft's maximum supported number of small (size=1) units (soldiers + vehicles).</summary>
	public int MaxSmallUnits => _maxSmallUnits;

	/// <summary>Gets the craft's maximum supported number of large (size=2) units (soldiers + vehicles).</summary>
	public int MaxLargeUnits => _maxLargeUnits;

	/// <summary>Gets the craft's maximum supported number of soldiers (small + large).</summary>
	public int MaxSoldiers => _maxSoldiers;

	/// <summary>Gets the craft's maximum supported number of vehicles (small + large).</summary>
	public int MaxVehicles => _maxVehicles;

	/// <summary>Gets the craft's monthly buy limit.</summary>
	public int MonthlyBuyLimit => _monthlyBuyLimit;

	/// <summary>Gets the craft's monthly buy limit message.</summary>
	public string MonthlyBuyLimitMessage => _monthlyBuyLimitMessage;

	/// <summary>Gets the cost of this craft for purchase/rent (0 if not purchasable).</summary>
	public int BuyCost => _costBuy;

	/// <summary>Gets the cost of rent for a month.</summary>
	public int RentCost => _costRent;

	/// <summary>Gets the sell value of this craft. Rented craft should use 0.</summary>
	public int SellCost => _costSell;

	/// <summary>Gets what item is required while the craft is refuelling (null if none).</summary>
	public RuleItem RefuelItem => _refuelItem;

	/// <summary>Gets how much damage is removed from the craft while repairing.</summary>
	public int RepairRate => _repairRate;

	/// <summary>Gets how much fuel is added to the craft while refuelling.</summary>
	public int RefuelRate => _refuelRate;

	/// <summary>Gets the craft's radar range for detecting UFOs.</summary>
	public int RadarRange => _stats.radarRange;

	/// <summary>Gets the craft's radar chance for detecting UFOs.</summary>
	public int RadarChance => _stats.radarChance;

	/// <summary>Gets the craft's sight range for detecting bases.</summary>
	public int SightRange => _stats.sightRange;

	/// <summary>Gets the amount of time this item takes to arrive at a base.</summary>
	public int TransferTime => _transferTime;

	/// <summary>Gets the number of points you lose when this craft is destroyed.</summary>
	public int Score => _score;

	/// <summary>Gets the terrain data needed to draw the Craft in the battlescape.</summary>
	public RuleTerrain BattlescapeTerrainData => _battlescapeTerrainData;

	/// <summary>Gets the craft's maximum skin index.</summary>
	public int MaxSkinIndex => _maxSkinIndex;

	/// <summary>Checks if this craft is lost after a failed mission or not.</summary>
	public bool KeepCraftAfterFailedMission() => _keepCraftAfterFailedMission;

	/// <summary>Checks if this craft is capable of landing (on missions).</summary>
	public bool AllowLanding => _allowLanding;

	/// <summary>Checks if this ship is capable of going to mars.</summary>
	public bool Spacecraft => _spacecraft;

	/// <summary>Checks if a notification should be displayed when the craft is refueled.</summary>
	public bool NotifyWhenRefueled() => _notifyWhenRefueled;

	/// <summary>Checks if the craft supports auto-patrol feature.</summary>
	public bool CanAutoPatrol() => _autoPatrol;

	/// <summary>Is this craft immune to detection by HKs and alien bases?</summary>
	public bool IsUndetectable => _undetectable;

	/// <summary>Can this craft patrol without fuel comsumption?</summary>
	public bool PatrolWithoutFuel() => _patrolWithoutFuel;

	/// <summary>Is this craft a self-destruct missile?</summary>
	public bool IsMissile => _missilePower > 0;

	/// <summary>Gets the missile power.</summary>
	public int MissilePower() => _missilePower;

	/// <summary>Gets the list weight for this research item.</summary>
	public int ListOrder => _listOrder;

	/// <summary>Gets the deployment layout for this craft.</summary>
	public List<List<int>> Deployment => _deployment;

	/// <summary>Gets the craft inventory tile position.</summary>
	public List<int> CraftInventoryTile => _craftInventoryTile;

	/// <summary>Gets the craft groups (used in map scripts).</summary>
	public List<int> Groups => _groups;

	/// <summary>Gets the list of allowed soldier groups.</summary>
	public List<int> AllowedSoldierGroups => _allowedSoldierGroups;

	/// <summary>Gets the list of allowed armor groups.</summary>
	public List<int> AllowedArmorGroups => _allowedArmorGroups;

	/// <summary>Gets the limits per armor group.</summary>
	public SortedDictionary<int, int> LimitArmorGroups => _limitArmorGroups;

	/// <summary>Does this craft allow soldiers of the same group only?</summary>
	public bool IsOnlyOneSoldierGroupAllowed => _onlyOneSoldierGroupAllowed;

	/// <summary>Gets the item limit for this craft.</summary>
	public int MaxItems => _stats.maxItems;

	/// <summary>Gets the item storage space limit for this craft.</summary>
	public double MaxStorageSpace => _stats.maxStorageSpace;

	/// <summary>Test for possibility of usage of weapon type in weapon slot.</summary>
	/// <param name="slot">Value less than WeaponMax.</param>
	/// <param name="weaponType">Weapon type of craft weapon.</param>
	public bool IsValidWeaponSlot(int slot, int weaponType)
	{
		for (int j = 0; j < WeaponTypeMax; ++j)
		{
			if (_weaponTypes[slot, j] == weaponType)
				return true;
		}
		return false;
	}

	public int GetWeaponTypesRaw(int slot, int subslot) => _weaponTypes[slot, subslot];

	/// <summary>Return string ID of weapon slot name for geoscape craft state.</summary>
	public string GetWeaponSlotString(int slot) => _weaponStrings[slot];

	/// <summary>Gets the string ID of a fixed weapon in a given slot.</summary>
	public string GetFixedWeaponInSlot(int slot) => _fixedWeaponNames[slot];

	/// <summary>Gets basic statistic of craft.</summary>
	public RuleCraftStats Stats => _stats;

	/// <summary>Gets how high this craft can go.</summary>
	public int MaxAltitude => _maxAltitude;

	/// <summary>Gets the craft's default display altitude.</summary>
	public string DefaultDisplayAltitude => _defaultAltitude;

	/// <summary>If the craft is underwater, it can only go so high.</summary>
	public bool IsWaterOnly => _maxAltitude > -1;

	/// <summary>Gets how many shield points are recharged when landed at base per hour.</summary>
	public int ShieldRechargeAtBase => _shieldRechargeAtBase;

	/// <summary>Gets whether or not the craft map should be visible at the beginning of the battlescape.</summary>
	public bool IsMapVisible => _mapVisible;

	/// <summary>Gets whether or not the craft type should be displayed in Monthly Costs even if not present in the base.</summary>
	public bool ForceShowInMonthlyCosts() => _forceShowInMonthlyCosts;

	/// <summary>Can the player utilize all start tiles on a craft or only the ones specified in the 'deployment' list?</summary>
	public bool UseAllStartTiles() => _useAllStartTiles;

	/// <summary>Gets the craft's custom preview type.</summary>
	public string CustomPreviewType => string.IsNullOrEmpty(_customPreview) ? DEFAULT_CRAFT_DEPLOYMENT_PREVIEW : _customPreview;

	public string CustomPreviewTypeRaw => _customPreview;

	/// <summary>Calculates the theoretical range of the craft (nautical miles).</summary>
	/// <param name="type">The type of range to calculate: 1 = min, 2 = average, other = max.</param>
	/// <returns>The calculated range, or -1 for infinite range.</returns>
	public int CalculateRange(int type)
	{
		// If the craft uses an item to refuel, the tick rate is one fuel unit per 10 minutes
		int totalFuelTicks = _stats.fuelMax;

		// If no item is used to refuel, the tick rate depends on speed
		if (_refuelItem == null)
		{
			// Craft with less than 100 speed don't consume fuel and therefore have infinite range
			if (_stats.speedMax < 100)
			{
				return -1;
			}

			totalFuelTicks = _stats.fuelMax / (_stats.speedMax / 100);
		}

		// Six ticks per hour, factor return trip and speed for total range
		int range;
		switch (type)
		{
			// Min range happens when the craft is sent at xx:x9:59, as a unit of fuel is immediately consumed, so we subtract an extra 'tick' of fuel in this case
			case 1:
				range = (totalFuelTicks - 1) * _stats.speedMax / 12;
				break;
			case 2:
				range = (2 * totalFuelTicks - 1) * _stats.speedMax / 12 / 2;
				break;
			default:
				range = totalFuelTicks * _stats.speedMax / 12;
				break;
		}

		return range;
	}

	/// <summary>Gets a random sound from a given list.</summary>
	private static int GetRandomSound(List<int> vector, int defaultValue = -1)
	{
		if (vector.Count != 0)
		{
			return vector[Rng.Generate(0, vector.Count - 1)];
		}
		return defaultValue;
	}

	/// <summary>Gets the sound played when the player directly selects a craft on the globe (uses the RNG).</summary>
	public int SelectSound => GetRandomSound(_selectSound);

	public List<int> SelectSoundRaw => _selectSound;

	/// <summary>Gets the sound played when a craft takes off from a base (uses the RNG).</summary>
	public int TakeoffSound => GetRandomSound(_takeoffSound);

	public List<int> TakeoffSoundRaw => _takeoffSound;

	/// <summary>Gets the minimum stats a soldier needs to be eligible for piloting this craft.</summary>
	public UnitStats PilotMinStatsRequired => _pilotMinStatsRequired;

	/// <summary>Gets the list of soldier bonuses a soldier needs to be eligible for piloting this craft.</summary>
	public List<RuleSoldierBonus> PilotSoldierBonusesRequired => _pilotSoldierBonusesRequired;
}
