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
// Ported from: src/Interface/ScrollBar.h, src/Interface/ScrollBar.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Vertical scrollbar control attached to a <see cref="TextList"/>.
/// </summary>
public class ScrollBar : InteractiveSurface
{
	private TextList _list;
	private byte _color;
	private bool _pressed, _contrast;
	private readonly Surface _track, _thumb;
	private SDL_Rect _thumbRect;
	private int _offset;
	private Surface _bg;

	/// <summary>Creates a new scrollbar with the specified size and position.</summary>
	public ScrollBar(int width, int height, int x = 0, int y = 0) : base(width, height, x, y)
	{
		_track = new Surface(width - 2, height, x + 1, y);
		_thumb = new Surface(width, height, x, y);
		_thumbRect = new SDL_Rect(0, 0, 0, 0);
	}

	/// <summary>Changes the position of the surface in the X axis.</summary>
	public override int X
	{
		get => base.X;
		set
		{
			base.X = value;
			_track.X = value + 1;
			_thumb.X = value;
		}
	}

	/// <summary>Changes the position of the surface in the Y axis.</summary>
	public override int Y
	{
		get => base.Y;
		set
		{
			base.Y = value;
			_track.Y = value;
			_thumb.Y = value;
		}
	}

	/// <summary>Changes the height of the scrollbar.</summary>
	public override void SetHeight(int height)
	{
		base.SetHeight(height);
		_track.SetHeight(height);
		_thumb.SetHeight(height);
		_redraw = true;
	}

	/// <summary>Changes the color used to render the scrollbar.</summary>
	public override void SetColor(byte color) => _color = color;

	/// <summary>The color used to render the scrollbar.</summary>
	public byte Color { get => _color; set => SetColor(value); }

	/// <summary>Enables/disables high contrast color. Mostly used for Battlescape text.</summary>
	public override void SetHighContrast(bool contrast) => _contrast = contrast;

	/// <summary>Changes the list associated with the scrollbar. This makes the button scroll that list.</summary>
	public void SetTextList(TextList list) => _list = list;

	/// <summary>Changes the surface used to draw the background of the track.</summary>
	public void SetBackground(Surface bg) => _bg = bg;

	/// <summary>Replaces a certain amount of colors in the scrollbar's palette.</summary>
	public override void SetPalette(ReadOnlySpan<Rgba> colors, int firstcolor = 0, int ncolors = 256)
	{
		base.SetPalette(colors, firstcolor, ncolors);
		_track.SetPalette(colors, firstcolor, ncolors);
		_thumb.SetPalette(colors, firstcolor, ncolors);
	}

	/// <summary>Automatically updates the scrollbar when the mouse moves.</summary>
	public override void Handle(Action action, State state)
	{
		base.Handle(action, state);
		var type = action.Details.type;
		if (_pressed && (type == SDLEventType.SDL_MOUSEMOTION || type == SDLEventType.SDL_MOUSEBUTTONDOWN))
		{
			int cursorY = (int)(action.AbsoluteYMouse - Y);
			int y = Math.Min(Math.Max(cursorY + _offset, 0), Height - _thumbRect.h + 1);
			double scale = (double)_list.RowsDoNotUse / Height;
			int scroll = (int)Round(y * scale);
			_list.ScrollTo(scroll);
		}
	}

	/// <summary>fmath.h Round(): rounds half away from zero.</summary>
	private static double Round(double x) => x < 0.0 ? Math.Ceiling(x - 0.5) : Math.Floor(x + 0.5);

	/// <summary>Blits the scrollbar contents.</summary>
	public override void Blit(Surface surface)
	{
		base.Blit(surface);
		if (_visible && !_hidden)
		{
			_track.Blit(surface);
			_thumb.Blit(surface);
			Invalidate();
		}
	}

