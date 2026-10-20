using System.Runtime.InteropServices;

namespace Acom.Tests.Engine;

/// <summary>Audio output for tests: accepts any format and plays nothing; the tests pull the signal with Mixer.Mix.</summary>
internal sealed class TestAudioDevice : IAudioDevice
{
	public (int Frequency, int Channels, int Samples)? Opened { get; private set; }

	public bool Open(int frequency, int channels, int samples)
	{
		Opened = (frequency, channels, samples);
		return true;
	}

	public void Close() => Opened = null;
}

/// <summary>Sound files built the same way as in port/tools/golden/mixer.cpp.</summary>
internal static class TestSounds
{
	public static byte[] Wav(int rate, int channels, int bits, byte[] data)
	{
		using var ms = new MemoryStream();
		using (var w = new BinaryWriter(ms))
		{
			w.Write("RIFF"u8);
			w.Write(36 + data.Length);
			w.Write("WAVE"u8);
			w.Write("fmt "u8);
			w.Write(16);
			w.Write((short)1);
			w.Write((short)channels);
			w.Write(rate);
			w.Write(rate * channels * bits / 8);
			w.Write((short)(channels * bits / 8));
			w.Write((short)bits);
			w.Write("data"u8);
			w.Write(data.Length);
			w.Write(data);
		}
		return ms.ToArray();
	}

	public static byte[] Mono8(int n, int rate = 11025)
	{
		var d = new byte[n];
		for (int i = 0; i < n; ++i)
			d[i] = (byte)(128 + ((i * 37) % 200) - 100);
		return Wav(rate, 1, 8, d);
	}

	public static byte[] Stereo16(int n)
	{
		var d = new byte[n * 4];
		for (int i = 0; i < n; ++i)
		{
			short l = (short)(i * 300 - 9000), r = (short)(20000 - i * 450);
			BitConverter.TryWriteBytes(d.AsSpan(i * 4), l);
			BitConverter.TryWriteBytes(d.AsSpan(i * 4 + 2), r);
		}
		return Wav(11025, 2, 16, d);
	}
}

[Collection(nameof(OptionsTests))] // the mixer is global state; Game reads Options
public sealed class MixerTests : IDisposable
{
	private readonly TestAudioDevice _device = new();

	public MixerTests()
	{
		Mixer.Device = _device;
	}

	public void Dispose()
	{
		Mixer.HookMusic(null, null);
		Mixer.HaltMusic();
		Mixer.HaltChannel(-1);
		Mixer.ReserveChannels(0);
		while (Mixer.QuerySpec(out _, out _, out _) != 0)
			Mixer.CloseAudio();
		Mixer.Device = null;
	}

	private static MixChunk Load(byte[] wav) => Mixer.LoadWAV(new MemoryStream(wav)) ?? throw new InvalidOperationException(Mixer.GetError());

	private static string Hex(short[] block) => string.Concat(block.Select(s => ((ushort)s).ToString("x4", System.Globalization.CultureInfo.InvariantCulture)));

	[Fact]
	public void Opening_fails_without_audio_output()
	{
		Mixer.Device = null;
		Assert.Equal(-1, Mixer.OpenAudio(22050, Mixer.MIX_DEFAULT_FORMAT, 2, 1024));
		Assert.Equal("No audio device available", Mixer.GetError());
		Assert.Null(Mixer.LoadWAV(new MemoryStream(TestSounds.Mono8(10))));
	}

	[Fact]
	public void A_second_open_with_the_same_format_only_counts_up()
	{
		Assert.Equal(0, Mixer.OpenAudio(22050, Mixer.MIX_DEFAULT_FORMAT, 2, 1024));
		Assert.Equal(0, Mixer.OpenAudio(11025, Mixer.MIX_DEFAULT_FORMAT, 2, 512));
		Assert.Equal(2, Mixer.QuerySpec(out int frequency, out _, out int channels));
		Assert.Equal((22050, 2), (frequency, channels));
		Mixer.CloseAudio();
		Assert.Equal(1, Mixer.QuerySpec(out _, out _, out _));
		Assert.Equal((22050, 2, 1024), _device.Opened);
		Mixer.CloseAudio();
		Assert.Null(_device.Opened);
	}

