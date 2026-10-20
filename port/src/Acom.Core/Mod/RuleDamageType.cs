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
// Ported from: src/Mod/RuleDamageType.h, src/Mod/RuleDamageType.cpp
#nullable disable

namespace Acom;

public enum ItemDamageType : uint
{
	DT_NONE = 0,
	DT_AP = 1,
	DT_IN = 2,
	DT_HE = 3,
	DT_LASER = 4,
	DT_PLASMA = 5,
	DT_STUN = 6,
	DT_MELEE = 7,
	DT_ACID = 8,
	DT_SMOKE = 9,
	DT_10 = 10,
	DT_11 = 11,
	DT_12 = 12,
	DT_13 = 13,
	DT_14 = 14,
	DT_15 = 15,
	DT_16 = 16,
	DT_17 = 17,
	DT_18 = 18,
	DT_19 = 19,
	DAMAGE_TYPES = 20,
}

public enum ItemDamageRandomType : uint
{
	DRT_DEFAULT = 0,
	DRT_UFO = 1,
	DRT_TFTD = 2,
	DRT_FLAT = 3,
	DRT_FIRE = 4,
	DRT_NONE = 5,
	DRT_UFO_WITH_TWO_DICE = 6,
	DRT_EASY = 7,
	DRT_STANDARD = 8,
	DRT_EXPLOSION = 9,
}

/// <summary>
/// Represents a specific damage type.
/// </summary>
/// <remarks>
/// A C++ struct that is copied by value (e.g. <c>RuleItem::_damageType = *mod->getDamageType(...)</c>); use
/// <see cref="Clone"/> for such copies.
/// </remarks>
public class RuleDamageType
{
	/// <summary>Set fixed arena of effect radius.</summary>
	public int FixRadius;
	/// <summary>Set what random range use when calculating damage.</summary>
	public ItemDamageRandomType RandomType;
	/// <summary>Set what resistance on armor is used.</summary>
	public ItemDamageType ResistType;
	/// <summary>Use fire calculation of radius and power.</summary>
	public bool FireBlastCalc;
	/// <summary>Damage type ignore direction of attack.</summary>
	public bool IgnoreDirection;
	/// <summary>Damage type do not cause self destruct explosion.</summary>
	public bool IgnoreSelfDestruct;
	/// <summary>Damage type can do stun damage to big units.</summary>
	public bool IgnorePainImmunity;
	/// <summary>Damage type doesn't cause morale loss from health damage.</summary>
	public bool IgnoreNormalMoraleLose;
	/// <summary>Damage type do not move health to negative values.</summary>
	public bool IgnoreOverKill;
	/// <summary>How much of armor is ignored.</summary>
	public float ArmorEffectiveness;
	/// <summary>Conversion from power to max explosion radius.</summary>
	public float RadiusEffectiveness;
	/// <summary>Reduction of explosion power per tile.</summary>
	public float RadiusReduction;
	/// <summary>Threshold of fire creation.</summary>
	public int FireThreshold;
	/// <summary>Threshold of smoke creation.</summary>
	public int SmokeThreshold;
	/// <summary>Conversion from power to unit damage.</summary>
	public float ToHealth;
	/// <summary>Conversion from power to mana loss.</summary>
	public float ToMana;
	/// <summary>Conversion from power to unit armor damage.</summary>
	public float ToArmor;
	/// <summary>Conversion from power to unit armor damage before applying armor protection.</summary>
	public float ToArmorPre;
	/// <summary>Conversion from power to wound chance.</summary>
	public float ToWound;
	/// <summary>Conversion from power to item damage.</summary>
	public float ToItem;
	/// <summary>Conversion from power to tile damage.</summary>
	public float ToTile;
	/// <summary>Conversion from power to stun level.</summary>
	public float ToStun;
	/// <summary>Conversion from power to energy lose.</summary>
	public float ToEnergy;
	/// <summary>Conversion from power to time units lose.</summary>
	public float ToTime;
	/// <summary>Conversion from power to morale lose.</summary>
	public float ToMorale;
	/// <summary>Damage type use random conversion to health damage.</summary>
	public bool RandomHealth;
	/// <summary>Damage type use random conversion mana loss.</summary>
	public bool RandomMana;
	/// <summary>Damage type use random conversion armor damage.</summary>
	public bool RandomArmor;
	/// <summary>Damage type use random conversion armor pre damage.</summary>
	public bool RandomArmorPre;
	/// <summary>Damage type use random chance for wound number or linear.</summary>
	public bool RandomWound;
	/// <summary>Damage type use random conversion item damage.</summary>
	public bool RandomItem;
	/// <summary>Damage type use random conversion tile damage.</summary>
	public bool RandomTile;
	/// <summary>Damage type use random conversion stun level.</summary>
	public bool RandomStun;
	/// <summary>Damage type use random conversion energy lose.</summary>
	public bool RandomEnergy;
	/// <summary>Damage type use random conversion time units lose.</summary>
	public bool RandomTime;
	/// <summary>Damage type use random conversion morale lose.</summary>
	public bool RandomMorale;
	/// <summary>
	/// Which tile damage method should be used?
	/// A. For AOE (area-of-effect) weapons:
	///   TileDamageMethod==1: (50%-150% * power * ToTile)
	///   TileDamageMethod==2: ((power x RandomTile) * ToTile), which defaults to vanilla 50% damage for HE and 0% for stun, smoke and incendiary
	/// B. For non-AOE (projectile) weapons:
	///   TileDamageMethod==1: (50%-150% * power * ToTile), which defaults to vanilla 25%-75% damage for AP/laser/plasma/etc.
	///   TileDamageMethod==2: ((damage x RandomTile) * ToTile)
	/// </summary>
	public int TileDamageMethod;
	/// <summary>Maximum final tile damage possible, regardless of Power, ToTile, RandomTile, TileDamageMethod, etc.</summary>
	public int TileDamageLimit;

