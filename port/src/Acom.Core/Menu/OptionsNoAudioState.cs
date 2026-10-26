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
// Ported from: src/Menu/OptionsNoAudioState.h, src/Menu/OptionsNoAudioState.cpp
#nullable disable

namespace Acom;

/// <summary>Screen that tells the user there is no audio hardware.</summary>
public class OptionsNoAudioState : OptionsBaseState
{
	private readonly Text _txtError;

	/// <summary>Initializes all the elements in the Audio Options screen.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	public OptionsNoAudioState(OptionsOrigin origin) : base(origin)
	{
		SetCategory(_btnAudio);

		// Create objects
		_txtError = new Text(218, 136, 94, 8);

		Add(_txtError, "text", "audioMenu");

		CenterAllSurfaces();

		// Set up objects
		_txtError.Align = TextHAlign.ALIGN_CENTER;
		_txtError.VerticalAlign = TextVAlign.ALIGN_MIDDLE;
		_txtError.SetBig();
		_txtError.SetWordWrap(true);
		_txtError.SetText(Tr("STR_NO_AUDIO_HARDWARE_DETECTED"));
	}
}
