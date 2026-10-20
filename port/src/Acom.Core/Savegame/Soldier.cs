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
// Ported from: src/Savegame/Soldier.h, src/Savegame/Soldier.cpp
// Port status: partial (missing: script hooks)
#nullable disable

using System.Globalization;

namespace Acom;

public enum SoldierRank : byte { RANK_ROOKIE, RANK_SQUADDIE, RANK_SERGEANT, RANK_CAPTAIN, RANK_COLONEL, RANK_COMMANDER }

public enum SoldierGender : byte { GENDER_MALE, GENDER_FEMALE }

public enum SoldierLook : byte { LOOK_BLONDE, LOOK_BROWNHAIR, LOOK_ORIENTAL, LOOK_AFRICAN }

/// <summary>
/// Represents a soldier hired by the player.
/// Soldiers have a wide variety of stats that affect
/// their performance during battles.
/// </summary>
public class Soldier
{
	/// <summary>Name of class used in script.</summary>
	public const string ScriptName = "GeoscapeSoldier";

	private string _name = "";
	private string _callsign = "";
	private int _id, _nationality, _improvement, _psiStrImprovement;
	private RuleSoldier _rules;
	private UnitStats _initialStats, _currentStats, _tmpStatsWithSoldierBonuses, _tmpStatsWithAllBonuses;
	private UnitStats _dailyDogfightExperienceCache;
	private SoldierRank _rank;
	private Craft _craft;
	private SoldierGender _gender;
	private SoldierLook _look;
	private int _lookVariant;
	private int _missions, _kills, _stuns;
	private int _healthMissing; // amount of health missing until full health recovery, this is less serious than wound recovery.
	private int _manaMissing;   // amount of mana missing until full mana recovery
	private float _recovery;    // amount of hospital attention soldier needs... used to calculate recovery time
	private bool _recentlyPromoted, _psiTraining, _training, _returnToTrainingWhenHealed;
	private Armor _armor;
	private Armor _replacedArmor;
	private Armor _transformedArmor;
	private readonly List<EquipmentLayoutItem> _equipmentLayout = [];           // last used equipment layout, managed by the game
	private readonly List<EquipmentLayoutItem> _personalEquipmentLayout = [];   // personal equipment layout, managed by the player
	private Armor _personalEquipmentArmor;
	private SoldierDeath _death;
	private SoldierDiary _diary;
	private string _statString = "";
	private bool _corpseRecovered;
	private SortedDictionary<string, int> _previousTransformations = new(StringComparer.Ordinal);
	private SortedDictionary<string, int> _transformationBonuses = new(StringComparer.Ordinal);
	private readonly List<RuleSoldierBonus> _bonusCache = [];

	/// <summary>Initializes a new soldier, either blank or randomly generated.</summary>
	/// <param name="rules">Soldier ruleset.</param>
	/// <param name="armor">Soldier armor.</param>
	/// <param name="nationality">Nationality (-1 = by name pool weights).</param>
	/// <param name="id">Unique soldier id for soldier generation.</param>
	public Soldier(RuleSoldier rules, Armor armor, int nationality, int id = 0)
	{
		_id = id;
		_nationality = 0;
		_improvement = 0;
		_psiStrImprovement = 0;
		_rules = rules;
		_rank = SoldierRank.RANK_ROOKIE;
		_craft = null;
		_gender = SoldierGender.GENDER_MALE;
		_look = SoldierLook.LOOK_BLONDE;
		_lookVariant = 0;
		_armor = armor;
		_diary = new SoldierDiary();

		if (id != 0)
		{
			UnitStats minStats = rules.MinStats;
			UnitStats maxStats = rules.MaxStats;

			_initialStats.tu = (short)Rng.Generate(minStats.tu, maxStats.tu);
			_initialStats.stamina = (short)Rng.Generate(minStats.stamina, maxStats.stamina);
			_initialStats.health = (short)Rng.Generate(minStats.health, maxStats.health);
			_initialStats.mana = (short)Rng.Generate(minStats.mana, maxStats.mana);
			_initialStats.bravery = (short)(Rng.Generate(minStats.bravery / 10, maxStats.bravery / 10) * 10);
			_initialStats.reactions = (short)Rng.Generate(minStats.reactions, maxStats.reactions);
			_initialStats.firing = (short)Rng.Generate(minStats.firing, maxStats.firing);
			_initialStats.throwing = (short)Rng.Generate(minStats.throwing, maxStats.throwing);
			_initialStats.strength = (short)Rng.Generate(minStats.strength, maxStats.strength);
			_initialStats.psiStrength = (short)Rng.Generate(minStats.psiStrength, maxStats.psiStrength);
			_initialStats.melee = (short)Rng.Generate(minStats.melee, maxStats.melee);
			_initialStats.psiSkill = minStats.psiSkill;

			_currentStats = _initialStats;

			List<SoldierNamePool> names = rules.Names;
			if (names.Count != 0)
			{
				if (nationality > -1)
				{
					// nationality by location, or hardcoded/technical nationality
					_nationality = nationality;
				}
				else
				{
					// nationality by name pool weights
					int tmp = Rng.Generate(1, rules.TotalSoldierNamePoolWeight);
					int nat = 0;
					foreach (var namepool in names)
					{
						if (tmp <= namepool.GlobalWeight)
						{
							break;
						}
						tmp -= namepool.GlobalWeight;
						++nat;
					}
					_nationality = nat;
				}
				if ((uint)_nationality >= (uint)names.Count)
				{
					// handling weird cases, e.g. corner cases in soldier transformations
					_nationality = Rng.Generate(0, names.Count - 1);
				}
				_name = names[_nationality].GenName(ref _gender, rules.FemaleFrequency);
				_callsign = GenerateCallsign(rules.Names);
				_look = (SoldierLook)names[_nationality].GenLook(4); // Once we add the ability to mod in extra looks, this will need to reference the ruleset for the maximum amount of looks.
			}
			else
			{
				// No possible names, just wing it
				_gender = Rng.Percent(rules.FemaleFrequency) ? SoldierGender.GENDER_FEMALE : SoldierGender.GENDER_MALE;
				_look = (SoldierLook)Rng.Generate(0, 3);
				_name = (_gender == SoldierGender.GENDER_FEMALE) ? "Jane" : "John";
				_name += " Doe";
				_callsign = "";
			}
		}
		_lookVariant = Rng.Seedless(0, RuleSoldier.LookVariantMax - 1);
	}

