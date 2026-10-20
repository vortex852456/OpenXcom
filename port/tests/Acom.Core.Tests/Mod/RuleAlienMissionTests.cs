using Acom.Tests.Engine;

#pragma warning disable CA1716 // namespace follows the source area (src/Mod), like Acom.Tests.<Area>
namespace Acom.Tests.Mod;
#pragma warning restore CA1716

public class RuleAlienMissionTests
{
	[Fact]
	public void Waves_and_settings_load()
	{
		var mission = new RuleAlienMission("STR_TEST");
		mission.Load(new YamlRootNodeReader("""
			points: 25
			objective: 4
			spawnUfo: STR_BATTLESHIP
			spawnZone: 3
			missionWeights: {0: 5, 4: 0, 2: 9}
			multiUfoRetaliationExtra: true
			operationType: 2
			operationSpawnZone: 1
			operationBaseType: STR_ALIEN_BASE_ASSAULT
			waves:
			  - ufo: STR_SMALL_SCOUT
			    count: 2
			    trajectory: P0
			    timer: 9000
			  - ufo: STR_BATTLESHIP
			    count: 1
			    trajectory: P5
			    timer: 1200
			    objective: true
			    hunterKillerPercentage: 50
			    escort: true
			    interruptPercentage: 10
			""", "test").ToBase());

		Assert.Equal("STR_TEST", mission.Type);
		Assert.Equal(25, mission.Points);
		Assert.Equal(MissionObjective.OBJECTIVE_RETALIATION, mission.Objective);
		Assert.Equal("STR_BATTLESHIP", mission.SpawnUfo);
		Assert.Equal(3, mission.SpawnZone);
		Assert.True(mission.IsMultiUfoRetaliation);
		Assert.True(mission.IsMultiUfoRetaliationExtra);
		Assert.True(mission.IsEndlessInfiltration);
		Assert.Equal(-1, mission.RetaliationOdds);
		Assert.Equal(AlienMissionOperationType.AMOT_REGION_NEW_BASE, mission.OperationType);
		Assert.Equal(1, mission.OperationSpawnZone);
		Assert.Equal("STR_ALIEN_BASE_ASSAULT", mission.OperationBaseType);
		Assert.False(mission.HasRaceWeights);
		Assert.False(mission.HasRegionWeights);
		Assert.Equal("", mission.GenerateRace(3));

		// weights by month, sorted by month: last entry not newer than the month wins
		Assert.Equal(5, mission.GetWeight(0));
		Assert.Equal(5, mission.GetWeight(1));
		Assert.Equal(9, mission.GetWeight(3));
		Assert.Equal(0, mission.GetWeight(10));
		Assert.Equal(1, new RuleAlienMission("EMPTY").GetWeight(7));

		Assert.Equal(2, mission.WaveCount);
		var scout = mission.GetWave(0);
		Assert.Equal("STR_SMALL_SCOUT", scout.ufoType);
		Assert.Equal(2, scout.ufoCount);
		Assert.Equal("P0", scout.trajectory);
		Assert.Equal(9000, scout.spawnTimer);
		Assert.False(scout.objective);
		Assert.Equal(-1, scout.hunterKillerPercentage);
		Assert.Equal(-1, scout.huntMode);
		Assert.Equal(-1, scout.huntBehavior);
		Assert.False(scout.escort);
		Assert.Equal(0, scout.interruptPercentage);
		var battleship = mission.GetWave(1);
		Assert.True(battleship.objective);
		Assert.Equal(50, battleship.hunterKillerPercentage);
		Assert.True(battleship.escort);
		Assert.Equal(10, battleship.interruptPercentage);
	}

	[Fact]
	public void Waves_of_the_repository_missions_load()
	{
		var path = Path.Combine(LanguageTests.RepoRoot(), "bin", "standard", "xcom1", "alienMissions.rul");
		var root = YamlRootNodeReader.FromFile(path);
		var waves = new Dictionary<string, List<MissionWave>>(StringComparer.Ordinal);
		foreach (var mission in root["alienMissions"].Children())
		{
			// the race weights need WeightedOptions, so only the waves are read here
			waves[mission["type"].ReadVal<string>()] = mission["waves"].ReadVal<List<MissionWave>>([]);
		}

		var research = waves["STR_ALIEN_RESEARCH"];
		Assert.Equal("STR_SMALL_SCOUT", research[0].ufoType);
		Assert.Equal(1, research[0].ufoCount);
		Assert.Equal("P0", research[0].trajectory);
		Assert.Equal(9000, research[0].spawnTimer);
		Assert.Equal("STR_LARGE_SCOUT", research[2].ufoType);
		Assert.Equal(2, research[2].ufoCount);
		Assert.DoesNotContain(research, w => w.objective);

		var terror = waves["STR_ALIEN_TERROR"];
		Assert.True(terror[^1].objective);
		Assert.Equal("STR_TERROR_SHIP", terror[^1].ufoType);
		Assert.Equal("P7", terror[^1].trajectory);
	}
}
