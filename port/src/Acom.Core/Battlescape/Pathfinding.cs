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
// Ported from: src/Battlescape/Pathfinding.h, src/Battlescape/Pathfinding.cpp
#nullable disable

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Acom;

/// <summary>
/// A utility class that calculates the shortest path between two points on the battlescape map.
/// </summary>
public class Pathfinding
{
	private const int dir_max = 10;
	private static readonly int[] dir_x = [0, +1, +1, +1, 0, -1, -1, -1, 0, 0];
	private static readonly int[] dir_y = [-1, -1, 0, +1, +1, +1, 0, -1, 0, 0];
	private static readonly int[] dir_z = [0, 0, 0, 0, 0, 0, 0, 0, +1, -1];

	private readonly SavedBattleGame _save;
	private readonly List<PathfindingNode> _nodes;
	private readonly int _size;
	private BattleUnit _unit;
	private bool _pathPreviewed;
	private bool _strafeMove;
	private bool _ctrlUsed;
	private bool _altUsed;
	private PathfindingCost _totalTUCost;
	private readonly List<int> _path = [];

	/// <summary>Default move cost for tile that have floor with 0 cost.</summary>
	public const int DEFAULT_MOVE_COST = 4;
	/// <summary>Default move cost for changing level for fly or gravlift.</summary>
	public const int DEFAULT_MOVE_FLY_COST = 8;
	/// <summary>How much time units one move can have.</summary>
	public const int MAX_MOVE_COST = 100;
	/// <summary>Fake cost representing invalid move.</summary>
	public const int INVALID_MOVE_COST = 255;
	/// <summary>Fire penalty used in path search.</summary>
	public const int FIRE_PREVIEW_MOVE_COST = 32;

	public const int DIR_UP = 8;
	public const int DIR_DOWN = 9;
	public const int O_BIGWALL = -1;
#pragma warning disable CA2211 // C++ public static members (marker colours of the path preview)
	public static int red = 3;
	public static int green = 4;
	public static int yellow = 10;
#pragma warning restore CA2211

	/// <summary>Four tiles of a (large) unit.</summary>
	[InlineArray(4)]
	private struct TileParts
	{
		private Tile _element0;
	}

	private static readonly Position[] s_partOffsets =
	[
		new(0, 0, 0),
		new(1, 0, 0),
		new(0, 1, 0),
		new(1, 1, 0),
	];

	private static readonly Position oneTileNorth = new(0, -1, 0);
	private static readonly Position oneTileEast = new(1, 0, 0);
	private static readonly Position oneTileSouth = new(0, 1, 0);
	private static readonly Position oneTileWest = new(-1, 0, 0);

	/// <summary>Sets up a Pathfinding.</summary>
	public Pathfinding(SavedBattleGame save)
	{
		_save = save;
		_unit = null;
		_pathPreviewed = false;
		_strafeMove = false;
		_size = _save.MapSizeXYZ;
		// Initialize one node per tile
		_nodes = new List<PathfindingNode>(_size);
		for (int i = 0; i < _size; ++i)
		{
			_nodes.Add(new PathfindingNode(_save.GetTileCoords(i)));
		}
	}

	/// <summary>Gets the Node on a given position on the map.</summary>
	private PathfindingNode GetNode(Position pos) => _nodes[_save.GetTileIndex(pos)];

	private static PathfindingStep InvalidStep() => new() { cost = new PathfindingCost(INVALID_MOVE_COST, 0) };

	/// <summary>
	/// Calculates the shortest path.
	/// </summary>
	/// <param name="unit">Unit taking the path.</param>
	/// <param name="endPosition">The position we want to reach.</param>
	/// <param name="bam">Move type.</param>
	/// <param name="missileTarget">Target of the path.</param>
	/// <param name="maxTUCost">Maximum time units the path can cost.</param>
	public void Calculate(BattleUnit unit, Position endPosition, BattleActionMove bam, BattleUnit missileTarget = null, int maxTUCost = 1000)
	{
		_totalTUCost = default;
		_path.Clear();

		int size = bam != BattleActionMove.BAM_MISSILE ? unit.Armor.Size : 1;

		// i'm DONE with these out of bounds errors.
		if (endPosition.x > _save.MapSizeX - size || endPosition.y > _save.MapSizeY - size || endPosition.x < 0 || endPosition.y < 0) return;

		bool sneak = Options.sneakyAI && unit.Faction == UnitFaction.FACTION_HOSTILE;

		Position startPosition = unit.Position;
		MovementType movementType = GetMovementType(unit, missileTarget, bam);
		if (missileTarget != null && maxTUCost == -1 && bam == BattleActionMove.BAM_MISSILE) // pathfinding for missile
		{
			maxTUCost = 10000;
		}
		_unit = unit;

		Tile destinationTile = _save.GetTile(endPosition);

		// check if destination is not blocked
		if (IsBlocked(_unit, destinationTile, (int)TilePart.O_FLOOR, bam, missileTarget) || IsBlocked(_unit, destinationTile, (int)TilePart.O_OBJECT, bam, missileTarget)) return;

		// the following check avoids that the unit walks behind the stairs if we click behind the stairs to make it go up the stairs.
		// it only works if the unit is on one of the 2 tiles on the stairs, or on the tile right in front of the stairs.
		if (IsOnStairs(startPosition, endPosition))
		{
			endPosition.z++;
			destinationTile = _save.GetTile(endPosition);
		}
		while (endPosition.z != _save.MapSizeZ && destinationTile.TerrainLevel == -24)
		{
			endPosition.z++;
			destinationTile = _save.GetTile(endPosition);
		}
		// make sure we didn't just try to adjust the Z of our destination outside the map
		// this occurs in rare circumstances where an object has terrainLevel -24 on the top floor
		// and is considered passable terrain for whatever reason (usually bigwall type objects)
		if (endPosition.z == _save.MapSizeZ)
		{
			return; // Icarus is a bad role model for XCom soldiers.
		}
		if (movementType != MovementType.MT_FLY && bam != BattleActionMove.BAM_MISSILE)
		{
			// check if we have floor, else lower destination (for non flying units only, because otherwise they never reached this place)
			while (CanFallDown(destinationTile, size) && !(size == 1 && destinationTile.HasLadder))
			{
				endPosition.z--;
				destinationTile = _save.GetTile(endPosition);
			}
		}
		// check if destination is not blocked
		if (IsBlocked(_unit, destinationTile, (int)TilePart.O_FLOOR, bam, missileTarget) || IsBlocked(_unit, destinationTile, (int)TilePart.O_OBJECT, bam, missileTarget)) return;

		// Strafing move allowed only to adjacent squares on same z. "Same z" rule mainly to simplify walking render.
		_strafeMove = bam == BattleActionMove.BAM_STRAFE && (startPosition.z == endPosition.z) &&
								(Math.Abs(startPosition.x - endPosition.x) <= 1) && (Math.Abs(startPosition.y - endPosition.y) <= 1);

		if (_strafeMove)
		{
			int direction = -1;
			VectorToDirection(endPosition - startPosition, ref direction);
			if (direction == -1 || Math.Min(Math.Abs(8 + direction - _unit.Direction), Math.Min(Math.Abs(_unit.Direction - direction), Math.Abs(8 + _unit.Direction - direction))) > 2)
			{
				// Strafing backwards-ish currently unsupported, turn it off and continue.
				_strafeMove = false;
			}
			else if (GetTUCost(startPosition, direction, _unit, null, bam).cost.time == INVALID_MOVE_COST)
			{
				// we can't reach in one step
				_strafeMove = false;
			}
		}

		// look for a possible fast and accurate bresenham path and skip A*
		if (BresenhamPath(startPosition, endPosition, bam, missileTarget, sneak))
		{
			_path.Reverse(); //paths are stored in reverse order
			return;
		}
		else
		{
			AbortPath(); // if bresenham failed, we shouldn't keep the path it was attempting, in case A* fails too.
		}
		// Now try through A*.
		if (!AStarPath(startPosition, endPosition, bam, missileTarget, sneak, maxTUCost))
		{
			AbortPath();
		}
	}

