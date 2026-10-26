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
// Ported from: src/Battlescape/PsiAttackBState.h, src/Battlescape/PsiAttackBState.cpp
#nullable disable

using static Acom.UnitFaction;

namespace Acom;

/// <summary>A Psi Attack state.</summary>
public class PsiAttackBState : BattleState
{
#pragma warning disable IDE0052 // kept like the C++ members
	private BattleUnit _unit, _target;
	private BattleItem _item;
#pragma warning restore IDE0052
	private bool _initialized;

	/// <summary>Sets up a PsiAttackBState.</summary>
	public PsiAttackBState(BattlescapeGame parent, BattleAction action) : base(parent, action)
	{
		_unit = null;
		_target = null;
		_item = null;
		_initialized = false;
	}

	/// <summary>
	/// Initializes the sequence:
	/// - checks if the action is valid,
	/// - adds a psi attack animation to the world.
	/// - from that point on, the explode state takes precedence (and perform psi attack).
	/// - when that state pops, we'll do our first think()
	/// </summary>
	public override void Init()
	{
		if (_initialized) return;
		_initialized = true;

		_item = _action.weapon;

		if (_item == null) // can't make a psi attack without a weapon
		{
			_parent.PopState();
			return;
		}

		if (_parent.Save.GetTile(_action.target) == null) // invalid target position
		{
			_parent.PopState();
			return;
		}

		_unit = _action.actor;

		int distanceSq = _action.actor.Distance3dToPositionSq(_action.target);
		if (_action.weapon.Rules.IsOutOfRange(distanceSq))
		{
			// out of range
			_action.result = "STR_OUT_OF_RANGE";
			_parent.PopState();
			return;
		}

		_target = _parent.Save.GetTile(_action.target).Unit;

		if (_target == null) // invalid target
		{
			_parent.PopState();
			return;
		}

		if (!_action.SpendTU(ref _action.result)) // not enough time units
		{
			_parent.PopState();
			return;
		}

		int height = _target.FloatHeight + (_target.Height / 2) - _parent.Save.GetTile(_action.target).TerrainLevel;
		Position voxel = _action.target.ToVoxel() + new Position(8, 8, height);
		_parent.StatePushFront(new ExplosionBState(_parent, new LastPositions(voxel), BattleActionAttack.GetAferShoot(_action, _action.weapon), null));
	}

	/// <summary>After the explosion animation is done doing its thing, restore the camera/cursor.</summary>
	public override void Think()
	{
		if (_action.cameraPosition.z != -1)
		{
			_parent.Map.Camera.MapOffset = _action.cameraPosition;
			_parent.Map.Invalidate();
		}
		if (_parent.Save.Side == FACTION_PLAYER || _parent.Save.DebugMode)
		{
			_parent.SetupCursor();
		}
		_parent.PopState();
	}
}