	/// <summary>Loads the soldier from a YAML file.</summary>
	/// <param name="node">YAML node.</param>
	/// <param name="mod">Game mod.</param>
	/// <param name="save">Pointer to savegame.</param>
	/// <param name="soldierTemplate">Is this a soldier template (stats are merged, the id is not read)?</param>
	/// <remarks>C++ also takes the script globals (<c>ScriptGlobal*</c>); script values are not ported.</remarks>
	public void Load(YamlNodeReader node, Mod mod, SavedGame save, bool soldierTemplate = false)
	{
		var reader = node.UseIndex();

		if (!soldierTemplate)
			reader.TryRead("id", ref _id);
		reader.TryRead("name", ref _name);
		reader.TryRead("callsign", ref _callsign);
		reader.TryRead("nationality", ref _nationality);
		if (soldierTemplate)
		{
			UnitStats ii = default, cc = default;
			reader.TryRead("initialStats", ref ii);
			reader.TryRead("currentStats", ref cc);
			_initialStats = UnitStats.TemplateMerge(_initialStats, ii);
			_currentStats = UnitStats.TemplateMerge(_currentStats, cc);
		}
		else
		{
			reader.TryRead("initialStats", ref _initialStats);
			reader.TryRead("currentStats", ref _currentStats);
		}
		reader.TryRead("dailyDogfightExperienceCache", ref _dailyDogfightExperienceCache);

		// re-roll mana stats when upgrading saves
		if (_currentStats.mana == 0 && _rules.MaxStats.mana > 0)
		{
			int reroll = Rng.Generate(_rules.MinStats.mana, _rules.MaxStats.mana);
			_currentStats.mana = (short)reroll;
			_initialStats.mana = (short)reroll;
		}

		reader.TryRead("rank", ref _rank);
		reader.TryRead("gender", ref _gender);
		reader.TryRead("look", ref _look);
		reader.TryRead("lookVariant", ref _lookVariant);
		reader.TryRead("missions", ref _missions);
		reader.TryRead("kills", ref _kills);
		reader.TryRead("stuns", ref _stuns);
		reader.TryRead("manaMissing", ref _manaMissing);
		reader.TryRead("healthMissing", ref _healthMissing);
		reader.TryRead("recovery", ref _recovery);
		Armor armor = _armor;
		if (reader["armor"])
			armor = mod.GetArmor(reader["armor"].ReadVal<string>());
		if (armor == null)
			armor = mod.GetSoldier(mod.SoldiersList[0]).DefaultArmor;
		_armor = armor;
		if (reader["replacedArmor"])
			_replacedArmor = mod.GetArmor(reader["replacedArmor"].ReadVal<string>());
		if (reader["transformedArmor"])
			_transformedArmor = mod.GetArmor(reader["transformedArmor"].ReadVal<string>());
		reader.TryRead("psiTraining", ref _psiTraining);
		reader.TryRead("training", ref _training);
		reader.TryRead("returnToTrainingWhenHealed", ref _returnToTrainingWhenHealed);

		reader.TryRead("improvement", ref _improvement);
		reader.TryRead("psiStrImprovement", ref _psiStrImprovement);
		foreach (var layoutItem in reader["equipmentLayout"].Children())
		{
			try
			{
				_equipmentLayout.Add(new EquipmentLayoutItem(layoutItem, mod));
			}
			catch (Exception ex) when (ex is InvalidOperationException or YamlReadException)
			{
				Logger.Error($"Error loading Layout: {ex.Message}");
			}
		}
		foreach (var personalLayoutItem in reader["personalEquipmentLayout"].Children())
		{
			try
			{
				_personalEquipmentLayout.Add(new EquipmentLayoutItem(personalLayoutItem, mod));
			}
			catch (Exception ex) when (ex is InvalidOperationException or YamlReadException)
			{
				Logger.Error($"Error loading Layout: {ex.Message}");
			}
		}
		if (reader["personalEquipmentArmor"])
		{
			_personalEquipmentArmor = mod.GetArmor(reader["personalEquipmentArmor"].ReadVal<string>());
		}
		if (reader["death"])
		{
			_death = new SoldierDeath();
			_death.Load(reader["death"]);
		}
		if (reader["diary"])
		{
			_diary = new SoldierDiary();
			_diary.Load(reader["diary"], mod);
		}
		CalcStatString(mod.StatStrings, Options.psiStrengthEval && save.IsResearched(mod.PsiRequirements));
		reader.TryRead("corpseRecovered", ref _corpseRecovered);
		ReadOrdinalMap(reader, "previousTransformations", ref _previousTransformations);
		ReadOrdinalMap(reader, "transformationBonuses", ref _transformationBonuses);

		var spawnInfo = reader["randomTransformationBonuses"];
		if (spawnInfo)
		{
			var randomTransformationBonuses = new WeightedOptions();
			randomTransformationBonuses.Load(spawnInfo);
			int transformationBonusesCount = reader["transformationBonusesCount"].ReadVal(1); // if not provided, default is 1
			while (transformationBonusesCount > 0 && !randomTransformationBonuses.Empty())
			{
				transformationBonusesCount--;
				string chosen = randomTransformationBonuses.Choose();
				randomTransformationBonuses.Set(chosen, 0);

				// Award a soldier bonus, if defined
				if (!Mod.IsEmptyRuleName(chosen))
				{
					if (_transformationBonuses.TryGetValue(chosen, out int count))
					{
						_transformationBonuses[chosen] = count + 1;
					}
					else
					{
						_transformationBonuses[chosen] = 1;
					}
				}
			}
		}
	}

	/// <summary>Reads a std::map&lt;std::string, int&gt;, keeping the C++ (ordinal) key order.</summary>
	private static void ReadOrdinalMap(YamlNodeReader reader, string key, ref SortedDictionary<string, int> map)
	{
		SortedDictionary<string, int> tmp = null;
		if (reader.TryRead(key, ref tmp))
			map = new SortedDictionary<string, int>(tmp, StringComparer.Ordinal);
	}

	/// <summary>Saves the soldier to a YAML file.</summary>
	/// <remarks>C++ also takes the script globals (<c>ScriptGlobal*</c>); script values are not ported.</remarks>
	public void Save(YamlNodeWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.SetAsMap();

		writer.Write("type", _rules.Type);
		writer.Write("id", _id);
		writer.Write("name", _name);
		if (_callsign.Length != 0)
			writer.Write("callsign", _callsign);
		writer.Write("nationality", _nationality);
		writer.Write("initialStats", _initialStats);
		writer.Write("currentStats", _currentStats);
		if (_dailyDogfightExperienceCache.firing > 0 || _dailyDogfightExperienceCache.reactions > 0 || _dailyDogfightExperienceCache.bravery > 0)
			writer.Write("dailyDogfightExperienceCache", _dailyDogfightExperienceCache);
		writer.Write("rank", _rank);
		if (_craft != null)
			_craft.SaveId(writer["craft"]);
		writer.Write("gender", _gender);
		writer.Write("look", _look);
		writer.Write("lookVariant", _lookVariant);
		writer.Write("missions", _missions);
		writer.Write("kills", _kills);
		writer.Write("stuns", _stuns);
		if (_manaMissing > 0)
			writer.Write("manaMissing", _manaMissing);
		if (_healthMissing > 0)
			writer.Write("healthMissing", _healthMissing);
		if (_recovery > 0.0f)
			writer.Write("recovery", _recovery);
		writer.Write("armor", _armor.Type);
		if (_replacedArmor != null)
			writer.Write("replacedArmor", _replacedArmor.Type);
		if (_transformedArmor != null)
			writer.Write("transformedArmor", _transformedArmor.Type);
		if (_psiTraining)
			writer.Write("psiTraining", _psiTraining);
		if (_training)
			writer.Write("training", _training);
		if (_returnToTrainingWhenHealed)
			writer.Write("returnToTrainingWhenHealed", _returnToTrainingWhenHealed);
		writer.Write("improvement", _improvement);
		writer.Write("psiStrImprovement", _psiStrImprovement);
		writer.Write("equipmentLayout", _equipmentLayout, (w, i) => i.Save(w.Write()));
		writer.Write("personalEquipmentLayout", _personalEquipmentLayout, (w, i) => i.Save(w.Write()));
		if (_personalEquipmentArmor != null)
			writer.Write("personalEquipmentArmor", _personalEquipmentArmor.Type);
		if (_death != null)
			_death.Save(writer["death"]);
		if (Options.soldierDiaries && (_diary.MissionIdList.Count != 0 || _diary.SoldierCommendations.Count != 0 || _diary.MonthsService > 0))
			_diary.Save(writer["diary"]);
		if (_corpseRecovered)
			writer.Write("corpseRecovered", _corpseRecovered);
		if (_previousTransformations.Count != 0)
			writer.Write("previousTransformations", _previousTransformations);
		if (_transformationBonuses.Count != 0)
			writer.Write("transformationBonuses", _transformationBonuses);
	}

	/// <summary>Number of Unicode code points (C++ Unicode::codePointLengthUTF8).</summary>
	private static int CodePointLength(string str)
	{
		int size = 0;
		for (int i = 0; i < str.Length; ++i)
		{
			if (char.IsHighSurrogate(str[i]) && i + 1 < str.Length && char.IsLowSurrogate(str[i + 1]))
				++i;
			++size;
		}
		return size;
	}

	/// <summary>The first <paramref name="count"/> code points (C++ Unicode::codePointSubstrUTF8(str, 0, count)).</summary>
	private static string CodePointPrefix(string str, long count)
	{
		int end = 0;
		for (long curr = 0; end < str.Length && curr < count; ++curr)
		{
			if (char.IsHighSurrogate(str[end]) && end + 1 < str.Length && char.IsLowSurrogate(str[end + 1]))
				++end;
			++end;
		}
		return str[..end];
	}

	/// <summary>Returns the soldier's full name (and, optionally, statString).</summary>
	/// <param name="statstring">Add stat string?</param>
	/// <param name="maxLength">Restrict length to a certain value.</param>
	public string GetName(bool statstring = false, uint maxLength = 20)
	{
		if (statstring && _statString.Length != 0)
		{
			int nameCodePointLength = CodePointLength(_name);
			int statCodePointLength = CodePointLength(_statString);
			if (nameCodePointLength + statCodePointLength > maxLength)
			{
				// C++ computes the count unsigned: a stat string longer than maxLength wraps around and keeps the whole name
				long count = maxLength - (long)statCodePointLength;
				return CodePointPrefix(_name, count < 0 ? long.MaxValue : count) + "/" + _statString;
			}
			else
			{
				return _name + "/" + _statString;
			}
		}
		else
		{
			return _name;
		}
	}