	/// <summary>
	/// Calculates the shortest path using a simple A-Star algorithm.
	/// The unit information and movement type must have already been set.
	/// The path information is set only if a valid path is found.
	/// </summary>
	/// <returns>True if a path exists, false otherwise.</returns>
	private bool AStarPath(Position startPosition, Position endPosition, BattleActionMove bam, BattleUnit missileTarget, bool sneak = false, int maxTUCost = 1000)
	{
		// reset every node, so we have to check them all
		foreach (PathfindingNode pn in _nodes)
		{
			pn.Reset();
		}

		// start position is the first one in our "open" list
		PathfindingNode start = GetNode(startPosition);
		start.Connect(default, null, 0, endPosition);
		PathfindingOpenSet openList = new();
		openList.Push(start);
		bool missile = bam == BattleActionMove.BAM_MISSILE;
		// if the open list is empty, we've reached the end
		while (!openList.Empty())
		{
			PathfindingNode currentNode = openList.Pop();
			Position currentPos = currentNode.Position;
			currentNode.SetChecked();
			if (currentPos == endPosition) // We found our target.
			{
				_path.Clear();
				PathfindingNode pf = currentNode;
				while (pf.PrevNode != null)
				{
					_path.Add(pf.PrevDir);
					pf = pf.PrevNode;
				}
				return true;
			}

			// Try all reachable neighbours.
			for (int direction = 0; direction < 10; direction++)
			{
				PathfindingStep r = GetTUCost(currentPos, direction, _unit, missileTarget, bam);
				if (r.cost.time == INVALID_MOVE_COST) // Skip unreachable / blocked
					continue;

				Position nextPos = r.pos;
				if (sneak && _save.GetTile(nextPos).Visible != 0) r.cost.time *= 2; // avoid being seen
				PathfindingNode nextNode = GetNode(nextPos);
				if (nextNode.IsChecked) // Our algorithm means this node is already at minimum cost.
					continue;
				_totalTUCost = currentNode.GetTUCost(missile) + r.cost + r.penalty;
				// If this node is unvisited or has only been visited from inferior paths...
				if ((!nextNode.InOpenSet() || nextNode.GetTUCost(missile).time > _totalTUCost.time) && _totalTUCost.time <= maxTUCost)
				{
					nextNode.Connect(_totalTUCost, currentNode, direction, endPosition);
					openList.Push(nextNode);
				}
			}
		}
		// Unable to reach the target
		return false;
	}

	/// <summary>Minimum climbing cost over the ladders of a tile (lambda calcMin in C++).</summary>
	private static void CalcMinClimbCost(ref int minCost, Tile tile, bool ladder, TilePart p, MovementType movementType)
	{
		if (ladder)
		{
			int value = tile.GetTUCost((int)p, movementType);
			minCost = Math.Min(minCost, value != 0 ? value : DEFAULT_MOVE_FLY_COST);
		}
	}

