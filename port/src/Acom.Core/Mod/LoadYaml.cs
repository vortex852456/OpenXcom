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
// Ported from: src/Mod/LoadYaml.h
#nullable disable

namespace Acom;

/// <summary>Small helpers for loading rule values from YAML (the C++ free functions of LoadYaml.h).</summary>
public static class LoadYaml
{
	/// <summary>Default value that is interpreted as null.</summary>
	public const int defIntNullable = -1;

	/// <summary>Default value that is interpreted as null.</summary>
	public const sbyte defBoolNullable = -1;

	/// <summary>Load bool from yaml.</summary>
	public static void LoadBool(ref bool value, YamlNodeReader reader)
	{
		if (!reader)
			return;
		value = reader.ReadVal<bool>();
	}

	/// <summary>Load bool as int from yaml.</summary>
	public static void LoadBoolNullable(ref int value, YamlNodeReader reader)
	{
		if (!reader)
			return;
		value = reader.HasNullVal ? defBoolNullable : (reader.ReadVal<bool>() ? 1 : 0);
	}

	/// <summary>Load bool as int from yaml.</summary>
	public static void LoadBoolNullable(ref sbyte value, YamlNodeReader reader)
	{
		if (!reader)
			return;
		value = reader.HasNullVal ? defBoolNullable : (sbyte)(reader.ReadVal<bool>() ? 1 : 0);
	}

	/// <summary>Load int from yaml.</summary>
	public static void LoadInt(ref int value, YamlNodeReader reader)
	{
		if (!reader)
			return;
		value = reader.ReadVal<int>();
	}

	/// <summary>Load int from yaml.</summary>
	public static void LoadIntNullable(ref int value, YamlNodeReader reader)
	{
		if (!reader)
			return;
		value = reader.HasNullVal ? defIntNullable : reader.ReadVal<int>();
	}

	public static bool UseBoolNullable(int value, bool def) => value == defBoolNullable ? def : value != 0;

	public static int UseIntNullable(int value, int def) => value == defIntNullable ? def : value;
}