	/// <summary>Default constructor.</summary>
	public RuleDamageType()
	{
		FixRadius = 0;
		RandomType = ItemDamageRandomType.DRT_STANDARD;
		ResistType = ItemDamageType.DT_NONE;
		FireBlastCalc = false;
		IgnoreDirection = false;
		IgnoreSelfDestruct = false;
		IgnorePainImmunity = false;
		IgnoreNormalMoraleLose = false;
		IgnoreOverKill = false;
		ArmorEffectiveness = 1.0f;
		RadiusEffectiveness = 0.0f;
		RadiusReduction = 10.0f;
		FireThreshold = 1000;
		SmokeThreshold = 1000;
		ToHealth = 1.0f;
		ToMana = 0.0f;
		ToArmor = 0.1f;
		ToArmorPre = 0.0f;
		ToWound = 1.0f;
		ToItem = 0.0f;
		ToTile = 0.5f;
		ToStun = 0.25f;
		ToEnergy = 0.0f;
		ToTime = 0.0f;
		ToMorale = 0.0f;
		RandomHealth = false;
		RandomMana = false;
		RandomArmor = false;
		RandomArmorPre = false;
		RandomWound = true;
		RandomItem = false;
		RandomTile = false;
		RandomStun = true;
		RandomEnergy = false;
		RandomTime = false;
		RandomMorale = false;
		TileDamageMethod = 1;
		TileDamageLimit = -1;
	}

	/// <summary>Copy of this damage type (C++ copies the struct by value).</summary>
	public RuleDamageType Clone() => (RuleDamageType)MemberwiseClone();

	/// <summary>Function converting power to damage.</summary>
	/// <param name="power">Input power.</param>
	/// <returns>Random damage based on power.</returns>
	public int GetRandomDamage(int power) => GetRandomDamage(power, static (min, max) => Rng.Generate(min, max));

	/// <summary>Function converting power to damage.</summary>
	/// <param name="power">Input power.</param>
	/// <param name="mode">Calc mode (0 = dmg, 1 = min dmg, 2 = max dmg).</param>
	/// <returns>Random damage based on power.</returns>
	public int GetRandomDamage(int power, int mode)
	{
		if (mode == 0)
		{
			return GetRandomDamage(power);
		}
		else if (mode == 1)
		{
			return GetRandomDamage(power, static (min, max) => min);
		}
		else
		{
			return GetRandomDamage(power, static (min, max) => max);
		}
	}

