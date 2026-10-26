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
// Ported from: src/Menu/OptionsGeoscapeState.h, src/Menu/OptionsGeoscapeState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>Screen that lets the user configure various Geoscape options.</summary>
public class OptionsGeoscapeState : OptionsBaseState
{
	private readonly Text _txtDragScroll;
	private readonly ComboBox _cbxDragScroll;
	private readonly Text _txtScrollSpeed, _txtDogfightSpeed, _txtClockSpeed;
	private readonly Slider _slrScrollSpeed, _slrDogfightSpeed, _slrClockSpeed;
	private readonly Text _txtGlobeDetails;
	private readonly ToggleTextButton _btnGlobeCountries, _btnGlobeRadars, _btnGlobePaths;
	private readonly Text _txtOptions;
	private readonly ToggleTextButton _btnShowFunds;

	/// <summary>Initializes all the elements in the Geoscape Options screen.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	public OptionsGeoscapeState(OptionsOrigin origin) : base(origin)
	{
		SetCategory(_btnGeoscape);

		// Create objects
		_txtDragScroll = new Text(114, 9, 206, 8);
		_cbxDragScroll = new ComboBox(this, 104, 16, 206, 18);

		_txtScrollSpeed = new Text(114, 9, 94, 8);
		_slrScrollSpeed = new Slider(104, 16, 94, 18);

		_txtDogfightSpeed = new Text(114, 9, 206, 40);
		_slrDogfightSpeed = new Slider(104, 16, 206, 50);

		_txtClockSpeed = new Text(114, 9, 94, 40);
		_slrClockSpeed = new Slider(104, 16, 94, 50);

		_txtGlobeDetails = new Text(114, 9, 94, 82);
		_btnGlobeCountries = new ToggleTextButton(104, 16, 94, 92);
		_btnGlobeRadars = new ToggleTextButton(104, 16, 94, 110);
		_btnGlobePaths = new ToggleTextButton(104, 16, 94, 128);

		_txtOptions = new Text(114, 9, 206, 82);
		_btnShowFunds = new ToggleTextButton(104, 16, 206, 92);

		Add(_txtScrollSpeed, "text", "geoscapeMenu");
		Add(_slrScrollSpeed, "button", "geoscapeMenu");

		Add(_txtDogfightSpeed, "text", "geoscapeMenu");
		Add(_slrDogfightSpeed, "button", "geoscapeMenu");

		Add(_txtClockSpeed, "text", "geoscapeMenu");
		Add(_slrClockSpeed, "button", "geoscapeMenu");

		Add(_txtGlobeDetails, "text", "geoscapeMenu");
		Add(_btnGlobeCountries, "button", "geoscapeMenu");
		Add(_btnGlobeRadars, "button", "geoscapeMenu");
		Add(_btnGlobePaths, "button", "geoscapeMenu");

		Add(_txtOptions, "text", "geoscapeMenu");
		Add(_btnShowFunds, "button", "geoscapeMenu");

		Add(_txtDragScroll, "text", "geoscapeMenu");
		Add(_cbxDragScroll, "button", "geoscapeMenu");

		CenterAllSurfaces();

		// Set up objects
		_txtDragScroll.SetText(Tr("STR_DRAG_SCROLL"));

		var dragScrolls = new List<string>
		{
			Tr("STR_DISABLED"),
			Tr("STR_LEFT_MOUSE_BUTTON"),
			Tr("STR_MIDDLE_MOUSE_BUTTON"),
			Tr("STR_RIGHT_MOUSE_BUTTON"),
		};

		_cbxDragScroll.SetOptions(dragScrolls);
		_cbxDragScroll.Selected = Options.geoDragScrollButton;
		_cbxDragScroll.OnChange(CbxDragScrollChange);
		_cbxDragScroll.Tooltip = "STR_DRAG_SCROLL_DESC";
		_cbxDragScroll.OnMouseIn(TxtTooltipIn);
		_cbxDragScroll.OnMouseOut(TxtTooltipOut);

		_txtScrollSpeed.SetText(Tr("STR_SCROLL_SPEED"));

		_slrScrollSpeed.SetRange(100, 10);
		_slrScrollSpeed.Value = Options.geoScrollSpeed;
		_slrScrollSpeed.Tooltip = "STR_SCROLL_SPEED_GEO_DESC";
		_slrScrollSpeed.OnChange(SlrScrollSpeedChange);
		_slrScrollSpeed.OnMouseIn(TxtTooltipIn);
		_slrScrollSpeed.OnMouseOut(TxtTooltipOut);

		_txtDogfightSpeed.SetText(Tr("STR_DOGFIGHT_SPEED"));

		_slrDogfightSpeed.SetRange(50, 20);
		_slrDogfightSpeed.Value = Options.dogfightSpeed;
		_slrDogfightSpeed.OnChange(SlrDogfightSpeedChange);
		_slrDogfightSpeed.Tooltip = "STR_DOGFIGHT_SPEED_DESC";
		_slrDogfightSpeed.OnMouseIn(TxtTooltipIn);
		_slrDogfightSpeed.OnMouseOut(TxtTooltipOut);

		_txtClockSpeed.SetText(Tr("STR_CLOCK_SPEED"));

		_slrClockSpeed.SetRange(250, 10);
		_slrClockSpeed.Value = Options.geoClockSpeed;
		_slrClockSpeed.Tooltip = "STR_CLOCK_SPEED_DESC";
		_slrClockSpeed.OnChange(SlrClockSpeedChange);
		_slrClockSpeed.OnMouseIn(TxtTooltipIn);
		_slrClockSpeed.OnMouseOut(TxtTooltipOut);

		_txtGlobeDetails.SetText(Tr("STR_GLOBE_DETAILS"));

		_btnGlobeCountries.Text = Tr("STR_GLOBE_COUNTRIES");
		_btnGlobeCountries.Pressed = Options.globeDetail;
		_btnGlobeCountries.OnMouseClick(BtnGlobeCountriesClick);
		_btnGlobeCountries.Tooltip = "STR_GLOBE_COUNTRIES_DESC";
		_btnGlobeCountries.OnMouseIn(TxtTooltipIn);
		_btnGlobeCountries.OnMouseOut(TxtTooltipOut);

		_btnGlobeRadars.Text = Tr("STR_GLOBE_RADARS");
		_btnGlobeRadars.Pressed = Options.globeRadarLines;
		_btnGlobeRadars.OnMouseClick(BtnGlobeRadarsClick);
		_btnGlobeRadars.Tooltip = "STR_GLOBE_RADARS_DESC";
		_btnGlobeRadars.OnMouseIn(TxtTooltipIn);
		_btnGlobeRadars.OnMouseOut(TxtTooltipOut);

		_btnGlobePaths.Text = Tr("STR_GLOBE_FLIGHT_PATHS");
		_btnGlobePaths.Pressed = Options.globeFlightPaths;
		_btnGlobePaths.OnMouseClick(BtnGlobePathsClick);
		_btnGlobePaths.Tooltip = "STR_GLOBE_FLIGHT_PATHS_DESC";
		_btnGlobePaths.OnMouseIn(TxtTooltipIn);
		_btnGlobePaths.OnMouseOut(TxtTooltipOut);

		_txtOptions.SetText(Tr("STR_USER_INTERFACE_OPTIONS"));

		_btnShowFunds.Text = Tr("STR_SHOW_FUNDS");
		_btnShowFunds.Pressed = Options.showFundsOnGeoscape;
		_btnShowFunds.OnMouseClick(BtnShowFundsClick);
		_btnShowFunds.Tooltip = "STR_SHOW_FUNDS_DESC";
		_btnShowFunds.OnMouseIn(TxtTooltipIn);
		_btnShowFunds.OnMouseOut(TxtTooltipOut);
	}

