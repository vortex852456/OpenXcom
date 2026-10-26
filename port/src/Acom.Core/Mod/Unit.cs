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
// Ported from: src/Mod/Unit.h, src/Mod/Unit.cpp
#nullable disable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Acom;

public enum SpecialAbility : uint
{
	SPECAB_NONE = 0,
	SPECAB_EXPLODEONDEATH = 1,
	SPECAB_BURNFLOOR = 2,
	SPECAB_BURN_AND_EXPLODE = 3,
}

public enum ForcedTorso : byte
{
	TORSO_USE_GENDER = 0,
	TORSO_ALWAYS_MALE = 1,
	TORSO_ALWAYS_FEMALE = 2,
}

public enum UnitSide : byte
{
	SIDE_FRONT = 0,
	SIDE_LEFT = 1,
	SIDE_RIGHT = 2,
	SIDE_REAR = 3,
	SIDE_UNDER = 4,
	SIDE_MAX = 5,
}

public enum UnitStatus : uint
{
	STATUS_STANDING = 0,
	STATUS_WALKING = 1,
	STATUS_FLYING = 2,
	STATUS_TURNING = 3,
	STATUS_AIMING = 4,
	STATUS_COLLAPSING = 5,
	STATUS_DEAD = 6,
	STATUS_UNCONSCIOUS = 7,
	STATUS_PANICKING = 8,
	STATUS_BERSERK = 9,
	STATUS_IGNORE_ME = 10,
}

/// <summary>
/// Faction naming "absolute": Xcom for FACTION_PLAYER, Aliens for FACTION_HOSTILE, Civilians for FACTION_NEUTRAL.
/// Faction naming "relative": Hostile (Aliens vs Xcom/Civilians), Friendly (same faction), Neutral (Xcom vs Civilians),
/// HostileCivilians (Aliens to Civilians, sometimes handled differently than to Xcom).
/// </summary>
public enum UnitFaction
{
	FACTION_NONE = -1,
	FACTION_PLAYER = 0,
	FACTION_HOSTILE = 1,
	FACTION_NEUTRAL = 2,
	FACTION_MAX = 3,
}

public enum UnitBodyPart
{
	BODYPART_HEAD = 0,
	BODYPART_TORSO = 1,
	BODYPART_RIGHTARM = 2,
	BODYPART_LEFTARM = 3,
	BODYPART_RIGHTLEG = 4,
	BODYPART_LEFTLEG = 5,
	BODYPART_MAX = 6,
}

#pragma warning disable CA1711 // C++ enum name
public enum UnitBodyPartEx : uint
#pragma warning restore CA1711
{
	BODYPART_LEGS = 6,
	BODYPART_COLLAPSING = 7,
	BODYPART_ITEM_RIGHTHAND = 8,
	BODYPART_ITEM_LEFTHAND = 9,
	BODYPART_ITEM_FLOOR = 10,
	BODYPART_ITEM_INVENTORY = 11,
	BODYPART_LARGE_TORSO = 12,
	BODYPART_LARGE_PROPULSION = 16,
	BODYPART_LARGE_TURRET = 20,
}

