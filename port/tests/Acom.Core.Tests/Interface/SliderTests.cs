using static Acom.Tests.Interface.WidgetTestHelpers;

namespace Acom.Tests.Interface;

public class SliderTests
{
	[Theory]
	[InlineData(50, 0, 100, 50, 0.5)]
	[InlineData(150, 0, 100, 100, 1.0)]
	[InlineData(-5, 0, 100, 0, 0.0)]
	[InlineData(7, 3, 3, 0, 0.0)] // empty range: value 0, position 0
	[InlineData(30, 100, 0, 30, 0.7)] // reversed range
	[InlineData(130, 100, 0, 100, 0.0)]
	public void Values_are_clamped_and_mapped_to_a_position(int value, int min, int max, int expected, double pos)
	{
		var (v, p) = Slider.ClampValue(value, min, max);
		Assert.Equal(expected, v);
		Assert.Equal(pos, p, 12);
	}

	[Theory]
	[InlineData(60, 0, 50)]
	[InlineData(65, -5, 50)] // drag offset
	[InlineData(200, 0, 100)] // clamped to the end of the track
	[InlineData(0, 0, 0)]
	[InlineData(11, 0, 1)] // 1.0 rounds to 1
	public void Cursor_maps_to_a_value(int cursorX, int offsetX, int expected)
	{
		Assert.Equal(expected, Slider.CursorToValue(cursorX, offsetX, 10, 110, 0, 100));
	}

	[Fact]
	public void Rounding_is_half_away_from_zero()
	{
		// position 0.5 of a range of size 1 / -1
		Assert.Equal(1, Slider.CursorToValue(2, 0, 0, 4, 0, 1));
		Assert.Equal(-1, Slider.CursorToValue(2, 0, 0, 4, 0, -1));
		Assert.Equal(60, Slider.ButtonX(10, 110, 0.5));
		Assert.Equal(60, Slider.ButtonX(10, 111, 0.5)); // 60.5 is floored
	}

	// Runs once Text, TextButton and Frame are ported.
	[Fact(SkipExceptions = [typeof(NotImplementedException)])]
	public void Dragging_the_button_changes_the_value()
	{
		var slider = new Slider(100, 16);
		slider.SetRange(0, 10);
		slider.Value = 5;
		int changes = 0;
		slider.OnChange(_ => changes++);

		// track 8..92, button 10 wide: min X 8, max X 82, value 5 at X 45
		slider.Handle(Press(50, 8), null);
		Assert.Equal(5, slider.Value);
		Assert.Equal(1, changes);
		slider.Handle(Move(99, 8), null);
		Assert.Equal(10, slider.Value);
		slider.Handle(Move(-20, 8), null);
		Assert.Equal(0, slider.Value);
		slider.Handle(Release(-20, 8), null);
		slider.Handle(Move(50, 8), null);
		Assert.Equal(0, slider.Value);
		Assert.Equal(3, changes);
	}
}
