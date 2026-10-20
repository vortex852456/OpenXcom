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

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Acom.Desktop;

/// <summary>
/// Receives input as SDL2 events from the SDL2 library that MonoGame DesktopGL runs on and
/// converts them into SDL 1.2 style events for the engine.
/// </summary>
/// <remarks>
/// MonoGame only offers polled input, which loses clicks shorter than one frame (touchpad taps)
/// and key repeats. An SDL event watch sees every event in order, like the original game loop.
/// </remarks>
internal sealed unsafe partial class SdlInput : IDisposable
{
	private const string SdlLibrary = "SDL2";

	// SDL2 event types
	private const uint SDL_KEYDOWN = 0x300, SDL_KEYUP = 0x301, SDL_TEXTINPUT = 0x303;
	private const uint SDL_MOUSEMOTION = 0x400, SDL_MOUSEBUTTONDOWN = 0x401, SDL_MOUSEBUTTONUP = 0x402, SDL_MOUSEWHEEL = 0x403;

	private static readonly Dictionary<int, SDLKey> s_scancodeMap = BuildScancodeMap();
	private static SdlInput? s_instance;

	private readonly Lock _lock = new();
	private readonly List<SDL_Event> _pending = [];
	private int _mouseX, _mouseY;
	private byte _buttons;
	/// <summary>A repeated key press was dropped; its text input follows and is dropped too.</summary>
	private bool _dropText;

	static SdlInput()
	{
		NativeLibrary.SetDllImportResolver(typeof(SdlInput).Assembly, ResolveSdl);
	}

	public SdlInput()
	{
		if (s_instance != null)
			throw new InvalidOperationException("Only one SdlInput can be active");
		s_instance = this;
		SDL_AddEventWatch(&EventWatch, 0);
	}

	public void Dispose()
	{
		SDL_DelEventWatch(&EventWatch, 0);
		s_instance = null;
	}

	/// <summary>Moves all events received since the last call into the engine's queue.</summary>
	public void Flush(Acom.Game game)
	{
		lock (_lock)
		{
			foreach (var ev in _pending)
				game.PushEvent(ev);
			_pending.Clear();
		}
	}

	[LibraryImport(SdlLibrary)]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	private static partial void SDL_AddEventWatch(delegate* unmanaged[Cdecl]<nint, nint, int> filter, nint userdata);

	[LibraryImport(SdlLibrary)]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	private static partial void SDL_DelEventWatch(delegate* unmanaged[Cdecl]<nint, nint, int> filter, nint userdata);

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int EventWatch(nint userdata, nint sdlEvent)
	{
		try
		{
			s_instance?.Translate((byte*)sdlEvent);
		}
#pragma warning disable CA1031 // An exception must never unwind into native SDL code.
		catch (Exception e)
#pragma warning restore CA1031
		{
			Console.Error.WriteLine($"Input error: {e}");
		}
		return 1;
	}

