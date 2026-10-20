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
}
