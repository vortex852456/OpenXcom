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
// Ported from: src/Mod/RuleVideo.h, src/Mod/RuleVideo.cpp
#nullable disable

namespace Acom;

public class SlideshowHeader
{
	/// <summary>Just the extension-less filename, like "GMWIN".</summary>
	public string musicId = "";
	/// <summary>Number of seconds to show each slide.</summary>
	public int transitionSeconds;

	public SlideshowHeader()
	{
		transitionSeconds = 0;
	}
}

public class SlideshowSlide
{
	/// <summary>Relative path to slide image.</summary>
	public string imagePath = "";
	/// <summary>STR id of caption text.</summary>
	public string caption = "";
	/// <summary>Caption rect and color info.</summary>
	public int w, h, x, y, color;
	/// <summary>Caption alignment.</summary>
	public TextHAlign align;
	/// <summary>Caption vertical alignment.</summary>
	public TextVAlign valign;
	/// <summary>Number of seconds to show this slide.</summary>
	public int transitionSeconds;

	public SlideshowSlide()
	{
		w = 0;
		h = 0;
		x = 0;
		y = 0;
		color = 0;
		align = TextHAlign.ALIGN_LEFT;
		valign = TextVAlign.ALIGN_TOP;
		transitionSeconds = 0;
	}
}

public class RuleVideo
{
#pragma warning disable IDE0052 // kept like the C++ member
	private readonly string _id;
#pragma warning restore IDE0052
	private bool _useUfoAudioSequence;
	private bool _winGame, _loseGame;
	private readonly List<string> _videos = [], _audioTracks = [];
	private readonly SlideshowHeader _slideshowHeader = new();
	private readonly List<SlideshowSlide> _slides = [];

	public RuleVideo(string type)
	{
		_id = type;
		_useUfoAudioSequence = false;
		_winGame = false;
		_loseGame = false;
		// backwards-compatibility failsafe
		if (_id == "winGame")
			_winGame = true;
		if (_id == "loseGame")
			_loseGame = true;
	}

	private static void LoadSlide(SlideshowSlide slide, YamlNodeReader reader)
	{
		slide.imagePath = reader["imagePath"].ReadVal("");
		slide.caption = reader["caption"].ReadVal("");

		var size = reader["captionSize"].ReadVal((Screen.ORIGINAL_WIDTH, Screen.ORIGINAL_HEIGHT));
		slide.w = size.Item1;
		slide.h = size.Item2;

		var pos = reader["captionPos"].ReadVal((0, 0));
		slide.x = pos.Item1;
		slide.y = pos.Item2;

		slide.color = reader["captionColor"].ReadVal(int.MaxValue);
		slide.transitionSeconds = reader["transitionSeconds"].ReadVal(0);
		slide.align = reader["captionAlign"].ReadVal(TextHAlign.ALIGN_LEFT);
		slide.valign = reader["captionVerticalAlign"].ReadVal(TextVAlign.ALIGN_TOP);
	}

	public void Load(YamlNodeReader reader)
	{
		_useUfoAudioSequence = reader["useUfoAudioSequence"].ReadVal(false);
		reader.TryRead("winGame", ref _winGame);
		reader.TryRead("loseGame", ref _loseGame);

		foreach (var video in reader["videos"].Children())
			_videos.Add(video.ReadVal<string>());

		foreach (var track in reader["audioTracks"].Children())
			_audioTracks.Add(track.ReadVal<string>());

		if (reader["slideshow"] is { IsValid: true } slideshow)
		{
			_slideshowHeader.musicId = slideshow["musicId"].ReadVal("");
			_slideshowHeader.transitionSeconds = slideshow["transitionSeconds"].ReadVal(30);

			foreach (var slideReader in slideshow["slides"].Children())
			{
				var slide = new SlideshowSlide();
				LoadSlide(slide, slideReader);
				_slides.Add(slide);
			}
		}
	}

	/// <summary>
	/// Returns whether to use the UFO audio sequence. In general, this
	/// should only ever be true for the vanilla UFO intro cutscene.
	/// </summary>
	public bool UseUfoAudioSequence() => _useUfoAudioSequence;

	public List<string> Videos => _videos;

	public SlideshowHeader SlideshowHeader => _slideshowHeader;

	public List<SlideshowSlide> Slides => _slides;

	public List<string> AudioTracks => _audioTracks;

	public bool WinGame => _winGame;

	public bool LoseGame => _loseGame;
}
