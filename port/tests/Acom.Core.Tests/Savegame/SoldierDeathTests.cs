using static Acom.Tests.Savegame.BattleUnitStatisticsTests;

namespace Acom.Tests.Savegame;

public class SoldierDeathTests
{
	// GameTime is ported separately; until then the test is skipped.
	[Fact(SkipExceptions = [typeof(NotImplementedException)])]
	public void Death_yaml_roundtrip()
	{
		const string yaml = """
			time: {second: 5, minute: 30, hour: 12, weekday: 3, day: 14, month: 2, year: 1999}
			cause: {type: STR_SECTOID_SOLDIER, rank: STR_LIVE_SOLDIER, race: STR_SECTOID, weapon: STR_PLASMA_PISTOL, weaponAmmo: STR_PLASMA_PISTOL_CLIP, status: 6, faction: 0, mission: 3, turn: 4, side: 1, bodypart: 0, id: 12}
			""";
		var death = new SoldierDeath();
		death.Load(Parse(yaml));
		Assert.Equal(1999, death.Time.Year);
		Assert.Equal("STR_PLASMA_PISTOL", death.Cause.weapon);

		var writer = new YamlRootNodeWriter();
		death.Save(writer);
		var back = writer.ToReader();
		Assert.Equal(["time", "cause"], Keys(back));
		Assert.Equal(["second", "minute", "hour", "weekday", "day", "month", "year"], Keys(back["time"]));
		Assert.Equal("12", back["cause"]["id"].Val);

		// missing in action: no cause
		var mia = new SoldierDeath(death.Time, null);
		writer = new YamlRootNodeWriter();
		mia.Save(writer);
		Assert.Equal(["time"], Keys(writer.ToReader()));
		Assert.NotSame(death.Time, mia.Time); // C++ holds the time by value
		Assert.Null(mia.Cause);
	}
}
