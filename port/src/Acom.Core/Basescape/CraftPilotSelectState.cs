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
// Ported from: src/Basescape/CraftPilotSelectState.h, src/Basescape/CraftPilotSelectState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Select Pilot window that allows assigning
/// a pilot to a craft.
/// </summary>
public class CraftPilotSelectState : State
{
	private readonly Base _base;
	private readonly int _craft;

	private readonly TextButton _btnCancel;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtName, _txtFiringAcc, _txtReactions, _txtBravery;
	private readonly TextList _lstPilot;
	private readonly List<int> _pilot = [];

	private static string Str(int value) => value.ToString(CultureInfo.InvariantCulture);

	/// <summary>
	/// Initializes all the elements in the Select Pilot window.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="craft">ID of the selected craft.</param>
	public CraftPilotSelectState(Base @base, int craft)
	{
		_base = @base;
		_craft = craft;
		_screen = false;

		// Create objects
		_window = new Window(this, 232, 156, 44, 28, WindowPopup.POPUP_BOTH);
		_btnCancel = new TextButton(180, 16, 70, 158);
		_txtTitle = new Text(222, 9, 49, 36);
		_txtName = new Text(120, 9, 60, 52);
		_txtFiringAcc = new Text(20, 9, 190, 52);
		_txtReactions = new Text(20, 9, 210, 52);
		_txtBravery = new Text(20, 9, 230, 52);
		_lstPilot = new TextList(200, 80, 53, 68);

		SetInterface("craftPilotsSelect");

		Add(_window, "window", "craftPilotsSelect");
		Add(_btnCancel, "button", "craftPilotsSelect");
		Add(_txtTitle, "text2", "craftPilotsSelect");
		Add(_txtName, "text1", "craftPilotsSelect");
		Add(_txtFiringAcc, "text1", "craftPilotsSelect");
		Add(_txtReactions, "text1", "craftPilotsSelect");
		Add(_txtBravery, "text1", "craftPilotsSelect");
		Add(_lstPilot, "list", "craftPilotsSelect");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "craftPilotsSelect");

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		Craft c = _base.Crafts[_craft];

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_SELECT_PILOT"));

		_txtName.SetText(Tr("STR_NAME"));

		_txtFiringAcc.SetText(Tr("STR_FIRING_ACCURACY_ABBREVIATION"));
		_txtReactions.SetText(Tr("STR_REACTIONS_ABBREVIATION"));
		_txtBravery.SetText(Tr("STR_BRAVERY_ABBREVIATION"));

		_lstPilot.SetColumns(4, 124, 20, 20, 20);
		_lstPilot.SetAlign(TextHAlign.ALIGN_RIGHT);
		_lstPilot.SetAlign(TextHAlign.ALIGN_LEFT, 0);
		_lstPilot.SetSelectable(true);
		_lstPilot.SetBackground(_window);
		_lstPilot.Margin = 8;

		foreach (var soldier in _base.Soldiers)
		{
			// must be on board & able to drive
			if (soldier.Craft == c && soldier.HasAllPilotingRequirements())
			{
				// is not assigned yet
				if (!c.IsPilot(soldier.Id))
				{
					_pilot.Add(soldier.Id);

					var stats = soldier.StatsWithSoldierBonusesOnly;
					_lstPilot.AddRow(4, soldier.GetName(false), Str(stats.firing), Str(stats.reactions), Str(stats.bravery));
				}
			}
		}

		_lstPilot.OnMouseClick(LstPilotClick);
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Adds the selected pilot to the list and returns to the previous screen.
	/// </summary>
	public void LstPilotClick(Action action)
	{
		int pilotId = _pilot[(int)_lstPilot.SelectedRow];
		Craft c = _base.Crafts[_craft];
		c.AddPilot(pilotId);

		_game.PopState();
	}
}
