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
// Ported from: src/Engine/Language.h, src/Engine/Language.cpp

using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Acom;

public enum TextDirection
{
	DIRECTION_LTR,
	DIRECTION_RTL,
}

public enum TextWrapping
{
	WRAP_AUTO,
	WRAP_WORDS,
	WRAP_LETTERS,
}

/// <summary>
/// Contains strings used throughout the game for localization.
/// Languages are just a set of strings identified by an ID string.
/// </summary>
public sealed class Language
{
	private static readonly Dictionary<string, string> s_names = new(StringComparer.Ordinal)
	{
		["en-US"] = "English (US)",
		["en-GB"] = "English (UK)",
		["bg"] = "Български",
		["ca-ES"] = "Català",
		["cs"] = "Česky",
		["cy"] = "Cymraeg",
		["da"] = "Dansk",
		["de"] = "Deutsch",
		["el"] = "Ελληνικά",
		["et"] = "Eesti",
		["es-ES"] = "Español (ES)",
		["es-419"] = "Español (AL)",
		["fr"] = "Français (FR)",
		["fr-CA"] = "Français (CA)",
		["fi"] = "Suomi",
		["ga"] = "Gaeilge",
		["hr"] = "Hrvatski",
		["hu"] = "Magyar",
		["it"] = "Italiano",
		["is"] = "Íslenska",
		["ja"] = "日本語",
		["ko"] = "한국어",
		["lb"] = "Lëtzebuergesch",
		["lv"] = "Latviešu",
		["nl"] = "Nederlands",
		["no"] = "Norsk",
		["pl"] = "Polski",
		["pt-BR"] = "Português (BR)",
		["pt-PT"] = "Português (PT)",
		["ro"] = "Română",
		["ru"] = "Русский",
		["sk"] = "Slovenčina",
		["sl"] = "Slovenščina",
		["sv"] = "Svenska",
		["tr"] = "Türkçe",
		["uk"] = "Українська",
		["vi"] = "Tiếng Việt",
		["zh-CN"] = "中文",
		["zh-TW"] = "文言",
	};

	private static readonly string[] s_rtl = [];
	private static readonly string[] s_cjk = ["ja", "ko", "zh-CN", "zh-TW"];
	private static readonly HashSet<string> s_notFoundIds = new(StringComparer.Ordinal);

	private readonly Dictionary<string, LocalizedText> _strings = new(StringComparer.Ordinal);
	private readonly LanguagePlurality _handler;
	private readonly TextDirection _direction;
	private readonly TextWrapping _wrap;

	/// <summary>Creates a blank language for <see cref="Options.language"/>.</summary>
	public Language()
	{
		string id = Options.language;
		_handler = LanguagePlurality.Create(id);
		_direction = s_rtl.Contains(id) ? TextDirection.DIRECTION_RTL : TextDirection.DIRECTION_LTR;
		if (Options.wordwrap == TextWrapping.WRAP_AUTO)
			_wrap = s_cjk.Contains(id) ? TextWrapping.WRAP_LETTERS : TextWrapping.WRAP_WORDS;
		else
			_wrap = Options.wordwrap;
	}

	/// <summary>Gets all the languages found in the Data folder (common layer) and their human-readable names.</summary>
	/// <param name="ids">List of language ids (sorted).</param>
	/// <param name="names">List of language human-readable names.</param>
	public static void GetList(List<string> ids, List<string> names)
	{
		ArgumentNullException.ThrowIfNull(ids);
		ArgumentNullException.ThrowIfNull(names);
		var nset = FileMap.FilterFiles(FileMap.GetVFolderContents("Language", 0), "yml");
		ids.Clear();
		foreach (var filename in nset)
			ids.Add(FileNameToId(filename));
		ids.Sort(StringComparer.Ordinal);
		names.Clear();
		foreach (var id in ids)
			names.Add(s_names.GetValueOrDefault(id, id));
	}

	/// <summary>Checks if a language is in the supported name list.</summary>
	public static bool IsSupported(string lang) => s_names.ContainsKey(lang);

	/// <summary>Native display name of a language id (or the id itself).</summary>
	public static string GetName(string id) => s_names.GetValueOrDefault(id, id);

	/// <summary>
	/// Language id of a file name: "en-us.yml" → "en-US" (upper case after the dash).
	/// </summary>
	public static string FileNameToId(string fileName)
	{
		string noExtName = CrossPlatform.NoExt(CrossPlatform.BaseFilename(fileName));
		int found = noExtName.IndexOf('-', StringComparison.Ordinal);
		if (found < 0)
			return noExtName;
		// uppercase it after dash (C toupper: ASCII letters only, UTF-8 bytes stay as they are)
		var id = noExtName.ToCharArray();
		for (int i = found + 1; i < id.Length; ++i)
		{
			if (char.IsAsciiLetterLower(id[i]))
				id[i] = (char)(id[i] - 'a' + 'A');
		}
		return new string(id);
	}

	/// <summary>Loads the language from an external file (Ruby-on-Rails YAML format).</summary>
	public void LoadFile(FileMap.FileRecord frec)
	{
		ArgumentNullException.ThrowIfNull(frec);
		LoadFile(frec.GetYaml());
	}

