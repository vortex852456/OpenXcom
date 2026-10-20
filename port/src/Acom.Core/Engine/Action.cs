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
// Ported from: src/Engine/Action.h, src/Engine/Action.cpp

namespace Acom.Engine;

/// <summary>
/// Container for all the information associated with a
/// given user action, like mouse clicks, key presses, etc.
/// </summary>
/// <remarks>Called action because event is reserved.</remarks>
public sealed class Action
{
	private readonly SDL_Event _ev;
	private readonly double _scaleX, _scaleY;
	private readonly int _topBlackBand, _leftBlackBand;
	private int _mouseX = -1, _mouseY = -1, _surfaceX = -1, _surfaceY = -1;

	/// <param name="ev">The event.</param>
	/// <param name="scaleX">Window pixels per logical screen pixel (horizontal).</param>
	/// <param name="scaleY">Window pixels per logical screen pixel (vertical).</param>
	/// <param name="topBlackBand">Letterbox height in window pixels.</param>
	/// <param name="leftBlackBand">Pillarbox width in window pixels.</param>
	public Action(SDL_Event ev, double scaleX, double scaleY, int topBlackBand, int leftBlackBand)
	{
		_ev = ev;
		_scaleX = scaleX;
		_scaleY = scaleY;
		_topBlackBand = topBlackBand;
		_leftBlackBand = leftBlackBand;
	}

	public double XScale => _scaleX;
	public double YScale => _scaleY;

	/// <summary>Sets this action as a mouse action with the respective mouse properties.</summary>
	public void SetMouseAction(int mouseX, int mouseY, int surfaceX, int surfaceY)
	{
		_mouseX = mouseX - _leftBlackBand;
		_mouseY = mouseY - _topBlackBand;
		_surfaceX = surfaceX;
		_surfaceY = surfaceY;
	}

	public bool IsMouseAction => _mouseX != -1;
	public int TopBlackBand => _topBlackBand;
	public int LeftBlackBand => _leftBlackBand;

	/// <summary>Mouse X in window pixels (without black bands).</summary>
	public int XMouse => _mouseX;

	/// <summary>Mouse Y in window pixels (without black bands).</summary>
	public int YMouse => _mouseY;

	/// <summary>Mouse X in logical screen coordinates.</summary>
	public double AbsoluteXMouse => _mouseX == -1 ? -1 : _mouseX / _scaleX;

	/// <summary>Mouse Y in logical screen coordinates.</summary>
	public double AbsoluteYMouse => _mouseY == -1 ? -1 : _mouseY / _scaleY;

	/// <summary>Mouse X relative to the surface, in window pixels.</summary>
	public double RelativeXMouse => _mouseX == -1 ? -1 : _mouseX - _surfaceX * _scaleX;

	/// <summary>Mouse Y relative to the surface, in window pixels.</summary>
	public double RelativeYMouse => _mouseY == -1 ? -1 : _mouseY - _surfaceY * _scaleY;

	/// <summary>The interactive surface that triggered this action (the sender).</summary>
	public InteractiveSurface? Sender { get; set; }

	/// <summary>The details about this action.</summary>
	public SDL_Event Details => _ev;
}
