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
// Ported from: src/Battlescape/TileEngine.cpp (field of view, line of sight/fire, voxel checks)
// Port status: partial (missing: script hooks)
#nullable disable

using static Acom.BattleActionType;
using static Acom.TilePart;
using static Acom.UnitFaction;
using static Acom.VoxelType;

namespace Acom;

public partial class TileEngine
{
	/// <summary>
	/// Setups the internal event visibility search space reduction system. This system defines a narrow circle sector around
	/// a given event as viewed from an external observer. This allows narrowing down which tiles/units may need to be updated for
	/// the observer based on the event affecting visibility at the event itself and beyond it in its direction.
	/// Imagines a circle around the event of eventRadius, calculates its tangents, and places points at the circle's tangent
	/// intersections for later bounds checking.
	/// </summary>
	/// <param name="observerPos">Position of the observer of this event.</param>
	/// <param name="eventPos">The centre of the event. Ie a moving unit's position, centre of explosion, a single destroyed tile, etc.</param>
	/// <param name="eventRadius">Radius big enough to fully envelop the event. Ie for a single tile change, set radius to 1.</param>
	/// <returns>true if area is unlimited.</returns>
	private bool SetupEventVisibilitySector(Position observerPos, Position eventPos, int eventRadius)
	{
		return SetupEventVisibilitySector(observerPos, eventPos, eventRadius, ref _eventVisibilitySectorL, ref _eventVisibilitySectorR, ref _eventVisibilityObserverPos);
	}

	/// <summary>Calculation of <see cref="SetupEventVisibilitySector(Position, Position, int)"/> on explicit state (C++ members).</summary>
	internal static bool SetupEventVisibilitySector(Position observerPos, Position eventPos, int eventRadius, ref Position sectorL, ref Position sectorR, ref Position sectorObserverPos)
	{
		if (eventRadius == 0 || eventPos == new Position(-1, -1, -1) || Position.Distance2dSq(observerPos, eventPos) <= eventRadius * eventRadius)
		{
			sectorObserverPos = new Position(-1, -1, -1);
			return true;
		}
		else
		{
			//Use search space reduction by updating within a narrow circle sector covering the event and any
			//units beyond it (they can now be hidden or revealed based on what occurred at the event position)
			//So, with a circle at eventPos of radius eventRadius, define its tangent points as viewed from this unit.
			Position posDiff = eventPos - observerPos;
			float a = MathF.Asin(eventRadius / MathF.Sqrt(posDiff.x * posDiff.x + posDiff.y * posDiff.y));
			float b = MathF.Atan2(posDiff.y, posDiff.x);
			float t1 = b - a;
			float t2 = b + a;
			//Define the points where the lines tangent to the circle intersect it. Note: resulting positions are relative to observer, not in direct tile space.
			sectorL.x = (short)(RoundF(eventPos.x + eventRadius * MathF.Sin(t1)) - observerPos.x);
			sectorL.y = (short)(RoundF(eventPos.y - eventRadius * MathF.Cos(t1)) - observerPos.y);
			sectorR.x = (short)(RoundF(eventPos.x - eventRadius * MathF.Sin(t2)) - observerPos.x);
			sectorR.y = (short)(RoundF(eventPos.y + eventRadius * MathF.Cos(t2)) - observerPos.y);
			sectorObserverPos = observerPos;
			return false;
		}
	}

	/// <summary>C <c>roundf</c>: halfway cases away from zero.</summary>
	private static float RoundF(float x) => MathF.Round(x, MidpointRounding.AwayFromZero);

	/// <summary>
	/// Checks whether toCheck is within a previously setup eventVisibilitySector. See setupEventVisibilitySector(...).
	/// May be used to rapidly reduce the search space when updating unit and tile visibility.
	/// </summary>
	/// <param name="toCheck">The position to check.</param>
	/// <returns>true if within the circle sector.</returns>
	private bool InEventVisibilitySector(Position toCheck)
	{
		return InEventVisibilitySector(toCheck, _eventVisibilitySectorL, _eventVisibilitySectorR, _eventVisibilityObserverPos);
	}

	/// <summary>Calculation of <see cref="InEventVisibilitySector(Position)"/> on explicit state (C++ members).</summary>
	internal static bool InEventVisibilitySector(Position toCheck, Position sectorL, Position sectorR, Position sectorObserverPos)
	{
		if (sectorObserverPos != new Position(-1, -1, -1))
		{
			Position posDiff = toCheck - sectorObserverPos;
			//Is toCheck within the arc as defined by the two tangent points?
			return (!(-sectorL.x * posDiff.y + sectorL.y * posDiff.x > 0) &&
				(-sectorR.x * posDiff.y + sectorR.y * posDiff.x > 0));
		}
		else
		{
			return true;
		}
	}

	/// <summary>Calculates visible units within the field of view (full check).</summary>
	public bool CalculateUnitsInFOV(BattleUnit unit)
	{
		return CalculateUnitsInFOV(unit, invalid);
	}

	/// <summary>Updates line of sight of a single soldier in a narrow arc around a given event position.</summary>
	/// <param name="unit">Unit to check line of sight of.</param>
	/// <param name="eventPos">The centre of the event which necessitated the FOV update. Used to optimize which tiles to update.</param>
	/// <param name="eventRadius">The radius of a circle able to fully encompass the event, in tiles. Hence: 1 for a single tile event.</param>
	/// <returns>True when new aliens are spotted.</returns>
	public bool CalculateUnitsInFOV(BattleUnit unit, Position eventPos, int eventRadius = 0)
	{
		int oldNumVisibleUnits = unit.UnitsSpottedThisTurn.Count;
		bool useTurretDirection = false;
		if (Options.strafe && (unit.TurretType > -1))
		{
			useTurretDirection = true;
		}

		if (unit.IsOut)
			return false;

		Position posSelf = unit.Position;
		if (SetupEventVisibilitySector(posSelf, eventPos, eventRadius))
		{
			//Asked to do a full check. Or the event is overlapping our tile. Better check everything.
			unit.ClearVisibleUnits();
		}

		//Loop through all units specified and figure out which ones we can actually see.
		foreach (BattleUnit bu in _save.Units)
		{
			Position posOther = bu.Position;
			if (!bu.IsOut && (unit.Id != bu.Id))
			{
				int sizeOther = bu.Armor.Size;
				for (int x = 0; x < sizeOther; ++x)
				{
					for (int y = 0; y < sizeOther; ++y)
					{
						Position posToCheck = posOther + new Position(x, y, 0);
						//If we can now find any unit within the arc defined by the event tangent points, its visibility may have been affected by the event.
						if (InEventVisibilitySector(posToCheck))
						{
							if (!unit.CheckViewSector(posToCheck, useTurretDirection))
							{
								//Unit within arc, but not in view sector. If it just walked out we need to remove it.
								unit.RemoveFromVisibleUnits(bu);
							}
							else if (Visible(unit, _save.GetTile(posToCheck))) // (distance is checked here)
							{
								//Unit (or part thereof) visible to one or more eyes of this unit.
								if (unit.Faction == FACTION_PLAYER)
								{
									bu.Visible = true;
								}
								if (((bu.Faction == FACTION_HOSTILE && unit.Faction == FACTION_PLAYER)
									|| (bu.Faction != FACTION_HOSTILE && unit.Faction == FACTION_HOSTILE))
									&& !unit.HasVisibleUnit(bu))
								{
									unit.AddToVisibleUnits(bu);
									unit.AddToVisibleTiles(bu.Tile);
								}

								if (unit.Faction != bu.Faction)
								{
									bu.SetTurnsSinceSpottedByFaction(unit.Faction, 0);
									bu.SetTurnsLeftSpottedForSnipersByFaction(
										unit.Faction,
										Math.Max(unit.SpotterDuration, bu.GetTurnsLeftSpottedForSnipersByFaction(unit.Faction))
									); // defaults to 0 = no information given to snipers
								}

								x = y = sizeOther; //If a unit's tile is visible there's no need to check the others: break the loops.
							}
							else
							{
								//Within arc, but not visible. Need to check to see if whatever happened at eventPos blocked a previously seen unit.
								unit.RemoveFromVisibleUnits(bu);
							}
						}
					}
				}
			}
		}
		// we only react when there are at least the same amount of visible units as before AND the checksum is different
		// this way we stop if there are the same amount of visible units, but a different unit is seen
		// or we stop if there are more visible units seen
		if (unit.UnitsSpottedThisTurn.Count > oldNumVisibleUnits && unit.VisibleUnits.Count != 0)
		{
			return true;
		}
		return false;
	}

