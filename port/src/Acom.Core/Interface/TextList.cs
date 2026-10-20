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
// Ported from: src/Interface/TextList.h, src/Interface/TextList.cpp
// Port status: partial (missing: PageUp/PageDown scrolling and IsMouseCursorOverMe(State), need State.HasOnlyOneScrollableTextList and State.CursorX/CursorY)
#nullable disable

namespace Acom;

public enum ArrowOrientation : uint
{
	ARROW_VERTICAL = 0,
	ARROW_HORIZONTAL = 1,
}

/// <summary>
/// List of Text's split into columns.
/// Contains a set of Text's that are automatically lined up by
/// rows and columns, like a big table, making it easy to manage
/// them together.
/// </summary>
/// <remarks>
/// A "text" is one entry of the list (one row of Text objects); a "row" is one physical
/// line, so a word-wrapped text takes several rows. Indices are unsigned (size_t) in C++.
/// </remarks>
public class TextList : InteractiveSurface
{
	private readonly List<List<Text>> _texts = [];
	private readonly List<int> _columns = [];
	/// <summary>Index of the text shown on each physical row.</summary>
	private readonly List<int> _rows = [];
	private Font _big, _small, _font;
	private Language _lang;
	private int _scroll, _visibleRows, _selRow;
	private byte _color, _color2;
	private readonly Dictionary<int, TextHAlign> _align = [];
	private bool _dot, _selectable, _condensed, _contrast, _wrap, _flooding, _ignoreSeparators;
	private Surface _bg, _selector;
	private readonly ArrowButton _up, _down;
	private readonly ScrollBar _scrollbar;
	private int _margin;
	private bool _scrolling = true;
	private readonly List<ArrowButton> _arrowLeft = [], _arrowRight = [];
	private int _arrowPos = -1, _scrollPos = 4;
	private ArrowOrientation _arrowType = ArrowOrientation.ARROW_VERTICAL;
	private ActionHandler _leftClick, _leftPress, _leftRelease, _rightClick, _rightPress, _rightRelease;
	private int _arrowsLeftEdge, _arrowsRightEdge;
	private int _noScrollLeftEdge, _noScrollRightEdge;
	private ComboBox _comboBox;

	/// <summary>Creates a text list with the specified size and position.</summary>
	public TextList(int width, int height, int x = 0, int y = 0) : base(width, height, x, y)
	{
		_up = new ArrowButton(ArrowShape.ARROW_BIG_UP, 13, 14, _x + Width + _scrollPos, _y);
		_up.Visible = false;
		_up.SetTextList(this);
		_down = new ArrowButton(ArrowShape.ARROW_BIG_DOWN, 13, 14, _x + Width + _scrollPos, _y + Height - 14);
		_down.Visible = false;
		_down.SetTextList(this);
		int h = Math.Max(_down.Y - _up.Y - _up.Height, 1);
		_scrollbar = new ScrollBar(_up.Width, h, _x + Width + _scrollPos, _up.Y + _up.Height);
		_scrollbar.Visible = false;
		_scrollbar.SetTextList(this);
	}

	/// <summary>Changes the position of the surface in the X axis.</summary>
	public override int X
	{
		get => base.X;
		set
		{
			base.X = value;
			_up.X = X + Width + _scrollPos;
			_down.X = X + Width + _scrollPos;
			_scrollbar.X = X + Width + _scrollPos;
			if (_selector != null)
				_selector.X = X;
		}
	}

	/// <summary>Changes the position of the surface in the Y axis.</summary>
	public override int Y
	{
		get => base.Y;
		set
		{
			base.Y = value;
			_up.Y = Y;
			_down.Y = Y + Height - 14;
			_scrollbar.Y = _up.Y + _up.Height;
			if (_selector != null)
				_selector.Y = Y;
		}
	}

	/// <summary>Gets the left edge of the arrow column.</summary>
	public int ArrowsLeftEdge => _arrowsLeftEdge;

	/// <summary>Gets the right edge of the arrow column.</summary>
	public int ArrowsRightEdge => _arrowsRightEdge;

	/// <summary>Unpresses all the arrow buttons.</summary>
	public override void Unpress(State state)
	{
		base.Unpress(state);
		foreach (var ab in _arrowLeft)
		{
			ab.Unpress(state);
		}
		foreach (var ab in _arrowRight)
		{
			ab.Unpress(state);
		}
	}

	/// <summary>Changes the color of a specific Text object in the list.</summary>
	public void SetCellColor(int row, int column, byte color)
	{
		_texts[row][column].Color = color;
		_redraw = true;
	}

