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
// Ported from: src/Menu/ListSaveState.h, src/Menu/ListSaveState.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Save Game screen for listing info on available
/// saved games and saving them.
/// </summary>
public class ListSaveState : ListGamesState
{
	private TextEdit _edtSave;
	private TextButton _btnSaveGame;
	private string _selected = "";
	private int _previousSelectedRow, _selectedRow;

	/// <summary>Initializes all the elements in the Save Game screen.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	public ListSaveState(OptionsOrigin origin) : base(origin, 1, false)
	{
		_previousSelectedRow = -1;
		_selectedRow = -1;

		// Create objects
		_edtSave = new TextEdit(this, 168, 9, 0, 0);
		_btnSaveGame = new TextButton(_game.SavedGame.IsIronman ? 200 : 80, 16, 60, 172);

		Add(_edtSave);
		Add(_btnSaveGame, "button", "saveMenus");

		// Set up objects
		_txtTitle.SetText(Tr("STR_SELECT_SAVE_POSITION"));

		if (_game.SavedGame.IsIronman)
		{
			_btnCancel.Visible = false;
		}
		else
		{
			_btnCancel.X = 180;
		}

		_btnSaveGame.Text = Tr("STR_SAVE_GAME");
		_btnSaveGame.OnMouseClick(BtnSaveGameClick);

		_edtSave.Color = _lstSaves.SecondaryColor;
		_edtSave.Visible = false;
		_edtSave.OnKeyboardPress(EdtSaveKeyPress);

		CenterAllSurfaces();
	}

	/// <summary>Updates the save game list with the current list of available savegames.</summary>
	public override void UpdateList()
	{
		_lstSaves.AddRow(1, Tr("STR_NEW_SAVED_GAME_SLOT"));
		if (_origin != OptionsOrigin.OPT_BATTLESCAPE)
			_lstSaves.SetRowColor(0, _lstSaves.SecondaryColor);
		base.UpdateList();
	}

	/// <summary>Names the selected save.</summary>
	public override void LstSavesPress(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if ((action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT || _btnDelete.Pressed) && _edtSave.IsFocused)
		{
			_edtSave.Text = "";
			_edtSave.Visible = false;
			_edtSave.SetFocus(false, false);
			_lstSaves.SetScrolling(true);
		}
		base.LstSavesPress(action);
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT && !_btnDelete.Pressed)
		{
			_previousSelectedRow = _selectedRow;
			_selectedRow = unchecked((int)_lstSaves.SelectedRow);

			switch (_previousSelectedRow)
			{
				case -1: // first click on the savegame list
					break;
				case 0:
					_lstSaves.SetCellText(_previousSelectedRow, 0, Tr("STR_NEW_SAVED_GAME_SLOT"));
					break;
				default:
					_lstSaves.SetCellText(_previousSelectedRow, 0, _selected);
					break;
			}

			_selected = _lstSaves.GetCellText((int)_lstSaves.SelectedRow, 0);
			_lstSaves.SetCellText((int)_lstSaves.SelectedRow, 0, "");
			if (_lstSaves.SelectedRow == 0)
			{
				_edtSave.Text = "";
				_selected = "";
			}
			else
			{
				_edtSave.Text = _selected;
			}
			_edtSave.X = _lstSaves.GetColumnX(0);
			_edtSave.Y = _lstSaves.GetRowY(_selectedRow);
			_edtSave.Visible = true;
			_edtSave.SetFocus(true, false);
			_lstSaves.SetScrolling(false);
			DisableSort();
		}
	}

	/// <summary>Saves the selected save.</summary>
	public void EdtSaveKeyPress(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (action.Details.key.keysym.sym == SDLKey.SDLK_RETURN ||
			action.Details.key.keysym.sym == SDLKey.SDLK_KP_ENTER)
		{
			SaveGame();
		}
	}

	/// <summary>Saves the selected save.</summary>
	public void BtnSaveGameClick(Action action)
	{
		if (_selectedRow != -1)
		{
			SaveGame();
		}
	}

	/// <summary>Saves the selected save.</summary>
	public void SaveGame()
	{
		_game.SavedGame.Name = _edtSave.Text;
		string oldFilename, newFilename;
		newFilename = CrossPlatform.SanitizeFilename(_edtSave.Text);
		if (_selectedRow > 0)
		{
			oldFilename = _saves[_selectedRow - 1].fileName;
			if (oldFilename != newFilename + ".sav")
			{
				while (CrossPlatform.FileExists(Options.GetMasterUserFolder() + newFilename + ".sav"))
				{
					newFilename += "_";
				}
				string oldPath = Options.GetMasterUserFolder() + oldFilename;
				string newPath = Options.GetMasterUserFolder() + newFilename + ".sav";
				CrossPlatform.MoveFile(oldPath, newPath);
			}
		}
		else
		{
			while (CrossPlatform.FileExists(Options.GetMasterUserFolder() + newFilename + ".sav"))
			{
				newFilename += "_";
			}
		}
		newFilename += ".sav";
		_game.PushState(new SaveGameState(_origin, newFilename, _palette));
	}
}
