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
// Ported from: src/Battlescape/NoExperienceState.h, src/Battlescape/NoExperienceState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window which lists the soldiers that have not gained any experience yet.
/// </summary>
public class NoExperienceState : State
{
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly TextButton _btnCancel;
	private readonly TextList _lstSoldiers;

	/// <summary>Initializes all the elements in the NoExperience window.</summary>
	public NoExperienceState()
	{
		_screen = false;

		// Create objects
		_window = new Window(this, 216, 160, 52, 20, WindowPopup.POPUP_BOTH);
		_txtTitle = new Text(206, 17, 57, 32);
		_btnCancel = new TextButton(140, 16, 90, 156);
		_lstSoldiers = new TextList(180, 96, 65, 52);

		// Set palette
		_game.SavedGame.SavedBattle.SetPaletteByDepth(this);

		Add(_window, "messageWindowBorder", "battlescape");
		Add(_txtTitle, "messageWindows", "battlescape");
		Add(_btnCancel, "messageWindowButtons", "battlescape");
		Add(_lstSoldiers, "optionLists", "battlescape");

		CenterAllSurfaces();

		// Set up objects
		_window.SetHighContrast(true);
		_window.SetBackground(_game.Mod.GetSurface("TAC00.SCR"));

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetBig();
		_txtTitle.SetHighContrast(true);
		_txtTitle.SetText(Tr("STR_NO_EXPERIENCE_YET"));

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.SetHighContrast(true);
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_lstSoldiers.SetColumns(1, 172);
		_lstSoldiers.SetSelectable(true);
		_lstSoldiers.SetBackground(_window);
		_lstSoldiers.Margin = 8;
		_lstSoldiers.SetAlign(TextHAlign.ALIGN_CENTER);
		_lstSoldiers.SetHighContrast(true);
		_lstSoldiers.SetWordWrap(true);

		int row = 0;
		foreach (BattleUnit bu in _game.SavedGame.SavedBattle.Units)
		{
			if (bu.OriginalFaction == UnitFaction.FACTION_PLAYER && !bu.IsOut)
			{
				if (bu.GeoscapeSoldier != null && !bu.HasGainedAnyExperience)
				{
					_lstSoldiers.AddRow(1, bu.GetName(_game.Language));
					if (row % 2 != 0)
					{
						_lstSoldiers.SetRowColor(row, _lstSoldiers.SecondaryColor);
					}
					++row;
				}
			}
		}
	}

	/// <summary>Returns to the previous screen.</summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}
}
