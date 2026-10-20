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
// Not a port of an OpenXcom unit: replaces the part of SDL_mixer 1.2 the game uses (see docs/architecture/csharp-port.md, "Audio").

using System.Runtime.InteropServices;

namespace Acom;

/// <summary>Music hook / mixing callback, like SDL_mixer's <c>void (*)(void *udata, Uint8 *stream, int len)</c>.</summary>
/// <param name="udata">User data given to <see cref="Mixer.HookMusic"/>.</param>
/// <param name="stream">Output buffer (interleaved signed 16-bit samples as bytes), silence on entry.</param>
/// <param name="len">Length of <paramref name="stream"/> in bytes.</param>
public delegate void MixFunc(object? udata, Span<byte> stream, int len);

/// <summary>Music types (SDL_mixer <c>Mix_MusicType</c>, same order).</summary>
public enum MusicType
{
	MUS_NONE,
	MUS_CMD,
	MUS_WAV,
	MUS_MOD,
	MUS_MID,
	MUS_OGG,
	MUS_MP3,
	MUS_MP3_MAD,
	MUS_FLAC,
	MUS_MODPLUG,
}

/// <summary>Fade state (SDL_mixer <c>Mix_Fading</c>).</summary>
public enum MixFading
{
	MIX_NO_FADING,
	MIX_FADING_OUT,
	MIX_FADING_IN,
}

/// <summary>
/// Audio output of the host. The host pulls the mixed signal with <see cref="Mixer.Mix"/>
/// (interleaved signed 16-bit samples at the opened rate and channel count).
/// </summary>
public interface IAudioDevice
{
	/// <summary>Starts the output; false if the hardware cannot play this format.</summary>
	bool Open(int frequency, int channels, int samples);

	/// <summary>Stops the output.</summary>
	void Close();
}

/// <summary>A decoded sound effect (SDL_mixer <c>Mix_Chunk</c>), already in the output format of the mixer.</summary>
public sealed class MixChunk
{
	internal MixChunk(short[] abuf)
	{
		Abuf = abuf;
	}

	/// <summary>Interleaved signed 16-bit samples.</summary>
	internal short[] Abuf { get; }

	/// <summary>Per-chunk volume, 0 to <see cref="Mixer.MIX_MAX_VOLUME"/>.</summary>
	public int Volume { get; set; } = Mixer.MIX_MAX_VOLUME;
}

/// <summary>A loaded music track (SDL_mixer <c>Mix_Music</c>).</summary>
public sealed class MixMusic
{
	internal MixMusic(MusicType type, IMusicDecoder decoder)
	{
		Type = type;
		Decoder = decoder;
	}

	/// <summary>Format of the track.</summary>
	public MusicType Type { get; }

	internal IMusicDecoder Decoder { get; }
	internal MixFading fading;
	internal int fade_step;
	internal int fade_steps;
}

/// <summary>Streaming music decoder behind a <see cref="MixMusic"/>.</summary>
internal interface IMusicDecoder
{
	/// <summary>Starts playing from the beginning in the given output format; false on error.</summary>
	bool Start(int frequency, int channels);

	/// <summary>Stops playing and releases decoder resources.</summary>
	void Stop();

	/// <summary>Whether there is still something to play.</summary>
	bool IsPlaying { get; }

	/// <summary>Mixes the next samples into <paramref name="stream"/> with the given volume.</summary>
	/// <returns>Number of samples at the end of <paramref name="stream"/> that could not be filled.</returns>
	int PlaySome(Span<short> stream, int volume);
}

/// <summary>
/// Software mixer with the semantics of SDL_mixer 1.2.12 for everything the game relies on:
/// channels (allocation, reservation, groups), per-channel and music volume, fade-out, looping,
/// stereo positioning, music with loops and fading, and the music hook used by the Adlib emulator
/// and the FLC player. Output is always signed 16-bit; the host pulls it with <see cref="Mix"/>.
/// </summary>
/// <remarks>
/// Like SDL_mixer, the signal is produced in blocks of the opened chunk size, whatever amount the
/// host asks for, so hooks and fades see the same block sizes as in the original. Fade times are
/// measured in mixed samples instead of <c>SDL_GetTicks</c>, which makes them deterministic.
/// All calls are serialized by one lock (SDL_LockAudio); hooks run while it is held.
/// </remarks>
public static class Mixer
{
	public const int MIX_MAX_VOLUME = 128;
	public const int SDL_MIX_MAXVOLUME = 128;
	public const int MIX_CHANNELS = 8;
	public const int MIX_DEFAULT_FREQUENCY = 22050;
	public const int MIX_DEFAULT_CHANNELS = 2;
	public const ushort AUDIO_U8 = 0x0008;
	public const ushort AUDIO_S8 = 0x8008;
	public const ushort AUDIO_S16LSB = 0x8010;
	public const ushort AUDIO_S16SYS = AUDIO_S16LSB;
	public const ushort MIX_DEFAULT_FORMAT = AUDIO_S16LSB;

