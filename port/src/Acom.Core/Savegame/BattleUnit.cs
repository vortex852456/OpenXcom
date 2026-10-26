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
// Ported from: src/Savegame/BattleUnit.h, src/Savegame/BattleUnit.cpp
#nullable disable

using static Acom.SoldierGender;
using static Acom.SoldierRank;
using static Acom.SpecialAbility;
using static Acom.UnitBodyPart;
using static Acom.UnitFaction;
using static Acom.UnitSide;
using static Acom.UnitStatus;

namespace Acom;

/// <summary>Placeholder class for future functionality.</summary>
public class BattleUnitVisibility
{
	/// <summary>Name of class used in script.</summary>
	public const string ScriptName = "BattleUnitVisibility";
}

/// <summary>
/// Represents a moving unit in the battlescape, player controlled or AI controlled
/// it holds info about it's position, items carrying, stats, etc
/// </summary>
/// <remarks>
/// The class is split into partial files: construction and YAML (this file), movement, combat,
/// inventory and remaining state; the script binding (<c>ScriptRegister</c>, <c>ScriptFill</c>) is in
/// <c>BattleUnit.Script.cs</c>.
/// </remarks>
public partial class BattleUnit
{
	/// <summary>User interface string identifier of body parts.</summary>
	public static readonly string[] PARTS_STRING =
	[
		"STR_HEAD",
		"STR_TORSO",
		"STR_RIGHT_ARM",
		"STR_LEFT_ARM",
		"STR_RIGHT_LEG",
		"STR_LEFT_LEG",
	];

	private const int SPEC_WEAPON_MAX = 4;

	public const int MAX_SOLDIER_ID = 1000000;
	public const int BUBBLES_FIRST_FRAME = 3;
	public const int BUBBLES_LAST_FRAME = BUBBLES_FIRST_FRAME + 15;

	/// <summary>Name of class used in script.</summary>
	public const string ScriptName = "BattleUnit";

	private UnitFaction _faction, _originalFaction;
	private UnitFaction _killedBy;
	private UnitFaction _spawnUnitFaction = FACTION_HOSTILE;
	private int _id;
	private Position _pos;
	private Tile _tile;
	private Position _lastPos;
	private int _direction, _toDirection;
	private int _directionTurret, _toDirectionTurret;
	private int _verticalDirection;
	private Position _destination;
	private UnitStatus _status;
	private bool _wantsToSurrender, _isSurrendering;
	private int _walkPhase, _fallPhase;
	private readonly List<BattleUnit> _visibleUnits = [], _unitsSpottedThisTurn = [];
	private readonly List<Tile> _visibleTiles = [];
	private readonly HashSet<Tile> _visibleTilesLookup = new(ReferenceEqualityComparer.Instance);
	private int _tu, _energy, _health, _morale, _stunlevel, _mana;
	private bool _kneeled, _floating, _dontReselect, _aiMedikitUsed;
	private bool _haveNoFloorBelow;
	private readonly int[] _currentArmor = new int[(int)SIDE_MAX], _maxArmor = new int[(int)SIDE_MAX];
	private readonly int[] _fatalWounds = new int[(int)BODYPART_MAX];
	private int _fire;
	private readonly List<BattleItem> _inventory = [];
	private readonly BattleItem[] _specWeapon = new BattleItem[SPEC_WEAPON_MAX];
	private AIModule _currentAIState;
	private bool _visible;
	private UnitStats _exp, _expTmp;
	private int _motionPoints;
	private int _scannedTurn;
	private int _customMarker;
	private int _kills;
	private int _faceDirection; // used only during strafing moves
	private List<int> _meleeAttackedBy = [];
	private bool _hitByFire, _hitByAnything, _alreadyExploded;
	private int _fireMaxHit;
	private int _smokeMaxHit;
	private int _moraleRestored;
	private int _notificationShown;
	private BattleUnit _charging;

	private readonly byte[] _turnsSinceSpotted = [255, 255, 255];
	private readonly byte[] _turnsLeftSpottedForSnipers = [0, 0, 0];
	private byte _turnsSinceStunned = 255;

	private BattleUnit _previousOwner;
	private Unit _spawnUnit;
	private string _activeHand = "";
	private string _preferredHandForReactions = "";
	private bool _reactionsDisabledForLeftHand;
	private bool _reactionsDisabledForRightHand;
	private BattleUnitStatistics _statistics;
	private int _murdererId; // used to credit the murderer with the kills that this unit got by blowing up on death
	private int _mindControllerID; // used to credit the mind controller with the kills of the mind controllee
	private UnitSide _fatalShotSide;
	private UnitBodyPart _fatalShotBodyPart;
	private string _murdererWeapon = "", _murdererWeaponAmmo = "";

	// static data
	private string _type = "";
	private string _rank = "";
	private string _race = "";
	private string _name = "";
	private UnitStats _stats;
	private int _standHeight, _kneelHeight, _floatHeight;
	private int _lastReloadSound;
	private List<int> _deathSound = [], _aggroSound = [];
	private List<int> _selectUnitSound = [], _startMovingSound = [], _selectWeaponSound = [], _annoyedSound = [];
	private int _value, _moveSound;
	private int _intelligence, _aggression;
	private int _maxViewDistanceAtDark, _maxViewDistanceAtDay;
	private int _maxViewDistanceAtDarkSquared;
	private int _psiVision;
	private int _visibilityThroughSmoke;
	private int _visibilityThroughFire = 100;
	private SpecialAbility _specab;
	private Armor _armor;
	private SoldierGender _gender;
	private readonly Soldier _geoscapeSoldier;
	private List<int> _loftempsSet = [];
	private readonly Unit _unitRules;
	private int _rankInt;
	private int _rankIntUnified;
	private int _turretType;
	private int _breathFrame;
	private bool _breathing;
	private bool _hidingForTurn, _floorAbove, _respawn, _alreadyRespawned;
	private bool _isLeeroyJenkins; // always charges enemy, never retreats.
	private bool _summonedPlayerUnit, _resummonedFakeCivilian;
	private bool _pickUpWeaponsMoreActively;
	private bool _disableIndicators;
	private MovementType _movementType;
	private MovementType _originalMovementType;
	private ArmorMoveCost _moveCostBase;
	private ArmorMoveCost _moveCostBaseFly;
	private ArmorMoveCost _moveCostBaseClimb;
	private ArmorMoveCost _moveCostBaseNormal;
	private readonly List<(byte first, byte second)> _recolor = [];
	private bool _capturable;
	private bool _vip;
	private bool _bannedInNextStage;
	private bool _skillMenuCheck;

