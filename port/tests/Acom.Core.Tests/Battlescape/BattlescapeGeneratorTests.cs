namespace Acom.Tests.Battlescape;

public class BattlescapeGeneratorTests
{
	[Fact]
	public void Map_header_is_y_x_z_signed()
	{
		Assert.Equal((20, 10, 4), BattlescapeGenerator.ReadMapHeader([10, 20, 4]));
		Assert.Equal((-1, 10, 0), BattlescapeGenerator.ReadMapHeader([10, 0xFF]));
	}

	[Fact]
	public void Map_tiles_run_x_then_y_then_down_in_z()
	{
		// 2x2x2 block, 8 records + 2 trailing bytes (incomplete record is ignored)
		var data = new List<byte> { 2, 2, 2 };
		for (byte i = 0; i < 8; ++i)
		{
			data.AddRange([i, (byte)(i + 10), (byte)(i + 20), (byte)(i + 30)]);
		}
		data.AddRange([99, 99]);

		var tiles = BattlescapeGenerator.ReadMapTiles([.. data], 10, 20, 1, 2, 2);

		Assert.Equal(8, tiles.Count);
		Assert.Equal(new MapTileRecord(10, 20, 1, 0, 10, 20, 30), tiles[0]);
		Assert.Equal((11, 20, 1), (tiles[1].X, tiles[1].Y, tiles[1].Z));
		Assert.Equal((10, 21, 1), (tiles[2].X, tiles[2].Y, tiles[2].Z));
		Assert.Equal((10, 20, 0), (tiles[4].X, tiles[4].Y, tiles[4].Z));
		Assert.Equal((11, 21, 0), (tiles[7].X, tiles[7].Y, tiles[7].Z));
		Assert.Equal(7, tiles[7][(int)TilePart.O_FLOOR]);
		Assert.Equal(17, tiles[7][(int)TilePart.O_WESTWALL]);
		Assert.Equal(27, tiles[7][(int)TilePart.O_NORTHWALL]);
		Assert.Equal(37, tiles[7][(int)TilePart.O_OBJECT]);
	}

	private static byte[] RmpNode(byte x, byte y, byte z, byte[] links, byte type = 0, byte rank = 0, byte flags = 0, byte reserved = 0, byte priority = 0)
	{
		var rec = new byte[BattlescapeGenerator.RMP_RECORD_SIZE];
		rec[0] = y;
		rec[1] = x;
		rec[2] = z;
		for (int j = 0; j < 5; ++j)
		{
			rec[4 + j * 3] = links[j];
		}
		rec[19] = type;
		rec[20] = rank;
		rec[21] = flags;
		rec[22] = reserved;
		rec[23] = priority;
		return rec;
	}

	[Fact]
	public void Rmp_nodes_offset_links_and_flip_z()
	{
		byte[] data =
		[
			.. RmpNode(1, 2, 0, [1, 255, 254, 253, 251], type: 3, rank: 4, flags: 5, reserved: 6, priority: 7),
			.. RmpNode(3, 4, 2, [0, 252, 250, 255, 255]),
			0, 1, 2, // incomplete record
		];

		var nodes = BattlescapeGenerator.ReadRmpNodes(data, "test.RMP", 10, 10, 4, 10, 20, 1, 5);

		Assert.Equal(2, nodes.Count);
		Assert.False(nodes[0].IsDummy);
		Assert.Equal(new Position(11, 22, 4 - 1 - 0 + 1), nodes[0].Position);
		Assert.Equal((3, 4, 5, 6, 7), (nodes[0].Type, nodes[0].Rank, nodes[0].Flags, nodes[0].Reserved, nodes[0].Priority));
		Assert.Equal([6, -1, -2, -3, -5], nodes[0].Links);
		Assert.Equal(new Position(13, 24, 2), nodes[1].Position);
		Assert.Equal([5, -4, 255, -1, -1], nodes[1].Links);
	}

