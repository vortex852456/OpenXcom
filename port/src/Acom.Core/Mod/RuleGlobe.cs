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
// Ported from: src/Mod/RuleGlobe.h, src/Mod/RuleGlobe.cpp
#nullable disable

using System.Buffers.Binary;

namespace Acom;

/// <summary>
/// Represents the contents of the Geoscape globe,
/// such as world polygons, polylines, etc.
/// </summary>
/// <seealso cref="Globe"/>
public class RuleGlobe
{
	private readonly List<Polygon> _polygons = [];
	private readonly List<Polyline> _polylines = [];
	private readonly SortedDictionary<int, Texture> _textures = new();

	/// <summary>Creates a blank ruleset for globe contents.</summary>
	public RuleGlobe()
	{
	}

	/// <summary>Loads the globe from YAML.</summary>
	public void Load(YamlNodeReader reader, Mod mod)
	{
		var data = reader["data"];
		if (data)
		{
			_polygons.Clear();
			LoadDat(data.ReadVal<string>());
		}

		var polygonsReader = reader["polygons"];
		if (polygonsReader)
		{
			_polygons.Clear();
			foreach (var polygonReader in polygonsReader.Children())
			{
				var polygon = new Polygon(3);
				polygon.Load(polygonReader);
				_polygons.Add(polygon);
			}
		}

		var polylinesReader = reader["polylines"];
		if (polylinesReader)
		{
			_polylines.Clear();
			foreach (var polylineReader in polylinesReader.Children())
			{
				var polyline = new Polyline(3);
				polyline.Load(polylineReader);
				_polylines.Add(polyline);
			}
		}

		foreach (var textureReader in reader["textures"].Children())
		{
			if (textureReader["id"])
			{
				int id = textureReader["id"].ReadVal<int>();
				if (!_textures.TryGetValue(id, out Texture texture))
				{
					texture = new Texture(id);
					_textures[id] = texture;
				}
				texture.Load(textureReader, mod);
			}
			else if (textureReader["delete"])
			{
				int id = textureReader["delete"].ReadVal<int>();
				_textures.Remove(id);
			}
		}

		reader.TryRead("countryColor", ref Globe.COUNTRY_LABEL_COLOR);
		reader.TryRead("cityColor", ref Globe.CITY_LABEL_COLOR);
		reader.TryRead("baseColor", ref Globe.BASE_LABEL_COLOR);
		reader.TryRead("lineColor", ref Globe.LINE_COLOR);

		if (reader["oceanPalette"])
		{
			Globe.OCEAN_COLOR = Palette.BlockOffset(reader["oceanPalette"].ReadVal(Globe.OCEAN_COLOR));
		}
		reader.TryRead("oceanShading", ref Globe.OCEAN_SHADING);
	}

	/// <summary>The list of polygons in the globe.</summary>
	public List<Polygon> Polygons => _polygons;

	/// <summary>The list of polylines in the globe.</summary>
	public List<Polyline> Polylines => _polylines;

	/// <summary>
	/// Loads a series of map polar coordinates in X-Com format,
	/// converts them and stores them in a set of polygons.
	/// </summary>
	/// <param name="filename">Filename of the DAT file.</param>
	/// <remarks>See http://www.ufopaedia.org/index.php?title=WORLD.DAT</remarks>
	public void LoadDat(string filename)
	{
		using var mapFile = FileMap.GetStream(filename);
		var buffer = new byte[20];
		var value = new short[10];
		// a short last record ends the file like the C++ eof check; read errors throw IOException ("Invalid globe map" in C++)
		while (mapFile.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false) == buffer.Length)
		{
			int points;
			for (int i = 0; i < 10; ++i)
			{
				value[i] = BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(i * 2));
			}

			if (value[6] != -1)
			{
				points = 4;
			}
			else
			{
				points = 3;
			}

			var poly = new Polygon(points);
			for (int i = 0, j = 0; i < points; ++i)
			{
				// Correct X-Com degrees and convert to radians
				double lonRad = Xcom2Rad(value[j++]);
				double latRad = Xcom2Rad(value[j++]);

				poly.SetLongitude(i, lonRad);
				poly.SetLatitude(i, latRad);
			}
			poly.Texture = value[8];

			_polygons.Add(poly);
		}
	}

	/// <summary>Returns the rules for the specified texture, or null.</summary>
	/// <param name="id">Texture ID.</param>
	public Texture GetTexture(int id) => _textures.GetValueOrDefault(id);

	/// <summary>Returns a list of all globe terrains associated with this deployment.</summary>
	/// <param name="deployment">Deployment name.</param>
	public List<string> GetTerrains(string deployment)
	{
		var terrains = new List<string>();
		foreach (var pair in _textures)
		{
			if ((string.IsNullOrEmpty(deployment) && pair.Value.Deployments.Count == 0) || pair.Value.Deployments.ContainsKey(deployment))
			{
				foreach (var tc in pair.Value.Terrain)
				{
					terrains.Add(tc.name);
				}
			}
		}
		return terrains;
	}

	/// <summary>Raw data.</summary>
	public SortedDictionary<int, Texture> TexturesRaw => _textures;

	private static double Xcom2Rad(int deg) => deg * 0.125 * Math.PI / 180.0; // fmath.h
}
