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
using System.Text;

namespace Acom.AssetSpec;

/// <summary>Renders the specification as a readable German Markdown document.</summary>
internal static class MarkdownReport
{
	private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

	public static string Render(AssetSpecification spec)
	{
		var md = new StringBuilder();
		string game = spec.Game == "ufo" ? "UFO" : "TFTD";
		md.Append(Inv, $"# Asset-Liste: {game}\n\n");
		md.Append("> Automatisch erzeugt von `port/tools/Acom.AssetSpec` aus lokal vorhandenen Originaldaten.\n");
		md.Append(Inv, $"> Enthält nur technische Fakten (Größen, Frames, belegte Bereiche, Farbgruppen, Verwendung laut `{spec.Rules}`) –\n");
		md.Append("> keine Pixel, Farbwerte, Texte oder Töne. Einträge unter „Verwendung“ sind Slot-Bezeichner der Engine, keine Namen für ACOM-Inhalte.\n");
		md.Append(Inv, $"> HD-Zielgröße = klassische Größe × {spec.HdScale}. Regeln für neue Inhalte: `docs/architecture/content-rights.md`.\n\n");

		Overview(md, spec);
		Sprites(md, spec);
		Images(md, spec);
		Terrain(md, spec);
		Maps(md, spec);
		Sounds(md, spec);
		Other(md, spec);

		if (spec.UnresolvedReferences.Count > 0)
		{
			md.Append("## Verweise ohne Originaldatei\n\n");
			md.Append("Ruleset-Verweise auf Dateien, die in den Originaldaten fehlen (meist vom Repo/Mod bereitgestellt):\n\n");
			foreach (var r in spec.UnresolvedReferences)
				md.Append(Inv, $"- {Escape(r)}\n");
			md.Append('\n');
		}
		return md.ToString();
	}

	private static void Overview(StringBuilder md, AssetSpecification spec)
	{
		md.Append("## Übersicht\n\n| Kategorie | Dateien | Frames/Einträge | Bytes |\n|---|---:|---:|---:|\n");
		foreach (var g in spec.Files.GroupBy(f => f.Category).OrderBy(g => g.Key, StringComparer.Ordinal))
		{
			long items = g.Sum(f => (long)(f.Frames ?? f.Records ?? 0));
			md.Append(Inv, $"| {g.Key} | {g.Count()} | {items} | {g.Sum(f => f.Bytes)} |\n");
		}
		md.Append('\n');
	}

	private static void Sprites(StringBuilder md, AssetSpecification spec)
	{
		var sets = spec.Files.Where(f => f.Category == "sprites").ToList();
		if (sets.Count == 0)
			return;
		md.Append("## Sprite-Sets\n\n");
		foreach (var f in sets)
		{
			md.Append(Inv, $"### {f.Path}\n\n");
			md.Append(Inv, $"{f.Purpose} · Frame {f.Width}×{f.Height} → HD {f.HdWidth}×{f.HdHeight} · {f.Frames} Frames ({f.NonEmptyFrames} belegt) · Farbgruppen: {Groups(f.ColorGroups)}\n\n");
			if (f.UsedBy.Count > 0)
				md.Append(Inv, $"Verwendung: {Escape(string.Join("; ", f.UsedBy.Distinct()))}\n\n");
			var used = f.FrameDetails!.Where(fr => fr.UsedBy.Count > 0).ToList();
			if (used.Count > 0)
			{
				md.Append("| Frame | Bereich x,y,b×h | Farbgruppen | Verwendung |\n|---:|---|---|---|\n");
				foreach (var fr in used)
					md.Append(Inv, $"| {fr.Index} | {Bounds(fr)} | {Groups(fr.ColorGroups)} | {Escape(string.Join("; ", fr.UsedBy.Distinct()))} |\n");
				md.Append('\n');
			}
			var unused = f.FrameDetails!.Where(fr => fr.UsedBy.Count == 0 && !fr.Empty).Select(fr => fr.Index).ToList();
			if (unused.Count > 0)
				md.Append(Inv, $"Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): {Ranges(unused)}\n\n");
			Notes(md, f);
		}
	}

	private static void Images(StringBuilder md, AssetSpecification spec)
	{
		var images = spec.Files.Where(f => f.Category == "images").ToList();
		if (images.Count == 0)
			return;
		md.Append("## Vollbilder und Einzelgrafiken\n\n| Datei | Format | Größe → HD | Farbgruppen | Zweck | Verwendung |\n|---|---|---|---|---|---|\n");
		foreach (var f in images)
			md.Append(Inv, $"| {f.Path} | {f.Format} | {f.Width}×{f.Height} → {f.HdWidth}×{f.HdHeight} | {Groups(f.ColorGroups)} | {Escape(f.Purpose)} | {Escape(string.Join("; ", f.UsedBy.Distinct()))} |\n");
		md.Append('\n');
	}