	/// <summary>Changes the text color of a whole row in the list.</summary>
	public void SetRowColor(int row, byte color)
	{
		foreach (var text in _texts[row])
		{
			text.Color = color;
		}
		_redraw = true;
	}

	/// <summary>Returns the text of a specific Text object in the list.</summary>
	public string GetCellText(int row, int column) => _texts[row][column].GetText();

	/// <summary>Changes the text of a specific Text object in the list.</summary>
	public void SetCellText(int row, int column, string text)
	{
		_texts[row][column].SetText(text);
		_redraw = true;
	}

	/// <summary>Returns the X position of a specific text column in the list.</summary>
	public int GetColumnX(int column) => X + _texts[0][column].X;

	/// <summary>Returns the Y position of a specific text row in the list.</summary>
	public int GetRowY(int row) => Y + _texts[row][0].Y;

	/// <summary>Returns the height of a specific text row in the list.</summary>
	public int GetTextHeight(int row) => _texts[row][0].GetTextHeight();

	/// <summary>Returns the number of lines in the wrapped text for the specified row.</summary>
	public int GetNumTextLines(int row) => _texts[row][0].NumLines;

	/// <summary>Returns the amount of text rows stored in the list.</summary>
	public int Texts => _texts.Count;

	/// <summary>Returns the amount of physical rows stored in the list.</summary>
	public int RowsDoNotUse => _rows.Count;

	/// <summary>Gets the index of the last row in the list, or -1 if the list is empty.</summary>
	public int LastRowIndex => _texts.Count - 1;

	/// <summary>Returns the amount of visible rows stored in the list.</summary>
	public int VisibleRows => _visibleRows;

	/// <summary>
	/// Adds a new row of text to the list, automatically creating
	/// the required Text objects lined up where they need to be.
	/// </summary>
	/// <param name="cols">Number of columns.</param>
	/// <param name="texts">Text for each cell in the new row (C++ varargs).</param>
	public void AddRow(int cols, params string[] texts)
	{
		int ncols;
		if (cols > 0)
		{
			ncols = cols;
		}
		else
		{
			ncols = 1;
		}

		var temp = new List<Text>();
		// Positions are relative to list surface.
		int rowX = 0, rowY = 0, rows = 1, rowHeight = 0;
		if (_texts.Count > 0)
		{
			rowY = _texts[^1][0].Y + _texts[^1][0].Height + _font.Spacing;
		}

		for (int i = 0; i < ncols; ++i)
		{
			int width;
			// Place text
			if (_flooding)
			{
				width = 340;
			}
			else
			{
				width = _columns[i];
			}
			var txt = new Text(width, _font.Height, _margin + rowX, rowY);
			txt.SetPalette(Palette);
			txt.InitText(_big, _small, _lang);
			txt.Color = _color;
			txt.SecondaryColor = _color2;
			TextHAlign align = _align.GetValueOrDefault(i);
			if (align != TextHAlign.ALIGN_LEFT)
			{
				txt.Align = align;
			}
			txt.SetHighContrast(_contrast);
			if (_font == _big)
			{
				txt.SetBig();
			}
			else
			{
				txt.SetSmall();
			}
			if (cols > 0)
				txt.SetText(texts[i]);
			// grab this before we enable word wrapping so we can use it to calculate
			// the total row height below
			int vmargin = _font.Height - txt.GetTextHeight();
			// Wordwrap text if necessary
			if (_wrap && txt.GetTextWidth() > txt.Width)
			{
				txt.SetWordWrap(true, true, _ignoreSeparators);
				rows = Math.Max(rows, txt.NumLines);
			}
			rowHeight = Math.Max(rowHeight, txt.GetTextHeight() + vmargin);

			// Places dots between text
			if (_dot && i < cols - 1)
			{
				string buf = txt.GetText();
				uint w = (uint)txt.GetTextWidth();
				while (w < (uint)_columns[i])
				{
					if (align != TextHAlign.ALIGN_RIGHT)
					{
						w += (uint)(_font.GetChar('.').Crop.w + _font.Spacing);
						buf += '.';
					}
					if (align != TextHAlign.ALIGN_LEFT)
					{
						w += (uint)(_font.GetChar('.').Crop.w + _font.Spacing);
						buf = buf.Insert(0, ".");
					}
				}
				txt.SetText(buf);
			}

			temp.Add(txt);
			if (_condensed)
			{
				rowX += txt.GetTextWidth();
			}
			else
			{
				rowX += _columns[i];
			}
		}

		// ensure all elements in this row are the same height
		for (int i = 0; i < cols; ++i)
		{
			temp[i].SetHeight(rowHeight);
		}

		_texts.Add(temp);
		for (int i = 0; i < rows; ++i)
		{
			_rows.Add(_texts.Count - 1);
		}

		// Place arrow buttons
		// Position defined w.r.t. main window, NOT TextList.
		if (_arrowPos != -1)
		{
			ArrowShape shape1, shape2;
			if (_arrowType == ArrowOrientation.ARROW_VERTICAL)
			{
				shape1 = ArrowShape.ARROW_SMALL_UP;
				shape2 = ArrowShape.ARROW_SMALL_DOWN;
			}
			else
			{
				shape1 = ArrowShape.ARROW_SMALL_LEFT;
				shape2 = ArrowShape.ARROW_SMALL_RIGHT;
			}
			var a1 = new ArrowButton(shape1, 11, 8, X + _arrowPos, Y);
			a1.SetListButton();
			a1.SetPalette(Palette);
			a1.Color = _up.Color;
			a1.OnMouseClick(_leftClick, 0);
			a1.OnMousePress(_leftPress);
			a1.OnMouseRelease(_leftRelease);
			_arrowLeft.Add(a1);
			var a2 = new ArrowButton(shape2, 11, 8, X + _arrowPos + 12, Y);
			a2.SetListButton();
			a2.SetPalette(Palette);
			a2.Color = _up.Color;
			a2.OnMouseClick(_rightClick, 0);
			a2.OnMousePress(_rightPress);
			a2.OnMouseRelease(_rightRelease);
			_arrowRight.Add(a2);
		}

		_redraw = true;
		UpdateArrows();
	}

