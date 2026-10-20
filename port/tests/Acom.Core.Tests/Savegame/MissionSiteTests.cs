namespace Acom.Tests.Savegame;

public class MissionSiteTests
{
	/// <summary>Keys in the order the C++ code writes them (Target fields first).</summary>
	private const string Saved =
		"lon: 1\nlat: 0.25\nid: 7\ntype: STR_ALIEN_TERROR\ndeployment: STR_TERROR_MISSION\ntexture: 3\nsecondsRemaining: 7200\n" +
		"race: STR_FLOATER\ndetected: true\n";

	[Fact]
	public void Yaml_roundtrip_keeps_cpp_keys_and_order()
	{
		var site = new MissionSite(new RuleAlienMission("STR_ALIEN_TERROR"), new AlienDeployment("STR_TERROR_MISSION"), null);
		Assert.Equal(-1, site.Texture);
		Assert.Equal(-1, site.UfoUniqueId);
		Assert.Equal(-1, site.Marker); // not detected

		site.Load(new YamlRootNodeReader(Saved + "inBattlescape: false\nufoUniqueId: 5\n", "site").ToBase());
		Assert.Equal(3, site.Texture);
		Assert.Equal(7200, site.SecondsRemaining);
		Assert.Equal("STR_FLOATER", site.AlienRace);
		Assert.True(site.Detected);
		Assert.Equal(5, site.UfoUniqueId);

		// ufoUniqueId is only written for a linked UFO, the city is never saved (as in C++)
		site.City = "STR_BERLIN";
		var writer = new YamlRootNodeWriter();
		site.Save(writer);
		Assert.Equal(Saved, writer.Emit().ReplaceLineEndings("\n"));
	}

	[Fact]
	public void Custom_deployment_is_saved_after_deployment()
	{
		var site = new MissionSite(new RuleAlienMission("STR_ALIEN_TERROR"), new AlienDeployment("STR_TERROR_MISSION"), new AlienDeployment("STR_CUSTOM"));
		var writer = new YamlRootNodeWriter();
		site.Save(writer);
		Assert.Equal("lon: 0\nlat: 0\ntype: STR_ALIEN_TERROR\ndeployment: STR_TERROR_MISSION\nmissionCustomDeploy: STR_CUSTOM\ntexture: -1\nrace: ''\ndetected: false\n",
			writer.Emit().ReplaceLineEndings("\n"));
	}
}
