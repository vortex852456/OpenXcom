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
// Ported from: src/Mod/City.h, src/Mod/City.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents a city of the world.
/// Aliens target cities for certain missions.
/// </summary>
public class City : Target
{
	/// <summary>Initializes a city with certain data.</summary>
	/// <param name="name">Name of the city.</param>
	/// <param name="lon">Longitude of the city.</param>
	/// <param name="lat">Latitude of the city.</param>
	public City(string name, double lon, double lat) : base()
	{
		_name = name;
		_lon = lon;
		_lat = lat;
	}

	/// <summary>Gets the city's type (unused; private in C++).</summary>
	public override string Type => "";

	/// <summary>Returns the city's unique identifying name.</summary>
	/// <param name="lang">Language to get strings from.</param>
	public override string GetName(Language lang)
	{
		ArgumentNullException.ThrowIfNull(lang);
		return lang.GetString(_name);
	}

	/// <summary>Returns the globe marker for the city.</summary>
	public override int Marker => 8;
}