	/// <summary>Function converting power to damage.</summary>
	/// <param name="power">Input power.</param>
	/// <param name="randFunc">Function to get random value.</param>
	/// <returns>Random damage based on power.</returns>
	public int GetRandomDamage(int power, Func<int, int, int> randFunc)
	{
		ArgumentNullException.ThrowIfNull(randFunc);
		ItemDamageRandomType randType = RandomType;
		if (randType == ItemDamageRandomType.DRT_UFO_WITH_TWO_DICE)
		{
			int firstThrow = randFunc(0, power);
			int secondThrow = randFunc(0, power);

			return firstThrow + secondThrow;
		}
		else if (randType == ItemDamageRandomType.DRT_EASY)
		{
			int min = power / 2; // 50%
			int max = power * 2; // 200%

			return randFunc(min, max);
		}

		bool def = randType == ItemDamageRandomType.DRT_DEFAULT;
		if (def)
		{
			switch (ResistType)
			{
			case ItemDamageType.DT_NONE: randType = ItemDamageRandomType.DRT_NONE; break;
			case ItemDamageType.DT_IN: randType = ItemDamageRandomType.DRT_FIRE; break;
			case ItemDamageType.DT_HE: randType = ItemDamageRandomType.DRT_EXPLOSION; break;
			case ItemDamageType.DT_SMOKE: randType = ItemDamageRandomType.DRT_NONE; break;
			default: randType = ItemDamageRandomType.DRT_STANDARD; break;
			}
		}

		int dmgRng = 0;
		switch (randType)
		{
		case ItemDamageRandomType.DRT_UFO: dmgRng = 100; break;
		case ItemDamageRandomType.DRT_TFTD: dmgRng = 50; break;
		case ItemDamageRandomType.DRT_STANDARD: dmgRng = Mod.DAMAGE_RANGE; break;
		case ItemDamageRandomType.DRT_EXPLOSION: dmgRng = Mod.EXPLOSIVE_DAMAGE_RANGE; break;
		case ItemDamageRandomType.DRT_FLAT: dmgRng = 0; break;
		case ItemDamageRandomType.DRT_FIRE:
			return randFunc(Mod.FIRE_DAMAGE_RANGE[0], Mod.FIRE_DAMAGE_RANGE[1]);

		case ItemDamageRandomType.DRT_NONE: return 0;
		default: return 0;
		}

		{
			int min = power * (100 - dmgRng) / 100;
			int max = power * (100 + dmgRng) / 100;

			return randFunc(min, max);
		}
	}

	/// <summary>Calculate random value of damage for tile attack.</summary>
	/// <param name="power">Raw power of attack.</param>
	/// <param name="damage">Random damage done to units.</param>
	/// <returns>Random damage for tile.</returns>
	public int GetRandomDamageForTile(int power, int damage) => TileDamageMethod == 1 ? Rng.Generate(power / 2, 3 * power / 2) : damage;

	/// <summary>Is this damage single target (can only hit one target)?</summary>
	public bool IsDirect { get => FixRadius == 0; }

	/// <summary>Load rule from YAML.</summary>
	public void Load(YamlNodeReader node)
	{
		var reader = node.UseIndex();
		reader.TryRead("FixRadius", ref FixRadius);
		reader.TryRead("RandomType", ref RandomType);
		reader.TryRead("ResistType", ref ResistType);
		reader.TryRead("FireBlastCalc", ref FireBlastCalc);
		reader.TryRead("IgnoreDirection", ref IgnoreDirection);
		reader.TryRead("IgnoreSelfDestruct", ref IgnoreSelfDestruct);
		reader.TryRead("IgnorePainImmunity", ref IgnorePainImmunity);
		reader.TryRead("IgnoreNormalMoraleLose", ref IgnoreNormalMoraleLose);
		reader.TryRead("IgnoreOverKill", ref IgnoreOverKill);
		TryReadFloat(reader, "ArmorEffectiveness", ref ArmorEffectiveness);
		TryReadFloat(reader, "RadiusEffectiveness", ref RadiusEffectiveness);
		TryReadFloat(reader, "RadiusReduction", ref RadiusReduction);

		reader.TryRead("FireThreshold", ref FireThreshold);
		reader.TryRead("SmokeThreshold", ref SmokeThreshold);

		TryReadFloat(reader, "ToHealth", ref ToHealth);
		TryReadFloat(reader, "ToMana", ref ToMana);
		TryReadFloat(reader, "ToArmor", ref ToArmor);
		TryReadFloat(reader, "ToArmorPre", ref ToArmorPre);
		TryReadFloat(reader, "ToWound", ref ToWound);
		TryReadFloat(reader, "ToItem", ref ToItem);
		TryReadFloat(reader, "ToTile", ref ToTile);
		TryReadFloat(reader, "ToStun", ref ToStun);
		TryReadFloat(reader, "ToEnergy", ref ToEnergy);
		TryReadFloat(reader, "ToTime", ref ToTime);
		TryReadFloat(reader, "ToMorale", ref ToMorale);

		reader.TryRead("RandomHealth", ref RandomHealth);
		reader.TryRead("RandomMana", ref RandomMana);
		reader.TryRead("RandomArmor", ref RandomArmor);
		reader.TryRead("RandomArmorPre", ref RandomArmorPre);
		reader.TryRead("RandomWound", ref RandomWound);
		reader.TryRead("RandomItem", ref RandomItem);
		reader.TryRead("RandomTile", ref RandomTile);
		reader.TryRead("RandomStun", ref RandomStun);
		reader.TryRead("RandomEnergy", ref RandomEnergy);
		reader.TryRead("RandomTime", ref RandomTime);
		reader.TryRead("RandomMorale", ref RandomMorale);

		reader.TryRead("TileDamageMethod", ref TileDamageMethod);
		reader.TryRead("TileDamageLimit", ref TileDamageLimit);
	}