	/// <summary>Position of the last cover found by the AI.</summary>
	public Position lastCover;

	/// <summary>
	/// The member initializer list shared by both C++ constructors, plus the body assignments that do not
	/// depend on the rules (C++ assigns them in both constructor bodies). Used directly by tests.
	/// </summary>
	/// <param name="faction">Faction of the unit (also original faction and "killed by").</param>
	/// <param name="id">Unique unit ID.</param>
	/// <param name="armor">Armor rules (null for soldiers, set later by <see cref="UpdateArmorFromSoldier"/>).</param>
	/// <param name="geoscapeSoldier">Geoscape soldier, or null.</param>
	/// <param name="unitRules">Alien/HWP/civilian rules, or null.</param>
	/// <param name="statistics">Mission statistics (C++ allocates them in the constructor body).</param>
	internal BattleUnit(UnitFaction faction, int id, Armor armor, Soldier geoscapeSoldier, Unit unitRules, BattleUnitStatistics statistics)
	{
		_faction = faction;
		_originalFaction = faction;
		_killedBy = faction;
		_id = id;
		_status = STATUS_STANDING;
		_scannedTurn = -1;
		_fatalShotSide = SIDE_FRONT;
		_fatalShotBodyPart = BODYPART_HEAD;
		_armor = armor;
		_geoscapeSoldier = geoscapeSoldier;
		_unitRules = unitRules;
		_turretType = -1;
		_capturable = true; // only the soldier constructor initializes it, the unit constructor sets it from the rules
		_statistics = statistics;

		_faceDirection = -1;
		_activeHand = "STR_RIGHT_HAND";
		_preferredHandForReactions = "";
		lastCover = TileEngine.invalid;
	}

	/// <summary>Initializes a BattleUnit from a Soldier.</summary>
	/// <param name="mod">Mod rules.</param>
	/// <param name="soldier">Pointer to the Soldier.</param>
	/// <param name="depth">The depth of the battlefield (used to determine movement type in case of MT_FLOAT).</param>
	/// <param name="sc">Starting condition of the mission, or null.</param>
	public BattleUnit(Mod mod, Soldier soldier, int depth, RuleStartingCondition sc)
		: this(FACTION_PLAYER, 0, null, soldier, null, new BattleUnitStatistics())
	{
		_name = soldier.GetName(true);
		_id = soldier.Id;

		_type = "SOLDIER";
		_rank = soldier.RankString;
		_gender = soldier.Gender;
		_intelligence = 2;
		_aggression = 1;
		_faceDirection = -1;
		_floorAbove = false;
		_breathing = false;

		int rankbonus = soldier.Rank switch
		{
			RANK_SERGEANT => 1,
			RANK_CAPTAIN => 3,
			RANK_COLONEL => 6,
			RANK_COMMANDER => 10,
			_ => 0,
		};

		_value = soldier.Rules.Value + soldier.Missions + rankbonus;

		DeriveSoldierRank();

		UpdateArmorFromSoldier(mod, soldier, soldier.Armor, depth, false, sc);

		// soldier bonus cache was built above in updateArmorFromSoldier(), so we can also calculate this now
		if (_geoscapeSoldier != null)
		{
			foreach (var skill in _geoscapeSoldier.Rules.Skills)
			{
				if (_geoscapeSoldier.HasAllRequiredBonusesForSkill(skill)
					&& (skill.Cost.Time > 0 || skill.Cost.Mana > 0)
					&& (!skill.IsPsiRequired || BaseStats.psiSkill > 0))
				{
					_skillMenuCheck = true;
					break;
				}
			}
		}
	}

