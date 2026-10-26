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
// Ported from: src/Geoscape/MonthlyReportState.h, src/Geoscape/MonthlyReportState.cpp
#nullable disable

using System.Text;

namespace Acom;

/// <summary>
/// Report screen shown monthly to display
/// changes in the player's performance and funding.
/// </summary>
public class MonthlyReportState : State
{
	private readonly TextButton _btnOk, _btnBigOk;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtMonth, _txtRating;
	private readonly Text _txtIncome, _txtMaintenance, _txtBalance, _txtBonus;
	private readonly Text _txtDesc, _txtFailure;
	private int _gameOver;
	private int _ratingTotal, _fundingDiff, _lastMonthsRating;
	private readonly List<string> _happyList = [], _sadList = [], _pactList = [], _cancelPactList = [];
#pragma warning disable IDE0052 // kept like the C++ member
	private readonly Globe _globe;
#pragma warning restore IDE0052
	private readonly List<Soldier> _soldiersMedalled = [];

	/// <summary>Monthly rating total (xcom score minus alien score) computed by <see cref="CalculateChanges"/>.</summary>
	internal int RatingTotal => _ratingTotal;

	/// <summary>Funding change of all countries this month.</summary>
	internal int FundingDiff => _fundingDiff;

	/// <summary>Rating of the previous month.</summary>
	internal int LastMonthsRating => _lastMonthsRating;

	/// <summary>0 = no game over, 1 = lost by rating, 2 = lost by debts.</summary>
	internal int GameOver => _gameOver;

	/// <summary>
	/// Initializes all the elements in the Monthly Report screen.
	/// </summary>
	/// <param name="globe">Pointer to the globe.</param>
	public MonthlyReportState(Globe globe)
	{
		_gameOver = 0;
		_ratingTotal = 0;
		_fundingDiff = 0;
		_lastMonthsRating = 0;
		_globe = globe;
		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnOk = new TextButton(50, 12, 135, 180);
		_btnBigOk = new TextButton(120, 18, 100, 174);
		_txtTitle = new Text(300, 17, 16, 8);
		_txtMonth = new Text(130, 9, 16, 24);
		_txtRating = new Text(160, 9, 146, 24);
		_txtIncome = new Text(300, 9, 16, 32);
		_txtMaintenance = new Text(130, 9, 16, 40);
		_txtBalance = new Text(160, 9, 146, 40);
		_txtBonus = new Text(300, 9, 16, 48);
		_txtDesc = new Text(280, 124, 16, 56);
		_txtFailure = new Text(290, 160, 15, 10);

		// Set palette
		SetInterface("monthlyReport");

		Add(_window, "window", "monthlyReport");
		Add(_btnOk, "button", "monthlyReport");
		Add(_btnBigOk, "button", "monthlyReport");
		Add(_txtTitle, "text1", "monthlyReport");
		Add(_txtMonth, "text1", "monthlyReport");
		Add(_txtRating, "text1", "monthlyReport");
		Add(_txtIncome, "text1", "monthlyReport");
		Add(_txtMaintenance, "text1", "monthlyReport");
		Add(_txtBalance, "text1", "monthlyReport");
		Add(_txtBonus, "text1", "monthlyReport");
		Add(_txtDesc, "text2", "monthlyReport");
		Add(_txtFailure, "text2", "monthlyReport");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "monthlyReport");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_btnBigOk.Text = Tr("STR_OK");
		_btnBigOk.OnMouseClick(BtnOkClick);
		_btnBigOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnBigOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnBigOk.Visible = false;

		_txtTitle.SetBig();
		_txtTitle.SetText(Tr("STR_XCOM_PROJECT_MONTHLY_REPORT"));

		_txtFailure.SetBig();
		_txtFailure.Align = TextHAlign.ALIGN_CENTER;
		_txtFailure.VerticalAlign = TextVAlign.ALIGN_MIDDLE;
		_txtFailure.SetWordWrap(true);
		_txtFailure.SetText(Tr("STR_YOU_HAVE_FAILED"));
		_txtFailure.Visible = false;

