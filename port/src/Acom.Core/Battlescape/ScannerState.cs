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
// Ported from: src/Battlescape/ScannerState.h, src/Battlescape/ScannerState.cpp
#nullable disable

namespace Acom;

/// <summary>
/// The Scanner User Interface (motion scanner).
/// </summary>
public class ScannerState : State
{
	private readonly InteractiveSurface _bg;
	private readonly Surface _scan;
	private readonly ScannerView _scannerView;
	private readonly BattleAction _action;
	private readonly Timer _timerAnimate;

	/// <summary>Initializes the Scanner State.</summary>
	/// <param name="action">Pointer to an action.</param>
	public ScannerState(BattleAction action)
	{
		ArgumentNullException.ThrowIfNull(action);
		_action = action;
		if (Options.maximizeInfoScreens)
		{
			Options.baseXResolution = Screen.ORIGINAL_WIDTH;
			Options.baseYResolution = Screen.ORIGINAL_HEIGHT;
			_game.Screen.ResetDisplay(false);
		}
		_bg = new InteractiveSurface(320, 200);
		_scan = new Surface(320, 200);
		_scannerView = new ScannerView(152, 152, 56, 24, _game, _action.actor);

		if (_game.Screen.DY > 50)
		{
			_screen = false;
		}

		// Set palette
		_game.SavedGame.SavedBattle.SetPaletteByDepth(this);

		Add(_scan);
		Add(_scannerView);
		Add(_bg);

		CenterAllSurfaces();

		_game.Mod.GetSurface("DETBORD.PCK").BlitNShade(_bg, 0, 0);
		_game.Mod.GetSurface("DETBORD2.PCK").BlitNShade(_scan, 0, 0);
		_bg.OnMouseClick(ExitClick);
		_bg.OnKeyboardPress(ExitClick, Options.keyCancel);

		_timerAnimate = new Timer(125);
		_timerAnimate.OnTimer((StateHandler)Animate);
		_timerAnimate.Start();

		Update();
	}

	/// <summary>Closes the window on right-click.</summary>
	public override void Handle(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		base.Handle(action);
		if (action.Details.type == SDLEventType.SDL_MOUSEBUTTONDOWN && _game.IsRightClick(action))
		{
			ExitClick(action);
		}
	}

	/// <summary>Updates scanner state.</summary>
#pragma warning disable CA1822 // empty in C++ as well
	private void Update()
#pragma warning restore CA1822
	{
		//_scannerView->draw();
	}

	/// <summary>Animation handler. Updates the minimap view animation.</summary>
	private void Animate()
	{
		_scannerView.Animate();
	}

	/// <summary>Handles timers.</summary>
	public override void Think()
	{
		base.Think();
		_timerAnimate.Think(this, null);
	}

	/// <summary>Exits the screen.</summary>
#pragma warning disable CA1822 // handler is an instance member as in C++ (State._game is static)
	public void ExitClick(Action action)
#pragma warning restore CA1822
	{
		if (Options.maximizeInfoScreens)
		{
			Screen.UpdateScale(Options.battlescapeScale, ref Options.baseXBattlescape, ref Options.baseYBattlescape, true);
			_game.Screen.ResetDisplay(false);
		}
		_game.PopState();
	}
}
