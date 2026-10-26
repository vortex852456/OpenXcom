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
// Ported from: src/Battlescape/UnitPanicBState.h, src/Battlescape/UnitPanicBState.cpp
#nullable disable

using static Acom.BattleActionType;
using static Acom.UnitStatus;

namespace Acom;

/// <summary>State for panicking units.</summary>
public class UnitPanicBState : BattleState
{
	private BattleUnit _unit;
	private bool _berserking;
	private int _shotsFired;

	/// <summary>Sets up an UnitPanicBState.</summary>
	/// <param name="parent">Pointer to the Battlescape.</param>
	/// <param name="unit">Panicking unit.</param>
	public UnitPanicBState(BattlescapeGame parent, BattleUnit unit) : base(parent)
	{
		_unit = unit;
		_shotsFired = 0;
		_berserking = _unit.Status == STATUS_BERSERK;
		unit.AbortTurn(); //makes the unit go to status STANDING :p
	}

	/// <summary>Initializes the state.</summary>
	public override void Init()
	{
	}

	/// <summary>Runs state functionality every cycle. Ends the panicking when done.</summary>
	public override void Think()
	{
		if (_unit != null)
		{
			// berserking requires handling here, as the target selection isn't completely random
			// and needs updating between shots.
			if (!_unit.IsOut && _shotsFired < 10 && _berserking)
			{
				_shotsFired++;
				var ba = new BattleAction();
				ba.actor = _unit;
				ba.weapon = _unit.GetMainHandWeapon();
				{
					// make autoshots if possible.
					ba.type = BA_AUTOSHOT;
					ba.UpdateTU();
					bool canShoot = ba.HaveTU() && _parent.Save.CanUseWeapon(ba.weapon, ba.actor, _berserking, ba.type);

					if (!canShoot)
					{
						ba.type = BA_SNAPSHOT;
						ba.UpdateTU();
						canShoot = ba.HaveTU() && _parent.Save.CanUseWeapon(ba.weapon, ba.actor, _berserking, ba.type);
					}

					if (!canShoot && Mod.EXTENDED_BERSERK_WITH_AIMED > 0)
					{
						if (Mod.EXTENDED_BERSERK_WITH_AIMED == 1 && ba.weapon.CurrentWaypoints != 0)
						{
							// can use BA_AIMEDSHOT, but cannot use BA_LAUNCH
						}
						else
						{
							ba.type = BA_AIMEDSHOT;
							ba.UpdateTU();
							canShoot = ba.HaveTU() && _parent.Save.CanUseWeapon(ba.weapon, ba.actor, _berserking, ba.type);
						}
					}

					if (canShoot)
					{
						// if we see enemies, shoot at the closest living one.
						if (_unit.VisibleUnits.Count != 0)
						{
							int dist = 255;
							foreach (BattleUnit bu in _unit.VisibleUnits)
							{
								int newDist = Position.Distance2d(_unit.Position, bu.Position);
								if (newDist < dist)
								{
									ba.target = bu.Position;
									dist = newDist;
								}
							}
						}
						else // otherwise shoot randomly
						{
							// C++ leaves the argument evaluation order unspecified; GCC (golden reference) evaluates
							// the constructor arguments right to left, so the y offset is rolled first.
							int randomY = Rng.Generate(-6, 6);
							int randomX = Rng.Generate(-6, 6);
							ba.target = new Position(_unit.Position.x + randomX, _unit.Position.y + randomY, _unit.Position.z);
						}
						// include the cost for facing our target
						int turnCost = Math.Abs(_unit.Direction - _unit.DirectionTo(ba.target));
						if (turnCost > 4)
						{
							turnCost = 8 - turnCost;
						}
						turnCost = turnCost * _unit.TurnCost;

						_unit.SpendTimeUnits(turnCost);
						_parent.StatePushFront(new UnitTurnBState(_parent, ba, false));
						// even if we don't have enough TUs to turn AND shoot, we still want to turn.
						if (ba.HaveTU())
						{
							_parent.StatePushNext(new ProjectileFlyBState(_parent, ba));
						}
					}
				}
				return;
			}
			if (!_unit.IsOut)
			{
				_unit.AbortTurn(); // set the unit status to standing in case it wasn't otherwise changed from berserk/panicked
			}
			// reset the unit's time units when all panicking is done
			_unit.ClearTimeUnits();
			_unit.MoraleChange(+15);
		}
		_parent.PopState();
		_parent.SetupCursor();
	}

	/// <summary>Panicking cannot be cancelled.</summary>
	public override void Cancel()
	{
	}
}
