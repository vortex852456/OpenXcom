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
// Ported from: src/Savegame/Target.h, src/Savegame/Target.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Base class for targets on the globe
/// with a set of radian coordinates.
/// </summary>
public abstract class Target
{
	protected double _lon;
	protected double _lat;
	protected int _id;
	protected string _name = "";
	protected List<MovingTarget> _followers = [];

	/// <summary>Initializes a target with blank coordinates.</summary>
	protected Target()
	{
		_lon = 0.0;
		_lat = 0.0;
		_id = 0;
	}

	/// <summary>
	/// C++ destructor: makes sure no crafts are chasing this target. Call it where the C++ code deletes the target;
	/// overrides run their own cleanup first and then call the base (like the C++ destructor chain).
	/// </summary>
	public virtual void Destroy()
	{
		foreach (var follower in CraftFollowers)
		{
			follower.ReturnToBase();
		}
		foreach (var ufoFollower in UfoFollowers)
		{
			ufoFollower.ResetOriginalDestination(false);
		}
	}

	/// <summary>Loads the target from a YAML file.</summary>
	public virtual void Load(YamlNodeReader reader)
	{
		reader.TryRead("lon", ref _lon);
		reader.TryRead("lat", ref _lat);
		reader.TryRead("id", ref _id);
		reader.TryRead("name", ref _name);
	}

	/// <summary>Saves the target to a YAML file.</summary>
	public virtual void Save(YamlNodeWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.SetAsMap();
		writer.Write("lon", _lon);
		writer.Write("lat", _lat);
		if (_id != 0)
			writer.Write("id", _id);
		if (!string.IsNullOrEmpty(_name))
			writer.Write("name", _name);
	}

	/// <summary>Saves the target's unique identifiers to a YAML file.</summary>
	public virtual void SaveId(YamlNodeWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.SetAsMap();
		writer.Write("lon", _lon);
		writer.Write("lat", _lat);
		writer.Write("type", Type);
		writer.Write("id", _id);
	}

	/// <summary>Gets the target's type.</summary>
	public abstract string Type { get; }

	/// <summary>Longitude coordinate of the target in radian; the setter keeps it between 0 and 2xPI.</summary>
	public double Longitude
	{
		get => _lon;
		set
		{
			_lon = value;

			// Keep between 0 and 2xPI
			while (_lon < 0)
				_lon += 2 * Math.PI;
			while (_lon >= 2 * Math.PI)
				_lon -= 2 * Math.PI;
		}
	}

	/// <summary>Latitude coordinate of the target in radian. If you travel past a pole, continue on the other side of the globe.</summary>
	public double Latitude
	{
		get => _lat;
		set
		{
			_lat = value;
			// If you travel past a pole, continue on the other side of the globe.
			if (_lat < -Math.PI / 2)
			{
				_lat = -Math.PI - _lat;
				Longitude = _lon + Math.PI;
			}
			else if (_lat > Math.PI / 2)
			{
				_lat = Math.PI - _lat;
				Longitude = _lon - Math.PI;
			}
		}
	}

	/// <summary>Target's unique ID, 0 if none.</summary>
	public int Id
	{
		get => _id;
		set => _id = value;
	}

	/// <summary>Returns the target's user-readable name. If there's no custom name, the language default is used.</summary>
	public virtual string GetName(Language lang)
	{
		if (string.IsNullOrEmpty(_name))
			return GetDefaultName(lang);
		return _name;
	}

	/// <summary>Changes the target's custom name. If set to blank, the language default is used.</summary>
	public void SetName(string newName)
	{
		_name = newName;
	}

	/// <summary>Returns the target's unique default name.</summary>
	public virtual string GetDefaultName(Language lang)
	{
		ArgumentNullException.ThrowIfNull(lang);
		return lang.GetString(MarkerName).Arg(_id);
	}

	/// <summary>Name on the globe for the target (string ID).</summary>
	public virtual string MarkerName => Type + "_";

	/// <summary>Marker ID on the globe for the target.</summary>
	public virtual int MarkerId => _id;

	/// <summary>Gets the target's marker sprite.</summary>
	public abstract int Marker { get; }

	/// <summary>List of targets currently following this target.</summary>
	public List<MovingTarget> Followers => _followers;

	/// <summary>List of crafts currently following this target.</summary>
	public List<Craft> CraftFollowers
	{
		get
		{
			var crafts = new List<Craft>();
			foreach (var mt in _followers)
			{
				if (mt is Craft craft)
				{
					crafts.Add(craft);
				}
			}
			return crafts;
		}
	}

	/// <summary>List of UFOs currently following this target.</summary>
	public List<Ufo> UfoFollowers
	{
		get
		{
			var ufos = new List<Ufo>();
			foreach (var mt in _followers)
			{
				if (mt is Ufo ufo)
				{
					ufos.Add(ufo);
				}
			}
			return ufos;
		}
	}

	/// <summary>Gets the distance to another target.</summary>
	public double GetDistance(Target target)
	{
		ArgumentNullException.ThrowIfNull(target);
		return GetDistance(target.Longitude, target.Latitude);
	}

	/// <summary>Returns the great circle distance to another target on the globe (in radian).</summary>
	public double GetDistance(double lon, double lat)
	{
		if (AreSame(lon, _lon) && AreSame(lat, _lat))
			return 0.0;
		return Math.Acos(Math.Cos(_lat) * Math.Cos(lat) * Math.Cos(lon - _lon) + Math.Sin(_lat) * Math.Sin(lat));
	}

	/// <summary>C DBL_EPSILON (not <see cref="double.Epsilon"/>, which is the smallest denormal).</summary>
	internal const double DBL_EPSILON = 2.2204460492503131e-16;

	/// <summary>fmath.h AreSame(double, double): equal within a relative epsilon.</summary>
	internal static bool AreSame(double l, double r)
	{
		return Math.Abs(l - r) <= DBL_EPSILON * Math.Max(1.0, Math.Max(Math.Abs(l), Math.Abs(r)));
	}
}
