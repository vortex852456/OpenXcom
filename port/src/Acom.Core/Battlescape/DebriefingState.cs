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
// Ported from: src/Battlescape/DebriefingState.h, src/Battlescape/DebriefingState.cpp
#nullable disable

using System.Globalization;
using System.Text;
using static Acom.BattleType;
using static Acom.EscapeType;
using static Acom.SpecialTileType;
using static Acom.UnitFaction;
using static Acom.UnitStatus;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>One line of the debriefing score table.</summary>
public class DebriefingStat
{
	public string item;
	public int qty;
	public int score;
	public bool recovery;

	public DebriefingStat(string _item, bool _recovery)
	{
		item = _item;
		qty = 0;
		score = 0;
		recovery = _recovery;
	}
}

/// <summary>An item that could not be put back on a craft.</summary>
public class ReequipStat
{
	public string item;
	public int qty;
	public string craft;
	public int listOrder;
}

/// <summary>A recoverable map object type (UFO power source, alloys, ...).</summary>
public class RecoveryItem
{
	public string name;
	public int value;
}

/// <summary>
/// Debriefing screen shown after a Battlescape
/// mission that displays the results.
/// </summary>
public class DebriefingState : State
{
	private RuleEvent _eventToSpawn;
	private Region _region;
	private Country _country;
	private Base _base;
	private readonly List<DebriefingStat> _stats = [];
	private readonly List<(string first, UnitStats second)> _soldierStats = [];
	private readonly TextButton _btnOk, _btnStats, _btnSell, _btnTransfer;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtItem, _txtQuantity, _txtScore, _txtRecovery, _txtRating;
	private readonly Text _txtSoldier, _txtTU, _txtStamina, _txtHealth, _txtBravery, _txtReactions;
	private readonly Text _txtFiring, _txtThrowing, _txtMelee, _txtStrength, _txtPsiStrength, _txtPsiSkill;
	private readonly TextList _lstStats, _lstRecovery, _lstTotal, _lstSoldierStats, _lstRecoveredItems;
	private string _currentTooltip = "";
	private readonly Text _txtTooltip;
	private readonly List<ReequipStat> _missingItems = [];
	// C++ std::map keyed by pointer (address order); the port keeps insertion order
	private readonly Dictionary<RuleItem, int> _rounds = [], _roundsPainKiller = [], _roundsStimulant = [], _roundsHeal = [], _recoveredItems = [];
	private readonly byte _ammoColor;
	private readonly SortedDictionary<int, RecoveryItem> _recoveryStats = [];
	private bool _positiveScore, _destroyBase, _promotions, _showSellButton, _initDone;
	private readonly SortedDictionary<int, int> _containmentStateInfo = [];
	private readonly int _limitsEnforced;
	private readonly MissionStatistics _missionStatistics;
	private readonly List<Soldier> _soldiersCommended = [], _deadSoldiersCommended = [];
	/// <summary>0 = score, 1 = stat improvement, 2 = recovered items</summary>
	private int _pageNumber;

	/// <summary>
	/// Initializes all the elements in the Debriefing screen.
	/// </summary>
	public DebriefingState()
	{
		_eventToSpawn = null;
		_region = null;
		_country = null;
		_positiveScore = true;
		_destroyBase = false;
		_promotions = false;
		_showSellButton = true;
		_initDone = false;
		_pageNumber = 0;

		_missionStatistics = new MissionStatistics();

		Options.baseXResolution = Options.baseXGeoscape;
		Options.baseYResolution = Options.baseYGeoscape;
		_game.Screen.ResetDisplay(false);

		// Restore the cursor in case something weird happened
		_game.Cursor.Visible = true;
		_limitsEnforced = Options.storageLimitsEnforced ? 1 : 0;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnOk = new TextButton(40, 12, 16, 180);
		_btnStats = new TextButton(60, 12, 244, 180);
		_btnSell = new TextButton(60, 12, 176, 180);
		_btnTransfer = new TextButton(80, 12, 88, 180);
		_txtTitle = new Text(300, 17, 16, 8);
		_txtItem = new Text(180, 9, 16, 24);
		_txtQuantity = new Text(60, 9, 200, 24);
		_txtScore = new Text(55, 9, 270, 24);
		_txtRecovery = new Text(180, 9, 16, 60);
		_txtRating = new Text(200, 9, 64, 180);
		_lstStats = new TextList(290, 80, 16, 32);
		_lstRecovery = new TextList(290, 80, 16, 32);
		_lstTotal = new TextList(290, 9, 16, 12);

		// Second page (soldier stats)
		_txtSoldier = new Text(90, 9, 16, 24); //16..106 = 90
		_txtTU = new Text(18, 9, 106, 24); //106
		_txtStamina = new Text(18, 9, 124, 24); //124
		_txtHealth = new Text(18, 9, 142, 24); //142
		_txtBravery = new Text(18, 9, 160, 24); //160
		_txtReactions = new Text(18, 9, 178, 24); //178
		_txtFiring = new Text(18, 9, 196, 24); //196
		_txtThrowing = new Text(18, 9, 214, 24); //214
		_txtMelee = new Text(18, 9, 232, 24); //232
		_txtStrength = new Text(18, 9, 250, 24); //250
		_txtPsiStrength = new Text(18, 9, 268, 24); //268
		_txtPsiSkill = new Text(18, 9, 286, 24); //286..304 = 18

		_lstSoldierStats = new TextList(288, 144, 16, 32); // 18 rows

		_txtTooltip = new Text(200, 9, 64, 180);

		// Third page (recovered items)
		_lstRecoveredItems = new TextList(272, 144, 16, 32); // 18 rows

		ApplyVisibility();

		// Set palette
		SetInterface("debriefing");

		_ammoColor = (byte)_game.Mod.GetInterface("debriefing").GetElement("totals").color;

		Add(_window, "window", "debriefing");
		Add(_btnOk, "button", "debriefing");
		Add(_btnStats, "button", "debriefing");
		Add(_btnSell, "button", "debriefing");
		Add(_btnTransfer, "button", "debriefing");
		Add(_txtTitle, "heading", "debriefing");
		Add(_txtItem, "text", "debriefing");
		Add(_txtQuantity, "text", "debriefing");
		Add(_txtScore, "text", "debriefing");
		Add(_txtRecovery, "text", "debriefing");
		Add(_txtRating, "text", "debriefing");
		Add(_lstStats, "list", "debriefing");
		Add(_lstRecovery, "list", "debriefing");
		Add(_lstTotal, "totals", "debriefing");

		Add(_txtSoldier, "text", "debriefing");
		Add(_txtTU, "text", "debriefing");
		Add(_txtStamina, "text", "debriefing");
		Add(_txtHealth, "text", "debriefing");
		Add(_txtBravery, "text", "debriefing");
		Add(_txtReactions, "text", "debriefing");
		Add(_txtFiring, "text", "debriefing");
		Add(_txtThrowing, "text", "debriefing");
		Add(_txtMelee, "text", "debriefing");
		Add(_txtStrength, "text", "debriefing");
		Add(_txtPsiStrength, "text", "debriefing");
		Add(_txtPsiSkill, "text", "debriefing");
		Add(_lstSoldierStats, "list", "debriefing");
		Add(_txtTooltip, "text", "debriefing");

		Add(_lstRecoveredItems, "list", "debriefing");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "debriefing");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_btnStats.OnMouseClick(BtnStatsClick);

		_btnSell.Text = Tr("STR_SELL");
		_btnSell.OnMouseClick(BtnSellClick);
		_btnTransfer.Text = Tr("STR_TRANSFER_UC");
		_btnTransfer.OnMouseClick(BtnTransferClick);

		_txtTitle.SetBig();

		_txtItem.SetText(Tr("STR_LIST_ITEM"));

		_txtQuantity.SetText(Tr("STR_QUANTITY_UC"));
		_txtQuantity.Align = TextHAlign.ALIGN_RIGHT;

		_txtScore.SetText(Tr("STR_SCORE"));

		_lstStats.SetColumns(3, 224, 30, 64);
		_lstStats.SetDot(true);

		_lstRecovery.SetColumns(3, 224, 30, 64);
		_lstRecovery.SetDot(true);

		_lstTotal.SetColumns(2, 254, 64);
		_lstTotal.SetDot(true);

		// Second page
		_txtSoldier.SetText(Tr("STR_NAME_UC"));

		SetupStatHeader(_txtTU, "STR_TIME_UNITS_ABBREVIATION", "STR_TIME_UNITS");
		SetupStatHeader(_txtStamina, "STR_STAMINA_ABBREVIATION", "STR_STAMINA");
		SetupStatHeader(_txtHealth, "STR_HEALTH_ABBREVIATION", "STR_HEALTH");
		SetupStatHeader(_txtBravery, "STR_BRAVERY_ABBREVIATION", "STR_BRAVERY");
		SetupStatHeader(_txtReactions, "STR_REACTIONS_ABBREVIATION", "STR_REACTIONS");
		SetupStatHeader(_txtFiring, "STR_FIRING_ACCURACY_ABBREVIATION", "STR_FIRING_ACCURACY");
		SetupStatHeader(_txtThrowing, "STR_THROWING_ACCURACY_ABBREVIATION", "STR_THROWING_ACCURACY");
		SetupStatHeader(_txtMelee, "STR_MELEE_ACCURACY_ABBREVIATION", "STR_MELEE_ACCURACY");
		SetupStatHeader(_txtStrength, "STR_STRENGTH_ABBREVIATION", "STR_STRENGTH");
		if (_game.Mod.IsManaFeatureEnabled)
		{
			SetupStatHeader(_txtPsiStrength, "STR_MANA_ABBREVIATION", "STR_MANA_POOL");
		}
		else
		{
			SetupStatHeader(_txtPsiStrength, "STR_PSIONIC_STRENGTH_ABBREVIATION", "STR_PSIONIC_STRENGTH");
		}
		SetupStatHeader(_txtPsiSkill, "STR_PSIONIC_SKILL_ABBREVIATION", "STR_PSIONIC_SKILL");

		_lstSoldierStats.SetColumns(13, 90, 18, 18, 18, 18, 18, 18, 18, 18, 18, 18, 18, 0);
		_lstSoldierStats.SetAlign(TextHAlign.ALIGN_CENTER);
		_lstSoldierStats.SetAlign(TextHAlign.ALIGN_LEFT, 0);
		_lstSoldierStats.SetDot(true);

