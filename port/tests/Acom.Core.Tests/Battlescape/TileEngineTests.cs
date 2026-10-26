using Acom.Tests.Savegame;

namespace Acom.Tests.Battlescape;

public class TileEngineTests
{
	// Synthetic LOFTEMPS: 0 empty, 1 solid, 2 west wall (voxel column x = 0), 3 north wall (row y = 0).
	private const int LoftEmpty = 0, LoftSolid = 1, LoftWest = 2, LoftNorth = 3;

	private static (SavedBattleGame save, TileEngine te) NewMap(int x, int y, int z)
	{
		var mod = new Acom.Mod();
		mod.Inventories["STR_GROUND"] = new RuleInventory("STR_GROUND", 0);
		for (int i = 0; i < 16; ++i) mod.VoxelData.Add(0);
		for (int i = 0; i < 16; ++i) mod.VoxelData.Add(0xFFFF);
		for (int i = 0; i < 16; ++i) mod.VoxelData.Add(0x8000);
		for (int i = 0; i < 16; ++i) mod.VoxelData.Add(i == 0 ? (ushort)0xFFFF : (ushort)0);
		var save = new SavedBattleGame(mod, new Language());
		save.InitMap(x, y, z);
		return (save, new TileEngine(save, mod));
	}

	private static MapData Part(TilePart part, int loft, int visionBlock = 0, int heBlock = 0, int smokeBlock = 0, bool noFloor = false)
	{
		MapData md = TileTests.MapDataWithFlags(objectType: part);
		md.SetNoFloor(noFloor);
		md.SetBlockValue(0, visionBlock, heBlock, smokeBlock, 0, 0);
		for (int layer = 0; layer < 12; ++layer)
		{
			md.SetLoftID(loft, layer);
		}
		return md;
	}

	private static List<Position> Helper(Position origin, Position target, bool drift)
	{
		var points = new List<Position>();
		TileEngine.CalculateLineHelper(origin, target, p => { points.Add(p); return false; }, p => { if (drift) points.Add(p); return false; });
		return points;
	}

	[Fact]
	public void Line_and_parabola_helpers_match_cpp()
	{
		int lines = 0, parabolas = 0;
		foreach (string line in Golden.ReadLines("trajectory.txt"))
		{
			string[] head = line.Split(" : ")[0].Split(' ');
			string[] f = Golden.Fields(line.Split(" : ").Length > 1 ? line.Split(" : ")[1] : "");
			var origin = new Position(int.Parse(head[1]), int.Parse(head[2]), int.Parse(head[3]));
			var target = new Position(int.Parse(head[4]), int.Parse(head[5]), int.Parse(head[6]));
			var actual = new List<string>();
			if (head[0] == "line")
			{
				int n = 0;
				TileEngine.CalculateLineHelper(origin, target,
					p => { actual.Add($"P {p.x} {p.y} {p.z}"); return ++n >= 2000; },
					p => { actual.Add($"D {p.x} {p.y} {p.z}"); return false; });
				Assert.Equal(string.Join(' ', f), string.Join(' ', actual));
				++lines;
			}
			else
			{
				double curvature = double.Parse(head[7], System.Globalization.CultureInfo.InvariantCulture);
				var delta = new Position(int.Parse(head[8]), int.Parse(head[9]), int.Parse(head[10]));
				int n = 0;
				TileEngine.CalculateParabolaHelper(origin, target, curvature, delta, p => { actual.Add($"{p.x} {p.y} {p.z}"); return ++n >= 400; });
				Assert.Equal(line, $"{line.Split(" :")[0]} :{string.Concat(actual.Select(a => " " + a))}");
				++parabolas;
			}
		}
		Assert.Equal(56, lines);
		Assert.Equal(252, parabolas);
	}

	[Fact]
	public void Line_voxel_through_empty_map_stores_main_steps()
	{
		var (_, te) = NewMap(4, 4, 2);
		var origin = new Position(8, 8, 12);
		var target = new Position(60, 30, 20);
		var trajectory = new List<Position>();
		Assert.Equal(VoxelType.V_EMPTY, te.CalculateLineVoxel(origin, target, true, trajectory, null));
		Assert.Equal(Helper(origin, target, false), trajectory);
		Assert.Equal(53, trajectory.Count);
		Assert.Equal(target, trajectory[^1]);
	}

	[Fact]
	public void Line_voxel_stops_at_west_wall_and_reports_impact()
	{
		var (save, te) = NewMap(4, 4, 2);
		save.GetTile(new Position(2, 1, 0)).SetMapData(Part(TilePart.O_WESTWALL, LoftWest), 0, 0, TilePart.O_WESTWALL);

		var trajectory = new List<Position>();
		VoxelType hit = te.CalculateLineVoxel(new Position(8, 24, 12), new Position(60, 24, 12), false, trajectory, null);
		Assert.Equal(VoxelType.V_WESTWALL, hit);
		Assert.Equal([new Position(32, 24, 12)], trajectory);

		// the wall only fills the first voxel column of its tile: a line starting behind it passes
		te.VoxelCheckFlush();
		Assert.Equal(VoxelType.V_EMPTY, te.CalculateLineVoxel(new Position(33, 24, 12), new Position(60, 24, 12), false, null, null));
		// a line along the wall column hits the very first voxel
		Assert.Equal(VoxelType.V_WESTWALL, te.VoxelCheck(new Position(32, 17, 3), null));
		Assert.Equal(VoxelType.V_EMPTY, te.VoxelCheck(new Position(31, 17, 3), null));
	}

