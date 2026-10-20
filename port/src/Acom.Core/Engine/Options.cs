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
// Ported from: src/Engine/Options.h, src/Engine/Options.cpp
// Port status: partial (missing: refreshMods/updateMods/getActiveMods/getModInfos (need FileMap/ModInfo), logging to file)
// Option declarations and registrations are generated: Options.Values.cs, Options.Create.cs (port/tools/gen/gen_options.py).

using System.Security.Cryptography;
using System.Text;

namespace Acom.Engine;

/// <summary>One entry of the mod list in options.cfg (C++ std::pair&lt;std::string, bool&gt;).</summary>
public sealed class ModToggle(string id, bool active)
{
	public string Id { get; } = id;

	public bool Active { get; set; } = active;
}

/// <summary>Container for all the various global game options and customizable settings.</summary>
public static partial class Options
{
	/// <summary>SDL_mixer's maximum volume, used for the volume defaults.</summary>
	private const int MIX_MAX_VOLUME = 128;

	private static string _dataFolder = "";
	private static List<string> _dataList = [];
	private static string _userFolder = "";
	private static string _configFolder = "";
	private static readonly Dictionary<string, string> _commandLine = new(StringComparer.Ordinal);
	private static readonly List<OptionInfo> _info = [];
	private static string _masterMod = "";
	private static int _passwordCheck = -1;
	private static bool _loadLastSave;
	private static string _loadThisSave = "";
	private static bool _loadLastSaveExpended;

	private static bool GameIsInstalled(string gameName)
	{
		// look for game data in either the data or user directories
		string dataGameFolder = CrossPlatform.SearchDataFolder(gameName, 8);
		string dataGameZipFile = CrossPlatform.SearchDataFile(gameName + ".zip");
		string userGameFolder = _userFolder + gameName;
		string userGameZipFile = _userFolder + gameName + ".zip";
		return CrossPlatform.FolderMinSize(dataGameFolder, 8)
			|| CrossPlatform.FolderMinSize(userGameFolder, 8)
			|| CrossPlatform.FileExists(dataGameZipFile)
			|| CrossPlatform.FileExists(userGameZipFile);
	}

	private static void SetDefaultMods()
	{
		bool haveUfo = GameIsInstalled("UFO");
		if (haveUfo)
			mods.Add(new ModToggle("xcom1", true));
		if (GameIsInstalled("TFTD"))
			mods.Add(new ModToggle("xcom2", !haveUfo));
	}

	/// <summary>Resets the options back to their defaults.</summary>
	public static void ResetDefault(bool includeMods)
	{
		foreach (var optionInfo in _info)
			optionInfo.Reset();
		BackupDisplay();

		if (includeMods)
		{
			mods.Clear();
			if (_dataList.Count > 0)
				SetDefaultMods();
		}
	}

	/// <summary>
	/// Loads options from a set of command line arguments, in the format "-option value".
	/// </summary>
	private static void LoadArgs()
	{
		var argv = CrossPlatform.GetArgs();
		for (int i = 1; i < argv.Count; ++i)
		{
			string arg = argv[i];
			if ((arg[0] == '-' || arg[0] == '/') && arg.Length > 1)
			{
				if (arg == "--")
					break;
				string argname = arg[1] == '-' && arg.Length > 2 ? arg[2..] : arg[1..];
				argname = argname.ToLowerInvariant();
				if (argname is "cont" or "continue")
				{
					_loadLastSave = true;
					continue;
				}
				if (argv.Count > i + 1)
				{
					++i; // we'll be consuming the next argument too
					Logger.Debug($"loadArgs(): {argname} -> {argv[i]}");
					switch (argname)
					{
						case "data":
							_dataFolder = WithSlash(argv[i]);
							break;
						case "user":
							_userFolder = WithSlash(argv[i]);
							break;
						case "cfg" or "config":
							_configFolder = WithSlash(argv[i]);
							break;
						case "master":
							_masterMod = argv[i];
							break;
						case "load":
							_loadLastSave = true;
							_loadThisSave = argv[i];
							break;
						default:
							// save this command line option for now, we will apply it later
							_commandLine[argname] = argv[i];
							break;
					}
				}
				else
				{
					Logger.Warning($"Unknown option: {argname}");
				}
			}
		}
	}

