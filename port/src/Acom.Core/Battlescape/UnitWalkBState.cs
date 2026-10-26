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
// Ported from: src/Battlescape/UnitWalkBState.h, src/Battlescape/UnitWalkBState.cpp
// Port status: partial (missing: script hooks)
#nullable disable

using static Acom.BattleActionType;
using static Acom.BattleType;
using static Acom.ItemDamageType;
using static Acom.LightLayers;
using static Acom.MovementType;
using static Acom.SpecialAbility;
using static Acom.UnitFaction;
using static Acom.UnitStatus;

namespace Acom;

/// <summary>State for walking units.</summary>
public class UnitWalkBState : BattleState
{
	private Position _target;
	private BattleUnit _unit;
	private Pathfinding _pf;
	private TileEngine _terrain;

	/// <summary>Unit will fall down always.</summary>
	private bool _falling;
	/// <summary>Allow to move over some tiles that normally should fall down.</summary>
	private bool _fallingWhenStopped;

	private bool _beforeFirstStep;
	private int _numUnitsSpotted;
	private int _preMovementCost;

	/// <summary>Sets up an UnitWalkBState.</summary>
	/// <param name="parent">Pointer to the Battlescape.</param>
	/// <param name="_action">Pointer to an action.</param>
#pragma warning disable CA1700, IDE1006 // parameter name kept from the C++ declaration (and the skeleton)
	public UnitWalkBState(BattlescapeGame parent, BattleAction _action) : base(parent, _action)
#pragma warning restore CA1700, IDE1006
	{
		_unit = null;
		_pf = null;
		_terrain = null;
		_beforeFirstStep = false;
		_numUnitsSpotted = 0;
		_preMovementCost = 0;
	}

	/// <summary>Initializes the state.</summary>
	public override void Init()
	{
		_unit = _action.actor;
		_numUnitsSpotted = _unit.UnitsSpottedThisTurn.Count;
		SetNormalWalkSpeed();
		_pf = _parent.Pathfinding;
		_terrain = _parent.TileEngine;
		_target = _action.target;
		if (Options.traceAI) { Logger.Info($"Walking from: {_unit.Position}, to {_target}"); }
		int dir = _pf.StartDirection;
		if (!_action.strafe && dir != -1 && dir != _unit.Direction)
		{
			_beforeFirstStep = true;
		}
		_terrain.AddMovingUnit(_unit);
	}

	/// <summary>Deinitalize the state.</summary>
	public override void Deinit()
	{
		_terrain.RemoveMovingUnit(_unit);
	}

	/// <summary>Stops the current move: falls down first when standing over a hole, otherwise aborts the path.</summary>
	private void CancelCurentMove()
	{
		if (_fallingWhenStopped && !_falling)
		{
			_falling = true;
		}
		else
		{
			_pf.AbortPath();
			_parent.PopState();
		}
	}