/// <summary>
/// This struct holds some plain unit attribute data together.
/// Fields are 16 bit like the C++ <c>Sint16</c>; arithmetic truncates the same way.
/// </summary>
/// <remarks>
/// C++ member pointers (<c>UnitStats::Ptr</c>, <c>stats.*p</c>) become the field index <see cref="Ptr"/> and the
/// indexer <c>stats[p]</c>, which returns a reference to the field. The layout is sequential, so the index is
/// the offset from <see cref="tu"/> in fields.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct UnitStats
{
	/// <summary>Max value that is allowed to set to stat, less that max value allowed by type.</summary>
	public const int BaseStatLimit = 8000;

	/// <summary>How much more stun can be than health.</summary>
	public const int StunMultipler = 4;
	/// <summary>Max value allowed for stun value.</summary>
	public const int StunStatLimit = BaseStatLimit * StunMultipler;

	/// <summary>How much more over kill can go to negative than health.</summary>
	public const int OverkillMultipler = 4;

	/// <summary>Number of stat fields.</summary>
	public const int FieldCount = 12;

	// The order must match Ptr.
	public short tu;
	public short stamina;
	public short health;
	public short bravery;
	public short reactions;
	public short firing;
	public short throwing;
	public short strength;
	public short psiStrength;
	public short psiSkill;
	public short melee;
	public short mana;

	/// <summary>
	/// Pointer to one stat field (C++ <c>UnitStats::Ptr</c>, i.e. <c>Sint16 UnitStats::*</c>; <c>&amp;UnitStats::tu</c> is
	/// <c>Ptr.tu</c>). Values are the field positions in declaration order, the order of C++ <c>fieldLoop</c>.
	/// </summary>
#pragma warning disable CA1720 // C++ name UnitStats::Ptr
	public enum Ptr
#pragma warning restore CA1720
	{
		tu, stamina,
		health, bravery,
		reactions, firing,
		throwing, strength,
		psiStrength, psiSkill,
		melee, mana,
	}

	/// <summary>The field <paramref name="p"/> points to (C++ <c>stats.*p</c>).</summary>
	[UnscopedRef]
	public ref short this[Ptr p]
	{
		get
		{
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)p, (uint)FieldCount, nameof(p));
			return ref Unsafe.Add(ref tu, (int)p);
		}
	}

	public UnitStats(int tu_, int stamina_, int health_, int bravery_, int reactions_, int firing_, int throwing_, int strength_, int psiStrength_, int psiSkill_, int melee_, int mana_)
	{
		tu = (short)tu_;
		stamina = (short)stamina_;
		health = (short)health_;
		bravery = (short)bravery_;
		reactions = (short)reactions_;
		firing = (short)firing_;
		throwing = (short)throwing_;
		strength = (short)strength_;
		psiStrength = (short)psiStrength_;
		psiSkill = (short)psiSkill_;
		melee = (short)melee_;
		mana = (short)mana_;
	}

	/// <summary>Field by field sum (also C++ <c>operator+=</c>).</summary>
	public static UnitStats operator +(UnitStats a, UnitStats stats) => new(a.tu + stats.tu, a.stamina + stats.stamina, a.health + stats.health, a.bravery + stats.bravery, a.reactions + stats.reactions, a.firing + stats.firing, a.throwing + stats.throwing, a.strength + stats.strength, a.psiStrength + stats.psiStrength, a.psiSkill + stats.psiSkill, a.melee + stats.melee, a.mana + stats.mana);
	/// <summary>Field by field difference (also C++ <c>operator-=</c>).</summary>
	public static UnitStats operator -(UnitStats a, UnitStats stats) => new(a.tu - stats.tu, a.stamina - stats.stamina, a.health - stats.health, a.bravery - stats.bravery, a.reactions - stats.reactions, a.firing - stats.firing, a.throwing - stats.throwing, a.strength - stats.strength, a.psiStrength - stats.psiStrength, a.psiSkill - stats.psiSkill, a.melee - stats.melee, a.mana - stats.mana);
	public static UnitStats operator -(UnitStats a) => new(-a.tu, -a.stamina, -a.health, -a.bravery, -a.reactions, -a.firing, -a.throwing, -a.strength, -a.psiStrength, -a.psiSkill, -a.melee, -a.mana);

	/// <summary>Takes over every non-zero stat of <paramref name="stats"/>.</summary>
	public void Merge(UnitStats stats)
	{
		tu = (stats.tu != 0 ? stats.tu : tu);
		stamina = (stats.stamina != 0 ? stats.stamina : stamina);
		health = (stats.health != 0 ? stats.health : health);
		bravery = (stats.bravery != 0 ? stats.bravery : bravery);
		reactions = (stats.reactions != 0 ? stats.reactions : reactions);
		firing = (stats.firing != 0 ? stats.firing : firing);
		throwing = (stats.throwing != 0 ? stats.throwing : throwing);
		strength = (stats.strength != 0 ? stats.strength : strength);
		psiStrength = (stats.psiStrength != 0 ? stats.psiStrength : psiStrength);
		psiSkill = (stats.psiSkill != 0 ? stats.psiSkill : psiSkill);
		melee = (stats.melee != 0 ? stats.melee : melee);
		mana = (stats.mana != 0 ? stats.mana : mana);
	}

	/// <summary>Calls <paramref name="f"/> for every field pointer in declaration order (C++ <c>fieldLoop</c>).</summary>
	public static void FieldLoop(Action<Ptr> f)
	{
		ArgumentNullException.ThrowIfNull(f);
		for (Ptr p = 0; p < (Ptr)FieldCount; ++p)
		{
			f(p);
		}
	}

	/// <summary>-1 in <paramref name="fixedStats"/> resets to 0, other non-zero values replace the original.</summary>
	public static UnitStats TemplateMerge(UnitStats origStats, UnitStats fixedStats)
	{
		UnitStats r = default;
		for (Ptr p = 0; p < (Ptr)FieldCount; ++p)
		{
			if (fixedStats[p] == -1)
			{
				r[p] = 0;
			}
			else if (fixedStats[p] != 0)
			{
				r[p] = fixedStats[p];
			}
			else
			{
				r[p] = origStats[p];
			}
		}
		return r;
	}

	/// <summary>
	/// Soft limit definition:
	/// 1. if the statChange is zero or negative, keep statChange as it is (i.e. don't apply any limits)
	/// 2. if the statChange is positive and currentStats &lt;= upperBound, consider upperBound (i.e. set result to min(statChange, upperBound-currentStats))
	/// 3. if the statChange is positive and currentStats &gt; upperBound, keep the currentStats (i.e. set result to 0)
	/// </summary>
	public static UnitStats SoftLimit(UnitStats statChange, UnitStats currentStats, UnitStats upperBound)
	{
		UnitStats r = default;
		for (Ptr p = 0; p < (Ptr)FieldCount; ++p)
		{
			if (statChange[p] <= 0)
			{
				// 1. keep statChange
				r[p] = statChange[p];
			}
			else
			{
				if (currentStats[p] <= upperBound[p])
				{
					// 2. consider upperBound
					short tmp = (short)(upperBound[p] - currentStats[p]);
					r[p] = Math.Min(statChange[p], tmp);
				}
				else
				{
					// 3. keep currentStats
					r[p] = 0;
				}
			}
		}
		return r;
	}

	/// <summary>Takes <paramref name="reroll"/> where <paramref name="mask"/> is non-zero, otherwise <paramref name="keep"/>.</summary>
	public static UnitStats Combine(UnitStats mask, UnitStats keep, UnitStats reroll)
	{
		UnitStats r = default;
		for (Ptr p = 0; p < (Ptr)FieldCount; ++p)
		{
			if (mask[p] != 0)
			{
				r[p] = reroll[p];
			}
			else
			{
				r[p] = keep[p];
			}
		}
		return r;
	}

	/// <summary>Random value between both stats, field by field (one RNG call per differing field, in field order).</summary>
	public static UnitStats Random(UnitStats a, UnitStats b)
	{
		UnitStats r = default;
		for (Ptr p = 0; p < (Ptr)FieldCount; ++p)
		{
			short min = Math.Min(a[p], b[p]);
			short max = Math.Max(a[p], b[p]);
			if (min == max)
			{
				r[p] = max;
			}
			else
			{
				short rnd = (short)Rng.Generate(min, max);
				r[p] = rnd;
			}
		}
		return r;
	}

	/// <summary>1 for every field that <see cref="Random"/> would randomize, else 0.</summary>
	public static UnitStats IsRandom(UnitStats a, UnitStats b)
	{
		UnitStats r = default;
		for (Ptr p = 0; p < (Ptr)FieldCount; ++p)
		{
			if ((a[p] != 0 || b[p] != 0) && a[p] != b[p])
			{
				r[p] = 1;
			}
			else
			{
				r[p] = 0;
			}
		}
		return r;
	}

	/// <summary><c>base * percent * multipler / 100</c> for every field (int math, truncated to 16 bit).</summary>
	public static UnitStats Percent(UnitStats @base, UnitStats percent, int multipler = 1)
	{
		UnitStats r = default;
		for (Ptr p = 0; p < (Ptr)FieldCount; ++p)
		{
			r[p] = (short)(@base[p] * percent[p] * multipler / 100);
		}
		return r;
	}

	/// <summary>All fields set to <paramref name="i"/>.</summary>
	public static UnitStats Scalar(int i)
	{
		UnitStats r = default;
		for (Ptr p = 0; p < (Ptr)FieldCount; ++p)
		{
			r[p] = (short)i;
		}
		return r;
	}

	/// <summary>Minimum 1 for health, minimum 0 for other stats.</summary>
	public static UnitStats ObeyFixedMinimum(UnitStats a)
	{
		// (note to self: it might be worth considering minimum 10 for bravery in the future)
		return Max(a, s_fixedMinimum);
	}

	private static readonly UnitStats s_fixedMinimum = new(0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0);

	public static UnitStats Max(UnitStats a, UnitStats b)
	{
		UnitStats r = default;
		for (Ptr p = 0; p < (Ptr)FieldCount; ++p)
		{
			r[p] = Math.Max(a[p], b[p]);
		}
		return r;
	}

	public static UnitStats Min(UnitStats a, UnitStats b)
	{
		UnitStats r = default;
		for (Ptr p = 0; p < (Ptr)FieldCount; ++p)
		{
			r[p] = Math.Min(a[p], b[p]);
		}
		return r;
	}

	/// <summary>YAML: map with optional keys; missing keys keep the current value (C++ read overload).</summary>
	public static bool YamlRead(YamlNodeReader reader, ref UnitStats val)
	{
		reader.TryRead("tu", ref val.tu);
		reader.TryRead("stamina", ref val.stamina);
		reader.TryRead("health", ref val.health);
		reader.TryRead("bravery", ref val.bravery);
		reader.TryRead("reactions", ref val.reactions);
		reader.TryRead("firing", ref val.firing);
		reader.TryRead("throwing", ref val.throwing);
		reader.TryRead("strength", ref val.strength);
		reader.TryRead("psiStrength", ref val.psiStrength);
		reader.TryRead("psiSkill", ref val.psiSkill);
		reader.TryRead("melee", ref val.melee);
		reader.TryRead("mana", ref val.mana);
		return true;
	}

	/// <summary>YAML: map with all twelve stats (C++ write overload).</summary>
	public static void YamlWrite(YamlNodeWriter writer, UnitStats val)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.SetAsMap();
		writer.Write("tu", val.tu);
		writer.Write("stamina", val.stamina);
		writer.Write("health", val.health);
		writer.Write("bravery", val.bravery);
		writer.Write("reactions", val.reactions);
		writer.Write("firing", val.firing);
		writer.Write("throwing", val.throwing);
		writer.Write("strength", val.strength);
		writer.Write("psiStrength", val.psiStrength);
		writer.Write("psiSkill", val.psiSkill);
		writer.Write("melee", val.melee);
		writer.Write("mana", val.mana);
	}
}

