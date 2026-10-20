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
// Ported from: src/Mod/RuleSoldier.h, src/Mod/RuleSoldier.cpp
// Port status: partial (missing: script hooks)
#nullable disable


namespace Acom;

/// <summary>
/// Represents the creation data for an X-COM unit.
/// This info is copied to either Soldier for Geoscape or BattleUnit for Battlescape.
/// </summary>
public class RuleSoldier
{
	/// <summary>Number of bits for soldier gender.</summary>
	public const int LookGenderBits = 1;
	/// <summary>Number of bits for soldier look.</summary>
	public const int LookBaseBits = 2;
	/// <summary>Number of bits for soldier lookVariants.</summary>
	public const int LookVariantBits = 6;
	/// <summary>Max number of soldier lookVariants.</summary>
	public const int LookVariantMax = (1 << LookVariantBits);
	/// <summary>Mask for soldier lookVariants.</summary>
	public const int LookVariantMask = LookVariantMax - 1;
	/// <summary>Mask for all possible looks types for soldier.</summary>
	public const int LookTotalMask = (1 << (LookVariantBits + LookBaseBits + LookGenderBits)) - 1;

	/// <summary>Name of class used in script.</summary>
	public const string ScriptName = "RuleSoldier";

	private readonly string _type;
	private string _spawnedSoldier = "";
	private int _group;
	private int _listOrder;
	private readonly List<string> _requires = [];
	private RuleBaseFacilityFunctions _requiresBuyBaseFunc;
	private string _requiresBuyCountry = "";
	private UnitStats _minStats, _maxStats, _statCaps, _trainingStatCaps, _dogfightExperience;
	private string _armorName = "";
	private Armor _armor;
	private string _specWeaponName = "";
	private RuleItem _specWeapon;
	private int _monthlyBuyLimit;
	private string _monthlyBuyLimitMessage = "";
	private int _costBuy, _costSalary, _costSalarySquaddie, _costSalarySergeant, _costSalaryCaptain, _costSalaryColonel, _costSalaryCommander;
	private int _standHeight, _kneelHeight, _floatHeight;
	private int _femaleFrequency, _value, _transferTime, _moraleLossWhenKilled;
	private int _manaMissingWoundThreshold = -1;
	private int _healthMissingWoundThreshold = -1;
	private readonly List<int> _deathSoundMale = [], _deathSoundFemale = [];
	private readonly List<int> _panicSoundMale = [], _panicSoundFemale = [], _berserkSoundMale = [], _berserkSoundFemale = [];
	private readonly List<int> _selectUnitSoundMale = [], _selectUnitSoundFemale = [];
	private readonly List<int> _startMovingSoundMale = [], _startMovingSoundFemale = [];
	private readonly List<int> _selectWeaponSoundMale = [], _selectWeaponSoundFemale = [];
	private readonly List<int> _annoyedSoundMale = [], _annoyedSoundFemale = [];
	private readonly List<SoldierNamePool> _names = [];
	private int _totalSoldierNamePoolWeight;
	private string _armorForAvatar = "";
	private int _avatarOffsetX, _avatarOffsetY, _flagOffset;
	private bool _allowPromotion, _allowPiloting, _showTypeInInventory;
	private readonly List<StatString> _statStrings = [];
	private readonly List<string> _rankStrings = [];
	private int _rankSprite, _rankSpriteBattlescape, _rankSpriteTiny;
	private int _skillIconSprite;
	private readonly List<string> _skillNames = [];
	private readonly List<RuleSkill> _skills = [];

