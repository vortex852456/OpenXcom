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
// Ported from: src/Basescape/BuildFacilitiesState.h, src/Basescape/BuildFacilitiesState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window shown with all the facilities
/// available to build.
/// </summary>
public class BuildFacilitiesState : State
{
	protected Base _base;
	protected State _state;
	protected List<RuleBaseFacility> _facilities = [];
	protected List<RuleBaseFacility> _disabledFacilities = [];
	protected int _lstScroll;
	protected TextButton _btnOk;
	protected Window _window;
	protected Text _txtTitle;
	protected TextList _lstFacilities;

	/// <summary>
	/// Initializes all the elements in the Build Facilities window.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="state">Pointer to the base state to refresh.</param>
	public BuildFacilitiesState(Base @base, State state)
	{
		_base = @base;
		_state = state;
		_lstScroll = 0;
		_screen = false;

		// Create objects
		_window = new Window(this, 128, 160, 192, 40, WindowPopup.POPUP_VERTICAL);
		_btnOk = new TextButton(112, 16, 200, 176);
		_lstFacilities = new TextList(104, 104, 200, 64);
		_txtTitle = new Text(118, 17, 197, 48);

		// Set palette
		SetInterface("selectFacility");

		Add(_window, "window", "selectFacility");
		Add(_btnOk, "button", "selectFacility");
		Add(_txtTitle, "text", "selectFacility");
		Add(_lstFacilities, "list", "selectFacility");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "selectFacility");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_INSTALLATION"));

		_lstFacilities.SetColumns(1, 104);
		_lstFacilities.SetSelectable(true);
		_lstFacilities.SetBackground(_window);
		_lstFacilities.Margin = 2;
		_lstFacilities.SetWordWrap(true);
		_lstFacilities.SetScrolling(true, 0);
		_lstFacilities.OnMouseClick(LstFacilitiesClick);
		_lstFacilities.OnMouseClick(LstFacilitiesClick, SDLMouse.SDL_BUTTON_MIDDLE);
	}

	/// <summary>
	/// Populates the build list from the current "available" facilities.
	/// </summary>
	public virtual void PopulateBuildList()
	{
		_facilities.Clear();
		_disabledFacilities.Clear();
		_lstFacilities.ClearList();

		RuleBaseFacilityFunctions providedBaseFunc = _base.GetProvidedBaseFunc(default);
		RuleBaseFacilityFunctions forbiddenBaseFunc = _base.GetForbiddenBaseFunc(default);
		RuleBaseFacilityFunctions futureBaseFunc = _base.GetFutureBaseFunc(default);

		foreach (var facilityType in _game.Mod.BaseFacilitiesList)
		{
			RuleBaseFacility rule = _game.Mod.GetBaseFacility(facilityType);
			if (!rule.IsAllowedForBaseType(_base.IsFakeUnderwater))
			{
				continue;
			}
			if ((rule.IsLift && !rule.IsUpgradeOnly)
				|| !_game.SavedGame.IsResearched(rule.Requirements))
			{
				continue;
			}
			if (_base.IsMaxAllowedLimitReached(rule))
			{
				_disabledFacilities.Add(rule);
				continue;
			}
			RuleBaseFacilityFunctions req = rule.RequireBaseFunc;
			RuleBaseFacilityFunctions forb = rule.ForbiddenBaseFunc;
			RuleBaseFacilityFunctions prov = rule.ProvidedBaseFunc;
			if ((~providedBaseFunc & req).Any())
			{
				_disabledFacilities.Add(rule);
				continue;
			}

			// do not check requirements for bulding that can overbuild others, correct check will be done when you will try to place it somewhere
			if (rule.BuildOverFacilities.Count == 0)
			{
				if ((forbiddenBaseFunc & prov).Any())
				{
					_disabledFacilities.Add(rule);
					continue;
				}
				if ((futureBaseFunc & forb).Any())
				{
					_disabledFacilities.Add(rule);
					continue;
				}
			}
			// but we can still check at least the inherent base funcs (of the base country/region)
			else
			{
				if ((_base.InherentForbiddenBaseFunc & prov).Any())
				{
					_disabledFacilities.Add(rule);
					continue;
				}
				if ((_base.InherentFutureBaseFunc & forb).Any())
				{
					_disabledFacilities.Add(rule);
					continue;
				}
			}
			_facilities.Add(rule);
		}

		int row = 0;
		foreach (var facRule in _facilities)
		{
			_lstFacilities.AddRow(1, Tr(facRule.Type));
			++row;
		}

		if (_disabledFacilities.Count != 0)
		{
			byte disabledColor = _lstFacilities.SecondaryColor;
			foreach (var facRule in _disabledFacilities)
			{
				_lstFacilities.AddRow(1, Tr(facRule.Type));
				_lstFacilities.SetRowColor(row, disabledColor);
				++row;
			}
		}

		if (_lstScroll > 0)
		{
			_lstFacilities.ScrollTo(_lstScroll);
			_lstScroll = 0;
		}
	}

	/// <summary>
	/// The player can change the selected base
	/// or change info on other screens.
	/// </summary>
	public override void Init()
	{
		_state.Init();
		base.Init();

		PopulateBuildList();
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Places the selected facility.
	/// </summary>
	public virtual void LstFacilitiesClick(Action action)
	{
		int index = (int)_lstFacilities.SelectedRow;
		_lstScroll = _lstFacilities.Scroll;

		if (action.Details.button.button == SDLMouse.SDL_BUTTON_MIDDLE)
		{
			string tmp = (index >= _facilities.Count) ? _disabledFacilities[index - _facilities.Count].UfopediaType : _facilities[index].UfopediaType;
			Ufopaedia.OpenArticle(_game, tmp);
			return;
		}

		if (index >= _facilities.Count)
		{
			return;
		}
		_game.PushState(new PlaceFacilityState(_base, _facilities[index]));
	}
}
