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
// Ported from: src/Mod/RuleRegion.h, src/Mod/RuleRegion.cpp
#nullable disable

using System.Diagnostics;

namespace Acom;

/// <summary>
/// Defines a rectangle in polar coordinates.
/// It is used to define areas for a mission zone.
/// </summary>
/// <remarks>A value type in C++: use the copy constructor where C++ copies it.</remarks>
public class MissionArea
{
	public double lonMin;
	public double lonMax;
	public double latMin;
	public double latMax;
	public int texture;
	public string name;

	/// <remarks>The defaults are in degrees while YAML values are converted to radians (as in C++).</remarks>
	public MissionArea()
	{
		lonMin = 0.0;
		lonMax = 360.0;
		latMin = -90.0;
		latMax = 90.0;
		texture = 0;
		name = "";
	}

	/// <summary>Copy (C++ copy constructor).</summary>
	public MissionArea(MissionArea other)
	{
		ArgumentNullException.ThrowIfNull(other);
		lonMin = other.lonMin;
		lonMax = other.lonMax;
		latMin = other.latMin;
		latMax = other.latMax;
		texture = other.texture;
		name = other.name;
	}

	/// <summary>Same rectangle (C++ <c>operator==</c>: compares the coordinates only).</summary>
	public bool IsSameArea(MissionArea ma)
	{
		ArgumentNullException.ThrowIfNull(ma);
		return AreSame(lonMax, ma.lonMax) && AreSame(lonMin, ma.lonMin) && AreSame(latMax, ma.latMax) && AreSame(latMin, ma.latMin);
	}

	public bool IsPoint => AreSame(lonMin, lonMax) && AreSame(latMin, latMax);

	/// <summary>YAML: <c>[lonMin, lonMax, latMin, latMax, texture?, name?]</c> in degrees (C++ read overload).</summary>
	public static bool YamlRead(YamlNodeReader reader, ref MissionArea val)
	{
		val ??= new MissionArea();
		val.lonMin = Deg2Rad(reader[0].ReadVal<double>());
		val.lonMax = Deg2Rad(reader[1].ReadVal<double>());
		val.latMin = Deg2Rad(reader[2].ReadVal<double>());
		val.latMax = Deg2Rad(reader[3].ReadVal<double>());
		if (val.latMin > val.latMax)
			(val.latMin, val.latMax) = (val.latMax, val.latMin);
		int count = reader.ChildrenCount;
		if (count >= 5)
			val.texture = reader[4].ReadVal<int>();
		if (count >= 6)
			val.name = reader[5].ReadVal<string>();
		return true;
	}

	/// <summary>fmath.h <c>AreSame(double, double)</c> with DBL_EPSILON.</summary>
	internal static bool AreSame(double l, double r)
	{
		const double DBL_EPSILON = 2.2204460492503131e-16;
		return Math.Abs(l - r) <= DBL_EPSILON * Math.Max(1.0, Math.Max(Math.Abs(l), Math.Abs(r)));
	}

	internal static double Deg2Rad(double deg) => deg * Math.PI / 180.0; // fmath.h

	internal static double Rad2Deg(double rad) => rad / Math.PI * 180.0; // fmath.h
}

/// <summary>
/// A zone (set of areas) on the globe.
/// </summary>
public class MissionZone
{
	public List<MissionArea> areas = [];

	public void Swap(MissionZone other)
	{
		ArgumentNullException.ThrowIfNull(other);
		(areas, other.areas) = (other.areas, areas);
	}

	/// <summary>YAML: sequence of mission areas (C++ read overload).</summary>
	public static bool YamlRead(YamlNodeReader reader, ref MissionZone val)
	{
		val ??= new MissionZone();
		reader.TryReadVal(ref val.areas);
		return true;
	}
}

