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
// Ported from: src/Menu/OptionsAdvancedState.h, src/Menu/OptionsAdvancedState.cpp
// Deviation: C++ identifies options by the address of their variable (i == &Options::FPS); the port compares the option id, which is the variable name.
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>The settings of the Advanced/Controls screens, by category and owner (C++ std::vector&lt;OptionInfo&gt; x[OPTION_OWNER_MAX]).</summary>
internal sealed class OptionCategories
{
	public readonly List<OptionInfo>[] General = NewGroup();
	public readonly List<OptionInfo>[] Geo = NewGroup();
	public readonly List<OptionInfo>[] Base = NewGroup();
	public readonly List<OptionInfo>[] Battle = NewGroup();
	public readonly List<OptionInfo>[] AI = NewGroup();

	private static List<OptionInfo>[] NewGroup()
	{
		var group = new List<OptionInfo>[(int)OptionOwner.OPTION_OWNER_MAX];
		for (int i = 0; i < group.Length; ++i)
			group[i] = [];
		return group;
	}

	/// <summary>Sorts the options with a description into the categories, in registration order.</summary>
	/// <param name="infos">All options.</param>
	/// <param name="keys">True: only key options (Controls screen, no AI category); false: all other options (Advanced screen).</param>
	public static OptionCategories Build(IEnumerable<OptionInfo> infos, bool keys)
	{
		var ret = new OptionCategories();
		foreach (var optionInfo in infos)
		{
			if ((optionInfo.Type == OptionType.OPTION_KEY) == keys && optionInfo.Description.Length != 0)
			{
				int owner = (int)optionInfo.Owner;
				if (optionInfo.Category == "STR_GENERAL")
				{
					ret.General[owner].Add(optionInfo);
				}
				else if (optionInfo.Category == "STR_GEOSCAPE")
				{
					ret.Geo[owner].Add(optionInfo);
				}
				else if (optionInfo.Category == "STR_BASESCAPE")
				{
					ret.Base[owner].Add(optionInfo);
				}
				else if (optionInfo.Category == "STR_BATTLESCAPE")
				{
					ret.Battle[owner].Add(optionInfo);
				}
				else if (!keys && optionInfo.Category == "STR_AI")
				{
					ret.AI[owner].Add(optionInfo);
				}
			}
		}
		return ret;
	}
}

/// <summary>Options window that displays the advanced game settings.</summary>
public class OptionsAdvancedState : OptionsBaseState
{
	private readonly TextButton _btnOXC, _btnOXCE, _btnOTHER;
	private readonly System.Runtime.CompilerServices.StrongBox<TextButton> _owner;
	private readonly TextList _lstOptions;
	private readonly bool _isTFTD;
	private readonly byte _colorGroup, _greyedOutColor;
	private readonly OptionCategories _settings;
	private int _offsetGeneralMin = -1;
	private int _offsetGeneralMax = -1;
	private int _offsetGeoMin = -1;
	private int _offsetGeoMax = -1;
	private int _offsetBaseMin = -1;
	private int _offsetBaseMax = -1;
	private int _offsetBattleMin = -1;
	private int _offsetBattleMax = -1;
	private int _offsetAIMin = -1;
	private int _offsetAIMax = -1;

