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
// Ported from: src/Geoscape/NewPossibleResearchState.h, src/Geoscape/NewPossibleResearchState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window which inform the player of new possible research projects.
/// Also allow to go to the ResearchState to dispatch available scientist.
/// </summary>
public class NewPossibleResearchState : State
{
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly TextList _lstPossibilities;
	private readonly TextButton _btnResearch, _btnOk;
	private readonly Base _base;

	/// <summary>
	/// Initializes all the elements in the EndResearch screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="possibilities">List of newly possible ResearchProject.</param>
	public NewPossibleResearchState(Base @base, List<RuleResearch> possibilities)
	{
		_base = @base;
		_screen = false;

		// Create objects
		_window = new Window(this, 288, 180, 16, 10);
		_btnOk = new TextButton(160, 14, 80, 149);
		_btnResearch = new TextButton(160, 14, 80, 165);
		_txtTitle = new Text(288, 40, 16, 20);
		_lstPossibilities = new TextList(250, 96, 35, 50);

		// Set palette
		SetInterface("geoResearch");

		Add(_window, "window", "geoResearch");
		Add(_btnOk, "button", "geoResearch");
		Add(_btnResearch, "button", "geoResearch");
		Add(_txtTitle, "text1", "geoResearch");
		Add(_lstPossibilities, "text2", "geoResearch");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "geoResearch");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnResearch.Text = Tr("STR_ALLOCATE_RESEARCH");
		_btnResearch.OnMouseClick(BtnResearchClick);
		_btnResearch.OnKeyboardPress(BtnResearchClick, Options.keyOk);
		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;

		_lstPossibilities.SetColumns(1, 250);
		_lstPossibilities.SetBig();
		_lstPossibilities.SetAlign(TextHAlign.ALIGN_CENTER);
		_lstPossibilities.SetScrolling(true, 0);

		bool foundNew = false;
		foreach (var rule in possibilities)
		{
			// Note: ignore all topics with "requires" (same reason as in NewResearchListState::fillProjectList())
			if (rule.Requirements.Count == 0)
			{
				// Also ignore:
				// 1. things that already popped before
				// 2. things that never popped, but are researched already (can happen for topics that can be researched multiple times)
				if (!_game.SavedGame.WasResearchPopped(rule) && !_game.SavedGame.IsResearched(rule, false))
				{
					_game.SavedGame.AddPoppedResearch(rule);
					_lstPossibilities.AddRow(1, Tr(rule.Name));
					foundNew = true;
				}
			}
		}

		if (foundNew)
		{
			_txtTitle.SetText(Tr("STR_WE_CAN_NOW_RESEARCH"));
		}
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Open the ResearchState so the player can dispatch available scientist.
	/// </summary>
	public void BtnResearchClick(Action action)
	{
		_game.PopState();
		_game.PushState(new ResearchState(_base));
	}
}