	private sealed class PositionArgs
	{
		public float left_f = 1.0f, right_f = 1.0f, distance_f = 1.0f;
		public int room_angle;
	}

	private sealed class Channel
	{
		public MixChunk? chunk;
		public int samples;   // read position in chunk.Abuf
		public int playing;   // samples left
		public int looping;
		public int volume = SDL_MIX_MAXVOLUME;
		public int fade_volume = SDL_MIX_MAXVOLUME;
		public int fade_volume_reset = SDL_MIX_MAXVOLUME;
		public MixFading fading = MixFading.MIX_NO_FADING;
		public uint ticks_fade;
		public int fade_length;
		public int tag = -1;
		public PositionArgs? position; // the registered position effect, null = none
	}

	private static readonly object s_lock = new();
	private static string s_error = "";

	private static int s_audioOpened;
	private static ushort s_format;
	private static int s_frequency;
	private static int s_channels;
	private static int s_samples;
	private static Channel[] s_mixChannel = [];
	private static int s_reservedChannels;

	private static short[] s_block = [];
	private static int s_blockPos;
	private static long s_framesMixed;

	private static MixFunc? s_musicHook; // null = the music player (music_mixer)
	private static object? s_musicData;
	private static System.Action? s_musicFinishedHook;
	private static MixMusic? s_musicPlaying;
	private static int s_musicLoops;
	private static bool s_musicActive = true;
	private static int s_musicVolume = MIX_MAX_VOLUME;
	private static int s_musicInternalVolume = MIX_MAX_VOLUME;
	private static int s_msPerStep;

	/// <summary>The host's audio output; null = no audio hardware (opening fails, like a missing SDL audio device).</summary>
	public static IAudioDevice? Device { get; set; }

	/// <summary>SoundFont (.sf2) for MIDI music; null = <c>SDL_SOUNDFONTS</c> or a system default.</summary>
	public static string? SoundFontPath { get; set; }

	/// <summary>The last error message (Mix_GetError).</summary>
	public static string GetError()
	{
		lock (s_lock)
			return s_error;
	}

	internal static void SetError(string message)
	{
		lock (s_lock)
			s_error = message;
	}

	/// <summary>Locks out the mixing (SDL_LockAudio); hooks and channel state do not change until <see cref="UnlockAudio"/>.</summary>
	public static void LockAudio() => Monitor.Enter(s_lock);

	/// <summary>Releases <see cref="LockAudio"/> (SDL_UnlockAudio).</summary>
	public static void UnlockAudio() => Monitor.Exit(s_lock);

	/// <summary>Milliseconds of audio mixed since the mixer was opened (the mixer's SDL_GetTicks).</summary>
	internal static uint Ticks => s_frequency == 0 ? 0 : (uint)(s_framesMixed * 1000 / s_frequency);

	/// <summary>
	/// Opens the mixer (Mix_OpenAudio). Output is always signed 16-bit, whatever <paramref name="format"/> asks for;
	/// as in SDL_mixer, a second open with the same format and channel count only counts up.
	/// </summary>
	/// <returns>0 on success, -1 on error (see <see cref="GetError"/>).</returns>
	public static int OpenAudio(int frequency, ushort format, int channels, int chunksize)
	{
		lock (s_lock)
		{
			// If the mixer is already opened, increment open count
			if (s_audioOpened != 0)
			{
				if (format == s_format && channels == s_channels)
				{
					++s_audioOpened;
					return 0;
				}
				while (s_audioOpened != 0)
				{
					CloseAudio();
				}
			}

			if (Device == null)
			{
				s_error = "No audio device available";
				return -1;
			}
			if (frequency <= 0 || chunksize <= 0)
			{
				s_error = "Invalid audio format";
				return -1;
			}
			channels = channels == 1 ? 1 : 2;
			if (!Device.Open(frequency, channels, chunksize))
			{
				s_error = "Couldn't open audio device";
				return -1;
			}
			s_format = format;
			s_frequency = frequency;
			s_channels = channels;
			s_samples = chunksize;
			s_block = new short[chunksize * channels];
			s_blockPos = s_block.Length;
			s_framesMixed = 0;
			s_msPerStep = (int)(((float)chunksize * 1000.0) / frequency);

			// Clear out the audio channels
			s_mixChannel = new Channel[MIX_CHANNELS];
			for (int i = 0; i < s_mixChannel.Length; ++i)
				s_mixChannel[i] = new Channel();
			VolumeMusic(SDL_MIX_MAXVOLUME);

			s_audioOpened = 1;
			return 0;
		}
	}