	/// <summary>Changes the soldier's full name.</summary>
	public void SetName(string name)
	{
		_name = name;
	}

	/// <summary>Generates a new name based on nationality.</summary>
	public void GenName()
	{
		List<SoldierNamePool> names = _rules.Names;
		if (names.Count != 0)
		{
			// clamp (and randomize) nationality if needed (i.e. if the modder messed up)
			if ((uint)_nationality >= (uint)names.Count)
			{
				_nationality = Rng.Generate(0, names.Count - 1);
			}
			_name = names[_nationality].GenName(ref _gender, _rules.FemaleFrequency);
			_callsign = GenerateCallsign(_rules.Names);
			_look = (SoldierLook)names[_nationality].GenLook(4); // Once we add the ability to mod in extra looks, this will need to reference the ruleset for the maximum amount of looks.
		}
		else
		{
			_nationality = 0;
		}
	}

	/// <summary>Returns the soldier's callsign (in quotes).</summary>
	/// <param name="maxLength">Restrict length to a certain value.</param>
	public string GetCallsign(uint maxLength = 20)
	{
		return "\"" + CodePointPrefix(_callsign, maxLength) + "\"";
	}

	/// <summary>Changes the soldier's callsign.</summary>
	public void SetCallsign(string callsign)
	{
		_callsign = callsign;
	}

	/// <summary>Check whether the soldier has a callsign assigned.</summary>
	public bool HasCallsign => _callsign.Length != 0;

	/// <summary>
	/// Generate a random callsign from the pool of names. Tries to fallback to the first entry in
	/// in the namepool list if no callsigns for the given nationality are defined.
	/// </summary>
	private string GenerateCallsign(List<SoldierNamePool> names)
	{
		string callsign = names[_nationality].GenCallsign(_gender);
		if (callsign.Length == 0)
		{
			callsign = names[0].GenCallsign(_gender);
		}
		return callsign;
	}

	/// <summary>The soldier's nationality.</summary>
	public int Nationality
	{
		get => _nationality;
		set => _nationality = value;
	}

	/// <summary>Returns the craft the soldier is assigned to.</summary>
	public Craft Craft => _craft;

	/// <summary>Automatically move equipment between the craft and the base when assigning/deassigning/reassigning soldiers.</summary>
	/// <remarks>Like C++, this works on the current craft (<c>_craft</c>), not on <paramref name="craft"/>.</remarks>
	private void AutoMoveEquipment(Craft craft, Base @base, int toBase)
	{
		var inTheBase = @base.StorageItems;
		var onTheCraft = _craft.Items;
		var reservedForTheCraft = _craft.SoldierItems;

		// Disclaimer: no checks for items not allowed on crafts; no checks for any craft limits (item number or weight). I'm not willing to spend the next 5+ years fixing it!
		foreach (var invItem in _equipmentLayout)
		{
			// ignore fixed weapons...
			if (!invItem.IsFixed)
			{
				var invItemMain = invItem.ItemType;
				if (toBase > 0)
				{
					if (onTheCraft.GetItem(invItemMain) > 0)
					{
						inTheBase.AddItem(invItemMain, 1);
						onTheCraft.RemoveItem(invItemMain, 1);
					}
					reservedForTheCraft.RemoveItem(invItemMain, 1);
				}
				else if (toBase < 0)
				{
					if (inTheBase.GetItem(invItemMain) > 0)
					{
						inTheBase.RemoveItem(invItemMain, 1);
						onTheCraft.AddItem(invItemMain, 1);
					}
					reservedForTheCraft.AddItem(invItemMain, 1);
				}
			}
			// ...but not their ammo
			for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
			{
				var invItemAmmo = invItem.GetAmmoItemForSlot(slot);
				if (invItemAmmo != null)
				{
					if (toBase > 0)
					{
						if (onTheCraft.GetItem(invItemAmmo) > 0)
						{
							inTheBase.AddItem(invItemAmmo, 1);
							onTheCraft.RemoveItem(invItemAmmo, 1);
						}
						reservedForTheCraft.RemoveItem(invItemAmmo, 1);
					}
					else if (toBase < 0)
					{
						if (inTheBase.GetItem(invItemAmmo) > 0)
						{
							inTheBase.RemoveItem(invItemAmmo, 1);
							onTheCraft.AddItem(invItemAmmo, 1);
						}
						reservedForTheCraft.AddItem(invItemAmmo, 1);
					}
				}
			}
		}
	}

	/// <summary>Assigns the soldier to a new craft.</summary>
	public void SetCraft(Craft craft, bool resetCustomDeployment = false)
	{
		_craft = craft;

		if (resetCustomDeployment && _craft != null)
		{
			// adding a soldier into a craft invalidates a custom craft deployment
			_craft.ResetCustomDeployment();
		}
	}

	/// <summary>Assigns the soldier to a new craft and automatically moves the equipment (if enabled).</summary>
	public void SetCraftAndMoveEquipment(Craft craft, Base @base, bool isNewBattle, bool resetCustomDeployment = false)
	{
		bool notTheSameCraft = _craft != craft;

		if (Options.oxceAlternateCraftEquipmentManagement && !isNewBattle && notTheSameCraft && @base != null)
		{
			if (_craft != null)
			{
				AutoMoveEquipment(_craft, @base, 1); // move from old craft to base
			}
		}

		SetCraft(craft, resetCustomDeployment);

		if (Options.oxceAlternateCraftEquipmentManagement && !isNewBattle && notTheSameCraft && @base != null)
		{
			if (craft != null)
			{
				AutoMoveEquipment(craft, @base, -1); // move from base to new craft
			}
		}
	}

	/// <summary>
	/// Returns the soldier's craft string, which
	/// is either the soldier's wounded status,
	/// the assigned craft name, or none.
	/// </summary>
	public string GetCraftString(Language lang, BaseSumDailyRecovery recovery)
	{
		string s;
		if (_death != null)
		{
			if (_death.Cause != null)
			{
				s = lang.GetString("STR_KILLED_IN_ACTION", _gender);
			}
			else
			{
				s = lang.GetString("STR_MISSING_IN_ACTION", _gender);
			}
		}
		else if (IsWounded)
		{
			string ss = lang.GetString("STR_WOUNDED");
			ss += ">";
			int days = GetNeededRecoveryTime(recovery);
			if (days < 0)
			{
				ss += "∞";
			}
			else
			{
				ss += days.ToString(CultureInfo.InvariantCulture);
			}
			s = ss;
		}
		else if (_craft == null)
		{
			s = lang.GetString("STR_NONE_UC");
		}
		else
		{
			s = _craft.GetName(lang);
		}
		return s;
	}

	/// <summary>Returns a localizable-string representation of the soldier's military rank.</summary>
	public string RankString
	{
		get
		{
			List<string> rankStrings = _rules.RankStrings;
			if (!_rules.AllowPromotion)
			{
				// even if promotion is not allowed, we allow to use a different "Rookie" translation per soldier type
				if (rankStrings.Count == 0)
				{
					return "STR_RANK_NONE";
				}
			}

			switch (_rank)
			{
				case SoldierRank.RANK_ROOKIE:
					if (rankStrings.Count > 0)
						return rankStrings[0];
					return "STR_ROOKIE";
				case SoldierRank.RANK_SQUADDIE:
					if (rankStrings.Count > 1)
						return rankStrings[1];
					return "STR_SQUADDIE";
				case SoldierRank.RANK_SERGEANT:
					if (rankStrings.Count > 2)
						return rankStrings[2];
					return "STR_SERGEANT";
				case SoldierRank.RANK_CAPTAIN:
					if (rankStrings.Count > 3)
						return rankStrings[3];
					return "STR_CAPTAIN";
				case SoldierRank.RANK_COLONEL:
					if (rankStrings.Count > 4)
						return rankStrings[4];
					return "STR_COLONEL";
				case SoldierRank.RANK_COMMANDER:
					if (rankStrings.Count > 5)
						return rankStrings[5];
					return "STR_COMMANDER";
				default:
					return "";
			}
		}
	}

	/// <summary>Returns a graphic representation of the soldier's military rank from BASEBITS.PCK.</summary>
	public int RankSprite => _rules.RankSprite + (int)_rank;

