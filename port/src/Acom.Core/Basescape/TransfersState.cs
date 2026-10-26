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
// Ported from: src/Basescape/TransfersState.h, src/Basescape/TransfersState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Transfers window that displays all
/// the items currently in-transit to a base.
/// </summary>
public class TransfersState : State
{
	private readonly Base _base;

	private readonly TextButton _btnOk;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtItem, _txtQuantity, _txtArrivalTime;
	private readonly TextList _lstTransfers;

	/// <summary>
	/// Initializes all the elements in the Transfers window.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	public TransfersState(Base @base)
	{
		_base = @base;
		_screen = false;

		// Create objects
		_window = new Window(this, 320, 184, 0, 8, WindowPopup.POPUP_BOTH);
		_btnOk = new TextButton(288, 16, 16, 166);
		_txtTitle = new Text(278, 17, 21, 18);
		_txtItem = new Text(114, 9, 16, 34);
		_txtQuantity = new Text(54, 9, 152, 34);
		_txtArrivalTime = new Text(112, 9, 212, 34);
		_lstTransfers = new TextList(273, 112, 14, 50);

		// Set palette
		SetInterface("transferInfo");

		Add(_window, "window", "transferInfo");
		Add(_btnOk, "button", "transferInfo");
		Add(_txtTitle, "text", "transferInfo");
		Add(_txtItem, "text", "transferInfo");
		Add(_txtQuantity, "text", "transferInfo");
		Add(_txtArrivalTime, "text", "transferInfo");
		Add(_lstTransfers, "list", "transferInfo");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "transferInfo");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_TRANSFERS"));

		_txtItem.SetText(Tr("STR_ITEM"));

		_txtQuantity.SetText(Tr("STR_QUANTITY_UC"));

		_txtArrivalTime.SetText(Tr("STR_ARRIVAL_TIME_HOURS"));

		_lstTransfers.SetColumns(3, 155, 75, 46);
		_lstTransfers.SetSelectable(true);
		_lstTransfers.SetBackground(_window);
		_lstTransfers.Margin = 2;

		foreach (var transfer in _base.Transfers)
		{
			_lstTransfers.AddRow(3, transfer.GetName(_game.Language), transfer.Quantity.ToString(CultureInfo.InvariantCulture), transfer.Hours.ToString(CultureInfo.InvariantCulture));
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
