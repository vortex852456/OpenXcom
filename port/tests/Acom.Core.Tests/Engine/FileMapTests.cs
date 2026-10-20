using System.IO.Compression;
using Acom.Engine;

namespace Acom.Tests.Engine;

public class ModInfoTests
{
	private static bool Check(string a, string b)
	{
		var aa = ModInfo.NormalizeModVersion("x", a);
		var bb = ModInfo.NormalizeModVersion("x", b);
		Assert.NotEmpty(aa.Normalized);
		Assert.NotEmpty(bb.Normalized);
		return ModInfo.CompareVersions(aa, bb);
	}

	/// <summary>Ported from the self-tests at the end of src/Engine/ModInfo.cpp.</summary>
	[Fact]
	public void Version_comparison_matches_cpp_self_tests()
	{
		Assert.True(ModInfo.FindCompatibleEngine("Extended", Acom.Version.OPENXCOM_VERSION_NUMBER));
		Assert.True(ModInfo.FindCompatibleEngine("Extended", [1, 0, 0, 0]));
		Assert.True(ModInfo.FindCompatibleEngine("", [0, 0, 0, 0]));
		Assert.False(ModInfo.FindCompatibleEngine("Extended", [8, 5, 8, 1]));
		Assert.False(ModInfo.FindCompatibleEngine("XYZ", Acom.Version.OPENXCOM_VERSION_NUMBER));
		Assert.False(ModInfo.FindCompatibleEngine("XYZ", [0, 0, 0, 0]));

		Assert.Empty(ModInfo.NormalizeModVersion("x", "").Normalized);
		Assert.True(Check("A", "1"));
		Assert.False(Check("1", "A"));
		Assert.True(Check("A0", "A.0"));
		Assert.True(Check("A.0", "A0"));
		Assert.True(Check("A1", "A.0"));
		Assert.False(Check("A.0", "A1"));
		Assert.True(Check("A0.0", "A.0"));
		Assert.True(Check("B", "A.0"));
		Assert.False(Check("A.0", "B"));
		Assert.True(Check("BA", "B"));
		Assert.True(Check("A11", "A2"));
		Assert.False(Check("0000", "0001"));
		Assert.True(Check("0001", "0000"));
		Assert.True(Check("0001", "0000000"));
		Assert.True(Check("1", "0000000"));
		Assert.True(Check("1", "0000001"));
		Assert.True(Check("0001", "0000001"));
		Assert.True(Check("0001", "1"));
		Assert.True(Check("A1", "A0000001"));
		Assert.True(Check("A0001", "A0000001"));
		Assert.True(Check("A0001", "A1"));
		Assert.True(Check("10001", "0000"));
		Assert.False(Check("0000", "10001"));
		Assert.True(Check("1.0", "1"));
		Assert.True(Check("1", "1.0"));
		Assert.True(Check("1", "1.0.0.0"));
		Assert.True(Check("1", "1.0.000.0"));
		Assert.False(Check("1", "1.0.000.0.1"));
	}

	[Fact]
	public void Metadata_is_loaded_with_defaults()
	{
		var mod = new ModInfo("mods/Better_Guns");
		mod.Load(new YamlRootNodeReader("name: Better Guns\nversion: 1.2\nreservedSpace: 500\nrequiredExtendedVersion: 8.0", "metadata.yml").ToBase());
		Assert.Equal("Better_Guns", mod.Id);
		Assert.Equal("Better Guns", mod.Name);
		Assert.Equal("1.2", mod.VersionDisplay);
		Assert.Equal(100, mod.ReservedSpace);
		Assert.Equal("xcom1", mod.Master);
		Assert.True(mod.IsEngineOk);
		Assert.True(mod.CanActivate("xcom1"));
		Assert.False(mod.CanActivate("xcom2"));

		var master = new ModInfo("standard/acom");
		master.Load(new YamlRootNodeReader("isMaster: true\nloadResources: [UFO]", "metadata.yml").ToBase());
		Assert.Equal("", master.Master);
		Assert.Equal(["UFO"], master.ExternalResourceDirs);
	}
}

// FileMap and Options are global state.
[Collection(nameof(OptionsTests))]
public sealed class FileMapTests : IDisposable
{
	private readonly string _root = Directory.CreateTempSubdirectory("acom-vfs-").FullName + "/";

