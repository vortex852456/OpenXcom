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
// Ported from: src/Mod/RuleConverter.h, src/Mod/RuleConverter.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents game-specific contents needed
/// for save conversion and ID matching.
/// </summary>
public class RuleConverter
{
	private SortedDictionary<string, int> _offsets = new(StringComparer.Ordinal);
	private List<string> _markers = [], _countries = [], _regions = [], _facilities = [], _items = [], _crews = [], _crafts = [], _ufos = [], _craftWeapons = [], _missions = [], _armor = [], _alienRaces = [], _alienRanks = [], _research = [], _manufacture = [], _ufopaedia = [];

	/// <summary>Creates a blank ruleset for converter data.</summary>
	public RuleConverter()
	{
	}

	/// <summary>Loads the converter data from a YAML file.</summary>
	public void Load(YamlNodeReader node)
	{
		var reader = node.UseIndex();
		reader.TryRead("offsets", ref _offsets);
		reader.TryRead("markers", ref _markers);
		reader.TryRead("countries", ref _countries);
		reader.TryRead("regions", ref _regions);
		reader.TryRead("facilities", ref _facilities);
		reader.TryRead("items", ref _items);
		reader.TryRead("crews", ref _crews);
		reader.TryRead("crafts", ref _crafts);
		reader.TryRead("ufos", ref _ufos);
		reader.TryRead("craftWeapons", ref _craftWeapons);
		reader.TryRead("missions", ref _missions);
		reader.TryRead("armor", ref _armor);
		reader.TryRead("alienRaces", ref _alienRaces);
		reader.TryRead("alienRanks", ref _alienRanks);
		reader.TryRead("research", ref _research);
		reader.TryRead("manufacture", ref _manufacture);
		reader.TryRead("ufopaedia", ref _ufopaedia);
	}

	/// <summary>Gets the offset for a specific attribute (a missing one is added as 0, like std::map::operator[]).</summary>
	public int GetOffset(string id)
	{
		if (!_offsets.TryGetValue(id, out int offset))
		{
			_offsets[id] = offset = 0;
		}
		return offset;
	}

	/// <summary>Gets the marker ID list.</summary>
	public List<string> Markers => _markers;
	/// <summary>Gets the country ID list.</summary>
	public List<string> Countries => _countries;
	/// <summary>Gets the region ID list.</summary>
	public List<string> Regions => _regions;
	/// <summary>Gets the facility ID list.</summary>
	public List<string> Facilities => _facilities;
	/// <summary>Gets the item ID list.</summary>
	public List<string> Items => _items;
	/// <summary>Gets the UFO crew ID list.</summary>
	public List<string> Crews => _crews;
	/// <summary>Gets the craft ID list.</summary>
	public List<string> Crafts => _crafts;
	/// <summary>Gets the UFO ID list.</summary>
	public List<string> Ufos => _ufos;
	/// <summary>Gets the craft weapon ID list.</summary>
	public List<string> CraftWeapons => _craftWeapons;
	/// <summary>Gets the alien mission ID list.</summary>
	public List<string> Missions => _missions;
	/// <summary>Gets the armor ID list.</summary>
	public List<string> Armor => _armor;
	/// <summary>Gets the alien race ID list.</summary>
	public List<string> AlienRaces => _alienRaces;
	/// <summary>Gets the alien rank ID list.</summary>
	public List<string> AlienRanks => _alienRanks;
	/// <summary>Gets the research ID list.</summary>
	public List<string> Research => _research;
	/// <summary>Gets the manufacture ID list.</summary>
	public List<string> Manufacture => _manufacture;
	/// <summary>Gets the ufopaedia ID list.</summary>
	public List<string> Ufopaedia => _ufopaedia;
}