public class StatAdjustment
{
	/// <summary>Name of class used in script.</summary>
	public const string ScriptName = "StatAdjustment";

	public UnitStats statGrowth;
	public UnitStats statGrowthAbs;
	public int growthMultiplier;
	public double aimMultiplier;
	public double armorMultiplier;
	public int armorMultiplierAbs;
}

/// <summary>
/// Represents the static data for a unit that is generated on the battlescape, this includes: HWPs, aliens and civilians.
/// </summary>
public partial class Unit
{
	/// <summary>Name of class used in script.</summary>
	public const string ScriptName = "RuleUnit";

	private readonly string _type;
	private string _civilianRecoveryTypeName = "", _spawnedPersonName = "", _liveAlienName;
	private RuleSoldier _civilianRecoverySoldierType;
	private RuleItem _civilianRecoveryItemType;
	private string _spawnedSoldier = "";
	private string _race = "";
	private int _showFullNameInAlienInventory;
	private string _rank = "";
	private UnitStats _stats;
	private string _armorName = "";
	private Armor _armor;
	private int _standHeight, _kneelHeight, _floatHeight;
	private readonly List<int> _deathSound = [], _panicSound = [], _berserkSound = [], _aggroSound = [];
	private readonly List<int> _selectUnitSound = [], _startMovingSound = [], _selectWeaponSound = [], _annoyedSound = [];
	private int _value, _moraleLossWhenKilled, _moveSound;
	private int _intelligence, _aggression, _spotter, _sniper, _energyRecovery;
	private SpecialAbility _specab;
	private RuleItem _liveAlien;
	private Unit _spawnUnit;
	private string _spawnUnitName = "";
	private bool _livingWeapon;
	private string _meleeWeapon = "", _psiWeapon;
	private List<List<string>> _builtInWeaponsNames = [];
	private readonly List<List<RuleItem>> _builtInWeapons = [];
	private readonly List<WeightedOptions> _weightedBuiltInWeapons = [];
	private bool _capturable;
	private bool _canSurrender, _autoSurrender;
	private bool _isLeeroyJenkins;
	private bool _waitIfOutsideWeaponRange;
	private int _pickUpWeaponsMoreActively;
	private sbyte _avoidsFire;
	private bool _vip;
	private bool _cosmetic, _ignoredByAI;
	private bool _canPanic;
	private bool _canBeMindControlled;
	private int _berserkChance;