	/// <summary>Closes the mixer (Mix_CloseAudio); the device closes when the last open is undone.</summary>
	public static void CloseAudio()
	{
		lock (s_lock)
		{
			if (s_audioOpened != 0)
			{
				if (s_audioOpened == 1)
				{
					foreach (var ch in s_mixChannel)
						ch.position = null;
					HaltMusic();
					s_msPerStep = 0;
					HaltChannel(-1);
					Device?.Close();
					s_mixChannel = [];
					s_block = [];
					s_blockPos = 0;
					s_frequency = 0;
				}
				--s_audioOpened;
			}
		}
	}

	/// <summary>Gets the output format (Mix_QuerySpec).</summary>
	/// <returns>Number of times the mixer was opened, 0 if closed.</returns>
	public static int QuerySpec(out int frequency, out ushort format, out int channels)
	{
		lock (s_lock)
		{
			frequency = s_frequency;
			format = AUDIO_S16LSB;
			channels = s_channels;
			return s_audioOpened;
		}
	}

	/// <summary>
	/// Produces the next <paramref name="stream"/>.Length samples (interleaved, opened channel count).
	/// Called by the host's audio output; silence while the mixer is closed.
	/// </summary>
	public static void Mix(Span<short> stream)
	{
		lock (s_lock)
		{
			if (s_audioOpened == 0)
			{
				stream.Clear();
				return;
			}
			while (!stream.IsEmpty)
			{
				if (s_blockPos == s_block.Length)
				{
					MixChannels(s_block);
					s_framesMixed += s_samples;
					s_blockPos = 0;
				}
				int n = Math.Min(stream.Length, s_block.Length - s_blockPos);
				s_block.AsSpan(s_blockPos, n).CopyTo(stream);
				s_blockPos += n;
				stream = stream[n..];
			}
		}
	}

	/// <summary>Mixes one block (mix_channels).</summary>
	private static void MixChannels(short[] block)
	{
		var stream = block.AsSpan();
		stream.Clear();
		int len = stream.Length;

		// Mix the music (must be done before the channels are added)
		if (s_musicActive || s_musicHook != null)
		{
			var bytes = MemoryMarshal.AsBytes(stream);
			if (s_musicHook != null)
				s_musicHook(s_musicData, bytes, bytes.Length);
			else
				MusicMixer(stream);
		}

		// Mix any playing channels...
		uint sdl_ticks = Ticks;
		for (int i = 0; i < s_mixChannel.Length; ++i)
		{
			var ch = s_mixChannel[i];
			if (ch.fading != MixFading.MIX_NO_FADING)
			{
				uint ticks = sdl_ticks - ch.ticks_fade;
				if (ticks > ch.fade_length)
				{
					Volume(i, ch.fade_volume_reset); // Restore the volume
					if (ch.fading == MixFading.MIX_FADING_OUT)
					{
						ch.playing = 0;
						ch.looping = 0;
						ChannelDonePlaying(ch);
					}
					ch.fading = MixFading.MIX_NO_FADING;
				}
				else
				{
					if (ch.fading == MixFading.MIX_FADING_OUT)
					{
						Volume(i, (int)((ch.fade_volume * (ch.fade_length - ticks)) / ch.fade_length));
					}
					else
					{
						Volume(i, (int)((ch.fade_volume * ticks) / ch.fade_length));
					}
				}
			}
			if (ch.playing > 0)
			{
				var chunk = ch.chunk!;
				int index = 0;
				int remaining;
				int volume = 0;
				while (ch.playing > 0 && index < len)
				{
					remaining = len - index;
					volume = (ch.volume * chunk.Volume) / MIX_MAX_VOLUME;
					int mixable = ch.playing;
					if (mixable > remaining)
					{
						mixable = remaining;
					}

					MixInput(ch, stream.Slice(index, mixable), chunk.Abuf.AsSpan(ch.samples, mixable), volume);

					ch.samples += mixable;
					ch.playing -= mixable;
					index += mixable;

					// Alert app if channel is done playing.
					if (ch.playing == 0 && ch.looping == 0)
					{
						ChannelDonePlaying(ch);
					}
				}

				// If looping the sample and we are at its end, make sure
				// we will still return a full buffer
				while (ch.looping != 0 && index < len)
				{
					int alen = chunk.Abuf.Length;
					remaining = len - index;
					if (remaining > alen)
					{
						remaining = alen;
					}

					MixInput(ch, stream.Slice(index, remaining), chunk.Abuf.AsSpan(0, remaining), volume);

					if (ch.looping > 0)
					{
						--ch.looping;
					}
					ch.samples = remaining;
					ch.playing = alen - remaining;
					index += remaining;
				}
				if (ch.playing == 0 && ch.looping != 0)
				{
					if (ch.looping > 0)
					{
						--ch.looping;
					}
					ch.samples = 0;
					ch.playing = chunk.Abuf.Length;
				}
			}
		}
	}

