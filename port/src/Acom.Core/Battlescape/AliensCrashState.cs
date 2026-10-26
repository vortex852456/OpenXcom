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
// Ported from: src/Battlescape/AliensCrashState.h, src/Battlescape/AliensCrashState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Screen shown when all aliens died in the crash.
/// </summary>
public class AliensCrashState : State
{
	private readonly TextButton _btnOk;
	private readonly Window _window;
	private readonly Text _txtTitle;

	/// <summary>Initializes all the elements in the Aliens Crash screen.</summary>
	public AliensCrashState()
	{
		// Create objects
		_window = new Window(this, 256, 160, 32, 20);
		_btnOk = new TextButton(120, 18, 100, 154);
		_txtTitle = new Text(246, 80, 37, 50);

		// Set palette
		SetStandardPalette("PAL_BATTLESCAPE");

		Add(_window, "messageWindowBorder", "battlescape");
		Add(_btnOk, "messageWindowButtons", "battlescape");
		Add(_txtTitle, "messageWindows", "battlescape");

		CenterAllSurfaces();

		// Set up objects
		_window.SetHighContrast(true);
		_window.SetBackground(_game.Mod.GetSurface("TAC00.SCR"));

		_btnOk.SetHighContrast(true);
		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtTitle.SetHighContrast(true);
		_txtTitle.SetText(Tr("STR_ALL_ALIENS_KILLED_IN_CRASH"));
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.VerticalAlign = TextVAlign.ALIGN_MIDDLE;
		_txtTitle.SetBig();
		_txtTitle.SetWordWrap(true);
	}

	/// <summary>Returns to the previous screen.</summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
		_game.PushState(new DebriefingState());
	}
}
