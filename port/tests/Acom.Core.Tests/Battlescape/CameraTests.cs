using static Acom.Tests.Battlescape.MapTestHelpers;

namespace Acom.Tests.Battlescape;

public class CameraTests
{
	[Theory]
	[InlineData(0, 0, 0, 0, 0)]
	[InlineData(1, 0, 0, 16, 8)]
	[InlineData(0, 1, 0, -16, 8)]
	[InlineData(3, 5, 0, -32, 64)]
	[InlineData(0, 0, 1, 0, -24)] // (spriteHeight + spriteWidth / 4) / 2 per level
	[InlineData(2, 1, 2, 16, -24)]
	public void Map_to_screen_is_isometric(int x, int y, int z, int sx, int sy)
	{
		Camera camera = NewCamera(10, 10, 4);
		Position screen = new(99, 99, 99);
		camera.ConvertMapToScreen(new Position(x, y, z), ref screen);
		Assert.Equal(new Position(sx, sy, 0), screen);
	}

	[Fact]
	public void Screen_to_map_inverts_map_to_screen_at_the_floor_centre()
	{
		Camera camera = NewCamera(20, 20, 4);
		for (int level = 0; level < 2; ++level)
		{
			Position offset = camera.MapOffset;
			offset.z = (short)level;
			camera.MapOffset = offset;
			for (int x = 0; x < 20; ++x)
			{
				for (int y = 0; y < 20; ++y)
				{
					Position screen = default;
					camera.ConvertMapToScreen(new Position(x, y, level), ref screen);
					screen += camera.MapOffset;
					// the floor diamond of a 32x40 tile sprite is centred at (16, 32)
					int mx = 0, my = 0;
					camera.ConvertScreenToMap(screen.x + 16, screen.y + 32, ref mx, ref my);
					Assert.Equal((x, y), (mx, my));
				}
			}
		}
	}

	[Fact]
	public void Screen_to_map_clamps_to_one_tile_outside_the_map()
	{
		Camera camera = NewCamera(10, 12, 4);
		int mx = 0, my = 0;
		camera.ConvertScreenToMap(-5000, -5000, ref mx, ref my);
		Assert.Equal(-1, mx);
		camera.ConvertScreenToMap(5000, 5000, ref mx, ref my);
		Assert.Equal(10, mx);
		camera.ConvertScreenToMap(-5000, 5000, ref mx, ref my);
		Assert.Equal(12, my);
	}

	[Fact]
	public void Voxel_to_screen_adds_sub_tile_offset_and_map_offset()
	{
		Camera camera = NewCamera(10, 10, 4);
		camera.MapOffset = new Position(100, 50, 0);
		Position screen = default;
		// tile (1, 2, 0) + voxel (4, 6, 10): x = 16 - 32 + (4 - 6) + 16, y = 8 + 16 + (int)(20 + 2 + 3 - 10)
		camera.ConvertVoxelToScreen(new Position(16 + 4, 32 + 6, 10), ref screen);
		Assert.Equal(new Position(100 - 16 - 2 + 16, 50 + 24 + 15, 0), screen);
	}

	[Fact]
	public void Center_on_position_sets_offset_and_clamps()
	{
		Camera camera = NewCamera(10, 10, 4);
		camera.CenterOnPosition(new Position(5, 5, 2), false);
		// screen of (5,5,2) = (0, 80 - 48); offset = (160, 82) - that
		Assert.Equal(new Position(160, 82 - 32, 2), camera.MapOffset);
		Assert.Equal(new Position(5, 5, 2), camera.CenterPosition);

		camera.CenterOnPosition(new Position(50, -7, 1), false);
		Assert.Equal(new Position(10, -1, 1), camera.CenterPosition);
	}

	[Theory]
	[InlineData(100000, 0)]
	[InlineData(-100000, 0)]
	[InlineData(0, 100000)]
	[InlineData(0, -100000)]
	[InlineData(-3000, 7000)]
	[InlineData(5, 3)]
	public void Scroll_keeps_the_view_centre_inside_the_map(int dx, int dy)
	{
		Camera camera = NewCamera(10, 15, 4);
		camera.CenterOnPosition(new Position(5, 7, 0), false);
		camera.ScrollXY(dx, dy, false);
		Position c = camera.CenterPosition;
		Assert.InRange(c.x, 0, 9);
		Assert.InRange(c.y, 0, 14);

		// the centre is the tile under the middle of the visible map
		int mx = 0, my = 0;
		camera.ConvertScreenToMap(320 / 2, 164 / 2, ref mx, ref my);
		Assert.Equal((c.x, c.y), (mx, my));
	}

	[Fact]
	public void Scroll_inside_the_map_moves_the_offset_exactly()
	{
		Camera camera = NewCamera(40, 40, 4);
		camera.CenterOnPosition(new Position(20, 20, 0), false);
		Position before = camera.MapOffset;
		camera.ScrollXY(8, -4, false);
		Assert.Equal(new Position(before.x + 8, before.y - 4, 0), camera.MapOffset);
	}

	[Fact]
	public void Jump_does_not_clamp()
	{
		Camera camera = NewCamera(10, 10, 4);
		camera.CenterOnPosition(new Position(5, 5, 0), false);
		camera.JumpXY(100000, 0);
		Assert.Equal(-1, camera.CenterPosition.y);
	}

	[Theory]
	[InlineData(0, 0, false, 0, false, true)]
	[InlineData(-10, 0, false, 0, false, false)]
	[InlineData(18, 0, false, 0, false, true)]
	[InlineData(19, 0, false, 0, false, false)]
	public void Is_on_screen_checks_screen_rectangle(int x, int y, bool walking, int size, bool boundary, bool expected)
	{
		Camera camera = NewCamera(40, 40, 4);
		camera.MapOffset = new Position(0, 0, 0);
		// tile (x, 0): screen (16x + 16, 8x + 36); visible while 0 <= x <= 310 and 0 <= y <= 190
		Assert.Equal(expected, camera.IsOnScreen(new Position(x, y, 0), walking, size, boundary));
	}

	[Fact]
	public void Show_all_layers_toggles_between_two_and_one()
	{
		Camera camera = NewCamera(10, 10, 4);
		Assert.False(camera.ShowAllLayers);
		Assert.Equal(2, camera.ToggleShowAllLayers());
		Assert.True(camera.ShowAllLayers);
		Assert.Equal(1, camera.ToggleShowAllLayers());
	}
}
