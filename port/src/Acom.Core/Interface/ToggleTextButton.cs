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
// Ported from: src/Interface/ToggleTextButton.h, src/Interface/ToggleTextButton.cpp
#nullable disable

using System.Runtime.CompilerServices;

namespace Acom;

/// <summary>Text button that stays pressed when clicked and is released by clicking it again.</summary>
public class ToggleTextButton : TextButton
{
	private bool _isPressed;
	private int _originalColor, _invertedColor;
	private readonly StrongBox<TextButton> _fakeGroup = new();

	public ToggleTextButton(int width, int height, int x, int y) : base(width, height, x, y)
	{
		_originalColor = -1;
		_invertedColor = -1;
		_isPressed = false;
		base.SetGroup(_fakeGroup);
	}

	/// <summary>Handle draw() in case we need to paint the button a garish color.</summary>
	public override void Draw()
	{
		if (_invertedColor > -1)
			_fakeGroup.Value = null; // nevermind, TextButton. We'll invert the surface ourselves.
		base.Draw();

		if (_invertedColor > -1 && _isPressed)
			Invert(unchecked((byte)(_invertedColor + 4)));
	}

	/// <summary>Handle mouse clicks by toggling the button state; use _fakeGroup to trick TextButton into drawing the right thing.</summary>
	public override void MousePress(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		byte button = action.Details.button.button;
		if (button == SDLMouse.SDL_BUTTON_LEFT || button == SDLMouse.SDL_BUTTON_RIGHT)
		{
			_isPressed = !_isPressed;
			_fakeGroup.Value = _isPressed ? this : null; // this is the trick that makes TextButton stick
			if (_isPressed && _invertedColor > -1)
				base.SetColor(unchecked((byte)_invertedColor));
			else
				base.SetColor(unchecked((byte)_originalColor));
		}

		InteractiveSurfaceMousePress(action, state); // skip TextButton's code as it will try to set *_group
		Draw();
	}

	/// <summary>The pressed state of the button; setting it forces a redraw.</summary>
	public bool Pressed
	{
		get => _isPressed;
		set
		{
			_isPressed = value;
			_fakeGroup.Value = _isPressed ? this : null;
			if (_isPressed && _invertedColor > -1)
				base.SetColor(unchecked((byte)_invertedColor));
			else
				base.SetColor(unchecked((byte)_originalColor));
			_redraw = true;
		}
	}

	public override void SetColor(byte color)
	{
		_originalColor = color;
		base.SetColor(color);
	}

	/// <summary>When this is set, Surface::invert() is called with the value from mid when it's time to invert the button.</summary>
	public void SetInvertColor(byte color)
	{
		_invertedColor = color;
		_fakeGroup.Value = null;
		_redraw = true;
	}
}
