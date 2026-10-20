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
// Ported from: src/Savegame/SoldierDeath.h, src/Savegame/SoldierDeath.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Stores info about a soldier's death.
/// </summary>
public class SoldierDeath
{
	private readonly GameTime _time;
	private BattleUnitKills _cause;

	public SoldierDeath()
	{
		_time = new GameTime(0, 0, 0, 0, 0, 0, 0);
		_cause = null;
	}

	/// <summary>Initializes a death event (the time is copied, the cause is owned by the death).</summary>
	public SoldierDeath(GameTime time, BattleUnitKills cause)
	{
		// C++ holds GameTime by value
		_time = new GameTime(time.Weekday, time.Day, time.Month, time.Year, time.Hour, time.Minute, time.Second);
		_cause = cause;
	}

	/// <summary>Loads the death from a YAML file.</summary>
	public void Load(YamlNodeReader reader)
	{
		_time.Load(reader["time"]);
		var kill = reader["cause"];
		if (kill)
			_cause = new BattleUnitKills(kill);
	}

	/// <summary>Saves the death to a YAML file.</summary>
	public void Save(YamlNodeWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.SetAsMap();
		_time.Save(writer["time"]);
		if (_cause != null)
			_cause.Save(writer["cause"]);
	}

	/// <summary>Returns the time of death of this soldier.</summary>
	public GameTime Time => _time;

	/// <summary>Returns the cause of death of this soldier (null if missing in action).</summary>
	public BattleUnitKills Cause => _cause;
}
