#nullable disable // null rule pointers as in the C++ code
using System.Reflection;
using static Acom.Tests.Savegame.SavegameTestHelpers;

namespace Acom.Tests.Battlescape;

[Collection(nameof(Acom.Tests.Mod.GlobalRng))]
public class DebriefingStateTests
{
	private static void SetField(object target, string name, object value)
		=> target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(target, value);

	private static RuleItem Item(string type, SortedDictionary<string, int> dividers = null, Dictionary<RuleItem, List<int>> transformations = null)
	{
		var item = Uninitialized<RuleItem>();
		SetField(item, "_type", type);
		SetField(item, "_recoveryDividers", dividers ?? new SortedDictionary<string, int>(StringComparer.Ordinal));
		SetField(item, "_recoveryTransformations", transformations ?? []);
		return item;
	}

	private static AlienDeployment Deployment(string type)
	{
		var deployment = Uninitialized<AlienDeployment>();
		SetField(deployment, "_type", type);
		return deployment;
	}

	[Theory]
	[InlineData(-500, "STR_RATING_TERRIBLE")]
	[InlineData(-200, "STR_RATING_TERRIBLE")]
	[InlineData(-199, "STR_RATING_POOR")]
	[InlineData(0, "STR_RATING_POOR")]
	[InlineData(1, "STR_RATING_OK")]
	[InlineData(200, "STR_RATING_OK")]
	[InlineData(201, "STR_RATING_GOOD")]
	[InlineData(500, "STR_RATING_GOOD")]
	[InlineData(501, "STR_RATING_EXCELLENT")]
	public void Vanilla_rating_thresholds(int total, string expected)
	{
		Assert.Equal(expected, DebriefingState.CalculateRating(total, []));
	}

	[Fact]
	public void Mod_ratings_pick_highest_threshold_not_above_total()
	{
		var ratings = new SortedDictionary<int, string> { [-100] = "BAD", [0] = "MEH", [300] = "WOW" };
		Assert.Equal("", DebriefingState.CalculateRating(-101, ratings)); // nothing matches: empty rating
		Assert.Equal("BAD", DebriefingState.CalculateRating(-100, ratings));
		Assert.Equal("MEH", DebriefingState.CalculateRating(299, ratings));
		Assert.Equal("WOW", DebriefingState.CalculateRating(1000, ratings));
	}

	[Fact]
	public void Soldier_stat_string_is_empty_for_zero_and_highlighted_otherwise()
	{
		Assert.Equal("", DebriefingState.MakeSoldierString(0));
		Assert.Equal("\u0001+3\u0001", DebriefingState.MakeSoldierString(3));
	}

	[Fact]
	public void Alien_alloys_use_hardcoded_dividers_without_rules()
	{
		Assert.Equal(10, DebriefingState.GetRecoveryDivider("STR_ALIEN_ALLOYS", "STR_ALIEN_ALLOYS", "STR_UFO", null, null));
		Assert.Equal(150, DebriefingState.GetRecoveryDivider("STR_ALIEN_ALLOYS", "STR_ALIEN_ALLOYS", "STR_ALIEN_BASE", null, null));
		Assert.Equal(1, DebriefingState.GetRecoveryDivider("STR_ELERIUM_115", "STR_ALIEN_ALLOYS", "STR_UFO", null, null));
		Assert.Equal(1, DebriefingState.GetRecoveryDivider("STR_ALIEN_ALLOYS", null, "STR_UFO", null, null));
	}

	[Fact]
	public void Recovery_dividers_check_deployment_then_target_then_default()
	{
		var dividers = new SortedDictionary<string, int>(StringComparer.Ordinal) { ["STR_UFO"] = 5, ["STR_OTHER"] = -3, ["STR_TERROR"] = 7 };
		RuleItem item = Item("STR_ALIEN_ALLOYS", dividers);
		Assert.Equal(7, DebriefingState.GetRecoveryDivider("STR_ALIEN_ALLOYS", "STR_ALIEN_ALLOYS", "STR_UFO", Deployment("STR_TERROR"), item));
		Assert.Equal(5, DebriefingState.GetRecoveryDivider("STR_ALIEN_ALLOYS", "STR_ALIEN_ALLOYS", "STR_UFO", Deployment("STR_SMALL_SCOUT"), item));
		Assert.Equal(-3, DebriefingState.GetRecoveryDivider("STR_ALIEN_ALLOYS", "STR_ALIEN_ALLOYS", "STR_BASE", null, item));

		// without "STR_OTHER" the hardcoded alloy default stays
		RuleItem partial = Item("STR_ALIEN_ALLOYS", new SortedDictionary<string, int>(StringComparer.Ordinal) { ["STR_UFO"] = 5 });
		Assert.Equal(150, DebriefingState.GetRecoveryDivider("STR_ALIEN_ALLOYS", "STR_ALIEN_ALLOYS", "STR_ALIEN_BASE", null, partial));
	}

