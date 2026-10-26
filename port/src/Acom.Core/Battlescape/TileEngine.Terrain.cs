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
// Ported from: src/Battlescape/TileEngine.cpp (blockage, doors, gravity, items, moving units, windows, unit placement)
#nullable disable

using static Acom.ItemDamageType;
using static Acom.LightLayers;
using static Acom.MovementType;
using static Acom.TilePart;
using static Acom.UnitStatus;

namespace Acom;

public partial class TileEngine
{
	// Pathfinding::bigWallTypes as int (MapData::getBigWall() is an int in C++).
	private const int BLOCK = (int)Pathfinding.bigWallTypes.BLOCK;
	private const int BIGWALLNESW = (int)Pathfinding.bigWallTypes.BIGWALLNESW;
	private const int BIGWALLNWSE = (int)Pathfinding.bigWallTypes.BIGWALLNWSE;
	private const int BIGWALLWEST = (int)Pathfinding.bigWallTypes.BIGWALLWEST;
	private const int BIGWALLNORTH = (int)Pathfinding.bigWallTypes.BIGWALLNORTH;
	private const int BIGWALLEAST = (int)Pathfinding.bigWallTypes.BIGWALLEAST;
	private const int BIGWALLSOUTH = (int)Pathfinding.bigWallTypes.BIGWALLSOUTH;
	private const int BIGWALLEASTANDSOUTH = (int)Pathfinding.bigWallTypes.BIGWALLEASTANDSOUTH;
	private const int BIGWALLWESTANDNORTH = (int)Pathfinding.bigWallTypes.BIGWALLWESTANDNORTH;

	/// <summary>Calculates the amount of power that is blocked going from one tile to another on a different level.</summary>
	/// <param name="startTile">The tile where the power starts.</param>
	/// <param name="endTile">The adjacent tile where the power ends.</param>
	/// <param name="type">The type of power/damage.</param>
	/// <param name="skipObject">Skip the object part.</param>
	/// <returns>Amount of blockage of this power.</returns>
	public int VerticalBlockage(Tile startTile, Tile endTile, ItemDamageType type, bool skipObject = false)
	{
		int block = 0;

		// safety check
		if (startTile == null || endTile == null) return 255;

		Position startPos = startTile.Position;
		Position endPos = endTile.Position;
		int direction = endPos.z - startPos.z;

		if (direction == 0) return 0;

		Tile tmpTile;
		if (direction < 0) // down
		{
			tmpTile = startTile;
			block += Blockage(tmpTile, O_FLOOR, type);
			if (!skipObject)
				block += Blockage(tmpTile, O_OBJECT, type, Pathfinding.DIR_DOWN);
			if (startPos.x != endPos.x || startPos.y != endPos.y)
			{
				tmpTile = _save.GetTile(new Position(endPos.x, endPos.y, startPos.z));
				block += HorizontalBlockage(startTile, tmpTile, type, skipObject);
				block += Blockage(tmpTile, O_FLOOR, type);
				if (!skipObject)
					block += Blockage(tmpTile, O_OBJECT, type, Pathfinding.DIR_DOWN);
			}
		}
		else if (direction > 0) // up
		{
			tmpTile = _save.GetTile(new Position(startPos.x, startPos.y, startPos.z + 1));
			block += Blockage(tmpTile, O_FLOOR, type);
			if (!skipObject)
				block += Blockage(tmpTile, O_OBJECT, type, Pathfinding.DIR_UP);
			if (startPos.x != endPos.x || startPos.y != endPos.y)
			{
				tmpTile = _save.GetTile(new Position(endPos.x, endPos.y, startPos.z + 1));
				block += HorizontalBlockage(startTile, tmpTile, type, skipObject);
				block += Blockage(tmpTile, O_FLOOR, type);
				if (!skipObject)
					block += Blockage(tmpTile, O_OBJECT, type, Pathfinding.DIR_UP);
			}
		}

		return block;
	}

