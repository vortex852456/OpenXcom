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
// Ported from: src/Mod/RuleItem.h, src/Mod/RuleItem.cpp
#nullable disable

namespace Acom;

public enum BattleType : uint
{
	BT_NONE = 0,
	BT_FIREARM = 1,
	BT_AMMO = 2,
	BT_MELEE = 3,
	BT_GRENADE = 4,
	BT_PROXIMITYGRENADE = 5,
	BT_MEDIKIT = 6,
	BT_SCANNER = 7,
	BT_MINDPROBE = 8,
	BT_PSIAMP = 9,
	BT_FLARE = 10,
	BT_CORPSE = 11,
}

public enum BattleFuseType
{
	BFT_NONE = -3,
	BFT_INSTANT = -2,
	BFT_SET = -1,
	BFT_FIX_MIN = 0,
	BFT_FIX_MAX = 64,
}

public enum BattleMediKitType : uint
{
	BMT_NORMAL = 0,
	BMT_HEAL = 1,
	BMT_STIMULANT = 2,
	BMT_PAINKILLER = 3,
}

public enum BattleMediKitAction : uint
{
	BMA_HEAL = 1,
	BMA_STIMULANT = 2,
	BMA_PAINKILLER = 4,
}

public enum ExperienceTrainingMode : uint
{
	ETM_DEFAULT = 0,
	ETM_MELEE_100 = 1,
	ETM_MELEE_50 = 2,
	ETM_MELEE_33 = 3,
	ETM_FIRING_100 = 4,
	ETM_FIRING_50 = 5,
	ETM_FIRING_33 = 6,
	ETM_THROWING_100 = 7,
	ETM_THROWING_50 = 8,
	ETM_THROWING_33 = 9,
	ETM_FIRING_AND_THROWING = 10,
	ETM_FIRING_OR_THROWING = 11,
	ETM_REACTIONS = 12,
	ETM_REACTIONS_AND_MELEE = 13,
	ETM_REACTIONS_AND_FIRING = 14,
	ETM_REACTIONS_AND_THROWING = 15,
	ETM_REACTIONS_OR_MELEE = 16,
	ETM_REACTIONS_OR_FIRING = 17,
	ETM_REACTIONS_OR_THROWING = 18,
	ETM_BRAVERY = 19,
	ETM_BRAVERY_2X = 20,
	ETM_BRAVERY_AND_REACTIONS = 21,
	ETM_BRAVERY_OR_REACTIONS = 22,
	ETM_BRAVERY_OR_REACTIONS_2X = 23,
	ETM_PSI_STRENGTH = 24,
	ETM_PSI_STRENGTH_2X = 25,
	ETM_PSI_SKILL = 26,
	ETM_PSI_SKILL_2X = 27,
	ETM_PSI_STRENGTH_AND_SKILL = 28,
	ETM_PSI_STRENGTH_AND_SKILL_2X = 29,
	ETM_PSI_STRENGTH_OR_SKILL = 30,
	ETM_PSI_STRENGTH_OR_SKILL_2X = 31,
	ETM_NOTHING = 32,
}

public enum BattleActionType : byte
{
	BA_NONE = 0,
	BA_TURN = 1,
	BA_WALK = 2,
	BA_KNEEL = 3,
	BA_PRIME = 4,
	BA_UNPRIME = 5,
	BA_THROW = 6,
	BA_AUTOSHOT = 7,
	BA_SNAPSHOT = 8,
	BA_AIMEDSHOT = 9,
	BA_HIT = 10,
	BA_USE = 11,
	BA_LAUNCH = 12,
	BA_MINDCONTROL = 13,
	BA_PANIC = 14,
	BA_RETHINK = 15,
	BA_CQB = 16,
	BA_TRIGGER_TIMED_GRENADE = 17,
	BA_TRIGGER_PROXY_GRENADE = 18,
	BA_SELF_DESTRUCT = 19,
}

public enum BattleActionOrigin
{
	CENTRE = 0,
	LEFT = 1,
	RIGHT = 2,
}

/// <summary>
/// Common base of the item use costs and flags (C++ template RuleItemUseRuleBase&lt;T&gt;).
/// <c>NullableValue&lt;T&gt;</c> of the C++ code is <c>T?</c> here (see the YAML layer).
/// </summary>
public class RuleItemUseRuleBase<T>
{
	public T Time;
	public T Energy;
	public T Morale;
	public T Health;
	public T Stun;
	public T Mana;

	/// <summary>Default constructor.</summary>
	public RuleItemUseRuleBase()
	{
	}

	/// <summary>Create new cost with one value for time units and another for rest.</summary>
	public RuleItemUseRuleBase(T tu, T rest)
	{
		Time = tu;
		Energy = rest;
		Morale = rest;
		Health = rest;
		Stun = rest;
		Mana = rest;
	}

	/// <summary>Copy constructor.</summary>
	public RuleItemUseRuleBase(RuleItemUseRuleBase<T> b)
	{
		Time = b.Time;
		Energy = b.Energy;
		Morale = b.Morale;
		Health = b.Health;
		Stun = b.Stun;
		Mana = b.Mana;
	}

	public void Load(YamlNodeReader reader)
	{
		reader.TryRead("time", ref Time);
		reader.TryRead("energy", ref Energy);
		reader.TryRead("morale", ref Morale);
		reader.TryRead("health", ref Health);
		reader.TryRead("stun", ref Stun);
		reader.TryRead("mana", ref Mana);
	}
}

/// <summary>Cost of an item action (a C++ value type: copy with the copy constructor).</summary>
public class RuleItemUseCost : RuleItemUseRuleBase<short>
{
	/// <summary>Default constructor.</summary>
	public RuleItemUseCost() : base(0, 0)
	{
	}

	/// <summary>Create new cost with one value for time units and another for rest.</summary>
	public RuleItemUseCost(short tu, short rest = 0) : base(tu, rest)
	{
	}

	/// <summary>Copy constructor.</summary>
	public RuleItemUseCost(RuleItemUseRuleBase<short> b) : base(b)
	{
	}

	/// <summary>Add cost (Sint16 arithmetic wraps like in C++).</summary>
	public void operator +=(RuleItemUseCost cost)
	{
		Time = unchecked((short)(Time + cost.Time));
		Energy = unchecked((short)(Energy + cost.Energy));
		Morale = unchecked((short)(Morale + cost.Morale));
		Health = unchecked((short)(Health + cost.Health));
		Stun = unchecked((short)(Stun + cost.Stun));
		Mana = unchecked((short)(Mana + cost.Mana));
	}
}

/// <summary>Flat (true) or percent (false) cost of an item action (a C++ value type: copy with the copy constructor).</summary>
public class RuleItemUseFlat : RuleItemUseRuleBase<bool>
{
	/// <summary>Default constructor.</summary>
	public RuleItemUseFlat() : base(false, false)
	{
	}

	/// <summary>Create new cost with one value for time units and another for rest.</summary>
	public RuleItemUseFlat(bool tu, bool rest = false) : base(tu, rest)
	{
	}

	/// <summary>Copy constructor.</summary>
	public RuleItemUseFlat(RuleItemUseRuleBase<bool> b) : base(b)
	{
	}
}

/// <summary>Cost of an item action as defined in the ruleset; null values fall back to other rules.</summary>
public class RuleItemUseCostRule : RuleItemUseRuleBase<short?>
{
	/// <summary>Default constructor.</summary>
	public RuleItemUseCostRule() : base(0, 0)
	{
	}

	/// <summary>Create new cost with one value for time units and another for rest.</summary>
	public RuleItemUseCostRule(short? tu, short? rest = 0) : base(tu, rest)
	{
	}

	/// <summary>Get final value of cost (C++ free function <c>getDefault(a, b...)</c>): each field from the first rule that defines it.</summary>
	/// <param name="b">Fallback rules, in order.</param>
	public RuleItemUseCost GetDefault(params ReadOnlySpan<RuleItemUseCostRule> b)
	{
		var n = new RuleItemUseCost();
		n.Time = RuleItemUse.Coalesce(Time, b, static r => r.Time);
		n.Energy = RuleItemUse.Coalesce(Energy, b, static r => r.Energy);
		n.Morale = RuleItemUse.Coalesce(Morale, b, static r => r.Morale);
		n.Health = RuleItemUse.Coalesce(Health, b, static r => r.Health);
		n.Stun = RuleItemUse.Coalesce(Stun, b, static r => r.Stun);
		n.Mana = RuleItemUse.Coalesce(Mana, b, static r => r.Mana);
		return n;
	}

	/// <summary>Load use cost.</summary>
	/// <param name="reader">YAML node.</param>
	/// <param name="name">Name of action type.</param>
	public void LoadCost(YamlNodeReader reader, string name)
	{
		reader.TryRead("tu" + name, ref Time);
		if (reader["cost" + name] is { IsValid: true } cost)
		{
			Load(cost);
		}
	}
}

/// <summary>Cost type (flat or percent) of an item action as defined in the ruleset; null values fall back to other rules.</summary>
public class RuleItemUseFlatRule : RuleItemUseRuleBase<bool?>
{
	/// <summary>Default constructor.</summary>
	public RuleItemUseFlatRule() : base(false, false)
	{
	}

	/// <summary>Create new cost with one value for time units and another for rest.</summary>
	public RuleItemUseFlatRule(bool? tu, bool? rest = false) : base(tu, rest)
	{
	}

	/// <summary>Get final value of cost type (C++ free function <c>getDefault(a, b...)</c>): each field from the first rule that defines it.</summary>
	/// <param name="b">Fallback rules, in order.</param>
	public RuleItemUseFlat GetDefault(params ReadOnlySpan<RuleItemUseFlatRule> b)
	{
		var n = new RuleItemUseFlat();
		n.Time = RuleItemUse.Coalesce(Time, b, static r => r.Time);
		n.Energy = RuleItemUse.Coalesce(Energy, b, static r => r.Energy);
		n.Morale = RuleItemUse.Coalesce(Morale, b, static r => r.Morale);
		n.Health = RuleItemUse.Coalesce(Health, b, static r => r.Health);
		n.Stun = RuleItemUse.Coalesce(Stun, b, static r => r.Stun);
		n.Mana = RuleItemUse.Coalesce(Mana, b, static r => r.Mana);
		return n;
	}

	/// <summary>Load use cost type (flat or percent).</summary>
	/// <param name="reader">YAML node.</param>
	/// <param name="name">Name of action type.</param>
	public void LoadFlat(YamlNodeReader reader, string name)
	{
		if (reader["flat" + name] is { IsValid: true } cost)
		{
			if (cost.HasVal)
			{
				cost.TryReadVal(ref Time);
			}
			else
			{
				Load(cost);
			}
		}
	}
}

/// <summary>Helper of the C++ free function <c>getDefault</c> (see <see cref="RuleItemUseCostRule.GetDefault"/>).</summary>
internal static class RuleItemUse
{
	/// <summary>coalesceNullValues: first non-null value, or the default value if all are null.</summary>
	public static T Coalesce<T, R>(T? first, ReadOnlySpan<R> rules, Func<R, T?> field) where T : struct
	{
		if (first is { } f)
			return f;
		foreach (var r in rules)
		{
			if (field(r) is { } v)
				return v;
		}
		return default;
	}
}

/// <summary>Common configuration of item action.</summary>
public class RuleItemAction
{
	public int accuracy;
	public int range;
	public int shots = 1;
	public int spendPerShot = 1;
	public bool followProjectiles = true;
	public int ammoSlot;
	public int ammoZombieUnitChanceOverride = -1;
	public int ammoSpawnUnitChanceOverride = -1;
	public int ammoSpawnItemChanceOverride = -1;
	public RuleItemUseCostRule cost = new();
	public RuleItemUseFlatRule flat = new();
	/// <summary>Only overrides arcing: false on a weapon for a specific action.</summary>
	public bool arcing;
	public string name = "";
	public string shortName = "";
}

/// <summary>Config for fuse triggers.</summary>
public struct RuleItemFuseTrigger
{
	public bool defaultBehavior = true;
	public bool throwTrigger;
	public bool throwExplode;
	public bool proximityTrigger;
	public bool proximityExplode;

	public RuleItemFuseTrigger()
	{
	}
}

/// <summary>
/// Helper struct that has all the data needed for weapon attack (a C++ value type).
/// The C++ read-only view <c>BattleActionAttack::ReadOnly</c> is this class itself in the port.
/// </summary>
public class BattleActionAttack
{
	public BattleActionType type;
	public BattleUnit attacker;
	public BattleItem weapon_item;
	public BattleItem damage_item;
	public RuleSkill skill_rules;

	public BattleActionAttack()
	{
	}

	/// <summary>Aggregate initialization like C++ <c>BattleActionAttack{ type, attacker, weapon_item, ... }</c>.</summary>
	public BattleActionAttack(BattleActionType type, BattleUnit attacker = null, BattleItem weapon_item = null, BattleItem damage_item = null, RuleSkill skill_rules = null)
	{
		this.type = type;
		this.attacker = attacker;
		this.weapon_item = weapon_item;
		this.damage_item = damage_item;
		this.skill_rules = skill_rules;
	}