	/// <summary>Loads strings from a language file (root map, optionally wrapped in the language id).</summary>
	public void LoadFile(YamlRootNodeReader reader)
	{
		ArgumentNullException.ThrowIfNull(reader);
		var langMap = reader[0].IsMap ? reader[0] : reader.ToBase();
		foreach (var langReader in langMap.Children())
		{
			// Regular strings
			if (langReader.HasVal)
			{
				string value = langReader.ReadVal<string>();
				if (value.Length > 0)
					_strings[langReader.ReadKey<string>()] = LoadString(value);
			}
			// Strings with plurality
			else if (langReader.IsMap)
			{
				foreach (var pluralityReader in langReader.Children())
				{
					string value = pluralityReader.ReadVal("");
					if (value.Length > 0)
						_strings[langReader.ReadKey<string>() + "_" + pluralityReader.ReadKey<string>()] = LoadString(value);
				}
			}
		}
	}

	/// <summary>Loads the language from a mod's ExtraStrings.</summary>
	/// <param name="extraStrings">List of ExtraStrings.</param>
	/// <param name="id">Language ID.</param>
	public void LoadRule(SortedDictionary<string, ExtraStrings> extraStrings, string id)
	{
		ArgumentNullException.ThrowIfNull(extraStrings);
		if (extraStrings.TryGetValue(id, out var it))
			LoadStrings(it.Strings);
	}

	/// <summary>Adds or replaces strings (the body of <see cref="LoadRule"/>).</summary>
	public void LoadStrings(IEnumerable<KeyValuePair<string, string>> strings)
	{
		ArgumentNullException.ThrowIfNull(strings);
		foreach (var (key, value) in strings)
			_strings[key] = LoadString(value);
	}

	/// <summary>Replaces the text markers of an external string with the engine's control characters.</summary>
	private static LocalizedText LoadString(string s)
	{
		s = s.Replace("{NEWLINE}", "\n", StringComparison.Ordinal)
			.Replace("{SMALLLINE}", Unicode.TOK_NL_SMALL.ToString(), StringComparison.Ordinal)
			.Replace("{ALT}", Unicode.TOK_COLOR_FLIP.ToString(), StringComparison.Ordinal);
		return new LocalizedText(s);
	}

	/// <summary>Gets a localized text; the id itself if missing.</summary>
	public LocalizedText GetString(string id)
	{
		if (string.IsNullOrEmpty(id))
			return new LocalizedText(id ?? string.Empty);
		// Check if translation strings recently learned pluralization.
		return _strings.TryGetValue(id, out var s) ? s : GetString(id, uint.MaxValue);
	}

	/// <summary>Gets a quantity-dependent localized text; <c>{N}</c> is replaced by <paramref name="n"/>.</summary>
	public LocalizedText GetString(string id, uint n)
	{
		// C++ assert(!id.empty()): compiled out in release builds, where an empty id is simply not found.
		Debug.Assert(!string.IsNullOrEmpty(id));
		LocalizedText? s = null;
		// Try specialized form.
		if (n == 0)
			_strings.TryGetValue(id + "_zero", out s);
		// Try proper form by language
		if (s == null)
			_strings.TryGetValue(id + _handler.GetSuffix(n), out s);
		// Try default form
		if (s == null)
			_strings.TryGetValue(id + "_other", out s);
		// Give up
		if (s == null)
		{
			lock (s_notFoundIds)
			{
				if (s_notFoundIds.Add(id))
					Logger.Warning($"{id} not found in {Options.language}");
			}
			return new LocalizedText(id);
		}
		if (n == uint.MaxValue) // Special case
		{
			lock (s_notFoundIds)
			{
				if (s_notFoundIds.Add(id))
					Logger.Warning($"{id} has plural format in ``{Options.language}``. Code assumes singular format.");
			}
			return s;
		}
		return new LocalizedText(((string)s).Replace("{N}", n.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal));
	}

	/// <summary>Same as <see cref="GetString(string, uint)"/>; C++ converts int arguments to unsigned implicitly.</summary>
	public LocalizedText GetString(string id, int n) => GetString(id, unchecked((uint)n));

	/// <summary>Gets a gender-dependent localized text (id_MALE / id_FEMALE).</summary>
	public LocalizedText GetString(string id, SoldierGender gender)
	{
		return GetString(gender == SoldierGender.GENDER_MALE ? id + "_MALE" : id + "_FEMALE");
	}

	/// <summary>Outputs all the language IDs and strings to an HTML file.</summary>
	public void ToHtml(string filename) => CrossPlatform.WriteFile(filename, ToHtml());

	/// <summary>Outputs the language to a HTML table (for translators).</summary>
	public string ToHtml()
	{
		var html = new StringBuilder();
		html.Append("<table border=\"1\" width=\"100%\">\n<tr><th>ID String</th><th>English String</th></tr>\n");
		foreach (var (key, value) in _strings.OrderBy(p => p.Key, StringComparer.Ordinal))
		{
			html.Append("<tr><td>").Append(key).Append("</td><td>");
			foreach (char c in (string)value)
			{
				if (c == Unicode.TOK_NL_SMALL || c == '\n')
					html.Append("<br />");
				else
					html.Append(c);
			}
			html.Append("</td></tr>\n");
		}
		html.Append("</table>\n");
		return html.ToString();
	}

	public TextDirection TextDirection => _direction;

	public TextWrapping TextWrapping => _wrap;
}
