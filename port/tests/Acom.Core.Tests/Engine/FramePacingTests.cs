namespace Acom.Tests.Engine;

/// <summary>Frame budget of Game::run (Options::FPS/FPSInactive, SDL_Delay).</summary>
[Collection("OptionsTests")]
public class FramePacingTests
{
	[Fact]
	public void Frame_budget_follows_the_fps_options()
	{
		Options.Create();
		Options.ResetDefault(false);
		Options.FPS = 60;
		Options.FPSInactive = 30;
		Options.useOpenGL = false;
		Assert.Equal(1000.0f / 60, Game.FrameBudget(true));
		Assert.Equal(1000.0f / 30, Game.FrameBudget(false));
		// OpenGL with vsync: no limit
		Options.useOpenGL = true;
		Options.vSyncForOpenGL = true;
		Assert.Equal(0, Game.FrameBudget(true));
		Options.vSyncForOpenGL = false;
		Assert.Equal(1000.0f / 60, Game.FrameBudget(true));
		Options.FPS = 0;
		Assert.Equal(0, Game.FrameBudget(true));
	}

	[Fact]
	public void Time_until_next_frame_is_truncated_like_the_original()
	{
		float budget = 1000.0f / 60; // 16.67
		Assert.Equal(16, Game.ComputeTimeUntilNextFrame(budget, 1000, 1000));
		Assert.Equal(6, Game.ComputeTimeUntilNextFrame(budget, 1010, 1000));
		Assert.Equal(0, Game.ComputeTimeUntilNextFrame(budget, 1016, 1000));
		Assert.Equal(-3, Game.ComputeTimeUntilNextFrame(budget, 1020, 1000));
		// unsigned tick difference across the 32-bit wrap
		Assert.Equal(6, Game.ComputeTimeUntilNextFrame(budget, 5, uint.MaxValue - 4));
		Assert.Equal(0, Game.ComputeTimeUntilNextFrame(0, 1000, 0));
	}

	[Fact]
	public void Frames_are_due_after_the_budget_and_the_loop_delay_depends_on_focus()
	{
		Options.Create();
		Options.ResetDefault(false);
		Options.FPS = 60;
		Options.pauseMode = 0;
		var ticks = SDLTimer.Source;
		uint now = 5000;
		SDLTimer.Source = () => now;
		try
		{
			var game = new Game(new Screen(4, 4));
			game.SetState(new BlankState());
			game.Update();
			Assert.True(game.FrameDue); // first frame: last frame at tick 0
			Assert.True(game.Render());
			now += 10;
			game.Update();
			Assert.False(game.FrameDue);
			Assert.Equal(6, game.TimeUntilNextFrame);
			now += 7;
			game.Update();
			Assert.True(game.FrameDue);
			Assert.Equal(TimeSpan.FromMilliseconds(1), game.LoopDelay);

			// minimized with pauseMode 0: slowed, 100 ms per iteration
			game.AppState = 0;
			game.PushEvent(SDL_Event.ActiveEvent(0, SDLApp.SDL_APPACTIVE));
			game.Update();
			Assert.Equal(ApplicationState.SLOWED, game.RunningState);
			Assert.Equal(TimeSpan.FromMilliseconds(100), game.LoopDelay);
		}
		finally
		{
			SDLTimer.Source = ticks;
		}
	}

	private sealed class BlankState : State;
}
