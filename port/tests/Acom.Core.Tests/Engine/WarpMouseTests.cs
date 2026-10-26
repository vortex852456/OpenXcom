namespace Acom.Tests.Engine;

// Game sets a static game pointer for State, so these tests must not run in parallel with other Game tests.
[Collection(nameof(GameTests))]
public class WarpMouseTests
{
	private sealed class MotionState : State
	{
		public List<(int x, int y, int xrel, int yrel, byte state)> Motions { get; } = [];
		public List<SDLEventType> Events { get; } = [];

		public override void Handle(Acom.Action action)
		{
			Events.Add(action.Details.type);
			if (action.Details.type == SDLEventType.SDL_MOUSEMOTION)
			{
				var m = action.Details.motion;
				Motions.Add((m.x, m.y, m.xrel, m.yrel, m.state));
			}
			base.Handle(action);
		}
	}

	private static (Game game, MotionState state) Start()
	{
		var game = new Game(new Screen(320, 200, 1));
		var state = new MotionState();
		game.PushState(state);
		game.RunFrame();
		state.Motions.Clear(); // mouse refresh of the init
		state.Events.Clear();
		return (game, state);
	}

	[Fact]
	public void Warp_moves_the_host_pointer_and_posts_a_motion_event()
	{
		var (game, state) = Start();
		var warps = new List<(int, int)>();
		game.WarpMouseHandler = (x, y) => warps.Add((x, y));
		game.PushEvent(SDL_Event.MouseButtonDown(SDLMouse.SDL_BUTTON_LEFT, 10, 20));
		SDLMouse.SDL_WarpMouse(15, 18);
		game.RunFrame();
		Assert.Equal([(15, 18)], warps);
		Assert.Equal([(15, 18, 5, -2, SDLMouse.SDL_BUTTON(SDLMouse.SDL_BUTTON_LEFT))], state.Motions);
		Assert.Equal(SDLMouse.SDL_BUTTON(SDLMouse.SDL_BUTTON_LEFT), SDLMouse.SDL_GetMouseState(out int x, out int y));
		Assert.Equal((15, 18), (x, y));
	}

	[Fact]
	public void Warp_is_clamped_to_the_window_and_dropped_without_change()
	{
		var (game, state) = Start();
		game.PushEvent(SDL_Event.MouseMotion(5, 5));
		game.WarpMouse(5, 5); // no change: no event
		game.WarpMouse(1000, 65535); // Uint16 -> Sint16: 65535 is -1
		game.RunFrame();
		Assert.Equal([(5, 5, 0, 0, (byte)0), (319, 0, 314, -5, (byte)0)], state.Motions);
	}

	[Fact]
	public void Platform_echo_of_a_warp_is_dropped_once()
	{
		var (game, state) = Start();
		game.WarpMouse(30, 40);
		game.PushEvent(SDL_Event.MouseMotion(30, 40)); // the platform reports the warp as well
		game.PushEvent(SDL_Event.MouseMotion(31, 40, 1, 0));
		game.PushEvent(SDL_Event.MouseMotion(30, 40, -1, 0));
		game.RunFrame();
		Assert.Equal([(30, 40, 30, 40, (byte)0), (31, 40, 1, 0, (byte)0), (30, 40, -1, 0, (byte)0)], state.Motions);
	}

	[Fact]
	public void Ignored_motion_events_are_not_posted_and_changing_the_state_flushes_the_queue()
	{
		var (game, state) = Start();
		game.PushEvent(SDL_Event.KeyDown(SDLKey.SDLK_a));
		Assert.Equal(SDLEvents.SDL_ENABLE, SDLEvents.SDL_EventState(SDLEventType.SDL_MOUSEMOTION, SDLEvents.SDL_IGNORE));
		// SDL 1.2 discards all pending events when the state is set
		game.PushEvent(SDL_Event.MouseMotion(7, 8));
		SDLMouse.SDL_WarpMouse(50, 60);
		game.PushEvent(SDL_Event.KeyDown(SDLKey.SDLK_b)); // other event types are still posted
		Assert.Equal(SDLEvents.SDL_IGNORE, SDLEvents.SDL_EventState(SDLEventType.SDL_MOUSEMOTION, SDLEvents.SDL_QUERY));
		game.RunFrame();
		Assert.Equal([SDLEventType.SDL_KEYDOWN], state.Events);
		Assert.Empty(state.Motions);

		Assert.Equal(SDLEvents.SDL_IGNORE, SDLEvents.SDL_EventState(SDLEventType.SDL_MOUSEMOTION, SDLEvents.SDL_ENABLE));
		// the pointer position was still tracked while ignored
		SDLMouse.SDL_GetMouseState(out int x, out int y);
		Assert.Equal((50, 60), (x, y));
		game.PushEvent(SDL_Event.MouseMotion(52, 60, 2, 0));
		game.RunFrame();
		Assert.Equal([(52, 60, 2, 0, (byte)0)], state.Motions);
	}

	[Fact]
	public void Warp_inside_an_event_handler_is_handled_in_the_same_frame()
	{
		var (game, state) = Start();
		var warper = new WarpOnKeyState();
		game.PopState();
		game.PushState(warper);
		game.RunFrame();
		warper.Motions.Clear();
		game.PushEvent(SDL_Event.KeyDown(SDLKey.SDLK_a));
		game.RunFrame();
		Assert.Equal([(100, 50)], warper.Motions);
	}

	private sealed class WarpOnKeyState : State
	{
		public List<(int x, int y)> Motions { get; } = [];

		public override void Handle(Acom.Action action)
		{
			if (action.Details.type == SDLEventType.SDL_KEYDOWN)
				SDLMouse.SDL_WarpMouse(100, 50);
			else if (action.Details.type == SDLEventType.SDL_MOUSEMOTION)
				Motions.Add((action.Details.motion.x, action.Details.motion.y));
			base.Handle(action);
		}
	}
}
