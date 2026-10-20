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
// Ported from: src/Mod/AlienDeployment.h, src/Mod/AlienDeployment.cpp
#nullable disable

namespace Acom;

/// <summary>A set of items carried by one alien.</summary>
public class ItemSet
{
	public List<string> items = [];

	/// <summary>YAML deserialization: a plain sequence of item names (C++ read(ryml::ConstNodeRef, ItemSet*)).</summary>
	public static bool YamlRead(YamlNodeReader reader, ref ItemSet val)
	{
		val ??= new ItemSet();
		reader.TryReadVal(ref val.items);
		return true;
	}
}

/// <summary>Deployment data of one alien rank.</summary>
public class DeploymentData
{
	public int alienRank;
	public string customUnitType;
	public int lowQty;
	public int medQty;
	public int highQty;
	public int dQty;
	public int extraQty;
	public int percentageOutsideUfo;
	public List<ItemSet> itemSets;
	public List<ItemSet> extraRandomItems;

	public DeploymentData()
	{
		alienRank = 0;
		customUnitType = "";
		lowQty = 0;
		medQty = 0;
		highQty = 0;
		dQty = 0;
		extraQty = 0;
		percentageOutsideUfo = 0;
		itemSets = [];
		extraRandomItems = [];
	}

	/// <summary>YAML deserialization (C++ read(ryml::ConstNodeRef, DeploymentData*)).</summary>
	public static bool YamlRead(YamlNodeReader reader, ref DeploymentData val)
	{
		val ??= new DeploymentData();
		reader.TryRead("alienRank", ref val.alienRank);
		reader.TryRead("customUnitType", ref val.customUnitType);
		reader.TryRead("lowQty", ref val.lowQty);
		reader.TryRead("medQty", ref val.medQty);
		reader.TryRead("highQty", ref val.highQty);
		reader.TryRead("dQty", ref val.dQty);
		reader.ReadNode("extraQty", ref val.extraQty, 0);
		reader.TryRead("percentageOutsideUfo", ref val.percentageOutsideUfo);
		reader.TryRead("itemSets", ref val.itemSets);
		reader.TryRead("extraRandomItems", ref val.extraRandomItems);
		return true;
	}
}

/// <summary>Data for the briefing window of a mission.</summary>
public class BriefingData
{
	public int palette;
	public int textOffset;
	public string title;
	public string desc;
	public string music;
	public string background;
	public string cutscene;
	public bool showCraft;
	public bool showTarget;

	public BriefingData()
	{
		palette = 0;
		textOffset = 0;
		title = "";
		desc = "";
		music = "GMDEFEND";
		background = "BACK16.SCR";
		cutscene = "";
		showCraft = true;
		showTarget = true;
	}

	/// <summary>Returns a copy (C++ copies this struct by value).</summary>
	public BriefingData Clone() => (BriefingData)MemberwiseClone();

	/// <summary>YAML deserialization (C++ read(ryml::ConstNodeRef, BriefingData*)).</summary>
	public static bool YamlRead(YamlNodeReader reader, ref BriefingData val)
	{
		val ??= new BriefingData();
		reader.TryRead("palette", ref val.palette);
		reader.TryRead("textOffset", ref val.textOffset);
		reader.TryRead("title", ref val.title);
		reader.TryRead("desc", ref val.desc);
		reader.TryRead("music", ref val.music);
		reader.TryRead("cutscene", ref val.cutscene);
		reader.TryRead("background", ref val.background);
		reader.TryRead("showCraft", ref val.showCraft);
		reader.TryRead("showTarget", ref val.showTarget);
		return true;
	}
}

public enum MapBlockFilterType
{
	MFT_NONE = 0,
	MFT_BY_MAPSCRIPT = 1,
	MFT_BY_REINFORCEMENTS = 2,
	MFT_BY_BOTH_UNION = 3,
	MFT_BY_BOTH_INTERSECTION = 4,
}

/// <summary>A wave of reinforcements during a battle.</summary>
public class ReinforcementsData
{
	public string type = "";
	public BriefingData briefing = new();
	public int minDifficulty; // 0
	public int maxDifficulty = 4;
	public bool objectiveDestroyed; // false
	public List<int> turns = [];
	public int minTurn; // 0
	public int maxTurn = -1;
	public int executionOdds = 100;
	public int maxRuns = -1;
	public bool useSpawnNodes = true;
	public MapBlockFilterType mapBlockFilterType = MapBlockFilterType.MFT_BY_BOTH_UNION;
	public List<string> spawnBlocks = [];
	public List<int> spawnBlockGroups = [];
	public List<int> spawnNodeRanks = [];
	public List<int> spawnZLevels = [];
	public bool randomizeZLevels = true;
	public int minDistanceFromXcomUnits; // 0
	public int maxDistanceFromBorders; // 0
	public bool forceSpawnNearFriend = true;
	public List<DeploymentData> data = [];

