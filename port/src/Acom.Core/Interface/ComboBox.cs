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
// Ported from: src/Interface/ComboBox.h, src/Interface/ComboBox.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Text button with a list dropdown when pressed.
/// Allows selection from multiple available options.
/// </summary>
public class ComboBox : InteractiveSurface
{
	private const int HORIZONTAL_MARGIN = 2;
	private const int VERTICAL_MARGIN = 3;
	private const int MAX_ITEMS = 10;
	private const int BUTTON_WIDTH = 14;
	private const int TEXT_HEIGHT = 8;

	private readonly TextButton _button;
	private readonly Surface _arrow;
	private readonly Window _window;
	private readonly TextList _list;
	private ActionHandler _change;
	/// <summary>Selected row; -1 (C++ <c>(size_t)-1</c>) for none.</summary>
	private int _sel;
	private readonly State _state;
	private Language _lang;
	private byte _color;
	private bool _toggled;
	private readonly bool _popupAboveButton;

	private static int GetPopupWindowY(int buttonHeight, int buttonY, int popupHeight, bool popupAboveButton)
	{
		int belowButtonY = buttonY + buttonHeight;
		if (popupAboveButton)
		{
			// used when popup list won't fit below the button; display it above
			return buttonY - popupHeight;
		}
		return belowButtonY;
	}

	/// <summary>Creates a combo box with the specified size and position.</summary>
	/// <param name="state">State the combobox belongs to.</param>
	public ComboBox(State state, int width, int height, int x = 0, int y = 0, bool popupAboveButton = false) : base(width, height, x, y)
	{
		_state = state;
		_popupAboveButton = popupAboveButton;

		_button = new TextButton(width, height, x, y);
		_button.SetComboBox(this);

		_arrow = new Surface(11, 8, x + width - BUTTON_WIDTH, y + 4);

		int popupHeight = MAX_ITEMS * TEXT_HEIGHT + VERTICAL_MARGIN * 2;
		int popupY = GetPopupWindowY(height, y, popupHeight, popupAboveButton);
		_window = new Window(state, width, popupHeight, x, popupY);
		_window.SetThinBorder();

		_list = new TextList(width - HORIZONTAL_MARGIN * 2 - BUTTON_WIDTH + 1,
							popupHeight - (VERTICAL_MARGIN * 2 + 2),
							x + HORIZONTAL_MARGIN,
							popupY + VERTICAL_MARGIN);
		_list.ComboBox = this;
		_list.SetColumns(1, _list.Width);
		_list.SetSelectable(true);
		_list.SetBackground(_window);
		_list.SetAlign(TextHAlign.ALIGN_CENTER);
		_list.SetScrolling(true, 0);

		Toggle(true, false);
	}

	/// <summary>Changes the position of the surface in the X axis.</summary>
	public override int X
	{
		get => base.X;
		set
		{
			base.X = value;
			_button.X = value;
			_arrow.X = value + Width - BUTTON_WIDTH;
			_window.X = value;
			_list.X = value + HORIZONTAL_MARGIN;
		}
	}

	/// <summary>Changes the position of the surface in the Y axis.</summary>
	public override int Y
	{
		get => base.Y;
		set
		{
			base.Y = value;
			_button.Y = value;
			_arrow.Y = value + 4;

			int popupHeight = _window.Height;
			int popupY = GetPopupWindowY(Height, value, popupHeight, _popupAboveButton);
			_window.Y = popupY;
			_list.Y = popupY + VERTICAL_MARGIN;
		}
	}

	/// <summary>Replaces a certain amount of colors in the palette of all the components.</summary>
	public override void SetPalette(ReadOnlySpan<Rgba> colors, int firstcolor = 0, int ncolors = 256)
	{
		base.SetPalette(colors, firstcolor, ncolors);
		_button.SetPalette(colors, firstcolor, ncolors);
		_arrow.SetPalette(colors, firstcolor, ncolors);
		_window.SetPalette(colors, firstcolor, ncolors);
		_list.SetPalette(colors, firstcolor, ncolors);
	}

	/// <summary>Changes the resources for the text in the combo box.</summary>
	public override void InitText(Font big, Font small, Language lang)
	{
		_lang = lang;
		_button.InitText(big, small, lang);
		_list.InitText(big, small, lang);
	}

	/// <summary>Changes the surface used to draw the background of the combo box.</summary>
	public void SetBackground(Surface bg) => _window.SetBackground(bg);

	/// <summary>Changes the color used to draw the combo box.</summary>
	public override void SetColor(byte color)
	{
		_color = color;
		DrawArrow();
		_button.SetColor(_color);
		_window.SetColor(_color);
		_list.SetColor(_color);
	}

	/// <summary>The color used to draw the combo box.</summary>
	public byte Color { get => _color; set => SetColor(value); }

	/// <summary>Draws the arrow used to indicate the combo box.</summary>
	private void DrawArrow()
	{
		_arrow.Clear();

		int color = _color + 1;
		if (color == 256)
			color++;

		// Draw arrow triangle 1
		var square = new SDL_Rect(1, 2, 9, 1);

		for (; square.w > 1; square.w -= 2)
		{
			_arrow.DrawRect(square, (byte)(color + 2));
			square.x++;
			square.y++;
		}
		_arrow.DrawRect(square, (byte)(color + 2));

		// Draw arrow triangle 2
		square = new SDL_Rect(2, 2, 7, 1);

		for (; square.w > 1; square.w -= 2)
		{
			_arrow.DrawRect(square, (byte)color);
			square.x++;
			square.y++;
		}
		_arrow.DrawRect(square, (byte)color);
	}