	/// <summary>Update <c>attacker</c> from <c>weapon_item</c>.</summary>
	private static void UpdateAttacker(BattleActionAttack attack)
	{
		if (attack.weapon_item != null && attack.attacker == null)
		{
			BattleType battleType = attack.weapon_item.Rules.BattleType;
			if (battleType == BattleType.BT_PROXIMITYGRENADE || battleType == BattleType.BT_GRENADE)
			{
				BattleUnit owner = attack.weapon_item.PreviousOwner;
				if (owner != null)
				{
					attack.attacker = owner;
				}
			}
		}
	}

	/// <summary>Update <c>damage_item</c> from <c>weapon_item</c>.</summary>
	private static void UpdateAmmo(BattleActionAttack attack)
	{
		if (attack.weapon_item != null && attack.damage_item == null)
		{
			BattleType battleType = attack.weapon_item.Rules.BattleType;
			if (battleType == BattleType.BT_PROXIMITYGRENADE || battleType == BattleType.BT_GRENADE || battleType == BattleType.BT_PSIAMP)
			{
				attack.damage_item = attack.weapon_item;
			}
			else
			{
				attack.damage_item = attack.weapon_item.GetAmmoForAction(attack.type);
			}
		}
	}

	/// <summary>Update grenade <c>damage_item</c> from <c>weapon_item</c>.</summary>
	private static void UpdateGrenade(BattleActionAttack attack)
	{
		if (attack.weapon_item != null && attack.damage_item == null)
		{
			BattleType battleType = attack.weapon_item.Rules.BattleType;
			if (battleType == BattleType.BT_PROXIMITYGRENADE || battleType == BattleType.BT_GRENADE)
			{
				attack.damage_item = attack.weapon_item;
			}
		}
	}

	/// <summary>Generate ActionAttack before shooting, this means we can get the ammo from the weapon.</summary>
	/// <param name="action">BattleCost of attack.</param>
	/// <returns>All attack action data.</returns>
	public static BattleActionAttack GetBeforeShoot(BattleActionCost action)
	{
		return GetBeforeShoot(action.type, action.actor, action.weapon, action.skillRules);
	}

	/// <summary>Get Action Attack with updated unit and ammo.</summary>
	public static BattleActionAttack GetBeforeShoot(BattleActionType type, BattleUnit unit, BattleItem wepon, RuleSkill skill = null)
	{
		var attack = new BattleActionAttack(type, unit, wepon);
		UpdateAttacker(attack);
		UpdateAmmo(attack);
		attack.skill_rules = skill;
		return attack;
	}

	/// <summary>Generate ActionAttack after shooting, the ammo can be already spent and unloaded from the weapon.</summary>
	/// <param name="action">BattleCost of attack.</param>
	/// <param name="ammo">Ammo used to shoot/attack.</param>
	/// <returns>All attack action data.</returns>
	public static BattleActionAttack GetAferShoot(BattleActionCost action, BattleItem ammo)
	{
		return GetAferShoot(action.type, action.actor, action.weapon, ammo, action.skillRules);
	}

	/// <summary>Get Action Attack with updated unit with defined ammo.</summary>
	public static BattleActionAttack GetAferShoot(BattleActionType type, BattleUnit unit, BattleItem wepon, BattleItem ammo, RuleSkill skill = null)
	{
		var attack = new BattleActionAttack(type, unit, wepon);
		UpdateAttacker(attack);
		attack.damage_item = ammo;
		attack.skill_rules = skill;
		UpdateGrenade(attack);
		return attack;
	}
}

/// <summary>
/// Represents a specific type of item.
/// Contains constant info about an item like
/// storage size, sell price, etc.
/// </summary>
public partial class RuleItem
{
	/// <summary>Maximum number of ammo slots on weapon.</summary>
	public const int AmmoSlotMax = 4;
	/// <summary>Special ammo slot that represent usage of weapon itself as ammo.</summary>
	public const int AmmoSlotSelfUse = -1;
	public const int MedikitSlots = 3;
	/// <summary>Name of class used in script.</summary>
	public const string ScriptName = "RuleItem";

	private const float VexelsToTiles = 0.0625f;
	private const float TilesToVexels = 16.0f;

	private string _ufopediaType = "";
	private readonly string _type;
	private string _name, _nameAsAmmo = ""; // two types of objects can have the same name
	private string _requiresBuyCountry = "";
	private readonly List<string> _requiresName = [];
	private readonly List<string> _requiresBuyName = [];
	private List<RuleResearch> _requires = [], _requiresBuy = [];
	private RuleBaseFacilityFunctions _requiresBuyBaseFunc;
	private readonly SortedDictionary<string, int> _recoveryDividers = new(StringComparer.Ordinal);
	private SortedDictionary<string, List<int>> _recoveryTransformationsName = new(StringComparer.Ordinal);
	private readonly Dictionary<RuleItem, List<int>> _recoveryTransformations = [];
	private readonly List<string> _categories = [];

	private Unit _vehicleUnit;
	private int _vehicleFixedAmmoSlot;
	private double _size;
	private int _monthlyBuyLimit;
	private string _monthlyBuyLimitMessage = "";
	private int _costBuy, _costSell, _transferTime, _weight;
	private int _throwRange, _underwaterThrowRange;
	private int _throwDropoffRange, _underwaterThrowDropoffRange, _throwDropoff;
	private int _bigSprite;
	private int _floorSprite;
	private int _handSprite, _bulletSprite;
	private int _specialIconSprite;
	private readonly List<int> _reloadSound = [], _primeSound = [], _unprimeSound = [];
	private readonly List<int> _fireSound = [], _hitSound = [];
	private int _hitAnimation, _hitAnimFrames;
	private readonly List<int> _hitMissSound = [];
	private int _hitMissAnimation, _hitMissAnimFrames;
	private readonly List<int> _meleeSound = [];
	private int _meleeAnimation, _meleeAnimFrames;
	private readonly List<int> _meleeMissSound = [];
	private int _meleeMissAnimation, _meleeMissAnimFrames;
	private readonly List<int> _meleeHitSound = [], _explosionHitSound = [], _psiSound = [];
	private int _psiAnimation, _psiAnimFrames;
	private readonly List<int> _psiMissSound = [];
	private int _psiMissAnimation, _psiMissAnimFrames;
	private int _power, _powerForAnimation;
	private bool _hidePower;
	private bool _ignoreAmmoPower;
	private float _powerRangeReduction;
	private float _powerRangeThreshold;
	private readonly List<List<string>> _compatibleAmmoNames = [.. Enumerable.Range(0, AmmoSlotMax).Select(_ => new List<string>())];
	private readonly List<RuleItem>[] _compatibleAmmo = [.. Enumerable.Range(0, AmmoSlotMax).Select(_ => new List<RuleItem>())];
	private readonly Dictionary<RuleItem, int> _compatibleAmmoSlots = [];
	private RuleDamageType _damageType, _meleeType;
	private bool _damageTypeSet, _meleeTypeSet;
	private readonly RuleItemAction _confAimed = new(), _confAuto = new(), _confSnap = new(), _confMelee = new();
	private int _accuracyUse, _accuracyMind, _accuracyPanic, _accuracyThrow, _accuracyCloseQuarters;
	private int _noLOSAccuracyPenalty;
	private int _explodeInventory;
	private readonly RuleItemUseCostRule _costUse, _costMind, _costPanic, _costThrow, _costPrime, _costUnprime;
	private int _clipSize, _specialChance;
	private readonly int[] _tuLoad = new int[AmmoSlotMax], _tuUnload = new int[AmmoSlotMax];
	private BattleType _battleType;
	private BattleFuseType _fuseType;
	private RuleItemFuseTrigger _fuseTriggerEvents;
	private bool _hiddenOnMinimap;
	private string _painKillerActionName, _stimulantActionName, _healActionName;
	private string _medikitActionName, _psiAttackName, _primeActionName, _unprimeActionName, _primeActionMessage, _unprimeActionMessage;
	private string _sellActionMessage = "";

	private bool _twoHanded, _blockBothHands, _fixedWeapon, _fixedWeaponShow, _isConsumable, _isFireExtinguisher;
	private bool _isAmmoRechargeable;
	private bool _specialUseEmptyHand, _specialUseEmptyHandShow;
	private int _inventoryMoveCostPercent = 100;
	private string _defaultInventorySlotName = "";
	private RuleInventory _defaultInventorySlot;
	private int _defaultInvSlotX, _defaultInvSlotY;
	private readonly List<string> _supportedInventorySectionsNames = [];
	private readonly List<RuleInventory> _supportedInventorySections = [];
	private int _waypoints, _invWidth, _invHeight;

	private int _painKiller, _heal, _stimulant;
	private BattleMediKitType _medikitType;
	private bool _medikitTargetSelf, _medikitTargetImmune;
	private int _medikitTargetMatrix;
	private string _medikitBackground = "";
	private int _woundRecovery, _healthRecovery, _stunRecovery, _energyRecovery, _manaRecovery, _moraleRecovery;
	private float _painKillerRecovery;

	private int _recoveryPoints;
	private int _armor;
	private int _turretType;
	private int _aiUseDelay, _aiMeleeHitCount;
	private bool _recover, _recoverCorpse, _ignoreInBaseDefense, _ignoreInCraftEquip, _liveAlien;
	private int _liveAlienPrisonType;
	private int _attraction;
	private readonly RuleItemUseFlatRule _flatUse, _flatThrow, _flatPrime, _flatUnprime;
	private bool _arcingShot;
	private ExperienceTrainingMode _experienceTrainingMode;
	private int _manaExperience;
	private int _loadOrder;
	private int _listOrder, _maxRange, _minRange, _dropoff, _bulletSpeed, _explosionSpeed, _shotgunPellets;
	private int _shotgunBehaviorType, _shotgunSpread, _shotgunChoke;

	private readonly SortedDictionary<string, string> _zombieUnitByArmorMale = new(StringComparer.Ordinal), _zombieUnitByArmorFemale = new(StringComparer.Ordinal), _zombieUnitByType = new(StringComparer.Ordinal);
	private string _zombieUnit = "", _spawnUnitName = "", _spawnItemName = "";
	private Unit _spawnUnit;
	private RuleItem _spawnItem;
	private UnitFaction _spawnUnitFaction;
	private UnitFaction _zombieUnitFaction;
	private int _spawnUnitChance = -1;
	private int _zombieUnitChance = -1;
	private int _spawnItemChance = -1;

	private int _targetMatrix;
	private bool _convertToCivilian;
	private bool _LOSRequired, _underwaterOnly, _landOnly, _psiReqiured, _manaRequired;
	private int _meleePower, _specialType, _vaporColor, _vaporDensity, _vaporProbability;
	private int _vaporColorSurface, _vaporDensitySurface, _vaporProbabilitySurface;
	private readonly List<int> _customItemPreviewIndex = [];
	private int _kneelBonus, _oneHandedPenalty;
	private int _monthlySalary, _monthlyMaintenance;
	private int _sprayWaypoints;
	private readonly RuleStatBonus _damageBonus, _meleeBonus, _accuracyMulti, _meleeMulti, _throwMulti, _closeQuartersMulti;

