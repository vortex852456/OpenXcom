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
// Ported from: src/Savegame/Ufo.h, src/Savegame/Ufo.cpp
// Port status: partial (missing: script hooks)
#nullable disable

namespace Acom;

public enum UfoDetection
{
	DETECTION_NONE = 0,
	DETECTION_RADAR = 1,
	DETECTION_HYPERWAVE = 3,
}

/// <summary>
/// Represents an alien UFO on the map.
/// Contains variable info about a UFO like
/// position, damage, speed, etc.
/// </summary>
/// <seealso cref="RuleUfo"/>
public class Ufo : MovingTarget
{
	/// <summary>C++ static const char *ALTITUDE_STRING[].</summary>
	public static readonly string[] ALTITUDE_STRING =
	[
		"STR_GROUND",
		"STR_VERY_LOW",
		"STR_LOW_UC",
		"STR_HIGH_UC",
		"STR_VERY_HIGH",
	];

	/// <summary>Name of class used in script.</summary>
	public const string ScriptName = "Ufo";

	private RuleUfo _rules;
	private int _uniqueId;
	private int _missionWaveNumber;
	private int _crashId, _landId, _damage;
	private string _direction, _altitude;
	private UfoStatus _status;
	/// <summary>C++ size_t.</summary>
	private int _secondsRemaining;
	private bool _inBattlescape;
	private (string first, int second) _shotDownByCraftId = ("", 0);
	private AlienMission _mission;
	private UfoTrajectory _trajectory;
	/// <summary>C++ size_t.</summary>
	private int _trajectoryPoint;
	private bool _detected, _hyperDetected, _processedIntercept;
	private int _shootingAt, _hitFrame, _fireCountdown, _escapeCountdown;
	private RuleUfoStats _stats;
	private int _shield, _shieldRechargeHandle;
	private int _tractorBeamSlowdown;
	private bool _isHunterKiller, _isEscort;
	private int _huntMode, _huntBehavior;
	private bool _isHunting, _isEscorting;
	private int _softlockShotCounter;
	private Waypoint _origWaypoint;

	/// <summary>Initializes a UFO of the specified type.</summary>
	/// <param name="rules">Pointer to ruleset.</param>
	/// <param name="uniqueId">Unique ID to assign to the UFO (0 to not assign).</param>
	/// <param name="hunterKillerPercentage">Chance to become a hunter-killer.</param>
	/// <param name="huntMode">Hunt mode (values above 1 pick randomly).</param>
	/// <param name="huntBehavior">Hunt behavior (values above 1 pick randomly).</param>
	public Ufo(RuleUfo rules, int uniqueId, int hunterKillerPercentage = 0, int huntMode = 0, int huntBehavior = 0) : base()
	{
		_rules = rules;
		_missionWaveNumber = -1;
		_crashId = 0;
		_landId = 0;
		_damage = 0;
		_direction = "STR_NORTH";
		_altitude = "STR_HIGH_UC";
		_status = UfoStatus.FLYING;
		_secondsRemaining = 0;
		_inBattlescape = false;
		_mission = null;
		_trajectory = null;
		_trajectoryPoint = 0;
		_detected = false;
		_hyperDetected = false;
		_processedIntercept = false;
		_shootingAt = 0;
		_hitFrame = 0;
		_fireCountdown = 0;
		_escapeCountdown = 0;
		_shield = -1;
		_shieldRechargeHandle = 0;
		_tractorBeamSlowdown = 0;
		_isHunterKiller = false;
		_isEscort = false;
		_huntMode = 0;
		_huntBehavior = 0;
		_isHunting = false;
		_isEscorting = false;
		_softlockShotCounter = 0;
		_origWaypoint = null;

		_stats = new RuleUfoStats(rules.Stats);
		if (uniqueId != 0)
		{
			_uniqueId = uniqueId;
		}
		if (hunterKillerPercentage > 0)
		{
			_isHunterKiller = Rng.Percent(hunterKillerPercentage);
			if (_isHunterKiller)
			{
				_huntMode = huntMode > 1 ? Rng.Generate(0, 1) : huntMode;
				_huntBehavior = huntBehavior > 1 ? Rng.Generate(0, 1) : huntBehavior;
			}
		}
	}

