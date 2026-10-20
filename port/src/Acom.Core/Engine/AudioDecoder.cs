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
// Not a port of an OpenXcom unit: decoders behind Mixer (SDL_mixer's WAV/Ogg/MIDI loaders and SDL_ConvertAudio).

using System.Buffers.Binary;
using MeltySynth;
using NVorbis;

namespace Acom;

/// <summary>Decoded PCM audio: interleaved float samples in [-1, 1].</summary>
internal sealed class PcmData
{
	public PcmData(float[] samples, int channels, int rate)
	{
		Samples = samples;
		Channels = channels;
		Rate = rate;
	}

	public float[] Samples { get; }
	public int Channels { get; }
	public int Rate { get; }

	/// <summary>Converts to interleaved signed 16-bit samples in the given format.</summary>
	public short[] Convert(int rate, int channels)
	{
		int offset = 0;
		var samples = Samples;
		int srcChannels = Channels;
		var converter = new PcmConverter(buf =>
		{
			int n = Math.Min(buf.Length, samples.Length - offset);
			n -= n % srcChannels;
			Array.Copy(samples, offset, buf, 0, n);
			offset += n;
			return n;
		}, Channels, Rate, channels, rate);
		long frames = (long)Math.Ceiling(samples.Length / srcChannels * ((double)rate / Rate)) + 2;
		var result = new short[frames * channels];
		int written = converter.Read(result);
		Array.Resize(ref result, written);
		return result;
	}
}

/// <summary>
/// Streaming sample format conversion (SDL_ConvertAudio): channel mapping (mono is duplicated,
/// stereo averaged to mono) and linear interpolation between sample rates. With equal rates and
/// channels, 16-bit input passes through unchanged.
/// </summary>
internal sealed class PcmConverter
{
	/// <summary>Fills the buffer with interleaved source samples; returns their number, 0 at the end.</summary>
	public delegate int SourceReader(float[] buffer);

	private const int ReadFrames = 4096;

	private readonly SourceReader _read;
	private readonly int _srcChannels;
	private readonly int _dstChannels;
	private readonly double _step;
	private readonly float[] _readBuf;
	private float[] _frames;
	private int _count;
	private double _pos;
	private bool _eof;

	public PcmConverter(SourceReader read, int srcChannels, int srcRate, int dstChannels, int dstRate)
	{
		_read = read;
		_srcChannels = Math.Max(1, srcChannels);
		_dstChannels = Math.Max(1, dstChannels);
		_step = (double)srcRate / dstRate;
		_readBuf = new float[ReadFrames * _srcChannels];
		_frames = new float[(ReadFrames + 2) * _dstChannels];
	}

	/// <summary>Whether all source samples have been converted.</summary>
	public bool Finished => _eof && (int)_pos >= _count;

	/// <summary>Converts the next samples; returns how many were written (fewer only at the end).</summary>
	public int Read(Span<short> dst)
	{
		int written = 0;
		int dc = _dstChannels;
		while (written + dc <= dst.Length)
		{
			int i = (int)_pos;
			while (i + 1 >= _count && !_eof)
			{
				Fill();
				i = (int)_pos;
			}
			if (i >= _count)
				break;
			float frac = (float)(_pos - i);
			int j = i + 1 < _count ? i + 1 : i;
			for (int c = 0; c < dc; ++c)
			{
				float a = _frames[i * dc + c];
				float b = _frames[j * dc + c];
				dst[written++] = ToShort(a + (b - a) * frac);
			}
			_pos += _step;
		}
		return written;
	}

	private static short ToShort(float v)
	{
		float s = MathF.Round(v * 32768f);
		if (s >= 32767f)
			return 32767;
		if (s <= -32768f)
			return -32768;
		return (short)s;
	}

