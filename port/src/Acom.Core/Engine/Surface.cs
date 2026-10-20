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
// Ported from: src/Engine/Surface.h, src/Engine/Surface.cpp
// Port status: partial (missing: loadImage of formats other than PNG/GIF (LBM, BMP, ...)).

#pragma warning disable CA1815 // SDL_Rect mirrors SDL 1.2, SurfaceCrop is a blit helper; equality is not needed.

using System.Text;

namespace Acom;

/// <summary>Rectangle with the field layout of SDL 1.2.</summary>
public struct SDL_Rect
{
	public short x, y;
	public ushort w, h;

	public SDL_Rect(int x, int y, int w, int h)
	{
		this.x = (short)x;
		this.y = (short)y;
		this.w = (ushort)w;
		this.h = (ushort)h;
	}
}

/// <summary>
/// Element that is blit (rendered) onto the screen.
/// Mainly an encapsulation of an 8bpp paletted pixel buffer that provides
/// shortcuts for common tasks to make code more readable.
/// </summary>
/// <remarks>
/// <para><b>HD extension:</b> all public coordinates and sizes are <i>logical</i> pixels, exactly as in
/// the C++ code (the UI is laid out on a 320x200 grid). Each surface additionally has a <see cref="Scale"/>
/// (pixel density): its buffer holds <c>Width*Scale x Height*Scale</c> palette indices. Classic art uses
/// scale 1, HD art e.g. scale 4. Blits between different densities use nearest-neighbour mapping, so
/// classic assets keep working inside an HD screen.</para>
/// <para>Palette index 0 is transparent when blitting, like SDL's colour key in the original.</para>
/// </remarks>
public class Surface
{
	/// <summary>Pixel density used for new surfaces; set once at startup (1 = classic, 2..4 = HD).</summary>
	public static int DefaultScale { get; set; } = 1;

	private byte[] _pixels;
	private int _width, _height;
	private int _scale;
	private readonly Rgba[] _palette = new Rgba[256];
	protected int _x, _y;
	protected bool _visible = true, _hidden, _redraw;

	/// <summary>Sets up a blank surface with the specified logical size and position.</summary>
	public Surface(int width, int height, int x = 0, int y = 0) : this(width, height, x, y, DefaultScale)
	{
	}