	/// <summary>YAML deserialization (C++ read(ryml::ConstNodeRef, ReinforcementsData*)).</summary>
	public static bool YamlRead(YamlNodeReader reader, ref ReinforcementsData val)
	{
		val ??= new ReinforcementsData();
		reader.TryRead("type", ref val.type);
		reader.TryRead("briefing", ref val.briefing);
		reader.TryRead("minDifficulty", ref val.minDifficulty);
		reader.TryRead("maxDifficulty", ref val.maxDifficulty);
		reader.TryRead("objectiveDestroyed", ref val.objectiveDestroyed);
		reader.TryRead("turns", ref val.turns);
		reader.TryRead("minTurn", ref val.minTurn);
		reader.TryRead("maxTurn", ref val.maxTurn);
		reader.TryRead("executionOdds", ref val.executionOdds);
		reader.TryRead("maxRuns", ref val.maxRuns);
		reader.TryRead("useSpawnNodes", ref val.useSpawnNodes);
		reader.TryRead("mapBlockFilterType", ref val.mapBlockFilterType);
		reader.TryRead("spawnBlocks", ref val.spawnBlocks);
		reader.TryRead("spawnBlockGroups", ref val.spawnBlockGroups);
		reader.TryRead("spawnNodeRanks", ref val.spawnNodeRanks);
		reader.TryRead("spawnZLevels", ref val.spawnZLevels);
		reader.TryRead("randomizeZLevels", ref val.randomizeZLevels);
		reader.TryRead("minDistanceFromXcomUnits", ref val.minDistanceFromXcomUnits);
		reader.TryRead("maxDistanceFromBorders", ref val.maxDistanceFromBorders);
		reader.TryRead("forceSpawnNearFriend", ref val.forceSpawnNearFriend);
		reader.TryRead("data", ref val.data);
		return true;
	}
}

public enum ChronoTrigger : uint
{
	FORCE_LOSE = 0,
	FORCE_ABORT = 1,
	FORCE_WIN = 2,
	FORCE_WIN_SURRENDER = 3,
}

public enum EscapeType
{
	ESCAPE_NONE = 0,
	ESCAPE_EXIT = 1,
	ESCAPE_ENTRY = 2,
	ESCAPE_EITHER = 3,
}

/// <summary>
/// Represents a specific type of Alien Deployment.
/// Contains constant info about a Alien Deployment like
/// the number of aliens for each alien type and what items they carry
/// (itemset depends on alien technology advancement level 0, 1 or 2).
/// - deployment type can be a craft's name, but also alien base or cydonia.
/// - alienRank is used to check which nodeRanks can be used to deploy this unit
///   + to match to a specific unit (=race/rank combination) that should be deployed.
/// </summary>
public class AlienDeployment
{
	private readonly string _type;
	private string _customUfo = "";
	private string _enviroEffects = "", _startingCondition = "";
	private string _unlockedResearchOnSuccess = "", _unlockedResearchOnFailure = "", _unlockedResearchOnDespawn = "";
	private string _counterSuccess = "", _counterFailure = "", _counterDespawn = "", _counterAll = "";
	private string _decreaseCounterSuccess = "", _decreaseCounterFailure = "", _decreaseCounterDespawn = "", _decreaseCounterAll = "";
	private string _missionBountyItem = "";
	private int _missionBountyItemCount;
	private int _bughuntMinTurn;
	private bool _forcePercentageOutsideUfo;
	private List<DeploymentData> _data = [];
	private List<ReinforcementsData> _reinforcements = [];
	private int _width, _length, _height, _civilians;
	private bool _ignoreLivingCivilians;
	private bool _markCiviliansAsVIP;
	private int _civilianSpawnNodeRank;
	private readonly SortedDictionary<string, int> _civiliansByType = new(StringComparer.Ordinal);
	private List<string> _terrains = [], _music = [];
	private int _shade, _minShade, _maxShade;
	private string _nextStage = "", _race = "", _mapScript = "";
	private List<string> _mapScripts = [];
	private List<string> _randomRaces = [];
	private bool _finalDestination, _isAlienBase, _isHidden;
	private int _fakeUnderwaterSpawnChance;
	private string _winCutscene = "", _loseCutscene = "", _abortCutscene = "";
	private string _alert, _alertBackground, _alertDescription;
	private int _alertSound;
	private BriefingData _briefingData = new();
	private string _markerName, _objectivePopup = "", _objectiveCompleteText = "", _objectiveFailedText = "";
	private string _missionCompleteText = "", _missionFailedText = "";
	private readonly WeightedOptions _genMission = new(), _successEvents = new(), _failureEvents = new(), _despawnEvents = new();
	private int _markerIcon, _durationMin, _durationMax, _minDepth, _maxDepth, _genMissionFrequency, _genMissionLimit;
	private bool _genMissionRaceFromAlienBase;
	private int _objectiveType, _objectivesRequired, _objectiveCompleteScore, _objectiveFailedScore, _despawnPenalty, _abortPenalty, _points, _turnLimit, _cheatTurn;
	private ChronoTrigger _chronoTrigger;
	private bool _keepCraftAfterFailedMission, _allowObjectiveRecovery;
	private EscapeType _escapeType;
	private int _vipSurvivalPercentage;
	private string _baseSelfDestructCode = "";
	private int _baseDetectionRange, _baseDetectionChance, _huntMissionMaxFrequency;
	private bool _huntMissionRaceFromAlienBase;
	private readonly List<(int first, WeightedOptions second)> _huntMissionDistribution = [];
	private readonly List<(int first, WeightedOptions second)> _alienBaseUpgrades = [];
	private bool _resetAlienBaseAgeAfterUpgrade, _resetAlienBaseAge;
	private string _upgradeRace = "";
	private List<(int, string, string)> _alienRaceEvolution = [];
	private bool _noWeaponPile;

