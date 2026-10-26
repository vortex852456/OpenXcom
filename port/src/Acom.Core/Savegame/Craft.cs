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
// Ported from: src/Savegame/Craft.h, src/Savegame/Craft.cpp
// Port status: partial (missing: script hooks)
#nullable disable

using System.Globalization;

namespace Acom;

public enum CraftPlacementErrors
{
	CPE_None = 0,
	CPE_NotEnoughSpace = 1,
	CPE_TooManySoldiers = 2,
	CPE_TooManySmallSoldiers = 3,
	CPE_TooManySmallUnits = 4,
	CPE_TooManyVehiclesAndLargeSoldiers = 5,
	CPE_TooManyLargeSoldiers = 6,
	CPE_TooManyLargeUnits = 7,
	CPE_SoldierGroupNotAllowed = 8,
	CPE_SoldierGroupNotSame = 9,
	CPE_ArmorGroupNotAllowed = 10,
}

/// <summary>Custom deployment of a vehicle in the craft (C++ struct).</summary>
public class VehicleDeploymentData
{
	public string type;
	public Position pos;
	public int dir;
	public bool used;

	public VehicleDeploymentData()
	{
		type = "";
		pos = new Position(-1, -1, -1);
		dir = 0;
		used = false;
	}

	/// <summary>YAML: flow map {type, pos, dir} (C++ read overload; "used" is not stored).</summary>
	public static bool YamlRead(YamlNodeReader reader, ref VehicleDeploymentData val)
	{
		if (!reader.IsMap)
			return false;
		val ??= new VehicleDeploymentData();
		reader.TryRead("type", ref val.type);
		reader.TryRead("pos", ref val.pos);
		reader.TryRead("dir", ref val.dir);
		// reader.tryRead("used", val->used); // not needed
		return true;
	}

	/// <summary>YAML: C++ write overload.</summary>
	public static void YamlWrite(YamlNodeWriter writer, VehicleDeploymentData val)
	{
		writer.SetAsMap();
		writer.SetFlowStyle();
		writer.Write("type", val.type);
		writer.Write("pos", val.pos);
		writer.Write("dir", val.dir);
		// writer.write("used", val.used); // not needed
	}
}

/// <summary>
/// Represents a craft stored in a base.
/// Contains variable info about a craft like
/// position, fuel, damage, etc.
/// </summary>
public partial class Craft : MovingTarget
{
	/// <summary>Name of class used in script.</summary>
	public const string ScriptName = "Craft";

	private RuleCraft _rules;
	private Base _base;
	private int _fuel, _excessFuel, _damage, _shield, _interceptionOrder, _takeoff;
	private readonly List<CraftWeapon> _weapons = [];
	private readonly ItemContainer _items;
	private readonly ItemContainer _tempSoldierItems;
	private readonly ItemContainer _tempExtraItems;
	private readonly List<Vehicle> _vehicles = [];
	private string _status;
	private bool _lowFuel, _mission, _inBattlescape, _inDogfight;
	private double _speedMaxRadian;
	private RuleCraftStats _stats;
	private bool _isAutoPatrolling;
	private double _lonAuto, _latAuto;
	private List<int> _pilots = [];
	private SortedDictionary<int, (Position first, int second)> _customSoldierDeployment = [];
	private List<VehicleDeploymentData> _customVehicleDeployment = [];
	private int _skinIndex;

	/// <summary>Initializes a craft of the specified type and assigns it the latest craft ID available.</summary>
	/// <param name="rules">Pointer to ruleset.</param>
	/// <param name="base">Pointer to base of origin.</param>
	/// <param name="id">ID to assign to the craft (0 to not assign).</param>
	public Craft(RuleCraft rules, Base @base, int id = 0) : base()
	{
		_rules = rules;
		_base = @base;
		_fuel = 0;
		_excessFuel = 0;
		_damage = 0;
		_shield = 0;
		_interceptionOrder = 0;
		_takeoff = 0;
		_status = "STR_READY";
		_lowFuel = false;
		_mission = false;
		_inBattlescape = false;
		_inDogfight = false;
		_isAutoPatrolling = false;
		_lonAuto = 0.0;
		_latAuto = 0.0;
		_skinIndex = 0;

		_stats = new RuleCraftStats(rules.Stats);
		_items = new ItemContainer();
		_tempSoldierItems = new ItemContainer();
		_tempExtraItems = new ItemContainer();
		if (id != 0)
		{
			_id = id;
		}
		for (int i = 0; i < _rules.Weapons; ++i)
		{
			_weapons.Add(null);
		}
		if (@base != null)
		{
			SetBase(@base);
		}
		RecalcSpeedMaxRadian();
	}

	/// <summary>Helper method.</summary>
	private void RecalcSpeedMaxRadian()
	{
		_speedMaxRadian = CalculateRadianSpeed(_stats.speedMax) * 120;
	}

	/// <summary>Loads the craft from YAML.</summary>
	/// <param name="node">YAML node.</param>
	/// <param name="mod">Mod for the saved game.</param>
	/// <param name="save">Pointer to the saved game.</param>
	public void Load(YamlNodeReader node, Mod mod, SavedGame save)
	{
		var reader = node.UseIndex();
		base.Load(reader);

		reader.TryRead("fuel", ref _fuel);
		reader.TryRead("excessFuel", ref _excessFuel);
		reader.TryRead("damage", ref _damage);
		reader.TryRead("shield", ref _shield);

		int j = 0;
		foreach (var weaponsReader in reader["weapons"].Children())
		{
			if (_rules.Weapons > j)
			{
				string type = weaponsReader["type"].ReadVal<string>();
				var weapon = mod.GetCraftWeapon(type);
				if (type != "0" && weapon != null)
				{
					var w = new CraftWeapon(weapon, 0);
					w.Load(weaponsReader);
					_weapons[j] = w;
					_stats += weapon.BonusStats;
				}
				else
				{
					_weapons[j] = null;
					if (type != "0")
					{
						Logger.Error($"Failed to load craft weapon {type}");
					}
				}
				j++;
			}
		}

		_items.Load(reader["items"], mod);
		// Some old saves have bad items, better get rid of them to avoid further bugs
		foreach (var pair in _items.Contents.ToList())
		{
			var ruleItem = pair.Key;
			if (!ruleItem.CanBeEquippedToCraftInventory())
			{
				Logger.Warning(string.Create(CultureInfo.InvariantCulture, $"Item '{pair.Key.Type}' cannot be equipped in the craft inventory ({_rules.Type}, {_id}). Skipping {pair.Value} items."));
				_items.RemoveItem(pair.Key, pair.Value);
			}
		}
		foreach (var vehiclesReader in reader["vehicles"].Children())
		{
			string type = vehiclesReader["type"].ReadVal<string>();
			var ruleItem = mod.GetItem(type);
			if (ruleItem != null)
			{
				var ruleUnit = ruleItem.VehicleUnit;
				if (ruleUnit != null)
				{
					int size = ruleUnit.Armor.TotalSize;
					var v = new Vehicle(ruleItem, 0, size);
					v.Load(vehiclesReader);
					_vehicles.Add(v);
				}
				else
				{
					Logger.Error($"Failed to load vehicle {type}");
				}
			}
			else
			{
				Logger.Error($"Failed to load vehicles item {type}");
			}
		}
		reader.TryRead("status", ref _status);
		reader.TryRead("lowFuel", ref _lowFuel);
		reader.TryRead("mission", ref _mission);
		reader.TryRead("interceptionOrder", ref _interceptionOrder);
		var dest = reader["dest"];
		if (dest)
		{
			string type = dest["type"].ReadVal<string>();
			int id = dest["id"].ReadVal<int>();
			if (type == "STR_BASE")
			{
				ReturnToBase();
			}
			else if (type == "STR_UFO")
			{
				foreach (var ufo in save.Ufos)
				{
					if (ufo.Id == id)
					{
						Destination = ufo;
						break;
					}
				}
			}
			else if (type == "STR_WAY_POINT")
			{
				foreach (var wp in save.Waypoints)
				{
					if (wp.Id == id)
					{
						Destination = wp;
						break;
					}
				}
			}
			else
			{
				// Backwards compatibility
				if (type == "STR_ALIEN_TERROR")
					type = "STR_TERROR_SITE";
				bool found = false;
				foreach (var ms in save.MissionSites)
				{
					if (found) break; // loop finished
					if (ms.Id == id && ms.Deployment.MarkerName == type)
					{
						Destination = ms;
						found = true;
					}
				}
				foreach (var ab in save.AlienBases)
				{
					if (found) break; // loop finished
					if (ab.Id == id && ab.Deployment.MarkerName == type)
					{
						Destination = ab;
						found = true;
					}
				}
			}
		}
		reader.TryRead("takeoff", ref _takeoff);
		reader.TryRead("inBattlescape", ref _inBattlescape);
		reader.TryRead("isAutoPatrolling", ref _isAutoPatrolling);
		reader.TryRead("lonAuto", ref _lonAuto);
		reader.TryRead("latAuto", ref _latAuto);
		reader.TryRead("pilots", ref _pilots);
		reader.TryRead("customSoldierDeployment", ref _customSoldierDeployment);
		reader.TryRead("customVehicleDeployment", ref _customVehicleDeployment);
		reader.TryRead("skinIndex", ref _skinIndex);
		if (_skinIndex > _rules.MaxSkinIndex)
		{
			_skinIndex = 0;
		}
		if (_inBattlescape)
			Speed = 0;

		RecalcSpeedMaxRadian();

		if (mod != null)
		{
			_scriptValues.Load(reader, mod.ScriptGlobal);
		}
	}