	/// <summary>Updates BattleUnit's armor and related attributes (after a change/transformation of armor).</summary>
	/// <param name="mod">Mod rules.</param>
	/// <param name="soldier">Pointer to the Geoscape Soldier.</param>
	/// <param name="ruleArmor">Pointer to the new Armor ruleset.</param>
	/// <param name="depth">The depth of the battlefield.</param>
	/// <param name="nextStage">Keep current health/mana (transition between mission stages).</param>
	/// <param name="sc">Starting condition of the mission, or null.</param>
	public void UpdateArmorFromSoldier(Mod mod, Soldier soldier, Armor ruleArmor, int depth, bool nextStage, RuleStartingCondition sc)
	{
		_armor = ruleArmor;

		_standHeight = _armor.StandHeight == -1 ? soldier.Rules.StandHeight : _armor.StandHeight;
		_kneelHeight = _armor.KneelHeight == -1 ? soldier.Rules.KneelHeight : _armor.KneelHeight;
		_floatHeight = _armor.FloatHeight == -1 ? soldier.Rules.FloatHeight : _armor.FloatHeight;
		_loftempsSet = new List<int>(_armor.LoftempsSet);

		_specab = (SpecialAbility)_armor.SpecialAbility;

		_originalMovementType = _movementType = _armor.GetMovementTypeByDepth(depth);
		_moveCostBase = _armor.MoveCostBase;
		_moveCostBaseFly = _armor.MoveCostBaseFly;
		_moveCostBaseClimb = _armor.MoveCostBaseClimb;
		_moveCostBaseNormal = _armor.MoveCostBaseNormal;

		_stats = soldier.CurrentStats;
		// armor and soldier bonuses may modify effective stats
		{
			soldier.PrepareStatsWithBonuses(mod); // refresh needed, because of armor stats
			_stats = soldier.StatsWithAllBonuses;
		}

		int visibilityDarkBonus = 0;
		int visibilityDayBonus = 0;
		int psiVision = 0;
		int bonusVisibilityThroughSmoke = 0;
		int bonusVisibilityThroughFire = 0;
		foreach (var bonusRule in soldier.GetBonuses(null))
		{
			visibilityDarkBonus += bonusRule.VisibilityAtDark;
			visibilityDayBonus += bonusRule.VisibilityAtDay;
			psiVision += bonusRule.PsiVision;
			bonusVisibilityThroughSmoke += bonusRule.VisibilityThroughSmoke;
			bonusVisibilityThroughFire += bonusRule.VisibilityThroughFire;
		}
		_maxViewDistanceAtDark = _armor.VisibilityAtDark != 0 ? _armor.VisibilityAtDark : 9;
		_maxViewDistanceAtDark = Clamp(_maxViewDistanceAtDark + visibilityDarkBonus, 1, mod.MaxViewDistance);
		_maxViewDistanceAtDarkSquared = _maxViewDistanceAtDark * _maxViewDistanceAtDark;
		_maxViewDistanceAtDay = _armor.VisibilityAtDay != 0 ? _armor.VisibilityAtDay : mod.MaxViewDistance;
		_maxViewDistanceAtDay = Clamp(_maxViewDistanceAtDay + visibilityDayBonus, 1, mod.MaxViewDistance);
		_psiVision = _armor.PsiVision + psiVision;
		_visibilityThroughSmoke = _armor.VisibilityThroughSmoke + bonusVisibilityThroughSmoke;
		_visibilityThroughFire = _armor.VisibilityThroughFire + bonusVisibilityThroughFire;

		_maxArmor[(int)SIDE_FRONT] = _armor.FrontArmor;
		_maxArmor[(int)SIDE_LEFT] = _armor.LeftSideArmor;
		_maxArmor[(int)SIDE_RIGHT] = _armor.RightSideArmor;
		_maxArmor[(int)SIDE_REAR] = _armor.RearArmor;
		_maxArmor[(int)SIDE_UNDER] = _armor.UnderArmor;
		{
			foreach (var bonusRule in soldier.GetBonuses(null))
			{
				_maxArmor[(int)SIDE_FRONT] += bonusRule.FrontArmor;
				_maxArmor[(int)SIDE_LEFT] += bonusRule.LeftSideArmor;
				_maxArmor[(int)SIDE_RIGHT] += bonusRule.RightSideArmor;
				_maxArmor[(int)SIDE_REAR] += bonusRule.RearArmor;
				_maxArmor[(int)SIDE_UNDER] += bonusRule.UnderArmor;
			}
			_maxArmor[(int)SIDE_FRONT] = Math.Max(0, _maxArmor[(int)SIDE_FRONT]);
			_maxArmor[(int)SIDE_LEFT] = Math.Max(0, _maxArmor[(int)SIDE_LEFT]);
			_maxArmor[(int)SIDE_RIGHT] = Math.Max(0, _maxArmor[(int)SIDE_RIGHT]);
			_maxArmor[(int)SIDE_REAR] = Math.Max(0, _maxArmor[(int)SIDE_REAR]);
			_maxArmor[(int)SIDE_UNDER] = Math.Max(0, _maxArmor[(int)SIDE_UNDER]);
		}
		_currentArmor[(int)SIDE_FRONT] = _maxArmor[(int)SIDE_FRONT];
		_currentArmor[(int)SIDE_LEFT] = _maxArmor[(int)SIDE_LEFT];
		_currentArmor[(int)SIDE_RIGHT] = _maxArmor[(int)SIDE_RIGHT];
		_currentArmor[(int)SIDE_REAR] = _maxArmor[(int)SIDE_REAR];
		_currentArmor[(int)SIDE_UNDER] = _maxArmor[(int)SIDE_UNDER];

		_breathFrame = _armor.DrawBubbles() ? 0 : -1;

		_tu = _stats.tu;
		_energy = _stats.stamina;
		if (nextStage)
		{
			_health = Math.Min(_health, (int)_stats.health);
			_mana = Math.Min(_mana, (int)_stats.mana);
		}
		else
		{
			_health = Math.Max(1, _stats.health - soldier.HealthMissing);
			_mana = Math.Max(0, _stats.mana - soldier.ManaMissing);
			_morale = 100;
			_stunlevel = 0;

			// wounded soldiers (defending the base) start with lowered morale
			{
				if (soldier.IsWounded)
				{
					_morale = 75;
					_health = Math.Max(1, _health - soldier.WoundRecoveryInt);
				}
			}
		}

		int look = (int)soldier.Gender + 2 * (int)soldier.Look + 8 * soldier.LookVariant;
		SetRecolor(look, look, _rankIntUnified);

		PrepareUnitSounds();
		PrepareUnitResponseSounds(mod);
		PrepareBannedFlag(sc);
	}

