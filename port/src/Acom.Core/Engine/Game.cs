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
// Ported from: src/Engine/Game.h, src/Engine/Game.cpp
// Port status: partial (state machine and frame loop; missing: Mod, SavedGame, Language, Cursor, FpsCounter, audio, debug shortcuts).

namespace Acom.Engine;

/// <summary>
/// The core of the game engine, manages the game's entire contents and structure.
/// Contains a stack state machine to handle all the initializations, events and
/// blits of each state, as well as transitions.
/// </summary>
/// <remarks>
/// The C++ version owns the main loop. Here the platform host (MonoGame) owns it: it queues
/// input with <see cref="PushEvent"/>, calls <see cref="Update"/> every tick and <see cref="Render"/>
/// whenever a frame is drawn. As in the C++ loop, logic runs every iteration while blitting may be
/// skipped; frame pacing (Options::FPS) is the host's job.
/// </remarks>
public sealed class Game
{
	private readonly Screen _screen;
	private readonly List<State> _states = [];
	private readonly List<State> _deleted = [];
	private readonly Queue<SDL_Event> _events = new();
	private bool _quit, _init, _mouseActive = true;
	private bool _ctrl, _alt, _shift, _rmb, _mmb;
	private int _mouseX, _mouseY;

	/// <summary>Starts up the engine with the given screen.</summary>
	public Game(Screen screen)
	{
		ArgumentNullException.ThrowIfNull(screen);
		_screen = screen;
		State.SetGamePtr(this);
	}

	/// <summary>The display screen used by the game.</summary>
	public Screen Screen => _screen;

	/// <summary>Bit mask of currently pressed mouse buttons (replaces SDL_GetMouseState).</summary>
	public byte MouseButtonState { get; private set; }

	/// <summary>Currently pressed modifier keys (replaces SDL_GetModState).</summary>
	public SDLMod ModState { get; set; }

	/// <summary>The state stack, bottom first.</summary>
	public IReadOnlyList<State> States => _states;

	/// <summary>Queues an input or window event for the next frame.</summary>
	public void PushEvent(SDL_Event ev) => _events.Enqueue(ev);

	/// <summary>Runs one full iteration of the original main loop: <see cref="Update"/> then <see cref="Render"/>.</summary>
	/// <returns>True if a new frame was flipped to the screen.</returns>
	public bool RunFrame()
	{
		Update();
		return Render();
	}

	/// <summary>
	/// Logic part of the original main loop: cleans up popped states, initializes the
	/// active state, processes queued events and lets the active state think.
	/// </summary>
	public void Update()
	{
		if (_quit)
			return;

		// Clean up states
		_deleted.Clear();

		if (_states.Count == 0)
			return;

		// Initialize active state
		if (!_init)
		{
			_init = true;
			_states[^1].Init();

			// Unpress buttons
			_states[^1].ResetAll();

			// Refresh mouse position
			var motion = SDL_Event.MouseMotion(_mouseX, _mouseY);
			_states[^1].Handle(CreateAction(motion));
		}

		// Process events
		while (_events.TryDequeue(out var ev))
		{
			switch (ev.type)
			{
				case SDLEventType.SDL_QUIT:
					Quit();
					break;
				case SDLEventType.SDL_MOUSEMOTION:
				case SDLEventType.SDL_MOUSEBUTTONDOWN:
				case SDLEventType.SDL_MOUSEBUTTONUP:
					TrackMouse(ev);
					// Skip mouse events if they're disabled
					if (!_mouseActive)
						continue;
					_states[^1].Handle(CreateAction(ev));
					break;
				case SDLEventType.SDL_KEYDOWN:
				case SDLEventType.SDL_KEYUP:
					ModState = ev.key.keysym.mod;
					_states[^1].Handle(CreateAction(ev));
					break;
				default:
					_states[^1].Handle(CreateAction(ev));
					break;
			}
			if (!_init || _quit)
			{
				// States stack was changed, break the loop so new state
				// can be initialized before processing new events
				break;
			}
		}

		if (_quit || _states.Count == 0)
			return;

		// Process logic
		_states[^1].Think();
	}