	[Fact]
	public void Rmp_culls_nodes_outside_block_and_terminates_links_to_them()
	{
		byte[] data =
		[
			.. RmpNode(0, 0, 0, [1, 2, 255, 255, 255]),
			.. RmpNode(10, 0, 0, [0, 255, 255, 255, 255]), // x outside the 10x10 block
			.. RmpNode(0, 0, 0, [1, 0, 255, 255, 255]),
		];

		var nodes = BattlescapeGenerator.ReadRmpNodes(data, "test.RMP", 10, 10, 1, 0, 0, 0, 3);

		Assert.True(nodes[1].IsDummy);
		Assert.Empty(nodes[1].Links);
		Assert.Equal([-1, 5, -1, -1, -1], nodes[0].Links);
		Assert.Equal([-1, 3, -1, -1, -1], nodes[2].Links);
	}
}

[Collection(nameof(Acom.Tests.Mod.GlobalRng))] // seeds the global generator
public class BattlescapeGeneratorLayoutTests
{
	private static MapBlock[][] Grid(int sizeX, int sizeY)
	{
		var grid = new MapBlock[sizeX][];
		for (int x = 0; x < sizeX; ++x)
		{
			grid[x] = new MapBlock[sizeY];
		}
		return grid;
	}

	[Fact]
	public void SelectPosition_picks_evenly_from_free_positions_in_x_major_order()
	{
		// 4x3 block map; (1,0) and (2,1) are used; a 20x20 block needs 2x2 free blocks
		var blocks = Grid(4, 3);
		blocks[1][0] = new MapBlock("used");
		blocks[2][1] = new MapBlock("used");
		// free 2x2 positions in x-major order: (0,1)
		(int, int)[] valid2 = [(0, 1)];
		// free 1x1 positions: all except the used ones
		var valid1 = new List<(int, int)>();
		for (int x = 0; x < 4; ++x)
			for (int y = 0; y < 3; ++y)
				if (blocks[x][y] == null) valid1.Add((x, y));

		for (ulong seed = 1; seed < 20; ++seed)
		{
			Rng.Seed = seed;
			int expected = Rng.Generate(0, valid1.Count - 1);
			Rng.Seed = seed;
			int x = -1, y = -1;
			Assert.True(BattlescapeGenerator.SelectPosition(blocks, 40, 30, [], ref x, ref y, 10, 10));
			Assert.Equal(valid1[expected], (x, y));
		}

		int bx = -1, by = -1;
		Assert.True(BattlescapeGenerator.SelectPosition(blocks, 40, 30, [], ref bx, ref by, 20, 20));
		Assert.Equal(valid2[0], (bx, by));
	}

	[Fact]
	public void SelectPosition_respects_rects_dedups_overlaps_and_fails_without_room()
	{
		var blocks = Grid(4, 4);
		// overlapping rects: (1..2, 1..2) and (2..3, 2..3) share (2,2)
		List<SDL_Rect> rects = [new SDL_Rect(1, 1, 2, 2), new SDL_Rect(2, 2, 2, 2)];
		(int, int)[] valid = [(1, 1), (1, 2), (2, 1), (2, 2), (2, 3), (3, 2), (3, 3)];

		Rng.Seed = 12345;
		var expected = new List<(int, int)>();
		for (int i = 0; i < 10; ++i) expected.Add(valid[Rng.Generate(0, valid.Length - 1)]);
		Rng.Seed = 12345;
		for (int i = 0; i < 10; ++i)
		{
			int x = 0, y = 0;
			Assert.True(BattlescapeGenerator.SelectPosition(blocks, 40, 40, rects, ref x, ref y, 10, 10));
			Assert.Equal(expected[i], (x, y));
		}

		// a rect smaller than the block, and a rect clipped by the map edge
		int fx = 7, fy = 8;
		Assert.False(BattlescapeGenerator.SelectPosition(blocks, 40, 40, [new SDL_Rect(0, 0, 1, 1)], ref fx, ref fy, 20, 20));
		Assert.False(BattlescapeGenerator.SelectPosition(blocks, 40, 40, [new SDL_Rect(3, 3, 2, 2)], ref fx, ref fy, 20, 20));
		Assert.Equal((7, 8), (fx, fy));
	}
}
