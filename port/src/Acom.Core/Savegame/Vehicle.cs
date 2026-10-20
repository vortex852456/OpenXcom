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
// Ported from: src/Savegame/Vehicle.h, src/Savegame/Vehicle.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents a vehicle (tanks etc.) kept in a craft.
/// Contains variable info about a vehicle like ammo.
/// </summary>
public class Vehicle
{
	private readonly RuleItem _rules;
	private int _ammo, _size;

	/// <summary>Initializes a vehicle of the specified type.</summary>
	/// <param name="rules">Pointer to ruleset.</param>
	/// <param name="ammo">Initial ammo.</param>
	/// <param name="size">Size in tiles.</param>
	public Vehicle(RuleItem rules, int ammo, int size)
	{
		_rules = rules;
		_ammo = ammo;
		_size = size;
	}

	/// <summary>Loads the vehicle from YAML.</summary>
	public void Load(YamlNodeReader reader)
	{
		reader.TryRead("ammo", ref _ammo);
		reader.TryRead("size", ref _size);
	}

	/// <summary>Saves the vehicle to YAML.</summary>
	public void Save(YamlNodeWriter writer)
	{
		writer.SetAsMap();
		writer.Write("type", _rules.Type);
		writer.Write("ammo", _ammo);
		writer.Write("size", _size);
	}

	/// <summary>Returns the ruleset for the vehicle's type.</summary>
	public RuleItem Rules => _rules;

	/// <summary>The ammo contained in this vehicle (-1 = unlimited, reported as 255; setting keeps unlimited ammo).</summary>
	public int Ammo
	{
		get
		{
			if (_ammo == -1)
			{
				return 255;
			}
			return _ammo;
		}
		set
		{
			if (_ammo != -1)
			{
				_ammo = value;
			}
		}
	}

	/// <summary>Returns the size occupied by this vehicle in a transport craft (in tiles).</summary>
	public int TotalSize => _size;
}
