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
// Ported from: src/Geoscape/NewPossibleFacilityState.h, src/Geoscape/NewPossibleFacilityState.cpp
#nullable disable

using System.Text;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window which inform the player of new possible base facilities to build.
/// </summary>
public class NewPossibleFacilityState : State
{
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly TextList _lstPossibilities;
	private readonly Text _txtCaveat;
	private readonly TextButton _btnOpen, _btnOk;
	private readonly Base _base;
	private readonly Globe _globe;

	/// <summary>
	/// Initializes all the elements in the NewPossibleFacility screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="globe">Pointer to the globe.</param>
	/// <param name="possibilities">List of newly possible RuleBaseFacility.</param>
	public NewPossibleFacilityState(Base @base, Globe globe, List<RuleBaseFacility> possibilities)
	{
		_base = @base;
		_globe = globe;
		_screen = false;

		// Create objects
		_window = new Window(this, 288, 180, 16, 10);
		_btnOk = new TextButton(160, 14, 80, 149);
		_btnOpen = new TextButton(160, 14, 80, 165);
		_txtTitle = new Text(288, 40, 16, 20);
		_lstPossibilities = new TextList(250, 80, 35, 50);
		_txtCaveat = new Text(250, 16, 35, 131);

		// Set palette
		SetInterface("geoNewFacility");

		Add(_window, "window", "geoNewFacility");
		Add(_btnOk, "button", "geoNewFacility");
		Add(_btnOpen, "button", "geoNewFacility");
		Add(_txtTitle, "text1", "geoNewFacility");
		Add(_lstPossibilities, "text2", "geoNewFacility");
		Add(_txtCaveat, "text1", "geoNewFacility");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "geoNewFacility");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnOpen.Text = Tr("STR_BASES");
		_btnOpen.OnMouseClick(BtnOpenClick);
		_btnOpen.OnKeyboardPress(BtnOpenClick, Options.keyOk);
		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_WE_CAN_NOW_BUILD"));

		// Caveat
		{
			RuleBaseFacilityFunctions requiredServices = default;
			foreach (var facRule in possibilities)
			{
				requiredServices |= facRule.RequireBaseFunc;
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
		foreach (var facRule in possibilities)
		{
			_lstPossibilities.AddRow(1, Tr(facRule.Type));
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
	/// Opens the BaseView.
	/// </summary>
	public void BtnOpenClick(Action action)
	{
		_game.PopState();
		_game.PushState(new BasescapeState(_base, _globe));
	}
}
