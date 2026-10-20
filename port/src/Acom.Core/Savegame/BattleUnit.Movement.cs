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
// Ported from: src/Savegame/BattleUnit.h, src/Savegame/BattleUnit.cpp
// Port status: partial (missing: script hooks)
#nullable disable

using static Acom.MovementType;
using static Acom.UnitStatus;

namespace Acom;

// Position, direction, walking, turning, falling, tile links and visibility.
public partial class BattleUnit
{
	/// <summary>Calculates the distance squared between the unit and a given position.</summary>
	/// <param name="pos">The position.</param>
	/// <returns>Distance squared.</returns>
	public int Distance3dToPositionSq(Position pos)
	{
		int x = _pos.x - pos.x;
		int y = _pos.y - pos.y;
		int z = _pos.z - pos.z;
		if (IsBigUnit)
		{
			if (_pos.x < pos.x)
				x++;
			if (_pos.y < pos.y)
				y++;
		}
		return x * x + y * y + z * z;
	}

	/// <summary>Calculates the distance squared between the unit and a given other unit.</summary>
	/// <param name="otherUnit">The other unit.</param>
	/// <returns>Distance squared.</returns>
	public int Distance3dToUnitSq(BattleUnit otherUnit)
	{
		// TODO?: distance calculation isn't precise for 2x2 units here
		// and even though improving just the distance calculation would be easy
		// it would also require changes on other places:
		// - we would need to improve AI targeting involving 2x2 units
		// - we would need to improve reaction fire targeting involving 2x2 units
		// and that is NOT trivial and currently not worth the effort
		// PS: targeting involving a player-controlled 2x2 unit/actor was fixed recently, OXC PR #1307 commit dd7f938

		return Position.DistanceSq(_pos, otherUnit.Position);
	}

	/// <summary>Changes the BattleUnit's position.</summary>
	/// <param name="pos">position</param>
	/// <param name="updateLastPos">refresh last stored position</param>
	public void SetPosition(Position pos, bool updateLastPos = true)
	{
		if (updateLastPos) { _lastPos = _pos; }
		_pos = pos;
	}

	/// <summary>Gets the BattleUnit's position.</summary>
	public Position Position => _pos;

	/// <summary>Gets the BattleUnit's last position.</summary>
	public Position LastPosition => _lastPos;

	/// <summary>Gets position of unit center in voxels.</summary>
	public Position PositionVexels
	{
		get
		{
			Position center = _pos.ToVoxel();
			center += new Position(8, 8, 0) * _armor.Size;
			return center;
		}
	}

	/// <summary>Gets the BattleUnit's destination.</summary>
	public Position Destination => _destination;

	/// <summary>
	/// Gets or changes the BattleUnit's (horizontal) direction 0-7.
	/// Setting is only used for initial unit placement (also sets the turret direction).
	/// </summary>
	public int Direction
	{
		get => _direction;
		set
		{
			_direction = value;
			_toDirection = value;
			_directionTurret = value;
			_toDirectionTurret = value;
		}
	}

	/// <summary>Gets or changes the BattleUnit's (horizontal) face direction. Only used by strafing moves.</summary>
	public int FaceDirection
	{
		get => _faceDirection;
		set => _faceDirection = value;
	}

	/// <summary>Gets the BattleUnit's turret direction.</summary>
	public int TurretDirection => _directionTurret;

	/// <summary>Gets the BattleUnit's turret To direction.</summary>
	public int TurretToDirection => _toDirectionTurret;

	/// <summary>Gets the BattleUnit's vertical direction. This is when going up or down.</summary>
	public int VerticalDirection => _verticalDirection;

	/// <summary>Gets the unit's status.</summary>
	public UnitStatus Status => _status;

	/// <summary>Initialises variables to start walking.</summary>
	/// <param name="direction">Which way to walk.</param>
	/// <param name="destination">The position we should end up on.</param>
	/// <param name="savedBattleGame">Which is used to get tile is currently below the unit.</param>
	public void StartWalking(int direction, Position destination, SavedBattleGame savedBattleGame)
	{
		if (direction >= Pathfinding.DIR_UP)
		{
			_verticalDirection = direction;
			_status = STATUS_FLYING;
		}
		else
		{
			_direction = direction;
			_status = STATUS_WALKING;
		}
		if (_haveNoFloorBelow || direction >= Pathfinding.DIR_UP)
		{
			_status = STATUS_FLYING;
			_floating = true;
		}
		else
		{
			_floating = false;
		}

		_walkPhase = 0;
		_destination = destination;
		_lastPos = _pos;
		_kneeled = false;
		if (_breathFrame >= 0)
		{
			_breathing = false;
			_breathFrame = 0;
		}
	}