	/// <summary>Helper function preparing unit sounds.</summary>
	private void PrepareUnitSounds()
	{
		_lastReloadSound = Mod.ITEM_RELOAD;

		if (_geoscapeSoldier != null)
		{
			_aggroSound.Clear();
			_moveSound = _armor.MoveSound != Mod.NO_SOUND ? _armor.MoveSound : Mod.NO_SOUND; // there's no soldier move sound, thus hardcoded -1
		}
		else if (_unitRules != null)
		{
			_aggroSound = new List<int>(_unitRules.AggroSounds);
			_moveSound = _armor.MoveSound != Mod.NO_SOUND ? _armor.MoveSound : _unitRules.MoveSound;
		}

		// lower priority: soldier type / unit type
		if (_geoscapeSoldier != null)
		{
			var soldierRules = _geoscapeSoldier.Rules;
			if (_gender == GENDER_MALE)
				_deathSound = new List<int>(soldierRules.MaleDeathSounds);
			else
				_deathSound = new List<int>(soldierRules.FemaleDeathSounds);
		}
		else if (_unitRules != null)
		{
			_deathSound = new List<int>(_unitRules.DeathSounds);
		}

		// higher priority: armor
		if (_gender == GENDER_MALE)
		{
			if (_armor.MaleDeathSounds.Count != 0)
				_deathSound = new List<int>(_armor.MaleDeathSounds);
		}
		else
		{
			if (_armor.FemaleDeathSounds.Count != 0)
				_deathSound = new List<int>(_armor.FemaleDeathSounds);
		}
	}

	/// <summary>Helper function preparing unit response sounds.</summary>
	private void PrepareUnitResponseSounds(Mod mod)
	{
		if (!mod.EnableUnitResponseSounds)
			return;

		// custom sounds by soldier name
		bool custom = false;
		if (mod.SelectUnitSounds.TryGetValue(_name, out var selectUnit))
		{
			custom = true;
			_selectUnitSound = new List<int>(selectUnit);
		}
		if (mod.StartMovingSounds.TryGetValue(_name, out var startMoving))
		{
			custom = true;
			_startMovingSound = new List<int>(startMoving);
		}
		if (mod.SelectWeaponSounds.TryGetValue(_name, out var selectWeapon))
		{
			custom = true;
			_selectWeaponSound = new List<int>(selectWeapon);
		}
		if (mod.AnnoyedSounds.TryGetValue(_name, out var annoyed))
		{
			custom = true;
			_annoyedSound = new List<int>(annoyed);
		}

		if (custom)
			return;

		// lower priority: soldier type / unit type
		if (_geoscapeSoldier != null)
		{
			var soldierRules = _geoscapeSoldier.Rules;
			if (_gender == GENDER_MALE)
			{
				_selectUnitSound = new List<int>(soldierRules.MaleSelectUnitSounds);
				_startMovingSound = new List<int>(soldierRules.MaleStartMovingSounds);
				_selectWeaponSound = new List<int>(soldierRules.MaleSelectWeaponSounds);
				_annoyedSound = new List<int>(soldierRules.MaleAnnoyedSounds);
			}
			else
			{
				_selectUnitSound = new List<int>(soldierRules.FemaleSelectUnitSounds);
				_startMovingSound = new List<int>(soldierRules.FemaleStartMovingSounds);
				_selectWeaponSound = new List<int>(soldierRules.FemaleSelectWeaponSounds);
				_annoyedSound = new List<int>(soldierRules.FemaleAnnoyedSounds);
			}
		}
		else if (_unitRules != null)
		{
			_selectUnitSound = new List<int>(_unitRules.SelectUnitSounds);
			_startMovingSound = new List<int>(_unitRules.StartMovingSounds);
			_selectWeaponSound = new List<int>(_unitRules.SelectWeaponSounds);
			_annoyedSound = new List<int>(_unitRules.AnnoyedSounds);
		}

		// higher priority: armor
		if (_gender == GENDER_MALE)
		{
			if (_armor.MaleSelectUnitSounds.Count != 0)
				_selectUnitSound = new List<int>(_armor.MaleSelectUnitSounds);
			if (_armor.MaleStartMovingSounds.Count != 0)
				_startMovingSound = new List<int>(_armor.MaleStartMovingSounds);
			if (_armor.MaleSelectWeaponSounds.Count != 0)
				_selectWeaponSound = new List<int>(_armor.MaleSelectWeaponSounds);
			if (_armor.MaleAnnoyedSounds.Count != 0)
				_annoyedSound = new List<int>(_armor.MaleAnnoyedSounds);
		}
		else
		{
			if (_armor.FemaleSelectUnitSounds.Count != 0)
				_selectUnitSound = new List<int>(_armor.FemaleSelectUnitSounds);
			if (_armor.FemaleStartMovingSounds.Count != 0)
				_startMovingSound = new List<int>(_armor.FemaleStartMovingSounds);
			if (_armor.FemaleSelectWeaponSounds.Count != 0)
				_selectWeaponSound = new List<int>(_armor.FemaleSelectWeaponSounds);
			if (_armor.FemaleAnnoyedSounds.Count != 0)
				_annoyedSound = new List<int>(_armor.FemaleAnnoyedSounds);
		}
	}

	/// <summary>Helper function preparing the banned flag.</summary>
	private void PrepareBannedFlag(RuleStartingCondition sc)
	{
		_bannedInNextStage = false;
		if (sc != null && sc.ForbiddenArmorsInNextStage.Count != 0)
		{
			var bannedList = sc.ForbiddenArmorsInNextStage;
			if (bannedList.Contains(_armor))
			{
				_bannedInNextStage = true;
			}
		}
	}

