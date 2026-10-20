using static Acom.Tests.Savegame.SavegameTestHelpers;

namespace Acom.Tests.Savegame;

[Collection(nameof(Acom.Tests.Mod.GlobalRng))] // the constructor of hunter-killers uses the global Rng state
public class UfoTests
{
	/// <summary>Keys in the order the C++ code writes them (Target, MovingTarget, then Ufo fields).</summary>
	private const string Saved =
		"lon: 1.5\nlat: -0.25\nid: 4\n" +
		"dest:\n  lon: 2\n  lat: 0.5\n  type: STR_WAY_POINT\n  id: 0\n" +
		"speedLon: 0.5\nspeedLat: -0.125\nspeedRadian: 0.25\nspeed: 800\n" +
		"type: STR_TEST_UFO\nuniqueId: 12\nmissionWaveNumber: 1\nlandId: 3\ndamage: 40\nshield: 20\nshieldRechargeHandle: 2\n" +
		"altitude: STR_GROUND\ndirection: STR_EAST\nstatus: 1\ndetected: true\nhyperDetected: true\nsecondsRemaining: 600\n" +
		"isHunterKiller: true\nhuntMode: 1\nhuntBehavior: 0\nsoftlockShotCounter: 2\n" +
		"origWaypoint:\n  lon: 0.5\n  lat: 0.25\n" +
		"fireCountdown: 5\nescapeCountdown: 6\n";

	private static RuleUfo Rule()
	{
		var rule = new RuleUfo("STR_TEST_UFO");
		rule.Stats.damageMax = 100;
		rule.Stats.speedMax = 1000;
		rule.Stats.shieldCapacity = 50;
		return rule;
	}

	private static string Save(Ufo ufo, bool newBattle = true)
	{
		var writer = new YamlRootNodeWriter();
		ufo.Save(writer, newBattle);
		return writer.Emit().ReplaceLineEndings("\n");
	}

	/// <summary>Load + finishLoading as SavedGame does it (a new battle save has no months passed, so no mission is looked up).</summary>
	private static Ufo Load(string yaml)
	{
		var ufo = new Ufo(Rule(), 0);
		var reader = Yaml(yaml);
		ufo.Load(reader, null, new SavedGame());
		ufo.FinishLoading(reader, null);
		return ufo;
	}

	[Fact]
	public void Yaml_roundtrip_keeps_cpp_keys_and_order()
	{
		var ufo = Load(Saved);
		Assert.Equal(12, ufo.UniqueId);
		Assert.Equal(Ufo.UfoStatus.LANDED, ufo.Status);
		Assert.Equal(40, ufo.Damage);
		Assert.Equal(600, ufo.SecondsRemaining);
		Assert.True(ufo.IsHunterKiller);
		Assert.Equal(1, ufo.HuntMode);
		Assert.Equal(2, ufo.SoftlockShotCounter);
		Assert.IsType<Waypoint>(ufo.Destination);
		Assert.Equal(2.0, ufo.Destination.Longitude);
		Assert.Equal(Saved, Save(ufo));
		Assert.Equal(Saved, Save(Load(Save(ufo))));

		// crashId wins over landId; the flags are written only when set
		var crashed = Load("{lon: 1, lat: 0, crashId: 7, landId: 3, status: 2, inBattlescape: true, speed: 500, isEscort: true}");
		Assert.Equal(0, crashed.Speed); // a UFO in battle does not move
		Assert.Equal(
			"lon: 1\nlat: 0\ndest:\n  lon: 1\n  lat: 0\n  type: STR_WAY_POINT\n  id: 0\nspeedLon: 0\nspeedLat: 0\nspeedRadian: 0\nspeed: 0\n" +
			"type: STR_TEST_UFO\nuniqueId: 0\nmissionWaveNumber: -1\ncrashId: 7\ndamage: 0\nshield: -1\nshieldRechargeHandle: 0\n" +
			"altitude: STR_HIGH_UC\ndirection: STR_NONE_UC\nstatus: 2\ninBattlescape: true\nisEscort: true\nhuntMode: 0\nhuntBehavior: 0\n" +
			"fireCountdown: 0\nescapeCountdown: 0\n",
			Save(crashed));
		Assert.Equal("STR_CRASH_SITE_", crashed.MarkerName);
		Assert.Equal(7, crashed.MarkerId);
	}

	[Fact]
	public void Mission_info_is_saved_and_tracks_live_ufos()
	{
		var trajectory = new UfoTrajectory("P0");
		trajectory.Load(Yaml("{id: P0, groundTimer: 3000, waypoints: [[5, 4, 100], [0, 3, 74]]}"));
		var mission = new AlienMission(new RuleAlienMission("STR_ALIEN_RESEARCH")) { Id = 9, Race = "STR_SECTOID" };
		var ufo = new Ufo(Rule(), 3);
		ufo.SetMissionInfo(mission, trajectory);
		ufo.TrajectoryPoint = 1;
		Assert.Same(mission, ufo.Mission);
		Assert.Equal("STR_SECTOID", ufo.AlienRace);
		Assert.Equal("STR_ALIEN_RESEARCH", ufo.MissionType);
		Assert.EndsWith("mission: 9\ntrajectory: P0\ntrajectoryPoint: 1\nfireCountdown: 0\nescapeCountdown: 0\n", Save(ufo, newBattle: false));
		Assert.DoesNotContain("mission:", Save(ufo, newBattle: true));
		Assert.False(mission.IsOver); // one live UFO

		ufo.Destroy();
		Assert.True(mission.IsOver);
	}

