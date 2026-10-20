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
// Ported from: src/Interface/BattlescapeButton.h, src/Interface/BattlescapeButton.cpp
#nullable disable

using System.Runtime.CompilerServices;

namespace Acom;

public enum InversionType : uint
{
	INVERT_NONE = 0,
	INVERT_CLICK = 1,
	INVERT_TOGGLE = 2,
}

/// <summary>
/// Regular image that works like a button.
/// Unlike the TextButton, this button doesn't draw
/// anything on its own. It takes an existing graphic and
/// treats it as a button, inverting colors when necessary.
/// This is necessary for special buttons like in the Geoscape.
/// </summary>
public class BattlescapeButton : InteractiveSurface
{
	protected byte _color;
	/// <summary>The pressed button of the group (C++ <c>BattlescapeButton **</c>), shared by all buttons of the group.</summary>
	protected StrongBox<BattlescapeButton> _group;
	protected bool _inverted;
	protected InversionType _toggleMode;
	protected Surface _altSurface;

	/// <summary>Creates a new image button with the specified size and position.</summary>
	public BattlescapeButton(int width, int height, int x = 0, int y = 0) : base(width, height, x, y)
	{
	}

	/// <summary>Changes the color for the battlescape button.</summary>
	public override void SetColor(byte color) => _color = color;

	/// <summary>The color for the battlescape button.</summary>
	public byte Color { get => _color; set => SetColor(value); }

	/// <summary>
	/// Changes the button group this battlescape button belongs to.
	/// </summary>
	/// <param name="group">Holder of the pressed button of the group. Null makes it a regular button.</param>
	public void SetGroup(StrongBox<BattlescapeButton> group)
	{
		_group = group;
		if (_group != null && _group.Value == this)
			_inverted = true;
	}

	/// <summary>
	/// Sets the button as the pressed button if it's part of a group,
	/// and inverts the colors when pressed.
	/// </summary>
	public override void MousePress(Action action, State state)
	{
		if (_group != null)
		{
			if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
			{
				_group.Value.Toggle(false);
				_group.Value = this;
				_inverted = true;
			}
		}
		else if ((_tftdMode || _toggleMode == InversionType.INVERT_CLICK) && !_inverted && IsButtonPressed() && IsButtonHandled(action.Details.button.button))
		{
			_inverted = true;
		}
		base.MousePress(action, state);
	}

	/// <summary>Sets the button as the released button if it's part of a group.</summary>
	public override void MouseRelease(Action action, State state)
	{
		if (_inverted && (_tftdMode || _toggleMode == InversionType.INVERT_CLICK) && IsButtonHandled(action.Details.button.button))
		{
			_inverted = false;
		}
		base.MouseRelease(action, state);
	}

	/// <summary>Invert a button explicitly either ON or OFF and keep track of the state using our internal variables.</summary>
	/// <param name="invert">Set this button as pressed.</param>
	public void Toggle(bool invert)
	{
		if (_tftdMode || _toggleMode == InversionType.INVERT_TOGGLE || _inverted)
		{
			_inverted = invert;
		}
	}

	/// <summary>Toggle inversion mode: click to press, click to unpress.</summary>
	public void AllowToggleInversion() => _toggleMode = InversionType.INVERT_TOGGLE;

	/// <summary>Click inversion mode: click to press, release to unpress.</summary>
	public void AllowClickInversion() => _toggleMode = InversionType.INVERT_CLICK;

	/// <summary>
	/// Initializes the alternate surface for swapping out as needed.
	/// Performs a colour swap for TFTD style buttons, and a palette inversion for coloured buttons.
	/// We use two separate surfaces because it's far easier to keep track of
	/// whether or not this surface is inverted.
	/// </summary>
	/// <remarks>HD: the alternate surface keeps this surface's pixel density and is converted per buffer pixel.</remarks>
	public void InitSurfaces(Surface custom = null)
	{
		_altSurface = new Surface(Width, Height, _x, _y, Scale);
		_altSurface.SetPalette(Palette);

		if (custom != null)
		{
			custom.BlitNShade(_altSurface, 0, 0);
			return;
		}

		ReadOnlySpan<byte> src = Pixels;
		Span<byte> dest = _altSurface.Pixels;

		// tftd mode: use a colour lookup table instead of simple palette inversion for our "pressed" state
		if (_tftdMode)
		{
			// this is our colour lookup table
			ReadOnlySpan<int> colorFrom = [1, 2, 3, 4, 7, 8, 31, 47, 153, 156, 159];
			ReadOnlySpan<int> colorTo = [2, 3, 4, 5, 11, 10, 2, 2, 96, 9, 97];

			for (int p = 0; p < src.Length; ++p)
			{
				byte pixel = src[p];
				for (int i = 0; i != colorFrom.Length; ++i)
				{
					if (pixel == colorFrom[i])
					{
						pixel = (byte)colorTo[i];
						break;
					}
				}
				dest[p] = pixel;
			}
		}
		else
		{
			for (int p = 0; p < src.Length; ++p)
			{
				byte pixel = src[p];
				if (pixel > 0)
				{
					dest[p] = unchecked((byte)(pixel + 2 * (_color + 3 - pixel)));
				}
				else
				{
					dest[p] = 0;
				}
			}
		}
	}

	/// <summary>
	/// Blits this surface or the alternate surface onto another one,
	/// depending on whether the button is "pressed" or not.
	/// </summary>
	public override void Blit(Surface surface)
	{
		if (_inverted)
		{
			_altSurface.Blit(surface);
		}
		else
		{
			base.Blit(surface);
		}
	}

	/// <summary>Alters both versions of the button's X pos.</summary>
	public override int X
	{
		get => base.X;
		set
		{
			base.X = value;
			if (_altSurface != null)
				_altSurface.X = value;
		}
	}

	/// <summary>Alters both versions of the button's Y pos.</summary>
	public override int Y
	{
		get => base.Y;
		set
		{
			base.Y = value;
			if (_altSurface != null)
				_altSurface.Y = value;
		}
	}
}