	/// <summary>Runs state functionality every cycle.</summary>
	public override void Think()
	{
		if (!_unit.Armor.AllowsMoving())
		{
			_pf.AbortPath();
			_parent.PopState();
			return;
		}

		bool unitSpotted = false;
		int size = _unit.Armor.Size - 1;
		bool onScreen = (_unit.Visible && _parent.Map.Camera.IsOnScreen(_unit.Position, true, size, false));
		if (_unit.IsKneeled)
		{
			if (_parent.Kneel(_unit))
			{
				return;
			}
			else
			{
				if (_parent.PanicHandled)
				{
					_action.result = "STR_NOT_ENOUGH_TIME_UNITS";
				}
				_pf.AbortPath();
				_parent.PopState();
				return;
			}
		}

		if (_unit.IsOut)
		{
			_pf.AbortPath();
			_parent.PopState();
			return;
		}

		if (_unit.Status == STATUS_WALKING || _unit.Status == STATUS_FLYING)
		{
			if ((_parent.Save.GetTile(_unit.Destination).Unit == null) || // next tile must be not occupied
				(_parent.Save.GetTile(_unit.Destination).Unit == _unit))
			{
				bool onScreenBoundary = (_unit.Visible && _parent.Map.Camera.IsOnScreen(_unit.Position, true, size, true));
				_unit.KeepWalking(_parent.Save, onScreenBoundary); // advances the phase
				PlayMovementSound();
				if (_parent.Save.IsPreview)
				{
					_unit.ResetTimeUnitsAndEnergy();
				}
			}
			else if (!_falling)
			{
				_unit.LookAt(_unit.Destination, (_unit.TurretType != -1)); // turn to undiscovered unit
				_pf.AbortPath();
			}

			// unit moved from one tile to the other, update the tiles
			if (_unit.Position != _unit.LastPosition)
			{
				Tile belowTile = _parent.Save.GetBelowTile(_unit.Tile);
				_fallingWhenStopped = _unit.HaveNoFloorBelow() && _unit.Position.z != 0 && _unit.MovementType != MT_FLY && _unit.WalkingPhase == 0;
				_falling = _fallingWhenStopped && !(
					belowTile != null && belowTile.HasLadder && // we do not have any footing but "jump" from ladder to reach ledge
					_unit.Position == _unit.LastPosition + new Position(0, 0, 1) && // only vertical move from ladder below
					_pf.StartDirection != -1 // move is not canceled, when you cancel "jump" you should fallback to ladder below
				);

				if (_falling)
				{
					for (int x = size; x >= 0; --x)
					{
						for (int y = size; y >= 0; --y)
						{
							Tile otherTileBelow = _parent.Save.GetTile(_unit.Position + new Position(x, y, -1));
							if (otherTileBelow != null && otherTileBelow.Unit != null)
							{
								_falling = false;
								_fallingWhenStopped = false;
								_pf.DequeuePath();
								_parent.Save.AddFallingUnit(_unit);
								_parent.StatePushFront(new UnitFallBState(_parent));
								return;
							}
						}
					}
				}

				if (!_parent.Map.Camera.IsOnScreen(_unit.Position, true, size, false) && _unit.Faction != FACTION_PLAYER && _unit.Visible)
					_parent.Map.Camera.CenterOnPosition(_unit.Position);
				// if the unit changed level, camera changes level with
				_parent.Map.Camera.ViewLevel = _unit.Position.z;
			}

			// is the step finished?
			if (_unit.Status == STATUS_STANDING)
			{
				// update the TU display
				_parent.Save.BattleState.UpdateSoldierInfo();
				// if the unit burns floor tiles, burn floor tiles as long as we're not falling
				if (!_falling && (_unit.SpecialAbility == (int)SPECAB_BURNFLOOR || _unit.SpecialAbility == (int)SPECAB_BURN_AND_EXPLODE))
				{
					_unit.Tile.Ignite(1);
					Position posHere = _unit.Position;
					Position voxelHere = posHere.ToVoxel() + new Position(8, 8, -(_unit.Tile.TerrainLevel));
					_parent.TileEngine.Hit(new BattleActionAttack(BA_NONE, _unit), voxelHere, _unit.BaseStats.strength, _parent.Mod.GetDamageType(DT_IN), false);

					if (_unit.Status != STATUS_STANDING) // ie: we burned a hole in the floor and fell through it
					{
						_pf.AbortPath();
						return;
					}
				}

				if (_unit.Faction != FACTION_PLAYER)
				{
					_unit.Visible = false;
				}

				int change = _parent.CheckForProximityGrenades(_unit);
				// move our personal lighting with us
				_terrain.CalculateLighting(change != 0 ? LL_ITEMS : LL_UNITS, _unit.Position, 2);
				_terrain.CalculateFOV(_unit.Position, 2, false); //update unit visibility for all units which can see last and current position.
				//tile visibility for this unit is handled later.
				unitSpotted = (!_action.ignoreSpottedEnemies && !_falling && !_action.desperate && _parent.PanicHandled && _numUnitsSpotted != _unit.UnitsSpottedThisTurn.Count);

				if (change > 1)
				{
					_parent.PopState();
					return;
				}
				if (unitSpotted)
				{
					CancelCurentMove();
					return;
				}
				// check for reaction fire
				if (!_falling && !_fallingWhenStopped)
				{
					if (_terrain.CheckReactionFire(_unit, _action))
					{
						// unit got fired upon - stop walking
						CancelCurentMove();
						return;
					}
				}
			}
			else if (onScreen)
			{
				// make sure the unit sprites are up to date
				if (_pf.StrafeMove)
				{
					// This is where we fake out the strafe movement direction so the unit "moonwalks"
					int dirTemp = _unit.Direction;
					_unit.Direction = _unit.FaceDirection;
					//TODO fix moonwalk
					_unit.Direction = dirTemp;
				}
			}
		}

		// we are just standing around, shouldn't we be walking?
		if (_unit.Status == STATUS_STANDING || _unit.Status == STATUS_PANICKING || _unit.Status == STATUS_BERSERK)
		{
			// check if we did spot new units
			if (unitSpotted && !_action.desperate && _unit.Charging == null && !_falling)
			{
				if (Options.traceAI) { Logger.Info("Uh-oh! Company!"); }
				_unit.SetHiding(false); // clearly we're not hidden now
				PostPathProcedures();
				return;
			}

			if (onScreen || _parent.Save.DebugMode)
			{
				SetNormalWalkSpeed();
			}
			else
			{
				_parent.SetStateInterval(0);
			}
			int dir = _pf.StartDirection;
			if (_falling)
			{
				dir = Pathfinding.DIR_DOWN;
			}

			if (dir != -1)
			{
				if (_pf.StrafeMove)
				{
					_unit.FaceDirection = _unit.Direction;
				}

				_pf.SetUnit(_unit); //TODO: remove as was done by `getTUCost`
				PathfindingStep r = _pf.GetTUCost(_unit.Position, dir, _unit, null, _action.MoveType);

				int tu = r.cost.time;
				int energy = r.cost.energy;
				Position destination = r.pos;

				if (tu == Pathfinding.INVALID_MOVE_COST)
				{
					CancelCurentMove();
					return;
				}

				if (tu > _unit.TimeUnits)
				{
					if (_parent.PanicHandled)
					{
						_action.result = "STR_NOT_ENOUGH_TIME_UNITS";
					}
					CancelCurentMove();
					return;
				}

				if (energy > _unit.Energy)
				{
					if (_parent.PanicHandled)
					{
						_action.result = "STR_NOT_ENOUGH_ENERGY";
					}
					CancelCurentMove();
					return;
				}

				if (_parent.PanicHandled && !_falling && _parent.CheckReservedTU(_unit, tu, energy) == false)
				{
					CancelCurentMove();
					return;
				}

				// we are looking in the wrong way, turn first (unless strafing)
				// we are not using the turn state, because turning during walking costs no tu
				if (dir != _unit.Direction && dir < Pathfinding.DIR_UP && !_pf.StrafeMove)
				{
					_unit.LookAt(dir);
					return;
				}

				// now open doors (if any)
				if (dir < Pathfinding.DIR_UP)
				{
					int door = _terrain.UnitOpensDoor(_unit, false, dir);
					if (door == 3)
					{
						return; // don't start walking yet, wait for the ufo door to open
					}
					if (door == 0)
					{
						_parent.Mod.GetSoundByDepth((uint)_parent.Depth, (uint)Mod.DOOR_OPEN).Play(-1, _parent.Map.GetSoundAngle(_unit.Position)); // normal door
					}
					if (door == 1)
					{
						_parent.Mod.GetSoundByDepth((uint)_parent.Depth, (uint)Mod.SLIDING_DOOR_OPEN).Play(-1, _parent.Map.GetSoundAngle(_unit.Position)); // ufo door
						return; // don't start walking yet, wait for the ufo door to open
					}
				}
				for (int x = size; x >= 0; --x)
				{
					for (int y = size; y >= 0; --y)
					{
						BattleUnit unitInMyWay = _parent.Save.GetTile(destination + new Position(x, y, 0)).GetOverlappingUnit(_parent.Save, TileUnitOverlapping.TUO_IGNORE_SMALL); // 2+ voxels poking into the tile above, we don't kick people in the head here at XCom.
						// can't walk into units in this tile, or on top of other units sticking their head into this tile
						if (!_falling && unitInMyWay != null && unitInMyWay != _unit)
						{
							_action.ClearTU();
							CancelCurentMove();
							return;
						}
					}
				}
				// now start moving
				dir = _pf.DequeuePath();
				if (_falling)
				{
					dir = Pathfinding.DIR_DOWN;
				}

				if (_unit.SpendTimeUnits(tu))
				{
					if (_unit.SpendEnergy(energy))
					{
						_unit.StartWalking(dir, destination, _parent.Save);
						_beforeFirstStep = false;
					}
				}
				// make sure the unit sprites are up to date
				if (onScreen)
				{
					if (_pf.StrafeMove)
					{
						// This is where we fake out the strafe movement direction so the unit "moonwalks"
						int dirTemp = _unit.Direction;
						_unit.Direction = _unit.FaceDirection;
						_unit.Direction = dirTemp;
					}
				}
			}
			else
			{
				PostPathProcedures();
				return;
			}
		}

		// turning during walking costs no tu
		if (_unit.Status == STATUS_TURNING)
		{
			// except before the first step.
			if (_beforeFirstStep)
			{
				if (_unit.Armor.TurnBeforeFirstStep)
				{
					_unit.SpendTimeUnits(_unit.TurnCost);
				}
				else
				{
					_preMovementCost++;
				}
			}

			_unit.Turn();

			// calculateFOV is unreliable for setting the unitSpotted bool, as it can be called from various other places
			// in the code, ie: doors opening, and this messes up the result.
			_terrain.CalculateFOV(_unit);
			unitSpotted = (!_action.ignoreSpottedEnemies && !_falling && !_action.desperate && _parent.PanicHandled && _numUnitsSpotted != _unit.UnitsSpottedThisTurn.Count);

			if (unitSpotted && !_action.desperate && _unit.Charging == null && !_falling)
			{
				if (_beforeFirstStep)
				{
					_preMovementCost = _preMovementCost * _unit.TurnCost;
					_unit.SpendTimeUnits(_preMovementCost);
				}
				if (Options.traceAI) { Logger.Info("Egads! A turn reveals new units! I must pause!"); }
				_unit.SetHiding(false); // not hidden, are we...
				_unit.AbortTurn(); //revert to a standing state.
				CancelCurentMove();
				return;
			}
		}
	}

