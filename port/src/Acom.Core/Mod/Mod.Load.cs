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
// Ported from: src/Mod/Mod.h, src/Mod/Mod.cpp
// Port status: partial (missing: script hooks)
//
// Ruleset loading part of Mod: YAML load helpers, offsets, loadAll/loadMod/loadFile and loadRule.
// The C++ ModScript parsers and ModScriptGlobal (tags, script values, RuleList constants) are left out.
#nullable disable

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Acom;

public partial class Mod
{
	private const string InfoTag = "!info";
	private const string AddTag = "!add";
	private const string RemoveTag = "!remove";

	private const string YamlRuleNodeDelete = "delete";
	private const string YamlRuleNodeNew = "new";
	private const string YamlRuleNodeOverride = "override";
	private const string YamlRuleNodeUpdate = "update";
	private const string YamlRuleNodeIgnore = "ignore";

	/// <summary>Default of the C++ <c>OPENXCOM_VERSION_GIT</c> (src/version.h).</summary>
	internal const string OpenXcomVersionGit = " (v2026-03-21)";

	/// <summary>Date part of the version string, like <c>" (v2023-10-21)"</c>.</summary>
	internal readonly struct OxceVersionDate
	{
		public int Year { get; }
		public int Month { get; }
		public int Day { get; }

		public OxceVersionDate(string data)
		{
			ArgumentNullException.ThrowIfNull(data);
			int year = 0, month = 0, day = 0;
			bool correct = false;
			// check if look like format " (v2023-10-21)"
			int offset = data.IndexOf(" (v", StringComparison.Ordinal);
			if (offset >= 0 && data.Length >= offset + 14 && data[offset + 2] == 'v' && data[offset + 7] == '-' && data[offset + 10] == '-' && data[offset + 13] == ')')
			{
				correct = ScanDate(data, offset, out year, out month, out day);
			}

			if (!correct)
			{
				year = 0;
				month = 0;
				day = 0;
			}
			Year = year;
			Month = month;
			Day = day;
		}

		/// <summary>C++ <c>explicit operator bool</c>.</summary>
		public bool IsValid => Year != 0 && Month != 0 && Day != 0;

		/// <summary>C <c>sscanf(s + pos, " (v%4d-%2d-%2d)", ...) == 3</c>.</summary>
		private static bool ScanDate(string s, int pos, out int year, out int month, out int day)
		{
			month = 0;
			day = 0;
			year = 0;
			SkipSpace(s, ref pos);
			if (!Match(s, ref pos, '(') || !Match(s, ref pos, 'v'))
				return false;
			if (!ScanInt(s, ref pos, 4, out year) || !Match(s, ref pos, '-'))
				return false;
			if (!ScanInt(s, ref pos, 2, out month) || !Match(s, ref pos, '-'))
				return false;
			return ScanInt(s, ref pos, 2, out day);
		}

		private static bool IsCSpace(char c) => c is ' ' or '\t' or '\n' or '\v' or '\f' or '\r';

		private static void SkipSpace(string s, ref int pos)
		{
			while (pos < s.Length && IsCSpace(s[pos]))
				++pos;
		}

		private static bool Match(string s, ref int pos, char c)
		{
			if (pos < s.Length && s[pos] == c)
			{
				++pos;
				return true;
			}
			return false;
		}

		/// <summary>C <c>%Nd</c>: skips white space, then reads at most <paramref name="width"/> characters (sign and digits).</summary>
		private static bool ScanInt(string s, ref int pos, int width, out int value)
		{
			value = 0;
			SkipSpace(s, ref pos);
			int end = Math.Min(s.Length, pos + width);
			bool negative = false;
			if (pos < end && (s[pos] == '+' || s[pos] == '-'))
			{
				negative = s[pos] == '-';
				++pos;
			}
			int digits = pos;
			long v = 0;
			while (pos < end && s[pos] >= '0' && s[pos] <= '9')
			{
				v = v * 10 + (s[pos] - '0');
				++pos;
			}
			if (pos == digits)
				return false;
			value = (int)(negative ? -v : v);
			return true;
		}
	}

	private static readonly OxceVersionDate s_currYear = new(OpenXcomVersionGit);

	/// <summary>
	/// Exceptions corresponding to the C++ <c>OpenXcom::Exception</c> (FileMap reports missing files as
	/// <see cref="FileNotFoundException"/>, the YAML layer reports errors as <see cref="YamlReadException"/>).
	/// </summary>
	private static bool IsOxceException(Exception e) => e is InvalidOperationException or FileNotFoundException or YamlReadException;

	/// <summary>Check for obsolete error based on year.</summary>
	/// <param name="year">Year when given function stop be available.</param>
	/// <returns>True if code still should run.</returns>
	public bool CheckForObsoleteErrorByYear(string parent, YamlNodeReader reader, string error, int year)
	{
		SeverityLevel level = SeverityLevel.LOG_INFO;
		bool r = true;

		if (s_currYear.IsValid)
		{
			if (s_currYear.Year < year)
			{
				level = SeverityLevel.LOG_INFO;
			}
			else // after obsolete year functionality is disabled
			{
				level = SeverityLevel.LOG_FATAL;
				r = false;
			}
		}
		CheckForSoftError(true, parent, reader, $"Obsolete (to removed after year {year}) operation {error}", level);

		return r;
	}

	/// <summary>Check for error that we can ignore by user request.</summary>
	public bool CheckForSoftError(bool check, string parent, YamlNodeReader reader, string error, SeverityLevel level = SeverityLevel.LOG_WARNING)
	{
		if (check)
		{
			var ex = new LoadRuleException(parent, reader, error);
			if (Options.oxceModValidationLevel < (int)level && level != SeverityLevel.LOG_FATAL)
			{
				Logger.Log(level, _currentFile + ": Suppressed " + ex.Message);
				return true;
			}
			else
			{
				throw ex;
			}
		}
		return false;
	}

	/// <summary>Check for error that we can ignore by user request.</summary>
	public bool CheckForSoftError(bool check, string parent, string error, SeverityLevel level = SeverityLevel.LOG_WARNING)
	{
		if (check)
		{
			var ex = new LoadRuleException(parent, error);
			if (Options.oxceModValidationLevel < (int)level && level != SeverityLevel.LOG_FATAL)
			{
				Logger.Log(level, _currentFile + ": Suppressed " + ex.Message);
				return true;
			}
			else
			{
				throw ex;
			}
		}
		return false;
	}

	/// <summary>Verify if value have defined surface in given set.</summary>
	public void VerifySpriteOffset(string parent, int sprite, string set)
	{
		if (Options.lazyLoadResources)
		{
			// we can't check if index is correct when set is loaded
			return;
		}

		var s = GetRule(set, "Sprite Set", _sets, true);

		if (s.TotalFrames == 0)
		{
			// HACK: some sprites should be shared between different sets (for example 'Projectiles' and 'UnderwaterProjectiles'),
			// but in some cases one set is not used (for example if the weapon is 'underwaterOnly: true').
			// If there are no surfaces at all, this means this index is not used.
			return;
		}

		CheckForSoftError(
			sprite != NO_SURFACE && s.GetFrame(sprite) == null,
			parent,
			$"Wrong index {sprite} for surface set {set} (please note that the index in the ruleset is smaller, by several thousands)",
			SeverityLevel.LOG_ERROR
		);
	}

	/// <summary>Verify if value have defined surface in given set.</summary>
	public void VerifySpriteOffset(string parent, List<int> sprites, string set)
	{
		ArgumentNullException.ThrowIfNull(sprites);
		if (Options.lazyLoadResources)
		{
			// we can't check if index is correct when set is loaded
			return;
		}

		var s = GetRule(set, "Sprite Set", _sets, true);

		if (s.TotalFrames == 0)
		{
			// HACK: see above
			return;
		}

		foreach (int sprite in sprites)
		{
			CheckForSoftError(
				sprite != NO_SURFACE && s.GetFrame(sprite) == null,
				parent,
				$"Wrong index {sprite} for surface set {set} (please note that the index in the ruleset is smaller, by several thousands)",
				SeverityLevel.LOG_ERROR
			);
		}
	}

	/// <summary>Verify if value have defined sound in given set.</summary>
	public void VerifySoundOffset(string parent, int sound, string set)
	{
		if (Options.mute)
		{
			// when mute is set not sound data is loaded and we can't check for correct data
			return;
		}

		var s = GetSoundSet(set);

		CheckForSoftError(
			sound != NO_SOUND && s.GetSound(sound) == null,
			parent,
			$"Wrong index {sound} for sound set {set} (please note that the index in the ruleset is smaller, by several thousands)",
			SeverityLevel.LOG_ERROR
		);
	}

	/// <summary>Verify if value have defined sound in given set.</summary>
	public void VerifySoundOffset(string parent, List<int> sounds, string set)
	{
		ArgumentNullException.ThrowIfNull(sounds);
		if (Options.mute)
		{
			// when mute is set not sound data is loaded and we can't check for correct data
			return;
		}

		var s = GetSoundSet(set);

		foreach (int sound in sounds)
		{
			CheckForSoftError(
				sound != NO_SOUND && s.GetSound(sound) == null,
				parent,
				$"Wrong index {sound} for sound set {set} (please note that the index in the ruleset is smaller, by several thousands)",
				SeverityLevel.LOG_ERROR
			);
		}
	}

	/// <summary>Returns the current mod-based offset for resources.</summary>
	public int ModOffset => _modCurrent.offset;

	// ---- YAML helpers (C++ anonymous namespace) ----

	/// <summary>rapidyaml <c>TAG_SEQ</c> (<c>!!seq</c>, expanded by the parser).</summary>
	private static bool HasSeqTag(YamlNodeReader reader) => reader.HasValTagNamed("tag:yaml.org,2002:seq") || reader.HasValTagNamed("!!seq");

	private static bool IsListHelper(YamlNodeReader reader) => reader.IsSeq && (!reader.HasValTag || HasSeqTag(reader) || reader.HasValTagNamed(InfoTag));

	private static bool IsListAddTagHelper(YamlNodeReader reader) => reader.IsSeq && reader.HasValTagNamed(AddTag);

	private static bool IsListRemoveTagHelper(YamlNodeReader reader) => reader.IsSeq && reader.HasValTagNamed(RemoveTag);

	// C++ checks TAG_SEQ here too
	private static bool IsMapHelper(YamlNodeReader reader) => reader.IsMap && (!reader.HasValTag || HasSeqTag(reader) || reader.HasValTagNamed(InfoTag));

	private static bool IsMapAddTagHelper(YamlNodeReader reader) => reader.IsMap && reader.HasValTagNamed(AddTag);

	[DoesNotReturn]
	private static void ThrowOnBadListHelper(string parent, YamlNodeReader reader)
	{
		string err;
		if (reader.IsSeq)
		{
			// it is a sequence, but it could not be loaded... this means the tag is not supported
			err = "unsupported node tag '" + reader.ValTag + "'";
		}
		else
		{
			err = "wrong node type, expected a list";
		}
		throw new LoadRuleException(parent, reader, err);
	}

	[DoesNotReturn]
	private static void ThrowOnBadMapHelper(string parent, YamlNodeReader reader)
	{
		string err;
		if (reader.IsMap)
		{
			// it is a map, but it could not be loaded... this means the tag is not supported
			err = "unsupported node tag '" + reader.ValTag + "'";
		}
		else
		{
			err = "wrong node type, expected a map";
		}
		throw new LoadRuleException(parent, reader, err);
	}

	private static void ShowInfo(string parent, YamlNodeReader reader, params string[] names)
	{
		if (reader.HasValTagNamed(InfoTag))
		{
			var info = new StringBuilder();
			info.Append("Options available for ").Append(parent).Append(" at line ").Append(reader.LocationInFile.Line).Append(" are: ");
			foreach (var name in names)
				info.Append(' ').Append(name);
			Logger.Info(info.ToString());
		}
	}

	/// <summary>
	/// One C++ <c>loadHelper</c> overload: loads a node into a value and returns the new value
	/// (containers are filled in place and returned).
	/// </summary>
	private delegate T LoadFunc<T>(string parent, T value, YamlNodeReader reader);

	/// <summary>Terminal function loading integer.</summary>
	private static int LoadHelperInt(string parent, int v, YamlNodeReader reader) => reader.ReadVal<int>();

	/// <summary>Terminal function loading string. Function can't load empty string.</summary>
	private static string LoadHelperName(string parent, string v, YamlNodeReader reader)
	{
		reader.TryReadVal(ref v);
		if (IsEmptyRuleName(v))
		{
			throw new LoadRuleException(parent, reader, "Invalid value for name");
		}
		return v;
	}

	/// <summary>List that is always replaced as a whole (C++ <c>LoadFuncStandard</c>).</summary>
	private static void LoadListStandard<T>(string parent, List<T> v, YamlNodeReader reader, Func<T> create, LoadFunc<T> loadNew)
	{
		if (reader)
		{
			ShowInfo(parent, reader);

			if (IsListHelper(reader))
			{
				v.Clear();
				foreach (var n in reader.Children())
				{
					v.Add(loadNew(parent, create(), n));
				}
			}
			else
			{
				ThrowOnBadListHelper(parent, reader);
			}
		}
	}

