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
// Port status: partial (no OpenGL/shader/scaler paths; the platform host scales the frame on the GPU; missing: resetDisplay, updateScale).

using System.Runtime.InteropServices;

namespace Acom;

/// <summary>
/// A display screen: holds the logical game screen surface and converts it into a true-colour
/// frame for the platform host. Also knows how the logical screen is mapped into the window
/// (scale and black bands), which <see cref="Action"/> needs to translate mouse coordinates.
/// </summary>
public sealed class Screen
{
	public const int ORIGINAL_WIDTH = 320;
	public const int ORIGINAL_HEIGHT = 200;

	private readonly Surface _surface;
	private readonly Rgba[] _palette = new Rgba[256];
	private readonly byte[] _frame;

	/// <param name="baseWidth">Logical width (320 for the classic layout).</param>
	/// <param name="baseHeight">Logical height (200 for the classic layout).</param>
	/// <param name="scale">Pixel density of the screen buffer (1 = classic, 4 = HD).</param>
	public Screen(int baseWidth = ORIGINAL_WIDTH, int baseHeight = ORIGINAL_HEIGHT, int scale = 1)
	{
		_surface = new Surface(baseWidth, baseHeight, 0, 0, scale);
		_frame = new byte[_surface.PhysicalWidth * _surface.PhysicalHeight * 4];
		SetWindowSize(_surface.PhysicalWidth, _surface.PhysicalHeight);
	}

	/// <summary>The surface everything is blit onto.</summary>
	public Surface Surface => _surface;

	/// <summary>Logical width.</summary>
	public int Width => _surface.Width;

	/// <summary>Logical height.</summary>
	public int Height => _surface.Height;

	/// <summary>Horizontal offset of the 320x200 layout centered on the logical screen.</summary>
	public int DX => (Width - ORIGINAL_WIDTH) / 2;

	/// <summary>Vertical offset of the 320x200 layout centered on the logical screen.</summary>
	public int DY => (Height - ORIGINAL_HEIGHT) / 2;

	/// <summary>Window pixels per logical pixel (horizontal).</summary>
	public double XScale { get; private set; }

	/// <summary>Window pixels per logical pixel (vertical).</summary>
	public double YScale { get; private set; }

	/// <summary>Height of the black band above the image, in window pixels.</summary>
	public int CursorTopBlackBand { get; private set; }

	/// <summary>Width of the black band left of the image, in window pixels.</summary>
	public int CursorLeftBlackBand { get; private set; }

	/// <summary>Size of the scaled image inside the window, in window pixels.</summary>
	public (int Width, int Height) DisplaySize { get; private set; }

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

	/// <summary>Resets the screen surfaces based on the current display options (not ported yet).</summary>
	public void ResetDisplay(bool resetVideo = true, bool noShaders = false) => throw new NotImplementedException();


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
	/// Fits the logical screen into a window of the given size, keeping the aspect ratio
	/// (black bands fill the rest).
	/// </summary>
	public void SetWindowSize(int windowWidth, int windowHeight)
	{
		double scale = Math.Min((double)windowWidth / Width, (double)windowHeight / Height);
		int w = (int)Math.Round(Width * scale), h = (int)Math.Round(Height * scale);
		XScale = (double)w / Width;
		YScale = (double)h / Height;
		CursorLeftBlackBand = (windowWidth - w) / 2;
		CursorTopBlackBand = (windowHeight - h) / 2;
		DisplaySize = (w, h);
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
