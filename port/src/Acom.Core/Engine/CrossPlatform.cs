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
// Ported from: src/Engine/CrossPlatform.h, src/Engine/CrossPlatform.cpp
// Port status: partial (command line, folder discovery, file/path helpers, versions; missing: screenshots, embedded assets,
// save headers, error dialogs, path conversions).
// Deviation: folder listings are sorted case-insensitively (like NTFS); the C++ code uses the unspecified readdir order.
// Deviation: ACOM uses its own folder names ("acom"/"ACOM") instead of "openxcom"/"OpenXcom",
// so it never touches an existing OpenXcom installation.

namespace Acom.Engine;

/// <summary>Platform-specific helpers: command line, standard folders and simple file access.</summary>
public static class CrossPlatform
{
	/// <summary>Folder name below lower-case Unix locations (XDG).</summary>
	public const string AppFolderLower = "acom";

	/// <summary>Folder name below Windows Documents and macOS Application Support.</summary>
	public const string AppFolderMixed = "ACOM";

	private static string[] s_args = ["acom"];

	/// <summary>Command line including the program name at index 0 (like argv).</summary>
	public static IReadOnlyList<string> GetArgs() => s_args;

	/// <summary>Sets the command line arguments (without program name, as passed to Main).</summary>
	public static void SetArgs(IEnumerable<string> args) => s_args = ["acom", .. args];

	private static string Slash(string path) => path.Replace('\\', '/').TrimEnd('/') + "/";

	private static string Home() => Environment.GetEnvironmentVariable("HOME") is { Length: > 0 } home
		? home
		: Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