	[Fact]
	public void Game_sets_up_the_mixer_like_the_original()
	{
		var (rate, chunk, bits) = (Options.audioSampleRate, Options.audioChunkSize, Options.audioBitDepth);
		var (sound, music, ui, mute) = (Options.soundVolume, Options.musicVolume, Options.uiVolume, Options.mute);
		try
		{
			Options.audioSampleRate = 22050;
			Options.audioChunkSize = 512; // raised to the minimum for the rate
			Options.audioBitDepth = 16;
			Options.soundVolume = 128;
			Options.musicVolume = 64;
			Options.uiVolume = 32;
			var game = new Game(new Screen(4, 4));
			Assert.False(Options.mute);
			Assert.Equal((22050, 2, 1024), _device.Opened);
			Assert.Equal(1024, Options.audioChunkSize);
			Assert.Equal(Game.MIX_CHANNELS, Mixer.AllocateChannels(-1));
			// reserved: 0-4, UI group 0: channels 1-2
			var click = Load(TestSounds.Mono8(10));
			Assert.Equal(1, SDLMixer.Mix_GroupAvailable(0));
			Assert.Equal(5, Mixer.PlayChannel(-1, click, 0));
			Assert.Equal(-1, Mixer.PlayChannel(-1, Load(TestSounds.Wav(11025, 1, 8, [])), 0)); // empty chunk

			Assert.Equal([128, 10, 10, 64, 128, 128], Enumerable.Range(0, 6).Select(i => Mixer.Volume(i, -1)).ToArray());
			Assert.Equal(29, Mixer.VolumeMusic(-1));
			// the sound volume also applies to the UI channels when ui is left unchanged
			game.SetVolume(64, -1, -1);
			Assert.Equal([29, 29, 29, 14, 29, 29], Enumerable.Range(0, 6).Select(i => Mixer.Volume(i, -1)).ToArray());
			Assert.Equal(29, Mixer.VolumeMusic(-1));
		}
		finally
		{
			(Options.audioSampleRate, Options.audioChunkSize, Options.audioBitDepth) = (rate, chunk, bits);
			(Options.soundVolume, Options.musicVolume, Options.uiVolume, Options.mute) = (sound, music, ui, mute);
		}
	}

	[Fact]
	public void Fade_out_lowers_the_volume_until_the_channel_stops()
	{
		Assert.Equal(0, Mixer.OpenAudio(1000, Mixer.AUDIO_S16LSB, 1, 10)); // 10 ms per block
		Mixer.PlayChannel(0, Load(TestSounds.Wav(1000, 1, 16, MemoryMarshal.AsBytes(Enumerable.Repeat((short)10000, 10).ToArray().AsSpan()).ToArray())), -1);
		Assert.Equal(1, Mixer.FadeOutChannel(0, 50));
		var levels = new List<int>();
		var block = new short[10];
		for (int i = 0; i < 7; ++i)
		{
			Mixer.Mix(block);
			levels.Add(block[9]);
		}
		Assert.Equal([10000, 7968, 5937, 3984, 1953, 0, 0], levels.ToArray());
		Assert.Equal(0, Mixer.Playing(0));
		Assert.Equal(Mixer.MIX_MAX_VOLUME, Mixer.Volume(0, -1)); // restored
	}

	[Fact]
	public void Music_fade_out_steps_down_per_block_and_halts()
	{
		Assert.Equal(0, Mixer.OpenAudio(1000, Mixer.AUDIO_S16LSB, 1, 10));
		var music = Mixer.LoadMUS(new MemoryStream(TestSounds.Wav(1000, 1, 16, MemoryMarshal.AsBytes(Enumerable.Repeat((short)12800, 100).ToArray().AsSpan()).ToArray())));
		Assert.Equal(0, Mixer.PlayMusic(music, -1));
		Assert.Equal(1, Mixer.FadeOutMusic(40)); // 4 steps of 10 ms
		var levels = new List<int>();
		var block = new short[10];
		for (int i = 0; i < 6; ++i)
		{
			Mixer.Mix(block);
			levels.Add(block[0]);
		}
		Assert.Equal([9600, 6400, 3200, 0, 0, 0], levels.ToArray());
		Assert.Equal(0, Mixer.PlayingMusic());
	}

