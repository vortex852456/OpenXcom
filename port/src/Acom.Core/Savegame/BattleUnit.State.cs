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
// Ported from: src/Savegame/BattleUnit.h, src/Savegame/BattleUnit.cpp
#nullable disable

using static Acom.SoldierRank;
using static Acom.SpecialAbility;
using static Acom.UnitFaction;
using static Acom.UnitSide;
using static Acom.UnitStatus;

namespace Acom;

// Identity, faction, AI hooks, experience, rules data, spotting counters and flags.
public partial class BattleUnit
{
	/// <summary>Returns the BattleUnit's unique ID.</summary>
	public int Id => _id;

	/// <summary>Does the unit want to surrender?</summary>
	public bool WantsToSurrender() => _wantsToSurrender;

	/// <summary>Is the unit surrendering this turn?</summary>
	public bool IsSurrendering => _isSurrendering;

	/// <summary>Mark the unit as surrendering this turn.</summary>
	public void SetSurrendering(bool isSurrendering)
	{
		_isSurrendering = isSurrendering;
	}

	/// <summary>Gets the soldier's gender.</summary>
	public SoldierGender Gender => _gender;

	/// <summary>Returns the unit's faction (player, hostile or neutral).</summary>
	public UnitFaction Faction => _faction;

	/// <summary>
	/// Gets values used for recoloring sprites: pairs where first is color group to replace
	/// and second is new color group with shade.
	/// </summary>
	public List<(byte first, byte second)> Recolor => _recolor;

	/// <summary>Mark this unit as not reselectable.</summary>
	public void DontReselect()
	{
		_dontReselect = true;
	}

	/// <summary>Mark this unit as reselectable.</summary>
	public void AllowReselect()
	{
		_dontReselect = false;
	}

	/// <summary>Check whether reselecting this unit is allowed.</summary>
	public bool ReselectAllowed() => !_dontReselect;

	/// <summary>Let AI do their thing.</summary>
	/// <param name="action">AI action.</param>
	public void Think(BattleAction action)
	{
		ReloadAmmo();
		if (!_aiMedikitUsed)
		{
			// only perform once per turn
			_aiMedikitUsed = true;
			while (_currentAIState.Medikit_think(BattleMediKitType.BMT_HEAL)) { }
			while (_currentAIState.Medikit_think(BattleMediKitType.BMT_STIMULANT)) { }
		}
		_currentAIState.Think(action);
	}

	/// <summary>Gets or changes the current AI state.</summary>
	public AIModule AIModule
	{
		get => _currentAIState;
		set => _currentAIState = value; // C++ deletes the previous state
	}

	/// <summary>Gets weight value as hostile unit.</summary>
	public AIAttackWeight GetAITargetWeightAsHostile(Mod mod) => _armor.AITargetWeightAsHostile ?? mod.AITargetWeightAsHostile;

	/// <summary>Gets weight value as civilian unit when consider by aliens.</summary>
	public AIAttackWeight GetAITargetWeightAsHostileCivilians(Mod mod) => _armor.AITargetWeightAsHostileCivilians ?? mod.AITargetWeightAsHostileCivilians;

	/// <summary>Gets weight value as same faction unit.</summary>
	public AIAttackWeight GetAITargetWeightAsFriendly(Mod mod) => _armor.AITargetWeightAsFriendly ?? mod.AITargetWeightAsFriendly;

	/// <summary>Gets weight value as neutral unit (xcom to civ or vice versa).</summary>
	public AIAttackWeight GetAITargetWeightAsNeutral(Mod mod) => _armor.AITargetWeightAsNeutral ?? mod.AITargetWeightAsNeutral;

	/// <summary>Gets or sets the unit's creator.</summary>
	public BattleUnit PreviousOwner
	{
		get => _previousOwner;
		set => _previousOwner = value;
	}

	/// <summary>Adds one to the bravery exp counter.</summary>
	public void AddBraveryExp()
	{
		_exp.bravery++;
	}

	/// <summary>Adds one to the reaction exp counter.</summary>
	public void AddReactionExp()
	{
		_exp.reactions++;
	}

	/// <summary>Adds one to the firing exp counter.</summary>
	public void AddFiringExp()
	{
		_exp.firing++;
	}

	/// <summary>Adds one to the throwing exp counter.</summary>
	public void AddThrowingExp()
	{
		_exp.throwing++;
	}

