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
// Ported from: src/Geoscape/CraftPatrolState.h, src/Geoscape/CraftPatrolState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window displayed when a craft
/// starts patrolling a waypoint.
/// </summary>
public class CraftPatrolState : State
{
	private readonly Craft _craft;
	private readonly Globe _globe;

	private readonly TextButton _btnOk, _btnRedirect;
	private readonly Window _window;
	private readonly Text _txtDestination, _txtPatrolling;

	/// <summary>
	/// Initializes all the elements in the Craft Patrol window.
	/// </summary>
	/// <param name="craft">Pointer to the craft to display.</param>
	/// <param name="globe">Pointer to the Geoscape globe.</param>
	public CraftPatrolState(Craft craft, Globe globe)
	{
		_craft = craft;
		_globe = globe;
		_screen = false;

		// Create objects
		_window = new Window(this, 224, 168, 16, 16, WindowPopup.POPUP_BOTH);
		_btnOk = new TextButton(140, 12, 58, 144);
		_btnRedirect = new TextButton(140, 12, 58, 160);
		_txtDestination = new Text(224, 64, 16, 48);
		_txtPatrolling = new Text(224, 17, 16, 120);

		// Set palette
		SetInterface("craftPatrol");

		Add(_window, "window", "craftPatrol");
		Add(_btnOk, "button", "craftPatrol");
		Add(_btnRedirect, "button", "craftPatrol");
		Add(_txtDestination, "text1", "craftPatrol");
		Add(_txtPatrolling, "text1", "craftPatrol");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "craftPatrol");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_btnRedirect.Text = Tr("STR_REDIRECT_CRAFT");
		_btnRedirect.OnMouseClick(BtnRedirectClick);
		_btnRedirect.OnKeyboardPress(BtnRedirectClick, Options.keyOk);

		_txtDestination.SetBig();
		_txtDestination.Align = TextHAlign.ALIGN_CENTER;
		_txtDestination.SetWordWrap(true);
		_txtDestination.SetText(Tr("STR_CRAFT_HAS_REACHED_DESTINATION")
								 .Arg(_craft.GetName(_game.Language))
								 .Arg(_craft.Destination.GetName(_game.Language)));

		_txtPatrolling.SetBig();
		_txtPatrolling.Align = TextHAlign.ALIGN_CENTER;
		_txtPatrolling.SetText(Tr("STR_NOW_PATROLLING"));
	}

	/// <summary>
	/// Closes the window.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Opens up the Craft window.
	/// </summary>
	public void BtnRedirectClick(Action action)
	{
		_game.PopState();
		_game.PushState(new GeoscapeCraftState(_craft, _globe, null, false));
	}
}