	/// <summary>Removes the last row from the text list.</summary>
	public void RemoveLastRow()
	{
		if (_texts.Count > 0)
		{
			_texts.RemoveAt(_texts.Count - 1);
		}
		if (_rows.Count > 0)
		{
			int toRemove = _rows[^1];
			while (_rows.Count > 0 && _rows[^1] == toRemove)
			{
				_rows.RemoveAt(_rows.Count - 1);
			}
		}
		if (_arrowPos != -1)
		{
			if (_arrowLeft.Count > 0)
			{
				_arrowLeft.RemoveAt(_arrowLeft.Count - 1);
			}
			if (_arrowRight.Count > 0)
			{
				_arrowRight.RemoveAt(_arrowRight.Count - 1);
			}
		}
		_redraw = true;
		UpdateArrows();
	}

	/// <summary>
	/// Changes the columns that the list contains.
	/// While rows can be unlimited, columns need to be specified
	/// since they can have various widths for lining up the text.
	/// </summary>
	/// <param name="cols">Number of columns.</param>
	/// <param name="widths">Width of each column (C++ varargs).</param>
	public void SetColumns(int cols, params int[] widths)
	{
		_columns.Clear();
		for (int i = 0; i < cols; ++i)
		{
			_columns.Add(widths[i]);
		}
	}

	/// <summary>Replaces a certain amount of colors in the palette of all the text contained in the list.</summary>
	public override void SetPalette(ReadOnlySpan<Rgba> colors, int firstcolor = 0, int ncolors = 256)
	{
		base.SetPalette(colors, firstcolor, ncolors);
		foreach (var vec in _texts)
		{
			foreach (var text in vec)
			{
				text.SetPalette(colors, firstcolor, ncolors);
			}
		}
		foreach (var ab in _arrowLeft)
		{
			ab.SetPalette(colors, firstcolor, ncolors);
		}
		foreach (var ab in _arrowRight)
		{
			ab.SetPalette(colors, firstcolor, ncolors);
		}
		_selector?.SetPalette(colors, firstcolor, ncolors);
		_up.SetPalette(colors, firstcolor, ncolors);
		_down.SetPalette(colors, firstcolor, ncolors);
		_scrollbar.SetPalette(colors, firstcolor, ncolors);
	}

	/// <summary>
	/// Changes the resources for the text in the list
	/// and calculates the selector and visible amount of rows.
	/// </summary>
	public override void InitText(Font big, Font small, Language lang)
	{
		_big = big;
		_small = small;
		_font = small;
		_lang = lang;

		RecreateSelector();

		UpdateVisible();
	}

