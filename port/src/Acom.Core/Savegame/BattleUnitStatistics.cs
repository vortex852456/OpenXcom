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
// Ported from: src/Savegame/BattleUnitStatistics.h
#nullable disable

namespace Acom;

/// <summary>
/// Container for battle unit kills statistics.
/// </summary>
public class BattleUnitKills
{
	public string name = "";
	public string type = "";
	public string rank = "";
	public string race = "";
	public string weapon = "";
	public string weaponAmmo = "";
	public UnitFaction faction;
	public UnitStatus status;
	public int mission;
	public int turn;
	public int id;
	public UnitSide side;
	public UnitBodyPart bodypart;

	/// <summary>Creates the kill and loads it from YAML.</summary>
	/// <remarks>C++ leaves the non-string fields uninitialized here; the port starts from the default values.</remarks>
	public BattleUnitKills(YamlNodeReader reader) : this()
	{
		Load(reader);
	}

	public BattleUnitKills()
	{
		faction = UnitFaction.FACTION_HOSTILE;
		status = UnitStatus.STATUS_IGNORE_ME;
		mission = 0;
		turn = 0;
		id = 0;
		side = UnitSide.SIDE_FRONT;
		bodypart = UnitBodyPart.BODYPART_HEAD;
	}

	/// <summary>Make turn unique across all kills.</summary>
	public int MakeTurnUnique()
	{
		return turn += mission * 300; // Maintains divisibility by 3 as well
	}

	/// <summary>Check to see if turn was on HOSTILE side.</summary>
	public bool HostileTurn()
	{
		if ((turn - 1) % 3 == 0) return true;
		return false;
	}

	/// <summary>Make turn unique across mission.</summary>
	public void SetTurn(int unitTurn, UnitFaction unitFaction)
	{
		turn = unitTurn * 3 + (int)unitFaction;
	}

	/// <summary>Load.</summary>
	public void Load(YamlNodeReader reader)
	{
		reader.TryRead("type", ref type); // The ones killed are usually hostiles, so read this first
		if (type.Length == 0)
			reader.TryRead("name", ref name); // Can't have both type and name at the same time
		reader.TryRead("rank", ref rank);
		reader.TryRead("race", ref race);
		reader.TryRead("weapon", ref weapon);
		reader.TryRead("weaponAmmo", ref weaponAmmo);
		reader.TryRead("status", ref status);
		reader.TryRead("faction", ref faction);
		reader.TryRead("mission", ref mission);
		reader.TryRead("turn", ref turn);
		reader.TryRead("side", ref side);
		reader.TryRead("bodypart", ref bodypart);
		reader.TryRead("id", ref id);
	}

	/// <summary>Save.</summary>
	public void Save(YamlNodeWriter writer)
	{
		writer.SetAsMap();
		writer.SetFlowStyle();
		if (name.Length != 0)
			writer.Write("name", name);
		if (type.Length != 0)
			writer.Write("type", type);
		writer.Write("rank", rank);
		writer.Write("race", race);
		writer.Write("weapon", weapon);
		writer.Write("weaponAmmo", weaponAmmo);
		writer.Write("status", status);
		writer.Write("faction", faction);
		writer.Write("mission", mission);
		writer.Write("turn", turn);
		writer.Write("side", side);
		writer.Write("bodypart", bodypart);
		writer.Write("id", id);
	}

	/// <summary>Convert kill Status to string.</summary>
	public string KillStatusString => status switch
	{
		UnitStatus.STATUS_DEAD => "STR_KILLED",
		UnitStatus.STATUS_UNCONSCIOUS => "STR_STUNNED",
		UnitStatus.STATUS_PANICKING => "STR_PANICKED",
		UnitStatus.STATUS_TURNING => "STR_MINDCONTROLLED",
		_ => "status error",
	};

