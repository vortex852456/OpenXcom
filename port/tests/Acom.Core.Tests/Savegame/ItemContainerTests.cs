#nullable disable // null rule pointers as in the C++ code
using static Acom.Tests.Savegame.SavegameTestHelpers;

namespace Acom.Tests.Savegame;

public class ItemContainerTests
{
	[Fact]
	public void Add_remove_and_totals_follow_cpp_map_semantics()
	{
		var a = Uninitialized<RuleItem>();
		var b = Uninitialized<RuleItem>();
		var c = new ItemContainer();
		Assert.True(c.Empty());

		c.AddItem(a);
		c.AddItem(a, 4);
		c.AddItem(b, 2);
		c.AddItem(null, 7); // ignored
		Assert.Equal(5, c.GetItem(a));
		Assert.Equal(2, c.GetItem(b));
		Assert.Equal(0, c.GetItem((RuleItem)null));
		Assert.Equal(7, c.TotalQuantity);

		c.RemoveItem(a, 3);
		Assert.Equal(2, c.GetItem(a));

		// removing all (or more) erases the entry
		c.RemoveItem(b, 2);
		Assert.False(c.Contents.ContainsKey(b));
		c.RemoveItem(a, 10);
		Assert.False(c.Contents.ContainsKey(a));
		Assert.True(c.Empty());

		// removing something absent is a no-op
		c.RemoveItem(a, 1);
		c.RemoveItem((RuleItem)null, 1);
		Assert.Equal(0, c.TotalQuantity);
	}

	[Fact]
	public void Zero_and_negative_amounts_behave_like_cpp()
	{
		var a = Uninitialized<RuleItem>();
		var c = new ItemContainer();

		// operator[] creates the entry even for 0
		c.AddItem(a, 0);
		Assert.False(c.Empty());
		Assert.Equal(0, c.GetItem(a));

		c.AddItem(a, -3);
		Assert.Equal(-3, c.GetItem(a));
		// qty < current: -5 < -3, so the entry is kept with -3 - (-5)
		c.RemoveItem(a, -5);
		Assert.Equal(2, c.GetItem(a));

		c.Clear();
		Assert.True(c.Empty());
	}

	[Fact]
	public void Load_replaces_contents_and_skips_unknown_items()
	{
		var a = Uninitialized<RuleItem>();
		var b = Uninitialized<RuleItem>();
		var rules = new Dictionary<string, RuleItem> { ["STR_A"] = a, ["STR_B"] = b };
		var c = new ItemContainer();
		c.AddItem(b, 9);

		c.Load(Yaml("items:\n  STR_A: 3\n  STR_B: 5\n  STR_UNKNOWN: 1\n")["items"], name => rules.GetValueOrDefault(name));

		Assert.Equal(3, c.GetItem(a));
		Assert.Equal(5, c.GetItem(b));
		Assert.Equal(8, c.TotalQuantity);
		Assert.Equal(2, c.Contents.Count);
	}

	[Fact]
	public void Load_of_missing_or_non_map_node_keeps_contents()
	{
		var a = Uninitialized<RuleItem>();
		var c = new ItemContainer();
		c.AddItem(a, 4);

		c.Load(Yaml("other: 1\n")["items"], _ => a);
		c.Load(Yaml("items: [1, 2]\n")["items"], _ => a);

		Assert.Equal(4, c.GetItem(a));
	}

	[Fact]
	public void Yaml_roundtrip_is_sorted_by_item_type()
	{
		var rifle = OrSkip(() => new RuleItem("STR_RIFLE", 0));
		var ammo = OrSkip(() => new RuleItem("STR_AMMO", 1));
		var rules = new Dictionary<string, RuleItem> { ["STR_RIFLE"] = rifle, ["STR_AMMO"] = ammo };
		var c = new ItemContainer();
		c.AddItem(rifle, 2);
		c.AddItem(ammo, 6);

		var writer = new YamlRootNodeWriter();
		c.Save(writer["items"]);
		var saved = writer.ToReader()["items"];
		Assert.Equal(["STR_AMMO", "STR_RIFLE"], Keys(saved));

		var loaded = new ItemContainer();
		loaded.Load(saved, name => rules.GetValueOrDefault(name));
		Assert.Equal(2, loaded.GetItem("STR_RIFLE"));
		Assert.Equal(6, loaded.GetItem(ammo));
	}
}
