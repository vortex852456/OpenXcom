#pragma warning disable CA1716 // namespace mirrors the src/Mod area like the other test folders
namespace Acom.Tests.Mod;

public class RuleInterfaceTests
{
	private static YamlNodeReader Node(string yaml) => new YamlRootNodeReader(yaml, "test").ToBase();

	[Fact]
	public void Elements_keep_unset_values_and_merge_repeated_ids()
	{
		var rule = new RuleInterface("test");
		const string yaml = """
			palette: PAL_TEST
			backgroundImage: BACK.SCR
			elements:
			  - id: window
			    color: 133
			    pos: [10, 20]
			    size: [30, 40]
			  - id: window
			    color2: 7
			    TFTDMode: true
			  - id: text
			    custom: -12
			""";
		rule.Load(Node(yaml), null!);
		Assert.Equal("PAL_TEST", rule.Palette);
		Assert.Equal("BACK.SCR", rule.GetBackgroundImage(null!, null!));
		Assert.Equal(-1, rule.Sound);

		var window = rule.GetElement("window");
		Assert.Equal((10, 20, 30, 40), (window.x, window.y, window.w, window.h));
		Assert.Equal(133, window.color);
		Assert.Equal(7, window.color2);
		Assert.Equal(int.MaxValue, window.border);
		Assert.True(window.TFTDMode);

		var text = rule.GetElement("text");
		Assert.Equal(-12, text.custom);
		Assert.Equal(int.MaxValue, text.x);
		Assert.Null(rule.GetElementOptional("missing"));
		Assert.Throws<InvalidOperationException>(() => rule.GetElement("missing"));
	}

	[Fact]
	public void Loads_all_xcom1_interfaces()
	{
		var path = Path.Combine(Engine.LanguageTests.RepoRoot(), "bin", "standard", "xcom1", "interfaces.rul");
		var root = new YamlRootNodeReader(File.ReadAllText(path), path);
		var rules = new Dictionary<string, RuleInterface>(StringComparer.Ordinal);
		foreach (var node in root["interfaces"].Children())
		{
			string type = node["type"].ReadVal<string>();
			if (!rules.TryGetValue(type, out var rule))
				rules[type] = rule = new RuleInterface(type);
			rule.Load(node, null!);
		}
		Assert.True(rules.Count > 50);
		Assert.Equal("GMSTORY", rules["mainMenu"].Music);
		Assert.Equal(-1, rules["mainMenu"].GetElement("battlescapeTheme").color2);
		Assert.Equal(-12, rules["geoscape"].GetElement("slackingIndicator").custom);
		var preview = rules.Values.Select(r => r.GetElementOptional("previewMid")).Single(e => e != null);
		Assert.Equal((0, 140, 160, 52), (preview.x, preview.y, preview.w, preview.h));
	}
}