	/// <summary>List that allows adding and removing elements (C++ <c>LoadFuncEditable</c>).</summary>
	private static void LoadListEditable<T>(string parent, List<T> v, YamlNodeReader reader, Func<T> create, LoadFunc<T> loadNew, LoadFunc<T> loadAdd)
	{
		if (reader)
		{
			ShowInfo(parent, reader, AddTag, RemoveTag);

			if (IsListHelper(reader))
			{
				v.Clear();
				foreach (var n in reader.Children())
				{
					v.Add(loadNew(parent, create(), n));
				}
			}
			else if (IsListAddTagHelper(reader))
			{
				foreach (var n in reader.Children())
				{
					v.Add(loadAdd(parent, create(), n));
				}
			}
			else if (IsListRemoveTagHelper(reader))
			{
				foreach (var n in reader.Children())
				{
					var value = n.ReadVal<T>();
					v.RemoveAll(x => EqualityComparer<T>.Default.Equals(x, value));
				}
			}
			else
			{
				ThrowOnBadListHelper(parent, reader);
			}
		}
	}

	/// <summary>Map that is always replaced as a whole (C++ <c>LoadFuncStandard</c>).</summary>
	private static void LoadMapStandard<V>(string parent, SortedDictionary<string, V> v, YamlNodeReader reader, Func<V> create, LoadFunc<V> loadNew)
	{
		if (reader)
		{
			ShowInfo(parent, reader);

			if (IsMapHelper(reader))
			{
				v.Clear();
				foreach (var n in reader.Children())
				{
					var key = n.ReadKey<string>();
					v[key] = loadNew(parent, v.TryGetValue(key, out var current) ? current : create(), n);
				}
			}
			else
			{
				ThrowOnBadMapHelper(parent, reader);
			}
		}
	}

	/// <summary>Map that allows adding and removing elements (C++ <c>LoadFuncEditable</c>).</summary>
	private static void LoadMapEditable<V>(string parent, SortedDictionary<string, V> v, YamlNodeReader reader, Func<V> create, LoadFunc<V> loadNew, LoadFunc<V> loadAdd)
	{
		if (reader)
		{
			ShowInfo(parent, reader, AddTag, RemoveTag);

			if (IsMapHelper(reader))
			{
				v.Clear();
				foreach (var n in reader.Children())
				{
					var key = n.ReadKey<string>();
					v[key] = loadNew(parent, v.TryGetValue(key, out var current) ? current : create(), n);
				}
			}
			else if (IsMapAddTagHelper(reader))
			{
				foreach (var n in reader.Children())
				{
					var key = n.ReadKey<string>();
					v[key] = loadAdd(parent, v.TryGetValue(key, out var current) ? current : create(), n);
				}
			}
			else if (IsListRemoveTagHelper(reader)) // we use a list here as we only need the keys
			{
				foreach (var n in reader.Children())
				{
					v.Remove(n.ReadVal<string>());
				}
			}
			else
			{
				ThrowOnBadMapHelper(parent, reader);
			}
		}
	}

	/// <summary>Fixed order map, relies on the fact that the YAML reader preserves map order from the loaded file.</summary>
	private static void LoadPairListEditable<V>(string parent, List<(string first, V second)> v, YamlNodeReader reader, Func<V> create, LoadFunc<V> loadNew, LoadFunc<V> loadAdd)
	{
		if (reader)
		{
			ShowInfo(parent, reader, AddTag, RemoveTag);

			if (IsMapHelper(reader))
			{
				v.Clear();
				foreach (var n in reader.Children())
				{
					var key = n.ReadKey<string>();
					v.Add((key, loadNew(parent, create(), n)));
				}
			}
			else if (IsMapAddTagHelper(reader))
			{
				foreach (var n in reader.Children())
				{
					var key = n.ReadKey<string>();
					int found = v.FindIndex(p => p.first == key);
					if (found >= 0)
					{
						v[found] = (key, loadAdd(parent, v[found].second, n));
					}
					else
					{
						v.Add((key, loadAdd(parent, create(), n)));
					}
				}
			}
			else if (IsListRemoveTagHelper(reader)) // we use a list here as we only need the keys
			{
				foreach (var n in reader.Children())
				{
					var key = n.ReadVal<string>();
					v.RemoveAll(p => p.first == key);
				}
			}
			else
			{
				ThrowOnBadMapHelper(parent, reader);
			}
		}
	}

	private static List<string> LoadNamesStandardHelper(string parent, List<string> v, YamlNodeReader reader)
	{
		LoadListStandard(parent, v, reader, () => "", LoadHelperName);
		return v;
	}

	private static List<string> LoadNamesEditableHelper(string parent, List<string> v, YamlNodeReader reader)
	{
		LoadListEditable(parent, v, reader, () => "", LoadHelperName, LoadHelperName);
		return v;
	}

	private static List<int> LoadIntsStandardHelper(string parent, List<int> v, YamlNodeReader reader)
	{
		LoadListStandard(parent, v, reader, () => 0, LoadHelperInt);
		return v;
	}

	private static SortedDictionary<string, int> LoadNamesToIntStandardHelper(string parent, SortedDictionary<string, int> v, YamlNodeReader reader)
	{
		LoadMapStandard(parent, v, reader, () => 0, LoadHelperInt);
		return v;
	}

	private static SortedDictionary<string, int> LoadNamesToIntEditableHelper(string parent, SortedDictionary<string, int> v, YamlNodeReader reader)
	{
		LoadMapEditable(parent, v, reader, () => 0, LoadHelperInt, LoadHelperInt);
		return v;
	}

	/// <summary>Get offset and index for sound set or sprite set.</summary>
	/// <param name="parent">Name of parent node, used for better error message.</param>
	/// <param name="offset">Member to load new value.</param>
	/// <param name="reader">Node with data.</param>
	/// <param name="shared">Max offset limit that is shared for every mod.</param>
	/// <param name="set">Name of the set, used for error messages.</param>
	/// <param name="multiplier">Value used by `projectile` surface set to convert projectile offset to index offset in surface.</param>
	/// <param name="sizeScale">Value used by transparency colors, reduce total number of available space for offset.</param>
	public void LoadOffsetNode(string parent, ref int offset, YamlNodeReader reader, int shared, string set, int multiplier, int sizeScale = 1)
	{
		Debug.Assert(_modCurrent != null);
		ModData curr = _modCurrent;
		if (reader.HasVal)
		{
			offset = reader.ReadVal<int>();
		}
		else if (IsMapHelper(reader))
		{
			offset = reader["index"].ReadVal<int>();
			string mod = reader["mod"].ReadVal<string>();
			if (mod == ModNameMaster)
			{
				curr = _modData[0];
			}
			else if (mod == ModNameCurrent)
			{
				//nothing
			}
			else
			{
				ModData n = null;
				foreach (var d in _modData)
				{
					if (d.name == mod)
					{
						n = d;
						break;
					}
				}

				if (n != null)
				{
					curr = n;
				}
				else
				{
					throw new LoadRuleException(parent, reader, "unknown mod '" + mod + "' used");
				}
			}
		}
		else
		{
			throw new LoadRuleException(parent, reader, "unsupported yaml node");
		}

		if (offset < -1)
		{
			throw new LoadRuleException(parent, reader, $"offset '{offset}' has incorrect value in set '{set}'");
		}
		else if (offset == -1)
		{
			//ok
		}
		else
		{
			int f = offset;
			f *= multiplier;
			// C++ compares as size_t
			if (unchecked((ulong)f) > (ulong)(curr.size / sizeScale))
			{
				throw new LoadRuleException(parent, reader, $"offset '{offset}' exceeds mod size limit {curr.size / multiplier / sizeScale} in set '{set}'");
			}
			if (f >= shared)
				f += curr.offset / sizeScale;
			offset = f;
		}
	}

	/// <summary>
	/// Returns the appropriate mod-based offset for a sprite.
	/// If the ID is bigger than the surfaceset contents, the mod offset is applied.
	/// </summary>
	/// <param name="parent">Name of parent node, used for better error message.</param>
	/// <param name="sprite">Member to load new sprite ID index.</param>
	/// <param name="reader">Node with data.</param>
	/// <param name="set">Name of the surfaceset to lookup.</param>
	/// <param name="multiplier">Value used by `projectile` surface set to convert projectile offset to index offset in surface.</param>
	public void LoadSpriteOffset(string parent, ref int sprite, YamlNodeReader reader, string set, int multiplier = 1)
	{
		if (reader)
		{
			LoadOffsetNode(parent, ref sprite, reader, GetRule(set, "Sprite Set", _sets, true).MaxSharedFrames, set, multiplier);
		}
	}

	/// <summary>Gets the mod offset array for a certain sprite.</summary>
	public void LoadSpriteOffset(string parent, List<int> sprites, YamlNodeReader reader, string set)
	{
		ArgumentNullException.ThrowIfNull(sprites);
		if (reader)
		{
			int maxShared = GetRule(set, "Sprite Set", _sets, true).MaxSharedFrames;
			sprites.Clear();
			if (IsListHelper(reader))
			{
				foreach (var spriteReader in reader.Children())
				{
					sprites.Add(NO_SURFACE);
					LoadOffsetNode(parent, ref CollectionsMarshal.AsSpan(sprites)[^1], spriteReader, maxShared, set, 1);
					if (CheckForSoftError(sprites[^1] == NO_SURFACE, parent, spriteReader, "incorrect value in sprite list"))
					{
						sprites.RemoveAt(sprites.Count - 1);
					}
				}
			}
			else
			{
				sprites.Add(NO_SURFACE);
				LoadOffsetNode(parent, ref CollectionsMarshal.AsSpan(sprites)[^1], reader, maxShared, set, 1);
			}
		}
	}

	/// <summary>
	/// Returns the appropriate mod-based offset for a sound.
	/// If the ID is bigger than the soundset contents, the mod offset is applied.
	/// </summary>
	public void LoadSoundOffset(string parent, ref int sound, YamlNodeReader reader, string set)
	{
		if (reader)
		{
			LoadOffsetNode(parent, ref sound, reader, GetSoundSet(set).MaxSharedSounds, set, 1);
		}
	}

	/// <summary>Gets the mod offset array for a certain sound.</summary>
	public void LoadSoundOffset(string parent, List<int> sounds, YamlNodeReader reader, string set)
	{
		ArgumentNullException.ThrowIfNull(sounds);
		if (reader)
		{
			int maxShared = GetSoundSet(set).MaxSharedSounds;
			sounds.Clear();
			if (IsListHelper(reader))
			{
				foreach (var soundReader in reader.Children())
				{
					sounds.Add(NO_SOUND);
					LoadOffsetNode(parent, ref CollectionsMarshal.AsSpan(sounds)[^1], soundReader, maxShared, set, 1);
					if (CheckForSoftError(sounds[^1] == NO_SOUND, parent, soundReader, "incorrect value in sound list"))
					{
						sounds.RemoveAt(sounds.Count - 1);
					}
				}
			}
			else
			{
				sounds.Add(NO_SOUND);
				LoadOffsetNode(parent, ref CollectionsMarshal.AsSpan(sounds)[^1], reader, maxShared, set, 1);
			}
		}
	}

	/// <summary>Gets the mod offset for a certain transparency index.</summary>
	public void LoadTransparencyOffset(string parent, ref int index, YamlNodeReader reader)
	{
		if (reader)
		{
			LoadOffsetNode(parent, ref index, reader, 0, "TransparencyLUTs", 1, ModTransparencySizeReduction);
		}
	}

	/// <summary>
	/// Returns the appropriate mod-based offset for a generic ID.
	/// If the ID is bigger than the max, the mod offset is applied.
	/// </summary>
	/// <param name="id">Numeric ID.</param>
	/// <param name="max">Maximum vanilla value.</param>
	public int GetOffset(int id, int max)
	{
		Debug.Assert(_modCurrent != null);
		if (id > max)
			return id + _modCurrent.offset;
		else
			return id;
	}

	/// <summary>Load base functions to bit set.</summary>
	public void LoadBaseFunction(string parent, ref RuleBaseFacilityFunctions f, YamlNodeReader reader)
	{
		if (reader)
		{
			try
			{
				if (IsListHelper(reader))
				{
					f.Reset();
					foreach (var n in reader.Children())
					{
						f.Set(_baseFunctionNames.AddName(n.ReadVal<string>(), RuleBaseFacilityFunctions.Size));
					}
				}
				else if (IsListAddTagHelper(reader))
				{
					foreach (var n in reader.Children())
					{
						f.Set(_baseFunctionNames.AddName(n.ReadVal<string>(), RuleBaseFacilityFunctions.Size));
					}
				}
				else if (IsListRemoveTagHelper(reader))
				{
					foreach (var n in reader.Children())
					{
						f.Set(_baseFunctionNames.AddName(n.ReadVal<string>(), RuleBaseFacilityFunctions.Size), false);
					}
				}
				else
				{
					ThrowOnBadListHelper(parent, reader);
				}
			}
			catch (LoadRuleException)
			{
				//context is already included in exception, no need add more
				throw;
			}
			catch (Exception ex) when (IsOxceException(ex))
			{
				throw new LoadRuleException(parent, reader, ex.Message);
			}
		}
	}

