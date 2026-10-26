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
// Ported from: src/Basescape/ItemLocationsState.h, src/Basescape/ItemLocationsState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window which displays item locations (i.e. which base has how much).
/// </summary>
public class ItemLocationsState : State
{
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly Text _txtBase, _txtQuantity;
	private readonly TextList _lstLocations;
	private readonly TextButton _btnOk;

	/// <summary>
	/// Initializes all the elements on the UI.
	/// </summary>
	public ItemLocationsState(RuleItem selectedItem)
	{
		_screen = false;

		_window = new Window(this, 222, 144, 49, 32);
		_txtTitle = new Text(200, 9, 59, 42);
		_txtBase = new Text(98, 9, 59, 56);
		_txtQuantity = new Text(98, 9, 159, 56);
		_lstLocations = new TextList(186, 64, 57, 70);
		_btnOk = new TextButton(100, 16, 110, 150);

		// Set palette
		SetInterface("itemLocations");

		Add(_window, "window", "itemLocations");
		Add(_txtTitle, "text", "itemLocations");
		Add(_txtBase, "text", "itemLocations");
		Add(_txtQuantity, "text", "itemLocations");
		Add(_btnOk, "button", "itemLocations");
		Add(_lstLocations, "list", "itemLocations");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "itemLocations");

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_TOPIC").Arg(Tr(selectedItem.Type)));

		_txtBase.SetText(Tr("STR_BASE"));
		_txtQuantity.SetText(Tr("STR_QUANTITY_UC"));

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_lstLocations.SetColumns(3, 100, 40, 30);
		_lstLocations.SetBackground(_window);
		_lstLocations.Margin = 2;
		_lstLocations.SetSelectable(true);

		foreach (var xbase in _game.SavedGame.Bases)
		{
			string inBase = xbase.StorageItems.GetItem(selectedItem).ToString(CultureInfo.InvariantCulture);
			int total = 0;
			foreach (var xtransfer in xbase.Transfers)
			{
				if (xtransfer.Type == TransferType.TRANSFER_ITEM && xtransfer.Items == selectedItem)
				{
					total += xtransfer.Quantity;
				}
			}
			string inTransfer = "+" + total.ToString(CultureInfo.InvariantCulture);
			_lstLocations.AddRow(3, xbase.GetName(null), inBase, inTransfer);
		}
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}
}
