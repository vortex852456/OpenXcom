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
// Ported from: src/Battlescape/ConfirmEndMissionState.h, src/Battlescape/ConfirmEndMissionState.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Screen which asks for confirmation to end the mission.
/// </summary>
public class ConfirmEndMissionState : State
{
	private readonly Window _window;
	private readonly Text _txtTitle, _txtWounded, _txtConfirm;
	private readonly TextButton _btnOk, _btnCancel;
	private readonly SavedBattleGame _battleGame;
	private readonly int _wounded;
	private readonly BattlescapeGame _parent;

	/// <summary>
	/// Initializes all the elements in the ConfirmEndMission window.
	/// </summary>
	public ConfirmEndMissionState(SavedBattleGame battleGame, int wounded, BattlescapeGame parent)
	{
		_battleGame = battleGame;
		_wounded = wounded;
		_parent = parent;

		// Create objects
		_screen = false;
		_window = new Window(this, 320, 144, 0, 0);
		_txtTitle = new Text(304, 17, 16, 26);
		_txtWounded = new Text(304, 17, 16, 54);
		_txtConfirm = new Text(320, 17, 0, 80);
		_btnOk = new TextButton(120, 16, 16, 110);
		_btnCancel = new TextButton(120, 16, 184, 110);

		// Set palette
		_battleGame.SetPaletteByDepth(this);

		Add(_window, "messageWindowBorder", "battlescape");
		Add(_txtTitle, "messageWindows", "battlescape");
		Add(_txtWounded, "messageWindows", "battlescape");
		Add(_txtConfirm, "messageWindows", "battlescape");
		Add(_btnOk, "messageWindowButtons", "battlescape");
		Add(_btnCancel, "messageWindowButtons", "battlescape");

		// Set up objects
		_window.SetHighContrast(true);
		_window.SetBackground(_game.Mod.GetSurface("TAC00.SCR"));

		_txtTitle.SetBig();
		_txtTitle.SetHighContrast(true);
		_txtTitle.SetText(Tr("STR_MISSION_OVER"));

		_txtWounded.SetBig();
		_txtWounded.SetHighContrast(true);
		_txtWounded.SetText(Tr("STR_UNITS_WITH_FATAL_WOUNDS", _wounded));

		_txtConfirm.SetBig();
		_txtConfirm.Align = TextHAlign.ALIGN_CENTER;
		_txtConfirm.SetHighContrast(true);
		_txtConfirm.SetText(Tr("STR_END_MISSION_QUESTION"));

		_btnOk.Text = Tr("STR_OK");
		_btnOk.SetHighContrast(true);
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.SetHighContrast(true);
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyBattleAbort);

		CenterAllSurfaces();
	}

	/// <summary>Confirms mission end.</summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
		_parent.RequestEndTurn(false);
	}

	/// <summary>Returns to the previous screen.</summary>
#pragma warning disable CA1822 // instance handler as in C++
	public void BtnCancelClick(Action action)
#pragma warning restore CA1822
	{
		_game.PopState();
	}
}
