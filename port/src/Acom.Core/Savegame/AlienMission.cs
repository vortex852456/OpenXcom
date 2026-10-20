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
// Ported from: src/Savegame/AlienMission.h, src/Savegame/AlienMission.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents an ongoing alien mission.
/// Contains variable info about the mission, like spawn counter, target region
/// and current wave.
/// </summary>
/// <seealso cref="RuleAlienMission"/>
public class AlienMission
{
	private readonly RuleAlienMission _rule;
	private string _region = "", _race = "";
	/// <summary>C++ size_t.</summary>
	private int _nextWave;
	/// <summary>C++ size_t.</summary>
	private int _nextUfoCounter;
	/// <summary>C++ size_t.</summary>
	private int _spawnCountdown;
	/// <summary>C++ size_t.</summary>
	private int _liveUfos;
	private bool _interrupted, _multiUfoRetaliationInProgress;
	private int _uniqueID, _missionSiteZoneArea;
	private AlienBase _base;

	/// <summary>Creates a mission of the specified type.</summary>
	public AlienMission(RuleAlienMission rule)
	{
		_rule = rule;
		_nextWave = 0;
		_nextUfoCounter = 0;
		_spawnCountdown = 0;
		_liveUfos = 0;
		_interrupted = false;
		_multiUfoRetaliationInProgress = false;
		_uniqueID = 0;
		_missionSiteZoneArea = -1;
		_base = null;
	}

	/// <summary>Loads the alien mission from a YAML file.</summary>
	/// <param name="reader">The YAML node containing the data.</param>
	/// <param name="game">The game data, required to locate the alien base.</param>
	/// <param name="mod">The game mod, used to fix invalid regions and races.</param>
	public void Load(YamlNodeReader reader, SavedGame game, Mod mod)
	{
		reader.TryRead("region", ref _region);
		reader.TryRead("race", ref _race);
		reader.TryRead("nextWave", ref _nextWave);
		reader.TryRead("nextUfoCounter", ref _nextUfoCounter);
		reader.TryRead("spawnCountdown", ref _spawnCountdown);
		reader.TryRead("liveUfos", ref _liveUfos);
		reader.TryRead("interrupted", ref _interrupted);
		reader.TryRead("multiUfoRetaliationInProgress", ref _multiUfoRetaliationInProgress);
		reader.TryRead("uniqueID", ref _uniqueID);
		var baseReader = reader["alienBase"];
		if (baseReader)
		{
			int id = baseReader.IsMap ? baseReader["id"].ReadVal<int>() : baseReader.ReadVal<int>();
			string type = baseReader.IsMap ? baseReader["type"].ReadVal<string>() : "STR_ALIEN_BASE";
			var found = game.AlienBases.Find(ab => ab.Id == id && ab.Deployment.MarkerName == type);
			if (found == null)
				throw new InvalidOperationException("Corrupted save: Invalid base for mission.");
			_base = found;
		}
		reader.TryRead("missionSiteZone", ref _missionSiteZoneArea);

		// fix invalid saves
		RuleRegion region = mod.GetRegion(_region, false);
		if (region == null)
		{
			Logger.Error($"Corrupted save: Mission with uniqueID: {_uniqueID} has an invalid region: {_region}");
			_interrupted = true;
			if (_missionSiteZoneArea > -1)
			{
				_missionSiteZoneArea = 0;
			}
			_region = mod.RegionsList[0];
			if (_liveUfos > 0)
			{
				Logger.Error("Mission still has some live UFOs, please leave them be until they disappear.");
			}
			Logger.Error($"Mission was interrupted! Temporarily assigned to a new region: {_region}");
		}
		AlienRace race = mod.GetAlienRace(_race, false);
		if (race == null)
		{
			Logger.Error($"Corrupted save: Mission with uniqueID: {_uniqueID} has an invalid alien race: {_race}");
			_interrupted = true;
			_race = mod.AlienRacesList[0];
			if (_liveUfos > 0)
			{
				Logger.Error("Mission still has some live UFOs, please leave them be until they disappear.");
			}
			Logger.Error($"Mission was interrupted! Temporarily assigned a new alien race: {_race}");
		}
	}

	/// <summary>Saves the alien mission to a YAML file.</summary>
	public void Save(YamlNodeWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.SetAsMap();
		writer.Write("type", _rule.Type);
		writer.Write("region", _region);
		writer.Write("race", _race);
		writer.Write("nextWave", _nextWave);
		writer.Write("nextUfoCounter", _nextUfoCounter);
		writer.Write("spawnCountdown", _spawnCountdown);
		writer.Write("liveUfos", _liveUfos);
		if (_interrupted)
			writer.Write("interrupted", _interrupted);
		if (_multiUfoRetaliationInProgress)
			writer.Write("multiUfoRetaliationInProgress", _multiUfoRetaliationInProgress);
		writer.Write("uniqueID", _uniqueID);
		if (_base != null)
			_base.SaveId(writer["alienBase"]);
		writer.Write("missionSiteZone", _missionSiteZoneArea);
	}

	/// <summary>The mission's ruleset.</summary>
	public RuleAlienMission Rules => _rule;

	/// <summary>The mission's region.</summary>
	public string Region => _region;

	/// <summary>
	/// Sets the mission's region. If the region is incompatible with
	/// actually carrying out an attack, use the "fallback" region as
	/// defined in the ruleset.
	/// (this is a slight difference from the original, which just
	/// defaulted them to zone[0], North America)
	/// </summary>
	/// <param name="region">The region we want to try to set the mission to.</param>
	/// <param name="rules">The mod, in case we need to swap out the region.</param>
	public void SetRegion(string region, Mod rules)
	{
		RuleRegion r = rules.GetRegion(region, true);
		if (!string.IsNullOrEmpty(r.MissionRegion))
		{
			_region = r.MissionRegion;
		}
		else
		{
			_region = region;
		}
	}

	/// <summary>The mission's race.</summary>
	public string Race { get => _race; set => _race = value; }

	/// <summary>
	/// The minutes until next wave spawns.
	/// The new time must be a multiple of 30 minutes, and more than 0.
	/// Setting this on a finished mission has no effect.
	/// </summary>
	public int WaveCountdown
	{
		get => _spawnCountdown;
		set
		{
			System.Diagnostics.Debug.Assert(value != 0 && value % 30 == 0);
			if (IsOver)
			{
				return;
			}
			_spawnCountdown = value;
		}
	}

	/// <summary>
	/// The unique ID assigned to this mission.
	/// It is an error to assign two IDs to the same mission.
	/// </summary>
	public int Id
	{
		get
		{
			System.Diagnostics.Debug.Assert(_uniqueID != 0, "Uninitialized mission!");
			return _uniqueID;
		}
		set
		{
			System.Diagnostics.Debug.Assert(_uniqueID == 0, "Reassigning ID!");
			_uniqueID = value;
		}
	}

	/// <summary>
	/// The alien base associated with this mission (possibly null).
	/// Only the alien supply missions care about this.
	/// </summary>
	public AlienBase AlienBase { get => _base; set => _base = value; }