		// Third page
		int firstColumnWidth = Math.Clamp(_game.Mod.GetInterface("debriefing").GetElement("list").custom, 90, 254);
		_lstRecoveredItems.SetColumns(2, firstColumnWidth, 18);
		_lstRecoveredItems.SetAlign(TextHAlign.ALIGN_LEFT);
		_lstRecoveredItems.SetDot(true);
	}

	/// <summary>Same setup sequence as the C++ code repeats for every stat column header.</summary>
	private void SetupStatHeader(Text txt, string text, string tooltip)
	{
		txt.Align = TextHAlign.ALIGN_CENTER;
		txt.SetText(Tr(text));
		txt.Tooltip = tooltip;
		txt.OnMouseIn(TxtTooltipIn);
		txt.OnMouseOut(TxtTooltipOut);
	}

	/// <summary>Creates a string for the soldier stats table from a stat difference value.</summary>
	internal static string MakeSoldierString(int stat)
	{
		if (stat == 0) return "";

		return $"{Unicode.TOK_COLOR_FLIP}+{stat.ToString(CultureInfo.InvariantCulture)}{Unicode.TOK_COLOR_FLIP}";
	}

	/// <summary>Sets the visibility according to the _pageNumber.</summary>
	private void ApplyVisibility()
	{
		bool showScore = _pageNumber == 0;
		bool showStats = _pageNumber == 1;
		bool showItems = _pageNumber == 2;

		// First page (scores)
		_txtItem.Visible = showScore || showItems;
		_txtQuantity.Visible = showScore;
		_txtScore.Visible = showScore;
		_txtRecovery.Visible = showScore;
		_txtRating.Visible = showScore;
		_lstStats.Visible = showScore;
		_lstRecovery.Visible = showScore;
		_lstTotal.Visible = showScore;

		// Second page (soldier stats)
		_txtSoldier.Visible = showStats;
		_txtTU.Visible = showStats;
		_txtStamina.Visible = showStats;
		_txtHealth.Visible = showStats;
		_txtBravery.Visible = showStats;
		_txtReactions.Visible = showStats;
		_txtFiring.Visible = showStats;
		_txtThrowing.Visible = showStats;
		_txtMelee.Visible = showStats;
		_txtStrength.Visible = showStats;
		_txtPsiStrength.Visible = showStats;
		_txtPsiSkill.Visible = showStats;
		_lstSoldierStats.Visible = showStats;
		_txtTooltip.Visible = showStats;

		// Third page (recovered items)
		_lstRecoveredItems.Visible = showItems;

		// Set text on toggle button accordingly
		_btnSell.Visible = showItems && _showSellButton;
		_btnTransfer.Visible = showItems && _showSellButton && _game.SavedGame.Bases.Count > 1;
		if (showScore)
		{
			_btnStats.Text = Tr("STR_STATS");
		}
		else if (showStats)
		{
			_btnStats.Text = Tr("STR_LOOT");
		}
		else if (showItems)
		{
			_btnStats.Text = Tr("STR_SCORE");
		}
	}

	/// <summary>Prepare debriefing.</summary>
	public override void Init()
	{
		base.Init();

		if (_initDone)
		{
			return;
		}
		_initDone = true;

		PrepareDebriefing();

		foreach (var sse in _soldierStats)
		{
			var tmp = sse.second.psiStrength;
			if (_game.Mod.IsManaFeatureEnabled)
			{
				tmp = sse.second.mana;
			}
			_lstSoldierStats.AddRow(13, sse.first,
					MakeSoldierString(sse.second.tu),
					MakeSoldierString(sse.second.stamina),
					MakeSoldierString(sse.second.health),
					MakeSoldierString(sse.second.bravery),
					MakeSoldierString(sse.second.reactions),
					MakeSoldierString(sse.second.firing),
					MakeSoldierString(sse.second.throwing),
					MakeSoldierString(sse.second.melee),
					MakeSoldierString(sse.second.strength),
					MakeSoldierString(tmp),
					MakeSoldierString(sse.second.psiSkill),
					"");
			// note: final dummy element to cause dot filling until the end of the line
		}

		// compare stuff from after and before recovery
		if (_base != null && _showSellButton)
		{
			int row = 0;
			ItemContainer origBaseItems = _game.SavedGame.SavedBattle.BaseStorageItems;
			foreach (string itemType in _game.Mod.ItemsList)
			{
				RuleItem rule = _game.Mod.GetItem(itemType);

				int qty = _base.StorageItems.GetItem(rule);
				if (qty > 0 && (Options.canSellLiveAliens || !rule.IsAlien))
				{
					// IGNORE vehicles and their ammo
					// Note: because their number in base has been messed up by Base::setupDefenses() already in geoscape :(
					if (rule.VehicleUnit != null)
					{
						// if this vehicle requires ammo, remember to ignore it later too
						if (rule.VehicleClipAmmo != null)
						{
							origBaseItems.AddItem(rule.VehicleClipAmmo, 1000000);
						}
						continue;
					}

					qty -= origBaseItems.GetItem(rule);
					if (qty > 0)
					{
						_recoveredItems[rule] = qty;

						string ss = $"{Unicode.TOK_COLOR_FLIP}{qty.ToString(CultureInfo.InvariantCulture)}{Unicode.TOK_COLOR_FLIP}";
						string item = Tr(itemType);
						if (rule.BattleType == BT_AMMO || (rule.BattleType == BT_NONE && rule.ClipSize > 0))
						{
							item = item.Insert(0, "  ");
							_lstRecoveredItems.AddRow(2, item, ss);
							_lstRecoveredItems.SetRowColor(row, _ammoColor);
						}
						else
						{
							_lstRecoveredItems.AddRow(2, item, ss);
						}
						++row;
					}
				}
			}
		}

		int total = 0, statsY = 0, recoveryY = 0;
		int civiliansSaved = 0, civiliansDead = 0;
		int aliensKilled = 0, aliensStunned = 0;
		foreach (DebriefingStat ds in _stats)
		{
			if (ds.qty == 0)
				continue;

			string ss = $"{Unicode.TOK_COLOR_FLIP}{ds.qty.ToString(CultureInfo.InvariantCulture)}{Unicode.TOK_COLOR_FLIP}";
			string ss2 = $"{Unicode.TOK_COLOR_FLIP}{ds.score.ToString(CultureInfo.InvariantCulture)}";
			total += ds.score;
			if (ds.recovery)
			{
				_lstRecovery.AddRow(3, Tr(ds.item), ss, ss2);
				recoveryY += 8;
			}
			else
			{
				_lstStats.AddRow(3, Tr(ds.item), ss, ss2);
				statsY += 8;
			}
			if (ds.item == "STR_CIVILIANS_SAVED")
			{
				civiliansSaved = ds.qty;
			}
			if (ds.item == "STR_CIVILIANS_KILLED_BY_XCOM_OPERATIVES" || ds.item == "STR_CIVILIANS_KILLED_BY_ALIENS")
			{
				civiliansDead += ds.qty;
			}
			if (ds.item == "STR_ALIENS_KILLED")
			{
				aliensKilled += ds.qty;
			}
			if (ds.item == "STR_LIVE_ALIENS_RECOVERED")
			{
				aliensStunned += ds.qty;
			}
		}
		if (civiliansSaved != 0 && civiliansDead == 0 && _missionStatistics.success == true)
		{
			_missionStatistics.valiantCrux = true;
		}

		_lstTotal.AddRow(2, Tr("STR_TOTAL_UC"), total.ToString(CultureInfo.InvariantCulture));

		// add the points to our activity score
		_region?.AddActivityXcom(total);
		_country?.AddActivityXcom(total);

		// Resize (if needed)
		if (statsY > 80) statsY = 80;
		if (recoveryY > 80) recoveryY = 80;
		if (statsY + recoveryY > 120)
		{
			recoveryY = 120 - statsY;
			if (recoveryY < 80) _lstRecovery.SetHeight(recoveryY);
			if (recoveryY > 80) recoveryY = 80;
		}

		// Reposition to fit the screen
		if (recoveryY > 0)
		{
			if (string.IsNullOrEmpty(_txtRecovery.GetText()))
			{
				_txtRecovery.SetText(Tr("STR_BOUNTY"));
			}
			_txtRecovery.Y = _lstStats.Y + statsY + 5;
			_lstRecovery.Y = _txtRecovery.Y + 8;
			_lstTotal.Y = _lstRecovery.Y + recoveryY + 5;
		}
		else
		{
			_txtRecovery.SetText("");
			_lstTotal.Y = _lstStats.Y + statsY + 5;
		}

		// Calculate rating
		string rating = CalculateRating(total, _game.Mod.MissionRatings);

		_missionStatistics.rating = rating;
		_missionStatistics.score = total;
		_txtRating.SetText(Tr("STR_RATING").Arg(Tr(rating)));

		SavedGame save = _game.SavedGame;
		SavedBattleGame battle = save.SavedBattle;

		_missionStatistics.daylight = save.SavedBattle.GlobalShade;
		_missionStatistics.id = _game.SavedGame.MissionStatistics.Count;
		_game.SavedGame.MissionStatistics.Add(_missionStatistics);

		// Award Best-of commendations.
		int[] bestScoreID = [0, 0, 0, 0, 0, 0, 0];
		int[] bestScore = [0, 0, 0, 0, 0, 0, 0];
		int bestOverallScorersID = 0;
		int bestOverallScore = 0;

		// Check to see if any of the dead soldiers were exceptional.
		foreach (BattleUnit deadUnit in battle.Units)
		{
			if (deadUnit.GeoscapeSoldier == null || deadUnit.Status != STATUS_DEAD)
			{
				continue;
			}

			/// Post-mortem kill award
			int killTurn = -1;
			foreach (BattleUnit killerUnit in battle.Units)
			{
				foreach (BattleUnitKills kill in killerUnit.Statistics.kills)
				{
					if (kill.id == deadUnit.Id)
					{
						killTurn = kill.turn;
						break;
					}
				}
				if (killTurn != -1)
				{
					break;
				}
			}
			int postMortemKills = 0;
			if (killTurn != -1)
			{
				foreach (BattleUnitKills deadUnitKill in deadUnit.Statistics.kills)
				{
					if (deadUnitKill.turn > killTurn && deadUnitKill.faction == FACTION_HOSTILE)
					{
						postMortemKills++;
					}
				}
			}
			deadUnit.GeoscapeSoldier.Diary.AwardPostMortemKill(postMortemKills);

			SoldierRank rank = deadUnit.GeoscapeSoldier.Rank;
			// Rookies don't get this next award. No one likes them.
			if (rank == SoldierRank.RANK_ROOKIE)
			{
				continue;
			}

			/// Best-of awards
			// Find the best soldier per rank by comparing score.
			foreach (Soldier deadSoldier in _game.SavedGame.DeadSoldiers)
			{
				int score = deadSoldier.Diary.GetScoreTotal(_game.SavedGame.MissionStatistics);

				// Don't forget this mission's score!
				if (deadSoldier.Id == deadUnit.Id)
				{
					score += _missionStatistics.score;
				}

				if (score > bestScore[(int)rank])
				{
					bestScoreID[(int)rank] = deadUnit.Id;
					bestScore[(int)rank] = score;
					if (score > bestOverallScore)
					{
						bestOverallScorersID = deadUnit.Id;
						bestOverallScore = score;
					}
				}
			}
		}
		// Now award those soldiers commendations!
		foreach (BattleUnit deadUnit in battle.Units)
		{
			if (deadUnit.GeoscapeSoldier == null || deadUnit.Status != STATUS_DEAD)
			{
				continue;
			}
			if (deadUnit.Id == bestScoreID[(int)deadUnit.GeoscapeSoldier.Rank])
			{
				deadUnit.GeoscapeSoldier.Diary.AwardBestOfRank(bestScore[(int)deadUnit.GeoscapeSoldier.Rank]);
			}
			if (deadUnit.Id == bestOverallScorersID)
			{
				deadUnit.GeoscapeSoldier.Diary.AwardBestOverall(bestOverallScore);
			}
		}

		foreach (BattleUnit bu in battle.Units)
		{
			if (bu.GeoscapeSoldier != null)
			{
				int soldierAlienKills = 0;
				int soldierAlienStuns = 0;
				foreach (BattleUnitKills kill in bu.Statistics.kills)
				{
					if (kill.faction == FACTION_HOSTILE && kill.status == STATUS_DEAD)
					{
						soldierAlienKills++;
					}
					if (kill.faction == FACTION_HOSTILE && kill.status == STATUS_UNCONSCIOUS)
					{
						soldierAlienStuns++;
					}
				}
				bu.GeoscapeSoldier.AddStunCount(soldierAlienStuns);

				if (aliensKilled != 0 && aliensKilled == soldierAlienKills && _missionStatistics.success == true && aliensStunned == soldierAlienStuns)
				{
					bu.Statistics.nikeCross = true;
				}
				if (aliensStunned != 0 && aliensStunned == soldierAlienStuns && _missionStatistics.success == true && aliensKilled == 0)
				{
					bu.Statistics.mercyCross = true;
				}
				int daysWoundedTmp = bu.GeoscapeSoldier.GetWoundRecovery(0.0f, 0.0f);
				bu.Statistics.daysWounded = daysWoundedTmp;
				if (daysWoundedTmp != 0)
				{
					_missionStatistics.injuryList[bu.GeoscapeSoldier.Id] = daysWoundedTmp;
				}

				// Award Martyr Medal
				if (bu.MurdererId == bu.Id && bu.Statistics.kills.Count != 0)
				{
					int martyrKills = 0; // How many aliens were killed on the same turn?
					int martyrTurn = -1;
					foreach (BattleUnitKills unitKill in bu.Statistics.kills)
					{
						if (unitKill.id == bu.Id)
						{
							martyrTurn = unitKill.turn;
							break;
						}
					}
					foreach (BattleUnitKills unitKill in bu.Statistics.kills)
					{
						if (unitKill.turn == martyrTurn && unitKill.faction == FACTION_HOSTILE)
						{
							martyrKills++;
						}
					}
					if (martyrKills > 0)
					{
						if (martyrKills > 10)
						{
							martyrKills = 10;
						}
						bu.Statistics.martyr = martyrKills;
					}
				}

				// Set the UnitStats delta
				bu.Statistics.delta = bu.GeoscapeSoldier.CurrentStats - bu.GeoscapeSoldier.InitStats;

				bu.GeoscapeSoldier.Diary.UpdateDiary(bu.Statistics, _game.SavedGame.MissionStatistics, _game.Mod);
				if (!bu.Statistics.MIA && !bu.Statistics.KIA &&
					bu.GeoscapeSoldier.Diary.ManageCommendations(_game.Mod, _game.SavedGame, bu.GeoscapeSoldier))
				{
					_soldiersCommended.Add(bu.GeoscapeSoldier);
				}
				else if (bu.Statistics.MIA || bu.Statistics.KIA)
				{
					bu.GeoscapeSoldier.Diary.ManageCommendations(_game.Mod, _game.SavedGame, bu.GeoscapeSoldier);
					_deadSoldiersCommended.Add(bu.GeoscapeSoldier);
				}
			}
		}

		_positiveScore = total > 0;

		List<Soldier> participants = [];
		foreach (BattleUnit bu in _game.SavedGame.SavedBattle.Units)
		{
			if (bu.GeoscapeSoldier != null)
			{
				if (Options.fieldPromotions && !bu.HasGainedAnyExperience)
				{
					// Note: difference from OXC, soldier needs to actually have done something during the mission
					continue;
				}
				participants.Add(bu.GeoscapeSoldier);
			}
		}

		if (Options.oxceAutomaticPromotions)
		{
			_promotions = _game.SavedGame.HandlePromotions(participants, _game.Mod);
		}

		_game.SavedGame.SetBattleGame(null);

		if (_positiveScore)
		{
			_game.Mod.PlayMusic(Mod.DEBRIEF_MUSIC_GOOD);
		}
		else
		{
			_game.Mod.PlayMusic(Mod.DEBRIEF_MUSIC_BAD);
		}
	}

	/// <summary>
	/// Calculates the mission rating string for a total score (vanilla thresholds, or the mod's missionRatings).
	/// </summary>
	internal static string CalculateRating(int total, SortedDictionary<int, string> missionRatings)
	{
		string rating;
		if (total <= -200)
		{
			rating = "STR_RATING_TERRIBLE";
		}
		else if (total <= 0)
		{
			rating = "STR_RATING_POOR";
		}
		else if (total <= 200)
		{
			rating = "STR_RATING_OK";
		}
		else if (total <= 500)
		{
			rating = "STR_RATING_GOOD";
		}
		else
		{
			rating = "STR_RATING_EXCELLENT";
		}

		if (missionRatings != null && missionRatings.Count != 0)
		{
			rating = "";
			int temp = int.MinValue;
			foreach (var pair in missionRatings)
			{
				if (pair.Key > temp && pair.Key <= total)
				{
					temp = pair.Key;
					rating = pair.Value;
				}
			}
		}
		return rating;
	}

	/// <summary>Shows a tooltip for the appropriate text.</summary>
	public void TxtTooltipIn(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		_currentTooltip = action.Sender.Tooltip;
		_txtTooltip.SetText(Tr(_currentTooltip));
	}

	/// <summary>Clears the tooltip text.</summary>
	public void TxtTooltipOut(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (_currentTooltip == action.Sender.Tooltip)
		{
			_txtTooltip.SetText("");
		}
	}

	/// <summary>Displays soldiers' stat increases.</summary>
	public void BtnStatsClick(Action action)
	{
		_pageNumber = (_pageNumber + 1) % 3;
		ApplyVisibility();
	}

	/// <summary>Opens the Sell/Sack UI (for recovered items ONLY).</summary>
	public void BtnSellClick(Action action)
	{
		if (!_destroyBase)
		{
			_game.PushState(new SellState(_base, this, OptionsOrigin.OPT_BATTLESCAPE));
		}
	}

	/// <summary>Opens the Transfer UI (for recovered items ONLY).</summary>
	public void BtnTransferClick(Action action)
	{
		if (!_destroyBase)
		{
			_game.PushState(new TransferBaseState(_base, this));
		}
	}

	/// <summary>Returns to the previous screen.</summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
		if (_game.SavedGame.MonthsPassed == -1)
		{
			_game.SetState(new MainMenuState());
		}
		else
		{
			// Autosave after mission
			if (_game.SavedGame.IsIronman)
			{
				_game.PushState(new SaveGameState(OptionsOrigin.OPT_GEOSCAPE, SaveType.SAVE_IRONMAN, _palette));
			}
			else if (Options.autosave)
			{
				_game.PushState(new SaveGameState(OptionsOrigin.OPT_GEOSCAPE, SaveType.SAVE_AUTO_GEOSCAPE, _palette));
			}

			if (_eventToSpawn != null)
			{
				bool canSpawn = _game.SavedGame.CanSpawnInstantEvent(_eventToSpawn);
				if (canSpawn)
				{
					_game.PushState(new GeoscapeEventState(_eventToSpawn));
				}
			}
			if (_deadSoldiersCommended.Count != 0)
			{
				_game.PushState(new CommendationLateState(_deadSoldiersCommended));
			}
			if (_soldiersCommended.Count != 0)
			{
				_game.PushState(new CommendationState(_soldiersCommended));
			}
			if (!_destroyBase)
			{
				if (_promotions)
				{
					_game.PushState(new PromotionsState());
				}
				if (_missingItems.Count != 0)
				{
					_game.PushState(new CannotReequipState(_missingItems, _base));
				}
				// remove the wounded soldiers (and their items too if needed); this was moved here from BattleUnit::postMissionProcedures()
				foreach (Soldier soldier in _base.Soldiers)
				{
					if (soldier.Craft != null && soldier.IsWounded)
					{
						soldier.SetCraftAndMoveEquipment(null, _base, _game.SavedGame.MonthsPassed == -1);
					}
				}

				// refresh! (we may have sold some prisoners in the meantime; directly from Debriefing)
				foreach (int prisonType in new List<int>(_containmentStateInfo.Keys))
				{
					if (_containmentStateInfo[prisonType] == 2)
					{
						int availableContainment = _base.GetAvailableContainment(prisonType);
						int usedContainment = _base.GetUsedContainment(prisonType);
						int freeContainment = availableContainment - (usedContainment * _limitsEnforced);
						if (availableContainment > 0 && freeContainment >= 0)
						{
							_containmentStateInfo[prisonType] = 0; // 0 = OK
						}
						else if (usedContainment == 0)
						{
							_containmentStateInfo[prisonType] = 0; // 0 = OK
						}
					}
				}

				RuleInterface debriefingInterface = _game.Mod.GetInterface("debriefing");
				foreach (var pair in _containmentStateInfo)
				{
					if (pair.Value == 2)
					{
						_game.PushState(new ManageAlienContainmentState(_base, pair.Key, OptionsOrigin.OPT_BATTLESCAPE));
						_game.PushState(new ErrorMessageState(TrAlt("STR_CONTAINMENT_EXCEEDED", pair.Key).Arg(_base.GetName(_game.Language)), _palette, (byte)debriefingInterface.GetElement("errorMessage").color, "BACK01.SCR", debriefingInterface.GetElement("errorPalette").color));
					}
					else if (pair.Value == 1)
					{
						_game.PushState(new ErrorMessageState(
							TrAlt("STR_ALIEN_DIES_NO_ALIEN_CONTAINMENT_FACILITY", pair.Key),
							_palette,
							(byte)debriefingInterface.GetElement("errorMessage").color,
							"BACK01.SCR",
							debriefingInterface.GetElement("errorPalette").color));
					}
				}

				if (Options.storageLimitsEnforced && _base.StoresOverfull())
				{
					_game.PushState(new SellState(_base, null, OptionsOrigin.OPT_BATTLESCAPE));
					_game.PushState(new ErrorMessageState(Tr("STR_STORAGE_EXCEEDED").Arg(_base.GetName(_game.Language)), _palette, (byte)debriefingInterface.GetElement("errorMessage").color, "BACK01.SCR", debriefingInterface.GetElement("errorPalette").color));
				}
			}
		}
	}

	/// <summary>Adds to the debriefing stats.</summary>
	/// <param name="name">The untranslated name of the stat.</param>
	/// <param name="quantity">The quantity to add.</param>
	/// <param name="score">The score to add.</param>
	private void AddStat(string name, int quantity, int score)
	{
		foreach (DebriefingStat ds in _stats)
		{
			if (ds.item == name)
			{
				ds.qty = ds.qty + quantity;
				ds.score = ds.score + score;
				break;
			}
		}
	}

	/// <summary>
	/// Prepares debriefing: gathers Aliens, Corpses, Artefacts, UFO Components.
	/// Adds the items to the craft.
	/// Also calculates the soldiers experience, and possible promotions.
	/// If aborted, only the things on the exit area are recovered.
	/// </summary>
	private void PrepareDebriefing()
	{
		foreach (string itemType in _game.Mod.ItemsList)
		{
			RuleItem rule = _game.Mod.GetItem(itemType);
			if (rule.SpecialType > 1 && rule.SpecialType < (int)DEATH_TRAPS)
			{
				RecoveryItem item = new()
				{
					name = itemType,
					value = rule.RecoveryPoints,
				};
				_recoveryStats[rule.SpecialType] = item;
				_missionStatistics.lootValue = item.value;
			}
		}

		SavedGame save = _game.SavedGame;
		SavedBattleGame battle = save.SavedBattle;

		AlienDeployment ruleDeploy = _game.Mod.GetDeployment(battle.MissionType);
		// OXCE: Don't forget custom mission overrides
		AlienDeployment alienCustomMission = _game.Mod.GetDeployment(battle.AlienCustomMission);
		if (alienCustomMission != null)
		{
			ruleDeploy = alienCustomMission;
		}
		// OXCE: Don't forget about UFO landings/crash sites
		if (ruleDeploy == null)
		{
			foreach (Ufo ufo in save.Ufos)
			{
				if (ufo.IsInBattlescape)
				{
					// Note: fake underwater UFO deployment was already considered above (via alienCustomMission)
					ruleDeploy = _game.Mod.GetDeployment(ufo.Rules.Type);
					break;
				}
			}
		}

		bool aborted = battle.IsAborted;
		bool success = !aborted || battle.AllObjectivesDestroyed();
		Craft craft = null;
		Base @base = null;
		string target = "";

		int playersInExitArea1 = 0; // if playersInExitArea2 stays 0 the craft is lost...
		int playersSurvived = 0; // if this stays 0 the craft is lost...
		int playersUnconscious = 0;
		int playersInEntryArea1 = 0;
		int playersMIA = 0;

		_stats.Add(new DebriefingStat("STR_ALIENS_KILLED", false));
		_stats.Add(new DebriefingStat("STR_ALIEN_CORPSES_RECOVERED", false));
		_stats.Add(new DebriefingStat("STR_LIVE_ALIENS_RECOVERED", false));
		_stats.Add(new DebriefingStat("STR_LIVE_ALIENS_SURRENDERED", false));
		_stats.Add(new DebriefingStat("STR_ALIEN_ARTIFACTS_RECOVERED", false));

		string missionCompleteText = "", missionFailedText = "";
		string objectiveCompleteText = "", objectiveFailedText = "";
		int objectiveCompleteScore = 0, objectiveFailedScore = 0;
		if (ruleDeploy != null)
		{
			if (ruleDeploy.GetObjectiveCompleteInfo(ref objectiveCompleteText, ref objectiveCompleteScore, ref missionCompleteText))
			{
				_stats.Add(new DebriefingStat(objectiveCompleteText, false));
			}
			if (ruleDeploy.GetObjectiveFailedInfo(ref objectiveFailedText, ref objectiveFailedScore, ref missionFailedText))
			{
				_stats.Add(new DebriefingStat(objectiveFailedText, false));
			}
			if (aborted && ruleDeploy.AbortPenalty != 0)
			{
				_stats.Add(new DebriefingStat("STR_MISSION_ABORTED", false));
				AddStat("STR_MISSION_ABORTED", 1, -ruleDeploy.AbortPenalty);
			}
		}
		if (battle.VIPSurvivalPercentage > 0)
		{
			_stats.Add(new DebriefingStat("STR_VIPS_LOST", false));
			_stats.Add(new DebriefingStat("STR_VIPS_SAVED", false));
		}

		_stats.Add(new DebriefingStat("STR_CIVILIANS_KILLED_BY_ALIENS", false));
		_stats.Add(new DebriefingStat("STR_CIVILIANS_KILLED_BY_XCOM_OPERATIVES", false));
		_stats.Add(new DebriefingStat("STR_CIVILIANS_SAVED", false));
		_stats.Add(new DebriefingStat("STR_XCOM_OPERATIVES_KILLED", false));
		//_stats.Add(new DebriefingStat("STR_XCOM_OPERATIVES_RETIRED_THROUGH_INJURY", false));
		_stats.Add(new DebriefingStat("STR_XCOM_OPERATIVES_MISSING_IN_ACTION", false));
		_stats.Add(new DebriefingStat("STR_TANKS_DESTROYED", false));
		_stats.Add(new DebriefingStat("STR_XCOM_CRAFT_LOST", false));

		foreach (var pair in _recoveryStats)
		{
			_stats.Add(new DebriefingStat(pair.Value.name, true));
		}

		_missionStatistics.time = new GameTime(save.Time);
		_missionStatistics.type = battle.MissionType;
		_stats.Add(new DebriefingStat(_game.Mod.AlienFuelName, true));

		foreach (Base xbase in save.Bases)
		{
			// in case we have a craft - check which craft it is about
			foreach (Craft xcraft in xbase.Crafts)
			{
				if (xcraft.IsInBattlescape)
				{
					foreach (Region region in save.Regions)
					{
						if (region.Rules.InsideRegion(xcraft.Longitude, xcraft.Latitude))
						{
							_region = region;
							_missionStatistics.region = _region.Rules.Type;
							break;
						}
					}
					foreach (Country country in save.Countries)
					{
						if (country.Rules.InsideCountry(xcraft.Longitude, xcraft.Latitude))
						{
							_country = country;
							_missionStatistics.country = _country.Rules.Type;
							break;
						}
					}
					craft = xcraft;
					@base = xbase;
					Target destination = craft.Destination;
					if (destination != null)
					{
						_missionStatistics.markerName = destination.MarkerName;
						_missionStatistics.markerId = destination.MarkerId;
						target = destination.Type;
						// Ignore custom mission names
						if (destination is AlienBase)
						{
							target = "STR_ALIEN_BASE";
						}
						else if (destination is MissionSite)
						{
							target = "STR_MISSION_SITE";
						}
					}
					craft.ReturnToBase();
					craft.MissionComplete = true;
					craft.SetInBattlescape(false);
					foreach (Craft follower in craft.CraftFollowers)
					{
						follower.ReturnToBase();
					}
				}
				else if (xcraft.Destination != null)
				{
					Ufo u = xcraft.Destination as Ufo;
					if (u != null && u.IsInBattlescape)
					{
						xcraft.ReturnToBase();
					}
					MissionSite ms = xcraft.Destination as MissionSite;
					if (ms != null && ms.IsInBattlescape)
					{
						xcraft.ReturnToBase();
					}
				}
			}
			// in case we DON'T have a craft (base defense)
			if (xbase.IsInBattlescape)
			{
				@base = xbase;
				target = @base.Type;
				@base.SetInBattlescape(false);
				@base.CleanupDefenses(false);
				foreach (Region region in save.Regions)
				{
					if (region.Rules.InsideRegion(@base.Longitude, @base.Latitude))
					{
						_region = region;
						_missionStatistics.region = _region.Rules.Type;
						break;
					}
				}
				foreach (Country country in save.Countries)
				{
					if (country.Rules.InsideCountry(@base.Longitude, @base.Latitude))
					{
						_country = country;
						_missionStatistics.country = _country.Rules.Type;
						break;
					}
				}
				// Loop through the UFOs and see which one is sitting on top of the base... that is probably the one attacking you.
				foreach (Ufo ufo in save.Ufos)
				{
					if (Target.AreSame(ufo.Longitude, @base.Longitude) && Target.AreSame(ufo.Latitude, @base.Latitude))
					{
						_missionStatistics.ufo = ufo.Rules.Type; // no need to check for fake underwater UFOs here
						_missionStatistics.alienRace = ufo.AlienRace;
						break;
					}
				}
				if (aborted)
				{
					_destroyBase = true;
				}

				// This is an overkill, since we may not lose any hangar/craft, but doing it properly requires tons of changes
				save.StopHuntingXcomCrafts(@base);

				List<BaseFacility> toBeDamaged = [];
				foreach (BaseFacility fac in @base.Facilities)
				{
					// this facility was demolished
					if (battle.ModuleMap[fac.X][fac.Y].second == 0)
					{
						toBeDamaged.Add(fac);
					}
				}
				foreach (BaseFacility fac in toBeDamaged)
				{
					@base.DamageFacility(fac);
				}
				// this may cause the base to become disjointed, destroy the disconnected parts.
				@base.DestroyDisconnectedFacilities();
			}
		}

		if (@base == null && save.IsIronman)
		{
			throw new InvalidOperationException("Your save is corrupted. Try asking someone on the Openxcom forum to fix it for you.");
		}

		// mission site disappears (even when you abort)
		Ufo ignoredUfo = null;
		for (int msIt = 0; msIt < save.MissionSites.Count; ++msIt)
		{
			MissionSite ms = save.MissionSites[msIt];
			if (ms.IsInBattlescape)
			{
				if (ms.Ufo != null)
				{
					ignoredUfo = ms.Ufo;
					ms.Ufo = null;
				}
				_missionStatistics.alienRace = ms.AlienRace;
				save.MissionSites.RemoveAt(msIt);
				break;
			}
		}

		// lets see what happens with units

		// manual update state of all units
		foreach (BattleUnit unit in battle.Units)
		{
			// scripts (or some bugs in the game) could make aliens or soldiers that have "unresolved" stun or death state.
			// Note: resolves the "last bleeding alien" too
			if (!unit.IsOut && unit.IsOutThresholdExceed)
			{
				unit.InstaFalling();
				if (unit.Tile != null)
				{
					battle.TileEngine.ItemDropInventory(unit.Tile, unit);
				}

				//spawn corpse/body for unit to recover
				for (int i = unit.Armor.TotalSize - 1; i >= 0; --i)
				{
					BattleItem corpse = battle.CreateItemForTile(unit.Armor.CorpseBattlescape[i], null, unit);
					battle.TileEngine.ItemDrop(unit.Tile, corpse, false);
				}
			}
		}

		// first, we evaluate how many surviving XCom units there are, and how many are conscious
		// and how many have died (to use for commendations)
		int deadSoldiers = 0;
		foreach (BattleUnit bu in battle.Units)
		{
			if (bu.OriginalFaction == FACTION_PLAYER && bu.Status != STATUS_DEAD)
			{
				if (bu.Status == STATUS_UNCONSCIOUS || bu.Faction == FACTION_HOSTILE)
				{
					playersUnconscious++;
				}
				else if (bu.IsIgnored && bu.Stunlevel >= bu.Health)
				{
					// even for ignored xcom units, we need to know if they're conscious or unconscious
					playersUnconscious++;
				}
				else if (bu.IsInExitArea(END_POINT))
				{
					playersInExitArea1++;
				}
				else if (bu.IsInExitArea(START_POINT))
				{
					playersInEntryArea1++;
				}
				else if (aborted)
				{
					// if aborted, conscious xcom unit that is not on start/end point counts as MIA
					playersMIA++;
				}
				playersSurvived++;
			}
			else if (bu.OriginalFaction == FACTION_PLAYER && bu.Status == STATUS_DEAD)
			{
				deadSoldiers++;
			}
		}
		// if all our men are unconscious, the aliens get to have their way with them.
		if (playersUnconscious + playersMIA == playersSurvived)
		{
			playersSurvived = playersMIA;
			foreach (BattleUnit bu in battle.Units)
			{
				if (bu.OriginalFaction == FACTION_PLAYER && bu.Status != STATUS_DEAD)
				{
					if (bu.Status == STATUS_UNCONSCIOUS || bu.Faction == FACTION_HOSTILE)
					{
						bu.InstaKill();
					}
					else if (bu.IsIgnored && bu.Stunlevel >= bu.Health)
					{
						bu.InstaKill();
					}
					else
					{
						// do nothing, units will be marked MIA later
					}
				}
			}
		}

		// if it's a UFO, let's see what happens to it
		for (int ufoIt = 0; ufoIt < save.Ufos.Count; ++ufoIt)
		{
			Ufo ufo = save.Ufos[ufoIt];
			if (ufo.IsInBattlescape)
			{
				_missionStatistics.ufo = ufo.Rules.Type; // no need to check for fake underwater UFOs here
				if (save.MonthsPassed != -1)
				{
					_missionStatistics.alienRace = ufo.AlienRace;
				}
				_txtRecovery.SetText(Tr("STR_UFO_RECOVERY"));
				ufo.SetInBattlescape(false);
				// if XCom failed to secure the landing zone, the UFO
				// takes off immediately and proceeds according to its mission directive
				if (ufo.Status == Ufo.UfoStatus.LANDED && (aborted || playersSurvived == 0))
				{
					ufo.SecondsRemaining = 5;
				}
				// if XCom succeeds, or it's a crash site, the UFO disappears
				else
				{
					// Note: just before removing a landed UFO, check for mission interruption (by setting the UFO damage to max)
					if (save.MonthsPassed > -1)
					{
						if (ufo.Status == Ufo.UfoStatus.LANDED)
						{
							ufo.SetDamage(ufo.CraftStats.damageMax, _game.Mod);
						}
					}
					save.Ufos.RemoveAt(ufoIt);
				}
				break;
			}
		}

		if (ruleDeploy != null && ruleDeploy.EscapeType != ESCAPE_NONE)
		{
			if (ruleDeploy.EscapeType != ESCAPE_EXIT)
			{
				success = playersInEntryArea1 > 0;
			}

			if (ruleDeploy.EscapeType != ESCAPE_ENTRY)
			{
				success = success || playersInExitArea1 > 0;
			}
		}

		int playersInExitArea2 = 0;

		if (playersSurvived == 1)
		{
			foreach (BattleUnit bu in battle.Units)
			{
				// if only one soldier survived, give him a medal! (unless he killed all the others...)
				if (bu.Status != STATUS_DEAD && bu.OriginalFaction == FACTION_PLAYER && !bu.Statistics.HasFriendlyFired && deadSoldiers != 0)
				{
					bu.Statistics.loneSurvivor = true;
					break;
				}
				// if only one soldier survived AND none have died, means only one soldier went on the mission...
				if (bu.Status != STATUS_DEAD && bu.OriginalFaction == FACTION_PLAYER && deadSoldiers == 0)
				{
					bu.Statistics.ironMan = true;
				}
			}
		}
		// alien base disappears (if you didn't abort)
		for (int abIt = 0; abIt < save.AlienBases.Count; ++abIt)
		{
			AlienBase ab = save.AlienBases[abIt];
			if (ab.IsInBattlescape)
			{
				_txtRecovery.SetText(Tr("STR_ALIEN_BASE_RECOVERY"));
				bool destroyAlienBase = true;

				if (aborted || playersSurvived == 0)
				{
					if (!battle.AllObjectivesDestroyed())
						destroyAlienBase = false;
				}

				if (ruleDeploy != null && !string.IsNullOrEmpty(ruleDeploy.NextStage))
				{
					_missionStatistics.alienRace = ab.AlienRace;
					destroyAlienBase = false;
				}

				success = destroyAlienBase;
				if (destroyAlienBase)
				{
					if (!string.IsNullOrEmpty(objectiveCompleteText))
					{
						AddStat(objectiveCompleteText, 1, objectiveCompleteScore);
					}
					save.ClearLinksForAlienBase(ab, _game.Mod);
					save.AlienBases.RemoveAt(abIt);
					break;
				}
				else
				{
					ab.SetInBattlescape(false);
					break;
				}
			}
		}

		// transform all zombie-like units to spawned ones
		List<BattleUnit> waitingTransformations = [];
		foreach (BattleUnit bu in battle.Units)
		{
			if (bu.SpawnUnit != null && (!bu.IsOut || bu.IsIgnored))
			{
				if (bu.OriginalFaction == FACTION_HOSTILE)
				{
					waitingTransformations.Add(bu);
				}
				else
				{
					//if unit belong to XCOM or CIVILIANS we leave it as-is
				}
			}
		}
		foreach (BattleUnit bu in waitingTransformations)
		{
			bool ignore = bu.IsIgnored;
			UnitFaction faction = bu.Faction;
			// convert it, and mind control the resulting unit.
			// reason: zombies don't create unconscious bodies... ever.
			// the only way we can get into this situation is if psi-capture is enabled.
			// we can use that knowledge to our advantage to save having to make it unconscious and spawn a body item for it.
			if (ignore)
			{
				//simplified handling for unit from previous stage
				BattleUnit newUnit = battle.CreateTempUnit(bu.SpawnUnit, bu.SpawnUnitFaction);
				battle.Units.Add(newUnit);
				newUnit.ConvertToFaction(faction);
			}
			else
			{
				BattleUnit newUnit = battle.ConvertUnit(bu);
				newUnit.ConvertToFaction(faction);
			}
			bu.KilledBy(FACTION_HOSTILE); //skip counting as kill
		}

		// time to care for units.
		bool psiStrengthEval = Options.psiStrengthEval && save.IsResearched(_game.Mod.PsiRequirements);
		bool ignoreLivingCivilians = false;
		if (ruleDeploy != null)
		{
			ignoreLivingCivilians = ruleDeploy.IgnoreLivingCivilians;
		}
		foreach (BattleUnit bunit in battle.Units)
		{
			UnitStatus status = bunit.Status;
			UnitFaction faction = bunit.Faction;
			UnitFaction oldFaction = bunit.OriginalFaction;
			int value = bunit.Value;
			Soldier soldier = save.GetSoldier(bunit.Id);

			if (bunit.Tile == null)
			{
				Position pos = bunit.Position;
				if (pos == TileEngine.invalid)
				{
					foreach (BattleItem bi in battle.Items)
					{
						if (bi.Unit != null && bi.Unit == bunit)
						{
							if (bi.Owner != null)
							{
								pos = bi.Owner.Position;
							}
							else if (bi.Tile != null)
							{
								pos = bi.Tile.Position;
							}
						}
					}
				}
				bunit.SetInventoryTile(battle.GetTile(pos));
			}

			if (status == STATUS_DEAD)
			{ // so this is a dead unit
				if (oldFaction == FACTION_HOSTILE && bunit.KilledBy() == FACTION_PLAYER)
				{
					AddStat("STR_ALIENS_KILLED", 1, value);
				}
				else if (oldFaction == FACTION_PLAYER)
				{
					if (soldier != null)
					{
						AddStat("STR_XCOM_OPERATIVES_KILLED", 1, -value);
						bunit.UpdateGeoscapeStats(soldier);

						// starting conditions: recover armor backup
						if (soldier.ReplacedArmor != null)
						{
							if (soldier.ReplacedArmor.StoreItem != null)
							{
								AddItemsToBaseStores(soldier.ReplacedArmor.StoreItem.Type, @base, 1, false);
							}
							soldier.ReplacedArmor = null;
						}
						// transformed armor doesn't get recovered
						soldier.TransformedArmor = null;

						bunit.Statistics.KIA = true;
						save.KillSoldier(true, soldier); // in case we missed the soldier death on battlescape
					}
					else
					{ // non soldier player = tank
						AddStat("STR_TANKS_DESTROYED", 1, -value);
						save.IncreaseVehiclesLost();
					}
				}
				else if (oldFaction == FACTION_NEUTRAL)
				{
					if (bunit.KilledBy() == FACTION_PLAYER)
					{
						if (!bunit.IsCosmetic)
						{
							AddStat("STR_CIVILIANS_KILLED_BY_XCOM_OPERATIVES", 1, -bunit.Value - (2 * (bunit.Value / 3)));
						}
					}
					else // if civilians happen to kill themselves XCOM shouldn't get penalty for it
					{
						if (!bunit.IsCosmetic)
						{
							AddStat("STR_CIVILIANS_KILLED_BY_ALIENS", 1, -bunit.Value);
						}
					}
				}
			}
			else
			{ // so this unit is not dead...
				if (oldFaction == FACTION_PLAYER)
				{
					if (
						((bunit.IsInExitArea(START_POINT) || bunit.IsIgnored) && (battle.MissionType != "STR_BASE_DEFENSE" || success))
						|| !aborted
						|| (aborted && bunit.IsInExitArea(END_POINT)))
					{ // so game is not aborted or aborted and unit is on exit area
						StatAdjustment statIncrease = new StatAdjustment();
						bunit.PostMissionProcedures(_game.Mod, save, battle, statIncrease);
						if (bunit.GeoscapeSoldier != null)
							_soldierStats.Add((bunit.GeoscapeSoldier.GetName(), statIncrease.statGrowth));
						playersInExitArea2++;

						RecoverItems(bunit.Inventory, @base, craft);

						if (soldier != null)
						{
							// calculate new statString
							soldier.CalcStatString(_game.Mod.StatStrings, psiStrengthEval);
						}
						else
						{ // non soldier player = tank
							AddItemsToBaseStores(bunit.Type, @base, 1, false);

							UnloadWeapon(bunit.RightHandWeapon, @base);
							UnloadWeapon(bunit.LeftHandWeapon, @base);
						}
					}
					else
					{ // so game is aborted and unit is not on exit area
						AddStat("STR_XCOM_OPERATIVES_MISSING_IN_ACTION", 1, -value);
						playersSurvived--;
						if (soldier != null)
						{
							bunit.UpdateGeoscapeStats(soldier);

							// starting conditions: recover armor backup
							if (soldier.ReplacedArmor != null)
							{
								if (soldier.ReplacedArmor.StoreItem != null)
								{
									AddItemsToBaseStores(soldier.ReplacedArmor.StoreItem.Type, @base, 1, false);
								}
								soldier.ReplacedArmor = null;
							}
							// transformed armor doesn't get recovered
							soldier.TransformedArmor = null;

							bunit.Statistics.MIA = true;
							save.KillSoldier(true, soldier);
						}
					}
				}
				else if (oldFaction == FACTION_HOSTILE && (!aborted || bunit.IsInExitArea(START_POINT)) && !_destroyBase
					// mind controlled units may as well count as unconscious
					&& faction == FACTION_PLAYER && (!bunit.IsOut || bunit.IsIgnored))
				{
					if (bunit.Tile != null)
					{
						battle.TileEngine.ItemDropInventory(bunit.Tile, bunit);
					}
					if (bunit.Armor.CorpseBattlescape.Count != 0)
					{
						RuleItem corpseRule = bunit.Armor.CorpseBattlescape[0];
						if (corpseRule != null && corpseRule.IsRecoverable)
						{
							RecoverAlien(bunit, @base, craft);
						}
					}
				}
				else if (oldFaction == FACTION_HOSTILE && !aborted && !_destroyBase
					// surrendered units may as well count as unconscious too
					&& playersSurvived > 0 && faction != FACTION_PLAYER && (!bunit.IsOut || bunit.IsIgnored)
					&& (bunit.IsSurrendering || battle.ChronoTrigger == ChronoTrigger.FORCE_WIN_SURRENDER))
				{
					if (bunit.Tile != null)
					{
						battle.TileEngine.ItemDropInventory(bunit.Tile, bunit);
					}
					if (bunit.Armor.CorpseBattlescape.Count != 0)
					{
						RuleItem corpseRule = bunit.Armor.CorpseBattlescape[0];
						if (corpseRule != null && corpseRule.IsRecoverable)
						{
							RecoverAlien(bunit, @base, craft);
						}
					}
				}
				else if (oldFaction == FACTION_NEUTRAL && !ignoreLivingCivilians)
				{
					// if mission fails, all civilians die
					if ((aborted && !success) || playersSurvived == 0)
					{
						if (!bunit.IsResummonedFakeCivilian && !bunit.IsCosmetic)
						{
							AddStat("STR_CIVILIANS_KILLED_BY_ALIENS", 1, -bunit.Value);
						}
					}
					else
					{
						if (!bunit.IsResummonedFakeCivilian && !bunit.IsCosmetic)
						{
							AddStat("STR_CIVILIANS_SAVED", 1, bunit.Value);
						}
						RecoverCivilian(bunit, @base, craft);
					}
				}
			}
		}

		bool lostCraft = false;
		if (craft != null && ((playersInExitArea2 == 0 && aborted) || (playersSurvived == 0)))
		{
			if (craft.Rules.KeepCraftAfterFailedMission())
			{
				// craft was not even on the battlescape (e.g. paratroopers)
			}
			else if (ruleDeploy.KeepCraftAfterFailedMission())
			{
				// craft didn't wait for you (e.g. escape/extraction missions)
			}
			else
			{
				AddStat("STR_XCOM_CRAFT_LOST", 1, -craft.Rules.Score);
				// Since this is not a base defense mission, we can safely erase the craft,
				// without worrying it's vehicles' destructor calling double (on base defense missions
				// all vehicle object in the craft is also referenced by base->getVehicles() !!)
				save.StopHuntingXcomCraft(craft); // lost during ground mission
				save.RemoveAllSoldiersFromXcomCraft(craft); // needed in case some soldiers couldn't spawn
				@base.RemoveCraft(craft, false);
				save.IncreaseCraftLostMission();
				craft = null; // To avoid a crash down there!!
				lostCraft = true;
			}
			playersSurvived = 0; // assuming you aborted and left everyone behind
			success = false;
		}
		if ((aborted || playersSurvived == 0) && target == "STR_BASE")
		{
			foreach (Craft xcraft in @base.Crafts)
			{
				AddStat("STR_XCOM_CRAFT_LOST", 1, -xcraft.Rules.Score);
			}
			playersSurvived = 0; // assuming you aborted and left everyone behind
			success = false;
		}

		bool savedEnoughVIPs = true;
		if (battle.VIPSurvivalPercentage > 0)
		{
			bool retreated = aborted && (playersSurvived > 0);

			// 1. correct our initial assessment if necessary
			battle.CorrectVIPStats(success, retreated);
			int vipSubtotal = battle.SavedVIPs + battle.LostVIPs;

			// 2. add non-fake civilian VIPs, no scoring
			foreach (BattleUnit unit in battle.Units)
			{
				if (unit.IsVIP && unit.OriginalFaction == FACTION_NEUTRAL && !unit.IsResummonedFakeCivilian)
				{
					if (unit.Status == STATUS_DEAD)
						battle.AddLostVIP(0);
					else if (success)
						battle.AddSavedVIP(0);
					else
						battle.AddLostVIP(0);
				}
			}

			// 3. check if we saved enough VIPs
			int vipTotal = battle.SavedVIPs + battle.LostVIPs;
			if (vipTotal > 0)
			{
				int ratio = battle.SavedVIPs * 100 / vipTotal;
				if (ratio < battle.VIPSurvivalPercentage)
				{
					savedEnoughVIPs = false; // didn't save enough VIPs
					success = false;
				}
			}
			else
			{
				savedEnoughVIPs = false; // nobody to save?
				success = false;
			}

			// 4. add stats
			if (vipSubtotal > 0 || (vipTotal > 0 && !savedEnoughVIPs))
			{
				AddStat("STR_VIPS_LOST", battle.LostVIPs, battle.LostVIPsScore);
				AddStat("STR_VIPS_SAVED", battle.SavedVIPs, battle.SavedVIPsScore);
			}
		}

		if ((!aborted || success) && playersSurvived > 0) 	// RECOVER UFO : run through all tiles to recover UFO components and items
		{
			if (target == "STR_BASE")
			{
				_txtTitle.SetText(Tr("STR_BASE_IS_SAVED"));
			}
			else if (target == "STR_UFO")
			{
				_txtTitle.SetText(Tr("STR_UFO_IS_RECOVERED"));
			}
			else if (target == "STR_ALIEN_BASE")
			{
				_txtTitle.SetText(Tr("STR_ALIEN_BASE_DESTROYED"));
			}
			else
			{
				_txtTitle.SetText(Tr("STR_ALIENS_DEFEATED"));
				if (!aborted && !savedEnoughVIPs)
				{
					// Special case: mission was NOT aborted, all enemies were neutralized, but we couldn't save enough VIPs...
					if (!string.IsNullOrEmpty(objectiveFailedText))
					{
						AddStat(objectiveFailedText, 1, objectiveFailedScore);
					}
				}
				else if (!string.IsNullOrEmpty(objectiveCompleteText))
				{
					int victoryStat = 0;
					if (ruleDeploy.EscapeType != ESCAPE_NONE)
					{
						if (ruleDeploy.EscapeType != ESCAPE_EXIT)
						{
							victoryStat += playersInEntryArea1;
						}
						if (ruleDeploy.EscapeType != ESCAPE_ENTRY)
						{
							victoryStat += playersInExitArea1;
						}
					}
					else
					{
						victoryStat = 1;
					}
					if (battle.VIPSurvivalPercentage > 0)
					{
						victoryStat = 1; // TODO: maybe show battle->getSavedVIPs() instead? need feedback...
					}

					AddStat(objectiveCompleteText, victoryStat, objectiveCompleteScore);
				}
			}
			if (!aborted && !savedEnoughVIPs)
			{
				// Special case: mission was NOT aborted, all enemies were neutralized, but we couldn't save enough VIPs...
				if (!string.IsNullOrEmpty(missionFailedText))
				{
					_txtTitle.SetText(Tr(missionFailedText));
				}
				else
				{
					_txtTitle.SetText(Tr("STR_TERROR_CONTINUES"));
				}
			}
			else if (!string.IsNullOrEmpty(missionCompleteText))
			{
				_txtTitle.SetText(Tr(missionCompleteText));
			}

			if (!aborted)
			{
				// if this was a 2-stage mission, and we didn't abort (ie: we have time to clean up)
				// we can recover items from the earlier stages as well
				RecoverItems(battle.ConditionalRecoveredItems, @base, craft);
				int nonRecoverType = 0;
				if (ruleDeploy != null && ruleDeploy.ObjectiveType != 0 && !ruleDeploy.AllowObjectiveRecovery())
				{
					nonRecoverType = ruleDeploy.ObjectiveType;
				}
				for (int i = 0; i < battle.MapSizeXYZ; ++i)
				{
					// get recoverable map data objects from the battlescape map
					for (int part = (int)TilePart.O_FLOOR; part < (int)TilePart.O_MAX; ++part)
					{
						TilePart tp = (TilePart)part;
						if (battle.GetTile(i).GetMapData(tp) != null)
						{
							// C++ compares as size_t
							uint specialType = (uint)battle.GetTile(i).GetMapData(tp).SpecialType;
							if (specialType != (uint)nonRecoverType && specialType < (uint)DEATH_TRAPS && _recoveryStats.TryGetValue((int)specialType, out RecoveryItem recoveryItem))
							{
								AddStat(recoveryItem.name, 1, recoveryItem.value);
							}
						}
					}
					// recover items from the floor
					RecoverItems(battle.GetTile(i).Inventory, @base, craft);
				}
			}
			else
			{
				for (int i = 0; i < battle.MapSizeXYZ; ++i)
				{
					if (battle.GetTile(i).FloorSpecialTileType == START_POINT)
						RecoverItems(battle.GetTile(i).Inventory, @base, craft);
				}
			}
		}
		else
		{
			if (lostCraft)
			{
				_txtTitle.SetText(Tr("STR_CRAFT_IS_LOST"));
			}
			else if (target == "STR_BASE")
			{
				_txtTitle.SetText(Tr("STR_BASE_IS_LOST"));
				_destroyBase = true;
			}
			else if (target == "STR_UFO")
			{
				_txtTitle.SetText(Tr("STR_UFO_IS_NOT_RECOVERED"));
			}
			else if (target == "STR_ALIEN_BASE")
			{
				_txtTitle.SetText(Tr("STR_ALIEN_BASE_STILL_INTACT"));
			}
			else
			{
				_txtTitle.SetText(Tr("STR_TERROR_CONTINUES"));
				if (!string.IsNullOrEmpty(objectiveFailedText))
				{
					AddStat(objectiveFailedText, 1, objectiveFailedScore);
				}
			}
			if (!string.IsNullOrEmpty(missionFailedText))
			{
				_txtTitle.SetText(Tr(missionFailedText));
			}

			if (playersSurvived > 0 && !_destroyBase)
			{
				// recover items from the craft floor
				for (int i = 0; i < battle.MapSizeXYZ; ++i)
				{
					if (battle.GetTile(i).FloorSpecialTileType == START_POINT)
						RecoverItems(battle.GetTile(i).Inventory, @base, craft);
				}
			}
		}

		// recover all our goodies
		if (playersSurvived > 0)
		{
			bool alienAlloysExist = _recoveryStats.ContainsKey((int)ALIEN_ALLOYS);
			foreach (DebriefingStat ds in _stats)
			{
				int aadivider = GetRecoveryDivider(ds.item, alienAlloysExist ? _recoveryStats[(int)ALIEN_ALLOYS].name : null, target, ruleDeploy, _game.Mod.GetItem(ds.item, false));
				ApplyRecoveryDivider(ds, aadivider);

				// recoverable battlescape tiles are now converted to items and put in base inventory
				if (ds.recovery && ds.qty > 0)
				{
					AddItemsToBaseStores(ds.item, @base, ds.qty, false);
				}
			}

			// assuming this was a multi-stage mission,
			// recover everything that was in the craft in the previous stage
			RecoverItems(battle.GuaranteedRecoveredItems, @base, craft);
		}

		// calculate the clips for each type based on the recovered rounds.
		foreach (var pair in _rounds)
		{
			int total_clips = 0;
			if (_game.Mod.StatisticalBulletConservation)
			{
				total_clips = (pair.Value + Rng.Generate(0, pair.Key.ClipSize - 1)) / pair.Key.ClipSize;
			}
			else
			{
				total_clips = pair.Value / pair.Key.ClipSize;
			}
			if (total_clips > 0)
			{
				AddItemsToBaseStores(pair.Key, @base, total_clips, true);
			}
		}

		// calculate the "remaining medikit items" for each type based on the recovered "clips".
		foreach (var pair in _roundsPainKiller)
		{
			int totalRecovered = int.MaxValue;
			int stimulant = _roundsStimulant.GetValueOrDefault(pair.Key);
			int heal = _roundsHeal.GetValueOrDefault(pair.Key);
			if (_game.Mod.StatisticalBulletConservation)
			{
				if (pair.Key.PainKillerQuantity > 0)
					totalRecovered = Math.Min(totalRecovered, (pair.Value + Rng.Generate(0, pair.Key.PainKillerQuantity - 1)) / pair.Key.PainKillerQuantity);
				if (pair.Key.StimulantQuantity > 0)
					totalRecovered = Math.Min(totalRecovered, (stimulant + Rng.Generate(0, pair.Key.StimulantQuantity - 1)) / pair.Key.StimulantQuantity);
				if (pair.Key.HealQuantity > 0)
					totalRecovered = Math.Min(totalRecovered, (heal + Rng.Generate(0, pair.Key.HealQuantity - 1)) / pair.Key.HealQuantity);
			}
			else
			{
				if (pair.Key.PainKillerQuantity > 0)
					totalRecovered = Math.Min(totalRecovered, pair.Value / pair.Key.PainKillerQuantity);
				if (pair.Key.StimulantQuantity > 0)
					totalRecovered = Math.Min(totalRecovered, stimulant / pair.Key.StimulantQuantity);
				if (pair.Key.HealQuantity > 0)
					totalRecovered = Math.Min(totalRecovered, heal / pair.Key.HealQuantity);
			}

			if (totalRecovered > 0)
			{
				AddItemsToBaseStores(pair.Key, @base, totalRecovered, true);
			}
		}

		// reequip craft after a non-base-defense mission (of course only if it's not lost already (that case craft=0))
		if (craft != null)
		{
			ReequipCraft(@base, craft, true);
		}
		else
		{
			if (target != "STR_BASE" || _destroyBase)
			{
				HideSellTransferButtons();
			}
		}

		if (@base != null && target == "STR_BASE")
		{
			AlienMission am = @base.RetaliationMission;
			if (am == null && _region != null)
			{
				// backwards-compatibility
				am = save.FindAlienMission(_region.Rules.Type, MissionObjective.OBJECTIVE_RETALIATION);
			}
			if (!_destroyBase && am != null && am.Rules.IsMultiUfoRetaliation)
			{
				// Remember that more UFOs may be coming (again, just in case)
				am.SetMultiUfoRetaliationInProgress(true);
			}
			else
			{
				// Delete the mission and any live UFOs
				save.DeleteRetaliationMission(am, @base);
			}

			if (!_destroyBase)
			{
				// reequip crafts (only those on the base) after a base defense mission
				foreach (Craft xcraft in @base.Crafts)
				{
					if (xcraft.Status != "STR_OUT")
						ReequipCraft(@base, xcraft, false);
				}
			}
			else if (save.MonthsPassed != -1)
			{
				for (int xbaseIt = 0; xbaseIt < save.Bases.Count; ++xbaseIt)
				{
					Base xbase = save.Bases[xbaseIt];
					if (xbase == @base)
					{
						save.StopHuntingXcomCrafts(xbase); // destroyed together with the base
						@base = null; // To avoid similar (potential) problems as with the deleted craft
						save.Bases.RemoveAt(xbaseIt);
						break;
					}
				}
			}
		}

		if (!_destroyBase)
		{
			// clean up remaining armor backups
			// Note: KIA and MIA soldiers have been handled already, only survivors can have non-empty values
			foreach (Soldier soldier in @base.Soldiers)
			{
				if (soldier.ReplacedArmor != null)
				{
					soldier.SetArmor(soldier.ReplacedArmor);
				}
				else if (soldier.TransformedArmor != null)
				{
					soldier.SetArmor(soldier.TransformedArmor);
				}
				soldier.ReplacedArmor = null;
				soldier.TransformedArmor = null;
			}
		}

		_missionStatistics.success = success;

		if (success && ruleDeploy != null && @base != null)
		{
			// Unlock research defined in alien deployment, if the mission was a success
			RuleResearch research = _game.Mod.GetResearch(ruleDeploy.UnlockedResearchOnSuccess);
			save.HandleResearchUnlockedByMissions(research, _game.Mod, ruleDeploy);

			// Give bounty item defined in alien deployment, if the mission was a success
			RuleItem bountyItem = _game.Mod.GetItem(ruleDeploy.MissionBountyItem);
			if (bountyItem != null)
			{
				int bountyQty = Math.Max(1, ruleDeploy.MissionBountyItemCount);
				AddItemsToBaseStores(bountyItem, @base, bountyQty, true);
				int specialType = bountyItem.SpecialType;
				if (specialType > 1)
				{
					if (_recoveryStats.TryGetValue(specialType, out RecoveryItem recoveryItem))
					{
						AddStat(recoveryItem.name, bountyQty, bountyQty * recoveryItem.value);
					}
				}
			}

			// Increase counters
			save.IncreaseCustomCounter(ruleDeploy.CounterSuccess);
			save.IncreaseCustomCounter(ruleDeploy.CounterAll);
			// Decrease counters
			save.DecreaseCustomCounter(ruleDeploy.DecreaseCounterSuccess);
			save.DecreaseCustomCounter(ruleDeploy.DecreaseCounterAll);

			// Generate a success event
			_eventToSpawn = _game.Mod.GetEvent(ruleDeploy.ChooseSuccessEvent());
		}
		else if (!success && ruleDeploy != null)
		{
			// Unlock research defined in alien deployment, if the mission was a failure
			RuleResearch research = _game.Mod.GetResearch(ruleDeploy.UnlockedResearchOnFailure);
			save.HandleResearchUnlockedByMissions(research, _game.Mod, ruleDeploy);

			// Increase counters
			save.IncreaseCustomCounter(ruleDeploy.CounterFailure);
			save.IncreaseCustomCounter(ruleDeploy.CounterAll);
			// Decrease counters
			save.DecreaseCustomCounter(ruleDeploy.DecreaseCounterFailure);
			save.DecreaseCustomCounter(ruleDeploy.DecreaseCounterAll);

			// Generate a failure event
			_eventToSpawn = _game.Mod.GetEvent(ruleDeploy.ChooseFailureEvent());
		}

		if (ignoredUfo != null)
		{
			if (!success || aborted || playersSurvived <= 0)
			{
				// either "reactivate" the corresponding Ufo
				ignoredUfo.Mission.UfoLifting(ignoredUfo, save);
			}
			else
			{
				// or finally destroy it
				ignoredUfo.Status = Ufo.UfoStatus.DESTROYED;
			}
		}

		// remember the base for later use (of course only if it's not lost already (in that case base=0))
		_base = @base;
	}

	/// <summary>Unloads the fixed ammo of a recovered HWP weapon into the base stores (C++ lambda unloadWeapon).</summary>
	private static void UnloadWeapon(BattleItem weapon, Base @base)
	{
		if (weapon != null)
		{
			RuleItem primaryWeaponRule = weapon.Rules;
			RuleItem fixedAmmoRule = primaryWeaponRule.VehicleClipAmmo;
			if (primaryWeaponRule.VehicleUnit != null && fixedAmmoRule != null)
			{
				BattleItem fixedAmmoItem = weapon.GetAmmoForSlot(primaryWeaponRule.VehicleFixedAmmoSlot);
				if (fixedAmmoItem != null && fixedAmmoItem.AmmoQuantity > 0)
				{
					int total = fixedAmmoItem.AmmoQuantity;

					if (primaryWeaponRule.ClipSize != 0) // meaning this tank can store multiple clips
					{
						total /= fixedAmmoItem.Rules.ClipSize;
					}

					AddItemsToBaseStores(fixedAmmoRule, @base, total, false);
				}
			}
		}
	}

	/// <summary>
	/// Finds the recovery divider for a debriefing stat: alien alloys default (10 for UFOs, 150 otherwise),
	/// overridden by the item's recoveryDividers (deployment type, then mission target, then "STR_OTHER").
	/// </summary>
	/// <param name="statItem">The stat name (= item type for recovery stats).</param>
	/// <param name="alienAlloysName">Name of the ALIEN_ALLOYS recovery item, or null if there is none.</param>
	/// <param name="target">Mission target type ("STR_UFO", "STR_BASE", ...).</param>
	/// <param name="ruleDeploy">The mission's deployment (may be null).</param>
	/// <param name="itemRule">The item rule for the stat (may be null).</param>
	internal static int GetRecoveryDivider(string statItem, string alienAlloysName, string target, AlienDeployment ruleDeploy, RuleItem itemRule)
	{
		// alien alloys recovery values are divided by 10 or divided by 150 in case of an alien base
		int aadivider = 1;
		if (alienAlloysName != null && statItem == alienAlloysName)
		{
			// hardcoded vanilla defaults, in case modders or players fail to install OXCE properly
			aadivider = (target == "STR_UFO") ? 10 : 150;
		}

		if (itemRule != null)
		{
			SortedDictionary<string, int> recoveryDividers = itemRule.RecoveryDividers;
			if (recoveryDividers.Count != 0)
			{
				bool done = false;
				if (ruleDeploy != null)
				{
					// step 1: check deployment
					if (recoveryDividers.TryGetValue(ruleDeploy.Type, out int byDeployment))
					{
						aadivider = byDeployment;
						done = true;
					}
				}
				if (!done)
				{
					// step 2: check mission type
					if (recoveryDividers.TryGetValue(target, out int byTarget))
					{
						aadivider = byTarget;
						done = true;
					}
				}
				if (!done)
				{
					// step 3: check global default
					if (recoveryDividers.TryGetValue("STR_OTHER", out int byDefault))
					{
						aadivider = byDefault;
					}
				}
			}
		}
		return aadivider;
	}

	/// <summary>Divides (positive divider) or multiplies (negative divider) a stat's quantity and score.</summary>
	internal static void ApplyRecoveryDivider(DebriefingStat ds, int aadivider)
	{
		if (aadivider > 1)
		{
			ds.qty = ds.qty / aadivider;
			ds.score = ds.score / aadivider;
		}
		else if (aadivider < -1)
		{
			ds.qty = ds.qty * (-1) * aadivider;
			ds.score = ds.score * (-1) * aadivider;
		}
	}

	/// <summary>Reequips a craft after a mission.</summary>
	/// <param name="base">Base to reequip from.</param>
	/// <param name="craft">Craft to reequip.</param>
	/// <param name="vehicleItemsCanBeDestroyed">Whether we can destroy the vehicles on the craft.</param>
	private void ReequipCraft(Base @base, Craft craft, bool vehicleItemsCanBeDestroyed)
	{
		var craftItemsCopy = new Dictionary<RuleItem, int>(craft.Items.Contents);
		foreach (var pair in craftItemsCopy)
		{
			int qty = @base.StorageItems.GetItem(pair.Key);
			if (qty >= pair.Value)
			{
				@base.StorageItems.RemoveItem(pair.Key, pair.Value);
			}
			else
			{
				int missing = pair.Value - qty;
				@base.StorageItems.RemoveItem(pair.Key, qty);
				craft.Items.RemoveItem(pair.Key, missing);
				ReequipStat stat = new() { item = pair.Key.Type, qty = missing, craft = craft.GetName(_game.Language), listOrder = 0 };
				_missingItems.Add(stat);
			}
		}

		// Now let's see the vehicles
		ItemContainer craftVehicles = new();
		foreach (Vehicle vehicle in craft.Vehicles)
		{
			craftVehicles.AddItem(vehicle.Rules);
		}

		// Now we know how many vehicles (separated by types) we have to read
		// Erase the current vehicles, because we have to reAdd them (cause we want to redistribute their ammo)
		// (C++ deletes the Vehicle objects if vehicleItemsCanBeDestroyed; nothing to free here)
		craft.Vehicles.Clear();

		// Ok, now read those vehicles
		foreach (var pair in craftVehicles.Contents)
		{
			int qty = @base.StorageItems.GetItem(pair.Key);
			RuleItem tankRule = pair.Key;
			int size = tankRule.VehicleUnit.Armor.TotalSize;
			int canBeAdded = Math.Min(qty, pair.Value);
			if (qty < pair.Value)
			{ // missing tanks
				int missing = pair.Value - qty;
				ReequipStat stat = new() { item = pair.Key.Type, qty = missing, craft = craft.GetName(_game.Language), listOrder = 0 };
				_missingItems.Add(stat);
			}
			if (tankRule.VehicleClipAmmo == null)
			{ // so this tank does NOT require ammo
				for (int j = 0; j < canBeAdded; ++j)
				{
					craft.Vehicles.Add(new Vehicle(tankRule, tankRule.VehicleClipSize, size));
				}
				@base.StorageItems.RemoveItem(pair.Key, canBeAdded);
			}
			else
			{ // so this tank requires ammo
				RuleItem ammo = tankRule.VehicleClipAmmo;
				int ammoPerVehicle = tankRule.VehicleClipsLoaded;

				int baqty = @base.StorageItems.GetItem(ammo); // Ammo Quantity for this vehicle-type on the base
				if (baqty < pair.Value * ammoPerVehicle)
				{ // missing ammo
					int missing = (pair.Value * ammoPerVehicle) - baqty;
					ReequipStat stat = new() { item = ammo.Type, qty = missing, craft = craft.GetName(_game.Language), listOrder = 0 };
					_missingItems.Add(stat);
				}
				canBeAdded = Math.Min(canBeAdded, baqty / ammoPerVehicle);
				if (canBeAdded > 0)
				{
					for (int j = 0; j < canBeAdded; ++j)
					{
						craft.Vehicles.Add(new Vehicle(tankRule, tankRule.VehicleClipSize, size));
						@base.StorageItems.RemoveItem(ammo, ammoPerVehicle);
					}
					@base.StorageItems.RemoveItem(pair.Key, canBeAdded);
				}
			}
		}
	}

	/// <summary>Adds item(s) to base stores.</summary>
	/// <param name="ruleItem">Rule of the item(s) to be recovered.</param>
	/// <param name="base">Base to add items to.</param>
	/// <param name="quantity">How many items to recover.</param>
	/// <param name="considerTransformations">Should the items be transformed before recovery?</param>
	private static void AddItemsToBaseStores(RuleItem ruleItem, Base @base, int quantity, bool considerTransformations)
	{
		AddItemsToStores(ruleItem, @base.StorageItems, quantity, considerTransformations);
	}

	/// <summary>Body of addItemsToBaseStores, working on the base's storage container (testable).</summary>
	internal static void AddItemsToStores(RuleItem ruleItem, ItemContainer stores, int quantity, bool considerTransformations)
	{
		if (!considerTransformations)
		{
			stores.AddItem(ruleItem, quantity);
		}
		else
		{
			Dictionary<RuleItem, List<int>> recoveryTransformations = ruleItem.RecoveryTransformations;
			if (recoveryTransformations.Count != 0)
			{
				foreach (var pair in recoveryTransformations)
				{
					if (pair.Value.Count > 1)
					{
						int totalWeight = 0;
						foreach (int it in pair.Value)
						{
							totalWeight += it;
						}
						// roll each item separately
						for (int i = 0; i < quantity; ++i)
						{
							int roll = Rng.Generate(1, totalWeight);
							int runningTotal = 0;
							int position = 0;
							foreach (int it in pair.Value)
							{
								runningTotal += it;
								if (runningTotal >= roll)
								{
									stores.AddItem(pair.Key, position);
									break;
								}
								++position;
							}
						}
					}
					else
					{
						// no RNG
						stores.AddItem(pair.Key, quantity * pair.Value[0]);
					}
				}
			}
			else
			{
				stores.AddItem(ruleItem, quantity);
			}
		}
	}

	/// <summary>Adds item(s) to base stores.</summary>
	/// <param name="itemType">Type of the item(s) to be recovered.</param>
	/// <param name="base">Base to add items to.</param>
	/// <param name="quantity">How many items to recover.</param>
	/// <param name="considerTransformations">Should the items be transformed before recovery?</param>
	private static void AddItemsToBaseStores(string itemType, Base @base, int quantity, bool considerTransformations)
	{
		RuleItem ruleItem = _game.Mod.GetItem(itemType, false);
		if (ruleItem == null)
		{
			Logger.Error($"Failed to add unknown item {itemType}");
			return;
		}

		AddItemsToBaseStores(ruleItem, @base, quantity, considerTransformations);
	}

	private static bool CheckForRecovery(BattleItem item, RuleItem rule)
	{
		return !rule.IsFixed && rule.IsRecoverable && (!rule.IsConsumable || item.FuseTimer < 0);
	}

	private void RecoveryAmmo(BattleItem clip, RuleItem rule, Base @base)
	{
		if (rule.BattleType == BT_AMMO && rule.ClipSize > 0)
		{
			// It's a clip, count any rounds left.
			if (rule.IsAmmoRechargeable)
				_rounds[rule] = _rounds.GetValueOrDefault(rule) + rule.ClipSize; // restore 100% of clip capacity (i.e. the clip will be recharged at the base)
			else
				_rounds[rule] = _rounds.GetValueOrDefault(rule) + clip.AmmoQuantity;
		}
		else
		{
			AddItemsToBaseStores(rule, @base, 1, true);
		}
	}

	private void RecoveryAmmoInWeapon(BattleItem weapon, Base @base)
	{
		// Don't need case of built-in ammo, since this is a fixed weapon
		for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
		{
			BattleItem clip = weapon.GetAmmoForSlot(slot);
			if (clip != null && clip != weapon)
			{
				RuleItem rule = clip.Rules;
				if (CheckForRecovery(clip, rule))
				{
					RecoveryAmmo(clip, rule, @base);
				}
			}
		}
	}

	/// <summary>
	/// Recovers items from the battlescape.
	/// Converts the battlescape inventory into a geoscape item container.
	/// </summary>
	/// <param name="from">Items recovered from the battlescape.</param>
	/// <param name="base">Base to add items to.</param>
	/// <param name="craft">Craft of the mission (may be null).</param>
	private void RecoverItems(List<BattleItem> from, Base @base, Craft craft)
	{
		foreach (BattleItem bi in from)
		{
			RuleItem rule = bi.Rules;
			if (rule.Name == _game.Mod.AlienFuelName)
			{
				// special case of an item counted as a stat
				AddStat(_game.Mod.AlienFuelName, _game.Mod.AlienFuelQuantity, rule.RecoveryPoints);
			}
			else
			{
				if (rule.IsRecoverable && !bi.XCOMProperty)
				{
					if (rule.BattleType == BT_CORPSE)
					{
						BattleUnit corpseUnit = bi.Unit;
						if (corpseUnit.Status == STATUS_DEAD)
						{
							if (rule.IsCorpseRecoverable)
							{
								AddItemsToBaseStores(corpseUnit.Armor.CorpseGeoscape, @base, 1, true);
								AddStat("STR_ALIEN_CORPSES_RECOVERED", 1, bi.Rules.RecoveryPoints);
							}
						}
						else if (corpseUnit.Status == STATUS_UNCONSCIOUS ||
								// or it's in timeout because it's unconscious from the previous stage
								// units can be in timeout and alive, and we assume they flee.
								(corpseUnit.IsIgnored &&
								corpseUnit.Health > 0 &&
								corpseUnit.Health < corpseUnit.Stunlevel))
						{
							if (corpseUnit.OriginalFaction == FACTION_HOSTILE)
							{
								RecoverAlien(corpseUnit, @base, craft);
							}
						}
					}
					// only add recovery points for unresearched items
					else if (!_game.SavedGame.IsResearched(rule.Requirements))
					{
						AddStat("STR_ALIEN_ARTIFACTS_RECOVERED", 1, rule.RecoveryPoints);
					}
					else if (_game.Mod.GiveScoreAlsoForResearchedArtifacts)
					{
						AddStat("STR_ALIEN_ARTIFACTS_RECOVERED", 1, rule.RecoveryPoints);
					}
				}

				// Check if the bodies of our dead soldiers were left, even if we don't recover them
				// This is so we can give them a proper burial... or raise the dead!
				if (bi.Unit != null && bi.Unit.Status == STATUS_DEAD && bi.Unit.GeoscapeSoldier != null)
				{
					bi.Unit.GeoscapeSoldier.SetCorpseRecovered(true);
				}

				// ammo in weapon are handled by weapon itself.
				if (bi.IsAmmo)
				{
					// noting
				}
				// put items back in the base
				else if (CheckForRecovery(bi, rule))
				{
					bool recoverWeapon = true;
					switch (rule.BattleType)
					{
						case BT_CORPSE: // corpses are handled above, do not process them here.
							break;
						case BT_MEDIKIT:
							if (rule.IsConsumable)
							{
								// Need to remember all three!
								_roundsPainKiller[rule] = _roundsPainKiller.GetValueOrDefault(rule) + bi.PainKillerQuantity;
								_roundsStimulant[rule] = _roundsStimulant.GetValueOrDefault(rule) + bi.StimulantQuantity;
								_roundsHeal[rule] = _roundsHeal.GetValueOrDefault(rule) + bi.HealQuantity;
							}
							else
							{
								// Vanilla behaviour (recover a full medikit).
								AddItemsToBaseStores(rule, @base, 1, true);
							}
							break;
						case BT_AMMO:
							RecoveryAmmo(bi, rule, @base);
							break;
						case BT_FIREARM:
						case BT_MELEE:
							// Special case: built-in ammo (e.g. throwing knives or bamboo stick)
							if (!bi.NeedsAmmoForSlot(0) && rule.ClipSize > 0)
							{
								if (rule.IsAmmoRechargeable)
									_rounds[rule] = _rounds.GetValueOrDefault(rule) + rule.ClipSize; // restore 100% of clip capacity (i.e. the clip will be recharged at the base)
								else
									_rounds[rule] = _rounds.GetValueOrDefault(rule) + bi.AmmoQuantity;
								recoverWeapon = false;
							}
							// It's a weapon, count any rounds left in the clip.
							RecoveryAmmoInWeapon(bi, @base);
							// Fall-through, to recover the weapon itself.
							if (recoverWeapon)
							{
								AddItemsToBaseStores(rule, @base, 1, true);
							}
							break;
						default:
							if (recoverWeapon)
							{
								AddItemsToBaseStores(rule, @base, 1, true);
							}
							break;
					}
					if (rule.BattleType == BT_NONE)
					{
						foreach (Craft xcraft in @base.Crafts)
						{
							xcraft.ReuseItem(rule);
						}
					}
				}
				// special case of fixed weapons on a soldier's armor (and HWPs, but only non-fixed ammo)
				// makes sure we recover the ammunition from this weapon
				else if (rule.IsFixed && bi.Owner != null && bi.Owner.OriginalFaction == FACTION_PLAYER)
				{
					switch (rule.BattleType)
					{
						case BT_FIREARM:
						case BT_MELEE:
							if (bi.Owner.GeoscapeSoldier != null)
							{
								// It's a weapon, count any rounds left in the clip.
								RecoveryAmmoInWeapon(bi, @base);
							}
							else
							{
								BattleItem hwpFixedAmmoItem = null;
								if (rule.VehicleUnit != null && rule.VehicleClipAmmo != null)
								{
									// remove fixed ammo (it will be recovered later elsewhere)
									hwpFixedAmmoItem = bi.SetAmmoForSlot(rule.VehicleFixedAmmoSlot, null);
								}

								// recover the rest (i.e. non-fixed ammo)
								RecoveryAmmoInWeapon(bi, @base);

								if (hwpFixedAmmoItem != null)
								{
									// put fixed ammo back in
									bi.SetAmmoForSlot(rule.VehicleFixedAmmoSlot, hwpFixedAmmoItem);
								}
							}
							break;
						default:
							break;
					}
				}
			}
		}
	}

	/// <summary>Recovers a live civilian from the battlescape.</summary>
	/// <param name="from">Battle unit to recover.</param>
	/// <param name="base">Base to add items to.</param>
	/// <param name="craft">Craft of the mission (may be null).</param>
	private void RecoverCivilian(BattleUnit from, Base @base, Craft craft)
	{
		Unit rule = from.UnitRules;
		if (rule.IsRecoverableAsCivilian == false)
		{
			return;
		}
		if (rule.IsRecoverableAsScientist)
		{
			Transfer t = new(24);
			t.SetScientists(1);
			@base.Transfers.Add(t);
		}
		else if (rule.IsRecoverableAsEngineer)
		{
			Transfer t = new(24);
			t.SetEngineers(1);
			@base.Transfers.Add(t);
		}
		else
		{
			RuleSoldier ruleSoldier = rule.CivilianRecoverySoldierType;
			if (ruleSoldier != null)
			{
				Transfer t = new(24);
				Target target = craft != null ? craft : @base;
				int nationality = _game.SavedGame.SelectSoldierNationalityByLocation(_game.Mod, ruleSoldier, target);
				Soldier s = _game.Mod.GenSoldier(_game.SavedGame, ruleSoldier, nationality);
				YamlRootNodeReader reader = new(from.UnitRules.SpawnedSoldierTemplate, "(spawned soldier template)");
				s.Load(reader.ToBase(), _game.Mod, _game.SavedGame, true); // load from soldier template
				if (!string.IsNullOrEmpty(from.UnitRules.SpawnedPersonName))
				{
					s.SetName(Tr(from.UnitRules.SpawnedPersonName));
				}
				else
				{
					s.GenName();
				}
				t.Soldier = s;
				@base.Transfers.Add(t);
			}
			else
			{
				RuleItem ruleItem = rule.CivilianRecoveryItemType;
				if (ruleItem != null)
				{
					if (!ruleItem.IsAlien)
					{
						AddItemsToBaseStores(ruleItem, @base, 1, true);
					}
					else
					{
						RuleItem ruleLiveAlienItem = ruleItem;
						bool killPrisonersAutomatically = @base.GetAvailableContainment(ruleLiveAlienItem.PrisonType) == 0;
						if (killPrisonersAutomatically)
						{
							// check also other bases, maybe we can transfer/redirect prisoners there
							foreach (Base xbase in _game.SavedGame.Bases)
							{
								if (xbase.GetAvailableContainment(ruleLiveAlienItem.PrisonType) > 0)
								{
									killPrisonersAutomatically = false;
									break;
								}
							}
						}
						if (killPrisonersAutomatically)
						{
							_containmentStateInfo[ruleLiveAlienItem.PrisonType] = 1; // 1 = not available in any base
						}
						else
						{
							AddItemsToBaseStores(ruleLiveAlienItem, @base, 1, false);
							int availableContainment = @base.GetAvailableContainment(ruleLiveAlienItem.PrisonType);
							int usedContainment = @base.GetUsedContainment(ruleLiveAlienItem.PrisonType);
							int freeContainment = availableContainment - (usedContainment * _limitsEnforced);
							// no capacity, or not enough capacity
							if (availableContainment == 0 || freeContainment < 0)
							{
								_containmentStateInfo[ruleLiveAlienItem.PrisonType] = 2; // 2 = overfull
							}
						}
					}
				}
			}
		}
	}

	/// <summary>Recovers a live alien from the battlescape.</summary>
	/// <param name="from">Battle unit to recover.</param>
	/// <param name="base">Base to add items to.</param>
	/// <param name="craft">Craft of the mission (may be null).</param>
	private void RecoverAlien(BattleUnit from, Base @base, Craft craft)
	{
		// Transform a live alien into one or more recovered items?
		RuleItem ruleLiveAlienItem = from.UnitRules.LiveAlienGeoscape;
		if (ruleLiveAlienItem != null && ruleLiveAlienItem.RecoveryTransformations.Count != 0)
		{
			AddItemsToBaseStores(ruleLiveAlienItem, @base, 1, true);

			// Ignore everything else, e.g. no points for live/dead aliens (since you did NOT recover them)
			// Also no points or anything else for the recovered items
			return;
		}

		if (ruleLiveAlienItem == null)
		{
			if (from.UnitRules.IsRecoverableAsCivilian)
			{
				RecoverCivilian(from, @base, craft);
				return;
			}

			// This ain't good! Let's display at least some useful info before we crash...
			StringBuilder ss = new();
			ss.Append(CultureInfo.InvariantCulture, $"Live alien item definition is missing. Unit ID = {from.Id}");
			ss.Append(CultureInfo.InvariantCulture, $"; Type = {from.Type}");
			ss.Append(CultureInfo.InvariantCulture, $"; Status = {(int)from.Status}");
			ss.Append(CultureInfo.InvariantCulture, $"; Faction = {(int)from.Faction}");
			ss.Append(CultureInfo.InvariantCulture, $"; Orig. faction = {(int)from.OriginalFaction}");
			if (from.SpawnUnit != null)
			{
				ss.Append(CultureInfo.InvariantCulture, $"; Spawn unit = [{from.SpawnUnit.Type}]");
			}
			ss.Append(CultureInfo.InvariantCulture, $"; isSurrendering = {(from.IsSurrendering ? 1 : 0)}");
			throw new InvalidOperationException(ss.ToString());
		}

		bool killPrisonersAutomatically = @base.GetAvailableContainment(ruleLiveAlienItem.PrisonType) == 0;
		if (killPrisonersAutomatically)
		{
			// check also other bases, maybe we can transfer/redirect prisoners there
			foreach (Base xbase in _game.SavedGame.Bases)
			{
				if (xbase.GetAvailableContainment(ruleLiveAlienItem.PrisonType) > 0)
				{
					killPrisonersAutomatically = false;
					break;
				}
			}
		}
		if (killPrisonersAutomatically)
		{
			_containmentStateInfo[ruleLiveAlienItem.PrisonType] = 1; // 1 = not available in any base

			if (from.Armor.CorpseBattlescape.Count != 0)
			{
				RuleItem corpseRule = from.Armor.CorpseBattlescape[0];
				if (corpseRule != null && corpseRule.IsRecoverable)
				{
					if (corpseRule.IsCorpseRecoverable)
					{
						AddStat("STR_ALIEN_CORPSES_RECOVERED", 1, corpseRule.RecoveryPoints);
						RuleItem corpseItem = from.Armor.CorpseGeoscape;
						AddItemsToBaseStores(corpseItem, @base, 1, true);
					}
				}
			}
		}
		else
		{
			RuleResearch research = _game.Mod.GetResearch(from.UnitRules.Type);
			bool surrendered = (!from.IsOut || from.IsIgnored)
				&& (from.IsSurrendering || _game.SavedGame.SavedBattle.ChronoTrigger == ChronoTrigger.FORCE_WIN_SURRENDER);
			if (research != null && !_game.SavedGame.IsResearched(research))
			{
				// more points if it's not researched
				AddStat(surrendered ? "STR_LIVE_ALIENS_SURRENDERED" : "STR_LIVE_ALIENS_RECOVERED", 1, from.Value * 2);
			}
			else if (_game.Mod.GiveScoreAlsoForResearchedArtifacts)
			{
				AddStat(surrendered ? "STR_LIVE_ALIENS_SURRENDERED" : "STR_LIVE_ALIENS_RECOVERED", 1, from.Value * 2);
			}
			else
			{
				// 10 points for recovery
				AddStat(surrendered ? "STR_LIVE_ALIENS_SURRENDERED" : "STR_LIVE_ALIENS_RECOVERED", 1, 10);
			}

			AddItemsToBaseStores(ruleLiveAlienItem, @base, 1, false);
			int availableContainment = @base.GetAvailableContainment(ruleLiveAlienItem.PrisonType);
			int usedContainment = @base.GetUsedContainment(ruleLiveAlienItem.PrisonType);
			int freeContainment = availableContainment - (usedContainment * _limitsEnforced);
			// no capacity, or not enough capacity
			if (availableContainment == 0 || freeContainment < 0)
			{
				_containmentStateInfo[ruleLiveAlienItem.PrisonType] = 2; // 2 = overfull
			}
		}
	}

	/// <summary>Gets the number of recovered items of certain type.</summary>
	public int GetRecoveredItemCount(RuleItem rule)
	{
		if (rule != null && _recoveredItems.TryGetValue(rule, out int count))
			return count;

		return 0;
	}

	/// <summary>Gets the total number of recovered items.</summary>
	public int TotalRecoveredItemCount
	{
		get
		{
			int total = 0;
			foreach (var item in _recoveredItems)
			{
				total += item.Value;
			}
			return total;
		}
	}

	/// <summary>Decreases the number of recovered items by the sold/transferred amount.</summary>
	public void DecreaseRecoveredItemCount(RuleItem rule, int amount)
	{
		if (rule != null && _recoveredItems.TryGetValue(rule, out int count))
		{
			_recoveredItems[rule] = Math.Max(0, count - amount);
		}
	}

	/// <summary>Hides the SELL and TRANSFER buttons.</summary>
	public void HideSellTransferButtons()
	{
		_showSellButton = false;
		_btnSell.Visible = _showSellButton;
		_btnTransfer.Visible = _showSellButton;
	}
}
