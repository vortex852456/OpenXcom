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
// Ported from: src/Savegame/CraftWeaponProjectile.h, src/Savegame/CraftWeaponProjectile.cpp
#nullable disable

namespace Acom;

// Do not change the order of these enums because they are related to blob order.
public enum CraftWeaponProjectileType : uint
{
	CWPT_STINGRAY_MISSILE = 0,
	CWPT_AVALANCHE_MISSILE = 1,
	CWPT_CANNON_ROUND = 2,
	CWPT_FUSION_BALL = 3,
	CWPT_LASER_BEAM = 4,
	CWPT_PLASMA_BEAM = 5,
}

public enum CraftWeaponProjectileGlobalType : uint
{
	CWPGT_MISSILE = 0,
	CWPGT_BEAM = 1,
}

public enum Directions : uint
{
	D_NONE = 0,
	D_UP = 1,
	D_DOWN = 2,
}

/// <summary>A projectile fired by a craft weapon during a dogfight.</summary>
public class CraftWeaponProjectile
{
	/// <summary>Horizontal positions (C++ namespace constants HP_LEFT/HP_CENTER/HP_RIGHT).</summary>
	public const int HP_LEFT = -1;
	public const int HP_CENTER = 0;
	public const int HP_RIGHT = 1;

	private readonly RuleItem _damageItem;
	private CraftWeaponProjectileType _type;
	private CraftWeaponProjectileGlobalType _globalType;
	private int _speed;
	private int _direction;
	private int _currentPosition; // relative to interceptor, apparently, which is a problem when the interceptor disengages while projectile is in flight
	private int _horizontalPosition;
	private int _state;
	private int _accuracy;
	private int _damage;
	private int _range;
	private bool _toBeRemoved;
	private bool _missed;
	private int _distanceCovered;
	private int _shieldDamageModifier;

	public CraftWeaponProjectile(RuleItem damageItem)
	{
		_damageItem = damageItem;
		_type = CraftWeaponProjectileType.CWPT_CANNON_ROUND;
		_globalType = CraftWeaponProjectileGlobalType.CWPGT_MISSILE;
		_speed = 0;
		_direction = (int)Directions.D_NONE;
		_currentPosition = 0;
		_horizontalPosition = 0;
		_state = 0;
		_accuracy = 0;
		_damage = 0;
		_range = 0;
		_toBeRemoved = false;
		_missed = false;
		_distanceCovered = 0;
		_shieldDamageModifier = 100;
	}

	/// <summary>
	/// The type of projectile according to the type of weapon it was shot from.
	/// This is used for drawing the projectiles; beam types also switch the global type.
	/// </summary>
	public CraftWeaponProjectileType Type
	{
		get => _type;
		set
		{
			_type = value;
			if (value >= CraftWeaponProjectileType.CWPT_LASER_BEAM)
			{
				_globalType = CraftWeaponProjectileGlobalType.CWPGT_BEAM;
				_state = 8;
			}
		}
	}

	/// <summary>Returns the global type of projectile (missile or beam).</summary>
	public CraftWeaponProjectileGlobalType GlobalType => _globalType;

	/// <summary>The direction of the projectile (<see cref="Directions"/>); setting D_UP resets the position.</summary>
	public int Direction
	{
		get => _direction;
		set
		{
			_direction = value;
			if (_direction == (int)Directions.D_UP)
			{
				_currentPosition = 0;
			}
		}
	}

	/// <summary>Moves the projectile according to it's speed or changes the phase of beam animation.</summary>
	public void Move()
	{
		if (_globalType == CraftWeaponProjectileGlobalType.CWPGT_MISSILE)
		{
			int positionChange = _speed;

			// Check if projectile would reach its maximum range this tick.
			if ((_distanceCovered / 8) < Range && ((_distanceCovered + _speed) / 8) >= Range)
				positionChange = Range * 8 - _distanceCovered;

			// Check if projectile passed its maximum range on previous tick.
			if ((_distanceCovered / 8) >= Range)
				Missed = true;

			if (_direction == (int)Directions.D_UP)
			{
				_currentPosition += positionChange;
			}
			else if (_direction == (int)Directions.D_DOWN)
			{
				_currentPosition -= positionChange;
			}

			_distanceCovered += positionChange;
		}
		else if (_globalType == CraftWeaponProjectileGlobalType.CWPGT_BEAM)
		{
			_state /= 2;
			if (_state == 1)
			{
				_toBeRemoved = true;
			}
		}
	}

	/// <summary>The y position of the projectile on the radar.</summary>
	public int Position
	{
		get => _currentPosition;
		set => _currentPosition = value;
	}

	/// <summary>The x position of the projectile on the radar (set once per projectile during firing).</summary>
	public int HorizontalPosition
	{
		get => _horizontalPosition;
		set => _horizontalPosition = value;
	}

	/// <summary>Marks the projectile to be removed.</summary>
	public void Remove()
	{
		_toBeRemoved = true;
	}

	/// <summary>Returns if a projectile should be removed.</summary>
	public bool ToBeRemoved() => _toBeRemoved;

	/// <summary>Returns animation state of a beam.</summary>
	public int State => _state;

	/// <summary>The amount of damage the projectile can do when hitting it's target.</summary>
	public int Damage
	{
		get => _damage;
		set => _damage = value;
	}

	/// <summary>The accuracy of the projectile.</summary>
	public int Accuracy
	{
		get => _accuracy;
		set => _accuracy = value;
	}

	/// <summary>True if the projectile missed it's target.</summary>
	public bool Missed
	{
		get => _missed;
		set => _missed = value;
	}

	/// <summary>Maximum range of projectile.</summary>
	public int Range
	{
		get => _range;
		set => _range = value;
	}

	/// <summary>Sets the speed of the projectile.</summary>
	public void SetSpeed(int speed)
	{
		_speed = speed;
	}

	/// <summary>How effective this projectile is against shields.</summary>
	public int ShieldDamageModifier
	{
		get => _shieldDamageModifier;
		set => _shieldDamageModifier = value;
	}

	/// <summary>Gets the damage item.</summary>
	public RuleItem DamageItem => _damageItem;
}