	/// <summary>Initializes a BattleUnit from a Unit (non-player) object.</summary>
	/// <param name="mod">Mod rules.</param>
	/// <param name="unit">Pointer to Unit object.</param>
	/// <param name="faction">Which faction the units belongs to.</param>
	/// <param name="id">Unique unit ID.</param>
	/// <param name="enviro">Pointer to battle enviro effects.</param>
	/// <param name="armor">Pointer to unit Armor.</param>
	/// <param name="adjustment">Difficulty stat adjustment (applied to hostile units).</param>
	/// <param name="depth">The depth of the battlefield (used to determine movement type in case of MT_FLOAT).</param>
	/// <param name="sc">Starting condition of the mission, or null.</param>
	public BattleUnit(Mod mod, Unit unit, UnitFaction faction, int id, RuleEnviroEffects enviro, Armor armor, StatAdjustment adjustment, int depth, RuleStartingCondition sc)
		: this(faction, id, armor, null, unit, new BattleUnitStatistics())
	{
		if (enviro != null)
		{
			var newArmor = enviro.GetArmorTransformation(_armor);
			if (newArmor != null)
			{
				_armor = newArmor;
			}
		}
		_type = unit.Type;
		_rank = unit.Rank;
		_race = unit.Race;
		_gender = GENDER_MALE;
		_intelligence = unit.Intelligence;
		_aggression = unit.Aggression;
		_faceDirection = -1;
		_floorAbove = false;
		_breathing = false;

		_spawnUnit = unit.SpawnUnit;
		_capturable = unit.Capturable;
		_isLeeroyJenkins = unit.IsLeeroyJenkins;
		if (unit.PickUpWeaponsMoreActively != -1)
		{
			_pickUpWeaponsMoreActively = unit.PickUpWeaponsMoreActively != 0;
		}
		else
		{
			if (faction == FACTION_HOSTILE)
				_pickUpWeaponsMoreActively = mod.AIPickUpWeaponsMoreActively;
			else
				_pickUpWeaponsMoreActively = mod.AIPickUpWeaponsMoreActivelyCiv;
		}
		if (_unitRules != null && _unitRules.IsVIP)
		{
			_vip = true;
		}

		_value = unit.Value;

		if (_originalFaction == FACTION_HOSTILE)
		{
			DeriveHostileRank();
		}
		else if (_originalFaction == FACTION_NEUTRAL)
		{
			DeriveNeutralRank();
		}

		UpdateArmorFromNonSoldier(mod, _armor, depth, false, sc);

		if (_specab == SPECAB_NONE)
		{
			_specab = (SpecialAbility)unit.SpecialAbility;
		}

		if (_originalFaction == FACTION_HOSTILE)
		{
			AdjustStats(adjustment);
		}
	}

	/// <summary>Updates BattleUnit's armor and related attributes (after a change/transformation of armor).</summary>
	/// <param name="mod">Mod rules.</param>
	/// <param name="newArmor">Pointer to the new Armor ruleset.</param>
	/// <param name="depth">The depth of the battlefield.</param>
	/// <param name="nextStage">Keep current health/mana (transition between mission stages).</param>
	/// <param name="sc">Starting condition of the mission, or null.</param>
	public void UpdateArmorFromNonSoldier(Mod mod, Armor newArmor, int depth, bool nextStage, RuleStartingCondition sc)
	{
		_armor = newArmor;

		_standHeight = _armor.StandHeight == -1 ? _unitRules.StandHeight : _armor.StandHeight;
		_kneelHeight = _armor.KneelHeight == -1 ? _unitRules.KneelHeight : _armor.KneelHeight;
		_floatHeight = _armor.FloatHeight == -1 ? _unitRules.FloatHeight : _armor.FloatHeight;
		_loftempsSet = new List<int>(_armor.LoftempsSet);

		_specab = (SpecialAbility)_armor.SpecialAbility;

		_originalMovementType = _movementType = _armor.GetMovementTypeByDepth(depth);
		_moveCostBase = _armor.MoveCostBase;
		_moveCostBaseFly = _armor.MoveCostBaseFly;
		_moveCostBaseClimb = _armor.MoveCostBaseClimb;
		_moveCostBaseNormal = _armor.MoveCostBaseNormal;

		_stats = _unitRules.Stats;
		_stats += _armor.Stats; // armors may modify effective stats
		_stats = UnitStats.ObeyFixedMinimum(_stats); // don't allow to go into minus!

		_maxViewDistanceAtDark = _armor.VisibilityAtDark != 0 ? _armor.VisibilityAtDark : _originalFaction == FACTION_HOSTILE ? mod.MaxViewDistance : 9;
		_maxViewDistanceAtDarkSquared = _maxViewDistanceAtDark * _maxViewDistanceAtDark;
		_maxViewDistanceAtDay = _armor.VisibilityAtDay != 0 ? _armor.VisibilityAtDay : mod.MaxViewDistance;
		_psiVision = _armor.PsiVision;
		_visibilityThroughSmoke = _armor.VisibilityThroughSmoke;
		_visibilityThroughFire = _armor.VisibilityThroughFire;

		_maxArmor[(int)SIDE_FRONT] = _armor.FrontArmor;
		_maxArmor[(int)SIDE_LEFT] = _armor.LeftSideArmor;
		_maxArmor[(int)SIDE_RIGHT] = _armor.RightSideArmor;
		_maxArmor[(int)SIDE_REAR] = _armor.RearArmor;
		_maxArmor[(int)SIDE_UNDER] = _armor.UnderArmor;

		_currentArmor[(int)SIDE_FRONT] = _maxArmor[(int)SIDE_FRONT];
		_currentArmor[(int)SIDE_LEFT] = _maxArmor[(int)SIDE_LEFT];
		_currentArmor[(int)SIDE_RIGHT] = _maxArmor[(int)SIDE_RIGHT];
		_currentArmor[(int)SIDE_REAR] = _maxArmor[(int)SIDE_REAR];
		_currentArmor[(int)SIDE_UNDER] = _maxArmor[(int)SIDE_UNDER];

		// most aliens don't breathe per-se, that's exclusive to humanoids
		_breathFrame = _armor.DrawBubbles() ? 0 : -1;

		_tu = _stats.tu;
		_energy = _stats.stamina;
		if (nextStage)
		{
			_health = Math.Min(_health, (int)_stats.health);
			_mana = Math.Min(_mana, (int)_stats.mana);
		}
		else
		{
			_health = _stats.health;
			_mana = _stats.mana;
			_morale = 100;
			_stunlevel = 0;
		}

		SetRecolor(Rng.Seedless(0, 127), Rng.Seedless(0, 127), _rankIntUnified);

		PrepareUnitSounds();
		PrepareUnitResponseSounds(mod);
		PrepareBannedFlag(sc);
	}

