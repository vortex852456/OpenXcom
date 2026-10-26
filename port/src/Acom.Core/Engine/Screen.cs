/*
 * Copyright 2010-2026 OpenXcom Developers.
 * C# port: Copyright 2026 ACOM contributors.
 *
 * This file is part of ACOM, a C# port of OpenXcom Extended.
 *
 * ACOM is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * ACOM is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Acom.  If not, see <http://www.gnu.org/licenses/>.
 */
// Ported from: src/Engine/Screen.h, src/Engine/Screen.cpp
// Not ported on purpose: the OpenGL/shader and software scaler paths (makeVideoFlags, use32bitScaler, useOpenGL, Zoom):
// the platform host owns the window and scales the frame on the GPU (docs/architecture/csharp-port.md, deviations).

using System.Runtime.InteropServices;

namespace Acom;

/// <summary>
/// A display screen: holds the logical game screen surface and converts it into a true-colour
/// frame for the platform host. Also knows how the logical screen is mapped into the window
/// (scale and black bands), which <see cref="Action"/> needs to translate mouse coordinates.
/// </summary>
/// <remarks>
/// C++ owns the SDL window (<c>SDL_SetVideoMode</c>). Here the host owns it: it reports the client size with
/// <see cref="SetWindowSize"/> and applies video mode requests through <see cref="VideoModeHandler"/>.
/// </remarks>
public sealed class Screen
{
	public const int ORIGINAL_WIDTH = 320;
	public const int ORIGINAL_HEIGHT = 200;

	/// <summary>
	/// Largest screen buffer in pixels (port only): when a big base resolution would exceed it, the screen's
	/// pixel density is lowered (not below 1), see <see cref="ResetDisplay"/>.
	/// </summary>
	public const int MAX_PHYSICAL_WIDTH = ORIGINAL_WIDTH * 8, MAX_PHYSICAL_HEIGHT = ORIGINAL_HEIGHT * 8;

	private readonly int _scale;
	private Surface _surface;
	private readonly Rgba[] _palette = new Rgba[256];
	private byte[] _frame;
	private int _baseWidth, _baseHeight;
	private int _windowWidth, _windowHeight;
	private double _scaleX = 1.0, _scaleY = 1.0;
	private int _topBlackBand, _bottomBlackBand, _leftBlackBand, _rightBlackBand, _cursorTopBlackBand, _cursorLeftBlackBand;

	/// <param name="baseWidth">Logical width (320 for the classic layout).</param>
	/// <param name="baseHeight">Logical height (200 for the classic layout).</param>
	/// <param name="scale">Pixel density of the screen buffer (1 = classic, 4 = HD).</param>
	public Screen(int baseWidth = ORIGINAL_WIDTH, int baseHeight = ORIGINAL_HEIGHT, int scale = 1)
	{
		_scale = scale;
		_baseWidth = baseWidth;
		_baseHeight = baseHeight;
		_surface = new Surface(baseWidth, baseHeight, 0, 0, scale);
		_frame = new byte[_surface.PhysicalWidth * _surface.PhysicalHeight * 4];
		SetWindowSize(_surface.PhysicalWidth, _surface.PhysicalHeight);
	}

	/// <summary>The surface everything is blit onto. Replaced by <see cref="ResetDisplay"/> when the base resolution changes.</summary>
	public Surface Surface => _surface;

	/// <summary>Logical width.</summary>
	public int Width => _surface.Width;

	/// <summary>Logical height.</summary>
	public int Height => _surface.Height;

	/// <summary>Window width in pixels (C++ <c>Screen::getWidth</c>: the SDL video surface).</summary>
	public int WindowWidth => _windowWidth;

	/// <summary>Window height in pixels (C++ <c>Screen::getHeight</c>: the SDL video surface).</summary>
	public int WindowHeight => _windowHeight;

	/// <summary>Horizontal offset of the 320x200 layout centered on the logical screen.</summary>
	public int DX => (Width - ORIGINAL_WIDTH) / 2;