	private void Fill()
	{
		// drop the frames before the current one
		int keep = Math.Min((int)_pos, _count);
		Array.Copy(_frames, keep * _dstChannels, _frames, 0, (_count - keep) * _dstChannels);
		_count -= keep;
		_pos -= keep;

		int n = _read(_readBuf);
		if (n <= 0)
		{
			_eof = true;
			return;
		}
		int frames = n / _srcChannels;
		int need = (_count + frames) * _dstChannels;
		if (need > _frames.Length)
			Array.Resize(ref _frames, need);
		int sc = _srcChannels, dc = _dstChannels;
		for (int f = 0; f < frames; ++f)
		{
			int s = f * sc;
			int d = (_count + f) * dc;
			if (sc == dc)
			{
				Array.Copy(_readBuf, s, _frames, d, dc);
			}
			else if (dc == 1)
			{
				float sum = 0;
				for (int c = 0; c < sc; ++c)
					sum += _readBuf[s + c];
				_frames[d] = sum / sc;
			}
			else
			{
				for (int c = 0; c < dc; ++c)
					_frames[d + c] = _readBuf[s + (c < sc ? c : sc - 1)];
			}
		}
		_count += frames;
	}
}

/// <summary>File format detection and decoding for <see cref="Mixer"/>.</summary>
internal static class AudioDecoder
{
	private static bool Magic(byte[] data, int offset, string magic)
	{
		if (data.Length < offset + magic.Length)
			return false;
		for (int i = 0; i < magic.Length; ++i)
		{
			if (data[offset + i] != magic[i])
				return false;
		}
		return true;
	}

	private static bool IsWave(byte[] data) => Magic(data, 0, "RIFF") && Magic(data, 8, "WAVE");

	/// <summary>Decodes a sound effect (Mix_LoadWAV_RW): WAV (8/16-bit PCM) or Ogg Vorbis.</summary>
	public static PcmData? DecodeSound(byte[] data, out string? error)
	{
		if (IsWave(data))
			return DecodeWav(data, out error);
		if (Magic(data, 0, "OggS"))
			return DecodeVorbis(data, out error);
		error = "Unrecognized sound file type";
		return null;
	}

	/// <summary>Reads a RIFF WAVE file (SDL_LoadWAV_RW, PCM only; a truncated data chunk is cut instead of rejected).</summary>
	public static PcmData? DecodeWav(byte[] data, out string? error)
	{
		error = null;
		if (!IsWave(data))
		{
			error = "Unrecognized file type (not WAVE)";
			return null;
		}
		int p = 12;
		bool haveFmt = false;
		int encoding = 0, channels = 0, rate = 0, bits = 0;
		while (p + 8 <= data.Length)
		{
			uint length = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(p + 4));
			int body = p + 8;
			int available = (int)Math.Min(length, (uint)(data.Length - body));
			if (Magic(data, p, "fmt "))
			{
				if (available < 16)
				{
					error = "Complex WAVE files not supported";
					return null;
				}
				var fmt = data.AsSpan(body);
				encoding = BinaryPrimitives.ReadUInt16LittleEndian(fmt);
				channels = BinaryPrimitives.ReadUInt16LittleEndian(fmt[2..]);
				rate = BinaryPrimitives.ReadInt32LittleEndian(fmt[4..]);
				bits = BinaryPrimitives.ReadUInt16LittleEndian(fmt[14..]);
				haveFmt = true;
			}
			else if (Magic(data, p, "data"))
			{
				if (!haveFmt)
				{
					error = "Complex WAVE files not supported";
					return null;
				}
				if (encoding != 1)
				{
					error = $"Unknown WAVE data format: 0x{encoding:x4}";
					return null;
				}
				if ((bits != 8 && bits != 16) || channels < 1 || rate <= 0)
				{
					error = $"Unknown {bits}-bit PCM data format";
					return null;
				}
				int sampleSize = bits / 8;
				int count = available / sampleSize;
				count -= count % channels;
				var samples = new float[count];
				var pcm = data.AsSpan(body);
				if (bits == 8)
				{
					for (int i = 0; i < count; ++i)
						samples[i] = (pcm[i] - 128) / 128f;
				}
				else
				{
					for (int i = 0; i < count; ++i)
						samples[i] = BinaryPrimitives.ReadInt16LittleEndian(pcm[(i * 2)..]) / 32768f;
				}
				return new PcmData(samples, channels, rate);
			}
			p = body + (int)Math.Min(length, (uint)(data.Length - body));
		}
		error = haveFmt ? "Couldn't find data chunk" : "Complex WAVE files not supported";
		return null;
	}