	/// <summary>Recreates the (hidden) selector for the current font.</summary>
	private void RecreateSelector()
	{
		_selector = new Surface(Width, _font.Height + _font.Spacing, X, Y);
		_selector.SetPalette(Palette);
		_selector.Visible = false;
	}

	/// <summary>Changes the height of the text list.</summary>
	public override void SetHeight(int height)
	{
		base.SetHeight(height);
		Y = _y;
		int h = Math.Max(_down.Y - _up.Y - _up.Height, 1);
		_scrollbar.SetHeight(h);
		UpdateVisible();
	}

	/// <summary>
	/// Changes the color of the text in the list (existing text too)
	/// and of the scroll buttons.
	/// </summary>
	public override void SetColor(byte color)
	{
		_color = color;
		_up.Color = color;
		_down.Color = color;
		_scrollbar.SetColor(color);
		foreach (var vec in _texts)
		{
			foreach (var text in vec)
			{
				text.Color = color;
			}
		}
	}

	/// <summary>The color of the text in the list.</summary>
	public byte Color { get => _color; set => SetColor(value); }

	/// <summary>Changes the secondary color of the text in the list (for text added from then on).</summary>
	public override void SetSecondaryColor(byte color) => _color2 = color;

	/// <summary>The secondary color of the text in the list.</summary>
	public byte SecondaryColor { get => _color2; set => SetSecondaryColor(value); }

	/// <summary>
	/// Enables/disables text wordwrapping. When enabled, rows can
	/// take up multiple lines of the list, otherwise every row
	/// is restricted to one line.
	/// </summary>
	public void SetWordWrap(bool wrap) => _wrap = wrap;

	/// <summary>Enables/disables high contrast color. Mostly used for Battlescape text.</summary>
	public override void SetHighContrast(bool contrast)
	{
		_contrast = contrast;
		foreach (var vec in _texts)
		{
			foreach (var text in vec)
			{
				text.SetHighContrast(contrast);
			}
		}
		_scrollbar.SetHighContrast(contrast);
	}

	/// <summary>
	/// Changes the horizontal alignment of the text in the list. This doesn't change
	/// the alignment of existing text, just the alignment of text added from then on.
	/// </summary>
	/// <param name="align">Horizontal alignment.</param>
	/// <param name="col">The column to set the alignment for (-1 = all columns).</param>
	public void SetAlign(TextHAlign align, int col = -1)
	{
		if (col == -1)
		{
			for (int i = 0; i < _columns.Count; ++i)
			{
				_align[i] = align;
			}
		}
		else
		{
			_align[col] = align;
		}
	}

	/// <summary>If enabled, the text in different columns will be separated by dots.</summary>
	public void SetDot(bool dot) => _dot = dot;

	/// <summary>If enabled, the list will respond to player input, highlighting selected rows and receiving clicks.</summary>
	public void SetSelectable(bool selectable) => _selectable = selectable;

	/// <summary>Changes the text list to use the big-size font.</summary>
	public void SetBig()
	{
		_font = _big;

		RecreateSelector();

		UpdateVisible();
	}

	/// <summary>Changes the text list to use the small-size font.</summary>
	public void SetSmall()
	{
		_font = _small;

		RecreateSelector();

		UpdateVisible();
	}

	/// <summary>If enabled, the columns will match the text width. Otherwise, they will have a fixed width.</summary>
	public void SetCondensed(bool condensed) => _condensed = condensed;

	/// <summary>Returns the currently selected text if the list is selectable, <c>(unsigned)-1</c> if none.</summary>
	public uint SelectedRow
	{
		get
		{
			if (_rows.Count == 0 || _selRow >= _rows.Count)
			{
				return uint.MaxValue;
			}
			else
			{
				return (uint)_rows[_selRow];
			}
		}
	}

	/// <summary>Changes the surface used to draw the background of the selector.</summary>
	public void SetBackground(Surface bg)
	{
		_bg = bg;
		_scrollbar.SetBackground(_bg);
	}

	/// <summary>The horizontal margin placed around the text.</summary>
	public int Margin { get => _margin; set => _margin = value; }

	/// <summary>Sets the no scroll area of the text list.</summary>
	public void SetNoScrollArea(int left, int right)
	{
		_noScrollLeftEdge = left;
		_noScrollRightEdge = right;
	}

	/// <summary>Checks if a given coordinate is inside of the no scroll area of the text list.</summary>
	public bool IsInsideNoScrollArea(int x)
	{
		if (_noScrollRightEdge > 0)
		{
			if (_noScrollLeftEdge <= x && x <= _noScrollRightEdge)
				return true;
		}
		return false;
	}

