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
// Ported from: src/Battlescape/MiniMapView.h, src/Battlescape/MiniMapView.cpp
// Port status: partial (missing: SDL_WarpMouse/SDL_EventState pointer warping while drag-scrolling)
#nullable disable

namespace Acom;

/// <summary>
/// MiniMapView is the class used to display the map in the MiniMapState.
/// </summary>
public class MiniMapView : InteractiveSurface
{
	private const int CELL_WIDTH = 4;
	private const int CELL_HEIGHT = 4;
	private const int MAX_FRAME = 2;

	private readonly Game _game;
	private readonly Camera _camera;
	private readonly SavedBattleGame _battleGame;
	private int _frame;
	private readonly SurfaceSet _set;
	private readonly int _emptySpaceIndex;

	private bool _isMouseScrolling;
	private bool _isMouseScrolled;
	private int _xBeforeMouseScrolling, _yBeforeMouseScrolling;
	private int _mouseScrollX, _mouseScrollY;
	private Position _posBeforeMouseScrolling, _cursorPosition;
	private uint _mouseScrollingStartTime;
	private int _totalMouseMoveX, _totalMouseMoveY;
	private bool _mouseMovedOverThreshold;

	/// <summary>Initializes all the elements in the MiniMapView.</summary>
	/// <param name="w">The MiniMapView width.</param>
	/// <param name="h">The MiniMapView height.</param>
	/// <param name="x">The MiniMapView x origin.</param>
	/// <param name="y">The MiniMapView y origin.</param>
	/// <param name="game">Pointer to the core game.</param>
	/// <param name="camera">The Battlescape camera.</param>
	/// <param name="battleGame">Pointer to the SavedBattleGame.</param>
	public MiniMapView(int w, int h, int x, int y, Game game, Camera camera, SavedBattleGame battleGame) : base(w, h, x, y)
	{
		ArgumentNullException.ThrowIfNull(game);
		_game = game;
		_camera = camera;
		_battleGame = battleGame;
		_frame = 0;
		_isMouseScrolling = false;
		_isMouseScrolled = false;
		_xBeforeMouseScrolling = 0;
		_yBeforeMouseScrolling = 0;
		_mouseScrollX = 0;
		_mouseScrollY = 0;
		_mouseScrollingStartTime = 0;
		_totalMouseMoveX = 0;
		_totalMouseMoveY = 0;
		_mouseMovedOverThreshold = false;
		_set = _game.Mod.GetSurfaceSet("SCANG.DAT");
		_emptySpaceIndex = _game.Mod.GetInterface("minimap").GetElement("emptySpace").color;
	}

