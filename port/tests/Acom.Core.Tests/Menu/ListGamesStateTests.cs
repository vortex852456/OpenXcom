namespace Acom.Tests.Menu;

public class ListGamesStateTests
{
	private static SaveInfo Save(string name, long timestamp, bool reserved = false)
		=> new() { fileName = name + ".sav", displayName = name, timestamp = timestamp, reserved = reserved };

	private static List<SaveInfo> Saves() =>
	[
		Save("Save 10", 300),
		Save("[Autosave]", 500, reserved: true),
		Save("save 2", 100),
		Save("Alpha", 200),
		Save("[Quicksave]", 50, reserved: true),
	];

	private static string[] Names(List<SaveInfo> saves) => saves.Select(s => s.displayName).ToArray();

	[Fact]
	public void Name_ascending_puts_reserved_first_and_uses_natural_order()
	{
		var saves = Saves();
		ListGamesState.SortSaves(saves, SaveSort.SORT_NAME_ASC);
		Assert.Equal(["[Autosave]", "[Quicksave]", "Alpha", "save 2", "Save 10"], Names(saves));
	}

	[Fact]
	public void Name_descending_keeps_reserved_first()
	{
		var saves = Saves();
		ListGamesState.SortSaves(saves, SaveSort.SORT_NAME_DESC);
		Assert.Equal(["[Quicksave]", "[Autosave]", "Save 10", "save 2", "Alpha"], Names(saves));
	}

	[Fact]
	public void Date_sorting_orders_by_timestamp_with_reserved_first()
	{
		var saves = Saves();
		ListGamesState.SortSaves(saves, SaveSort.SORT_DATE_ASC);
		Assert.Equal(["[Quicksave]", "[Autosave]", "save 2", "Alpha", "Save 10"], Names(saves));
		ListGamesState.SortSaves(saves, SaveSort.SORT_DATE_DESC);
		Assert.Equal(["[Autosave]", "[Quicksave]", "Save 10", "Alpha", "save 2"], Names(saves));
	}

	[Fact]
	public void Comparers_match_the_cpp_functors()
	{
		var reserved = Save("Z", 9, reserved: true);
		var normal = Save("A", 1);
		Assert.True(ListGamesState.CompareSaveName(reserved, normal, false));
		Assert.False(ListGamesState.CompareSaveName(normal, reserved, false));
		// reverse functor: the reserved save is "greater", so it ends up first of the reversed range
		Assert.True(ListGamesState.CompareSaveName(normal, reserved, true));
		Assert.True(ListGamesState.CompareSaveTimestamp(normal, Save("B", 2), false));
		Assert.False(ListGamesState.CompareSaveTimestamp(normal, Save("B", 1), false));
	}

	[Theory]
	[InlineData("My: save/1?", "My_ save_1_")]
	[InlineData("a<b>c*d|e\"f'g\\h", "a_b_c_d_e_f_g_h")]
	[InlineData("plain name", "plain name")]
	public void Save_names_are_sanitized_like_cpp(string name, string expected)
	{
		Assert.Equal(expected, CrossPlatform.SanitizeFilename(name));
	}
}
