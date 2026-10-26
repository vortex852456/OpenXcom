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
// Ported from: src/Battlescape/CommendationState.h, src/Battlescape/CommendationState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Medals screen that displays new soldier medals.
/// </summary>
public class CommendationState : State
{
	private readonly TextButton _btnOk;
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly TextList _lstSoldiers;
	private readonly List<string> _commendationsNames = [];

	/// <summary>Initializes all the elements in the Medals screen.</summary>
	/// <param name="soldiers">List of soldiers with medals.</param>
	public CommendationState(List<Soldier> soldiers)
	{
		ArgumentNullException.ThrowIfNull(soldiers);
		// Create object
		_window = new Window(this, 320, 200, 0, 0);
		_btnOk = new TextButton(288, 16, 16, 176);
		_txtTitle = new Text(300, 16, 10, 8);
		_lstSoldiers = new TextList(288, 128, 8, 32);

		// Set palette
		SetInterface("commendations");

		Add(_window, "window", "commendations");
		Add(_btnOk, "button", "commendations");
		Add(_txtTitle, "heading", "commendations");
		Add(_lstSoldiers, "list", "commendations");

		CenterAllSurfaces();

		// Set up object
		SetWindowBackground(_window, "commendations");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtTitle.SetText(Tr("STR_MEDALS"));
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetBig();

		_lstSoldiers.SetColumns(2, 204, 84);
		_lstSoldiers.SetSelectable(true);
		_lstSoldiers.SetBackground(_window);
		_lstSoldiers.Margin = 8;
		_lstSoldiers.OnMouseClick(LstSoldiersMouseClick);

		int row = 0;
		int titleRow = 0;
		var commendationsList = new List<KeyValuePair<string, RuleCommendations>>(_game.Mod.CommendationsList);
		bool modularCommendation;
		string noun;
		bool titleChosen = true;

		for (int commIter = 0; commIter < commendationsList.Count;)
		{
			string commType = commendationsList[commIter].Key;
			RuleCommendations commRule = commendationsList[commIter].Value;

			modularCommendation = false;
			noun = "noNoun";
			if (titleChosen)
			{
				_lstSoldiers.AddRow(2, "", ""); // Blank row, will be filled in later
				_commendationsNames.Add("");
				row++;
			}
			titleChosen = false;
			titleRow = row - 1;

			foreach (Soldier soldier in soldiers)
			{
				foreach (SoldierCommendations soldierComm in soldier.Diary.SoldierCommendations)
				{
					if (soldierComm.Type == commType && soldierComm.IsNew && noun == "noNoun")
					{
						soldierComm.MakeOld();
						row++;

						if (soldierComm.Noun != "noNoun")
						{
							noun = soldierComm.Noun;
							modularCommendation = true;
						}

						// Soldier name
						string wssName = "   " + soldier.GetName();
						// Decoration level name
						int skipCounter = GetSkipCounter(commRule, soldierComm.DecorationLevelInt);
						_lstSoldiers.AddRow(2, wssName, Tr(soldierComm.GetDecorationLevelName(skipCounter)));
						_commendationsNames.Add("");
						break;
					}
				}
			}
			if (titleRow != row - 1)
			{
				// Medal name
				if (modularCommendation)
				{
					_lstSoldiers.SetCellText(titleRow, 0, Tr(commType).Arg(Tr(noun)));
				}
				else
				{
					_lstSoldiers.SetCellText(titleRow, 0, Tr(commType));
				}
				_lstSoldiers.SetRowColor(titleRow, _lstSoldiers.SecondaryColor);
				_commendationsNames[titleRow] = commType;
				titleChosen = true;
			}
			if (noun == "noNoun")
			{
				++commIter;
			}
		}
	}

	/// <summary>
	/// Counts the repeated thresholds of the commendation's first criteria up to the given decoration level
	/// (the "skipCounter" loop shared by the commendation screens).
	/// </summary>
	internal static int GetSkipCounter(RuleCommendations commRule, int decorationLevel)
	{
		ArgumentNullException.ThrowIfNull(commRule);
		List<int> criteria = null;
		foreach (var pair in commRule.Criteria)
		{
			criteria = pair.Value; // commRule->getCriteria()->begin()->second
			break;
		}
		return GetSkipCounter(criteria, decorationLevel);
	}

	/// <summary>Counts the repeated thresholds of a criteria list up to the given decoration level.</summary>
	internal static int GetSkipCounter(List<int> criteria, int decorationLevel)
	{
		ArgumentNullException.ThrowIfNull(criteria);
		int skipCounter = 0;
		int lastInt = -2;
		int thisInt = -1;
		int vectorIterator = 0;
		for (int wtf = 0; wtf < criteria.Count; ++wtf)
		{
			if (vectorIterator == decorationLevel + 1)
			{
				break;
			}
			thisInt = criteria[wtf];
			if (wtf != 0)
			{
				lastInt = criteria[wtf - 1];
			}
			if (thisInt == lastInt)
			{
				skipCounter++;
			}
			vectorIterator++;
		}
		return skipCounter;
	}

	/// <summary>Opens the Ufopaedia article of the clicked commendation.</summary>
	public void LstSoldiersMouseClick(Action action)
	{
		Ufopaedia.OpenArticle(_game, _commendationsNames[(int)_lstSoldiers.SelectedRow]);
	}

	/// <summary>Returns to the previous screen.</summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}
}
