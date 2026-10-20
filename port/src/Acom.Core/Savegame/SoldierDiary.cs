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
// Ported from: src/Savegame/SoldierDiary.h, src/Savegame/SoldierDiary.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Each entry will be its own commendation.
/// </summary>
public class SoldierCommendations
{
	private string _type = "", _noun = "";
	private int _decorationLevel;
	private bool _isNew;
	private readonly RuleCommendations _rule;

	/// <summary>Initializes a new commendation entry from YAML.</summary>
	public SoldierCommendations(YamlNodeReader reader, Mod mod)
	{
		Load(reader);
		_rule = mod.GetCommendation(_type, false); //TODO: during load we can load obsolete value, some else need cleanup
	}

	/// <summary>Initializes a soldier commendation.</summary>
	public SoldierCommendations(string commendationName, string noun, Mod mod)
	{
		_type = commendationName;
		_noun = noun;
		_decorationLevel = 0;
		_isNew = true;
		_rule = mod.GetCommendation(_type, true); //if there is no commendation rule then someone messed up
	}

	/// <summary>Loads the commendation from a YAML file.</summary>
	public void Load(YamlNodeReader reader)
	{
		reader.TryRead("commendationName", ref _type);
		reader.ReadNode("noun", ref _noun, "noNoun");
		reader.TryRead("decorationLevel", ref _decorationLevel);
		reader.ReadNode("isNew", ref _isNew, false);
	}

	/// <summary>Saves the commendation to a YAML file.</summary>
	public void Save(YamlNodeWriter writer)
	{
		writer.SetAsMap();
		writer.SetFlowStyle();
		writer.Write("commendationName", _type);
		if (_noun != "noNoun")
			writer.Write("noun", _noun);
		writer.Write("decorationLevel", _decorationLevel);
	}

	/// <summary>Get commendation rule config.</summary>
	public RuleCommendations Rule => _rule;

	/// <summary>Get the soldier's commendation's name.</summary>
	public string Type => _type;

	/// <summary>Get the soldier's commendation's noun.</summary>
	public string Noun => _noun;

	/// <summary>Get the soldier commendation level's name.</summary>
	public string GetDecorationLevelName(int skipCounter) => "STR_AWARD_" + (_decorationLevel - skipCounter);

	/// <summary>Get the soldier commendation level's description.</summary>
	public string DecorationDescription => "STR_AWARD_DECOR_" + _decorationLevel;

	/// <summary>Get the soldier commendation level's int.</summary>
	public int DecorationLevelInt => _decorationLevel;

	/// <summary>Get newness of commendation.</summary>
	public bool IsNew => _isNew;

	/// <summary>Set the newness of the commendation to old.</summary>
	public void MakeOld()
	{
		_isNew = false;
	}

	/// <summary>Add a level of decoration to the commendation. Sets isNew to true.</summary>
	public void AddDecoration()
	{
		_decorationLevel++;
		_isNew = true;
	}
}

/// <summary>
/// The soldier's diary: career statistics, kill list and commendations.
/// </summary>
public class SoldierDiary
{
	private static readonly string[] battleTypeArray = { "BT_NONE", "BT_FIREARM", "BT_AMMO", "BT_MELEE", "BT_GRENADE",
		"BT_PROXIMITYGRENADE", "BT_MEDIKIT", "BT_SCANNER", "BT_MINDPROBE", "BT_PSIAMP", "BT_FLARE", "BT_CORPSE", "BT_END" };
	private static readonly string[] damageTypeArray = { "DT_NONE", "DT_AP", "DT_IN", "DT_HE", "DT_LASER", "DT_PLASMA",
		"DT_STUN", "DT_MELEE", "DT_ACID", "DT_SMOKE",
		"DT_10", "DT_11", "DT_12", "DT_13", "DT_14", "DT_15", "DT_16", "DT_17", "DT_18", "DT_19", "DT_END" };

	private readonly List<SoldierCommendations> _commendations = [];
	private readonly List<BattleUnitKills> _killList = [];
	private List<int> _missionIdList = [];
	private int _daysWoundedTotal, _totalShotByFriendlyCounter, _totalShotFriendlyCounter, _loneSurvivorTotal, _monthsService, _unconciousTotal, _shotAtCounterTotal,
		_hitCounterTotal, _ironManTotal, _longDistanceHitCounterTotal, _lowAccuracyHitCounterTotal, _shotsFiredCounterTotal, _shotsLandedCounterTotal,
		_shotAtCounter10in1Mission, _hitCounter5in1Mission, _timesWoundedTotal, _KIA, _allAliensKilledTotal, _allAliensStunnedTotal,
		_woundsHealedTotal, _allUFOs, _allMissionTypes, _statGainTotal, _revivedUnitTotal, _wholeMedikitTotal, _braveryGainTotal, _bestOfRank, _MIA,
		_martyrKillsTotal, _postMortemKills, _slaveKillsTotal, _bestSoldier, _revivedSoldierTotal, _revivedHostileTotal, _revivedNeutralTotal;
	private bool _globeTrotter;

	/// <summary>Initializes a new blank diary.</summary>
	public SoldierDiary()
	{
	}

