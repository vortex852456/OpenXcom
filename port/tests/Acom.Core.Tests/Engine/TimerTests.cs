using Timer = Acom.Timer;

namespace Acom.Tests.Engine;

// Replaces the static clock and uses the static game pointer of State.
[Collection(nameof(GameTests))]
public sealed class TimerTests : IDisposable
{
	private sealed class EmptyState : State
	{
	}

	private readonly Func<uint> _oldSource = SDLTimer.Source;
	private uint _now = 5000;
	private int _stateCalls, _surfaceCalls;

	public TimerTests()
	{
		SDLTimer.Source = () => _now;
		Timer.ResetSlowTick();
		Options.maxFrameSkip = 8;
	}

	public void Dispose()
	{
		SDLTimer.Source = _oldSource;
		Timer.ResetSlowTick();
		Timer.GameSlowSpeed = 1;
	}

	private Timer Create(uint interval, bool frameSkipping = false)
	{
		var timer = new Timer(interval, frameSkipping);
		timer.OnTimer((StateHandler)(() => _stateCalls++));
		timer.OnTimer((SurfaceHandler)(() => _surfaceCalls++));
		return timer;
	}

	[Fact]
	public void Calls_handlers_once_per_interval()
	{
		var timer = Create(100);
		var state = new EmptyState();
		var surface = new Surface(1, 1);
		timer.Think(state, surface);
		Assert.Equal((0, 0), (_stateCalls, _surfaceCalls)); // not started
		timer.Start();
		_now += 99;
		timer.Think(state, surface);
		Assert.Equal((0, 0), (_stateCalls, _surfaceCalls));
		_now += 1;
		timer.Think(state, surface);
		Assert.Equal((1, 1), (_stateCalls, _surfaceCalls));
		// without frame skipping a late think runs once and does not catch up
		_now += 350;
		timer.Think(state, surface);
		Assert.Equal((2, 2), (_stateCalls, _surfaceCalls));
		_now += 99;
		timer.Think(state, surface);
		Assert.Equal(2, _stateCalls);
		Assert.Equal(99u, timer.Time);
	}

	[Fact]
	public void State_handler_needs_a_state_and_surface_handler_a_surface()
	{
		var timer = Create(10);
		timer.Start();
		_now += 10;
		timer.Think(null, new Surface(1, 1));
		Assert.Equal((0, 1), (_stateCalls, _surfaceCalls));
		_now += 10;
		timer.Think(new EmptyState(), null);
		Assert.Equal((1, 1), (_stateCalls, _surfaceCalls));
	}

	[Fact]
	public void Frame_skipping_catches_up_for_the_active_state()
	{
		var game = new Game(new Screen(4, 4));
		var state = new EmptyState();
		game.PushState(state);
		var timer = Create(100, frameSkipping: true);
		timer.Start();
		_now += 350;
		timer.Think(state, null);
		Assert.Equal(3, _stateCalls);
		// at most maxFrameSkip + 1 calls
		_now += 100 * 20;
		timer.Think(state, null);
		Assert.Equal(3 + 9, _stateCalls);
		// an inactive state is called once only
		var top = new EmptyState();
		game.PushState(top);
		_now += 300;
		timer.Think(state, null);
		Assert.Equal(13, _stateCalls);
	}

	[Fact]
	public void Slow_speed_stretches_game_time_and_stop_resets()
	{
		Timer.GameSlowSpeed = 4;
		var timer = Create(100);
		timer.Start();
		_now += 200;
		Assert.Equal(50u, timer.Time);
		Assert.True(timer.IsRunning);
		timer.Stop();
		Assert.False(timer.IsRunning);
		Assert.Equal(0u, timer.Time);
	}
}