	/// <summary>Creates a blank ruleset for a certain type of deployment data.</summary>
	/// <param name="type">String defining the type.</param>
	public AlienDeployment(string type)
	{
		_type = type;
		_missionBountyItemCount = 1;
		_bughuntMinTurn = 0;
		_forcePercentageOutsideUfo = false;
		_width = 0;
		_length = 0;
		_height = 0;
		_civilians = 0;
		_ignoreLivingCivilians = false;
		_markCiviliansAsVIP = false;
		_civilianSpawnNodeRank = 0;
		_shade = -1;
		_minShade = -1;
		_maxShade = -1;
		_finalDestination = false;
		_isAlienBase = false;
		_isHidden = false;
		_fakeUnderwaterSpawnChance = 0;
		_alert = "STR_ALIENS_TERRORISE";
		_alertBackground = "BACK03.SCR";
		_alertDescription = "";
		_alertSound = -1;
		_markerName = "STR_TERROR_SITE";
		_markerIcon = -1;
		_durationMin = 0;
		_durationMax = 0;
		_minDepth = 0;
		_maxDepth = 0;
		_genMissionFrequency = 0;
		_genMissionLimit = 1000;
		_genMissionRaceFromAlienBase = true;
		_objectiveType = -1;
		_objectivesRequired = 0;
		_objectiveCompleteScore = 0;
		_objectiveFailedScore = 0;
		_despawnPenalty = 0;
		_abortPenalty = 0;
		_points = 0;
		_turnLimit = 0;
		_cheatTurn = 20;
		_chronoTrigger = ChronoTrigger.FORCE_LOSE;
		_keepCraftAfterFailedMission = false;
		_allowObjectiveRecovery = false;
		_escapeType = EscapeType.ESCAPE_NONE;
		_vipSurvivalPercentage = 0;
		_baseDetectionRange = 0;
		_baseDetectionChance = 100;
		_huntMissionMaxFrequency = 60;
		_huntMissionRaceFromAlienBase = true;
		_resetAlienBaseAgeAfterUpgrade = false;
		_resetAlienBaseAge = false;
		_noWeaponPile = false;
	}