	/// <summary>Candidate data folders, most specific first.</summary>
	public static List<string> FindDataFolders()
	{
		var list = new List<string>();
		if (OperatingSystem.IsWindows())
		{
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			void Add(string p)
			{
				if (seen.Add(p))
					list.Add(p);
			}
			Add(Slash(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), AppFolderMixed)));
			Add(Slash(AppContext.BaseDirectory));
			Add(Slash(Directory.GetCurrentDirectory()));
			return list;
		}

		// Get user-specific data folders
		string? xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
		if (!string.IsNullOrEmpty(xdgDataHome))
			list.Add($"{xdgDataHome}/{AppFolderLower}/");
		else if (OperatingSystem.IsMacOS())
			list.Add($"{Home()}/Library/Application Support/{AppFolderMixed}/");
		else
			list.Add($"{Home()}/.local/share/{AppFolderLower}/");

		// Get global data folders
		string? xdgDataDirs = Environment.GetEnvironmentVariable("XDG_DATA_DIRS");
		if (!string.IsNullOrEmpty(xdgDataDirs))
		{
			foreach (var dir in xdgDataDirs.Split(':', StringSplitOptions.RemoveEmptyEntries))
				list.Add($"{dir}/{AppFolderLower}/");
		}
		else if (OperatingSystem.IsMacOS())
		{
			list.Add($"/Users/Shared/{AppFolderMixed}/");
		}
		else
		{
			list.Add($"/usr/local/share/{AppFolderLower}/");
			list.Add($"/usr/share/{AppFolderLower}/");
		}

		// Executable folder and relative FHS location
		string exeDir = Slash(AppContext.BaseDirectory);
		list.Add(exeDir);
		if (OperatingSystem.IsLinux())
			list.Add($"{exeDir}../share/{AppFolderLower}/");

		// Get working directory
		list.Add("./");
		return list;
	}

	/// <summary>Candidate user folders (saves, mods, screenshots), most preferred first.</summary>
	public static List<string> FindUserFolders()
	{
		var list = new List<string>();
		if (OperatingSystem.IsWindows())
		{
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			void Add(string p)
			{
				if (seen.Add(p))
					list.Add(p);
			}
			Add(Slash(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), AppFolderMixed)));
			Add(Slash(Path.Combine(AppContext.BaseDirectory, "user")));
			Add(Slash(Path.Combine(Directory.GetCurrentDirectory(), "user")));
			return list;
		}
		string? xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
		if (xdgDataHome != null)
			list.Add($"{xdgDataHome}/{AppFolderLower}/");
		else if (OperatingSystem.IsMacOS())
			list.Add($"{Home()}/Library/Application Support/{AppFolderMixed}/");
		else
			list.Add($"{Home()}/.local/share/{AppFolderLower}/");
		// Get old-style folder
		list.Add($"{Home()}/.{AppFolderLower}/");
		// Get working directory
		list.Add("./user/");
		return list;
	}

	/// <summary>Config folder; empty on Windows/macOS (the user folder is used).</summary>
	public static string FindConfigFolder()
	{
		if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
			return string.Empty;
		string? xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
		return xdgConfigHome != null ? $"{xdgConfigHome}/{AppFolderLower}/" : $"{Home()}/.config/{AppFolderLower}/";
	}

	/// <summary>Searches the data folders for a file; returns the name unchanged if not found.</summary>
	public static string SearchDataFile(string filename)
	{
		string path = Options.GetDataFolder() + filename;
		if (FileExists(path))
			return path;
		foreach (var dataPath in Options.GetDataList())
		{
			path = dataPath + filename;
			if (FileExists(path))
				return path;
		}
		return filename;
	}

	/// <summary>Searches the data folders for a folder with at least a minimum number of entries.</summary>
	public static string SearchDataFolder(string foldername, int size = 0)
	{
		// Set minimum number of entries
		int minNumOfElementsInFolder = foldername switch
		{
			"TFTD" or "UFO" => 9, // At least 9 directories with original data
			"common" => 6,        // Language/, Palettes/, Resources/, Shaders/, SoldierName/, openxcom.png
			"standard" => 20,     // some buffer if some mods are dropped
			_ => size,
		};
		string path;
		if (Options.GetDataFolder().Length > 0)
		{
			path = Options.GetDataFolder() + foldername;
			if (FolderMinSize(path, minNumOfElementsInFolder))
				return path;
		}
		foreach (var dataPath in Options.GetDataList())
		{
			path = dataPath + foldername;
			if (FolderMinSize(path, minNumOfElementsInFolder))
				return path;
		}
		return foldername;
	}

	public static bool FileExists(string path) => File.Exists(path);

	public static bool FolderExists(string path) => Directory.Exists(path);

	/// <summary>Creates a folder; false if it already exists or can't be created (like mkdir).</summary>
	public static bool CreateFolder(string path)
	{
		if (Directory.Exists(path))
			return false;
		try
		{
			Directory.CreateDirectory(path);
			return true;
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			return false;
		}
	}

	/// <summary>Does a folder exist with at least <paramref name="size"/> entries?</summary>
	public static bool FolderMinSize(string path, int size)
	{
		if (!FolderExists(path))
			return false;
		if (size == 0)
			return true;
		return Directory.EnumerateFileSystemEntries(path).Take(size).Count() >= size;
	}

	/// <summary>Entries of a folder (no dot files), optionally filtered by extension; sorted case-insensitively.</summary>
	public static List<(string Name, bool IsFolder, DateTime Modified)> GetFolderContents(string path, string ext = "")
	{
		if (!Directory.Exists(path))
			throw new IOException("Failed to open directory: " + path);
		var files = new List<(string Name, bool IsFolder, DateTime Modified)>();
		foreach (var entry in new DirectoryInfo(path).EnumerateFileSystemInfos())
		{
			string filename = entry.Name;
			// skip ".", "..", ".git", ".svn", ...
			if (filename.StartsWith('.') || !CompareExt(filename, ext))
				continue;
			files.Add((filename, entry is DirectoryInfo, entry.LastWriteTimeUtc));
		}
		files.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase) is var c && c != 0 ? c : string.CompareOrdinal(a.Name, b.Name));
		return files;
	}

	/// <summary>File name without the folders ("a/b/c.txt" → "c.txt"; a trailing slash is ignored).</summary>
	public static string BaseFilename(string path)
	{
		int sep = path.LastIndexOf('/');
		if (sep < 0)
			return path;
		if (sep == path.Length - 1)
			return BaseFilename(path[..^1]);
		return path[(sep + 1)..];
	}

	/// <summary>Folder part of a path including the trailing slash ("a/b/c.txt" → "a/b/").</summary>
	public static string DirFilename(string path)
	{
		int sep = path.LastIndexOf('/');
		if (sep < 0)
			return string.Empty;
		if (sep == path.Length - 1)
			return DirFilename(path[..^1]);
		return path[..(sep + 1)];
	}

	/// <summary>File name without the last extension.</summary>
	public static string NoExt(string filename)
	{
		int dot = filename.LastIndexOf('.');
		return dot < 0 ? filename : filename[..dot];
	}

	/// <summary>Last extension including the dot, or empty.</summary>
	public static string GetExt(string filename)
	{
		int dot = filename.LastIndexOf('.');
		return dot < 0 ? string.Empty : filename[dot..];
	}

	/// <summary>Case-insensitive extension check (without dot); an empty extension matches everything.</summary>
	public static bool CompareExt(string filename, string extension)
	{
		if (extension.Length == 0)
			return true;
		int j = filename.Length - extension.Length;
		if (j <= 0 || filename[j - 1] != '.')
			return false;
		return string.Compare(filename, j, extension, 0, extension.Length, StringComparison.OrdinalIgnoreCase) == 0;
	}

	/// <summary>Parses "a.b.c.d" into four numbers (missing or invalid parts are 0, like std::stoi).</summary>
	public static int[] ParseVersion(string newVersion)
	{
		var version = new int[4];
		var parts = (newVersion ?? string.Empty).Split('.');
		for (int j = 0; j < parts.Length && j < version.Length; ++j)
		{
			var s = parts[j].AsSpan().TrimStart();
			int len = 0;
			if (len < s.Length && (s[len] == '-' || s[len] == '+'))
				len++;
			while (len < s.Length && char.IsAsciiDigit(s[len]))
				len++;
			if (int.TryParse(s[..len], System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out int i))
				version[j] = i;
		}
		return version;
	}

	/// <summary>Is <paramref name="newVersion"/> higher than <paramref name="ver"/> (component-wise from the left)?</summary>
	public static bool IsHigherThanCurrentVersion(IReadOnlyList<int> newVersion, IReadOnlyList<int> ver)
	{
		for (int k = 0; k < ver.Count; ++k)
		{
			if (newVersion[k] > ver[k])
				return true;
			if (newVersion[k] < ver[k])
				break;
		}
		return false;
	}

	/// <summary>Is the given version higher than the OXCE version this port is based on?</summary>
	public static bool IsHigherThanCurrentVersion(string newVersion) => IsHigherThanCurrentVersion(ParseVersion(newVersion), Acom.Version.OPENXCOM_VERSION_NUMBER);

	/// <summary>Reads a whole file.</summary>
	public static byte[] ReadFileRaw(string filename) => File.ReadAllBytes(filename);

	/// <summary>Writes a text file (UTF-8 without BOM).</summary>
	public static bool WriteFile(string filename, string data)
	{
		try
		{
			File.WriteAllText(filename, data);
			return true;
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			Logger.Error($"Failed to write {filename}: {e.Message}");
			return false;
		}
	}
}
