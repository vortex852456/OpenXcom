using Acom.Tests.Engine;

#pragma warning disable CA1716 // namespace follows the source area (src/Mod), like Acom.Tests.<Area>
namespace Acom.Tests.Mod;
#pragma warning restore CA1716

/// <summary>
/// AlienDeployment.Load needs Mod helpers (civiliansByType, alertSound) that are ported separately,
/// so these tests read the deployment sub-structures of the repository rulesets directly.
/// </summary>
public class AlienDeploymentTests
{
	private static Dictionary<string, YamlNodeReader> RepoDeployments()
	{
		var path = Path.Combine(LanguageTests.RepoRoot(), "bin", "standard", "xcom1", "alienDeployments.rul");
		var root = YamlRootNodeReader.FromFile(path);
		var result = new Dictionary<string, YamlNodeReader>(StringComparer.Ordinal);
		foreach (var deployment in root["alienDeployments"].Children())
		{
			result[deployment["type"].ReadVal<string>()] = deployment;
		}
		return result;
	}

	[Fact]
	public void Deployment_data_of_the_repository_rulesets_loads()
	{
		var deployments = RepoDeployments();

		var scout = deployments["STR_SMALL_SCOUT"]["data"].ReadVal<List<DeploymentData>>([]);
		var leader = Assert.Single(scout);
		Assert.Equal(5, leader.alienRank);
		Assert.Equal(1, leader.lowQty);
		Assert.Equal(0, leader.medQty);
		Assert.Equal(1, leader.highQty);
		Assert.Equal(0, leader.dQty);
		Assert.Equal(0, leader.extraQty);
		Assert.Equal(50, leader.percentageOutsideUfo);
		Assert.Equal("", leader.customUnitType);
		Assert.Equal(3, leader.itemSets.Count);
		Assert.Equal(["STR_PLASMA_PISTOL", "STR_PLASMA_PISTOL_CLIP", "STR_PLASMA_PISTOL_CLIP", "STR_MIND_PROBE"], leader.itemSets[0].items);
		Assert.Equal(["STR_HEAVY_PLASMA", "STR_HEAVY_PLASMA_CLIP", "STR_MIND_PROBE"], leader.itemSets[2].items);
		Assert.Empty(leader.extraRandomItems);

		var medium = deployments["STR_MEDIUM_SCOUT"]["data"].ReadVal<List<DeploymentData>>([]);
		Assert.Equal([5, 4], medium.Select(d => d.alienRank));
		Assert.Equal(2, medium[0].dQty);

		// terror units carry no items: "-\n  []"
		var terror = deployments["STR_TERROR_MISSION"];
		var terrorUnits = terror["data"].ReadVal<List<DeploymentData>>([]);
		var reaper = terrorUnits.Single(d => d.alienRank == 6);
		Assert.Equal(3, reaper.itemSets.Count);
		Assert.All(reaper.itemSets, s => Assert.Empty(s.items));
	}

	[Fact]
	public void Briefing_data_keeps_defaults_of_missing_keys()
	{
		var deployments = RepoDeployments();

		var terror = deployments["STR_TERROR_MISSION"]["briefing"].ReadVal(new BriefingData());
		Assert.Equal(2, terror.palette);
		Assert.Equal("GMENBASE", terror.music);
		Assert.Equal("BACK16.SCR", terror.background);
		Assert.Equal(0, terror.textOffset);
		Assert.True(terror.showCraft);
		Assert.True(terror.showTarget);

		var alienBase = deployments["STR_ALIEN_BASE_ASSAULT"]["briefing"].ReadVal(new BriefingData());
		Assert.Equal(-16, alienBase.textOffset);
		Assert.Equal("BACK01.SCR", alienBase.background);
		Assert.Equal("GMDEFEND", alienBase.music);
		Assert.False(alienBase.showTarget);
		Assert.True(alienBase.showCraft);

		var copy = alienBase.Clone();
		copy.music = "OTHER";
		Assert.Equal("GMDEFEND", alienBase.music);
	}

	[Fact]
	public void Reinforcements_load_with_defaults()
	{
		var reader = new YamlRootNodeReader("""
			reinforcements:
			  - type: wave1
			    turns: [2, 4]
			    mapBlockFilterType: 1
			    spawnBlocks: [UFO1]
			    briefing: {title: STR_REINFORCEMENTS}
			    data:
			      - {alienRank: 3, lowQty: 2, extraQty: 1, itemSets: [[STR_PLASMA_RIFLE]]}
			  - type: wave2
			    minDifficulty: 2
			    useSpawnNodes: false
			""", "test").ToBase();
		var waves = reader["reinforcements"].ReadVal<List<ReinforcementsData>>([]);
		Assert.Equal(2, waves.Count);

		var first = waves[0];
		Assert.Equal("wave1", first.type);
		Assert.Equal([2, 4], first.turns);
		Assert.Equal(MapBlockFilterType.MFT_BY_MAPSCRIPT, first.mapBlockFilterType);
		Assert.Equal(["UFO1"], first.spawnBlocks);
		Assert.Equal("STR_REINFORCEMENTS", first.briefing.title);
		Assert.Equal("GMDEFEND", first.briefing.music);
		var unit = Assert.Single(first.data);
		Assert.Equal(3, unit.alienRank);
		Assert.Equal(1, unit.extraQty);
		Assert.Equal(["STR_PLASMA_RIFLE"], Assert.Single(unit.itemSets).items);
		Assert.Equal(0, first.minDifficulty);
		Assert.Equal(4, first.maxDifficulty);
		Assert.Equal(-1, first.maxTurn);
		Assert.Equal(100, first.executionOdds);
		Assert.Equal(-1, first.maxRuns);
		Assert.True(first.useSpawnNodes);
		Assert.True(first.randomizeZLevels);
		Assert.True(first.forceSpawnNearFriend);

		var second = waves[1];
		Assert.Equal(2, second.minDifficulty);
		Assert.False(second.useSpawnNodes);
		Assert.Equal(MapBlockFilterType.MFT_BY_BOTH_UNION, second.mapBlockFilterType);
		Assert.Empty(second.data);
	}
}