	/// <summary>This will increment the walking phase.</summary>
	/// <param name="savedBattleGame">Pointer to save to get tile currently below the unit.</param>
	/// <param name="fullWalkCycle">Do full walk cycle or short one when unit is off screen.</param>
	public void KeepWalking(SavedBattleGame savedBattleGame, bool fullWalkCycle)
	{
		int middle, end;
		if (_verticalDirection != 0)
		{
			middle = 4;
			end = 8;
		}
		else
		{
			// diagonal walking takes double the steps
			middle = 4 + 4 * (_direction % 2);
			end = 8 + 8 * (_direction % 2);
			if (IsBigUnit)
			{
				if (_direction < 1 || _direction > 5)
					middle = end;
				else if (_direction == 5)
					middle = 12;
				else if (_direction == 1)
					middle = 5;
				else
					middle = 1;
			}
		}

		if (!fullWalkCycle)
		{
			_pos = _destination;
			end = 2;
		}

		_walkPhase++;

		if (_walkPhase == middle)
		{
			// we assume we reached our destination tile
			// this is actually a drawing hack, so soldiers are not overlapped by floor tiles
			_pos = _destination;
		}

		if (!fullWalkCycle || (_walkPhase == middle))
		{
			SetTile(savedBattleGame.GetTile(_destination), savedBattleGame);
		}

		if (_walkPhase >= end)
		{
			if (_floating && !_haveNoFloorBelow)
			{
				_floating = false;
			}
			// we officially reached our destination tile
			_status = STATUS_STANDING;
			_walkPhase = 0;
			_verticalDirection = 0;
			if (_faceDirection >= 0)
			{
				// Finish strafing move facing the correct way.
				_direction = _faceDirection;
				_faceDirection = -1;
			}

			// motion points calculation for the motion scanner blips
			if (IsBigUnit)
			{
				_motionPoints += 30;
			}
			else
			{
				// sectoids actually have less motion points
				// but instead of create yet another variable,
				// I used the height of the unit instead (logical)
				if (StandHeight > 16)
					_motionPoints += 4;
				else
					_motionPoints += 3;
			}
		}
	}

	/// <summary>Gets the walking phase for animation and sound (always 0-7).</summary>
	public int WalkingPhase => _walkPhase % 8;

	/// <summary>Gets the walking phase for diagonal walking (0 or 8).</summary>
	public int DiagonalWalkingPhase => (_walkPhase / 8) * 8;

	/// <summary>Look at a point.</summary>
	/// <param name="point">Position to look at.</param>
	/// <param name="turret">True to turn the turret, false to turn the unit.</param>
	public void LookAt(Position point, bool turret = false)
	{
		int dir = DirectionTo(point);

		if (turret)
		{
			_toDirectionTurret = dir;
			if (_toDirectionTurret != _directionTurret)
			{
				_status = STATUS_TURNING;
			}
		}
		else
		{
			_toDirection = dir;
			if (_toDirection != _direction
				&& _toDirection < 8
				&& _toDirection > -1)
			{
				_status = STATUS_TURNING;
			}
		}
	}

	/// <summary>Look at a direction.</summary>
	/// <param name="direction">Direction to look at.</param>
	/// <param name="force">True to reset the direction, false to animate to it.</param>
	public void LookAt(int direction, bool force = false)
	{
		if (!force)
		{
			if (direction < 0 || direction >= 8) return;
			_toDirection = direction;
			if (_toDirection != _direction)
			{
				_status = STATUS_TURNING;
			}
		}
		else
		{
			_toDirection = direction;
			_direction = direction;
		}
	}