#pragma warning disable CA1031 // decoders throw many exception types on broken data; all of them mean "cannot load"
	/// <summary>Decodes a whole Ogg Vorbis stream.</summary>
	public static PcmData? DecodeVorbis(byte[] data, out string? error)
	{
		error = null;
		try
		{
			using var reader = new VorbisReader(new MemoryStream(data, false), true);
			var samples = new List<float>();
			var buf = new float[4096 * reader.Channels];
			int n;
			while ((n = reader.ReadSamples(buf, 0, buf.Length)) > 0)
				samples.AddRange(buf.AsSpan(0, n));
			return new PcmData([.. samples], reader.Channels, reader.SampleRate);
		}
		catch (Exception e)
		{
			error = $"Ogg Vorbis: {e.Message}";
			return null;
		}
	}

	/// <summary>Detects the format of a music file and prepares its decoder (Mix_LoadMUS_RW).</summary>
	public static MixMusic? LoadMusic(byte[] data, out string? error)
	{
		error = null;
		if (IsWave(data))
		{
			var pcm = DecodeWav(data, out error);
			return pcm == null ? null : new MixMusic(MusicType.MUS_WAV, new WavMusic(pcm));
		}
		if (Magic(data, 0, "MThd"))
		{
			string? soundFont = MidiMusic.FindSoundFont();
			if (soundFont == null)
			{
				error = "MIDI support not available: no SoundFont found (set SDL_SOUNDFONTS)";
				return null;
			}
			try
			{
				var midi = new MidiFile(new MemoryStream(data, false));
				return new MixMusic(MusicType.MUS_MID, new MidiMusic(midi, soundFont));
			}
			catch (Exception e)
			{
				error = $"MIDI: {e.Message}";
				return null;
			}
		}
		if (Magic(data, 0, "OggS"))
		{
			try
			{
				using var reader = new VorbisReader(new MemoryStream(data, false), true);
				if (reader.Channels < 1 || reader.SampleRate <= 0)
				{
					error = "Ogg Vorbis: invalid stream";
					return null;
				}
			}
			catch (Exception e)
			{
				error = $"Ogg Vorbis: {e.Message}";
				return null;
			}
			return new MixMusic(MusicType.MUS_OGG, new OggMusic(data));
		}
		if (Magic(data, 0, "fLaC"))
		{
			error = "FLAC music is not supported";
			return null;
		}
		if ((data.Length >= 2 && data[0] == 0xFF && (data[1] & 0xF0) == 0xF0) || Magic(data, 0, "ID3"))
		{
			error = "MP3 music is not supported";
			return null;
		}
		error = "Unrecognized music format";
		return null;
	}

	/// <summary>WAV music, converted to the output format on start (WAVStream).</summary>
	private sealed class WavMusic(PcmData source) : IMusicDecoder
	{
		private short[] _pcm = [];
		private int _rate, _channels;
		private int _pos;
		private bool _active;

		public bool Start(int frequency, int channels)
		{
			if (_rate != frequency || _channels != channels)
			{
				_pcm = source.Convert(frequency, channels);
				_rate = frequency;
				_channels = channels;
			}
			_pos = 0;
			_active = true;
			return true;
		}

		public void Stop() => _active = false;

		public bool IsPlaying => _active && _pos < _pcm.Length;

		public int PlaySome(Span<short> stream, int volume)
		{
			int n = Math.Min(stream.Length, _pcm.Length - _pos);
			Mixer.MixAudio(stream[..n], _pcm.AsSpan(_pos, n), volume);
			_pos += n;
			return stream.Length - n;
		}
	}

	/// <summary>Ogg Vorbis music, decoded while playing.</summary>
	private sealed class OggMusic(byte[] data) : IMusicDecoder
	{
		private VorbisReader? _reader;
		private PcmConverter? _converter;
		private short[] _tmp = [];

		public bool Start(int frequency, int channels)
		{
			Stop();
			try
			{
				var reader = new VorbisReader(new MemoryStream(data, false), true);
				_reader = reader;
				_converter = new PcmConverter(buf => reader.ReadSamples(buf, 0, buf.Length), reader.Channels, reader.SampleRate, channels, frequency);
				return true;
			}
			catch (Exception e)
			{
				Stop();
				Mixer.SetError($"Ogg Vorbis: {e.Message}");
				return false;
			}
		}

		public void Stop()
		{
			_reader?.Dispose();
			_reader = null;
			_converter = null;
		}

		public bool IsPlaying => _converter != null && !_converter.Finished;

		public int PlaySome(Span<short> stream, int volume)
		{
			if (_tmp.Length < stream.Length)
				_tmp = new short[stream.Length];
			int n;
			try
			{
				n = _converter!.Read(_tmp.AsSpan(0, stream.Length));
			}
			catch (Exception e)
			{
				Logger.Warning($"Ogg Vorbis: {e.Message}");
				Stop();
				return stream.Length;
			}
			Mixer.MixAudio(stream[..n], _tmp.AsSpan(0, n), volume);
			return stream.Length - n;
		}
	}
