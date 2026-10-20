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
// Ported from: src/Savegame/Region.h, src/Savegame/Region.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents a region of the world.
/// Contains variable info about a region like
/// X-Com and alien activity in it.
/// </summary>
public class Region
{
	private readonly RuleRegion _rules;
	private List<int> _activityXcom = [], _activityAlien = [];

	/// <summary>Initializes a region of the specified type.</summary>
	public Region(RuleRegion rules)
	{
		_rules = rules;
		_activityAlien.Add(0);
		_activityXcom.Add(0);
	}

	/// <summary>Loads the region from a YAML file.</summary>
	public void Load(YamlNodeReader reader)
	{
		reader.TryRead("activityXcom", ref _activityXcom);
		reader.TryRead("activityAlien", ref _activityAlien);
	}

	/// <summary>Saves the region to a YAML file.</summary>
	public void Save(YamlNodeWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.SetAsMap();
		writer.Write("type", _rules.Type);
		writer.Write("activityXcom", _activityXcom);
		writer.Write("activityAlien", _activityAlien);
	}

	/// <summary>Ruleset for the region's type.</summary>
	public RuleRegion Rules => _rules;

	/// <summary>Adds to the region's xcom activity level.</summary>
	public void AddActivityXcom(int activity)
	{
		_activityXcom[^1] += activity;
	}

	/// <summary>Adds to the region's alien activity level.</summary>
	public void AddActivityAlien(int activity)
	{
		_activityAlien[^1] += activity;
	}

	/// <summary>The region's xcom activity level (one entry per month).</summary>
	public List<int> ActivityXcom => _activityXcom;

	/// <summary>The region's alien activity level (one entry per month).</summary>
	public List<int> ActivityAlien => _activityAlien;

	/// <summary>Store last month's counters, start new counters.</summary>
	public void NewMonth()
	{
		_activityAlien.Add(0);
		_activityXcom.Add(0);
		if (_activityAlien.Count > 12)
			_activityAlien.RemoveAt(0);
		if (_activityXcom.Count > 12)
			_activityXcom.RemoveAt(0);
	}
}