	/// <summary>Adds one to the psi skill exp counter.</summary>
	public void AddPsiSkillExp()
	{
		_exp.psiSkill++;
	}

	/// <summary>Adds one to the psi strength exp counter.</summary>
	public void AddPsiStrengthExp()
	{
		_exp.psiStrength++;
	}

	/// <summary>Adds to the mana exp counter.</summary>
	public void AddManaExp(int weaponStat)
	{
		if (weaponStat > 0)
		{
			_exp.mana = (short)(_exp.mana + weaponStat / 100);
			if (Rng.Percent(weaponStat % 100))
			{
				_exp.mana++;
			}
		}
	}

	/// <summary>Adds one to the melee exp counter.</summary>
	public void AddMeleeExp()
	{
		_exp.melee++;
	}

	/// <summary>Did the unit gain any experience yet?</summary>
	public bool HasGainedAnyExperience
	{
		get
		{
			if (!Mod.EXTENDED_EXPERIENCE_AWARD_SYSTEM)
			{
				// vanilla compatibility (throwing doesn't count)
				return _exp.bravery != 0 || _exp.reactions != 0 || _exp.firing != 0 || _exp.psiSkill != 0 || _exp.psiStrength != 0 || _exp.melee != 0 || _exp.mana != 0;
			}
			return _exp.bravery != 0 || _exp.reactions != 0 || _exp.firing != 0 || _exp.psiSkill != 0 || _exp.psiStrength != 0 || _exp.melee != 0 || _exp.throwing != 0 || _exp.mana != 0;
		}
	}

	/// <summary>Updates the stats of a Geoscape soldier.</summary>
	public void UpdateGeoscapeStats(Soldier soldier)
	{
		soldier.AddMissionCount();
		soldier.AddKillCount(_kills);
	}