	/// <summary>
	/// Gets the TU cost to move from 1 tile to the other (ONE STEP ONLY).
	/// But also updates the endPosition, because it is possible
	/// the unit goes upstairs or falls down while walking.
	/// </summary>
	/// <param name="startPosition">The position to start from.</param>
	/// <param name="direction">The direction we are facing.</param>
	/// <param name="unit">The unit moving.</param>
	/// <param name="missileTarget">The target unit used for BAM_MISSILE.</param>
	/// <param name="bam">What move type is required (one special case is BAM_MISSILE)?</param>
	/// <returns>TU cost or 255 if movement is impossible.</returns>
	public PathfindingStep GetTUCost(Position startPosition, int direction, BattleUnit unit, BattleUnit missileTarget, BattleActionMove bam)
	{
		Position pos = default;
		DirectionToVector(direction, ref pos);
		pos += startPosition;

		MovementType movementType = GetMovementType(unit, missileTarget, bam);
		Armor armor = unit.Armor;
		int size = armor.Size - 1;
		int numberOfParts = armor.TotalSize;
		int maskOfPartsGoingUp = 0x0;
		int maskOfPartsHoleUp = 0x0;
		int maskOfPartsGoingDown = 0x0;
		int maskOfPartsFalling = 0x0;
		int maskOfPartsFlying = 0x0;
		int maskOfPartsGround = 0x0;
		int maskOfPartsClimb = 0x0;
		int maskArmor = size != 0 ? 0xF : 0x1;

		TileParts startTile = default;
		TileParts aboveStart = default;
		TileParts belowStart = default;
		TileParts destinationTile = default;
		TileParts aboveDestination = default;
		TileParts belowDestination = default;

		// init variables
		for (int i = 0; i < numberOfParts; ++i)
		{
			Tile st = _save.GetTile(startPosition + s_partOffsets[i]);
			Tile dt = _save.GetTile(pos + s_partOffsets[i]);
			if (st == null || dt == null)
			{
				return InvalidStep();
			}
			startTile[i] = st;
			aboveStart[i] = _save.GetAboveTile(st);
			belowStart[i] = _save.GetBelowTile(st);
			destinationTile[i] = dt;
			aboveDestination[i] = _save.GetAboveTile(dt);
			belowDestination[i] = _save.GetBelowTile(dt);
		}

		// check move up or down
		for (int i = 0; i < numberOfParts; ++i)
		{
			int maskCurrentPart = 1 << i;
			bool checkClimbing = (i == 0) && (numberOfParts == 1) && movementType != MovementType.MT_FLY;

			if (direction < DIR_UP && startTile[i].TerrainLevel > -16)
			{
				// check if we can go this way
				if (IsBlockedDirection(unit, startTile[i], direction, bam, missileTarget))
					return InvalidStep();
				if (startTile[i].TerrainLevel - destinationTile[i].TerrainLevel > 8)
					return InvalidStep();
			}

			// if we are on a stairs try to go up a level
			if (direction < DIR_UP && startTile[i].TerrainLevel <= -16 && aboveDestination[i] != null && !aboveDestination[i].HasNoFloor(_save))
			{
				maskOfPartsGoingUp |= maskCurrentPart;
			}
			else if (direction < DIR_UP && movementType != MovementType.MT_FLY && belowDestination[i] != null && CanFallDown(destinationTile[i]) && belowDestination[i].TerrainLevel <= -12)
			{
				maskOfPartsGoingDown |= maskCurrentPart;
			}
			else if (bam != BattleActionMove.BAM_MISSILE && movementType == MovementType.MT_FLY)
			{
				// 2 or more voxels poking into this tile = no go
				BattleUnit overlaping = destinationTile[i].GetOverlappingUnit(_save, TileUnitOverlapping.TUO_IGNORE_SMALL);
				if (overlaping != null && overlaping != unit)
				{
					return InvalidStep();
				}
			}

			if (aboveStart[i] != null && aboveStart[i].HasNoFloor(_save))
			{
				maskOfPartsHoleUp |= maskCurrentPart;
			}
			// check if we have floor, else fall down
			if (movementType != MovementType.MT_FLY && CanFallDown(startTile[i]))
			{
				if (!checkClimbing || (!startTile[i].HasLadder && !(direction < DIR_UP && belowStart[i] != null && belowStart[i].HasLadder && !CanFallDown(destinationTile[i]))))
				{
					maskOfPartsFalling |= maskCurrentPart;
				}
			}
			if (movementType != MovementType.MT_FLY && checkClimbing && direction >= DIR_UP && startTile[i].HasLadder)
			{
				//TODO: need check interaction with GravLift and other corner cases
				maskOfPartsClimb |= maskCurrentPart;
			}
			if (movementType == MovementType.MT_FLY && (CanFallDown(startTile[i]) || CanFallDown(destinationTile[i])))
			{
				maskOfPartsFlying |= maskCurrentPart;
			}
			if (direction != DIR_DOWN && !CanFallDown(destinationTile[i]))
			{
				maskOfPartsGround |= maskCurrentPart;
			}
		}

		bool triedStairs = maskOfPartsGoingUp != 0 && ((maskOfPartsGoingUp | maskOfPartsHoleUp) == maskArmor);
		bool triedStairsDown = maskOfPartsGround == 0 && ((maskOfPartsGoingDown | maskOfPartsFalling) == maskArmor);
		bool fallingDown = maskOfPartsFalling == maskArmor;
		bool flying = maskOfPartsFlying == maskArmor;
		bool climb = (maskOfPartsClimb == maskArmor) && (bam != BattleActionMove.BAM_MISSILE);

		if (movementType != MovementType.MT_FLY && fallingDown)
		{
			if (direction != DIR_DOWN)
			{
				return InvalidStep(); //cannot walk on air
			}
		}

		for (int i = 0; i < numberOfParts; ++i)
		{
			if (triedStairs)
			{
				destinationTile[i] = aboveDestination[i];
			}
			else if (direction != DIR_DOWN && triedStairsDown)
			{
				destinationTile[i] = belowDestination[i];
			}

			// check if the destination tile can be walked over
			if (IsBlocked(unit, destinationTile[i], (int)TilePart.O_FLOOR, bam, missileTarget) || IsBlocked(unit, destinationTile[i], (int)TilePart.O_OBJECT, bam, missileTarget))
			{
				return InvalidStep();
			}
		}

		// don't let tanks phase through doors.
		if (size != 0)
		{
			Tile t = destinationTile[3];
			if (t.IsDoor(TilePart.O_NORTHWALL) ||
				t.IsDoor(TilePart.O_WESTWALL))
			{
				return InvalidStep();
			}
		}

		// pre-calculate fire penalty (to make it consistent for 2x2 units)
		int firePenaltyCost = 0;
		if (unit.Faction != UnitFaction.FACTION_PLAYER &&
			unit.AvoidsFire())
		{
			for (int i = 0; i < numberOfParts; ++i)
			{
				if (destinationTile[i].Fire > 0)
				{
					firePenaltyCost = FIRE_PREVIEW_MOVE_COST; // try to find a better path, but don't exclude this path entirely.
				}
			}
		}


		// calculate cost and some final checks
		int totalCost = 0;

		for (int i = 0; i < numberOfParts; ++i)
		{
			int cost = 0;
			bool upperLevel = destinationTile[i].Position.z > startTile[i].Position.z;
			bool sameLevel = destinationTile[i].Position.z == startTile[i].Position.z;
			if (direction < DIR_UP && sameLevel)
			{
				// check if we can go this way
				if (IsBlockedDirection(unit, startTile[i], direction, bam, missileTarget))
					return InvalidStep();
				if (startTile[i].TerrainLevel - destinationTile[i].TerrainLevel > 8)
					return InvalidStep();
			}
			else if (direction >= DIR_UP && !triedStairsDown)
			{
				// check if we can go up or down through gravlift or fly
				if (ValidateUpDown(unit, startTile[i].Position, direction, bam == BattleActionMove.BAM_MISSILE))
				{
					if (climb)
					{
						int minCost = INVALID_MOVE_COST;

						CalcMinClimbCost(ref minCost, startTile[i], startTile[i].HasLadderOnObject, TilePart.O_OBJECT, movementType);
						CalcMinClimbCost(ref minCost, startTile[i], startTile[i].HasLadderOnNorthWall, TilePart.O_NORTHWALL, movementType);
						CalcMinClimbCost(ref minCost, startTile[i], startTile[i].HasLadderOnWestWall, TilePart.O_WESTWALL, movementType);

						if (minCost >= INVALID_MOVE_COST)
						{
							return InvalidStep();
						}
						cost = minCost;
					}
					else
					{
						cost = DEFAULT_MOVE_FLY_COST; // vertical movement by flying suit or grav lift
					}
				}
				else
				{
					return InvalidStep();
				}
			}
			if (upperLevel)
			{
				startTile[i] = _save.GetAboveTile(startTile[i]);

				if (direction < DIR_UP)
				{
					// check if we can go this way
					if (IsBlockedDirection(unit, startTile[i], direction, bam, missileTarget))
						return InvalidStep();
					if (startTile[i].TerrainLevel - destinationTile[i].TerrainLevel > 8)
						return InvalidStep();
				}
			}

			int wallCounter = 0;
			int wallTmp;
			int wallcost = 0; // walking through rubble walls, but don't charge for walking diagonally through doors (which is impossible),
							// they're a special case unto themselves, if we can walk past them diagonally, it means we can go around,
							// as there is no wall blocking us.
			if ((direction == 0 || direction == 7 || direction == 1) && !startTile[i].HasLadderOnNorthWall)
			{
				wallTmp = startTile[i].GetTUCost((int)TilePart.O_NORTHWALL, movementType);
				if (wallTmp > 0)
				{
					wallcost += wallTmp;
					wallCounter += 1;
				}
			}
			if (!triedStairsDown && (direction == 2 || direction == 1 || direction == 3) && !destinationTile[i].HasLadderOnWestWall)
			{
				wallTmp = destinationTile[i].GetTUCost((int)TilePart.O_WESTWALL, movementType);
				if (wallTmp > 0)
				{
					wallcost += wallTmp;
					wallCounter += 1;
				}
			}
			if (!triedStairsDown && (direction == 4 || direction == 3 || direction == 5) && !destinationTile[i].HasLadderOnNorthWall)
			{
				wallTmp = destinationTile[i].GetTUCost((int)TilePart.O_NORTHWALL, movementType);
				if (wallTmp > 0)
				{
					wallcost += wallTmp;
					wallCounter += 1;
				}
			}
			if ((direction == 6 || direction == 5 || direction == 7) && !startTile[i].HasLadderOnWestWall)
			{
				wallTmp = startTile[i].GetTUCost((int)TilePart.O_WESTWALL, movementType);
				if (wallTmp > 0)
				{
					wallcost += wallTmp;
					wallCounter += 1;
				}
			}

			// "average" cost: https://openxcom.org/forum/index.php?topic=12589.0
			if (wallCounter > 0)
			{
				wallcost /= wallCounter;
			}

			// for backward compatiblity (100 + 100 + 100 > 255) or for (255 + 10 > 255)
			if (wallcost >= INVALID_MOVE_COST)
			{
				return InvalidStep();
			}

			// if we don't want to fall down and there is no floor, we can't know the TUs so it's default to 4
			if (direction < DIR_UP && !triedStairsDown && destinationTile[i].HasNoFloor(null))
			{
				cost = DEFAULT_MOVE_COST;
			}

			// calculate the cost by adding floor walk cost and object walk cost
			if (direction < DIR_UP)
			{
				cost += destinationTile[i].GetTUCost((int)TilePart.O_FLOOR, movementType);
				if (!triedStairsDown && !triedStairs && destinationTile[i].GetMapData(TilePart.O_OBJECT) != null && !destinationTile[i].HasLadderOnObject)
				{
					cost += destinationTile[i].GetTUCost((int)TilePart.O_OBJECT, movementType);
				}
				if (cost == 0)
				{
					// some mods have broken tiles with 0 move cost, as it could be problematic for the engine, we override it to default cost.
					cost = DEFAULT_MOVE_COST;
				}
				// climbing up a level costs one extra
				if (upperLevel)
				{
					cost++;
				}
			}

			// diagonal walking (uneven directions) costs 50% more tu's
			if (direction < DIR_UP && (direction & 1) != 0)
			{
				cost = (int)((double)cost * 1.5);
			}

			cost += wallcost;

			// TFTD thing: underwater tiles on fire or filled with smoke cost 2 TUs more for whatever reason.
			if (_save.Depth > 0 && (destinationTile[i].Fire > 0 || destinationTile[i].Smoke > 0))
			{
				cost += 2;
			}

			if (missileTarget != null && destinationTile[i].Unit != null)
			{
				BattleUnit unitHere = destinationTile[i].Unit;
				if (unitHere != missileTarget && !unitHere.IsOut)
				{
					if (unitHere.Faction == unit.Faction)
					{
						return InvalidStep(); // consider any tile occupied by a friendly as being blocked
					}
					else if (unit.UnitRules != null && unitHere.GetTurnsSinceSpottedByFaction(unit.Faction) <= unit.UnitRules.Intelligence)
					{
						return InvalidStep(); // consider any tile occupied by a known unit that isn't our target as being blocked
					}
				}
			}

			// Strafing costs +1 for forwards-ish or sidewards, propose +2 for backwards-ish directions
			// Maybe if flying then it makes no difference?
			if (_strafeMove && bam == BattleActionMove.BAM_STRAFE)
			{
				if (unit.Direction != direction)
				{
					cost += 1;
				}
			}

			// cap move cost to given limit
			cost = Math.Min(cost, +MAX_MOVE_COST);

			totalCost += cost;
		}

		// because unit move up or down we adjust final position
		if (triedStairs)
		{
			pos.z++;
		}
		else if (direction != DIR_DOWN && triedStairsDown)
		{
			pos.z--;
		}

		// for bigger sized units, check the path between parts in an X shape at the end position
		if (size != 0)
		{
			totalCost /= numberOfParts;
			Tile originTile = _save.GetTile(pos + new Position(1, 1, 0));
			Tile finalTile = _save.GetTile(pos);
			int tmpDirection = 7;
			if (IsBlockedDirection(unit, originTile, tmpDirection, bam, missileTarget))
				return InvalidStep();
			if (!triedStairsDown && Math.Abs(originTile.TerrainLevel - finalTile.TerrainLevel) > 10)
				return InvalidStep();
			originTile = _save.GetTile(pos + new Position(1, 0, 0));
			finalTile = _save.GetTile(pos + new Position(0, 1, 0));
			tmpDirection = 5;
			if (IsBlockedDirection(unit, originTile, tmpDirection, bam, missileTarget))
				return InvalidStep();
			if (!triedStairsDown && Math.Abs(originTile.TerrainLevel - finalTile.TerrainLevel) > 10)
				return InvalidStep();
		}


		if (bam == BattleActionMove.BAM_MISSILE)
		{
			return new PathfindingStep { pos = pos };
		}

		if (direction == DIR_DOWN && fallingDown)
		{
			return new PathfindingStep { penalty = new PathfindingCost(firePenaltyCost, 0), pos = pos };
		}

		const int costDiv = 100 * 100 * 100;
		ArmorMoveCost moveCost = new() { TimePercent = totalCost, EnergyPercent = totalCost };

		moveCost *= unit.MoveCostBase;

		if (climb)
		{
			moveCost *= unit.MoveCostBaseClimb;
		}
		else if (flying)
		{
			moveCost *= unit.MoveCostBaseFly;
		}
		else
		{
			moveCost *= unit.MoveCostBaseNormal;
		}

		if (direction >= DIR_UP)
		{
			if (climb)
			{
				if (direction == DIR_UP)
				{
					moveCost *= armor.MoveCostClimbUp;
				}
				else
				{
					moveCost *= armor.MoveCostClimbDown;
				}
			}
			else if (flying)
			{
				if (direction == DIR_UP)
				{
					moveCost *= armor.MoveCostFlyUp;
				}
				else
				{
					moveCost *= armor.MoveCostFlyDown;
				}
			}
			else
			{
				//unit use GravLift
				moveCost *= armor.MoveCostGravLift;
			}
		}
		else if (bam == BattleActionMove.BAM_NORMAL)
		{
			if (flying)
			{
				moveCost *= armor.MoveCostFlyWalk;
			}
			else
			{
				moveCost *= armor.MoveCostWalk;
			}
		}
		else if (bam == BattleActionMove.BAM_RUN)
		{
			if (flying)
			{
				moveCost *= armor.MoveCostFlyRun;
			}
			else
			{
				moveCost *= armor.MoveCostRun;
			}
		}
		else if (bam == BattleActionMove.BAM_STRAFE)
		{
			if (flying)
			{
				moveCost *= armor.MoveCostFlyStrafe;
			}
			else
			{
				moveCost *= armor.MoveCostStrafe;
			}
		}
		else if (bam == BattleActionMove.BAM_SNEAK)
		{
			//no flight
			moveCost *= armor.MoveCostSneak;
		}
		else
		{
			Debug.Fail("Unreachable code in pathfinding cost");
		}

		int timeCost = Mod.EXTENDED_MOVEMENT_COST_ROUNDING == 0 ? (moveCost.TimePercent) / costDiv :
						Mod.EXTENDED_MOVEMENT_COST_ROUNDING == 1 ? (moveCost.TimePercent + (costDiv / 2)) / costDiv :
																	(moveCost.TimePercent - 1 + (costDiv / 2)) / costDiv;

		int energyCost = Mod.EXTENDED_MOVEMENT_COST_ROUNDING == 0 ? (moveCost.EnergyPercent) / costDiv :
						Mod.EXTENDED_MOVEMENT_COST_ROUNDING == 1 ? (moveCost.EnergyPercent + (costDiv / 2)) / costDiv :
																	(moveCost.EnergyPercent - 1 + (costDiv / 2)) / costDiv;

		return new PathfindingStep
		{
			cost = new PathfindingCost(Math.Clamp(timeCost, 1, INVALID_MOVE_COST - 1), Math.Clamp(energyCost, 0, INVALID_MOVE_COST)),
			penalty = new PathfindingCost(firePenaltyCost, 0),
			pos = pos,
		};
	}

