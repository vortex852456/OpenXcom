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
// Ported from: src/Mod/Mod.h, src/Mod/Mod.cpp
#nullable disable

namespace Acom;

/// <summary>Mod data used when loading resources.</summary>
public class ModData
{
	/// <summary>Mod name.</summary>
	public string name;
	/// <summary>Optional info about mod.</summary>
	public ModInfo info;
	/// <summary>Offset that mod use is common sets.</summary>
	public int offset;
	/// <summary>Maximum size allowed by mod in common sets.</summary>
	public int size;
}

/// <summary>
/// Helper exception representing the final message with all the required context for the end user to fix the errors in rulesets.
/// </summary>
/// <remarks>Derives from <see cref="InvalidOperationException"/>, the port's counterpart of the C++ <c>Exception</c> base.</remarks>
public class LoadRuleException : InvalidOperationException
{
	public LoadRuleException(string parent, YamlNodeReader reader, string message)
		: base("Error for '" + parent + "': " + message + " at line " + reader.LocationInFile.Line)
	{
	}

	public LoadRuleException(string parent, string message) : base("Error for '" + parent + "': " + message)
	{
	}

	public LoadRuleException()
	{
	}

	public LoadRuleException(string message) : base(message)
	{
	}

	public LoadRuleException(string message, Exception innerException) : base(message, innerException)
	{
	}
}

/// <summary>
/// Contains all the game-specific static data that never changes
/// throughout the game, like rulesets and resources.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "C++ class name")]
public partial class Mod
{
	/// <summary>Number of color per opacity level.</summary>
	public const int TransparenciesPaletteColors = 256;
	/// <summary>Number of opacity levels.</summary>
	public const int TransparenciesOpacityLevels = 4;

	private Music _muteMusic;
	private Sound _muteSound;
	private string _playingMusic = "", _currentMusicTrack = "";

	// internal for tests of the ruleset part, which register the vanilla sets themselves
	internal readonly SortedDictionary<string, Palette> _palettes = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, Font> _fonts = new(StringComparer.Ordinal);
	internal readonly SortedDictionary<string, Surface> _surfaces = new(StringComparer.Ordinal);
	internal readonly SortedDictionary<string, SurfaceSet> _sets = new(StringComparer.Ordinal);
	internal readonly SortedDictionary<string, SoundSet> _sounds = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, Music> _musics = new(StringComparer.Ordinal);
	private readonly List<ushort> _voxelData = [];
	private readonly List<List<byte>> _transparencyLUTs = [];

	private readonly SortedDictionary<string, RuleCountry> _countries = new(StringComparer.Ordinal), _extraGlobeLabels = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleRegion> _regions = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleBaseFacility> _facilities = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleCraft> _crafts = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleCraftWeapon> _craftWeapons = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleItemCategory> _itemCategories = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleItem> _items = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleWeaponSet> _weaponSets = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleUfo> _ufos = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleTerrain> _terrains = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, MapDataSet> _mapDataSets = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleSkill> _skills = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleSoldier> _soldiers = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, Unit> _units = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, AlienRace> _alienRaces = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleEnviroEffects> _enviroEffects = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleStartingCondition> _startingConditions = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, AlienDeployment> _alienDeployments = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, Armor> _armors = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, ArticleDefinition> _ufopaediaArticles = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleInventory> _invs = new(StringComparer.Ordinal);
	private bool _inventoryOverlapsPaperdoll;
	private readonly SortedDictionary<string, RuleResearch> _research = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleManufacture> _manufacture = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleManufactureShortcut> _manufactureShortcut = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleSoldierBonus> _soldierBonus = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleSoldierTransformation> _soldierTransformation = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, UfoTrajectory> _ufoTrajectories = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleAlienMission> _alienMissions = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleInterface> _interfaces = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, SoundDefinition> _soundDefs = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleVideo> _videos = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, MCDPatch> _MCDPatches = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, List<MapScript>> _mapScripts = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleCommendations> _commendations = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleArcScript> _arcScripts = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleEventScript> _eventScripts = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleEvent> _events = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleMissionScript> _missionScripts = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, RuleMissionScript> _adhocScripts = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, List<ExtraSprites>> _extraSprites = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, CustomPalettes> _customPalettes = new(StringComparer.Ordinal);
	private readonly List<(string first, ExtraSounds second)> _extraSounds = [];
	private readonly SortedDictionary<string, ExtraStrings> _extraStrings = new(StringComparer.Ordinal);
	private readonly List<StatString> _statStrings = [];
	private readonly List<RuleDamageType> _damageTypes = [];
	private readonly SortedDictionary<string, RuleMusic> _musicDefs = new(StringComparer.Ordinal);

	private readonly RuleGlobe _globe;
	internal RuleConverter _converter; // internal for tests, see Mod(bool)

	private int _maxViewDistance, _maxDarknessToSeeUnits;
	private int _maxStaticLightDistance, _maxDynamicLightDistance, _enhancedLighting;
	private int _costHireEngineer, _costHireScientist;
	private int _costEngineer, _costScientist, _timePersonnel, _hireByCountryOdds, _hireByRegionOdds, _initialFunding;

	private int _aiUseDelayBlaster, _aiUseDelayFirearm, _aiUseDelayGrenade, _aiUseDelayProxy, _aiUseDelayMelee, _aiUseDelayPsionic, _aiUseDelayMedikit;
	private int _aiFireChoiceIntelCoeff, _aiFireChoiceAggroCoeff;
	private bool _aiExtendedFireModeChoice, _aiRespectMaxRange, _aiDestroyBaseFacilities;
	private bool _aiPickUpWeaponsMoreActively, _aiPickUpWeaponsMoreActivelyCiv;
	private int _aiReactionFireThreshold, _aiReactionFireThresholdCiv;
	private AIAttackWeight _aiTargetWeightThreatThreshold = (AIAttackWeight)50;
	private AIAttackWeight _aiTargetWeightAsHostile = (AIAttackWeight)100;
	private AIAttackWeight _aiTargetWeightAsHostileCivilians = (AIAttackWeight)50;
	private AIAttackWeight _aiTargetWeightAsFriendly = (AIAttackWeight)(-200);
	private AIAttackWeight _aiTargetWeightAsNeutral = (AIAttackWeight)(-100);

	private int _maxLookVariant, _tooMuchSmokeThreshold, _customTrainingFactor;
	private int _chanceToStopRetaliation;
	private int _chanceToDetectAlienBaseEachMonth;
	private bool _lessAliensDuringBaseDefense;
	private bool _allowCountriesToCancelAlienPact, _buildInfiltrationBaseCloseToTheCountry, _infiltrateRandomCountryInTheRegion;
	private bool _allowAlienBasesOnWrongTextures;
	private int _kneelBonusGlobal, _oneHandedPenaltyGlobal;
	private int _enableCloseQuartersCombat, _closeQuartersAccuracyGlobal, _closeQuartersTuCostGlobal, _closeQuartersEnergyCostGlobal, _closeQuartersSneakUpGlobal;
	private int _noLOSAccuracyPenaltyGlobal;
	private int _explodeInventoryGlobal;
	private int _surrenderMode;
	private int _bughuntMinTurn, _bughuntMaxEnemies, _bughuntRank, _bughuntLowMorale, _bughuntTimeUnitsLeft;

	private int _manaMissingWoundThreshold = 200;
	private int _healthMissingWoundThreshold = 100;
	private bool _manaEnabled, _manaBattleUI, _manaTrainingPrimary, _manaTrainingSecondary, _manaReplenishAfterMission;
	private bool _healthReplenishAfterMission = true;
	private string _manaUnlockResearch = "";

	private string _loseMoney, _loseRating, _loseDefeat;
	private int _ufoGlancingHitThreshold, _ufoBeamWidthParameter;
	private readonly int[] _ufoTractorBeamSizeModifiers = new int[5];
	private int _escortRange, _drawEnemyRadarCircles;
	private bool _escortsJoinFightAgainstHK, _hunterKillerFastRetarget;
	private int _crewEmergencyEvacuationSurvivalChance, _pilotsEmergencyEvacuationSurvivalChance;
	private bool _showUfoPreviewInBaseDefense;
	private readonly int[] _soldiersPerRank;
	private int _pilotAccuracyZeroPoint, _pilotAccuracyRange, _pilotReactionsZeroPoint, _pilotReactionsRange;
	private readonly int[] _pilotBraveryThresholds = new int[3];
	private double _performanceBonusFactor;
	private bool _enableNewResearchSorting;
	private int _displayCustomCategories;
	private bool _shareAmmoCategories, _showDogfightDistanceInKm, _showFullNameInAlienInventory;
	private int _alienInventoryOffsetX, _alienInventoryOffsetBigUnit;
	private bool _hidePediaInfoButton;
	private int _extraNerdyPediaInfoType;
	private bool _giveScoreAlsoForResearchedArtifacts, _statisticalBulletConservation, _stunningImprovesMorale;
	private int _tuRecoveryWakeUpNewTurn;
	private int _shortRadarRange;
	private int _buildTimeReductionScaling;
	private int _defeatScore, _defeatFunds;
	private bool _difficultyDemigod;
	private (string first, int second) _alienFuel = ("", 0);
	private RuleResearch _finalResearch;
	private string _fontName = "", _psiUnlockResearch = "", _fakeUnderwaterBaseUnlockResearch = "", _newBaseUnlockResearch = "";
	private string _hireScientistsUnlockResearch = "", _hireEngineersUnlockResearch = "";
	private RuleBaseFacilityFunctions _hireScientistsRequiresBaseFunc, _hireEngineersRequiresBaseFunc;

	private string _destroyedFacility = "";
	// YAML::YamlString
	private string _startingBaseDefault = "", _startingBaseBeginner = "", _startingBaseExperienced = "", _startingBaseVeteran = "", _startingBaseGenius = "", _startingBaseSuperhuman = "";
	private readonly NamesToIndex _baseFunctionNames = new();

	private readonly GameTime _startingTime;
	private int _startingDifficulty;
	private int _baseDefenseMapFromLocation;
	private SortedDictionary<int, string> _missionRatings = [], _monthlyRatings = [];
	private readonly SortedDictionary<string, string> _fixedUserOptions = new(StringComparer.Ordinal), _recommendedUserOptions = new(StringComparer.Ordinal);
	private readonly List<string> _hiddenMovementBackgrounds = [];
	private readonly List<string> _baseNamesFirst = [], _baseNamesMiddle = [], _baseNamesLast = [];
	private readonly List<string> _operationNamesFirst = [], _operationNamesLast = [];
	private bool _disableUnderwaterSounds;
	private bool _enableUnitResponseSounds;
	private readonly SortedDictionary<string, List<int>> _selectUnitSound = new(StringComparer.Ordinal), _startMovingSound = new(StringComparer.Ordinal), _selectWeaponSound = new(StringComparer.Ordinal), _annoyedSound = new(StringComparer.Ordinal);
	private readonly List<int> _selectBaseSound = [], _startDogfightSound = [];
	private List<int> _flagByKills = [];
	private int _pediaReplaceCraftFuelWithRangeType;
	private readonly List<StatAdjustment> _statAdjustment = [];

	// overrides for DIFFICULTY_COEFFICIENT[]
	private List<int> _monthlyRatingThresholds = [];
	private List<int> _ufoFiringRateCoefficients = [];
	private List<int> _ufoEscapeCountdownCoefficients = [];
	private List<int> _retaliationTriggerOdds = [];
	private List<int> _retaliationBaseRegionOdds = [];
	private List<int> _aliensFacingCraftOdds = [];

