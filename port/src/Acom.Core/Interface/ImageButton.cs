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
// Ported from: src/Interface/ImageButton.h, src/Interface/ImageButton.cpp
#nullable disable

using System.Runtime.CompilerServices;

namespace Acom;

/// <summary>
/// Regular image that works like a button.
/// Unlike the TextButton, this button doesn't draw
/// anything on its own. It takes an existing graphic and
/// treats it as a button, inverting colors when necessary.
/// This is necessary for special buttons like in the Geoscape.
/// </summary>
/// <remarks>A button group is a shared variable holding the pressed button (C++ <c>ImageButton **</c>), see <see cref="TextButton"/>.</remarks>
public class ImageButton : InteractiveSurface
{
	protected byte _color;
	protected StrongBox<ImageButton> _group;
	protected bool _inverted;

	/// <summary>Sets up an image button with the specified size and position.</summary>
	public ImageButton(int width, int height, int x = 0, int y = 0) : base(width, height, x, y)
	{
	}

	/// <summary>Changes the color for the image button.</summary>
	public override void SetColor(byte color) => _color = color;

	/// <summary>Returns the color for the image button.</summary>
	public byte Color => _color;

	/// <summary>Changes the button group this image button belongs to. Null makes it a regular button.</summary>
	/// <param name="group">Shared variable holding the pressed button of the group.</param>
	public void SetGroup(StrongBox<ImageButton> group)
	{
		_group = group;
		if (_group != null && _group.Value == this)
			Invert(unchecked((byte)(_color + 3)));
	}

	/// <summary>Sets the button as the pressed button if it's part of a group, and inverts the colors when pressed.</summary>
	public override void MousePress(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (_group != null)
		{
			if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
			{
				_group.Value.Invert(unchecked((byte)(_group.Value.Color + 3)));
				_group.Value = this;
				Invert(unchecked((byte)(_color + 3)));
			}
		}
		else if (!_inverted && IsButtonPressed() && IsButtonHandled(action.Details.button.button))
		{
			_inverted = true;
			Invert(unchecked((byte)(_color + 3)));
		}
		base.MousePress(action, state);
	}

	/// <summary>Sets the button as the released button if it's part of a group.</summary>
	public override void MouseRelease(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (_inverted && IsButtonHandled(action.Details.button.button))
		{
			_inverted = false;
			Invert(unchecked((byte)(_color + 3)));
		}
		base.MouseRelease(action, state);
	}

	/// <summary>Invert a button explicitly either ON or OFF and keep track of the state using our internal variables.</summary>
	/// <param name="invert">Set the button as pressed.</param>
	public void Toggle(bool invert)
	{
		if (_inverted != invert)
		{
			_inverted = !_inverted;
			Invert(unchecked((byte)(_color + 3)));
		}
	}
}