	/// <summary>Convert victim Status to string.</summary>
	public string UnitStatusString => status switch
	{
		UnitStatus.STATUS_DEAD => "STATUS_DEAD",
		UnitStatus.STATUS_UNCONSCIOUS => "STATUS_UNCONSCIOUS",
		UnitStatus.STATUS_PANICKING => "STATUS_PANICKING",
		UnitStatus.STATUS_TURNING => "STATUS_TURNING",
		_ => "status error",
	};

	/// <summary>Convert victim Faction to string.</summary>
	public string UnitFactionString => faction switch
	{
		UnitFaction.FACTION_PLAYER => "FACTION_PLAYER",
		UnitFaction.FACTION_HOSTILE => "FACTION_HOSTILE",
		UnitFaction.FACTION_NEUTRAL => "FACTION_NEUTRAL",
		_ => "faction error",
	};

	/// <summary>Convert victim Side to string.</summary>
	public string UnitSideString => side switch
	{
		UnitSide.SIDE_FRONT => "SIDE_FRONT",
		UnitSide.SIDE_LEFT => "SIDE_LEFT",
		UnitSide.SIDE_RIGHT => "SIDE_RIGHT",
		UnitSide.SIDE_REAR => "SIDE_REAR",
		UnitSide.SIDE_UNDER => "SIDE_UNDER",
		_ => "side error",
	};

	/// <summary>Convert victim Body part to string.</summary>
	public string UnitBodyPartString => bodypart switch
	{
		UnitBodyPart.BODYPART_HEAD => "BODYPART_HEAD",
		UnitBodyPart.BODYPART_TORSO => "BODYPART_TORSO",
		UnitBodyPart.BODYPART_RIGHTARM => "BODYPART_RIGHTARM",
		UnitBodyPart.BODYPART_LEFTARM => "BODYPART_LEFTARM",
		UnitBodyPart.BODYPART_RIGHTLEG => "BODYPART_RIGHTLEG",
		UnitBodyPart.BODYPART_LEFTLEG => "BODYPART_LEFTLEG",
		_ => "body part error",
	};

	/// <summary>Get human-readable victim name.</summary>
	public string GetUnitName(Language lang)
	{
		if (name.Length != 0)
		{
			return name;
		}
		else if (type.Length != 0)
		{
			return lang.GetString(type);
		}
		else
		{
			return lang.GetString(race) + " " + lang.GetString(rank);
		}
	}

	/// <summary>Decide victim name, race and rank.</summary>
	public void SetUnitStats(BattleUnit unit)
	{
		name = "";
		type = "";
		if (unit.GeoscapeSoldier != null)
		{
			name = unit.GeoscapeSoldier.GetName();
		}
		else
		{
			type = unit.Type;
		}

		if (unit.OriginalFaction == UnitFaction.FACTION_PLAYER)
		{
			// Soldiers
			if (unit.GeoscapeSoldier != null)
			{
				if (unit.GeoscapeSoldier.RankString.Length != 0)
				{
					rank = unit.GeoscapeSoldier.RankString;
				}
				else
				{
					rank = "STR_SOLDIER";
				}
				if (unit.UnitRules != null && unit.UnitRules.Race.Length != 0)
				{
					race = unit.UnitRules.Race;
				}
				else
				{
					race = "STR_FRIENDLY";
				}
			}
			// HWPs
			else
			{
				if (unit.UnitRules != null && unit.UnitRules.Rank.Length != 0)
				{
					rank = unit.UnitRules.Rank;
				}
				else
				{
					rank = "STR_HWPS";
				}
				if (unit.UnitRules != null && unit.UnitRules.Race.Length != 0)
				{
					race = unit.UnitRules.Race;
				}
				else
				{
					race = "STR_FRIENDLY";
				}
			}
		}
		// Aliens
		else if (unit.OriginalFaction == UnitFaction.FACTION_HOSTILE)
		{
			if (unit.UnitRules != null && unit.UnitRules.Rank.Length != 0)
			{
				rank = unit.UnitRules.Rank;
			}
			else
			{
				rank = "STR_LIVE_SOLDIER";
			}
			if (unit.UnitRules != null && unit.UnitRules.Race.Length != 0)
			{
				race = unit.UnitRules.Race;
			}
			else
			{
				race = "STR_HOSTILE";
			}
		}
		// Civilians
		else if (unit.OriginalFaction == UnitFaction.FACTION_NEUTRAL)
		{
			if (unit.UnitRules != null && unit.UnitRules.Rank.Length != 0)
			{
				rank = unit.UnitRules.Rank;
			}
			else
			{
				rank = "STR_CIVILIAN";
			}
			if (unit.UnitRules != null && unit.UnitRules.Race.Length != 0)
			{
				race = unit.UnitRules.Race;
			}
			else
			{
				race = "STR_NEUTRAL";
			}
		}
		// Error
		else
		{
			rank = "STR_UNKNOWN";
			race = "STR_UNKNOWN";
		}
	}
}

