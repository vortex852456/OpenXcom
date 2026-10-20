#pragma warning disable CA1716 // namespace mirrors the src/Mod area like the other test folders
namespace Acom.Tests.Mod;

public class RuleItemTests
{
	private static YamlNodeReader Node(string yaml) => new YamlRootNodeReader(yaml, "test").ToBase();

	/// <summary>Runs code that needs units that may still be stubs (Mod) and skips the test until they are ported.</summary>
	internal static T NeedsPorted<T>(Func<T> code, string what)
	{
		try
		{
			return code();
		}
		catch (NotImplementedException e)
		{
			Assert.Skip($"needs ported {what}: {e.TargetSite?.DeclaringType?.Name}.{e.TargetSite?.Name}");
			throw;
		}
	}

	private static RuleItem NewItem(string type, int listOrder = 0) => new(type, listOrder);

	[Fact]
	public void Use_cost_rule_reads_tu_and_cost_map()
	{
		var rule = new RuleItemUseCostRule(0, null);
		rule.LoadCost(Node("tuSnap: 33\ncostSnap: {energy: 5, stun: ~}\ntuAuto: 99"), "Snap");
		Assert.Equal((short?)33, rule.Time);
		Assert.Equal((short?)5, rule.Energy);
		Assert.Null(rule.Stun);
		Assert.Null(rule.Morale);

		var cost = new RuleItemUseCostRule(25);
		cost.LoadCost(Node("costUse: {time: 10, mana: 3}"), "Use");
		Assert.Equal((short?)10, cost.Time);
		Assert.Equal((short?)3, cost.Mana);
		Assert.Equal((short?)0, cost.Health);
	}

	[Fact]
	public void Use_flat_rule_reads_scalar_or_map()
	{
		var flat = new RuleItemUseFlatRule(null, null);
		flat.LoadFlat(Node("flatAimed: true"), "Aimed");
		Assert.True(flat.Time);
		Assert.Null(flat.Energy);

		var map = new RuleItemUseFlatRule(null, null);
		map.LoadFlat(Node("flatAuto: {time: false, energy: true}"), "Auto");
		Assert.False(map.Time);
		Assert.True(map.Energy);
		Assert.Null(map.Stun);
	}

	[Fact]
	public void GetDefault_takes_each_value_from_the_first_rule_that_defines_it()
	{
		var snap = new RuleItemUseCostRule(0, null) { Energy = 7 };
		var aimed = new RuleItemUseCostRule(40, 3);
		var cost = snap.GetDefault(aimed);
		Assert.Equal(0, cost.Time);
		Assert.Equal(7, cost.Energy);
		Assert.Equal(3, cost.Morale);
		Assert.Equal(3, cost.Mana);

		var none = new RuleItemUseCostRule(null, null).GetDefault();
		Assert.Equal(0, none.Time);
		Assert.Equal(0, none.Stun);

		var flat = new RuleItemUseFlatRule(null, null).GetDefault(new RuleItemUseFlatRule(null, null), new RuleItemUseFlatRule(false, true));
		Assert.False(flat.Time);
		Assert.True(flat.Energy);
		Assert.True(flat.Mana);
	}

	[Fact]
	public void Use_cost_addition_wraps_like_Sint16()
	{
		var cost = new RuleItemUseCost(32767, 1);
		cost += new RuleItemUseCost(1, 2);
		Assert.Equal(-32768, cost.Time);
		Assert.Equal(3, cost.Energy);
		Assert.Equal(3, cost.Mana);
		var copy = new RuleItemUseCost(cost);
		copy.Time = 5;
		Assert.Equal(-32768, cost.Time);
	}

	[Theory]
	[InlineData("ammoSlot: 3", 3)]
	[InlineData("ammoSlot: -1", -1)]
	[InlineData("ammoSlot: 4", 2)]
	[InlineData("ammoSlot: -2", 2)]
	[InlineData("other: 1", 2)]
	public void LoadAmmoSlotChecked_ignores_values_out_of_range(string yaml, int expected)
	{
		int slot = 2;
		RuleItem.LoadAmmoSlotChecked(ref slot, Node(yaml)["ammoSlot"], "TEST");
		Assert.Equal(expected, slot);
	}

	[Fact]
	public void Fuse_trigger_defaults_to_default_behavior()
	{
		var fuse = new RuleItemFuseTrigger();
		Assert.True(fuse.defaultBehavior);
		Assert.False(fuse.throwTrigger || fuse.throwExplode || fuse.proximityTrigger || fuse.proximityExplode);
	}