	/// <summary>Advances the turning towards the target direction.</summary>
	/// <param name="turret">True to turn the turret, false to turn the unit.</param>
	public void Turn(bool turret = false)
	{
		int a;

		if (turret)
		{
			if (_directionTurret == _toDirectionTurret)
			{
				AbortTurn();
				return;
			}
			a = _toDirectionTurret - _directionTurret;
		}
		else
		{
			if (_direction == _toDirection)
			{
				AbortTurn();
				return;
			}
			a = _toDirection - _direction;
		}

		if (a != 0)
		{
			if (a > 0)
			{
				if (a <= 4)
				{
					if (!turret)
					{
						if (_turretType > -1)
							_directionTurret++;
						_direction++;
					}
					else _directionTurret++;
				}
				else
				{
					if (!turret)
					{
						if (_turretType > -1)
							_directionTurret--;
						_direction--;
					}
					else _directionTurret--;
				}
			}
			else
			{
				if (a > -4)
				{
					if (!turret)
					{
						if (_turretType > -1)
							_directionTurret--;
						_direction--;
					}
					else _directionTurret--;
				}
				else
				{
					if (!turret)
					{
						if (_turretType > -1)
							_directionTurret++;
						_direction++;
					}
					else _directionTurret++;
				}
			}
			if (_direction < 0) _direction = 7;
			if (_direction > 7) _direction = 0;
			if (_directionTurret < 0) _directionTurret = 7;
			if (_directionTurret > 7) _directionTurret = 0;
		}

		if (turret)
		{
			if (_toDirectionTurret == _directionTurret)
			{
				// we officially reached our destination
				_status = STATUS_STANDING;
			}
		}
		else if (_toDirection == _direction || _status == STATUS_UNCONSCIOUS)
		{
			// we officially reached our destination
			_status = STATUS_STANDING;
		}
	}

	/// <summary>Stops the turning towards the target direction.</summary>
	public void AbortTurn()
	{
		_status = STATUS_STANDING;
	}

	/// <summary>Kneel down.</summary>
	/// <param name="kneeled">to kneel or to stand up</param>
	public void Kneel(bool kneeled)
	{
		_kneeled = kneeled;
	}

	/// <summary>Is kneeled down?</summary>
	public bool IsKneeled => _kneeled;

	/// <summary>Is floating? A unit is floating when there is no ground under him/her.</summary>
	public bool IsFloating => _floating;

	/// <summary>Have unit floor below?</summary>
	public bool HaveNoFloorBelow() => _haveNoFloorBelow;

	/// <summary>Aim. (shows the right hand sprite and weapon holding)</summary>
	/// <param name="aiming">true/false</param>
	public void Aim(bool aiming)
	{
		if (aiming)
			_status = STATUS_AIMING;
		else
			_status = STATUS_STANDING;
	}

