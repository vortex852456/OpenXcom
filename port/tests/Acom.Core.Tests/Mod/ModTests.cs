#pragma warning disable CA1716 // namespace mirrors src/Mod like the other test folders
using GameMod = Acom.Mod;

namespace Acom.Tests.Mod;

/// <summary>
/// Mod load helpers: offset arithmetic (loadOffsetNode/loadSpriteOffset/loadTransparencyOffset), base functions,
/// kill criteria, the editable name/int lists and the main node handling of loadRule.
/// </summary>
[Collection("OptionsTests")]
public sealed class ModLoadHelperTests : IDisposable
{
	private readonly string _dir = Directory.CreateTempSubdirectory("acom-mod-").FullName + "/";

	public ModLoadHelperTests()
	{
		Options.SetFoldersForTest(_dir, _dir, _dir);
		CrossPlatform.SetArgs([]);
		Options.Create();
		Options.ResetDefault(includeMods: false);
		GameMod.ResetGlobalStatics();
	}

	public void Dispose()
	{
		CrossPlatform.SetArgs([]);
		Directory.Delete(_dir, recursive: true);
	}

	private static YamlNodeReader Node(string yaml) => new YamlRootNodeReader("n: " + yaml, "test").ToBase()["n"];

	/// <summary>Master "xcom1" (offset 0, size 1000) and the current mod "mod2" (offset 1000, size 2000).</summary>
	private static GameMod CreateMod()
	{
		var mod = new GameMod(createConverter: false);
		mod._modData.Add(new ModData { name = "xcom1", offset = 0, size = 1000 });
		mod._modData.Add(new ModData { name = "mod2", offset = 1000, size = 2000 });
		mod._modCurrent = mod._modData[1];
		mod._sets["BASEBITS.PCK"] = new SurfaceSet(32, 40) { MaxSharedFrames = 50 };
		mod._sets["Projectiles"] = new SurfaceSet(3, 3) { MaxSharedFrames = 385 };
		return mod;
	}

	private static int SpriteOffset(GameMod mod, string yaml, string set = "BASEBITS.PCK", int multiplier = 1)
	{
		int sprite = 0;
		mod.LoadSpriteOffset("test", ref sprite, Node(yaml), set, multiplier);
		return sprite;
	}

	[Fact]
	public void Sprite_offsets_above_the_shared_frames_get_the_mod_offset()
	{
		var mod = CreateMod();
		Assert.Equal(10, SpriteOffset(mod, "10"));
		Assert.Equal(49, SpriteOffset(mod, "49"));
		Assert.Equal(1050, SpriteOffset(mod, "50"));
		Assert.Equal(3000, SpriteOffset(mod, "2000"));
		Assert.Equal(GameMod.NO_SURFACE, SpriteOffset(mod, "-1"));

		// explicit mod: master has offset 0, "current" is the loading mod, other mods by name
		Assert.Equal(60, SpriteOffset(mod, "{index: 60, mod: master}"));
		Assert.Equal(1060, SpriteOffset(mod, "{index: 60, mod: current}"));
		Assert.Equal(60, SpriteOffset(mod, "{index: 60, mod: xcom1}"));
		Assert.Equal(1060, SpriteOffset(mod, "{index: 60, mod: mod2}"));

		// a missing node leaves the value unchanged
		int sprite = 7;
		mod.LoadSpriteOffset("test", ref sprite, new YamlRootNodeReader("x: 1", "test").ToBase()["n"], "BASEBITS.PCK");
		Assert.Equal(7, sprite);
	}

	[Fact]
	public void Invalid_sprite_offsets_throw()
	{
		var mod = CreateMod();
		var e = Assert.Throws<LoadRuleException>(() => SpriteOffset(mod, "-2"));
		Assert.Contains("offset '-2' has incorrect value in set 'BASEBITS.PCK'", e.Message, StringComparison.Ordinal);
		e = Assert.Throws<LoadRuleException>(() => SpriteOffset(mod, "2001"));
		Assert.Contains("offset '2001' exceeds mod size limit 2000 in set 'BASEBITS.PCK'", e.Message, StringComparison.Ordinal);
		// the master is smaller
		Assert.Throws<LoadRuleException>(() => SpriteOffset(mod, "{index: 1001, mod: master}"));
		e = Assert.Throws<LoadRuleException>(() => SpriteOffset(mod, "{index: 5, mod: other}"));
		Assert.Contains("unknown mod 'other' used", e.Message, StringComparison.Ordinal);
		e = Assert.Throws<LoadRuleException>(() => SpriteOffset(mod, "[1, 2]"));
		Assert.Contains("unsupported yaml node", e.Message, StringComparison.Ordinal);
		Assert.StartsWith("Error for 'test': ", e.Message, StringComparison.Ordinal);
		Assert.Throws<InvalidOperationException>(() => SpriteOffset(mod, "1", "NOSUCH.PCK"));
	}

