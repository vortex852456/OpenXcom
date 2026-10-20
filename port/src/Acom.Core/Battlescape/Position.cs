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
// Ported from: src/Battlescape/Position.h, src/Battlescape/Position.cpp

namespace Acom;

/// <summary>
/// Easy handling of X-Y-Z coordinates.
/// </summary>
public struct Position : IEquatable<Position>
{
	public const int TileXY = 16;
	public const int TileZ = 24;

	public short x, y, z;

	/// <summary>X Y Z position constructor (values are truncated to 16 bit like the C++ Sint16 fields).</summary>
	public Position(int x_, int y_, int z_)
	{
		x = (short)x_;
		y = (short)y_;
		z = (short)z_;
	}

	public static Position operator +(Position a, Position b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
	public static Position operator -(Position a, Position b) => new(a.x - b.x, a.y - b.y, a.z - b.z);
	public static Position operator *(Position a, Position b) => new(a.x * b.x, a.y * b.y, a.z * b.z);
	public static Position operator *(Position a, int v) => new(a.x * v, a.y * v, a.z * v);
	public static Position operator /(Position a, Position b) => new(a.x / b.x, a.y / b.y, a.z / b.z);
	public static Position operator /(Position a, int v) => new(a.x / v, a.y / v, a.z / v);
	public static bool operator ==(Position a, Position b) => a.x == b.x && a.y == b.y && a.z == b.z;
	public static bool operator !=(Position a, Position b) => a.x != b.x || a.y != b.y || a.z != b.z;

	public readonly bool Equals(Position other) => this == other;
	public override readonly bool Equals(object? obj) => obj is Position p && this == p;
	public override readonly int GetHashCode() => HashCode.Combine(x, y, z);

	/// <summary>Like the C++ <c>operator&lt;&lt;</c>: "(x,y,z)".</summary>
	public override readonly string ToString() => "(" + x + "," + y + "," + z + ")";

	public readonly Position ToVoxel() => new(x * TileXY, y * TileXY, z * TileZ);

	public readonly Position ToTile() => new(x / TileXY, y / TileXY, z / TileZ);

	public readonly Position ClipVoxel() => new(x % TileXY, y % TileXY, z % TileZ);

	public static float Distance(Position pos1, Position pos2) => (float)Math.Sqrt(DistanceSq(pos1, pos2)); // std::sqrt(int) computes in double

	/// <summary>Calculates the distance squared between 2 points in 3d. No sqrt(), not floating point math, and sometimes it's all you need.</summary>
	public static int DistanceSq(Position pos1, Position pos2)
	{
		int x = pos1.x - pos2.x;
		int y = pos1.y - pos2.y;
		int z = pos1.z - pos2.z;
		return x * x + y * y + z * z;
	}

	/// <summary>Calculates the distance between 2 points in 2d. Rounded up to first INT.</summary>
	public static int Distance2d(Position pos1, Position pos2) => (int)Math.Ceiling(Math.Sqrt(Distance2dSq(pos1, pos2)));

	/// <summary>Calculates the distance squared between 2 points in 2d. No sqrt(), not floating point math, and sometimes it's all you need.</summary>
	public static int Distance2dSq(Position pos1, Position pos2)
	{
		int x = pos1.x - pos2.x;
		int y = pos1.y - pos2.y;
		return x * x + y * y;
	}

	/// <summary>YAML: flow sequence [x, y, z] (C++ read/write overloads).</summary>
	public static bool YamlRead(YamlNodeReader reader, ref Position val)
	{
		val.x = reader[0].ReadVal<short>();
		val.y = reader[1].ReadVal<short>();
		val.z = reader[2].ReadVal<short>();
		return true;
	}

	public static void YamlWrite(YamlNodeWriter writer, Position val)
	{
		writer.SetAsSeq();
		writer.SetFlowStyle();
		writer.Write(val.x);
		writer.Write(val.y);
		writer.Write(val.z);
	}
}

/// <summary>Helper class storing last two position from Projectile.</summary>
public struct LastPositions
{
	public Position last;
	public Position before;

	public LastPositions(Position l)
	{
		last = l;
		before = l;
	}

	public LastPositions(Position l, Position b)
	{
		last = l;
		before = b;
	}
}