	/// <summary>
	/// C++ destructor (~Ufo): makes sure our mission forgets us, and we only delete targets we own (waypoints).
	/// Call where the C++ code deletes the UFO.
	/// </summary>
	public override void Destroy()
	{
		if (_mission != null)
		{
			_mission.DecreaseLiveUfos();
		}

		ResetOriginalDestination(false);

		if (_dest != null)
		{
			if (_dest is Waypoint wp)
			{
				wp.Destroy();
				_dest = null;
			}
		}
		base.Destroy();
	}

	/// <summary>Loads the UFO from a YAML file.</summary>
	/// <param name="node">YAML node.</param>
	/// <param name="mod">The game mod. Use to access the trajectory rules.</param>
	/// <param name="game">The game data. Used to find the UFO's mission.</param>
	public void Load(YamlNodeReader node, Mod mod, SavedGame game)
	{
		var reader = node.UseIndex();
		base.Load(reader);
		reader.TryRead("uniqueId", ref _uniqueId);
		reader.TryRead("missionWaveNumber", ref _missionWaveNumber);
		reader.TryRead("crashId", ref _crashId);
		reader.TryRead("landId", ref _landId);
		reader.TryRead("damage", ref _damage);
		reader.TryRead("shield", ref _shield);
		reader.TryRead("shieldRechargeHandle", ref _shieldRechargeHandle);
		reader.TryRead("altitude", ref _altitude);
		reader.TryRead("direction", ref _direction);
		reader.TryRead("detected", ref _detected);
		reader.TryRead("hyperDetected", ref _hyperDetected);
		reader.TryRead("secondsRemaining", ref _secondsRemaining);
		reader.TryRead("inBattlescape", ref _inBattlescape);
		_dest = new Waypoint();
		_dest.Longitude = reader["dest"]["lon"].ReadVal(_lon);
		_dest.Latitude = reader["dest"]["lat"].ReadVal(_lat);
		reader.TryRead("status", ref _status);
		if (game.MonthsPassed != -1)
		{
			int missionID = reader["mission"].ReadVal<int>();
			AlienMission found = null;
			foreach (var am in game.AlienMissions)
			{
				if (am.Id == missionID)
				{
					found = am;
					break;
				}
			}
			if (found == null)
			{
				// Corrupt save file.
				throw new InvalidOperationException("Unknown UFO mission, save file is corrupt.");
			}
			_mission = found;
			_stats.Add(_rules.GetRaceBonus(_mission.Race));

			string tid = reader["trajectory"].ReadVal<string>();
			_trajectory = mod.GetUfoTrajectory(tid);
			if (_trajectory == null)
			{
				// Corrupt save file.
				throw new InvalidOperationException("Unknown UFO trajectory, save file is corrupt.");
			}
			reader.TryRead("trajectoryPoint", ref _trajectoryPoint);
		}
		reader.TryRead("fireCountdown", ref _fireCountdown);
		reader.TryRead("escapeCountdown", ref _escapeCountdown);
		if (_inBattlescape)
			Speed = 0;
	}

