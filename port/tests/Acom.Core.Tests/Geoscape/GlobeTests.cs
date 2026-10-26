using System.Reflection;
using GameMod = Acom.Mod;

namespace Acom.Tests.Geoscape;

/// <summary>Globe projection and hit testing on a globe without original data (empty surface sets, YAML polygons).</summary>
// Game sets a static game pointer for State, so these tests must not run in parallel with the other Game tests.
[Collection(nameof(Acom.Tests.Engine.GameTests))]
public class GlobeTests
{
	private const int W = 256, H = 200;

	private static double Rad(double deg) => deg * Math.PI / 180.0;

	private static YamlNodeReader Yaml(string text) => new YamlRootNodeReader(text, "test").ToBase();

	private static (Globe globe, Game game) Create(string polygons = "[]")
	{
		var game = new Game(new Screen(4, 4));
		var mod = new GameMod(createConverter: false);
		var texture = new SurfaceSet(32, 32);
		for (int i = 0; i < 39; ++i)
			texture.AddFrame(i);
		mod._sets["TEXTURE.DAT"] = texture;
		mod._sets["GlobeMarkers"] = new SurfaceSet(3, 3);
		mod.Globe.Load(Yaml("polygons: " + polygons), null);
		typeof(Game).GetField("_mod", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(game, mod);
		game.SavedGame = new SavedGame();
		return (new Globe(game, W / 2, H / 2, W, H), game);
	}

	[Fact]
	public void Cart_to_polar_and_back_hits_the_same_pixel()
	{
		var (globe, _) = Create();
		globe.Center(Rad(30), Rad(-20));
		Assert.Equal(0, globe.Zoom);
		for (int y = 20; y < 180; y += 7)
		{
			for (int x = 50; x < 206; x += 9)
			{
				double lon = 0, lat = 0;
				globe.CartToPolar((short)x, (short)y, ref lon, ref lat);
				if (double.IsNaN(lat))
					continue; // outside the globe (radius 0.45 * 200 = 90)
				Assert.InRange(lon, 0.0, 2 * Math.PI);
				double cx = 0, cy = 0;
				globe.PolarToCart(lon, lat, ref cx, ref cy);
				Assert.Equal(x, cx, 1e-6);
				Assert.Equal(y, cy, 1e-6);
			}
		}
	}

	[Fact]
	public void Polar_to_cart_projects_center_and_floors()
	{
		var (globe, game) = Create();
		globe.Center(Rad(10), Rad(5));
		Assert.Equal(Rad(10), game.SavedGame!.GlobeLongitude, 12);

		short x = 0, y = 0;
		globe.PolarToCart(Rad(10), Rad(5), ref x, ref y);
		Assert.Equal((W / 2, H / 2), (x, y));

		// 0.45 * 200 = 90 px radius: a point 90 degrees east lands on the right rim
		globe.Center(0, 0);
		globe.PolarToCart(Rad(90), 0, ref x, ref y);
		Assert.Equal((W / 2 + 90, H / 2), (x, y));
		// floor, not truncation: slightly west of the center is one pixel left
		globe.PolarToCart(-1e-6, 0, ref x, ref y);
		Assert.Equal(W / 2 - 1, x);

		double lon = 0, lat = 0;
		globe.CartToPolar(W / 2, H / 2, ref lon, ref lat);
		Assert.Equal((0.0, 0.0), (lon, lat));
		globe.CartToPolar(W / 2 + 91, H / 2, ref lon, ref lat);
		Assert.True(double.IsNaN(lon) && double.IsNaN(lat));

		globe.ZoomMax();
		Assert.Equal(5, globe.Zoom);
		Assert.Equal(5, game.SavedGame!.GlobeZoom);
		globe.PolarToCart(Rad(10), 0, ref x, ref y);
		Assert.Equal(W / 2 + (int)Math.Floor(3.60 * H * Math.Sin(Rad(10))), x);
	}

	[Fact]
	public void Inside_land_uses_the_polygons_of_the_front_side()
	{
		// texture 3, a square of 20 degrees around (0, 0) and a triangle further east
		var (globe, _) = Create("[[3, -10, -10, 10, -10, 10, 10, -10, 10], [4, 40, 0, 50, 0, 45, 10]]");
		Assert.True(globe.InsideLand(0, 0));
		Assert.True(globe.InsideLand(Rad(9), Rad(-9)));
		Assert.True(globe.InsideLand(Rad(359), Rad(5)));
		Assert.False(globe.InsideLand(Rad(11), 0));
		Assert.False(globe.InsideLand(Rad(180), 0));
		Assert.True(globe.InsideLand(Rad(45), Rad(2)));
		Assert.False(globe.InsideLand(Rad(45), Rad(-2)));
		Assert.False(globe.InsideFakeUnderwaterTexture(0, 0)); // no texture rules

		int texture = 0, shade = 0;
		globe.GetPolygonTextureAndShade(Rad(45), Rad(2), ref texture, ref shade);
		Assert.Equal(4, texture);
		Assert.InRange(shade, 0, 15);
		globe.GetPolygonTextureAndShade(Rad(90), 0, ref texture, ref shade);
		Assert.Equal(-1, texture);
	}

	[Fact]
	public void Get_targets_finds_targets_within_five_pixels_in_cpp_order()
	{
		var (globe, game) = Create();
		globe.Center(0, 0);
		var save = game.SavedGame!;
		double px = Math.Asin(1 / 90.0); // one pixel east of the center at zoom 0

		double edgeLat = Math.Asin(-3.5 / 90.0); // floor(-3.5) -> dy 4
		var near = new Waypoint { Longitude = 3.5 * px, Latitude = 0 };
		var edge = new Waypoint { Longitude = Math.Asin(3.5 / (90.0 * Math.Cos(edgeLat))), Latitude = edgeLat }; // dx 3 / dy 4 -> 25 <= 25
		var far = new Waypoint { Longitude = 5.5 * px, Latitude = Math.Asin(-0.5 / 90.0) }; // dx 5 / dy 1 -> 26 > 25
		var back = new Waypoint { Longitude = Math.PI, Latitude = 0 };
		save.Waypoints.AddRange([near, edge, far, back]);

		var unplaced = new Base(game.Mod); // lon 0 / lat 0 is skipped
		var xbase = new Base(game.Mod) { Longitude = 1.5 * px, Latitude = 0 };
		save.Bases.AddRange([unplaced, xbase]);

		var rule = new RuleUfo("STR_TEST_UFO");
		var hidden = new Ufo(rule, 0) { Longitude = 0.5 * px, Latitude = 0 };
		var seen = new Ufo(rule, 0) { Longitude = 0.5 * px, Latitude = 0, Detected = true };
		save.Ufos.AddRange([hidden, seen]);

		var targets = globe.GetTargets(W / 2, H / 2, false, null);
		Assert.Equal(new Target[] { xbase, seen, near, edge }, targets);
		Assert.Empty(globe.GetTargets(W / 2 + 40, H / 2 + 40, false, null));
	}

	[Fact]
	public void Shadow_is_zero_under_the_sun_and_full_on_the_night_side()
	{
		var sun = new Cord(0.0, 0.0, 1.0);
		Assert.Equal(0, Globe.GetShadowValue(sun, sun, 0));
		Assert.Equal(31, Globe.GetShadowValue(-sun, sun, 0));
		Assert.Equal(-1, Globe.GlobeStaticData.ShadeCurve(0));
		Assert.Equal(32, Globe.GlobeStaticData.ShadeCurve(255));
		var gradient = Globe.StaticData.shade_gradient;
		for (int i = 1; i < gradient.Length; ++i)
			Assert.True(gradient[i] >= gradient[i - 1]);
	}
}
