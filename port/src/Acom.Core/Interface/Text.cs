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
// Ported from: src/Interface/Text.h, src/Interface/Text.cpp
#nullable disable

namespace Acom;

public enum TextHAlign : uint
{
	ALIGN_LEFT = 0,
	ALIGN_CENTER = 1,
	ALIGN_RIGHT = 2,
}

public enum TextVAlign : uint
{
	ALIGN_TOP = 0,
	ALIGN_MIDDLE = 1,
	ALIGN_BOTTOM = 2,
}

/// <summary>
/// Text string displayed on screen.
/// Takes the characters from a Font and puts them together on screen
/// to display a string of text, taking care of any required aligning
/// or wrapping.
/// </summary>
/// <remarks>
/// C++ processes the text as UTF-32 code points; the port works on UTF-16 <see cref="char"/>s like
/// <see cref="Font"/> and turns every code point outside the Basic Multilingual Plane into a single '?',
/// which is what the original draws for characters the font does not have.
/// </remarks>
public class Text : InteractiveSurface
{
	private Font _big, _small, _font, _fontOrig;
	private Language _lang;
	private string _text = string.Empty;
	private readonly List<char> _processedText = [];
	private readonly List<int> _lineWidth = [], _lineHeight = [];
	private bool _wrap, _invert, _contrast, _indent, _scroll, _ignoreSeparators;
	private TextHAlign _align;
	private TextVAlign _valign;
	private byte _color, _color2;
	private int _scrollY;

	/// <summary>Sets up a blank text with the specified size and position.</summary>
	/// <param name="width">Width in pixels.</param>
	/// <param name="height">Height in pixels.</param>
	/// <param name="x">X position in pixels.</param>
	/// <param name="y">Y position in pixels.</param>
	public Text(int width, int height, int x = 0, int y = 0) : base(width, height, x, y)
	{
		_align = TextHAlign.ALIGN_LEFT;
		_valign = TextVAlign.ALIGN_TOP;
	}

	/// <summary>Changes the text to use the big-size font.</summary>
	public void SetBig()
	{
		_font = _big;
		_fontOrig = _big;
		ProcessText();
	}

	/// <summary>Changes the text to use the small-size font.</summary>
	public void SetSmall()
	{
		_font = _small;
		_fontOrig = _small;
		ProcessText();
	}

	/// <summary>Returns the font currently used by the text.</summary>
	public Font Font => _font;

	/// <summary>
	/// Changes the various resources needed for text rendering.
	/// The different fonts need to be passed in advance since the
	/// text size can change mid-text, and the language affects
	/// how the text is rendered.
	/// </summary>
	/// <param name="big">Large-size font.</param>
	/// <param name="small">Small-size font.</param>
	/// <param name="lang">Current language.</param>
	public override void InitText(Font big, Font small, Language lang)
	{
		_big = big;
		_small = small;
		_lang = lang;
		SetSmall();
	}

	/// <summary>Changes the string displayed on screen.</summary>
	/// <param name="text">Text string.</param>
	public void SetText(string text)
	{
		_text = text ?? string.Empty;
		_font = _fontOrig;
		ProcessText();
		// If big text won't fit the space, try small text
		if (_text.Length != 0)
		{
			if (_font == _big && (GetTextWidth() > Width || GetTextHeight() > Height) && _text[^1] != '.')
			{
				_font = _small;
				ProcessText();
			}
		}
	}

	/// <summary>Returns the string displayed on screen.</summary>
	public string GetText() => _text;

	/// <summary>
	/// Enables/disables text wordwrapping. When enabled, lines of
	/// text are automatically split to ensure they stay within the
	/// drawing area, otherwise they simply go off the edge.
	/// </summary>
	/// <param name="wrap">Wordwrapping setting.</param>
	/// <param name="indent">Indent wrapped text.</param>
	/// <param name="ignoreSeparators">Handle separators as spaces (false) or as normal text (true)?</param>
	public void SetWordWrap(bool wrap, bool indent = false, bool ignoreSeparators = false)
	{
		if (wrap != _wrap || indent != _indent || ignoreSeparators != _ignoreSeparators)
		{
			_wrap = wrap;
			_indent = indent;
			_ignoreSeparators = ignoreSeparators;
			ProcessText();
		}
	}

