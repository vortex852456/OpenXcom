using GameMod = Acom.Mod;

namespace Acom.Tests.Basescape;

/// <summary>
/// List filtering/sorting of the research and manufacture screens (NewResearchListState, NewManufactureListState,
/// GlobalResearchDiaryState) and the small formulas of ManufactureInfoState/TechTreeViewerState.
/// </summary>
public class ResearchManufactureListTests
{
	private static readonly GameMod s_mod = new(createConverter: false);

	private static RuleResearch Research(string name, int cost, int listOrder, bool withRequirement = false)
	{
		var rule = new RuleResearch(name, listOrder);
		rule.Load(new YamlRootNodeReader($"cost: {cost}", "test").ToBase(), s_mod);
		if (withRequirement)
		{
			rule.Requirements.Add(new RuleResearch("STR_OTHER", 0));
		}
		return rule;
	}

	private static RuleManufacture Manufacture(string name, int cost, int time = 10)
	{
		var rule = new RuleManufacture(name, 0);
		rule.Load(new YamlRootNodeReader($"cost: {cost}\ntime: {time}", "test").ToBase(), s_mod);
		return rule;
	}

	private static string Tr(string id) => id switch
	{
		"STR_LASER" => "Laser Weapons",
		"STR_PLASMA" => "Plasma Rifle",
		"STR_ALIEN" => "alien origins",
		_ => id,
	};

	[Fact]
	public void Research_sort_by_list_order_cost_and_name()
	{
		var laser = Research("STR_LASER", 300, 3);
		var plasma = Research("STR_PLASMA", 100, 1);
		var alien = Research("STR_ALIEN", 200, 2);

		List<RuleResearch> list = [laser, plasma, alien];
		NewResearchListState.SortProjects(list, 0, false, Tr);
		Assert.Equal([plasma, alien, laser], list);

		NewResearchListState.SortProjects(list, 1, false, Tr);
		Assert.Equal([plasma, alien, laser], list);

		// "show only new" (3) sorts by cost only when sortByCost is set, else by list order
		list = [laser, alien, plasma];
		NewResearchListState.SortProjects(list, 3, true, Tr);
		Assert.Equal([plasma, alien, laser], list);

		// natural compare is case-insensitive: "alien origins" < "Laser Weapons" < "Plasma Rifle"
		list = [plasma, laser, alien];
		NewResearchListState.SortProjects(list, 2, false, Tr);
		Assert.Equal([alien, laser, plasma], list);
	}

	[Theory]
	// status, showOnlyNew, selectedSort, expected filtered out
	[InlineData(RuleResearch.RESEARCH_STATUS_NEW, false, 0, false)]
	[InlineData(RuleResearch.RESEARCH_STATUS_HIDDEN, false, 0, true)]
	[InlineData(RuleResearch.RESEARCH_STATUS_HIDDEN, false, 2, true)]
	[InlineData(RuleResearch.RESEARCH_STATUS_NORMAL, true, 0, true)]
	[InlineData(RuleResearch.RESEARCH_STATUS_NEW, true, 0, false)]
	[InlineData(RuleResearch.RESEARCH_STATUS_NORMAL, false, 3, true)]
	[InlineData(RuleResearch.RESEARCH_STATUS_HIDDEN, false, 4, false)]
	[InlineData(RuleResearch.RESEARCH_STATUS_NORMAL, false, 4, true)]
	public void Research_status_filter(int status, bool showOnlyNew, int selectedSort, bool expected)
	{
		var rule = Research("STR_LASER", 100, 0);
		Assert.Equal(expected, NewResearchListState.IsFilteredOut(rule, status, showOnlyNew, selectedSort, "", Tr));
	}

	[Fact]
	public void Research_quick_search_and_requirements()
	{
		var rule = Research("STR_LASER", 100, 0);
		Assert.False(NewResearchListState.IsFilteredOut(rule, RuleResearch.RESEARCH_STATUS_NORMAL, false, 0, "WEAP", Tr));
		Assert.True(NewResearchListState.IsFilteredOut(rule, RuleResearch.RESEARCH_STATUS_NORMAL, false, 0, "PLASMA", Tr));
		// search is done on the translated, upper-cased name only
		Assert.True(NewResearchListState.IsFilteredOut(rule, RuleResearch.RESEARCH_STATUS_NORMAL, false, 0, "STR_", Tr));

		// projects with "requires" can only be discovered indirectly
		var withRequires = Research("STR_PLASMA", 100, 0, withRequirement: true);
		Assert.True(NewResearchListState.IsFilteredOut(withRequires, RuleResearch.RESEARCH_STATUS_NORMAL, false, 0, "", Tr));
	}

