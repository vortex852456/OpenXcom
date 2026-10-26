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
// Ported from: src/Basescape/CraftPilotsState.h, src/Basescape/CraftPilotsState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Craft Pilots screen that shows the pilots of a craft and their bonuses.
/// </summary>
public class CraftPilotsState : State
{
	private readonly Base _base;
	private readonly int _craft;

	private readonly TextButton _btnOk, _btnAdd, _btnRemoveAll;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtFiringAcc, _txtReactions, _txtBravery, _txtPilots;
	private readonly Text _txtRequired;
	private readonly TextList _lstPilots;
	private readonly Text _txtAccuracyBonus, _txtAccuracyBonusValue;
	private readonly Text _txtDodgeBonus, _txtDodgeBonusValue;
	private readonly Text _txtApproachSpeed, _txtApproachSpeedValue;

	private static string Str(int value) => value.ToString(CultureInfo.InvariantCulture);

	/// <summary>
	/// Initializes all the elements in the Craft Pilots screen.
	/// </summary>
	public CraftPilotsState(Base @base, int craft)
	{
		_base = @base;
		_craft = craft;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnOk = new TextButton(300, 20, 10, 170);
		_txtTitle = new Text(310, 17, 5, 12);
		_txtFiringAcc = new Text(108, 9, 74, 32);
		_txtReactions = new Text(58, 9, 182, 32);
		_txtBravery = new Text(58, 9, 240, 32);
		_txtPilots = new Text(150, 9, 10, 40);
		_lstPilots = new TextList(288, 32, 10, 48);
		_btnAdd = new TextButton(146, 20, 10, 96);
		_btnRemoveAll = new TextButton(146, 20, 164, 96);

		_txtRequired = new Text(288, 9, 10, 84);

		_txtAccuracyBonus = new Text(100, 9, 10, 128);
		_txtAccuracyBonusValue = new Text(150, 9, 110, 128);
		_txtDodgeBonus = new Text(100, 9, 10, 140);
		_txtDodgeBonusValue = new Text(150, 9, 110, 140);
		_txtApproachSpeed = new Text(100, 9, 10, 152);
		_txtApproachSpeedValue = new Text(150, 9, 110, 152);

		// Set palette
		SetInterface("craftPilots");

		Add(_window, "window", "craftPilots");
		Add(_btnOk, "button", "craftPilots");
		Add(_txtTitle, "text1", "craftPilots");
		Add(_txtFiringAcc, "text1", "craftPilots");
		Add(_txtReactions, "text1", "craftPilots");
		Add(_txtBravery, "text1", "craftPilots");
		Add(_txtPilots, "text1", "craftPilots");
		Add(_lstPilots, "list", "craftPilots");
		Add(_btnAdd, "button", "craftPilots");
		Add(_btnRemoveAll, "button", "craftPilots");
		Add(_txtRequired, "text1", "craftPilots");
		Add(_txtAccuracyBonus, "text1", "craftPilots");
		Add(_txtAccuracyBonusValue, "text2", "craftPilots");
		Add(_txtDodgeBonus, "text1", "craftPilots");
		Add(_txtDodgeBonusValue, "text2", "craftPilots");
		Add(_txtApproachSpeed, "text1", "craftPilots");
		Add(_txtApproachSpeedValue, "text2", "craftPilots");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "craftPilots");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		Craft c = _base.Crafts[_craft];

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_PILOTS_FOR_CRAFT").Arg(c.GetName(_game.Language)));

		_txtFiringAcc.SetText(Tr("STR_FIRING_ACCURACY"));
		_txtFiringAcc.Align = TextHAlign.ALIGN_RIGHT;

		_txtReactions.SetText(Tr("STR_REACTIONS"));
		_txtReactions.Align = TextHAlign.ALIGN_RIGHT;

		_txtBravery.SetText(Tr("STR_BRAVERY"));
		_txtBravery.Align = TextHAlign.ALIGN_RIGHT;

