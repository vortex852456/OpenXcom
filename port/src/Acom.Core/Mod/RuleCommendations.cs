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
// Ported from: src/Mod/RuleCommendations.h, src/Mod/RuleCommendations.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents a specific type of commendation.
/// Contains constant info about a commendation like award criteria, sprite, description, etc.
/// </summary>
public class RuleCommendations
{
	private readonly string _type;
	private readonly SortedDictionary<string, List<int>> _criteria = new(StringComparer.Ordinal);
	private readonly List<List<(int first, List<string> second)>> _killCriteria = [];
	private string _description;
	private int _sprite;
	private readonly List<string> _soldierBonusTypesNames = [];
	private readonly List<RuleSoldierBonus> _soldierBonusTypes = [];
	private readonly List<string> _missionMarkerNames = []; // these are not alien deployment type names!
	private readonly List<string> _missionTypeNames = [];   // these are not alien deployment type names!
	private readonly List<string> _requiresNames = [];
	private readonly List<RuleResearch> _requires = [];
	private readonly List<string> _unitsNames = [];
	private readonly List<RuleSoldier> _units = [];

	/// <summary>Creates a blank set of commendation data.</summary>
	public RuleCommendations(string type)
	{
		_type = type;
		_description = "";
		_sprite = 0;
	}

	/// <summary>Loads the commendation from YAML.</summary>
	public void Load(YamlNodeReader reader, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent, mod);
		}

		reader.TryRead("description", ref _description);
		mod.LoadUnorderedNamesToInts(_type, _criteria, reader["criteria"]);
		reader.TryRead("sprite", ref _sprite);
		mod.LoadKillCriteria(_type, _killCriteria, reader["killCriteria"]);
		mod.LoadNames(_type, _soldierBonusTypesNames, reader["soldierBonusTypes"]);
		mod.LoadNames(_type, _missionMarkerNames, reader["missionMarkerFilter"]);
		mod.LoadNames(_type, _missionTypeNames, reader["missionTypeFilter"]);

		mod.LoadUnorderedNames(_type, _requiresNames, reader["requires"]);
		mod.LoadUnorderedNames(_type, _unitsNames, reader["units"]);
	}

	/// <summary>Cross link with other rules.</summary>
	public void AfterLoad(Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		mod.LinkRule(_soldierBonusTypes, _soldierBonusTypesNames);

		mod.LinkRule(_requires, _requiresNames);
		mod.LinkRule(_units, _unitsNames);

		// C++ sorts _units by pointer for a binary search in isSupportedBy; the order is not visible otherwise.
	}

	/// <summary>Gets the commendation's type.</summary>
	public string Type { get => _type; }
	/// <summary>Get the commendation's description.</summary>
	public string Description { get => _description; }
	/// <summary>Get the commendation's award criteria.</summary>
	public SortedDictionary<string, List<int>> Criteria { get => _criteria; }
	/// <summary>Get the commendation's award kill related criteria.</summary>
	public List<List<(int first, List<string> second)>> KillCriteria { get => _killCriteria; }
	/// <summary>Get the commendation's sprite.</summary>
	public int Sprite { get => _sprite; }

	/// <summary>Gets the soldier bonus type corresponding to the commendation's decoration level.</summary>
	public RuleSoldierBonus GetSoldierBonus(int decorationLevel)
	{
		if (_soldierBonusTypes.Count != 0)
		{
			int lastIndex = _soldierBonusTypes.Count - 1;
			int index = decorationLevel > lastIndex ? lastIndex : decorationLevel;
			return _soldierBonusTypes[index];
		}
		return null;
	}

	/// <summary>Gets the commendation's mission marker filter.</summary>
	public List<string> MissionMarkerNames { get => _missionMarkerNames; }
	/// <summary>Gets the commendation's mission type filter.</summary>
	public List<string> MissionTypeNames { get => _missionTypeNames; }
	/// <summary>Gets the commendation's research requirements.</summary>
	public List<RuleResearch> Requires { get => _requires; }

	/// <summary>Check if a given soldier type can be awarded this commendation.</summary>
	public bool IsSupportedBy(RuleSoldier soldier) => _units.Count == 0 || _units.Contains(soldier);
}
