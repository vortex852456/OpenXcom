#pragma warning disable CA1716 // namespace mirrors src/Mod like the other test folders
namespace Acom.Tests.Mod;

public class PolygonTests
{
	private static double Rad(double deg) => deg * Math.PI / 180.0;

	private static YamlNodeReader Yaml(string text) => new YamlRootNodeReader(text, "test").ToBase();

	[Fact]
	public void Polygon_load_reads_texture_and_points_in_radians()
	{
		var polygon = new Polygon(3);
		polygon.Load(Yaml("[3, 10, 20, 30, 40, 50, 60.5]"));

		Assert.Equal(3, polygon.Texture);
		Assert.Equal(3, polygon.Points);
		Assert.Equal(Rad(10), polygon.GetLongitude(0));
		Assert.Equal(Rad(20), polygon.GetLatitude(0));
		Assert.Equal(Rad(50), polygon.GetLongitude(2));
		Assert.Equal(Rad(60.5), polygon.GetLatitude(2));
		Assert.Equal(0, polygon.GetX(2));
		Assert.Equal(0, polygon.GetY(2));
	}

	[Fact]
	public void Polygon_copy_is_deep()
	{
		var polygon = new Polygon(4);
		polygon.SetLongitude(1, 1.5);
		polygon.SetX(1, -7);
		polygon.Texture = 9;

		var copy = new Polygon(polygon);
		copy.SetLongitude(1, 2.5);
		copy.SetX(1, 7);

		Assert.Equal(4, copy.Points);
		Assert.Equal(9, copy.Texture);
		Assert.Equal(1.5, polygon.GetLongitude(1));
		Assert.Equal(-7, polygon.GetX(1));
		Assert.Equal(2.5, copy.GetLongitude(1));
	}

	[Fact]
	public void Polyline_load_reads_pairs_from_globe_ruleset()
	{
		var path = Path.Combine(Engine.LanguageTests.RepoRoot(), "bin", "standard", "xcom1", "globe.rul");
		var globe = YamlRootNodeReader.FromFile(path)["globe"];

		// - [351.25, -41.625, 353.875, -41.625, 352.375, -37]
		var polyline = new Polyline(3);
		polyline.Load(globe["polylines"][1]);

		Assert.Equal(3, polyline.Points);
		Assert.Equal(Rad(351.25), polyline.GetLongitude(0));
		Assert.Equal(Rad(-41.625), polyline.GetLatitude(0));
		Assert.Equal(Rad(352.375), polyline.GetLongitude(2));
		Assert.Equal(Rad(-37), polyline.GetLatitude(2));
	}
}
