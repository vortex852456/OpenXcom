using static Acom.Tests.Savegame.BattleUnitStatisticsTests;

namespace Acom.Tests.Savegame;

public class EquipmentLayoutItemTests
{
	// RuleItem/RuleInventory are ported separately; until then the test is skipped.
	[Fact(SkipExceptions = [typeof(NotImplementedException)])]
	public void Save_writes_cpp_keys_and_ammo_slots()
	{
		var rifle = new RuleItem("STR_RIFLE", 0);
		var clip = new RuleItem("STR_RIFLE_CLIP", 1);
		var grenade = new RuleItem("STR_GRENADE", 2);
		var hand = new RuleInventory("STR_RIGHT_HAND", 0);
		var belt = new RuleInventory("STR_BELT", 1);

		var writer = new YamlRootNodeWriter();
		new EquipmentLayoutItem(rifle, hand, 0, 0, [clip, null, clip, null], -1, false).Save(writer);
		var back = writer.ToReader();
		Assert.Equal(["itemType", "slot", "ammoItem", "ammoItemSlots"], Keys(back));
		Assert.Equal(["STR_RIFLE_CLIP", "NONE", "STR_RIFLE_CLIP"], back["ammoItemSlots"].Children().Select(c => c.Val));
		Assert.StartsWith("{", writer.Emit().Trim(), StringComparison.Ordinal); // flow style

		writer = new YamlRootNodeWriter();
		new EquipmentLayoutItem(rifle, belt, 2, 1, [null, clip], 3, true).Save(writer);
		back = writer.ToReader();
		Assert.Equal(["itemType", "slot", "slotX", "slotY", "ammoItemSlots", "fuseTimer", "fixed"], Keys(back));
		Assert.Equal(["NONE", "STR_RIFLE_CLIP"], back["ammoItemSlots"].Children().Select(c => c.Val));

		writer = new YamlRootNodeWriter();
		new EquipmentLayoutItem(grenade, belt, 0, 0, [], 0, false).Save(writer);
		back = writer.ToReader();
		Assert.Equal(["itemType", "slot", "fuseTimer"], Keys(back));
		Assert.Equal("0", back["fuseTimer"].Val);
	}
}
