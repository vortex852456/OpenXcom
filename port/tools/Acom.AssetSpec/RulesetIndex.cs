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

using System.Globalization;
using YamlDotNet.RepresentationModel;

namespace Acom.AssetSpec;

/// <summary>
/// Collects which ruleset entries use which original file, frame or sound.
/// The mapping of ruleset keys to files and frame offsets follows the engine
/// (Mod.cpp, StatsForNerdsState.cpp).
/// </summary>
internal sealed class RulesetIndex
{
	/// <summary>References per bare file name (upper case), file level.</summary>
	public Dictionary<string, List<string>> FileUsers { get; } = new(StringComparer.Ordinal);

	/// <summary>References per bare file name (upper case) and frame index.</summary>
	public Dictionary<string, SortedDictionary<int, List<string>>> FrameUsers { get; } = new(StringComparer.Ordinal);

	/// <summary>File name prefixes (e.g. inventory images MAN_0*) with their users.</summary>
	public List<(string Prefix, string User)> PrefixUsers { get; } = [];

	/// <summary>Logical sound sets (BATTLE.CAT, GEO.CAT): index -> users.</summary>
	public SortedDictionary<string, SortedDictionary<int, List<string>>> SoundSets { get; } = new(StringComparer.Ordinal);

	/// <summary>TFTD sound definitions: logical set -> (archive file, physical index per logical index).</summary>
	public Dictionary<string, (string File, List<int> Sounds)> SoundDefs { get; } = new(StringComparer.Ordinal);

	private static readonly HashSet<string> s_geoSections = new(StringComparer.Ordinal) { "crafts", "craftWeapons", "facilities", "ufos", "interfaces", "missionScripts", "alienMissions", "events" };
	private static readonly HashSet<string> s_geoConstants = new(StringComparer.Ordinal) { "buttonPress", "windowPopup", "ufoFire", "ufoHit", "ufoCrash", "ufoExplode", "interceptorHit", "interceptorExplode" };

