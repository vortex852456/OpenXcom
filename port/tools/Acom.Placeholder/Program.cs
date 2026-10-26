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
using Acom.Placeholder;

// Writes a procedurally generated placeholder data set in the layout of the original data folder.
// Usage: acom-placeholder --out <dir> [--spec <assets.json>] [--rules <mod folder>] [--seed <n>]

string? outDir = null, specPath = null, rulesDir = null;
uint seed = PlaceholderGenerator.DefaultSeed;
for (int i = 0; i + 1 < args.Length; i += 2)
{
	switch (args[i])
	{
		case "--out": outDir = args[i + 1]; break;
		case "--spec": specPath = args[i + 1]; break;
		case "--rules": rulesDir = args[i + 1]; break;
		case "--seed": seed = uint.Parse(args[i + 1], CultureInfo.InvariantCulture); break;
		default: outDir = null; i = args.Length; break;
	}
}
if (outDir == null || args.Length % 2 != 0)
{
	Console.Error.WriteLine("Usage: acom-placeholder --out <Zielordner> [--spec <assets.json>] [--rules <Mod-Ordner>] [--seed <n>]");
	return 2;
}

string? root = RepoPaths.FindRoot();
specPath ??= root == null ? null : Path.Combine(root, "content", "spec", "ufo", "assets.json");
rulesDir ??= root == null ? null : Path.Combine(root, "bin", "standard", "xcom1");
if (specPath == null || !File.Exists(specPath) || rulesDir == null || !Directory.Exists(rulesDir))
{
	Console.Error.WriteLine("Asset-Liste oder Rulesets nicht gefunden; --spec und --rules angeben.");
	return 2;
}

var generator = new PlaceholderGenerator(AssetList.Load(specPath), Rules.Load(rulesDir), rulesDir, seed);
var report = generator.Generate(outDir);
Console.WriteLine($"{report.Written.Count} Dateien ({report.Bytes / 1024} KiB) nach {outDir} geschrieben, {report.Skipped.Count} nicht erzeugt.");
foreach (var skipped in report.Skipped)
	Console.WriteLine("  nicht erzeugt: " + skipped);
return 0;
