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
// Ported from: src/Basescape/SoldierDiaryPerformanceState.h, src/Basescape/SoldierDiaryPerformanceState.cpp
#nullable disable

using System.Globalization;
using System.Runtime.CompilerServices;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)
#pragma warning disable IDE0060 // handler signatures keep the unused Action parameter like C++

/// <summary>What the diary totals screen shows.</summary>
public enum SoldierDiaryDisplay : uint
{
	DIARY_KILLS = 0,
	DIARY_MISSIONS = 1,
	DIARY_COMMENDATIONS = 2,
}

/// <summary>
/// Diary screen that lists soldier totals.
/// </summary>
public class SoldierDiaryPerformanceState : State
{
	private readonly Base _base;
	private int _soldierId; // C++ size_t
	private readonly SoldierDiaryOverviewState _soldierDiaryOverviewState;
	private Soldier _soldier;
	private readonly List<Soldier> _list;
	private readonly TextButton _btnOk, _btnPrev, _btnNext, _btnKills, _btnMissions, _btnCommendations;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtMedalName, _txtMedalLevel, _txtMedalInfo;
	private readonly TextList _lstPerformance, _lstKillTotals, _lstMissionTotals, _lstCommendations;
	private readonly List<string> _commendationsListEntry = [];
	private readonly List<string> _commendationsNames = [];
	private readonly List<(string first, SoldierCommendations second)> _sortedCommendations = [];
	private readonly List<Surface> _commendations = [], _commendationDecorations = [];
	private SoldierDiaryDisplay _display;
	private int _lastScrollPos;
	private readonly StrongBox<TextButton> _group = new();
	private bool _doNotReset;

	private static string Str(int value) => value.ToString(CultureInfo.InvariantCulture);

	/// <summary>
	/// Initializes all the elements in the Soldier Diary Totals screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="soldierId">ID of the selected soldier.</param>
	/// <param name="soldierDiaryState">Pointer to the Soldier Diary screen.</param>
	/// <param name="display">Type of totals to display.</param>
	public SoldierDiaryPerformanceState(Base @base, int soldierId, SoldierDiaryOverviewState soldierDiaryState, SoldierDiaryDisplay display)
	{
		_base = @base;
		_soldierId = soldierId;
		_soldierDiaryOverviewState = soldierDiaryState;
		_display = display;
		_lastScrollPos = 0;
		_doNotReset = false;

		if (_base == null)
		{
			_list = _game.SavedGame.DeadSoldiers;
		}
		else
		{
			_list = _base.Soldiers;
		}

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnPrev = new TextButton(28, 14, 8, 8);
		_btnNext = new TextButton(28, 14, 284, 8);
		_btnKills = new TextButton(70, 16, 8, 176);
		_btnMissions = new TextButton(70, 16, 86, 176);
		_btnCommendations = new TextButton(70, 16, 164, 176);
		_btnOk = new TextButton(70, 16, 242, 176);
		_txtTitle = new Text(310, 16, 5, 8);
		_lstPerformance = new TextList(288, 128, 8, 28);
		_lstKillTotals = new TextList(302, 9, 8, 164);
		_lstMissionTotals = new TextList(302, 9, 8, 164);
		// Commendation stat
		_txtMedalName = new Text(120, 18, 16, 36);
		_txtMedalLevel = new Text(120, 18, 186, 36);
		_txtMedalInfo = new Text(280, 32, 20, 135);
		_lstCommendations = new TextList(240, 80, 48, 52);
		for (int i = 0; i != 10; ++i)
		{
			_commendations.Add(new Surface(31, 8, 16, 52 + 8 * i));
			_commendationDecorations.Add(new Surface(31, 8, 16, 52 + 8 * i));
		}

		// Set palette
		SetInterface("soldierDiaryPerformance");

		Add(_window, "window", "soldierDiaryPerformance");
		Add(_btnOk, "button", "soldierDiaryPerformance");
		Add(_btnKills, "button", "soldierDiaryPerformance");
		Add(_btnMissions, "button", "soldierDiaryPerformance");
		Add(_btnCommendations, "button", "soldierDiaryPerformance");
		Add(_btnPrev, "button", "soldierDiaryPerformance");
		Add(_btnNext, "button", "soldierDiaryPerformance");
		Add(_txtTitle, "text1", "soldierDiaryPerformance");
		Add(_lstPerformance, "list", "soldierDiaryPerformance");
		Add(_lstKillTotals, "text2", "soldierDiaryPerformance");
		Add(_lstMissionTotals, "text2", "soldierDiaryPerformance");
		Add(_txtMedalName, "text2", "soldierDiaryPerformance");
		Add(_txtMedalLevel, "text2", "soldierDiaryPerformance");
		Add(_txtMedalInfo, "text2", "soldierDiaryPerformance");
		Add(_lstCommendations, "list", "soldierDiaryPerformance");
		for (int i = 0; i != 10; ++i)
		{
			Add(_commendations[i]);
			Add(_commendationDecorations[i]);
		}

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "soldierDiaryPerformance");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_btnKills.Text = Tr("STR_COMBAT");
		_btnKills.OnMouseClick(BtnKillsToggle);

