using System.Globalization;
using Acom.Engine;

namespace Acom.Tests.Engine;

[Collection(nameof(OptionsTests))] // swaps Logger.Sink
public class ImageTests
{
	private static string Hash(IEnumerable<byte> data)
	{
		ulong h = 14695981039346656037UL;
		foreach (byte b in data)
			h = (h ^ b) * 1099511628211UL;
		return h.ToString("x16", CultureInfo.InvariantCulture);
	}

	private static IEnumerable<byte> Pixels(Surface s)
	{
		for (int y = 0; y < s.Height; ++y)
			for (int x = 0; x < s.Width; ++x)
				yield return s.GetPixel(x, y);
	}

	/// <summary>Surface.LoadImage against lodepng/SDL_image as used by the C++ Surface::loadImage (Golden/images.txt).</summary>
	[Fact]
	public void LoadImage_matches_original()
	{
		var root = LanguageTests.RepoRoot();
		var failures = new List<string>();
		var lines = Golden.ReadLines("images.txt");
		Assert.True(lines.Length > 70);
		var oldSink = Logger.Sink;
		var log = new List<string>();
		Logger.Sink = (level, message) => { lock (log) log.Add(message); };
		try
		{
			foreach (var line in lines)
			{
				var f = Golden.Fields(line);
				var data = File.ReadAllBytes(Path.Combine(root, f[0]));
				var surface = new Surface(1, 1, 5, 5, 1);
				string actual;
				try
				{
					surface.LoadImage(data, f[0]);
					bool lodepng = f[0].EndsWith(".png", StringComparison.OrdinalIgnoreCase) && TryLodePng(data);
					int colors = int.Parse(f.Length > 6 ? f[6] : "0", CultureInfo.InvariantCulture);
					var pal = surface.Palette[..colors].ToArray().SelectMany(c => lodepng ? new[] { c.R, c.G, c.B, c.A } : new[] { c.R, c.G, c.B });
					string warning;
					lock (log)
						warning = log.LastOrDefault(m => m.Contains(f[0] + " (from", StringComparison.Ordinal)) ?? "";
					int transparent = warning.Length == 0 ? 0 : int.Parse(warning.Split("index ")[1].Split(' ')[0], CultureInfo.InvariantCulture);
					actual = string.Join(' ', f[0], lodepng ? "lodepng" : "sdl", surface.Width, surface.Height, transparent, Hash(Pixels(surface)), colors, Hash(pal));
					Assert.Equal(0, surface.X);
				}
				catch (InvalidDataException)
				{
					actual = f[0] + " error";
				}
				if (actual != line)
					failures.Add("expected " + line + "\n  actual " + actual);
			}
		}
		finally
		{
			Logger.Sink = oldSink;
		}
		Assert.Empty(failures);
	}

	private static bool TryLodePng(byte[] data)
	{
		try
		{
			return data.Length > 32 && ImageDecoder.DecodeLodePng(data) != null;
		}
		catch (InvalidDataException)
		{
			return false;
		}
	}

	[Fact]
	public void LoadImage_with_pixel_density_keeps_logical_size()
	{
		var path = Path.Combine(LanguageTests.RepoRoot(), "port", "tests", "Acom.Core.Tests", "Images", "gif_big.gif");
		var surface = new Surface(1, 1);
		surface.LoadImage(File.ReadAllBytes(path), "gif_big.gif", 4);
		Assert.Equal((32, 24, 4), (surface.Width, surface.Height, surface.Scale));
		Assert.Equal(128 * 96, surface.Pixels.Length);
		Assert.Throws<InvalidDataException>(() => surface.LoadImage(File.ReadAllBytes(path), "gif_big.gif", 5));
	}

	[Fact]
	public void LoadImage_rejects_unknown_formats()
	{
		var e = Assert.Throws<InvalidDataException>(() => new Surface(1, 1).LoadImage([.. "BM not supported"u8], "x.bmp"));
		Assert.Equal("x.bmp:Unsupported image format", e.Message);
	}
}
