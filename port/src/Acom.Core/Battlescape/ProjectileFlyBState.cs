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
// Ported from: src/Battlescape/ProjectileFlyBState.h, src/Battlescape/ProjectileFlyBState.cpp
#nullable disable

using static Acom.BattleActionType;
using static Acom.BattleType;
using static Acom.LightLayers;
using static Acom.UnitFaction;
using static Acom.UnitStatus;
using static Acom.VoxelType;

namespace Acom;

/// <summary>A projectile state.</summary>
public class ProjectileFlyBState : BattleState
{
	private BattleUnit _unit;
	private BattleItem _ammo;
	private Position _origin, _targetVoxel, _originVoxel;
	private int _projectileImpact;
	private int _range;
	private bool _initialized, _targetFloor;

	private static readonly int[,] SurroundingTilePositions = {
		{ 0, -1 }, // north (-y direction)
		{ 1, -1 }, // northeast
		{ 1, 0 }, // east (+ x direction)
		{ 1, 1 }, // southeast
		{ 0, 1 }, // south (+y direction)
		{ -1, 1 }, // southwest
		{ -1, 0 }, // west (-x direction)
		{ -1, -1 } }; // northwest

	private static readonly int[] CloseQuartersFailedResults = {
		0,   // Fire straight down
		0,   // Fire straight up
		6,   // Fire left 90 degrees
		7,   // Fire left 45 degrees
		1,   // Fire right 45 degrees
		2 }; // Fire right 90 degrees

	/// <summary>Sets up an ProjectileFlyBState.</summary>
	public ProjectileFlyBState(BattlescapeGame parent, BattleAction action) : base(parent, action)
	{
		_unit = null;
		_ammo = null;
		_origin = action.actor.Position;
		_originVoxel = new Position(-1, -1, -1);
		_projectileImpact = 0;
		_range = 0;
		_initialized = false;
		_targetFloor = false;
	}

	/// <summary>Sets up an ProjectileFlyBState.</summary>
	public ProjectileFlyBState(BattlescapeGame parent, BattleAction action, Position origin, int range) : base(parent, action)
	{
		_unit = null;
		_ammo = null;
		_origin = origin;
		_originVoxel = new Position(-1, -1, -1);
		_projectileImpact = 0;
		_range = range;
		_initialized = false;
		_targetFloor = false;
	}

