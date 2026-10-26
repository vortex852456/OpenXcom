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
// Ported from: src/Geoscape/LowFuelState.h, src/Geoscape/LowFuelState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window displayed when a craft
/// starts running out of fuel.
/// </summary>
public class LowFuelState : State
{
	private readonly Craft _craft;
	private readonly GeoscapeState _state;

	private readonly TextButton _btnOk, _btnOk5Secs;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtMessage;

	/// <summary>
	/// Initializes all the elements in the Low Fuel window.
	/// </summary>
	/// <param name="craft">Pointer to the craft to display.</param>
	/// <param name="state">Pointer to the Geoscape.</param>
	public LowFuelState(Craft craft, GeoscapeState state)
	{
		_craft = craft;
		_state = state;
		_screen = false;

		// Create objects
		_window = new Window(this, 224, 120, 16, 40, WindowPopup.POPUP_BOTH);
		_btnOk = new TextButton(90, 18, 30, 120);
		_btnOk5Secs = new TextButton(90, 18, 136, 120);
		_txtTitle = new Text(214, 17, 21, 60);
		_txtMessage = new Text(214, 17, 21, 90);

		// Set palette
		SetInterface("lowFuel");

		Add(_window, "window", "lowFuel");
		Add(_btnOk, "button", "lowFuel");
		Add(_btnOk5Secs, "button", "lowFuel");
		Add(_txtTitle, "text", "lowFuel");
		Add(_txtMessage, "text", "lowFuel");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "lowFuel");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_btnOk5Secs.Text = Tr("STR_OK_5_SECONDS");
		_btnOk5Secs.OnMouseClick(BtnOk5SecsClick);
		_btnOk5Secs.OnKeyboardPress(BtnOk5SecsClick, Options.keyOk);

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetBig();
		_txtTitle.SetText(_craft.GetName(_game.Language));

		_txtMessage.Align = TextHAlign.ALIGN_CENTER;
		_txtMessage.SetText(Tr("STR_IS_LOW_ON_FUEL_RETURNING_TO_BASE"));
	}

	/// <summary>
	/// Closes the window.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Closes the window and sets the timer to 5 Secs.
	/// </summary>
	public void BtnOk5SecsClick(Action action)
	{
		_state.TimerReset();
		_game.PopState();
	}
}
