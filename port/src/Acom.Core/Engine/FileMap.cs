/*
 * Copyright 2010-2026 OpenXcom Developers.
 * C# port: Copyright 2026 ACOM contributors.
 *
 * This file is part of ACOM, a C# port of OpenXcom Extended.
 *
 * ACOM is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * ACOM is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Acom.  If not, see <http://www.gnu.org/licenses/>.
 */
// Ported from: src/Engine/FileMap.h, src/Engine/FileMap.cpp
// Deviation: no embedded assets (exe:common.zip/standard.zip, scanModZipRW/mapZipFileRW), see docs/architecture/csharp-port.md;
// embeddedOnly therefore never skips the folder/zip search. getAllYAML is declared but not implemented in C++ either.
// Zip files are read with System.IO.Compression instead of miniz.

using System.IO.Compression;
using System.Text;

namespace Acom;

/// <summary>
/// Maps canonical (lower-case) names to file paths and maintains the virtual file system for
/// resource files: "common", then the active mods in load order, later layers override earlier ones.
/// </summary>
public static class FileMap
{
	/// <summary>A file in a folder or inside a zip archive.</summary>
	public sealed class FileRecord
	{
		internal FileRecord(string fullpath, ZipArchive? zip = null, string? entryName = null)
		{
			FullPath = fullpath;
			Zip = zip;
			EntryName = entryName;
		}

		/// <summary>Full path; includes the zip file name if the file is inside a zip.</summary>
		public string FullPath { get; }

		internal ZipArchive? Zip { get; }

		internal string? EntryName { get; }

		/// <summary>Opens the file for reading (zip entries are decompressed into memory).</summary>
		public Stream OpenRead()
		{
			if (Zip == null)
				return File.OpenRead(FullPath);
			return new MemoryStream(ReadAll(), writable: false);
		}

		/// <summary>Reads the whole file.</summary>
		public byte[] ReadAll()
		{
			if (Zip == null)
				return CrossPlatform.ReadFileRaw(FullPath);
			lock (Zip)
			{
				var entry = Zip.GetEntry(EntryName!) ?? throw new IOException($"FileRecord: {FullPath} missing in zip");
				using var s = entry.Open();
				using var buffer = new MemoryStream();
				s.CopyTo(buffer);
				return buffer.ToArray();
			}
		}

		/// <summary>Parses the file as YAML.</summary>
		public YamlRootNodeReader GetYaml()
		{
			try
			{
				return new YamlRootNodeReader(ReadAll(), FullPath);
			}
			catch
			{
				Logger.Fatal($"Error loading file '{FullPath}'");
				throw;
			}
		}
	}

	private static readonly HashSet<string> s_emptySet = new(StringComparer.Ordinal);

	/// <summary>One mapped folder or zip.</summary>
	private sealed class VfsLayer(string path)
	{
		public string FullPath { get; private set; } = path;
		public Dictionary<string, FileRecord> Resources { get; } = new(StringComparer.Ordinal);
		public List<FileRecord> Rulesets { get; } = [];
		public Dictionary<string, HashSet<string>> Vdirs { get; } = new(StringComparer.Ordinal);
		private bool _mapped;

		public FileRecord? At(string relpath) => Resources.GetValueOrDefault(Canonicalize(relpath));

		public HashSet<string> Ls(string relpath) => Vdirs.GetValueOrDefault(Canonicalize(relpath).TrimEnd('/')) ?? s_emptySet;

		public void Insert(string relpath, FileRecord frec)
		{
			string crelpath = Canonicalize(relpath);
			if (IsRuleset(crelpath))
			{
				Rulesets.Add(frec);
				return;
			}
			Resources[crelpath] = frec;
			// update corresponding vdir
			int pos = crelpath.LastIndexOf('/');
			string basename = pos < 0 ? crelpath : crelpath[(pos + 1)..];
			string dirname = pos < 0 ? "" : crelpath[..pos];
			if (!Vdirs.TryGetValue(dirname, out var names))
				Vdirs[dirname] = names = new HashSet<string>(StringComparer.Ordinal);
			names.Add(basename);
		}

