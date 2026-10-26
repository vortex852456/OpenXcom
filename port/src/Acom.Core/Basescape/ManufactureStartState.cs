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
// Ported from: src/Basescape/ManufactureStartState.h, src/Basescape/ManufactureStartState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Screen which displays needed elements to start productions (items/required workshop state/cost to build a unit, ...).
/// </summary>
public class ManufactureStartState : State
{
	private readonly Base _base;
	private readonly RuleManufacture _item;
	private readonly Window _window;
	private readonly TextButton _btnCancel, _btnStart;
	private readonly Text _txtTitle, _txtManHour, _txtCost, _txtWorkSpace, _txtRequiredItemsTitle, _txtItemNameColumn, _txtUnitRequiredColumn, _txtUnitAvailableColumn;
	private readonly TextList _lstRequiredItems;

	/// <summary>
	/// Initializes all the elements in the productions start screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="item">The RuleManufacture to produce.</param>
	public ManufactureStartState(Base @base, RuleManufacture item)
	{
		_base = @base;
		_item = item;

		_screen = false;

		_window = new Window(this, 320, 160, 0, 20);
		_btnCancel = new TextButton(136, 16, 16, 155);
		_txtTitle = new Text(320, 17, 0, 30);
		_txtManHour = new Text(290, 9, 16, 50);
		_txtCost = new Text(290, 9, 16, 60);
		_txtWorkSpace = new Text(290, 9, 16, 70);

		_txtRequiredItemsTitle = new Text(290, 9, 16, 84);
		_txtItemNameColumn = new Text(60, 16, 30, 92);
		_txtUnitRequiredColumn = new Text(60, 16, 155, 92);
		_txtUnitAvailableColumn = new Text(60, 16, 230, 92);
		_lstRequiredItems = new TextList(270, 40, 30, 108);

		_btnStart = new TextButton(136, 16, 168, 155);

		// Set palette
		SetInterface("allocateManufacture");

		Add(_window, "window", "allocateManufacture");
		Add(_txtTitle, "text", "allocateManufacture");
		Add(_txtManHour, "text", "allocateManufacture");
		Add(_txtCost, "text", "allocateManufacture");
		Add(_txtWorkSpace, "text", "allocateManufacture");
		Add(_btnCancel, "button", "allocateManufacture");

		Add(_txtRequiredItemsTitle, "text", "allocateManufacture");
		Add(_txtItemNameColumn, "text", "allocateManufacture");
		Add(_txtUnitRequiredColumn, "text", "allocateManufacture");
		Add(_txtUnitAvailableColumn, "text", "allocateManufacture");
		Add(_lstRequiredItems, "list", "allocateManufacture");

		Add(_btnStart, "button", "allocateManufacture");

		CenterAllSurfaces();

		SetWindowBackground(_window, "allocateManufacture");

		_txtTitle.SetText(Tr(_item.Name));
		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;

		_txtManHour.SetText(Tr("STR_ENGINEER_HOURS_TO_PRODUCE_ONE_UNIT").Arg(_item.ManufactureTime));

		_txtCost.SetText(Tr("STR_COST_PER_UNIT_").Arg(Unicode.FormatFunding(_item.ManufactureCost)));

		_txtWorkSpace.SetText(Tr("STR_WORK_SPACE_REQUIRED").Arg(_item.RequiredSpace));

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		bool productionPossible = _item.HaveEnoughMoneyForOneMoreUnit(_game.SavedGame.Funds);
		// check available workspace later
		//int availableWorkSpace = _base->getFreeWorkshops();
		//productionPossible &= (availableWorkSpace > 0);

		_txtRequiredItemsTitle.SetText(Tr("STR_SPECIAL_MATERIALS_REQUIRED"));
		_txtRequiredItemsTitle.Align = TextHAlign.ALIGN_CENTER;

		_txtItemNameColumn.SetText(Tr("STR_ITEM_REQUIRED"));
		_txtItemNameColumn.SetWordWrap(true);

		_txtUnitRequiredColumn.SetText(Tr("STR_UNITS_REQUIRED"));
		_txtUnitRequiredColumn.SetWordWrap(true);

		_txtUnitAvailableColumn.SetText(Tr("STR_UNITS_AVAILABLE"));
		_txtUnitAvailableColumn.SetWordWrap(true);

		_lstRequiredItems.SetColumns(3, 140, 75, 55);
		_lstRequiredItems.SetBackground(_window);

		bool hasRequirements = _item.RequiredCrafts.Count > 0 || _item.RequiredItems.Count > 0;
		int row = 0;
		foreach (var iter in _item.RequiredCrafts)
		{
			int count = @base.GetCraftCountForProduction(iter.Key);

			string s1 = iter.Value.ToString(CultureInfo.InvariantCulture);
			string s2 = count.ToString(CultureInfo.InvariantCulture);
			productionPossible &= (count >= iter.Value);
			_lstRequiredItems.AddRow(3, Tr(iter.Key.Type), s1, s2);
			_lstRequiredItems.SetCellColor(row, 1, _lstRequiredItems.SecondaryColor);
			_lstRequiredItems.SetCellColor(row, 2, _lstRequiredItems.SecondaryColor);
			row++;
		}
		foreach (var iter in _item.RequiredItems)
		{
			int count = @base.StorageItems.GetItem(iter.Key);

			string s1 = iter.Value.ToString(CultureInfo.InvariantCulture);
			string s2 = count.ToString(CultureInfo.InvariantCulture);
			productionPossible &= (count >= iter.Value);
			_lstRequiredItems.AddRow(3, Tr(iter.Key.Type), s1, s2);
			_lstRequiredItems.SetCellColor(row, 1, _lstRequiredItems.SecondaryColor);
			_lstRequiredItems.SetCellColor(row, 2, _lstRequiredItems.SecondaryColor);
			row++;
		}
		if (_item.SpawnedPersonType != "")
		{
			if (@base.AvailableQuarters <= @base.UsedQuarters)
			{
				productionPossible = false;
			}

			// separator line
			_lstRequiredItems.AddRow(1, Tr("STR_PERSON_JOINING"));
			_lstRequiredItems.SetCellColor(row, 0, _lstRequiredItems.SecondaryColor);
			row++;

			// person joining
			string s1 = Unicode.TOK_COLOR_FLIP + "1";
			_lstRequiredItems.AddRow(2, Tr(_item.SpawnedPersonName != "" ? _item.SpawnedPersonName : _item.SpawnedPersonType), s1);
			row++;
		}
		if (_item.RandomProducedItems.Count != 0)
		{
			// separator line
			_lstRequiredItems.AddRow(1, Tr("STR_RANDOM_PRODUCTION_DISCLAIMER"));
			_lstRequiredItems.SetCellColor(row, 0, _lstRequiredItems.SecondaryColor);
			row++;
		}
		bool hasVanillaOutput = false;
		if (_item.ProducedItems.Count == 1)
		{
			RuleItem match = _game.Mod.GetItem(_item.Name, false);
			if (match != null)
			{
				if (_item.ProducedItems.TryGetValue(match, out int amount))
				{
					hasVanillaOutput = (amount == 1);
				}
			}
		}
		if (!hasVanillaOutput && _item.ProducedItems.Count != 0)
		{
			// separator line
			_lstRequiredItems.AddRow(1, Tr("STR_UNITS_PRODUCED"));
			_lstRequiredItems.SetCellColor(row, 0, _lstRequiredItems.SecondaryColor);
			row++;

			// produced items
			foreach (var iter in _item.ProducedItems)
			{
				string s1 = Unicode.TOK_COLOR_FLIP + iter.Value.ToString(CultureInfo.InvariantCulture);
				_lstRequiredItems.AddRow(2, Tr(iter.Key.Type), s1);
				row++;
			}
		}

		_txtRequiredItemsTitle.Visible = hasRequirements;
		_txtItemNameColumn.Visible = hasRequirements;
		_txtUnitRequiredColumn.Visible = hasRequirements;
		_txtUnitAvailableColumn.Visible = hasRequirements;
		_lstRequiredItems.Visible = row != 0;

		_btnStart.Text = Tr("STR_START_PRODUCTION");
		_btnStart.OnMouseClick(BtnStartClick);
		_btnStart.OnKeyboardPress(BtnStartClick, Options.keyOk);
		_btnStart.Visible = productionPossible;

		if (_item != null)
		{
			// mark new as normal
			if (_game.SavedGame.GetManufactureRuleStatus(_item.Name) == RuleManufacture.MANU_STATUS_NEW)
			{
				_game.SavedGame.SetManufactureRuleStatus(_item.Name, RuleManufacture.MANU_STATUS_NORMAL);
			}
		}
	}

	/// <summary>
	/// Returns to previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Go to the Production settings screen.
	/// </summary>
	public void BtnStartClick(Action action)
	{
		if (_item.ProducedCraft != null && _base.AvailableHangars - _base.UsedHangars <= 0)
		{
			_game.PushState(new ErrorMessageState(Tr("STR_NO_FREE_HANGARS_FOR_CRAFT_PRODUCTION"), _palette, unchecked((byte)_game.Mod.GetInterface("basescape").GetElement("errorMessage").color), "BACK17.SCR", _game.Mod.GetInterface("basescape").GetElement("errorPalette").color));
		}
		//else if (_item->getRequiredSpace() > _base->getFreeWorkshops())
		//{
		//	_game->pushState(new ErrorMessageState(tr("STR_NOT_ENOUGH_WORK_SPACE"), ...));
		//}
		else
		{
			_game.PushState(new ManufactureInfoState(_base, _item));
		}
	}
}
