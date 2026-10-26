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
// Ported from: src/Battlescape/UnitFallBState.h, src/Battlescape/UnitFallBState.cpp
#nullable disable

using static Acom.BattleActionType;
using static Acom.ItemDamageType;
using static Acom.LightLayers;
using static Acom.MovementType;
using static Acom.SpecialAbility;
using static Acom.UnitFaction;
using static Acom.UnitStatus;

namespace Acom;

/// <summary>State for falling units.</summary>
public class UnitFallBState : BattleState
{
	private TileEngine _terrain;
	private List<Tile> tilesToFallInto = new();
	private List<BattleUnit> unitsToMove = new();

	/// <summary>Sets up an UnitFallBState.</summary>
	/// <param name="parent">Pointer to the Battlescape.</param>
	public UnitFallBState(BattlescapeGame parent) : base(parent)
	{
		_terrain = null;
	}

	/// <summary>Initializes the state.</summary>
	public override void Init()
	{
		_terrain = _parent.TileEngine;
		if (_parent.Save.Side == FACTION_PLAYER)
			_parent.SetStateInterval((uint)Options.battleXcomSpeed);
		else
			_parent.SetStateInterval((uint)Options.battleAlienSpeed);
	}

	/// <summary>
	/// Runs state functionality every cycle.
	/// Progresses the fall, updates the battlescape, ...
	/// </summary>
	public override void Think()
	{
		List<BattleUnit> fallingUnitsList = _parent.Save.FallingUnits;
		for (int unitIt = 0; unitIt < fallingUnitsList.Count;)
		{
			BattleUnit unit = fallingUnitsList[unitIt];

			// I ain't got time to panic
			if (unit.Status == STATUS_TURNING || unit.Status == STATUS_PANICKING || unit.Status == STATUS_BERSERK)
			{
				unit.AbortTurn();
			}
			bool falling = true;
			int size = unit.Armor.Size - 1;
			if (unit.IsOutThresholdExceed)
			{
				fallingUnitsList.RemoveAt(unitIt);
				continue;
			}

			if (unit.Status == STATUS_WALKING || unit.Status == STATUS_FLYING)
			{
				unit.KeepWalking(_parent.Save, true); // advances the phase

				++unitIt;
				continue;
			}

			falling = unit.HaveNoFloorBelow()
				&& unit.Position.z != 0
				&& unit.MovementType != MT_FLY
				&& unit.WalkingPhase == 0;

			if (falling)
			{
				// Tile(s) unit is falling into.
				for (int x = size; x >= 0; --x)
				{
					for (int y = size; y >= 0; --y)
					{
						Tile tileTarget = _parent.Save.GetTile(unit.Position + new Position(x, y, -1));
						tilesToFallInto.Add(tileTarget);
					}
				}
				List<BattleUnit> fallingUnits = _parent.Save.FallingUnits;
				// Check each tile for units that need moving out of the way.
				foreach (Tile tile in tilesToFallInto)
				{
					BattleUnit unitBelow = tile.Unit;
					if (unitBelow != null
						&& !fallingUnits.Contains(unitBelow)  // ignore falling units (including self)
						&& !unitsToMove.Contains(unitBelow))  // ignore already added units
					{
						unitsToMove.Add(unitBelow);
					}
				}
			}

			// we are just standing around, we are done falling.
			if (unit.Status == STATUS_STANDING)
			{
				if (falling)
				{
					Position destination = unit.Position + new Position(0, 0, -1);
					unit.StartWalking(Pathfinding.DIR_DOWN, destination, _parent.Save);
					// C++ has `++unit;` here, which increments the local pointer and not the iterator:
					// the same unit is visited again (now walking) in the next loop pass.
				}
				else
				{
					// if the unit burns floor tiles, burn floor tiles
					if (unit.SpecialAbility == (int)SPECAB_BURNFLOOR || unit.SpecialAbility == (int)SPECAB_BURN_AND_EXPLODE)
					{
						unit.Tile.Ignite(1);
						Position groundVoxel = (unit.Position.ToVoxel()) + new Position(8, 8, -(unit.Tile.TerrainLevel));
						_parent.TileEngine.Hit(new BattleActionAttack(BA_NONE, unit), groundVoxel, unit.BaseStats.strength, _parent.Mod.GetDamageType(DT_IN), false);

						if (unit.Status != STATUS_STANDING) // ie: we burned a hole in the floor and fell through it
						{
							_parent.Pathfinding.AbortPath();
						}
					}
					// move our personal lighting with us
					int change = _parent.CheckForProximityGrenades(unit);
					_terrain.CalculateLighting(change != 0 ? LL_ITEMS : LL_UNITS, unit.Position, 2);
					_terrain.CalculateFOV(unit.Position, 2, false); //update everyone else to see this unit, as well as all this unit's visible units.
					_terrain.CalculateFOV(unit, true, false); //update tiles
					if (unit.Status == STATUS_STANDING)
					{
						var fall = new BattleAction();
						fall.type = BA_WALK;
						fall.actor = unit;
						if (_parent.TileEngine.CheckReactionFire(unit, fall))
							_parent.Pathfinding.AbortPath();
						fallingUnitsList.RemoveAt(unitIt);
					}
				}
			}
			else
			{
				++unitIt;
			}
		}

		// Find somewhere to move the unit(s) In danger of being squashed.
		if (unitsToMove.Count != 0)
		{
			var escapeTiles = new List<Tile>();
			for (int ubIt = 0; ubIt < unitsToMove.Count;)
			{
				BattleUnit unitBelow = unitsToMove[ubIt];
				bool escapeFound = false;

				// We need to move all sections of the unit out of the way.
				int bodyTotalSize = unitBelow.Armor.TotalSize;
				Position[] bodyOffsets =
				[
					new Position(0, 0, 0),
					new Position(1, 0, 0),
					new Position(0, 1, 0),
					new Position(1, 1, 0),
				];

				// Check in each compass direction.
				for (int dir = 0; dir < Pathfinding.DIR_UP && !escapeFound; dir++)
				{
					var offset = new Position();
					Pathfinding.DirectionToVector(dir, ref offset);

					_parent.Save.Pathfinding.SetUnit(unitBelow); //TODO: remove as was done by `getTUCost`
					PathfindingStep r = _parent.Save.Pathfinding.GetTUCost(unitBelow.Position, dir, unitBelow, null, BattleActionMove.BAM_NORMAL);
					if (r.cost.time == Pathfinding.INVALID_MOVE_COST)
					{
						// can't move this way, try another direction
						continue;
					}

					for (int bs = 0; bs < bodyTotalSize;)
					{
						Position originalPosition = unitBelow.Position + bodyOffsets[bs];
						Position endPosition = originalPosition + offset;

						Tile t = _parent.Save.GetTile(endPosition);
						if (t == null)
						{
							// Try next direction.
							break;
						}

						bool aboutToBeOccupiedFromAbove = tilesToFallInto.Contains(t);
						bool alreadyTaken = escapeTiles.Contains(t);
						bool alreadyOccupied = t.Unit != null && (t.Unit != unitBelow);
						bool hasFloor = !t.HasNoFloor(_parent.Save);
						bool unitCanFly = unitBelow.MovementType == MT_FLY;

						bool canMoveToTile = !alreadyOccupied && !alreadyTaken && !aboutToBeOccupiedFromAbove && (hasFloor || unitCanFly);
						if (canMoveToTile)
						{
							// Check next section of the unit.
							++bs;
						}
						else
						{
							// Try next direction.
							break;
						}

						// If all sections of the fallen onto unit can be moved, then we move it.
						if (bs == bodyTotalSize)
						{
							if (_parent.Save.AddFallingUnit(unitBelow))
							{
								escapeFound = true;
								// Now ensure no other unit escapes to here too.
								for (int x = unitBelow.Armor.Size - 1; x >= 0; --x)
								{
									for (int y = unitBelow.Armor.Size - 1; y >= 0; --y)
									{
										Tile et = _parent.Save.GetTile(t.Position + new Position(x, y, 0));
										escapeTiles.Add(et);
									}
								}

								unitBelow.StartWalking(dir, unitBelow.Position + offset, _parent.Save);
								unitsToMove.RemoveAt(ubIt);
							}
						}
					}
				}
				if (!escapeFound)
				{
					// STOMP THAT GOOMBA!
					unitBelow.KnockOut(_parent);
					unitsToMove.RemoveAt(ubIt);
				}
			}
			_parent.CheckForCasualties(null, new BattleActionAttack(BA_NONE, null));
		}

		if (_parent.Save.FallingUnits.Count == 0)
		{
			tilesToFallInto.Clear();
			unitsToMove.Clear();
			_parent.PopState();
			return;
		}
	}
}