	/// <summary>Get names of function names in given bitset.</summary>
	public List<string> GetBaseFunctionNames(RuleBaseFacilityFunctions f)
	{
		var vec = new List<string>(f.Count());
		for (int i = 0; i < RuleBaseFacilityFunctions.Size; ++i)
		{
			if (f.Test(i))
			{
				vec.Add(_baseFunctionNames.GetName(i));
			}
		}
		return vec;
	}

#pragma warning disable CA1822 // C++ const member functions, called as mod.loadXxx(...) by the rule classes
	/// <summary>
	/// Loads a list of ints.
	/// Another mod can only override the whole list, no partial edits allowed.
	/// </summary>
	public void LoadInts(string parent, List<int> ints, YamlNodeReader reader)
	{
		ArgumentNullException.ThrowIfNull(ints);
		LoadListStandard(parent, ints, reader, () => 0, LoadHelperInt);
	}

	/// <summary>
	/// Loads a list of ints where order of items does not matter.
	/// Another mod can remove or add new values without altering the whole list.
	/// </summary>
	public void LoadUnorderedInts(string parent, List<int> ints, YamlNodeReader reader)
	{
		ArgumentNullException.ThrowIfNull(ints);
		LoadListEditable(parent, ints, reader, () => 0, LoadHelperInt, LoadHelperInt);
	}

	/// <summary>Loads a name. If the node does not exist, the value is not changed.</summary>
	public void LoadName(string parent, ref string names, YamlNodeReader reader)
	{
		if (reader)
		{
			names = LoadHelperName(parent, names, reader);
		}
	}

	/// <summary>Loads a name. Has the option of loading null `~` as special string value.</summary>
	public void LoadNameNull(string parent, ref string names, YamlNodeReader reader)
	{
		if (reader)
		{
			if (reader.HasNullVal)
			{
				names = STR_NULL;
			}
			else
			{
				names = reader.ReadVal<string>();
				if (names == STR_NULL)
				{
					throw new LoadRuleException(parent, reader, "Invalid value for name ");
				}
			}
		}
	}

	/// <summary>
	/// Loads a list of names.
	/// Another mod can only override the whole list, no partial edits allowed.
	/// </summary>
	public void LoadNames(string parent, List<string> names, YamlNodeReader reader)
	{
		ArgumentNullException.ThrowIfNull(names);
		LoadNamesStandardHelper(parent, names, reader);
	}

	/// <summary>
	/// Loads a list of names where order of items does not matter.
	/// Another mod can remove or add new values without altering the whole list.
	/// </summary>
	public void LoadUnorderedNames(string parent, List<string> names, YamlNodeReader reader)
	{
		ArgumentNullException.ThrowIfNull(names);
		LoadNamesEditableHelper(parent, names, reader);
	}

	/// <summary>Loads a map from names to names (keeps the order of the file).</summary>
	public void LoadNamesToNames(string parent, List<(string first, List<string> second)> names, YamlNodeReader reader)
	{
		ArgumentNullException.ThrowIfNull(names);
		LoadPairListEditable(parent, names, reader, () => [], LoadNamesStandardHelper, LoadNamesEditableHelper);
	}

	/// <summary>Loads a map from names to names.</summary>
	public void LoadUnorderedNamesToNames(string parent, SortedDictionary<string, string> names, YamlNodeReader reader)
	{
		ArgumentNullException.ThrowIfNull(names);
		LoadMapEditable(parent, names, reader, () => "", LoadHelperName, LoadHelperName);
	}

	/// <summary>Loads a map from names to ints.</summary>
	public void LoadUnorderedNamesToInt(string parent, SortedDictionary<string, int> names, YamlNodeReader reader)
	{
		ArgumentNullException.ThrowIfNull(names);
		LoadMapEditable(parent, names, reader, () => 0, LoadHelperInt, LoadHelperInt);
	}

	/// <summary>Loads a map from names to vector of ints.</summary>
	public void LoadUnorderedNamesToInts(string parent, SortedDictionary<string, List<int>> names, YamlNodeReader reader)
	{
		ArgumentNullException.ThrowIfNull(names);
		LoadMapEditable(parent, names, reader, () => [], LoadIntsStandardHelper, LoadIntsStandardHelper);
	}

	/// <summary>Loads a map from names to names to int.</summary>
	public void LoadUnorderedNamesToNamesToInt(string parent, SortedDictionary<string, SortedDictionary<string, int>> names, YamlNodeReader reader)
	{
		ArgumentNullException.ThrowIfNull(names);
		LoadMapEditable(parent, names, reader, () => new SortedDictionary<string, int>(StringComparer.Ordinal), LoadNamesToIntStandardHelper, LoadNamesToIntEditableHelper);
	}

	/// <summary>Loads data for kill criteria from Commendations.</summary>
	public void LoadKillCriteria(string parent, List<List<(int first, List<string> second)>> names, YamlNodeReader reader)
	{
		ArgumentNullException.ThrowIfNull(names);
		//TODO: very specific use case, not all levels fully supported
		if (reader)
		{
			List<(int first, List<string> second)> loadInner(YamlNodeReader n)
			{
				ShowInfo(parent, n);

				if (!IsListHelper(n))
				{
					ThrowOnBadListHelper(parent, n);
				}
				return n.ReadVal<List<(int first, List<string> second)>>();
			}

			ShowInfo(parent, reader, AddTag);

			if (IsListHelper(reader))
			{
				names.Clear();
				foreach (var n in reader.Children())
				{
					names.Add(loadInner(n));
				}
			}
			else if (IsListAddTagHelper(reader))
			{
				foreach (var n in reader.Children())
				{
					names.Add(loadInner(n));
				}
			}
			else
			{
				ThrowOnBadListHelper(parent, reader);
			}
		}
	}

#pragma warning restore CA1822

	/// <summary>Cross links the rule objects of one kind; collects up to 30 errors before throwing.</summary>
	private static void AfterLoadHelper<T>(string name, Mod mod, SortedDictionary<string, T> list, Action<T, Mod> func)
	{
		var errorStream = new StringBuilder();
		const int errorLimit = 30;
		int errorCount = 0;

		errorStream.Append("During linking rulesets of ").Append(name).Append(":\n");
		foreach (var rule in list)
		{
			try
			{
				func(rule.Value, mod);
			}
			catch (LoadRuleException e)
			{
				++errorCount;
				errorStream.Append(e.Message).Append('\n');
				if (errorCount == errorLimit)
				{
					break;
				}
			}
			catch (Exception e) when (IsOxceException(e))
			{
				++errorCount;
				errorStream.Append("Error processing '").Append(rule.Key).Append("' in ").Append(name).Append(": ").Append(e.Message).Append('\n');
				if (errorCount == errorLimit)
				{
					break;
				}
			}
		}
		if (errorCount != 0)
		{
			throw new InvalidOperationException(errorStream.ToString());
		}
	}

	/// <summary>Helper function used to disable invalid mod and throw exception to quit game.</summary>
	/// <param name="modId">Mod id.</param>
	/// <param name="error">Error message.</param>
	[DoesNotReturn]
	private static void ThrowModOnErrorHelper(string modId, string error)
	{
		var errorStream = new StringBuilder();

		errorStream.Append("failed to load '").Append(Options.GetModInfos()[modId].Name).Append('\'');

		if (!Options.debug)
		{
			Logger.Warning("disabling mod with invalid ruleset: " + modId);
			int it = Options.mods.FindIndex(m => m.Id == modId && m.Active);
			if (it < 0)
			{
				Logger.Error("cannot find broken mod in mods list: " + modId);
				Logger.Error("clearing mods list");
				Options.mods.Clear();
			}
			else
			{
				Options.mods[it].Active = false;
			}
			Options.Save();

			errorStream.Append("; mod disabled");
		}
		errorStream.Append('\n').Append(error);

		throw new InvalidOperationException(errorStream.ToString());
	}

	/// <summary>
	/// Loads a list of mods specified in the options.
	/// List of &lt;modId, rulesetFiles&gt; pairs is fetched from the FileMap / VFS
	/// being set up in options updateMods.
	/// </summary>
	public void LoadAll() => LoadAll(loadResources: true);

	/// <summary>Loads a list of mods specified in the options (see <see cref="LoadAll()"/>).</summary>
	/// <param name="loadResources">
	/// False only for tests that load the rulesets without the original game resources: the vanilla
	/// resources, extra resources and their modifications are skipped (the test registers empty sets instead).
	/// </param>
	internal void LoadAll(bool loadResources)
	{
		// script hooks: ModScript parser (not ported)
		var mods = FileMap.GetRulesets();

		Logger.Info("Loading begins...");
		if (Options.oxceModValidationLevel < (int)SeverityLevel.LOG_ERROR)
		{
			Logger.Error("Validation of mod data disabled, game can crash when run");
		}
		else if (Options.oxceModValidationLevel < (int)SeverityLevel.LOG_WARNING)
		{
			Logger.Warning("Validation of mod data reduced, game can behave incorrectly");
		}
		_modData.Clear();
		for (int i = 0; i < mods.Count; ++i)
		{
			_modData.Add(new ModData());
		}

		var usedModNames = new HashSet<string>(StringComparer.Ordinal) { ModNameMaster, ModNameCurrent };

		// calculated offsets and other things for all mods
		int offset = 0;
		for (int i = 0; mods.Count > i; ++i)
		{
			string modId = mods[i].ModId;
			if (!usedModNames.Add(modId))
			{
				ThrowModOnErrorHelper(modId, "this mod name is already used");
			}
			// script hooks: _scriptGlobal->addMod(modId, 1000 * offset)
			ModInfo modInfo = Options.GetModInfos()[modId];
			int size = modInfo.ReservedSpace;
			_modData[i].name = modId;
			_modData[i].offset = 1000 * offset;
			_modData[i].info = modInfo;
			_modData[i].size = 1000 * size;
			offset += size;
		}

		Logger.Info("Pre-loading rulesets...");
		// load rulesets that can affect loading vanilla resources
		for (int i = 0; _modData.Count > i; ++i)
		{
			_modCurrent = _modData[i];
			//if (_modCurrent->info->isMaster())
			{
				var file = FileMap.GetModRuleFile(_modCurrent.info, _modCurrent.info.ResourceConfigFile);
				if (file != null)
				{
					LoadResourceConfigFile(file);
				}
			}
		}

		Logger.Info("Loading vanilla resources...");
		// vanilla resources load
		_modCurrent = _modData[0];
		if (loadResources)
		{
			LoadVanillaResources();
		}
		_surfaceOffsetBasebits = _sets["BASEBITS.PCK"].MaxSharedFrames;
		_surfaceOffsetBigobs = _sets["BIGOBS.PCK"].MaxSharedFrames;
		_surfaceOffsetFloorob = _sets["FLOOROB.PCK"].MaxSharedFrames;
		_surfaceOffsetHandob = _sets["HANDOB.PCK"].MaxSharedFrames;
		_surfaceOffsetHit = _sets["HIT.PCK"].MaxSharedFrames;
		_surfaceOffsetSmoke = _sets["SMOKE.PCK"].MaxSharedFrames;

		_soundOffsetBattle = _sounds["BATTLE.CAT"].MaxSharedSounds;
		_soundOffsetGeo = _sounds["GEO.CAT"].MaxSharedSounds;

		Logger.Info("Loading rulesets...");
		// load rest rulesets
		for (int i = 0; mods.Count > i; ++i)
		{
			try
			{
				_modCurrent = _modData[i];
				// script hooks: _scriptGlobal->setMod(_modCurrent->offset)
				LoadMod(mods[i].Rulesets);
			}
			catch (Exception e) when (IsOxceException(e))
			{
				string modId = mods[i].ModId;
				ThrowModOnErrorHelper(modId, e.Message);
			}
		}
		Logger.Info("Loading rulesets done.");

		//back master
		_modCurrent = _modData[0];
		// script hooks: _scriptGlobal->endLoad()
		_currentFile = "After-load validation";

		AfterLoad(loadResources);

		Logger.Info("Loading ended.");

		SortLists();
		if (loadResources)
		{
			ModResources();
		}
	}

