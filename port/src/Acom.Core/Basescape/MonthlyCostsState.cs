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
// Ported from: src/Basescape/MonthlyCostsState.h, src/Basescape/MonthlyCostsState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Monthly Costs screen that displays all
/// the maintenance costs of a particular base.
/// </summary>
public class MonthlyCostsState : State
{
	private readonly Base _base;

	private readonly TextButton _btnOk;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtCost, _txtQuantity, _txtTotal, _txtRental, _txtSalaries, _txtIncome, _txtMaintenance;
	private readonly TextList _lstCrafts, _lstSalaries, _lstMaintenance, _lstTotal;

	private static string Str(int value) => value.ToString(CultureInfo.InvariantCulture);

	/// <summary>
	/// Initializes all the elements in the Monthly Costs screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	public MonthlyCostsState(Base @base)
	{
		_base = @base;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnOk = new TextButton(300, 20, 10, 170);
		_txtTitle = new Text(310, 17, 5, 12);
		_txtCost = new Text(80, 9, 115, 32);
		_txtQuantity = new Text(55, 9, 195, 32);
		_txtTotal = new Text(60, 9, 249, 32);
		_txtRental = new Text(150, 9, 10, 40);
		_txtSalaries = new Text(150, 9, 10, 80);
		_txtIncome = new Text(150, 9, 10, 146);
		_txtMaintenance = new Text(150, 9, 10, 154);
		_lstCrafts = new TextList(288, 32, 10, 48);
		_lstSalaries = new TextList(288, 40, 10, 88);
		_lstMaintenance = new TextList(300, 9, 10, 128);
		_lstTotal = new TextList(100, 9, 205, 150);

		// Set palette
		SetInterface("costsInfo");

		Add(_window, "window", "costsInfo");
		Add(_btnOk, "button", "costsInfo");
		Add(_txtTitle, "text1", "costsInfo");
		Add(_txtCost, "text1", "costsInfo");
		Add(_txtQuantity, "text1", "costsInfo");
		Add(_txtTotal, "text1", "costsInfo");
		Add(_txtRental, "text1", "costsInfo");
		Add(_lstCrafts, "list", "costsInfo");
		Add(_txtSalaries, "text1", "costsInfo");
		Add(_lstSalaries, "list", "costsInfo");
		Add(_lstMaintenance, "text1", "costsInfo");
		Add(_txtIncome, "list", "costsInfo");
		Add(_txtMaintenance, "list", "costsInfo");
		Add(_lstTotal, "text2", "costsInfo");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "costsInfo");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_MONTHLY_COSTS"));

		_txtCost.SetText(Tr("STR_COST_PER_UNIT"));

		_txtQuantity.SetText(Tr("STR_QUANTITY"));

		_txtTotal.SetText(Tr("STR_TOTAL"));

		_txtRental.SetText(Tr("STR_CRAFT_RENTAL"));

		_txtSalaries.SetText(Tr("STR_SALARIES"));

		_txtIncome.SetText(Tr("STR_INCOME") + "=" + Unicode.FormatFunding(_game.SavedGame.CountryFunding));

		_txtMaintenance.SetText(Tr("STR_MAINTENANCE") + "=" + Unicode.FormatFunding(_game.SavedGame.BaseMaintenance));

		_lstCrafts.SetColumns(4, 125, 70, 44, 50);
		_lstCrafts.SetDot(true);

		foreach (var craftType in _game.Mod.CraftsList)
		{
			RuleCraft craft = _game.Mod.GetCraft(craftType);
			if (craft.RentCost != 0 && _game.SavedGame.IsResearched(craft.Requirements))
			{
				int count = _base.GetCraftCount(craft);
				if (count > 0 || craft.ForceShowInMonthlyCosts())
				{
					_lstCrafts.AddRow(4, Tr(craftType), Unicode.FormatFunding(craft.RentCost), Str(count), Unicode.FormatFunding(count * craft.RentCost));
				}
			}
		}

		_lstSalaries.SetColumns(4, 125, 70, 44, 50);
		_lstSalaries.SetDot(true);

		var soldierTypes = _game.Mod.SoldiersList;

		bool dynamicSalaries = false;
		foreach (var soldierType in soldierTypes)
		{
			if (_game.Mod.GetSoldier(soldierType).IsSalaryDynamic)
			{
				dynamicSalaries = true;
				break;
			}
		}

		if (!dynamicSalaries)
		{
			// vanilla
			foreach (var soldierType in soldierTypes)
			{
				RuleSoldier soldier = _game.Mod.GetSoldier(soldierType);
				if (soldier.GetSalaryCost(0) != 0 && _game.SavedGame.IsResearched(soldier.Requirements))
				{
					var info = _base.GetSoldierCountAndSalary(soldierType);
					string name = soldierType;
					if (soldierTypes.Count == 1)
					{
						name = "STR_SOLDIERS";
					}
					string costPerUnit = Unicode.FormatFunding(soldier.GetSalaryCost(0)); // 0 = default rookie salary
					if (info.first > 0 || soldierTypes.Count == 1)
					{
						_lstSalaries.AddRow(4, Tr(name), costPerUnit, Str(info.first), Unicode.FormatFunding(info.second));
					}
				}
			}
		}
		else
		{
			// one or more soldier types with *different* salaries per rank
			int count = 0;
			int salary = 0;
			foreach (var soldierType in soldierTypes)
			{
				var info = _base.GetSoldierCountAndSalary(soldierType);
				count += info.first;
				salary += info.second;
			}
			_lstSalaries.AddRow(4, Tr("STR_SOLDIERS"), "", Str(count), Unicode.FormatFunding(salary));
		}
		_lstSalaries.AddRow(4, Tr("STR_ENGINEERS"), Unicode.FormatFunding(_game.Mod.EngineerCost), Str(_base.TotalEngineers), Unicode.FormatFunding(_base.TotalEngineers * _game.Mod.EngineerCost));
		_lstSalaries.AddRow(4, Tr("STR_SCIENTISTS"), Unicode.FormatFunding(_game.Mod.ScientistCost), Str(_base.TotalScientists), Unicode.FormatFunding(_base.TotalScientists * _game.Mod.ScientistCost));
		int staffCount = 0, inventoryCount = 0;
		int totalOtherCost = _base.GetTotalOtherStaffAndInventoryCost(ref staffCount, ref inventoryCount);
		_lstSalaries.AddRow(4, Tr("STR_OTHER_EMPLOYEES"), "", Str(staffCount) + "/" + Str(inventoryCount), Unicode.FormatFunding(totalOtherCost));

		_lstMaintenance.SetColumns(2, 239, 60);
		_lstMaintenance.SetDot(true);
		_lstMaintenance.AddRow(2, Tr("STR_BASE_MAINTENANCE"), Unicode.TOK_COLOR_FLIP + Unicode.FormatFunding(_base.FacilityMaintenance));

		_lstTotal.SetColumns(2, 44, 55);
		_lstTotal.SetDot(true);
		_lstTotal.AddRow(2, Tr("STR_TOTAL"), Unicode.FormatFunding(_base.MonthlyMaintenace));
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}
}