	/// <summary>Loads the unit from YAML.</summary>
	/// <param name="node">YAML node.</param>
	/// <param name="mod">Mod rules (resolves the spawn unit).</param>
	public void Load(YamlNodeReader node, Mod mod)
	{
		var reader = node.UseIndex();
		reader.TryRead("id", ref _id);
		reader.TryRead("faction", ref _faction);
		reader.TryRead("status", ref _status);
		reader.TryRead("wantsToSurrender", ref _wantsToSurrender);
		reader.TryRead("isSurrendering", ref _isSurrendering);
		reader.TryRead("position", ref _pos);
		reader.TryRead("direction", ref _direction);
		_toDirection = _direction;
		reader.TryRead("directionTurret", ref _directionTurret);
		_toDirectionTurret = _directionTurret;
		reader.TryRead("tu", ref _tu);
		reader.TryRead("health", ref _health);
		reader.TryRead("mana", ref _mana);
		reader.TryRead("stunlevel", ref _stunlevel);
		reader.TryRead("energy", ref _energy);
		reader.TryRead("morale", ref _morale);
		reader.TryRead("kneeled", ref _kneeled);
		reader.TryRead("floating", ref _floating);
		for (int i = 0; i < (int)SIDE_MAX; i++)
			reader["armor"][i].TryReadVal(ref _currentArmor[i]);
		for (int i = 0; i < (int)BODYPART_MAX; i++)
			reader["fatalWounds"][i].TryReadVal(ref _fatalWounds[i]);
		reader.TryRead("fire", ref _fire);
		reader.TryRead("expBravery", ref _exp.bravery);
		reader.TryRead("expReactions", ref _exp.reactions);
		reader.TryRead("expFiring", ref _exp.firing);
		reader.TryRead("expThrowing", ref _exp.throwing);
		reader.TryRead("expPsiSkill", ref _exp.psiSkill);
		reader.TryRead("expPsiStrength", ref _exp.psiStrength);
		reader.TryRead("expMana", ref _exp.mana);
		reader.TryRead("expMelee", ref _exp.melee);
		reader.TryRead("currStats", ref _stats);
		reader.TryRead("turretType", ref _turretType);
		reader.TryRead("visible", ref _visible);

		TryReadAsInt(reader, "turnsSinceSpotted", ref _turnsSinceSpotted[(int)FACTION_HOSTILE]);
		TryReadAsInt(reader, "turnsLeftSpottedForSnipers", ref _turnsLeftSpottedForSnipers[(int)FACTION_HOSTILE]);
		TryReadAsInt(reader, "turnsSinceSpottedByXcom", ref _turnsSinceSpotted[(int)FACTION_PLAYER]);
		TryReadAsInt(reader, "turnsLeftSpottedForSnipersByXcom", ref _turnsLeftSpottedForSnipers[(int)FACTION_PLAYER]);
		TryReadAsInt(reader, "turnsSinceSpottedByCivilian", ref _turnsSinceSpotted[(int)FACTION_NEUTRAL]);
		TryReadAsInt(reader, "turnsLeftSpottedForSnipersByCivilian", ref _turnsLeftSpottedForSnipers[(int)FACTION_NEUTRAL]);
		TryReadAsInt(reader, "turnsSinceStunned", ref _turnsSinceStunned);

		reader.TryRead("rankInt", ref _rankInt);
		reader.TryRead("rankIntUnified", ref _rankIntUnified);
		reader.TryRead("moraleRestored", ref _moraleRestored);
		reader.TryRead("notificationShown", ref _notificationShown);
		reader.TryRead("killedBy", ref _killedBy);
		reader.TryRead("kills", ref _kills);
		reader.TryRead("dontReselect", ref _dontReselect);
		reader.TryRead("aiMedikitUsed", ref _aiMedikitUsed);
		_charging = null;
		// Mod::getUnit returns null for an empty name without touching the rules; checked here so units without a spawn unit load without a Mod
		string spawnUnitName = reader["spawnUnit"].ReadVal("");
		if ((_spawnUnit = spawnUnitName.Length != 0 ? mod.GetUnit(spawnUnitName, false) : null) != null) // ignore bugged types
		{
			reader.TryRead("respawn", ref _respawn);
			reader.TryRead("spawnUnitFaction", ref _spawnUnitFaction);
		}
		reader.TryRead("motionPoints", ref _motionPoints);
		reader.TryRead("customMarker", ref _customMarker);
		reader.TryRead("alreadyRespawned", ref _alreadyRespawned);
		reader.TryRead("activeHand", ref _activeHand);
		reader.TryRead("preferredHandForReactions", ref _preferredHandForReactions);
		reader.TryRead("reactionsDisabledForLeftHand", ref _reactionsDisabledForLeftHand);
		reader.TryRead("reactionsDisabledForRightHand", ref _reactionsDisabledForRightHand);
		if (reader["tempUnitStatistics"])
			_statistics.Load(reader["tempUnitStatistics"]);
		reader.TryRead("murdererId", ref _murdererId);
		reader.TryRead("fatalShotSide", ref _fatalShotSide);
		reader.TryRead("fatalShotBodyPart", ref _fatalShotBodyPart);
		reader.TryRead("murdererWeapon", ref _murdererWeapon);
		reader.TryRead("murdererWeaponAmmo", ref _murdererWeaponAmmo);

		var recolor = reader["recolor"];
		if (recolor)
		{
			_recolor.Clear();
			for (int i = 0; i < recolor.ChildrenCount; ++i)
				_recolor.Add((recolor[i][0].ReadVal<byte>(), recolor[i][1].ReadVal<byte>()));
		}
		reader.TryRead("mindControllerID", ref _mindControllerID);
		reader.TryRead("summonedPlayerUnit", ref _summonedPlayerUnit);
		reader.TryRead("resummonedFakeCivilian", ref _resummonedFakeCivilian);
		reader.TryRead("pickUpWeaponsMoreActively", ref _pickUpWeaponsMoreActively);
		reader.TryRead("disableIndicators", ref _disableIndicators);
		reader.TryRead("movementType", ref _movementType);
		var moveCost = reader["moveCost"];
		if (moveCost)
		{
			_moveCostBase.Load(moveCost["basePercent"]);
			_moveCostBaseFly.Load(moveCost["baseFlyPercent"]);
			_moveCostBaseClimb.Load(moveCost["baseClimbPercent"]);
			_moveCostBaseNormal.Load(moveCost["baseNormalPercent"]);
		}
		reader.TryRead("vip", ref _vip);
		reader.TryRead("bannedInNextStage", ref _bannedInNextStage);
		reader.TryRead("meleeAttackedBy", ref _meleeAttackedBy);
		if (mod != null)
		{
			_scriptValues.Load(reader, mod.ScriptGlobal);
		}
	}