	/// <summary>
	/// Initializes the sequence:
	/// - checks if the shot is valid,
	/// - calculates the base accuracy.
	/// </summary>
	public override void Init()
	{
		if (_initialized) return;
		_initialized = true;

		BattleItem weapon = _action.weapon;

		if (weapon == null) // can't shoot without weapon
		{
			_parent.PopState();
			return;
		}

		if (_parent.Save.GetTile(_action.target) == null) // invalid target position
		{
			_parent.PopState();
			return;
		}

		//test TU only on first lunch waypoint or normal shoot
		if (_range == 0 && !_action.HaveTU(ref _action.result))
		{
			_parent.PopState();
			return;
		}

		_unit = _action.actor;

		bool reactionShoot = _unit.Faction != _parent.Save.Side;
		if (_action.type != BA_THROW)
		{
			if (reactionShoot)
			{
				_ammo = _action.weapon.GetAmmoForAction(_action.type);
			}
			else
			{
				_ammo = _action.weapon.GetAmmoForAction(_action.type, ref _action.result);
			}
			if (_ammo == null)
			{
				_parent.PopState();
				return;
			}
		}

		if (_unit.IsOut || _unit.IsOutThresholdExceed)
		{
			// something went wrong - we can't shoot when dead or unconscious, or if we're about to fall over.
			_parent.PopState();
			return;
		}

		// reaction fire
		if (reactionShoot)
		{
			BattleUnit target = _parent.Save.GetTile(_action.target).Unit;
			// target is dead: cancel the shot.
			if (target == null || target.IsOut || target.IsOutThresholdExceed || target != _parent.Save.SelectedUnit)
			{
				_parent.PopState();
				return;
			}
			_unit.LookAt(_action.target, _unit.TurretType != -1);
			while (_unit.Status == STATUS_TURNING)
			{
				_unit.Turn(_unit.TurretType != -1);
			}
		}

		Tile endTile = _parent.Save.GetTile(_action.target);
		int distanceSq = _action.actor.Distance3dToPositionSq(_action.target);
		bool isPlayer = _parent.Save.Side == FACTION_PLAYER;
		if (isPlayer) _parent.Map.ResetObstacles();
		switch (_action.type)
		{
			case BA_SNAPSHOT:
			case BA_AIMEDSHOT:
			case BA_AUTOSHOT:
			case BA_LAUNCH:
				if (weapon.Rules.IsOutOfRange(distanceSq))
				{
					// out of range
					_action.result = "STR_OUT_OF_RANGE";
					_parent.PopState();
					return;
				}
				break;
			case BA_THROW:
				if (!ValidThrowRange(_action, _parent.TileEngine.GetOriginVoxel(_action, null), _parent.Save.GetTile(_action.target), _parent.Save.Depth))
				{
					// out of range
					_action.result = "STR_OUT_OF_RANGE";
					_parent.PopState();
					return;
				}
				if (endTile != null &&
					endTile.TerrainLevel == -24 &&
					endTile.Position.z + 1 < _parent.Save.MapSizeZ)
				{
					_action.target.z += 1;
				}
				break;
			default:
				_parent.PopState();
				return;
		}

		// Check for close quarters combat
		if (_parent.Mod.EnableCloseQuartersCombat != 0 && _action.type != BA_THROW && _action.type != BA_LAUNCH && _unit.TurretType == -1 && !_unit.Armor.GetIgnoresMeleeThreat())
		{
			// Start by finding 'targets' for the check
			var closeQuartersTargetList = new List<BattleUnit>();
			for (int dir = 0; dir < 8; dir++)
			{
				Position tileToCheck = _origin;
				tileToCheck.x += (short)SurroundingTilePositions[dir, 0];
				tileToCheck.y += (short)SurroundingTilePositions[dir, 1];

				if (_parent.Save.GetTile(tileToCheck) != null) // Make sure the tile is in bounds
				{
					BattleUnit closeQuartersTarget = _parent.Save.SelectUnit(tileToCheck);
					// Variable for LOS check
					int checkDirection = _parent.TileEngine.GetDirectionTo(tileToCheck, _unit.Position);
					if (closeQuartersTarget != null && _unit.Faction != closeQuartersTarget.Faction // Unit must exist and not be same faction
						&& closeQuartersTarget.Armor.GetCreatesMeleeThreat() // Unit must be valid defender, 2x2 default false here
						&& closeQuartersTarget.TimeUnits >= _parent.Mod.CloseQuartersTuCostGlobal // Unit must have enough TUs
						&& closeQuartersTarget.Energy >= _parent.Mod.CloseQuartersEnergyCostGlobal // Unit must have enough Energy
						&& _parent.TileEngine.ValidMeleeRange(closeQuartersTarget, _unit, checkDirection) // Unit must be able to see the unit attempting to fire
						&& !(_unit.Faction == FACTION_PLAYER && closeQuartersTarget.Faction == FACTION_NEUTRAL) // Civilians don't inhibit player
						&& !(_unit.Faction == FACTION_NEUTRAL && closeQuartersTarget.Faction == FACTION_PLAYER)) // Player doesn't inhibit civilians
					{
						if (Rng.Percent(_parent.Mod.CloseQuartersSneakUpGlobal))
						{
							if (_unit.Faction == FACTION_HOSTILE) // alien attacker (including mind-controlled xcom)
							{
								if (!closeQuartersTarget.HasVisibleUnit(_unit))
								{
									continue; // the xcom/civilian victim *DOES NOT SEE* the attacker and cannot defend itself
								}
							}
							else // xcom/civilian attacker (including mind-controlled aliens)
							{
								if (_unit.TurnsSinceSpotted > 1)
								{
									continue; // the aliens (as a collective) *ARE NOT AWARE* of the attacker and cannot defend themselves
								}
							}
						}
						closeQuartersTargetList.Add(closeQuartersTarget);
					}
				}
			}

			if (closeQuartersTargetList.Count != 0)
			{
				foreach (var bu in closeQuartersTargetList)
				{
					var attack = new BattleActionAttack();
					attack.type = BA_CQB;
					attack.attacker = _action.actor;
					attack.weapon_item = _action.weapon;
					attack.damage_item = _action.weapon;

					// Roll for the check
					if (!_parent.TileEngine.MeleeAttack(attack, bu))
					{
						// Failed the check, roll again to see result
						if (_parent.Save.Side == FACTION_PLAYER) // Only show message during player's turn
						{
							_action.result = "STR_FAILED_CQB_CHECK";
						}
						int rng = Rng.Generate(0, 5);
						Position closeQuartersFailedNewTarget = _unit.Position;
						if (rng == 1)
						{
							closeQuartersFailedNewTarget.z += 1;
						}
						else if (rng > 1)
						{
							int newFacing = (_unit.Direction + CloseQuartersFailedResults[rng]) % 8;
							closeQuartersFailedNewTarget.x += (short)SurroundingTilePositions[newFacing, 0];
							closeQuartersFailedNewTarget.y += (short)SurroundingTilePositions[newFacing, 1];
						}

						// Make sure the new target is in bounds
						if (_parent.Save.GetTile(closeQuartersFailedNewTarget) == null)
						{
							// Default to firing at our feet
							closeQuartersFailedNewTarget = _unit.Position;
						}

						// Turn to look at new target
						_action.target = closeQuartersFailedNewTarget;
						_unit.LookAt(_action.target, _unit.TurretType != -1);
						while (_unit.Status == STATUS_TURNING)
						{
							_unit.Turn(_unit.TurretType != -1);
						}

						// We're done, spend TUs and Energy; and don't check remaining CQB candidates anymore
						bu.SpendTimeUnits(_parent.Mod.CloseQuartersTuCostGlobal);
						bu.SpendEnergy(_parent.Mod.CloseQuartersEnergyCostGlobal);
						break;
					}
				}
			}
		}

		bool forceEnableObstacles = false;
		if (_action.type == BA_LAUNCH || (Options.forceFire && _parent.Save.IsCtrlPressed(true) && isPlayer) || !_parent.PanicHandled)
		{
			// target nothing, targets the middle of the tile
			_targetVoxel = _action.target.ToVoxel() + TileEngine.voxelTileCenter;
			if (_action.type == BA_LAUNCH)
			{
				if (_targetFloor)
				{
					// launched missiles with two waypoints placed on the same tile: target the floor.
					_targetVoxel.z -= 10;
				}
				else
				{
					// launched missiles go slightly higher than the middle.
					_targetVoxel.z += 4;
				}
			}
		}
		else if (!_action.weapon.GetArcingShot(_action.type))
		{
			// determine the target voxel.
			// aim at the center of the unit, the object, the walls or the floor (in that priority)
			// if there is no LOF to the center, try elsewhere (more outward).
			// Store this target voxel.
			Tile targetTile = _parent.Save.GetTile(_action.target);
			Position originVoxel = _parent.TileEngine.GetOriginVoxel(_action, _parent.Save.GetTile(_origin));
			if (targetTile.Unit != null &&
				((_unit.Faction != FACTION_PLAYER) ||
				targetTile.Unit.Visible))
			{
				if (_origin == _action.target || targetTile.Unit == _unit)
				{
					// don't shoot at yourself but shoot at the floor
					_targetVoxel = _action.target.ToVoxel() + new Position(8, 8, 0);
				}
				else
				{
					bool foundLoF = _parent.TileEngine.CanTargetUnit(ref originVoxel, targetTile, ref _targetVoxel, _unit, isPlayer, null);

					if (!foundLoF && Options.oxceEnableOffCentreShooting)
					{
						// If we can't target from the standard shooting position, try a bit left and right from the centre.
						foreach (var rel_pos in new[] { BattleActionOrigin.LEFT, BattleActionOrigin.RIGHT })
						{
							_action.relativeOrigin = rel_pos;
							originVoxel = _parent.TileEngine.GetOriginVoxel(_action, _parent.Save.GetTile(_origin));
							foundLoF = _parent.TileEngine.CanTargetUnit(ref originVoxel, targetTile, ref _targetVoxel, _unit, isPlayer, null);
							if (foundLoF)
							{
								break;
							}
						}
					}

					if (!foundLoF)
					{
						// Failed to find LOF
						_action.relativeOrigin = BattleActionOrigin.CENTRE; // reset to the normal origin

						_targetVoxel = TileEngine.invalid.ToVoxel(); // out of bounds, even after voxel to tile calculation.
						if (isPlayer)
						{
							forceEnableObstacles = true;
						}
					}
				}
			}
			else if (targetTile.GetMapData(TilePart.O_OBJECT) != null)
			{
				if (!_parent.TileEngine.CanTargetTile(ref originVoxel, targetTile, (int)TilePart.O_OBJECT, ref _targetVoxel, _unit, isPlayer))
				{
					_targetVoxel = _action.target.ToVoxel() + new Position(8, 8, 10);
				}
			}
			else if (targetTile.GetMapData(TilePart.O_NORTHWALL) != null)
			{
				if (!_parent.TileEngine.CanTargetTile(ref originVoxel, targetTile, (int)TilePart.O_NORTHWALL, ref _targetVoxel, _unit, isPlayer))
				{
					_targetVoxel = _action.target.ToVoxel() + new Position(8, 0, 9);
				}
			}
			else if (targetTile.GetMapData(TilePart.O_WESTWALL) != null)
			{
				if (!_parent.TileEngine.CanTargetTile(ref originVoxel, targetTile, (int)TilePart.O_WESTWALL, ref _targetVoxel, _unit, isPlayer))
				{
					_targetVoxel = _action.target.ToVoxel() + new Position(0, 8, 9);
				}
			}
			else if (targetTile.GetMapData(TilePart.O_FLOOR) != null)
			{
				if (!_parent.TileEngine.CanTargetTile(ref originVoxel, targetTile, (int)TilePart.O_FLOOR, ref _targetVoxel, _unit, isPlayer))
				{
					_targetVoxel = _action.target.ToVoxel() + new Position(8, 8, 2);
				}
			}
			else
			{
				// dummy attempt (only to highlight obstacles)
				_parent.TileEngine.CanTargetTile(ref originVoxel, targetTile, MapData.O_DUMMY, ref _targetVoxel, _unit, isPlayer);

				// target nothing, targets the middle of the tile
				_targetVoxel = _action.target.ToVoxel() + TileEngine.voxelTileCenter;
			}
		}

		if (CreateNewProjectile())
		{
			var conf = weapon.GetActionConf(_action.type);
			if (_parent.Map.IsAltPressed || (conf != null && !conf.followProjectiles))
			{
				// temporarily turn off camera following projectiles to prevent annoying flashing effects (e.g. on minigun-like weapons)
				_parent.Map.FollowProjectile = false;
			}
			if (_range == 0) _action.SpendTU();
			_parent.Map.SetCursorType(CursorType.CT_NONE);
			_parent.Map.Camera.StopMouseScrolling();
			_parent.Map.DisableObstacles();
		}
		else if (isPlayer && (_targetVoxel.z >= 0 || forceEnableObstacles))
		{
			_parent.Map.EnableObstacles();
		}
	}

