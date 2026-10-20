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
// Ported from: src/Mod/Polyline.h, src/Mod/Polyline.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents a polyline in the world map.
/// Polylines constitute the detail portions of the
/// X-Com globe and typically represent borders and rivers.
/// </summary>
public class Polyline
{
	private double[] _lat, _lon;
	private int _points;

	/// <summary>Initializes the polyline with arrays to store each point's coordinates.</summary>
	/// <param name="points">Number of points.</param>
	public Polyline(int points)
	{
		_points = points;
		_lat = new double[points];
		_lon = new double[points];
	}

	/// <summary>Loads the polyline from YAML: <c>[lon1, lat1, lon2, lat2, ...]</c> in degrees.</summary>
	public void Load(YamlNodeReader reader)
	{
		List<double> coords = reader.ReadVal<List<double>>();
		_points = coords.Count / 2;
		_lat = new double[_points];
		_lon = new double[_points];

		for (int i = 0; i < coords.Count; i += 2)
		{
			int j = i / 2;
			_lon[j] = Deg2Rad(coords[i]);
			_lat[j] = Deg2Rad(coords[i + 1]);
		}
	}

	/// <summary>Returns the latitude (X) of a given point.</summary>
	public double GetLatitude(int i) => _lat[i];

	/// <summary>Changes the latitude of a given point.</summary>
	public void SetLatitude(int i, double lat) => _lat[i] = lat;

	/// <summary>Returns the longitude (Y) of a given point.</summary>
	public double GetLongitude(int i) => _lon[i];

	/// <summary>Changes the longitude of a given point.</summary>
	public void SetLongitude(int i, double lon) => _lon[i] = lon;

	/// <summary>Number of points (vertexes) that make up the polyline.</summary>
	public int Points => _points;

	private static double Deg2Rad(double deg) => deg * Math.PI / 180.0; // fmath.h
}