	/// <summary>Finishes loading the craft from YAML (called after all other XCOM craft are loaded too).</summary>
	/// <param name="reader">YAML node.</param>
	/// <param name="save">The game data. Used to find the UFO's target (= xcom craft).</param>
	public void FinishLoading(YamlNodeReader reader, SavedGame save)
	{
		var dest = reader["dest"];
		if (dest)
		{
			string type = dest["type"].ReadVal<string>();
			int id = dest["id"].ReadVal<int>();

			bool found = false;
			foreach (var xbase in save.Bases)
			{
				if (found) break; // loop finished
				foreach (var xcraft in xbase.Crafts)
				{
					if (found) break; // loop finished
					if (xcraft.Id == id && xcraft.Rules.Type == type)
					{
						Destination = xcraft;
						found = true;
					}
				}
			}
		}
	}

	/// <summary>Initializes fixed weapons.</summary>
	public void InitFixedWeapons(Mod mod)
	{
		for (int i = 0; i < _rules.Weapons; ++i)
		{
			if (!string.IsNullOrEmpty(_rules.GetFixedWeaponInSlot(i)))
			{
				var rule = mod.GetCraftWeapon(_rules.GetFixedWeaponInSlot(i), true);
				var w = new CraftWeapon(rule, 0);
				AddCraftStats(w.Rules.BonusStats);
				_weapons[i] = w;
			}
		}

		// Remove craft weapons if needed
		if (MaxUnitsRaw < 0 || MaxVehiclesAndLargeSoldiersRaw < 0)
		{
			int weaponIndex = 0;
			foreach (var current in _weapons.ToList())
			{
				// as in C++, an empty slot is dereferenced here (crash there, NullReferenceException here)
				AddCraftStats(-current.Rules.BonusStats);
#pragma warning disable CA2245 // C++ setShield(getShield()) re-clamps the shield to the new capacity
				Shield = Shield;
#pragma warning restore CA2245
				_weapons[weaponIndex] = null;
				weaponIndex++;
			}
		}
	}

	/// <summary>Saves the craft to YAML (C++ <c>save(writer, shared)</c>; hides the <see cref="MovingTarget"/> overload like C++ does).</summary>
	public void Save(YamlNodeWriter writer, ScriptGlobal shared = null)
	{
		writer.SetAsMap();
		base.Save(writer);
		writer.Write("type", _rules.Type);
		writer.Write("fuel", _fuel);
		if (_excessFuel != 0)
			writer.Write("excessFuel", _excessFuel);
		writer.Write("damage", _damage);
		writer.Write("shield", _shield);
		writer.Write("weapons", _weapons,
			(vectorWriter, w) =>
			{
				var cwWriter = vectorWriter.Write();
				cwWriter.SetAsMap();
				if (w != null)
					w.Save(cwWriter);
				else
					cwWriter.Write("type", "0");
			});
		_items.Save(writer["items"]);
		writer.Write("vehicles", _vehicles,
			(vectorWriter, v) => v.Save(vectorWriter.Write()));
		writer.Write("status", _status);
		if (_lowFuel)
			writer.Write("lowFuel", _lowFuel);
		if (_mission)
			writer.Write("mission", _mission);
		if (_inBattlescape)
			writer.Write("inBattlescape", _inBattlescape);
		if (_interceptionOrder != 0)
			writer.Write("interceptionOrder", _interceptionOrder);
		if (_takeoff != 0)
			writer.Write("takeoff", _takeoff);
		if (_isAutoPatrolling)
			writer.Write("isAutoPatrolling", _isAutoPatrolling);
		writer.Write("lonAuto", SerializeDouble(_lonAuto));
		writer.Write("latAuto", SerializeDouble(_latAuto));
		if (_pilots.Count != 0)
			writer.Write("pilots", _pilots);
		if (_customSoldierDeployment.Count != 0)
			writer.Write("customSoldierDeployment", _customSoldierDeployment);
		if (_customVehicleDeployment.Count != 0)
			writer.Write("customVehicleDeployment", _customVehicleDeployment);
		if (_skinIndex != 0)
			writer.Write("skinIndex", _skinIndex);

		if (shared != null)
		{
			_scriptValues.Save(writer, shared);
		}
	}

	/// <summary>
	/// C++ <c>serializeDouble</c> (src/Savegame/SerializationHelper.cpp): ostream with precision DBL_DIG + 2, i.e. %.17g
	/// (including "nan"/"inf" like the C++ stream; shared with <see cref="MovingTarget"/>).
	/// </summary>
	internal static string SerializeDouble(double value) => SerializationHelper.SerializeDouble(value);

	/// <summary>Loads a craft unique identifier from YAML.</summary>
	public static (string first, int second) LoadId(YamlNodeReader reader)
	{
		return (reader["type"].ReadVal<string>(), reader["id"].ReadVal<int>());
	}

	/// <summary>Returns the craft's unique type used for savegame purposes.</summary>
	public override string Type => _rules.Type;

	/// <summary>Returns the ruleset for the craft's type.</summary>
	public RuleCraft Rules => _rules;