	/// <summary>
	/// Check if a mission is over and can be safely removed from the game.
	/// A mission is over if it will spawn no more UFOs and it has no UFOs still in the game.
	/// </summary>
	public bool IsOver
	{
		get
		{
			if (_interrupted && _liveUfos == 0)
			{
				return true;
			}
			if (_rule.Objective == MissionObjective.OBJECTIVE_INFILTRATION && _rule.IsEndlessInfiltration)
			{
				//Infiltrations continue for ever.
				return false;
			}
			if (_nextWave == _rule.WaveCount && _liveUfos == 0)
			{
				return true;
			}
			return false;
		}
	}

	/// <summary>Handle UFO spawning for the mission.</summary>
	public void Think(Game engine, Globe globe)
	{
		// if interrupted, don't generate any more UFOs or anything else
		if (_interrupted || (_multiUfoRetaliationInProgress && !_rule.IsMultiUfoRetaliationExtra))
		{
			return;
		}

		Mod mod = engine.Mod;
		SavedGame game = engine.SavedGame;
		if (_nextWave >= _rule.WaveCount)
			return;
		if (_spawnCountdown > 30)
		{
			_spawnCountdown -= 30;
			return;
		}
		MissionWave wave = _rule.GetWave(_nextWave);
		UfoTrajectory trajectory = mod.GetUfoTrajectory(wave.trajectory, true);
		Ufo ufo = SpawnUfo(game, mod, globe, wave, trajectory);
		if (ufo != null)
		{
			//Some missions may not spawn a UFO!
			ufo.MissionWaveNumber = _nextWave;
			game.Ufos.Add(ufo);
		}
		else if ((mod.GetDeployment(wave.ufoType) != null && mod.GetUfo(wave.ufoType) == null && !string.IsNullOrEmpty(mod.GetDeployment(wave.ufoType).MarkerName)) // a mission site that we want to spawn directly
				|| (_rule.Objective == MissionObjective.OBJECTIVE_SITE && wave.objective)) // or we want to spawn one at random according to our terrain
		{
			RuleRegion regionRules = mod.GetRegion(_region, true);
			List<MissionArea> areas = regionRules.MissionZones[(_rule.SpawnZone == -1) ? trajectory.GetZone(0) : _rule.SpawnZone].areas;
			MissionArea area = new MissionArea(areas[(_missionSiteZoneArea == -1) ? Rng.Generate(0, areas.Count - 1) : _missionSiteZoneArea]);

			if (wave.objectiveOnXcomBase)
			{
				Base xcombase = SelectXcomBase(game, regionRules);
				// no xcom base = don't spawn a mission site
				if (xcombase != null)
				{
					area.lonMin = xcombase.Longitude;
					area.lonMax = xcombase.Longitude;
					area.latMin = xcombase.Latitude;
					area.latMax = xcombase.Latitude;
					// area.texture is taken from the ruleset
					area.name = ""; // remove the city name, if there was any
					SpawnMissionSite(game, mod, area, null, mod.GetDeployment(wave.ufoType, false));
				}
			}
			else
			{
				SpawnMissionSite(game, mod, area, null, mod.GetDeployment(wave.ufoType, false));
			}
		}

		++_nextUfoCounter;
		if (_nextUfoCounter >= wave.ufoCount)
		{
			_nextUfoCounter = 0;
			++_nextWave;
		}
		if (_rule.Objective == MissionObjective.OBJECTIVE_INFILTRATION && _nextWave == _rule.WaveCount)
		{
			var countriesCopy = new List<Country>(game.Countries);
			if (mod.InfiltrateRandomCountryInTheRegion)
			{
				Rng.Shuffle(countriesCopy);
			}
			foreach (var c in countriesCopy)
			{
				RuleRegion region = mod.GetRegion(_region, true);
				if (c.CanBeInfiltrated() && region.InsideRegion(c.Rules.LabelLongitude, c.Rules.LabelLatitude))
				{
					(double first, double second) pos = default;
					int tries = 0;
					bool dynamicBaseType = string.IsNullOrEmpty(_rule.SiteType);
					AlienDeployment alienBaseType = null;
					bool wantsToSpawnFakeUnderwater = false;
					bool found = false;
					if (!mod.BuildInfiltrationBaseCloseToTheCountry)
					{
						List<MissionArea> areas = region.MissionZones[_rule.SpawnZone].areas;
						while (!found)
						{
							MissionArea area = areas[Rng.Generate(0, areas.Count - 1)];
							pos.first = Rng.Generate(StdMin(area.lonMin, area.lonMax), StdMax(area.lonMin, area.lonMax));
							pos.second = Rng.Generate(StdMin(area.latMin, area.latMax), StdMax(area.latMin, area.latMax));

							if (tries == 0 || dynamicBaseType)
							{
								alienBaseType = ChooseAlienBaseType(mod, area);
								wantsToSpawnFakeUnderwater = Rng.Percent(alienBaseType.FakeUnderwaterSpawnChance);
							}
							++tries;

							if (tries == 100)
							{
								found = true; // forced spawn on invalid location
							}
							else if (globe.InsideLand(pos.first, pos.second) && region.InsideRegion(pos.first, pos.second))
							{
								bool isFakeUnderwater = globe.InsideFakeUnderwaterTexture(pos.first, pos.second);
								if (wantsToSpawnFakeUnderwater)
								{
									if (isFakeUnderwater) found = true; // found spawn point on fakeUnderwater texture
								}
								else
								{
									if (!isFakeUnderwater) found = true; // found spawn point on land
								}
							}
						}
					}
					else
					{
						dynamicBaseType = false; // just to make absolutely clear this feature is not supported in this case
						var dummyArea = new MissionArea();
						alienBaseType = ChooseAlienBaseType(mod, dummyArea);
						wantsToSpawnFakeUnderwater = Rng.Percent(alienBaseType.FakeUnderwaterSpawnChance);

						RuleCountry cRule = c.Rules;
						int pick = 0;
						double lonMini, lonMaxi, latMini, latMaxi;
						while (!found)
						{
							pick = Rng.Generate(0, cRule.LonMin.Count - 1);
							lonMini = cRule.LonMin[pick];
							lonMaxi = cRule.LonMax[pick];
							if (lonMini > lonMaxi)
							{
								// UK, France, Spain, or anything crossed by the prime meridian
								if (Rng.Percent(50)) { lonMini = Deg2Rad(0.0); } else { lonMaxi = Deg2Rad(359.99); }
							}
							latMini = cRule.LatMin[pick];
							latMaxi = cRule.LatMax[pick];
							pos.first = Rng.Generate(StdMin(lonMini, lonMaxi), StdMax(lonMini, lonMaxi));
							pos.second = Rng.Generate(StdMin(latMini, latMaxi), StdMax(latMini, latMaxi));
							++tries;

							if (tries == 100)
							{
								found = true; // forced spawn on invalid location
							}
							else if (globe.InsideLand(pos.first, pos.second) && cRule.InsideCountry(pos.first, pos.second))
							{
								bool isFakeUnderwater = globe.InsideFakeUnderwaterTexture(pos.first, pos.second);
								if (wantsToSpawnFakeUnderwater)
								{
									if (isFakeUnderwater) found = true; // found spawn point on fakeUnderwater texture
								}
								else
								{
									if (!isFakeUnderwater) found = true; // found spawn point on land
								}
							}
						}
					}
					if (tries < 100 || mod.AllowAlienBasesOnWrongTextures)
					{
						// only create a pact if the base is going to be spawned too
						c.SetNewPact();

						SpawnAlienBase(c, engine, pos, alienBaseType);

						// if the base can't be spawned for this country, try the next country
						break;
					}
				}
			}
			if (_rule.IsEndlessInfiltration)
			{
				// Infiltrations loop for ever.
				_nextWave = 0;
			}
		}
		if (_rule.Objective == MissionObjective.OBJECTIVE_BASE && _nextWave == _rule.WaveCount && !wave.objectiveOnTheLandingSite)
		{
			RuleRegion region = mod.GetRegion(_region, true);
			if (_rule.SpawnZone < 0 || _rule.SpawnZone >= region.MissionZones.Count)
			{
				throw new InvalidOperationException("Cannot spawn alien base, invalid spawnZone! Mission: " + _rule.Type);
			}
			List<MissionArea> areas = region.MissionZones[_rule.SpawnZone].areas;
			(double first, double second) pos = default;
			int tries = 0;
			bool dynamicBaseType = string.IsNullOrEmpty(_rule.SiteType);
			AlienDeployment alienBaseType = null;
			bool wantsToSpawnFakeUnderwater = false;
			bool found = false;
			while (!found)
			{
				MissionArea area = areas[Rng.Generate(0, areas.Count - 1)];
				pos.first = Rng.Generate(StdMin(area.lonMin, area.lonMax), StdMax(area.lonMin, area.lonMax));
				pos.second = Rng.Generate(StdMin(area.latMin, area.latMax), StdMax(area.latMin, area.latMax));

				if (tries == 0 || dynamicBaseType)
				{
					alienBaseType = ChooseAlienBaseType(mod, area);
					wantsToSpawnFakeUnderwater = Rng.Percent(alienBaseType.FakeUnderwaterSpawnChance);
				}
				++tries;

				if (tries == 100)
				{
					found = true; // forced spawn on invalid location
				}
				else if (globe.InsideLand(pos.first, pos.second) && region.InsideRegion(pos.first, pos.second, true))
				{
					bool isFakeUnderwater = globe.InsideFakeUnderwaterTexture(pos.first, pos.second);
					if (wantsToSpawnFakeUnderwater)
					{
						if (isFakeUnderwater) found = true; // found spawn point on fakeUnderwater texture
					}
					else
					{
						if (!isFakeUnderwater) found = true; // found spawn point on land
					}
				}
			}
			if (tries < 100 || mod.AllowAlienBasesOnWrongTextures)
			{
				SpawnAlienBase(null, engine, pos, alienBaseType);
			}
		}

		if (_nextWave != _rule.WaveCount)
		{
			int origSpawnTimerValue = _rule.GetWave(_nextWave).spawnTimer;
			int spawnTimer = origSpawnTimerValue / 30;
			_spawnCountdown = (spawnTimer / 2 + Rng.Generate(0, spawnTimer)) * 30;

			if (origSpawnTimerValue == 0)
			{
				// spawn more UFOs immediately
				Think(engine, globe);
			}
		}
	}

