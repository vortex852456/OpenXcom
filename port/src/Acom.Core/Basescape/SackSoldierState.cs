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
// Ported from: src/Basescape/SackSoldierState.h, src/Basescape/SackSoldierState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)
#pragma warning disable IDE0060 // handler signatures keep the unused Action parameter like C++

/// <summary>
/// Window shown when the player tries to
/// sack a soldier.
/// </summary>
public class SackSoldierState : State
{
	private readonly Base _base;
	private readonly int _soldierId; // C++ size_t
	private readonly TextButton _btnOk, _btnCancel;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtSoldier;

	/// <summary>
	/// Initializes all the elements in a Sack Soldier window.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="soldierId">ID of the soldier to sack.</param>
	public SackSoldierState(Base @base, int soldierId)
	{
		_base = @base;
		_soldierId = soldierId;
		_screen = false;

		// Create objects
		_window = new Window(this, 152, 80, 84, 60);
		_btnOk = new TextButton(44, 16, 100, 115);
		_btnCancel = new TextButton(44, 16, 176, 115);
		_txtTitle = new Text(142, 9, 89, 75);
		_txtSoldier = new Text(142, 9, 89, 85);

		// Set palette
		SetInterface("sackSoldier");

		Add(_window, "window", "sackSoldier");
		Add(_btnOk, "button", "sackSoldier");
		Add(_btnCancel, "button", "sackSoldier");
		Add(_txtTitle, "text", "sackSoldier");
		Add(_txtSoldier, "text", "sackSoldier");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "sackSoldier");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_SACK"));

		_txtSoldier.Align = TextHAlign.ALIGN_CENTER;
		_txtSoldier.SetText(_base.Soldiers[_soldierId].GetName(true) + "?");
	}

	/// <summary>
	/// Sacks the soldier and returns
	/// to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		Soldier soldier = _base.Soldiers[_soldierId];
		if (soldier.Armor.StoreItem != null)
		{
			_base.StorageItems.AddItem(soldier.Armor.StoreItem);
		}
		_base.Soldiers.RemoveAt(_soldierId);
		_game.PopState();
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}
}
