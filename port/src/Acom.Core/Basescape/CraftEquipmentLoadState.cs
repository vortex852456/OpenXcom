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
// Ported from: src/Basescape/CraftEquipmentLoadState.h, src/Basescape/CraftEquipmentLoadState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Load Craft Loadout window that lets the player
/// apply a global equipment template to a craft.
/// </summary>
public class CraftEquipmentLoadState : State
{
	private readonly CraftEquipmentState _parent;
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly TextList _lstLoadout;
	private readonly ToggleTextButton _btnOnlyAdd;
	private readonly TextButton _btnCancel;

	/// <summary>
	/// Initializes all the elements in the Load Craft Loadout window.
	/// </summary>
	public CraftEquipmentLoadState(CraftEquipmentState parent)
	{
		_parent = parent;
		_screen = false;

		// Create objects
		_window = new Window(this, 240, 136, 40, 36 + 1, WindowPopup.POPUP_BOTH);
		_txtTitle = new Text(230, 16, 45, 44 + 3);
		_lstLoadout = new TextList(208, 80, 48, 60);
		_btnOnlyAdd = new ToggleTextButton(100, 16, 56, 148);
		_btnCancel = new TextButton(100, 16, 164, 148);

		// Set palette
		SetInterface("craftEquipmentLoad");

		Add(_window, "window", "craftEquipmentLoad");
		Add(_txtTitle, "text", "craftEquipmentLoad");
		Add(_lstLoadout, "list", "craftEquipmentLoad");
		Add(_btnOnlyAdd, "button", "craftEquipmentLoad");
		Add(_btnCancel, "button", "craftEquipmentLoad");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "craftEquipmentLoad");

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_LOAD_CRAFT_LOADOUT_TEMPLATE"));

		_lstLoadout.SetColumns(1, 192);
		_lstLoadout.SetSelectable(true);
		_lstLoadout.SetBackground(_window);
		_lstLoadout.Margin = 8;
		_lstLoadout.OnMouseClick(LstLoadoutClick);

		_btnOnlyAdd.Text = Tr("STR_ADD_ON_TOP");

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		for (int i = 0; i < SavedGame.MAX_CRAFT_LOADOUT_TEMPLATES; ++i)
		{
			ItemContainer item = _game.SavedGame.GetGlobalCraftLoadout(i);
			if (item.Contents.Count == 0)
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
	/// Loads the template and returns to the previous screen.
	/// </summary>
	public void LstLoadoutClick(Action action)
	{
		_game.PopState();
		bool addOnTop = _btnOnlyAdd.Pressed || _game.IsCtrlPressed();
		_parent.LoadGlobalLoadout((int)_lstLoadout.SelectedRow, addOnTop);
	}
}
