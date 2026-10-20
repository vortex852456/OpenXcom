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
// Ported from: src/Mod/RuleAlienMission.h, src/Mod/RuleAlienMission.cpp
#nullable disable

namespace Acom;

public enum AlienMissionOperationType : uint
{
	AMOT_SPACE = 0,
	AMOT_REGION_EXISTING_BASE = 1,
	AMOT_REGION_NEW_BASE = 2,
	AMOT_REGION_NEW_BASE_IF_NECESSARY = 3,
	AMOT_EARTH_EXISTING_BASE = 4,
	AMOT_EARTH_NEW_BASE_IF_NECESSARY = 5,
	AMOT_EXISTING_BASE_HUNT_MISSION = 6,
}

/// <summary>
/// Information about a mission wave.
/// Mission waves control the UFOs that will be generated during an alien mission.
/// </summary>
public class MissionWave
{
	/// <summary>The type of the spawned UFOs.</summary>
	public string ufoType = "";
	/// <summary>The number of UFOs that will be generated (sequentially, one every <see cref="spawnTimer"/> minutes).</summary>
	public int ufoCount;
	/// <summary>The trajectory ID for this wave's UFOs (controls the way UFOs fly around the Geoscape).</summary>
	public string trajectory = "";
	/// <summary>Number of minutes between UFOs in the wave (the actual value used is spawnTimer/4 or 3*spawnTimer/4).</summary>
	public int spawnTimer;
	/// <summary>This wave performs the mission objective (the UFO executes a special action based on the mission objective).</summary>
	public bool objective;
	/// <summary>
	/// This wave performs the mission objective in a rectangular (non-point) area: make the UFO land on a random
	/// landing site (instead of always top left) and make the mission site spawn exactly on the landing site.
	/// </summary>
	public bool objectiveOnTheLandingSite;
	/// <summary>Make the mission site spawn on an xcom base (or not at all).</summary>
	public bool objectiveOnXcomBase;
	/// <summary>
	/// The chance to become a hunter-killer UFO upon spawning.
	/// -1 (default): take the info from RuleUfo; 0: not a hunter-killer;
	/// 1..99: percentage chance to be flagged as hunter-killer upon spawn; 100: always a hunter-killer.
	/// </summary>
	public int hunterKillerPercentage;
	/// <summary>
	/// Algorithm to use when prioritizing xcom targets.
	/// -1 (default): take the info from RuleUfo; 0: prefer hunting xcom interceptors;
	/// 1: prefer hunting xcom transports; 2: random preference (0 or 1) determined at spawn.
	/// </summary>
	public int huntMode;
	/// <summary>
	/// Algorithm to use when considering retreating from the dogfight.
	/// -1 (default): take the info from RuleUfo; 0: flee if you're losing;
	/// 1: never flee, never crash (is destroyed instead of crashing); 2: random preference (0 or 1) determined at spawn.
	/// </summary>
	public int huntBehavior;
	/// <summary>Does this wave escort/protect the previous wave(s) of the same mission?</summary>
	public bool escort;
	/// <summary>
	/// The chance to interrupt the alien mission when successfully shooting down a UFO from this wave.
	/// 0 (default): cannot interrupt; 1..99: percentage chance to be interrupted; 100: always interrupted.
	/// </summary>
	public int interruptPercentage;

	/// <summary>YAML deserialization (C++ read(ryml::ConstNodeRef, MissionWave*)).</summary>
	public static bool YamlRead(YamlNodeReader reader, ref MissionWave val)
	{
		val ??= new MissionWave();
		reader.TryRead("ufo", ref val.ufoType);
		reader.TryRead("count", ref val.ufoCount);
		reader.TryRead("trajectory", ref val.trajectory);
		reader.TryRead("timer", ref val.spawnTimer);
		reader.ReadNode("objective", ref val.objective, false);
		reader.ReadNode("objectiveOnTheLandingSite", ref val.objectiveOnTheLandingSite, false);
		reader.ReadNode("objectiveOnXcomBase", ref val.objectiveOnXcomBase, false);
		reader.ReadNode("hunterKillerPercentage", ref val.hunterKillerPercentage, -1);
		reader.ReadNode("huntMode", ref val.huntMode, -1);
		reader.ReadNode("huntBehavior", ref val.huntBehavior, -1);
		reader.ReadNode("escort", ref val.escort, false);
		reader.ReadNode("interruptPercentage", ref val.interruptPercentage, 0);
		return true;
	}
}

