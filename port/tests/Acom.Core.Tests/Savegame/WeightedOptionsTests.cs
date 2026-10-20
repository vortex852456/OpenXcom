namespace Acom.Tests.Savegame;

[Collection(nameof(Acom.Tests.Mod.GlobalRng))] // uses the global Rng state
public class WeightedOptionsTests
{
	/// <summary>Same setup as the C++ reference run (expected values were printed by the original WeightedOptions/RNG code).</summary>
	private static WeightedOptions Sample()
	{
		var wo = new WeightedOptions();
		wo.Set("STR_B", 7); // replaced by load
		wo.Set("keep", 4);
		wo.Load(new YamlRootNodeReader("{STR_a: 5, STR_B: 2, STR_A: 3, zero: 0, STR_C: 10}", "wo").ToBase());
		return wo;
	}

	[Theory]
	[InlineData(1UL, "STR_C STR_C STR_C keep STR_C STR_A STR_C STR_C STR_C STR_C STR_C STR_B STR_a keep STR_A STR_C STR_A STR_A STR_C STR_C STR_a STR_C STR_A STR_A", 340862277345022344UL)]
	[InlineData(42UL, "STR_A STR_A STR_C STR_C STR_C STR_C STR_a keep STR_a STR_C STR_C STR_B keep STR_a STR_C STR_B STR_a STR_A STR_C STR_C STR_A STR_C STR_a STR_C", 14141347070837254100UL)]
	[InlineData(0xDEADBEEFUL, "STR_A STR_C STR_A STR_C STR_B STR_C STR_B STR_C STR_C STR_a STR_C STR_C STR_a keep STR_A keep keep STR_C STR_a keep STR_a STR_A STR_A STR_C", 5055841910864777518UL)]
	public void Choose_matches_cpp_with_fixed_seed(ulong seed, string expected, ulong seedAfter)
	{
		var wo = Sample();
		Rng.Seed = seed;
		var picks = Enumerable.Range(0, 24).Select(_ => wo.Choose()).ToList();
		Assert.Equal(expected.Split(' '), picks);
		Assert.Equal(seedAfter, Rng.Seed);
	}

	[Fact]
	public void Save_matches_cpp_and_writes_null_when_empty()
	{
		var writer = new YamlRootNodeWriter();
		writer.SetAsMap();
		Sample().Save(writer["opts"]);
		new WeightedOptions().Save(writer["empty"]);
		Assert.Equal("opts:\n  STR_A: 3\n  STR_B: 2\n  STR_C: 10\n  STR_a: 5\n  keep: 4\nempty: ~\n", writer.Emit().ReplaceLineEndings("\n"));

		var reloaded = new WeightedOptions();
		reloaded.Load(writer.ToReader()["opts"]);
		Assert.Equal(Sample().Names, reloaded.Names);
		var empty = new WeightedOptions();
		empty.Load(writer.ToReader()["empty"]);
		Assert.True(empty.Empty());
	}

	[Fact]
	public void Set_load_and_copy_semantics()
	{
		var wo = Sample();
		// std::map order: ordinal, upper case before lower case; weight 0 is never stored
		Assert.Equal(["STR_A", "STR_B", "STR_C", "STR_a", "keep"], wo.Names);
		wo.Set("STR_C", 0);
		wo.Set("missing", 0);
		Assert.Equal(["STR_A", "STR_B", "STR_a", "keep"], wo.Names);
		// load only adds / changes entries, 0 removes
		wo.Load(new YamlRootNodeReader("{keep: 0, new: 1}", "wo").ToBase());
		Assert.Equal(["STR_A", "STR_B", "STR_a", "new"], wo.Names);

		var copy = new WeightedOptions(wo);
		copy.Clear();
		Assert.True(copy.Empty());
		Assert.False(wo.Empty());
		Assert.Empty(copy.Names);

		Rng.Seed = 5;
		Assert.Equal("", copy.Choose());
		Assert.Equal(5UL, Rng.Seed); // empty choice does not touch the RNG

		var single = new WeightedOptions();
		single.Set("only", 3);
		Assert.Equal("only", single.Choose());
		Assert.Throws<YamlReadException>(() => single.Load(new YamlRootNodeReader("{bad: -1}", "wo").ToBase()));
	}
}