	/// <summary>Calculates the amount of power that is blocked going from one tile to another on the same level.</summary>
	/// <param name="startTile">The tile where the power starts.</param>
	/// <param name="endTile">The adjacent tile where the power ends.</param>
	/// <param name="type">The type of power/damage.</param>
	/// <param name="skipObject">Skip the object part.</param>
	/// <returns>Amount of blockage.</returns>
	public int HorizontalBlockage(Tile startTile, Tile endTile, ItemDamageType type, bool skipObject = false)
	{
		Position oneTileNorth = new(0, -1, 0);
		Position oneTileEast = new(1, 0, 0);
		Position oneTileSouth = new(0, 1, 0);
		Position oneTileWest = new(-1, 0, 0);

		// safety check
		if (startTile == null || endTile == null) return 255;

		Position startPos = startTile.Position;
		Position endPos = endTile.Position;
		if (startPos.z != endPos.z) return 0;
		Tile tmpTile;

		int direction = 0;
		Pathfinding.VectorToDirection(endPos - startPos, ref direction);
		if (direction == -1) return 0;
		int block = 0;

		switch (direction)
		{
			case 0: // north
				block = Blockage(startTile, O_NORTHWALL, type);
				break;
			case 1: // north east
				if (type == DT_NONE) //this is two-way diagonal visibility check, used in original game
				{
					block = Blockage(startTile, O_NORTHWALL, type) + Blockage(endTile, O_WESTWALL, type); //up+right

					tmpTile = _save.GetTile(startPos + oneTileNorth);
					if (tmpTile != null && tmpTile.GetMapData(O_OBJECT) != null && tmpTile.GetMapData(O_OBJECT).BigWall != BIGWALLNESW)
						block += Blockage(tmpTile, O_OBJECT, type, 3);

					if (block == 0) break; //this way is opened

					tmpTile = _save.GetTile(startPos + oneTileEast);
					block = Blockage(tmpTile, O_NORTHWALL, type)
						+ Blockage(tmpTile, O_WESTWALL, type); //right+up
					if (tmpTile != null && tmpTile.GetMapData(O_OBJECT) != null && tmpTile.GetMapData(O_OBJECT).BigWall != BIGWALLNESW)
						block += Blockage(tmpTile, O_OBJECT, type, 7);
				}
				else
				{
					tmpTile = _save.GetTile(startPos + oneTileEast);
					block = (Blockage(startTile, O_NORTHWALL, type) + Blockage(endTile, O_WESTWALL, type)) / 2
						+ (Blockage(tmpTile, O_WESTWALL, type)
						+ Blockage(tmpTile, O_NORTHWALL, type)) / 2;

					tmpTile = _save.GetTile(startPos + oneTileNorth);
					block += (Blockage(tmpTile, O_OBJECT, type, 4)
						+ Blockage(tmpTile, O_OBJECT, type, 6)) / 2;
				}
				break;
			case 2: // east
				block = Blockage(endTile, O_WESTWALL, type);
				break;
			case 3: // south east
				if (type == DT_NONE)
				{
					tmpTile = _save.GetTile(startPos + oneTileSouth);
					block = Blockage(tmpTile, O_NORTHWALL, type)
						+ Blockage(endTile, O_WESTWALL, type); //down+right
					if (tmpTile != null && tmpTile.GetMapData(O_OBJECT) != null && tmpTile.GetMapData(O_OBJECT).BigWall != BIGWALLNWSE)
						block += Blockage(tmpTile, O_OBJECT, type, 1);

					if (block == 0) break; //this way is opened

					tmpTile = _save.GetTile(startPos + oneTileEast);
					block = Blockage(tmpTile, O_WESTWALL, type)
						+ Blockage(endTile, O_NORTHWALL, type); //right+down
					if (tmpTile != null && tmpTile.GetMapData(O_OBJECT) != null && tmpTile.GetMapData(O_OBJECT).BigWall != BIGWALLNWSE)
						block += Blockage(tmpTile, O_OBJECT, type, 5);
				}
				else
				{
					block = (Blockage(endTile, O_WESTWALL, type) + Blockage(endTile, O_NORTHWALL, type)) / 2
						+ (Blockage(_save.GetTile(startPos + oneTileEast), O_WESTWALL, type)
						+ Blockage(_save.GetTile(startPos + oneTileSouth), O_NORTHWALL, type)) / 2;
					block += (Blockage(_save.GetTile(startPos + oneTileSouth), O_OBJECT, type, 0)
						+ Blockage(_save.GetTile(startPos + oneTileEast), O_OBJECT, type, 6)) / 2;
				}
				break;
			case 4: // south
				block = Blockage(endTile, O_NORTHWALL, type);
				break;
			case 5: // south west
				if (type == DT_NONE)
				{
					tmpTile = _save.GetTile(startPos + oneTileSouth);
					block = Blockage(tmpTile, O_NORTHWALL, type)
						+ Blockage(tmpTile, O_WESTWALL, type); //down+left
					if (tmpTile != null && tmpTile.GetMapData(O_OBJECT) != null && tmpTile.GetMapData(O_OBJECT).BigWall != BIGWALLNESW)
						block += Blockage(tmpTile, O_OBJECT, type, 7);

					if (block == 0) break; //this way is opened

					block = Blockage(startTile, O_WESTWALL, type) + Blockage(endTile, O_NORTHWALL, type); //left+down
					tmpTile = _save.GetTile(startPos + oneTileWest);
					if (tmpTile != null && tmpTile.GetMapData(O_OBJECT) != null && tmpTile.GetMapData(O_OBJECT).BigWall != BIGWALLNESW)
						block += Blockage(tmpTile, O_OBJECT, type, 3);
				}
				else
				{
					block = (Blockage(endTile, O_NORTHWALL, type) + Blockage(startTile, O_WESTWALL, type)) / 2
						+ (Blockage(_save.GetTile(startPos + oneTileSouth), O_WESTWALL, type)
						+ Blockage(_save.GetTile(startPos + oneTileSouth), O_NORTHWALL, type)) / 2;
					block += (Blockage(_save.GetTile(startPos + oneTileSouth), O_OBJECT, type, 0)
						+ Blockage(_save.GetTile(startPos + oneTileWest), O_OBJECT, type, 2)) / 2;
				}
				break;
			case 6: // west
				block = Blockage(startTile, O_WESTWALL, type);
				break;
			case 7: // north west
				if (type == DT_NONE)
				{
					tmpTile = _save.GetTile(startPos + oneTileNorth);
					block = Blockage(startTile, O_NORTHWALL, type)
						+ Blockage(tmpTile, O_WESTWALL, type); //up+left
					if (tmpTile != null && tmpTile.GetMapData(O_OBJECT) != null && tmpTile.GetMapData(O_OBJECT).BigWall != BIGWALLNWSE)
						block += Blockage(tmpTile, O_OBJECT, type, 5);

					if (block == 0) break; //this way is opened

					tmpTile = _save.GetTile(startPos + oneTileWest);
					block = Blockage(startTile, O_WESTWALL, type)
						+ Blockage(tmpTile, O_NORTHWALL, type); //left+up
					if (tmpTile != null && tmpTile.GetMapData(O_OBJECT) != null && tmpTile.GetMapData(O_OBJECT).BigWall != BIGWALLNWSE)
						block += Blockage(tmpTile, O_OBJECT, type, 1);
				}
				else
				{
					block = (Blockage(startTile, O_WESTWALL, type) + Blockage(startTile, O_NORTHWALL, type)) / 2
						+ (Blockage(_save.GetTile(startPos + oneTileNorth), O_WESTWALL, type)
						+ Blockage(_save.GetTile(startPos + oneTileWest), O_NORTHWALL, type)) / 2;
					block += (Blockage(_save.GetTile(startPos + oneTileNorth), O_OBJECT, type, 4)
						+ Blockage(_save.GetTile(startPos + oneTileWest), O_OBJECT, type, 2)) / 2;
				}
				break;
		}

		if (!skipObject || (type == DT_NONE && startTile.IsBigWall))
			block += Blockage(startTile, O_OBJECT, type, direction);

		if (type != DT_NONE)
		{
			// not too sure about removing this line,
			// i have a sneaking suspicion we might end up blocking things that we shouldn't

			//if (skipObject) return block;

			direction += 4;
			if (direction > 7)
				direction -= 8;
			if (endTile.IsBigWall)
				block += Blockage(endTile, O_OBJECT, type, direction, true);
		}
		else
		{
			if (block <= 127)
			{
				direction += 4;
				if (direction > 7)
					direction -= 8;
				if (Blockage(endTile, O_OBJECT, type, direction, true) > 127)
				{
					return -1; //hit bigwall, reveal bigwall tile
				}
			}
		}

		return block;
	}