	/// <summary>Vertical offset of the 320x200 layout centered on the logical screen.</summary>
	public int DY => (Height - ORIGINAL_HEIGHT) / 2;

	/// <summary>Window pixels per logical pixel (horizontal), as used for mouse input.</summary>
	public double XScale => _scaleX;

	/// <summary>Window pixels per logical pixel (vertical), as used for mouse input.</summary>
	public double YScale => _scaleY;

	/// <summary>Height of the black band above the image that the cursor may enter, in window pixels.</summary>
	public int CursorTopBlackBand => _cursorTopBlackBand;

	/// <summary>Width of the black band left of the image that the cursor may enter, in window pixels.</summary>
	public int CursorLeftBlackBand => _cursorLeftBlackBand;

	/// <summary>Height of the black band above the image, in window pixels (where the host draws the frame).</summary>
	public int TopBlackBand => _topBlackBand;

	/// <summary>Width of the black band left of the image, in window pixels (where the host draws the frame).</summary>
	public int LeftBlackBand => _leftBlackBand;

	/// <summary>Size of the scaled image inside the window, in window pixels.</summary>
	public (int Width, int Height) DisplaySize
		=> (_windowWidth - _leftBlackBand - _rightBlackBand, _windowHeight - _topBlackBand - _bottomBlackBand);

	/// <summary>
	/// Host callback for a video mode change (C++ <c>SDL_SetVideoMode</c> in resetDisplay): it applies
	/// <see cref="Options.fullscreen"/>/<see cref="Options.borderless"/> and the requested display size
	/// (width, height) to the window and reports the resulting client size through <see cref="SetWindowSize"/>
	/// before returning. Without a host the window keeps its size.
	/// </summary>
	public System.Action<int, int>? VideoModeHandler { get; set; }

	/// <summary>Incremented on every <see cref="Flip"/>; lets the host skip unchanged frames.</summary>
	public long FrameNumber { get; private set; }

	/// <summary>Last flipped frame as RGBA bytes, <see cref="Surface.PhysicalWidth"/> per row.</summary>
	public ReadOnlySpan<byte> Frame => _frame;

	/// <summary>The screen's 8bpp palette.</summary>
	public ReadOnlySpan<Rgba> Palette => _palette;

	/// <summary>Changes the screen's palette.</summary>
	public void SetPalette(ReadOnlySpan<Rgba> colors, int firstcolor = 0, int ncolors = 256)
	{
		colors[..Math.Min(ncolors, colors.Length)].CopyTo(_palette.AsSpan(firstcolor));
	}

	/// <summary>
	/// Resets the screen surfaces based on the current display options,
	/// as they don't automatically take effect.
	/// </summary>
	/// <param name="resetVideo">Reset display surface (window mode and size, through <see cref="VideoModeHandler"/>).</param>
	/// <param name="noShaders">Don't load shaders (no OpenGL path in the port).</param>
	public void ResetDisplay(bool resetVideo = true, bool noShaders = false)
	{
		_ = noShaders; // OpenGL shaders are not ported
		int width = Options.displayWidth;
		int height = Options.displayHeight;
		// makeVideoFlags: the window flags are the host's business
		_baseWidth = Options.baseXResolution;
		_baseHeight = Options.baseYResolution;

		if (_surface.Width != _baseWidth || _surface.Height != _baseHeight) // don't reallocate _surface if not necessary, it's a waste of CPU cycles
		{
			// Port: keep the pixel density, unless the buffer would get too big (the palette lives in _palette and is kept).
			int scale = Math.Clamp(Math.Min(MAX_PHYSICAL_WIDTH / _baseWidth, MAX_PHYSICAL_HEIGHT / _baseHeight), 1, _scale);
			_surface = new Surface(_baseWidth, _baseHeight, 0, 0, scale);
			_frame = new byte[_surface.PhysicalWidth * _surface.PhysicalHeight * 4];
		}

		if (resetVideo)
		{
			Logger.Info($"Attempting to set display to {width}x{height}x8...");
			VideoModeHandler?.Invoke(width, height);
			Logger.Info($"Display set to {_windowWidth}x{_windowHeight}x8.");
		}
		else
		{
			Clear();
		}

		Options.displayWidth = _windowWidth;
		Options.displayHeight = _windowHeight;
		UpdateBlackBands();
	}

