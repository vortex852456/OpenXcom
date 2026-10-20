#pragma warning disable CA1716 // namespace mirrors src/Mod like the other test folders
namespace Acom.Tests.Mod;

public class RuleManufactureTests
{
	[Fact]
	public void Load_with_refNode_and_defaults()
	{
		const string yaml = """
			base: &base
			  startFrom: STR_LASER_RIFLE
			  breakDownItems: [STR_LASER_PISTOL, STR_ALLOY]
			  breakDownRequiresBaseFunc: false
			shortcut:
			  refNode: *base
			  breakDownRequires: true
			""";
		var shortcut = new RuleManufactureShortcut("STR_SHORTCUT");
		shortcut.Load(new YamlRootNodeReader(yaml, "test")["shortcut"]);

		Assert.Equal("STR_SHORTCUT", shortcut.Name);
		Assert.Equal("STR_LASER_RIFLE", shortcut.StartFrom);
		Assert.Equal(["STR_LASER_PISTOL", "STR_ALLOY"], shortcut.BreakDownItems);
		Assert.True(shortcut.BreakDownRequires);
		Assert.False(shortcut.BreakDownRequiresBaseFunc);

		var plain = new RuleManufactureShortcut("STR_PLAIN");
		plain.Load(new YamlRootNodeReader("{}", "test").ToBase());
		Assert.Equal("", plain.StartFrom);
		Assert.Empty(plain.BreakDownItems);
		Assert.False(plain.BreakDownRequires);
		Assert.True(plain.BreakDownRequiresBaseFunc);
	}

	[Fact]
	public void Manufacture_and_research_defaults()
	{
		var manufacture = new RuleManufacture("STR_ITEM", 7);
		Assert.Equal("STR_ITEM", manufacture.Name);
		Assert.Equal(7, manufacture.ListOrder);
		Assert.True(manufacture.CanAutoSell());
		Assert.True(manufacture.HaveEnoughMoneyForOneMoreUnit(-5)); // cost 0
		Assert.Null(manufacture.ProducedCraft);

		var research = new RuleResearch("STR_TOPIC", 3);
		Assert.Equal(3, research.ListOrder);
		Assert.Equal(1, research.SpawnedItemCount);
		Assert.False(research.IsHoldingNeededItem);
		Assert.Null(research.Lookup);
	}

	[Fact]
	public void Copy_does_not_share_containers_with_the_original()
	{
		// C++ Mod::loadAll copies the startFrom project by value before breakDown() rewrites the copy.
		var topic = new RuleResearch("STR_TOPIC", 0);
		var original = new RuleManufacture("STR_ITEM", 7);
		original.Requirements.Add(topic);
		original.TransferTimes.Add(24);

		var copy = new RuleManufacture(original);
		Assert.Equal("STR_ITEM", copy.Name);
		Assert.Equal(7, copy.ListOrder);
		Assert.Equal([topic], copy.Requirements);
		Assert.Equal([24], copy.TransferTimes);

		copy.Requirements.Clear();
		copy.TransferTimes.Add(48);
		Assert.Equal([topic], original.Requirements);
		Assert.Equal([24], original.TransferTimes);
	}
}