	/// <summary>Returns a graphic representation of the soldier's military rank from SMOKE.PCK.</summary>
	public int RankSpriteBattlescape => _rules.RankSpriteBattlescape + (int)_rank;

	/// <summary>Returns a graphic representation of the soldier's military rank from TinyRanks.</summary>
	public int RankSpriteTiny => _rules.RankSpriteTiny + (int)_rank;

	/// <summary>The soldier's military rank; setting it promotes/demotes the soldier to a specific rank (C++ setRank).</summary>
	public SoldierRank Rank
	{
		get => _rank;
		set
		{
			if (!_rules.AllowPromotion)
				return;

			List<string> rankStrings = _rules.RankStrings;
			if (rankStrings.Count != 0)
			{
				// abort if the desired rank is not indexed in the rank strings
				if ((int)value >= rankStrings.Count)
				{
					return;
				}
			}

			_rank = value;

			// Note: we don't need to show a notification for this style of promotion
		}
	}

	/// <summary>Increase the soldier's military rank.</summary>
	public void PromoteRank()
	{
		if (!_rules.AllowPromotion)
			return;

		List<string> rankStrings = _rules.RankStrings;
		if (rankStrings.Count != 0)
		{
			// stop if the soldier already has the maximum possible rank for his soldier type
			if ((int)_rank >= rankStrings.Count - 1)
			{
				return;
			}
		}

		_rank = (SoldierRank)((int)_rank + 1);
		if (_rank > SoldierRank.RANK_SQUADDIE)
		{
			// only promotions above SQUADDIE are worth to be mentioned
			_recentlyPromoted = true;
		}
	}

	/// <summary>Returns the soldier's amount of missions.</summary>
	public int Missions => _missions;

	/// <summary>Returns the soldier's amount of kills.</summary>
	public int Kills => _kills;

	/// <summary>Returns the soldier's amount of stuns.</summary>
	public int Stuns => _stuns;

	/// <summary>The soldier's gender.</summary>
	public SoldierGender Gender
	{
		get => _gender;
		set => _gender = value;
	}

	/// <summary>The soldier's look.</summary>
	public SoldierLook Look
	{
		get => _look;
		set => _look = value;
	}

	/// <summary>The soldier's look sub type.</summary>
	public int LookVariant
	{
		get => _lookVariant;
		set => _lookVariant = value;
	}

	/// <summary>Returns the soldier's rules.</summary>
	public RuleSoldier Rules => _rules;

	/// <summary>Returns the soldier's unique ID. Each soldier can be identified by its ID. (not it's name)</summary>
	public int Id => _id;

	/// <summary>Add a mission to the counter.</summary>
	public void AddMissionCount()
	{
		_missions++;
	}

	/// <summary>Add a kill to the counter.</summary>
	public void AddKillCount(int count)
	{
		_kills += count;
	}

	/// <summary>Add a stun to the counter.</summary>
	public void AddStunCount(int count)
	{
		_stuns += count;
	}

	/// <summary>Get initial stats.</summary>
	public UnitStats InitStats => _initialStats;

	/// <summary>Get current stats (editable, C++ returns a pointer).</summary>
	public ref UnitStats CurrentStatsEditable => ref _currentStats;

	/// <summary>Get current stats.</summary>
	public UnitStats CurrentStats => _currentStats;

	/// <summary>Set initial and current stats.</summary>
	public void SetBothStats(ref UnitStats stats)
	{
		_currentStats = stats;
		_initialStats = stats;
	}

	/// <summary>Returns the unit's promotion status and resets it.</summary>
	public bool IsPromoted
	{
		get
		{
			bool promoted = _recentlyPromoted;
			_recentlyPromoted = false;
			return promoted;
		}
	}

	/// <summary>Returns the unit's current armor.</summary>
	public Armor Armor => _armor;

	/// <summary>Changes the unit's current armor.</summary>
	public void SetArmor(Armor armor, bool resetCustomDeployment = false)
	{
		if (resetCustomDeployment && _craft != null && _armor != null && armor != null && _armor.Size < armor.Size)
		{
			// increasing the size of a soldier's armor invalidates a custom craft deployment
			_craft.ResetCustomDeployment();
		}

		_armor = armor;
	}

	/// <summary>Returns a list of armor layers (sprite names).</summary>
	public List<string> GetArmorLayers(Armor customArmor = null)
	{
		Armor armor = customArmor ?? _armor;

		string gender = _gender == SoldierGender.GENDER_MALE ? "M" : "F";
		var layoutDefinition = armor.LayersDefinition;

		// find relevant layer
		for (int i = 0; i <= RuleSoldier.LookVariantBits; ++i)
		{
			string key = gender + ((int)_look + (_lookVariant & (RuleSoldier.LookVariantMask >> i)) * 4).ToString(CultureInfo.InvariantCulture);
			if (layoutDefinition.TryGetValue(key, out var layers))
			{
				return layers;
			}
		}

		{
			// try also gender + hardcoded look 0
			if (layoutDefinition.TryGetValue(gender + "0", out var layers))
			{
				return layers;
			}
		}

		throw new InvalidOperationException("Layered armor sprite definition (" + armor.Type + ") not found!");
	}

	/// <summary>The soldier's original armor (before replacement).</summary>
	public Armor ReplacedArmor
	{
		get => _replacedArmor;
		set => _replacedArmor = value;
	}

	/// <summary>The soldier's original armor (before transformation).</summary>
	public Armor TransformedArmor
	{
		get => _transformedArmor;
		set => _transformedArmor = value;
	}

	/// <summary>Calculates absolute threshold based on percentage threshold.</summary>
	private static int ValueThreshold(int @base, int threshold)
	{
		return @base * threshold / 100;
	}

	/// <summary>Calculates the amount that exceeds the threshold of the base value.</summary>
	private static int ValueOverThreshold(int value, int @base, int threshold)
	{
		return Math.Max(0, value - ValueThreshold(@base, threshold));
	}

	/// <summary>Calculates how long will it take to recover; -1 means infinity.</summary>
	private static int RecoveryTime(int current, int recovery)
	{
		if (current <= 0)
		{
			return 0;
		}

		if (recovery <= 0)
			return -1; // represents infinity

		int days = current / recovery;
		if (current % recovery > 0)
			++days;

		return days;
	}

	/// <summary>Is the soldier wounded or not?</summary>
	public bool IsWounded
	{
		get
		{
			if (_manaMissing > 0)
			{
				if (ValueOverThreshold(_manaMissing, _currentStats.mana, _rules.ManaWoundThreshold) != 0)
				{
					return true;
				}
			}
			if (_healthMissing > 0)
			{
				if (ValueOverThreshold(_healthMissing, _currentStats.health, _rules.HealthWoundThreshold) != 0)
				{
					return true;
				}
			}
			return _recovery > 0.0f;
		}
	}

	/// <summary>Is the soldier fully healed (not wounded)?</summary>
	public bool HasFullHealth => !IsWounded;

	/// <summary>Is the soldier capable of defending a base? False if wounded too much.</summary>
	public bool CanDefendBase()
	{
		int currentHealthPercentage = Math.Max(0, _currentStats.health - WoundRecoveryInt - HealthMissing) * 100 / _currentStats.health;
		return currentHealthPercentage >= Options.oxceWoundedDefendBaseIf;
	}

	/// <summary>The amount of missing mana (never negative).</summary>
	public int ManaMissing
	{
		get => _manaMissing;
		set => _manaMissing = Math.Max(value, 0);
	}

	/// <summary>Returns the amount of time until the soldier's mana is fully replenished (days, -1 = infinity).</summary>
	public int GetManaRecovery(int manaRecoveryPerDay)
	{
		return RecoveryTime(_manaMissing, manaRecoveryPerDay);
	}

	/// <summary>The amount of missing health (never negative).</summary>
	public int HealthMissing
	{
		get => _healthMissing;
		set => _healthMissing = Math.Max(value, 0);
	}

	/// <summary>Returns the amount of time until the soldier's health is fully replenished (days, -1 = infinity).</summary>
	public int GetHealthRecovery(int healthRecoveryPerDay)
	{
		return RecoveryTime(_healthMissing, healthRecoveryPerDay);
	}

	/// <summary>Returns the amount of time until the soldier is healed (days).</summary>
	public int WoundRecoveryInt
	{
		get
		{
			// Note: only for use in Yankes scripts! ...and in base defense HP calculations :/
			return (int)MathF.Ceiling(_recovery);
		}
	}

