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
// Ported from: src/Basescape/PlaceLiftState.h, src/Basescape/PlaceLiftState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Screen shown when the player has to
/// place the access lift of a base.
/// </summary>
public class PlaceLiftState : State
{
	private readonly Base _base;
	private readonly Globe _globe;
	private readonly BaseView _view;
	private readonly Text _txtTitle;
	private readonly bool _first;
	private RuleBaseFacility _lift;

	private readonly List<RuleBaseFacility> _accessLifts = [];
	private readonly Window _window;
	private readonly Text _txtHeader;
	private readonly TextList _lstAccessLifts;

	/// <summary>
	/// Initializes all the elements in the Place Lift screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="globe">Pointer to the Geoscape globe.</param>
	/// <param name="first">Is this a custom starting base?</param>
	public PlaceLiftState(Base @base, Globe globe, bool first)
	{
		_base = @base;
		_globe = globe;
		_first = first;

		// Create objects
		_view = new BaseView(192, 192, 0, 8);
		_txtTitle = new Text(320, 9, 0, 0);
		_window = new Window(this, 128, 160, 192, 40, WindowPopup.POPUP_NONE);
		_txtHeader = new Text(118, 17, 197, 48);
		_lstAccessLifts = new TextList(104, 104, 200, 64);

		// Set palette
		SetInterface("placeFacility");

		Add(_view, "baseView", "basescape");
		Add(_txtTitle, "text", "placeFacility");
		Add(_window, "window", "selectFacility");
		Add(_txtHeader, "text", "selectFacility");
		Add(_lstAccessLifts, "list", "selectFacility");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "selectFacility");

		if (_globe != null && _base != null)
		{
			int texture = 0, shade = 0;
			_globe.GetPolygonTextureAndShade(_base.Longitude, _base.Latitude, ref texture, ref shade);
			var globeTexture = _game.Mod.Globe.GetTexture(texture);
			_base.GlobeTexture = globeTexture;
		}

		var itf = _game.Mod.GetInterface("basescape").GetElementOptional("trafficLights");
		if (itf != null)
		{
			_view.SetOtherColors((byte)itf.color, (byte)itf.color2, (byte)itf.border, !itf.TFTDMode);
		}
		_view.SetTexture(_game.Mod.GetSurfaceSet("BASEBITS.PCK"));
		_view.SetBase(_base);

		_lift = null;
		foreach (var facilityType in _game.Mod.BaseFacilitiesList)
		{
			var facilityRule = _game.Mod.GetBaseFacility(facilityType);
			if ((facilityRule.IsLift && !facilityRule.IsUpgradeOnly)
				&& facilityRule.IsAllowedForBaseType(_base.IsFakeUnderwater) && _game.SavedGame.IsResearched(facilityRule.Requirements))
			{
				_accessLifts.Add(facilityRule);
			}
		}
		if (_first || _accessLifts.Count == 1)
		{
			_lift = _accessLifts[0];
		}

		_txtHeader.SetBig();
		_txtHeader.Align = TextHAlign.ALIGN_CENTER;
		_txtHeader.SetText(Tr("STR_INSTALLATION"));

		_lstAccessLifts.SetColumns(1, 104);
		_lstAccessLifts.SetSelectable(true);
		_lstAccessLifts.SetBackground(_window);
		_lstAccessLifts.Margin = 2;
		_lstAccessLifts.SetWordWrap(true);
		_lstAccessLifts.SetScrolling(true, 0);
		_lstAccessLifts.OnMouseClick(LstAccessLiftsClick);
		_lstAccessLifts.OnMouseClick(LstAccessLiftsClick, SDLMouse.SDL_BUTTON_MIDDLE);

		foreach (var facRule in _accessLifts)
		{
			_lstAccessLifts.AddRow(1, Tr(facRule.Type));
		}

		if (_lift != null)
		{
			_lstAccessLifts.Visible = false;
			_txtHeader.Visible = false;
			_window.Visible = false;

			_view.SetSelectable(_lift.SizeX, _lift.SizeY);
			_view.OnMouseClick(ViewClick);
		}

		_txtTitle.SetText(Tr("STR_SELECT_POSITION_FOR_ACCESS_LIFT"));
	}

	/// <summary>
	/// Processes clicking on facilities.
	/// </summary>
	public void ViewClick(Action action)
	{
		var fac = new BaseFacility(_lift, _base);
		fac.X = _view.GridX;
		fac.Y = _view.GridY;
		_base.Facilities.Add(fac);
		if (fac.Rules.PlaceSound != Mod.NO_SOUND)
		{
			_game.Mod.GetSound("GEO.CAT", fac.Rules.PlaceSound).Play();
		}
		_game.PopState();
		var bState = new BasescapeState(_base, _globe);
		_game.SavedGame.SetSelectedBase(_game.SavedGame.Bases.Count - 1);
		_game.PushState(bState);
		if (_first)
		{
			_game.PushState(new SelectStartFacilityState(_base, bState, _globe));
		}
	}

	/// <summary>
	/// Selects the access lift to place.
	/// </summary>
	public void LstAccessLiftsClick(Action action)
	{
		uint index = _lstAccessLifts.SelectedRow;
		if (index >= (uint)_accessLifts.Count)
		{
			return;
		}

		if (action.Details.button.button == SDLMouse.SDL_BUTTON_MIDDLE)
		{
			Ufopaedia.OpenArticle(_game, _accessLifts[(int)index].UfopediaType);
			return;
		}

		_lift = _accessLifts[(int)index];

		if (_lift != null)
		{
			_lstAccessLifts.Visible = false;
			_txtHeader.Visible = false;
			_window.Visible = false;

			_view.SetSelectable(_lift.SizeX, _lift.SizeY);
			_view.OnMouseClick(ViewClick);
		}
	}
}
