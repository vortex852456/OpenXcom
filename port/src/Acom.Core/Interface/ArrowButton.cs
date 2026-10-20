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
// Ported from: src/Interface/ArrowButton.h, src/Interface/ArrowButton.cpp
#nullable disable

namespace Acom;

public enum ArrowShape : uint
{
	ARROW_NONE = 0,
	ARROW_BIG_UP = 1,
	ARROW_BIG_DOWN = 2,
	ARROW_SMALL_UP = 3,
	ARROW_SMALL_DOWN = 4,
	ARROW_SMALL_LEFT = 5,
	ARROW_SMALL_RIGHT = 6,
}

/// <summary>
/// Button with an arrow on it. Can be used for
/// scrolling lists, spinners, etc. Contains various
/// arrow shapes.
/// </summary>
public class ArrowButton : ImageButton
{
	private ArrowShape _shape;
	private TextList _list;
	private readonly Timer _timer;

	/// <summary>Sets up an arrow button with the specified size and position.</summary>
	/// <param name="shape">Shape of the arrow.</param>
	/// <param name="width">Width in pixels.</param>
	/// <param name="height">Height in pixels.</param>
	/// <param name="x">X position in pixels.</param>
	/// <param name="y">Y position in pixels.</param>
	public ArrowButton(ArrowShape shape, int width, int height, int x = 0, int y = 0) : base(width, height, x, y)
	{
		_shape = shape;
		_timer = new Timer(50);
		_timer.OnTimer((SurfaceHandler)Scroll);
	}

	protected override bool IsButtonHandled(byte button = 0)
	{
		if (_list != null)
			return button == SDLMouse.SDL_BUTTON_LEFT || button == SDLMouse.SDL_BUTTON_RIGHT;
		else
			return base.IsButtonHandled(button);
	}

	/// <summary>Changes the color for the image button.</summary>
	public override void SetColor(byte color)
	{
		base.SetColor(color);
		_redraw = true;
	}

	/// <summary>Changes the shape for the arrow button.</summary>
	public void SetShape(ArrowShape shape)
	{
		_shape = shape;
		_redraw = true;
	}

	/// <summary>Changes the list associated with the arrow button. This makes the button scroll that list.</summary>
	public void SetTextList(TextList list) => _list = list;

	/// <summary>Draws a filled rectangle; the colour is truncated to a palette index like the C++ Uint8 parameter.</summary>
	private void Fill(ref SDL_Rect square, int color) => DrawRect(ref square, unchecked((byte)color));