		CalculateChanges();

		int month = _game.SavedGame.Time.Month - 1, year = _game.SavedGame.Time.Year;
		if (month == 0)
		{
			month = 12;
			year--;
		}
		string m = month switch
		{
			1 => "STR_JAN",
			2 => "STR_FEB",
			3 => "STR_MAR",
			4 => "STR_APR",
			5 => "STR_MAY",
			6 => "STR_JUN",
			7 => "STR_JUL",
			8 => "STR_AUG",
			9 => "STR_SEP",
			10 => "STR_OCT",
			11 => "STR_NOV",
			12 => "STR_DEC",
			_ => "",
		};
		_txtMonth.SetText(Tr("STR_MONTH").Arg(Tr(m)).Arg(year));

		// Calculate rating
		int difficulty_threshold = DifficultyThreshold(_game.Mod, _game.SavedGame);
		string ratingId = RatingId(_ratingTotal, difficulty_threshold, _game.Mod.MonthlyRatings);
		string rating = ratingId.Length == 0 ? "" : Tr(ratingId);

		_txtRating.SetText(Tr("STR_MONTHLY_RATING").Arg(_ratingTotal).Arg(rating));

		var ss = new StringBuilder();
		ss.Append((string)Tr("STR_INCOME")).Append("> ").Append(Unicode.TOK_COLOR_FLIP).Append(Unicode.FormatFunding(_game.SavedGame.CountryFunding));
		ss.Append(" (");
		if (_fundingDiff > 0)
			ss.Append('+');
		ss.Append(Unicode.FormatFunding(_fundingDiff)).Append(')');
		_txtIncome.SetText(ss.ToString());

		_txtMaintenance.SetText((string)Tr("STR_MAINTENANCE") + "> " + Unicode.TOK_COLOR_FLIP + Unicode.FormatFunding(_game.SavedGame.BaseMaintenance));

		int performanceBonus = _game.Mod.GetPerformanceBonus(_ratingTotal);
		if (performanceBonus > 0)
		{
			// increase funds by performance bonus
			_game.SavedGame.Funds = _game.SavedGame.Funds + performanceBonus;
			// display
			_txtBonus.SetText((string)Tr("STR_PERFORMANCE_BONUS") + "> " + Unicode.TOK_COLOR_FLIP + Unicode.FormatFunding(performanceBonus));
			// shuffle the fields a bit for better overview
			int upper = _txtMaintenance.Y;
			int lower = _txtBonus.Y;
			_txtMaintenance.Y = lower;
			_txtBalance.Y = lower;
			_txtBonus.Y = upper;
		}
		else
		{
			// vanilla view
			_txtBonus.Visible = false;
			_txtDesc.Y = _txtBonus.Y;
		}

		_txtBalance.SetText((string)Tr("STR_BALANCE") + "> " + Unicode.TOK_COLOR_FLIP + Unicode.FormatFunding(_game.SavedGame.Funds));

		_txtDesc.SetWordWrap(true);
		_txtDesc.SetScrollable(true);

		// calculate satisfaction
		var ss5 = new StringBuilder();
		string satisFactionString = Tr("STR_COUNCIL_IS_DISSATISFIED");
		bool resetWarning = true;
		if (_ratingTotal > difficulty_threshold)
		{
			satisFactionString = Tr("STR_COUNCIL_IS_GENERALLY_SATISFIED");
		}
		if (_ratingTotal > 500)
		{
			satisFactionString = Tr("STR_COUNCIL_IS_VERY_PLEASED");
		}
		if (_lastMonthsRating <= difficulty_threshold && _ratingTotal <= difficulty_threshold)
		{
			satisFactionString = Tr("STR_YOU_HAVE_NOT_SUCCEEDED");
			_pactList.Clear();
			_cancelPactList.Clear();
			_happyList.Clear();
			_sadList.Clear();
			_gameOver = 1;
		}

