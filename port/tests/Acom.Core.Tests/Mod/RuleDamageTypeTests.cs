#pragma warning disable CA1716 // namespace mirrors the C++ folder Mod
namespace Acom.Tests.Mod;

public class RuleDamageTypeTests
{
	private static int Min(int min, int max) => min;
	private static int Max(int min, int max) => max;

	[Fact]
	public void Defaults_and_yaml_load()
	{
		var dmg = new RuleDamageType();
		Assert.Equal(ItemDamageRandomType.DRT_STANDARD, dmg.RandomType);
		Assert.Equal(0.1f, dmg.ToArmor);
		Assert.True(dmg.RandomWound);
		Assert.True(dmg.IsDirect);

		dmg.Load(new YamlRootNodeReader("FixRadius: -1\nRandomType: 2\nResistType: 3\nToTile: 0.25\nRandomTile: true\nTileDamageLimit: 30\n", "test").ToBase());
		Assert.False(dmg.IsDirect);
		Assert.Equal(ItemDamageRandomType.DRT_TFTD, dmg.RandomType);
		Assert.Equal(ItemDamageType.DT_HE, dmg.ResistType);
		Assert.Equal(0.25f, dmg.ToTile);
		Assert.True(dmg.RandomTile);
		Assert.Equal(30, dmg.TileDamageLimit);
		Assert.Equal(1000, dmg.FireThreshold);

		var copy = dmg.Clone();
		copy.ToTile = 2;
		Assert.Equal(0.25f, dmg.ToTile);
		Assert.Equal(30, copy.TileDamageLimit);
	}

	[Theory]
	[InlineData(ItemDamageRandomType.DRT_UFO, ItemDamageType.DT_AP, 0, 200)]
	[InlineData(ItemDamageRandomType.DRT_TFTD, ItemDamageType.DT_AP, 50, 150)]
	[InlineData(ItemDamageRandomType.DRT_FLAT, ItemDamageType.DT_AP, 100, 100)]
	[InlineData(ItemDamageRandomType.DRT_EASY, ItemDamageType.DT_AP, 50, 200)]
	[InlineData(ItemDamageRandomType.DRT_UFO_WITH_TWO_DICE, ItemDamageType.DT_AP, 0, 200)]
	[InlineData(ItemDamageRandomType.DRT_NONE, ItemDamageType.DT_AP, 0, 0)]
	[InlineData(ItemDamageRandomType.DRT_DEFAULT, ItemDamageType.DT_NONE, 0, 0)]
	[InlineData(ItemDamageRandomType.DRT_DEFAULT, ItemDamageType.DT_SMOKE, 0, 0)]
	public void Random_damage_ranges(ItemDamageRandomType randomType, ItemDamageType resist, int min, int max)
	{
		var dmg = new RuleDamageType { RandomType = randomType, ResistType = resist };
		Assert.Equal(min, dmg.GetRandomDamage(100, Min));
		Assert.Equal(max, dmg.GetRandomDamage(100, Max));
		Assert.Equal(min, dmg.GetRandomDamage(100, 1));
		Assert.Equal(max, dmg.GetRandomDamage(100, 2));
	}

	[Fact]
	public void Two_dice_call_the_random_function_twice()
	{
		var dmg = new RuleDamageType { RandomType = ItemDamageRandomType.DRT_UFO_WITH_TWO_DICE };
		var calls = new List<(int, int)>();
		int result = dmg.GetRandomDamage(30, (a, b) => { calls.Add((a, b)); return calls.Count; });
		Assert.Equal(3, result);
		Assert.Equal([(0, 30), (0, 30)], calls);
	}

	[Fact]
	public void Final_damage_rounds_half_away_from_zero()
	{
		var dmg = new RuleDamageType { ToHealth = 0.5f, ToArmor = 0.1f, ToTile = 1.5f, TileDamageLimit = 10, RandomWound = false, ToWound = 0.5f };
		Assert.Equal(3, dmg.GetHealthFinalDamage(5)); // 2.5
		Assert.Equal(4, dmg.GetHealthFinalDamage(7)); // 3.5
		Assert.Equal(0, dmg.GetHealthFinalDamage(0));
		Assert.Equal(0, dmg.GetHealthFinalDamage(-8));
		Assert.Equal(3, dmg.GetArmorFinalDamage(25));
		Assert.Equal(2, dmg.GetArmorFinalDamage(15));
		Assert.Equal(9, dmg.GetTileFinalDamage(6));
		Assert.Equal(10, dmg.GetTileFinalDamage(8));
		Assert.Equal(2, dmg.GetWoundFinalDamage(3));
		Assert.Equal(0, dmg.GetManaFinalDamage(100));

		dmg.TileDamageMethod = 2;
		Assert.Equal(17, dmg.GetRandomDamageForTile(100, 17));
	}
}
