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
// Ported from: src/Battlescape/Camera.h, src/Battlescape/Camera.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Class handling camera movement, either by mouse or by events on the battlescape.
/// </summary>
/// <remarks>All coordinates are logical pixels (HD: the map surface's <see cref="Surface.Scale"/> does not matter here).</remarks>
public class Camera
{
	public const int SCROLL_BORDER = 5;
	public const int SCROLL_DIAGONAL_EDGE = 60;

	private Timer _scrollMouseTimer, _scrollKeyTimer;
	private readonly int _spriteWidth, _spriteHeight;
	private readonly int _mapsize_x, _mapsize_y, _mapsize_z;
	private int _screenWidth, _screenHeight;
	private Position _mapOffset, _center;
	private int _scrollMouseX, _scrollMouseY, _scrollKeyX, _scrollKeyY;
	private bool _scrollTrigger;
	private int _visibleMapHeight;
	private bool _showAllLayers;
	private bool _showSingleLayer;
	private readonly Map _map;

	/// <summary>Sets up a camera.</summary>
	/// <param name="spriteWidth">Width of map sprite.</param>
	/// <param name="spriteHeight">Height of map sprite.</param>
	/// <param name="mapsize_x">Current map size in X axis.</param>
	/// <param name="mapsize_y">Current map size in Y axis.</param>
	/// <param name="mapsize_z">Current map size in Z axis.</param>
	/// <param name="map">Pointer to map surface.</param>
	/// <param name="visibleMapHeight">Current height the view is at.</param>
	public Camera(int spriteWidth, int spriteHeight, int mapsize_x, int mapsize_y, int mapsize_z, Map map, int visibleMapHeight)
	{
		ArgumentNullException.ThrowIfNull(map);
		_spriteWidth = spriteWidth;
		_spriteHeight = spriteHeight;
		_mapsize_x = mapsize_x;
		_mapsize_y = mapsize_y;
		_mapsize_z = mapsize_z;
		_screenWidth = map.Width;
		_screenHeight = map.Height;
		_mapOffset = new Position(-250, 250, 0);
		_visibleMapHeight = visibleMapHeight;
		_map = map;
	}

	/// <summary>SDL_GetMouseState(0, 0): bit mask of the pressed mouse buttons.</summary>
	private static byte MouseState() => State.GamePtr?.MouseButtonState ?? 0;

	private static int Clamp(int x, int min, int max) => Math.Min(Math.Max(x, min), max);

	/// <summary>Sets the camera's scrolling timer.</summary>
	/// <param name="mouse">Pointer to mouse timer.</param>
	/// <param name="key">Pointer to key timer.</param>
	public void SetScrollTimer(Timer mouse, Timer key)
	{
		_scrollMouseTimer = mouse;
		_scrollKeyTimer = key;
	}