	/// <summary>
	/// Converts direction to a vector. Direction starts north = 0 and goes clockwise.
	/// </summary>
	/// <param name="direction">Source direction.</param>
	/// <param name="vector">Position (which acts as a vector).</param>
	public static void DirectionToVector(int direction, ref Position vector)
	{
		vector.x = (short)dir_x[direction];
		vector.y = (short)dir_y[direction];
		vector.z = (short)dir_z[direction];
	}

	/// <summary>
	/// Converts a vector to a direction. Direction starts north = 0 and goes clockwise.
	/// </summary>
	/// <param name="vector">Position (which acts as a vector).</param>
	/// <param name="dir">Resulting direction.</param>
	public static void VectorToDirection(Position vector, ref int dir)
	{
		dir = VectorToDirection(vector);
	}

	/// <summary>
	/// Converts a vector to a direction. Direction starts north = 0 and goes clockwise.
	/// </summary>
	/// <param name="vector">Position (which acts as a vector).</param>
	/// <returns>Resulting direction, -1 if there is none (the z component is ignored).</returns>
	public static int VectorToDirection(Position vector)
	{
		for (int i = 0; i < 8; ++i)
		{
			if (dir_x[i] == vector.x && dir_y[i] == vector.y)
			{
				return i;
			}
		}
		return -1;
	}

	/// <summary>Checks whether a path is ready and gives the first direction (-1 if it's the end of the path).</summary>
	public int StartDirection
	{
		get
		{
			if (_path.Count == 0) return -1;
			return _path[^1];
		}
	}

