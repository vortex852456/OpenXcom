namespace Acom.Tests.Battlescape;

/// <summary>
/// Open set order and std::sort order against the C++ original (port/tools/golden/pathfinding.cpp, GCC libstdc++).
/// </summary>
public class PathfindingOpenSetTests
{
	private uint _seed;

	/// <summary>Same LCG as the C++ reference program.</summary>
	private int Rnd()
	{
		_seed = _seed * 1103515245u + 12345u;
		return (int)((_seed >> 16) & 0x7fff);
	}

	private static int[] Parse(string s) => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();

	[Fact]
	public void Pop_order_with_ties_and_discarded_entries_matches_original()
	{
		const string expected = "28 52 45 13 33 19 51 2 22 24 56 1 57 28 34 20 50 1 3 16 38 36 37 42 12 46 31 6 10 6 59 54 16 36 18 12 54 28 56 45 53 37 51 14 4 51 35 26 56 31 34 48 43 20 46 23 57 14 25 4 17 15 22 38 37 16 47 19 16 16 12 30 48 7 25 55 18 22 17 19 56 44 16 16 54 17 22 1 52 37 1";
		const string expectedRest = "13 34 38 5 26 23 44 33 50 56 9 45 57 7 29 35 2 10 27 53 28 58 41 20 36 11 51 49 39 21 8 40";

		_seed = 12345;
		const int N = 60;
		var nodes = new List<PathfindingNode>();
		for (int i = 0; i < N; ++i)
			nodes.Add(new PathfindingNode(new Position(i % 10, i / 10, 0)));
		var set = new PathfindingOpenSet();
		var popped = new List<int>();
		for (int step = 0; step < 300; ++step)
		{
			int r = Rnd() % 3;
			if (r < 2 || set.Empty())
			{
				int k = Rnd() % N;
				int t = Rnd() % 6;
				int tx = Rnd() % 10;
				int ty = Rnd() % 6;
				nodes[k].Connect(new PathfindingCost(t, 0), null, 0, new Position(tx, ty, 0));
				set.Push(nodes[k]);
			}
			else
			{
				popped.Add(nodes.IndexOf(set.Pop()));
			}
		}
		var rest = new List<int>();
		while (!set.Empty())
			rest.Add(nodes.IndexOf(set.Pop()));

		Assert.Equal(Parse(expected), popped);
		Assert.Equal(Parse(expectedRest), rest);
		Assert.All(nodes, n => Assert.False(n.InOpenSet()));
	}

	[Fact]
	public void Push_keeps_cheapest_entry_and_discards_stale_ones()
	{
		var a = new PathfindingNode(new Position(0, 0, 0));
		var b = new PathfindingNode(new Position(1, 0, 0));
		var set = new PathfindingOpenSet();
		a.Connect(new PathfindingCost(10, 0), null, 0);
		set.Push(a);
		b.Connect(new PathfindingCost(5, 0), null, 0);
		set.Push(b);
		a.Connect(new PathfindingCost(1, 0), null, 0); // re-push with lower cost, old entry becomes stale
		set.Push(a);
		Assert.Same(a, set.Pop());
		Assert.Same(b, set.Pop());
		Assert.True(set.Empty()); // the stale entry of a was removed
	}

	[Theory]
	[InlineData(0, 200, "151 137 138 176 42 30 140 97 181 165 81 163 103 162 145 146 78 73 110 72 136 147 157 135 156 194 152 6 150 139 68 154 86 71 141 74 36 119 142 144 121 112 108 105 183 24 100 134 91 198 197 120 114 92 113 158 123 124 143 125 126 133 132 128 129 127 131 130 0 32 178 35 27 179 25 22 43 180 182 184 185 186 187 53 188 56 190 189 191 5 192 193 195 4 175 177 49 153 173 149 155 159 160 161 54 164 31 148 47 166 167 169 40 171 170 199 122 172 65 46 89 95 1 34 174 52 38 44 168 33 51 66 62 58 55 59 28 3 7 9 10 57 12 14 50 48 17 18 16 41 15 39 19 37 20 21 26 29 60 98 104 107 94 109 88 87 85 84 115 116 111 117 75 70 64 69 13 2 23 63 8 11 196 106 118 102 101 99 96 93 90 83 82 80 79 77 45 76 67 61")]
	[InlineData(1, 50, "38 26 27 28 29 30 31 32 33 34 35 36 37 25 39 40 41 42 43 44 45 46 47 48 49 13 1 2 3 4 5 6 7 8 9 10 11 12 0 14 15 16 17 18 19 20 21 22 23 24")]
	[InlineData(2, 12, "0 5 1 2 3 6 7 8 4 9 10 11")]
	public void Sort_by_min_node_costs_matches_libstdcxx_order(int variant, int n, string expected)
	{
		_seed = (uint)(777 + variant);
		var nodes = new List<PathfindingNode>();
		for (int i = 0; i < n; ++i)
			nodes.Add(new PathfindingNode(new Position(i % 100, i / 100, 0)));
		for (int i = 0; i < n; ++i)
		{
			int t = variant == 1 ? 3 : Rnd() % 8;
			int e = variant == 1 ? 3 : Rnd() % 8;
			nodes[i].Connect(new PathfindingCost(t, e), null, 0);
		}
		var sorted = new List<PathfindingNode>(nodes);
		StdAlgorithm.Sort(sorted, MinNodeCosts.Compare);
		Assert.Equal(Parse(expected), sorted.Select(x => nodes.IndexOf(x)));
	}

	[Fact]
	public void Cost_operators_compare_both_components_and_wrap_like_Sint16()
	{
		var a = new PathfindingCost(1, 5);
		var b = new PathfindingCost(2, 6);
		var c = new PathfindingCost(3, 1);
		Assert.True(a < b);
		Assert.True(b > a);
		Assert.False(a < c); // incomparable: neither is smaller
		Assert.False(c < a);
		Assert.True(a <= new PathfindingCost(1, 5));
		Assert.False(c <= b);
		Assert.True(b >= a);
		var sum = new PathfindingCost(32767, 0) + new PathfindingCost(1, 2);
		Assert.Equal(-32768, sum.time);
		Assert.Equal(2, sum.energy);
		Assert.Equal(-1, new PathfindingCost(65535, 0).time);
	}

	[Fact]
	public void Node_connect_sets_guess_only_outside_open_set()
	{
		var node = new PathfindingNode(new Position(0, 0, 0));
		var prev = new PathfindingNode(new Position(1, 1, 0));
		node.Connect(new PathfindingCost(7, 3), prev, 5, new Position(3, 4, 1));
		Assert.Equal(20, node.TUGuess); // (short)(4 * 5.099f)
		Assert.Same(prev, node.PrevNode);
		Assert.Equal(5, node.PrevDir);
		Assert.Equal(7, node.GetTUCost(false).time);
		Assert.Equal(0, node.GetTUCost(true).time);

		var set = new PathfindingOpenSet();
		set.Push(node);
		node.Connect(new PathfindingCost(1, 1), null, 2, new Position(10, 0, 0));
		Assert.Equal(20, node.TUGuess); // already in the open set: guess kept
		node.Connect(new PathfindingCost(1, 1), null, 2);
		Assert.Equal(0, node.TUGuess);

		node.SetChecked();
		Assert.True(node.IsChecked);
		node.Reset();
		Assert.False(node.IsChecked);
		Assert.False(node.InOpenSet());
	}
}
