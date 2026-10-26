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
// Ported from: src/Mod/RuleCountry.h, src/Mod/RuleCountry.cpp
// Port status: partial (missing: script hooks)
#nullable disable

namespace Acom;

/// <summary>
/// Represents a specific funding country.
/// Contains constant info like its location in the
/// world and starting funding range.
/// </summary>
public partial class RuleCountry
{
	/// <summary>Name of class used in script.</summary>
	public const string ScriptName = "RuleCountry";

	private readonly string _type;
	private string _signedPactEventName = "", _rejoinedXcomEventName = "";
	private int _fundingBase, _fundingCap;
	private double _labelLon, _labelLat;
	private readonly List<double> _lonMin = [], _lonMax = [], _latMin = [], _latMax = [];
	private int _labelColor, _zoomLevel;
	private RuleEvent _signedPactEvent;
	private RuleEvent _rejoinedXcomEvent;
	private RuleBaseFacilityFunctions _provideBaseFunc;
	private RuleBaseFacilityFunctions _forbiddenBaseFunc;

	/// <summary>Creates a blank ruleset for a certain type of country.</summary>
	/// <param name="type">String defining the type.</param>
	public RuleCountry(string type)
	{
		_type = type;
		_fundingBase = 0;
		_fundingCap = 0;
		_labelLon = 0.0;
		_labelLat = 0.0;
		_labelColor = 0;
		_zoomLevel = 0;
	}

	/// <summary>Loads the country type from YAML.</summary>
	/// <remarks>The C++ <c>parsers</c> argument (script containers) is left out with the script hooks.</remarks>
	public void Load(YamlNodeReader reader, Mod mod)
	{
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent, mod);
		}

		reader.TryRead("signedPactEvent", ref _signedPactEventName);
		reader.TryRead("rejoinedXcomEvent", ref _rejoinedXcomEventName);

		reader.TryRead("fundingBase", ref _fundingBase);
		reader.TryRead("fundingCap", ref _fundingCap);
		if (reader["labelLon"])
			_labelLon = Deg2Rad(reader["labelLon"].ReadVal<double>());
		if (reader["labelLat"])
			_labelLat = Deg2Rad(reader["labelLat"].ReadVal<double>());
		reader.TryRead("labelColor", ref _labelColor);
		reader.TryRead("zoomLevel", ref _zoomLevel);
		var areas = new List<List<double>>();
		reader.TryRead("areas", ref areas);
		for (int i = 0; i != areas.Count; ++i)
		{
			_lonMin.Add(Deg2Rad(areas[i][0]));
			_lonMax.Add(Deg2Rad(areas[i][1]));
			_latMin.Add(Deg2Rad(areas[i][2]));
			_latMax.Add(Deg2Rad(areas[i][3]));

			int last = _latMin.Count - 1;
			if (_latMin[last] > _latMax[last])
				(_latMin[last], _latMax[last]) = (_latMax[last], _latMin[last]);
		}
		mod.LoadBaseFunction(_type, ref _provideBaseFunc, reader["provideBaseFunc"]);
		mod.LoadBaseFunction(_type, ref _forbiddenBaseFunc, reader["forbiddenBaseFunc"]);

		LoadScripts(reader, mod.ScriptParsers);
	}

	/// <summary>Cross link with other rules.</summary>
	public void AfterLoad(Mod mod)
	{
		mod.LinkRule(ref _signedPactEvent, ref _signedPactEventName);
		mod.LinkRule(ref _rejoinedXcomEvent, ref _rejoinedXcomEventName);
	}

	/// <summary>
	/// Gets the language string that names this country.
	/// Each country type has a unique name.
	/// </summary>
	public string Type => _type;

	/// <summary>Gets the event triggered after the country leaves Xcom.</summary>
	public RuleEvent SignedPactEvent => _signedPactEvent;

	/// <summary>Gets the event triggered after the country rejoins Xcom.</summary>
	public RuleEvent RejoinedXcomEvent => _rejoinedXcomEvent;

	/// <summary>Generates the random starting funding for the country.</summary>
	/// <returns>The monthly funding.</returns>
	public int GenerateFunding() => Rng.Generate(_fundingBase, _fundingBase * 2) * 1000;

	/// <summary>Gets the country's funding cap. Country funding can never exceed this (in thousands).</summary>
	public int FundingCap => _fundingCap;

	/// <summary>Gets the longitude of the country's label on the globe (radians).</summary>
	public double LabelLongitude => _labelLon;

	/// <summary>Gets the latitude of the country's label on the globe (radians).</summary>
	public double LabelLatitude => _labelLat;

	/// <summary>Checks if a point is inside this country.</summary>
	/// <param name="lon">Longitude in radians.</param>
	/// <param name="lat">Latitude in radians.</param>
	/// <returns>True if it's inside, false if it's outside.</returns>
	public bool InsideCountry(double lon, double lat)
	{
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

	public List<double> LonMax => _lonMax;
	public List<double> LonMin => _lonMin;
	public List<double> LatMax => _latMax;
	public List<double> LatMin => _latMin;

	/// <summary>Gets the country's label color.</summary>
	public int LabelColor => _labelColor;

	/// <summary>
	/// Gets the minimum zoom level required to display the label.
	/// Note: this works for extraGlobeLabels only, not for vanilla countries.
	/// </summary>
	public int ZoomLevel => _zoomLevel;

	/// <summary>Gets the functions provided by the country.</summary>
	public RuleBaseFacilityFunctions ProvidedBaseFunc => _provideBaseFunc;

	/// <summary>Gets the functions forbidden by the country.</summary>
	public RuleBaseFacilityFunctions ForbiddenBaseFunc => _forbiddenBaseFunc;

	private static double Deg2Rad(double deg) => deg * Math.PI / 180.0; // fmath.h
}
