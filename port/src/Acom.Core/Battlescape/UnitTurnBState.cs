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
// Ported from: src/Battlescape/UnitTurnBState.h, src/Battlescape/UnitTurnBState.cpp
#nullable disable

using static Acom.BattleActionType;
using static Acom.UnitFaction;
using static Acom.UnitStatus;

namespace Acom;

/// <summary>State for turning units.</summary>
public class UnitTurnBState : BattleState
{
	private BattleUnit _unit;
	private bool _turret, _chargeTUs;

	/// <summary>Sets up an UnitTurnBState.</summary>
	/// <param name="parent">Pointer to the Battlescape.</param>
	/// <param name="action">Pointer to an action.</param>
	/// <param name="chargeTUs">Spend time units for turning?</param>
	public UnitTurnBState(BattlescapeGame parent, BattleAction action, bool chargeTUs = true) : base(parent, action)
	{
		_unit = null;
		_turret = false;
		_chargeTUs = chargeTUs;
	}

	/// <summary>Initializes the state.</summary>
	public override void Init()
	{
		_unit = _action.actor;
		if (_unit.IsOut)
		{
			_parent.PopState();
			return;
		}
		_action.ClearTU();
		if (_unit.Faction == FACTION_PLAYER)
			_parent.SetStateInterval((uint)Options.battleXcomSpeed);
		else
			_parent.SetStateInterval((uint)Options.battleAlienSpeed);

		// if the unit has a turret and we are turning during targeting, then only the turret turns
		_turret = _unit.TurretType != -1 && (_action.targeting || _action.strafe);

		_unit.LookAt(_action.target, _turret);

		if (_chargeTUs && _unit.Status != STATUS_TURNING)
		{
			if (_action.type == BA_NONE)
			{
				// try to open a door
				int door = _parent.TileEngine.UnitOpensDoor(_unit, true);
				if (door == 0)
				{
					_parent.Mod.GetSoundByDepth((uint)_parent.Depth, (uint)Mod.DOOR_OPEN).Play(-1, _parent.Map.GetSoundAngle(_unit.Position)); // normal door
				}
				if (door == 1)
				{
					_parent.Mod.GetSoundByDepth((uint)_parent.Depth, (uint)Mod.SLIDING_DOOR_OPEN).Play(-1, _parent.Map.GetSoundAngle(_unit.Position)); // ufo door
				}
				if (door == 4)
				{
					_action.result = "STR_NOT_ENOUGH_TIME_UNITS";
				}
			}
			_parent.PopState();
		}
	}

	/// <summary>Runs state functionality every cycle.</summary>
	public override void Think()
	{
		int tu = _chargeTUs ? (_turret ? 1 : _unit.TurnCost) : 0;

		if (_chargeTUs && _unit.Faction == _parent.Save.Side && _parent.PanicHandled && !_action.targeting && !_parent.CheckReservedTU(_unit, tu, 0))
		{
			_unit.AbortTurn();
			_parent.PopState();
			return;
		}

		if (_unit.SpendTimeUnits(tu))
		{
			int unitSpotted = _unit.UnitsSpottedThisTurn.Count;
			_unit.Turn(_turret);
			_parent.TileEngine.CalculateFOV(_unit);
			if (_chargeTUs && _unit.Faction == _parent.Save.Side && _parent.PanicHandled && _action.type == BA_NONE && _unit.UnitsSpottedThisTurn.Count > unitSpotted)
			{
				_unit.AbortTurn();
				_parent.PopState();
			}
			else if (_unit.Status == STATUS_STANDING)
			{
				_parent.PopState();

				if (_action.kneel && !_unit.IsFloating && !_unit.IsKneeled)
				{
					var kneel = new BattleAction();
					kneel.type = BA_KNEEL;
					kneel.actor = _unit;
					kneel.Time = (short)_unit.KneelChangeCost;
					if (kneel.SpendTU())
					{
						_unit.Kneel(!_unit.IsKneeled);
						// kneeling or standing up can reveal new terrain or units. I guess.
						_parent.TileEngine.CalculateFOV(_unit.Position, 1, false); //Update unit FOV for everyone through this position, skip tiles.
						_parent.TileEngine.CheckReactionFire(_unit, kneel);
					}
				}
			}
		}
		else if (_parent.PanicHandled)
		{
			_action.result = "STR_NOT_ENOUGH_TIME_UNITS";
			_unit.AbortTurn();
			_parent.PopState();
		}
	}

	/// <summary>Unit turning cannot be cancelled.</summary>
	public override void Cancel()
	{
	}
}