	/// <summary>Initializes all the elements in the Advanced Options window.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	public OptionsAdvancedState(OptionsOrigin origin) : base(origin)
	{
		SetCategory(_btnAdvanced);

		// Create objects
		_btnOXC = new TextButton(70, 16, 94, 8);
		_btnOXCE = new TextButton(70, 16, 168, 8);
		_btnOTHER = new TextButton(70, 16, 242, 8);
		_lstOptions = new TextList(200, 120, 94, 26);

		_owner = new(_btnOXC);

		_isTFTD = false;
		foreach (var pair in Options.mods)
		{
			if (pair.Active)
			{
				if (pair.Id == "xcom2")
				{
					_isTFTD = true;
					break;
				}
			}
		}

		Add(_btnOXC, "button", "advancedMenu");
		Add(_btnOXCE, "button", "advancedMenu");
		Add(_btnOTHER, "button", "advancedMenu");

		if (origin != OptionsOrigin.OPT_BATTLESCAPE)
		{
			_greyedOutColor = (byte)_game.Mod.GetInterface("advancedMenu").GetElement("disabledUserOption").color;
			Add(_lstOptions, "optionLists", "advancedMenu");
		}
		else
		{
			_greyedOutColor = (byte)_game.Mod.GetInterface("battlescape").GetElement("disabledUserOption").color;
			Add(_lstOptions, "optionLists", "battlescape");
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

		// how much room do we need for YES/NO
		var text = new Text(100, 9, 0, 0);
		text.InitText(_game.Mod.GetFont("FONT_BIG"), _game.Mod.GetFont("FONT_SMALL"), _game.Language);
		text.SetText(Tr("STR_YES"));
		int yes = text.GetTextWidth();
		text.SetText(Tr("STR_NO"));
		int no = text.GetTextWidth();

		int rightcol = Math.Max(yes, no) + 2;
		int leftcol = _lstOptions.Width - rightcol;

		// Set up objects
		_lstOptions.SetAlign(TextHAlign.ALIGN_RIGHT, 1);
		_lstOptions.SetColumns(2, leftcol, rightcol);
		_lstOptions.SetWordWrap(true);
		_lstOptions.SetSelectable(true);
		_lstOptions.SetBackground(_window);
		_lstOptions.OnMouseClick(LstOptionsClick, 0);
		_lstOptions.OnMouseOver(LstOptionsMouseOver);
		_lstOptions.OnMouseOut(LstOptionsMouseOut);

		_colorGroup = _lstOptions.SecondaryColor;

		_settings = OptionCategories.Build(Options.GetOptionInfo(), false);
	}

	/// <summary>Refreshes the UI.</summary>
	public override void Init()
	{
		base.Init();

		UpdateList();
	}

	private OptionOwner CurrentOwner => _owner.Value == _btnOXC ? OptionOwner.OPTION_OXC : _owner.Value == _btnOXCE ? OptionOwner.OPTION_OXCE : OptionOwner.OPTION_OTHER;

	/// <summary>Fills the settings list based on category.</summary>
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
		_offsetAIMin = -1;
		_offsetAIMax = -1;

		_lstOptions.ClearList();

		int row = -1;

		if (_settings.General[idx].Count > 0)
		{
			_lstOptions.AddRow(2, Tr("STR_GENERAL"), "");
			row++;
			_offsetGeneralMin = row;
			_lstOptions.SetCellColor(_offsetGeneralMin, 0, _colorGroup);
			AddSettings(_settings.General[idx]);
			row += _settings.General[idx].Count;
			_offsetGeneralMax = row;
		}
		if (_settings.Geo[idx].Count > 0)
		{
			if (row > -1) { _lstOptions.AddRow(2, "", ""); row++; }
			_lstOptions.AddRow(2, Tr("STR_GEOSCAPE"), "");
			row++;
			_offsetGeoMin = row;
			_lstOptions.SetCellColor(_offsetGeoMin, 0, _colorGroup);
			AddSettings(_settings.Geo[idx]);
			row += _settings.Geo[idx].Count;
			_offsetGeoMax = row;
		}
		if (_settings.Base[idx].Count > 0)
		{
			if (row > -1) { _lstOptions.AddRow(2, "", ""); row++; }
			_lstOptions.AddRow(2, Tr("STR_BASESCAPE"), "");
			row++;
			_offsetBaseMin = row;
			_lstOptions.SetCellColor(_offsetBaseMin, 0, _colorGroup);
			AddSettings(_settings.Base[idx]);
			row += _settings.Base[idx].Count;
			_offsetBaseMax = row;
		}
		if (_settings.Battle[idx].Count > 0)
		{
			if (row > -1) { _lstOptions.AddRow(2, "", ""); row++; }
			_lstOptions.AddRow(2, Tr("STR_BATTLESCAPE"), "");
			row++;
			_offsetBattleMin = row;
			_lstOptions.SetCellColor(_offsetBattleMin, 0, _colorGroup);
			AddSettings(_settings.Battle[idx]);
			row += _settings.Battle[idx].Count;
			_offsetBattleMax = row;
		}
		if (_settings.AI[idx].Count > 0)
		{
			if (row > -1) { _lstOptions.AddRow(2, "", ""); row++; }
			_lstOptions.AddRow(2, Tr("STR_AI"), "");
			row++;
			_offsetAIMin = row;
			_lstOptions.SetCellColor(_offsetAIMin, 0, _colorGroup);
			AddSettings(_settings.AI[idx]);
			row += _settings.AI[idx].Count;
			_offsetAIMax = row;
		}
	}