	/// <summary>Enables/disables color inverting. Mostly used to make button text look pressed along with the button.</summary>
	public void SetInvert(bool invert)
	{
		_invert = invert;
		_redraw = true;
	}

	/// <summary>Enables/disables high contrast color. Mostly used for Battlescape UI.</summary>
	public override void SetHighContrast(bool contrast)
	{
		_contrast = contrast;
		_redraw = true;
	}

	/// <summary>The way the text is aligned horizontally relative to the drawing area.</summary>
	public TextHAlign Align
	{
		get => _align;
		set
		{
			_align = value;
			_redraw = true;
		}
	}

	/// <summary>The way the text is aligned vertically relative to the drawing area.</summary>
	public TextVAlign VerticalAlign
	{
		get => _valign;
		set
		{
			_valign = value;
			_redraw = true;
		}
	}

	/// <summary>
	/// Changes the color used to render the text. Unlike regular graphics,
	/// fonts are greyscale so they need to be assigned a specific position
	/// in the palette to be displayed.
	/// </summary>
	public override void SetColor(byte color)
	{
		_color = color;
		_color2 = color;
		_redraw = true;
	}

	/// <summary>Returns the color used to render the text.</summary>
	public byte Color => _color;

	/// <summary>
	/// Changes the secondary color used to render the text. The text
	/// switches between the primary and secondary color whenever there's
	/// a 0x01 in the string.
	/// </summary>
	public override void SetSecondaryColor(byte color)
	{
		_color2 = color;
		_redraw = true;
	}

	/// <summary>Returns the secondary color used to render the text.</summary>
	public byte SecondaryColor => _color2;

	/// <summary>Gets the number of lines in the (wrapped, if wrapping is enabled) text.</summary>
	public int NumLines => _wrap ? _lineHeight.Count : 1;

	/// <summary>Returns the rendered text's height. Useful to check if wordwrap applies.</summary>
	/// <param name="line">Line to get the height, or -1 to get whole text height.</param>
	/// <returns>Height in pixels.</returns>
	public int GetTextHeight(int line = -1)
	{
		if (line == -1)
		{
			int height = 0;
			foreach (int lh in _lineHeight)
				height += lh;
			return height;
		}
		else
		{
			return _lineHeight[line];
		}
	}

	/// <summary>Returns the rendered text's width.</summary>
	/// <param name="line">Line to get the width, or -1 to get whole text width.</param>
	/// <returns>Width in pixels.</returns>
	public int GetTextWidth(int line = -1)
	{
		if (line == -1)
		{
			int width = 0;
			foreach (int lw in _lineWidth)
			{
				if (lw > width)
					width = lw;
			}
			return width;
		}
		else
		{
			return _lineWidth[line];
		}
	}

	/// <summary>The text after processing (code points, inserted line breaks and indentation); for tests.</summary>
	internal string ProcessedText => new([.. _processedText]);

	/// <summary>Measured width of every line (also when not wrapping); for tests.</summary>
	internal IReadOnlyList<int> LineWidths => _lineWidth;

	/// <summary>Measured height of every line; for tests.</summary>
	internal IReadOnlyList<int> LineHeights => _lineHeight;

	/// <summary>Unicode::convUtf8ToUtf32: one entry per code point; code points outside the BMP become '?'.</summary>
	private static void ToCodePoints(string text, List<char> result)
	{
		result.Clear();
		foreach (var rune in text.EnumerateRunes())
			result.Add(rune.IsBmp ? (char)rune.Value : '?');
	}