	/// <summary>Finishes loading the UFO from YAML (called after XCOM craft and other UFOs are loaded).</summary>
	/// <param name="reader">YAML node.</param>
	/// <param name="save">The game data. Used to find the UFO's target (= XCOM craft or another UFO).</param>
	public void FinishLoading(YamlNodeReader reader, SavedGame save)
	{
		reader.TryRead("isHunterKiller", ref _isHunterKiller);
		reader.TryRead("isEscort", ref _isEscort);
		reader.TryRead("huntMode", ref _huntMode);
		reader.TryRead("huntBehavior", ref _huntBehavior);
		reader.TryRead("isHunting", ref _isHunting);
		reader.TryRead("isEscorting", ref _isEscorting);
		reader.TryRead("softlockShotCounter", ref _softlockShotCounter);

		// Note (C++ behaviour kept): Destination resets _isHunting/_isEscorting.
		if (_isHunting)
		{
			var dest = reader["dest"];
			if (dest)
			{
				string type = dest["type"].ReadVal<string>();
				int id = dest["id"].ReadVal<int>();
				bool found = false;
				foreach (var xbase in save.Bases)
				{
					foreach (var xcraft in xbase.Crafts)
					{
						if (xcraft.Id == id && xcraft.Rules.Type == type)
						{
							if (_dest != null)
							{
								// this is just a dummy waypoint created during normal loading, not a craft... yet
								_dest.Destroy();
								_dest = null;
							}
							Destination = xcraft;
							found = true;
							break;
						}
					}
					if (found) break;
				}
			}
		}
		else if (_isEscorting)
		{
			var dest = reader["dest"];
			if (dest)
			{
				string type = dest["type"].ReadVal<string>();
				if (type == "STR_UFO")
				{
					int uniqueUfoId = dest["uniqueId"].ReadVal(0);
					if (uniqueUfoId > 0)
					{
						foreach (var ufo in save.Ufos)
						{
							if (ufo.UniqueId == uniqueUfoId)
							{
								if (_dest != null)
								{
									// this is just a dummy waypoint created during normal loading, not a UFO... yet
									_dest.Destroy();
									_dest = null;
								}
								Destination = ufo;
								break;
							}
						}
					}
				}
			}
		}
		//if (_isHunting || _isEscorting)
		{
			var origWaypoint = reader["origWaypoint"];
			if (origWaypoint)
			{
				_origWaypoint = new Waypoint();
				_origWaypoint.Longitude = origWaypoint["lon"].ReadVal<double>();
				_origWaypoint.Latitude = origWaypoint["lat"].ReadVal<double>();
			}
		}
	}

	/// <summary>Saves the UFO to a YAML file.</summary>
	/// <param name="writer">YAML node.</param>
	/// <param name="newBattle">Saving for a new battle (no mission/trajectory).</param>
	public void Save(YamlNodeWriter writer, bool newBattle)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.SetAsMap();
		base.Save(writer);
		writer.Write("type", _rules.Type);
		writer.Write("uniqueId", _uniqueId);
		writer.Write("missionWaveNumber", _missionWaveNumber);
		if (_crashId != 0)
			writer.Write("crashId", _crashId);
		else if (_landId != 0)
			writer.Write("landId", _landId);
		writer.Write("damage", _damage);
		writer.Write("shield", _shield);
		writer.Write("shieldRechargeHandle", _shieldRechargeHandle);
		writer.Write("altitude", _altitude);
		writer.Write("direction", _direction);
		writer.Write("status", _status);
		if (_detected)
			writer.Write("detected", _detected);
		if (_hyperDetected)
			writer.Write("hyperDetected", _hyperDetected);
		if (_secondsRemaining != 0)
			writer.Write("secondsRemaining", _secondsRemaining);
		if (_inBattlescape)
			writer.Write("inBattlescape", _inBattlescape);
		if (_isHunterKiller)
			writer.Write("isHunterKiller", _isHunterKiller);
		if (_isEscort)
			writer.Write("isEscort", _isEscort);
		writer.Write("huntMode", _huntMode);
		writer.Write("huntBehavior", _huntBehavior);
		if (_isHunting)
			writer.Write("isHunting", _isHunting);
		if (_isEscorting)
			writer.Write("isEscorting", _isEscorting);
		if (_softlockShotCounter != 0)
			writer.Write("softlockShotCounter", _softlockShotCounter);
		if (_origWaypoint != null)
			_origWaypoint.Save(writer["origWaypoint"]);
		if (!newBattle)
		{
			writer.Write("mission", _mission.Id);
			writer.Write("trajectory", _trajectory.ID);
			writer.Write("trajectoryPoint", _trajectoryPoint);
		}