	/// <summary>Creates a blank RuleSoldier for a certain type of soldier.</summary>
	public RuleSoldier(string type, int listOrder)
	{
		_type = type;
		_group = 0;
		_listOrder = listOrder;
		_armor = null;
		_specWeapon = null;
		_monthlyBuyLimit = 0;
		_costBuy = 0;
		_costSalary = 0;
		_costSalarySquaddie = 0;
		_costSalarySergeant = 0;
		_costSalaryCaptain = 0;
		_costSalaryColonel = 0;
		_costSalaryCommander = 0;
		_standHeight = 0;
		_kneelHeight = 0;
		_floatHeight = 0;
		_femaleFrequency = 50;
		_value = 20;
		_transferTime = 0;
		_moraleLossWhenKilled = 100;
		_totalSoldierNamePoolWeight = 0;
		_avatarOffsetX = 67;
		_avatarOffsetY = 48;
		_flagOffset = 0;
		_allowPromotion = true;
		_allowPiloting = true;
		_showTypeInInventory = false;
		_rankSprite = 42;
		_rankSpriteBattlescape = 20;
		_rankSpriteTiny = 0;
		_skillIconSprite = 1;
	}

	/// <summary>Loads the soldier from a YAML file (C++ also takes the ModScript parsers for script values, see port status).</summary>
	public void Load(YamlNodeReader node, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		var reader = node.UseIndex();
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent, mod);
		}

		//requires
		mod.LoadUnorderedNames(_type, _requires, reader["requires"]);
		mod.LoadBaseFunction(_type, ref _requiresBuyBaseFunc, reader["requiresBuyBaseFunc"]);
		reader.TryRead("requiresBuyCountry", ref _requiresBuyCountry);


		_minStats.Merge(reader["minStats"].ReadVal(_minStats));
		_maxStats.Merge(reader["maxStats"].ReadVal(_maxStats));
		_statCaps.Merge(reader["statCaps"].ReadVal(_statCaps));
		if (reader["trainingStatCaps"])
		{
			_trainingStatCaps.Merge(reader["trainingStatCaps"].ReadVal(_trainingStatCaps));
		}
		else
		{
			_trainingStatCaps.Merge(reader["statCaps"].ReadVal(_trainingStatCaps));
		}
		_dogfightExperience.Merge(reader["dogfightExperience"].ReadVal(_dogfightExperience));
		mod.LoadName(_type, ref _armorName, reader["armor"]);
		reader.TryRead("specialWeapon", ref _specWeaponName);
		reader.TryRead("armorForAvatar", ref _armorForAvatar);
		reader.TryRead("avatarOffsetX", ref _avatarOffsetX);
		reader.TryRead("avatarOffsetY", ref _avatarOffsetY);
		reader.TryRead("flagOffset", ref _flagOffset);
		reader.TryRead("allowPromotion", ref _allowPromotion);
		reader.TryRead("allowPiloting", ref _allowPiloting);
		reader.TryRead("monthlyBuyLimit", ref _monthlyBuyLimit);
		reader.TryRead("monthlyBuyLimitMessage", ref _monthlyBuyLimitMessage);
		reader.TryRead("costBuy", ref _costBuy);
		reader.TryRead("costSalary", ref _costSalary);
		reader.TryRead("costSalarySquaddie", ref _costSalarySquaddie);
		reader.TryRead("costSalarySergeant", ref _costSalarySergeant);
		reader.TryRead("costSalaryCaptain", ref _costSalaryCaptain);
		reader.TryRead("costSalaryColonel", ref _costSalaryColonel);
		reader.TryRead("costSalaryCommander", ref _costSalaryCommander);
		reader.TryRead("standHeight", ref _standHeight);
		reader.TryRead("kneelHeight", ref _kneelHeight);
		reader.TryRead("floatHeight", ref _floatHeight);
		reader.TryRead("femaleFrequency", ref _femaleFrequency);
		reader.TryRead("value", ref _value);
		reader.TryRead("transferTime", ref _transferTime);
		reader.TryRead("moraleLossWhenKilled", ref _moraleLossWhenKilled);
		reader.TryRead("showTypeInInventory", ref _showTypeInInventory);

		mod.LoadSoundOffset(_type, _deathSoundMale, reader["deathMale"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _deathSoundFemale, reader["deathFemale"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _panicSoundMale, reader["panicMale"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _panicSoundFemale, reader["panicFemale"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _berserkSoundMale, reader["berserkMale"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _berserkSoundFemale, reader["berserkFemale"], "BATTLE.CAT");

		mod.LoadSoundOffset(_type, _selectUnitSoundMale, reader["selectUnitMale"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _selectUnitSoundFemale, reader["selectUnitFemale"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _startMovingSoundMale, reader["startMovingMale"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _startMovingSoundFemale, reader["startMovingFemale"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _selectWeaponSoundMale, reader["selectWeaponMale"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _selectWeaponSoundFemale, reader["selectWeaponFemale"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _annoyedSoundMale, reader["annoyedMale"], "BATTLE.CAT");
		mod.LoadSoundOffset(_type, _annoyedSoundFemale, reader["annoyedFemale"], "BATTLE.CAT");

		foreach (var fileNameOrCommand in reader["soldierNames"].Children())
		{
			string fileName = fileNameOrCommand.ReadVal<string>();
			if (fileName == "delete")
			{
				_names.Clear();
			}
			else
			{
				if (fileName[^1] == '/')
				{
					// load all *.nam files in given directory
					var names = new List<string>(FileMap.FilterFiles(FileMap.GetVFolderContents(fileName), "nam"));
					names.Sort(NaturalCompare);
					foreach (var name in names)
					{
						AddSoldierNamePool(fileName + name);
					}
				}
				else
				{
					// load given file
					AddSoldierNamePool(fileName);
				}
			}
		}

		foreach (var statStringReader in reader["statStrings"].Children())
		{
			var statString = new StatString();
			statString.Load(statStringReader);
			_statStrings.Add(statString);
		}

		mod.LoadNames(_type, _rankStrings, reader["rankStrings"]);
		mod.LoadSpriteOffset(_type, ref _rankSprite, reader["rankSprite"], "BASEBITS.PCK");
		mod.LoadSpriteOffset(_type, ref _rankSpriteBattlescape, reader["rankBattleSprite"], "SMOKE.PCK");
		mod.LoadSpriteOffset(_type, ref _rankSpriteTiny, reader["rankTinySprite"], "TinyRanks");
		mod.LoadSpriteOffset(_type, ref _skillIconSprite, reader["skillIconSprite"], "SPICONS.DAT");

		mod.LoadNames(_type, _skillNames, reader["skills"]);

		if (reader["spawnedSoldier"])
		{
			_spawnedSoldier = Unit.EmitDescendants(reader["spawnedSoldier"], new YamlRootNodeReader(_spawnedSoldier, "(spawned soldier template)").ToBase());
		}
		reader.TryRead("group", ref _group);
		reader.TryRead("listOrder", ref _listOrder);

		// _scriptValues: script hooks missing
	}

	/// <summary>
	/// Unicode::naturalCompare. C++ uses StrCmpLogicalW on Windows and a locale collation elsewhere; the port
	/// uses one deterministic order everywhere: digit runs by numeric value, other characters case-insensitive.
	/// </summary>
	private static int NaturalCompare(string a, string b)
	{
		int i = 0, j = 0;
		while (i < a.Length && j < b.Length)
		{
			if (char.IsAsciiDigit(a[i]) && char.IsAsciiDigit(b[j]))
			{
				int si = i, sj = j;
				while (si < a.Length && a[si] == '0') ++si;
				while (sj < b.Length && b[sj] == '0') ++sj;
				int ei = si, ej = sj;
				while (ei < a.Length && char.IsAsciiDigit(a[ei])) ++ei;
				while (ej < b.Length && char.IsAsciiDigit(b[ej])) ++ej;
				int cmp = (ei - si).CompareTo(ej - sj);
				if (cmp == 0)
					cmp = string.CompareOrdinal(a, si, b, sj, ei - si);
				if (cmp != 0)
					return cmp;
				i = ei;
				j = ej;
			}
			else
			{
				int cmp = char.ToUpperInvariant(a[i]).CompareTo(char.ToUpperInvariant(b[j]));
				if (cmp != 0)
					return cmp;
				++i;
				++j;
			}
		}
		int rest = (a.Length - i).CompareTo(b.Length - j);
		return rest != 0 ? rest : string.CompareOrdinal(a, b);
	}

	/// <summary>Cross link with other Rules.</summary>
	public void AfterLoad(Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		_totalSoldierNamePoolWeight = 0;
		foreach (var namepool in _names)
		{
			_totalSoldierNamePoolWeight += namepool.GlobalWeight;
		}
		if (_totalSoldierNamePoolWeight < 1)
		{
			Logger.Error($"{_type}: total soldier name pool weight is invalid. Forgotten 'soldierNames:' ?");
		}

		mod.LinkRule(ref _armor, ref _armorName);
		mod.CheckForSoftError(_armor == null, _type, "Soldier type is missing the default armor", SeverityLevel.LOG_ERROR);

		mod.VerifySoundOffset(_type, _deathSoundMale, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _deathSoundFemale, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _panicSoundMale, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _panicSoundFemale, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _berserkSoundMale, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _berserkSoundFemale, "BATTLE.CAT");

		mod.VerifySoundOffset(_type, _selectUnitSoundMale, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _selectUnitSoundFemale, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _startMovingSoundMale, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _startMovingSoundFemale, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _selectWeaponSoundMale, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _selectWeaponSoundFemale, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _annoyedSoundMale, "BATTLE.CAT");
		mod.VerifySoundOffset(_type, _annoyedSoundFemale, "BATTLE.CAT");

		mod.VerifySpriteOffset(_type, _rankSprite, "BASEBITS.PCK");
		mod.VerifySpriteOffset(_type, _rankSpriteBattlescape, "SMOKE.PCK");
		mod.VerifySpriteOffset(_type, _rankSpriteTiny, "TinyRanks");
		mod.VerifySpriteOffset(_type, _skillIconSprite, "SPICONS.DAT");

		if (_specWeaponName.Length != 0)
		{
			mod.LinkRule(ref _specWeapon, ref _specWeaponName);

			if ((_specWeapon.BattleType == BattleType.BT_FIREARM || _specWeapon.BattleType == BattleType.BT_MELEE) && _specWeapon.ClipSize == 0)
			{
				// as in C++ the name was already cleared by linkRule
				throw new InvalidOperationException("Weapon " + _specWeaponName + " is used as a special weapon, but doesn't have its own ammo - give it a clipSize!");
			}
		}
		mod.LinkRule(_skills, _skillNames);

		_manaMissingWoundThreshold = mod.ManaWoundThreshold;
		_healthMissingWoundThreshold = mod.HealthWoundThreshold;
	}

	private void AddSoldierNamePool(string namFile)
	{
		var pool = new SoldierNamePool();
		pool.Load(namFile);
		_names.Add(pool);
	}

	/// <summary>Gets the soldier's type (also its language string).</summary>
	public string Type { get => _type; }
	/// <summary>Gets the spawned soldier template.</summary>
	public string SpawnedSoldierTemplate { get => _spawnedSoldier; }
	/// <summary>Gets the soldier type group.</summary>
	public int Group { get => _group; }
	/// <summary>Gets whether or not the soldier type should be displayed in the inventory.</summary>
	public bool ShowTypeInInventory { get => _showTypeInInventory; }
	/// <summary>Gets the list/sort order of the soldier's type.</summary>
	public int ListOrder { get => _listOrder; }
	/// <summary>Gets the list of research required to acquire this soldier.</summary>
	public List<string> Requirements { get => _requires; }
	/// <summary>Gets the base functions required to hire this soldier type (a copy, like the C++ bitset by value).</summary>
	public RuleBaseFacilityFunctions RequiresBuyBaseFunc => _requiresBuyBaseFunc;
	/// <summary>Gets the allied country name required to hire this soldier type.</summary>
	public string RequiresBuyCountry { get => _requiresBuyCountry; }
	/// <summary>Gets the minimum stats for the random stats generator.</summary>
	public UnitStats MinStats { get => _minStats; }
	/// <summary>Gets the maximum stats for the random stats generator.</summary>
	public UnitStats MaxStats { get => _maxStats; }
	/// <summary>Gets the stat caps.</summary>
	public UnitStats StatCaps { get => _statCaps; }
	/// <summary>Gets the training stat caps.</summary>
	public UnitStats TrainingStatCaps { get => _trainingStatCaps; }
	/// <summary>Gets the improvement chances for pilots (after dogfight).</summary>
	public UnitStats DogfightExperience { get => _dogfightExperience; }
	/// <summary>Gets the monthly hiring limit.</summary>
	public int MonthlyBuyLimit { get => _monthlyBuyLimit; }
	/// <summary>Gets the monthly hiring limit message.</summary>
	public string MonthlyBuyLimitMessage { get => _monthlyBuyLimitMessage; }
	/// <summary>Gets the cost of hiring this soldier.</summary>
	public int BuyCost { get => _costBuy; }
	/// <summary>Does salary depend on rank?</summary>
	public bool IsSalaryDynamic { get => _costSalarySquaddie != 0 || _costSalarySergeant != 0 || _costSalaryCaptain != 0 || _costSalaryColonel != 0 || _costSalaryCommander != 0; }
	/// <summary>Gets the list of defined skills.</summary>
	public List<RuleSkill> Skills { get => _skills; }
	/// <summary>Gets the sprite index into SPICONS for the skill icon sprite.</summary>
	public int SkillIconSprite { get => _skillIconSprite; }

	/// <summary>Gets the cost of salary for a month (for a given rank).</summary>
	public int GetSalaryCost(int rank)
	{
		int total = _costSalary;
		switch (rank)
		{
			case 1: total += _costSalarySquaddie; break;
			case 2: total += _costSalarySergeant; break;
			case 3: total += _costSalaryCaptain; break;
			case 4: total += _costSalaryColonel; break;
			case 5: total += _costSalaryCommander; break;
			default: break;
		}
		return total;
	}

	/// <summary>Gets the height of the soldier when it's standing.</summary>
	public int StandHeight { get => _standHeight; }
	/// <summary>Gets the height of the soldier when it's kneeling.</summary>
	public int KneelHeight { get => _kneelHeight; }
	/// <summary>Gets the elevation of the soldier when it's flying.</summary>
	public int FloatHeight { get => _floatHeight; }
	/// <summary>Gets the default-equipped armor.</summary>
	public Armor DefaultArmor { get => _armor; }
	/// <summary>Gets the armor for avatar display.</summary>
	public string ArmorForAvatar { get => _armorForAvatar; }
	/// <summary>Gets the X offset used for avatar.</summary>
	public int AvatarOffsetX { get => _avatarOffsetX; }
	/// <summary>Gets the Y offset used for avatar.</summary>
	public int AvatarOffsetY { get => _avatarOffsetY; }
	/// <summary>Gets the flag offset.</summary>
	public int FlagOffset { get => _flagOffset; }
	/// <summary>Gets the special weapon.</summary>
	public RuleItem SpecialWeapon { get => _specWeapon; }
	/// <summary>Gets the allow promotion flag.</summary>
	public bool AllowPromotion { get => _allowPromotion; }
	/// <summary>Gets the allow piloting flag.</summary>
	public bool AllowPiloting { get => _allowPiloting; }
	/// <summary>Gets the female appearance ratio.</summary>
	public int FemaleFrequency { get => _femaleFrequency; }
	/// <summary>Gets the soldier's male death sounds.</summary>
	public List<int> MaleDeathSounds { get => _deathSoundMale; }
	/// <summary>Gets the soldier's female death sounds.</summary>
	public List<int> FemaleDeathSounds { get => _deathSoundFemale; }
	/// <summary>Gets the soldier's male panic sounds.</summary>
	public List<int> MalePanicSounds { get => _panicSoundMale; }
	/// <summary>Gets the soldier's female panic sounds.</summary>
	public List<int> FemalePanicSounds { get => _panicSoundFemale; }
	/// <summary>Gets the soldier's male berserk sounds.</summary>
	public List<int> MaleBerserkSounds { get => _berserkSoundMale; }
	/// <summary>Gets the soldier's female berserk sounds.</summary>
	public List<int> FemaleBerserkSounds { get => _berserkSoundFemale; }
	/// <summary>Gets the soldier's male "select unit" sounds.</summary>
	public List<int> MaleSelectUnitSounds { get => _selectUnitSoundMale; }
	/// <summary>Gets the soldier's female "select unit" sounds.</summary>
	public List<int> FemaleSelectUnitSounds { get => _selectUnitSoundFemale; }
	/// <summary>Gets the soldier's male "start moving" sounds.</summary>
	public List<int> MaleStartMovingSounds { get => _startMovingSoundMale; }
	/// <summary>Gets the soldier's female "start moving" sounds.</summary>
	public List<int> FemaleStartMovingSounds { get => _startMovingSoundFemale; }
	/// <summary>Gets the soldier's male "select weapon" sounds.</summary>
	public List<int> MaleSelectWeaponSounds { get => _selectWeaponSoundMale; }
	/// <summary>Gets the soldier's female "select weapon" sounds.</summary>
	public List<int> FemaleSelectWeaponSounds { get => _selectWeaponSoundFemale; }
	/// <summary>Gets the soldier's male "annoyed" sounds.</summary>
	public List<int> MaleAnnoyedSounds { get => _annoyedSoundMale; }
	/// <summary>Gets the soldier's female "annoyed" sounds.</summary>
	public List<int> FemaleAnnoyedSounds { get => _annoyedSoundFemale; }
	/// <summary>Gets the pool list for soldier names.</summary>
	public List<SoldierNamePool> Names { get => _names; }
	/// <summary>Gets the total weight of all the soldier name pools.</summary>
	public int TotalSoldierNamePoolWeight { get => _totalSoldierNamePoolWeight; }
	/// <summary>Gets the soldier's base value, without experience modifiers.</summary>
	public int Value { get => _value; }
	/// <summary>Gets the amount of time (in hours) this soldier takes to arrive at a base.</summary>
	public int TransferTime { get => _transferTime; }
	/// <summary>Percentage modifier for morale loss when this unit is killed.</summary>
	public int MoraleLossWhenKilled { get => _moraleLossWhenKilled; }
	/// <summary>Gets the list of StatStrings.</summary>
	public List<StatString> StatStrings { get => _statStrings; }
	/// <summary>Gets the list of strings for this soldier's ranks.</summary>
	public List<string> RankStrings { get => _rankStrings; }
	/// <summary>Gets the offset of the rank sprite in BASEBITS.PCK.</summary>
	public int RankSprite { get => _rankSprite; }
	/// <summary>Gets the offset of the rank sprite in SMOKE.PCK.</summary>
	public int RankSpriteBattlescape { get => _rankSpriteBattlescape; }
	/// <summary>Gets the offset of the rank sprite in TinyRanks.</summary>
	public int RankSpriteTiny { get => _rankSpriteTiny; }
	/// <summary>How much missing mana will act as "fatal wounds" and prevent the soldier from going into battle.</summary>
	public int ManaWoundThreshold { get => _manaMissingWoundThreshold; }
	/// <summary>How much missing health will act as "fatal wounds" and prevent the soldier from going into battle.</summary>
	public int HealthWoundThreshold { get => _healthMissingWoundThreshold; }
}
