namespace Acom.Tests.Battlescape;

public class PositionTests
{
	[Fact]
	public void Arithmetic_truncates_to_16_bit_like_Sint16()
	{
		var p = new Position(40000, 2, 3) + new Position(1, 1, 1);
		Assert.Equal(new Position(40001 - 65536, 3, 4), p);
		Assert.Equal(new Position(2, 1, 0), new Position(35, 20, 12).ToTile());
		Assert.Equal(new Position(3, 4, 12), new Position(35, 20, 12).ClipVoxel());
		Assert.Equal(5, Position.Distance2d(new Position(0, 0, 0), new Position(3, 4, 9)));
		Assert.Equal("(1,2,3)", new Position(1, 2, 3).ToString());
	}

	[Fact]
	public void Yaml_roundtrip_uses_flow_sequence()
	{
		var writer = new YamlRootNodeWriter();
		writer.Write("pos", new Position(1, -2, 3));
		Assert.Equal("pos: [1, -2, 3]", writer.Emit().Trim());
		var reader = new YamlRootNodeReader(writer.Emit(), "test").ToBase();
		Assert.Equal(new Position(1, -2, 3), reader.Read("pos", default(Position)));
	}
}