	/// <summary>Draws the button with the specified arrow shape.</summary>
	public override void Draw()
	{
		base.Draw();
		Lock();

		// Draw button
		var square = new SDL_Rect();
		int color = _color + 2;

		square.x = 0;
		square.y = 0;
		square.w = unchecked((ushort)(Width - 1));
		square.h = unchecked((ushort)(Height - 1));

		Fill(ref square, color);

		square.x++;
		square.y++;
		color = _color + 5;

		Fill(ref square, color);

		square.w--;
		square.h--;
		color = _color + 4;

		Fill(ref square, color);

		SetPixel(0, 0, unchecked((byte)(_color + 1)));
		SetPixel(0, Height - 1, unchecked((byte)(_color + 4)));
		SetPixel(Width - 1, 0, unchecked((byte)(_color + 4)));

		color = _color + 1;

		switch (_shape)
		{
			case ArrowShape.ARROW_BIG_UP:
				// Draw arrow square
				square.x = 5;
				square.y = 8;
				square.w = 3;
				square.h = 3;

				Fill(ref square, color);

				// Draw arrow triangle
				square.x = 2;
				square.y = 7;
				square.w = 9;
				square.h = 1;

				for (; square.w > 1; square.w -= 2)
				{
					Fill(ref square, color);
					square.x++;
					square.y--;
				}
				Fill(ref square, color);
				break;
			case ArrowShape.ARROW_BIG_DOWN:
				// Draw arrow square
				square.x = 5;
				square.y = 3;
				square.w = 3;
				square.h = 3;

				Fill(ref square, color);

				// Draw arrow triangle
				square.x = 2;
				square.y = 6;
				square.w = 9;
				square.h = 1;

				for (; square.w > 1; square.w -= 2)
				{
					Fill(ref square, color);
					square.x++;
					square.y++;
				}
				Fill(ref square, color);
				break;
			case ArrowShape.ARROW_SMALL_UP:
				// Draw arrow triangle 1
				square.x = 1;
				square.y = 5;
				square.w = 9;
				square.h = 1;

				for (; square.w > 1; square.w -= 2)
				{
					Fill(ref square, color + 2);
					square.x++;
					square.y--;
				}
				Fill(ref square, color + 2);

				// Draw arrow triangle 2
				square.x = 2;
				square.y = 5;
				square.w = 7;
				square.h = 1;

				for (; square.w > 1; square.w -= 2)
				{
					Fill(ref square, color);
					square.x++;
					square.y--;
				}
				Fill(ref square, color);
				break;
			case ArrowShape.ARROW_SMALL_DOWN:
				// Draw arrow triangle 1
				square.x = 1;
				square.y = 2;
				square.w = 9;
				square.h = 1;

				for (; square.w > 1; square.w -= 2)
				{
					Fill(ref square, color + 2);
					square.x++;
					square.y++;
				}
				Fill(ref square, color + 2);

				// Draw arrow triangle 2
				square.x = 2;
				square.y = 2;
				square.w = 7;
				square.h = 1;

				for (; square.w > 1; square.w -= 2)
				{
					Fill(ref square, color);
					square.x++;
					square.y++;
				}
				Fill(ref square, color);
				break;
			case ArrowShape.ARROW_SMALL_LEFT:
				// Draw arrow triangle 1
				square.x = 2;
				square.y = 4;
				square.w = 2;
				square.h = 1;

				for (; square.h < 5; square.h += 2)
				{
					Fill(ref square, color + 2);
					square.x += 2;
					square.y--;
				}
				square.w = 1;
				Fill(ref square, color + 2);

				// Draw arrow triangle 2
				square.x = 3;
				square.y = 4;
				square.w = 2;
				square.h = 1;

				for (; square.h < 5; square.h += 2)
				{
					Fill(ref square, color);
					square.x += 2;
					square.y--;
				}
				square.w = 1;
				Fill(ref square, color);
				break;
			case ArrowShape.ARROW_SMALL_RIGHT:
				// Draw arrow triangle 1
				square.x = 7;
				square.y = 4;
				square.w = 2;
				square.h = 1;

				for (; square.h < 5; square.h += 2)
				{
					Fill(ref square, color + 2);
					square.x -= 2;
					square.y--;
				}
				square.x++;
				square.w = 1;
				Fill(ref square, color + 2);

				// Draw arrow triangle 2
				square.x = 6;
				square.y = 4;
				square.w = 2;
				square.h = 1;

				for (; square.h < 5; square.h += 2)
				{
					Fill(ref square, color);
					square.x -= 2;
					square.y--;
				}
				square.x++;
				square.w = 1;
				Fill(ref square, color);
				break;
			default:
				break;
		}

		Unlock();
	}

	/// <summary>Keeps the scrolling timers running.</summary>
	public override void Think() => _timer.Think(null, this);

	/// <summary>Scrolls the list.</summary>
	public void Scroll()
	{
		if (_shape == ArrowShape.ARROW_BIG_UP)
			_list.ScrollUp(false);
		else if (_shape == ArrowShape.ARROW_BIG_DOWN)
			_list.ScrollDown(false);
	}

	/// <summary>Starts scrolling the associated list.</summary>
	public override void MousePress(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		base.MousePress(action, state);
		if (_list != null)
		{
			byte button = action.Details.button.button;
			if (button == SDLMouse.SDL_BUTTON_LEFT)
				_timer.Start();
			else if (button == SDLMouse.SDL_BUTTON_WHEELUP)
				_list.ScrollUp(false, true);
			else if (button == SDLMouse.SDL_BUTTON_WHEELDOWN)
				_list.ScrollDown(false, true);
		}
	}

	/// <summary>Stops scrolling the associated list.</summary>
	public override void MouseRelease(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		base.MouseRelease(action, state);
		if (_list != null && action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
			_timer.Stop();
	}

	/// <summary>Scrolls the associated list to top or bottom.</summary>
	public override void MouseClick(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		base.MouseClick(action, state);
		if (_list != null && SDLMouse.SDL_BUTTON_RIGHT == action.Details.button.button)
		{
			if (_shape == ArrowShape.ARROW_BIG_UP)
				_list.ScrollUp(true);
			else if (_shape == ArrowShape.ARROW_BIG_DOWN)
				_list.ScrollDown(true);
		}
	}
}
