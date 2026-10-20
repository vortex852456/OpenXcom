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
// Ported from: src/Engine/ModInfo.h, src/Engine/ModInfo.cpp

namespace Acom;

/// <summary>Represents mod metadata (metadata.yml).</summary>
public sealed class ModInfo
{
	private const sbyte PrefixNumMin = 0;
	private const sbyte PrefixNumMax = 10;
	private const sbyte PrefixText = 11;

	/// <summary>Engines whose mods this port can run: OXCE up to the version it is based on, and plain OpenXcom.</summary>
	private static readonly (string Name, int[] Version)[] s_supportedEngines =
	[
		(Acom.Version.OPENXCOM_VERSION_ENGINE, Acom.Version.OPENXCOM_VERSION_NUMBER),
		("", [0, 0, 0, 0]), // assume that every engine support mods from base game
	];

	private static readonly (string Text, sbyte[] Normalized) s_defaultModVersion = NormalizeModVersion("def", "1.0");

	private (string Text, sbyte[] Normalized) _version = s_defaultModVersion;
	private (string Text, sbyte[] Normalized) _requiredMasterModVersion = ("", []);

	public ModInfo(string path)
	{
		Path = path;
		Name = CrossPlatform.BaseFilename(path);
		Id = Name;
	}

	public string Path { get; }
	public string Name { get; private set; }
	public string Description { get; private set; } = "No description.";
	public string Author { get; private set; } = "unknown author";
	public string Id { get; private set; }

	/// <summary>Id of the master mod this mod belongs to (empty = any master).</summary>
	public string Master { get; private set; } = "xcom1";

	public string Version => _version.Text;
	public string VersionDisplay { get; private set; } = "1.0";
	public bool IsMaster { get; private set; }

	/// <summary>Sprite/sound index space reserved by the mod, in units of 1000 (1..100).</summary>
	public int ReservedSpace { get; private set; } = 1;

	public bool IsEngineOk { get; private set; }
	public string RequiredExtendedEngine { get; private set; } = "";
	public string RequiredExtendedVersion { get; private set; } = "";
	public string RequiredMasterVersion => _requiredMasterModVersion.Text;
	public string ResourceConfigFile { get; private set; } = "";
	public IReadOnlyList<string> ExternalResourceDirs { get; private set; } = [];

	/// <summary>Loads the metadata from YAML.</summary>
	public void Load(YamlNodeReader reader)
	{
		string id = Id, name = Name, desc = Description, author = Author, versionDisplay = VersionDisplay;
		bool isMaster = IsMaster;
		int reservedSpace = ReservedSpace;
		string requiredEngine = RequiredExtendedEngine, requiredVersion = RequiredExtendedVersion, resourceConfig = ResourceConfigFile;

		reader.TryRead("id", ref id);
		Id = id;
		reader.TryRead("name", ref name);
		reader.TryRead("description", ref desc);
		if (reader["version"] is { IsValid: true } ver)
		{
			_version = NormalizeModVersion(Id, ver.ReadVal<string>());
			versionDisplay = _version.Text;
		}
		reader.TryRead("versionDisplay", ref versionDisplay);
		reader.TryRead("author", ref author);
		reader.TryRead("isMaster", ref isMaster);
		reader.TryRead("reservedSpace", ref reservedSpace);
		if (reader.TryRead("requiredExtendedVersion", ref requiredVersion))
			requiredEngine = "Extended"; // for backward compatibility
		reader.TryRead("requiredExtendedEngine", ref requiredEngine);

		Name = name;
		Description = desc;
		Author = author;
		VersionDisplay = versionDisplay;
		IsMaster = isMaster;
		RequiredExtendedEngine = requiredEngine;
		RequiredExtendedVersion = requiredVersion;
		IsEngineOk = FindCompatibleEngine(requiredEngine, CrossPlatform.ParseVersion(requiredVersion));
		ReservedSpace = Math.Clamp(reservedSpace, 1, 100);

		string master = Master;
		if (IsMaster)
		{
			// default a master's master to none. masters can still have
			// masters, but they must be explicitly declared.
			master = "";
		}
		reader.TryRead("resourceConfig", ref resourceConfig);
		ResourceConfigFile = resourceConfig;
		reader.TryRead("master", ref master);
		Master = master == "*" ? "" : master;
		if (IsMaster && Master.Length == 0)
		{
			// only top-level masters can load external resource dirs
			List<string> dirs = [];
			if (reader.TryRead("loadResources", ref dirs))
				ExternalResourceDirs = dirs;
		}
		if (reader["requiredMasterModVersion"] is { IsValid: true } req)
		{
			if (Master.Length == 0)
				Logger.Warning($"Mod '{Id}' without master can't have 'requiredMasterModVersion'.");
			else
				_requiredMasterModVersion = NormalizeModVersion(Id, req.ReadVal<string>());
		}
	}