	/// <summary>Second half of <see cref="LoadAll()"/> (after all rulesets are read): post-processing and cross-linking.</summary>
	/// <param name="loadExtraResources">False only for tests that load the rulesets without any game resources.</param>
	private void AfterLoad(bool loadExtraResources)
	{
		// post-processing item categories
		var replacementRules = new SortedDictionary<string, string>(StringComparer.Ordinal);
		foreach (var pair in _itemCategories)
		{
			if (pair.Value.ReplaceBy.Length != 0)
			{
				replacementRules[pair.Key] = pair.Value.ReplaceBy;
			}
		}
		foreach (var pair in _items)
		{
			pair.Value.UpdateCategories(replacementRules);
		}

		// find out if paperdoll overlaps with inventory slots
		int x1 = RuleInventory.PAPERDOLL_X;
		int y1 = RuleInventory.PAPERDOLL_Y;
		int w1 = RuleInventory.PAPERDOLL_W;
		int h1 = RuleInventory.PAPERDOLL_H;
		foreach (var invCategory in _invs)
		{
			foreach (var invSlot in invCategory.Value.Slots)
			{
				int x2 = invCategory.Value.X + (invSlot.x * RuleInventory.SLOT_W);
				int y2 = invCategory.Value.Y + (invSlot.y * RuleInventory.SLOT_H);
				int w2 = RuleInventory.SLOT_W;
				int h2 = RuleInventory.SLOT_H;
				if (x1 + w1 < x2 || x2 + w2 < x1 || y1 + h1 < y2 || y2 + h2 < y1)
				{
					// intersection is empty
				}
				else
				{
					_inventoryOverlapsPaperdoll = true;
				}
			}
		}

		if (loadExtraResources)
		{
			LoadExtraResources();
		}

		Logger.Info("After load.");
		// cross link rule objects

		AfterLoadHelper("research", this, _research, (r, m) => r.AfterLoad(m));
		AfterLoadHelper("items", this, _items, (r, m) => r.AfterLoad(m));
		AfterLoadHelper("weaponSets", this, _weaponSets, (r, m) => r.AfterLoad(m));
		AfterLoadHelper("manufacture", this, _manufacture, (r, m) => r.AfterLoad(m));
		AfterLoadHelper("armors", this, _armors, (r, m) => r.AfterLoad(m));
		AfterLoadHelper("units", this, _units, (r, m) => r.AfterLoad(m));
		AfterLoadHelper("soldiers", this, _soldiers, (r, m) => r.AfterLoad(m));
		AfterLoadHelper("facilities", this, _facilities, (r, m) => r.AfterLoad(m));
		AfterLoadHelper("startingConditions", this, _startingConditions, (r, m) => r.AfterLoad(m));
		AfterLoadHelper("enviroEffects", this, _enviroEffects, (r, m) => r.AfterLoad(m));
		AfterLoadHelper("commendations", this, _commendations, (r, m) => r.AfterLoad(m));
		AfterLoadHelper("skills", this, _skills, (r, m) => r.AfterLoad(m));
		AfterLoadHelper("craftWeapons", this, _craftWeapons, (r, m) => r.AfterLoad(m));
		AfterLoadHelper("countries", this, _countries, (r, m) => r.AfterLoad(m));
		AfterLoadHelper("crafts", this, _crafts, (r, m) => r.AfterLoad(m));
		AfterLoadHelper("events", this, _events, (r, m) => r.AfterLoad(m));

		foreach (var a in _armors)
		{
			if (a.Value.HasInfiniteSupply)
			{
				_armorsForSoldiersCache.Add(a.Value);
			}
			else if (a.Value.StoreItem != null)
			{
				_armorsForSoldiersCache.Add(a.Value);
				_armorStorageItemsCache.Add(a.Value.StoreItem);
			}
		}
		//_armorsForSoldiersCache sorted in sortList()

		foreach (var c in _craftWeapons)
		{
			RuleItem item = c.Value.LauncherItem;
			if (item != null)
			{
				_craftWeaponStorageItemsCache.Add(item);
			}

			item = c.Value.ClipItem;
			if (item != null)
			{
				_craftWeaponStorageItemsCache.Add(item);
			}
		}

		foreach (var r in _research)
		{
			if (r.Value.UnlockFinalMission())
			{
				if (_finalResearch != null)
				{
					CheckForSoftError(true, "mod", "Both '" + _finalResearch.Name + "' and '" + r.Value.Name + "' research are marked as 'unlockFinalMission: true'", SeverityLevel.LOG_INFO);

					// to make old mods semi-compatible with new code we decide that last updated rule will be consider final research. This could make false-positive as last update could not touch this flag.
					if (GetModLastUpdatingRule(r.Value).offset < GetModLastUpdatingRule(_finalResearch).offset)
					{
						continue;
					}
				}
				_finalResearch = r.Value;
			}
		}

		// check unique listOrder
		{
			var tmp = new List<int>(_soldierBonus.Count);
			foreach (var i in _soldierBonus)
			{
				tmp.Add(i.Value.ListOrder);
			}
			bool wasUnique = tmp.Distinct().Count() == tmp.Count;
			if (!wasUnique)
			{
				throw new InvalidOperationException("List order for soldier bonus types must be unique!");
			}
		}

		// auto-create alternative manufacture rules
		foreach (var shortcutPair in _manufactureShortcut)
		{
			// 1. check if the new project has a unique name
			var typeNew = shortcutPair.Key;
			if (_manufacture.ContainsKey(typeNew))
			{
				throw new InvalidOperationException("Manufacture project '" + typeNew + "' already exists! Choose a different name for this alternative project.");
			}

			// 2. copy an existing manufacture project
			RuleManufacture ruleStartFrom = GetManufacture(shortcutPair.Value.StartFrom, true);
			RuleManufacture ruleNew = new RuleManufacture(ruleStartFrom);
			_manufacture[typeNew] = ruleNew;
			_manufactureIndex.Add(typeNew);

			// 3. change the name and break down the sub-projects into simpler components
			ruleNew?.BreakDown(this, shortcutPair.Value);
		}

		// recommended user options
		if (_recommendedUserOptions.Count != 0 && !Options.oxceRecommendedOptionsWereSet)
		{
			_recommendedUserOptions.Remove("maximizeInfoScreens"); // FIXME: make proper categorisations in the next release
			_recommendedUserOptions.Remove("oxceModValidationLevel");

			foreach (var optionInfo in Options.GetOptionInfo())
			{
				if (optionInfo.Type != OptionType.OPTION_KEY && optionInfo.Category.Length != 0)
				{
					optionInfo.Load(_recommendedUserOptions, false);
				}
			}

			Options.oxceRecommendedOptionsWereSet = true;
			Options.Save();
		}

		// fixed user options
		if (_fixedUserOptions.Count != 0)
		{
			_fixedUserOptions.Remove("oxceLinks");
			_fixedUserOptions.Remove("oxceUpdateCheck");
			_fixedUserOptions.Remove("maximizeInfoScreens"); // FIXME: make proper categorisations in the next release
			_fixedUserOptions.Remove("oxceModValidationLevel");
			_fixedUserOptions.Remove("oxceAutoNightVisionThreshold");
			_fixedUserOptions.Remove("oxceAlternateCraftEquipmentManagement");

			foreach (var optionInfo in Options.GetOptionInfo())
			{
				if (optionInfo.Type != OptionType.OPTION_KEY && optionInfo.Category.Length != 0)
				{
					optionInfo.Load(_fixedUserOptions, false);
				}
			}
			Options.Save();
		}

		// additional validation of options not visible in the GUI
		{
			if (Options.oxceMaxEquipmentLayoutTemplates < 10 ||
				Options.oxceMaxEquipmentLayoutTemplates > SavedGame.MAX_EQUIPMENT_LAYOUT_TEMPLATES ||
				Options.oxceMaxEquipmentLayoutTemplates % 10 != 0)
			{
				Options.oxceMaxEquipmentLayoutTemplates = 20;
			}
		}
	}

	/// <summary>
	/// Loads a list of rulesets from YAML files for the current mod. The first
	/// mod loaded should be the master at index 0, then 1, and so on.
	/// </summary>
	/// <param name="rulesetFiles">List of rulesets to load.</param>
	internal void LoadMod(IReadOnlyList<FileMap.FileRecord> rulesetFiles)
	{
		ArgumentNullException.ThrowIfNull(rulesetFiles);
		var sortedRulesetFiles = rulesetFiles.ToList();
		// C++ std::sort with "a.fullpath > b.fullpath" (byte order, descending); paths are unique
		sortedRulesetFiles.Sort((a, b) => string.CompareOrdinal(b.FullPath, a.FullPath));
		foreach (var filerec in sortedRulesetFiles)
		{
			Logger.Verbose("- " + filerec.FullPath);
			try
			{
				_currentFile = filerec.FullPath;
				LoadFile(filerec);
			}
			catch (Exception e) when (IsOxceException(e))
			{
				throw new InvalidOperationException(filerec.FullPath + ": " + e.Message, e);
			}
		}

		// these need to be validated, otherwise we're gonna get into some serious trouble down the line.
		// it may seem like a somewhat arbitrary limitation, but there is a good reason behind it.
		// i'd need to know what results are going to be before they are formulated, and there's a hierarchical structure to
		// the order in which variables are determined for a mission, and the order is DIFFERENT for regular missions vs
		// missions that spawn a mission site. where normally we pick a region, then a mission based on the weights for that region.
		// a terror-type mission picks a mission type FIRST, then a region based on the criteria defined by the mission.
		// there is no way i can conceive of to reconcile this difference to allow mixing and matching,
		// short of knowing the results of calls to the RNG before they're determined.
		// the best solution i can come up with is to disallow it, as there are other ways to achieve what this would amount to anyway,
		// and they don't require time travel. - Warboy
		foreach (var map in new[] { _missionScripts, _adhocScripts })
		{
			foreach (var pair in map)
			{
				RuleMissionScript rule = pair.Value;
				var missions = rule.AllMissionTypes;
				if (missions.Count != 0)
				{
					string first = missions.First();
					if (GetAlienMission(first) == null)
					{
						throw new InvalidOperationException("Error with MissionScript: " + pair.Key + ": alien mission type: " + first + " not defined, do not incite the judgement of Amaunator.");
					}
					bool isSiteType = GetAlienMission(first).Objective == MissionObjective.OBJECTIVE_SITE;
					rule.SiteType = isSiteType;
					foreach (var j in missions)
					{
						if (GetAlienMission(j) != null && (GetAlienMission(j).Objective == MissionObjective.OBJECTIVE_SITE) != isSiteType)
						{
							throw new InvalidOperationException("Error with MissionScript: " + pair.Key + ": cannot mix terror/non-terror missions in a single command, so sayeth the wise Alaundo.");
						}
					}
				}
			}
		}

		// instead of passing a pointer to the region load function and moving the alienMission loading before region loading
		// and sanitizing there, i'll sanitize here, i'm sure this sanitation will grow, and will need to be refactored into
		// its own function at some point, but for now, i'll put it here next to the missionScript sanitation, because it seems
		// the logical place for it, given that this sanitation is required as a result of moving all terror mission handling
		// into missionScripting behaviour. apologies to all the modders that will be getting errors and need to adjust their
		// rulesets, but this will save you weird errors down the line.
		foreach (var pair in _regions)
		{
			var names = pair.Value.AvailableMissions.Names;
			foreach (var name in names)
			{
				if (GetAlienMission(name) == null)
				{
					throw new InvalidOperationException("Error with MissionWeights: Region: " + pair.Key + ": alien mission type: " + name + " not defined, do not incite the judgement of Amaunator.");
				}
				if (GetAlienMission(name).Objective == MissionObjective.OBJECTIVE_SITE)
				{
					throw new InvalidOperationException("Error with MissionWeights: Region: " + pair.Key + " has " + name + " listed. Terror mission can only be invoked via missionScript, so sayeth the Spider Queen.");
				}
			}
		}
	}

	/// <summary>Loads a ruleset from a YAML file that has basic resources configuration.</summary>
	private void LoadResourceConfigFile(FileMap.FileRecord filerec)
	{
		var reader = filerec.GetYaml();

		foreach (var soundDefReader in reader["soundDefs"].Children())
		{
			SoundDefinition rule = LoadRule(soundDefReader, _soundDefs, null, "type", type => new SoundDefinition(type));
			rule?.Load(soundDefReader);
		}

		var luts = reader["transparencyLUTs"];
		if (luts)
		{
			int start = _modCurrent.offset / ModTransparencySizeReduction;
			int limit = _modCurrent.size / ModTransparencySizeReduction;
			int curr = 0;

			// C++ std::vector::resize
			if (_transparencies.Count > start + limit)
				_transparencies.RemoveRange(start + limit, _transparencies.Count - (start + limit));
			while (_transparencies.Count < start + limit)
				_transparencies.Add(new Rgba[TransparenciesOpacityLevels]);

			foreach (var lut in luts.Children())
			{
				var colors = lut["colors"];
				if (colors.IsSeq)
				{
					foreach (var colorReader in colors.Children())
					{
						if (curr == limit)
						{
							throw new InvalidOperationException("transparencyLUTs mod limit reach");
						}

						int loadByteValue(YamlNodeReader n)
						{
							int v = n.ReadVal(-1);
							CheckForSoftError(v < 0 || v > 255, "transparencyLUTs", n, "value outside allowed range");
							return Math.Clamp(v, 0, 255);
						}

						var target = _transparencies[start + curr];
						if (colorReader[0].HasVal)
						{
							byte r = (byte)loadByteValue(colorReader[0]);
							byte g = (byte)loadByteValue(colorReader[1]);
							byte b = (byte)loadByteValue(colorReader[2]);
							byte unused = colorReader[3] ? (byte)loadByteValue(colorReader[3]) : (byte)2;

							for (int opacity = 0; opacity < TransparenciesOpacityLevels; ++opacity)
							{
								// pseudo interpolation of palette color with tint
								// for small values `op` its should behave same as original TFTD
								// but for bigger values it make result closer to tint color
								int op = Math.Clamp((opacity + 1) * unused, 0, 64);
								float co = 1.0f - Sqr(op / 64.0f); // 1.0 -> 0.0
								float to = op * 1.0f; // 0.0 -> 64.0

								target[opacity] = new Rgba(
									(byte)Math.Clamp((int)(r * to), 0, 255),
									(byte)Math.Clamp((int)(g * to), 0, 255),
									(byte)Math.Clamp((int)(b * to), 0, 255),
									(byte)(255 * co));
							}
						}
						else
						{
							for (int opacity = 0; opacity < TransparenciesOpacityLevels; ++opacity)
							{
								var n = colorReader[opacity];

								byte r = (byte)loadByteValue(n[0]);
								byte g = (byte)loadByteValue(n[1]);
								byte b = (byte)loadByteValue(n[2]);
								byte unused = (byte)(255 - loadByteValue(n[3]));
								target[opacity] = new Rgba(r, g, b, unused);
							}
							Array.Reverse(target);
						}
						curr++;
					}
				}
				else
				{
					throw new InvalidOperationException("unknown transparencyLUTs node type");
				}
			}
		}
	}