	/// <summary>
	/// Returns the direction from this unit to a given point.
	/// 0 &lt;-&gt; y = -1, x = 0;
	/// 1 &lt;-&gt; y = -1, x = 1;
	/// 3 &lt;-&gt; y = 1, x = 1;
	/// 5 &lt;-&gt; y = 1, x = -1;
	/// 7 &lt;-&gt; y = -1, x = -1
	/// </summary>
	/// <param name="point">given position.</param>
	/// <returns>direction.</returns>
	public int DirectionTo(Position point)
	{
		const double M_PI_4 = Math.PI / 4.0;
		double ox = point.x - _pos.x;
		double oy = point.y - _pos.y;
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

	/// <summary>Initialises the falling sequence. Occurs after death or stunned.</summary>
	public void StartFalling()
	{
		_status = STATUS_COLLAPSING;
		_fallPhase = 0;
		_turnsSinceStunned = 0;
	}

	/// <summary>Advances the phase of falling sequence.</summary>
	public void KeepFalling()
	{
		_fallPhase++;
		if (_fallPhase == _armor.DeathFrames)
		{
			_fallPhase--;
			if (_health <= 0)
			{
				_status = STATUS_DEAD;
			}
			else
				_status = STATUS_UNCONSCIOUS;
		}
	}

	/// <summary>Set final falling state. Skipping animation.</summary>
	public void InstaFalling()
	{
		StartFalling();
		_fallPhase = _armor.DeathFrames - 1;
		if (_health <= 0)
		{
			_status = STATUS_DEAD;
		}
		else
		{
			_status = STATUS_UNCONSCIOUS;
		}
	}

	/// <summary>Returns the phase of the falling sequence.</summary>
	public int FallingPhase => _fallPhase;

	/// <summary>
	/// Returns whether the soldier is out of combat, dead or unconscious.
	/// A soldier that is out, cannot perform any actions, cannot be selected, but it's still a unit.
	/// </summary>
	public bool IsOut => _status == STATUS_DEAD || _status == STATUS_UNCONSCIOUS || IsIgnored;

	/// <summary>Return true when unit stun level is greater that current health or unit have no health.</summary>
	public bool IsOutThresholdExceed => Health <= 0 || Health <= Stunlevel;

	/// <summary>Unit is removed from game.</summary>
	public bool IsIgnored => _status == STATUS_IGNORE_ME;

	/// <summary>Add this unit to the list of visible units. Returns true if this is a new one.</summary>
	public bool AddToVisibleUnits(BattleUnit unit)
	{
		bool add = true;
		foreach (var bu in _unitsSpottedThisTurn)
		{
			if (bu == unit)
			{
				add = false;
				break;
			}
		}
		if (add)
		{
			_unitsSpottedThisTurn.Add(unit);
		}
		foreach (var bu in _visibleUnits)
		{
			if (bu == unit)
			{
				return false;
			}
		}
		_visibleUnits.Add(unit);
		return true;
	}

	/// <summary>Removes the given unit from the list of visible units.</summary>
	/// <param name="unit">The unit to add to our visibility cache.</param>
	/// <returns>true if unit found and removed</returns>
	public bool RemoveFromVisibleUnits(BattleUnit unit)
	{
		if (_visibleUnits.Count == 0)
		{
			return false;
		}
		int i = _visibleUnits.IndexOf(unit);
		if (i < 0)
		{
			return false;
		}
		//Slow to remove stuff from vector as it shuffles all the following items. Swap in rearmost element before removal.
		_visibleUnits[i] = _visibleUnits[^1];
		_visibleUnits.RemoveAt(_visibleUnits.Count - 1);
		return true;
	}

	/// <summary>Checks if the given unit is on the list of visible units.</summary>
	/// <param name="unit">The unit to check whether we have in our visibility cache.</param>
	/// <returns>true if on the visible list or of the same faction</returns>
	public bool HasVisibleUnit(BattleUnit unit)
	{
		if (Faction == unit.Faction)
		{
			//Units of same faction are always visible, but not stored in the visible unit list
			return true;
		}
		return _visibleUnits.Contains(unit);
	}

	/// <summary>Get the list of visible units.</summary>
	public List<BattleUnit> VisibleUnits => _visibleUnits;

	/// <summary>Clear visible units.</summary>
	public void ClearVisibleUnits()
	{
		_visibleUnits.Clear();
	}

	/// <summary>Add this unit to the list of visible tiles.</summary>
	/// <param name="tile">that we're now able to see.</param>
	/// <returns>true if a new tile.</returns>
	public bool AddToVisibleTiles(Tile tile)
	{
		//Only add once, otherwise we're going to mess up the visibility value and make trouble for the AI (if sneaky).
		if (_visibleTilesLookup.Add(tile))
		{
			tile.Visible = 1; // Tile::setVisible adds to the counter
			_visibleTiles.Add(tile);
			return true;
		}
		return false;
	}

	/// <summary>Has this unit marked this tile as within its view?</summary>
	public bool HasVisibleTile(Tile tile) => _visibleTilesLookup.Contains(tile);

	/// <summary>Get the list of visible tiles.</summary>
	public List<Tile> VisibleTiles => _visibleTiles;

	/// <summary>Clears visible tiles. Also reduces the associated visibility counter used by the AI.</summary>
	public void ClearVisibleTiles()
	{
		foreach (var tile in _visibleTiles)
		{
			tile.Visible = -1; // Tile::setVisible adds to the counter
		}
		_visibleTilesLookup.Clear();
		_visibleTiles.Clear();
	}

	/// <summary>Get or set whether this unit is visible.</summary>
	public bool Visible
	{
		get
		{
			if (Faction == UnitFaction.FACTION_PLAYER || _armor.IsAlwaysVisible)
			{
				return true;
			}
			else
			{
				return _visible;
			}
		}
		set => _visible = value;
	}

	/// <summary>Check if unit can fall down.</summary>
	public void UpdateTileFloorState(SavedBattleGame saveBattleGame)
	{
		if (_tile != null)
		{
			_haveNoFloorBelow = true;

			if (IsBigUnit)
			{
				var armorSize = _armor.Size - 1;
				var newPos = _tile.Position;
				for (int x = armorSize; x >= 0; --x)
				{
					for (int y = armorSize; y >= 0; --y)
					{
						var t = saveBattleGame.GetTile(newPos + new Position(x, y, 0));
						if (t != null)
						{
							if (!t.HasNoFloor(saveBattleGame))
							{
								_haveNoFloorBelow = false;
								return;
							}
						}
					}
				}
			}
			else
			{
				_haveNoFloorBelow &= _tile.HasNoFloor(saveBattleGame) && !_tile.HasLadder;
			}
		}
		else
		{
			_haveNoFloorBelow = false;
		}
	}

	/// <summary>Sets the unit's tile it's standing on</summary>
	/// <param name="tile">Pointer to tile.</param>
	/// <param name="saveBattleGame">Pointer to save to get tile below.</param>
	public void SetTile(Tile tile, SavedBattleGame saveBattleGame = null)
	{
		if (_tile == tile)
		{
			return;
		}

		var armorSize = _armor.Size - 1;
		// Reset tiles moved from.
		if (_tile != null)
		{
			var prevPos = _tile.Position;
			for (int x = armorSize; x >= 0; --x)
			{
				for (int y = armorSize; y >= 0; --y)
				{
					var t = saveBattleGame.GetTile(prevPos + new Position(x, y, 0));
					if (t != null && t.Unit == this)
					{
						t.Unit = null;
					}
				}
			}
		}

		_tile = tile;

		UpdateTileFloorState(saveBattleGame);

		if (_tile == null)
		{
			_floating = false;
			return;
		}

		// Update tiles moved to.
		var newPos = _tile.Position;
		for (int x = armorSize; x >= 0; --x)
		{
			for (int y = armorSize; y >= 0; --y)
			{
				var t = saveBattleGame.GetTile(newPos + new Position(x, y, 0));
				if (t != null)
				{
					t.Unit = this;
				}
			}
		}

		// unit could have changed from flying to walking or vice versa
		if (_status == STATUS_WALKING && _haveNoFloorBelow && _movementType == MT_FLY)
		{
			_status = STATUS_FLYING;
			_floating = true;
		}
		else if (_status == STATUS_FLYING && !_haveNoFloorBelow && _verticalDirection == 0)
		{
			_status = STATUS_WALKING;
			_floating = false;
		}
		else if (_status == STATUS_UNCONSCIOUS)
		{
			_floating = _movementType == MT_FLY && _haveNoFloorBelow;
		}
	}

	/// <summary>
	/// Set only unit tile without any additional logic.
	/// Used only in before battle, other wise will break game.
	/// Need call setTile after to fix links
	/// </summary>
	public void SetInventoryTile(Tile tile)
	{
		_tile = tile;
	}

	/// <summary>Gets the unit's tile.</summary>
	public Tile Tile => _tile;

	/// <summary>Check if this unit is in the exit area.</summary>
	/// <param name="stt">Type of exit tile to check for.</param>
	public bool IsInExitArea(SpecialTileType stt) => LiesInExitArea(_tile, stt);

	/// <summary>Check if this unit lies (e.g. unconscious) in the exit area.</summary>
	/// <param name="tile">Unit's location.</param>
	/// <param name="stt">Type of exit tile to check for.</param>
#pragma warning disable CA1822 // C++ member function; kept as instance method for a 1:1 API
	public bool LiesInExitArea(Tile tile, SpecialTileType stt) => tile != null && tile.FloorSpecialTileType == stt;
#pragma warning restore CA1822

	/// <summary>Gets the unit height taking into account kneeling/standing.</summary>
	public int Height => IsKneeled ? KneelHeight : StandHeight;

	/// <summary>Gets the unit floating elevation (voxels over the ground, when flying).</summary>
	public int FloatHeight => _floatHeight;

	/// <summary>Gets the unit's stand height (in voxels, when standing up).</summary>
	public int StandHeight => _standHeight;

	/// <summary>Gets the unit's kneel height (in voxels, when kneeling).</summary>
	public int KneelHeight => _kneelHeight;

	/// <summary>Checks if a tile is visible from either of the unit's tiles, using maths.</summary>
	/// <param name="pos">the position to check against</param>
	/// <param name="useTurretDirection">use turret facing (true) or body facing (false) for sector calculation</param>
	/// <returns>what the maths decide</returns>
	public bool CheckViewSector(Position pos, bool useTurretDirection = false)
	{
		int unitSize = Armor.Size;
		//Check view cone from each of the unit's tiles
		for (int x = 0; x < unitSize; ++x)
		{
			for (int y = 0; y < unitSize; ++y)
			{
				int deltaX = pos.x - (_pos.x + x);
				int deltaY = (_pos.y + y) - pos.y;
				switch (useTurretDirection ? _directionTurret : _direction)
				{
					case 0:
						if ((deltaX + deltaY >= 0) && (deltaY - deltaX >= 0))
							return true;
						break;
					case 1:
						if ((deltaX >= 0) && (deltaY >= 0))
							return true;
						break;
					case 2:
						if ((deltaX + deltaY >= 0) && (deltaY - deltaX <= 0))
							return true;
						break;
					case 3:
						if ((deltaY <= 0) && (deltaX >= 0))
							return true;
						break;
					case 4:
						if ((deltaX + deltaY <= 0) && (deltaY - deltaX <= 0))
							return true;
						break;
					case 5:
						if ((deltaX <= 0) && (deltaY <= 0))
							return true;
						break;
					case 6:
						if ((deltaX + deltaY <= 0) && (deltaY - deltaX >= 0))
							return true;
						break;
					case 7:
						if ((deltaY >= 0) && (deltaX <= 0))
							return true;
						break;
					default:
						break;
				}
			}
		}
		return false;
	}

	/// <summary>If this unit is breathing, what frame should be displayed?</summary>
	public int BreathExhaleFrame
	{
		get
		{
			if (_breathing)
			{
				var frame = _breathFrame - BUBBLES_FIRST_FRAME;
				if (frame >= 0)
				{
					return frame;
				}
			}

			return -1;
		}
	}

	/// <summary>Count frames to next start of breath animation.</summary>
	public int BreathInhaleFrame
	{
		get
		{
			if (_breathing)
			{
				var frame = BUBBLES_FIRST_FRAME - _breathFrame;
				if (frame >= 0)
				{
					return frame;
				}
			}

			return -1;
		}
	}

	/// <summary>Decides if we should start producing bubbles, and/or updates which bubble frame we are on.</summary>
	public void Breathe()
	{
		// _breathFrame of -1 means this unit doesn't produce bubbles
		if (_breathFrame < 0)
		{
			_breathing = false;
			return;
		}

		// moving or knock out do not breathe, even when still alive :)
		if (IsOut || _status == STATUS_WALKING)
		{
			_breathing = false;
			_breathFrame = 0;
			return;
		}

		if (!_breathing)
		{
			// deviation from original: TFTD used a static 10% chance for every animation frame,
			// instead let's use 5%, but allow morale to affect it.
			_breathing = Rng.Seedless(0, 99) < (105 - _morale);
			_breathFrame = 0;
		}

		if (_breathing)
		{
			// advance the bubble frame
			_breathFrame++;

			// we've reached the end of the cycle, get rid of the bubbles
			if (_breathFrame > BUBBLES_LAST_FRAME)
			{
				_breathFrame = 0;
				_breathing = false;
			}
		}
	}

	/// <summary>Gets or sets the flag for "floor above me" meaning stop rendering bubbles.</summary>
	public bool FloorAbove
	{
		get => _floorAbove;
		set => _floorAbove = value;
	}

	/// <summary>Gets original unit's movement type.</summary>
	public MovementType OriginalMovementType => _originalMovementType;

	/// <summary>Gets or sets the unit's movement type.</summary>
	public MovementType MovementType
	{
		get => _movementType;
		set => _movementType = value;
	}

	/// <summary>Gets the turn cost.</summary>
	public int TurnCost => _armor.TurnCost;

	/// <summary>Gets cost of standing up from kneeling.</summary>
#pragma warning disable CA1822 // C++ member function; kept as instance property for a 1:1 API
	public int KneelUpCost => 8;

	/// <summary>Gets cost of kneel down.</summary>
	public int KneelDownCost => 4;
#pragma warning restore CA1822

	/// <summary>Gets cost of current transiton form kneeling to standing or reverse.</summary>
	public int KneelChangeCost => IsKneeled ? KneelUpCost : KneelDownCost;

	/// <summary>Multiplier of move cost.</summary>
	public ArmorMoveCost MoveCostBase => _moveCostBase;

	/// <summary>Multiplier of fly move cost.</summary>
	public ArmorMoveCost MoveCostBaseFly => _moveCostBaseFly;

	/// <summary>Multiplier of climb move cost.</summary>
	public ArmorMoveCost MoveCostBaseClimb => _moveCostBaseClimb;

	/// <summary>Multiplier of normal move cost.</summary>
	public ArmorMoveCost MoveCostBaseNormal => _moveCostBaseNormal;
}
