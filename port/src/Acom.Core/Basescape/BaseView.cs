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
// Ported from: src/Basescape/BaseView.h, src/Basescape/BaseView.cpp
#nullable disable

using System.Globalization;

namespace Acom;

/// <summary>
/// Interactive view of a base.
/// Takes a certain base and displays all its facilities
/// and status, allowing players to manage them.
/// </summary>
public class BaseView : InteractiveSurface
{
	private const int BASE_SIZE = 6;
	private const int GRID_SIZE = 32;

	private Base _base;
	private SurfaceSet _texture;
	private readonly BaseFacility[,] _facilities = new BaseFacility[BASE_SIZE, BASE_SIZE];
	private BaseFacility _selFacility;
	private Font _big, _small;
	private Language _lang;
	private int _gridX, _gridY;
	private int _selSizeX, _selSizeY;
	private Surface _selector;
	private bool _blink;
	private readonly Timer _timer;
	private byte _redColor, _yellowColor, _greenColor;
	private bool _highContrast;
	private byte _cellColor, _selectorColor;

	/// <summary>
	/// Sets up a base view with the specified size and position.
	/// </summary>
	public BaseView(int width, int height, int x = 0, int y = 0) : base(width, height, x, y)
	{
		_blink = true;
		_highContrast = true;

		_timer = new Timer(100);
		_timer.OnTimer((SurfaceHandler)Blink);
		_timer.Start();
	}

	/// <summary>
	/// Changes the various resources needed for text rendering.
	/// </summary>
	public override void InitText(Font big, Font small, Language lang)
	{
		_big = big;
		_small = small;
		_lang = lang;
	}

	/// <summary>
	/// Changes the current base to display and
	/// initializes the internal base grid.
	/// </summary>
	public void SetBase(Base @base)
	{
		_base = @base;
		_selFacility = null;

		// Clear grid
		Array.Clear(_facilities);

		// Fill grid with base facilities
		foreach (var fac in _base.Facilities)
		{
			for (int y = fac.Y; y < fac.Y + fac.Rules.SizeY; ++y)
			{
				for (int x = fac.X; x < fac.X + fac.Rules.SizeX; ++x)
				{
					_facilities[x, y] = fac;
				}
			}
		}

		_redraw = true;
	}

	/// <summary>
	/// Changes the texture to use for drawing
	/// the various base elements.
	/// </summary>
	public void SetTexture(SurfaceSet texture)
	{
		_texture = texture;
	}

	/// <summary>
	/// Returns the facility the mouse is currently over (null if none).
	/// </summary>
	public BaseFacility SelectedFacility => _selFacility;

	/// <summary>
	/// Prevents any mouseover bugs on dismantling base facilities before setBase has had time to update the base.
	/// </summary>
	public void ResetSelectedFacility()
	{
		_facilities[_selFacility.X, _selFacility.Y] = null;
		_selFacility = null;
	}

	/// <summary>
	/// Returns the X position of the grid square
	/// the mouse is currently over.
	/// </summary>
	public int GridX => _gridX;

	/// <summary>
	/// Returns the Y position of the grid square
	/// the mouse is currently over.
	/// </summary>
	public int GridY => _gridY;

	/// <summary>
	/// If enabled, the base view will respond to player input,
	/// highlighting the selected facility.
	/// </summary>
	/// <param name="sizeX">Facility width (0 disables it).</param>
	/// <param name="sizeY">Facility height (0 disables it).</param>
	public void SetSelectable(int sizeX, int sizeY)
	{
		_selSizeX = sizeX;
		_selSizeY = sizeY;
		if (_selSizeX > 0 && _selSizeY > 0)
		{
			_selector = new Surface(sizeX * GRID_SIZE, sizeY * GRID_SIZE, _x, _y);
			_selector.SetPalette(Palette);
			var r = new SDL_Rect(0, 0, (ushort)_selector.Width, (ushort)_selector.Height);
			_selector.DrawRect(ref r, _selectorColor);
			r.w -= 2;
			r.h -= 2;
			r.x++;
			r.y++;
			_selector.DrawRect(ref r, 0);
			_selector.Visible = false;
		}
		else
		{
			// C++ deletes the selector but keeps the dangling pointer
			_selector = null;
		}
	}