	/// <summary>Draws the minimap.</summary>
	public override void Draw()
	{
		int _startX = _camera.CenterPosition.x - ((Width / CELL_WIDTH) / 2);
		int _startY = _camera.CenterPosition.y - ((Height / CELL_HEIGHT) / 2);

		base.Draw();
		if (_set == null)
		{
			return;
		}
		DrawRect(0, 0, (short)Width, (short)Height, 15);
		Lock();
		Surface emptySpace = _set.GetFrame(_emptySpaceIndex);
		bool isAltPressed = _game.IsAltPressed(true);
		if (Options.IsPasswordCorrect())
		{
			isAltPressed = !isAltPressed;
		}
		for (int lvl = 0; lvl <= _camera.CenterPosition.z; lvl++)
		{
			int py = _startY;
			for (int y = 0; y < Height; y += CELL_HEIGHT)
			{
				int px = _startX;
				for (int x = 0; x < Width; x += CELL_WIDTH)
				{
					Position p = new(px, py, lvl);
					Tile t = _battleGame.GetTile(p);
					if (t == null)
					{
						if (isAltPressed)
						{
							emptySpace.BlitNShade(this, x, y, 0);
						}
						px++;
						continue;
					}
					for (int i = (int)TilePart.O_FLOOR; i < (int)TilePart.O_MAX; i++)
					{
						MapData data = t.GetMapData((TilePart)i);

						if (data != null && data.MiniMapIndex != 0)
						{
							Surface s = _set.GetFrame(data.MiniMapIndex + 35);
							if (s != null)
							{
								int shade = 16;
								if (t.IsDiscovered(TilePart.O_FLOOR))
								{
									shade = t.Shade;
									if (shade > 7) shade = 7; //vanilla
								}
								s.BlitNShade(this, x, y, shade);
							}
						}
					}
					// alive units
					if (t.Unit != null && (t.Unit.Visible || _battleGame.BughuntMode || _battleGame.DebugMode))
					{
						int frame = t.Unit.MiniMapSpriteIndex;
						int size = t.Unit.Armor.Size;
						frame += (t.Position.y - t.Unit.Position.y) * size;
						frame += t.Position.x - t.Unit.Position.x;
						frame += _frame * size * size;
						Surface s = _set.GetFrame(frame);
						if (size > 1 && t.Unit.Faction == UnitFaction.FACTION_NEUTRAL)
						{
							s.BlitNShade(this, x, y, 0, false, Pathfinding.red);
						}
						else
						{
							s.BlitNShade(this, x, y, 0);
						}
					}
					// perhaps (at least one) item on this tile?
					if (t.IsDiscovered(TilePart.O_FLOOR) && t.Inventory.Count != 0)
					{
						int frame = 9 + _frame;
						Surface s = _set.GetFrame(frame);
						bool allHidden = true;
						bool atLeastOnePrimed = false;
						foreach (BattleItem item in t.Inventory)
						{
							if (!item.Rules.IsHiddenOnMinimap)
							{
								allHidden = false;
								if (item.FuseTimer >= 0)
								{
									atLeastOnePrimed = true;
									break; // no need to search further
								}
							}
						}
						if (allHidden)
						{
							// empty
						}
						else if (atLeastOnePrimed)
						{
							// dye red
							s.BlitNShade(this, x, y, 0, false, Pathfinding.red);
						}
						else
						{
							// vanilla
							s.BlitNShade(this, x, y, 0);
						}
					}

					px++;
				}
				py++;
			}
		}
		Unlock();
		int centerX = Width / 2 - 1;
		int centerY = Height / 2 - 1;
		byte color = (byte)(1 + _frame * 3);
		int xOffset = CELL_WIDTH / 2;
		int yOffset = CELL_HEIGHT / 2;
		DrawLine(centerX - CELL_WIDTH, centerY - CELL_HEIGHT,
			 centerX - xOffset, centerY - yOffset,
			 color); // top left
		DrawLine(centerX + xOffset, centerY - yOffset,
			 centerX + CELL_WIDTH, centerY - CELL_HEIGHT,
			 color); // top right
		DrawLine(centerX - CELL_WIDTH, centerY + CELL_HEIGHT,
			 centerX - xOffset, centerY + yOffset,
			 color); // bottom left
		DrawLine(centerX + CELL_WIDTH, centerY + CELL_HEIGHT,
			 centerX + xOffset, centerY + yOffset,
			 color); //bottom right
	}

	/// <summary>Increments the displayed level.</summary>
	/// <returns>New display level.</returns>
	public int Up()
	{
		_camera.ViewLevel = _camera.ViewLevel + 1;
		_redraw = true;
		return _camera.ViewLevel;
	}

	/// <summary>Decrements the displayed level.</summary>
	/// <returns>New display level.</returns>
	public int Down()
	{
		_camera.ViewLevel = _camera.ViewLevel - 1;
		_redraw = true;
		return _camera.ViewLevel;
	}

	/// <summary>True if the drag-scroll mouse button is not held anymore (SDL_GetMouseState).</summary>
	private bool IsDragScrollButtonReleased()
		=> (_game.MouseButtonState & SDLMouse.SDL_BUTTON(Options.battleDragScrollButton)) == 0;

	/// <summary>Was the drag-scroll too short in time, so it was a click?</summary>
	private bool IsDragScrollTooShort()
		=> !_mouseMovedOverThreshold && unchecked((int)(SDLTimer.SDL_GetTicks() - _mouseScrollingStartTime)) <= Options.dragScrollTimeTolerance;