#pragma warning restore CA1031
}

/// <summary>MIDI music rendered with a SoundFont synthesizer (instead of Timidity/native MIDI).</summary>
internal sealed class MidiMusic(MidiFile midi, string soundFontPath) : IMusicDecoder
{
	private static readonly Dictionary<string, SoundFont> s_soundFonts = new(StringComparer.Ordinal);

	private Synthesizer? _synth;
	private MidiFileSequencer? _sequencer;
	private float[] _left = [], _right = [];
	private short[] _tmp = [];
	private int _channels = 2;

	/// <summary>The SoundFont to use: <see cref="Mixer.SoundFontPath"/>, <c>SDL_SOUNDFONTS</c>, or a common system location.</summary>
	public static string? FindSoundFont()
	{
		var candidates = new List<string>();
		if (!string.IsNullOrEmpty(Mixer.SoundFontPath))
			candidates.Add(Mixer.SoundFontPath);
		string? env = Environment.GetEnvironmentVariable("SDL_SOUNDFONTS");
		if (!string.IsNullOrEmpty(env))
			candidates.AddRange(env.Split(';', StringSplitOptions.RemoveEmptyEntries));
		candidates.Add("/usr/share/sounds/sf2/FluidR3_GM.sf2");
		candidates.Add("/usr/share/soundfonts/FluidR3_GM.sf2");
		candidates.Add("/usr/share/soundfonts/default.sf2");
		candidates.Add("/usr/share/sounds/sf2/default-GM.sf2");
		return candidates.FirstOrDefault(File.Exists);
	}

#pragma warning disable CA1031 // SoundFont/synthesizer errors only disable the track
	public bool Start(int frequency, int channels)
	{
		Stop();
		try
		{
			if (!s_soundFonts.TryGetValue(soundFontPath, out var soundFont))
			{
				soundFont = new SoundFont(soundFontPath);
				s_soundFonts[soundFontPath] = soundFont;
			}
			if (_synth == null || _synth.SampleRate != frequency)
				_synth = new Synthesizer(soundFont, frequency);
			else
				_synth.Reset();
			_sequencer = new MidiFileSequencer(_synth);
			_sequencer.Play(midi, false);
			_channels = channels == 1 ? 1 : 2;
			return true;
		}
		catch (Exception e)
		{
			Mixer.SetError($"MIDI: {e.Message}");
			_sequencer = null;
			return false;
		}
	}
#pragma warning restore CA1031

	public void Stop()
	{
		_sequencer?.Stop();
		_sequencer = null;
	}

	public bool IsPlaying => _sequencer != null && !_sequencer.EndOfSequence;

	public int PlaySome(Span<short> stream, int volume)
	{
		int channels = _channels;
		int frames = stream.Length / channels;
		if (_left.Length < frames)
		{
			_left = new float[frames];
			_right = new float[frames];
			_tmp = new short[frames * channels];
		}
		_sequencer!.Render(_left.AsSpan(0, frames), _right.AsSpan(0, frames));
		for (int f = 0; f < frames; ++f)
		{
			if (channels == 1)
			{
				_tmp[f] = ToShort((_left[f] + _right[f]) * 0.5f);
			}
			else
			{
				_tmp[f * channels] = ToShort(_left[f]);
				_tmp[f * channels + 1] = ToShort(_right[f]);
			}
		}
		Mixer.MixAudio(stream, _tmp.AsSpan(0, frames * channels), volume);
		return 0;
	}

	private static short ToShort(float v)
	{
		float s = MathF.Round(v * 32768f);
		if (s >= 32767f)
			return 32767;
		if (s <= -32768f)
			return -32768;
		return (short)s;
	}
}
