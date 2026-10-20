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
// Ported from: src/Interface/TextButton.h, src/Interface/TextButton.cpp
#nullable disable

using System.Runtime.CompilerServices;

namespace Acom;

/// <summary>
/// Coloured button with a text label.
/// Drawn to look like 3D-shaped box with text on top,
/// responds to mouse clicks. Can be attached to a group of
/// buttons to turn it into a radio button (only one button
/// pushed at a time).
/// </summary>
/// <remarks>
/// A button group is a shared variable holding the pressed button (C++ <c>TextButton **</c>):
/// the owning state keeps a <see cref="StrongBox{T}"/> and reads/writes its <c>Value</c>.
/// </remarks>
public class TextButton : InteractiveSurface
{
#pragma warning disable CA2211 // static sound like the C++ class member, set by the game when sounds load
	public static Sound soundPress;
#pragma warning restore CA2211

	private byte _color;
	private readonly Text _text;
	private StrongBox<TextButton> _group;
	private bool _contrast, _geoscapeButton;
	private ComboBox _comboBox;

	/// <summary>Sets up a text button with the specified size and position. The text is centered on the button.</summary>
	/// <param name="width">Width in pixels.</param>
	/// <param name="height">Height in pixels.</param>
	/// <param name="x">X position in pixels.</param>
	/// <param name="y">Y position in pixels.</param>
	public TextButton(int width, int height, int x = 0, int y = 0) : base(width, height, x, y)
	{
		_text = new Text(width, height, 0, 0);
		_text.SetSmall();
		_text.Align = TextHAlign.ALIGN_CENTER;
		_text.VerticalAlign = TextVAlign.ALIGN_MIDDLE;
		_text.SetWordWrap(true);
	}

	/// <summary>Changes the text color (for use by RuleInterface; private in C++).</summary>
	public override void SetSecondaryColor(byte color) => SetTextColor(color);

	protected override bool IsButtonHandled(byte button = 0)
	{
		if (_comboBox != null)
			return button == SDLMouse.SDL_BUTTON_LEFT;
		else
			return base.IsButtonHandled(button);
	}

	/// <summary>Changes the color for the button and text.</summary>
	public override void SetColor(byte color)
	{
		_color = color;
		_text.SetColor(color);
		_redraw = true;
	}

	/// <summary>Returns the color for the button and text.</summary>
	public byte Color => _color;

	/// <summary>Changes the color for the text only.</summary>
	public void SetTextColor(byte color)
	{
		_text.SetColor(color);
		_redraw = true;
	}

	/// <summary>Changes the text to use the big-size font.</summary>
	public void SetBig()
	{
		_text.SetBig();
		_redraw = true;
	}

	/// <summary>Changes the text to use the small-size font.</summary>
	public void SetSmall()
	{
		_text.SetSmall();
		_redraw = true;
	}

	/// <summary>Returns the font currently used by the text.</summary>
	public Font Font => _text.Font;

	/// <summary>
	/// Changes the various resources needed for text rendering.
	/// The different fonts need to be passed in advance since the
	/// text size can change mid-text, and the language affects
	/// how the text is rendered.
	/// </summary>
	public override void InitText(Font big, Font small, Language lang)
	{
		_text.InitText(big, small, lang);
		_redraw = true;
	}

	/// <summary>Enables/disables high contrast color. Mostly used for Battlescape UI.</summary>
	public override void SetHighContrast(bool contrast)
	{
		_contrast = contrast;
		_text.SetHighContrast(contrast);
		_redraw = true;
	}

	/// <summary>The text of the button label.</summary>
	public string Text
	{
		get => _text.GetText();
		set
		{
			_text.SetText(value);
			_redraw = true;
		}
	}

	/// <summary>Changes the button group this button belongs to. Null makes it a regular button.</summary>
	/// <param name="group">Shared variable holding the pressed button of the group.</param>
	public void SetGroup(StrongBox<TextButton> group)
	{
		_group = group;
		_redraw = true;
	}

	/// <summary>Replaces a certain amount of colors in the surface's palette.</summary>
	public override void SetPalette(ReadOnlySpan<Rgba> colors, int firstcolor = 0, int ncolors = 256)
	{
		base.SetPalette(colors, firstcolor, ncolors);
		_text.SetPalette(colors, firstcolor, ncolors);
	}

	/// <summary>Draws the labeled button. The colors are inverted if the button is pressed.</summary>
	public override void Draw()
	{
		base.Draw();
		var square = new SDL_Rect();

		int mul = 1;
		if (_contrast)
			mul = 2;

		int color = _color + 1 * mul;

		square.x = 0;
		square.y = 0;
		square.w = (ushort)Width;
		square.h = (ushort)Height;

		for (int i = 0; i < 5; ++i)
		{
			DrawRect(ref square, unchecked((byte)color));

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
					color = _color + 5 * mul;
					SetPixel(square.w, 0, unchecked((byte)color));
					break;
				case 1:
					color = _color + 2 * mul;
					break;
				case 2:
					color = _color + 4 * mul;
					SetPixel(square.w + 1, 1, unchecked((byte)color));
					break;
				case 3:
					color = _color + 3 * mul;
					break;
				case 4:
					if (_geoscapeButton)
					{
						SetPixel(0, 0, _color);
						SetPixel(1, 1, _color);
					}
					break;
			}
		}

		bool press;
		if (_group == null)
			press = IsButtonPressed();
		else
			press = _group.Value == this;

		if (press)
		{
			if (_geoscapeButton)
				Invert(unchecked((byte)(_color + 2 * mul)));
			else
				Invert(unchecked((byte)(_color + 3 * mul)));
		}
		_text.SetInvert(press);

		_text.Blit(this);
	}

	/// <summary>Sets the button as the pressed button if it's part of a group.</summary>
	public override void MousePress(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		byte button = action.Details.button.button;
		if (button == SDLMouse.SDL_BUTTON_LEFT && _group != null)
		{
			TextButton old = _group.Value;
			_group.Value = this;
			old?.Draw();
			Draw();
		}

		if (IsButtonHandled(button))
		{
			if (soundPress != null && _group == null &&
				button != SDLMouse.SDL_BUTTON_WHEELUP && button != SDLMouse.SDL_BUTTON_WHEELDOWN)
			{
				soundPress.Play(SDLMixer.Mix_GroupAvailable(0));
			}

			_comboBox?.Toggle(false, false);

			Draw();
			//_redraw = true;
		}
		base.MousePress(action, state);
	}

	/// <summary>InteractiveSurface::mousePress for subclasses that skip the group handling above (C++ calls it by name).</summary>
	protected void InteractiveSurfaceMousePress(Action action, State state) => base.MousePress(action, state);

	/// <summary>Sets the button as the released button.</summary>
	public override void MouseRelease(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (IsButtonHandled(action.Details.button.button))
		{
			Draw();
			//_redraw = true;
		}
		base.MouseRelease(action, state);
	}

	/// <summary>Hooks up the button to work as part of an existing combobox, toggling its state when it's pressed.</summary>
	public void SetComboBox(ComboBox comboBox)
	{
		_comboBox = comboBox;
		if (_comboBox != null)
			_text.X = -6;
		else
			_text.X = 0;
	}

	public override void SetWidth(int width)
	{
		base.SetWidth(width);
		_text.SetWidth(width);
	}

	public override void SetHeight(int height)
	{
		base.SetHeight(height);
		_text.SetHeight(height);
	}

	public void SetGeoscapeButton(bool geo) => _geoscapeButton = geo;
}
