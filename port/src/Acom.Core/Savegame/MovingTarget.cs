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
// Ported from: src/Savegame/MovingTarget.h, src/Savegame/MovingTarget.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Base class for moving targets on the globe
/// with a certain speed and destination.
/// </summary>
public abstract class MovingTarget : Target
{
	// Note: GLOBE_RADIUS is declared but never defined (or used) in the C++ code, so it is left out.

	protected Target _dest;
	protected double _speedLon;
	protected double _speedLat;
	protected double _speedRadian;
	protected double _meetPointLon;
	protected double _meetPointLat;
	protected int _speed;
	protected bool _meetCalculated;

	/// <summary>Initializes a moving target with blank coordinates.</summary>
	protected MovingTarget()
	{
		_dest = null;
		_speedLon = 0.0;
		_speedLat = 0.0;
		_speedRadian = 0.0;
		_meetPointLon = 0.0;
		_meetPointLat = 0.0;
		_speed = 0;
		_meetCalculated = false;
	}

	/// <summary>
	/// C++ destructor: makes sure to cleanup the target's destination followers.
	/// Like in a C++ destructor, the MovingTarget versions of setDestination/calculateSpeed run (no virtual dispatch).
	/// </summary>
	public override void Destroy()
	{
		SetDestinationImpl(null, calculateSpeedVirtual: false);
		base.Destroy();
	}

	/// <summary>Loads the moving target from a YAML file.</summary>
	public override void Load(YamlNodeReader reader)
	{
		base.Load(reader);
		reader.TryRead("speedLon", ref _speedLon);
		reader.TryRead("speedLat", ref _speedLat);
		reader.TryRead("speedRadian", ref _speedRadian);
		reader.TryRead("speed", ref _speed);
	}

	/// <summary>Saves the moving target to a YAML file.</summary>
	public override void Save(YamlNodeWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.SetAsMap();
		base.Save(writer);
		_dest?.SaveId(writer["dest"]);
		writer.Write("speedLon", SerializationHelper.SerializeDouble(_speedLon));
		writer.Write("speedLat", SerializationHelper.SerializeDouble(_speedLat));
		writer.Write("speedRadian", SerializationHelper.SerializeDouble(_speedRadian));
		writer.Write("speed", _speed);
	}

	/// <summary>The destination the moving target is heading to (the setter is virtual like C++ setDestination).</summary>
	public virtual Target Destination
	{
		get => _dest;
		set => SetDestinationImpl(value, calculateSpeedVirtual: true);
	}

	private void SetDestinationImpl(Target dest, bool calculateSpeedVirtual)
	{
		_meetCalculated = false;
		// Remove moving target from old destination's followers
		if (_dest != null)
		{
			var followers = _dest.Followers;
			for (int i = 0; i < followers.Count; ++i)
			{
				if (followers[i] == this)
				{
					followers.RemoveAt(i);
					break;
				}
			}
		}
		_dest = dest;
		// Add moving target to new destination's followers
		_dest?.Followers.Add(this);
		// Recalculate meeting point for any followers
		foreach (var mt in Followers)
		{
			mt.ResetMeetPoint();
		}
		if (calculateSpeedVirtual)
			CalculateSpeed();
		else
			CalculateSpeedImpl();
	}

	/// <summary>
	/// Speed of the moving target in knots. The setter converts it from standard knots
	/// (nautical miles per hour) into radians per 5 in-game seconds.
	/// </summary>
	public int Speed
	{
		get => _speed;
		set
		{
			_speed = value;
			_speedRadian = CalculateRadianSpeed(_speed);
			// Recalculate meeting point for any followers
			foreach (var mt in Followers)
			{
				mt.ResetMeetPoint();
			}
			CalculateSpeed();
		}
	}

	/// <summary>Radial speed of the moving target (in 1 / 5 sec).</summary>
	public double SpeedRadian => _speedRadian;

	/// <summary>
	/// Converts a speed in degrees to a speed in radians.
	/// Each nautical mile is 1/60th of a degree.
	/// Each hour contains 720 5-seconds.
	/// </summary>
	protected static double CalculateRadianSpeed(int speed)
	{
		return Nautical(speed) / 720.0;
	}

	/// <summary>fmath.h Nautical(): nautical miles to radians.</summary>
	private static double Nautical(double x) => x * (1 / 60.0) * (Math.PI / 180.0);

	/// <summary>Calculates the speed vector based on the great circle distance to destination and current raw speed.</summary>
	protected virtual void CalculateSpeed() => CalculateSpeedImpl();

	private void CalculateSpeedImpl()
	{
		CalculateMeetPoint();
		if (_dest != null)
		{
			double dLon, dLat, length;
			dLon = Math.Sin(_meetPointLon - _lon) * Math.Cos(_meetPointLat);
			dLat = Math.Cos(_lat) * Math.Sin(_meetPointLat) - Math.Sin(_lat) * Math.Cos(_meetPointLat) * Math.Cos(_meetPointLon - _lon);
			length = Math.Sqrt(dLon * dLon + dLat * dLat);
			_speedLat = dLat / length * _speedRadian;
			_speedLon = dLon / length * _speedRadian / Math.Cos(_lat + _speedLat);

			// Check for invalid speeds when a division by zero occurs due to near-zero values
			if (double.IsNaN(_speedLon) || double.IsNaN(_speedLat))
			{
				_speedLon = 0;
				_speedLat = 0;
			}
		}
		else
		{
			_speedLon = 0;
			_speedLat = 0;
		}
	}

	/// <summary>Checks if the moving target has reached its destination.</summary>
	public bool ReachedDestination()
	{
		if (_dest == null)
		{
			return false;
		}
		return AreSame(_dest.Longitude, _lon) && AreSame(_dest.Latitude, _lat);
	}

	/// <summary>Executes a movement cycle for the moving target.</summary>
	public void Move()
	{
		CalculateSpeed();
		if (_dest != null)
		{
			if (GetDistance(_meetPointLon, _meetPointLat) > _speedRadian)
			{
				Longitude = _lon + _speedLon;
				Latitude = _lat + _speedLat;
			}
			else
			{
				if (GetDistance(_dest) > _speedRadian)
				{
					Longitude = _meetPointLon;
					Latitude = _meetPointLat;
				}
				else
				{
					Longitude = _dest.Longitude;
					Latitude = _dest.Latitude;
				}
				ResetMeetPoint();
			}
		}
	}

	/// <summary>
	/// Calculate meeting point with the target.
	/// The actual interception calculation is disabled in C++ ("#if 0", most probably forever) and not ported.
	/// </summary>
	public void CalculateMeetPoint()
	{
		_meetCalculated = false;

		// Initialize
		if (_dest != null)
		{
			_meetPointLat = _dest.Latitude;
			_meetPointLon = _dest.Longitude;
		}
		else
		{
			_meetPointLat = _lat;
			_meetPointLon = _lon;
		}
	}

	/// <summary>Latitude of the meeting point (angle in rad).</summary>
	public double MeetLatitude => _meetPointLat;

	/// <summary>Longitude of the meeting point (angle in rad).</summary>
	public double MeetLongitude => _meetPointLon;

	/// <summary>Forces the meeting point to be recalculated in the event that the target has changed direction.</summary>
	public void ResetMeetPoint()
	{
		_meetCalculated = false;
	}

	/// <summary>Returns if the meeting point was calculated.</summary>
	public bool IsMeetCalculated => _meetCalculated;
}
