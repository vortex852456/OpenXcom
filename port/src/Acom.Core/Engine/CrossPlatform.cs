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
// Not ported: getEmbeddedAsset (no embedded assets, see the deviation table in docs/architecture/csharp-port.md),
// setWindowIcon (the host owns the window), testInternetConnection/downloadFile/startUpdateProcess (only used by the
// dropped oxceUpdateCheck), processArgs (SetArgs; .NET already passes UTF-16 arguments). flashWindow is a no-op.
// Deviation: crashDump logs the .NET exception and stack trace instead of writing a Windows minidump / native backtrace.
// Deviation: folder listings are sorted case-insensitively (like NTFS); the C++ code uses the unspecified readdir order.
// Deviation: ACOM uses its own folder names ("acom"/"ACOM") instead of "openxcom"/"OpenXcom",
// so it never touches an existing OpenXcom installation.

namespace Acom;

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

	/// <summary>Removes a file from the specified path; false if it doesn't exist or can't be removed (like remove()).</summary>
	public static bool DeleteFile(string path)
	{
		if (!File.Exists(path))
			return false;
		try
		{
			File.Delete(path);
			return true;
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			return false;
		}
	}

	/// <summary>Replaces invalid filesystem characters with _.</summary>
	public static string SanitizeFilename(string filename)
	{
		ArgumentNullException.ThrowIfNull(filename);
		var newFilename = filename.ToCharArray();
		for (int i = 0; i < newFilename.Length; ++i)
		{
			char c = newFilename[i];
			if (c == '<' || c == '>' || c == ':' || c == '*' || c == '|' || c == '"' || c == '\'' || c == '/' || c == '?' || c == '\0' || c == '\\')
				newFilename[i] = '_';
		}
		return new string(newFilename);
	}

	/// <summary>Moves a file from one path to another, replacing any existing file.</summary>
	/// <returns>True if the operation succeeded, False otherwise.</returns>
	public static bool MoveFile(string src, string dest)
	{
		try
		{
			File.Move(src, dest, true);
			return true;
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			return false;
		}
	}

	/// <summary>Notifies the user that maybe he should have a look (C++ only flashes the window on Windows; no-op here).</summary>
	public static void FlashWindow()
	{
	}

	/// <summary>Generates a timestamp of the current (local) time in D-M-Y_H-M-S format.</summary>
	public static string Now() => DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss", System.Globalization.CultureInfo.InvariantCulture);

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

	/// <summary>Is <paramref name="dataVersion"/> lower than <paramref name="ver"/> (component-wise from the left)?</summary>
	public static bool IsLowerThanRequiredVersion(IReadOnlyList<int> dataVersion, IReadOnlyList<int> ver)
	{
		ArgumentNullException.ThrowIfNull(dataVersion);
		ArgumentNullException.ThrowIfNull(ver);
		bool isLower = false;
		for (int k = 0; k < ver.Count; ++k)
		{
			if (dataVersion[k] < ver[k])
			{
				isLower = true;
				break;
			}
			else if (dataVersion[k] > ver[k])
			{
				break;
			}
		}
		return isLower;
	}

	/// <summary>Is the version of the data (common/standard folders) lower than the minimum required ruleset version?</summary>
	public static bool IsLowerThanRequiredVersion(string dataVersion) => IsLowerThanRequiredVersion(ParseVersion(dataVersion), Acom.Version.MIN_REQUIRED_RULESET_VERSION_NUMBER);

	/// <summary>Fully reads a file and returns a stream over its bytes.</summary>
	public static Stream ReadFile(string filename) => new MemoryStream(ReadFileRaw(filename), writable: false);

	/// <summary>Fully reads a file.</summary>
	/// <exception cref="IOException">The file can't be read (the error is logged too).</exception>
	public static byte[] ReadFileRaw(string filename)
	{
		try
		{
			return File.ReadAllBytes(filename);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			string err = $"Failed to read {filename}: {e.Message}";
			Logger.Error(err);
			throw new IOException(err, e);
		}
	}

	/// <summary>Gets a stream over a file's bytes at least up to and including the first "\n---" sequence (savegames only).</summary>
	public static Stream GetYamlSaveHeader(string filename) => new MemoryStream(GetYamlSaveHeaderRaw(filename), writable: false);

	/// <summary>
	/// Reads a file in 4096 byte chunks until the data contains the first "\n---" sequence or the file ends
	/// (savegames only, so the load/save lists don't parse the whole game).
	/// </summary>
	/// <returns>All bytes read so far, which may extend past the separator.</returns>
	/// <exception cref="IOException">The file can't be read (the error is logged too).</exception>
	public static byte[] GetYamlSaveHeaderRaw(string filename)
	{
		ReadOnlySpan<byte> separator = "\n---"u8;
		const int chunksize = 4096;
		try
		{
			using var stream = File.OpenRead(filename);
			byte[] data = new byte[chunksize];
			int size = 0;
			int offs = 0;
			while (true)
			{
				int actuallyRead = stream.Read(data, offs, chunksize);
				if (actuallyRead == 0)
				{
					break;
				}
				size += actuallyRead;
				// C++ strstr stops at a NUL byte; savegames are text, so the plain search is the same
				int searchFrom = offs > 4 ? offs - 4 : 0;
				if (data.AsSpan(searchFrom, size - searchFrom).IndexOf(separator) >= 0)
				{
					break;
				}
				Array.Resize(ref data, size + chunksize);
				offs = size;
			}
			return data[..size];
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			string err = $"Failed to read {filename}: {e.Message}";
			Logger.Error(err);
			throw new IOException(err, e);
		}
	}

	/// <summary>Writes a text file (UTF-8 without BOM).</summary>
	/// <returns>If we did write it.</returns>
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

	/// <summary>Writes a binary file.</summary>
	/// <returns>If we did write it.</returns>
	public static bool WriteFile(string filename, ReadOnlySpan<byte> data)
	{
		try
		{
			using var stream = File.Create(filename);
			stream.Write(data);
			return true;
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			Logger.Error($"Failed to write {filename}: {e.Message}");
			return false;
		}
	}

	/// <summary>Copies a file, replacing any existing file (like C++, only on Windows; elsewhere it returns false).</summary>
	/// <returns>True if the operation succeeded, False otherwise.</returns>
	public static bool CopyFile(string src, string dest)
	{
		if (!OperatingSystem.IsWindows())
			return false;
		try
		{
			File.Copy(src, dest, true);
			return true;
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			return false;
		}
	}

	/// <summary>Adds an ending slash to a path if necessary.</summary>
	public static string ConvertPath(string path)
	{
		ArgumentNullException.ThrowIfNull(path);
		if (path.Length != 0 && path[^1] != '/')
			return path + '/';
		return path;
	}

	/// <summary>Gets the last modified date of a file as a Unix timestamp (0 if it can't be read).</summary>
	public static long GetDateModified(string path)
	{
		if (!File.Exists(path))
			return 0;
		return new DateTimeOffset(File.GetLastWriteTimeUtc(path)).ToUnixTimeSeconds();
	}

	/// <summary>Converts a Unix timestamp into local date and time strings using ISO 8601 ("%Y-%m-%d", "%H:%M").</summary>
	public static (string first, string second) TimeToString(long time)
	{
		var local = DateTimeOffset.FromUnixTimeSeconds(time).ToLocalTime();
		return (local.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), local.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture));
	}

	/// <summary>Gets the user's locale as "language-COUNTRY" (e.g. "de-DE").</summary>
	/// <remarks>
	/// Deviation: the port runs with invariant globalization, so the locale comes from the POSIX
	/// variables (LC_ALL, LC_MESSAGES, LANG) on every platform, giving what the C++ Windows branch returns.
	/// The C++ Linux branch cuts one character too many ("de_DE.UTF-8" → "d-e_D") and never finds a language.
	/// </remarks>
	public static string GetLocale()
	{
		string? name = null;
		foreach (var variable in new[] { "LC_ALL", "LC_MESSAGES", "LANG" })
		{
			name = Environment.GetEnvironmentVariable(variable);
			if (!string.IsNullOrEmpty(name))
				break;
		}
		if (string.IsNullOrEmpty(name))
			return "x-";
		int dot = name.IndexOfAny(['.', '@']);
		if (dot >= 0)
			name = name[..dot];
		int dash = name.IndexOf('_', StringComparison.Ordinal);
		if (dash >= 0)
			return name[..dash] + "-" + name[(dash + 1)..];
		return name + "-";
	}

	/// <summary>Is the event the platform's quit shortcut (Alt+F4 on Windows, Command+Q on macOS)?</summary>
	public static bool IsQuitShortcut(SDL_Event ev)
	{
		ArgumentNullException.ThrowIfNull(ev);
		if (OperatingSystem.IsWindows())
			return ev.type == SDLEventType.SDL_KEYDOWN && ev.key.keysym.sym == SDLKey.SDLK_F4 && (ev.key.keysym.mod & SDLMod.KMOD_ALT) != 0;
		if (OperatingSystem.IsMacOS())
			return ev.type == SDLEventType.SDL_KEYDOWN && ev.key.keysym.sym == SDLKey.SDLK_q && (ev.key.keysym.mod & SDLMod.KMOD_LMETA) != 0;
		//TODO add other OSs shortcuts.
		return false;
	}

	/// <summary>Gets the DOS-style path shown on the start screen (the C++ Windows build shortens the exe path).</summary>
	public static string GetDosPath() => "C:\\GAMES\\ACOM";

	/// <summary>Gets the folder of the executable, with a trailing slash (C++ returns it only on Windows; the port searches it for data on every platform).</summary>
	public static string GetExeFolder() => Slash(AppContext.BaseDirectory);

	/// <summary>
	/// Opens a folder or URL in the system's file browser (C++ openExplorer: ShellExecute/open/xdg-open).
	/// .NET picks the same handler per platform through the shell.
	/// </summary>
	/// <returns>True if the handler was started.</returns>
	public static bool OpenExplorer(string url)
	{
		try
		{
			// null means an existing process handled it, which is a success too
			System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true })?.Dispose();
			return true;
		}
		catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException or PlatformNotSupportedException)
		{
			return false;
		}
	}

	/// <summary>
	/// Gets the file name of the executable (C++ only knows it on Windows and returns "" elsewhere).
	/// </summary>
	/// <param name="includingPath">Including full path or just the file name?</param>
	public static string GetExeFilename(bool includingPath)
	{
		if (OperatingSystem.IsWindows() && Environment.ProcessPath is { } path)
			return includingPath ? path.Replace('\\', '/') : Path.GetFileName(path);
		return string.Empty;
	}

	private static string s_errorDlg = string.Empty;

	/// <summary>
	/// Shows an error message box where the C++ code calls MessageBoxW (Windows). Set by the host, which has a
	/// windowing library; without it the error goes to standard error.
	/// </summary>
	public static Action<string>? ErrorDialogHandler { get; set; }

	/// <summary>Determines the available Linux error dialogs (kdialog, zenity, gdialog, xdialog).</summary>
	public static void GetErrorDialog()
	{
		if (OperatingSystem.IsWindows())
			return;
		if (Environment.GetEnvironmentVariable("KDE_SESSION_UID") != null && RunShell("which kdialog > /dev/null 2>&1") == 0)
			s_errorDlg = "kdialog --error ";
		else if (RunShell("which zenity > /dev/null 2>&1") == 0)
			s_errorDlg = "zenity --no-wrap --error --text=";
		else if (RunShell("which kdialog > /dev/null 2>&1") == 0)
			s_errorDlg = "kdialog --error ";
		else if (RunShell("which gdialog > /dev/null 2>&1") == 0)
			s_errorDlg = "gdialog --msgbox ";
		else if (RunShell("which xdialog > /dev/null 2>&1") == 0)
			s_errorDlg = "xdialog --msgbox ";
	}

	/// <summary>Displays a message box with an error message and logs it as fatal.</summary>
	public static void ShowError(string error)
	{
		ArgumentNullException.ThrowIfNull(error);
		if (OperatingSystem.IsWindows())
		{
			if (ErrorDialogHandler != null)
				ErrorDialogHandler(error);
			else
				Console.Error.WriteLine(error);
		}
		else if (s_errorDlg.Length == 0)
		{
			Console.Error.WriteLine(error);
		}
		else
		{
			string nError = '"' + error + '"';
			nError = nError.Replace("\n", "\\n", StringComparison.Ordinal);
			string cmd = s_errorDlg + nError;
			if (RunShell(cmd) != 0)
				Console.Error.WriteLine(error);
		}
		Logger.Fatal(error);
	}

	/// <summary>Runs a command through /bin/sh like C <c>system()</c>; -1 if the shell can't be started.</summary>
	private static int RunShell(string cmd)
	{
		try
		{
			var info = new System.Diagnostics.ProcessStartInfo("/bin/sh") { UseShellExecute = false };
			info.ArgumentList.Add("-c");
			info.ArgumentList.Add(cmd);
			using var process = System.Diagnostics.Process.Start(info);
			if (process == null)
				return -1;
			process.WaitForExit();
			return process.ExitCode;
		}
		catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException or PlatformNotSupportedException)
		{
			return -1;
		}
	}

	/// <summary>Logs the stack back trace (of the exception, or leading up to this call if null).</summary>
	public static void StackTrace(Exception? ex)
	{
		string trace = ex?.StackTrace ?? Environment.StackTrace;
		foreach (var line in trace.Split('\n'))
		{
			string frame = line.TrimEnd('\r');
			if (frame.Length != 0)
				Logger.Fatal(frame);
		}
	}

	/// <summary>Logs the details of this crash and shows an error.</summary>
	/// <param name="ex">The unhandled exception, if any.</param>
	/// <param name="err">Exception message, if any (used when <paramref name="ex"/> is null).</param>
	public static void CrashDump(Exception? ex, string err)
	{
		string error = ex == null ? err : $"{ex.GetType().FullName}: {ex.Message}";
		Logger.Fatal($"A fatal error has occurred: {error}");
		StackTrace(ex);
		var msg = new System.Text.StringBuilder();
		// Deviation: names ACOM instead of OpenXcom and its OXCE forum board
		msg.Append("ACOM has crashed: ").Append(error).Append('\n');
		msg.Append("Log file: ").Append(GetLogFileName()).Append('\n');
		msg.Append("If this error was unexpected, please report it to the ACOM project.").Append('\n');
		msg.Append("The following can help us solve the problem:").Append('\n');
		msg.Append("1. a saved game from just before the crash (helps 98%)").Append('\n');
		msg.Append("2. a detailed description how to reproduce the crash (helps 80%)").Append('\n');
		msg.Append("3. a log file (helps 10%)").Append('\n');
		msg.Append("4. a screenshot of this error message (helps 5%)");
		ShowError(msg.ToString());
	}

	private const int LOG_BUFFER_LIMIT = 1 << 10;
	private static readonly LinkedList<(SeverityLevel level, string msg)> s_logBuffer = new();
	private static readonly System.Threading.Lock s_logLock = new();
	private static string s_logFileName = string.Empty;

	/// <summary>Gets the log file (empty until <see cref="Options.Init"/> sets it).</summary>
	public static string GetLogFileName() => s_logFileName;

	/// <summary>
	/// Setting the log file name and setting the effective reportingLevel to not LOG_UNCENSORED turns off
	/// buffering of the log messages, and turns on writing them to the actual log (and flushes the buffer).
	/// </summary>
	public static void SetLogFileName(string name)
	{
		DeleteFile(name);
		int sz;
		lock (s_logLock)
			sz = s_logBuffer.Count;
		Logger.Debug($"setLogFileName({name}) was '{s_logFileName}'; {sz} in buffer");
		s_logFileName = name;
	}

	/// <summary>Appends a file, logs nothing to avoid recursion.</summary>
	private static bool LogToFile(string filename, string data)
	{
		try
		{
			File.AppendAllText(filename, data);
			return true;
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
		{
			return false;
		}
	}

	/// <summary>
	/// Writes a reported log message: to standard error at debug levels, into the log file once it is set
	/// (messages before that are buffered and filtered by the then effective level).
	/// </summary>
	public static void Log(SeverityLevel level, string message)
	{
		string msg = $"[{Now()}]\t[{Logger.ToString(level)}]\t{message}\n";

		var effectiveLevel = Logger.ReportingLevel;
		lock (s_logLock)
		{
			if (effectiveLevel >= SeverityLevel.LOG_DEBUG)
			{
				Console.Error.Write(msg);
				Console.Error.Flush();
			}
			if (s_logBuffer.Count > LOG_BUFFER_LIMIT) // drop earliest message so as to not eat all memory
			{
				s_logBuffer.RemoveFirst();
			}
			if (s_logFileName.Length == 0 || effectiveLevel == SeverityLevel.LOG_UNCENSORED) // no log file; accumulate.
			{
				s_logBuffer.AddLast((level, msg));
				return;
			}
			// attempt to flush the buffer
			bool failed = false;
			while (s_logBuffer.Count != 0)
			{
				var front = s_logBuffer.First!.Value;
				if (effectiveLevel >= front.level)
				{
					if (!LogToFile(s_logFileName, front.msg))
					{
						string err = $"Failed to append to '{s_logFileName}'";
						s_logBuffer.AddLast((SeverityLevel.LOG_ERROR, err));
						failed = true;
						break;
					}
				}
				s_logBuffer.RemoveFirst();
			}
			// retain the current message if write fails.
			if (failed || !LogToFile(s_logFileName, msg))
			{
				s_logBuffer.AddLast((level, msg));
			}
		}
	}
}
