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
// Ported from: src/Savegame/BaseFacility.h, src/Savegame/BaseFacility.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents a base facility placed in a base.
/// Contains variable info about a facility like
/// position and build time.
/// </summary>
public class BaseFacility
{
	private readonly RuleBaseFacility _rules;
	private readonly Base _base;
	private int _x, _y, _buildTime;
	private int _ammo;
	private bool _ammoMissingReported;
	private bool _disabled;
	private Craft _craftForDrawing; // craft, used for drawing facility
	private bool _hadPreviousFacility;

	/// <summary>Initializes a base facility of the specified type.</summary>
	/// <param name="rules">Pointer to ruleset.</param>
	/// <param name="base">Pointer to base of origin.</param>
	public BaseFacility(RuleBaseFacility rules, Base @base)
	{
		_rules = rules;
		_base = @base;
		_x = -1;
		_y = -1;
		_buildTime = 0;
		_ammo = 0;
		_ammoMissingReported = false;
		_disabled = false;
		_craftForDrawing = null;
		_hadPreviousFacility = false;
	}

	/// <summary>Loads the base facility from YAML.</summary>
	public void Load(YamlNodeReader reader)
	{
		reader.TryRead("x", ref _x);
		reader.TryRead("y", ref _y);
		reader.TryRead("buildTime", ref _buildTime);
		reader.TryRead("ammo", ref _ammo);
		reader.TryRead("ammoMissingReported", ref _ammoMissingReported);
		reader.TryRead("disabled", ref _disabled);
		reader.TryRead("hadPreviousFacility", ref _hadPreviousFacility);
	}

	/// <summary>Saves the base facility to YAML.</summary>
	public void Save(YamlNodeWriter writer)
	{
		writer.SetAsMap();
		writer.Write("type", _rules.Type);
		writer.Write("x", _x);
		writer.Write("y", _y);
		if (_buildTime != 0)
			writer.Write("buildTime", _buildTime);
		if (_ammo != 0)
			writer.Write("ammo", _ammo);
		if (_ammoMissingReported)
			writer.Write("ammoMissingReported", _ammoMissingReported);
		if (_disabled)
			writer.Write("disabled", _disabled);
		if (_hadPreviousFacility)
			writer.Write("hadPreviousFacility", _hadPreviousFacility);
	}

	/// <summary>Returns the ruleset for the base facility's type.</summary>
	public RuleBaseFacility Rules => _rules;

	/// <summary>The base facility's X position on the base grid that it's placed on (grid squares).</summary>
	public int X
	{
		get => _x;
		set => _x = value;
	}

	/// <summary>The base facility's Y position on the base grid that it's placed on (grid squares).</summary>
	public int Y
	{
		get => _y;
		set => _y = value;
	}

	/// <summary>Get placement of facility in base.</summary>
	public BaseAreaSubset Placement
	{
		get
		{
			int sizeX = _rules.SizeX;
			int sizeY = _rules.SizeY;
			return new BaseAreaSubset(sizeX, sizeY).Offset(_x, _y);
		}
	}

	/// <summary>The base facility's remaining time until it's finished building (0 = complete), in days.</summary>
	public int BuildTime
	{
		get => _buildTime;
		set => _buildTime = value;
	}

	/// <summary>
	/// Returns the base facility's remaining time until it's finished building (0 = complete).
	/// Facility upgrades and downgrades are ignored in this calculation.
	/// </summary>
	public int AdjustedBuildTime
	{
		get
		{
			if (_hadPreviousFacility)
				return 0;

			return _buildTime;
		}
	}

	/// <summary>Handles the facility building every day.</summary>
	public void Build()
	{
		_buildTime--;
		if (_buildTime == 0)
			_hadPreviousFacility = false;
	}

	/// <summary>Returns if this facility is currently being used by its base.</summary>
	public BasePlacementErrors InUse()
	{
		if (_buildTime > 0)
		{
			return BasePlacementErrors.BPE_None;
		}

		return _base.IsAreaInUse(Placement);
	}

	/// <summary>The facility's ammo count.</summary>
	public int Ammo
	{
		get => _ammo;
		set => _ammo = value;
	}

	/// <summary>Resets the helper flag.</summary>
	public void ResetAmmoMissingReported() => _ammoMissingReported = false;

	/// <summary>Rearms the facility.</summary>
	/// <returns>The ammo item missing for rearming, or null if none or reported already.</returns>
	public RuleItem Rearm()
	{
		// facility doesn't need to be rearmed at all (vanilla)
		if (_rules.AmmoMax <= 0)
			return null;

		// facility is not operational
		if (_buildTime > 0)
			return null;

		// facility is already fully armed
		if (_ammo >= _rules.AmmoMax)
		{
			ResetAmmoMissingReported();
			return null;
		}

		int ammoMissing = _rules.AmmoMax - _ammo;
		int ammoUsed = Math.Min(ammoMissing, _rules.RearmRate);

		RuleItem ammoItem = null;
		if (_rules.AmmoItem != null)
		{
			int ammoAvailable = _base.StorageItems.GetItem(_rules.AmmoItem);
			if (ammoAvailable < ammoUsed)
			{
				if (!_ammoMissingReported)
				{
					ammoItem = _rules.AmmoItem;
					_ammoMissingReported = true;
				}
				ammoUsed = ammoAvailable;
			}
			_base.StorageItems.RemoveItem(_rules.AmmoItem, ammoUsed);
		}

		_ammo += ammoUsed;

		return ammoItem;
	}

	/// <summary>The facility's disabled flag.</summary>
	public bool Disabled
	{
		get => _disabled;
		set => _disabled = value;
	}

	/// <summary>Craft, used for drawing facility (hangar).</summary>
	public Craft CraftForDrawing
	{
		get => _craftForDrawing;
		set => _craftForDrawing = value;
	}

	/// <summary>Whether this facility was placed over another or was placed by removing another.</summary>
	public bool IfHadPreviousFacility
	{
		get => _hadPreviousFacility;
		set => _hadPreviousFacility = value;
	}

	/// <summary>
	/// Is the facility fully built or being upgraded/downgraded?
	/// Used for determining if this facility should count for base connectivity.
	/// </summary>
	public bool IsBuiltOrHadPreviousFacility => _buildTime == 0 || _hadPreviousFacility;
}
