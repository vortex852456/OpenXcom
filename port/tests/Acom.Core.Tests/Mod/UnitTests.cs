#pragma warning disable CA1716 // namespace mirrors the C++ folder Mod
namespace Acom.Tests.Mod;

/// <summary>Tests that use the global <see cref="Rng"/>; they run alone so <c>RngTests</c> is not disturbed.</summary>
[CollectionDefinition(nameof(GlobalRng), DisableParallelization = true)]
public sealed class GlobalRng;

public class UnitStatsTests
{
	private static UnitStats Seq(int start) => new(start, start + 1, start + 2, start + 3, start + 4, start + 5, start + 6, start + 7, start + 8, start + 9, start + 10, start + 11);

	[Fact]
	public void Ptr_indexer_addresses_the_named_field()
	{
		var s = Seq(1);
		Assert.Equal(s.tu, s[UnitStats.Ptr.tu]);
		Assert.Equal(s.stamina, s[UnitStats.Ptr.stamina]);
		Assert.Equal(s.health, s[UnitStats.Ptr.health]);
		Assert.Equal(s.bravery, s[UnitStats.Ptr.bravery]);
		Assert.Equal(s.reactions, s[UnitStats.Ptr.reactions]);
		Assert.Equal(s.firing, s[UnitStats.Ptr.firing]);
		Assert.Equal(s.throwing, s[UnitStats.Ptr.throwing]);
		Assert.Equal(s.strength, s[UnitStats.Ptr.strength]);
		Assert.Equal(s.psiStrength, s[UnitStats.Ptr.psiStrength]);
		Assert.Equal(s.psiSkill, s[UnitStats.Ptr.psiSkill]);
		Assert.Equal(s.melee, s[UnitStats.Ptr.melee]);
		Assert.Equal(s.mana, s[UnitStats.Ptr.mana]);

		s[UnitStats.Ptr.mana] = 99;
		Assert.Equal(99, s.mana);
		Assert.Throws<ArgumentOutOfRangeException>(() => Seq(0)[(UnitStats.Ptr)UnitStats.FieldCount]);

		var visited = new List<UnitStats.Ptr>();
		UnitStats.FieldLoop(visited.Add);
		Assert.Equal(Enum.GetValues<UnitStats.Ptr>(), visited);
	}

	[Fact]
	public void Arithmetic_operators_work_field_by_field_and_wrap_to_16_bit()
	{
		var a = Seq(10);
		var b = Seq(1);
		Assert.Equal(new UnitStats(11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31, 33), a + b);
		Assert.Equal(UnitStats.Scalar(9), a - b);
		Assert.Equal(new UnitStats(-10, -11, -12, -13, -14, -15, -16, -17, -18, -19, -20, -21), -a);

		var big = UnitStats.Scalar(30000);
		Assert.Equal(UnitStats.Scalar(60000 - 65536), big + big);
		Assert.Equal(UnitStats.Scalar(short.MinValue), -UnitStats.Scalar(short.MinValue));
	}

	[Fact]
	public void Merge_takes_over_non_zero_stats()
	{
		var s = Seq(1);
		s.Merge(new UnitStats(0, 50, 0, 0, 0, -3, 0, 0, 0, 0, 0, 7));
		Assert.Equal(new UnitStats(1, 50, 3, 4, 5, -3, 7, 8, 9, 10, 11, 7), s);
	}

	[Fact]
	public void Template_merge_soft_limit_combine_and_is_random()
	{
		var orig = Seq(1);
		var fix = new UnitStats(-1, 0, 40, 0, 0, 0, 0, 0, 0, 0, 0, -1);
		Assert.Equal(new UnitStats(0, 2, 40, 4, 5, 6, 7, 8, 9, 10, 11, 0), UnitStats.TemplateMerge(orig, fix));

		// change <= 0 is kept; positive changes stop at the bound; above the bound nothing is added
		var change = new UnitStats(-5, 0, 10, 10, 10, 0, 0, 0, 0, 0, 0, 0);
		var current = new UnitStats(50, 50, 50, 55, 70, 0, 0, 0, 0, 0, 0, 0);
		var bound = UnitStats.Scalar(60);
		Assert.Equal(new UnitStats(-5, 0, 10, 5, 0, 0, 0, 0, 0, 0, 0, 0), UnitStats.SoftLimit(change, current, bound));

		var mask = new UnitStats(1, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 1);
		Assert.Equal(new UnitStats(100, 2, 100, 4, 5, 6, 7, 8, 9, 10, 11, 100), UnitStats.Combine(mask, Seq(1), UnitStats.Scalar(100)));

		Assert.Equal(new UnitStats(0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0),
			UnitStats.IsRandom(new UnitStats(0, 1, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0), new UnitStats(0, 2, 5, 3, 0, 0, 0, 0, 0, 0, 0, 0)));
	}