	/// <summary>Calculates the amount this certain wall or floor-part of the tile blocks.</summary>
	/// <param name="tile">The tile where the power starts.</param>
	/// <param name="part">The part of the tile the power needs to go through.</param>
	/// <param name="type">The type of power/damage.</param>
	/// <param name="direction">Direction the power travels.</param>
	/// <param name="checkingFromOrigin">Whether the check starts at the tile of the big wall.</param>
	/// <returns>Amount of blockage.</returns>
	private static int Blockage(Tile tile, TilePart part, ItemDamageType type, int direction = -1, bool checkingFromOrigin = false)
	{
		int blockage = 0;

		if (tile == null) return 255; // probably outside the map here

		MapData mapData = tile.GetMapData(part);
		if (mapData != null)
		{
			bool check = true;
			int wall = -1;
			if (direction != -1)
			{
				wall = tile.GetMapData(O_OBJECT).BigWall;

				if (type != DT_SMOKE &&
					checkingFromOrigin &&
					(wall == BIGWALLNESW ||
					wall == BIGWALLNWSE))
				{
					check = false;
				}
				switch (direction)
				{
					case 0: // north
						if (wall == BIGWALLWEST ||
							wall == BIGWALLEAST ||
							wall == BIGWALLSOUTH ||
							wall == BIGWALLEASTANDSOUTH)
						{
							check = false;
						}
						break;
					case 1: // north east
						if (wall == BIGWALLWEST ||
							wall == BIGWALLSOUTH)
						{
							check = false;
						}
						break;
					case 2: // east
						if (wall == BIGWALLNORTH ||
							wall == BIGWALLSOUTH ||
							wall == BIGWALLWEST ||
							wall == BIGWALLWESTANDNORTH)
						{
							check = false;
						}
						break;
					case 3: // south east
						if (wall == BIGWALLNORTH ||
							wall == BIGWALLWEST ||
							wall == BIGWALLWESTANDNORTH)
						{
							check = false;
						}
						break;
					case 4: // south
						if (wall == BIGWALLWEST ||
							wall == BIGWALLEAST ||
							wall == BIGWALLNORTH ||
							wall == BIGWALLWESTANDNORTH)
						{
							check = false;
						}
						break;
					case 5: // south west
						if (wall == BIGWALLNORTH ||
							wall == BIGWALLEAST)
						{
							check = false;
						}
						break;
					case 6: // west
						if (wall == BIGWALLNORTH ||
							wall == BIGWALLSOUTH ||
							wall == BIGWALLEAST ||
							wall == BIGWALLEASTANDSOUTH)
						{
							check = false;
						}
						break;
					case 7: // north west
						if (wall == BIGWALLSOUTH ||
							wall == BIGWALLEAST ||
							wall == BIGWALLEASTANDSOUTH)
						{
							check = false;
						}
						break;
					case 8: // up
					case 9: // down
						if (wall != 0 && wall != BLOCK)
						{
							check = false;
						}
						break;
					default:
						break;
				}
			}
			else if (part == O_FLOOR &&
						mapData.GetBlock(type) == 0)
			{
				if (type != DT_NONE)
				{
					blockage += mapData.Armor;
				}
				else if (!mapData.IsNoFloor)
				{
					return 256;
				}
			}

			if (check)
			{
				// -1 means we have a regular wall, and anything over 0 means we have a bigwall.
				if (type == DT_SMOKE && wall != 0 && !tile.IsUfoDoorOpen(part))
				{
					return 256;
				}
				blockage += mapData.GetBlock(type);
			}
		}

		// open ufo doors are actually still closed behind the scenes
		// so a special trick is needed to see if they are open, if they are, they obviously don't block anything
		if (tile.IsUfoDoorOpen(part))
			blockage = 0;

		return blockage;
	}