	/// <summary>Selects an xcom base in a given region.</summary>
	/// <param name="game">The saved game information.</param>
	/// <param name="regionRules">The rule for the given region.</param>
	/// <returns>Pointer to the selected xcom base or null.</returns>
	private Base SelectXcomBase(SavedGame game, RuleRegion regionRules)
	{
		var validxcombases = new List<Base>();
		foreach (var xb in game.Bases)
		{
			if (regionRules.InsideRegion(xb.Longitude, xb.Latitude))
			{
				if (_rule.Objective == MissionObjective.OBJECTIVE_RETALIATION)
				{
					// only discovered xcom bases!
					if (xb.RetaliationTarget)
					{
						validxcombases.Add(xb);
						if (_rule.IsMultiUfoRetaliation)
						{
							continue; // non-vanilla: let's consider all, for fun
						}
						break; // vanilla: the first is enough
					}
				}
				else // instant retaliation; or a mission wave with `objectiveOnXcomBase: true`
				{
					validxcombases.Add(xb);
				}
			}
		}
		Base xcombase = null;
		if (validxcombases.Count != 0)
		{
			if (validxcombases.Count == 1)
			{
				// take the first (don't mess with the RNG seed)
				xcombase = validxcombases[0];
			}
			else
			{
				int rngpick = Rng.Generate(0, validxcombases.Count - 1);
				xcombase = validxcombases[rngpick];
			}
		}
		return xcombase;
	}

	/// <summary>Adds a UFO line to the geoscape debug log (the C++ logUfo lambda).</summary>
	private static void LogUfo(Ufo u, SavedGame g, AlienMission a)
	{
		if (Options.oxceGeoscapeDebugLogMaxEntries > 0)
		{
			var ss = new System.Text.StringBuilder();
			ss.Append("gameTime: ").Append(g.Time.FullString);
			ss.Append(" ufoId: ").Append(Str(u.UniqueId));
			ss.Append(" ufoType: ").Append(u.Rules.Type);
			ss.Append(" race: ").Append(u.AlienRace);
			ss.Append(" region: ").Append(a.Region);
			ss.Append(" trajectory: ").Append(u.Trajectory.ID);
			if (u.IsHunterKiller)
			{
				ss.Append(" hk: true");
			}
			ss.Append(" missionId: ").Append(Str(a.Id));
			ss.Append(" missionType: ").Append(a.Rules.Type);
			g.GeoscapeDebugLog.Add(ss.ToString());
		}
	}

