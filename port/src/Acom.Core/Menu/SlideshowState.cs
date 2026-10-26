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
// Ported from: src/Menu/SlideshowState.h, src/Menu/SlideshowState.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Shows a slideshow (images with captions, timed or advanced by clicking).
/// </summary>
public class SlideshowState : State
{
	private readonly SlideshowHeader _slideshowHeader;
	private readonly List<SlideshowSlide> _slideshowSlides;
	private readonly bool _wasLetterboxed;
	private readonly List<InteractiveSurface> _slides = [];
	private readonly List<Text> _captions = [];
	private int _curScreen;
	private readonly Timer _transitionTimer;

	public SlideshowState(SlideshowHeader slideshowHeader, List<SlideshowSlide> slideshowRule)
	{
		ArgumentNullException.ThrowIfNull(slideshowHeader);
		ArgumentNullException.ThrowIfNull(slideshowRule);
		_slideshowHeader = slideshowHeader;
		_slideshowSlides = slideshowRule;
		_curScreen = -1;
		_wasLetterboxed = CutsceneState.InitDisplay();

		// pre-render and queue up all the frames
		foreach (var def in _slideshowSlides)
		{
			InteractiveSurface slide = new InteractiveSurface(Screen.ORIGINAL_WIDTH, Screen.ORIGINAL_HEIGHT, 0, 0);
			slide.LoadImage(def.imagePath);
			slide.OnMouseClick(ScreenClick);
			slide.OnKeyboardPress(ScreenClick, Options.keyOk);
			slide.OnKeyboardPress(ScreenSkip, Options.keyCancel);
			slide.Visible = false;
			_slides.Add(slide);
			SetStatePalette(slide.Palette);
			Add(slide);

			// initialize with default rect; may get overridden by
			// category/id definition
			Text caption = new Text(def.w, def.h, def.x, def.y);
			caption.SetColor((byte)def.color);
			caption.SetText(Tr(def.caption));
			caption.Align = def.align;
			caption.VerticalAlign = def.valign;
			caption.SetWordWrap(true);
			caption.Visible = false;
			_captions.Add(caption);
			Add(caption);
		}

		CenterAllSurfaces();

		int transitionSeconds = _slideshowHeader.transitionSeconds;
		if (_slideshowSlides[0].transitionSeconds > 0)
			transitionSeconds = _slideshowSlides[0].transitionSeconds;
		_transitionTimer = new Timer((uint)(transitionSeconds * 1000));
		_transitionTimer.OnTimer((StateHandler)ScreenTimer);

		_game.Mod.PlayMusic(_slideshowHeader.musicId);
		_game.Cursor.Visible = false;
		ScreenClick(null);
	}

	/// <summary>
	/// Shows the next screen on a timed basis.
	/// </summary>
	public void ScreenTimer()
	{
		ScreenClick(null);
	}

	/// <summary>
	/// Handle timers.
	/// </summary>
	public override void Think()
	{
		_transitionTimer.Think(this, null);
	}

	/// <summary>
	/// Shows the next screen in the slideshow; pops the state when there are no more slides
	/// </summary>
	public void ScreenClick(Action action)
	{
		if (_curScreen >= 0)
		{
			_slides[_curScreen].Visible = false;
			_captions[_curScreen].Visible = false;
		}

		++_curScreen;

		// next screen
		if (_curScreen < _slideshowSlides.Count)
		{
			int transitionSeconds = _slideshowHeader.transitionSeconds;
			if (_slideshowSlides[_curScreen].transitionSeconds > 0)
				transitionSeconds = _slideshowSlides[_curScreen].transitionSeconds;
			_transitionTimer.SetInterval((uint)(transitionSeconds * 1000));
			_transitionTimer.Start();
			SetStatePalette(_slides[_curScreen].Palette);
			_slides[_curScreen].Visible = true;
			_captions[_curScreen].Visible = true;
			Init();
		}
		else
		{
			ScreenSkip(action);
		}
	}

	/// <summary>
	/// Skips the slideshow
	/// </summary>
	public void ScreenSkip(Action action)
	{
		// slideshow is over.  restore the screen scale and pop the state
		_game.Cursor.Visible = true;
		CutsceneState.ResetDisplay(_wasLetterboxed);
		_game.PopState();
	}
}