	/// <summary>Does the parent master have at least the version this mod requires?</summary>
	public bool IsParentMasterOk(ModInfo parentMod)
	{
		ArgumentNullException.ThrowIfNull(parentMod);
		return _requiredMasterModVersion.Text.Length == 0 || CompareVersions(parentMod._version, _requiredMasterModVersion);
	}

	/// <summary>Can this mod be activated together with the given master?</summary>
	public bool CanActivate(string curMaster) => IsMaster || Master.Length == 0 || Master == curMaster;

	internal static bool FindCompatibleEngine(string engine, int[] version)
	{
		foreach (var (name, supported) in s_supportedEngines)
		{
			if (name == engine)
				return !CrossPlatform.IsHigherThanCurrentVersion(version, supported);
		}
		return false;
	}

	/// <summary>
	/// Converts a version string into a comparable form: numbers are length-prefixed (without leading
	/// zeros), letters are upper-cased and prefixed, dots separate parts, trailing zero parts are dropped.
	/// An invalid version yields an empty normalized value.
	/// </summary>
	internal static (string Text, sbyte[] Normalized) NormalizeModVersion(string parent, string ver)
	{
		const int wasNothing = 0, wasNum = 1, wasText = 2, wasDot = 3;
		int state = wasNothing;
		int lastPrefixNum = 0;
		var normalized = new List<sbyte>();
		foreach (char c in ver)
		{
			char curr = c is >= 'a' and <= 'z' ? (char)(c + ('A' - 'a')) : c; // make case-insensitive
			if (curr is >= 'A' and <= 'Z')
			{
				if (state != wasText)
				{
					state = wasText;
					normalized.Add(PrefixText);
				}
				normalized.Add((sbyte)curr);
			}
			else if (curr is >= '0' and <= '9')
			{
				if (state != wasNum)
				{
					state = wasNum;
					lastPrefixNum = normalized.Count;
					normalized.Add(PrefixNumMin);
					if (curr > '0')
					{
						normalized[lastPrefixNum]++;
						normalized.Add((sbyte)curr);
					}
				}
				else
				{
					sbyte temp = normalized[lastPrefixNum];
					if (temp == PrefixNumMax)
					{
						Logger.Warning($"Error in version number in mod '{parent}': unsupported number length");
						return (ver, []);
					}
					if (temp != PrefixNumMin || curr > '0')
					{
						normalized[lastPrefixNum]++;
						normalized.Add((sbyte)curr);
					}
				}
			}
			else if (curr == '.')
			{
				if (state == wasDot)
				{
					Logger.Warning($"Error in version number in mod '{parent}': duplicated dots");
					return (ver, []);
				}
				state = wasDot;
			}
			else
			{
				Logger.Warning($"Error in version number in mod '{parent}': unexpected symbol");
				return (ver, []);
			}
		}
		// version could end with a chain of "0.0.0.0.0", trim to the last non-zero
		int last = normalized.FindLastIndex(v => v != 0);
		if (last >= 0)
			normalized.RemoveRange(last + 1, normalized.Count - last - 1);
		else if (normalized.Count > 0)
			normalized.RemoveRange(1, normalized.Count - 1); // version is only "0": keep the first "0"
		return (ver, [.. normalized]);
	}

	/// <summary>Is the provided version at least the required one?</summary>
	internal static bool CompareVersions((string Text, sbyte[] Normalized) provided, (string Text, sbyte[] Normalized) required)
	{
		return provided.Text == required.Text || Compare(provided.Normalized, required.Normalized) >= 0;
	}

	/// <summary>Lexicographic comparison like std::basic_string&lt;signed char&gt;.</summary>
	private static int Compare(sbyte[] a, sbyte[] b)
	{
		int n = Math.Min(a.Length, b.Length);
		for (int i = 0; i < n; ++i)
		{
			if (a[i] != b[i])
				return a[i] < b[i] ? -1 : 1;
		}
		return a.Length.CompareTo(b.Length);
	}
}
