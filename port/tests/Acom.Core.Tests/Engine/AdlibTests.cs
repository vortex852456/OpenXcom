using System.Globalization;

namespace Acom.Tests.Engine;

/// <summary>
/// Golden tests of the Adlib emulation against src/Engine/Adlib (port/tools/golden/adlib.cpp):
/// register writes rendered by the YM3812 emulator, and songs played through AdlibMusic.Player.
/// </summary>
[Collection(nameof(OptionsTests))] // global player/chip state and Options
public class AdlibTests
{
	private static ulong Fnv(ReadOnlySpan<byte> data)
	{
		ulong h = 14695981039346656037UL;
		foreach (byte b in data)
		{
			h ^= b;
			h *= 1099511628211UL;
		}
		return h;
	}

	private static int Int(string s) => int.Parse(s, CultureInfo.InvariantCulture);

	private static byte[] Hex(string s) => Convert.FromHexString(s);

	[Fact]
	public void Register_writes_render_like_the_original()
	{
		FM_OPL? chip = null;
		int renders = 0;
		try
		{
			foreach (string line in Golden.ReadLines("adlib.txt"))
			{
				string[] f = Golden.Fields(line);
				if (f[0] == "song")
					break;
				switch (f[0])
				{
					case "noise":
						Fmopl.NoiseState = new Rng.RandomState(ulong.Parse(f[1], CultureInfo.InvariantCulture));
						break;
					case "create":
						chip = Fmopl.OPLCreate(Fmopl.OPL_TYPE_YM3812, 3579545, Int(f[1]));
						break;
					case "write":
						Fmopl.OPLWrite(chip!, 0, Int(f[1]));
						Assert.Equal(Int(f[3]), Fmopl.OPLWrite(chip!, 1, Int(f[2])));
						break;
					case "read":
						Assert.Equal(Int(f[2]), Fmopl.OPLRead(chip!, Int(f[1])));
						break;
					case "timer":
						Assert.Equal(Int(f[2]), Fmopl.OPLTimerOver(chip!, Int(f[1])));
						break;
					case "render":
						{
							int length = Int(f[1]);
							var buf = new short[length];
							Fmopl.YM3812UpdateOne(chip!, buf, length, Int(f[2]), float.Parse(f[3], CultureInfo.InvariantCulture));
							byte[] expected = Hex(f[4]);
							for (int i = 0; i < length; ++i)
							{
								short e = (short)((expected[i * 2] << 8) | expected[i * 2 + 1]);
								Assert.True(e == buf[i], $"render #{renders} sample {i}: expected {e}, got {buf[i]}");
							}
							++renders;
						}
						break;
					case "destroy":
						Fmopl.OPLDestroy(chip);
						chip = null;
						break;
					default:
						Assert.Fail($"unknown golden line: {f[0]}");
						break;
				}
			}
		}
		finally
		{
			Fmopl.OPLDestroy(chip);
			Fmopl.NoiseState = null;
		}
		Assert.Equal(8, renders);
	}

	[Fact]
	public void Songs_play_like_the_original()
	{
		int rate = Options.audioSampleRate, musicVolume = Options.musicVolume;
		bool mute = Options.mute, alwaysLoop = Options.musicAlwaysLoop;
		AdlPlayer.ResetForTest();
		AdlibMusic.ResetDelayForTest();
		int blocks = 0;
		try
		{
			Options.mute = false;
			Mixer.VolumeMusic(Mixer.MIX_MAX_VOLUME);
			AdlibMusic? music = null;
			bool inSongs = false;
			foreach (string line in Golden.ReadLines("adlib.txt"))
			{
				string[] f = Golden.Fields(line);
				switch (f[0])
				{
					case "song":
						inSongs = true;
						Options.audioSampleRate = Int(f[1]);
						Options.musicVolume = Int(f[2]);
						Options.musicAlwaysLoop = f[4] == "1";
						music = new AdlibMusic(float.Parse(f[3], CultureInfo.InvariantCulture));
						music.Load(new MemoryStream(Hex(f[5])));
						music.Play();
						break;
					case "block":
						{
							var buf = new byte[Int(f[1])];
							AdlibMusic.Player(music, buf, buf.Length);
							Assert.True(ulong.Parse(f[2], NumberStyles.HexNumber, CultureInfo.InvariantCulture) == Fnv(buf), $"block {blocks}");
							Assert.Equal(f[3] == "1", AdlPlayer.func_is_music_playing());
							Assert.Equal(Int(f[4]), AdlPlayer.func_get_polyphony());
							++blocks;
						}
						break;
					case "fade":
						AdlPlayer.func_fade();
						break;
					default:
						Assert.False(inSongs, $"unexpected golden line: {f[0]}");
						break;
				}
			}
		}
		finally
		{
			Mixer.HookMusic(null, null);
			AdlPlayer.ResetForTest();
			AdlibMusic.ResetDelayForTest();
			Options.audioSampleRate = rate;
			Options.musicVolume = musicVolume;
			Options.mute = mute;
			Options.musicAlwaysLoop = alwaysLoop;
		}
		Assert.Equal(64 + 48, blocks);
	}
}
