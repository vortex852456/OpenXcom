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
// Ported from: SDL_mixer 1.2 SDL_mixer.h (Mix_GroupAvailable)

namespace Acom;

/// <summary>The SDL_mixer calls used by game code outside the sound classes.</summary>
public static class SDLMixer
{
	/// <summary>Channel lookup of the audio backend (tag -> free channel or -1); set by the host.</summary>
	public static Func<int, int> GroupAvailableSource { get; set; } = _ => -1;

	/// <summary>Finds the first available (not playing) channel in a group; -1 if none (Sound.Play then picks any free channel).</summary>
	public static int Mix_GroupAvailable(int tag) => GroupAvailableSource(tag);
}
