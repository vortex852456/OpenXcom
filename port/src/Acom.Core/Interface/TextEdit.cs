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
// Ported from: src/Interface/TextEdit.h, src/Interface/TextEdit.cpp
#nullable disable

namespace Acom;

public enum TextEditConstraint : uint
{
	TEC_NONE = 0,
	TEC_NUMERIC_POSITIVE = 1,
	TEC_NUMERIC = 2,
}

/// <summary>
/// Editable version of Text.
/// Receives keyboard input to allow the player
/// to change the text himself.
/// </summary>
/// <remarks>
/// There is no destructor: the C++ one turns key repeat off and clears the state's modal surface
/// in case the text edit was left focused; that is up to the owning state here.
/// </remarks>
public class TextEdit : InteractiveSurface
{
	private readonly Text _text, _caret;
	private string _value = string.Empty;
	private bool _blink = true, _modal = true;
	private bool _drawBackground = true;
	private readonly Timer _timer;
	private char _char = 'A';
	private int _caretPos;
	private TextEditConstraint _textEditConstraint = TextEditConstraint.TEC_NONE;
	private ActionHandler _change;
	private ActionHandler _enter;
	private readonly State _state;

	/// <summary>Creates a new text edit with the specified size and position.</summary>
	/// <param name="state">State the text edit belongs to.</param>
	public TextEdit(State state, int width, int height, int x = 0, int y = 0) : base(width, height, x, y)
	{
		_state = state;
		_isFocused = false;
		_text = new Text(width, height, 0, 0);
		_timer = new Timer(100);
		_timer.OnTimer((SurfaceHandler)Blink);
		_caret = new Text(16, 17, 0, 0);
		_caret.SetText("|");
	}

	/// <summary>Passes events to internal components; a click outside unfocuses a modal text edit.</summary>
	public override void Handle(Action action, State state)
	{
		base.Handle(action, state);
		if (_isFocused && _modal && action.Details.type == SDLEventType.SDL_MOUSEBUTTONDOWN &&
			(action.AbsoluteXMouse < X || action.AbsoluteXMouse >= X + Width ||
			 action.AbsoluteYMouse < Y || action.AbsoluteYMouse >= Y + Height))
		{
			SetFocus(false);
		}
	}

	/// <summary>Controls the blinking animation when the text edit is focused.</summary>
	/// <param name="focus">True if focused, false otherwise.</param>
	/// <param name="modal">True to lock input to this control, false otherwise.</param>
	public override void SetFocus(bool focus, bool modal = true)
	{
		_modal = modal;
		if (focus != _isFocused)
		{
			_redraw = true;
			base.SetFocus(focus);
			if (_isFocused)
			{
				SDLKeyboard.SDL_EnableKeyRepeat(SDLKeyboard.SDL_DEFAULT_REPEAT_DELAY, SDLKeyboard.SDL_DEFAULT_REPEAT_INTERVAL);
				_caretPos = _value.Length;
				_blink = true;
				_timer.Start();
				if (_modal)
					_state.SetModal(this);
			}
			else
			{
				_blink = false;
				_timer.Stop();
				SDLKeyboard.SDL_EnableKeyRepeat(0, SDLKeyboard.SDL_DEFAULT_REPEAT_INTERVAL);
				if (_modal)
					_state.SetModal(null);
			}
		}
	}

	/// <summary>Changes the text edit to use the big-size font.</summary>
	public void SetBig()
	{
		_text.SetBig();
		_caret.SetBig();
	}

	/// <summary>Changes the text edit to use the small-size font.</summary>
	public void SetSmall()
	{
		_text.SetSmall();
		_caret.SetSmall();
	}

	/// <summary>Changes the various fonts for the text edit to use.</summary>
	public override void InitText(Font big, Font small, Language lang)
	{
		_text.InitText(big, small, lang);
		_caret.InitText(big, small, lang);
	}

	/// <summary>The string displayed on screen; setting it moves the caret to the end.</summary>
	public string Text
	{
		get => _value;
		set
		{
			_value = value;
			_caretPos = _value.Length;
			_redraw = true;
		}
	}

	/// <summary>Current caret position (index into <see cref="Text"/>).</summary>
	internal int CaretPos => _caretPos;

	/// <summary>Enables/disables text wordwrapping.</summary>
	public void SetWordWrap(bool wrap) => _text.SetWordWrap(wrap);

	/// <summary>Enables/disables color inverting.</summary>
	public void SetInvert(bool invert)
	{
		_text.SetInvert(invert);
		_caret.SetInvert(invert);
	}

