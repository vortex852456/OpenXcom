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
// Ported from: src/Engine/Sound.h, src/Engine/Sound.cpp

namespace Acom;

/// <summary>
/// Container for sound effects.
/// Handles loading and playing various formats through the <see cref="Mixer"/>.
/// </summary>
public class Sound
{
	private MixChunk? _sound;

	/// <summary>Takes ownership of a loaded chunk (C++: smart pointer for Mix_Chunk).</summary>
	public static MixChunk? NewSound(MixChunk? sound) => sound;

	/// <summary>Creates a blank sound effect.</summary>
	public Sound()
	{
	}

	/// <summary>Loads a sound file from a specified filename.</summary>
	/// <param name="filename">Filename of the sound file.</param>
	public void Load(string filename)
	{
		MixChunk? s;
		using (var rw = FileMap.GetStream(filename))
			s = NewSound(Mixer.LoadWAV(rw));
		if (s == null)
		{
			Logger.Error($"Sound::load({filename}): mix error={Mixer.GetError()}");
		}

		//always overwrite
		_sound = s;
	}

	/// <summary>Loads a sound file from a specified stream.</summary>
	/// <param name="rw">Stream of the sound data.</param>
	public void Load(Stream rw)
	{
		var s = NewSound(Mixer.LoadWAV(rw));
		if (s == null)
		{
			Logger.Error($"Sound::load(data): mix error={Mixer.GetError()}");
		}

		//always overwrite
		_sound = s;
	}

	/// <summary>Plays the contained sound effect.</summary>
	/// <param name="channel">Use specified channel, -1 to use any channel.</param>
	/// <param name="angle">Stereo angle (see <see cref="Mixer.SetPosition"/>).</param>
	/// <param name="distance">Distance (see <see cref="Mixer.SetPosition"/>).</param>
	public void Play(int channel = -1, int angle = 0, int distance = 0)
	{
		if (!Options.mute && _sound != null)
		{
			int chan = Mixer.PlayChannel(channel, _sound, 0);
			if (chan == -1)
			{
				Logger.Warning(Mixer.GetError());
			}
			else if (Options.StereoSound)
			{
				if (Mixer.SetPosition(chan, (short)angle, (byte)distance) == 0)
				{
					Logger.Warning(Mixer.GetError());
				}
			}
		}
	}

	/// <summary>Stops all sounds playing.</summary>
	public static void Stop()
	{
		if (!Options.mute)
		{
			Mixer.HaltChannel(-1);
		}
	}

	/// <summary>Plays the contained sound effect repeatedly on the reserved ambience channel.</summary>
	public void Loop()
	{
		if (!Options.mute && _sound != null && Mixer.Playing(3) == 0)
		{
			int chan = Mixer.PlayChannel(3, _sound, -1);
			if (chan == -1)
			{
				Logger.Warning(Mixer.GetError());
			}
		}
	}

	/// <summary>Stops the contained sound from looping.</summary>
#pragma warning disable CA1822 // instance member as in C++
	public void StopLoop()
#pragma warning restore CA1822
	{
		if (!Options.mute)
		{
			Mixer.HaltChannel(3);
		}
	}

	/// <summary>Whether a sound is loaded (diagnostics and tests).</summary>
	internal bool IsLoaded => _sound != null;

	/// <summary>The decoded samples (tests).</summary>
	internal MixChunk? Chunk => _sound;
}