	private static float Sqr(float x) => x * x;

	private static int Sqr(int x) => x * x;

	/// <summary>Loads "constants" node.</summary>
	private void LoadConstants(YamlNodeReader reader)
	{
		LoadSoundOffset("constants", ref DOOR_OPEN, reader["doorSound"], "BATTLE.CAT");
		LoadSoundOffset("constants", ref SLIDING_DOOR_OPEN, reader["slidingDoorSound"], "BATTLE.CAT");
		LoadSoundOffset("constants", ref SLIDING_DOOR_CLOSE, reader["slidingDoorClose"], "BATTLE.CAT");
		LoadSoundOffset("constants", ref SMALL_EXPLOSION, reader["smallExplosion"], "BATTLE.CAT");
		LoadSoundOffset("constants", ref LARGE_EXPLOSION, reader["largeExplosion"], "BATTLE.CAT");

		LoadSpriteOffset("constants", ref EXPLOSION_OFFSET, reader["explosionOffset"], "X1.PCK");
		LoadSpriteOffset("constants", ref SMOKE_OFFSET, reader["smokeOffset"], "SMOKE.PCK");
		LoadSpriteOffset("constants", ref UNDERWATER_SMOKE_OFFSET, reader["underwaterSmokeOffset"], "SMOKE.PCK");

		LoadSoundOffset("constants", ref ITEM_DROP, reader["itemDrop"], "BATTLE.CAT");
		LoadSoundOffset("constants", ref ITEM_THROW, reader["itemThrow"], "BATTLE.CAT");
		LoadSoundOffset("constants", ref ITEM_RELOAD, reader["itemReload"], "BATTLE.CAT");
		LoadSoundOffset("constants", ref WALK_OFFSET, reader["walkOffset"], "BATTLE.CAT");
		LoadSoundOffset("constants", ref FLYING_SOUND, reader["flyingSound"], "BATTLE.CAT");

		LoadSoundOffset("constants", ref BUTTON_PRESS, reader["buttonPress"], "GEO.CAT");
		var arrayReader = reader["windowPopup"];
		if (arrayReader)
			for (int j = 0; j < WINDOW_POPUP.Length; j++)
				LoadSoundOffset("constants", ref WINDOW_POPUP[j], arrayReader[j], "GEO.CAT");

		LoadSoundOffset("constants", ref UFO_FIRE, reader["ufoFire"], "GEO.CAT");
		LoadSoundOffset("constants", ref UFO_HIT, reader["ufoHit"], "GEO.CAT");
		LoadSoundOffset("constants", ref UFO_CRASH, reader["ufoCrash"], "GEO.CAT");
		LoadSoundOffset("constants", ref UFO_EXPLODE, reader["ufoExplode"], "GEO.CAT");
		LoadSoundOffset("constants", ref INTERCEPTOR_HIT, reader["interceptorHit"], "GEO.CAT");
		LoadSoundOffset("constants", ref INTERCEPTOR_EXPLODE, reader["interceptorExplode"], "GEO.CAT");
		reader.TryRead("geoscapeCursor", ref GEOSCAPE_CURSOR);
		reader.TryRead("basescapeCursor", ref BASESCAPE_CURSOR);
		reader.TryRead("battlescapeCursor", ref BATTLESCAPE_CURSOR);
		reader.TryRead("ufopaediaCursor", ref UFOPAEDIA_CURSOR);
		reader.TryRead("graphsCursor", ref GRAPHS_CURSOR);
		reader.TryRead("damageRange", ref DAMAGE_RANGE);
		reader.TryRead("explosiveDamageRange", ref EXPLOSIVE_DAMAGE_RANGE);
		arrayReader = reader["fireDamageRange"];
		if (arrayReader)
			for (int j = 0; j < FIRE_DAMAGE_RANGE.Length; j++)
				arrayReader[j].TryReadVal(ref FIRE_DAMAGE_RANGE[j]);
		reader.TryRead("goodDebriefingMusic", ref DEBRIEF_MUSIC_GOOD);
		reader.TryRead("badDebriefingMusic", ref DEBRIEF_MUSIC_BAD);
		arrayReader = reader["extendedPediaFacilityParams"];
		if (arrayReader)
			for (int j = 0; j < PEDIA_FACILITY_RENDER_PARAMETERS.Length; j++)
				arrayReader[j].TryReadVal(ref PEDIA_FACILITY_RENDER_PARAMETERS[j]);
		reader.TryRead("extendedItemReloadCost", ref EXTENDED_ITEM_RELOAD_COST);
		reader.TryRead("extendedInventorySlotSorting", ref EXTENDED_INVENTORY_SLOT_SORTING);
		reader.TryRead("extendedRunningCost", ref EXTENDED_RUNNING_COST);
		reader.TryRead("extendedMovementCostRounding", ref EXTENDED_MOVEMENT_COST_ROUNDING);
		reader.TryRead("extendedHwpLoadOrder", ref EXTENDED_HWP_LOAD_ORDER);
		reader.TryRead("extendedSpotOnHitForSniping", ref EXTENDED_SPOT_ON_HIT_FOR_SNIPING);
		reader.TryRead("extendedBerserkWithAimed", ref EXTENDED_BERSERK_WITH_AIMED);
		reader.TryRead("extendedMeleeReactions", ref EXTENDED_MELEE_REACTIONS);
		reader.TryRead("extendedTerrainMelee", ref EXTENDED_TERRAIN_MELEE);
		reader.TryRead("extendedUnderwaterThrowFactor", ref EXTENDED_UNDERWATER_THROW_FACTOR);
		reader.TryRead("extendedExperienceAwardSystem", ref EXTENDED_EXPERIENCE_AWARD_SYSTEM);
		reader.TryRead("extendedForceSpawn", ref EXTENDED_FORCE_SPAWN);
		reader.TryRead("extendedSmokeOffset", ref EXTENDED_SMOKE_OFFSET);

		reader.TryRead("extendedCurrencySymbol", ref Unicode.OXCE_CURRENCY_SYMBOL);
	}

	/// <summary>
	/// C++ <c>YamlNodeReader::emitDescendants(defaultValuesReader)</c>: copies the children of the node and
	/// appends the children of the default node whose key is missing, then emits the result.
	/// </summary>
	private static string EmitDescendants(YamlNodeReader reader, YamlNodeReader defaultValuesReader)
	{
		YamlWriteNode root;
		if (reader.IsMap)
			root = new YamlWriteNode { Kind = YamlWriteNode.NodeKind.Map };
		else if (reader.IsSeq)
			root = new YamlWriteNode { Kind = YamlWriteNode.NodeKind.Seq };
		else
			return "";
		foreach (var child in reader.Node.Children)
			root.Children.Add(YamlEmitter.FromReader(child));
		if (defaultValuesReader)
		{
			foreach (var child in defaultValuesReader.Node.Children)
			{
				if (!root.Children.Exists(c => c.Key == child.Key))
					root.Children.Add(YamlEmitter.FromReader(child));
			}
		}
		return YamlEmitter.Emit(root);
	}

	/// <summary>
	/// Loads a ruleset's contents from a YAML file.
	/// Rules that match pre-existing rules overwrite them.
	/// </summary>
	private void LoadFile(FileMap.FileRecord filerec) => LoadFile(filerec.GetYaml());

