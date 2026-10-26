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
// Ported from: src/Geoscape/AlienBaseState.h, src/Geoscape/AlienBaseState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Displays info on an alien base.
/// </summary>
public class AlienBaseState : State
{
	private readonly GeoscapeState _state;
	private readonly AlienBase _base;
	private readonly TextButton _btnOk;
	private readonly Window _window;
	private readonly Text _txtTitle;

	/// <summary>
	/// Initializes all the elements in the Aliens Base discovered window.
	/// </summary>
	/// <param name="base">Pointer to the alien base to get info from.</param>
	/// <param name="state">Pointer to the Geoscape.</param>
	public AlienBaseState(AlienBase @base, GeoscapeState state)
	{
		_state = state;
		_base = @base;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnOk = new TextButton(50, 12, 135, 180);
		_txtTitle = new Text(308, 60, 6, 60);

		SetInterface("alienBase");

		Add(_window, "window", "alienBase");
		// Note: element ids swapped as in C++
		Add(_btnOk, "text", "alienBase");
		Add(_txtTitle, "button", "alienBase");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "alienBase");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetBig();
		_txtTitle.SetWordWrap(true);

		// Check location of base
		string regionName = "", countryName = "";
		foreach (var country in _game.SavedGame.Countries)
		{
			if (country.Rules.InsideCountry(_base.Longitude, _base.Latitude))
			{
				countryName = Tr(country.Rules.Type);
				break;
			}
		}
		foreach (var region in _game.SavedGame.Regions)
		{
			if (region.Rules.InsideRegion(_base.Longitude, _base.Latitude))
			{
				regionName = Tr(region.Rules.Type);
				break;
			}
		}
		string location;
		if (countryName.Length != 0)
		{
			location = Tr("STR_COUNTRIES_COMMA").Arg(countryName).Arg(regionName);
		}
		else if (regionName.Length != 0)
		{
			location = regionName;
		}
		else
		{
			location = Tr("STR_UNKNOWN");
		}
		_txtTitle.SetText(Tr("STR_XCOM_AGENTS_HAVE_LOCATED_AN_ALIEN_BASE_IN_REGION").Arg(location));
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_state.TimerReset();
		_state.Globe.Center(_base.Longitude, _base.Latitude);
		_game.PopState();
	}
}
