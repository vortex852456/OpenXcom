namespace Acom.Tests.Savegame;

public class CraftWeaponProjectileTests
{
	private static CraftWeaponProjectile Missile(int speed, int range, Directions direction)
	{
		var p = new CraftWeaponProjectile(null);
		p.SetSpeed(speed);
		p.Range = range;
		p.Direction = (int)direction;
		return p;
	}

	[Fact]
	public void Defaults_match_cpp_constructor()
	{
		var p = new CraftWeaponProjectile(null);
		Assert.Equal(CraftWeaponProjectileType.CWPT_CANNON_ROUND, p.Type);
		Assert.Equal(CraftWeaponProjectileGlobalType.CWPGT_MISSILE, p.GlobalType);
		Assert.Equal((int)Directions.D_NONE, p.Direction);
		Assert.Equal(100, p.ShieldDamageModifier);
		Assert.Equal(0, p.State);
		Assert.False(p.Missed);
		Assert.False(p.ToBeRemoved());
	}

	[Fact]
	public void Missile_moving_up_stops_exactly_at_range_then_misses()
	{
		// range 3 = 24 position units, speed 10
		var p = Missile(10, 3, Directions.D_UP);
		int[] expected = [10, 20, 24, 34];
		bool[] missed = [false, false, false, true];
		for (int i = 0; i < expected.Length; ++i)
		{
			p.Move();
			Assert.Equal(expected[i], p.Position);
			Assert.Equal(missed[i], p.Missed);
		}
	}

	[Fact]
	public void Missile_moving_down_decreases_position_and_up_resets_it()
	{
		var p = Missile(7, 10, Directions.D_NONE);
		p.Position = 100;
		p.Direction = (int)Directions.D_DOWN; // only D_UP resets the position
		p.Move();
		p.Move();
		Assert.Equal(86, p.Position);

		p.Direction = (int)Directions.D_UP;
		Assert.Equal(0, p.Position);
	}

	[Fact]
	public void Missile_without_direction_only_covers_distance()
	{
		var p = Missile(8, 1, Directions.D_NONE);
		p.Move(); // reaches range 1 (8 units)
		Assert.False(p.Missed);
		p.Move(); // passed range on previous tick
		Assert.True(p.Missed);
		Assert.Equal(0, p.Position);
	}

	[Fact]
	public void Beam_halves_state_and_is_removed_at_one()
	{
		var p = new CraftWeaponProjectile(null);
		p.Type = CraftWeaponProjectileType.CWPT_LASER_BEAM;
		Assert.Equal(CraftWeaponProjectileGlobalType.CWPGT_BEAM, p.GlobalType);
		Assert.Equal(8, p.State);

		p.Move();
		Assert.Equal(4, p.State);
		Assert.False(p.ToBeRemoved());
		p.Move();
		Assert.Equal(2, p.State);
		Assert.False(p.ToBeRemoved());
		p.Move();
		Assert.Equal(1, p.State);
		Assert.True(p.ToBeRemoved());
		Assert.Equal(0, p.Position);
	}

	[Fact]
	public void Non_beam_types_stay_missiles()
	{
		var p = new CraftWeaponProjectile(null);
		p.Type = CraftWeaponProjectileType.CWPT_FUSION_BALL;
		Assert.Equal(CraftWeaponProjectileGlobalType.CWPGT_MISSILE, p.GlobalType);
		Assert.Equal(0, p.State);
		p.Remove();
		Assert.True(p.ToBeRemoved());
	}
}
