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
// Ported from: src/Mod/RuleStatBonus.h, src/Mod/RuleStatBonus.cpp
#nullable disable

using System.Diagnostics;

namespace Acom;

/// <summary>
/// Helper class used for storing unit stat bonuses.
/// </summary>
/// <remarks>
/// C++ turns the bonus map into an OXCE script (<c>mul bonus 1000; unit.firingBonusStats bonus c1 c2 c3 c4; ...</c>)
/// and runs it. The port compiles the same script, but evaluates it directly with the same int/float arithmetic
/// (see <see cref="Execute{T}"/>) unless a custom script or global bonus scripts (events) have to run.
/// </remarks>
public class RuleStatBonus
{
	/// <summary>Number of polynomial coefficients per stat (C++ <c>statDataFuncSize</c>).</summary>
	private const int statDataFuncSize = 4;
	/// <summary>Fixed point scale of the generated script (C++ <c>statMultiper</c>).</summary>
	private const int statMultiper = 1000;

	/// <summary>One line <c>unit.&lt;stat&gt;BonusStats bonus c1 c2 c3 c4;</c> of the generated script.</summary>
	private readonly record struct Term(int StatIndex, int Pow1, int Pow2, int Pow3, int Pow4);

	private List<(string first, List<float> second)> _bonusOrig = [];
	private bool _modded;
	private bool _refresh = true;
	/// <summary>Compiled form of the generated script (C++ <c>_container</c>).</summary>
	private Term[] _terms = [];
	/// <summary>Custom script text from the ruleset (C++ loads it into <c>_container</c>).</summary>
	private string _customScript;
	/// <summary>Compiled script (C++ <c>_container</c>), loaded when a parser is given.</summary>
	private readonly ScriptContainerEvents _container = new();

	/// <summary>Default constructor.</summary>
	public RuleStatBonus()
	{
	}

	/// <summary>Loads the bonus from YAML.</summary>
	/// <param name="parentName">Name of the owning rule (for error messages).</param>
	/// <param name="reader">Node that contains the property.</param>
	/// <param name="propertyNodeName">
	/// Name of the property (C++ <c>parser.getPropertyNodeName()</c> of the <c>ModScript::BonusStatsCommon</c> parser, e.g.
	/// <c>accuracyMultiplier</c>, <c>psiDefence</c>, <c>time</c> under <c>recovery</c>).
	/// </param>
	public void Load(string parentName, YamlNodeReader reader, string propertyNodeName) => Load(parentName, reader, propertyNodeName, null);

	/// <summary>Loads the bonus from YAML (C++ <c>load(parentName, reader, parser)</c>).</summary>
	/// <param name="parentName">Name of the owning rule (for error messages).</param>
	/// <param name="reader">Node that contains the property.</param>
	/// <param name="parser">Parser of the script, gives the property name.</param>
	public void Load(string parentName, YamlNodeReader reader, ModScript.BonusStatsBaseParser parser)
	{
		ArgumentNullException.ThrowIfNull(parser);
		Load(parentName, reader, parser.PropertyNodeName, parser);
	}

	/// <summary>Loads the bonus from YAML with <paramref name="parser"/> if given, otherwise by property name only (tests).</summary>
	public void Load(string parentName, YamlNodeReader reader, ModScript.BonusStatsBaseParser parser, string propertyNodeName)
	{
		Load(parentName, reader, parser?.PropertyNodeName ?? propertyNodeName, parser);
	}

