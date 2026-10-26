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
// Ported from: src/Basescape/MonthlyCostsState.h, src/Basescape/MonthlyCostsState.cpp
// Ported from: src/Basescape/CraftEquipmentSaveState.h, src/Basescape/CraftEquipmentSaveState.cpp
#nullable disable

using static Acom.SDLKey;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Save Craft Loadout window that lets the player
/// store the current craft equipment as a global template.
/// </summary>
public class CraftEquipmentSaveState : State
{
	private readonly CraftEquipmentState _parent;
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly TextList _lstLoadout;
	private readonly TextButton _btnCancel, _btnSave;
	private readonly TextEdit _edtSave;
	private string _selected = "";
	private int _previousSelectedRow, _selectedRow;

	/// <summary>
	/// Initializes all the elements in the Save Craft Loadout window.
	/// </summary>
	public CraftEquipmentSaveState(CraftEquipmentState parent)
	{
		_parent = parent;
		_previousSelectedRow = -1;
		_selectedRow = -1;
		_screen = false;

		// Create objects
		_window = new Window(this, 240, 136, 40, 36 + 1, WindowPopup.POPUP_BOTH);
		_txtTitle = new Text(230, 16, 45, 44 + 3);
		_lstLoadout = new TextList(208, 80, 48, 60);
		_btnCancel = new TextButton(80, 16, 165, 148);
		_btnSave = new TextButton(80, 16, 75, 148);
		_edtSave = new TextEdit(this, 188, 9, 0, 0);

		// Set palette
		SetInterface("craftEquipmentSave");

		Add(_window, "window", "craftEquipmentSave");
		Add(_txtTitle, "text", "craftEquipmentSave");
		Add(_lstLoadout, "list", "craftEquipmentSave");
		Add(_btnCancel, "button", "craftEquipmentSave");
		Add(_btnSave, "button", "craftEquipmentSave");
		Add(_edtSave);

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "craftEquipmentSave");

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_SAVE_CRAFT_LOADOUT_TEMPLATE"));

		_lstLoadout.SetColumns(1, 192);
		_lstLoadout.SetSelectable(true);
		_lstLoadout.SetBackground(_window);
		_lstLoadout.Margin = 8;
		_lstLoadout.OnMousePress(LstLoadoutPress);

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_btnSave.Text = Tr("STR_SAVE_UC");
		_btnSave.OnMouseClick(BtnSaveClick);

		_edtSave.SetColor(_lstLoadout.SecondaryColor);
		_edtSave.Visible = false;
		_edtSave.OnKeyboardPress(EdtSaveKeyPress);

		for (int i = 0; i < SavedGame.MAX_CRAFT_LOADOUT_TEMPLATES; ++i)
		{
			ItemContainer item = _game.SavedGame.GetGlobalCraftLoadout(i);
			if (item.Empty())
			{
				_lstLoadout.AddRow(1, Tr("STR_EMPTY_SLOT_N").Arg(i + 1));
			}
			else
			{
				string itemName = _game.SavedGame.GetGlobalCraftLoadoutName(i);
				if (string.IsNullOrEmpty(itemName))
				{
					_lstLoadout.AddRow(1, Tr("STR_UNNAMED_SLOT_N").Arg(i + 1));
				}
				else
				{
					_lstLoadout.AddRow(1, itemName);
				}
			}
		}
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Saves the selected template.
	/// </summary>
	public void BtnSaveClick(Action action)
	{
		if (_selectedRow != -1)
		{
			SaveTemplate();
		}
	}

	/// <summary>
	/// Names the selected template.
	/// </summary>
	public void LstLoadoutPress(Action action)
	{
		_previousSelectedRow = _selectedRow;
		_selectedRow = (int)_lstLoadout.SelectedRow;
		if (_previousSelectedRow > -1)
		{
			_lstLoadout.SetCellText(_previousSelectedRow, 0, _selected);
		}
		_selected = _lstLoadout.GetCellText(_selectedRow, 0);

		if (action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT && _edtSave.IsFocused)
		{
			_previousSelectedRow = -1;
			_selectedRow = -1;

			_edtSave.Text = "";
			_edtSave.Visible = false;
			_edtSave.SetFocus(false, false);
			_lstLoadout.SetScrolling(true);
		}
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
		{
			_lstLoadout.SetCellText(_selectedRow, 0, "");

			_edtSave.Text = _selected;
			_edtSave.X = _lstLoadout.GetColumnX(0);
			_edtSave.Y = _lstLoadout.GetRowY(_selectedRow);
			_edtSave.Visible = true;
			_edtSave.SetFocus(true, false);
			_lstLoadout.SetScrolling(false);
		}
	}

	/// <summary>
	/// Saves the selected template.
	/// </summary>
	public void EdtSaveKeyPress(Action action)
	{
		if (action.Details.key.keysym.sym == SDLK_RETURN ||
			action.Details.key.keysym.sym == SDLK_KP_ENTER)
		{
			SaveTemplate();
		}
	}

	/// <summary>
	/// Saves the selected template.
	/// </summary>
	public void SaveTemplate()
	{
		if (_selectedRow >= 0 && _selectedRow < SavedGame.MAX_CRAFT_LOADOUT_TEMPLATES)
		{
			_game.SavedGame.SetGlobalCraftLoadoutName(_selectedRow, _edtSave.Text);
			_parent.SaveGlobalLoadout(_selectedRow);

			_game.PopState();
		}
	}
}