	/// <summary>
	/// Handles screen key shortcuts: debug slow motion (Alt+F8), fullscreen toggle (Alt+Enter, applied by the host
	/// through <see cref="VideoModeHandler"/>) and the screenshot key.
	/// </summary>
	/// <param name="action">Pointer to an action.</param>
	public void Handle(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		var ev = action.Details;
		// SDL_GetModState(): the modifiers of the key event being handled
		bool alt = (ev.key.keysym.mod & SDLMod.KMOD_ALT) != 0;
		if (Options.debug)
		{
			if (ev.type == SDLEventType.SDL_KEYDOWN && ev.key.keysym.sym == SDLKey.SDLK_F8 && alt)
			{
				switch (Timer.GameSlowSpeed)
				{
					case 1: Timer.GameSlowSpeed = 5; break;
					case 5: Timer.GameSlowSpeed = 15; break;
					default: Timer.GameSlowSpeed = 1; break;
				}
			}
		}

		if (ev.type == SDLEventType.SDL_KEYDOWN && ev.key.keysym.sym == SDLKey.SDLK_RETURN && alt)
		{
			Options.fullscreen = !Options.fullscreen;
			ResetDisplay();
		}
		else if (ev.type == SDLEventType.SDL_KEYDOWN && ev.key.keysym.sym == Options.keyScreenshot)
		{
			string filename;
			int i = 0;
			do
			{
				filename = Options.GetMasterUserFolder() + "screen" + i.ToString("D3", System.Globalization.CultureInfo.InvariantCulture) + ".png";
				i++;
			}
			while (CrossPlatform.FileExists(filename));
			Screenshot(filename);
			return;
		}
	}

	/// <summary>
	/// Saves a screenshot of the screen's contents.
	/// </summary>
	/// <remarks>
	/// Like the C++ software path: the window image (the frame scaled into the window with its black bands, width
	/// rounded down to a multiple of 4) as RGB, or with <see cref="Options.oxceRawScreenShots"/> the indexed screen
	/// buffer with its palette (the port's screen is always 8 bpp). The host scales on the GPU with nearest-neighbour
	/// sampling, which the window image here reproduces.
	/// </remarks>
	/// <param name="filename">Filename of the PNG file.</param>
	public void Screenshot(string filename)
	{
		byte[] png;
		if (Options.oxceRawScreenShots)
		{
			var palette = new Rgba[256];
			for (int i = 0; i < 256; ++i)
				palette[i] = _palette[i] with { A = 255 };
			png = PngEncoder.EncodePalette(_surface.Pixels, _surface.PhysicalWidth, _surface.PhysicalHeight, palette);
		}
		else
		{
			var (rgb, width, height) = WindowImage();
			png = PngEncoder.EncodeRgb(rgb, width, height);
		}
		PngEncoder.WriteFile(filename, png);
	}

	/// <summary>
	/// The window's contents as RGB: the last flipped frame scaled (nearest neighbour) into the area between the black
	/// bands, the width rounded down to a multiple of 4 like the C++ screenshot surface.
	/// </summary>
	public (byte[] Rgb, int Width, int Height) WindowImage()
	{
		int width = Math.Max(_windowWidth - _windowWidth % 4, 1);
		int height = Math.Max(_windowHeight, 1);
		var rgb = new byte[width * height * 3];
		int srcW = _surface.PhysicalWidth, srcH = _surface.PhysicalHeight;
		var (dstW, dstH) = DisplaySize;
		if (dstW <= 0 || dstH <= 0)
			return (rgb, width, height);
		for (int y = 0; y < dstH; ++y)
		{
			int wy = y + _topBlackBand;
			if (wy < 0 || wy >= height)
				continue;
			int sy = (int)((long)y * srcH / dstH);
			for (int x = 0; x < dstW; ++x)
			{
				int wx = x + _leftBlackBand;
				if (wx < 0 || wx >= width)
					continue;
				int sx = (int)((long)x * srcW / dstW);
				int s = (sy * srcW + sx) * 4, d = (wy * width + wx) * 3;
				rgb[d] = _frame[s];
				rgb[d + 1] = _frame[s + 1];
				rgb[d + 2] = _frame[s + 2];
			}
		}
		return (rgb, width, height);
	}

