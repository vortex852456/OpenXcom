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
// Ported from: SDL 1.2 SDL_timer.h (SDL_GetTicks)

using System.Diagnostics;

namespace Acom.Engine;

/// <summary>Millisecond clock of SDL 1.2.</summary>
public static class SDLTimer
{
	private static readonly long s_start = Stopwatch.GetTimestamp();

	/// <summary>Clock source in milliseconds since start; replaceable for tests.</summary>
	public static Func<uint> Source { get; set; } = () => (uint)(long)Stopwatch.GetElapsedTime(s_start).TotalMilliseconds;

	/// <summary>Gets the number of milliseconds since the start.</summary>
	public static uint SDL_GetTicks() => Source();
}
