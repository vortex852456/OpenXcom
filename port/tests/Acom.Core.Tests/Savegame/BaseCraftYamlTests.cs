using static Acom.Tests.Savegame.SavegameTestHelpers;

namespace Acom.Tests.Savegame;

/// <summary>
/// YAML load/save of the base-related Savegame units: key names and order as in the C++ save format.
/// Round trips that need a rule's type name skip themselves while that rule is still a stub.
/// </summary>
public class BaseCraftYamlTests
{
	private static YamlNodeReader SaveToReader(System.Action<YamlNodeWriter> save)
	{
		var writer = new YamlRootNodeWriter();
		save(writer);
		return writer.ToReader();
	}

	[Fact]
	public void Transfer_of_personnel_roundtrips()
	{
		var t = new Transfer(0);
		Assert.True(t.Load(Yaml("hours: 12\nscientists: 3\ndelivered: true\n"), null, null, null));
		Assert.Equal(12, t.Hours);
		Assert.Equal(3, t.Quantity);
		Assert.Equal(TransferType.TRANSFER_SCIENTIST, t.Type);
		Assert.True(t.Delivered);

		var saved = SaveToReader(w => t.Save(w, null, null));
		Assert.Equal(["hours", "scientists", "delivered"], Keys(saved));
		Assert.Equal(3, saved["scientists"].ReadVal<int>());

		var engineers = new Transfer(5);
		engineers.SetEngineers(2);
		saved = SaveToReader(w => engineers.Save(w, null, null));
		Assert.Equal(["hours", "engineers"], Keys(saved));
		var loaded = new Transfer(saved["hours"].ReadVal<int>());
		Assert.True(loaded.Load(saved, null, null, null));
		Assert.Equal(TransferType.TRANSFER_ENGINEER, loaded.Type);
		Assert.Equal(2, loaded.Quantity);
		Assert.Equal(5, loaded.Hours);
		Assert.False(loaded.Delivered);
	}

	[Fact]
	public void Transfer_of_items_roundtrips()
	{
		var rule = OrSkip(() => new RuleItem("STR_PISTOL", 0));
		var t = new Transfer(24);
		t.SetItems(rule, 4);
		Assert.Equal(TransferType.TRANSFER_ITEM, t.Type);

		var saved = SaveToReader(w => t.Save(w, null, null));
		Assert.Equal(["hours", "itemId", "itemQty"], Keys(saved));
		Assert.Equal("STR_PISTOL", saved["itemId"].ReadVal<string>());
	}

	[Fact]
	public void BaseFacility_load_reads_all_fields_and_build_clears_upgrade_flag()
	{
		var f = new BaseFacility(null, null);
		Assert.Equal(-1, f.X);
		Assert.Equal(-1, f.Y);

		f.Load(Yaml("type: STR_HANGAR\nx: 2\ny: 3\nbuildTime: 2\nammo: 7\nammoMissingReported: true\ndisabled: true\nhadPreviousFacility: true\n"));
		Assert.Equal(2, f.X);
		Assert.Equal(3, f.Y);
		Assert.Equal(2, f.BuildTime);
		Assert.Equal(7, f.Ammo);
		Assert.True(f.Disabled);
		Assert.True(f.IfHadPreviousFacility);
		Assert.Equal(0, f.AdjustedBuildTime);
		Assert.True(f.IsBuiltOrHadPreviousFacility);

		f.Build();
		Assert.Equal(1, f.BuildTime);
		Assert.True(f.IfHadPreviousFacility);
		f.Build();
		Assert.Equal(0, f.BuildTime);
		Assert.False(f.IfHadPreviousFacility);
		Assert.True(f.IsBuiltOrHadPreviousFacility);
	}

	[Fact]
	public void BaseFacility_roundtrip_writes_defaults_only_when_set()
	{
		var rule = OrSkip(() => new RuleBaseFacility("STR_LIFT", 0));
		var f = new BaseFacility(rule, null) { X = 1, Y = 4 };
		var saved = SaveToReader(f.Save);
		Assert.Equal(["type", "x", "y"], Keys(saved));

		f.Load(Yaml("buildTime: 3\nammo: 2\nammoMissingReported: true\ndisabled: true\nhadPreviousFacility: true\n"));
		saved = SaveToReader(f.Save);
		Assert.Equal(["type", "x", "y", "buildTime", "ammo", "ammoMissingReported", "disabled", "hadPreviousFacility"], Keys(saved));

		var loaded = new BaseFacility(rule, null);
		loaded.Load(saved);
		Assert.Equal((1, 4, 3, 2), (loaded.X, loaded.Y, loaded.BuildTime, loaded.Ammo));
	}

