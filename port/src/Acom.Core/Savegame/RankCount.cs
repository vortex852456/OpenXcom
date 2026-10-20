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
// Ported from: src/Savegame/RankCount.h, src/Savegame/RankCount.cpp
#nullable disable

namespace Acom;

/// <summary>Base class for the rank count classes.</summary>
public class RankCountBase
{
	/// <summary>Count of soldiers in each rank.</summary>
	protected int[] _rankCounts;
	protected int _totalSoldiers;

	protected RankCountBase()
	{
		_rankCounts = new int[(int)SoldierRank.RANK_COMMANDER + 1];
		_totalSoldiers = 0;
	}

	/// <summary>Gets/sets the count by SoldierRank (C++ <c>int&amp; operator[]</c>).</summary>
	public int this[SoldierRank rank]
	{
		get => _rankCounts[(int)rank];
		set => _rankCounts[(int)rank] = value;
	}

	/// <summary>Gets the total number of soldiers counted.</summary>
	public int TotalSoldiers => _totalSoldiers;
}

/// <summary>Container for counts of soldier ranks.</summary>
public class RankCount : RankCountBase
{
	/// <summary>Creates a new RankCount object containing rank information.</summary>
	/// <param name="soldiers">The list of soldiers to collect rank information about. Typically should be all soldiers.</param>
	public RankCount(List<Soldier> soldiers)
	{
		ArgumentNullException.ThrowIfNull(soldiers);
		_totalSoldiers = 0;
		foreach (var soldier in soldiers)
		{
			if (soldier.Rules.AllowPromotion)
			{
				_rankCounts[(int)soldier.Rank]++;
				_totalSoldiers++;
			}
		}
	}
}

/// <summary>Container for counts of promotion openings.</summary>
public class PromotionOpenings : RankCountBase
{
	/// <summary>
	/// Creates a new PromotionOpenings object, containing the count of promotion openings based on the mods promotion rules.
	/// Ranks that are overfull for some reason will show as 0 openings, ranks that have unlimited openings will have -1 openings.
	/// </summary>
	/// <param name="soldiers">The set of soldiers to consider for calculation, typically should be all active soldiers.</param>
	/// <param name="mod">The mod containing the rules for promotions.</param>
	public PromotionOpenings(List<Soldier> soldiers, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		RankCount currentRankCount = new RankCount(soldiers);

		int totalSoldiers = currentRankCount.TotalSoldiers;

		// special logic, there is only one Commander ever.
		if (currentRankCount[SoldierRank.RANK_COMMANDER] == 0 && totalSoldiers >= mod.GetSoldiersPerRank(SoldierRank.RANK_COMMANDER))
		{
			_rankCounts[(int)SoldierRank.RANK_COMMANDER]++;
		}

		_rankCounts[(int)SoldierRank.RANK_COLONEL] += Math.Max(0, totalSoldiers / mod.GetSoldiersPerRank(SoldierRank.RANK_COLONEL) - currentRankCount[SoldierRank.RANK_COLONEL]);
		_rankCounts[(int)SoldierRank.RANK_CAPTAIN] += Math.Max(0, totalSoldiers / mod.GetSoldiersPerRank(SoldierRank.RANK_CAPTAIN) - currentRankCount[SoldierRank.RANK_CAPTAIN]);
		_rankCounts[(int)SoldierRank.RANK_SERGEANT] += Math.Max(0, totalSoldiers / mod.GetSoldiersPerRank(SoldierRank.RANK_SERGEANT) - currentRankCount[SoldierRank.RANK_SERGEANT]);

		// promotions to Squaddie and Rookie are unlimited, indicate this with a -1.
		_rankCounts[(int)SoldierRank.RANK_SQUADDIE] = -1;
		_rankCounts[(int)SoldierRank.RANK_ROOKIE] = -1;
	}

	/// <summary>Tests if a soldier can be manually promoted to a new rank.</summary>
	/// <param name="soldier">The soldier to test for promotion.</param>
	/// <param name="newRank">The new rank to promote the soldier to.</param>
	/// <returns>True if the soldier can be promoted to this rank, false otherwise.</returns>
	public bool IsManualPromotionPossible(Soldier soldier, SoldierRank newRank)
	{
		ArgumentNullException.ThrowIfNull(soldier);
		// check if the soldiers rules allow promotion.
		var soldierRules = soldier.Rules;
		if (!soldierRules.AllowPromotion)
		{
			return false;
		}

		SoldierRank currentRank = soldier.Rank;

		// rookies cannot be promoted
		if (currentRank == SoldierRank.RANK_ROOKIE /* || newRank == RANK_ROOKIE */)
		{
			return false;
		}

		// can't promote to same rank.
		if (currentRank == newRank)
		{
			return false;
		}

		// If the rankString for the soldier is not empty, check if the new rank is defined in the rank strings.
		// If not, it is not allowed. If no rankStrings are defined, we assume default behavior.
		int rankStringsSize = soldierRules.RankStrings.Count;
		if (rankStringsSize != 0 && (int)newRank >= rankStringsSize)
		{
			return false;
		}

		// can always demote to rookie or squaddie
		if (newRank == SoldierRank.RANK_ROOKIE || newRank == SoldierRank.RANK_SQUADDIE)
		{
			return true;
		}

		// otherwise promotion or demotion depends on there being an opening.
		return _rankCounts[(int)newRank] > 0;
	}
}
