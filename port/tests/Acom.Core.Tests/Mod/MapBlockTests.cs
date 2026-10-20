using Acom.Tests.Engine;

#pragma warning disable CA1716 // namespace follows the source area (src/Mod), like Acom.Tests.<Area>
namespace Acom.Tests.Mod;
#pragma warning restore CA1716

public class MapBlockTests
{
	[Fact]
	public void Map_block_loads_groups_items_and_sizes()
	{
		var block = new MapBlock("X");
		block.Load(new YamlRootNodeReader("""
			name: UFO1
			width: 20
			length: 10
			height: 3
			groups: 2
			revealedFloors: [0, 2]
			items:
			  STR_ELERIUM_115: [[1, 2, 0], [3, 4, 1]]
			fuseTimers:
			  STR_PROXIMITY_GRENADE: [1, 3]
			randomizedItems:
			  - {position: [5, 5, 0], amount: 2, itemList: [A, B]}
			extendedItems:
			  - {type: STR_RIFLE, pos: [[1, 1, 0]], ammoDef: [[STR_RIFLE_CLIP, 0]]}
			craftInventoryTile: [1, 2, 0]
			""", "test").ToBase());
		Assert.Equal("UFO1", block.Name);
		Assert.Equal(20, block.SizeX);
		Assert.Equal(10, block.SizeY);
		Assert.Equal(3, block.SizeZ);
		Assert.True(block.IsInGroup(2));
		Assert.False(block.IsInGroup(0));
		Assert.True(block.IsFloorRevealed(2));
		Assert.False(block.IsFloorRevealed(1));
		Assert.Equal([new Position(1, 2, 0), new Position(3, 4, 1)], block.Items["STR_ELERIUM_115"]);
		Assert.Equal((1, 3), block.ItemsFuseTimers["STR_PROXIMITY_GRENADE"]);
		var random = Assert.Single(block.RandomizedItems);
		Assert.Equal(new Position(5, 5, 0), random.position);
		Assert.Equal(2, random.amount);
		Assert.Equal(-1, random.fuseTimerMin);
		Assert.False(random.mixed);
		Assert.Equal(["A", "B"], random.itemList);
		var extended = Assert.Single(block.ExtendedItems);
		Assert.Equal("STR_RIFLE", extended.type);
		Assert.Equal([new Position(1, 1, 0)], extended.pos);
		Assert.Equal([("STR_RIFLE_CLIP", 0)], extended.ammoDef);
		Assert.Equal(-1, extended.fuseTimerMax);
		Assert.Equal([1, 2, 0], block.CraftInventoryTile);

		Assert.Throws<InvalidOperationException>(() => new MapBlock("BAD").Load(new YamlRootNodeReader("width: 15", "test").ToBase()));
	}

	[Fact]
	public void Repository_terrain_blocks_load()
	{
		var path = Path.Combine(LanguageTests.RepoRoot(), "bin", "standard", "xcom1", "terrains.rul");
		var root = YamlRootNodeReader.FromFile(path);
		int count = 0;
		foreach (var terrain in root["terrains"].Children())
		{
			foreach (var blockReader in terrain["mapBlocks"].Children())
			{
				var block = new MapBlock(blockReader["name"].ReadVal<string>());
				block.Load(blockReader);
				Assert.Equal(0, block.SizeX % 10);
				count++;
			}
		}
		Assert.True(count > 100);

		var culta = root["terrains"][0]["mapBlocks"];
		var first = new MapBlock("CULTA00");
		first.Load(culta[0]);
		Assert.True(first.IsInGroup(1));
		Assert.False(first.IsInGroup(0));
		var second = new MapBlock("CULTA01");
		second.Load(culta[1]);
		Assert.True(second.IsInGroup(0)); // default group
		Assert.Equal(4, second.SizeZ);
	}
}