	/// <summary>Tries to create a projectile sprite and add it to the map, calculating its trajectory.</summary>
	/// <returns>True, if the projectile was successfully created.</returns>
	private bool CreateNewProjectile()
	{
		++_action.autoShotCounter;

		// Special handling for "spray" auto-attack, get target positions from the action's waypoints, starting from the back
		if (_action.sprayTargeting)
		{
			// Since we're just spraying, target the middle of the tile
			_targetVoxel = _action.waypoints[^1];
			Position targetPosition = _targetVoxel.ToTile();

			// The waypoint targeting is possibly out of range of the gun, so move the voxel to the max range of the gun if it is
			int distanceSq = _action.actor.Distance3dToPositionSq(targetPosition);
			if (_action.weapon.Rules.IsOutOfRange(distanceSq))
			{
				Position actorPosition = _action.actor.Position;
				int maxRange = _action.weapon.Rules.MaxRange;
				int distance = (int)Math.Ceiling(Math.Sqrt((float)distanceSq));
				_targetVoxel = (actorPosition + (targetPosition - actorPosition) * maxRange / distance).ToVoxel() + TileEngine.voxelTileCenter;
				targetPosition = _targetVoxel.ToTile();
			}

			// Turn at the end (to a potentially modified target position)
			_unit.LookAt(targetPosition, _unit.TurretType != -1);
			while (_unit.Status == STATUS_TURNING)
			{
				_unit.Turn(_unit.TurretType != -1);
			}

			_action.waypoints.RemoveAt(_action.waypoints.Count - 1);
		}

		// create a new projectile
		var projectile = new Projectile(_parent.Mod, _parent.Save, _action, _origin, _targetVoxel, _ammo);

		// add the projectile on the map
		_parent.Map.Projectile = projectile;

		// set the speed of the state think cycle to 16 ms (roughly one think cycle per frame)
		_parent.SetStateInterval(1000 / 60);

		// let it calculate a trajectory
		_projectileImpact = (int)V_EMPTY;

		double accuracyDivider = 100.0;
		// berserking units are half as accurate
		if (!_parent.PanicHandled)
		{
			accuracyDivider = 200.0;
		}

		BattleActionAttack attack = BattleActionAttack.GetAferShoot(_action, _ammo);
		if (_action.type == BA_THROW)
		{
			_projectileImpact = projectile.CalculateThrow(BattleUnit.GetFiringAccuracy(attack, _parent.Mod) / accuracyDivider);
			RuleItem ruleItem = _action.weapon.Rules;
			if (_projectileImpact == (int)V_FLOOR || _projectileImpact == (int)V_UNIT || _projectileImpact == (int)V_OBJECT)
			{
				if (_unit.Faction != FACTION_PLAYER && ruleItem.IsGrenadeOrProxy)
				{
					_action.weapon.FuseTimer = ruleItem.FuseTimerDefault;
				}
				_action.weapon.MoveToOwner(null);
				if (_action.weapon.Glow)
				{
					_parent.TileEngine.CalculateLighting(LL_UNITS, _unit.Position);
					_parent.TileEngine.CalculateFOV(_unit.Position, _action.weapon.GlowRange, false);
				}
				_parent.Mod.GetSoundByDepth((uint)_parent.Depth, (uint)Mod.ITEM_THROW).Play(-1, _parent.Map.GetSoundAngle(_unit.Position));
				if (!Mod.EXTENDED_EXPERIENCE_AWARD_SYSTEM)
				{
					// vanilla compatibility (throwing anything anywhere gives throwing exp)
					_unit.AddThrowingExp();
				}
			}
			else
			{
				// unable to throw here
				_parent.Map.Projectile = null;
				_action.result = "STR_UNABLE_TO_THROW_HERE";
				_action.ClearTU();
				_parent.PopState();
				return false;
			}
		}
		else if (_action.weapon.GetArcingShot(_action.type)) // special code for the "spit" trajectory
		{
			_projectileImpact = projectile.CalculateThrow(BattleUnit.GetFiringAccuracy(attack, _parent.Mod) / accuracyDivider);
			if (_projectileImpact != (int)V_EMPTY && _projectileImpact != (int)V_OUTOFBOUNDS)
			{
				// set the soldier in an aiming position
				_unit.Aim(true);
				// and we have a lift-off
				if (_ammo.Rules.FireSound != Mod.NO_SOUND)
				{
					_parent.Mod.GetSoundByDepth((uint)_parent.Depth, (uint)_ammo.Rules.FireSound).Play(-1, _parent.Map.GetSoundAngle(_unit.Position));
				}
				else if (_action.weapon.Rules.FireSound != Mod.NO_SOUND)
				{
					_parent.Mod.GetSoundByDepth((uint)_parent.Depth, (uint)_action.weapon.Rules.FireSound).Play(-1, _parent.Map.GetSoundAngle(_unit.Position));
				}
				if (_action.type != BA_LAUNCH)
				{
					_action.weapon.SpendAmmoForAction(_action.type, _parent.Save);
				}
			}
			else
			{
				// no line of fire
				_parent.Map.Projectile = null;
				if (_parent.PanicHandled)
				{
					_action.result = "STR_NO_TRAJECTORY";
				}
				_unit.AbortTurn();
				_parent.PopState();
				return false;
			}
		}
		else
		{
			if (_originVoxel != TileEngine.invalid)
			{
				_projectileImpact = projectile.CalculateTrajectory(BattleUnit.GetFiringAccuracy(attack, _parent.Mod) / accuracyDivider, _originVoxel, false);
			}
			else
			{
				_projectileImpact = projectile.CalculateTrajectory(BattleUnit.GetFiringAccuracy(attack, _parent.Mod) / accuracyDivider);
			}
			if (_targetVoxel != TileEngine.invalid.ToVoxel() && (_projectileImpact != (int)V_EMPTY || _action.type == BA_LAUNCH))
			{
				// set the soldier in an aiming position
				_unit.Aim(true);
				// and we have a lift-off
				if (_ammo.Rules.FireSound != Mod.NO_SOUND)
				{
					_parent.Mod.GetSoundByDepth((uint)_parent.Depth, (uint)_ammo.Rules.FireSound).Play(-1, _parent.Map.GetSoundAngle(projectile.Origin));
				}
				else if (_action.weapon.Rules.FireSound != Mod.NO_SOUND)
				{
					_parent.Mod.GetSoundByDepth((uint)_parent.Depth, (uint)_action.weapon.Rules.FireSound).Play(-1, _parent.Map.GetSoundAngle(projectile.Origin));
				}
				if (_action.type != BA_LAUNCH)
				{
					_action.weapon.SpendAmmoForAction(_action.type, _parent.Save);
				}
			}
			else
			{
				// no line of fire
				_parent.Map.Projectile = null;
				if (_parent.PanicHandled)
				{
					_action.result = "STR_NO_LINE_OF_FIRE";
				}
				_unit.AbortTurn();
				_parent.PopState();
				return false;
			}
		}

		if (_action.type != BA_THROW && _action.type != BA_LAUNCH)
			_unit.Statistics.shotsFiredCounter++;

		// hit log - new bullet
		if (_action.actor != null)
		{
			_parent.Save.AppendToHitLog(HitLogEntryType.HITLOG_NEW_SHOT, _action.actor.Faction);
		}

		return true;
	}

