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
// Ported from: src/Mod/RuleCraftWeapon.h, src/Mod/RuleCraftWeapon.cpp
#nullable disable

namespace Acom;

public enum CraftWeaponCategory : uint
{
	CWC_WEAPON = 0,
	CWC_TRACTOR_BEAM = 1,
	CWC_EQUIPMENT = 2,
}

/// <summary>
/// Represents a specific type of craft weapon.
/// Contains constant info about a craft weapon like
/// damage, range, accuracy, items used, etc.
/// </summary>
/// <seealso cref="CraftWeapon"/>
public class RuleCraftWeapon
{
	private string _ufopediaType = "";
	private readonly string _type;
	private string _tooltip = "";
	private int _sprite, _sound, _damage, _shieldDamageModifier, _range, _accuracy, _reloadCautious, _reloadStandard, _reloadAggressive, _ammoMax, _rearmRate, _projectileSpeed, _weaponType;
	private CraftWeaponProjectileType _projectileType;
	private string _launcherName = "", _clipName = "";
	private RuleItem _launcher;
	private RuleItem _clip;
	private readonly RuleCraftStats _stats;
	private bool _underwaterOnly;
	private int _tractorBeamPower;
	private bool _hidePediaInfo;
	private bool _statisticalBulletSaving;
	private bool _unifiedDamageFormula;

	/// <summary>Creates a blank ruleset for a certain type of craft weapon.</summary>
	/// <param name="type">String defining the type.</param>
	public RuleCraftWeapon(string type)
	{
		_type = type;
		_sprite = -1;
		_sound = -1;
		_damage = 0;
		_shieldDamageModifier = 100;
		_range = 0;
		_accuracy = 0;
		_reloadCautious = 0;
		_reloadStandard = 0;
		_reloadAggressive = 0;
		_ammoMax = 0;
		_rearmRate = 1;
		_projectileSpeed = 0;
		_weaponType = 0;
		_projectileType = CraftWeaponProjectileType.CWPT_CANNON_ROUND;
		_stats = new RuleCraftStats();
		_underwaterOnly = false;
		_tractorBeamPower = 0;
		_hidePediaInfo = false;
		_statisticalBulletSaving = false;
		_unifiedDamageFormula = false;
	}

