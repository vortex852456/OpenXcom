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
// Ported from: src/Engine/State.h, src/Engine/State.cpp

using System.Globalization;

namespace Acom;

/// <summary>
/// A game state that receives user input and reacts accordingly.
/// Game states typically represent a whole window or screen that
/// the user interacts with, making the game... well, interactive.
/// They automatically handle child elements used to transmit
/// information from/to the user, and are linked to the core game
/// engine which manages them.
/// </summary>
/// <remarks>
/// The C++ destructor deletes the owned surfaces; here the garbage collector does that.
/// </remarks>
public class State
{
	private static Game? s_game;

	protected readonly List<Surface> _surfaces = [];
	protected readonly List<Surface> _surfacesOwned = [];
	protected bool _screen = true;
	protected bool _soundPlayed;
	protected InteractiveSurface? _modal;
	protected RuleInterface? _ruleInterface;
	protected RuleInterface? _ruleInterfaceParent;
	protected Sound? _customSound;

	protected readonly Rgba[] _palette = new Rgba[256];
	protected byte _cursorColor;

	/// <summary>
	/// Initializes a brand new state with no child elements.
	/// By default states are full-screen; the palette starts all black.
	/// </summary>
	public State()
	{
		// Deviation: tolerates a missing game (C++ dereferences it), so states can be created in tests.
		_cursorColor = s_game?.Cursor.Color ?? 0;
	}

	/// <summary>The game this state belongs to (static, as in the C++ code).</summary>
	public static Game? GamePtr => s_game;

	/// <summary>The game this state belongs to; throws if no game was created yet.</summary>
	protected static Game _game => s_game ?? throw new InvalidOperationException("No Game instance");

	/// <summary>The loaded mod; C++ dereferences <c>_game->getMod()</c> without a check.</summary>
	private static Mod GameMod => _game.Mod ?? throw new InvalidOperationException("No mod loaded");

	/// <summary>Sets the game object pointer.</summary>
	public static void SetGamePtr(Game? game) => s_game = game;

	/// <summary>Set interface data from the ruleset, also sets the palette for the state.</summary>
	/// <param name="category">Name of the interface set.</param>
	/// <param name="alterPal">Should we swap out the backpal colors?</param>
	/// <param name="battleGame">Should we use battlescape palette? (this only applies to options screens)</param>
	public void SetInterface(string category, bool alterPal = false, SavedBattleGame? battleGame = null)
	{
		int backPal = -1;
		string pal = "PAL_GEOSCAPE";

		var mod = GameMod;
		_ruleInterface = mod.GetInterface(category);
		if (_ruleInterface != null)
		{
			_ruleInterfaceParent = mod.GetInterface(_ruleInterface.Parent);
			pal = _ruleInterface.Palette;
			var element = _ruleInterface.GetElementOptional("palette");
			if (_ruleInterfaceParent != null)
			{
				element ??= _ruleInterfaceParent.GetElementOptional("palette");
				if (string.IsNullOrEmpty(pal))
					pal = _ruleInterfaceParent.Palette;
			}
			if (element != null)
			{
				int color = alterPal ? element.color2 : element.color;
				if (color != int.MaxValue)
					backPal = color;
			}
		}
		if (battleGame != null)
		{
			battleGame.SetPaletteByDepth(this);
		}
		else if (string.IsNullOrEmpty(pal))
		{
			pal = "PAL_GEOSCAPE";
			SetStandardPalette(pal, backPal);
		}
		else
		{
			SetStandardPalette(pal, backPal);
		}
	}

	/// <summary>Set window background from the ruleset.</summary>
	/// <param name="window">Window handle.</param>
	/// <param name="s">ID of the interface ruleset entry.</param>
	public void SetWindowBackground(Window window, string s)
	{
		var mod = GameMod;
		string bgImageName = mod.GetInterface(s).GetBackgroundImage(mod, _game.SavedGame);
		SetWindowBackgroundImage(window, bgImageName);
	}

#pragma warning disable CA1822 // instance members as in C++; they only reach the static game pointer
	/// <summary>Set window background by image name (instead of by interface name).</summary>
	/// <param name="window">Window handle.</param>
	/// <param name="bgImageName">ID of the image.</param>
	public void SetWindowBackgroundImage(Window window, string bgImageName)
	{
		ArgumentNullException.ThrowIfNull(window);
		var bgImage = GameMod.GetSurface(bgImageName);
		window.SetBackground(bgImage);
	}
#pragma warning restore CA1822