	/// <summary>Opens a door (if any) by rightclick, or by walking through it. The unit has to face in the right direction.</summary>
	/// <param name="unit">Unit.</param>
	/// <param name="rClick">Whether the player right clicked.</param>
	/// <param name="dir">Direction.</param>
	/// <returns>
	/// -1 there is no door, you can walk through;
	///  0 normal door opened, make a squeaky sound and you can walk through;
	///  1 ufo door is starting to open, make a whoosh sound, don't walk through;
	///  3 ufo door is still opening, don't walk through it yet. (have patience, futuristic technology...)
	///  4 not enough TUs
	///  5 would contravene fire reserve
	/// </returns>
	public int UnitOpensDoor(BattleUnit unit, bool rClick = false, int dir = -1)
	{
		int door = -1;
		int TUCost = 0;
		int size = unit.Armor.Size;
		int z = unit.Tile.TerrainLevel < -12 ? 1 : 0; // if we're standing on stairs, check the tile above instead.
		int doorsOpened = 0;
		Position doorCentre = default;

		if (dir == -1)
		{
			dir = unit.Direction;
		}
		Tile tile;
		for (int x = 0; x < size && door == -1; x++)
		{
			for (int y = 0; y < size && door == -1; y++)
			{
				List<(Position first, TilePart second)> checkPositions = [];
				tile = _save.GetTile(unit.Position + new Position(x, y, z));
				if (tile == null) continue;

				switch (dir)
				{
					case 0: // north
						checkPositions.Add((new Position(0, 0, 0), O_NORTHWALL)); // origin
						if (x != 0)
						{
							checkPositions.Add((new Position(0, -1, 0), O_WESTWALL)); // one tile north
						}
						break;
					case 1: // north east
						checkPositions.Add((new Position(0, 0, 0), O_NORTHWALL)); // origin
						checkPositions.Add((new Position(1, -1, 0), O_WESTWALL)); // one tile north-east
						if (rClick)
						{
							checkPositions.Add((new Position(1, 0, 0), O_WESTWALL)); // one tile east
							checkPositions.Add((new Position(1, 0, 0), O_NORTHWALL)); // one tile east
						}
						break;
					case 2: // east
						checkPositions.Add((new Position(1, 0, 0), O_WESTWALL)); // one tile east
						break;
					case 3: // south-east
						if (y == 0)
							checkPositions.Add((new Position(1, 1, 0), O_WESTWALL)); // one tile south-east
						if (x == 0)
							checkPositions.Add((new Position(1, 1, 0), O_NORTHWALL)); // one tile south-east
						if (rClick)
						{
							checkPositions.Add((new Position(1, 0, 0), O_WESTWALL)); // one tile east
							checkPositions.Add((new Position(0, 1, 0), O_NORTHWALL)); // one tile south
						}
						break;
					case 4: // south
						checkPositions.Add((new Position(0, 1, 0), O_NORTHWALL)); // one tile south
						break;
					case 5: // south-west
						checkPositions.Add((new Position(0, 0, 0), O_WESTWALL)); // origin
						checkPositions.Add((new Position(-1, 1, 0), O_NORTHWALL)); // one tile south-west
						if (rClick)
						{
							checkPositions.Add((new Position(0, 1, 0), O_WESTWALL)); // one tile south
							checkPositions.Add((new Position(0, 1, 0), O_NORTHWALL)); // one tile south
						}
						break;
					case 6: // west
						checkPositions.Add((new Position(0, 0, 0), O_WESTWALL)); // origin
						if (y != 0)
						{
							checkPositions.Add((new Position(-1, 0, 0), O_NORTHWALL)); // one tile west
						}
						break;
					case 7: // north-west
						checkPositions.Add((new Position(0, 0, 0), O_WESTWALL)); // origin
						checkPositions.Add((new Position(0, 0, 0), O_NORTHWALL)); // origin
						if (x != 0)
						{
							checkPositions.Add((new Position(-1, -1, 0), O_WESTWALL)); // one tile north
						}
						if (y != 0)
						{
							checkPositions.Add((new Position(-1, -1, 0), O_NORTHWALL)); // one tile north
						}
						if (rClick)
						{
							checkPositions.Add((new Position(0, -1, 0), O_WESTWALL)); // one tile north
							checkPositions.Add((new Position(-1, 0, 0), O_NORTHWALL)); // one tile west
						}
						break;
					default:
						break;
				}

				TilePart part = O_FLOOR;
				foreach (var pair in checkPositions)
				{
					if (door != -1)
					{
						break; // loop finished
					}
					tile = _save.GetTile(unit.Position + new Position(x, y, z) + pair.first);
					if (tile != null)
					{
						door = tile.OpenDoor(pair.second, unit, _save.BattleGame.ReservedAction, rClick);
						if (door != -1)
						{
							part = pair.second;
							if (door == 0)
							{
								++doorsOpened;
								doorCentre = unit.Position + new Position(x, y, z) + pair.first;
							}
							else if (door == 1)
							{
								var adjacentDoors = CheckAdjacentDoors(unit.Position + new Position(x, y, z) + pair.first, pair.second);
								doorsOpened += adjacentDoors.first + 1;
								doorCentre = adjacentDoors.second;
							}
						}
					}
				}
				if (door == 0 && rClick)
				{
					if (part == O_WESTWALL)
					{
						part = O_NORTHWALL;
					}
					else
					{
						part = O_WESTWALL;
					}
					TUCost = tile.GetTUCost((int)part, unit.MovementType);
				}
				else if (door == 1 || door == 4)
				{
					TUCost = tile.GetTUCost((int)part, unit.MovementType);
				}
			}
		}

		if (door == 0 || door == 1)
		{
			if (_save.BattleGame.CheckReservedTU(unit, TUCost, 0))
			{
				if (unit.SpendTimeUnits(TUCost))
				{
					CalculateLighting(LL_FIRE, doorCentre, doorsOpened, true);
					// Update FOV through the doorway.
					CalculateFOV(doorCentre, doorsOpened, true, true);
				}
				else return 4;
			}
			else return 5;
		}

		return door;
	}

