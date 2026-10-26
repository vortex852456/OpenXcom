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
// Ported from: src/Battlescape/TurnDiaryState.h, src/Battlescape/TurnDiaryState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window which displays the hit log / turn diary.
/// </summary>
public class TurnDiaryState : State
{
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly TextButton _btnCancel;
	private readonly TextList _lstTurnDiary;

	/// <summary>Initializes all the elements in the Turn Diary window.</summary>
	public TurnDiaryState(HitLog hitLog)
	{
		ArgumentNullException.ThrowIfNull(hitLog);
		_screen = false;

		// Create objects
		_window = new Window(this, 216, 160, 52, 20, WindowPopup.POPUP_BOTH);
		_txtTitle = new Text(206, 17, 57, 32);
		_btnCancel = new TextButton(140, 16, 90, 156);
		_lstTurnDiary = new TextList(180, 96, 65, 52);

		// Set palette
		_game.SavedGame.SavedBattle.SetPaletteByDepth(this);

		Add(_window, "messageWindowBorder", "battlescape");
		Add(_txtTitle, "messageWindows", "battlescape");
		Add(_btnCancel, "messageWindowButtons", "battlescape");
		Add(_lstTurnDiary, "optionLists", "battlescape");

		CenterAllSurfaces();

		// Set up objects
		_window.SetHighContrast(true);
		_window.SetBackground(_game.Mod.GetSurface("TAC00.SCR"));

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetBig();
		_txtTitle.SetHighContrast(true);
		_txtTitle.SetText(Tr("STR_HIT_LOG"));

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.SetHighContrast(true);
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_lstTurnDiary.SetColumns(1, 172);
		_lstTurnDiary.SetSelectable(true);
		_lstTurnDiary.SetBackground(_window);
		_lstTurnDiary.Margin = 8;
		_lstTurnDiary.SetAlign(TextHAlign.ALIGN_CENTER);
		_lstTurnDiary.SetHighContrast(true);
		_lstTurnDiary.SetWordWrap(true);

		// in reverse order
		_lstTurnDiary.AddRow(1, hitLog.GetHitLogText(true));
		int row = 1;
		List<string> turnDiary = hitLog.TurnDiary;
		for (int i = turnDiary.Count - 1; i >= 0; --i)
		{
			_lstTurnDiary.AddRow(1, turnDiary[i]);
			if (row % 2 != 0)
			{
				_lstTurnDiary.SetRowColor(row, _lstTurnDiary.SecondaryColor);
			}
			++row;
		}
	}

	/// <summary>Returns to the previous screen.</summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}
}
