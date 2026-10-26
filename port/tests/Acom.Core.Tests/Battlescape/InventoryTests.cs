using System.Reflection;
using Acom.Tests.Engine;
using static Acom.UnitFaction;

namespace Acom.Tests.Battlescape;

/// <summary>Slot fitting, stacking and ground arrangement logic of the inventory widget (no game needed).</summary>
public class InventoryTests
{
	private static readonly Dictionary<string, RuleInventory> Invs = LoadInventories();
	private static int s_id = 1;

	private static Dictionary<string, RuleInventory> LoadInventories()
	{
		var path = Path.Combine(LanguageTests.RepoRoot(), "bin", "standard", "xcom1", "inventories.rul");
		var result = new Dictionary<string, RuleInventory>();
		int order = 0;
		foreach (var node in YamlRootNodeReader.FromFile(path).ToBase()["invs"].Children())
		{
			var inv = new RuleInventory(node["id"].ReadVal<string>(), order += 100);
			inv.Load(node);
			result.Add(inv.Id, inv);
		}
		return result;
	}

	private static RuleItem Rule(string type, int width, int height, int listOrder = 0, string extra = "")
	{
		var rule = new RuleItem(type, listOrder);
		string yaml = $"type: {type}\ninvWidth: {width}\ninvHeight: {height}\nlistOrder: {listOrder}\n{extra}";
		rule.Load(new YamlRootNodeReader(yaml, "test").ToBase(), new Acom.Mod());
		return rule;
	}

	private static BattleItem Item(RuleItem rule, RuleInventory? slot = null, int x = 0, int y = 0)
	{
		var item = new BattleItem(rule, ref s_id) { Slot = slot, SlotX = x, SlotY = y };
		return item;
	}

