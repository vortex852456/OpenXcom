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
// Ported from: src/Savegame/CraftWeapon.h, src/Savegame/CraftWeapon.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents a craft weapon equipped by a craft.
/// Contains variable info about a craft weapon like ammo.
/// </summary>
public class CraftWeapon
{
	private readonly RuleCraftWeapon _rules;
	private int _ammo;
	private bool _rearming;
	private bool _disabled;

	/// <summary>Initializes a craft weapon of the specified type.</summary>
	/// <param name="rules">Pointer to ruleset.</param>
	/// <param name="ammo">Initial ammo.</param>
	public CraftWeapon(RuleCraftWeapon rules, int ammo)
	{
		_rules = rules;
		_ammo = ammo;
		_rearming = false;
		_disabled = false;
	}

	/// <summary>Loads the craft weapon from YAML.</summary>
	public void Load(YamlNodeReader reader)
	{
		reader.TryRead("ammo", ref _ammo);
		reader.TryRead("rearming", ref _rearming);
		reader.TryRead("disabled", ref _disabled);
	}

	/// <summary>Saves the craft weapon to YAML.</summary>
	public void Save(YamlNodeWriter writer)
	{
		writer.SetAsMap();

		writer.Write("type", _rules.Type);
		writer.Write("ammo", _ammo);
		if (_rearming)
			writer.Write("rearming", _rearming);
		if (_disabled)
			writer.Write("disabled", _disabled);
	}

	/// <summary>Returns the ruleset for the craft weapon's type.</summary>
	public RuleCraftWeapon Rules => _rules;

	/// <summary>Returns the ammo contained in this craft weapon.</summary>
	public int Ammo => _ammo;

	/// <summary>Changes the ammo contained in this craft weapon.</summary>
	/// <returns>If the weapon ran out of ammo (false = ran out).</returns>
	public bool SetAmmo(int ammo)
	{
		_ammo = ammo;
		if (_ammo < 0)
		{
			_ammo = 0;
			return false;
		}
		if (_ammo > _rules.AmmoMax)
		{
			_ammo = _rules.AmmoMax;
		}
		return true;
	}

	/// <summary>Returns whether this craft weapon needs rearming.</summary>
	public bool IsRearming
	{
		get
		{
			if (_disabled)
				return false;

			return _rearming;
		}
	}

	/// <summary>Changes whether this craft weapon needs rearming (for example, in case there's no more ammo).</summary>
	public void SetRearming(bool rearming)
	{
		_rearming = rearming;
	}

	/// <summary>Returns whether this craft weapon is disabled.</summary>
	public bool IsDisabled => _disabled;

	/// <summary>Sets whether this craft weapon is disabled or not.</summary>
	public void SetDisabled(bool disabled)
	{
		_disabled = disabled;
	}

	/// <summary>Rearms this craft weapon's ammo.</summary>
	/// <param name="available">Number of clips available.</param>
	/// <param name="clipSize">Number of rounds in said clips.</param>
	/// <returns>Number of clips used.</returns>
	public int Rearm(int available, int clipSize)
	{
		int ammoUsed = _rules.RearmRate;
		int clipsSaved = 0;

		if (clipSize > 0)
		{   // +(clipSize - 1) correction for rounding up
			int needed = Math.Min(_rules.RearmRate, _rules.AmmoMax - _ammo + clipSize - 1) / clipSize;
			ammoUsed = ((needed > available) ? available : needed) * clipSize;

			// statistical bullet saving
			if (clipSize > 1 && _rules.UseStatisticalBulletSaving())
			{
				int overusedAmmo = _ammo + ammoUsed - _rules.AmmoMax;
				if (overusedAmmo > 0)
				{
					if (Rng.Generate(0, clipSize - 1) < overusedAmmo)
					{
						clipsSaved = 1;
					}
				}
			}
		}

		SetAmmo(_ammo + ammoUsed);

		_rearming = _ammo < _rules.AmmoMax;

		return (clipSize <= 0) ? 0 : (ammoUsed / clipSize) - clipsSaved;
	}

	/// <summary>Fires a projectile from crafts weapon. Used during dogfights.</summary>
	/// <returns>The new projectile.</returns>
	public CraftWeaponProjectile Fire()
	{
		RuleItem damageItem = null;
		if (Rules.UnifiedDamageFormula())
		{
			damageItem = Rules.ClipItem ?? Rules.LauncherItem;
		}
		var p = new CraftWeaponProjectile(damageItem);
		p.Type = Rules.ProjectileType;
		p.SetSpeed(Rules.ProjectileSpeed);
		p.Accuracy = Rules.Accuracy;
		p.Damage = Rules.Damage;
		p.Range = Rules.Range;
		p.ShieldDamageModifier = Rules.ShieldDamageModifier;
		return p;
	}

	/// <summary>Gets how many clips are loaded into this weapon.</summary>
	public int ClipsLoaded
	{
		get
		{
			int retVal = (int)Math.Floor((double)_ammo / _rules.RearmRate);
			var clip = _rules.ClipItem;

			if (clip != null && clip.ClipSize > 0)
			{
				retVal = (int)Math.Floor((double)_ammo / clip.ClipSize);
			}

			return retVal;
		}
	}
}
