namespace Acom.Tests.Geoscape;

/// <summary>Deterministic helpers of the Geoscape popup states and the State destructor hook they rely on.</summary>
// Game sets a static game pointer for State, so these tests must not run in parallel with the other Game tests.
[Collection(nameof(Acom.Tests.Engine.GameTests))]
public class GeoscapePopupsTests
{
	private sealed class DestroyRecordingState(List<string> log, string name) : State
	{
		public override void Destroy()
		{
			log.Add(name + ".destroy");
			base.Destroy();
		}
	}

	[Fact]
	public void Popped_state_is_destroyed_at_start_of_next_frame_like_cpp_delete()
	{
		var log = new List<string>();
		var game = new Game(new Screen(4, 4));
		game.PushState(new DestroyRecordingState(log, "a"));
		game.PushState(new DestroyRecordingState(log, "b"));
		game.RunFrame();

		game.PopState();
		Assert.Empty(log); // C++ only queues the state in _deleted

		game.RunFrame();
		Assert.Equal(["b.destroy"], log);

		game.RunFrame();
		Assert.Equal(["b.destroy"], log); // destroyed only once
	}

	[Fact]
	public void SetState_destroys_all_previous_states_in_pop_order()
	{
		var log = new List<string>();
		var game = new Game(new Screen(4, 4));
		game.PushState(new DestroyRecordingState(log, "a"));
		game.PushState(new DestroyRecordingState(log, "b"));
		game.RunFrame();

		game.SetState(new DestroyRecordingState(log, "c"));
		game.RunFrame();
		Assert.Equal(["b.destroy", "a.destroy"], log);
	}

	[Fact]
	public void Intercept_craft_list_sort_is_stable_like_std_stable_sort()
	{
		List<(string craft, double distance, string xbase)> list =
		[
			("c1", 3.0, "b1"),
			("c2", 1.0, "b1"),
			("c3", 3.0, "b2"),
			("c4", 0.5, "b2"),
			("c5", 1.0, "b3"),
		];
		InterceptState.StableSortByDistance(list);
		Assert.Equal(["c4", "c2", "c5", "c1", "c3"], list.Select(t => t.craft));
	}
}
