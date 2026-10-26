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
// Ported from: src/Menu/CutsceneState.h, src/Menu/CutsceneState.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Shows cutscenes: inspects the relevant rules and loads the appropriate state
/// for showing slideshows or videos.
/// </summary>
public class CutsceneState : State
{
	private readonly string _cutsceneId;

	/// <summary>Creates a new CutsceneState that will play the cutscene with the given ID.</summary>
	public CutsceneState(string cutsceneId)
	{
		_cutsceneId = cutsceneId;
	}

	public override void Init()
	{
		base.Init();

		// pop self off stack and replace with actual player state
		_game.PopState();

		RuleVideo videoRule = _game.Mod.GetVideo(_cutsceneId, true);
		if (_game.SavedGame != null && _game.SavedGame.Ending != GameEnding.END_NONE)
		{
			if (_game.SavedGame.MonthsPassed > -1)
			{
				_game.SetState(new StatisticsState());
			}
			else
			{
				_game.SavedGame = null;
				_game.SetState(new GoToMainMenuState());
			}
		}

		bool fmv = false, slide = false;
		if (videoRule.Videos.Count != 0)
		{
			fmv = FileMap.FileExists(videoRule.Videos[0]);
		}
		if (videoRule.Slides.Count != 0)
		{
			slide = FileMap.FileExists(videoRule.Slides[0].imagePath);
		}

		if (fmv && (!slide || Options.preferredVideo == VideoFormat.VIDEO_FMV))
		{
			_game.PushState(new VideoState(videoRule.Videos, videoRule.AudioTracks, videoRule.UseUfoAudioSequence()));
		}
		else if (slide && (!fmv || Options.preferredVideo == VideoFormat.VIDEO_SLIDE))
		{
			_game.PushState(new SlideshowState(videoRule.SlideshowHeader, videoRule.Slides));
		}
		else
		{
			Logger.Warning("cutscene definition empty: " + _cutsceneId);
		}
	}

	/// <summary>Sets up the screen for cutscenes (letterboxed 320x200).</summary>
	/// <returns>Whether the display was letterboxed before.</returns>
	public static bool InitDisplay()
	{
		bool letterboxed = Options.keepAspectRatio;
		Options.keepAspectRatio = true;
		Options.baseXResolution = Screen.ORIGINAL_WIDTH;
		Options.baseYResolution = Screen.ORIGINAL_HEIGHT;
		_game.Screen.ResetDisplay(false);
		return letterboxed;
	}

	/// <summary>Restores the screen after a cutscene.</summary>
	public static void ResetDisplay(bool wasLetterboxed)
	{
		Options.keepAspectRatio = wasLetterboxed;
		Screen.UpdateScale(Options.geoscapeScale, ref Options.baseXGeoscape, ref Options.baseYGeoscape, true);
		_game.Screen.ResetDisplay(false);
	}
}
