using System.Buffers.Binary;

namespace Acom.Tests.Engine;

/// <summary>BMP and LBM decoding of small hand-made files (the golden test covers the generated ones).</summary>
public class ImageFormatTests
{
	private static byte[] Bmp8(int w, int h, byte[] pixels, Rgba[] colors, bool topDown = false)
	{
		int pitch = (w + 3) & ~3;
		using var m = new MemoryStream();
		var bw = new BinaryWriter(m);
		int off = 14 + 40 + 256 * 4;
		bw.Write("BM"u8);
		bw.Write(off + pitch * h);
		bw.Write(0);
		bw.Write(off);
		bw.Write(40);
		bw.Write(w);
		bw.Write(topDown ? -h : h);
		bw.Write((short)1);
		bw.Write((short)8);
		bw.Write(0); // BI_RGB
		bw.Write(pitch * h);
		bw.Write(0);
		bw.Write(0);
		bw.Write(colors.Length);
		bw.Write(0);
		for (int i = 0; i < 256; ++i)
		{
			var c = i < colors.Length ? colors[i] : new Rgba(0, 0, 0);
			bw.Write([c.B, c.G, c.R, 0]);
		}
		for (int r = 0; r < h; ++r)
		{
			int y = topDown ? r : h - 1 - r;
			bw.Write(pixels, y * w, w);
			bw.Write(new byte[pitch - w]);
		}
		return m.ToArray();
	}

	[Fact]
	public void Bmp_8bit_is_read_bottom_up_with_its_palette()
	{
		byte[] pixels = [1, 2, 3, 3, 2, 1];
		Rgba[] colors = [new(0, 0, 0), new(255, 0, 0), new(0, 255, 0), new(0, 0, 255)];
		foreach (bool topDown in new[] { false, true })
		{
			var s = new Surface(1, 1);
			s.LoadImage(Bmp8(3, 2, pixels, colors, topDown), "x.bmp");
			Assert.Equal((3, 2), (s.Width, s.Height));
			Assert.Equal(pixels, s.Pixels.ToArray());
			Assert.Equal(colors, s.Palette[..4].ToArray());
		}
	}

	[Fact]
	public void Bmp_rle8_decodes_runs_absolute_data_and_delta()
	{
		var data = Bmp8(4, 2, new byte[8], [new(9, 9, 9)]);
		int off = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(10));
		BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(30), 1); // BI_RLE8
		// bottom row: run of 2 x 5, then absolute 3 bytes (7, 8, 9 + pad byte; the 9 falls outside the buffer);
		// end of line; top row: delta +1, run 2 x 4; end of bitmap
		byte[] rle = [2, 5, 0, 3, 7, 8, 9, 0, 0, 0, 0, 2, 1, 0, 2, 4, 0, 1];
		var file = data[..off].Concat(rle).ToArray();
		var s = new Surface(1, 1);
		s.LoadImage(file, "x.bmp");
		Assert.Equal(new byte[] { 0, 4, 4, 0, 5, 5, 7, 8 }, s.Pixels.ToArray());
	}

	private static byte[] Chunk(string id, byte[] body)
	{
		var head = new byte[8];
		System.Text.Encoding.ASCII.GetBytes(id).CopyTo(head, 0);
		BinaryPrimitives.WriteInt32BigEndian(head.AsSpan(4), body.Length);
		return [.. head, .. body, .. (body.Length % 2 == 1 ? new byte[1] : [])];
	}

	private static byte[] Lbm(string kind, int w, int h, int planes, int mask, int compression, int tcolor, byte[] cmap, byte[] body)
	{
		var bmhd = new byte[20];
		BinaryPrimitives.WriteUInt16BigEndian(bmhd, (ushort)w);
		BinaryPrimitives.WriteUInt16BigEndian(bmhd.AsSpan(2), (ushort)h);
		bmhd[8] = (byte)planes;
		bmhd[9] = (byte)mask;
		bmhd[10] = (byte)compression;
		BinaryPrimitives.WriteUInt16BigEndian(bmhd.AsSpan(12), (ushort)tcolor);
		byte[] form = [.. System.Text.Encoding.ASCII.GetBytes(kind), .. Chunk("BMHD", bmhd), .. Chunk("CMAP", cmap), .. Chunk("BODY", body)];
		return [.. Chunk("FORM", form)];
	}

	[Fact]
	public void Ilbm_planes_are_deinterleaved_and_the_width_padded_to_16()
	{
		// 2 rows of 3 pixels, 1 plane: bits 101 and 010; one 16-bit word per plane and row
		byte[] body = [0b1010_0000, 0, 0b0100_0000, 0];
		var file = Lbm("ILBM", 3, 2, 1, 2, 0, 1, [0, 0, 0, 255, 255, 255], body);
		var image = ImageDecoder.DecodeSdl(file)!;
		Assert.Equal((16, 2, 1), (image.Width, image.Height, image.ColorKey));
		Assert.Equal(2, image.Palette.Length);
		Assert.Equal(new byte[] { 1, 0, 1, 0 }, image.Pixels[..4]);
		Assert.Equal(new byte[] { 0, 1, 0 }, image.Pixels[16..19]);

		var s = new Surface(1, 1);
		s.LoadImage(file, "x.lbm");
		// colour key 1: its pixels become the transparent index 0
		Assert.Equal(new byte[] { 0, 0, 0 }, s.Pixels[..3].ToArray());
	}

	[Fact]
	public void Pbm_byterun1_is_unpacked()
	{
		// 16 pixels: run of 5 x 7 (0xFC), 11 literal bytes
		byte[] body = [0xFC, 7, 10, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
		var file = Lbm("PBM ", 16, 1, 8, 0, 1, 0, [1, 2, 3, 4, 5, 6], body);
		var image = ImageDecoder.DecodeSdl(file)!;
		Assert.Equal(new byte[] { 7, 7, 7, 7, 7, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, image.Pixels);
		Assert.Equal(256, image.Palette.Length);
		Assert.Equal(new Rgba(4, 5, 6), image.Palette[1]);
		Assert.Throws<InvalidDataException>(() => ImageDecoder.DecodeSdl(file[..^3]));
	}

	[Fact]
	public void Unsupported_plane_counts_are_rejected()
	{
		var file = Lbm("ILBM", 16, 1, 3, 0, 0, 0, [], new byte[6]);
		Assert.Equal("unsupported number of color planes", Assert.Throws<InvalidDataException>(() => ImageDecoder.DecodeSdl(file)).Message);
	}
}