		ss5.Append(satisFactionString);

		if (_gameOver == 0)
		{
			if (_game.SavedGame.Funds <= _game.Mod.DefeatFunds)
			{
				if (_game.SavedGame.Warned)
				{
					ss5.Clear();
					ss5.Append((string)Tr("STR_YOU_HAVE_NOT_SUCCEEDED"));
					_pactList.Clear();
					_cancelPactList.Clear();
					_happyList.Clear();
					_sadList.Clear();
					_gameOver = 2;
				}
				else
				{
					ss5.Append("\n\n").Append((string)Tr("STR_COUNCIL_REDUCE_DEBTS"));
					_game.SavedGame.Warned = true;
					resetWarning = false;
				}
			}
		}
		if (resetWarning && _game.SavedGame.Warned)
		{
			_game.SavedGame.Warned = false;
		}

		ss5.Append(CountryList(_happyList, "STR_COUNTRY_IS_PARTICULARLY_PLEASED", "STR_COUNTRIES_ARE_PARTICULARLY_HAPPY"));
		ss5.Append(CountryList(_sadList, "STR_COUNTRY_IS_UNHAPPY_WITH_YOUR_ABILITY", "STR_COUNTRIES_ARE_UNHAPPY_WITH_YOUR_ABILITY"));
		ss5.Append(CountryList(_pactList, "STR_COUNTRY_HAS_SIGNED_A_SECRET_PACT", "STR_COUNTRIES_HAVE_SIGNED_A_SECRET_PACT"));
		ss5.Append(CountryList(_cancelPactList, "STR_COUNTRY_HAS_CANCELLED_A_SECRET_PACT", "STR_COUNTRIES_HAVE_CANCELLED_A_SECRET_PACT"));

		_txtDesc.SetText(ss5.ToString());

