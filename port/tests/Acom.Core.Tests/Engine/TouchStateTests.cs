namespace Acom.Tests.Engine;

// Game sets a static game pointer for State; Options are static as well.
[Collection(nameof(GameTests))]
public class TouchStateTests
{
	private sealed class TestTouchState : TouchState
	{
		public TextButton Touch => _btnTouch;
		public TextButton Btn1 => _btn1;
		public TextButton Btn10 => _btn10;
		public TextButton Btn100 => _btn100;
		public TextButton Lmb => _btnLMB;
		public TextButton Rmb => _btnRMB;
		public TextButton Mmb => _btnMMB;
		public ToggleTextButton Ctrl => _btnCtrl;
		public ToggleTextButton Shift => _btnShift;
		public TextButton Owner100 { get => _owner100.Value!; set => _owner100.Value = value; }
		public TextButton OwnerLRM { get => _ownerLRM.Value!; set => _ownerLRM.Value = value; }
	}

	private static void WithTouchButtons(bool enabled, System.Action test)
	{
		bool old = Options.oxceBaseTouchButtons;
		Options.oxceBaseTouchButtons = enabled;
		try
		{
			test();
		}
		finally
		{
			Options.oxceBaseTouchButtons = old;
		}
	}

	[Fact]
	public void Without_touch_buttons_only_the_flags_are_reset()
	{
		WithTouchButtons(false, () =>
		{
			var game = new Game(new Screen(4, 4)) { RMBFlag = true, CtrlPressedFlag = true, ScrollStep = 100 };
			var state = new TestTouchState();
			state.TouchComponentsCreate(null);
			state.TouchComponentsConfigure();
			state.TouchComponentsRefresh();
			Assert.False(game.RMBFlag);
			Assert.False(game.CtrlPressedFlag);
			Assert.Null(state.Touch);
		});
	}

	[Fact]
	public void Buttons_are_laid_out_hidden_and_shown_by_the_touch_button()
	{
		WithTouchButtons(true, () =>
		{
			_ = new Game(new Screen(4, 4));
			var state = new TestTouchState();
			state.TouchComponentsCreate(null, hideGroup100: true, horizontalOffset: 2, verticalOffset: 3);
			state.TouchComponentsConfigure();
			Assert.Equal((275, 10, 40), (state.Touch.X, state.Touch.Y, state.Touch.Width));
			Assert.Equal((63 + 26, 10), (state.Btn10.X, state.Btn10.Y));
			Assert.Equal(67 + 4 * 26, state.Rmb.X);
			Assert.Equal((75 + 8 * 26, 32), (state.Shift.X, state.Shift.Width));
			Assert.True(state.Touch.Visible);
			Assert.False(state.Lmb.Visible);
			Assert.Same(state.Btn1, state.Owner100);
			Assert.Same(state.Lmb, state.OwnerLRM);

			state.BtnTouchClick(null);
			Assert.False(state.Touch.Visible);
			Assert.False(state.Btn1.Visible); // hideGroup100
			Assert.True(state.Lmb.Visible);
			Assert.True(state.Ctrl.Visible);
		});
	}

	[Fact]
	public void Group_buttons_and_toggles_drive_the_game_flags()
	{
		WithTouchButtons(true, () =>
		{
			var game = new Game(new Screen(4, 4));
			var state = new TestTouchState();
			state.TouchComponentsCreate(null);
			state.TouchComponentsConfigure();

			game.ScrollStep = 10;
			game.MMBFlag = true;
			game.ShiftPressedFlag = true;
			state.TouchComponentsRefresh();
			Assert.Same(state.Btn10, state.Owner100);
			Assert.Same(state.Mmb, state.OwnerLRM);
			Assert.True(state.Shift.Pressed);
			Assert.False(state.Ctrl.Pressed);

			state.Owner100 = state.Btn100;
			state.BtnGroup100Press(null);
			Assert.Equal(100, game.ScrollStep);

			state.OwnerLRM = state.Rmb;
			state.BtnGroupLRMPress(null);
			Assert.True(game.RMBFlag);
			Assert.False(game.MMBFlag);

			state.Ctrl.Pressed = true;
			state.BtnCtrlClick(null);
			Assert.True(game.CtrlPressedFlag);
		});
	}
}
