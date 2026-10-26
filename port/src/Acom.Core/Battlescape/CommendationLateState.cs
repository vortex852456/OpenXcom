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
// Ported from: src/Battlescape/CommendationLateState.h, src/Battlescape/CommendationLateState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Medals screen that displays the medals of soldiers lost in service.
/// </summary>
public class CommendationLateState : State
{
	private readonly TextButton _btnOk;
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly TextList _lstSoldiers;
	private readonly List<string> _commendationsNames = [];

	/// <summary>Initializes all the elements in the Medals screen.</summary>
	/// <param name="soldiers">List of soldiers with medals.</param>
	public CommendationLateState(List<Soldier> soldiers)
	{
		ArgumentNullException.ThrowIfNull(soldiers);
		// Create object
		_window = new Window(this, 320, 200, 0, 0);
		_btnOk = new TextButton(288, 16, 16, 176);
		_txtTitle = new Text(300, 16, 10, 8);
		_lstSoldiers = new TextList(288, 128, 8, 32);

		// Set palette
		SetInterface("commendationsLate");

		Add(_window, "window", "commendationsLate");
		Add(_btnOk, "button", "commendationsLate");
		Add(_txtTitle, "text", "commendationsLate");
		Add(_lstSoldiers, "list", "commendationsLate");

		CenterAllSurfaces();

		// Set up object
		SetWindowBackground(_window, "commendationsLate");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_LOST_IN_SERVICE"));

		_lstSoldiers.SetColumns(3, 114, 90, 84);
		_lstSoldiers.SetSelectable(true);
		_lstSoldiers.SetBackground(_window);
		_lstSoldiers.Margin = 8;
		_lstSoldiers.SetFlooding(true);
		_lstSoldiers.OnMouseClick(LstSoldiersMouseClick);

		/***

												LOST IN SERVICE

		SOLDIER NAME, RANK: ___, SCORE: ___, KILLS: ___, NUMBER OF MISSIONS: ___, DAYS WOUNDED: ___, TIMES HIT: ___
		  COMMENDATION
		  COMMENDATION
		  COMMENDATION
		  CAUSE OF DEATH: KILLED BY ALIEN_RACE ALIEN_RANK, USING WEAPON

		***/

		var commendationsList = new List<KeyValuePair<string, RuleCommendations>>(_game.Mod.CommendationsList);
		bool modularCommendation;
		string noun;

		int row = 0;
		// Loop over dead soldiers
		foreach (Soldier soldier in soldiers)
		{
			// Establish some base information
			_lstSoldiers.AddRow(3, soldier.GetName(),
									Tr(soldier.RankString),
									Tr("STR_KILLS").Arg(soldier.Diary.KillTotal));
			_lstSoldiers.SetRowColor(row, _lstSoldiers.SecondaryColor);
			_commendationsNames.Add("");
			row++;

			// Loop over all commendation
			for (int commIter = 0; commIter < commendationsList.Count;)
			{
				string commType = commendationsList[commIter].Key;
				RuleCommendations commRule = commendationsList[commIter].Value;

				string wssCommendation = "";
				modularCommendation = false;
				noun = "noNoun";

				// Loop over soldier's commendation
				foreach (SoldierCommendations soldierComm in soldier.Diary.SoldierCommendations)
				{
					if (soldierComm.Type == commType && soldierComm.IsNew && noun == "noNoun")
					{
						soldierComm.MakeOld();

						if (soldierComm.Noun != "noNoun")
						{
							noun = soldierComm.Noun;
							modularCommendation = true;
						}
						// Decoration level name
						int skipCounter = CommendationState.GetSkipCounter(commRule, soldierComm.DecorationLevelInt);
						// Establish commendation's name
						// Medal name
						wssCommendation += "   ";
						if (modularCommendation)
						{
							wssCommendation += Tr(commType).Arg(Tr(noun));
						}
						else
						{
							wssCommendation += Tr(commType);
						}
						_lstSoldiers.AddRow(3, wssCommendation, "", Tr(soldierComm.GetDecorationLevelName(skipCounter)));
						_commendationsNames.Add(commType);
						row++;
						break;
					}
				} // END SOLDIER COMMS LOOP

				if (noun == "noNoun")
				{
					++commIter;
				}
			} // END COMMS LOOPS
			_lstSoldiers.AddRow(3, "", "", ""); // Separator
			_commendationsNames.Add("");
			row++;
		} // END SOLDIER LOOP
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
