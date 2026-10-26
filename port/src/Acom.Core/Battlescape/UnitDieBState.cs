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
// Ported from: src/Battlescape/UnitDieBState.h, src/Battlescape/UnitDieBState.cpp
#nullable disable

using static Acom.LightLayers;
using static Acom.UnitFaction;
using static Acom.UnitStatus;

namespace Acom;

/// <summary>State for dying units.</summary>
public class UnitDieBState : BattleState
{
	private readonly BattleUnit _unit;
	private readonly RuleDamageType _damageType;
	private readonly bool _noSound;
	private int _extraFrame;
	private readonly bool _overKill;

	/// <summary>Sets up an UnitDieBState.</summary>
	/// <param name="parent">Pointer to the Battlescape.</param>
	/// <param name="unit">Dying unit.</param>
	/// <param name="damageType">Type of damage that caused the death.</param>
	/// <param name="noSound">Whether to disable the death sound.</param>
	public UnitDieBState(BattlescapeGame parent, BattleUnit unit, RuleDamageType damageType, bool noSound) : base(parent)
	{
		_unit = unit;
		_damageType = damageType;
		_noSound = noSound;
		_extraFrame = 0;
		_overKill = unit.OverKillDamage != 0;

		// don't show the "fall to death" animation when a unit is blasted with explosives or he is already unconscious
		if (!_damageType.IsDirect || _unit.Status == STATUS_UNCONSCIOUS)
		{
			/********************************************************
			Proclamation from Lord Xenu:

			any unit that is going to skip its death pirouette
			MUST have its direction set to 3 first.

			Failure to comply is treason, and treason is punishable
			by death. (after being correctly oriented)

			********************************************************/
			_unit.Direction = 3;

			_unit.InstaFalling();
			if (_parent.Save.IsBeforeGame)
			{
				ConvertUnitToCorpse();
				_extraFrame = 3; // shortcut to popState()
			}
		}
		else
		{
			if (_unit.Faction == FACTION_PLAYER)
			{
				_parent.Map.SetUnitDying(true);
			}
			_parent.SetStateInterval(BattlescapeState.DEFAULT_ANIM_SPEED);
			if (_unit.Direction != 3)
			{
				_parent.SetStateInterval(BattlescapeState.DEFAULT_ANIM_SPEED / 3);
			}
		}

		_unit.ClearVisibleTiles();
		_unit.ClearVisibleUnits();
		_unit.FreePatrolTarget();

		if (!_parent.Save.IsBeforeGame && _unit.Faction == FACTION_HOSTILE)
		{
			List<Node> nodes = _parent.Save.Nodes;
			if (nodes == null) return; // this better not happen.

			foreach (var node in nodes)
			{
				if (!node.IsDummy && Position.DistanceSq(node.Position, _unit.Position) < 4)
				{
					node.Type = node.Type | Node.TYPE_DANGEROUS;
				}
			}
		}
	}

	/// <summary>Initializes the state.</summary>
	public override void Init()
	{
		// check for presence of battlestate to ensure that we're not pre-battle
		// check for the unit's tile to make sure we're not trying to kill a dead guy
		if (_parent.Save.BattleState != null && _unit.Tile == null)
		{
			if (_unit.OriginalFaction == FACTION_PLAYER)
			{
				if (_unit.NotificationShown == 2)
				{
					// skip completely
					_parent.PopState();
				}
				else if (_unit.NotificationShown == 1)
				{
					// can't skip this (there could still be a death notification), but at least speed it up
					_parent.SetStateInterval(1);
				}
			}
			else
			{
				_parent.PopState();
			}
		}
	}

