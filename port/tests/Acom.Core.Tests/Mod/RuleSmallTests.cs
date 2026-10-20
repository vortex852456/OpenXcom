#pragma warning disable CA1716 // namespace follows the source area (src/Mod), like Acom.Tests.<Area>
namespace Acom.Tests.Mod;
#pragma warning restore CA1716

/// <summary>Deterministic parts of the smaller rulesets that load without Mod helpers or WeightedOptions.</summary>
public class RuleSmallTests
{
	[Fact]
	public void Mission_script_loads_and_requires_var_name_for_max_runs()
	{
		var script = new RuleMissionScript("SCRIPT");
		script.Load(new YamlRootNodeReader("""
			varName: TERROR
			firstMonth: 2
			lastMonth: 8
			label: 3
			executionOdds: 40
			maxRuns: 2
			startDelay: 30
			minFunds: -5000000000
			conditionals: [-1, 2]
			researchTriggers: {STR_B: true, STR_A: false}
			useTable: false
			""", "test").ToBase());
		Assert.Equal("TERROR", script.VarName);
		Assert.Equal(2, script.FirstMonth);
		Assert.Equal(8, script.LastMonth);
		Assert.Equal(3, script.Label);
		Assert.Equal(40, script.ExecutionOdds);
		Assert.Equal(2, script.MaxRuns);
		Assert.Equal(30, script.Delay);
		Assert.Equal(-5000000000L, script.MinFunds);
		Assert.Equal(long.MaxValue, script.MaxFunds);
		Assert.Equal(int.MinValue, script.MinScore);
		Assert.Equal(4, script.MaxDifficulty);
		Assert.Equal([-1, 2], script.Conditionals);
		Assert.Equal(["STR_A", "STR_B"], script.ResearchTriggers.Keys);
		Assert.False(script.ResearchTriggers["STR_A"]);
		Assert.False(script.UseTable);
		Assert.False(script.HasMissionWeights);
		Assert.Empty(script.AllMissionTypes);

		Assert.Throws<InvalidOperationException>(() => new RuleMissionScript("BAD").Load(new YamlRootNodeReader("avoidRepeats: 1", "test").ToBase()));
	}

	[Fact]
	public void Environmental_conditions_keep_struct_defaults()
	{
		var reader = new YamlRootNodeReader("""
			environmentalConditions:
			  STR_FRIENDLY: {chancePerTurn: 25, weaponOrAmmo: STR_GAS, side: 2}
			""", "test").ToBase();
		var conditions = reader["environmentalConditions"].ReadVal<SortedDictionary<string, EnvironmentalCondition>>([]);
		var friendly = conditions["STR_FRIENDLY"];
		Assert.Equal(25, friendly.chancePerTurn);
		Assert.Equal("STR_GAS", friendly.weaponOrAmmo);
		Assert.Equal(2, friendly.side);
		Assert.Equal(100, friendly.globalChance);
		Assert.Equal(1, friendly.firstTurn);
		Assert.Equal(1000, friendly.lastTurn);
		Assert.Equal(29, friendly.color);
		Assert.Equal(-1, friendly.bodyPart);

		var effects = new RuleEnviroEffects("FX");
		Assert.Equal(15, effects.MapBackgroundColor);
		var none = effects.GetEnvironmetalCondition("STR_HOSTILE");
		Assert.Equal(0, none.chancePerTurn);
		Assert.Equal(100, none.globalChance);
		Assert.Null(effects.GetArmorTransformation(null));
	}

	[Fact]
	public void Armor_move_cost_and_color_fallback()
	{
		var cost = new ArmorMoveCost { TimePercent = 100, EnergyPercent = 50 };
		cost.Load(new YamlRootNodeReader("[75, 25]", "test").ToBase());
		Assert.Equal(new ArmorMoveCost { TimePercent = 75, EnergyPercent = 25 }, cost);
		cost.Load(default);
		Assert.Equal(75, cost.TimePercent);
		var product = cost * new ArmorMoveCost { TimePercent = 2, EnergyPercent = 3 };
		Assert.True(product == new ArmorMoveCost { TimePercent = 150, EnergyPercent = 75 });
		Assert.True(product != cost);

		var writer = new YamlRootNodeWriter();
		writer.SetAsMap();
		product.Save(writer, "walkPercent");
		var back = new ArmorMoveCost();
		back.Load(new YamlRootNodeReader(writer.Emit(), "test")["walkPercent"]);
		Assert.Equal(product, back);

		// pos 31 tests 31, 15, 7; pos 36 tests 36, 4
		var colors = Enumerable.Range(10, 8).ToList();
		Assert.Equal(17, Armor.FindWithFallback(colors, 31));
		Assert.Equal(14, Armor.FindWithFallback(colors, 36));
		Assert.Equal(13, Armor.FindWithFallback(colors, 3));
		Assert.Equal(0, Armor.FindWithFallback([], 3));
	}

	[Fact]
	public void Spawned_soldier_template_merges_previous_values()
	{
		var first = RuleEvent.EmitDescendants(new YamlRootNodeReader("{name: Bob, rank: 2}", "test").ToBase(), default);
		var merged = RuleEvent.EmitDescendants(new YamlRootNodeReader("{rank: 3, armor: STR_NONE_UC}", "test").ToBase(),
			new YamlRootNodeReader(first, "previous").ToBase());
		var reader = new YamlRootNodeReader(merged, "merged").ToBase();
		Assert.Equal(3, reader["rank"].ReadVal<int>());
		Assert.Equal("STR_NONE_UC", reader["armor"].ReadVal<string>());
		Assert.Equal("Bob", reader["name"].ReadVal<string>());
		Assert.Equal("", RuleEvent.EmitDescendants(new YamlRootNodeReader("scalar", "test").ToBase(), default));
		// an empty previous template (the default "" of the C++ YamlString) is valid too
		var fromEmpty = RuleEvent.EmitDescendants(new YamlRootNodeReader("{rank: 1}", "test").ToBase(), new YamlRootNodeReader("", "empty").ToBase());
		Assert.Equal(1, new YamlRootNodeReader(fromEmpty, "x").ToBase()["rank"].ReadVal<int>());
	}
}
