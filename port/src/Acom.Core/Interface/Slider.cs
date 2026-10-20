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
// Ported from: src/Interface/Slider.h, src/Interface/Slider.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Horizontal slider control to select from a range of values.
/// </summary>
public class Slider : InteractiveSurface
{
	private readonly Frame _frame;
	private readonly Text _txtMinus, _txtPlus;
	private readonly TextButton _button;
	private double _pos;
	private int _min, _max = 100, _value;
	private bool _pressed;
	private ActionHandler _change;
	private readonly int _thickness, _textness;
	private int _minX, _maxX, _offsetX;

	/// <summary>Creates a new slider with the specified size and position.</summary>
	public Slider(int width, int height, int x = 0, int y = 0) : base(width, height, x, y)
	{
		_thickness = 5;
		_textness = 8;
		_txtMinus = new Text(_textness, height - 2, x - 1, y);
		_txtPlus = new Text(_textness, height - 2, x + width - _textness, y);
		_frame = new Frame(width - _textness * 2, _thickness, x + _textness, y + (height - _thickness) / 2);
		_button = new TextButton(10, height, x, y);

		_frame.SetThickness(_thickness);

		_txtMinus.Align = TextHAlign.ALIGN_CENTER;
		_txtMinus.VerticalAlign = TextVAlign.ALIGN_MIDDLE;
		_txtMinus.SetText("-");

		_txtPlus.Align = TextHAlign.ALIGN_CENTER;
		_txtPlus.VerticalAlign = TextVAlign.ALIGN_MIDDLE;
		_txtPlus.SetText("+");

		_minX = _frame.X;
		_maxX = _frame.X + _frame.Width - _button.Width;

		Value = (int)_pos;
	}

	/// <summary>Changes the position of the surface in the X axis.</summary>
	public override int X
	{
		get => base.X;
		set
		{
			base.X = value;
			_txtMinus.X = value - 1;
			_txtPlus.X = value + Width - _textness;
			_frame.X = X + _textness;

			_minX = _frame.X;
			_maxX = _frame.X + _frame.Width - _button.Width;
			// As in C++ (setValue(_pos)): the position 0..1 is passed as the value.
			Value = (int)_pos;
		}
	}

	/// <summary>Changes the position of the surface in the Y axis.</summary>
	public override int Y
	{
		get => base.Y;
		set
		{
			base.Y = value;
			_txtMinus.Y = value;
			_txtPlus.Y = value;
			_frame.Y = Y + (Height - _thickness) / 2;
			_button.Y = Y;
		}
	}

	/// <summary>Changes the various resources needed for text rendering.</summary>
	public virtual void InitText(Font big, Font small, Language lang)
	{
		_txtMinus.InitText(big, small, lang);
		_txtPlus.InitText(big, small, lang);
		_button.InitText(big, small, lang);
	}

	/// <summary>Enables/disables high contrast color. Mostly used for Battlescape.</summary>
	public override void SetHighContrast(bool contrast)
	{
		_txtMinus.SetHighContrast(contrast);
		_txtPlus.SetHighContrast(contrast);
		_frame.SetHighContrast(contrast);
		_button.SetHighContrast(contrast);
	}

	/// <summary>Changes the color used to render the slider.</summary>
	public override void SetColor(byte color)
	{
		_txtMinus.Color = color;
		_txtPlus.Color = color;
		_frame.SetColor(color);
		_button.Color = color;
	}

	/// <summary>The color used to render the slider.</summary>
	public byte Color { get => _button.Color; set => SetColor(value); }

	/// <summary>Replaces a certain amount of colors in the slider's palette.</summary>
	public override void SetPalette(ReadOnlySpan<Rgba> colors, int firstcolor = 0, int ncolors = 256)
	{
		base.SetPalette(colors, firstcolor, ncolors);
		_txtMinus.SetPalette(colors, firstcolor, ncolors);
		_txtPlus.SetPalette(colors, firstcolor, ncolors);
		_frame.SetPalette(colors, firstcolor, ncolors);
		_button.SetPalette(colors, firstcolor, ncolors);
	}