	private static string WithSlash(string folder) => folder.Length > 1 && folder[^1] != '/' ? folder + "/" : folder;

	/// <summary>Displays command-line help when appropriate.</summary>
	/// <returns>True if help or version was shown and the game should exit.</returns>
	private static bool ShowHelp(TextWriter output)
	{
		var help = new StringBuilder();
		help.Append($"ACOM {Version.ACOM_VERSION} (based on OpenXcom {Version.OPENXCOM_VERSION_SHORT})\n");
		help.Append("Usage: acom [OPTION]...\n\n");
		help.Append("-data PATH\n        use PATH as the default Data Folder instead of auto-detecting\n\n");
		help.Append("-user PATH\n        use PATH as the default User Folder instead of auto-detecting\n\n");
		help.Append("-cfg PATH  or  -config PATH\n        use PATH as the default Config Folder instead of auto-detecting\n\n");
		help.Append("-master MOD\n        set MOD to the current master mod (eg. -master xcom2)\n\n");
		help.Append("-KEY VALUE\n        override option KEY with VALUE (eg. -displayWidth 640)\n\n");
		help.Append("-continue\n        load last save\n\n");
		help.Append("-load FILENAME\n        load the specified FILENAME (from the corresponding master mod subfolder)\n\n");
		help.Append("-version\n        show version number\n\n");
		help.Append("-help\n-?\n        show command-line help\n");
		var argv = CrossPlatform.GetArgs();
		for (int i = 1; i < argv.Count; ++i)
		{
			string arg = argv[i];
			if ((arg[0] == '-' || arg[0] == '/') && arg.Length > 1)
			{
				if (arg == "--")
					break;
				string argname = (arg[1] == '-' && arg.Length > 2 ? arg[2..] : arg[1..]).ToLowerInvariant();
				if (argname is "help" or "?")
				{
					output.Write(help.ToString());
					return true;
				}
				if (argname == "version")
				{
					output.WriteLine(Version.ACOM_VERSION);
					return true;
				}
				if (argname is "cont" or "continue")
					continue;
				// skip next option argument, only couple options do not have it.
				++i;
			}
			else
			{
				Console.Error.WriteLine($"Unknown parameter '{arg}'");
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Handles the initialization of setting up default options and finding and loading any existing ones.
	/// </summary>
	/// <returns>False if the game should exit (help or version was requested).</returns>
	public static bool Init()
	{
		if (ShowHelp(Console.Out))
			return false;
		Create();
		ResetDefault(true);
		LoadArgs();
		SetFolders();
		SetDefaultMods();
		UpdateOptions();

		// set up the logging reportingLevel
#if DEBUG
		Logger.ReportingLevel = SeverityLevel.LOG_DEBUG;
#else
		Logger.ReportingLevel = SeverityLevel.LOG_INFO;
#endif
		if (verboseLogging)
			Logger.ReportingLevel = SeverityLevel.LOG_VERBOSE;

		Logger.Info($"ACOM Version: {Version.ACOM_VERSION} (OpenXcom {Version.OPENXCOM_VERSION_SHORT})");
		Logger.Info($"Platform: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
		Logger.Info($"Data folder is: {_dataFolder}");
		Logger.Info("Data search is: ");
		foreach (var dataPath in _dataList)
			Logger.Info($"- {dataPath}");
		Logger.Info($"User folder is: {_userFolder}");
		Logger.Info($"Config folder is: {_configFolder}");
		Logger.Info("Options loaded successfully.");
		return true;
	}

	/// <summary>Checks the developer password from options.cfg.</summary>
	public static bool IsPasswordCorrect()
	{
		if (_passwordCheck < 0)
		{
#pragma warning disable CA5351 // Not a security feature: compares with the hash the original uses.
			string md5hash = Convert.ToHexStringLower(MD5.HashData(Encoding.UTF8.GetBytes(password)));
#pragma warning restore CA5351
			_passwordCheck = md5hash == "52bd8e15118862c40fc0d6107e197f42" ? 1 : 0; // Password: "openxcom"
		}
		return _passwordCheck > 0;
	}

	public static string GetActiveMaster() => _masterMod;

	public static bool GetLoadLastSave() => _loadLastSave && !_loadLastSaveExpended;

	public static string GetLoadThisSave() => _loadThisSave;

	public static void ExpendLoadLastSave() => _loadLastSaveExpended = true;

	/// <summary>Sets up the game's data, user and config folders.</summary>
	public static void SetFolders()
	{
		_dataList = CrossPlatform.FindDataFolders();
		if (_dataFolder.Length > 0)
		{
			_dataList.Insert(0, _dataFolder);
			Logger.Debug($"setFolders(): inserting {_dataFolder}");
		}
		if (_userFolder.Length == 0)
		{
			var user = CrossPlatform.FindUserFolders();
			if (_configFolder.Length == 0)
				_configFolder = CrossPlatform.FindConfigFolder();

			// Look for an existing user folder
			for (int i = user.Count - 1; i >= 0; --i)
			{
				if (CrossPlatform.FolderExists(user[i]))
				{
					_userFolder = user[i];
					break;
				}
			}

			// Set up folders
			if (_userFolder.Length == 0)
			{
				foreach (var userFolder in user)
				{
					if (CrossPlatform.CreateFolder(userFolder))
					{
						_userFolder = userFolder;
						break;
					}
				}
			}
		}
		if (_userFolder.Length > 0)
		{
			// create mod folder if it doesn't already exist
			CrossPlatform.CreateFolder(_userFolder + "mods");
		}

		if (_configFolder.Length == 0)
			_configFolder = _userFolder;
	}

	/// <summary>Updates the game's options with those in the configuration file and the command line.</summary>
	public static void UpdateOptions()
	{
		// Load existing options
		if (CrossPlatform.FolderExists(_configFolder))
		{
			if (CrossPlatform.FileExists(_configFolder + "options.cfg"))
			{
				Load();
				oxceEmbeddedOnly = false; // no embedded assets in this port
			}
			else
			{
				Save();
			}
		}
		// Create config folder and save options
		else
		{
			CrossPlatform.CreateFolder(_configFolder);
			Save();
		}

		// now apply options set on the command line, overriding defaults and those loaded from config file
		foreach (var optionInfo in _info)
			optionInfo.Load(_commandLine, true);
	}

	/// <summary>Loads options from a YAML file.</summary>
	/// <returns>True if the file was read.</returns>
	public static bool Load(string filename = "options")
	{
		string s = _configFolder + filename + ".cfg";
		try
		{
			var reader = YamlRootNodeReader.FromFile(s);
			// Ignore old options files
			if (reader["options"]["NewBattleMission"].IsValid)
				return false;
			foreach (var optionInfo in _info)
				optionInfo.Load(reader["options"]);

			mods.Clear();
			foreach (var mod in reader["mods"].Children())
			{
				string id = mod["id"].ReadVal<string>();
				bool active = mod["active"].ReadVal(false);
				mods.Add(new ModToggle(id, active));
			}
			if (mods.Count == 0)
				SetDefaultMods();
		}
		catch (Exception e) when (e is YamlReadException or IOException)
		{
			Logger.Warning(e.Message);
			return false;
		}
		return true;
	}

	/// <summary>Saves options to a YAML file; unknown options of the existing file are kept unless <paramref name="reset"/>.</summary>
	public static bool Save(bool reset = false, string filename = "options")
	{
		string filepath = _configFolder + filename + ".cfg";
		string yaml;
		try
		{
			var writer = new YamlRootNodeWriter();
			writer.SetAsMap();
			var modsWriter = writer["mods"];
			modsWriter.SetAsSeq();
			foreach (var pair in mods)
			{
				var modWriter = modsWriter.Write();
				modWriter.SetAsMap();
				modWriter.Write("active", pair.Active);
				modWriter.Write("id", pair.Id);
			}
			var optionsWriter = writer["options"];
			optionsWriter.SetAsMap();
			foreach (var optionInfo in _info.OrderBy(o => o.Id, StringComparer.Ordinal))
				optionInfo.Save(optionsWriter);

			if (!reset && CrossPlatform.FileExists(filepath))
			{
				// Preserve any undefined options, because they could be from a different fork
				var old = YamlRootNodeReader.FromFile(filepath);
				var currentOptions = optionsWriter.ToReader();
				foreach (var oldOption in old["options"].Children())
				{
					if (!currentOptions[oldOption.Key].IsValid)
						optionsWriter.Write(oldOption.Key, oldOption.Val);
				}
			}
			yaml = writer.Emit();
		}
		catch (Exception e) when (e is YamlReadException or IOException)
		{
			Logger.Warning(e.Message);
			return false;
		}
		if (!CrossPlatform.WriteFile(filepath, yaml + "\n"))
		{
			Logger.Warning($"Failed to save {filepath}");
			return false;
		}
		return true;
	}

	public static string GetDataFolder() => _dataFolder;

	public static void SetDataFolder(string folder)
	{
		_dataFolder = folder;
		Logger.Debug($"setDataFolder({folder});");
	}

	public static IReadOnlyList<string> GetDataList() => _dataList;

	public static string GetUserFolder() => _userFolder;

	public static string GetConfigFolder() => _configFolder;

	public static string GetMasterUserFolder() => _userFolder + _masterMod + "/";

	public static IReadOnlyList<OptionInfo> GetOptionInfo() => _info;

	/// <summary>Saves display settings temporarily to be able to revert to old ones.</summary>
	public static void BackupDisplay()
	{
		newDisplayWidth = displayWidth;
		newDisplayHeight = displayHeight;
		newBattlescapeScale = battlescapeScale;
		newGeoscapeScale = geoscapeScale;
		newOpenGL = useOpenGL;
		newScaleFilter = useScaleFilter;
		newHQXFilter = useHQXFilter;
		newOpenGLShader = useOpenGLShader;
		newXBRZFilter = useXBRZFilter;
		newRootWindowedMode = rootWindowedMode;
		newWindowedModePositionX = windowedModePositionX;
		newWindowedModePositionY = windowedModePositionY;
		newFullscreen = fullscreen;
		newAllowResize = allowResize;
		newBorderless = borderless;
	}

	/// <summary>Switches old/new display options for temporarily testing a new display setup.</summary>
	public static void SwitchDisplay()
	{
		(displayWidth, newDisplayWidth) = (newDisplayWidth, displayWidth);
		(displayHeight, newDisplayHeight) = (newDisplayHeight, displayHeight);
		(useOpenGL, newOpenGL) = (newOpenGL, useOpenGL);
		(useScaleFilter, newScaleFilter) = (newScaleFilter, useScaleFilter);
		(battlescapeScale, newBattlescapeScale) = (newBattlescapeScale, battlescapeScale);
		(geoscapeScale, newGeoscapeScale) = (newGeoscapeScale, geoscapeScale);
		(useHQXFilter, newHQXFilter) = (newHQXFilter, useHQXFilter);
		(useOpenGLShader, newOpenGLShader) = (newOpenGLShader, useOpenGLShader);
		(useXBRZFilter, newXBRZFilter) = (newXBRZFilter, useXBRZFilter);
		(rootWindowedMode, newRootWindowedMode) = (newRootWindowedMode, rootWindowedMode);
		(windowedModePositionX, newWindowedModePositionX) = (newWindowedModePositionX, windowedModePositionX);
		(windowedModePositionY, newWindowedModePositionY) = (newWindowedModePositionY, windowedModePositionY);
		(fullscreen, newFullscreen) = (newFullscreen, fullscreen);
		(allowResize, newAllowResize) = (newAllowResize, allowResize);
		(borderless, newBorderless) = (newBorderless, borderless);
	}

	/// <summary>Test hook: sets folders directly instead of searching the system.</summary>
	internal static void SetFoldersForTest(string dataFolder, string userFolder, string configFolder)
	{
		_dataFolder = dataFolder;
		_dataList = [dataFolder];
		_userFolder = userFolder;
		_configFolder = configFolder;
	}
}
