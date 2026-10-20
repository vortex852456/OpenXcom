using Bulwark.Engine;

namespace Bulwark.Tests.Engine;

public class InteractiveSurfaceTests
{
	// Window equals logical screen: scale 1, no black bands.
	private static Bulwark.Engine.Action Act(SDL_Event ev) => new(ev, 1.0, 1.0, 0, 0);

	[Fact]
	public void Click_requires_press_and_release_over_surface()
	{
		var button = new InteractiveSurface(10, 10, 5, 5);
		var log = new List<string>();
		button.OnMousePress(_ => log.Add("press"));
		button.OnMouseRelease(_ => log.Add("release"));
		button.OnMouseClick(_ => log.Add("click"));
		button.OnMouseIn(_ => log.Add("in"));
		button.OnMouseOut(_ => log.Add("out"));

		button.Handle(Act(SDL_Event.MouseButtonDown(SDLMouse.SDL_BUTTON_LEFT, 6, 6)), null);
		button.Handle(Act(SDL_Event.MouseButtonUp(SDLMouse.SDL_BUTTON_LEFT, 7, 7)), null);
		button.Handle(Act(SDL_Event.MouseMotion(50, 50)), null);
		Assert.Equal(["in", "press", "release", "click", "out"], log);
	}

	[Fact]
	public void Release_outside_does_not_click()
	{
		var button = new InteractiveSurface(10, 10);
		int clicks = 0;
		button.OnMouseClick(_ => clicks++);
		button.Handle(Act(SDL_Event.MouseButtonDown(SDLMouse.SDL_BUTTON_LEFT, 1, 1)), null);
		button.Handle(Act(SDL_Event.MouseMotion(30, 30)), null);
		button.Handle(Act(SDL_Event.MouseButtonUp(SDLMouse.SDL_BUTTON_LEFT, 30, 30)), null);
		Assert.Equal(0, clicks);
	}

	[Fact]
	public void Key_handler_ignores_modifiers_but_any_key_handler_does_not()
	{
		var surface = new InteractiveSurface(1, 1);
		int ok = 0, any = 0;
		surface.OnKeyboardPress(_ => ok++, SDLKey.SDLK_RETURN);
		surface.OnKeyboardPress(_ => any++);
		surface.Handle(Act(SDL_Event.KeyDown(SDLKey.SDLK_RETURN)), null);
		surface.Handle(Act(SDL_Event.KeyDown(SDLKey.SDLK_RETURN, SDLMod.KMOD_LCTRL)), null);
		Assert.Equal(1, ok);
		Assert.Equal(2, any);
	}

	[Fact]
	public void Hiding_a_pressed_surface_unpresses_it()
	{
		var button = new InteractiveSurface(10, 10);
		int releases = 0;
		button.OnMouseRelease(_ => releases++);
		button.Handle(Act(SDL_Event.MouseButtonDown(SDLMouse.SDL_BUTTON_LEFT, 1, 1)), null);
		button.Visible = false;
		Assert.Equal(1, releases);
	}

	[Fact]
	public void Mouse_coordinates_respect_scale_and_black_bands()
	{
		var button = new InteractiveSurface(10, 10, 100, 50);
		int clicks = 0;
		button.OnMouseClick(_ => clicks++);
		// 4x window scale, 20px pillarbox: logical (105, 55) = window (20 + 420, 220).
		var down = new Bulwark.Engine.Action(SDL_Event.MouseButtonDown(SDLMouse.SDL_BUTTON_LEFT, 440, 220), 4.0, 4.0, 0, 20);
		var up = new Bulwark.Engine.Action(SDL_Event.MouseButtonUp(SDLMouse.SDL_BUTTON_LEFT, 440, 220), 4.0, 4.0, 0, 20);
		button.Handle(down, null);
		button.Handle(up, null);
		Assert.Equal(1, clicks);
	}
}