	/// <summary>
	/// Check if unit eligible for squaddie promotion. If yes, promote the unit.
	/// Increase the mission counter. Calculate the experience increases.
	/// </summary>
	/// <param name="mod">Mod rules.</param>
	/// <param name="geoscape">Pointer to geoscape save.</param>
	/// <param name="battle">The battle.</param>
	/// <param name="statsDiff">(out) The passed UnitStats struct will be filled with the stats differences.</param>
	/// <returns>True if the soldier was eligible for squaddie promotion.</returns>
	public bool PostMissionProcedures(Mod mod, SavedGame geoscape, SavedBattleGame battle, StatAdjustment statsDiff)
	{
		Soldier s = geoscape.GetSoldier(_id);
		if (s == null)
		{
			return false;
		}

		UpdateGeoscapeStats(s);

		ref UnitStats stats = ref s.CurrentStatsEditable;
		StatAdjustment statsOld = new StatAdjustment();
		statsOld.statGrowth = stats;
		statsDiff.statGrowth = -stats; // subtract old stat
		UnitStats caps = s.Rules.StatCaps;
		int manaLossOriginal = _stats.mana - _mana;
		int healthLossOriginal = _stats.health - _health;
		int manaLoss = mod.ReplenishManaAfterMission ? 0 : manaLossOriginal;
		int healthLoss = mod.ReplenishHealthAfterMission ? 0 : healthLossOriginal;

		var recovery = (int)Rng.Generate((healthLossOriginal * 0.5), (healthLossOriginal * 1.5));

		if (_exp.bravery != 0 && stats.bravery < caps.bravery)
		{
			if (_exp.bravery > Rng.Generate(0, 10)) stats.bravery += 10;
		}
		if (_exp.reactions != 0 && stats.reactions < caps.reactions)
		{
			stats.reactions = (short)(stats.reactions + ImproveStat(_exp.reactions));
		}
		if (_exp.firing != 0 && stats.firing < caps.firing)
		{
			stats.firing = (short)(stats.firing + ImproveStat(_exp.firing));
		}
		if (_exp.melee != 0 && stats.melee < caps.melee)
		{
			stats.melee = (short)(stats.melee + ImproveStat(_exp.melee));
		}
		if (_exp.throwing != 0 && stats.throwing < caps.throwing)
		{
			stats.throwing = (short)(stats.throwing + ImproveStat(_exp.throwing));
		}
		if (_exp.psiSkill != 0 && stats.psiSkill < caps.psiSkill)
		{
			stats.psiSkill = (short)(stats.psiSkill + ImproveStat(_exp.psiSkill));
		}
		if (_exp.psiStrength != 0 && stats.psiStrength < caps.psiStrength)
		{
			stats.psiStrength = (short)(stats.psiStrength + ImproveStat(_exp.psiStrength));
		}
		if (mod.IsManaTrainingPrimary)
		{
			if (_exp.mana != 0 && stats.mana < caps.mana)
			{
				stats.mana = (short)(stats.mana + ImproveStat(_exp.mana));
			}
		}

		bool hasImproved = false;
		if (HasGainedAnyExperience)
		{
			hasImproved = true;
			if (s.Rank == RANK_ROOKIE)
				s.PromoteRank();
			int v;
			v = caps.tu - stats.tu;
			if (v > 0) stats.tu = (short)(stats.tu + Rng.Generate(0, v / 10 + 2));
			v = caps.health - stats.health;
			if (v > 0) stats.health = (short)(stats.health + Rng.Generate(0, v / 10 + 2));
			if (mod.IsManaTrainingSecondary)
			{
				v = caps.mana - stats.mana;
				if (v > 0) stats.mana = (short)(stats.mana + Rng.Generate(0, v / 10 + 2));
			}
			v = caps.strength - stats.strength;
			if (v > 0) stats.strength = (short)(stats.strength + Rng.Generate(0, v / 10 + 2));
			v = caps.stamina - stats.stamina;
			if (v > 0) stats.stamina = (short)(stats.stamina + Rng.Generate(0, v / 15 + 2));
		}

		statsDiff.statGrowth += stats; // add new stat

		if (_armor.InstantWoundRecovery)
		{
			recovery = 0;
		}

		{
			var arg = new ScriptOutputArgs(recovery, manaLossOriginal, healthLossOriginal, manaLoss, healthLoss);

			ModScript.Execute(Armor.GetScript(ModScript.ReturnFromMissionUnit), arg, this, battle, s, statsDiff, statsOld);

			recovery = arg.GetInt(0);
			manaLoss = arg.GetInt(3);
			healthLoss = arg.GetInt(4);
		}

		//after mod execution this value could change
		statsDiff.statGrowth = stats - statsOld.statGrowth;

		s.SetWoundRecovery(recovery);
		s.ManaMissing = manaLoss;
		s.HealthMissing = healthLoss;

		if (s.IsWounded)
		{
			// remove from craft
			//s->setCraft(nullptr); // Note to self: we need to do this much later (as late as possible), so that we can correctly remove the items too (without side effects)

			// remove from training, but remember to return to training when healed
			{
				if (s.IsInTraining)
				{
					s.ReturnToTrainingWhenHealed = true;
				}
				s.SetTraining(false);
			}
		}

		return hasImproved;
	}

	/// <summary>Converts the number of experience to the stat increase.</summary>
	/// <param name="exp">Experience counter.</param>
	/// <returns>Stat increase.</returns>
#pragma warning disable CA1822 // C++ member function; kept as instance method for a 1:1 API
	private int ImproveStat(int exp)
#pragma warning restore CA1822
	{
		if (exp > 10) return Rng.Generate(2, 6);
		else if (exp > 5) return Rng.Generate(1, 4);
		else if (exp > 2) return Rng.Generate(1, 3);
		else if (exp > 0) return Rng.Generate(0, 1);
		else return 0;
	}

	/// <summary>
	/// Get the unit's minimap sprite index. Used to display the unit on the minimap:
	/// 0-2 Xcom soldier, 3-5 Alien, 6-8 Civilian, 9-11 Item, 12-23 Xcom HWP, 24-35 Alien big terror unit (cyberdisk, ...).
	/// </summary>
	public int MiniMapSpriteIndex
	{
		get
		{
			if (IsOut)
			{
				return 9;
			}
			switch (Faction)
			{
				case FACTION_HOSTILE:
					if (IsSmallUnit)
						return 3;
					else
						return 24;
				case FACTION_NEUTRAL:
					if (IsSmallUnit)
						return 6;
					else
						return 12;
				default:
					if (IsSmallUnit)
						return 0;
					else
						return 12;
			}
		}
	}

	/// <summary>Gets or sets the turret type. -1 is no turret.</summary>
	public int TurretType
	{
		get => _turretType;
		set => _turretType = value;
	}