		_btnMissions.Text = Tr("STR_PERFORMANCE");
		_btnMissions.OnMouseClick(BtnMissionsToggle);

		_btnCommendations.Text = Tr("STR_AWARDS");
		_btnCommendations.OnMouseClick(BtnCommendationsToggle);

		_btnPrev.Text = "<<";
		if (_base == null)
		{
			_btnPrev.OnMouseClick(BtnNextClick);
			_btnPrev.OnKeyboardPress(BtnNextClick, Options.keyBattlePrevUnit);
		}
		else
		{
			_btnPrev.OnMouseClick(BtnPrevClick);
			_btnPrev.OnKeyboardPress(BtnPrevClick, Options.keyBattlePrevUnit);
		}

		_btnNext.Text = ">>";
		if (_base == null)
		{
			_btnNext.OnMouseClick(BtnPrevClick);
			_btnNext.OnKeyboardPress(BtnPrevClick, Options.keyBattleNextUnit);
		}
		else
		{
			_btnNext.OnMouseClick(BtnNextClick);
			_btnNext.OnKeyboardPress(BtnNextClick, Options.keyBattleNextUnit);
		}

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;

		// Text is decided in Init()
		_lstPerformance.SetColumns(2, 273, 15);
		_lstPerformance.SetDot(true);

		_lstKillTotals.SetColumns(4, 72, 72, 72, 86);

		_lstMissionTotals.SetColumns(4, 72, 72, 72, 86);

		_txtMedalName.SetText(Tr("STR_MEDAL_NAME"));

		_txtMedalLevel.SetText(Tr("STR_MEDAL_DECOR_LEVEL"));

		_txtMedalInfo.SetWordWrap(true);

		_lstCommendations.SetColumns(2, 138, 100);
		_lstCommendations.SetSelectable(true);
		_lstCommendations.SetBackground(_window);
		_lstCommendations.OnMouseOver(LstInfoMouseOver);
		_lstCommendations.OnMouseOut(LstInfoMouseOut);
		_lstCommendations.OnMousePress(Handle); // C++: (ActionHandler)&SoldierDiaryPerformanceState::handle
		_lstCommendations.OnMouseClick(LstInfoMouseClick);