	/// <summary>Changes the color of the arrow buttons in the list.</summary>
	public void SetArrowColor(byte color)
	{
		_up.Color = color;
		_down.Color = color;
		_scrollbar.SetColor(color);
	}

	/// <summary>Sets the position of the column of arrow buttons in the text list.</summary>
	/// <param name="pos">X in pixels (-1 to disable).</param>
	/// <param name="type">Arrow orientation type.</param>
	public void SetArrowColumn(int pos, ArrowOrientation type)
	{
		_arrowPos = pos;
		_arrowType = type;
		if (pos == -1)
		{
			// turned off
			_arrowsLeftEdge = 0;
			_arrowsRightEdge = 0;
			return;
		}
		_arrowsLeftEdge = X + _arrowPos;
		_arrowsRightEdge = _arrowsLeftEdge + 12 + 11;
	}

	/// <summary>Sets a function to be called every time the left arrows are mouse clicked.</summary>
	public void OnLeftArrowClick(ActionHandler handler)
	{
		_leftClick = handler;
		foreach (var ab in _arrowLeft)
		{
			ab.OnMouseClick(handler, 0);
		}
	}

	/// <summary>Sets a function to be called every time the left arrows are mouse pressed.</summary>
	public void OnLeftArrowPress(ActionHandler handler)
	{
		_leftPress = handler;
		foreach (var ab in _arrowLeft)
		{
			ab.OnMousePress(handler);
		}
	}

	/// <summary>Sets a function to be called every time the left arrows are mouse released.</summary>
	public void OnLeftArrowRelease(ActionHandler handler)
	{
		_leftRelease = handler;
		foreach (var ab in _arrowLeft)
		{
			ab.OnMouseRelease(handler);
		}
	}

	/// <summary>Sets a function to be called every time the right arrows are mouse clicked.</summary>
	public void OnRightArrowClick(ActionHandler handler)
	{
		_rightClick = handler;
		foreach (var ab in _arrowRight)
		{
			ab.OnMouseClick(handler, 0);
		}
	}

	/// <summary>Sets a function to be called every time the right arrows are mouse pressed.</summary>
	public void OnRightArrowPress(ActionHandler handler)
	{
		_rightPress = handler;
		foreach (var ab in _arrowRight)
		{
			ab.OnMousePress(handler);
		}
	}

	/// <summary>Sets a function to be called every time the right arrows are mouse released.</summary>
	public void OnRightArrowRelease(ActionHandler handler)
	{
		_rightRelease = handler;
		foreach (var ab in _arrowRight)
		{
			ab.OnMouseRelease(handler);
		}
	}

	/// <summary>Removes all the rows currently stored in the list.</summary>
	/// <remarks>As in C++, the arrow buttons of the rows are kept and reused by new rows.</remarks>
	public void ClearList()
	{
		foreach (var vec in _texts)
		{
			vec.Clear();
		}
		ScrollUp(true, false);
		_texts.Clear();
		_rows.Clear();
		_redraw = true;
	}

	/// <summary>Scrolls the text in the list up by one row (or <paramref name="howMany"/>) or to the top.</summary>
	/// <param name="toMax">If true then scrolls to the top of the list, otherwise one row up.</param>
	/// <param name="scrollByWheel">If true then use wheel scroll, otherwise scroll normally.</param>
	/// <param name="howMany">Rows to scroll (unsigned in C++).</param>
	public void ScrollUp(bool toMax, bool scrollByWheel = false, int howMany = 1)
	{
		if (!_scrolling)
			return;
		if (_rows.Count > _visibleRows && _scroll > 0)
		{
			if (toMax)
			{
				ScrollTo(0);
			}
			else
			{
				if (scrollByWheel)
				{
					ScrollTo(_scroll - (int)Math.Min((uint)Options.mousewheelSpeed, (uint)_scroll));
				}
				else
				{
					ScrollTo(_scroll - (int)Math.Min((uint)howMany, (uint)_scroll));
				}
			}
		}
	}

