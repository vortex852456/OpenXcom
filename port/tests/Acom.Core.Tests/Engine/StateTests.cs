namespace Acom.Tests.Engine;

// Uses the static game pointer of State.
[Collection(nameof(GameTests))]
public class StateTests
{
	private sealed class TestState : State
	{
		public byte CursorColor => _cursorColor;
	}

	[Fact]
	public void Tr_forwards_to_the_game_language()
	{
		var game = new Game(new Screen(4, 4));
		game.Language.LoadStrings(new Dictionary<string, string>
		{
			["STR_A"] = "A",
			["STR_A_2"] = "A2",
			["STR_N_zero"] = "none",
			["STR_N_other"] = "{N} items",
			["STR_G_FEMALE"] = "she",
		});
		var state = new TestState();
		Assert.Equal("A", state.Tr("STR_A").ToString());
		Assert.Equal("A", state.TrAlt("STR_A", 0).ToString());
		Assert.Equal("A2", state.TrAlt("STR_A", 2).ToString());
		Assert.Equal("none", state.Tr("STR_N", 0).ToString());
		Assert.Equal("7 items", state.Tr("STR_N", 7).ToString());
		Assert.Equal("7 items", state.Tr("STR_N", 7u).ToString());
		Assert.Equal("she", state.Tr("STR_G", SoldierGender.GENDER_FEMALE).ToString());
	}

	[Fact]
	public void Center_lower_and_resize_move_all_surfaces()
	{
		_ = new Game(new Screen(400, 250)); // 40 / 25 pixels around the 320x200 layout
		var state = new TestState();
		var surface = new Surface(10, 10, 10, 20);
		state.Add(surface);
		state.CenterAllSurfaces();
		Assert.Equal((50, 45), (surface.X, surface.Y));
		state.LowerAllSurfaces();
		Assert.Equal((50, 57), (surface.X, surface.Y));
		int dX = 10, dY = -7;
		state.Resize(ref dX, ref dY);
		Assert.Equal((55, 54), (surface.X, surface.Y));
	}

	[Fact]
	public void Custom_palette_reaches_surfaces_and_screen()
	{
		var game = new Game(new Screen(4, 4));
		var state = new TestState();
		var colors = Enumerable.Range(0, 256).Select(i => new Rgba((byte)i, 0, 0)).ToArray();
		state.SetCustomPalette(colors, 300);
		Assert.Equal(44, state.CursorColor); // Uint8 conversion
		Assert.Equal(colors, state.Palette.ToArray());
		// surfaces get a copy of the palette when they are added
		var surface = new Surface(1, 1);
		state.Add(surface);
		Assert.Equal(colors, surface.Palette.ToArray());
		game.PushState(state);
		game.RunFrame();
		Assert.Equal(colors, game.Screen.Palette.ToArray());
	}
}
