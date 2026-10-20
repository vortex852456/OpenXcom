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
// Ported from: src/Engine/GraphSubset.h

global using GraphSubset = Acom.AreaSubset<Acom.GraphTag, int>;

using System.Numerics;

namespace Acom;

/// <summary>Tag type of <see cref="GraphSubset"/> (C++ <c>void</c>): a subset of a graphic surface.</summary>
public sealed class GraphTag
{
	private GraphTag()
	{
	}
}

/// <summary>
/// Generic class that represents some subset of 2d space: the half-open ranges
/// [beg_x, end_x) x [beg_y, end_y). <typeparamref name="TTag"/> only distinguishes the kinds of
/// subsets (C++: GraphSubset = AreaSubset&lt;void, int&gt;, MapSubset, BaseAreaSubset).
/// </summary>
/// <remarks>Arithmetic is done in <c>int</c> and stored back with truncation, like the C++ integer promotions.</remarks>
#pragma warning disable CA1000 // static members of the generic type mirror the C++ static template members
public struct AreaSubset<TTag, T> : IEquatable<AreaSubset<TTag, T>> where T : struct, IBinaryInteger<T>
{
	//define part of surface
	public T beg_x, end_x;
	public T beg_y, end_y;

	/// <summary>Subset [0, max_x) x [0, max_y).</summary>
	public AreaSubset(int max_x, int max_y)
	{
		beg_x = T.Zero;
		end_x = T.CreateTruncating(max_x);
		beg_y = T.Zero;
		end_y = T.CreateTruncating(max_y);
	}

	/// <summary>Subset [range_x.first, range_x.second) x [range_y.first, range_y.second).</summary>
	public AreaSubset((int first, int second) range_x, (int first, int second) range_y)
	{
		beg_x = T.CreateTruncating(range_x.first);
		end_x = T.CreateTruncating(range_x.second);
		beg_y = T.CreateTruncating(range_y.first);
		end_y = T.CreateTruncating(range_y.second);
	}

	private static T Add(T a, int b) => T.CreateTruncating(int.CreateTruncating(a) + b);

	/// <summary>Returns a copy moved by (x, y).</summary>
	public readonly AreaSubset<TTag, T> Offset(int x, int y)
	{
		AreaSubset<TTag, T> ret = this;
		ret.beg_x = Add(ret.beg_x, x);
		ret.end_x = Add(ret.end_x, x);
		ret.beg_y = Add(ret.beg_y, y);
		ret.end_y = Add(ret.end_y, y);
		return ret;
	}

	public readonly int SizeX() => int.CreateTruncating(end_x) - int.CreateTruncating(beg_x);

	public readonly int SizeY() => int.CreateTruncating(end_y) - int.CreateTruncating(beg_y);

	/// <summary>Check if area is empty: true if it has a non-zero area (C++ <c>operator bool</c>).</summary>
	public readonly bool ToBoolean() => SizeX() != 0 && SizeY() != 0;

	/// <summary>True if the area is non-empty.</summary>
	public static explicit operator bool(AreaSubset<TTag, T> area) => area.ToBoolean();

	/// <summary>Check if two areas are same.</summary>
	public readonly bool Equals(AreaSubset<TTag, T> other)
	{
		return beg_x == other.beg_x && end_x == other.end_x && beg_y == other.beg_y && end_y == other.end_y;
	}

	public override readonly bool Equals(object? obj) => obj is AreaSubset<TTag, T> other && Equals(other);

	public override readonly int GetHashCode() => HashCode.Combine(beg_x, end_x, beg_y, end_y);

	public static bool operator ==(AreaSubset<TTag, T> left, AreaSubset<TTag, T> right) => left.Equals(right);

	public static bool operator !=(AreaSubset<TTag, T> left, AreaSubset<TTag, T> right) => !left.Equals(right);

	public override readonly string ToString() => $"[{beg_x}, {end_x}) x [{beg_y}, {end_y})";

	/// <summary>Intersects range a with range b (an empty result keeps begin_a).</summary>
	public static void IntersectionRange(ref T begin_a, ref T end_a, T begin_b, T end_b)
	{
		if (begin_a >= end_b || begin_b >= end_a)
		{
			//intersection is empty
			end_a = begin_a;
		}
		else
		{
			begin_a = T.Max(begin_a, begin_b);
			end_a = T.Min(end_a, end_b);
		}
	}

	/// <summary>Extends range a to cover range b; empty ranges are ignored.</summary>
	public static void BoundBoxRange(ref T begin_a, ref T end_a, T begin_b, T end_b)
	{
		if (begin_b < end_b && begin_a < end_a)
		{
			begin_a = T.Min(begin_a, begin_b);
			end_a = T.Max(end_a, end_b);
		}
		else if (begin_b < end_b)
		{
			begin_a = begin_b;
			end_a = end_b;
		}
		else
		{
			//keep first range same
		}
	}

	public static AreaSubset<TTag, T> Intersection(AreaSubset<TTag, T> a, AreaSubset<TTag, T> b)
	{
		AreaSubset<TTag, T> ret = a;
		IntersectionRange(ref ret.beg_x, ref ret.end_x, b.beg_x, b.end_x);
		IntersectionRange(ref ret.beg_y, ref ret.end_y, b.beg_y, b.end_y);
		return ret;
	}

	public static AreaSubset<TTag, T> Intersection(AreaSubset<TTag, T> a, AreaSubset<TTag, T> b, AreaSubset<TTag, T> c)
	{
		AreaSubset<TTag, T> ret = Intersection(a, b);
		IntersectionRange(ref ret.beg_x, ref ret.end_x, c.beg_x, c.end_x);
		IntersectionRange(ref ret.beg_y, ref ret.end_y, c.beg_y, c.end_y);
		return ret;
	}

	public static AreaSubset<TTag, T> Intersection(AreaSubset<TTag, T> a, AreaSubset<TTag, T> b, AreaSubset<TTag, T> c, AreaSubset<TTag, T> d)
	{
		AreaSubset<TTag, T> ret = Intersection(a, b, c);
		IntersectionRange(ref ret.beg_x, ref ret.end_x, d.beg_x, d.end_x);
		IntersectionRange(ref ret.beg_y, ref ret.end_y, d.beg_y, d.end_y);
		return ret;
	}

	public static AreaSubset<TTag, T> BoundBox(AreaSubset<TTag, T> a, AreaSubset<TTag, T> b)
	{
		AreaSubset<TTag, T> ret = a;
		BoundBoxRange(ref ret.beg_x, ref ret.end_x, b.beg_x, b.end_x);
		BoundBoxRange(ref ret.beg_y, ref ret.end_y, b.beg_y, b.end_y);
		return ret;
	}

	public static AreaSubset<TTag, T> BoundBox(AreaSubset<TTag, T> a, AreaSubset<TTag, T> b, AreaSubset<TTag, T> c)
	{
		AreaSubset<TTag, T> ret = BoundBox(a, b);
		BoundBoxRange(ref ret.beg_x, ref ret.end_x, c.beg_x, c.end_x);
		BoundBoxRange(ref ret.beg_y, ref ret.end_y, c.beg_y, c.end_y);
		return ret;
	}

	/// <summary>The 1x1 subset at (x, y).</summary>
	public static AreaSubset<TTag, T> FromPoint(int x, int y) => new AreaSubset<TTag, T>(1, 1).Offset(x, y);
}
#pragma warning restore CA1000
