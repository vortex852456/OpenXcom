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
// Ported from: src/Menu/OptionsConfirmState.h, src/Menu/OptionsConfirmState.cpp
// Port status: partial (missing: Screen::resetDisplay – the host applies the restored display options)
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>Confirmation window for the new display options, reverting them after a countdown.</summary>
public class OptionsConfirmState : State
{
	private readonly OptionsOrigin _origin;
	private readonly Window _window;
	private readonly TextButton _btnYes, _btnNo;
	private readonly Text _txtTitle, _txtTimer;
	private readonly Timer _timer;
	private int _countdown;

	/// <summary>Initializes all the elements in the Confirm Display Options screen.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	public OptionsConfirmState(OptionsOrigin origin)
	{
		_origin = origin;
		_countdown = 15;
		_screen = false;

		// Create objects
		_window = new Window(this, 216, 100, 52, 50, WindowPopup.POPUP_BOTH);
		_btnYes = new TextButton(50, 20, 70, 120);
		_btnNo = new TextButton(50, 20, 200, 120);
		_txtTitle = new Text(206, 20, 57, 70);
		_txtTimer = new Text(206, 20, 57, 100);
		_timer = new Timer(1000);

		// Set palette
		SetInterface("optionsMenu", false, _game.SavedGame?.SavedBattle);

		Add(_window, "confirmVideo", "optionsMenu");
		Add(_btnYes, "confirmVideo", "optionsMenu");
		Add(_btnNo, "confirmVideo", "optionsMenu");
		Add(_txtTitle, "confirmVideo", "optionsMenu");
		Add(_txtTimer, "confirmVideo", "optionsMenu");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "optionsMenu");

		_btnYes.Text = Tr("STR_YES");
		_btnYes.OnMouseClick(BtnYesClick);

		_btnNo.Text = Tr("STR_NO");
		_btnNo.OnMouseClick(BtnNoClick);
		// no keyboard shortcuts to make sure users can see the message

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetWordWrap(true);
		_txtTitle.SetText(Tr("STR_DISPLAY_OPTIONS_CONFIRM"));

		_txtTimer.Align = TextHAlign.ALIGN_CENTER;
		_txtTimer.SetWordWrap(true);
		_txtTimer.SetText(Tr("STR_DISPLAY_OPTIONS_REVERT").Arg(_countdown));

		if (_origin == OptionsOrigin.OPT_BATTLESCAPE)
		{
			ApplyBattlescapeTheme("optionsMenu");
		}

		_timer.OnTimer((StateHandler)Countdown);
		_timer.Start();
	}

	/// <summary>Runs the countdown timer.</summary>
	public override void Think()
	{
		base.Think();

		_timer.Think(this, null);
	}

	/// <summary>Counts down the timer for reverting display options.</summary>
	public void Countdown()
	{
		_countdown--;
		_txtTimer.SetText(Tr("STR_DISPLAY_OPTIONS_REVERT").Arg(_countdown.ToString("00", CultureInfo.InvariantCulture)));
		if (_countdown == 0)
		{
			BtnNoClick(null);
		}
	}

	/// <summary>Goes back to the Main Menu.</summary>
	public void BtnYesClick(Action action)
	{
		_game.PopState();
		OptionsBaseState.Restart(_origin);
	}

	/// <summary>Restores the original display options.</summary>
	public void BtnNoClick(Action action)
	{
		Options.SwitchDisplay();
		Screen.UpdateScale(Options.battlescapeScale, ref Options.baseXBattlescape, ref Options.baseYBattlescape, _origin == OptionsOrigin.OPT_BATTLESCAPE);
		Screen.UpdateScale(Options.geoscapeScale, ref Options.baseXGeoscape, ref Options.baseYGeoscape, _origin != OptionsOrigin.OPT_BATTLESCAPE);
		Options.Save();
		// missing: _game->getScreen()->resetDisplay(); (host matter)
		_game.PopState();
		OptionsBaseState.Restart(_origin);
	}
}