	[Fact]
	public void Voxel_check_bounds_floor_and_object()
	{
		var (save, te) = NewMap(3, 3, 2);
		save.GetTile(new Position(1, 1, 0)).SetMapData(Part(TilePart.O_FLOOR, LoftSolid), 0, 0, TilePart.O_FLOOR);
		MapData obj = Part(TilePart.O_OBJECT, LoftEmpty);
		obj.SetLoftID(LoftNorth, 5); // voxel z 10..11
		save.GetTile(new Position(0, 0, 1)).SetMapData(obj, 0, 0, TilePart.O_OBJECT);

		Assert.Equal(VoxelType.V_OUTOFBOUNDS, te.VoxelCheck(new Position(-1, 0, 0), null));
		Assert.Equal(VoxelType.V_OUTOFBOUNDS, te.VoxelCheck(new Position(48, 0, 0), null));
		Assert.Equal(VoxelType.V_OUTOFBOUNDS, te.VoxelCheck(new Position(0, 0, 48), null));
		Assert.Equal(VoxelType.V_FLOOR, te.VoxelCheck(new Position(20, 20, 5), null));
		Assert.Equal(VoxelType.V_EMPTY, te.VoxelCheck(new Position(4, 4, 5), null));
		Assert.Equal(VoxelType.V_OBJECT, te.VoxelCheck(new Position(7, 0, 24 + 10), null));
		Assert.Equal(VoxelType.V_OBJECT, te.VoxelCheck(new Position(7, 0, 24 + 11), null));
		Assert.Equal(VoxelType.V_EMPTY, te.VoxelCheck(new Position(7, 1, 24 + 11), null));
		Assert.Equal(VoxelType.V_EMPTY, te.VoxelCheck(new Position(7, 0, 24 + 12), null));

		// straight down onto the solid floor
		var trajectory = new List<Position>();
		Assert.Equal(VoxelType.V_FLOOR, te.CalculateLineVoxel(new Position(20, 20, 40), new Position(20, 20, 0), true, trajectory, null));
		Assert.Equal(new Position(20, 20, 23), trajectory[^1]);
		Assert.Equal(trajectory[^2], trajectory[^1]); // impact point is stored twice when storing the whole path
	}

	[Fact]
	public void Parabola_lands_on_floor()
	{
		var (save, te) = NewMap(6, 3, 2);
		for (int x = 0; x < 6; ++x)
		{
			MapData floor = Part(TilePart.O_FLOOR, LoftEmpty);
			floor.SetLoftID(LoftSolid, 0); // voxel z 0..1
			save.GetTile(new Position(x, 1, 0)).SetMapData(floor, 0, 0, TilePart.O_FLOOR);
		}
		var trajectory = new List<Position>();
		int result = te.CalculateParabolaVoxel(new Position(8, 16, 20), new Position(80, 16, 2), true, trajectory, null, 1.5, new Position(0, 0, 0));
		Assert.Equal((int)VoxelType.V_FLOOR, result);
		Position impact = trajectory[^1];
		Assert.Equal(16, impact.y);
		Assert.True(impact.z <= 1, impact.ToString());
		Assert.True(impact.x > 8 && impact.x < 96, impact.ToString());
		Assert.Equal(new Position(8, 16, 20), trajectory[0]);
	}

	[Theory]
	[InlineData(0, -5, 0)]
	[InlineData(5, -5, 1)]
	[InlineData(5, 0, 2)]
	[InlineData(5, 5, 3)]
	[InlineData(0, 5, 4)]
	[InlineData(-5, 5, 5)]
	[InlineData(-5, 0, 6)]
	[InlineData(-5, -5, 7)]
	[InlineData(0, 0, 4)] // atan2(+0, -0) == pi
	[InlineData(1, -3, 0)]
	[InlineData(2, -3, 1)]
	[InlineData(3, 1, 2)]
	[InlineData(-1, 4, 4)]
	public void Direction_to_target(int dx, int dy, int dir)
	{
		var (_, te) = NewMap(1, 1, 1);
		var origin = new Position(10, 10, 0);
		Assert.Equal(dir, te.GetDirectionTo(origin, origin + new Position(dx, dy, 3)));
	}

	[Fact]
	public void Arc_direction()
	{
		var (_, te) = NewMap(1, 1, 1);
		Assert.Equal(4, te.GetArcDirection(7, 3));
		Assert.Equal(1, te.GetArcDirection(0, 7));
		Assert.Equal(1, te.GetArcDirection(7, 0));
		Assert.Equal(0, te.GetArcDirection(5, 5));
		Assert.Equal(2, te.GetArcDirection(1, 7));
		Assert.Equal(3, te.GetArcDirection(2, 7));
	}

