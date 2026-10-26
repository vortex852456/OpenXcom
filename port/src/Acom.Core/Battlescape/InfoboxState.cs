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
// Ported from: src/Battlescape/InfoboxState.h, src/Battlescape/InfoboxState.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Frame that briefly shows some info like : Yasuaki Okamoto Has Panicked. It disappears after 2 seconds.
/// </summary>
public class InfoboxState : State
{
	public const int INFOBOX_DELAY = 2000;

	private readonly Text _text;
	private readonly Frame _frame;
	private readonly Timer _timer;

	/// <summary>
	/// Initializes all the elements.
	/// </summary>
	/// <param name="msg">Message string.</param>
	public InfoboxState(string msg)
	{
		_screen = false;

		// Create objects
		_frame = new Frame(261, 122, 34, 10);
		_text = new Text(251, 112, 39, 15);

		// Set palette
		_game.SavedGame.SavedBattle.SetPaletteByDepth(this);

		Add(_frame, "infoBox", "battlescape");
		Add(_text, "infoBoxText", "battlescape");

		CenterAllSurfaces();

		_frame.SetHighContrast(true);
		_frame.SetThickness(9);

		_text.Align = TextHAlign.ALIGN_CENTER;
		_text.VerticalAlign = TextVAlign.ALIGN_MIDDLE;
		_text.SetBig();
		_text.SetWordWrap(true);
		_text.SetText(msg);
		_text.SetHighContrast(true);

		int delay = INFOBOX_DELAY;
		if (string.IsNullOrEmpty(msg))
		{
			delay = 500;
			_frame.Visible = false;
			_text.Visible = false;
		}

		_timer = new Timer((uint)delay);
		_timer.OnTimer((StateHandler)Close);
		_timer.Start();
	}

	/// <summary>Closes the window.</summary>
	public override void Handle(Action action)
	{
		base.Handle(action);

		if (action.Details.type == SDLEventType.SDL_KEYDOWN || action.Details.type == SDLEventType.SDL_MOUSEBUTTONDOWN)
		{
			Close();
		}
	}

	/// <summary>Keeps the animation timers running.</summary>
	public override void Think()
	{
		_timer.Think(this, null);
	}

	/// <summary>Closes the window.</summary>
#pragma warning disable CA1822 // instance handler as in C++
	public void Close()
#pragma warning restore CA1822
	{
		_game.PopState();
	}
}