	/// <summary>Applies the channel's effects to a copy of the input and mixes it (Mix_DoEffects + SDL_MixAudio).</summary>
	private static void MixInput(Channel ch, Span<short> dst, ReadOnlySpan<short> src, int volume)
	{
		var pos = ch.position;
		if (pos == null)
		{
			MixAudio(dst, src, volume);
			return;
		}
		short[] buf = new short[src.Length];
		src.CopyTo(buf);
		// _Eff_position_s16lsb: 16 signed bits * 2 channels. Behind the listener (room angle 180) the input
		// channels are exchanged and then attenuated for the side they end up on (measured with SDL_mixer 1.2.12).
		bool opp = pos.room_angle == 180;
		for (int i = 0; i + 1 < buf.Length; i += 2)
		{
			short inl = opp ? buf[i + 1] : buf[i];
			short inr = opp ? buf[i] : buf[i + 1];
			buf[i] = (short)((((float)inl) * pos.left_f) * pos.distance_f);
			buf[i + 1] = (short)((((float)inr) * pos.right_f) * pos.distance_f);
		}
		MixAudio(dst, buf, volume);
	}

	/// <summary>Adds <paramref name="src"/> scaled by <paramref name="volume"/> to <paramref name="dst"/> with clipping (SDL_MixAudio, AUDIO_S16).</summary>
	internal static void MixAudio(Span<short> dst, ReadOnlySpan<short> src, int volume)
	{
		if (volume == 0)
		{
			return;
		}
		const int max_audioval = (1 << (16 - 1)) - 1;
		const int min_audioval = -(1 << (16 - 1));
		int n = Math.Min(dst.Length, src.Length);
		for (int i = 0; i < n; ++i)
		{
			short src1 = (short)((src[i] * volume) / SDL_MIX_MAXVOLUME); // ADJUST_VOLUME
			int dst_sample = src1 + dst[i];
			if (dst_sample > max_audioval)
			{
				dst_sample = max_audioval;
			}
			else if (dst_sample < min_audioval)
			{
				dst_sample = min_audioval;
			}
			dst[i] = (short)dst_sample;
		}
	}

	/// <summary>A channel stopped: remove its effects (_Mix_channel_done_playing).</summary>
	private static void ChannelDonePlaying(Channel ch)
	{
		ch.position = null;
	}

	// ---------------- channels ----------------

	/// <summary>Changes the number of channels (Mix_AllocateChannels).</summary>
	public static int AllocateChannels(int numchans)
	{
		lock (s_lock)
		{
			int num_channels = s_mixChannel.Length;
			if (numchans < 0 || numchans == num_channels)
				return num_channels;

			if (numchans < num_channels)
			{
				// Stop the affected channels
				for (int i = numchans; i < num_channels; i++)
				{
					s_mixChannel[i].position = null;
					HaltChannel(i);
				}
			}
			var channels = new Channel[numchans];
			for (int i = 0; i < numchans; i++)
				channels[i] = i < num_channels ? s_mixChannel[i] : new Channel();
			s_mixChannel = channels;
			return numchans;
		}
	}

	/// <summary>Reserves the first channels from being used by <see cref="PlayChannel"/> with channel -1 (Mix_ReserveChannels).</summary>
	public static int ReserveChannels(int num)
	{
		lock (s_lock)
		{
			if (num > s_mixChannel.Length)
				num = s_mixChannel.Length;
			s_reservedChannels = num;
			return num;
		}
	}

	/// <summary>Attaches a tag to a channel (Mix_GroupChannel).</summary>
	public static int GroupChannel(int which, int tag)
	{
		lock (s_lock)
		{
			if (which < 0 || which >= s_mixChannel.Length) // SDL_mixer: "> num_channels"
				return 0;
			s_mixChannel[which].tag = tag;
			return 1;
		}
	}