	/// <summary>
	/// Dequeues the next path direction. Ie returns the direction and removes it from queue.
	/// </summary>
	/// <returns>Direction where the unit needs to go next, -1 if it's the end of the path.</returns>
	public int DequeuePath()
	{
		if (_path.Count == 0) return -1;
		int last_element = _path[^1];
		_path.RemoveAt(_path.Count - 1);
		return last_element;
	}

	/// <summary>Aborts the current path. Clears the path vector.</summary>
	public void AbortPath()
	{
		_totalTUCost = default;
		_path.Clear();
	}

	/// <summary>
	/// Gets movement type of unit or movement of missile.
	/// </summary>
	/// <returns><c>MT_FLY</c> if we have <paramref name="missileTarget"/>, otherwise the <paramref name="unit"/> move type.</returns>
	private static MovementType GetMovementType(BattleUnit unit, BattleUnit missileTarget, BattleActionMove bam)
	{
		if (missileTarget != null)
		{
			return MovementType.MT_FLY;
		}
		else
		{
			if (bam == BattleActionMove.BAM_SNEAK)
			{
				return MovementType.MT_WALK;
			}
			else
			{
				return unit.MovementType;
			}
		}
	}

	private static bool IsBigWall(MapData obj, Pathfinding.bigWallTypes a, Pathfinding.bigWallTypes b) =>
		obj != null && (obj.BigWall == (int)a || obj.BigWall == (int)b);

	/// <summary>
	/// Determines whether a certain part of a tile blocks movement.
	/// </summary>
	/// <param name="unit">Unit that moves.</param>
	/// <param name="tile">Specified tile, can be null.</param>
	/// <param name="part">Part of the tile.</param>
	/// <param name="bam">Move type.</param>
	/// <param name="missileTarget">Target for a missile.</param>
	/// <param name="bigWallExclusion">Big wall type that does not block.</param>
	/// <returns>True if the movement is blocked.</returns>
	private bool IsBlocked(BattleUnit unit, Tile tile, int part, BattleActionMove bam, BattleUnit missileTarget, int bigWallExclusion = -1)
	{
		if (tile == null) return true; // probably outside the map here

		MovementType movementType = GetMovementType(unit, missileTarget, bam);

		if (part == O_BIGWALL)
		{
			MapData obj = tile.GetMapData(TilePart.O_OBJECT);
			if (obj != null &&
				obj.BigWall != 0 &&
				obj.BigWall <= (int)bigWallTypes.BIGWALLNWSE &&
				obj.BigWall != bigWallExclusion)
				return true; // blocking part
			else
				return false;
		}
		if (part == (int)TilePart.O_WESTWALL)
		{
			if (IsBigWall(tile.GetMapData(TilePart.O_OBJECT), bigWallTypes.BIGWALLWEST, bigWallTypes.BIGWALLWESTANDNORTH))
				return true; // blocking part
			Tile tileWest = _save.GetTile(tile.Position + new Position(-1, 0, 0));
			if (tileWest == null) return true; // do not look outside of map
			if (IsBigWall(tileWest.GetMapData(TilePart.O_OBJECT), bigWallTypes.BIGWALLEAST, bigWallTypes.BIGWALLEASTANDSOUTH))
				return true; // blocking part
		}
		if (part == (int)TilePart.O_NORTHWALL)
		{
			if (IsBigWall(tile.GetMapData(TilePart.O_OBJECT), bigWallTypes.BIGWALLNORTH, bigWallTypes.BIGWALLWESTANDNORTH))
				return true; // blocking part
			Tile tileNorth = _save.GetTile(tile.Position + new Position(0, -1, 0));
			if (tileNorth == null)
				return true; // do not look outside of map
			if (IsBigWall(tileNorth.GetMapData(TilePart.O_OBJECT), bigWallTypes.BIGWALLSOUTH, bigWallTypes.BIGWALLEASTANDSOUTH))
				return true; // blocking part
		}
		if (part == (int)TilePart.O_FLOOR)
		{
			if (tile.Unit != null)
			{
				BattleUnit u = tile.Unit;
				if (u == unit || u == missileTarget || u.IsOut)
					return false;
				if (unit != null)
				{
					if (unit.Faction == UnitFaction.FACTION_PLAYER && u.Visible)
						return true; // player know all visible units
					if (unit.Faction == u.Faction)
						return true;
					if (unit.Faction == UnitFaction.FACTION_HOSTILE &&
						unit.UnitsSpottedThisTurn.Contains(u))
						return true;
				}
			}
			else if (tile.HasNoFloor(null) && movementType != MovementType.MT_FLY) // this whole section is devoted to making large units not take part in any kind of falling behaviour
			{
				Position pos = tile.Position;
				while (pos.z >= 0)
				{
					Tile t = _save.GetTile(pos);
					BattleUnit u = t.Unit;

					if (u != null && u != unit)
					{
						// don't let large units fall on other units
						if (unit != null && unit.IsBigUnit)
						{
							return true;
						}
						// don't let any units fall on large units
						if (u != unit && u != missileTarget && !u.IsOut && u.IsBigUnit)
						{
							return true;
						}
					}
					// not gonna fall any further, so we can stop checking.
					if (!t.HasNoFloor(null))
					{
						break;
					}
					pos.z--;
				}
			}
		}
		// missiles can't pathfind through closed doors.
		{
			TilePart tp = (TilePart)part;
			if (missileTarget != null && tile.GetMapData(tp) != null &&
				(tile.IsDoor(tp) ||
				(tile.IsUfoDoor(tp) &&
				!tile.IsUfoDoorOpen(tp))))
			{
				return true;
			}
		}
		if (tile.GetTUCost(part, movementType) == INVALID_MOVE_COST) return true; // blocking part
		return false;
	}

