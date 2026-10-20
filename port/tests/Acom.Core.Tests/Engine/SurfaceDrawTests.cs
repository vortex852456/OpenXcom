using System.Globalization;

namespace Acom.Tests.Engine;

/// <summary>
/// Surface.DrawLine/DrawCircle/DrawPolygon/DrawTexturedPolygon/DrawString against SDL_gfx
/// (Golden/gfx.txt from port/tools/golden/gfx.cpp).
/// </summary>
public class SurfaceDrawTests
{
	private sealed record Case(string Line, string[] Args, ulong Hash);

	private static int Int(string s) => int.Parse(s, CultureInfo.InvariantCulture);

	private static (int W, int H, Surface Texture, List<Case> Cases) Load()
	{
		var lines = Golden.ReadLines("gfx.txt");
		var size = Golden.Fields(lines[0]);
		Assert.Equal("size", size[0]);
		Surface? texture = null;
		var cases = new List<Case>();
		foreach (var line in lines.Skip(1))
		{
			var f = Golden.Fields(line);
			if (f[0] == "texture")
			{
				int tw = Int(f[1]), th = Int(f[2]);
				texture = new Surface(tw, th, 0, 0, 1);
				for (int i = 0; i < tw * th; ++i)
					texture.SetPixel(i % tw, i / tw, (byte)Int(f[3 + i]));
				continue;
			}
			cases.Add(new Case(line, f[..^1], ulong.Parse(f[^1], NumberStyles.HexNumber, CultureInfo.InvariantCulture)));
		}
		Assert.NotNull(texture);
		return (Int(size[1]), Int(size[2]), texture, cases);
	}

	private static void Fill(Surface s)
	{
		for (int y = 0; y < s.Height; ++y)
			for (int x = 0; x < s.Width; ++x)
				s.SetPixel(x, y, (byte)((x * 7 + y * 13) % 251));
	}

	private static void Run(Surface s, string[] a, Surface texture)
	{
		switch (a[0])
		{
			case "line":
				s.DrawLine(Int(a[1]), Int(a[2]), Int(a[3]), Int(a[4]), (byte)Int(a[5]));
				break;
			case "circle":
				s.DrawCircle(Int(a[1]), Int(a[2]), Int(a[3]), (byte)Int(a[4]));
				break;
			case "polygon":
			case "textured":
			{
				int n = Int(a[1]);
				var x = new short[n];
				var y = new short[n];
				for (int i = 0; i < n; ++i)
				{
					x[i] = (short)Int(a[2 + 2 * i]);
					y[i] = (short)Int(a[3 + 2 * i]);
				}
				if (a[0] == "polygon")
					s.DrawPolygon(x, y, n, (byte)Int(a[2 + 2 * n]));
				else
					s.DrawTexturedPolygon(x, y, n, texture, Int(a[2 + 2 * n]), Int(a[3 + 2 * n]));
				break;
			}
			case "string":
			{
				byte[] text = a[4] == "-" ? [] : Convert.FromHexString(a[4]);
				s.DrawString(Int(a[1]), Int(a[2]), text, (byte)Int(a[3]));
				break;
			}
			default:
				Assert.Fail("unknown case " + a[0]);
				break;
		}
	}

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

	[Fact]
	public void Primitives_match_sdl_gfx()
	{
		var (w, h, texture, cases) = Load();
		Assert.True(cases.Count > 900);
		var failed = new List<string>();
		foreach (var c in cases)
		{
			var s = new Surface(w, h, 0, 0, 1);
			Fill(s);
			Run(s, c.Args, texture);
			if (Fnv(s.Pixels) != c.Hash)
				failed.Add(c.Line);
		}
		Assert.Empty(failed);
	}

	[Fact]
	public void Hd_surfaces_draw_the_same_logical_pixels()
	{
		var (w, h, texture, cases) = Load();
		foreach (var c in cases)
		{
			var classic = new Surface(w, h, 0, 0, 1);
			var hd = new Surface(w, h, 0, 0, 3);
			Fill(classic);
			Fill(hd);
			Run(classic, c.Args, texture);
			Run(hd, c.Args, texture);
			for (int py = 0; py < hd.PhysicalHeight; ++py)
			{
				for (int px = 0; px < hd.PhysicalWidth; ++px)
				{
					if (hd.Pixels[py * hd.PhysicalWidth + px] != classic.GetPixel(px / 3, py / 3))
						Assert.Fail($"{c.Line}: buffer pixel ({px}, {py})");
				}
			}
		}
	}

	[Fact]
	public void Hd_texture_keeps_its_detail()
	{
		var screen = new Surface(4, 1, 0, 0, 2);
		var texture = new Surface(2, 1, 0, 0, 2);
		for (int i = 0; i < texture.Pixels.Length; ++i)
			texture.Pixels[i] = (byte)(10 + i);
		short[] x = [0, 3, 3, 0], y = [0, 0, 1, 1];
		screen.DrawTexturedPolygon(x, y, 4, texture, 0, 0);
		// Row 0 of the polygon spans logical x 0..3; the 2x1 texture repeats, its 2x2 buffer pixels unchanged.
		Assert.Equal([10, 11, 12, 13, 10, 11, 12, 13], screen.Pixels[..8].ToArray());
		Assert.Equal([14, 15, 16, 17, 14, 15, 16, 17], screen.Pixels[8..16].ToArray());
	}
}
