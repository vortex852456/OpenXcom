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
// Ported from: src/Basescape/SoldierSortUtil.h, src/Basescape/SoldierSortUtil.cpp
#nullable disable

namespace Acom;

/// <summary>Soldier stat getter used for sorting soldier lists (C++ <c>getStatFn_t</c>).</summary>
public delegate int GetStatFn(Game game, Soldier s);

/// <summary>Compares two soldiers by a stat (C++ <c>SortFunctor</c>, a "less than" predicate).</summary>
public class SortFunctor
{
	public Game _game;
	public GetStatFn _getStatFn;

	public SortFunctor(Game game, GetStatFn getStatFn)
	{
		_game = game;
		_getStatFn = getStatFn;
	}

	/// <summary>C++ operator(): true if <paramref name="a"/> sorts before <paramref name="b"/>.</summary>
	public bool Invoke(Soldier a, Soldier b)
	{
		bool ret = _getStatFn(_game, a) < _getStatFn(_game, b);
		return ret;
	}

	public GetStatFn Getter => _getStatFn;
}

#pragma warning disable IDE0060 // the getters share one signature; most ignore the game

/// <summary>Stat getters of SoldierSortUtil (free functions in C++).</summary>
public static class SoldierSortUtilFunctions
{
	/// <summary>Don't reveal psi strength before it would otherwise be known.</summary>
	private static bool PsiStrengthKnown(Game game, Soldier s)
		=> s.CurrentStats.psiSkill > 0
		|| (Options.psiStrengthEval
		&& game.SavedGame.IsResearched(game.Mod.PsiRequirements));

	public static int TuStat(Game game, Soldier s) => s.StatsWithAllBonuses.tu;
	public static int StaminaStat(Game game, Soldier s) => s.StatsWithAllBonuses.stamina;
	public static int HealthStat(Game game, Soldier s) => s.StatsWithAllBonuses.health;
	public static int BraveryStat(Game game, Soldier s) => s.StatsWithAllBonuses.bravery;
	public static int ReactionsStat(Game game, Soldier s) => s.StatsWithAllBonuses.reactions;
	public static int FiringStat(Game game, Soldier s) => s.StatsWithAllBonuses.firing;
	public static int ThrowingStat(Game game, Soldier s) => s.StatsWithAllBonuses.throwing;
	public static int StrengthStat(Game game, Soldier s) => s.StatsWithAllBonuses.strength;
	public static int ManaStat(Game game, Soldier s)
	{
		// don't reveal mana before it would otherwise be known
		if (game.SavedGame.IsManaUnlocked(game.Mod))
		{
			return s.StatsWithAllBonuses.mana;
		}
		return 0;
	}
	public static int PsiStrengthStat(Game game, Soldier s)
	{
		// don't reveal psi strength before it would otherwise be known
		if (PsiStrengthKnown(game, s))
		{
			return s.StatsWithAllBonuses.psiStrength;
		}
		return 0;
	}
	public static int PsiSkillStat(Game game, Soldier s)
	{
		// when Options::anytimePsiTraining is turned on, psiSkill can actually have a negative value
		if (s.CurrentStats.psiSkill > 0)
		{
			return s.StatsWithAllBonuses.psiSkill;
		}
		return 0;
	}
	public static int MeleeStat(Game game, Soldier s) => s.StatsWithAllBonuses.melee;

