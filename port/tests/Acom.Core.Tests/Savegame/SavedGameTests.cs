namespace Acom.Tests.Savegame;

[Collection(nameof(Acom.Tests.Engine.OptionsTests))] // Language reads Options.language / Options.wordwrap
public sealed class SavedGameTests : IDisposable
{
	private readonly string _folder = Path.Combine(Path.GetTempPath(), "acom-savedgame-" + Guid.NewGuid().ToString("N"));

	public SavedGameTests()
	{
		Directory.CreateDirectory(_folder);
	}

	public void Dispose()
	{
		Directory.Delete(_folder, true);
	}

	private string WriteFile(string name, string content)
	{
		string path = Path.Combine(_folder, name);
		File.WriteAllText(path, content);
		return path;
	}

	private static Language English()
	{
		Options.language = "en-US";
		Options.wordwrap = TextWrapping.WRAP_AUTO;
		var lang = new Language();
		lang.LoadFile(new YamlRootNodeReader("""
			en-US:
			  STR_DATE_FIRST: "{0}st"
			  STR_MAY: "May"
			  STR_GEOSCAPE: "Geoscape"
			  STR_BATTLESCAPE: "Battlescape"
			  STR_TURN: "Turn {0}"
			  STR_IRONMAN: "Ironman"
			  STR_QUICK_SAVE_SLOT: "[Quicksave]"
			  STR_AUTO_SAVE_BATTLESCAPE_SLOT_WITH_NUMBER: "[Autosave {0}]"
			""", "en-US.yml"));
		return lang;
	}

	// Header as written by the C++ SavedGame::save; the body is deliberately broken YAML to prove it is never parsed.
	private const string GeoscapeSave = """
		name: My campaign
		version: Extended 8.5.8
		engine: Extended
		build: " (v2026-03-21)"
		time: {second: 0, minute: 5, hour: 14, weekday: 6, day: 21, month: 5, year: 1999}
		mods:
		  - 'xcom1 ver: 1.0'
		  - 'other ver: 2'
		ironman: true
		---
		difficulty: [unclosed
		""";

	[Fact]
	public void Header_reading_stops_at_the_document_separator()
	{
		string path = WriteFile("a.sav", GeoscapeSave);
		// only the first document is parsed: the broken body would throw otherwise
		var header = YamlRootNodeReader.FromFile(path, true);
		Assert.True(header["ironman"].ReadVal<bool>());
		Assert.False(header["difficulty"].IsValid);
		Assert.Throws<YamlReadException>(() => YamlRootNodeReader.FromFile(path));

		// no separator: C++ ends up with an empty header (npos + 1)
		Assert.False(YamlRootNodeReader.FromFile(WriteFile("b.sav", "name: x\n"), true).IsValid);

		// separator across the 4096 byte read chunks
		string big = "name: " + new string('a', 4088) + "\n";
		Assert.Equal(4095, big.Length);
		string bigPath = WriteFile("c.sav", big + "---\nbody: 1\n");
		Assert.Equal(new string('a', 4088), YamlRootNodeReader.FromFile(bigPath, true)["name"].ReadVal<string>());
		Assert.False(YamlRootNodeReader.FromFile(bigPath, true)["body"].IsValid);

		// the raw header stops reading after the chunk that holds the separator
		string huge = big + "---\n" + new string('b', 20000);
		Assert.Equal(8192, CrossPlatform.GetYamlSaveHeaderRaw(WriteFile("d.sav", huge)).Length);

		Assert.Throws<IOException>(() => YamlRootNodeReader.FromFile(Path.Combine(_folder, "missing.sav"), true));
	}

	[Fact]
	public void Save_info_of_a_geoscape_save()
	{
		string path = WriteFile("campaign.sav", GeoscapeSave);
		var info = SavedGame.ReadSaveInfo(path, "campaign.sav", English());
		Assert.Equal("campaign.sav", info.fileName);
		Assert.Equal("My campaign", info.displayName);
		Assert.False(info.reserved);
		Assert.Equal(["xcom1 ver: 1.0", "other ver: 2"], info.mods);
		Assert.Equal("Geoscape: 21st May 1999, 14:05 (Ironman)", info.details);
		Assert.Equal(new DateTimeOffset(File.GetLastWriteTimeUtc(path)).ToUnixTimeSeconds(), info.timestamp);
		Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", info.isoDate);
		Assert.Matches(@"^\d{2}:\d{2}$", info.isoTime);
		Assert.True(SavedGame.IsCurrentGameType(info, "xcom1"));
		Assert.False(SavedGame.IsCurrentGameType(info, "xcom2"));
	}

