
namespace Acom.Tests.Engine;

public class LoaderTests
{
	private static MemoryStream Bytes(params int[] values) => new([.. values.Select(v => (byte)v)]);

	private static byte[] Row(Surface s, int y) => [.. Enumerable.Range(0, s.Width).Select(x => s.GetPixel(x, y))];

	[Fact]
	public void LoadPck_decodes_rle_frames_with_16bit_tab()
	{
		// Frame 0: skip 1 row, pixels 1 2, 3 transparent, pixel 5. Frame 1: pixel 7.
		var pck = Bytes(1, 1, 2, 254, 3, 5, 255, 0, 7, 255);
		var tab = Bytes(0, 0, 7, 0); // two 16-bit offsets
		var set = new SurfaceSet(4, 3);
		set.LoadPck(pck, tab);
		Assert.Equal(2, set.TotalFrames);
		var f0 = set.GetFrame(0)!;
		Assert.Equal([0, 0, 0, 0], Row(f0, 0));
		Assert.Equal([1, 2, 0, 0], Row(f0, 1));
		Assert.Equal([0, 5, 0, 0], Row(f0, 2));
		Assert.Equal(7, set.GetFrame(1)!.GetPixel(0, 0));
		Assert.Null(set.GetFrame(2));
	}

	[Fact]
	public void LoadPck_detects_32bit_tab()
	{
		var set = new SurfaceSet(2, 2);
		set.LoadPck(Bytes(0, 255, 0, 255), Bytes(0, 0, 0, 0, 2, 0, 0, 0));
		Assert.Equal(2, set.TotalFrames);
	}

	[Fact]
	public void LoadDat_splits_into_equal_frames()
	{
		var set = new SurfaceSet(2, 2);
		set.LoadDat(Bytes(1, 2, 3, 4, 5, 6, 7, 8, 9));
		Assert.Equal(2, set.TotalFrames);
		Assert.Equal(4, set.GetFrame(0)!.GetPixel(1, 1));
		Assert.Equal(5, set.GetFrame(1)!.GetPixel(0, 0));
	}

	[Fact]
	public void LoadSpk_handles_transparent_and_literal_runs()
	{
		var s = new Surface(4, 2, 0, 0, 1);
		s.LoadSpk(Bytes(0xFF, 0xFF, 1, 0, 0xFE, 0xFF, 1, 0, 0xAA, 0xBB, 0xFD, 0xFF));
		Assert.Equal([0, 0, 0xAA, 0xBB], Row(s, 0));
	}

	[Fact]
	public void LoadBdy_stops_runs_at_row_end()
	{
		var s = new Surface(4, 2, 0, 0, 1);
		// 3x colour 9, then 2 literals (4, 5) where 5 would overscan into the next row
		s.LoadBdy(Bytes(254, 9, 1, 4, 5));
		Assert.Equal([9, 9, 9, 4], Row(s, 0));
		Assert.Equal([0, 0, 0, 0], Row(s, 1));
	}

	[Fact]
	public void LoadScr_copies_raw_indices_and_upscales_for_hd()
	{
		var s = new Surface(2, 1, 0, 0, 2);
		s.LoadScr(Bytes(3, 4));
		Assert.Equal(4, s.GetPixel(1, 0));
		Assert.Equal(4, s.Pixels[2 * 2 + 3]); // second buffer row, last pixel
	}

	[Fact]
	public void CatFile_sizes_come_from_offsets()
	{
		// Two items at offsets 16 and 20; stored sizes are deliberately wrong.
		var cat = new CatFile(Bytes(16, 0, 0, 0, 99, 0, 0, 0, 20, 0, 0, 0, 99, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7));
		Assert.Equal(2, cat.Size);
		Assert.Equal([1, 2, 3, 4], cat.GetItem(0).ToArray());
		Assert.Equal([5, 6, 7], cat.GetItem(1).ToArray());
		Assert.True(cat.GetItem(2).IsEmpty);
	}
}
