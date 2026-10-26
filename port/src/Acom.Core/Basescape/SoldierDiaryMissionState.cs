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
// Ported from: src/Basescape/SoldierDiaryMissionState.h, src/Basescape/SoldierDiaryMissionState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)
#pragma warning disable IDE0060 // handler signatures keep the unused Action parameter like C++

/// <summary>
/// Diary window that show
/// mission details for a soldier.
/// </summary>
public class SoldierDiaryMissionState : State
{
	private readonly Soldier _soldier;
	private readonly TextButton _btnOk, _btnPrev, _btnNext;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtUFO, _txtScore, _txtKills, _txtLocation, _txtRace, _txtDaylight, _txtDaysWounded;
	private readonly Text _txtNoRecord;
	private readonly TextList _lstKills;
	private int _rowEntry;

	/// <summary>
	/// Initializes all the elements in the Soldier Diary Mission window.
	/// </summary>
	/// <param name="soldier">Pointer to the selected soldier.</param>
	/// <param name="rowEntry">number to get mission info from.</param>
	public SoldierDiaryMissionState(Soldier soldier, int rowEntry)
	{
		_soldier = soldier;
		_rowEntry = rowEntry;
		_screen = false;

		// Create object
		_window = new Window(this, 300, 128, 10, 36, WindowPopup.POPUP_HORIZONTAL);
		_btnOk = new TextButton(240, 16, 40, 140);
		_btnPrev = new TextButton(28, 14, 18, 44);
		_btnNext = new TextButton(28, 14, 274, 44);
		_txtTitle = new Text(262, 9, 29, 44);
		_txtUFO = new Text(262, 9, 29, 52);
		_txtScore = new Text(180, 9, 29, 68);
		_txtKills = new Text(120, 9, 169, 68);
		_txtLocation = new Text(180, 9, 29, 76);
		_txtRace = new Text(120, 9, 169, 76);
		_txtDaylight = new Text(120, 9, 169, 84);
		_txtDaysWounded = new Text(180, 9, 29, 84);
		_txtNoRecord = new Text(240, 9, 29, 100);
		_lstKills = new TextList(270, 32, 20, 100);

		// Set palette
		SetInterface("soldierDiaryMission");

		Add(_window, "window", "soldierDiaryMission");
		Add(_btnOk, "button", "soldierDiaryMission");
		Add(_btnPrev, "button", "soldierDiaryMission");
		Add(_btnNext, "button", "soldierDiaryMission");
		Add(_txtTitle, "text", "soldierDiaryMission");
		Add(_txtUFO, "text", "soldierDiaryMission");
		Add(_txtScore, "text", "soldierDiaryMission");
		Add(_txtKills, "text", "soldierDiaryMission");
		Add(_txtLocation, "text", "soldierDiaryMission");
		Add(_txtRace, "text", "soldierDiaryMission");
		Add(_txtDaylight, "text", "soldierDiaryMission");
		Add(_txtDaysWounded, "text", "soldierDiaryMission");
		Add(_txtNoRecord, "text", "soldierDiaryMission");
		Add(_lstKills, "list", "soldierDiaryMission");

		CenterAllSurfaces();

		// Set up object
		SetWindowBackground(_window, "soldierDiaryMission");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_btnPrev.Text = "<<";
		_btnPrev.OnMouseClick(BtnPrevClick);
		_btnPrev.OnKeyboardPress(BtnPrevClick, Options.keyBattleNextUnit);

		_btnNext.Text = ">>";
		_btnNext.OnMouseClick(BtnNextClick);
		_btnNext.OnKeyboardPress(BtnNextClick, Options.keyBattlePrevUnit);

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;

		_txtUFO.Align = TextHAlign.ALIGN_CENTER;

		_lstKills.SetColumns(3, 60, 110, 100);
	}

	/// <summary>
	///  Clears all the variables and reinitializes the stats for the mission.
	/// </summary>
	public override void Init()
	{
		base.Init();
		if (_soldier.Diary.MissionIdList.Count == 0)
		{
			_game.PopState();
			return;
		}
		List<MissionStatistics> missionStatistics = _game.SavedGame.MissionStatistics;
		uint missionId = unchecked((uint)_soldier.Diary.MissionIdList[_rowEntry]);
		if (missionId > (uint)missionStatistics.Count)
		{
			missionId = 0;
		}
		MissionStatistics ms = missionStatistics[(int)missionId];

		int daysWounded = 0;
		if (ms.injuryList.TryGetValue(_soldier.Id, out int injury))
		{
			daysWounded = injury;
		}

		_lstKills.ClearList();
		_txtTitle.SetText(Tr(ms.type));
		if (ms.IsUfoMission)
		{
			_txtUFO.SetText(Tr(ms.ufo));
		}
		_txtUFO.Visible = ms.IsUfoMission;
		_txtScore.SetText(Tr("STR_SCORE_VALUE").Arg(ms.score));
		_txtLocation.SetText(Tr("STR_LOCATION").Arg(Tr(ms.LocationString)));
		_txtRace.SetText(Tr("STR_RACE_TYPE").Arg(Tr(ms.alienRace)));
		_txtRace.Visible = ms.alienRace != "STR_UNKNOWN";
		_txtDaylight.SetText(Tr("STR_DAYLIGHT_TYPE").Arg(Tr(ms.GetDaylightString(_game.Mod))));
		_txtDaysWounded.SetText(Tr("STR_DAYS_WOUNDED").Arg(daysWounded));
		_txtDaysWounded.Visible = daysWounded != 0;

		int kills = 0;
		bool stunOrKill = false;

		foreach (var battleUnitKills in _soldier.Diary.Kills)
		{
			if (unchecked((uint)battleUnitKills.mission) != missionId) continue;

			switch (battleUnitKills.status)
			{
				case UnitStatus.STATUS_DEAD:
					kills++;
					stunOrKill = true;
					break;
				case UnitStatus.STATUS_UNCONSCIOUS:
				case UnitStatus.STATUS_PANICKING:
				case UnitStatus.STATUS_TURNING:
					stunOrKill = true;
					break;
				default:
					break;
			}

			_lstKills.AddRow(3, Tr(battleUnitKills.KillStatusString),
				battleUnitKills.GetUnitName(_game.Language),
				Tr(battleUnitKills.weapon));
		}

		_txtNoRecord.Align = TextHAlign.ALIGN_CENTER;
		_txtNoRecord.SetText(Tr("STR_NO_RECORD"));
		_txtNoRecord.Visible = !stunOrKill;

		_txtKills.SetText(Tr("STR_KILLS").Arg(kills));
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Goes to the previous mission.
	/// </summary>
	public void BtnPrevClick(Action action)
	{
		if (_rowEntry == 0)
			_rowEntry = _soldier.Diary.MissionTotal - 1;
		else
			_rowEntry--;
		Init();
	}

	/// <summary>
	/// Goes to the next mission.
	/// </summary>
	public void BtnNextClick(Action action)
	{
		_rowEntry++;
		if (_rowEntry >= _soldier.Diary.MissionTotal)
			_rowEntry = 0;
		Init();
	}
}