	/// <summary>Changes the ruleset for the craft's type. ONLY FOR NEW BATTLE USE!</summary>
	public void ChangeRules(RuleCraft rules)
	{
		_rules = rules;
		_stats = new RuleCraftStats(rules.Stats);
		_weapons.Clear();
		for (int i = 0; i < _rules.Weapons; ++i)
		{
			_weapons.Add(null);
		}
	}

	/// <summary>Returns the craft's unique default name.</summary>
	public override string GetDefaultName(Language lang)
	{
		return lang.GetString("STR_CRAFTNAME").Arg(lang.GetString(Type)).Arg(_id);
	}

	/// <summary>Returns the globe marker for the craft (-1 if none).</summary>
	public override int Marker
	{
		get
		{
			if (_status != "STR_OUT")
				return -1;
			else if (_rules.Marker == -1)
				return 1;
			return _rules.Marker;
		}
	}

	/// <summary>Returns the base the craft belongs to.</summary>
	public Base Base => _base;

	/// <summary>Changes the base the craft belongs to.</summary>
	/// <param name="base">Pointer to base.</param>
	/// <param name="move">Move the craft to the base coordinates.</param>
	public void SetBase(Base @base, bool move = true)
	{
		_base = @base;
		if (move)
		{
			_lon = @base.Longitude;
			_lat = @base.Latitude;
		}
	}

	/// <summary>The current status of the craft.</summary>
	public string Status
	{
		get => _status;
		set => _status = value;
	}

	/// <summary>Returns the current altitude of the craft.</summary>
	public string Altitude
	{
		get
		{
			if (_dest is Ufo u && u.Altitude != "STR_GROUND")
			{
				return u.Altitude;
			}
			else
			{
				return _rules.DefaultDisplayAltitude;
			}
		}
	}

	/// <summary>Changes the destination the craft is heading to.</summary>
	public override Target Destination
	{
		// Explicit getter: a setter-only override crashes the CA1859 analyzer (AD0001) at call sites reading it.
		get => base.Destination;
		set
		{
			if (_status != "STR_OUT")
			{
				_takeoff = 60;
			}
			if (value == null)
				Speed = _stats.speedMax / 2;
			else
				Speed = _stats.speedMax;
			base.Destination = value;
		}
	}

	public bool IsAutoPatrolling
	{
		get => _isAutoPatrolling;
		set => _isAutoPatrolling = value;
	}

	public double LongitudeAuto
	{
		get => _lonAuto;
		set => _lonAuto = value;
	}

	public double LatitudeAuto
	{
		get => _latAuto;
		set => _latAuto = value;
	}

	/// <summary>Returns the amount of weapons currently equipped on this craft.</summary>
	public int GetNumWeapons(bool onlyLoaded = false)
	{
		if (_rules.Weapons == 0)
		{
			return 0;
		}

		int total = 0;

		foreach (var cw in _weapons)
		{
			if (cw != null)
			{
				if (onlyLoaded && cw.Ammo == 0)
				{
					continue;
				}
				total++;
			}
		}

		return total;
	}

	/// <summary>Returns the amount of equipment currently equipped on this craft.</summary>
	public int NumEquipment => _items.TotalQuantity;

	/// <summary>Returns the list of weapons currently equipped in the craft.</summary>
	public List<CraftWeapon> Weapons => _weapons;

	/// <summary>Returns the list of items in the craft.</summary>
	public ItemContainer Items => _items;

	/// <summary>Returns the list of items in the craft equipped by the soldiers.</summary>
	public ItemContainer SoldierItems => _tempSoldierItems;

	/// <summary>Returns the list of items in the craft not equipped by the soldiers.</summary>
	public ItemContainer ExtraItems => _tempExtraItems;

	/// <summary>Returns the list of vehicles currently equipped in the craft.</summary>
	public List<Vehicle> Vehicles => _vehicles;

	/// <summary>Calculates (and stores) the sum of all equipment of all soldiers on the craft.</summary>
	public void CalculateTotalSoldierEquipment()
	{
		_tempSoldierItems.Clear();

		foreach (var soldier in _base.Soldiers)
		{
			if (soldier.Craft == this)
			{
				foreach (var invItem in soldier.EquipmentLayout)
				{
					// ignore fixed weapons...
					if (!invItem.IsFixed)
					{
						_tempSoldierItems.AddItem(invItem.ItemType);
					}
					// ...but not their ammo
					for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
					{
						var invItemAmmo = invItem.GetAmmoItemForSlot(slot);
						if (invItemAmmo != null)
						{
							_tempSoldierItems.AddItem(invItemAmmo);
						}
					}
				}
			}
		}
	}

	/// <summary>Gets the total storage size of all items in the craft. Including vehicles+ammo and craft weapons+ammo.</summary>
	public double TotalItemStorageSize
	{
		get
		{
			double total = _items.TotalSize;

			foreach (var v in _vehicles)
			{
				total += v.Rules.Size;

				var clip = v.Rules.VehicleClipAmmo;
				if (clip != null)
				{
					total += clip.Size * v.Rules.VehicleClipsLoaded;
				}
			}

			foreach (var w in _weapons)
			{
				if (w != null)
				{
					total += w.Rules.LauncherItem.Size;

					var clip = w.Rules.ClipItem;
					if (clip != null)
					{
						total += clip.Size * w.ClipsLoaded;
					}
				}
			}

			return total;
		}
	}

	/// <summary>Gets the total number of items of a given type in the craft. Including vehicles+ammo and craft weapons+ammo.</summary>
	public int GetTotalItemCount(RuleItem item)
	{
		int qty = _items.GetItem(item);

		foreach (var v in _vehicles)
		{
			if (v.Rules == item)
			{
				qty += 1;
			}
			else if (v.Rules.VehicleClipAmmo == item)
			{
				qty += v.Rules.VehicleClipsLoaded;
			}
		}

		foreach (var w in _weapons)
		{
			if (w != null)
			{
				if (w.Rules.LauncherItem == item)
				{
					qty += 1;
				}
				else if (w.Rules.ClipItem == item)
				{
					qty += w.ClipsLoaded;
				}
			}
		}

		return qty;
	}

	/// <summary>Update stats of craft.</summary>
	public void AddCraftStats(RuleCraftStats s)
	{
		Damage = _damage + s.damageMax; //you need "fix" new damage capability first before use.
		_stats += s;

		Fuel = _fuel;

		RecalcSpeedMaxRadian();
	}

	/// <summary>Gets all basic stats of craft.</summary>
	public RuleCraftStats CraftStats => _stats;

	/// <summary>Returns current max amount of fuel that craft can carry.</summary>
	public int FuelMax => _stats.fuelMax;

	/// <summary>The amount of fuel currently contained in this craft (overflow returns refuel items to the base).</summary>
	public int Fuel
	{
		get => _fuel;
		set
		{
			_fuel = value;
			if (_fuel > _stats.fuelMax)
			{
				int overflowFuel = _fuel - _stats.fuelMax + _excessFuel;
				if (overflowFuel > 0 && _rules.RefuelItem != null)
				{
					int returnQty = overflowFuel / _rules.RefuelRate;
					if (returnQty > 0)
					{
						_base.StorageItems.AddItem(_rules.RefuelItem, returnQty);
					}
					_excessFuel = overflowFuel % _rules.RefuelRate;
				}
				_fuel = _stats.fuelMax;
			}
			else if (_fuel < 0)
			{
				_fuel = 0;
			}
		}
	}

	/// <summary>Returns the ratio between the amount of fuel currently contained in this craft and the total it can carry.</summary>
	public int FuelPercentage => (int)Math.Floor((double)_fuel / _stats.fuelMax * 100.0);