	/// <summary>Rendering part of the original main loop: blits the visible states and flips the screen.</summary>
	/// <returns>True if a new frame was flipped to the screen.</returns>
	public bool Render()
	{
		if (_quit || !_init || _states.Count == 0)
			return false;

		// Process rendering: start at the topmost full-screen state
		_screen.Clear();
		int first = _states.Count - 1;
		while (first > 0 && !_states[first].IsScreen)
			--first;
		for (int i = first; i < _states.Count; ++i)
			_states[i].Blit();
		_screen.Flip();
		return true;
	}

	private Action CreateAction(SDL_Event ev)
	{
		return new Action(ev, _screen.XScale, _screen.YScale, _screen.CursorTopBlackBand, _screen.CursorLeftBlackBand);
	}

	private void TrackMouse(SDL_Event ev)
	{
		switch (ev.type)
		{
			case SDLEventType.SDL_MOUSEMOTION:
				_mouseX = ev.motion.x;
				_mouseY = ev.motion.y;
				break;
			case SDLEventType.SDL_MOUSEBUTTONDOWN:
				_mouseX = ev.button.x;
				_mouseY = ev.button.y;
				MouseButtonState |= SDLMouse.SDL_BUTTON(ev.button.button);
				break;
			case SDLEventType.SDL_MOUSEBUTTONUP:
				_mouseX = ev.button.x;
				_mouseY = ev.button.y;
				MouseButtonState &= (byte)~SDLMouse.SDL_BUTTON(ev.button.button);
				break;
		}
	}

	/// <summary>Stops the state machine and the game is shut down.</summary>
	public void Quit() => _quit = true;

	/// <summary>Is the game shutting down?</summary>
	public bool IsQuitting => _quit;

	/// <summary>Pops all the states currently in stack and pushes in the new state.</summary>
	public void SetState(State state)
	{
		while (_states.Count > 0)
			PopState();
		PushState(state);
		_init = false;
	}

	/// <summary>Pushes a new state into the top of the stack; it is initialized on the next cycle.</summary>
	public void PushState(State state)
	{
		ArgumentNullException.ThrowIfNull(state);
		_states.Add(state);
		_init = false;
	}

	/// <summary>
	/// Pops the last state from the top of the stack. Since states can't actually be
	/// deleted mid-cycle, it's moved into a separate queue which is cleared at the start of
	/// every cycle, so the transition is seamless.
	/// </summary>
	public void PopState()
	{
		_deleted.Add(_states[^1]);
		_states.RemoveAt(_states.Count - 1);
		_init = false;
	}

	/// <summary>Is <paramref name="state"/> the current (topmost) state?</summary>
	public bool IsState(State state) => _states.Count > 0 && _states[^1] == state;

	/// <summary>Sets whether mouse events are processed.</summary>
	public void SetMouseActive(bool active) => _mouseActive = active;

	public bool IsCtrlPressed(bool considerTouchButtons = false) => (considerTouchButtons && _ctrl) || (ModState & SDLMod.KMOD_CTRL) != 0;

	public bool IsAltPressed(bool considerTouchButtons = false) => (considerTouchButtons && _alt) || (ModState & SDLMod.KMOD_ALT) != 0;

	public bool IsShiftPressed(bool considerTouchButtons = false) => (considerTouchButtons && _shift) || (ModState & SDLMod.KMOD_SHIFT) != 0;

	public bool IsLeftClick(Action action, bool considerTouchButtons = false)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (considerTouchButtons)
			return action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT && !_rmb && !_mmb;
		return action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT;
	}

	public bool IsRightClick(Action action, bool considerTouchButtons = false)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (considerTouchButtons && action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT && _rmb)
			return true;
		return action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT;
	}

	public bool IsMiddleClick(Action action, bool considerTouchButtons = false)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (considerTouchButtons && action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT && _mmb)
			return true;
		return action.Details.button.button == SDLMouse.SDL_BUTTON_MIDDLE;
	}

	/// <summary>Touch-button emulation flags (mobile UI).</summary>
	public bool CtrlPressedFlag { get => _ctrl; set => _ctrl = value; }

	public bool AltPressedFlag { get => _alt; set => _alt = value; }

	public bool ShiftPressedFlag { get => _shift; set => _shift = value; }

	public bool RMBFlag { get => _rmb; set => _rmb = value; }

	public bool MMBFlag { get => _mmb; set => _mmb = value; }
}
