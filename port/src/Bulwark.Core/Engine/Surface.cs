/*
 * Copyright 2010-2026 OpenXcom Developers.
 * C# port: Copyright 2026 Bulwark contributors.
 *
 * This file is part of Bulwark, a C# port of OpenXcom Extended.
 *
 * Bulwark is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Bulwark is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Bulwark.  If not, see <http://www.gnu.org/licenses/>.
 */
// Ported from: src/Engine/Surface.h, src/Engine/Surface.cpp
// Port status: partial (missing: image loaders, drawLine/Circle/Polygon/TexturedPolygon/String, SurfaceCrop, GraphSubset blit).

#pragma warning disable CA1815 // SDL_Rect mirrors SDL 1.2; equality is not needed.

namespace Bulwark.Engine;

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
	private readonly int _scale;
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

	/// <summary>Sets whether the surface needs to be redrawn.</summary>
	public void Invalidate(bool valid = true) => _redraw = valid;

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
	private interface IPixelOp
	{
		void Apply(ref byte dest, byte src);
	}

	/// <summary>Copy with colour key 0 (transparent), like SDL_BlitSurface in the original.</summary>
	private readonly struct KeyedCopyOp : IPixelOp
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
	/// Core blit loop: applies <paramref name="op"/> to every destination pixel covered by this
	/// surface placed at logical (x, y), source columns starting at logical <paramref name="begX"/>.
	/// Handles different pixel densities by nearest-neighbour mapping.
	/// </summary>
	private void BlitPixels<TOp>(Surface dest, int x, int y, int begX, TOp op) where TOp : struct, IPixelOp
	{
		int ds = dest._scale, ss = _scale;
		int dpw = dest.PhysicalWidth, dph = dest.PhysicalHeight, spw = PhysicalWidth;
		// Destination area in destination buffer pixels, clipped.
		int dx0 = Math.Max(0, (x + begX) * ds), dy0 = Math.Max(0, y * ds);
		int dx1 = Math.Min(dpw, (x + _width) * ds), dy1 = Math.Min(dph, (y + _height) * ds);
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