	/// <summary>Tags a range of channels (Mix_GroupChannels).</summary>
	public static int GroupChannels(int from, int to, int tag)
	{
		int status = 0;
		for (; from <= to; ++from)
		{
			status += GroupChannel(from, tag);
		}
		return status;
	}

	/// <summary>First free channel of a group, -1 if none (Mix_GroupAvailable).</summary>
	public static int GroupAvailable(int tag)
	{
		lock (s_lock)
		{
			for (int i = 0; i < s_mixChannel.Length; i++)
			{
				if (((tag == -1) || (tag == s_mixChannel[i].tag)) && (s_mixChannel[i].playing <= 0))
					return i;
			}
			return -1;
		}
	}

	/// <summary>Plays a chunk (Mix_PlayChannel).</summary>
	/// <param name="which">Channel, -1 = first free unreserved channel.</param>
	/// <param name="chunk">The sound.</param>
	/// <param name="loops">Number of extra repetitions, -1 = forever.</param>
	/// <returns>The channel, -1 on error.</returns>
	public static int PlayChannel(int which, MixChunk? chunk, int loops)
	{
		lock (s_lock)
		{
			// Don't play null pointers :-)
			if (chunk == null)
			{
				s_error = "Tried to play a NULL chunk";
				return -1;
			}
			// checkchunkintegral: chunks hold whole frames of the output format, so only an empty one fails
			if (chunk.Abuf.Length == 0)
			{
				s_error = "Tried to play a chunk with a bad frame";
				return -1;
			}

			// If which is -1, play on the first free channel
			if (which == -1)
			{
				int i;
				for (i = s_reservedChannels; i < s_mixChannel.Length; ++i)
				{
					if (s_mixChannel[i].playing <= 0)
						break;
				}
				if (i >= s_mixChannel.Length)
				{
					s_error = "No free channels available";
					which = -1;
				}
				else
				{
					which = i;
				}
			}

			// Queue up the audio data for this channel
			if (which >= 0 && which < s_mixChannel.Length)
			{
				var ch = s_mixChannel[which];
				if (PlayingLocked(which) != 0)
					ChannelDonePlaying(ch);
				ch.samples = 0;
				ch.playing = chunk.Abuf.Length;
				ch.looping = loops;
				ch.chunk = chunk;
				ch.fading = MixFading.MIX_NO_FADING;
			}

			// Return the channel on which the sound is being played
			return which;
		}
	}

	/// <summary>Number of playing channels (which = -1) or whether one channel plays (Mix_Playing).</summary>
	public static int Playing(int which)
	{
		lock (s_lock)
			return PlayingLocked(which);
	}

	private static int PlayingLocked(int which)
	{
		int status = 0;
		if (which == -1)
		{
			foreach (var ch in s_mixChannel)
			{
				if ((ch.playing > 0) || (ch.looping > 0))
					++status;
			}
		}
		else if (which >= 0 && which < s_mixChannel.Length)
		{
			var ch = s_mixChannel[which];
			if ((ch.playing > 0) || (ch.looping > 0))
				++status;
		}
		return status;
	}

	/// <summary>Stops a channel, -1 = all (Mix_HaltChannel).</summary>
	public static int HaltChannel(int which)
	{
		lock (s_lock)
		{
			if (which == -1)
			{
				for (int i = 0; i < s_mixChannel.Length; ++i)
				{
					HaltChannel(i);
				}
			}
			else if (which >= 0 && which < s_mixChannel.Length)
			{
				var ch = s_mixChannel[which];
				if (ch.playing != 0)
				{
					ChannelDonePlaying(ch);
					ch.playing = 0;
					ch.looping = 0;
				}
				if (ch.fading != MixFading.MIX_NO_FADING) // Restore volume
					ch.volume = ch.fade_volume_reset;
				ch.fading = MixFading.MIX_NO_FADING;
			}
			return 0;
		}
	}

	/// <summary>Fades out a channel, -1 = all (Mix_FadeOutChannel).</summary>
	/// <returns>Number of channels set to fade out.</returns>
	public static int FadeOutChannel(int which, int ms)
	{
		lock (s_lock)
		{
			int status = 0;
			if (s_audioOpened != 0)
			{
				if (which == -1)
				{
					for (int i = 0; i < s_mixChannel.Length; ++i)
					{
						status += FadeOutChannel(i, ms);
					}
				}
				else if (which >= 0 && which < s_mixChannel.Length)
				{
					var ch = s_mixChannel[which];
					if (ch.playing != 0 && (ch.volume > 0) && (ch.fading != MixFading.MIX_FADING_OUT))
					{
						ch.fade_volume = ch.volume;
						ch.fading = MixFading.MIX_FADING_OUT;
						ch.fade_length = ms;
						ch.ticks_fade = Ticks;

						// only change fade_volume_reset if we're not fading.
						// (SDL_mixer 1.2.12 checks this after setting MIX_FADING_OUT, so it never changes)
						if (ch.fading == MixFading.MIX_NO_FADING)
						{
							ch.fade_volume_reset = ch.volume;
						}
						++status;
					}
				}
			}
			return status;
		}
	}

