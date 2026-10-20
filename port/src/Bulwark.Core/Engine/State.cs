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
// Ported from: src/Engine/State.h, src/Engine/State.cpp
// Port status: partial (missing: interface rulesets, windows/sounds/music in init, text, palettes from mods, resize/recenter).

namespace Bulwark.Engine;

/// <summary>
/// A game state that receives user input and reacts accordingly.
/// Game states typically represent a whole window or screen that
/// the user interacts with, making the game... well, interactive.
/// They automatically handle child elements used to transmit
/// information from/to the user, and are linked to the core game
/// engine which manages them.
/// </summary>
public class State
{
	private static Game? s_game;

	protected readonly List<Surface> _surfaces = [];
	protected readonly List<Surface> _surfacesOwned = [];
	protected bool _screen = true;
	protected bool _soundPlayed;
	protected InteractiveSurface? _modal;
	protected readonly Rgba[] _palette = new Rgba[256];
	protected byte _cursorColor;

	/// <summary>The game this state belongs to (static, as in the C++ code).</summary>
	public static Game? GamePtr => s_game;

	/// <summary>The game this state belongs to; throws if no game was created yet.</summary>
	protected static Game _game => s_game ?? throw new InvalidOperationException("No Game instance");

	/// <summary>Sets the game object pointer.</summary>
	public static void SetGamePtr(Game? game) => s_game = game;

	/// <summary>Adds an optional child element; it will not be displayed.</summary>
	public T PreAdd<T>(T surface) where T : Surface
	{
		if (!_surfacesOwned.Contains(surface))
			_surfacesOwned.Add(surface);
		return surface;
	}

	/// <summary>Adds a child element to the state.</summary>
	public void Add(Surface surface)
	{
		ArgumentNullException.ThrowIfNull(surface);
		// Set palette
		surface.SetPalette(_palette);
		_surfaces.Add(surface);
		PreAdd(surface);
	}

	/// <summary>Whether this state is a full-screen (states below it are not drawn).</summary>
	public bool IsScreen => _screen;

	/// <summary>Toggles the full-screen flag.</summary>
	public void ToggleScreen() => _screen = !_screen;

	/// <summary>Initializes the state and its child elements.</summary>
	public virtual void Init()
	{
		_game.Screen.SetPalette(_palette);
	}

	/// <summary>Runs any code the state needs to keep updating every game cycle.</summary>
	public virtual void Think()
	{
		foreach (var surface in _surfaces)
			surface.Think();
	}

	/// <summary>Takes care of any events from the core game engine, and passes them on to its child elements.</summary>
	public virtual void Handle(Action action)
	{
		if (_modal == null)
		{
			for (int i = _surfaces.Count - 1; i >= 0; --i)
			{
				if (_surfaces[i] is InteractiveSurface j)
					j.Handle(action, this);
			}
		}
		else
		{
			_modal.Handle(action, this);
		}
	}

	/// <summary>Blits all the child elements on to the screen.</summary>
	public virtual void Blit()
	{
		foreach (var surface in _surfaces)
			surface.Blit(_game.Screen.Surface);
	}

	/// <summary>Hides all the state surfaces.</summary>
	public void HideAll()
	{
		foreach (var surface in _surfaces)
			surface.Hidden = true;
	}

	/// <summary>Shows all the hidden state surfaces.</summary>
	public void ShowAll()
	{
		foreach (var surface in _surfaces)
			surface.Hidden = false;
	}

	/// <summary>Resets the status of all the state surfaces (unpresses buttons).</summary>
	public void ResetAll()
	{
		foreach (var surface in _surfaces)
		{
			if (surface is InteractiveSurface s)
				s.Unpress(this);
		}
	}

	/// <summary>Makes one surface the only one receiving input (null = all of them).</summary>
	public void SetModal(InteractiveSurface? surface) => _modal = surface;

	/// <summary>Replaces a certain amount of colors in the state's palette.</summary>
	public void SetStatePalette(ReadOnlySpan<Rgba> colors, int firstcolor = 0, int ncolors = 256)
	{
		colors[..Math.Min(ncolors, colors.Length)].CopyTo(_palette.AsSpan(firstcolor));
	}

	/// <summary>The state's 8bpp palette.</summary>
	public ReadOnlySpan<Rgba> Palette => _palette;
}