	/// <summary>Second half of <see cref="LoadFile(FileMap.FileRecord)"/>, split off so tests can load rulesets from memory.</summary>
	internal void LoadFile(YamlRootNodeReader r)
	{
		ArgumentNullException.ThrowIfNull(r);
		var reader = r.UseIndex();

		YamlNodeReader loadDocInfoHelper(string nodeName)
		{
			if (reader.HasValTagNamed(InfoTag))
			{
				Logger.Info("Available rule '" + nodeName + ":'");
			}
			return reader[nodeName];
		}

		var extended = loadDocInfoHelper("extended");
		if (extended)
		{
			var t = extended["tagsFile"];
			if (t)
			{
				var filePath = t.ReadVal<string>();
				var file = FileMap.GetModRuleFile(_modCurrent.info, filePath);

				if (!CheckForSoftError(file == null, "extended", t, "Unknown file name for 'tagsFile': '" + filePath + "'", SeverityLevel.LOG_ERROR))
				{
					// script hooks: the tags of that file are copied into the script globals (not ported)
				}
			}

			// script hooks: _scriptGlobal->load(extended), script values "globals" (not ported)
		}

		IReadOnlyList<YamlNodeReader> iterateRules(string nodeName, string type)
		{
			var node = loadDocInfoHelper(nodeName);

			LoadRuleInfoHelper(node, nodeName, type);

			return node.Children();
		}

		IReadOnlyList<YamlNodeReader> iterateRulesSpecific(string nodeName)
		{
			var node = loadDocInfoHelper(nodeName);

			return node.Children();
		}

		foreach (var ruleReader in iterateRules("countries", "type"))
		{
			RuleCountry rule = LoadRule(ruleReader, _countries, _countriesIndex, "type", type => new RuleCountry(type));
			rule?.Load(ruleReader, this);
		}
		foreach (var ruleReader in iterateRules("extraGlobeLabels", "type"))
		{
			RuleCountry rule = LoadRule(ruleReader, _extraGlobeLabels, _extraGlobeLabelsIndex, "type", type => new RuleCountry(type));
			rule?.Load(ruleReader, this);
		}
		foreach (var ruleReader in iterateRules("regions", "type"))
		{
			RuleRegion rule = LoadRule(ruleReader, _regions, _regionsIndex, "type", type => new RuleRegion(type));
			rule?.Load(ruleReader, this);
		}
		foreach (var ruleReader in iterateRules("facilities", "type"))
		{
			RuleBaseFacility rule = LoadRule(ruleReader, _facilities, _facilitiesIndex, "type", type => { _facilityListOrder += 100; return new RuleBaseFacility(type, _facilityListOrder); });
			rule?.Load(ruleReader, this);
		}
		foreach (var ruleReader in iterateRules("crafts", "type"))
		{
			RuleCraft rule = LoadRule(ruleReader, _crafts, _craftsIndex, "type", type => { _craftListOrder += 100; return new RuleCraft(type, _craftListOrder); });
			rule?.Load(ruleReader, this);
		}
		foreach (var ruleReader in iterateRules("craftWeapons", "type"))
		{
			RuleCraftWeapon rule = LoadRule(ruleReader, _craftWeapons, _craftWeaponsIndex, "type", type => new RuleCraftWeapon(type));
			rule?.Load(ruleReader, this);
		}
		foreach (var ruleReader in iterateRules("itemCategories", "type"))
		{
			RuleItemCategory rule = LoadRule(ruleReader, _itemCategories, _itemCategoriesIndex, "type", type => { _itemCategoryListOrder += 100; return new RuleItemCategory(type, _itemCategoryListOrder); });
			rule?.Load(ruleReader, this);
		}
		foreach (var ruleReader in iterateRules("items", "type"))
		{
			RuleItem rule = LoadRule(ruleReader, _items, _itemsIndex, "type", type => { _itemListOrder += 100; return new RuleItem(type, _itemListOrder); });
			rule?.Load(ruleReader, this);
		}
		foreach (var ruleReader in iterateRules("weaponSets", "type"))
		{
			RuleWeaponSet rule = LoadRule(ruleReader, _weaponSets, null, "type", type => new RuleWeaponSet(type));
			rule?.Load(ruleReader, this);
		}
		foreach (var ruleReader in iterateRules("ufos", "type"))
		{
			RuleUfo rule = LoadRule(ruleReader, _ufos, _ufosIndex, "type", type => new RuleUfo(type));
			rule?.Load(ruleReader, this);
		}
		foreach (var ruleReader in iterateRules("invs", "id"))
		{
			RuleInventory rule = LoadRule(ruleReader, _invs, _invsIndex, "id", type => { _invListOrder += 10; return new RuleInventory(type, _invListOrder); });
			rule?.Load(ruleReader);
		}
		foreach (var ruleReader in iterateRules("terrains", "name"))
		{
			RuleTerrain rule = LoadRule(ruleReader, _terrains, _terrainIndex, "name", type => new RuleTerrain(type));
			rule?.Load(ruleReader, this);
		}

		foreach (var ruleReader in iterateRules("armors", "type"))
		{
			Armor rule = LoadRule(ruleReader, _armors, _armorsIndex, "type", type => { _armorListOrder += 100; return new Armor(type, _armorListOrder); });
			rule?.Load(ruleReader, this);
		}
		foreach (var ruleReader in iterateRules("skills", "type"))
		{
			RuleSkill rule = LoadRule(ruleReader, _skills, _skillsIndex, "type", type => new RuleSkill(type));
			rule?.Load(ruleReader, this);
		}
		foreach (var ruleReader in iterateRules("soldiers", "type"))
		{
			RuleSoldier rule = LoadRule(ruleReader, _soldiers, _soldiersIndex, "type", type => { _soldierListOrder += 1; return new RuleSoldier(type, _soldierListOrder); });
			rule?.Load(ruleReader, this);
		}
		foreach (var ruleReader in iterateRules("units", "type"))
		{
			Unit rule = LoadRule(ruleReader, _units, null, "type", type => new Unit(type));
			rule?.Load(ruleReader, this);
		}
		foreach (var ruleReader in iterateRules("alienRaces", "id"))
		{
			AlienRace rule = LoadRule(ruleReader, _alienRaces, _aliensIndex, "id", type => { _alienRaceListOrder += 100; return new AlienRace(type, _alienRaceListOrder); });
			rule?.Load(ruleReader, this);
		}
		foreach (var ruleReader in iterateRules("enviroEffects", "type"))
		{
			RuleEnviroEffects rule = LoadRule(ruleReader, _enviroEffects, _enviroEffectsIndex, "type", type => new RuleEnviroEffects(type));
			rule?.Load(ruleReader, this);
		}
		foreach (var ruleReader in iterateRules("startingConditions", "type"))
		{
			RuleStartingCondition rule = LoadRule(ruleReader, _startingConditions, _startingConditionsIndex, "type", type => new RuleStartingCondition(type));
			rule?.Load(ruleReader, this);
		}
		foreach (var ruleReader in iterateRules("alienDeployments", "type"))
		{
			AlienDeployment rule = LoadRule(ruleReader, _alienDeployments, _deploymentsIndex, "type", type => new AlienDeployment(type));
			rule?.Load(ruleReader, this);
		}
		foreach (var ruleReader in iterateRules("research", "name"))
		{
			RuleResearch rule = LoadRule(ruleReader, _research, _researchIndex, "name", type => { _researchListOrder += 100; return new RuleResearch(type, _researchListOrder); });
			rule?.Load(ruleReader, this);
		}
		foreach (var ruleReader in iterateRules("manufacture", "name"))
		{
			RuleManufacture rule = LoadRule(ruleReader, _manufacture, _manufactureIndex, "name", type => { _manufactureListOrder += 100; return new RuleManufacture(type, _manufactureListOrder); });
			rule?.Load(ruleReader, this);
		}
		foreach (var ruleReader in iterateRules("manufactureShortcut", "name"))
		{
			RuleManufactureShortcut rule = LoadRule(ruleReader, _manufactureShortcut, null, "name", type => new RuleManufactureShortcut(type));
			rule?.Load(ruleReader);
		}
		foreach (var ruleReader in iterateRules("soldierBonuses", "name"))
		{
			RuleSoldierBonus rule = LoadRule(ruleReader, _soldierBonus, _soldierBonusIndex, "name", type => { _soldierBonusListOrder += 100; return new RuleSoldierBonus(type, _soldierBonusListOrder); });
			rule?.Load(ruleReader, this);
		}
		foreach (var ruleReader in iterateRules("soldierTransformation", "name"))
		{
			RuleSoldierTransformation rule = LoadRule(ruleReader, _soldierTransformation, _soldierTransformationIndex, "name", type => { _transformationListOrder += 100; return new RuleSoldierTransformation(type, _transformationListOrder); });
			rule?.Load(ruleReader, this);
		}
		foreach (var ruleReader in iterateRules("commendations", "type"))
		{
			RuleCommendations rule = LoadRule(ruleReader, _commendations, null, "type", type => new RuleCommendations(type));
			rule?.Load(ruleReader, this);
		}

		foreach (var ruleReader in iterateRules("ufoTrajectories", "id"))
		{
			UfoTrajectory rule = LoadRule(ruleReader, _ufoTrajectories, null, "id", type => new UfoTrajectory(type));
			rule?.Load(ruleReader);
		}
		foreach (var ruleReader in iterateRules("alienMissions", "type"))
		{
			RuleAlienMission rule = LoadRule(ruleReader, _alienMissions, _alienMissionsIndex, "type", type => new RuleAlienMission(type));
			rule?.Load(ruleReader);
		}
		foreach (var ruleReader in iterateRules("arcScripts", "type"))
		{
			RuleArcScript rule = LoadRule(ruleReader, _arcScripts, _arcScriptIndex, "type", type => new RuleArcScript(type));
			rule?.Load(ruleReader);
		}
		foreach (var ruleReader in iterateRules("eventScripts", "type"))
		{
			RuleEventScript rule = LoadRule(ruleReader, _eventScripts, _eventScriptIndex, "type", type => new RuleEventScript(type));
			rule?.Load(ruleReader);
		}
		foreach (var ruleReader in iterateRules("events", "name"))
		{
			RuleEvent rule = LoadRule(ruleReader, _events, _eventIndex, "name", type => new RuleEvent(type));
			rule?.Load(ruleReader);
		}
		foreach (var ruleReader in iterateRules("missionScripts", "type"))
		{
			RuleMissionScript rule = LoadRule(ruleReader, _missionScripts, _missionScriptIndex, "type", type => new RuleMissionScript(type));
			rule?.Load(ruleReader);
		}
		foreach (var ruleReader in iterateRules("adhocScripts", "type"))
		{
			RuleMissionScript rule = LoadRule(ruleReader, _adhocScripts, _adhocScriptIndex, "type", type => new RuleMissionScript(type));
			rule?.Load(ruleReader);
		}

		foreach (var ruleReader in iterateRulesSpecific("mapScripts"))
		{
			string type = ruleReader["type"].ReadVal<string>();
			if (ruleReader["delete"])
			{
				type = ruleReader["delete"].ReadVal(type);
			}
			if (_mapScripts.TryGetValue(type, out var oldScripts))
			{
				oldScripts.Clear(); // Collections::deleteAll
			}
			foreach (var commandsReader in ruleReader["commands"].Children())
			{
				MapScript mapScript = new MapScript();
				mapScript.Load(commandsReader);
				GetOrAdd(_mapScripts, type).Add(mapScript);
			}
		}

		foreach (var ruleReader in iterateRulesSpecific("ufopaedia"))
		{
			if (ruleReader["id"])
			{
				string id = ruleReader["id"].ReadVal<string>();
				ArticleDefinition rule;
				if (_ufopaediaArticles.TryGetValue(id, out var existing))
				{
					rule = existing;
				}
				else
				{
					if (!ruleReader["type_id"])
					{ // otherwise it throws and I wasted hours
						Logger.Error("ufopaedia item misses type_id attribute.");
						continue;
					}
					UfopaediaTypeId type = ruleReader["type_id"].ReadVal<UfopaediaTypeId>();
					switch (type)
					{
						case UfopaediaTypeId.UFOPAEDIA_TYPE_UNIT: rule = new ArticleDefinitionUnit(); break;
						case UfopaediaTypeId.UFOPAEDIA_TYPE_SOLDIER: rule = new ArticleDefinitionSoldier(); break;
						case UfopaediaTypeId.UFOPAEDIA_TYPE_CRAFT: rule = new ArticleDefinitionCraft(); break;
						case UfopaediaTypeId.UFOPAEDIA_TYPE_CRAFT_WEAPON: rule = new ArticleDefinitionCraftWeapon(); break;
						case UfopaediaTypeId.UFOPAEDIA_TYPE_VEHICLE: rule = new ArticleDefinitionVehicle(); break;
						case UfopaediaTypeId.UFOPAEDIA_TYPE_ITEM: rule = new ArticleDefinitionItem(); break;
						case UfopaediaTypeId.UFOPAEDIA_TYPE_ARMOR: rule = new ArticleDefinitionArmor(); break;
						case UfopaediaTypeId.UFOPAEDIA_TYPE_BASE_FACILITY: rule = new ArticleDefinitionBaseFacility(); break;
						case UfopaediaTypeId.UFOPAEDIA_TYPE_TEXTIMAGE: rule = new ArticleDefinitionTextImage(); break;
						case UfopaediaTypeId.UFOPAEDIA_TYPE_TEXT: rule = new ArticleDefinitionText(); break;
						case UfopaediaTypeId.UFOPAEDIA_TYPE_UFO: rule = new ArticleDefinitionUfo(); break;
						case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD:
						case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_CRAFT:
						case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_CRAFT_WEAPON:
						case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_VEHICLE:
						case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_ITEM:
						case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_ARMOR:
						case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_BASE_FACILITY:
						case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_USO:
							rule = new ArticleDefinitionTFTD();
							break;
						default: rule = null; break;
					}
					_ufopaediaArticles[id] = rule;
					_ufopaediaIndex.Add(id);
				}
				_ufopaediaListOrder += 100;
				rule.Load(ruleReader, _ufopaediaListOrder);
			}
			else if (ruleReader["delete"])
			{
				string type = ruleReader["delete"].ReadVal<string>();
				_ufopaediaArticles.Remove(type);
				_ufopaediaIndex.Remove(type);
			}
		}

		string loadStartingBase(string startingBaseType, string destRef)
		{
			// Bases can't be copied, so for savegame purposes we store the node instead
			var @base = loadDocInfoHelper(startingBaseType);
			if (@base)
			{
				if (IsMapHelper(@base))
				{
					return EmitDescendants(@base, new YamlRootNodeReader(destRef, "(starting base template)").ToBase());
				}
				else
				{
					throw new LoadRuleException(startingBaseType, @base, "expected normal map node");
				}
			}
			return destRef;
		}
		_startingBaseDefault = loadStartingBase("startingBase", _startingBaseDefault);
		_startingBaseBeginner = loadStartingBase("startingBaseBeginner", _startingBaseBeginner);
		_startingBaseExperienced = loadStartingBase("startingBaseExperienced", _startingBaseExperienced);
		_startingBaseVeteran = loadStartingBase("startingBaseVeteran", _startingBaseVeteran);
		_startingBaseGenius = loadStartingBase("startingBaseGenius", _startingBaseGenius);
		_startingBaseSuperhuman = loadStartingBase("startingBaseSuperhuman", _startingBaseSuperhuman);

		if (reader["startingTime"])
		{
			_startingTime.Load(reader["startingTime"]);
		}
		reader.TryRead("startingDifficulty", ref _startingDifficulty);
		reader.TryRead("maxViewDistance", ref _maxViewDistance);
		reader.TryRead("maxDarknessToSeeUnits", ref _maxDarknessToSeeUnits);
		reader.TryRead("costHireEngineer", ref _costHireEngineer);
		reader.TryRead("costHireScientist", ref _costHireScientist);
		reader.TryRead("costEngineer", ref _costEngineer);
		reader.TryRead("costScientist", ref _costScientist);
		reader.TryRead("timePersonnel", ref _timePersonnel);
		reader.TryRead("hireByCountryOdds", ref _hireByCountryOdds);
		reader.TryRead("hireByRegionOdds", ref _hireByRegionOdds);
		reader.TryRead("initialFunding", ref _initialFunding);
		reader.TryRead("alienFuel", ref _alienFuel);
		reader.TryRead("fontName", ref _fontName);
		reader.TryRead("psiUnlockResearch", ref _psiUnlockResearch);
		reader.TryRead("fakeUnderwaterBaseUnlockResearch", ref _fakeUnderwaterBaseUnlockResearch);
		reader.TryRead("newBaseUnlockResearch", ref _newBaseUnlockResearch);
		reader.TryRead("hireScientistsUnlockResearch", ref _hireScientistsUnlockResearch);
		reader.TryRead("hireEngineersUnlockResearch", ref _hireEngineersUnlockResearch);
		LoadBaseFunction("mod", ref _hireScientistsRequiresBaseFunc, reader["hireScientistsRequiresBaseFunc"]);
		LoadBaseFunction("mod", ref _hireEngineersRequiresBaseFunc, reader["hireEngineersRequiresBaseFunc"]);
		reader.TryRead("destroyedFacility", ref _destroyedFacility);

		reader.TryRead("turnAIUseGrenade", ref _aiUseDelayGrenade);
		reader.TryRead("turnAIUseBlaster", ref _aiUseDelayBlaster);
		var nodeAI = loadDocInfoHelper("ai");
		if (nodeAI)
		{
			nodeAI.TryRead("useDelayBlaster", ref _aiUseDelayBlaster);
			nodeAI.TryRead("useDelayFirearm", ref _aiUseDelayFirearm);
			nodeAI.TryRead("useDelayGrenade", ref _aiUseDelayGrenade);
			nodeAI.TryRead("aiUseDelayProxy", ref _aiUseDelayProxy);
			nodeAI.TryRead("useDelayMelee", ref _aiUseDelayMelee);
			nodeAI.TryRead("useDelayPsionic", ref _aiUseDelayPsionic);
			nodeAI.TryRead("useDelayMedikit", ref _aiUseDelayMedikit);

			nodeAI.TryRead("fireChoiceIntelCoeff", ref _aiFireChoiceIntelCoeff);
			nodeAI.TryRead("fireChoiceAggroCoeff", ref _aiFireChoiceAggroCoeff);
			nodeAI.TryRead("extendedFireModeChoice", ref _aiExtendedFireModeChoice);
			nodeAI.TryRead("respectMaxRange", ref _aiRespectMaxRange);
			nodeAI.TryRead("destroyBaseFacilities", ref _aiDestroyBaseFacilities);
			nodeAI.TryRead("pickUpWeaponsMoreActively", ref _aiPickUpWeaponsMoreActively);
			nodeAI.TryRead("pickUpWeaponsMoreActivelyCiv", ref _aiPickUpWeaponsMoreActivelyCiv);
			nodeAI.TryRead("reactionFireThreshold", ref _aiReactionFireThreshold);
			nodeAI.TryRead("reactionFireThresholdCiv", ref _aiReactionFireThresholdCiv);

			nodeAI.TryRead("targetWeightThreatThreshold", ref _aiTargetWeightThreatThreshold);
			nodeAI.TryRead("targetWeightAsHostile", ref _aiTargetWeightAsHostile);
			nodeAI.TryRead("targetWeightAsHostileCivilians", ref _aiTargetWeightAsHostileCivilians);
			nodeAI.TryRead("targetWeightAsFriendly", ref _aiTargetWeightAsFriendly);
			nodeAI.TryRead("targetWeightAsNeutral", ref _aiTargetWeightAsNeutral);
		}
		reader.TryRead("maxLookVariant", ref _maxLookVariant);
		reader.TryRead("tooMuchSmokeThreshold", ref _tooMuchSmokeThreshold);
		reader.TryRead("customTrainingFactor", ref _customTrainingFactor);
		reader.TryRead("chanceToStopRetaliation", ref _chanceToStopRetaliation);
		reader.TryRead("chanceToDetectAlienBaseEachMonth", ref _chanceToDetectAlienBaseEachMonth);
		reader.TryRead("lessAliensDuringBaseDefense", ref _lessAliensDuringBaseDefense);
		reader.TryRead("allowCountriesToCancelAlienPact", ref _allowCountriesToCancelAlienPact);
		reader.TryRead("buildInfiltrationBaseCloseToTheCountry", ref _buildInfiltrationBaseCloseToTheCountry);
		reader.TryRead("infiltrateRandomCountryInTheRegion", ref _infiltrateRandomCountryInTheRegion);
		reader.TryRead("allowAlienBasesOnWrongTextures", ref _allowAlienBasesOnWrongTextures);
		reader.TryRead("kneelBonusGlobal", ref _kneelBonusGlobal);
		reader.TryRead("oneHandedPenaltyGlobal", ref _oneHandedPenaltyGlobal);
		reader.TryRead("enableCloseQuartersCombat", ref _enableCloseQuartersCombat);
		reader.TryRead("closeQuartersAccuracyGlobal", ref _closeQuartersAccuracyGlobal);
		reader.TryRead("closeQuartersTuCostGlobal", ref _closeQuartersTuCostGlobal);
		reader.TryRead("closeQuartersEnergyCostGlobal", ref _closeQuartersEnergyCostGlobal);
		reader.TryRead("closeQuartersSneakUpGlobal", ref _closeQuartersSneakUpGlobal);
		reader.TryRead("noLOSAccuracyPenaltyGlobal", ref _noLOSAccuracyPenaltyGlobal);
		reader.TryRead("explodeInventoryGlobal", ref _explodeInventoryGlobal);
		reader.TryRead("surrenderMode", ref _surrenderMode);
		reader.TryRead("bughuntMinTurn", ref _bughuntMinTurn);
		reader.TryRead("bughuntMaxEnemies", ref _bughuntMaxEnemies);
		reader.TryRead("bughuntRank", ref _bughuntRank);
		reader.TryRead("bughuntLowMorale", ref _bughuntLowMorale);
		reader.TryRead("bughuntTimeUnitsLeft", ref _bughuntTimeUnitsLeft);

		var nodeMana = loadDocInfoHelper("mana");
		if (nodeMana)
		{
			nodeMana.TryRead("enabled", ref _manaEnabled);
			nodeMana.TryRead("battleUI", ref _manaBattleUI);
			nodeMana.TryRead("unlockResearch", ref _manaUnlockResearch);
			nodeMana.TryRead("trainingPrimary", ref _manaTrainingPrimary);
			nodeMana.TryRead("trainingSecondary", ref _manaTrainingSecondary);

			nodeMana.TryRead("woundThreshold", ref _manaMissingWoundThreshold);
			nodeMana.TryRead("replenishAfterMission", ref _manaReplenishAfterMission);
		}
		var nodeHealth = loadDocInfoHelper("health");
		if (nodeHealth)
		{
			nodeHealth.TryRead("woundThreshold", ref _healthMissingWoundThreshold);
			nodeHealth.TryRead("replenishAfterMission", ref _healthReplenishAfterMission);
		}

		var nodeGameOver = loadDocInfoHelper("gameOver");
		if (nodeGameOver)
		{
			nodeGameOver.TryRead("loseMoney", ref _loseMoney);
			nodeGameOver.TryRead("loseRating", ref _loseRating);
			nodeGameOver.TryRead("loseDefeat", ref _loseDefeat);
		}
		reader.TryRead("ufoGlancingHitThreshold", ref _ufoGlancingHitThreshold);
		reader.TryRead("ufoBeamWidthParameter", ref _ufoBeamWidthParameter);
		var arrayReader = reader["ufoTractorBeamSizeModifiers"];
		if (arrayReader)
		{
			for (int j = 0; j < _ufoTractorBeamSizeModifiers.Length; j++)
				arrayReader[j].TryReadVal(ref _ufoTractorBeamSizeModifiers[j]);
		}
		reader.TryRead("escortRange", ref _escortRange);
		reader.TryRead("drawEnemyRadarCircles", ref _drawEnemyRadarCircles);
		reader.TryRead("escortsJoinFightAgainstHK", ref _escortsJoinFightAgainstHK);
		reader.TryRead("hunterKillerFastRetarget", ref _hunterKillerFastRetarget);
		reader.TryRead("crewEmergencyEvacuationSurvivalChance", ref _crewEmergencyEvacuationSurvivalChance);
		reader.TryRead("pilotsEmergencyEvacuationSurvivalChance", ref _pilotsEmergencyEvacuationSurvivalChance);
		reader.TryRead("showUfoPreviewInBaseDefense", ref _showUfoPreviewInBaseDefense);
		reader.TryRead("soldiersPerSergeant", ref _soldiersPerRank[(int)SoldierRank.RANK_SERGEANT]);
		reader.TryRead("soldiersPerCaptain", ref _soldiersPerRank[(int)SoldierRank.RANK_CAPTAIN]);
		reader.TryRead("soldiersPerColonel", ref _soldiersPerRank[(int)SoldierRank.RANK_COLONEL]);
		reader.TryRead("soldiersPerCommander", ref _soldiersPerRank[(int)SoldierRank.RANK_COMMANDER]);
		reader.TryRead("pilotAccuracyZeroPoint", ref _pilotAccuracyZeroPoint);
		reader.TryRead("pilotAccuracyRange", ref _pilotAccuracyRange);
		reader.TryRead("pilotReactionsZeroPoint", ref _pilotReactionsZeroPoint);
		reader.TryRead("pilotReactionsRange", ref _pilotReactionsRange);
		arrayReader = reader["pilotBraveryThresholds"];
		if (arrayReader)
		{
			for (int j = 0; j < _pilotBraveryThresholds.Length; j++)
				arrayReader[j].TryReadVal(ref _pilotBraveryThresholds[j]);
		}
		reader.TryRead("performanceBonusFactor", ref _performanceBonusFactor);
		reader.TryRead("enableNewResearchSorting", ref _enableNewResearchSorting);
		reader.TryRead("displayCustomCategories", ref _displayCustomCategories);
		reader.TryRead("shareAmmoCategories", ref _shareAmmoCategories);
		reader.TryRead("showDogfightDistanceInKm", ref _showDogfightDistanceInKm);
		reader.TryRead("showFullNameInAlienInventory", ref _showFullNameInAlienInventory);
		reader.TryRead("alienInventoryOffsetX", ref _alienInventoryOffsetX);
		reader.TryRead("alienInventoryOffsetBigUnit", ref _alienInventoryOffsetBigUnit);
		reader.TryRead("hidePediaInfoButton", ref _hidePediaInfoButton);
		reader.TryRead("extraNerdyPediaInfoType", ref _extraNerdyPediaInfoType);
		reader.TryRead("giveScoreAlsoForResearchedArtifacts", ref _giveScoreAlsoForResearchedArtifacts);
		reader.TryRead("statisticalBulletConservation", ref _statisticalBulletConservation);
		reader.TryRead("stunningImprovesMorale", ref _stunningImprovesMorale);
		reader.TryRead("tuRecoveryWakeUpNewTurn", ref _tuRecoveryWakeUpNewTurn);
		reader.TryRead("shortRadarRange", ref _shortRadarRange);
		reader.TryRead("buildTimeReductionScaling", ref _buildTimeReductionScaling);
		reader.TryRead("baseDefenseMapFromLocation", ref _baseDefenseMapFromLocation);
		reader.TryRead("pediaReplaceCraftFuelWithRangeType", ref _pediaReplaceCraftFuelWithRangeType);
		reader.TryRead("missionRatings", ref _missionRatings);
		reader.TryRead("monthlyRatings", ref _monthlyRatings);
		LoadUnorderedNamesToNames("mod", _fixedUserOptions, reader["fixedUserOptions"]);
		LoadUnorderedNamesToNames("mod", _recommendedUserOptions, reader["recommendedUserOptions"]);
		LoadUnorderedNames("mod", _hiddenMovementBackgrounds, reader["hiddenMovementBackgrounds"]);
		LoadUnorderedNames("mod", _baseNamesFirst, reader["baseNamesFirst"]);
		LoadUnorderedNames("mod", _baseNamesMiddle, reader["baseNamesMiddle"]);
		LoadUnorderedNames("mod", _baseNamesLast, reader["baseNamesLast"]);
		LoadUnorderedNames("mod", _operationNamesFirst, reader["operationNamesFirst"]);
		LoadUnorderedNames("mod", _operationNamesLast, reader["operationNamesLast"]);
		reader.TryRead("disableUnderwaterSounds", ref _disableUnderwaterSounds);
		reader.TryRead("enableUnitResponseSounds", ref _enableUnitResponseSounds);
		foreach (var unitResponseSound in iterateRulesSpecific("unitResponseSounds"))
		{
			string type = unitResponseSound["name"].ReadVal<string>();
			if (unitResponseSound["selectUnitSound"])
				LoadSoundOffset(type, GetOrAdd(_selectUnitSound, type), unitResponseSound["selectUnitSound"], "BATTLE.CAT");
			if (unitResponseSound["startMovingSound"])
				LoadSoundOffset(type, GetOrAdd(_startMovingSound, type), unitResponseSound["startMovingSound"], "BATTLE.CAT");
			if (unitResponseSound["selectWeaponSound"])
				LoadSoundOffset(type, GetOrAdd(_selectWeaponSound, type), unitResponseSound["selectWeaponSound"], "BATTLE.CAT");
			if (unitResponseSound["annoyedSound"])
				LoadSoundOffset(type, GetOrAdd(_annoyedSound, type), unitResponseSound["annoyedSound"], "BATTLE.CAT");
		}
		LoadSoundOffset("global", _selectBaseSound, reader["selectBaseSound"], "BATTLE.CAT");
		LoadSoundOffset("global", _startDogfightSound, reader["startDogfightSound"], "BATTLE.CAT");
		reader.TryRead("flagByKills", ref _flagByKills);
		reader.TryRead("defeatScore", ref _defeatScore);
		reader.TryRead("defeatFunds", ref _defeatFunds);
		reader.TryRead("difficultyDemigod", ref _difficultyDemigod);

		var difficultyCoefficientOverrides = loadDocInfoHelper("difficultyCoefficientOverrides");
		if (difficultyCoefficientOverrides)
		{
			difficultyCoefficientOverrides.TryRead("monthlyRatingThresholds", ref _monthlyRatingThresholds);
			difficultyCoefficientOverrides.TryRead("ufoFiringRateCoefficients", ref _ufoFiringRateCoefficients);
			difficultyCoefficientOverrides.TryRead("ufoEscapeCountdownCoefficients", ref _ufoEscapeCountdownCoefficients);
			difficultyCoefficientOverrides.TryRead("retaliationTriggerOdds", ref _retaliationTriggerOdds);
			difficultyCoefficientOverrides.TryRead("retaliationBaseRegionOdds", ref _retaliationBaseRegionOdds);
			difficultyCoefficientOverrides.TryRead("aliensFacingCraftOdds", ref _aliensFacingCraftOdds);
		}

		arrayReader = reader["difficultyCoefficient"];
		if (arrayReader)
		{
			for (int j = 0; j < DIFFICULTY_COEFFICIENT.Length; j++)
			{
				arrayReader[j].TryReadVal(ref DIFFICULTY_COEFFICIENT[j]);
				_statAdjustment[j].growthMultiplier = DIFFICULTY_COEFFICIENT[j];
			}
		}
		arrayReader = reader["sellPriceCoefficient"];
		if (arrayReader)
		{
			for (int j = 0; j < SELL_PRICE_COEFFICIENT.Length; j++)
				arrayReader[j].TryReadVal(ref SELL_PRICE_COEFFICIENT[j]);
		}
		arrayReader = reader["buyPriceCoefficient"];
		if (arrayReader)
		{
			for (int j = 0; j < BUY_PRICE_COEFFICIENT.Length; j++)
				arrayReader[j].TryReadVal(ref BUY_PRICE_COEFFICIENT[j]);
		}
		arrayReader = reader["difficultyBasedRetaliationDelay"];
		if (arrayReader)
		{
			for (int j = 0; j < DIFFICULTY_BASED_RETAL_DELAY.Length; j++)
				arrayReader[j].TryReadVal(ref DIFFICULTY_BASED_RETAL_DELAY[j]);
		}
		arrayReader = reader["unitResponseSoundsFrequency"];
		if (arrayReader)
		{
			for (int j = 0; j < UNIT_RESPONSE_SOUNDS_FREQUENCY.Length; j++)
				arrayReader[j].TryReadVal(ref UNIT_RESPONSE_SOUNDS_FREQUENCY[j]);
		}

		reader.TryRead("alienItemLevels", ref _alienItemLevels);

		foreach (var patchReader in reader["MCDPatches"].Children()) // this should not be used by mods
		{
			string type = patchReader["type"].ReadVal<string>();
			if (_MCDPatches.TryGetValue(type, out var oldPatch))
			{
				oldPatch.Load(patchReader);
			}
			else
			{
				MCDPatch patch = new MCDPatch();
				patch.Load(patchReader);
				_MCDPatches[type] = patch;
			}
		}
		foreach (var ruleReader in iterateRulesSpecific("extraSprites"))
		{
			if (ruleReader["type"] || ruleReader["typeSingle"])
			{
				string type = "";
				ruleReader["type"].TryReadVal(ref type);
				if (type.Length == 0)
				{
					type = ruleReader["typeSingle"].ReadVal<string>();
				}
				ExtraSprites extraSprites = new ExtraSprites();
				ModData data = _modCurrent;
				// doesn't support modIndex
				if (type == "TEXTURE.DAT")
					data = _modData[0];
				extraSprites.Load(ruleReader, data);
				GetOrAdd(_extraSprites, type).Add(extraSprites);
			}
			else if (ruleReader["delete"])
			{
				string type = ruleReader["delete"].ReadVal<string>();
				_extraSprites.Remove(type);
			}
		}
		foreach (var ruleReader in iterateRulesSpecific("customPalettes"))
		{
			CustomPalettes rule = LoadRule(ruleReader, _customPalettes, _customPalettesIndex, "type", type => new CustomPalettes(type));
			rule?.Load(ruleReader);
		}
		foreach (var ruleReader in iterateRulesSpecific("extraSounds"))
		{
			string type = ruleReader["type"].ReadVal<string>();
			ExtraSounds extraSounds = new ExtraSounds();
			extraSounds.Load(ruleReader, _modCurrent);
			_extraSounds.Add((type, extraSounds));
		}
		foreach (var ruleReader in iterateRulesSpecific("extraStrings"))
		{
			string type = ruleReader["type"].ReadVal<string>();
			if (_extraStrings.TryGetValue(type, out var oldStrings))
			{
				oldStrings.Load(ruleReader);
			}
			else
			{
				ExtraStrings extraStrings = new ExtraStrings();
				extraStrings.Load(ruleReader);
				_extraStrings[type] = extraStrings;
			}
		}

		foreach (var ruleReader in iterateRulesSpecific("statStrings"))
		{
			StatString statString = new StatString();
			statString.Load(ruleReader);
			_statStrings.Add(statString);
		}

		foreach (var ruleReader in iterateRulesSpecific("interfaces"))
		{
			RuleInterface rule = LoadRule(ruleReader, _interfaces, null, "type", type => new RuleInterface(type));
			rule?.Load(ruleReader, this);
		}

		foreach (var ruleReader in iterateRulesSpecific("cutscenes"))
		{
			RuleVideo rule = LoadRule(ruleReader, _videos, null, "type", type => new RuleVideo(type));
			rule?.Load(ruleReader);
		}
		foreach (var ruleReader in iterateRulesSpecific("musics"))
		{
			RuleMusic rule = LoadRule(ruleReader, _musicDefs, null, "type", type => new RuleMusic(type));
			rule?.Load(ruleReader);
		}

		if (reader["globe"])
		{
			_globe.Load(reader["globe"], this);
		}
		if (reader["converter"])
		{
			_converter.Load(reader["converter"]);
		}
		var constants = reader["constants"];
		if (constants)
		{
			//backward compatibility version
			if (constants.IsSeq)
			{
				foreach (var constant in constants.Children())
				{
					LoadConstants(constant);
				}
			}
			else
			{
				LoadConstants(constants.UseIndex());
			}
		}

		// refresh _psiRequirements for psiStrengthEval
		foreach (var facType in _facilitiesIndex)
		{
			RuleBaseFacility rule = GetBaseFacility(facType);
			if (rule.PsiLaboratories > 0)
			{
				_psiRequirements = new List<string>(rule.Requirements); // copy
				break;
			}
		}
		// override the default (used when you want to separate screening and training)
		if (_psiUnlockResearch.Length != 0)
		{
			_psiRequirements.Clear();
			_psiRequirements.Add(_psiUnlockResearch);
		}

		arrayReader = reader["aimAndArmorMultipliers"];
		if (arrayReader)
		{
			for (int j = 0; j < MaxDifficultyLevels; j++)
			{
				arrayReader[j].TryReadVal(ref _statAdjustment[j].aimMultiplier);
				arrayReader[j].TryReadVal(ref _statAdjustment[j].armorMultiplier);
			}
		}
		arrayReader = reader["aimMultipliers"];
		if (arrayReader)
		{
			for (int j = 0; j < MaxDifficultyLevels; j++)
				arrayReader[j].TryReadVal(ref _statAdjustment[j].aimMultiplier);
		}
		arrayReader = reader["armorMultipliers"];
		if (arrayReader)
		{
			for (int j = 0; j < MaxDifficultyLevels; j++)
				arrayReader[j].TryReadVal(ref _statAdjustment[j].armorMultiplier);
		}
		arrayReader = reader["armorMultipliersAbs"];
		if (arrayReader)
		{
			for (int j = 0; j < MaxDifficultyLevels; j++)
				arrayReader[j].TryReadVal(ref _statAdjustment[j].armorMultiplierAbs);
		}
		arrayReader = reader["statGrowthMultipliersAbs"];
		if (arrayReader)
		{
			for (int j = 0; j < MaxDifficultyLevels; j++)
				arrayReader[j].TryReadVal(ref _statAdjustment[j].statGrowthAbs);
		}
		arrayReader = reader["statGrowthMultipliers"];
		if (arrayReader)
		{
			arrayReader.TryReadVal(ref _statAdjustment[0].statGrowth);
			for (int j = 1; j < MaxDifficultyLevels; j++)
				_statAdjustment[j].statGrowth = _statAdjustment[0].statGrowth;
		}
		var lighting = loadDocInfoHelper("lighting");
		if (lighting)
		{
			lighting.TryRead("maxStatic", ref _maxStaticLightDistance);
			lighting.TryRead("maxDynamic", ref _maxDynamicLightDistance);
			lighting.TryRead("enhanced", ref _enhancedLighting);
		}
	}