	/// <summary>Opens any doors connected to this part at this position, keeps processing till it hits a non-ufo-door.</summary>
	/// <param name="pos">The starting position.</param>
	/// <param name="part">The part to open, defines which direction to check.</param>
	/// <returns>The number of adjacent doors opened, position of door centre.</returns>
	public (int first, Position second) CheckAdjacentDoors(Position pos, TilePart part)
	{
		Position offset;
		int adjacentDoorsOpened = 0;
		int doorOffset = 0;
		bool westSide = part == O_WESTWALL;
		for (int i = 1; ; ++i)
		{
			offset = westSide ? new Position(0, i, 0) : new Position(i, 0, 0);
			Tile tile = _save.GetTile(pos + offset);
			if (tile != null && tile.IsUfoDoor(part))
			{
				int doorAdj = tile.OpenDoor(part);
				if (doorAdj == 1) //only expecting ufo doors
				{
					adjacentDoorsOpened++;
					doorOffset++;
				}
			}
			else break;
		}
		for (int i = -1; ; --i)
		{
			offset = westSide ? new Position(0, i, 0) : new Position(i, 0, 0);
			Tile tile = _save.GetTile(pos + offset);
			if (tile != null && tile.IsUfoDoor(part))
			{
				int doorAdj = tile.OpenDoor(part);
				if (doorAdj == 1)
				{
					adjacentDoorsOpened++;
					doorOffset--;
				}
			}
			else break;
		}
		doorOffset /= 2; //Find ~centre of door
		return (adjacentDoorsOpened, pos + (westSide ? new Position(0, doorOffset, 0) : new Position(doorOffset, 0, 0)));
	}