	/// <summary>
	/// This function will spawn a UFO according the mission rules.
	/// Some code is duplicated between cases, that's ok for now. It's on different
	/// code paths and the function is MUCH easier to read written this way.
	/// </summary>
	/// <param name="game">The saved game information.</param>
	/// <param name="mod">The mod.</param>
	/// <param name="globe">The globe, for land checks.</param>
	/// <param name="wave">The wave for the desired UFO.</param>
	/// <param name="trajectory">The rule for the desired trajectory.</param>
	/// <returns>Pointer to the spawned UFO. If the mission does not desire to spawn a UFO, null is returned.</returns>
	private Ufo SpawnUfo(SavedGame game, Mod mod, Globe globe, MissionWave wave, UfoTrajectory trajectory)
	{
		RuleUfo ufoRule = mod.GetUfo(wave.ufoType);
		int hunterKillerPercentage = wave.hunterKillerPercentage;
		if (hunterKillerPercentage == -1 && ufoRule != null)
		{
			hunterKillerPercentage = ufoRule.HunterKillerPercentage;
		}
		int huntMode = wave.huntMode;
		if (huntMode == -1 && ufoRule != null)
		{
			huntMode = ufoRule.HuntMode;
		}
		int huntBehavior = wave.huntBehavior;
		if (huntBehavior == -1 && ufoRule != null)
		{
			huntBehavior = ufoRule.HuntBehavior;
		}
		if (_rule.Objective == MissionObjective.OBJECTIVE_RETALIATION || _rule.Objective == MissionObjective.OBJECTIVE_INSTANT_RETALIATION)
		{
			RuleRegion regionRules = mod.GetRegion(_region, true);
			Base xcombase = null;

			// skip the scouting phase of a retaliation mission
			if (_rule.SkipScoutingPhase() && _rule.Objective == MissionObjective.OBJECTIVE_RETALIATION)
			{
				foreach (var xbase in game.Bases)
				{
					if (regionRules.InsideRegion(xbase.Longitude, xbase.Latitude))
					{
						xcombase = xbase;
						break;
					}
				}
			}
			else
			{
				xcombase = SelectXcomBase(game, regionRules);
			}

			if (xcombase != null)
			{
				// Spawn a battleship straight for the XCOM base.
				RuleUfo battleshipRule = string.IsNullOrEmpty(_rule.SpawnUfo) ? ufoRule : mod.GetUfo(_rule.SpawnUfo, true);
				UfoTrajectory assaultTrajectory = mod.GetUfoTrajectory(UfoTrajectory.RETALIATION_ASSAULT_RUN, true);
				var ufo = new Ufo(battleshipRule, game.GetId("STR_UFO_UNIQUE"));
				ufo.SetMissionInfo(this, assaultTrajectory);
				(double first, double second) pos;
				if (_rule.Objective == MissionObjective.OBJECTIVE_INSTANT_RETALIATION)
				{
					pos.first = xcombase.Longitude;
					pos.second = xcombase.Latitude;
				}
				else if (_rule.OperationType != AlienMissionOperationType.AMOT_SPACE && _base != null)
				{
					pos.first = _base.Longitude;
					pos.second = _base.Latitude;
				}
				else if (trajectory.GetAltitude(0) == "STR_GROUND")
				{
					pos = GetLandPoint(globe, regionRules, trajectory.GetZone(0), ufo);
				}
				else
				{
					pos = regionRules.GetRandomPoint(trajectory.GetZone(0));
				}
				ufo.Altitude = assaultTrajectory.GetAltitude(0);
				ufo.Speed = assaultTrajectory.ApplySpeedPercentage(0, ufo.CraftStats.speedMax);
				ufo.Longitude = pos.first;
				ufo.Latitude = pos.second;
				var wp = new Waypoint();
				wp.Longitude = xcombase.Longitude;
				wp.Latitude = xcombase.Latitude;
				ufo.Destination = wp;
				LogUfo(ufo, game, this);
				return ufo;
			}
			else if (_rule.Objective == MissionObjective.OBJECTIVE_INSTANT_RETALIATION)
			{
				// no xcom base found, nothing to do, terminate
				_interrupted = true;
				return null;
			}
		}
		else if (_rule.Objective == MissionObjective.OBJECTIVE_SUPPLY)
		{
			if (ufoRule == null || (wave.objective && _base == null))
			{
				// No base to supply!
				return null;
			}
			// Our destination is always an alien base.
			Ufo ufo;
			if (wave.objective)
			{
				ufo = new Ufo(ufoRule, game.GetId("STR_UFO_UNIQUE"));
			}
			else
			{
				ufo = new Ufo(ufoRule, game.GetId("STR_UFO_UNIQUE"), hunterKillerPercentage, huntMode, huntBehavior);
			}
			ufo.SetMissionInfo(this, trajectory);
			RuleRegion regionRules = mod.GetRegion(_region, true);
			(double first, double second) pos;
			if (_rule.OperationType != AlienMissionOperationType.AMOT_SPACE && _base != null)
			{
				pos.first = _base.Longitude;
				pos.second = _base.Latitude;
			}
			else if (trajectory.GetAltitude(0) == "STR_GROUND")
			{
				pos = GetLandPoint(globe, regionRules, trajectory.GetZone(0), ufo);
			}
			else
			{
				pos = regionRules.GetRandomPoint(trajectory.GetZone(0));
			}
			ufo.Altitude = trajectory.GetAltitude(0);
			ufo.Speed = trajectory.ApplySpeedPercentage(0, ufo.CraftStats.speedMax);
			ufo.Longitude = pos.first;
			ufo.Latitude = pos.second;
			var wp = new Waypoint();
			if (trajectory.GetAltitude(1) == "STR_GROUND")
			{
				if (wave.objective)
				{
					// Supply ships on supply missions land on bases, ignore trajectory zone.
					pos.first = _base.Longitude;
					pos.second = _base.Latitude;
				}
				else
				{
					// Other ships can land where they want.
					pos = GetLandPoint(globe, regionRules, trajectory.GetZone(1), ufo);
				}
			}
			else
			{
				pos = regionRules.GetRandomPoint(trajectory.GetZone(1));
			}
			wp.Longitude = pos.first;
			wp.Latitude = pos.second;
			ufo.Destination = wp;

			// Only hunter-killers can escort
			if (ufo.IsHunterKiller && wave.escort && !wave.objective)
			{
				ufo.SetEscort(true);
				// Find a UFO to escort
				foreach (var ufoToBeEscorted in game.Ufos)
				{
					// From the same mission
					if (ufoToBeEscorted.Mission.Id == ufo.Mission.Id)
					{
						// But not another hunter-killer, we escort only normal UFOs
						if (!ufoToBeEscorted.IsHunterKiller)
						{
							ufo.Longitude = ufoToBeEscorted.Longitude;
							ufo.Latitude = ufoToBeEscorted.Latitude;
							ufo.SetEscortedUfo(ufoToBeEscorted);
							break;
						}
					}
				}
			}
			LogUfo(ufo, game, this);
			return ufo;
		}
		if (ufoRule == null)
			return null;
		{
			// Spawn according to sequence.
			var ufo = new Ufo(ufoRule, game.GetId("STR_UFO_UNIQUE"), hunterKillerPercentage, huntMode, huntBehavior);
			ufo.SetMissionInfo(this, trajectory);
			RuleRegion regionRules = mod.GetRegion(_region, true);
			(double first, double second) pos = GetWaypoint(wave, trajectory, 0, globe, regionRules, ufo);
			ufo.Altitude = trajectory.GetAltitude(0);
			if (trajectory.GetAltitude(0) == "STR_GROUND")
			{
				ufo.SecondsRemaining = trajectory.GroundTimer() * 5;
			}
			ufo.Speed = trajectory.ApplySpeedPercentage(0, ufo.CraftStats.speedMax);
			ufo.Longitude = pos.first;
			ufo.Latitude = pos.second;
			if (_rule.OperationType != AlienMissionOperationType.AMOT_SPACE && _base != null)
			{
				// override starting location for Earth-based alien operations
				ufo.Longitude = _base.Longitude;
				ufo.Latitude = _base.Latitude;
			}
			var wp = new Waypoint();
			if (trajectory.WaypointCount > 1)
			{
				pos = GetWaypoint(wave, trajectory, 1, globe, regionRules, ufo);
			}
			else
			{
				throw new InvalidOperationException("Missing second waypoint! Error occurred while trying to determine UFO waypoint for mission type: " + _rule.Type
					+ " in region: " + regionRules.Type + "; ufo type: " + ufo.Rules.Type + ", trajectory ID: " + trajectory.ID + ".");
			}
			wp.Longitude = pos.first;
			wp.Latitude = pos.second;
			ufo.Destination = wp;

			// Only hunter-killers can escort
			if (ufo.IsHunterKiller && wave.escort)
			{
				ufo.SetEscort(true);
				// Find a UFO to escort
				foreach (var ufoToBeEscorted in game.Ufos)
				{
					// From the same mission
					if (ufoToBeEscorted.Mission.Id == ufo.Mission.Id)
					{
						// But not another hunter-killer, we escort only normal UFOs
						if (!ufoToBeEscorted.IsHunterKiller)
						{
							ufo.Longitude = ufoToBeEscorted.Longitude;
							ufo.Latitude = ufoToBeEscorted.Latitude;
							ufo.SetEscortedUfo(ufoToBeEscorted);
							break;
						}
					}
				}
			}
			LogUfo(ufo, game, this);
			return ufo;
		}
	}