	public static int TuStatBase(Game game, Soldier s) => s.CurrentStats.tu;
	public static int StaminaStatBase(Game game, Soldier s) => s.CurrentStats.stamina;
	public static int HealthStatBase(Game game, Soldier s) => s.CurrentStats.health;
	public static int BraveryStatBase(Game game, Soldier s) => s.CurrentStats.bravery;
	public static int ReactionsStatBase(Game game, Soldier s) => s.CurrentStats.reactions;
	public static int FiringStatBase(Game game, Soldier s) => s.CurrentStats.firing;
	public static int ThrowingStatBase(Game game, Soldier s) => s.CurrentStats.throwing;
	public static int StrengthStatBase(Game game, Soldier s) => s.CurrentStats.strength;
	public static int ManaStatBase(Game game, Soldier s)
	{
		// don't reveal mana before it would otherwise be known
		if (game.SavedGame.IsManaUnlocked(game.Mod))
		{
			return s.CurrentStats.mana;
		}
		return 0;
	}
	public static int PsiStrengthStatBase(Game game, Soldier s)
	{
		// don't reveal psi strength before it would otherwise be known
		if (PsiStrengthKnown(game, s))
		{
			return s.CurrentStats.psiStrength;
		}
		return 0;
	}
	public static int PsiSkillStatBase(Game game, Soldier s)
	{
		// when Options::anytimePsiTraining is turned on, psiSkill can actually have a negative value
		if (s.CurrentStats.psiSkill > 0)
		{
			return s.CurrentStats.psiSkill;
		}
		return 0;
	}
	public static int MeleeStatBase(Game game, Soldier s) => s.CurrentStats.melee;

	public static int TuStatPlus(Game game, Soldier s) => s.StatsWithSoldierBonusesOnly.tu;
	public static int StaminaStatPlus(Game game, Soldier s) => s.StatsWithSoldierBonusesOnly.stamina;
	public static int HealthStatPlus(Game game, Soldier s) => s.StatsWithSoldierBonusesOnly.health;
	public static int BraveryStatPlus(Game game, Soldier s) => s.StatsWithSoldierBonusesOnly.bravery;
	public static int ReactionsStatPlus(Game game, Soldier s) => s.StatsWithSoldierBonusesOnly.reactions;
	public static int FiringStatPlus(Game game, Soldier s) => s.StatsWithSoldierBonusesOnly.firing;
	public static int ThrowingStatPlus(Game game, Soldier s) => s.StatsWithSoldierBonusesOnly.throwing;
	public static int StrengthStatPlus(Game game, Soldier s) => s.StatsWithSoldierBonusesOnly.strength;
	public static int ManaStatPlus(Game game, Soldier s)
	{
		// don't reveal mana before it would otherwise be known
		if (game.SavedGame.IsManaUnlocked(game.Mod))
		{
			return s.StatsWithSoldierBonusesOnly.mana;
		}
		return 0;
	}
	public static int PsiStrengthStatPlus(Game game, Soldier s)
	{
		// don't reveal psi strength before it would otherwise be known
		if (PsiStrengthKnown(game, s))
		{
			return s.StatsWithSoldierBonusesOnly.psiStrength;
		}
		return 0;
	}
	public static int PsiSkillStatPlus(Game game, Soldier s)
	{
		// when Options::anytimePsiTraining is turned on, psiSkill can actually have a negative value
		if (s.CurrentStats.psiSkill > 0)
		{
			return s.StatsWithSoldierBonusesOnly.psiSkill;
		}
		return 0;
	}
	public static int MeleeStatPlus(Game game, Soldier s) => s.StatsWithSoldierBonusesOnly.melee;

	public static int IdStat(Game game, Soldier s) => s.Id;
	public static int NameStat(Game game, Soldier s) => 0;
	public static int CraftIdStat(Game game, Soldier s)
	{
		if (s.Craft != null)
		{
			return s.Craft.Id;
		}
		return 0;
	}
	public static int TypeStat(Game game, Soldier s) => s.Rules.ListOrder;
	public static int RankStat(Game game, Soldier s) => (int)s.Rank;
	public static int MissionsStat(Game game, Soldier s) => s.Missions;
	public static int KillsStat(Game game, Soldier s) => s.Kills;
	public static int WoundRecoveryStat(Game game, Soldier s) => s.GetWoundRecovery(0.0f, 0.0f);
	public static int ManaMissingStat(Game game, Soldier s) => s.ManaMissing;
	public static int IdleDaysStat(Game game, Soldier s) => game.SavedGame.GetSoldierIdleDays(s);
}
