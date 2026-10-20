using static Acom.Tests.Interface.WidgetTestHelpers;

namespace Acom.Tests.Interface;

// The list tests set Options.mousewheelSpeed and Options.changeValueByMouseWheel.
[Collection(nameof(Engine.OptionsTests))]
public class TextListTests
{
	[Theory]
	[InlineData(0, 8, 0)]
	[InlineData(1, 8, 1)]
	[InlineData(64, 8, 8)]
	[InlineData(65, 8, 9)] // a partially visible row counts
	[InlineData(80, 9, 9)]
	public void Visible_rows_include_partial_rows(int height, int rowHeight, int expected)
	{
		Assert.Equal(expected, TextList.CountVisibleRows(height, rowHeight));
	}

	[Theory]
	[InlineData(0, 20, 8, 0)]
	[InlineData(5, 20, 8, 5)]
	[InlineData(15, 20, 8, 12)]
	[InlineData(-1, 20, 8, 12)] // size_t in C++: negative is huge
	public void Scroll_is_clamped_to_the_last_page(int scroll, int rows, int visible, int expected)
	{
		Assert.Equal(expected, TextList.ClampScroll(scroll, rows, visible));
	}

	// Text 0 wraps into three rows, text 2 into two rows.
	private static readonly int[] WrappedRows = [0, 0, 0, 1, 2, 2, 3];

	[Theory]
	[InlineData(0, 0)]
	[InlineData(1, 1)]
	[InlineData(2, 2)]
	[InlineData(3, 0)]
	[InlineData(5, 1)]
	[InlineData(6, 0)]
	public void Wrapped_text_is_drawn_above_the_top(int scroll, int expected)
	{
		Assert.Equal(expected, TextList.WrappedRowsAbove(WrappedRows, scroll));
	}

	[Theory]
	[InlineData(0, 3, 0, 1)] // only text 0 is on screen
	[InlineData(1, 3, 1, 2)] // text 0 starts above the top: its arrows are skipped
	[InlineData(3, 3, 1, 3)]
	[InlineData(4, 3, 2, 4)]
	[InlineData(4, 10, 2, 4)] // end of the list
	public void Arrows_get_events_for_texts_starting_on_screen(int scroll, int visible, int start, int end)
	{
		Assert.Equal((start, end), TextList.VisibleArrowRange(WrappedRows, scroll, visible));
	}

	private static TextList Create(int rows, int visibleRows = 2)
	{
		var font = TerminalFont();
		int rowHeight = font.Height + font.Spacing;
		var list = new TextList(100, visibleRows * rowHeight, 10, 20);
		list.InitText(font, font, new Language());
		list.SetColumns(2, 50, 50);
		for (int i = 0; i < rows; ++i)
			list.AddRow(2, $"r{i}", "x");
		return list;
	}

	/// <summary>A wheel step is a press and a release of the wheel "button", as in SDL 1.2.</summary>
	private static void Wheel(TextList list, int x, int y, byte button)
	{
		list.Handle(Press(x, y, button), null);
		list.Handle(Release(x, y, button), null);
	}

	// Runs once Text, ArrowButton and ScrollBar's dependencies are ported.
	[Fact(SkipExceptions = [typeof(NotImplementedException)])]
	public void Rows_are_added_and_removed()
	{
		var list = Create(0);
		Assert.Equal(2, list.VisibleRows);
		Assert.Equal(-1, list.LastRowIndex);
		Assert.Equal(uint.MaxValue, list.SelectedRow);

		list.AddRow(2, "a", "b");
		list.AddRow(2, "c", "d");
		Assert.Equal(2, list.Texts);
		Assert.Equal(2, list.RowsDoNotUse);
		Assert.Equal(1, list.LastRowIndex);
		Assert.Equal("d", list.GetCellText(1, 1));
		Assert.False(list.IsScrollbarVisible);

		list.AddRow(2, "e", "f");
		Assert.True(list.IsScrollbarVisible);
		list.SetCellText(2, 0, "g");
		Assert.Equal("g", list.GetCellText(2, 0));
		Assert.Equal(20, list.GetRowY(0));
		Assert.Equal((10, 60), (list.GetColumnX(0), list.GetColumnX(1)));

		list.RemoveLastRow();
		Assert.Equal((2, 2), (list.Texts, list.RowsDoNotUse));
		Assert.False(list.IsScrollbarVisible);

		list.ClearList();
		Assert.Equal((0, 0), (list.Texts, list.RowsDoNotUse));
	}

