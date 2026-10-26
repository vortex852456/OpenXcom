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
// Ported from: src/Menu/LoadGameState.h, src/Menu/LoadGameState.cpp
#nullable disable

namespace Acom;

/// <summary>Loads a saved game, with an optional message.</summary>
public class LoadGameState : State
{
	private int _firstRun;
	private OptionsOrigin _origin;
	private Text _txtStatus;
	private string _filename = "";

	/// <summary>Initializes all the elements in the Load Game screen.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	/// <param name="filename">Name of the save file without extension.</param>
	/// <param name="palette">Parent state palette.</param>
	public LoadGameState(OptionsOrigin origin, string filename, ReadOnlySpan<Rgba> palette)
	{
		_firstRun = 0;
		_origin = origin;
		_filename = filename;
		BuildUi(palette);
	}

	/// <summary>Initializes all the elements in the Load Game screen.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	/// <param name="type">Type of auto-load being used.</param>
	/// <param name="palette">Parent state palette.</param>
	public LoadGameState(OptionsOrigin origin, SaveType type, ReadOnlySpan<Rgba> palette)
	{
		_firstRun = 0;
		_origin = origin;
		switch (type)
		{
			case SaveType.SAVE_QUICK:
				_filename = SavedGame.QUICKSAVE;
				break;
			case SaveType.SAVE_AUTO_GEOSCAPE:
				_filename = SavedGame.AUTOSAVE_GEOSCAPE;
				break;
			case SaveType.SAVE_AUTO_BATTLESCAPE:
				_filename = SavedGame.AUTOSAVE_BATTLESCAPE;
				break;
			default:
				// can't auto-load ironman games
				break;
		}

		BuildUi(palette);
	}

	/// <summary>Builds the interface.</summary>
	/// <param name="palette">Parent state palette.</param>
	public void BuildUi(ReadOnlySpan<Rgba> palette)
	{
		_screen = false;

		// Create objects
		_txtStatus = new Text(320, 17, 0, 92);

		// Set palette
		SetStatePalette(palette);

		if (_origin == OptionsOrigin.OPT_BATTLESCAPE)
		{
			Add(_txtStatus, "textLoad", "battlescape");
			_txtStatus.SetHighContrast(true);
			if (_game.SavedGame.SavedBattle.AmbientSound != Mod.NO_SOUND)
			{
				_game.Mod.GetSoundByDepth(0, unchecked((uint)_game.SavedGame.SavedBattle.AmbientSound)).StopLoop();
			}
		}
		else
		{
			Add(_txtStatus, "textLoad", "geoscape");
		}

		CenterAllSurfaces();

		// Set up objects
		_txtStatus.SetBig();
		_txtStatus.Align = TextHAlign.ALIGN_CENTER;
		_txtStatus.SetText(Tr("STR_LOADING_GAME"));
	}

	/// <summary>Ignore quick loads without a save available.</summary>
	public override void Init()
	{
		base.Init();
		if (_filename == SavedGame.QUICKSAVE && !CrossPlatform.FileExists(Options.GetMasterUserFolder() + _filename))
		{
			_game.PopState();
			return;
		}
	}

	/// <summary>Loads the specified save.</summary>
	public override void Think()
	{
		base.Think();
		// Make sure it gets drawn properly
		if (_firstRun < 10)
		{
			_firstRun++;
		}
		else
		{
			_game.PopState();

			// Remember for later (palette reset)
			BattlescapeState origBattleState = null;
			if (_game.SavedGame != null && _game.SavedGame.SavedBattle != null)
			{
				origBattleState = _game.SavedGame.SavedBattle.BattleState;
			}

			// Reset touch flags
			_game.ResetTouchButtonFlags();

			// Load the game
			var s = new SavedGame();
			try
			{
				s.Load(_filename, _game.Mod, _game.Language);
				_game.SavedGame = s;
				if (_game.SavedGame.Ending != GameEnding.END_NONE)
				{
					Options.baseXResolution = Screen.ORIGINAL_WIDTH;
					Options.baseYResolution = Screen.ORIGINAL_HEIGHT;
					_game.Screen.ResetDisplay(false);
					_game.SetState(new StatisticsState());
				}
				else
				{
					Options.baseXResolution = Options.baseXGeoscape;
					Options.baseYResolution = Options.baseYGeoscape;
					_game.Screen.ResetDisplay(false);
					if (origBattleState != null)
					{
						// We need to reset palettes here already, can't wait for the destructor
						origBattleState.ResetPalettes();
					}
					_game.SetState(new GeoscapeState());
					if (_game.SavedGame.SavedBattle != null)
					{
						_game.SavedGame.SavedBattle.LoadMapResources(_game.Mod);
						Options.baseXResolution = Options.baseXBattlescape;
						Options.baseYResolution = Options.baseYBattlescape;
						_game.Screen.ResetDisplay(false);
						var bs = new BattlescapeState();
						_game.PushState(bs);
						_game.SavedGame.SavedBattle.BattleState = bs;
						// Try to reactivate the touch buttons
						bs.ToggleTouchButtons(false, true);
					}
				}

				// Clear the SDL event queue (i.e. ignore input from impatient users)
				_game.ClearEvents();
			}
			catch (Exception e) when (ListGamesState.IsSaveError(e))
			{
				Error(e.Message, s);
			}
			CrossPlatform.FlashWindow();
		}
	}

	/// <summary>Pops up a window with an error message and cleans up afterwards.</summary>
	/// <param name="msg">Error message.</param>
	/// <param name="save">Pending save.</param>
	public void Error(string msg, SavedGame save)
	{
		Logger.Error(msg);
		string error = Tr("STR_LOAD_UNSUCCESSFUL") + Unicode.TOK_NL_SMALL + msg;
		var errorMessages = _game.Mod.GetInterface("errorMessages");
		if (_origin != OptionsOrigin.OPT_BATTLESCAPE)
			_game.PushState(new ErrorMessageState(error, _palette, unchecked((byte)errorMessages.GetElement("geoscapeColor").color), "BACK01.SCR", errorMessages.GetElement("geoscapePalette").color));
		else
			_game.PushState(new ErrorMessageState(error, _palette, unchecked((byte)errorMessages.GetElement("battlescapeColor").color), "TAC00.SCR", errorMessages.GetElement("battlescapePalette").color));

		if (_game.SavedGame == save)
			_game.SavedGame = null;
		// else: C++ deletes the pending save; the garbage collector does that here.
	}
}
