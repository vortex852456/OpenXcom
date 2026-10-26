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
// Ported from: src/Menu/OptionsControlsState.h, src/Menu/OptionsControlsState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>Controls screen which allows the user to customize the various key shortcuts in the game.</summary>
public class OptionsControlsState : OptionsBaseState
{
	private readonly TextButton _btnOXC, _btnOXCE, _btnOTHER;
	private readonly System.Runtime.CompilerServices.StrongBox<TextButton> _owner;
	private readonly TextList _lstControls;
	private readonly OptionCategories _controls;
	private int _offsetGeneralMin = -1;
	private int _offsetGeneralMax = -1;
	private int _offsetGeoMin = -1;
	private int _offsetGeoMax = -1;
	private int _offsetBaseMin = -1;
	private int _offsetBaseMax = -1;
	private int _offsetBattleMin = -1;
	private int _offsetBattleMax = -1;

	private int _selected;
	private OptionInfo _selKey;
	private readonly byte _colorGroup, _colorSel, _colorNormal;

	/// <summary>Initializes all the elements in the Controls screen.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	public OptionsControlsState(OptionsOrigin origin) : base(origin)
	{
		_selected = -1;
		_selKey = null;

		SetCategory(_btnControls);

		// Create objects
		_btnOXC = new TextButton(70, 16, 94, 8);
		_btnOXCE = new TextButton(70, 16, 168, 8);
		_btnOTHER = new TextButton(70, 16, 242, 8);
		_lstControls = new TextList(200, 120, 94, 26);

		_owner = new(_btnOXC);

		Add(_btnOXC, "button", "controlsMenu");
		Add(_btnOXCE, "button", "controlsMenu");
		Add(_btnOTHER, "button", "controlsMenu");

		if (origin != OptionsOrigin.OPT_BATTLESCAPE)
		{
			Add(_lstControls, "optionLists", "controlsMenu");
		}
		else
		{
			Add(_lstControls, "optionLists", "battlescape");
		}

		CenterAllSurfaces();

		_btnOXC.Text = Tr("STR_ENGINE_OXC");
		_btnOXC.SetGroup(_owner);
		_btnOXC.OnMousePress(BtnGroupPress, SDLMouse.SDL_BUTTON_LEFT);

		_btnOXCE.Text = Tr("STR_ENGINE_OXCE");
		_btnOXCE.SetGroup(_owner);
		_btnOXCE.OnMousePress(BtnGroupPress, SDLMouse.SDL_BUTTON_LEFT);

		_btnOTHER.Text = Tr("STR_ENGINE_OTHER"); // rename in your fork
		_btnOTHER.SetGroup(_owner);
		_btnOTHER.OnMousePress(BtnGroupPress, SDLMouse.SDL_BUTTON_LEFT);
		_btnOTHER.Visible = false; // enable in your fork

		// Set up objects
		_lstControls.SetColumns(2, 152, 48);
		_lstControls.SetWordWrap(true);
		_lstControls.SetSelectable(true);
		_lstControls.SetBackground(_window);
		_lstControls.OnMouseClick(LstControlsClick, 0);
		_lstControls.OnKeyboardPress(LstControlsKeyPress);
		_lstControls.SetFocus(true);
		_lstControls.Tooltip = "STR_CONTROLS_DESC";
		_lstControls.OnMouseIn(TxtTooltipIn);
		_lstControls.OnMouseOut(TxtTooltipOut);

		_colorGroup = _lstControls.SecondaryColor;
		_colorSel = (byte)_lstControls.ScrollbarColor;
		_colorNormal = _lstControls.Color;

		_controls = OptionCategories.Build(Options.GetOptionInfo(), true);
	}

	/// <summary>Refreshes the UI.</summary>
	public override void Init()
	{
		base.Init();

		UpdateList();
	}

	private OptionOwner CurrentOwner => _owner.Value == _btnOXC ? OptionOwner.OPTION_OXC : _owner.Value == _btnOXCE ? OptionOwner.OPTION_OXCE : OptionOwner.OPTION_OTHER;

	/// <summary>Fills the controls list based on category.</summary>
	public void UpdateList()
	{
		int idx = (int)CurrentOwner;

		_offsetGeneralMin = -1;
		_offsetGeneralMax = -1;
		_offsetGeoMin = -1;
		_offsetGeoMax = -1;
		_offsetBaseMin = -1;
		_offsetBaseMax = -1;
		_offsetBattleMin = -1;
		_offsetBattleMax = -1;

		_lstControls.ClearList();

		int row = -1;

		if (_controls.General[idx].Count > 0)
		{
			_lstControls.AddRow(2, Tr("STR_GENERAL"), "");
			row++;
			_offsetGeneralMin = row;
			_lstControls.SetCellColor(_offsetGeneralMin, 0, _colorGroup);
			AddControls(_controls.General[idx]);
			row += _controls.General[idx].Count;
			_offsetGeneralMax = row;
		}
		if (_controls.Geo[idx].Count > 0)
		{
			if (row > -1) { _lstControls.AddRow(2, "", ""); row++; }
			_lstControls.AddRow(2, Tr("STR_GEOSCAPE"), "");
			row++;
			_offsetGeoMin = row;
			_lstControls.SetCellColor(_offsetGeoMin, 0, _colorGroup);
			AddControls(_controls.Geo[idx]);
			row += _controls.Geo[idx].Count;
			_offsetGeoMax = row;
		}
		if (_controls.Base[idx].Count > 0)
		{
			if (row > -1) { _lstControls.AddRow(2, "", ""); row++; }
			_lstControls.AddRow(2, Tr("STR_BASESCAPE"), "");
			row++;
			_offsetBaseMin = row;
			_lstControls.SetCellColor(_offsetBaseMin, 0, _colorGroup);
			AddControls(_controls.Base[idx]);
			row += _controls.Base[idx].Count;
			_offsetBaseMax = row;
		}
		if (_controls.Battle[idx].Count > 0)
		{
			if (row > -1) { _lstControls.AddRow(2, "", ""); row++; }
			_lstControls.AddRow(2, Tr("STR_BATTLESCAPE"), "");
			row++;
			_offsetBattleMin = row;
			_lstControls.SetCellColor(_offsetBattleMin, 0, _colorGroup);
			AddControls(_controls.Battle[idx]);
			row += _controls.Battle[idx].Count;
			_offsetBattleMax = row;
		}
	}