	/// <summary>Handles mouse presses on the minimap. Enters mouse-moving mode when the right button is used.</summary>
	public override void MousePress(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		base.MousePress(action, state);

		if (action.Details.button.button == Options.battleDragScrollButton)
		{
			_isMouseScrolling = true;
			_isMouseScrolled = false;
			// SDL_GetMouseState(&x, &y): the pointer's window position, which is that of this event
			_xBeforeMouseScrolling = action.Details.button.x;
			_yBeforeMouseScrolling = action.Details.button.y;
			_posBeforeMouseScrolling = _camera.CenterPosition;
			if (!Options.battleDragScrollInvert && _cursorPosition.z == 0)
			{
				// C++ reads motion.x/y, which alias button.x/y in the SDL event union
				_cursorPosition.x = (short)action.Details.button.x;
				_cursorPosition.y = (short)action.Details.button.y;
				// the Z is irrelevant to our mouse position, but we can use it as a boolean to check if the position is set or not
				_cursorPosition.z = 1;
			}
			_mouseScrollX = 0; _mouseScrollY = 0;
			_totalMouseMoveX = 0; _totalMouseMoveY = 0;
			_mouseMovedOverThreshold = false;
			_mouseScrollingStartTime = SDLTimer.SDL_GetTicks();
		}
	}

	/// <summary>Handles mouse clicks on the minimap. Will change the camera center to the clicked point.</summary>
	public override void MouseClick(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		base.MouseClick(action, state);

		// The following is the workaround for a rare problem where sometimes
		// the mouse-release event is missed for any reason.
		// However if the SDL is also missed the release event, then it is to no avail :(
		// (this part handles the release if it is missed and now an other button is used)
		if (_isMouseScrolling)
		{
			if (action.Details.button.button != Options.battleDragScrollButton
			&& IsDragScrollButtonReleased())
			{ // so we missed again the mouse-release :(
				// Check if we have to revoke the scrolling, because it was too short in time, so it was a click
				if (IsDragScrollTooShort())
				{ _camera.CenterOnPosition(_posBeforeMouseScrolling); _redraw = true; }
				_isMouseScrolled = _isMouseScrolling = false;
				StopScrolling(action);
			}
		}

		// Drag-Scroll release: release mouse-scroll-mode
		if (_isMouseScrolling)
		{
			// While scrolling, other buttons are ineffective
			if (action.Details.button.button == Options.battleDragScrollButton)
			{
				_isMouseScrolling = false;
				StopScrolling(action);
			}
			else
			{
				return;
			}
			// Check if we have to revoke the scrolling, because it was too short in time, so it was a click
			if (IsDragScrollTooShort())
			{
				_isMouseScrolled = false;
				StopScrolling(action);
				_camera.CenterOnPosition(_posBeforeMouseScrolling);
				_redraw = true;
			}
			if (_isMouseScrolled) return;
		}

		if (_game.IsRightClick(action))
		{
			((MiniMapState)state).BtnOkClick(action);
		}

		if (_game.IsLeftClick(action))
		{
			int origX = (int)(action.RelativeXMouse / action.XScale);
			int origY = (int)(action.RelativeYMouse / action.YScale);
			// get offset (in cells) of the click relative to center of screen
			int xOff = (origX / CELL_WIDTH) - ((Width / 2) / CELL_WIDTH);
			int yOff = (origY / CELL_HEIGHT) - ((Height / 2) / CELL_HEIGHT);
			// center the camera on this new position
			int newX = _camera.CenterPosition.x + xOff;
			int newY = _camera.CenterPosition.y + yOff;
			_camera.CenterOnPosition(new Position(newX, newY, _camera.ViewLevel));
			_redraw = true;
		}
	}

