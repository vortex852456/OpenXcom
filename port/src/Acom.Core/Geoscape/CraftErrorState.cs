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
// Ported from: src/Geoscape/CraftErrorState.h, src/Geoscape/CraftErrorState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window used to notify the player when
/// an error occurs with a craft procedure.
/// </summary>
public class CraftErrorState : State
{
	private readonly GeoscapeState _state;
	private readonly Target _centerOnTarget;
	private readonly TextButton _btnOk, _btnOk5Secs;
	private readonly Window _window;
	private readonly Text _txtMessage;

	/// <summary>
	/// Initializes all the elements in a Craft Error window.
	/// </summary>
	/// <param name="state">Pointer to the Geoscape state.</param>
	/// <param name="msg">Error message.</param>
	/// <param name="enableHotkeys">Enable keyboard shortcuts.</param>
	/// <param name="centerOnTarget">Target to center the globe on (5 secs button).</param>
	public CraftErrorState(GeoscapeState state, string msg, bool enableHotkeys = true, Target centerOnTarget = null)
	{
		_state = state;
		_centerOnTarget = centerOnTarget;
		_screen = false;

		// Create objects
		_window = new Window(this, 256, 160, 32, 20, WindowPopup.POPUP_BOTH);
		_btnOk = new TextButton(100, 18, _state != null ? 48 : 110, 150);
		_btnOk5Secs = new TextButton(100, 18, 172, 150);
		_txtMessage = new Text(246, 96, 37, 42);

		// Set palette
		SetInterface("craftError");

		Add(_window, "window", "craftError");
		Add(_btnOk, "button", "craftError");
		Add(_btnOk5Secs, "button", "craftError");
		Add(_txtMessage, "text1", "craftError");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "craftError");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		if (enableHotkeys)
		{
			_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
			if (_state == null)
			{
				_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
			}
		}

		_btnOk5Secs.Text = Tr("STR_OK_5_SECONDS");
		_btnOk5Secs.OnMouseClick(BtnOk5SecsClick);
		if (enableHotkeys && _state != null)
		{
			_btnOk5Secs.OnKeyboardPress(BtnOk5SecsClick, Options.keyOk);
		}
		_btnOk5Secs.Visible = _state != null;

		_txtMessage.Align = TextHAlign.ALIGN_CENTER;
		_txtMessage.VerticalAlign = TextVAlign.ALIGN_MIDDLE;
		_txtMessage.SetBig();
		_txtMessage.SetWordWrap(true);
		_txtMessage.SetText(msg);
	}

	/// <summary>
	/// Closes the window.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Closes the window and resets the timer to 5 Secs.
	/// </summary>
	public void BtnOk5SecsClick(Action action)
	{
		_state.TimerReset();
		if (_centerOnTarget != null)
		{
			_state.Globe.Center(_centerOnTarget.Longitude, _centerOnTarget.Latitude);
		}
		_game.PopState();
	}
}