	/// <summary>Replays port/tools/golden/mixer.cpp, which ran the same calls against SDL_mixer 1.2.</summary>
	[Fact]
	public void Mixing_matches_SDL_mixer()
	{
		const int RATE = 11025, CHANNELS = 2, SAMPLES = 64;
		string[] golden = Golden.ReadLines("mixer.txt");
		int line = 0;
		void Expect(string actual) => Assert.Equal(golden[line++], actual);
		void Capture(string name, int n)
		{
			for (int i = 0; i < n; ++i)
			{
				var block = new short[SAMPLES * CHANNELS];
				Mixer.Mix(block);
				Expect($"{name} {Hex(block)}");
			}
			Expect($"state {name} {Mixer.Playing(-1)} {Mixer.PlayingMusic()}");
		}

		Assert.Equal(0, Mixer.OpenAudio(RATE, Mixer.AUDIO_S16LSB, CHANNELS, SAMPLES));
		Mixer.AllocateChannels(16);
		Mixer.ReserveChannels(5);
		Mixer.GroupChannels(1, 2, 0);
		MixChunk a = Load(TestSounds.Mono8(150)), b = Load(TestSounds.Stereo16(100)), c = Load(TestSounds.Mono8(40));

		int played = Mixer.PlayChannel(Mixer.GroupAvailable(0), a, 0);
		Expect($"channel {played} {Mixer.GroupAvailable(0)}");
		played = Mixer.PlayChannel(-1, b, 0);
		Expect($"channel {played} {Mixer.GroupAvailable(0)}");
		played = Mixer.PlayChannel(3, c, 2);
		Expect($"channel {played} {Mixer.GroupAvailable(-1)}");
		Mixer.Volume(-1, 100);
		Mixer.Volume(1, 64);
		Mixer.Volume(3, 50);
		Expect($"position {Mixer.SetPosition(5, 280, 100)}");
		Capture("mix", 6);

		foreach (int angle in new[] { 0, 45, 80, 90, 135, 180, 200, 270, 300, 359 })
		{
			Mixer.PlayChannel(6, b, 0);
			Expect($"position {Mixer.SetPosition(6, (short)angle, (byte)(angle / 3))}");
			Capture("angle", 1);
			Mixer.HaltChannel(6);
		}

		int phase = 0;
		Mixer.HookMusic((_, stream, len) =>
		{
			var s = MemoryMarshal.Cast<byte, short>(stream);
			for (int i = 0; i < len / 2; ++i, ++phase)
				s[i] = (short)((phase % 50) * 200 - 5000);
		}, null);
		Mixer.VolumeMusic(10);
		Mixer.PlayChannel(-1, c, 0);
		Capture("hook", 2);
		Mixer.HookMusic(null, null);

		var music = Mixer.LoadMUS(new MemoryStream(TestSounds.Stereo16(90)));
		Mixer.VolumeMusic(96);
		int rc = Mixer.PlayMusic(music, 2);
		Expect($"music {rc} {(int)Mixer.GetMusicType(null)}");
		Capture("music", 4);
		Mixer.HaltMusic();

		Mixer.PlayChannel(-1, a, -1);
		Mixer.PlayChannel(-1, b, -1);
		Mixer.HaltChannel(-1);
		Capture("halt", 1);
		Assert.Equal(golden.Length, line);
	}

	[Fact]
	public void Wav_decoder_rejects_what_SDL_rejects()
	{
		Assert.Null(AudioDecoder.DecodeSound([1, 2, 3], out string? error));
		Assert.Equal("Unrecognized sound file type", error);
		byte[] adpcm = TestSounds.Wav(11025, 1, 8, [1, 2]);
		adpcm[20] = 2; // encoding
		Assert.Null(AudioDecoder.DecodeWav(adpcm, out error));
		Assert.Equal("Unknown WAVE data format: 0x0002", error);
		Assert.Null(Mixer.LoadMUS(new MemoryStream([0, 1, 2, 3])));
		Assert.Equal("Unrecognized music format", Mixer.GetError());
	}
}