	/// <summary>Loads the diary from a YAML file.</summary>
	public void Load(YamlNodeReader node, Mod mod)
	{
		var reader = node.UseIndex();
		foreach (var commendation in reader["commendations"].Children())
		{
			var sc = new SoldierCommendations(commendation, mod);
			if (sc.Rule != null)
			{
				_commendations.Add(sc);
			}
			// else: obsolete commendation, ignore it... otherwise it would cause a crash later
		}
		foreach (var battleUnitKillReader in reader["killList"].Children())
			_killList.Add(new BattleUnitKills(battleUnitKillReader));
		reader.TryRead("missionIdList", ref _missionIdList);
		reader.TryRead("daysWoundedTotal", ref _daysWoundedTotal);
		reader.TryRead("totalShotByFriendlyCounter", ref _totalShotByFriendlyCounter);
		reader.TryRead("totalShotFriendlyCounter", ref _totalShotFriendlyCounter);
		reader.TryRead("loneSurvivorTotal", ref _loneSurvivorTotal);
		reader.TryRead("monthsService", ref _monthsService);
		reader.TryRead("unconciousTotal", ref _unconciousTotal);
		reader.TryRead("shotAtCounterTotal", ref _shotAtCounterTotal);
		reader.TryRead("hitCounterTotal", ref _hitCounterTotal);
		reader.TryRead("ironManTotal", ref _ironManTotal);
		reader.TryRead("longDistanceHitCounterTotal", ref _longDistanceHitCounterTotal);
		reader.TryRead("lowAccuracyHitCounterTotal", ref _lowAccuracyHitCounterTotal);
		reader.TryRead("shotsFiredCounterTotal", ref _shotsFiredCounterTotal);
		reader.TryRead("shotsLandedCounterTotal", ref _shotsLandedCounterTotal);
		reader.TryRead("shotAtCounter10in1Mission", ref _shotAtCounter10in1Mission);
		reader.TryRead("hitCounter5in1Mission", ref _hitCounter5in1Mission);
		reader.TryRead("timesWoundedTotal", ref _timesWoundedTotal);
		reader.TryRead("killedInAction", ref _KIA);
		reader.TryRead("allAliensKilledTotal", ref _allAliensKilledTotal);
		reader.TryRead("allAliensStunnedTotal", ref _allAliensStunnedTotal);
		reader.TryRead("woundsHealedTotal", ref _woundsHealedTotal);
		reader.TryRead("allUFOs", ref _allUFOs);
		reader.TryRead("allMissionTypes", ref _allMissionTypes);
		reader.TryRead("statGainTotal", ref _statGainTotal);
		reader.TryRead("revivedUnitTotal", ref _revivedUnitTotal);
		reader.TryRead("revivedSoldierTotal", ref _revivedSoldierTotal);
		reader.TryRead("revivedHostileTotal", ref _revivedHostileTotal);
		reader.TryRead("revivedNeutralTotal", ref _revivedNeutralTotal);
		reader.TryRead("wholeMedikitTotal", ref _wholeMedikitTotal);
		reader.TryRead("braveryGainTotal", ref _braveryGainTotal);
		reader.TryRead("bestOfRank", ref _bestOfRank);
		reader.TryRead("missingInAction", ref _MIA);
		reader.TryRead("bestSoldier", ref _bestSoldier);
		reader.TryRead("martyrKillsTotal", ref _martyrKillsTotal);
		reader.TryRead("postMortemKills", ref _postMortemKills);
		reader.TryRead("globeTrotter", ref _globeTrotter);
		reader.TryRead("slaveKillsTotal", ref _slaveKillsTotal);
	}

	/// <summary>Saves the diary to a YAML file.</summary>
	public void Save(YamlNodeWriter writer)
	{
		writer.SetAsMap();
		writer.Write("commendations", _commendations, (w, c) => c.Save(w.Write()));
		writer.Write("killList", _killList, (w, b) => b.Save(w.Write()));
		if (_missionIdList.Count != 0)
			writer.Write("missionIdList", _missionIdList).SetFlowStyle();
		if (_daysWoundedTotal != 0) writer.Write("daysWoundedTotal", _daysWoundedTotal);
		if (_totalShotByFriendlyCounter != 0) writer.Write("totalShotByFriendlyCounter", _totalShotByFriendlyCounter);
		if (_totalShotFriendlyCounter != 0) writer.Write("totalShotFriendlyCounter", _totalShotFriendlyCounter);
		if (_loneSurvivorTotal != 0) writer.Write("loneSurvivorTotal", _loneSurvivorTotal);
		if (_monthsService != 0) writer.Write("monthsService", _monthsService);
		if (_unconciousTotal != 0) writer.Write("unconciousTotal", _unconciousTotal);
		if (_shotAtCounterTotal != 0) writer.Write("shotAtCounterTotal", _shotAtCounterTotal);
		if (_hitCounterTotal != 0) writer.Write("hitCounterTotal", _hitCounterTotal);
		if (_ironManTotal != 0) writer.Write("ironManTotal", _ironManTotal);
		if (_longDistanceHitCounterTotal != 0) writer.Write("longDistanceHitCounterTotal", _longDistanceHitCounterTotal);
		if (_lowAccuracyHitCounterTotal != 0) writer.Write("lowAccuracyHitCounterTotal", _lowAccuracyHitCounterTotal);
		if (_shotsFiredCounterTotal != 0) writer.Write("shotsFiredCounterTotal", _shotsFiredCounterTotal);
		if (_shotsLandedCounterTotal != 0) writer.Write("shotsLandedCounterTotal", _shotsLandedCounterTotal);
		if (_shotAtCounter10in1Mission != 0) writer.Write("shotAtCounter10in1Mission", _shotAtCounter10in1Mission);
		if (_hitCounter5in1Mission != 0) writer.Write("hitCounter5in1Mission", _hitCounter5in1Mission);
		if (_timesWoundedTotal != 0) writer.Write("timesWoundedTotal", _timesWoundedTotal);
		if (_KIA != 0) writer.Write("killedInAction", _KIA);
		if (_allAliensKilledTotal != 0) writer.Write("allAliensKilledTotal", _allAliensKilledTotal);
		if (_allAliensStunnedTotal != 0) writer.Write("allAliensStunnedTotal", _allAliensStunnedTotal);
		if (_woundsHealedTotal != 0) writer.Write("woundsHealedTotal", _woundsHealedTotal);
		if (_allUFOs != 0) writer.Write("allUFOs", _allUFOs);
		if (_allMissionTypes != 0) writer.Write("allMissionTypes", _allMissionTypes);
		if (_statGainTotal != 0) writer.Write("statGainTotal", _statGainTotal);
		if (_revivedUnitTotal != 0) writer.Write("revivedUnitTotal", _revivedUnitTotal);
		if (_revivedSoldierTotal != 0) writer.Write("revivedSoldierTotal", _revivedSoldierTotal);
		if (_revivedHostileTotal != 0) writer.Write("revivedHostileTotal", _revivedHostileTotal);
		if (_revivedNeutralTotal != 0) writer.Write("revivedNeutralTotal", _revivedNeutralTotal);
		if (_wholeMedikitTotal != 0) writer.Write("wholeMedikitTotal", _wholeMedikitTotal);
		if (_braveryGainTotal != 0) writer.Write("braveryGainTotal", _braveryGainTotal);
		if (_bestOfRank != 0) writer.Write("bestOfRank", _bestOfRank);
		if (_MIA != 0) writer.Write("missingInAction", _MIA);
		if (_bestSoldier != 0) writer.Write("bestSoldier", _bestSoldier);
		if (_martyrKillsTotal != 0) writer.Write("martyrKillsTotal", _martyrKillsTotal);
		if (_postMortemKills != 0) writer.Write("postMortemKills", _postMortemKills);
		if (_globeTrotter) writer.Write("globeTrotter", _globeTrotter);
		if (_slaveKillsTotal != 0) writer.Write("slaveKillsTotal", _slaveKillsTotal);
	}

