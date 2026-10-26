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
// Ported from: src/Mod/StatString.h, src/Mod/StatString.cpp
#nullable disable

namespace Acom;

/// <summary>
/// For adding statStrings to the game (automatic soldier renaming, from XcomUtil's NameStats).
/// See http://ufopaedia.org/index.php?title=Statstrings
/// A string is added when all its stat ranges are met; a string longer than one character ends the search.
/// </summary>
public class StatString
{
	private static readonly string[] s_conditionNames = ["psiStrength", "psiSkill", "bravery", "strength", "firing", "reactions", "stamina", "tu", "health", "throwing", "melee", "psiTraining", "manaPool"];

	private string _stringToBeAddedIfAllConditionsAreMet = "";
	private readonly List<StatStringCondition> _conditions = [];

	/// <summary>Creates a blank StatString.</summary>
	public StatString()
	{
	}

	/// <summary>Loads the StatString from YAML.</summary>
	public void Load(YamlNodeReader reader)
	{
		reader.TryRead("string", ref _stringToBeAddedIfAllConditionsAreMet);
		foreach (var name in s_conditionNames)
		{
			var conditionReader = reader[name];
			if (conditionReader)
			{
				_conditions.Add(GetCondition(name, conditionReader));
			}
		}
	}

	/// <summary>Generates a condition from YAML.</summary>
	private static StatStringCondition GetCondition(string conditionName, YamlNodeReader conditionReader)
	{
		// These are the defaults from xcomutil
		int minValue = 0, maxValue = 255;
		if (conditionReader[0] && !conditionReader[0].HasNullVal)
			conditionReader[0].TryReadVal(ref minValue);
		if (conditionReader[1] && !conditionReader[1].HasNullVal)
			conditionReader[1].TryReadVal(ref maxValue);
		return new StatStringCondition(conditionName, minValue, maxValue);
	}

	/// <summary>Gets the conditions associated with this StatString.</summary>
	public List<StatStringCondition> Conditions { get => _conditions; }

	/// <summary>Gets the string to add to a name for this StatString.</summary>
#pragma warning disable CA1720 // C++ getString()
	public string String { get => _stringToBeAddedIfAllConditionsAreMet; }
#pragma warning restore CA1720

	/// <summary>Calculates the list of StatStrings that apply to certain unit stats.</summary>
	/// <param name="currentStats">Unit stats.</param>
	/// <param name="statStrings">List of statString rules.</param>
	/// <param name="psiStrengthEval">Are psi stats available?</param>
	/// <param name="inTraining">Is the soldier in psi training?</param>
	/// <returns>Resulting string of all valid StatStrings.</returns>
	public static string CalcStatString(ref UnitStats currentStats, List<StatString> statStrings, bool psiStrengthEval, bool inTraining)
	{
		ArgumentNullException.ThrowIfNull(statStrings);
		string result = "";
		var currentStatsMap = GetCurrentStats(ref currentStats);
		if (inTraining)
		{
			currentStatsMap["psiTraining"] = 1;
		}
		foreach (var statStringDef in statStrings)
		{
			bool conditionsMet = true;
			foreach (var statStringCondition in statStringDef.Conditions)
			{
				if (!conditionsMet) break; // loop finished
				if (currentStatsMap.TryGetValue(statStringCondition.ConditionName, out int value))
				{
					conditionsMet = conditionsMet && statStringCondition.IsMet(value, currentStats.psiSkill > 0 || psiStrengthEval);
				}
				else
				{
					// if the name is not in the map we've searched for a stat that doesn't exist.
					// this means psi training. if there's no "psiTraining" stat in the statsMap,
					// this soldier isn't in training, so we won't append his name with the psiTraining tag.
					conditionsMet = false;
				}
			}
			if (conditionsMet)
			{
				string wstring = statStringDef.String;
				result += wstring;
				if (Unicode.CodePointLength(wstring) > 1)
				{
					break;
				}
			}
		}
		return result;
	}

	/// <summary>Gets a map associating stat names to unit stats.</summary>
	public static SortedDictionary<string, int> GetCurrentStats(ref UnitStats currentStats)
	{
		return new SortedDictionary<string, int>(StringComparer.Ordinal)
		{
			["psiStrength"] = currentStats.psiStrength,
			["psiSkill"] = currentStats.psiSkill,
			["bravery"] = currentStats.bravery,
			["strength"] = currentStats.strength,
			["firing"] = currentStats.firing,
			["reactions"] = currentStats.reactions,
			["stamina"] = currentStats.stamina,
			["tu"] = currentStats.tu,
			["health"] = currentStats.health,
			["throwing"] = currentStats.throwing,
			["melee"] = currentStats.melee,
			["manaPool"] = currentStats.mana,
		};
	}
}