	/// <summary>Get motion points for the motion scanner. More points is a larger blip on the scanner.</summary>
	public int MotionPoints => _motionPoints;

	/// <summary>Gets or sets the turn when unit was scanned by the motion scanner.</summary>
	public int ScannedTurn
	{
		get => _scannedTurn;
		set => _scannedTurn = value;
	}

	/// <summary>Gets or sets the unit custom marker.</summary>
	public int CustomMarker
	{
		get => _customMarker;
		set => _customMarker = value;
	}

	/// <summary>Gets the unit's armor.</summary>
	public Armor Armor => _armor;

	/// <summary>Set the unit's name.</summary>
	public void SetName(string name)
	{
		_name = name;
	}

	/// <summary>
	/// Get unit's name.
	/// An aliens name is the translation of it's race and rank.
	/// hence the language pointer needed.
	/// </summary>
	/// <param name="lang">Pointer to language.</param>
	/// <param name="debugAppendId">Append unit ID to name for debug purposes.</param>
	/// <returns>name String of the unit's name.</returns>
	public string GetName(Language lang, bool debugAppendId = false)
	{
		if (_type != "SOLDIER" && lang != null)
		{
			string ret;

			if (_type.Contains("STR_", StringComparison.Ordinal))
				ret = lang.GetString(_type);
			else
				ret = lang.GetString(_race);

			if (debugAppendId)
			{
				ret = ret + " " + _id.ToString(System.Globalization.CultureInfo.InvariantCulture);
			}
			return ret;
		}

		return _name;
	}

	/// <summary>Gets the unit's gained experience points.</summary>
	public ref readonly UnitStats ExpStats => ref _exp;

	/// <summary>Gets the unit's stats (C++ returns a mutable pointer).</summary>
	public ref UnitStats BaseStats => ref _stats;

	/// <summary>Get the unit's loft ID, one per unit tile. Each tile only has one loft, as it is repeated over the entire height of the unit.</summary>
	/// <param name="entry">Unit tile</param>
	/// <returns>The unit's line of fire template ID.</returns>
	public int GetLoftemps(int entry = 0) => _loftempsSet[entry];

	/// <summary>Get the unit's value. Used for score at debriefing.</summary>
	public int Value => _value;

	/// <summary>Get the reload sound (of the last reloaded weapon).</summary>
	public int ReloadSound => _lastReloadSound;

	/// <summary>Get the unit's death sounds.</summary>
	public List<int> DeathSounds => _deathSound;

	/// <summary>Gets the unit's "select unit" sounds.</summary>
	public List<int> SelectUnitSounds => _selectUnitSound;

	/// <summary>Gets the unit's "start moving" sounds.</summary>
	public List<int> StartMovingSounds => _startMovingSound;

	/// <summary>Gets the unit's "select weapon" sounds.</summary>
	public List<int> SelectWeaponSounds => _selectWeaponSound;

	/// <summary>Gets the unit's "annoyed" sounds.</summary>
	public List<int> AnnoyedSounds => _annoyedSound;

	/// <summary>Get the unit's move sound.</summary>
	public int MoveSound => _moveSound;

	/// <summary>Is this unit capable of shooting beyond max. visual range?</summary>
	public bool IsSniper => _unitRules != null && _unitRules.SniperPercentage > 0;

	/// <summary>Gets true when unit is 1x1 sized unit.</summary>
	public bool IsSmallUnit => _armor.Size == 1;

	/// <summary>Gets true when unit is 2x2 sized unit.</summary>
	public bool IsBigUnit => _armor.Size > 1;

	/// <summary>Get the number of turns an AI unit remembers a soldier's position.</summary>
	public int Intelligence => _intelligence;

	/// <summary>Get the unit's aggression.</summary>
	public int Aggression => _aggression;

	/// <summary>Returns the unit's special ability.</summary>
	public int SpecialAbility => (int)_specab;

