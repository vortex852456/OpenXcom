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
// Ported from: src/Menu/SaveGameState.h, src/Menu/SaveGameState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

/// <summary>Saves the current game, with an optional message.</summary>
public class SaveGameState : State
{
	private int _firstRun;
	private OptionsOrigin _origin;
	private Text _txtStatus;
	private string _filename = "";
	private SaveType _type;

	/// <summary>Initializes all the elements in the Save Game screen.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	/// <param name="filename">Name of the save file without extension.</param>
	/// <param name="palette">Parent state palette.</param>
	public SaveGameState(OptionsOrigin origin, string filename, ReadOnlySpan<Rgba> palette)
	{
		_firstRun = 0;
		_origin = origin;
		_filename = filename;
		_type = SaveType.SAVE_DEFAULT;
		BuildUi(palette);
	}

	/// <summary>Initializes all the elements in the Save Game screen.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	/// <param name="type">Type of auto-save being used.</param>
	/// <param name="palette">Parent state palette.</param>
	/// <param name="currentTurn">Current turn/day, for multi-slot autosaves.</param>
	public SaveGameState(OptionsOrigin origin, SaveType type, ReadOnlySpan<Rgba> palette, int currentTurn = 0)
	{
		_firstRun = 0;
		_origin = origin;
		_type = type;
		switch (type)
		{
			case SaveType.SAVE_QUICK:
				_filename = SavedGame.QUICKSAVE;
				break;
			case SaveType.SAVE_INSTA:
				_filename = "Instasave_" + CrossPlatform.SanitizeFilename(CrossPlatform.Now()) + ".sav";
				break;
			case SaveType.SAVE_AUTO_GEOSCAPE:
				if (Options.oxceGeoAutosaveFrequency > 0 && Options.oxceGeoAutosaveSlots >= 2 && Options.oxceGeoAutosaveSlots <= 10 && currentTurn > 0)
				{
					// multi-slot autosave
					int slotIndex = (currentTurn / Options.oxceGeoAutosaveFrequency) % Options.oxceGeoAutosaveSlots;
					_filename = "_" + slotIndex.ToString(CultureInfo.InvariantCulture) + SavedGame.AUTOSAVE_GEOSCAPE;
				}
				else
				{
					// classic autosave
					_filename = SavedGame.AUTOSAVE_GEOSCAPE;
				}
				break;
			case SaveType.SAVE_AUTO_BATTLESCAPE:
				if (currentTurn > 0 && Options.autosaveSlots >= 2 && Options.autosaveSlots <= 10)
				{
					// multi-slot autosave
					int slotIndex = (currentTurn / Options.autosaveFrequency) % Options.autosaveSlots;
					_filename = "_" + slotIndex.ToString(CultureInfo.InvariantCulture) + SavedGame.AUTOSAVE_BATTLESCAPE;
				}
				else
				{
					// classic autosave
					_filename = SavedGame.AUTOSAVE_BATTLESCAPE;
				}
				break;
			case SaveType.SAVE_IRONMAN:
			case SaveType.SAVE_IRONMAN_END:
				_filename = CrossPlatform.SanitizeFilename(_game.SavedGame.Name) + ".sav";
				break;
			default:
				break;
		}

		BuildUi(palette);
	}

	/// <summary>The target file name (inside the master user folder).</summary>
	internal string Filename => _filename;

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
		}
		else
		{
			Add(_txtStatus, "textLoad", "geoscape");
		}

		CenterAllSurfaces();

		// Set up objects
		_txtStatus.SetBig();
		_txtStatus.Align = TextHAlign.ALIGN_CENTER;
		_txtStatus.SetText(Tr("STR_SAVING_GAME"));
	}

	/// <summary>Saves the current save.</summary>
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

			switch (_type)
			{
				case SaveType.SAVE_DEFAULT:
					// manual save, close the save screen
					_game.PopState();
					if (!_game.SavedGame.IsIronman)
					{
						// and pause screen too
						_game.PopState();
					}
					break;
				case SaveType.SAVE_INSTA:
					// timestamp is visible already, no need to repeat it
					_game.SavedGame.Name = Tr("STR_INSTA_SAVE");
					break;
				case SaveType.SAVE_QUICK:
				case SaveType.SAVE_AUTO_GEOSCAPE:
				case SaveType.SAVE_AUTO_BATTLESCAPE:
					// automatic save, give it a default name
					_game.SavedGame.Name = _filename;
					break;
				default:
					break;
			}

			// Save the game
			try
			{
				string backup = _filename + ".bak";
				_game.SavedGame.Save(backup, _game.Mod);
				string fullPath = Options.GetMasterUserFolder() + _filename;
				string bakPath = Options.GetMasterUserFolder() + backup;
				if (!CrossPlatform.MoveFile(bakPath, fullPath))
				{
					throw new InvalidOperationException("Save backed up in " + backup);
				}

				if (_type == SaveType.SAVE_IRONMAN_END)
				{
					Screen.UpdateScale(Options.geoscapeScale, ref Options.baseXGeoscape, ref Options.baseYGeoscape, true);
					_game.Screen.ResetDisplay(false);

					_game.SetState(new MainMenuState());
					_game.SavedGame = null;
				}

				// Clear the SDL event queue (i.e. ignore input from impatient users)
				_game.ClearEvents();
			}
			catch (Exception e) when (ListGamesState.IsSaveError(e))
			{
				Error(e.Message);
			}
		}
	}

	/// <summary>Pops up a window with an error message.</summary>
	/// <param name="msg">Error message.</param>
	public void Error(string msg)
	{
		Logger.Error(msg);
		string error = Tr("STR_SAVE_UNSUCCESSFUL") + Unicode.TOK_NL_SMALL + msg;
		var errorMessages = _game.Mod.GetInterface("errorMessages");
		if (_origin != OptionsOrigin.OPT_BATTLESCAPE)
			_game.PushState(new ErrorMessageState(error, _palette, unchecked((byte)errorMessages.GetElement("geoscapeColor").color), "BACK01.SCR", errorMessages.GetElement("geoscapePalette").color));
		else
			_game.PushState(new ErrorMessageState(error, _palette, unchecked((byte)errorMessages.GetElement("battlescapeColor").color), "TAC00.SCR", errorMessages.GetElement("battlescapePalette").color));
	}
}