	/// <summary>Changes the amount of time until the soldier is healed.</summary>
	public void SetWoundRecovery(int recovery)
	{
		_recovery = Math.Max(recovery, 0);
	}

	/// <summary>Gets the soldier's wound recovery time in days, considering the sick bay bonuses.</summary>
	public int GetWoundRecovery(float absBonus, float relBonus)
	{
		float hpPerDay = 1.0f + absBonus + (relBonus * _currentStats.health * 0.01f);
		return (int)MathF.Ceiling(_recovery / hpPerDay);
	}

	/// <summary>Heals soldier wounds.</summary>
	public void HealWound(float absBonus, float relBonus)
	{
		// 1 hp per day as minimum
		_recovery -= 1.0f;

		// absolute bonus from sick bay facilities
		_recovery -= absBonus;

		// relative bonus from sick bay facilities
		_recovery -= (relBonus * _currentStats.health * 0.01f);

		if (_recovery < 0.0f)
			_recovery = 0.0f;
	}

	/// <summary>Replenishes the soldier's mana.</summary>
	public void ReplenishMana(int manaRecoveryPerDay)
	{
		_manaMissing -= manaRecoveryPerDay;

		if (_manaMissing < 0)
			_manaMissing = 0;

		// maximum amount of mana missing can be up to 2x the current mana pool (WITHOUT armor and bonuses!); at least 100
		int maxThreshold = Math.Max(100, _currentStats.mana * 2);
		if (_manaMissing > maxThreshold)
			_manaMissing = maxThreshold;
	}

	/// <summary>Replenishes the soldier's health.</summary>
	public void ReplenishHealth(int healthRecoveryPerDay)
	{
		_healthMissing -= healthRecoveryPerDay;

		if (_healthMissing < 0)
			_healthMissing = 0;
	}

	/// <summary>Daily stat replenish and healing of the soldier based on the facilities available in the base.</summary>
	/// <param name="recovery">Recovery values provided by the base.</param>
	public void ReplenishStats(BaseSumDailyRecovery recovery)
	{
		if (_recovery > 0.0f)
		{
			HealWound(recovery.SickBayAbsoluteBonus, recovery.SickBayRelativeBonus);
		}
		else
		{
			if (ManaMissing > 0 && recovery.ManaRecovery > 0)
			{
				// positive mana recovery only when NOT wounded
				ReplenishMana(recovery.ManaRecovery);
			}

			if (HealthMissing > 0 && recovery.HealthRecovery > 0)
			{
				// health recovery only when NOT wounded
				ReplenishHealth(recovery.HealthRecovery);
			}
		}

		if (recovery.ManaRecovery < 0)
		{
			// negative mana recovery always
			ReplenishMana(recovery.ManaRecovery);
		}
	}

	/// <summary>Gets number of days until the soldier is ready for action again (-1 = infinity).</summary>
	public int GetNeededRecoveryTime(BaseSumDailyRecovery recovery)
	{
		int time = GetWoundRecovery(recovery.SickBayAbsoluteBonus, recovery.SickBayRelativeBonus);

		int bonusTime = 0;
		if (_healthMissing > 0)
		{
			int t = RecoveryTime(
				ValueOverThreshold(_healthMissing, _currentStats.health, _rules.HealthWoundThreshold),
				recovery.HealthRecovery
			);

			if (t < 0)
			{
				return t;
			}

			bonusTime = Math.Max(bonusTime, t);
		}
		if (_manaMissing > 0)
		{
			int t = RecoveryTime(
				ValueOverThreshold(_manaMissing, _currentStats.mana, _rules.ManaWoundThreshold),
				recovery.ManaRecovery
			);

			if (t < 0)
			{
				return t;
			}

			bonusTime = Math.Max(bonusTime, t);
		}

		return time + bonusTime;
	}

	/// <summary>Returns the list of EquipmentLayoutItems of a soldier (last used layout, managed by the game).</summary>
	public List<EquipmentLayoutItem> EquipmentLayout => _equipmentLayout;

	/// <summary>Returns the personal equipment layout (managed by the player).</summary>
	public List<EquipmentLayoutItem> PersonalEquipmentLayout => _personalEquipmentLayout;

	/// <summary>The soldier's personal equipment armor.</summary>
	public Armor PersonalEquipmentArmor
	{
		get => _personalEquipmentArmor;
		set => _personalEquipmentArmor = value;
	}

	/// <summary>Trains a soldier's Psychic abilities after 1 month.</summary>
	public void TrainPsi()
	{
		short psiSkillCap = _rules.StatCaps.psiSkill;
		short psiStrengthCap = _rules.StatCaps.psiStrength;

		_improvement = _psiStrImprovement = 0;
		// -10 days - tolerance threshold for switch from anytimePsiTraining option.
		// If soldier has psi skill -10..-1, he was trained 20..59 days. 81.7% probability, he was trained more that 30 days.
		if (_currentStats.psiSkill < -10 + _rules.MinStats.psiSkill)
			_currentStats.psiSkill = _rules.MinStats.psiSkill;
		else if (_currentStats.psiSkill <= _rules.MaxStats.psiSkill)
		{
			int max = _rules.MaxStats.psiSkill + _rules.MaxStats.psiSkill / 2;
			_improvement = Rng.Generate(_rules.MaxStats.psiSkill, max);
		}
		else
		{
			if (_currentStats.psiSkill <= (psiSkillCap / 2)) _improvement = Rng.Generate(5, 12);
			else if (_currentStats.psiSkill < psiSkillCap) _improvement = Rng.Generate(1, 3);

			if (Options.allowPsiStrengthImprovement)
			{
				if (_currentStats.psiStrength <= (psiStrengthCap / 2)) _psiStrImprovement = Rng.Generate(5, 12);
				else if (_currentStats.psiStrength < psiStrengthCap) _psiStrImprovement = Rng.Generate(1, 3);
			}
		}
		// std::min<UnitStats::Type>: the sum is narrowed to Sint16 first
		_currentStats.psiSkill = Math.Max(_currentStats.psiSkill, Math.Min((short)(_currentStats.psiSkill + _improvement), psiSkillCap));
		_currentStats.psiStrength = Math.Max(_currentStats.psiStrength, Math.Min((short)(_currentStats.psiStrength + _psiStrImprovement), psiStrengthCap));
	}

	/// <summary>Trains a soldier's Psychic abilities after 1 day (anytimePsiTraining option).</summary>
	public void TrainPsi1Day()
	{
		if (!_psiTraining)
		{
			_improvement = 0;
			return;
		}

		if (_currentStats.psiSkill > 0) // yes, 0. _rules->getMinStats().psiSkill was wrong.
		{
			if (8 * 100 >= _currentStats.psiSkill * Rng.Generate(1, 100) && _currentStats.psiSkill < _rules.StatCaps.psiSkill)
			{
				++_improvement;
				++_currentStats.psiSkill;
			}

			if (Options.allowPsiStrengthImprovement)
			{
				if (8 * 100 >= _currentStats.psiStrength * Rng.Generate(1, 100) && _currentStats.psiStrength < _rules.StatCaps.psiStrength)
				{
					++_psiStrImprovement;
					++_currentStats.psiStrength;
				}
			}
		}
		else if (_currentStats.psiSkill < _rules.MinStats.psiSkill)
		{
			if (++_currentStats.psiSkill == _rules.MinStats.psiSkill) // initial training is over
			{
				_improvement = _rules.MaxStats.psiSkill + Rng.Generate(0, _rules.MaxStats.psiSkill / 2);
				_currentStats.psiSkill = (short)_improvement;
			}
		}
		else // minStats.psiSkill <= 0 && _currentStats.psiSkill == minStats.psiSkill
			_currentStats.psiSkill -= (short)Rng.Generate(30, 60); // set initial training from 30 to 60 days
	}

	/// <summary>Is the soldier already fully psi-trained (cannot gain any more stats in the psi-training facility)?</summary>
	public bool IsFullyPsiTrained
	{
		get
		{
			if (_currentStats.psiSkill >= _rules.StatCaps.psiSkill)
			{
				if (Options.allowPsiStrengthImprovement)
				{
					if (_currentStats.psiStrength >= _rules.StatCaps.psiStrength)
					{
						return true;
					}
				}
				else
				{
					return true;
				}
			}
			return false;
		}
	}

