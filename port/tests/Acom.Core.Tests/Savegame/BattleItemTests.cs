using static Acom.Tests.Savegame.SavegameTestHelpers;

namespace Acom.Tests.Savegame;

/// <summary>BattleItem logic that does not need the item rules (RuleItem is still a stub).</summary>
public class BattleItemTests
{
	private static BattleItem NewItem()
	{
		int id = 7;
		return new BattleItem(null, ref id);
	}

	[Fact]
	public void Constructor_takes_and_advances_the_id_counter()
	{
		int id = 5;
		var item = new BattleItem(null, ref id);
		Assert.Equal(5, item.Id);
		Assert.Equal(6, id);
		Assert.Equal(-1, item.FuseTimer); // unprimed
		Assert.False(item.IsFuseEnabled);
		Assert.False(item.IsWeaponWithAmmo);
	}

	[Fact]
	public void Spend_bullet_keeps_the_ammo_if_a_shot_costs_more()
	{
		var item = NewItem();
		item.AmmoQuantity = 5;
		Assert.True(item.SpendBullet(2));
		Assert.True(item.SpendBullet(2));
		Assert.True(item.SpendBullet(2)); // 1 left, not enough for the shot: nothing spent
		Assert.False(item.SpendBullet(1)); // now empty
		Assert.False(item.SpendBullet(1));
	}

	[Fact]
	public void Medikit_uses()
	{
		var item = NewItem();
		item.HealQuantity = 3;
		item.PainKillerQuantity = 2;
		item.StimulantQuantity = 1;
		item.SpendHealingItemUse(BattleMediKitAction.BMA_HEAL);
		item.SpendHealingItemUse(BattleMediKitAction.BMA_PAINKILLER);
		item.SpendHealingItemUse(BattleMediKitAction.BMA_STIMULANT);
		Assert.Equal((2, 1, 0), (item.HealQuantity, item.PainKillerQuantity, item.StimulantQuantity));
	}

	[Fact]
	public void Fuse_can_only_be_enabled_when_primed()
	{
		var item = NewItem();
		item.SetFuseEnabled(true);
		Assert.False(item.IsFuseEnabled);
	}

	[Fact]
	public void Item_without_action_configs_needs_and_has_no_ammo()
	{
		var item = NewItem();
		foreach (var action in new[] { BattleActionType.BA_AIMEDSHOT, BattleActionType.BA_LAUNCH, BattleActionType.BA_SNAPSHOT, BattleActionType.BA_AUTOSHOT, BattleActionType.BA_HIT, BattleActionType.BA_THROW })
		{
			Assert.Null(item.GetActionConf(action));
			Assert.False(item.NeedsAmmoForAction(action));
			Assert.Null(item.GetAmmoForAction(action));
			Assert.Null(item.GetAmmoForActionConst(action));
			Assert.False(item.HaveNextShotsForAction(action, 0));
			string message = "unchanged";
			int spend = 42;
			Assert.Null(item.GetAmmoForAction(action, ref message, ref spend));
			Assert.Equal(("unchanged", 42), (message, spend)); // no config: nothing is reported
		}
		for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
		{
			Assert.False(item.NeedsAmmoForSlot(slot));
			Assert.Null(item.SetAmmoForSlot(slot, NewItem())); // weapon without ammo slots ignores ammo
			Assert.Null(item.GetAmmoForSlot(slot));
			Assert.False(item.IsAmmoVisibleForSlot(slot));
		}
		Assert.False(item.HaveAllAmmo());
		Assert.True(item.HaveAnyAmmo()); // not a weapon that uses ammo
	}

	[Fact]
	public void Owner_and_special_weapon()
	{
		var item = NewItem();
		var first = Uninitialized<BattleUnit>();
		var second = Uninitialized<BattleUnit>();
		Assert.False(item.IsSpecialWeapon);
		item.Owner = first;
		Assert.True(item.IsSpecialWeapon); // owned but in no inventory slot
		item.Owner = second;
		Assert.Same(first, item.PreviousOwner);
		Assert.Same(second, item.Owner);
		item.Slot = new RuleInventory("STR_RIGHT_HAND", 0);
		Assert.False(item.IsSpecialWeapon);
	}

	[Fact]
	public void Occupies_slot_needs_a_slot_and_ignores_itself()
	{
		var item = NewItem();
		Assert.False(item.OccupiesSlot(0, 0));
		var hand = new RuleInventory("STR_RIGHT_HAND", 0);
		hand.Load(Yaml("{id: STR_RIGHT_HAND, type: 1}"));
		item.Slot = hand;
		Assert.True(item.OccupiesSlot(5, 5)); // hands are a single slot
		Assert.False(item.OccupiesSlot(5, 5, item));
	}

	[Fact]
	public void Move_to_owner_takes_the_item_from_its_tile()
	{
		var tile = new Tile(new Position(1, 1, 0), null);
		var item = NewItem();
		tile.AddItem(item, null);
		item.MoveToOwner(null); // same (no) owner: only leaves the tile
		Assert.Null(item.Tile);
		Assert.Empty(tile.Inventory);
	}
}