	/// <summary>Adds a bunch of settings to the list.</summary>
	/// <param name="settings">List of settings.</param>
	private void AddSettings(List<OptionInfo> settings)
	{
		var fixeduserOptions = _game.Mod.FixedUserOptions;
		foreach (var optionInfo in settings)
		{
			string name = Tr(optionInfo.Description);
			string value = "";
			if (optionInfo.Type == OptionType.OPTION_BOOL)
			{
				value = optionInfo.AsBool ? Tr("STR_YES") : Tr("STR_NO");
			}
			else if (optionInfo.Type == OptionType.OPTION_INT)
			{
				value = optionInfo.AsInt.ToString(CultureInfo.InvariantCulture);
			}
			_lstOptions.AddRow(2, name, value);
			// grey out fixed options
			if (fixeduserOptions.ContainsKey(optionInfo.Id))
			{
				_lstOptions.SetRowColor(_lstOptions.LastRowIndex, _greyedOutColor);
			}
		}
	}

	/// <summary>Gets the currently selected setting.</summary>
	/// <param name="sel">Selected row.</param>
	/// <returns>Option, null if none selected.</returns>
	private OptionInfo GetSetting(uint sel)
	{
		int selInt = (int)sel;
		int idx = (int)CurrentOwner;

		if (selInt > _offsetGeneralMin && selInt <= _offsetGeneralMax)
		{
			return _settings.General[idx][selInt - 1 - _offsetGeneralMin];
		}
		else if (selInt > _offsetGeoMin && selInt <= _offsetGeoMax)
		{
			return _settings.Geo[idx][selInt - 1 - _offsetGeoMin];
		}
		else if (selInt > _offsetBaseMin && selInt <= _offsetBaseMax)
		{
			return _settings.Base[idx][selInt - 1 - _offsetBaseMin];
		}
		else if (selInt > _offsetBattleMin && selInt <= _offsetBattleMax)
		{
			return _settings.Battle[idx][selInt - 1 - _offsetBattleMin];
		}
		else if (selInt > _offsetAIMin && selInt <= _offsetAIMax)
		{
			return _settings.AI[idx][selInt - 1 - _offsetAIMin];
		}
		else
		{
			return null;
		}
	}

