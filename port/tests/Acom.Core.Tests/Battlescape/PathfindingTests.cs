namespace Acom.Tests.Battlescape;

public class PathfindingTests
{
	[Fact]
	public void Direction_vectors_match_original()
	{
		// port/tools/golden/pathfinding.cpp, line "C:"
		var expected = new (int x, int y, int z, int dir)[]
		{
			(0, -1, 0, 0), (1, -1, 0, 1), (1, 0, 0, 2), (1, 1, 0, 3), (0, 1, 0, 4),
			(-1, 1, 0, 5), (-1, 0, 0, 6), (-1, -1, 0, 7), (0, 0, 1, -1), (0, 0, -1, -1),
		};
		for (int d = 0; d < 10; ++d)
		{
			Position v = new(7, 7, 7);
			Pathfinding.DirectionToVector(d, ref v);
			Assert.Equal(new Position(expected[d].x, expected[d].y, expected[d].z), v);
			Assert.Equal(expected[d].dir, Pathfinding.VectorToDirection(v));
			int dir = 42;
			Pathfinding.VectorToDirection(v, ref dir);
			Assert.Equal(expected[d].dir, dir);
		}
	}

	[Fact]
	public void Vector_to_direction_ignores_z_and_rejects_longer_vectors()
	{
		Assert.Equal(3, Pathfinding.VectorToDirection(new Position(1, 1, 5)));
		Assert.Equal(-1, Pathfinding.VectorToDirection(new Position(2, 0, 0)));
		Assert.Equal(-1, Pathfinding.VectorToDirection(new Position(0, 0, 0)));
	}

	[Fact]
	public void Constants_match_original()
	{
		Assert.Equal(4, Pathfinding.DEFAULT_MOVE_COST);
		Assert.Equal(8, Pathfinding.DEFAULT_MOVE_FLY_COST);
		Assert.Equal(255, Pathfinding.INVALID_MOVE_COST);
		Assert.Equal(8, Pathfinding.DIR_UP);
		Assert.Equal(9, Pathfinding.DIR_DOWN);
		Assert.Equal(3, Pathfinding.red);
		Assert.Equal(4, Pathfinding.green);
		Assert.Equal(10, Pathfinding.yellow);
	}
}
