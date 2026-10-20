/*
 * Copyright 2010-2026 OpenXcom Developers.
 * C# port: Copyright 2026 Bulwark contributors.
 *
 * This file is part of Bulwark, a C# port of OpenXcom Extended.
 *
 * Bulwark is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Bulwark is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Bulwark.  If not, see <http://www.gnu.org/licenses/>.
 */
// Ported from: src/Engine/InteractiveSurface.h, src/Engine/InteractiveSurface.cpp

namespace Bulwark.Engine;

/// <summary>Handler for user actions; in C++ a pointer to a State member function.</summary>
public delegate void ActionHandler(Action action);

/// <summary>
/// Surface that the user can interact with.
/// Specialized version of the standard Surface that processes all the various
/// input events and turns them into useful interactions with the Surface,
/// so specialized subclasses don't need to worry about it.
/// </summary>
public class InteractiveSurface : Surface
{
	private const int NUM_BUTTONS = 7;

	/// <summary>Unused keycode representing "any key".</summary>
	public const SDLKey SDLK_ANY = (SDLKey)(-1);

	private byte _buttonsPressed;
	private string _tooltip = string.Empty;
	protected readonly Dictionary<byte, ActionHandler> _click = [], _press = [], _release = [];
	protected ActionHandler? _in, _over, _out;
	protected readonly Dictionary<SDLKey, ActionHandler> _keyPress = [], _keyRelease = [];
	protected bool _isHovered, _isFocused = true, _listButton, _tftdMode;

	/// <summary>Sets up a blank interactive surface with the specified size and position.</summary>
	public InteractiveSurface(int width, int height, int x = 0, int y = 0) : base(width, height, x, y)
	{
	}

	/// <summary>Is this mouse button pressed? (0 = any button)</summary>
	protected bool IsButtonPressed(byte button = 0)
	{
		if (button == 0)
			return _buttonsPressed != 0;
		return (_buttonsPressed & SDLMouse.SDL_BUTTON(button)) != 0;
	}

	/// <summary>Is this mouse button event handled?</summary>
	protected virtual bool IsButtonHandled(byte button = 0)
	{
		bool handled = _click.ContainsKey(0) || _press.ContainsKey(0) || _release.ContainsKey(0);
		if (!handled && button != 0)
			handled = _click.ContainsKey(button) || _press.ContainsKey(button) || _release.ContainsKey(button);
		return handled;
	}

	/// <summary>Sets a mouse button's internal state.</summary>
	protected void SetButtonPressed(byte button, bool pressed)
	{
		if (pressed)
			_buttonsPressed |= SDLMouse.SDL_BUTTON(button);
		else
			_buttonsPressed &= (byte)~SDLMouse.SDL_BUTTON(button);
	}

	/// <summary>Changes the visibility; hidden surfaces get unpressed.</summary>
	public override bool Visible
	{
		get => base.Visible;
		set
		{
			base.Visible = value;
			// Unpress button if it was hidden
			if (!_visible)
				Unpress(null);
		}
	}

	/// <summary>Processes any pending events and calls the respective handlers.</summary>
	public virtual void Handle(Action action, State? state)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (!_visible || _hidden)
			return;

		action.Sender = this;
		var ev = action.Details;

		if (ev.type == SDLEventType.SDL_MOUSEBUTTONUP || ev.type == SDLEventType.SDL_MOUSEBUTTONDOWN)
			action.SetMouseAction(ev.button.x, ev.button.y, X, Y);
		else if (ev.type == SDLEventType.SDL_MOUSEMOTION)
			action.SetMouseAction(ev.motion.x, ev.motion.y, X, Y);

		if (action.IsMouseAction)
		{
			if (action.AbsoluteXMouse >= X && action.AbsoluteXMouse < X + Width &&
				action.AbsoluteYMouse >= Y && action.AbsoluteYMouse < Y + Height)
			{
				if (!_isHovered)
				{
					_isHovered = true;
					MouseIn(action, state);
				}
				if (_listButton && ev.type == SDLEventType.SDL_MOUSEMOTION)
				{
					_buttonsPressed = State.GamePtr?.MouseButtonState ?? 0;
					for (byte i = 1; i <= NUM_BUTTONS; ++i)
					{
						if (IsButtonPressed(i))
						{
							ev.button.button = i;
							MousePress(action, state);
						}
					}
				}
				MouseOver(action, state);
			}
			else
			{
				if (_isHovered)
				{
					_isHovered = false;
					MouseOut(action, state);
					if (_listButton && ev.type == SDLEventType.SDL_MOUSEMOTION)
					{
						for (byte i = 1; i <= NUM_BUTTONS; ++i)
						{
							if (IsButtonPressed(i))
								SetButtonPressed(i, false);
							ev.button.button = i;
							MouseRelease(action, state);
						}
					}
				}
			}
		}

		if (ev.type == SDLEventType.SDL_MOUSEBUTTONDOWN)
		{
			if (_isHovered && !IsButtonPressed(ev.button.button))
			{
				SetButtonPressed(ev.button.button, true);
				MousePress(action, state);
			}
		}
		else if (ev.type == SDLEventType.SDL_MOUSEBUTTONUP)
		{
			if (IsButtonPressed(ev.button.button))
			{
				SetButtonPressed(ev.button.button, false);
				MouseRelease(action, state);
				if (_isHovered)
					MouseClick(action, state);
			}
		}

