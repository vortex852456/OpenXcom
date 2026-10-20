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
// Ported from: src/Mod/RuleBaseFacilityFunctions.h
#nullable disable

using System.Numerics;

namespace Acom;

/// <summary>
/// Fixed collection of base functionalites (C++ <c>std::bitset&lt;128&gt;</c>).
/// </summary>
/// <remarks>
/// A value type like the C++ bitset: getters return copies, and <c>a |= b</c> never changes a rule's set.
/// </remarks>
public struct RuleBaseFacilityFunctions : IEquatable<RuleBaseFacilityFunctions>
{
	/// <summary>Number of bits (C++ <c>size()</c>).</summary>
	public const int Size = 128;

	private ulong _lo, _hi;

	/// <summary>Bits 0-63 set from a value (C++ <c>bitset(unsigned long long)</c>).</summary>
	public RuleBaseFacilityFunctions(ulong value)
	{
		_lo = value;
		_hi = 0;
	}

	/// <summary>Bit at <paramref name="pos"/> (C++ <c>operator[]</c>).</summary>
	public bool this[int pos]
	{
		readonly get => Test(pos);
		set => Set(pos, value);
	}

	/// <summary>Checks one bit; throws for positions outside the set like <c>std::bitset::test</c>.</summary>
	public readonly bool Test(int pos)
	{
		CheckPos(pos);
		return pos < 64 ? (_lo >> pos & 1) != 0 : (_hi >> (pos - 64) & 1) != 0;
	}

	/// <summary>Sets all bits.</summary>
	public void Set()
	{
		_lo = ulong.MaxValue;
		_hi = ulong.MaxValue;
	}

	/// <summary>Sets or clears one bit.</summary>
	public void Set(int pos, bool value = true)
	{
		CheckPos(pos);
		if (pos < 64)
			_lo = value ? _lo | 1UL << pos : _lo & ~(1UL << pos);
		else
			_hi = value ? _hi | 1UL << (pos - 64) : _hi & ~(1UL << (pos - 64));
	}

	/// <summary>Clears all bits.</summary>
	public void Reset()
	{
		_lo = 0;
		_hi = 0;
	}

	/// <summary>Clears one bit.</summary>
	public void Reset(int pos) => Set(pos, false);

	/// <summary>Is any bit set?</summary>
	public readonly bool Any() => (_lo | _hi) != 0;

	/// <summary>Is no bit set?</summary>
	public readonly bool None() => (_lo | _hi) == 0;

	/// <summary>Are all bits set?</summary>
	public readonly bool All() => (_lo & _hi) == ulong.MaxValue;

	/// <summary>Number of set bits.</summary>
	public readonly int Count() => BitOperations.PopCount(_lo) + BitOperations.PopCount(_hi);

	public static RuleBaseFacilityFunctions operator &(RuleBaseFacilityFunctions a, RuleBaseFacilityFunctions b) => new() { _lo = a._lo & b._lo, _hi = a._hi & b._hi };
	public static RuleBaseFacilityFunctions operator |(RuleBaseFacilityFunctions a, RuleBaseFacilityFunctions b) => new() { _lo = a._lo | b._lo, _hi = a._hi | b._hi };
	public static RuleBaseFacilityFunctions operator ^(RuleBaseFacilityFunctions a, RuleBaseFacilityFunctions b) => new() { _lo = a._lo ^ b._lo, _hi = a._hi ^ b._hi };
	public static RuleBaseFacilityFunctions operator ~(RuleBaseFacilityFunctions a) => new() { _lo = ~a._lo, _hi = ~a._hi };
	public static bool operator ==(RuleBaseFacilityFunctions a, RuleBaseFacilityFunctions b) => a._lo == b._lo && a._hi == b._hi;
	public static bool operator !=(RuleBaseFacilityFunctions a, RuleBaseFacilityFunctions b) => !(a == b);

	public static RuleBaseFacilityFunctions BitwiseAnd(RuleBaseFacilityFunctions a, RuleBaseFacilityFunctions b) => a & b;
	public static RuleBaseFacilityFunctions BitwiseOr(RuleBaseFacilityFunctions a, RuleBaseFacilityFunctions b) => a | b;
	public static RuleBaseFacilityFunctions Xor(RuleBaseFacilityFunctions a, RuleBaseFacilityFunctions b) => a ^ b;
	public static RuleBaseFacilityFunctions OnesComplement(RuleBaseFacilityFunctions a) => ~a;

	public readonly bool Equals(RuleBaseFacilityFunctions other) => this == other;
	public override readonly bool Equals(object obj) => obj is RuleBaseFacilityFunctions f && this == f;
	public override readonly int GetHashCode() => HashCode.Combine(_lo, _hi);

	/// <summary>Like <c>std::bitset::to_string()</c>: 128 characters '0'/'1', highest bit first.</summary>
	public override readonly string ToString()
	{
		var chars = new char[Size];
		for (int i = 0; i < Size; ++i)
			chars[Size - 1 - i] = Test(i) ? '1' : '0';
		return new string(chars);
	}

	private static void CheckPos(int pos)
	{
		if ((uint)pos >= Size)
			throw new ArgumentOutOfRangeException(nameof(pos), pos, "bitset position out of range");
	}

/// <summary>
/// Subset area of base: <c>AreaSubset&lt;Base, Sint8&gt;</c> from src/Engine/GraphSubset.h (C++ alias <c>BaseAreaSubset</c>).
/// </summary>
/// <remarks>Fields are <see cref="sbyte"/> like the C++ <c>Sint8</c> data type; arithmetic wraps the same way.</remarks>
public struct BaseAreaSubset : IEquatable<BaseAreaSubset>
{
	// define part of surface
	public sbyte beg_x, end_x;
	public sbyte beg_y, end_y;