	/// <summary>The scrollbar only moves while the button is pressed.</summary>
	public override void MousePress(Action action, State state)
	{
		base.MousePress(action, state);
		byte button = action.Details.button.button;
		if (button == SDLMouse.SDL_BUTTON_LEFT)
		{
			int cursorY = (int)(action.AbsoluteYMouse - Y);
			if (cursorY >= _thumbRect.y && cursorY < _thumbRect.y + _thumbRect.h)
			{
				_offset = _thumbRect.y - cursorY;
			}
			else
			{
				_offset = -_thumbRect.h / 2;
			}
			_pressed = true;
		}
		else if (button == SDLMouse.SDL_BUTTON_WHEELUP)
		{
			_list.ScrollUp(false, true);
		}
		else if (button == SDLMouse.SDL_BUTTON_WHEELDOWN)
		{
			_list.ScrollDown(false, true);
		}
	}

	/// <summary>The scrollbar stops moving when the button is released.</summary>
	public override void MouseRelease(Action action, State state)
	{
		base.MouseRelease(action, state);
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
		{
			_pressed = false;
			_offset = 0;
		}
	}

	/// <summary>Updates the thumb according to the current list position.</summary>
	public override void Draw()
	{
		base.Draw();
		DrawTrack();
		DrawThumb();
	}

	/// <summary>Draws the track (background bar) semi-transparent.</summary>
	private void DrawTrack()
	{
		if (_bg != null)
		{
			_track.Copy(_bg);
			if (_list.ComboBox != null)
			{
				_track.Offset(+1, Acom.Palette.BackPos);
			}
			else
			{
				_track.OffsetBlock(-5);
			}
		}
	}

	/// <summary>Draws the thumb (button) as a hollow square.</summary>
	private void DrawThumb()
	{
		double scale = (double)Height / _list.RowsDoNotUse;
		_thumbRect.x = 0;
		_thumbRect.y = (short)(int)Math.Floor(_list.Scroll * scale);
		_thumbRect.w = (ushort)_thumb.Width;
		_thumbRect.h = (ushort)(int)Math.Ceiling(_list.VisibleRows * scale);

		// Draw base button
		_thumb.Clear();

		SDL_Rect square = _thumbRect;
		int color = _color + 2;

		square.w--;
		square.h--;

		_thumb.DrawRect(ref square, (byte)color);

		square.x++;
		square.y++;
		color = _color + 5;

		_thumb.DrawRect(ref square, (byte)color);

		square.w--;
		square.h--;
		color = _color + 4;

		_thumb.DrawRect(ref square, (byte)color);

		_thumb.SetPixel(_thumbRect.x, _thumbRect.y, (byte)(_color + 1));
		_thumb.SetPixel(_thumbRect.x, _thumbRect.y + _thumbRect.h - 1, (byte)(_color + 4));
		_thumb.SetPixel(_thumbRect.x + _thumbRect.w - 1, _thumbRect.y, (byte)(_color + 4));

		// Hollow it out
		if (square.h - 4 > 0)
		{
			color = _color + 5;

			square.x++;
			square.y++;
			square.w -= 3;
			square.h -= 3;

			_thumb.DrawRect(ref square, (byte)color);

			square.x++;
			square.y++;
			color = _color + 2;

			_thumb.DrawRect(ref square, (byte)color);

			square.w--;
			square.h--;
			color = 0;

			_thumb.DrawRect(ref square, (byte)color);

			_thumb.SetPixel(_thumbRect.x + 2 + _thumbRect.w - 1 - 4, _thumbRect.y + 2 + _thumbRect.h - 1 - 4, (byte)(_color + 1));
			_thumb.SetPixel(_thumbRect.x + 2, _thumbRect.y + 2 + _thumbRect.h - 1 - 4, (byte)(_color + 4));
			_thumb.SetPixel(_thumbRect.x + 2 + _thumbRect.w - 1 - 4, _thumbRect.y + 2, (byte)(_color + 4));
		}
	}
}