		if (_isFocused)
		{
			if (ev.type == SDLEventType.SDL_KEYDOWN)
				KeyboardPress(action, state);
			else if (ev.type == SDLEventType.SDL_KEYUP)
				KeyboardRelease(action, state);
		}
	}

	/// <summary>Changes the surface's focus. Surfaces will only receive keyboard events if focused.</summary>
	public virtual void SetFocus(bool focus, bool modal = false) => _isFocused = focus;

	/// <summary>Is this surface focused?</summary>
	public bool IsFocused => _isFocused;

	/// <summary>Simulates a "mouse button release". Used in circumstances where the surface is unpressed without user input.</summary>
	public virtual void Unpress(State? state)
	{
		if (IsButtonPressed())
		{
			_buttonsPressed = 0;
			var ev = new SDL_Event { type = SDLEventType.SDL_MOUSEBUTTONUP, button = { button = SDLMouse.SDL_BUTTON_LEFT } };
			MouseRelease(new Action(ev, 0.0, 0.0, 0, 0), state);
		}
	}

	private static void Dispatch(Dictionary<byte, ActionHandler> handlers, Action action)
	{
		if (handlers.TryGetValue(0, out var all))
			all(action);
		if (handlers.TryGetValue(action.Details.button.button, out var one))
			one(action);
	}

	private static void DispatchKey(Dictionary<SDLKey, ActionHandler> handlers, Action action)
	{
		if (handlers.TryGetValue(SDLK_ANY, out var all))
			all(action);
		// Check if Ctrl, Alt and Shift aren't pressed
		var keysym = action.Details.key.keysym;
		bool mod = (keysym.mod & (SDLMod.KMOD_CTRL | SDLMod.KMOD_ALT | SDLMod.KMOD_SHIFT)) != 0;
		if (handlers.TryGetValue(keysym.sym, out var one) && !mod)
			one(action);
	}

	/// <summary>Called every time there's a mouse press over the surface.</summary>
	public virtual void MousePress(Action action, State? state) => Dispatch(_press, action);

	/// <summary>Called every time there's a mouse release over the surface.</summary>
	public virtual void MouseRelease(Action action, State? state) => Dispatch(_release, action);

	/// <summary>Called every time there's a mouse click on the surface.</summary>
	public virtual void MouseClick(Action action, State? state) => Dispatch(_click, action);

	/// <summary>Called every time the mouse moves into the surface.</summary>
	public virtual void MouseIn(Action action, State? state) => _in?.Invoke(action);

	/// <summary>Called every time the mouse moves over the surface.</summary>
	public virtual void MouseOver(Action action, State? state) => _over?.Invoke(action);

	/// <summary>Called every time the mouse moves out of the surface.</summary>
	public virtual void MouseOut(Action action, State? state) => _out?.Invoke(action);

	/// <summary>Called every time there's a keyboard press when the surface is focused.</summary>
	public virtual void KeyboardPress(Action action, State? state) => DispatchKey(_keyPress, action);

	/// <summary>Called every time there's a keyboard release when the surface is focused.</summary>
	public virtual void KeyboardRelease(Action action, State? state) => DispatchKey(_keyRelease, action);

	private static void SetHandler<TKey>(Dictionary<TKey, ActionHandler> map, TKey key, ActionHandler? handler) where TKey : notnull
	{
		if (handler != null)
			map[key] = handler;
		else
			map.Remove(key);
	}

	/// <summary>Hooks an action handler to a mouse click on the surface (0 = any button).</summary>
	public void OnMouseClick(ActionHandler? handler, byte button = SDLMouse.SDL_BUTTON_LEFT) => SetHandler(_click, button, handler);

	/// <summary>Hooks an action handler to a mouse press over the surface (0 = any button).</summary>
	public void OnMousePress(ActionHandler? handler, byte button = 0) => SetHandler(_press, button, handler);

	/// <summary>Hooks an action handler to a mouse release over the surface (0 = any button).</summary>
	public void OnMouseRelease(ActionHandler? handler, byte button = 0) => SetHandler(_release, button, handler);

	/// <summary>Hooks an action handler to moving the mouse into the surface.</summary>
	public void OnMouseIn(ActionHandler? handler) => _in = handler;

	/// <summary>Hooks an action handler to moving the mouse over the surface.</summary>
	public void OnMouseOver(ActionHandler? handler) => _over = handler;

	/// <summary>Hooks an action handler to moving the mouse out of the surface.</summary>
	public void OnMouseOut(ActionHandler? handler) => _out = handler;

	/// <summary>Hooks an action handler to pressing a key when the surface is focused.</summary>
	public void OnKeyboardPress(ActionHandler? handler, SDLKey key = SDLK_ANY)
	{
		// Ignore unknown keys
		if (key != SDLKey.SDLK_UNKNOWN)
			SetHandler(_keyPress, key, handler);
	}

	/// <summary>Hooks an action handler to releasing a key when the surface is focused.</summary>
	public void OnKeyboardRelease(ActionHandler? handler, SDLKey key = SDLK_ANY)
	{
		// Ignore unknown keys
		if (key != SDLKey.SDLK_UNKNOWN)
			SetHandler(_keyRelease, key, handler);
	}

	/// <summary>Marks this surface as a text list button.</summary>
	public void SetListButton() => _listButton = true;

	/// <summary>The tooltip string id of the surface.</summary>
	public string Tooltip { get => _tooltip; set => _tooltip = value; }

	/// <summary>Use a colour lookup table instead of inversion for the alternate form (TFTD style).</summary>
	public bool TFTDMode { get => _tftdMode; set => _tftdMode = value; }
}