	/// <summary>Update soldier diary statistics.</summary>
	/// <param name="unitStatistics">BattleUnitStatistics to get stats from (its kills are moved into the diary).</param>
	/// <param name="allMissionStatistics">MissionStatistics to get stats from.</param>
	/// <param name="rules">The mod.</param>
	public void UpdateDiary(BattleUnitStatistics unitStatistics, List<MissionStatistics> allMissionStatistics, Mod rules)
	{
		if (allMissionStatistics.Count == 0) return;
		var missionStatistics = allMissionStatistics[^1];
		_missionIdList.Add(missionStatistics.id);
		var unitKills = unitStatistics.kills;
		foreach (var buk in unitKills)
		{
			buk.MakeTurnUnique();
			_killList.Add(buk);
		}
		unitKills.Clear();
		if (missionStatistics.success)
		{
			if (unitStatistics.loneSurvivor)
				_loneSurvivorTotal++;
			if (unitStatistics.ironMan)
				_ironManTotal++;
			if (unitStatistics.nikeCross)
				_allAliensKilledTotal++;
			if (unitStatistics.mercyCross)
				_allAliensStunnedTotal++;
		}
		_daysWoundedTotal += unitStatistics.daysWounded;
		if (unitStatistics.daysWounded != 0)
			_timesWoundedTotal++;

		if (unitStatistics.wasUnconcious)
			_unconciousTotal++;
		_shotAtCounterTotal += unitStatistics.shotAtCounter;
		_shotAtCounter10in1Mission += (unitStatistics.shotAtCounter) / 10;
		_hitCounterTotal += unitStatistics.hitCounter;
		_hitCounter5in1Mission += (unitStatistics.hitCounter) / 5;
		_totalShotByFriendlyCounter += unitStatistics.shotByFriendlyCounter;
		_totalShotFriendlyCounter += unitStatistics.shotFriendlyCounter;
		_longDistanceHitCounterTotal += unitStatistics.longDistanceHitCounter;
		_lowAccuracyHitCounterTotal += unitStatistics.lowAccuracyHitCounter;
		_shotsFiredCounterTotal += unitStatistics.shotsFiredCounter;
		_shotsLandedCounterTotal += unitStatistics.shotsLandedCounter;
		if (unitStatistics.KIA)
			_KIA++;
		if (unitStatistics.MIA)
			_MIA++;
		_woundsHealedTotal += unitStatistics.woundsHealed;
		if (GetUFOTotal(allMissionStatistics).Count >= rules.UfosList.Count)
			_allUFOs = 1;
		// C++ computes the right side in size_t; with fewer than 2 entries it wraps and never matches, like the negative int here
		if ((GetUFOTotal(allMissionStatistics).Count + GetTypeTotal(allMissionStatistics).Count) == (rules.UfosList.Count + rules.DeploymentsList.Count - 2))
			_allMissionTypes = 1;
		if (GetCountryTotal(allMissionStatistics).Count == 1 + rules.CountriesList.Count)
			_globeTrotter = true;
		_martyrKillsTotal += unitStatistics.martyr;
		_slaveKillsTotal += unitStatistics.slaveKills;

		// Stat change long hand calculation
		_statGainTotal = 0; // Reset.
		_statGainTotal += unitStatistics.delta.tu;
		_statGainTotal += unitStatistics.delta.stamina;
		_statGainTotal += unitStatistics.delta.health;
		_statGainTotal += unitStatistics.delta.bravery / 10; // Normalize
		_statGainTotal += unitStatistics.delta.reactions;
		_statGainTotal += unitStatistics.delta.firing;
		_statGainTotal += unitStatistics.delta.throwing;
		_statGainTotal += unitStatistics.delta.strength;
		_statGainTotal += unitStatistics.delta.mana;
		_statGainTotal += unitStatistics.delta.psiStrength;
		_statGainTotal += unitStatistics.delta.melee;
		_statGainTotal += unitStatistics.delta.psiSkill;

		_braveryGainTotal = unitStatistics.delta.bravery;
		_revivedUnitTotal += (unitStatistics.revivedSoldier + unitStatistics.revivedHostile + unitStatistics.revivedNeutral);
		_revivedSoldierTotal += unitStatistics.revivedSoldier;
		_revivedNeutralTotal += unitStatistics.revivedNeutral;
		_revivedHostileTotal += unitStatistics.revivedHostile;
		_wholeMedikitTotal += Math.Min(Math.Min(unitStatistics.woundsHealed, unitStatistics.appliedStimulant), unitStatistics.appliedPainKill);
	}

	/// <summary>Get soldier commendations.</summary>
	public List<SoldierCommendations> SoldierCommendations => _commendations;