	/// <summary>Return current max amount of damage this craft can take.</summary>
	public int DamageMax => _stats.damageMax;

	/// <summary>The amount of damage this craft has taken (never below 0).</summary>
	public int Damage
	{
		get => _damage;
		set
		{
			_damage = value;
			if (_damage < 0)
			{
				_damage = 0;
			}
		}
	}

	/// <summary>Returns the ratio between the amount of damage this craft can take and the total it can take before it's destroyed.</summary>
	public int DamagePercentage => (int)Math.Floor((double)_damage / _stats.damageMax * 100);

	/// <summary>Gets the max shield capacity of this craft.</summary>
	public int ShieldCapacity => _stats.shieldCapacity;

	/// <summary>The amount of shield this craft has remaining, capped at the capacity plus bonuses.</summary>
	public int Shield
	{
		get => _shield;
		set => _shield = Math.Max(0, Math.Min(_stats.shieldCapacity, value));
	}

	/// <summary>Returns the percentage of shields remaining out of the max capacity.</summary>
	public int ShieldPercentage => _stats.shieldCapacity != 0 ? _shield * 100 / _stats.shieldCapacity : 0;

	/// <summary>Returns whether the craft is ignored by hunter-killers (is returning from a mission or is low on fuel).</summary>
	public bool IsIgnoredByHK => MissionComplete || LowFuel;

	/// <summary>Whether the craft is currently low on fuel (only has enough to head back to base).</summary>
	public bool LowFuel
	{
		get => _lowFuel;
		set => _lowFuel = value;
	}

	/// <summary>Whether the craft has just done a ground mission, and is forced to return to base.</summary>
	public bool MissionComplete
	{
		get => _mission;
		set => _mission = value;
	}

	/// <summary>Returns the current distance between the craft and the base it belongs to (radian).</summary>
	public double DistanceFromBase => GetDistance(_base);

	/// <summary>Returns the amount of fuel the craft uses up while it's on the air.</summary>
	/// <param name="speed">Craft speed for estimation.</param>
	/// <param name="escortSpeed">Speed of the escorted craft (0 = none).</param>
	public int GetFuelConsumption(int speed, int escortSpeed)
	{
		if (_rules.RefuelItem != null)
			return 1;
		if (escortSpeed > 0)
		{
			// based on the speed of the escorted craft, but capped between 50% and 100% of escorting craft's speed
			return Math.Max(_stats.speedMax / 200, Math.Min(escortSpeed / 100, _stats.speedMax / 100));
		}
		return (int)Math.Floor(speed / 100.0);
	}

	/// <summary>Returns the minimum required fuel for the craft to make it back to base.</summary>
	public int FuelLimit => GetFuelLimit(_base);

	/// <summary>Returns the minimum required fuel for the craft to go to a base.</summary>
	public int GetFuelLimit(Base @base)
	{
		return (int)Math.Floor(GetFuelConsumption(_stats.speedMax, 0) * GetDistance(@base) / _speedMaxRadian);
	}

	/// <summary>C++ fmath Clamp: std::min(std::max(x, min), max) (does not throw when min &gt; max).</summary>
	private static int Clamp(int x, int min, int max) => Math.Min(Math.Max(x, min), max);

	/// <summary>Gets the maximum number of units (soldiers and vehicles, small and large) that the craft can carry.</summary>
	public int MaxUnitsClamped => Clamp(_stats.soldiers, 0, _rules.MaxUnitsLimit);

	public int MaxUnitsRaw => _stats.soldiers;

	/// <summary>Gets the maximum number of vehicles (and 2x2 soldiers) that the craft can carry.</summary>
	public int MaxVehiclesAndLargeSoldiersClamped => Clamp(_stats.vehicles, 0, _rules.MaxVehiclesAndLargeSoldiersLimit);

	public int MaxVehiclesAndLargeSoldiersRaw => _stats.vehicles;

	/// <summary>Gets the item limit for this craft.</summary>
	public int MaxItemsClamped => Math.Max(0, _stats.maxItems);

	public int MaxItemsRaw
	{
		get => _stats.maxItems;
		set => _stats.maxItems = value;
	}

	/// <summary>Gets the item storage space limit for this craft.</summary>
	public double MaxStorageSpaceClamped => 0.0 < _stats.maxStorageSpace ? _stats.maxStorageSpace : 0.0; // std::max(0.0, x)

	public double MaxStorageSpaceRaw
	{
		get => _stats.maxStorageSpace;
		set => _stats.maxStorageSpace = value;
	}

	/// <summary>Returns the maximum range the craft can travel from its origin base on its current fuel (radians).</summary>
	public double BaseRange => _fuel / 2.0 / GetFuelConsumption(_stats.speedMax, 0) * _speedMaxRadian;

	/// <summary>Sends the craft back to its origin base.</summary>
	public void ReturnToBase()
	{
		Destination = _base;
	}

	/// <summary>Returns the crew to their base (using transfers).</summary>
	public void EvacuateCrew(Mod mod)
	{
		var soldiers = _base.Soldiers;
		for (int i = 0; i < soldiers.Count;)
		{
			var soldier = soldiers[i];
			if (soldier.Craft == this)
			{
				int survivalChance = IsPilot(soldier.Id) ? mod.PilotsEmergencyEvacuationSurvivalChance : mod.CrewEmergencyEvacuationSurvivalChance;
				if (Rng.Percent(survivalChance))
				{
					// remove from craft
					soldier.SetCraft(null);
					// remove from training, but remember to return to training when back in the base
					{
						if (soldier.IsInTraining)
						{
							soldier.ReturnToTrainingWhenHealed = true;
						}
						soldier.SetTraining(false);
					}
					// transfer to base
					var t = new Transfer(mod.PersonnelTime);
					t.Soldier = soldier;
					_base.Transfers.Add(t);
					// next
					soldiers.RemoveAt(i);
				}
				else
				{
					++i; // will be killed later
				}
			}
			else
			{
				++i; // next
			}
		}
		RemoveAllPilots(); // just in case
	}

	/// <summary>Checks if a target is detected by the craft's radar, taking in account the range and chance.</summary>
	public UfoDetection Detect(Ufo target, SavedGame save, bool alreadyTracked)
	{
		int distance = XcomDistance(GetDistance(target));

		int detectionChance = 0;
		UfoDetection detectionType = UfoDetection.DETECTION_NONE;

		if (distance < _stats.radarRange)
		{
			// backward compatibility with vanilla
			if (_stats.radarChance == 100)
			{
				detectionType = UfoDetection.DETECTION_RADAR;
				detectionChance = 100;
			}
			else
			{
				detectionType = UfoDetection.DETECTION_RADAR;
				if (alreadyTracked)
				{
					detectionChance = 100;
				}
				else
				{
					detectionChance = _stats.radarChance * (100 + target.Visibility) / 100;
				}
			}
		}

		// script hook DetectUfoFromCraft not ported: the unmodified values are used
		return Rng.Percent(detectionChance) ? detectionType : UfoDetection.DETECTION_NONE;
	}

	/// <summary>C++ fmath XcomDistance: inversion of Nautical distance, use same value as in rulesets definitions.</summary>
	internal static int XcomDistance(double nautical) => (int)(nautical * 60.0 * (180.0 / Math.PI));