	/// <summary>Automatically updates the slider when the mouse moves.</summary>
	public override void Handle(Action action, State state)
	{
		base.Handle(action, state);
		//_button->handle(action, state);
		var type = action.Details.type;
		if (_pressed && (type == SDLEventType.SDL_MOUSEMOTION || type == SDLEventType.SDL_MOUSEBUTTONDOWN))
		{
			int cursorX = (int)action.AbsoluteXMouse;
			Value = CursorToValue(cursorX, _offsetX, _minX, _maxX, _min, _max);
			_change?.Invoke(action);
		}
	}

	/// <summary>Maps the cursor X position (plus drag offset) to a slider value (part of C++ handle()).</summary>
	internal static int CursorToValue(int cursorX, int offsetX, int minX, int maxX, int min, int max)
	{
		double buttonX = Clamp(cursorX + offsetX, minX, maxX);
		double pos = (buttonX - minX) / (maxX - minX);
		return min + (int)Round((max - min) * pos);
	}

	/// <summary>Clamps a value into the slider's range and gets its position 0..1 (part of C++ setValue()).</summary>
	internal static (int Value, double Pos) ClampValue(int value, int min, int max)
	{
		if (min == max)
		{
			return (0, 0.0);
		}
		int v = min < max ? Clamp(value, min, max) : Clamp(value, max, min);
		double pos = (double)(v - min) / (double)(max - min);
		return (v, pos);
	}

	/// <summary>Gets the button's X position for a slider position 0..1 (part of C++ setPosition()).</summary>
	internal static int ButtonX(int minX, int maxX, double pos) => (int)Math.Floor(minX + (maxX - minX) * pos);

	/// <summary>fmath.h Clamp(): no exception when min &gt; max (unlike Math.Clamp).</summary>
	private static int Clamp(int x, int min, int max) => Math.Min(Math.Max(x, min), max);

	/// <summary>fmath.h Round(): rounds half away from zero.</summary>
	private static double Round(double x) => x < 0.0 ? Math.Ceiling(x - 0.5) : Math.Floor(x + 0.5);

	/// <summary>Moves the slider to the new position.</summary>
	private void SetPosition(double pos)
	{
		_pos = pos;
		_button.X = ButtonX(_minX, _maxX, _pos);
	}

	/// <summary>Changes the range of values the slider can contain.</summary>
	public void SetRange(int min, int max)
	{
		_min = min;
		_max = max;
		Value = _value;
	}

	/// <summary>The current value of the slider; setting it positions the slider appropriately.</summary>
	public int Value
	{
		get => _value;
		set
		{
			(_value, double pos) = ClampValue(value, _min, _max);
			SetPosition(pos);
		}
	}

	/// <summary>Blits the slider contents.</summary>
	public override void Blit(Surface surface)
	{
		base.Blit(surface);
		if (_visible && !_hidden)
		{
			_txtMinus.Blit(surface);
			_txtPlus.Blit(surface);
			_frame.Blit(surface);
			_button.Blit(surface);
		}
	}

	/// <summary>The slider only moves while the button is pressed.</summary>
	public override void MousePress(Action action, State state)
	{
		base.MousePress(action, state);
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
		{
			_pressed = true;
			int cursorX = (int)action.AbsoluteXMouse;
			if (cursorX >= _button.X && cursorX < _button.X + _button.Width)
			{
				_offsetX = _button.X - cursorX;
			}
			else
			{
				_offsetX = -_button.Width / 2;
			}
		}
	}

	/// <summary>The slider stops moving when the button is released.</summary>
	public override void MouseRelease(Action action, State state)
	{
		base.MouseRelease(action, state);
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
		{
			_pressed = false;
			_offsetX = 0;
		}
	}

	/// <summary>Sets a function to be called every time the slider's value changes.</summary>
	public void OnChange(ActionHandler handler) => _change = handler;
}
