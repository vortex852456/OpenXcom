#pragma warning disable CA1716 // namespace mirrors the C++ folder Ufopaedia
namespace Acom.Tests.Ufopaedia;

/// <summary>Value formatting helpers of the Stats for Nerds screen and the item article.</summary>
public class StatsForNerdsStateTests
{
	private static RuleStatBonus Bonus(string yaml)
	{
		var bonus = new RuleStatBonus();
		bonus.Load("STR_TEST", new YamlRootNodeReader("b:\n" + yaml, "test.rul").ToBase(), "b");
		return bonus;
	}

	[Theory]
	[InlineData(0.0f, "0")]
	[InlineData(0.5f, "0.5")]
	[InlineData(1.25f, "1.25")]
	[InlineData(100.0f, "100")]
	[InlineData(1234567.0f, "1.23457e+06")]
	[InlineData(0.00001f, "1e-05")]
	[InlineData(-2.5f, "-2.5")]
	public void Floats_are_formatted_like_ostream(float value, string expected)
	{
		Assert.Equal(expected, StatsForNerdsState.FormatFloat(value));
	}

	[Fact]
	public void Float_percentages_keep_float_precision()
	{
		// ostream << (value * 100.0f): float math, 6 significant digits
		Assert.Equal("33.3333", StatsForNerdsState.FormatFloat(1.0f / 3.0f * 100.0f));
		Assert.Equal("15", StatsForNerdsState.FormatFloat(0.15f * 100.0f));
		Assert.Equal("0.333333", StatsForNerdsState.FormatDouble(1.0 / 3.0));
	}

	[Fact]
	public void AreSame_uses_relative_epsilon()
	{
		Assert.True(StatsForNerdsState.AreSame(1.0f, 1.0f + 1e-8f));
		Assert.False(StatsForNerdsState.AreSame(1.0f, 1.001f));
		Assert.True(StatsForNerdsState.AreSame(1e6, 1e6 + 1e-11));
		Assert.False(StatsForNerdsState.AreSame(0.0, 1e-300 + 1e-15));
	}

	[Fact]
	public void Stat_bonus_terms_are_joined_with_signs_and_powers()
	{
		var bonus = Bonus("  firing: [0.5]\n  reactions: [0, -0.02]\n");
		Assert.Equal("0.5*[firing] - 0.02*[reactions]^2", StatsForNerdsState.FormatRuleStatBonus(bonus, s => "[" + s + "]", false));
	}

	[Fact]
	public void Stat_bonus_flat_terms()
	{
		// flatHundred is multiplied by 100^power, a factor of 1 is not printed
		var bonus = Bonus("  flatHundred: [0.3]\n  strength: [1.0]\n");
		Assert.Equal("30 + [strength]", StatsForNerdsState.FormatRuleStatBonus(bonus, s => "[" + s + "]", false));

		// flatOne: Stats for Nerds prints the number only, the item article (missing "else" in C++) also the stat name
		var flatOne = Bonus("  flatOne: [2]\n");
		Assert.Equal("2", StatsForNerdsState.FormatRuleStatBonus(flatOne, s => "[" + s + "]", false));
		Assert.Equal("22*[flatOne]", StatsForNerdsState.FormatRuleStatBonus(flatOne, s => "[" + s + "]", true));
	}

	[Fact]
	public void Translation_maps_cover_the_same_stats()
	{
		Assert.Equal(StatsForNerdsState.translationMap.Keys, StatsForNerdsState.shortTranslationMap.Keys);
		Assert.Equal("STR_FIRING_ACCURACY", StatsForNerdsState.translationMap["firing"]);
		Assert.Equal("STR_FIRING_ACCURACY_ABBREVIATION", StatsForNerdsState.shortTranslationMap["firing"]);
		Assert.Equal("", StatsForNerdsState.translationMap["flatHundred"]);
		// every stat name a RuleStatBonus can parse has a translation
		var bonus = Bonus("  tu: [1]\n  psi: [1]\n  manaNormalized: [1]\n  firingReactionsScaled: [1]\n");
		Assert.All(bonus.BonusRaw, term => Assert.True(StatsForNerdsState.translationMap.ContainsKey(term.first)));
	}

	[Theory]
	[InlineData(0L, "", "0")]
	[InlineData(999L, "", "999")]
	[InlineData(1000L, "", "1\u00a0000")]
	[InlineData(-1234567L, "", "-1\u00a0234\u00a0567")]
	[InlineData(1500L, "$", "$1\u00a0500")]
	[InlineData(-250L, "$", "-$250")]
	[InlineData(42000L, "\u20ac", "42\u00a0000\u20ac")] // multi-byte currency goes behind
	public void Numbers_are_grouped_by_thousands(long value, string currency, string expected)
	{
		Assert.Equal(expected, Unicode.FormatNumber(value, currency));
	}

	[Fact]
	public void Percentages_and_funding()
	{
		Assert.Equal("15%", Unicode.FormatPercentage(15));
		Assert.Equal("-3%", Unicode.FormatPercentage(-3));
		Assert.Equal(Unicode.OXCE_CURRENCY_SYMBOL.Length == 1 ? Unicode.OXCE_CURRENCY_SYMBOL + "1\u00a0000\u00a0000" : "1\u00a0000\u00a0000" + Unicode.OXCE_CURRENCY_SYMBOL, Unicode.FormatFunding(1000000));
	}
}