	/// <summary>Scrolls the text in the list down by one row (or <paramref name="howMany"/>) or to the bottom.</summary>
	/// <param name="toMax">If true then scrolls to the bottom of the list, otherwise one row down.</param>
	/// <param name="scrollByWheel">If true then use wheel scroll, otherwise scroll normally.</param>
	/// <param name="howMany">Rows to scroll (unsigned in C++).</param>
	public void ScrollDown(bool toMax, bool scrollByWheel = false, int howMany = 1)
	{
		if (!_scrolling)
			return;
		if (_rows.Count > _visibleRows && _scroll < _rows.Count - _visibleRows)
		{
			if (toMax)
			{
				ScrollTo(_rows.Count - _visibleRows);
			}
			else
			{
				if (scrollByWheel)
				{
					ScrollTo(_scroll + Options.mousewheelSpeed);
				}
				else
				{
					ScrollTo(_scroll + howMany);
				}
			}
		}
	}

	/// <summary>Updates the visibility of the arrow buttons according to the current scroll position.</summary>
	private void UpdateArrows()
	{
		_up.Visible = _rows.Count > _visibleRows /*&& _scroll > 0*/;
		_down.Visible = _rows.Count > _visibleRows /*&& _scroll < _rows.size() - _visibleRows*/;
		_scrollbar.Visible = _rows.Count > _visibleRows;
		_scrollbar.Invalidate();
		_scrollbar.Blit(this);
	}

	/// <summary>Updates the amount of visible rows according to the current list and font size.</summary>
	private void UpdateVisible()
	{
		_visibleRows = CountVisibleRows(Height, _font.Height + _font.Spacing);
		UpdateArrows();
	}

	/// <summary>Number of rows that fit (also partially) into the given height (part of C++ updateVisible()).</summary>
	internal static int CountVisibleRows(int height, int rowHeight)
	{
		int visibleRows = 0;
		for (int y = 0; y < height; y += rowHeight)
		{
			visibleRows++;
		}
		return visibleRows;
	}

	/// <summary>
	/// Clamps a scroll position to 0..rows-visibleRows (part of C++ scrollTo()).
	/// Negative values count as huge, like the C++ size_t argument.
	/// </summary>
	internal static int ClampScroll(int scroll, int rows, int visibleRows) => (int)Math.Min((uint)scroll, (uint)(rows - visibleRows));

	/// <summary>
	/// Number of physical rows of the first visible text that are scrolled above the top of the list
	/// (wrapped items are drawn that far above the visible surface; part of C++ draw()/blit()).
	/// </summary>
	internal static int WrappedRowsAbove(IReadOnlyList<int> rows, int scroll)
	{
		int n = 0;
		for (int row = scroll; row > 0 && rows[row] == rows[row - 1]; --row)
		{
			n++;
		}
		return n;
	}

	/// <summary>
	/// Range [start, end) of texts whose arrow buttons receive events: the texts that have
	/// their first row on screen (part of C++ handle()).
	/// </summary>
	internal static (int Start, int End) VisibleArrowRange(IReadOnlyList<int> rows, int scroll, int visibleRows)
	{
		int startArrowIdx = rows[scroll];
		if (0 < scroll && rows[scroll] == rows[scroll - 1])
		{
			// arrows for first partially-visible line of text are off-screen; don't process them
			++startArrowIdx;
		}
		int endArrowIdx = rows[scroll] + 1;
		int endRow = Math.Min(rows.Count, scroll + visibleRows);
		for (int i = scroll + 1; i < endRow; ++i)
		{
			if (rows[i] != rows[i - 1])
			{
				++endArrowIdx;
			}
		}
		return (startArrowIdx, endArrowIdx);
	}

	/// <summary>Physical rows (text index per row) for tests.</summary>
	internal IReadOnlyList<int> Rows => _rows;

	/// <summary>Changes whether the list can be scrolled.</summary>
	/// <param name="scrolling">True to allow scrolling, false otherwise.</param>
	/// <param name="scrollPos">Custom X position for the scroll buttons.</param>
	public void SetScrolling(bool scrolling, int scrollPos = 4)
	{
		_scrolling = scrolling;
		if (scrollPos != _scrollPos)
		{
			_scrollPos = scrollPos;
			_up.X = X + Width + _scrollPos;
			_down.X = X + Width + _scrollPos;
			_scrollbar.X = X + Width + _scrollPos;
		}
	}

	/// <summary>Draws the text list and all the text contained within.</summary>
	public override void Draw()
	{
		base.Draw();
		int y = 0;
		if (_rows.Count > 0)
		{
			// for wrapped items, offset the draw height above the visible surface
			// so that the correct row appears at the top
			y -= WrappedRowsAbove(_rows, _scroll) * (_font.Height + _font.Spacing);
			for (int i = _rows[_scroll]; i < _texts.Count && i < _rows[_scroll] + _visibleRows; ++i)
			{
				foreach (var text in _texts[i])
				{
					text.Y = y;
					text.Blit(this);
				}
				if (_texts[i].Count > 0)
				{
					y += _texts[i][0].Height + _font.Spacing;
				}
				else
				{
					y += _font.Height + _font.Spacing;
				}
			}
		}
	}