	/// <summary>Checks whether the diary contains a given commendation.</summary>
	public bool ContainsCommendation(RuleCommendations rule)
	{
		foreach (var comm in _commendations)
		{
			if (comm.Rule == rule)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Manage the soldier's commendations.
	/// Award new ones, if deserved.
	/// </summary>
	/// <returns>Has a commendation been awarded?</returns>
	public bool ManageCommendations(Mod mod, SavedGame save, Soldier soldier)
	{
		List<MissionStatistics> missionStatistics = save.MissionStatistics;

		const int BATTLE_TYPES = 13;
		const int DAMAGE_TYPES = 21;

		// std::map iteration; the iterator only advances when no commendation was awarded
		var commendationsList = mod.CommendationsList.ToList();
		bool ret = false;                                   // This value is returned if at least one commendation was given.
		var nextCommendationLevel = new Dictionary<string, int>(StringComparer.Ordinal); // Noun, threshold.
		var modularCommendations = new List<string>();      // Noun.
		bool awardCommendationBool;                         // This value determines if a commendation will be given.
		// Loop over all possible commendations
		for (int iter = 0; iter < commendationsList.Count; )
		{
			string commType = commendationsList[iter].Key;
			RuleCommendations commRule = commendationsList[iter].Value;

			if (!commRule.IsSupportedBy(soldier.Rules))
			{
				// commendation does not apply to this soldier type
				++iter;
				continue;
			}
			if (!save.IsResearched(commRule.Requires, false))
			{
				// commendation is not unlocked yet
				++iter;
				continue;
			}

			awardCommendationBool = true;
			nextCommendationLevel.Clear();
			nextCommendationLevel["noNoun"] = 0;
			modularCommendations.Clear();
			// Loop over all the soldier's commendations, see if we already have the commendation.
			// If so, get the level and noun.
			foreach (var sc in _commendations)
			{
				if (commType == sc.Type)
				{
					nextCommendationLevel[sc.Noun] = sc.DecorationLevelInt + 1;
				}
			}
			// Go through each possible criteria. Assume the medal is awarded, set to false if not.
			// As soon as we find a medal criteria that we FAIL TO achieve, then we are not awarded a medal.
			foreach (var critPair in commRule.Criteria)
			{
				string critName = critPair.Key;
				List<int> critDef = critPair.Value;
				int nextLevel = nextCommendationLevel["noNoun"];
				int nextLevelThreshold = (nextLevel >= critDef.Count) ? 0 /* just a dummy */ : critDef[nextLevel];

				// Skip this medal if we have reached its max award level.
				if (nextLevel >= critDef.Count)
				{
					awardCommendationBool = false;
					break;
				}
				// These criteria have no nouns, so only the nextCommendationLevel["noNoun"] will ever be used.
				else if
				(
					nextCommendationLevel.ContainsKey("noNoun") &&
					(
						(critName == "totalKills" && KillTotal < nextLevelThreshold) ||
						(critName == "totalMissions" && GetMissionTotalFiltered(missionStatistics, commRule) < nextLevelThreshold) ||
						(critName == "totalWins" && GetWinTotal(missionStatistics) < nextLevelThreshold) ||
						(critName == "totalScore" && GetScoreTotal(missionStatistics) < nextLevelThreshold) ||
						(critName == "totalStuns" && StunTotal < nextLevelThreshold) ||
						(critName == "totalDaysWounded" && _daysWoundedTotal < nextLevelThreshold) ||
						(critName == "totalBaseDefenseMissions" && GetBaseDefenseMissionTotal(missionStatistics) < nextLevelThreshold) ||
						(critName == "totalTerrorMissions" && GetTerrorMissionTotal(missionStatistics) < nextLevelThreshold) ||
						(critName == "totalNightMissions" && GetNightMissionTotal(missionStatistics, mod) < nextLevelThreshold) ||
						(critName == "totalNightTerrorMissions" && GetNightTerrorMissionTotal(missionStatistics, mod) < nextLevelThreshold) ||
						(critName == "totalMonthlyService" && _monthsService < nextLevelThreshold) ||
						(critName == "totalFellUnconcious" && _unconciousTotal < nextLevelThreshold) ||
						(critName == "totalShotAt10Times" && _shotAtCounter10in1Mission < nextLevelThreshold) ||
						(critName == "totalHit5Times" && _hitCounter5in1Mission < nextLevelThreshold) ||
						(critName == "totalFriendlyFired" && (_totalShotByFriendlyCounter < nextLevelThreshold || _KIA != 0 || _MIA != 0)) ||
						(critName == "total_lone_survivor" && _loneSurvivorTotal < nextLevelThreshold) ||
						(critName == "totalIronMan" && _ironManTotal < nextLevelThreshold) ||
						(critName == "totalImportantMissions" && GetImportantMissionTotal(missionStatistics) < nextLevelThreshold) ||
						(critName == "totalLongDistanceHits" && _longDistanceHitCounterTotal < nextLevelThreshold) ||
						(critName == "totalLowAccuracyHits" && _lowAccuracyHitCounterTotal < nextLevelThreshold) ||
						(critName == "totalReactionFire" && GetReactionFireKillTotal(mod) < nextLevelThreshold) ||
						(critName == "totalTimesWounded" && _timesWoundedTotal < nextLevelThreshold) ||
						(critName == "totalDaysWounded" && _daysWoundedTotal < nextLevelThreshold) ||
						(critName == "totalValientCrux" && GetValiantCruxTotal(missionStatistics) < nextLevelThreshold) ||
						(critName == "isDead" && _KIA < nextLevelThreshold) ||
						(critName == "totalTrapKills" && GetTrapKillTotal(mod) < nextLevelThreshold) ||
						(critName == "totalAlienBaseAssaults" && GetAlienBaseAssaultTotal(missionStatistics) < nextLevelThreshold) ||
						(critName == "totalAllAliensKilled" && _allAliensKilledTotal < nextLevelThreshold) ||
						(critName == "totalAllAliensStunned" && _allAliensStunnedTotal < nextLevelThreshold) ||
						(critName == "totalWoundsHealed" && _woundsHealedTotal < nextLevelThreshold) ||
						(critName == "totalAllUFOs" && _allUFOs < nextLevelThreshold) ||
						(critName == "totalAllMissionTypes" && _allMissionTypes < nextLevelThreshold) ||
						(critName == "totalStatGain" && _statGainTotal < nextLevelThreshold) ||
						(critName == "totalRevives" && _revivedUnitTotal < nextLevelThreshold) ||
						(critName == "totalSoldierRevives" && _revivedSoldierTotal < nextLevelThreshold) ||
						(critName == "totalHostileRevives" && _revivedHostileTotal < nextLevelThreshold) ||
						(critName == "totalNeutralRevives" && _revivedNeutralTotal < nextLevelThreshold) ||
						(critName == "totalWholeMedikit" && _wholeMedikitTotal < nextLevelThreshold) ||
						(critName == "totalBraveryGain" && _braveryGainTotal < nextLevelThreshold) ||
						(critName == "bestOfRank" && _bestOfRank < nextLevelThreshold) ||
						(critName == "bestSoldier" && _bestSoldier < nextLevelThreshold) ||
						(critName == "isMIA" && _MIA < nextLevelThreshold) ||
						(critName == "totalMartyrKills" && _martyrKillsTotal < nextLevelThreshold) ||
						(critName == "totalPostMortemKills" && _postMortemKills < nextLevelThreshold) ||
						(critName == "globeTrotter" && (_globeTrotter ? 1 : 0) < nextLevelThreshold) ||
						(critName == "totalSlaveKills" && _slaveKillsTotal < nextLevelThreshold)
					)
				)
				{
					awardCommendationBool = false;
					break;
				}
				// Medals with the following criteria are unique because they need a noun.
				// And because they loop over a map<> (this allows for maximum moddability).
				else if (critName == "totalKillsWithAWeapon" || critName == "totalMissionsInARegion" || critName == "totalKillsByRace" || critName == "totalKillsByRank")
				{
					SortedDictionary<string, int> tempTotal = null;
					if (critName == "totalKillsWithAWeapon")
						tempTotal = WeaponTotal;
					else if (critName == "totalMissionsInARegion")
						tempTotal = GetRegionTotal(missionStatistics);
					else if (critName == "totalKillsByRace")
						tempTotal = AlienRaceTotal;
					else if (critName == "totalKillsByRank")
						tempTotal = AlienRankTotal;
					// Loop over the temporary map.
					// Match nouns and decoration levels.
					foreach (var pair in tempTotal)
					{
						int criteria = -1;
						string noun = pair.Key;
						// If there is no matching noun, get the first award criteria.
						if (!nextCommendationLevel.TryGetValue(noun, out int nounLevel))
							criteria = critDef[0];
						// Otherwise, get the criteria that reflects the soldier's commendation level.
						else if ((uint)nounLevel != (uint)critDef.Count)
							criteria = critDef[nounLevel];

						// If a criteria was set AND the stat's count exceeds the criteria.
						if (criteria != -1 && pair.Value >= criteria)
						{
							modularCommendations.Add(noun);
						}
					}
					// If it is still empty, we did not get a commendation.
					if (modularCommendations.Count == 0)
					{
						awardCommendationBool = false;
						break;
					}
				}
				// Medals that are based on _how_ a kill was achieved are found here.
				else if (critName == "killsWithCriteriaCareer" || critName == "killsWithCriteriaMission" || critName == "killsWithCriteriaTurn")
				{
					// Fetch the kill criteria list.
					if (commRule.KillCriteria == null)
						break;
					var killCriteriaList = commRule.KillCriteria;

					int totalKillGroups = 0; // holds the total number of kill groups which satisfy one of the OR criteria blocks
					bool enoughForNextCommendation = false;

					// Loop over the OR vectors.
					// if OR criteria are not disjunctive (e.g. "kill 1 enemy" or "kill 1 enemy"), each one will be counted and added to totals - avoid that if you want good statistics
					foreach (var orCriteria in killCriteriaList)
					{
						// prepare counters
						var referenceBlockCounters = new List<int>(new int[orCriteria.Count]);
						int referenceTotalCounters = 0;
						for (int index = 0; index < orCriteria.Count; ++index)
						{
							referenceBlockCounters[index] = orCriteria[index].first;
							referenceTotalCounters += orCriteria[index].first;
						}
						var currentBlockCounters = new List<int>();
						if (critName == "killsWithCriteriaCareer")
						{
							currentBlockCounters = new List<int>(referenceBlockCounters);
						}
						int currentTotalCounters = referenceTotalCounters;
						int lastTimeSpan = -1;
						bool skipThisTimeSpan = false;
						// Loop over the KILLS, seeking to fulfill all criteria from entire AND block within the specified time span (career/mission/turn)
						foreach (var singleKill in _killList)
						{
							int thisTimeSpan = -1;
							if (critName == "killsWithCriteriaMission")
							{
								thisTimeSpan = singleKill.mission;
							}
							else if (critName == "killsWithCriteriaTurn")
							{
								thisTimeSpan = singleKill.turn;
							}
							if (thisTimeSpan != lastTimeSpan)
							{
								// next time span, reset counters
								lastTimeSpan = thisTimeSpan;
								skipThisTimeSpan = false;
								currentBlockCounters = new List<int>(referenceBlockCounters);
								currentTotalCounters = referenceTotalCounters;
							}
							// same time span, we're skipping the rest of it if we already fulfilled criteria
							else if (skipThisTimeSpan)
							{
								continue;
							}

							bool andCriteriaMet = false;

							// Loop over the AND vectors.
							for (int index = 0; index < orCriteria.Count; ++index)
							{
								var andCriteria = orCriteria[index];
								bool foundMatch = true;

								// Loop over the DETAILs of one AND vector.
								foreach (var detail in andCriteria.second)
								{
									// Look if for match for this criteria.
									// If we find a match, continue to the next criteria. (We must match all criteria in the list.)
									// If we don't find a match, set foundMatch = false; then break.

									if (singleKill.rank == detail ||
										singleKill.race == detail ||
										singleKill.weapon == detail ||
										singleKill.weaponAmmo == detail ||
										singleKill.UnitStatusString == detail ||
										singleKill.UnitFactionString == detail ||
										singleKill.UnitSideString == detail ||
										singleKill.UnitBodyPartString == detail)
									{
										// Found match
										continue;
									}

									// check the weapon's battle type and damage type
									RuleItem weapon = mod.GetItem(singleKill.weapon);
									if (weapon != null)
									{
										int battleType = (int)weapon.BattleType;

										if (battleType >= 0 && battleType < BATTLE_TYPES && battleTypeArray[battleType] == detail)
										{
											// the detail matched the weapon's battle type
											continue;
										}

										RuleItem weaponAmmo = mod.GetItem(singleKill.weaponAmmo);
										int damageType = -1;

										if (weaponAmmo != null)
										{
											damageType = (int)weaponAmmo.DamageType.ResistType;
										}
										else if (singleKill.weaponAmmo == "__GUNBUTT")
										{
											// If weaponAmmo == "__GUNBUTT", that means the gun's secondary melee attack was used.
											damageType = (int)weapon.MeleeType.ResistType;
										}
										// If we were unable to determine the damage type, leave it as -1.

										if (damageType >= 0 && damageType < DAMAGE_TYPES && damageTypeArray[damageType] == detail)
										{
											// the detail matched the damage type
											continue;
										}
									}

									// That's all we can check. We didn't find a match
									foundMatch = false;
									break;
								} // End of DETAIL loop.

								if (foundMatch)
								{
									// some current block counters might go into negatives, this is used to tally career kills correctly
									// currentTotalCounters will always ensure we're counting in proper batches
									if (currentBlockCounters[index]-- > 0 && --currentTotalCounters <= 0)
									{
										// we just counted all counters in a block to zero, this certainly means that the entire block criteria is fulfilled
										andCriteriaMet = true;
										break;
									}
								}
							} // End of AND loop.

							if (andCriteriaMet)
							{
								// early exit if we got enough, no reason to continue iterations
								if (++totalKillGroups >= critDef[nextCommendationLevel["noNoun"]])
								{
									enoughForNextCommendation = true;
									break;
								}

								// "killsWithCriteriaTurn" and "killsWithCriteriaMission" are "peak achivements", they are counted once per their respective time span if criteria are fulfilled
								// so if we got them, we're skipping the rest of this time span to avoid counting more than once
								// e.g. 20 kills in a mission will not be counted as "10 kills in a mission" criteria twice
								// "killsWithCriteriaCareer" are totals, so they are never skipped this way
								if (critName == "killsWithCriteriaTurn" || critName == "killsWithCriteriaMission")
								{
									skipThisTimeSpan = true;
								}
								// for career kills we'll ADD reference counters to the current values and recalculate current total
								// this is used to count instances of full criteria blocks, e.g. if rules state that a career commendation must be awarded for 2 kills of alien leaders
								// and 1 kill of  alien commander, then we must ensure there's 2 leader kills + 1 commander kill for each instance of criteria fulfilled
								else if (critName == "killsWithCriteriaCareer")
								{
									currentTotalCounters = 0;
									for (int i2 = 0; i2 < currentBlockCounters.Count; i2++)
									{
										currentBlockCounters[i2] += referenceBlockCounters[i2];
										currentTotalCounters += Math.Max(currentBlockCounters[i2], 0);
									}
								}
							}
						} // End of KILLs loop.

						if (enoughForNextCommendation)
							break; // stop iterating here too, we've got enough

					} // End of OR loop.

					if (!enoughForNextCommendation)
						awardCommendationBool = false;

				}
			}
			if (awardCommendationBool)
			{
				// If we do not have modular medals, but are awarded a different medal,
				// its noun will be "noNoun".
				if (modularCommendations.Count == 0)
				{
					modularCommendations.Add("noNoun");
				}
				foreach (var noun in modularCommendations)
				{
					bool newCommendation = true;
					foreach (var soldierComm in _commendations)
					{
						if (soldierComm.Type == commType && soldierComm.Noun == noun)
						{
							soldierComm.AddDecoration();
							newCommendation = false;
							break;
						}
					}
					if (newCommendation)
					{
						_commendations.Add(new SoldierCommendations(commType, noun, mod));
					}
				}
				ret = true;
			}
			else
			{
				++iter;
			}
		}
		return ret;
	}

	/// <summary>Get vector of mission ids.</summary>
	public List<int> MissionIdList => _missionIdList;

	/// <summary>Get vector of kills.</summary>
	public List<BattleUnitKills> Kills => _killList;

	private static void Increment(SortedDictionary<string, int> list, string key)
	{
		list.TryGetValue(key, out int count);
		list[key] = count + 1;
	}

	/// <summary>Get list of kills sorted by rank.</summary>
	public SortedDictionary<string, int> AlienRankTotal
	{
		get
		{
			var list = new SortedDictionary<string, int>(StringComparer.Ordinal);
			foreach (var buk in _killList)
			{
				Increment(list, buk.rank);
			}
			return list;
		}
	}

	/// <summary>Get list of kills sorted by race.</summary>
	public SortedDictionary<string, int> AlienRaceTotal
	{
		get
		{
			var list = new SortedDictionary<string, int>(StringComparer.Ordinal);
			foreach (var buk in _killList)
			{
				Increment(list, buk.race);
			}
			return list;
		}
	}

	/// <summary>Get list of hostile kills sorted by weapon.</summary>
	public SortedDictionary<string, int> WeaponTotal
	{
		get
		{
			var list = new SortedDictionary<string, int>(StringComparer.Ordinal);
			foreach (var buk in _killList)
			{
				if (buk.faction == UnitFaction.FACTION_HOSTILE)
					Increment(list, buk.weapon);
			}
			return list;
		}
	}

	/// <summary>Get list of hostile kills sorted by weapon ammo.</summary>
	public SortedDictionary<string, int> WeaponAmmoTotal
	{
		get
		{
			var list = new SortedDictionary<string, int>(StringComparer.Ordinal);
			foreach (var buk in _killList)
			{
				if (buk.faction == UnitFaction.FACTION_HOSTILE)
					Increment(list, buk.weaponAmmo);
			}
			return list;
		}
	}

	/// <summary>Get a map of the amount of missions done in each region.</summary>
	public SortedDictionary<string, int> GetRegionTotal(List<MissionStatistics> missionStatistics)
	{
		var regionTotal = new SortedDictionary<string, int>(StringComparer.Ordinal);

		foreach (var ms in missionStatistics)
		{
			foreach (int missionId in _missionIdList)
			{
				if (missionId == ms.id)
				{
					Increment(regionTotal, ms.region);
				}
			}
		}

		return regionTotal;
	}

	/// <summary>Get a map of the amount of missions done in each country.</summary>
	public SortedDictionary<string, int> GetCountryTotal(List<MissionStatistics> missionStatistics)
	{
		var countryTotal = new SortedDictionary<string, int>(StringComparer.Ordinal);

		foreach (var ms in missionStatistics)
		{
			foreach (int missionId in _missionIdList)
			{
				if (missionId == ms.id)
				{
					Increment(countryTotal, ms.country);
				}
			}
		}

		return countryTotal;
	}

	/// <summary>Get a map of the amount of missions done in each type.</summary>
	public SortedDictionary<string, int> GetTypeTotal(List<MissionStatistics> missionStatistics)
	{
		var typeTotal = new SortedDictionary<string, int>(StringComparer.Ordinal);

		foreach (var ms in missionStatistics)
		{
			foreach (int missionId in _missionIdList)
			{
				if (missionId == ms.id)
				{
					Increment(typeTotal, ms.type);
				}
			}
		}

		return typeTotal;
	}

	/// <summary>Get a map of the amount of missions done in each UFO.</summary>
	public SortedDictionary<string, int> GetUFOTotal(List<MissionStatistics> missionStatistics)
	{
		var ufoTotal = new SortedDictionary<string, int>(StringComparer.Ordinal);

		foreach (var ms in missionStatistics)
		{
			foreach (int missionId in _missionIdList)
			{
				if (missionId == ms.id)
				{
					Increment(ufoTotal, ms.ufo);
				}
			}
		}

		return ufoTotal;
	}

	/// <summary>Get the total number of hostile kills.</summary>
	public int KillTotal
	{
		get
		{
			int killTotal = 0;

			foreach (var buk in _killList)
			{
				if (buk.status == UnitStatus.STATUS_DEAD && buk.faction == UnitFaction.FACTION_HOSTILE)
				{
					killTotal++;
				}
			}

			return killTotal;
		}
	}

	/// <summary>Get the total number of missions.</summary>
	public int MissionTotal => _missionIdList.Count;

	/// <summary>Get the total number of successful missions filtered by modder's criteria (or all missions).</summary>
	public int GetMissionTotalFiltered(List<MissionStatistics> missionStatistics, RuleCommendations rule)
	{
		if (rule.MissionTypeNames.Count != 0)
		{
			int total = 0;
			foreach (var ms in missionStatistics)
			{
				if (ms.success)
				{
					if (_missionIdList.Contains(ms.id))
					{
						if (rule.MissionTypeNames.Contains(ms.type))
						{
							++total;
						}
					}
				}
			}
			return total;
		}
		else if (rule.MissionMarkerNames.Count != 0)
		{
			int total = 0;
			foreach (var ms in missionStatistics)
			{
				if (ms.success)
				{
					if (_missionIdList.Contains(ms.id))
					{
						if (rule.MissionMarkerNames.Contains(ms.markerName))
						{
							++total;
						}
					}
				}
			}
			return total;
		}

		return _missionIdList.Count;
	}

	/// <summary>Get the total of wins.</summary>
	public int GetWinTotal(List<MissionStatistics> missionStatistics)
	{
		int winTotal = 0;

		foreach (var ms in missionStatistics)
		{
			foreach (int missionId in _missionIdList)
			{
				if (missionId == ms.id)
				{
					if (ms.success)
					{
						winTotal++;
					}
				}
			}
		}

		return winTotal;
	}

	/// <summary>Get the total number of hostile stuns.</summary>
	public int StunTotal
	{
		get
		{
			int stunTotal = 0;

			foreach (var buk in _killList)
			{
				if (buk.status == UnitStatus.STATUS_UNCONSCIOUS && buk.faction == UnitFaction.FACTION_HOSTILE)
				{
					stunTotal++;
				}
			}

			return stunTotal;
		}
	}

	/// <summary>Get the total number of hostile psi panics.</summary>
	public int PanickTotal
	{
		get
		{
			int panickTotal = 0;

			foreach (var buk in _killList)
			{
				if (buk.status == UnitStatus.STATUS_PANICKING && buk.faction == UnitFaction.FACTION_HOSTILE)
				{
					panickTotal++;
				}
			}

			return panickTotal;
		}
	}

	/// <summary>Get the total number of hostile psi mind controls.</summary>
	public int ControlTotal
	{
		get
		{
			int controlTotal = 0;

			foreach (var buk in _killList)
			{
				if (buk.status == UnitStatus.STATUS_TURNING && buk.faction == UnitFaction.FACTION_HOSTILE)
				{
					controlTotal++;
				}
			}

			return controlTotal;
		}
	}

	/// <summary>Get the total number of days wounded.</summary>
	public int DaysWoundedTotal => _daysWoundedTotal;

	/// <summary>Increment soldier's service time one month.</summary>
	public void AddMonthlyService()
	{
		_monthsService++;
	}

	/// <summary>Returns the total months this soldier has been in service.</summary>
	public int MonthsService => _monthsService;

	/// <summary>Award special commendation to the original 8 soldiers.</summary>
	public void AwardOriginalEightCommendation(Mod mod)
	{
		// TODO: Unhardcode this
		_commendations.Add(new SoldierCommendations("STR_MEDAL_ORIGINAL8_NAME", "NoNoun", mod));
	}

	/// <summary>Award posthumous best-of commendation.</summary>
	public void AwardBestOfRank(int score)
	{
		_bestOfRank = score;
	}

	/// <summary>Award posthumous best-of commendation.</summary>
	public void AwardBestOverall(int score)
	{
		_bestSoldier = score;
	}

	/// <summary>Award posthumous kills commendation.</summary>
	public void AwardPostMortemKill(int kills)
	{
		_postMortemKills = kills;
	}

	/// <summary>Get the total number of shots fired.</summary>
	public int ShotsFiredTotal => _shotsFiredCounterTotal;

	/// <summary>Get the total number of shots landed on target.</summary>
	public int ShotsLandedTotal => _shotsLandedCounterTotal;

	/// <summary>Get the soldier's accuracy.</summary>
	public int Accuracy
	{
		get
		{
			if (_shotsFiredCounterTotal != 0)
				return 100 * _shotsLandedCounterTotal / _shotsFiredCounterTotal;
			return 0;
		}
	}

	/// <summary>Get trap kills total.</summary>
	public int GetTrapKillTotal(Mod mod)
	{
		int trapKillTotal = 0;

		foreach (var buk in _killList)
		{
			RuleItem item = mod.GetItem(buk.weapon);
			if (buk.HostileTurn() && (item == null || item.BattleType == BattleType.BT_GRENADE || item.BattleType == BattleType.BT_PROXIMITYGRENADE))
			{
				trapKillTotal++;
			}
		}

		return trapKillTotal;
	}

	/// <summary>Get reaction kill total.</summary>
	public int GetReactionFireKillTotal(Mod mod)
	{
		int reactionFireKillTotal = 0;

		foreach (var buk in _killList)
		{
			RuleItem item = mod.GetItem(buk.weapon);
			if (buk.HostileTurn() && item != null && item.BattleType != BattleType.BT_GRENADE && item.BattleType != BattleType.BT_PROXIMITYGRENADE)
			{
				reactionFireKillTotal++;
			}
		}

		return reactionFireKillTotal;
	}

	/// <summary>Get the total of terror missions (not a UFO, not the base, not the alien base or colony).</summary>
	public int GetTerrorMissionTotal(List<MissionStatistics> missionStatistics)
	{
		int terrorMissionTotal = 0;

		foreach (var ms in missionStatistics)
		{
			foreach (int misionId in _missionIdList)
			{
				if (misionId == ms.id)
				{
					if (ms.success && !ms.IsBaseDefense && !ms.IsUfoMission && !ms.IsAlienBase)
					{
						terrorMissionTotal++;
					}
				}
			}
		}

		return terrorMissionTotal;
	}

	/// <summary>Get the total of night missions.</summary>
	public int GetNightMissionTotal(List<MissionStatistics> missionStatistics, Mod mod)
	{
		int nightMissionTotal = 0;

		foreach (var ms in missionStatistics)
		{
			foreach (int misionId in _missionIdList)
			{
				if (misionId == ms.id)
				{
					if (ms.success && ms.IsDarkness(mod) && !ms.IsBaseDefense && !ms.IsAlienBase)
					{
						nightMissionTotal++;
					}
				}
			}
		}

		return nightMissionTotal;
	}

	/// <summary>Get the total of night terror missions.</summary>
	public int GetNightTerrorMissionTotal(List<MissionStatistics> missionStatistics, Mod mod)
	{
		int nightTerrorMissionTotal = 0;

		foreach (var ms in missionStatistics)
		{
			foreach (int misionId in _missionIdList)
			{
				if (misionId == ms.id)
				{
					if (ms.success && ms.IsDarkness(mod) && !ms.IsBaseDefense && !ms.IsUfoMission && !ms.IsAlienBase)
					{
						nightTerrorMissionTotal++;
					}
				}
			}
		}

		return nightTerrorMissionTotal;
	}

	/// <summary>Get the total of base defense missions.</summary>
	public int GetBaseDefenseMissionTotal(List<MissionStatistics> missionStatistics)
	{
		int baseDefenseMissionTotal = 0;

		foreach (var ms in missionStatistics)
		{
			foreach (int misionId in _missionIdList)
			{
				if (misionId == ms.id)
				{
					if (ms.success && ms.IsBaseDefense)
					{
						baseDefenseMissionTotal++;
					}
				}
			}
		}

		return baseDefenseMissionTotal;
	}

	/// <summary>Get the total of alien base assaults.</summary>
	public int GetAlienBaseAssaultTotal(List<MissionStatistics> missionStatistics)
	{
		int alienBaseAssaultTotal = 0;

		foreach (var ms in missionStatistics)
		{
			foreach (int misionId in _missionIdList)
			{
				if (misionId == ms.id)
				{
					if (ms.success && ms.IsAlienBase)
					{
						alienBaseAssaultTotal++;
					}
				}
			}
		}

		return alienBaseAssaultTotal;
	}

	/// <summary>Get the total of important missions.</summary>
	public int GetImportantMissionTotal(List<MissionStatistics> missionStatistics)
	{
		int importantMissionTotal = 0;

		foreach (var ms in missionStatistics)
		{
			foreach (int misionId in _missionIdList)
			{
				if (misionId == ms.id)
				{
					if (ms.success && ms.type != "STR_UFO_CRASH_RECOVERY")
					{
						importantMissionTotal++;
					}
				}
			}
		}

		return importantMissionTotal;
	}

	/// <summary>Get the total score.</summary>
	public int GetScoreTotal(List<MissionStatistics> missionStatistics)
	{
		int scoreTotal = 0;

		foreach (var ms in missionStatistics)
		{
			foreach (int misionId in _missionIdList)
			{
				if (misionId == ms.id)
				{
					scoreTotal += ms.score;
				}
			}
		}

		return scoreTotal;
	}

	/// <summary>Get the Valiant Crux total.</summary>
	public int GetValiantCruxTotal(List<MissionStatistics> missionStatistics)
	{
		int valiantCruxTotal = 0;

		foreach (var ms in missionStatistics)
		{
			foreach (int misionId in _missionIdList)
			{
				if (misionId == ms.id && ms.valiantCrux)
				{
					valiantCruxTotal++;
				}
			}
		}

		return valiantCruxTotal;
	}

	/// <summary>Get the loot value total.</summary>
	public int GetLootValueTotal(List<MissionStatistics> missionStatistics)
	{
		int lootValueTotal = 0;

		foreach (var ms in missionStatistics)
		{
			foreach (int misionId in _missionIdList)
			{
				if (misionId == ms.id)
				{
					lootValueTotal += ms.lootValue;
				}
			}
		}

		return lootValueTotal;
	}
}