	/// <summary>Helper method: view distance after a camouflage nerf and an anti-camouflage buff.</summary>
#pragma warning disable CA1822 // C++ member function; kept as instance method for a 1:1 API
	public int GetMaxViewDistance(int baseVisibility, int nerf, int buff)
#pragma warning restore CA1822
	{
		int result = baseVisibility;
		if (nerf > 0)
		{
			result = nerf; // fixed distance nerf
		}
		else
		{
			result += nerf; // relative distance nerf
		}
		if (result < 1)
		{
			result = 1; // can't go under melee distance
		}
		result += buff; // relative distance buff
		if (result > baseVisibility)
		{
			result = baseVisibility; // don't overbuff (buff is only supposed to counter the nerf)
		}
		return result;
	}

	/// <summary>Get maximum view distance at dark.</summary>
	public int GetMaxViewDistanceAtDark(BattleUnit otherUnit)
	{
		if (otherUnit != null)
		{
			return GetMaxViewDistance(_maxViewDistanceAtDark, otherUnit.Armor.CamouflageAtDark, _armor.AntiCamouflageAtDark);
		}
		else
		{
			return _maxViewDistanceAtDark;
		}
	}

	/// <summary>Max view distance at dark squared.</summary>
	public int MaxViewDistanceAtDarkSquared => _maxViewDistanceAtDarkSquared;

	/// <summary>Get maximum view distance at day.</summary>
	public int GetMaxViewDistanceAtDay(BattleUnit otherUnit)
	{
		if (otherUnit != null)
		{
			return GetMaxViewDistance(_maxViewDistanceAtDay, otherUnit.Armor.CamouflageAtDay, _armor.AntiCamouflageAtDay);
		}
		else
		{
			return _maxViewDistanceAtDay;
		}
	}

	/// <summary>Get unit psi vision with bonuses.</summary>
	public int PsiVision => _psiVision;

	/// <summary>Get unit heat vision with bonuses.</summary>
	public int VisibilityThroughSmoke => _visibilityThroughSmoke;

	/// <summary>Get unit visibility through fire with bonuses.</summary>
	public int VisibilityThroughFire => _visibilityThroughFire;

	/// <summary>Gets or sets the unit that is spawned when this one dies.</summary>
	public Unit SpawnUnit
	{
		get => _spawnUnit;
		set => _spawnUnit = value;
	}

	/// <summary>Gets or sets the faction of spawned unit.</summary>
	public UnitFaction SpawnUnitFaction
	{
		get => _spawnUnitFaction;
		set => _spawnUnitFaction = value;
	}

	/// <summary>Gets or sets this unit's respawn flag.</summary>
	public bool Respawn
	{
		get => _respawn;
		set => _respawn = value;
	}

	/// <summary>Gets or sets this unit's alreadyRespawned flag.</summary>
	public bool AlreadyRespawned
	{
		get => _alreadyRespawned;
		set => _alreadyRespawned = value;
	}

	/// <summary>Clear all information for spawn unit.</summary>
	public void ClearSpawnUnit()
	{
		SpawnUnit = null;
		SpawnUnitFaction = FACTION_HOSTILE;
		Respawn = false;
	}

	/// <summary>Get the units's rank string.</summary>
	public string RankString => _rank;

	/// <summary>Get the geoscape-soldier object.</summary>
	public Soldier GeoscapeSoldier => _geoscapeSoldier;

	/// <summary>Add a kill to the counter.</summary>
	public void AddKillCount()
	{
		_kills++;
	}

	/// <summary>Get unit type.</summary>
	public string Type => _type;

	/// <summary>Converts unit to another faction (original faction is still stored).</summary>
	public void ConvertToFaction(UnitFaction f)
	{
		_faction = f;
	}

	/// <summary>Gets whether the unit has any aggro sounds.</summary>
	public bool HasAggroSound => _aggroSound.Count != 0;

	/// <summary>Gets a unit's random aggro sound.</summary>
	public int RandomAggroSound
	{
		get
		{
			if (HasAggroSound)
			{
				return _aggroSound[Rng.Generate(0, _aggroSound.Count - 1)];
			}
			return -1;
		}
	}

	/// <summary>Get the faction the unit was killed by.</summary>
	public UnitFaction KilledBy() => _killedBy;

	/// <summary>Set the faction the unit was killed by.</summary>
	public void KilledBy(UnitFaction f)
	{
		_killedBy = f;
	}

	/// <summary>Gets or sets the units we are charging towards.</summary>
	public BattleUnit Charging
	{
		get => _charging;
		set => _charging = value;
	}

