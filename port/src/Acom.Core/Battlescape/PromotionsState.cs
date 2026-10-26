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
// Ported from: src/Battlescape/PromotionsState.h, src/Battlescape/PromotionsState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Promotions screen that displays new soldier ranks.
/// </summary>
public class PromotionsState : State
{
	private readonly TextButton _btnOk;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtName, _txtRank, _txtBase;
	private readonly TextList _lstSoldiers;

	/// <summary>Initializes all the elements in the Promotions screen.</summary>
	public PromotionsState()
	{
		// Create object
		_window = new Window(this, 320, 200, 0, 0);
		_btnOk = new TextButton(288, 16, 16, 176);
		_txtTitle = new Text(300, 17, 10, 8);
		_txtName = new Text(114, 9, 16, 32);
		_txtRank = new Text(90, 9, 130, 32);
		_txtBase = new Text(80, 9, 220, 32);
		_lstSoldiers = new TextList(288, 128, 8, 40);

		// Set palette
		SetInterface("promotions");

		Add(_window, "window", "promotions");
		Add(_btnOk, "button", "promotions");
		Add(_txtTitle, "heading", "promotions");
		Add(_txtName, "text", "promotions");
		Add(_txtRank, "text", "promotions");
		Add(_txtBase, "text", "promotions");
		Add(_lstSoldiers, "list", "promotions");

		CenterAllSurfaces();

		// Set up object
		SetWindowBackground(_window, "promotions");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtTitle.SetText(Tr("STR_PROMOTIONS"));
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetBig();

		_txtName.SetText(Tr("STR_NAME"));

		_txtRank.SetText(Tr("STR_NEW_RANK"));

		_txtBase.SetText(Tr("STR_BASE"));

		_lstSoldiers.SetColumns(3, 114, 90, 84);
		_lstSoldiers.SetSelectable(true);
		_lstSoldiers.SetBackground(_window);
		_lstSoldiers.Margin = 8;

		foreach (Base xbase in _game.SavedGame.Bases)
		{
			foreach (Soldier soldier in xbase.Soldiers)
			{
				if (soldier.IsPromoted)
				{
					_lstSoldiers.AddRow(3, soldier.GetName(), Tr(soldier.RankString), xbase.GetName(_game.Language));
				}
			}
			foreach (Transfer transfer in xbase.Transfers)
			{
				if (transfer.Type == TransferType.TRANSFER_SOLDIER)
				{
					if (transfer.Soldier.IsPromoted)
					{
						_lstSoldiers.AddRow(3, transfer.Soldier.GetName(), Tr(transfer.Soldier.RankString), xbase.GetName(_game.Language));
					}
				}
			}
		}
	}

	/// <summary>Returns to the previous screen.</summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}
}
