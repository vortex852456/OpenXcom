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
// Ported from: src/Interface/Window.h, src/Interface/Window.cpp
#nullable disable

namespace Acom;

/// <summary>Enumeration for the type of animation when a window pops up.</summary>
public enum WindowPopup : uint
{
	POPUP_NONE = 0,
	POPUP_HORIZONTAL = 1,
	POPUP_VERTICAL = 2,
	POPUP_BOTH = 3,
}

/// <summary>
/// Box with a coloured border and custom background.
/// Pretty much used as the background in most of the interface. In fact
/// it's also used in screens, so it's not really much of a window, just a... box.
/// But box sounds lame.
/// </summary>
public class Window : Surface
{
	private const double POPUP_SPEED = 0.05;

#pragma warning disable CA2211 // static sounds like the C++ class member, set by the game when sounds load
	public static Sound[] soundPopup = new Sound[3];
#pragma warning restore CA2211

	private int _dx, _dy;
	private Surface _bg;
	private byte _color;
	private readonly WindowPopup _popup;
	private double _popupStep;
	private readonly Timer _timer;
	private readonly State _state;
	private bool _contrast, _screen, _thinBorder;
	private byte _innerColor;
	private bool _mute;

	/// <summary>Sets up a blank window with the specified size and position.</summary>
	/// <param name="state">State the window belongs to.</param>
	/// <param name="width">Width in pixels.</param>
	/// <param name="height">Height in pixels.</param>
	/// <param name="x">X position in pixels.</param>
	/// <param name="y">Y position in pixels.</param>
	/// <param name="popup">Popup animation.</param>
	public Window(State state, int width, int height, int x = 0, int y = 0, WindowPopup popup = WindowPopup.POPUP_NONE) : base(width, height, x, y)
	{
		_dx = -x;
		_dy = -y;
		_popup = popup;
		_state = state;
		_timer = new Timer(10);
		_timer.OnTimer((SurfaceHandler)Popup);

		if (_popup == WindowPopup.POPUP_NONE)
		{
			_popupStep = 1.0;
		}
		else
		{
			Hidden = true;
			_timer.Start();
			if (_state != null)
			{
				_screen = state.IsScreen;
				if (_screen)
					_state.ToggleScreen();
			}
		}
	}

	/// <summary>Changes the surface used to draw the background of the window.</summary>
	public void SetBackground(Surface bg)
	{
		_bg = bg;
		_redraw = true;
	}

	/// <summary>Changes the color used to draw the shaded border.</summary>
	public override void SetColor(byte color)
	{
		_color = color;
		_redraw = true;
	}

	/// <summary>Returns the color used to draw the shaded border.</summary>
	public byte Color => _color;

	/// <summary>Enables/disables high contrast color. Mostly used for Battlescape UI.</summary>
	public override void SetHighContrast(bool contrast)
	{
		_contrast = contrast;
		_redraw = true;
	}

	/// <summary>Keeps the animation timers running.</summary>
	public override void Think()
	{
		if (_hidden && _popupStep < 1.0)
		{
			_state.HideAll();
			Hidden = false;
		}

		_timer.Think(null, this);
	}

	/// <summary>fmath AreSame: equal within DBL_EPSILON relative to the larger magnitude (at least 1).</summary>
	private static bool AreSame(double l, double r) => Math.Abs(l - r) <= 2.2204460492503131e-16 * Math.Max(1.0, Math.Max(Math.Abs(l), Math.Abs(r)));

	/// <summary>Plays the window popup animation.</summary>
	public void Popup()
	{
		if (!_mute && AreSame(_popupStep, 0.0))
		{
			int sound = Rng.Seedless(0, 2);
			soundPopup[sound]?.Play(SDLMixer.Mix_GroupAvailable(0));
		}
		if (_popupStep < 1.0)
		{
			_popupStep += POPUP_SPEED;
		}
		else
		{
			if (_screen)
				_state.ToggleScreen();
			_state.ShowAll();
			_popupStep = 1.0;
			_timer.Stop();
		}
		_redraw = true;
	}

	/// <summary>
	/// Draws the bordered window with a graphic background.
	/// The background never moves with the window, it's
	/// always aligned to the top-left corner of the screen
	/// and cropped to fit the inside area.
	/// </summary>
	public override void Draw()
	{
		base.Draw();
		var square = new SDL_Rect();

		if (_popup == WindowPopup.POPUP_HORIZONTAL || _popup == WindowPopup.POPUP_BOTH)
		{
			square.x = (short)(int)((Width - Width * _popupStep) / 2);
			square.w = (ushort)(int)(Width * _popupStep);
		}
		else
		{
			square.x = 0;
			square.w = (ushort)Width;
		}
		if (_popup == WindowPopup.POPUP_VERTICAL || _popup == WindowPopup.POPUP_BOTH)
		{
			square.y = (short)(int)((Height - Height * _popupStep) / 2);
			square.h = (ushort)(int)(Height * _popupStep);
		}
		else
		{
			square.y = 0;
			square.h = (ushort)Height;
		}

		int mul = 1;
		if (_contrast)
			mul = 2;
		byte color = unchecked((byte)(_color + 3 * mul));

		if (_thinBorder)
		{
			color = unchecked((byte)(_color + 1 * mul));
			for (int i = 0; i < 5; ++i)
			{
				DrawRect(square, color);

				if (i % 2 == 0)
				{
					square.x++;
					square.y++;
				}
				square.w--;
				square.h--;

				switch (i)
				{
					case 0:
						color = unchecked((byte)(_color + 5 * mul));
						SetPixel(square.w, 0, color);
						break;
					case 1:
						color = unchecked((byte)(_color + 2 * mul));
						break;
					case 2:
						color = unchecked((byte)(_color + 4 * mul));
						SetPixel(square.w + 1, 1, color);
						break;
					case 3:
						color = unchecked((byte)(_color + 3 * mul));
						break;
				}
			}
		}
		else
		{
			for (int i = 0; i < 5; ++i)
			{
				DrawRect(square, color);
				if (i < 2)
					color = unchecked((byte)(color - 1 * mul));
				else
					color = unchecked((byte)(color + 1 * mul));
				square.x++;
				square.y++;
				if (square.w >= 2)
					square.w -= 2;
				else
					square.w = 1;

				if (square.h >= 2)
					square.h -= 2;
				else
					square.h = 1;
			}
			if (_innerColor != 0)
				DrawRect(square, _innerColor);
		}

		if (_bg != null)
		{
			var crop = _bg.GetCrop();
			crop.Crop.x = (short)(square.x - _dx);
			crop.Crop.y = (short)(square.y - _dy);
			crop.Crop.w = square.w;
			crop.Crop.h = square.h;
			crop.X = square.x;
			crop.Y = square.y;
			crop.Blit(this);
		}
	}

	/// <summary>Changes the horizontal offset of the surface in the X axis.</summary>
	public void SetDX(int dx) => _dx = dx;

	/// <summary>Changes the vertical offset of the surface in the Y axis.</summary>
	public void SetDY(int dy) => _dy = dy;

	/// <summary>Changes the window to have a thin border.</summary>
	public void SetThinBorder() => _thinBorder = true;

	/// <summary>Changes the window to have a custom inner color.</summary>
	public void SetInnerColor(byte innerColor) => _innerColor = innerColor;

	/// <summary>Mute the window.</summary>
	public void Mute() => _mute = true;
}
