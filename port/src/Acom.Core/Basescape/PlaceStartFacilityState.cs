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
// Ported from: src/Basescape/PlaceStartFacilityState.h, src/Basescape/PlaceStartFacilityState.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Window shown when the player tries to
/// build a facility (on a custom starting base).
/// </summary>
public class PlaceStartFacilityState : PlaceFacilityState
{
	private readonly SelectStartFacilityState _select;

	/// <summary>
	/// Initializes all the elements in the Place Facility window.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="select">Pointer to the selection state.</param>
	/// <param name="rule">Pointer to the facility ruleset to build.</param>
	public PlaceStartFacilityState(Base @base, SelectStartFacilityState select, RuleBaseFacility rule) : base(@base, rule)
	{
		_select = select;
		_view.OnMouseClick(ViewClick);
		_numCost.SetText(Tr("STR_NONE"));
		_numTime.SetText(Tr("STR_NONE"));
	}

	/// <summary>
	/// Processes clicking on facilities.
	/// </summary>
	public new void ViewClick(Action action)
	{
		if (_view.GetPlacementError(_rule, null, true) != BasePlacementErrors.BPE_None)
		{
			_game.PopState();
			_game.PushState(new ErrorMessageState(Tr("STR_CANNOT_BUILD_HERE"), _palette, (byte)_game.Mod.GetInterface("basescape").GetElement("errorMessage").color, "BACK01.SCR", _game.Mod.GetInterface("basescape").GetElement("errorPalette").color));
		}
		else
		{
			var fac = new BaseFacility(_rule, _base);
			fac.X = _view.GridX;
			fac.Y = _view.GridY;
			_base.Facilities.Add(fac);
			if (fac.Rules.PlaceSound != Mod.NO_SOUND)
			{
				_game.Mod.GetSound("GEO.CAT", fac.Rules.PlaceSound).Play();
			}
			_game.PopState();
			_select.FacilityBuilt();
		}
	}
}