		// Give modders some handles on political situation
		foreach (var traitorName in _pactList)
		{
			var traitor = _game.Mod.GetCountry(traitorName, false);
			if (traitor != null)
			{
				_game.SavedGame.SpawnEvent(traitor.SignedPactEvent);
			}
		}
		foreach (var exTraitorName in _cancelPactList)
		{
			var exTraitor = _game.Mod.GetCountry(exTraitorName, false);
			if (exTraitor != null)
			{
				_game.SavedGame.SpawnEvent(exTraitor.RejoinedXcomEvent);
			}
		}
	}

	/// <summary>
	/// Score threshold below which the council is dissatisfied (defeat score plus difficulty, or the mod's custom value).
	/// </summary>
	internal static int DifficultyThreshold(Mod mod, SavedGame save)
	{
		int difficulty_threshold = mod.DefeatScore + 100 * save.DifficultyCoefficient;
		{
			int diff = (int)save.Difficulty;
			var custom = mod.MonthlyRatingThresholds;
			if (custom.Count > diff)
			{
				// only negative values are allowed!
				if (custom[diff] < 0)
				{
					difficulty_threshold = custom[diff];
				}
			}
		}
		return difficulty_threshold;
	}

	/// <summary>
	/// String ID of the monthly rating for a score ("" if the mod's custom ratings have none at or below the score).
	/// </summary>
	internal static string RatingId(int ratingTotal, int difficultyThreshold, SortedDictionary<int, string> monthlyRatings)
	{
		string rating = "STR_RATING_TERRIBLE";
		if (ratingTotal > difficultyThreshold - 300)
		{
			rating = "STR_RATING_POOR";
		}
		if (ratingTotal > difficultyThreshold)
		{
			rating = "STR_RATING_OK";
		}
		if (ratingTotal > 0)
		{
			rating = "STR_RATING_GOOD";
		}
		if (ratingTotal > 500)
		{
			rating = "STR_RATING_EXCELLENT";
		}

		if (monthlyRatings.Count != 0)
		{
			rating = "";
			int temp = int.MinValue;
			foreach (var pair in monthlyRatings)
			{
				if (pair.Key > temp && pair.Key <= ratingTotal)
				{
					temp = pair.Key;
					rating = pair.Value;
				}
			}
		}
		return rating;
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		if (_gameOver == 0)
		{
			_game.PopState();
			// Award medals for service time
			// Iterate through all your bases
			foreach (var xbase in _game.SavedGame.Bases)
			{
				// Iterate through all your soldiers
				foreach (var soldier in xbase.Soldiers)
				{
					// Award medals to eligible soldiers
					soldier.Diary.AddMonthlyService();
					if (soldier.Diary.ManageCommendations(_game.Mod, _game.SavedGame, soldier))
					{
						_soldiersMedalled.Add(soldier);
					}
				}
			}
			if (_soldiersMedalled.Count != 0)
			{
				_game.PushState(new CommendationState(_soldiersMedalled));
			}

			bool psi = false;
			foreach (var xbase in _game.SavedGame.Bases)
			{
				psi = psi || xbase.AvailablePsiLabs != 0;
			}
			if (psi && !Options.anytimePsiTraining)
			{
				_game.PushState(new PsiTrainingState());
			}
			// Autosave
			if (_game.SavedGame.IsIronman)
			{
				_game.PushState(new SaveGameState(OptionsOrigin.OPT_GEOSCAPE, SaveType.SAVE_IRONMAN, _palette));
			}
			else if (Options.autosave)
			{
				_game.PushState(new SaveGameState(OptionsOrigin.OPT_GEOSCAPE, SaveType.SAVE_AUTO_GEOSCAPE, _palette));
			}
		}
		else
		{
			if (_txtFailure.Visible)
			{
				_game.PopState(); // in case the cutscene is not marked as "game over" (by accident or not) let's return to the geoscape

				string cutsceneId;
				if (_gameOver == 1)
					cutsceneId = _game.Mod.LoseRatingCutscene;
				else
					cutsceneId = _game.Mod.LoseMoneyCutscene;

				RuleVideo videoRule = _game.Mod.GetVideo(cutsceneId, true);
				if (videoRule.LoseGame)
				{
					_game.SavedGame.Ending = GameEnding.END_LOSE;
				}

				_game.PushState(new CutsceneState(cutsceneId));
				if (_game.SavedGame.IsIronman)
				{
					_game.PushState(new SaveGameState(OptionsOrigin.OPT_GEOSCAPE, SaveType.SAVE_IRONMAN, _palette));
				}
			}
			else
			{
				_window.SetColor(unchecked((byte)_game.Mod.GetInterface("monthlyReport").GetElement("window").color2));
				_txtTitle.Visible = false;
				_txtMonth.Visible = false;
				_txtRating.Visible = false;
				_txtIncome.Visible = false;
				_txtMaintenance.Visible = false;
				_txtBalance.Visible = false;
				_txtBonus.Visible = false;
				_txtDesc.Visible = false;
				_btnOk.Visible = false;
				_btnBigOk.Visible = true;
				_txtFailure.Visible = true;
				_game.Mod.PlayMusic("GMLOSE");
			}
		}
	}

	/// <summary>
	/// Update all our activity counters, gather all our scores,
	/// get our countries to make sign pacts, adjust their fundings,
	/// assess their satisfaction, and finally calculate our overall
	/// total score, with thanks to Volutar for the formulas.
	/// </summary>
	public void CalculateChanges()
	{
		// initialize all our variables.
		_lastMonthsRating = 0;
		int xcomSubTotal = 0;
		int xcomTotal = 0;
		int alienTotal = 0;
		int monthOffset = _game.SavedGame.FundsList.Count - 2;
		int lastMonthOffset = _game.SavedGame.FundsList.Count - 3;
		if (lastMonthOffset < 0)
			lastMonthOffset += 2;
		// update activity meters, calculate a total score based on regional activity
		// and gather last month's score
		foreach (var region in _game.SavedGame.Regions)
		{
			region.NewMonth();
			if (region.ActivityXcom.Count > 2)
				_lastMonthsRating += region.ActivityXcom[lastMonthOffset] - region.ActivityAlien[lastMonthOffset];
			xcomSubTotal += region.ActivityXcom[monthOffset];
			alienTotal += region.ActivityAlien[monthOffset];
		}
		// apply research bonus AFTER calculating our total, because this bonus applies to the council ONLY,
		// and shouldn't influence each country's decision.

		// the council is more lenient after the first month
		if (_game.SavedGame.MonthsPassed > 1)
			_game.SavedGame.ResearchScores[monthOffset] += 400;

		xcomTotal = _game.SavedGame.ResearchScores[monthOffset] + xcomSubTotal;

		if (_game.SavedGame.ResearchScores.Count > 2)
			_lastMonthsRating += _game.SavedGame.ResearchScores[lastMonthOffset];

		// now that we have our totals we can send the relevant info to the countries
		// and have them make their decisions weighted on the council's perspective.
		RuleAlienMission infiltration = _game.Mod.GetRandomMission(MissionObjective.OBJECTIVE_INFILTRATION, _game.SavedGame.MonthsPassed);
		int pactScore = 0;
		if (infiltration != null)
		{
			pactScore = infiltration.Points;
		}
		// C++ divides int by size_t: the funding is converted to an unsigned 64-bit value first
		int averageFunding = unchecked((int)((ulong)(long)_game.SavedGame.CountryFunding / (ulong)_game.SavedGame.Countries.Count / 1000 * 1000));
		foreach (var country in _game.SavedGame.Countries)
		{
			// check pact status before and after, because scripting can arbitrarily form/break pacts
			bool wasInPact = country.Pact;

			// determine satisfaction level, sign pacts, adjust funding
			// and update activity meters,
			country.NewMonth(xcomTotal, alienTotal, pactScore, averageFunding, _game.SavedGame);
			// and after they've made their decisions, calculate the difference, and add
			// them to the appropriate lists.
			_fundingDiff += country.Funding[^1] - country.Funding[country.Funding.Count - 2];

			bool isInPact = country.Pact;
			if (!wasInPact && isInPact) // signed a new pact this month
			{
				_pactList.Add(country.Rules.Type);
			}
			else if (wasInPact && !isInPact) // renounced a pact this month
			{
				_cancelPactList.Add(country.Rules.Type);
			}

			switch (country.GetSatisfaction())
			{
				case Country.Satisfaction.UNHAPPY:
					_sadList.Add(country.Rules.Type);
					break;
				case Country.Satisfaction.HAPPY:
					_happyList.Add(country.Rules.Type);
					break;
				default:
					break;
			}
		}
		//calculate total.
		_ratingTotal = xcomTotal - alienTotal;
	}

	/// <summary>
	/// Builds a sentence from a list of countries, adding the appropriate
	/// separators and pluralization.
	/// </summary>
	/// <param name="countries">List of country string IDs.</param>
	/// <param name="singular">String ID to append at the end if the list is singular.</param>
	/// <param name="plural">String ID to append at the end if the list is plural.</param>
	private string CountryList(List<string> countries, string singular, string plural)
	{
		var ss = new StringBuilder();
		if (countries.Count != 0)
		{
			ss.Append("\n\n");
			if (countries.Count == 1)
			{
				ss.Append((string)Tr(singular).Arg(Tr(countries[0])));
			}
			else
			{
				LocalizedText list = Tr(countries[0]);
				int i;
				for (i = 1; i < countries.Count - 1; ++i)
				{
					list = Tr("STR_COUNTRIES_COMMA").Arg(list).Arg(Tr(countries[i]));
				}
				list = Tr("STR_COUNTRIES_AND").Arg(list).Arg(Tr(countries[i]));
				ss.Append((string)Tr(plural).Arg(list));
			}
		}
		return ss.ToString();
	}
}