	private readonly SortedDictionary<string, int> _ufopaediaSections = new(StringComparer.Ordinal);
	private readonly List<string> _countriesIndex = [], _extraGlobeLabelsIndex = [], _regionsIndex = [], _facilitiesIndex = [], _craftsIndex = [], _craftWeaponsIndex = [], _itemCategoriesIndex = [], _itemsIndex = [], _invsIndex = [], _ufosIndex = [];
	private readonly List<string> _aliensIndex = [], _enviroEffectsIndex = [], _startingConditionsIndex = [], _deploymentsIndex = [], _armorsIndex = [], _ufopaediaIndex = [], _ufopaediaCatIndex = [], _researchIndex = [], _manufactureIndex = [];
	private readonly List<string> _skillsIndex = [], _soldiersIndex = [], _soldierTransformationIndex = [], _soldierBonusIndex = [];
	private readonly List<string> _alienMissionsIndex = [], _terrainIndex = [], _customPalettesIndex = [], _arcScriptIndex = [], _eventScriptIndex = [], _eventIndex = [], _missionScriptIndex = [], _adhocScriptIndex = [];
	private List<List<int>> _alienItemLevels = [];
	private readonly List<Rgba[]> _transparencies = [];
	private int _facilityListOrder, _craftListOrder, _itemCategoryListOrder, _itemListOrder, _armorListOrder, _alienRaceListOrder, _researchListOrder, _manufactureListOrder;
	private int _soldierBonusListOrder, _transformationListOrder, _ufopaediaListOrder, _invListOrder, _soldierListOrder;
	// internal for tests of the load helpers
	internal readonly List<ModData> _modData = [];
	internal ModData _modCurrent;
	/// <summary>Never set in the original either (always null).</summary>
	private readonly Rgba[] _statePalette;

	private List<string> _psiRequirements = []; // it's a cache for psiStrengthEval
	private readonly List<Armor> _armorsForSoldiersCache = [];
	// C++ keeps sorted pointer vectors and binary-searches them; only membership is ever queried
	private readonly HashSet<RuleItem> _armorStorageItemsCache = new(ReferenceEqualityComparer.Instance);
	private readonly HashSet<RuleItem> _craftWeaponStorageItemsCache = new(ReferenceEqualityComparer.Instance);
	/// <summary>Track of what mod create rule object.</summary>
	private readonly Dictionary<object, ModData> _ruleCreationTracking = new(ReferenceEqualityComparer.Instance);
	/// <summary>Track of what mod last update rule object.</summary>
	private readonly Dictionary<object, ModData> _ruleLastUpdateTracking = new(ReferenceEqualityComparer.Instance);

	private int _surfaceOffsetBigobs;
	private int _surfaceOffsetFloorob;
	private int _surfaceOffsetHandob;
	private int _surfaceOffsetSmoke;
	private int _surfaceOffsetHit;
	private int _surfaceOffsetBasebits;
	private int _soundOffsetBattle;
	private int _soundOffsetGeo;

	/// <summary>File being loaded, used in error messages (C++ keeps it in the script global data).</summary>
	private string _currentFile = "";

#pragma warning disable CA2211 // Global ruleset constants, as in the C++ code.
	public static int DOOR_OPEN;
	public static int SLIDING_DOOR_OPEN;
	public static int SLIDING_DOOR_CLOSE;
	public static int SMALL_EXPLOSION;
	public static int LARGE_EXPLOSION;
	public static int EXPLOSION_OFFSET;
	public static int SMOKE_OFFSET;
	public static int UNDERWATER_SMOKE_OFFSET;

	/// <summary>Empty surface.</summary>
	public const int NO_SURFACE = -1;
	/// <summary>Empty sound.</summary>
	public const int NO_SOUND = -1;
	/// <summary>Special value for default string different to empty one.</summary>
	public const string STR_NULL = "\0";

	public static int ITEM_DROP;
	public static int ITEM_THROW;
	public static int ITEM_RELOAD;
	public static int WALK_OFFSET;
	public static int FLYING_SOUND;
	public static int BUTTON_PRESS;
	public static readonly int[] WINDOW_POPUP = new int[3];
	public static int UFO_FIRE;
	public static int UFO_HIT;
	public static int UFO_CRASH;
	public static int UFO_EXPLODE;
	public static int INTERCEPTOR_HIT;
	public static int INTERCEPTOR_EXPLODE;

	public static int GEOSCAPE_CURSOR;
	public static int BASESCAPE_CURSOR;
	public static int BATTLESCAPE_CURSOR;
	public static int UFOPAEDIA_CURSOR;
	public static int GRAPHS_CURSOR;
	public static int DAMAGE_RANGE;
	public static int EXPLOSIVE_DAMAGE_RANGE;
	public static readonly int[] FIRE_DAMAGE_RANGE = new int[2];
	public static string DEBRIEF_MUSIC_GOOD;
	public static string DEBRIEF_MUSIC_BAD;
	public static readonly int[] DIFFICULTY_COEFFICIENT = new int[5];
	public static readonly int[] SELL_PRICE_COEFFICIENT = new int[5];
	public static readonly int[] BUY_PRICE_COEFFICIENT = new int[5];
	public static readonly int[] DIFFICULTY_BASED_RETAL_DELAY = new int[5];
	public static readonly int[] UNIT_RESPONSE_SOUNDS_FREQUENCY = new int[4];
	public static readonly int[] PEDIA_FACILITY_RENDER_PARAMETERS = new int[4];
	public static bool EXTENDED_ITEM_RELOAD_COST;
	public static bool EXTENDED_INVENTORY_SLOT_SORTING;
	public static bool EXTENDED_RUNNING_COST;
	public static int EXTENDED_MOVEMENT_COST_ROUNDING;
	public static bool EXTENDED_HWP_LOAD_ORDER;
	public static int EXTENDED_SPOT_ON_HIT_FOR_SNIPING;
	public static int EXTENDED_BERSERK_WITH_AIMED;
	public static int EXTENDED_MELEE_REACTIONS;
	public static int EXTENDED_TERRAIN_MELEE;
	public static int EXTENDED_UNDERWATER_THROW_FACTOR;
	public static bool EXTENDED_EXPERIENCE_AWARD_SYSTEM;
	public static bool EXTENDED_FORCE_SPAWN;
	public static int EXTENDED_SMOKE_OFFSET;
#pragma warning restore CA2211

	private const int MaxDifficultyLevels = 5;

	/// <summary>Predefined name for first loaded mod that have all original data.</summary>
	private const string ModNameMaster = "master";
	/// <summary>Predefined name for current mod that is loading rulesets.</summary>
	private const string ModNameCurrent = "current";

	/// <summary>Reduction of size allocated for transparency LUTs.</summary>
	private const int ModTransparencySizeReduction = 100;

	/// <summary>Return <c>true</c> when given string is empty or pseudo null value.</summary>
	public static bool IsEmptyRuleName(string s) => string.IsNullOrEmpty(s) || s == STR_NULL;

	/// <summary>Reset all the statics in all classes to default values.</summary>
	public static void ResetGlobalStatics()
	{
		DOOR_OPEN = 3;
		SLIDING_DOOR_OPEN = 20;
		SLIDING_DOOR_CLOSE = 21;
		SMALL_EXPLOSION = 2;
		LARGE_EXPLOSION = 5;
		EXPLOSION_OFFSET = 0;
		SMOKE_OFFSET = 8;
		UNDERWATER_SMOKE_OFFSET = 0;
		ITEM_DROP = 38;
		ITEM_THROW = 39;
		ITEM_RELOAD = 17;
		WALK_OFFSET = 22;
		FLYING_SOUND = 15;
		BUTTON_PRESS = 0;
		WINDOW_POPUP[0] = 1;
		WINDOW_POPUP[1] = 2;
		WINDOW_POPUP[2] = 3;
		UFO_FIRE = 8;
		UFO_HIT = 12;
		UFO_CRASH = 10;
		UFO_EXPLODE = 11;
		INTERCEPTOR_HIT = 10;
		INTERCEPTOR_EXPLODE = 13;
		GEOSCAPE_CURSOR = 252;
		BASESCAPE_CURSOR = 252;
		BATTLESCAPE_CURSOR = 144;
		UFOPAEDIA_CURSOR = 252;
		GRAPHS_CURSOR = 252;
		DAMAGE_RANGE = 100;
		EXPLOSIVE_DAMAGE_RANGE = 50;
		FIRE_DAMAGE_RANGE[0] = 5;
		FIRE_DAMAGE_RANGE[1] = 10;
		DEBRIEF_MUSIC_GOOD = "GMMARS";
		DEBRIEF_MUSIC_BAD = "GMMARS";

		// "Acom." because the Globe property of this class hides the Geoscape class
		Acom.Globe.OCEAN_COLOR = Palette.BlockOffset(12);
		Acom.Globe.OCEAN_SHADING = true;
		Acom.Globe.COUNTRY_LABEL_COLOR = 239;
		Acom.Globe.LINE_COLOR = 162;
		Acom.Globe.CITY_LABEL_COLOR = 138;
		Acom.Globe.BASE_LABEL_COLOR = 133;

		TextButton.soundPress = null;

		// C++: static Sound *soundPopup[3]
		Window.soundPopup ??= new Sound[3];
		Window.soundPopup[0] = null;
		Window.soundPopup[1] = null;
		Window.soundPopup[2] = null;

		Pathfinding.red = 3;
		Pathfinding.yellow = 10;
		Pathfinding.green = 4;

		DIFFICULTY_COEFFICIENT[0] = 0;
		DIFFICULTY_COEFFICIENT[1] = 1;
		DIFFICULTY_COEFFICIENT[2] = 2;
		DIFFICULTY_COEFFICIENT[3] = 3;
		DIFFICULTY_COEFFICIENT[4] = 4;

		SELL_PRICE_COEFFICIENT[0] = 100;
		SELL_PRICE_COEFFICIENT[1] = 100;
		SELL_PRICE_COEFFICIENT[2] = 100;
		SELL_PRICE_COEFFICIENT[3] = 100;
		SELL_PRICE_COEFFICIENT[4] = 100;

		BUY_PRICE_COEFFICIENT[0] = 100;
		BUY_PRICE_COEFFICIENT[1] = 100;
		BUY_PRICE_COEFFICIENT[2] = 100;
		BUY_PRICE_COEFFICIENT[3] = 100;
		BUY_PRICE_COEFFICIENT[4] = 100;

		DIFFICULTY_BASED_RETAL_DELAY[0] = 0;
		DIFFICULTY_BASED_RETAL_DELAY[1] = 0;
		DIFFICULTY_BASED_RETAL_DELAY[2] = 0;
		DIFFICULTY_BASED_RETAL_DELAY[3] = 0;
		DIFFICULTY_BASED_RETAL_DELAY[4] = 0;

		UNIT_RESPONSE_SOUNDS_FREQUENCY[0] = 100; // select unit
		UNIT_RESPONSE_SOUNDS_FREQUENCY[1] = 100; // start moving
		UNIT_RESPONSE_SOUNDS_FREQUENCY[2] = 100; // select weapon
		UNIT_RESPONSE_SOUNDS_FREQUENCY[3] = 20;  // annoyed

		PEDIA_FACILITY_RENDER_PARAMETERS[0] = 2; // pedia facility max width
		PEDIA_FACILITY_RENDER_PARAMETERS[1] = 2; // pedia facility max height
		PEDIA_FACILITY_RENDER_PARAMETERS[2] = 0; // pedia facility X offset
		PEDIA_FACILITY_RENDER_PARAMETERS[3] = 0; // pedia facility Y offset

		EXTENDED_ITEM_RELOAD_COST = false;
		EXTENDED_INVENTORY_SLOT_SORTING = false;
		EXTENDED_RUNNING_COST = false;
		EXTENDED_MOVEMENT_COST_ROUNDING = 0;
		EXTENDED_HWP_LOAD_ORDER = false;
		EXTENDED_SPOT_ON_HIT_FOR_SNIPING = 0;
		EXTENDED_BERSERK_WITH_AIMED = 0;
		EXTENDED_MELEE_REACTIONS = 0;
		EXTENDED_TERRAIN_MELEE = 0;
		EXTENDED_UNDERWATER_THROW_FACTOR = 0;
		EXTENDED_EXPERIENCE_AWARD_SYSTEM = false;
		EXTENDED_FORCE_SPAWN = false;
		EXTENDED_SMOKE_OFFSET = 0;

		Unicode.OXCE_CURRENCY_SYMBOL = "$";
	}