	/// <summary>Closes ufo doors.</summary>
	/// <returns>Whether doors are closed.</returns>
	public int CloseUfoDoors()
	{
		int doorsclosed = 0;

		// prepare a list of tiles on fire/smoke & close any ufo doors
		for (int i = 0; i < _save.MapSizeXYZ; ++i)
		{
			if (_save.GetTile(i).Unit != null && _save.GetTile(i).Unit.IsBigUnit)
			{
				BattleUnit bu = _save.GetTile(i).Unit;
				Tile tile = _save.GetTile(i);
				Tile oneTileNorth = _save.GetTile(tile.Position + new Position(0, -1, 0));
				Tile oneTileWest = _save.GetTile(tile.Position + new Position(-1, 0, 0));
				if ((tile.IsUfoDoorOpen(O_NORTHWALL) && oneTileNorth != null && oneTileNorth.Unit != null && oneTileNorth.Unit == bu) ||
					(tile.IsUfoDoorOpen(O_WESTWALL) && oneTileWest != null && oneTileWest.Unit != null && oneTileWest.Unit == bu))
				{
					continue;
				}
			}
			doorsclosed += _save.GetTile(i).CloseUfoDoor();
		}

		return doorsclosed;
	}

	/// <summary>Applies gravity to a tile. Causes items and units to drop.</summary>
	/// <param name="t">Tile.</param>
	/// <returns>Tile where the items end up in eventually.</returns>
	public Tile ApplyGravity(Tile t)
	{
		if (t == null || (t.Inventory.Count == 0 && t.Unit == null)) return t; // skip this if there are no items

		BattleUnit occupant = t.Unit;

		if (occupant != null)
		{
			occupant.UpdateTileFloorState(_save);
			if (occupant.HaveNoFloorBelow())
			{
				if (!occupant.IsOutThresholdExceed)
				{
					if (occupant.MovementType == MT_FLY)
					{
						// move to the position you're already in. this will unset the kneeling flag, set the floating flag, etc.
						occupant.StartWalking(occupant.Direction, occupant.Position, _save);
						// and set our status to standing (rather than walking or flying) to avoid weirdness.
						occupant.AbortTurn();
					}
					else
					{
						occupant.SetPosition(occupant.Position); // this is necessary to set the unit up for falling correctly, updating their "lastPos"
						_save.AddFallingUnit(occupant);
					}
				}
			}
		}

		Tile rt = t;
		while (rt.Position.z > 0 && rt.HasNoFloor(_save))
		{
			rt = _save.GetBelowTile(rt);
		}

		foreach (BattleItem bi in t.Inventory)
		{
			if (bi.Unit != null && t.Position == bi.Unit.Position)
			{
				bi.Unit.SetPosition(rt.Position);
			}
			if (t != rt)
			{
				rt.AddItem(bi, bi.Slot);
			}
		}

		if (t != rt)
		{
			// clear tile
			t.Inventory.Clear();
		}

		return rt;
	}

