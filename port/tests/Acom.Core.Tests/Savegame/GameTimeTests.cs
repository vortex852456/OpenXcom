namespace Acom.Tests.Savegame;

public class GameTimeTests
{
	[Fact]
	public void Advance_returns_the_largest_elapsed_trigger()
	{
		Assert.Equal(TimeTrigger.TIME_5SEC, new GameTime(1, 1, 1, 1999, 0, 0, 0).Advance());
		Assert.Equal(TimeTrigger.TIME_5SEC, new GameTime(1, 1, 1, 1999, 0, 0, 55).Advance()); // minute 1
		Assert.Equal(TimeTrigger.TIME_10MIN, new GameTime(1, 1, 1, 1999, 0, 9, 55).Advance());
		Assert.Equal(TimeTrigger.TIME_30MIN, new GameTime(1, 1, 1, 1999, 0, 29, 55).Advance());
		Assert.Equal(TimeTrigger.TIME_1HOUR, new GameTime(1, 1, 1, 1999, 0, 59, 55).Advance());
		Assert.Equal(TimeTrigger.TIME_1DAY, new GameTime(1, 1, 1, 1999, 23, 59, 55).Advance());
	}

	[Fact]
	public void Advance_rolls_over_into_the_next_year()
	{
		var t = new GameTime(6, 31, 12, 1999, 23, 59, 55);
		Assert.Equal(TimeTrigger.TIME_1MONTH, t.Advance());
		Assert.Equal("2000-01-01 00:00:00", t.FullString);
		Assert.Equal(7, t.Weekday);
		Assert.Equal("STR_SATURDAY", t.WeekdayString);
		Assert.Equal("STR_JAN", t.MonthString);

		var w = new GameTime(7, 1, 1, 2000, 23, 59, 55);
		Assert.Equal(TimeTrigger.TIME_1DAY, w.Advance());
		Assert.Equal(1, w.Weekday);
		Assert.Equal("STR_SUNDAY", w.WeekdayString);
	}

	[Theory]
	[InlineData(2000, true)]
	[InlineData(2004, true)]
	[InlineData(1900, false)]
	[InlineData(2100, false)]
	[InlineData(1999, false)]
	public void February_has_29_days_in_leap_years(int year, bool leap)
	{
		var t = new GameTime(1, 28, 2, year, 23, 59, 55);
		Assert.Equal(!leap, t.IsLastDayOfMonth);
		Assert.Equal(leap ? TimeTrigger.TIME_1DAY : TimeTrigger.TIME_1MONTH, t.Advance());
		Assert.Equal(leap ? 2 : 3, t.Month);
		Assert.Equal(leap ? 29 : 1, t.Day);
		Assert.Equal(leap, t.IsLastDayOfMonth);
	}

	[Fact]
	public void A_full_day_produces_the_expected_triggers()
	{
		var t = new GameTime(1, 15, 6, 1999, 0, 0, 0);
		var counts = new Dictionary<TimeTrigger, int>();
		for (int i = 0; i < 24 * 60 * 12; ++i)
		{
			var trigger = t.Advance();
			counts[trigger] = counts.GetValueOrDefault(trigger) + 1;
		}
		Assert.Equal("1999-06-16 00:00:00", t.FullString);
		Assert.Equal(24 * 714, counts[TimeTrigger.TIME_5SEC]);
		Assert.Equal(24 * 4, counts[TimeTrigger.TIME_10MIN]);
		Assert.Equal(24, counts[TimeTrigger.TIME_30MIN]);
		Assert.Equal(23, counts[TimeTrigger.TIME_1HOUR]);
		Assert.Equal(1, counts[TimeTrigger.TIME_1DAY]);
	}

	[Fact]
	public void Daylight_is_zero_at_6h_and_half_at_18h()
	{
		Assert.Equal(0.0, new GameTime(1, 1, 1, 1999, 6, 0, 0).Daylight);
		Assert.Equal(0.25, new GameTime(1, 1, 1, 1999, 12, 0, 0).Daylight);
		Assert.Equal(0.5, new GameTime(1, 1, 1, 1999, 18, 0, 0).Daylight);
		Assert.Equal((double)((18 * 60 + 30) * 60 + 5) / 86400, new GameTime(1, 1, 1, 1999, 0, 30, 5).Daylight);
	}

	[Fact]
	public void Yaml_roundtrip_uses_a_flow_map_in_cpp_key_order()
	{
		var writer = new YamlRootNodeWriter();
		new GameTime(3, 21, 5, 1999, 14, 30, 5).Save(writer["time"]);
		Assert.Equal("time: {second: 5, minute: 30, hour: 14, weekday: 3, day: 21, month: 5, year: 1999}", writer.Emit().Trim());

		var t = new GameTime(0, 0, 0, 0, 0, 0, 0);
		t.Load(new YamlRootNodeReader(writer.Emit(), "test")["time"]);
		Assert.Equal("1999-05-21 14:30:05", t.FullString);
		Assert.Equal(3, t.Weekday);

		// missing keys keep their values
		var partial = new GameTime(6, 1, 1, 1999, 12, 0, 0);
		partial.Load(new YamlRootNodeReader("{hour: 3, year: 2001}", "test").ToBase());
		Assert.Equal("2001-01-01 03:00:00", partial.FullString);
	}

	[Fact]
	public void Copy_constructor_makes_an_independent_copy()
	{
		var a = new GameTime(1, 1, 1, 1999, 0, 0, 0);
		var b = new GameTime(a);
		b.Advance();
		Assert.Equal(0, a.Second);
		Assert.Equal(5, b.Second);
	}
}

[Collection(nameof(Acom.Tests.Engine.OptionsTests))] // Language reads Options.language / Options.wordwrap
public class GameTimeLanguageTests
{
	[Fact]
	public void Day_string_uses_the_ordinal_suffix_ids()
	{
		Options.language = "en-US";
		Options.wordwrap = TextWrapping.WRAP_AUTO;
		var lang = new Language();
		lang.LoadFile(new YamlRootNodeReader("""
			en-US:
			  STR_DATE_FIRST: "{0}st"
			  STR_DATE_SECOND: "{0}nd"
			  STR_DATE_THIRD: "{0}rd"
			  STR_DATE_FOURTH: "{0}th"
			""", "en-US.yml"));
		string Day(int day) => new GameTime(1, day, 1, 1999, 0, 0, 0).GetDayString(lang);
		Assert.Equal("1st", Day(1));
		Assert.Equal("2nd", Day(2));
		Assert.Equal("3rd", Day(3));
		Assert.Equal("4th", Day(4));
		Assert.Equal("11th", Day(11));
		Assert.Equal("12th", Day(12));
		Assert.Equal("21st", Day(21));
		Assert.Equal("22nd", Day(22));
		Assert.Equal("23rd", Day(23));
		Assert.Equal("31st", Day(31));
	}
}
