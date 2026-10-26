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
// Ported from: src/Geoscape/ItemsArrivingState.h, src/Geoscape/ItemsArrivingState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Items Arriving window that displays all
/// the items that have arrived at bases.
/// </summary>
public class ItemsArrivingState : State
{
	private readonly GeoscapeState _state;
	private readonly Base _base;
	private readonly TextButton _btnOk, _btnGotoBase;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtItem, _txtQuantity, _txtDestination;
	private readonly TextList _lstTransfers;

	/// <summary>
	/// Initializes all the elements in the Items Arriving window.
	/// </summary>
	/// <param name="state">Pointer to the Geoscape state.</param>
	public ItemsArrivingState(GeoscapeState state)
	{
		_state = state;
		_base = null;
		_screen = false;

		// Create objects
		_window = new Window(this, 320, 184, 0, 8, WindowPopup.POPUP_BOTH);
		_btnOk = new TextButton(142, 16, 16, 166);
		_btnGotoBase = new TextButton(142, 16, 162, 166);
		_txtTitle = new Text(310, 17, 5, 18);
		_txtItem = new Text(114, 9, 16, 34);
		_txtQuantity = new Text(54, 9, 152, 34);
		_txtDestination = new Text(112, 9, 212, 34);
		_lstTransfers = new TextList(271, 112, 14, 50);

		// Set palette
		SetInterface("itemsArriving");

		Add(_window, "window", "itemsArriving");
		Add(_btnOk, "button", "itemsArriving");
		Add(_btnGotoBase, "button", "itemsArriving");
		Add(_txtTitle, "text1", "itemsArriving");
		Add(_txtItem, "text1", "itemsArriving");
		Add(_txtQuantity, "text1", "itemsArriving");
		Add(_txtDestination, "text1", "itemsArriving");
		Add(_lstTransfers, "text2", "itemsArriving");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "itemsArriving");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_btnGotoBase.Text = Tr("STR_GO_TO_BASE");
		_btnGotoBase.OnMouseClick(BtnGotoBaseClick);
		_btnGotoBase.OnKeyboardPress(BtnGotoBaseClick, Options.keyOk);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_ITEMS_ARRIVING"));

		_txtItem.SetText(Tr("STR_ITEM"));

		_txtQuantity.SetText(Tr("STR_QUANTITY_UC"));

		_txtDestination.SetText(Tr("STR_DESTINATION_UC"));

		_lstTransfers.SetColumns(3, 155, 41, 98);
		_lstTransfers.SetSelectable(true);
		_lstTransfers.SetBackground(_window);
		_lstTransfers.Margin = 2;

		foreach (var xbase in _game.SavedGame.Bases)
		{
			var transfers = xbase.Transfers;
			for (int transferIt = 0; transferIt < transfers.Count;)
			{
				Transfer transfer = transfers[transferIt];
				if (transfer.Hours == 0)
				{
					_base = xbase;

					// Check if we have an automated use for an item
					if (transfer.Type == TransferType.TRANSFER_ITEM)
					{
						var item = transfer.Items;
						if (item.BattleType == BattleType.BT_NONE)
						{
							foreach (var xcraft in xbase.Crafts)
							{
								xcraft.ReuseItem(item);
							}
						}
					}

					// Remove transfer
					_lstTransfers.AddRow(3, transfer.GetName(_game.Language), transfer.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture), xbase.GetName(null));
					transfers.RemoveAt(transferIt);
				}
				else
				{
					++transferIt;
				}
			}
		}
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Goes to the base for the respective transfer.
	/// </summary>
	public void BtnGotoBaseClick(Action action)
	{
		_state.TimerReset();
		_game.PopState();
		_game.PushState(new BasescapeState(_base, _state.Globe));
	}
}
