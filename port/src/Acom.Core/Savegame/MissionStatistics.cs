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
// Ported from: src/Savegame/MissionStatistics.h
#nullable disable

namespace Acom;

/// <summary>
/// Container for mission statistics.
/// </summary>
public class MissionStatistics
{
	// Variables
	public int id;
	public string markerName = "";
	public int markerId;
	public GameTime time;
	public string region = "", country = "", type = "", ufo = "";
	public bool success;
	public string rating = "";
	public int score;
	public string alienRace = "";
	public int daylight;
	public SortedDictionary<int, int> injuryList = [];
	public bool valiantCrux;
	public int lootValue;

	/// <summary>Load.</summary>
	public void Load(YamlNodeReader reader)
	{
		reader.TryRead("id", ref id);
		reader.TryRead("markerName", ref markerName);
		reader.TryRead("markerId", ref markerId);
		time.Load(reader["time"]);
		reader.TryRead("region", ref region);
		reader.TryRead("country", ref country);
		reader.TryRead("type", ref type);
		reader.TryRead("ufo", ref ufo);
		reader.TryRead("success", ref success);
		reader.TryRead("score", ref score);
		reader.TryRead("rating", ref rating);
		reader.TryRead("alienRace", ref alienRace);
		reader.TryRead("daylight", ref daylight);
		reader.TryRead("injuryList", ref injuryList);
		reader.TryRead("valiantCrux", ref valiantCrux);
		reader.TryRead("lootValue", ref lootValue);
	}

	/// <summary>Save.</summary>
	public void Save(YamlNodeWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.SetAsMap();
		writer.Write("id", id);
		if (!string.IsNullOrEmpty(markerName))
		{
			writer.Write("markerName", markerName);
			writer.Write("markerId", markerId);
		}
		time.Save(writer["time"]);
		writer.Write("region", region);
		writer.Write("country", country);
		writer.Write("type", type);
		writer.Write("ufo", ufo);
		writer.Write("success", success);
		writer.Write("score", score);
		writer.Write("rating", rating);
		writer.Write("alienRace", alienRace);
		writer.Write("daylight", daylight);
		if (injuryList.Count != 0)
			writer.Write("injuryList", injuryList);
		if (valiantCrux) writer.Write("valiantCrux", valiantCrux);
		if (lootValue != 0) writer.Write("lootValue", lootValue);
	}

	public string GetMissionName(Language lang)
	{
		ArgumentNullException.ThrowIfNull(lang);
		if (!string.IsNullOrEmpty(markerName))
		{
			return lang.GetString(markerName).Arg(markerId);
		}
		else
		{
			return lang.GetString(type);
		}
	}

	public string GetRatingString(Language lang)
	{
		ArgumentNullException.ThrowIfNull(lang);
		string ss;
		if (success)
		{
			ss = lang.GetString("STR_VICTORY");
		}
		else
		{
			ss = lang.GetString("STR_DEFEAT");
		}
		return ss + " - " + lang.GetString(rating);
	}

	public string LocationString
	{
		get
		{
			if (country == "STR_UNKNOWN")
			{
				return region;
			}
			else
			{
				return country;
			}
		}
	}

	public bool IsDarkness(Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		return daylight > mod.MaxDarknessToSeeUnits;
	}

	public string GetDaylightString(Mod mod)
	{
		if (IsDarkness(mod))
		{
			return "STR_NIGHT";
		}
		else
		{
			return "STR_DAY";
		}
	}

	public bool IsAlienBase
	{
		get
		{
			if (type.Contains("STR_ALIEN_BASE", StringComparison.Ordinal) || type.Contains("STR_ALIEN_COLONY", StringComparison.Ordinal))
			{
				return true;
			}
			return false;
		}
	}

	public bool IsBaseDefense => type == "STR_BASE_DEFENSE";

	public bool IsUfoMission => ufo != "NO_UFO";

	/// <summary>Creates the statistics from YAML (fields that are not in the YAML stay empty/zero).</summary>
	public MissionStatistics(YamlNodeReader reader)
	{
		time = new GameTime(0, 0, 0, 0, 0, 0, 0);
		Load(reader);
	}

	public MissionStatistics()
	{
		id = 0;
		markerId = 0;
		time = new GameTime(0, 0, 0, 0, 0, 0, 0);
		region = "STR_REGION_UNKNOWN";
		country = "STR_UNKNOWN";
		ufo = "NO_UFO";
		success = false;
		score = 0;
		alienRace = "STR_UNKNOWN";
		daylight = 0;
		valiantCrux = false;
		lootValue = 0;
	}
}
