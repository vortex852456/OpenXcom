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
// Ported from: src/Mod/RuleEnviroEffects.h, src/Mod/RuleEnviroEffects.cpp
#nullable disable

namespace Acom;

/// <summary>An environmental condition (e.g. a weapon or ammo fired at units of a faction each turn).</summary>
public struct EnvironmentalCondition
{
	public int globalChance;
	public int chancePerTurn;
	public int firstTurn;
	public int lastTurn;
	public string message;
	public int color;
	public string weaponOrAmmo;
	public int side;
	public int bodyPart;

	public EnvironmentalCondition()
	{
		globalChance = 100;
		chancePerTurn = 0;
		firstTurn = 1;
		lastTurn = 1000;
		message = "";
		color = 29;
		weaponOrAmmo = "";
		side = -1;
		bodyPart = -1;
	}

	/// <summary>YAML deserialization (C++ read(ryml::ConstNodeRef, EnvironmentalCondition*)).</summary>
	public static bool YamlRead(YamlNodeReader reader, ref EnvironmentalCondition val)
	{
		reader.TryRead("globalChance", ref val.globalChance);
		reader.TryRead("chancePerTurn", ref val.chancePerTurn);
		reader.TryRead("firstTurn", ref val.firstTurn);
		reader.TryRead("lastTurn", ref val.lastTurn);
		reader.TryRead("message", ref val.message);
		reader.TryRead("color", ref val.color);
		reader.TryRead("weaponOrAmmo", ref val.weaponOrAmmo);
		reader.TryRead("side", ref val.side);
		reader.TryRead("bodyPart", ref val.bodyPart);
		return true;
	}
}

/// <summary>
/// Represents a specific set of environmental effects (environmental conditions, palette and armor transformations, ...).
/// </summary>
public class RuleEnviroEffects
{
	private readonly string _type;
	private SortedDictionary<string, EnvironmentalCondition> _environmentalConditions = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, string> _paletteTransformations = new(StringComparer.Ordinal);
	private readonly SortedDictionary<string, string> _armorTransformationsName = new(StringComparer.Ordinal);
	private readonly Dictionary<Armor, Armor> _armorTransformations = [];
	private int _mapBackgroundColor;
	private bool _ignoreAutoNightVisionUserSetting;
	private string _inventoryShockIndicator = "";
	private string _mapShockIndicator = "";

	/// <summary>Creates a blank ruleset for a certain type of EnviroEffects.</summary>
	/// <param name="type">String defining the type.</param>
	public RuleEnviroEffects(string type)
	{
		_type = type;
		_mapBackgroundColor = 15;
		_ignoreAutoNightVisionUserSetting = false;
	}

	/// <summary>Loads the EnviroEffects from YAML.</summary>
	/// <param name="reader">YAML node.</param>
	/// <param name="mod">Mod for the EnviroEffects.</param>
	public void Load(YamlNodeReader reader, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent, mod);
		}

		reader.TryRead("environmentalConditions", ref _environmentalConditions);
		mod.LoadUnorderedNamesToNames(_type, _paletteTransformations, reader["paletteTransformations"]);
		mod.LoadUnorderedNamesToNames(_type, _armorTransformationsName, reader["armorTransformations"]);
		reader.TryRead("mapBackgroundColor", ref _mapBackgroundColor);
		reader.TryRead("ignoreAutoNightVisionUserSetting", ref _ignoreAutoNightVisionUserSetting);
		reader.TryRead("inventoryShockIndicator", ref _inventoryShockIndicator);
		reader.TryRead("mapShockIndicator", ref _mapShockIndicator);
	}

	/// <summary>Cross link with other rules.</summary>
	public void AfterLoad(Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		foreach (var pair in _armorTransformationsName)
		{
			var src = mod.GetArmor(pair.Key, true);
			var dest = mod.GetArmor(pair.Value, true);
			_armorTransformations[src] = dest;
		}

		//remove not needed data
		_armorTransformationsName.Clear();
	}

	/// <summary>Gets the EnviroEffects's type.</summary>
	public string Type => _type;

	/// <summary>Gets the palette transformations.</summary>
	public SortedDictionary<string, string> PaletteTransformations => _paletteTransformations;

	/// <summary>Gets the environmental condition for a given faction.</summary>
	/// <param name="faction">Faction code (STR_FRIENDLY, STR_HOSTILE or STR_NEUTRAL).</param>
	/// <returns>Environmental condition definition (a copy).</returns>
	public EnvironmentalCondition GetEnvironmetalCondition(string faction)
	{
		if (_environmentalConditions.Count != 0)
		{
			if (_environmentalConditions.TryGetValue(faction, out var i))
			{
				return i;
			}
		}
		return new EnvironmentalCondition();
	}

	/// <summary>Gets the transformed armor.</summary>
	/// <param name="sourceArmor">Existing/old armor type.</param>
	/// <returns>Transformed armor type (or null if there is no transformation).</returns>
	public Armor GetArmorTransformation(Armor sourceArmor)
	{
		if (_armorTransformations.Count != 0 && sourceArmor != null)
		{
			if (_armorTransformations.TryGetValue(sourceArmor, out var i))
			{
				// cannot switch into a bigger armor size!
				if (sourceArmor.Size >= i.Size)
				{
					return i;
				}
			}
		}
		return null;
	}

	/// <summary>Gets the battlescape map background color.</summary>
	public int MapBackgroundColor => _mapBackgroundColor;

	/// <summary>Gets the corresponding flag.</summary>
	public bool IgnoreAutoNightVisionUserSetting() => _ignoreAutoNightVisionUserSetting;

	/// <summary>Gets the inventory shock indicator sprite name.</summary>
	public string InventoryShockIndicator => _inventoryShockIndicator;

	/// <summary>Gets the map shock indicator sprite name.</summary>
	public string MapShockIndicator => _mapShockIndicator;
}
