namespace Acom.Tests.Engine;

// Replaces the static clock and uses the static game pointer and Options.
[Collection(nameof(OptionsTests))]
public sealed class FlcPlayerTests : IDisposable
{
	private const int W = 8, H = 4;
	private readonly Func<uint> _oldSource = SDLTimer.Source;
	private uint _now = 1000;

	public FlcPlayerTests()
	{
		SDLTimer.Source = () => _now;
		FlcPlayer.ResetFrameClock();
	}

	public void Dispose()
	{
		SDLTimer.Source = _oldSource;
		FlcPlayer.ResetFrameClock();
	}

	/// <summary>Little-endian writer for building FLC files.</summary>
	private sealed class Flc
	{
		private readonly List<byte> _data = [];
		public int Count => _data.Count;
		public Flc U8(params int[] values) { foreach (var v in values) _data.Add((byte)v); return this; }
		public Flc U16(int v) => U8(v & 0xFF, (v >> 8) & 0xFF);
		public Flc U32(int v) => U16(v & 0xFFFF).U16((v >> 16) & 0xFFFF);
		public Flc Bytes(byte[] b) { _data.AddRange(b); return this; }
		public byte[] ToArray() => [.. _data];
	}

	private static byte[] Chunk(int type, Flc body)
	{
		var data = body.ToArray();
		return new Flc().U32(6 + data.Length).U16(type).Bytes(data).ToArray();
	}

	private static byte[] Frame(params byte[][] chunks)
	{
		int size = 16 + chunks.Sum(c => c.Length);
		var f = new Flc().U32(size).U16(0xF1FA).U16(chunks.Length).U16(0).U16(0).U16(0).U16(0);
		foreach (var c in chunks)
			f.Bytes(c);
		return f.ToArray();
	}

	private static byte[] AudioChunk(int sampleRate, byte[] pcm)
		=> new Flc().U32(pcm.Length).U16(0xAAAA).U16(0).U16(sampleRate).U16(1).U16(10).U16(0).Bytes(pcm).ToArray();

	/// <summary>A 8x4 FLC: palette + byte run, a line delta, a word delta (with prefix and audio chunks between).</summary>
	private static byte[] BuildFlc(int speed, byte[]? audio = null)
	{
		var colors = Chunk(0x04, new Flc().U16(2)
			.U8(0, 2, 10, 20, 30, 40, 50, 60) // colors 0-1
			.U8(3, 1, 70, 80, 90));          // skip is added to the previous count + 1: color 6
		var brun = new Flc();
		for (int y = 0; y < H; ++y)
		{
			brun.U8(0); // packet count (ignored)
			if (y == 2)
				brun.U8(3, 5).U8(unchecked((byte)-5), 1, 2, 3, 4, 5); // run of 3, then 5 literal bytes
			else
				brun.U8(W, y + 1);
		}
		var lc = Chunk(0x0C, new Flc().U16(1).U16(1) // skip 1 line, 1 line
			.U8(2) // packets
			.U8(2, 2, 9, 9) // skip 2, copy 2
			.U8(1, unchecked((byte)-2), 7)); // skip 1, fill 2
		var ss2 = Chunk(0x07, new Flc().U16(2)
			.U16(0xFFFF) // skip one line (does not count)
			.U16(1).U8(0, 1, 0x11, 0x12) // y=1: one packet, one word
			.U16(0x802B).U16(1).U8(4, unchecked((byte)-1), 0x21, 0x22)); // y=2: last pixel 0x2B, fill one word at x=4
		// (a last pixel of 0x80 or more would be read as a line skip, see FlcPlayer.ReadS16)

		var file = new Flc();
		var frames = new Flc()
			.Bytes(new Flc().U32(6).U16(0xF100).ToArray()) // prefix chunk
			.Bytes(Frame(colors, Chunk(0x0F, brun)));
		if (audio != null)
			frames.Bytes(AudioChunk(11025, audio));
		frames.Bytes(Frame(lc)).Bytes(Frame(ss2));
		file.U32(128 + frames.Count).U16(0xAF12).U16(3).U16(W).U16(H).U16(8).U16(0).U32(speed);
		while (file.Count < 128)
			file.U8(0);
		return file.Bytes(frames.ToArray()).ToArray();
	}

	private static Game NewGame()
	{
		var game = new Game(new Screen(320, 200));
		Options.mute = true;
		return game;
	}

