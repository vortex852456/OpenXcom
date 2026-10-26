namespace Acom.Tests.Battlescape;

public class ParticleTests
{
	[Theory]
	// port/tools/golden/pathfinding.cpp, line "D:" (tile offset | offsetX offsetY layerZ tileZ opacity)
	[InlineData(0, 37, 21, 50, 0, 0, 0, "0 0 0 0 -46 4 2 3")]
	[InlineData(1, 15, 15, 23, 300, -200, 100, "1 0 0 -14 -16 0 0 3")]
	[InlineData(2, 0, 0, 0, -384, 384, -384, "-1 0 -1 13 -3063 254 127 3")]
	[InlineData(3, 100, 3, 71, 4000, 100, 6000, "1 0 1 0 -92 6 3 3")]
	public void Screen_position_matches_original(int i, int vx, int vy, int vz, int ox, int oy, int oz, string expected)
	{
		var p = new Particle(new Position(vx, vy, vz), new Position(ox, oy, oz), default, default, 128, 5, (byte)(32 + i * 4), 2);
		Position t = p.UpdateScreenPosition();
		string actual = $"{t.x} {t.y} {t.z} {p.OffsetX} {p.OffsetY} {p.LayerZ} {p.TileZ} {p.Opacity}";
		Assert.Equal(expected, actual);
		Assert.Equal(5, p.Color);
		Assert.Equal(2, p.Size);
	}

	[Fact]
	public void Animate_counts_down_lifetime_and_copy_is_independent()
	{
		// voxel (8,8,12) = subvoxel (2048,2048,3072), no drift, rising 100 subvoxels per frame
		var p = new Particle(new Position(8, 8, 12), default, new Position(0, 0, 100), default, 0, 1, 3, 0);
		var copy = new Particle(p);
		Assert.True(p.Animate());
		Assert.True(p.Animate());
		Assert.False(p.Animate()); // opacity reached 0
		p.UpdateScreenPosition();
		copy.UpdateScreenPosition();
		Assert.Equal(-4, copy.OffsetY); // 8/2 + 8/2 - 12
		Assert.Equal(1, copy.Opacity); // (3 + 7) / 10
		Assert.Equal(-5, p.OffsetY); // z = (3072 + 300) / 256 = 13
		Assert.Equal(0, p.Opacity);
		Assert.Equal(0, p.OffsetX);
	}
}
