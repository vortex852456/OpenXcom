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
// Ported from: src/Mod/Armor.h, src/Mod/Armor.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Move cost multiplier.
/// </summary>
public struct ArmorMoveCost : IEquatable<ArmorMoveCost>
{
	public int TimePercent;
	public int EnergyPercent;

	/// <summary>Component-wise multiplication (C++ operator*=).</summary>
	public static ArmorMoveCost operator *(ArmorMoveCost a, ArmorMoveCost c)
	{
		a.TimePercent *= c.TimePercent;
		a.EnergyPercent *= c.EnergyPercent;
		return a;
	}

	/// <summary>Component-wise multiplication (named alternative of the operator).</summary>
	public static ArmorMoveCost Multiply(ArmorMoveCost a, ArmorMoveCost c) => a * c;

	public static bool operator ==(ArmorMoveCost a, ArmorMoveCost c) => a.Equals(c);

	public static bool operator !=(ArmorMoveCost a, ArmorMoveCost c) => !a.Equals(c);

	public readonly bool Equals(ArmorMoveCost other) => TimePercent == other.TimePercent && EnergyPercent == other.EnergyPercent;

	public override readonly bool Equals(object obj) => obj is ArmorMoveCost other && Equals(other);

	public override readonly int GetHashCode() => HashCode.Combine(TimePercent, EnergyPercent);

	/// <summary>Loads the pair [time%, energy%] (nothing happens if the node does not exist).</summary>
	public void Load(YamlNodeReader reader)
	{
		if (!reader)
			return;
		TimePercent = reader[0].ReadVal<int>();
		EnergyPercent = reader[1].ReadVal<int>();
	}

	/// <summary>Saves the pair [time%, energy%] under the given key.</summary>
	public readonly void Save(YamlNodeWriter writer, string name)
	{
		ArgumentNullException.ThrowIfNull(writer);
		var pairWriter = writer[name];
		pairWriter.SetAsSeq();
		pairWriter.Write(TimePercent);
		pairWriter.Write(EnergyPercent);
	}
}

/// <summary>
/// Represents a specific type of armor.
/// Not only soldier armor, but also alien armor - some alien races wear
/// Soldier Armor, Leader Armor or Commander Armor depending on their rank.
/// </summary>
public partial class Armor
{
	/// <summary>Name of class used in script.</summary>
	public const string ScriptName = "RuleArmor";
	public const string NONE = "STR_NONE";

	/// <summary>Default value that is interpreted as null (src/Mod/LoadYaml.h).</summary>
	private const sbyte defBoolNullable = -1;

	private string _ufopediaType = "";
	private readonly string _type;
	private string _spriteSheet = "", _spriteInv = "", _corpseGeoName = "", _storeItemName = "", _selfDestructItemName = "", _specWeaponName = "";
	private string _requiresName = "";
	private string _requiresAwardName = "";
	private string _requiresBonusName = "";
	private string _layersDefaultPrefix = "";
	private SortedDictionary<int, string> _layersSpecificPrefix = [];
	private SortedDictionary<string, List<string>> _layersDefinition = new(StringComparer.Ordinal);
	private List<string> _corpseBattleNames = [];
	private List<string> _builtInWeaponsNames = [];
	private List<string> _unitsNames = [];
	private readonly List<RuleItem> _corpseBattle = [];
	private readonly List<RuleItem> _builtInWeapons = [];
	private readonly List<RuleSoldier> _units = [];
	private readonly List<int> _ranks = [];
	private RuleResearch _requires;
	private RuleCommendations _requiresAward;
	private RuleSoldierBonus _requiresBonus;
	private RuleItem _corpseGeo;
	private RuleItem _storeItem;
	private RuleItem _selfDestructItem;
	private RuleItem _specWeapon;
	private bool _infiniteSupply;
	private int _frontArmor, _sideArmor, _leftArmorDiff, _rearArmor, _underArmor, _drawingRoutine;
	private bool _drawBubbles;
	private MovementType _movementType;
	private SpecialAbility _specab;
	private AIAttackWeight? _aiTargetWeightAsHostile;
	private AIAttackWeight? _aiTargetWeightAsHostileCivilians;
	private AIAttackWeight? _aiTargetWeightAsFriendly;
	private AIAttackWeight? _aiTargetWeightAsNeutral;
	private bool _turnBeforeFirstStep;
	private int _turnCost;
	private ArmorMoveCost _moveCostBase = new() { TimePercent = 100, EnergyPercent = 100 };
	private ArmorMoveCost _moveCostBaseFly = new() { TimePercent = 100, EnergyPercent = 100 };
	private ArmorMoveCost _moveCostBaseClimb = new() { TimePercent = 100, EnergyPercent = 100 };
	private ArmorMoveCost _moveCostBaseNormal = new() { TimePercent = 100, EnergyPercent = 100 };
	private ArmorMoveCost _moveCostWalk = new() { TimePercent = 100, EnergyPercent = 50 };
	private ArmorMoveCost _moveCostRun = new() { TimePercent = 75, EnergyPercent = 75 };
	private ArmorMoveCost _moveCostStrafe = new() { TimePercent = 100, EnergyPercent = 50 };
	private ArmorMoveCost _moveCostSneak = new() { TimePercent = 100, EnergyPercent = 50 };
	private ArmorMoveCost _moveCostFlyWalk = new() { TimePercent = 100, EnergyPercent = 50 };
	private ArmorMoveCost _moveCostFlyRun = new() { TimePercent = 75, EnergyPercent = 75 };
	private ArmorMoveCost _moveCostFlyStrafe = new() { TimePercent = 100, EnergyPercent = 50 };
	private ArmorMoveCost _moveCostFlyUp = new() { TimePercent = 100, EnergyPercent = 0 };
	private ArmorMoveCost _moveCostFlyDown = new() { TimePercent = 100, EnergyPercent = 0 };
	private ArmorMoveCost _moveCostClimbUp = new() { TimePercent = 100, EnergyPercent = 50 };
	private ArmorMoveCost _moveCostClimbDown = new() { TimePercent = 100, EnergyPercent = 50 };
	private ArmorMoveCost _moveCostGravLift = new() { TimePercent = 100, EnergyPercent = 0 };
	private int _moveSound;
	private readonly List<int> _deathSoundMale = [], _deathSoundFemale = [];
	private readonly List<int> _selectUnitSoundMale = [], _selectUnitSoundFemale = [];
	private readonly List<int> _startMovingSoundMale = [], _startMovingSoundFemale = [];
	private readonly List<int> _selectWeaponSoundMale = [], _selectWeaponSoundFemale = [];
	private readonly List<int> _annoyedSoundMale = [], _annoyedSoundFemale = [];
	private int _size, _weight, _visibilityAtDark, _visibilityAtDay;
	private int _personalLightFriend = 15;
	private int _personalLightHostile;
	private int _personalLightNeutral;
	private int _camouflageAtDay, _camouflageAtDark, _antiCamouflageAtDay, _antiCamouflageAtDark;
	private int _visibilityThroughSmoke, _visibilityThroughFire;
	private int _psiVision, _psiCamouflage;
	private readonly float[] _damageModifier = new float[(int)ItemDamageType.DAMAGE_TYPES];
	private readonly List<int> _loftempsSet = [];
	private UnitStats _stats;
	private int _deathFrames;
	private bool _constantAnimation, _hasInventory;
	private ForcedTorso _forcedTorso;
	private int _faceColorGroup, _hairColorGroup, _utileColorGroup, _rankColorGroup;
	private readonly List<int> _faceColor = [], _hairColor = [], _utileColor = [], _rankColor = [];
	private sbyte _fearImmune, _bleedImmune, _painImmune, _zombiImmune;
	private sbyte _ignoresMeleeThreat, _createsMeleeThreat;
	private float _overKill, _meleeDodgeBackPenalty;
	private readonly RuleStatBonus _psiDefence = new(), _meleeDodge = new();
	private readonly RuleStatBonus _timeRecovery = new(), _energyRecovery = new(), _moraleRecovery = new(), _healthRecovery = new(), _stunRecovery = new(), _manaRecovery = new();
	private readonly List<int> _customArmorPreviewIndex = [];
	private sbyte _allowsRunning, _allowsStrafing, _allowsSneaking, _allowsKneeling, _allowsMoving;
	private bool _isPilotArmor;
	private bool _allowTwoMainWeapons;
	private bool _instantWoundRecovery;
	private bool _isAlwaysVisible;
	private int _standHeight, _kneelHeight, _floatHeight;
	private int _meleeOriginVoxelVerticalOffset;
	private int _group;
	private int _listOrder;