	/// <summary>
	/// Determines whether going from one tile to another blocks movement.
	/// </summary>
	/// <param name="unit">Unit that move.</param>
	/// <param name="startTile">The tile to start from.</param>
	/// <param name="direction">The direction we are facing.</param>
	/// <param name="bam">Move type.</param>
	/// <param name="missileTarget">Target for a missile.</param>
	/// <returns>True if the movement is blocked.</returns>
	private bool IsBlockedDirection(BattleUnit unit, Tile startTile, int direction, BattleActionMove bam, BattleUnit missileTarget)
	{
		// check if the difference in height between start and destination is not too high
		// so we can not jump to the highest part of the stairs from the floor
		// stairs terrainlevel goes typically -8 -16 (2 steps) or -4 -12 -20 (3 steps)
		// this "maximum jump height" is therefore set to 8

		Position currentPosition = startTile.Position;
		const int O_WESTWALL = (int)TilePart.O_WESTWALL;
		const int O_NORTHWALL = (int)TilePart.O_NORTHWALL;
		const int BIGWALLNESW = (int)bigWallTypes.BIGWALLNESW;
		const int BIGWALLNWSE = (int)bigWallTypes.BIGWALLNWSE;

		switch (direction)
		{
		case 0: // north
			if (IsBlocked(unit, startTile, O_NORTHWALL, bam, missileTarget)) return true;
			break;
		case 1: // north-east
			if (IsBlocked(unit, startTile, O_NORTHWALL, bam, missileTarget)) return true;
			if (IsBlocked(unit, _save.GetTile(currentPosition + oneTileNorth + oneTileEast), O_WESTWALL, bam, missileTarget)) return true;
			if (IsBlocked(unit, _save.GetTile(currentPosition + oneTileEast), O_WESTWALL, bam, missileTarget)) return true;
			if (IsBlocked(unit, _save.GetTile(currentPosition + oneTileEast), O_NORTHWALL, bam, missileTarget)) return true;
			if (IsBlocked(unit, _save.GetTile(currentPosition + oneTileEast), O_BIGWALL, bam, missileTarget, BIGWALLNESW)) return true;
			if (IsBlocked(unit, _save.GetTile(currentPosition + oneTileNorth), O_BIGWALL, bam, missileTarget, BIGWALLNESW)) return true;
			break;
		case 2: // east
			if (IsBlocked(unit, _save.GetTile(currentPosition + oneTileEast), O_WESTWALL, bam, missileTarget)) return true;
			break;
		case 3: // south-east
			if (IsBlocked(unit, _save.GetTile(currentPosition + oneTileEast), O_WESTWALL, bam, missileTarget)) return true;
			if (IsBlocked(unit, _save.GetTile(currentPosition + oneTileSouth), O_NORTHWALL, bam, missileTarget)) return true;
			if (IsBlocked(unit, _save.GetTile(currentPosition + oneTileSouth + oneTileEast), O_NORTHWALL, bam, missileTarget)) return true;
			if (IsBlocked(unit, _save.GetTile(currentPosition + oneTileSouth + oneTileEast), O_WESTWALL, bam, missileTarget)) return true;
			if (IsBlocked(unit, _save.GetTile(currentPosition + oneTileEast), O_BIGWALL, bam, missileTarget, BIGWALLNWSE)) return true;
			if (IsBlocked(unit, _save.GetTile(currentPosition + oneTileSouth), O_BIGWALL, bam, missileTarget, BIGWALLNWSE)) return true;
			break;
		case 4: // south
			if (IsBlocked(unit, _save.GetTile(currentPosition + oneTileSouth), O_NORTHWALL, bam, missileTarget)) return true;
			break;
		case 5: // south-west
			if (IsBlocked(unit, startTile, O_WESTWALL, bam, missileTarget)) return true;
			if (IsBlocked(unit, _save.GetTile(currentPosition + oneTileSouth), O_WESTWALL, bam, missileTarget)) return true;
			if (IsBlocked(unit, _save.GetTile(currentPosition + oneTileSouth), O_NORTHWALL, bam, missileTarget)) return true;
			if (IsBlocked(unit, _save.GetTile(currentPosition + oneTileSouth), O_BIGWALL, bam, missileTarget, BIGWALLNESW)) return true;
			if (IsBlocked(unit, _save.GetTile(currentPosition + oneTileWest), O_BIGWALL, bam, missileTarget, BIGWALLNESW)) return true;
			if (IsBlocked(unit, _save.GetTile(currentPosition + oneTileSouth + oneTileWest), O_NORTHWALL, bam, missileTarget)) return true;
			break;
		case 6: // west
			if (IsBlocked(unit, startTile, O_WESTWALL, bam, missileTarget)) return true;
			break;
		case 7: // north-west
			if (IsBlocked(unit, startTile, O_WESTWALL, bam, missileTarget)) return true;
			if (IsBlocked(unit, startTile, O_NORTHWALL, bam, missileTarget)) return true;
			if (IsBlocked(unit, _save.GetTile(currentPosition + oneTileWest), O_NORTHWALL, bam, missileTarget)) return true;
			if (IsBlocked(unit, _save.GetTile(currentPosition + oneTileNorth), O_WESTWALL, bam, missileTarget)) return true;
			if (IsBlocked(unit, _save.GetTile(currentPosition + oneTileNorth), O_BIGWALL, bam, missileTarget, BIGWALLNWSE)) return true;
			if (IsBlocked(unit, _save.GetTile(currentPosition + oneTileWest), O_BIGWALL, bam, missileTarget, BIGWALLNWSE)) return true;
			break;
		}

		return false;
	}

	/// <summary>
	/// Determines whether or not movement between start tile and end tile is possible in the direction.
	/// </summary>
	/// <param name="unit">Unit that move.</param>
	/// <param name="startTile">The tile to start from.</param>
	/// <param name="direction">The direction we are facing.</param>
	public bool IsBlockedDirection(BattleUnit unit, Tile startTile, int direction)
	{
		return IsBlockedDirection(unit, startTile, direction, BattleActionMove.BAM_NORMAL, null);
	}

	/// <summary>
	/// Determines whether a unit can fall down from this tile.
	/// We can fall down here, if the tile does not exist, the tile has no floor
	/// the current position is higher than 0, if there is no unit standing below us.
	/// </summary>
	private bool CanFallDown(Tile here)
	{
		if (here.Position.z == 0)
			return false;

		return here.HasNoFloor(_save);
	}

	/// <summary>
	/// Determines whether a unit can fall down from this tile.
	/// We can fall down here, if the tile does not exist, the tile has no floor
	/// the current position is higher than 0, if there is no unit standing below us.
	/// </summary>
	/// <param name="here">The current tile.</param>
	/// <param name="size">The size of the unit.</param>
	private bool CanFallDown(Tile here, int size)
	{
		for (int x = 0; x != size; ++x)
		{
			for (int y = 0; y != size; ++y)
			{
				Position checkPos = here.Position + new Position(x, y, 0);
				Tile checkTile = _save.GetTile(checkPos);
				if (!CanFallDown(checkTile))
					return false;
			}
		}

		return true;
	}

	/// <summary>Terrain level of the tile at a position, or null outside of the map.</summary>
	private int? TerrainLevelAt(Position pos) => _save.GetTile(pos)?.TerrainLevel;