	private static void Terrain(StringBuilder md, AssetSpecification spec)
	{
		var terrain = spec.Files.Where(f => f.Category == "terrain").ToList();
		if (terrain.Count == 0)
			return;
		md.Append("## Geländesätze (TERRAIN)\n\n| Satz | Kacheln 32×40 (belegt) | MCD-Einträge | Farbgruppen | Verwendet von |\n|---|---:|---:|---|---|\n");
		foreach (var g in terrain.GroupBy(f => f.Path[..^4], StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal))
		{
			var pck = g.FirstOrDefault(f => f.Format == "PCK");
			var mcd = g.FirstOrDefault(f => f.Format == "MCD");
			var users = g.SelectMany(f => f.UsedBy).Distinct();
			md.Append(Inv, $"| {g.Key} | {pck?.Frames} ({pck?.NonEmptyFrames}) | {mcd?.Records} | {Groups(pck?.ColorGroups)} | {Escape(string.Join("; ", users))} |\n");
		}
		md.Append('\n');
	}

	private static void Maps(StringBuilder md, AssetSpecification spec)
	{
		var maps = spec.Files.Where(f => f.Category is "maps" or "routes").ToList();
		if (maps.Count == 0)
			return;
		md.Append("## Kartenblöcke (MAPS) und Wegpunkte (ROUTES)\n\n| Block | Größe x×y×z | Wegpunkte | Verwendet von |\n|---|---|---:|---|\n");
		foreach (var g in maps.GroupBy(f => Path.GetFileNameWithoutExtension(f.Path), StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal))
		{
			var map = g.FirstOrDefault(f => f.Format == "MAP");
			var rmp = g.FirstOrDefault(f => f.Format == "RMP");
			string size = map?.MapSize is { } s ? $"{s[0]}×{s[1]}×{s[2]}" : "–";
			md.Append(Inv, $"| {g.Key} | {size} | {rmp?.Records} | {Escape(string.Join("; ", g.SelectMany(f => f.UsedBy).Distinct()))} |\n");
		}
		md.Append('\n');
	}

	private static void Sounds(StringBuilder md, AssetSpecification spec)
	{
		var archives = spec.Files.Where(f => f.SoundDetails != null).ToList();
		if (archives.Count == 0)
			return;
		md.Append("## Soundeffekte\n\n");
		foreach (var f in archives)
		{
			md.Append(Inv, $"### {f.Path}\n\n{f.Purpose} · {f.Records} Einträge\n\n");
			Notes(md, f);
			md.Append("| # | Format | Rate | Sekunden | Verwendung |\n|---:|---|---:|---:|---|\n");
			foreach (var s in f.SoundDetails!)
				md.Append(Inv, $"| {s.Index} | {s.Format} | {s.SampleRate} | {s.Seconds?.ToString("0.00", Inv)} | {Escape(string.Join("; ", s.UsedBy.Distinct()))} |\n");
			md.Append('\n');
		}
	}

	private static void Other(StringBuilder md, AssetSpecification spec)
	{
		var rest = spec.Files.Where(f => f.Category is not ("sprites" or "images" or "terrain" or "maps" or "routes") && f.SoundDetails == null).ToList();
		if (rest.Count == 0)
			return;
		md.Append("## Musik, Videos, Paletten, Schriften, Texte, Daten\n\n| Datei | Kategorie | Format | Details | Zweck | Verwendung |\n|---|---|---|---|---|---|\n");
		foreach (var f in rest)
		{
			var details = new List<string>();
			if (f.Width != null)
				details.Add($"{f.Width}×{f.Height}");
			if (f.Frames != null)
				details.Add($"{f.Frames} Frames");
			if (f.Records != null)
				details.Add($"{f.Records} Einträge");
			details.Add($"{f.Bytes} Bytes");
			details.AddRange(f.Notes);
			md.Append(Inv, $"| {f.Path} | {f.Category} | {f.Format} | {Escape(string.Join(", ", details))} | {Escape(f.Purpose)} | {Escape(string.Join("; ", f.UsedBy.Distinct()))} |\n");
		}
		md.Append('\n');
	}

	private static void Notes(StringBuilder md, AssetEntry f)
	{
		foreach (var n in f.Notes)
			md.Append(Inv, $"- Hinweis: {Escape(n)}\n");
		if (f.Notes.Count > 0)
			md.Append('\n');
	}

	private static string Groups(int[]? groups) => groups == null || groups.Length == 0 ? "–" : string.Join(",", groups);

	private static string Bounds(FrameInfo f) => f.Bounds is { } b ? $"{b[0]},{b[1]},{b[2]}×{b[3]}" : "leer";

	private static string Escape(string s) => s.Replace("|", "\\|", StringComparison.Ordinal);

	/// <summary>Formats sorted numbers as ranges: 0–3, 7, 9–12.</summary>
	internal static string Ranges(IReadOnlyList<int> sorted)
	{
		var parts = new List<string>();
		for (int i = 0; i < sorted.Count;)
		{
			int j = i;
			while (j + 1 < sorted.Count && sorted[j + 1] == sorted[j] + 1)
				++j;
			parts.Add(i == j ? sorted[i].ToString(Inv) : $"{sorted[i]}–{sorted[j]}");
			i = j + 1;
		}
		return string.Join(", ", parts);
	}
}
