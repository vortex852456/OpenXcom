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
// Port status: partial (missing: audio backend (initAudio opens no device, setVolume only records the Mix_Volume levels), Screen::handle, SDL_VIDEORESIZE (Screen::updateScale/resetDisplay), mouse grab (host), frame pacing (Options::FPS/FPSInactive, SDL_Delay: host))

namespace Acom;

/// <summary>Run mode of the main loop, depending on the window focus and Options::pauseMode.</summary>
public enum ApplicationState
{
	RUNNING = 0,
	SLOWED = 1,
	PAUSED = 2,
}

/// <summary>
/// The core of the game engine, manages the game's entire contents and structure.
/// Provides access to all the game's resources and contains a stack state machine to handle
/// all the initializations, events and blits of each state, as well as transitions.
/// </summary>
/// <remarks>
/// The C++ version owns the main loop. Here the platform host (MonoGame) owns it: it queues
/// input with <see cref="PushEvent"/>, calls <see cref="Update"/> every tick and <see cref="Render"/>
/// whenever a frame is drawn. As in the C++ loop, logic runs every iteration while blitting may be
/// skipped; frame pacing (Options::FPS, SDL_Delay) is the host's job, see <see cref="RunningState"/>.
/// </remarks>
public sealed class Game
{
	private const double VOLUME_GRADIENT = 10.0;

	/// <summary>Maximum volume of SDL_mixer (MIX_MAX_VOLUME).</summary>
	public const int SDL_MIX_MAXVOLUME = 128;

	/// <summary>Number of mixing channels (Mix_AllocateChannels in initAudio).</summary>
	public const int MIX_CHANNELS = 16;

	private static readonly ApplicationState[] s_kbFocusRun = [ApplicationState.RUNNING, ApplicationState.RUNNING, ApplicationState.SLOWED, ApplicationState.PAUSED];
	private static readonly ApplicationState[] s_stateRun = [ApplicationState.SLOWED, ApplicationState.PAUSED, ApplicationState.PAUSED, ApplicationState.PAUSED];

	private readonly Screen _screen;
	private readonly Cursor? _cursor;
	private Language _lang;
	private readonly List<State> _states = [];
	private readonly List<State> _deleted = [];
	private SavedGame? _save;
	private Mod? _mod;
	private bool _quit, _init, _update;
	private readonly FpsCounter? _fpsCounter;
	private bool _mouseActive = true;
	private bool _ctrl, _alt, _shift, _rmb, _mmb;
	private int _scrollStep = 1;

	// Locals of Game::run() that live as long as the loop.
	private readonly Queue<SDL_Event> _events = new();
	private ApplicationState _runningState = ApplicationState.RUNNING;
	private uint _lastMouseMoveEvent;
	private short _xrel, _yrel;
	private int _mouseX, _mouseY;

	// Volumes set through Mix_Volume/Mix_VolumeMusic; the audio backend applies them once ported.
	private readonly int[] _mixChannelVolume = Enumerable.Repeat(SDL_MIX_MAXVOLUME, MIX_CHANNELS).ToArray();
	private int _mixMusicVolume = SDL_MIX_MAXVOLUME;

	/// <summary>
	/// Starts up the engine with the given screen and sets up the cursor.
	/// In C++ this also initializes SDL and creates the window and screen; here the host does that.
	/// </summary>
	public Game(Screen screen)
	{
		ArgumentNullException.ThrowIfNull(screen);
		Options.reload = false;
		Options.mute = false;

		// Initialize SDL_mixer
		InitAudio();

		// Mouse grab, window icon/caption and unicode input belong to the host.

		// Create display
		_screen = screen;

		// Create cursor
		_cursor = CreateOptional(() => new Cursor(9, 13));

		// Create fps counter
		_fpsCounter = CreateOptional(() => new FpsCounter(15, 5, 0, 0));

		// Create blank language
		_lang = new Language();

		State.SetGamePtr(this);
	}

