
namespace Acom.Tests.Engine;

public class FontTests
{
	[Fact]
	public void Terminal_font_is_monospace_cp437()
	{
		var font = new Font();
		font.LoadTerminal();
		Assert.Equal((9, 16, 0), (font.Width, font.Height, font.Spacing));
		// 'A' is character 33 of the string, 32 characters per row (288 / 9)
		var a = font.GetChar('A');
		Assert.Equal((9, 16, 9, 16), (a.Crop.x, a.Crop.y, (int)a.Crop.w, (int)a.Crop.h));
		Assert.Equal(1, Enumerable.Range(0, 16).Max(y => Enumerable.Range(9, 9).Max(x => a.Surface!.GetPixel(x, y))));
		// unknown characters fall back to '?'
		Assert.Equal(font.GetChar('?').Crop.x, font.GetChar('é').Crop.x);
		var space = font.GetCharSize(' ');
		Assert.Equal((9, 16, 9, 16), (space.x, space.y, (int)space.w, (int)space.h));
	}

	[Fact]
	public void Proportional_glyphs_are_trimmed_to_their_pixels()
	{
		// 2 cells of 4x3: 'a' uses columns 1..2, 'b' is empty
		var yaml = new YamlRootNodeReader("width: 4\nheight: 3\nspacing: 1\nimages:\n  - file: f.gif\n    chars: ab\n", "font.dat");
		var font = new Font();
		string? requested = null;
		font.Load(yaml.ToBase(), f => { requested = f; return Glyphs(); });
		Assert.Equal("Language/f.gif", requested);
		var a = font.GetChar('a');
		Assert.Equal((1, 0, 2, 3), (a.Crop.x, a.Crop.y, (int)a.Crop.w, (int)a.Crop.h));
		var b = font.GetChar('b');
		Assert.Equal((-1, 1), (b.Crop.x, (int)b.Crop.w));
		Assert.Equal((3, 4), ((int)font.GetCharSize('a').w, (int)font.GetCharSize('a').h));
		Assert.Equal(1, (int)font.GetCharSize(Unicode.TOK_NBSP).w); // width / 4
		Assert.Equal(3, (int)font.GetCharSize('\t').w);
		Assert.Equal(2, (int)font.GetCharSize('\n').w);

		// crop blit draws the glyph's left column at the target position
		var dest = new Surface(4, 3, 0, 0, 1);
		a.X = 0;
		a.Y = 0;
		a.Blit(dest);
		Assert.Equal([5, 6, 0, 0], Enumerable.Range(0, 4).Select(x => dest.GetPixel(x, 1)));
	}

	[Fact]
	public void Fonts_of_the_repo_load()
	{
		var language = Path.Combine(LanguageTests.RepoRoot(), "bin", "common");
		var reader = YamlRootNodeReader.FromFile(Path.Combine(language, "Language", "Font.dat"));
		int count = 0;
		foreach (var fontReader in reader["fonts"].Children())
		{
			var font = new Font();
			font.Load(fontReader, f => File.ReadAllBytes(Path.Combine(language, f)));
			var a = font.GetChar('A');
			Assert.True(a.Crop.w > 0 && a.Crop.w <= font.Width, fontReader["id"].ReadVal<string>());
			++count;
		}
		Assert.True(count >= 2);
	}

	/// <summary>8x3 GIF: cell 'a' has pixels 5,6 in columns 1..2 of row 1, cell 'b' is empty.</summary>
	private static byte[] Glyphs()
	{
		// LZW stream of 24 pixels with code size 3 (clear=8, end=9): written as raw codes without compression gains
		byte[] pixels = [0, 0, 0, 0, 0, 0, 0, 0, 0, 5, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
		return TestGif.Create(8, 3, pixels);
	}
}

/// <summary>Minimal GIF writer for tests (clear code before every pixel keeps the code size constant).</summary>
internal static class TestGif
{
	public static byte[] Create(int width, int height, byte[] pixels)
	{
		var codes = new List<int>();
		foreach (var p in pixels)
		{
			codes.Add(8); // clear
			codes.Add(p);
		}
		codes.Add(9); // end
		var data = new List<byte>();
		int acc = 0, bits = 0;
		foreach (var code in codes)
		{
			acc |= code << bits;
			bits += 4;
			while (bits >= 8)
			{
				data.Add((byte)acc);
				acc >>= 8;
				bits -= 8;
			}
		}
		if (bits > 0)
			data.Add((byte)acc);
		var gif = new List<byte>();
		gif.AddRange("GIF89a"u8.ToArray());
		gif.AddRange([(byte)width, (byte)(width >> 8), (byte)height, (byte)(height >> 8), 0xF2, 0, 0]); // global table, 8 colours
		for (int i = 0; i < 8; ++i)
			gif.AddRange([(byte)(i * 30), (byte)(i * 30), (byte)(i * 30)]);
		gif.AddRange([(byte)',', 0, 0, 0, 0, (byte)width, (byte)(width >> 8), (byte)height, (byte)(height >> 8), 0, 3]);
		for (int i = 0; i < data.Count; i += 255)
		{
			int n = Math.Min(255, data.Count - i);
			gif.Add((byte)n);
			gif.AddRange(data.GetRange(i, n));
		}
		gif.Add(0);
		gif.Add((byte)';');
		return [.. gif];
	}
}