public enum MissionObjective : uint
{
	OBJECTIVE_SCORE = 0,
	OBJECTIVE_INFILTRATION = 1,
	OBJECTIVE_BASE = 2,
	OBJECTIVE_SITE = 3,
	OBJECTIVE_RETALIATION = 4,
	OBJECTIVE_SUPPLY = 5,
	OBJECTIVE_INSTANT_RETALIATION = 6,
}

/// <summary>
/// Stores fixed information about a mission type.
/// It stores the mission waves and the distribution of the races that can
/// undertake the mission based on game date.
/// </summary>
public class RuleAlienMission
{
	/// <summary>The mission's type ID.</summary>
	private readonly string _type;
	/// <summary>The UFO to use for spawning.</summary>
	private string _spawnUfo = "";
	/// <summary>Should the scouting phase of the retaliation mission be skipped?</summary>
	private bool _skipScoutingPhase;
	/// <summary>The race distribution over game time.</summary>
	private readonly List<(int first, WeightedOptions second)> _raceDistribution = [];
	/// <summary>The mission's weights.</summary>
	private SortedDictionary<int, int> _weights = [];
	/// <summary>The mission's waves.</summary>
	private List<MissionWave> _waves = [];
	/// <summary>The mission's points.</summary>
	private int _points;
	/// <summary>The mission's objective.</summary>
	private MissionObjective _objective;
	/// <summary>The mission zone to use for spawning.</summary>
	private int _spawnZone;
	/// <summary>The odds that this mission will result in retaliation.</summary>
	private int _retaliationOdds;
	/// <summary>Should the infiltration end after first cycle or continue indefinitely?</summary>
	private bool _endlessInfiltration;
	/// <summary>Should the retaliation mission end after the first base defense or continue until all already spawned UFOs disappear?</summary>
	private bool _multiUfoRetaliation;
	/// <summary>Should the retaliation mission continue generating UFOs even after the first base defense?</summary>
	private bool _multiUfoRetaliationExtra;
	/// <summary>Should the retaliation UFO ignore xcom base defenses?</summary>
	private bool _ignoreBaseDefenses;
	/// <summary>Should the retaliation UFO display extra info before xcom base defenses?</summary>
	private bool _instaHyper;
	/// <summary>Should the mission site despawn even if targeted?</summary>
	private bool _despawnEvenIfTargeted;
	/// <summary>Should the original UFO be respawned when the mission site despawns?</summary>
	private bool _respawnUfoAfterSiteDespawn;
	/// <summary>Should the spawned alien base be revealed immediately?</summary>
	private bool _showAlienBase;
	/// <summary>The research topic that interrupts this mission type (when discovered).</summary>
	private string _interruptResearch = "";
	/// <summary>The type of missionSite to spawn (if any).</summary>
	private string _siteType = "";
	/// <summary>From where does this mission operate?</summary>
	private AlienMissionOperationType _operationType;
	/// <summary>The mission zone for spawning the operation base (if necessary).</summary>
	private int _operationSpawnZone;
	/// <summary>The type of operation base to spawn (if any).</summary>
	private string _operationBaseType = "";
	/// <summary>The odds of this mission targetting an xcom base. Works only for "gen missions" spawned by an alien base.</summary>
	private int _targetBaseOdds;
	/// <summary>The region distribution over game time. Works only for "gen missions" spawned by an alien base.</summary>
	private readonly List<(int first, WeightedOptions second)> _regionWeights = [];

	public RuleAlienMission(string type)
	{
		_type = type;
		_skipScoutingPhase = false;
		_points = 0;
		_objective = MissionObjective.OBJECTIVE_SCORE;
		_spawnZone = -1;
		_retaliationOdds = -1;
		_endlessInfiltration = true;
		_multiUfoRetaliation = false;
		_multiUfoRetaliationExtra = false;
		_ignoreBaseDefenses = false;
		_instaHyper = false;
		_despawnEvenIfTargeted = false;
		_respawnUfoAfterSiteDespawn = false;
		_showAlienBase = false;
		_operationType = AlienMissionOperationType.AMOT_SPACE;
		_operationSpawnZone = -1;
		_targetBaseOdds = 0;
	}

