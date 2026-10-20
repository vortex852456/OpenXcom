#pragma warning disable CA1716 // namespace mirrors src/Mod like the other test folders
namespace Acom.Tests.Mod;

/// <summary>Craft, craft weapon and UFO rules (their Load needs Mod helpers, still stubs).</summary>
public class RuleCraftTests
{
	private static YamlNodeReader Yaml(string text) => new YamlRootNodeReader(text, "test").ToBase();

	[Fact]
	public void Craft_defaults_match_cpp_constructor()
	{
		var craft = new RuleCraft("STR_CRAFT", 5);
		Assert.Equal("STR_CRAFT", craft.Type);
		Assert.Equal(5, craft.ListOrder);
		Assert.Equal(672, craft.RadarRange);
		Assert.Equal(100, craft.RadarChance);
		Assert.Equal(1696, craft.SightRange);
		Assert.Equal(999999, craft.MaxItems);
		Assert.Equal(99999.0, craft.MaxStorageSpace);
		Assert.Equal(24, craft.TransferTime);
		Assert.Equal("STR_WEAPON_ONE", craft.GetWeaponSlotString(0));
		Assert.Equal("STR_WEAPON_TWO", craft.GetWeaponSlotString(1));
		Assert.Equal("", craft.GetWeaponSlotString(2));
		Assert.Equal("", craft.GetFixedWeaponInSlot(3));
		Assert.Equal(RuleCraft.DEFAULT_CRAFT_DEPLOYMENT_PREVIEW, craft.CustomPreviewType);
		Assert.Equal("STR_VERY_LOW", craft.DefaultDisplayAltitude);
		Assert.True(craft.IsValidWeaponSlot(0, 0));
		Assert.False(craft.IsValidWeaponSlot(0, 1));
		Assert.False(craft.IsWaterOnly);
		Assert.False(craft.IsForNewBattle);
		Assert.False(craft.IsMissile);
		Assert.Equal(-1, craft.SelectSound);
		Assert.Equal(-1, craft.GetSprite(1));
		Assert.False(craft.RequiresBuyBaseFunc.Any());
	}

	[Fact]
	public void Calculate_range_without_refuel_item()
	{
		var craft = new RuleCraft("STR_CRAFT", 0);
		craft.Stats.fuelMax = 1000;
		craft.Stats.speedMax = 2000;
		// fuel ticks = 1000 / (2000 / 100) = 50
		Assert.Equal(49 * 2000 / 12, craft.CalculateRange(1));
		Assert.Equal(99 * 2000 / 12 / 2, craft.CalculateRange(2));
		Assert.Equal(50 * 2000 / 12, craft.CalculateRange(0));

		craft.Stats.speedMax = 99;
		Assert.Equal(-1, craft.CalculateRange(0));
	}

	[Fact]
	public void Craft_stats_load_and_arithmetic()
	{
		var stats = new RuleCraftStats();
		stats.Load(Yaml("{fuelMax: 100, speedMax: 50, soldiers: 8, maxStorageSpace: 2.5, unknown: 1}"));
		Assert.Equal(100, stats.fuelMax);
		Assert.Equal(50, stats.speedMax);
		Assert.Equal(8, stats.soldiers);
		Assert.Equal(2.5, stats.maxStorageSpace);
		Assert.Equal(0, stats.accel);

		var bonus = new RuleCraftStats { fuelMax = 10, armor = 3 };
		var sum = stats + bonus;
		Assert.Equal(110, sum.fuelMax);
		Assert.Equal(3, sum.armor);
		Assert.Equal(100, stats.fuelMax); // operands are not changed

		var neg = -bonus;
		Assert.Equal(-10, neg.fuelMax);
		Assert.Equal(-3, neg.armor);
		Assert.Equal(100, (sum - bonus).fuelMax);

		stats.Add(bonus);
		stats.Add(bonus);
		stats.Subtract(bonus);
		Assert.Equal(110, stats.fuelMax);
	}

	[Fact]
	public void Craft_weapon_defaults()
	{
		var weapon = new RuleCraftWeapon("STR_CANNON");
		Assert.Equal("STR_CANNON", weapon.UfopediaType);
		Assert.Equal(-1, weapon.Sprite);
		Assert.Equal(-1, weapon.Sound);
		Assert.Equal(100, weapon.ShieldDamageModifier);
		Assert.Equal(1, weapon.RearmRate);
		Assert.Equal(CraftWeaponProjectileType.CWPT_CANNON_ROUND, weapon.ProjectileType);
		Assert.Null(weapon.LauncherItem);
		Assert.Equal(0, weapon.BonusStats.fuelMax);
	}

	[Fact]
	public void Ufo_defaults_depend_on_size()
	{
		var ufo = new RuleUfo("STR_PROBE");
		Assert.Equal("STR_VERY_SMALL", ufo.Size);
		Assert.Equal(2, ufo.Radius);
		Assert.Equal(-30, ufo.DefaultVisibility);
		Assert.Equal(0, ufo.BlobSize);
		Assert.Equal(268, ufo.Stats.sightRange);
		Assert.Equal(672, ufo.Stats.radarRange);
		Assert.Equal(1, ufo.MissionScore);
		Assert.Equal(100, ufo.HuntSpeed);
		Assert.Equal(2, ufo.HuntBehavior);
		Assert.Same(ufo.GetRaceBonus(""), ufo.GetRaceBonus("STR_SECTOID"));
		Assert.Single(ufo.RaceBonusRaw);
	}

	[Fact]
	public void Ufo_stats_load_and_add_custom_deployments()
	{
		var stats = new RuleUfoStats();
		stats.Load(Yaml("{damageMax: 50, craftCustomDeploy: STR_A}"));
		Assert.Equal(50, stats.damageMax);
		Assert.Equal("STR_A", stats.craftCustomDeploy);
		Assert.Equal("", stats.missionCustomDeploy);

		var bonus = new RuleUfoStats { damageMax = 5, missionCustomDeploy = "STR_M" };
		var sum = stats + bonus;
		Assert.Equal(55, sum.damageMax);
		Assert.Equal("STR_A", sum.craftCustomDeploy); // empty strings do not replace
		Assert.Equal("STR_M", sum.missionCustomDeploy);
		Assert.Equal("", stats.missionCustomDeploy);
		Assert.Same(stats, stats.Base);
	}
}
