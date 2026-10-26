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
// Ported from: src/Battlescape/PrimeGrenadeState.h, src/Battlescape/PrimeGrenadeState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

/// <summary>
/// Window that allows the player
/// to set the timer of an explosive.
/// </summary>
public class PrimeGrenadeState : State
{
	private readonly BattleAction _action;
	private readonly bool _inInventoryView;
	private readonly BattleItem _grenadeInInventory;
	private readonly Text[] _number = new Text[24];
	private readonly Text _title;
	private readonly Frame _frame;
	private readonly InteractiveSurface[] _button = new InteractiveSurface[24];
	private readonly Surface _bg;

	/// <summary>Initializes all the elements in the Prime Grenade window.</summary>
	/// <param name="action">Pointer to the action.</param>
	/// <param name="inInventoryView">Called from inventory?</param>
	/// <param name="grenadeInInventory">Pointer to associated grenade.</param>
	public PrimeGrenadeState(BattleAction action, bool inInventoryView, BattleItem grenadeInInventory)
	{
		_action = action;
		_inInventoryView = inInventoryView;
		_grenadeInInventory = grenadeInInventory;
		_screen = false;

		// Create objects
		_title = new Text(192, 24, 65, 44);
		_frame = new Frame(192, 27, 65, 37);
		_bg = new Surface(192, 93, 65, 45);

		int x = 67;
		int y = 68;
		for (int i = 0; i < 24; ++i)
		{
			_button[i] = new InteractiveSurface(22, 22, x - 1 + ((i % 8) * 24), y - 4 + ((i / 8) * 25));
			_number[i] = new Text(20, 20, x + ((i % 8) * 24), y - 1 + ((i / 8) * 25));
		}

		// Set palette
		if (inInventoryView)
		{
			SetStandardPalette("PAL_BATTLESCAPE");
		}
		else
		{
			_game.SavedGame.SavedBattle.SetPaletteByDepth(this);
		}

		Element grenadeBackground = _game.Mod.GetInterface("battlescape").GetElement("grenadeBackground");

		// Set up objects
		Add(_bg);
		_bg.DrawRect(0, 0, (short)_bg.Width, (short)_bg.Height, (byte)grenadeBackground.color);

		Add(_frame, "grenadeMenu", "battlescape");
		_frame.SetThickness(3);
		_frame.SetHighContrast(true);

		Add(_title, "grenadeMenu", "battlescape");
		_title.Align = TextHAlign.ALIGN_CENTER;
		_title.SetBig();
		_title.SetText(Tr("STR_SET_TIMER"));
		_title.SetHighContrast(true);

		for (int i = 0; i < 24; ++i)
		{
			SDL_Rect square;
			Add(_button[i]);
			_button[i].OnMouseClick(BtnClick);
			square.x = 0;
			square.y = 0;
			square.w = (ushort)_button[i].Width;
			square.h = (ushort)_button[i].Height;
			_button[i].DrawRect(ref square, (byte)grenadeBackground.border);
			square.x++;
			square.y++;
			square.w -= 2;
			square.h -= 2;
			_button[i].DrawRect(ref square, (byte)grenadeBackground.color2);

			Add(_number[i], "grenadeMenu", "battlescape");
			_number[i].SetBig();
			_number[i].SetText(i.ToString(CultureInfo.InvariantCulture));
			_number[i].SetHighContrast(true);
			_number[i].Align = TextHAlign.ALIGN_CENTER;
			_number[i].VerticalAlign = TextVAlign.ALIGN_MIDDLE;
		}

		_button[0].OnKeyboardRelease(BtnClick, SDLKey.SDLK_q); // FIXME: this is just a workaround, fix it better!
		_button[0].OnKeyboardPress(BtnClick, SDLKey.SDLK_0);
		_button[1].OnKeyboardPress(BtnClick, SDLKey.SDLK_1);
		_button[2].OnKeyboardPress(BtnClick, SDLKey.SDLK_2);
		_button[3].OnKeyboardPress(BtnClick, SDLKey.SDLK_3);

		CenterAllSurfaces();
		LowerAllSurfaces();
	}

	/// <summary>Closes the window on right-click.</summary>
	public override void Handle(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		base.Handle(action);
		if (action.Details.type == SDLEventType.SDL_MOUSEBUTTONDOWN && _game.IsRightClick(action))
		{
			if (!_inInventoryView) _action.value = -1;
			_game.PopState();
		}
		else if (action.Details.type == SDLEventType.SDL_KEYDOWN && action.Details.key.keysym.sym == Options.keyCancel)
		{
			if (!_inInventoryView) _action.value = -1;
			_game.PopState();
		}
	}

	/// <summary>Executes the action corresponding to this action menu item.</summary>
	public void BtnClick(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		int btnID = -1;

		if (_game.IsRightClick(action))
		{
			if (!_inInventoryView) _action.value = btnID;
			_game.PopState();
			return;
		}

		// got to find out which button was pressed
		for (int i = 0; i < 24 && btnID == -1; ++i)
		{
			if (action.Sender == _button[i])
			{
				btnID = i;
			}
		}

		if (btnID != -1)
		{
			if (_inInventoryView)
			{
				_grenadeInInventory.FuseTimer = 0 + btnID;
				// prime sound
				int sound = _grenadeInInventory.Rules.PrimeSound;
				if (sound != Mod.NO_SOUND)
				{
					_game.Mod.GetSoundByDepth((uint)_game.SavedGame.SavedBattle.Depth, (uint)sound).Play();
				}
			}
			else
			{
				_action.value = btnID;
			}
			_game.PopState();
			if (!_inInventoryView) _game.PopState();
		}
	}
}