		writer.Write("fireCountdown", _fireCountdown);
		writer.Write("escapeCountdown", _escapeCountdown);
	}

	/// <summary>Saves the UFO's unique identifiers to a YAML file.</summary>
	public override void SaveId(YamlNodeWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.SetAsMap();
		base.SaveId(writer);
		// this is needed, because _id is NOT unique until the UFO is detected
		// and UFOs can be referenced by other entities even before they are detected
		// (e.g. when they escort other UFOs)
		writer.Write("uniqueId", _uniqueId);
	}

	/// <summary>The UFO's unique type used for savegame purposes.</summary>
	public override string Type => "STR_UFO";

	/// <summary>The ruleset for the UFO's type.</summary>
	public RuleUfo Rules => _rules;

	/// <summary>Changes the ruleset for the UFO's type. ONLY FOR NEW BATTLE USE!</summary>
	/// <param name="rules">Pointer to ruleset.</param>
	public void ChangeRules(RuleUfo rules) => _rules = rules;

	/// <summary>The UFO's unique ID.</summary>
	public int UniqueId => _uniqueId;

	/// <summary>The mission wave number that created this UFO.</summary>
	public int MissionWaveNumber { get => _missionWaveNumber; set => _missionWaveNumber = value; }

	/// <summary>Returns the UFO's unique default name.</summary>
	/// <param name="lang">Language to get strings from.</param>
	/// <returns>Full name.</returns>
	public override string GetDefaultName(Language lang)
	{
		ArgumentNullException.ThrowIfNull(lang);
		switch (_status)
		{
			case UfoStatus.LANDED:
				return lang.GetString(MarkerName).Arg(_landId);
			case UfoStatus.CRASHED:
				return lang.GetString(MarkerName).Arg(_crashId);
			default:
				return lang.GetString(MarkerName).Arg(_id);
		}
	}

	/// <summary>The name on the globe for the UFO.</summary>
	public override string MarkerName
	{
		get
		{
			switch (_status)
			{
				case UfoStatus.LANDED:
					return "STR_LANDING_SITE_";
				case UfoStatus.CRASHED:
					return "STR_CRASH_SITE_";
				default:
					return "STR_UFO_";
			}
		}
	}

	/// <summary>The marker ID on the globe for the UFO.</summary>
	public override int MarkerId
	{
		get
		{
			switch (_status)
			{
				case UfoStatus.LANDED:
					return _landId;
				case UfoStatus.CRASHED:
					return _crashId;
				default:
					return _id;
			}
		}
	}

	/// <summary>The globe marker for the UFO (-1 if none).</summary>
	public override int Marker
	{
		get
		{
			if (!_detected)
				return -1;
			switch (_status)
			{
				case UfoStatus.LANDED:
					return _rules.LandMarker == -1 ? 3 : _rules.LandMarker;
				case UfoStatus.CRASHED:
					return _rules.CrashMarker == -1 ? 4 : _rules.CrashMarker;
				default:
					return _rules.Marker == -1 ? 2 : _rules.Marker;
			}
		}
	}

	/// <summary>The amount of damage this UFO has taken.</summary>
	public int Damage => _damage;

	/// <summary>Changes the amount of damage this UFO has taken.</summary>
	/// <param name="damage">Amount of damage.</param>
	/// <param name="mod">The game mod.</param>
	public void SetDamage(int damage, Mod mod)
	{
		_damage = damage;
		if (_damage < 0)
		{
			_damage = 0;
		}
		if (IsDestroyed)
		{
			_status = UfoStatus.DESTROYED;
		}
		else if (IsCrashed)
		{
			_status = UfoStatus.CRASHED;
		}
		if (_status == UfoStatus.CRASHED || _status == UfoStatus.DESTROYED)
		{
			int waveNumber = _missionWaveNumber;

			// special case for retaliation UFO attacking the base
			RuleUfo battleshipRule = mod.GetUfo(_mission.Rules.SpawnUfo, true);
			UfoTrajectory assaultTrajectory = mod.GetUfoTrajectory(UfoTrajectory.RETALIATION_ASSAULT_RUN, true);
			if (_rules == battleshipRule && _trajectory == assaultTrajectory)
			{
				waveNumber = _mission.Rules.WaveCount - 1; // last wave
			}

			// backwards save compatibility
			if (waveNumber > -1)
			{
				MissionWave wave = _mission.Rules.GetWave(waveNumber);
				if (wave.interruptPercentage > 0)
				{
					if (Rng.Percent(wave.interruptPercentage))
					{
						_mission.SetInterrupted(true);
					}
				}
			}
		}
	}

	/// <summary>
	/// The ratio between the amount of damage this ufo can take
	/// and the total it can take before it's destroyed.
	/// </summary>
	public int DamagePercentage => (int)Math.Floor((double)_damage / _stats.damageMax * 100);

	/// <summary>Whether this UFO has been detected by radars.</summary>
	public bool Detected { get => _detected; set => _detected = value; }

	/// <summary>
	/// The amount of remaining seconds the UFO has left on the ground.
	/// After this many seconds the UFO will take off, if landed, or disappear, if crashed.
	/// </summary>
	public int SecondsRemaining { get => _secondsRemaining; set => _secondsRemaining = value; }

	/// <summary>The current direction the UFO is heading in.</summary>
	public string Direction => _direction;

	/// <summary>The current altitude of the UFO (string ID); setting it updates the status.</summary>
	public string Altitude
	{
		get => _altitude;
		set
		{
			_altitude = value;
			if (_altitude != "STR_GROUND")
			{
				_status = UfoStatus.FLYING;
			}
			else
			{
				_status = IsCrashed ? UfoStatus.CRASHED : UfoStatus.LANDED;
			}
		}
	}

	/// <summary>The current altitude of the UFO as integer (0-4), -1 if unknown.</summary>
	public int AltitudeInt
	{
		get
		{
			for (int i = 0; i < 5; ++i)
			{
				if (ALTITUDE_STRING[i] == _altitude)
				{
					return i;
				}
			}
			return -1;
		}
	}

	/// <summary>The UFO status.</summary>
	public Ufo.UfoStatus Status { get => _status; set => _status = value; }

	/// <summary>If this UFO took enough damage to cause it to crash.</summary>
	public bool IsCrashed
	{
		get
		{
			// Note: yes, this condition is necessary (in OXCE) and cannot be removed!
			if (IsDestroyed)
				return true;

			if (_huntBehavior == 1 || _rules.IsUnmanned)
			{
				// kamikaze never crash lands; unmanned ditto
				return false;
			}

			return _damage > _stats.damageMax / 2;
		}
	}

	/// <summary>Returns if this UFO is capable of crash-landing (not a kamikaze or unmanned drone).</summary>
	/// <returns>True if the UFO can crash land rather than being destroyed.</returns>
	public bool CanCrash() => _huntBehavior != 1 && !_rules.IsUnmanned;

	/// <summary>If this UFO took enough damage to be destroyed.</summary>
	public bool IsDestroyed
	{
		get
		{
			if (Options.oxceUFONeverDestroyed)
			{
				// Crashable UFOs crash-land instead of being destroyed
				return (_damage >= _stats.damageMax) && !CanCrash();
			}
			return _damage >= _stats.damageMax;
		}
	}

	/// <summary>Calculates the direction for the UFO based on the current raw speed and destination.</summary>
	protected override void CalculateSpeed()
	{
		base.CalculateSpeed();

		double x = _speedLon;
		double y = -_speedLat;

		// This section guards vs. divide-by-zero.
		if (AreSame(x, 0.0) || AreSame(y, 0.0))
		{
			if (AreSame(x, 0.0) && AreSame(y, 0.0))
			{
				_direction = "STR_NONE_UC";
			}
			else if (AreSame(x, 0.0))
			{
				if (y > 0.0f)
				{
					_direction = "STR_NORTH";
				}
				else if (y < 0.0f)
				{
					_direction = "STR_SOUTH";
				}
			}
			else if (AreSame(y, 0.0))
			{
				if (x > 0.0f)
				{
					_direction = "STR_EAST";
				}
				else if (x < 0.0f)
				{
					_direction = "STR_WEST";
				}
			}

			return;
		}

		double theta = Math.Atan2(y, x); // radians
		theta = theta * 180.0f / Math.PI; // +/- 180 deg.

		if (22.5f > theta && theta > -22.5f)
		{
			_direction = "STR_EAST";
		}
		else if (-22.5f > theta && theta > -67.5f)
		{
			_direction = "STR_SOUTH_EAST";
		}
		else if (-67.5f > theta && theta > -112.5f)
		{
			_direction = "STR_SOUTH";
		}
		else if (-112.5f > theta && theta > -157.5f)
		{
			_direction = "STR_SOUTH_WEST";
		}
		else if (-157.5f > theta || theta > 157.5f)
		{
			_direction = "STR_WEST";
		}
		else if (157.5f > theta && theta > 112.5f)
		{
			_direction = "STR_NORTH_WEST";
		}
		else if (112.5f > theta && theta > 67.5f)
		{
			_direction = "STR_NORTH";
		}
		else
		{
			_direction = "STR_NORTH_EAST";
		}
	}

	/// <summary>Moves the UFO to its destination.</summary>
	public void Think()
	{
		switch (_status)
		{
			case UfoStatus.FLYING:
				Move();
				if (ReachedDestination() && !IsHunting && !IsEscorting)
				{
					// Prevent further movement.
					Speed = 0;
				}
				break;
			case UfoStatus.LANDED:
				System.Diagnostics.Debug.Assert(_secondsRemaining >= 5, "Wrong time management.");
				_secondsRemaining -= 5;
				break;
			case UfoStatus.CRASHED:
				if (!_detected)
				{
					_detected = true;
				}
				// This gets handled in GeoscapeState::time30Minutes()
				// Because the original game processes it every 30 minutes!
				// (C++ falls through to DESTROYED/IGNORE_ME, which do nothing)
				break;
			case UfoStatus.DESTROYED:
			case UfoStatus.IGNORE_ME:
				// Do nothing
				break;
		}
	}

	/// <summary>Sets the UFO's battlescape status.</summary>
	/// <param name="inbattle">True if it's in battle, False otherwise.</param>
	public void SetInBattlescape(bool inbattle)
	{
		if (inbattle)
			Speed = 0;
		_inBattlescape = inbattle;
	}

	/// <summary>Is the UFO currently in battle?</summary>
	public bool IsInBattlescape => _inBattlescape;

	/// <summary>The alien race currently residing in the UFO.</summary>
	public string AlienRace => _mission.Race;

	/// <summary>The ID of craft which shot down the UFO.</summary>
	public (string first, int second) ShotDownByCraftId { get => _shotDownByCraftId; set => _shotDownByCraftId = value; }

	/// <summary>
	/// The UFO's visibility to radar detection.
	/// The UFO's size and altitude affect the chances of it being detected by radars.
	/// </summary>
	public int Visibility
	{
		get
		{
			int size = _rules.DefaultVisibility;

			int visibility = 0;
			if (_altitude == "STR_GROUND")
				visibility = -30;
			else if (_altitude == "STR_VERY_LOW")
				visibility = size - 20;
			else if (_altitude == "STR_LOW_UC")
				visibility = size - 10;
			else if (_altitude == "STR_HIGH_UC")
				visibility = size;
			else if (_altitude == "STR_VERY_HIGH")
				visibility = size - 10;

			return visibility;
		}
	}

	/// <summary>The Mission type of the UFO.</summary>
	public string MissionType => _mission.Rules.Type;

	/// <summary>
	/// Sets the mission information of the UFO.
	/// The UFO will start at the first point of the trajectory. The actual UFO
	/// information is not changed here, this only sets the information kept on
	/// behalf of the mission.
	/// </summary>
	/// <param name="mission">Pointer to the actual mission object.</param>
	/// <param name="trajectory">Pointer to the actual mission trajectory.</param>
	public void SetMissionInfo(AlienMission mission, UfoTrajectory trajectory)
	{
		System.Diagnostics.Debug.Assert(_mission == null && mission != null && trajectory != null);
		_mission = mission;
		_mission.IncreaseLiveUfos();
		_trajectoryPoint = 0;
		_trajectory = trajectory;
		_stats.Add(_rules.GetRaceBonus(_mission.Race));
	}

	/// <summary>Whether this UFO has been detected by hyper-wave.</summary>
	public bool HyperDetected { get => _hyperDetected; set => _hyperDetected = value; }

	/// <summary>The UFO's progress on the trajectory track.</summary>
	public int TrajectoryPoint { get => _trajectoryPoint; set => _trajectoryPoint = value; }

	/// <summary>The UFO's trajectory.</summary>
	public UfoTrajectory Trajectory => _trajectory;

	/// <summary>The UFO's mission object.</summary>
	public AlienMission Mission => _mission;

	/// <summary>The Xcom craft targeted by this UFO.</summary>
	public Craft TargetedXcomCraft
	{
		get => _dest as Craft;
		set
		{
			if (value != null)
			{
				BackupOriginalDestination();
				Destination = value;
				_isHunting = true; // must be after setDestination()

				// go hunt your target as quickly as you can
				if (_rules.HuntSpeed > 0)
				{
					Speed = _stats.speedMax * _rules.HuntSpeed / 100;
				}
			}
		}
	}

	/// <summary>Resets the original destination if targeting the given craft.</summary>
	/// <param name="target">Pointer to Xcom craft.</param>
	public void ResetOriginalDestination(Craft target)
	{
		if (target == TargetedXcomCraft)
		{
			ResetOriginalDestination(true);
		}
	}

	/// <summary>Resets the original destination.</summary>
	/// <param name="debugHelper">Throw if there is no original destination.</param>
	public void ResetOriginalDestination(bool debugHelper)
	{
		if (_origWaypoint != null)
		{
			var wp = new Waypoint();
			wp.Latitude = _origWaypoint.Latitude;
			wp.Longitude = _origWaypoint.Longitude;

			Destination = wp; // hunting and escorting flags will be reset too

			_origWaypoint.Destroy();
			_origWaypoint = null;
		}
		else if (debugHelper)
		{
			throw new InvalidOperationException("Corrupt state: Unknown original UFO destination.");
		}
	}

	/// <summary>Sets the UFO escorted by this UFO.</summary>
	/// <param name="ufo">Pointer to escorted UFO.</param>
	public void SetEscortedUfo(Ufo ufo)
	{
		if (ufo != null)
		{
			BackupOriginalDestination();
			Destination = ufo;
			_isEscorting = true; // must be after setDestination()

			// go protect your target as quickly as you can
			Speed = _stats.speedMax;
		}
	}

	/// <summary>Backs up the original destination.</summary>
	private void BackupOriginalDestination()
	{
		// if we're not hunting or escorting yet, remember the original destination waypoint
		// if we're already occupied, keep the original information
		if (!_isHunting && !_isEscorting)
		{
			if (_origWaypoint != null)
			{
				_origWaypoint.Destroy();
				_origWaypoint = null;
			}
			_origWaypoint = new Waypoint();
			_origWaypoint.Latitude = _dest.Latitude;
			_origWaypoint.Longitude = _dest.Longitude;
		}
	}

	/// <summary>Handle destination changes, making sure to delete old waypoint destinations.</summary>
	public override Target Destination
	{
		set
		{
			var old = _dest as Waypoint;
			base.Destination = value;
			if (old != null && !_isHunting && !_isEscorting)
			{
				// delete only waypoints, not xcom craft or other UFOs
				old.Destroy();
			}
			_isHunting = false;   // reset flag, will be set in setTargetedXcomCraft()
			_isEscorting = false; // reset flag, will be set in setEscortedUfo()
		}
	}

	/// <summary>Which interception window the UFO is active in.</summary>
	public int ShootingAt { get => _shootingAt; set => _shootingAt = value; }

	/// <summary>The UFO's landing site ID.</summary>
	public int LandId { get => _landId; set => _landId = value; }

	/// <summary>The UFO's crash site ID.</summary>
	public int CrashId { get => _crashId; set => _crashId = value; }

	/// <summary>The UFO's hit frame.</summary>
	public int HitFrame { get => _hitFrame; set => _hitFrame = value; }

	/// <summary>The UFO's stats.</summary>
	public RuleUfoStats CraftStats => _stats;

	/// <summary>The number of ticks until the ufo is ready to fire.</summary>
	public int FireCountdown { get => _fireCountdown; set => _fireCountdown = value; }

	/// <summary>The number of ticks until the ship tries to leave a dogfight.</summary>
	public int EscapeCountdown { get => _escapeCountdown; set => _escapeCountdown = value; }

	/// <summary>
	/// Whether this ufo has had its timers decremented on this cycle of interception updates.
	/// Prevents multiple interceptions from decrementing or resetting an already running timer.
	/// </summary>
	public bool InterceptionProcessed { get => _processedIntercept; set => _processedIntercept = value; }

	/// <summary>The UFO's shield level (clamped to 0..shieldCapacity).</summary>
	public int Shield { get => _shield; set => _shield = Math.Max(0, Math.Min(_stats.shieldCapacity, value)); }

	/// <summary>Which _interceptionNumber handles the UFO's shield recharge in a dogfight.</summary>
	public int ShieldRechargeHandle { get => _shieldRechargeHandle; set => _shieldRechargeHandle = value; }

	/// <summary>The percent shield remaining.</summary>
	public int ShieldPercentage => _stats.shieldCapacity != 0 ? _shield * 100 / _stats.shieldCapacity : 0;

	/// <summary>How much this UFO is being slowed down by craft tractor beams (clamped to 0..speedMax).</summary>
	public int TractorBeamSlowdown { get => _tractorBeamSlowdown; set => _tractorBeamSlowdown = Math.Max(0, Math.Min(_stats.speedMax, value)); }

	/// <summary>Is this UFO a hunter-killer?</summary>
	public bool IsHunterKiller => _isHunterKiller;

	/// <summary>Sets if the UFO is a hunter-killer.</summary>
	public void SetHunterKiller(bool isHunterKiller) => _isHunterKiller = isHunterKiller;

	/// <summary>Is this UFO an escort?</summary>
	public bool IsEscort => _isEscort;

	/// <summary>Sets if the UFO is an escort.</summary>
	public void SetEscort(bool isEscort) => _isEscort = isEscort;

	/// <summary>The UFO's hunting preferences.</summary>
	public int HuntMode => _huntMode;

	/// <summary>The UFO's hunting behavior.</summary>
	public int HuntBehavior => _huntBehavior;

	/// <summary>Is this UFO actively hunting right now?</summary>
	public bool IsHunting => _isHunting;

	/// <summary>Is this UFO escorting other UFO right now?</summary>
	public bool IsEscorting => _isEscorting;

	/// <summary>The value of the softlock shot counter.</summary>
	public int SoftlockShotCounter => _softlockShotCounter;

	/// <summary>Increases the value of the softlock shot counter.</summary>
	public void IncreaseSoftlockShotCounter() => _softlockShotCounter++;

	/// <summary>Returns if a certain target is inside the UFO's radar range, taking in account the positions of both.</summary>
	/// <param name="target">Pointer to target to compare.</param>
	/// <returns>True if inside radar range.</returns>
	public bool InsideRadarRange(Target target)
	{
		if (_stats.radarRange == 0)
			return false;

		double range = Nautical(_stats.radarRange);
		return GetDistance(target) <= range;
	}

	/// <summary>fmath.h Nautical(double).</summary>
	private static double Nautical(double x) => x * (1 / 60.0) * (Math.PI / 180.0);

	public enum UfoStatus : uint
	{
		FLYING = 0,
		LANDED = 1,
		CRASHED = 2,
		DESTROYED = 3,
		IGNORE_ME = 4,
	}
}