	/// <summary>Enables/disables high contrast color. Mostly used for Battlescape text.</summary>
	public override void SetHighContrast(bool contrast)
	{
		_text.SetHighContrast(contrast);
		_caret.SetHighContrast(contrast);
	}

	/// <summary>Changes the way the text is aligned horizontally relative to the drawing area.</summary>
	public void SetAlign(TextHAlign align) => _text.Align = align;

	/// <summary>Changes the way the text is aligned vertically relative to the drawing area.</summary>
	public void SetVerticalAlign(TextVAlign valign) => _text.VerticalAlign = valign;

	/// <summary>Restricts the text to only numerical input or signed numerical input.</summary>
	public void SetConstraint(TextEditConstraint constraint) => _textEditConstraint = constraint;

	/// <summary>Changes the color used to render the text.</summary>
	public override void SetColor(byte color)
	{
		_text.SetColor(color);
		_caret.SetColor(color);
	}

	/// <summary>The color used to render the text.</summary>
	public byte Color { get => _text.Color; set => SetColor(value); }

	/// <summary>Changes the secondary color used to render the text.</summary>
	public override void SetSecondaryColor(byte color) => _text.SetSecondaryColor(color);

	/// <summary>The secondary color used to render the text.</summary>
	public byte SecondaryColor { get => _text.SecondaryColor; set => SetSecondaryColor(value); }

	/// <summary>Replaces a certain amount of colors in the text edit's palette.</summary>
	public override void SetPalette(ReadOnlySpan<Rgba> colors, int firstcolor = 0, int ncolors = 256)
	{
		base.SetPalette(colors, firstcolor, ncolors);
		_text.SetPalette(colors, firstcolor, ncolors);
		_caret.SetPalette(colors, firstcolor, ncolors);
	}

	/// <summary>Keeps the animation timers running.</summary>
	public override void Think() => _timer.Think(null, this);

	/// <summary>Plays the blinking animation when the text edit is focused.</summary>
	public void Blink()
	{
		_blink = !_blink;
		_redraw = true;
	}

	/// <summary>Adds a flashing | caret to the text to show when it's focused and editable.</summary>
	public override void Draw()
	{
		base.Draw();
		string newValue = _value;
		if (Options.keyboardMode == KeyboardType.KEYBOARD_OFF)
		{
			if (_isFocused && _blink)
			{
				newValue += _char;
			}
		}
		_text.SetText(newValue);
		Clear();

		// TODO: this whole thing is old and ugly, rework later
		if (_enter != null && _drawBackground)
		{
			var square = new SDL_Rect(0, 0, Width, Height);
			DrawRect(ref square, Color);
		}

		_text.Blit(this);
		if (Options.keyboardMode == KeyboardType.KEYBOARD_ON)
		{
			if (_isFocused && _blink)
			{
				int x = 0;
				switch (_text.Align)
				{
					case TextHAlign.ALIGN_LEFT:
						x = 0;
						break;
					case TextHAlign.ALIGN_CENTER:
						x = (_text.Width - _text.GetTextWidth()) / 2;
						break;
					case TextHAlign.ALIGN_RIGHT:
						x = _text.Width - _text.GetTextWidth();
						break;
				}
				for (int i = 0; i < _caretPos; ++i)
				{
					x += _text.Font.GetCharSize(_value[i]).w;
				}
				_caret.X = x;
				int y = 0;
				switch (_text.VerticalAlign)
				{
					case TextVAlign.ALIGN_TOP:
						y = 0;
						break;
					case TextVAlign.ALIGN_MIDDLE:
						y = (int)Math.Ceiling((Height - _text.GetTextHeight()) / 2.0);
						break;
					case TextVAlign.ALIGN_BOTTOM:
						y = Height - _text.GetTextHeight();
						break;
				}
				_caret.Y = y;
				_caret.Blit(this);
			}
		}
	}

	/// <summary>
	/// Checks if adding a certain character to the text edit will exceed the maximum width.
	/// Used to make sure user input stays within bounds.
	/// </summary>
	private bool ExceedsMaxWidth(char c)
	{
		int w = 0;
		string s = _value + c;
		foreach (char i in s)
		{
			w += _text.Font.GetCharSize(i).w;
		}
		return w > Width;
	}

	/// <summary>
	/// Checks if input key character is valid to be inserted at caret position
	/// in the text edit without breaking the text edit constraint.
	/// </summary>
	private bool IsValidChar(char c) => IsValidChar(_textEditConstraint, _value, _caretPos, c);

