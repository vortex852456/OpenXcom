namespace Acom.Tests.Savegame;

[Collection(nameof(Acom.Tests.Mod.GlobalRng))] // uses the global Rng state
public class AlienStrategyTests
{
	private const string Input =
		"regions:\n  STR_NORTH_AMERICA: 14\n  STR_ARCTIC: 0\n  STR_EUROPA: 17\n" +
		"missionLocations:\n  STR_TERROR:\n    - [STR_EUROPA, 2]\n    - [STR_ASIA, 0]\n  STR_BASE:\n    - [STR_ARCTIC, 1]\n" +
		"missionsRun:\n  STR_TERROR: 3\n  STR_ALIEN_BASE: 1\n";

	private static string Save(AlienStrategy strategy)
	{
		var writer = new YamlRootNodeWriter();
		strategy.Save(writer);
		return writer.Emit().ReplaceLineEndings("\n");
	}

	/// <summary>Expected values were printed by the original C++ code with the same calls.</summary>
	[Fact]
	public void Load_choose_and_save_match_cpp()
	{
		var strategy = new AlienStrategy();
		// without possibleMissions the mod is not consulted
		strategy.Load(new YamlRootNodeReader(Input, "as").ToBase(), null);
		Rng.Seed = 7;
		var regions = Enumerable.Range(0, 12).Select(_ => strategy.ChooseRandomRegion(null)).ToList();
		Assert.Equal("STR_NORTH_AMERICA STR_NORTH_AMERICA STR_NORTH_AMERICA STR_EUROPA STR_EUROPA STR_EUROPA STR_NORTH_AMERICA STR_EUROPA STR_NORTH_AMERICA STR_EUROPA STR_EUROPA STR_EUROPA".Split(' '), regions);

		strategy.AddMissionRun("STR_TERROR");
		strategy.AddMissionRun("", 5);
		strategy.AddMissionRun("STR_NEW", 2);
		Assert.Equal(4, strategy.GetMissionsRun("STR_TERROR"));
		Assert.Equal(2, strategy.GetMissionsRun("STR_NEW"));
		Assert.Equal(0, strategy.GetMissionsRun("STR_NONE"));
		Assert.False(strategy.ValidMissionLocation("STR_TERROR", "STR_EUROPA", 2));
		Assert.True(strategy.ValidMissionLocation("STR_TERROR", "STR_EUROPA", 1));
		Assert.True(strategy.ValidMissionLocation("STR_X", "STR_EUROPA", 2));
		// C++ quirk: going over the maximum erases the first map entry (STR_BASE), not the oldest location
		strategy.AddMissionLocation("STR_TERROR", "STR_AFRICA", 4, 2);
		strategy.AddMissionLocation("STR_IGNORED", "STR_AFRICA", 4, 0);

		Assert.Equal(
			"regions:\n  STR_EUROPA: 17\n  STR_NORTH_AMERICA: 14\n" +
			"missionLocations:\n  STR_TERROR:\n    - - STR_EUROPA\n      - 2\n    - - STR_ASIA\n      - 0\n    - - STR_AFRICA\n      - 4\n" +
			"missionsRun:\n  STR_ALIEN_BASE: 1\n  STR_NEW: 2\n  STR_TERROR: 4\n",
			Save(strategy));
	}

	[Fact]
	public void Yaml_roundtrip_is_stable()
	{
		var first = new AlienStrategy();
		first.Load(new YamlRootNodeReader(Input, "as").ToBase(), null);
		string saved = Save(first);
		var second = new AlienStrategy();
		second.Load(new YamlRootNodeReader(saved, "as").ToBase(), null);
		Assert.Equal(saved, Save(second));
		Assert.Equal(3, second.GetMissionsRun("STR_TERROR"));
		Assert.False(second.ValidMissionLocation("STR_BASE", "STR_ARCTIC", 1));

		var empty = new AlienStrategy();
		Assert.Equal("regions: ~\nmissionLocations: {}\nmissionsRun: {}\n", Save(empty));
		Assert.True(empty.RemoveMission("STR_EUROPA", "STR_ALIEN_TERROR"));
		Assert.False(empty.ValidMissionRegion("STR_EUROPA"));
	}
}
