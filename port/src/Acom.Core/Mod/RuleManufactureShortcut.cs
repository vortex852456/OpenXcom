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
// Ported from: src/Mod/RuleManufactureShortcut.h, src/Mod/RuleManufactureShortcut.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents the information needed to create a new manufacture rule based on some existing manufacture rule.
/// </summary>
public class RuleManufactureShortcut
{
	private readonly string _name;
	private string _startFrom = "";
	private List<string> _breakDownItems = [];
	private bool _breakDownRequires, _breakDownRequiresBaseFunc;

	/// <summary>Creates a new RuleManufactureShortcut.</summary>
	/// <param name="name">The unique manufacture name.</param>
	public RuleManufactureShortcut(string name)
	{
		_name = name;
		_breakDownRequires = false;
		_breakDownRequiresBaseFunc = true;
	}

	/// <summary>Loads the RuleManufactureShortcut from YAML.</summary>
	public void Load(YamlNodeReader reader)
	{
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent);
		}

		reader.TryRead("startFrom", ref _startFrom);
		reader.TryRead("breakDownItems", ref _breakDownItems);
		reader.TryRead("breakDownRequires", ref _breakDownRequires);
		reader.TryRead("breakDownRequiresBaseFunc", ref _breakDownRequiresBaseFunc);
	}

	/// <summary>Gets the name of a new manufacture rule to be created.</summary>
	public string Name => _name;

	/// <summary>Gets the name of a manufacture rule to be used as a starting point.</summary>
	public string StartFrom => _startFrom;

	/// <summary>Gets the names of items that should be broken down into simpler components.</summary>
	public List<string> BreakDownItems => _breakDownItems;

	/// <summary>Gets whether or not the required research should also be broken down.</summary>
	public bool BreakDownRequires => _breakDownRequires;

	/// <summary>Gets whether or not the required base services should also be broken down.</summary>
	public bool BreakDownRequiresBaseFunc => _breakDownRequiresBaseFunc;
}
