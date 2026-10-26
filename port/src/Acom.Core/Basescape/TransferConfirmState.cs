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
// Ported from: src/Basescape/TransferConfirmState.h, src/Basescape/TransferConfirmState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window to confirm a transfer between bases.
/// </summary>
public class TransferConfirmState : State
{
	private readonly TextButton _btnCancel, _btnOk;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtCost, _txtTotal;
	private readonly Base _base;
	private readonly TransferItemsState _state;

	/// <summary>
	/// Initializes all the elements in the Confirm Transfer window.
	/// </summary>
	/// <param name="base">Pointer to the destination base.</param>
	/// <param name="state">Pointer to the Transfer state.</param>
	public TransferConfirmState(Base @base, TransferItemsState state)
	{
		_base = @base;
		_state = state;
		_screen = false;

		// Create objects
		_window = new Window(this, 320, 80, 0, 60);
		_btnCancel = new TextButton(128, 16, 176, 115);
		_btnOk = new TextButton(128, 16, 16, 115);
		_txtTitle = new Text(310, 17, 5, 75);
		_txtCost = new Text(60, 17, 110, 95);
		_txtTotal = new Text(100, 17, 170, 95);

		// Set palette
		SetInterface("transferConfirm");

		Add(_window, "window", "transferConfirm");
		Add(_btnCancel, "button", "transferConfirm");
		Add(_btnOk, "button", "transferConfirm");
		Add(_txtTitle, "text", "transferConfirm");
		Add(_txtCost, "text", "transferConfirm");
		Add(_txtTotal, "text", "transferConfirm");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "transferConfirm");

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_TRANSFER_ITEMS_TO").Arg(_base.GetName(null)));

		_txtCost.SetBig();
		_txtCost.SetText(Tr("STR_COST"));

		_txtTotal.SetBig();
		_txtTotal.SetText(Unicode.TOK_COLOR_FLIP + Unicode.FormatFunding(_state.Total));
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Completes the transfer.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_state.CompleteTransfer();
		_game.PopState();
		_game.PopState();
		_game.PopState();
	}
}
