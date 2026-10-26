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
// Ported from: src/Geoscape/BuildNewBaseState.h, src/Geoscape/BuildNewBaseState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Screen that allows the player
/// to place a new base on the globe.
/// </summary>
public class BuildNewBaseState : State
{
	private readonly Base _base;
	private readonly Globe _globe;
	private readonly InteractiveSurface _btnRotateLeft, _btnRotateRight, _btnRotateUp, _btnRotateDown, _btnZoomIn, _btnZoomOut;
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly TextButton _btnCancel;
	private readonly Timer _hoverTimer;
	private readonly bool _first;
	private readonly bool _oldshowradar;
	private double _oldlat, _oldlon;
	private int _mousex, _mousey;

	/// <summary>
	/// Initializes all the elements in the Build New Base window.
	/// </summary>
	/// <param name="base">Pointer to the base to place.</param>
	/// <param name="globe">Pointer to the Geoscape globe.</param>
	/// <param name="first">Is this the first base in the game?</param>
	public BuildNewBaseState(Base @base, Globe globe, bool first)
	{
		_base = @base;
		_globe = globe;
		_first = first;
		_oldlat = 0;
		_oldlon = 0;
		_mousex = 0;
		_mousey = 0;

		int dx = _game.Screen.DX;
		int dy = _game.Screen.DY;
		_screen = false;

		_oldshowradar = Options.globeRadarLines;
		if (!_oldshowradar)
			Options.globeRadarLines = true;

		// Create objects
		_btnRotateLeft = new InteractiveSurface(12, 10, 259 + dx * 2, 176 + dy);
		_btnRotateRight = new InteractiveSurface(12, 10, 283 + dx * 2, 176 + dy);
		_btnRotateUp = new InteractiveSurface(13, 12, 271 + dx * 2, 162 + dy);
		_btnRotateDown = new InteractiveSurface(13, 12, 271 + dx * 2, 187 + dy);
		_btnZoomIn = new InteractiveSurface(23, 23, 295 + dx * 2, 156 + dy);
		_btnZoomOut = new InteractiveSurface(13, 17, 300 + dx * 2, 182 + dy);

		_window = new Window(this, 256, 28, 0, 0);
		_window.X = dx;
		_window.SetDY(0);
		_btnCancel = new TextButton(54, 12, 186 + dx, 8);
		_txtTitle = new Text(180, 16, 8 + dx, 6);

		_hoverTimer = new Timer(50);
		_hoverTimer.OnTimer((StateHandler)HoverRedraw);
		_hoverTimer.Start();

		// Set palette
		SetInterface("geoscape");

		Add(_btnRotateLeft);
		Add(_btnRotateRight);
		Add(_btnRotateUp);
		Add(_btnRotateDown);
		Add(_btnZoomIn);
		Add(_btnZoomOut);

		Add(_window, "genericWindow", "geoscape");
		Add(_btnCancel, "genericButton2", "geoscape");
		Add(_txtTitle, "genericText", "geoscape");

		// Set up objects
		_globe.OnMouseClick(GlobeClick);

		_btnRotateLeft.OnMousePress(BtnRotateLeftPress);
		_btnRotateLeft.OnMouseRelease(BtnRotateLeftRelease);
		_btnRotateLeft.OnKeyboardPress(BtnRotateLeftPress, Options.keyGeoLeft);
		_btnRotateLeft.OnKeyboardRelease(BtnRotateLeftRelease, Options.keyGeoLeft);

		_btnRotateRight.OnMousePress(BtnRotateRightPress);
		_btnRotateRight.OnMouseRelease(BtnRotateRightRelease);
		_btnRotateRight.OnKeyboardPress(BtnRotateRightPress, Options.keyGeoRight);
		_btnRotateRight.OnKeyboardRelease(BtnRotateRightRelease, Options.keyGeoRight);

		_btnRotateUp.OnMousePress(BtnRotateUpPress);
		_btnRotateUp.OnMouseRelease(BtnRotateUpRelease);
		_btnRotateUp.OnKeyboardPress(BtnRotateUpPress, Options.keyGeoUp);
		_btnRotateUp.OnKeyboardRelease(BtnRotateUpRelease, Options.keyGeoUp);

		_btnRotateDown.OnMousePress(BtnRotateDownPress);
		_btnRotateDown.OnMouseRelease(BtnRotateDownRelease);
		_btnRotateDown.OnKeyboardPress(BtnRotateDownPress, Options.keyGeoDown);
		_btnRotateDown.OnKeyboardRelease(BtnRotateDownRelease, Options.keyGeoDown);

		_btnZoomIn.OnMouseClick(BtnZoomInLeftClick, SDLMouse.SDL_BUTTON_LEFT);
		_btnZoomIn.OnMouseClick(BtnZoomInRightClick, SDLMouse.SDL_BUTTON_RIGHT);
		_btnZoomIn.OnKeyboardPress(BtnZoomInLeftClick, Options.keyGeoZoomIn);

		_btnZoomOut.OnMouseClick(BtnZoomOutLeftClick, SDLMouse.SDL_BUTTON_LEFT);
		_btnZoomOut.OnMouseClick(BtnZoomOutRightClick, SDLMouse.SDL_BUTTON_RIGHT);
		_btnZoomOut.OnKeyboardPress(BtnZoomOutLeftClick, Options.keyGeoZoomOut);

		// dirty hacks to get the rotate buttons to work in "classic" style
		_btnRotateLeft.SetListButton();
		_btnRotateRight.SetListButton();
		_btnRotateUp.SetListButton();
		_btnRotateDown.SetListButton();

		SetWindowBackground(_window, "geoscape");

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_txtTitle.SetText(Tr("STR_SELECT_SITE_FOR_NEW_BASE"));
		_txtTitle.VerticalAlign = TextVAlign.ALIGN_MIDDLE;
		_txtTitle.SetWordWrap(true);

		if (_first)
		{
			_btnCancel.Visible = false;
		}
	}

