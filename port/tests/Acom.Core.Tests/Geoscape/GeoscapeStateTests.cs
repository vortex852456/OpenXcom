using System.Reflection;
using GameMod = Acom.Mod;

namespace Acom.Tests.Geoscape;

/// <summary>
/// Deterministic parts of the Geoscape time handlers on a tiny campaign (stand-in surfaces, fonts and palette,
/// rules from inline YAML; no original data).
/// </summary>
// Game sets a static game pointer for State, so these tests must not run in parallel with the other Game tests.
[Collection(nameof(Acom.Tests.Engine.GameTests))]
public sealed class GeoscapeStateTests : IDisposable
{
	private const string Rules = """
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
		    areas:
		      - [0, 45, -45, 45]
		""";

	private static double Rad(double deg) => deg * Math.PI / 180.0;

	private readonly Game _game;
	private readonly GameMod _mod;
	private readonly SavedGame _save;
	private readonly int _oldGeoscapeX = Options.baseXGeoscape, _oldGeoscapeY = Options.baseYGeoscape;
	private readonly bool _oldShowFunds = Options.showFundsOnGeoscape;

	public GeoscapeStateTests()
	{
		Options.baseXGeoscape = 320;
		Options.baseYGeoscape = 200;
		Options.showFundsOnGeoscape = false;
		_game = new Game(new Screen(4, 4));
		_mod = new GameMod(createConverter: false);
		var texture = new SurfaceSet(32, 32);
		for (int i = 0; i < 39; ++i)
			texture.AddFrame(i);
		_mod._sets["TEXTURE.DAT"] = texture;
		var markers = new SurfaceSet(3, 3);
		for (int i = 0; i < 16; ++i)
			markers.AddFrame(i);
		_mod._sets["GlobeMarkers"] = markers;
		_mod._surfaces["GEOBORD.SCR"] = new Surface(320, 200);
		_mod._surfaces["ALTGEOBORD.SCR"] = new Surface(320, 200);
		var palette = new Palette();
		palette.LoadDat(new MemoryStream(new byte[256 * 3]), 256);
		_mod._palettes["PAL_GEOSCAPE"] = palette;
		var fonts = (SortedDictionary<string, Font>)typeof(GameMod).GetField("_fonts", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_mod)!;
		foreach (var name in new[] { "FONT_BIG", "FONT_SMALL", "FONT_GEO_BIG", "FONT_GEO_SMALL" })
		{
			var font = new Font();
			font.LoadTerminal();
			fonts[name] = font;
		}
		_mod.LoadFile(new YamlRootNodeReader(Rules, "test.rul"));
		_mod.Globe.Load(new YamlRootNodeReader("polygons: []", "test").ToBase(), null);
		typeof(Game).GetField("_mod", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(_game, _mod);

		_save = new SavedGame();
		_save.Regions.Add(new Region(_mod.GetRegion("STR_TEST_REGION")));
		_save.Countries.Add(new Country(_mod.GetCountry("STR_TEST_COUNTRY"), false));
		// 1999-01-05 12:00:00, far from the autosave days and the end of the month
		_save.Time = new GameTime(2, 5, 1, 1999, 12, 0, 0);
		_save.Bases.Add(new Base(_mod) { Longitude = Rad(10), Latitude = Rad(10) });
		_game.SavedGame = _save;
	}

	public void Dispose()
	{
		Options.baseXGeoscape = _oldGeoscapeX;
		Options.baseYGeoscape = _oldGeoscapeY;
		Options.showFundsOnGeoscape = _oldShowFunds;
		State.SetGamePtr(null);
	}

	private static T Field<T>(GeoscapeState state, string name) =>
		(T)typeof(GeoscapeState).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(state)!;

	private Ufo NewUfo(Ufo.UfoStatus status, int missionScore = 3)
	{
		var rule = new RuleUfo("STR_TEST_UFO");
		typeof(RuleUfo).GetField("_missionScore", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(rule, missionScore);
		var mission = new AlienMission(new RuleAlienMission("STR_TEST_MISSION"));
		var ufo = new Ufo(rule, 0) { Longitude = Rad(20), Latitude = Rad(5), Status = status };
		mission.Race = "STR_TEST_RACE";
		ufo.SetMissionInfo(mission, new UfoTrajectory("P0"));
		_save.Ufos.Add(ufo);
		return ufo;
	}

	[Fact]
	public void Time_display_shows_the_game_clock()
	{
		var state = new GeoscapeState();
		Assert.Equal("00", Field<Text>(state, "_txtSec").GetText());
		Assert.Equal("00", Field<Text>(state, "_txtMin").GetText());
		Assert.Equal("12", Field<Text>(state, "_txtHour").GetText());
		Assert.Equal("1999", Field<Text>(state, "_txtYear").GetText());
		Assert.Same(state.Globe, Field<Globe>(state, "_globe"));
		Assert.Equal(1, state.FirstFreeDogfightSlot);
		Assert.Equal(0, state.MinimizedDogfightsCount());
	}

	[Fact]
	public void Time_advance_runs_one_step_per_5_seconds_and_a_full_day_at_1_day_speed()
	{
		var state = new GeoscapeState();
		state.TimeAdvance();
		Assert.Equal((12, 0, 5), (_save.Time.Hour, _save.Time.Minute, _save.Time.Second));

		// "1 day" speed: 17280 steps of 5 seconds, triggering the 10/30 minute, hour and day handlers on the way
		Field<System.Runtime.CompilerServices.StrongBox<TextButton>>(state, "_timeSpeed").Value = Field<TextButton>(state, "_btn1Day");
		int days = _save.DaysPassed;
		state.TimeAdvance();
		Assert.Equal((6, 12, 0, 5), (_save.Time.Day, _save.Time.Hour, _save.Time.Minute, _save.Time.Second));
		Assert.Equal(days + 1, _save.DaysPassed);
	}

	[Fact]
	public void Popups_pause_the_clock_and_are_pushed_one_per_think()
	{
		var state = new GeoscapeState();
		var popup1 = new State();
		var popup2 = new State();
		state.Popup(popup1);
		state.Popup(popup2);
		state.TimeAdvance();
		Assert.Equal(0, _save.Time.Second); // paused by the popups
		Assert.Equal(new[] { popup1, popup2 }, Field<List<State>>(state, "_popups"));

		state.Think();
		Assert.Equal(new[] { popup2 }, Field<List<State>>(state, "_popups"));
		state.Think();
		Assert.Empty(Field<List<State>>(state, "_popups"));
		// the paused advance recomputed the pause flag (no dogfights or zoom effects): the clock runs again
		state.TimeAdvance();
		Assert.Equal(5, _save.Time.Second);
	}

	[Fact]
	public void Game_over_without_bases()
	{
		var state = new GeoscapeState();
		_save.Bases.Clear();
		state.Time5Seconds();
		Assert.Equal(GameEnding.END_LOSE, _save.Ending);
		Assert.Equal(0, _save.Time.Second); // the time handlers do not advance the clock themselves
	}

	[Fact]
	public void Time_30_minutes_scores_ufos_and_expires_crashed_ones()
	{
		var state = new GeoscapeState();
		var flying = NewUfo(Ufo.UfoStatus.FLYING, 3);
		var landed = NewUfo(Ufo.UfoStatus.LANDED, 3);
		landed.Longitude = Rad(60); // inside the region, outside the country
		landed.SecondsRemaining = 3600;
		var crashed = NewUfo(Ufo.UfoStatus.CRASHED);
		crashed.SecondsRemaining = 1900;
		var expired = NewUfo(Ufo.UfoStatus.CRASHED);
		expired.SecondsRemaining = 1799;

		state.Time30Minutes();

		Assert.Equal(3 + 3 * 2, _save.Regions[0].ActivityAlien[^1]);
		Assert.Equal(3, _save.Countries[0].ActivityAlien[^1]);
		Assert.Equal(Ufo.UfoStatus.CRASHED, crashed.Status);
		Assert.Equal(100, crashed.SecondsRemaining);
		Assert.Equal(Ufo.UfoStatus.DESTROYED, expired.Status);
		Assert.False(flying.Detected); // no radar
		Assert.Empty(Field<List<State>>(state, "_popups"));

		// destroyed UFOs are cleaned up by the next 5 second step
		state.Time5Seconds();
		Assert.DoesNotContain(expired, _save.Ufos);
		Assert.Contains(crashed, _save.Ufos);
	}

	[Fact]
	public void Mission_sites_count_down_and_score_for_the_aliens()
	{
		var state = new GeoscapeState();
		var deployment = new AlienDeployment("STR_TEST_SITE");
		typeof(AlienDeployment).GetField("_points", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(deployment, 7);
		var site = new MissionSite(new RuleAlienMission("STR_TEST_MISSION"), deployment, null)
		{
			Longitude = Rad(20),
			Latitude = Rad(5),
			SecondsRemaining = 3 * 3600,
		};
		_save.MissionSites.Add(site);

		Assert.False(state.ProcessMissionSite(site));
		Assert.Equal(3 * 3600 - 1800, site.SecondsRemaining);
		Assert.Equal(7, _save.Regions[0].ActivityAlien[^1]);
		Assert.Equal(7, _save.Countries[0].ActivityAlien[^1]);

		// expired and nobody follows it: removed, scored with the despawn penalty (0)
		site.SecondsRemaining = 1000;
		Assert.True(state.ProcessMissionSite(site));
	}
}
