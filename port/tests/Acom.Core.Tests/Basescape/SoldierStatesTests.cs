namespace Acom.Tests.Basescape;

/// <summary>Pure helpers of the soldier screens (stable sorting, transformation stat cells).</summary>
public sealed class SoldierStatesTests
{
	[Fact]
	public void StableSort_keeps_the_order_of_equal_elements()
	{
		var list = new List<(int key, string tag)> { (2, "a"), (1, "b"), (2, "c"), (1, "d"), (0, "e") };
		SoldiersState.StableSort(list, (x, y) => x.key < y.key);
		Assert.Equal(["e", "b", "d", "a", "c"], list.Select(x => x.tag));
	}

	[Fact]
	public void Descending_sort_matches_stable_sort_over_reverse_iterators()
	{
		// std::stable_sort(rbegin, rend, less): equal names keep their reversed relative order
		var list = new List<ArmorItem> { new("A1", "Alpha", ""), new("B", "Beta", ""), new("A2", "Alpha", ""), new("G", "Gamma", "") };
		list.Reverse();
		SoldiersState.StableSort(list, (a, b) => Unicode.NaturalCompare(a.name, b.name));
		list.Reverse();
		Assert.Equal(["G", "B", "A1", "A2"], list.Select(x => x.type));
	}

	[Theory]
	[InlineData(0, true, false, "")]
	[InlineData(5, true, false, "\u0001+5\u0001")]
	[InlineData(5, false, false, "\u00015\u0001")]
	[InlineData(-3, true, false, "\u0001-3\u0001")]
	[InlineData(0, true, true, "\u0001?\u0001")]
	public void FormatStat_matches_cpp(int stat, bool plus, bool hide, string expected)
	{
		Assert.Equal(expected, SoldierTransformationState.FormatStat(stat, plus, hide));
	}
}
