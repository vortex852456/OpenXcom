using Acom.Tests.Engine;

#pragma warning disable CA1716 // namespace mirrors the C++ folder Mod
namespace Acom.Tests.Mod;

[Collection(nameof(GlobalRng))]
public class SoldierNamePoolTests
{
	private static SoldierNamePool FromText(string yaml)
	{
		var pool = new SoldierNamePool();
		pool.Load(new YamlRootNodeReader(yaml, "test.nam").ToBase(), "test.nam");
		return pool;
	}

	[Fact]
	public void All_repository_name_pools_load()
	{
		var files = Directory.GetFiles(Path.Combine(LanguageTests.RepoRoot(), "bin", "common", "SoldierName"), "*.nam");
		Assert.True(files.Length >= 30, "name pools missing");
		Rng.Seed = 99;
		foreach (var file in files)
		{
			var pool = new SoldierNamePool();
			pool.Load(YamlRootNodeReader.FromFile(file).ToBase(), file);
			Assert.Equal(100, pool.GlobalWeight);
			Assert.Equal("", pool.Country);
			Assert.Equal("", pool.Region);

			SoldierGender gender = SoldierGender.GENDER_MALE;
			string name = pool.GenName(ref gender, 50);
			Assert.False(string.IsNullOrWhiteSpace(name), file);
			int look = pool.GenLook(4);
			Assert.InRange(look, 0, 3);
		}
	}

	[Fact]
	public void Female_lists_default_to_the_male_lists()
	{
		var pool = FromText("maleFirst: [Alan]\nmaleLast: [Smith]\nfemaleFrequency: 100\nmaleCallsign: [Ace]\n");
		SoldierGender gender = SoldierGender.GENDER_MALE;
		Rng.Seed = 5;
		Assert.Equal("Alan Smith", pool.GenName(ref gender, 0));
		Assert.Equal(SoldierGender.GENDER_FEMALE, gender);
		Assert.Equal("Ace", pool.GenCallsign(SoldierGender.GENDER_FEMALE));

		var noLast = FromText("maleFirst: [Bob]\nfemaleFirst: [Eve]\nfemaleFrequency: 0\n");
		Assert.Equal("Bob", noLast.GenName(ref gender, 100));
		Assert.Equal(SoldierGender.GENDER_MALE, gender);
		Assert.Equal("", noLast.GenCallsign(SoldierGender.GENDER_MALE));

		// without its own frequency the pool uses the soldier type's
		var shared = FromText("maleFirst: [Bob]\nfemaleFirst: [Eve]\n");
		Assert.Equal("Eve", shared.GenName(ref gender, 100));
		Assert.Equal("Bob", shared.GenName(ref gender, 0));
	}

	[Fact]
	public void Name_generation_follows_the_cpp_rng_call_order()
	{
		var pool = FromText("maleFirst: [A, B, C]\nmaleLast: [X, Y]\nfemaleFirst: [F, G]\nfemaleLast: [Q]\n");
		Rng.Seed = 777;
		SoldierGender gender = SoldierGender.GENDER_MALE;
		string name = pool.GenName(ref gender, 40);

		var rng = new Rng.RandomState(777);
		bool female = rng.Percent(40);
		string expected = female
			? new[] { "F", "G" }[rng.Generate(0, 1)] + " " + new[] { "Q" }[rng.Generate(0, 0)]
			: new[] { "A", "B", "C" }[rng.Generate(0, 2)] + " " + new[] { "X", "Y" }[rng.Generate(0, 1)];
		Assert.Equal(expected, name);
		Assert.Equal(female ? SoldierGender.GENDER_FEMALE : SoldierGender.GENDER_MALE, gender);
		Assert.Equal(rng.Seed, Rng.Seed);
	}

	[Fact]
	public void Look_weights_are_padded_and_trimmed()
	{
		// weight 0 means never: only look 1 can be chosen
		var pool = FromText("maleFirst: [A]\nlookWeights: [0, 5, 0, 0, 7]\n");
		Rng.Seed = 3;
		for (int i = 0; i < 20; ++i)
			Assert.Equal(1, pool.GenLook(2));

		// a missing look gets the minimum chance of 2
		var padded = FromText("maleFirst: [A]\nlookWeights: [0]\n");
		for (int i = 0; i < 20; ++i)
			Assert.Equal(1, padded.GenLook(2));

		// all weights zero: uniform pick
		var zero = FromText("maleFirst: [A]\nlookWeights: [0, 0]\n");
		Rng.Seed = 11;
		int look = zero.GenLook(2);
		Assert.Equal(new Rng.RandomState(11).Generate(0, 1), look);
	}

	[Fact]
	public void Global_weight_and_empty_lists_are_checked()
	{
		var pool = FromText("maleFirst: [A]\nglobalWeight: -5\ncountry: STR_X\nregion: STR_Y\n");
		Assert.Equal(100, pool.GlobalWeight);
		Assert.Equal("STR_X", pool.Country);
		Assert.Equal("STR_Y", pool.Region);
		Assert.Equal(7, FromText("maleFirst: [A]\nglobalWeight: 7\n").GlobalWeight);
		var e = Assert.Throws<InvalidOperationException>(() => FromText("femaleFirst: [A]\n"));
		Assert.Contains("maleFirst", e.Message, StringComparison.Ordinal);
	}
}