	[Fact]
	public void Constructor_sets_the_cpp_defaults()
	{
		var item = NewItem("TEST_ITEM", 7);
		Assert.Equal("TEST_ITEM", item.Name);
		Assert.Equal("TEST_ITEM", item.UfopediaType);
		Assert.Equal(7, item.ListOrder);
		Assert.Equal(24, item.TransferTime);
		Assert.Equal(3, item.Weight);
		Assert.Equal(-1, item.BigSprite);
		Assert.Equal(120, item.HandSprite);
		Assert.Equal(20, item.AccuracyPanic);
		Assert.Equal(100, item.AccuracyThrow);
		Assert.Equal(200, item.AimRange);
		Assert.Equal(15, item.SnapRange);
		Assert.Equal(7, item.AutoRange);
		Assert.Equal(3, item.ConfigAuto.shots);
		Assert.Equal("STR_AIMED_SHOT", item.ConfigAimed.name);
		Assert.Equal(15, item.GetTULoad(3));
		Assert.Equal(8, item.GetTUUnload(0));
		Assert.Equal(25, item.CostUse.Time);
		Assert.Equal(25, item.CostMind.Time); // falls back to costUse
		Assert.Equal(50, item.CostPrime.Time);
		Assert.Equal(0, item.CostAimed.Time);
		Assert.False(item.FlatUse.Time);
		Assert.True(item.FlatAimed.Energy); // falls back to flatUse
		Assert.Equal(-1, item.FuseTimerDefault);
		Assert.Equal([Acom.Mod.NO_SURFACE], item.CustomItemPreviewIndex);
		Assert.Equal(100, item.ZombieUnitChance); // null falls back to specialChance
		Assert.True(item.IsTargetAllowed(UnitFaction.FACTION_PLAYER, UnitFaction.FACTION_HOSTILE));
		Assert.False(item.CanBeEquippedToCraftInventory());
		Assert.Equal(-1, item.GetAIUseDelay());
		Assert.True(item.AllowTargetGround);
		Assert.Equal(8, item.HandSpriteOffX);
		Assert.Equal(16, item.HandSpriteOffY);
		Assert.False(item.IsOutOfRange(200 * 200));
		Assert.True(item.IsOutOfRange(200 * 200 + 1));
	}

	[Fact]
	public void Load_reads_weapon_settings()
	{
		var mod = NeedsPorted(() => new Acom.Mod(), "Mod");
		var item = NewItem("TEST_WEAPON");
		const string yaml = """
			type: TEST_WEAPON
			name: STR_TEST
			battleType: 1
			twoHanded: true
			clipSize: -1
			accuracySnap: 60
			tuSnap: 25
			costAuto: {energy: 4}
			flatRate: true
			confAuto: {shots: 5, ammoSlot: 1, ammoSpawnUnitChanceOverride: ~}
			ammo:
			  1: {tuLoad: 10}
			fuseTriggerEvents: {throwTrigger: true}
			inventoryMoveCost: {basePercent: 50}
			ai: {useDelay: 3}
			""";
		NeedsPorted(() => { item.Load(Node(yaml), mod); return 0; }, "Mod helpers");
		Assert.Equal("STR_TEST", item.Name);
		Assert.Equal(BattleType.BT_FIREARM, item.BattleType);
		Assert.True(item.IsRifle);
		Assert.True(item.IsMeleeTypeSet);
		Assert.False(item.IsDamageTypeSet);
		Assert.True(item.CanBeEquippedToCraftInventory());
		Assert.Equal(RuleItem.AmmoSlotSelfUse, item.ConfigMelee.ammoSlot);
		Assert.Equal(60, item.AccuracySnap);
		Assert.Equal(25, item.CostSnap.Time);
		Assert.Equal(4, item.CostAuto.Energy);
		Assert.Equal(0, item.CostAuto.Time);
		Assert.True(item.FlatAuto.Time);
		Assert.Equal(5, item.ConfigAuto.shots);
		Assert.Equal(1, item.ConfigAuto.ammoSlot);
		Assert.Equal(LoadYaml.defIntNullable, item.ConfigAuto.ammoSpawnUnitChanceOverride);
		Assert.Equal(10, item.GetTULoad(1));
		Assert.Equal(15, item.GetTULoad(0));
		Assert.True(item.FuseTriggerEvent.throwTrigger);
		Assert.True(item.FuseTriggerEvent.defaultBehavior);
		Assert.Equal(50, item.InventoryMoveCostPercent);
		Assert.Equal(3, item.GetAIUseDelay(mod));
	}

	[Fact]
	public void Load_xcom1_items_without_sprites()
	{
		var mod = NeedsPorted(() => new Acom.Mod(), "Mod");
		var items = new YamlRootNodeReader(File.ReadAllText(Path.Combine(Engine.LanguageTests.RepoRoot(), "bin", "standard", "xcom1", "items.rul")), "items.rul")["items"].Children();
		RuleItem LoadXcom1(string type)
		{
			var node = items.First(n => n["type"].ReadVal("") == type);
			var item = NewItem(type);
			NeedsPorted(() => { item.Load(node, mod); return 0; }, "Mod helpers");
			return item;
		}

		var missiles = LoadXcom1("STR_STINGRAY_MISSILES");
		Assert.Equal(0.4, missiles.Size);
		Assert.Equal(3000, missiles.BuyCost);
		Assert.Equal(2400, missiles.SellCost);
		Assert.Equal(48, missiles.TransferTime);
		Assert.Equal(1, missiles.ClipSize);
		Assert.Equal(BattleType.BT_NONE, missiles.BattleType);

		var corpse = LoadXcom1("STR_CYBERDISC_CORPSE");
		Assert.Equal(BattleType.BT_CORPSE, corpse.BattleType);
		Assert.True(corpse.IsDamageTypeSet);
		Assert.Equal(6, corpse.DamageType.FixRadius);
		Assert.Equal(120, corpse.Power);
		Assert.False(corpse.CanBeEquippedToCraftInventory());

		var food = LoadXcom1("STR_ALIEN_FOOD");
		Assert.Equal(5, food.SpecialType);
		Assert.Equal(2, food.RecoveryPoints);

		var alien = LoadXcom1("STR_SECTOID_SOLDIER");
		Assert.True(alien.IsAlien);
		Assert.True(alien.IsRecoverable);
		Assert.Equal(0.0, alien.Size);
	}
}
