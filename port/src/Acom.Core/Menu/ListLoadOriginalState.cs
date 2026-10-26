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
// Ported from: src/Menu/ListLoadOriginalState.h, src/Menu/ListLoadOriginalState.cpp
#nullable disable
#pragma warning disable CA1822 // UI handlers stay instance members like in C++ (State._game is static here)

using System.Globalization;

namespace Acom;

/// <summary>Load screen for the saved games of the original X-Com.</summary>
public class ListLoadOriginalState : State
{
	private TextButton _btnNew, _btnCancel;
	private Window _window;
	private Text _txtTitle, _txtName, _txtTime, _txtDate;
	private readonly TextButton[] _btnSlot = new TextButton[SaveConverter.NUM_SAVES];
	private readonly Text[] _txtSlotName = new Text[SaveConverter.NUM_SAVES];
	private readonly Text[] _txtSlotTime = new Text[SaveConverter.NUM_SAVES];
	private readonly Text[] _txtSlotDate = new Text[SaveConverter.NUM_SAVES];
	private readonly SaveOriginal[] _saves = new SaveOriginal[SaveConverter.NUM_SAVES];
	private OptionsOrigin _origin;

	/// <summary>Initializes all the elements in the Saved Game screen.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	public ListLoadOriginalState(OptionsOrigin origin)
	{
		_origin = origin;
		for (int i = 0; i < SaveConverter.NUM_SAVES; ++i)
			_saves[i] = new SaveOriginal();
		_screen = false;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnNew = new TextButton(80, 16, 60, 172);
		_btnCancel = new TextButton(80, 16, 180, 172);
		_txtTitle = new Text(310, 17, 5, 7);
		_txtName = new Text(160, 9, 36, 24);
		_txtTime = new Text(30, 9, 195, 24);
		_txtDate = new Text(90, 9, 225, 24);

		// Set palette
		SetInterface("geoscape", true, _game.SavedGame?.SavedBattle);

		Add(_window, "window", "saveMenus");
		Add(_btnNew, "button", "saveMenus");
		Add(_btnCancel, "button", "saveMenus");
		Add(_txtTitle, "text", "saveMenus");
		Add(_txtName, "text", "saveMenus");
		Add(_txtTime, "text", "saveMenus");
		Add(_txtDate, "text", "saveMenus");

		int y = 34;
		for (int i = 0; i < SaveConverter.NUM_SAVES; ++i)
		{
			_btnSlot[i] = new TextButton(24, 12, 10, y - 2);
			_txtSlotName[i] = new Text(160, 9, 36, y);
			_txtSlotTime[i] = new Text(30, 9, 195, y);
			_txtSlotDate[i] = new Text(90, 9, 225, y);

			Add(_btnSlot[i], "button", "saveMenus");
			Add(_txtSlotName[i], "list", "saveMenus");
			Add(_txtSlotTime[i], "list", "saveMenus");
			Add(_txtSlotDate[i], "list", "saveMenus");

			y += 14;
		}

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "saveMenus");

		_btnNew.Text = Tr("STR_OPENXCOM").Arg(Options.GetActiveMasterInfo().VersionDisplay);
		_btnNew.OnMouseClick(BtnNewClick);
		_btnNew.OnKeyboardPress(BtnNewClick, Options.keyCancel);

		_btnCancel.Text = Tr("STR_CANCEL");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_SELECT_GAME_TO_LOAD"));

		_txtName.SetText(Tr("STR_NAME"));

		_txtTime.SetText(Tr("STR_TIME"));

		_txtDate.SetText(Tr("STR_DATE"));

		string dots = new('.', 80);
		SaveConverter.GetList(_game.Language, _saves);
		for (int i = 0; i < SaveConverter.NUM_SAVES; ++i)
		{
			_btnSlot[i].Text = (i + 1).ToString(CultureInfo.InvariantCulture);
			_btnSlot[i].OnMouseClick(BtnSlotClick);

			_txtSlotName[i].SetText(_saves[i].name + dots);
			_txtSlotTime[i].SetText(_saves[i].time);
			_txtSlotDate[i].SetText(_saves[i].date);
		}
	}

	/// <summary>Refreshes the saves list.</summary>
	public override void Init()
	{
		base.Init();

		if (_origin == OptionsOrigin.OPT_BATTLESCAPE)
		{
			ApplyBattlescapeTheme("saveMenus");
		}
	}

	/// <summary>Switches to OpenXcom saves.</summary>
	public void BtnNewClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>Returns to the previous screen.</summary>
	public void BtnCancelClick(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		_game.PopState();
		_game.PopState();
		action.Details.type = SDLEventType.SDL_NOEVENT;
	}

	/// <summary>Loads the specified save.</summary>
	public void BtnSlotClick(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		int n = 0;
		for (int i = 0; i < SaveConverter.NUM_SAVES; ++i)
		{
			if (action.Sender == _btnSlot[i])
			{
				n = i;
				break;
			}
		}
		if (_saves[n].id > 0)
		{
			if (_saves[n].tactical)
			{
				string error = Tr("STR_LOAD_UNSUCCESSFUL") + Unicode.TOK_NL_SMALL + "Battlescape saves aren't supported.";
				var errorMessages = _game.Mod.GetInterface("errorMessages");
				_game.PushState(new ErrorMessageState(error, _palette, unchecked((byte)errorMessages.GetElement("geoscapeColor").color), "BACK01.SCR", errorMessages.GetElement("geoscapePalette").color));
			}
			else
			{
				// Reset touch flags
				_game.ResetTouchButtonFlags();

				var converter = new SaveConverter(_saves[n].id, _game.Mod);
				_game.SavedGame = converter.LoadOriginal();
				Options.baseXResolution = Options.baseXGeoscape;
				Options.baseYResolution = Options.baseYGeoscape;
				_game.Screen.ResetDisplay(false);
				_game.SetState(new GeoscapeState());
				if (_game.SavedGame.SavedBattle != null)
				{
					_game.SavedGame.SavedBattle.LoadMapResources(_game.Mod);
					Options.baseXResolution = Options.baseXBattlescape;
					Options.baseYResolution = Options.baseYBattlescape;
					_game.Screen.ResetDisplay(false);
					var bs = new BattlescapeState();
					_game.PushState(bs);
					_game.SavedGame.SavedBattle.BattleState = bs;
					// Try to reactivate the touch buttons
					bs.ToggleTouchButtons(false, true);
				}
			}
		}
	}
}
