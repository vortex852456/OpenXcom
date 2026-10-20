using static Acom.Tests.Savegame.SavegameTestHelpers;

namespace Acom.Tests.Savegame;

[Collection(nameof(Acom.Tests.Mod.GlobalRng))] // uses the global Rng state
public class AlienMissionTests
{
	private static RuleAlienMission Rule(string objective = "0")
	{
		var rule = new RuleAlienMission("STR_ALIEN_RESEARCH");
		rule.Load(Yaml($$"""
			points: 10
			objective: {{objective}}
			waves:
			  - ufo: STR_SMALL_SCOUT
			    count: 1
			    trajectory: P0
			    timer: 9000
			  - ufo: STR_MEDIUM_SCOUT
			    count: 1
			    trajectory: P0
			    timer: 0
			"""));
		return rule;
	}

	private static string Save(AlienMission mission)
	{
		var writer = new YamlRootNodeWriter();
		mission.Save(writer);
		return writer.Emit().ReplaceLineEndings("\n");
	}

	[Fact]
	public void Save_keeps_cpp_keys_and_order()
	{
		var mission = new AlienMission(Rule()) { Id = 3, Race = "STR_SECTOID" };
		Assert.Equal(
			"type: STR_ALIEN_RESEARCH\nregion: ''\nrace: STR_SECTOID\nnextWave: 0\nnextUfoCounter: 0\nspawnCountdown: 0\nliveUfos: 0\n" +
			"uniqueID: 3\nmissionSiteZone: -1\n",
			Save(mission));

		var ab = new AlienBase(new AlienDeployment("STR_ALIEN_BASE_ASSAULT"), 0) { Id = 2, Longitude = 1.0, Latitude = 0.5 };
		mission.AlienBase = ab;
		mission.SetMissionSiteZoneArea(4);
		mission.SetInterrupted(true);
		mission.SetMultiUfoRetaliationInProgress(true);
		mission.IncreaseLiveUfos();
		Assert.Equal(
			"type: STR_ALIEN_RESEARCH\nregion: ''\nrace: STR_SECTOID\nnextWave: 0\nnextUfoCounter: 0\nspawnCountdown: 0\nliveUfos: 1\n" +
			"interrupted: true\nmultiUfoRetaliationInProgress: true\nuniqueID: 3\n" +
			$"alienBase:\n  lon: 1\n  lat: 0.5\n  type: {ab.Deployment.MarkerName}\n  id: 2\nmissionSiteZone: 4\n",
			Save(mission));
	}

	/// <summary>Expected values were printed by the original RNG code with the call order of AlienMission::start/ufoShotDown.</summary>
	[Theory]
	[InlineData(5UL, 6000, 13740, 20610, 17171073277665293UL)]
	[InlineData(99UL, 13380, 16530, 24480, 186651722954619033UL)]
	public void Start_and_shot_down_delay_match_cpp(ulong seed, int afterStart, int afterFirst, int afterSecond, ulong seedAfter)
	{
		var mission = new AlienMission(Rule()) { Id = 1 };
		var ufo = new Ufo(new RuleUfo("STR_SMALL_SCOUT"), 1) { Status = Ufo.UfoStatus.CRASHED };
		Rng.Seed = seed;
		mission.Start(null, null); // a space operation does not need the game
		Assert.Equal(afterStart, mission.WaveCountdown);
		mission.UfoShotDown(ufo);
		Assert.Equal(afterFirst, mission.WaveCountdown);
		ufo.Status = Ufo.UfoStatus.DESTROYED;
		mission.UfoShotDown(ufo);
		Assert.Equal(afterSecond, mission.WaveCountdown);
		ufo.Status = Ufo.UfoStatus.IGNORE_ME;
		mission.UfoShotDown(ufo);
		Assert.Equal(afterSecond, mission.WaveCountdown);
		Assert.Equal(seedAfter, Rng.Seed);

		// an initial count skips the RNG
		mission.Start(null, null, 60);
		Assert.Equal(60, mission.WaveCountdown);
		Assert.Equal(seedAfter, Rng.Seed);
	}

	[Fact]
	public void Over_when_all_waves_are_done_and_no_ufo_is_left()
	{
		var empty = new AlienMission(new RuleAlienMission("STR_EMPTY")) { Id = 1 };
		Assert.True(empty.IsOver);
		empty.WaveCountdown = 30; // no effect on a finished mission
		Assert.Equal(0, empty.WaveCountdown);
		empty.IncreaseLiveUfos();
		Assert.False(empty.IsOver);
		empty.DecreaseLiveUfos();
		Assert.True(empty.IsOver);

		var mission = new AlienMission(Rule()) { Id = 2 };
		Assert.False(mission.IsOver);
		mission.WaveCountdown = 90;
		Assert.Equal(90, mission.WaveCountdown);
		mission.IncreaseLiveUfos();
		mission.SetInterrupted(true);
		Assert.False(mission.IsOver); // a live UFO keeps it running
		mission.DecreaseLiveUfos();
		Assert.True(mission.IsOver);

		// endless infiltrations never end on their own
		var infiltration = new AlienMission(Rule("1")) { Id = 3 };
		Assert.False(infiltration.IsOver);
	}

	[Fact]
	public void Ufo_lifting_and_interrupted_waypoints()
	{
		var trajectory = new UfoTrajectory("P0");
		trajectory.Load(Yaml("{id: P0, groundTimer: 3000, waypoints: [[5, 4, 100], [0, 0, 74]]}"));
		var rule = new RuleUfo("STR_SMALL_SCOUT");
		rule.Stats.speedMax = 1000;
		rule.Stats.damageMax = 100; // with 0 even an undamaged UFO counts as destroyed (as in C++)
		var mission = new AlienMission(Rule()) { Id = 4 };
		var ufo = new Ufo(rule, 1);
		ufo.SetMissionInfo(mission, trajectory);
		ufo.TrajectoryPoint = 1;
		ufo.Altitude = "STR_GROUND";
		Assert.Equal(Ufo.UfoStatus.LANDED, ufo.Status);

		mission.UfoLifting(ufo, new SavedGame()); // no regions or countries to score in
		Assert.Equal(Ufo.UfoStatus.FLYING, ufo.Status);
		Assert.Equal("STR_VERY_LOW", ufo.Altitude);
		Assert.Equal(740, ufo.Speed);

		var crashed = new Ufo(rule, 2) { Status = Ufo.UfoStatus.CRASHED, Detected = true };
		mission.UfoLifting(crashed, new SavedGame());
		Assert.Equal(Ufo.UfoStatus.DESTROYED, crashed.Status);
		Assert.False(crashed.Detected);

		mission.SetInterrupted(true);
		ufo.Detected = true;
		mission.UfoReachedWaypoint(ufo, null, null); // an interrupted mission removes its UFOs right away
		Assert.Equal(Ufo.UfoStatus.DESTROYED, ufo.Status);
		Assert.False(ufo.Detected);
	}
}