	/// <summary>Blits the text list and selector.</summary>
	public override void Blit(Surface surface)
	{
		if (_visible && !_hidden)
		{
			_selector.Blit(surface);
		}
		base.Blit(surface);
		if (_visible && !_hidden)
		{
			if (_arrowPos != -1 && _rows.Count > 0)
			{
				int y = Y;
				y -= WrappedRowsAbove(_rows, _scroll) * (_font.Height + _font.Spacing);
				int maxY = Y + Height;
				for (int i = _rows[_scroll]; i < _texts.Count && i < _rows[_scroll] + _visibleRows && y < maxY; ++i)
				{
					_arrowLeft[i].Y = y;
					_arrowRight[i].Y = y;

					if (y >= Y)
					{
						// only blit arrows that belong to texts that have their first row on-screen
						_arrowLeft[i].Blit(surface);
						_arrowRight[i].Blit(surface);
					}

					if (_texts[i].Count > 0)
					{
						y += _texts[i][0].Height + _font.Spacing;
					}
					else
					{
						y += _font.Height + _font.Spacing;
					}
				}
			}
			_up.Blit(surface);
			_down.Blit(surface);
			_scrollbar.Blit(surface);
		}
	}

	/// <summary>Passes events to arrow buttons.</summary>
	public override void Handle(Action action, State state)
	{
		base.Handle(action, state);
		_up.Handle(action, state);
		_down.Handle(action, state);
		_scrollbar.Handle(action, state);
		if (_arrowPos != -1 && _rows.Count > 0)
		{
			var (startArrowIdx, endArrowIdx) = VisibleArrowRange(_rows, _scroll, _visibleRows);
			for (int i = startArrowIdx; i < endArrowIdx; ++i)
			{
				_arrowLeft[i].Handle(action, state);
				_arrowRight[i].Handle(action, state);
			}
		}
		// scrolling by keyboard
		// TODO: C++ also requires (state->hasOnlyOneScrollableTextList() || isMouseCursorOverMe(state));
		// enable once State.HasOnlyOneScrollableTextList and State.CursorX/CursorY are ported.
		if (action.Details.type == SDLEventType.SDL_KEYDOWN &&
			_rows.Count > _visibleRows &&
			IsKeyboardScrollTarget(state))
		{
			if (action.Details.key.keysym.sym == SDLKey.SDLK_PAGEDOWN)
			{
				ScrollDown(false, false, _visibleRows);
			}
			else if (action.Details.key.keysym.sym == SDLKey.SDLK_PAGEUP)
			{
				ScrollUp(false, false, _visibleRows);
			}
		}
	}

	/// <summary>C++: <c>state->hasOnlyOneScrollableTextList() || isMouseCursorOverMe(state)</c> (not ported yet, see Handle).</summary>
	private static bool IsKeyboardScrollTarget(State state) => false;

	/// <summary>Is the mouse cursor over the list?</summary>
	/// <remarks>Needs State.CursorX/CursorY (C++ <c>state->getCursorX()</c>), which are not ported yet.</remarks>
	public bool IsMouseCursorOverMe(State state) => throw new NotImplementedException("needs State.CursorX/CursorY");

	/// <summary>Is the cursor (screen coordinates) over the list? Bounds are inclusive like in C++.</summary>
	internal bool IsMouseCursorOverMe(int cursorX, int cursorY)
	{
		if (cursorX < _x ||
			cursorX > _x + Width ||
			cursorY < _y ||
			cursorY > _y + Height)
		{
			return false;
		}
		return true;
	}

	/// <summary>Passes ticks to arrow buttons.</summary>
	public override void Think()
	{
		base.Think();
		_up.Think();
		_down.Think();
		_scrollbar.Think();
		foreach (var ab in _arrowLeft)
		{
			ab.Think();
		}
		foreach (var ab in _arrowRight)
		{
			ab.Think();
		}
	}

