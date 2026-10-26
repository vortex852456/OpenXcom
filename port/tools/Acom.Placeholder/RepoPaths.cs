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

namespace Acom.Placeholder;

/// <summary>Finds the repository (asset list and master mod rulesets) for the default arguments.</summary>
public static class RepoPaths
{
	/// <summary>Walks up from the working directory and the program folder to the folder that holds content/spec and bin/standard.</summary>
	public static string? FindRoot()
	{
		foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
		{
			for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
			{
				if (File.Exists(Path.Combine(dir.FullName, "content", "spec", "ufo", "assets.json"))
					&& Directory.Exists(Path.Combine(dir.FullName, "bin", "standard", "xcom1")))
					return dir.FullName;
			}
		}
		return null;
	}
}