	/// <summary>Deinitialize the state.</summary>
	public override void Deinit()
	{
		_parent.Map.FollowProjectile = true; // turn back on when done shooting
	}

	/// <summary>Checks if a weapon has any more shots to fire.</summary>
	private bool NoMoreShotsToShoot()
	{
		return !_action.weapon.HaveNextShotsForAction(_action.type, _action.autoShotCounter) || _action.weapon.GetAmmoForAction(_action.type) == null;
	}

	/// <summary>
	/// Animates the projectile (moves to the next point in its trajectory).
	/// If the animation is finished the projectile sprite is removed from the map,
	/// and this state is finished.
	/// </summary>
	public override void Think()
	{
		_parent.Save.BattleState.ClearMouseScrollingState();
		/* TODO refactoring : store the projectile in this state, instead of getting it from the map each time? */
		if (_parent.Map.Projectile == null)
		{
			bool hasFloor = _action.actor.HaveNoFloorBelow() == false;
			bool unitCanFly = _action.actor.MovementType == MovementType.MT_FLY;

			if (_action.weapon.HaveNextShotsForAction(_action.type, _action.autoShotCounter)
				&& !_action.actor.IsOut
				&& _ammo.AmmoQuantity != 0
				&& (hasFloor || unitCanFly))
			{
				CreateNewProjectile();
				if (_action.cameraPosition.z != -1)
				{
					_parent.Map.Camera.MapOffset = _action.cameraPosition;
					_parent.Map.Invalidate();
				}
			}
			else
			{
				if (_action.cameraPosition.z != -1 && _action.waypoints.Count <= 1)
				{
					_parent.Map.Camera.MapOffset = _action.cameraPosition;
					_parent.Map.Invalidate();
				}
				if (!_parent.Save.UnitsFalling && _parent.PanicHandled)
				{
					_parent.TileEngine.CheckReactionFire(_unit, _action);
				}
				if (!_unit.IsOut)
				{
					_unit.AbortTurn();
				}
				if (_parent.Save.Side == FACTION_PLAYER || _parent.Save.DebugMode)
				{
					_parent.SetupCursor();
				}
				_parent.ConvertInfected();
				_parent.PopState();
			}
		}
		else
		{
			BattleActionAttack attack = BattleActionAttack.GetAferShoot(_action, _ammo);
			if (_action.type != BA_THROW && _ammo != null && _ammo.Rules.ShotgunPellets != 0)
			{
				// shotgun pellets move to their terminal location instantly as fast as possible
				_parent.Map.Projectile.SkipTrajectory();
			}
			if (!_parent.Map.Projectile.Move())
			{
				// impact !
				if (_action.type == BA_THROW)
				{
					_parent.Map.ResetCameraSmoothing();
					Position pos = _parent.Map.Projectile.GetPosition(Projectile.ItemDropVoxelOffset).ToTile();
					if (pos.y > _parent.Save.MapSizeY)
					{
						pos.y--;
					}
					if (pos.x > _parent.Save.MapSizeX)
					{
						pos.x--;
					}

					_parent.Mod.GetSoundByDepth((uint)_parent.Depth, (uint)Mod.ITEM_DROP).Play(-1, _parent.Map.GetSoundAngle(pos));
					RuleItem ruleItem = _action.weapon.Rules;
					if (_action.weapon.FuseThrowEvent())
					{
						if (ruleItem.BattleType == BT_GRENADE || ruleItem.BattleType == BT_PROXIMITYGRENADE)
						{
							// it's a hot grenade to explode immediately
							_parent.StatePushFront(new ExplosionBState(_parent, _parent.Map.Projectile.GetLastPositions(Projectile.ItemDropVoxelOffset), attack, null));
						}
						else
						{
							_parent.Save.RemoveItem(_action.weapon);
						}
					}
					else
					{
						_parent.DropItem(pos, _action.weapon);
						if (_unit.Faction != FACTION_PLAYER && ruleItem.IsGrenadeOrProxy)
						{
							_parent.TileEngine.SetDangerZone(pos, ruleItem.GetExplosionRadius(attack), _action.actor);
						}
					}
				}
				else if (_action.type == BA_LAUNCH && _action.waypoints.Count > 1 && _projectileImpact == (int)V_EMPTY)
				{
					_origin = _action.waypoints[0];
					_action.waypoints.RemoveAt(0);
					_action.target = _action.waypoints[0];
					// launch the next projectile in the waypoint cascade
					var nextWaypoint = new ProjectileFlyBState(_parent, _action, _origin, (int)(_range + _parent.Map.Projectile.Distance));
					nextWaypoint.SetOriginVoxel(_parent.Map.Projectile.GetPosition(-1));
					if (_origin == _action.target)
					{
						nextWaypoint.TargetFloor();
					}
					_parent.StatePushNext(nextWaypoint);
				}
				else
				{
					var tmpUnit = _parent.Save.GetTile(_action.target).Unit;
					if (tmpUnit != null && tmpUnit != _unit)
					{
						tmpUnit.Statistics.shotAtCounter++; // Only counts for guns, not throws or launches
					}

					_parent.Map.ResetCameraSmoothing();
					if (_action.type == BA_LAUNCH)
					{
						_action.weapon.SpendAmmoForAction(_action.type, _parent.Save);
					}

					if (_projectileImpact != (int)V_OUTOFBOUNDS)
					{
						bool shotgun = _ammo != null && _ammo.Rules.ShotgunPellets != 0 && _ammo.Rules.DamageType.IsDirect;
						int offset = 0;
						// explosions impact not inside the voxel but two steps back (projectiles generally move 2 voxels at a time)
						if (_ammo != null && _ammo.Rules.GetExplosionRadius(attack) != 0 && _projectileImpact != (int)V_UNIT)
						{
							offset = -2;
						}

						_parent.StatePushFront(new ExplosionBState(
							_parent, _parent.Map.Projectile.GetLastPositions(offset),
							attack, null,
							NoMoreShotsToShoot(),
							shotgun ? 0 : (int)(_range + _parent.Map.Projectile.Distance)
						));

						if (_projectileImpact == (int)V_UNIT)
						{
							ProjectileHitUnit(_parent.Map.Projectile.GetPosition(offset));
						}

						// remember unit's original XP values, used for nerfing below
						_unit.RememberXP();

						// special shotgun behaviour: trace extra projectile paths, and add bullet hits at their termination points.
						if (shotgun)
						{
							int behaviorType = _ammo.Rules.ShotgunBehaviorType;
							int spread = _ammo.Rules.ShotgunSpread;
							int choke = _action.weapon.Rules.ShotgunChoke;
							Position firstPelletImpact = _parent.Map.Projectile.GetPosition(-2);
							Position originalTarget = _targetVoxel;

							int i = 1;
							while (i != _ammo.Rules.ShotgunPellets)
							{
								if (behaviorType == 1)
								{
									// use impact location to determine spread (instead of originally targeted voxel), as long as it's not the same as the origin
									if (firstPelletImpact != _parent.Save.TileEngine.GetOriginVoxel(_action, _parent.Save.GetTile(_origin)))
									{
										_targetVoxel = firstPelletImpact;
									}
									else
									{
										_targetVoxel = originalTarget;
									}
								}

								var proj = new Projectile(_parent.Mod, _parent.Save, _action, _origin, _targetVoxel, _ammo);

								// let it trace to the point where it hits
								int secondaryImpact = (int)V_EMPTY;
								if (behaviorType == 1)
								{
									// pellet spread based on spread and choke values
									secondaryImpact = proj.CalculateTrajectory(Math.Max(0.0, (1.0 - spread / 100.0) * choke / 100.0));
								}
								else
								{
									// pellet spread based on spread and firing accuracy with diminishing formula
									// identical with vanilla formula when spread = 100 (default)
									secondaryImpact = proj.CalculateTrajectory(Math.Max(0.0, (BattleUnit.GetFiringAccuracy(attack, _parent.Mod) / 100.0) - i * 5.0 * spread / 100.0));
								}

								if (secondaryImpact != (int)V_EMPTY)
								{
									// as above: skip the shot to the end of it's path
									proj.SkipTrajectory();
									// insert an explosion and hit
									if (secondaryImpact != (int)V_OUTOFBOUNDS)
									{
										if (secondaryImpact == (int)V_UNIT)
										{
											ProjectileHitUnit(proj.GetPosition(offset));
										}
										var explosion = new Explosion(proj.GetPosition(offset), _ammo.Rules.HitAnimation, 0, false, false, _ammo.Rules.HitAnimationFrames);
										int power = 0;
										if (_action.weapon.Rules.IgnoreAmmoPower)
										{
											power = (int)(_action.weapon.Rules.GetPowerBonus(attack) - _action.weapon.Rules.GetPowerRangeReduction(proj.Distance)); // int - float, truncated
										}
										else
										{
											power = (int)(_ammo.Rules.GetPowerBonus(attack) - _ammo.Rules.GetPowerRangeReduction(proj.Distance)); // int - float, truncated
										}
										_parent.Map.Explosions.Add(explosion);
										_parent.Save.TileEngine.Hit(attack, proj.GetPosition(offset), power, _ammo.Rules.DamageType);
									}
								}
								++i;
							}

							// reset back for the next shot in the (potential) autoshot sequence
							_targetVoxel = originalTarget;
						}

						// nerf unit's XP values (gained via extra shotgun bullets)
						_unit.NerfXP();
					}
					else if (NoMoreShotsToShoot())
					{
						_unit.Aim(false);
					}
				}

				_parent.Map.Projectile = null;
			}
		}
	}