	/// <summary>Handles camera mouse shortcuts.</summary>
	/// <param name="action">Pointer to an action.</param>
	/// <param name="state">State that the action handlers belong to.</param>
	public void MousePress(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT && Options.battleEdgeScroll == ScrollType.SCROLL_TRIGGER)
		{
			_scrollTrigger = true;
			MouseOver(action, null);
		}
		else if (Options.battleDragScrollButton != SDLMouse.SDL_BUTTON_MIDDLE || (MouseState() & SDLMouse.SDL_BUTTON(Options.battleDragScrollButton)) == 0)
		{
			if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELUP)
			{
				Up();
			}
			else if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELDOWN)
			{
				Down();
			}
		}
	}

	/// <summary>Handles camera mouse shortcuts.</summary>
	/// <param name="action">Pointer to an action.</param>
	/// <param name="state">State that the action handlers belong to.</param>
	public void MouseRelease(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT && Options.battleEdgeScroll == ScrollType.SCROLL_TRIGGER)
		{
			_scrollMouseX = 0;
			_scrollMouseY = 0;
			_scrollMouseTimer.Stop();
			_scrollTrigger = false;
			int posX = action.XMouse;
			int posY = action.YMouse;
			if ((posX < (SCROLL_BORDER * action.XScale) && posX > 0)
				|| (posX > (_screenWidth - SCROLL_BORDER) * action.XScale)
				|| (posY < (SCROLL_BORDER * action.YScale) && posY > 0)
				|| (posY > (_screenHeight - SCROLL_BORDER) * action.YScale))
				// A cheap hack to avoid handling this event as a click
				// on the map when the mouse is on the scroll-border
				action.Details.button.button = 0;
		}
	}

	/// <summary>Handles mouse over events.</summary>
	/// <param name="action">Pointer to an action.</param>
	/// <param name="state">State that the action handlers belong to.</param>
	public void MouseOver(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (_map.CursorType == CursorType.CT_NONE)
		{
			return;
		}

		if (Options.battleEdgeScroll == ScrollType.SCROLL_AUTO || _scrollTrigger)
		{
			int posX = action.XMouse;
			int posY = action.YMouse;
			int scrollSpeed = Options.battleScrollSpeed;

			//left scroll
			if (posX < (SCROLL_BORDER * action.XScale) && posX >= 0)
			{
				_scrollMouseX = scrollSpeed;
				// if close to top or bottom, also scroll diagonally
				//downleft
				if (posY < (SCROLL_DIAGONAL_EDGE * action.YScale) && posY >= 0)
				{
					_scrollMouseY = scrollSpeed / 2;
				}
				//upleft
				else if (posY > (_screenHeight - SCROLL_DIAGONAL_EDGE) * action.YScale)
				{
					_scrollMouseY = -scrollSpeed / 2;
				}
				else _scrollMouseY = 0;
			}
			//right scroll
			else if (posX > (_screenWidth - SCROLL_BORDER) * action.XScale)
			{
				_scrollMouseX = -scrollSpeed;
				// if close to top or bottom, also scroll diagonally
				//downright
				if (posY <= (SCROLL_DIAGONAL_EDGE * action.YScale) && posY >= 0)
				{
					_scrollMouseY = scrollSpeed / 2;
				}
				//upright
				else if (posY > (_screenHeight - SCROLL_DIAGONAL_EDGE) * action.YScale)
				{
					_scrollMouseY = -scrollSpeed / 2;
				}
				else _scrollMouseY = 0;
			}
			else if (posX != 0)
			{
				_scrollMouseX = 0;
			}

			//up
			if (posY < (SCROLL_BORDER * action.YScale) && posY >= 0)
			{
				_scrollMouseY = scrollSpeed;
				// if close to left or right edge, also scroll diagonally
				//up left
				if (posX < (SCROLL_DIAGONAL_EDGE * action.XScale) && posX >= 0)
				{
					_scrollMouseX = scrollSpeed;
					_scrollMouseY /= 2;
				}
				//up right
				else if (posX > (_screenWidth - SCROLL_DIAGONAL_EDGE) * action.XScale)
				{
					_scrollMouseX = -scrollSpeed;
					_scrollMouseY /= 2;
				}
			}
			//down
			else if (posY > (_screenHeight - SCROLL_BORDER) * action.YScale)
			{
				_scrollMouseY = -scrollSpeed;
				// if close to left or right edge, also scroll diagonally
				//down left
				if (posX < (SCROLL_DIAGONAL_EDGE * action.XScale) && posX >= 0)
				{
					_scrollMouseX = scrollSpeed;
					_scrollMouseY /= 2;
				}
				//down right
				else if (posX > (_screenWidth - SCROLL_DIAGONAL_EDGE) * action.XScale)
				{
					_scrollMouseX = -scrollSpeed;
					_scrollMouseY /= 2;
				}
			}
			else if (posY != 0 && _scrollMouseX == 0)
			{
				_scrollMouseY = 0;
			}

			if ((_scrollMouseX != 0 || _scrollMouseY != 0) && !_scrollMouseTimer.IsRunning && !_scrollKeyTimer.IsRunning && 0 == (MouseState() & SDLMouse.SDL_BUTTON(Options.battleDragScrollButton)))
			{
				_scrollMouseTimer.Start();
			}
			else if ((_scrollMouseX == 0 && _scrollMouseY == 0) && _scrollMouseTimer.IsRunning)
			{
				_scrollMouseTimer.Stop();
			}
		}
	}

	/// <summary>Handles camera keyboard shortcuts.</summary>
	/// <param name="action">Pointer to an action.</param>
	/// <param name="state">State that the action handlers belong to.</param>
	public void KeyboardPress(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (_map.CursorType == CursorType.CT_NONE)
		{
			return;
		}

		SDLKey key = action.Details.key.keysym.sym;
		int scrollSpeed = Options.battleScrollSpeed;
		if (key == Options.keyBattleLeft)
		{
			_scrollKeyX = scrollSpeed;
		}
		else if (key == Options.keyBattleRight)
		{
			_scrollKeyX = -scrollSpeed;
		}
		else if (key == Options.keyBattleUp)
		{
			_scrollKeyY = scrollSpeed;
		}
		else if (key == Options.keyBattleDown)
		{
			_scrollKeyY = -scrollSpeed;
		}

		UpdateKeyTimer();
	}

	/// <summary>Handles camera keyboard shortcuts.</summary>
	/// <param name="action">Pointer to an action.</param>
	/// <param name="state">State that the action handlers belong to.</param>
	public void KeyboardRelease(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (_map.CursorType == CursorType.CT_NONE)
		{
			return;
		}

		SDLKey key = action.Details.key.keysym.sym;
		if (key == Options.keyBattleLeft)
		{
			_scrollKeyX = 0;
		}
		else if (key == Options.keyBattleRight)
		{
			_scrollKeyX = 0;
		}
		else if (key == Options.keyBattleUp)
		{
			_scrollKeyY = 0;
		}
		else if (key == Options.keyBattleDown)
		{
			_scrollKeyY = 0;
		}

		UpdateKeyTimer();
	}

	/// <summary>Starts/stops the key scroll timer (shared tail of keyboardPress/keyboardRelease).</summary>
	private void UpdateKeyTimer()
	{
		if ((_scrollKeyX != 0 || _scrollKeyY != 0) && !_scrollKeyTimer.IsRunning && !_scrollMouseTimer.IsRunning && 0 == (MouseState() & SDLMouse.SDL_BUTTON(Options.battleDragScrollButton)))
		{
			_scrollKeyTimer.Start();
		}
		else if ((_scrollKeyX == 0 && _scrollKeyY == 0) && _scrollKeyTimer.IsRunning)
		{
			_scrollKeyTimer.Stop();
		}
	}

	/// <summary>Handles mouse-scrolling.</summary>
	public void ScrollMouse()
	{
		ScrollXY(_scrollMouseX, _scrollMouseY, true);
	}

	/// <summary>Handles keyboard-scrolling.</summary>
	public void ScrollKey()
	{
		ScrollXY(_scrollKeyX, _scrollKeyY, true);
	}

	/// <summary>Handles scrolling with given deviation.</summary>
	/// <param name="x">X deviation.</param>
	/// <param name="y">Y deviation.</param>
	/// <param name="redraw">Redraw map or not.</param>
	public void ScrollXY(int x, int y, bool redraw)
	{
		_mapOffset.x = (short)(_mapOffset.x + x);
		_mapOffset.y = (short)(_mapOffset.y + y);

		do
		{
			int xx = 0;
			int yy = 0;
			ConvertScreenToMap((_screenWidth / 2), (_visibleMapHeight / 2), ref xx, ref yy);
			_center.x = (short)xx;
			_center.y = (short)yy;

			// Handling map bounds...
			// Ok, this is a prototype, it should be optimized.
			// Actually this should be calculated instead of slow-approximation.
			if (_center.x < 0)             { _mapOffset.x -= 1; _mapOffset.y -= 1; continue; }
			if (_center.x > _mapsize_x -1) { _mapOffset.x += 1; _mapOffset.y += 1; continue; }
			if (_center.y < 0)             { _mapOffset.x += 1; _mapOffset.y -= 1; continue; }
			if (_center.y > _mapsize_y -1) { _mapOffset.x -= 1; _mapOffset.y += 1; continue; }
			break;
		}
		while (true);

		_map.RefreshSelectorPosition();
		if (redraw) _map.Invalidate();
	}

	/// <summary>Handles jumping with given deviation.</summary>
	/// <param name="x">X deviation.</param>
	/// <param name="y">Y deviation.</param>
	public void JumpXY(int x, int y)
	{
		_mapOffset.x = (short)(_mapOffset.x + x);
		_mapOffset.y = (short)(_mapOffset.y + y);
		int xx = 0;
		int yy = 0;
		ConvertScreenToMap((_screenWidth / 2), (_visibleMapHeight / 2), ref xx, ref yy);
		_center.x = (short)xx;
		_center.y = (short)yy;
	}

	/// <summary>Goes one level up.</summary>
	public void Up()
	{
		if (_mapOffset.z < _mapsize_z - 1)
		{
			_mapOffset.z++;
			_mapOffset.y = (short)(_mapOffset.y + _spriteHeight * 3 / 5);
			_map.Draw();
		}
	}

	/// <summary>Goes one level down.</summary>
	public void Down()
	{
		if (_mapOffset.z > 0)
		{
			_mapOffset.z--;
			_mapOffset.y = (short)(_mapOffset.y - _spriteHeight * 3 / 5);
			_map.Draw();
		}
	}

	/// <summary>The displayed level; setting it clamps to the map and redraws.</summary>
	public int ViewLevel
	{
		get => _mapOffset.z;
		set
		{
			_mapOffset.z = (short)Clamp(value, 0, _mapsize_z - 1);
			_map.Draw();
		}
	}

	/// <summary>Centers map on a certain position.</summary>
	/// <param name="pos">Position to center on.</param>
	/// <param name="redraw">Redraw map or not.</param>
	public void CenterOnPosition(Position pos, bool redraw = true)
	{
		Position screenPos = default;
		_center = pos;
		_center.x = (short)Clamp(_center.x, -1, _mapsize_x);
		_center.y = (short)Clamp(_center.y, -1, _mapsize_y);
		ConvertMapToScreen(_center, ref screenPos);

		_mapOffset.x = (short)(-(screenPos.x - (_screenWidth / 2)));
		_mapOffset.y = (short)(-(screenPos.y - (_visibleMapHeight / 2)));

		_mapOffset.z = _center.z;
		if (redraw) _map.Draw();
	}

	/// <summary>Gets map's center position (its z is the current view level).</summary>
	public Position CenterPosition
	{
		get
		{
			_center.z = _mapOffset.z;
			return _center;
		}
	}

	/// <summary>Converts screen coordinates to map coordinates.</summary>
	/// <param name="screenX">Screen x position.</param>
	/// <param name="screenY">Screen y position.</param>
	/// <param name="mapX">Map x position.</param>
	/// <param name="mapY">Map y position.</param>
	public void ConvertScreenToMap(int screenX, int screenY, ref int mapX, ref int mapY)
	{
		// add half a tile height to the mouse position per layer we are above the floor
		screenY += (-_spriteWidth / 2) + (_mapOffset.z) * ((_spriteHeight + _spriteWidth / 4) / 2);

		// calculate the actual x/y pixel position on a diamond shaped map
		// taking the view offset into account
		mapY = -screenX + _mapOffset.x + 2 * screenY - 2 * _mapOffset.y;
		mapX = screenY - _mapOffset.y - mapY / 4 - (_spriteWidth / 4);

		// to get the row & column itself, divide by the size of a tile
		mapX /= (_spriteWidth / 4);
		mapY /= _spriteWidth;

		mapX = Clamp(mapX, -1, _mapsize_x);
		mapY = Clamp(mapY, -1, _mapsize_y);
	}

	/// <summary>Converts map coordinates X,Y,Z to screen positions X, Y.</summary>
	/// <param name="mapPos">X,Y,Z coordinates on the map.</param>
	/// <param name="screenPos">Screen position.</param>
	public void ConvertMapToScreen(Position mapPos, ref Position screenPos)
	{
		screenPos.z = 0; // not used
		screenPos.x = (short)(mapPos.x * (_spriteWidth / 2) - mapPos.y * (_spriteWidth / 2));
		screenPos.y = (short)(mapPos.x * (_spriteWidth / 4) + mapPos.y * (_spriteWidth / 4) - mapPos.z * ((_spriteHeight + _spriteWidth / 4) / 2));
	}

	/// <summary>Converts voxel coordinates X,Y,Z to screen positions X, Y.</summary>
	/// <param name="voxelPos">X,Y,Z coordinates of the voxel.</param>
	/// <param name="screenPos">Screen position.</param>
	public void ConvertVoxelToScreen(Position voxelPos, ref Position screenPos)
	{
		Position mapPosition = voxelPos.ToTile();
		ConvertMapToScreen(mapPosition, ref screenPos);
		double dx = voxelPos.x - (mapPosition.x * 16);
		double dy = voxelPos.y - (mapPosition.y * 16);
		double dz = voxelPos.z - (mapPosition.z * 24);
		screenPos.x = (short)(screenPos.x + (int)(dx - dy) + (_spriteWidth / 2));
		screenPos.y = (short)(screenPos.y + (int)(((_spriteHeight / 2.0)) + (dx / 2.0) + (dy / 2.0) - dz));
		screenPos.x = (short)(screenPos.x + _mapOffset.x);
		screenPos.y = (short)(screenPos.y + _mapOffset.y);
	}

	/// <summary>Gets the map size x.</summary>
	public int MapSizeX => _mapsize_x;

	/// <summary>Gets the map size y.</summary>
	public int MapSizeY => _mapsize_y;

	/// <summary>The map x/y screen offset (z is the view level).</summary>
	public Position MapOffset
	{
		get => _mapOffset;
		set => _mapOffset = value;
	}

	/// <summary>Toggles showing all map layers.</summary>
	/// <returns>New layer setting.</returns>
	public int ToggleShowAllLayers()
	{
		_showAllLayers = !_showAllLayers;
		return _showAllLayers ? 2 : 1;
	}

	/// <summary>Checks if the camera is showing all map layers.</summary>
	public bool ShowAllLayers => _showAllLayers;

	/// <summary>Toggles showing a single map layer.</summary>
	public void ToggleShowSingleLayer() { _showSingleLayer = !_showSingleLayer; }

	/// <summary>Checks if the camera is showing a single map layer.</summary>
	public bool ShowSingleLayer => _showSingleLayer;

	/// <summary>Checks if map coordinates X,Y,Z are on screen.</summary>
	/// <param name="mapPos">Coordinates to check.</param>
	/// <param name="unitWalking">True to offset coordinates for a unit walking.</param>
	/// <param name="unitSize">size of unit (0 - single, 1 - 2x2, etc, used for walking only</param>
	/// <param name="boundary">True if it's for caching calculation</param>
	/// <returns>True if the map coordinates are on screen.</returns>
	public bool IsOnScreen(Position mapPos, bool unitWalking, int unitSize, bool boundary)
	{
		Position screenPos = default;
		ConvertMapToScreen(mapPos, ref screenPos);
		int posx = _spriteWidth / 2, posy = _spriteHeight - _spriteWidth / 4;
		int sizex = _spriteWidth / 2, sizey = _spriteHeight / 2;
		if (unitSize > 0)
		{
			posy -= _spriteWidth / 4;
			sizex = _spriteWidth * unitSize;
			sizey = _spriteWidth * unitSize / 2;
		}
		screenPos.x = (short)(screenPos.x + _mapOffset.x + posx);
		screenPos.y = (short)(screenPos.y + _mapOffset.y + posy);
		if (unitWalking)
		{
			/* pretty hardcoded hack to handle overlapping by icons
			(they are always in the center at the bottom of the screen)
			Free positioned icons would require more complex workaround.
			__________
			|________|
			||      ||
			|| ____ ||
			||_|XX|_||
			|________|
			 */
			if (boundary) //to make sprite updates even being slightly outside of screen
			{
				sizex += _spriteWidth;
				sizey += _spriteWidth / 2;
			}
			if (screenPos.x < 0 - sizex
				|| screenPos.x >= _screenWidth + sizex
				|| screenPos.y < 0 - sizey
				|| screenPos.y >= _screenHeight + sizey) return false; //totally outside
			int side = (_screenWidth - _map.IconWidth) / 2;
			if ((screenPos.y < (_screenHeight - _map.IconHeight) + sizey)) return true; //above icons
			if ((side > 1) && ((screenPos.x < side + sizex) || (screenPos.x >= (_screenWidth - side - sizex)))) return true; //at sides (if there are any)
			return false;
		}
		else
		{
			return screenPos.x >= 0
				&& screenPos.x <= _screenWidth - 10
				&& screenPos.y >= 0
				&& screenPos.y <= _screenHeight - 10;
		}
	}

	/// <summary>Resizes the viewable window of the camera.</summary>
	public void Resize()
	{
		_screenWidth = _map.Width;
		_screenHeight = _map.Height;
		_visibleMapHeight = _map.Height - _map.IconHeight;
	}

	/// <summary>Stops key scrolling.</summary>
	public void StopKeyScrolling()
	{
		_scrollKeyTimer.Stop();
	}

	/// <summary>Stops mouse scrolling.</summary>
	public void StopMouseScrolling()
	{
		_scrollMouseTimer.Stop();
	}
}