	/// <summary>Set default state on unit.</summary>
	public void ResetTurnsSince()
	{
		for (int i = 0; i < _turnsSinceSpotted.Length; ++i)
		{
			_turnsSinceSpotted[i] = 255;
		}
		for (int i = 0; i < _turnsLeftSpottedForSnipers.Length; ++i)
		{
			_turnsLeftSpottedForSnipers[i] = 0;
		}
		//_turnsSinceStunned is reset elsewhere
	}

	/// <summary>Update counters on unit.</summary>
	public void UpdateTurnsSince()
	{
		for (int i = 0; i < _turnsSinceSpotted.Length; ++i)
		{
			_turnsSinceSpotted[i] = (byte)Clamp(_turnsSinceSpotted[i] + 1, 0, 255);
		}
		for (int i = 0; i < _turnsLeftSpottedForSnipers.Length; ++i)
		{
			_turnsLeftSpottedForSnipers[i] = (byte)Clamp(_turnsLeftSpottedForSnipers[i] - 1, 0, 255);
		}
		//_turnsSinceStunned is updated elsewhere
	}

	/// <summary>Safe setter of value in array.</summary>
	private static void SetUint8Array(byte[] arr, int offset, int value)
	{
		if (0 <= offset && offset < arr.Length)
		{
			arr[offset] = (byte)Clamp(value, 0, 255);
		}
	}

	/// <summary>Safe getter of value in array.</summary>
	private static int GetUint8Array(byte[] arr, int offset)
	{
		if (0 <= offset && offset < arr.Length)
		{
			return arr[offset];
		}

		return 0;
	}

	/// <summary>Gets or sets how long since this unit was last exposed (to the aliens).</summary>
	public int TurnsSinceSpotted
	{
		get => _turnsSinceSpotted[(int)FACTION_HOSTILE];
		set => _turnsSinceSpotted[(int)FACTION_HOSTILE] = unchecked((byte)value);
	}

	/// <summary>Set how many turns this unit will be exposed for. For specific faction.</summary>
	public void SetTurnsSinceSpottedByFaction(UnitFaction faction, int turns)
	{
		SetUint8Array(_turnsSinceSpotted, (int)faction, turns);
	}

	/// <summary>Get how many turns this unit will be exposed for. For specific faction.</summary>
	public int GetTurnsSinceSpottedByFaction(UnitFaction faction) => GetUint8Array(_turnsSinceSpotted, (int)faction);

	/// <summary>Gets or sets how many turns left snipers will know about this unit.</summary>
	public int TurnsLeftSpottedForSnipers
	{
		get => _turnsLeftSpottedForSnipers[(int)FACTION_HOSTILE];
		set => _turnsLeftSpottedForSnipers[(int)FACTION_HOSTILE] = unchecked((byte)value);
	}

	/// <summary>Set how many turns left snipers know about this target. For specific faction.</summary>
	public void SetTurnsLeftSpottedForSnipersByFaction(UnitFaction faction, int turns)
	{
		SetUint8Array(_turnsLeftSpottedForSnipers, (int)faction, turns);
	}

	/// <summary>Get how many turns left snipers know about this target. For specific faction.</summary>
	public int GetTurnsLeftSpottedForSnipersByFaction(UnitFaction faction) => GetUint8Array(_turnsLeftSpottedForSnipers, (int)faction);

	/// <summary>Reset how many turns passed since stunned last time.</summary>
	public void ResetTurnsSinceStunned()
	{
		_turnsSinceStunned = 255;
	}

	/// <summary>Increase how many turns passed since stunned last time.</summary>
	public void IncTurnsSinceStunned()
	{
		_turnsSinceStunned = (byte)Math.Min(255, _turnsSinceStunned + 1);
	}

	/// <summary>Return how many turns passed since stunned last time.</summary>
	public int TurnsSinceStunned => _turnsSinceStunned;

	/// <summary>Get this unit's original Faction.</summary>
	public UnitFaction OriginalFaction => _originalFaction;

	/// <summary>Get alien/HWP unit.</summary>
	public Unit UnitRules => _unitRules;

	/// <summary>Get the list of units spotted this turn.</summary>
	public List<BattleUnit> UnitsSpottedThisTurn => _unitsSpottedThisTurn;

	/// <summary>Gets or changes the numeric version of the unit's rank (0 = lowest).</summary>
	public int RankInt
	{
		get => _rankInt;
		set => _rankInt = value;
	}