	/// <summary>Calculates visible tiles within the field of view (full check).</summary>
	public void CalculateTilesInFOV(BattleUnit unit)
	{
		CalculateTilesInFOV(unit, invalid);
	}

	/// <summary>
	/// Calculates line of sight of tiles for a player controlled soldier.
	/// If supplied with an event position differing from the soldier's position, it will only
	/// calculate tiles within a narrow arc.
	/// </summary>
	/// <param name="unit">Unit to check line of sight of.</param>
	/// <param name="eventPos">The centre of the event which necessitated the FOV update. Used to optimize which tiles to update.</param>
	/// <param name="eventRadius">The radius of a circle able to fully encompass the event, in tiles. Hence: 1 for a single tile event.</param>
	public void CalculateTilesInFOV(BattleUnit unit, Position eventPos, int eventRadius = 0)
	{
		bool useTurretDirection = false;
		bool skipNarrowArcTest = false;
		int direction;
		if (Options.strafe && (unit.TurretType > -1))
		{
			direction = unit.TurretDirection;
			useTurretDirection = true;
		}
		else
		{
			direction = unit.Direction;
		}
		if (unit.Faction != FACTION_PLAYER || (eventRadius == 1 && !unit.CheckViewSector(eventPos, useTurretDirection)))
		{
			//The event wasn't meant for us and/or visible for us.
			return;
		}
		else if (unit.IsOut)
		{
			unit.ClearVisibleTiles();
			return;
		}
		Position posSelf = unit.Position;
		if (SetupEventVisibilitySector(posSelf, eventPos, eventRadius))
		{
			//Asked to do a full check. Or unit within event. Should update all.
			unit.ClearVisibleTiles();
			skipNarrowArcTest = true;
		}

		//Only recalculate bresenham lines to tiles that are at the event or further away.
		int distanceSqrMin = skipNarrowArcTest ? 0 : Math.Max(Position.Distance2dSq(posSelf, eventPos) - eventRadius * eventRadius, 0);

		//Variables for finding the tiles to test based on the view direction.
		Position posTest = default;
		var _trajectory = new List<Position>();
		bool swap = (direction == 0 || direction == 4);
		ReadOnlySpan<int> signX = [+1, +1, +1, +1, -1, -1, -1, -1];
		ReadOnlySpan<int> signY = [-1, -1, -1, +1, +1, +1, -1, -1];
		int y1, y2;

		if ((unit.Height + unit.FloatHeight + -_save.GetTile(unit.Position).TerrainLevel) >= 24 + 4)
		{
			Tile tileAbove = _save.GetTile(posSelf + new Position(0, 0, 1));
			if (tileAbove != null && tileAbove.HasNoFloor(null))
			{
				++posSelf.z;
			}
		}
		//Test all tiles within view cone for visibility.
		for (int x = 0; x <= MaxViewDistance; ++x) //TODO: Possible improvement: find the intercept points of the arc at max view distance and choose a more intelligent sweep of values when an event arc is defined.
		{
			if ((direction & 1) != 0)
			{
				y1 = 0;
				y2 = MaxViewDistance;
			}
			else
			{
				y1 = -x;
				y2 = x;
			}
			for (int y = y1; y <= y2; ++y) //TODO: Possible improvement: find the intercept points of the arc at max view distance and choose a more intelligent sweep of values when an event arc is defined.
			{
				int distanceSqr = x * x + y * y;
				if (distanceSqr <= MaxViewDistanceSq && distanceSqr >= distanceSqrMin)
				{
					posTest.x = (short)(posSelf.x + signX[direction] * (swap ? y : x));
					posTest.y = (short)(posSelf.y + signY[direction] * (swap ? x : y));
					//Only continue if the column of tiles at (x,y) is within the narrow arc of interest (if enabled)
					if (InEventVisibilitySector(posTest))
					{
						for (int z = 0; z < _save.MapSizeZ; z++)
						{
							posTest.z = (short)z;

							if (_save.GetTile(posTest) != null) //inside map?
							{
								// this sets tiles to discovered if they are in LOS - tile visibility is not calculated in voxelspace but in tilespace
								// large units have "4 pair of eyes"
								int size = unit.Armor.Size;
								for (int xo = 0; xo < size; xo++)
								{
									for (int yo = 0; yo < size; yo++)
									{
										Position poso = posSelf + new Position(xo, yo, 0);
										_trajectory.Clear();
										int tst = CalculateLineTile(poso, posTest, _trajectory);
										if (tst > 127)
										{
											//Vision impacted something before reaching posTest. Throw away the impact point.
											_trajectory.RemoveAt(_trajectory.Count - 1);
										}
										//Reveal all tiles along line of vision. Note: needed due to width of bresenham stroke.
										foreach (Position posVisited in _trajectory)
										{
											//Add tiles to the visible list only once. BUT we still need to calculate the whole trajectory as
											// this bresenham line's period might be different from the one that originally revealed the tile.
											if (!unit.HasVisibleTile(_save.GetTile(posVisited)))
											{
												unit.AddToVisibleTiles(_save.GetTile(posVisited));
												_save.GetTile(posVisited).Visible = +1; // C++ setVisible(+1)
												_save.GetTile(posVisited).SetDiscovered(true, O_FLOOR);

												// walls to the east or south of a visible tile, we see that too
												Tile t = _save.GetTile(new Position(posVisited.x + 1, posVisited.y, posVisited.z));
												t?.SetDiscovered(true, O_WESTWALL);
												t = _save.GetTile(new Position(posVisited.x, posVisited.y + 1, posVisited.z));
												t?.SetDiscovered(true, O_NORTHWALL);
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
	}

	/// <summary>Recalculates line of sight of a soldier.</summary>
	/// <param name="unit">Unit to check line of sight of.</param>
	/// <param name="doTileRecalc">True (default) to recalculate the visible tiles for this unit.</param>
	/// <param name="doUnitRecalc">True (default) to recalculate the visible units for this unit.</param>
	/// <returns>True when new aliens are spotted.</returns>
	public bool CalculateFOV(BattleUnit unit, bool doTileRecalc = true, bool doUnitRecalc = true)
	{
		//Force a full FOV recheck for this unit.
		if (doTileRecalc) CalculateTilesInFOV(unit);
		return doUnitRecalc ? CalculateUnitsInFOV(unit) : false;
	}

	/// <summary>Gets the origin voxel of a unit's eyesight.</summary>
	/// <param name="currentUnit">The watcher.</param>
	/// <returns>Approximately an eyeball voxel.</returns>
	public Position GetSightOriginVoxel(BattleUnit currentUnit)
	{
		Position pos = currentUnit.Position;
		Tile tile = currentUnit.Tile;

		// determine the origin and target voxels for the raytrace
		Position originVoxel;
		originVoxel = pos.ToVoxel() + new Position(8, 8, 0);
		originVoxel.z = (short)(originVoxel.z + -tile.TerrainLevel);
		originVoxel.z = (short)(originVoxel.z + currentUnit.Height + currentUnit.FloatHeight - 1); //one voxel lower (eye level)
		Tile tileAbove = _save.GetAboveTile(tile);
		if (currentUnit.IsBigUnit)
		{
			originVoxel.x += 8;
			originVoxel.y += 8;
			originVoxel.z += 1; //topmost voxel
		}
		if (originVoxel.z >= (pos.z + 1) * Position.TileZ && (tileAbove == null || !tileAbove.HasNoFloor(null)))
		{
			while (originVoxel.z >= (pos.z + 1) * Position.TileZ)
			{
				originVoxel.z--;
			}
		}

		return originVoxel;
	}

	/// <summary>Calculate max visible distance.</summary>
	/// <param name="tile">Target tile that is look at.</param>
	/// <param name="currentUnit">Unit that look on tile or unit.</param>
	/// <param name="targetUnit">Unit that is look at.</param>
	/// <returns>Effective visible distance that consider camouflage and shade, and max unit visibility distance in tiles independent of target darkness.</returns>
	private (int visibleDistanceMaxVoxel, int visibleDistanceUnitMaxTile) GetVisibleDistanceMaxHelper(Tile tile, BattleUnit currentUnit, BattleUnit targetUnit)
	{
		bool targetIsDark = tile.Shade > MaxDarknessToSeeUnits;
		bool targetOnFire = (targetUnit != null && targetUnit.Fire > 0);
		if (targetOnFire)
		{
			// Note: fire cancels enemy's camouflage
			targetUnit = null;
			targetIsDark = false;
		}

		int viewDistanceAtDarkTiles = currentUnit.GetMaxViewDistanceAtDark(targetUnit);
		int viewDistanceAtDayTiles = currentUnit.GetMaxViewDistanceAtDay(targetUnit);

		// global max distance, independent of unit
		int visibleDistanceGlobalMaxVoxel = MaxVoxelViewDistance;
		// max distance, affected by target unit too
		int visibleDistanceMaxVoxel = visibleDistanceGlobalMaxVoxel;
		// unit max distance, mix of dark and day range
		int visibleDistanceUnitMaxTile = Math.Min(
			MaxViewDistance,
			Math.Max(
				viewDistanceAtDarkTiles,
				viewDistanceAtDayTiles
			)
		);

		// during dark aliens can see 20 tiles, xcom can see 9 by default... unless overridden by armor
		if (targetIsDark)
		{
			visibleDistanceMaxVoxel = Math.Min(
				visibleDistanceGlobalMaxVoxel,
				viewDistanceAtDarkTiles * Position.TileXY
			);
		}
		// during day (or if enough other light) both see 20 tiles ... unless overridden by armor
		else
		{
			visibleDistanceMaxVoxel = Math.Min(
				visibleDistanceGlobalMaxVoxel,
				viewDistanceAtDayTiles * Position.TileXY
			);
		}

		// small buffer that allow for very short visibility distance still work in smoke or some diagonal directions still be visible
		visibleDistanceMaxVoxel += Position.TileXY / 4;

		return (visibleDistanceMaxVoxel, visibleDistanceUnitMaxTile);
	}

	/// <summary>Get data for given trajectory.</summary>
	/// <param name="currentUnit">Unit that look on tile or unit.</param>
	/// <param name="originVoxel">Start trajectory voxel.</param>
	/// <param name="scanVoxel">End trajectory voxel.</param>
	/// <returns>visibleDistanceVoxels, densityOfSmoke, densityOfFire, densityOfSmokeNearUnit, densityOfFireNearUnit.</returns>
	private (int, int, int, int, int) GetTrajectoryDataHelper(BattleUnit currentUnit, Position originVoxel, Position scanVoxel)
	{
		// predict used distance to avoid multiple allocations
		Position diff = (originVoxel - scanVoxel);
		var _trajectory = new List<Position>(Math.Max(Math.Max(Math.Abs((int)diff.x), Math.Abs((int)diff.y)), Math.Abs((int)diff.z)) + 1);

		// now check if we really see it taking into account smoke tiles
		// initial smoke "density" of a smoke grenade is around 15 per tile
		// we do density/3 to get the decay of visibility
		// so in fresh smoke we should only have 4 tiles of visibility
		// this is traced in voxel space, with smoke affecting visibility every step of the way
		CalculateLineVoxel(originVoxel, scanVoxel, true, _trajectory, currentUnit);
		int trajectorySize = _trajectory.Count;
		float densityOfSmoke = 0;
		float densityOfFire = 0;
		float densityOfSmokeNearUnit = 0;
		float densityOfFireeNearUnit = 0;
		float visibleDistanceVoxels = 0;
		var trackTile = new Position(-1, -1, -1);
		Tile t = null;

		for (int i = 0; i < trajectorySize; i++)
		{
			Position posTile = _trajectory[i].ToTile();
			float step = TrajectoryStepSize(_trajectory, i);
			if (trackTile != posTile)
			{
				trackTile = posTile;
				t = _save.GetTile(trackTile);
			}
			visibleDistanceVoxels += step;
			if (t.Fire == 0)
			{
				densityOfSmoke += step * t.Smoke;
			}
			else
			{
				densityOfFire += step * t.Smoke; // this boost fire blocking visibility for thermo vision as usually smoke value is bigger
			}
			if (visibleDistanceVoxels < Position.TileXY * 2)
			{
				if (t.Fire == 0)
				{
					densityOfSmokeNearUnit += step * t.Smoke;
				}
				else
				{
					densityOfFireeNearUnit += step * t.Smoke; // this boost fire blocking visibility for thermo vision as usually smoke value is bigger
				}
			}
		}

		return ((int)visibleDistanceVoxels, (int)densityOfSmoke, (int)densityOfFire, (int)densityOfSmokeNearUnit, (int)densityOfFireeNearUnit);
	}

	/// <summary>Checks for an opposing unit on this tile.</summary>
	/// <param name="currentUnit">The watcher.</param>
	/// <param name="tile">The tile to check for.</param>
	/// <returns>True if visible.</returns>
	public bool Visible(BattleUnit currentUnit, Tile tile)
	{
		// if there is no tile or no unit, we can't see it
		if (tile == null || tile.Unit == null)
		{
			return false;
		}

		// friendlies are always seen
		if (currentUnit.Faction == tile.Unit.Faction) return true;

		// if beyond global max. range, nobody can see anyone
		int currentDistanceSq = Position.Distance2dSq(currentUnit.Position, tile.Position);
		if (currentDistanceSq > MaxViewDistanceSq)
		{
			return false;
		}

		// psi vision
		int psiVisionDistance = currentUnit.PsiVision;
		bool fearImmune = tile.Unit.Armor.GetFearImmune();
		if (psiVisionDistance > 0 && !fearImmune)
		{
			int psiCamo = tile.Unit.Armor.PsiCamouflage;
			if (psiCamo > 0)
			{
				psiVisionDistance = Math.Min(psiVisionDistance, psiCamo);
			}
			else if (psiCamo < 0)
			{
				psiVisionDistance = Math.Max(0, psiVisionDistance + psiCamo);
			}
			if (currentDistanceSq <= (psiVisionDistance * psiVisionDistance))
			{
				return true; // we already sense the unit, no need to check obstacles or smoke
			}
		}

		var (visibleDistanceMaxVoxel, _) = GetVisibleDistanceMaxHelper(tile, currentUnit, tile.Unit);

		Position originVoxel = GetSightOriginVoxel(currentUnit);

		Position scanVoxel = default;
		bool unitSeen = CanTargetUnit(ref originVoxel, tile, ref scanVoxel, currentUnit, false);

		// heat vision 100% = smoke effectiveness 0%
		int smokeDensityFactor = 100 - Clamp(currentUnit.VisibilityThroughSmoke, 0, 100);
		int fireDensityFactor = 100 - Clamp(currentUnit.VisibilityThroughFire, 0, 100);

		if (unitSeen)
		{
			var (visibleDistanceVoxels, densityOfSmoke, densityOfFire, densityOfSmokeNearUnit, densityOfFireeNearUnit) = GetTrajectoryDataHelper(currentUnit, originVoxel, scanVoxel);

			// 3  - coefficient of calculation (see getTrajectoryDataHelper).
			// 20 - maximum view distance in vanilla Xcom.
			// 100 - % for smokeDensityFactor.
			// 16 - for voxel scale calculation.
			// Even if MaxViewDistance will be increased via ruleset, smoke will keep effect.
			int visibilityQuality = visibleDistanceMaxVoxel - visibleDistanceVoxels - ((densityOfSmoke - densityOfSmokeNearUnit / 2) * smokeDensityFactor + (densityOfFire - densityOfFireeNearUnit / 2) * fireDensityFactor) * visibleDistanceMaxVoxel / (3 * 20 * 100 * 16);
			// script hook ModScript::VisibilityUnit (armor) not ported: the unmodified quality decides
			unitSeen = 0 < visibilityQuality;
		}
		return unitSeen;
	}

	/// <summary>
	/// Checks to see if a tile is visible through darkness, obstacles and smoke.
	/// Note: psi vision, camouflage/anti-camouflage are intentionally removed.
	/// </summary>
	/// <param name="action">Current battle action.</param>
	/// <param name="tile">The tile to check for.</param>
	/// <param name="drawing">Use the unit of the tile (drawing) instead of the overlapping unit.</param>
	/// <returns>True if visible.</returns>
	public bool IsTileInLOS(BattleAction action, Tile tile, bool drawing)
	{
		// if there is no tile, we can't see it
		if (tile == null)
		{
			return false;
		}

		BattleUnit currentUnit = action.actor;

		// if beyond global max. range, nobody can see anything
		int currentDistanceSq = Position.Distance2dSq(currentUnit.Position, tile.Position);
		if (currentDistanceSq > MaxViewDistanceSq)
		{
			return false;
		}

		var (visibleDistanceMaxVoxel, _) = GetVisibleDistanceMaxHelper(tile, currentUnit, /*targetUnit*/ null);

		// We MUST build a temp action, because current action doesn't yet have updated target (when only aiming)
		var tempAction = new BattleAction();
		tempAction.actor = currentUnit;
		tempAction.type = action.type;
		tempAction.target = tile.Position;

		Position originVoxel = GetOriginVoxel(tempAction, currentUnit.Tile);
		Position scanVoxel = default;
		var _trajectory = new List<Position>();
		bool seen = false;

		bool forceFire = Options.forceFire && _save.IsCtrlPressed(true) && _save.Side == FACTION_PLAYER;

		// Primary LOF check
		if (forceFire)
		{
			// if force-firing, always aim at the center of the target tile
			scanVoxel = tile.Position.ToVoxel() + voxelTileCenter;
		}
		else if (tile.GetMapData(O_OBJECT) != null)
		{
			if (CanTargetTile(ref originVoxel, tile, (int)O_OBJECT, ref scanVoxel, currentUnit, false))
			{
				seen = true;
			}
			else
			{
				scanVoxel = tile.Position.ToVoxel() + new Position(8, 8, 10);
			}
		}
		else if (tile.GetMapData(O_NORTHWALL) != null)
		{
			if (CanTargetTile(ref originVoxel, tile, (int)O_NORTHWALL, ref scanVoxel, currentUnit, false))
			{
				seen = true;
			}
			else
			{
				scanVoxel = tile.Position.ToVoxel() + new Position(8, 0, 9);
			}
		}
		else if (tile.GetMapData(O_WESTWALL) != null)
		{
			if (CanTargetTile(ref originVoxel, tile, (int)O_WESTWALL, ref scanVoxel, currentUnit, false))
			{
				seen = true;
			}
			else
			{
				scanVoxel = tile.Position.ToVoxel() + new Position(0, 8, 9);
			}
		}
		else if (tile.GetMapData(O_FLOOR) != null)
		{
			if (CanTargetTile(ref originVoxel, tile, (int)O_FLOOR, ref scanVoxel, currentUnit, false))
			{
				seen = true;
			}
			else
			{
				scanVoxel = tile.Position.ToVoxel() + new Position(8, 8, 2);
			}
		}
		else
		{
			scanVoxel = tile.Position.ToVoxel() + new Position(8, 8, 12);
		}

		// Secondary LOF check
		if (!seen)
		{
			VoxelType test = CalculateLineVoxel(originVoxel, scanVoxel, false, _trajectory, currentUnit);
			if (test == V_EMPTY)
			{
				// We have hit nothing at all, LOF is clear (Note: _trajectory is empty)
				seen = true;
			}
			else if (test == V_OUTOFBOUNDS)
			{
				// FIXME/TESTME
				seen = false;
			}
			else
			{
				// Let's assume we hit and double-check
				seen = true;

				// inspired by Projectile::calculateTrajectory()
				Position hitPos = _trajectory[0].ToTile();
				if (test == V_UNIT && _save.GetTile(hitPos) != null && _save.GetTile(hitPos).Unit == null) //no unit? must be lower
				{
					hitPos = new Position(hitPos.x, hitPos.y, hitPos.z - 1);
				}

				if (hitPos != tempAction.target /*&& tempAction.result.empty()*/)
				{
					if (test == V_NORTHWALL)
					{
						if (hitPos.y - 1 != tempAction.target.y)
						{
							seen = false;
						}
					}
					else if (test == V_WESTWALL)
					{
						if (hitPos.x - 1 != tempAction.target.x)
						{
							seen = false;
						}
					}
					else if (test == V_UNIT)
					{
						BattleUnit hitUnit = _save.GetTile(hitPos).Unit;
						BattleUnit targetUnit = drawing ? tile.Unit : tile.GetOverlappingUnit(_save);
						if (hitUnit != targetUnit)
						{
							seen = false;
						}
					}
					else
					{
						seen = false;
					}
				}
			}
		}

		// LOS check uses sight origin voxel (LOF check uses origin voxel)
		originVoxel = GetSightOriginVoxel(currentUnit);

		// heat vision 100% = smoke effectiveness 0%
		int smokeDensityFactor = 100 - Clamp(currentUnit.VisibilityThroughSmoke, 0, 100);
		int fireDensityFactor = 100 - Clamp(currentUnit.VisibilityThroughFire, 0, 100);

		if (seen)
		{
			var (visibleDistanceVoxels, densityOfSmoke, densityOfFire, densityOfSmokeNearUnit, densityOfFireeNearUnit) = GetTrajectoryDataHelper(currentUnit, originVoxel, scanVoxel);

			// 3  - coefficient of calculation (see getTrajectoryDataHelper).
			// 20 - maximum view distance in vanilla Xcom.
			// 100 - % for smokeDensityFactor.
			// 16 - for voxel scale calculation.
			// Even if MaxViewDistance will be increased via ruleset, smoke will keep effect.
			int visibilityQuality = visibleDistanceMaxVoxel - visibleDistanceVoxels - ((densityOfSmoke - densityOfSmokeNearUnit / 2) * smokeDensityFactor + (densityOfFire - densityOfFireeNearUnit / 2) * fireDensityFactor) * visibleDistanceMaxVoxel / (3 * 20 * 100 * 16);
			// script hook ModScript::VisibilityUnit (armor) not ported: the unmodified quality decides
			seen = 0 < visibilityQuality;
		}
		return seen;
	}

	/// <summary>Checks for how exposed unit is for another unit.</summary>
	/// <param name="originVoxel">Voxel of trace origin (eye or gun's barrel).</param>
	/// <param name="tile">The tile to check for.</param>
	/// <param name="excludeUnit">Is self (not to hit self).</param>
	/// <param name="excludeAllBut">[Optional] is unit which is the only one to be considered for ray hits.</param>
	/// <returns>Degree of exposure (as percent).</returns>
	public int CheckVoxelExposure(ref Position originVoxel, Tile tile, BattleUnit excludeUnit, BattleUnit excludeAllBut)
	{
		Position targetVoxel = tile.Position.ToVoxel() + new Position(8, 8, 0);
		Position scanVoxel = default;
		var _trajectory = new List<Position>();
		BattleUnit otherUnit = tile.Unit;
		if (otherUnit == null) return 0; //no unit in this tile, even if it elevated and appearing in it.
		if (otherUnit == excludeUnit) return 0; //skip self

		int targetMinHeight = targetVoxel.z - tile.TerrainLevel;
		if (otherUnit != null)
			targetMinHeight += otherUnit.FloatHeight;

		// if there is an other unit on target tile, we assume we want to check against this unit's height
		int heightRange;

		int unitRadius = otherUnit.GetLoftemps(); //width == loft in default loftemps set
		if (otherUnit.IsBigUnit)
		{
			unitRadius = 3;
		}

		// vector manipulation to make scan work in view-space
		Position relPos = targetVoxel - originVoxel;
		// C++ calls the double sqrt here (no float overload in scope)
		float normal = (float)(unitRadius / Math.Sqrt((float)(relPos.x * relPos.x + relPos.y * relPos.y)));
		int relX = (int)Math.Floor(((float)relPos.y) * normal + 0.5);
		int relY = (int)Math.Floor(((float)-relPos.x) * normal + 0.5);

		ReadOnlySpan<int> sliceTargets = [0, 0, relX, relY, -relX, -relY];

		if (!otherUnit.IsOut)
		{
			heightRange = otherUnit.Height;
		}
		else
		{
			heightRange = 12;
		}

		int targetMaxHeight = targetMinHeight + heightRange;
		// scan ray from top to bottom  plus different parts of target cylinder
		int total = 0;
		int visible = 0;
		for (int i = heightRange; i >= 0; i -= 2)
		{
			++total;
			scanVoxel.z = (short)(targetMinHeight + i);
			for (int j = 0; j < 3; ++j)
			{
				scanVoxel.x = (short)(targetVoxel.x + sliceTargets[j * 2]);
				scanVoxel.y = (short)(targetVoxel.y + sliceTargets[j * 2 + 1]);
				_trajectory.Clear();
				VoxelType test = CalculateLineVoxel(originVoxel, scanVoxel, false, _trajectory, excludeUnit, excludeAllBut);
				if (test == V_UNIT)
				{
					//voxel of hit must be inside of scanned box
					if (_trajectory[0].x / 16 == scanVoxel.x / 16 &&
						_trajectory[0].y / 16 == scanVoxel.y / 16 &&
						_trajectory[0].z >= targetMinHeight &&
						_trajectory[0].z <= targetMaxHeight)
					{
						++visible;
					}
				}
			}
		}
		return (visible * 100) / total;
	}

	/// <summary>Checks for another unit available for targeting and what particular voxel.</summary>
	/// <param name="originVoxel">Voxel of trace origin (eye or gun's barrel).</param>
	/// <param name="tile">The tile to check for.</param>
	/// <param name="scanVoxel">is returned coordinate of hit.</param>
	/// <param name="excludeUnit">is self (not to hit self).</param>
	/// <param name="rememberObstacles">Remember obstacles for no LOF indicator?</param>
	/// <param name="potentialUnit">is a hypothetical unit to draw a virtual line of fire for AI. if left blank, this function behaves normally.</param>
	/// <returns>True if the unit can be targetted.</returns>
	public bool CanTargetUnit(ref Position originVoxel, Tile tile, ref Position scanVoxel, BattleUnit excludeUnit, bool rememberObstacles, BattleUnit potentialUnit = null)
	{
		Position targetVoxel = tile.Position.ToVoxel() + new Position(8, 8, 0);
		var _trajectory = new List<Position>();
		bool hypothetical = potentialUnit != null;
		if (potentialUnit == null)
		{
			potentialUnit = tile.Unit;
			if (potentialUnit == null) return false; //no unit in this tile, even if it elevated and appearing in it.
		}

		if (potentialUnit == excludeUnit) return false; //skip self

		int targetMinHeight = targetVoxel.z - tile.TerrainLevel;
		targetMinHeight += potentialUnit.FloatHeight;

		int targetMaxHeight = targetMinHeight;
		int targetCenterHeight;
		// if there is an other unit on target tile, we assume we want to check against this unit's height
		int heightRange;

		int unitRadius = potentialUnit.GetLoftemps(); //width == loft in default loftemps set
		int targetSize = potentialUnit.Armor.Size - 1;
		int xOffset = potentialUnit.Position.x - tile.Position.x;
		int yOffset = potentialUnit.Position.y - tile.Position.y;
		if (targetSize > 0)
		{
			unitRadius = 3;
		}
		// vector manipulation to make scan work in view-space
		Position relPos = targetVoxel - originVoxel;
		// C++ calls the double sqrt here (no float overload in scope)
		float normal = (float)(unitRadius / Math.Sqrt((float)(relPos.x * relPos.x + relPos.y * relPos.y)));
		int relX = (int)Math.Floor(((float)relPos.y) * normal + 0.5);
		int relY = (int)Math.Floor(((float)-relPos.x) * normal + 0.5);

		ReadOnlySpan<int> sliceTargets = [0, 0, relX, relY, -relX, -relY, relY, -relX, -relY, relX];

		if (!potentialUnit.IsOut)
		{
			heightRange = potentialUnit.Height;
		}
		else
		{
			heightRange = 12;
		}

		targetMaxHeight += heightRange;
		targetCenterHeight = (targetMaxHeight + targetMinHeight) / 2;
		heightRange /= 2;
		if (heightRange > 10) heightRange = 10;
		if (heightRange <= 0) heightRange = 0;

		// scan ray from top to bottom  plus different parts of target cylinder
		for (int i = 0; i <= heightRange; ++i)
		{
			scanVoxel.z = (short)(targetCenterHeight + heightFromCenter[i]);
			for (int j = 0; j < 5; ++j)
			{
				if (i < (heightRange - 1) && j > 2) break; //skip unnecessary checks
				scanVoxel.x = (short)(targetVoxel.x + sliceTargets[j * 2]);
				scanVoxel.y = (short)(targetVoxel.y + sliceTargets[j * 2 + 1]);
				_trajectory.Clear();
				VoxelType test = CalculateLineVoxel(originVoxel, scanVoxel, false, _trajectory, excludeUnit);
				if (test == V_UNIT)
				{
					for (int x = 0; x <= targetSize; ++x)
					{
						for (int y = 0; y <= targetSize; ++y)
						{
							//voxel of hit must be inside of scanned box
							if (_trajectory[0].x / 16 == (scanVoxel.x / 16) + x + xOffset &&
								_trajectory[0].y / 16 == (scanVoxel.y / 16) + y + yOffset &&
								_trajectory[0].z >= targetMinHeight &&
								_trajectory[0].z <= targetMaxHeight)
							{
								return true;
							}
						}
					}
				}
				else if (test == V_EMPTY && hypothetical && _trajectory.Count != 0)
				{
					return true;
				}
				if (rememberObstacles && _trajectory.Count > 0)
				{
					Tile tileObstacle = _save.GetTile(_trajectory[0].ToTile());
					tileObstacle?.SetObstacle((int)test);
				}
			}
		}
		return false;
	}

	private static readonly int[] sliceObjectSpiral = [8,8, 8,6, 10,6, 10,8, 10,10, 8,10, 6,10, 6,8, 6,6, //first circle
		8,4, 10,4, 12,4, 12,6, 12,8, 12,10, 12,12, 10,12, 8,12, 6,12, 4,12, 4,10, 4,8, 4,6, 4,4, 6,4, //second circle
		8,1, 12,1, 15,1, 15,4, 15,8, 15,12, 15,15, 12,15, 8,15, 4,15, 1,15, 1,12, 1,8, 1,4, 1,1, 4,1]; //third circle
	private static readonly int[] westWallSpiral = [0,7, 0,9, 0,6, 0,11, 0,4, 0,13, 0,2];
	private static readonly int[] northWallSpiral = [7,0, 9,0, 6,0, 11,0, 4,0, 13,0, 2,0];

	/// <summary>Checks for a tile part available for targeting and what particular voxel.</summary>
	/// <param name="originVoxel">Voxel of trace origin (gun's barrel).</param>
	/// <param name="tile">The tile to check for.</param>
	/// <param name="part">Tile part to check for.</param>
	/// <param name="scanVoxel">Is returned coordinate of hit.</param>
	/// <param name="excludeUnit">Is self (not to hit self).</param>
	/// <param name="rememberObstacles">Remember obstacles for no LOF indicator?</param>
	/// <returns>True if the tile can be targetted.</returns>
	public bool CanTargetTile(ref Position originVoxel, Tile tile, int part, ref Position scanVoxel, BattleUnit excludeUnit, bool rememberObstacles)
	{
		var targetVoxel = new Position((tile.Position.x * 16), (tile.Position.y * 16), tile.Position.z * 24);
		var _trajectory = new List<Position>();

		int[] spiralArray;
		int spiralCount;

		int minZ = 0, maxZ = 0;
		bool minZfound = false, maxZfound = false;
		bool dummy = false;

		if (part == (int)O_OBJECT)
		{
			spiralArray = sliceObjectSpiral;
			spiralCount = 41;
		}
		else
		if (part == (int)O_NORTHWALL)
		{
			spiralArray = northWallSpiral;
			spiralCount = 7;
		}
		else
		if (part == (int)O_WESTWALL)
		{
			spiralArray = westWallSpiral;
			spiralCount = 7;
		}
		else if (part == (int)O_FLOOR)
		{
			spiralArray = sliceObjectSpiral;
			spiralCount = 41;
			minZfound = true; minZ = 0;
			maxZfound = true; maxZ = 0;
		}
		else if (part == MapData.O_DUMMY) // used only for no line of fire indicator
		{
			spiralArray = sliceObjectSpiral;
			spiralCount = 41;
			minZfound = true; minZ = 12;
			maxZfound = true; maxZ = 12;
		}
		else
		{
			return false;
		}
		VoxelCheckFlush();
		// find out height range

		if (!minZfound)
		{
			for (int j = 1; j < 12; ++j)
			{
				if (minZfound) break;
				for (int i = 0; i < spiralCount; ++i)
				{
					int tX = spiralArray[i * 2];
					int tY = spiralArray[i * 2 + 1];
					if ((int)VoxelCheck(new Position(targetVoxel.x + tX, targetVoxel.y + tY, targetVoxel.z + j * 2), null, true) == part) //bingo
					{
						if (!minZfound)
						{
							minZ = j * 2;
							minZfound = true;
							break;
						}
					}
				}
			}
		}

		if (!minZfound)
		{
			if (rememberObstacles)
			{
				// dummy attempt (only to highlight obstacles)
				minZfound = true;
				minZ = 10;
				dummy = true;
			}
			else
			{
				return false;//empty object!!!
			}
		}

		if (!maxZfound)
		{
			for (int j = 10; j >= 0; --j)
			{
				if (maxZfound) break;
				for (int i = 0; i < spiralCount; ++i)
				{
					int tX = spiralArray[i * 2];
					int tY = spiralArray[i * 2 + 1];
					if ((int)VoxelCheck(new Position(targetVoxel.x + tX, targetVoxel.y + tY, targetVoxel.z + j * 2), null, true) == part) //bingo
					{
						if (!maxZfound)
						{
							maxZ = j * 2;
							maxZfound = true;
							break;
						}
					}
				}
			}
		}

		if (!maxZfound)
		{
			if (rememberObstacles)
			{
				// dummy attempt (only to highlight obstacles)
				maxZfound = true;
				maxZ = 10;
				dummy = true;
			}
			else
			{
				return false;//it's impossible to get there
			}
		}

		if (minZ > maxZ) minZ = maxZ;
		int rangeZ = maxZ - minZ;
		if (rangeZ > 10) rangeZ = 10; //as above, clamping height range to prevent buffer overflow
		int centerZ = (maxZ + minZ) / 2;

		for (int j = 0; j <= rangeZ; ++j)
		{
			scanVoxel.z = (short)(targetVoxel.z + centerZ + heightFromCenter[j]);
			for (int i = 0; i < spiralCount; ++i)
			{
				scanVoxel.x = (short)(targetVoxel.x + spiralArray[i * 2]);
				scanVoxel.y = (short)(targetVoxel.y + spiralArray[i * 2 + 1]);
				_trajectory.Clear();
				VoxelType test = CalculateLineVoxel(originVoxel, scanVoxel, false, _trajectory, excludeUnit);
				if ((int)test == part && !dummy) //bingo
				{
					if (_trajectory[0].x / 16 == scanVoxel.x / 16 &&
						_trajectory[0].y / 16 == scanVoxel.y / 16 &&
						_trajectory[0].z / 24 == scanVoxel.z / 24)
					{
						return true;
					}
				}
				if (rememberObstacles && _trajectory.Count > 0)
				{
					Tile tileObstacle = _save.GetTile(_trajectory[0].ToTile());
					tileObstacle?.SetObstacle((int)test);
				}
			}
		}
		return false;
	}

	/// <summary>
	/// Calculates line of sight of a soldiers within range of the Position
	/// (used when terrain has changed, which can reveal new parts of terrain or units).
	/// </summary>
	/// <param name="position">Position of the changed terrain.</param>
	/// <param name="eventRadius">Radius of circle big enough to encompass the event.</param>
	/// <param name="updateTiles">true to do an update of visible tiles.</param>
	/// <param name="appendToTileVisibility">true to append only new tiles and skip previously seen ones.</param>
	public void CalculateFOV(Position position, int eventRadius = -1, bool updateTiles = true, bool appendToTileVisibility = false)
	{
		int updateRadius;
		if (eventRadius == -1)
		{
			eventRadius = MaxViewDistance;
			updateRadius = MaxViewDistanceSq;
		}
		else
		{
			//Need to grab units which are out of range of the centre of the event, but can still see the edge of the effect.
			updateRadius = MaxViewDistance + (eventRadius > 0 ? eventRadius : 0);
			updateRadius *= updateRadius;
		}
		foreach (BattleUnit bu in _save.Units)
		{
			if (Position.Distance2dSq(position, bu.Position) <= updateRadius) //could this unit have observed the event?
			{
				if (updateTiles)
				{
					if (!appendToTileVisibility)
					{
						bu.ClearVisibleTiles();
					}
					CalculateTilesInFOV(bu, position, eventRadius);
				}

				CalculateUnitsInFOV(bu, position, eventRadius);
			}
		}
	}

	/// <summary>Recalculates FOV of all units in-game.</summary>
	public void RecalculateFOV()
	{
		foreach (BattleUnit bu in _save.Units)
		{
			if (bu.Tile != null)
			{
				CalculateFOV(bu);
			}
		}
	}

	/// <summary>Calculates a line trajectory, using bresenham algorithm in 3D.</summary>
	/// <param name="origin">Origin tile.</param>
	/// <param name="target">Target tile.</param>
	/// <param name="trajectory">A vector of positions in which the trajectory is stored.</param>
	/// <returns>0 or some value greater than 127 (256) when the line is blocked.</returns>
	public int CalculateLineTile(Position origin, Position target, List<Position> trajectory)
	{
		Position lastPoint = origin;
		int steps = 0;

		bool hit = CalculateLineHelper(origin, target,
			point =>
			{
				trajectory.Add(point);

				Position difference = point - lastPoint;
				int dir = Pathfinding.VectorToDirection(difference);
				VisibilityBlockCache cache = _blockVisibility[_save.GetTileIndex(lastPoint)];

				bool result = GetBlockDir(cache, dir, difference.z);
				if (result && difference.z == 0 && GetBigWallDir(cache, dir))
				{
					if (point == target)
					{
						result = false;
					}
				}

				steps++;
				lastPoint = point;
				return result;
			},
			point =>
			{
				return false;
			}
		);
		if (hit)
		{
			return 256;
		}
		return 0;
	}

	/// <summary>Calculates a line trajectory, using bresenham algorithm in 3D.</summary>
	/// <param name="origin">Origin in voxel.</param>
	/// <param name="target">Target in voxel.</param>
	/// <param name="storeTrajectory">True will store the whole trajectory - otherwise it just stores the last position.</param>
	/// <param name="trajectory">A vector of positions in which the trajectory is stored.</param>
	/// <param name="excludeUnit">Excludes this unit in the collision detection.</param>
	/// <param name="excludeAllBut">[Optional] The only unit to be considered for ray hits.</param>
	/// <param name="onlyVisible">Skip invisible units? used in FPS view.</param>
	/// <returns>the objectnumber(0-3) or unit(4) or out of map (5) or -1(hit nothing).</returns>
	public VoxelType CalculateLineVoxel(Position origin, Position target, bool storeTrajectory, List<Position> trajectory, BattleUnit excludeUnit, BattleUnit excludeAllBut = null, bool onlyVisible = false)
	{
		VoxelType result = V_EMPTY;
		bool excludeAllUnits = false;
		if (_save.IsBeforeGame)
		{
			excludeAllUnits = true; // don't start unit spotting before pre-game inventory stuff (large units on the craftInventory tile will cause a crash if they're "spotted")
		}

		bool hit = CalculateLineHelper(origin, target,
			point =>
			{
				if (storeTrajectory && trajectory != null)
				{
					trajectory.Add(point);
				}

				result = VoxelCheck(point, excludeUnit, excludeAllUnits, onlyVisible, excludeAllBut);
				if (result != V_EMPTY)
				{
					if (trajectory != null)
					{ // store the position of impact
						trajectory.Add(point);
					}
					return true;
				}
				return false;
			},
			point =>
			{
				//check for xy diagonal intermediate voxel step
				result = VoxelCheck(point, excludeUnit, excludeAllUnits, onlyVisible, excludeAllBut);
				if (result != V_EMPTY)
				{
					if (trajectory != null)
					{ // store the position of impact
						trajectory.Add(point);
					}
					return true;
				}
				return false;
			}
		);
		if (hit)
		{
			return result;
		}
		return V_EMPTY;
	}

	/// <summary>Calculates a parabola trajectory, used for throwing items.</summary>
	/// <param name="origin">Origin in voxelspace.</param>
	/// <param name="target">Target in voxelspace.</param>
	/// <param name="storeTrajectory">True will store the whole trajectory - otherwise it just stores the last position.</param>
	/// <param name="trajectory">A vector of positions in which the trajectory is stored.</param>
	/// <param name="excludeUnit">Makes sure the trajectory does not hit the shooter itself.</param>
	/// <param name="curvature">How high the parabola goes: 1.0 is almost straight throw, 3.0 is a very high throw, to throw over a fence for example.</param>
	/// <param name="delta">Is the deviation of the angles it should take into account, 0,0,0 is perfection.</param>
	/// <returns>The objectnumber(0-3) or unit(4) or out of map (5) or -1(hit nothing).</returns>
	public int CalculateParabolaVoxel(Position origin, Position target, bool storeTrajectory, List<Position> trajectory, BattleUnit excludeUnit, double curvature, Position delta)
	{
		if (target == origin) return (int)V_EMPTY;//just in case

		int result = (int)V_EMPTY;
		Position lastPosition = origin;
		Position nextPosition = lastPosition;

		if (storeTrajectory && trajectory != null)
		{
			//initla value for small hack to glue `calculateLineVoxel` into one continuous arc
			trajectory.Add(lastPosition);
		}

		CalculateParabolaHelper(origin, target, curvature, delta,
			p =>
			{
				//passes through this point?
				nextPosition = p;

				if (storeTrajectory && trajectory != null)
				{
					//remove end point of previous trajectory part, because next one will add this point again
					trajectory.RemoveAt(trajectory.Count - 1);
				}
				result = (int)CalculateLineVoxel(lastPosition, nextPosition, storeTrajectory, storeTrajectory ? trajectory : null, excludeUnit);
				if (result != (int)V_EMPTY)
				{
					if (!storeTrajectory && trajectory != null)
					{
						result = (int)CalculateLineVoxel(lastPosition, nextPosition, false, trajectory, excludeUnit); //pick the INSIDE position of impact
					}
					return true;
				}
				lastPosition = nextPosition;
				return false;
			}
		);

		return result;
	}

	/// <summary>Calculates z "grounded" value for a particular voxel (used for projectile shadow).</summary>
	/// <param name="voxel">The voxel to trace down.</param>
	/// <returns>z coord of "ground".</returns>
	public int CastedShade(Position voxel)
	{
		int zstart = voxel.z;
		Position tmpCoord = voxel.ToTile();
		Tile t = _save.GetTile(tmpCoord);
		while (t != null && t.IsVoid && t.Unit == null)
		{
			zstart = tmpCoord.z * 24;
			--tmpCoord.z;
			t = _save.GetTile(tmpCoord);
		}

		Position tmpVoxel = voxel;
		int z;

		VoxelCheckFlush();
		for (z = zstart; z > 0; z--)
		{
			tmpVoxel.z = (short)z;
			if (VoxelCheck(tmpVoxel, null) != V_EMPTY) break;
		}
		return z;
	}

	/// <summary>Traces voxel visibility.</summary>
	/// <param name="voxel">Voxel coordinates.</param>
	/// <returns>True if visible.</returns>
	public bool IsVoxelVisible(Position voxel)
	{
		int zstart = voxel.z + 3; // slight Z adjust
		if ((zstart / 24) != (voxel.z / 24))
			return true; // visible!
		Position tmpVoxel = voxel;
		int zend = (zstart / 24) * 24 + 24;

		VoxelCheckFlush();
		for (int z = zstart; z < zend; z++)
		{
			tmpVoxel.z = (short)z;
			// only OBJECT can cause additional occlusion (because of any shape)
			if (VoxelCheck(tmpVoxel, null) == V_OBJECT) return false;
			++tmpVoxel.x;
			if (VoxelCheck(tmpVoxel, null) == V_OBJECT) return false;
			++tmpVoxel.y;
			if (VoxelCheck(tmpVoxel, null) == V_OBJECT) return false;
		}
		return true;
	}

	private static readonly int[] voxelCheckBigUnitParts = [1, 0, 3, 2]; // Change order 0,1,2,3 -> 1,0,3,2  (read commit description)

	/// <summary>Checks if we hit a voxel.</summary>
	/// <param name="voxel">The voxel to check.</param>
	/// <param name="excludeUnit">Don't do checks on this unit.</param>
	/// <param name="excludeAllUnits">Don't do checks on any unit.</param>
	/// <param name="onlyVisible">Whether to consider only visible units.</param>
	/// <param name="excludeAllBut">If set, the only unit to be considered for ray hits.</param>
	/// <returns>The objectnumber(0-3) or unit(4) or out of map (5) or -1 (hit nothing).</returns>
	public VoxelType VoxelCheck(Position voxel, BattleUnit excludeUnit, bool excludeAllUnits = false, bool onlyVisible = false, BattleUnit excludeAllBut = null)
	{
		if (voxel.x < 0 || voxel.y < 0 || voxel.z < 0) //preliminary out of map
		{
			return V_OUTOFBOUNDS;
		}
		Position pos = voxel.ToTile();
		Tile tile, tileBelow;
		if (_cacheTilePos == pos)
		{
			tile = _cacheTile;
			tileBelow = _cacheTileBelow;
		}
		else
		{
			tile = _save.GetTile(pos);
			if (tile == null) // check if we are not out of the map
			{
				return V_OUTOFBOUNDS; //not even cache
			}
			tileBelow = _save.GetBelowTile(tile);
			_cacheTilePos = pos;
			_cacheTile = tile;
			_cacheTileBelow = tileBelow;
		}

		if (tile.IsVoid && tile.Unit == null && (tileBelow == null || tileBelow.Unit == null))
		{
			return V_EMPTY;
		}

		if (tile.HasGravLiftFloor && (voxel.z % 24 == 0 || voxel.z % 24 == 1))
		{
			if (!(tileBelow != null && tileBelow.HasGravLiftFloor))
			{
				return V_FLOOR;
			}
		}

		// first we check terrain voxel data, not to allow 2x2 units stick through walls
		for (int i = (int)V_FLOOR; i <= (int)V_OBJECT; ++i)
		{
			TilePart tp = (TilePart)i;
			MapData mp = tile.GetMapData(tp);
			if (((tp == O_WESTWALL) || (tp == O_NORTHWALL)) && tile.IsUfoDoorOpen(tp))
				continue;
			if (mp != null)
			{
				int x = 15 - voxel.x % 16;
				int y = voxel.y % 16;
				int idx = (mp.GetLoftID((voxel.z % 24) / 2) * 16) + y;
				if ((_voxelData[idx] & (1 << x)) != 0)
				{
					return (VoxelType)i;
				}
			}
		}

		if (!excludeAllUnits)
		{
			BattleUnit unit = tile.GetOverlappingUnit(_save);

			if (unit != null && !unit.IsOut && unit != excludeUnit && (excludeAllBut == null || unit == excludeAllBut) && (!onlyVisible || unit.Visible))
			{
				Position tilepos;
				Position unitpos = unit.Position;
				int terrainHeight = 0;
				for (int x = 0; x < unit.Armor.Size; ++x)
				{
					for (int y = 0; y < unit.Armor.Size; ++y)
					{
						Tile tempTile = _save.GetTile(unitpos + new Position(x, y, 0));
						if (tempTile.TerrainLevel < terrainHeight)
						{
							terrainHeight = tempTile.TerrainLevel;
						}
					}
				}
				int tz = unitpos.z * 24 + unit.FloatHeight - terrainHeight; //bottom most voxel, terrain heights are negative, so we subtract.
				if ((voxel.z > tz) && (voxel.z <= tz + unit.Height))
				{
					int x = 15 - voxel.x % 16;
					int y = voxel.y % 16;
					int part = 0;
					if (unit.IsBigUnit)
					{
						tilepos = tile.Position;
						part = voxelCheckBigUnitParts[tilepos.x - unitpos.x + (tilepos.y - unitpos.y) * 2];
					}
					int idx = (unit.GetLoftemps(part) * 16) + y;
					if ((_voxelData[idx] & (1 << x)) != 0)
					{
						return V_UNIT;
					}
				}
			}
		}
		return V_EMPTY;
	}

	/// <summary>Flushes cache of voxel check.</summary>
	public void VoxelCheckFlush()
	{
		_cacheTilePos = invalid;
		_cacheTile = null;
		_cacheTileBelow = null;
	}

	/// <summary>Returns the direction from origin to target.</summary>
	/// <param name="origin">The origin point of the action.</param>
	/// <param name="target">The target point of the action.</param>
	/// <returns>direction.</returns>
#pragma warning disable CA1822 // instance member in C++, called through the TileEngine instance
	public int GetDirectionTo(Position origin, Position target)
#pragma warning restore CA1822
	{
		return DirectionTo(origin, target);
	}

	private const double M_PI_4 = 0.785398163397448309616;

	/// <summary>Calculation of <see cref="GetDirectionTo"/> (the C++ method uses no state).</summary>
	internal static int DirectionTo(Position origin, Position target)
	{
		double ox = target.x - origin.x;
		double oy = target.y - origin.y;
		double angle = Math.Atan2(ox, -oy);
		// divide the pie in 4 angles each at 1/8th before each quarter
		ReadOnlySpan<double> pie = [(M_PI_4 * 4.0) - M_PI_4 / 2.0, (M_PI_4 * 3.0) - M_PI_4 / 2.0, (M_PI_4 * 2.0) - M_PI_4 / 2.0, (M_PI_4 * 1.0) - M_PI_4 / 2.0];
		int dir = 0;

		if (angle > pie[0] || angle < -pie[0])
		{
			dir = 4;
		}
		else if (angle > pie[1])
		{
			dir = 3;
		}
		else if (angle > pie[2])
		{
			dir = 2;
		}
		else if (angle > pie[3])
		{
			dir = 1;
		}
		else if (angle < -pie[1])
		{
			dir = 5;
		}
		else if (angle < -pie[2])
		{
			dir = 6;
		}
		else if (angle < -pie[3])
		{
			dir = 7;
		}
		else if (angle < pie[0])
		{
			dir = 0;
		}
		return dir;
	}

	/// <summary>Calculate arc between two unit directions. e.g. Arc of 7 and 3 is 4. Arc of 0 and 7 is 1.</summary>
	/// <param name="directionA">Value of 0 to 7.</param>
	/// <param name="directionB">Value of 0 to 7.</param>
	/// <returns>Value in range of 0 - 4.</returns>
#pragma warning disable CA1822 // instance member in C++, called through the TileEngine instance
	public int GetArcDirection(int directionA, int directionB)
#pragma warning restore CA1822
	{
		return ArcDirection(directionA, directionB);
	}

	/// <summary>Calculation of <see cref="GetArcDirection"/> (the C++ method uses no state).</summary>
	internal static int ArcDirection(int directionA, int directionB)
	{
		return Math.Abs((((directionA - directionB) + 12) % 8) - 4);
	}

	private static readonly int[] originDirYshift = [1, 1, 8, 15, 15, 15, 8, 1];
	private static readonly int[] originDirXshift = [8, 14, 15, 15, 8, 1, 1, 1];

	/// <summary>Gets the origin voxel of a certain action.</summary>
	/// <param name="action">Battle action.</param>
	/// <param name="tile">Pointer to the action tile.</param>
	/// <returns>origin position.</returns>
	public Position GetOriginVoxel(BattleAction action, Tile tile)
	{
		int[] dirYshift = originDirYshift;
		int[] dirXshift = originDirXshift;
		if (tile == null)
		{
			tile = action.actor.Tile;
		}

		Position origin = tile.Position;
		Tile tileAbove = _save.GetTile(origin + new Position(0, 0, 1));
		var originVoxel = new Position(origin.x * 16, origin.y * 16, origin.z * 24);

		// take into account soldier height and terrain level if the projectile is launched from a soldier
		if (action.actor.Position == origin || action.type != BA_LAUNCH)
		{
			// calculate offset of the starting point of the projectile
			originVoxel.z = (short)(originVoxel.z + -tile.TerrainLevel);

			originVoxel.z = (short)(originVoxel.z + action.actor.Height + action.actor.FloatHeight);

			if (action.type == BA_THROW)
			{
				originVoxel.z -= 3;
			}
			else
			{
				originVoxel.z -= 4;
			}

			if (originVoxel.z >= (origin.z + 1) * 24)
			{
				if (tileAbove != null && tileAbove.HasNoFloor(null))
				{
					origin.z++;
				}
				else
				{
					while (originVoxel.z >= (origin.z + 1) * 24)
					{
						originVoxel.z--;
					}
					originVoxel.z -= 4;
				}
			}
			int direction = GetDirectionTo(origin, action.target);
			int size = action.actor.Armor.Size;

			// Offset for different relativeOrigin values
			switch (action.relativeOrigin)
			{
				case BattleActionOrigin.CENTRE:
					// Standard offset.
					originVoxel.x = (short)(originVoxel.x + dirXshift[direction] * size);
					originVoxel.y = (short)(originVoxel.y + dirYshift[direction] * size);
					break;

				// 2:1 Weighted average of the standard offset and a rotation, either left or right.
				case BattleActionOrigin.LEFT:
					originVoxel.x = (short)(originVoxel.x + ((2 * dirXshift[direction] + dirXshift[(direction + 7) % 8]) * size + 1) / 3);
					originVoxel.y = (short)(originVoxel.y + ((2 * dirYshift[direction] + dirYshift[(direction + 7) % 8]) * size + 1) / 3);
					break;

				case BattleActionOrigin.RIGHT:
					originVoxel.x = (short)(originVoxel.x + ((2 * dirXshift[direction] + dirXshift[(direction + 1) % 8]) * size + 1) / 3);
					originVoxel.y = (short)(originVoxel.y + ((2 * dirYshift[direction] + dirYshift[(direction + 1) % 8]) * size + 1) / 3);
					break;
			}
		}
		else
		{
			// don't take into account soldier height and terrain level if the projectile is not launched from a soldier(from a waypoint)
			originVoxel.x += 8;
			originVoxel.y += 8;
			originVoxel.z += 16;
		}
		return originVoxel;
	}

	/// <summary>Mark a region of the map as "dangerous" for a turn.</summary>
	/// <param name="pos">is the epicenter of the explosion.</param>
	/// <param name="radius">how far to spread out.</param>
	/// <param name="unit">the unit that is triggering this action.</param>
	public void SetDangerZone(Position pos, int radius, BattleUnit unit)
	{
		Tile tile = _save.GetTile(pos);
		if (tile == null)
		{
			return;
		}
		// set the epicenter as dangerous
		tile.Dangerous = true;
		Position originVoxel = pos.ToVoxel() + new Position(8, 8, 12 + -tile.TerrainLevel);
		Position targetVoxel;
		for (int x = -radius; x != radius; ++x)
		{
			for (int y = -radius; y != radius; ++y)
			{
				// we can skip the epicenter
				if (x != 0 || y != 0)
				{
					// make sure we're within the radius
					if ((x * x) + (y * y) <= (radius * radius))
					{
						tile = _save.GetTile(pos + new Position(x, y, 0));
						if (tile != null)
						{
							targetVoxel = ((pos + new Position(x, y, 0)).ToVoxel()) + new Position(8, 8, 12 + -tile.TerrainLevel);
							var trajectory = new List<Position>();
							// we'll trace a line here, ignoring all units, to check if the explosion will reach this point
							// granted this won't properly account for explosions tearing through walls, but then we can't really
							// know that kind of information before the fact, so let's have the AI assume that the wall (or tree)
							// is enough to protect them.
							if (CalculateLineVoxel(originVoxel, targetVoxel, false, trajectory, unit, unit) == V_EMPTY)
							{
								if (trajectory.Count != 0 && (trajectory[^1].ToTile()) == pos + new Position(x, y, 0))
								{
									tile.Dangerous = true;
								}
							}
						}
					}
				}
			}
		}
	}
}