	/// <summary>Aborts unit walking.</summary>
	public override void Cancel()
	{
		if (_beforeFirstStep)
		{
			// cancel here would allow turning without spending any TUs
			return;
		}

		if (_parent.Save.Side == FACTION_PLAYER && _parent.PanicHandled)
			_pf.AbortPath();
	}

	/// <summary>Handles some calculations when the path is finished.</summary>
	private void PostPathProcedures()
	{
		_action.ClearTU();
		if (_unit.Faction != FACTION_PLAYER)
		{
			int dir = _action.finalFacing;
			if (_action.finalAction)
			{
				_unit.DontReselect();
			}
			if (_unit.Charging != null)
			{
				dir = _parent.TileEngine.GetDirectionTo(_unit.Position, _unit.Charging.Position);
				if (_parent.TileEngine.ValidMeleeRange(_unit, _action.actor.Charging, dir))
				{
					var action = new BattleAction();
					action.actor = _unit;
					action.target = _unit.Charging.Position;
					action.weapon = _unit.GetUtilityWeapon(BT_MELEE);
					action.type = BA_HIT;
					action.targeting = true;
					action.UpdateTU();
					_unit.Charging = null;
					_parent.StatePushBack(new MeleeAttackBState(_parent, action));
				}
			}
			else if (_unit.IsHiding)
			{
				dir = _unit.Direction + 4;
				_unit.SetHiding(false);
				_unit.DontReselect();
			}
			if (dir != -1)
			{
				if (dir >= 8)
				{
					dir -= 8;
				}
				_unit.LookAt(dir);
				while (_unit.Status == STATUS_TURNING)
				{
					_unit.Turn();
					_parent.TileEngine.CalculateFOV(_unit);
				}
			}
		}
		else if (!_parent.PanicHandled)
		{
			//todo: set the unit to aggrostate and try to find cover?
			_unit.ClearTimeUnits();
		}

		_terrain.CalculateLighting(LL_UNITS, _unit.Position);
		_terrain.CalculateFOV(_unit);
		if (!_falling)
			_parent.PopState();
	}