	/// <summary>
	/// Handles moving over the minimap.
	/// Will change the camera center when the mouse is moved in mouse-moving mode.
	/// </summary>
	public override void MouseOver(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		base.MouseOver(action, state);

		if (_isMouseScrolling && action.Details.type == SDLEventType.SDL_MOUSEMOTION)
		{
			// The following is the workaround for a rare problem where sometimes
			// the mouse-release event is missed for any reason.
			// However if the SDL is also missed the release event, then it is to no avail :(
			// (checking: is the dragScroll-mouse-button still pressed?)
			if (IsDragScrollButtonReleased())
			{ // so we missed again the mouse-release :(
				// Check if we have to revoke the scrolling, because it was too short in time, so it was a click
				if (IsDragScrollTooShort())
				{
					_camera.CenterOnPosition(_posBeforeMouseScrolling);
					_redraw = true;
				}
				_isMouseScrolled = _isMouseScrolling = false;
				StopScrolling(action);
				return;
			}

			_isMouseScrolled = true;

			// C++ (without touch): sets the mouse cursor back with SDL_WarpMouse(_xBeforeMouseScrolling, _yBeforeMouseScrolling)
			// while ignoring the resulting motion event. Missing: the host cannot warp the pointer yet.

			// Check the threshold
			_totalMouseMoveX += action.Details.motion.xrel;
			_totalMouseMoveY += action.Details.motion.yrel;
			if (!_mouseMovedOverThreshold)
				_mouseMovedOverThreshold = ((Math.Abs(_totalMouseMoveX) > Options.dragScrollPixelTolerance) || (Math.Abs(_totalMouseMoveY) > Options.dragScrollPixelTolerance));

			// Calculate the move
			int newX, newY;
			int scrollX, scrollY;

			if (Options.battleDragScrollInvert)
			{
				scrollX = action.Details.motion.xrel;
				scrollY = action.Details.motion.yrel;
			}
			else
			{
				scrollX = -action.Details.motion.xrel;
				scrollY = -action.Details.motion.yrel;
			}
			_mouseScrollX += scrollX;
			_mouseScrollY += scrollY;
			newX = (int)(_posBeforeMouseScrolling.x + _mouseScrollX / action.XScale / 4);
			newY = (int)(_posBeforeMouseScrolling.y + _mouseScrollY / action.YScale / 4);

			// Keep the limits...
			if (newX < -1 || _camera.MapSizeX < newX)
			{
				_mouseScrollX -= scrollX;
				newX = _posBeforeMouseScrolling.x + _mouseScrollX / 4;
			}
			if (newY < -1 || _camera.MapSizeY < newY)
			{
				_mouseScrollY -= scrollY;
				newY = _posBeforeMouseScrolling.y + _mouseScrollY / 4;
			}

			// Scrolling
			_camera.CenterOnPosition(new Position(newX, newY, _camera.ViewLevel));
			_redraw = true;

			if (Options.touchEnabled == false)
			{
				// We don't want to see the mouse-cursor jumping :)
				if (Options.battleDragScrollInvert)
				{
					action.Details.motion.x = _xBeforeMouseScrolling;
					action.Details.motion.y = _yBeforeMouseScrolling;
				}
				else
				{
					Position delta = new(-scrollX, -scrollY, 0);
					int barWidth = _game.Screen.CursorLeftBlackBand;
					int barHeight = _game.Screen.CursorTopBlackBand;
					int cursorX = _cursorPosition.x + delta.x;
					int cursorY = _cursorPosition.y + delta.y;
					_cursorPosition.x = (short)Clamp(cursorX, Round(X * action.XScale) + barWidth, Round((X + Width) * action.XScale) + barWidth);
					_cursorPosition.y = (short)Clamp(cursorY, Round(Y * action.YScale) + barHeight, Round((Y + Height) * action.YScale) + barHeight);
					action.Details.motion.x = _cursorPosition.x;
					action.Details.motion.y = _cursorPosition.y;
				}
			}
			_game.Cursor.Handle(action);
		}
	}

	/// <summary>
	/// Handles moving into the minimap.
	/// Stops the mouse-scrolling mode, if its left on.
	/// </summary>
	public override void MouseIn(Action action, State state)
	{
		base.MouseIn(action, state);

		_isMouseScrolling = false;
		SetButtonPressed(SDLMouse.SDL_BUTTON_RIGHT, false);
	}

	/// <summary>Updates the minimap animation.</summary>
	public void Animate()
	{
		_frame++;
		if (_frame > MAX_FRAME)
		{
			_frame = 0;
		}
		_redraw = true;
	}

	/// <summary>Moves the mouse back to where it started after we finish drag scrolling.</summary>
	public void StopScrolling(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (!Options.battleDragScrollInvert)
		{
			// C++: SDL_WarpMouse(_cursorPosition.x, _cursorPosition.y); missing, the host cannot warp the pointer yet.
			action.SetMouseAction(_cursorPosition.x, _cursorPosition.y, X, Y);
		}
		// reset our "mouse position stored" flag
		_cursorPosition.z = 0;
	}

	private static int Clamp(int x, int min, int max) => Math.Min(Math.Max(x, min), max);

	/// <summary>fmath.h Round: rounds half away from zero.</summary>
	private static int Round(double x) => (int)(x < 0.0 ? Math.Ceiling(x - 0.5) : Math.Floor(x + 0.5));
}