	/// <summary>
	/// The new value of an integer setting after a click: left-click increases, right-click decreases,
	/// some settings use bigger steps, and values wrap around at the setting's limits.
	/// </summary>
	/// <param name="id">Option id (the C++ variable name).</param>
	/// <param name="value">Current value.</param>
	/// <param name="leftClick">Left (true) or right (false) mouse button.</param>
	/// <param name="isTFTD">TFTD is the active master (night vision colours differ).</param>
	internal static int StepIntSetting(string id, int value, bool leftClick, bool isTFTD)
	{
		int i = value;
		int increment = leftClick ? 1 : -1; // left-click increases, right-click decreases
		if (id is "changeValueByMouseWheel" or "FPS" or "FPSInactive" or "oxceWoundedDefendBaseIf")
		{
			increment *= 10;
		}
		else if (id is "oxceResearchScrollSpeedWithCtrl" or "oxceManufactureScrollSpeedWithCtrl" or "oxceReactionFireThreshold")
		{
			increment *= 5;
		}
		else if (id == "oxceInterceptTableSize")
		{
			increment *= 4;
		}
		i += increment;

		int min = 0, max = 0;
		if (id == "battleExplosionHeight")
		{
			min = 0;
			max = 3;
		}
		else if (id == "changeValueByMouseWheel")
		{
			min = 0;
			max = 100;
		}
		else if (id == "FPS")
		{
			min = 0;
			max = 120;
		}
		else if (id == "FPSInactive")
		{
			min = 10;
			max = 120;
		}
		else if (id == "mousewheelSpeed")
		{
			min = 1;
			max = 7;
		}
		else if (id == "autosaveFrequency")
		{
			min = 1;
			max = 5;
		}
		else if (id == "oxceGeoAutosaveFrequency")
		{
			min = 0;
			max = 10;
		}
		else if (id is "autosaveSlots" or "oxceGeoAutosaveSlots" or "oxceResearchScrollSpeed" or "oxceManufactureScrollSpeed")
		{
			min = 1;
			max = 10;
		}
		else if (id is "oxceInterceptGuiMaintenanceTime" or "oxceShowETAMode" or "oxceShowAccuracyOnCrosshair" or "oxceCrashedOrLanded")
		{
			min = 0;
			max = 2;
		}
		else if (id == "oxceInterceptTableSize")
		{
			min = 8;
			max = 80;
		}
		else if (id is "oxceWoundedDefendBaseIf" or "oxceReactionFireThreshold")
		{
			min = 0;
			max = 100;
		}
		else if (id is "oxceResearchScrollSpeedWithCtrl" or "oxceManufactureScrollSpeedWithCtrl")
		{
			min = 5;
			max = 50;
		}
		else if (id == "oxceAutoNightVisionThreshold")
		{
			min = 0;
			max = 15;
		}
		else if (id == "oxceNightVisionColor")
		{
			// UFO: 1-15, TFTD: 2-16 except 8 and 10
			if (isTFTD && (i == 8 || i == 10))
			{
				i += increment;
			}
			min = isTFTD ? 2 : 1;
			max = isTFTD ? 16 : 15;
		}

		if (i < min)
		{
			i = max;
		}
		else if (i > max)
		{
			i = min;
		}
		return i;
	}

	/// <summary>Changes the clicked setting.</summary>
	public void LstOptionsClick(Action action)
	{
		byte button = action.Details.button.button;
		if (button != SDLMouse.SDL_BUTTON_LEFT && button != SDLMouse.SDL_BUTTON_RIGHT)
		{
			return;
		}
		uint sel = _lstOptions.SelectedRow;
		OptionInfo setting = GetSetting(sel);
		if (setting == null) return;

		// greyed out options are fixed, cannot be changed by the user
		if (_game.Mod.FixedUserOptions.ContainsKey(setting.Id))
		{
			return;
		}

		string settingText = "";
		if (setting.Type == OptionType.OPTION_BOOL)
		{
			bool b = !setting.AsBool;
			setting.AsBool = b;
			settingText = b ? Tr("STR_YES") : Tr("STR_NO");
			if (setting.Id == "lazyLoadResources" && !b)
			{
				Options.reload = true; // reload when turning lazy loading off
			}
		}
		else if (setting.Type == OptionType.OPTION_INT) // integer variables will need special handling
		{
			int i = StepIntSetting(setting.Id, setting.AsInt, button == SDLMouse.SDL_BUTTON_LEFT, _isTFTD);
			setting.AsInt = i;
			settingText = i.ToString(CultureInfo.InvariantCulture);
		}
		_lstOptions.SetCellText((int)sel, 1, settingText);
	}

	/// <summary>Handler for moving the mouse over a setting.</summary>
	public void LstOptionsMouseOver(Action action)
	{
		uint sel = _lstOptions.SelectedRow;
		OptionInfo setting = GetSetting(sel);
		string desc = "";
		if (setting != null)
		{
			desc = Tr(setting.Description + "_DESC");
		}
		_txtTooltip.SetText(desc);
	}

	/// <summary>Handler for moving the mouse outside the settings.</summary>
	public void LstOptionsMouseOut(Action action)
	{
		_txtTooltip.SetText("");
	}

	/// <summary>Handler for clicking buttons.</summary>
	public new void BtnGroupPress(Action action)
	{
		UpdateList();
	}
}