	[Fact(SkipExceptions = [typeof(NotImplementedException)])]
	public void Scrolling_stays_within_the_list()
	{
		int oldSpeed = Options.mousewheelSpeed;
		try
		{
			Options.mousewheelSpeed = 3;
			var list = Create(6);
			list.ScrollDown(false);
			Assert.Equal(1, list.Scroll);
			list.ScrollDown(false, false, 10);
			Assert.Equal(4, list.Scroll);
			list.ScrollDown(false);
			Assert.Equal(4, list.Scroll);
			list.ScrollUp(false, true); // wheel: 3 rows
			Assert.Equal(1, list.Scroll);
			list.ScrollUp(false, true); // never below 0
			Assert.Equal(0, list.Scroll);
			list.ScrollDown(true);
			Assert.Equal(4, list.Scroll);
			list.ScrollUp(true);
			Assert.Equal(0, list.Scroll);
			list.ScrollTo(-1);
			Assert.Equal(4, list.Scroll);

			list.SetScrolling(false);
			list.ScrollTo(0);
			list.ScrollUp(true);
			Assert.Equal(4, list.Scroll);
			list.SetScrolling(true);

			// clearing scrolls back to the top
			list.ClearList();
			Assert.Equal(0, list.Scroll);

			// lists that fit do not scroll
			list.AddRow(2, "a", "b");
			list.ScrollDown(true);
			Assert.Equal(0, list.Scroll);
		}
		finally
		{
			Options.mousewheelSpeed = oldSpeed;
		}
	}

	[Fact(SkipExceptions = [typeof(NotImplementedException)])]
	public void Mouse_wheel_scrolls_outside_the_no_scroll_area()
	{
		int oldSpeed = Options.mousewheelSpeed;
		int oldChange = Options.changeValueByMouseWheel;
		try
		{
			Options.mousewheelSpeed = 1;
			Options.changeValueByMouseWheel = 0;
			var list = Create(6);
			Wheel(list, 20, 25, SDLMouse.SDL_BUTTON_WHEELDOWN);
			Assert.Equal(1, list.Scroll);

			list.SetNoScrollArea(15, 30);
			Assert.True(list.IsInsideNoScrollArea(15));
			Assert.False(list.IsInsideNoScrollArea(31));
			Wheel(list, 20, 25, SDLMouse.SDL_BUTTON_WHEELDOWN);
			Assert.Equal(1, list.Scroll);
			Wheel(list, 40, 25, SDLMouse.SDL_BUTTON_WHEELUP);
			Assert.Equal(0, list.Scroll);
		}
		finally
		{
			Options.mousewheelSpeed = oldSpeed;
			Options.changeValueByMouseWheel = oldChange;
		}
	}

	[Fact(SkipExceptions = [typeof(NotImplementedException)])]
	public void Hovering_selects_the_row_under_the_cursor()
	{
		var list = Create(6);
		list.SetSelectable(true);
		list.SetBackground(new Surface(320, 200));
		int rowHeight = TerminalFont().Height + TerminalFont().Spacing;

		list.Handle(Move(15, 20 + rowHeight + 1), null);
		Assert.Equal(1u, list.SelectedRow);
		list.ScrollDown(false);
		list.Handle(Move(16, 20 + rowHeight + 1), null);
		Assert.Equal(2u, list.SelectedRow);
		list.Handle(Move(16, 20), null);
		Assert.Equal(1u, list.SelectedRow);
	}

	[Fact(SkipExceptions = [typeof(NotImplementedException)])]
	public void Wrapped_texts_take_several_rows()
	{
		var font = TerminalFont();
		var list = new TextList(100, 4 * (font.Height + font.Spacing));
		list.InitText(font, font, new Language());
		list.SetColumns(1, 50);
		list.SetWordWrap(true);
		list.AddRow(1, "aaaa bbbb cccc");
		list.AddRow(1, "dd");
		int lines = list.GetNumTextLines(0);
		Assert.True(lines > 1);
		Assert.Equal(lines + 1, list.RowsDoNotUse);
		Assert.Equal(2, list.Texts);
		int[] expected = [.. Enumerable.Repeat(0, lines), 1];
		Assert.Equal(expected, list.Rows);
	}
}
