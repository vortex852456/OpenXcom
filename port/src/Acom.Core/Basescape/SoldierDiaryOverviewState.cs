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
// Ported from: src/Basescape/SoldierDiaryOverviewState.h, src/Basescape/SoldierDiaryOverviewState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)
#pragma warning disable IDE0060 // handler signatures keep the unused Action parameter like C++

/// <summary>
/// Diary screen that shows all the
/// missions a soldier has.
/// </summary>
public class SoldierDiaryOverviewState : State
{
	private readonly Base _base;
	private int _soldierId; // C++ size_t
	private readonly SoldierInfoState _soldierInfoState;
	private Soldier _soldier;
	private readonly List<Soldier> _list;
	private readonly TextButton _btnOk, _btnPrev, _btnNext, _btnKills, _btnMissions, _btnCommendations;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtMission, _txtRating, _txtDate, _txtDeathTitle, _txtDeathInfo, _txtDeathDate;
	private readonly TextList _lstDiary;
	private bool _doNotReset;

	/// <summary>
	/// Initializes all the elements in the Soldier Diary screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="soldierId">ID of the selected soldier.</param>
	/// <param name="soldierInfoState">Pointer to the Soldier Info screen.</param>
	public SoldierDiaryOverviewState(Base @base, int soldierId, SoldierInfoState soldierInfoState)
	{
		_base = @base;
		_soldierId = soldierId;
		_soldierInfoState = soldierInfoState;
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
		_btnKills = new TextButton(70, 16, 8, 176);
		_btnMissions = new TextButton(70, 16, 86, 176);
		_btnCommendations = new TextButton(70, 16, 164, 176);
		_btnOk = new TextButton(70, 16, 242, 176);
		_btnPrev = new TextButton(28, 14, 8, 8);
		_btnNext = new TextButton(28, 14, 284, 8);
		_txtTitle = new Text(310, 16, 5, 8);
		if (_base == null)
		{
			_txtDeathTitle = new Text(150, 9, 16, 36);
			_txtDeathDate = new Text(90, 9, 218, 36);
			_txtDeathInfo = new Text(292, 9, 16, 44);
		}
		int offset = (_base == null) ? 24 : 0;
		_txtMission = new Text(114, 9, 16, 36 + offset);
		_txtRating = new Text(102, 9, 120, 36 + offset);
		_txtDate = new Text(90, 9, 218, 36 + offset);
		_lstDiary = new TextList(288, 120 - offset, 8, 44 + offset);

		// Set palette
		SetInterface("soldierDiary");

		Add(_window, "window", "soldierDiary");
		Add(_btnOk, "button", "soldierDiary");
		Add(_btnKills, "button", "soldierDiary");
		Add(_btnMissions, "button", "soldierDiary");
		Add(_btnCommendations, "button", "soldierDiary");
		Add(_btnPrev, "button", "soldierDiary");
		Add(_btnNext, "button", "soldierDiary");
		Add(_txtTitle, "text1", "soldierDiary");
		if (_base == null)
		{
			Add(_txtDeathTitle, "text2", "soldierDiary");
			Add(_txtDeathInfo, "text3", "soldierDiary");
			Add(_txtDeathDate, "text3", "soldierDiary");
		}
		Add(_txtMission, "text2", "soldierDiary");
		Add(_txtRating, "text2", "soldierDiary");
		Add(_txtDate, "text2", "soldierDiary");
		Add(_lstDiary, "list", "soldierDiary");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "soldierDiary");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_btnKills.Text = Tr("STR_COMBAT");
		_btnKills.OnMouseClick(BtnKillsClick);

		_btnMissions.Text = Tr("STR_PERFORMANCE");
		_btnMissions.OnMouseClick(BtnMissionsClick);

		_btnCommendations.Text = Tr("STR_AWARDS");
		_btnCommendations.OnMouseClick(BtnCommendationsClick);
		_btnCommendations.Visible = _game.Mod.CommendationsList.Count != 0;

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

		_txtMission.SetText(Tr("STR_MISSION"));

		_txtRating.SetText(Tr("STR_RATING_UC"));

		_txtDate.SetText(Tr("STR_DATE_UC"));