	/// <summary>Initialize with values from rules.</summary>
	public void Start(Game engine, Globe globe, int initialCount = 0)
	{
		_nextWave = 0;
		_nextUfoCounter = 0;
		_liveUfos = 0;
		if (initialCount == 0)
		{
			int spawnTimer = _rule.GetWave(0).spawnTimer / 30;
			_spawnCountdown = (spawnTimer / 2 + Rng.Generate(0, spawnTimer)) * 30;
		}
		else
		{
			_spawnCountdown = initialCount;
		}

		// Earth-based alien operations
		if (_rule.OperationType != AlienMissionOperationType.AMOT_SPACE && _base == null)
		{
			Mod mod = engine.Mod;
			SavedGame game = engine.SavedGame;

			var possibilities = new List<AlienBase>();
			if (_rule.OperationType == AlienMissionOperationType.AMOT_REGION_NEW_BASE)
			{
				// new base, leave possibilities empty
			}
			else
			{
				if (_rule.OperationType == AlienMissionOperationType.AMOT_REGION_EXISTING_BASE || _rule.OperationType == AlienMissionOperationType.AMOT_REGION_NEW_BASE_IF_NECESSARY)
				{
					// region only
					RuleRegion missionRegion = mod.GetRegion(_region, true);
					foreach (var ab in game.AlienBases)
					{
						if (missionRegion.InsideRegion(ab.Longitude, ab.Latitude))
						{
							possibilities.Add(ab);
						}
					}
				}
				else
				{
					// whole Earth
					possibilities = new List<AlienBase>(game.AlienBases);
				}
			}
			if (possibilities.Count != 0)
			{
				// 1. existing base selected
				int selected = Rng.Generate(0, possibilities.Count - 1);
				_base = possibilities[selected];
			}
			else
			{
				if (_rule.OperationType == AlienMissionOperationType.AMOT_REGION_EXISTING_BASE || _rule.OperationType == AlienMissionOperationType.AMOT_EARTH_EXISTING_BASE)
				{
					// 2. no base available, mission aborted!
					_interrupted = true;
				}
				else
				{
					// 3. spawn a new base
					RuleRegion region = mod.GetRegion(_region, true);
					if (_rule.OperationSpawnZone < 0 || _rule.OperationSpawnZone >= region.MissionZones.Count)
					{
						throw new InvalidOperationException("Cannot spawn alien base, invalid operationSpawnZone! Mission: " + _rule.Type);
					}
					List<MissionArea> areas = region.MissionZones[_rule.OperationSpawnZone].areas;
					(double first, double second) pos = default;
					int tries = 0;
					AlienDeployment operationBaseType = mod.GetDeployment(_rule.OperationBaseType, true);
					bool wantsToSpawnFakeUnderwater = Rng.Percent(operationBaseType.FakeUnderwaterSpawnChance);
					bool found = false;
					while (!found)
					{
						MissionArea area = areas[Rng.Generate(0, areas.Count - 1)];
						pos.first = Rng.Generate(StdMin(area.lonMin, area.lonMax), StdMax(area.lonMin, area.lonMax));
						pos.second = Rng.Generate(StdMin(area.latMin, area.latMax), StdMax(area.latMin, area.latMax));
						++tries;

						if (tries == 100)
						{
							found = true; // forced spawn on invalid location
						}
						else if (globe.InsideLand(pos.first, pos.second) && region.InsideRegion(pos.first, pos.second, true))
						{
							bool isFakeUnderwater = globe.InsideFakeUnderwaterTexture(pos.first, pos.second);
							if (wantsToSpawnFakeUnderwater)
							{
								if (isFakeUnderwater) found = true; // found spawn point on fakeUnderwater texture
							}
							else
							{
								if (!isFakeUnderwater) found = true; // found spawn point on land
							}
						}
					}
					AlienBase newAlienOperationBase = null;
					if (tries < 100 || mod.AllowAlienBasesOnWrongTextures)
					{
						newAlienOperationBase = SpawnAlienBase(null, engine, pos, operationBaseType);
					}
					if (newAlienOperationBase != null)
					{
						_base = newAlienOperationBase;
					}
					else
					{
						_interrupted = true;
					}
				}
			}
		}
	}

	/// <summary>Increase number of live UFOs.</summary>
	public void IncreaseLiveUfos() => ++_liveUfos;

	/// <summary>Decrease number of live UFOs.</summary>
	public void DecreaseLiveUfos() => --_liveUfos;

	/// <summary>Sets the interrupted flag.</summary>
	public void SetInterrupted(bool interrupted) => _interrupted = interrupted;

	/// <summary>Sets the multiUfoRetaliationInProgress flag.</summary>
	public void SetMultiUfoRetaliationInProgress(bool multiUfoRetaliationInProgress) => _multiUfoRetaliationInProgress = multiUfoRetaliationInProgress;