	/// <summary>Flying projectiles cannot be cancelled, but they can be "skipped".</summary>
	public override void Cancel()
	{
		if (_parent.Map.Projectile != null)
		{
			_parent.Map.Projectile.SkipTrajectory();
			Position p = _parent.Map.Projectile.GetPosition().ToTile();
			if (!_parent.Map.Camera.IsOnScreen(p, false, 0, false))
				_parent.Map.Camera.CenterOnPosition(p);
		}
		if (_parent.AreAllEnemiesNeutralized())
		{
			// stop autoshots when battle auto-ends
			_action.autoShotCounter = 1000;

			// Rationale: if there are any fatally wounded soldiers
			// the game still allows the player to resume playing the current turn (and heal them)
			// but we don't want to resume auto-shooting (it just looks silly)
		}
	}

	/// <summary>Validates the throwing range.</summary>
	/// <param name="action">Pointer to throw action.</param>
	/// <param name="origin">Position to throw from.</param>
	/// <param name="target">Tile to throw to.</param>
	/// <param name="depth">Battlescape depth.</param>
	/// <returns>True when the range is valid.</returns>
	public static bool ValidThrowRange(BattleAction action, Position origin, Tile target, int depth)
	{
		// note that all coordinates and thus also distances below are in number of tiles (not in voxels).
		if (action.type != BA_THROW)
		{
			return true;
		}
		int xdiff = action.target.x - action.actor.Position.x;
		int ydiff = action.target.y - action.actor.Position.y;
		int realDistanceSq = (xdiff * xdiff) + (ydiff * ydiff);

		int compatibilityDistanceSq = action.actor.Distance3dToPositionSq(action.target); // 3d distance for compatibility with Map::drawTerrain()
		if (action.weapon.Rules.IsOutOfThrowRange(compatibilityDistanceSq, depth))
		{
			// if out of item's throw range, stop... no need to check weight- and strength-based range
			return false;
		}

		double realDistance = Math.Sqrt((double)realDistanceSq);

		int offset = 2;
		int zd = (origin.z) - ((action.target.z * 24 + offset) - target.TerrainLevel);
		int weight = action.weapon.TotalWeight;
		double maxDistance = (GetMaxThrowDistance(weight, action.actor.BaseStats.strength, zd) + 8) / 16.0;

		if (depth > 0 && Mod.EXTENDED_UNDERWATER_THROW_FACTOR > 0)
		{
			maxDistance = maxDistance * (double)Mod.EXTENDED_UNDERWATER_THROW_FACTOR / 100.0;
		}

		return realDistance <= maxDistance;
	}