	private void Load(string parentName, YamlNodeReader reader, string propertyNodeName, ModScript.BonusStatsBaseParser parser)
	{
		if (reader)
		{
			var stats = reader[propertyNodeName];
			if (stats)
			{
				_bonusOrig.Clear();
				if (stats.IsMap)
				{
					var statsChildren = stats.Children();
					_bonusOrig.Capacity = statsChildren.Count;
					foreach (var child in statsChildren)
						_bonusOrig.Add((child.Key, new List<float>()));
					int bonusIndex = 0;
					for (int statIndex = 0; statIndex < s_statDataMap.Length && bonusIndex < _bonusOrig.Count; ++statIndex)
					{
						for (int bonusSearchIndex = bonusIndex; bonusSearchIndex < _bonusOrig.Count; ++bonusSearchIndex)
						{
							if (s_statDataMap[statIndex].name != _bonusOrig[bonusSearchIndex].first)
								continue;
							if (bonusSearchIndex != bonusIndex)
								(_bonusOrig[bonusSearchIndex], _bonusOrig[bonusIndex]) = (_bonusOrig[bonusIndex], _bonusOrig[bonusSearchIndex]);
							var statReader = stats[_bonusOrig[bonusIndex].first];
							var vec = _bonusOrig[bonusIndex].second;
							if (statReader.HasVal)
							{
								float val = statReader.ReadVal<float>();
								vec.Add(val);
							}
							else
							{
								for (int j = 0; j < statDataFuncSize; ++j)
								{
									if (j < statReader.ChildrenCount)
									{
										float val = statReader[j].ReadVal<float>();
										vec.Add(val);
									}
								}
							}
							bonusIndex++;
							break;
						}
					}
					if (bonusIndex != _bonusOrig.Count) // one more stats were not processed
					{
						for (int errorIndex = bonusIndex; errorIndex < _bonusOrig.Count; ++errorIndex)
						{
							var term = stats[_bonusOrig[errorIndex].first];
							Logger.Error($"Unknown stat multiplier term '{_bonusOrig[errorIndex].first}' at {term.Node?.File}:{term.LocationInFile.Line}");
						}
						_bonusOrig.RemoveRange(bonusIndex, _bonusOrig.Count - bonusIndex);
					}
					_refresh = true;
				}
				else if (stats.HasVal)
				{
					_customScript = stats.ReadVal<string>();
					_terms = [];
					if (parser != null)
					{
						_container.LoadContainer(parentName, _customScript, parser);
					}
					_refresh = false;
				}
				// let's remember that this was modified by a modder (i.e. is not a default value)
				_modded = true;
			}
		}

		//convert bonus vector to script
		if (_refresh)
		{
			var terms = new Term[_bonusOrig.Count];
			for (int i = 0; i < _bonusOrig.Count; ++i)
			{
				var p = _bonusOrig[i];
				terms[i] = new Term(FindStat(parentName, p.first), Coefficient(p.second, 0), Coefficient(p.second, 1), Coefficient(p.second, 2), Coefficient(p.second, 3));
			}
			_terms = terms;
			_customScript = null;
			if (parser != null)
			{
				_container.LoadContainer(parentName, GenerateScript(), parser);
			}
			_refresh = false;
		}
	}

	/// <summary>Script generated from the bonus vector (C++ <c>load</c>).</summary>
	private string GenerateScript()
	{
		var script = new System.Text.StringBuilder(1024);

		if (_bonusOrig.Count != 0)
		{
			//scale up for rounding
			script.Append("mul bonus 1000;\n");

			foreach (var p in _bonusOrig)
			{
				script.Append("unit.");
				script.Append(p.first);
				script.Append(statNamePostfix);
				script.Append(" bonus");
				for (int j = 0; j < statDataFuncSize; ++j)
				{
					if (j < p.second.Count)
					{
						script.Append(' ');
						script.Append(Coefficient(p.second, j).ToString(System.Globalization.CultureInfo.InvariantCulture));
					}
					else
					{
						script.Append(" 0");
					}
				}
				script.Append(";\n");
			}

			//rounding to the nearest
			script.Append("if ge bonus 0; add bonus 500; else; sub bonus 500; end;\n");
			script.Append("div bonus 1000;\n");
		}
		script.Append("return bonus;");
		return script.ToString();
	}

	private const string statNamePostfix = "BonusStats";

	/// <summary>The compiled script has to run: custom script or global scripts of this bonus type.</summary>
	private bool NeedScript
	{
		get
		{
			if (_container.Parser == null)
			{
				return false;
			}
			if (_customScript != null)
			{
				return true;
			}
			var events = _container.DataEvents;
			return events != null && events.Exists(e => e.IsValid);
		}
	}

	/// <summary>Script argument <c>(int)(p.second[j] * statMultiper * 1000)</c>, all in float like C++ (float * size_t * int).</summary>
	private static int Coefficient(List<float> values, int j)
	{
		if (j < values.Count)
		{
			return (int)(float)((float)(values[j] * (float)statMultiper) * 1000f);
		}
		return 0;
	}

