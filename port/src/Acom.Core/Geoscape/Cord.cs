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
// Ported from: src/Geoscape/Cord.h
#nullable disable

namespace Acom;

/// <summary>Polar coordinates (longitude, latitude) in radians.</summary>
/// <remarks>Value type like the C++ struct.</remarks>
public struct CordPolar
{
	public double lon;
	public double lat;

	public CordPolar(double plon, double plat)
	{
		lon = plon;
		lat = plat;
	}

	public CordPolar()
	{
		lon = 0;
		lat = 0;
	}

	/// <summary>Converts a cartesian vector (any length) to polar coordinates.</summary>
	public CordPolar(Cord c)
	{
		double inv = 1 / c.Norm();
		lat = Math.Asin(c.y * inv);
		lon = Math.Atan2(c.x, c.z);
	}
}

/// <summary>Cartesian vector on/around the unit globe.</summary>
/// <remarks>
/// Value type like the C++ struct. The compound C++ operators (<c>*=</c>, <c>/=</c>, <c>+=</c>, <c>-=</c>) come from
/// the binary operators below; <c>operator==</c> is <see cref="AreSame(Cord)"/> because it compares with an epsilon.
/// </remarks>
public struct Cord
{
	public double x;
	public double y;
	public double z;

	public Cord(double px, double py, double pz)
	{
		x = px;
		y = py;
		z = pz;
	}

	public Cord()
	{
		x = 0.0;
		y = 0.0;
		z = 0.0;
	}

	/// <summary>Converts polar coordinates to a unit vector.</summary>
	public Cord(CordPolar pol)
	{
		x = Math.Sin(pol.lon) * Math.Cos(pol.lat);
		y = Math.Sin(pol.lat);
		z = Math.Cos(pol.lon) * Math.Cos(pol.lat);
	}

#pragma warning disable CA2225 // operators mirror the C++ struct; named alternates are not needed by the port
	public static Cord operator +(Cord c) => c;

	public static Cord operator -(Cord c) => new(-c.x, -c.y, -c.z);

	public static Cord operator *(Cord c, double d) => new(c.x * d, c.y * d, c.z * d);

	/// <summary>Divides by multiplying with the reciprocal, like the C++ <c>operator/=</c>.</summary>
	public static Cord operator /(Cord c, double d)
	{
		double re = 1.0 / d;
		return new(c.x * re, c.y * re, c.z * re);
	}

	public static Cord operator +(Cord a, Cord c) => new(a.x + c.x, a.y + c.y, a.z + c.z);

	public static Cord operator -(Cord a, Cord c) => new(a.x - c.x, a.y - c.y, a.z - c.z);
#pragma warning restore CA2225

	/// <summary>C++ <c>operator==</c>: all components equal within a relative epsilon (fmath.h AreSame).</summary>
	public readonly bool AreSame(Cord c)
	{
		return Target.AreSame(x, c.x) && Target.AreSame(y, c.y) && Target.AreSame(z, c.z);
	}

	/// <summary>Length of the vector.</summary>
	public readonly double Norm()
	{
		return Math.Sqrt(x * x + y * y + z * z);
	}
}