	/// <summary>C++ <c>reader.tryReadAs&lt;int&gt;</c> into an 8 bit counter (the value is truncated like <c>static_cast&lt;Uint8&gt;</c>).</summary>
	private static void TryReadAsInt(YamlNodeReader reader, string key, ref byte outputValue)
	{
		int temp = 0;
		if (reader.TryRead(key, ref temp))
		{
			outputValue = unchecked((byte)temp);
		}
	}

	/// <summary>Saves the unit to YAML.</summary>
	/// <param name="writer">YAML node.</param>
	public void Save(YamlNodeWriter writer, ScriptGlobal shared = null)
	{
		writer.SetAsMap();
		writer.Write("id", _id);
		writer.Write("genUnitType", _type);
		writer.Write("genUnitArmor", _armor.Type);
		writer.Write("faction", _faction);
		writer.Write("status", _status);
		if (_wantsToSurrender)
			writer.Write("wantsToSurrender", _wantsToSurrender);
		if (_isSurrendering)
			writer.Write("isSurrendering", _isSurrendering);
		writer.Write("position", _pos);
		writer.Write("direction", _direction);
		writer.Write("directionTurret", _directionTurret);
		writer.Write("tu", _tu);
		writer.Write("health", _health);
		writer.Write("mana", _mana);
		writer.Write("stunlevel", _stunlevel);
		writer.Write("energy", _energy);
		writer.Write("morale", _morale);
		if (_kneeled)
			writer.Write("kneeled", _kneeled);
		if (_floating)
			writer.Write("floating", _floating);
		var armorWriter = writer["armor"];
		armorWriter.SetAsSeq();
		armorWriter.SetFlowStyle();
		for (int i = 0; i < (int)SIDE_MAX; i++)
			armorWriter.Write(_currentArmor[i]);
		var fwWriter = writer["fatalWounds"];
		fwWriter.SetAsSeq();
		fwWriter.SetFlowStyle();
		for (int i = 0; i < (int)BODYPART_MAX; i++)
			fwWriter.Write(_fatalWounds[i]);
		writer.Write("fire", _fire);
		writer.Write("expBravery", _exp.bravery);
		writer.Write("expReactions", _exp.reactions);
		writer.Write("expFiring", _exp.firing);
		writer.Write("expThrowing", _exp.throwing);
		writer.Write("expPsiSkill", _exp.psiSkill);
		writer.Write("expPsiStrength", _exp.psiStrength);
		writer.Write("expMana", _exp.mana);
		writer.Write("expMelee", _exp.melee);
		writer.Write("currStats", _stats);
		if (_turretType > -1)
			writer.Write("turretType", _turretType);
		if (_visible)
			writer.Write("visible", _visible);

		writer.Write("turnsSinceSpotted", (int)_turnsSinceSpotted[(int)FACTION_HOSTILE]);
		writer.Write("turnsLeftSpottedForSnipers", (int)_turnsLeftSpottedForSnipers[(int)FACTION_HOSTILE]);
		writer.TryWrite("turnsSinceSpottedByXcom", (int)_turnsSinceSpotted[(int)FACTION_PLAYER], 255);
		writer.TryWrite("turnsLeftSpottedForSnipersByXcom", (int)_turnsLeftSpottedForSnipers[(int)FACTION_PLAYER], 0);
		writer.TryWrite("turnsSinceSpottedByCivilian", (int)_turnsSinceSpotted[(int)FACTION_NEUTRAL], 255);
		writer.TryWrite("turnsLeftSpottedForSnipersByCivilian", (int)_turnsLeftSpottedForSnipers[(int)FACTION_NEUTRAL], 0);
		writer.Write("turnsSinceStunned", (int)_turnsSinceStunned);

		writer.Write("rankInt", _rankInt);
		writer.Write("rankIntUnified", _rankIntUnified);
		writer.Write("moraleRestored", _moraleRestored);
		if (_notificationShown > 0)
			writer.Write("notificationShown", _notificationShown);
		AIModule?.Save(writer["AI"]);
		writer.Write("killedBy", _killedBy); // does not have a default value, must always be saved
		if (_originalFaction != _faction)
			writer.Write("originalFaction", _originalFaction);
		if (_kills != 0)
			writer.Write("kills", _kills);
		if (_faction == FACTION_PLAYER && _dontReselect)
			writer.Write("dontReselect", _dontReselect);
		if (_aiMedikitUsed)
			writer.Write("aiMedikitUsed", _aiMedikitUsed);
		if (_previousOwner != null)
			writer.Write("previousOwner", _previousOwner.Id);
		if (_spawnUnit != null)
		{
			writer.Write("spawnUnit", _spawnUnit.Type);
			writer.Write("respawn", _respawn);
			writer.Write("spawnUnitFaction", _spawnUnitFaction);
		}
		writer.Write("motionPoints", _motionPoints);
		if (_customMarker > 0)
			writer.Write("customMarker", _customMarker);
		if (_alreadyRespawned)
			writer.Write("alreadyRespawned", _alreadyRespawned);
		writer.Write("activeHand", _activeHand);
		if (_preferredHandForReactions.Length != 0)
			writer.Write("preferredHandForReactions", _preferredHandForReactions);
		if (_reactionsDisabledForLeftHand)
			writer.Write("reactionsDisabledForLeftHand", _reactionsDisabledForLeftHand);
		if (_reactionsDisabledForRightHand)
			writer.Write("reactionsDisabledForRightHand", _reactionsDisabledForRightHand);
		_statistics.Save(writer["tempUnitStatistics"]);
		if (_murdererId != 0)
			writer.Write("murdererId", _murdererId);
		if (_fatalShotSide != 0)
			writer.Write("fatalShotSide", _fatalShotSide);
		if (_fatalShotBodyPart != 0)
			writer.Write("fatalShotBodyPart", _fatalShotBodyPart);
		if (_murdererWeapon.Length != 0)
			writer.Write("murdererWeapon", _murdererWeapon);
		if (_murdererWeaponAmmo.Length != 0)
			writer.Write("murdererWeaponAmmo", _murdererWeaponAmmo);
		writer.Write("recolor", _recolor,
			(vectorWriter, pair) =>
			{
				var pairWriter = vectorWriter.Write();
				pairWriter.SetAsSeq();
				pairWriter.SetFlowStyle();
				pairWriter.Write(pair.first);
				pairWriter.Write(pair.second);
			});
		if (_mindControllerID != 0)
			writer.Write("mindControllerID", _mindControllerID);
		if (_summonedPlayerUnit)
			writer.Write("summonedPlayerUnit", _summonedPlayerUnit);
		if (_resummonedFakeCivilian)
			writer.Write("resummonedFakeCivilian", _resummonedFakeCivilian);
		if (_pickUpWeaponsMoreActively)
			writer.Write("pickUpWeaponsMoreActively", _pickUpWeaponsMoreActively);
		if (_disableIndicators)
			writer.Write("disableIndicators", _disableIndicators);

		if (_originalMovementType != _movementType)
			writer.Write("movementType", (int)_movementType);
		if (_moveCostBase != _armor.MoveCostBase ||
			_moveCostBaseFly != _armor.MoveCostBaseFly ||
			_moveCostBaseClimb != _armor.MoveCostBaseClimb ||
			_moveCostBaseNormal != _armor.MoveCostBaseNormal)
		{
			var moveCostWriter = writer["moveCost"];
			moveCostWriter.SetAsMap();
			moveCostWriter.SetFlowStyle();
			if (_moveCostBase != _armor.MoveCostBase)
				_moveCostBase.Save(moveCostWriter, "basePercent");
			if (_moveCostBaseFly != _armor.MoveCostBaseFly)
				_moveCostBaseFly.Save(moveCostWriter, "baseFlyPercent");
			if (_moveCostBaseClimb != _armor.MoveCostBaseClimb)
				_moveCostBaseClimb.Save(moveCostWriter, "baseClimbPercent");
			if (_moveCostBaseNormal != _armor.MoveCostBaseNormal)
				_moveCostBaseNormal.Save(moveCostWriter, "baseNormalPercent");
		}
		if (_vip)
			writer.Write("vip", _vip);
		if (_bannedInNextStage)
			writer.Write("bannedInNextStage", _bannedInNextStage);
		if (_meleeAttackedBy.Count != 0)
			writer.Write("meleeAttackedBy", _meleeAttackedBy);
		if (shared != null)
		{
			_scriptValues.Save(writer, shared);
		}
	}