	/// <summary>Returns whether or not the unit is in psi training.</summary>
	public bool IsInPsiTraining => _psiTraining;

	/// <summary>Changes whether or not the unit is in psi training.</summary>
	public void SetPsiTraining(bool psi)
	{
		_psiTraining = psi;
	}

	/// <summary>Returns this soldier's psionic skill improvement score for this month.</summary>
	public int Improvement => _improvement;

	/// <summary>Returns this soldier's psionic strength improvement score for this month.</summary>
	public int PsiStrImprovement => _psiStrImprovement;

	/// <summary>Returns the soldier's death details (null if no death has occurred).</summary>
	public SoldierDeath Death => _death;

	/// <summary>Kills the soldier in the Geoscape.</summary>
	/// <param name="death">Death data.</param>
	public void Die(SoldierDeath death)
	{
		_death = death;

		// Clean up associations
		_craft = null;
		_psiTraining = false;
		_training = false;
		_returnToTrainingWhenHealed = false;
		_recentlyPromoted = false;
		_manaMissing = 0;
		_healthMissing = 0;
		_recovery = 0.0f;
		ClearEquipmentLayout();
		_personalEquipmentLayout.Clear();
	}

	/// <summary>Clears the equipment layout.</summary>
	public void ClearEquipmentLayout()
	{
		_equipmentLayout.Clear();
	}

	/// <summary>Returns the soldier's diary.</summary>
	public SoldierDiary Diary => _diary;

	/// <summary>Resets the soldier's diary.</summary>
	public void ResetDiary()
	{
		_diary = new SoldierDiary();
	}

	/// <summary>Calculates the soldier's statString.</summary>
	/// <param name="statStrings">List of statString rules.</param>
	/// <param name="psiStrengthEval">Are psi stats available?</param>
	public void CalcStatString(List<StatString> statStrings, bool psiStrengthEval)
	{
		if (_rules.StatStrings.Count == 0)
		{
			_statString = StatString.CalcStatString(ref _currentStats, statStrings, psiStrengthEval, _psiTraining);
		}
		else
		{
			_statString = StatString.CalcStatString(ref _currentStats, _rules.StatStrings, psiStrengthEval, _psiTraining);
		}
	}

	/// <summary>Trains a soldier's Physical abilities.</summary>
	public void TrainPhys(int customTrainingFactor)
	{
		UnitStats caps1 = _rules.StatCaps;
		UnitStats caps2 = _rules.TrainingStatCaps;
		// no P.T. for the wounded
		if (HasFullHealth)
		{
			if (_currentStats.firing < caps1.firing && Rng.Generate(0, caps2.firing) > _currentStats.firing && Rng.Percent(customTrainingFactor))
				_currentStats.firing++;
			if (_currentStats.health < caps1.health && Rng.Generate(0, caps2.health) > _currentStats.health && Rng.Percent(customTrainingFactor))
				_currentStats.health++;
			if (_currentStats.melee < caps1.melee && Rng.Generate(0, caps2.melee) > _currentStats.melee && Rng.Percent(customTrainingFactor))
				_currentStats.melee++;
			if (_currentStats.throwing < caps1.throwing && Rng.Generate(0, caps2.throwing) > _currentStats.throwing && Rng.Percent(customTrainingFactor))
				_currentStats.throwing++;
			if (_currentStats.strength < caps1.strength && Rng.Generate(0, caps2.strength) > _currentStats.strength && Rng.Percent(customTrainingFactor))
				_currentStats.strength++;
			if (_currentStats.tu < caps1.tu && Rng.Generate(0, caps2.tu) > _currentStats.tu && Rng.Percent(customTrainingFactor))
				_currentStats.tu++;
			if (_currentStats.stamina < caps1.stamina && Rng.Generate(0, caps2.stamina) > _currentStats.stamina && Rng.Percent(customTrainingFactor))
				_currentStats.stamina++;
		}
	}

	/// <summary>Is the soldier already fully trained (cannot gain any more stats in the training facility)?</summary>
	public bool IsFullyTrained
	{
		get
		{
			UnitStats trainingCaps = _rules.TrainingStatCaps;

			if (_currentStats.firing < trainingCaps.firing
				|| _currentStats.health < trainingCaps.health
				|| _currentStats.melee < trainingCaps.melee
				|| _currentStats.throwing < trainingCaps.throwing
				|| _currentStats.strength < trainingCaps.strength
				|| _currentStats.tu < trainingCaps.tu
				|| _currentStats.stamina < trainingCaps.stamina)
			{
				return false;
			}
			return true;
		}
	}

	/// <summary>Returns whether or not the unit is in physical training.</summary>
	public bool IsInTraining => _training;

	/// <summary>Changes whether or not the unit is in physical training.</summary>
	public void SetTraining(bool training)
	{
		_training = training;
	}

	/// <summary>Should the soldier return to martial training automatically when fully healed?</summary>
	public bool ReturnToTrainingWhenHealed
	{
		get => _returnToTrainingWhenHealed;
		set => _returnToTrainingWhenHealed = value;
	}

	/// <summary>Sets whether or not the unit's corpse was recovered from a battle.</summary>
	public void SetCorpseRecovered(bool corpseRecovered)
	{
		_corpseRecovered = corpseRecovered;
	}

	/// <summary>Gets the previous transformations performed on this soldier.</summary>
	public SortedDictionary<string, int> PreviousTransformations => _previousTransformations;

	/// <summary>Checks whether or not the soldier is eligible for a certain transformation.</summary>
	public bool IsEligibleForTransformation(RuleSoldierTransformation transformationRule)
	{
		// rank check
		if ((int)_rank < transformationRule.MinRank)
			return false;

		// alive and well
		if (_death == null && !IsWounded && !transformationRule.IsAllowingAliveSoldiers)
			return false;

		// alive and wounded
		if (_death == null && IsWounded && !transformationRule.IsAllowingWoundedSoldiers)
			return false;

		// dead
		if (_death != null && !transformationRule.IsAllowingDeadSoldiers)
			return false;

		// dead and vaporized, or missing in action
		if (_death != null && !_corpseRecovered && transformationRule.NeedsCorpseRecovered())
			return false;

		// Is the soldier of the correct type?
		if (!transformationRule.AllowedSoldierTypes.Contains(_rules.Type))
			return false;

		// Does this soldier's transformation history preclude this new project?
		foreach (var reqd_trans in transformationRule.RequiredPreviousTransformations)
		{
			if (!_previousTransformations.ContainsKey(reqd_trans))
				return false;
		}

		foreach (var forb_trans in transformationRule.ForbiddenPreviousTransformations)
		{
			if (_previousTransformations.ContainsKey(forb_trans))
				return false;
		}

		// Does this soldier meet the minimum stat requirements for the project?
		UnitStats currentStats = transformationRule.IncludeBonusesForMinStats ? _tmpStatsWithSoldierBonuses : _currentStats;
		UnitStats minStats = transformationRule.RequiredMinStats;
		if (currentStats.tu < minStats.tu ||
			currentStats.stamina < minStats.stamina ||
			currentStats.health < minStats.health ||
			currentStats.bravery < minStats.bravery ||
			currentStats.reactions < minStats.reactions ||
			currentStats.firing < minStats.firing ||
			currentStats.throwing < minStats.throwing ||
			currentStats.melee < minStats.melee ||
			currentStats.mana < minStats.mana ||
			currentStats.strength < minStats.strength ||
			currentStats.psiStrength < minStats.psiStrength ||
			(currentStats.psiSkill < minStats.psiSkill && minStats.psiSkill != 0)) // The != 0 is required for the "psi training at any time" option, as it sets skill to negative in training
			return false;

		// Does this soldier meet the maximum stat requirements for the project?
		currentStats = transformationRule.IncludeBonusesForMaxStats ? _tmpStatsWithSoldierBonuses : _currentStats;
		UnitStats maxStats = transformationRule.RequiredMaxStats;
		if (currentStats.tu > maxStats.tu ||
			currentStats.stamina > maxStats.stamina ||
			currentStats.health > maxStats.health ||
			currentStats.bravery > maxStats.bravery ||
			currentStats.reactions > maxStats.reactions ||
			currentStats.firing > maxStats.firing ||
			currentStats.throwing > maxStats.throwing ||
			currentStats.melee > maxStats.melee ||
			currentStats.mana > maxStats.mana ||
			currentStats.strength > maxStats.strength ||
			currentStats.psiStrength > maxStats.psiStrength ||
			currentStats.psiSkill > maxStats.psiSkill)
			return false;

		// Does the soldier have the required commendations?
		foreach (var reqd_comm in transformationRule.RequiredCommendations)
		{
			bool found = false;
			foreach (var comm in _diary.SoldierCommendations)
			{
				if (comm.DecorationLevelInt >= reqd_comm.Value && comm.Type == reqd_comm.Key)
				{
					found = true;
					break;
				}
			}
			if (!found)
				return false;
		}

		return true;
	}

