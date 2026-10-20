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
// Ported from: src/Savegame/Waypoint.h, src/Savegame/Waypoint.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents a fixed waypoint on the world.
/// </summary>
public class Waypoint : Target
{
	/// <summary>Initializes a waypoint.</summary>
	public Waypoint()
	{
	}

	/// <summary>Returns the waypoint's unique type used for savegame purposes.</summary>
	public override string Type => "STR_WAY_POINT";

	/// <summary>Returns the globe marker for the waypoint (marker sprite, -1 if none).</summary>
	public override int Marker => 6;
}