	/// <inheritdoc cref="IsValidChar(char)"/>
	internal static bool IsValidChar(TextEditConstraint constraint, string value, int caretPos, char c)
	{
		switch (constraint)
		{
			case TextEditConstraint.TEC_NUMERIC_POSITIVE:
				return c >= '0' && c <= '9';

			// If constraint is "(signed) numeric", need to check:
			// - user does not input a character before '-' or '+'
			// - user enter either figure anywhere, or a sign at first position
			case TextEditConstraint.TEC_NUMERIC:
				if (caretPos > 0)
				{
					return c >= '0' && c <= '9';
				}
				else
				{
					return ((c >= '0' && c <= '9') || c == '+' || c == '-') &&
						(value.Length == 0 || (value[0] != '+' && value[0] != '-'));
				}

			case TextEditConstraint.TEC_NONE:
				return (c >= ' ' && c <= '~') || c >= 160;

			default:
				return false;
		}
	}

	/// <summary>Focuses the text edit when it's pressed on; if focused, moves the caret to the click.</summary>
	public override void MousePress(Action action, State state)
	{
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
		{
			if (!_isFocused)
			{
				SetFocus(true);
			}
			else
			{
				double mouseX = action.RelativeXMouse;
				double scaleX = action.XScale;
				double w = 0;
				int c = 0;
				foreach (char i in _value)
				{
					if (mouseX <= w)
					{
						break;
					}
					w += (double)_text.Font.GetCharSize(i).w / 2 * scaleX;
					if (mouseX <= w)
					{
						break;
					}
					c++;
					w += (double)_text.Font.GetCharSize(i).w / 2 * scaleX;
				}
				_caretPos = c;
			}
		}
		base.MousePress(action, state);
	}

	/// <summary>
	/// Changes the text edit according to keyboard input, and
	/// unfocuses the text if Enter is pressed.
	/// </summary>
	public override void KeyboardPress(Action action, State state)
	{
		bool enterPressed = false;
		var keysym = action.Details.key.keysym;
		if (Options.keyboardMode == KeyboardType.KEYBOARD_OFF)
		{
			switch (keysym.sym)
			{
				case SDLKey.SDLK_UP:
					_char++;
					if (_char > '~')
					{
						_char = ' ';
					}
					break;
				case SDLKey.SDLK_DOWN:
					_char--;
					if (_char < ' ')
					{
						_char = '~';
					}
					break;
				case SDLKey.SDLK_LEFT:
					if (_value.Length > 0)
					{
						_value = _value[..^1];
					}
					break;
				case SDLKey.SDLK_RIGHT:
					if (!ExceedsMaxWidth(_char))
					{
						_value += _char;
					}
					break;
				default:
					break;
			}
		}
		else if (Options.keyboardMode == KeyboardType.KEYBOARD_ON)
		{
			switch (keysym.sym)
			{
				case SDLKey.SDLK_LEFT:
					if (_caretPos > 0)
					{
						_caretPos--;
					}
					break;
				case SDLKey.SDLK_RIGHT:
					if (_caretPos < _value.Length)
					{
						_caretPos++;
					}
					break;
				case SDLKey.SDLK_HOME:
					_caretPos = 0;
					break;
				case SDLKey.SDLK_END:
					_caretPos = _value.Length;
					break;
				case SDLKey.SDLK_BACKSPACE:
					if (_caretPos > 0)
					{
						_value = _value.Remove(_caretPos - 1, 1);
						_caretPos--;
					}
					break;
				case SDLKey.SDLK_DELETE:
					if (_caretPos < _value.Length)
					{
						_value = _value.Remove(_caretPos, 1);
					}
					break;
				case SDLKey.SDLK_ESCAPE:
					_value = string.Empty;
					_caretPos = 0;
					// no break; do the ENTER action too
					goto case SDLKey.SDLK_RETURN;
				case SDLKey.SDLK_RETURN:
				case SDLKey.SDLK_KP_ENTER:
					if (_value.Length > 0 || _enter != null)
					{
						enterPressed = true;
						SetFocus(false);
					}
					break;
				default:
					char c = keysym.unicode;
					if (IsValidChar(c) && !ExceedsMaxWidth(c))
					{
						_value = _value.Insert(_caretPos, new string(c, 1));
						_caretPos++;
					}
					break;
			}
		}
		_redraw = true;
		_change?.Invoke(action);
		if (_enter != null && enterPressed)
		{
			_enter(action);
		}

		base.KeyboardPress(action, state);
	}

	/// <summary>Sets a function to be called every time the text changes.</summary>
	public void OnChange(ActionHandler handler) => _change = handler;

	/// <summary>Sets a function to be called every time ENTER is pressed.</summary>
	public void OnEnter(ActionHandler handler) => _enter = handler;

	/// <summary>Sets the text edit's background drawing setting.</summary>
	public void SetDrawBackground(bool drawBackground) => _drawBackground = drawBackground;
}