	public const string ScriptName = "RuleMod";

	/// <summary>Creates an empty mod.</summary>
	public Mod() : this(createConverter: true)
	{
	}

	/// <summary>Creates an empty mod.</summary>
	/// <param name="createConverter">
	/// False only for tests of the ruleset loading while RuleConverter is still a stub
	/// (the tests set <see cref="_converter"/> themselves).
	/// </param>
	internal Mod(bool createConverter)
	{
		_inventoryOverlapsPaperdoll = false;
		_maxViewDistance = 20; _maxDarknessToSeeUnits = 9; _maxStaticLightDistance = 16; _maxDynamicLightDistance = 24; _enhancedLighting = 0;
		_costHireEngineer = 0; _costHireScientist = 0;
		_costEngineer = 0; _costScientist = 0; _timePersonnel = 0; _hireByCountryOdds = 0; _hireByRegionOdds = 0; _initialFunding = 0;
		_aiUseDelayBlaster = 3; _aiUseDelayFirearm = 0; _aiUseDelayGrenade = 3; _aiUseDelayProxy = 999; _aiUseDelayMelee = 0; _aiUseDelayPsionic = 0; _aiUseDelayMedikit = 999;
		_aiFireChoiceIntelCoeff = 5; _aiFireChoiceAggroCoeff = 5; _aiExtendedFireModeChoice = false; _aiRespectMaxRange = false; _aiDestroyBaseFacilities = false;
		_aiPickUpWeaponsMoreActively = false; _aiPickUpWeaponsMoreActivelyCiv = false;
		_aiReactionFireThreshold = 0; _aiReactionFireThresholdCiv = 0;
		_maxLookVariant = 0; _tooMuchSmokeThreshold = 10; _customTrainingFactor = 100;
		_chanceToStopRetaliation = 0; _chanceToDetectAlienBaseEachMonth = 20; _lessAliensDuringBaseDefense = false;
		_allowCountriesToCancelAlienPact = false; _buildInfiltrationBaseCloseToTheCountry = false; _infiltrateRandomCountryInTheRegion = false; _allowAlienBasesOnWrongTextures = true;
		_kneelBonusGlobal = 115; _oneHandedPenaltyGlobal = 80;
		_enableCloseQuartersCombat = 0; _closeQuartersAccuracyGlobal = 100; _closeQuartersTuCostGlobal = 12; _closeQuartersEnergyCostGlobal = 8; _closeQuartersSneakUpGlobal = 0;
		_noLOSAccuracyPenaltyGlobal = -1;
		_explodeInventoryGlobal = 0;
		_surrenderMode = 0;
		_bughuntMinTurn = 999; _bughuntMaxEnemies = 2; _bughuntRank = 0; _bughuntLowMorale = 40; _bughuntTimeUnitsLeft = 60;
		_manaEnabled = false; _manaBattleUI = false; _manaTrainingPrimary = false; _manaTrainingSecondary = false; _manaReplenishAfterMission = true;
		_loseMoney = "loseGame"; _loseRating = "loseGame"; _loseDefeat = "loseGame";
		_ufoGlancingHitThreshold = 0; _ufoBeamWidthParameter = 1000;
		_escortRange = 20; _drawEnemyRadarCircles = 1; _escortsJoinFightAgainstHK = true; _hunterKillerFastRetarget = true;
		_crewEmergencyEvacuationSurvivalChance = 100; _pilotsEmergencyEvacuationSurvivalChance = 100;
		_showUfoPreviewInBaseDefense = false;
		_soldiersPerRank = [-1, -1, 5, 11, 23, 30];
		_pilotAccuracyZeroPoint = 55; _pilotAccuracyRange = 40; _pilotReactionsZeroPoint = 55; _pilotReactionsRange = 60;
		_performanceBonusFactor = 0.0; _enableNewResearchSorting = false; _displayCustomCategories = 0; _shareAmmoCategories = false; _showDogfightDistanceInKm = false; _showFullNameInAlienInventory = false;
		_alienInventoryOffsetX = 80; _alienInventoryOffsetBigUnit = 32;
		_hidePediaInfoButton = false; _extraNerdyPediaInfoType = 0;
		_giveScoreAlsoForResearchedArtifacts = false; _statisticalBulletConservation = false; _stunningImprovesMorale = false;
		_tuRecoveryWakeUpNewTurn = 100; _shortRadarRange = 0; _buildTimeReductionScaling = 100;
		_defeatScore = 0; _defeatFunds = 0; _difficultyDemigod = false;
		_startingDifficulty = 0;
		_baseDefenseMapFromLocation = 0; _disableUnderwaterSounds = false; _enableUnitResponseSounds = false; _pediaReplaceCraftFuelWithRangeType = -1;
		_modCurrent = null; _statePalette = null;

		// _muteMusic and _muteSound are created on first use (see MuteMusic/MuteSound)

		_startingTime = new GameTime(6, 1, 1, 1999, 12, 0, 0);
		_globe = new RuleGlobe();
		CreateBaseDamageTypes();
		if (createConverter)
		{
			_converter = new RuleConverter();
		}

		for (int i = 0; i != MaxDifficultyLevels; ++i)
			_statAdjustment.Add(new StatAdjustment());
		_statAdjustment[0].aimMultiplier = 0.5;
		_statAdjustment[0].armorMultiplier = 0.5;
		_statAdjustment[0].armorMultiplierAbs = 0;
		_statAdjustment[0].growthMultiplier = 0;
		for (int i = 1; i != MaxDifficultyLevels; ++i)
		{
			_statAdjustment[i].aimMultiplier = 1.0;
			_statAdjustment[i].armorMultiplier = 1.0;
			_statAdjustment[i].armorMultiplierAbs = 0;
			_statAdjustment[i].growthMultiplier = i;
		}

		// Setting default value for array
		_ufoTractorBeamSizeModifiers[0] = 400;
		_ufoTractorBeamSizeModifiers[1] = 200;
		_ufoTractorBeamSizeModifiers[2] = 100;
		_ufoTractorBeamSizeModifiers[3] = 50;
		_ufoTractorBeamSizeModifiers[4] = 25;

		_pilotBraveryThresholds[0] = 90;
		_pilotBraveryThresholds[1] = 80;
		_pilotBraveryThresholds[2] = 30;
	}

	/// <summary>Load base damage types.</summary>
	private void CreateBaseDamageTypes()
	{
		RuleDamageType dmg;
		for (int i = 0; i < (int)ItemDamageType.DAMAGE_TYPES; ++i)
			_damageTypes.Add(null);

		dmg = new RuleDamageType();
		dmg.ResistType = ItemDamageType.DT_NONE;
		dmg.RandomType = ItemDamageRandomType.DRT_NONE;
		_damageTypes[(int)dmg.ResistType] = dmg;

		dmg = new RuleDamageType();
		dmg.ResistType = ItemDamageType.DT_AP;
		dmg.IgnoreOverKill = true;
		_damageTypes[(int)dmg.ResistType] = dmg;

		dmg = new RuleDamageType();
		dmg.ResistType = ItemDamageType.DT_ACID;
		dmg.IgnoreOverKill = true;
		_damageTypes[(int)dmg.ResistType] = dmg;

		dmg = new RuleDamageType();
		dmg.ResistType = ItemDamageType.DT_LASER;
		dmg.IgnoreOverKill = true;
		_damageTypes[(int)dmg.ResistType] = dmg;

		dmg = new RuleDamageType();
		dmg.ResistType = ItemDamageType.DT_PLASMA;
		dmg.IgnoreOverKill = true;
		_damageTypes[(int)dmg.ResistType] = dmg;

		dmg = new RuleDamageType();
		dmg.ResistType = ItemDamageType.DT_MELEE;
		dmg.IgnoreOverKill = true;
		dmg.IgnoreSelfDestruct = true;
		_damageTypes[(int)dmg.ResistType] = dmg;

		dmg = new RuleDamageType();
		dmg.ResistType = ItemDamageType.DT_STUN;
		dmg.FixRadius = -1;
		dmg.IgnoreOverKill = true;
		dmg.IgnoreSelfDestruct = true;
		dmg.IgnorePainImmunity = true;
		dmg.RadiusEffectiveness = 0.05f;
		dmg.ToHealth = 0.0f;
		dmg.ToArmor = 0.0f;
		dmg.ToWound = 0.0f;
		dmg.ToItem = 0.0f;
		dmg.ToTile = 0.0f;
		dmg.ToStun = 1.0f;
		dmg.RandomStun = false;
		dmg.TileDamageMethod = 2;
		_damageTypes[(int)dmg.ResistType] = dmg;

		dmg = new RuleDamageType();
		dmg.ResistType = ItemDamageType.DT_HE;
		dmg.RandomType = ItemDamageRandomType.DRT_EXPLOSION;
		dmg.FixRadius = -1;
		dmg.IgnoreOverKill = true;
		dmg.IgnoreSelfDestruct = true;
		dmg.RadiusEffectiveness = 0.05f;
		dmg.ToItem = 1.0f;
		dmg.TileDamageMethod = 2;
		_damageTypes[(int)dmg.ResistType] = dmg;

		dmg = new RuleDamageType();
		dmg.ResistType = ItemDamageType.DT_SMOKE;
		dmg.RandomType = ItemDamageRandomType.DRT_NONE;
		dmg.FixRadius = -1;
		dmg.IgnoreOverKill = true;
		dmg.IgnoreDirection = true;
		dmg.ArmorEffectiveness = 0.0f;
		dmg.RadiusEffectiveness = 0.05f;
		dmg.SmokeThreshold = 0;
		dmg.ToHealth = 0.0f;
		dmg.ToArmor = 0.0f;
		dmg.ToWound = 0.0f;
		dmg.ToItem = 0.0f;
		dmg.ToTile = 0.0f;
		dmg.ToStun = 1.0f;
		dmg.TileDamageMethod = 2;
		_damageTypes[(int)dmg.ResistType] = dmg;

		dmg = new RuleDamageType();
		dmg.ResistType = ItemDamageType.DT_IN;
		dmg.RandomType = ItemDamageRandomType.DRT_FIRE;
		dmg.FixRadius = -1;
		dmg.FireBlastCalc = true;
		dmg.IgnoreOverKill = true;
		dmg.IgnoreDirection = true;
		dmg.IgnoreSelfDestruct = true;
		dmg.ArmorEffectiveness = 0.0f;
		dmg.RadiusEffectiveness = 0.03f;
		dmg.FireThreshold = 0;
		dmg.ToHealth = 1.0f;
		dmg.ToArmor = 0.0f;
		dmg.ToWound = 0.0f;
		dmg.ToItem = 0.0f;
		dmg.ToTile = 0.0f;
		dmg.ToStun = 0.0f;
		dmg.TileDamageMethod = 2;
		_damageTypes[(int)dmg.ResistType] = dmg;

		for (int itd = (int)ItemDamageType.DT_10; itd < (int)ItemDamageType.DAMAGE_TYPES; ++itd)
		{
			dmg = new RuleDamageType();
			dmg.ResistType = (ItemDamageType)itd;
			dmg.IgnoreOverKill = true;
			_damageTypes[(int)dmg.ResistType] = dmg;
		}
	}

	/// <summary>For internal use only.</summary>
	public SortedDictionary<string, int> UfopaediaSections => _ufopaediaSections;