	/// <summary>Loads the craft weapon from YAML.</summary>
	public void Load(YamlNodeReader node, Mod mod)
	{
		var reader = node.UseIndex();
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent, mod);
		}

		reader.TryRead("ufopediaType", ref _ufopediaType);
		reader.TryRead("tooltip", ref _tooltip);
		if (reader["stats"])
		{
			_stats.Load(reader["stats"]);
		}
		if (reader["sprite"])
		{
			// used in
			// Surface set (baseOffset):
			//   BASEBITS.PCK (48)
			//   INTICON.PCK (5)
			//
			// Final index in surfaceset is `baseOffset + sprite + (sprite > 5 ? modOffset : 0)`
			_sprite = mod.GetOffset(reader["sprite"].ReadVal(_sprite), 5);
		}
		mod.LoadSoundOffset(_type, ref _sound, reader["sound"], "GEO.CAT");
		reader.TryRead("damage", ref _damage);
		reader.TryRead("unifiedDamageFormula", ref _unifiedDamageFormula);
		reader.TryRead("shieldDamageModifier", ref _shieldDamageModifier);
		reader.TryRead("range", ref _range);
		reader.TryRead("accuracy", ref _accuracy);
		reader.TryRead("reloadCautious", ref _reloadCautious);
		reader.TryRead("reloadStandard", ref _reloadStandard);
		reader.TryRead("reloadAggressive", ref _reloadAggressive);
		reader.TryRead("ammoMax", ref _ammoMax);
		reader.TryRead("rearmRate", ref _rearmRate);
		reader.TryRead("projectileType", ref _projectileType);
		reader.TryRead("projectileSpeed", ref _projectileSpeed);
		reader.TryRead("launcher", ref _launcherName);
		reader.TryRead("clip", ref _clipName);
		reader.TryRead("weaponType", ref _weaponType);
		reader.TryRead("underwaterOnly", ref _underwaterOnly);
		reader.TryRead("tractorBeamPower", ref _tractorBeamPower);
		reader.TryRead("hidePediaInfo", ref _hidePediaInfo);
		reader.TryRead("bulletSaving", ref _statisticalBulletSaving);
	}

	/// <summary>Cross link with other rules.</summary>
	public void AfterLoad(Mod mod)
	{
		mod.LinkRule(ref _launcher, ref _launcherName);
		mod.LinkRule(ref _clip, ref _clipName);

		if (_unifiedDamageFormula && _launcher == null && _clip == null)
		{
			throw new InvalidOperationException("Unified damage formula requires `clip` or `launcher` to be defined.");
		}

		if (_projectileType < CraftWeaponProjectileType.CWPT_LASER_BEAM && _damage > 0)
		{
			if (_projectileSpeed <= 0)
			{
				throw new InvalidOperationException("Missile-like craft weapons (with 'damage' > 0) must have a positive 'projectileSpeed'.");
			}
			else if (_projectileSpeed <= 4 && _range > 10)
			{
				Logger.Warning($"Missile speed for {_type} is very low! Depending on craft approach speed, the missile may seem not moving, or even moving backwards. Speed: {_projectileSpeed}; range: {_range}");
			}
			else if (_projectileSpeed <= 5 && _range > 20)
			{
				Logger.Info($"Missile speed for {_type} is quite low. Depending on craft approach speed, the missile may seem moving very slowly. Speed: {_projectileSpeed}; range: {_range}");
			}
		}

		if (_launcher == null)
		{
			throw new InvalidOperationException("Launcher item is required for a craft weapon");
		}
		if (_ammoMax != 0)
		{
			if (_rearmRate <= 0)
			{
				throw new InvalidOperationException("Attribute 'rearmRate' must be positive when 'ammoMax' is set");
			}
			if (_clip != null && _clip.ClipSize > _rearmRate)
			{
				throw new InvalidOperationException("Attribute 'clipSize' of the clip item is too big for the given 'rearmRate'");
			}
		}
	}

	/// <summary>Gets the custom name of the Ufopedia article related to this craft weapon.</summary>
	public string UfopediaType => !string.IsNullOrEmpty(_ufopediaType) ? _ufopediaType : _type;

	/// <summary>Gets the language string that names this craft weapon. Each craft weapon type has a unique name.</summary>
	public string Type => _type;

	/// <summary>Gets the craft weapon's tooltip.</summary>
	public string Tooltip => _tooltip;

	/// <summary>Gets the ID of the sprite used to draw the craft weapon in the Equip Craft screen.</summary>
	public int Sprite => _sprite;

	/// <summary>Gets the ID of the sound used when firing the weapon in the Dogfight screen.</summary>
	public int Sound => _sound;

	/// <summary>Gets the amount of damage this craft weapon inflicts on UFOs.</summary>
	public int Damage => _damage;

	/// <summary>Should the weapon's stats be displayed in Ufopedia or not?</summary>
	public bool HidePediaInfo => _hidePediaInfo;

	/// <summary>Should the statistical bullet saving be used or not?</summary>
	public bool UseStatisticalBulletSaving() => _statisticalBulletSaving;

	/// <summary>Should unified or vanilla formula be used?</summary>
	public bool UnifiedDamageFormula() => _unifiedDamageFormula;

	/// <summary>Gets the craft weapon's effectiveness against shields.</summary>
	public int ShieldDamageModifier => _shieldDamageModifier;

	/// <summary>Gets the maximum range of this craft weapon.</summary>
	public int Range => _range;

	/// <summary>Gets the percentage chance of each shot of this craft weapon hitting an UFO.</summary>
	public int Accuracy => _accuracy;

	/// <summary>Gets the amount of time the craft weapon takes to reload in cautious mode.</summary>
	public int CautiousReload => _reloadCautious;

	/// <summary>Gets the amount of time the craft weapon takes to reload in standard mode.</summary>
	public int StandardReload => _reloadStandard;

	/// <summary>Gets the amount of time the craft weapon takes to reload in aggressive mode.</summary>
	public int AggressiveReload => _reloadAggressive;

	/// <summary>Gets the maximum amount of ammo the craft weapon can carry.</summary>
	public int AmmoMax => _ammoMax;

	/// <summary>Gets how much ammo is added to the craft weapon while rearming.</summary>
	public int RearmRate => _rearmRate;

	/// <summary>Gets the item that represents the launcher of this craft weapon.</summary>
	public RuleItem LauncherItem => _launcher;

	/// <summary>Gets the item that represents the ammo clip of this craft weapon (null if none).</summary>
	public RuleItem ClipItem => _clip;

	/// <summary>Gets the craft weapon's projectile's type.</summary>
	public CraftWeaponProjectileType ProjectileType => _projectileType;

	/// <summary>Gets the craft weapon's projectile speed.</summary>
	public int ProjectileSpeed => _projectileSpeed;

	/// <summary>Gets weapon type used by craft slots.</summary>
	public int WeaponType => _weaponType;

	/// <summary>Gets bonus stats given by this weapon.</summary>
	public RuleCraftStats BonusStats => _stats;

	/// <summary>Is this item restricted to use underwater?</summary>
	public bool IsWaterOnly => _underwaterOnly;

	/// <summary>Gets the craft weapon's tractor beam power.</summary>
	public int TractorBeamPower => _tractorBeamPower;
}