	/// <summary>Performs a transformation on this unit.</summary>
	public void Transform(Mod mod, RuleSoldierTransformation transformationRule, Soldier sourceSoldier, Base @base)
	{
		if (_death != null)
		{
			_corpseRecovered = false; // They're not a corpse anymore!
			_death = null;
		}

		if (transformationRule.RecoveryTime > 0)
		{
			_recovery = transformationRule.RecoveryTime;
		}

		// needed, because the armor size may change (also, it just makes sense)
		sourceSoldier.SetCraftAndMoveEquipment(null, @base, false);

		if (transformationRule.IsCreatingClone)
		{
			// a clone already has the correct soldier type, but random stats
			// if we don't want random stats, let's copy them from the source soldier
			UnitStats sourceStats = sourceSoldier.CurrentStats + CalculateStatChanges(mod, transformationRule, sourceSoldier, 0, sourceSoldier.Rules);
			UnitStats mergedStats = UnitStats.Combine(transformationRule.RerollStats, sourceStats, _currentStats);
			SetBothStats(ref mergedStats);
		}
		else
		{
			// backup original soldier type, it will still be needed later for stat change calculations
			RuleSoldier sourceSoldierType = _rules;

			// change soldier type if needed
			if (!Mod.IsEmptyRuleName(transformationRule.ProducedSoldierType) && _rules.Type != transformationRule.ProducedSoldierType)
			{
				_rules = mod.GetSoldier(transformationRule.ProducedSoldierType);

				// demote soldier if needed (i.e. when new soldier type doesn't support the current rank)
				if (!_rules.AllowPromotion)
				{
					_rank = SoldierRank.RANK_ROOKIE;
				}
				else if (_rules.RankStrings.Count != 0 && (int)_rank > _rules.RankStrings.Count - 1)
				{
					switch (_rules.RankStrings.Count - 1)
					{
						case 1:
							_rank = SoldierRank.RANK_SQUADDIE;
							break;
						case 2:
							_rank = SoldierRank.RANK_SERGEANT;
							break;
						case 3:
							_rank = SoldierRank.RANK_CAPTAIN;
							break;
						case 4:
							_rank = SoldierRank.RANK_COLONEL;
							break;
						case 5:
							_rank = SoldierRank.RANK_COMMANDER; // I hereby demote you to commander! :P
							break;
						default:
							_rank = SoldierRank.RANK_ROOKIE;
							break;
					}
				}

				// clamp (and randomize) nationality if needed
				{
					List<SoldierNamePool> names = _rules.Names;
					if (names.Count != 0)
					{
						if ((uint)_nationality >= (uint)_rules.Names.Count)
						{
							_nationality = Rng.Generate(0, names.Count - 1);
						}
					}
					else
					{
						_nationality = 0;
					}
				}
			}

			// handle training (soldier type change rules)
			if (sourceSoldierType != _rules && _rules.TrainingStatCaps.psiSkill <= 0)
			{
				// transformed into a new soldier type, which doesn't support psi training
				_psiTraining = false;
			}
			// handle training (recovery rules)
			if (_training && IsWounded)
			{
				_training = false;
				_returnToTrainingWhenHealed = true;
			}

			// reset soldier rank, if needed
			if (transformationRule.ResetRank)
			{
				_rank = SoldierRank.RANK_ROOKIE;
			}

			// change stats
			_currentStats += CalculateStatChanges(mod, transformationRule, sourceSoldier, 0, sourceSoldierType);

			// and randomize stats where needed
			{
				var tmpSoldier = new Soldier(_rules, null, 0 /*nationality*/, _id);
				_currentStats = UnitStats.Combine(transformationRule.RerollStats, _currentStats, tmpSoldier.CurrentStats);
			}
		}

		if (!transformationRule.IsKeepingSoldierArmor)
		{
			var oldArmor = _armor;
			if (Mod.IsEmptyRuleName(transformationRule.ProducedSoldierArmor))
			{
				// default armor of the soldier's type
				_armor = _rules.DefaultArmor;
			}
			else
			{
				// explicitly defined armor
				_armor = mod.GetArmor(transformationRule.ProducedSoldierArmor);
			}
			if (oldArmor != _armor && !transformationRule.IsCreatingClone)
			{
				if (oldArmor.StoreItem != null)
				{
					@base.StorageItems.AddItem(oldArmor.StoreItem);
				}
			}
		}

		// Reset performed transformations (on the destination soldier), if needed
		if (transformationRule.Reset)
		{
			_previousTransformations.Clear();
		}
		else if (transformationRule.RemoveTransformations.Count != 0)
		{
			// Remove specific transformations and their related bonuses
			foreach (var remove_transf in transformationRule.RemoveTransformations)
			{
				int count = 0;
				if (_previousTransformations.TryGetValue(remove_transf, out int it1))
				{
					count = it1;
					_previousTransformations.Remove(remove_transf);
				}
				if (count > 0)
				{
					var rtRule = mod.GetSoldierTransformation(remove_transf, false);
					if (rtRule != null)
					{
						if (!Mod.IsEmptyRuleName(rtRule.SoldierBonusType))
						{
							if (_transformationBonuses.TryGetValue(rtRule.SoldierBonusType, out int it2))
							{
								if (it2 > count)
								{
									_transformationBonuses[rtRule.SoldierBonusType] = it2 - count;
								}
								else
								{
									_transformationBonuses.Remove(rtRule.SoldierBonusType);
								}
							}
						}
					}
				}
			}
		}

		// Remember the performed transformation (on the source soldier)
		var history = sourceSoldier.PreviousTransformations;
		if (history.TryGetValue(transformationRule.Name, out int it))
		{
			history[transformationRule.Name] = it + 1;
		}
		else
		{
			history[transformationRule.Name] = 1;
		}

		// Reset soldier bonuses, if needed
		if (transformationRule.Reset)
		{
			_transformationBonuses.Clear();
		}

		// Award a soldier bonus, if defined
		if (!Mod.IsEmptyRuleName(transformationRule.SoldierBonusType))
		{
			if (_transformationBonuses.TryGetValue(transformationRule.SoldierBonusType, out int it2))
			{
				_transformationBonuses[transformationRule.SoldierBonusType] = it2 + 1;
			}
			else
			{
				_transformationBonuses[transformationRule.SoldierBonusType] = 1;
			}
		}
	}