		_txtPilots.SetText(Tr("STR_PILOTS"));

		_lstPilots.SetColumns(5, 114, 58, 58, 58, 0);
		_lstPilots.SetAlign(TextHAlign.ALIGN_RIGHT);
		_lstPilots.SetAlign(TextHAlign.ALIGN_LEFT, 0);
		_lstPilots.SetDot(true);

		_txtRequired.SetText(Tr("STR_PILOTS_REQUIRED").Arg(c.Rules.Pilots));

		_btnAdd.Text = Tr("STR_ADD_PILOT");
		_btnAdd.OnMouseClick(BtnAddClick);
		_btnRemoveAll.Text = Tr("STR_REMOVE_ALL_PILOTS");
		_btnRemoveAll.OnMouseClick(BtnRemoveAllClick);

		_txtAccuracyBonus.SetText(Tr("STR_ACCURACY_BONUS"));
		_txtDodgeBonus.SetText(Tr("STR_DODGE_BONUS"));
		_txtApproachSpeed.SetText(Tr("STR_APPROACH_SPEED"));

		foreach (var soldier in _base.Soldiers)
		{
			if (soldier.Craft == c)
			{
				soldier.PrepareStatsWithBonuses(_game.Mod); // refresh soldier bonuses
			}
		}
	}

	/// <summary>
	/// Initializes the state.
	/// </summary>
	public override void Init()
	{
		base.Init();
		UpdateUI();
	}

	/// <summary>Formats a percentage bonus with an explicit plus sign.</summary>
	internal static string FormatBonus(int bonus) => (bonus > 0 ? "+" : "") + Str(bonus) + "%";

	/// <summary>
	/// Updates the data on the screen.
	/// </summary>
	private void UpdateUI()
	{
		_lstPilots.ClearList();

		Craft c = _base.Crafts[_craft];

		List<Soldier> pilots = c.GetPilotList(false, null); // stats already refreshed in the constructor
		foreach (var pilot in pilots)
		{
			var stats = pilot.StatsWithSoldierBonusesOnly;
			_lstPilots.AddRow(5, pilot.GetName(false), Str(stats.firing), Str(stats.reactions), Str(stats.bravery), "");
		}

		int accBonus = c.GetPilotAccuracyBonus(pilots, _game.Mod);
		_txtAccuracyBonusValue.SetText(FormatBonus(accBonus));

		int dodgeBonus = c.GetPilotDodgeBonus(pilots, _game.Mod);
		_txtDodgeBonusValue.SetText(FormatBonus(dodgeBonus));

		int approachSpeed = c.GetPilotApproachSpeedModifier(pilots, _game.Mod);
		string ss3 = approachSpeed switch
		{
			1 => Tr("STR_COWARDLY"),
			2 => Tr("STR_NORMAL"),
			3 => Tr("STR_BOLD"),
			4 => Tr("STR_VERY_BOLD"),
			_ => Tr("STR_UNKNOWN"),
		};
		_txtApproachSpeedValue.SetText(ss3);

		_btnAdd.Visible = _lstPilots.Texts < c.Rules.Pilots;

		int availablePilots = 0;
		foreach (var soldier in _base.Soldiers)
		{
			// must be on board & able to drive
			if (soldier.Craft == c && soldier.HasAllPilotingRequirements())
			{
				availablePilots++;
			}
		}
		_btnRemoveAll.Visible = availablePilots != c.Rules.Pilots;
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Opens a Pilot Selection screen.
	/// </summary>
	public void BtnAddClick(Action action)
	{
		_game.PushState(new CraftPilotSelectState(_base, _craft));
	}

	/// <summary>
	/// Clears the pilot list.
	/// </summary>
	public void BtnRemoveAllClick(Action action)
	{
		Craft c = _base.Crafts[_craft];

		c.RemoveAllPilots();

		UpdateUI();
	}
}