	/// <summary>Enables/disables high contrast color. Mostly used for Battlescape UI.</summary>
	public override void SetHighContrast(bool contrast)
	{
		_button.SetHighContrast(contrast);
		_window.SetHighContrast(contrast);
		_list.SetHighContrast(contrast);
	}

	/// <summary>Changes the color of the arrow buttons in the list.</summary>
	public void SetArrowColor(byte color) => _list.SetArrowColor(color);

	/// <summary>The currently selected option (row); setting it updates the button text.</summary>
	public int Selected
	{
		get => _sel;
		set
		{
			_sel = value;
			// unsigned comparison like C++ size_t: -1 is never a valid row
			if ((uint)_sel < (uint)_list.Texts)
			{
				_button.Text = _list.GetCellText(_sel, 0);
			}
		}
	}

	/// <summary>
	/// Gets the item that is currently hovered over in the popup list, or the current
	/// selected item if no item is hovered over.
	/// </summary>
	/// <remarks>
	/// The C++ code compares the 32-bit "no row" value of the list with a 64-bit <c>(size_t)-1</c>,
	/// so 64-bit builds return 0xFFFFFFFF for an open list without a hovered row; here the
	/// selection is returned, as intended (and as in 32-bit builds).
	/// </remarks>
	public int HoveredListIdx
	{
		get
		{
			int ret = -1;
			if (_list.Visible)
			{
				ret = unchecked((int)_list.SelectedRow);
			}
			if (ret == -1)
			{
				ret = _sel;
			}
			return ret;
		}
	}

	/// <summary>Sets the button text independent of the currently selected option.</summary>
	public void SetText(string text) => _button.Text = text;

	/// <summary>Updates the size of the dropdown list based on the number of options available.</summary>
	private void SetDropdown(int options)
	{
		int items = Math.Min(options, MAX_ITEMS);
		int h = _button.Font.Height + _button.Font.Spacing;
		int dy = (Options.baseYResolution - 200) / 2;
		while (_window.Y + items * h + VERTICAL_MARGIN * 2 > 200 + dy)
		{
			items--;
		}

		int popupHeight = items * h + VERTICAL_MARGIN * 2;
		int popupY = GetPopupWindowY(Height, Y, popupHeight, _popupAboveButton);
		_window.Y = popupY;
		_window.SetHeight(popupHeight);
		_list.Y = popupY + VERTICAL_MARGIN;
		_list.SetHeight(items * h);
	}

	/// <summary>Changes the list of available options to choose from.</summary>
	/// <param name="options">List of strings.</param>
	/// <param name="translate">True for a list of string IDs, false for a list of raw strings.</param>
	public void SetOptions(IReadOnlyList<string> options, bool translate = false)
	{
		ArgumentNullException.ThrowIfNull(options);
		SetDropdown(options.Count);
		_list.ClearList();
		foreach (var option in options)
		{
			if (translate)
				_list.AddRow(1, (string)_lang.GetString(option));
			else
				_list.AddRow(1, option);
		}
		Selected = _sel;
	}

	/// <summary>Blits the combo box components.</summary>
	public override void Blit(Surface surface)
	{
		base.Blit(surface);
		_list.Invalidate();
		if (_visible && !_hidden)
		{
			_button.Blit(surface);
			_arrow.Blit(surface);
			_window.Blit(surface);
			_list.Blit(surface);
		}
	}

	/// <summary>Passes events to internal components.</summary>
	public override void Handle(Action action, State state)
	{
		if (!_visible || _hidden)
			return;

		_button.Handle(action, state);
		_list.Handle(action, state);
		base.Handle(action, state);
		int topY = Math.Min(Y, _window.Y);
		if (_window.Visible && action.Details.type == SDLEventType.SDL_MOUSEBUTTONDOWN &&
			(action.AbsoluteXMouse < X || action.AbsoluteXMouse >= X + Width ||
			 action.AbsoluteYMouse < topY || action.AbsoluteYMouse >= topY + Height + _window.Height))
		{
			Toggle(false, false);
		}
		if (_toggled)
		{
			_change?.Invoke(action);
			_toggled = false;
		}
	}

	/// <summary>Passes ticks to arrow buttons.</summary>
	public override void Think()
	{
		_button.Think();
		_arrow.Think();
		_window.Think();
		_list.Think();
		base.Think();
	}

	/// <summary>Opens/closes the combo box list.</summary>
	/// <param name="first">Is it the initialization toggle?</param>
	/// <param name="listClick">Should the change handler be triggered? (Yes = list click; No = button click or click anywhere else)</param>
	public void Toggle(bool first, bool listClick)
	{
		_window.Visible = !_window.Visible;
		_list.Visible = !_list.Visible;
		_state.SetModal(_window.Visible ? this : null);
		if (!first && !_window.Visible)
		{
			_toggled = listClick;
		}
		if (_list.Visible)
		{
			// unsigned comparison like C++ size_t; ScrollTo() treats a negative result as huge
			if ((uint)_sel < (uint)(_list.VisibleRows / 2) || _sel == -1)
			{
				_list.ScrollTo(0);
			}
			else
			{
				_list.ScrollTo(_sel - _list.VisibleRows / 2);
			}
		}
	}

	/// <summary>Sets a function to be called every time the selection changes.</summary>
	public void OnChange(ActionHandler handler) => _change = handler;

	/// <summary>Sets a function to be called every time the mouse moves in to the listbox surface.</summary>
	public void OnListMouseIn(ActionHandler handler) => _list.OnMouseIn(handler);

	/// <summary>Sets a function to be called every time the mouse moves out of the listbox surface.</summary>
	public void OnListMouseOut(ActionHandler handler) => _list.OnMouseOut(handler);

	/// <summary>Sets a function to be called every time the mouse moves over the listbox surface.</summary>
	public void OnListMouseOver(ActionHandler handler) => _list.OnMouseOver(handler);
}
