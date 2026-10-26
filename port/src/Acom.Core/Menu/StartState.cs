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
// Ported from: src/Menu/StartState.h, src/Menu/StartState.cpp
// Deviation: loading runs on a Task instead of an SDL thread; a running load cannot be killed when the state is dropped.
// Deviation: the terminal names the program "ACOM" (the C++ text says "openxcom"/"OpenXcom").
#nullable disable

using System.Text;
using System.Threading.Tasks;

namespace Acom;

public enum LoadingPhase : uint
{
	LOADING_STARTED = 0,
	LOADING_FAILED = 1,
	LOADING_SUCCESSFUL = 2,
	LOADING_DONE = 3,
}

/// <summary>
/// Initializes the game and loads all required content.
/// </summary>
public class StartState : State
{
	private readonly Text _text, _cursor;
	private readonly Font _font;
	private readonly Timer _timer;
	private readonly Language _lang;
	private int _anim;
	private readonly string _oldMaster;

	private Task _thread;
	private readonly StringBuilder _output = new();

#pragma warning disable CA2211 // public static fields as in C++ (written by the loading task)
	public static volatile LoadingPhase loading;
	public static volatile string error;
#pragma warning restore CA2211

	/// <summary>
	/// Initializes all the elements in the Loading screen.
	/// </summary>
	public StartState()
	{
		_anim = 0;
		//updateScale() uses newDisplayWidth/Height and needs to be set ahead of time
		Options.newDisplayWidth = Options.displayWidth;
		Options.newDisplayHeight = Options.displayHeight;
		Screen.UpdateScale(Options.geoscapeScale, ref Options.baseXGeoscape, ref Options.baseYGeoscape, false);
		Screen.UpdateScale(Options.battlescapeScale, ref Options.baseXBattlescape, ref Options.baseYBattlescape, false);
		Options.baseXResolution = Options.displayWidth;
		Options.baseYResolution = Options.displayHeight;
		_game.Screen.ResetDisplay(false, true);

		// Create objects
		_thread = null;
		loading = LoadingPhase.LOADING_STARTED;
		error = "";
		_oldMaster = Options.GetActiveMaster();

		_font = new Font();
		_font.LoadTerminal();
		_lang = new Language();

		_text = new Text(Options.baseXResolution, Options.baseYResolution, 0, 0);
		_cursor = new Text(_font.Width, _font.Height, 0, 0);
		_timer = new Timer(150);

		SetStatePalette(Font.TerminalColors, 0, Font.TerminalColors.Length);

		Add(_text);
		Add(_cursor);

		// Set up objects
		_text.InitText(_font, _font, _lang);
		_text.SetColor(0);
		_text.SetWordWrap(true);

		_cursor.InitText(_font, _font, _lang);
		_cursor.SetColor(0);
		_cursor.SetText("_");

		_timer.OnTimer((StateHandler)Animate);
		_timer.Start();

		// Hide UI
		_game.Cursor.Visible = false;
		_game.FpsCounter.Visible = false;

		if (Options.reload)
		{
			if (Options.oxceStartUpTextMode < 2)
			{
				AddLine("Restarting...");
				AddLine("");
			}
		}
		else
		{
			if (Options.oxceStartUpTextMode < 2)
			{
				AddLine(CrossPlatform.GetDosPath() + ">acom");
			}
		}
	}

	/// <summary>The loading task (null before <see cref="Init"/>), for hosts and tests that need to wait for it.</summary>
	public Task LoadingTask => _thread;

	/// <summary>
	/// Reset and reload data.
	/// </summary>
	public override void Init()
	{
		base.Init();

		// Silence!
		Sound.Stop();
		Music.Stop();
		if (!Options.mute && Options.reload)
		{
			Mixer.CloseAudio();
			_game.InitAudio();
		}

		// Load the game data in a separate thread
		var game = _game;
		try
		{
			_thread = Task.Run(() => Load(game));
		}
		catch (InvalidOperationException)
		{
			// If we can't create the thread, just load it as usual
			Load(game);
		}
	}

