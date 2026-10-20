using System.Runtime.CompilerServices;
using static Acom.Tests.Savegame.BattleUnitStatisticsTests;

namespace Acom.Tests.Savegame;

public class SoldierDiaryTests
{
	private const string DiaryYaml = """
		killList:
		  - {type: STR_SECTOID_SOLDIER, rank: STR_LIVE_SOLDIER, race: STR_SECTOID, weapon: STR_RIFLE, weaponAmmo: STR_RIFLE_CLIP, status: 6, faction: 1, mission: 1, turn: 4, side: 0, bodypart: 0, id: 1000001}
		  - {type: STR_SECTOID_LEADER, rank: STR_LIVE_LEADER, race: STR_SECTOID, weapon: STR_RIFLE, weaponAmmo: STR_RIFLE_CLIP, status: 7, faction: 1, mission: 1, turn: 5, side: 0, bodypart: 0, id: 1000002}
		  - {type: STR_FLOATER_SOLDIER, rank: STR_LIVE_SOLDIER, race: STR_FLOATER, weapon: STR_PISTOL, weaponAmmo: STR_PISTOL_CLIP, status: 6, faction: 1, mission: 2, turn: 3, side: 0, bodypart: 0, id: 1000003}
		  - {name: Jane Doe, rank: STR_ROOKIE, race: STR_FRIENDLY, weapon: STR_PISTOL, weaponAmmo: STR_PISTOL_CLIP, status: 6, faction: 0, mission: 2, turn: 3, side: 0, bodypart: 0, id: 7}
		missionIdList: [1, 2]
		daysWoundedTotal: 1
		totalShotByFriendlyCounter: 2
		totalShotFriendlyCounter: 3
		loneSurvivorTotal: 4
		monthsService: 5
		unconciousTotal: 6
		shotAtCounterTotal: 7
		hitCounterTotal: 8
		ironManTotal: 9
		longDistanceHitCounterTotal: 10
		lowAccuracyHitCounterTotal: 11
		shotsFiredCounterTotal: 12
		shotsLandedCounterTotal: 9
		shotAtCounter10in1Mission: 14
		hitCounter5in1Mission: 15
		timesWoundedTotal: 16
		killedInAction: 1
		allAliensKilledTotal: 18
		allAliensStunnedTotal: 19
		woundsHealedTotal: 20
		allUFOs: 1
		allMissionTypes: 1
		statGainTotal: 23
		revivedUnitTotal: 24
		revivedSoldierTotal: 25
		revivedHostileTotal: 26
		revivedNeutralTotal: 27
		wholeMedikitTotal: 28
		braveryGainTotal: 29
		bestOfRank: 30
		missingInAction: 1
		bestSoldier: 1
		martyrKillsTotal: 33
		postMortemKills: 34
		globeTrotter: true
		slaveKillsTotal: 35
		""";

	[Fact]
	public void Diary_yaml_roundtrip_keeps_cpp_keys_and_order()
	{
		var diary = new SoldierDiary();
		diary.Load(Parse(DiaryYaml), null); // no commendations, so no mod lookups
		Assert.Equal(4, diary.Kills.Count);
		Assert.Equal([1, 2], diary.MissionIdList);
		Assert.Equal(5, diary.MonthsService);
		Assert.Equal(1, diary.DaysWoundedTotal);

		var writer = new YamlRootNodeWriter();
		diary.Save(writer);
		var back = writer.ToReader();
		Assert.Equal(Keys(Parse(DiaryYaml)), Keys(back));
		Assert.Contains("missionIdList: [1, 2]", writer.Emit(), StringComparison.Ordinal);

		var again = new SoldierDiary();
		again.Load(back, null);
		var writer2 = new YamlRootNodeWriter();
		again.Save(writer2);
		Assert.Equal(writer.Emit(), writer2.Emit());
	}

	[Fact]
	public void Empty_diary_saves_an_empty_map()
	{
		var writer = new YamlRootNodeWriter();
		new SoldierDiary().Save(writer);
		Assert.Empty(Keys(writer.ToReader()));
	}

	[Fact]
	public void Kill_totals()
	{
		var diary = new SoldierDiary();
		diary.Load(Parse(DiaryYaml), null);
		Assert.Equal(2, diary.KillTotal); // the friendly kill does not count
		Assert.Equal(1, diary.StunTotal);
		Assert.Equal(0, diary.PanickTotal);
		Assert.Equal(0, diary.ControlTotal);
		Assert.Equal(2, diary.MissionTotal);
		Assert.Equal(75, diary.Accuracy);
		Assert.Equal(12, diary.ShotsFiredTotal);

		// std::map order (ordinal)
		Assert.Equal(["STR_LIVE_LEADER", "STR_LIVE_SOLDIER", "STR_ROOKIE"], diary.AlienRankTotal.Keys);
		Assert.Equal(2, diary.AlienRankTotal["STR_LIVE_SOLDIER"]);
		Assert.Equal(["STR_FLOATER", "STR_FRIENDLY", "STR_SECTOID"], diary.AlienRaceTotal.Keys);
		// weapons only count hostile victims
		Assert.Equal(2, diary.WeaponTotal["STR_RIFLE"]);
		Assert.Equal(1, diary.WeaponTotal["STR_PISTOL"]);
		Assert.Equal(1, diary.WeaponAmmoTotal["STR_PISTOL_CLIP"]);
	}

