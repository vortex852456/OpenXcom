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
// Ported from: src/Menu/StatisticsState.h, src/Menu/StatisticsState.cpp
#nullable disable
#pragma warning disable CA1822 // UI handlers stay instance members like in C++ (State._game is static here)

namespace Acom;

/// <summary>Statistics window that shows up at the end of the game.</summary>
public class StatisticsState : State
{
	private TextButton _btnOk;
	private Window _window;
	private Text _txtTitle;
	private TextList _lstStats;

	/// <summary>Initializes all the elements in the Statistics window.</summary>
	public StatisticsState()
	{
		// Create objects
		_window = new Window(this, 320, 200, 0, 0, WindowPopup.POPUP_BOTH);
		_btnOk = new TextButton(50, 12, 135, 180);
		_txtTitle = new Text(310, 25, 5, 8);
		_lstStats = new TextList(280, 136, 12, 36);

		// Set palette
		SetInterface("endGameStatistics");

		Add(_window, "window", "endGameStatistics");
		Add(_btnOk, "button", "endGameStatistics");
		Add(_txtTitle, "text", "endGameStatistics");
		Add(_lstStats, "list", "endGameStatistics");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "endGameStatistics");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;

		_lstStats.SetColumns(2, 200, 80);
		_lstStats.SetDot(true);