	/// <summary>Get the rank unified integer.</summary>
	public int RankIntUnified => _rankIntUnified;

	/// <summary>Derive the numeric unit rank from the string rank (for soldier units).</summary>
	public void DeriveSoldierRank()
	{
		if (_geoscapeSoldier != null)
		{
			_rankInt = _geoscapeSoldier.Rank switch
			{
				RANK_ROOKIE => 0,
				RANK_SQUADDIE => 1,
				RANK_SERGEANT => 2,
				RANK_CAPTAIN => 3,
				RANK_COLONEL => 4,
				RANK_COMMANDER => 5,
				_ => 0,
			};
		}
		_rankIntUnified = _rankInt;
	}

	/// <summary>Derive a rank integer based on rank string (for Alien).</summary>
	public void DeriveHostileRank()
	{
		const int max = 7;
		string[] rankList =
		[
			"STR_LIVE_SOLDIER",
			"STR_LIVE_ENGINEER",
			"STR_LIVE_MEDIC",
			"STR_LIVE_NAVIGATOR",
			"STR_LIVE_LEADER",
			"STR_LIVE_COMMANDER",
			"STR_LIVE_TERRORIST",
		];
		for (int i = 0; i < max; ++i)
		{
			if (_rank == rankList[i]) // _rank.compare(rankList[i]) == 0
			{
				_rankIntUnified = i;
				break;
			}
		}
	}

	/// <summary>Derive a rank integer based on rank string (for Civilians).</summary>
	public void DeriveNeutralRank()
	{
		_rankIntUnified = Rng.Seedless(0, 7);
	}

	/// <summary>Common function to adjust a unit's stats according to difficulty setting.</summary>
	/// <param name="adjustment">the stat adjustment variables coefficient value.</param>
	public void AdjustStats(StatAdjustment adjustment)
	{
		_stats += UnitStats.Percent(_stats, adjustment.statGrowth, adjustment.growthMultiplier);

		_stats.firing = (short)(_stats.firing * adjustment.aimMultiplier);
		_stats += adjustment.statGrowthAbs;

		for (int i = 0; i < (int)SIDE_MAX; ++i)
		{
			_maxArmor[i] = (int)(_maxArmor[i] * adjustment.armorMultiplier);
			_maxArmor[i] += adjustment.armorMultiplierAbs;
			_currentArmor[i] = _maxArmor[i];
		}

		// update base stats again as they could be altered by `adjustment`.
		_tu = _stats.tu;
		_energy = _stats.stamina;
		_health = _stats.health;
		_mana = _stats.mana;
	}

	/// <summary>Checks if this unit can be selected. Only alive units belonging to the faction can be selected.</summary>
	/// <param name="faction">The faction to compare with.</param>
	/// <param name="checkReselect">Check if the unit is reselectable.</param>
	/// <param name="checkInventory">Check if the unit has an inventory.</param>
	/// <returns>True if the unit can be selected, false otherwise.</returns>
	public bool IsSelectable(UnitFaction faction, bool checkReselect, bool checkInventory)
	{
		return _faction == faction && !IsOut && (!checkReselect || ReselectAllowed()) && (!checkInventory || HasInventory);
	}

	/// <summary>Checks if this unit has an inventory. Large units and/or terror units generally don't have inventories.</summary>
	public bool HasInventory => _armor.HasInventory;

	/// <summary>Checks if this unit is in hiding for a turn.</summary>
	public bool IsHiding => _hidingForTurn;

	/// <summary>Sets this unit is in hiding for a turn (or not).</summary>
	public void SetHiding(bool hiding)
	{
		_hidingForTurn = hiding;
	}

	/// <summary>Get the unit's mission statistics.</summary>
	public BattleUnitStatistics Statistics => _statistics;

	/// <summary>Gets or sets the unit murderer's id.</summary>
	public int MurdererId
	{
		get => _murdererId;
		set => _murdererId = value;
	}

	/// <summary>Gets or sets the unit murderer's weapon.</summary>
	public string MurdererWeapon
	{
		get => _murdererWeapon;
		set => _murdererWeapon = value;
	}

	/// <summary>Gets or sets the unit murderer's weapon's ammo.</summary>
	public string MurdererWeaponAmmo
	{
		get => _murdererWeaponAmmo;
		set => _murdererWeaponAmmo = value;
	}

