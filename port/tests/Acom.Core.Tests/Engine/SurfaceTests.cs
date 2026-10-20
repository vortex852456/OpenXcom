
namespace Acom.Tests.Engine;

public class SurfaceTests
{
	private static Surface Filled(int w, int h, byte color, int scale = 1, int x = 0, int y = 0)
	{
		var s = new Surface(w, h, x, y, scale);
		s.DrawRect(0, 0, (short)w, (short)h, color);
		return s;
	}

	[Fact]
	public void Blit_treats_index_zero_as_transparent_and_clips()
	{
		var dest = Filled(8, 8, 9);
		var src = new Surface(4, 4, 6, 6, 1);
		src.SetPixel(0, 0, 5);
		src.SetPixel(1, 1, 7);
		src.SetPixel(3, 3, 8); // lands outside the destination
		src.Blit(dest);
		Assert.Equal(5, dest.GetPixel(6, 6));
		Assert.Equal(7, dest.GetPixel(7, 7));
		Assert.Equal(9, dest.GetPixel(7, 6)); // transparent source pixel keeps destination
	}

	[Fact]
	public void DrawRect_stores_the_clipped_rectangle_like_SDL_FillRect()
	{
		var s = new Surface(10, 8, 0, 0, 2);
		var r = new SDL_Rect(-2, 3, 20, 10);
		s.DrawRect(ref r, 7);
		Assert.Equal((0, 3, 10, 5), ((int)r.x, (int)r.y, (int)r.w, (int)r.h));
		Assert.Equal(7, s.GetPixel(9, 7));
		Assert.Equal(0, s.GetPixel(9, 2));
		// no overlap: SDL_IntersectRect still stores the (empty) intersection
		var outside = new SDL_Rect(12, 1, 4, 4);
		s.DrawRect(ref outside, 7);
		Assert.Equal((12, 1, 0, 4), ((int)outside.x, (int)outside.y, (int)outside.w, (int)outside.h));
		// empty rectangles return before SDL_FillRect and stay unchanged
		var empty = new SDL_Rect(-5, -5, 0, 3);
		s.DrawRect(ref empty, 7);
		Assert.Equal((-5, -5, 0, 3), ((int)empty.x, (int)empty.y, (int)empty.w, (int)empty.h));
	}

	[Fact]
	public void Hidden_or_invisible_surfaces_are_not_blit()
	{
		var dest = new Surface(4, 4, 0, 0, 1);
		var src = Filled(4, 4, 3);
		src.Hidden = true;
		src.Blit(dest);
		Assert.Equal(0, dest.GetPixel(0, 0));
		src.Hidden = false;
		src.Visible = false;
		src.Blit(dest);
		Assert.Equal(0, dest.GetPixel(0, 0));
	}

	[Fact]
	public void Classic_art_is_upscaled_into_hd_surface()
	{
		var screen = new Surface(4, 4, 0, 0, scale: 4);
		var classic = new Surface(2, 1, 1, 1, scale: 1);
		classic.SetPixel(0, 0, 10);
		classic.SetPixel(1, 0, 11);
		classic.Blit(screen);
		// Logical pixel (1,1) covers buffer pixels 4..7 x 4..7.
		Assert.Equal(10, screen.Pixels[4 * 16 + 4]);
		Assert.Equal(10, screen.Pixels[7 * 16 + 7]);
		Assert.Equal(11, screen.Pixels[4 * 16 + 8]);
		Assert.Equal(0, screen.Pixels[3 * 16 + 4]);
		Assert.Equal(10, screen.GetPixel(1, 1));
	}

	[Fact]
	public void Hd_art_keeps_full_detail_on_hd_screen()
	{
		var screen = new Surface(2, 2, 0, 0, scale: 4);
		var hd = new Surface(1, 1, 1, 0, scale: 4);
		hd.Pixels[5] = 42; // buffer pixel (1,1) inside the single logical pixel
		hd.Blit(screen);
		Assert.Equal(42, screen.Pixels[1 * 8 + 4 + 1]);
		Assert.Equal(0, screen.Pixels[0 * 8 + 4]);
	}

	[Fact]
	public void BlitNShade_shades_and_recolours_like_cpp()
	{
		var dest = new Surface(2, 1, 0, 0, 1);
		var src = new Surface(2, 1, 0, 0, 1);
		src.SetPixel(0, 0, 0x23);
		src.SetPixel(1, 0, 0x2E);
		src.BlitNShade(dest, 0, 0, shade: 3);
		Assert.Equal(0x26, dest.GetPixel(0, 0));
		Assert.Equal(0x0F, dest.GetPixel(1, 0)); // would flip into the next colour group -> black

		var recoloured = new Surface(2, 1, 0, 0, 1);
		src.BlitNShade(recoloured, 0, 0, shade: 1, newBaseColor: 5);
		Assert.Equal(0x44, recoloured.GetPixel(0, 0));
		Assert.Equal(0x4F, recoloured.GetPixel(1, 0));
	}

	[Fact]
	public void BlitNShade_half_draws_only_right_half()
	{
		var dest = new Surface(4, 1, 0, 0, 1);
		var src = Filled(4, 1, 0x11);
		src.BlitNShade(dest, 0, 0, half: true);
		Assert.Equal([0, 0, 0x11, 0x11], dest.Pixels.ToArray());
	}

	[Fact]
	public void Offset_and_invert_leave_transparent_pixels()
	{
		var s = new Surface(3, 1, 0, 0, 1);
		s.SetPixel(1, 0, 20);
		s.SetPixel(2, 0, 250);
		s.Offset(10, -1, 255);
		Assert.Equal([0, 30, 255], s.Pixels.ToArray());
		s.Invert(100);
		Assert.Equal([0, 170, unchecked((byte)(255 + 2 * (100 - 255)))], s.Pixels.ToArray());
	}

	[Fact]
	public void OffsetBlock_clamps_to_colour_block()
	{
		var s = new Surface(2, 1, 0, 0, 1);
		s.SetPixel(0, 0, 0x12);
		s.SetPixel(1, 0, 0x1E);
		s.OffsetBlock(5);
		Assert.Equal([0x17, 0x20], s.Pixels.ToArray()); // C++ clamps to min + blk (inclusive)
	}

	[Fact]
	public void Resize_keeps_overlapping_content()
	{
		var s = Filled(2, 2, 7, scale: 2);
		s.Resize(3, 1);
		Assert.Equal(6, s.PhysicalWidth);
		Assert.Equal(7, s.GetPixel(1, 0));
		Assert.Equal(0, s.GetPixel(2, 0));
	}

	[Fact]
	public void Copy_takes_pixels_under_the_surface_including_zero()
	{
		var background = new Surface(4, 4, 0, 0, 1);
		background.SetPixel(2, 2, 9);
		var s = Filled(2, 2, 5, 1, x: 2, y: 2);
		s.Copy(background);
		Assert.Equal(9, s.GetPixel(0, 0));
		Assert.Equal(0, s.GetPixel(1, 1));
	}
}
