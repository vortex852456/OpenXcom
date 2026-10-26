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
// Ported from: src/Geoscape/DogfightExperienceState.h, src/Geoscape/DogfightExperienceState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // UI handlers stay instance members like in C++ (State._game is static here)

/// <summary>
/// Dogfight experience screen: shows the pilot experience gained today.
/// </summary>
public class DogfightExperienceState : State
{
	private readonly Window _window;
	private readonly TextButton _btnOk;
	private readonly Text _txtTitle, _txtFiringAcc, _txtReactions, _txtBravery, _txtPilots;
	private readonly TextList _lstPilots;

	/// <summary>
	/// Initializes all the elements in the Dogfight Experience screen.
	/// </summary>
	public DogfightExperienceState()
	{
		_screen = false;

		// Create objects
		_window = new Window(this, 320, 140, 0, 30, WindowPopup.POPUP_HORIZONTAL);
		_btnOk = new TextButton(300, 16, 10, 146);
		_txtTitle = new Text(300, 17, 10, 46);
		_txtFiringAcc = new Text(108, 9, 74, 70);
		_txtReactions = new Text(58, 9, 182, 70);
		_txtBravery = new Text(58, 9, 240, 70);
		_txtPilots = new Text(80, 9, 10, 70);
		_lstPilots = new TextList(288, 64, 10, 78);

		// Set palette
		SetInterface("dogfightExperience");

		Add(_window, "window", "dogfightExperience");
		Add(_btnOk, "button", "dogfightExperience");
		Add(_txtTitle, "text1", "dogfightExperience");
		Add(_txtFiringAcc, "text2", "dogfightExperience");
		Add(_txtReactions, "text2", "dogfightExperience");
		Add(_txtBravery, "text2", "dogfightExperience");
		Add(_txtPilots, "text2", "dogfightExperience");
		Add(_lstPilots, "list", "dogfightExperience");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "dogfightExperience");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_DAILY_PILOT_EXPERIENCE"));

		_txtFiringAcc.SetText(Tr("STR_FIRING_ACCURACY"));
		_txtFiringAcc.Align = TextHAlign.ALIGN_RIGHT;

		_txtReactions.SetText(Tr("STR_REACTIONS"));
		_txtReactions.Align = TextHAlign.ALIGN_RIGHT;

		_txtBravery.SetText(Tr("STR_BRAVERY"));
		_txtBravery.Align = TextHAlign.ALIGN_RIGHT;

		_txtPilots.SetText(Tr("STR_NAME"));

		_lstPilots.SetColumns(5, 114, 58, 58, 58, 0);
		_lstPilots.SetAlign(TextHAlign.ALIGN_RIGHT);
		_lstPilots.SetAlign(TextHAlign.ALIGN_LEFT, 0);
		_lstPilots.SetDot(true);

		_lstPilots.ClearList();
		foreach (var xbase in _game.SavedGame.Bases)
		{
			foreach (var soldier in xbase.Soldiers)
			{
				UnitStats tmp = soldier.DailyDogfightExperienceCache;
				if (tmp.firing > 0 || tmp.reactions > 0 || tmp.bravery > 0)
				{
					string ss1 = tmp.firing > 0 ? Gain(tmp.firing) : "";
					string ss2 = tmp.reactions > 0 ? Gain(tmp.reactions) : "";
					string ss3 = tmp.bravery > 0 ? Gain(tmp.bravery) : "";
					_lstPilots.AddRow(5, soldier.GetName(false), ss1, ss2, ss3, "");
				}
			}
		}
	}

	/// <summary>Formats a stat gain as a highlighted "+N".</summary>
	internal static string Gain(int value) =>
		$"{Unicode.TOK_COLOR_FLIP}+{value.ToString(CultureInfo.InvariantCulture)}{Unicode.TOK_COLOR_FLIP}";

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}
}
