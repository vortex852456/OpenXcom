using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Scope = "namespace", Target = "~N:Acom.Tests.Interface", Justification = "Test namespaces mirror the port areas (Acom.Tests.<Area>)")]

namespace Acom.Tests.Interface;

/// <summary>Shared helpers for widget tests.</summary>
internal static class WidgetTestHelpers
{
	/// <summary>Monospace terminal font: every character is 9x16 pixels, no spacing.</summary>
	public static Font TerminalFont()
	{
		var font = new Font();
		font.LoadTerminal();
		return font;
	}

	/// <summary>Wraps an event as an action; window equals logical screen (scale 1, no black bands).</summary>
	public static Acom.Action Act(SDL_Event ev) => new(ev, 1.0, 1.0, 0, 0);

	public static Acom.Action Key(SDLKey key, char unicode = '\0') => Act(SDL_Event.KeyDown(key, SDLMod.KMOD_NONE, unicode));

	public static Acom.Action Press(int x, int y, byte button = SDLMouse.SDL_BUTTON_LEFT) => Act(SDL_Event.MouseButtonDown(button, x, y));

	public static Acom.Action Release(int x, int y, byte button = SDLMouse.SDL_BUTTON_LEFT) => Act(SDL_Event.MouseButtonUp(button, x, y));

	public static Acom.Action Move(int x, int y) => Act(SDL_Event.MouseMotion(x, y));
}

/// <summary>State that exposes its modal surface.</summary>
internal sealed class ModalState : State
{
	public InteractiveSurface? Modal => _modal;
}