	/// <summary>Calculates the maximum throwing range.</summary>
	/// <param name="weight">The weight of the object.</param>
	/// <param name="strength">The strength of the thrower.</param>
	/// <param name="level">The difference in height between the thrower and the target.</param>
	/// <returns>The maximum throwing range.</returns>
	public static int GetMaxThrowDistance(int weight, int strength, int level)
	{
		double curZ = level + 0.5;
		double dz = 1.0;
		int dist = 0;
		while (dist < 4000) //just in case
		{
			dist += 8;
			if (dz < -1)
				curZ -= 8;
			else
				curZ += dz * 8;

			if (curZ < 0 && dz < 0) //roll back
			{
				dz = Math.Max(dz, -1.0);
				if (Math.Abs(dz) > 1e-10) //rollback horizontal
					dist = (int)(dist - curZ / dz); // C++ int -= double: computed in double, then truncated
				break;
			}
			dz -= (double)(50 * weight / strength) / 100;
			if (dz <= -2.0) //become falling
				break;
		}
		return dist;
	}

	/// <summary>Set the origin voxel, used for the blaster launcher.</summary>
	/// <param name="pos">The origin voxel.</param>
	public void SetOriginVoxel(Position pos)
	{
		_originVoxel = pos;
	}

	/// <summary>Set the boolean flag to angle a blaster bomb towards the floor.</summary>
	public void TargetFloor()
	{
		_targetFloor = true;
	}