		public bool MapZipFile(string zippath, string prefix, bool ignoreRuls = false)
		{
			var zip = OpenZip($"mapZipFile({zippath}, '{prefix}'): ", zippath);
			return zip != null && MapZip(zip, zippath, prefix, ignoreRuls);
		}

		public bool MapZip(ZipArchive zip, string zippath, string prefix, bool ignoreRuls = false)
		{
			string logCtx = $"mapZip(zip, '{zippath}', '{prefix}', '{ignoreRuls}'): ";
			if (_mapped)
				throw new InvalidOperationException(logCtx + "Fatal: already mapped.");
			if (prefix.Length > 0 && prefix[^1] != '/')
				throw new InvalidOperationException(logCtx + "Bogus prefix of '" + prefix);
			_mapped = true;
			FullPath = zippath;
			int mappedCount = 0;
			foreach (var entry in zip.Entries)
			{
				string fname = entry.FullName.Replace('\\', '/');
				if (fname.EndsWith('/') || fname.Length <= prefix.Length)
					continue; // directories
				string relfname = fname;
				if (prefix.Length > 0)
				{
					// cut out the prefix to get the real relfname
					if (!fname.StartsWith(prefix, StringComparison.Ordinal))
						continue;
					relfname = fname[prefix.Length..];
				}
				if (IsRuleset(relfname) && ignoreRuls)
					continue;
				Insert(relfname, new FileRecord(ConcatPaths(zippath, fname), zip, entry.FullName));
				mappedCount++;
			}
			Logger.Verbose(logCtx + "mapped_count=" + mappedCount);
			return mappedCount > 1;
		}

		public bool MapPlainDir(string dirpath, bool ignoreRuls = false)
		{
			if (_mapped)
				throw new InvalidOperationException($"mapPlainDir({dirpath}): fatal: already mapped.");
			var dlist = new List<(string Dir, string Name)>();
			ListRecursive(dirpath, "", dlist);
			FullPath = dirpath;
			int mappedCount = 0;
			foreach (var (dir, name) in dlist)
			{
				string relpath = ConcatOptionalPaths(dir, name);
				if (IsRuleset(name) && ignoreRuls)
					continue;
				Insert(relpath, new FileRecord(ConcatPaths(FullPath, relpath)));
				mappedCount++;
			}
			return mappedCount > 1;
		}
	}

	/// <summary>Stack of layers with merged resources (later layers win).</summary>
	private sealed class VfsLayerStack
	{
		public List<VfsLayer> Layers { get; } = [];
		public Dictionary<string, FileRecord> Resources { get; } = new(StringComparer.Ordinal);
		public List<FileRecord> Rulesets { get; } = [];
		public Dictionary<string, HashSet<string>> Vdirs { get; } = new(StringComparer.Ordinal);

		public void Clear()
		{
			Layers.Clear(); // not disposing them, they're owned by MappedVfsLayers
			Resources.Clear();
			Rulesets.Clear();
			Vdirs.Clear();
		}

		private void MergeVdirs(VfsLayer src)
		{
			foreach (var (dir, names) in src.Vdirs)
			{
				if (!Vdirs.TryGetValue(dir, out var existing))
					Vdirs[dir] = existing = new HashSet<string>(StringComparer.Ordinal);
				existing.UnionWith(names);
			}
		}

		public void PushBack(VfsLayer layer)
		{
			Layers.Add(layer);
			Rulesets.AddRange(layer.Rulesets);
			MergeVdirs(layer);
			foreach (var (path, frec) in layer.Resources)
				Resources[path] = frec;
		}

