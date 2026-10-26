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
// Ported from: src/Menu/AbandonGameState.h, src/Menu/AbandonGameState.cpp
// Port status: partial (missing: Screen::resetDisplay, the port's screen keeps its logical size)
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Abandon Game window shown before
/// quitting the game from the Geoscape.
/// </summary>
public class AbandonGameState : State
{
	private readonly OptionsOrigin _origin;
	private readonly Window _window;
	private readonly TextButton _btnYes, _btnNo;
	private readonly Text _txtTitle;

	/// <summary>
	/// Initializes all the elements in the Abandon Game screen.
	/// </summary>
	/// <param name="origin">Game section that originated this state.</param>
	public AbandonGameState(OptionsOrigin origin)
	{
		_origin = origin;
		_screen = false;

		int x;
		if (_origin == OptionsOrigin.OPT_GEOSCAPE)
		{
			x = 20;
		}
		else
		{
			x = 52;
		}

		// Create objects
		_window = new Window(this, 216, 160, x, 20, WindowPopup.POPUP_BOTH);
		_btnYes = new TextButton(50, 20, x + 18, 140);
		_btnNo = new TextButton(50, 20, x + 148, 140);
		_txtTitle = new Text(206, 17, x + 5, 70);

		// Set palette
		SetInterface("geoscape", false, _game.SavedGame != null ? _game.SavedGame.SavedBattle : null);

		Add(_window, "genericWindow", "geoscape");
		Add(_btnYes, "genericButton2", "geoscape");
		Add(_btnNo, "genericButton2", "geoscape");
		Add(_txtTitle, "genericText", "geoscape");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "geoscape");

		_btnYes.Text = Tr("STR_YES");
		_btnYes.OnMouseClick(BtnYesClick);
		_btnYes.OnKeyboardPress(BtnYesClick, Options.keyOk);

		_btnNo.Text = Tr("STR_NO");
		_btnNo.OnMouseClick(BtnNoClick);
		_btnNo.OnKeyboardPress(BtnNoClick, Options.keyCancel);

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetBig();
		_txtTitle.SetText(Tr("STR_ABANDON_GAME_QUESTION"));

		if (_origin == OptionsOrigin.OPT_BATTLESCAPE)
		{
			ApplyBattlescapeTheme("geoscape");
		}
	}

	/// <summary>
	/// Goes back to the Main Menu.
	/// </summary>
	public void BtnYesClick(Action action)
	{
		// Reset touch flags
		_game.ResetTouchButtonFlags();

		if (_origin == OptionsOrigin.OPT_BATTLESCAPE && _game.SavedGame.SavedBattle.AmbientSound != Mod.NO_SOUND)
			_game.Mod.GetSoundByDepth(0, (uint)_game.SavedGame.SavedBattle.AmbientSound).StopLoop();
		if (!_game.SavedGame.IsIronman)
		{
			Screen.UpdateScale(Options.geoscapeScale, ref Options.baseXGeoscape, ref Options.baseYGeoscape, true);
			// missing: _game->getScreen()->resetDisplay(false);

			_game.SetState(new MainMenuState());
			_game.SavedGame = null;
		}
		else
		{
			_game.PushState(new SaveGameState(OptionsOrigin.OPT_GEOSCAPE, SaveType.SAVE_IRONMAN_END, _palette));
		}
	}

	/// <summary>
	/// Closes the window.
	/// </summary>
	public void BtnNoClick(Action action)
	{
		_game.PopState();
	}
}