	[Fact]
	public void Divider_divides_or_multiplies_with_integer_truncation()
	{
		var ds = new DebriefingStat("X", true) { qty = 25, score = -19 };
		DebriefingState.ApplyRecoveryDivider(ds, 10);
		Assert.Equal(2, ds.qty);
		Assert.Equal(-1, ds.score); // C++ truncates toward zero

		ds = new DebriefingStat("X", true) { qty = 4, score = 3 };
		DebriefingState.ApplyRecoveryDivider(ds, -3);
		Assert.Equal(12, ds.qty);
		Assert.Equal(9, ds.score);

		ds = new DebriefingStat("X", true) { qty = 4, score = 3 };
		DebriefingState.ApplyRecoveryDivider(ds, -1); // -1, 0 and 1 leave the values alone
		DebriefingState.ApplyRecoveryDivider(ds, 0);
		DebriefingState.ApplyRecoveryDivider(ds, 1);
		Assert.Equal(4, ds.qty);
		Assert.Equal(3, ds.score);
	}

	[Fact]
	public void Items_without_transformations_are_stored_as_is()
	{
		RuleItem item = Item("STR_PISTOL");
		var stores = new ItemContainer();
		DebriefingState.AddItemsToStores(item, stores, 3, true);
		DebriefingState.AddItemsToStores(item, stores, 2, false);
		Assert.Equal(5, stores.GetItem(item));
	}

	[Fact]
	public void Single_weight_transformation_multiplies_without_rng()
	{
		RuleItem result = Item("STR_RESULT");
		RuleItem item = Item("STR_SOURCE", transformations: new Dictionary<RuleItem, List<int>> { [result] = [4] });
		var stores = new ItemContainer();
		Rng.Seed = 7;
		DebriefingState.AddItemsToStores(item, stores, 3, true);
		Assert.Equal(7UL, Rng.Seed); // no RNG call
		Assert.Equal(12, stores.GetItem(result));
		Assert.Equal(0, stores.GetItem(item));

		// considerTransformations = false ignores them
		DebriefingState.AddItemsToStores(item, stores, 3, false);
		Assert.Equal(3, stores.GetItem(item));
	}

	[Fact]
	public void Weighted_transformation_rolls_once_per_item_and_adds_the_weight_index()
	{
		RuleItem result = Item("STR_RESULT");
		List<int> weights = [1, 0, 3, 6]; // index = quantity added
		RuleItem item = Item("STR_SOURCE", transformations: new Dictionary<RuleItem, List<int>> { [result] = weights });

		Rng.Seed = 1234;
		int expected = 0;
		for (int i = 0; i < 20; ++i)
		{
			int roll = Rng.Generate(1, 10);
			expected += roll <= 1 ? 0 : roll <= 4 ? 2 : 3;
		}
		ulong after = Rng.Seed;

		var stores = new ItemContainer();
		Rng.Seed = 1234;
		DebriefingState.AddItemsToStores(item, stores, 20, true);
		Assert.Equal(after, Rng.Seed);
		Assert.Equal(expected, stores.GetItem(result));
	}

	[Fact]
	public void Commendation_skip_counter_counts_repeated_thresholds()
	{
		// criteria 1,1,5,5,5,10: decoration levels with repeated thresholds are skipped in the award name
		List<int> criteria = [1, 1, 5, 5, 5, 10];
		Assert.Equal(0, CommendationState.GetSkipCounter(criteria, 0));
		Assert.Equal(1, CommendationState.GetSkipCounter(criteria, 1));
		Assert.Equal(1, CommendationState.GetSkipCounter(criteria, 2));
		Assert.Equal(3, CommendationState.GetSkipCounter(criteria, 4));
		Assert.Equal(3, CommendationState.GetSkipCounter(criteria, 5));
		Assert.Equal(3, CommendationState.GetSkipCounter(criteria, 99));
		Assert.Equal(0, CommendationState.GetSkipCounter([], 3));
	}
}
