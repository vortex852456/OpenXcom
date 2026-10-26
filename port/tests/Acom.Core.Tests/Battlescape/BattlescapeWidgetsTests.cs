using System.Reflection;

namespace Acom.Tests.Battlescape;

/// <summary>WarningMessage fade, BattlescapeMessage child layout, state disposal and helpers of the battlescape HUD.</summary>
// Game sets a static game pointer for State, so these tests must not run in parallel with the other Game tests.
[Collection(nameof(Acom.Tests.Engine.GameTests))]
public class BattlescapeWidgetsTests
{
	private static T Field<T>(object obj, string name) =>
		(T)obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(obj)!;

	[Fact]
	public void WarningMessage_fades_out_after_time_times_12_steps()
	{
		var warning = new WarningMessage(20, 10);
		Assert.False(warning.Visible);
		warning.SetColor(32);
		warning.ShowMessage("x", 2);
		Assert.True(warning.Visible);

		// _fade = 24: background starts at color + Clamp(24 - 24, 0, 12)
		warning.Draw();
		Assert.Equal(32, warning.GetPixel(0, 0));

		for (int i = 0; i < 12; ++i)
			warning.Fade();
		warning.Draw();
		Assert.Equal(32 + 12, warning.GetPixel(0, 0));

		for (int i = 0; i < 11; ++i)
			warning.Fade();
		Assert.True(warning.Visible);
		warning.Fade();
		Assert.False(warning.Visible);
	}

	[Fact]
	public void BattlescapeMessage_moves_its_parts_with_offsets()
	{
		var message = new BattlescapeMessage(320, 200, 0, 0);
		message.X = 10;
		message.Y = 30;
		Assert.Equal(10, message.X);
		Assert.Equal(30, message.Y);
		Assert.Equal(10, Field<Window>(message, "_window").X);
		Assert.Equal(30, Field<Text>(message, "_text").Y);
		Assert.Equal(210, Field<Text>(message, "_txtThinking").X);
		Assert.Equal(40, Field<Text>(message, "_txtThinking").Y);
		Assert.Equal(210, Field<ProgressBar>(message, "_progressBar").X);
		Assert.Equal(50, Field<ProgressBar>(message, "_progressBar").Y);

		message.SetHeight(100);
		Assert.Equal(100, message.Height);
		Assert.Equal(100, Field<Window>(message, "_window").Height);
		Assert.Equal(100, Field<Text>(message, "_text").Height);
	}

	private sealed class DisposableState : State, IDisposable
	{
		public int Disposed { get; private set; }
		public void Dispose() => Disposed++;
	}

	[Fact]
	public void Game_disposes_popped_states_on_the_next_cycle()
	{
		var game = new Game(new Screen(4, 4));
		var a = new State();
		var b = new DisposableState();
		game.PushState(a);
		game.PushState(b);
		game.RunFrame();
		game.PopState();
		Assert.Equal(0, b.Disposed); // C++ deletes popped states at the start of the next cycle
		game.RunFrame();
		Assert.Equal(1, b.Disposed);
		game.RunFrame();
		Assert.Equal(1, b.Disposed);
	}

	[Theory]
	[InlineData(0, "0%")]
	[InlineData(57, "57%")]
	[InlineData(-5, "-5%")]
	public void FormatPercentage_matches_cpp(int value, string expected)
	{
		Assert.Equal(expected, Unicode.FormatPercentage(value));
	}
}