	public FileMapTests()
	{
		Write("data/common/Language/en-US.yml", "en-US:\n  STR_A: common");
		Write("data/common/Resources/a.png", "common-a");
		Write("data/common/Resources/b.png", "common-b");
		// "common" needs at least 6 entries to be recognized (CrossPlatform::searchDataFolder)
		Write("data/common/Palettes/p.txt", "");
		Write("data/common/Shaders/s.txt", "");
		Write("data/common/SoldierName/n.nam", "");
		Write("data/common/icon.png", "");
		Write("data/standard/xcom1/metadata.yml", "name: Base\nisMaster: true");
		Write("data/standard/xcom1/Rulesets/b.rul", "items: []");
		Write("data/standard/xcom1/Rulesets/a.rul", "items: []");
		Write("data/standard/xcom1/Resources/B.PNG", "master-b");
		Write("data/standard/not_allowed/metadata.yml", "isMaster: true");
		Write("user/mods/GunMod/metadata.yml", "id: gunmod\nname: Gun Mod");
		Write("user/mods/GunMod/Ruleset/guns.rul", "items: []");
		Write("user/mods/GunMod/Resources/b.png", "mod-b");
		Write("user/mods/GunMod/Language/en-US.yml", "en-US:\n  STR_A: mod");
		Write("user/mods/Orphan/metadata.yml", "master: missing_master");
		Write("user/mods/LoopA/metadata.yml", "master: LoopB");
		Write("user/mods/LoopB/metadata.yml", "master: LoopA");
		using (var zip = ZipFile.Open(_root + "user/mods/zipped.zip", ZipArchiveMode.Create))
		{
			AddEntry(zip, "ZipMod/metadata.yml", "id: zipmod");
			AddEntry(zip, "ZipMod/Resources/z.png", "zip-z");
			AddEntry(zip, "ZipMod/Ruleset/z.rul", "items: []");
		}
		Options.SetFoldersForTest(_root + "data/", _root + "user/", _root + "user/");
	}

	public void Dispose()
	{
		FileMap.Clear(clearOnly: true, embeddedOnly: false);
		Directory.Delete(_root, recursive: true);
	}

	private void Write(string path, string content)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(_root + path)!);
		File.WriteAllText(_root + path, content);
	}

	private static void AddEntry(ZipArchive zip, string name, string content)
	{
		using var w = new StreamWriter(zip.CreateEntry(name).Open());
		w.Write(content);
	}

	private static string Text(string path) => System.Text.Encoding.UTF8.GetString(FileMap.ReadAll(path));

	private static void ScanAll()
	{
		FileMap.Clear(clearOnly: false, embeddedOnly: false);
		FileMap.ScanModDir(Options.GetDataFolder(), "standard", protectedLocation: true);
		FileMap.ScanModDir(Options.GetUserFolder(), "mods", protectedLocation: false);
		FileMap.CheckModsDependencies();
	}

	[Fact]
	public void Scan_rejects_unknown_standard_mods_and_broken_dependencies()
	{
		ScanAll();
		var mods = FileMap.GetModInfos();
		Assert.Equal(["gunmod", "xcom1", "zipmod"], mods.Keys);
		Assert.Equal("Gun Mod", mods["gunmod"].Name);
	}

	[Fact]
	public void Later_layers_override_and_lookups_ignore_case()
	{
		ScanAll();
		var mods = FileMap.GetModInfos();
		FileMap.Setup([mods["gunmod"], mods["zipmod"]], embeddedOnly: false);

		Assert.Equal("common-a", Text("Resources/A.PNG"));
		Assert.Equal("mod-b", Text("resources/b.png")); // common < xcom1 < gunmod
		Assert.Equal("zip-z", Text("Resources/z.png"));
		Assert.Equal(["a.png", "b.png", "z.png"], FileMap.GetVFolderContents("Resources/").Order(StringComparer.Ordinal));
		Assert.Equal(["b.png"], FileMap.GetVFolderContents("Resources", 1)); // layer 1 = xcom1
		Assert.False(FileMap.FileExists("Resources/missing.png"));
		Assert.Throws<FileNotFoundException>(() => FileMap.At("Resources/missing.png"));

		// language slice: one entry per layer (common, xcom1, gunmod, zipmod)
		var slice = FileMap.GetSlice("Language/en-US.yml");
		Assert.Equal(4, slice.Count);
		Assert.NotNull(slice[0]);
		Assert.Null(slice[1]);
		Assert.NotNull(slice[2]);

		// rulesets per mod, master first, files sorted
		var rulesets = FileMap.GetRulesets();
		Assert.Equal(["xcom1", "gunmod", "zipmod"], rulesets.Select(r => r.ModId));
		Assert.Equal(["a.rul", "b.rul"], rulesets[0].Rulesets.Select(r => Path.GetFileName(r.FullPath)));
		Assert.NotNull(FileMap.GetModRuleFile(mods["gunmod"], "Ruleset/guns.rul"));
	}

	[Fact]
	public void Filter_files_by_extension()
	{
		var files = FileMap.FilterFiles(["a.yml", "b.YML", "yml", ".yml", "c.txt"], "yml");
		Assert.Equal(["a.yml", "b.YML"], files.Order(StringComparer.Ordinal));
	}
}

