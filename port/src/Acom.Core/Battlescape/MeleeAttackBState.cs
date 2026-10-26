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
// Ported from: src/Battlescape/MeleeAttackBState.h, src/Battlescape/MeleeAttackBState.cpp
#nullable disable

using static Acom.BattleActionType;
using static Acom.UnitFaction;
using static Acom.UnitStatus;

namespace Acom;

/// <summary>A Melee Attack state.</summary>
public class MeleeAttackBState : BattleState
{
	private BattleUnit _unit, _target;
	private BattleItem _weapon, _ammo;
	private Position _voxel;
	private int _hitNumber;
	private bool _initialized;
	private bool _reaction;

	/// <summary>Sets up a MeleeAttackBState.</summary>
	public MeleeAttackBState(BattlescapeGame parent, BattleAction action) : base(parent, action)
	{
		_unit = null;
		_target = null;
		_weapon = null;
		_ammo = null;
		_hitNumber = 0;
		_initialized = false;
		_reaction = false;
	}

	/// <summary>Initializes the sequence. Does a lot of validity checking.</summary>
	public override void Init()
	{
		if (_initialized) return;
		_initialized = true;

		int terrainMeleeTilePart = _action.terrainMeleeTilePart;
		_action.terrainMeleeTilePart = 0; // reset!

		_weapon = _action.weapon;
		if (_weapon == null) // can't hit without weapon
		{
			_parent.PopState();
			return;
		}

		_unit = _action.actor;

		bool reactionShoot = _unit.Faction != _parent.Save.Side;
		if (reactionShoot)
		{
			_ammo = _action.weapon.GetAmmoForAction(BA_HIT);
		}
		else
		{
			_ammo = _action.weapon.GetAmmoForAction(BA_HIT, ref _action.result);
		}
		if (_ammo == null)
		{
			_parent.PopState();
			return;
		}

		if (_parent.Save.GetTile(_action.target) == null) // invalid target position
		{
			_parent.PopState();
			return;
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
			// no ammo or target is dead: give the time units back and cancel the shot.
			BattleUnit target = _parent.Save.GetTile(_action.target).Unit;
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

		//spend TU
		if (!_action.SpendTU(ref _action.result))
		{
			_parent.PopState();
			return;
		}

		// terrain melee
		if (terrainMeleeTilePart > 0)
		{
			_voxel = _action.target.ToVoxel() + new Position(8, 8, 12);
			PerformMeleeAttack(terrainMeleeTilePart);
			return;
		}

		AIModule ai = _unit.AIModule;

		if (_unit.Faction == _parent.Save.Side &&
			_unit.Faction != FACTION_PLAYER &&
			BattlescapeGame._debugPlay == false &&
			ai != null && ai.Target != null)
		{
			_target = ai.Target;
		}
		else
		{
			_target = _parent.Save.GetTile(_action.target).Unit;
		}

		if (_target == null)
		{
			throw new InvalidOperationException("This is a known (but tricky) bug... still fixing it, sorry. In the meantime, try save scumming option or kill all aliens in debug mode to finish the mission.");
		}

		int height = _target.FloatHeight + (_target.Height / 2) - _parent.Save.GetTile(_action.target).TerrainLevel;
		_voxel = _action.target.ToVoxel() + new Position(8, 8, height);

		if (_parent.Save.GetTile(_voxel.ToTile()) == null)
		{
			throw new InvalidOperationException("Melee attack animation overflow: target voxel is outside of the map boundaries.");
		}

		if (_unit.Faction == FACTION_HOSTILE)
		{
			_hitNumber = _weapon.Rules.AIMeleeHitCount - 1;
		}

		PerformMeleeAttack();
	}

	/// <summary>Performs all the overall functions of the state, this code runs AFTER the explosion state pops.</summary>
	public override void Think()
	{
		_parent.Save.BattleState.ClearMouseScrollingState();
		if (_reaction && !_parent.Save.UnitsFalling)
		{
			_reaction = false;
			if (_parent.TileEngine.CheckReactionFire(_unit, _action))
			{
				return;
			}
		}

		// if the unit burns floor tiles, burn floor tiles
		if (_unit.SpecialAbility == (int)SpecialAbility.SPECAB_BURNFLOOR || _unit.SpecialAbility == (int)SpecialAbility.SPECAB_BURN_AND_EXPLODE)
		{
			_parent.Save.GetTile(_action.target).Ignite(15);
		}
		if (_hitNumber > 0 &&
			// not performing a reaction attack
			_unit.Faction == _parent.Save.Side &&
			// whose target is still alive or at least conscious
			_target != null && !_target.IsOutThresholdExceed &&
			// and we still have ammo to make the attack
			_weapon.GetAmmoForAction(BA_HIT) != null &&
			// spend the TUs immediately
			_action.SpendTU())
		{
			--_hitNumber;
			PerformMeleeAttack();
		}
		else
		{
			if (_action.cameraPosition.z != -1)
			{
				_parent.Map.Camera.MapOffset = _action.cameraPosition;
				_parent.Map.Invalidate();
			}

			if (_unit.Faction == _parent.Save.Side) // not a reaction attack
			{
				_parent.CurrentAction.type = BA_NONE; // do this to restore cursor
			}

			if (_parent.Save.Side == FACTION_PLAYER || _parent.Save.DebugMode)
			{
				_parent.SetupCursor();
			}
			_parent.ConvertInfected();
			_parent.PopState();
		}
	}

	/// <summary>Sets up a melee attack, inserts an explosion into the map and make noises.</summary>
	public void PerformMeleeAttack(int terrainMeleeTilePart = 0)
	{
		// set the soldier in an aiming position
		_unit.Aim(true);

		// use up ammo if applicable
		_action.weapon.SpendAmmoForAction(BA_HIT, _parent.Save);
		_parent.Map.SetCursorType(CursorType.CT_NONE);

		// offset the damage voxel ever so slightly so that the target knows which side the attack came from
		Position difference = _unit.Position - _action.target;
		// large units may cause it to offset too much, so we'll clamp the values.
		difference.x = Math.Clamp(difference.x, (short)-1, (short)1);
		difference.y = Math.Clamp(difference.y, (short)-1, (short)1);

		Position damagePosition = _voxel + difference;

		// make an explosion action
		_parent.StatePushFront(new ExplosionBState(_parent, new LastPositions(damagePosition), BattleActionAttack.GetAferShoot(_action, _ammo), null, true, 0, 0, terrainMeleeTilePart));

		_reaction = true;
	}
}
