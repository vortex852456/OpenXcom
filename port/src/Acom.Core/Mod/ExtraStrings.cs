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
// Ported from: src/Mod/ExtraStrings.h, src/Mod/ExtraStrings.cpp
#nullable disable

namespace Acom;

/// <summary>For adding a set of extra strings to the game.</summary>
public class ExtraStrings
{
	private readonly SortedDictionary<string, string> _strings = new(StringComparer.Ordinal);

	/// <summary>Creates a blank set of extra strings data.</summary>
	public ExtraStrings()
	{
	}

	/// <summary>Loads the extra strings set from YAML.</summary>
	public void Load(YamlNodeReader reader)
	{
		foreach (var str in reader["strings"].Children())
		{
			// Regular strings
			string key = str.ReadKey<string>();
			if (str.HasVal)
			{
				_strings[key] = str.ReadVal<string>();
			}
			// Strings with plurality
			else if (str.IsMap)
			{
				foreach (var plurality in str.Children())
				{
					string s = key + "_" + plurality.ReadKey<string>();
					_strings[s] = plurality.ReadVal<string>();
				}
			}
		}
	}

	/// <summary>Gets the list of strings defined by this mod.</summary>
	public SortedDictionary<string, string> Strings => _strings;
}