	[Fact]
	public void Trajectory_step_size()
	{
		List<Position> path = [new(0, 0, 0), new(1, 0, 0), new(2, 1, 0), new(3, 1, 1)];
		Assert.Equal(0.0f, TileEngine.TrajectoryStepSize(path, 0));
		Assert.Equal(1.0f, TileEngine.TrajectoryStepSize(path, 1));
		Assert.Equal(1.0f, TileEngine.TrajectoryStepSize(path, 2));
		Assert.Equal(0.5f * (float)Math.Sqrt(6), TileEngine.TrajectoryStepSize(path, 3));
		Assert.Equal(0.0f, TileEngine.TrajectoryStepSize(path, 4));
		Assert.Equal(0.0f, TileEngine.TrajectoryStepSize(path, -1));
		Assert.Equal(new Position(-1, -1, -1), TileEngine.invalid);
	}

	[Fact]
	public void Vertical_blockage_by_floors()
	{
		var (save, te) = NewMap(2, 2, 3);
		Tile ground = save.GetTile(new Position(0, 0, 0));
		Tile middle = save.GetTile(new Position(0, 0, 1));
		Tile top = save.GetTile(new Position(0, 0, 2));
		middle.SetMapData(Part(TilePart.O_FLOOR, LoftSolid, heBlock: 0), 0, 0, TilePart.O_FLOOR);
		MapData md = middle.GetMapData(TilePart.O_FLOOR);
		md.Armor = 30;

		Assert.Equal(255, te.VerticalBlockage(null, middle, ItemDamageType.DT_HE));
		Assert.Equal(0, te.VerticalBlockage(middle, middle, ItemDamageType.DT_HE));
		// a floor without vision block stops sight completely, explosions by its armor
		Assert.Equal(256, te.VerticalBlockage(ground, middle, ItemDamageType.DT_NONE));
		Assert.Equal(256, te.VerticalBlockage(middle, ground, ItemDamageType.DT_NONE));
		Assert.Equal(30, te.VerticalBlockage(ground, middle, ItemDamageType.DT_HE));
		Assert.Equal(30, te.VerticalBlockage(middle, ground, ItemDamageType.DT_HE));
		// no floor at the top level: nothing blocks between middle and top
		Assert.Equal(0, te.VerticalBlockage(middle, top, ItemDamageType.DT_HE));
		Assert.Equal(0, te.VerticalBlockage(top, middle, ItemDamageType.DT_NONE));

		md.SetHEBlock(12);
		Assert.Equal(12, te.VerticalBlockage(ground, middle, ItemDamageType.DT_HE));
		// smoke never passes a floor (regular wall: wall == -1)
		Assert.Equal(256, te.VerticalBlockage(ground, middle, ItemDamageType.DT_SMOKE));
	}

	[Fact]
	public void Face_window_picks_first_see_through_wall()
	{
		var (save, te) = NewMap(3, 3, 1);
		Assert.Equal(-1, te.FaceWindow(new Position(1, 1, 0)));
		save.GetTile(new Position(1, 1, 0)).SetMapData(Part(TilePart.O_WESTWALL, LoftWest), 0, 0, TilePart.O_WESTWALL);
		Assert.Equal(6, te.FaceWindow(new Position(1, 1, 0)));
		save.GetTile(new Position(1, 2, 0)).SetMapData(Part(TilePart.O_NORTHWALL, LoftNorth), 0, 0, TilePart.O_NORTHWALL);
		Assert.Equal(4, te.FaceWindow(new Position(1, 1, 0)));
		save.GetTile(new Position(1, 1, 0)).SetMapData(Part(TilePart.O_NORTHWALL, LoftNorth, visionBlock: 1), 0, 0, TilePart.O_NORTHWALL);
		Assert.Equal(4, te.FaceWindow(new Position(1, 1, 0)));
		save.GetTile(new Position(1, 1, 0)).SetMapData(Part(TilePart.O_NORTHWALL, LoftNorth), 0, 0, TilePart.O_NORTHWALL);
		Assert.Equal(0, te.FaceWindow(new Position(1, 1, 0)));
	}

	[Fact]
	public void Moving_unit_stack()
	{
		var (_, te) = NewMap(1, 1, 1);
		var a = SavegameTestHelpers.Uninitialized<BattleUnit>();
		var b = SavegameTestHelpers.Uninitialized<BattleUnit>();
		Assert.Null(te.MovingUnit);
		te.AddMovingUnit(a);
		te.AddMovingUnit(b);
		Assert.Same(b, te.MovingUnit);
		Assert.Throws<InvalidOperationException>(() => te.RemoveMovingUnit(a));
		te.RemoveMovingUnit(b);
		Assert.Same(a, te.MovingUnit);
		te.RemoveMovingUnit(a);
		Assert.Null(te.MovingUnit);
	}
}
