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
// Ported from: src/Mod/CustomPalettes.h, src/Mod/CustomPalettes.cpp
#nullable disable

namespace Acom;

/// <summary>For adding custom palettes to the game.</summary>
public class CustomPalettes
{
	private readonly string _type;
	private string _target = "", _file = "";
	private SortedDictionary<int, Position> _palette = [];

	/// <summary>Creates a blank custom palette.</summary>
	public CustomPalettes(string type)
	{
		_type = type;
	}

	/// <summary>Loads the custom palette from YAML.</summary>
	public void Load(YamlNodeReader reader)
	{
		reader.TryRead("target", ref _target);
		reader.TryRead("file", ref _file);
		reader.TryRead("palette", ref _palette);
	}

	/// <summary>Returns the custom palette definition (list of RGB values).</summary>
	public SortedDictionary<int, Position> Palette => _palette;

	/// <summary>Gets the name of the palette.</summary>
	public string Type => _type;

	/// <summary>Gets the name of the target palette to be modded.</summary>
	public string Target => _target;

	/// <summary>Gets the source file in JASC palette format (256 colors).</summary>
	public string File => _file;
}