	/// <summary>
	/// Bridge while Interface/Cursor and Interface/FpsCounter are generated stubs (their constructors throw):
	/// the game runs without them until they are ported. Remove together with the nullability of
	/// <see cref="Cursor"/> and <see cref="FpsCounter"/>.
	/// </summary>
	private static T? CreateOptional<T>(Func<T> create) where T : class
	{
		try
		{
			return create();
		}
		catch (NotImplementedException)
		{
			return null;
		}
	}

	/// <summary>The display screen used by the game.</summary>
	public Screen Screen => _screen;

	/// <summary>The game's cursor (null while Interface/Cursor is not ported).</summary>
	public Cursor? Cursor => _cursor;

	/// <summary>The FpsCounter (null while Interface/FpsCounter is not ported).</summary>
	public FpsCounter? FpsCounter => _fpsCounter;

	/// <summary>The currently loaded language.</summary>
	public Language Language => _lang;

	/// <summary>The currently loaded saved game (C++ deletes the previous one when a new one is set).</summary>
	public SavedGame? SavedGame { get => _save; set => _save = value; }

	/// <summary>The currently loaded mod (null until <see cref="LoadMods"/>).</summary>
	public Mod? Mod => _mod;

	/// <summary>The update flag.</summary>
	public bool UpdateFlag { get => _update; set => _update = value; }

	/// <summary>The scroll step value.</summary>
	public int ScrollStep { get => _scrollStep; set => _scrollStep = value; }

	/// <summary>Bit mask of currently pressed mouse buttons (replaces SDL_GetMouseState).</summary>
	public byte MouseButtonState { get; private set; }

	/// <summary>Currently pressed modifier keys (replaces SDL_GetModState).</summary>
	public SDLMod ModState { get; set; }

	/// <summary>Focus state of the window (replaces SDL_GetAppState); the host updates it along with SDL_ACTIVEEVENT.</summary>
	public byte AppState { get; set; } = SDLApp.SDL_APPMOUSEFOCUS | SDLApp.SDL_APPINPUTFOCUS | SDLApp.SDL_APPACTIVE;

	/// <summary>Run mode of the loop. While not RUNNING the C++ loop sleeps 100 ms per iteration instead of 1 ms.</summary>
	public ApplicationState RunningState => _runningState;

	/// <summary>The state stack, bottom first.</summary>
	public IReadOnlyList<State> States => _states;

	/// <summary>Channel volumes as set through Mix_Volume (0..<see cref="SDL_MIX_MAXVOLUME"/>).</summary>
	public ReadOnlySpan<int> MixChannelVolumes => _mixChannelVolume;

	/// <summary>Music volume as set through Mix_VolumeMusic.</summary>
	public int MixMusicVolume => _mixMusicVolume;

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

		// Clean up states (C++ deletes them here)
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
			if (CrossPlatform.IsQuitShortcut(ev))
				ev.type = SDLEventType.SDL_QUIT;
			switch (ev.type)
			{
				case SDLEventType.SDL_QUIT:
					Quit();
					break;
				case SDLEventType.SDL_ACTIVEEVENT:
					HandleActiveEvent(ev);
					break;
				case SDLEventType.SDL_VIDEORESIZE:
					// missing: changing the logical resolution with the window (Options::allowResize,
					// Screen::updateScale, State::resize, Screen::resetDisplay); the host letterboxes instead.
					break;
				case SDLEventType.SDL_MOUSEMOTION:
				case SDLEventType.SDL_MOUSEBUTTONDOWN:
				case SDLEventType.SDL_MOUSEBUTTONUP:
					TrackMouse(ev);
					if (ev.type == SDLEventType.SDL_MOUSEMOTION && ThrottleMouseMotion(ev))
						continue;
					// Skip mouse events if they're disabled
					if (!_mouseActive)
						continue;
					// re-gain focus on mouse-over or keypress.
					_runningState = ApplicationState.RUNNING;
					// Go on, feed the event to others
					HandleEvent(ev);
					break;
				default:
					if (ev.type is SDLEventType.SDL_KEYDOWN or SDLEventType.SDL_KEYUP)
						ModState = ev.key.keysym.mod;
					HandleEvent(ev);
					break;
			}
			// Deviation: also stops after a quit (C++ handles the remaining events and draws one more frame).
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
		if (_runningState != ApplicationState.PAUSED)
		{
			_states[^1].Think();
			_fpsCounter?.Think();
		}
	}

	/// <summary>Rendering part of the original main loop: blits the visible states and flips the screen.</summary>
	/// <returns>True if a new frame was flipped to the screen.</returns>
	public bool Render()
	{
		if (_quit || !_init || _states.Count == 0 || _runningState == ApplicationState.PAUSED)
			return false;

		_fpsCounter?.AddFrame();
		_screen.Clear();
		// start at the topmost full-screen state
		int first = _states.Count - 1;
		while (first > 0 && !_states[first].IsScreen)
			--first;
		for (int i = first; i < _states.Count; ++i)
			_states[i].Blit();
		_fpsCounter?.Blit(_screen.Surface);
		_cursor?.Blit(_screen.Surface);
		_screen.Flip();
		return true;
	}

	/// <summary>
	/// End of the original main loop: saves the options, then stops all sounds and music like the
	/// C++ destructor. The host calls this once its loop has ended.
	/// </summary>