	/// <summary>Prepare vector values for recolor.</summary>
	/// <param name="basicLook">select index for hair and face color.</param>
	/// <param name="utileLook">select index for utile color.</param>
	/// <param name="rankLook">select index for rank color.</param>
	private void SetRecolor(int basicLook, int utileLook, int rankLook)
	{
		_recolor.Clear(); // reset in case of OXCE on-the-fly armor changes/transformations
		const int colorsMax = 4;
		var colors = new (int first, int second)[colorsMax]
		{
			(_armor.FaceColorGroup, _armor.GetFaceColor(basicLook)),
			(_armor.HairColorGroup, _armor.GetHairColor(basicLook)),
			(_armor.UtileColorGroup, _armor.GetUtileColor(utileLook)),
			(_armor.RankColorGroup, _armor.GetRankColor(rankLook)),
		};

		for (int i = 0; i < colorsMax; ++i)
		{
			if (colors[i].first > 0 && colors[i].second > 0)
			{
				_recolor.Add((unchecked((byte)(colors[i].first << 4)), unchecked((byte)colors[i].second)));
			}
		}
	}

	/// <summary>C++ <c>Clamp</c> (fmath.h): <c>std::min(std::max(x, min), max)</c>, unlike <see cref="Math.Clamp(int, int, int)"/> it allows min &gt; max.</summary>
	private static int Clamp(int x, int min, int max) => Math.Min(Math.Max(x, min), max);

	/// <summary>Helper function for setting value with max bound.</summary>
	private static void SetValueMax(ref int value, int diff, int min, int max)
	{
		value = Clamp(value + diff, min, max);
	}
}