	[Fact]
	public void Save_info_of_battlescape_and_reserved_saves()
	{
		var lang = English();
		const string battle = """
			name: ignored for autosaves
			time: {second: 0, minute: 0, hour: 1, weekday: 1, day: 1, month: 1, year: 1999}
			mission: STR_TERROR_MISSION
			target: STR_CITY
			craftOrBase: "SKYRANGER-1"
			turn: 3
			---
			""";
		var auto = SavedGame.ReadSaveInfo(WriteFile("x" + SavedGame.AUTOSAVE_BATTLESCAPE, battle), "x" + SavedGame.AUTOSAVE_BATTLESCAPE, lang);
		Assert.Equal("[Autosave 3]", auto.displayName);
		Assert.True(auto.reserved);
		Assert.Equal("Battlescape: STR_TERROR_MISSION, Turn 3", auto.details);
		Assert.Empty(auto.mods); // old-style save without mods
		Assert.True(SavedGame.IsCurrentGameType(auto, "xcom1"));
		Assert.False(SavedGame.IsCurrentGameType(auto, "xcom2"));

		var quick = SavedGame.ReadSaveInfo(WriteFile(SavedGame.QUICKSAVE, battle), SavedGame.QUICKSAVE, lang);
		Assert.Equal("[Quicksave]", quick.displayName);
		Assert.True(quick.reserved);

		var unnamed = SavedGame.ReadSaveInfo(WriteFile("unnamed.sav", "time: {hour: 9}\n---\n"), "unnamed.sav", lang);
		Assert.Equal("unnamed", unnamed.displayName);
		Assert.Equal("Geoscape: 1st STR_JAN 1999, 9:00", unnamed.details);
	}

	[Fact]
	public void Emitted_header_and_body_form_two_documents_like_SavedGame_Save()
	{
		// SavedGame.Save concatenates header.Emit() + "---\n" + body.Emit()
		var header = new YamlRootNodeWriter();
		header.SetAsMap();
		header.Write("name", "Emitted");
		new GameTime(6, 21, 5, 1999, 14, 5, 0).Save(header["time"]);
		header.Write("craftOrBase", "SKYRANGER-1").SetAsQuotedAndEscaped();
		header.Write("mods", new List<string> { "xcom1 ver: 1.0" });
		var body = new YamlRootNodeWriter();
		body.SetAsMap();
		body.Write("difficulty", GameDifficulty.DIFF_VETERAN);
		body.Write("rng", ulong.MaxValue);
		body.Write("ids", new SortedDictionary<string, int>(StringComparer.Ordinal) { ["STR_UFO"] = 3, ["ALIEN_BASE"] = 2, ["STR_a"] = 1 });
		string text = header.Emit();
		Assert.EndsWith("\n", text, StringComparison.Ordinal);
		text += "---\n" + body.Emit();

		var documents = new YamlRootNodeReader(text, "save");
		Assert.Equal("Emitted", documents[0]["name"].ReadVal<string>());
		Assert.Equal(GameDifficulty.DIFF_VETERAN, documents[1]["difficulty"].ReadVal<GameDifficulty>());
		Assert.Equal(ulong.MaxValue, documents[1]["rng"].ReadVal<ulong>());
		Assert.Equal(["ALIEN_BASE", "STR_UFO", "STR_a"], documents[1]["ids"].Children().Select(c => c.Key).ToArray());

		var info = SavedGame.ReadSaveInfo(WriteFile("emitted.sav", text), "emitted.sav", English());
		Assert.Equal("Emitted", info.displayName);
		Assert.Equal(["xcom1 ver: 1.0"], info.mods);
		Assert.Equal("Geoscape: 21st May 1999, 14:05", info.details);
	}

	[Theory]
	[InlineData("xcom1 ver: 1.0", "xcom1")]
	[InlineData("xcom1", "xcom1")]
	[InlineData("my mod ver: 2 ver: 3", "my mod")]
	public void Mod_names_lose_their_version(string name, string expected)
	{
		Assert.Equal(expected, SavedGame.SanitizeModName(name));
	}
}
