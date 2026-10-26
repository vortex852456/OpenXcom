using System.Reflection;
using GameMod = Acom.Mod;

namespace Acom.Tests.Geoscape;

/// <summary>
/// A tiny campaign for Geoscape screen tests: stand-in surfaces, fonts and palette, rules from inline YAML
/// (no original data). Sets the static game pointer, so users must run in the GameTests collection.
/// </summary>
internal sealed class GeoscapeTestGame : IDisposable
{
	internal const string BaseRules = """
		interfaces:
		  - type: geoscape
		    palette: PAL_GEOSCAPE
		    elements:
		      - id: button
		        color: 1
		      - id: text
		        color: 2
		      - id: slackingIndicator
		        color: 3
		      - id: trainingIndicator
		        color: 4
		      - id: errorMessage
		        color: 5
		      - id: errorPalette
		        color: 6
		regions:
		  - type: STR_TEST_REGION
		    areas:
		      - [0, 90, -45, 45]
		countries:
		  - type: STR_TEST_COUNTRY
		    fundingBase: 100
		    areas:
		      - [0, 45, -45, 45]
		""";

	internal static double Rad(double deg) => deg * Math.PI / 180.0;

	private readonly int _oldGeoscapeX = Options.baseXGeoscape, _oldGeoscapeY = Options.baseYGeoscape;
	private readonly bool _oldShowFunds = Options.showFundsOnGeoscape, _oldMute = Options.mute;

	internal Game Game { get; }
	internal GameMod Mod { get; }
	internal SavedGame Save { get; }

	internal GeoscapeTestGame(string extraRules = "")
	{
		Options.baseXGeoscape = 320;
		Options.baseYGeoscape = 200;
		Options.showFundsOnGeoscape = false;
		Options.mute = true; // no sound sets
		Game = new Game(new Screen(4, 4));
		Mod = new GameMod(createConverter: false);
		var texture = new SurfaceSet(32, 32);
		for (int i = 0; i < 39; ++i)
			texture.AddFrame(i);
		Mod._sets["TEXTURE.DAT"] = texture;
		var markers = new SurfaceSet(3, 3);
		for (int i = 0; i < 16; ++i)
			markers.AddFrame(i);
		Mod._sets["GlobeMarkers"] = markers;
		var icons = new SurfaceSet(32, 32);
		for (int i = 0; i < 40; ++i)
			icons.AddFrame(i);
		Mod._sets["INTICON.PCK"] = icons;
		Mod._surfaces["GEOBORD.SCR"] = new Surface(320, 200);
		Mod._surfaces["ALTGEOBORD.SCR"] = new Surface(320, 200);
		Mod._surfaces["INTERWIN.DAT"] = new Surface(160, 556);
		Mod._surfaces["BACK01.SCR"] = new Surface(320, 200);
		var palette = new Palette();
		palette.LoadDat(new MemoryStream(new byte[256 * 3]), 256);
		Mod._palettes["PAL_GEOSCAPE"] = palette;
		var fonts = (SortedDictionary<string, Font>)typeof(GameMod).GetField("_fonts", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Mod)!;
		foreach (var name in new[] { "FONT_BIG", "FONT_SMALL", "FONT_GEO_BIG", "FONT_GEO_SMALL" })
		{
			var font = new Font();
			font.LoadTerminal();
			fonts[name] = font;
		}
		Mod.LoadFile(new YamlRootNodeReader(BaseRules, "test.rul"));
		if (extraRules.Length != 0)
			Mod.LoadFile(new YamlRootNodeReader(extraRules, "extra.rul"));
		Mod.Globe.Load(new YamlRootNodeReader("polygons: []", "test").ToBase(), null);
		typeof(Game).GetField("_mod", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(Game, Mod);

		Save = new SavedGame();
		Save.Regions.Add(new Region(Mod.GetRegion("STR_TEST_REGION")));
		Save.Countries.Add(new Country(Mod.GetCountry("STR_TEST_COUNTRY"), false));
		// 1999-01-05 12:00:00
		Save.Time = new GameTime(2, 5, 1, 1999, 12, 0, 0);
		Save.Bases.Add(new Base(Mod) { Longitude = Rad(10), Latitude = Rad(10) });
		Game.SavedGame = Save;
	}

	public void Dispose()
	{
		Options.baseXGeoscape = _oldGeoscapeX;
		Options.baseYGeoscape = _oldGeoscapeY;
		Options.showFundsOnGeoscape = _oldShowFunds;
		Options.mute = _oldMute;
		State.SetGamePtr(null);
	}

	internal static T Field<T>(object obj, string name) =>
		(T)obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(obj)!;
}
