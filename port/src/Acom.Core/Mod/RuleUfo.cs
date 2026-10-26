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
// Ported from: src/Mod/RuleUfo.h, src/Mod/RuleUfo.cpp
// Port status: partial (missing: script hooks)
#nullable disable

namespace Acom;

/// <summary>
/// Battle statistic of UFO type, plus custom deployments.
/// </summary>
/// <remarks>A value type in C++: the operators return new objects; use the copy constructor where C++ copies it.</remarks>
public class RuleUfoStats : RuleCraftStats
{
	public string craftCustomDeploy = "";
	public string missionCustomDeploy = "";

	/// <summary>Default constructor (all zero).</summary>
	public RuleUfoStats()
	{
	}

	/// <summary>Copy (C++ copy constructor).</summary>
	public RuleUfoStats(RuleUfoStats other) : base(other)
	{
		craftCustomDeploy = other.craftCustomDeploy;
		missionCustomDeploy = other.missionCustomDeploy;
	}

	/// <summary>Adds different stats in place (C++ <c>operator+=</c>); non-empty custom deployments replace the current ones.</summary>
	public void Add(RuleUfoStats r)
	{
		base.Add(r);
		if (!string.IsNullOrEmpty(r.craftCustomDeploy)) craftCustomDeploy = r.craftCustomDeploy;
		if (!string.IsNullOrEmpty(r.missionCustomDeploy)) missionCustomDeploy = r.missionCustomDeploy;
	}

	/// <summary>Sum of two stats as a new object.</summary>
	public static RuleUfoStats operator +(RuleUfoStats l, RuleUfoStats r)
	{
		var s = new RuleUfoStats(l);
		s.Add(r);
		return s;
	}

	/// <summary>Loads stats from YAML.</summary>
	public new void Load(YamlNodeReader reader)
	{
		base.Load(reader);
		reader.TryRead("craftCustomDeploy", ref craftCustomDeploy);
		reader.TryRead("missionCustomDeploy", ref missionCustomDeploy);
	}

	/// <summary>Helper function needed by scripts.</summary>
	public RuleCraftStats Base => this;
}

/// <summary>
/// Represents a specific type of UFO.
/// Contains constant info about a UFO like
/// speed, weapons, scores, etc.
/// </summary>
/// <seealso cref="Ufo"/>
public partial class RuleUfo
{
	/// <summary>Name of class used in script.</summary>
	public const string ScriptName = "RuleUfo";

	private readonly string _type;
	private string _size;
	private int _radius, _visibility, _blobSize;
	private int _sprite, _marker, _markerLand, _markerCrash;
	private int _power, _range, _score, _reload, _breakOffTime, _missionScore;
	private int _hunterKillerPercentage, _huntMode, _huntSpeed, _huntBehavior, _softlockThreshold;
	private int _missilePower;
	private int _missileStopChance;
	private bool _unmanned;
	private bool _instaHyper;
	private bool _noAlert;
	private int _splashdownSurvivalChance;
	private int _fakeWaterLandingChance;
	private int _fireSound;
	private int _alertSound;
	private int _huntAlertSound;
	private int _hitSound;
	private RuleTerrain _battlescapeTerrainData;
	private readonly RuleUfoStats _stats;
	private readonly SortedDictionary<string, RuleUfoStats> _statsRaceBonus;
	private string _modSprite = "";
	private string _hitImage = "";

	/// <summary>Creates a blank ruleset for a certain type of UFO.</summary>
	/// <param name="type">String defining the type.</param>
	public RuleUfo(string type)
	{
		_type = type;
		_size = "STR_VERY_SMALL";
		_radius = -1;
		_visibility = 0;
		_blobSize = -1;
		_sprite = -1;
		_marker = -1;
		_markerLand = -1;
		_markerCrash = -1;
		_power = 0;
		_range = 0;
		_score = 0;
		_reload = 0;
		_breakOffTime = 0;
		_missionScore = 1;
		_hunterKillerPercentage = 0;
		_huntMode = 0;
		_huntSpeed = 100;
		_huntBehavior = 2;
		_softlockThreshold = 100;
		_missilePower = 0;
		_missileStopChance = 0;
		_unmanned = false;
		_instaHyper = false;
		_noAlert = false;
		_splashdownSurvivalChance = 100;
		_fakeWaterLandingChance = 0;
		_fireSound = -1;
		_alertSound = -1;
		_huntAlertSound = -1;
		_hitSound = -1;
		_battlescapeTerrainData = null;
		_stats = new RuleUfoStats();
		_statsRaceBonus = new SortedDictionary<string, RuleUfoStats>(StringComparer.Ordinal);

		_stats.sightRange = 268;
		_stats.radarRange = 672; // same default as in RuleCraft (used by hunter-killers)
		_statsRaceBonus[""] = new RuleUfoStats();
	}

