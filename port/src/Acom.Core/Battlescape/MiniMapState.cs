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
// Ported from: src/Battlescape/MiniMapState.h, src/Battlescape/MiniMapState.cpp
#nullable disable

namespace Acom;

/// <summary>
/// The MiniMap is a representation of a Battlescape map that allows you to see more of the map.
/// </summary>
public class MiniMapState : State
{
	private readonly Surface _bg;
	private readonly MiniMapView _miniMapView;
	private readonly BattlescapeButton _btnLvlUp, _btnLvlDwn, _btnOk;
	private readonly Text _txtLevel;
	private readonly Timer _timerAnimate;

	/// <summary>Initializes all the elements in the MiniMapState screen.</summary>
	/// <param name="camera">The Battlescape camera.</param>
	/// <param name="battleGame">The Battlescape save.</param>
	public MiniMapState(Camera camera, SavedBattleGame battleGame)
	{
		ArgumentNullException.ThrowIfNull(camera);
		ArgumentNullException.ThrowIfNull(battleGame);
		if (Options.maximizeInfoScreens)
		{
			Options.baseXResolution = Screen.ORIGINAL_WIDTH;
			Options.baseYResolution = Screen.ORIGINAL_HEIGHT;
			_game.Screen.ResetDisplay(false);
		}

		_bg = new Surface(320, 200);
		_miniMapView = new MiniMapView(221, 148, 48, 16, _game, camera, battleGame);
		_btnLvlUp = new BattlescapeButton(18, 20, 24, 62);
		_btnLvlDwn = new BattlescapeButton(18, 20, 24, 88);
		_btnOk = new BattlescapeButton(32, 32, 275, 145);
		_txtLevel = new Text(28, 16, 281, 75);

		// Set palette
		battleGame.SetPaletteByDepth(this);

		Add(_bg);
		_game.Mod.GetSurface("SCANBORD.PCK").BlitNShade(_bg, 0, 0);
		Add(_miniMapView);
		Add(_btnLvlUp, "buttonUp", "minimap", _bg);
		Add(_btnLvlDwn, "buttonDown", "minimap", _bg);
		Add(_btnOk, "buttonOK", "minimap", _bg);
		Add(_txtLevel, "textLevel", "minimap", _bg);

		CenterAllSurfaces();

		if (_game.Screen.DY > 50)
		{
			_screen = false;
			_bg.DrawRect(46, 14, 223, 151, (byte)(Acom.Palette.BlockOffset(15) + 15));
		}

		_btnLvlUp.OnMouseClick(BtnLevelUpClick);
		_btnLvlDwn.OnMouseClick(BtnLevelDownClick);
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyBattleMap);
		_txtLevel.SetBig();
		_txtLevel.SetHighContrast(true);
		_txtLevel.SetText(Tr("STR_LEVEL_SHORT").Arg(camera.ViewLevel));
		_timerAnimate = new Timer(125);
		_timerAnimate.OnTimer((StateHandler)Animate);
		_timerAnimate.Start();
		_miniMapView.Draw();
	}

	/// <summary>Handles mouse-wheeling.</summary>
	public override void Handle(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		base.Handle(action);
		if (action.Details.type == SDLEventType.SDL_MOUSEBUTTONDOWN)
		{
			if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELUP)
			{
				BtnLevelUpClick(action);
			}
			else if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELDOWN)
			{
				BtnLevelDownClick(action);
			}
		}
	}

	/// <summary>Returns to the previous screen.</summary>
#pragma warning disable CA1822 // handler is an instance member as in C++ (State._game is static)
	public void BtnOkClick(Action action)
#pragma warning restore CA1822
	{
		if (Options.maximizeInfoScreens)
		{
			Screen.UpdateScale(Options.battlescapeScale, ref Options.baseXBattlescape, ref Options.baseYBattlescape, true);
			_game.Screen.ResetDisplay(false);
		}
		_game.PopState();
	}

	/// <summary>Changes the currently displayed minimap level.</summary>
	public void BtnLevelUpClick(Action action)
	{
		_txtLevel.SetText(Tr("STR_LEVEL_SHORT").Arg(_miniMapView.Up()));
	}

	/// <summary>Changes the currently displayed minimap level.</summary>
	public void BtnLevelDownClick(Action action)
	{
		_txtLevel.SetText(Tr("STR_LEVEL_SHORT").Arg(_miniMapView.Down()));
	}

	/// <summary>Animation handler. Updates the minimap view animation.</summary>
	private void Animate()
	{
		_miniMapView.Animate();
	}

	/// <summary>Handles timers.</summary>
	public override void Think()
	{
		base.Think();
		_timerAnimate.Think(this, null);
	}
}