	/// <summary>
	/// <c>reader.tryRead(key, floatValue)</c>. YamlConvert parses floats as double (like c4core) but boxes the
	/// result as double for float targets, so the value is read as double and narrowed here.
	/// </summary>
	private static void TryReadFloat(YamlNodeReader reader, string key, ref float value)
	{
		double d = value;
		if (reader.TryRead(key, ref d))
		{
			value = (float)d;
		}
	}

	/// <summary>Helper function for calculating damage from damage.</summary>
	private static int GetDamageHelper(bool random, float multipler, int damage)
	{
		if (damage > 0)
		{
			if (random)
			{
				return (int)MathF.Round(Rng.Generate(0, damage) * multipler, MidpointRounding.AwayFromZero); // std::round(float)
			}
			else
			{
				return (int)MathF.Round(damage * multipler, MidpointRounding.AwayFromZero);
			}
		}
		return 0;
	}

	/// <summary>Get final damage value to health based on damage.</summary>
	public int GetHealthFinalDamage(int damage) => GetDamageHelper(RandomHealth, ToHealth, damage);

	/// <summary>Get final damage value to mana based on damage.</summary>
	public int GetManaFinalDamage(int damage) => GetDamageHelper(RandomMana, ToMana, damage);

	/// <summary>Get final damage value to armor based on damage.</summary>
	public int GetArmorFinalDamage(int damage) => GetDamageHelper(RandomArmor, ToArmor, damage);

	/// <summary>Get final damage value to armor based on damage before armor reduction.</summary>
	public int GetArmorPreFinalDamage(int damage) => GetDamageHelper(RandomArmorPre, ToArmorPre, damage);

	/// <summary>Get numbers of wound based on damage.</summary>
	public int GetWoundFinalDamage(int damage)
	{
		if (damage > 0)
		{
			if (RandomWound)
			{
				if (Rng.Generate(0, 10) < (int)(damage * ToWound))
				{
					return Rng.Generate(1, 3);
				}
			}
			else
			{
				return (int)MathF.Round(damage * ToWound, MidpointRounding.AwayFromZero);
			}
		}
		return 0;
	}

	/// <summary>Get final damage value to item based on damage.</summary>
	public int GetItemFinalDamage(int damage) => GetDamageHelper(RandomItem, ToItem, damage);

	/// <summary>Get final damage value to tile based on damage.</summary>
	public int GetTileFinalDamage(int damage)
	{
		int tmp = GetDamageHelper(RandomTile, ToTile, damage);
		if (TileDamageLimit >= 0 && tmp > TileDamageLimit)
		{
			return TileDamageLimit;
		}
		return tmp;
	}

	/// <summary>Get stun level change based on damage.</summary>
	public int GetStunFinalDamage(int damage) => GetDamageHelper(RandomStun, ToStun, damage);

	/// <summary>Get energy change based on damage.</summary>
	public int GetEnergyFinalDamage(int damage) => GetDamageHelper(RandomEnergy, ToEnergy, damage);

	/// <summary>Get time units change based on damage.</summary>
	public int GetTimeFinalDamage(int damage) => GetDamageHelper(RandomTime, ToTime, damage);

	/// <summary>Get morale change based on damage.</summary>
	public int GetMoraleFinalDamage(int damage) => GetDamageHelper(RandomMorale, ToMorale, damage);
}