	/// <summary>Changes the Drag Scroll option.</summary>
	public void CbxDragScrollChange(Action action)
	{
		Options.geoDragScrollButton = _cbxDragScroll.Selected;
	}

	/// <summary>Updates the scroll speed.</summary>
	public void SlrScrollSpeedChange(Action action)
	{
		Options.geoScrollSpeed = _slrScrollSpeed.Value;
	}

	/// <summary>Updates the dogfight speed.</summary>
	public void SlrDogfightSpeedChange(Action action)
	{
		Options.dogfightSpeed = _slrDogfightSpeed.Value;
	}

	/// <summary>Updates the clock speed.</summary>
	public void SlrClockSpeedChange(Action action)
	{
		Options.geoClockSpeed = _slrClockSpeed.Value;
	}

	/// <summary>Changes the Globe Country Borders option.</summary>
	public void BtnGlobeCountriesClick(Action action)
	{
		Options.globeDetail = _btnGlobeCountries.Pressed;
	}

	/// <summary>Changes the Globe Radar Ranges option.</summary>
	public void BtnGlobeRadarsClick(Action action)
	{
		Options.globeRadarLines = _btnGlobeRadars.Pressed;
	}

	/// <summary>Changes the Globe Flight Paths option.</summary>
	public void BtnGlobePathsClick(Action action)
	{
		Options.globeFlightPaths = _btnGlobePaths.Pressed;
	}

	/// <summary>Changes the Show Funds option.</summary>
	public void BtnShowFundsClick(Action action)
	{
		Options.showFundsOnGeoscape = _btnShowFunds.Pressed;
	}
}
