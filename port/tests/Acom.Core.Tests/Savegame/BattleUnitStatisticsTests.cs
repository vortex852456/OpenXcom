namespace Acom.Tests.Savegame;

public class BattleUnitStatisticsTests
{
	internal static YamlNodeReader Parse(string yaml) => new YamlRootNodeReader(yaml, "test").ToBase();

	internal static string[] Keys(YamlNodeReader reader) => reader.Children().Select(c => c.Key).ToArray();

	private const string KillYaml = "{type: STR_SECTOID_SOLDIER, rank: STR_LIVE_SOLDIER, race: STR_SECTOID, weapon: STR_RIFLE, weaponAmmo: STR_RIFLE_CLIP, status: 6, faction: 1, mission: 2, turn: 7, side: 3, bodypart: 1, id: 1000042}";

	[Fact]
	public void Kill_yaml_roundtrip_keeps_cpp_keys_and_order()
	{
		var kill = new BattleUnitKills(Parse(KillYaml));
		Assert.Equal("STR_SECTOID_SOLDIER", kill.type);
		Assert.Equal("", kill.name);
		Assert.Equal(UnitStatus.STATUS_DEAD, kill.status);
		Assert.Equal(UnitFaction.FACTION_HOSTILE, kill.faction);
		Assert.Equal(UnitSide.SIDE_REAR, kill.side);
		Assert.Equal(UnitBodyPart.BODYPART_TORSO, kill.bodypart);
		Assert.Equal(1000042, kill.id);

		var writer = new YamlRootNodeWriter();
		kill.Save(writer);
		var back = writer.ToReader();
		Assert.Equal(["type", "rank", "race", "weapon", "weaponAmmo", "status", "faction", "mission", "turn", "side", "bodypart", "id"], Keys(back));
		Assert.Equal("6", back["status"].Val);
		Assert.StartsWith("{", writer.Emit().Trim(), StringComparison.Ordinal); // flow style

		var again = new BattleUnitKills(back);
		Assert.Equal(kill.weaponAmmo, again.weaponAmmo);
		Assert.Equal(kill.turn, again.turn);
	}

	[Fact]
	public void Kill_reads_name_only_without_type_and_saves_name_first()
	{
		var withBoth = new BattleUnitKills(Parse("{name: Joe, type: STR_X}"));
		Assert.Equal("", withBoth.name);
		Assert.Equal("STR_X", withBoth.type);

		var soldier = new BattleUnitKills(Parse("{name: Joe, rank: STR_ROOKIE}"));
		Assert.Equal("Joe", soldier.name);
		// defaults of the C++ default constructor
		Assert.Equal(UnitFaction.FACTION_HOSTILE, soldier.faction);
		Assert.Equal(UnitStatus.STATUS_IGNORE_ME, soldier.status);

		var writer = new YamlRootNodeWriter();
		soldier.Save(writer);
		Assert.Equal("name", Keys(writer.ToReader())[0]);
		Assert.DoesNotContain("type", Keys(writer.ToReader()));
	}

	[Fact]
	public void Kill_turn_helpers_and_strings()
	{
		var kill = new BattleUnitKills { mission = 2 };
		kill.SetTurn(4, UnitFaction.FACTION_HOSTILE);
		Assert.Equal(13, kill.turn);
		Assert.True(kill.HostileTurn());
		Assert.Equal(613, kill.MakeTurnUnique());
		Assert.True(kill.HostileTurn());
		kill.SetTurn(4, UnitFaction.FACTION_NEUTRAL);
		Assert.False(kill.HostileTurn());

		kill.status = UnitStatus.STATUS_UNCONSCIOUS;
		Assert.Equal("STR_STUNNED", kill.KillStatusString);
		Assert.Equal("STATUS_UNCONSCIOUS", kill.UnitStatusString);
		kill.status = UnitStatus.STATUS_STANDING;
		Assert.Equal("status error", kill.KillStatusString);
		Assert.Equal("FACTION_HOSTILE", kill.UnitFactionString);
		Assert.Equal("SIDE_FRONT", kill.UnitSideString);
		kill.bodypart = UnitBodyPart.BODYPART_LEFTLEG;
		Assert.Equal("BODYPART_LEFTLEG", kill.UnitBodyPartString);
	}

	[Fact]
	public void Statistics_yaml_roundtrip_keeps_cpp_keys_and_order()
	{
		const string yaml = """
			wasUnconcious: true
			kills:
			  - {type: STR_A, rank: R, race: X, weapon: W, weaponAmmo: A, status: 6, faction: 1, mission: 1, turn: 4, side: 0, bodypart: 0, id: 5}
			  - {name: Bob, rank: R, race: X, weapon: W, weaponAmmo: A, status: 7, faction: 0, mission: 1, turn: 6, side: 0, bodypart: 0, id: 6}
			shotAtCounter: 1
			hitCounter: 2
			shotByFriendlyCounter: 3
			shotFriendlyCounter: 4
			loneSurvivor: true
			ironMan: true
			longDistanceHitCounter: 5
			lowAccuracyHitCounter: 6
			shotsFiredCounter: 7
			shotsLandedCounter: 8
			nikeCross: true
			mercyCross: true
			woundsHealed: 9
			appliedStimulant: 10
			appliedPainKill: 11
			revivedSoldier: 12
			revivedHostile: 13
			revivedNeutral: 14
			martyr: 15
			slaveKills: 16
			""";
		var stats = new BattleUnitStatistics(Parse(yaml));
		Assert.Equal(2, stats.kills.Count);
		Assert.Equal(16, stats.slaveKills);
		Assert.True(stats.DuplicateEntry(UnitStatus.STATUS_UNCONSCIOUS, 6));
		Assert.False(stats.DuplicateEntry(UnitStatus.STATUS_DEAD, 6));
		Assert.True(stats.HasFriendlyFired);

		var writer = new YamlRootNodeWriter();
		stats.Save(writer);
		var back = writer.ToReader();
		Assert.Equal(Keys(Parse(yaml)), Keys(back));
		Assert.Equal("Bob", back["kills"][1]["name"].Val);

		var again = new BattleUnitStatistics(back);
		Assert.Equal(stats.revivedNeutral, again.revivedNeutral);
		Assert.Equal(stats.kills[0].id, again.kills[0].id);
	}

	[Fact]
	public void Empty_statistics_are_saved_as_null()
	{
		var writer = new YamlRootNodeWriter();
		var stats = new BattleUnitStatistics { daysWounded = 3, KIA = true }; // not saved
		stats.Save(writer["unitStats"]);
		var back = writer.ToReader();
		Assert.True(back["unitStats"].HasNullVal);
		Assert.Empty(new BattleUnitStatistics(back["unitStats"]).kills);
	}
}