	/// <summary>Creates a blank ruleset for a certain type of armor.</summary>
	/// <param name="type">String defining the type.</param>
	/// <param name="listOrder">The list weight for this armor.</param>
	public Armor(string type, int listOrder)
	{
		_type = type;
		_infiniteSupply = false;
		_frontArmor = 0;
		_sideArmor = 0;
		_leftArmorDiff = 0;
		_rearArmor = 0;
		_underArmor = 0;
		_drawingRoutine = 0;
		_drawBubbles = false;
		_movementType = MovementType.MT_WALK;
		_specab = Acom.SpecialAbility.SPECAB_NONE;
		_turnBeforeFirstStep = false;
		_turnCost = 1;
		_moveSound = -1;
		_size = 1;
		_weight = 0;
		_visibilityAtDark = 0;
		_visibilityAtDay = 0;
		_camouflageAtDay = 0;
		_camouflageAtDark = 0;
		_antiCamouflageAtDay = 0;
		_antiCamouflageAtDark = 0;
		_visibilityThroughSmoke = 0;
		_visibilityThroughFire = 100;
		_psiVision = 0;
		_psiCamouflage = 0;
		_deathFrames = 3;
		_constantAnimation = false;
		_hasInventory = true;
		_forcedTorso = ForcedTorso.TORSO_USE_GENDER;
		_faceColorGroup = 0;
		_hairColorGroup = 0;
		_utileColorGroup = 0;
		_rankColorGroup = 0;
		_fearImmune = defBoolNullable;
		_bleedImmune = defBoolNullable;
		_painImmune = defBoolNullable;
		_zombiImmune = defBoolNullable;
		_ignoresMeleeThreat = defBoolNullable;
		_createsMeleeThreat = defBoolNullable;
		_overKill = 0.5f;
		_meleeDodgeBackPenalty = 0;
		_allowsRunning = defBoolNullable;
		_allowsStrafing = defBoolNullable;
		_allowsSneaking = defBoolNullable;
		_allowsKneeling = defBoolNullable;
		_allowsMoving = 1;
		_isPilotArmor = false;
		_allowTwoMainWeapons = false;
		_instantWoundRecovery = false;
		_standHeight = -1;
		_kneelHeight = -1;
		_floatHeight = -1;
		_meleeOriginVoxelVerticalOffset = 0;
		_group = 0;
		_listOrder = listOrder;

		for (int i = 0; i < (int)ItemDamageType.DAMAGE_TYPES; i++)
			_damageModifier[i] = 1.0f;

		_psiDefence.SetPsiDefense();
		_timeRecovery.SetTimeRecovery();
		_energyRecovery.SetEnergyRecovery();
		_stunRecovery.SetStunRecovery();

		_customArmorPreviewIndex.Add(Mod.NO_SURFACE);
	}

	/// <summary>Load bool as int from yaml (src/Mod/LoadYaml.h loadBoolNullable).</summary>
	private static void LoadBoolNullable(ref sbyte value, YamlNodeReader reader)
	{
		if (!reader)
			return;
		value = reader.HasNullVal ? defBoolNullable : (sbyte)(reader.ReadVal<bool>() ? 1 : 0);
	}

	/// <summary>src/Mod/LoadYaml.h useBoolNullable.</summary>
	private static bool UseBoolNullable(int value, bool def) => value == defBoolNullable ? def : value != 0;