	/// <summary>
	/// Returns if a certain facility can be successfully
	/// placed on the currently selected square.
	/// </summary>
	/// <param name="rule">Facility type.</param>
	/// <param name="facilityBeingMoved">Selected facility.</param>
	/// <param name="isStartFacility">Is this a start facility?</param>
	/// <returns>BPE_None if placeable, otherwise the reason why it couldn't be placed.</returns>
	public BasePlacementErrors GetPlacementError(RuleBaseFacility rule, BaseFacility facilityBeingMoved = null, bool isStartFacility = false)
	{
		// We'll need to know for the final check if we're upgrading an existing facility
		bool buildingOverExisting = false;

		// Area where we want to place a new building
		BaseAreaSubset placementArea = new BaseAreaSubset(rule.SizeX, rule.SizeY).Offset(_gridX, _gridY);
		// Whole base
		BaseAreaSubset baseArea = new BaseAreaSubset(BASE_SIZE, BASE_SIZE);

		// Check if the facility fits inside the base boundaries
		if (BaseAreaSubset.Intersection(placementArea, baseArea) != placementArea)
		{
			return BasePlacementErrors.BPE_NotConnected;
		}

		// Check usage of facilites in the area that will be replaced by a new building
		if (facilityBeingMoved == null)
		{
			BasePlacementErrors areaUseError = _base.IsAreaInUse(placementArea, rule);
			if (areaUseError != BasePlacementErrors.BPE_None)
			{
				return areaUseError;
			}
		}

		// Check if all squares are occupied already (for facilities that can be built only as upgrades)
		if (rule.IsUpgradeOnly)
		{
			for (int y = placementArea.beg_y; y < placementArea.end_y; ++y)
			{
				for (int x = placementArea.beg_x; x < placementArea.end_x; ++x)
				{
					BaseFacility facility = _facilities[x, y];
					if (facility == null)
					{
						return BasePlacementErrors.BPE_UpgradeOnly;
					}
				}
			}
		}

		// Check if square isn't occupied
		for (int y = placementArea.beg_y; y < placementArea.end_y; ++y)
		{
			for (int x = placementArea.beg_x; x < placementArea.end_x; ++x)
			{
				BaseFacility facility = _facilities[x, y];
				if (facility != null)
				{
					if (isStartFacility)
					{
						return BasePlacementErrors.BPE_NotConnected;
					}
					// when moving an existing facility, it should not block itself
					if (facilityBeingMoved == null)
					{
						// Further check to see if the facility already there can be built over and we're not removing an important base function
						BasePlacementErrors canBuildOverError = rule.GetCanBuildOverOtherFacility(facility.Rules);
						if (canBuildOverError != BasePlacementErrors.BPE_None)
						{
							return canBuildOverError;
						}

						// Make sure the facility we're building over is entirely within the size of the one we're checking
						BaseAreaSubset removedArea = facility.Placement;
						if (BaseAreaSubset.Intersection(placementArea, removedArea) != removedArea)
						{
							return BasePlacementErrors.BPE_UpgradeSizeMismatch;
						}

						// Make sure this facility is not already being upgraded
						if (facility.IfHadPreviousFacility && facility.BuildTime != 0)
						{
							return BasePlacementErrors.BPE_Upgrading;
						}

						buildingOverExisting = true;
					}
					else if (facility != facilityBeingMoved)
					{
						return BasePlacementErrors.BPE_NotConnected;
					}
				}
			}
		}

		bool bq = Options.allowBuildingQueue;
		bool hasConnectingFacility = false;

		// Check for another facility to connect to
		for (int i = 0; i < rule.SizeX; ++i)
		{
			// top
			if (_gridY > 0 && _facilities[_gridX + i, _gridY - 1] != null)
			{
				hasConnectingFacility = true;
				if ((!buildingOverExisting && bq) || _facilities[_gridX + i, _gridY - 1].IsBuiltOrHadPreviousFacility)
					return BasePlacementErrors.BPE_None;
			}
			// bottom
			if (_gridY + rule.SizeY < BASE_SIZE && _facilities[_gridX + i, _gridY + rule.SizeY] != null)
			{
				hasConnectingFacility = true;
				if ((!buildingOverExisting && bq) || _facilities[_gridX + i, _gridY + rule.SizeY].IsBuiltOrHadPreviousFacility)
					return BasePlacementErrors.BPE_None;
			}
		}
		for (int i = 0; i < rule.SizeY; ++i)
		{
			// left
			if (_gridX > 0 && _facilities[_gridX - 1, _gridY + i] != null)
			{
				hasConnectingFacility = true;
				if ((!buildingOverExisting && bq) || _facilities[_gridX - 1, _gridY + i].IsBuiltOrHadPreviousFacility)
					return BasePlacementErrors.BPE_None;
			}
			// right
			if (_gridX + rule.SizeX < BASE_SIZE && _facilities[_gridX + rule.SizeX, _gridY + i] != null)
			{
				hasConnectingFacility = true;
				if ((!buildingOverExisting && bq) || _facilities[_gridX + rule.SizeX, _gridY + i].IsBuiltOrHadPreviousFacility)
					return BasePlacementErrors.BPE_None;
			}
		}

		// We can assume if we've reached this point that none of the connecting facilities are finished!
		if (hasConnectingFacility && (!bq || buildingOverExisting))
			return BasePlacementErrors.BPE_Queue;

		return BasePlacementErrors.BPE_NotConnected;
	}