		public void PushFront(VfsLayer layer)
		{
			Layers.Insert(0, layer);
			Rulesets.InsertRange(0, layer.Rulesets);
			MergeVdirs(layer);
			foreach (var (path, frec) in layer.Resources)
				Resources.TryAdd(path, frec);
		}

		public FileRecord? At(string relpath) => Resources.GetValueOrDefault(Canonicalize(relpath));

		public HashSet<string> Ls(string relpath) => Vdirs.GetValueOrDefault(Canonicalize(relpath).TrimEnd('/')) ?? s_emptySet;

		public List<FileRecord?> GetSlice(string relpath)
		{
			string crelpath = Canonicalize(relpath);
			return Layers.Select(l => l.At(crelpath)).ToList();
		}
	}

	/// <summary>Each mod is also a (sub)set of layers.</summary>
	private sealed class ModRecord(string path)
	{
		public ModInfo ModInfo { get; } = new(path);
		public VfsLayerStack Stack { get; } = new();
	}

	private static readonly Dictionary<string, ModRecord> s_modsAvailable = new(StringComparer.Ordinal);
	private static readonly List<ZipArchive> s_zipContexts = [];
	private static readonly List<ModRecord> s_vfsMods = [];
	private static readonly VfsLayerStack s_vfsStack = new();
	private static readonly List<(string ModId, IReadOnlyList<FileRecord> Rulesets)> s_rsOrder = [];

	/// <summary>
	/// "standard" directory is for built-in mods only! otherwise automatic updates would delete user data.
	/// </summary>
	private static readonly HashSet<string> s_standardMods = new(StringComparer.Ordinal)
	{
		"Aliens_Pick_Up_Weapons", "Aliens_Pick_Up_Weapons_TFTD", "Demigod_Difficulty", "Demigod_Difficulty_TFTD",
		"Limit_Craft_Item_Capacities", "Limit_Craft_Item_Capacities_TFTD", "OpenXCom_Unlimited_Waypoints",
		"OpenXCom_Unlimited_Waypoints_TFTD", "PSX_Static_Cydonia_Map", "Smarter_Equip", "Smarter_Equip_TFTD",
		"StrategyCore_Swap_Small_USOs_TFTD", "TFTD_Damage", "UFOextender_Gun_Melee", "UFOextender_Gun_Melee_TFTD",
		"UFOextender_Psionic_Line_Of_Fire", "UFOextender_Psionic_Line_Of_Fire_TFTD", "UFOextender_Starting_Avalanches",
		"xcom1", "xcom2", "XcomUtil_Always_Daytime", "XcomUtil_Always_Daytime_TFTD", "XcomUtil_Always_Nighttime",
		"XcomUtil_Always_Nighttime_TFTD", "XcomUtil_Fighter_Transports", "XcomUtil_Fighter_Transports_TFTD",
		"XcomUtil_High_Explosive_Damage", "XcomUtil_High_Explosive_Damage_TFTD", "XcomUtil_Improved_Gauss",
		"XcomUtil_Improved_Ground_Tanks", "XcomUtil_Improved_Heavy_Laser", "XcomUtil_Infinite_Gauss",
		"XcomUtil_No_Psionics", "XcomUtil_No_Psionics_TFTD", "XcomUtil_Pistol_Auto_Shot", "XcomUtil_Pistol_Auto_Shot_TFTD",
		"XcomUtil_Skyranger_Weapon_Slot", "XcomUtil_Starting_Defensive_Base", "XcomUtil_Starting_Defensive_Base_TFTD",
		"XcomUtil_Starting_Defensive_Improved_Base", "XcomUtil_Starting_Defensive_Improved_Base_TFTD",
		"XcomUtil_Starting_Improved_Base", "XcomUtil_Starting_Improved_Base_TFTD", "XcomUtil_Statstrings",
		"XcomUtil_Statstrings_TFTD", "XcomUtil_Triton_Weapon_Slot", "XCOM_Damage",
	};