	/// <summary>
	/// C++ destructor: restores the radar lines option.
	/// </summary>
	public override void Destroy()
	{
		if (Options.globeRadarLines != _oldshowradar)
		{
			Options.globeRadarLines = false;
		}
		base.Destroy();
	}

	/// <summary>
	/// Stops the globe and adds radar hover effect.
	/// </summary>
	public override void Init()
	{
		base.Init();
		_globe.OnMouseOver(GlobeHover);
		_globe.RotateStop();
		_globe.SetNewBaseHover(true);
	}

	/// <summary>
	/// Runs the globe rotation timer.
	/// </summary>
	public override void Think()
	{
		base.Think();
		_globe.Think();
		_hoverTimer.Think(this, null);
	}

	/// <summary>
	/// Handles the globe.
	/// </summary>
	public override void Handle(Action action)
	{
		base.Handle(action);
		_globe.Handle(action, this);
	}

	/// <summary>
	/// Processes mouse-hover event for base placement.
	/// </summary>
	public void GlobeHover(Action action)
	{
		_mousex = (int)Math.Floor(action.AbsoluteXMouse);
		_mousey = (int)Math.Floor(action.AbsoluteYMouse);
		if (!_hoverTimer.IsRunning) _hoverTimer.Start();
	}

	/// <summary>
	/// Redraws the new base hover marker and radar range.
	/// </summary>
	public void HoverRedraw()
	{
		double lon = 0, lat = 0;
		_globe.CartToPolar((short)_mousex, (short)_mousey, ref lon, ref lat);
		if (!double.IsNaN(lon) && !double.IsNaN(lat))
		{
			_globe.SetNewBaseHoverPos(lon, lat);
			_globe.SetNewBaseHover(true);
		}
		if (Options.globeRadarLines && !(Target.AreSame(_oldlat, lat) && Target.AreSame(_oldlon, lon)))
		{
			_oldlat = lat;
			_oldlon = lon;
			_globe.Invalidate();
		}
	}

