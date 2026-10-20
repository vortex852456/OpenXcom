using System.Runtime.CompilerServices;

namespace Acom.Tests.Interface;

public class BattlescapeButtonTests
{
	private sealed class TestButton(int width = 10, int height = 10, int x = 0, int y = 0) : BattlescapeButton(width, height, x, y)
	{
		public bool Inverted => _inverted;
	}

	private static Acom.Action Act(SDL_Event ev) => new(ev, 1.0, 1.0, 0, 0);

	private static void Click(InteractiveSurface surface, int x, int y)
	{
		surface.Handle(Act(SDL_Event.MouseButtonDown(SDLMouse.SDL_BUTTON_LEFT, x, y)), null);
		surface.Handle(Act(SDL_Event.MouseButtonUp(SDLMouse.SDL_BUTTON_LEFT, x, y)), null);
	}

	[Fact]
	public void Group_press_moves_the_inversion_to_the_pressed_button()
	{
		var group = new StrongBox<BattlescapeButton>();
		var a = new TestButton(10, 10, 0, 0);
		var b = new TestButton(10, 10, 20, 0);
		group.Value = a;
		a.SetGroup(group);
		b.SetGroup(group);
		Assert.True(a.Inverted);
		Assert.False(b.Inverted);

		Click(b, 25, 5);
		Assert.Same(b, group.Value);
		Assert.False(a.Inverted);
		Assert.True(b.Inverted);

		// releasing does not undo a group selection
		Click(b, 25, 5);
		Assert.True(b.Inverted);
	}

	[Fact]
	public void Click_inversion_only_lasts_while_pressed_and_needs_a_handler()
	{
		var button = new TestButton();
		button.AllowClickInversion();
		button.Handle(Act(SDL_Event.MouseButtonDown(SDLMouse.SDL_BUTTON_LEFT, 1, 1)), null);
		Assert.False(button.Inverted); // no handler for the button
		button.Handle(Act(SDL_Event.MouseButtonUp(SDLMouse.SDL_BUTTON_LEFT, 1, 1)), null);

		int clicks = 0;
		button.OnMouseClick(_ => clicks++);
		button.Handle(Act(SDL_Event.MouseButtonDown(SDLMouse.SDL_BUTTON_LEFT, 1, 1)), null);
		Assert.True(button.Inverted);
		button.Handle(Act(SDL_Event.MouseButtonUp(SDLMouse.SDL_BUTTON_LEFT, 1, 1)), null);
		Assert.False(button.Inverted);
		Assert.Equal(1, clicks);
	}

	[Fact]
	public void Toggle_needs_toggle_mode_unless_already_inverted()
	{
		var button = new TestButton();
		button.Toggle(true);
		Assert.False(button.Inverted);
		button.AllowToggleInversion();
		button.Toggle(true);
		Assert.True(button.Inverted);
		button.Toggle(false);
		Assert.False(button.Inverted);

		var tftd = new TestButton { TFTDMode = true };
		tftd.Toggle(true);
		Assert.True(tftd.Inverted);
	}

	[Fact]
	public void Pressed_surface_inverts_colours_around_the_button_colour()
	{
		var button = new TestButton(4, 1);
		button.Color = 16;
		button.SetPixel(1, 0, 20);
		button.SetPixel(2, 0, 30);
		button.SetPixel(3, 0, 250);
		button.InitSurfaces();
		button.AllowToggleInversion();
		button.Toggle(true);

		var dest = new Surface(4, 1, 0, 0, 1);
		button.Blit(dest);
		// pixel + 2 * (color + 3 - pixel), wrapping like Uint8; 0 stays transparent
		Assert.Equal([0, 18, 8, unchecked((byte)(250 + 2 * (19 - 250)))], Enumerable.Range(0, 4).Select(x => dest.GetPixel(x, 0)));

		button.Toggle(false);
		dest.Clear();
		button.Blit(dest);
		Assert.Equal([0, 20, 30, 250], Enumerable.Range(0, 4).Select(x => dest.GetPixel(x, 0)));
	}

	[Fact]
	public void Tftd_pressed_surface_uses_the_colour_table()
	{
		var button = new TestButton(6, 1) { TFTDMode = true };
		byte[] pixels = [1, 7, 31, 153, 159, 5];
		for (int x = 0; x < pixels.Length; ++x)
			button.SetPixel(x, 0, pixels[x]);
		button.InitSurfaces();
		button.Toggle(true);

		var dest = new Surface(6, 1, 0, 0, 1);
		button.Blit(dest);
		Assert.Equal([2, 11, 2, 96, 97, 5], Enumerable.Range(0, 6).Select(x => dest.GetPixel(x, 0)));
	}

	[Fact]
	public void Custom_pressed_surface_and_position_follow_the_button()
	{
		var button = new TestButton(2, 1);
		var custom = new Surface(2, 1, 0, 0, 1);
		custom.SetPixel(0, 0, 42);
		button.InitSurfaces(custom);
		button.AllowToggleInversion();
		button.Toggle(true);
		button.X = 1;

		var dest = new Surface(3, 1, 0, 0, 1);
		button.Blit(dest);
		Assert.Equal([0, 42, 0], Enumerable.Range(0, 3).Select(x => dest.GetPixel(x, 0)));
	}
}