	/// <summary>Adds an optional child element; it will not be displayed.</summary>
	public T PreAdd<T>(T surface) where T : Surface
	{
		//TODO: O(n^2) but number of surfaces is less than 100 (it become lag araund 100k surfaces) and sort of hash will make deleting order nondetermistic
		if (!_surfacesOwned.Contains(surface))
			_surfacesOwned.Add(surface);
		return surface;
	}

	/// <summary>
	/// Adds a new child surface for the state to take care of, giving it the game's display palette.
	/// Once associated, the state handles all of the surface's behaviour and management automatically.
	/// </summary>
	/// <remarks>Since visible elements can overlap one another, they have to be added in ascending Z-Order to be blitted correctly onto the screen.</remarks>
	public void Add(Surface surface)
	{
		ArgumentNullException.ThrowIfNull(surface);
		// Set palette
		surface.SetPalette(_palette);

		// Set default text resources
		InitText(surface);

		_surfaces.Add(surface);
		PreAdd(surface);
	}

	/// <summary>
	/// As above, except this adds a surface based on an interface element defined in the ruleset.
	/// This REQUIRES the ruleset to have been loaded prior to use.
	/// </summary>
	/// <param name="surface">Child surface.</param>
	/// <param name="id">The ID of the element defined in the ruleset, if any.</param>
	/// <param name="category">The category of elements this interface is associated with.</param>
	/// <param name="parent">The surface to base the coordinates of this element off; without one the element is not moved.</param>
	public void Add(Surface surface, string id, string category, Surface? parent = null)
	{
		ArgumentNullException.ThrowIfNull(surface);
		// Set palette
		surface.SetPalette(_palette);

		// this only works if we're dealing with a battlescape button
		var bsbtn = surface as BattlescapeButton;

		var mod = GameMod;
		if (mod.GetInterface(category, false) != null)
		{
			var element = mod.GetInterface(category).GetElementOptional(id);
			if (element != null)
			{
				if (parent != null && element.w != int.MaxValue && element.h != int.MaxValue)
				{
					surface.SetWidth(element.w);
					surface.SetHeight(element.h);
				}

				if (parent != null && element.x != int.MaxValue && element.y != int.MaxValue)
				{
					surface.X = parent.X + element.x;
					surface.Y = parent.Y + element.y;
				}

				if (surface is InteractiveSurface inter)
					inter.TFTDMode = element.TFTDMode;

				if (element.color != int.MaxValue)
					surface.SetColor((byte)element.color);
				if (element.color2 != int.MaxValue)
					surface.SetSecondaryColor((byte)element.color2);
				if (element.border != int.MaxValue)
					surface.SetBorderColor((byte)element.border);
			}
		}

		if (bsbtn != null)
		{
			// this will initialize the graphics and settings of the battlescape button.
			bsbtn.Copy(parent!);
			bsbtn.InitSurfaces();
		}

		// Set default text resources
		InitText(surface);

		_surfaces.Add(surface);
		PreAdd(surface);
	}

	/// <summary>Gives a surface the default fonts and language, once both are loaded.</summary>
	private static void InitText(Surface surface)
	{
		var mod = _game.Mod;
		if (_game.Language != null && mod != null)
			surface.InitText(mod.GetFont("FONT_BIG"), mod.GetFont("FONT_SMALL"), _game.Language);
	}

	/// <summary>
	/// Whether this is a full-screen state. This is used to optimize the state machine since
	/// full-screen states automatically cover the whole screen, (whether they actually use it
	/// all or not) so states behind them can be safely ignored since they'd be covered up.
	/// </summary>
	public bool IsScreen => _screen;

	/// <summary>
	/// Toggles the full-screen flag. Used by windows to keep the previous screen in display
	/// while the window is still "popping up".
	/// </summary>
	public void ToggleScreen() => _screen = !_screen;

	/// <summary>
	/// Initializes the state and its child elements. This is used for settings that have to be
	/// reset every time the state is returned to focus (eg. palettes), so can't just be put in the
	/// constructor (remember there's a stack of states, so they can be created once while being
	/// repeatedly switched back into focus).
	/// </summary>
	public virtual void Init()
	{
		_game.Screen.SetPalette(_palette);
		_game.Cursor.SetPalette(_palette);
		_game.Cursor.SetColor(_cursorColor);
		_game.Cursor.Draw();
		_game.FpsCounter.SetPalette(_palette);
		_game.FpsCounter.SetColor(_cursorColor);
		_game.FpsCounter.Draw();

		// Highest priority: custom sound set explicitly in the code
		// Medium priority: sound defined by the interface ruleset
		// Lowest priority: default window popup sound
		bool muteWindowPopupSound = false;
		if (!_soundPlayed)
		{
			_soundPlayed = true;
			if (_customSound == null && _ruleInterface != null && _ruleInterface.Sound != Mod.NO_SOUND)
				_customSound = GameMod.GetSound("GEO.CAT", _ruleInterface.Sound);
			if (_customSound != null)
			{
				muteWindowPopupSound = true;
				_customSound.Play();
			}
		}

		foreach (var surface in _surfaces)
		{
			if (surface is Window window)
			{
				if (muteWindowPopupSound)
					window.Mute();
				window.Invalidate();
			}
		}
		if (_ruleInterface != null && !string.IsNullOrEmpty(_ruleInterface.Music))
			GameMod.PlayMusic(_ruleInterface.Music);
	}

