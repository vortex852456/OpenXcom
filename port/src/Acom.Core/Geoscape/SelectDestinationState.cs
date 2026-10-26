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
// Ported from: src/Geoscape/SelectDestinationState.h, src/Geoscape/SelectDestinationState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Screen that allows the player
/// to pick a target for a craft on the globe.
/// </summary>
public class SelectDestinationState : State
{
	private readonly List<Craft> _crafts;
	private readonly Globe _globe;
	private readonly InteractiveSurface _btnRotateLeft, _btnRotateRight, _btnRotateUp, _btnRotateDown, _btnZoomIn, _btnZoomOut;
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly TextButton _btnCancel, _btnCydonia;

	/// <summary>
	/// Initializes all the elements in the Select Destination window.
	/// </summary>
	/// <param name="crafts">Pointers to the crafts to target.</param>
	/// <param name="globe">Pointer to the Geoscape globe.</param>
	public SelectDestinationState(List<Craft> crafts, Globe globe)
	{
		_crafts = [.. crafts]; // passed by value in C++
		_globe = globe;

		int dx = _game.Screen.DX;
		int dy = _game.Screen.DY;
		_screen = false;

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
		_btnCancel = new TextButton(60, 12, 110 + dx, 8);
		_btnCydonia = new TextButton(60, 12, 180 + dx, 8);
		_txtTitle = new Text(100, 16, 10 + dx, 6);

		// Set palette
		SetInterface("geoscape");

		Add(_btnRotateLeft);
		Add(_btnRotateRight);
		Add(_btnRotateUp);
		Add(_btnRotateDown);
		Add(_btnZoomIn);
		Add(_btnZoomOut);

		Add(_window, "genericWindow", "geoscape");
		Add(_btnCancel, "genericButton1", "geoscape");
		Add(_btnCydonia, "genericButton1", "geoscape");
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

		_txtTitle.SetText(Tr("STR_SELECT_DESTINATION"));
		_txtTitle.VerticalAlign = TextVAlign.ALIGN_MIDDLE;
		_txtTitle.SetWordWrap(true);

		if (_crafts.Count != 1 ||
			_crafts[0].FuelPercentage < 100 ||
			!_crafts[0].Rules.Spacecraft ||
			(_game.Mod.FinalResearch != null && // if not Research specified then we look only on `getSpacecraft`
				!_game.SavedGame.IsResearched(_game.Mod.FinalResearch)))
		{
			_btnCydonia.Visible = false;
		}
		else
		{
			_btnCydonia.Text = Tr("STR_CYDONIA");
			_btnCydonia.OnMouseClick(BtnCydoniaClick);

			// one more check...
			foreach (var depl in _game.Mod.DeploymentsList)
			{
				AlienDeployment deploymentRule = _game.Mod.GetDeployment(depl);
				if (deploymentRule.IsFinalDestination)
				{
					RuleStartingCondition sc = _game.Mod.GetStartingCondition(deploymentRule.StartingCondition);
					if (sc != null && sc.RequiresCommanderOnboard() && !_crafts[0].IsCommanderOnboard)
					{
						_btnCydonia.Visible = false;
					}
					break;
				}
			}
			// ok, ok, one more...
			if (_crafts[0].AreBannedArmorsOnboard())
			{
				_btnCydonia.Visible = false;
			}
			if (!_crafts[0].ArePilotsOnboard(_game.Mod))
			{
				_btnCydonia.Visible = false;
			}
		}

		if (_crafts[0].Status != "STR_OUT")
		{
			_globe.SetCraftRange(_crafts[0].Longitude, _crafts[0].Latitude, _crafts[0].BaseRange);
			_globe.Invalidate();
		}
	}

	/// <summary>
	/// C++ destructor: clears the craft range on the globe.
	/// </summary>
	public override void Destroy()
	{
		_globe.SetCraftRange(0.0, 0.0, 0.0);
		base.Destroy();
	}

	/// <summary>
	/// Stop the globe movement.
	/// </summary>
	public override void Init()
	{
		base.Init();
		_globe.RotateStop();
	}

	/// <summary>
	/// Runs the globe rotation timer.
	/// </summary>
	public override void Think()
	{
		base.Think();
		_globe.Think();
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
	/// Processes any left-clicks for picking a target,
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

		// Clicking on a valid target
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
		{
			List<Target> v = _globe.GetTargets(mouseX, mouseY, true, _crafts[0]);
			if (v.Count == 0)
			{
				Waypoint w = new Waypoint();
				w.Longitude = lon;
				w.Latitude = lat;
				v.Add(w);
			}
			_game.PushState(new MultipleTargetsState(v, _crafts, null, false));
		}
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
		_game.PopState();
	}

	/// <summary>
	/// Starts the final mission (after confirmation).
	/// </summary>
	public void BtnCydoniaClick(Action action)
	{
		if (_crafts[0].NumTotalUnits > 0)
		{
			_game.PushState(new ConfirmCydoniaState(_crafts[0]));
		}
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
			if (surface != _window && surface != _btnCancel && surface != _txtTitle && surface != _btnCydonia)
			{
				surface.Y = surface.Y + dY / 2;
			}
		}
	}
}
