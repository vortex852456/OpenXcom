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
// Ported from: src/Geoscape/TargetInfoState.h, src/Geoscape/TargetInfoState.cpp
#nullable disable

using System.Text;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Generic window used to display all the
/// crafts targeting a certain point on the map.
/// </summary>
public class TargetInfoState : State
{
	private readonly Target _target;
	private readonly Globe _globe;

	private readonly TextButton _btnIntercept, _btnOk, _btnInfo;
	private readonly Window _window;
	private readonly TextEdit _edtTitle;
	private readonly Text _txtTargetted, _txtFollowers, _txtPenalty;
	private readonly AlienDeployment _deploymentRule;

	/// <summary>
	/// Initializes all the elements in the Target Info window.
	/// </summary>
	/// <param name="target">Pointer to the target to show info from.</param>
	/// <param name="globe">Pointer to the Geoscape globe.</param>
	public TargetInfoState(Target target, Globe globe)
	{
		_target = target;
		_globe = globe;
		_deploymentRule = null;
		_screen = false;

		// Create objects
		_window = new Window(this, 192, 120, 32, 40, WindowPopup.POPUP_BOTH);
		_btnIntercept = new TextButton(160, 12, 48, 124);
		_btnInfo = new TextButton(77, 12, 48, 140);
		_btnOk = new TextButton(77, 12, 131, 140);
		_edtTitle = new TextEdit(this, 182, 32, 37, 46);
		_txtTargetted = new Text(182, 9, 37, 78);
		_txtFollowers = new Text(182, 40, 37, 88);
		_txtPenalty = new Text(160, 9, 48, 114);

		// Set palette
		SetInterface("targetInfo");

		Add(_window, "window", "targetInfo");
		Add(_btnIntercept, "button", "targetInfo");
		Add(_btnInfo, "button", "targetInfo");
		Add(_btnOk, "button", "targetInfo");
		Add(_edtTitle, "text2", "targetInfo");
		Add(_txtTargetted, "text1", "targetInfo");
		Add(_txtFollowers, "text1", "targetInfo");
		Add(_txtPenalty, "text2", "targetInfo");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "targetInfo");

		_btnIntercept.Text = Tr("STR_INTERCEPT");
		_btnIntercept.OnMouseClick(BtnInterceptClick);

		_btnInfo.Text = Tr("STR_INFO");
		_btnInfo.OnMouseClick(BtnInfoClick);

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_edtTitle.SetBig();
		_edtTitle.SetAlign(TextHAlign.ALIGN_CENTER);
		_edtTitle.SetVerticalAlign(TextVAlign.ALIGN_MIDDLE);
		_edtTitle.SetWordWrap(true);
		_edtTitle.Text = _target.GetName(_game.Language);
		_edtTitle.OnChange(EdtTitleChange);

		_txtTargetted.Align = TextHAlign.ALIGN_CENTER;
		_txtTargetted.SetText(Tr("STR_TARGETTED_BY"));
		_txtFollowers.Align = TextHAlign.ALIGN_CENTER;
		StringBuilder ss = new StringBuilder();
		foreach (var follower in _target.Followers)
		{
			ss.Append(follower.GetName(_game.Language)).Append('\n');
		}
		_txtFollowers.SetText(ss.ToString());

		// info
		if (_target is MissionSite m)
		{
			_deploymentRule = _game.Mod.GetDeployment(m.Deployment.Type);
		}
		else if (_target is AlienBase b)
		{
			AlienRace race = _game.Mod.GetAlienRace(b.AlienRace);
			_deploymentRule = _game.Mod.GetDeployment(race.BaseCustomMission);
			if (_deploymentRule == null) _deploymentRule = _game.Mod.GetDeployment(b.Deployment.Type);
		}
		if (_deploymentRule != null && _deploymentRule.AlertDescription.Length != 0)
		{
			// all OK
		}
		else
		{
			_btnInfo.Visible = false;
			_btnOk.SetWidth(_btnOk.X + _btnOk.Width - _btnInfo.X);
			_btnOk.X = _btnInfo.X;
		}

		if (_deploymentRule != null && _deploymentRule.DespawnPenalty != 0)
		{
			_txtPenalty.Align = TextHAlign.ALIGN_CENTER;
			_txtPenalty.SetText(Tr("STR_DESPAWN_PENALTY").Arg(_deploymentRule.DespawnPenalty));
		}
	}

	/// <summary>
	/// Picks a craft to intercept the UFO.
	/// </summary>
	public void BtnInterceptClick(Action action)
	{
		_game.PushState(new InterceptState(_globe, false, null, _target));
	}

	/// <summary>
	/// Closes the window.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Shows the BriefingLight screen.
	/// </summary>
	public void BtnInfoClick(Action action)
	{
		_game.PushState(new BriefingLightState(_deploymentRule));
	}

	/// <summary>
	/// Changes the target name.
	/// </summary>
	public void EdtTitleChange(Action action)
	{
		if (_edtTitle.Text == _target.GetDefaultName(_game.Language))
		{
			_target.SetName("");
		}
		else
		{
			_target.SetName(_edtTitle.Text);
		}
		if (action.Details.key.keysym.sym == SDLKey.SDLK_RETURN ||
			action.Details.key.keysym.sym == SDLKey.SDLK_KP_ENTER)
		{
			_edtTitle.Text = _target.GetName(_game.Language);
		}
	}
}
