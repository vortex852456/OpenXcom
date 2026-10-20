using System.Globalization;
using Bulwark.Engine;

namespace Bulwark.Tests.Engine;

public class RngTests
{
	/// <summary>Replays every recorded C++ sequence in golden/rng.txt and compares bit by bit.</summary>
	[Fact]
	public void Matches_cpp_reference_sequences()
	{
		var lines = Golden.ReadLines("rng.txt");
		Rng.RandomState? state = null;
		int checkedValues = 0;
		foreach (var line in lines)
		{
			var f = Golden.Fields(line);
			var inv = CultureInfo.InvariantCulture;
			switch (f[0])
			{
				case "seed":
					state = new Rng.RandomState(ulong.Parse(f[1], inv));
					Rng.Seed = ulong.Parse(f[1], inv);
					break;
				case "next":
					foreach (var v in f.Skip(1))
					{
						Assert.Equal(ulong.Parse(v, inv), state!.Next());
						checkedValues++;
					}
					break;
				case "generate":
					int min = int.Parse(f[1], inv), max = int.Parse(f[2], inv);
					foreach (var v in f.Skip(3))
					{
						Assert.Equal(int.Parse(v, inv), state!.Generate(min, max));
						checkedValues++;
					}
					break;
				case "percent":
					foreach (var v in f.Skip(1))
					{
						Assert.Equal(v == "1", state!.Percent(37));
						checkedValues++;
					}
					break;
				case "subseq":
					var sub = state!.SubSequence();
					Assert.Equal(ulong.Parse(f[1], inv), sub.Seed);
					Assert.Equal(ulong.Parse(f[2], inv), state.Seed);
					break;
				case "double":
					foreach (var v in f.Skip(1))
					{
						Assert.Equal(double.Parse(v, inv), Rng.Generate(-2.5, 7.25));
						checkedValues++;
					}
					break;
				case "shuffle":
					var list = Enumerable.Range(0, 12).ToList();
					Rng.Shuffle(list);
					Assert.Equal(f.Skip(1).Select(x => int.Parse(x, inv)), list);
					break;
				case "end":
					Assert.Equal(ulong.Parse(f[1], inv), Rng.Seed);
					break;
				default:
					Assert.Fail($"Unknown golden line: {line}");
					break;
			}
		}
		Assert.True(checkedValues > 500, "golden file looks truncated");
	}

	[Fact]
	public void Shuffle_of_empty_list_does_not_touch_seed()
	{
		Rng.Seed = 7;
		Rng.Shuffle(new List<int>());
		Assert.Equal(7UL, Rng.Seed);
	}
}
