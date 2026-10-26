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
// Ported from: src/Geoscape/UfoDetectedState.h, src/Geoscape/UfoDetectedState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Displays info on a detected UFO.
/// </summary>
public class UfoDetectedState : State
{
	private readonly Ufo _ufo;
	private readonly GeoscapeState _state;

	private readonly TextButton _btnIntercept, _btnCentre, _btnCancel;
	private readonly Window _window;
	private readonly Text _txtUfo, _txtDetected, _txtHyperwave;
	private readonly TextList _lstInfo, _lstInfo2;

	/// <summary>
	/// Initializes all the elements in the Ufo Detected window.
	/// </summary>
	/// <param name="ufo">Pointer to the UFO to get info from.</param>
	/// <param name="state">Pointer to the Geoscape.</param>
	/// <param name="detected">Was the UFO detected?</param>
	/// <param name="hyperwave">Was it a hyperwave radar?</param>
	public UfoDetectedState(Ufo ufo, GeoscapeState state, bool detected, bool hyperwave)
	{
		_ufo = ufo;
		_state = state;

		// Generate UFO ID
		if (_ufo.Id == 0)
		{
			_ufo.Id = _game.SavedGame.GetId("STR_UFO");
			int soundId = _ufo.Rules.AlertSound;
			if (soundId != Mod.NO_SOUND)
			{
				_customSound = _game.Mod.GetSound("GEO.CAT", soundId);
			}
		}
		if (_ufo.Altitude == "STR_GROUND" && _ufo.LandId == 0)
		{
			_ufo.LandId = _game.SavedGame.GetId("STR_LANDING_SITE");
		}

		_screen = false;

		// Create objects
		if (hyperwave)
		{
			_window = new Window(this, 224, 180, 16, 10, WindowPopup.POPUP_BOTH);
		}
		else
		{
			_window = new Window(this, 224, 128, 16, 44, WindowPopup.POPUP_BOTH);
		}
		_btnIntercept = new TextButton(200, 12, 28, 118);
		_btnCentre = new TextButton(200, 12, 28, 134);
		_btnCancel = new TextButton(200, 12, 28, 150);
		_txtUfo = new Text(207, 17, 28, 53);
		_txtDetected = new Text(100, 9, 28, 69);
		_txtHyperwave = new Text(214, 17, 21, 44);
		_lstInfo = new TextList(217, 32, 28, 80);
		_lstInfo2 = new TextList(217, 32, 28, 96);

		if (hyperwave)
		{
			_btnIntercept.Y = 136;
			_btnCentre.Y = 152;
			_btnCancel.Y = 168;
			_txtUfo.Y = 20;
			_txtDetected.Y = 36;
			_lstInfo.Y = 60;
		}
		else
		{
			_txtHyperwave.Visible = false;
			_lstInfo2.Visible = false;
		}

		// Set palette
		SetInterface("UFOInfo", hyperwave);

		Add(_window, "window", "UFOInfo");
		Add(_btnIntercept, "button", "UFOInfo");
		Add(_btnCentre, "button", "UFOInfo");
		Add(_btnCancel, "button", "UFOInfo");
		Add(_txtUfo, "text", "UFOInfo");
		Add(_txtDetected, "text", "UFOInfo");
		Add(_txtHyperwave, "text", "UFOInfo");
		Add(_lstInfo, "text", "UFOInfo");
		Add(_lstInfo2, "text", "UFOInfo");

		// Set up objects
		SetWindowBackground(_window, "UFOInfo");

		CenterAllSurfaces();

		_btnIntercept.Text = Tr("STR_INTERCEPT");
		_btnIntercept.OnMouseClick(BtnInterceptClick);

		_btnCentre.Text = Tr("STR_CENTER_ON_UFO_TIME_5_SECONDS");
		_btnCentre.OnMouseClick(BtnCentreClick);

		{
			// Option ON: click=ignore, Ctrl=cancel. Option OFF (default): click=cancel, Ctrl=ignore.
			bool willIgnore = Options.oxceIgnoreNewUFOs != _game.IsCtrlPressed();
			_btnCancel.Text = Tr(willIgnore ? "STR_IGNORE_UC" : "STR_CANCEL_UC");
		}
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);
		_btnCancel.OnKeyboardPress(ToggleCancel, SDLKey.SDLK_LCTRL);
		_btnCancel.OnKeyboardRelease(ToggleCancel, SDLKey.SDLK_LCTRL);
		_btnCancel.OnKeyboardPress(ToggleCancel, SDLKey.SDLK_RCTRL);
		_btnCancel.OnKeyboardRelease(ToggleCancel, SDLKey.SDLK_RCTRL);