	/// <summary>Loads the UFO from YAML.</summary>
	/// <remarks>The C++ <c>parsers</c> argument (script containers) is left out with the script hooks.</remarks>
	public void Load(YamlNodeReader node, Mod mod)
	{
		var reader = node.UseIndex();
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent, mod);
		}

		reader.TryRead("size", ref _size);
		// sigh
		if (_size == "STR_MEDIUM")
		{
			_size = "STR_MEDIUM_UC";
		}
		reader.TryRead("radius", ref _radius);
		reader.TryRead("visibility", ref _visibility);
		reader.TryRead("blobSize", ref _blobSize);
		if (_blobSize > 7)
		{
			_blobSize = 7; // maximum possible
		}
		reader.TryRead("sprite", ref _sprite);
		if (reader["marker"])
		{
			_marker = mod.GetOffset(reader["marker"].ReadVal(_marker), 8);
		}
		if (reader["markerLand"])
		{
			_markerLand = mod.GetOffset(reader["markerLand"].ReadVal(_markerLand), 8);
		}
		if (reader["markerCrash"])
		{
			_markerCrash = mod.GetOffset(reader["markerCrash"].ReadVal(_markerCrash), 8);
		}
		reader.TryRead("power", ref _power);
		reader.TryRead("range", ref _range);
		reader.TryRead("score", ref _score);
		reader.TryRead("reload", ref _reload);
		reader.TryRead("breakOffTime", ref _breakOffTime);
		reader.TryRead("missionScore", ref _missionScore);
		reader.TryRead("hunterKillerPercentage", ref _hunterKillerPercentage);
		reader.TryRead("huntMode", ref _huntMode);
		reader.TryRead("huntSpeed", ref _huntSpeed);
		reader.TryRead("huntBehavior", ref _huntBehavior);
		reader.TryRead("softlockThreshold", ref _softlockThreshold);
		reader.TryRead("missilePower", ref _missilePower);
		reader.TryRead("missileStopChance", ref _missileStopChance);
		reader.TryRead("unmanned", ref _unmanned);
		reader.TryRead("instaHyper", ref _instaHyper);
		reader.TryRead("noAlert", ref _noAlert);
		reader.TryRead("splashdownSurvivalChance", ref _splashdownSurvivalChance);
		reader.TryRead("fakeWaterLandingChance", ref _fakeWaterLandingChance);

		_stats.Load(reader);

		var terrain = reader["battlescapeTerrainData"];
		if (terrain)
		{
			var rule = new RuleTerrain(terrain["name"].ReadVal<string>());
			rule.Load(terrain, mod);
			_battlescapeTerrainData = rule;
		}
		reader.TryRead("modSprite", ref _modSprite);
		reader.TryRead("hitImage", ref _hitImage);
		foreach (var raceBonus in reader["raceBonus"].Children())
		{
			string race = raceBonus.ReadKey<string>();
			if (!_statsRaceBonus.TryGetValue(race, out var stats))
			{
				stats = new RuleUfoStats();
				_statsRaceBonus[race] = stats;
			}
			stats.Load(raceBonus);
		}

		mod.LoadSoundOffset(_type, ref _fireSound, reader["fireSound"], "GEO.CAT");
		mod.LoadSoundOffset(_type, ref _alertSound, reader["alertSound"], "GEO.CAT");
		mod.LoadSoundOffset(_type, ref _huntAlertSound, reader["huntAlertSound"], "GEO.CAT");
		mod.LoadSoundOffset(_type, ref _hitSound, reader["hitSound"], "GEO.CAT");

		LoadScripts(reader, mod.ScriptParsers);
	}

	/// <summary>Gets the language string that names this UFO. Each UFO type has a unique name.</summary>
	public string Type => _type;

	/// <summary>Gets the size of this type of UFO.</summary>
	public string Size => _size;

	/// <summary>Gets the radius of this type of UFO on the dogfighting window.</summary>
	public int Radius
	{
		get
		{
			if (_radius > -1)
			{
				return _radius;
			}

			if (_size == "STR_VERY_SMALL")
			{
				return 2;
			}
			else if (_size == "STR_SMALL")
			{
				return 3;
			}
			else if (_size == "STR_MEDIUM_UC")
			{
				return 4;
			}
			else if (_size == "STR_LARGE")
			{
				return 5;
			}
			else if (_size == "STR_VERY_LARGE")
			{
				return 6;
			}
			return 0;
		}
	}

	/// <summary>Gets the UFO's default visibility.</summary>
	public int DefaultVisibility
	{
		get
		{
			if (_visibility != 0)
			{
				return _visibility;
			}

			// vanilla = 15*(3-ufosize);
			if (_size == "STR_VERY_SMALL")
				return -30;
			else if (_size == "STR_SMALL")
				return -15;
			else if (_size == "STR_MEDIUM_UC")
				return 0;
			else if (_size == "STR_LARGE")
				return 15;
			else if (_size == "STR_VERY_LARGE")
				return 30;

			return 0;
		}
	}

	/// <summary>Gets the size of the UFO's blob on the radar.</summary>
	public int BlobSize
	{
		get
		{
			if (_blobSize > -1 && _blobSize < 8)
			{
				return _blobSize;
			}

			if (_size == "STR_VERY_SMALL")
			{
				return 0;
			}
			else if (_size == "STR_SMALL")
			{
				return 1;
			}
			else if (_size == "STR_MEDIUM_UC")
			{
				return 2;
			}
			else if (_size == "STR_LARGE")
			{
				return 3;
			}
			else if (_size == "STR_VERY_LARGE")
			{
				return 4;
			}
			return 4;
		}
	}

	/// <summary>Gets the ID of the sprite used to draw the UFO in the Dogfight window.</summary>
	public int Sprite => _sprite;

	/// <summary>Gets the globe marker for the UFO while in flight (-1 if none).</summary>
	public int Marker => _marker;

	/// <summary>Gets the globe marker for the UFO while landed (-1 if none).</summary>
	public int LandMarker => _markerLand;

	/// <summary>Gets the globe marker for the UFO when crashed (-1 if none).</summary>
	public int CrashMarker => _markerCrash;

	/// <summary>Gets the maximum damage done by the UFO's weapons per shot.</summary>
	public int WeaponPower => _power;

	/// <summary>Gets the maximum range for the UFO's weapons.</summary>
	public int WeaponRange => _range;

	/// <summary>Gets the amount of points the player gets for shooting down the UFO.</summary>
	public int Score => _score;

	/// <summary>Gets the terrain data needed to draw the UFO in the battlescape.</summary>
	public RuleTerrain BattlescapeTerrainData => _battlescapeTerrainData;

	/// <summary>Gets the weapon reload for UFO ships.</summary>
	public int WeaponReload => _reload;

	/// <summary>Gets the UFO's break off time.</summary>
	public int BreakOffTime => _breakOffTime;

	/// <summary>Gets the UFO's fire sound.</summary>
	public int FireSound => _fireSound;

	/// <summary>Gets the alert sound for this UFO (UFO detected alert).</summary>
	public int AlertSound => _alertSound;

	/// <summary>Gets the alert sound for this UFO (UFO on intercept course alert).</summary>
	public int HuntAlertSound => _huntAlertSound;

	/// <summary>Gets the hit sound for this UFO (to be used in the BaseDestroyedState UI).</summary>
	public int HitSound => _hitSound;

	/// <summary>For user-defined UFOs, use a surface for the "preview" image.</summary>
	public string ModSprite => _modSprite;

	/// <summary>Gets the name of the surface to be displayed in the BaseDestroyedState UI.</summary>
	public string HitImage => _hitImage;

	/// <summary>Gets basic statistic of UFO.</summary>
	public RuleUfoStats Stats => _stats;

	/// <summary>Gets bonus statistic of UFO based on race (falls back to the "" entry).</summary>
	public RuleUfoStats GetRaceBonus(string s)
	{
		if (_statsRaceBonus.TryGetValue(s, out var stats))
			return stats;
		else
			return _statsRaceBonus[""];
	}

	/// <summary>Gets all bonus statistics of the UFO.</summary>
	public SortedDictionary<string, RuleUfoStats> RaceBonusRaw => _statsRaceBonus;

	/// <summary>Gets the amount of points awarded every 30 minutes while the UFO is on a mission.</summary>
	public int MissionScore => _missionScore;

	/// <summary>Gets the UFO's chance to become a hunter-killer.</summary>
	public int HunterKillerPercentage => _hunterKillerPercentage;

	/// <summary>Gets the UFO's hunting preferences.</summary>
	public int HuntMode => _huntMode;

	/// <summary>Gets the UFO's hunting speed (in percent of maximum speed).</summary>
	public int HuntSpeed => _huntSpeed;

	/// <summary>Gets the UFO's hunting behavior (normal, kamikaze or random).</summary>
	public int HuntBehavior => _huntBehavior;

	/// <summary>Gets the amount of shots fired before a softlock is detected.</summary>
	public int SoftlockThreshold => _softlockThreshold;

	/// <summary>Gets the missile power (of a UFO that represents one or more missiles).</summary>
	public int MissilePower => _missilePower;

	/// <summary>Gets the chance to stop retaliation mission after a successful missile strike (default = 0).</summary>
	public int MissileStopChance => _missileStopChance;

	/// <summary>Is this an unmanned UFO (drone, missile, etc.)?</summary>
	public bool IsUnmanned => _unmanned;

	/// <summary>Show extra info during base defense even if the UFO was not yet hyper-detected classically?</summary>
	public bool IsInstaHyper => _instaHyper;

	/// <summary>Suppress UFO detection alerts?</summary>
	public bool IsNoAlert => _noAlert;

	/// <summary>Gets the chance of surviving a splashdown on fake water.</summary>
	public int SplashdownSurvivalChance => _splashdownSurvivalChance;

	/// <summary>Gets the chance for deciding to land on fake water texture.</summary>
	public int FakeWaterLandingChance => _fakeWaterLandingChance;
}
