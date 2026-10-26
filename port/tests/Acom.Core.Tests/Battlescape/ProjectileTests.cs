namespace Acom.Tests.Battlescape;

public class ProjectileTests
{
	private static readonly List<Position> s_trajectory = [new(1, 0, 0), new(2, 0, 0), new(3, 1, 0), new(4, 1, 1)];

	[Fact]
	public void Position_from_start_clamps_to_trajectory()
	{
		Assert.Equal(new Position(1, 0, 0), Projectile.GetPositionFromStart(s_trajectory, -5));
		Assert.Equal(new Position(1, 0, 0), Projectile.GetPositionFromStart(s_trajectory, 0));
		Assert.Equal(new Position(3, 1, 0), Projectile.GetPositionFromStart(s_trajectory, 2));
		Assert.Equal(new Position(4, 1, 1), Projectile.GetPositionFromStart(s_trajectory, 3));
		Assert.Equal(new Position(4, 1, 1), Projectile.GetPositionFromStart(s_trajectory, 99));
	}

	[Fact]
	public void Position_from_end_counts_back_from_last_step()
	{
		Assert.Equal(new Position(4, 1, 1), Projectile.GetPositionFromEnd(s_trajectory, 0));
		Assert.Equal(new Position(2, 0, 0), Projectile.GetPositionFromEnd(s_trajectory, Projectile.ItemDropVoxelOffset));
		Assert.Equal(new Position(1, 0, 0), Projectile.GetPositionFromEnd(s_trajectory, -10));
		Assert.Equal(new Position(4, 1, 1), Projectile.GetPositionFromEnd(s_trajectory, 3));
	}

	[Fact]
	public void Empty_trajectory_throws_like_vector_at()
	{
		Assert.ThrowsAny<ArgumentOutOfRangeException>(() => Projectile.GetPositionFromStart([], 0));
	}
}
