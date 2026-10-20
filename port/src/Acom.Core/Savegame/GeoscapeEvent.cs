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
// Ported from: src/Savegame/GeoscapeEvent.h, src/Savegame/GeoscapeEvent.cpp
#nullable disable

using System.Diagnostics;

namespace Acom;

/// <summary>
/// Represents a custom Geoscape event, generated in the background and waiting to pop up.
/// </summary>
public class GeoscapeEvent
{
	private readonly RuleEvent _rule;
	private int _spawnCountdown;
	private bool _over;

	/// <summary>Creates a blank GeoscapeEvent.</summary>
	public GeoscapeEvent(RuleEvent rule)
	{
		_rule = rule;
		_spawnCountdown = 0;
		_over = false;
	}

	/// <summary>Loads the event from YAML.</summary>
	public void Load(YamlNodeReader reader)
	{
		reader.TryRead("spawnCountdown", ref _spawnCountdown);
		reader.TryRead("over", ref _over);
	}

	/// <summary>Saves the event to YAML.</summary>
	public void Save(YamlNodeWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.SetAsMap();
		writer.Write("name", _rule.Name);
		writer.Write("spawnCountdown", _spawnCountdown);
		if (_over)
			writer.Write("over", _over);
	}

	/// <summary>Gets the event's ruleset.</summary>
	public RuleEvent Rules => _rule;

	/// <summary>
	/// Minutes until the event pops up. The new time must be a multiple of 30 minutes, and more than 0.
	/// Setting this on a finished event has no effect.
	/// </summary>
	public int SpawnCountdown
	{
		get => _spawnCountdown;
		set
		{
			Debug.Assert(value != 0 && value % 30 == 0);
			if (_over)
			{
				return;
			}
			_spawnCountdown = value;
		}
	}

	/// <summary>Is this event over?</summary>
	public bool IsOver => _over;

	/// <summary>Handle event spawning schedule.</summary>
	public void Think()
	{
		// if finished, don't do anything
		if (_over)
		{
			return;
		}

		// are we there yet?
		if (_spawnCountdown > 30)
		{
			_spawnCountdown -= 30;
			return;
		}

		// ok, the time has come...
		_over = true;
	}
}
