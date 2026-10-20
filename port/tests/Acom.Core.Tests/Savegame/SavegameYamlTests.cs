namespace Acom.Tests.Savegame;

/// <summary>YAML load/save of the small savegame records (rule-dependent parts need the Mod port).</summary>
public class SavegameYamlTests
{
	private static YamlNodeReader Parse(string yaml) => new YamlRootNodeReader(yaml, "test").ToBase();

	private static string[] Keys(YamlNodeReader reader) => reader.Children().Select(c => c.Key).ToArray();

	[Fact]
	public void Mission_statistics_roundtrip_in_cpp_key_order()
	{
		var ms = new MissionStatistics
		{
			id = 4,
			markerName = "STR_UFO_",
			markerId = 2,
			time = new GameTime(2, 3, 4, 1999, 5, 6, 0),
			region = "STR_EUROPE",
			country = "STR_FRANCE",
			type = "STR_UFO_CRASH_RECOVERY",
			ufo = "STR_SMALL_SCOUT",
			success = true,
			rating = "STR_RATING_GOOD",
			score = 120,
			alienRace = "STR_SECTOID",
			daylight = 5,
			valiantCrux = true,
			lootValue = 1000,
		};
		ms.injuryList[7] = 3;
		ms.injuryList[2] = 1;

		var writer = new YamlRootNodeWriter();
		ms.Save(writer);
		var r = Parse(writer.Emit());
		Assert.Equal(["id", "markerName", "markerId", "time", "region", "country", "type", "ufo", "success", "score", "rating", "alienRace", "daylight", "injuryList", "valiantCrux", "lootValue"], Keys(r));
		Assert.Equal(["2", "7"], Keys(r["injuryList"]));

		var loaded = new MissionStatistics(r);
		Assert.Equal(4, loaded.id);
		Assert.Equal("STR_UFO_", loaded.markerName);
		Assert.Equal(2, loaded.markerId);
		Assert.Equal("1999-04-03 05:06:00", loaded.time.FullString);
		Assert.Equal("STR_FRANCE", loaded.LocationString);
		Assert.True(loaded.success);
		Assert.Equal(120, loaded.score);
		Assert.Equal(3, loaded.injuryList[7]);
		Assert.True(loaded.valiantCrux);
		Assert.Equal(1000, loaded.lootValue);
		Assert.True(loaded.IsUfoMission);
		Assert.False(loaded.IsAlienBase);
	}

	[Fact]
	public void Mission_statistics_defaults_skip_optional_keys()
	{
		var ms = new MissionStatistics { type = "STR_ALIEN_BASE_ASSAULT" };
		var writer = new YamlRootNodeWriter();
		ms.Save(writer);
		var r = Parse(writer.Emit());
		Assert.Equal(["id", "time", "region", "country", "type", "ufo", "success", "score", "rating", "alienRace", "daylight"], Keys(r));
		Assert.Equal("STR_REGION_UNKNOWN", ms.LocationString);
		Assert.True(ms.IsAlienBase);
		Assert.False(ms.IsUfoMission);
		Assert.False(ms.IsBaseDefense);
	}

	[Fact]
	public void Region_activity_keeps_twelve_months()
	{
		var region = new Region(null);
		region.Load(Parse("{type: STR_EUROPE, activityXcom: [1, 2], activityAlien: [3, 4]}"));
		region.AddActivityXcom(10);
		region.AddActivityAlien(20);
		Assert.Equal([1, 12], region.ActivityXcom);
		Assert.Equal([3, 24], region.ActivityAlien);
		for (int i = 0; i < 11; ++i)
			region.NewMonth();
		Assert.Equal(12, region.ActivityXcom.Count);
		Assert.Equal(12, region.ActivityXcom[0]);
		Assert.Equal(0, region.ActivityXcom[^1]);
	}

	[Fact]
	public void Country_load_and_pact_flags()
	{
		var country = new Country(null, false);
		Assert.Empty(country.Funding);
		country.Load(Parse("{type: STR_USA, funding: [600000, 700000], activityXcom: [5], activityAlien: [7], pact: false, newPact: true}"));
		Assert.Equal([600000, 700000], country.Funding);
		Assert.True(country.NewPact);
		Assert.False(country.Pact);
		Assert.False(country.CanBeInfiltrated());
		Assert.Equal(Country.Satisfaction.SATISFIED, country.GetSatisfaction());

		country.SetCancelPact(); // prevents the not-yet-signed pact
		Assert.False(country.NewPact);
		Assert.False(country.CancelPact);
		Assert.True(country.CanBeInfiltrated());

		country.SetPact();
		Assert.Equal(Country.Satisfaction.ALIEN_PACT, country.GetSatisfaction());
		Assert.False(country.CanBeInfiltrated());
		country.SetCancelPact();
		Assert.True(country.CancelPact);
		Assert.True(country.CanBeInfiltrated());

		country.SetFunding(123000);
		Assert.Equal(123000, country.Funding[^1]);
	}

	[Fact]
	public void Geoscape_event_counts_down_in_30_minute_steps()
	{
		var ev = new GeoscapeEvent(null);
		ev.Load(Parse("{name: STR_EVENT, spawnCountdown: 90}"));
		Assert.Equal(90, ev.SpawnCountdown);
		ev.Think();
		Assert.Equal(60, ev.SpawnCountdown);
		ev.Think();
		ev.Think();
		Assert.True(ev.IsOver);
		ev.SpawnCountdown = 120; // ignored once over
		Assert.Equal(30, ev.SpawnCountdown);

		var over = new GeoscapeEvent(null);
		over.Load(Parse("{spawnCountdown: 60, over: true}"));
		Assert.True(over.IsOver);
	}
}
