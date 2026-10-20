using static Acom.Tests.Interface.WidgetTestHelpers;

namespace Acom.Tests.Interface;

// The tests set Options.keyboardMode, which OptionsTests reset and check.
[Collection(nameof(Engine.OptionsTests))]
public class TextEditTests
{
	[Theory]
	[InlineData(TextEditConstraint.TEC_NUMERIC_POSITIVE, "", 0, '5', true)]
	[InlineData(TextEditConstraint.TEC_NUMERIC_POSITIVE, "", 0, '-', false)]
	[InlineData(TextEditConstraint.TEC_NUMERIC_POSITIVE, "1", 1, 'a', false)]
	[InlineData(TextEditConstraint.TEC_NUMERIC, "", 0, '-', true)]
	[InlineData(TextEditConstraint.TEC_NUMERIC, "", 0, '+', true)]
	[InlineData(TextEditConstraint.TEC_NUMERIC, "5", 0, '-', true)]
	[InlineData(TextEditConstraint.TEC_NUMERIC, "-5", 0, '-', false)] // only one sign
	[InlineData(TextEditConstraint.TEC_NUMERIC, "-5", 0, '3', false)] // nothing before the sign
	[InlineData(TextEditConstraint.TEC_NUMERIC, "-5", 1, '3', true)]
	[InlineData(TextEditConstraint.TEC_NUMERIC, "5", 1, '-', false)] // sign only at the start
	[InlineData(TextEditConstraint.TEC_NONE, "", 0, ' ', true)]
	[InlineData(TextEditConstraint.TEC_NONE, "", 0, '~', true)]
	[InlineData(TextEditConstraint.TEC_NONE, "", 0, '\u007f', false)]
	[InlineData(TextEditConstraint.TEC_NONE, "", 0, '\u009f', false)]
	[InlineData(TextEditConstraint.TEC_NONE, "", 0, 'é', true)]
	[InlineData(TextEditConstraint.TEC_NONE, "", 0, '\0', false)]
	[InlineData(TextEditConstraint.TEC_NONE, "", 0, '\t', false)]
	public void Characters_are_checked_against_the_constraint(TextEditConstraint constraint, string value, int caretPos, char c, bool valid)
	{
		Assert.Equal(valid, TextEdit.IsValidChar(constraint, value, caretPos, c));
	}

	private static (TextEdit Edit, ModalState State) Create(int width = 100)
	{
		Options.keyboardMode = KeyboardType.KEYBOARD_ON;
		var state = new ModalState();
		var edit = new TextEdit(state, width, 16, 10, 10);
		var font = TerminalFont();
		edit.InitText(font, font, new Language());
		return (edit, state);
	}

	private static void Type(TextEdit edit, string text)
	{
		foreach (char c in text)
			edit.KeyboardPress(Key(SDLKey.SDLK_UNKNOWN, c), null);
	}

	// Runs once Text is ported.
	[Fact(SkipExceptions = [typeof(NotImplementedException)])]
	public void Caret_moves_and_edits_at_its_position()
	{
		var (edit, state) = Create();
		edit.SetFocus(true);
		Assert.True(edit.IsFocused);
		Assert.Same(edit, state.Modal);

		edit.Text = "12";
		Assert.Equal(2, edit.CaretPos);
		Type(edit, "3");
		Assert.Equal("123", edit.Text);
		edit.KeyboardPress(Key(SDLKey.SDLK_LEFT), null);
		edit.KeyboardPress(Key(SDLKey.SDLK_LEFT), null);
		Assert.Equal(1, edit.CaretPos);
		Type(edit, "x");
		Assert.Equal("1x23", edit.Text);
		edit.KeyboardPress(Key(SDLKey.SDLK_BACKSPACE), null);
		Assert.Equal(("123", 1), (edit.Text, edit.CaretPos));
		edit.KeyboardPress(Key(SDLKey.SDLK_DELETE), null);
		Assert.Equal(("13", 1), (edit.Text, edit.CaretPos));
		edit.KeyboardPress(Key(SDLKey.SDLK_HOME), null);
		edit.KeyboardPress(Key(SDLKey.SDLK_BACKSPACE), null);
		Assert.Equal(("13", 0), (edit.Text, edit.CaretPos));
		edit.KeyboardPress(Key(SDLKey.SDLK_END), null);
		edit.KeyboardPress(Key(SDLKey.SDLK_RIGHT), null);
		Assert.Equal(2, edit.CaretPos);
	}