[Collection(nameof(OptionsTests))]
public sealed class ModListTests : IDisposable
{
	private readonly string _root = Directory.CreateTempSubdirectory("acom-mods-").FullName + "/";

	public ModListTests()
	{
		// like the original, a layer needs more than one file to be mapped
		foreach (var dir in new[] { "Language", "Resources", "Palettes", "Shaders", "SoldierName" })
			Write("data/common/" + dir + "/file.txt", "");
		Write("data/common/icon.png", "");
		Write("data/standard/xcom1/metadata.yml", "isMaster: true\nversion: 1.0");
		Write("data/standard/xcom1/Rulesets/a.rul", "");
		Write("data/standard/xcom2/metadata.yml", "isMaster: true\nversion: 1.0");
		Write("data/standard/xcom2/Rulesets/a.rul", "");
		Write("user/mods/NewMod/metadata.yml", "id: newmod\nmaster: xcom1");
		Write("user/mods/NewMod/Ruleset/a.rul", "");
		Write("user/mods/FutureMod/metadata.yml", "id: futuremod\nrequiredExtendedVersion: 99.0");
		Write("user/mods/FutureMod/Ruleset/a.rul", "");
		Options.SetFoldersForTest(_root + "data/", _root + "user/", _root + "user/");
		Options.Create();
		Options.ResetDefault(includeMods: false);
	}

	public void Dispose()
	{
		FileMap.Clear(clearOnly: true, embeddedOnly: false);
		Directory.Delete(_root, recursive: true);
	}

	private void Write(string path, string content)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(_root + path)!);
		File.WriteAllText(_root + path, content);
	}

	[Fact]
	public void Refresh_removes_missing_mods_and_activates_one_master()
	{
		Options.mods = [new ModToggle("gone", true), new ModToggle("newmod", true), new ModToggle("xcom2", true), new ModToggle("xcom1", true)];
		Options.RefreshMods();
		// misplaced masters are re-inserted at the front one by one in sorted order (as in C++),
		// so xcom2 ends up before xcom1; only one master stays active
		Assert.Equal(["xcom2", "xcom1", "newmod", "futuremod"], Options.mods.Select(m => m.Id));
		Assert.True(Options.mods[1].Active);
		Assert.Single(Options.mods, m => m.Active && Options.GetModInfos()[m.Id].IsMaster);
		Assert.True(File.Exists(_root + "user/options.cfg"));
	}

	[Fact]
	public void Update_refuses_mods_that_need_a_newer_engine()
	{
		Options.mods = [new ModToggle("xcom1", true), new ModToggle("futuremod", true)];
		Assert.Throws<InvalidOperationException>(Options.UpdateMods);

		Options.mods = [new ModToggle("xcom1", true), new ModToggle("newmod", true)];
		Options.UpdateMods();
		Assert.Equal(["xcom1", "newmod"], Options.GetActiveMods().Select(m => m.Id));
		Assert.Equal("xcom1", Options.GetActiveMaster());
		Assert.True(Directory.Exists(_root + "user/xcom1"));
	}
}