	/// <summary>
	/// This function is called when one of the mission's UFOs arrives at it's current destination.
	/// It takes care of sending the UFO to the next waypoint, landing UFOs and
	/// marking them for removal as required. It must set the game data in a way that the rest of the code
	/// understands what to do.
	/// </summary>
	/// <param name="ufo">The UFO that reached it's waypoint.</param>
	/// <param name="engine">The game engine, required to get access to game data and game rules.</param>
	/// <param name="globe">The earth globe, required to get access to land checks.</param>
	public void UfoReachedWaypoint(Ufo ufo, Game engine, Globe globe)
	{
		if (_interrupted)
		{
			ufo.Detected = false;
			ufo.Status = Ufo.UfoStatus.DESTROYED;
			return;
		}

		Mod mod = engine.Mod;
		SavedGame game = engine.SavedGame;
		int curWaypoint = ufo.TrajectoryPoint;
		int nextWaypoint = curWaypoint + 1;
		UfoTrajectory trajectory = ufo.Trajectory;
		int waveNumber = ufo.MissionWaveNumber > -1 ? ufo.MissionWaveNumber : _nextWave - 1;
		if (waveNumber < 0)
		{
			waveNumber = _rule.WaveCount - 1;
		}

		MissionWave wave = _rule.GetWave(waveNumber);
		if (nextWaypoint >= trajectory.WaypointCount)
		{
			ufo.Detected = false;
			ufo.Status = Ufo.UfoStatus.DESTROYED;
			return;
		}
		ufo.Altitude = trajectory.GetAltitude(nextWaypoint);
		ufo.TrajectoryPoint = nextWaypoint;
		RuleRegion regionRules = mod.GetRegion(_region, true);

		{
			(double first, double second) pos = GetWaypoint(wave, trajectory, nextWaypoint, globe, regionRules, ufo);

			var wp = new Waypoint();
			wp.Longitude = pos.first;
			wp.Latitude = pos.second;
			ufo.Destination = wp;
		}

		if (ufo.Altitude != "STR_GROUND")
		{
			if (ufo.LandId != 0)
			{
				ufo.LandId = 0;
			}
			// Set next waypoint.
			ufo.Speed = trajectory.ApplySpeedPercentage(nextWaypoint, ufo.CraftStats.speedMax);
		}
		else
		{
			// UFO landed.
			if (_missionSiteZoneArea != -1 && wave.objective && trajectory.GetZone(curWaypoint) == _rule.SpawnZone)
			{
				// Remove UFO, replace with MissionSite.
				AddScore(ufo.Longitude, ufo.Latitude, game);

				MissionArea area = new MissionArea(regionRules.MissionZones[trajectory.GetZone(curWaypoint)].areas[_missionSiteZoneArea]);
				if (wave.objectiveOnTheLandingSite)
				{
					// Note: 'area' is a local copy; we're not changing the ruleset
					area.lonMin = ufo.Longitude;
					area.lonMax = ufo.Longitude;
					area.latMin = ufo.Latitude;
					area.latMax = ufo.Latitude;
				}
				MissionSite missionSite = SpawnMissionSite(game, mod, area, ufo);
				if (missionSite != null && _rule.RespawnUfoAfterSiteDespawn())
				{
					ufo.Status = Ufo.UfoStatus.IGNORE_ME;
					missionSite.Ufo = ufo;
				}
				else
				{
					ufo.Status = Ufo.UfoStatus.DESTROYED;
				}
				if (missionSite != null)
				{
					foreach (var follower in ufo.CraftFollowers)
					{
						if (follower.NumTotalUnits > 0)
						{
							follower.Destination = missionSite;
						}
						else if (ufo.Status == Ufo.UfoStatus.IGNORE_ME)
						{
							follower.ReturnToBase(); // no craft is allowed to follow an ignored UFO
						}
					}
				}
			}
			else if (trajectory.ID == UfoTrajectory.RETALIATION_ASSAULT_RUN)
			{
				// Ignore what the trajectory might say, this is a base assault.
				// Remove UFO, replace with Base defense.
				ufo.Detected = false;
				Base found = null;
				foreach (var xbase in game.Bases)
				{
					if (Target.AreSame(xbase.Longitude, ufo.Longitude) && Target.AreSame(xbase.Latitude, ufo.Latitude))
					{
						found = xbase;
						break;
					}
				}
				if (found == null)
				{
					ufo.Status = Ufo.UfoStatus.DESTROYED;
					// Only spawn mission if the base is still there.
					return;
				}
				ufo.Destination = found;
			}
			else
			{
				bool landingAllowed = true;
				if (!globe.InsideLand(ufo.Longitude, ufo.Latitude))
				{
					landingAllowed = false; // real water
				}
				else if (globe.InsideFakeUnderwaterTexture(ufo.Longitude, ufo.Latitude))
				{
					// decision to land on fake water was done earlier already
					// most of the time it's a proper decision, but sometimes it's a forced decision (i.e. no other option left)
					// because of forced decisions, let's check if the UFO can (at least theoretically) land on fake water...
					// ...and if not, don't land!
					if (ufo.Rules.FakeWaterLandingChance <= 0)
					{
						landingAllowed = false; // UFO was forced to go here, but it's not going to land!
					}
				}
				if (landingAllowed)
				{
					// Set timer for UFO on the ground.
					ufo.SecondsRemaining = trajectory.GroundTimer() * 5;
					if (ufo.Detected && ufo.LandId == 0)
					{
						ufo.LandId = engine.SavedGame.GetId("STR_LANDING_SITE");
					}

					// Many players wanted this over the years... you're welcome
					if (_rule.Objective == MissionObjective.OBJECTIVE_BASE && wave.objectiveOnTheLandingSite && trajectory.GetZone(curWaypoint) == _rule.SpawnZone)
					{
						(double first, double second) pos;
						pos.first = ufo.Longitude;
						pos.second = ufo.Latitude;

						var dummyArea = new MissionArea();
						AlienDeployment alienBaseType = ChooseAlienBaseType(mod, dummyArea);

						SpawnAlienBase(null, engine, pos, alienBaseType);
					}
				}
				else
				{
					// There's nothing to land on
					ufo.SecondsRemaining = 5;
				}
			}
		}
	}

	/// <summary>
	/// This function is called when one of the mission's UFOs is shot down (crashed or destroyed).
	/// Currently the only thing that happens is delaying the next UFO in the mission sequence.
	/// </summary>
	/// <param name="ufo">The UFO that was shot down.</param>
	public void UfoShotDown(Ufo ufo)
	{
		switch (ufo.Status)
		{
			case Ufo.UfoStatus.FLYING:
			case Ufo.UfoStatus.LANDED:
				System.Diagnostics.Debug.Fail("Ufo seems ok!");
				break;
			case Ufo.UfoStatus.CRASHED:
			case Ufo.UfoStatus.DESTROYED:
				if (_nextWave != _rule.WaveCount)
				{
					// Delay next wave
					_spawnCountdown += 30 * (Rng.Generate(0, 400) + 48);
				}
				break;
			case Ufo.UfoStatus.IGNORE_ME:
				break;
		}
	}

	/// <summary>
	/// This function is called when one of the mission's UFOs has finished it's time on the ground.
	/// It takes care of sending the UFO to the next waypoint and marking them for removal as required.
	/// It must set the game data in a way that the rest of the code understands what to do.
	/// </summary>
	/// <param name="ufo">The UFO that reached it's waypoint.</param>
	/// <param name="game">The saved game information.</param>
	public void UfoLifting(Ufo ufo, SavedGame game)
	{
		switch (ufo.Status)
		{
			case Ufo.UfoStatus.FLYING:
				System.Diagnostics.Debug.Fail("Ufo is already on the air!");
				break;
			case Ufo.UfoStatus.LANDED:
			case Ufo.UfoStatus.IGNORE_ME:
				{
					// base missions only get points when they are completed.
					if (_rule.Points > 0 && _rule.Objective != MissionObjective.OBJECTIVE_BASE)
					{
						AddScore(ufo.Longitude, ufo.Latitude, game);
					}
					ufo.Altitude = "STR_VERY_LOW";
					ufo.Speed = ufo.Trajectory.ApplySpeedPercentage(ufo.TrajectoryPoint, ufo.CraftStats.speedMax);
				}
				break;
			case Ufo.UfoStatus.CRASHED:
				// Mission expired
				ufo.Detected = false;
				ufo.Status = Ufo.UfoStatus.DESTROYED;
				break;
			case Ufo.UfoStatus.DESTROYED:
				System.Diagnostics.Debug.Fail("UFO can't fly!");
				break;
		}
	}

	/// <summary>Add alien points to the country and region at the coordinates given.</summary>
	/// <param name="lon">Longitudinal coordinates to check.</param>
	/// <param name="lat">Latitudinal coordinates to check.</param>
	/// <param name="game">The saved game information.</param>
	public void AddScore(double lon, double lat, SavedGame game)
	{
		if (_rule.Objective == MissionObjective.OBJECTIVE_INFILTRATION)
			return; // pact score is a special case
		foreach (var region in game.Regions)
		{
			if (region.Rules.InsideRegion(lon, lat))
			{
				region.AddActivityAlien(_rule.Points);
				break;
			}
		}
		foreach (var country in game.Countries)
		{
			if (country.Rules.InsideCountry(lon, lat))
			{
				country.AddActivityAlien(_rule.Points);
				break;
			}
		}
	}

