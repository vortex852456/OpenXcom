namespace Acom.Tests.Engine;

/// <summary>
/// Game tests that change global options and the file map. They need both the static game
/// pointer (GameTests) and the global options (OptionsTests), so they run alone.
/// </summary>
[CollectionDefinition(nameof(GameOptionsTests), DisableParallelization = true)]
public sealed class GameOptionsTestsDefinition;

[Collection(nameof(GameOptionsTests))]
public sealed class GameOptionsTests : IDisposable
{
	private sealed class CountingState : State
	{
		public int Thinks { get; private set; }

		public override void Think() => Thinks++;
	}

	private readonly string _root = Directory.CreateTempSubdirectory("acom-lang-").FullName + "/";
	private readonly string _language = Options.language;
	private readonly TextWrapping _wordwrap = Options.wordwrap;
	private readonly int _pauseMode = Options.pauseMode, _throttle = Options.oxceThrottleMouseMoveEvent;
	private readonly bool _debug = Options.debug, _debugUi = Options.debugUi, _backgroundMute = Options.backgroundMute;
	private readonly SDL_GrabMode _captureMouse = Options.captureMouse;

	public void Dispose()
	{
		Options.language = _language;
		Options.wordwrap = _wordwrap;
		Options.pauseMode = _pauseMode;
		Options.oxceThrottleMouseMoveEvent = _throttle;
		Options.debug = _debug;
		Options.debugUi = _debugUi;
		Options.backgroundMute = _backgroundMute;
		Options.captureMouse = _captureMouse;
		FileMap.Clear(clearOnly: true, embeddedOnly: false);
		Directory.Delete(_root, recursive: true);
	}

