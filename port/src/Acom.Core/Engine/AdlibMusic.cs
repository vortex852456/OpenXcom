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
// Ported from: src/Engine/AdlibMusic.h, src/Engine/AdlibMusic.cpp

using System.Runtime.InteropServices;

namespace Acom;

/// <summary>
/// Container for Adlib music tracks.
/// Uses a custom YM3812 music player (<see cref="AdlPlayer"/> on two emulated <see cref="Fmopl"/> chips),
/// which runs as the mixer's music hook.
/// </summary>
/// <remarks>
/// The C++ destructor stops the music and destroys the shared chips; here tracks have no destructor,
/// the chips are shared by all tracks and only recreated when the sample rate changes.
/// </remarks>
public class AdlibMusic : Music
{
	private byte[] _data = [];
	private int _size;
	private readonly float _volume;
	private static int delay, rate;
	private static readonly Dictionary<int, int> delayRates = [];

	/// <summary>Initializes a new music track.</summary>
	/// <param name="volume">Music volume modifier (1.0 = 100%).</param>
	public AdlibMusic(float volume = 1.0f)
	{
		_volume = volume;
		rate = Options.audioSampleRate;
		Mixer.LockAudio(); // the music hook may be rendering the chips
		try
		{
			// C++ destroys the chips with every track (at the latest when the mod is reloaded), so the next
			// track creates them for the current sample rate; shared chips are recreated when it changed.
			if (AdlPlayer.opl[0] != null && AdlPlayer.opl[0]!.rate != rate)
			{
				AdlPlayer.ResetChips();
			}
			if (AdlPlayer.opl[0] == null)
			{
				AdlPlayer.opl[0] = Fmopl.OPLCreate(Fmopl.OPL_TYPE_YM3812, 3579545, rate);
			}
			if (AdlPlayer.opl[1] == null)
			{
				AdlPlayer.opl[1] = Fmopl.OPLCreate(Fmopl.OPL_TYPE_YM3812, 3579545, rate);
			}
		}
		finally
		{
			Mixer.UnlockAudio();
		}
		// magic value - length of 1 tick per sample rate
		if (delayRates.Count == 0)
		{
			delayRates[8000] = 114 * 4;
			delayRates[11025] = 157 * 4;
			delayRates[16000] = 228 * 4;
			delayRates[22050] = 314 * 4;
			delayRates[32000] = 456 * 4;
			delayRates[44100] = 629 * 4;
			delayRates[48000] = 685 * 4;
		}
	}

	/// <summary>Loads a music file from a specified filename.</summary>
	/// <param name="filename">Filename of the music file.</param>
	public override void Load(string filename)
	{
		using var rw = FileMap.GetStream(filename);
		Load(rw);
	}

	/// <summary>Loads a music file from a specified stream.</summary>
	/// <param name="rwops">Stream of the music data.</param>
	public override void Load(Stream rwops)
	{
		ArgumentNullException.ThrowIfNull(rwops);
		using var ms = new MemoryStream();
		rwops.CopyTo(ms);
		_data = ms.ToArray();
		_size = _data.Length;
	}

	/// <summary>Plays the contained music track.</summary>
	/// <param name="loop">Ignored: the track loops by itself or with <c>Options.musicAlwaysLoop</c>.</param>
	public override void Play(int loop = -1)
	{
		if (!Options.mute)
		{
			Stop();
			AdlPlayer.func_setup_music(_data, _size);
			AdlPlayer.func_set_music_volume((int)(127 * _volume));
			Mixer.HookMusic(Player, this);
		}
	}

	/// <summary>Custom audio player (the music hook).</summary>
	/// <param name="udata">The playing <see cref="AdlibMusic"/>.</param>
	/// <param name="stream">Output: interleaved signed 16-bit stereo.</param>
	/// <param name="len">Length of <paramref name="stream"/> in bytes.</param>
	public static void Player(object? udata, Span<byte> stream, int len)
	{
		// Check SDL volume for Background Mute functionality
		if (Options.musicVolume == 0 || Mixer.VolumeMusic(-1) == 0)
			return;
		if (Options.musicAlwaysLoop && !AdlPlayer.func_is_music_playing())
		{
			// (C++ dereferences udata without a check; Music.Resume hooks the player with null)
			var music = udata as AdlibMusic;
			music?.Play();
			return;
		}
		while (len != 0)
		{
			if (AdlPlayer.opl[0] == null || AdlPlayer.opl[1] == null)
				return;
			int i = Math.Min(delay, len);
			if (i != 0)
			{
				float volume = (float)Game.VolumeExponent(Options.musicVolume);
				var samples = MemoryMarshal.Cast<byte, short>(stream);
				Fmopl.YM3812UpdateOne(AdlPlayer.opl[0]!, samples, i / 2, 2, volume);
				Fmopl.YM3812UpdateOne(AdlPlayer.opl[1]!, samples[1..], i / 2, 2, volume);
				stream = stream[i..];
				delay -= i;
				len -= i;
			}
			if (len == 0)
				return;
			AdlPlayer.func_play_tick();

			delay = DelayRate(rate);
		}
	}

	/// <summary>Length of one tick in bytes of output.</summary>
	/// <remarks>
	/// C++ reads <c>delayRates[rate]</c>, which is 0 for rates outside the table and hangs the
	/// audio thread; such rates get a proportional value here.
	/// </remarks>
	private static int DelayRate(int r)
	{
		if (delayRates.TryGetValue(r, out int d))
			return d;
		return Math.Max(4, (int)((long)r * 629 / 44100) * 4);
	}

	/// <summary>Checks if the Adlib player is playing.</summary>
#pragma warning disable CA1822 // instance member as in C++
	public new bool IsPlaying
#pragma warning restore CA1822
	{
		get
		{
			if (!Options.mute)
			{
				return AdlPlayer.func_is_music_playing();
			}
			return false;
		}
	}

	/// <summary>Resets the static tick counter (tests: C++ keeps it across tracks).</summary>
	internal static void ResetDelayForTest() => delay = 0;
}