	/// <summary>
	/// Processes any left-clicks for base placement,
	/// or right-clicks to scroll the globe.
	/// </summary>
	public void GlobeClick(Action action)
	{
		double lon = 0, lat = 0;
		int mouseX = (int)Math.Floor(action.AbsoluteXMouse), mouseY = (int)Math.Floor(action.AbsoluteYMouse);
		_globe.CartToPolar((short)mouseX, (short)mouseY, ref lon, ref lat);

		// Ignore window clicks
		if (mouseY < 28)
		{
			return;
		}

		// Clicking on a polygon for a base location
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
		{
			if (_globe.InsideLand(lon, lat))
			{
				bool fakeUnderwaterBasesUnlocked = true;
				if (_game.Mod.FakeUnderwaterBaseUnlockResearch.Length != 0)
				{
					fakeUnderwaterBasesUnlocked = _game.SavedGame.IsResearched(_game.Mod.FakeUnderwaterBaseUnlockResearch, true);
				}
				bool fakeUnderwaterTexture = _globe.InsideFakeUnderwaterTexture(lon, lat);
				if ((_first || !fakeUnderwaterBasesUnlocked) && fakeUnderwaterTexture)
				{
					// first (starting) base can't be fake underwater base
					PushCannotBeBuilt();
				}
				else
				{
					_base.SetFakeUnderwater(fakeUnderwaterTexture);
					_base.Longitude = lon;
					_base.Latitude = lat;
					_base.CalculateServices(_game.SavedGame);
					foreach (var craft in _base.Crafts)
					{
						craft.Longitude = lon;
						craft.Latitude = lat;
					}
					if (_first)
					{
						_game.PushState(new BaseNameState(_base, _globe, _first, false));
					}
					else
					{
						_game.PushState(new ConfirmNewBaseState(_base, _globe));
					}
				}
			}
			else
			{
				PushCannotBeBuilt();
			}
		}
	}

	private void PushCannotBeBuilt()
	{
		RuleInterface geoscape = _game.Mod.GetInterface("geoscape");
		_game.PushState(new ErrorMessageState(Tr("STR_XCOM_BASE_CANNOT_BE_BUILT"), _palette, unchecked((byte)geoscape.GetElement("genericWindow").color), "BACK01.SCR", geoscape.GetElement("palette").color));
	}

	/// <summary>Starts rotating the globe to the left.</summary>
	public void BtnRotateLeftPress(Action action)
	{
		_globe.RotateLeft();
	}

	/// <summary>Stops rotating the globe to the left.</summary>
	public void BtnRotateLeftRelease(Action action)
	{
		_globe.RotateStopLon();
	}

	/// <summary>Starts rotating the globe to the right.</summary>
	public void BtnRotateRightPress(Action action)
	{
		_globe.RotateRight();
	}

	/// <summary>Stops rotating the globe to the right.</summary>
	public void BtnRotateRightRelease(Action action)
	{
		_globe.RotateStopLon();
	}

	/// <summary>Starts rotating the globe upwards.</summary>
	public void BtnRotateUpPress(Action action)
	{
		_globe.RotateUp();
	}

	/// <summary>Stops rotating the globe upwards.</summary>
	public void BtnRotateUpRelease(Action action)
	{
		_globe.RotateStopLat();
	}

	/// <summary>Starts rotating the globe downwards.</summary>
	public void BtnRotateDownPress(Action action)
	{
		_globe.RotateDown();
	}

	/// <summary>Stops rotating the globe downwards.</summary>
	public void BtnRotateDownRelease(Action action)
	{
		_globe.RotateStopLat();
	}

	/// <summary>Zooms into the globe.</summary>
	public void BtnZoomInLeftClick(Action action)
	{
		_globe.ZoomIn();
	}

	/// <summary>Zooms the globe maximum.</summary>
	public void BtnZoomInRightClick(Action action)
	{
		_globe.ZoomMax();
	}

	/// <summary>Zooms out of the globe.</summary>
	public void BtnZoomOutLeftClick(Action action)
	{
		_globe.ZoomOut();
	}

	/// <summary>Zooms the globe minimum.</summary>
	public void BtnZoomOutRightClick(Action action)
	{
		_globe.ZoomMin();
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		// C++: delete _base; (~Base deletes its crafts, then ~Target)
		foreach (var xcraft in _base.Crafts)
		{
			xcraft.Destroy();
		}
		_base.Destroy();
		_game.PopState();
	}

	/// <summary>
	/// Updates the scale.
	/// </summary>
	/// <param name="dX">delta of X;</param>
	/// <param name="dY">delta of Y;</param>
	public override void Resize(ref int dX, ref int dY)
	{
		foreach (var surface in _surfaces)
		{
			surface.X = surface.X + dX / 2;
			if (surface != _window && surface != _btnCancel && surface != _txtTitle)
			{
				surface.Y = surface.Y + dY / 2;
			}
		}
	}
}
