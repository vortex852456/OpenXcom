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
