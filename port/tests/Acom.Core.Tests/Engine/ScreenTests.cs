namespace Acom.Tests.Engine;

// Screen reads and writes the global display options.
[Collection("OptionsTests")]
public class ScreenTests
{
	private static void DefaultOptions()
	{
		Options.Create();
		Options.ResetDefault(false);
	}

	[Fact]
	public void Window_mapping_keeps_aspect_ratio()
	{
		DefaultOptions();
		var screen = new Screen();
		screen.SetWindowSize(1920, 1080);
		Assert.Equal(5.4, screen.XScale, 6);
		Assert.Equal(5.4, screen.YScale, 6);
		Assert.Equal(96, screen.CursorLeftBlackBand);
		Assert.Equal(0, screen.CursorTopBlackBand);
		Assert.Equal(96, screen.LeftBlackBand);
		Assert.Equal((1728, 1080), screen.DisplaySize);
	}

	[Fact]
	public void Cursor_outside_black_bands_scales_over_the_whole_window()
	{
		DefaultOptions();
		Options.fullscreen = true; // cursorInBlackBandsInFullscreen defaults to false
		var screen = new Screen();
		screen.SetWindowSize(1920, 1080);
		Assert.Equal(6.0, screen.XScale, 6);
		Assert.Equal(5.4, screen.YScale, 6);
		Assert.Equal(0, screen.CursorLeftBlackBand);
		Assert.Equal(96, screen.LeftBlackBand);
		Assert.Equal((1728, 1080), screen.DisplaySize);
	}

	[Fact]
	public void Without_aspect_ratio_the_image_is_stretched()
	{
		DefaultOptions();
		Options.keepAspectRatio = false;
		var screen = new Screen();
		screen.SetWindowSize(1000, 1000);
		Assert.Equal(1000 / 320.0, screen.XScale, 6);
		Assert.Equal(5.0, screen.YScale, 6);
		Assert.Equal((1000, 1000), screen.DisplaySize);
	}

	[Fact]
	public void Letterbox_with_non_square_pixels()
	{
		DefaultOptions();
		Options.nonSquarePixelRatio = true;
		var screen = new Screen();
		screen.SetWindowSize(640, 1000);
		// targetHeight = floor(2 * 200 * 1.2) = 480, top = (1000 - 480) / 2
		Assert.Equal(260, screen.TopBlackBand);
		Assert.Equal(260, screen.CursorTopBlackBand);
		Assert.Equal((640, 480), screen.DisplaySize);
	}

	[Fact]
	public void ResetDisplay_resizes_the_buffer_and_reports_the_window_as_display()
	{
		DefaultOptions();
		var screen = new Screen(scale: 2);
		screen.SetWindowSize(1280, 800);
		screen.Surface.SetPixel(0, 0, 5);
		Options.baseXResolution = 640;
		Options.baseYResolution = 400;
		screen.ResetDisplay(false);
		Assert.Equal(640, screen.Width);
		Assert.Equal(400, screen.Height);
		Assert.Equal(1280, screen.Surface.PhysicalWidth);
		Assert.Equal(1280 * 800 * 4, screen.Frame.Length);
		Assert.Equal(160, screen.DX);
		Assert.Equal(100, screen.DY);
		Assert.Equal(2.0, screen.XScale, 6);
		Assert.Equal(1280, Options.displayWidth);
		Assert.Equal(800, Options.displayHeight);

		// same size: the surface is kept, but cleared
		var surface = screen.Surface;
		surface.SetPixel(0, 0, 5);
		screen.ResetDisplay(false);
		Assert.Same(surface, screen.Surface);
		Assert.Equal(0, surface.GetPixel(0, 0));
	}

	[Fact]
	public void ResetDisplay_lowers_the_pixel_density_of_big_screens()
	{
		DefaultOptions();
		var screen = new Screen(scale: 4);
		Options.baseXResolution = 1920;
		Options.baseYResolution = 1080;
		screen.ResetDisplay(false);
		Assert.Equal(1920, screen.Surface.PhysicalWidth);
		Assert.Equal(1080, screen.Surface.PhysicalHeight);
	}

	[Fact]
	public void ResetDisplay_asks_the_host_for_the_video_mode()
	{
		DefaultOptions();
		var screen = new Screen();
		(int, int)? requested = null;
		screen.VideoModeHandler = (w, h) =>
		{
			requested = (w, h);
			screen.SetWindowSize(w + 1, h + 1); // the window manager may pick another size
		};
		Options.displayWidth = 1024;
		Options.displayHeight = 768;
		Options.baseXResolution = 320;
		Options.baseYResolution = 200;
		screen.ResetDisplay();
		Assert.Equal((1024, 768), requested);
		Assert.Equal(1025, Options.displayWidth);
		Assert.Equal(769, Options.displayHeight);
	}

	[Fact]
	public void UpdateScale_uses_the_display_size()
	{
		DefaultOptions();
		Options.displayWidth = 1920;
		Options.displayHeight = 1080;
		int w = 0, h = 0;
		Screen.UpdateScale((int)ScaleType.SCALE_SCREEN_DIV_3, ref w, ref h, true);
		Assert.Equal((640, 360), (w, h));
		Assert.Equal(640, Options.baseXResolution);
		Assert.Equal(360, Options.baseYResolution);
		Screen.UpdateScale((int)ScaleType.SCALE_SCREEN_DIV_8, ref w, ref h, false);
		Assert.Equal((320, 200), (w, h)); // 240x135 is raised to the minimum
	}
}