	private static int FindStat(string parentName, string name)
	{
		for (int i = 0; i < s_statDataMap.Length; ++i)
		{
			if (s_statDataMap[i].name == name)
				return i;
		}
		// C++: the generated script would not compile
		throw new InvalidOperationException($"Error in stat bonus of '{parentName}': unknown stat '{name}'");
	}

	/// <summary>Set new values of bonus vector.</summary>
	private void SetValues(List<(string first, List<float> second)> bonuses)
	{
		_bonusOrig = bonuses;
		_refresh = true;
	}

	/// <summary>Set default bonus for firing accuracy.</summary>
	public void SetFiring() => SetValues([("firing", [1.0f])]);

	/// <summary>Set default bonus for melee accuracy.</summary>
	public void SetMelee() => SetValues([("melee", [1.0f])]);

	/// <summary>Set default bonus for throwing accuracy.</summary>
	public void SetThrowing() => SetValues([("throwing", [1.0f])]);

	/// <summary>Set default bonus for close quarters combat.</summary>
	public void SetCloseQuarters() => SetValues([("melee", [0.5f]), ("reactions", [0.5f])]);

	/// <summary>Set default bonus for psi attack accuracy.</summary>
	public void SetPsiAttack() => SetValues([("psi", [0.02f])]);

	/// <summary>Set default bonus for psi defense.</summary>
	public void SetPsiDefense() => SetValues([("psiStrength", [1.0f]), ("psiSkill", [0.2f])]);

	/// <summary>Set default bonus for melee power.</summary>
	public void SetStrength() => SetValues([("strength", [1.0f])]);

	/// <summary>Set flat bonus equal 100% hit chance.</summary>
	public void SetFlatHundred() => SetValues([("flatHundred", [1.0f])]);

	/// <summary>Set default for TU recovery.</summary>
	public void SetTimeRecovery() => SetValues([("tu", [1.0f])]);

	/// <summary>Set default for Energy recovery.</summary>
	public void SetEnergyRecovery() => SetValues([("energyRegen", [1.0f])]);

	/// <summary>Set default for Stun recovery.</summary>
	public void SetStunRecovery() => SetValues([("flatOne", [1.0f])]);

	/// <summary>Calculate bonus based on attack unit and weapons.</summary>
	public int GetBonus(BattleActionAttack attack, int externalBonuses = 0)
	{
		ArgumentNullException.ThrowIfNull(attack);
		Debug.Assert(!_refresh, "RuleStatBonus not loaded correctly");
		if (NeedScript)
		{
			var arg = new ScriptOutputArgs(externalBonuses);
			ModScript.Execute(_container, arg, attack.attacker, externalBonuses, attack.weapon_item, attack.damage_item, (int)attack.type, attack.skill_rules);
			return arg.GetFirst();
		}
		if (_customScript != null)
		{
			return externalBonuses; // never loaded with a parser
		}
		return Execute(attack.attacker, externalBonuses, s_unitStat);
	}

	/// <summary>Calculate bonus based on unit stats.</summary>
	public int GetBonus(BattleUnit unit, int externalBonuses = 0)
	{
		Debug.Assert(!_refresh, "RuleStatBonus not loaded correctly");
		if (NeedScript)
		{
			var arg = new ScriptOutputArgs(externalBonuses);
			ModScript.Execute(_container, arg, unit, externalBonuses, null, null, (int)BattleActionType.BA_NONE, null);
			return arg.GetFirst();
		}
		if (_customScript != null)
		{
			return externalBonuses; // never loaded with a parser
		}
		return Execute(unit, externalBonuses, s_unitStat);
	}

	private static readonly Func<BattleUnit, int, float> s_unitStat = static (unit, statIndex) => s_statDataMap[statIndex].func(unit);

