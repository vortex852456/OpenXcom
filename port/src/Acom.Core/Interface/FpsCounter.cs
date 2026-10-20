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
// Ported from: src/Interface/FpsCounter.h, src/Interface/FpsCounter.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Counts the amount of frames each second
/// and displays them in a NumberText surface.
/// </summary>
public class FpsCounter : Surface
{
	private readonly NumberText _text;
	private readonly Timer _timer;
	private int _frames;

	/// <summary>Creates a FPS counter of the specified size.</summary>
	/// <param name="width">Width in pixels.</param>
	/// <param name="height">Height in pixels.</param>
	/// <param name="x">X position in pixels.</param>
	/// <param name="y">Y position in pixels.</param>
	public FpsCounter(int width, int height, int x, int y) : base(width, height, x, y)
	{
		_visible = Options.fpsCounter;

		_timer = new Timer(1000);
		_timer.OnTimer((SurfaceHandler)Update);
		_timer.Start();

		_text = new NumberText(width, height, x, y);
	}

	/// <summary>Replaces a certain amount of colors in the FPS counter palette.</summary>
	public override void SetPalette(ReadOnlySpan<Rgba> colors, int firstcolor = 0, int ncolors = 256)
	{
		base.SetPalette(colors, firstcolor, ncolors);
		_text.SetPalette(colors, firstcolor, ncolors);
	}

	/// <summary>Sets the text color of the counter.</summary>
	public override void SetColor(byte color) => _text.SetColor(color);

	/// <summary>Shows / hides the FPS counter.</summary>
	/// <param name="action">Pointer to an action.</param>
	public void Handle(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		var ev = action.Details;
		if (ev.type == SDLEventType.SDL_KEYDOWN && ev.key.keysym.sym == Options.keyFps)
		{
			_visible = !_visible;
			Options.fpsCounter = _visible;
		}
	}

	/// <summary>Advances frame counter.</summary>
	public override void Think() => _timer.Think(null, this);

	/// <summary>Updates the amount of Frames per Second.</summary>
	public void Update()
	{
		int fps = (int)Math.Floor((double)_frames / _timer.Time * 1000);
		_text.Value = unchecked((uint)fps);
		_frames = 0;
		_redraw = true;
	}

	/// <summary>Draws the FPS counter.</summary>
	public override void Draw()
	{
		base.Draw();
		_text.Blit(this);
	}

	public void AddFrame() => _frames++;
}
