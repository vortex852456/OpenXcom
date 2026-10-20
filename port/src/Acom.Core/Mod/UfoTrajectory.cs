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
// Ported from: src/Mod/UfoTrajectory.h, src/Mod/UfoTrajectory.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Information for points on a UFO trajectory.
/// </summary>
/// <remarks>The C++ fields are <c>size_t</c>; negative YAML values wrap like there (see <see cref="UfoTrajectory.ApplySpeedPercentage"/>).</remarks>
public class TrajectoryWaypoint
{
	/// <summary>The mission zone.</summary>
	public int zone;
	/// <summary>The altitude to reach.</summary>
	public int altitude;
	/// <summary>The speed percentage ([0..100]).</summary>
	public int speed;

	/// <summary>YAML: flow sequence <c>[zone, altitude, speed]</c> (C++ read overload).</summary>
	public static bool YamlRead(YamlNodeReader reader, ref TrajectoryWaypoint val)
	{
		val ??= new TrajectoryWaypoint();
		val.zone = reader[0].ReadVal<int>();
		val.altitude = reader[1].ReadVal<int>();
		val.speed = reader[2].ReadVal<int>();
		return true;
	}
}

/// <summary>
/// Holds information about a specific trajectory.
/// Ufo trajectories are a sequence of mission zones, altitudes and speed percentages.
/// </summary>
public class UfoTrajectory
{
	public const string RETALIATION_ASSAULT_RUN = "__RETALIATION_ASSAULT_RUN";

	private readonly string _id;
	private ulong _groundTimer;
	private List<TrajectoryWaypoint> _waypoints = [];

	public UfoTrajectory(string id)
	{
		_id = id;
		_groundTimer = 5;
	}

	/// <summary>Gets the trajectory's ID.</summary>
	public string ID => _id;

	/// <summary>
	/// Overwrites trajectory data with the data stored in <paramref name="reader"/>.
	/// Only the fields contained in the node will be overwritten.
	/// </summary>
	public void Load(YamlNodeReader reader)
	{
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent);
		}

		reader.TryRead("groundTimer", ref _groundTimer);
		reader.TryRead("waypoints", ref _waypoints);
	}

	/// <summary>Gets the number of waypoints in this trajectory.</summary>
	public int WaypointCount => _waypoints.Count;

	/// <summary>Gets the zone index at a waypoint.</summary>
	public int GetZone(int wp) => _waypoints[wp].zone;

	/// <summary>Gets the altitude at a waypoint.</summary>
	public string GetAltitude(int wp) => Ufo.ALTITUDE_STRING[_waypoints[wp].altitude];

	/// <summary>Applies the speed percentage at a waypoint to the base speed.</summary>
	/// <returns>The new speed after applying the percentage.</returns>
	public int ApplySpeedPercentage(int wp, int baseSpeed) => unchecked((int)((ulong)baseSpeed * (ulong)_waypoints[wp].speed / 100)); // size_t math like C++

	/// <summary>Gets the number of seconds UFOs should spend on the ground.</summary>
	public int GroundTimer() => unchecked((int)_groundTimer);
}
