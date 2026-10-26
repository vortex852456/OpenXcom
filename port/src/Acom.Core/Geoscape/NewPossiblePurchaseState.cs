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
// Ported from: src/Geoscape/NewPossiblePurchaseState.h, src/Geoscape/NewPossiblePurchaseState.cpp
#nullable disable

using System.Text;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window which inform the player of new possible items to purchase.
/// </summary>
public class NewPossiblePurchaseState : State
{
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly TextList _lstPossibilities;
	private readonly Text _txtCaveat;
	private readonly TextButton _btnPurchase, _btnOk;
	private readonly Base _base;

	/// <summary>
	/// Initializes all the elements in the NewPossiblePurchase screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="possibilities">List of newly possible RuleItem.</param>
	public NewPossiblePurchaseState(Base @base, List<RuleItem> possibilities)
	{
		_base = @base;
		_screen = false;

		// Create objects
		_window = new Window(this, 288, 180, 16, 10);
		_btnOk = new TextButton(160, 14, 80, 149);
		_btnPurchase = new TextButton(160, 14, 80, 165);
		_txtTitle = new Text(288, 40, 16, 20);
		_lstPossibilities = new TextList(250, 80, 35, 50);
		_txtCaveat = new Text(250, 16, 35, 131);

		// Set palette
		SetInterface("geoNewItem");

		Add(_window, "window", "geoNewItem");
		Add(_btnOk, "button", "geoNewItem");
		Add(_btnPurchase, "button", "geoNewItem");
		Add(_txtTitle, "text1", "geoNewItem");
		Add(_lstPossibilities, "text2", "geoNewItem");
		Add(_txtCaveat, "text1", "geoNewItem");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "geoNewItem");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnPurchase.Text = Tr("STR_PURCHASE_HIRE_PERSONNEL");
		_btnPurchase.OnMouseClick(BtnPurchaseClick);
		_btnPurchase.OnKeyboardPress(BtnPurchaseClick, Options.keyOk);
		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_WE_CAN_NOW_PURCHASE"));

		// Caveat
		{
			RuleBaseFacilityFunctions requiredServices = default;
			foreach (var ruleItem in possibilities)
			{
				requiredServices |= ruleItem.RequiresBuyBaseFunc;
			}
			StringBuilder ss = new StringBuilder();
			int i = 0;
			foreach (var serviceName in _game.Mod.GetBaseFunctionNames(requiredServices))
			{
				if (i > 0)
					ss.Append(", ");
				ss.Append((string)Tr(serviceName));
				i++;
			}
			string argument = ss.ToString();

			_txtCaveat.Align = TextHAlign.ALIGN_CENTER;
			_txtCaveat.SetText(Tr("STR_REQUIRED_BASE_SERVICES").Arg(argument));
			_txtCaveat.Visible = requiredServices.Any();
		}

		_lstPossibilities.SetColumns(1, 250);
		_lstPossibilities.SetBig();
		_lstPossibilities.SetAlign(TextHAlign.ALIGN_CENTER);
		_lstPossibilities.SetScrolling(true, 0);
		foreach (var ruleItem in possibilities)
		{
			_lstPossibilities.AddRow(1, Tr(ruleItem.Type));
		}
	}

	/// <summary>
	/// Closes the screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Opens the PurchaseState.
	/// </summary>
	public void BtnPurchaseClick(Action action)
	{
		_game.PopState();
		_game.PushState(new PurchaseState(_base));
	}
}
