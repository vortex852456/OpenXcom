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
// Ported from: src/Engine/RNG.h, src/Engine/RNG.cpp

namespace Acom;

/// <summary>
/// Random Number Generator used throughout the game
/// for all your randomness needs. Uses a 64-bit xorshift
/// pseudorandom number generator.
/// </summary>
/// <remarks>
/// The sequence is bit-identical to the C++ implementation, so seeds stored in
/// savegames reproduce the same game. Verified by golden tests.
/// </remarks>
public static class Rng
{
	/// <summary>
	/// A single generator state. Reference type, because the C++ code hands out
	/// references to the global state and mutates it through them.
	/// </summary>
	public sealed class RandomState
	{
		private ulong _seedState;

		/// <summary>Initializes the seed by time and a per-process random value.</summary>
		public RandomState() : this(CreateTimeSeed())
		{
		}

		/// <summary>Constructor from predefined seed.</summary>
		public RandomState(ulong seed)
		{
			_seedState = seed;
		}

		/// <summary>Gets the current seed.</summary>
		public ulong Seed => _seedState;

		/// <summary>Gets the next random number.</summary>
		public ulong Next() => NextImpl(ref _seedState);

		/// <summary>Generates a random integer number, inclusive.</summary>
		public int Generate(int min, int max)
		{
			// Mirrors C++ integer promotion: int operands are sign-extended to uint64_t.
			return unchecked((int)(Next() % (ulong)(long)(max - min + 1) + (ulong)(long)min));
		}

		/// <summary>Generates a percentage chance.</summary>
		public bool Percent(int value) => Generate(0, 99) < value;

		/// <summary>
		/// Gets a new random sub-sequence that depends on the current seed,
		/// but each call creates a different sequence.
		/// </summary>
		public RandomState SubSequence()
		{
			// Random value to have a different new seed but still deterministic values when the game is run again.
			return new RandomState(Next() ^ 0x055e3ac3461280cfUL);
		}

		private static ulong CreateTimeSeed()
		{
			// C++ uses time(0) ^ ~address; an address is not available here, so a random value takes its place.
			return (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds() ^ ~(ulong)Random.Shared.NextInt64();
		}
	}

	/* NextImpl written in 2014 by Sebastiano Vigna (vigna@acm.org)
	To the extent possible under law, the author has dedicated all copyright
	and related and neighboring rights to this software to the public domain
	worldwide. This software is distributed without any warranty.
	See <http://creativecommons.org/publicdomain/zero/1.0/>. */
	private static ulong NextImpl(ref ulong state)
	{
		state ^= state >> 12; // a
		state ^= state << 25; // b
		state ^= state >> 27; // c
		return unchecked(state * 2685821657736338717UL);
	}

	/// <summary>State for game random number generator.</summary>
	private static RandomState s_state = new();

	/// <summary>Separate state for auxiliary random numbers that do not affect game state.</summary>
	private static readonly RandomState s_seedless = new();

	/// <summary>Gets or sets the seed in use by the game generator.</summary>
	public static ulong Seed
	{
		get => s_state.Seed;
		set => s_state = new RandomState(value);
	}

	/// <summary>Gets the game generator state.</summary>
	public static RandomState GlobalRandomState => s_state;

	/// <summary>Generates a random integer number within a certain range, inclusive.</summary>
	public static int Generate(int min, int max) => s_state.Generate(min, max);

	/// <summary>Generates a random decimal number within a certain range.</summary>
	public static double Generate(double min, double max)
	{
		double num = s_state.Next();
		return num / ((double)ulong.MaxValue / (max - min)) + min;
	}

	/// <summary>
	/// Generates a random integer number within a certain range, inclusive.
	/// Distinct from <see cref="Generate(int, int)"/> in that it doesn't touch the seed.
	/// </summary>
	public static int Seedless(int min, int max) => s_seedless.Generate(min, max);

	/// <summary>Generates a random percent chance of an event occurring.</summary>
	public static bool Percent(int value) => s_state.Percent(value);

	/// <summary>Randomly changes the order of the elements in a list.</summary>
	public static void Shuffle<T>(IList<T> list)
	{
		ArgumentNullException.ThrowIfNull(list);
		if (list.Count == 0)
			return;
		for (int i = list.Count - 1; i > 0; --i)
		{
			int j = Generate(0, i);
			(list[i], list[j]) = (list[j], list[i]);
		}
	}
}
