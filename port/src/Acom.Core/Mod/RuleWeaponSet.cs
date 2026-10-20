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
// Ported from: src/Mod/RuleWeaponSet.h, src/Mod/RuleWeaponSet.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents a weapon set.
/// </summary>
public class RuleWeaponSet
{
	private readonly string _type;
	private readonly List<string> _weaponNames = [];
	private readonly List<RuleItem> _weapons = [];

	/// <summary>Creates a blank ruleset for a weapon set.</summary>
	public RuleWeaponSet(string type)
	{
		_type = type;
	}

	/// <summary>Loads the weapon set from YAML.</summary>
	public void Load(YamlNodeReader reader, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent, mod);
		}

		mod.LoadUnorderedNames(_type, _weaponNames, reader["weapons"]);
	}

	/// <summary>Cross link with other Rules.</summary>
	public void AfterLoad(Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		mod.LinkRule(_weapons, _weaponNames);
	}

	/// <summary>Gets the weapon set type.</summary>
	public string Type { get => _type; }
	/// <summary>Gets the weapon set content.</summary>
	public List<RuleItem> Weapons { get => _weapons; }
}
