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
// Ported from: src/Battlescape/ExplosionBState.h, src/Battlescape/ExplosionBState.cpp
#nullable disable

using static Acom.BattleActionType;
using static Acom.BattleType;
using static Acom.ItemDamageType;
using static Acom.UnitFaction;

namespace Acom;

/// <summary>
/// Explosion state not only handles explosions, but also bullet impacts!
/// Refactoring tip : ImpactBState.
/// </summary>
public class ExplosionBState : BattleState
{
	private readonly int _explosionCounter;
	private readonly int _terrainMeleeTilePart;
	private readonly BattleActionAttack _attack;
	private Position _center, _before;
	private RuleDamageType _damageType;
	private readonly Tile _tile;
	private BattleUnit _targetPsiOrHit;
	private int _power;
	private int _radius;
	private readonly int _range;
	private bool _areaOfEffect;
	private readonly bool _lowerWeapon;
	private bool _hit, _psi;

	/// <summary>Sets up an ExplosionBState.</summary>
	/// <param name="parent">Pointer to the BattleScape.</param>
	/// <param name="center">Center position in voxelspace.</param>
	/// <param name="attack">Attack involved in the explosion (copied, like the C++ value parameter).</param>
	/// <param name="tile">Tile the explosion is on.</param>
	/// <param name="lowerWeapon">Whether the unit causing this explosion should now lower their weapon.</param>
	/// <param name="range">Distance between weapon and target.</param>
	/// <param name="explosionCounter">Counter for chain terrain explosions.</param>
	/// <param name="terrainMeleeTilePart">Tile part for terrain melee.</param>
	public ExplosionBState(BattlescapeGame parent, LastPositions center, BattleActionAttack attack, Tile tile, bool lowerWeapon = false, int range = 0, int explosionCounter = 0, int terrainMeleeTilePart = 0) : base(parent)
	{
		_explosionCounter = explosionCounter;
		_terrainMeleeTilePart = terrainMeleeTilePart;
		_attack = new BattleActionAttack(attack.type, attack.attacker, attack.weapon_item, attack.damage_item, attack.skill_rules);
		_center = center.last;
		_before = center.before;
		_damageType = null;
		_tile = tile;
		_targetPsiOrHit = null;
		_power = 0;
		_radius = 6;
		_range = range;
		_areaOfEffect = false;
		_lowerWeapon = lowerWeapon;
		_hit = false;
		_psi = false;
	}

	/// <summary>Set new value to reference if new value is not equal -1.</summary>
	/// <param name="oldValue">Old value to change.</param>
	/// <param name="newValue">New value to set, but only if is not equal -1.</param>
	private static void OptValue(ref int oldValue, int newValue)
	{
		if (newValue != -1)
		{
			oldValue = newValue;
		}
	}

