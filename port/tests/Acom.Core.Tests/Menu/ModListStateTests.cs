namespace Acom.Tests.Menu;

// Changes the global Options.mods.
[Collection(nameof(Acom.Tests.Engine.OptionsTests))]
public sealed class ModListStateTests : IDisposable
{
	private readonly List<ModToggle> _saved = Options.mods;

	public ModListStateTests()
	{
		Options.mods = [new("xcom1", true), new("a", true), new("b", false), new("c", true), new("d", false)];
	}

	public void Dispose() => Options.mods = _saved;

	private static string Order() => string.Join(",", Options.mods.Select(m => m.Id + (m.Active ? "+" : "-")));

	[Fact]
	public void MoveAbove_inserts_a_copy_above_the_target_and_removes_the_old_entry()
	{
		ModListState.MoveAbove(new ModToggle("c", true), new ModToggle("a", true));
		Assert.Equal("xcom1+,c+,a+,b-,d-", Order());
	}

	[Fact]
	public void MoveBelow_inserts_a_copy_below_the_target_and_removes_the_old_entry()
	{
		ModListState.MoveBelow(new ModToggle("a", true), new ModToggle("c", true));
		Assert.Equal("xcom1+,b-,c+,a+,d-", Order());
	}

	[Fact]
	public void Moving_to_the_ends_keeps_the_master_in_place()
	{
		// right-click on the arrows: to the first/last shown mod (the master is not shown)
		ModListState.MoveAbove(new ModToggle("d", false), new ModToggle("a", true));
		Assert.Equal("xcom1+,d-,a+,b-,c+", Order());
		ModListState.MoveBelow(new ModToggle("d", false), new ModToggle("c", true));
		Assert.Equal("xcom1+,a+,b-,c+,d-", Order());
	}

	[Fact]
	public void The_moved_copy_keeps_the_state_of_the_shown_row()
	{
		// the list holds copies: the state of the copy is what gets inserted
		ModListState.MoveAbove(new ModToggle("b", true), new ModToggle("a", true));
		Assert.Equal("xcom1+,b+,a+,c+,d-", Order());
	}
}