	/// <summary>ACOM's own built-in mods; allowed in the protected "standard" folder in addition to the original list.</summary>
	public static ISet<string> AdditionalStandardMods { get; } = new HashSet<string>(StringComparer.Ordinal);

	private static string ConcatPaths(string basePath, string relativePath)
	{
		if (basePath.Length == 0)
			throw new ArgumentException("Need correct basePath");
		if (relativePath.Length == 0)
			throw new ArgumentException("Need correct relativePath");
		return basePath[^1] == '/' ? basePath + relativePath : basePath + "/" + relativePath;
	}

	private static string ConcatOptionalPaths(string basePath, string relativePath)
	{
		if (basePath.Length == 0)
			return relativePath;
		if (relativePath.Length == 0)
			return basePath;
		return ConcatPaths(basePath, relativePath);
	}

	private static bool IsRuleset(string fname) => fname.EndsWith(".rul", StringComparison.OrdinalIgnoreCase);

	/// <summary>Recursively lists a directory: (relative folder, file name).</summary>
	private static void ListRecursive(string basePath, string relPath, List<(string Dir, string Name)> dlist)
	{
		string fullDir = ConcatOptionalPaths(basePath, relPath);
		foreach (var (name, isFolder, _) in CrossPlatform.GetFolderContents(fullDir))
		{
			if (isFolder)
				ListRecursive(basePath, ConcatOptionalPaths(relPath, name), dlist);
			else
				dlist.Add((relPath, name));
		}
	}

