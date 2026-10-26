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
// Ported from: src/Basescape/SelectStartFacilityState.h, src/Basescape/SelectStartFacilityState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window shown with all the facilities
/// available to build.
/// </summary>
public class SelectStartFacilityState : BuildFacilitiesState
{
	private readonly Globe _globe;

	/// <summary>
	/// Initializes all the elements in the Build Facilities window.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="state">Pointer to the base state to refresh.</param>
	/// <param name="globe">Pointer to the globe to refresh.</param>
	public SelectStartFacilityState(Base @base, State state, Globe globe) : base(@base, state)
	{
		_globe = globe;
		_facilities = _game.Mod.GetCustomBaseFacilities(_game.SavedGame.Difficulty);

		_btnOk.Text = Tr("STR_RESET");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(null, Options.keyCancel);

		_lstFacilities.OnMouseClick(LstFacilitiesClick);
	}

	/// <summary>
	/// Populates the build list from the current "available" facilities.
	/// </summary>
	public override void PopulateBuildList()
	{
		_lstFacilities.ClearList();
		foreach (var rule in _facilities)
		{
			_lstFacilities.AddRow(1, Tr(rule.Type));
		}
	}

	/// <summary>
	/// Resets the base building.
	/// </summary>
	public new void BtnOkClick(Action action)
	{
		_base.Facilities.Clear();
		_game.PopState();
		_game.PopState();
		_game.PushState(new PlaceLiftState(_base, _globe, true));
	}

	/// <summary>
	/// Places the selected facility.
	/// </summary>
	public override void LstFacilitiesClick(Action action)
	{
		_game.PushState(new PlaceStartFacilityState(_base, this, _facilities[(int)_lstFacilities.SelectedRow]));
	}

	/// <summary>
	/// Callback from PlaceStartFacilityState.
	/// Removes placed facility from the list.
	/// </summary>
	public void FacilityBuilt()
	{
		_facilities.RemoveAt((int)_lstFacilities.SelectedRow);
		if (_facilities.Count == 0)
		{
			_game.PopState();
			_game.PopState(); // return to geoscape, force timer to start.
		}
		else
		{
			PopulateBuildList();
		}
	}
}