	/// <summary>
	/// Takes care of any text post-processing like converting
	/// encoded text to individual codepoints and calculating
	/// line metrics for alignment and wordwrapping.
	/// </summary>
	private void ProcessText()
	{
		if (_font == null || _lang == null)
			return;

		ToCodePoints(_text, _processedText);
		_lineWidth.Clear();
		_lineHeight.Clear();
		_scrollY = 0;

		int width = 0, word = 0;
		int space = 0, textIndentation = 0;
		bool start = true;
		Font font = _font;
		List<char> str = _processedText;

		// Go through the text character by character
		for (int c = 0; c <= str.Count; ++c)
		{
			// End of the line
			if (c == str.Count || Unicode.IsLinebreak(str[c]))
			{
				// Add line measurements for alignment later
				_lineWidth.Add(width);
				_lineHeight.Add(font.GetCharSize('\n').h);
				start = true;
				width = 0;
				word = 0;

				if (c == str.Count)
					break;
				else if (str[c] == Unicode.TOK_NL_SMALL)
					font = _small;
			}
			// Keep track of spaces for wordwrapping
			else if (Unicode.IsSpace(str[c]) || (!_ignoreSeparators && Unicode.IsSeparator(str[c])))
			{
				// Store existing indentation
				if (c == textIndentation)
					textIndentation++;
				space = c;
				start = start && (word == 0); // consider initial spaces still as start of line until first character is met
				width += font.GetCharSize(str[c]).w;
				word = 0;
			}
			// Custom format, skip 3 next chars
			else if (str[c] == Unicode.TOK_CUSTOM_FORMAT)
			{
				if (c + 3 > str.Count)
				{
					while (str.Count < c + 3)
						str.Add('\0'); // add missing character
					str[c + 1] = '\0';
					str[c + 2] = '\0';
				}
				c += 2;
			}
			// Keep track of the width of the last line and word
			else if (str[c] != Unicode.TOK_COLOR_FLIP)
			{
				int charWidth = font.GetCharSize(str[c]).w;

				width += charWidth;
				word += charWidth;

				// Wordwrap if the last word doesn't fit the line
				if (_wrap && width >= Width && (!start || _lang.TextWrapping == TextWrapping.WRAP_LETTERS))
				{
					int indentLocation = c;
					if (_lang.TextWrapping == TextWrapping.WRAP_WORDS || Unicode.IsSpace(str[c]))
					{
						// Go back to the last space and put a linebreak there
						width -= word;
						indentLocation = space;
						if (Unicode.IsSpace(str[space]))
						{
							width -= font.GetCharSize(str[space]).w;
							str[space] = '\n';
						}
						else
						{
							str.Insert(space + 1, '\n');
							indentLocation++;
						}
					}
					else if (_lang.TextWrapping == TextWrapping.WRAP_LETTERS)
					{
						// Go back to the last letter and put a linebreak there
						str.Insert(c, '\n');
						width -= charWidth;
					}

					// Keep initial indentation of text
					if (textIndentation > 0)
					{
						str.InsertRange(indentLocation + 1, Enumerable.Repeat('\t', textIndentation));
						indentLocation += textIndentation;
					}
					// Indent due to word wrap.
					if (_indent)
					{
						str.Insert(indentLocation + 1, '\t');
						width += font.GetCharSize('\t').w;
					}

					_lineWidth.Add(width);
					_lineHeight.Add(font.GetCharSize('\n').h);
					if (_lang.TextWrapping == TextWrapping.WRAP_WORDS)
					{
						width = word;
					}
					else if (_lang.TextWrapping == TextWrapping.WRAP_LETTERS)
					{
						width = 0;
					}
					start = true;
				}
			}
		}

		_redraw = true;
	}

	/// <summary>Font pixel recolouring: dest = off + src * mul, inverted around mid (if non-zero); 0 is transparent.</summary>
	private readonly struct PaletteShift(int off, int mul, int mid) : Surface.IPixelOp
	{
		public void Apply(ref byte dest, byte src)
		{
			if (src != 0)
			{
				int inverseOffset = mid != 0 ? 2 * (mid - src) : 0;
				dest = unchecked((byte)(off + src * mul + inverseOffset));
			}
		}
	}

	/// <summary>Calculates the starting X position for a line of text.</summary>
	/// <param name="line">The line number (0 = first, etc).</param>
	/// <returns>The X position in pixels.</returns>
	private int GetLineX(int line)
	{
		int x = 0;
		switch (_lang.TextDirection)
		{
			case TextDirection.DIRECTION_LTR:
				switch (_align)
				{
					case TextHAlign.ALIGN_LEFT:
						break;
					case TextHAlign.ALIGN_CENTER:
						x = (int)Math.Ceiling((Width + _font.Spacing - _lineWidth[line]) / 2.0);
						break;
					case TextHAlign.ALIGN_RIGHT:
						x = Width - 1 - _lineWidth[line];
						break;
				}
				break;
			case TextDirection.DIRECTION_RTL:
				switch (_align)
				{
					case TextHAlign.ALIGN_LEFT:
						x = Width - 1;
						break;
					case TextHAlign.ALIGN_CENTER:
						x = Width - (int)Math.Ceiling((Width + _font.Spacing - _lineWidth[line]) / 2.0);
						break;
					case TextHAlign.ALIGN_RIGHT:
						x = _lineWidth[line];
						break;
				}
				break;
		}
		return x;
	}