		_lstDiary.SetColumns(5, 104, 98, 30, 25, 35);
		_lstDiary.SetSelectable(true);
		_lstDiary.SetBackground(_window);
		_lstDiary.Margin = 8;
		_lstDiary.OnMouseClick(LstDiaryInfoClick);
	}

	/// <summary>
	///  Clears all the variables and reinitializes the list of medals for the soldier.
	/// </summary>
	public override void Init()
	{
		base.Init();

		// coming back from SoldierDiaryMissionState
		if (_doNotReset)
		{
			_doNotReset = false;
			return;
		}

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
		_txtTitle.SetText(_soldier.GetName());

		if (_base == null && _soldier.Death != null)
		{
			string deathTitleText, deathInfoText = "";
			BattleUnitKills cause = _soldier.Death.Cause;
			if (cause != null)
			{
				deathTitleText = _game.Language.GetString("STR_KILLED_IN_ACTION", _soldier.Gender);
				deathInfoText = Tr("STR_KILLER_AND_WEAPON")
					.Arg(cause.GetUnitName(_game.Language))
					.Arg(Tr(cause.weapon));
			}
			else
			{
				deathTitleText = _game.Language.GetString("STR_MISSING_IN_ACTION", _soldier.Gender);
			}

			GameTime t = _soldier.Death.Time;
			string deathDateText = t.GetDayString(_game.Language) + " " + _game.Language.GetString(t.MonthString) + " " + t.Year.ToString(CultureInfo.InvariantCulture);

			_txtDeathTitle.SetText(deathTitleText);
			_txtDeathDate.SetText(deathDateText);
			_txtDeathInfo.SetText(deathInfoText);
		}

		_lstDiary.ClearList();

		uint row = 0;
		foreach (var missionStats in _game.SavedGame.MissionStatistics)
		{
			int missionId = missionStats.id;
			bool wasOnMission = false;

			// See if this mission is part of the soldier's vector of missions
			foreach (int mId in _soldier.Diary.MissionIdList)
			{
				if (missionId == mId)
				{
					wasOnMission = true;
					break;
				}
			}
			if (!wasOnMission)
			{
				continue;
			}

			_lstDiary.AddRow(5, missionStats.GetMissionName(_game.Language),
				missionStats.GetRatingString(_game.Language),
				missionStats.time.GetDayString(_game.Language),
				Tr(missionStats.time.MonthString),
				missionStats.time.Year.ToString(CultureInfo.InvariantCulture));
			row++;
		}
		if (row > 0 && (uint)_lstDiary.Scroll >= row)
		{
			_lstDiary.ScrollTo(0);
		}
	}

	/// <summary>
	/// Set the soldier's Id.
	/// </summary>
	public void SetSoldierId(int soldierId)
	{
		_soldierId = soldierId;
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_soldierInfoState.SetSoldierId(_soldierId);
		_game.PopState();
	}

	/// <summary>
	/// Shows the kills of the soldier.
	/// </summary>
	public void BtnKillsClick(Action action)
	{
		_game.PushState(new SoldierDiaryPerformanceState(_base, _soldierId, this, SoldierDiaryDisplay.DIARY_KILLS));
	}

	/// <summary>
	/// Shows the mission performance of the soldier.
	/// </summary>
	public void BtnMissionsClick(Action action)
	{
		_game.PushState(new SoldierDiaryPerformanceState(_base, _soldierId, this, SoldierDiaryDisplay.DIARY_MISSIONS));
	}

	/// <summary>
	/// Shows the commendations of the soldier.
	/// </summary>
	public void BtnCommendationsClick(Action action)
	{
		_game.PushState(new SoldierDiaryPerformanceState(_base, _soldierId, this, SoldierDiaryDisplay.DIARY_COMMENDATIONS));
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
	/// Shows the selected soldier's info.
	/// </summary>
	public void LstDiaryInfoClick(Action action)
	{
		int absoluteRowEntry = unchecked((int)_lstDiary.SelectedRow);
		_doNotReset = true;
		_game.PushState(new SoldierDiaryMissionState(_soldier, absoluteRowEntry));
	}
}
