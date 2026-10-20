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
// Ported from: src/Mod/Polygon.h, src/Mod/Polygon.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents a polygon in the world map.
/// Polygons constitute the textured land portions
/// of the X-Com globe and typically have 3-4 points.
/// </summary>
public class Polygon
{
	private double[] _lat, _lon;
	private short[] _x, _y;
	private int _points, _texture;

	/// <summary>Initializes the polygon with arrays to store each point's coordinates.</summary>
	/// <param name="points">Number of points.</param>
	public Polygon(int points)
	{
		_points = points;
		_texture = 0;
		_lat = new double[_points];
		_lon = new double[_points];
		_x = new short[_points];
		_y = new short[_points];
	}

	/// <summary>Performs a deep copy of an existing polygon.</summary>
	/// <param name="other">Polygon to copy from.</param>
	public Polygon(Polygon other)
	{
		ArgumentNullException.ThrowIfNull(other);
		_points = other._points;
		_lat = (double[])other._lat.Clone();
		_lon = (double[])other._lon.Clone();
		_x = (short[])other._x.Clone();
		_y = (short[])other._y.Clone();
		_texture = other._texture;
	}

	/// <summary>Loads the polygon from YAML: <c>[texture, lon1, lat1, lon2, lat2, ...]</c> in degrees.</summary>
	public void Load(YamlNodeReader reader)
	{
		List<double> coords = reader.ReadVal<List<double>>();
		_points = (coords.Count - 1) / 2;
		_lat = new double[_points];
		_lon = new double[_points];
		_x = new short[_points];
		_y = new short[_points];

		_texture = (int)coords[0];
		for (int i = 1; i < coords.Count; i += 2)
		{
			int j = (i - 1) / 2;
			_lon[j] = Deg2Rad(coords[i]);
			_lat[j] = Deg2Rad(coords[i + 1]);
			_x[j] = 0;
			_y[j] = 0;
		}
	}

	/// <summary>Returns the latitude of a given point.</summary>
	public double GetLatitude(int i) => _lat[i];

	/// <summary>Changes the latitude of a given point.</summary>
	public void SetLatitude(int i, double lat) => _lat[i] = lat;

	/// <summary>Returns the longitude of a given point.</summary>
	public double GetLongitude(int i) => _lon[i];

	/// <summary>Changes the longitude of a given point.</summary>
	public void SetLongitude(int i, double lon) => _lon[i] = lon;

	/// <summary>Returns the X coordinate of a given point.</summary>
	public short GetX(int i) => _x[i];

	/// <summary>Changes the X coordinate of a given point.</summary>
	public void SetX(int i, short x) => _x[i] = x;

	/// <summary>Returns the Y coordinate of a given point.</summary>
	public short GetY(int i) => _y[i];

	/// <summary>Changes the Y coordinate of a given point.</summary>
	public void SetY(int i, short y) => _y[i] = y;

	/// <summary>The texture used to draw the polygon (textures are stored in a set).</summary>
	public int Texture
	{
		get => _texture;
		set => _texture = value;
	}

	/// <summary>Number of points (vertexes) that make up the polygon.</summary>
	public int Points => _points;

	private static double Deg2Rad(double deg) => deg * Math.PI / 180.0; // fmath.h
}
