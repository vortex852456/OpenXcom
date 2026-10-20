using Acom.Tests.Engine;

#pragma warning disable CA1716 // namespace mirrors the C++ folder Mod
namespace Acom.Tests.Mod;

public class RuleInventoryTests
{
	private static Dictionary<string, RuleInventory> LoadXcom1()
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

	[Fact]
	public void Loads_sections_slots_and_costs()
	{
		var invs = LoadXcom1();
		var belt = invs["STR_BELT"];
		Assert.Equal(InventoryType.INV_SLOT, belt.Type);
		Assert.Equal((192, 104), (belt.X, belt.Y));
		Assert.Equal([(0, 0), (1, 0), (2, 0), (3, 0), (0, 1), (3, 1)], belt.Slots.Select(s => (s.x, s.y)));
		Assert.Equal(InventoryType.INV_GROUND, invs["STR_GROUND"].Type);
		Assert.True(invs["STR_RIGHT_HAND"].IsRightHand);
		Assert.False(invs["STR_RIGHT_HAND"].IsLeftHand);
		Assert.True(invs["STR_LEFT_HAND"].IsLeftHand);
		Assert.False(belt.IsLeftHand || belt.IsRightHand);

		Assert.Equal(6, belt.GetCost(invs["STR_GROUND"]));
		Assert.Equal(2, invs["STR_RIGHT_HAND"].GetCost(invs["STR_GROUND"]));
		Assert.Equal(0, belt.GetCost(belt));
	}

	[Fact]
	public void Mouse_position_maps_to_slots()
	{
		var invs = LoadXcom1();
		var belt = invs["STR_BELT"];
		int x = 192 + 3 * 16 + 5, y = 104 + 16 + 3;
		Assert.True(belt.CheckSlotInPosition(ref x, ref y));
		Assert.Equal((3, 1), (x, y));
		x = 192 + 16 + 1;
		y = 104 + 16 + 1;
		Assert.False(belt.CheckSlotInPosition(ref x, ref y));
		Assert.Equal((192 + 16 + 1, 104 + 16 + 1), (x, y));

		var hand = invs["STR_RIGHT_HAND"];
		x = 31;
		y = 63 + 47;
		Assert.True(hand.CheckSlotInPosition(ref x, ref y));
		Assert.Equal((0, 0), (x, y));
		x = 32;
		y = 63;
		Assert.False(hand.CheckSlotInPosition(ref x, ref y));

		var ground = invs["STR_GROUND"];
		x = 5 + 16 * 7;
		y = 152 + 20;
		Assert.True(ground.CheckSlotInPosition(ref x, ref y));
		Assert.Equal((7, 1), (x, y));
		x = 5;
		y = 150;
		Assert.False(ground.CheckSlotInPosition(ref x, ref y));
	}

	[Fact]
	public void Items_fit_only_into_complete_slot_areas()
	{
		var invs = LoadXcom1();
		var belt = invs["STR_BELT"];
		Assert.True(belt.FitItemInSlot(1, 1, 3, 1));
		Assert.True(belt.FitItemInSlot(2, 1, 0, 0));
		Assert.False(belt.FitItemInSlot(2, 2, 0, 0));
		Assert.False(belt.FitItemInSlot(1, 2, 1, 0));
		Assert.True(invs["STR_RIGHT_HAND"].FitItemInSlot(2, 3, 9, 9));

		// ground: 20 x 3 slots per page, items may not cross a page border
		var ground = invs["STR_GROUND"];
		Assert.False(ground.FitItemInSlot(2, 3, 19, 0));
		Assert.True(ground.FitItemInSlot(2, 3, 20, 0));
		Assert.False(ground.FitItemInSlot(1, 2, 0, 2));
	}

	[Fact]
	public void Ref_node_values_are_overridden()
	{
		var reader = new YamlRootNodeReader("base: &b\n  x: 10\n  slots: [[0, 0]]\nleft:\n  refNode: *b\n  y: 20\n", "test").ToBase();
		var inv = new RuleInventory("STR_LEFT_HAND", 1);
		inv.Load(reader["left"]);
		Assert.Equal((10, 20), (inv.X, inv.Y));
		Assert.Single(inv.Slots);
		Assert.True(inv.IsLeftHand);
		Assert.Equal(1, inv.ListOrder);
	}
}