	/// <summary>
	/// Runs the generated script: the output starts as <paramref name="externalBonuses"/>, each term adds
	/// <c>polynomial(stat) / 1000</c> (C++ <c>getBonusStatsScript::func</c>, skipped without a unit), then the result is rounded back.
	/// </summary>
	/// <param name="unit">Unit (or a test stand-in).</param>
	/// <param name="externalBonuses">Initial value of the script output.</param>
	/// <param name="stat">Value of stat <c>statIndex</c> (index into the stat table) for the unit.</param>
	internal int Execute<T>(T unit, int externalBonuses, Func<T, int, float> stat) where T : class
	{
		int bonus = externalBonuses;
		if (_terms.Length == 0)
		{
			return bonus; // "return bonus;"
		}
		//scale up for rounding
		bonus *= 1000;
		foreach (var term in _terms)
		{
			if (unit != null)
			{
				float s = stat(unit, term.StatIndex);
				float b = 0;
				b += term.Pow4; b *= s;
				b += term.Pow3; b *= s;
				b += term.Pow2; b *= s;
				b += term.Pow1; b *= s;
				bonus = (int)(bonus + b / statMultiper); // int& ret += float
			}
		}
		//rounding to the nearest
		if (bonus >= 0) bonus += 500; else bonus -= 500;
		bonus /= 1000;
		return bonus;
	}

	/// <summary>Index of a stat name in the stat table, or -1 (for tests).</summary>
	internal static int StatIndex(string name) => Array.FindIndex(s_statDataMap, s => s.name == name);

	/// <summary>Used for "Stats for Nerds".</summary>
	public List<(string first, List<float> second)> BonusRaw { get => _bonusOrig; }

	public bool IsModded { get => _modded; }

	public void SetModded(bool modded) => _modded = modded;

	// Stat getters (C++ stat0/stat1/stat2 templates and current*/normalized* functions).

	private static Func<BattleUnit, float> Stat0(int i) => _ => i;

	private static Func<BattleUnit, float> Stat1(UnitStats.Ptr field, int divisor = 1)
	{
		float scale = 1.0f / divisor;
		return unit =>
		{
			var stat = unit.BaseStats;
			return stat[field] * scale;
		};
	}

	private static Func<BattleUnit, float> Stat2(UnitStats.Ptr fieldA, UnitStats.Ptr fieldB, int divisor = 1)
	{
		float scale = 1.0f / divisor;
		return unit =>
		{
			var stat = unit.BaseStats;
			return stat[fieldA] * stat[fieldB] * scale;
		};
	}

	private static float NormalizedHelper(int val, int max) => max > 0 ? 1.0f * val / max : 0.0f;

	private static float BasicEnergyRegeneration(BattleUnit unit)
	{
		Soldier solder = unit.GeoscapeSoldier;
		if (solder != null)
		{
			return solder.InitStats.tu / 3;
		}
		else
		{
			return unit.UnitRules.EnergyRecovery;
		}
	}

	/// <summary>
	/// Register stat functions <c>unit.&lt;stat&gt;BonusStats</c> (C++ <c>BonusStatsBaseParser</c> constructor, <c>getBonusStatsScript</c>):
	/// <c>(const BattleUnit*, int&amp;, int, int, int, int)</c>.
	/// </summary>
	internal static void ScriptRegisterStats(Bind<BattleUnit> bu)
	{
		foreach (var (name, func) in s_statDataMap)
		{
			bu.AddFunc(name + statNamePostfix, [Bind<BattleUnit>.Self, ScriptParam.IntRef, ScriptParam.Int, ScriptParam.Int, ScriptParam.Int, ScriptParam.Int], (ref ScriptArgs a) =>
			{
				var unit = a.Ptr<BattleUnit>(0);
				if (unit != null)
				{
					float stat = func(unit);
					float bonus = 0;
					bonus += a.Int(5); bonus *= stat;
					bonus += a.Int(4); bonus *= stat;
					bonus += a.Int(3); bonus *= stat;
					bonus += a.Int(2); bonus *= stat;
					ref int ret = ref a.IntRef(1);
					ret = (int)(ret + bonus / statMultiper);
				}
				return ScriptArg.RetContinue;
			}, "add stat '" + name + "' transformed by polynomial (const arguments are coefficients), final result of polynomial is divided by " + statMultiper);
		}
	}

