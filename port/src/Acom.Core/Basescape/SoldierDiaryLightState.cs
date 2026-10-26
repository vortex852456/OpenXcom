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
// Ported from: src/Basescape/SoldierDiaryLightState.h, src/Basescape/SoldierDiaryLightState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)
#pragma warning disable IDE0060 // handler signatures keep the unused Action parameter like C++

/// <summary>
/// Soldier Diary Light window that allows viewing basic data from the soldier's diary.
/// </summary>
public class SoldierDiaryLightState : State
{
	private readonly Soldier _soldier;
	private readonly TextButton _btnOk;
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly TextList _lstStats;

	/// <summary>
	/// Initializes all the elements in the Soldier Diary Light window.
	/// </summary>
	/// <param name="soldier">Pointer to the soldier to get info from.</param>
	public SoldierDiaryLightState(Soldier soldier)
	{
		_soldier = soldier;
		_screen = false;

		// Create objects
		_window = new Window(this, 240, 160, 40, 24, WindowPopup.POPUP_BOTH);
		_btnOk = new TextButton(100, 16, 110, 160);
		_txtTitle = new Text(220, 9, 50, 36);
		_lstStats = new TextList(177, 96, 74, 56);

		// Set palette
		SetStandardPalette("PAL_BATTLESCAPE");

		Add(_window, "window", "soldierDiaryLight");
		Add(_btnOk, "button", "soldierDiaryLight");
		Add(_txtTitle, "text", "soldierDiaryLight");
		Add(_lstStats, "list", "soldierDiaryLight");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "soldierDiaryLight");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_NEUTRALIZATIONS_BY_WEAPON"));

		_lstStats.SetColumns(2, 156, 20);
		_lstStats.SetBackground(_window);
		_lstStats.SetDot(true);

		foreach (var mapItem in _soldier.Diary.WeaponTotal)
		{
			_lstStats.AddRow(2, Tr(mapItem.Key), mapItem.Value.ToString(CultureInfo.InvariantCulture));
		}

		// switch to battlescape theme if called from inventory
		ApplyBattlescapeTheme("soldierDiaryLight");
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}
}