	/// <summary>Creates a blank ruleset for a certain type of item.</summary>
	/// <param name="type">String defining the type.</param>
	/// <param name="listOrder">The list weight for this item.</param>
	public RuleItem(string type, int listOrder)
	{
		_type = type;
		_name = type;
		_vehicleUnit = null;
		_vehicleFixedAmmoSlot = 0;
		_size = 0.0;
		_monthlyBuyLimit = 0;
		_costBuy = 0;
		_costSell = 0;
		_transferTime = 24;
		_weight = 3;
		_throwRange = 200;
		_underwaterThrowRange = 200;
		_throwDropoffRange = 99;
		_underwaterThrowDropoffRange = 99;
		_throwDropoff = 5;
		_bigSprite = -1;
		_floorSprite = -1;
		_handSprite = 120;
		_bulletSprite = -1;
		_specialIconSprite = -1;
		_hitAnimation = 0;
		_hitAnimFrames = -1;
		_hitMissAnimation = -1;
		_hitMissAnimFrames = -1;
		_meleeAnimation = 0;
		_meleeAnimFrames = -1;
		_meleeMissAnimation = -1;
		_meleeMissAnimFrames = -1;
		_psiAnimation = -1;
		_psiAnimFrames = -1;
		_psiMissAnimation = -1;
		_psiMissAnimFrames = -1;
		_power = 0;
		_powerForAnimation = 0;
		_hidePower = false;
		_ignoreAmmoPower = false;
		_powerRangeReduction = 0;
		_powerRangeThreshold = 0;
		_damageType = new RuleDamageType();
		_meleeType = new RuleDamageType();
		_damageTypeSet = false;
		_meleeTypeSet = false;
		_accuracyUse = 0;
		_accuracyMind = 0;
		_accuracyPanic = 20;
		_accuracyThrow = 100;
		_accuracyCloseQuarters = -1;
		_noLOSAccuracyPenalty = -1;
		_explodeInventory = -1;
		_costUse = new RuleItemUseCostRule(25);
		_costMind = new RuleItemUseCostRule(null, null);
		_costPanic = new RuleItemUseCostRule(null, null);
		_costThrow = new RuleItemUseCostRule(25);
		_costPrime = new RuleItemUseCostRule(50);
		_costUnprime = new RuleItemUseCostRule(25);
		_clipSize = 0;
		_specialChance = 100;
		_battleType = BattleType.BT_NONE;
		_fuseType = BattleFuseType.BFT_NONE;
		_fuseTriggerEvents = new RuleItemFuseTrigger();
		_hiddenOnMinimap = false;
		_painKillerActionName = "STR_PAIN_KILLER";
		_stimulantActionName = "STR_STIMULANT";
		_healActionName = "STR_HEAL";
		_medikitActionName = "STR_USE_MEDI_KIT";
		_psiAttackName = "";
		_primeActionName = "STR_PRIME_GRENADE";
		_unprimeActionName = "";
		_primeActionMessage = "STR_GRENADE_IS_ACTIVATED";
		_unprimeActionMessage = "STR_GRENADE_IS_DEACTIVATED";
		_twoHanded = false;
		_blockBothHands = false;
		_fixedWeapon = false;
		_fixedWeaponShow = false;
		_isConsumable = false;
		_isFireExtinguisher = false;
		_isAmmoRechargeable = false;
		_specialUseEmptyHand = false;
		_specialUseEmptyHandShow = false;
		_defaultInvSlotX = 0;
		_defaultInvSlotY = 0;
		_waypoints = 0;
		_invWidth = 1;
		_invHeight = 1;
		_painKiller = 0;
		_heal = 0;
		_stimulant = 0;
		_medikitType = BattleMediKitType.BMT_NORMAL;
		_medikitTargetSelf = false;
		_medikitTargetImmune = false;
		_medikitTargetMatrix = 63;
		_woundRecovery = 0;
		_healthRecovery = 0;
		_stunRecovery = 0;
		_energyRecovery = 0;
		_manaRecovery = 0;
		_moraleRecovery = 0;
		_painKillerRecovery = 1.0f;
		_recoveryPoints = 0;
		_armor = 20;
		_turretType = -1;
		_aiUseDelay = -1;
		_aiMeleeHitCount = 25;
		_recover = true;
		_recoverCorpse = true;
		_ignoreInBaseDefense = false;
		_ignoreInCraftEquip = true;
		_liveAlien = false;
		_liveAlienPrisonType = 0;
		_attraction = 0;
		_flatUse = new RuleItemUseFlatRule(false, true);
		_flatThrow = new RuleItemUseFlatRule(false, true);
		_flatPrime = new RuleItemUseFlatRule(false, true);
		_flatUnprime = new RuleItemUseFlatRule(false, true);
		_arcingShot = false;
		_experienceTrainingMode = ExperienceTrainingMode.ETM_DEFAULT;
		_manaExperience = 0;
		_loadOrder = 0;
		_listOrder = listOrder;
		_maxRange = 200;
		_minRange = 0;
		_dropoff = 2;
		_bulletSpeed = 0;
		_explosionSpeed = 0;
		_shotgunPellets = 0;
		_shotgunBehaviorType = 0;
		_shotgunSpread = 100;
		_shotgunChoke = 100;
		_spawnUnitFaction = UnitFaction.FACTION_NONE;
		_zombieUnitFaction = UnitFaction.FACTION_HOSTILE;
		_targetMatrix = 7;
		_convertToCivilian = false;
		_LOSRequired = false;
		_underwaterOnly = false;
		_landOnly = false;
		_psiReqiured = false;
		_manaRequired = false;
		_meleePower = 0;
		_specialType = -1;
		_vaporColor = -1;
		_vaporDensity = 0;
		_vaporProbability = 15;
		_vaporColorSurface = -1;
		_vaporDensitySurface = 0;
		_vaporProbabilitySurface = 15;
		_kneelBonus = -1;
		_oneHandedPenalty = -1;
		_monthlySalary = 0;
		_monthlyMaintenance = 0;
		_sprayWaypoints = 0;
		_damageBonus = new RuleStatBonus();
		_meleeBonus = new RuleStatBonus();
		_accuracyMulti = new RuleStatBonus();
		_meleeMulti = new RuleStatBonus();
		_throwMulti = new RuleStatBonus();
		_closeQuartersMulti = new RuleStatBonus();

		_accuracyMulti.SetFiring();
		_meleeMulti.SetMelee();
		_throwMulti.SetThrowing();
		_closeQuartersMulti.SetCloseQuarters();

		for (int i = 0; i < AmmoSlotMax; ++i)
		{
			_tuLoad[i] = 15;
		}
		for (int i = 0; i < AmmoSlotMax; ++i)
		{
			_tuUnload[i] = 8;
		}

		_confAimed.range = 200;
		_confSnap.range = 15;
		_confAuto.range = 7;

		_confAimed.cost = new RuleItemUseCostRule(0);
		_confSnap.cost = new RuleItemUseCostRule(0, null);
		_confAuto.cost = new RuleItemUseCostRule(0, null);
		_confMelee.cost = new RuleItemUseCostRule(0);

		_confAimed.flat = new RuleItemUseFlatRule(null, null);
		_confSnap.flat = new RuleItemUseFlatRule(null, null);
		_confAuto.flat = new RuleItemUseFlatRule(null, null);
		_confMelee.flat = new RuleItemUseFlatRule(null, null);

		_confAimed.name = "STR_AIMED_SHOT";
		_confSnap.name = "STR_SNAP_SHOT";
		_confAuto.name = "STR_AUTO_SHOT";

		_confAuto.shots = 3;

		_customItemPreviewIndex.Add(Mod.NO_SURFACE);
	}

	/// <summary>Load ammo slot with checking correct range.</summary>
	public static void LoadAmmoSlotChecked(ref int result, YamlNodeReader reader, string parentName)
	{
		if (reader)
		{
			int s = reader.ReadVal(result);
			if (s < AmmoSlotSelfUse || s >= AmmoSlotMax)
			{
				Logger.Error("ammoSlot outside of allowed range in '" + parentName + "'");
			}
			else
			{
				result = s;
			}
		}
	}

	/// <summary>Load RuleItemAction from yaml.</summary>
	/// <param name="a">Item use config.</param>
	/// <param name="reader">YAML node.</param>
	/// <param name="name">Name of action type.</param>
	private void LoadConfAction(RuleItemAction a, YamlNodeReader reader, string name)
	{
		if (reader["conf" + name] is { IsValid: true } conf)
		{
			conf.TryRead("shots", ref a.shots);
			conf.TryRead("spendPerShot", ref a.spendPerShot);
			conf.TryRead("followProjectiles", ref a.followProjectiles);
			conf.TryRead("name", ref a.name);
			conf.TryRead("shortName", ref a.shortName);
			LoadAmmoSlotChecked(ref a.ammoSlot, conf["ammoSlot"], _name);
			LoadYaml.LoadIntNullable(ref a.ammoZombieUnitChanceOverride, conf["ammoZombieUnitChanceOverride"]);
			LoadYaml.LoadIntNullable(ref a.ammoSpawnUnitChanceOverride, conf["ammoSpawnUnitChanceOverride"]);
			LoadYaml.LoadIntNullable(ref a.ammoSpawnItemChanceOverride, conf["ammoSpawnItemChanceOverride"]);
			conf.TryRead("arcing", ref a.arcing);
		}
	}

	/// <summary>Load RuleItemFuseTrigger from yaml.</summary>
	private static void LoadConfFuse(ref RuleItemFuseTrigger a, YamlNodeReader reader, string name)
	{
		if (reader[name] is { IsValid: true } conf)
		{
			LoadYaml.LoadBool(ref a.defaultBehavior, conf["defaultBehavior"]);
			LoadYaml.LoadBool(ref a.throwTrigger, conf["throwTrigger"]);
			LoadYaml.LoadBool(ref a.throwExplode, conf["throwExplode"]);
			LoadYaml.LoadBool(ref a.proximityTrigger, conf["proximityTrigger"]);
			LoadYaml.LoadBool(ref a.proximityExplode, conf["proximityExplode"]);
		}
	}

	/// <summary>Updates item categories based on replacement rules.</summary>
	/// <param name="replacementRules">The list replacement rules.</param>
	public void UpdateCategories(SortedDictionary<string, string> replacementRules)
	{
		foreach (var pair in replacementRules)
		{
			// std::replace
			for (int i = 0; i < _categories.Count; ++i)
			{
				if (_categories[i] == pair.Key)
					_categories[i] = pair.Value;
			}
		}
	}