	[Fact(SkipExceptions = [typeof(NotImplementedException)])]
	public void Input_respects_constraint_and_width()
	{
		var (edit, _) = Create(width: 50); // 9 pixels per character: at most 5
		edit.SetFocus(true);
		edit.SetConstraint(TextEditConstraint.TEC_NUMERIC);
		int changes = 0;
		edit.OnChange(_ => changes++);
		Type(edit, "-1a2+34567");
		Assert.Equal("-1234", edit.Text);
		Assert.Equal(10, changes); // every key press reports a change
	}

	[Fact(SkipExceptions = [typeof(NotImplementedException)])]
	public void Enter_and_escape_unfocus()
	{
		var (edit, state) = Create();
		edit.SetFocus(true);
		int enters = 0;

		// empty text without enter handler: stays focused
		edit.KeyboardPress(Key(SDLKey.SDLK_RETURN), null);
		Assert.True(edit.IsFocused);

		Type(edit, "abc");
		edit.KeyboardPress(Key(SDLKey.SDLK_RETURN), null);
		Assert.False(edit.IsFocused);
		Assert.Null(state.Modal);
		Assert.Equal("abc", edit.Text);

		edit.OnEnter(_ => enters++);
		edit.SetFocus(true);
		edit.KeyboardPress(Key(SDLKey.SDLK_ESCAPE), null);
		Assert.False(edit.IsFocused);
		Assert.Equal("", edit.Text);
		Assert.Equal(1, enters);
	}

	[Fact(SkipExceptions = [typeof(NotImplementedException)])]
	public void Clicks_focus_and_place_the_caret()
	{
		var (edit, state) = Create();
		edit.Text = "abcd";
		edit.Handle(Press(12, 12), state);
		edit.Handle(Release(12, 12), state);
		Assert.True(edit.IsFocused);
		Assert.Equal(4, edit.CaretPos);

		// relative x 14: past the middle of 'b' (9..18 is 'b', middle 13.5)
		edit.Handle(Press(24, 12), state);
		edit.Handle(Release(24, 12), state);
		Assert.Equal(2, edit.CaretPos);
		// relative x 13: before the middle of 'b'
		edit.Handle(Press(23, 12), state);
		edit.Handle(Release(23, 12), state);
		Assert.Equal(1, edit.CaretPos);

		// a click outside unfocuses a modal text edit
		edit.Handle(Press(200, 12), state);
		Assert.False(edit.IsFocused);
	}

	[Fact(SkipExceptions = [typeof(NotImplementedException)])]
	public void Keyboard_off_mode_picks_characters_with_the_arrows()
	{
		var (edit, _) = Create();
		Options.keyboardMode = KeyboardType.KEYBOARD_OFF;
		try
		{
			edit.SetFocus(true);
			edit.KeyboardPress(Key(SDLKey.SDLK_UP), null); // 'A' -> 'B'
			edit.KeyboardPress(Key(SDLKey.SDLK_RIGHT), null);
			edit.KeyboardPress(Key(SDLKey.SDLK_DOWN), null);
			edit.KeyboardPress(Key(SDLKey.SDLK_DOWN), null);
			edit.KeyboardPress(Key(SDLKey.SDLK_RIGHT), null);
			Assert.Equal("B@", edit.Text);
			edit.KeyboardPress(Key(SDLKey.SDLK_LEFT), null);
			Assert.Equal("B", edit.Text);
		}
		finally
		{
			Options.keyboardMode = KeyboardType.KEYBOARD_ON;
		}
	}
}
