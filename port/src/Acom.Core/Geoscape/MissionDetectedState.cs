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
// Ported from: src/Geoscape/MissionDetectedState.h, src/Geoscape/MissionDetectedState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Displays info on a mission site.
/// </summary>
public class MissionDetectedState : State
{
	private readonly MissionSite _mission;
	private readonly GeoscapeState _state;

	private readonly TextButton _btnIntercept, _btnCenter, _btnCancel;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtCity;

	/// <summary>
	/// Initializes all the elements in the Mission Detected window.
	/// </summary>
	/// <param name="mission">Pointer to the respective Mission Site.</param>
	/// <param name="state">Pointer to the Geoscape.</param>
	public MissionDetectedState(MissionSite mission, GeoscapeState state)
	{
		_mission = mission;
		_state = state;
		_screen = false;

		int soundId = mission.Deployment.AlertSound;
		if (soundId != Mod.NO_SOUND)
		{
			_customSound = _game.Mod.GetSound("GEO.CAT", soundId);
		}

		// Create objects
		_window = new Window(this, 256, 200, 0, 0, WindowPopup.POPUP_BOTH);
		_btnIntercept = new TextButton(200, 16, 28, 130);
		_btnCenter = new TextButton(200, 16, 28, 150);
		_btnCancel = new TextButton(200, 16, 28, 170);
		_txtTitle = new Text(246, 32, 5, 48);
		_txtCity = new Text(246, 17, 5, 80);

		// Set palette
		SetInterface("terrorSite");

		Add(_window, "window", "terrorSite");
		Add(_btnIntercept, "button", "terrorSite");
		Add(_btnCenter, "button", "terrorSite");
		Add(_btnCancel, "button", "terrorSite");
		Add(_txtTitle, "text", "terrorSite");
		Add(_txtCity, "text", "terrorSite");

		CenterAllSurfaces();

		// Set up objects
		_window.SetBackground(_game.Mod.GetSurface(mission.Deployment.AlertBackground));

		_btnIntercept.Text = Tr("STR_INTERCEPT");
		_btnIntercept.OnMouseClick(BtnInterceptClick);

		_btnCenter.Text = Tr("STR_CENTER_ON_SITE_TIME_5_SECONDS");
		_btnCenter.OnMouseClick(BtnCenterClick);

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetWordWrap(true);
		_txtTitle.SetText(Tr(mission.Deployment.AlertMessage));

		_txtCity.SetBig();
		_txtCity.Align = TextHAlign.ALIGN_CENTER;
		_txtCity.SetText(Tr(mission.City));
	}

	/// <summary>
	/// Picks a craft to intercept the mission site.
	/// </summary>
	public void BtnInterceptClick(Action action)
	{
		_state.TimerReset();
		_state.Globe.Center(_mission.Longitude, _mission.Latitude);
		_game.PushState(new InterceptState(_state.Globe, false, null, _mission));
	}

	/// <summary>
	/// Centers on the mission site and returns to the previous screen.
	/// </summary>
	public void BtnCenterClick(Action action)
	{
		_state.TimerReset();
		_state.Globe.Center(_mission.Longitude, _mission.Latitude);
		_game.PopState();
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}
}
