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

namespace Acom.Placeholder;

/// <summary>
/// Deterministic, stateless pseudo random values (integer hash). Every pixel and record is computed from its
/// coordinates and the seed, so the output does not depend on the order in which files are written.
/// </summary>
public static class Noise
{
	/// <summary>Hashes a sequence of integers to 32 bits (xxHash-like mixing).</summary>
	public static uint Hash(uint seed, params ReadOnlySpan<int> values)
	{
		uint h = seed * 0x9E3779B1u + 0x7F4A7C15u;
		foreach (int v in values)
		{
			h ^= (uint)v + 0x85EBCA77u + (h << 6) + (h >> 2);
			h *= 0xC2B2AE3Du;
			h ^= h >> 15;
		}
		h ^= h >> 16;
		h *= 0x85EBCA6Bu;
		h ^= h >> 13;
		return h;
	}

	/// <summary>Hash of a string (stable across runs, unlike string.GetHashCode).</summary>
	public static int Of(string text)
	{
		ArgumentNullException.ThrowIfNull(text);
		uint h = 2166136261u;
		foreach (char c in text)
		{
			h ^= c;
			h *= 16777619u;
		}
		return (int)(h & 0x7FFFFFFF);
	}

	/// <summary>Value in [0, max).</summary>
	public static int Range(uint seed, int max, params ReadOnlySpan<int> values) => max <= 0 ? 0 : (int)(Hash(seed, values) % (uint)max);

	/// <summary>Value in [0, 1).</summary>
	public static double Unit(uint seed, params ReadOnlySpan<int> values) => (Hash(seed, values) >> 8) / (double)(1 << 24);
}
