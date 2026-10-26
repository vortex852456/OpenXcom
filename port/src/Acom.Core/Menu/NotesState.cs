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
// Ported from: src/Menu/NotesState.h, src/Menu/NotesState.cpp
#nullable disable
#pragma warning disable CA1822 // UI handlers stay instance members like in C++ (State._game is static here)

namespace Acom;

/// <summary>Allows the player to take notes.</summary>
/// <remarks>The C++ __MOBILE__ variant (delete toggle button) is not ported; the desktop layout is used.</remarks>
public class NotesState : State
{
	protected Window _window;
	protected Text _txtTitle;
	protected Text _txtDelete;
	protected TextList _lstNotes;
	protected TextEdit _edtNote;
	protected TextButton _btnSave;
	protected TextButton _btnCancel;
	protected ToggleTextButton _btnDelete;
	protected OptionsOrigin _origin;
	protected string _selectedNote = "";
	protected int _previousSelectedRow;
	protected int _selectedRow;

	/// <summary>Initializes all the elements in the Notes screen.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	public NotesState(OptionsOrigin origin)
	{
		_origin = origin;
		_previousSelectedRow = -1;
		_selectedRow = -1;
		_screen = false;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0, WindowPopup.POPUP_NONE);
		_txtTitle = new Text(310, 17, 5, 7);
		_txtDelete = new Text(310, 9, 5, 23);
		_lstNotes = new TextList(288, 120, 8, 42);
		_edtNote = new TextEdit(this, 268, 9, 0, 0);
		_btnSave = new TextButton(80, 16, 60, 172);
		_btnCancel = new TextButton(80, 16, 180, 172);
		_btnDelete = new ToggleTextButton(288, 16, 16, 23);

		// Set palette
		SetInterface("geoscape", true, _game.SavedGame?.SavedBattle);

		Add(_window, "window", "noteMenu");
		Add(_txtTitle, "text", "noteMenu");
		Add(_txtDelete, "text", "noteMenu");
		Add(_lstNotes, "list", "noteMenu");
		Add(_edtNote);
		Add(_btnSave, "button", "noteMenu");
		Add(_btnCancel, "button", "noteMenu");
		Add(_btnDelete, "button", "noteMenu");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "noteMenu");

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_NOTES"));

		_btnDelete.Visible = false;
		_txtDelete.Align = TextHAlign.ALIGN_CENTER;
		_txtDelete.SetText(Tr("STR_RIGHT_CLICK_TO_DELETE"));

		_lstNotes.SetColumns(1, 288);
		_lstNotes.SetSelectable(true);
		_lstNotes.SetBackground(_window);
		_lstNotes.Margin = 8;
		_lstNotes.SetWordWrap(true); // just in case someone modifies the save manually
		_lstNotes.OnMousePress(LstNotesPress);

		_edtNote.Color = _lstNotes.SecondaryColor;
		_edtNote.Visible = false;
		_edtNote.OnKeyboardPress(EdtNoteKeyPress);

		_btnSave.Text = Tr("STR_SAVE_UC");
		_btnSave.OnMouseClick(BtnSaveClick);

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);
	}

	/// <summary>Refreshes the Notes state.</summary>
	public override void Init()
	{
		base.Init();

		if (_origin == OptionsOrigin.OPT_BATTLESCAPE)
		{
			ApplyBattlescapeTheme("noteMenu");
		}

		UpdateList();
	}

	/// <summary>Updates the Notes list.</summary>
	protected void UpdateList()
	{
		_lstNotes.ClearList();

		byte color = _lstNotes.SecondaryColor;

		foreach (var note in _game.SavedGame.UserNotes)
		{
			_lstNotes.AddRow(1, note);
		}

		_lstNotes.AddRow(1, Tr("STR_NEW_NOTE"));
		if (_origin != OptionsOrigin.OPT_BATTLESCAPE)
		{
			_lstNotes.SetRowColor(_lstNotes.LastRowIndex, color);
		}
		_lstNotes.ScrollDown(true);
	}

	/// <summary>Returns to the previous screen without saving anything.</summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>Allows to enter, edit and delete notes.</summary>
	public void LstNotesPress(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		// ignore scrolling, process only LMB and RMB
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT || action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT)
		{
			_previousSelectedRow = _selectedRow;
			_selectedRow = unchecked((int)_lstNotes.SelectedRow);

			// restore previous
			if (_previousSelectedRow > -1)
			{
				_lstNotes.SetCellText(_previousSelectedRow, 0, _selectedNote);
			}

			// back up current
			_selectedNote = _lstNotes.GetCellText(_selectedRow, 0);
		}

		if (action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT || _btnDelete.Pressed)
		{
			if (_edtNote.IsFocused)
			{
				// cancel editing the current note
				_edtNote.Text = "";
				_edtNote.Visible = false;
				_edtNote.SetFocus(false, false);
				_lstNotes.SetScrolling(true);
			}
			else
			{
				// any row except for the last
				if (_selectedRow >= 0 && _selectedRow < _lstNotes.LastRowIndex)
				{
					// delete the selected note
					_selectedNote = "";
					_lstNotes.SetCellText(_selectedRow, 0, _selectedNote);
				}
			}
		}
		else if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
		{
			// temporarily set to empty during editing
			_lstNotes.SetCellText(_selectedRow, 0, "");

			// set the initial text for editing
			if (_selectedRow == _lstNotes.LastRowIndex)
			{
				_edtNote.Text = "";
			}
			else
			{
				_edtNote.Text = _selectedNote;
			}
			_edtNote.X = _lstNotes.GetColumnX(0);
			_edtNote.Y = _lstNotes.GetRowY(_selectedRow);
			_edtNote.Visible = true;
			_edtNote.SetFocus(true, false);
			_lstNotes.SetScrolling(false);
		}
	}

	/// <summary>Updates the currently edited note.</summary>
	public void EdtNoteKeyPress(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (action.Details.key.keysym.sym == SDLKey.SDLK_RETURN ||
			action.Details.key.keysym.sym == SDLKey.SDLK_KP_ENTER)
		{
			// update the selected note
			_selectedNote = _edtNote.Text;
			_lstNotes.SetCellText(_selectedRow, 0, _selectedNote);

			// clean up
			_edtNote.Text = "";
			_edtNote.Visible = false;
			_edtNote.SetFocus(false, false);
			_lstNotes.SetScrolling(true);

			// if we're adding a new note...
			if (_selectedRow == _lstNotes.LastRowIndex)
			{
				// change color to normal
				_lstNotes.SetRowColor(_lstNotes.LastRowIndex, _lstNotes.Color);

				// add a new empty note
				_lstNotes.AddRow(1, Tr("STR_NEW_NOTE"));
				if (_origin != OptionsOrigin.OPT_BATTLESCAPE)
				{
					_lstNotes.SetRowColor(_lstNotes.LastRowIndex, _lstNotes.SecondaryColor);
				}
				_lstNotes.ScrollDown(true);
			}
		}
	}

	/// <summary>Saves all changes and returns to the previous screen.</summary>
	public void BtnSaveClick(Action action)
	{
		if (_edtNote.IsFocused)
		{
			// edit still in progress
			return;
		}

		// overwrite everything, no way back :)
		var notes = _game.SavedGame.UserNotes;
		notes.Clear();
		if (_lstNotes.Texts > 1)
		{
			// ignore last row
			for (int i = 0; i < _lstNotes.LastRowIndex; ++i)
			{
				string note = _lstNotes.GetCellText(i, 0);
				if (!string.IsNullOrEmpty(note))
				{
					notes.Add(note);
				}
			}
		}

		_game.PopState();
	}
}