	[Fact]
	public void CraftWeapon_load_and_rearming_flag()
	{
		var w = new CraftWeapon(null, 5);
		Assert.Equal(5, w.Ammo);
		w.Load(Yaml("type: STR_CANNON\nammo: 40\nrearming: true\n"));
		Assert.Equal(40, w.Ammo);
		Assert.True(w.IsRearming);
		w.SetDisabled(true);
		Assert.False(w.IsRearming); // disabled weapons never rearm
	}

	[Fact]
	public void CraftWeapon_roundtrip()
	{
		var rule = OrSkip(() => new RuleCraftWeapon("STR_CANNON_UC"));
		var w = new CraftWeapon(rule, 0);
		w.Load(Yaml("ammo: 0\nrearming: true\ndisabled: true\n"));
		var saved = SaveToReader(w.Save);
		Assert.Equal(["type", "ammo", "rearming", "disabled"], Keys(saved));
		Assert.Equal("STR_CANNON_UC", saved["type"].ReadVal<string>());
	}

	[Fact]
	public void Vehicle_load_keeps_unlimited_ammo()
	{
		var v = new Vehicle(null, 10, 1);
		v.Load(Yaml("type: STR_TANK_CANNON\nammo: -1\nsize: 4\n"));
		Assert.Equal(255, v.Ammo); // -1 = unlimited
		v.Ammo = 3;
		Assert.Equal(255, v.Ammo);
		Assert.Equal(4, v.TotalSize);

		var limited = new Vehicle(null, 10, 1);
		limited.Ammo = 3;
		Assert.Equal(3, limited.Ammo);
	}

	[Fact]
	public void Vehicle_roundtrip()
	{
		var rule = OrSkip(() => new RuleItem("STR_TANK_CANNON", 0));
		var saved = SaveToReader(new Vehicle(rule, 30, 4).Save);
		Assert.Equal(["type", "ammo", "size"], Keys(saved));
		var loaded = new Vehicle(rule, 0, 1);
		loaded.Load(saved);
		Assert.Equal(30, loaded.Ammo);
		Assert.Equal(4, loaded.TotalSize);
	}

	[Fact]
	public void ResearchProject_load_and_step()
	{
		var r = new ResearchProject(null);
		r.Load(Yaml("project: STR_LASER_WEAPONS\nassigned: 10\nspent: 50\ncost: 100\n"));
		Assert.Equal(10, r.Assigned);
		Assert.Equal(50, r.Spent);
		Assert.Equal(100, r.Cost);
		Assert.False(r.Step());
		Assert.Equal(60, r.Spent);
		for (int i = 0; i < 3; ++i)
			Assert.False(r.Step());
		Assert.True(r.Step());
		Assert.True(r.IsFinished);
	}

	[Fact]
	public void ResearchProject_roundtrip()
	{
		var rule = OrSkip(() => new RuleResearch("STR_LASER_WEAPONS", 0));
		var r = new ResearchProject(rule, 120) { Assigned = 7, Spent = 33 };
		var saved = SaveToReader(r.Save);
		Assert.Equal(["project", "assigned", "spent", "cost"], Keys(saved));
		var loaded = new ResearchProject(rule);
		loaded.Load(saved);
		Assert.Equal((7, 33, 120), (loaded.Assigned, loaded.Spent, loaded.Cost));
	}

	[Fact]
	public void Production_roundtrip_and_backwards_compatible_infinite_amount()
	{
		var rule = OrSkip(() => new RuleManufacture("STR_LASER_RIFLE", 0));
		var p = new Production(rule, 0);
		p.Load(Yaml("item: STR_LASER_RIFLE\nassigned: 4\nspent: 90\namount: 2147483647\n"));
		Assert.Equal(999, p.AmountTotal);
		Assert.True(p.InfiniteAmount);
		Assert.True(p.SellItems);
		Assert.False(p.IsQueuedOnly);

		var saved = SaveToReader(p.Save);
		Assert.Equal(["item", "assigned", "spent", "amount", "infinite", "sell"], Keys(saved));
		var loaded = new Production(rule, 0);
		loaded.Load(saved);
		Assert.Equal((4, 90, 999, true, true), (loaded.AssignedEngineers, loaded.TimeSpent, loaded.AmountTotal, loaded.InfiniteAmount, loaded.SellItems));
	}

	[Fact]
	public void Production_is_queued_only_without_progress_and_engineers()
	{
		var p = new Production(null, 3);
		Assert.True(p.IsQueuedOnly);
		p.AssignedEngineers = 1;
		Assert.False(p.IsQueuedOnly);
		p.AssignedEngineers = 0;
		p.TimeSpent = 1;
		Assert.False(p.IsQueuedOnly);
	}
}
