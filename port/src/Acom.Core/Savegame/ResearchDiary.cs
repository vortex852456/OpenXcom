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
// Ported from: src/Savegame/ResearchDiary.h
#nullable disable

namespace Acom;

public enum DiscoverySourceType : byte
{
	BASE = 0,
	FREE_FROM = 1,
	FREE_AFTER = 2,
	MISSION = 3,
	EVENT = 4,
}

/// <summary>Where a research topic was discovered.</summary>
public class DiscoverySource
{
	public DiscoverySourceType type;
	/// <summary>Not guaranteed to be valid.</summary>
	public string name = "";
	public RuleResearch research;
	public RuleEvent @event;
	public AlienDeployment mission;

	public string TypeString => type switch
	{
		DiscoverySourceType.BASE => "STR_DISCOVERY_SOURCE_BASE",
		DiscoverySourceType.FREE_FROM => "STR_DISCOVERY_SOURCE_FREE_FROM",
		DiscoverySourceType.FREE_AFTER => "STR_DISCOVERY_SOURCE_FREE_AFTER",
		DiscoverySourceType.MISSION => "STR_DISCOVERY_SOURCE_MISSION",
		DiscoverySourceType.EVENT => "STR_DISCOVERY_SOURCE_EVENT",
		_ => throw new InvalidOperationException("Invalid DiscoverySourceType!"),
	};
}

/// <summary>
/// Element of the research diary.
/// </summary>
public class ResearchDiaryEntry
{
	public RuleResearch research;
	public uint year;
	public uint month;
	public uint day;
	public DiscoverySource source = new();

	public ResearchDiaryEntry(RuleResearch r)
	{
		research = r;
	}

	public void SetDate(GameTime time)
	{
		ArgumentNullException.ThrowIfNull(time);
		year = unchecked((uint)time.Year);
		month = unchecked((uint)time.Month);
		day = unchecked((uint)time.Day);
	}

	public void Load(YamlNodeReader reader, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		if (research == null)
		{
			string name = "";
			reader.TryRead("name", ref name);
			research = mod.GetResearch(name);
		}
		YamlNodeReader dateReader = reader["date"];
		year = dateReader[0].ReadVal<uint>();
		month = dateReader[1].ReadVal<uint>();
		day = dateReader[2].ReadVal<uint>();
		reader.TryRead("sourceType", ref source.type);
		reader.TryRead("sourceName", ref source.name);
		switch (source.type)
		{
			case DiscoverySourceType.FREE_FROM:
			case DiscoverySourceType.FREE_AFTER:
				source.research = mod.GetResearch(source.name, false);
				break;
			case DiscoverySourceType.MISSION:
				source.mission = mod.GetDeployment(source.name, false);
				break;
			case DiscoverySourceType.EVENT:
				source.@event = mod.GetEvent(source.name, false);
				break;
			default:
				break;
		}
	}

	public void Save(YamlNodeWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.SetAsMap();
		writer.SetFlowStyle();
		YamlNodeWriter dateWriter = writer["date"];
		dateWriter.SetAsSeq();
		dateWriter.SetFlowStyle();
		dateWriter.Write(year);
		dateWriter.Write(month);
		dateWriter.Write(day);
		writer.Write("name", research.Name);
		writer.Write("sourceType", source.type);
		writer.Write("sourceName", source.name);
	}
}