	private static byte[] Row(Surface s, int y) => Enumerable.Range(0, W).Select(x => s.GetPixel(x, y + (200 - H) / 2)).ToArray();

	[Fact]
	public void Decodes_frames_and_palette_at_the_header_speed()
	{
		var game = NewGame();
		var player = new FlcPlayer();
		Assert.True(player.Init(BuildFlc(50), null, game, false, 0, 0));
		player.Play(false);
		var s = player.Surface!;

		Assert.True(player.Update()); // the first frame is shown at once
		Assert.Equal(1, player.FrameCount);
		Assert.Equal(new Rgba(10, 20, 30), game.Screen.Palette[0]);
		Assert.Equal(new Rgba(40, 50, 60), game.Screen.Palette[1]);
		Assert.Equal(new Rgba(70, 80, 90), game.Screen.Palette[6]);
		Assert.Equal(new Rgba(70, 80, 90), s.Palette[6]);
		Assert.Equal([1, 1, 1, 1, 1, 1, 1, 1], Row(s, 0));
		Assert.Equal([5, 5, 5, 1, 2, 3, 4, 5], Row(s, 2));
		Assert.Equal([4, 4, 4, 4, 4, 4, 4, 4], Row(s, 3));

		_now += 49;
		Assert.True(player.Update()); // too early
		Assert.Equal(1, player.FrameCount);
		_now += 1;
		Assert.True(player.Update());
		Assert.Equal(2, player.FrameCount);
		Assert.Equal([2, 2, 9, 9, 2, 7, 7, 2], Row(s, 1));

		_now += 50;
		Assert.False(player.Update()); // last frame: shown, then finished
		Assert.Equal(3, player.FrameCount);
		Assert.Equal([0x11, 0x12, 9, 9, 2, 7, 7, 2], Row(s, 1));
		Assert.Equal([5, 5, 5, 1, 0x21, 0x22, 4, 5], Row(s, 2));
		Assert.Equal(0x2B, s.GetPixel(319, 2 + (200 - H) / 2)); // last pixel of the surface line
		Assert.False(player.WasSkipped());
		player.DeInit();
		Assert.Null(player.Surface);
	}

	[Fact]
	public void Skip_last_frame_leaves_it_undecoded()
	{
		var game = NewGame();
		var player = new FlcPlayer();
		Assert.True(player.Init(BuildFlc(0), null, game, false, 0, 0));
		player.Play(true);
		while (player.Update())
		{
		}
		Assert.Equal(2, player.FrameCount);
		Assert.Equal([2, 2, 9, 9, 2, 7, 7, 2], Row(player.Surface!, 1));
	}

	[Fact]
	public void Internal_audio_sets_the_frame_delay()
	{
		var game = NewGame();
		var player = new FlcPlayer();
		Assert.True(player.Init(BuildFlc(50, new byte[1102]), null, game, true, 0, 0));
		player.Play(false);
		Assert.True(player.Update());
		Assert.Equal(1, player.FrameCount);
		_now += 50;
		Assert.True(player.Update()); // 1000 / (11025 / 1102) = 100 ms per frame, not the header speed
		Assert.Equal(1, player.FrameCount);
		_now += 50;
		Assert.True(player.Update());
		Assert.Equal(2, player.FrameCount);
		player.DeInit();
	}

	[Fact]
	public void Key_press_skips_and_delay_waits()
	{
		var game = NewGame();
		var player = new FlcPlayer();
		Assert.True(player.Init(BuildFlc(10), null, game, false, 0, 0));
		player.Play(false);
		Assert.True(player.Update());
		player.HandleEvent(SDL_Event.MouseMotion(1, 1));
		Assert.True(player.IsPlaying);
		player.HandleEvent(SDL_Event.KeyDown(SDLKey.SDLK_SPACE));
		Assert.False(player.Update());
		Assert.True(player.WasSkipped());
		player.DeInit();

		var second = new FlcPlayer();
		Assert.True(second.Init(BuildFlc(10), null, game, false, 0, 0));
		second.Play(false);
		while (second.Update())
			_now += 10;
		second.Delay(100);
		Assert.True(second.Update());
		_now += 100;
		Assert.False(second.Update());
	}

	[Fact]
	public void Rejects_files_that_are_not_flics()
	{
		var game = NewGame();
		var data = BuildFlc(10);
		data[4] = 0x12;
		data[5] = 0x34;
		Assert.False(new FlcPlayer().Init(data, null, game, false, 0, 0));
	}
}