	/// <summary>Moves the craft to its destination.</summary>
	/// <returns>True if the craft arrived at its base.</returns>
	public bool Think()
	{
		if (_takeoff == 0)
		{
			Move();
		}
		else
		{
			_takeoff--;
			ResetMeetPoint();
		}
		if (ReachedDestination() && _dest == (Target)_base)
		{
			InterceptionOrder = 0; // just to be sure
			Checkup();
			Destination = null;
			Speed = 0;
			_lowFuel = false;
			_mission = false;
			_takeoff = 0;
			return true;
		}
		return false;
	}

	/// <summary>Is the craft about to take off?</summary>
	public bool IsTakingOff => _takeoff == 60;

	/// <summary>Checks the condition of all the craft's systems to define its new status (eg. when arriving at base).</summary>
	public void Checkup()
	{
		int available = 0, full = 0;
		foreach (var cw in _weapons)
		{
			if (cw == null)
				continue;
			available++;
			if (cw.Ammo >= cw.Rules.AmmoMax || cw.IsDisabled)
			{
				full++;
			}
			else
			{
				cw.SetRearming(true);
			}
		}

		if (_damage > 0)
		{
			_status = "STR_REPAIRS";
		}
		else if (available != full)
		{
			_status = "STR_REARMING";
		}
		else if (_fuel < _stats.fuelMax)
		{
			_status = "STR_REFUELLING";
		}
		else
		{
			_status = "STR_READY";
		}
	}

	/// <summary>Consumes the craft's fuel every 10 minutes while it's on the air.</summary>
	public void ConsumeFuel(int escortSpeed)
	{
		if (_dest == null && _rules.PatrolWithoutFuel())
		{
			// patrol without fuel consumption
			return;
		}
		Fuel = _fuel - GetFuelConsumption(_speed, escortSpeed);
	}

	/// <summary>Returns how long in hours until the craft is repaired.</summary>
	public uint CalcRepairTime()
	{
		uint repairTime = 0;

		if (_damage > 0)
		{
			repairTime = (uint)(int)Math.Ceiling((double)_damage / _rules.RepairRate);
		}
		return repairTime;
	}

	/// <summary>Returns how long in hours until the craft is refuelled (assumes fuel is available).</summary>
	public uint CalcRefuelTime()
	{
		uint refuelTime = 0;

		int needed = _stats.fuelMax - _fuel;
		if (needed > 0)
		{
			refuelTime = (uint)(int)Math.Ceiling((double)needed / _rules.RefuelRate / 2.0);
		}
		return refuelTime;
	}

	/// <summary>Returns how long in hours until the craft is re-armed (assumes ammo is available).</summary>
	public uint CalcRearmTime()
	{
		uint rearmTime = 0;

		for (int idx = 0; idx < _rules.Weapons; idx++)
		{
			var w1 = _weapons[idx];
			if (w1 != null && !w1.IsDisabled)
			{
				int needed = w1.Rules.AmmoMax - w1.Ammo;
				if (needed > 0)
				{
					rearmTime += (uint)(int)Math.Ceiling((double)needed / w1.Rules.RearmRate);
				}
			}
		}

		return rearmTime;
	}

	/// <summary>Repairs the craft's damage every hour while it's docked in the base.</summary>
	public void Repair()
	{
		Damage = _damage - _rules.RepairRate;
		if (_damage <= 0)
		{
			_status = "STR_REARMING";
		}
	}

	/// <summary>Refuels the craft every 30 minutes while it's docked in the base.</summary>
	/// <returns>The item ID missing for refuelling, or "" if none.</returns>
	public string Refuel()
	{
		string fuel = "";
		if (_fuel < _stats.fuelMax)
		{
			var item = _rules.RefuelItem;
			if (item == null)
			{
				Fuel = _fuel + _rules.RefuelRate;
			}
			else
			{
				if (_base.StorageItems.GetItem(item) > 0)
				{
					_base.StorageItems.RemoveItem(item);
					Fuel = _fuel + _rules.RefuelRate;
					_lowFuel = false;
				}
				else if (!_lowFuel)
				{
					fuel = item.Type;
					if (_fuel > 0)
					{
						_status = "STR_READY";
					}
					else
					{
						_lowFuel = true;
					}
				}
			}
		}
		if (_fuel >= _stats.fuelMax)
		{
			_status = "STR_READY";
			foreach (var cw in _weapons)
			{
				if (cw != null && cw.IsRearming)
				{
					_status = "STR_REARMING";
					break;
				}
			}
		}
		return fuel;
	}

	/// <summary>Rearms the craft's weapons by adding ammo every hour while it's docked in the base.</summary>
	/// <returns>The ammo missing for rearming, or null if none.</returns>
	public RuleItem Rearm()
	{
		RuleItem ammo = null;
		for (int i = 0; ; ++i)
		{
			if (i == _weapons.Count)
			{
				_status = "STR_REFUELLING";
				break;
			}
			var cw = _weapons[i];
			if (cw != null && cw.IsRearming)
			{
				var clip = cw.Rules.ClipItem;
				int available = _base.StorageItems.GetItem(clip);
				if (clip == null)
				{
					cw.Rearm(0, 0);
				}
				else if (available > 0)
				{
					int used = cw.Rearm(available, clip.ClipSize);

					if (used == available && cw.IsRearming)
					{
						ammo = clip;
						cw.SetRearming(false);
					}

					_base.StorageItems.RemoveItem(clip, used);
				}
				else
				{
					ammo = clip;
					cw.SetRearming(false);
				}
				break;
			}
		}
		return ammo;
	}

	/// <summary>Changes the craft's battlescape status.</summary>
	public void SetInBattlescape(bool inbattle)
	{
		if (inbattle)
			Speed = 0;
		_inBattlescape = inbattle;
	}

	/// <summary>Returns the craft's battlescape status.</summary>
	public bool IsInBattlescape => _inBattlescape;

	/// <summary>Returns the craft destroyed status (damage reached the maximum during dogfights).</summary>
	public bool IsDestroyed => _damage >= _stats.damageMax;

	/// <summary>Returns the amount of space available for soldiers and vehicles.</summary>
	public int SpaceAvailable => MaxUnitsClamped - SpaceUsed;

	/// <summary>Returns the amount of space in use by soldiers and vehicles.</summary>
	public int SpaceUsed
	{
		get
		{
			int vehicleSpaceUsed = 0;
			foreach (var vehicle in _vehicles)
			{
				vehicleSpaceUsed += vehicle.TotalSize;
			}
			foreach (var soldier in _base.Soldiers)
			{
				if (soldier.Craft == this)
				{
					vehicleSpaceUsed += soldier.Armor.TotalSize;
				}
			}
			return vehicleSpaceUsed;
		}
	}

	/// <summary>Checks if the commander is onboard.</summary>
	public bool IsCommanderOnboard
	{
		get
		{
			foreach (var soldier in _base.Soldiers)
			{
				if (soldier.Craft == this && soldier.Rank == SoldierRank.RANK_COMMANDER)
				{
					return true;
				}
			}
			return false;
		}
	}