		if (detected)
		{
			_txtDetected.SetText(Tr("STR_DETECTED"));
		}
		else
		{
			_txtDetected.SetText("");
		}

		_txtHyperwave.Align = TextHAlign.ALIGN_CENTER;
		_txtHyperwave.SetWordWrap(true);
		_txtHyperwave.SetText(Tr("STR_HYPER_WAVE_TRANSMISSIONS_ARE_DECODED"));

		_txtUfo.SetBig();
		_txtUfo.SetText(_ufo.GetName(_game.Language));

		_lstInfo.SetColumns(2, 77, 140);
		_lstInfo.SetDot(true);

		string ss = Unicode.TOK_COLOR_FLIP + (string)Tr(_ufo.Rules.Size);
		_lstInfo.AddRow(2, Tr("STR_SIZE_UC"), ss);

		string altitude = _ufo.Altitude == "STR_GROUND" ? "STR_GROUNDED" : _ufo.Altitude;
		// Let's assume if there's any underwater craft, the UFO are underwater too
		bool underwater = false;
		foreach (var craftType in _game.Mod.CraftsList)
		{
			if (underwater)
			{
				break; // loop finished
			}
			underwater = _game.Mod.GetCraft(craftType).IsWaterOnly;
		}
		if (underwater && !_state.Globe.InsideLand(_ufo.Longitude, _ufo.Latitude))
		{
			altitude = "STR_AIRBORNE";
		}
		ss = Unicode.TOK_COLOR_FLIP + (string)Tr(altitude);
		_lstInfo.AddRow(2, Tr("STR_ALTITUDE"), ss);

		string heading = _ufo.Direction;
		if (_ufo.Status != Ufo.UfoStatus.FLYING)
		{
			heading = "STR_NONE_UC";
		}
		ss = Unicode.TOK_COLOR_FLIP + (string)Tr(heading);
		_lstInfo.AddRow(2, Tr("STR_HEADING"), ss);

		ss = Unicode.TOK_COLOR_FLIP + Unicode.FormatNumber(_ufo.Speed);
		_lstInfo.AddRow(2, Tr("STR_SPEED"), ss);

		_lstInfo2.SetColumns(2, 77, 140);
		_lstInfo2.SetDot(true);

		ss = Unicode.TOK_COLOR_FLIP + (string)Tr(_ufo.Rules.Type);
		_lstInfo2.AddRow(2, Tr("STR_CRAFT_TYPE"), ss);

		ss = Unicode.TOK_COLOR_FLIP + (string)Tr(_ufo.AlienRace);
		_lstInfo2.AddRow(2, Tr("STR_RACE"), ss);

		ss = Unicode.TOK_COLOR_FLIP + (string)Tr(_ufo.MissionType);
		_lstInfo2.AddRow(2, Tr("STR_MISSION"), ss);

		ss = Unicode.TOK_COLOR_FLIP + (string)Tr(_ufo.Mission.Region);
		_lstInfo2.AddRow(2, Tr("STR_ZONE"), ss);
	}

	/// <summary>
	/// Picks a craft to intercept the UFO.
	/// </summary>
	public void BtnInterceptClick(Action action)
	{
		_state.TimerReset();
		_state.Globe.Center(_ufo.Longitude, _ufo.Latitude);
		_game.PushState(new InterceptState(_state.Globe, false, null, _ufo));
	}

	/// <summary>
	/// Centers on the UFO and returns to the previous screen.
	/// </summary>
	public void BtnCentreClick(Action action)
	{
		_state.TimerReset();
		_state.Globe.Center(_ufo.Longitude, _ufo.Latitude);
		_game.PopState();
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		bool willIgnore = Options.oxceIgnoreNewUFOs != _game.IsCtrlPressed();
		if (willIgnore)
		{
			// don't show UFO Detected window for this UFO anymore
			_game.SavedGame.AddUfoToIgnoreList(_ufo.Id);
		}
		_game.PopState();
	}

	/// <summary>
	/// Toggles Cancel button label when Ctrl is pressed/released.
	/// </summary>
	public void ToggleCancel(Action action)
	{
		bool willIgnore = Options.oxceIgnoreNewUFOs != _game.IsCtrlPressed();
		_btnCancel.Text = Tr(willIgnore ? "STR_IGNORE_UC" : "STR_CANCEL_UC");
	}
}
