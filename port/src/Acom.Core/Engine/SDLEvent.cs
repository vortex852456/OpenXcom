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
// Ported from: SDL 1.2 SDL_events.h / SDL_mouse.h (subset used by the engine)
// Field names follow SDL 1.2 so C++ code like `ev.key.keysym.sym` ports as a 1:1 rename.

#pragma warning disable CA1815 // Plain SDL data structs; equality is not needed.

namespace Acom.Engine;

/// <summary>Event types with the numeric values of SDL 1.2.</summary>
public enum SDLEventType : byte
{
	SDL_NOEVENT = 0,
	SDL_ACTIVEEVENT = 1,
	SDL_KEYDOWN = 2,
	SDL_KEYUP = 3,
	SDL_MOUSEMOTION = 4,
	SDL_MOUSEBUTTONDOWN = 5,
	SDL_MOUSEBUTTONUP = 6,
	SDL_QUIT = 12,
	SDL_VIDEORESIZE = 16,
	SDL_VIDEOEXPOSE = 17,
	SDL_USEREVENT = 24,
}

/// <summary>Mouse button constants and helpers of SDL 1.2.</summary>
public static class SDLMouse
{
	public const byte SDL_BUTTON_LEFT = 1;
	public const byte SDL_BUTTON_MIDDLE = 2;
	public const byte SDL_BUTTON_RIGHT = 3;
	public const byte SDL_BUTTON_WHEELUP = 4;
	public const byte SDL_BUTTON_WHEELDOWN = 5;
	public const byte SDL_BUTTON_X1 = 6;
	public const byte SDL_BUTTON_X2 = 7;

	/// <summary>Bit mask of a button, like the SDL_BUTTON(X) macro.</summary>
	public static byte SDL_BUTTON(int button) => (byte)(1 << (button - 1));
}

/// <summary>Key symbol of a keyboard event.</summary>
public struct SDL_keysym
{
	public SDLKey sym;
	public SDLMod mod;
	/// <summary>Translated character, if any (SDL_EnableUNICODE).</summary>
	public char unicode;
}

/// <summary>Keyboard event data.</summary>
public struct SDL_KeyboardEvent
{
	public SDL_keysym keysym;
}

/// <summary>Mouse motion event data. Coordinates are window pixels.</summary>
public struct SDL_MouseMotionEvent
{
	public byte state;
	public int x, y;
	public int xrel, yrel;
}

/// <summary>Mouse button event data. Coordinates are window pixels.</summary>
public struct SDL_MouseButtonEvent
{
	public byte button;
	public int x, y;
}

/// <summary>Window resize event data.</summary>
public struct SDL_ResizeEvent
{
	public int w, h;
}

/// <summary>
/// Input/window event. A class instead of SDL's union, because handlers modify
/// fields of the event they receive (e.g. <c>button.button</c>).
/// </summary>
public sealed class SDL_Event
{
	public SDLEventType type;
	public SDL_KeyboardEvent key;
	public SDL_MouseMotionEvent motion;
	public SDL_MouseButtonEvent button;
	public SDL_ResizeEvent resize;

	public static SDL_Event KeyDown(SDLKey sym, SDLMod mod = SDLMod.KMOD_NONE, char unicode = '\0')
		=> new() { type = SDLEventType.SDL_KEYDOWN, key = { keysym = { sym = sym, mod = mod, unicode = unicode } } };

	public static SDL_Event KeyUp(SDLKey sym, SDLMod mod = SDLMod.KMOD_NONE)
		=> new() { type = SDLEventType.SDL_KEYUP, key = { keysym = { sym = sym, mod = mod } } };

	public static SDL_Event MouseMotion(int x, int y, int xrel = 0, int yrel = 0, byte state = 0)
		=> new() { type = SDLEventType.SDL_MOUSEMOTION, motion = { x = x, y = y, xrel = xrel, yrel = yrel, state = state } };

	public static SDL_Event MouseButtonDown(byte button, int x, int y)
		=> new() { type = SDLEventType.SDL_MOUSEBUTTONDOWN, button = { button = button, x = x, y = y } };

	public static SDL_Event MouseButtonUp(byte button, int x, int y)
		=> new() { type = SDLEventType.SDL_MOUSEBUTTONUP, button = { button = button, x = x, y = y } };

	public static SDL_Event Quit() => new() { type = SDLEventType.SDL_QUIT };
}