	/// <summary>Runs any code the state needs to keep updating every game cycle, like timers and other real-time elements.</summary>
	public virtual void Think()
	{
		foreach (var surface in _surfaces)
			surface.Think();
	}

	/// <summary>Takes care of any events from the core game engine, and passes them on to its InteractiveSurface child elements.</summary>
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

	/// <summary>Blits all the visible Surface child elements onto the display screen, by order of addition.</summary>
	public virtual void Blit()
	{
		foreach (var surface in _surfaces)
			surface.Blit(_game.Screen.Surface);
	}

	/// <summary>Hides all the Surface child elements on display.</summary>
	public void HideAll()
	{
		foreach (var surface in _surfaces)
			surface.Hidden = true;
	}

	/// <summary>Shows all the hidden Surface child elements.</summary>
	public void ShowAll()
	{
		foreach (var surface in _surfaces)
			surface.Hidden = false;
	}

	/// <summary>Resets the status of all the Surface child elements, like unpressing buttons.</summary>
	public void ResetAll()
	{
		foreach (var surface in _surfaces)
		{
			if (surface is InteractiveSurface s)
				s.Unpress(this);
		}
	}

#pragma warning disable CA1822 // instance members as in C++; they only reach the static game pointer
	/// <summary>Get the localized text for dictionary key <paramref name="id"/>.</summary>
	public LocalizedText Tr(string id) => _game.Language.GetString(id);

	/// <summary>Get the localized text for <paramref name="id"/> (alt = 0, the original) or its alternative id_alt (alt &gt; 0).</summary>
	public LocalizedText TrAlt(string id, int alt)
	{
		// alt = 0 is the original, alt > 0 are the alternatives
		string key = alt > 0 ? id + "_" + alt.ToString(CultureInfo.InvariantCulture) : id;
		return _game.Language.GetString(key);
	}

	/// <summary>Get the localized text for dictionary key <paramref name="id"/>, in the proper form for the number <paramref name="n"/>.</summary>
	public LocalizedText Tr(string id, uint n) => _game.Language.GetString(id, n);

	/// <summary>Same as <see cref="Tr(string, uint)"/>; C++ converts int arguments to unsigned implicitly.</summary>
	public LocalizedText Tr(string id, int n) => _game.Language.GetString(id, n);

	/// <summary>Get the localized text for dictionary key <paramref name="id"/>, in the proper form for the soldier gender.</summary>
	public LocalizedText Tr(string id, SoldierGender gender) => _game.Language.GetString(id, gender);
#pragma warning restore CA1822

	/// <summary>Redraw all the text-type surfaces.</summary>
	public void RedrawText()
	{
		foreach (var surface in _surfaces)
		{
			if (surface is Text or TextButton or TextEdit or TextList)
				surface.Draw();
		}
	}

	/// <summary>Does the state only have one text list (to scroll)?</summary>
	public bool HasOnlyOneScrollableTextList
	{
		get
		{
			int count = 0;
			foreach (var surface in _surfaces)
			{
				if (surface is TextList list && list.RowsDoNotUse > list.VisibleRows)
					count++;
			}
			return count == 1;
		}
	}

	/// <summary>Centers all the surfaces on the screen.</summary>
	public void CenterAllSurfaces()
	{
		foreach (var surface in _surfaces)
		{
			surface.X = surface.X + _game.Screen.DX;
			surface.Y = surface.Y + _game.Screen.DY;
		}
	}

	/// <summary>Drop all the surfaces by half the screen height.</summary>
	public void LowerAllSurfaces()
	{
		foreach (var surface in _surfaces)
			surface.Y = surface.Y + _game.Screen.DY / 2;
	}