	/// <summary>Uppercases the first letter of every word (after a space) in a string.</summary>
	/// <param name="str">Source string.</param>
	/// <returns>Destination string.</returns>
	internal static string UcWords(string str)
	{
		var s = str.ToCharArray();
		if (s.Length != 0)
		{
			s[0] = OptionsVideoState.ToUpper(s[0]);
		}
		for (int i = Array.IndexOf(s, ' '); i != -1; i = Array.IndexOf(s, ' ', i + 1))
		{
			if (s.Length > i + 1)
				s[i + 1] = OptionsVideoState.ToUpper(s[i + 1]);
			else
				break;
		}
		return new string(s);
	}

	/// <summary>Adds a bunch of controls to the list.</summary>
	/// <param name="keys">List of controls.</param>
	private void AddControls(List<OptionInfo> keys)
	{
		foreach (var optionInfo in keys)
		{
			string name = Tr(optionInfo.Description);
			SDLKey key = optionInfo.AsKey;
			string keyName = UcWords(SDLKeyNames.SDL_GetKeyName(key));
			if (key == SDLKey.SDLK_UNKNOWN)
				keyName = "";
			_lstControls.AddRow(2, name, keyName);
		}
	}

	/// <summary>Gets the currently selected control.</summary>
	/// <param name="sel">Selected row.</param>
	/// <returns>Option, null if none selected.</returns>
	private OptionInfo GetControl(uint sel)
	{
		int selInt = (int)sel;
		int idx = (int)CurrentOwner;

		if (selInt > _offsetGeneralMin && selInt <= _offsetGeneralMax)
		{
			return _controls.General[idx][selInt - 1 - _offsetGeneralMin];
		}
		else if (selInt > _offsetGeoMin && selInt <= _offsetGeoMax)
		{
			return _controls.Geo[idx][selInt - 1 - _offsetGeoMin];
		}
		else if (selInt > _offsetBaseMin && selInt <= _offsetBaseMax)
		{
			return _controls.Base[idx][selInt - 1 - _offsetBaseMin];
		}
		else if (selInt > _offsetBattleMin && selInt <= _offsetBattleMax)
		{
			return _controls.Battle[idx][selInt - 1 - _offsetBattleMin];
		}
		else
		{
			return null;
		}
	}

	/// <summary>Select a control for changing.</summary>
	public void LstControlsClick(Action action)
	{
		if (action.Details.button.button != SDLMouse.SDL_BUTTON_LEFT && action.Details.button.button != SDLMouse.SDL_BUTTON_RIGHT)
		{
			return;
		}
		if (_selected != -1)
		{
			uint selected = (uint)_selected;
			_lstControls.SetCellColor(_selected, 0, _colorNormal);
			_lstControls.SetCellColor(_selected, 1, _colorNormal);
			_selected = -1;
			_selKey = null;
			if (selected == _lstControls.SelectedRow)
				return;
		}
		_selected = (int)_lstControls.SelectedRow;
		_selKey = GetControl((uint)_selected);
		if (_selKey == null)
		{
			_selected = -1;
			return;
		}

		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
		{
			_lstControls.SetCellColor(_selected, 0, _colorSel);
			_lstControls.SetCellColor(_selected, 1, _colorSel);
		}
		else if (action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT)
		{
			_lstControls.SetCellText(_selected, 1, "");
			_selKey.AsKey = SDLKey.SDLK_UNKNOWN;
			_selected = -1;
			_selKey = null;
		}
	}

	/// <summary>Change selected control.</summary>
	public void LstControlsKeyPress(Action action)
	{
		if (_selected != -1)
		{
			SDLKey key = action.Details.key.keysym.sym;
			if (key != 0 &&
				key != SDLKey.SDLK_LSHIFT && key != SDLKey.SDLK_LALT && key != SDLKey.SDLK_LCTRL &&
				key != SDLKey.SDLK_RSHIFT && key != SDLKey.SDLK_RALT && key != SDLKey.SDLK_RCTRL)
			{
				_selKey.AsKey = key;
				string name = UcWords(SDLKeyNames.SDL_GetKeyName(_selKey.AsKey));
				_lstControls.SetCellText(_selected, 1, name);
			}
			_lstControls.SetCellColor(_selected, 0, _colorNormal);
			_lstControls.SetCellColor(_selected, 1, _colorNormal);
			_selected = -1;
			_selKey = null;
		}
	}

	/// <summary>Handler for clicking buttons.</summary>
	public new void BtnGroupPress(Action action)
	{
		UpdateList();
	}
}