	/// <summary>Updates the hit statistics of the unit hit by a projectile.</summary>
	public void ProjectileHitUnit(Position pos)
	{
		BattleUnit victim = _parent.Save.GetTile(pos.ToTile()).GetOverlappingUnit(_parent.Save);
		BattleUnit targetVictim = _parent.Save.GetTile(_action.target).Unit; // Who we were aiming at (not necessarily who we hit)
		if (victim != null && !victim.IsOut)
		{
			victim.Statistics.hitCounter++;
			if (_unit.OriginalFaction == FACTION_PLAYER && victim.OriginalFaction == FACTION_PLAYER)
			{
				victim.Statistics.shotByFriendlyCounter++;
				_unit.Statistics.shotFriendlyCounter++;
			}
			if (victim == targetVictim) // Hit our target
			{
				int distanceSq = _action.actor.Distance3dToUnitSq(victim);
				int distance = (int)Math.Ceiling(Math.Sqrt((float)distanceSq));
				int accuracy = BattleUnit.GetFiringAccuracy(BattleActionAttack.GetAferShoot(_action, _ammo), _parent.Mod);

				{
					int upperLimit = 0, lowerLimit = 0;
					int dropoff = _action.weapon.Rules.CalculateLimits(ref upperLimit, ref lowerLimit, _parent.Save.Depth, _action.type);

					if (distance > upperLimit)
					{
						accuracy -= (distance - upperLimit) * dropoff;
					}
					else if (distance < lowerLimit)
					{
						accuracy -= (lowerLimit - distance) * dropoff;
					}
					if (accuracy < 0)
					{
						accuracy = 0;
					}
				}

				_unit.Statistics.shotsLandedCounter++;
				if (distance > 30)
				{
					_unit.Statistics.longDistanceHitCounter++;
				}
				if (accuracy < distance)
				{
					_unit.Statistics.lowAccuracyHitCounter++;
				}
			}
		}
	}
}