	[Theory]
	[InlineData(RuleResearch.RESEARCH_STATUS_NEW, true, true, RuleResearch.RESEARCH_STATUS_NORMAL)]
	[InlineData(RuleResearch.RESEARCH_STATUS_NEW, false, true, RuleResearch.RESEARCH_STATUS_HIDDEN)]
	[InlineData(RuleResearch.RESEARCH_STATUS_NORMAL, true, true, RuleResearch.RESEARCH_STATUS_HIDDEN)]
	[InlineData(RuleResearch.RESEARCH_STATUS_NORMAL, true, false, RuleResearch.RESEARCH_STATUS_NEW)]
	[InlineData(RuleResearch.RESEARCH_STATUS_HIDDEN, true, true, RuleResearch.RESEARCH_STATUS_NEW)]
	public void Research_status_toggle(int oldState, bool highlight, bool sorting, int expected)
	{
		Assert.Equal(expected, NewResearchListState.NextResearchStatus(oldState, highlight, sorting));
	}

	[Theory]
	// status, filter, showOnlyNew, expected filtered out
	[InlineData(RuleManufacture.MANU_STATUS_NORMAL, ManufacturingFilterType.MANU_FILTER_DEFAULT, false, false)]
	[InlineData(RuleManufacture.MANU_STATUS_HIDDEN, ManufacturingFilterType.MANU_FILTER_DEFAULT, false, true)]
	[InlineData(RuleManufacture.MANU_STATUS_HIDDEN, ManufacturingFilterType.MANU_FILTER_DEFAULT_SUPPLIES_OK, false, true)]
	[InlineData(RuleManufacture.MANU_STATUS_HIDDEN, ManufacturingFilterType.MANU_FILTER_DEFAULT_NO_SUPPLIES, false, true)]
	[InlineData(RuleManufacture.MANU_STATUS_HIDDEN, ManufacturingFilterType.MANU_FILTER_FACILITY_REQUIRED, false, false)]
	[InlineData(RuleManufacture.MANU_STATUS_HIDDEN, ManufacturingFilterType.MANU_FILTER_HIDDEN, false, false)]
	[InlineData(RuleManufacture.MANU_STATUS_NEW, ManufacturingFilterType.MANU_FILTER_HIDDEN, false, true)]
	[InlineData(RuleManufacture.MANU_STATUS_NORMAL, ManufacturingFilterType.MANU_FILTER_DEFAULT, true, true)]
	[InlineData(RuleManufacture.MANU_STATUS_NEW, ManufacturingFilterType.MANU_FILTER_DEFAULT, true, false)]
	public void Manufacture_status_filter(int status, ManufacturingFilterType filter, bool showOnlyNew, bool expected)
	{
		var rule = Manufacture("STR_LASER", 100);
		Assert.Equal(expected, NewManufactureListState.IsFilteredOut(rule, status, filter, showOnlyNew, "", Tr));
	}

	[Fact]
	public void Manufacture_quick_search()
	{
		var rule = Manufacture("STR_PLASMA", 100);
		Assert.False(NewManufactureListState.IsFilteredOut(rule, RuleManufacture.MANU_STATUS_NORMAL, ManufacturingFilterType.MANU_FILTER_DEFAULT, false, "RIFLE", Tr));
		Assert.True(NewManufactureListState.IsFilteredOut(rule, RuleManufacture.MANU_STATUS_NORMAL, ManufacturingFilterType.MANU_FILTER_DEFAULT, false, "LASER", Tr));
	}

	[Theory]
	[InlineData(RuleManufacture.MANU_STATUS_NEW, true, RuleManufacture.MANU_STATUS_NORMAL)]
	[InlineData(RuleManufacture.MANU_STATUS_NORMAL, true, RuleManufacture.MANU_STATUS_HIDDEN)]
	[InlineData(RuleManufacture.MANU_STATUS_HIDDEN, true, RuleManufacture.MANU_STATUS_NEW)]
	[InlineData(RuleManufacture.MANU_STATUS_NEW, false, RuleManufacture.MANU_STATUS_HIDDEN)]
	[InlineData(RuleManufacture.MANU_STATUS_HIDDEN, false, RuleManufacture.MANU_STATUS_NORMAL)]
	public void Manufacture_status_toggle(int oldState, bool highlight, int expected)
	{
		Assert.Equal(expected, NewManufactureListState.NextManufactureStatus(oldState, highlight));
	}

	[Fact]
	public void Manufacture_supplies_limited_by_funds_and_items()
	{
		var alloy = new RuleItem("STR_ALIEN_ALLOYS", 0);
		var rule = Manufacture("STR_LASER", 1000);
		var items = new ItemContainer();

		Assert.Equal(10, NewManufactureListState.GetProductionPossible(rule, 1_000_000, items));
		Assert.Equal(3, NewManufactureListState.GetProductionPossible(rule, 3999, items));
		Assert.Equal(0, NewManufactureListState.GetProductionPossible(rule, 999, items));

		rule.RequiredItems[alloy] = 4;
		items.AddItem(alloy, 9);
		Assert.Equal(2, NewManufactureListState.GetProductionPossible(rule, 1_000_000, items));
		Assert.Equal(0, NewManufactureListState.GetProductionPossible(rule, 999, items));

		var free = Manufacture("STR_FREE", 0);
		Assert.Equal(10, NewManufactureListState.GetProductionPossible(free, -5, items));
	}

