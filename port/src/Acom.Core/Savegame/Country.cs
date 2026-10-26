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
// Ported from: src/Savegame/Country.h, src/Savegame/Country.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents a country that funds the player.
/// Contains variable info about a country like
/// monthly funding and various activities.
/// </summary>
public partial class Country
{
	public enum Satisfaction
	{
		ALIEN_PACT = 0,
		UNHAPPY = 1,
		SATISFIED = 2,
		HAPPY = 3,
	}

	/// <summary>Name of class used in script.</summary>
	public const string ScriptName = "Country";

	private readonly RuleCountry _rules;
	private bool _pact, _newPact, _cancelPact;
	private List<int> _funding = [], _activityXcom = [], _activityAlien = [];
	private Satisfaction _satisfaction;

	/// <summary>Initializes a country of the specified type.</summary>
	/// <param name="rules">Pointer to ruleset.</param>
	/// <param name="gen">Generate new funding.</param>
	public Country(RuleCountry rules, bool gen = true)
	{
		_rules = rules;
		_pact = false;
		_newPact = false;
		_cancelPact = false;
		_satisfaction = Satisfaction.SATISFIED;
		if (gen)
		{
			_funding.Add(_rules.GenerateFunding());
		}
		_activityAlien.Add(0);
		_activityXcom.Add(0);
	}

	/// <summary>Loads the country from a YAML file.</summary>
	public void Load(YamlNodeReader reader, ScriptGlobal shared = null)
	{
		reader.TryRead("funding", ref _funding);
		reader.TryRead("activityXcom", ref _activityXcom);
		reader.TryRead("activityAlien", ref _activityAlien);
		reader.TryRead("pact", ref _pact);
		reader.TryRead("newPact", ref _newPact);
		reader.TryRead("cancelPact", ref _cancelPact);

		if (shared != null)
		{
			_scriptValues.Load(reader, shared);
		}
	}

	/// <summary>Saves the country to a YAML file.</summary>
	public void Save(YamlNodeWriter writer, ScriptGlobal shared = null)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.SetAsMap();
		writer.Write("type", _rules.Type);
		writer.Write("funding", _funding);
		writer.Write("activityXcom", _activityXcom);
		writer.Write("activityAlien", _activityAlien);
		if (_pact)
		{
			writer.Write("pact", _pact);
			if (_cancelPact)
				writer.Write("cancelPact", _cancelPact);
		}
		// Note: can have a _newPact flag, even if already has a _pact from earlier (when xcom liberates and aliens retake a country during the same month)
		if (_newPact)
			writer.Write("newPact", _newPact);

