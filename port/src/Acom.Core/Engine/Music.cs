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
// Ported from: src/Engine/Music.h, src/Engine/Music.cpp

namespace Acom;

/// <summary>
/// Container for music tracks.
/// Handles loading and playing various formats through the <see cref="Mixer"/>.
/// </summary>
/// <remarks>
/// The C++ destructor stops all music and frees the track; tracks here are plain objects
/// that hold their data until they are collected.
/// </remarks>
public class Music
{
	private MixMusic? _music;

	/// <summary>Initializes a new music track.</summary>
	public Music()
	{
	}

	/// <summary>Loads a music file from a specified filename.</summary>
	/// <param name="filename">Filename of the music file.</param>
	public virtual void Load(string filename)
	{
		using (var rw = FileMap.GetStream(filename))
			Load(rw);
		Logger.Verbose($"Music::load('{filename}')");
	}

	/// <summary>Loads a music file from a specified stream.</summary>
	/// <param name="rwops">Stream of the music data.</param>
	/// <exception cref="InvalidOperationException">The format is not supported.</exception>
	public virtual void Load(Stream rwops)
	{
		_music = Mixer.LoadMUS(rwops);

		if (_music == null)
		{
			throw new InvalidOperationException(Mixer.GetError());
		}
	}

	/// <summary>Plays the contained music track.</summary>
	/// <param name="loop">Amount of times to loop the track. -1 = infinite.</param>
#pragma warning disable CA1716 // parameter name as in C++
	public virtual void Play(int loop = -1)
#pragma warning restore CA1716
	{
		if (!Options.mute)
		{
			if (_music != null)
			{
				Stop();
				if (Mixer.PlayMusic(_music, loop) == -1)
				{
					Logger.Warning(Mixer.GetError());
				}
			}
		}
	}

	/// <summary>Stops all music playing.</summary>
	public static void Stop()
	{
		if (!Options.mute)
		{
			// C++ mutes the player while the audio thread may still run it; the lock keeps the chips consistent
			Mixer.LockAudio();
			try
			{
				AdlPlayer.func_mute();
			}
			finally
			{
				Mixer.UnlockAudio();
			}
			Mixer.HookMusic(null, null);
			Mixer.HaltMusic();
		}
	}

	/// <summary>Pauses music playback when game loses focus.</summary>
	public static void Pause()
	{
		if (!Options.mute)
		{
			Mixer.PauseMusic();
			if (Mixer.GetMusicType(null) == MusicType.MUS_NONE)
				Mixer.HookMusic(null, null);
		}
	}

	/// <summary>Resumes music playback when game gains focus.</summary>
	public static void Resume()
	{
		if (!Options.mute)
		{
			Mixer.ResumeMusic();
			if (Mixer.GetMusicType(null) == MusicType.MUS_NONE)
				Mixer.HookMusic(AdlibMusic.Player, null);
		}
	}

	/// <summary>Checks if music is playing (C++: actually checks the sound channels, Mix_Playing(-1)).</summary>
	public static bool IsPlaying
	{
		get
		{
			if (!Options.mute)
			{
				return Mixer.Playing(-1) != 0;
			}
			return false;
		}
	}

	/// <summary>Format of the loaded track (MUS_NONE if nothing is loaded).</summary>
	internal MusicType Type => _music?.Type ?? MusicType.MUS_NONE;
}