	/// <summary>Sets the volume of a channel, -1 = all; a negative volume only queries (Mix_Volume).</summary>
	/// <returns>The previous volume (average for -1).</returns>
	public static int Volume(int which, int volume)
	{
		lock (s_lock)
		{
			int prev_volume = 0;
			if (which == -1)
			{
				if (s_mixChannel.Length == 0)
					return 0;
				for (int i = 0; i < s_mixChannel.Length; ++i)
				{
					prev_volume += Volume(i, volume);
				}
				prev_volume /= s_mixChannel.Length;
			}
			else if (which >= 0 && which < s_mixChannel.Length)
			{
				prev_volume = s_mixChannel[which].volume;
				if (volume >= 0)
				{
					if (volume > SDL_MIX_MAXVOLUME)
					{
						volume = SDL_MIX_MAXVOLUME;
					}
					s_mixChannel[which].volume = volume;
				}
			}
			return prev_volume;
		}
	}

	/// <summary>Positions the sound of a channel in stereo space until it stops (Mix_SetPosition).</summary>
	/// <param name="channel">The channel.</param>
	/// <param name="angle">0 = front, 90 = right, 180 = behind, 270 = left.</param>
	/// <param name="distance">0 = near (loud) to 255 = far (silent).</param>
	/// <returns>0 on error.</returns>
	public static int SetPosition(int channel, short angle, byte distance)
	{
		lock (s_lock)
		{
			if (s_audioOpened == 0)
			{
				s_error = "Audio device hasn't been opened";
				return 0;
			}
			if (channel < 0 || channel >= s_mixChannel.Length)
			{
				s_error = "Invalid channel number";
				return 0;
			}
			var ch = s_mixChannel[channel];
			int a = Math.Abs((int)angle) % 360; // make angle between 0 and 359.

			// it's a no-op; unregister the effect, if it's registered.
			if (distance == 0 && a == 0)
			{
				ch.position = null;
				return 1;
			}

			int room_angle = 0;
			if (s_channels == 2)
			{
				if (a > 180)
					room_angle = 180; // exchange left and right channels
				else
					room_angle = 0;
			}

			distance = (byte)(255 - distance); // flip it to scale Mix_SetDistance() uses.

			// set_amplitudes
			int left = 255, right = 255;
			if (s_channels == 2)
			{
				if (a < 90)
				{
					left = 255 - ((int)(255.0f * (((float)a) / 89.0f)));
				}
				else if (a < 180)
				{
					left = (int)(255.0f * (((float)(a - 90)) / 89.0f));
				}
				else if (a < 270)
				{
					right = 255 - ((int)(255.0f * (((float)(a - 180)) / 89.0f)));
				}
				else
				{
					right = (int)(255.0f * (((float)(a - 270)) / 89.0f));
				}
			}

			var args = ch.position ?? new PositionArgs();
			args.left_f = ((float)(byte)left) / 255.0f;
			args.right_f = ((float)(byte)right) / 255.0f;
			args.distance_f = ((float)distance) / 255.0f;
			args.room_angle = room_angle;
			ch.position = args;
			return 1;
		}
	}

	// ---------------- chunks ----------------

	/// <summary>Loads a sound (WAV or Ogg Vorbis) and converts it to the output format (Mix_LoadWAV_RW).</summary>
	/// <returns>The chunk, null on error (see <see cref="GetError"/>).</returns>
	public static MixChunk? LoadWAV(Stream src)
	{
		ArgumentNullException.ThrowIfNull(src);
		byte[] data;
		using (var ms = new MemoryStream())
		{
			src.CopyTo(ms);
			data = ms.ToArray();
		}
		int frequency, channels;
		lock (s_lock)
		{
			if (s_audioOpened == 0)
			{
				s_error = "Audio device hasn't been opened";
				return null;
			}
			frequency = s_frequency;
			channels = s_channels;
		}
		var pcm = AudioDecoder.DecodeSound(data, out string? error);
		if (pcm == null)
		{
			SetError(error ?? "Unrecognized sound file type");
			return null;
		}
		return new MixChunk(pcm.Convert(frequency, channels));
	}

	// ---------------- music ----------------

