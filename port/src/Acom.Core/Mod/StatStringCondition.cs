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
// Ported from: src/Mod/StatStringCondition.h, src/Mod/StatStringCondition.cpp
#nullable disable

namespace Acom;

/// <summary>One stat range of a <see cref="StatString"/>.</summary>
public class StatStringCondition
{
	private readonly string _conditionName;
	private readonly int _minVal;
	private readonly int _maxVal;

	/// <summary>Creates a StatStringCondition.</summary>
	public StatStringCondition(string conditionName, int minVal, int maxVal)
	{
		_conditionName = conditionName;
		_minVal = minVal;
		_maxVal = maxVal;
	}

	/// <summary>Gets the name of the associated stat.</summary>
	public string ConditionName { get => _conditionName; }

	/// <summary>Gets the minimum value for the condition (default is 0).</summary>
	public int MinVal { get => _minVal; }

	/// <summary>Gets the maximum value for the condition (default is 255).</summary>
	public int MaxVal { get => _maxVal; }

	/// <summary>Checks if this condition is valid for the current stat.</summary>
	/// <param name="stat">The current soldier stat.</param>
	/// <param name="psi">Can we show psi stats?</param>
	public bool IsMet(int stat, bool psi)
	{
		if (_conditionName == "psiTraining")
		{
			return true;
		}
		bool conditionMet = (stat >= _minVal && stat <= _maxVal);
		if (_conditionName == "psiStrength" || _conditionName == "psiSkill")
		{
			conditionMet = conditionMet && psi;
		}
		return conditionMet;
	}
}
