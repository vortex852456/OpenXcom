/*
 * Copyright 2010-2026 OpenXcom Developers.
 * C# port: Copyright 2026 ACOM contributors.
 *
 * This file is part of ACOM, a C# port of OpenXcom Extended.
 *
 * ACOM is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * ACOM is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Acom.  If not, see <http://www.gnu.org/licenses/>.
 */
// Ported from: src/Engine/Timer.h, src/Engine/Timer.cpp

namespace Acom;

/// <summary>Action handler of a state, called by a <see cref="Timer"/>.</summary>
public delegate void StateHandler();

/// <summary>Action handler of a surface, called by a <see cref="Timer"/>.</summary>
public delegate void SurfaceHandler();

/// <summary>
/// Timer used to run code in fixed intervals.
/// Used for code that should run at the same fixed interval
/// in various machines, based on milliseconds instead of CPU cycles.
/// </summary>
/// <remarks>
/// The handlers are bound delegates; as in C++ the state handler only runs if <see cref="Think"/>
/// gets a state and the surface handler only if it gets a surface. Pass them with a cast like in C++:
/// <c>timer.OnTimer((StateHandler)Animate)</c>.
/// </remarks>
public sealed class Timer
{
	private const int accurate = 4;
	private static bool s_slowTickInit;
	private static ulong s_oldTime, s_falseTime;

	private uint _start;
	private uint _frameSkipStart;
	private int _interval;
	private bool _running;
	private readonly bool _frameSkipping;
	private StateHandler? _state;
	private SurfaceHandler? _surface;

	/// <summary>Maximum number of state handler calls to catch up per think (frame skipping).</summary>
	public static int MaxFrameSkip { get; set; } = 8; // this is a pretty good default at 60FPS.

	/// <summary>Slow-down factor of the game time (1 = normal speed).</summary>
	public static uint GameSlowSpeed { get; set; } = 1;

	/// <summary>Initializes a new, stopped timer with a set interval.</summary>
	/// <param name="interval">Time interval in milliseconds.</param>
	/// <param name="frameSkipping">Use frameskipping.</param>
	public Timer(uint interval, bool frameSkipping = false)
	{
		_interval = (int)interval;
		_frameSkipping = frameSkipping;
		MaxFrameSkip = Options.maxFrameSkip;
	}

	/// <summary>
	/// Game time in milliseconds: real time divided by <see cref="GameSlowSpeed"/>, with 4 extra bits of precision.
	/// </summary>
	/// <remarks>
	/// Deviation: the C++ code mixes shifted and unshifted values on its first call (a one-time jump)
	/// and keeps the shifted old time in 32 bits (wraps after 74 hours); here both stay consistent.
	/// </remarks>
	private static uint SlowTick()
	{
		ulong newTime = (ulong)SDLTimer.SDL_GetTicks() << accurate;
		if (!s_slowTickInit)
		{
			s_oldTime = s_falseTime = newTime;
			s_slowTickInit = true;
		}
		s_falseTime += (newTime - s_oldTime) / GameSlowSpeed;
		s_oldTime = newTime;
		return (uint)(s_falseTime >> accurate);
	}

	/// <summary>Restarts the game time from the current clock (for tests that replace <see cref="SDLTimer.Source"/>).</summary>
	internal static void ResetSlowTick() => s_slowTickInit = false;

	/// <summary>Starts the timer running and counting time.</summary>
	public void Start()
	{
		_frameSkipStart = _start = SlowTick();
		_running = true;
	}

	/// <summary>Stops the timer from running.</summary>
	public void Stop()
	{
		_start = 0;
		_running = false;
	}

	/// <summary>Gets the time passed since the last interval, in milliseconds.</summary>
	public uint Time => _running ? SlowTick() - _start : 0;

	/// <summary>Gets if the timer has been started.</summary>
	public bool IsRunning => _running;

	/// <summary>
	/// The timer keeps calculating the passed time while it's running,
	/// calling the respective action handler whenever the set interval passes.
	/// </summary>
	/// <param name="state">State that the action handler belongs to.</param>
	/// <param name="surface">Surface that the action handler belongs to.</param>
	public void Think(State? state, Surface? surface)
	{
		long now = SlowTick(); // must be signed to permit negative numbers
		// this is used to make sure we stop calling the state handler once the state has been popped
		var game = state != null ? State.GamePtr : null;
		if (_running)
		{
			if (now - _frameSkipStart >= _interval)
			{
				for (int i = 0; i <= MaxFrameSkip && IsRunning && now - _frameSkipStart >= _interval; ++i)
				{
					if (state != null && _state != null)
						_state();
					_frameSkipStart += (uint)_interval;
					// breaking here after one iteration effectively returns this function to its old functionality:
					if (game == null || !_frameSkipping || !game.IsState(state!))
						break; // if game isn't set, we can't verify the state
				}
				if (_running && surface != null && _surface != null)
					_surface();
				_start = SlowTick();
				if (_start > _frameSkipStart)
					_frameSkipStart = _start; // don't play animations in ffwd to catch up :P
			}
		}
	}

	/// <summary>Changes the timer's interval to a new value in milliseconds.</summary>
	public void SetInterval(uint interval) => _interval = (int)interval;

	/// <summary>Sets a state function for the timer to call every interval.</summary>
	public void OnTimer(StateHandler? handler) => _state = handler;

	/// <summary>Sets a surface function for the timer to call every interval.</summary>
	public void OnTimer(SurfaceHandler? handler) => _surface = handler;
}
