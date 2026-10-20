#pragma warning disable CA1716 // namespace mirrors the src/Mod area like the other test folders
namespace Acom.Tests.Mod;

public class ExtraStringsTests
{
	private static YamlNodeReader Node(string yaml) => new YamlRootNodeReader(yaml, "test").ToBase();

	[Fact]
	public void Reads_plain_and_plural_strings()
	{
		var strings = new ExtraStrings();
		const string yaml = """
			type: en-US
			strings:
			  STR_B: "Second"
			  STR_ITEMS:
			    one: "{N} item"
			    other: "{N} items"
			  STR_A: First
			  STR_LIST: [ignored]
			""";
		strings.Load(Node(yaml));
		Assert.Equal(
			[("STR_A", "First"), ("STR_B", "Second"), ("STR_ITEMS_one", "{N} item"), ("STR_ITEMS_other", "{N} items")],
			strings.Strings.Select(p => (p.Key, p.Value)));
	}

	[Fact]
	public void Later_load_overwrites_earlier_strings()
	{
		var strings = new ExtraStrings();
		strings.Load(Node("strings:\n  STR_A: old\n  STR_B: keep"));
		strings.Load(Node("strings:\n  STR_A: new"));
		Assert.Equal("new", strings.Strings["STR_A"]);
		Assert.Equal("keep", strings.Strings["STR_B"]);
	}
}