#pragma warning disable CA1822 // instance member as in C++
	public void Shutdown()
#pragma warning restore CA1822
	{
		Options.Save();
		Sound.Stop();
		Music.Stop();
	}

	/// <summary>Feeds an event to the screen, cursor, fps counter, global shortcuts and the active state.</summary>
	private void HandleEvent(SDL_Event ev)
	{
		var action = CreateAction(ev);
		// missing: _screen->handle(&action) (debug slow motion, Alt+Enter, screenshot key)
		_cursor?.Handle(action);
		_fpsCounter?.Handle(action);
		if (action.Details.type == SDLEventType.SDL_KEYDOWN)
		{
			var sym = action.Details.key.keysym.sym;
			// "ctrl-g" grab input
			if (sym == SDLKey.SDLK_g && IsCtrlPressed())
			{
				// missing: SDL_WM_GrabInput, the host has to apply the option
				Options.captureMouse = Options.captureMouse == SDL_GrabMode.SDL_GRAB_OFF ? SDL_GrabMode.SDL_GRAB_ON : SDL_GrabMode.SDL_GRAB_OFF;
			}
			// "ctrl-n" notes UI
			else if (sym == SDLKey.SDLK_n && IsCtrlPressed() && !IsAltPressed())
			{
				if (_save != null && !ContainsNotesState())
				{
					var battle = _save.SavedBattle;
					if (battle != null)
					{
						if (!battle.IsBattlescapeStateBusy)
							PushState(new NotesState(OptionsOrigin.OPT_BATTLESCAPE));
					}
					else
					{
						PushState(new NotesState(OptionsOrigin.OPT_GEOSCAPE));
					}
				}
			}
			else if (Options.debug)
			{
				if (sym == SDLKey.SDLK_t && IsCtrlPressed())
				{
					PushState(new TestState());
				}
				// "ctrl-u" debug UI
				else if (sym == SDLKey.SDLK_u && IsCtrlPressed())
				{
					Options.debugUi = !Options.debugUi;
					_states[^1].RedrawText();
				}
			}
		}
		_states[^1].Handle(action);
	}

	/// <summary>Window focus changes switch between running, slowed and paused (Options::pauseMode).</summary>
	private void HandleActiveEvent(SDL_Event ev)
	{
		// An event other than SDL_APPMOUSEFOCUS change happened.
		if ((ev.active.state & ~SDLApp.SDL_APPMOUSEFOCUS) != 0)
		{
			byte currentState = AppState;
			// Game is minimized
			if ((currentState & SDLApp.SDL_APPACTIVE) == 0)
			{
				_runningState = s_stateRun[Options.pauseMode];
				if (Options.backgroundMute)
					SetVolume(0, 0, 0);
			}
			// Game is not minimized but has no keyboard focus.
			else if ((currentState & SDLApp.SDL_APPINPUTFOCUS) == 0)
			{
				_runningState = s_kbFocusRun[Options.pauseMode];
				if (Options.backgroundMute)
					SetVolume(0, 0, 0);
			}
			// Game has keyboard focus.
			else
			{
				_runningState = ApplicationState.RUNNING;
				if (Options.backgroundMute)
					SetVolume(Options.soundVolume, Options.musicVolume, Options.uiVolume);
			}
		}
	}

	/// <summary>Merges mouse motion events that come faster than Options::oxceThrottleMouseMoveEvent ms.</summary>
	/// <returns>True if the event was merged into a later one and must be skipped.</returns>
	private bool ThrottleMouseMotion(SDL_Event ev)
	{
		if (Options.oxceThrottleMouseMoveEvent <= 0)
			return false;
		uint last = SDLTimer.SDL_GetTicks();
		if (_lastMouseMoveEvent == 0)
			_lastMouseMoveEvent = last;
		if (last - _lastMouseMoveEvent < (uint)Options.oxceThrottleMouseMoveEvent)
		{
			// Sint16 arithmetic as in SDL 1.2
			_xrel += (short)ev.motion.xrel;
			_yrel += (short)ev.motion.yrel;
			return true;
		}
		_lastMouseMoveEvent = 0;
		ev.motion.xrel = (short)(ev.motion.xrel + _xrel);
		ev.motion.yrel = (short)(ev.motion.yrel + _yrel);
		_xrel = 0;
		_yrel = 0;
		return false;
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
	/// <remarks>The C++ code has a disabled (#if 0) ironman save here: it caused corrupted saves.</remarks>
	public void Quit() => _quit = true;

	/// <summary>
	/// Changes the audio volume of the music and sound effect channels.
	/// Negative values leave the respective volume unchanged.
	/// </summary>
	/// <param name="sound">Sound volume, from 0 to MIX_MAX_VOLUME.</param>
	/// <param name="music">Music volume, from 0 to MIX_MAX_VOLUME.</param>
	/// <param name="ui">UI volume, from 0 to MIX_MAX_VOLUME.</param>
	public void SetVolume(int sound, int music, int ui)
	{
		if (!Options.mute)
		{
			if (sound >= 0)
			{
				sound = (int)(VolumeExponent(sound) * (double)SDL_MIX_MAXVOLUME);
				MixVolume(-1, sound);
				var battle = _save?.SavedBattle;
				if (battle != null)
				{
					MixVolume(3, (int)(sound * battle.AmbientVolume));
				}
				else
				{
					// channel 3: reserved for ambient sound effect.
					MixVolume(3, sound / 2);
				}
				// channel 4: reserved for unit responses
				MixVolume(4, sound);
			}
			if (music >= 0)
			{
				music = (int)(VolumeExponent(music) * (double)SDL_MIX_MAXVOLUME);
				MixVolumeMusic(music);
			}
			if (ui >= 0)
			{
				ui = (int)(VolumeExponent(ui) * (double)SDL_MIX_MAXVOLUME);
				MixVolume(1, ui);
				MixVolume(2, ui);
			}
		}
	}

	/// <summary>Adjusts a linear volume level to an exponential one.</summary>
	public static double VolumeExponent(int volume)
	{
		return (Math.Exp(Math.Log(VOLUME_GRADIENT + 1.0) * volume / (double)SDL_MIX_MAXVOLUME) - 1.0) / VOLUME_GRADIENT;
	}

	/// <summary>Mix_Volume: sets one channel (or all for -1); negative volumes are ignored, larger ones clamped.</summary>
	private void MixVolume(int which, int volume)
	{
		if (volume < 0)
			return;
		volume = Math.Min(volume, SDL_MIX_MAXVOLUME);
		if (which == -1)
			Array.Fill(_mixChannelVolume, volume);
		else if (which < MIX_CHANNELS)
			_mixChannelVolume[which] = volume;
	}

	/// <summary>Mix_VolumeMusic: negative volumes are ignored, larger ones clamped.</summary>
	private void MixVolumeMusic(int volume)
	{
		if (volume >= 0)
			_mixMusicVolume = Math.Min(volume, SDL_MIX_MAXVOLUME);
	}

	/// <summary>
	/// Pops all the states currently in stack and pushes in the new state.
	/// A shortcut for cleaning up all the old states when they're not necessary like in one-way transitions.
	/// </summary>
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

	/// <summary>Loads the mods specified in the game options.</summary>
	public void LoadMods()
	{
		Mod.ResetGlobalStatics();
		_mod = new Mod();
		_mod.LoadAll();
	}

	/// <summary>
	/// Sets whether the mouse is activated. If it is, mouse events are processed,
	/// otherwise they are ignored and the cursor is hidden.
	/// </summary>
	public void SetMouseActive(bool active)
	{
		_mouseActive = active;
		if (_cursor != null)
			_cursor.Visible = active;
	}

	/// <summary>Is <paramref name="state"/> the current (topmost) state?</summary>
	public bool IsState(State state) => _states.Count > 0 && _states[^1] == state;

	/// <summary>Returns whether a UfopaediaStartState is in the background.</summary>
	public bool ContainsUfopaediaStartState() => _states.Exists(state => state is UfopaediaStartState);

	/// <summary>Returns whether a NotesState is in the background.</summary>
	public bool ContainsNotesState() => _states.Exists(state => state is NotesState);

	/// <summary>The GeoscapeState from the background (if available).</summary>
	public GeoscapeState? GeoscapeState
	{
		get
		{
			foreach (var state in _states)
			{
				if (state is GeoscapeState geoscape)
					return geoscape;
			}
			return null;
		}
	}

	/// <summary>Is the game shutting down?</summary>
	public bool IsQuitting => _quit;

	/// <summary>Loads the most appropriate languages given current system and game options.</summary>
	public void LoadLanguages()
	{
		const string defaultLang = "en-US";
		string currentLang;

		// No language set, detect based on system
		if (string.IsNullOrEmpty(Options.language))
		{
			string locale = CrossPlatform.GetLocale();
			int dash = locale.IndexOf('-', StringComparison.Ordinal);
			string lang = dash < 0 ? locale : locale[..dash];
			// Try to load full locale
			if (Language.IsSupported(locale) && FileMap.FileExists("Language/" + locale + ".yml"))
				currentLang = locale;
			// Try to load language locale
			else if (Language.IsSupported(lang) && FileMap.FileExists("Language/" + lang + ".yml"))
				currentLang = lang;
			// Give up, use default
			else
				currentLang = defaultLang;
		}
		else
		{
			// Use options language; language not found, use default
			currentLang = FileMap.FileExists("Language/" + Options.language + ".yml") ? Options.language : defaultLang;
		}
		Options.language = currentLang;

		_lang = new Language();

		const string dirLanguage = "Language/";
		const string dirLanguageAndroid = "Language/Android/";
		const string dirLanguageOXCE = "Language/OXCE/";
		const string dirLanguageTechnical = "Language/Technical/";

		const string defaultLangYml = defaultLang + ".yml";
		string currentLangYml = currentLang + ".yml";

		// get vertical VFS map slices for the four filenames,
		// then submit frecs in lockstep to the _lang.LoadFile().
		var slice = FileMap.GetSlice(dirLanguage + defaultLangYml);
		var sliceAndroid = FileMap.GetSlice(dirLanguageAndroid + defaultLangYml);
		var sliceOXCE = FileMap.GetSlice(dirLanguageOXCE + defaultLangYml);
		var sliceTechnical = FileMap.GetSlice(dirLanguageTechnical + defaultLangYml);

		var slice2 = FileMap.GetSlice(dirLanguage + currentLangYml);
		var sliceAndroid2 = FileMap.GetSlice(dirLanguageAndroid + currentLangYml);
		var sliceOXCE2 = FileMap.GetSlice(dirLanguageOXCE + currentLangYml);
		var sliceTechnical2 = FileMap.GetSlice(dirLanguageTechnical + currentLangYml);

		void Load(FileMap.FileRecord? frec)
		{
			if (frec != null)
				_lang.LoadFile(frec);
		}

		bool twoLangs = currentLang != defaultLang;
		for (int i = 0; i < slice.Count; ++i)
		{
			Load(slice[i]);
			if (twoLangs)
				Load(slice2[i]);
			Load(sliceAndroid[i]);
			if (twoLangs)
				Load(sliceAndroid2[i]);
			Load(sliceOXCE[i]);
			if (twoLangs)
				Load(sliceOXCE2[i]);
			Load(sliceTechnical[i]);
			if (twoLangs)
				Load(sliceTechnical2[i]);
		}

		// Deviation: C++ always has a mod here; tolerated for tools and tests without rulesets.
		if (_mod != null)
		{
			_lang.LoadRule(_mod.ExtraStrings, defaultLang);
			if (twoLangs)
				_lang.LoadRule(_mod.ExtraStrings, currentLang);
		}
	}

	/// <summary>Initializes the audio subsystem.</summary>
	/// <remarks>
	/// Port status: there is no audio backend yet, so this behaves like the C++ code when SDL
	/// finds no sound device (audio disabled through Options::mute).
	/// </remarks>
#pragma warning disable CA1822 // instance member as in C++
	public void InitAudio()
#pragma warning restore CA1822
	{
		Logger.Error("No audio backend in this port yet.");
		Logger.Warning("No sound device detected, audio disabled.");
		Options.mute = true;
	}

	/// <summary>Is CTRL pressed?</summary>
	public bool IsCtrlPressed(bool considerTouchButtons = false) => (considerTouchButtons && _ctrl) || (ModState & SDLMod.KMOD_CTRL) != 0;

	/// <summary>Is ALT pressed?</summary>
	public bool IsAltPressed(bool considerTouchButtons = false) => (considerTouchButtons && _alt) || (ModState & SDLMod.KMOD_ALT) != 0;

	/// <summary>Is SHIFT pressed?</summary>
	public bool IsShiftPressed(bool considerTouchButtons = false) => (considerTouchButtons && _shift) || (ModState & SDLMod.KMOD_SHIFT) != 0;

	/// <summary>Is LMB pressed?</summary>
	public bool IsLeftClick(Action action, bool considerTouchButtons = false)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (considerTouchButtons)
			return action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT && !_rmb && !_mmb;
		return action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT;
	}

	/// <summary>Is RMB pressed?</summary>
	public bool IsRightClick(Action action, bool considerTouchButtons = false)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (considerTouchButtons && action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT && _rmb)
			return true;
		return action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT;
	}

	/// <summary>Is MMB pressed?</summary>
	public bool IsMiddleClick(Action action, bool considerTouchButtons = false)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (considerTouchButtons && action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT && _mmb)
			return true;
		return action.Details.button.button == SDLMouse.SDL_BUTTON_MIDDLE;
	}

	/// <summary>Resets the touch button flags.</summary>
	public void ResetTouchButtonFlags()
	{
		_ctrl = false;
		_alt = false;
		_shift = false;
		_rmb = false;
		_mmb = false;
		_scrollStep = 1;
	}

	/// <summary>Touch-button emulation flags (mobile UI).</summary>
	public bool CtrlPressedFlag { get => _ctrl; set => _ctrl = value; }

	public bool AltPressedFlag { get => _alt; set => _alt = value; }

	public bool ShiftPressedFlag { get => _shift; set => _shift = value; }

	public bool RMBFlag { get => _rmb; set => _rmb = value; }

	public bool MMBFlag { get => _mmb; set => _mmb = value; }

	public void ToggleCtrlPressedFlag() => _ctrl = !_ctrl;

	public void ToggleAltPressedFlag() => _alt = !_alt;

	public void ToggleShiftPressedFlag() => _shift = !_shift;

	public void ToggleRMBFlag() => _rmb = !_rmb;

	public void ToggleMMBFlag() => _mmb = !_mmb;
}