	/// <summary>Spawn an alien base.</summary>
	/// <param name="pactCountry">A country that has signed a pact with the aliens and allowed them to build this base.</param>
	/// <param name="engine">The game engine, required to get access to game data and game rules.</param>
	/// <param name="pos">The base coordinates.</param>
	/// <param name="deployment">The base type.</param>
	/// <returns>Pointer to the spawned alien base.</returns>
	private AlienBase SpawnAlienBase(Country pactCountry, Game engine, (double first, double second) pos, AlienDeployment deployment)
	{
		SavedGame game = engine.SavedGame;
		var ab = new AlienBase(deployment, game.MonthsPassed);
		if (pactCountry != null)
		{
			ab.PactCountry = pactCountry.Rules.Type;
		}
		ab.AlienRace = _race;
		ab.Id = game.GetId(deployment.MarkerName);
		ab.Longitude = pos.first;
		ab.Latitude = pos.second;
		game.AlienBases.Add(ab);
		AddScore(ab.Longitude, ab.Latitude, game);

		if (Options.oxceGeoscapeDebugLogMaxEntries > 0)
		{
			var ss = new System.Text.StringBuilder();
			ss.Append("gameTime: ").Append(game.Time.FullString);
			ss.Append(" baseId: ").Append(Str(ab.Id));
			ss.Append(" baseType: ").Append(ab.Type);
			ss.Append(" race: ").Append(_race);
			ss.Append(" region: ").Append(_region);
			ss.Append(" deployment: ").Append(deployment.Type);
			ss.Append(" missionId: ").Append(Str(_uniqueID));
			ss.Append(" missionType: ").Append(_rule.Type);
			game.GeoscapeDebugLog.Add(ss.ToString());
		}

		return ab;
	}

	/// <summary>Chooses a mission type for a new alien base.</summary>
	private AlienDeployment ChooseAlienBaseType(Mod mod, MissionArea area)
	{
		AlienDeployment deployment = null;
		if (!string.IsNullOrEmpty(_rule.SiteType))
		{
			deployment = mod.GetDeployment(_rule.SiteType, true);
		}
		if (deployment == null)
		{
			Texture texture = mod.Globe.GetTexture(area.texture);
			if (texture != null && texture.Deployments.Count != 0)
			{
				// Note: only used in Area51 mod (as of April 2020)
				deployment = mod.GetDeployment(texture.RandomDeployment, true);
			}
		}
		if (deployment == null)
		{
			// Note: emergency fall-back
			deployment = mod.GetDeployment("STR_ALIEN_BASE_ASSAULT", true);
		}
		return deployment;
	}

	/// <summary>Select a destination based on the criteria of our trajectory and desired waypoint.</summary>
	/// <param name="wave">The wave of the UFO.</param>
	/// <param name="trajectory">The trajectory in question.</param>
	/// <param name="nextWaypoint">The next logical waypoint in sequence (0 for newly spawned UFOs).</param>
	/// <param name="globe">The earth globe, required to get access to land checks.</param>
	/// <param name="region">The ruleset for the region of our mission.</param>
	/// <param name="ufo">Required when making landing decisions on fake water.</param>
	/// <returns>A set of lon and lat coordinates based on the criteria of the trajectory.</returns>
	private (double first, double second) GetWaypoint(MissionWave wave, UfoTrajectory trajectory, int nextWaypoint, Globe globe, RuleRegion region, Ufo ufo)
	{
		if (trajectory.GetZone(nextWaypoint) >= region.MissionZones.Count)
		{
			LogMissionError(trajectory.GetZone(nextWaypoint), region);
		}

		if (_missionSiteZoneArea != -1 && wave.objective && trajectory.GetZone(nextWaypoint) == _rule.SpawnZone)
		{
			if (wave.objectiveOnTheLandingSite)
			{
				return GetLandPointForMissionSite(globe, region, _rule.SpawnZone, _missionSiteZoneArea, ufo);
			}
			MissionArea area = region.MissionZones[_rule.SpawnZone].areas[_missionSiteZoneArea];
			return (area.lonMin, area.latMin);
		}

		// if we are an Earth-based operation
		if (_rule.OperationType != AlienMissionOperationType.AMOT_SPACE && _base != null)
		{
			// and this is our last waypoint (C++ size_t: never true without waypoints)
			if (trajectory.WaypointCount > 0 && nextWaypoint >= trajectory.WaypointCount - 1)
			{
				// finish our business at the operation base
				return (_base.Longitude, _base.Latitude);
			}
		}
		// Note: if the operation base is destroyed in the meantime, remaining live UFOs will follow the original trajectory...
		// ...therefore it is important to have a valid last trajectory waypoint even for such missions

		if (trajectory.WaypointCount > nextWaypoint + 1 && trajectory.GetAltitude(nextWaypoint + 1) == "STR_GROUND")
		{
			return GetLandPoint(globe, region, trajectory.GetZone(nextWaypoint), ufo);
		}
		return region.GetRandomPoint(trajectory.GetZone(nextWaypoint));
	}

	/// <summary>
	/// Get a random point inside the given region zone.
	/// The point will be used to land a UFO, so it HAS to be on land (UNLESS it's landing on a city).
	/// </summary>
	/// <param name="globe">Reference to the globe data.</param>
	/// <param name="region">Reference to the region we want a land point in.</param>
	/// <param name="zone">The missionZone set within the region to find a landing zone in.</param>
	/// <param name="ufo">Required when making landing decisions on fake water.</param>
	/// <returns>A set of longitudinal and latitudinal coordinates.</returns>
	private (double first, double second) GetLandPoint(Globe globe, RuleRegion region, int zone, Ufo ufo)
	{
		if (zone >= region.MissionZones.Count || region.MissionZones[zone].areas.Count == 0)
		{
			LogMissionError(zone, region);
		}

		(double first, double second) pos = default;

		if (region.MissionZones[zone].areas[0].IsPoint) // if a UFO wants to land on a city, let it.
		{
			pos = region.GetRandomPoint(zone);
		}
		else
		{
			int tries = 0;
			bool wantsToLandOnFakeWater = Rng.Percent(ufo.Rules.FakeWaterLandingChance);
			bool found = false;
			while (!found)
			{
				pos = region.GetRandomPoint(zone);
				++tries;

				if (tries == 100)
				{
					found = true; // forced decision
				}
				else if (globe.InsideLand(pos.first, pos.second) && region.InsideRegion(pos.first, pos.second))
				{
					bool isFakeWater = globe.InsideFakeUnderwaterTexture(pos.first, pos.second);
					if (wantsToLandOnFakeWater)
					{
						if (isFakeWater)
						{
							found = true; // found landing point on fake water
						}
					}
					else
					{
						if (!isFakeWater)
						{
							found = true; // found landing point on land
						}
					}
				}
			}

			if (tries == 100)
			{
				Logger.Warning("Region: " + region.Type + " Longitude: " + Str(pos.first) + " Latitude: " + Str(pos.second) + " invalid zone: " + Str(zone) + " ufo forced to land on water (or invalid texture)!");
				if (wantsToLandOnFakeWater)
				{
					Logger.Warning("UFO: " + ufo.Rules.Type + " wanted to land on fake water.");
				}
			}
		}
		return pos;
	}