	/// <summary>Sets up a blank surface with an explicit pixel density.</summary>
	public Surface(int width, int height, int x, int y, int scale)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(width);
		ArgumentOutOfRangeException.ThrowIfNegative(height);
		ArgumentOutOfRangeException.ThrowIfLessThan(scale, 1);
		_width = width;
		_height = height;
		_scale = scale;
		_x = x;
		_y = y;
		_pixels = new byte[width * scale * height * scale];
	}

	/// <summary>Performs a deep copy of an existing surface.</summary>
	public Surface(Surface other)
	{
		ArgumentNullException.ThrowIfNull(other);
		_width = other._width;
		_height = other._height;
		_scale = other._scale;
		_pixels = (byte[])other._pixels.Clone();
		other._palette.CopyTo(_palette, 0);
		_x = other._x;
		_y = other._y;
		_visible = other._visible;
		_hidden = other._hidden;
		_redraw = other._redraw;
	}

	/// <summary>Logical width.</summary>
	public int Width => _width;

	/// <summary>Logical height.</summary>
	public int Height => _height;

	/// <summary>Pixel density (buffer pixels per logical pixel).</summary>
	public int Scale => _scale;

	/// <summary>Buffer width in pixels.</summary>
	public int PhysicalWidth => _width * _scale;

	/// <summary>Buffer height in pixels.</summary>
	public int PhysicalHeight => _height * _scale;

	/// <summary>Raw palette indices, row-major, <see cref="PhysicalWidth"/> per row.</summary>
	public Span<byte> Pixels => _pixels;

	/// <summary>Logical X position (relative to the parent/screen).</summary>
	public virtual int X { get => _x; set => _x = value; }

	/// <summary>Logical Y position (relative to the parent/screen).</summary>
	public virtual int Y { get => _y; set => _y = value; }

	/// <summary>Whether the surface is shown. Only visible surfaces are blit.</summary>
	public virtual bool Visible { get => _visible; set => _visible = value; }

	/// <summary>Hides the surface without changing its visibility.</summary>
	public bool Hidden { get => _hidden; set => _hidden = value; }

	/// <summary>Whether the surface will be redrawn before the next blit.</summary>
	public bool NeedsRedraw => _redraw;

	/// <summary>The surface's 8bpp palette.</summary>
	public ReadOnlySpan<Rgba> Palette => _palette;

	/// <summary>Changes a set of colors on the surface's palette.</summary>
	public virtual void SetPalette(ReadOnlySpan<Rgba> colors, int firstcolor = 0, int ncolors = 256)
	{
		colors[..Math.Min(ncolors, colors.Length)].CopyTo(_palette.AsSpan(firstcolor));
	}

	/// <summary>Changes the logical width, keeping the overlapping content.</summary>
	public virtual void SetWidth(int width)
	{
		Resize(width, _height);
		_redraw = true;
	}

	/// <summary>Changes the logical height, keeping the overlapping content.</summary>
	public virtual void SetHeight(int height)
	{
		Resize(_width, height);
		_redraw = true;
	}

	/// <summary>Recreates the buffer with a new logical size, keeping the overlapping content.</summary>
	public void Resize(int width, int height)
	{
		var pixels = new byte[width * _scale * height * _scale];
		int oldPitch = PhysicalWidth, newPitch = width * _scale;
		int rows = Math.Min(height, _height) * _scale, cols = Math.Min(newPitch, oldPitch);
		for (int y = 0; y < rows; ++y)
			_pixels.AsSpan(y * oldPitch, cols).CopyTo(pixels.AsSpan(y * newPitch));
		_pixels = pixels;
		_width = width;
		_height = height;
	}

	/// <summary>Gets the palette index of a logical pixel; 0 if out of bounds.</summary>
	public byte GetPixel(int x, int y)
	{
		if (x < 0 || x >= _width || y < 0 || y >= _height)
			return 0;
		return _pixels[y * _scale * PhysicalWidth + x * _scale];
	}

	/// <summary>Sets a logical pixel (a Scale x Scale block); ignored if out of bounds.</summary>
	public void SetPixel(int x, int y, byte pixel)
	{
		if (x < 0 || x >= _width || y < 0 || y >= _height)
			return;
		int pitch = PhysicalWidth;
		for (int py = 0; py < _scale; ++py)
			_pixels.AsSpan((y * _scale + py) * pitch + x * _scale, _scale).Fill(pixel);
	}

	/// <summary>
	/// Sets a pixel and moves the position to the next one, row by row
	/// (used by the image decoders).
	/// </summary>
	public void SetPixelIterative(ref int x, ref int y, byte pixel)
	{
		SetPixel(x, y, pixel);
		x++;
		if (x == _width)
		{
			y++;
			x = 0;
		}
	}

	/// <summary>Loads raw palette indices, row by row (extra bytes are ignored).</summary>
	public void LoadRaw(ReadOnlySpan<byte> bytes)
	{
		if (_scale == 1)
		{
			bytes[..Math.Min(bytes.Length, _pixels.Length)].CopyTo(_pixels);
			return;
		}
		int x = 0, y = 0;
		foreach (byte b in bytes[..Math.Min(bytes.Length, _width * _height)])
			SetPixelIterative(ref x, ref y, b);
	}

	/// <summary>Loads an X-Com SCR image: uncompressed palette indices.</summary>
	public void LoadScr(Stream stream)
	{
		ArgumentNullException.ThrowIfNull(stream);
		using var buffer = new MemoryStream();
		stream.CopyTo(buffer);
		LoadRaw(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
	}

	/// <summary>
	/// Loads an X-Com SPK image: 16-bit commands, 0xFFFF n = 2n transparent pixels,
	/// 0xFFFE n = 2n literal pixels, 0xFFFD = end.
	/// </summary>
	public void LoadSpk(Stream stream)
	{
		ArgumentNullException.ThrowIfNull(stream);
		var data = ReadAll(stream);
		int pos = 0, x = 0, y = 0;
		while (pos < data.Length - 1)
		{
			int flag = ReadLE16(data, ref pos);
			if (flag == 0xFFFF)
			{
				int n = ReadLE16(data, ref pos);
				for (int i = 0; i < n * 2; ++i)
					SetPixelIterative(ref x, ref y, 0);
			}
			else if (flag == 0xFFFE)
			{
				int n = ReadLE16(data, ref pos);
				for (int i = 0; i < n * 2; ++i)
					SetPixelIterative(ref x, ref y, pos < data.Length ? data[pos++] : (byte)0);
			}
		}
	}

	/// <summary>
	/// Loads an X-Com BDY image: byte-wise run length encoding,
	/// values &gt;= 129 repeat the next byte 257-n times, otherwise n+1 literal bytes follow.
	/// </summary>
	public void LoadBdy(Stream stream)
	{
		ArgumentNullException.ThrowIfNull(stream);
		var data = ReadAll(stream);
		int pos = 0, x = 0, y = 0;
		while (pos < data.Length)
		{
			byte dataByte = data[pos++];
			if (dataByte >= 129)
			{
				int pixelCnt = 257 - dataByte;
				dataByte = pos < data.Length ? data[pos++] : (byte)0;
				int currentRow = y;
				for (int i = 0; i < pixelCnt; ++i)
				{
					SetPixelIterative(ref x, ref y, dataByte);
					if (currentRow != y) // avoid overscan into next row
						break;
				}
			}
			else
			{
				int pixelCnt = 1 + dataByte;
				int currentRow = y;
				for (int i = 0; i < pixelCnt; ++i)
				{
					dataByte = pos < data.Length ? data[pos++] : (byte)0;
					if (currentRow == y) // avoid overscan into next row
						SetPixelIterative(ref x, ref y, dataByte);
				}
			}
		}
	}

	/// <summary>Loads an image file of a known format (PNG, GIF) through the mod file system.</summary>
	public void LoadImage(string filename, int scale = 1) => LoadImage(FileMap.ReadAll(filename), filename, scale);

	/// <summary>
	/// Loads the contents of an image file of a known format into the surface, replacing it
	/// (size, palette, position and flags) like <c>*this = Surface(width, height, 0, 0)</c> in C++.
	/// PNGs with 8 bits per pixel are read like lodepng (the first palette entry with alpha 0 becomes
	/// transparent), everything else like SDL_image (the colour key becomes transparent).
	/// </summary>
	/// <param name="data">Content of the file.</param>
	/// <param name="filename">Filename of the image (selects the PNG path, used in messages).</param>
	/// <param name="scale">HD extension: pixel density of the file; its size must be a multiple of it.</param>
	/// <exception cref="InvalidDataException">Unknown format, broken file or not an 8-bit image.</exception>
	public void LoadImage(byte[] data, string filename, int scale = 1)
	{
		ArgumentNullException.ThrowIfNull(data);
		ArgumentOutOfRangeException.ThrowIfLessThan(scale, 1);
		Logger.Verbose("Loading image: " + filename);

		IndexedImage? image = null;
		bool lodepng = false;
		// Try loading with LodePNG first
		if (CrossPlatform.CompareExt(filename, "png") && data.Length > 8 + 12 + 12) // minimal PNG file size: header and two empty chunks
		{
			try
			{
				image = ImageDecoder.DecodeLodePng(data);
				lodepng = image != null;
			}
			catch (InvalidDataException e)
			{
				Logger.Error("Image " + filename + " lodepng failed:" + e.Message);
			}
		}
		if (image == null) // Otherwise default to SDL_Image
		{
			try
			{
				image = ImageDecoder.DecodeSdl(data);
			}
			catch (InvalidDataException e)
			{
				throw new InvalidDataException(filename + ":" + e.Message, e);
			}
			if (image == null)
				throw new InvalidDataException(filename + ": ACOM supports only 8bit images.");
		}
		if (image.Width % scale != 0 || image.Height % scale != 0)
			throw new InvalidDataException(filename + ": image size " + image.Width + "x" + image.Height + " is no multiple of the pixel density " + scale + ".");

		_scale = scale;
		_width = image.Width / scale;
		_height = image.Height / scale;
		_pixels = image.Pixels;
		Array.Clear(_palette);
		_x = _y = 0;
		_visible = true;
		_hidden = _redraw = false;
		SetPalette(image.Palette, 0, image.Palette.Length);

		int transparent = image.ColorKey;
		if (lodepng)
		{
			transparent = 0;
			for (int c = 0; c < _palette.Length; ++c)
			{
				if (_palette[c].A == 0)
				{
					transparent = c;
					break;
				}
			}
		}
		if (transparent != 0)
		{
			// graphics should have index 0 as transparent; incorrect ones are changed to 0
			for (int i = 0; i < _pixels.Length; ++i)
			{
				if (_pixels[i] == transparent)
					_pixels[i] = 0;
			}
			Logger.Warning("Image " + filename + (lodepng ? " (from lodepng)" : " (from SDL)") + " has incorrect transparent color index " + transparent + " (instead of 0).");
		}
	}

	private static byte[] ReadAll(Stream stream)
	{
		using var buffer = new MemoryStream();
		stream.CopyTo(buffer);
		return buffer.ToArray();
	}

	private static int ReadLE16(byte[] data, ref int pos)
	{
		int lo = pos < data.Length ? data[pos] : 0;
		int hi = pos + 1 < data.Length ? data[pos + 1] : 0;
		pos += 2;
		return lo | (hi << 8);
	}

	/// <summary>Clears the surface to transparent.</summary>
	public void Clear() => Array.Clear(_pixels);

	/// <summary>Shifts all non-transparent colors by an offset, clamped to [min, max].</summary>
	public void Offset(int off, int min = -1, int max = -1, int mul = 1)
	{
		if (off == 0)
			return;
		for (int i = 0; i < _pixels.Length; ++i)
		{
			byte pixel = _pixels[i];
			int p = off > 0 ? pixel * mul + off : (pixel + off) / mul;
			if (min != -1 && p < min)
				p = min;
			else if (max != -1 && p > max)
				p = max;
			_pixels[i] = pixel > 0 ? unchecked((byte)p) : (byte)0;
		}
	}

	/// <summary>Shifts all non-transparent colors by an offset, clamped to their colour block.</summary>
	public void OffsetBlock(int off, int blk = 16, int mul = 1)
	{
		if (off == 0)
			return;
		for (int i = 0; i < _pixels.Length; ++i)
		{
			byte pixel = _pixels[i];
			int min = pixel / blk * blk;
			int max = min + blk;
			int p = off > 0 ? pixel * mul + off : (pixel + off) / mul;
			if (p < min)
				p = min;
			else if (p > max)
				p = max;
			_pixels[i] = pixel > 0 ? unchecked((byte)p) : (byte)0;
		}
	}

	/// <summary>Inverts all non-transparent colors around a middle color.</summary>
	public void Invert(byte mid)
	{
		for (int i = 0; i < _pixels.Length; ++i)
		{
			byte pixel = _pixels[i];
			_pixels[i] = pixel > 0 ? unchecked((byte)(pixel + 2 * (mid - pixel))) : (byte)0;
		}
	}

	/// <summary>Runs surface functionality every cycle.</summary>
	public virtual void Think()
	{
	}

	/// <summary>Draws the surface's graphic (default: clear).</summary>
	public virtual void Draw()
	{
		_redraw = false;
		Clear();
	}

	/// <summary>Blits this surface onto another one at its own position.</summary>
	public virtual void Blit(Surface surface)
	{
		ArgumentNullException.ThrowIfNull(surface);
		if (_visible && !_hidden)
		{
			if (_redraw)
				Draw();
			BlitPixels(surface, _x, _y, 0, new KeyedCopyOp());
		}
	}

	/// <summary>Gets a helper to blit a part of this surface, positioned at this surface's position.</summary>
	public SurfaceCrop GetCrop() => new(this);

	/// <summary>
	/// Blits the rectangle <paramref name="crop"/> of this surface (whole surface if empty) to
	/// <paramref name="dest"/> with its top-left corner at (x, y); index 0 is transparent.
	/// </summary>
	internal void BlitCrop(Surface dest, int x, int y, SDL_Rect crop) => BlitCrop(dest, x, y, crop, new KeyedCopyOp());

	/// <summary>
	/// Applies <paramref name="op"/> to the rectangle <paramref name="crop"/> of this surface (whole surface if empty)
	/// placed on <paramref name="dest"/> with its top-left corner at (x, y), like <c>ShaderCrop</c> in C++.
	/// </summary>
	internal void BlitCrop<TOp>(Surface dest, int x, int y, SDL_Rect crop, TOp op) where TOp : struct, IPixelOp
	{
		ArgumentNullException.ThrowIfNull(dest);
		int sx0 = 0, sy0 = 0, sx1 = _width, sy1 = _height;
		if (crop.w != 0 || crop.h != 0)
		{
			sx0 = Math.Max(0, (int)crop.x);
			sy0 = Math.Max(0, (int)crop.y);
			sx1 = Math.Min(_width, crop.x + crop.w);
			sy1 = Math.Min(_height, crop.y + crop.h);
			x -= crop.x;
			y -= crop.y;
		}
		if (sx0 < sx1 && sy0 < sy1)
			BlitRect(dest, x, y, sx0, sy0, sx1, sy1, op);
	}

	/// <summary>Copies the part of another surface lying under this surface (including transparent pixels).</summary>
	public void Copy(Surface surface)
	{
		ArgumentNullException.ThrowIfNull(surface);
		// Map every pixel of this surface onto the source: this(q) = surface(q + this.pos - surface.pos).
		int fromX = _x - surface._x, fromY = _y - surface._y;
		int pw = PhysicalWidth, ph = PhysicalHeight;
		for (int py = 0; py < ph; ++py)
		{
			int sy = fromY * surface._scale + py * surface._scale / _scale;
			if (sy < 0 || sy >= surface.PhysicalHeight)
				continue;
			for (int px = 0; px < pw; ++px)
			{
				int sx = fromX * surface._scale + px * surface._scale / _scale;
				if (sx < 0 || sx >= surface.PhysicalWidth)
					continue;
				_pixels[py * pw + px] = surface._pixels[sy * surface.PhysicalWidth + sx];
			}
		}
	}

	/// <summary>Draws a filled rectangle (logical coordinates, clipped).</summary>
	public void DrawRect(SDL_Rect rect, byte color)
	{
		if (rect.w == 0 || rect.h == 0)
			return;
		int x0 = Math.Max(0, (int)rect.x), y0 = Math.Max(0, (int)rect.y);
		int x1 = Math.Min(_width, rect.x + rect.w), y1 = Math.Min(_height, rect.y + rect.h);
		if (x0 >= x1 || y0 >= y1)
			return;
		int pitch = PhysicalWidth;
		for (int py = y0 * _scale; py < y1 * _scale; ++py)
			_pixels.AsSpan(py * pitch + x0 * _scale, (x1 - x0) * _scale).Fill(color);
	}

	/// <summary>Draws a filled rectangle (logical coordinates, clipped).</summary>
	public void DrawRect(short x, short y, short w, short h, byte color)
	{
		if (w == 0 || h == 0)
			return;
		DrawRect(new SDL_Rect(x, y, (ushort)w, (ushort)h), color);
	}

	/// <summary>Fills a logical rectangle that lies inside the surface (no clipping).</summary>
	internal void FillLogical(int x, int y, int w, int h, byte color)
	{
		int pitch = PhysicalWidth;
		if (_scale == 1 && w == 1 && h == 1)
		{
			_pixels[y * pitch + x] = color;
			return;
		}
		for (int py = y * _scale; py < (y + h) * _scale; ++py)
			_pixels.AsSpan(py * pitch + x * _scale, w * _scale).Fill(color);
	}

	// The following primitives come from SDL_gfx in the original (see GfxPrimitives); the coordinates
	// are Sint16 there, so the int arguments are truncated like at the C++ call sites.

	/// <summary>Draws a line on the surface (logical coordinates, clipped, both end points included).</summary>
	/// <param name="x1">Start x coordinate in pixels.</param>
	/// <param name="y1">Start y coordinate in pixels.</param>
	/// <param name="x2">End x coordinate in pixels.</param>
	/// <param name="y2">End y coordinate in pixels.</param>
	/// <param name="color">Color of the line.</param>
	public void DrawLine(int x1, int y1, int x2, int y2, byte color)
	{
		GfxPrimitives.LineColor(this, (short)x1, (short)y1, (short)x2, (short)y2, color);
	}

	/// <summary>Draws a filled circle on the surface.</summary>
	/// <param name="x">X coordinate in pixels.</param>
	/// <param name="y">Y coordinate in pixels.</param>
	/// <param name="r">Radius in pixels.</param>
	/// <param name="color">Color of the circle.</param>
	public void DrawCircle(int x, int y, int r, byte color)
	{
		GfxPrimitives.FilledCircleColor(this, (short)x, (short)y, (short)r, color);
	}

	/// <summary>Draws a filled polygon on the surface.</summary>
	/// <param name="x">Array of x coordinates.</param>
	/// <param name="y">Array of y coordinates.</param>
	/// <param name="n">Number of points.</param>
	/// <param name="color">Color of the polygon.</param>
	public void DrawPolygon(ReadOnlySpan<short> x, ReadOnlySpan<short> y, int n, byte color)
	{
		GfxPrimitives.FilledPolygonColor(this, x, y, n, color);
	}

	/// <summary>Draws a textured polygon on the surface; the texture repeats and its index 0 is transparent.</summary>
	/// <param name="x">Array of x coordinates.</param>
	/// <param name="y">Array of y coordinates.</param>
	/// <param name="n">Number of points.</param>
	/// <param name="texture">Texture for polygon.</param>
	/// <param name="dx">X offset of texture relative to the screen.</param>
	/// <param name="dy">Y offset of texture relative to the screen.</param>
	public void DrawTexturedPolygon(ReadOnlySpan<short> x, ReadOnlySpan<short> y, int n, Surface texture, int dx, int dy)
	{
		ArgumentNullException.ThrowIfNull(texture);
		GfxPrimitives.TexturedPolygon(this, x, y, n, texture, dx, dy);
	}

	/// <summary>Draws a text string with SDL_gfx's built-in 8x8 font; each UTF-8 byte is one character, as in C++.</summary>
	/// <param name="x">X coordinate in pixels.</param>
	/// <param name="y">Y coordinate in pixels.</param>
	/// <param name="s">Character string to draw.</param>
	/// <param name="color">Color of string.</param>
	public void DrawString(int x, int y, string s, byte color)
	{
		ArgumentNullException.ThrowIfNull(s);
		DrawString(x, y, Encoding.UTF8.GetBytes(s), color);
	}

	/// <summary>Draws a zero-terminated byte string with SDL_gfx's built-in 8x8 font (code page 437 glyphs).</summary>
	public void DrawString(int x, int y, ReadOnlySpan<byte> s, byte color)
	{
		GfxPrimitives.StringColor(this, (short)x, (short)y, s, color);
	}

	/// <summary>
	/// Blits this surface onto another one with shading and optional colour replacement.
	/// </summary>
	/// <param name="surface">Destination.</param>
	/// <param name="x">Logical X offset in the destination.</param>
	/// <param name="y">Logical Y offset in the destination.</param>
	/// <param name="shade">Shade offset (0 = unchanged, 15 = darkest).</param>
	/// <param name="half">Only draw the right half of the source.</param>
	/// <param name="newBaseColor">1-based colour group to recolour into (0 = keep colours).</param>
	public void BlitNShade(Surface surface, int x, int y, int shade = 0, bool half = false, int newBaseColor = 0)
	{
		ArgumentNullException.ThrowIfNull(surface);
		int begX = half ? _width / 2 : 0;
		if (newBaseColor != 0)
		{
			BlitPixels(surface, x, y, begX, new ColorReplaceOp(shade, (newBaseColor - 1) << 4));
		}
		else
		{
			BlitPixels(surface, x, y, begX, new StandardShadeOp(shade));
		}
	}

	/// <summary>
	/// Specific blit function to blit battlescape terrain data in different shades in a fast way
	/// (C++ takes raw buffers; the port blits surfaces, see <see cref="BlitNShade(Surface, int, int, int, bool, int)"/>).
	/// </summary>
	public static void BlitRaw(Surface dest, Surface src, int x, int y, int shade, bool half = false, int newBaseColor = 0)
	{
		ArgumentNullException.ThrowIfNull(src);
		src.BlitNShade(dest, x, y, shade, half, newBaseColor);
	}

	/// <summary>
	/// Blits this surface onto another one with shading, limited to an area of the destination.
	/// </summary>
	/// <param name="surface">Destination.</param>
	/// <param name="x">Logical X offset in the destination.</param>
	/// <param name="y">Logical Y offset in the destination.</param>
	/// <param name="shade">Shade offset.</param>
	/// <param name="range">Area of the destination (logical pixels) that limits drawing.</param>
	public void BlitNShade(Surface surface, int x, int y, int shade, GraphSubset range)
	{
		ArgumentNullException.ThrowIfNull(surface);
		// Source rectangle whose destination lies inside the range.
		int sx0 = Math.Max(0, range.beg_x - x), sy0 = Math.Max(0, range.beg_y - y);
		int sx1 = Math.Min(_width, range.end_x - x), sy1 = Math.Min(_height, range.end_y - y);
		if (sx0 < sx1 && sy0 < sy1)
			BlitRect(surface, x, y, sx0, sy0, sx1, sy1, new StandardShadeOp(shade));
	}

	/// <summary>Sets whether the surface needs to be redrawn.</summary>
	public void Invalidate(bool valid = true) => _redraw = valid;

	/// <summary>Initializes the surface's various text resources (fonts and language); used by text widgets.</summary>
	public virtual void InitText(Font big, Font small, Language lang)
	{
	}

#pragma warning disable CA1822 // instance methods like C++ lock()/unlock(), so drawing code ports 1:1
	/// <summary>Locks the surface for pixel access; nothing to do in the port (kept for 1:1 drawing code).</summary>
	public void Lock()
	{
	}

	/// <summary>Unlocks the surface after <see cref="Lock"/>; nothing to do in the port.</summary>
	public void Unlock()
	{
	}
#pragma warning restore CA1822

	/// <summary>Sets the main color of a widget; ignored by plain surfaces.</summary>
	public virtual void SetColor(byte color)
	{
	}

	/// <summary>Sets the secondary color of a widget; ignored by plain surfaces.</summary>
	public virtual void SetSecondaryColor(byte color)
	{
	}

	/// <summary>Sets the border color of a widget; ignored by plain surfaces.</summary>
	public virtual void SetBorderColor(byte color)
	{
	}

	/// <summary>Sets high contrast mode of a widget; ignored by plain surfaces.</summary>
	public virtual void SetHighContrast(bool contrast)
	{
	}


	/// <summary>Per-pixel blit operation; a struct type parameter lets the JIT inline it into the loop.</summary>
	internal interface IPixelOp
	{
		void Apply(ref byte dest, byte src);
	}

	/// <summary>Copy with colour key 0 (transparent), like SDL_BlitSurface in the original.</summary>
	internal readonly struct KeyedCopyOp : IPixelOp
	{
		public void Apply(ref byte dest, byte src)
		{
			if (src != 0)
				dest = src;
		}
	}

	private readonly struct StandardShadeOp(int shade) : IPixelOp
	{
		public void Apply(ref byte dest, byte src) => ShaderDrawHelper.StandardShade(ref dest, src, shade);
	}

	private readonly struct ColorReplaceOp(int shade, int newColor) : IPixelOp
	{
		public void Apply(ref byte dest, byte src) => ShaderDrawHelper.ColorReplace(ref dest, src, shade, newColor);
	}

	/// <summary>
	/// Applies <paramref name="op"/> to every destination pixel covered by this surface
	/// placed at logical (x, y), source columns starting at logical <paramref name="begX"/>.
	/// </summary>
	private void BlitPixels<TOp>(Surface dest, int x, int y, int begX, TOp op) where TOp : struct, IPixelOp
	{
		BlitRect(dest, x, y, begX, 0, _width, _height, op);
	}

	/// <summary>
	/// Core blit loop: applies <paramref name="op"/> to every destination pixel covered by the logical source
	/// rectangle [sx0, sx1) x [sy0, sy1) (inside this surface); source pixel (c, r) lands on (x + c, y + r).
	/// Handles different pixel densities by nearest-neighbour mapping.
	/// </summary>
	internal void BlitRect<TOp>(Surface dest, int x, int y, int sx0, int sy0, int sx1, int sy1, TOp op) where TOp : struct, IPixelOp
	{
		int ds = dest._scale, ss = _scale;
		int dpw = dest.PhysicalWidth, dph = dest.PhysicalHeight, spw = PhysicalWidth;
		// Destination area in destination buffer pixels, clipped.
		int dx0 = Math.Max(0, (x + sx0) * ds), dy0 = Math.Max(0, (y + sy0) * ds);
		int dx1 = Math.Min(dpw, (x + sx1) * ds), dy1 = Math.Min(dph, (y + sy1) * ds);
		if (dx0 >= dx1 || dy0 >= dy1)
			return;
		var dpix = dest._pixels;
		var spix = _pixels;
		if (ds == ss)
		{
			for (int dy = dy0; dy < dy1; ++dy)
			{
				int drow = dy * dpw, srow = (dy - y * ds) * spw - x * ds;
				for (int dx = dx0; dx < dx1; ++dx)
					op.Apply(ref dpix[drow + dx], spix[srow + dx]);
			}
		}
		else
		{
			// Nearest-neighbour mapping: source column of every destination column, computed once.
			int cols = dx1 - dx0;
			int[]? rented = cols > 4096 ? System.Buffers.ArrayPool<int>.Shared.Rent(cols) : null;
			Span<int> srcCol = rented ?? stackalloc int[cols];
			for (int i = 0; i < cols; ++i)
				srcCol[i] = (dx0 + i - x * ds) * ss / ds;
			for (int dy = dy0; dy < dy1; ++dy)
			{
				int drow = dy * dpw + dx0, srow = (dy - y * ds) * ss / ds * spw;
				for (int i = 0; i < cols; ++i)
					op.Apply(ref dpix[drow + i], spix[srow + srcCol[i]]);
			}
			if (rented != null)
				System.Buffers.ArrayPool<int>.Shared.Return(rented);
		}
	}
}

/// <summary>Helper used to blit part of a surface to another one.</summary>
public struct SurfaceCrop
{
	/// <summary>Crop rectangle in the surface; empty = whole surface.</summary>
	public SDL_Rect Crop;

	/// <summary>Creates a crop helper positioned at the surface's position.</summary>
	public SurfaceCrop(Surface surf)
	{
		ArgumentNullException.ThrowIfNull(surf);
		Surface = surf;
		X = surf.X;
		Y = surf.Y;
	}

	/// <summary>Gets the surface.</summary>
	public readonly Surface? Surface { get; }

	/// <summary>Position on the destination in the X axis.</summary>
	public int X { get; set; }

	/// <summary>Position on the destination in the Y axis.</summary>
	public int Y { get; set; }

	/// <summary>Blits the cropped surface to another surface.</summary>
	public readonly void Blit(Surface dest) => Surface?.BlitCrop(dest, X, Y, Crop);

	/// <summary>Applies a pixel operation to the cropped surface placed on another surface (ShaderDraw with ShaderCrop).</summary>
	internal readonly void Blit<TOp>(Surface dest, TOp op) where TOp : struct, Surface.IPixelOp => Surface?.BlitCrop(dest, X, Y, Crop, op);
}
