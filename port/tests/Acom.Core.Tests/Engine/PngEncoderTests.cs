using System.Buffers.Binary;
using System.IO.Compression;

namespace Acom.Tests.Engine;

public class PngEncoderTests
{
	/// <summary>Minimal PNG reader for 8-bit RGB files (non-interlaced, all filter types), independent of the encoder.</summary>
	private static (int Width, int Height, byte[] Rgb) DecodeRgb(byte[] png)
	{
		Assert.Equal(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 13, 10, 26, 10 }, png[..8]);
		int pos = 8, width = 0, height = 0;
		using var idat = new MemoryStream();
		while (pos < png.Length)
		{
			int length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(pos));
			string type = System.Text.Encoding.ASCII.GetString(png, pos + 4, 4);
			var body = png.AsSpan(pos + 8, length);
			if (type == "IHDR")
			{
				width = BinaryPrimitives.ReadInt32BigEndian(body);
				height = BinaryPrimitives.ReadInt32BigEndian(body[4..]);
				Assert.Equal(new byte[] { 8, 2, 0, 0, 0 }, body[8..].ToArray());
			}
			else if (type == "IDAT")
			{
				idat.Write(body);
			}
			pos += 12 + length;
		}
		idat.Position = 0;
		using var zlib = new ZLibStream(idat, CompressionMode.Decompress);
		using var raw = new MemoryStream();
		zlib.CopyTo(raw);
		var data = raw.ToArray();
		int stride = width * 3;
		Assert.Equal((stride + 1) * height, data.Length);
		var rgb = new byte[stride * height];
		for (int y = 0; y < height; ++y)
		{
			int filter = data[y * (stride + 1)];
			for (int i = 0; i < stride; ++i)
			{
				int a = i >= 3 ? rgb[y * stride + i - 3] : 0;
				int b = y > 0 ? rgb[(y - 1) * stride + i] : 0;
				int c = i >= 3 && y > 0 ? rgb[(y - 1) * stride + i - 3] : 0;
				int pa = Math.Abs(b - c), pb = Math.Abs(a - c), pc = Math.Abs(a + b - 2 * c);
				int pred = filter switch
				{
					0 => 0,
					1 => a,
					2 => b,
					3 => (a + b) >> 1,
					4 => pa <= pb && pa <= pc ? a : pb <= pc ? b : c,
					_ => throw new InvalidDataException("filter"),
				};
				rgb[y * stride + i] = (byte)(data[y * (stride + 1) + 1 + i] + pred);
			}
		}
		return (width, height, rgb);
	}

	[Fact]
	public void Palette_image_round_trips_through_the_png_reader()
	{
		var palette = Enumerable.Range(0, 256).Select(i => new Rgba((byte)i, (byte)(255 - i), (byte)(i * 7))).ToArray();
		var pixels = Enumerable.Range(0, 37 * 11).Select(i => (byte)(i * 31 % 256)).ToArray();
		var png = PngEncoder.EncodePalette(pixels, 37, 11, palette);

		var image = ImageDecoder.DecodeLodePng(png);
		Assert.NotNull(image);
		Assert.Equal((37, 11), (image.Width, image.Height));
		Assert.Equal(pixels, image.Pixels);
		Assert.Equal(palette, image.Palette);
	}

	[Fact]
	public void Palette_alpha_goes_into_trns_and_marks_the_transparent_colour()
	{
		Rgba[] palette = [new(1, 2, 3), new(4, 5, 6, 0), new(7, 8, 9)];
		byte[] pixels = [0, 1, 2, 1, 0, 2];
		var png = PngEncoder.EncodePalette(pixels, 3, 2, palette);
		var surface = new Surface(1, 1);
		surface.LoadImage(png, "t.png");
		// lodepng path: the first colour with alpha 0 (index 1) is the transparent one, its pixels become index 0
		Assert.Equal(new byte[] { 0, 0, 2, 0, 0, 2 }, surface.Pixels.ToArray());
		Assert.Equal(palette, ImageDecoder.DecodeLodePng(png)!.Palette);
	}

	[Fact]
	public void Rgb_image_round_trips()
	{
		const int w = 29, h = 13;
		var rgb = new byte[w * h * 3];
		for (int i = 0; i < rgb.Length; ++i)
			rgb[i] = (byte)(i % 5 == 0 ? i * 13 : i / 3 % 7 * 30); // mix of noise and gradients: several filter types
		var png = PngEncoder.EncodeRgb(rgb, w, h);
		// valid chunks and CRCs (the decoder checks them; true colour gives no 8-bit image)
		Assert.Null(ImageDecoder.DecodeLodePng(png));
		var (width, height, decoded) = DecodeRgb(png);
		Assert.Equal((w, h), (width, height));
		Assert.Equal(rgb, decoded);
	}

	[Fact]
	public void Bad_arguments_are_rejected()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => PngEncoder.EncodeRgb(new byte[3], 0, 1));
		Assert.Throws<ArgumentException>(() => PngEncoder.EncodeRgb(new byte[5], 2, 1));
		Assert.Throws<ArgumentOutOfRangeException>(() => PngEncoder.EncodePalette(new byte[1], 1, 1, []));
	}

	internal static (int Width, int Height, byte[] Rgb) Decode(byte[] png) => DecodeRgb(png);
}
