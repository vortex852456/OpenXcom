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
// Ported from: src/Battlescape/InventorySaveState.h, src/Battlescape/InventorySaveState.cpp
#nullable disable

using System.Text;

namespace Acom;

/// <summary>
/// Save Inventory window.
/// </summary>
public class InventorySaveState : State
{
	private readonly InventoryState _parent;
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly TextList _lstLayout;
	private readonly TextButton _btnCancel, _btnSave, _btnSaveWithArmor;
	private readonly TextEdit _edtSave;
	private string _selected = "";
	private int _previousSelectedRow, _selectedRow;

	/// <summary>Initializes all the elements in the Save Inventory window.</summary>
	public InventorySaveState(InventoryState parent)
	{
		_parent = parent;
		_previousSelectedRow = -1;
		_selectedRow = -1;
		_screen = false;

		// Create objects
		_window = new Window(this, 240, 136, 40, 36 + 1, WindowPopup.POPUP_BOTH);
		_txtTitle = new Text(230, 16, 45, 44 + 3);
		_lstLayout = new TextList(208, 80, 48, 60);
		_btnCancel = new TextButton(70, 16, 201, 148);
		_btnSave = new TextButton(70, 16, 50, 148);
		_btnSaveWithArmor = new TextButton(70, 16, 126, 148);
		_edtSave = new TextEdit(this, 188, 9, 0, 0);

		SetStandardPalette("PAL_BATTLESCAPE");

		Add(_window, "messageWindowBorder", "battlescape");
		Add(_txtTitle, "messageWindows", "battlescape");
		Add(_lstLayout, "optionLists", "battlescape");
		Add(_btnCancel, "messageWindowButtons", "battlescape");
		Add(_btnSave, "messageWindowButtons", "battlescape");
		Add(_btnSaveWithArmor, "messageWindowButtons", "battlescape");
		Add(_edtSave);

		CenterAllSurfaces();

		// Set up objects
		_window.SetHighContrast(true);
		_window.SetBackground(_game.Mod.GetSurface("TAC00.SCR"));

		_txtTitle.SetHighContrast(true);
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_SAVE_EQUIPMENT_TEMPLATE"));

		_lstLayout.SetHighContrast(true);
		_lstLayout.SetColumns(1, 192);
		_lstLayout.SetSelectable(true);
		_lstLayout.SetBackground(_window);
		_lstLayout.Margin = 8;
		_lstLayout.OnMousePress(LstLayoutPress);

		_btnCancel.SetHighContrast(true);
		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_btnSave.SetHighContrast(true);
		_btnSave.Text = Tr("STR_SAVE_UC");
		_btnSave.OnMouseClick(BtnSaveClick);

		_btnSaveWithArmor.SetHighContrast(true);
		string savePlus = Tr("STR_SAVE_UC");
		savePlus += "+";
		_btnSaveWithArmor.Text = savePlus;
		_btnSaveWithArmor.OnMouseClick(BtnSaveWithArmorClick);

		_edtSave.SetHighContrast(true);
		_edtSave.SetColor(_lstLayout.SecondaryColor);
		_edtSave.Visible = false;
		_edtSave.OnKeyboardPress(EdtSaveKeyPress);

		for (int i = 0; i < Options.oxceMaxEquipmentLayoutTemplates; ++i)
		{
			_lstLayout.AddRow(1, FormatLayoutRow(this, i));
		}
	}

	/// <summary>Builds the list row of a global equipment layout (shared with InventoryLoadState).</summary>
	internal static string FormatLayoutRow(State state, int i)
	{
		SavedGame save = _game.SavedGame;
		List<EquipmentLayoutItem> item = save.GetGlobalEquipmentLayout(i);
		var ss = new StringBuilder();
		string armorName = save.GetGlobalEquipmentLayoutArmor(i);
		if (!string.IsNullOrEmpty(armorName))
		{
			ss.Append('[').Append(state.Tr(armorName).ToString()).Append("] ");
		}
		if (item.Count == 0)
		{
			ss.Append(state.Tr("STR_EMPTY_SLOT_N").Arg(i + 1).ToString());
		}
		else
		{
			string itemName = save.GetGlobalEquipmentLayoutName(i);
			if (string.IsNullOrEmpty(itemName))
				ss.Append(state.Tr("STR_UNNAMED_SLOT_N").Arg(i + 1).ToString());
			else
				ss.Append(itemName);
		}
		return ss.ToString();
	}

#pragma warning disable CA1822 // instance handler in C++ (State._game is static here)
	/// <summary>Returns to the previous screen.</summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}
#pragma warning restore CA1822

	/// <summary>Saves the selected template.</summary>
	public void BtnSaveClick(Action action)
	{
		if (_selectedRow != -1)
		{
			SaveTemplate(false);
		}
	}

	/// <summary>Saves the selected template (including the armor).</summary>
	public void BtnSaveWithArmorClick(Action action)
	{
		if (_selectedRow != -1)
		{
			SaveTemplate(true);
		}
	}

	/// <summary>Names the selected template.</summary>
	public void LstLayoutPress(Action action)
	{
		_previousSelectedRow = _selectedRow;
		_selectedRow = (int)_lstLayout.SelectedRow;
		if (_previousSelectedRow > -1)
		{
			_lstLayout.SetCellText(_previousSelectedRow, 0, _selected);
		}
		_selected = _lstLayout.GetCellText(_selectedRow, 0);

		if (_game.IsRightClick(action) && _edtSave.IsFocused)
		{
			_previousSelectedRow = -1;
			_selectedRow = -1;

			_edtSave.Text = "";
			_edtSave.Visible = false;
			_edtSave.SetFocus(false, false);
			_lstLayout.SetScrolling(true);
		}
		if (_game.IsLeftClick(action))
		{
			_lstLayout.SetCellText(_selectedRow, 0, "");

			int pos = _selected.IndexOf("] ", StringComparison.Ordinal);
			if (pos != -1)
			{
				// cut off the armor info
				_edtSave.Text = _selected.Substring(pos + 2);
			}
			else
			{
				_edtSave.Text = _selected;
			}
			_edtSave.X = _lstLayout.GetColumnX(0);
			_edtSave.Y = _lstLayout.GetRowY(_selectedRow);
			_edtSave.Visible = true;
			_edtSave.SetFocus(true, false);
			_lstLayout.SetScrolling(false);
		}
	}

	/// <summary>Saves the selected template.</summary>
	public void EdtSaveKeyPress(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (action.Details.key.keysym.sym == SDLKey.SDLK_RETURN ||
			action.Details.key.keysym.sym == SDLKey.SDLK_KP_ENTER)
		{
			SaveTemplate(false);
		}
	}

	/// <summary>Saves the selected template.</summary>
	public void SaveTemplate(bool includingArmor)
	{
		if (_selectedRow >= 0 && _selectedRow < Options.oxceMaxEquipmentLayoutTemplates)
		{
			_game.SavedGame.SetGlobalEquipmentLayoutName(_selectedRow, _edtSave.Text);
			_parent.SaveGlobalLayout(_selectedRow, includingArmor);

			_game.PopState();
		}
	}
}