	[Fact]
	public void Projectile_offsets_are_multiplied_before_the_checks()
	{
		var mod = CreateMod();
		// 10 * 35 = 350 < 385 shared frames
		Assert.Equal(350, SpriteOffset(mod, "10", "Projectiles", 35));
		// 20 * 35 = 700 is a mod frame
		Assert.Equal(1700, SpriteOffset(mod, "20", "Projectiles", 35));
		// 58 * 35 = 2030 > 2000; the message gives the limit in projectile units
		var e = Assert.Throws<LoadRuleException>(() => SpriteOffset(mod, "58", "Projectiles", 35));
		Assert.Contains("offset '58' exceeds mod size limit 57", e.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Transparency_offsets_use_a_hundredth_of_the_mod_space()
	{
		var mod = CreateMod();
		int index = 0;
		mod.LoadTransparencyOffset("test", ref index, Node("5"));
		Assert.Equal(15, index);
		mod.LoadTransparencyOffset("test", ref index, Node("20"));
		Assert.Equal(30, index);
		Assert.Throws<LoadRuleException>(() => mod.LoadTransparencyOffset("test", ref index, Node("21")));
	}

	[Fact]
	public void Sprite_lists_load_each_element()
	{
		var mod = CreateMod();
		var sprites = new List<int> { 99 };
		mod.LoadSpriteOffset("test", sprites, Node("[1, 60, {index: 2, mod: master}]"), "BASEBITS.PCK");
		Assert.Equal([1, 1060, 2], sprites);
		mod.LoadSpriteOffset("test", sprites, Node("70"), "BASEBITS.PCK");
		Assert.Equal([1070], sprites);
		// -1 is an error in a list ...
		Assert.Throws<LoadRuleException>(() => mod.LoadSpriteOffset("test", sprites, Node("[1, -1]"), "BASEBITS.PCK"));
		// ... unless the validation is turned down, then it is dropped
		Options.oxceModValidationLevel = (int)SeverityLevel.LOG_ERROR;
		mod.LoadSpriteOffset("test", sprites, Node("[1, -1, 3]"), "BASEBITS.PCK");
		Assert.Equal([1, 3], sprites);
	}

	[Fact]
	public void Generic_offsets_and_mod_offset()
	{
		var mod = CreateMod();
		Assert.Equal(1000, mod.ModOffset);
		Assert.Equal(5, mod.GetOffset(5, 5));
		Assert.Equal(1006, mod.GetOffset(6, 5));
	}

	[Fact]
	public void Base_functions_get_shared_indexes_and_support_add_and_remove()
	{
		var mod = CreateMod();
		var f = default(RuleBaseFacilityFunctions);
		mod.LoadBaseFunction("test", ref f, Node("[LAB, WORKSHOP]"));
		// index 0 is reserved for "empty"
		Assert.False(f.Test(0));
		Assert.True(f.Test(1));
		Assert.True(f.Test(2));
		Assert.Equal(2, f.Count());
		Assert.Equal(["LAB", "WORKSHOP"], mod.GetBaseFunctionNames(f));

		// another rule reuses the indexes of known names
		var g = default(RuleBaseFacilityFunctions);
		mod.LoadBaseFunction("test", ref g, Node("[WORKSHOP, HANGAR]"));
		Assert.True(g.Test(2));
		Assert.True(g.Test(3));
		Assert.Equal(2, g.Count());

		mod.LoadBaseFunction("test", ref f, Node("!add [HANGAR]"));
		Assert.Equal(["LAB", "WORKSHOP", "HANGAR"], mod.GetBaseFunctionNames(f));
		mod.LoadBaseFunction("test", ref f, Node("!remove [LAB, PSI]"));
		Assert.Equal(["WORKSHOP", "HANGAR"], mod.GetBaseFunctionNames(f));
		// a plain list replaces everything
		mod.LoadBaseFunction("test", ref f, Node("[PSI]"));
		Assert.True(f.Test(4));
		Assert.Equal(["PSI"], mod.GetBaseFunctionNames(f));
		// missing node: unchanged
		mod.LoadBaseFunction("test", ref f, new YamlRootNodeReader("x: 1", "test").ToBase()["n"]);
		Assert.Equal(["PSI"], mod.GetBaseFunctionNames(f));

		var e = Assert.Throws<LoadRuleException>(() => mod.LoadBaseFunction("test", ref f, Node("LAB")));
		Assert.Contains("wrong node type, expected a list", e.Message, StringComparison.Ordinal);
		e = Assert.Throws<LoadRuleException>(() => mod.LoadBaseFunction("test", ref f, Node("!foo [LAB]")));
		Assert.Contains("unsupported node tag '!foo'", e.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Base_functions_are_limited_to_127_names()
	{
		var mod = CreateMod();
		var f = default(RuleBaseFacilityFunctions);
		var names = Enumerable.Range(1, 127).Select(i => "F" + i).ToList();
		mod.LoadBaseFunction("test", ref f, Node("[" + string.Join(", ", names) + "]"));
		Assert.Equal(127, f.Count());
		Assert.False(f.Test(0));
		Assert.Equal(names, mod.GetBaseFunctionNames(f));

		var e = Assert.Throws<LoadRuleException>(() => mod.LoadBaseFunction("test", ref f, Node("[F1, TOO_MANY]")));
		Assert.Contains("Error for 'test': Number of unique names reached limit because of name 'TOO_MANY'", e.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Kill_criteria_load_nested_lists_and_add()
	{
		var mod = CreateMod();
		var criteria = new List<List<(int first, List<string> second)>>();
		mod.LoadKillCriteria("test", criteria, Node("[[[1, [FACTION_HOSTILE, STATUS_DEAD]], [2, [STR_SECTOID]]], [[3, [STATUS_UNCONSCIOUS]]]]"));
		Assert.Equal(2, criteria.Count);
		Assert.Equal(2, criteria[0].Count);
		Assert.Equal(1, criteria[0][0].first);
		Assert.Equal(["FACTION_HOSTILE", "STATUS_DEAD"], criteria[0][0].second);
		Assert.Equal(2, criteria[0][1].first);
		Assert.Equal(["STR_SECTOID"], criteria[0][1].second);
		Assert.Equal(3, criteria[1][0].first);

		mod.LoadKillCriteria("test", criteria, Node("!add [[[4, [STR_FLOATER]]]]"));
		Assert.Equal(3, criteria.Count);
		Assert.Equal(4, criteria[2][0].first);

		mod.LoadKillCriteria("test", criteria, Node("[[[5, [A]]]]"));
		Assert.Single(criteria);
		Assert.Equal(5, criteria[0][0].first);

		Assert.Throws<LoadRuleException>(() => mod.LoadKillCriteria("test", criteria, Node("{a: 1}")));
		Assert.Throws<LoadRuleException>(() => mod.LoadKillCriteria("test", criteria, Node("[{a: 1}]")));
		Assert.Throws<LoadRuleException>(() => mod.LoadKillCriteria("test", criteria, Node("!remove [[[5, [A]]]]")));
	}

	[Fact]
	public void Editable_lists_and_maps()
	{
		var mod = CreateMod();
		var names = new List<string>();
		mod.LoadUnorderedNames("test", names, Node("[A, B, A]"));
		Assert.Equal(["A", "B", "A"], names);
		mod.LoadUnorderedNames("test", names, Node("!add [C]"));
		Assert.Equal(["A", "B", "A", "C"], names);
		mod.LoadUnorderedNames("test", names, Node("!remove [A]"));
		Assert.Equal(["B", "C"], names);
		Assert.Throws<LoadRuleException>(() => mod.LoadUnorderedNames("test", names, Node("[A, '']")));
		// standard lists cannot be edited
		Assert.Throws<LoadRuleException>(() => mod.LoadNames("test", names, Node("!add [D]")));
		mod.LoadNames("test", names, Node("[D]"));
		Assert.Equal(["D"], names);

		var ints = new List<int> { 1 };
		mod.LoadInts("test", ints, Node("[3, 2]"));
		Assert.Equal([3, 2], ints);
		mod.LoadUnorderedInts("test", ints, Node("!remove [3]"));
		Assert.Equal([2], ints);

		var map = new SortedDictionary<string, int>(StringComparer.Ordinal);
		mod.LoadUnorderedNamesToInt("test", map, Node("{B: 2, A: 1}"));
		mod.LoadUnorderedNamesToInt("test", map, Node("!add {C: 3, A: 5}"));
		Assert.Equal([("A", 5), ("B", 2), ("C", 3)], map.Select(p => (p.Key, p.Value)));
		mod.LoadUnorderedNamesToInt("test", map, Node("!remove [B]"));
		Assert.Equal(["A", "C"], map.Keys);
		mod.LoadUnorderedNamesToInt("test", map, Node("{Z: 0}"));
		Assert.Equal(["Z"], map.Keys);

		var pairs = new List<(string first, List<string> second)>();
		mod.LoadNamesToNames("test", pairs, Node("{Y: [A], X: [B]}"));
		mod.LoadNamesToNames("test", pairs, Node("!add {Y: !add [C], W: [D]}"));
		Assert.Equal(["Y", "X", "W"], pairs.Select(p => p.first));
		Assert.Equal(["A", "C"], pairs[0].second);

		string name = "old";
		mod.LoadNameNull("test", ref name, Node("~"));
		Assert.Equal(GameMod.STR_NULL, name);
		Assert.True(GameMod.IsEmptyRuleName(name));
		mod.LoadName("test", ref name, Node("NEW"));
		Assert.Equal("NEW", name);
		Assert.Throws<LoadRuleException>(() => mod.LoadName("test", ref name, Node("''")));
	}

	private sealed class TestRule(string type)
	{
		public string Type { get; } = type;
	}

	[Fact]
	public void Load_rule_handles_the_main_nodes()
	{
		var mod = CreateMod();
		var map = new SortedDictionary<string, TestRule>(StringComparer.Ordinal);
		var index = new List<string>();
		TestRule Load(string yaml) => mod.LoadRule(new YamlRootNodeReader(yaml, "test").ToBase(), map, index, "type", type => new TestRule(type));

		var a = Load("type: A");
		Assert.Equal("A", a.Type);
		Assert.Same(a, Load("type: A"));
		Assert.Same(mod._modCurrent, mod.GetModCreatingRule(a));

		var b = Load("new: B");
		Assert.Equal(["A", "B"], index);
		Assert.Throws<LoadRuleException>(() => Load("new: A"));

		Assert.Same(a, Load("update: A"));
		Assert.Null(Load("update: C"));
		Assert.Same(b, Load("override: B"));
		Assert.Throws<LoadRuleException>(() => Load("override: C"));
		Assert.Null(Load("ignore: A"));

		Assert.Null(Load("delete: A"));
		Assert.Equal(["B"], index);
		Assert.Equal(["B"], map.Keys);
		Assert.Null(Load("delete: A"));

		var e = Assert.Throws<InvalidOperationException>(() => Load("type: A\nnew: B"));
		Assert.Equal("Conflict of main node 'type' at line 1 and 'new' at line 2", e.Message);
		e = Assert.Throws<LoadRuleException>(() => Load("other: A"));
		Assert.Contains("Missing main node", e.Message, StringComparison.Ordinal);
		e = Assert.Throws<InvalidOperationException>(() => Load("type: ''"));
		Assert.Equal("Invalid value for main node 'type' at line 1", e.Message);
		e = Assert.Throws<InvalidOperationException>(() => Load("type: D\nrefNode: 5"));
		Assert.Equal("Invalid refNode at nest level of 0 in D", e.Message);
		string nested = "type: E\n";
		for (int i = 0; i < 66; ++i)
			nested += new string(' ', i * 2) + "refNode:\n";
		nested += new string(' ', 66 * 2) + "x: 1\n";
		e = Assert.Throws<InvalidOperationException>(() => Load(nested));
		Assert.Equal("Nest limit of refNode reach in E", e.Message);
	}

	[Fact]
	public void Soft_errors_throw_or_are_suppressed_by_the_validation_level()
	{
		var mod = CreateMod();
		Assert.False(mod.CheckForSoftError(false, "test", "error"));
		var e = Assert.Throws<LoadRuleException>(() => mod.CheckForSoftError(true, "test", "error"));
		Assert.Equal("Error for 'test': error", e.Message);
		e = Assert.Throws<LoadRuleException>(() => mod.CheckForSoftError(true, "test", Node("1"), "error"));
		Assert.Equal("Error for 'test': error at line 1", e.Message);

		Options.oxceModValidationLevel = (int)SeverityLevel.LOG_ERROR;
		Assert.True(mod.CheckForSoftError(true, "test", "error", SeverityLevel.LOG_WARNING));
		Assert.Throws<LoadRuleException>(() => mod.CheckForSoftError(true, "test", "error", SeverityLevel.LOG_ERROR));
		Options.oxceModValidationLevel = (int)SeverityLevel.LOG_FATAL - 1;
		Assert.Throws<LoadRuleException>(() => mod.CheckForSoftError(true, "test", "error", SeverityLevel.LOG_FATAL));
	}

	[Fact]
	public void Global_statics_are_reset()
	{
		GameMod.DOOR_OPEN = 99;
		GameMod.WINDOW_POPUP[1] = 99;
		GameMod.DIFFICULTY_COEFFICIENT[4] = 99;
		Unicode.OXCE_CURRENCY_SYMBOL = "#";
		GameMod.ResetGlobalStatics();
		Assert.Equal(3, GameMod.DOOR_OPEN);
		Assert.Equal([1, 2, 3], GameMod.WINDOW_POPUP);
		Assert.Equal([0, 1, 2, 3, 4], GameMod.DIFFICULTY_COEFFICIENT);
		Assert.Equal("GMMARS", GameMod.DEBRIEF_MUSIC_GOOD);
		Assert.Equal("$", Unicode.OXCE_CURRENCY_SYMBOL);
	}
}

/// <summary>
/// Loads the rulesets of bin/standard/xcom1 like the ruleset part of loadAll, without the original game resources:
/// the vanilla sprite and sound sets are registered empty and GEODATA/WORLD.DAT is an empty stand-in.
/// Files whose rule classes are still stubs are skipped (NotImplementedException).
/// </summary>
[Collection("OptionsTests")]
public sealed class ModRulesetTests : IDisposable
{
	private static readonly string XcomDir = Path.Combine(Engine.LanguageTests.RepoRoot(), "bin", "standard", "xcom1");

	private readonly string _dir = Directory.CreateTempSubdirectory("acom-modrul-").FullName + "/";

	public ModRulesetTests()
	{
		// the "UFO" resources of the master come from the user folder: only an empty globe stand-in
		Directory.CreateDirectory(_dir + "UFO/GEODATA");
		File.WriteAllBytes(_dir + "UFO/GEODATA/WORLD.DAT", []);
		File.WriteAllText(_dir + "UFO/README.txt", "stand-in");
		Options.SetFoldersForTest(Path.Combine(Engine.LanguageTests.RepoRoot(), "bin") + "/", _dir, _dir);
		CrossPlatform.SetArgs([]);
		Options.Create();
		Options.ResetDefault(includeMods: false);
		Options.mods = [new ModToggle("xcom1", true)];
		Options.UpdateMods();
		GameMod.ResetGlobalStatics();
	}

	public void Dispose()
	{
		FileMap.Clear(clearOnly: true, embeddedOnly: false);
		CrossPlatform.SetArgs([]);
		GameMod.ResetGlobalStatics();
		Directory.Delete(_dir, recursive: true);
	}

	/// <summary>The sets the vanilla resource loading creates (sizes do not matter, they stay empty).</summary>
	private static readonly string[] VanillaSurfaceSets =
	[
		"GlobeMarkers", "CustomArmorPreviews", "CustomItemPreviews", "TinyRanks", "Touch",
		"BASEBITS.PCK", "INTICON.PCK", "TEXTURE.DAT", "SCANG.DAT",
		"SPICONS.DAT", "CURSOR.PCK", "SMOKE.PCK", "HIT.PCK", "X1.PCK", "MEDIBITS.DAT", "DETBLOB.DAT",
		"Projectiles", "UnderwaterProjectiles", "BLANKS.PCK", "BIGOBS.PCK", "FLOOROB.PCK", "HANDOB.PCK",
	];

	private static readonly string[] VanillaSoundSets = ["GEO.CAT", "BATTLE.CAT", "BATTLE2.CAT", "SAMPLE3.CAT", "INTRO.CAT"];

	/// <summary>
	/// A sound set; while SoundSet is a stub, an uninitialized object whose members throw NotImplementedException
	/// (so rules using sounds are skipped instead of failing with "Sound Set not found").
	/// </summary>
	private static SoundSet CreateSoundSet()
	{
		try
		{
			return new SoundSet();
		}
		catch (NotImplementedException)
		{
			return (SoundSet)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(SoundSet));
		}
	}

	private static void RegisterVanillaSets(GameMod mod)
	{
		foreach (var name in VanillaSurfaceSets)
			mod._sets[name] = new SurfaceSet(32, 40);
		foreach (var name in VanillaSoundSets)
			mod._sounds[name] = CreateSoundSet();
	}

	private static GameMod CreateMod()
	{
		var mod = new GameMod(createConverter: false);
		RegisterVanillaSets(mod);
		ModInfo info = Options.GetModInfos()["xcom1"];
		mod._modData.Add(new ModData { name = info.Id, offset = 0, size = 1000 * info.ReservedSpace, info = info });
		mod._modCurrent = mod._modData[0];
		return mod;
	}

	public static TheoryData<string> StandardRulesetFiles()
	{
		var data = new TheoryData<string>();
		foreach (var file in Directory.GetFiles(XcomDir, "*.rul").Select(f => Path.GetFileName(f)).Order(StringComparer.Ordinal))
			data.Add(file);
		return data;
	}

	/// <summary>Rule lists: section, key node and lookup of a loaded rule.</summary>
	private static readonly (string section, string key, Func<GameMod, string, object?> get)[] RuleSections =
	[
		("countries", "type", (m, n) => m.GetCountry(n)),
		("regions", "type", (m, n) => m.GetRegion(n)),
		("facilities", "type", (m, n) => m.GetBaseFacility(n)),
		("crafts", "type", (m, n) => m.GetCraft(n)),
		("craftWeapons", "type", (m, n) => m.GetCraftWeapon(n)),
		("items", "type", (m, n) => m.GetItem(n)),
		("ufos", "type", (m, n) => m.GetUfo(n)),
		("invs", "id", (m, n) => m.GetInventory(n)),
		("terrains", "name", (m, n) => m.GetTerrain(n)),
		("armors", "type", (m, n) => m.GetArmor(n)),
		("soldiers", "type", (m, n) => m.GetSoldier(n)),
		("units", "type", (m, n) => m.GetUnit(n)),
		("alienRaces", "id", (m, n) => m.GetAlienRace(n)),
		("alienDeployments", "type", (m, n) => m.GetDeployment(n)),
		("research", "name", (m, n) => m.GetResearch(n)),
		("manufacture", "name", (m, n) => m.GetManufacture(n)),
		("ufoTrajectories", "id", (m, n) => m.GetUfoTrajectory(n)),
		("alienMissions", "type", (m, n) => m.GetAlienMission(n)),
		("missionScripts", "type", (m, n) => m.GetMissionScript(n)),
		("mapScripts", "type", (m, n) => m.GetMapScript(n)),
		("ufopaedia", "id", (m, n) => m.GetUfopaediaArticle(n)),
		("interfaces", "type", (m, n) => m.GetInterface(n, false)),
		("cutscenes", "type", (m, n) => m.GetVideo(n)),
		("musics", "type", (m, n) => m.Music.GetValueOrDefault(n)),
		("MCDPatches", "type", (m, n) => m.GetMCDPatch(n)),
		("extraSprites", "type", (m, n) => m.ExtraSprites.GetValueOrDefault(n)),
	];

	[Theory(SkipExceptions = [typeof(NotImplementedException)])]
	[MemberData(nameof(StandardRulesetFiles))]
	public void Standard_ruleset_file_loads(string fileName)
	{
		var mod = CreateMod();
		if (fileName == "converter.rul")
			mod._converter = new RuleConverter(); // still a stub
		var path = Path.Combine(XcomDir, fileName);
		mod.LoadFile(YamlRootNodeReader.FromFile(path));

		var root = YamlRootNodeReader.FromFile(path).ToBase();
		int rules = 0;
		foreach (var (section, key, get) in RuleSections)
		{
			foreach (var rule in root[section].Children())
			{
				string name = rule[key].ReadVal<string>();
				Assert.True(get(mod, name) != null, $"{section}: {name} not loaded");
				++rules;
			}
		}

		switch (fileName)
		{
			case "countries.rul":
				Assert.Equal(16, rules);
				Assert.Equal(rules, mod.CountriesList.Count);
				Assert.Equal(10000, mod.GetCountry("STR_USA").FundingCap);
				break;
			case "research.rul":
				Assert.Equal(rules, mod.ResearchList.Count);
				// list order follows the file
				Assert.Equal(root["research"][0]["name"].ReadVal<string>(), mod.ResearchList[0]);
				Assert.Equal(100, mod.GetResearch(mod.ResearchList[0]).ListOrder);
				Assert.Equal(200, mod.GetResearch(mod.ResearchList[1]).ListOrder);
				break;
			case "manufacture.rul":
				Assert.Equal(rules, mod.ManufactureList.Count);
				break;
			case "inventories.rul":
				Assert.Equal(rules, mod.InvsList.Count);
				Assert.Equal(10, mod.GetInventory(mod.InvsList[0]).ListOrder);
				break;
			case "vars.rul":
				Assert.Equal(1999, mod.StartingTime.Year);
				Assert.Equal(12, mod.StartingTime.Hour);
				Assert.Equal(25000, mod.EngineerCost);
				Assert.Equal(50000, mod.HireEngineerCost);
				Assert.Equal(30000, mod.ScientistCost);
				Assert.Equal(72, mod.PersonnelTime);
				Assert.Equal("STR_ELERIUM_115", mod.AlienFuelName);
				Assert.Equal(50, mod.AlienFuelQuantity);
				Assert.Equal("Font.dat", mod.FontName);
				break;
			case "startingBase.rul":
				var startingBase = new YamlRootNodeReader(mod.DefaultStartingBase, "test").ToBase();
				Assert.Equal(root["startingBase"]["facilities"].Children().Count, startingBase["facilities"].Children().Count);
				Assert.Equal(mod.DefaultStartingBase, mod.GetStartingBase(GameDifficulty.DIFF_GENIUS));
				break;
			case "alienItemLevels.rul":
				Assert.Equal(root["alienItemLevels"].Children().Count, mod.AlienItemLevels.Count);
				Assert.Equal([0, 0, 0, 0, 0, 0, 0, 1, 1, 2], mod.AlienItemLevels[0]);
				break;
			case "difficulty.rul":
				Assert.Equal(root["defeatScore"].ReadVal<int>(), mod.DefeatScore);
				break;
			case "globe.rul":
				// WORLD.DAT is an empty stand-in
				Assert.Empty(mod.Globe.Polygons);
				Assert.NotNull(mod.Globe.GetTexture(0));
				break;
		}
	}

	[Fact(SkipExceptions = [typeof(NotImplementedException)])]
	public void All_standard_rulesets_load_and_link()
	{
		var mod = new GameMod(createConverter: false);
		RegisterVanillaSets(mod);
		mod._converter = new RuleConverter(); // still a stub
		mod.LoadAll(loadResources: false);

		Assert.Equal(16, mod.CountriesList.Count);
		Assert.Equal("xcom1", mod.GetModCreatingRule(mod.GetCountry("STR_USA")).name);
		Assert.NotNull(mod.FinalResearch);
		Assert.NotEmpty(mod.ArmorsForSoldiers);
		Assert.NotEmpty(mod.UfopaediaCategoryList);
		Assert.Equal(mod.ItemsList.Count, mod.ItemsList.Distinct().Count());
	}
}