	/// <summary>
	/// Runs state functionality every cycle.
	/// Progresses the death, displays any messages, checks if the mission is over, ...
	/// </summary>
	public override void Think()
	{
		if (_extraFrame == 3)
		{
			_parent.PopState();
			return;
		}
		if (_unit.Direction != 3 && _damageType.IsDirect)
		{
			int dir = _unit.Direction + 1;
			if (dir == 8)
			{
				dir = 0;
			}
			_unit.LookAt(dir);
			_unit.Turn();
			if (dir == 3)
			{
				_parent.SetStateInterval(BattlescapeState.DEFAULT_ANIM_SPEED);
			}
		}
		else if (_unit.Status == STATUS_COLLAPSING)
		{
			_unit.KeepFalling();
		}
		else if (!_unit.IsOut)
		{
			_unit.StartFalling();

			if (!_noSound)
			{
				PlayDeathSound();
			}
			if (_unit.Respawn)
			{
				while (_unit.Status == STATUS_COLLAPSING)
				{
					_unit.KeepFalling();
				}
			}
		}
		if (_extraFrame == 2)
		{
			_parent.Map.SetUnitDying(false);
			_parent.TileEngine.CalculateLighting(LL_ITEMS, _unit.Position, _unit.Armor.Size);
			_parent.TileEngine.CalculateFOV(_unit.Position, _unit.Armor.Size, false); //Update FOV for anyone that can see me
			_parent.PopState();
			if (_unit.OriginalFaction == FACTION_PLAYER)
			{
				Game game = _parent.Save.BattleState.Game;
				if (_unit.Status == STATUS_DEAD)
				{
					if (_damageType.ResistType == ItemDamageType.DT_NONE && _unit.SpawnUnit == null)
					{
						// Note: yes, this condition is necessary, init() will filter out most duplicates, but not everything
						if (_unit.NotificationShown < 2)
						{
							_unit.NotificationShown = 2;
							game.PushState(new InfoboxOKState(game.Language.GetString("STR_HAS_DIED_FROM_A_FATAL_WOUND", _unit.Gender).Arg(_unit.GetName(game.Language))));
						}
					}
					else if (Options.battleNotifyDeath && _unit.GeoscapeSoldier != null)
					{
						// Note: yes, this condition is necessary, init() will filter out most duplicates, but not everything
						if (_unit.NotificationShown < 2)
						{
							_unit.NotificationShown = 2;
							game.PushState(new InfoboxState(game.Language.GetString("STR_HAS_BEEN_KILLED", _unit.Gender).Arg(_unit.GetName(game.Language))));
						}
					}
				}
				else if (_unit.IndicatorsAreEnabled())
				{
					if (_unit.NotificationShown < 1)
					{
						_unit.NotificationShown = 1;
						game.PushState(new InfoboxOKState(game.Language.GetString("STR_HAS_BECOME_UNCONSCIOUS", _unit.Gender).Arg(_unit.GetName(game.Language))));
					}
				}
			}
			// if all units from either faction are killed - auto-end the mission.
			if (_parent.Save.Side == FACTION_PLAYER)
			{
				_parent.AutoEndBattle();
			}
		}
		else if (_extraFrame == 1)
		{
			_extraFrame++;
		}
		else if (_unit.IsOut)
		{
			_extraFrame = 1;
			if (!_noSound && !_damageType.IsDirect && _unit.Status != STATUS_UNCONSCIOUS)
			{
				PlayDeathSound();
			}
			if (_unit.Status == STATUS_UNCONSCIOUS && !_unit.Capturable)
			{
				_unit.InstaKill();
			}
			_unit.ResetTurnsSince();
			if (_unit.SpawnUnit != null && !_overKill)
			{
				if (!_unit.AlreadyRespawned)
				{
					// converts the dead zombie to a chryssalid
					_parent.ConvertUnit(_unit);
				}
			}
			else
			{
				ConvertUnitToCorpse();
			}

			_parent.Save.ClearUnitSelection(_unit);
		}
	}

	/// <summary>Unit falling cannot be cancelled.</summary>
	public override void Cancel()
	{
	}

	/// <summary>Converts unit to a corpse (item).</summary>
	public void ConvertUnitToCorpse()
	{
		Position lastPosition = _unit.Position;
		int size = _unit.Armor.Size;
		bool dropItems = (_unit.HasInventory &&
			(!Options.weaponSelfDestruction ||
			(_unit.OriginalFaction != FACTION_HOSTILE || _unit.Status == STATUS_UNCONSCIOUS)));

		if (!_noSound)
		{
			_parent.Save.BattleState.ResetUiButton();
		}
		// remove the unconscious body item corresponding to this unit, and if it was being carried, keep track of what slot it was in
		if (lastPosition != TileEngine.invalid)
		{
			_parent.Save.RemoveUnconsciousBodyItem(_unit);
		}

		// move inventory from unit to the ground
		if (dropItems && _unit.Tile != null)
		{
			_parent.TileEngine.ItemDropInventory(_unit.Tile, _unit);
		}

		// remove unit-tile link
		_unit.SetTile(null, _parent.Save);

		if (lastPosition == TileEngine.invalid) // we're being carried
		{
			if (_overKill)
			{
				_parent.Save.RemoveUnconsciousBodyItem(_unit);
			}
			else
			{
				// replace the unconscious body item with a corpse in the carrying unit's inventory
				foreach (var bi in _parent.Save.Items)
				{
					if (bi.Unit == _unit)
					{
						var corpseRules = _unit.Armor.CorpseBattlescape[0]; // we're in an inventory, so we must be a 1x1 unit
						bi.ConvertToCorpse(corpseRules);
						break;
					}
				}
			}
		}
		else
		{
			if (!_overKill)
			{
				int i = size * size - 1;
				for (int y = size - 1; y >= 0; --y)
				{
					for (int x = size - 1; x >= 0; --x)
					{
						BattleItem corpse = _parent.Save.CreateItemForTile(_unit.Armor.CorpseBattlescape[i], null, _unit);
						_parent.DropItem(lastPosition + new Position(x, y, 0), corpse, false);
						--i;
					}
				}
			}
			else
			{
				_parent.Save.TileEngine.ApplyGravity(_parent.Save.GetTile(lastPosition));
			}
		}
	}

	/// <summary>Plays the death sound.</summary>
	public void PlayDeathSound()
	{
		List<int> sounds = _unit.DeathSounds;
		if (sounds.Count != 0)
		{
			int i = sounds[Rng.Generate(0, sounds.Count - 1)];
			if (i >= 0)
			{
				_parent.Mod.GetSoundByDepth((uint)_parent.Depth, (uint)i).Play(-1, _parent.Map.GetSoundAngle(_unit.Position));
			}
		}
	}
}