	/// <summary>
	/// Gets a specific rule element by ID.
	/// </summary>
	/// <param name="id">String ID of the rule element.</param>
	/// <param name="name">Human-readable name of the rule type.</param>
	/// <param name="map">Map associated to the rule type.</param>
	/// <param name="error">Throw an error if not found.</param>
	/// <returns>The rule element, or null if not found.</returns>
	private static T GetRule<T>(string id, string name, SortedDictionary<string, T> map, bool error) where T : class
	{
		if (IsEmptyRuleName(id))
		{
			return null;
		}
		if (map.TryGetValue(id, out var rule) && rule != null)
		{
			return rule;
		}
		else
		{
			if (error)
			{
				throw new InvalidOperationException(name + " " + id + " not found");
			}
			return null;
		}
	}

	/// <summary>C++ <c>std::map::operator[]</c> read access on a map of pointers: inserts null for a missing key.</summary>
	private static T GetOrInsertNull<T>(SortedDictionary<string, T> map, string key) where T : class
	{
		if (!map.TryGetValue(key, out var value))
		{
			map[key] = null;
		}
		return value;
	}

	/// <summary>C++ <c>std::map::operator[]</c> on a map of containers: inserts an empty one for a missing key.</summary>
	private static TValue GetOrAdd<TKey, TValue>(SortedDictionary<TKey, TValue> map, TKey key) where TValue : new()
	{
		if (!map.TryGetValue(key, out var value))
		{
			value = new TValue();
			map[key] = value;
		}
		return value;
	}

	/// <summary>Convert names to correct rule objects.</summary>
	public void LinkRule<T>(ref T rule, ref string name) where T : class
	{
		object linked;
		if (typeof(T) == typeof(RuleItem))
			linked = GetItem(name, true);
		else if (typeof(T) == typeof(RuleSoldier))
			linked = GetSoldier(name, true);
		else if (typeof(T) == typeof(RuleSoldierBonus))
			linked = GetSoldierBonus(name, true);
		else if (typeof(T) == typeof(Armor))
			linked = GetArmor(name, true);
		else if (typeof(T) == typeof(RuleResearch))
			linked = GetResearch(name, true);
		else if (typeof(T) == typeof(Unit))
			linked = GetUnit(name, true);
		else if (typeof(T) == typeof(RuleSkill))
			linked = GetSkill(name, true);
		else if (typeof(T) == typeof(RuleCraft))
			linked = GetCraft(name, true);
		else if (typeof(T) == typeof(RuleUfo))
			linked = GetUfo(name, true);
		else if (typeof(T) == typeof(RuleBaseFacility))
			linked = GetBaseFacility(name, true);
		else if (typeof(T) == typeof(RuleInventory))
			linked = GetInventory(name, true);
		else if (typeof(T) == typeof(RuleEvent))
			linked = GetEvent(name, true);
		else if (typeof(T) == typeof(RuleCommendations))
			linked = GetCommendation(name, true);
		else
			throw new NotSupportedException("Unsupported type to link: " + typeof(T).Name);
		rule = (T)linked;
		name = "";
	}

	/// <summary>Convert names to correct rule objects (appends to <paramref name="rule"/>, clears <paramref name="names"/>).</summary>
	public void LinkRule<T>(List<T> rule, List<string> names) where T : class
	{
		ArgumentNullException.ThrowIfNull(rule);
		ArgumentNullException.ThrowIfNull(names);
		rule.Capacity = Math.Max(rule.Capacity, rule.Count + names.Count);
		foreach (var n in names)
		{
			T r = null;
			string name = n;
			LinkRule(ref r, ref name);
			rule.Add(r);
		}
		// C++: names = {}
		names.Clear();
	}

	/// <summary>Convert names to correct rule objects (appends to <paramref name="rule"/>, clears <paramref name="names"/>).</summary>
	public void LinkRule<T>(List<List<T>> rule, List<List<string>> names) where T : class
	{
		ArgumentNullException.ThrowIfNull(rule);
		ArgumentNullException.ThrowIfNull(names);
		rule.Capacity = Math.Max(rule.Capacity, rule.Count + names.Count);
		foreach (var n in names)
		{
			var inner = new List<T>();
			LinkRule(inner, n);
			rule.Add(inner);
		}
		// C++: names = {}
		names.Clear();
	}

	/// <summary>Generates a brand new saved game with starting data.</summary>
	/// <returns>A new saved game.</returns>
	public SavedGame NewSave(GameDifficulty diff)
	{
		SavedGame save = new SavedGame();
		save.Difficulty = diff;

		// Add countries
		foreach (var countryName in _countriesIndex)
		{
			RuleCountry countryRule = GetCountry(countryName);
			if (countryRule.LonMin.Count != 0)
				save.Countries.Add(new Country(countryRule));
		}
		// Adjust funding to total $6M
		int missing = ((_initialFunding - save.CountryFunding / 1000) / save.Countries.Count) * 1000;
		foreach (var country in save.Countries)
		{
			int funding = country.Funding[^1] + missing;
			if (funding < 0)
			{
				funding = country.Funding[^1];
			}
			country.SetFunding(funding);
		}
		save.Funds = save.CountryFunding;

		// Add regions
		foreach (var regionName in _regionsIndex)
		{
			RuleRegion regionRule = GetRegion(regionName);
			if (regionRule.LonMin.Count != 0)
				save.Regions.Add(new Region(regionRule));
		}

		// Set up starting base
		var startingBaseByDiff = new YamlRootNodeReader(GetStartingBase(diff), "(starting base template)");
		Base @base = new Base(this);
		@base.Load(startingBaseByDiff.ToBase(), save, true);
		var globalTemplates = startingBaseByDiff["globalTemplates"];
		if (globalTemplates)
		{
			save.LoadTemplates(globalTemplates, this);
		}
		var ufopediaRuleStatus = startingBaseByDiff["ufopediaRuleStatus"];
		if (ufopediaRuleStatus)
		{
			save.LoadUfopediaRuleStatus(ufopediaRuleStatus);
		}
		save.Bases.Add(@base);

		// Correct IDs
		foreach (var craft in @base.Crafts)
		{
			save.GetId(craft.Rules.Type);
		}

		// Correct soldier IDs
		for (int i = 0; i < @base.Soldiers.Count; ++i)
		{
			save.GetId("STR_SOLDIER");
		}

		// Remove craft weapons if needed
		foreach (var craft in @base.Crafts)
		{
			if (craft.MaxUnitsRaw < 0 || craft.MaxVehiclesAndLargeSoldiersRaw < 0)
			{
				int weaponIndex = 0;
				foreach (var current in craft.Weapons.ToList())
				{
					@base.StorageItems.AddItem(current.Rules.LauncherItem);
					@base.StorageItems.AddItem(current.Rules.ClipItem, current.ClipsLoaded);
					craft.AddCraftStats(-current.Rules.BonusStats);
					craft.Shield = craft.Shield;
					craft.Weapons[weaponIndex] = null;
					weaponIndex++;
				}
			}
		}

		// Determine starting soldier types
		var soldierTypes = new List<string>(_soldiersIndex); // copy!
		for (int iter = 0; iter < soldierTypes.Count;)
		{
			if (GetSoldier(soldierTypes[iter]).Requirements.Count == 0)
			{
				++iter;
			}
			else
			{
				soldierTypes.RemoveAt(iter);
			}
		}

		var randomSoldiersReader = startingBaseByDiff["randomSoldiers"];
		var randomTypes = new List<string>();
		if (randomSoldiersReader)
		{
			// Starting soldiers specified by type
			if (randomSoldiersReader.IsMap)
			{
				var randomSoldiers = new SortedDictionary<string, int>(StringComparer.Ordinal);
				randomSoldiersReader.TryReadVal(ref randomSoldiers);
				foreach (var pair in randomSoldiers)
				{
					for (int s = 0; s < pair.Value; ++s)
					{
						randomTypes.Add(pair.Key);
					}
				}
			}
			// Starting soldiers specified by amount
			else if (randomSoldiersReader.HasVal)
			{
				int randomSoldiers = randomSoldiersReader.ReadVal(0);
				if (randomSoldiers > 0 && soldierTypes.Count == 0)
				{
					Logger.Error("Cannot generate soldiers for the starting base. There are no available soldier types. Maybe all of them are locked by research?");
				}
				else
				{
					for (int s = 0; s < randomSoldiers; ++s)
					{
						randomTypes.Add(soldierTypes[Rng.Generate(0, soldierTypes.Count - 1)]);
					}
				}
			}
			// Generate soldiers
			for (int i = 0; i < randomTypes.Count; ++i)
			{
				RuleSoldier ruleSoldier = GetSoldier(randomTypes[i], true);
				int nationality = save.SelectSoldierNationalityByLocation(this, ruleSoldier, null); // -1 (unfortunately the first base is not placed yet)
				Soldier soldier = GenSoldier(save, ruleSoldier, nationality);
				@base.Soldiers.Add(soldier);
				// Award soldier a special 'original eight' commendation
				if (_commendations.ContainsKey("STR_MEDAL_ORIGINAL8_NAME"))
				{
					SoldierDiary diary = soldier.Diary;
					diary.AwardOriginalEightCommendation(this);
					foreach (var comm in diary.SoldierCommendations)
					{
						comm.MakeOld();
					}
				}
			}
			// Assign pilots to craft (interceptors first, transport last) and non-pilots to transports only
			foreach (var soldier in @base.Soldiers)
			{
				if (soldier.Armor.Size > 1)
				{
					// "Large soldiers" just stay in the base
				}
				else
				{
					if (soldier.Rules.AllowPiloting)
					{
						soldier.PrepareStatsWithBonuses(this); // refresh stats for checking pilot requirements
					}

					Craft found = null;
					foreach (var craft in @base.Crafts)
					{
						CraftPlacementErrors err = craft.ValidateAddingSoldier(craft.SpaceAvailable, soldier);
						if (found == null && craft.Rules.AllowLanding && err == CraftPlacementErrors.CPE_None)
						{
							// Remember transporter as fall-back, but search further for interceptors
							found = craft;
						}
						if (!craft.Rules.AllowLanding && err == CraftPlacementErrors.CPE_None && craft.SpaceUsed < craft.Rules.Pilots)
						{
							// Fill interceptors with minimum amount of pilots necessary
							if (soldier.HasAllPilotingRequirements(craft))
							{
								found = craft;
								break;
							}
						}
					}
					soldier.SetCraft(found);
				}
			}
		}

		// Setup alien strategy
		save.AlienStrategy.Init(this);
		save.Time = _startingTime;

		return save;
	}

	/// <summary>Returns the rules for the specified country.</summary>
	public RuleCountry GetCountry(string id, bool error = false) => GetRule(id, "Country", _countries, error);

	/// <summary>Returns the list of all countries provided by the mod.</summary>
	public List<string> CountriesList => _countriesIndex;

	/// <summary>Returns the rules for the specified extra globe label.</summary>
	public RuleCountry GetExtraGlobeLabel(string id, bool error = false) => GetRule(id, "Extra Globe Label", _extraGlobeLabels, error);

	/// <summary>Returns the list of all extra globe labels provided by the mod.</summary>
	public List<string> ExtraGlobeLabelsList => _extraGlobeLabelsIndex;

	/// <summary>Returns the rules for the specified region.</summary>
	public RuleRegion GetRegion(string id, bool error = false) => GetRule(id, "Region", _regions, error);

	/// <summary>Returns the list of all regions provided by the mod.</summary>
	public List<string> RegionsList => _regionsIndex;

	/// <summary>Returns the rules for the specified base facility.</summary>
	public RuleBaseFacility GetBaseFacility(string id, bool error = false) => GetRule(id, "Facility", _facilities, error);

	/// <summary>Returns the list of all base facilities provided by the mod.</summary>
	public List<string> BaseFacilitiesList => _facilitiesIndex;

	/// <summary>Returns the rules for the specified craft.</summary>
	public RuleCraft GetCraft(string id, bool error = false) => GetRule(id, "Craft", _crafts, error);

	/// <summary>Returns the list of all crafts provided by the mod.</summary>
	public List<string> CraftsList => _craftsIndex;

