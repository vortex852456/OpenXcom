#pragma warning disable CA1716 // namespace mirrors src/Mod like the other test folders
namespace Acom.Tests.Mod;

/// <summary>
/// RuleRegion.Load and RuleCountry.Load need Mod.LoadBaseFunction (still a stub), so these tests cover
/// the mission zone parsing and the region logic that works without loaded data.
/// </summary>
public class RuleRegionTests
{
	private static double Rad(double deg) => deg * Math.PI / 180.0;

	[Fact]
	public void Mission_zones_of_the_standard_ruleset_are_read_in_radians()
	{
		var path = Path.Combine(Engine.LanguageTests.RepoRoot(), "bin", "standard", "xcom1", "regions.rul");
		var northAmerica = YamlRootNodeReader.FromFile(path)["regions"][0];
		Assert.Equal("STR_NORTH_AMERICA", northAmerica["type"].ReadVal<string>());

		var zones = new List<MissionZone>();
		Assert.True(northAmerica.TryRead("missionZones", ref zones));
		Assert.Equal(6, zones.Count);

		// first area: [200, 220, -65, -60]
		var area = zones[0].areas[0];
		Assert.Equal(10, zones[0].areas.Count);
		Assert.Equal(Rad(200), area.lonMin);
		Assert.Equal(Rad(220), area.lonMax);
		Assert.Equal(Rad(-65), area.latMin);
		Assert.Equal(Rad(-60), area.latMax);
		Assert.Equal(0, area.texture);
		Assert.Equal("", area.name);
		Assert.False(area.IsPoint);

		// cities: [286.125, 286.125, -40.75, -40.75, -1, STR_NEW_YORK]
		var city = zones[3].areas[0];
		Assert.True(city.IsPoint);
		Assert.Equal("STR_NEW_YORK", city.name);
		Assert.Equal(-1, city.texture);
		Assert.Equal(Rad(286.125), city.lonMin);
		Assert.Equal(Rad(-40.75), city.latMax);
		Assert.All(zones[3].areas, a => Assert.True(a.IsPoint));
	}

	[Fact]
	public void Mission_area_swaps_latitudes_and_keeps_longitudes()
	{
		var zones = new List<MissionZone>();
		Assert.True(new YamlRootNodeReader("z: [[[350, 8, 20, -30, 4]]]", "test").ToBase().TryRead("z", ref zones));
		var area = zones[0].areas[0];
		Assert.Equal(Rad(350), area.lonMin);
		Assert.Equal(Rad(8), area.lonMax);
		Assert.Equal(Rad(-30), area.latMin);
		Assert.Equal(Rad(20), area.latMax);
		Assert.Equal(4, area.texture);

		var copy = new MissionArea(area);
		Assert.True(copy.IsSameArea(area));
		copy.latMax = 0;
		Assert.False(copy.IsSameArea(area));
		Assert.Equal(Rad(20), area.latMax);
	}

	[Fact]
	public void Mission_area_defaults_are_in_degrees_like_cpp()
	{
		var area = new MissionArea();
		Assert.Equal(0.0, area.lonMin);
		Assert.Equal(360.0, area.lonMax);
		Assert.Equal(-90.0, area.latMin);
		Assert.Equal(90.0, area.latMax);
		Assert.False(area.IsPoint);
	}

	[Fact]
	public void Technical_region_without_areas()
	{
		var region = new RuleRegion("STR_TECHNICAL");
		Assert.True(region.InsideRegion(1.0, 0.5, ignoreTechnicalRegion: true));
		Assert.False(region.InsideRegion(1.0, 0.5));
		Assert.Equal(0, region.BaseCost);
		Assert.Equal(0, region.Weight);
		Assert.Empty(region.MissionZones);
		Assert.Equal("", region.MissionRegion);
	}

	[Fact]
	public void Country_without_areas()
	{
		var country = new RuleCountry("STR_NOWHERE");
		Assert.False(country.InsideCountry(1.0, 0.5));
		Assert.Equal(0, country.GenerateFunding());
		Assert.Equal(0, country.FundingCap);
		Assert.False(country.ProvidedBaseFunc.Any());
	}
}