	public BaseAreaSubset(int max_x, int max_y)
	{
		beg_x = 0;
		end_x = (sbyte)max_x;
		beg_y = 0;
		end_y = (sbyte)max_y;
	}

	public BaseAreaSubset((int first, int second) range_x, (int first, int second) range_y)
	{
		beg_x = (sbyte)range_x.first;
		end_x = (sbyte)range_x.second;
		beg_y = (sbyte)range_y.first;
		end_y = (sbyte)range_y.second;
	}

	public readonly BaseAreaSubset Offset(int x, int y)
	{
		BaseAreaSubset ret = this;
		ret.beg_x = (sbyte)(ret.beg_x + x);
		ret.end_x = (sbyte)(ret.end_x + x);
		ret.beg_y = (sbyte)(ret.beg_y + y);
		ret.end_y = (sbyte)(ret.end_y + y);
		return ret;
	}

	public readonly int SizeX() => end_x - beg_x;

	public readonly int SizeY() => end_y - beg_y;

	/// <summary>Check if area is empty (C++ explicit operator bool): true if it has non-zero area.</summary>
	public readonly bool ToBoolean() => SizeX() != 0 && SizeY() != 0;

	public static explicit operator bool(BaseAreaSubset area) => area.ToBoolean();

	/// <summary>Check if two areas are same.</summary>
	public static bool operator ==(BaseAreaSubset a, BaseAreaSubset b) =>
		a.beg_x == b.beg_x &&
		a.end_x == b.end_x &&
		a.beg_y == b.beg_y &&
		a.end_y == b.end_y;

	/// <summary>Check if two areas are not same.</summary>
	public static bool operator !=(BaseAreaSubset a, BaseAreaSubset b) => !(a == b);

	public readonly bool Equals(BaseAreaSubset other) => this == other;

	public override readonly bool Equals(object? obj) => obj is BaseAreaSubset other && this == other;

	public override readonly int GetHashCode() => HashCode.Combine(beg_x, end_x, beg_y, end_y);

	private static void IntersectionRange(ref sbyte begin_a, ref sbyte end_a, sbyte begin_b, sbyte end_b)
	{
		if (begin_a >= end_b || begin_b >= end_a)
		{
			// intersection is empty
			end_a = begin_a;
		}
		else
		{
			begin_a = Math.Max(begin_a, begin_b);
			end_a = Math.Min(end_a, end_b);
		}
	}

	private static void BoundBoxRange(ref sbyte begin_a, ref sbyte end_a, sbyte begin_b, sbyte end_b)
	{
		if (begin_b < end_b && begin_a < end_a)
		{
			begin_a = Math.Min(begin_a, begin_b);
			end_a = Math.Max(end_a, end_b);
		}
		else if (begin_b < end_b)
		{
			begin_a = begin_b;
			end_a = end_b;
		}
		else
		{
			// keep first range same
		}
	}

	public static BaseAreaSubset Intersection(BaseAreaSubset a, BaseAreaSubset b)
	{
		BaseAreaSubset ret = a;
		IntersectionRange(ref ret.beg_x, ref ret.end_x, b.beg_x, b.end_x);
		IntersectionRange(ref ret.beg_y, ref ret.end_y, b.beg_y, b.end_y);
		return ret;
	}

	public static BaseAreaSubset Intersection(BaseAreaSubset a, BaseAreaSubset b, BaseAreaSubset c)
	{
		BaseAreaSubset ret = Intersection(a, b);
		IntersectionRange(ref ret.beg_x, ref ret.end_x, c.beg_x, c.end_x);
		IntersectionRange(ref ret.beg_y, ref ret.end_y, c.beg_y, c.end_y);
		return ret;
	}

	public static BaseAreaSubset Intersection(BaseAreaSubset a, BaseAreaSubset b, BaseAreaSubset c, BaseAreaSubset d)
	{
		BaseAreaSubset ret = Intersection(a, b, c);
		IntersectionRange(ref ret.beg_x, ref ret.end_x, d.beg_x, d.end_x);
		IntersectionRange(ref ret.beg_y, ref ret.end_y, d.beg_y, d.end_y);
		return ret;
	}

	public static BaseAreaSubset BoundBox(BaseAreaSubset a, BaseAreaSubset b)
	{
		BaseAreaSubset ret = a;
		BoundBoxRange(ref ret.beg_x, ref ret.end_x, b.beg_x, b.end_x);
		BoundBoxRange(ref ret.beg_y, ref ret.end_y, b.beg_y, b.end_y);
		return ret;
	}

	public static BaseAreaSubset BoundBox(BaseAreaSubset a, BaseAreaSubset b, BaseAreaSubset c)
	{
		BaseAreaSubset ret = BoundBox(a, b);
		BoundBoxRange(ref ret.beg_x, ref ret.end_x, c.beg_x, c.end_x);
		BoundBoxRange(ref ret.beg_y, ref ret.end_y, c.beg_y, c.end_y);
		return ret;
	}

	public static BaseAreaSubset FromPoint(int x, int y) => new BaseAreaSubset(1, 1).Offset(x, y);
}
