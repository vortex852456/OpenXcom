namespace Acom.Tests.Geoscape;

/// <summary>Monthly rating thresholds, rating names and the monthly score computation.</summary>
[Collection(nameof(Acom.Tests.Engine.GameTests))]
public sealed class MonthlyReportStateTests : IDisposable
{
	private const string Rules = """
		interfaces:
		  - type: monthlyReport
		    palette: PAL_GEOSCAPE
		    backgroundImage: BACK01.SCR
		    elements:
		      - { id: window, color: 1, color2: 2 }
		      - { id: button, color: 1 }
		      - { id: text1, color: 2 }
		      - { id: text2, color: 3 }
		  - type: fundingWindow
		    palette: PAL_GEOSCAPE
		    backgroundImage: BACK01.SCR
		    elements:
		      - { id: window, color: 1 }
		      - { id: button, color: 1 }
		      - { id: text1, color: 2 }
		      - { id: text2, color: 3 }
		      - { id: list, color: 4, color2: 5 }
		countries:
		  - type: STR_TEST_COUNTRY2
		    fundingBase: 100
		    areas:
		      - [90, 120, -45, 45]
		""";

	private readonly GeoscapeTestGame _env = new(Rules);

	public void Dispose() => _env.Dispose();

	[Theory]
	[InlineData(-1000, "STR_RATING_TERRIBLE")]
	[InlineData(-300, "STR_RATING_TERRIBLE")]
	[InlineData(-299, "STR_RATING_POOR")]
	[InlineData(0, "STR_RATING_POOR")]
	[InlineData(1, "STR_RATING_GOOD")]
	[InlineData(500, "STR_RATING_GOOD")]
	[InlineData(501, "STR_RATING_EXCELLENT")]
	public void Vanilla_rating_names(int score, string expected)
	{
		// threshold 0 (defeat score 0, beginner difficulty): "OK" needs > 0 and is then overridden by "GOOD"
		Assert.Equal(expected, MonthlyReportState.RatingId(score, 0, []));
	}

	[Fact]
	public void Rating_ok_lies_between_threshold_and_zero()
	{
		Assert.Equal("STR_RATING_POOR", MonthlyReportState.RatingId(-400, -200, []));
		Assert.Equal("STR_RATING_OK", MonthlyReportState.RatingId(-199, -200, []));
		Assert.Equal("STR_RATING_TERRIBLE", MonthlyReportState.RatingId(-500, -200, []));
	}

	[Fact]
	public void Custom_ratings_pick_the_highest_bound_at_or_below_the_score()
	{
		var custom = new SortedDictionary<int, string> { [-100] = "STR_BAD", [0] = "STR_MEH", [200] = "STR_WOW" };
		Assert.Equal("STR_MEH", MonthlyReportState.RatingId(150, 0, custom));
		Assert.Equal("STR_WOW", MonthlyReportState.RatingId(200, 0, custom));
		Assert.Equal("STR_BAD", MonthlyReportState.RatingId(-100, 0, custom));
		Assert.Equal("", MonthlyReportState.RatingId(-101, 0, custom));
	}

	[Fact]
	public void Difficulty_threshold_uses_defeat_score_and_difficulty()
	{
		_env.Save.Difficulty = GameDifficulty.DIFF_VETERAN;
		Assert.Equal(_env.Mod.DefeatScore + 100 * _env.Save.DifficultyCoefficient, MonthlyReportState.DifficultyThreshold(_env.Mod, _env.Save));
		// only negative custom values apply
		_env.Mod.MonthlyRatingThresholds.AddRange([5, 5, -700]);
		Assert.Equal(-700, MonthlyReportState.DifficultyThreshold(_env.Mod, _env.Save));
		_env.Mod.MonthlyRatingThresholds[2] = 10;
		Assert.Equal(_env.Mod.DefeatScore + 100 * _env.Save.DifficultyCoefficient, MonthlyReportState.DifficultyThreshold(_env.Mod, _env.Save));
	}

	[Fact]
	public void Calculate_changes_sums_region_activity_and_research()
	{
		var save = _env.Save;
		save.Countries[0].Funding.Add(100000);
		save.Funds = 1000000;
		save.MonthlyFunding();
		var region = save.Regions[0];
		region.ActivityXcom[^1] = 300;
		region.ActivityAlien[^1] = 100;
		save.ResearchScores[^2] = 50; // the month that just ended (MonthlyFunding opened a new one)

		var state = new MonthlyReportState(null);

		Assert.Equal(50 + 300 - 100, state.RatingTotal);
		Assert.Equal(0, state.LastMonthsRating); // no previous month yet
		Assert.Equal(0, state.GameOver);
		Assert.Equal(2, region.ActivityXcom.Count); // newMonth opened the next month
		// funding change of the single country this month
		var funding = save.Countries[0].Funding;
		Assert.Equal(funding[^1] - funding[^2], state.FundingDiff);
	}

	[Fact]
	public void Funding_list_sorts_stably_by_name_funding_and_change()
	{
		var save = _env.Save;
		save.Countries[0].Funding.AddRange([100000, 120000]);
		var second = new Country(_env.Mod.GetCountry("STR_TEST_COUNTRY2"), false);
		second.Funding.AddRange([200000, 180000]);
		save.Countries.Add(second);

		var state = new FundingState();
		Assert.Equal([20000, -20000], state.FundingCountryList.Select(c => c.change));

		state.SortFundingClick(null);
		Assert.Equal([120000, 180000], state.FundingCountryList.Select(c => c.funding));
		state.SortFundingClick(null);
		Assert.Equal([180000, 120000], state.FundingCountryList.Select(c => c.funding));
		state.SortChangeClick(null);
		Assert.Equal([-20000, 20000], state.FundingCountryList.Select(c => c.change));
		state.SortChangeClick(null);
		Assert.Equal([20000, -20000], state.FundingCountryList.Select(c => c.change));
		state.SortNameClick(null);
		Assert.Equal(["STR_TEST_COUNTRY", "STR_TEST_COUNTRY2"], state.FundingCountryList.Select(c => c.name));
	}

	[Fact]
	public void Descending_stable_sort_keeps_reverse_order_of_equal_elements()
	{
		// std::stable_sort over reverse iterators: descending, equal elements keep their order
		var list = new List<FundingCountry> { new("a", 1, 0), new("b", 1, 0), new("c", 2, 0) };
		FundingState.StableSort(list, (x, y) => x.funding < y.funding, true);
		Assert.Equal(["c", "a", "b"], list.Select(c => c.name));
		FundingState.StableSort(list, (x, y) => x.funding < y.funding, false);
		Assert.Equal(["a", "b", "c"], list.Select(c => c.name));
	}
}