	/// <summary>Clears all the contents out of the internal buffer.</summary>
	public void Clear() => _surface.Clear();

	/// <summary>Converts the indexed screen buffer into the true-colour frame.</summary>
	public void Flip()
	{
		// Palette lookup table holding RGBA byte quadruples, written as one 32-bit store per pixel.
		Span<uint> lut = stackalloc uint[256];
		Span<byte> rgba = stackalloc byte[4];
		for (int i = 0; i < 256; ++i)
		{
			var c = _palette[i];
			rgba[0] = c.R;
			rgba[1] = c.G;
			rgba[2] = c.B;
			rgba[3] = 255;
			lut[i] = MemoryMarshal.Read<uint>(rgba);
		}
		var src = _surface.Pixels;
		var dst = MemoryMarshal.Cast<byte, uint>(_frame.AsSpan());
		for (int i = 0; i < src.Length; ++i)
			dst[i] = lut[src[i]];
		FrameNumber++;
	}

	/// <summary>
	/// Tells the screen the window's client size (the host calls this whenever it changes) and fits the
	/// logical screen into it like resetDisplay does (black bands with <see cref="Options.keepAspectRatio"/>).
	/// </summary>
	public void SetWindowSize(int windowWidth, int windowHeight)
	{
		_windowWidth = windowWidth;
		_windowHeight = windowHeight;
		UpdateBlackBands();
	}

	/// <summary>Scale and black bands, the second half of C++ resetDisplay (getWidth/getHeight = window size).</summary>
	private void UpdateBlackBands()
	{
		_scaleX = _windowWidth / (double)_baseWidth;
		_scaleY = _windowHeight / (double)_baseHeight;

		double pixelRatioY = 1.0;
		if (Options.nonSquarePixelRatio && !Options.allowResize)
		{
			pixelRatioY = 1.2;
		}
		bool cursorInBlackBands;
		if (!Options.keepAspectRatio)
		{
			cursorInBlackBands = false;
		}
		else if (Options.fullscreen)
		{
			cursorInBlackBands = Options.cursorInBlackBandsInFullscreen;
		}
		else if (!Options.borderless)
		{
			cursorInBlackBands = Options.cursorInBlackBandsInWindow;
		}
		else
		{
			cursorInBlackBands = Options.cursorInBlackBandsInBorderlessWindow;
		}

		if (_scaleX > _scaleY && Options.keepAspectRatio)
		{
			int targetWidth = (int)Math.Floor(_scaleY * (double)_baseWidth);
			_topBlackBand = _bottomBlackBand = 0;
			_leftBlackBand = (_windowWidth - targetWidth) / 2;
			if (_leftBlackBand < 0)
			{
				_leftBlackBand = 0;
			}
			_rightBlackBand = _windowWidth - targetWidth - _leftBlackBand;
			_cursorTopBlackBand = 0;

			if (cursorInBlackBands)
			{
				_scaleX = _scaleY;
				_cursorLeftBlackBand = _leftBlackBand;
			}
			else
			{
				_cursorLeftBlackBand = 0;
			}
		}
		else if (_scaleY > _scaleX && Options.keepAspectRatio)
		{
			int targetHeight = (int)Math.Floor(_scaleX * (double)_baseHeight * pixelRatioY);
			_topBlackBand = (_windowHeight - targetHeight) / 2;
			if (_topBlackBand < 0)
			{
				_topBlackBand = 0;
			}
			_bottomBlackBand = _windowHeight - targetHeight - _topBlackBand;
			if (_bottomBlackBand < 0)
			{
				_bottomBlackBand = 0;
			}
			_leftBlackBand = _rightBlackBand = 0;
			_cursorLeftBlackBand = 0;

			if (cursorInBlackBands)
			{
				_scaleY = _scaleX;
				_cursorTopBlackBand = _topBlackBand;
			}
			else
			{
				_cursorTopBlackBand = 0;
			}
		}
		else
		{
			_topBlackBand = _bottomBlackBand = _leftBlackBand = _rightBlackBand = _cursorTopBlackBand = _cursorLeftBlackBand = 0;
		}
	}