	/// <summary>Reads all *.rul files of a ruleset folder.</summary>
	public static RulesetIndex Load(string rulesDir)
	{
		var index = new RulesetIndex();
		foreach (var file in Directory.EnumerateFiles(rulesDir, "*.rul", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
		{
			var yaml = new YamlStream();
			using (var reader = new StreamReader(file))
				yaml.Load(reader);
			foreach (var doc in yaml.Documents)
			{
				if (doc.RootNode is not YamlMappingNode root)
					continue;
				foreach (var (k, v) in root.Children)
				{
					string section = Scalar(k) ?? "";
					if (v is YamlSequenceNode seq)
					{
						foreach (var item in seq.Children.OfType<YamlMappingNode>())
							index.Process(section, RuleId(item), item);
					}
					else if (v is YamlMappingNode map)
					{
						index.Process(section, section, map);
					}
				}
			}
		}
		return index;
	}

	private static string? Scalar(YamlNode node) => node is YamlScalarNode s ? s.Value : null;

	private static string RuleId(YamlMappingNode item)
	{
		foreach (var key in new[] { "type", "name", "id" })
		{
			if (item.Children.TryGetValue(new YamlScalarNode(key), out var v) && Scalar(v) is { } id)
				return id;
		}
		return "?";
	}

	private static IEnumerable<int> Ints(YamlNode node)
	{
		var values = node is YamlSequenceNode seq ? seq.Children.Select(Scalar) : [Scalar(node)];
		foreach (var v in values)
		{
			if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n >= 0)
				yield return n;
		}
	}

	private static IEnumerable<string> Strings(YamlNode node)
	{
		var values = node is YamlSequenceNode seq ? seq.Children.Select(Scalar) : [Scalar(node)];
		return values.Where(v => !string.IsNullOrEmpty(v)).Select(v => v!);
	}

	/// <summary>Rulesets may name files with their folder (e.g. ANIMS/INTRO.VID); the index uses the bare file name.</summary>
	private static string BareName(string name) => name[(name.LastIndexOfAny(['/', '\\']) + 1)..].ToUpperInvariant();

	private void AddFile(string bareName, string user)
	{
		bareName = BareName(bareName);
		if (!FileUsers.TryGetValue(bareName, out var list))
			FileUsers[bareName] = list = [];
		list.Add(user);
	}

	private void AddFrame(string bareName, int frame, string user)
	{
		bareName = BareName(bareName);
		if (!FrameUsers.TryGetValue(bareName, out var frames))
			FrameUsers[bareName] = frames = [];
		if (!frames.TryGetValue(frame, out var list))
			frames[frame] = list = [];
		list.Add(user);
	}

	private void AddSound(string set, int index, string user)
	{
		if (!SoundSets.TryGetValue(set, out var sounds))
			SoundSets[set] = sounds = [];
		if (!sounds.TryGetValue(index, out var list))
			sounds[index] = list = [];
		list.Add(user);
	}

	private void Process(string section, string id, YamlMappingNode item)
	{
		string? drawingRoutine = item.Children.TryGetValue(new YamlScalarNode("drawingRoutine"), out var dr) ? Scalar(dr) : null;
		foreach (var (k, v) in item.Children)
		{
			string key = Scalar(k) ?? "";
			string user = $"{section}/{id}: {key}";
			switch (section, key)
			{
				case ("items", "bigSprite"):
					foreach (int n in Ints(v)) AddFrame("BIGOBS.PCK", n, user);
					break;
				case ("items", "floorSprite"):
					foreach (int n in Ints(v)) AddFrame("FLOOROB.PCK", n, user);
					break;
				case ("items", "handSprite"):
					foreach (int n in Ints(v))
						for (int d = 0; d < 8; ++d)
							AddFrame("HANDOB.PCK", n + d, $"{user} (Richtung {d})");
					break;
				case ("items", "specialIconSprite"):
				case ("soldiers", "skillIconSprite"):
					foreach (int n in Ints(v)) AddFrame("SPICONS.DAT", n, user);
					break;
				case ("items", "hitAnimation"):
				case ("items", "meleeAnimation"):
				case ("items", "psiAnimation"):
					foreach (int n in Ints(v))
					{
						AddFrame("SMOKE.PCK", n, $"{user} (je nach Trefferart SMOKE.PCK oder HIT.PCK)");
						AddFrame("HIT.PCK", n, $"{user} (je nach Trefferart SMOKE.PCK oder HIT.PCK)");
					}
					break;
				case ("armors", "spriteSheet"):
					foreach (var s in Strings(v))
						AddFile(s, drawingRoutine != null ? $"{user} (drawingRoutine {drawingRoutine})" : user);
					break;
				case ("armors", "spriteInv"):
					foreach (var s in Strings(v)) PrefixUsers.Add((s.ToUpperInvariant(), user));
					break;
				case ("soldiers", "rankSprite"):
					foreach (int n in Ints(v)) AddFrame("BASEBITS.PCK", n, user);
					break;
				case ("soldiers", "rankSpriteBattlescape"):
					foreach (int n in Ints(v)) AddFrame("SMOKE.PCK", n, user);
					break;
				case ("crafts", "sprite"):
					foreach (int n in Ints(v))
					{
						AddFrame("INTICON.PCK", n, $"{user} (Liste)");
						AddFrame("INTICON.PCK", n + 11, $"{user} (Abfangfenster, +11)");
						AddFrame("BASEBITS.PCK", n + 33, $"{user} (Hangar, +33)");
					}
					break;
				case ("craftWeapons", "sprite"):
					foreach (int n in Ints(v))
					{
						AddFrame("INTICON.PCK", n + 5, $"{user} (Abfangfenster, +5)");
						AddFrame("BASEBITS.PCK", n + 48, $"{user} (Basis, +48)");
					}
					break;
				case ("facilities", "spriteShape"):
				case ("facilities", "spriteFacility"):
					foreach (int n in Ints(v)) AddFrame("BASEBITS.PCK", n, user);
					break;
				case ("ufos", "sprite"):
					foreach (int n in Ints(v)) AddFile("INTERWIN.DAT", $"{user} (Vorschau-Zeile {n})");
					break;
				case ("ufopaedia", "image_id"):
				case ("interfaces", "backgroundImage"):
				case ("cutscenes", "videos"):
					foreach (var s in Strings(v)) AddFile(s, user);
					break;
				case ("terrains", "mapDataSets"):
					foreach (var s in Strings(v))
					{
						AddFile(s + ".PCK", user);
						AddFile(s + ".MCD", user);
					}
					break;
				case ("terrains", "mapBlocks"):
					if (v is YamlSequenceNode blocks)
					{
						foreach (var block in blocks.Children.OfType<YamlMappingNode>())
						{
							string name = RuleId(block);
							AddFile(name + ".MAP", $"terrains/{id}: mapBlocks");
							AddFile(name + ".RMP", $"terrains/{id}: mapBlocks");
						}
					}
					break;
				case ("soundDefs", "sounds"):
					if (item.Children.TryGetValue(new YamlScalarNode("file"), out var catFile) && Scalar(catFile) is { } fileName)
						SoundDefs[id] = (fileName.ToUpperInvariant(), [.. Ints(v)]);
					break;
				case ("musics", "catPos"):
					foreach (int n in Ints(v)) AddFile("GM.CAT", $"musics/{id}: catPos {n}");
					break;
				case ("extraSprites", "files"):
					if (v is YamlMappingNode files)
					{
						foreach (var (fk, fv) in files.Children)
						{
							if (int.TryParse(Scalar(fk), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
								AddFrame(id, n, $"ersetzt/ergänzt durch Repo-Datei {Scalar(fv)} (extraSprites)");
						}
					}
					break;
				default:
					if (key.Contains("sound", StringComparison.OrdinalIgnoreCase) && section is not ("extraSounds" or "soundDefs"))
					{
						bool geo = s_geoSections.Contains(section) || (section == "constants" && s_geoConstants.Contains(key));
						foreach (int n in Ints(v)) AddSound(geo ? "GEO.CAT" : "BATTLE.CAT", n, user);
					}
					break;
			}
		}
	}
}