	[Fact]
	public void Percent_scalar_min_max_and_fixed_minimum()
	{
		Assert.Equal(new UnitStats(5, 11, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0),
			UnitStats.Percent(new UnitStats(50, 75, 1, -7, 0, 0, 0, 0, 0, 0, 0, 0), new UnitStats(10, 15, 50, 20, 0, 0, 0, 0, 0, 0, 0, 0)));
		// int math before truncating to 16 bit: 300 * 300 * 2 / 100 = 1800
		Assert.Equal(1800, UnitStats.Percent(UnitStats.Scalar(300), UnitStats.Scalar(300), 2).tu);

		var a = new UnitStats(1, 9, -4, 0, 0, 0, 0, 0, 0, 0, 0, 0);
		var b = new UnitStats(2, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1);
		Assert.Equal(new UnitStats(2, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), UnitStats.Max(a, b));
		Assert.Equal(new UnitStats(1, 3, -4, 0, 0, 0, 0, 0, 0, 0, 0, -1), UnitStats.Min(a, b));
		Assert.Equal(new UnitStats(0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0), UnitStats.ObeyFixedMinimum(new UnitStats(-3, 0, -10, 0, 0, 0, 0, 0, 0, 0, 0, 0)));
		Assert.Equal(new UnitStats(5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5), UnitStats.ObeyFixedMinimum(UnitStats.Scalar(5)));
	}

	[Fact]
	public void Yaml_read_and_write_round_trip()
	{
		var reader = new YamlRootNodeReader("stats:\n  tu: 60\n  mana: -2\n", "test").ToBase();
		// tryRead reads into the existing value, readVal(default) starts from zero like C++ (the default is only used without node)
		var s = Seq(1);
		Assert.True(reader.TryRead("stats", ref s));
		Assert.Equal(new UnitStats(60, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, -2), s);
		Assert.Equal(new UnitStats(60, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -2), reader["stats"].ReadVal(Seq(1)));
		Assert.Equal(Seq(1), reader["missing"].ReadVal(Seq(1)));
		var merged = Seq(1);
		merged.Merge(reader["stats"].ReadVal(merged));
		Assert.Equal(new UnitStats(60, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, -2), merged);

		var writer = new YamlRootNodeWriter();
		writer.Write("stats", Seq(1));
		var text = writer.Emit();
		Assert.Contains("psiStrength: 9", text, StringComparison.Ordinal);
		Assert.Equal(Seq(1), new YamlRootNodeReader(text, "test").ToBase().Read("stats", default(UnitStats)));
	}
}

[Collection(nameof(GlobalRng))]
public class UnitStatsRandomTests
{
	[Fact]
	public void Random_calls_the_rng_only_for_differing_stats_in_field_order()
	{
		var a = new UnitStats(10, 5, 30, 0, 0, 0, 0, 0, 0, 0, 0, 7);
		var b = new UnitStats(20, 5, 10, 0, 0, 0, 0, 0, 0, 0, 0, 3);
		Rng.Seed = 1234;
		var r = UnitStats.Random(a, b);
		var expected = new Rng.RandomState(1234);
		Assert.Equal(new UnitStats(expected.Generate(10, 20), 5, expected.Generate(10, 30), 0, 0, 0, 0, 0, 0, 0, 0, expected.Generate(3, 7)), r);
		Assert.Equal(expected.Seed, Rng.Seed);
	}
}

public class UnitTests
{
	[Fact]
	public void Emit_descendants_adds_missing_default_keys()
	{
		var node = new YamlRootNodeReader("t:\n  a: 1\n  b: [x, y]\n", "test").ToBase()["t"];
		var first = Unit.EmitDescendants(node, new YamlRootNodeReader("", "(spawned soldier template)").ToBase());
		var parsed = new YamlRootNodeReader(first, "first").ToBase();
		Assert.Equal(1, parsed.Read("a", 0));
		Assert.Equal(["x", "y"], parsed.Read<List<string>>("b", null!));

		// a later mod only changes "a": "b" comes from the previous template
		var later = new YamlRootNodeReader("t:\n  a: 2\n", "test").ToBase()["t"];
		var merged = new YamlRootNodeReader(Unit.EmitDescendants(later, parsed), "merged").ToBase();
		Assert.Equal(2, merged.Read("a", 0));
		Assert.Equal(["x", "y"], merged.Read<List<string>>("b", null!));
		Assert.Equal(["a", "b"], merged.Children().Select(c => c.Key));

		Assert.Equal("", Unit.EmitDescendants(new YamlRootNodeReader("t: 5", "test").ToBase()["t"], default));
	}
}
