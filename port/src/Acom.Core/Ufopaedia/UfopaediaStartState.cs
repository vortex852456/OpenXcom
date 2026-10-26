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
// Ported from: src/Ufopaedia/UfopaediaStartState.h, src/Ufopaedia/UfopaediaStartState.cpp
#nullable disable

namespace Acom;

/// <summary>
/// UfopaediaStartState is the screen that opens when clicking Ufopaedia button in Geoscape.
/// Presents buttons to all sections of Ufopaedia, opening a UfopaediaSelectState on click.
/// </summary>
public class UfopaediaStartState : State
{
	protected Window _window;
	protected Text _txtTitle;
	protected TextButton _btnOk;
	protected List<TextButton> _btnSections = [];
	protected ArrowButton _btnScrollUp;
	protected ArrowButton _btnScrollDown;
	protected Timer _timerScroll;
	protected int _offset;
	protected int _scroll;
	protected int _maxButtons;
	protected int _heightOffset;
	protected int _windowOffset;
	protected List<string> _cats;

	public UfopaediaStartState()
	{
		_offset = 0;
		_scroll = 0;
		_maxButtons = 0;
		_heightOffset = 0;
		_windowOffset = 0;
		_cats = new List<string>(_game.Mod.UfopaediaCategoryList);

		const int MAX_VANILLA_BUTTONS = 10;
		const int SPACE_PER_BUTTON = 13;
		const int HALF_SPACE_PER_BUTTON = 7;

		_screen = false;

		int extraSpace = (_game.Screen.DY * 2) + 20; // upper extra + lower extra + 20 pixels from the original
		int maxButtons = (extraSpace / SPACE_PER_BUTTON) + MAX_VANILLA_BUTTONS;
		_maxButtons = (int)Math.Min((uint)_cats.Count, (uint)maxButtons); // size_t comparison
		maxButtons = _maxButtons;
		int buttonOffset = 0;
		if (maxButtons >= MAX_VANILLA_BUTTONS)
		{
			_heightOffset = (maxButtons - MAX_VANILLA_BUTTONS) * SPACE_PER_BUTTON;
			_windowOffset = ((maxButtons - MAX_VANILLA_BUTTONS) / 2 * SPACE_PER_BUTTON) + ((maxButtons - MAX_VANILLA_BUTTONS) % 2 * HALF_SPACE_PER_BUTTON);
			buttonOffset = _windowOffset + SPACE_PER_BUTTON;
		}

		// set background window
		_window = new Window(this, 256, 180 + _heightOffset, 32, 10 - _windowOffset, WindowPopup.POPUP_BOTH);
		_window.SetInnerColor(239); // almost black = darkest index from backpals.dat

		// set title
		_txtTitle = new Text(220, 17, 50, 33);

		// Set palette
		SetInterface("ufopaedia");

		Add(_window, "window", "ufopaedia");
		Add(_txtTitle, "text", "ufopaedia");

		// set buttons
		int y = 50 - buttonOffset;
		_btnScrollUp = new ArrowButton(ArrowShape.ARROW_BIG_UP, 13, 14, 270, y);
		Add(_btnScrollUp, "button1", "ufopaedia");
		for (int i = 0; i < _maxButtons; ++i)
		{
			TextButton button = new TextButton(220, 12, 50, y);
			y += 13;

			Add(button, "button1", "ufopaedia");

			button.OnMouseClick(BtnSectionClick);
			button.OnMousePress(BtnScrollUpClick, SDLMouse.SDL_BUTTON_WHEELUP);
			button.OnMousePress(BtnScrollDownClick, SDLMouse.SDL_BUTTON_WHEELDOWN);

			_btnSections.Add(button);
		}
		_btnOk = new TextButton(220, 12, 50, y);
		Add(_btnOk, "button1", "ufopaedia");
		_btnScrollDown = new ArrowButton(ArrowShape.ARROW_BIG_DOWN, 13, 14, 270, y - 15);
		Add(_btnScrollDown, "button1", "ufopaedia");

		UpdateButtons();
		if (_btnSections.Count != 0)
		{
			int titleY = _btnSections[0].Y - _txtTitle.Height;
			if (titleY < _window.Y) titleY = _window.Y;
			_txtTitle.Y = titleY;
		}

		CenterAllSurfaces();

		SetWindowBackground(_window, "ufopaedia");

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_UFOPAEDIA"));

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyGeoUfopedia);

		_btnScrollUp.Visible = _cats.Count > _maxButtons;
		_btnScrollUp.OnMousePress(BtnScrollUpPress);
		_btnScrollUp.OnMouseRelease(BtnScrollRelease);

		_btnScrollDown.Visible = _cats.Count > _maxButtons;
		_btnScrollDown.OnMousePress(BtnScrollDownPress);
		_btnScrollDown.OnMouseRelease(BtnScrollRelease);

		_timerScroll = new Timer(50);
		_timerScroll.OnTimer((StateHandler)Scroll);
	}

	/// <summary>Run timers.</summary>
	public override void Think()
	{
		base.Think();
		_timerScroll.Think(this, null);
	}

	/// <summary>Displays the list of articles for this section.</summary>
	protected void BtnSectionClick(Action action)
	{
		for (int i = 0; i < _btnSections.Count; ++i)
		{
			if (action.Sender == _btnSections[i])
			{
				_game.PushState(new UfopaediaSelectState(_cats[_offset + i], _heightOffset, _windowOffset));
				break;
			}
		}
	}

#pragma warning disable CA1822 // instance handlers in C++ (State._game is static here)
	/// <summary>Returns to the previous screen.</summary>
	protected void BtnOkClick(Action action)
	{
		_game.PopState();
	}
#pragma warning restore CA1822

	/// <summary>Starts scrolling the section buttons up.</summary>
	protected void BtnScrollUpPress(Action action)
	{
		_scroll = -1;
		_timerScroll.Start();
	}

	/// <summary>Scrolls the section buttons up.</summary>
	protected void BtnScrollUpClick(Action action)
	{
		_scroll = -1;
		Scroll();
	}

	/// <summary>Starts scrolling the section buttons down.</summary>
	protected void BtnScrollDownPress(Action action)
	{
		_scroll = 1;
		_timerScroll.Start();
	}

	/// <summary>Scrolls the section buttons down.</summary>
	protected void BtnScrollDownClick(Action action)
	{
		_scroll = 1;
		Scroll();
	}

	/// <summary>Stops scrolling the section buttons.</summary>
	protected void BtnScrollRelease(Action action)
	{
		_timerScroll.Stop();
	}

	/// <summary>Offsets the list of section buttons.</summary>
	protected void Scroll()
	{
		if (_cats.Count > _maxButtons)
		{
			_offset = Math.Clamp(_offset + _scroll, 0, _cats.Count - _maxButtons);
			UpdateButtons();
		}
	}

	/// <summary>Updates the section button labels based on scroll.</summary>
	protected void UpdateButtons()
	{
		for (int i = 0; i < _btnSections.Count; ++i)
		{
			_btnSections[i].Text = Tr(_cats[_offset + i]);
		}
	}
}