	/// <summary>Gets the mission's type.</summary>
	public string Type => _type;

	/// <summary>Does this mission use a weighted distribution to pick a race?</summary>
	public bool HasRaceWeights => _raceDistribution.Count != 0;

	/// <summary>
	/// Chooses one of the available races for this mission.
	/// The racial distribution may vary based on the current game date.
	/// </summary>
	/// <param name="monthsPassed">The number of months that have passed in the game world.</param>
	/// <returns>The string id of the race.</returns>
	public string GenerateRace(int monthsPassed)
	{
		int rc = _raceDistribution.Count - 1;
		while (rc >= 0 && monthsPassed < _raceDistribution[rc].first)
			--rc;
		if (rc < 0)
			return "";
		return _raceDistribution[rc].second.Choose();
	}

	/// <summary>Loads the mission data from a YAML node.</summary>
	public void Load(YamlNodeReader node)
	{
		var reader = node.UseIndex();
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent);
		}

		reader.TryRead("points", ref _points);
		reader.TryRead("waves", ref _waves);
		reader.TryRead("objective", ref _objective);
		reader.TryRead("spawnUfo", ref _spawnUfo);
		reader.TryRead("skipScoutingPhase", ref _skipScoutingPhase);
		reader.TryRead("spawnZone", ref _spawnZone);
		reader.TryRead("missionWeights", ref _weights);
		reader.TryRead("retaliationOdds", ref _retaliationOdds);
		reader.TryRead("endlessInfiltration", ref _endlessInfiltration);
		reader.TryRead("multiUfoRetaliation", ref _multiUfoRetaliation);
		reader.TryRead("multiUfoRetaliationExtra", ref _multiUfoRetaliationExtra);
		if (_multiUfoRetaliationExtra)
		{
			// yes, I should have changed _multiUfoRetaliation to int instead, but I'm lazy
			// I also don't want to break existing mods or handle it in a complicated way
			_multiUfoRetaliation = true;
		}
		reader.TryRead("ignoreBaseDefenses", ref _ignoreBaseDefenses);
		reader.TryRead("instaHyper", ref _instaHyper);
		reader.TryRead("despawnEvenIfTargeted", ref _despawnEvenIfTargeted);
		reader.TryRead("respawnUfoAfterSiteDespawn", ref _respawnUfoAfterSiteDespawn);
		reader.TryRead("showAlienBase", ref _showAlienBase);
		reader.TryRead("interruptResearch", ref _interruptResearch);
		reader.TryRead("siteType", ref _siteType);
		reader.TryRead("operationType", ref _operationType);
		reader.TryRead("operationSpawnZone", ref _operationSpawnZone);
		reader.TryRead("operationBaseType", ref _operationBaseType);
		reader.TryRead("targetBaseOdds", ref _targetBaseOdds);
		foreach (var weights in reader["regionWeights"].Children())
		{
			var nw = new WeightedOptions();
			nw.Load(weights);
			_regionWeights.Add((weights.ReadKey(0), nw));
		}
		//Only allow full replacement of mission racial distribution.
		var raceWeights = reader["raceWeights"];
		if (raceWeights)
		{
			//Place in the associative container so we can index by month and keep entries sorted.
			var assoc = new SortedDictionary<int, WeightedOptions>();
			foreach (var (first, second) in _raceDistribution)
			{
				assoc.TryAdd(first, second); // std::map::insert keeps the first entry
			}
			// Now go through the node contents and merge with existing data.
			foreach (var nn in raceWeights.Children())
			{
				int month = nn.ReadKey<int>();
				if (!assoc.TryGetValue(month, out var existing))
				{
					// New entry, load and add it.
					var nw = new WeightedOptions();
					nw.Load(nn);
					assoc.Add(month, nw);
				}
				else
				{
					// Existing entry, update it.
					existing.Load(nn);
				}
			}
			// Now replace values in our actual member variable!
			_raceDistribution.Clear();
			foreach (var ii in assoc)
			{
				if (ii.Value.Empty())
				{
					// Don't keep empty lists.
				}
				else
				{
					// Place it
					_raceDistribution.Add((ii.Key, ii.Value));
				}
			}
		}
	}

	/// <summary>Gets the number of waves.</summary>
	public int WaveCount => _waves.Count;

	/// <summary>Gets the full wave information.</summary>
	public MissionWave GetWave(int index) => _waves[index];

	/// <summary>Returns the Alien score for this mission.</summary>
	public int Points => _points;

	/// <summary>Gets the objective for this mission.</summary>
	public MissionObjective Objective => _objective;

	/// <summary>Gets the UFO type for special spawns.</summary>
	public string SpawnUfo => _spawnUfo;

	/// <summary>Should the scouting phase of the retaliation mission be skipped?</summary>
	public bool SkipScoutingPhase() => _skipScoutingPhase;

	/// <summary>Gets the zone for spawning an alien site or base.</summary>
	public int SpawnZone => _spawnZone;

	/// <summary>Returns the chances of this mission being generated based on the current game date.</summary>
	/// <param name="monthsPassed">The number of months that have passed in the game world.</param>
	public int GetWeight(int monthsPassed)
	{
		if (_weights.Count == 0)
		{
			return 1;
		}
		int weight = 0;
		foreach (var pair in _weights)
		{
			if (pair.Key > monthsPassed)
			{
				break;
			}
			weight = pair.Value;
		}
		return weight;
	}

	/// <summary>Gets the inherent odds of this mission spawning a retaliation mission.</summary>
	public int RetaliationOdds => _retaliationOdds;

	/// <summary>Should the infiltration end after first cycle or continue indefinitely?</summary>
	public bool IsEndlessInfiltration => _endlessInfiltration;

	/// <summary>Should the retaliation mission end after the first base defense or continue until all already spawned UFOs disappear?</summary>
	public bool IsMultiUfoRetaliation => _multiUfoRetaliation;

	/// <summary>Should the retaliation mission continue generating UFOs even after the first base defense?</summary>
	public bool IsMultiUfoRetaliationExtra => _multiUfoRetaliationExtra;

	/// <summary>Should the retaliation UFO ignore xcom base defenses?</summary>
	public bool IgnoreBaseDefenses() => _ignoreBaseDefenses;

	/// <summary>Show extra info during base defense even if the UFO was not yet hyper-detected classically?</summary>
	public bool IsInstaHyper => _instaHyper;

	/// <summary>Should the mission site despawn even if targeted?</summary>
	public bool DespawnEvenIfTargeted() => _despawnEvenIfTargeted;

	/// <summary>Should the original UFO be respawned when the mission site despawns?</summary>
	public bool RespawnUfoAfterSiteDespawn() => _respawnUfoAfterSiteDespawn;

	/// <summary>Should the spawned alien base be revealed immediately?</summary>
	public bool ShowAlienBase() => _showAlienBase;

	/// <summary>Gets the ID of the research topic that interrupts this mission (if any).</summary>
	public string InterruptResearch => _interruptResearch;

	/// <summary>The type of missionSite to spawn (if any).</summary>
	public string SiteType => _siteType;

	/// <summary>From where does this mission operate?</summary>
	public AlienMissionOperationType OperationType => _operationType;

	/// <summary>Gets the mission zone for spawning the operation base (if necessary).</summary>
	public int OperationSpawnZone => _operationSpawnZone;

	/// <summary>Gets the type of operation base to spawn (if any).</summary>
	public string OperationBaseType => _operationBaseType;

	/// <summary>Gets the odds of this mission targetting an xcom base. Works only for "gen missions" spawned by an alien base.</summary>
	public int TargetBaseOdds => _targetBaseOdds;

	/// <summary>Does this mission have region weights? Works only for "gen missions" spawned by an alien base.</summary>
	public bool HasRegionWeights => _regionWeights.Count != 0;

	/// <summary>
	/// Chooses one of the available regions for this mission.
	/// The region distribution may vary based on the current game date.
	/// </summary>
	/// <param name="monthsPassed">The number of months that have passed in the game world.</param>
	public string GenerateRegion(int monthsPassed)
	{
		// reverse iteration like the C++ const_reverse_iterator (running past the start is UB there, throws here)
		int rc = _regionWeights.Count - 1;
		while (monthsPassed < _regionWeights[rc].first)
			--rc;
		return _regionWeights[rc].second.Choose();
	}
}
