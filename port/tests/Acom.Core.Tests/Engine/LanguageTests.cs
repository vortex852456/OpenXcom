using Acom.Engine;
using Acom.Savegame;

namespace Acom.Tests.Engine;

[Collection(nameof(OptionsTests))] // reads Options.language / Options.wordwrap
public class LanguageTests
{
	private static Language Load(string languageId, string yaml)
	{
		Options.language = languageId;
		Options.wordwrap = TextWrapping.WRAP_AUTO;
		var language = new Language();
		language.LoadFile(new YamlRootNodeReader(yaml, languageId + ".yml"));
		return language;
	}

	[Fact]
	public void Loads_strings_markers_and_plural_forms()
	{
		var lang = Load("en-US", """
			en-US:
			  STR_HELLO: "Hello{NEWLINE}World{ALT}!"
			  STR_SMALL: "a{SMALLLINE}b"
			  STR_EMPTY: ""
			  STR_ITEMS:
			    zero: "no items"
			    one: "{N} item"
			    other: "{N} items"
			""");
		Assert.Equal("Hello\nWorld\u0001!", lang.GetString("STR_HELLO").ToString());
		Assert.Equal("a\u0002b", lang.GetString("STR_SMALL").ToString());
		Assert.Equal("STR_EMPTY", lang.GetString("STR_EMPTY").ToString()); // empty strings are not loaded
		Assert.Equal("no items", lang.GetString("STR_ITEMS", 0).ToString());
		Assert.Equal("1 item", lang.GetString("STR_ITEMS", 1).ToString());
		Assert.Equal("5 items", lang.GetString("STR_ITEMS", 5).ToString());
		Assert.Equal("STR_MISSING", lang.GetString("STR_MISSING").ToString());
		Assert.Equal(TextWrapping.WRAP_WORDS, lang.TextWrapping);
	}

	[Fact]
	public void Uses_language_specific_plural_rules_and_cjk_wrapping()
	{
		// like real files, wrapped in the language id (a bare plural map as first entry would be taken as the wrapper)
		var ru = Load("ru", "ru:\n  STR_X: {one: \"{N} one\", few: \"{N} few\", many: \"{N} many\", other: \"{N} other\"}");
		Assert.Equal("21 one", ru.GetString("STR_X", 21).ToString());
		Assert.Equal("3 few", ru.GetString("STR_X", 3).ToString());
		Assert.Equal("11 many", ru.GetString("STR_X", 11).ToString());
		Assert.Equal(TextWrapping.WRAP_LETTERS, Load("ja", "STR_A: a").TextWrapping);
	}

	[Fact]
	public void Gender_specific_strings()
	{
		var lang = Load("de", "STR_RANK_MALE: Herr\nSTR_RANK_FEMALE: Frau");
		Assert.Equal("Frau", lang.GetString("STR_RANK", SoldierGender.GENDER_FEMALE).ToString());
	}

	[Fact]
	public void Arguments_replace_all_markers_in_order()
	{
		var text = new LocalizedText("{0} and {1} and {0}").Arg(5).Arg("x");
		Assert.Equal("5 and x and 5", text.ToString());
		// a missing marker still advances to the next argument
		Assert.Equal("b", new LocalizedText("{1}").Arg("a").Arg("b").ToString());
		Assert.Equal("1.23457e+06 0.1", new LocalizedText("{0} {1}").Arg(1234567.0).Arg(0.1).ToString());
	}

	[Theory]
	[InlineData("en-us.yml", "en-US")]
	[InlineData("de.yml", "de")]
	[InlineData("zh-tw.yml", "zh-TW")]
	public void File_names_map_to_ids(string file, string id)
	{
		Assert.Equal(id, Language.FileNameToId(file));
	}

	[Fact]
	public void Real_language_file_loads()
	{
		var path = Path.Combine(RepoRoot(), "bin", "common", "Language", "de.yml");
		Options.language = "de";
		var lang = new Language();
		lang.LoadFile(YamlRootNodeReader.FromFile(path));
		Assert.NotEqual("STR_NEW_GAME", lang.GetString("STR_NEW_GAME").ToString());
	}

	internal static string RepoRoot()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir != null && !File.Exists(Path.Combine(dir.FullName, "port", "Acom.slnx")))
			dir = dir.Parent;
		return dir!.FullName;
	}
}

public class RepoYamlTests
{
	/// <summary>Every ruleset and language file of the repo must parse with the port's YAML reader.</summary>
	[Fact]
	public void All_repository_yaml_files_parse()
	{
		var bin = Path.Combine(LanguageTests.RepoRoot(), "bin");
		var files = Directory.EnumerateFiles(bin, "*.*", SearchOption.AllDirectories)
			.Where(f => f.EndsWith(".rul", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
			.ToList();
		Assert.True(files.Count > 100, "expected the repository rulesets and language files");
		var failures = new List<string>();
		foreach (var file in files)
		{
			try
			{
				var root = YamlRootNodeReader.FromFile(file);
				Assert.True(root.IsValid || new FileInfo(file).Length < 16, file);
			}
			catch (YamlReadException e)
			{
				failures.Add(e.Message);
			}
		}
		Assert.Empty(failures);
	}
}
