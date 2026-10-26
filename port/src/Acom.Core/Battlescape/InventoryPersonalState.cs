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
// Ported from: src/Battlescape/InventoryPersonalState.h, src/Battlescape/InventoryPersonalState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

/// <summary>
/// Window showing the personal equipment layout of a soldier.
/// </summary>
public class InventoryPersonalState : State
{
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly TextList _lstLayout;
	private readonly TextButton _btnCancel;

	/// <summary>Initializes all the elements in the InventoryPersonalState.</summary>
	public InventoryPersonalState(Soldier soldier)
	{
		ArgumentNullException.ThrowIfNull(soldier);
		_screen = false;

		// Create objects
		_window = new Window(this, 196, 160, 64, 20, WindowPopup.POPUP_BOTH);
		_txtTitle = new Text(186, 16, 69, 30);
		_lstLayout = new TextList(164, 104, 72, 44);
		_btnCancel = new TextButton(144, 16, 90, 156);

		SetStandardPalette("PAL_BATTLESCAPE");

		Add(_window, "messageWindowBorder", "battlescape");
		Add(_txtTitle, "messageWindows", "battlescape");
		Add(_lstLayout, "optionLists", "battlescape");
		Add(_btnCancel, "messageWindowButtons", "battlescape");

		CenterAllSurfaces();

		// Set up objects
		_window.SetHighContrast(true);
		_window.SetBackground(_game.Mod.GetSurface("TAC00.SCR"));

		_txtTitle.SetHighContrast(true);
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_PERSONAL_EQUIPMENT"));

		_lstLayout.SetHighContrast(true);
		_lstLayout.SetColumns(2, 136, 12);
		_lstLayout.SetSelectable(true);
		_lstLayout.SetBackground(_window);
		_lstLayout.Margin = 8;

		_btnCancel.SetHighContrast(true);
		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		// 1. tally items
		// C++ std::map<const RuleItem*, int> iterates in pointer order (unspecified); insertion order here
		var summary = new Dictionary<RuleItem, int>();
		var sorted = new List<RuleItem>();
		void tally(RuleItem type)
		{
			if (!summary.TryGetValue(type, out int count))
			{
				sorted.Add(type);
			}
			summary[type] = count + 1;
		}
		foreach (EquipmentLayoutItem layoutItem in soldier.PersonalEquipmentLayout)
		{
			// item
			tally(layoutItem.ItemType);

			// ammo
			for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
			{
				RuleItem loadedAmmoType = layoutItem.GetAmmoItemForSlot(slot);
				if (loadedAmmoType != null)
				{
					tally(loadedAmmoType);
				}
			}
		}

		// 2. sort items
		StdAlgorithm.Sort(sorted, (a, b) => a.ListOrder < b.ListOrder);

		// display armor
		if (Options.oxcePersonalLayoutIncludingArmor)
		{
			string ss1 = "", ss2 = "";
			Armor armor = soldier.PersonalEquipmentArmor;
			if (armor != null)
			{
				ss1 += Tr(armor.Type);
			}
			_lstLayout.AddRow(2, ss1, ss2);
		}

		// 3. display items
		foreach (RuleItem ruleItem in sorted)
		{
			string ss1 = "";
			if (ruleItem.BattleType == BattleType.BT_AMMO)
			{
				ss1 += "  ";
			}
			ss1 += Tr(ruleItem.Type);
			string ss2 = summary[ruleItem].ToString(CultureInfo.InvariantCulture);
			_lstLayout.AddRow(2, ss1, ss2);
		}
	}

#pragma warning disable CA1822 // instance handler in C++ (State._game is static here)
	/// <summary>Returns to the previous screen.</summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}
#pragma warning restore CA1822
}
