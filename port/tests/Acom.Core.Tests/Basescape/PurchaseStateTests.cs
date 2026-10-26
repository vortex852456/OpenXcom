#nullable disable // null rule pointers as in the C++ code
using static Acom.Tests.Savegame.SavegameTestHelpers;

namespace Acom.Tests.Basescape;

/// <summary>
/// Cost/space computations of the Purchase, Sell and Transfer screens. Expected values come from the
/// C++ expressions compiled with g++ (std::ostream / printf formatting, int truncation, trig).
/// </summary>
public class PurchaseStateTests
{
	[Theory]
	[InlineData(12.5, 0.0, 50, "12.5:50")]
	[InlineData(12.5, 0.04, 50, "12.5:50")]           // |change| <= 0.05 is not shown
	[InlineData(12.5, 2.25, 50, "12.5(+2.2):50")]     // printf ties to even, .NET "F1" would give 2.3
	[InlineData(7.0, -1.05, 10, "7(-1.1):10")]        // -1.05 is slightly below -1.05 in binary
	[InlineData(1234567.0, 0.35, 2000000, "1.23457e+06(+0.3):2000000")]
	public void Space_used_text_matches_cpp_ostream(double used, double change, int available, string expected)
	{
		Assert.Equal(expected, PurchaseState.FormatSpaceUsed(used, change, available));
	}

	[Theory]
	[InlineData(2.25, "2.2")]
	[InlineData(0.25, "0.2")]
	[InlineData(0.75, "0.8")]
	[InlineData(-2.25, "-2.2")]
	[InlineData(1.05, "1.1")]
	[InlineData(0.35, "0.3")]
	[InlineData(-0.01, "-0.0")]
	[InlineData(99.96, "100.0")]
	public void Fixed_one_decimal_matches_glibc_printf(double value, string expected)
	{
		Assert.Equal(expected, PurchaseState.FormatFixed1(value));
	}

	[Theory]
	[InlineData(10.0, 0.3, 33)]                 // (10 + 0.05) / 0.3 = 33.5 -> 33
	[InlineData(-1.0, 0.5, -1)]                 // over the limit: C++ yields a negative change too
	[InlineData(10.0, 0.0, int.MaxValue)]       // size 0 never limits
	[InlineData(10.0, -1.0, int.MaxValue)]      // negative size never limits
	[InlineData(1e300, 1e-300, int.MaxValue)]   // UB in C++, clamped here
	public void Max_items_by_free_stores(double freeStores, double size, int expected)
	{
		Assert.Equal(expected, PurchaseState.MaxByStores(freeStores, size));
	}

	[Fact]
	public void Map_reads_insert_defaults_like_cpp_operator_brackets()
	{
		var log = new SortedDictionary<string, int>(StringComparer.Ordinal);
		Assert.Equal(0, PurchaseState.MapGet(log, "STR_X"));
		Assert.True(log.ContainsKey("STR_X"));
		log["STR_X"] = 3;
		Assert.Equal(3, PurchaseState.MapGet(log, "STR_X"));

		var used = new SortedDictionary<string, bool>(StringComparer.Ordinal);
		Assert.False(PurchaseState.MapGetBool(used, "STR_Y"));
		Assert.True(used.ContainsKey("STR_Y"));
	}
}

public class TransferItemsStateTests
{
	private static Base MakeBase(double lat, double lon)
	{
		var b = Uninitialized<Base>();
		b.Latitude = lat;
		b.Longitude = lon;
		return b;
	}

	[Fact]
	public void Distance_time_and_costs_match_cpp()
	{
		double d = TransferItemsState.GetDistance(MakeBase(0.5, 1.0), MakeBase(-0.3, 2.5));
		Assert.Equal(75.331011140616511, d, 12);
		Assert.Equal(13, TransferItemsState.TransferTime(d));
		Assert.Equal(376, (int)(5 * d));   // personnel cost
		Assert.Equal(1883, (int)(25 * d)); // craft cost
		Assert.Equal(75, (int)(1 * d));    // item cost

		Assert.Equal(0.0, TransferItemsState.GetDistance(MakeBase(0.1, 0.2), MakeBase(0.1, 0.2)));
		Assert.Equal(6, TransferItemsState.TransferTime(0.0));
	}

	[Theory]
	[InlineData(100, 99.0, 100, 50.0, 1.0, false)]    // destination still fits exactly
	[InlineData(100, 99.0, 100, 50.0, 1.5, true)]     // destination overflows
	[InlineData(100, 99.0, 50, 49.5, -0.4, false)]    // negative size: source gets the extra space
	[InlineData(100, 99.0, 50, 49.5, -0.6, true)]     // negative size: source overflows
	public void Final_store_check_looks_at_the_base_losing_space(int availTo, double usedTo, int availFrom, double usedFrom, double iQty, bool exceeded)
	{
		Assert.Equal(exceeded, TransferItemsState.StoresExceeded(availTo, usedTo, availFrom, usedFrom, iQty));
	}
}

public class SellStateTests
{
	private static TransferRow Row(int id, int cost, long totalCost, double size, double totalSize, int listOrder)
		=> new() { name = id.ToString(System.Globalization.CultureInfo.InvariantCulture), cost = cost, totalCost = totalCost, size = size, totalSize = totalSize, listOrder = listOrder };

	private static string Names(List<TransferRow> rows) => string.Join(",", rows.Select(r => r.name));

