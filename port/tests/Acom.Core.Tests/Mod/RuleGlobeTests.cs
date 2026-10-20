#pragma warning disable CA1716 // namespace mirrors src/Mod like the other test folders
namespace Acom.Tests.Mod;

/// <summary>RuleGlobe without textures (Texture.Load needs Mod.LoadSpriteOffset, still a stub).</summary>
public class RuleGlobeTests
{
	private static double Rad(double deg) => deg * Math.PI / 180.0;

	private static YamlNodeReader Yaml(string text) => new YamlRootNodeReader(text, "test").ToBase();

	[Fact]
	public void Polygons_and_polylines_are_replaced_only_when_given()
	{
		var globe = new RuleGlobe();
		globe.Load(Yaml("""
			polygons:
			  - [2, 0, 0, 90, 0, 90, 45]
			  - [5, 10, 10, 20, 10, 20, 20, 10, 20]
			polylines:
			  - [0, 0, 180, 45]
			textures:
			  - delete: 7
			"""), null);

		Assert.Equal(2, globe.Polygons.Count);
		Assert.Equal(2, globe.Polygons[0].Texture);
		Assert.Equal(3, globe.Polygons[0].Points);
		Assert.Equal(4, globe.Polygons[1].Points);
		Assert.Equal(Rad(45), globe.Polygons[0].GetLatitude(2));
		Assert.Single(globe.Polylines);
		Assert.Equal(Rad(180), globe.Polylines[0].GetLongitude(1));

		globe.Load(Yaml("polylines: [[1, 1, 2, 2, 3, 3]]"), null);
		Assert.Equal(2, globe.Polygons.Count);
		Assert.Single(globe.Polylines);
		Assert.Equal(3, globe.Polylines[0].Points);

		Assert.Null(globe.GetTexture(0));
		Assert.Empty(globe.TexturesRaw);
		Assert.Empty(globe.GetTerrains(""));
	}

	[Fact]
	public void Terrain_criteria_of_the_standard_globe()
	{
		var path = Path.Combine(Engine.LanguageTests.RepoRoot(), "bin", "standard", "xcom1", "globe.rul");
		var texture = YamlRootNodeReader.FromFile(path)["globe"]["textures"][0];
		Assert.Equal(0, texture["id"].ReadVal<int>());

		// - name: FOREST
		//   area: [0, 360, -90, 0]
		var terrain = new List<TerrainCriteria>();
		Assert.True(texture.TryRead("terrain", ref terrain));
		Assert.Equal("FOREST", terrain[0].name);
		Assert.Equal(1, terrain[0].weight);
		Assert.Equal(Rad(360), terrain[0].lonMax);
		Assert.Equal(Rad(-90), terrain[0].latMin);
		Assert.Equal(0.0, terrain[0].latMax);
	}

	[Fact]
	public void Terrain_criteria_defaults_without_area()
	{
		var terrain = new List<TerrainCriteria>();
		Assert.True(Yaml("t: [{name: X, weight: 3}]").TryRead("t", ref terrain));
		Assert.Equal("X", terrain[0].name);
		Assert.Equal(3, terrain[0].weight);
		Assert.Equal(360.0, terrain[0].lonMax); // degrees, as in C++
		Assert.Equal(90.0, terrain[0].latMax);
	}

	[Fact]
	public void Texture_defaults()
	{
		var texture = new Texture(4);
		Assert.Equal("", texture.RandomDeployment);
		Assert.Empty(texture.Deployments);
		Assert.Empty(texture.Terrain);
		Assert.Equal(0, texture.BaseGridSprite);
		Assert.False(texture.IsCosmeticOcean);
		Assert.False(texture.IsFakeUnderwater);
		Assert.Equal("", texture.StartingCondition);
	}
}
