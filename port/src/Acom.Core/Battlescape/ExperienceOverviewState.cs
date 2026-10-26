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
// Ported from: src/Battlescape/ExperienceOverviewState.h, src/Battlescape/ExperienceOverviewState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Screen which shows the experience gained by all soldiers in the current battle.
/// </summary>
public class ExperienceOverviewState : State
{
	private readonly TextButton _btnOk;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtName;
	private readonly Text _txtBravery, _txtReactions, _txtFiring, _txtThrowing, _txtPsiSkill, _txtPsiStrength, _txtMelee, _txtMana;
	private readonly TextList _lstSoldiers;

	private readonly BattlescapeState _parent;
	private readonly List<BattleUnit> _soldiers = [];

	/// <summary>Initializes all the elements in the Experience Overview screen.</summary>
	public ExperienceOverviewState(BattlescapeState parent)
	{
		_parent = parent;
		_screen = false;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_txtTitle = new Text(300, 17, 10, 13);
		_txtName = new Text(100, 10, 10, 40);
		_btnOk = new TextButton(160, 16, 80, 174);
		_lstSoldiers = new TextList(286, 112, 8, 52);
		_txtBravery = new Text(18, 10, 120 - 3, 40);
		_txtReactions = new Text(18, 10, 141 - 3, 40);
		_txtFiring = new Text(18, 10, 162 - 3, 40);
		_txtThrowing = new Text(18, 10, 183 - 3, 40);
		_txtPsiSkill = new Text(18, 10, 204 - 3, 40);
		_txtPsiStrength = new Text(18, 10, 225 - 3, 40);
		_txtMelee = new Text(18, 10, 246 - 3, 40);
		_txtMana = new Text(18, 10, 267 - 3, 40);

		// Set palette
		_game.SavedGame.SavedBattle.SetPaletteByDepth(this);

		Add(_window, "messageWindowBorder", "battlescape");
		Add(_btnOk, "messageWindowButtons", "battlescape");
		Add(_txtName, "messageWindows", "battlescape");
		Add(_txtTitle, "messageWindows", "battlescape");
		Add(_lstSoldiers, "optionLists", "battlescape");
		Add(_txtBravery, "messageWindows", "battlescape");
		Add(_txtReactions, "messageWindows", "battlescape");
		Add(_txtFiring, "messageWindows", "battlescape");
		Add(_txtThrowing, "messageWindows", "battlescape");
		Add(_txtPsiSkill, "messageWindows", "battlescape");
		Add(_txtPsiStrength, "messageWindows", "battlescape");
		Add(_txtMelee, "messageWindows", "battlescape");
		Add(_txtMana, "messageWindows", "battlescape");

		CenterAllSurfaces();

		// Set up objects
		_window.SetHighContrast(true);
		_window.SetBackground(_game.Mod.GetSurface("TAC00.SCR"));

		_btnOk.SetHighContrast(true);
		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtTitle.SetHighContrast(true);
		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_EXPERIENCE_OVERVIEW"));

		_txtName.SetHighContrast(true);
		_txtBravery.SetHighContrast(true);
		_txtReactions.SetHighContrast(true);
		_txtFiring.SetHighContrast(true);
		_txtThrowing.SetHighContrast(true);
		_txtPsiSkill.SetHighContrast(true);
		_txtPsiStrength.SetHighContrast(true);
		_txtMelee.SetHighContrast(true);
		_txtMana.SetHighContrast(true);

		_txtName.SetText(Tr("STR_NAME"));
		_txtBravery.SetText(Tr("STR_BRAVERY_ABBREVIATION"));
		_txtReactions.SetText(Tr("STR_REACTIONS_ABBREVIATION"));
		_txtFiring.SetText(Tr("STR_FIRING_ACCURACY_ABBREVIATION"));
		_txtThrowing.SetText(Tr("STR_THROWING_ACCURACY_ABBREVIATION"));
		_txtPsiSkill.SetText(Tr("STR_PSIONIC_SKILL_ABBREVIATION"));
		_txtPsiStrength.SetText(Tr("STR_PSIONIC_STRENGTH_ABBREVIATION"));
		_txtMelee.SetText(Tr("STR_MELEE_ACCURACY_ABBREVIATION"));
		bool showMana = _game.Mod.IsManaFeatureEnabled && _game.Mod.IsManaTrainingPrimary;
		if (showMana)
		{
			_txtMana.SetText(Tr("STR_MANA_ABBREVIATION"));
		}

		_lstSoldiers.SetColumns(10, 110, 21, 21, 21, 21, 21, 21, 21, 21, 18);
		_lstSoldiers.SetSelectable(true);
		_lstSoldiers.SetHighContrast(true);
		_lstSoldiers.SetBackground(_window);
		_lstSoldiers.Margin = 2;
		_lstSoldiers.OnMouseClick(LstSoldiersClick);

		_lstSoldiers.ClearList();
		int row = 0;
		foreach (BattleUnit soldier in _game.SavedGame.SavedBattle.Units)
		{
			if (soldier.GeoscapeSoldier == null)
			{
				continue;
			}
			if (soldier.Status == UnitStatus.STATUS_DEAD || soldier.Status == UnitStatus.STATUS_IGNORE_ME)
			{
				continue;
			}

			UnitStats stats = soldier.ExpStats;

			string mana = "";
			if (showMana)
			{
				mana = stats.mana.ToString(CultureInfo.InvariantCulture);
			}

			_soldiers.Add(soldier);
			_lstSoldiers.AddRow(10,
				soldier.GetName(_game.Language),
				stats.bravery.ToString(CultureInfo.InvariantCulture),
				stats.reactions.ToString(CultureInfo.InvariantCulture),
				stats.firing.ToString(CultureInfo.InvariantCulture),
				stats.throwing.ToString(CultureInfo.InvariantCulture),
				stats.psiSkill.ToString(CultureInfo.InvariantCulture),
				stats.psiStrength.ToString(CultureInfo.InvariantCulture),
				stats.melee.ToString(CultureInfo.InvariantCulture),
				mana,
				"");
			for (int col = 1; col < 9; ++col)
			{
				if (_lstSoldiers.GetCellText(row, col) != "0")
				{
					_lstSoldiers.SetCellColor(row, col, _lstSoldiers.SecondaryColor);
				}
			}
			row++;
		}
	}

	/// <summary>Returns to the previous screen.</summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>Selects the soldier if possible and returns to the previous screen.</summary>
	public void LstSoldiersClick(Action action)
	{
		if (_parent.AllowButtons())
		{
			int index = (int)_lstSoldiers.SelectedRow;
			BattleUnit bu = _soldiers[index];

			if (bu.IsSelectable(_game.SavedGame.SavedBattle.Side, false, false))
			{
				// select
				_parent.BattleGame.CancelAllActions();
				_parent.BattleGame.PrimaryAction(bu.Position);
			}

			// center on position
			_parent.Map.Camera.CenterOnPosition(bu.Position);

			_game.PopState();
		}
	}
}