	/// <summary>Returns the rules for the specified craft weapon.</summary>
	public RuleCraftWeapon GetCraftWeapon(string id, bool error = false) => GetRule(id, "Craft Weapon", _craftWeapons, error);

	/// <summary>Returns the list of all craft weapons provided by the mod.</summary>
	public List<string> CraftWeaponsList => _craftWeaponsIndex;

	/// <summary>Is given item a launcher or ammo for craft weapon.</summary>
	public bool IsCraftWeaponStorageItem(RuleItem item) => item != null && _craftWeaponStorageItemsCache.Contains(item);

	/// <summary>Returns the rules for the specified item category, or null when the item category is not found.</summary>
	public RuleItemCategory GetItemCategory(string id, bool error = false)
	{
		if (id != null && _itemCategories.TryGetValue(id, out var i)) return i; else return null;
	}

	/// <summary>Returns the list of all item categories provided by the mod.</summary>
	public List<string> ItemCategoriesList => _itemCategoriesIndex;

	/// <summary>Returns the rules for the specified item, or null when the item is not found.</summary>
	public RuleItem GetItem(string id, bool error = false)
	{
		if (id == Armor.NONE)
		{
			return null;
		}
		return GetRule(id, "Item", _items, error);
	}

	/// <summary>Returns the list of all items provided by the mod.</summary>
	public List<string> ItemsList => _itemsIndex;

	/// <summary>Returns the rules for the specified weapon set.</summary>
	public RuleWeaponSet GetWeaponSet(string type, bool error = false) => GetRule(type, "WeaponSet", _weaponSets, error);

	/// <summary>Returns the rules for the specified UFO.</summary>
	public RuleUfo GetUfo(string id, bool error = false) => GetRule(id, "UFO", _ufos, error);

	/// <summary>Returns the list of all ufos provided by the mod.</summary>
	public List<string> UfosList => _ufosIndex;

	/// <summary>Returns the rules for the specified terrain.</summary>
	public RuleTerrain GetTerrain(string name, bool error = false) => GetRule(name, "Terrain", _terrains, error);

	/// <summary>Returns the list of all terrains provided by the mod.</summary>
	public List<string> TerrainList => _terrainIndex;

	/// <summary>Returns the info about a specific map data file.</summary>
	public MapDataSet GetMapDataSet(string name)
	{
		if (!_mapDataSets.TryGetValue(name, out var map))
		{
			MapDataSet set = new MapDataSet(name);
			_mapDataSets[name] = set;
			return set;
		}
		else
		{
			return map;
		}
	}

	/// <summary>Returns the rules for the specified skill.</summary>
	public RuleSkill GetSkill(string name, bool error = false) => GetRule(name, "Skill", _skills, error);

	/// <summary>Returns the info about a specific unit.</summary>
	public RuleSoldier GetSoldier(string name, bool error = false) => GetRule(name, "Soldier", _soldiers, error);

	/// <summary>Returns the list of all soldiers provided by the mod.</summary>
	public List<string> SoldiersList => _soldiersIndex;

	/// <summary>Returns the rules for the specified commendation.</summary>
	public RuleCommendations GetCommendation(string id, bool error = false) => GetRule(id, "Commendation", _commendations, error);

	/// <summary>Gets the list of commendations provided by the mod.</summary>
	public SortedDictionary<string, RuleCommendations> CommendationsList => _commendations;

	/// <summary>Returns the info about a specific unit.</summary>
	public Unit GetUnit(string name, bool error = false) => GetRule(name, "Unit", _units, error);

	/// <summary>Returns the info about a specific alien race.</summary>
	public AlienRace GetAlienRace(string name, bool error = false) => GetRule(name, "Alien Race", _alienRaces, error);

	/// <summary>Returns the list of all alien races provided by the mod.</summary>
	public List<string> AlienRacesList => _aliensIndex;

	/// <summary>Returns specific EnviroEffects.</summary>
	public RuleEnviroEffects GetEnviroEffects(string name)
	{
		if (name != null && _enviroEffects.TryGetValue(name, out var i)) return i; else return null;
	}

	/// <summary>Returns the list of all EnviroEffects provided by the mod.</summary>
	public List<string> EnviroEffectsList => _enviroEffectsIndex;

	/// <summary>Returns the info about a specific starting condition.</summary>
	public RuleStartingCondition GetStartingCondition(string name)
	{
		if (name != null && _startingConditions.TryGetValue(name, out var i)) return i; else return null;
	}

	/// <summary>Returns the list of all starting conditions provided by the mod.</summary>
	public List<string> StartingConditionsList => _startingConditionsIndex;

	/// <summary>Returns the info about a specific deployment.</summary>
	public AlienDeployment GetDeployment(string name, bool error = false) => GetRule(name, "Alien Deployment", _alienDeployments, error);

	/// <summary>Returns the list of all alien deployments provided by the mod.</summary>
	public List<string> DeploymentsList => _deploymentsIndex;

	/// <summary>Returns the info about a specific armor.</summary>
	public Armor GetArmor(string name, bool error = false) => GetRule(name, "Armor", _armors, error);

	/// <summary>Returns the list of all armors provided by the mod.</summary>
	public List<string> ArmorsList => _armorsIndex;

	/// <summary>Gets the available armors for soldiers.</summary>
	public List<Armor> ArmorsForSoldiers => _armorsForSoldiersCache;

	/// <summary>Check if item is used for armor storage.</summary>
	public bool IsArmorStorageItem(RuleItem item) => item != null && _armorStorageItemsCache.Contains(item);

	/// <summary>Returns the article definition for a given name.</summary>
	public ArticleDefinition GetUfopaediaArticle(string name, bool error = false) => GetRule(name, "UFOpaedia Article", _ufopaediaArticles, error);

	/// <summary>Returns the list of all articles provided by the mod.</summary>
	public List<string> UfopaediaList => _ufopaediaIndex;

	/// <summary>Returns the list of all article categories provided by the mod.</summary>
	public List<string> UfopaediaCategoryList => _ufopaediaCatIndex;

	/// <summary>Returns the list of inventories.</summary>
	public SortedDictionary<string, RuleInventory> Inventories => _invs;

	/// <summary>Returns the rules for a specific inventory.</summary>
	public RuleInventory GetInventory(string id, bool error = false) => GetRule(id, "Inventory", _invs, error);

	/// <summary>Gets the ruleset for right hand inventory slot.</summary>
	public RuleInventory InventoryRightHand => GetInventory("STR_RIGHT_HAND", true);
	/// <summary>Gets the ruleset for left hand inventory slot.</summary>
	public RuleInventory InventoryLeftHand => GetInventory("STR_LEFT_HAND", true);
	/// <summary>Gets the ruleset for backpack inventory slot.</summary>
	public RuleInventory InventoryBackpack => GetInventory("STR_BACK_PACK", true);
	/// <summary>Gets the ruleset for belt inventory slot.</summary>
	public RuleInventory InventoryBelt => GetInventory("STR_BELT", true);
	/// <summary>Gets the ruleset for ground inventory slot.</summary>
	public RuleInventory InventoryGround => GetInventory("STR_GROUND", true);

	/// <summary>Gets whether or not the inventory slots overlap with the paperdoll button.</summary>
	public bool InventoryOverlapsPaperdoll => _inventoryOverlapsPaperdoll;
	/// <summary>Gets max view distance in BattleScape.</summary>
	public int MaxViewDistance => _maxViewDistance;
	/// <summary>Gets threshold of darkness for LoS calculation.</summary>
	public int MaxDarknessToSeeUnits => _maxDarknessToSeeUnits;
	/// <summary>Gets max static (tiles &amp; fire) light distance in BattleScape.</summary>
	public int MaxStaticLightDistance => _maxStaticLightDistance;
	/// <summary>Gets max dynamic (items &amp; units) light distance in BattleScape.</summary>
	public int MaxDynamicLightDistance => _maxDynamicLightDistance;
	/// <summary>Get flags for enhanced lighting, 0x1 - tiles and fire, 0x2 - items, 0x4 - units.</summary>
	public int EnhancedLighting => _enhancedLighting;

	/// <summary>Returns basic damage type.</summary>
	public RuleDamageType GetDamageType(ItemDamageType type) => _damageTypes[(int)type];

	/// <summary>Returns the hiring cost of an individual engineer.</summary>
	public int HireEngineerCost => _costHireEngineer != 0 ? _costHireEngineer : _costEngineer * 2;
	/// <summary>Gets the research topic required for hiring new engineers.</summary>
	public string HireEngineersUnlockResearch => _hireEngineersUnlockResearch;
	/// <summary>Gets the base functions required for hiring new engineers.</summary>
	public RuleBaseFacilityFunctions HireEngineersRequiresBaseFunc => _hireEngineersRequiresBaseFunc;
	/// <summary>Returns the hiring cost of an individual scientist.</summary>
	public int HireScientistCost => _costHireScientist != 0 ? _costHireScientist : _costScientist * 2;
	/// <summary>Gets the research topic required for hiring new scientists.</summary>
	public string HireScientistsUnlockResearch => _hireScientistsUnlockResearch;
	/// <summary>Gets the base functions topic required for hiring new scientists.</summary>
	public RuleBaseFacilityFunctions HireScientistsRequiresBaseFunc => _hireScientistsRequiresBaseFunc;
	/// <summary>Returns the monthly cost of an individual engineer.</summary>
	public int EngineerCost => _costEngineer;
	/// <summary>Returns the monthly cost of an individual scientist.</summary>
	public int ScientistCost => _costScientist;
	/// <summary>Returns the time it takes to transfer personnel between bases (hours).</summary>
	public int PersonnelTime => _timePersonnel;
	/// <summary>Gets the odds of hiring soldiers by country.</summary>
	public int HireByCountryOdds => _hireByCountryOdds;
	/// <summary>Gets the odds of hiring soldiers by region.</summary>
	public int HireByRegionOdds => _hireByRegionOdds;

	/// <summary>Gets first turn when AI can use Blaster launcher.</summary>
	public int AIUseDelayBlaster => _aiUseDelayBlaster;
	/// <summary>Gets first turn when AI can use firearms.</summary>
	public int AIUseDelayFirearm => _aiUseDelayFirearm;
	/// <summary>Gets first turn when AI can use grenades.</summary>
	public int AIUseDelayGrenade => _aiUseDelayGrenade;
	/// <summary>Gets first turn when AI can use proxy grenades.</summary>
	public int AIUseDelayProxy => _aiUseDelayProxy;
	/// <summary>Gets first turn when AI can use martial arts.</summary>
	public int AIUseDelayMelee => _aiUseDelayMelee;
	/// <summary>Gets first turn when AI can use psionic abilities.</summary>
	public int AIUseDelayPsionic => _aiUseDelayPsionic;
	/// <summary>Gets first turn when AI can use self-target medikits.</summary>
	public int AIUseDelayMedikit => _aiUseDelayMedikit;
	/// <summary>Gets how much AI intelligence should be used to determine firing mode for sniping.</summary>
	public int AIFireChoiceIntelCoeff => _aiFireChoiceIntelCoeff;
	/// <summary>Gets how much AI aggression should be used to determine firing mode for sniping.</summary>
	public int AIFireChoiceAggroCoeff => _aiFireChoiceAggroCoeff;
	/// <summary>Gets whether or not to use extended firing mode scoring for determining which attack the AI should use.</summary>
	public bool AIExtendedFireModeChoice => _aiExtendedFireModeChoice;
	/// <summary>Gets whether or not the AI should try to shoot beyond a weapon's max range, true = don't shoot if you can't.</summary>
	public bool AIRespectMaxRange => _aiRespectMaxRange;
	/// <summary>Gets whether or not the AI should be allowed to continue destroying base facilities after first encountering XCom.</summary>
	public bool AIDestroyBaseFacilities => _aiDestroyBaseFacilities;
	/// <summary>Gets whether or not the alien AI should pick up weapons more actively.</summary>
	public bool AIPickUpWeaponsMoreActively => _aiPickUpWeaponsMoreActively;
	/// <summary>Gets whether or not the civilian AI should pick up weapons more actively.</summary>
	public bool AIPickUpWeaponsMoreActivelyCiv => _aiPickUpWeaponsMoreActivelyCiv;