	/// <summary>
	/// Initializes the explosion.
	/// The animation and sound starts here.
	/// If the animation is finished, the actual effect takes place.
	/// </summary>
	public override void Init()
	{
		BattleType type = BT_NONE;
		BattleActionType action = _attack.type;
		RuleItem itemRule = null;
		bool miss = false;
		if (_attack.damage_item != null)
		{
			itemRule = _attack.damage_item.Rules;
			type = itemRule.BattleType;

			_power = 0;
			_hit = action == BA_HIT;
			_psi = type == BT_PSIAMP && action != BA_USE && !_hit;
			if (_hit && type != BT_MELEE)
			{
				_power += itemRule.GetMeleeBonus(_attack);

				_radius = 0;
				_damageType = itemRule.MeleeType;
			}
			else
			{
				// Note: C++ int -= float is computed in float, then truncated.
				if (_attack.weapon_item != null && _attack.weapon_item.Rules.IgnoreAmmoPower)
				{
					_power += _attack.weapon_item.Rules.GetPowerBonus(_attack);
					_power = (int)(_power - _attack.weapon_item.Rules.GetPowerRangeReduction(_range));
				}
				else
				{
					_power += itemRule.GetPowerBonus(_attack);
					_power = (int)(_power - itemRule.GetPowerRangeReduction(_range));
				}

				_radius = itemRule.GetExplosionRadius(_attack);
				_damageType = itemRule.DamageType;
			}

			if (type == BT_PSIAMP || _hit)
			{
				Position targetPos = _center.ToTile();
				_targetPsiOrHit = _parent.Save.GetTile(targetPos).GetOverlappingUnit(_parent.Save);
			}

			//testing if we hit target
			if (action == BA_SELF_DESTRUCT)
			{
				if (!Rng.Percent(itemRule.SpecialChance))
				{
					_power = 0;
				}
			}
			else if (type == BT_PSIAMP && !_hit)
			{
				if (action != BA_USE)
				{
					_power = 0;
				}
				if (!_parent.TileEngine.PsiAttack(_attack, _targetPsiOrHit))
				{
					_power = 0;
					miss = true;
				}
				else
				{
					_parent.PsiAttackMessage(_attack, _targetPsiOrHit);
				}
			}
			else if (type == BT_MELEE || _hit)
			{
				if (!_parent.TileEngine.MeleeAttack(_attack, _targetPsiOrHit, _terrainMeleeTilePart))
				{
					_power = 0;
					miss = true;
				}
			}
			else if (type == BT_FIREARM)
			{
				if (_power <= 0)
				{
					miss = true;
				}
			}

			_areaOfEffect = type != BT_MELEE && _radius != 0 &&
							(type != BT_PSIAMP || action == BA_USE) &&
							!_hit && !miss;
		}
		else if (_tile != null)
		{
			ItemDamageType DT;
			switch (_tile.ExplosiveType)
			{
				case 0:
					DT = DT_HE;
					break;
				case 5:
					DT = DT_IN;
					break;
				case 6:
					DT = DT_STUN;
					break;
				default:
					DT = DT_SMOKE;
					break;
			}
			_power = _tile.Explosive;
			_tile.SetExplosive(0, 0, true);
			_damageType = _parent.Mod.GetDamageType(DT);
			_radius = _power / 10;
			_areaOfEffect = true;
		}
		else
		{
			_power = 120;
			_damageType = _parent.Mod.GetDamageType(DT_HE);
			_areaOfEffect = true;
		}

		bool range = !(_hit || (_attack.weapon_item != null && _attack.weapon_item.Rules.BattleType == BT_PSIAMP));

		if (_areaOfEffect)
		{
			if (_power > 0)
			{
				_parent.Save.TileEngine.Explode(_attack, _center, _power, _damageType, _radius, range);

				int powerForAnimation = _power;
				if (itemRule != null && itemRule.PowerForAnimation > 0)
				{
					powerForAnimation = itemRule.PowerForAnimation;
				}

				int frame = Mod.EXPLOSION_OFFSET;
				int frameCount = -1;
				int sound = powerForAnimation <= 80 ? Mod.SMALL_EXPLOSION : Mod.LARGE_EXPLOSION;

				if (itemRule != null)
				{
					frame = itemRule.HitAnimation;
					frameCount = itemRule.HitAnimationFrames;
					OptValue(ref sound, itemRule.ExplosionHitSound);
				}
				if (_parent.Depth > 0)
				{
					frame -= (frameCount > 0 ? frameCount : Explosion.EXPLODE_FRAMES);
				}
				int frameDelay = 0;
				int counter = Math.Max(1, (powerForAnimation / 5) / 5);
				_parent.Map.BlastFlash = true;
				int lowerLimit = Math.Max(1, powerForAnimation / 5);
				for (int i = 0; i < lowerLimit; i++)
				{
					int X = Rng.Generate(-powerForAnimation / 2, powerForAnimation / 2);
					int Y = Rng.Generate(-powerForAnimation / 2, powerForAnimation / 2);
					Position p = _center;
					p.x = (short)(p.x + X); p.y = (short)(p.y + Y);
					var explosion = new Explosion(p, frame, frameDelay, true, false, frameCount);
					// add the explosion on the map
					_parent.Map.Explosions.Add(explosion);
					if (i > 0 && i % counter == 0)
					{
						frameDelay++;
					}
				}
				int explosionSpeed = BattlescapeState.DEFAULT_ANIM_SPEED / 2;
				if (itemRule != null)
				{
					explosionSpeed -= (10 * itemRule.ExplosionSpeed);
				}
				if (_explosionCounter > 6)
				{
					explosionSpeed = 1; // maximum animation speed for long chain terrain explosions
				}
				_parent.SetStateInterval((uint)Math.Max(1, explosionSpeed));
				// explosion sound
				_parent.PlaySound(sound);
				if (_parent.Map.FollowProjectile || _explosionCounter > 0)
				{
					_parent.Map.Camera.CenterOnPosition(_center.ToTile(), false);
				}
			}
			else
			{
				_parent.PopState();
			}
		}
		else
		// create a bullet hit
		{
			_parent.Save.TileEngine.Hit(_attack, _center, _power, _damageType, range, _terrainMeleeTilePart);

			_parent.SetStateInterval((uint)Math.Max(1, ((BattlescapeState.DEFAULT_ANIM_SPEED / 2) - (10 * itemRule.ExplosionSpeed))));
			int anim = -1;
			int animFrames = -1;
			int sound = -1;

			RuleItem weaponRule = _attack.weapon_item.Rules;
			RuleItem damageRule = _attack.weapon_item != _attack.damage_item ? itemRule : null;

			if (_hit || _psi)
			{
				anim = weaponRule.MeleeAnimation;
				animFrames = weaponRule.MeleeAnimationFrames;
				if (_psi)
				{
					// psi attack sound is based weapon hit sound
					sound = weaponRule.HitSound;

					OptValue(ref anim, weaponRule.PsiAnimation);
					OptValue(ref animFrames, weaponRule.PsiAnimationFrames);
					OptValue(ref sound, weaponRule.PsiSound);
				}
				else
				{
					sound = weaponRule.MeleeSound;
					if (damageRule != null)
					{
						OptValue(ref anim, damageRule.MeleeAnimation);
						OptValue(ref animFrames, damageRule.MeleeAnimationFrames);
						OptValue(ref sound, damageRule.MeleeSound);
					}
				}
			}
			else
			{
				anim = itemRule.HitAnimation;
				animFrames = itemRule.HitAnimationFrames;
				sound = itemRule.HitSound;
			}

			if (miss)
			{
				if (_hit || _psi)
				{
					OptValue(ref anim, weaponRule.MeleeMissAnimation);
					OptValue(ref animFrames, weaponRule.MeleeMissAnimationFrames);
					if (_psi)
					{
						// psi attack sound is based weapon hit sound
						OptValue(ref sound, weaponRule.HitMissSound);

						OptValue(ref anim, weaponRule.PsiMissAnimation);
						OptValue(ref animFrames, weaponRule.PsiMissAnimationFrames);
						OptValue(ref sound, weaponRule.PsiMissSound);
					}
					else
					{
						OptValue(ref sound, weaponRule.MeleeMissSound);
						if (damageRule != null)
						{
							OptValue(ref anim, damageRule.MeleeMissAnimation);
							OptValue(ref animFrames, damageRule.MeleeMissAnimationFrames);
							OptValue(ref sound, damageRule.MeleeMissSound);
						}
					}
				}
				else
				{
					OptValue(ref anim, itemRule.HitMissAnimation);
					OptValue(ref animFrames, itemRule.HitMissAnimationFrames);
					OptValue(ref sound, itemRule.HitMissSound);
				}
			}

			if (anim != -1)
			{
				var explosion = new Explosion(_center, anim, 0, false, (_hit || _psi), animFrames); // Don't burn the tile
				_parent.Map.Explosions.Add(explosion);
			}
			if (_parent.Map.FollowProjectile)
			{
				_parent.Map.Camera.ViewLevel = _center.z / 24;
			}

			if (_targetPsiOrHit != null && _parent.Save.Side == FACTION_HOSTILE && _targetPsiOrHit.Faction == FACTION_PLAYER)
			{
				_parent.Map.Camera.CenterOnPosition(_center.ToTile(), false);
			}
			// bullet hit sound
			_parent.PlaySound(sound, _center.ToTile());
		}

		if (_attack.type == BA_SELF_DESTRUCT)
		{
			if (_attack.attacker != null)
			{
				_attack.attacker.SetAlreadyExploded(false);
			}
		}
	}