	/// <summary>Gets or sets the unit mind controller's id.</summary>
	public int MindControllerId
	{
		get => _mindControllerID;
		set => _mindControllerID = value;
	}

	/// <summary>Get the unit leeroyJenkins flag (always charges enemy, never retreats).</summary>
	public bool IsLeeroyJenkins => _isLeeroyJenkins;

	/// <summary>Gets the spotter score. Determines how many turns sniper AI units can act on this unit seeing your troops.</summary>
	public int SpotterDuration
	{
		get
		{
			if (_unitRules != null)
			{
				return _unitRules.SpotterDuration;
			}
			return 0;
		}
	}

	/// <summary>Remembers the unit's XP (used for shotguns).</summary>
	public void RememberXP()
	{
		_expTmp = _exp;
	}

	/// <summary>Artificially alter a unit's XP (used for shotguns).</summary>
	public void NerfXP()
	{
		_exp = UnitStats.Min(_exp, _expTmp + UnitStats.Scalar(1));
	}

	/// <summary>Did this unit explode already?</summary>
	public bool HasAlreadyExploded => _alreadyExploded;

	/// <summary>Set the already exploded flag.</summary>
	public void SetAlreadyExploded(bool alreadyExploded)
	{
		_alreadyExploded = alreadyExploded;
	}

	/// <summary>Gets or sets the unconscious/dead notification shown flag.</summary>
	public int NotificationShown
	{
		get => _notificationShown;
		set => _notificationShown = value;
	}

	/// <summary>Gets whether this unit can be captured alive (applies to aliens).</summary>
	public bool Capturable => _capturable;

	/// <summary>Free up the patrol node target, to allow others to use it.</summary>
	public void FreePatrolTarget()
	{
		_currentAIState?.FreePatrolTarget();
	}

	/// <summary>Marks this unit as summoned by an item or not.</summary>
	public void SetSummonedPlayerUnit(bool summonedPlayerUnit)
	{
		_summonedPlayerUnit = summonedPlayerUnit;
	}

	/// <summary>Was this unit summoned by an item? (won't count for recovery or total player units left)</summary>
	public bool IsSummonedPlayerUnit => _summonedPlayerUnit;

	/// <summary>Should this unit (player, alien or civilian) be ignored for various things related to soldier diaries and commendations?</summary>
	public bool IsCosmetic => _unitRules != null && _unitRules.IsCosmetic;

	/// <summary>Should this AI unit (alien or civilian) be ignored by other AI units?</summary>
	public bool IsIgnoredByAI => _unitRules != null && _unitRules.IsIgnoredByAI;

	/// <summary>Marks this unit as resummoned fake civilian and therefore won't count for civilian scoring in the Debriefing.</summary>
	public void MarkAsResummonedFakeCivilian()
	{
		_resummonedFakeCivilian = true;
		_status = STATUS_IGNORE_ME;
	}

	/// <summary>Is this unit a resummoned fake civilian?</summary>
	public bool IsResummonedFakeCivilian => _resummonedFakeCivilian;

	/// <summary>Marks this unit as VIP.</summary>
	public void MarkAsVIP()
	{
		_vip = true;
	}

	/// <summary>Is this a VIP unit?</summary>
	public bool IsVIP => _vip;

	/// <summary>Is this unit banned in the next stage?</summary>
	public bool IsBannedInNextStage => _bannedInNextStage;

	/// <summary>Is at least one soldier skill usable? (i.e. shown in the skill menu)</summary>
	public bool SkillMenuCheck() => _skillMenuCheck;

	/// <summary>Is the unit eagerly picking up weapons?</summary>
	public bool PickUpWeaponsMoreActively => _pickUpWeaponsMoreActively;

	/// <summary>Is the unit afraid to pathfind through fire? True if this unit has a penalty when pathfinding through fire.</summary>
	public bool AvoidsFire()
	{
		if (_unitRules != null)
		{
			return _unitRules.AvoidsFire();
		}
		return _specab < SPECAB_BURNFLOOR;
	}

	/// <summary>Show indicators for this unit or not?</summary>
	public bool IndicatorsAreEnabled() => !_disableIndicators;

	/// <summary>Disable showing indicators for this unit.</summary>
	public void DisableIndicators()
	{
		_disableIndicators = true;
	}
}