	/// <summary>Returns the reaction fire threshold for a given faction (default = 0).</summary>
	public int GetReactionFireThreshold(UnitFaction faction)
	{
		switch (faction)
		{
			case UnitFaction.FACTION_PLAYER:
				return Options.oxceReactionFireThreshold;
			case UnitFaction.FACTION_HOSTILE:
				return _aiReactionFireThreshold;
			case UnitFaction.FACTION_NEUTRAL:
				return _aiReactionFireThresholdCiv;
			default:
				break;
		}

		return 0;
	}

	/// <summary>Gets weight value that AI use to determine if target is dangerous.</summary>
	public AIAttackWeight AITargetWeightThreatThreshold => _aiTargetWeightThreatThreshold;
	/// <summary>Gets default weight value of hostile unit.</summary>
	public AIAttackWeight AITargetWeightAsHostile => _aiTargetWeightAsHostile;
	/// <summary>Gets default weight value of civilian unit when consider by aliens.</summary>
	public AIAttackWeight AITargetWeightAsHostileCivilians => _aiTargetWeightAsHostileCivilians;
	/// <summary>Gets default weight value of same faction unit.</summary>
	public AIAttackWeight AITargetWeightAsFriendly => _aiTargetWeightAsFriendly;
	/// <summary>Gets default weight value of neutral unit (xcom to civ or vice versa).</summary>
	public AIAttackWeight AITargetWeightAsNeutral => _aiTargetWeightAsNeutral;

	/// <summary>Gets maximum supported lookVariant (value in range from 0 to 63).</summary>
	public int MaxLookVariant => Math.Abs(_maxLookVariant) % RuleSoldier.LookVariantMax;
	/// <summary>Gets the threshold for too much smoke (vanilla default = 10).</summary>
	public int TooMuchSmokeThreshold => _tooMuchSmokeThreshold;
	/// <summary>Gets the custom physical training factor in percent (default = 100).</summary>
	public int CustomTrainingFactor => _customTrainingFactor;
	/// <summary>Gets the chance to stop retaliation after unsuccessful xcom base attack (default = 0).</summary>
	public int ChanceToStopRetaliation => _chanceToStopRetaliation;
	/// <summary>Gets the chance to detect an alien base by xcom operatives each month (default = 20).</summary>
	public int ChanceToDetectAlienBaseEachMonth => _chanceToDetectAlienBaseEachMonth;
	/// <summary>Should a damaged UFO deploy less aliens during the base defense?</summary>
	public bool LessAliensDuringBaseDefense => _lessAliensDuringBaseDefense;
	/// <summary>Will countries join the good side again after the infiltrator base is destroyed?</summary>
	public bool AllowCountriesToCancelAlienPact => _allowCountriesToCancelAlienPact;
	/// <summary>Should alien infiltration bases be built close to the infiltrated country?</summary>
	public bool BuildInfiltrationBaseCloseToTheCountry => _buildInfiltrationBaseCloseToTheCountry;
	/// <summary>Should alien infiltration pick countries in a region randomly (or in a pre-defined ruleset order)?</summary>
	public bool InfiltrateRandomCountryInTheRegion => _infiltrateRandomCountryInTheRegion;
	/// <summary>Should alien bases be allowed (in worst case) on invalid globe textures or not?</summary>
	public bool AllowAlienBasesOnWrongTextures => _allowAlienBasesOnWrongTextures;
	/// <summary>Gets the global kneel bonus (default = 115).</summary>
	public int KneelBonusGlobal => _kneelBonusGlobal;
	/// <summary>Gets the global one-handed penalty (default = 80).</summary>
	public int OneHandedPenaltyGlobal => _oneHandedPenaltyGlobal;
	/// <summary>Gets whether close quarters combat is enabled (default = 0 is off).</summary>
	public int EnableCloseQuartersCombat => _enableCloseQuartersCombat;
	/// <summary>Gets the default close quarters combat accuracy (default = 100).</summary>
	public int CloseQuartersAccuracyGlobal => _closeQuartersAccuracyGlobal;
	/// <summary>Gets the default close quarters combat TU cost (default = 12).</summary>
	public int CloseQuartersTuCostGlobal => _closeQuartersTuCostGlobal;
	/// <summary>Gets the default close quarters combat energy cost (default = 8).</summary>
	public int CloseQuartersEnergyCostGlobal => _closeQuartersEnergyCostGlobal;
	/// <summary>Gets the percentage for successfully avoiding CQC when sneaking up on the enemy (default = 0% = turned off).</summary>
	public int CloseQuartersSneakUpGlobal => _closeQuartersSneakUpGlobal;
	/// <summary>Gets the default accuracy penalty for having no LOS to the target (default = 0 is no penalty).</summary>
	public int NoLOSAccuracyPenaltyGlobal => _noLOSAccuracyPenaltyGlobal;
	/// <summary>Gets the default setting for primed grenades exploding in the inventory (default = 0 is no explosion).</summary>
	public int ExplodeInventoryGlobal => _explodeInventoryGlobal;
	/// <summary>Gets the surrender mode (default = 0).</summary>
	public int SurrenderMode => _surrenderMode;
	/// <summary>Gets the bug hunt mode minimum turn requirement (default = 20).</summary>
	public int BughuntMinTurn => _bughuntMinTurn;
	/// <summary>Gets the bug hunt mode maximum remaining enemies requirement (default = 2).</summary>
	public int BughuntMaxEnemies => _bughuntMaxEnemies;
	/// <summary>Gets the bug hunt mode "VIP rank" parameter (default = 0).</summary>
	public int BughuntRank => _bughuntRank;
	/// <summary>Gets the bug hunt mode low morale threshold parameter (default = 40).</summary>
	public int BughuntLowMorale => _bughuntLowMorale;
	/// <summary>Gets the bug hunt mode time units % parameter (default = 60).</summary>
	public int BughuntTimeUnitsLeft => _bughuntTimeUnitsLeft;

	/// <summary>Is the mana feature enabled (default false)?</summary>
	public bool IsManaFeatureEnabled => _manaEnabled;
	/// <summary>Is the mana bar enabled for the Battlescape UI (default false)?</summary>
	public bool IsManaBarEnabled => _manaBattleUI;
	/// <summary>Is the mana trained as a primary skill (e.g. like firing accuracy)?</summary>
	public bool IsManaTrainingPrimary => _manaTrainingPrimary;
	/// <summary>Is the mana trained as a secondary skill (e.g. like strength)?</summary>
	public bool IsManaTrainingSecondary => _manaTrainingSecondary;
	/// <summary>Gets the mana unlock research topic (default empty)?</summary>
	public string ManaUnlockResearch => _manaUnlockResearch;

	/// <summary>How much missing mana will act as "fatal wounds" and prevent the soldier from going into battle.</summary>
	public int ManaWoundThreshold => _manaMissingWoundThreshold;
	/// <summary>Should a soldier's mana be fully replenished after a mission?</summary>
	public bool ReplenishManaAfterMission => _manaReplenishAfterMission;

	/// <summary>How much missing health will act as "fatal wounds" and prevent the soldier from going into battle.</summary>
	public int HealthWoundThreshold => _healthMissingWoundThreshold;
	/// <summary>Should a soldier's health be fully replenished after a mission?</summary>
	public bool ReplenishHealthAfterMission => _healthReplenishAfterMission;

	/// <summary>Gets the cutscene ID that should be played when the player loses due to poor economy management.</summary>
	public string LoseMoneyCutscene => _loseMoney;
	/// <summary>Gets the cutscene ID that should be played when the player loses due to poor rating.</summary>
	public string LoseRatingCutscene => _loseRating;
	/// <summary>Gets the cutscene ID that should be played when the player loses the last base.</summary>
	public string LoseDefeatCutscene => _loseDefeat;

	/// <summary>Gets the research topic required for building XCOM bases on fakeUnderwater globe textures.</summary>
	public string FakeUnderwaterBaseUnlockResearch => _fakeUnderwaterBaseUnlockResearch;
	/// <summary>Gets the research topic required for building XCOM bases.</summary>
	public string NewBaseUnlockResearch => _newBaseUnlockResearch;

	/// <summary>Gets the threshold for defining a glancing hit on a ufo during interception.</summary>
	public int UfoGlancingHitThreshold => _ufoGlancingHitThreshold;
	/// <summary>Gets the parameter for drawing the width of a ufo's beam weapon based on power.</summary>
	public int UfoBeamWidthParameter => _ufoBeamWidthParameter;
	/// <summary>Gets the modifier to a tractor beam's power based on a ufo's size.</summary>
	public int GetUfoTractorBeamSizeModifier(int ufoSize) => _ufoTractorBeamSizeModifiers[ufoSize];
	/// <summary>Gets the escort range.</summary>
	public double EscortRange => _escortRange;
	/// <summary>Gets the setting for drawing alien base/UFO radar circles.</summary>
	public int DrawEnemyRadarCircles => _drawEnemyRadarCircles;
	/// <summary>Should escorts join the fight against HK (automatically)? Or is only fighting one-on-one allowed?</summary>
	public bool EscortsJoinFightAgainstHK => _escortsJoinFightAgainstHK;
	/// <summary>Should hunter-killers be able to retarget every 5 seconds on slow game timers (5Sec / 1Min)?</summary>
	public bool HunterKillerFastRetarget => _hunterKillerFastRetarget;
	/// <summary>Gets the crew emergency evacuation survival chance.</summary>
	public int CrewEmergencyEvacuationSurvivalChance => _crewEmergencyEvacuationSurvivalChance;
	/// <summary>Gets the pilots emergency evacuation survival chance.</summary>
	public int PilotsEmergencyEvacuationSurvivalChance => _pilotsEmergencyEvacuationSurvivalChance;
	/// <summary>Should UFO preview be shown in base defense UI?</summary>
	public bool ShowUfoPreviewInBaseDefense() => _showUfoPreviewInBaseDefense;
	/// <summary>Gets how many soldiers are needed for one promotion of a given rank.</summary>
	public int GetSoldiersPerRank(SoldierRank rank) => _soldiersPerRank[(int)rank];
	/// <summary>Gets the firing accuracy needed for no bonus/penalty.</summary>
	public int PilotAccuracyZeroPoint => _pilotAccuracyZeroPoint;
	/// <summary>Gets the firing accuracy impact (as percentage of distance to zero point) on pilot's aim in dogfight.</summary>
	public int PilotAccuracyRange => _pilotAccuracyRange;
	/// <summary>Gets the reactions needed for no bonus/penalty.</summary>
	public int PilotReactionsZeroPoint => _pilotReactionsZeroPoint;
	/// <summary>Gets the reactions impact (as percentage of distance to zero point) on pilot's dodge ability in dogfight.</summary>
	public int PilotReactionsRange => _pilotReactionsRange;
	/// <summary>Gets the pilot's bravery needed for very bold approach speed.</summary>
	public int PilotBraveryThresholdVeryBold => _pilotBraveryThresholds[0];
	/// <summary>Gets the pilot's bravery needed for bold approach speed.</summary>
	public int PilotBraveryThresholdBold => _pilotBraveryThresholds[1];
	/// <summary>Gets the pilot's bravery needed for normal approach speed.</summary>
	public int PilotBraveryThresholdNormal => _pilotBraveryThresholds[2];
	/// <summary>Gets a performance bonus for a given score.</summary>
	public int GetPerformanceBonus(int score) => (int)(score * _performanceBonusFactor);
	/// <summary>Should the player have the option to sort the 'New Research' list?</summary>
	public bool EnableNewResearchSorting => _enableNewResearchSorting;
	/// <summary>Should custom categories be used in Buy/Sell/Transfer GUIs? 0=no, 1=yes, custom only, 2=both vanilla and custom.</summary>
	public int DisplayCustomCategories => _displayCustomCategories;
	/// <summary>Should weapons "inherit" categories of their ammo?</summary>
	public bool ShareAmmoCategories => _shareAmmoCategories;
	/// <summary>Should distance in dogfight GUI be shown in kilometers?</summary>
	public bool ShowDogfightDistanceInKm => _showDogfightDistanceInKm;
	/// <summary>Should alien inventory show full name (e.g. Sectoid Leader) or just the race (e.g. Sectoid)?</summary>
	public bool ShowFullNameInAlienInventory => _showFullNameInAlienInventory;
	/// <summary>Gets the offset for alien inventory paperdoll and hand slots.</summary>
	public int AlienInventoryOffsetX => _alienInventoryOffsetX;
	/// <summary>Gets the extra offset for alien inventory hand slots for 2x2 units.</summary>
	public int AlienInventoryOffsetBigUnit => _alienInventoryOffsetBigUnit;
	/// <summary>Show the INFO button (where applicable) or not?</summary>
	public bool ShowPediaInfoButton => !_hidePediaInfoButton;
	/// <summary>Display extra item info (accuracy modifier and power bonus) in the main pedia article?</summary>
	public int ExtraNerdyPediaInfoType => _extraNerdyPediaInfoType;
	/// <summary>In debriefing, give score also for already researched alien artifacts?</summary>
	public bool GiveScoreAlsoForResearchedArtifacts => _giveScoreAlsoForResearchedArtifacts;
	/// <summary>When recovering ammo, should partially spent clip have a chance to recover as full?</summary>
	public bool StatisticalBulletConservation => _statisticalBulletConservation;
	/// <summary>Does stunning an enemy improve unit and squad morale?</summary>
	public bool StunningImprovesMorale => _stunningImprovesMorale;
	/// <summary>Gets how much TU (in percent) should be given to a unit waking up from stun at the beginning of a new turn.</summary>
	public int TURecoveryWakeUpNewTurn => _tuRecoveryWakeUpNewTurn;
	/// <summary>Gets whether or not to load base defense terrain from globe texture.</summary>
	public int BaseDefenseMapFromLocation => _baseDefenseMapFromLocation;