/// <summary>
/// Represents a specific region of the world.
/// Contains constant info about a region like area
/// covered and base construction costs.
/// </summary>
public class RuleRegion
{
	private readonly string _type;
	private int _cost;
	private readonly List<double> _lonMin = [], _lonMax = [], _latMin = [], _latMax = [];
	private readonly List<City> _cities = [];
	/// <summary>Weighted list of the different mission types for this region (created on first use).</summary>
	private WeightedOptions _missionWeights;
	/// <summary>Weight of this region when selecting regions for alien missions.</summary>
	private ulong _regionWeight;
	/// <summary>All the mission zones in this region.</summary>
	private List<MissionZone> _missionZones = [];
	/// <summary>Do missions in the region defined by this string instead.</summary>
	private string _missionRegion = "";
	private RuleBaseFacilityFunctions _provideBaseFunc;
	private RuleBaseFacilityFunctions _forbiddenBaseFunc;

	/// <summary>Creates a blank ruleset for a certain type of region.</summary>
	/// <param name="type">String defining the type.</param>
	public RuleRegion(string type)
	{
		_type = type;
		_cost = 0;
		_regionWeight = 0;
	}

	/// <summary>Loads the region type from YAML.</summary>
	public void Load(YamlNodeReader reader, Mod mod)
	{
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent, mod);
		}

		reader.TryRead("cost", ref _cost);

		if (reader["deleteOldAreas"].ReadVal(false))
		{
			_lonMin.Clear();
			_lonMax.Clear();
			_latMin.Clear();
			_latMax.Clear();
		}
		foreach (var area in reader["areas"].Children())
		{
			_lonMin.Add(MissionArea.Deg2Rad(area[0].ReadVal<double>()));
			_lonMax.Add(MissionArea.Deg2Rad(area[1].ReadVal<double>()));
			_latMin.Add(MissionArea.Deg2Rad(area[2].ReadVal<double>()));
			_latMax.Add(MissionArea.Deg2Rad(area[3].ReadVal<double>()));

			int last = _latMin.Count - 1;
			if (_latMin[last] > _latMax[last])
				(_latMin[last], _latMax[last]) = (_latMax[last], _latMin[last]);
		}

		reader.TryRead("missionZones", ref _missionZones);
		{
			int zn = 0;
			foreach (var z in _missionZones)
			{
				if (z.areas.Count < 1)
				{
					Logger.Warning($"Empty zone, region: {_type}, zone: {zn}");
					continue;
				}
				int an = 0;
				bool firstAreaType = z.areas[0].IsPoint;
				foreach (var a in z.areas)
				{
					if (a.IsPoint != firstAreaType)
					{
						Logger.Warning($"Mixed area types (point vs non-point), region: {_type}, zone: {zn}, area: {an}");
					}
					if (a.lonMin > a.lonMax)
					{
						Logger.Error($"Crossing the prime meridian in mission zones requires a different syntax, region: {_type}, zone: {zn}, area: {an}, lonMin: {MissionArea.Rad2Deg(a.lonMin):G6}, lonMax: {MissionArea.Rad2Deg(a.lonMax):G6}");
						Logger.Info("  Wrong example: [350,   8, 20, 30]");
						Logger.Info("Correct example: [350, 368, 20, 30]");
					}
					++an;
				}
				++zn;
			}
		}

		var weights = reader["missionWeights"];
		if (weights)
		{
			(_missionWeights ??= new WeightedOptions()).Load(weights);
		}
		reader.TryRead("regionWeight", ref _regionWeight);
		reader.TryRead("missionRegion", ref _missionRegion);

		mod.LoadBaseFunction(_type, ref _provideBaseFunc, reader["provideBaseFunc"]);
		mod.LoadBaseFunction(_type, ref _forbiddenBaseFunc, reader["forbiddenBaseFunc"]);
	}

	/// <summary>
	/// Gets the language string that names this region.
	/// Each region type has a unique name.
	/// </summary>
	public string Type => _type;

	/// <summary>Gets the cost of building a base inside this region.</summary>
	public int BaseCost => _cost;

	/// <summary>Checks if a point is inside this region.</summary>
	/// <param name="lon">Longitude in radians.</param>
	/// <param name="lat">Latitude in radians.</param>
	/// <param name="ignoreTechnicalRegion">If true, empty technical regions (i.e. regions with no areas, just having mission zones) will return true.</param>
	/// <returns>True if it's inside, false if it's outside.</returns>
	public bool InsideRegion(double lon, double lat, bool ignoreTechnicalRegion = false)
	{
		if (ignoreTechnicalRegion && _lonMin.Count == 0)
			return true;

		for (int i = 0; i < _lonMin.Count; ++i)
		{
			bool inLon, inLat;

			if (_lonMin[i] <= _lonMax[i])
				inLon = lon >= _lonMin[i] && lon < _lonMax[i];
			else
				inLon = (lon >= _lonMin[i] && lon < Math.PI * 2.0) || (lon >= 0 && lon < _lonMax[i]);

			if (lat > 0) // make that both poles could be in some regions, this means `M_PI == _latMax[i]` or `-M_PI == _latMin[i]`
				inLat = lat > _latMin[i] && lat <= _latMax[i];
			else
				inLat = lat >= _latMin[i] && lat < _latMax[i];

			if (inLon && inLat)
				return true;
		}
		return false;
	}

	/// <summary>Gets the list of cities contained in this region (built from the named point areas on first use).</summary>
	public List<City> Cities
	{
		get
		{
			// Build a cached list of all mission zones that are cities
			// Saves us from constantly searching for them
			if (_cities.Count == 0)
			{
				foreach (var mz in _missionZones)
				{
					foreach (var ma in mz.areas)
					{
						if (ma.IsPoint && !string.IsNullOrEmpty(ma.name))
						{
							_cities.Add(new City(ma.name, ma.lonMin, ma.latMin));
						}
					}
				}
			}
			return _cities;
		}
	}

	/// <summary>
	/// Gets the weight of this region for mission selection.
	/// This is only used when creating a new game, since these weights change in the course of the game.
	/// </summary>
	public int Weight => unchecked((int)_regionWeight);

	/// <summary>Gets the weighted list of missions for this region.</summary>
	public WeightedOptions AvailableMissions => _missionWeights ??= new WeightedOptions();

	/// <summary>Gets the substitute mission region.</summary>
	public string MissionRegion => _missionRegion;

	/// <summary>Gets a random point that is guaranteed to be inside the given zone.</summary>
	/// <param name="zone">The target zone.</param>
	/// <param name="area">The area in the zone, or -1 for a random one.</param>
	/// <returns>A pair of longitude and latitude.</returns>
	public (double first, double second) GetRandomPoint(int zone, int area = -1)
	{
		if ((uint)zone < (uint)_missionZones.Count)
		{
			int a = area != -1 ? area : Rng.Generate(0, _missionZones[zone].areas.Count - 1);
			double lonMin = _missionZones[zone].areas[a].lonMin;
			double lonMax = _missionZones[zone].areas[a].lonMax;
			double latMin = _missionZones[zone].areas[a].latMin;
			double latMax = _missionZones[zone].areas[a].latMax;
			if (lonMin > lonMax)
			{
				lonMin = _missionZones[zone].areas[a].lonMax;
				lonMax = _missionZones[zone].areas[a].lonMin;
			}
			if (latMin > latMax)
			{
				latMin = _missionZones[zone].areas[a].latMax;
				latMax = _missionZones[zone].areas[a].latMin;
			}
			double lon = Rng.Generate(lonMin, lonMax);
			double lat = Rng.Generate(latMin, latMax);
			return (lon, lat);
		}
		Debug.Fail("Invalid zone number");
		return (0.0, 0.0);
	}

	/// <summary>Gets the maximum longitude.</summary>
	public List<double> LonMax => _lonMax;

	/// <summary>Gets the minimum longitude.</summary>
	public List<double> LonMin => _lonMin;

	/// <summary>Gets the maximum latitude.</summary>
	public List<double> LatMax => _latMax;

	/// <summary>Gets the minimum latitude.</summary>
	public List<double> LatMin => _latMin;

	/// <summary>Gets a list of all the missionZones in the region.</summary>
	public List<MissionZone> MissionZones => _missionZones;

	/// <summary>Gets the functions provided by the region.</summary>
	public RuleBaseFacilityFunctions ProvidedBaseFunc => _provideBaseFunc;

	/// <summary>Gets the functions forbidden by the region.</summary>
	public RuleBaseFacilityFunctions ForbiddenBaseFunc => _forbiddenBaseFunc;
}