	/// <summary>
	/// If the loading fails, it shows an error, otherwise moves on to the game.
	/// </summary>
	public override void Think()
	{
		base.Think();
		_timer.Think(this, null);

		switch (loading)
		{
			case LoadingPhase.LOADING_FAILED:
				CrossPlatform.FlashWindow();
				AddLine("");
				AddLine("ERROR: " + error);
				AddLine("");
				AddLine("More details here: " + CrossPlatform.GetLogFileName());
				AddLine("Make sure ACOM and any mods are installed correctly.");
				AddLine("");
				AddLine("Press any key to continue.");
				loading = LoadingPhase.LOADING_DONE;
				break;
			case LoadingPhase.LOADING_SUCCESSFUL:
				CrossPlatform.FlashWindow();
				Logger.Info("ACOM started successfully!");
				_game.SetState(new GoToMainMenuState(true));
				if (_oldMaster != Options.GetActiveMaster() && Options.playIntro)
				{
					_game.PushState(new CutsceneState("intro"));
				}
				if (Options.reload)
				{
					Options.reload = false;
				}
				_game.Cursor.Visible = true;
				_game.FpsCounter.Visible = Options.fpsCounter;
				break;
			default:
				break;
		}
	}

	/// <summary>
	/// The game quits if the player presses any key when an error
	/// message is on display.
	/// </summary>
	public override void Handle(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		base.Handle(action);
		if (loading == LoadingPhase.LOADING_DONE)
		{
			if (action.Details.type == SDLEventType.SDL_KEYDOWN)
			{
				_game.Quit();
			}
		}
	}

	/// <summary>
	/// Blinks the cursor and spreads out terminal output.
	/// </summary>
	public void Animate()
	{
		_cursor.Visible = !_cursor.Visible;
		_anim++;

		if (loading == LoadingPhase.LOADING_STARTED)
		{
			string ss = $"Loading ACOM {Version.ACOM_VERSION} (OpenXcom {Version.OPENXCOM_VERSION_SHORT})...";
			if (Options.reload)
			{
				if (Options.oxceStartUpTextMode < 2)
				{
					if (_anim == 2)
						AddLine(ss);
				}
			}
			else
			{
				switch (_anim)
				{
					case 1:
						if (Options.oxceStartUpTextMode < 1)
						{
							AddLine("DOS/4GW Protected Mode Run-time  Version 1.9");
							AddLine("Copyright (c) Rational Systems, Inc. 1990-1993");
						}
						break;
					case 6:
						if (Options.oxceStartUpTextMode < 2)
						{
							AddLine("");
							AddLine("ACOM initialisation");
						}
						break;
					case 7:
						if (Options.oxceStartUpTextMode < 1)
						{
							AddLine("");
							if (Options.mute)
							{
								AddLine("No Sound Detected");
							}
							else
							{
								AddLine("SoundBlaster Sound Effects");
								if (Options.preferredMusic == MusicFormat.MUSIC_MIDI)
									AddLine("General MIDI Music");
								else
									AddLine("SoundBlaster Music");
								AddLine("Base Port 220  Irq 7  Dma 1");
							}
						}
						if (Options.oxceStartUpTextMode < 2)
						{
							AddLine("");
						}
						break;
					case 9:
						if (Options.oxceStartUpTextMode < 2)
						{
							AddLine(ss);
						}
						break;
				}
			}
		}
	}

	/// <summary>
	/// Adds a line of text to the terminal and moves
	/// the cursor appropriately.
	/// </summary>
	public void AddLine(string str)
	{
		_output.Append('\n').Append(str);
		_text.SetText(_output.ToString());
		int y = _text.GetTextHeight() - _font.Height;
		int x = _text.GetTextWidth(y / _font.Height);
		_cursor.X = x;
		_cursor.Y = y;
	}

	/// <summary>
	/// Loads game data and updates status accordingly.
	/// </summary>
	/// <param name="game_ptr">The game.</param>
	/// <returns>Thread status, 0 = ok</returns>
#pragma warning disable CA1031 // C++ catches std::exception: any loading error is shown on the terminal
	public static int Load(Game game_ptr)
	{
		ArgumentNullException.ThrowIfNull(game_ptr);
		Game game = game_ptr;
		try
		{
			Logger.Info("Loading data...");
			Options.UpdateMods();
			game.LoadMods();
			Logger.Info("Data loaded successfully.");
			Logger.Info("Loading language...");
			game.LoadLanguages();
			Logger.Info("Language loaded successfully.");
			loading = LoadingPhase.LOADING_SUCCESSFUL;
		}
		catch (Exception e)
		{
			error = e.Message;
			Logger.Error(error);
			loading = LoadingPhase.LOADING_FAILED;
		}

		return 0;
	}
#pragma warning restore CA1031
}