		ListStats();
	}

	/// <summary>Sums a list of numbers.</summary>
	private static int SumVector(List<int> vec)
	{
		int total = 0;
		foreach (var i in vec)
		{
			total += i;
		}
		return total;
	}

	/// <summary>Sums a list of numbers.</summary>
	private static long SumVector(List<long> vec)
	{
		long total = 0;
		foreach (var i in vec)
		{
			total += i;
		}
		return total;
	}

	/// <summary>Gets the save stats.</summary>
	public void ListStats()
	{
		SavedGame save = _game.SavedGame;

		string ss;
		GameTime time = save.Time;
		if (save.Ending == GameEnding.END_WIN)
		{
			ss = Tr("STR_VICTORY");
		}
		else if (save.Ending == GameEnding.END_LOSE)
		{
			ss = Tr("STR_DEFEAT");
		}
		else
		{
			ss = Tr("STR_STATISTICS");
		}
		ss += Unicode.TOK_NL_SMALL + time.GetDayString(_game.Language) + " " + Tr(time.MonthString) + " " + time.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);
		_txtTitle.SetText(ss);

		int totalScore = SumVector(save.ResearchScores);
		foreach (var region in save.Regions)
		{
			totalScore += SumVector(region.ActivityXcom) - SumVector(region.ActivityAlien);
		}

		int monthlyScore = totalScore / save.ResearchScores.Count;
		long totalIncome = SumVector(save.Incomes);
		long totalExpenses = SumVector(save.Expenditures);

		int alienBasesDestroyed = 0, xcomBasesLost = 0;
		int missionsWin = 0, missionsLoss = 0, nightMissions = 0;
		int bestScore = -9999, worstScore = 9999;
		foreach (var ms in save.MissionStatistics)
		{
			if (ms.success)
			{
				missionsWin++;
			}
			else
			{
				missionsLoss++;
			}
			bestScore = Math.Max(bestScore, ms.score);
			worstScore = Math.Min(worstScore, ms.score);
			if (ms.IsDarkness(_game.Mod))
			{
				nightMissions++;
			}
			if (ms.IsAlienBase && ms.success)
			{
				alienBasesDestroyed++;
			}
			if (ms.IsBaseDefense && !ms.success)
			{
				xcomBasesLost++;
			}
		}
		// Make sure dummy values aren't left in
		bestScore = (bestScore == -9999) ? 0 : bestScore;
		worstScore = (worstScore == 9999) ? 0 : worstScore;

		var allSoldiers = new List<Soldier>();
		foreach (var xbase in save.Bases)
		{
			allSoldiers.AddRange(xbase.Soldiers);
		}
		allSoldiers.AddRange(save.DeadSoldiers);
		int soldiersRecruited = allSoldiers.Count;
		int soldiersLost = save.DeadSoldiers.Count;
		int vehiclesLost = save.VehiclesLost;
		int craftLostDogfight = save.CraftLostDogfight;
		int craftLostMission = save.CraftLostMission;

		int aliensKilled = 0, aliensCaptured = 0, friendlyKills = 0;
		int daysWounded = 0, longestMonths = 0;
		int shotsFired = 0, shotsLanded = 0;
		var weaponKills = new SortedDictionary<string, int>(StringComparer.Ordinal);
		var alienKills = new SortedDictionary<string, int>(StringComparer.Ordinal);
		foreach (var soldier in allSoldiers)
		{
			SoldierDiary diary = soldier.Diary;
			aliensKilled += diary.KillTotal;
			aliensCaptured += diary.StunTotal;
			daysWounded += diary.DaysWoundedTotal;
			longestMonths = Math.Max(longestMonths, diary.MonthsService);
			var weaponTotal = diary.WeaponTotal;
			shotsFired += diary.ShotsFiredTotal;
			shotsLanded += diary.ShotsLandedTotal;
			foreach (var pair in weaponTotal)
			{
				if (!weaponKills.ContainsKey(pair.Key))
				{
					weaponKills[pair.Key] = pair.Value;
				}
				else
				{
					weaponKills[pair.Key] += pair.Value;
				}
			}

			if (soldier.Death != null && soldier.Death.Cause != null)
			{
				BattleUnitKills kills = soldier.Death.Cause;
				if (kills.faction == UnitFaction.FACTION_PLAYER)
				{
					friendlyKills++;
				}
				if (!string.IsNullOrEmpty(kills.race))
				{
					if (!alienKills.ContainsKey(kills.race))
					{
						alienKills[kills.race] = 1;
					}
					else
					{
						alienKills[kills.race] += 1;
					}
				}
			}
		}
		int accuracy = 0;
		if (shotsFired > 0)
		{
			accuracy = 100 * shotsLanded / shotsFired;
		}

		int maxWeapon = 0;
		string highestWeapon = "STR_NONE";
		foreach (var pair in weaponKills)
		{
			if (pair.Value > maxWeapon)
			{
				maxWeapon = pair.Value;
				highestWeapon = pair.Key;
			}
		}
		int maxAlien = 0;
		string highestAlien = "STR_NONE";
		foreach (var pair in alienKills)
		{
			if (pair.Value > maxAlien)
			{
				maxAlien = pair.Value;
				highestAlien = pair.Key;
			}
		}

		// C++ copies the map and reads missing ids as 0 (operator[])
		var ids = save.AllIds;
		int alienBases = alienBasesDestroyed;
		foreach (var ab in save.AlienBases)
		{
			if (ab.IsDiscovered)
			{
				alienBases++;
			}
		}
		int ufosDetected = Math.Max(0, ids.GetValueOrDefault("STR_UFO") - 1);
		int terrorSites = Math.Max(0, ids.GetValueOrDefault("STR_TERROR_SITE") - 1);
		int totalCrafts = 0;
		foreach (var craftType in _game.Mod.CraftsList)
		{
			totalCrafts += Math.Max(0, ids.GetValueOrDefault(craftType) - 1);
		}

		int xcomBases = save.Bases.Count + xcomBasesLost;
		int currentScientists = 0, currentEngineers = 0;
		foreach (var xbase in save.Bases)
		{
			currentScientists += xbase.TotalScientists;
			currentEngineers += xbase.TotalEngineers;
		}

		int countriesLost = 0;
		foreach (var country in save.Countries)
		{
			if (country.Pact)
			{
				countriesLost++;
			}
		}

		int researchDone = save.DiscoveredResearch.Count;

		string[] difficulty = ["STR_1_BEGINNER", "STR_2_EXPERIENCED", "STR_3_VETERAN", "STR_4_GENIUS", "STR_5_SUPERHUMAN"];

		_lstStats.AddRow(2, Tr("STR_DIFFICULTY"), Tr(difficulty[(int)save.Difficulty]));
		_lstStats.AddRow(2, Tr("STR_AVERAGE_MONTHLY_RATING"), Unicode.FormatNumber(monthlyScore));
		_lstStats.AddRow(2, Tr("STR_TOTAL_INCOME"), Unicode.FormatFunding(totalIncome));
		_lstStats.AddRow(2, Tr("STR_TOTAL_EXPENDITURE"), Unicode.FormatFunding(totalExpenses));
		if (Options.soldierDiaries)
		{
			_lstStats.AddRow(2, Tr("STR_MISSIONS_WON"), Unicode.FormatNumber(missionsWin));
			_lstStats.AddRow(2, Tr("STR_MISSIONS_LOST"), Unicode.FormatNumber(missionsLoss));
			_lstStats.AddRow(2, Tr("STR_NIGHT_MISSIONS"), Unicode.FormatNumber(nightMissions));
			_lstStats.AddRow(2, Tr("STR_BEST_RATING"), Unicode.FormatNumber(bestScore));
			_lstStats.AddRow(2, Tr("STR_WORST_RATING"), Unicode.FormatNumber(worstScore));
		}
		_lstStats.AddRow(2, Tr("STR_SOLDIERS_RECRUITED"), Unicode.FormatNumber(soldiersRecruited));
		_lstStats.AddRow(2, Tr("STR_SOLDIERS_LOST"), Unicode.FormatNumber(soldiersLost));
		_lstStats.AddRow(2, Tr("STR_VEHICLES_LOST"), Unicode.FormatNumber(vehiclesLost)); // "STR_TANKS_DESTROYED" from DebriefingState
		if (Options.soldierDiaries)
		{
			_lstStats.AddRow(2, Tr("STR_ALIEN_KILLS"), Unicode.FormatNumber(aliensKilled));
			_lstStats.AddRow(2, Tr("STR_ALIEN_CAPTURES"), Unicode.FormatNumber(aliensCaptured));
			_lstStats.AddRow(2, Tr("STR_FRIENDLY_KILLS"), Unicode.FormatNumber(friendlyKills));
			_lstStats.AddRow(2, Tr("STR_AVERAGE_ACCURACY"), Unicode.FormatPercentage(accuracy));
			_lstStats.AddRow(2, Tr("STR_WEAPON_MOST_KILLS"), Tr(highestWeapon));
			_lstStats.AddRow(2, Tr("STR_ALIEN_MOST_KILLS"), Tr(highestAlien));
			_lstStats.AddRow(2, Tr("STR_LONGEST_SERVICE"), Unicode.FormatNumber(longestMonths));
			_lstStats.AddRow(2, Tr("STR_TOTAL_DAYS_WOUNDED"), Unicode.FormatNumber(daysWounded));
		}
		_lstStats.AddRow(2, Tr("STR_TOTAL_UFOS"), Unicode.FormatNumber(ufosDetected));
		if (Options.soldierDiaries)
		{
			_lstStats.AddRow(2, Tr("STR_TOTAL_ALIEN_BASES"), Unicode.FormatNumber(alienBases));
			_lstStats.AddRow(2, Tr("STR_ALIEN_BASES_DESTROYED"), Unicode.FormatNumber(alienBasesDestroyed));
		}
		_lstStats.AddRow(2, Tr("STR_COUNTRIES_LOST"), Unicode.FormatNumber(countriesLost));
		_lstStats.AddRow(2, Tr("STR_TOTAL_TERROR_SITES"), Unicode.FormatNumber(terrorSites));
		if (Options.soldierDiaries)
		{
			_lstStats.AddRow(2, Tr("STR_TOTAL_BASES"), Unicode.FormatNumber(xcomBases));
			_lstStats.AddRow(2, Tr("STR_XCOM_BASES_LOST"), Unicode.FormatNumber(xcomBasesLost));
		}
		_lstStats.AddRow(2, Tr("STR_TOTAL_CRAFT"), Unicode.FormatNumber(totalCrafts));
		_lstStats.AddRow(2, Tr("STR_CRAFT_LOST_DOGFIGHT"), Unicode.FormatNumber(craftLostDogfight));
		_lstStats.AddRow(2, Tr("STR_CRAFT_LOST_MISSION"), Unicode.FormatNumber(craftLostMission));
		_lstStats.AddRow(2, Tr("STR_TOTAL_SCIENTISTS"), Unicode.FormatNumber(currentScientists));
		_lstStats.AddRow(2, Tr("STR_TOTAL_ENGINEERS"), Unicode.FormatNumber(currentEngineers));
		_lstStats.AddRow(2, Tr("STR_TOTAL_RESEARCH"), Unicode.FormatNumber(researchDone));
	}

	/// <summary>Returns to the previous screen.</summary>
	public void BtnOkClick(Action action)
	{
		if (_game.SavedGame.Ending == GameEnding.END_NONE)
		{
			_game.PopState();
		}
		else
		{
			_game.SavedGame = null;
			_game.SetState(new GoToMainMenuState());
		}
	}
}
