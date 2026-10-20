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
// Ported from: src/Savegame/ResearchProject.h, src/Savegame/ResearchProject.cpp
#nullable disable

using System.Globalization;

namespace Acom;

/// <summary>
/// Represent a ResearchProject.
/// Contain information about assigned scientist, time already spent and cost of the project.
/// </summary>
public class ResearchProject
{
	private const float PROGRESS_LIMIT_UNKNOWN = 0.333f;
	private const float PROGRESS_LIMIT_POOR = 0.07f;
	private const float PROGRESS_LIMIT_AVERAGE = 0.13f;
	private const float PROGRESS_LIMIT_GOOD = 0.25f;

	private readonly RuleResearch _project;
	private int _assigned;
	private int _spent;
	private int _cost;

	public ResearchProject(RuleResearch p, int c = 0)
	{
		_project = p;
		_assigned = 0;
		_spent = 0;
		_cost = c;
	}

	/// <summary>Called every day to compute time spent on this ResearchProject.</summary>
	/// <returns>true if the ResearchProject is finished</returns>
	public bool Step()
	{
		_spent += _assigned;
		return IsFinished;
	}

	/// <summary>Gets state of project.</summary>
	public bool IsFinished => _spent >= Cost;

	/// <summary>The number of scientists assigned to this project.</summary>
	public int Assigned
	{
		get => _assigned;
		set => _assigned = value;
	}

	/// <summary>The time already spent on this project (in man/day).</summary>
	public int Spent
	{
		get => _spent;
		set => _spent = value;
	}

	/// <summary>The cost of the ResearchProject (in man/day).</summary>
	public int Cost
	{
		get => _cost;
		set => _cost = value;
	}

	/// <summary>Gets the ResearchProject rule.</summary>
	public RuleResearch Rules => _project;

	/// <summary>Loads the research project from YAML.</summary>
	public void Load(YamlNodeReader reader)
	{
		Assigned = reader["assigned"].ReadVal(Assigned);
		Spent = reader["spent"].ReadVal(Spent);
		Cost = reader["cost"].ReadVal(Cost);
	}

	/// <summary>Saves the research project to YAML.</summary>
	public void Save(YamlNodeWriter writer)
	{
		writer.SetAsMap();
		writer.Write("project", Rules.Name);
		writer.Write("assigned", Assigned);
		writer.Write("spent", Spent);
		writer.Write("cost", Cost);
	}

	/// <summary>Return a string describing Research progress.</summary>
	public string ResearchProgress
	{
		get
		{
			if (Options.oxceFixedResearchCosts)
			{
				int spent = Spent;
				int cost = Math.Max(1, Cost);
				int progress = (int)((float)spent / cost * 100);
				return string.Create(CultureInfo.InvariantCulture, $"{progress}% ({spent}/{cost})");
			}

			float progressRatio = (float)Spent / Rules.Cost;
			if (Assigned == 0)
			{
				return "STR_NONE";
			}
			else if (progressRatio <= PROGRESS_LIMIT_UNKNOWN)
			{
				return "STR_UNKNOWN";
			}
			else
			{
				float rating = (float)Assigned;
				rating /= Rules.Cost;
				if (rating <= PROGRESS_LIMIT_POOR)
				{
					return "STR_POOR";
				}
				else if (rating <= PROGRESS_LIMIT_AVERAGE)
				{
					return "STR_AVERAGE";
				}
				else if (rating <= PROGRESS_LIMIT_GOOD)
				{
					return "STR_GOOD";
				}
				return "STR_EXCELLENT";
			}
		}
	}
}