	/// <summary>
	/// Animates explosion sprites. If their animation is finished remove them from the list.
	/// If the list is empty, this state is finished and the actual calculations take place.
	/// </summary>
	public override void Think()
	{
		if (!_parent.Map.BlastFlash)
		{
			var explosions = _parent.Map.Explosions;
			if (explosions.Count == 0)
				Explode();

			for (int iter = 0; iter < explosions.Count;)
			{
				Explosion explosion = explosions[iter];
				if (!explosion.Animate())
				{
					explosions.RemoveAt(iter);
					if (explosions.Count == 0)
					{
						Explode();
						return;
					}
				}
				else
				{
					++iter;
				}
			}
		}
	}

	/// <summary>Explosions cannot be cancelled.</summary>
	public override void Cancel()
	{
	}

	/// <summary>Calculates the effects of the explosion.</summary>
	private void Explode()
	{
		bool terrainExplosion = false;
		SavedBattleGame save = _parent.Save;
		// last minute adjustment: determine if we actually
		if (_hit)
		{
			if (_attack.attacker != null && !_attack.attacker.IsOut)
			{
				_attack.attacker.Aim(false);
			}

			if (_power <= 0)
			{
				_parent.PopState();
				return;
			}

			int sound = _attack.weapon_item.Rules.MeleeHitSound;
			if (_attack.weapon_item != _attack.damage_item)
			{
				// melee weapon with ammo
				OptValue(ref sound, _attack.damage_item.Rules.MeleeHitSound);
			}
			_parent.PlaySound(sound, _center.ToTile());
		}

		if (_tile != null)
		{
			terrainExplosion = true;
		}
		if (_tile == null && _attack.damage_item == null)
		{
			terrainExplosion = true;
		}

		// now check for new casualties
		_parent.CheckForCasualties(_attack.damage_item != null ? _damageType : null, _attack, false, terrainExplosion);
		// revive units if damage could give hp or reduce stun
		_parent.Save.ReviveUnconsciousUnits(true);
		// if any unit get infected turn it to zombie
		_parent.ConvertInfected();

		// if this explosion was caused by a unit shooting, now it's the time to put the gun down
		if (_attack.attacker != null && !_attack.attacker.IsOut && _lowerWeapon)
		{
			_attack.attacker.Aim(false);
		}

		if (_attack.damage_item != null && (_attack.damage_item.Rules.BattleType == BT_GRENADE || _attack.damage_item.Rules.BattleType == BT_PROXIMITYGRENADE))
		{
			_parent.Save.RemoveItem(_attack.damage_item);
		}

		_parent.PopState();

		// check for terrain explosions
		Tile t = save.TileEngine.CheckForTerrainExplosions();
		if (t != null)
		{
			Position p = t.Position.ToVoxel();
			p += new Position(8, 8, 0);
			_parent.StatePushFront(new ExplosionBState(_parent, new LastPositions(p), new BattleActionAttack(BA_NONE, _attack.attacker), t, false, 0, _explosionCounter + 1));
		}

		// Spawn a unit if the item does that
		if (_attack.damage_item != null)
		{
			_parent.SpawnNewUnit(_attack, _before.ToTile());
		}

		// Spawn a item if the weapon does that
		if (_attack.damage_item != null)
		{
			_parent.SpawnNewItem(_attack, _before.ToTile());
		}
	}
}
