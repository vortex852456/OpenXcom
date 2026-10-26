namespace Acom.Tests.Battlescape;

public class ExplosionTests
{
	private static string Frames(Explosion e)
	{
		var frames = new List<int>();
		do
		{
			frames.Add(e.CurrentFrame);
		}
		while (e.Animate());
		frames.Add(e.CurrentFrame);
		return string.Join(",", frames);
	}

	[Fact]
	public void Frames_match_original()
	{
		// port/tools/golden/pathfinding.cpp, line "E:"
		Assert.Equal("-1,-1,0,1,2,3,4,5,6,7,8,9,10", Frames(new Explosion(new Position(1, 2, 3), 0, 2)));
		Assert.Equal("5,6,7,8,9,10,11,12,13", Frames(new Explosion(default, 5, 0, true)));
		Assert.Equal("-1,3,4,5,6,7", Frames(new Explosion(default, 3, 1, false, true)));
		Assert.Equal("0,1,2,3", Frames(new Explosion(default, 0, 0, false, false, 3)));
	}

	[Fact]
	public void Properties_reflect_constructor()
	{
		var e = new Explosion(new Position(4, 5, 6), 2, 0, true, true);
		Assert.Equal(new Position(4, 5, 6), e.Position);
		Assert.True(e.IsBig);
		Assert.True(e.IsHit);
		Assert.Equal(2, e.CurrentFrame);
	}
}