	/// <summary>Converts one SDL2 event (raw SDL_Event union) into SDL 1.2 form.</summary>
	private void Translate(byte* e)
	{
		uint type = *(uint*)e;
		lock (_lock)
		{
			switch (type)
			{
				case SDL_KEYDOWN:
				case SDL_KEYUP:
				{
					// SDL_KeyboardEvent: repeat @13, keysym.scancode @16, keysym.sym @20, keysym.mod @24
					// SDL 1.2 only repeated held keys after SDL_EnableKeyRepeat; drop the repeat and its text.
					_dropText = type == SDL_KEYDOWN && *(e + 13) != 0 && !SDLKeyboard.KeyRepeat;
					if (_dropText)
						break;
					int scancode = *(int*)(e + 16);
					int sym = *(int*)(e + 20);
					// SDL2 modifier bits match SDL 1.2 (GUI keys took over the META bits).
					var mod = (SDLMod)(*(ushort*)(e + 24));
					var key = MapKey(scancode, sym);
					if (type == SDL_KEYDOWN)
					{
						// SDL 1.2 reported these control characters as unicode as well.
						char unicode = key is SDLKey.SDLK_RETURN or SDLKey.SDLK_ESCAPE or SDLKey.SDLK_BACKSPACE or SDLKey.SDLK_TAB ? (char)(int)key : '\0';
						_pending.Add(SDL_Event.KeyDown(key, mod, unicode));
					}
					else
					{
						_pending.Add(SDL_Event.KeyUp(key, mod));
					}
					break;
				}
				case SDL_TEXTINPUT:
				{
					// SDL_TextInputEvent: UTF-8 text @12, up to 32 bytes. SDL 1.2 attached the character to the key press.
					if (_dropText)
					{
						_dropText = false;
						break;
					}
					var text = Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(e + 12));
					foreach (char c in text)
					{
						if (_pending.Count > 0 && _pending[^1] is { type: SDLEventType.SDL_KEYDOWN } last && last.key.keysym.unicode == '\0')
							last.key.keysym.unicode = c;
						else
							_pending.Add(SDL_Event.KeyDown(SDLKey.SDLK_UNKNOWN, SDLMod.KMOD_NONE, c));
					}
					break;
				}
				case SDL_MOUSEMOTION:
				{
					// SDL_MouseMotionEvent: x @20, y @24, xrel @28, yrel @32
					int x = *(int*)(e + 20), y = *(int*)(e + 24);
					_pending.Add(SDL_Event.MouseMotion(x, y, *(int*)(e + 28), *(int*)(e + 32), _buttons));
					_mouseX = x;
					_mouseY = y;
					break;
				}
				case SDL_MOUSEBUTTONDOWN:
				case SDL_MOUSEBUTTONUP:
				{
					// SDL_MouseButtonEvent: button @16, x @20, y @24
					byte button = MapButton(*(e + 16));
					int x = *(int*)(e + 20), y = *(int*)(e + 24);
					if (type == SDL_MOUSEBUTTONDOWN)
					{
						_buttons |= SDLMouse.SDL_BUTTON(button);
						_pending.Add(SDL_Event.MouseButtonDown(button, x, y));
					}
					else
					{
						_buttons &= (byte)~SDLMouse.SDL_BUTTON(button);
						_pending.Add(SDL_Event.MouseButtonUp(button, x, y));
					}
					break;
				}
				case SDL_MOUSEWHEEL:
				{
					// SDL_MouseWheelEvent: y @20. SDL 1.2 reported the wheel as button 4/5 press and release.
					int y = *(int*)(e + 20);
					if (y != 0)
					{
						byte button = y > 0 ? SDLMouse.SDL_BUTTON_WHEELUP : SDLMouse.SDL_BUTTON_WHEELDOWN;
						_pending.Add(SDL_Event.MouseButtonDown(button, _mouseX, _mouseY));
						_pending.Add(SDL_Event.MouseButtonUp(button, _mouseX, _mouseY));
					}
					break;
				}
			}
		}
	}

	/// <summary>SDL2 numbers X1/X2 as 4/5; SDL 1.2 used 4/5 for the wheel and 6/7 for X1/X2.</summary>
	private static byte MapButton(byte sdl2Button) => sdl2Button switch
	{
		4 => SDLMouse.SDL_BUTTON_X1,
		5 => SDLMouse.SDL_BUTTON_X2,
		_ => sdl2Button,
	};

	/// <summary>Non-character keys via scancode, characters via their (ASCII) keycode, as in SDL 1.2.</summary>
	private static SDLKey MapKey(int scancode, int sym)
	{
		if (s_scancodeMap.TryGetValue(scancode, out var key))
			return key;
		if (sym > 0 && sym < 128)
			return (SDLKey)sym;
		return SDLKey.SDLK_UNKNOWN;
	}

	private static Dictionary<int, SDLKey> BuildScancodeMap()
	{
		// SDL2 scancode numbers (SDL_scancode.h) -> SDL 1.2 keys
		var map = new Dictionary<int, SDLKey>
		{
			[57] = SDLKey.SDLK_CAPSLOCK,
			[70] = SDLKey.SDLK_PRINT,
			[71] = SDLKey.SDLK_SCROLLOCK,
			[72] = SDLKey.SDLK_PAUSE,
			[73] = SDLKey.SDLK_INSERT,
			[74] = SDLKey.SDLK_HOME,
			[75] = SDLKey.SDLK_PAGEUP,
			[77] = SDLKey.SDLK_END,
			[78] = SDLKey.SDLK_PAGEDOWN,
			[79] = SDLKey.SDLK_RIGHT,
			[80] = SDLKey.SDLK_LEFT,
			[81] = SDLKey.SDLK_DOWN,
			[82] = SDLKey.SDLK_UP,
			[83] = SDLKey.SDLK_NUMLOCK,
			[84] = SDLKey.SDLK_KP_DIVIDE,
			[85] = SDLKey.SDLK_KP_MULTIPLY,
			[86] = SDLKey.SDLK_KP_MINUS,
			[87] = SDLKey.SDLK_KP_PLUS,
			[88] = SDLKey.SDLK_KP_ENTER,
			[98] = SDLKey.SDLK_KP0,
			[99] = SDLKey.SDLK_KP_PERIOD,
			[101] = SDLKey.SDLK_MENU,
			[103] = SDLKey.SDLK_KP_EQUALS,
			[224] = SDLKey.SDLK_LCTRL,
			[225] = SDLKey.SDLK_LSHIFT,
			[226] = SDLKey.SDLK_LALT,
			[227] = SDLKey.SDLK_LSUPER,
			[228] = SDLKey.SDLK_RCTRL,
			[229] = SDLKey.SDLK_RSHIFT,
			[230] = SDLKey.SDLK_RALT,
			[231] = SDLKey.SDLK_RSUPER,
		};
		for (int i = 0; i < 12; ++i)
			map[58 + i] = SDLKey.SDLK_F1 + i; // F1..F12
		for (int i = 0; i < 3; ++i)
			map[104 + i] = SDLKey.SDLK_F13 + i; // F13..F15
		for (int i = 0; i < 9; ++i)
			map[89 + i] = SDLKey.SDLK_KP1 + i; // keypad 1..9
		return map;
	}

	/// <summary>Finds the SDL2 library bundled with MonoGame (runtimes/&lt;rid&gt;/native).</summary>
	private static nint ResolveSdl(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
	{
		if (libraryName != SdlLibrary)
			return 0;
		string file, folder;
		if (OperatingSystem.IsWindows())
			(file, folder) = ("SDL2.dll", RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "win-arm64" : "win-x64");
		else if (OperatingSystem.IsMacOS())
			(file, folder) = ("libSDL2-2.0.0.dylib", "osx");
		else
			(file, folder) = ("libSDL2-2.0.so.0", RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64");
		foreach (var candidate in new[] { Path.Combine(AppContext.BaseDirectory, "runtimes", folder, "native", file), Path.Combine(AppContext.BaseDirectory, file), file })
		{
			if (NativeLibrary.TryLoad(candidate, out var handle))
				return handle;
		}
		return 0;
	}
}