		if (_display == SoldierDiaryDisplay.DIARY_KILLS)
		{
			_group.Value = _btnKills;
		}
		else if (_display == SoldierDiaryDisplay.DIARY_MISSIONS)
		{
			_group.Value = _btnMissions;
		}
		else if (_display == SoldierDiaryDisplay.DIARY_COMMENDATIONS)
		{
			_group.Value = _btnCommendations;
		}
		_btnKills.SetGroup(_group);
		_btnMissions.SetGroup(_group);
		_btnCommendations.SetGroup(_group);
	}

	/// <summary>
	///  Clears all the variables and reinitializes the list of kills or missions for the soldier.
	/// </summary>
	public override void Init()
	{
		base.Init();

		// coming back from Ufopedia
		if (_doNotReset)
		{
			_doNotReset = false;
			return;
		}

		// Clear sprites
		for (int i = 0; i != 10; ++i)
		{
			_commendations[i].Clear();
			_commendationDecorations[i].Clear();
		}
		// Reset scroll depth for lists
		_lstPerformance.ScrollTo(0);
		_lstKillTotals.ScrollTo(0);
		_lstMissionTotals.ScrollTo(0);
		_lstCommendations.ScrollTo(0);
		_lastScrollPos = 0;
		_lstPerformance.Visible = _display != SoldierDiaryDisplay.DIARY_COMMENDATIONS;
		// Set visibility for kills
		_lstKillTotals.Visible = _display == SoldierDiaryDisplay.DIARY_KILLS;
		// Set visibility for missions
		_lstMissionTotals.Visible = _display == SoldierDiaryDisplay.DIARY_MISSIONS;
		// Set visibility for commendations
		_txtMedalName.Visible = _display == SoldierDiaryDisplay.DIARY_COMMENDATIONS;
		_txtMedalLevel.Visible = _display == SoldierDiaryDisplay.DIARY_COMMENDATIONS;
		_txtMedalInfo.Visible = _display == SoldierDiaryDisplay.DIARY_COMMENDATIONS;
		_lstCommendations.Visible = _display == SoldierDiaryDisplay.DIARY_COMMENDATIONS;
		_btnCommendations.Visible = _game.Mod.CommendationsList.Count != 0;

		if (_list.Count == 0)
		{
			_game.PopState();
			return;
		}
		if ((uint)_soldierId >= (uint)_list.Count)
		{
			_soldierId = 0;
		}
		_soldier = _list[_soldierId];
		_lstKillTotals.ClearList();
		_lstMissionTotals.ClearList();
		_commendationsListEntry.Clear();
		_commendationsNames.Clear();
		_sortedCommendations.Clear();
		_txtTitle.SetText(_soldier.GetName());
		_lstPerformance.ClearList();
		_lstCommendations.ClearList();
		SoldierDiary diary = _soldier.Diary;
		if (_display == SoldierDiaryDisplay.DIARY_KILLS)
		{
			SortedDictionary<string, int>[] mapArray = [
				diary.WeaponTotal,
				diary.AlienRaceTotal,
				diary.AlienRankTotal
			];
			string[] titleArray = ["STR_NEUTRALIZATIONS_BY_WEAPON", "STR_NEUTRALIZATIONS_BY_RACE", "STR_NEUTRALIZATIONS_BY_RANK"];

			for (int i = 0; i != 3; ++i)
			{
				_lstPerformance.AddRow(1, Tr(titleArray[i]));
				_lstPerformance.SetRowColor(_lstPerformance.LastRowIndex, _lstPerformance.SecondaryColor);
				foreach (var mapItem in mapArray[i])
				{
					_lstPerformance.AddRow(2, Tr(mapItem.Key), Str(mapItem.Value));
				}
				if (i != 2)
				{
					_lstPerformance.AddRow(1, "");
				}
			}

			if (_soldier.CurrentStats.psiSkill > 0 || (Options.psiStrengthEval && _game.SavedGame.IsResearched(_game.Mod.PsiRequirements)))
			{
				_lstKillTotals.AddRow(4, Tr("STR_KILLS").Arg(diary.KillTotal),
					Tr("STR_STUNS").Arg(diary.StunTotal),
					Tr("STR_DIARY_ACCURACY").Arg(diary.Accuracy),
					Tr("STR_MINDCONTROLS").Arg(diary.ControlTotal));
			}
			else
			{
				_lstKillTotals.AddRow(3, Tr("STR_KILLS").Arg(diary.KillTotal),
					Tr("STR_STUNS").Arg(diary.StunTotal),
					Tr("STR_DIARY_ACCURACY").Arg(diary.Accuracy));
			}
		}
		else if (_display == SoldierDiaryDisplay.DIARY_MISSIONS)
		{
			var missionStatistics = _game.SavedGame.MissionStatistics;
			SortedDictionary<string, int>[] mapArray = [
				diary.GetRegionTotal(missionStatistics),
				diary.GetTypeTotal(missionStatistics),
				diary.GetUFOTotal(missionStatistics)
			];
			string[] titleArray = ["STR_MISSIONS_BY_LOCATION", "STR_MISSIONS_BY_TYPE", "STR_MISSIONS_BY_UFO"];

			for (int i = 0; i != 3; ++i)
			{
				_lstPerformance.AddRow(1, Tr(titleArray[i]));
				_lstPerformance.SetRowColor(_lstPerformance.LastRowIndex, _lstPerformance.SecondaryColor);
				foreach (var mapItem in mapArray[i])
				{
					if (mapItem.Key == "NO_UFO") continue;
					_lstPerformance.AddRow(2, Tr(mapItem.Key), Str(mapItem.Value));
				}
				if (i != 2)
				{
					_lstPerformance.AddRow(1, "");
				}
			}

			_lstMissionTotals.AddRow(4, Tr("STR_MISSIONS").Arg(diary.MissionTotal),
				Tr("STR_WINS").Arg(diary.GetWinTotal(missionStatistics)),
				Tr("STR_SCORE_VALUE").Arg(diary.GetScoreTotal(missionStatistics)),
				Tr("STR_DAYS_WOUNDED").Arg(diary.DaysWoundedTotal));
		}
		else if (_display == SoldierDiaryDisplay.DIARY_COMMENDATIONS && _game.Mod.CommendationsList.Count != 0)
		{
			// pre-calc translations
			foreach (var sc in diary.SoldierCommendations)
			{
				if (sc.Noun != "noNoun")
				{
					string tmp = Tr(sc.Type).Arg(Tr(sc.Noun));
					_sortedCommendations.Add((tmp, sc));
				}
				else
				{
					string tmp = Tr(sc.Type);
					_sortedCommendations.Add((tmp, sc));
				}
			}
			// sort by translation
			SoldiersState.StableSort(_sortedCommendations, (lhs, rhs) => Unicode.NaturalCompare(lhs.first, rhs.first));
			// fill the lists
			foreach (var pair in _sortedCommendations)
			{
				RuleCommendations commendation = pair.second.Rule;
				if (pair.second.Noun != "noNoun")
				{
					_lstCommendations.AddRow(2, pair.first, Tr(pair.second.DecorationDescription));
					_commendationsListEntry.Add(Tr(commendation.Description).Arg(Tr(pair.second.Noun)));
				}
				else
				{
					_lstCommendations.AddRow(2, pair.first, Tr(pair.second.DecorationDescription));
					_commendationsListEntry.Add(Tr(commendation.Description));
				}
				_commendationsNames.Add(pair.second.Type);
			}
			DrawSprites();
		}
	}

	/// <summary>
	/// Draws sprites
	/// </summary>
	public void DrawSprites()
	{
		if (_display != SoldierDiaryDisplay.DIARY_COMMENDATIONS) return;

		// Commendation sprites
		SurfaceSet commendationSprite = _game.Mod.GetSurfaceSet("Commendations");
		SurfaceSet commendationDecoration = _game.Mod.GetSurfaceSet("CommendationDecorations");

		// Clear sprites
		for (int i = 0; i != 10; ++i)
		{
			_commendations[i].Clear();
			_commendationDecorations[i].Clear();
		}

		int vectorPosition = 0; // Where we are currently located in the vector
		int scrollDepth = _lstCommendations.Scroll; // So we know where to start

		foreach (var pair in _sortedCommendations)
		{
			RuleCommendations commendation = pair.second.Rule;
			// Skip commendations that are not visible in the textlist
			if (vectorPosition < scrollDepth || vectorPosition - scrollDepth >= _commendations.Count)
			{
				vectorPosition++;
				continue;
			}

			int sprite = commendation.Sprite;
			int decorationSprite = pair.second.DecorationLevelInt;

			// Handle commendation sprites
			commendationSprite.GetFrame(sprite).BlitNShade(_commendations[vectorPosition - scrollDepth], 0, 0);

			// Handle commendation decoration sprites
			if (decorationSprite != 0)
			{
				commendationDecoration.GetFrame(decorationSprite).BlitNShade(_commendationDecorations[vectorPosition - scrollDepth], 0, 0);
			}

			vectorPosition++;
		}
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_soldierDiaryOverviewState.SetSoldierId(_soldierId);
		_game.PopState();
	}

	/// <summary>
	/// Goes to the previous soldier.
	/// </summary>
	public void BtnPrevClick(Action action)
	{
		if (_soldierId == 0)
			_soldierId = _list.Count - 1;
		else
			_soldierId--;
		Init();
	}

	/// <summary>
	/// Goes to the next soldier.
	/// </summary>
	public void BtnNextClick(Action action)
	{
		_soldierId++;
		if ((uint)_soldierId >= (uint)_list.Count)
			_soldierId = 0;
		Init();
	}

	/// <summary>
	/// Display Kills totals.
	/// </summary>
	public void BtnKillsToggle(Action action)
	{
		_display = SoldierDiaryDisplay.DIARY_KILLS;
		Init();
	}

	/// <summary>
	/// Display Missions totals.
	/// </summary>
	public void BtnMissionsToggle(Action action)
	{
		_display = SoldierDiaryDisplay.DIARY_MISSIONS;
		Init();
	}

	/// <summary>
	/// Display Commendations.
	/// </summary>
	public void BtnCommendationsToggle(Action action)
	{
		_display = SoldierDiaryDisplay.DIARY_COMMENDATIONS;
		Init();
	}

	/// <summary>
	/// Shows the Medal information.
	/// </summary>
	public void LstInfoMouseOver(Action action)
	{
		uint sel = _lstCommendations.SelectedRow;

		if (_commendationsListEntry.Count == 0 || sel > (uint)(_commendationsListEntry.Count - 1))
		{
			_txtMedalInfo.SetText("");
		}
		else
		{
			_txtMedalInfo.SetText(_commendationsListEntry[(int)sel]);
		}
	}

	/// <summary>
	///  Clears the Medal information
	/// </summary>
	public void LstInfoMouseOut(Action action)
	{
		_txtMedalInfo.SetText("");
	}

	/// <summary>
	/// Opens the Ufopaedia article of the commendation.
	/// </summary>
	public void LstInfoMouseClick(Action action)
	{
		_doNotReset = true;
		Ufopaedia.OpenArticle(_game, _commendationsNames[(int)_lstCommendations.SelectedRow]);
	}

	/// <summary>
	/// Runs state functionality every cycle.
	/// Used to update sprite vector
	/// </summary>
	public override void Think()
	{
		base.Think();

		if ((uint)_lastScrollPos != (uint)_lstCommendations.Scroll)
		{
			DrawSprites();
			_lastScrollPos = _lstCommendations.Scroll;
		}
	}
}