	/// <summary>Drop item on ground.</summary>
	public void ItemDrop(Tile t, BattleItem item, bool updateLight)
	{
		// don't spawn anything outside of bounds
		if (t == null)
			return;

		Position p = t.Position;

		// don't ever drop fixed items
		if (item.Rules.IsFixed)
			return;

		if (_save.Side != UnitFaction.FACTION_PLAYER)
		{
			item.TurnFlag = true;
		}

		ItemMoveInventory(t, null, item, _inventorySlotGround, 0, 0);

		ApplyGravity(t);

		if (updateLight)
		{
			CalculateLighting(LL_ITEMS, p);
			CalculateFOV(p, item.VisibilityUpdateRange, false);
		}
	}

	/// <summary>Drop all unit items on ground.</summary>
	public void ItemDropInventory(Tile t, BattleUnit unit, bool unprimeItems = false, bool deleteFixedItems = false)
	{
		// Collections::removeIf: the predicate runs once per element in order and must not modify this collection.
		unit.Inventory.RemoveAll(i =>
		{
			if (!i.Rules.IsFixed)
			{
				i.Owner = null;
				if (unprimeItems && i.Rules.FuseTimerType != BattleFuseType.BFT_NONE)
				{
					if (i.Rules.CostUnprime.Time > 0 /* && !i->getRules()->getUnprimeActionName().empty() */)
					{
						i.FuseTimer = -1; // unprime explosives before dropping them
					}
				}
				t.AddItem(i, _inventorySlotGround);
				if (i.Unit != null && i.Unit.Status == STATUS_UNCONSCIOUS)
				{
					i.Unit.SetPosition(t.Position);
				}
				return true;
			}
			else
			{
				if (deleteFixedItems)
				{
					// first unload all ammo
					for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
					{
						if (i.NeedsAmmoForSlot(slot) && i.GetAmmoForSlot(slot) != null)
						{
							// unload the existing ammo (if any) from the weapon
							BattleItem oldAmmo = i.SetAmmoForSlot(slot, null);
							if (oldAmmo != null)
							{
								ItemDrop(t, oldAmmo, false);
							}
						}
					}

					// delete fixed items completely (e.g. when changing armor)
					i.Owner = null;
					_save.RemoveItem(i);
					return true;
				}
				else
				{
					// do nothing, fixed items cannot be moved (individually by the player)!
					return false;
				}
			}
		});

		// handle special built-in items
		if (deleteFixedItems)
		{
			unit.RemoveSpecialWeapons(_save);
		}
	}

	/// <summary>Move item to other place in inventory or ground.</summary>
	public void ItemMoveInventory(Tile t, BattleUnit unit, BattleItem item, RuleInventory slot, int x, int y)
	{
		// Handle dropping from/to ground.
		if (slot != item.Slot)
		{
			if (slot == _inventorySlotGround)
			{
				item.MoveToOwner(null);
				t.AddItem(item, slot);
				if (item.Unit != null && item.Unit.Status == STATUS_UNCONSCIOUS)
				{
					item.Unit.SetPosition(t.Position);
				}
			}
			else if (item.Slot == null || item.Slot == _inventorySlotGround)
			{
				item.MoveToOwner(unit);
				item.TurnFlag = false;
				if (item.Unit != null && item.Unit.Status == STATUS_UNCONSCIOUS)
				{
					item.Unit.SetPosition(invalid);
				}
			}
		}
		item.Slot = slot;
		item.SlotX = x;
		item.SlotY = y;
	}

	/// <summary>Add moving unit.</summary>
	public void AddMovingUnit(BattleUnit unit)
	{
		if (_movingUnit != null)
		{
			_movingUnitPrev.Add(_movingUnit);
		}
		_movingUnit = unit;
	}

	/// <summary>Remove moving unit.</summary>
	public void RemoveMovingUnit(BattleUnit unit)
	{
		if (_movingUnit != unit)
		{
			throw new InvalidOperationException("Wrong unit is removed from TileEngine movingUnit");
		}
		if (_movingUnitPrev.Count == 0)
		{
			_movingUnit = null;
		}
		else
		{
			_movingUnit = _movingUnitPrev[^1];
			_movingUnitPrev.RemoveAt(_movingUnitPrev.Count - 1);
		}
	}