	[Fact]
	public void Monthly_net_funds()
	{
		var rule = Manufacture("STR_LASER", 1000, time: 100);
		// 730 man hours per engineer and month; infinite: 2 engineers -> 14.6 items -> (5000 - 1000) * 14.6
		Assert.Equal(58400, ManufactureInfoState.CalcMonthlyNetFunds(rule, 5000, 2, true, 0, 0));
		// limited by the remaining 3 units (300 man hours)
		Assert.Equal(12000, ManufactureInfoState.CalcMonthlyNetFunds(rule, 5000, 2, false, 5, 2));
		// not sold: only the costs, truncated towards zero
		Assert.Equal(-7300, ManufactureInfoState.CalcMonthlyNetFunds(rule, 0, 1, true, 0, 0));
	}

	[Theory]
	[InlineData(0, "")]
	[InlineData(3, "...")]
	[InlineData(9, "-....")]
	[InlineData(12, "--")]
	[InlineData(147, "#==-")]
	[InlineData(250, "##==--")]
	public void Tech_tree_cost_indicator(int cost, string expected)
	{
		Assert.Equal(expected, TechTreeViewerState.GetCostIndicator(cost));
	}

	private static TranslatedResearchDiaryItem DiaryItem(string name, uint year, uint month, uint day, int sortOrder)
	{
		var entry = new ResearchDiaryEntry(new RuleResearch("STR_" + name, 0)) { year = year, month = month, day = day };
		string date = GlobalResearchDiaryState.FormatDate(entry);
		return new TranslatedResearchDiaryItem(entry, name, Unicode.UpperCase(name), date, sortOrder);
	}

	[Fact]
	public void Diary_quick_search_by_name_or_date()
	{
		var a = DiaryItem("Laser Weapons", 1999, 1, 5, 2);
		var b = DiaryItem("Plasma Rifle", 2000, 11, 23, 1);
		Assert.Equal("1999-01-05", a.date);
		Assert.Equal("2000-11-23", b.date);

		List<TranslatedResearchDiaryItem> filtered = [];
		GlobalResearchDiaryState.FilterItems([a, b], "", filtered);
		Assert.Equal([a, b], filtered);

		filtered.Clear();
		GlobalResearchDiaryState.FilterItems([a, b], "RIFLE", filtered);
		Assert.Equal([b], filtered);

		filtered.Clear();
		GlobalResearchDiaryState.FilterItems([a, b], "1999-01", filtered);
		Assert.Equal([a], filtered);
	}

	[Fact]
	public void Diary_sorting_is_stable_in_both_directions()
	{
		var a1 = DiaryItem("Alpha", 1999, 1, 1, 4);
		var b = DiaryItem("Beta", 1999, 1, 2, 3);
		var a2 = DiaryItem("Alpha", 1999, 1, 3, 2);
		var c = DiaryItem("Gamma", 1999, 1, 4, 1);

		List<TranslatedResearchDiaryItem> items = [a1, b, a2, c];
		GlobalResearchDiaryState.SortItems(items, ReserachDiarySort.RESEARCH_DIARY_SORT_NAME_ASC);
		Assert.Equal([a1, a2, b, c], items);

		// std::stable_sort over reverse iterators: descending, equal names keep their forward order
		items = [a1, b, a2, c];
		GlobalResearchDiaryState.SortItems(items, ReserachDiarySort.RESEARCH_DIARY_SORT_NAME_DESC);
		Assert.Equal([c, b, a1, a2], items);

		items = [a1, b, a2, c];
		GlobalResearchDiaryState.SortItems(items, ReserachDiarySort.RESEARCH_DIARY_SORT_DATE_ASC);
		Assert.Equal([c, a2, b, a1], items);

		GlobalResearchDiaryState.SortItems(items, ReserachDiarySort.RESEARCH_DIARY_SORT_DATE_DESC);
		Assert.Equal([a1, b, a2, c], items);

		GlobalResearchDiaryState.SortItems(items, ReserachDiarySort.RESEARCH_DIARY_SORT_NONE);
		Assert.Equal([a1, b, a2, c], items);
	}

	[Theory]
	[InlineData("SHAZAM", "Anything", false, true)]
	[InlineData("SHAZAM", "Anything", true, false)]
	[InlineData("LAS", "Laser Weapons", true, true)]
	[InlineData("LAS", "Mind Probe", false, false)]
	public void Tech_tree_select_search(string search, string name, bool discovered, bool expected)
	{
		Assert.Equal(expected, TechTreeSelectState.MatchesSearch(search, name, discovered));
	}
}