	private static MissionStatistics Mission(int id, bool success, string region, string type, int score)
	{
		// MissionStatistics is not ported yet; the diary only reads its fields
		var ms = (MissionStatistics)RuntimeHelpers.GetUninitializedObject(typeof(MissionStatistics));
		ms.id = id;
		ms.success = success;
		ms.region = region;
		ms.country = "STR_NONE";
		ms.type = type;
		ms.ufo = "NO_UFO";
		ms.score = score;
		ms.valiantCrux = id == 3;
		ms.lootValue = 10 * id;
		return ms;
	}

	[Fact]
	public void Mission_totals_only_count_own_missions()
	{
		var diary = new SoldierDiary();
		diary.Load(Parse("missionIdList: [1, 3, 4]"), null);
		List<MissionStatistics> missions =
		[
			Mission(1, true, "STR_EUROPE", "STR_UFO_CRASH_RECOVERY", 100),
			Mission(2, true, "STR_EUROPE", "STR_TERROR_MISSION", 500),
			Mission(3, false, "STR_ASIA", "STR_TERROR_MISSION", -200),
			Mission(4, true, "STR_ARCTIC", "STR_UFO_GROUND_ASSAULT", 300),
		];
		Assert.Equal(2, diary.GetWinTotal(missions));
		Assert.Equal(200, diary.GetScoreTotal(missions));
		Assert.Equal(1, diary.GetImportantMissionTotal(missions));
		Assert.Equal(1, diary.GetValiantCruxTotal(missions));
		Assert.Equal(80, diary.GetLootValueTotal(missions));
		Assert.Equal(["STR_ARCTIC", "STR_ASIA", "STR_EUROPE"], diary.GetRegionTotal(missions).Keys);
		Assert.Equal(3, diary.GetCountryTotal(missions)["STR_NONE"]);
		Assert.Equal(3, diary.GetTypeTotal(missions).Count);
		Assert.Single(diary.GetUFOTotal(missions));
	}

	[Fact]
	public void Commendation_yaml_roundtrip()
	{
		// SoldierCommendations looks up its rule in the Mod (not ported yet); Load/Save do not need it
		var sc = (SoldierCommendations)RuntimeHelpers.GetUninitializedObject(typeof(SoldierCommendations));
		sc.Load(Parse("{commendationName: STR_MEDAL_BRONZE_STAR_NAME, decorationLevel: 2}"));
		Assert.Equal("STR_MEDAL_BRONZE_STAR_NAME", sc.Type);
		Assert.Equal("noNoun", sc.Noun);
		Assert.Equal(2, sc.DecorationLevelInt);
		Assert.False(sc.IsNew);
		Assert.Equal("STR_AWARD_1", sc.GetDecorationLevelName(1));
		Assert.Equal("STR_AWARD_DECOR_2", sc.DecorationDescription);

		var writer = new YamlRootNodeWriter();
		sc.Save(writer);
		Assert.Equal(["commendationName", "decorationLevel"], Keys(writer.ToReader()));

		sc.Load(Parse("{commendationName: STR_MEDAL_SNIPER_NAME, noun: STR_RIFLE, decorationLevel: 0, isNew: true}"));
		Assert.True(sc.IsNew);
		sc.AddDecoration();
		Assert.Equal(1, sc.DecorationLevelInt);
		sc.MakeOld();
		Assert.False(sc.IsNew);
		writer = new YamlRootNodeWriter();
		sc.Save(writer);
		var back = writer.ToReader();
		Assert.Equal(["commendationName", "noun", "decorationLevel"], Keys(back)); // isNew is not saved
		Assert.Equal("STR_RIFLE", back["noun"].Val);
		Assert.Equal("1", back["decorationLevel"].Val);
	}

	[Fact]
	public void Awards_and_service()
	{
		var diary = new SoldierDiary();
		diary.AddMonthlyService();
		diary.AddMonthlyService();
		diary.AwardBestOfRank(7);
		diary.AwardBestOverall(1);
		diary.AwardPostMortemKill(3);
		var writer = new YamlRootNodeWriter();
		diary.Save(writer);
		var back = writer.ToReader();
		Assert.Equal(["monthsService", "bestOfRank", "bestSoldier", "postMortemKills"], Keys(back));
		Assert.Equal(2, back["monthsService"].ReadVal<int>());
	}
}