		if (shared != null)
		{
			_scriptValues.Save(writer, shared);
		}
	}

	/// <summary>Ruleset for the country's type.</summary>
	public RuleCountry Rules => _rules;

	/// <summary>The country's monthly funding (one entry per month, last = current).</summary>
	public List<int> Funding => _funding;

	/// <summary>Changes the country's current monthly funding.</summary>
	public void SetFunding(int funding)
	{
		_funding[^1] = funding;
	}

	/// <summary>Keith Richards would be so proud.</summary>
	/// <returns>Satisfaction level, 0 = alien pact, 1 = unhappy, 2 = satisfied, 3 = happy.</returns>
	public Country.Satisfaction GetSatisfaction()
	{
		if (_pact)
			return Satisfaction.ALIEN_PACT;
		return _satisfaction;
	}

	/// <summary>Adds to the country's xcom activity level.</summary>
	public void AddActivityXcom(int activity)
	{
		_activityXcom[^1] += activity;
	}

	/// <summary>Adds to the country's alien activity level.</summary>
	public void AddActivityAlien(int activity)
	{
		_activityAlien[^1] += activity;
	}

	/// <summary>The country's xcom activity level.</summary>
	public List<int> ActivityXcom => _activityXcom;

	/// <summary>The country's alien activity level.</summary>
	public List<int> ActivityAlien => _activityAlien;

	/// <summary>
	/// Reset all the counters, calculate this month's funding, set the change value for the month.
	/// </summary>
	/// <param name="xcomTotal">The council's xcom score.</param>
	/// <param name="alienTotal">The council's alien score.</param>
	/// <param name="pactScore">The penalty for signing a pact.</param>
	/// <param name="averageFunding">Current average funding across all countries (including withdrawn countries).</param>
	/// <param name="save">The saved game (script argument).</param>
	public void NewMonth(int xcomTotal, int alienTotal, int pactScore, int averageFunding, SavedGame save)
	{
		// Note: this is a TEMPORARY variable! it's not saved in the save file, i.e. we don't know the value from the previous month!
		_satisfaction = Satisfaction.SATISFIED;
		int funding = Funding[^1];
		int good = (xcomTotal / 10) + _activityXcom[^1];
		int bad = (alienTotal / 20) + _activityAlien[^1];
		int oldFunding = _funding[^1] / 1000;
		int newFunding = (oldFunding * Rng.Generate(5, 20) / 100) * 1000;
		if (newFunding == 0)
		{
			newFunding = 1000; // increase at least by 1000
		}

		if (bad <= good + 30)
		{
			if (good > bad + 30)
			{
				if (Rng.Generate(0, good) > bad)
				{
					// don't go over the cap
					int cap = Rules.FundingCap * 1000;
					if (funding + newFunding > cap)
						newFunding = cap - funding;
					if (newFunding != 0)
						_satisfaction = Satisfaction.HAPPY;
				}
			}
		}
		else
		{
			if (Rng.Generate(0, bad) > good)
			{
				if (newFunding != 0)
				{
					newFunding = -newFunding;
					// don't go below zero
					if (funding + newFunding < 0)
						newFunding = 0 - funding;
					if (newFunding != 0)
						_satisfaction = Satisfaction.UNHAPPY;
				}
			}
		}

		if (_satisfaction == Satisfaction.SATISFIED)
		{
			newFunding = 0;
		}
		if (_cancelPact)
		{
			if (oldFunding <= 0)
			{
				_satisfaction = Satisfaction.SATISFIED; // satisfied, not happy or unhappy
				newFunding = averageFunding;
			}
		}

		// call script which can adjust values.
		var args = new ScriptOutputArgs(newFunding, (int)_satisfaction, _newPact ? 1 : 0, _cancelPact ? 1 : 0);
		ModScript.Execute(Rules.GetScript(ModScript.NewMonthCountry), args, this, save, xcomTotal, alienTotal);

		newFunding = args.GetInt(0);
		_satisfaction = (Satisfaction)args.GetInt(1);
		_newPact = args.GetInt(2) != 0;
		_cancelPact = args.GetInt(3) != 0;

		// form/cancel pacts
		if (_newPact)
		{
			_pact = true;
			AddActivityAlien(pactScore);
		}
		else if (_cancelPact)
		{
			_pact = false;
		}

		// reset pact change states
		_newPact = false;
		_cancelPact = false;

		// set the new funding and reset the activity meters
		if (_pact)
			_funding.Add(0); // yes, hardcoded!
		else
			_funding.Add(funding + newFunding);

		_activityAlien.Add(0);
		_activityXcom.Add(0);
		if (_activityAlien.Count > 12)
			_activityAlien.RemoveAt(0);
		if (_activityXcom.Count > 12)
			_activityXcom.RemoveAt(0);
		if (_funding.Count > 12)
			_funding.RemoveAt(0);
	}

	/// <summary>If we will sign a new pact.</summary>
	public bool NewPact => _newPact;

	/// <summary>Sign a new pact at month's end.</summary>
	public void SetNewPact()
	{
		_newPact = true;
		_cancelPact = false;
	}

	/// <summary>If we will cancel a pact at month's end.</summary>
	public bool CancelPact => _cancelPact;

	/// <summary>Cancel or prevent a pact.</summary>
	public void SetCancelPact()
	{
		if (_pact)
		{
			// cancel an existing signed pact
			_cancelPact = true;
			_newPact = false;
		}
		else
		{
			// prevent a not-yet-signed pact
			_cancelPact = false;
			_newPact = false;
		}
	}

	/// <summary>
	/// If we have signed a pact. No setter for this one, as it gets set automatically
	/// at month's end if _newPact is set.
	/// </summary>
	public bool Pact => _pact;

	/// <summary>Sign a new pact.</summary>
	public void SetPact()
	{
		_pact = true;
	}

	/// <summary>Can be (re)infiltrated?</summary>
	public bool CanBeInfiltrated()
	{
		if (!_pact && !_newPact)
		{
			// completely new infiltration; or retaking a previously liberated country
			return true;
		}
		if (_pact && _cancelPact)
		{
			// xcom tried to liberate them this month, but the aliens were not amused... who shall they listen to at the end?
			return true;
		}
		return false;
	}
}
