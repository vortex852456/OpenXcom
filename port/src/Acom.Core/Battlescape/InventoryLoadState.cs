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
// Ported from: src/Battlescape/InventoryLoadState.h, src/Battlescape/InventoryLoadState.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Load Inventory window.
/// </summary>
public class InventoryLoadState : State
{
	private readonly InventoryState _parent;
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly TextList _lstLayout;
	private readonly TextButton _btnCancel;

	/// <summary>Initializes all the elements in the Load Inventory window.</summary>
	public InventoryLoadState(InventoryState parent)
	{
		_parent = parent;
		_screen = false;

		// Create objects
		_window = new Window(this, 240, 136, 40, 36 + 1, WindowPopup.POPUP_BOTH);
		_txtTitle = new Text(230, 16, 45, 44 + 3);
		_lstLayout = new TextList(208, 80, 48, 60);
		_btnCancel = new TextButton(120, 16, 90, 148);

		SetStandardPalette("PAL_BATTLESCAPE");

		Add(_window, "messageWindowBorder", "battlescape");
		Add(_txtTitle, "messageWindows", "battlescape");
		Add(_lstLayout, "optionLists", "battlescape");
		Add(_btnCancel, "messageWindowButtons", "battlescape");

		CenterAllSurfaces();

		// Set up objects
		_window.SetHighContrast(true);
		_window.SetBackground(_game.Mod.GetSurface("TAC00.SCR"));

		_txtTitle.SetHighContrast(true);
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_LOAD_EQUIPMENT_TEMPLATE"));

		_lstLayout.SetHighContrast(true);
		_lstLayout.SetColumns(1, 192);
		_lstLayout.SetSelectable(true);
		_lstLayout.SetBackground(_window);
		_lstLayout.Margin = 8;
		_lstLayout.OnMouseClick(LstLayoutClick);

		_btnCancel.SetHighContrast(true);
		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		for (int i = 0; i < Options.oxceMaxEquipmentLayoutTemplates; ++i)
		{
			_lstLayout.AddRow(1, InventorySaveState.FormatLayoutRow(this, i));
		}
	}

#pragma warning disable CA1822 // instance handler in C++ (State._game is static here)
	/// <summary>Returns to the previous screen.</summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}
#pragma warning restore CA1822

	/// <summary>Loads the template and returns to the previous screen.</summary>
	public void LstLayoutClick(Action action)
	{
		int index = (int)_lstLayout.SelectedRow;
		bool armorChanged = _parent.LoadGlobalLayoutArmor(index);
		_parent.SetGlobalLayoutIndex(index, armorChanged);
		_game.PopState();
	}
}