	/// <summary>Loads the armor from YAML.</summary>
	/// <param name="node">YAML node.</param>
	/// <param name="mod">Mod for the armor.</param>
	public void Load(YamlNodeReader node, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		var reader = node.UseIndex();
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent, mod);
		}

		reader.TryRead("ufopediaType", ref _ufopediaType);
		reader.TryRead("spriteSheet", ref _spriteSheet);
		reader.TryRead("spriteInv", ref _spriteInv);
		reader.TryRead("allowInv", ref _hasInventory);
		if (reader["corpseItem"])
		{
			_corpseBattleNames.Clear();
			_corpseBattleNames.Add(reader["corpseItem"].ReadVal<string>());
			_corpseGeoName = _corpseBattleNames[0];
		}
		else if (reader["corpseBattle"])
		{
			mod.LoadNames(_type, _corpseBattleNames, reader["corpseBattle"]);
			_corpseGeoName = _corpseBattleNames[0];
		}
		mod.LoadNames(_type, _builtInWeaponsNames, reader["builtInWeapons"]);
		mod.LoadName(_type, ref _corpseGeoName, reader["corpseGeo"]);
		mod.LoadNameNull(_type, ref _storeItemName, reader["storeItem"]);
		mod.LoadNameNull(_type, ref _selfDestructItemName, reader["selfDestructItem"]);
		mod.LoadNameNull(_type, ref _specWeaponName, reader["specialWeapon"]);
		mod.LoadNameNull(_type, ref _requiresName, reader["requires"]);
		mod.LoadNameNull(_type, ref _requiresAwardName, reader["requiresAward"]);
		mod.LoadNameNull(_type, ref _requiresBonusName, reader["requiresBonus"]);

		reader.TryRead("layersDefaultPrefix", ref _layersDefaultPrefix);
		reader.TryRead("layersSpecificPrefix", ref _layersSpecificPrefix);
		reader.TryRead("layersDefinition", ref _layersDefinition);

		reader.TryRead("frontArmor", ref _frontArmor);
		reader.TryRead("sideArmor", ref _sideArmor);
		reader.TryRead("leftArmorDiff", ref _leftArmorDiff);
		reader.TryRead("rearArmor", ref _rearArmor);
		reader.TryRead("underArmor", ref _underArmor);
		reader.TryRead("drawingRoutine", ref _drawingRoutine);
		reader.TryRead("drawBubbles", ref _drawBubbles);
		reader.TryRead("movementType", ref _movementType);
		reader.TryRead("specab", ref _specab);

		var ai = reader["ai"];
		if (ai)
		{
			ai.TryRead("targetWeightAsHostile", ref _aiTargetWeightAsHostile);
			ai.TryRead("targetWeightAsHostileCivilians", ref _aiTargetWeightAsHostileCivilians);
			ai.TryRead("targetWeightAsFriendly", ref _aiTargetWeightAsFriendly);
			ai.TryRead("targetWeightAsNeutral", ref _aiTargetWeightAsNeutral);
		}

		reader.TryRead("turnBeforeFirstStep", ref _turnBeforeFirstStep);
		reader.TryRead("turnCost", ref _turnCost);
		var move = reader["moveCost"];
		if (move)
		{
			_moveCostBase.Load(move["basePercent"]);
			_moveCostBaseFly.Load(move["baseFlyPercent"]);
			_moveCostBaseClimb.Load(move["baseClimbPercent"]);
			_moveCostBaseNormal.Load(move["baseNormalPercent"]);

			_moveCostWalk.Load(move["walkPercent"]);
			_moveCostRun.Load(move["runPercent"]);
			_moveCostStrafe.Load(move["strafePercent"]);
			_moveCostSneak.Load(move["sneakPercent"]);

			_moveCostFlyWalk.Load(move["flyWalkPercent"]);
			_moveCostFlyRun.Load(move["flyRunPercent"]);
			_moveCostFlyStrafe.Load(move["flyStrafePercent"]);

			_moveCostFlyUp.Load(move["flyUpPercent"]);
			_moveCostFlyDown.Load(move["flyDownPercent"]);

			_moveCostClimbUp.Load(move["climbUpPercent"]);
			_moveCostClimbDown.Load(move["climbDownPercent"]);

			_moveCostGravLift.Load(move["gravLiftPercent"]);
		}

		mod.LoadSoundOffset(_type, ref _moveSound, reader["moveSound"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _deathSoundMale, reader["deathMale"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _deathSoundFemale, reader["deathFemale"], "BATTLE.CAT");

		mod.LoadSoundOffset(_type, _selectUnitSoundMale, reader["selectUnitMale"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _selectUnitSoundFemale, reader["selectUnitFemale"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _startMovingSoundMale, reader["startMovingMale"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _startMovingSoundFemale, reader["startMovingFemale"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _selectWeaponSoundMale, reader["selectWeaponMale"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _selectWeaponSoundFemale, reader["selectWeaponFemale"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _annoyedSoundMale, reader["annoyedMale"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _annoyedSoundFemale, reader["annoyedFemale"], "BATTLE.CAT");

		reader.TryRead("weight", ref _weight);
		reader.TryRead("visibilityAtDark", ref _visibilityAtDark);
		reader.TryRead("visibilityAtDay", ref _visibilityAtDay);
		reader.TryRead("personalLight", ref _personalLightFriend);
		reader.TryRead("personalLightHostile", ref _personalLightHostile);
		reader.TryRead("personalLightNeutral", ref _personalLightNeutral);
		reader.TryRead("camouflageAtDay", ref _camouflageAtDay);
		reader.TryRead("camouflageAtDark", ref _camouflageAtDark);
		reader.TryRead("antiCamouflageAtDay", ref _antiCamouflageAtDay);
		reader.TryRead("antiCamouflageAtDark", ref _antiCamouflageAtDark);
		reader.TryRead("heatVision", ref _visibilityThroughSmoke);
		reader.TryRead("visibilityThroughFire", ref _visibilityThroughFire);
		reader.TryRead("psiVision", ref _psiVision);
		reader.TryRead("psiCamouflage", ref _psiCamouflage);
		reader.TryRead("alwaysVisible", ref _isAlwaysVisible);

		_stats.Merge(reader["stats"].ReadVal(_stats));
		var dmg = reader["damageModifier"];
		if (dmg)
		{
			int end = Math.Min(dmg.ChildrenCount, (int)ItemDamageType.DAMAGE_TYPES);
			for (int i = 0; i < end; ++i)
			{
				_damageModifier[i] = dmg[i].ReadVal<float>();
			}
		}
		mod.LoadInts(_type, _loftempsSet, reader["loftempsSet"]);
		if (reader["loftemps"])
		{
			int loftemps = reader["loftemps"].ReadVal<int>();
			_loftempsSet.Clear();
			_loftempsSet.Add(loftemps);
		}
		reader.TryRead("deathFrames", ref _deathFrames);
		reader.TryRead("constantAnimation", ref _constantAnimation);
		reader.TryRead("forcedTorso", ref _forcedTorso);

		if (reader.TryRead("size", ref _size))
		{
			if (_size != 1) //TODO: Add handling for opposite case too
			{
				_fearImmune = 1;
				_bleedImmune = 1;
				_painImmune = 1;
				_zombiImmune = 1;
				_ignoresMeleeThreat = 1;
				_createsMeleeThreat = 0;
			}
		}
		LoadBoolNullable(ref _fearImmune, reader["fearImmune"]);
		LoadBoolNullable(ref _bleedImmune, reader["bleedImmune"]);
		LoadBoolNullable(ref _painImmune, reader["painImmune"]);
		if (_size == 1) //Big units are always immune, because game we don't have 2x2 unit zombie
		{
			LoadBoolNullable(ref _zombiImmune, reader["zombiImmune"]);
		}
		LoadBoolNullable(ref _ignoresMeleeThreat, reader["ignoresMeleeThreat"]);
		LoadBoolNullable(ref _createsMeleeThreat, reader["createsMeleeThreat"]);

		reader.TryRead("overKill", ref _overKill);
		reader.TryRead("meleeDodgeBackPenalty", ref _meleeDodgeBackPenalty);

		var parsers = mod.ScriptParsers;
		_psiDefence.Load(_type, reader, parsers.bonusStatsScripts.Get(ModScript.PsiDefenceStatBonus));
		_meleeDodge.Load(_type, reader, parsers.bonusStatsScripts.Get(ModScript.MeleeDodgeStatBonus));

		_timeRecovery.Load(_type, reader["recovery"], parsers.bonusStatsScripts.Get(ModScript.TimeRecoveryStatBonus));
		_energyRecovery.Load(_type, reader["recovery"], parsers.bonusStatsScripts.Get(ModScript.EnergyRecoveryStatBonus));
		_moraleRecovery.Load(_type, reader["recovery"], parsers.bonusStatsScripts.Get(ModScript.MoraleRecoveryStatBonus));
		_healthRecovery.Load(_type, reader["recovery"], parsers.bonusStatsScripts.Get(ModScript.HealthRecoveryStatBonus));
		_manaRecovery.Load(_type, reader["recovery"], parsers.bonusStatsScripts.Get(ModScript.ManaRecoveryStatBonus));
		_stunRecovery.Load(_type, reader["recovery"], parsers.bonusStatsScripts.Get(ModScript.StunRecoveryStatBonus));

		reader.TryRead("spriteFaceGroup", ref _faceColorGroup);
		reader.TryRead("spriteHairGroup", ref _hairColorGroup);
		reader.TryRead("spriteRankGroup", ref _rankColorGroup);
		reader.TryRead("spriteUtileGroup", ref _utileColorGroup);
		mod.LoadInts(_type, _faceColor, reader["spriteFaceColor"]);
		mod.LoadInts(_type, _hairColor, reader["spriteHairColor"]);
		mod.LoadInts(_type, _rankColor, reader["spriteRankColor"]);
		mod.LoadInts(_type, _utileColor, reader["spriteUtileColor"]);

		LoadScripts(reader, parsers);

		mod.LoadUnorderedNames(_type, _unitsNames, reader["units"]);
		mod.LoadUnorderedInts(_type, _ranks, reader["ranks"]);
		_scriptValues.Load(reader, parsers.GetShared());
		mod.LoadSpriteOffset(_type, _customArmorPreviewIndex, reader["customArmorPreviewIndex"], "CustomArmorPreviews");
		LoadBoolNullable(ref _allowsRunning, reader["allowsRunning"]);
		LoadBoolNullable(ref _allowsStrafing, reader["allowsStrafing"]);
		LoadBoolNullable(ref _allowsSneaking, reader["allowsSneaking"]);
		LoadBoolNullable(ref _allowsKneeling, reader["allowsKneeling"]);
		LoadBoolNullable(ref _allowsMoving, reader["allowsMoving"]);
		reader.TryRead("isPilotArmor", ref _isPilotArmor);
		reader.TryRead("allowTwoMainWeapons", ref _allowTwoMainWeapons);
		reader.TryRead("instantWoundRecovery", ref _instantWoundRecovery);
		reader.TryRead("standHeight", ref _standHeight);
		reader.TryRead("kneelHeight", ref _kneelHeight);
		reader.TryRead("floatHeight", ref _floatHeight);
		reader.TryRead("meleeOriginVoxelVerticalOffset", ref _meleeOriginVoxelVerticalOffset);
		reader.TryRead("group", ref _group);
		reader.TryRead("listOrder", ref _listOrder);
	}

	/// <summary>Cross link with other rules.</summary>
	public void AfterLoad(Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		mod.VerifySoundOffset(_type, _moveSound, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _deathSoundMale, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _deathSoundFemale, "BATTLE.CAT");

		mod.VerifySoundOffset(_type, _selectUnitSoundMale, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _selectUnitSoundFemale, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _startMovingSoundMale, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _startMovingSoundFemale, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _selectWeaponSoundMale, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _selectWeaponSoundFemale, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _annoyedSoundMale, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _annoyedSoundFemale, "BATTLE.CAT");

		mod.VerifySpriteOffset(_type, _customArmorPreviewIndex, "CustomArmorPreviews");

		// mod->linkRule(...): look up each name (error if missing), then clear the names
		foreach (var n in _corpseBattleNames)
		{
			_corpseBattle.Add(mod.GetItem(n, true));
		}
		_corpseBattleNames = [];
		_corpseGeo = mod.GetItem(_corpseGeoName, true);
		_corpseGeoName = "";
		foreach (var n in _builtInWeaponsNames)
		{
			_builtInWeapons.Add(mod.GetItem(n, true));
		}
		_builtInWeaponsNames = [];
		foreach (var n in _unitsNames)
		{
			_units.Add(mod.GetSoldier(n, true));
		}
		_unitsNames = [];
		_requires = mod.GetResearch(_requiresName, true);
		_requiresName = "";
		_requiresAward = mod.GetCommendation(_requiresAwardName, true);
		_requiresAwardName = "";
		_requiresBonus = mod.GetSoldierBonus(_requiresBonusName, true);
		_requiresBonusName = "";
		if (_storeItemName == Armor.NONE)
		{
			_infiniteSupply = true;
		}
		_storeItem = mod.GetItem(_storeItemName, true); //special logic there: "STR_NONE" -> null
		_storeItemName = "";
		_selfDestructItem = mod.GetItem(_selfDestructItemName, true);
		_selfDestructItemName = "";
		_specWeapon = mod.GetItem(_specWeaponName, true);
		_specWeaponName = "";

		{
			int totalSize = TotalSize;

			mod.CheckForSoftError(_corpseBattle.Count != totalSize, _type, "Number of battle corpse items for 'corpseBattle' does not match the armor size.", SeverityLevel.LOG_ERROR);
			mod.CheckForSoftError(_loftempsSet.Count != totalSize, _type, "Number of defined templates for 'loftempsSet' or 'loftemps' does not match the armor size.", SeverityLevel.LOG_ERROR);

			int s = mod.VoxelData.Count / 16;
			foreach (var lof in _loftempsSet)
			{
				mod.CheckForSoftError(lof >= s, _type, "Value " + lof + " in 'loftempsSet' or 'loftemps' is larger than number of avaiable templates.", SeverityLevel.LOG_ERROR);
			}
		}

		int numCorpse = 0;
		foreach (var c in _corpseBattle)
		{
			if (c == null)
			{
				throw new InvalidOperationException("Battle corpse item(s) cannot be empty.");
			}

			if (numCorpse++ == 0)
			{
				// only the first item needs to be a corpse item
				mod.CheckForSoftError(c.BattleType != BattleType.BT_CORPSE, _type, "The first battle corpse item must be of item type 'corpse' (battleType: 11)");
			}
			else
			{
				mod.CheckForSoftError(c.IsRecoverable, _type, "Multiple recoverable battle corpse item(s)");
			}
		}
		if (_corpseGeo == null)
		{
			throw new InvalidOperationException("Geo corpse item cannot be empty.");
		}

		// calcualte final surfaces used by layers
		if (_layersDefaultPrefix.Length != 0)
		{
			foreach (var version in _layersDefinition)
			{
				int layerIndex = 0;
				var layers = version.Value;
				for (int i = 0; i < layers.Count; i++)
				{
					if (layers[i].Length != 0)
					{
						string prefix = _layersSpecificPrefix.TryGetValue(layerIndex, out var pre) ? pre : _layersDefaultPrefix;
						layers[i] = prefix + "__" + layerIndex + "__" + layers[i];

						//check if surface is valid
						if (Options.lazyLoadResources == false)
						{
							mod.CheckForSoftError(mod.GetSurface(layers[i], false) == null, _type, "Missing surface definition for '" + layers[i] + "'", SeverityLevel.LOG_ERROR);
						}
					}
					layerIndex++;
				}
				//clean unused layers
				layers.RemoveAll(s => s.Length == 0);
			}
		}

		// C++ sorts the RuleSoldier pointers by address (for binary search); here the units keep the
		// definition order and GetCanBeUsedBy uses a linear search.
		_ranks.Sort();
	}

	/// <summary>Gets whether or not there is an infinite supply of this armor.</summary>
	public bool HasInfiniteSupply => _infiniteSupply;

	/// <summary>Gets the custom name of the Ufopedia article related to this armor.</summary>
	public string UfopediaType => _ufopediaType.Length != 0 ? _ufopediaType : _type;

	/// <summary>Returns the language string that names this armor. Each armor has a unique name. Coveralls, Power Suit,...</summary>
	public string Type => _type;

	/// <summary>Gets the unit's sprite sheet.</summary>
	public string SpriteSheet => _spriteSheet;

	/// <summary>Gets the unit's inventory sprite.</summary>
	public string SpriteInventory => _spriteInv;

	/// <summary>Gets the front armor level.</summary>
	public int FrontArmor => _frontArmor;

	/// <summary>Gets the left side armor level.</summary>
	public int LeftSideArmor => _sideArmor + _leftArmorDiff;

	/// <summary>Gets the right side armor level.</summary>
	public int RightSideArmor => _sideArmor;

	/// <summary>Gets the rear armor level.</summary>
	public int RearArmor => _rearArmor;

	/// <summary>Gets the under armor level.</summary>
	public int UnderArmor => _underArmor;

	/// <summary>Gets the armor level of armor side.</summary>
	public int GetArmor(UnitSide side)
	{
		switch (side)
		{
			case UnitSide.SIDE_FRONT: return _frontArmor;
			case UnitSide.SIDE_LEFT: return _sideArmor + _leftArmorDiff;
			case UnitSide.SIDE_RIGHT: return _sideArmor;
			case UnitSide.SIDE_REAR: return _rearArmor;
			case UnitSide.SIDE_UNDER: return _underArmor;
			default: return 0;
		}
	}

	/// <summary>Gets the corpse item used in the Geoscape.</summary>
	public RuleItem CorpseGeoscape => _corpseGeo;

	/// <summary>Gets the list of corpse items dropped by the unit in the Battlescape (one per unit tile).</summary>
	public List<RuleItem> CorpseBattlescape => _corpseBattle;

	/// <summary>Gets the self destruct item (or the Geoscape corpse item if it has power).</summary>
	public RuleItem SelfDestructItem => _selfDestructItem ?? (_corpseGeo != null && _corpseGeo.Power > 0 ? _corpseGeo : null);

	/// <summary>Gets the storage item needed to equip this (null: infinite supply).</summary>
	public RuleItem StoreItem => _storeItem;

	/// <summary>Gets the type of special weapon.</summary>
	public RuleItem SpecialWeapon => _specWeapon;

	/// <summary>Gets the research required to be able to equip this armor.</summary>
	public RuleResearch RequiredResearch => _requires;

	/// <summary>Gets the commendation required to be able to equip this armor.</summary>
	public RuleCommendations RequiredAward => _requiresAward;

	/// <summary>Gets the soldier bonus required to be able to equip this armor.</summary>
	public RuleSoldierBonus RequiredBonus => _requiresBonus;

	/// <summary>Armor have layered armor definition. Check by Prefix.</summary>
	public bool HasLayersDefinition => _layersDefaultPrefix.Length != 0;

	/// <summary>Gets the default prefix for layered armor sprite names.</summary>
	public string LayersDefaultPrefix => _layersDefaultPrefix;

	/// <summary>Gets the overrides for layered armor sprite name prefix, per layer.</summary>
	public SortedDictionary<int, string> LayersSpecificPrefix => _layersSpecificPrefix;

	/// <summary>Gets the layered armor definition.</summary>
	public SortedDictionary<string, List<string>> LayersDefinition => _layersDefinition;

	/// <summary>Gets the drawing routine ID.</summary>
	public int DrawingRoutine => _drawingRoutine;

	/// <summary>Gets whether or not to draw bubbles (breathing animation).</summary>
	public bool DrawBubbles() => _drawBubbles;

	/// <summary>
	/// Gets the movement type of this armor. Useful for determining whether the armor can fly.
	/// DO NOT USE THIS OUTSIDE THE BATTLEUNIT CONSTRUCTOR.
	/// </summary>
	public MovementType MovementType => _movementType;

	/// <summary>Gets the MovementType based on the depth of the battle.</summary>
	public MovementType GetMovementTypeByDepth(int depth)
	{
		if (_movementType == MovementType.MT_FLOAT)
		{
			if (depth > 0)
			{
				return MovementType.MT_FLY;
			}
			else
			{
				return MovementType.MT_WALK;
			}
		}
		else if (_movementType == MovementType.MT_SINK)
		{
			if (depth == 0)
			{
				return MovementType.MT_FLY;
			}
			else
			{
				return MovementType.MT_WALK;
			}
		}
		else
		{
			return _movementType;
		}
	}

	/// <summary>Gets the armor's special ability.</summary>
	public int SpecialAbility => (int)_specab;

	/// <summary>Gets weight value as hostile unit (null: not defined).</summary>
	public AIAttackWeight? AITargetWeightAsHostile => _aiTargetWeightAsHostile;

	/// <summary>Gets weight value as civilian unit when consider by aliens (null: not defined).</summary>
	public AIAttackWeight? AITargetWeightAsHostileCivilians => _aiTargetWeightAsHostileCivilians;

	/// <summary>Gets weight value as same faction unit (null: not defined).</summary>
	public AIAttackWeight? AITargetWeightAsFriendly => _aiTargetWeightAsFriendly;

	/// <summary>Gets weight value as neutral unit (xcom to civ or vice versa) (null: not defined).</summary>
	public AIAttackWeight? AITargetWeightAsNeutral => _aiTargetWeightAsNeutral;

	/// <summary>Should turning before first step cost TU or not?</summary>
	public bool TurnBeforeFirstStep => _turnBeforeFirstStep;

	/// <summary>Gets the turn cost.</summary>
	public int TurnCost => _turnCost;

	/// <summary>Multiplier of all move costs.</summary>
	public ArmorMoveCost MoveCostBase => _moveCostBase;

	/// <summary>Multiplier of normal base move cost.</summary>
	public ArmorMoveCost MoveCostBaseNormal => _moveCostBaseNormal;

	/// <summary>Multiplier of fly base move cost.</summary>
	public ArmorMoveCost MoveCostBaseFly => _moveCostBaseFly;

	/// <summary>Multiplier of climb base move cost.</summary>
	public ArmorMoveCost MoveCostBaseClimb => _moveCostBaseClimb;

	/// <summary>Multiplier of walk move cost.</summary>
	public ArmorMoveCost MoveCostWalk => _moveCostWalk;

	/// <summary>Multiplier of run move cost.</summary>
	public ArmorMoveCost MoveCostRun => _moveCostRun;

	/// <summary>Multiplier of strafe move cost.</summary>
	public ArmorMoveCost MoveCostStrafe => _moveCostStrafe;

	/// <summary>Multiplier of sneak move cost.</summary>
	public ArmorMoveCost MoveCostSneak => _moveCostSneak;

	/// <summary>Multiplier of fly "walk" move cost.</summary>
	public ArmorMoveCost MoveCostFlyWalk => _moveCostFlyWalk;

	/// <summary>Multiplier of fly "run" move cost.</summary>
	public ArmorMoveCost MoveCostFlyRun => _moveCostFlyRun;

	/// <summary>Multiplier of fly "strafe" cost.</summary>
	public ArmorMoveCost MoveCostFlyStrafe => _moveCostFlyStrafe;

	/// <summary>Multiplier of fly up move cost.</summary>
	public ArmorMoveCost MoveCostFlyUp => _moveCostFlyUp;

	/// <summary>Multiplier of fly down move cost.</summary>
	public ArmorMoveCost MoveCostFlyDown => _moveCostFlyDown;

	/// <summary>Multiplier of climb up move cost.</summary>
	public ArmorMoveCost MoveCostClimbUp => _moveCostClimbUp;

	/// <summary>Multiplier of climb down move cost.</summary>
	public ArmorMoveCost MoveCostClimbDown => _moveCostClimbDown;

	/// <summary>Multiplier of moving using GravLift cost.</summary>
	public ArmorMoveCost MoveCostGravLift => _moveCostGravLift;

	/// <summary>Gets the move sound id. Overrides default/unit's move sound. To be used in BattleUnit constructors only too!</summary>
	public int MoveSound => _moveSound;

	/// <summary>Gets the male death sounds.</summary>
	public List<int> MaleDeathSounds => _deathSoundMale;

	/// <summary>Gets the female death sounds.</summary>
	public List<int> FemaleDeathSounds => _deathSoundFemale;

	/// <summary>Gets the male "select unit" sounds.</summary>
	public List<int> MaleSelectUnitSounds => _selectUnitSoundMale;

	/// <summary>Gets the female "select unit" sounds.</summary>
	public List<int> FemaleSelectUnitSounds => _selectUnitSoundFemale;

	/// <summary>Gets the male "start moving" sounds.</summary>
	public List<int> MaleStartMovingSounds => _startMovingSoundMale;

	/// <summary>Gets the female "start moving" sounds.</summary>
	public List<int> FemaleStartMovingSounds => _startMovingSoundFemale;

	/// <summary>Gets the male "select weapon" sounds.</summary>
	public List<int> MaleSelectWeaponSounds => _selectWeaponSoundMale;

	/// <summary>Gets the female "select weapon" sounds.</summary>
	public List<int> FemaleSelectWeaponSounds => _selectWeaponSoundFemale;

	/// <summary>Gets the male "annoyed" sounds.</summary>
	public List<int> MaleAnnoyedSounds => _annoyedSoundMale;

	/// <summary>Gets the female "annoyed" sounds.</summary>
	public List<int> FemaleAnnoyedSounds => _annoyedSoundFemale;

	/// <summary>Gets the size of the unit. Normally this is 1 (small) or 2 (big).</summary>
	public int Size => _size;

	/// <summary>Gets the total size of the unit. Normally this is 1 for small or 4 for big.</summary>
	public int TotalSize => _size * _size;

	/// <summary>Gets the damage modifier for a certain damage type.</summary>
	public float GetDamageModifier(ItemDamageType dt) => _damageModifier[(int)dt];

	/// <summary>Gets the raw damage modifiers (a copy).</summary>
	public List<float> DamageModifiersRaw
	{
		get
		{
			var result = new List<float>();
			for (int i = 0; i < (int)ItemDamageType.DAMAGE_TYPES; i++)
			{
				result.Add(_damageModifier[i]);
			}
			return result;
		}
	}

	/// <summary>Gets the loftempSet.</summary>
	public List<int> LoftempsSet => _loftempsSet;

	/// <summary>Gets pointer to the armor's stats (a copy here, UnitStats is a value type).</summary>
	public UnitStats Stats => _stats;

	/// <summary>Gets unit psi defense.</summary>
	public int GetPsiDefence(BattleUnit unit) => _psiDefence.GetBonus(unit);

	public RuleStatBonus PsiDefenceRaw => _psiDefence;

	/// <summary>Gets unit melee dodge chance.</summary>
	public int GetMeleeDodge(BattleUnit unit) => _meleeDodge.GetBonus(unit);

	public RuleStatBonus MeleeDodgeRaw => _meleeDodge;

	/// <summary>Gets unit dodge penalty if hit from behind.</summary>
	public float MeleeDodgeBackPenalty => _meleeDodgeBackPenalty;

	/// <summary>Gets unit TU recovery.</summary>
	public int GetTimeRecovery(BattleUnit unit, int externalBonuses) => _timeRecovery.GetBonus(unit, externalBonuses);

	public RuleStatBonus TimeRecoveryRaw => _timeRecovery;

	/// <summary>Gets unit Energy recovery.</summary>
	public int GetEnergyRecovery(BattleUnit unit, int externalBonuses) => _energyRecovery.GetBonus(unit, externalBonuses);

	public RuleStatBonus EnergyRecoveryRaw => _energyRecovery;

	/// <summary>Gets unit Morale recovery.</summary>
	public int GetMoraleRecovery(BattleUnit unit, int externalBonuses) => _moraleRecovery.GetBonus(unit, externalBonuses);

	public RuleStatBonus MoraleRecoveryRaw => _moraleRecovery;

	/// <summary>Gets unit Health recovery.</summary>
	public int GetHealthRecovery(BattleUnit unit, int externalBonuses) => _healthRecovery.GetBonus(unit, externalBonuses);

	public RuleStatBonus HealthRecoveryRaw => _healthRecovery;

	/// <summary>Gets unit Mana recovery.</summary>
	public int GetManaRecovery(BattleUnit unit, int externalBonuses) => _manaRecovery.GetBonus(unit, externalBonuses);

	public RuleStatBonus ManaRecoveryRaw => _manaRecovery;

	/// <summary>Gets unit Stun recovery.</summary>
	public int GetStunRegeneration(BattleUnit unit, int externalBonuses) => _stunRecovery.GetBonus(unit, externalBonuses);

	public RuleStatBonus StunRegenerationRaw => _stunRecovery;

	/// <summary>Gets the armor's weight.</summary>
	public int Weight => _weight;

	/// <summary>Gets number of death frames.</summary>
	public int DeathFrames => _deathFrames;

	/// <summary>Gets if armor uses constant animation.</summary>
	public bool ConstantAnimation => _constantAnimation;

	/// <summary>Checks if this armor ignores gender (power suit/flying suit).</summary>
	public ForcedTorso ForcedTorso => _forcedTorso;

	/// <summary>Gets built-in weapons of armor.</summary>
	public List<RuleItem> BuiltInWeapons => _builtInWeapons;

	/// <summary>Gets max view distance at dark in BattleScape.</summary>
	public int VisibilityAtDark => _visibilityAtDark;

	/// <summary>Gets max view distance at day in BattleScape.</summary>
	public int VisibilityAtDay => _visibilityAtDay;

	/// <summary>Gets info about camouflage at day.</summary>
	public int CamouflageAtDay => _camouflageAtDay;

	/// <summary>Gets info about camouflage at dark.</summary>
	public int CamouflageAtDark => _camouflageAtDark;

	/// <summary>Gets info about anti camouflage at day.</summary>
	public int AntiCamouflageAtDay => _antiCamouflageAtDay;

	/// <summary>Gets info about anti camouflage at dark.</summary>
	public int AntiCamouflageAtDark => _antiCamouflageAtDark;

	/// <summary>Gets info about heat vision.</summary>
	public int VisibilityThroughSmoke => _visibilityThroughSmoke;

	/// <summary>Gets info about visibility through fire.</summary>
	public int VisibilityThroughFire => _visibilityThroughFire;

	/// <summary>Gets info about psi vision.</summary>
	public int PsiVision => _psiVision;

	/// <summary>Gets info about psi camouflage.</summary>
	public int PsiCamouflage => _psiCamouflage;

	/// <summary>Gets personal soldier light radius.</summary>
	public int PersonalLightFriend => _personalLightFriend;

	/// <summary>Gets personal alien light radius.</summary>
	public int PersonalLightHostile => _personalLightHostile;

	/// <summary>Gets personal civilian light radius.</summary>
	public int PersonalLightNeutral => _personalLightNeutral;

	/// <summary>Gets if unit should be always visible.</summary>
	public bool IsAlwaysVisible => _isAlwaysVisible;

	/// <summary>Gets how armor react to fear.</summary>
	public bool GetFearImmune(bool def = false) => UseBoolNullable(_fearImmune, def);

	/// <summary>Gets how armor react to bleeding.</summary>
	public bool GetBleedImmune(bool def = false) => UseBoolNullable(_bleedImmune, def);

	/// <summary>Gets how armor react to inflicted pain.</summary>
	public bool GetPainImmune(bool def = false) => UseBoolNullable(_painImmune, def);

	/// <summary>Gets how armor react to zombification.</summary>
	public bool GetZombiImmune(bool def = false) => UseBoolNullable(_zombiImmune, def);

	/// <summary>Gets whether or not this unit ignores close quarters threats.</summary>
	public bool GetIgnoresMeleeThreat(bool def = false) => UseBoolNullable(_ignoresMeleeThreat, def);

	/// <summary>Gets whether or not this unit is a close quarters threat.</summary>
	public bool GetCreatesMeleeThreat(bool def = true) => UseBoolNullable(_createsMeleeThreat, def);

	/// <summary>Gets how much damage (over the maximum HP) is needed to vaporize/disintegrate a unit.</summary>
	public float OverKill => _overKill;

	/// <summary>Gets face base color.</summary>
	public int FaceColorGroup => _faceColorGroup;

	/// <summary>Gets hair base color.</summary>
	public int HairColorGroup => _hairColorGroup;

	/// <summary>Gets utile base color.</summary>
	public int UtileColorGroup => _utileColorGroup;

	/// <summary>Gets rank base color.</summary>
	public int RankColorGroup => _rankColorGroup;

	/// <summary>
	/// Helper function finding value in vector with fallback if vector is shorter.
	/// If pos == 31 then we test for 31, 15, 7; if pos == 36 then we test for 36, 4;
	/// we stop on p &lt; 8 for compatibility reasons.
	/// </summary>
	internal static int FindWithFallback(List<int> vec, int pos)
	{
		for (int i = 0; i <= RuleSoldier.LookVariantBits; ++i)
		{
			int p = pos & (RuleSoldier.LookTotalMask >> i);
			if (p < vec.Count)
			{
				return vec[p];
			}
		}
		return 0;
	}

	/// <summary>Gets new face colors for replacement, if 0 then don't replace colors.</summary>
	public int GetFaceColor(int i) => FindWithFallback(_faceColor, i);

	public List<int> FaceColorRaw => _faceColor;

	/// <summary>Gets new hair colors for replacement, if 0 then don't replace colors.</summary>
	public int GetHairColor(int i) => FindWithFallback(_hairColor, i);

	public List<int> HairColorRaw => _hairColor;

	/// <summary>Gets new utile colors for replacement, if 0 then don't replace colors.</summary>
	public int GetUtileColor(int i) => FindWithFallback(_utileColor, i);

	public List<int> UtileColorRaw => _utileColor;

	/// <summary>Gets new rank colors for replacement, if 0 then don't replace colors.</summary>
	public int GetRankColor(int i) => FindWithFallback(_rankColor, i);

	public List<int> RankColorRaw => _rankColor;

	/// <summary>Can this unit's inventory be accessed for any reason?</summary>
	public bool HasInventory => _hasInventory;

	/// <summary>Gets the list of units this armor applies to (empty = applies to all).</summary>
	public List<RuleSoldier> UnitsRaw => _units;

	/// <summary>Gets the armor's supported soldier ranks (sorted).</summary>
	public List<int> RanksRaw => _ranks;

	/// <summary>Check if a soldier can use this armor.</summary>
	public bool GetCanBeUsedBy(Soldier soldier)
	{
		ArgumentNullException.ThrowIfNull(soldier);
		if (_units.Count != 0)
		{
			if (!_units.Contains(soldier.Rules))
			{
				return false;
			}
		}
		if (_ranks.Count != 0)
		{
			int rankInt = (int)soldier.Rank;
			if (_ranks.BinarySearch(rankInt) < 0)
			{
				return false;
			}
		}
		if (_requiresAward != null)
		{
			if (!soldier.Diary.ContainsCommendation(_requiresAward))
			{
				return false;
			}
		}
		if (_requiresBonus != null)
		{
			if (!soldier.HasBonus(_requiresBonus))
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>Gets the index of the sprite in the CustomArmorPreview sprite set.</summary>
	public List<int> CustomArmorPreviewIndex => _customArmorPreviewIndex;

	/// <summary>Can you run while wearing this armor?</summary>
	public bool AllowsRunning(bool def = true) => UseBoolNullable(_allowsRunning, def);

	/// <summary>Can you strafe while wearing this armor?</summary>
	public bool AllowsStrafing(bool def = true) => UseBoolNullable(_allowsStrafing, def);

	/// <summary>Can you sneak while wearing this armor?</summary>
	public bool AllowsSneaking(bool def = true) => UseBoolNullable(_allowsSneaking, def);

	/// <summary>Can you kneel while wearing this armor?</summary>
	public bool AllowsKneeling(bool def = true) => UseBoolNullable(_allowsKneeling, def);

	/// <summary>Can you move while wearing this armor? (a "null" value (-1) counts as true, like the C++ Sint8 to bool conversion)</summary>
	public bool AllowsMoving() => _allowsMoving != 0;

	/// <summary>Does this armor count for pilot bonuses?</summary>
	public bool IsPilotArmor => _isPilotArmor;

	/// <summary>Does this armor allow two main weapons during autoequip?</summary>
	public bool AllowTwoMainWeapons => _allowTwoMainWeapons;

	/// <summary>Does this armor instantly recover any wounds after the battle?</summary>
	public bool InstantWoundRecovery => _instantWoundRecovery;

	/// <summary>Gets a unit's height when standing while wearing this armor.</summary>
	public int StandHeight => _standHeight;

	/// <summary>Gets a unit's height when kneeling while wearing this armor.</summary>
	public int KneelHeight => _kneelHeight;

	/// <summary>Gets a unit's float elevation while wearing this armor.</summary>
	public int FloatHeight => _floatHeight;

	/// <summary>Gets a unit's offset for melee attacks.</summary>
	public int MeleeOriginVoxelVerticalOffset => _meleeOriginVoxelVerticalOffset;

	/// <summary>Gets the armor type group.</summary>
	public int Group => _group;

	/// <summary>Gets the list weight for this armor.</summary>
	public int ListOrder => _listOrder;
}