	/// <summary>Loads a music track (Mix_LoadMUS_RW): WAV, Ogg Vorbis, MIDI (needs a SoundFont).</summary>
	/// <returns>The track, null on error (see <see cref="GetError"/>).</returns>
	public static MixMusic? LoadMUS(Stream src)
	{
		ArgumentNullException.ThrowIfNull(src);
		byte[] data;
		using (var ms = new MemoryStream())
		{
			src.CopyTo(ms);
			data = ms.ToArray();
		}
		var music = AudioDecoder.LoadMusic(data, out string? error);
		if (music == null)
			SetError(error ?? "Unrecognized music format");
		return music;
	}

	/// <summary>Plays a track (Mix_PlayMusic).</summary>
	/// <param name="music">The track.</param>
	/// <param name="loops">Number of times to play, -1 = forever (0 and 1 both play once).</param>
	/// <returns>0 on success, -1 on error.</returns>
	public static int PlayMusic(MixMusic? music, int loops)
	{
		lock (s_lock)
		{
			// Don't play null pointers :-)
			if (music == null)
			{
				s_error = "music parameter was NULL";
				return -1;
			}

			// Setup the data
			music.fading = MixFading.MIX_NO_FADING;
			music.fade_step = 0;
			music.fade_steps = 0;

			// If the current music is fading out, SDL_mixer waits for the fade to complete;
			// here it stops at once (waiting would block when nobody pulls the audio).
			if (s_musicPlaying != null && s_musicPlaying.fading == MixFading.MIX_FADING_OUT)
			{
				MusicInternalHalt();
			}
			s_musicActive = true;
			if (loops == 1)
			{
				// Loop is the number of times to play the audio
				loops = 0;
			}
			s_musicLoops = loops;
			return MusicInternalPlay(music);
		}
	}

	/// <summary>Stops the music (Mix_HaltMusic).</summary>
	public static int HaltMusic()
	{
		lock (s_lock)
		{
			if (s_musicPlaying != null)
			{
				MusicInternalHalt();
				s_musicFinishedHook?.Invoke();
			}
			return 0;
		}
	}

	/// <summary>Pauses the music player; a music hook keeps running (Mix_PauseMusic).</summary>
	public static void PauseMusic()
	{
		lock (s_lock)
			s_musicActive = false;
	}

	/// <summary>Resumes the music player (Mix_ResumeMusic).</summary>
	public static void ResumeMusic()
	{
		lock (s_lock)
			s_musicActive = true;
	}

	/// <summary>Whether the music player is paused (Mix_PausedMusic).</summary>
	public static int PausedMusic()
	{
		lock (s_lock)
			return s_musicActive ? 0 : 1;
	}

	/// <summary>Whether a track is playing (Mix_PlayingMusic).</summary>
	public static int PlayingMusic()
	{
		lock (s_lock)
		{
			int playing = 0;
			if (s_musicPlaying != null)
			{
				playing = (s_musicLoops != 0 || MusicInternalPlaying()) ? 1 : 0;
			}
			return playing;
		}
	}

	/// <summary>Fades out the music over <paramref name="ms"/> milliseconds (Mix_FadeOutMusic).</summary>
	/// <returns>1 if a fade was started.</returns>
	public static int FadeOutMusic(int ms)
	{
		lock (s_lock)
		{
			int retval = 0;

			if (s_msPerStep == 0)
			{
				s_error = "Audio device hasn't been opened";
				return 0;
			}

			if (ms <= 0)
			{  // just halt immediately.
				HaltMusic();
				return 1;
			}

			if (s_musicPlaying != null)
			{
				int fade_steps = (ms + s_msPerStep - 1) / s_msPerStep;
				if (s_musicPlaying.fading == MixFading.MIX_NO_FADING)
				{
					s_musicPlaying.fade_step = 0;
				}
				else
				{
					int step;
					int old_fade_steps = s_musicPlaying.fade_steps;
					if (s_musicPlaying.fading == MixFading.MIX_FADING_OUT)
					{
						step = s_musicPlaying.fade_step;
					}
					else
					{
						step = old_fade_steps - s_musicPlaying.fade_step + 1;
					}
					s_musicPlaying.fade_step = (step * fade_steps) / old_fade_steps;
				}
				s_musicPlaying.fading = MixFading.MIX_FADING_OUT;
				s_musicPlaying.fade_steps = fade_steps;
				retval = 1;
			}
			return retval;
		}
	}

