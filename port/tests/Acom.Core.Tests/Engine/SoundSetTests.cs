namespace Acom.Tests.Engine;

[Collection(nameof(OptionsTests))] // opens the global mixer
public sealed class SoundSetTests : IDisposable
{
	public SoundSetTests()
	{
		Mixer.Device = new TestAudioDevice();
		// mono at the rate of the converted sounds, so the chunks show the 8-bit samples 1:1
		Assert.Equal(0, Mixer.OpenAudio(11025, Mixer.AUDIO_S16LSB, 1, 256));
	}

	public void Dispose()
	{
		while (Mixer.QuerySpec(out _, out _, out _) != 0)
			Mixer.CloseAudio();
		Mixer.Device = null;
	}

	/// <summary>Builds a CAT archive: offset/size table, then the items.</summary>
	private static CatFile Cat(params byte[][] items)
	{
		var data = new List<byte>();
		int offset = items.Length * 8;
		foreach (var item in items)
		{
			data.AddRange(BitConverter.GetBytes(offset));
			data.AddRange(BitConverter.GetBytes(item.Length));
			offset += item.Length;
		}
		foreach (var item in items)
			data.AddRange(item);
		return new CatFile(new MemoryStream([.. data]), "TEST.CAT");
	}

	/// <summary>A CAT item: name chunk, then the sound.</summary>
	private static byte[] Item(string name, byte[] sound) => [(byte)name.Length, .. System.Text.Encoding.ASCII.GetBytes(name), .. sound];

	/// <summary>The original raw format: 5 header bytes, the samples, one trailing byte.</summary>
	private static byte[] Raw(byte[] samples) => [1, 2, 3, 4, 5, .. samples, 0xEE];

	private static short S16(int u8) => (short)(((u8 & 0xff) - 128) * 256);

	private static short[] Samples(Sound? sound) => sound!.Chunk!.Abuf;

	/// <summary>8 kHz -> 11025 Hz by picking the nearest older sample (16.16 fixed point step), as SoundSet does.</summary>
	private static short[] Resampled(byte[] src, Func<int, int> scale)
	{
		var result = new List<short>();
		const uint step16 = (8000 << 16) / 11025;
		for (uint offset16 = 0; (offset16 >> 16) < src.Length; offset16 += step16)
			result.Add(S16(scale(src[offset16 >> 16])));
		return [.. result];
	}

	[Fact]
	public void Raw_ufo_samples_are_scaled_and_resampled_to_11khz()
	{
		byte[] raw = new byte[40];
		for (int i = 0; i < raw.Length; ++i)
			raw[i] = (byte)((i * 7) % 64); // 6-bit samples
		var set = new SoundSet();
		set.LoadCat(Cat(Item("BANG", Raw(raw))));

		short[] expected = Resampled(raw, s => s * 4);
		Assert.Equal(56, expected.Length);
		Assert.Equal(expected, Samples(set.GetSound(0)));
	}

	[Fact]
	public void Tftd_samples_lose_their_sign_and_go_behind_the_last_sound()
	{
		byte[] raw = [0x00, 0x7F, 0x80, 0xFF, 0x10, 0x20, 0x30, 0x40, 0x50, 0x60];
		var set = new SoundSet();
		set.AddSound(4);
		set.LoadCatByIndex(Cat(Item("A", Raw(raw))), 0, true);
		Assert.Equal(2, set.TotalSounds);
		Assert.Equal(raw.Select(s => S16(s + 128)).ToArray(), Samples(set.GetSound(1)));
	}

	[Fact]
	public void Wav_items_are_kept_or_resampled_by_their_rate()
	{
		byte[] pcm = [10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110];
		var set = new SoundSet();
		set.LoadCat(Cat(Item("W1", TestSounds.Wav(11025, 1, 8, pcm)), Item("W2", TestSounds.Wav(8000, 1, 8, pcm))));
		Assert.Equal(pcm.Select(s => S16(s)).ToArray(), Samples(set.GetSound(0)));
		// below 11025 Hz: the data after the 44-byte header is resampled like raw samples (without scaling)
		short[] expected = Resampled(pcm, s => s);
		Assert.Equal([S16(10), S16(10), S16(20), S16(30), S16(30)], expected[..5]);
		Assert.Equal(expected, Samples(set.GetSound(1)));
	}

	[Fact]
	public void Wav_items_with_missing_data_get_a_fixed_header()
	{
		byte[] wav = TestSounds.Wav(11025, 1, 8, [1, 2, 3, 4, 5, 6, 7, 8]);
		var set = new SoundSet();
		set.LoadCat(Cat(Item("CUT", wav[..^3])));
		Assert.Equal([S16(1), S16(2), S16(3), S16(4), S16(5)], Samples(set.GetSound(0)));
	}

	[Fact]
	public void Short_or_missing_items_give_empty_sounds()
	{
		var set = new SoundSet();
		var cat = Cat(Item("X", [1, 2, 3]));
		set.LoadCatByIndex(cat, 0);
		set.LoadCatByIndex(cat, 7);
		Assert.Equal(2, set.TotalSounds);
		Assert.False(set.GetSound(0)!.IsLoaded);
		Assert.False(set.GetSound(7)!.IsLoaded);
		Assert.Null(set.GetSound(1));
	}

	[Fact]
	public void Shared_sounds_are_unlimited_until_set()
	{
		var set = new SoundSet();
		Assert.Equal(int.MaxValue, set.MaxSharedSounds);
		set.MaxSharedSounds = -5;
		Assert.Equal(0, set.MaxSharedSounds);
		set.MaxSharedSounds = 12;
		Assert.Equal(12, set.MaxSharedSounds);
	}
}