	/// <summary>Calculates the stat changes a soldier undergoes from this project.</summary>
	/// <param name="mod">The mod.</param>
	/// <param name="transformationRule">The transformation.</param>
	/// <param name="sourceSoldier">The soldier the stats come from.</param>
	/// <param name="mode">0 = final, 1 = min, 2 = max.</param>
	/// <param name="sourceSoldierType">The soldier type before the transformation.</param>
	/// <returns>The stat changes.</returns>
	public UnitStats CalculateStatChanges(Mod mod, RuleSoldierTransformation transformationRule, Soldier sourceSoldier, int mode, RuleSoldier sourceSoldierType)
	{
		UnitStats statChange = default;

		UnitStats initialStats = sourceSoldier.InitStats;
		UnitStats currentStats = sourceSoldier.CurrentStats;
		UnitStats gainedStats = currentStats - initialStats;

		// Flat stat changes
		statChange += transformationRule.FlatOverallStatChange;
		UnitStats rnd0;
		if (mode == 2)
			rnd0 = UnitStats.Max(transformationRule.FlatMin, transformationRule.FlatMax);
		else if (mode == 1)
			rnd0 = UnitStats.Min(transformationRule.FlatMin, transformationRule.FlatMax);
		else
			rnd0 = UnitStats.Random(transformationRule.FlatMin, transformationRule.FlatMax);
		statChange += rnd0;

		// Stat changes based on current stats
		statChange += UnitStats.Percent(currentStats, transformationRule.PercentOverallStatChange);
		UnitStats rnd1;
		if (mode == 2)
			rnd1 = UnitStats.Max(transformationRule.PercentMin, transformationRule.PercentMax);
		else if (mode == 1)
			rnd1 = UnitStats.Min(transformationRule.PercentMin, transformationRule.PercentMax);
		else
			rnd1 = UnitStats.Random(transformationRule.PercentMin, transformationRule.PercentMax);
		statChange += UnitStats.Percent(currentStats, rnd1);

		// Stat changes based on gained stats
		statChange += UnitStats.Percent(gainedStats, transformationRule.PercentGainedStatChange);
		UnitStats rnd2;
		if (mode == 2)
			rnd2 = UnitStats.Max(transformationRule.PercentGainedMin, transformationRule.PercentGainedMax);
		else if (mode == 1)
			rnd2 = UnitStats.Min(transformationRule.PercentGainedMin, transformationRule.PercentGainedMax);
		else
			rnd2 = UnitStats.Random(transformationRule.PercentGainedMin, transformationRule.PercentGainedMax);
		statChange += UnitStats.Percent(gainedStats, rnd2);

		// round (mathematically) to whole tens
		int sign = statChange.bravery < 0 ? -1 : 1;
		statChange.bravery = (short)(((statChange.bravery + (sign * 5)) / 10) * 10);

		RuleSoldier transformationSoldierType = _rules;
		if (!Mod.IsEmptyRuleName(transformationRule.ProducedSoldierType))
		{
			transformationSoldierType = mod.GetSoldier(transformationRule.ProducedSoldierType);
		}

		if (transformationRule.HasLowerBoundAtMinStats)
		{
			UnitStats lowerBound = transformationSoldierType.MinStats;
			UnitStats cappedChange = lowerBound - currentStats;

			statChange = UnitStats.Max(statChange, cappedChange);
		}

		if (transformationRule.HasUpperBoundAtMaxStats || transformationRule.HasUpperBoundAtStatCaps)
		{
			UnitStats upperBound = transformationRule.HasUpperBoundAtMaxStats
				? transformationSoldierType.MaxStats
				: transformationSoldierType.StatCaps;
			UnitStats cappedChange = upperBound - currentStats;

			bool isSameSoldierType = transformationSoldierType == sourceSoldierType;
			bool softLimit = transformationRule.IsSoftLimit(isSameSoldierType);
			if (softLimit)
			{
				// soft limit
				statChange = UnitStats.SoftLimit(statChange, currentStats, upperBound);
			}
			else
			{
				// hard limit
				statChange = UnitStats.Min(statChange, cappedChange);
			}
		}

		return statChange;
	}

	/// <summary>Checks whether the soldier has a given bonus. Disclaimer: DOES NOT REFRESH THE BONUS CACHE!</summary>
	public bool HasBonus(RuleSoldierBonus bonus)
	{
		foreach (var sb in _bonusCache)
		{
			if (sb == bonus)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Gets all the soldier bonuses (sorted by list order); with a mod the cache is rebuilt first.</summary>
	public List<RuleSoldierBonus> GetBonuses(Mod mod)
	{
		if (mod != null)
		{
			_bonusCache.Clear();
			void AddSorted(RuleSoldierBonus b)
			{
				if (b == null)
				{
					return;
				}

				// std::lower_bound by list order
				int p = 0;
				while (p < _bonusCache.Count && _bonusCache[p].ListOrder < b.ListOrder)
				{
					++p;
				}
				if (p == _bonusCache.Count || _bonusCache[p] != b)
				{
					_bonusCache.Insert(p, b);
				}
			}

			foreach (var bonusName in _transformationBonuses)
			{
				var bonusRule = mod.GetSoldierBonus(bonusName.Key, false);

				AddSorted(bonusRule);
			}
			foreach (var commendation in _diary.SoldierCommendations)
			{
				var bonusRule = commendation.Rule.GetSoldierBonus(commendation.DecorationLevelInt);

				AddSorted(bonusRule);
			}
		}

		return _bonusCache;
	}

	/// <summary>Get current stats with soldier bonuses, but without armor bonuses.</summary>
	public UnitStats StatsWithSoldierBonusesOnly => _tmpStatsWithSoldierBonuses;

	/// <summary>Get current stats with armor and soldier bonuses.</summary>
	public UnitStats StatsWithAllBonuses => _tmpStatsWithAllBonuses;

	/// <summary>Pre-calculates soldier stats with various bonuses.</summary>
	public bool PrepareStatsWithBonuses(Mod mod)
	{
		bool hasSoldierBonus = false;

		// 1. current stats
		UnitStats tmp = _currentStats;
		var basePsiSkill = _currentStats.psiSkill;

		// 2. refresh soldier bonuses
		var bonuses = GetBonuses(mod); // this is the only place where bonus cache is rebuilt

		// 3. apply soldier bonuses
		foreach (var bonusRule in bonuses)
		{
			hasSoldierBonus = true;
			tmp += bonusRule.Stats;
		}

		// 4. stats with soldier bonuses, but without armor bonuses
		_tmpStatsWithSoldierBonuses = UnitStats.ObeyFixedMinimum(tmp);

		// if the psi skill has not been "unlocked" yet by training, do not allow soldier bonuses to unlock it
		if (basePsiSkill <= 0 && _tmpStatsWithSoldierBonuses.psiSkill > 0)
		{
			_tmpStatsWithSoldierBonuses.psiSkill = basePsiSkill;
		}

		// 5. apply armor bonus
		tmp += _armor.Stats;

		// 6. stats with all bonuses
		_tmpStatsWithAllBonuses = UnitStats.ObeyFixedMinimum(tmp);

		// 7. pilot armors count as soldier bonuses
		if (_armor.IsPilotArmor)
		{
			_tmpStatsWithSoldierBonuses = _tmpStatsWithAllBonuses;
		}

		return hasSoldierBonus;
	}

	/// <summary>Gets the daily dogfight experience cache (editable, C++ returns a pointer).</summary>
	public ref UnitStats DailyDogfightExperienceCache => ref _dailyDogfightExperienceCache;

	/// <summary>Resets the daily dogfight experience cache.</summary>
	public void ResetDailyDogfightExperienceCache()
	{
		_dailyDogfightExperienceCache = UnitStats.Scalar(0);
	}

	/// <summary>Check if the soldier has all the required soldier bonuses for the given soldier skill.</summary>
	public bool HasAllRequiredBonusesForSkill(RuleSkill skillRules)
	{
		foreach (var requiredBonusRule in skillRules.RequiredBonuses)
		{
			bool found = false;
			foreach (var bonusRule in GetBonuses(null))
			{
				if (bonusRule == requiredBonusRule)
				{
					found = true;
					break;
				}
			}
			if (!found)
				return false;
		}
		return true;
	}

	/// <summary>Check if the soldier has all the required stats and soldier bonuses for piloting the (current or new) craft.</summary>
	public bool HasAllPilotingRequirements(Craft newCraft = null)
	{
		if (!_rules.AllowPiloting)
			return false;

		Craft craft = newCraft ?? _craft;

		if (craft == null)
			return false;

		// Does this soldier meet the minimum stat requirements for piloting the current craft?
		UnitStats currentStats = _tmpStatsWithAllBonuses; // all bonuses count
		UnitStats minStats = craft.Rules.PilotMinStatsRequired;
		if (currentStats.tu < minStats.tu ||
			currentStats.stamina < minStats.stamina ||
			currentStats.health < minStats.health ||
			currentStats.bravery < minStats.bravery ||
			currentStats.reactions < minStats.reactions ||
			currentStats.firing < minStats.firing ||
			currentStats.throwing < minStats.throwing ||
			currentStats.melee < minStats.melee ||
			currentStats.mana < minStats.mana ||
			currentStats.strength < minStats.strength ||
			currentStats.psiStrength < minStats.psiStrength ||
			(currentStats.psiSkill < minStats.psiSkill && minStats.psiSkill != 0)) // The != 0 is required for the "psi training at any time" option, as it sets skill to negative in training
			return false;

		// Does this soldier have all required soldier bonuses for piloting the current craft?
		foreach (var requiredBonusRule in craft.Rules.PilotSoldierBonusesRequired)
		{
			bool found = false;
			foreach (var bonusRule in _bonusCache) // *getBonuses(nullptr)
			{
				if (bonusRule == requiredBonusRule)
				{
					found = true;
					break;
				}
			}
			if (!found)
				return false;
		}

		return true;
	}
}