	/// <summary>Ignores any mouse clicks that aren't on a row; scrolls with the mouse wheel.</summary>
	public override void MousePress(Action action, State state)
	{
		bool allowScroll = true;
		if (Options.changeValueByMouseWheel != 0)
		{
			allowScroll = action.AbsoluteXMouse < _arrowsLeftEdge || action.AbsoluteXMouse > _arrowsRightEdge;
		}
		if (IsInsideNoScrollArea((int)action.AbsoluteXMouse))
		{
			allowScroll = false;
		}
		if (allowScroll)
		{
			if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELUP) ScrollUp(false, true);
			else if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELDOWN) ScrollDown(false, true);
		}
		if (_selectable)
		{
			if (_selRow < _rows.Count)
			{
				base.MousePress(action, state);
			}
		}
		else
		{
			base.MousePress(action, state);
		}
	}

	/// <summary>Ignores any mouse clicks that aren't on a row.</summary>
	public override void MouseRelease(Action action, State state)
	{
		if (_selectable)
		{
			if (_selRow < _rows.Count)
			{
				base.MouseRelease(action, state);
			}
		}
		else
		{
			base.MouseRelease(action, state);
		}
	}

	/// <summary>Ignores any mouse clicks that aren't on a row; updates an attached combo box.</summary>
	public override void MouseClick(Action action, State state)
	{
		if (_selectable)
		{
			if (_selRow < _rows.Count)
			{
				base.MouseClick(action, state);
				if (_comboBox != null && action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
				{
					_comboBox.Selected = _selRow;
					_comboBox.Toggle(false, true);
				}
			}
		}
		else
		{
			base.MouseClick(action, state);
		}
	}

	/// <summary>Selects the row the mouse is over.</summary>
	public override void MouseOver(Action action, State state)
	{
		if (_selectable)
		{
			int rowHeight = _font.Height + _font.Spacing; //theoretical line height
			_selRow = Math.Max(0, _scroll + (int)Math.Floor(action.RelativeYMouse / (rowHeight * action.YScale)));
			if (_selRow < _rows.Count)
			{
				Text selText = _texts[_rows[_selRow]][0];
				int y = Y + selText.Y;
				int actualHeight = selText.Height + _font.Spacing; //current line height
				if (y < Y || y + actualHeight > Y + Height)
				{
					actualHeight /= 2;
				}
				if (y < Y)
				{
					y = Y;
				}
				if (_selector.Height != actualHeight)
				{
					// resizing doesn't work, but recreating does, so let's do that!
					_selector = new Surface(Width, actualHeight, X, y);
					_selector.SetPalette(Palette);
				}
				_selector.Y = y;
				_selector.Copy(_bg);
				if (_contrast)
				{
					_selector.OffsetBlock(-5);
				}
				else if (_comboBox != null)
				{
					_selector.Offset(+1, Acom.Palette.BackPos);
				}
				else
				{
					_selector.OffsetBlock(-10);
				}
				_selector.Visible = true;
			}
			else
			{
				_selector.Visible = false;
			}
		}

		base.MouseOver(action, state);
	}

	/// <summary>Deselects the row.</summary>
	public override void MouseOut(Action action, State state)
	{
		if (_selectable)
		{
			_selector.Visible = false;
		}

		base.MouseOut(action, state);
	}

	/// <summary>The scroll depth (index of the first visible physical row).</summary>
	public int Scroll => _scroll;

	/// <summary>Sets the scroll depth, clamped to the list (negative values count as huge, like C++ size_t).</summary>
	public void ScrollTo(int scroll)
	{
		if (!_scrolling)
			return;
		if (_rows.Count <= _visibleRows)
			return;
		_scroll = ClampScroll(scroll, _rows.Count, _visibleRows);
		Draw(); // can't just set _redraw here because reasons
		UpdateArrows();
	}

	/// <summary>The combobox that this list is attached to, if any.</summary>
	public ComboBox ComboBox { get => _comboBox; set => _comboBox = value; }

	/// <summary>Changes the color of the scroll buttons.</summary>
	public override void SetBorderColor(byte color)
	{
		_up.Color = color;
		_down.Color = color;
		_scrollbar.SetColor(color);
	}

	/// <summary>Gets the color of the scrollbar.</summary>
	public int ScrollbarColor => _scrollbar.Color;

	/// <summary>Is the scrollbar shown (more rows than fit)?</summary>
	public bool IsScrollbarVisible => _scrollbar.Visible;

	/// <summary>Allows the cell to flood into other columns.</summary>
	public void SetFlooding(bool flooding) => _flooding = flooding;

	/// <summary>Treat separators as spaces (false) or as normal text (true)?</summary>
	public void SetIgnoreSeparators(bool ignoreSeparators) => _ignoreSeparators = ignoreSeparators;
}