	/// <summary>
	/// Get a random point for the given region, zone and area.
	/// The point doesn't necessarily need to be inside the region boundaries.
	/// The point will be used to land a UFO, which immediately converts into a mission site, so it HAS to be on land (or fake water).
	/// </summary>
	private (double first, double second) GetLandPointForMissionSite(Globe globe, RuleRegion region, int zone, int area, Ufo ufo)
	{
		// Full error report in case of corrupted saves and/or modding-related issues
		// Note: checking only areas; zones were already checked earlier in getWaypoint()
		if (region.MissionZones[zone].areas.Count == 0)
		{
			throw new InvalidOperationException("Error occurred while trying to determine land+site point for mission type: " + _rule.Type + " in region: " + region.Type + ", in zone: " + Str(zone)
				+ ", zone has no valid areas.");
		}
		else if (area < 0 || area >= region.MissionZones[zone].areas.Count) // C++ (size_t)area
		{
			throw new InvalidOperationException("Error occurred while trying to determine land+site point for mission type: " + _rule.Type + " in region: " + region.Type + ", in zone: " + Str(zone)
				+ ", mission tried to find a land+site point in area " + Str(area)
				+ ", but this zone only has areas valid up to " + Str(region.MissionZones[zone].areas.Count - 1) + ".");
		}

		(double first, double second) pos = default;
		{
			int tries = 0;
			bool wantsToLandOnFakeWater = Rng.Percent(ufo.Rules.FakeWaterLandingChance);
			bool found = false;
			while (!found)
			{
				pos = region.GetRandomPoint(zone, area); // pass the area as a parameter too!
				++tries;

				if (tries == 100)
				{
					found = true; // forced decision
				}
				else if (globe.InsideLand(pos.first, pos.second) /* && region.insideRegion(pos.first, pos.second) */) // doesn't need to be inside the region!
				{
					bool isFakeWater = globe.InsideFakeUnderwaterTexture(pos.first, pos.second);
					if (wantsToLandOnFakeWater)
					{
						if (isFakeWater)
						{
							found = true; // found landing point on fake water
						}
					}
					else
					{
						if (!isFakeWater)
						{
							found = true; // found landing point on land
						}
					}
				}
			}

			if (tries == 100)
			{
				Logger.Warning("Region: " + region.Type + " Longitude: " + Str(pos.first) + " Latitude: " + Str(pos.second) + " zone: " + Str(zone) + " area: " + Str(area) + " ufo forced to land on water (or invalid texture)!");
				if (wantsToLandOnFakeWater)
				{
					Logger.Warning("UFO: " + ufo.Rules.Type + " wanted to land on fake water.");
				}
			}
		}
		return pos;
	}

	/// <summary>Attempt to spawn a Mission Site at a given location.</summary>
	/// <param name="game">Reference to the saved game.</param>
	/// <param name="mod">Reference to the game rules.</param>
	/// <param name="area">The point on the globe at which to spawn this site.</param>
	/// <param name="ufo">Ufo that spawn that mission.</param>
	/// <param name="missionOveride">Deployment to use instead of the mission's site type.</param>
	/// <returns>A pointer to the mission site.</returns>
	private MissionSite SpawnMissionSite(SavedGame game, Mod mod, MissionArea area, Ufo ufo = null, AlienDeployment missionOveride = null)
	{
		Texture texture = mod.Globe.GetTexture(area.texture);
		AlienDeployment deployment = null;
		AlienDeployment alienCustomDeploy = ufo != null ? mod.GetDeployment(ufo.CraftStats.missionCustomDeploy) : null;

		if (missionOveride != null)
		{
			deployment = missionOveride;
		}
		else if (mod.GetDeployment(_rule.SiteType) != null)
		{
			deployment = mod.GetDeployment(_rule.SiteType);
		}
		else
		{
			if (texture == null)
			{
				throw new InvalidOperationException("Error occurred while spawning mission site: " + _rule.Type);
			}
			deployment = mod.GetDeployment(texture.RandomDeployment, true);
		}

		if (deployment != null)
		{
			var missionSite = new MissionSite(_rule, deployment, alienCustomDeploy);
			missionSite.Longitude = Rng.Generate(area.lonMin, area.lonMax);
			missionSite.Latitude = Rng.Generate(area.latMin, area.latMax);
			missionSite.Id = game.GetId(deployment.MarkerName);
			missionSite.SecondsRemaining = Rng.Generate(deployment.DurationMin, deployment.DurationMax) * 3600;
			missionSite.AlienRace = _race;
			missionSite.Texture = area.texture;
			missionSite.City = area.name;
			game.MissionSites.Add(missionSite);

			if (Options.oxceGeoscapeDebugLogMaxEntries > 0)
			{
				var ss = new System.Text.StringBuilder();
				ss.Append("gameTime: ").Append(game.Time.FullString);
				ss.Append(" siteId: ").Append(Str(missionSite.Id));
				ss.Append(" siteType: ").Append(missionSite.Type);
				ss.Append(" race: ").Append(_race);
				ss.Append(" region: ").Append(_region);
				if (!string.IsNullOrEmpty(missionSite.City))
				{
					ss.Append(" city: ").Append(missionSite.City);
				}
				ss.Append(" deployment: ").Append(deployment.Type);
				if (alienCustomDeploy != null)
				{
					ss.Append(" / ").Append(alienCustomDeploy.Type);
				}
				ss.Append(" missionId: ").Append(Str(_uniqueID));
				ss.Append(" missionType: ").Append(_rule.Type);
				game.GeoscapeDebugLog.Add(ss.ToString());
			}

			return missionSite;
		}
		return null;
	}

	/// <summary>Tell the mission which entry in the 'areas' array we're targetting for our missionSite payload.</summary>
	/// <param name="area">The number of the area to target, synonymous with a city.</param>
	public void SetMissionSiteZoneArea(int area) => _missionSiteZoneArea = area;

	/// <summary>Provides some error information for bad mission definitions (always throws).</summary>
	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	private void LogMissionError(int zone, RuleRegion region)
	{
		if (region.MissionZones.Count != 0)
		{
			throw new InvalidOperationException("Error occurred while trying to determine waypoint for mission type: " + _rule.Type + " in region: " + region.Type + ", mission tried to find a waypoint in zone " + Str(zone) + " but this region only has zones valid up to " + Str(region.MissionZones.Count - 1) + ".");
		}
		else
		{
			throw new InvalidOperationException("Error occurred while trying to determine waypoint for mission type: " + _rule.Type + " in region: " + region.Type + ", region has no valid zones.");
		}
	}

	/// <summary>std::min: returns <paramref name="a"/> unless <paramref name="b"/> is smaller.</summary>
	private static double StdMin(double a, double b) => (b < a) ? b : a;

	/// <summary>std::max: returns <paramref name="a"/> unless it is smaller than <paramref name="b"/>.</summary>
	private static double StdMax(double a, double b) => (a < b) ? b : a;

	/// <summary>fmath.h Deg2Rad.</summary>
	private static double Deg2Rad(double deg) => deg * Math.PI / 180.0;

	/// <summary>Formats like std::ostream &lt;&lt; int.</summary>
	private static string Str(int value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

	/// <summary>Formats like std::ostream &lt;&lt; double.</summary>
	private static string Str(double value) => LocalizedText.FormatLikeOstream(value);
}