	private static void LoadRuleInfoHelper(YamlNodeReader reader, string nodeName, string type)
	{
		if (reader.HasValTagNamed(InfoTag))
		{
			var info = new StringBuilder();
			info.Append("Main node names available for '").Append(nodeName).Append(":' at line ").Append(reader.LocationInFile.Line).Append(" are: ");
			info.Append(" '").Append(YamlRuleNodeDelete).Append(":',");
			info.Append(" '").Append(YamlRuleNodeNew).Append(":',");
			info.Append(" '").Append(YamlRuleNodeOverride).Append(":',");
			info.Append(" '").Append(YamlRuleNodeUpdate).Append(":',");
			info.Append(" '").Append(YamlRuleNodeIgnore).Append(":',");
			info.Append(" '").Append(type).Append(":'");
			Logger.Info(info.ToString());
		}
	}

	/// <summary>Helper function protecting from circular references in node definition.</summary>
	/// <param name="reader">Node to test.</param>
	/// <param name="name">Name of original node.</param>
	/// <param name="limit">Current depth.</param>
	private static void RefNodeTestDeepth(YamlNodeReader reader, string name, int limit)
	{
		if (limit > 64)
		{
			throw new InvalidOperationException("Nest limit of refNode reach in " + name);
		}
		var nested = reader["refNode"];
		if (nested)
		{
			if (!nested.IsMap)
			{
				throw new InvalidOperationException($"Invalid refNode at nest level of {limit} in {name}");
			}
			RefNodeTestDeepth(nested, name, limit + 1);
		}
	}