	private static ZipArchive? OpenZip(string logCtx, string zippath)
	{
		try
		{
			var zip = ZipFile.OpenRead(zippath);
			s_zipContexts.Add(zip);
			return zip;
		}
		catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException)
		{
			Logger.Warning(logCtx + "Ignoring zip: " + e.Message);
			return null;
		}
	}

	/// <summary>Lower-cases a path (all VFS lookups are case-insensitive).</summary>
	public static string Canonicalize(string fname) => fname.ToLowerInvariant();

	/// <summary>Ruleset files grouped by mod in load order; the highest-priority mod is last.</summary>
	public static IReadOnlyList<(string ModId, IReadOnlyList<FileRecord> Rulesets)> GetRulesets() => s_rsOrder;

	/// <summary>Clears the VFS and (unless <paramref name="clearOnly"/>) maps the common resources.</summary>
	public static void Clear(bool clearOnly, bool embeddedOnly)
	{
		s_rsOrder.Clear();
		s_vfsMods.Clear();
		s_vfsStack.Clear();
		s_modsAvailable.Clear();
		foreach (var zip in s_zipContexts)
			zip.Dispose();
		s_zipContexts.Clear();
		if (!clearOnly)
		{
			Logger.Verbose("FileMap::clear(): mapping 'common'");
			MapCommon(embeddedOnly);
		}
	}

	private static void MapCommon(bool embeddedOnly)
	{
		var mrec = new ModRecord("common");
		if (!MapExtResources(mrec, "common", embeddedOnly))
		{
			Logger.Error("VFS::map_common(): failed to map 'common'");
			return;
		}
		foreach (var layer in mrec.Stack.Layers)
			s_vfsStack.PushBack(layer);
	}

	/// <summary>Sets up the VFS for the given active mods (their masters are added automatically).</summary>
	public static void Setup(IReadOnlyList<ModInfo> active, bool embeddedOnly)
	{
		ArgumentNullException.ThrowIfNull(active);
		s_rsOrder.Clear();
		s_vfsMods.Clear();
		s_vfsStack.Clear();
		MapCommon(embeddedOnly);
		// expand the mod list into map order, listing all the mod dependencies
		var mapOrder = new List<string>();
		foreach (var modInfo in active)
		{
			string currentId = modInfo.Id;
			int insertBefore = mapOrder.Count;
			while (true)
			{
				mapOrder.Insert(insertBefore, currentId);
				string masterId = s_modsAvailable[currentId].ModInfo.Master;
				if (masterId.Length == 0)
					break;
				currentId = masterId;
			}
		}
		// map the mods skipping duplicates; this will preserve the order.
		var seen = new HashSet<string>(StringComparer.Ordinal);
		foreach (var id in mapOrder)
		{
			if (!seen.Add(id))
				continue;
			var mod = s_modsAvailable[id];
			s_vfsMods.Add(mod);
			foreach (var layer in mod.Stack.Layers)
				s_vfsStack.PushBack(layer);
			s_rsOrder.Add((mod.ModInfo.Id, mod.Stack.Rulesets.ToList()));
		}
	}

	/// <summary>Maps an external resource folder/zip (e.g. "common", "UFO") as layers in front of a mod.</summary>
	private static bool MapExtResources(ModRecord mrec, string basename, bool embeddedOnly)
	{
		_ = embeddedOnly; // no embedded assets in this port
		string logCtx = $"FileMap::mapExtResources({mrec.ModInfo.Id}, {basename}): ";
		bool mappedAnything = false;
		string zipname = basename + ".zip";

		// first try finding a directory (ass-backwards since we got to push this into front re layers)
		string fullname = Options.GetUserFolder() + basename;
		if (!CrossPlatform.FolderExists(fullname))
			fullname = CrossPlatform.SearchDataFolder(basename);
		if (CrossPlatform.FolderExists(fullname))
		{
			Logger.Verbose(logCtx + "found dir (" + fullname + ")");
			var layer = new VfsLayer(fullname);
			if (layer.MapPlainDir(fullname, true))
			{
				mrec.Stack.PushFront(layer);
				mappedAnything = true;
			}
		}
		else
		{
			Logger.Verbose(logCtx + "dir not found (" + fullname + ")");
		}

		// then try finding a zipfile
		fullname = Options.GetUserFolder() + zipname;
		if (!CrossPlatform.FileExists(fullname))
			fullname = CrossPlatform.SearchDataFile(zipname);
		if (CrossPlatform.FileExists(fullname))
		{
			Logger.Verbose(logCtx + "found zip (" + fullname + ")");
			var layer = new VfsLayer(fullname);
			bool mapped = layer.MapZipFile(fullname, basename + "/", true);
			if (!mapped)
			{
				// some garbage can stay in `layer` after failed mapping, clean up and try different path
				layer = new VfsLayer(fullname);
				mapped = layer.MapZipFile(fullname, "", true);
			}
			if (mapped)
			{
				mrec.Stack.PushFront(layer);
				mappedAnything = true;
			}
		}
		else
		{
			Logger.Verbose(logCtx + "zip not found (" + fullname + ")");
		}

		// now try the embedded zip: getEmbeddedAsset never finds one in this port
		Logger.Verbose(logCtx + "embedded asset not found (" + zipname + ")");

		if (!mappedAnything)
			Logger.Info(logCtx + "external resources not found.");
		return mappedAnything;
	}

	private static void MapZippedMod(ZipArchive zip, string zipfname, string prefix)
	{
		string logCtx = $"mapZippedMod({zipfname}, '{prefix}'): ";
		var layer = new VfsLayer(ConcatOptionalPaths(zipfname, prefix));
		if (!layer.MapZip(zip, zipfname, prefix))
		{
			Logger.Warning(logCtx + "Failed to map, skipping.");
			return;
		}
		RegisterMod(logCtx, layer, ConcatOptionalPaths(zipfname, prefix));
	}

	private static void RegisterMod(string logCtx, VfsLayer layer, string modpath)
	{
		var frec = layer.At("metadata.yml");
		if (frec == null)
		{
			Logger.Warning(logCtx + "No metadata.yml found, skipping.");
			return;
		}
		var reader = frec.GetYaml().ToBase();
		if (!reader.IsMap)
		{
			Logger.Warning(logCtx + "Bad metadata.yml found, skipping.");
			return;
		}
		var mrec = new ModRecord(modpath);
		mrec.ModInfo.Load(reader);
		if (s_modsAvailable.ContainsKey(mrec.ModInfo.Id))
		{
			Logger.Error(logCtx + "modId " + mrec.ModInfo.Id + " already mapped in, skipping " + modpath);
			return;
		}
		Logger.Verbose(logCtx + $"mapped mod '{mrec.ModInfo.Id}' from {modpath} master={mrec.ModInfo.Master} version={mrec.ModInfo.Version}");
		mrec.Stack.PushBack(layer);
		s_modsAvailable[mrec.ModInfo.Id] = mrec;
	}

	/// <summary>Scans a zip for mods: either one mod (metadata.yml at the top) or one mod per top-level folder.</summary>
	public static void ScanModZip(string fullpath)
	{
		string logCtx = $"scanModZip({fullpath}): ";
		var zip = OpenZip(logCtx, fullpath);
		if (zip == null)
			return;
		// check if this is maybe a zip of a single mod (metadata.yml at the top level)
		if (zip.GetEntry("metadata.yml") != null)
		{
			Logger.Verbose(logCtx + "retrying as a single-mod .zip");
			MapZippedMod(zip, fullpath, "");
			return;
		}
		// one mod per top-level folder; folders are derived from the entry names because not every
		// zip tool stores directory entries
		var prefixes = zip.Entries
			.Select(e => e.FullName.Replace('\\', '/'))
			.Where(n => n.IndexOf('/', StringComparison.Ordinal) > 0)
			.Select(n => n[..(n.IndexOf('/', StringComparison.Ordinal) + 1)])
			.Distinct(StringComparer.Ordinal)
			.Order(StringComparer.OrdinalIgnoreCase);
		foreach (var prefix in prefixes)
			MapZippedMod(zip, fullpath, prefix);
	}

	/// <summary>Scans "<paramref name="dirname"/><paramref name="basename"/>[.zip]" for mod folders and mod zips.</summary>
	public static void ScanModDir(string dirname, string basename, bool protectedLocation)
	{
		string logCtx = $"scanModDir('{dirname}', '{basename}'): ";
		// first check for a .zip
		string fullname = dirname + basename + ".zip";
		if (CrossPlatform.FileExists(fullname))
		{
			Logger.Verbose(logCtx + "scanning zip " + fullname);
			ScanModZip(fullname);
		}
		// then check for a dir
		fullname = dirname + basename;
		if (!CrossPlatform.FolderExists(fullname))
		{
			Logger.Verbose(logCtx + "no dir " + fullname);
			return;
		}
		Logger.Verbose(logCtx + "scanning dir " + fullname);
		// now this dir can contain both moddirs and modzips; first scan for modzips: anything but a directory
		var dirlist = new List<string>();
		foreach (var (name, isDir, _) in CrossPlatform.GetFolderContents(fullname))
		{
			if (isDir)
			{
				if (protectedLocation && !s_standardMods.Contains(name) && !AdditionalStandardMods.Contains(name))
				{
					Logger.Error($"Invalid standard mod '{name}', skipping.");
					continue;
				}
				dirlist.Add(name); // stash for later
				continue;
			}
			if (protectedLocation)
			{
				Logger.Error($"Invalid standard mod '{name}', skipping.");
				continue;
			}
			ScanModZip(ConcatPaths(fullname, name));
		}
		foreach (var name in dirlist)
		{
			string modpath = ConcatPaths(fullname, name);
			// map dat dir! (if it has metadata.yml, naturally)
			var layer = new VfsLayer(modpath);
			if (!layer.MapPlainDir(modpath))
			{
				Logger.Warning(logCtx + "Can't scan " + name + ", skipping.");
				continue;
			}
			RegisterMod(logCtx, layer, modpath);
		}
	}

	private static void DropMods(string logCtx, HashSet<string> dropList)
	{
		bool droppedSomething = true;
		while (droppedSomething)
		{
			droppedSomething = false;
			foreach (var (modId, mrec) in s_modsAvailable)
			{
				if (dropList.Contains(modId))
					continue;
				string masterId = mrec.ModInfo.Master;
				if (dropList.Contains(masterId))
				{
					Logger.Debug(logCtx + "Dropping mod " + modId + ": depends " + masterId);
					dropList.Add(modId);
					droppedSomething = true;
				}
			}
		}
		foreach (var id in dropList)
			s_modsAvailable.Remove(id);
		dropList.Clear();
	}

	/// <summary>
	/// Removes mods that lack their external resources, depend on missing mods or participate in
	/// dependency loops.
	/// </summary>
	public static void CheckModsDependencies()
	{
		const string logCtx = "checkModsDependencies(): ";
		var dropList = new HashSet<string>(StringComparer.Ordinal);
		// break circular dependency loops.
		foreach (var startedAt in s_modsAvailable.Keys)
		{
			string current = startedAt;
			while (true)
			{
				string masterId = s_modsAvailable[current].ModInfo.Master;
				if (masterId.Length == 0)
					break; // no loop here, move along.
				if (dropList.Contains(masterId))
					break; // this loop is already broken
				if (!s_modsAvailable.ContainsKey(masterId))
				{
					Logger.Warning(logCtx + current + ": missing master mod " + masterId);
					dropList.Add(startedAt);
					break;
				}
				if (masterId == startedAt)
				{
					Logger.Warning(logCtx + current + ": dependency loop detected for " + masterId);
					dropList.Add(startedAt);
					break;
				}
				current = masterId;
			}
		}
		// drop mods involved and anything that depends on them
		DropMods(logCtx, dropList);
		// now map extResources for all the remaining mods and drop any mod where this fails.
		foreach (var (modId, mrec) in s_modsAvailable)
		{
			foreach (var dir in mrec.ModInfo.ExternalResourceDirs)
			{
				if (!MapExtResources(mrec, dir, false))
				{
					Logger.Debug(logCtx + "dropping mod " + modId + ": extResources '" + dir + "' not found.");
					dropList.Add(modId);
					break;
				}
			}
		}
		DropMods(logCtx, dropList);
	}

	/// <summary>All mods that can be loaded, by id.</summary>
	public static SortedDictionary<string, ModInfo> GetModInfos()
	{
		var rv = new SortedDictionary<string, ModInfo>(StringComparer.Ordinal);
		foreach (var (id, mrec) in s_modsAvailable)
			rv[id] = mrec.ModInfo;
		return rv;
	}

	/// <summary>Finds a ruleset file of a mod by its path relative to the mod folder.</summary>
	public static FileRecord? GetModRuleFile(ModInfo modInfo, string relpath)
	{
		ArgumentNullException.ThrowIfNull(modInfo);
		if (string.IsNullOrEmpty(relpath))
			return null;
		string fullPath = ConcatPaths(modInfo.Path, relpath);
		var found = s_modsAvailable[modInfo.Id].Stack.Rulesets.FirstOrDefault(r => r.FullPath == fullPath);
		if (found == null)
			Logger.Warning($"mod {modInfo.Id}: unknown rulefile '{relpath}'.");
		return found;
	}

	/// <summary>Is the file present in the VFS?</summary>
	public static bool FileExists(string relativeFilePath) => s_vfsStack.At(relativeFilePath) != null;

	/// <summary>Gets a file of the VFS.</summary>
	/// <exception cref="FileNotFoundException">The file is not mapped.</exception>
	public static FileRecord At(string relativeFilePath)
	{
		var frec = s_vfsStack.At(relativeFilePath);
		if (frec == null)
		{
			string fail = "FileRecord::at(" + relativeFilePath + "): requested file not found.";
			Logger.Fatal(fail);
			throw new FileNotFoundException(fail, relativeFilePath);
		}
		return frec;
	}

	/// <summary>Opens a VFS file (C++ getRWops/getIStream).</summary>
	public static Stream GetStream(string relativeFilePath) => At(relativeFilePath).OpenRead();

	/// <summary>Reads a whole VFS file (C++ getRWopsReadAll).</summary>
	public static byte[] ReadAll(string relativeFilePath) => At(relativeFilePath).ReadAll();

	public static YamlRootNodeReader GetYaml(string relativeFilePath) => At(relativeFilePath).GetYaml();

	/// <summary>A 'vertical slice' through all VFS layers for one file (null where a layer lacks it); used for languages.</summary>
	public static IReadOnlyList<FileRecord?> GetSlice(string relativeFilePath) => s_vfsStack.GetSlice(relativeFilePath);

	/// <summary>Files in a virtual folder over all active layers (rulesets excluded), lower-case names.</summary>
	public static IReadOnlySet<string> GetVFolderContents(string relativePath) => s_vfsStack.Ls(relativePath);

	/// <summary>Files in a virtual folder of one layer (0 = the bottommost, i.e. common).</summary>
	public static IReadOnlySet<string> GetVFolderContents(string relativePath, int level)
	{
		if (s_vfsStack.Layers.Count == 0)
			return s_emptySet;
		if (level >= s_vfsStack.Layers.Count)
			level = s_vfsStack.Layers.Count - 1;
		return s_vfsStack.Layers[level].Ls(relativePath);
	}

	/// <summary>The subset of the files that have the given extension (case-insensitive, without dot).</summary>
	public static HashSet<string> FilterFiles(IEnumerable<string> files, string ext)
	{
		ArgumentNullException.ThrowIfNull(files);
		var ret = new HashSet<string>(StringComparer.Ordinal);
		int extLen = ext.Length + 1; // +1 for the '.'
		string canonicalExt = Canonicalize(ext);
		foreach (var f in files)
		{
			// less-than since we should have at least one character that is not part of the extension
			if (extLen < f.Length && Canonicalize(f[^(extLen - 1)..]) == canonicalExt)
				ret.Add(f);
		}
		return ret;
	}

	/// <summary>Reads one file from a zip (case-insensitive name), or null.</summary>
	public static byte[]? ZipGetFileByName(string zipfile, string fullpath)
	{
		string sanpath = Canonicalize(fullpath.Replace('\\', '/'));
		try
		{
			using var zip = ZipFile.OpenRead(zipfile);
			var entry = zip.Entries.FirstOrDefault(e => !e.FullName.EndsWith('/') && Canonicalize(e.FullName.Replace('\\', '/')) == sanpath);
			if (entry == null)
			{
				Logger.Error($"zipGetFileByName({zipfile}, {fullpath}): File not found in the .zip");
				return null;
			}
			using var s = entry.Open();
			using var buffer = new MemoryStream();
			s.CopyTo(buffer);
			return buffer.ToArray();
		}
		catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException)
		{
			Logger.Error($"zipGetFileByName({zipfile}, {fullpath}): {e.Message}");
			return null;
		}
	}

	/// <summary>Human readable dump of the active VFS (for verbose logging).</summary>
	public static string Dump(bool verbose)
	{
		var sb = new StringBuilder();
		for (int i = 0; i < s_vfsStack.Layers.Count; ++i)
		{
			var layer = s_vfsStack.Layers[i];
			sb.Append($"layer {i}: origin: {layer.FullPath}; {layer.Resources.Count} resources, {layer.Rulesets.Count} rulesets\n");
			if (verbose)
			{
				foreach (var (path, frec) in layer.Resources.OrderBy(p => p.Key, StringComparer.Ordinal))
					sb.Append($"    {path} -> {frec.FullPath}\n");
			}
		}
		return sb.ToString();
	}
}