	/// <summary>
	/// Determines whether the unit is going up a stairs.
	/// </summary>
	/// <param name="startPosition">The position to start from.</param>
	/// <param name="endPosition">The position we wanna reach.</param>
	/// <returns>True if the unit is going up a stairs.</returns>
	public bool IsOnStairs(Position startPosition, Position endPosition)
	{
		//condition 1 : endposition has to the south a terrainlevel -16 object (upper part of the stairs)
		if (TerrainLevelAt(endPosition + new Position(0, 1, 0)) == -16)
		{
			// condition 2 : one position further to the south there has to be a terrainlevel -8 object (lower part of the stairs)
			int? level = TerrainLevelAt(endPosition + new Position(0, 2, 0));
			if (level != null && level != -8)
			{
				return false;
			}

			// condition 3 : the start position has to be on either of the 3 tiles to the south of the endposition
			if (startPosition == endPosition + new Position(0, 1, 0) || startPosition == endPosition + new Position(0, 2, 0) || startPosition == endPosition + new Position(0, 3, 0))
			{
				return true;
			}
		}

		// same for the east-west oriented stairs.
		if (TerrainLevelAt(endPosition + new Position(1, 0, 0)) == -16)
		{
			int? level = TerrainLevelAt(endPosition + new Position(2, 0, 0));
			if (level != null && level != -8)
			{
				return false;
			}
			if (startPosition == endPosition + new Position(1, 0, 0) || startPosition == endPosition + new Position(2, 0, 0) || startPosition == endPosition + new Position(3, 0, 0))
			{
				return true;
			}
		}

		//TFTD stairs 1 : endposition has to the south a terrainlevel -18 object (upper part of the stairs)
		if (TerrainLevelAt(endPosition + new Position(0, 1, 0)) == -18)
		{
			// condition 2 : one position further to the south there has to be a terrainlevel -8 object (lower part of the stairs)
			int? level = TerrainLevelAt(endPosition + new Position(0, 2, 0));
			if (level != null && level != -12)
			{
				return false;
			}

			// condition 3 : the start position has to be on either of the 3 tiles to the south of the endposition
			if (startPosition == endPosition + new Position(0, 1, 0) || startPosition == endPosition + new Position(0, 2, 0) || startPosition == endPosition + new Position(0, 3, 0))
			{
				return true;
			}
		}

		// same for the east-west oriented stairs.
		if (TerrainLevelAt(endPosition + new Position(1, 0, 0)) == -18)
		{
			int? level = TerrainLevelAt(endPosition + new Position(2, 0, 0));
			if (level != null && level != -12)
			{
				return false;
			}
			if (startPosition == endPosition + new Position(1, 0, 0) || startPosition == endPosition + new Position(2, 0, 0) || startPosition == endPosition + new Position(3, 0, 0))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Checks, for the up/down button, if the movement is valid. Either there is a grav lift or the unit can fly and there are no obstructions.
	/// </summary>
	/// <param name="bu">Pointer to unit.</param>
	/// <param name="startPosition">Unit starting position.</param>
	/// <param name="direction">Up or Down</param>
	/// <param name="missile">Is this a missile?</param>
	/// <returns>Whether it's valid.</returns>
	public bool ValidateUpDown(BattleUnit bu, Position startPosition, int direction, bool missile = false)
	{
		Position endPosition = default;
		DirectionToVector(direction, ref endPosition);
		endPosition += startPosition;
		Tile startTile = _save.GetTile(startPosition);
		Tile destinationTile = _save.GetTile(endPosition);

		if (destinationTile == null)
		{
			return false;
		}

		if (startTile.HasGravLiftFloor && destinationTile.HasGravLiftFloor)
		{
			if (missile)
			{
				if (direction == DIR_UP)
				{
					if (destinationTile.GetMapData(TilePart.O_FLOOR).GetLoftID(0) != 0)
						return false;
				}
				else if (startTile.GetMapData(TilePart.O_FLOOR).GetLoftID(0) != 0)
				{
					return false;
				}
			}
			return true;
		}
		else
		{
			if (bu.MovementType == MovementType.MT_FLY || startTile.HasLadder)
			{
				if ((direction == DIR_UP && destinationTile.HasNoFloor(_save)) // flying up only possible when there is no roof
					|| (direction == DIR_DOWN && startTile.HasNoFloor(_save)) // falling down only possible when there is no floor
					)
				{
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Marks tiles for the path preview.
	/// </summary>
	/// <param name="bRemove">Remove preview?</param>
	/// <returns>True, if a path is previewed.</returns>
	public bool PreviewPath(bool bRemove = false)
	{
		if (_path.Count == 0)
			return false;

		if (!bRemove && _pathPreviewed)
			return false;

		_pathPreviewed = !bRemove;

		if (bRemove)
		{
			// use old values, player could unpress buttons.
		}
		else
		{
			_ctrlUsed = Options.strafe && _save.IsCtrlPressed(true);
			_altUsed = Options.strafe && _save.IsAltPressed(true);
		}

		RefreshPath();

		return true;
	}

	/// <summary>
	/// Unmarks the tiles used for the path preview.
	/// </summary>
	/// <returns>True, if the previewed path was removed.</returns>
	public bool RemovePreview()
	{
		if (!_pathPreviewed)
			return false;
		PreviewPath(true);
		return true;
	}

	/// <summary>
	/// Refresh the path preview.
	/// </summary>
	public void RefreshPath()
	{
		Position pos = _unit.Position;

		int tus = _unit.TimeUnits;
		if (_unit.IsKneeled)
		{
			tus -= _unit.KneelUpCost;
		}
		int energy = _unit.Energy;
		int size = _unit.Armor.Size - 1;
		int total = _unit.IsKneeled ? _unit.KneelUpCost : 0;
		if (_unit.Armor.TurnBeforeFirstStep)
		{
			int dir = StartDirection;
			if (dir != -1 && dir != _unit.Direction)
			{
				int turnCost = Math.Abs(_unit.Direction - dir);
				if (turnCost > 4)
				{
					turnCost = 8 - turnCost;
				}
				turnCost = turnCost * _unit.TurnCost;

				tus -= turnCost;
				total += turnCost;
			}
		}
		bool switchBack = false;
		if (_save.BattleGame.ReservedAction == BattleActionType.BA_NONE)
		{
			switchBack = true;
			_save.BattleGame.SetTUReserved(BattleActionType.BA_AUTOSHOT);
		}

		bool running = _ctrlUsed && _unit.Armor.AllowsRunning(_unit.IsSmallUnit) && (_path.Count > 1 || _altUsed);
		bool strafing = !running && _ctrlUsed && _unit.Armor.AllowsStrafing(_unit.IsSmallUnit) && _path.Count == 1;
		bool sneaking = !running && _altUsed && _unit.Armor.AllowsSneaking(_unit.IsSmallUnit);

		BattleActionMove bam = strafing ? BattleActionMove.BAM_STRAFE : running ? BattleActionMove.BAM_RUN : sneaking ? BattleActionMove.BAM_SNEAK : BattleActionMove.BAM_NORMAL;
		MovementType movementType = GetMovementType(_unit, null, bam); //preview always for unit not missiles
		// reverse iteration over the path (C++ reverse_iterator i <-> index k)
		for (int k = _path.Count - 1; k >= 0; --k)
		{
			int dir = _path[k];
			PathfindingStep r = GetTUCost(pos, dir, _unit, null, bam);
			pos = r.pos;
			energy -= r.cost.energy;
			tus -= r.cost.time;
			total += r.cost.time;
			bool reserve = _save.BattleGame.CheckReservedTU(_unit, total, _unit.Energy - energy, true);
			for (int x = size; x >= 0; x--)
			{
				for (int y = size; y >= 0; y--)
				{
					Tile tile = _save.GetTile(pos + new Position(x, y, 0));
					Tile tileAbove = _save.GetTile(pos + new Position(x, y, 1));
					if (_pathPreviewed)
					{
						if (k == 0)
						{
							tile.Preview = 10;
						}
						else
						{
							int nextDir = _path[k - 1];
							tile.Preview = nextDir;
						}
						if ((x != 0 && y != 0) || size == 0)
						{
							tile.TUMarker = Math.Max(0, tus);
							tile.EnergyMarker = Math.Max(0, energy);
						}
						if (tileAbove != null && tileAbove.Preview == 0 && r.cost.time == 0 && movementType != MovementType.MT_FLY) //unit fell down, retroactively make the tile above's direction marker to DOWN
						{
							tileAbove.Preview = DIR_DOWN;
						}
					}
					else
					{
						tile.Preview = -1;
						tile.TUMarker = -1;
						tile.EnergyMarker = -1;
					}
					tile.MarkerColor = !_pathPreviewed ? 0 : ((tus >= 0 && energy >= 0) ? (reserve ? green : yellow) : red);
				}
			}
		}
		if (switchBack)
		{
			_save.BattleGame.SetTUReserved(BattleActionType.BA_NONE);
		}
	}

	/// <summary>
	/// Calculates the shortest path using Bresenham path algorithm.
	/// </summary>
	/// <remarks>This only works in the X/Y plane.</remarks>
	/// <param name="origin">The position to start from.</param>
	/// <param name="target">The position we want to reach.</param>
	/// <param name="bam">Move type.</param>
	/// <param name="missileTarget">Target of the path.</param>
	/// <param name="sneak">Is the unit sneaking?</param>
	/// <param name="maxTUCost">Maximum time units the path can cost (unused, like in C++).</param>
	/// <returns>True if a path exists, false otherwise.</returns>
	private bool BresenhamPath(Position origin, Position target, BattleActionMove bam, BattleUnit missileTarget, bool sneak = false, int maxTUCost = 1000)
	{
		_ = maxTUCost;
		ReadOnlySpan<int> xd = [0, 1, 1, 1, 0, -1, -1, -1];
		ReadOnlySpan<int> yd = [-1, -1, 0, 1, 1, 1, 0, -1];

		int x, x0, x1, delta_x, step_x;
		int y, y0, y1, delta_y, step_y;
		int z, z0, z1, delta_z, step_z;
		bool swap_xy, swap_xz;
		int drift_xy, drift_xz;
		int cx, cy, cz;
		Position lastPoint = origin;
		int dir;
		int lastTUCost = -1;
		Position nextPoint;
		_totalTUCost = default;

		//start and end points
		x0 = origin.x; x1 = target.x;
		y0 = origin.y; y1 = target.y;
		z0 = origin.z; z1 = target.z;

		//'steep' xy Line, make longest delta x plane
		swap_xy = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
		if (swap_xy)
		{
			(x0, y0) = (y0, x0);
			(x1, y1) = (y1, x1);
		}

		//do same for xz
		swap_xz = Math.Abs(z1 - z0) > Math.Abs(x1 - x0);
		if (swap_xz)
		{
			(x0, z0) = (z0, x0);
			(x1, z1) = (z1, x1);
		}

		//delta is Length in each plane
		delta_x = Math.Abs(x1 - x0);
		delta_y = Math.Abs(y1 - y0);
		delta_z = Math.Abs(z1 - z0);

		//drift controls when to step in 'shallow' planes
		//starting value keeps Line centred
		drift_xy = delta_x / 2;
		drift_xz = delta_x / 2;

		//direction of line
		step_x = 1; if (x0 > x1) { step_x = -1; }
		step_y = 1; if (y0 > y1) { step_y = -1; }
		step_z = 1; if (z0 > z1) { step_z = -1; }

		//starting point
		y = y0;
		z = z0;

		//step through longest delta (which we have swapped to x)
		for (x = x0; x != (x1 + step_x); x += step_x)
		{
			//copy position
			cx = x; cy = y; cz = z;

			//unswap (in reverse)
			if (swap_xz) (cx, cz) = (cz, cx);
			if (swap_xy) (cx, cy) = (cy, cx);

			if (x != x0 || y != y0 || z != z0)
			{
				Position realNextPoint = new(cx, cy, cz);
				// get direction
				for (dir = 0; dir < 8; ++dir)
				{
					if (xd[dir] == cx - lastPoint.x && yd[dir] == cy - lastPoint.y) break;
				}
				if (dir == 8)
				{
					if (cz - lastPoint.z > 0)
					{
						dir = DIR_UP;
					}
					else
					{
						dir = DIR_DOWN;
					}
				}
				PathfindingStep r = GetTUCost(lastPoint, dir, _unit, missileTarget, bam);
				nextPoint = r.pos;
				int tuCost = r.cost.time + r.penalty.time;

				if (sneak && _save.GetTile(nextPoint).Visible != 0) return false;

				// delete the following
				bool isDiagonal = (dir & 1) != 0;
				int lastTUCostDiagonal = lastTUCost + lastTUCost / 2;
				int tuCostDiagonal = tuCost + tuCost / 2;
				if (nextPoint == realNextPoint && r.cost.time != INVALID_MOVE_COST && (tuCost == lastTUCost || (isDiagonal && tuCost == lastTUCostDiagonal) || (!isDiagonal && tuCostDiagonal == lastTUCost) || lastTUCost == -1)
					&& !IsBlockedDirection(_unit, _save.GetTile(lastPoint), dir, bam, missileTarget))
				{
					_path.Add(dir);
				}
				else
				{
					return false;
				}
				if (missileTarget == null && r.cost.time != INVALID_MOVE_COST)
				{
					lastTUCost = tuCost;
					_totalTUCost = _totalTUCost + r.cost + r.penalty;
				}
				lastPoint = new Position(cx, cy, cz);
			}
			//update progress in other planes
			drift_xy = drift_xy - delta_y;
			drift_xz = drift_xz - delta_z;

			//step in y plane
			if (drift_xy < 0)
			{
				y = y + step_y;
				drift_xy = drift_xy + delta_x;
			}

			//same in z
			if (drift_xz < 0)
			{
				z = z + step_z;
				drift_xz = drift_xz + delta_x;
			}
		}

		return true;
	}

	/// <summary>
	/// Locates all tiles reachable to the unit with a TU cost no more than its TUs minus the given cost.
	/// Uses Dijkstra's algorithm.
	/// </summary>
	/// <param name="unit">Pointer to the unit.</param>
	/// <param name="cost">The cost to keep for an action.</param>
	/// <returns>An array of reachable tiles, sorted in ascending order of cost. The first tile is the start location.</returns>
	public List<int> FindReachable(BattleUnit unit, BattleActionCost cost)
	{
		Position start = unit.Position;
		int tuMax = unit.TimeUnits - cost.Time;
		int energyMax = unit.Energy - cost.Energy;

		PathfindingCost costMax = new(tuMax, energyMax);

		foreach (PathfindingNode pn in _nodes)
		{
			pn.Reset();
		}
		PathfindingNode startNode = GetNode(start);
		startNode.Connect(default, null, 0);
		PathfindingOpenSet unvisited = new();
		unvisited.Push(startNode);
		List<PathfindingNode> reachable = [];
		while (!unvisited.Empty())
		{
			PathfindingNode currentNode = unvisited.Pop();
			Position currentPos = currentNode.Position;

			// Try all reachable neighbours.
			for (int direction = 0; direction < 10; direction++)
			{
				PathfindingStep r = GetTUCost(currentPos, direction, unit, null, BattleActionMove.BAM_NORMAL);
				if (r.cost.time == INVALID_MOVE_COST) // Skip unreachable / blocked
					continue;
				PathfindingCost totalTuCost = currentNode.GetTUCost(false) + r.cost + r.penalty;
				if (!(totalTuCost <= costMax)) // Run out of TUs/Energy
					continue;
				PathfindingNode nextNode = GetNode(r.pos);
				if (nextNode.IsChecked) // Our algorithm means this node is already at minimum cost.
					continue;
				// If this node is unvisited or visited from a better path.
				if (!nextNode.InOpenSet() || nextNode.GetTUCost(false).time > totalTuCost.time)
				{
					nextNode.Connect(totalTuCost, currentNode, direction);
					unvisited.Push(nextNode);
				}
			}
			currentNode.SetChecked();
			reachable.Add(currentNode);
		}
		StdAlgorithm.Sort(reachable, MinNodeCosts.Compare); // std::sort, same (unstable) order as libstdc++
		List<int> tiles = new(reachable.Count);
		foreach (PathfindingNode pn in reachable)
		{
			tiles.Add(_save.GetTileIndex(pn.Position));
		}
		return tiles;
	}

	/// <summary>Gets the strafe move setting.</summary>
	public bool StrafeMove => _strafeMove;

	/// <summary>Sets _unit in order to abuse low-level pathfinding functions from outside the class.</summary>
	public void SetUnit(BattleUnit unit)
	{
		_unit = unit;
	}

	/// <summary>Gets _totalTUCost; finds out whether we can hike somewhere in this turn or not.</summary>
	public int TotalTUCost => _totalTUCost.time;

	/// <summary>Gets the path preview setting.</summary>
	public bool IsPathPreviewed => _pathPreviewed;

	/// <summary>Gets the modifier setting.</summary>
	public bool IsModifierCtrlUsed => _ctrlUsed;

	/// <summary>Gets the modifier setting.</summary>
	public bool IsModifierAltUsed => _altUsed;

	/// <summary>Gets a reference to the current path (stored in reverse order).</summary>
	public List<int> Path => _path;

	/// <summary>Makes a copy of the current path.</summary>
	public List<int> CopyPath() => new(_path);

	public enum bigWallTypes : uint
	{
		BLOCK = 1,
		BIGWALLNESW = 2,
		BIGWALLNWSE = 3,
		BIGWALLWEST = 4,
		BIGWALLNORTH = 5,
		BIGWALLEAST = 6,
		BIGWALLSOUTH = 7,
		BIGWALLEASTANDSOUTH = 8,
		BIGWALLWESTANDNORTH = 9,
	}
}