	/// <summary>
	/// Loads a rule element, adding/removing from vectors as necessary.
	/// </summary>
	/// <param name="reader">YAML node.</param>
	/// <param name="map">Map associated to the rule type.</param>
	/// <param name="index">Index vector for the rule type, or null.</param>
	/// <param name="key">Rule key name.</param>
	/// <param name="factory">Creates a new rule (C++ RuleFactory / RuleListOrderedFactory).</param>
	/// <returns>The rule to load (new or existing), or null if one was removed or nothing should be loaded.</returns>
	internal T LoadRule<T>(YamlNodeReader reader, SortedDictionary<string, T> map, List<string> index, string key, Func<string, T> factory) where T : class
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentNullException.ThrowIfNull(factory);
		T rule = null;

		static (string name, YamlNodeReader node, bool have) getNode(YamlNodeReader i, string nodeName)
		{
			var n = i[nodeName];
			return (nodeName, n, n.IsValid);
		}
		static string getDescriptionNode((string name, YamlNodeReader node, bool have) nn)
		{
			int line = nn.node.LocationInFile.Line;
			return $"'{nn.name}' at line {line}";
		}
		static string getNameFromNode((string name, YamlNodeReader node, bool have) nn)
		{
			var name = nn.node.ReadVal<string>();
			if (IsEmptyRuleName(name))
			{
				int line = nn.node.LocationInFile.Line;
				throw new InvalidOperationException($"Invalid value for main node '{nn.name}' at line {line}");
			}
			return name;
		}

		var defaultNode = getNode(reader, key);
		var deleteNode = getNode(reader, YamlRuleNodeDelete);
		var newNode = getNode(reader, YamlRuleNodeNew);
		var overrideNode = getNode(reader, YamlRuleNodeOverride);
		var updateNode = getNode(reader, YamlRuleNodeUpdate);
		var ignoreNode = getNode(reader, YamlRuleNodeIgnore);

		{
			// check for duplicates
			(string name, YamlNodeReader node, bool have)? last = null;
			foreach (var p in new[] { defaultNode, deleteNode, newNode, updateNode, overrideNode, ignoreNode })
			{
				if (p.have)
				{
					if (last != null)
					{
						throw new InvalidOperationException("Conflict of main node " + getDescriptionNode(last.Value) + " and " + getDescriptionNode(p));
					}
					else
					{
						last = p;
					}
				}
			}
		}

		if (defaultNode.have)
		{
			string type = getNameFromNode(defaultNode);

			if (map.TryGetValue(type, out var existing))
			{
				rule = existing;
			}
			else
			{
				rule = factory(type);
				_ruleCreationTracking[rule] = _modCurrent;
				map[type] = rule;
				index?.Add(type);
			}

			// protection from self referencing refNode node
			RefNodeTestDeepth(reader, type, 0);
			_ruleLastUpdateTracking[rule] = _modCurrent;
		}
		else if (deleteNode.have)
		{
			string type = getNameFromNode(deleteNode);

			if (map.TryGetValue(type, out var existing))
			{
				if (existing != null)
				{
					_ruleCreationTracking.Remove(existing);
					_ruleLastUpdateTracking.Remove(existing);
				}
				map.Remove(type);
			}
			index?.Remove(type);
		}
		else if (newNode.have)
		{
			string type = getNameFromNode(newNode);

			if (map.ContainsKey(type))
			{
				CheckForSoftError(true, type, "Rule named '" + type + "' already used for " + getDescriptionNode(newNode), SeverityLevel.LOG_ERROR);
			}
			else
			{
				rule = factory(type);
				_ruleCreationTracking[rule] = _modCurrent;
				map[type] = rule;
				index?.Add(type);

				// protection from self referencing refNode node
				RefNodeTestDeepth(reader, type, 0);
				_ruleLastUpdateTracking[rule] = _modCurrent;
			}
		}
		else if (overrideNode.have)
		{
			string type = getNameFromNode(overrideNode);

			if (map.TryGetValue(type, out var existing))
			{
				rule = existing;

				// protection from self referencing refNode node
				RefNodeTestDeepth(reader, type, 0);
				_ruleLastUpdateTracking[rule] = _modCurrent;
			}
			else
			{
				CheckForSoftError(true, type, "Rule named '" + type + "' do not exist for " + getDescriptionNode(overrideNode), SeverityLevel.LOG_ERROR);
			}
		}
		else if (updateNode.have)
		{
			string type = getNameFromNode(updateNode);

			if (map.TryGetValue(type, out var existing))
			{
				rule = existing;

				// protection from self referencing refNode node
				RefNodeTestDeepth(reader, type, 0);
				_ruleLastUpdateTracking[rule] = _modCurrent;
			}
			else
			{
				Logger.Info("Rule named '" + type + "' do not exist for " + getDescriptionNode(updateNode));
			}
		}
		else if (ignoreNode.have)
		{
			// nothing to see there...
		}
		else
		{
			throw new LoadRuleException("Mod", reader, "Missing main node");
		}

		return rule;
	}
}