	/// <summary>Loads the Deployment from YAML.</summary>
	/// <param name="node">YAML node.</param>
	/// <param name="mod">Mod for the deployment.</param>
	public void Load(YamlNodeReader node, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		var reader = node.UseIndex();
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent, mod);
		}

		reader.TryRead("customUfo", ref _customUfo);
		reader.TryRead("enviroEffects", ref _enviroEffects);
		reader.TryRead("startingCondition", ref _startingCondition);
		reader.TryRead("unlockedResearch", ref _unlockedResearchOnSuccess);
		reader.TryRead("unlockedResearchOnFailure", ref _unlockedResearchOnFailure);
		reader.TryRead("unlockedResearchOnDespawn", ref _unlockedResearchOnDespawn);
		reader.TryRead("counterSuccess", ref _counterSuccess);
		reader.TryRead("counterFailure", ref _counterFailure);
		reader.TryRead("counterDespawn", ref _counterDespawn);
		reader.TryRead("counterAll", ref _counterAll);
		reader.TryRead("decreaseCounterSuccess", ref _decreaseCounterSuccess);
		reader.TryRead("decreaseCounterFailure", ref _decreaseCounterFailure);
		reader.TryRead("decreaseCounterDespawn", ref _decreaseCounterDespawn);
		reader.TryRead("decreaseCounterAll", ref _decreaseCounterAll);
		reader.TryRead("missionBountyItem", ref _missionBountyItem);
		reader.TryRead("missionBountyItemCount", ref _missionBountyItemCount);
		reader.TryRead("bughuntMinTurn", ref _bughuntMinTurn);
		reader.TryRead("forcePercentageOutsideUfo", ref _forcePercentageOutsideUfo);
		reader.TryRead("data", ref _data);
		reader.TryRead("reinforcements", ref _reinforcements);
		reader.TryRead("width", ref _width);
		reader.TryRead("length", ref _length);
		reader.TryRead("height", ref _height);
		reader.TryRead("civilians", ref _civilians);
		reader.TryRead("ignoreLivingCivilians", ref _ignoreLivingCivilians);
		reader.TryRead("markCiviliansAsVIP", ref _markCiviliansAsVIP);
		reader.TryRead("civilianSpawnNodeRank", ref _civilianSpawnNodeRank);
		mod.LoadUnorderedNamesToInt(_type, _civiliansByType, reader["civiliansByType"]);
		reader.TryRead("terrains", ref _terrains);
		reader.TryRead("shade", ref _shade);
		reader.TryRead("minShade", ref _minShade);
		reader.TryRead("maxShade", ref _maxShade);
		reader.TryRead("nextStage", ref _nextStage);
		reader.TryRead("race", ref _race);
		reader.TryRead("randomRace", ref _randomRaces);
		reader.TryRead("finalDestination", ref _finalDestination);
		reader.TryRead("winCutscene", ref _winCutscene);
		reader.TryRead("loseCutscene", ref _loseCutscene);
		reader.TryRead("abortCutscene", ref _abortCutscene);
		reader.TryRead("script", ref _mapScript);
		reader.TryRead("mapScripts", ref _mapScripts);
		reader.TryRead("alert", ref _alert);
		reader.TryRead("alertBackground", ref _alertBackground);
		reader.TryRead("alertDescription", ref _alertDescription);
		mod.LoadSoundOffset(_type, ref _alertSound, reader["alertSound"], "GEO.CAT");
		reader.TryRead("briefing", ref _briefingData);
		reader.TryRead("markerName", ref _markerName);
		if (reader["markerIcon"])
		{
			_markerIcon = mod.GetOffset(reader["markerIcon"].ReadVal<int>(), 8);
		}
		if (reader["depth"])
		{
			_minDepth = reader["depth"][0].ReadVal(_minDepth);
			_maxDepth = reader["depth"][1].ReadVal(_maxDepth);
		}
		if (reader["duration"])
		{
			_durationMin = reader["duration"][0].ReadVal(_durationMin);
			_durationMax = reader["duration"][1].ReadVal(_durationMax);
		}
		reader.TryRead("music", ref _music);
		reader.TryRead("objectiveType", ref _objectiveType);
		reader.TryRead("objectivesRequired", ref _objectivesRequired);
		reader.TryRead("objectivePopup", ref _objectivePopup);

		if (reader["objectiveComplete"])
		{
			_objectiveCompleteText = reader["objectiveComplete"][0].ReadVal(_objectiveCompleteText);
			_objectiveCompleteScore = reader["objectiveComplete"][1].ReadVal(_objectiveCompleteScore);
		}
		if (reader["objectiveFailed"])
		{
			_objectiveFailedText = reader["objectiveFailed"][0].ReadVal(_objectiveFailedText);
			_objectiveFailedScore = reader["objectiveFailed"][1].ReadVal(_objectiveFailedScore);
		}
		reader.TryRead("missionCompleteText", ref _missionCompleteText);
		reader.TryRead("missionFailedText", ref _missionFailedText);
		if (reader["successEvents"])
		{
			_successEvents.Load(reader["successEvents"]);
		}
		if (reader["despawnEvents"])
		{
			_despawnEvents.Load(reader["despawnEvents"]);
		}
		if (reader["failureEvents"])
		{
			_failureEvents.Load(reader["failureEvents"]);
		}
		reader.TryRead("despawnPenalty", ref _despawnPenalty);
		reader.TryRead("abortPenalty", ref _abortPenalty);
		reader.TryRead("points", ref _points);
		reader.TryRead("cheatTurn", ref _cheatTurn);
		reader.TryRead("turnLimit", ref _turnLimit);
		reader.TryRead("chronoTrigger", ref _chronoTrigger);
		reader.TryRead("alienBase", ref _isAlienBase);
		reader.TryRead("isHidden", ref _isHidden);
		reader.TryRead("fakeUnderwaterSpawnChance", ref _fakeUnderwaterSpawnChance);
		reader.TryRead("keepCraftAfterFailedMission", ref _keepCraftAfterFailedMission);
		reader.TryRead("allowObjectiveRecovery", ref _allowObjectiveRecovery);
		reader.TryRead("escapeType", ref _escapeType);
		reader.TryRead("vipSurvivalPercentage", ref _vipSurvivalPercentage);
		if (reader["genMission"])
		{
			_genMission.Load(reader["genMission"]);
		}
		reader.TryRead("genMissionFreq", ref _genMissionFrequency);
		reader.TryRead("genMissionLimit", ref _genMissionLimit);
		reader.TryRead("genMissionRaceFromAlienBase", ref _genMissionRaceFromAlienBase);

		reader.TryRead("baseSelfDestructCode", ref _baseSelfDestructCode);
		reader.TryRead("baseDetectionRange", ref _baseDetectionRange);
		reader.TryRead("baseDetectionChance", ref _baseDetectionChance);
		reader.TryRead("huntMissionMaxFrequency", ref _huntMissionMaxFrequency);
		reader.TryRead("huntMissionRaceFromAlienBase", ref _huntMissionRaceFromAlienBase);
		foreach (var weights in reader["huntMissionWeights"].Children())
		{
			var nw = new WeightedOptions();
			nw.Load(weights);
			_huntMissionDistribution.Add((weights.ReadKey(0), nw));
		}
		foreach (var weights in reader["alienBaseUpgrades"].Children())
		{
			var nw = new WeightedOptions();
			nw.Load(weights);
			_alienBaseUpgrades.Add((weights.ReadKey(0), nw));
		}
		reader.TryRead("resetAlienBaseAgeAfterUpgrade", ref _resetAlienBaseAgeAfterUpgrade);
		reader.TryRead("resetAlienBaseAge", ref _resetAlienBaseAge);
		reader.TryRead("upgradeRace", ref _upgradeRace);
		reader.TryRead("alienRaceEvolution", ref _alienRaceEvolution);
		if (_alienRaceEvolution.Count != 0)
		{
			// std::stable_sort by month, descending (OrderByDescending is stable)
			_alienRaceEvolution = [.. _alienRaceEvolution.OrderByDescending(e => e.Item1)];
		}
		reader.TryRead("noWeaponPile", ref _noWeaponPile);
	}

	/// <summary>Returns the language string that names this deployment. Each deployment type has a unique name.</summary>
	public string Type => _type;

	/// <summary>Gets the custom UFO name to use for the dummy/blank 'addUFO' mapscript command.</summary>
	public string CustomUfoName => _customUfo;

	/// <summary>Returns the enviro effects name for this mission.</summary>
	public string EnviroEffects => _enviroEffects;

	/// <summary>Returns the starting condition name for this mission.</summary>
	public string StartingCondition => _startingCondition;

	/// <summary>Gets the research topic to be unlocked after a successful mission.</summary>
	public string UnlockedResearchOnSuccess => _unlockedResearchOnSuccess;

	/// <summary>Gets the research topic to be unlocked after a failed mission.</summary>
	public string UnlockedResearchOnFailure => _unlockedResearchOnFailure;

	/// <summary>Gets the research topic to be unlocked after a despawned mission site.</summary>
	public string UnlockedResearchOnDespawn => _unlockedResearchOnDespawn;

	/// <summary>Gets the name of a custom counter variable to increase on mission success.</summary>
	public string CounterSuccess => _counterSuccess;

	/// <summary>Gets the name of a custom counter variable to increase on mission failure (incl. mission despawn).</summary>
	public string CounterFailure => _counterFailure;

	/// <summary>Gets the name of a custom counter variable to increase on mission despawn.</summary>
	public string CounterDespawn => _counterDespawn;

	/// <summary>Gets the name of a custom counter variable to increase on any mission result (success, failure and despawn).</summary>
	public string CounterAll => _counterAll;

	/// <summary>Gets the name of a custom counter variable to decrease on mission success.</summary>
	public string DecreaseCounterSuccess => _decreaseCounterSuccess;

	/// <summary>Gets the name of a custom counter variable to decrease on mission failure (incl. mission despawn).</summary>
	public string DecreaseCounterFailure => _decreaseCounterFailure;

	/// <summary>Gets the name of a custom counter variable to decrease on mission despawn.</summary>
	public string DecreaseCounterDespawn => _decreaseCounterDespawn;

	/// <summary>Gets the name of a custom counter variable to decrease on any mission result (success, failure and despawn).</summary>
	public string DecreaseCounterAll => _decreaseCounterAll;

	/// <summary>Returns the item to be recovered/given after a successful mission.</summary>
	public string MissionBountyItem => _missionBountyItem;

	/// <summary>Gets the number of items to be recovered/given after a successful mission.</summary>
	public int MissionBountyItemCount => _missionBountyItemCount;

	/// <summary>Gets the bug hunt mode minimum turn requirement (default = 0 = not used).</summary>
	public int BughuntMinTurn => _bughuntMinTurn;

	/// <summary>Should `percentageOutsideUfo` be forced for all kinds of missions? (instead of just UFO crash/landing sites)</summary>
	public bool ForcePercentageOutsideUfo => _forcePercentageOutsideUfo;

	/// <summary>Gets the deployment data.</summary>
	public List<DeploymentData> DeploymentData => _data;

	/// <summary>Gets the highest used alien rank.</summary>
	public int MaxAlienRank
	{
		get
		{
			int max = 0;
			foreach (var dd in _data)
			{
				if (dd.alienRank > max)
					max = dd.alienRank;
			}
			return max;
		}
	}

	/// <summary>Gets the reinforcements data.</summary>
	public List<ReinforcementsData> ReinforcementsData => _reinforcements;

	/// <summary>Gets dimensions.</summary>
	public void GetDimensions(ref int width, ref int length, ref int height)
	{
		width = _width;
		length = _length;
		height = _height;
	}

	/// <summary>Gets the number of civilians.</summary>
	public int Civilians => _civilians;

	/// <summary>Should living civilians be ignored for scoring, commendations, etc.?</summary>
	public bool IgnoreLivingCivilians => _ignoreLivingCivilians;

	/// <summary>Should civilians be marked as VIP?</summary>
	public bool MarkCiviliansAsVIP => _markCiviliansAsVIP;

	/// <summary>Gets the civilian spawn node rank.</summary>
	public int CivilianSpawnNodeRank => _civilianSpawnNodeRank;

	/// <summary>Gets the number of civilians per type.</summary>
	public SortedDictionary<string, int> CiviliansByType => _civiliansByType;

	/// <summary>Gets the terrains for battlescape generation (a copy, like the C++ return by value).</summary>
	public List<string> Terrains => [.. _terrains];

	/// <summary>Gets the shade level for battlescape generation.</summary>
	public int Shade => _shade;

	/// <summary>Gets the min shade level for battlescape generation.</summary>
	public int MinShade => _minShade;

	/// <summary>Gets the max shade level for battlescape generation.</summary>
	public int MaxShade => _maxShade;

	/// <summary>Gets the next stage of the mission.</summary>
	public string NextStage => _nextStage;

	/// <summary>Gets the race to use on the next stage of the mission (picks one of the random races if defined).</summary>
	public string Race
	{
		get
		{
			if (_randomRaces.Count != 0)
			{
				return _randomRaces[Rng.Generate(0, _randomRaces.Count - 1)];
			}
			return _race;
		}
	}

	/// <summary>Gets the script to use to generate a mission of this type (picks one of the map scripts if defined).</summary>
	public string RandomMapScript
	{
		get
		{
			if (_mapScripts.Count != 0)
			{
				int pick = Rng.Generate(0, _mapScripts.Count - 1);
				return _mapScripts[pick];
			}
			return _mapScript;
		}
	}

	/// <summary>Does any map script use globe terrain?</summary>
	/// <returns>1 = yes, 0 = no, -1 = no map script found.</returns>
	public int HasTextureBasedScript(Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		int ret = -1;
		// iterate _mapScripts
		foreach (string script in _mapScripts)
		{
			var vec = mod.GetMapScript(script);
			if (vec != null)
			{
				ret = 0;
				if (RuleTerrain.UsesTextureBasedTerrain(vec))
				{
					return 1;
				}
			}
		}
		// check also _mapScript
		{
			var vec = mod.GetMapScript(_mapScript);
			if (vec != null)
			{
				ret = 0;
				if (RuleTerrain.UsesTextureBasedTerrain(vec))
				{
					return 1;
				}
			}
		}
		return ret;
	}

	/// <summary>Gets if winning this mission completes the game.</summary>
	public bool IsFinalDestination => _finalDestination;

	/// <summary>Gets the cutscene to play when the mission is won.</summary>
	public string WinCutscene => _winCutscene;

	/// <summary>Gets the cutscene to play when the mission is lost.</summary>
	public string LoseCutscene => _loseCutscene;

	/// <summary>Gets the cutscene to play when the mission is aborted.</summary>
	public string AbortCutscene => _abortCutscene;

	/// <summary>Gets geoscape event rule name to spawn after success mission.</summary>
	public string ChooseSuccessEvent() => _successEvents.Choose();

	/// <summary>Gets geoscape event rule name to spawn after despawned mission.</summary>
	public string ChooseDespawnEvent() => _despawnEvents.Choose();

	/// <summary>Gets geoscape event rule name to spawn after failure mission.</summary>
	public string ChooseFailureEvent() => _failureEvents.Choose();

	/// <summary>Gets the alert message displayed when this mission spawns.</summary>
	public string AlertMessage => _alert;

	/// <summary>Gets the alert background displayed when this mission spawns.</summary>
	public string AlertBackground => _alertBackground;

	/// <summary>Gets the alert description (displayed when clicking on [Info] button in TargetInfo).</summary>
	public string AlertDescription => _alertDescription;

	/// <summary>Gets the alert sound (played when mission detected screen pops up).</summary>
	public int AlertSound => _alertSound;

	/// <summary>Gets the briefing data for this mission type (a copy, like the C++ return by value).</summary>
	public BriefingData BriefingData => _briefingData.Clone();

	/// <summary>Returns the globe marker name for this mission.</summary>
	public string MarkerName => _markerName;

	/// <summary>Returns the globe marker icon for this mission, -1 if none.</summary>
	public int MarkerIcon => _markerIcon;

	/// <summary>Returns the minimum duration for this mission type, in hours.</summary>
	public int DurationMin => _durationMin;

	/// <summary>Returns the maximum duration for this mission type, in hours.</summary>
	public int DurationMax => _durationMax;

	/// <summary>Gets the list of music tracks this deployment has to choose from.</summary>
	public List<string> Music => _music;

	/// <summary>Gets the minimum depth for this deployment.</summary>
	public int MinDepth => _minDepth;

	/// <summary>Gets the maximum depth for this deployment.</summary>
	public int MaxDepth => _maxDepth;

	/// <summary>Gets the target type for this mission (ie: alien control consoles and synomium devices).</summary>
	public int ObjectiveType => _objectiveType;

	/// <summary>Gets the number of objectives required by this mission.</summary>
	public int ObjectivesRequired => _objectivesRequired;

	/// <summary>Gets the string name for the popup to splash when the objective conditions are met.</summary>
	public string ObjectivePopup => _objectivePopup;

	/// <summary>Fills out the variables associated with mission success, and returns if those variables actually contain anything.</summary>
	public bool GetObjectiveCompleteInfo(ref string text, ref int score, ref string missionText)
	{
		text = _objectiveCompleteText;
		score = _objectiveCompleteScore;
		missionText = _missionCompleteText;
		return !string.IsNullOrEmpty(text);
	}

	/// <summary>Fills out the variables associated with mission failure, and returns if those variables actually contain anything.</summary>
	public bool GetObjectiveFailedInfo(ref string text, ref int score, ref string missionText)
	{
		text = _objectiveFailedText;
		score = _objectiveFailedScore;
		missionText = _missionFailedText;
		return !string.IsNullOrEmpty(text);
	}

	/// <summary>Gets the score penalty XCom receives for letting this mission despawn.</summary>
	public int DespawnPenalty => _despawnPenalty;

	/// <summary>Gets the score penalty XCom receives for aborting this mission.</summary>
	public int AbortPenalty => _abortPenalty;

	/// <summary>
	/// Gets the score penalty against XCom for this site existing.
	/// This penalty is applied half-hourly for sites and daily for bases.
	/// </summary>
	public int Points => _points;

	/// <summary>Gets the maximum number of turns we have before this mission ends.</summary>
	public int TurnLimit => _turnLimit;

	/// <summary>Gets the action type to perform when the timer expires.</summary>
	public ChronoTrigger ChronoTrigger => _chronoTrigger;

	/// <summary>Gets the turn at which the players become exposed to the AI.</summary>
	public int CheatTurn => _cheatTurn;

	/// <summary>Gets whether or not this is an alien base (purely for new battle mode).</summary>
	public bool IsAlienBase => _isAlienBase;

	/// <summary>Gets whether or not this mission should be hidden (purely for new battle mode).</summary>
	public bool IsHidden => _isHidden;

	/// <summary>Gets the chance for deciding to spawn an alien base on fakeUnderwater globe texture.</summary>
	public int FakeUnderwaterSpawnChance => _fakeUnderwaterSpawnChance;

	public string ChooseGenMissionType() => _genMission.Choose();

	public int GenMissionFrequency => _genMissionFrequency;

	public int GenMissionLimit => _genMissionLimit;

	public bool IsGenMissionRaceFromAlienBase => _genMissionRaceFromAlienBase;

	public bool KeepCraftAfterFailedMission() => _keepCraftAfterFailedMission;

	public bool AllowObjectiveRecovery() => _allowObjectiveRecovery;

	public EscapeType EscapeType => _escapeType;

	/// <summary>Gets the percentage of VIP units that must survive in order to accomplish the mission.</summary>
	public int VIPSurvivalPercentage => _vipSurvivalPercentage;

	/// <summary>Chooses one of the available hunt missions.</summary>
	/// <param name="monthsPassed">The number of months that have passed in the game world.</param>
	public string GenerateHuntMission(int monthsPassed)
	{
		// reverse iteration like the C++ const_reverse_iterator (running past the start is UB there, throws here)
		int rw = _huntMissionDistribution.Count - 1;
		while (monthsPassed < _huntMissionDistribution[rw].first)
			--rw;
		return _huntMissionDistribution[rw].second.Choose();
	}

	/// <summary>Returns the Alien Base self destruct code (research topic).</summary>
	public string BaseSelfDestructCode => _baseSelfDestructCode;

	/// <summary>Gets the detection range of an alien base.</summary>
	public double BaseDetectionRange => _baseDetectionRange;

	/// <summary>Gets the chance (in percent) of an alien base to detect a player's craft (once every 10 minutes).</summary>
	public int BaseDetectionChance => _baseDetectionChance;

	/// <summary>Gets the maximum frequency (in minutes) of hunt missions generated by an alien base.</summary>
	public int HuntMissionMaxFrequency => _huntMissionMaxFrequency;

	/// <summary>Should the hunt missions inherit the race from the alien base (vanilla behavior), or take it from their own 'raceWeights'?</summary>
	public bool IsHuntMissionRaceFromAlienBase => _huntMissionRaceFromAlienBase;

	/// <summary>Chooses one of the available alien base upgrades.</summary>
	/// <param name="baseAgeInMonths">The number of months that have passed in the game world since the alien base spawned.</param>
	public string GenerateAlienBaseUpgrade(int baseAgeInMonths)
	{
		if (_alienBaseUpgrades.Count == 0)
			return "";

		int rw = _alienBaseUpgrades.Count - 1;
		while (baseAgeInMonths < _alienBaseUpgrades[rw].first)
			--rw;
		return _alienBaseUpgrades[rw].second.Choose();
	}

	/// <summary>Should the age of an alien base be reset after an upgrade (from this type)?</summary>
	public bool ResetAlienBaseAgeAfterUpgrade() => _resetAlienBaseAgeAfterUpgrade;

	/// <summary>Should the age of an alien base be reset after an upgrade (into this type)?</summary>
	public bool ResetAlienBaseAge() => _resetAlienBaseAge;

	/// <summary>Gets the new race for an alien base after an upgrade (into this type).</summary>
	public string UpgradeRace => _upgradeRace;

	/// <summary>Gets the alien race evolution rules (month, old race, new race), sorted by month descending.</summary>
	public List<(int, string, string)> AlienRaceEvolution => _alienRaceEvolution;

	/// <summary>Should items on the "weapon pile" be hidden from the player?</summary>
	public bool NoWeaponPile => _noWeaponPile;
}
