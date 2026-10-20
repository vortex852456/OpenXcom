namespace Acom.Tests.Savegame;

public class NodeTests
{
	private static YamlNodeReader Parse(string yaml) => new YamlRootNodeReader(yaml, "test").ToBase();

	private static string Emit(Node node)
	{
		var writer = new YamlRootNodeWriter();
		node.Save(writer);
		return writer.Emit().Trim();
	}

	[Fact]
	public void Save_writes_a_flow_map_in_cpp_key_order()
	{
		var node = new Node(3, new Position(10, 20, 1), 7, Node.TYPE_SMALL, 2, 5, 0, 4);
		node.NodeLinks.AddRange([1, 2, -1, 255, 255]);

		Assert.Equal("{id: 3, position: [10, 20, 1], type: 2, rank: 2, flags: 5, reserved: 0, priority: 4, links: [1, 2, -1, 255, 255]}", Emit(node));
	}

	[Fact]
	public void Yaml_roundtrip_keeps_all_fields_but_the_segment()
	{
		const string yaml = "{id: 12, position: [3, 4, 2], type: 5, rank: 7, flags: 9, reserved: 5, priority: 6, allocated: true, links: [11, -1, 0], dummy: true}";
		var node = new Node();
		node.Load(Parse(yaml));

		Assert.Equal(12, node.ID);
		Assert.Equal(new Position(3, 4, 2), node.Position);
		Assert.Equal(5, node.Type);
		Assert.Equal(NodeRank.NR_MEDIC, node.Rank);
		Assert.Equal(9, node.Flags);
		Assert.Equal(6, node.Priority);
		Assert.True(node.IsAllocated);
		Assert.True(node.IsTarget); // reserved == 5
		Assert.True(node.IsDummy);
		Assert.Equal([11, -1, 0], node.NodeLinks);
		Assert.Equal(0, node.Segment); // "segment" is neither saved nor loaded

		Assert.Equal(yaml, Emit(node));
	}

	[Fact]
	public void Optional_flags_are_only_written_when_set()
	{
		var node = new Node();
		node.Load(Parse("{id: 1, allocated: true, dummy: true}"));
		node.FreeNode();
		node.SetDummy(false);
		Assert.Equal("{id: 1, position: [0, 0, 0], type: 0, rank: 0, flags: 0, reserved: 0, priority: 0, links: []}", Emit(node));

		node.AllocateNode();
		Assert.True(node.IsAllocated);
		Assert.False(node.IsTarget);
	}

	[Fact]
	public void Node_rank_table_maps_alien_ranks_to_node_ranks()
	{
		Assert.Equal(8, Node.nodeRank.GetLength(0));
		Assert.Equal(7, Node.nodeRank.GetLength(1));
		Assert.Equal(4, Node.nodeRank[0, 0]); // commander
		Assert.Equal(7, Node.nodeRank[3, 0]); // medic
		for (int rank = 0; rank < 8; ++rank)
			Assert.Equal(0, Node.nodeRank[rank, 6]); // all fall back to scout nodes
	}
}