	/// <summary>Sets the music volume, a negative value only queries (Mix_VolumeMusic). Does not affect a music hook.</summary>
	/// <returns>The previous volume.</returns>
	public static int VolumeMusic(int volume)
	{
		lock (s_lock)
		{
			int prev_volume = s_musicVolume;
			if (volume < 0)
			{
				return prev_volume;
			}
			if (volume > SDL_MIX_MAXVOLUME)
			{
				volume = SDL_MIX_MAXVOLUME;
			}
			s_musicVolume = volume;
			if (s_musicPlaying != null)
			{
				s_musicInternalVolume = s_musicVolume;
			}
			return prev_volume;
		}
	}

	/// <summary>Type of a track; for null the type of the playing track (Mix_GetMusicType).</summary>
	public static MusicType GetMusicType(MixMusic? music)
	{
		lock (s_lock)
		{
			if (music != null)
				return music.Type;
			return s_musicPlaying?.Type ?? MusicType.MUS_NONE;
		}
	}

	/// <summary>Replaces the music player by a custom generator; null restores the player (Mix_HookMusic).</summary>
	public static void HookMusic(MixFunc? mix_func, object? arg)
	{
		lock (s_lock)
		{
			if (mix_func != null)
			{
				s_musicData = arg;
				s_musicHook = mix_func;
			}
			else
			{
				s_musicData = null;
				s_musicHook = null;
			}
		}
	}

	/// <summary>Called when a track stops (Mix_HookMusicFinished).</summary>
	public static void HookMusicFinished(System.Action? music_finished)
	{
		lock (s_lock)
			s_musicFinishedHook = music_finished;
	}

	/// <summary>The music player (music_mixer).</summary>
	private static void MusicMixer(Span<short> stream)
	{
		int left = 0;
		var music = s_musicPlaying;

		if (music != null && s_musicActive)
		{
			// Handle fading
			if (music.fading != MixFading.MIX_NO_FADING)
			{
				if (music.fade_step++ < music.fade_steps)
				{
					int volume;
					int fade_step = music.fade_step;
					int fade_steps = music.fade_steps;

					if (music.fading == MixFading.MIX_FADING_OUT)
					{
						volume = (s_musicVolume * (fade_steps - fade_step)) / fade_steps;
					}
					else
					{ // Fading in
						volume = (s_musicVolume * fade_step) / fade_steps;
					}
					s_musicInternalVolume = volume;
				}
				else
				{
					if (music.fading == MixFading.MIX_FADING_OUT)
					{
						MusicInternalHalt();
						s_musicFinishedHook?.Invoke();
						return;
					}
					music.fading = MixFading.MIX_NO_FADING;
				}
			}

			if (MusicHaltOrLoop() == 0)
				return;
			if (!MusicInternalPlaying())
				return;

			left = s_musicPlaying!.Decoder.PlaySome(stream, s_musicInternalVolume);
		}

		// Handle seamless music looping
		if (left > 0 && left < stream.Length)
		{
			MusicHaltOrLoop();
			if (MusicInternalPlaying())
				MusicMixer(stream[(stream.Length - left)..]);
		}
	}

	/// <summary>Restarts the music if it has to loop, otherwise stops it (music_halt_or_loop).</summary>
	private static int MusicHaltOrLoop()
	{
		if (!MusicInternalPlaying())
		{
			if (s_musicPlaying == null)
				return 0;
			// Restart music if it has to loop at a high level
			if (s_musicLoops != 0)
			{
				if (s_musicLoops > 0)
				{
					--s_musicLoops;
				}
				var current_fade = s_musicPlaying.fading;
				MusicInternalPlay(s_musicPlaying);
				if (s_musicPlaying != null)
					s_musicPlaying.fading = current_fade;
			}
			else
			{
				MusicInternalHalt();
				s_musicFinishedHook?.Invoke();
				return 0;
			}
		}
		return 1;
	}

	private static int MusicInternalPlay(MixMusic music)
	{
		// Note the music we're playing
		if (s_musicPlaying != null)
		{
			MusicInternalHalt();
		}
		s_musicPlaying = music;

		// Set the initial volume
		if (music.Type != MusicType.MUS_MOD)
		{
			s_musicInternalVolume = music.fading == MixFading.MIX_FADING_IN ? 0 : s_musicVolume;
		}

		// Set up for playback
		if (!music.Decoder.Start(s_frequency, s_channels))
		{
			s_musicPlaying = null;
			return -1;
		}
		return 0;
	}

	private static void MusicInternalHalt()
	{
		var music = s_musicPlaying!;
		music.Decoder.Stop();
		music.fading = MixFading.MIX_NO_FADING;
		s_musicPlaying = null;
	}

	private static bool MusicInternalPlaying() => s_musicPlaying != null && s_musicPlaying.Decoder.IsPlaying;
}
