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
// Ported from: src/Menu/SetWindowedRootState.h, src/Menu/SetWindowedRootState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>Asks the user for the new position of the windowed-mode window.</summary>
public class SetWindowedRootState : State
{
	private readonly OptionsOrigin _origin;
	private readonly OptionsVideoState _optionsVideoState;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtWindowedModePositionX, _txtWindowedModePositionY;
	private readonly TextButton _btnOk, _btnCancel;
	private readonly TextEdit _edtWindowedModePositionX, _edtWindowedModePositionY;

	/// <summary>Initializes all the elements in the Asks user new coordinates screen.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	/// <param name="optionsVideoState">Options screen that originated this state.</param>
	public SetWindowedRootState(OptionsOrigin origin, OptionsVideoState optionsVideoState)
	{
		_origin = origin;
		_optionsVideoState = optionsVideoState;
		_screen = false;

		// Create object
		_window = new Window(this, 216, 100, 52, 50, WindowPopup.POPUP_BOTH);
		_txtTitle = new Text(206, 20, 57, 70);
		_txtWindowedModePositionX = new Text(160, 10, 25, 90);
		_txtWindowedModePositionY = new Text(160, 10, 25, 100);
		_btnOk = new TextButton(50, 20, 70, 120);
		_btnCancel = new TextButton(50, 20, 200, 120);
		_edtWindowedModePositionX = new TextEdit(this, 40, 10, 190, 90);
		_edtWindowedModePositionY = new TextEdit(this, 40, 10, 190, 100);

		// Set palette
		SetInterface("optionsMenu", false, _game.SavedGame?.SavedBattle);

		Add(_window, "confirmVideo", "optionsMenu");
		Add(_btnOk, "confirmVideo", "optionsMenu");
		Add(_btnCancel, "confirmVideo", "optionsMenu");
		Add(_txtTitle, "confirmVideo", "optionsMenu");
		Add(_txtWindowedModePositionX, "confirmVideo", "optionsMenu");
		Add(_txtWindowedModePositionY, "confirmVideo", "optionsMenu");
		Add(_edtWindowedModePositionX, "confirmVideo", "optionsMenu");
		Add(_edtWindowedModePositionY, "confirmVideo", "optionsMenu");

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetWordWrap(true);
		_txtTitle.SetText(Tr("STR_DISPLAY_SET_WINDOW_POSITION"));

		_txtWindowedModePositionX.Align = TextHAlign.ALIGN_RIGHT;
		_txtWindowedModePositionX.SetWordWrap(true);
		_txtWindowedModePositionX.SetText(Tr("STR_DISPLAY_WINDOW_POSITION_NEW_X"));

		_txtWindowedModePositionY.Align = TextHAlign.ALIGN_RIGHT;
		_txtWindowedModePositionY.SetWordWrap(true);
		_txtWindowedModePositionY.SetText(Tr("STR_DISPLAY_WINDOW_POSITION_NEW_Y"));

		_edtWindowedModePositionX.Text = Options.newWindowedModePositionX.ToString(CultureInfo.InvariantCulture);
		_edtWindowedModePositionX.SetConstraint(TextEditConstraint.TEC_NUMERIC);

		_edtWindowedModePositionY.Text = Options.newWindowedModePositionY.ToString(CultureInfo.InvariantCulture);
		_edtWindowedModePositionY.SetConstraint(TextEditConstraint.TEC_NUMERIC);

		CenterAllSurfaces();

		// Set up object
		SetWindowBackground(_window, "optionsMenu");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);

		_btnCancel.Text = Tr("STR_CANCEL");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		if (_origin == OptionsOrigin.OPT_BATTLESCAPE)
		{
			ApplyBattlescapeTheme("optionsMenu");
		}
	}

	/// <summary>Roots borderless window.</summary>
	public void BtnOkClick(Action action)
	{
		int newWindowedModePositionX = OptionsVideoState.ParseLeadingInt(_edtWindowedModePositionX.Text);
		int newWindowedModePositionY = OptionsVideoState.ParseLeadingInt(_edtWindowedModePositionY.Text);

		Options.newRootWindowedMode = true;
		Options.newWindowedModePositionX = newWindowedModePositionX;
		Options.newWindowedModePositionY = newWindowedModePositionY;

		_game.PopState();
	}

	/// <summary>Cancels borderless window rooting.</summary>
	public void BtnCancelClick(Action action)
	{
		_optionsVideoState.UnpressRootWindowedMode();

		_game.PopState();
	}
}