	/// <summary>Handles some calculations when the walking is finished.</summary>
	private void SetNormalWalkSpeed()
	{
		if (_unit.Faction == FACTION_PLAYER)
			_parent.SetStateInterval((uint)Options.battleXcomSpeed);
		else
			_parent.SetStateInterval((uint)Options.battleAlienSpeed);
	}

	/// <summary>Handles the stepping sounds.</summary>
	private void PlayMovementSound()
	{
		int size = _unit.Armor.Size - 1;
		if ((!_unit.Visible && !_parent.Save.DebugMode) || !_parent.Map.Camera.IsOnScreen(_unit.Position, true, size, false)) return;

		Tile tile = _unit.Tile;
		int sound = -1;
		int unitSound = _unit.MoveSound;
		int tileSoundOffset = tile.GetFootstepSound(_parent.Save.GetBelowTile(tile));
		int tileSound = Mod.NO_SOUND;
		if (tileSoundOffset > -1)
		{
			// play footstep sound 1
			if (_unit.WalkingPhase == 3)
			{
				tileSound = Mod.WALK_OFFSET + (tileSoundOffset * 2);
			}
			// play footstep sound 2
			if (_unit.WalkingPhase == 7)
			{
				tileSound = Mod.WALK_OFFSET + (tileSoundOffset * 2) + 1;
			}
		}
		if (unitSound != Mod.NO_SOUND)
		{
			// if a sound is configured in the ruleset, play that one
			if (_unit.WalkingPhase == 0)
			{
				sound = unitSound;
			}
		}
		else
		{
			if (_unit.Status == STATUS_WALKING)
			{
				if (tileSound > Mod.NO_SOUND) //TODO: it should be `!=` but its possbile that offset could get negative is based on mod data
				{
					sound = tileSound;
				}
			}
			else if (_unit.MovementType == MT_FLY)
			{
				// play default flying sound
				if (_unit.WalkingPhase == 1)
				{
					sound = Mod.FLYING_SOUND;
				}
			}
		}

		// C++ lets the armor script (selectMoveSoundUnit) change the sound here; script hooks are not ported.
		if (sound >= 0)
		{
			_parent.Mod.GetSoundByDepth((uint)_parent.Depth, (uint)sound).Play(-1, _parent.Map.GetSoundAngle(_unit.Position));
		}
	}
}
