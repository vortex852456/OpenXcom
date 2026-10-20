#pragma warning disable CA1716 // namespace mirrors the C++ folder Mod
namespace Acom.Tests.Mod;

public class RuleStatBonusTests
{
	private static YamlNodeReader Yaml(string text) => new YamlRootNodeReader(text, "test.rul").ToBase();

	/// <summary>Evaluates the bonus for a stand-in unit whose stats are given by name.</summary>
	private static int Eval(RuleStatBonus bonus, Dictionary<string, float> stats, int external = 0) =>
		bonus.Execute(new object(), external, (_, index) => stats.First(s => RuleStatBonus.StatIndex(s.Key) == index).Value);

	[Fact]
	public void Map_terms_are_sorted_by_the_stat_table_and_unknown_terms_dropped()
	{
		var bonus = new RuleStatBonus();
		bonus.SetFiring();
		bonus.Load("STR_GUN", Yaml("accuracyMultiplier:\n  reactions: 0.5\n  bogus: 3\n  firing: [0.0, 0.01]\n"), "accuracyMultiplier");
		Assert.True(bonus.IsModded);
		var raw = bonus.BonusRaw;
		Assert.Equal(2, raw.Count);
		Assert.Equal("firing", raw[0].first);
		Assert.Equal([0.0f, 0.01f], raw[0].second);
		Assert.Equal("reactions", raw[1].first);
		Assert.Equal([0.5f], raw[1].second);

		// 0.01 * 50^2 + 0.5 * 50, rounded
		var stats = new Dictionary<string, float> { ["firing"] = 50, ["reactions"] = 50 };
		Assert.Equal(50, Eval(bonus, stats));
		Assert.Equal(57, Eval(bonus, stats, 7));
		stats["reactions"] = 51; // 25 + 25.5 rounds up
		Assert.Equal(51, Eval(bonus, stats));
	}

	[Fact]
	public void Polynomial_uses_at_most_four_coefficients()
	{
		var bonus = new RuleStatBonus();
		bonus.Load("X", Yaml("p:\n  tu: [1, 2, 3, 4, 5]\n"), "p");
		Assert.Equal([1f, 2f, 3f, 4f], bonus.BonusRaw[0].second);
		// 1*2 + 2*4 + 3*8 + 4*16 = 98
		Assert.Equal(98, Eval(bonus, new() { ["tu"] = 2 }));
	}

	[Fact]
	public void Defaults_stay_until_a_mod_changes_them()
	{
		var bonus = new RuleStatBonus();
		bonus.SetPsiDefense();
		bonus.Load("X", Yaml("other: 1\n"), "psiDefence");
		Assert.False(bonus.IsModded);
		Assert.Equal(["psiStrength", "psiSkill"], bonus.BonusRaw.Select(b => b.first));
		Assert.Equal(60 + 10, Eval(bonus, new() { ["psiStrength"] = 60, ["psiSkill"] = 50 }));

		// an invalid reader (missing parent node) only compiles the defaults
		var recovery = new RuleStatBonus();
		recovery.SetTimeRecovery();
		recovery.Load("X", default, "time");
		Assert.Equal(40, Eval(recovery, new() { ["tu"] = 40 }));
	}

	[Fact]
	public void Rounding_is_to_the_nearest_away_from_zero()
	{
		var bonus = new RuleStatBonus();
		bonus.Load("X", Yaml("b:\n  flatOne: -0.6\n"), "b");
		Assert.Equal(-1, Eval(bonus, new() { ["flatOne"] = 1 }));
		bonus.Load("X", Yaml("b:\n  flatOne: -0.4\n"), "b");
		Assert.Equal(0, Eval(bonus, new() { ["flatOne"] = 1 }));
		bonus.Load("X", Yaml("b:\n  flatHundred: 0.025\n"), "b");
		Assert.Equal(3, Eval(bonus, new() { ["flatHundred"] = 100 }));
	}

	[Fact]
	public void Empty_map_and_custom_script_return_the_external_bonus()
	{
		var bonus = new RuleStatBonus();
		bonus.SetFiring();
		bonus.Load("X", Yaml("b: {}\n"), "b");
		Assert.True(bonus.IsModded);
		Assert.Empty(bonus.BonusRaw);
		Assert.Equal(4, bonus.GetBonus((BattleUnit)null!, 4));

		var script = new RuleStatBonus();
		script.Load("X", Yaml("b: \"return 5;\"\n"), "b");
		Assert.True(script.IsModded);
		Assert.Equal(3, script.GetBonus((BattleUnit)null!, 3));
		// a later map replaces the script again
		script.Load("X", Yaml("b:\n  flatOne: 2\n"), "b");
		Assert.Equal(2, Eval(script, new() { ["flatOne"] = 1 }));

		// without a unit the stat terms are skipped
		var firing = new RuleStatBonus();
		firing.SetFiring();
		firing.Load("X", default, "accuracyMultiplier");
		Assert.Equal(-2, firing.GetBonus((BattleUnit)null!, -2));
	}

	[Fact]
	public void Soldier_bonus_reads_recovery_stats_and_armor()
	{
		var bonus = new RuleSoldierBonus("STR_BONUS", 3);
		bonus.Load(Yaml("visibilityAtDark: 5\nheatVision: 30\nsideArmor: 4\nleftArmorDiff: -1\nstats:\n  firing: 10\nrecovery:\n  time:\n    flatOne: 3\n  stun:\n    flatHundred: [0.01]\nlistOrder: 9\n"), null!);
		Assert.Equal(5, bonus.VisibilityAtDark);
		Assert.Equal(30, bonus.VisibilityThroughSmoke);
		Assert.Equal(4, bonus.RightSideArmor);
		Assert.Equal(3, bonus.LeftSideArmor);
		Assert.Equal(10, bonus.Stats.firing);
		Assert.Equal(9, bonus.ListOrder);
		Assert.True(bonus.TimeRecoveryRaw.IsModded);
		Assert.Equal(3, Eval(bonus.TimeRecoveryRaw, new() { ["flatOne"] = 1 }));
		Assert.Equal(1, Eval(bonus.StunRegenerationRaw, new() { ["flatHundred"] = 100 }));
		Assert.False(bonus.EnergyRecoveryRaw.IsModded);
		Assert.Empty(bonus.EnergyRecoveryRaw.BonusRaw);
		Assert.Equal(0, bonus.GetEnergyRecovery(null!));
	}
}
