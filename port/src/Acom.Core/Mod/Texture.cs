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
// Ported from: src/Mod/Texture.h, src/Mod/Texture.cpp
#nullable disable

namespace Acom;

/// <summary>Terrain choice of a globe texture, optionally limited to an area.</summary>
public class TerrainCriteria
{
	public string name;
	public int weight;
	public double lonMin;
	public double lonMax;
	public double latMin;
	public double latMax;

	/// <remarks>The defaults are in degrees while <c>area</c> is converted to radians (as in C++); they still cover the whole globe.</remarks>
	public TerrainCriteria()
	{
		name = "";
		weight = 1;
		lonMin = 0.0;
		lonMax = 360.0;
		latMin = -90.0;
		latMax = 90.0;
	}

	/// <summary>YAML: map with <c>name</c>, <c>weight</c> and <c>area: [lonMin, lonMax, latMin, latMax]</c> (C++ read overload).</summary>
	public static bool YamlRead(YamlNodeReader reader, ref TerrainCriteria val)
	{
		val ??= new TerrainCriteria();
		reader.TryRead("name", ref val.name);
		reader.TryRead("weight", ref val.weight);
		var areaReader = reader["area"];
		if (areaReader)
		{
			List<double> area = areaReader.ReadVal<List<double>>();
			val.lonMin = Deg2Rad(area[0]);
			val.lonMax = Deg2Rad(area[1]);
			val.latMin = Deg2Rad(area[2]);
			val.latMax = Deg2Rad(area[3]);
		}
		return true;
	}

	private static double Deg2Rad(double deg) => deg * Math.PI / 180.0; // fmath.h
}

/// <summary>
/// Represents the relations between a Geoscape texture
/// and the corresponding Battlescape mission attributes.
/// </summary>
public class Texture
{
	private int _id;
	private int _baseGridSprite;
	private bool _isOcean;
	private bool _fakeUnderwater;
	private string _startingCondition = "";
	private SortedDictionary<string, int> _deployments = new(StringComparer.Ordinal);
	private List<TerrainCriteria> _terrain = [];
	private List<TerrainCriteria> _baseTerrain = [];

	/// <summary>Initializes a globe texture.</summary>
	/// <param name="id">Texture identifier.</param>
	public Texture(int id)
	{
		_id = id;
		_baseGridSprite = 0;
		_isOcean = false;
		_fakeUnderwater = false;
	}

	/// <summary>Loads the texture type from YAML.</summary>
	public void Load(YamlNodeReader reader, Mod mod)
	{
		reader.TryRead("id", ref _id);
		{
			string parent = "id_" + _id;
			mod.LoadSpriteOffset(parent, ref _baseGridSprite, reader["baseGridSprite"], "BASEBITS.PCK");
		}
		reader.TryRead("isOcean", ref _isOcean);
		reader.TryRead("fakeUnderwater", ref _fakeUnderwater);
		reader.TryRead("startingCondition", ref _startingCondition);
		reader.TryRead("deployments", ref _deployments);
		reader.TryRead("terrain", ref _terrain);
		reader.TryRead("baseTerrain", ref _baseTerrain);
	}

	/// <summary>The list of terrain criteria associated with this texture.</summary>
	public List<TerrainCriteria> Terrain => _terrain;

	/// <summary>
	/// Calculates a random terrain for a mission target based
	/// on the texture's available terrain criteria.
	/// </summary>
	/// <param name="target">Pointer to the mission target.</param>
	/// <returns>The name of the picked terrain.</returns>
	public string GetRandomTerrain(Target target)
	{
		int totalWeight = 0;
		var possibilities = new SortedDictionary<int, string>();
		foreach (var terrainCrit in _terrain)
		{
			if (terrainCrit.weight > 0 &&
				target.Longitude >= terrainCrit.lonMin && target.Longitude < terrainCrit.lonMax &&
				target.Latitude >= terrainCrit.latMin && target.Latitude < terrainCrit.latMax)
			{
				totalWeight += terrainCrit.weight;
				possibilities[totalWeight] = terrainCrit.name;
			}
		}

		if (totalWeight > 0)
		{
			int pick = Rng.Generate(1, totalWeight);
			foreach (var pair in possibilities)
			{
				if (pick <= pair.Key)
				{
					return pair.Value;
				}
			}
		}

		return "";
	}

	/// <summary>The list of terrain criteria associated with this texture for base defense missions.</summary>
	public List<TerrainCriteria> BaseTerrain => _baseTerrain;

	/// <summary>
	/// Calculates a random terrain for a base defense mission target based
	/// on the texture's available terrain criteria.
	/// </summary>
	/// <param name="target">Pointer to the mission target.</param>
	/// <returns>The name of the picked terrain.</returns>
	public string GetRandomBaseTerrain(Target target)
	{
		int totalWeight = 0;
		var possibilities = new SortedDictionary<int, string>();
		foreach (var terrainCrit in _baseTerrain)
		{
			if (terrainCrit.weight > 0 &&
				target.Longitude >= terrainCrit.lonMin && target.Longitude < terrainCrit.lonMax &&
				target.Latitude >= terrainCrit.latMin && target.Latitude < terrainCrit.latMax)
			{
				totalWeight += terrainCrit.weight;
				possibilities[totalWeight] = terrainCrit.name;
			}
		}

		// no totalWeight check here, as in C++
		int pick = Rng.Generate(1, totalWeight);
		foreach (var pair in possibilities)
		{
			if (pick <= pair.Key)
			{
				return pair.Value;
			}
		}

		return "";
	}

	/// <summary>The list of deployments associated with this texture.</summary>
	public SortedDictionary<string, int> Deployments => _deployments;

	/// <summary>
	/// Calculates a random deployment for a mission target based
	/// on the texture's available deployments (uses the RNG).
	/// </summary>
	public string RandomDeployment
	{
		get
		{
			if (_deployments.Count == 0)
			{
				return "";
			}

			if (_deployments.Count == 1)
			{
				return _deployments.First().Key;
			}

			int totalWeight = 0;
			foreach (var pair in _deployments)
			{
				totalWeight += pair.Value;
			}

			if (totalWeight >= 1)
			{
				int pick = Rng.Generate(1, totalWeight);
				foreach (var pair in _deployments)
				{
					if (pick <= pair.Value)
					{
						return pair.Key;
					}
					else
					{
						pick -= pair.Value;
					}
				}
			}

			return "";
		}
	}

	/// <summary>Gets the index into BASEBITS.PCK sprite set. Used to draw the base grid.</summary>
	public int BaseGridSprite => _baseGridSprite;

	/// <summary>Is the texture a cosmetic-only ocean texture?</summary>
	public bool IsCosmeticOcean => _isOcean;

	/// <summary>Is the texture a fake underwater texture?</summary>
	public bool IsFakeUnderwater => _fakeUnderwater;

	/// <summary>Gets the Texture's starting condition.</summary>
	public string StartingCondition => _startingCondition;
}
