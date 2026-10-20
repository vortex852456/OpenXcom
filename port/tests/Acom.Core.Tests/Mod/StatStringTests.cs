using Acom.Tests.Engine;

#pragma warning disable CA1716 // namespace mirrors the C++ folder Mod
namespace Acom.Tests.Mod;

public class StatStringTests
{
	private static List<StatString> LoadXcomUtil()
	{
		var path = Path.Combine(LanguageTests.RepoRoot(), "bin", "standard", "XcomUtil_Statstrings", "XcomUtil_Statstrings.rul");
		var reader = YamlRootNodeReader.FromFile(path).ToBase();
		var list = new List<StatString>();
		foreach (var child in reader["statStrings"].Children())
		{
			var s = new StatString();
			s.Load(child);
			list.Add(s);
		}
		return list;
	}

	private static UnitStats Stats(int firing, int reactions, int tu, int bravery, int strength, int psiStrength, int psiSkill = 0)
	{
		var s = UnitStats.Scalar(40);
		s.firing = (short)firing;
		s.reactions = (short)reactions;
		s.tu = (short)tu;
		s.bravery = (short)bravery;
		s.strength = (short)strength;
		s.psiStrength = (short)psiStrength;
		s.psiSkill = (short)psiSkill;
		return s;
	}

	[Fact]
	public void Conditions_use_xcomutil_defaults_for_open_ranges()
	{
		var list = LoadXcomUtil();
		Assert.Equal(14, list.Count);
		Assert.Equal("x", list[0].String);
		var x = Assert.Single(list[0].Conditions);
		Assert.Equal(("psiStrength", 0, 30), (x.ConditionName, x.MinVal, x.MaxVal));
		Assert.Equal(("psiStrength", 80, 255), (list[1].Conditions[0].ConditionName, list[1].Conditions[0].MinVal, list[1].Conditions[0].MaxVal));
		// conditions are kept in the fixed stat name order, not in file order
		var snpr = list.Single(s => s.String == "Snpr");
		Assert.Equal(["firing", "reactions"], snpr.Conditions.Select(c => c.ConditionName));
		var sct = list.Single(s => s.String == "Sct");
		Assert.Equal(["reactions", "tu"], sct.Conditions.Select(c => c.ConditionName));
	}

	[Fact]
	public void Single_letters_accumulate_and_a_longer_string_ends_the_search()
	{
		var list = LoadXcomUtil();
		var sniper = Stats(firing: 72, reactions: 65, tu: 50, bravery: 30, strength: 30, psiStrength: 25);
		Assert.Equal("Snpr", StatString.CalcStatString(ref sniper, list, psiStrengthEval: false, inTraining: false));
		// psi stats only count when psi skill is known or the psi strength may be shown
		Assert.Equal("xSnpr", StatString.CalcStatString(ref sniper, list, psiStrengthEval: true, inTraining: false));
		var withSkill = Stats(firing: 72, reactions: 65, tu: 50, bravery: 30, strength: 30, psiStrength: 25, psiSkill: 35);
		Assert.Equal("xkSnpr", StatString.CalcStatString(ref withSkill, list, psiStrengthEval: false, inTraining: false));

		var scout = Stats(firing: 65, reactions: 55, tu: 70, bravery: 70, strength: 20, psiStrength: 50);
		Assert.Equal("bwmSct", StatString.CalcStatString(ref scout, list, psiStrengthEval: true, inTraining: false));

		var none = Stats(firing: 40, reactions: 40, tu: 40, bravery: 40, strength: 40, psiStrength: 40);
		Assert.Equal("", StatString.CalcStatString(ref none, list, psiStrengthEval: true, inTraining: false));
	}

	[Fact]
	public void Psi_training_condition_needs_the_soldier_in_training()
	{
		var reader = new YamlRootNodeReader("- string: \"T\"\n  psiTraining: [~, ~]\n- string: \"Z\"\n  bravery: [10]\n", "test").ToBase();
		var list = new List<StatString>();
		foreach (var child in reader.Children())
		{
			var s = new StatString();
			s.Load(child);
			list.Add(s);
		}
		Assert.Equal((10, 255), (list[1].Conditions[0].MinVal, list[1].Conditions[0].MaxVal));

		var stats = UnitStats.Scalar(5);
		Assert.Equal("", StatString.CalcStatString(ref stats, list, psiStrengthEval: false, inTraining: false));
		Assert.Equal("T", StatString.CalcStatString(ref stats, list, psiStrengthEval: false, inTraining: true));
		stats.bravery = 10;
		Assert.Equal("TZ", StatString.CalcStatString(ref stats, list, psiStrengthEval: false, inTraining: true));

		var condition = new StatStringCondition("psiTraining", 50, 60);
		Assert.True(condition.IsMet(0, false));
		Assert.False(new StatStringCondition("psiSkill", 0, 100).IsMet(50, false));
		Assert.True(new StatStringCondition("psiSkill", 0, 100).IsMet(50, true));
		Assert.False(new StatStringCondition("melee", 0, 100).IsMet(101, true));
	}

	[Fact]
	public void Current_stats_map_uses_rule_names()
	{
		var stats = new UnitStats(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
		var map = StatString.GetCurrentStats(ref stats);
		Assert.Equal(12, map.Count);
		Assert.Equal(12, map["manaPool"]);
		Assert.Equal(1, map["tu"]);
		Assert.Equal(9, map["psiStrength"]);
		Assert.False(map.ContainsKey("psiTraining"));
	}
}