	/// <summary>Checks if there are only permitted soldier types onboard.</summary>
	public bool AreOnlyPermittedSoldierTypesOnboard(RuleStartingCondition sc)
	{
		foreach (var soldier in _base.Soldiers)
		{
			if (soldier.Craft == this && !sc.IsSoldierTypePermitted(soldier.Rules.Type))
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>Checks if there are enough required items onboard.</summary>
	public bool AreRequiredItemsOnboard(SortedDictionary<string, int> requiredItems)
	{
		foreach (var mapItem in requiredItems)
		{
			if (_items.GetItem(mapItem.Key) < mapItem.Value)
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>Destroys given required items.</summary>
	public void DestroyRequiredItems(SortedDictionary<string, int> requiredItems)
	{
		foreach (var mapItem in requiredItems)
		{
			_items.RemoveItem(mapItem.Key, mapItem.Value);
		}
	}

	/// <summary>Checks item limits.</summary>
	/// <returns>True if there are too many items onboard.</returns>
	public bool AreTooManyItemsOnboard()
	{
		if (_items.TotalQuantity > MaxItemsClamped)
		{
			return true;
		}
		if (_items.TotalSize > MaxStorageSpaceClamped + 0.05)
		{
			return true;
		}
		return false;
	}

	/// <summary>Checks armor constraints.</summary>
	/// <returns>True if there are soldiers wearing banned armor onboard.</returns>
	public bool AreBannedArmorsOnboard()
	{
		if (_rules.AllowedArmorGroups.Count != 0)
		{
			var allowedArmorGroups = _rules.AllowedArmorGroups;
			foreach (var xsoldier in _base.Soldiers)
			{
				if (xsoldier.Craft == this)
				{
					if (!allowedArmorGroups.Contains(xsoldier.Armor.Group))
					{
						return true;
					}
				}
			}
		}
		if (_rules.LimitArmorGroups.Count != 0)
		{
			var limitArmorGroups = _rules.LimitArmorGroups;
			foreach (var limit in limitArmorGroups)
			{
				int subTotal = 0;
				foreach (var tmpSoldier in _base.Soldiers)
				{
					if (tmpSoldier.Craft == this && tmpSoldier.Armor.Group == limit.Key)
					{
						++subTotal;
					}
				}
				if (subTotal > limit.Value)
				{
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>Checks if there are enough pilots onboard.</summary>
	public bool ArePilotsOnboard(Mod mod)
	{
		if (_rules.Pilots == 0)
			return true;

		// refresh the list of pilots (must be performed here, list may be out-of-date!)
		var pilots = GetPilotList(true, mod);

		return pilots.Count >= _rules.Pilots;
	}

	/// <summary>Checks if a pilot is already on the list.</summary>
	public bool IsPilot(int pilotId)
	{
		return _pilots.Contains(pilotId);
	}

	/// <summary>Adds a pilot to the list.</summary>
	public void AddPilot(int pilotId)
	{
		if (!_pilots.Contains(pilotId))
		{
			_pilots.Add(pilotId);
		}
	}

	/// <summary>Removes all pilots from the list.</summary>
	public void RemoveAllPilots()
	{
		_pilots.Clear();
	}

	/// <summary>Gets the list of craft pilots.</summary>
	public List<Soldier> GetPilotList(bool autoAdd, Mod mod)
	{
		var result = new List<Soldier>();

		// 1. no pilots needed
		if (_rules.Pilots == 0)
			return result;

		{
			// 2. just enough pilots or pilot candidates onboard (assign them all automatically)
			int total = 0;
			foreach (var soldier in _base.Soldiers)
			{
				if (soldier.Craft == this && mod != null)
				{
					soldier.PrepareStatsWithBonuses(mod); // refresh stats for checking pilot requirements
				}
				if (soldier.Craft == this && soldier.HasAllPilotingRequirements())
				{
					result.Add(soldier);
					total++;
				}
			}
			if (total == _rules.Pilots)
			{
				// nothing more to do
			}
			else
			{
				// 3. mix of manually selected pilots and pilot candidates onboard
				int total2 = 0;
				result.Clear();
				// 3a. first take all available (manually selected) pilots
				foreach (int soldierId in _pilots)
				{
					foreach (var soldier in _base.Soldiers)
					{
						if (soldier.Craft == this && soldier.Id == soldierId && soldier.HasAllPilotingRequirements())
						{
							result.Add(soldier);
							total2++;
							break; // pilot found, don't search anymore
						}
					}
					if (total2 >= _rules.Pilots)
					{
						break; // enough pilots found
					}
				}
				if (autoAdd)
				{
					// 3b. if not enough manually selected pilots, take some pilot candidates automatically (take from the rear first)
					var soldiers = _base.Soldiers;
					for (int i = soldiers.Count - 1; i >= 0; --i)
					{
						var soldier = soldiers[i];
						if (soldier.Craft == this && !IsPilot(soldier.Id) && soldier.HasAllPilotingRequirements())
						{
							result.Add(soldier);
							total2++;
						}
						if (total2 >= _rules.Pilots)
						{
							break; // enough pilots found
						}
					}
				}
			}
		}

		// remember the pilots and return
		RemoveAllPilots();
		foreach (var soldier in result)
		{
			AddPilot(soldier.Id);
		}
		return result;
	}

	/// <summary>C++ <c>int / size_t</c>: the division is unsigned (only differs for a negative sum).</summary>
	private static int AverageLikeCpp(int sum, int count) => (int)((ulong)sum / (ulong)count);

	/// <summary>Calculates the accuracy bonus based on pilot skills.</summary>
#pragma warning disable CA1822 // instance member in C++ API
	public int GetPilotAccuracyBonus(List<Soldier> pilots, Mod mod)
	{
		if (pilots.Count == 0)
			return 0;

		int firingAccuracy = 0;
		foreach (var soldier in pilots)
		{
			firingAccuracy += soldier.StatsWithSoldierBonusesOnly.firing;
		}
		firingAccuracy = AverageLikeCpp(firingAccuracy, pilots.Count); // average firing accuracy of all pilots

		return ((firingAccuracy - mod.PilotAccuracyZeroPoint) * mod.PilotAccuracyRange) / 100;
	}

	/// <summary>Calculates the dodge bonus based on pilot skills.</summary>
	public int GetPilotDodgeBonus(List<Soldier> pilots, Mod mod)
	{
		if (pilots.Count == 0)
			return 0;

		int reactions = 0;
		foreach (var soldier in pilots)
		{
			reactions += soldier.StatsWithSoldierBonusesOnly.reactions;
		}
		reactions = AverageLikeCpp(reactions, pilots.Count); // average reactions of all pilots

		return ((reactions - mod.PilotReactionsZeroPoint) * mod.PilotReactionsRange) / 100;
	}

	/// <summary>Calculates the approach speed modifier based on pilot skills.</summary>
	public int GetPilotApproachSpeedModifier(List<Soldier> pilots, Mod mod)
	{
		if (pilots.Count == 0)
			return 2; // vanilla

		int bravery = 0;
		foreach (var soldier in pilots)
		{
			bravery += soldier.StatsWithSoldierBonusesOnly.bravery;
		}
		bravery = AverageLikeCpp(bravery, pilots.Count); // average bravery of all pilots

		if (bravery >= mod.PilotBraveryThresholdVeryBold)
		{
			return 4; // double the speed
		}
		else if (bravery >= mod.PilotBraveryThresholdBold)
		{
			return 3; // 50% speed increase
		}
		else if (bravery >= mod.PilotBraveryThresholdNormal)
		{
			return 2; // normal speed
		}
		else
		{
			return 1; // half the speed
		}
	}
#pragma warning restore CA1822

	/// <summary>Returns the total amount of vehicles of a certain type stored in the craft.</summary>
	public int GetVehicleCount(string vehicle)
	{
		int total = 0;
		foreach (var v in _vehicles)
		{
			if (v.Rules.Type == vehicle)
			{
				total++;
			}
		}
		return total;
	}

	/// <summary>Changes the craft's dogfight status.</summary>
	public void SetInDogfight(bool inDogfight)
	{
		_inDogfight = inDogfight;
	}

	/// <summary>Returns the craft's dogfight status.</summary>
	public bool IsInDogfight => _inDogfight;

	/// <summary>Interception order (first craft to leave the base gets 1, second 2, etc.).</summary>
	public int InterceptionOrder
	{
		get => _interceptionOrder;
		set => _interceptionOrder = value;
	}

	/// <summary>Gets the craft's unique id: the craft's type and per-type id.</summary>
	public (string first, int second) UniqueId => (_rules.Type, _id);

	/// <summary>Unloads all the craft contents to the base.</summary>
	public void Unload()
	{
		// Remove weapons
		for (int i = 0; i < _weapons.Count; ++i)
		{
			var cw = _weapons[i];
			if (cw != null)
			{
				_base.StorageItems.AddItem(cw.Rules.LauncherItem);
				_base.StorageItems.AddItem(cw.Rules.ClipItem, cw.ClipsLoaded);
				_weapons[i] = null;
			}
		}

		// Remove items
		foreach (var pair in _items.Contents)
		{
			_base.StorageItems.AddItem(pair.Key, pair.Value);
		}

		// Remove vehicles
		foreach (var vehicle in _vehicles)
		{
			_base.StorageItems.AddItem(vehicle.Rules);
			if (vehicle.Rules.VehicleClipAmmo != null)
			{
				_base.StorageItems.AddItem(vehicle.Rules.VehicleClipAmmo, vehicle.Rules.VehicleClipsLoaded);
			}
		}
		_vehicles.Clear();

		// Remove soldiers
		foreach (var soldier in _base.Soldiers)
		{
			if (soldier.Craft == this)
			{
				soldier.SetCraft(null);
			}
		}
	}

	/// <summary>Checks if an item can be reused by the craft and updates its status appropriately.</summary>
	public void ReuseItem(RuleItem item)
	{
		// Note: Craft in-base status hierarchy is repair, rearm, refuel, ready.
		// We only want to interrupt processes that are lower in the hierarchy.
		// (And we don't want to interrupt any out-of-base status.)

		// The only states we are willing to interrupt are "ready" and "refuelling"
		if (_status != "STR_READY" && _status != "STR_REFUELLING")
		{
			return;
		}

		// Check if it's ammo to reload the craft
		foreach (var cw in _weapons)
		{
			if (cw != null && item == cw.Rules.ClipItem && cw.Ammo < cw.Rules.AmmoMax && !cw.IsDisabled)
			{
				cw.SetRearming(true);
				_status = "STR_REARMING";
			}
		}

		// Only consider refuelling if everything else is complete
		if (_status != "STR_READY")
			return;

		// Check if it's fuel to refuel the craft
		if (item == _rules.RefuelItem && _fuel < _stats.fuelMax)
			_status = "STR_REFUELLING";
	}

	/// <summary>Gets the attraction value of the craft for alien hunter-killers (the higher the less attractive).</summary>
	public int GetHunterKillerAttraction(int huntMode)
	{
		int attraction = 0;
		if (huntMode == 0)
		{
			// prefer interceptors...
			if (_rules.AllowLanding)
			{
				// craft that can land (i.e. transports) are not attractive
				attraction += 1000000;
			}
			if (MaxUnitsClamped > 0)
			{
				// craft with more crew capacity (i.e. transports) are less attractive
				attraction += 500000 + (MaxUnitsClamped * 1000);
			}
			// faster craft (i.e. interceptors) are more attractive
			attraction += 100000 - _stats.speedMax;
			// craft with more damage taken are less attractive
			// this is just to simplify re-targeting when interceptor is fast enough to disengage
			// and another identical but healthier interceptor is waiting for its chance
			attraction += _damage * 100 / _stats.damageMax;
		}
		else
		{
			// prefer transports...
			if (!_rules.AllowLanding)
			{
				// craft that cannot land (i.e. interceptors) are not attractive
				attraction += 1000000;
			}
			// craft with more crew capacity (i.e. transports) are more attractive
			attraction += 500000 - (MaxUnitsClamped * 1000);
			// faster craft (i.e. interceptors) are less attractive
			attraction += 100000 + _stats.speedMax;
		}

		// the higher the number the less attractive the target is for UFO hunter-killers
		return attraction;
	}

	/// <summary>The craft's skin index.</summary>
	public int SkinIndex
	{
		get => _skinIndex;
		set => _skinIndex = value;
	}

	/// <summary>Gets the craft's skin sprite ID.</summary>
	public int SkinSprite => Rules.GetSprite(_skinIndex);

	/// <summary>Gets the craft's custom deployment of soldiers.</summary>
	public SortedDictionary<int, (Position first, int second)> CustomSoldierDeployment => _customSoldierDeployment;

	/// <summary>Gets the craft's custom deployment of vehicles.</summary>
	public List<VehicleDeploymentData> CustomVehicleDeployment => _customVehicleDeployment;

	/// <summary>Does this craft have a custom deployment set?</summary>
	public bool HasCustomDeployment => _customSoldierDeployment.Count != 0 || _customVehicleDeployment.Count != 0;

	/// <summary>Resets the craft's custom deployment.</summary>
	public void ResetCustomDeployment()
	{
		if (_customSoldierDeployment.Count != 0)
		{
			_customSoldierDeployment.Clear();
		}
		if (_customVehicleDeployment.Count != 0)
		{
			_customVehicleDeployment.Clear();
		}
	}

	/// <summary>Resets the craft's custom deployment of vehicles temp variables.</summary>
	public void ResetTemporaryCustomVehicleDeploymentFlags()
	{
		foreach (var depl in _customVehicleDeployment)
		{
			depl.used = false;
		}
	}

	/// <summary>Returns the amount of vehicles and 2x2 soldiers currently contained in this craft.</summary>
	public int NumVehiclesAndLargeSoldiers => NumTotalVehicles + NumLargeSoldiers;

	/// <summary>Returns the amount of 1x1 soldiers that are currently attached to this craft.</summary>
	public int NumSmallSoldiers
	{
		get
		{
			if (_rules.MaxUnitsLimit == 0)
				return 0;

			int total = 0;

			foreach (var s in _base.Soldiers)
			{
				if (s.Craft == this && s.Armor.Size == 1)
					++total;
			}

			return total;
		}
	}

	/// <summary>Returns the amount of 2x2 soldiers that are currently attached to this craft.</summary>
	public int NumLargeSoldiers
	{
		get
		{
			if (_rules.MaxUnitsLimit == 0)
				return 0;

			int total = 0;

			foreach (var s in _base.Soldiers)
			{
				if (s.Craft == this && s.Armor.Size == 2)
					++total;
			}

			return total;
		}
	}

	/// <summary>Returns the amount of 1x1 vehicles that are currently attached to this craft.</summary>
	public int NumSmallVehicles
	{
		get
		{
			if (_rules.MaxUnitsLimit == 0)
				return 0;

			int total = 0;

			foreach (var v in _vehicles)
			{
				if (v.TotalSize == 1)
					++total;
			}

			return total;
		}
	}

	/// <summary>Returns the amount of 2x2 vehicles that are currently attached to this craft.</summary>
	public int NumLargeVehicles
	{
		get
		{
			if (_rules.MaxUnitsLimit == 0)
				return 0;

			int total = 0;

			foreach (var v in _vehicles)
			{
				if (v.TotalSize > 1)
					++total;
			}

			return total;
		}
	}

	/// <summary>Returns the amount of 1x1 units that are currently attached to this craft.</summary>
	public int NumSmallUnits => NumSmallSoldiers + NumSmallVehicles;

	/// <summary>Returns the amount of 2x2 units that are currently attached to this craft.</summary>
	public int NumLargeUnits => NumLargeSoldiers + NumLargeVehicles;

	/// <summary>Returns the total amount of soldiers that are currently attached to this craft.</summary>
	public int NumTotalSoldiers
	{
		get
		{
			if (_rules.MaxUnitsLimit == 0)
				return 0;

			int total = 0;

			foreach (var s in _base.Soldiers)
			{
				if (s.Craft == this)
					++total;
			}

			return total;
		}
	}

	/// <summary>Returns the total amount of vehicles that are currently attached to this craft.</summary>
	public int NumTotalVehicles => _vehicles.Count;

	/// <summary>Returns the total amount of units that are currently attached to this craft.</summary>
	public int NumTotalUnits => NumTotalSoldiers + NumTotalVehicles;

	/// <summary>Validates craft space and craft constraints on soldier armor change.</summary>
	/// <returns>True, if armor change is allowed.</returns>
	public bool ValidateArmorChange(int sizeFrom, int sizeTo)
	{
		if (sizeFrom == sizeTo)
		{
			return true;
		}
		else
		{
			if (sizeFrom < sizeTo)
			{
				if (SpaceAvailable < 3)
				{
					return false;
				}
				if (NumVehiclesAndLargeSoldiers >= MaxVehiclesAndLargeSoldiersClamped)
				{
					return false;
				}
				if (_rules.MaxLargeSoldiers > -1 && NumLargeSoldiers >= _rules.MaxLargeSoldiers)
				{
					return false;
				}
				if (_rules.MaxLargeUnits > -1 && NumLargeUnits >= _rules.MaxLargeUnits)
				{
					return false;
				}
			}
			else if (sizeFrom > sizeTo)
			{
				if (_rules.MaxSmallSoldiers > -1 && NumSmallSoldiers >= _rules.MaxSmallSoldiers)
				{
					return false;
				}
				if (_rules.MaxSmallUnits > -1 && NumSmallUnits >= _rules.MaxSmallUnits)
				{
					return false;
				}
			}
		}
		return true;
	}

	/// <summary>Validates craft space and craft constraints on adding soldier to a craft.</summary>
	public CraftPlacementErrors ValidateAddingSoldier(int space, Soldier s)
	{
		if (space < s.Armor.TotalSize)
		{
			return CraftPlacementErrors.CPE_NotEnoughSpace;
		}
		if (_rules.MaxSoldiers > -1 && NumTotalSoldiers >= _rules.MaxSoldiers)
		{
			return CraftPlacementErrors.CPE_TooManySoldiers;
		}
		if (s.Armor.Size == 1)
		{
			if (_rules.MaxSmallSoldiers > -1 && NumSmallSoldiers >= _rules.MaxSmallSoldiers)
			{
				return CraftPlacementErrors.CPE_TooManySmallSoldiers;
			}
			if (_rules.MaxSmallUnits > -1 && NumSmallUnits >= _rules.MaxSmallUnits)
			{
				return CraftPlacementErrors.CPE_TooManySmallUnits;
			}
		}
		else // armorSize > 1
		{
			if (NumVehiclesAndLargeSoldiers >= MaxVehiclesAndLargeSoldiersClamped)
			{
				return CraftPlacementErrors.CPE_TooManyVehiclesAndLargeSoldiers;
			}
			if (_rules.MaxLargeSoldiers > -1 && NumLargeSoldiers >= _rules.MaxLargeSoldiers)
			{
				return CraftPlacementErrors.CPE_TooManyLargeSoldiers;
			}
			if (_rules.MaxLargeUnits > -1 && NumLargeUnits >= _rules.MaxLargeUnits)
			{
				return CraftPlacementErrors.CPE_TooManyLargeUnits;
			}
		}
		var allowedSoldierGroups = _rules.AllowedSoldierGroups;
		if (allowedSoldierGroups.Count != 0)
		{
			if (!allowedSoldierGroups.Contains(s.Rules.Group))
			{
				return CraftPlacementErrors.CPE_SoldierGroupNotAllowed;
			}
		}
		if (_rules.IsOnlyOneSoldierGroupAllowed && NumTotalSoldiers > 0)
		{
			int currentGroup = -1;
			foreach (var tmpSoldier in _base.Soldiers)
			{
				if (tmpSoldier.Craft == this)
				{
					currentGroup = tmpSoldier.Rules.Group;
					break;
				}
			}
			if (s.Rules.Group != currentGroup)
			{
				return CraftPlacementErrors.CPE_SoldierGroupNotSame;
			}
		}
		var allowedArmorGroups = _rules.AllowedArmorGroups;
		if (allowedArmorGroups.Count != 0)
		{
			if (!allowedArmorGroups.Contains(s.Armor.Group))
			{
				return CraftPlacementErrors.CPE_ArmorGroupNotAllowed;
			}
		}
		var limitArmorGroups = _rules.LimitArmorGroups;
		if (limitArmorGroups.Count != 0)
		{
			foreach (var limit in limitArmorGroups)
			{
				int subTotal = 0;
				foreach (var tmpSoldier in _base.Soldiers)
				{
					if (tmpSoldier.Craft == this && tmpSoldier.Armor.Group == limit.Key)
					{
						++subTotal;
					}
				}
				if (subTotal >= limit.Value)
				{
					return CraftPlacementErrors.CPE_ArmorGroupNotAllowed;
				}
			}
		}
		return CraftPlacementErrors.CPE_None;
	}

	/// <summary>Validates craft space and craft constraints on adding vehicles to a craft.</summary>
	/// <returns>Maximum allowed number of vehicles to add.</returns>
	public int ValidateAddingVehicles(int totalSize)
	{
		int maximumAllowed = SpaceAvailable / totalSize;

		{
			maximumAllowed = Math.Min(maximumAllowed, MaxVehiclesAndLargeSoldiersClamped - NumVehiclesAndLargeSoldiers);
		}
		if (_rules.MaxVehicles > -1)
		{
			maximumAllowed = Math.Min(maximumAllowed, _rules.MaxVehicles - NumTotalVehicles);
		}
		if (totalSize == 1)
		{
			if (_rules.MaxSmallVehicles > -1)
			{
				maximumAllowed = Math.Min(maximumAllowed, _rules.MaxSmallVehicles - NumSmallVehicles);
			}
			if (_rules.MaxSmallUnits > -1)
			{
				maximumAllowed = Math.Min(maximumAllowed, _rules.MaxSmallUnits - NumSmallUnits);
			}
		}
		else // armorSize > 1
		{
			if (_rules.MaxLargeVehicles > -1)
			{
				maximumAllowed = Math.Min(maximumAllowed, _rules.MaxLargeVehicles - NumLargeVehicles);
			}
			if (_rules.MaxLargeUnits > -1)
			{
				maximumAllowed = Math.Min(maximumAllowed, _rules.MaxLargeUnits - NumLargeUnits);
			}
		}
		return maximumAllowed;
	}
}
