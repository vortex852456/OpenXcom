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
// Ported from: src/Battlescape/CannotReequipState.h, src/Battlescape/CannotReequipState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Screen shown when there's not enough equipment
/// to re-equip a craft after a mission.
/// </summary>
public class CannotReequipState : State
{
	private readonly List<ReequipStat> _missingItems;
	private readonly Base _base;
	private readonly TextButton _btnOk, _btnManufacture, _btnPurchase;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtItem, _txtQuantity, _txtCraft;
	private readonly TextList _lstItems;

	/// <summary>Initializes all the elements in the Cannot Reequip screen.</summary>
	/// <param name="missingItems">List of items still needed for reequip (copied, like the C++ vector of values).</param>
	/// <param name="base">Relevant xcom base.</param>
	public CannotReequipState(List<ReequipStat> missingItems, Base @base)
	{
		ArgumentNullException.ThrowIfNull(missingItems);
		_missingItems = missingItems.ConvertAll(x => new ReequipStat { item = x.item, qty = x.qty, craft = x.craft, listOrder = x.listOrder });
		_base = @base;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnManufacture = new TextButton(128, 14, 10, 178);
		_btnPurchase = new TextButton(128, 14, 144, 178);
		_btnOk = new TextButton(34, 14, 278, 178);
		_txtTitle = new Text(220, 32, 50, 8);
		_txtItem = new Text(142, 9, 10, 50);
		_txtQuantity = new Text(88, 9, 152, 50);
		_txtCraft = new Text(74, 9, 218, 50);
		_lstItems = new TextList(288, 112, 8, 58);

		// Set palette
		SetInterface("cannotReequip");

		Add(_window, "window", "cannotReequip");
		Add(_btnManufacture, "button", "cannotReequip");
		Add(_btnPurchase, "button", "cannotReequip");
		Add(_btnOk, "button", "cannotReequip");
		Add(_txtTitle, "heading", "cannotReequip");
		Add(_txtItem, "text", "cannotReequip");
		Add(_txtQuantity, "text", "cannotReequip");
		Add(_txtCraft, "text", "cannotReequip");
		Add(_lstItems, "list", "cannotReequip");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "cannotReequip");

		_btnManufacture.Text = Tr("STR_MANUFACTURE");
		_btnManufacture.OnMouseClick(BtnManufactureClick);

		_btnPurchase.Text = Tr("STR_PURCHASE_RECRUIT");
		_btnPurchase.OnMouseClick(BtnPurchaseClick);

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtTitle.SetText(Tr("STR_NOT_ENOUGH_EQUIPMENT_TO_FULLY_RE_EQUIP_SQUAD"));
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetBig();
		_txtTitle.SetWordWrap(true);

		_txtItem.SetText(Tr("STR_ITEM"));

		_txtQuantity.SetText(Tr("STR_QUANTITY_UC"));

		_txtCraft.SetText(Tr("STR_CRAFT"));

		_lstItems.SetColumns(3, 162, 46, 80);
		_lstItems.SetSelectable(true);
		_lstItems.SetBackground(_window);
		_lstItems.Margin = 2;
	}

	/// <summary>Resets stuff when coming back from other screens.</summary>
	public override void Init()
	{
		base.Init();

		_lstItems.ClearList();

		foreach (ReequipStat reequipStat in _missingItems)
		{
			if (reequipStat.qty > 0)
			{
				_lstItems.AddRow(3, Tr(reequipStat.item), reequipStat.qty.ToString(CultureInfo.InvariantCulture), reequipStat.craft);
			}
		}
	}

	/// <summary>Returns to the previous screen.</summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>Goes to the Manufacture screen.</summary>
	public void BtnManufactureClick(Action action)
	{
		_game.PushState(new ManufactureState(_base));
	}

	/// <summary>Goes to the Purchase screen.</summary>
	public void BtnPurchaseClick(Action action)
	{
		_game.PushState(new PurchaseState(_base, this));
	}

	/// <summary>Gets the list of missing items.</summary>
	public List<ReequipStat> MissingItems { get => _missingItems; }

	/// <summary>Decreases the number of missing items by the bought amount.</summary>
	/// <param name="rule">Type of item.</param>
	/// <param name="amount">Number of items bought.</param>
	public void DecreaseMissingItemCount(RuleItem rule, int amount)
	{
		ArgumentNullException.ThrowIfNull(rule);
		foreach (ReequipStat reequipStat in _missingItems)
		{
			if (reequipStat.item == rule.Type)
			{
				reequipStat.qty = Math.Max(0, reequipStat.qty - amount);
				break;
			}
		}
	}
}