	private static BattleUnit Unit(Tile? tile = null)
	{
		var unit = new BattleUnit(FACTION_PLAYER, 1, new Armor("STR_TEST_ARMOR", 0), null, new Unit("STR_TEST"), new BattleUnitStatistics());
		typeof(BattleUnit).GetField("_tile", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(unit, tile);
		return unit;
	}

	[Fact]
	public void Same_plain_items_stack()
	{
		var clip = Rule("STR_CLIP", 1, 1);
		Assert.True(Inventory.CanBeStacked(Item(clip), Item(clip)));
		Assert.False(Inventory.CanBeStacked(Item(clip), null));
		Assert.False(Inventory.CanBeStacked(null, Item(clip)));
		Assert.False(Inventory.CanBeStacked(Item(clip), Item(Rule("STR_OTHER", 1, 1))));
	}

	[Fact]
	public void Primed_or_used_items_do_not_stack()
	{
		var grenade = Rule("STR_GRENADE", 1, 1);
		var primed = Item(grenade);
		primed.FuseTimer = 0;
		Assert.False(Inventory.CanBeStacked(primed, Item(grenade)));

		var medikit = Rule("STR_KIT", 1, 1);
		var used = Item(medikit);
		used.HealQuantity = 3;
		Assert.False(Inventory.CanBeStacked(used, Item(medikit)));

		var withUnit = Item(medikit);
		withUnit.Unit = Unit();
		Assert.False(Inventory.CanBeStacked(withUnit, Item(medikit)));
	}

	[Fact]
	public void Overlap_checks_only_the_same_section()
	{
		var belt = Invs["STR_BELT"];
		var backPack = Invs["STR_BACK_PACK"];
		var unit = Unit();
		var big = Rule("STR_BIG", 2, 2);
		var small = Rule("STR_SMALL", 1, 1);
		unit.Inventory.Add(Item(big, backPack, 1, 0));

		var probe = Item(small);
		Assert.True(Inventory.OverlapItems(unit, probe, backPack, 1, 0));
		Assert.True(Inventory.OverlapItems(unit, probe, backPack, 2, 1));
		Assert.False(Inventory.OverlapItems(unit, probe, backPack, 0, 0));
		Assert.False(Inventory.OverlapItems(unit, probe, backPack, 1, 2));
		Assert.False(Inventory.OverlapItems(unit, probe, belt, 1, 0));
		// a 2x2 item at (0,0) touches the item at (1,0)
		Assert.True(Inventory.OverlapItems(unit, Item(big), backPack, 0, 0));
		// an item never overlaps itself
		Assert.False(Inventory.OverlapItems(unit, unit.Inventory[0], backPack, 1, 0));
	}

	[Fact]
	public void Overlap_on_the_ground_uses_the_tile_inventory()
	{
		var ground = Invs["STR_GROUND"];
		var tile = new Tile(new Position(1, 2, 0), null);
		var small = Rule("STR_SMALL", 1, 1);
		tile.Inventory.Add(Item(small, ground, 3, 1));
		var unit = Unit(tile);
		Assert.True(Inventory.OverlapItems(unit, Item(small), ground, 3, 1));
		Assert.False(Inventory.OverlapItems(unit, Item(small), ground, 4, 1));
		Assert.False(Inventory.OverlapItems(Unit(), Item(small), ground, 3, 1)); // no tile
	}

	private static (int xMax, Dictionary<(int, int), int> stacks) Arrange(List<BattleItem> ground, Func<BattleItem, bool>? search = null)
	{
		var groundRule = Invs["STR_GROUND"];
		int slotsX = (Screen.ORIGINAL_WIDTH - groundRule.X) / RuleInventory.SLOT_W;
		int slotsY = (Screen.ORIGINAL_HEIGHT - groundRule.Y) / RuleInventory.SLOT_H;
		var occupied = new List<List<bool>>();
		for (int i = 0; i < slotsY; ++i)
			occupied.Add(new List<bool>(new bool[slotsX * 2]));
		var stacks = new Dictionary<(int, int), int>();
		int xMax = Inventory.ArrangeGroundItems(ground, groundRule, slotsX, slotsY, occupied, search ?? (_ => true),
			(x, y) => stacks[(x, y)] = stacks.GetValueOrDefault((x, y)) + 1);
		return (xMax, stacks);
	}

	[Fact]
	public void Ground_items_are_grouped_and_placed_by_list_order()
	{
		Assert.Equal((20, 3), ((Screen.ORIGINAL_WIDTH - Invs["STR_GROUND"].X) / RuleInventory.SLOT_W, (Screen.ORIGINAL_HEIGHT - Invs["STR_GROUND"].Y) / RuleInventory.SLOT_H));
		var rifle = Rule("STR_RIFLE", 2, 3, listOrder: 20);
		var clip = Rule("STR_CLIP", 1, 1, listOrder: 10);
		var hidden = Rule("STR_HIDDEN", 0, 0, listOrder: 0);
		var ground = new List<BattleItem> { Item(rifle), Item(clip), Item(hidden), Item(clip), Item(rifle) };
		var (xMax, stacks) = Arrange(ground);

		// clips first (lower list order), both in one stack at (0,0)
		Assert.Equal((0, 0), (ground[1].SlotX, ground[1].SlotY));
		Assert.Equal((0, 0), (ground[3].SlotX, ground[3].SlotY));
		// rifles stack too and go right of the clips (3 rows high: no room below)
		Assert.Equal((1, 0), (ground[0].SlotX, ground[0].SlotY));
		Assert.Equal((1, 0), (ground[4].SlotX, ground[4].SlotY));
		// invisible items stay out of the way
		Assert.Equal((1000000, 0), (ground[2].SlotX, ground[2].SlotY));
		Assert.All(ground, i => Assert.Same(Invs["STR_GROUND"], i.Slot));
		Assert.Equal(3, xMax);
		Assert.Equal(new Dictionary<(int, int), int> { [(0, 0)] = 2, [(1, 0)] = 2 }, stacks);
	}

	[Fact]
	public void Ground_items_fill_columns_top_down_and_do_not_wrap_screens()
	{
		var small = Rule("STR_SMALL", 1, 1);
		var wide = Rule("STR_WIDE", 2, 1, listOrder: 1);
		var ground = new List<BattleItem>();
		// 4 different small stacks (primed grenades do not stack)
		for (int i = 0; i < 4; ++i)
		{
			var item = Item(small);
			item.FuseTimer = i;
			ground.Add(item);
		}
		ground.Add(Item(wide));
		var (xMax, _) = Arrange(ground);
		Assert.Equal([(0, 0), (0, 1), (0, 2), (1, 0)], ground.Take(4).Select(i => (i.SlotX, i.SlotY)));
		Assert.Equal((1, 1), (ground[4].SlotX, ground[4].SlotY));
		Assert.Equal(3, xMax);

		// a 2-wide item may not straddle two ground pages (x 19 -> 20)
		ground.Clear();
		var tall = Rule("STR_TALL", 1, 3);
		for (int i = 0; i < 19; ++i)
		{
			var item = Item(tall);
			item.FuseTimer = i;
			ground.Add(item);
		}
		var wide3 = Rule("STR_WIDE3", 2, 3, listOrder: 1);
		ground.Add(Item(wide3));
		(xMax, _) = Arrange(ground);
		Assert.Equal(18, ground[18].SlotX);
		Assert.Equal((20, 0), (ground[19].SlotX, ground[19].SlotY));
		Assert.Equal(22, xMax);
	}

	[Fact]
	public void Ground_items_not_matching_the_search_are_skipped()
	{
		var a = Rule("STR_A", 1, 1, listOrder: 1);
		var b = Rule("STR_B", 1, 1, listOrder: 2);
		var ground = new List<BattleItem> { Item(a), Item(b) };
		var (xMax, stacks) = Arrange(ground, i => i.Rules == b);
		Assert.Equal(1000000, ground[0].SlotX);
		Assert.Equal((0, 0), (ground[1].SlotX, ground[1].SlotY));
		Assert.Equal(1, xMax);
		Assert.Single(stacks);
	}
}
