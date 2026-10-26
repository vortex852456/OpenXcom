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

namespace Acom.Placeholder;

/// <summary>A map block declared in a terrain rule.</summary>
public sealed record BlockRule(string Name, int Width, int Length, string Terrain, IReadOnlyList<string> MapDataSets, bool Vehicle);

/// <summary>A longitude/latitude rectangle in degrees (RuleRegion/RuleCountry areas, mission zones); lonMin &gt; lonMax wraps.</summary>
public readonly record struct Area(double LonMin, double LonMax, double LatMin, double LatMax);

/// <summary>
/// The parts of the master mod's rulesets the placeholder data must match: map blocks of all terrains
/// (including UFO and craft terrains) and the globe areas of regions, countries and mission zones.
/// </summary>
public sealed class Rules
{
	/// <summary>Map blocks by name (first declaration wins).</summary>
	public SortedDictionary<string, BlockRule> Blocks { get; } = new(StringComparer.Ordinal);

	/// <summary>Areas that must be land: countries and the region mission zones except the flight zone.</summary>
	public List<Area> LandAreas { get; } = [];

	/// <summary>
	/// Terrain sets whose floors are soldier start points (MCD target type START_POINT): the craft terrains, and the
	/// terrains of missions without a craft (base defense and follow-up stages, which deploy on START_POINT tiles).
	/// </summary>
	public SortedSet<string> StartPointSets { get; } = new(StringComparer.OrdinalIgnoreCase);

	private readonly Dictionary<string, List<string>> _terrainSets = new(StringComparer.Ordinal);
	private readonly List<(string Type, List<string> Terrains, string? NextStage)> _deployments = [];

	/// <summary>Reads all *.rul files of a ruleset folder (sorted, like the engine).</summary>
	public static Rules Load(string rulesDir)
	{
		var rules = new Rules();
		foreach (var file in Directory.EnumerateFiles(rulesDir, "*.rul", SearchOption.AllDirectories).Order(StringComparer.OrdinalIgnoreCase))
		{
			var yaml = new YamlStream();
			using (var reader = new StreamReader(file))
				yaml.Load(reader);
			foreach (var doc in yaml.Documents)
			{
				if (doc.RootNode is YamlMappingNode root)
					rules.Read(root);
			}
		}
		var noCraft = new HashSet<string>(rules._deployments.Select(d => d.NextStage).OfType<string>(), StringComparer.Ordinal) { "STR_BASE_DEFENSE" };
		foreach (var (type, terrains, _) in rules._deployments)
		{
			if (!noCraft.Contains(type))
				continue;
			foreach (var terrain in terrains)
				if (rules._terrainSets.TryGetValue(terrain, out var sets))
					rules.StartPointSets.UnionWith(sets);
		}
		rules.StartPointSets.Remove("BLANKS");
		return rules;
	}

	private void Read(YamlMappingNode root)
	{
		foreach (var (key, value) in root.Children)
		{
			if (value is not YamlSequenceNode seq)
				continue;
			string section = Scalar(key) ?? "";
			foreach (var item in seq.Children.OfType<YamlMappingNode>())
			{
				switch (section)
				{
					case "terrains":
						ReadTerrain(item, vehicle: false);
						break;
					case "ufos":
					case "crafts":
						if (Child(item, "battlescapeTerrainData") is YamlMappingNode terrain)
						{
							var sets = ReadTerrain(terrain, vehicle: true);
							if (section == "crafts")
								StartPointSets.UnionWith(sets);
						}
						break;
					case "alienDeployments":
					{
						string type = Scalar(Child(item, "type")) ?? "";
						var terrains = (Child(item, "terrains") as YamlSequenceNode)?.Children.Select(Scalar).OfType<string>().ToList() ?? [];
						_deployments.Add((type, terrains, Scalar(Child(item, "nextStage"))));
						break;
					}
					case "countries":
						ReadAreas(Child(item, "areas"));
						break;
					case "regions":
						// region areas cover the oceans too, and zone 0 holds the UFO flight areas: only the other
						// mission zones (landing sites, cities, alien bases, ...) need land
						if (Child(item, "missionZones") is YamlSequenceNode zones)
						{
							foreach (var zone in zones.Children.Skip(1))
								ReadAreas(zone);
						}
						break;
				}
			}
		}
	}

	private List<string> ReadTerrain(YamlMappingNode terrain, bool vehicle)
	{
		string name = Scalar(Child(terrain, "name")) ?? "";
		var sets = (Child(terrain, "mapDataSets") as YamlSequenceNode)?.Children.Select(Scalar).OfType<string>().ToList() ?? [];
		_terrainSets.TryAdd(name, sets);
		if (Child(terrain, "mapBlocks") is not YamlSequenceNode blocks)
			return sets;
		foreach (var block in blocks.Children.OfType<YamlMappingNode>())
		{
			string? blockName = Scalar(Child(block, "name"));
			if (blockName == null || Blocks.ContainsKey(blockName))
				continue;
			int width = Int(Child(block, "width"), 10), length = Int(Child(block, "length"), 10);
			Blocks[blockName] = new BlockRule(blockName, width, length, name, sets, vehicle);
		}
		return sets;
	}

	private void ReadAreas(YamlNode? areas)
	{
		if (areas is not YamlSequenceNode seq)
			return;
		foreach (var area in seq.Children.OfType<YamlSequenceNode>())
		{
			if (area.Children.Count < 4)
				continue;
			LandAreas.Add(new Area(Num(area[0]), Num(area[1]), Num(area[2]), Num(area[3])));
		}
	}

	private static YamlNode? Child(YamlMappingNode node, string key) =>
		node.Children.TryGetValue(new YamlScalarNode(key), out var value) ? value : null;

	private static string? Scalar(YamlNode? node) => (node as YamlScalarNode)?.Value;

	private static int Int(YamlNode? node, int fallback) =>
		int.TryParse(Scalar(node), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : fallback;

	private static double Num(YamlNode node) =>
		double.TryParse(Scalar(node), NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0;
}