	/// <summary>Loads the item from a YAML file (the C++ ModScript parser argument comes from <see cref="Mod.ScriptParsers"/>).</summary>
	/// <param name="node">YAML node.</param>
	/// <param name="mod">Mod for the item.</param>
	public void Load(YamlNodeReader node, Mod mod)
	{
		var reader = node.UseIndex();
		if (reader["refNode"] is { IsValid: true } parent)
		{
			Load(parent, mod);
		}

		reader.TryRead("ufopediaType", ref _ufopediaType);
		reader.TryRead("name", ref _name);
		reader.TryRead("nameAsAmmo", ref _nameAsAmmo);

		//requires
		reader.TryRead("requiresBuyCountry", ref _requiresBuyCountry);
		mod.LoadUnorderedNames(_type, _requiresName, reader["requires"]);
		mod.LoadUnorderedNames(_type, _requiresBuyName, reader["requiresBuy"]);
		mod.LoadBaseFunction(_type, ref _requiresBuyBaseFunc, reader["requiresBuyBaseFunc"]);

		mod.LoadUnorderedNamesToInt(_type, _recoveryDividers, reader["recoveryDividers"]);
		reader.TryRead("recoveryTransformations", ref _recoveryTransformationsName);
		mod.LoadUnorderedNames(_type, _categories, reader["categories"]);

		reader.TryRead("vehicleFixedAmmoSlot", ref _vehicleFixedAmmoSlot);
		reader.TryRead("size", ref _size);
		reader.TryRead("monthlyBuyLimit", ref _monthlyBuyLimit);
		reader.TryRead("monthlyBuyLimitMessage", ref _monthlyBuyLimitMessage);
		reader.TryRead("costBuy", ref _costBuy);
		reader.TryRead("costSell", ref _costSell);
		reader.TryRead("transferTime", ref _transferTime);
		reader.TryRead("weight", ref _weight);
		reader.TryRead("throwRange", ref _throwRange);
		reader.TryRead("underwaterThrowRange", ref _underwaterThrowRange);
		reader.TryRead("throwDropoffRange", ref _throwDropoffRange);
		reader.TryRead("underwaterThrowDropoffRange", ref _underwaterThrowDropoffRange);
		reader.TryRead("throwDropoff", ref _throwDropoff);

		mod.LoadSpriteOffset(_type, ref _bigSprite, reader["bigSprite"], "BIGOBS.PCK");
		mod.LoadSpriteOffset(_type, ref _floorSprite, reader["floorSprite"], "FLOOROB.PCK");
		mod.LoadSpriteOffset(_type, ref _handSprite, reader["handSprite"], "HANDOB.PCK");
		// Projectiles: 0-384 entries ((105*33) / (3*3)) (35 sprites per projectile(0-34), 11 projectiles (0-10))
		mod.LoadSpriteOffset(_type, ref _bulletSprite, reader["bulletSprite"], "Projectiles", 35);
		mod.LoadSpriteOffset(_type, ref _specialIconSprite, reader["specialIconSprite"], "SPICONS.DAT");

		mod.LoadSoundOffset(_type, _reloadSound, reader["reloadSound"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _primeSound, reader["primeSound"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _unprimeSound, reader["unprimeSound"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _fireSound, reader["fireSound"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _hitSound, reader["hitSound"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _hitMissSound, reader["hitMissSound"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _meleeSound, reader["meleeSound"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _meleeHitSound, reader["meleeHitSound"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _meleeMissSound, reader["meleeMissSound"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _psiSound, reader["psiSound"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _psiMissSound, reader["psiMissSound"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _explosionHitSound, reader["explosionHitSound"], "BATTLE.CAT");

		mod.LoadSpriteOffset(_type, ref _hitAnimation, reader["hitAnimation"], "SMOKE.PCK");
		mod.LoadSpriteOffset(_type, ref _hitMissAnimation, reader["hitMissAnimation"], "SMOKE.PCK");
		mod.LoadSpriteOffset(_type, ref _meleeAnimation, reader["meleeAnimation"], "HIT.PCK");
		mod.LoadSpriteOffset(_type, ref _meleeMissAnimation, reader["meleeMissAnimation"], "HIT.PCK");
		mod.LoadSpriteOffset(_type, ref _psiAnimation, reader["psiAnimation"], "HIT.PCK");
		mod.LoadSpriteOffset(_type, ref _psiMissAnimation, reader["psiMissAnimation"], "HIT.PCK");

		reader.TryRead("hitAnimFrames", ref _hitAnimFrames);
		reader.TryRead("hitMissAnimFrames", ref _hitMissAnimFrames);
		reader.TryRead("meleeAnimFrames", ref _meleeAnimFrames);
		reader.TryRead("meleeMissAnimFrames", ref _meleeMissAnimFrames);
		reader.TryRead("psiAnimFrames", ref _psiAnimFrames);
		reader.TryRead("psiMissAnimFrames", ref _psiMissAnimFrames);

		if (reader["battleType"])
		{
			reader.TryRead("battleType", ref _battleType);
			_ignoreInCraftEquip = !IsUsefulBattlescapeItem;

			if (_battleType == BattleType.BT_PSIAMP)
			{
				_psiReqiured = true;
				_dropoff = 1;
				_confAimed.range = 0;
				_accuracyMulti.SetPsiAttack();
				_targetMatrix = 6; // only hostile and neutral by default
			}
			else
			{
				_psiReqiured = false;
			}

			if (_battleType == BattleType.BT_PROXIMITYGRENADE)
			{
				_fuseType = BattleFuseType.BFT_INSTANT;
			}
			else if (_battleType == BattleType.BT_GRENADE)
			{
				_fuseType = BattleFuseType.BFT_SET;
			}
			else
			{
				_fuseType = BattleFuseType.BFT_NONE;
			}

			if (_battleType == BattleType.BT_MELEE)
			{
				_confMelee.ammoSlot = 0;
			}
			else
			{
				_confMelee.ammoSlot = AmmoSlotSelfUse;
			}

			if (_battleType == BattleType.BT_CORPSE)
			{
				//compatibility hack for corpse explosion, that didn't have defined damage type
				_damageType = mod.GetDamageType(ItemDamageType.DT_HE).Clone();
				_damageTypeSet = true;
			}
			_meleeType = mod.GetDamageType(ItemDamageType.DT_MELEE).Clone();
			_meleeTypeSet = true;
		}

		if (reader["damageType"] is { IsValid: true } damageType)
		{
			//load predefined damage type
			_damageType = mod.GetDamageType((ItemDamageType)damageType.ReadVal<int>()).Clone();
			_damageTypeSet = true;
		}
		reader.TryRead("blastRadius", ref _damageType.FixRadius);
		if (reader["damageAlter"] is { IsValid: true } damageAlter)
		{
			_damageType.Load(damageAlter);
		}

		if (reader["meleeType"] is { IsValid: true } meleeType)
		{
			//load predefined damage type
			_meleeType = mod.GetDamageType((ItemDamageType)meleeType.ReadVal<int>()).Clone();
			_meleeTypeSet = true;
		}
		if (reader["meleeAlter"] is { IsValid: true } meleeAlter)
		{
			_meleeType.Load(meleeAlter);
		}

		if (reader["skillApplied"] is { IsValid: true } skill)
		{
			if (skill.ReadVal(false))
			{
				_meleeMulti.SetMelee();
			}
			else
			{
				_meleeMulti.SetFlatHundred();
				_meleeMulti.SetModded(true); // vanilla default = true
			}
		}
		if (reader["strengthApplied"].ReadVal(false))
		{
			_damageBonus.SetStrength();
			_damageBonus.SetModded(true); // vanilla default = false
		}

		reader.TryRead("power", ref _power);
		reader.TryRead("powerForAnimation", ref _powerForAnimation);
		reader.TryRead("hidePower", ref _hidePower);
		reader.TryRead("ignoreAmmoPower", ref _ignoreAmmoPower);
		reader.TryRead("painKillerActionName", ref _painKillerActionName);
		reader.TryRead("stimulantActionName", ref _stimulantActionName);
		reader.TryRead("healActionName", ref _healActionName);
		reader.TryRead("medikitActionName", ref _medikitActionName);
		reader.TryRead("psiAttackName", ref _psiAttackName);
		reader.TryRead("primeActionName", ref _primeActionName);
		reader.TryRead("primeActionMessage", ref _primeActionMessage);
		reader.TryRead("unprimeActionName", ref _unprimeActionName);
		reader.TryRead("unprimeActionMessage", ref _unprimeActionMessage);
		reader.TryRead("sellActionMessage", ref _sellActionMessage);
		reader.TryRead("fuseType", ref _fuseType);
		reader.TryRead("hiddenOnMinimap", ref _hiddenOnMinimap);
		reader.TryRead("clipSize", ref _clipSize);

		LoadConfFuse(ref _fuseTriggerEvents, reader, "fuseTriggerEvents");

		reader.TryRead("accuracyAimed", ref _confAimed.accuracy);
		reader.TryRead("accuracyAuto", ref _confAuto.accuracy);
		reader.TryRead("accuracySnap", ref _confSnap.accuracy);
		reader.TryRead("accuracyMelee", ref _confMelee.accuracy);
		reader.TryRead("accuracyUse", ref _accuracyUse);
		reader.TryRead("accuracyMindControl", ref _accuracyMind);
		reader.TryRead("accuracyPanic", ref _accuracyPanic);
		reader.TryRead("accuracyThrow", ref _accuracyThrow);
		reader.TryRead("accuracyCloseQuarters", ref _accuracyCloseQuarters);
		reader.TryRead("noLOSAccuracyPenalty", ref _noLOSAccuracyPenalty);
		if (reader["isExplodingInHands"])
		{
			// FIXME: backwards-compatibility only, remove in 2026
			bool tmpBool = false;
			reader.TryRead("isExplodingInHands", ref tmpBool);
			_explodeInventory = tmpBool ? 2 : 0;
		}
		reader.TryRead("explodeInventory", ref _explodeInventory);

		_confAimed.cost.LoadCost(reader, "Aimed");
		_confAuto.cost.LoadCost(reader, "Auto");
		_confSnap.cost.LoadCost(reader, "Snap");
		_confMelee.cost.LoadCost(reader, "Melee");
		_costUse.LoadCost(reader, "Use");
		_costMind.LoadCost(reader, "MindControl");
		_costPanic.LoadCost(reader, "Panic");
		_costThrow.LoadCost(reader, "Throw");
		_costPrime.LoadCost(reader, "Prime");
		_costUnprime.LoadCost(reader, "Unprime");

		reader.TryRead("flatRate", ref _flatUse.Time);

		_confAimed.flat.LoadFlat(reader, "Aimed");
		_confAuto.flat.LoadFlat(reader, "Auto");
		_confSnap.flat.LoadFlat(reader, "Snap");
		_confMelee.flat.LoadFlat(reader, "Melee");
		_flatUse.LoadFlat(reader, "Use");
		_flatThrow.LoadFlat(reader, "Throw");
		_flatPrime.LoadFlat(reader, "Prime");
		_flatUnprime.LoadFlat(reader, "Unprime");

		LoadConfAction(_confAimed, reader, "Aimed");
		LoadConfAction(_confAuto, reader, "Auto");
		LoadConfAction(_confSnap, reader, "Snap");
		LoadConfAction(_confMelee, reader, "Melee");

		void LoadAmmoConf(int offset, YamlNodeReader n)
		{
			if (n)
			{
				mod.LoadUnorderedNames(_type, _compatibleAmmoNames[offset], n["compatibleAmmo"]);
				n.TryRead("tuLoad", ref _tuLoad[offset]);
				n.TryRead("tuUnload", ref _tuUnload[offset]);
			}
		}

		LoadAmmoConf(0, reader);
		if (reader["ammo"] is { IsValid: true } nodeAmmo)
		{
			for (int slot = 0; slot < AmmoSlotMax; ++slot)
			{
				LoadAmmoConf(slot, nodeAmmo[slot.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
			}
		}

		reader.TryRead("specialChance", ref _specialChance);
		reader.TryRead("twoHanded", ref _twoHanded);
		reader.TryRead("blockBothHands", ref _blockBothHands);
		reader.TryRead("waypoints", ref _waypoints);
		reader.TryRead("fixedWeapon", ref _fixedWeapon);
		reader.TryRead("fixedWeaponShow", ref _fixedWeaponShow);
		reader["inventoryMoveCost"]["basePercent"].TryReadVal(ref _inventoryMoveCostPercent);
		mod.LoadNameNull(_type, ref _defaultInventorySlotName, reader["defaultInventorySlot"]);
		reader.TryRead("defaultInvSlotX", ref _defaultInvSlotX);
		reader.TryRead("defaultInvSlotY", ref _defaultInvSlotY);
		mod.LoadUnorderedNames(_type, _supportedInventorySectionsNames, reader["supportedInventorySections"]);
		reader.TryRead("isConsumable", ref _isConsumable);
		reader.TryRead("isFireExtinguisher", ref _isFireExtinguisher);
		reader.TryRead("isAmmoRechargeable", ref _isAmmoRechargeable);
		reader.TryRead("specialUseEmptyHand", ref _specialUseEmptyHand);
		reader.TryRead("specialUseEmptyHandShow", ref _specialUseEmptyHandShow);
		reader.TryRead("invWidth", ref _invWidth);
		reader.TryRead("invHeight", ref _invHeight);

		reader.TryRead("painKiller", ref _painKiller);
		reader.TryRead("heal", ref _heal);
		reader.TryRead("stimulant", ref _stimulant);
		reader.TryRead("woundRecovery", ref _woundRecovery);
		reader.TryRead("healthRecovery", ref _healthRecovery);
		reader.TryRead("stunRecovery", ref _stunRecovery);
		reader.TryRead("energyRecovery", ref _energyRecovery);
		reader.TryRead("manaRecovery", ref _manaRecovery);
		reader.TryRead("moraleRecovery", ref _moraleRecovery);
		reader.TryRead("painKillerRecovery", ref _painKillerRecovery);
		reader.TryRead("medikitType", ref _medikitType);
		reader.TryRead("medikitTargetSelf", ref _medikitTargetSelf);
		reader.TryRead("medikitTargetImmune", ref _medikitTargetImmune);
		reader.TryRead("medikitTargetMatrix", ref _medikitTargetMatrix);
		reader.TryRead("medikitBackground", ref _medikitBackground);

		reader.TryRead("recoveryPoints", ref _recoveryPoints);
		reader.TryRead("armor", ref _armor);
		reader.TryRead("turretType", ref _turretType);
		if (reader["ai"] is { IsValid: true } nodeAI)
		{
			nodeAI.TryRead("useDelay", ref _aiUseDelay);
			nodeAI.TryRead("meleeHitCount", ref _aiMeleeHitCount);
		}
		reader.TryRead("recover", ref _recover);
		reader.TryRead("recoverCorpse", ref _recoverCorpse);
		reader.TryRead("ignoreInBaseDefense", ref _ignoreInBaseDefense);
		reader.TryRead("ignoreInCraftEquip", ref _ignoreInCraftEquip);
		reader.TryRead("liveAlien", ref _liveAlien);
		reader.TryRead("prisonType", ref _liveAlienPrisonType);
		reader.TryRead("attraction", ref _attraction);
		reader.TryRead("arcingShot", ref _arcingShot);
		reader.TryRead("experienceTrainingMode", ref _experienceTrainingMode);
		reader.TryRead("manaExperience", ref _manaExperience);
		reader.TryRead("loadOrder", ref _loadOrder);
		reader.TryRead("listOrder", ref _listOrder);
		reader.TryRead("maxRange", ref _maxRange);
		reader.TryRead("aimRange", ref _confAimed.range);
		reader.TryRead("autoRange", ref _confAuto.range);
		reader.TryRead("snapRange", ref _confSnap.range);
		reader.TryRead("minRange", ref _minRange);
		reader.TryRead("dropoff", ref _dropoff);
		reader.TryRead("bulletSpeed", ref _bulletSpeed);
		reader.TryRead("explosionSpeed", ref _explosionSpeed);
		reader.TryRead("autoShots", ref _confAuto.shots);
		reader.TryRead("shotgunPellets", ref _shotgunPellets);
		reader.TryRead("shotgunBehavior", ref _shotgunBehaviorType);
		reader.TryRead("shotgunSpread", ref _shotgunSpread);
		reader.TryRead("shotgunChoke", ref _shotgunChoke);

		mod.LoadUnorderedNamesToNames(_type, _zombieUnitByArmorMale, reader["zombieUnitByArmorMale"]);
		mod.LoadUnorderedNamesToNames(_type, _zombieUnitByArmorFemale, reader["zombieUnitByArmorFemale"]);
		mod.LoadUnorderedNamesToNames(_type, _zombieUnitByType, reader["zombieUnitByType"]);
		mod.LoadNameNull(_type, ref _zombieUnit, reader["zombieUnit"]);
		mod.LoadNameNull(_type, ref _spawnUnitName, reader["spawnUnit"]);
		mod.LoadNameNull(_type, ref _spawnItemName, reader["spawnItem"]);
		reader.TryRead("spawnUnitFaction", ref _spawnUnitFaction);
		reader.TryRead("zombieUnitFaction", ref _zombieUnitFaction);
		LoadYaml.LoadIntNullable(ref _spawnUnitChance, reader["spawnUnitChance"]);
		LoadYaml.LoadIntNullable(ref _zombieUnitChance, reader["zombieUnitChance"]);
		LoadYaml.LoadIntNullable(ref _spawnItemChance, reader["spawnItemChance"]);

		// TODO: just backwards-compatibility, remove in 2022, update ruleset validator too
		reader.TryRead("psiTargetMatrix", ref _targetMatrix);

		reader.TryRead("targetMatrix", ref _targetMatrix);
		reader.TryRead("convertToCivilian", ref _convertToCivilian);
		reader.TryRead("LOSRequired", ref _LOSRequired);
		reader.TryRead("meleePower", ref _meleePower);
		reader.TryRead("underwaterOnly", ref _underwaterOnly);
		reader.TryRead("landOnly", ref _landOnly);
		reader.TryRead("specialType", ref _specialType);

		mod.LoadTransparencyOffset(_type, ref _vaporColor, reader["vaporColor"]);
		reader.TryRead("vaporDensity", ref _vaporDensity);
		reader.TryRead("vaporProbability", ref _vaporProbability);

		mod.LoadTransparencyOffset(_type, ref _vaporColorSurface, reader["vaporColorSurface"]);
		reader.TryRead("vaporDensitySurface", ref _vaporDensitySurface);
		reader.TryRead("vaporProbabilitySurface", ref _vaporProbabilitySurface);

		mod.LoadSpriteOffset(_type, _customItemPreviewIndex, reader["customItemPreviewIndex"], "CustomItemPreviews");
		reader.TryRead("kneelBonus", ref _kneelBonus);
		reader.TryRead("oneHandedPenalty", ref _oneHandedPenalty);
		reader.TryRead("monthlySalary", ref _monthlySalary);
		reader.TryRead("monthlyMaintenance", ref _monthlyMaintenance);
		reader.TryRead("sprayWaypoints", ref _sprayWaypoints);

		var parsers = mod.ScriptParsers;
		_damageBonus.Load(_type, reader, parsers.bonusStatsScripts.Get(ModScript.DamageBonusStatBonus));
		_meleeBonus.Load(_type, reader, parsers.bonusStatsScripts.Get(ModScript.MeleeBonusStatBonus));
		_accuracyMulti.Load(_type, reader, parsers.bonusStatsScripts.Get(ModScript.AccuracyMultiplierStatBonus));
		_meleeMulti.Load(_type, reader, parsers.bonusStatsScripts.Get(ModScript.MeleeMultiplierStatBonus));
		_throwMulti.Load(_type, reader, parsers.bonusStatsScripts.Get(ModScript.ThrowMultiplierStatBonus));
		_closeQuartersMulti.Load(_type, reader, parsers.bonusStatsScripts.Get(ModScript.CloseQuarterMultiplierStatBonus));

		reader.TryRead("powerRangeReduction", ref _powerRangeReduction);
		reader.TryRead("powerRangeThreshold", ref _powerRangeThreshold);

		reader.TryRead("psiRequired", ref _psiReqiured);
		reader.TryRead("manaRequired", ref _manaRequired);
		LoadScripts(reader, parsers);
	}

	/// <summary>Cross link with other Rules.</summary>
	public void AfterLoad(Mod mod)
	{
		if ((_battleType == BattleType.BT_MELEE || _battleType == BattleType.BT_FIREARM) && _clipSize == 0)
		{
			foreach (RuleItemAction conf in (RuleItemAction[])[_confAimed, _confAuto, _confSnap, _confMelee])
			{
				if (conf.ammoSlot != AmmoSlotSelfUse && _compatibleAmmoNames[conf.ammoSlot].Count == 0)
				{
					throw new InvalidOperationException("Weapon " + _type + " has clip size 0 and no ammo defined. Please use 'clipSize: -1' for unlimited ammo, or allocate a compatibleAmmo item.");
				}
			}
		}

		// these are good defaults for vanilla
		if (_loadOrder <= 0)
		{
			_loadOrder = _listOrder;
		}

		mod.VerifySpriteOffset(_type, _bigSprite, "BIGOBS.PCK");
		mod.VerifySpriteOffset(_type, _floorSprite, "FLOOROB.PCK");
		mod.VerifySpriteOffset(_type, _handSprite, "HANDOB.PCK");
		// Projectiles: this will check only if first one is correct
		if (!IsWaterOnly) mod.VerifySpriteOffset(_type, _bulletSprite, "Projectiles");
		if (IsWaterOnly) mod.VerifySpriteOffset(_type, _bulletSprite, "UnderwaterProjectiles");
		mod.VerifySpriteOffset(_type, _specialIconSprite, "SPICONS.DAT");

		mod.VerifySoundOffset(_type, _reloadSound, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _fireSound, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _hitSound, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _hitMissSound, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _meleeSound, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _meleeHitSound, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _meleeMissSound, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _psiSound, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _psiMissSound, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _explosionHitSound, "BATTLE.CAT");

		mod.VerifySpriteOffset(_type, _hitAnimation, _damageType.FixRadius != 0 ? "X1.PCK" : "SMOKE.PCK");
		mod.VerifySpriteOffset(_type, _hitMissAnimation, _damageType.FixRadius != 0 ? "X1.PCK" : "SMOKE.PCK");
		mod.VerifySpriteOffset(_type, _meleeAnimation, "HIT.PCK");
		mod.VerifySpriteOffset(_type, _meleeMissAnimation, "HIT.PCK");
		mod.VerifySpriteOffset(_type, _psiAnimation, "HIT.PCK");
		mod.VerifySpriteOffset(_type, _psiMissAnimation, "HIT.PCK");

		mod.VerifySpriteOffset(_type, _customItemPreviewIndex, "CustomItemPreviews");

		_requires = mod.GetResearch(_requiresName);
		_requiresBuy = mod.GetResearch(_requiresBuyName);
		// fixedWeapons can mean vehicle
		if (_fixedWeapon)
		{
			_vehicleUnit = mod.GetUnit(_type);
		}
		mod.LinkRule(ref _spawnUnit, ref _spawnUnitName);
		mod.LinkRule(ref _spawnItem, ref _spawnItemName);

		foreach (var pair in _recoveryTransformationsName)
		{
			var item = mod.GetItem(pair.Key, true);
			if (!item.IsAlien)
			{
				if (pair.Value.Count != 0)
				{
					_recoveryTransformations[item] = pair.Value;
				}
				else
				{
					throw new InvalidOperationException("Right-hand value of recovery transformations definition cannot be empty!");
				}
			}
			else
			{
				throw new InvalidOperationException("Sorry modders, cannot recover live aliens from random inorganic junk '" + pair.Key + "'!");
			}
		}

		mod.LinkRule(ref _defaultInventorySlot, ref _defaultInventorySlotName);
		if (_supportedInventorySectionsNames.Count != 0)
		{
			mod.LinkRule(_supportedInventorySections, _supportedInventorySectionsNames);
			// Deviation: C++ sorts the list by pointer value (Collections::sortVector) for a binary search; the port keeps
			// the ruleset order (pointer order is arbitrary) and CanBePlacedIntoInventorySection uses a linear search.
		}
		for (int i = 0; i < AmmoSlotMax; ++i)
		{
			mod.LinkRule(_compatibleAmmo[i], _compatibleAmmoNames[i]);
			foreach (var a in _compatibleAmmo[i])
			{
				_compatibleAmmoSlots.TryAdd(a, i);
			}
		}

		if (_vehicleUnit != null)
		{
			if (_vehicleFixedAmmoSlot != 0 && _vehicleFixedAmmoSlot != -1)
			{
				throw new InvalidOperationException("Vehicle primary weapon fixed ammo slot can be only 0 or -1.");
			}
			if (_vehicleFixedAmmoSlot > -1)
			{
				if (_compatibleAmmo[_vehicleFixedAmmoSlot].Count > 1)
				{
					throw new InvalidOperationException("Vehicle weapons support only one ammo type");
				}
				if (_compatibleAmmo[_vehicleFixedAmmoSlot].Count == 1)
				{
					var ammo = _compatibleAmmo[_vehicleFixedAmmoSlot][0];
					if (ammo.ClipSize > 0 && ClipSize > 0)
					{
						if (ClipSize % ammo.ClipSize != 0)
						{
							throw new InvalidOperationException("Vehicle weapon clip size is not a multiple of '" + ammo.Type + "' clip size");
						}
					}
				}
			}
		}

		//remove not needed data
		_requiresName.Clear();
		_requiresBuyName.Clear();
		_recoveryTransformationsName.Clear();
		_compatibleAmmoNames.Clear();
	}

	/// <summary>Gets the custom name of the Ufopedia article related to this item.</summary>
	public string UfopediaType => _ufopediaType.Length != 0 ? _ufopediaType : _type;

	/// <summary>Gets the item type. Each item has a unique type.</summary>
	public string Type => _type;

	/// <summary>Gets the language string that names this item. This is not necessarily unique.</summary>
	public string Name => _name;

	/// <summary>Gets name id to use when displaying in loaded weapon.</summary>
	public string NameAsAmmo => _nameAsAmmo;

	/// <summary>Gets the list of research required to use this item.</summary>
	public List<RuleResearch> Requirements => _requires;

	/// <summary>Gets the list of research required to buy this item from market.</summary>
	public List<RuleResearch> BuyRequirements => _requiresBuy;

	/// <summary>Gets the allied country name required to buy this item.</summary>
	public string RequiresBuyCountry => _requiresBuyCountry;

	/// <summary>Gets the base functions required to buy craft.</summary>
	public RuleBaseFacilityFunctions RequiresBuyBaseFunc => _requiresBuyBaseFunc;

	/// <summary>Gets the dividers used for recovery of special items (specialType > 1).</summary>
	public SortedDictionary<string, int> RecoveryDividers => _recoveryDividers;

	/// <summary>Gets the item(s) to be recovered instead of this item.</summary>
	public Dictionary<RuleItem, List<int>> RecoveryTransformations => _recoveryTransformations;

	/// <summary>Gets the list of categories this item belongs to.</summary>
	public List<string> Categories => _categories;

	/// <summary>Checks if the item belongs to a category.</summary>
	public bool BelongsToCategory(string category) => _categories.Contains(category);

	/// <summary>Returns the first item category that has a non-empty invOrder, if it exists.</summary>
	public RuleItemCategory GetFirstCategoryWithInvOrder(Mod mod)
	{
		foreach (var catName in _categories)
		{
			var cat = mod.GetItemCategory(catName, false);
			if (cat != null && cat.InvOrder.Count != 0)
			{
				return cat;
			}
		}
		return null;
	}

	/// <summary>Gets unit rule if the item is vehicle weapon.</summary>
	public Unit VehicleUnit => _vehicleUnit;

	/// <summary>Gets the fixed ammo slot of the primary vehicle weapon.</summary>
	public int VehicleFixedAmmoSlot => _vehicleFixedAmmoSlot;

	/// <summary>Gets the amount of space this item takes up in a storage facility.</summary>
	public double Size => _size;

	/// <summary>Gets the item's monthly buy limit.</summary>
	public int MonthlyBuyLimit => _monthlyBuyLimit;

	/// <summary>Gets the item's monthly buy limit message.</summary>
	public string MonthlyBuyLimitMessage => _monthlyBuyLimitMessage;

	/// <summary>Gets the amount of money this item costs to purchase (0 if not purchasable).</summary>
	public int BuyCost => _costBuy;

	/// <summary>Gets the item's purchase cost.</summary>
	/// <param name="base">Current base from where item is bought.</param>
	/// <param name="save">Game.</param>
	public int GetBuyCostAdjusted(Base @base, SavedGame save)
	{
		_ = @base; //TODO: not exposed to scripts yet
		int buyPriceCoefficient = save.BuyPriceCoefficient;
		int cost = BuyCost;
		int adjusted = (int)((long)cost * buyPriceCoefficient / 100);

		adjusted = ModScript.ScriptFunc2(GetScriptEvents(ModScript.BuyCostItem), adjusted, cost, this, save, buyPriceCoefficient);

		return adjusted;
	}

	/// <summary>Gets the amount of money this item is worth to sell.</summary>
	public int SellCost => _costSell;

	/// <summary>Gets the item's sale cost.</summary>
	/// <param name="base">Current base from where item is sold.</param>
	/// <param name="save">Game.</param>
	public int GetSellCostAdjusted(Base @base, SavedGame save)
	{
		_ = @base; //TODO: not exposed to scripts yet
		int sellPriceCoefficient = save.SellPriceCoefficient;
		int cost = SellCost;
		int adjusted = (int)((long)cost * sellPriceCoefficient / 100);

		adjusted = ModScript.ScriptFunc2(GetScriptEvents(ModScript.SellCostItem), adjusted, cost, this, save, sellPriceCoefficient);

		return adjusted;
	}

	/// <summary>Gets the amount of time this item takes to arrive at a base (in hours).</summary>
	public int TransferTime => _transferTime;

	/// <summary>Gets the weight of the item (in strength units).</summary>
	public int Weight => _weight;

	/// <summary>Gets the item's maximum throw range.</summary>
	public int ThrowRange => _throwRange;

	/// <summary>Gets the item's maximum underwater throw range.</summary>
	public int UnderwaterThrowRange => _underwaterThrowRange;

	/// <summary>Gets the reference in BIGOBS.PCK for use in inventory.</summary>
	public int BigSprite => _bigSprite;

	/// <summary>Gets the reference in FLOOROB.PCK for use in battlescape.</summary>
	public int FloorSprite => _floorSprite;

	/// <summary>Gets the reference in HANDOB.PCK for use in inventory.</summary>
	public int HandSprite => _handSprite;

	/// <summary>Gets the reference in SPICONS.DAT for use in battlescape.</summary>
	public int SpecialIconSprite => _specialIconSprite;

	/// <summary>Gets cost of moving item around inventory.</summary>
	public int InventoryMoveCostPercent => _inventoryMoveCostPercent;

	/// <summary>Returns whether this item is held with two hands.</summary>
	public bool IsTwoHanded => _twoHanded;

	/// <summary>Returns whether this item must be used with both hands.</summary>
	public bool IsBlockingBothHands => _blockBothHands;

	/// <summary>Returns whether this item is a fixed weapon. You can't move/throw/drop fixed weapons - e.g. HWP turrets.</summary>
	public bool IsFixed => _fixedWeapon;

	/// <summary>Do show fixed item on unit.</summary>
	public bool FixedShow => _fixedWeaponShow;

	/// <summary>Get the default inventory slot.</summary>
	public RuleInventory DefaultInventorySlot => _defaultInventorySlot;

	/// <summary>Get inventory slot default X position.</summary>
	public int DefaultInventorySlotX => _defaultInvSlotX;

	/// <summary>Get inventory slot default Y position.</summary>
	public int DefaultInventorySlotY => _defaultInvSlotY;

	/// <summary>Gets the item's supported inventory sections.</summary>
	public List<RuleInventory> SupportedInventorySections => _supportedInventorySections;

	/// <summary>Checks if the item can be placed into a given inventory section.</summary>
	public bool CanBePlacedIntoInventorySection(RuleInventory inventorySection)
	{
		// backwards-compatibility
		if (_supportedInventorySections.Count == 0)
			return true;

		// always possible to put an item on the ground
		if (inventorySection.Type == InventoryType.INV_GROUND)
			return true;

		// otherwise check allowed inventory sections
		return _supportedInventorySections.Contains(inventorySection);
	}

	/// <summary>Returns whether this item uses waypoints.</summary>
	public int Waypoints => _waypoints;

	/// <summary>Gets the item's bullet sprite reference.</summary>
	public int BulletSprite => _bulletSprite;

	/// <summary>Gets a random sound id from a given sound vector.</summary>
	/// <param name="vector">The source vector.</param>
	/// <param name="defaultValue">Default value (in case nothing is specified = vector is empty).</param>
	private static int GetRandomSound(List<int> vector, int defaultValue = -1)
	{
		if (vector.Count != 0)
		{
			return vector[Rng.Generate(0, vector.Count - 1)];
		}
		return defaultValue;
	}

	/// <summary>Gets the item's reload sound.</summary>
	public int ReloadSound => GetRandomSound(_reloadSound);
	public List<int> ReloadSoundRaw => _reloadSound;

	/// <summary>Gets the item's prime sound.</summary>
	public int PrimeSound => GetRandomSound(_primeSound);
	public List<int> PrimeSoundRaw => _primeSound;

	/// <summary>Gets the item's unprime sound.</summary>
	public int UnprimeSound => GetRandomSound(_unprimeSound);
	public List<int> UnprimeSoundRaw => _unprimeSound;

	/// <summary>Gets the item's fire sound.</summary>
	public int FireSound => GetRandomSound(_fireSound);
	public List<int> FireSoundRaw => _fireSound;

	/// <summary>Gets the item's hit sound.</summary>
	public int HitSound => GetRandomSound(_hitSound);
	public List<int> HitSoundRaw => _hitSound;

	/// <summary>Gets the item's hit animation.</summary>
	public int HitAnimation => _hitAnimation;

	/// <summary>Gets the item's hit animation frame count.</summary>
	public int HitAnimationFrames => _hitAnimFrames;

	/// <summary>Gets the item's miss sound.</summary>
	public int HitMissSound => GetRandomSound(_hitMissSound);
	public List<int> HitMissSoundRaw => _hitMissSound;

	/// <summary>Gets the item's miss animation.</summary>
	public int HitMissAnimation => _hitMissAnimation;

	/// <summary>Gets the item's miss animation frame count.</summary>
	public int HitMissAnimationFrames => _hitMissAnimFrames;

	/// <summary>What sound does this weapon make when you swing this at someone?</summary>
	public int MeleeSound => GetRandomSound(_meleeSound, 39);
	public List<int> MeleeSoundRaw => _meleeSound;

	/// <summary>What is the starting frame offset in hit.pck to use for the animation?</summary>
	public int MeleeAnimation => _meleeAnimation;

	/// <summary>Gets the melee animation frame count.</summary>
	public int MeleeAnimationFrames => _meleeAnimFrames;

	/// <summary>What sound does this weapon make when you miss a swing?</summary>
	public int MeleeMissSound => GetRandomSound(_meleeMissSound);
	public List<int> MeleeMissSoundRaw => _meleeMissSound;

	/// <summary>What is the starting frame offset in hit.pck to use for the miss animation?</summary>
	public int MeleeMissAnimation => _meleeMissAnimation;

	/// <summary>Gets the melee miss animation frame count.</summary>
	public int MeleeMissAnimationFrames => _meleeMissAnimFrames;

	/// <summary>What sound does this weapon make when you punch someone in the face with it?</summary>
	public int MeleeHitSound => GetRandomSound(_meleeHitSound);
	public List<int> MeleeHitSoundRaw => _meleeHitSound;

	/// <summary>What sound does explosion sound?</summary>
	public int ExplosionHitSound => GetRandomSound(_explosionHitSound);
	public List<int> ExplosionHitSoundRaw => _explosionHitSound;

	/// <summary>Gets the item's psi hit sound.</summary>
	public int PsiSound => GetRandomSound(_psiSound);
	public List<int> PsiSoundRaw => _psiSound;

	/// <summary>Get the psi animation starting frame (comes from hit.pck).</summary>
	public int PsiAnimation => _psiAnimation;

	/// <summary>Gets the psi animation frame count.</summary>
	public int PsiAnimationFrames => _psiAnimFrames;

	/// <summary>Gets the item's psi miss sound.</summary>
	public int PsiMissSound => GetRandomSound(_psiMissSound);
	public List<int> PsiMissSoundRaw => _psiMissSound;

	/// <summary>Get the psi miss animation starting frame (comes from hit.pck).</summary>
	public int PsiMissAnimation => _psiMissAnimation;

	/// <summary>Gets the psi miss animation frame count.</summary>
	public int PsiMissAnimationFrames => _psiMissAnimFrames;

	/// <summary>Gets the item's power.</summary>
	public int Power => _power;

	/// <summary>Gets the item's power used for AoE explosion animation.</summary>
	public int PowerForAnimation => _powerForAnimation;

	/// <summary>Should the item's power be displayed in Ufopedia or not?</summary>
	public bool HidePower => _hidePower;

	/// <summary>How much damage does this weapon do when you punch someone in the face with it?</summary>
	public int MeleePower => _meleePower;

	/// <summary>Should the (selected) power attributes come from the weapon/firearm only (and not from the ammo, even if defined)?</summary>
	public bool IgnoreAmmoPower => _ignoreAmmoPower && _battleType == BattleType.BT_FIREARM;

	/// <summary>Gets amount of power dropped for range in voxels.</summary>
	public float GetPowerRangeReduction(float range)
	{
		range -= _powerRangeThreshold * TilesToVexels;
		return (_powerRangeReduction * VexelsToTiles) * (range > 0 ? range : 0);
	}

	public float PowerRangeReductionRaw => _powerRangeReduction;
	public float PowerRangeThresholdRaw => _powerRangeThreshold;

	/// <summary>Get amount of psi accuracy dropped for range in voxels.</summary>
	public float GetPsiAccuracyRangeReduction(float range)
	{
		range -= _confAimed.range * TilesToVexels;
		return (_dropoff * VexelsToTiles) * (range > 0 ? range : 0);
	}

	/// <summary>Compute power bonus based on unit stats.</summary>
	public int GetPowerBonus(BattleActionAttack attack) => _damageBonus.GetBonus(attack, _power);
	public RuleStatBonus DamageBonusRaw => _damageBonus;

	/// <summary>Compute power bonus for melee attack in range weapon based on unit stats.</summary>
	public int GetMeleeBonus(BattleActionAttack attack) => _meleeBonus.GetBonus(attack, _meleePower);
	public RuleStatBonus MeleeBonusRaw => _meleeBonus;

	/// <summary>Compute multiplier of accuracy based on unit stats.</summary>
	public int GetAccuracyMultiplier(BattleActionAttack attack) => _accuracyMulti.GetBonus(attack);
	public RuleStatBonus AccuracyMultiplierRaw => _accuracyMulti;

	/// <summary>Compute multiplier of melee hit chance based on unit stats.</summary>
	public int GetMeleeMultiplier(BattleActionAttack attack) => _meleeMulti.GetBonus(attack);
	public RuleStatBonus MeleeMultiplierRaw => _meleeMulti;

	/// <summary>Compute multiplier of throw accuracy based on unit stats.</summary>
	public int GetThrowMultiplier(BattleActionAttack attack) => _throwMulti.GetBonus(attack);
	public RuleStatBonus ThrowMultiplierRaw => _throwMulti;

	/// <summary>Compute multiplier of close quarters accuracy based on unit stats.</summary>
	public int GetCloseQuartersMultiplier(BattleActionAttack attack) => _closeQuartersMulti.GetBonus(attack);
	public RuleStatBonus CloseQuartersMultiplierRaw => _closeQuartersMulti;

	/// <summary>Get configuration of aimed shot action.</summary>
	public RuleItemAction ConfigAimed => _confAimed;

	/// <summary>Get configuration of autoshot action.</summary>
	public RuleItemAction ConfigAuto => _confAuto;

	/// <summary>Get configuration of snapshot action.</summary>
	public RuleItemAction ConfigSnap => _confSnap;

	/// <summary>Get configuration of melee action.</summary>
	public RuleItemAction ConfigMelee => _confMelee;

	/// <summary>Gets the item's accuracy for aimed shots.</summary>
	public int AccuracyAimed => _confAimed.accuracy;

	/// <summary>Gets the item's accuracy for autoshots.</summary>
	public int AccuracyAuto => _confAuto.accuracy;

	/// <summary>Gets the item's accuracy for snapshots.</summary>
	public int AccuracySnap => _confSnap.accuracy;

	/// <summary>Gets the item's accuracy for melee attacks.</summary>
	public int AccuracyMelee => _confMelee.accuracy;

	/// <summary>Gets the item's accuracy for use psi-amp.</summary>
	public int AccuracyUse => _accuracyUse;

	/// <summary>Gets the item's accuracy for mind control use.</summary>
	public int AccuracyMind => _accuracyMind;

	/// <summary>Gets the item's accuracy for panic use.</summary>
	public int AccuracyPanic => _accuracyPanic;

	/// <summary>Gets the item's accuracy for throw.</summary>
	public int AccuracyThrow => _accuracyThrow;

	/// <summary>Gets the item's accuracy for close quarters combat.</summary>
	public int GetAccuracyCloseQuarters(Mod mod) => _accuracyCloseQuarters != -1 ? _accuracyCloseQuarters : mod.CloseQuartersAccuracyGlobal;

	/// <summary>Gets the item's accuracy penalty for out-of-LOS targets.</summary>
	public int GetNoLOSAccuracyPenalty(Mod mod) => _noLOSAccuracyPenalty != -1 ? _noLOSAccuracyPenalty : mod.NoLOSAccuracyPenaltyGlobal;

	/// <summary>Gets the setting for primed explosives exploding in the inventory.</summary>
	public int GetExplodeInventory(Mod mod) => _explodeInventory != -1 ? _explodeInventory : (_battleType == BattleType.BT_GRENADE ? mod.ExplodeInventoryGlobal : 0);

	/// <summary>Gets the item's time unit percentage for aimed shots.</summary>
	public RuleItemUseCost CostAimed => _confAimed.cost.GetDefault();

	/// <summary>Gets the item's time unit percentage for autoshots.</summary>
	public RuleItemUseCost CostAuto => _confAuto.cost.GetDefault(_confAimed.cost);

	/// <summary>Gets the item's time unit percentage for snapshots.</summary>
	public RuleItemUseCost CostSnap => _confSnap.cost.GetDefault(_confAimed.cost);

	/// <summary>Gets the item's time unit percentage for melee attacks.</summary>
	public RuleItemUseCost CostMelee => _confMelee.cost.GetDefault();

	/// <summary>Gets the number of Time Units needed to use this item.</summary>
	public RuleItemUseCost CostUse
	{
		get
		{
			if (_battleType != BattleType.BT_PSIAMP || _psiAttackName.Length != 0)
			{
				return _costUse.GetDefault();
			}
			else
			{
				return new RuleItemUseCost();
			}
		}
	}

	/// <summary>Gets the number of Time Units needed to use mind control action.</summary>
	public RuleItemUseCost CostMind => _costMind.GetDefault(_costUse);

	/// <summary>Gets the number of Time Units needed to use panic action.</summary>
	public RuleItemUseCost CostPanic => _costPanic.GetDefault(_costUse);

	/// <summary>Gets the item's time unit percentage for throwing.</summary>
	public RuleItemUseCost CostThrow => _costThrow.GetDefault();

	/// <summary>Gets the item's time unit percentage for prime grenade.</summary>
	public RuleItemUseCost CostPrime
	{
		get
		{
			if (_primeActionName.Length != 0)
			{
				return _costPrime.GetDefault();
			}
			else
			{
				return new RuleItemUseCost();
			}
		}
	}

	/// <summary>Gets the item's time unit percentage for unprime grenade.</summary>
	public RuleItemUseCost CostUnprime => _costUnprime.GetDefault();

	/// <summary>Returns whether this item charges a flat rate for costAimed.</summary>
	public RuleItemUseFlat FlatAimed => _confAimed.flat.GetDefault(_flatUse);

	/// <summary>Returns whether this item charges a flat rate for costAuto.</summary>
	public RuleItemUseFlat FlatAuto => _confAuto.flat.GetDefault(_confAimed.flat, _flatUse);

	/// <summary>Returns whether this item charges a flat rate for costSnap.</summary>
	public RuleItemUseFlat FlatSnap => _confSnap.flat.GetDefault(_confAimed.flat, _flatUse);

	/// <summary>Returns whether this item charges a flat rate for costMelee.</summary>
	public RuleItemUseFlat FlatMelee => _confMelee.flat.GetDefault(_flatUse);

	/// <summary>Returns whether this item charges a flat rate of use and attack cost.</summary>
	public RuleItemUseFlat FlatUse => _flatUse.GetDefault();

	/// <summary>Returns whether this item charges a flat rate for costThrow.</summary>
	public RuleItemUseFlat FlatThrow => _flatThrow.GetDefault();

	/// <summary>Returns whether this item charges a flat rate for costPrime.</summary>
	public RuleItemUseFlat FlatPrime => _flatPrime.GetDefault();

	/// <summary>Returns whether this item charges a flat rate for costUnprime.</summary>
	public RuleItemUseFlat FlatUnprime => _flatUnprime.GetDefault();

	/// <summary>Gets the item's time unit for loading weapon ammo.</summary>
	public int GetTULoad(int slot) => _tuLoad[slot];

	/// <summary>Gets the item's time unit for unloading weapon ammo.</summary>
	public int GetTUUnload(int slot) => _tuUnload[slot];

	/// <summary>Gets the ammo type for a vehicle.</summary>
	public RuleItem VehicleClipAmmo
	{
		get
		{
			if (_vehicleFixedAmmoSlot > -1)
			{
				return _compatibleAmmo[_vehicleFixedAmmoSlot].Count == 0 ? null : _compatibleAmmo[_vehicleFixedAmmoSlot][0];
			}
			return null;
		}
	}

	/// <summary>Gets the maximum number of rounds for a vehicle. E.g. a vehicle that can load 6 clips with 10 rounds each, returns 60.</summary>
	public int VehicleClipSize
	{
		get
		{
			var ammo = VehicleClipAmmo;
			if (ammo != null)
			{
				if (ammo.ClipSize > 0 && ClipSize > 0)
				{
					return ClipSize;
				}
				else
				{
					return ammo.ClipSize;
				}
			}
			else
			{
				return ClipSize;
			}
		}
	}

	/// <summary>Gets the number of clips needed to fully load a vehicle. E.g. a vehicle that holds max 60 rounds and clip size is 10, returns 6.</summary>
	public int VehicleClipsLoaded
	{
		get
		{
			var ammo = VehicleClipAmmo;
			if (ammo != null)
			{
				if (ammo.ClipSize > 0 && ClipSize > 0)
				{
					return ClipSize / ammo.ClipSize;
				}
				else
				{
					return ammo.ClipSize;
				}
			}
			else
			{
				return 0;
			}
		}
	}

	/// <summary>Gets a list of compatible ammo.</summary>
	public List<RuleItem> PrimaryCompatibleAmmo => _compatibleAmmo[0];

	/// <summary>Gets slot position for ammo type.</summary>
	public int GetSlotForAmmo(RuleItem type) => _compatibleAmmoSlots.TryGetValue(type, out int slot) ? slot : -1;

	/// <summary>Gets list of compatible ammo for a slot.</summary>
	public List<RuleItem> GetCompatibleAmmoForSlot(int slot) => _compatibleAmmo[slot];

	/// <summary>Gets the item's damage type.</summary>
	public RuleDamageType DamageType => _damageType;

	public bool IsDamageTypeSet => _damageTypeSet;

	/// <summary>Gets the item's melee damage type for range weapons.</summary>
	public RuleDamageType MeleeType => _meleeType;

	public bool IsMeleeTypeSet => _meleeTypeSet;

	/// <summary>Gets the item's battle type.</summary>
	public BattleType BattleType => _battleType;

	/// <summary>Is the item's type BT_GRENADE or BT_PROXIMITYGRENADE?</summary>
	public bool IsGrenadeOrProxy => _battleType == BattleType.BT_GRENADE || _battleType == BattleType.BT_PROXIMITYGRENADE;

	/// <summary>Gets the item's fuse timer type.</summary>
	public BattleFuseType FuseTimerType => _fuseType;

	/// <summary>Gets the item's default fuse timer.</summary>
	public int FuseTimerDefault
	{
		get
		{
			if (_fuseType >= BattleFuseType.BFT_FIX_MIN && _fuseType < BattleFuseType.BFT_FIX_MAX)
			{
				return (int)_fuseType;
			}
			else if (_fuseType == BattleFuseType.BFT_SET || _fuseType == BattleFuseType.BFT_INSTANT)
			{
				return 0;
			}
			else
			{
				return -1; //can't prime
			}
		}
	}

	/// <summary>Is this item (e.g. a mine) hidden on the minimap?</summary>
	public bool IsHiddenOnMinimap => _hiddenOnMinimap;

	/// <summary>Get fuse trigger event.</summary>
	public RuleItemFuseTrigger FuseTriggerEvent => _fuseTriggerEvents;

	/// <summary>Gets the item's width in a soldier's inventory.</summary>
	public int InventoryWidth => _invWidth;

	/// <summary>Gets the item's height in a soldier's inventory.</summary>
	public int InventoryHeight => _invHeight;

	/// <summary>Gets the item's ammo clip size.</summary>
	public int ClipSize => _clipSize;

	/// <summary>Gets the chance of special effect like zombify or corpse explosion or mine triggering.</summary>
	public int SpecialChance => _specialChance;

	/// <summary>Draws and centers the hand sprite on a surface according to its dimensions.</summary>
	public void DrawHandSprite(SurfaceSet texture, Surface surface, BattleItem item = null, SavedBattleGame save = null, int animFrame = 0)
	{
		//TODO: split this function to one using only `this` and another using only `item`
		Surface frame;
		if (item != null)
		{
			frame = item.GetBigSprite(texture, save, animFrame);
			if (frame != null)
			{
				var scr = new ScriptWorkerBlit();
				BattleItem.ScriptFill(scr, item, save, (int)UnitBodyPartEx.BODYPART_ITEM_INVENTORY, animFrame, 0);
				scr.ExecuteBlit(frame, surface, HandSpriteOffX, HandSpriteOffY, 0);
			}
		}
		else
		{
			frame = texture.GetFrame(BigSprite);
			frame.BlitNShade(surface, HandSpriteOffX, HandSpriteOffY);
		}
	}

	/// <summary>Item's hand sprite x offset.</summary>
	public int HandSpriteOffX => (RuleInventory.HAND_W - InventoryWidth) * RuleInventory.SLOT_W / 2;

	/// <summary>Item's hand sprite y offset.</summary>
	public int HandSpriteOffY => (RuleInventory.HAND_H - InventoryHeight) * RuleInventory.SLOT_H / 2;

	/// <summary>Gets the heal quantity of the item.</summary>
	public int HealQuantity => _heal;

	/// <summary>Gets the pain killer quantity of the item.</summary>
	public int PainKillerQuantity => _painKiller;

	/// <summary>Gets the stimulant quantity of the item.</summary>
	public int StimulantQuantity => _stimulant;

	/// <summary>Gets the amount of fatal wound healed per usage.</summary>
	public int WoundRecovery => _woundRecovery;

	/// <summary>Gets the amount of health added to a wounded soldier's health.</summary>
	public int HealthRecovery => _healthRecovery;

	/// <summary>Gets the amount of energy added to a soldier's energy.</summary>
	public int EnergyRecovery => _energyRecovery;

	/// <summary>Gets the amount of stun removed from a soldier's stun level.</summary>
	public int StunRecovery => _stunRecovery;

	/// <summary>Gets the medikit mana recovered per shot.</summary>
	public int ManaRecovery => _manaRecovery;

	/// <summary>Gets the amount of morale added to a solders's morale.</summary>
	public int MoraleRecovery => _moraleRecovery;

	/// <summary>Gets the medikit morale recovered based on missing health.</summary>
	public float PainKillerRecovery => _painKillerRecovery;

	/// <summary>Gets the medikit's allowed targets.</summary>
	public bool AllowTargetSelf => _medikitTargetSelf;
	public bool AllowTargetImmune => _medikitTargetImmune;
	public bool AllowTargetGround => (_medikitTargetMatrix & 21) != 0; // 1 + 4 + 16
	public bool AllowTargetStanding => (_medikitTargetMatrix & 42) != 0; // 2 + 8 + 32
	public bool AllowTargetFriendGround => (_medikitTargetMatrix & 1) != 0;
	public bool AllowTargetFriendStanding => (_medikitTargetMatrix & 2) != 0;
	public bool AllowTargetNeutralGround => (_medikitTargetMatrix & 4) != 0;
	public bool AllowTargetNeutralStanding => (_medikitTargetMatrix & 8) != 0;
	public bool AllowTargetHostileGround => (_medikitTargetMatrix & 16) != 0;
	public bool AllowTargetHostileStanding => (_medikitTargetMatrix & 32) != 0;
	public int MedikitTargetMatrixRaw => _medikitTargetMatrix;

	/// <summary>Is this (medikit-type &amp; items with prime) item consumable?</summary>
	public bool IsConsumable => _isConsumable;

	/// <summary>Does this item extinguish fire?</summary>
	public bool IsFireExtinguisher => _isFireExtinguisher;

	/// <summary>Is ammo replenished after battle? (Intended side effect: ammo item with 0 ammo left does not disappear.)</summary>
	public bool IsAmmoRechargeable => _isAmmoRechargeable;

	/// <summary>If this item is used as a specialWeapon, can it be accessed by an empty hand?</summary>
	public bool IsSpecialUsingEmptyHand => _specialUseEmptyHand;

	/// <summary>Display icon in an empty hand?</summary>
	public bool ShowSpecialInEmptyHand() => _specialUseEmptyHandShow;

	/// <summary>Gets the medikit type of how it operate.</summary>
	public BattleMediKitType MediKitType => _medikitType;

	/// <summary>Gets the medikit custom background.</summary>
	public string MediKitCustomBackground => _medikitBackground;

	/// <summary>
	/// Returns the item's max explosion radius. Small explosions don't have a restriction.
	/// Larger explosions are restricted using a formula, with a maximum of radius 10 no matter how large the explosion is.
	/// </summary>
	public int GetExplosionRadius(BattleActionAttack attack)
	{
		int radius;

		if (_damageType.FixRadius == -1)
		{
			radius = (int)(GetPowerBonus(attack) * _damageType.RadiusEffectiveness);
			if (_damageType.FireBlastCalc)
			{
				radius += 1;
			}
			// cap the formula to 11
			if (radius > 11)
			{
				radius = 11;
			}
			if (radius <= 0)
			{
				radius = 1;
			}
		}
		else
		{
			// unless a blast radius is actually defined.
			radius = _damageType.FixRadius;
		}

		return radius;
	}

	/// <summary>Returns the item's recovery points. This is used during the battlescape debriefing score calculation.</summary>
	public int RecoveryPoints => _recoveryPoints;

	/// <summary>Returns the item's armor. The item is destroyed when an explosion power bigger than its armor hits it.</summary>
	public int Armor => _armor;

	/// <summary>Check if item is normal inventory item.</summary>
	public bool IsInventoryItem => BigSprite > -1 && IsFixed == false;

	/// <summary>Checks if item have some use in battlescape.</summary>
	public bool IsUsefulBattlescapeItem => _battleType != BattleType.BT_CORPSE && _battleType != BattleType.BT_NONE;

	/// <summary>Returns if the item should be recoverable from the battlescape.</summary>
	public bool IsRecoverable => _recover;

	/// <summary>
	/// Returns if the corpse item should be recoverable from the battlescape.
	/// Since the "recover" flag applies to both live body (prisoner capture) and dead body (corpse recovery) in OXC,
	/// OXCE adds this new flag to allow recovery of a live body, but disable recovery of the corpse
	/// (used in mods mostly to ignore dead bodies of killed humans).
	/// </summary>
	public bool IsCorpseRecoverable => _recoverCorpse;

	/// <summary>Checks if the item can be equipped in base defense mission.</summary>
	public bool CanBeEquippedBeforeBaseDefense() => !_ignoreInBaseDefense;

	/// <summary>Check if the item can be equipped to craft inventory.</summary>
	public bool CanBeEquippedToCraftInventory() => !_ignoreInCraftEquip;

	/// <summary>Returns the item's Turret Type.</summary>
	public int TurretType => _turretType;

	/// <summary>
	/// Returns first turn when AI can use item.
	/// If mod == null returns only local defined aiUseDelay,
	/// else takes into account global define of aiUseDelay for this item.
	/// </summary>
	public int GetAIUseDelay(Mod mod = null)
	{
		if (mod == null || _aiUseDelay >= 0)
			return _aiUseDelay;

		switch (BattleType)
		{
			case BattleType.BT_FIREARM:
				if (Waypoints != 0)
				{
					return mod.AIUseDelayBlaster;
				}
				else
				{
					return mod.AIUseDelayFirearm;
				}

			case BattleType.BT_MELEE:
				return mod.AIUseDelayMelee;

			case BattleType.BT_GRENADE:
				return mod.AIUseDelayGrenade;

			case BattleType.BT_PROXIMITYGRENADE:
				return mod.AIUseDelayProxy;

			case BattleType.BT_PSIAMP:
				return mod.AIUseDelayPsionic;

			case BattleType.BT_MEDIKIT:
				return mod.AIUseDelayMedikit;

			default:
				return _aiUseDelay;
		}
	}

	/// <summary>Returns number of melee hits AI should do when attacking enemy.</summary>
	public int AIMeleeHitCount => _aiMeleeHitCount;

	/// <summary>Returns if this is a live alien.</summary>
	public bool IsAlien => _liveAlien;

	/// <summary>Returns to which type of prison does the live alien belong.</summary>
	public int PrisonType => _liveAlienPrisonType;

	/// <summary>Returns if this weapon should arc its shots.</summary>
	public bool ArcingShot => _arcingShot;

	/// <summary>Returns the experience training mode configured for this weapon.</summary>
	public ExperienceTrainingMode ExperienceTrainingMode => _experienceTrainingMode;

	/// <summary>How much mana experience does this weapon provide?</summary>
	public int ManaExperience => _manaExperience;

	/// <summary>Gets the attraction value for this item (for AI).</summary>
	public int Attraction => _attraction;

	/// <summary>Get the load order for this item.</summary>
	public int LoadOrder => _loadOrder;

	/// <summary>Gets the list weight for this item.</summary>
	public int ListOrder => _listOrder;

	/// <summary>Gets the speed at which this bullet travels.</summary>
	public int BulletSpeed => _bulletSpeed;

	/// <summary>Gets the speed at which this bullet explodes.</summary>
	public int ExplosionSpeed => _explosionSpeed;

	/// <summary>Get name of medikit pain killer action for medikit view.</summary>
	public string PainKillerActionName => _painKillerActionName;

	/// <summary>Get name of medikit stimulant action for medikit view.</summary>
	public string StimulantActionName => _stimulantActionName;

	/// <summary>Get name of medikit heal action for medikit view.</summary>
	public string HealActionName => _healActionName;

	/// <summary>Get name of medikit action for action menu.</summary>
	public string MedikitActionName => _medikitActionName;

	/// <summary>Get name of psi attack for action menu.</summary>
	public string PsiAttackName => _psiAttackName;

	/// <summary>Get name of prime action for action menu.</summary>
	public string PrimeActionName => _primeActionName;

	/// <summary>Get message for prime action.</summary>
	public string PrimeActionMessage => _primeActionMessage;

	/// <summary>Get name of unprime action for action menu.</summary>
	public string UnprimeActionName => _unprimeActionName;

	/// <summary>Get message for unprime action.</summary>
	public string UnprimeActionMessage => _unprimeActionMessage;

	/// <summary>Get message when trying to sell the item.</summary>
	public string SellActionMessage => _sellActionMessage;

	/// <summary>Is this item a rifle?</summary>
	public bool IsRifle => (_battleType == BattleType.BT_FIREARM || _battleType == BattleType.BT_MELEE) && _twoHanded;

	/// <summary>Is this item a pistol?</summary>
	public bool IsPistol => (_battleType == BattleType.BT_FIREARM || _battleType == BattleType.BT_MELEE) && !_twoHanded;

	/// <summary>Gets the maximum range of this weapon.</summary>
	public int MaxRange => _maxRange;

	/// <summary>Checks whether a given distance is out of range for this item.</summary>
	public bool IsOutOfRange(int distanceSq)
	{
		bool outOfRange = distanceSq > (_maxRange * _maxRange);
		// special handling for short ranges and diagonals
		if (outOfRange)
		{
			// special handling for maxRange 1: allow it to target diagonally adjacent tiles (one diagonal move)
			if (_maxRange == 1 && distanceSq <= 3)
			{
				outOfRange = false;
			}
			// special handling for maxRange 2: allow it to target diagonally adjacent tiles (one diagonal move + one straight move)
			else if (_maxRange == 2 && distanceSq <= 6)
			{
				outOfRange = false;
			}
		}
		return outOfRange;
	}

	/// <summary>Checks whether a given distance is out of throw range for this item.</summary>
	public bool IsOutOfThrowRange(int distanceSq, int depth)
	{
		bool outOfRange = false;
		if (depth > 0)
		{
			if (distanceSq > _underwaterThrowRange * _underwaterThrowRange)
			{
				outOfRange = true;
			}
		}
		else
		{
			if (distanceSq > _throwRange * _throwRange)
			{
				outOfRange = true;
			}
		}
		// no special handling for short ranges and diagonals
		return outOfRange;
	}

	/// <summary>Gets the maximum effective range of this weapon when using Aimed Shot.</summary>
	public int AimRange => _confAimed.range;

	/// <summary>Gets the maximum effective range of this weapon for Snap Shot.</summary>
	public int SnapRange => _confSnap.range;

	/// <summary>Gets the maximum effective range of this weapon for Auto Shot.</summary>
	public int AutoRange => _confAuto.range;

	/// <summary>Gets the minimum effective range of this weapon.</summary>
	public int MinRange => _minRange;

	/// <summary>Gets the item's throw dropoff range.</summary>
	public int ThrowDropoffRange => _throwDropoffRange;

	/// <summary>Gets the item's underwater throw dropoff range.</summary>
	public int UnderwaterThrowDropoffRange => _underwaterThrowDropoffRange;

	/// <summary>Get the throwing accuracy dropoff of this weapon.</summary>
	public int ThrowDropoff => _throwDropoff;

	/// <summary>Gets the accuracy dropoff value of this weapon.</summary>
	public int Dropoff => _dropoff;

	/// <summary>Helper function to calculate limits and dropoff.</summary>
	public int CalculateLimits(ref int upperLimit, ref int lowerLimit, int depth, BattleActionType type)
	{
		upperLimit = type == BattleActionType.BA_THROW ? 200 : AimRange;
		lowerLimit = type == BattleActionType.BA_THROW ? 0 : MinRange;
		if (Options.battleUFOExtenderAccuracy)
		{
			switch (type)
			{
				case BattleActionType.BA_SNAPSHOT:
					upperLimit = SnapRange;
					break;
				case BattleActionType.BA_AUTOSHOT:
					upperLimit = AutoRange;
					break;
				case BattleActionType.BA_THROW:
					upperLimit = depth > 0 ? UnderwaterThrowDropoffRange : ThrowDropoffRange;
					break;
				default:
					break;
			}
		}
		return type == BattleActionType.BA_THROW ? ThrowDropoff : Dropoff;
	}

	/// <summary>Gets the number of projectiles this ammo shoots at once.</summary>
	public int ShotgunPellets => _shotgunPellets;

	/// <summary>Gets the shotgun behavior type. This is an attribute of shotgun ammo (0 = cone-like spread (vanilla), 1 = grouping).</summary>
	public int ShotgunBehaviorType => _shotgunBehaviorType;

	/// <summary>Gets the spread of shotgun projectiles. This is an attribute of shotgun ammo. Can be used in both shotgun behavior types.</summary>
	public int ShotgunSpread => _shotgunSpread;

	/// <summary>Gets the shotgun choke value for modifying pellet spread. This is an attribute of the weapon (not ammo).</summary>
	public int ShotgunChoke => _shotgunChoke;

	/// <summary>Gets the unit that the victim is morphed into when attacked.</summary>
	public string GetZombieUnit(BattleUnit victim)
	{
		if (victim != null)
		{
			// by armor and gender
			if (victim.Gender == SoldierGender.GENDER_MALE)
			{
				if (_zombieUnitByArmorMale.TryGetValue(victim.Armor.Type, out var i))
				{
					return i;
				}
			}
			else
			{
				if (_zombieUnitByArmorFemale.TryGetValue(victim.Armor.Type, out var j))
				{
					return j;
				}
			}
			// by type
			string victimType = victim.UnitRules != null ? victim.UnitRules.Type : victim.GeoscapeSoldier.Rules.Type;
			if (_zombieUnitByType.TryGetValue(victimType, out var k))
			{
				return k;
			}
		}
		// fall back
		return _zombieUnit;
	}

	public SortedDictionary<string, string> ZombieUnitByArmorMaleRaw => _zombieUnitByArmorMale;
	public SortedDictionary<string, string> ZombieUnitByArmorFemaleRaw => _zombieUnitByArmorFemale;
	public SortedDictionary<string, string> ZombieUnitByTypeRaw => _zombieUnitByType;

	/// <summary>Gets which faction the spawned unit should have.</summary>
	public UnitFaction ZombieUnitFaction => _zombieUnitFaction;

	/// <summary>Gets chance for zombie transformation on attack.</summary>
	public int ZombieUnitChance => LoadYaml.UseIntNullable(_zombieUnitChance, _specialChance);

	/// <summary>Gets the weapon's spawn unit.</summary>
	public Unit SpawnUnit => _spawnUnit;

	/// <summary>Gets which faction the spawned unit should have.</summary>
	public UnitFaction SpawnUnitFaction => _spawnUnitFaction;

	/// <summary>Gets chance for unit spawn on attack.</summary>
	public int SpawnUnitChance => LoadYaml.UseIntNullable(_spawnUnitChance, _specialChance);

	/// <summary>Gets the weapon's spawn item.</summary>
	public RuleItem SpawnItem => _spawnItem;

	/// <summary>Gets chance for item spawn on attack.</summary>
	public int SpawnItemChance => LoadYaml.UseIntNullable(_spawnItemChance, _specialChance);

	/// <summary>
	/// Checks if this item can be used to target a given faction.
	/// Usage #1: checks the psiamp's allowed targets.
	/// - Not used in AI.
	/// - Mind control of the same faction is hardcoded disabled.
	/// Usage #2: checks if a death trap item applies to a given faction (it use `attacker == FACTION_PLAYER`).
	/// </summary>
	public bool IsTargetAllowed(UnitFaction targetFaction, UnitFaction attacker)
	{
		if (targetFaction == attacker) // same faction "attack"
		{
			return (_targetMatrix & 1) != 0;
		}
		else if ((targetFaction == UnitFaction.FACTION_HOSTILE) || (attacker == UnitFaction.FACTION_HOSTILE)) // can't be both true as first `if` cover this case
		{
			return (_targetMatrix & 2) != 0;
		}
		else if ((targetFaction == UnitFaction.FACTION_NEUTRAL) || (attacker == UnitFaction.FACTION_NEUTRAL)) // only `Player -> Neutral` or `Neutral -> Player` left
		{
			return (_targetMatrix & 4) != 0;
		}
		return false;
	}

	public int TargetMatrixRaw => _targetMatrix;

	/// <summary>Should mind control convert the unit to the neutral faction?</summary>
	public bool ConvertToCivilian() => _convertToCivilian;

	/// <summary>Is line of sight required for this psionic weapon to function?</summary>
	public bool IsLOSRequired => _LOSRequired;

	/// <summary>Can this item only be used underwater?</summary>
	public bool IsWaterOnly => _underwaterOnly;

	/// <summary>Can this item only be used on land?</summary>
	public bool IsLandOnly => _landOnly;

	/// <summary>Is psi skill is required to use this weapon.</summary>
	public bool IsPsiRequired => _psiReqiured;

	/// <summary>Is mana required to use this weapon?</summary>
	public bool IsManaRequired => _manaRequired;

	/// <summary>
	/// Gets the associated special type of this item.
	/// Note that type 14 is the alien brain, and types
	/// 0 and 1 are "regular tile" and "starting point"
	/// so try not to use those ones.
	/// </summary>
	public int SpecialType => _specialType;

	/// <summary>Gets the color offset to use for the vapor trail.</summary>
	public int GetVaporColor(int depth)
	{
		if (depth == 0)
			return _vaporColorSurface;
		return _vaporColor;
	}

	/// <summary>Gets the vapor cloud density for the vapor trail.</summary>
	public int GetVaporDensity(int depth)
	{
		if (depth == 0)
			return _vaporDensitySurface;
		return _vaporDensity;
	}

	/// <summary>Gets the vapor cloud probability for the vapor trail.</summary>
	public int GetVaporProbability(int depth)
	{
		if (depth == 0)
			return _vaporProbabilitySurface;
		return _vaporProbability;
	}

	/// <summary>Gets the index of the sprite in the CustomItemPreview sprite set.</summary>
	public List<int> CustomItemPreviewIndex => _customItemPreviewIndex;

	/// <summary>Gets the kneel bonus (15% bonus is encoded as 100+15 = 115).</summary>
	public int GetKneelBonus(Mod mod) => _kneelBonus != -1 ? _kneelBonus : mod.KneelBonusGlobal;

	/// <summary>Gets the one-handed penalty (20% penalty is encoded as 100-20 = 80).</summary>
	public int GetOneHandedPenalty(Mod mod) => _oneHandedPenalty != -1 ? _oneHandedPenalty : mod.OneHandedPenaltyGlobal;

	/// <summary>Gets the monthly salary.</summary>
	public int MonthlySalary => _monthlySalary;

	/// <summary>Gets the monthly maintenance.</summary>
	public int MonthlyMaintenance => _monthlyMaintenance;

	/// <summary>Gets how many waypoints are used for a "spray" attack.</summary>
	public int SprayWaypoints => _sprayWaypoints;
}