	private void Write(string path, string content)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(_root + path)!);
		File.WriteAllText(_root + path, content);
	}

	/// <summary>Maps a "common" layer (0) and the master mod "xcom1" (layer 1).</summary>
	private void SetupFileMap()
	{
		Write("data/common/Language/en-US.yml", "en-US:\n  STR_A: common A\n  STR_B: common B\n  STR_C: common C");
		Write("data/common/Language/de.yml", "de:\n  STR_A: Gemeinsam A");
		Write("data/common/Language/pt-br.yml", "pt-BR:\n  STR_A: comum A");
		Write("data/common/Language/qq.yml", "qq:\n  STR_A: qq A");
		Write("data/common/Language/OXCE/en-US.yml", "en-US:\n  STR_B: oxce B");
		Write("data/common/Language/OXCE/de.yml", "de:\n  STR_C: OXCE C");
		// "common" needs at least 6 entries to be recognized (CrossPlatform::searchDataFolder)
		Write("data/common/Resources/a.png", "");
		Write("data/common/Palettes/p.txt", "");
		Write("data/common/Shaders/s.txt", "");
		Write("data/common/SoldierName/n.nam", "");
		Write("data/common/icon.png", "");
		Write("data/standard/xcom1/metadata.yml", "isMaster: true");
		Write("data/standard/xcom1/Rulesets/a.rul", "items: []");
		Write("data/standard/xcom1/Language/en-US.yml", "en-US:\n  STR_A: mod A");
		Write("data/standard/xcom1/Language/fr.yml", "fr:\n  STR_A: mod A fr");
		Options.SetFoldersForTest(_root + "data/", _root + "user/", _root + "user/");

		FileMap.Clear(clearOnly: false, embeddedOnly: false);
		FileMap.ScanModDir(Options.GetDataFolder(), "standard", protectedLocation: true);
		FileMap.CheckModsDependencies();
		FileMap.Setup([FileMap.GetModInfos()["xcom1"]], embeddedOnly: false);
	}

	[Fact]
	public void GetList_lists_the_common_language_files_sorted()
	{
		SetupFileMap();
		List<string> ids = ["old"], names = ["old"];
		Language.GetList(ids, names);
		// mod layers do not add languages (fr.yml), file names are upper-cased after the dash
		Assert.Equal(["de", "en-US", "pt-BR", "qq"], ids);
		Assert.Equal(["Deutsch", "English (US)", "Português (BR)", "qq"], names);
	}

	[Fact]
	public void LoadLanguages_loads_default_and_current_language_in_layer_lockstep()
	{
		SetupFileMap();
		var game = new Game(new Screen(4, 4));
		Options.wordwrap = TextWrapping.WRAP_AUTO;

		Options.language = "de";
		game.LoadLanguages();
		Assert.Equal("de", Options.language);
		var lang = game.Language;
		// layer 0: en-US, de, OXCE/en-US, OXCE/de; then layer 1: en-US of the mod
		Assert.Equal("mod A", lang.GetString("STR_A").ToString());
		Assert.Equal("oxce B", lang.GetString("STR_B").ToString());
		Assert.Equal("OXCE C", lang.GetString("STR_C").ToString());

		// unknown language: fall back to the default
		Options.language = "zz";
		game.LoadLanguages();
		Assert.Equal("en-US", Options.language);
		Assert.NotSame(lang, game.Language);
		Assert.Equal("common C", game.Language.GetString("STR_C").ToString());
	}

	[Fact]
	public void Focus_changes_pause_or_slow_down_the_loop_by_pause_mode()
	{
		var game = new Game(new Screen(4, 4));
		var state = new CountingState();
		game.PushState(state);
		game.RunFrame();
		Assert.Equal(1, state.Thinks);
		const byte all = SDLApp.SDL_APPMOUSEFOCUS | SDLApp.SDL_APPINPUTFOCUS | SDLApp.SDL_APPACTIVE;

		// only the mouse focus changed: ignored
		Options.pauseMode = 3;
		game.AppState = SDLApp.SDL_APPACTIVE;
		game.PushEvent(SDL_Event.ActiveEvent(0, SDLApp.SDL_APPMOUSEFOCUS));
		Assert.True(game.RunFrame());
		Assert.Equal(ApplicationState.RUNNING, game.RunningState);

		// keyboard focus lost: pauseMode 3 pauses (no logic, no rendering)
		game.PushEvent(SDL_Event.ActiveEvent(0, SDLApp.SDL_APPINPUTFOCUS));
		Assert.False(game.RunFrame());
		Assert.Equal(ApplicationState.PAUSED, game.RunningState);
		Assert.Equal(2, state.Thinks);

		// mouse activity regains focus
		Options.oxceThrottleMouseMoveEvent = 0;
		game.PushEvent(SDL_Event.MouseMotion(1, 1));
		Assert.True(game.RunFrame());
		Assert.Equal(ApplicationState.RUNNING, game.RunningState);
		Assert.Equal(3, state.Thinks);

		// minimized with pauseMode 0: slowed down, still running
		Options.pauseMode = 0;
		game.AppState = 0;
		game.PushEvent(SDL_Event.ActiveEvent(0, SDLApp.SDL_APPACTIVE | SDLApp.SDL_APPINPUTFOCUS));
		Assert.True(game.RunFrame());
		Assert.Equal(ApplicationState.SLOWED, game.RunningState);

		// focus back
		game.AppState = all;
		game.PushEvent(SDL_Event.ActiveEvent(1, SDLApp.SDL_APPACTIVE | SDLApp.SDL_APPINPUTFOCUS));
		game.RunFrame();
		Assert.Equal(ApplicationState.RUNNING, game.RunningState);
	}

	private sealed class MotionState : State
	{
		public List<(int X, int XRel, int YRel)> Motions { get; } = [];

		public override void Handle(Acom.Action action)
		{
			if (action.Details.type == SDLEventType.SDL_MOUSEMOTION)
				Motions.Add((action.Details.motion.x, action.Details.motion.xrel, action.Details.motion.yrel));
			base.Handle(action);
		}
	}

	[Fact]
	public void Throttled_mouse_motion_is_merged_into_the_next_passing_event()
	{
		var oldSource = SDLTimer.Source;
		uint now = 100;
		SDLTimer.Source = () => now;
		try
		{
			var game = new Game(new Screen(4, 4));
			var state = new MotionState();
			game.PushState(state);
			game.RunFrame();
			state.Motions.Clear(); // mouse refresh of the init

			Options.oxceThrottleMouseMoveEvent = 10;
			// the first event starts the interval and is merged as well (the clock is read when the event is processed)
			game.PushEvent(SDL_Event.MouseMotion(1, 0, 3, -1));
			game.RunFrame();
			now = 105;
			game.PushEvent(SDL_Event.MouseMotion(2, 0, 4, -2));
			game.RunFrame();
			Assert.Empty(state.Motions);
			now = 110;
			game.PushEvent(SDL_Event.MouseMotion(3, 0, 5, -3));
			game.RunFrame();
			Assert.Equal([(3, 12, -6)], state.Motions);
			// the interval restarts with the next event
			game.PushEvent(SDL_Event.MouseMotion(4, 0, 1, 1));
			game.RunFrame();
			Assert.Single(state.Motions);
		}
		finally
		{
			SDLTimer.Source = oldSource;
		}
	}

	[Fact]
	public void Global_shortcuts_toggle_mouse_grab_and_debug_ui()
	{
		var game = new Game(new Screen(4, 4));
		game.PushState(new CountingState());
		game.RunFrame();
		Options.captureMouse = SDL_GrabMode.SDL_GRAB_OFF;
		Options.debug = false;
		Options.debugUi = false;

		game.PushEvent(SDL_Event.KeyDown(SDLKey.SDLK_g, SDLMod.KMOD_LCTRL));
		game.PushEvent(SDL_Event.KeyDown(SDLKey.SDLK_u, SDLMod.KMOD_LCTRL));
		game.RunFrame();
		Assert.Equal(SDL_GrabMode.SDL_GRAB_ON, Options.captureMouse);
		Assert.False(Options.debugUi); // debug shortcuts need Options::debug

		Options.debug = true;
		game.PushEvent(SDL_Event.KeyDown(SDLKey.SDLK_u, SDLMod.KMOD_LCTRL));
		game.PushEvent(SDL_Event.KeyDown(SDLKey.SDLK_u));
		game.RunFrame();
		Assert.True(Options.debugUi);
		Assert.False(game.IsCtrlPressed());
	}
}