	/// <summary>List of all possible getters of basic stats (C++ <c>statDataMap</c>); the order decides the term order.</summary>
	private static readonly (string name, Func<BattleUnit, float> func)[] s_statDataMap =
	[
		("flatOne", Stat0(1)),
		("flatHundred", Stat0(100)),

		("strength", Stat1(UnitStats.Ptr.strength)),
		("psi", Stat2(UnitStats.Ptr.psiSkill, UnitStats.Ptr.psiStrength)),
		("psiSkill", Stat1(UnitStats.Ptr.psiSkill)),
		("psiStrength", Stat1(UnitStats.Ptr.psiStrength)),
		("throwing", Stat1(UnitStats.Ptr.throwing)),
		("bravery", Stat1(UnitStats.Ptr.bravery)),
		("firing", Stat1(UnitStats.Ptr.firing)),
		("health", Stat1(UnitStats.Ptr.health)),
		("mana", Stat1(UnitStats.Ptr.mana)),
		("tu", Stat1(UnitStats.Ptr.tu)),
		("reactions", Stat1(UnitStats.Ptr.reactions)),
		("stamina", Stat1(UnitStats.Ptr.stamina)),
		("melee", Stat1(UnitStats.Ptr.melee)),
		("strengthMelee", Stat2(UnitStats.Ptr.strength, UnitStats.Ptr.melee)),
		("strengthThrowing", Stat2(UnitStats.Ptr.strength, UnitStats.Ptr.throwing)),
		("firingReactions", Stat2(UnitStats.Ptr.firing, UnitStats.Ptr.reactions)),

		("strengthScaled", Stat1(UnitStats.Ptr.strength, 100)),
		("psiScaled", Stat2(UnitStats.Ptr.psiSkill, UnitStats.Ptr.psiStrength, 10000)),
		("psiSkillScaled", Stat1(UnitStats.Ptr.psiSkill, 100)),
		("psiStrengthScaled", Stat1(UnitStats.Ptr.psiStrength, 100)),
		("throwingScaled", Stat1(UnitStats.Ptr.throwing, 100)),
		("braveryScaled", Stat1(UnitStats.Ptr.bravery, 100)),
		("firingScaled", Stat1(UnitStats.Ptr.firing, 100)),
		("healthScaled", Stat1(UnitStats.Ptr.health, 100)),
		("manaScaled", Stat1(UnitStats.Ptr.mana, 100)),
		("tuScaled", Stat1(UnitStats.Ptr.tu, 100)),
		("reactionsScaled", Stat1(UnitStats.Ptr.reactions, 100)),
		("staminaScaled", Stat1(UnitStats.Ptr.stamina, 100)),
		("meleeScaled", Stat1(UnitStats.Ptr.melee, 100)),
		("strengthMeleeScaled", Stat2(UnitStats.Ptr.strength, UnitStats.Ptr.melee, 10000)),
		("strengthThrowingScaled", Stat2(UnitStats.Ptr.strength, UnitStats.Ptr.throwing, 10000)),
		("firingReactionsScaled", Stat2(UnitStats.Ptr.firing, UnitStats.Ptr.reactions, 10000)),

		("rank", unit => unit.RankInt),
		("rankUnified", unit => unit.RankIntUnified),
		("fatalWounds", unit => unit.FatalWounds),

		("healthCurrent", unit => unit.Health),
		("manaCurrent", unit => unit.Mana),
		("tuCurrent", unit => unit.TimeUnits),
		("energyCurrent", unit => unit.Energy),
		("moraleCurrent", unit => unit.Morale),
		("stunCurrent", unit => unit.Stunlevel),

		("healthNormalized", unit => NormalizedHelper(unit.Health, unit.BaseStats.health)),
		("manaNormalized", unit => NormalizedHelper(unit.Mana, unit.BaseStats.mana)),
		("tuNormalized", unit => NormalizedHelper(unit.TimeUnits, unit.BaseStats.tu)),
		("energyNormalized", unit => NormalizedHelper(unit.Energy, unit.BaseStats.stamina)),
		("moraleNormalized", unit => NormalizedHelper(unit.Morale, 100)),
		("stunNormalized", unit => NormalizedHelper(unit.Stunlevel, unit.Health)),

		("energyRegen", BasicEnergyRegeneration),
	];
}
