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
// Ported from: src/Geoscape/CraftNotEnoughPilotsState.h, src/Geoscape/CraftNotEnoughPilotsState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window used to notify the player when
/// a craft doesn't have enough pilots.
/// </summary>
public class CraftNotEnoughPilotsState : State
{
	private readonly TextButton _btnOk, _btnAssignPilots;
	private readonly Window _window;
	private readonly Text _txtMessage;
	private readonly Craft _craft;

	/// <summary>
	/// Initializes all the elements in a Craft Not Enough Pilots window.
	/// </summary>
	/// <param name="craft">Relevant craft.</param>
	public CraftNotEnoughPilotsState(Craft craft)
	{
		_craft = craft;
		_screen = false;

		// Create objects
		_window = new Window(this, 256, 160, 32, 20, WindowPopup.POPUP_BOTH);
		_btnOk = new TextButton(108, 18, 48, 150);
		_btnAssignPilots = new TextButton(108, 18, 164, 150);
		_txtMessage = new Text(246, 96, 37, 42);

		// Set palette
		SetInterface("craftPilotError");

		Add(_window, "window", "craftPilotError");
		Add(_btnOk, "button", "craftPilotError");
		Add(_btnAssignPilots, "button", "craftPilotError");
		Add(_txtMessage, "text1", "craftPilotError");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "craftPilotError");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_btnAssignPilots.Text = Tr("STR_ASSIGN_PILOTS");
		_btnAssignPilots.OnMouseClick(BtnAssignPilotsClick);
		_btnAssignPilots.OnKeyboardPress(BtnAssignPilotsClick, Options.keyOk);

		if (_craft.MissionComplete || _craft.Status == "STR_OUT")
		{
			_btnAssignPilots.Visible = false;
		}

		_txtMessage.Align = TextHAlign.ALIGN_CENTER;
		_txtMessage.VerticalAlign = TextVAlign.ALIGN_MIDDLE;
		_txtMessage.SetBig();
		_txtMessage.SetWordWrap(true);
		_txtMessage.SetText(Tr("STR_NOT_ENOUGH_PILOTS").Arg(_craft.Rules.Pilots));
	}

	/// <summary>
	/// Closes the window.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Opens CraftInfo window.
	/// </summary>
	public void BtnAssignPilotsClick(Action action)
	{
		Base b = _craft.Base;
		if (b != null)
		{
			for (int i = 0; i < b.Crafts.Count; ++i)
			{
				if (b.Crafts[i] == _craft)
				{
					_game.PopState();
					_game.PushState(new CraftInfoState(_craft.Base, i));
					break;
				}
			}
		}
	}
}
