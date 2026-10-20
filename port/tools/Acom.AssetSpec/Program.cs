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

using System.Text.Json;
using System.Text.Json.Serialization;
using Acom.AssetSpec;

// Writes a metadata-only inventory of the original game data (see content/spec/README.md).
// Usage: acom-assetspec --data <folder with GEODATA, UFOGRAPH, ...> [--game ufo|tftd] [--rules <dir>] [--out <dir>] [--hd-scale 4]

var options = Options.Parse(args);
if (options == null)
{
	Console.Error.WriteLine("Usage: acom-assetspec --data <Originaldaten-Ordner> [--game ufo|tftd] [--rules <Ruleset-Ordner>] [--out <Ausgabeordner>] [--hd-scale 4]");
	return 2;
}

Console.WriteLine($"Lese Originaldaten ({options.Game}) ...");
var scanner = new DataScanner(options.Data, options.HdScale);
var spec = new AssetSpecification
{
	Game = options.Game,
	Rules = options.RulesDisplay,
	HdScale = options.HdScale,
	Files = scanner.ScanAll(),
};
Console.WriteLine($"{spec.Files.Count} Dateien analysiert. Lese Rulesets aus {options.RulesDisplay} ...");
var index = RulesetIndex.Load(options.Rules);
Merger.Apply(spec, index);

Directory.CreateDirectory(options.Out);
var json = Path.Combine(options.Out, "assets.json");
await using (var stream = File.Create(json))
	await JsonSerializer.SerializeAsync(stream, spec, Merger.JsonOptions);
var md = Path.Combine(options.Out, "ASSET-LIST.md");
await File.WriteAllTextAsync(md, MarkdownReport.Render(spec));
Console.WriteLine($"Geschrieben: {json}");
Console.WriteLine($"Geschrieben: {md}");
return 0;

namespace Acom.AssetSpec
{
	internal sealed record Options(string Data, string Game, string Rules, string RulesDisplay, string Out, int HdScale)
	{
		public static Options? Parse(string[] args)
		{
			string? data = null, rules = null, outDir = null;
			string game = "ufo";
			int hdScale = 4;
			for (int i = 0; i + 1 < args.Length; i += 2)
			{
				switch (args[i])
				{
					case "--data": data = args[i + 1]; break;
					case "--game": game = args[i + 1].ToLowerInvariant(); break;
					case "--rules": rules = args[i + 1]; break;
					case "--out": outDir = args[i + 1]; break;
					case "--hd-scale": hdScale = int.Parse(args[i + 1], System.Globalization.CultureInfo.InvariantCulture); break;
					default: return null;
				}
			}
			if (data == null || !Directory.Exists(data) || game is not ("ufo" or "tftd"))
				return null;
			string? repo = FindRepoRoot();
			rules ??= repo != null ? Path.Combine(repo, "bin", "standard", game == "ufo" ? "xcom1" : "xcom2") : null;
			outDir ??= repo != null ? Path.Combine(repo, "content", "spec", game) : null;
			if (rules == null || outDir == null || !Directory.Exists(rules))
				return null;
			string display = repo != null ? Path.GetRelativePath(repo, rules).Replace('\\', '/') : Path.GetFileName(rules);
			return new Options(data, game, rules, display, outDir, hdScale);
		}

		/// <summary>Walks up from the working directory to the folder containing port/Acom.slnx.</summary>
		private static string? FindRepoRoot()
		{
			for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir != null; dir = dir.Parent)
			{
				if (File.Exists(Path.Combine(dir.FullName, "port", "Acom.slnx")))
					return dir.FullName;
			}
			return null;
		}
	}

	/// <summary>Attaches ruleset references to the scanned files.</summary>
	internal static class Merger
	{
		public static readonly JsonSerializerOptions JsonOptions = new()
		{
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		};

		public static void Apply(AssetSpecification spec, RulesetIndex index)
		{
			var byName = spec.Files
				.GroupBy(f => f.Path[(f.Path.LastIndexOf('/') + 1)..], StringComparer.Ordinal)
				.ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
			var unresolved = new SortedSet<string>(StringComparer.Ordinal);

			foreach (var (name, users) in index.FileUsers)
			{
				if (byName.TryGetValue(name, out var entries))
					entries.ForEach(e => e.UsedBy.AddRange(users));
				else if (name.Contains('.', StringComparison.Ordinal))
					unresolved.Add($"{name} ← {string.Join("; ", users.Distinct())}");
			}

			foreach (var (name, frames) in index.FrameUsers)
			{
				if (!byName.TryGetValue(name, out var entries))
				{
					if (name.Contains('.', StringComparison.Ordinal))
						unresolved.Add($"{name} ← {frames.Count} Frame-Verweise");
					continue;
				}
				foreach (var entry in entries)
				{
					var missing = new List<int>();
					foreach (var (frame, users) in frames)
					{
						if (entry.FrameDetails != null && frame < entry.FrameDetails.Count)
							entry.FrameDetails[frame].UsedBy.AddRange(users);
						else
							missing.Add(frame);
					}
					if (missing.Count > 0)
					{
						string example = string.Join("; ", frames[missing[0]]);
						entry.Notes.Add($"Verweise auf Frames, die in den Originaldaten fehlen (vom Repo/Mod ergänzt): {MarkdownReport.Ranges(missing)} – z. B. {example}");
					}
				}
			}

			foreach (var (prefix, user) in index.PrefixUsers)
			{
				foreach (var entry in spec.Files)
				{
					string name = entry.Path[(entry.Path.LastIndexOf('/') + 1)..];
					if (name.StartsWith(prefix, StringComparison.Ordinal) && entry.Format is "SPK" or "BDY")
						entry.UsedBy.Add(user);
				}
			}

			foreach (var (set, sounds) in index.SoundSets)
				spec.SoundSets[set] = sounds;
			if (index.SoundDefs.Count > 0)
			{
				foreach (var (set, (file, physical)) in index.SoundDefs)
					AttachSounds(spec, set, ["SOUND/" + file], physical);
			}
			else
			{
				AttachSounds(spec, "BATTLE.CAT", ["SOUND/SAMPLE2.CAT", "SOUND/SOUND1.CAT"], null);
				AttachSounds(spec, "GEO.CAT", ["SOUND/SAMPLE.CAT", "SOUND/SOUND2.CAT"], null);
			}

			spec.UnresolvedReferences.AddRange(unresolved);
		}

		/// <summary>
		/// Maps a logical sound set onto its physical archive. UFO uses one of two archives (Windows or DOS
		/// version) with identical numbering; TFTD maps each logical index through soundDefs.
		/// </summary>
		private static void AttachSounds(AssetSpecification spec, string set, string[] candidates, List<int>? physical)
		{
			if (!spec.SoundSets.TryGetValue(set, out var sounds))
				return;
			var archive = candidates.Select(c => spec.Files.FirstOrDefault(f => f.Path == c)).FirstOrDefault(f => f?.SoundDetails != null);
			if (archive == null)
				return;
			archive.Notes.Add($"Enthält den logischen Soundsatz {set}");
			foreach (var (n, users) in sounds)
			{
				int target = physical == null ? n : (n < physical.Count ? physical[n] : -1);
				if (target >= 0 && target < archive.SoundDetails!.Count)
					archive.SoundDetails[target].UsedBy.AddRange(users.Select(u => $"{u} ({set} #{n})"));
			}
		}
	}
}