	/// <summary>Expected values were printed by the original RNG code with the call order of the C++ constructor.</summary>
	[Theory]
	[InlineData(3UL, false, 0, 0, 100663299UL)]
	[InlineData(11UL, true, 1, 1, 33497996521990169UL)]
	[InlineData(2024UL, false, 0, 0, 67914171922UL)]
	[InlineData(77UL, true, 0, 1, 242375795940077817UL)]
	public void Hunter_killer_roll_matches_cpp(ulong seed, bool hunterKiller, int huntMode, int huntBehavior, ulong seedAfter)
	{
		Rng.Seed = seed;
		var ufo = new Ufo(Rule(), 1, 60, 2, 2);
		Assert.Equal(hunterKiller, ufo.IsHunterKiller);
		Assert.Equal(huntMode, ufo.HuntMode);
		Assert.Equal(huntBehavior, ufo.HuntBehavior);
		Assert.Equal(seedAfter, Rng.Seed);

		// fixed hunt settings do not use the RNG beyond the percentage roll
		Rng.Seed = seed;
		var fixedUfo = new Ufo(Rule(), 1, 100, 1, 0);
		Assert.True(fixedUfo.IsHunterKiller);
		Assert.Equal(1, fixedUfo.HuntMode);
		Assert.Equal(0, fixedUfo.HuntBehavior);
	}

	[Theory]
	[InlineData(1.2, 0.0, "STR_EAST")]
	[InlineData(0.8, 0.0, "STR_WEST")]
	[InlineData(1.0, -0.2, "STR_NORTH")]
	[InlineData(1.0, 0.2, "STR_SOUTH")]
	[InlineData(1.2, -0.2, "STR_NORTH_EAST")]
	[InlineData(0.8, 0.2, "STR_SOUTH_WEST")]
	[InlineData(0.8, -0.2, "STR_NORTH_WEST")]
	[InlineData(1.2, 0.2, "STR_SOUTH_EAST")]
	public void Direction_follows_the_speed_vector(double lon, double lat, string direction)
	{
		var ufo = new Ufo(Rule(), 1) { Longitude = 1.0, Latitude = 0.0 };
		ufo.Destination = new Waypoint { Longitude = lon, Latitude = lat };
		Assert.Equal("STR_NONE_UC", ufo.Direction); // no speed yet
		ufo.Speed = 500;
		Assert.Equal(direction, ufo.Direction);
	}

	[Fact]
	public void Damage_altitude_and_clamps()
	{
		var ufo = Load("{damage: 50, altitude: STR_LOW_UC}");
		Assert.Equal(50, ufo.DamagePercentage);
		Assert.False(ufo.IsCrashed); // more than half of damageMax is needed
		Assert.Equal(2, ufo.AltitudeInt);
		Assert.Equal(ufo.Rules.DefaultVisibility - 10, ufo.Visibility);
		ufo.Altitude = "STR_GROUND";
		Assert.Equal(Ufo.UfoStatus.LANDED, ufo.Status);
		Assert.Equal(-30, ufo.Visibility);
		Assert.Equal(-1, ufo.Marker); // not detected
		ufo.Detected = true;
		Assert.Equal(3, ufo.Marker);

		var crashed = Load("{damage: 51}");
		Assert.True(crashed.IsCrashed);
		Assert.False(crashed.IsDestroyed);
		crashed.Altitude = "STR_GROUND";
		Assert.Equal(Ufo.UfoStatus.CRASHED, crashed.Status);
		Assert.Equal("STR_CRASH_SITE_", crashed.MarkerName);
		Assert.True(Load("{damage: 100}").IsDestroyed);
		Assert.True(Load("{damage: 100}").IsCrashed);
		Assert.False(Load("{damage: 90, huntBehavior: 1}").IsCrashed); // kamikaze never crash lands

		ufo.Shield = 80;
		Assert.Equal(50, ufo.Shield);
		Assert.Equal(100, ufo.ShieldPercentage);
		ufo.Shield = -5;
		Assert.Equal(0, ufo.Shield);
		ufo.TractorBeamSlowdown = 5000;
		Assert.Equal(1000, ufo.TractorBeamSlowdown);
		Assert.Equal(-1, new Ufo(Rule(), 1) { Altitude = "STR_UNKNOWN" }.AltitudeInt);
	}

	[Fact]
	public void Escorting_and_original_destination()
	{
		var escorted = new Ufo(Rule(), 1) { Longitude = 2.0, Latitude = 0.1 };
		var ufo = new Ufo(Rule(), 2) { Longitude = 1.0, Latitude = 0.0 };
		ufo.Destination = new Waypoint { Longitude = 1.5, Latitude = 0.25 };
		ufo.SetEscortedUfo(escorted);
		Assert.True(ufo.IsEscorting);
		Assert.Same(escorted, ufo.Destination);
		Assert.Equal(1000, ufo.Speed);
		Assert.Contains(ufo, escorted.UfoFollowers);
		Assert.Contains("origWaypoint:\n  lon: 1.5\n  lat: 0.25\n", Save(ufo));

		// back to (a copy of) the original waypoint, flags reset
		ufo.ResetOriginalDestination(true);
		Assert.False(ufo.IsEscorting);
		var wp = Assert.IsType<Waypoint>(ufo.Destination);
		Assert.Equal(1.5, wp.Longitude);
		Assert.DoesNotContain(ufo, escorted.UfoFollowers);
		Assert.Throws<InvalidOperationException>(() => ufo.ResetOriginalDestination(true));
		ufo.ResetOriginalDestination(false); // nothing to do
		Assert.Null(ufo.TargetedXcomCraft);
	}
}