	[Fact]
	public void Sorting_is_stable_and_descending_except_list_order()
	{
		var rows = new List<TransferRow>
		{
			Row(0, 10, 100, 1.0, 5.0, 3),
			Row(1, 20, 100, 0.5, 5.0, 1),
			Row(2, 10, 300, 1.0, 1.0, 2),
			Row(3, 30, 50, 2.0, 2.0, 1),
		};
		SellState.SortRows(rows, TransferSortDirection.BY_TOTAL_COST);
		Assert.Equal("2,0,1,3", Names(rows));
		SellState.SortRows(rows, TransferSortDirection.BY_UNIT_COST);
		Assert.Equal("3,1,2,0", Names(rows));
		SellState.SortRows(rows, TransferSortDirection.BY_TOTAL_SIZE);
		Assert.Equal("1,0,3,2", Names(rows));
		SellState.SortRows(rows, TransferSortDirection.BY_UNIT_SIZE);
		Assert.Equal("3,0,2,1", Names(rows));
		SellState.SortRows(rows, TransferSortDirection.BY_LIST_ORDER);
		Assert.Equal("3,1,2,0", Names(rows));
	}

	[Fact]
	public void Clean_up_container_returns_the_remaining_amount()
	{
		var rule = Uninitialized<RuleItem>();
		var c = new ItemContainer();
		c.AddItem(rule, 5);
		Assert.Equal(0, SellState.CleanUpContainer(c, rule, 3));
		Assert.Equal(2, c.GetItem(rule));
		Assert.Equal(4, SellState.CleanUpContainer(c, rule, 6));
		Assert.Equal(0, c.GetItem(rule));
		Assert.Equal(1, SellState.CleanUpContainer(c, rule, 1));
	}
}

public class StoresStateTests
{
	private static StoredItem Item(string name, int qty, double size) => new(null, name, qty, size, qty * size);

	private static string Names(List<StoredItem> items) => string.Join(",", items.Select(i => i.name));

	[Fact]
	public void Sorting_matches_cpp_stable_sort_and_reverse_range()
	{
		var items = new List<StoredItem> { Item("Item10", 2, 1.0), Item("Item2", 5, 0.5), Item("A", 2, 3.0), Item("B", 5, 1.0) };

		StoresState.SortItems(items, ItemSort.ITEM_SORT_NAME_ASC);
		Assert.Equal("A,B,Item2,Item10", Names(items)); // natural order
		StoresState.SortItems(items, ItemSort.ITEM_SORT_NAME_DESC);
		Assert.Equal("Item10,Item2,B,A", Names(items));

		// stable_sort over reverse iterators == stable descending sort: ties keep their order
		StoresState.SortItems(items, ItemSort.ITEM_SORT_QUANTITY_DESC);
		Assert.Equal("Item2,B,Item10,A", Names(items));
		StoresState.SortItems(items, ItemSort.ITEM_SORT_QUANTITY_ASC);
		Assert.Equal("Item10,A,Item2,B", Names(items));
		StoresState.SortItems(items, ItemSort.ITEM_SORT_SIZE_ASC);
		Assert.Equal("Item2,Item10,B,A", Names(items));
		StoresState.SortItems(items, ItemSort.ITEM_SORT_SPACE_USED_DESC);
		Assert.Equal("A,B,Item2,Item10", Names(items));

		StoresState.SortItems(items, ItemSort.ITEM_SORT_NONE);
		Assert.Equal("A,B,Item2,Item10", Names(items));
	}
}

public class CraftEquipmentStateTests
{
	[Theory]
	[InlineData(5, 3, false, true, "3/+2")]
	[InlineData(3, 3, false, true, "3")]
	[InlineData(1, 3, false, true, "1/-2")]
	[InlineData(1, 3, true, true, "1")]
	[InlineData(1, 3, false, false, "1")]
	public void Craft_quantity_column(int cQty, int reserved, bool vehicle, bool alternate, string expected)
	{
		Assert.Equal(expected, CraftEquipmentState.FormatCraftQuantity(cQty, reserved, vehicle, alternate));
	}

	[Theory]
	[InlineData(8.0, 7.5, 0.2, 10, 2)]   // floor((8 + 0.05 - 7.5) / 0.2) = 2
	[InlineData(8.0, 7.5, 0.2, 2, 2)]    // fits: unchanged
	[InlineData(8.0, 9.0, 0.2, 3, 0)]    // already over the limit: clamped to 0
	[InlineData(8.0, 9.0, 0.0, 3, 3)]    // size 0 always fits
	public void Max_items_by_craft_storage(double max, double used, double size, int change, int expected)
	{
		Assert.Equal(expected, CraftEquipmentState.MaxByCraftStorage(max, used, size, change));
	}

	[Fact]
	public void Stable_sort_keeps_equal_elements_in_order()
	{
		var list = new List<(int key, int id)> { (2, 0), (1, 1), (2, 2), (0, 3), (1, 4), (2, 5), (0, 6) };
		CraftSoldiersState.StableSort(list, (a, b) => a.key < b.key);
		Assert.Equal([(0, 3), (0, 6), (1, 1), (1, 4), (2, 0), (2, 2), (2, 5)], list);

		var empty = new List<int>();
		CraftSoldiersState.StableSort(empty, (a, b) => a < b);
		Assert.Empty(empty);
	}

	[Fact]
	public void Pilot_bonus_text()
	{
		Assert.Equal("+5%", CraftPilotsState.FormatBonus(5));
		Assert.Equal("0%", CraftPilotsState.FormatBonus(0));
		Assert.Equal("-3%", CraftPilotsState.FormatBonus(-3));
	}
}
