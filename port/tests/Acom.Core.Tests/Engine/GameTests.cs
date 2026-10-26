
namespace Acom.Tests.Engine;

// Game sets a static game pointer for State, so these tests must not run in parallel with each other.
[Collection(nameof(GameTests))]
public class GameTests
{
	private sealed class RecordingState(List<string> log, string name, byte color) : State
	{
		public System.Action? OnKey { get; set; }

		public override void Init()
		{
			base.Init();
			log.Add(name + ".init");
		}

		public override void Think() => log.Add(name + ".think");

		public override void Handle(Acom.Action action)
		{
			log.Add($"{name}.{action.Details.type}");
			if (action.Details.type == SDLEventType.SDL_KEYDOWN)
				OnKey?.Invoke();
			base.Handle(action);
		}

		public override void Blit()
		{
			log.Add(name + ".blit");
			_game.Screen.Surface.SetPixel(0, 0, color);
		}

		public void MakeOverlay() => ToggleScreen();
	}

	[Fact]
	public void New_state_is_initialized_before_further_events()
	{
		var log = new List<string>();
		var game = new Game(new Screen(4, 4));
		var a = new RecordingState(log, "a", 1);
		var b = new RecordingState(log, "b", 2);
		game.PushState(a);
		game.RunFrame();
		log.Clear();

		game.PushEvent(SDL_Event.KeyDown(SDLKey.SDLK_a));
		game.PushEvent(SDL_Event.KeyDown(SDLKey.SDLK_b));
		a.OnKey = () => game.PushState(b);
		// first event pushes b -> loop breaks, second event waits for b to be initialized
		game.RunFrame();
		Assert.Equal(["a.SDL_KEYDOWN", "b.think"], log);
		log.Clear();
		game.RunFrame();
		Assert.Equal(["b.init", "b.SDL_MOUSEMOTION", "b.SDL_KEYDOWN", "b.think", "b.blit"], log);
	}

	[Fact]
	public void Rendering_starts_at_topmost_full_screen_state()
	{
		var log = new List<string>();
		var game = new Game(new Screen(4, 4));
		game.PushState(new RecordingState(log, "bottom", 1));
		game.PushState(new RecordingState(log, "screen", 2));
		var overlay = new RecordingState(log, "overlay", 3);
		overlay.MakeOverlay();
		game.PushState(overlay);
		Assert.True(game.RunFrame());
		Assert.Equal(["overlay.init", "overlay.SDL_MOUSEMOTION", "overlay.think", "screen.blit", "overlay.blit"], log);
		Assert.Equal(1, game.Screen.FrameNumber);
	}

	[Fact]
	public void PopState_returns_to_previous_state_and_reinitializes_it()
	{
		var log = new List<string>();
		var game = new Game(new Screen(4, 4));
		var a = new RecordingState(log, "a", 1);
		game.PushState(a);
		game.PushState(new RecordingState(log, "b", 2));
		game.RunFrame();
		game.PopState();
		Assert.True(game.IsState(a));
		log.Clear();
		game.RunFrame();
		Assert.Equal("a.init", log[0]);
	}

	[Fact]
	public void Quit_event_stops_the_loop()
	{
		var game = new Game(new Screen(4, 4));
		game.PushState(new RecordingState([], "a", 1));
		game.PushEvent(SDL_Event.Quit());
		Assert.False(game.RunFrame());
		Assert.True(game.IsQuitting);
	}

	[Fact]
	public void Volume_exponent_maps_linear_to_exponential_levels()
	{
		Assert.Equal(0.0, Game.VolumeExponent(0));
		Assert.Equal(1.0, Game.VolumeExponent(Game.SDL_MIX_MAXVOLUME), 12);
		Assert.Equal((Math.Sqrt(11) - 1) / 10, Game.VolumeExponent(64), 12);
	}

	[Fact]
	public void Without_audio_output_the_game_runs_muted()
	{
		var game = new Game(new Screen(4, 4));
		Assert.True(Options.mute); // like a missing sound device
		game.SetVolume(10, 10, 10); // muted: leaves the mixer alone
		Assert.Equal(0, Mixer.QuerySpec(out _, out _, out _));
	}

	[Fact]
	public void Flip_converts_palette_indices_to_rgba()
	{
		var screen = new Screen(2, 1, scale: 2);
		screen.SetPalette([new Rgba(0, 0, 0), new Rgba(10, 20, 30)]);
		screen.Surface.SetPixel(1, 0, 1);
		screen.Flip();
		Assert.Equal(2 * 2 * 2 * 4, screen.Frame.Length);
		Assert.Equal([10, 20, 30, 255], screen.Frame.Slice(2 * 4, 4).ToArray());
		Assert.Equal([0, 0, 0, 255], screen.Frame[..4].ToArray());
	}
}