/// <summary>
/// Container for battle unit statistics.
/// </summary>
public class BattleUnitStatistics
{
	public bool wasUnconcious;                  // Tracks if the soldier fell unconscious
	public int shotAtCounter;                   // Tracks how many times the unit was shot at
	public int hitCounter;                      // Tracks how many times the unit was hit
	public int shotByFriendlyCounter;           // Tracks how many times the unit was hit by a friendly
	public int shotFriendlyCounter;             // Tracks how many times the unit was hit a friendly
	public bool loneSurvivor;                   // Tracks if the soldier was the only survivor
	public bool ironMan;                        // Tracks if the soldier was the only soldier on the mission
	public int longDistanceHitCounter;          // Tracks how many long distance shots were landed
	public int lowAccuracyHitCounter;           // Tracks how many times the unit landed a low probability shot
	public int shotsFiredCounter;               // Tracks how many times a unit has shot
	public int shotsLandedCounter;              // Tracks how many times a unit has hit his target
	public List<BattleUnitKills> kills = [];    // Tracks kills
	public int daysWounded;                     // Tracks how many days the unit was wounded for
	public bool KIA;                            // Tracks if the soldier was killed in battle
	public bool nikeCross;                      // Tracks if a soldier killed every alien or killed and stunned every alien
	public bool mercyCross;                     // Tracks if a soldier stunned every alien
	public int woundsHealed;                    // Tracks how many times a fatal wound was healed by this unit
	public UnitStats delta;                     // Tracks the increase in unit stats (is not saved, only used during debriefing)
	public int appliedStimulant;                // Tracks how many times this soldier applied stimulant
	public int appliedPainKill;                 // Tracks how many times this soldier applied pain killers
	public int revivedSoldier;                  // Tracks how many times this soldier revived another soldier
	public int revivedHostile;                  // Tracks how many times this soldier revived another hostile
	public int revivedNeutral;                  // Tracks how many times this soldier revived another civilian
	public bool MIA;                            // Tracks if the soldier was left behind :(
	public int martyr;                          // Tracks how many kills the soldier landed on the turn of his death
	public int slaveKills;                      // Tracks how many kills the soldier landed thanks to a mind controlled unit.

	/// <summary>Creates the statistics and loads them from YAML.</summary>
	/// <remarks>C++ leaves the fields uninitialized here; the port starts from the default values.</remarks>
	public BattleUnitStatistics(YamlNodeReader reader)
	{
		Load(reader);
	}

	public BattleUnitStatistics()
	{
	}