	/// <summary>Creates a certain type of unit.</summary>
	public Unit(string type)
	{
		_type = type;
		_liveAlienName = Mod.STR_NULL;
		_showFullNameInAlienInventory = -1;
		_armor = null;
		_standHeight = 0;
		_kneelHeight = 0;
		_floatHeight = 0;
		_value = 0;
		_moraleLossWhenKilled = 100;
		_moveSound = -1;
		_intelligence = 0;
		_aggression = 0;
		_spotter = 0;
		_sniper = 0;
		_energyRecovery = 30;
		_specab = Acom.SpecialAbility.SPECAB_NONE;
		_livingWeapon = false;
		_psiWeapon = "ALIEN_PSI_WEAPON";
		_capturable = true;
		_canSurrender = false;
		_autoSurrender = false;
		_isLeeroyJenkins = false;
		_waitIfOutsideWeaponRange = false;
		_pickUpWeaponsMoreActively = -1;
		_avoidsFire = -1; // defBoolNullable
		_vip = false;
		_cosmetic = false;
		_ignoredByAI = false;
		_canPanic = true;
		_canBeMindControlled = true;
		_berserkChance = -1;
	}

	/// <summary>Loads the unit from a YAML file.</summary>
	public void Load(YamlNodeReader node, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		var reader = node.UseIndex();
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent, mod);
		}

		mod.LoadNameNull(_type, ref _civilianRecoveryTypeName, reader["civilianRecoveryType"]);
		mod.LoadNameNull(_type, ref _spawnedPersonName, reader["spawnedPersonName"]);
		mod.LoadNameNull(_type, ref _liveAlienName, reader["liveAlien"]);
		if (reader["spawnedSoldier"])
		{
			_spawnedSoldier = EmitDescendants(reader["spawnedSoldier"], new YamlRootNodeReader(_spawnedSoldier, "(spawned soldier template)").ToBase());
		}
		reader.TryRead("race", ref _race);
		reader.TryRead("showFullNameInAlienInventory", ref _showFullNameInAlienInventory);

		reader.TryRead("rank", ref _rank);
		_stats.Merge(reader["stats"].ReadVal(_stats));
		mod.LoadName(_type, ref _armorName, reader["armor"]);
		reader.TryRead("standHeight", ref _standHeight);
		reader.TryRead("kneelHeight", ref _kneelHeight);
		reader.TryRead("floatHeight", ref _floatHeight);
		if (_floatHeight + _standHeight > 25)
		{
			throw new InvalidOperationException("Error with unit " + _type + ": Unit height may not exceed 25");
		}
		reader.TryRead("value", ref _value);
		reader.TryRead("moraleLossWhenKilled", ref _moraleLossWhenKilled);
		reader.TryRead("intelligence", ref _intelligence);
		reader.TryRead("aggression", ref _aggression);
		reader.TryRead("spotter", ref _spotter);
		reader.TryRead("sniper", ref _sniper);
		reader.TryRead("energyRecovery", ref _energyRecovery);
		reader.TryRead("specab", ref _specab);
		reader.TryRead("spawnUnit", ref _spawnUnitName);
		reader.TryRead("livingWeapon", ref _livingWeapon);
		reader.TryRead("canSurrender", ref _canSurrender);
		reader.TryRead("autoSurrender", ref _autoSurrender);
		reader.TryRead("isLeeroyJenkins", ref _isLeeroyJenkins);
		reader.TryRead("waitIfOutsideWeaponRange", ref _waitIfOutsideWeaponRange);
		reader.TryRead("pickUpWeaponsMoreActively", ref _pickUpWeaponsMoreActively);
		// loadBoolNullable (Mod/LoadYaml.h)
		var avoidsFire = reader["avoidsFire"];
		if (avoidsFire)
		{
			_avoidsFire = avoidsFire.HasNullVal ? (sbyte)-1 : (sbyte)(avoidsFire.ReadVal<bool>() ? 1 : 0);
		}
		reader.TryRead("meleeWeapon", ref _meleeWeapon);
		reader.TryRead("psiWeapon", ref _psiWeapon);
		reader.TryRead("capturable", ref _capturable);
		reader.TryRead("vip", ref _vip);
		reader.TryRead("cosmetic", ref _cosmetic);
		reader.TryRead("ignoredByAI", ref _ignoredByAI);
		reader.TryRead("canPanic", ref _canPanic);
		reader.TryRead("canBeMindControlled", ref _canBeMindControlled);
		reader.TryRead("berserkChance", ref _berserkChance);
		reader.TryRead("builtInWeaponSets", ref _builtInWeaponsNames);
		if (reader["builtInWeapons"])
		{
			_builtInWeaponsNames.Add(reader["builtInWeapons"].ReadVal<List<string>>());
		}
		foreach (var weights in reader["weightedBuiltInWeaponSets"].Children())
		{
			var nw = new WeightedOptions();
			nw.Load(weights);
			_weightedBuiltInWeapons.Add(nw);
		}

		mod.LoadSoundOffset(_type, _deathSound, reader["deathSound"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _panicSound, reader["panicSound"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _berserkSound, reader["berserkSound"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _aggroSound, reader["aggroSound"], "BATTLE.CAT");

		mod.LoadSoundOffset(_type, _selectUnitSound, reader["selectUnitSound"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _startMovingSound, reader["startMovingSound"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _selectWeaponSound, reader["selectWeaponSound"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _annoyedSound, reader["annoyedSound"], "BATTLE.CAT");

		mod.LoadSoundOffset(_type, ref _moveSound, reader["moveSound"], "BATTLE.CAT");
	}

	/// <summary>Cross link with other rules.</summary>
	public void AfterLoad(Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		mod.LinkRule(ref _armor, ref _armorName);
		mod.LinkRule(ref _spawnUnit, ref _spawnUnitName);
		mod.LinkRule(_builtInWeapons, _builtInWeaponsNames);
		if (_liveAlienName == Mod.STR_NULL)
		{
			_liveAlien = mod.GetItem(_type, false); // this is optional default behavior
		}
		else
		{
			mod.LinkRule(ref _liveAlien, ref _liveAlienName);
		}

		if (Mod.IsEmptyRuleName(_civilianRecoveryTypeName) == false)
		{
			if (!IsRecoverableAsEngineer && !IsRecoverableAsScientist)
			{
				_civilianRecoverySoldierType = mod.GetSoldier(_civilianRecoveryTypeName, false);
				if (_civilianRecoverySoldierType != null)
				{
					_civilianRecoveryTypeName = "";
				}
				else
				{
					mod.LinkRule(ref _civilianRecoveryItemType, ref _civilianRecoveryTypeName);
				}
			}
			Debug.Assert(IsRecoverableAsCivilian, "Check missing some cases");
		}

		mod.CheckForSoftError(_armor == null, _type, "Unit is missing armor", SeverityLevel.LOG_ERROR);
		if (_armor != null)
		{
			if (_capturable && _armor.CorpseBattlescape[0].IsRecoverable && _spawnUnit == null)
			{
				mod.CheckForSoftError(
					_liveAlien == null && !IsRecoverableAsCivilian,
					_type,
					"This unit can be recovered (in theory), but there is no corresponding 'liveAlien:' or 'civilianRecoveryType:' to recover.",
					SeverityLevel.LOG_INFO
				);
			}
			else
			{
				string s =
					!_capturable ? "the unit is marked with 'capturable: false'" :
					!_armor.CorpseBattlescape[0].IsRecoverable ? "the first 'corpseBattle' item of the unit's armor is marked with 'recover: false'" :
					_spawnUnit != null ? "the unit will be converted into another unit type on stun/kill/capture" :
					"???";

				mod.CheckForSoftError(
					_liveAlien != null
					&& _liveAlien.VehicleUnit == null
					&& _spawnUnit == null, // if unit is `_capturable` we can still get live species even if it can spawn unit
					_type,
					"This unit has a corresponding item to recover, but still isn't recoverable. Reason: (" + s + "). Consider marking the unit with 'liveAlien: \"\"'.",
					SeverityLevel.LOG_INFO
				);
			}
		}
	}

	/// <summary>
	/// Serializes the node's descendants, plus the top-level keys of <paramref name="defaultValuesReader"/> that the node
	/// lacks (C++ <c>YamlNodeReader::emitDescendants(defaultValuesReader)</c>, used for spawned soldier templates).
	/// </summary>
	internal static string EmitDescendants(YamlNodeReader node, YamlNodeReader defaultValuesReader)
	{
		YamlWriteNode root;
		if (node.IsMap)
			root = new YamlWriteNode { Kind = YamlWriteNode.NodeKind.Map };
		else if (node.IsSeq)
			root = new YamlWriteNode { Kind = YamlWriteNode.NodeKind.Seq };
		else
			return string.Empty;
		foreach (var child in node.Node.Children)
			root.Children.Add(YamlEmitter.FromReader(child));
		if (defaultValuesReader)
		{
			foreach (var child in defaultValuesReader.Node.Children)
			{
				if (!root.Children.Exists(c => c.Key == child.Key))
					root.Children.Add(YamlEmitter.FromReader(child));
			}
		}
		return YamlEmitter.Emit(root);
	}

	/// <summary>Gets the unit's type (also its language string).</summary>
	public string Type { get => _type; }

	/// <summary>Gets if unit can be recovered as civilian.</summary>
	public bool IsRecoverableAsCivilian { get => !string.IsNullOrEmpty(_civilianRecoveryTypeName) || _civilianRecoverySoldierType != null || _civilianRecoveryItemType != null; }
	/// <summary>Gets if engineer is recovered when a civilian is saved.</summary>
	public bool IsRecoverableAsEngineer { get => _civilianRecoveryTypeName == "STR_ENGINEER"; }
	/// <summary>Gets if scientist is recovered when a civilian is saved.</summary>
	public bool IsRecoverableAsScientist { get => _civilianRecoveryTypeName == "STR_SCIENTIST"; }
	/// <summary>Gets soldier type that is recovered when a civilian is saved.</summary>
	public RuleSoldier CivilianRecoverySoldierType { get => _civilianRecoverySoldierType; }
	/// <summary>Gets item type that is recovered when a civilian is saved.</summary>
	public RuleItem CivilianRecoveryItemType { get => _civilianRecoveryItemType; }
	/// <summary>Gets the custom name of the "spawned person".</summary>
	public string SpawnedPersonName { get => _spawnedPersonName; }
	/// <summary>Gets the spawned soldier template.</summary>
	public string SpawnedSoldierTemplate { get => _spawnedSoldier; }

	/// <summary>Gets the unit's stats.</summary>
	public UnitStats Stats { get => _stats; }
	/// <summary>Gets the unit's height when standing.</summary>
	public int StandHeight { get => _standHeight; }
	/// <summary>Gets the unit's height when kneeling.</summary>
	public int KneelHeight { get => _kneelHeight; }
	/// <summary>Gets the unit's float elevation.</summary>
	public int FloatHeight { get => _floatHeight; }
	/// <summary>Gets the armor type.</summary>
	public Armor Armor { get => _armor; }
	/// <summary>Gets the alien race type.</summary>
	public string Race { get => _race; }
	/// <summary>Gets the alien rank.</summary>
	public string Rank { get => _rank; }
	/// <summary>Gets the value - for score calculation.</summary>
	public int Value { get => _value; }
	/// <summary>Percentage modifier for morale loss when this unit is killed.</summary>
	public int MoraleLossWhenKilled { get => _moraleLossWhenKilled; }
	/// <summary>Gets the death sound ids.</summary>
	public List<int> DeathSounds { get => _deathSound; }
	/// <summary>Gets the unit's panic sounds.</summary>
	public List<int> PanicSounds { get => _panicSound; }
	/// <summary>Gets the unit's berserk sounds.</summary>
	public List<int> BerserkSounds { get => _berserkSound; }
	/// <summary>Gets the unit's "select unit" sounds.</summary>
	public List<int> SelectUnitSounds { get => _selectUnitSound; }
	/// <summary>Gets the unit's "start moving" sounds.</summary>
	public List<int> StartMovingSounds { get => _startMovingSound; }
	/// <summary>Gets the unit's "select weapon" sounds.</summary>
	public List<int> SelectWeaponSounds { get => _selectWeaponSound; }
	/// <summary>Gets the unit's "annoyed" sounds.</summary>
	public List<int> AnnoyedSounds { get => _annoyedSound; }
	/// <summary>Gets the move sound id.</summary>
	public int MoveSound { get => _moveSound; }
	/// <summary>Gets the intelligence. This is the number of turns AI remembers your troop positions.</summary>
	public int Intelligence { get => _intelligence; }
	/// <summary>Gets the aggression. Determines the chance of revenge and taking cover.</summary>
	public int Aggression { get => _aggression; }
	/// <summary>Gets the spotter score. This is the number of turns sniper AI units can use spotting info from this unit.</summary>
	public int SpotterDuration
	{
		// Lazy balance - use -1 to make this the same as intelligence value
		get => (_spotter == -1) ? _intelligence : _spotter;
	}
	/// <summary>Gets the sniper score. Determines chance of acting on information gained by spotter units.</summary>
	public int SniperPercentage { get => _sniper; }
	/// <summary>Gets the alien's special ability.</summary>
	public int SpecialAbility { get => (int)_specab; }
	/// <summary>Gets the Geoscape live alien item.</summary>
	public RuleItem LiveAlienGeoscape { get => _liveAlien; }
	/// <summary>Gets the unit that is spawned when this one dies.</summary>
	public Unit SpawnUnit { get => _spawnUnit; }
	/// <summary>Gets the unit's warcries.</summary>
	public List<int> AggroSounds { get => _aggroSound; }
	/// <summary>Gets how much energy this unit recovers per turn.</summary>
	public int EnergyRecovery { get => _energyRecovery; }
	/// <summary>
	/// Checks if this unit is a living weapon. A living weapon ignores any loadout that may be available to
	/// its rank and uses the one associated with its race.
	/// </summary>
	public bool IsLivingWeapon { get => _livingWeapon; }
	/// <summary>Gets the name of any melee weapon that may be built in to this unit.</summary>
	public string MeleeWeapon { get => _meleeWeapon; }
	/// <summary>Gets the name of any psi weapon that may be built in to this unit.</summary>
	public string PsiWeapon { get => _psiWeapon; }
	/// <summary>
	/// Gets the integrated items this unit has available. Unlike "livingWeapon" this is used in ADDITION to
	/// any loadout or living weapon item that may be defined.
	/// </summary>
	public List<List<RuleItem>> BuiltInWeapons { get => _builtInWeapons; }
	/// <summary>Gets the integrated item options this unit has available.</summary>
	public List<WeightedOptions> WeightedBuiltInWeapons { get => _weightedBuiltInWeapons; }
	/// <summary>Gets whether the alien can be captured alive.</summary>
	public bool Capturable { get => _capturable; }
	/// <summary>Checks if this unit can surrender.</summary>
	public bool CanSurrender() => _canSurrender || _autoSurrender;
	/// <summary>Checks if this unit surrenders automatically, if all other units surrendered too.</summary>
	public bool AutoSurrender() => _autoSurrender;
	public bool IsLeeroyJenkins { get => _isLeeroyJenkins; }
	/// <summary>Should the unit get "stuck" trying to fire from outside of weapon range? Vanilla bug, that may serve as "feature" in rare cases.</summary>
	public bool WaitIfOutsideWeaponRange() => _waitIfOutsideWeaponRange;
	/// <summary>Should the unit try to pick up weapons more actively?</summary>
	public int PickUpWeaponsMoreActively { get => _pickUpWeaponsMoreActively; }
	/// <summary>Is the unit afraid to pathfind through fire?</summary>
	public bool AvoidsFire() => _avoidsFire == -1 ? _specab < Acom.SpecialAbility.SPECAB_BURNFLOOR : _avoidsFire != 0; // useBoolNullable

	/// <summary>Should alien inventory show full name (e.g. Sectoid Leader) or just the race (e.g. Sectoid)?</summary>
	public bool GetShowFullNameInAlienInventory(Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		if (_showFullNameInAlienInventory != -1)
		{
			return _showFullNameInAlienInventory != 0;
		}
		return mod.ShowFullNameInAlienInventory;
	}

	/// <summary>Is this a VIP unit?</summary>
	public bool IsVIP { get => _vip; }
	/// <summary>Is this only a cosmetic unit?</summary>
	public bool IsCosmetic { get => _cosmetic; }
	/// <summary>Should this unit be ignored by the AI?</summary>
	public bool IsIgnoredByAI { get => _ignoredByAI; }
	/// <summary>Checks if this unit can panic.</summary>
	public bool CanPanic() => _canPanic;
	/// <summary>Checks if this unit can be mind controlled.</summary>
	public bool CanBeMindControlled() => _canBeMindControlled;
	/// <summary>Gets the probability of unit berserking when panicked.</summary>
	public int BerserkChance { get => _berserkChance; }
}