	/// <summary>Get current moving unit.</summary>
	public BattleUnit MovingUnit { get => _movingUnit; }

	/// <summary>Gets the AI to look through a window.</summary>
	/// <param name="position">Current position.</param>
	/// <returns>Direction or -1 when no window found.</returns>
	public int FaceWindow(Position position)
	{
		Position oneTileEast = new(1, 0, 0);
		Position oneTileSouth = new(0, 1, 0);

		Tile tile = _save.GetTile(position);
		if (tile != null && tile.GetMapData(O_NORTHWALL) != null && tile.GetMapData(O_NORTHWALL).GetBlock(DT_NONE) == 0) return 0;
		tile = _save.GetTile(position + oneTileEast);
		if (tile != null && tile.GetMapData(O_WESTWALL) != null && tile.GetMapData(O_WESTWALL).GetBlock(DT_NONE) == 0) return 2;
		tile = _save.GetTile(position + oneTileSouth);
		if (tile != null && tile.GetMapData(O_NORTHWALL) != null && tile.GetMapData(O_NORTHWALL).GetBlock(DT_NONE) == 0) return 4;
		tile = _save.GetTile(position);
		if (tile != null && tile.GetMapData(O_WESTWALL) != null && tile.GetMapData(O_WESTWALL).GetBlock(DT_NONE) == 0) return 6;

		return -1;
	}

	private static readonly int[,] surroundingTilePositions = {
		{0, -1}, // north (-y direction)
		{1, -1}, // northeast
		{1, 0}, // east (+ x direction)
		{1, 1}, // southeast
		{0, 1}, // south (+y direction)
		{-1, 1}, // southwest
		{-1, 0}, // west (-x direction)
		{-1, -1}}; // northwest

	/// <summary>Checks if a position is a valid place for a unit to be placed.</summary>
	/// <param name="position">The position to check; receives the valid position found.</param>
	/// <param name="unit">The unit.</param>
	/// <param name="checkSurrounding">Do we need to check the 8 tiles around the selected one?</param>
	/// <param name="startSurroundingCheckDirection">Which direction should we start the check? (0 = north)</param>
	/// <returns>True if we found a valid position and stored it in <paramref name="position"/>.</returns>
	public bool IsPositionValidForUnit(ref Position position, BattleUnit unit, bool checkSurrounding = false, int startSurroundingCheckDirection = 0)
	{
		int unitSize = unit.Armor.Size;
		List<Position> positionsToCheck = [position];
		if (checkSurrounding)
		{
			// Look up the surrounding directions
			for (int i = 0; i < 8; i++)
			{
				positionsToCheck.Add(position + new Position(surroundingTilePositions[(startSurroundingCheckDirection + i) % 8, 0] * unitSize, surroundingTilePositions[(startSurroundingCheckDirection + i) % 8, 1] * unitSize, 0));
			}
		}

		foreach (Position pos in positionsToCheck)
		{
			bool passedCheck = true;

			for (int x = unitSize - 1; x >= 0; x--)
			{
				for (int y = unitSize - 1; y >= 0; y--)
				{
					// Make sure the location is in bounds and nothing blocks being there
					Position positionToCheck = pos + new Position(x, y, 0);
					Tile tileToCheck = _save.GetTile(positionToCheck);
					if (tileToCheck == null || (tileToCheck.Unit != null && tileToCheck.Unit != unit) ||
						tileToCheck.GetTUCost((int)O_OBJECT, unit.MovementType) == Pathfinding.INVALID_MOVE_COST ||
						(tileToCheck.GetMapData(O_OBJECT) != null && tileToCheck.GetMapData(O_OBJECT).BigWall != 0 && tileToCheck.GetMapData(O_OBJECT).BigWall <= 3))
					{
						passedCheck = false;
					}
				}
			}

			// Extra test for large units
			if (passedCheck && unitSize > 1)
			{
				_save.Pathfinding.SetUnit(unit); //TODO: remove as was required by `isBlockedDirection`
				for (int dir = 2; dir <= 4; ++dir)
				{
					if (_save.Pathfinding.IsBlockedDirection(unit, _save.GetTile(pos), dir))
					{
						passedCheck = false;
					}
				}
			}

			if (passedCheck)
			{
				position = pos;
				return true;
			}
		}

		return false;
	}
}