	/// <summary>Duplicate entry check.</summary>
	public bool DuplicateEntry(UnitStatus status, int id)
	{
		foreach (var buk in kills)
		{
			if (buk.id == id && buk.status == status)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Friendly fire check.</summary>
	public bool HasFriendlyFired
	{
		get
		{
			foreach (var buk in kills)
			{
				if (buk.faction == UnitFaction.FACTION_PLAYER)
					return true;
			}
			return false;
		}
	}

	/// <summary>Load function.</summary>
	public void Load(YamlNodeReader node)
	{
		var reader = node.UseIndex();
		reader.TryRead("wasUnconcious", ref wasUnconcious);
		foreach (var kill in reader["kills"].Children())
			kills.Add(new BattleUnitKills(kill));
		reader.TryRead("shotAtCounter", ref shotAtCounter);
		reader.TryRead("hitCounter", ref hitCounter);
		reader.TryRead("shotByFriendlyCounter", ref shotByFriendlyCounter);
		reader.TryRead("shotFriendlyCounter", ref shotFriendlyCounter);
		reader.TryRead("loneSurvivor", ref loneSurvivor);
		reader.TryRead("ironMan", ref ironMan);
		reader.TryRead("longDistanceHitCounter", ref longDistanceHitCounter);
		reader.TryRead("lowAccuracyHitCounter", ref lowAccuracyHitCounter);
		reader.TryRead("shotsFiredCounter", ref shotsFiredCounter);
		reader.TryRead("shotsLandedCounter", ref shotsLandedCounter);
		reader.TryRead("nikeCross", ref nikeCross);
		reader.TryRead("mercyCross", ref mercyCross);
		reader.TryRead("woundsHealed", ref woundsHealed);
		reader.TryRead("appliedStimulant", ref appliedStimulant);
		reader.TryRead("appliedPainKill", ref appliedPainKill);
		reader.TryRead("revivedSoldier", ref revivedSoldier);
		reader.TryRead("revivedHostile", ref revivedHostile);
		reader.TryRead("revivedNeutral", ref revivedNeutral);
		reader.TryRead("martyr", ref martyr);
		reader.TryRead("slaveKills", ref slaveKills);
	}

	/// <summary>Save function.</summary>
	public void Save(YamlNodeWriter writer)
	{
		writer.SetAsMap();
		if (wasUnconcious) writer.Write("wasUnconcious", wasUnconcious);
		writer.Write("kills", kills, (w, k) => k.Save(w.Write()));
		if (shotAtCounter != 0) writer.Write("shotAtCounter", shotAtCounter);
		if (hitCounter != 0) writer.Write("hitCounter", hitCounter);
		if (shotByFriendlyCounter != 0) writer.Write("shotByFriendlyCounter", shotByFriendlyCounter);
		if (shotFriendlyCounter != 0) writer.Write("shotFriendlyCounter", shotFriendlyCounter);
		if (loneSurvivor) writer.Write("loneSurvivor", loneSurvivor);
		if (ironMan) writer.Write("ironMan", ironMan);
		if (longDistanceHitCounter != 0) writer.Write("longDistanceHitCounter", longDistanceHitCounter);
		if (lowAccuracyHitCounter != 0) writer.Write("lowAccuracyHitCounter", lowAccuracyHitCounter);
		if (shotsFiredCounter != 0) writer.Write("shotsFiredCounter", shotsFiredCounter);
		if (shotsLandedCounter != 0) writer.Write("shotsLandedCounter", shotsLandedCounter);
		if (nikeCross) writer.Write("nikeCross", nikeCross);
		if (mercyCross) writer.Write("mercyCross", mercyCross);
		if (woundsHealed != 0) writer.Write("woundsHealed", woundsHealed);
		if (appliedStimulant != 0) writer.Write("appliedStimulant", appliedStimulant);
		if (appliedPainKill != 0) writer.Write("appliedPainKill", appliedPainKill);
		if (revivedSoldier != 0) writer.Write("revivedSoldier", revivedSoldier);
		if (revivedHostile != 0) writer.Write("revivedHostile", revivedHostile);
		if (revivedNeutral != 0) writer.Write("revivedNeutral", revivedNeutral);
		if (martyr != 0) writer.Write("martyr", martyr);
		if (slaveKills != 0) writer.Write("slaveKills", slaveKills);
		// for backwards compatibility, we output empty map as null
		if (!writer.ToReader()[0])
		{
			writer.UnsetAsMap();
			writer.SetValueNull();
		}
	}
}