	/// <summary>Switch all the colours to something a little more battlescape appropriate.</summary>
	public void ApplyBattlescapeTheme(string category)
	{
		var mod = GameMod;
		var element = mod.GetInterface("mainMenu").GetElement("battlescapeTheme");
		string altBg = mod.GetInterface(category).AltBackgroundImage;
		if (string.IsNullOrEmpty(altBg))
			altBg = "TAC00.SCR";
		foreach (var surface in _surfaces)
		{
			surface.SetColor((byte)element.color);
			surface.SetHighContrast(true);
			if (surface is Window window)
				window.SetBackground(mod.GetSurface(altBg));
			if (surface is TextList list)
				list.SetArrowColor((byte)element.border);
			if (surface is ComboBox combo)
				combo.SetArrowColor((byte)element.border);
		}
	}

	/// <summary>
	/// Changes the current modal surface. If a surface is modal, then only that surface can
	/// receive events. This is used when an element needs to take priority over everything else, eg. focus.
	/// </summary>
	/// <param name="surface">Modal surface, null for no modal.</param>
	public void SetModal(InteractiveSurface? surface) => _modal = surface;

	/// <summary>Replaces a certain amount of colors in the state's palette.</summary>
	/// <param name="colors">The set of colors.</param>
	/// <param name="firstcolor">Offset of the first color to replace.</param>
	/// <param name="ncolors">Amount of colors to replace.</param>
	public void SetStatePalette(ReadOnlySpan<Rgba> colors, int firstcolor = 0, int ncolors = 256)
	{
		colors[..Math.Min(ncolors, colors.Length)].CopyTo(_palette.AsSpan(firstcolor));
	}

	/// <summary>Set palette for helper surfaces like cursor or fps counter.</summary>
	public void SetModPalette()
	{
		_game.Cursor.SetPalette(_palette);
		_game.Cursor.Draw();
		_game.FpsCounter.SetPalette(_palette);
		_game.FpsCounter.Draw();
	}

	/// <summary>Loads palettes from the game resources into the state.</summary>
	/// <param name="palette">String ID of the palette to load.</param>
	/// <param name="backpals">BACKPALS.DAT offset to use.</param>
	public void SetStandardPalette(string palette, int backpals = -1)
	{
		var mod = GameMod;
		SetStatePalette(mod.GetPalette(palette).GetColors(), 0, 256);
		_cursorColor = palette switch
		{
			"PAL_GEOSCAPE" => (byte)Mod.GEOSCAPE_CURSOR,
			"PAL_BASESCAPE" => (byte)Mod.BASESCAPE_CURSOR,
			"PAL_UFOPAEDIA" => (byte)Mod.UFOPAEDIA_CURSOR,
			"PAL_GRAPHS" => (byte)Mod.GRAPHS_CURSOR,
			_ => (byte)Mod.BATTLESCAPE_CURSOR,
		};
		if (backpals != -1)
			SetStatePalette(mod.GetPalette("BACKPALS.DAT").GetColors(Acom.Palette.BlockOffset((byte)backpals)), Acom.Palette.BackPos, 16);
		SetModPalette(); // delay actual update to the end
	}

	/// <summary>Loads palettes from the given resources into the state.</summary>
	/// <param name="colors">The set of colors.</param>
	/// <param name="cursorColor">Cursor color to use.</param>
	public void SetCustomPalette(ReadOnlySpan<Rgba> colors, int cursorColor)
	{
		SetStatePalette(colors, 0, 256);
		_cursorColor = (byte)cursorColor;
		SetModPalette(); // delay actual update to the end
	}

	/// <summary>The state's 8bpp palette.</summary>
	public ReadOnlySpan<Rgba> Palette => _palette;

	/// <summary>Let the state know the window has been resized; each state will probably need its own handling.</summary>
	/// <param name="dX">Delta of X.</param>
	/// <param name="dY">Delta of Y.</param>
	public virtual void Resize(ref int dX, ref int dY)
	{
		Recenter(dX, dY);
	}

	/// <summary>Re-orients all the surfaces in the state.</summary>
	/// <param name="dX">Delta of X.</param>
	/// <param name="dY">Delta of Y.</param>
	public virtual void Recenter(int dX, int dY)
	{
		foreach (var surface in _surfaces)
		{
			surface.X = surface.X + dX / 2;
			surface.Y = surface.Y + dY / 2;
		}
	}

#pragma warning disable CA1822 // instance members as in C++; they only reach the static game pointer
	/// <summary>Gets cursor X coordinate.</summary>
	public int CursorX => _game.Cursor.X;

	/// <summary>Gets cursor Y coordinate.</summary>
	public int CursorY => _game.Cursor.Y;
#pragma warning restore CA1822
}