	/// <summary>Return mod what created given rule object.</summary>
	public ModData GetModCreatingRule(object rule) => _ruleCreationTracking[rule];
	/// <summary>Return mod what last updated given rule object.</summary>
	public ModData GetModLastUpdatingRule(object rule) => _ruleLastUpdateTracking[rule];

	/// <summary>Returns the rules for the specified research project.</summary>
	public RuleResearch GetResearch(string id, bool error = false) => GetRule(id, "Research", _research, error);

	/// <summary>Gets the ruleset list for from research list.</summary>
	public List<RuleResearch> GetResearch(List<string> id)
	{
		ArgumentNullException.ThrowIfNull(id);
		var dest = new List<RuleResearch>(id.Count);
		foreach (var name in id)
		{
			var rule = GetResearch(name, false);
			if (rule != null)
			{
				dest.Add(rule);
			}
			else
			{
				throw new InvalidOperationException("Unknown research '" + name + "'");
			}
		}
		return dest;
	}

	/// <summary>Gets the ruleset for a specific research project.</summary>
	public SortedDictionary<string, RuleResearch> ResearchMap => _research;

	/// <summary>Returns the list of research projects.</summary>
	public List<string> ResearchList => _researchIndex;

	/// <summary>Returns the rules for the specified manufacture project.</summary>
	public RuleManufacture GetManufacture(string id, bool error = false) => GetRule(id, "Manufacture", _manufacture, error);

	/// <summary>Returns the list of manufacture projects.</summary>
	public List<string> ManufactureList => _manufactureIndex;

	/// <summary>Returns the rules for the specified soldier bonus type.</summary>
	public RuleSoldierBonus GetSoldierBonus(string id, bool error = false) => GetRule(id, "SoldierBonus", _soldierBonus, error);

	/// <summary>Returns the list of soldier bonus types.</summary>
	public List<string> SoldierBonusList => _soldierBonusIndex;

	/// <summary>Returns the rules for the specified soldier transformation project.</summary>
	public RuleSoldierTransformation GetSoldierTransformation(string id, bool error = false) => GetRule(id, "SoldierTransformation", _soldierTransformation, error);

	/// <summary>Returns the list of soldier transformation projects.</summary>
	public List<string> SoldierTransformationList => _soldierTransformationIndex;

	/// <summary>
	/// Generates and returns a list of facilities for custom bases.
	/// The list contains all the facilities that are listed in the 'startingBase'
	/// part of the ruleset.
	/// </summary>
	public List<RuleBaseFacility> GetCustomBaseFacilities(GameDifficulty diff)
	{
		var placeList = new List<RuleBaseFacility>();

		var startingBaseByDiff = new YamlRootNodeReader(GetStartingBase(diff), "(starting base template)");
		foreach (var facilityReader in startingBaseByDiff["facilities"].Children())
		{
			string type = facilityReader["type"].ReadVal<string>();
			RuleBaseFacility facility = GetBaseFacility(type, true);
			if (!facility.IsLift && !facility.IsUpgradeOnly)
			{
				placeList.Add(facility);
			}
		}
		return placeList;
	}

	/// <summary>Returns the data for the specified ufo trajectory.</summary>
	public UfoTrajectory GetUfoTrajectory(string id, bool error = false) => GetRule(id, "Trajectory", _ufoTrajectories, error);

	/// <summary>Returns the rules for the specified alien mission.</summary>
	public RuleAlienMission GetAlienMission(string id, bool error = false) => GetRule(id, "Alien Mission", _alienMissions, error);

	/// <summary>Returns the rules for a random alien mission based on a specific objective.</summary>
	public RuleAlienMission GetRandomMission(MissionObjective objective, int monthsPassed)
	{
		int totalWeight = 0;
		var possibilities = new SortedDictionary<int, RuleAlienMission>();
		foreach (var pair in _alienMissions)
		{
			if (pair.Value.Objective == objective && pair.Value.GetWeight(monthsPassed) > 0)
			{
				totalWeight += pair.Value.GetWeight(monthsPassed);
				possibilities[totalWeight] = pair.Value;
			}
		}
		if (totalWeight > 0)
		{
			int pick = Rng.Generate(1, totalWeight);
			foreach (var pair in possibilities)
			{
				if (pick <= pair.Key)
				{
					return pair.Value;
				}
			}
		}
		return null;
	}

	/// <summary>Returns the list of alien mission types.</summary>
	public List<string> AlienMissionList => _alienMissionsIndex;

	/// <summary>Gets the alien item level table.</summary>
	public List<List<int>> AlienItemLevels => _alienItemLevels;

	/// <summary>Gets the default starting base (YAML text).</summary>
	public string DefaultStartingBase => _startingBaseDefault;

	/// <summary>Gets the custom starting base (by game difficulty, YAML text).</summary>
	public string GetStartingBase(GameDifficulty diff)
	{
		if (diff == GameDifficulty.DIFF_BEGINNER && _startingBaseBeginner.Length != 0)
		{
			return _startingBaseBeginner;
		}
		else if (diff == GameDifficulty.DIFF_EXPERIENCED && _startingBaseExperienced.Length != 0)
		{
			return _startingBaseExperienced;
		}
		else if (diff == GameDifficulty.DIFF_VETERAN && _startingBaseVeteran.Length != 0)
		{
			return _startingBaseVeteran;
		}
		else if (diff == GameDifficulty.DIFF_GENIUS && _startingBaseGenius.Length != 0)
		{
			return _startingBaseGenius;
		}
		else if (diff == GameDifficulty.DIFF_SUPERHUMAN && _startingBaseSuperhuman.Length != 0)
		{
			return _startingBaseSuperhuman;
		}

		return _startingBaseDefault;
	}

	/// <summary>Gets the defined starting time.</summary>
	public GameTime StartingTime => _startingTime;

	/// <summary>Gets the game starting difficulty.</summary>
	public int StartingDifficulty => _startingDifficulty;

	/// <summary>Gets an MCDPatch, or null if none defined.</summary>
	public MCDPatch GetMCDPatch(string id)
	{
		if (id != null && _MCDPatches.TryGetValue(id, out var i)) return i; else return null;
	}

	/// <summary>Gets the list of external sprites.</summary>
	public SortedDictionary<string, List<ExtraSprites>> ExtraSprites => _extraSprites;

	/// <summary>Gets the list of custom palettes.</summary>
	public List<string> CustomPalettes => _customPalettesIndex;

	/// <summary>Gets the list of external sounds.</summary>
	public List<(string first, ExtraSounds second)> ExtraSounds => _extraSounds;

	/// <summary>Gets the list of external strings.</summary>
	public SortedDictionary<string, ExtraStrings> ExtraStrings => _extraStrings;

	/// <summary>Gets the list of StatStrings.</summary>
	public List<StatString> StatStrings => _statStrings;

	/// <summary>Strict weak ordering from a C++ "less" comparator.</summary>
	private static Comparison<T> FromLess<T>(Func<T, T, bool> less) => (a, b) => less(a, b) ? -1 : less(b, a) ? 1 : 0;

	/// <summary>
	/// Sorts like C++ <c>std::sort</c>. Deviation: the C++ sort is not stable, so the order of equal
	/// elements is implementation-defined; this sort is stable (equal elements keep their order).
	/// </summary>
	private static void SortStable<T>(List<T> list, Comparison<T> comparison)
	{
		var sorted = list.Order(Comparer<T>.Create(comparison)).ToList();
		list.Clear();
		list.AddRange(sorted);
	}

	/// <summary>Sort helper to avoid having to do loop-ups inside comparator function.</summary>
	private static void SortIndex<T>(List<string> index, SortedDictionary<string, T> map, Comparison<T> comparator)
	{
		var tempVector = map.ToList();
		SortStable(tempVector, (a, b) => comparator(a.Value, b.Value));
		for (int i = 0; i < index.Count; ++i)
			index[i] = tempVector[i].Key;
	}

	/// <summary>Compares rules based on their list orders.</summary>
	private static Comparison<T> CompareRule<T>(Func<T, int> listOrder) => FromLess<T>((r1, r2) => listOrder(r1) < listOrder(r2));

	/// <summary>
	/// Armor uses the list order of their store item.
	/// Itemless armor comes before all else.
	/// </summary>
	private static bool CompareArmor(Armor armor1, Armor armor2)
	{
		RuleItem rule1 = armor1.StoreItem;
		RuleItem rule2 = armor2.StoreItem;
		if (rule1 == null && rule2 == null)
			return armor1.ListOrder < armor2.ListOrder; // tiebreaker
		else if (rule1 == null)
			return true;
		else if (rule2 == null)
			return false;
		else
			return rule1.ListOrder < rule2.ListOrder ||
				(rule1.ListOrder == rule2.ListOrder && armor1.ListOrder < armor2.ListOrder);
	}

