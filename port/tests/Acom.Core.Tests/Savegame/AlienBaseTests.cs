namespace Acom.Tests.Savegame;

public class AlienBaseTests
{
	/// <summary>Keys in the order the C++ code writes them (Target fields first).</summary>
	private const string Saved =
		"lon: 1.5\nlat: -0.5\nid: 3\npactCountry: STR_X\nrace: STR_SECTOID\ndiscovered: true\ndeployment: STR_ALIEN_BASE\n" +
		"startMonth: 2\nminutesSinceLastHuntMissionGeneration: 30\ngenMissionCount: 1\n";

	[Fact]
	public void Yaml_roundtrip_keeps_cpp_keys_and_order()
	{
		var ab = new AlienBase(new AlienDeployment("STR_ALIEN_BASE"), 0);
		ab.Load(new YamlRootNodeReader(Saved + "inBattlescape: false\n", "ab").ToBase());
		Assert.Equal("STR_X", ab.PactCountry);
		Assert.Equal("STR_SECTOID", ab.AlienRace);
		Assert.True(ab.IsDiscovered);
		Assert.False(ab.IsInBattlescape);
		Assert.Equal(2, ab.StartMonth);
		Assert.Equal(30, ab.MinutesSinceLastHuntMissionGeneration);
		Assert.Equal(1, ab.GenMissionCount);
		Assert.Equal(3, ab.Id);

		var writer = new YamlRootNodeWriter();
		ab.Save(writer);
		Assert.Equal(Saved, writer.Emit().ReplaceLineEndings("\n"));
	}
}