	/// <summary>
	/// Draws all the characters in the text with a really
	/// nasty complex gritty text rendering algorithm logic stuff.
	/// </summary>
	public override void Draw()
	{
		base.Draw();
		if (_text.Length == 0 || _font == null)
			return;

		// Show text borders for debugging
		if (Options.debugUi)
		{
			var r = new SDL_Rect(0, 0, Width, Height);
			DrawRect(ref r, 5);
			r.w -= 2;
			r.h -= 2;
			r.x++;
			r.y++;
			DrawRect(ref r, 0);
		}

		int x, y = 0, line = 0, height;
		Font font = _font;
		int color = _color;
		bool isAltColor = false;
		List<char> s = _processedText;

		height = GetTextHeight();

		if (_scroll && (Height - height < 0))
		{
			y = _scrollY;
		}
		else
		{
			switch (_valign)
			{
				case TextVAlign.ALIGN_TOP:
					y = 0;
					break;
				case TextVAlign.ALIGN_MIDDLE:
					y = (int)Math.Ceiling((Height - height) / 2.0);
					break;
				case TextVAlign.ALIGN_BOTTOM:
					y = Height - height;
					break;
			}
		}

		x = GetLineX(line);

		// Set up text color
		int mul = 1;
		if (_contrast)
			mul = 3;

		// Set up text direction
		int dir = 1;
		if (_lang.TextDirection == TextDirection.DIRECTION_RTL)
			dir = -1;

		// Invert text by inverting the font palette on index 3 (font palettes use indices 1-5)
		int mid = _invert ? 3 : 0;

		// Draw each letter one by one
		for (int c = 0; c < s.Count; ++c)
		{
			char ch = s[c];
			if (Unicode.IsSpace(ch) || ch == '\t')
			{
				x += dir * font.GetCharSize(ch).w;
			}
			else if (Unicode.IsLinebreak(ch))
			{
				line++;
				y += font.GetCharSize(ch).h;
				x = GetLineX(line);
				if (ch == Unicode.TOK_NL_SMALL)
					font = _small;
			}
			else if (ch == Unicode.TOK_COLOR_FLIP)
			{
				if (!isAltColor)
				{
					color = _color2;
					isAltColor = true;
				}
				else
				{
					color = _color;
					isAltColor = false;
				}
			}
			else if (ch == Unicode.TOK_CUSTOM_FORMAT)
			{
				char op = s[c + 1];
				char arg = s[c + 2];
				switch (op)
				{
					case 'C': // custom color like "\eC\x45"
						color = arg;
						isAltColor = false;
						break;

					case 'c': // specific color like "\ecP" - primary, "\ecS" - secondary
						if (arg == 'P')
						{
							color = _color;
							isAltColor = false;
						}
						else if (arg == 'S')
						{
							color = _color2;
							isAltColor = true;
						}
						break;

					default:
						// nothing
						break;
				}
				c += 2;
			}
			else
			{
				if (dir < 0)
					x += dir * font.GetCharSize(ch).w;
				var chr = font.GetChar(ch);
				chr.X = x;
				chr.Y = y;
				chr.Blit(this, new PaletteShift(color, mul, mid));
				if (dir > 0)
					x += dir * font.GetCharSize(ch).w;
			}
		}
	}

	/// <summary>Allows the text to be scrollable via mouse wheel.</summary>
	public void SetScrollable(bool scroll) => _scroll = scroll;

	/// <summary>Handles scrolling.</summary>
	/// <param name="action">Pointer to an action.</param>
	/// <param name="state">State that the action handlers belong to.</param>
	public override void MousePress(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		base.MousePress(action, state);
		byte button = action.Details.button.button;
		if (_scroll && (button == SDLMouse.SDL_BUTTON_WHEELUP || button == SDLMouse.SDL_BUTTON_WHEELDOWN))
		{
			int scrollArea = Height - GetTextHeight();
			if (scrollArea < 0)
			{
				int scrollAmount = _font.Height + _font.Spacing;
				if (button == SDLMouse.SDL_BUTTON_WHEELDOWN)
					scrollAmount = -scrollAmount;

				_scrollY = Math.Min(Math.Max(_scrollY + scrollAmount, scrollArea), 0);
				_redraw = true;
			}
		}
	}
}