	/// <summary>
	/// Updates the base resolution of a scale type (geoscape/battlescape) from the display size.
	/// </summary>
	/// <param name="type">Scale type (<see cref="ScaleType"/> as int, like the options).</param>
	/// <param name="width">Base width to set.</param>
	/// <param name="height">Base height to set.</param>
	/// <param name="change">Also set the current base resolution if it differs.</param>
	public static void UpdateScale(int type, ref int width, ref int height, bool change)
	{
		double pixelRatioY = 1.0;

		if (Options.nonSquarePixelRatio)
		{
			pixelRatioY = 1.2;
		}

		switch ((ScaleType)type)
		{
			case ScaleType.SCALE_15X:
				width = (int)(ORIGINAL_WIDTH * 1.5);
				height = (int)(ORIGINAL_HEIGHT * 1.5);
				break;
			case ScaleType.SCALE_2X:
				width = ORIGINAL_WIDTH * 2;
				height = ORIGINAL_HEIGHT * 2;
				break;
			case ScaleType.SCALE_SCREEN_DIV_10:
				width = (int)(Options.displayWidth / 10.0);
				height = (int)(Options.displayHeight / pixelRatioY / 10.0);
				break;
			case ScaleType.SCALE_SCREEN_DIV_8:
				width = (int)(Options.displayWidth / 8.0);
				height = (int)(Options.displayHeight / pixelRatioY / 8.0);
				break;
			case ScaleType.SCALE_SCREEN_DIV_6:
				width = (int)(Options.displayWidth / 6.0);
				height = (int)(Options.displayHeight / pixelRatioY / 6.0);
				break;
			case ScaleType.SCALE_SCREEN_DIV_5:
				width = (int)(Options.displayWidth / 5.0);
				height = (int)(Options.displayHeight / pixelRatioY / 5.0);
				break;
			case ScaleType.SCALE_SCREEN_DIV_4:
				width = (int)(Options.displayWidth / 4.0);
				height = (int)(Options.displayHeight / pixelRatioY / 4.0);
				break;
			case ScaleType.SCALE_SCREEN_DIV_3:
				width = (int)(Options.displayWidth / 3.0);
				height = (int)(Options.displayHeight / pixelRatioY / 3.0);
				break;
			case ScaleType.SCALE_SCREEN_DIV_2:
				width = (int)(Options.displayWidth / 2.0);
				height = (int)(Options.displayHeight / pixelRatioY / 2.0);
				break;
			case ScaleType.SCALE_SCREEN:
				width = Options.displayWidth;
				height = (int)(Options.displayHeight / pixelRatioY);
				break;
			case ScaleType.SCALE_ORIGINAL:
			default:
				width = ORIGINAL_WIDTH;
				height = ORIGINAL_HEIGHT;
				break;
		}

		// don't go under minimum resolution... it's bad, mmkay?
		width = Math.Max(width, ORIGINAL_WIDTH);
		height = Math.Max(height, ORIGINAL_HEIGHT);

		if (change && (Options.baseXResolution != width || Options.baseYResolution != height))
		{
			Options.baseXResolution = width;
			Options.baseYResolution = height;
		}
	}
}