	/// <summary>Sorts all our lists according to their weight.</summary>
	private void SortLists()
	{
		foreach (var rulePair in _ufopaediaArticles)
		{
			var rule = rulePair.Value;
			if (rule.section != Ufopaedia.UFOPAEDIA_NOT_AVAILABLE)
			{
				if (!_ufopaediaSections.TryGetValue(rule.section, out int sectionOrder))
				{
					_ufopaediaSections[rule.section] = rule.ListOrder;
					_ufopaediaCatIndex.Add(rule.section);
				}
				else
				{
					_ufopaediaSections[rule.section] = Math.Min(sectionOrder, rule.ListOrder);
				}
			}
		}
		SortIndex(_itemCategoriesIndex, _itemCategories, CompareRule<RuleItemCategory>(r => r.ListOrder));
		SortIndex(_itemsIndex, _items, CompareRule<RuleItem>(r => r.ListOrder));
		SortIndex(_craftsIndex, _crafts, CompareRule<RuleCraft>(r => r.ListOrder));
		SortIndex(_facilitiesIndex, _facilities, CompareRule<RuleBaseFacility>(r => r.ListOrder));
		SortIndex(_researchIndex, _research, CompareRule<RuleResearch>(r => r.ListOrder));
		SortIndex(_manufactureIndex, _manufacture, CompareRule<RuleManufacture>(r => r.ListOrder));
		SortIndex(_soldierTransformationIndex, _soldierTransformation, CompareRule<RuleSoldierTransformation>(r => r.ListOrder));
		SortIndex(_invsIndex, _invs, CompareRule<RuleInventory>(r => r.ListOrder));
		// special cases
		// Craft weapons use the list order of their launcher item.
		SortIndex(_craftWeaponsIndex, _craftWeapons, CompareRule<RuleCraftWeapon>(r => r.LauncherItem.ListOrder));
		SortIndex(_armorsIndex, _armors, FromLess<Armor>(CompareArmor));
		SortStable(_armorsForSoldiersCache, FromLess<Armor>(CompareArmor));
		_ufopaediaSections[Ufopaedia.UFOPAEDIA_NOT_AVAILABLE] = 0;
		// Ufopaedia articles use section and list order.
		SortIndex(_ufopaediaIndex, _ufopaediaArticles, FromLess<ArticleDefinition>((r1, r2) =>
		{
			if (r1.section == r2.section)
				return r1.ListOrder < r2.ListOrder;
			else
				return _ufopaediaSections[r1.section] < _ufopaediaSections[r2.section];
		}));
		// Ufopaedia sections use article list order.
		SortStable(_ufopaediaCatIndex, FromLess<string>((r1, r2) => _ufopaediaSections[r1] < _ufopaediaSections[r2]));
		SortIndex(_soldiersIndex, _soldiers, CompareRule<RuleSoldier>(r => r.ListOrder));
		SortIndex(_aliensIndex, _alienRaces, CompareRule<AlienRace>(r => r.ListOrder));
	}

	/// <summary>Gets the research-requirements for Psi-Lab (it's a cache for psiStrengthEval).</summary>
	public List<string> PsiRequirements => _psiRequirements;

	/// <summary>Returns the list of inventories.</summary>
	public List<string> InvsList => _invsIndex;

	/// <summary>Creates a new randomly-generated soldier.</summary>
	/// <param name="save">Saved game the soldier belongs to.</param>
	/// <param name="ruleSoldier">The soldier type to generate.</param>
	/// <param name="nationality">Nationality of the soldier.</param>
	/// <returns>Newly generated soldier.</returns>
	public Soldier GenSoldier(SavedGame save, RuleSoldier ruleSoldier, int nationality)
	{
		ArgumentNullException.ThrowIfNull(save);
		ArgumentNullException.ThrowIfNull(ruleSoldier);
		Soldier soldier = null;
		int newId = save.GetId("STR_SOLDIER");

		// Check for duplicates
		// Original X-COM gives up after 10 tries so might as well do the same here
		bool duplicate = true;
		for (int tries = 0; tries < 10 && duplicate; ++tries)
		{
			soldier = new Soldier(ruleSoldier, ruleSoldier.DefaultArmor, nationality, newId);
			duplicate = false;
			foreach (var xbase in save.Bases)
			{
				if (duplicate) break; // loop finished
				foreach (var xsoldier in xbase.Soldiers)
				{
					if (duplicate) break; // loop finished
					if (xsoldier.GetName() == soldier.GetName())
					{
						duplicate = true;
					}
				}
				foreach (var transfer in xbase.Transfers)
				{
					if (duplicate) break; // loop finished
					if (transfer.Type == TransferType.TRANSFER_SOLDIER && transfer.Soldier.GetName() == soldier.GetName())
					{
						duplicate = true;
					}
				}
			}
		}

		// calculate new statString
		soldier.CalcStatString(StatStrings, Options.psiStrengthEval && save.IsResearched(PsiRequirements));

		return soldier;
	}

	/// <summary>Gets the name of the item to be used as alien fuel.</summary>
	public string AlienFuelName => _alienFuel.first;

	/// <summary>Gets the amount of alien fuel to recover.</summary>
	public int AlienFuelQuantity => _alienFuel.second;

	/// <summary>Gets name of font collection (the name of YAML-file with font data).</summary>
	public string FontName => _fontName;

	/// <summary>Returns the maximum radar range still considered as short.</summary>
	public int ShortRadarRange
	{
		get
		{
			if (_shortRadarRange > 0)
				return _shortRadarRange;

			int minRadarRange = 0;

			foreach (var facType in _facilitiesIndex)
			{
				RuleBaseFacility f = GetBaseFacility(facType);
				if (f == null) continue;

				int radarRange = f.RadarRange;
				if (radarRange > 0 && (minRadarRange == 0 || minRadarRange > radarRange))
				{
					minRadarRange = radarRange;
				}
			}

			return minRadarRange;
		}
	}

	/// <summary>Gets the custom scaling (defined by the modder) applied on the facility upgrade/build time reduction calculated by the game.</summary>
	public int BuildTimeReductionScaling => _buildTimeReductionScaling;

	/// <summary>
	/// Returns what should be displayed in craft pedia articles for fuel capacity/range:
	/// 0 = Max theoretical range, 1 = Min and max theoretical max range, 2 = average of the two;
	/// otherwise (default), just show the fuel capacity.
	/// </summary>
	public int PediaReplaceCraftFuelWithRangeType => _pediaReplaceCraftFuelWithRangeType;

	/// <summary>Gets information on an interface.</summary>
	public RuleInterface GetInterface(string id, bool error = true) => GetRule(id, "Interface", _interfaces, error);

	/// <summary>Gets the rules for the Geoscape globe.</summary>
	public RuleGlobe Globe => _globe;

	/// <summary>Gets the rules for the Save Converter.</summary>
	public RuleConverter Converter => _converter;

	/// <summary>Gets the list of selective files for insertion into our cat files.</summary>
	public SortedDictionary<string, SoundDefinition> SoundDefinitions => _soundDefs;

	/// <summary>Gets the commands of a map script, or null.</summary>
	public List<MapScript> GetMapScript(string id)
	{
		if (id != null && _mapScripts.TryGetValue(id, out var i))
		{
			return i;
		}
		else
		{
			return null;
		}
	}

	/// <summary>All map scripts.</summary>
	public SortedDictionary<string, List<MapScript>> MapScriptsRaw => _mapScripts;

	/// <summary>Returns the data for the specified video cutscene.</summary>
	public RuleVideo GetVideo(string id, bool error = false) => GetRule(id, "Video", _videos, error);

	/// <summary>Music definitions.</summary>
	public SortedDictionary<string, RuleMusic> Music => _musicDefs;

	public List<string> ArcScriptList => _arcScriptIndex;

	public RuleArcScript GetArcScript(string name, bool error = false) => GetRule(name, "Arc Script", _arcScripts, error);

	public List<string> EventScriptList => _eventScriptIndex;

	public RuleEventScript GetEventScript(string name, bool error = false) => GetRule(name, "Event Script", _eventScripts, error);

	public List<string> EventList => _eventIndex;

	public RuleEvent GetEvent(string name, bool error = false) => GetRule(name, "Event", _events, error);

	public List<string> MissionScriptList => _missionScriptIndex;

	public List<string> AdhocScriptList => _adhocScriptIndex;

	public RuleMissionScript GetMissionScript(string name, bool error = false) => GetRule(name, "Mission Script", _missionScripts, error);

	public RuleMissionScript GetAdhocScript(string name, bool error = false) => GetRule(name, "Adhoc Script", _adhocScripts, error);

	public RuleResearch FinalResearch => _finalResearch;

	public RuleBaseFacility DestroyedFacility
	{
		get
		{
			if (IsEmptyRuleName(_destroyedFacility))
				return null;

			var temp = GetBaseFacility(_destroyedFacility, true);
			if (!temp.IsSmall)
			{
				throw new InvalidOperationException("Destroyed base facility definition must have size: 1");
			}
			return temp;
		}
	}

	public SortedDictionary<int, string> MissionRatings => _missionRatings;
	public SortedDictionary<int, string> MonthlyRatings => _monthlyRatings;
	public SortedDictionary<string, string> FixedUserOptions => _fixedUserOptions;
	public SortedDictionary<string, string> RecommendedUserOptions => _recommendedUserOptions;
	public List<string> HiddenMovementBackgrounds => _hiddenMovementBackgrounds;
	public List<string> BaseNamesFirst => _baseNamesFirst;
	public List<string> BaseNamesMiddle => _baseNamesMiddle;
	public List<string> BaseNamesLast => _baseNamesLast;
	public List<string> OperationNamesFirst => _operationNamesFirst;
	public List<string> OperationNamesLast => _operationNamesLast;
	public bool EnableUnitResponseSounds => _enableUnitResponseSounds;
	public SortedDictionary<string, List<int>> SelectUnitSounds => _selectUnitSound;
	public SortedDictionary<string, List<int>> StartMovingSounds => _startMovingSound;
	public SortedDictionary<string, List<int>> SelectWeaponSounds => _selectWeaponSound;
	public SortedDictionary<string, List<int>> AnnoyedSounds => _annoyedSound;
	public List<int> SelectBaseSounds => _selectBaseSound;
	public List<int> StartDogfightSounds => _startDogfightSound;
	public List<int> FlagByKills => _flagByKills;

	public StatAdjustment GetStatAdjustment(int difficulty)
	{
		if ((uint)difficulty >= MaxDifficultyLevels)
		{
			return _statAdjustment[MaxDifficultyLevels - 1];
		}
		return _statAdjustment[difficulty];
	}

	/// <summary>Returns the minimum amount of score the player can have, otherwise they are defeated.</summary>
	public int DefeatScore => _defeatScore;

	/// <summary>Returns the minimum amount of funds the player can have, otherwise they are defeated.</summary>
	public int DefeatFunds => _defeatFunds;

	/// <summary>
	/// Enables non-vanilla difficulty features.
	/// Dehumanize yourself and face the Warboy.
	/// </summary>
	public bool IsDemigod => _difficultyDemigod;

	public List<int> MonthlyRatingThresholds => _monthlyRatingThresholds;
	public List<int> UfoFiringRateCoefficients => _ufoFiringRateCoefficients;
	public List<int> UfoEscapeCountdownCoefficients => _ufoEscapeCountdownCoefficients;
	public List<int> RetaliationTriggerOdds => _retaliationTriggerOdds;
	public List<int> RetaliationBaseRegionOdds => _retaliationBaseRegionOdds;
	public List<int> AliensFacingCraftOdds => _aliensFacingCraftOdds;

	/// <summary>Assigns indexes to base function names (C++ <c>Collections::NamesToIndex</c>).</summary>
	private sealed class NamesToIndex
	{
		private readonly Dictionary<string, int> _usedValues = new(StringComparer.Ordinal);
		private readonly Dictionary<int, string> _usedNames = new() { { 0, "" } };
		private int _last = 1; // zero is reserved for "empty"

		/// <summary>Return an index for a given name.</summary>
		/// <param name="name">New name or old already added name.</param>
		/// <param name="max">How many unique names we can hold.</param>
		/// <returns>Index assigned to a name.</returns>
		public int AddName(string name, int max)
		{
			_usedValues.TryGetValue(name, out int value);
			if (value != 0)
			{
				return value;
			}
			if (_last == max)
			{
				// C++ creates the map entry before throwing
				_usedValues[name] = 0;
				throw new InvalidOperationException("Number of unique names reached limit because of name '" + name + "'");
			}
			value = _last++;
			_usedValues[name] = value;
			_usedNames[value] = name;
			return value;
		}

		/// <summary>Get name based on index, or empty string if index is not assigned.</summary>
		public string GetName(int i) => _usedNames.TryGetValue(i, out var name) ? name : _usedNames[0];
	}
}