	/// <summary>
	/// Returns if the placed facility is placed in queue or not.
	/// </summary>
	public bool IsQueuedBuilding(RuleBaseFacility rule)
	{
		for (int i = 0; i < rule.SizeX; ++i)
		{
			if ((_gridY > 0 && _facilities[_gridX + i, _gridY - 1] != null && _facilities[_gridX + i, _gridY - 1].IsBuiltOrHadPreviousFacility) ||
				(_gridY + rule.SizeY < BASE_SIZE && _facilities[_gridX + i, _gridY + rule.SizeY] != null && _facilities[_gridX + i, _gridY + rule.SizeY].IsBuiltOrHadPreviousFacility))
			{
				return false;
			}
		}
		for (int i = 0; i < rule.SizeY; ++i)
		{
			if ((_gridX > 0 && _facilities[_gridX - 1, _gridY + i] != null && _facilities[_gridX - 1, _gridY + i].IsBuiltOrHadPreviousFacility) ||
				(_gridX + rule.SizeX < BASE_SIZE && _facilities[_gridX + rule.SizeX, _gridY + i] != null && _facilities[_gridX + rule.SizeX, _gridY + i].IsBuiltOrHadPreviousFacility))
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// ReCalculates the remaining build-time of all queued buildings.
	/// </summary>
	public void ReCalcQueuedBuildings()
	{
		SetBase(_base);
		var facilities = new List<BaseFacility>();
		foreach (var fac in _base.Facilities)
		{
			if (fac.AdjustedBuildTime > 0)
			{
				// Set all queued buildings to infinite.
				if (fac.AdjustedBuildTime > fac.Rules.BuildTime)
				{
					fac.BuildTime = int.MaxValue;
				}
				facilities.Add(fac);
			}
		}

		// Applying a simple Dijkstra Algorithm
		while (facilities.Count != 0)
		{
			int min = 0;
			for (int it = 0; it < facilities.Count; ++it)
			{
				if (facilities[it].AdjustedBuildTime < facilities[min].AdjustedBuildTime)
				{
					min = it;
				}
			}
			BaseFacility facility = facilities[min];
			facilities.RemoveAt(min);
			RuleBaseFacility rule = facility.Rules;
			int x = facility.X, y = facility.Y;
			for (int i = 0; i < rule.SizeX; ++i)
			{
				if (y > 0) UpdateNeighborFacilityBuildTime(facility, _facilities[x + i, y - 1]);
				if (y + rule.SizeY < BASE_SIZE) UpdateNeighborFacilityBuildTime(facility, _facilities[x + i, y + rule.SizeY]);
			}
			for (int i = 0; i < rule.SizeY; ++i)
			{
				if (x > 0) UpdateNeighborFacilityBuildTime(facility, _facilities[x - 1, y + i]);
				if (x + rule.SizeX < BASE_SIZE) UpdateNeighborFacilityBuildTime(facility, _facilities[x + rule.SizeX, y + i]);
			}
		}
	}

	/// <summary>
	/// Updates the neighborFacility's build time. This is for internal use only (reCalcQueuedBuildings()).
	/// </summary>
	private static void UpdateNeighborFacilityBuildTime(BaseFacility facility, BaseFacility neighbor)
	{
		if (facility != null && neighbor != null
		&& neighbor.AdjustedBuildTime > neighbor.Rules.BuildTime
		&& facility.AdjustedBuildTime + neighbor.Rules.BuildTime < neighbor.AdjustedBuildTime)
			neighbor.BuildTime = facility.AdjustedBuildTime + neighbor.Rules.BuildTime;
	}

	/// <summary>
	/// Keeps the animation timers running.
	/// </summary>
	public override void Think()
	{
		_timer.Think(null, this);
	}

	/// <summary>
	/// Makes the facility selector blink.
	/// </summary>
	public void Blink()
	{
		_blink = !_blink;

		if (_selSizeX > 0 && _selSizeY > 0)
		{
			var r = new SDL_Rect(0, 0, (ushort)_selector.Width, (ushort)_selector.Height);
			if (_blink)
			{
				_selector.DrawRect(ref r, _selectorColor);
				r.w -= 2;
				r.h -= 2;
				r.x++;
				r.y++;
				_selector.DrawRect(ref r, 0);
			}
			else
			{
				_selector.DrawRect(ref r, 0);
			}
		}
	}

	/// <summary>
	/// Draws the view of all the facilities in the base, connectors
	/// between them and crafts landed in hangars.
	/// </summary>
	public override void Draw()
	{
		base.Draw();

		// Draw grid squares
		for (int x = 0; x < BASE_SIZE; ++x)
		{
			for (int y = 0; y < BASE_SIZE; ++y)
			{
				Surface frame = _texture.GetFrame(_base.GlobeTexture != null ? _base.GlobeTexture.BaseGridSprite : 0);
				int fx = (x * GRID_SIZE);
				int fy = (y * GRID_SIZE);
				frame.BlitNShade(this, fx, fy);
			}
		}

		int craftIt = 0;
		var crafts = _base.Crafts;

		foreach (var fac in _base.Facilities)
		{
			// Draw facility shape
			int num = 0;
			for (int y = fac.Y; y < fac.Y + fac.Rules.SizeY; ++y)
			{
				for (int x = fac.X; x < fac.X + fac.Rules.SizeX; ++x)
				{
					Surface frame;

					int outline = fac.Rules.IsSmall ? 3 : fac.Rules.SizeX * fac.Rules.SizeY;
					if (fac.BuildTime == 0)
						frame = _texture.GetFrame(fac.Rules.SpriteShape + num);
					else
						frame = _texture.GetFrame(fac.Rules.SpriteShape + num + outline);

					int fx = (x * GRID_SIZE);
					int fy = (y * GRID_SIZE);
					frame.BlitNShade(this, fx, fy);

					num++;
				}
			}
		}

		foreach (var fac in _base.Facilities)
		{
			// Draw connectors
			if (fac.IsBuiltOrHadPreviousFacility && !fac.Rules.ConnectorsDisabled())
			{
				// Facilities to the right
				int x = fac.X + fac.Rules.SizeX;
				if (x < BASE_SIZE)
				{
					for (int y = fac.Y; y < fac.Y + fac.Rules.SizeY; ++y)
					{
						if (_facilities[x, y] != null && _facilities[x, y].IsBuiltOrHadPreviousFacility && !_facilities[x, y].Rules.ConnectorsDisabled())
						{
							Surface frame = _texture.GetFrame(7);
							int fx = (x * GRID_SIZE - GRID_SIZE / 2);
							int fy = (y * GRID_SIZE);
							frame.BlitNShade(this, fx, fy);
						}
					}
				}

				// Facilities to the bottom
				int y2 = fac.Y + fac.Rules.SizeY;
				if (y2 < BASE_SIZE)
				{
					for (int subX = fac.X; subX < fac.X + fac.Rules.SizeX; ++subX)
					{
						if (_facilities[subX, y2] != null && _facilities[subX, y2].IsBuiltOrHadPreviousFacility && !_facilities[subX, y2].Rules.ConnectorsDisabled())
						{
							Surface frame = _texture.GetFrame(8);
							int fx = (subX * GRID_SIZE);
							int fy = (y2 * GRID_SIZE - GRID_SIZE / 2);
							frame.BlitNShade(this, fx, fy);
						}
					}
				}
			}
		}

		foreach (var fac in _base.Facilities)
		{
			// Draw facility graphic
			int num = 0;
			for (int y = fac.Y; y < fac.Y + fac.Rules.SizeY; ++y)
			{
				for (int x = fac.X; x < fac.X + fac.Rules.SizeX; ++x)
				{
					if (fac.Rules.SpriteEnabled)
					{
						Surface frame = _texture.GetFrame(fac.Rules.SpriteFacility + num);
						int fx = (x * GRID_SIZE);
						int fy = (y * GRID_SIZE);
						frame.BlitNShade(this, fx, fy);
					}

					num++;
				}
			}

			// Draw crafts
			fac.CraftForDrawing = null;
			if (fac.BuildTime == 0 && fac.Rules.Crafts > 0)
			{
				if (craftIt != crafts.Count)
				{
					if (crafts[craftIt].Status != "STR_OUT")
					{
						Surface frame = _texture.GetFrame(crafts[craftIt].SkinSprite + 33);
						int fx = (fac.X * GRID_SIZE + (fac.Rules.SizeX - 1) * GRID_SIZE / 2 + 2);
						int fy = (fac.Y * GRID_SIZE + (fac.Rules.SizeY - 1) * GRID_SIZE / 2 - 4);
						frame.BlitNShade(this, fx, fy);
						fac.CraftForDrawing = crafts[craftIt];
					}
					++craftIt;
				}
			}

			// Draw time remaining
			if (fac.BuildTime > 0 || fac.Disabled)
			{
				var text = new Text(GRID_SIZE * fac.Rules.SizeX, 16, 0, 0);
				text.SetPalette(Palette);
				text.InitText(_big, _small, _lang);
				text.X = fac.X * GRID_SIZE;
				text.Y = fac.Y * GRID_SIZE + (GRID_SIZE * fac.Rules.SizeY - 16) / 2;
				text.SetBig();
				string ss;
				if (fac.Disabled)
					ss = "X";
				else
					ss = fac.BuildTime.ToString(CultureInfo.InvariantCulture);
				if (fac.IfHadPreviousFacility) // Indicate that this facility still counts for connectivity
					ss += "*";
				text.Align = TextHAlign.ALIGN_CENTER;
				text.SetColor(_cellColor);
				text.SetText(ss);
				text.Blit(this);
			}

			// Draw ammo indicator
			if (fac.BuildTime == 0 && fac.Rules.AmmoMax > 0)
			{
				var text = new Text(GRID_SIZE * fac.Rules.SizeX, 9, 0, 0);
				text.SetPalette(Palette);
				text.InitText(_big, _small, _lang);
				text.X = fac.X * GRID_SIZE;
				text.Y = fac.Y * GRID_SIZE;
				text.SetHighContrast(_highContrast);
				if (fac.Ammo >= fac.Rules.AmmoMax)
					text.SetColor(_greenColor); // 100%
				else if (fac.Ammo <= fac.Rules.AmmoMax / 2)
					text.SetColor(_redColor); // 0-50%
				else
					text.SetColor(_yellowColor); // 51-99%
				text.SetText(string.Create(CultureInfo.InvariantCulture, $"{fac.Ammo}/{fac.Rules.AmmoMax}"));
				text.Blit(this);
			}
		}
	}

	/// <summary>
	/// Blits the base view and selector.
	/// </summary>
	public override void Blit(Surface surface)
	{
		base.Blit(surface);
		if (_selector != null)
		{
			_selector.Blit(surface);
		}
	}

	/// <summary>
	/// Selects the facility the mouse is over.
	/// </summary>
	public override void MouseOver(Action action, State state)
	{
		_gridX = (int)Math.Floor(action.RelativeXMouse / (GRID_SIZE * action.XScale));
		_gridY = (int)Math.Floor(action.RelativeYMouse / (GRID_SIZE * action.YScale));
		if (_gridX >= 0 && _gridX < BASE_SIZE && _gridY >= 0 && _gridY < BASE_SIZE)
		{
			_selFacility = _facilities[_gridX, _gridY];
			if (_selSizeX > 0 && _selSizeY > 0)
			{
				if (_gridX + _selSizeX - 1 < BASE_SIZE && _gridY + _selSizeY - 1 < BASE_SIZE)
				{
					_selector.X = _x + _gridX * GRID_SIZE;
					_selector.Y = _y + _gridY * GRID_SIZE;
					_selector.Visible = true;
				}
				else
				{
					_selector.Visible = false;
				}
			}
		}
		else
		{
			_selFacility = null;
			if (_selSizeX > 0 && _selSizeY > 0)
			{
				_selector.Visible = false;
			}
		}

		base.MouseOver(action, state);
	}

	/// <summary>
	/// Deselects the facility.
	/// </summary>
	public override void MouseOut(Action action, State state)
	{
		_selFacility = null;
		if (_selSizeX > 0 && _selSizeY > 0)
		{
			_selector.Visible = false;
		}

		base.MouseOut(action, state);
	}

	public override void SetColor(byte color)
	{
		_cellColor = color;
	}

	public override void SetSecondaryColor(byte color)
	{
		_selectorColor = color;
	}

	public void SetOtherColors(byte red, byte yellow, byte green, bool highContrast)
	{
		_redColor = red;
		_yellowColor = yellow;
		_greenColor = green;
		_highContrast = highContrast;
	}

	/// <summary>Test hook: the grid square the mouse is over (C++ sets it in mouseOver).</summary>
	internal void SetGridPosition(int x, int y)
	{
		_gridX = x;
		_gridY = y;
	}
}
