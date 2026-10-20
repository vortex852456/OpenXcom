using static Acom.Tests.Savegame.SavegameTestHelpers;

namespace Acom.Tests.Savegame;

public class BaseTests
{
	[Fact]
	public void DeleteIf_stops_calling_the_predicate_at_the_limit()
	{
		var list = new List<int> { 1, 2, 3, 4, 5, 6 };
		var calls = new List<int>();
		int left = Base.DeleteIf(list, 2, x => { calls.Add(x); return x % 2 == 0; });
		Assert.Equal(0, left);
		Assert.Equal([1, 3, 5, 6], list);
		Assert.Equal([1, 2, 3, 4], calls);
	}

	[Fact]
	public void DeleteIf_returns_remaining_count_and_ignores_non_positive_limits()
	{
		var list = new List<int> { 1, 2, 3 };
		Assert.Equal(2, Base.DeleteIf(list, 3, x => x == 2));
		Assert.Equal([1, 3], list);

		bool called = false;
		Assert.Equal(0, Base.DeleteIf(list, -4, _ => called = true));
		Assert.Equal(0, Base.DeleteIf(list, 0, _ => called = true));
		Assert.False(called);
		Assert.Equal([1, 3], list);
	}

	[Fact]
	public void BaseAreaSubset_offset_intersection_and_bound_box()
	{
		var area = new BaseAreaSubset(2, 2).Offset(1, 1);
		Assert.Equal((1, 3, 1, 3), ((int)area.beg_x, (int)area.end_x, (int)area.beg_y, (int)area.end_y));
		Assert.Equal(2, area.SizeX());

		Assert.True((bool)BaseAreaSubset.Intersection(area, BaseAreaSubset.FromPoint(2, 2)));
		Assert.False((bool)BaseAreaSubset.Intersection(area, BaseAreaSubset.FromPoint(3, 3)));
		Assert.False((bool)BaseAreaSubset.Intersection(area, BaseAreaSubset.FromPoint(0, 2)));
		Assert.Equal(BaseAreaSubset.FromPoint(2, 1), BaseAreaSubset.Intersection(area, new BaseAreaSubset((2, 5), (0, 2))));

		var box = BaseAreaSubset.BoundBox(BaseAreaSubset.FromPoint(0, 0), BaseAreaSubset.FromPoint(4, 2));
		Assert.Equal(new BaseAreaSubset((0, 5), (0, 3)), box);
		// an empty second area keeps the first one
		Assert.Equal(area, BaseAreaSubset.BoundBox(area, new BaseAreaSubset(0, 0)));
	}

	[Fact]
	public void Personnel_totals_count_transfers_research_and_production()
	{
		var b = OrSkip(() => new Base(null));
		b.Scientists = 5;
		b.Engineers = 3;
		var scientists = new Transfer(10);
		scientists.SetScientists(2);
		var engineers = new Transfer(10);
		engineers.SetEngineers(4);
		b.Transfers.Add(scientists);
		b.Transfers.Add(engineers);
		var research = new ResearchProject(Uninitialized<RuleResearch>()) { Assigned = 6 };
		b.AddResearch(research);
		var production = new Production(Uninitialized<RuleManufacture>(), 1) { AssignedEngineers = 7 };
		b.AddProduction(production);

		Assert.Equal(5 + 2 + 6, b.TotalScientists);
		Assert.Equal(3 + 4 + 7, b.TotalEngineers);
		Assert.Equal(5, b.AvailableScientists);
		Assert.Equal(6, b.AllocatedScientists);
		Assert.Equal(7, b.AllocatedEngineers);
		Assert.Equal(6, b.UsedLaboratories);
		Assert.Equal(0, b.TotalSoldiers);

		b.RemoveProduction(production);
		Assert.Empty(b.Productions);
		Assert.Equal(10, b.Engineers); // assigned engineers return to the base
	}

	[Fact]
	public void Yaml_roundtrip_of_a_base_without_facilities()
	{
		var b = OrSkip(() => new Base(null));
		OrSkip(() => b.Load(Yaml(
			"lon: 1.5\nlat: -0.5\nname: Alpha\nscientists: 10\nengineers: 7\ninBattlescape: true\n" +
			"transfers:\n  - hours: 3\n    engineers: 2\nretaliationTarget: true\nfakeUnderwater: true\n"), null, false));
		Assert.Equal(10, b.Scientists);
		Assert.Equal(7, b.Engineers);
		Assert.True(b.IsInBattlescape);
		Assert.True(b.RetaliationTarget);
		Assert.True(b.IsFakeUnderwater);
		Assert.Single(b.Transfers);
		Assert.Equal(9, b.TotalEngineers);

		var writer = new YamlRootNodeWriter();
		OrSkip(() => b.Save(writer));
		var saved = writer.ToReader();
		var keys = Keys(saved);
		string[] baseKeys = ["scientists", "engineers", "inBattlescape", "transfers", "retaliationTarget", "fakeUnderwater"];
		Assert.Equal(baseKeys, keys.Where(k => baseKeys.Contains(k)).ToList());
		Assert.True(keys.IndexOf("items") < keys.IndexOf("scientists"));

		var loaded = new Base(null);
		loaded.Load(saved, null, false);
		Assert.Equal((10, 7, 9), (loaded.Scientists, loaded.Engineers, loaded.TotalEngineers));
	}
}
