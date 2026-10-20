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
// Ported from: src/Mod/RuleBaseFacility.h, src/Mod/RuleBaseFacility.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents a specific type of base facility.
/// Contains constant info about a facility like
/// costs, capacities, size, etc.
/// </summary>
/// <seealso cref="BaseFacility"/>
public class RuleBaseFacility
{
	private string _ufopediaType = "";
	private readonly string _type;
	private readonly List<string> _requires = [];
	private RuleBaseFacilityFunctions _requiresBaseFunc;
	private RuleBaseFacilityFunctions _provideBaseFunc;
	private RuleBaseFacilityFunctions _forbiddenBaseFunc;
	private int _spriteShape, _spriteFacility;
	private bool _connectorsDisabled;
	private int _missileAttraction;
	private int _fakeUnderwater;
	private bool _lift, _hyper, _mind, _grav;
	private int _mindPower;
	private int _sizeX, _sizeY;
	private int _buildCost, _refundValue, _buildTime, _monthlyCost;
	private readonly SortedDictionary<string, (int first, int second)> _buildCostItems = new(StringComparer.Ordinal);
	private int _storage, _personnel, _aliens, _crafts, _labs, _workshops, _psiLabs;
	private bool _spriteEnabled;
	private int _sightRange, _sightChance;
	private int _radarRange, _radarChance, _defense, _hitRatio, _fireSound, _hitSound, _placeSound;
	private int _ammoMax, _rearmRate;
	private int _ammoNeeded;
	private bool _unifiedDamageFormula;
	private int _shieldDamageModifier;
	private RuleItem _ammoItem;
	private string _ammoItemName = "";
	private string _mapName = "";
	private int _listOrder, _trainingRooms;
	private int _maxAllowedPerBase;
	private int _manaRecoveryPerDay;
	private int _healthRecoveryPerDay;
	private float _sickBayAbsoluteBonus, _sickBayRelativeBonus;
	private int _prisonType;
	private int _rightClickActionType;
	private readonly List<VerticalLevel> _verticalLevels = [];
	private readonly List<RuleBaseFacility> _leavesBehindOnSell = [];
	private int _removalTime;
	private bool _canBeBuiltOver;
	private bool _upgradeOnly;
	private readonly List<RuleBaseFacility> _buildOverFacilities = [];
	private List<Position> _storageTiles = [];
	private string _destroyedFacilityName = "";
	private RuleBaseFacility _destroyedFacility;

	private readonly List<string> _leavesBehindOnSellNames = [];
	private readonly List<string> _buildOverFacilitiesNames = [];

	/// <summary>Creates a blank ruleset for a certain type of base facility.</summary>
	/// <param name="type">String defining the type.</param>
	/// <param name="listOrder">The list weight for this facility.</param>
	public RuleBaseFacility(string type, int listOrder)
	{
		_type = type;
		_spriteShape = -1;
		_spriteFacility = -1;
		_connectorsDisabled = false;
		_missileAttraction = 100;
		_fakeUnderwater = -1;
		_lift = false;
		_hyper = false;
		_mind = false;
		_grav = false;
		_mindPower = 1;
		_sizeX = 1;
		_sizeY = 1;
		_buildCost = 0;
		_refundValue = 0;
		_buildTime = 0;
		_monthlyCost = 0;
		_storage = 0;
		_personnel = 0;
		_aliens = 0;
		_crafts = 0;
		_labs = 0;
		_workshops = 0;
		_psiLabs = 0;
		_spriteEnabled = false;
		_sightRange = 0;
		_sightChance = 0;
		_radarRange = 0;
		_radarChance = 0;
		_defense = 0;
		_hitRatio = 0;
		_fireSound = 0;
		_hitSound = 0;
		_placeSound = -1;
		_ammoMax = 0;
		_rearmRate = 1;
		_ammoNeeded = 1;
		_unifiedDamageFormula = false;
		_shieldDamageModifier = 100;
		_listOrder = listOrder;
		_trainingRooms = 0;
		_maxAllowedPerBase = 0;
		_sickBayAbsoluteBonus = 0.0f;
		_sickBayRelativeBonus = 0.0f;
		_prisonType = 0;
		_rightClickActionType = 0;
		_removalTime = 0;
		_canBeBuiltOver = false;
		_upgradeOnly = false;
		_destroyedFacility = null;
	}

	/// <summary>Loads the base facility type from YAML.</summary>
	/// <param name="node">YAML node.</param>
	/// <param name="mod">Mod for the facility.</param>
	public void Load(YamlNodeReader node, Mod mod)
	{
		var reader = node.UseIndex();
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent, mod);
		}

		reader.TryRead("ufopediaType", ref _ufopediaType);

		mod.LoadUnorderedNames(_type, _requires, reader["requires"]);

		mod.LoadBaseFunction(_type, ref _requiresBaseFunc, reader["requiresBaseFunc"]);
		mod.LoadBaseFunction(_type, ref _provideBaseFunc, reader["provideBaseFunc"]);
		mod.LoadBaseFunction(_type, ref _forbiddenBaseFunc, reader["forbiddenBaseFunc"]);

		mod.LoadSpriteOffset(_type, ref _spriteShape, reader["spriteShape"], "BASEBITS.PCK");
		mod.LoadSpriteOffset(_type, ref _spriteFacility, reader["spriteFacility"], "BASEBITS.PCK");

		reader.TryRead("connectorsDisabled", ref _connectorsDisabled);

		reader.TryRead("fakeUnderwater", ref _fakeUnderwater);
		reader.TryRead("missileAttraction", ref _missileAttraction);
		reader.TryRead("lift", ref _lift);
		reader.TryRead("hyper", ref _hyper);
		reader.TryRead("mind", ref _mind);
		reader.TryRead("grav", ref _grav);
		reader.TryRead("mindPower", ref _mindPower);
		if (reader["size"])
		{
			// backwards-compatibility
			reader.TryRead("size", ref _sizeX);
			reader.TryRead("size", ref _sizeY);
		}
		reader.TryRead("sizeX", ref _sizeX);
		reader.TryRead("sizeY", ref _sizeY);
		reader.TryRead("buildCost", ref _buildCost);
		reader.TryRead("refundValue", ref _refundValue);
		reader.TryRead("buildTime", ref _buildTime);
		reader.TryRead("monthlyCost", ref _monthlyCost);
		reader.TryRead("storage", ref _storage);
		reader.TryRead("personnel", ref _personnel);
		reader.TryRead("aliens", ref _aliens);
		reader.TryRead("crafts", ref _crafts);
		reader.TryRead("labs", ref _labs);
		reader.TryRead("workshops", ref _workshops);
		reader.TryRead("psiLabs", ref _psiLabs);

		reader.TryRead("spriteEnabled", ref _spriteEnabled);

		reader.TryRead("sightRange", ref _sightRange);
		reader.TryRead("sightChance", ref _sightChance);
		reader.TryRead("radarRange", ref _radarRange);
		reader.TryRead("radarChance", ref _radarChance);
		reader.TryRead("defense", ref _defense);
		reader.TryRead("hitRatio", ref _hitRatio);

		mod.LoadSoundOffset(_type, ref _fireSound, reader["fireSound"], "GEO.CAT");
		mod.LoadSoundOffset(_type, ref _hitSound, reader["hitSound"], "GEO.CAT");
		mod.LoadSoundOffset(_type, ref _placeSound, reader["placeSound"], "GEO.CAT");

		reader.TryRead("ammoMax", ref _ammoMax);
		reader.TryRead("rearmRate", ref _rearmRate);
		reader.TryRead("ammoNeeded", ref _ammoNeeded);
		reader.TryRead("unifiedDamageFormula", ref _unifiedDamageFormula);
		reader.TryRead("shieldDamageModifier", ref _shieldDamageModifier);
		reader.TryRead("ammoItem", ref _ammoItemName);
		reader.TryRead("mapName", ref _mapName);
		reader.TryRead("listOrder", ref _listOrder);
		reader.TryRead("trainingRooms", ref _trainingRooms);
		reader.TryRead("maxAllowedPerBase", ref _maxAllowedPerBase);
		reader.TryRead("manaRecoveryPerDay", ref _manaRecoveryPerDay);
		reader.TryRead("healthRecoveryPerDay", ref _healthRecoveryPerDay);
		reader.TryRead("sickBayAbsoluteBonus", ref _sickBayAbsoluteBonus);
		reader.TryRead("sickBayRelativeBonus", ref _sickBayRelativeBonus);
		reader.TryRead("prisonType", ref _prisonType);
		reader.TryRead("rightClickActionType", ref _rightClickActionType);

		var items = reader["buildCostItems"];
		if (items)
		{
			foreach (var buildCostReader in items.Children())
			{
				string id = buildCostReader.ReadKey<string>();
				_buildCostItems.TryGetValue(id, out var cost); // std::map::operator[] gives (0, 0) for a new id

				buildCostReader.TryRead("build", ref cost.first);
				buildCostReader.TryRead("refund", ref cost.second);

				if (cost.first <= 0 && cost.second <= 0)
				{
					_buildCostItems.Remove(id);
				}
				else
				{
					_buildCostItems[id] = cost;
				}
			}
		}

		// Load any VerticalLevels into a map if we have them
		if (reader["verticalLevels"])
		{
			_verticalLevels.Clear();
			foreach (var levelReader in reader["verticalLevels"].Children())
			{
				if (levelReader["type"])
				{
					var level = new VerticalLevel();
					level.Load(levelReader);
					_verticalLevels.Add(level);
				}
			}
		}

		mod.LoadNames(_type, _leavesBehindOnSellNames, reader["leavesBehindOnSell"]);
		reader.TryRead("removalTime", ref _removalTime);
		reader.TryRead("canBeBuiltOver", ref _canBeBuiltOver);
		reader.TryRead("upgradeOnly", ref _upgradeOnly);
		mod.LoadUnorderedNames(_type, _buildOverFacilitiesNames, reader["buildOverFacilities"]);
		// C++ sorts _buildOverFacilities by pointer here (still empty before afterLoad); see AfterLoad.

		reader.TryRead("storageTiles", ref _storageTiles);
		reader.TryRead("destroyedFacility", ref _destroyedFacilityName);
	}

	/// <summary>Cross link with other rules.</summary>
	public void AfterLoad(Mod mod)
	{
		mod.VerifySpriteOffset(_type, _spriteShape, "BASEBITS.PCK");
		mod.VerifySpriteOffset(_type, _spriteFacility, "BASEBITS.PCK");
		mod.VerifySoundOffset(_type, _fireSound, "GEO.CAT");
		mod.VerifySoundOffset(_type, _hitSound, "GEO.CAT");
		mod.VerifySoundOffset(_type, _placeSound, "GEO.CAT");

		mod.LinkRule(ref _ammoItem, ref _ammoItemName);

		if (_unifiedDamageFormula && _ammoItem == null)
		{
			throw new InvalidOperationException("Unified damage formula requires `ammoItem` to be defined.");
		}

		if (!string.IsNullOrEmpty(_destroyedFacilityName))
		{
			mod.LinkRule(ref _destroyedFacility, ref _destroyedFacilityName);
			if (_destroyedFacility.SizeX != _sizeX || _destroyedFacility.SizeY != _sizeY)
			{
				throw new InvalidOperationException("Destroyed version of a facility must have the same size as the original facility.");
			}
		}
		if (_leavesBehindOnSellNames.Count != 0)
		{
			var first = mod.GetBaseFacility(_leavesBehindOnSellNames[0], true);
			if (first.SizeX == _sizeX && first.SizeY == _sizeY)
			{
				if (_leavesBehindOnSellNames.Count != 1)
				{
					throw new InvalidOperationException("Only one replacement facility allowed (when using the same size as the original facility).");
				}
				_leavesBehindOnSell.Add(first);
			}
			else
			{
				foreach (var n in _leavesBehindOnSellNames)
				{
					var r = mod.GetBaseFacility(n, true);
					if (!r.IsSmall)
					{
						throw new InvalidOperationException("All replacement facilities must have size=1 (when using different size as the original facility).");
					}
					_leavesBehindOnSell.Add(r);
				}
			}
		}
		if (_buildOverFacilitiesNames.Count != 0)
		{
			mod.LinkRule(_buildOverFacilities, _buildOverFacilitiesNames);
			// Deviation: C++ sorts by pointer address for a binary search (Collections::sortVector);
			// the list keeps the ruleset order here and GetCanBuildOverOtherFacility searches linearly.
		}
		if (string.IsNullOrEmpty(_mapName))
		{
			throw new InvalidOperationException("Battlescape map name is missing.");
		}
		if (_storageTiles.Count > 0)
		{
			if (_storageTiles.Count != 1 || _storageTiles[0] != TileEngine.invalid)
			{
				int sizeX = 10 * _sizeX;
				int sizeY = 10 * _sizeY;
				foreach (var p in _storageTiles)
				{
					if (p.x < 0 || p.x > sizeX ||
						p.y < 0 || p.y > sizeY ||
						p.z < 0 || p.z > 8) // accurate max z will be check during map creation when we know map heigth, now we only check for very bad values.
					{
						if (p == TileEngine.invalid)
						{
							throw new InvalidOperationException("Invalid tile position (-1, -1, -1) can be only one in storage position list.");
						}
						else
						{
							throw new InvalidOperationException("Tile position (" + p.x + ", " + p.y + ", " + p.z + ") is outside the facility area.");
						}
					}
				}
			}
		}

		_leavesBehindOnSellNames.Clear();
	}

	/// <summary>Gets the custom name of the Ufopedia article related to this facility.</summary>
	public string UfopediaType => !string.IsNullOrEmpty(_ufopediaType) ? _ufopediaType : _type;

	/// <summary>Gets the language string that names this base facility. Each base facility type has a unique name.</summary>
	public string Type => _type;

	/// <summary>Gets the list of research required to build this base facility.</summary>
	public List<string> Requirements => _requires;

	/// <summary>Gets the facility's required function in base to build.</summary>
	public RuleBaseFacilityFunctions RequireBaseFunc => _requiresBaseFunc;

	/// <summary>Gets the functions that facility provide in base.</summary>
	public RuleBaseFacilityFunctions ProvidedBaseFunc => _provideBaseFunc;

	/// <summary>Gets the functions that facility prevent in base.</summary>
	public RuleBaseFacilityFunctions ForbiddenBaseFunc => _forbiddenBaseFunc;

	/// <summary>Gets the ID of the sprite used to draw the base structure of the facility that defines its shape.</summary>
	public int SpriteShape => _spriteShape;

	/// <summary>Gets the ID of the sprite used to draw the facility's contents inside the base shape.</summary>
	public int SpriteFacility => _spriteFacility;

	/// <summary>Retain sprite over shape behavior for bigger facility?</summary>
	public bool SpriteEnabled => IsSmall || _spriteEnabled;

	public bool SpriteEnabledRaw => _spriteEnabled;

	/// <summary>Should there be connectors leading to this facility?</summary>
	public bool ConnectorsDisabled() => _connectorsDisabled;

	/// <summary>Gets if the facility's size is 1x1.</summary>
	public bool IsSmall => _sizeX == 1 && _sizeY == 1;

	/// <summary>Gets the facility's size.</summary>
	public int SizeX => _sizeX;

	/// <summary>Gets the facility's size.</summary>
	public int SizeY => _sizeY;

	/// <summary>Gets the facility's missile attraction.</summary>
	public int MissileAttraction => _missileAttraction;

	/// <summary>Is this facility allowed for a given type of base?</summary>
	public bool IsAllowedForBaseType(bool fakeUnderwaterBase)
	{
		if (_fakeUnderwater == -1)
			return true;
		else if (_fakeUnderwater == 0 && !fakeUnderwaterBase)
			return true;
		else if (_fakeUnderwater == 1 && fakeUnderwaterBase)
			return true;

		return false;
	}

	public int FakeUnderwaterRaw => _fakeUnderwater;

	/// <summary>Checks if this facility is the core access lift of a base. Every base has an access lift and all facilities have to be connected to it.</summary>
	public bool IsLift => _lift;

	/// <summary>Checks if this facility has hyperwave detection capabilities.</summary>
	public bool IsHyperwave => _hyper;

	/// <summary>Checks if this facility has a mind shield, which covers your base from alien detection.</summary>
	public bool IsMindShield => _mind;

	/// <summary>Gets the mind shield power.</summary>
	public int MindShieldPower => _mindPower;

	/// <summary>Checks if this facility has a grav shield, which doubles base defense's fire ratio.</summary>
	public bool IsGravShield => _grav;

	/// <summary>Gets the amount of funds that this facility costs to build on a base.</summary>
	public int BuildCost => _buildCost;

	/// <summary>Gets the amount that is refunded when the facility is dismantled.</summary>
	public int RefundValue => _refundValue;

	/// <summary>Gets the facility's construction cost in items, <c>first</c> is build cost, <c>second</c> is refund.</summary>
	public SortedDictionary<string, (int first, int second)> BuildCostItems => _buildCostItems;

	/// <summary>Gets the amount of time that this facility takes to be constructed since placement.</summary>
	public int BuildTime => _buildTime;

	/// <summary>Gets the amount of funds this facility costs monthly to maintain once it's fully built.</summary>
	public int MonthlyCost => _monthlyCost;

	/// <summary>Gets the amount of storage space this facility provides for base equipment.</summary>
	public int Storage => _storage;

	/// <summary>Gets the number of base personnel (soldiers, scientists, engineers) this facility can contain.</summary>
	public int Personnel => _personnel;

	/// <summary>Gets the number of captured live aliens this facility can contain.</summary>
	public int Aliens => _aliens;

	/// <summary>Gets the number of base craft this facility can contain.</summary>
	public int Crafts => _crafts;

	/// <summary>Gets the amount of laboratory space this facility provides for research projects.</summary>
	public int Laboratories => _labs;

	/// <summary>Gets the amount of workshop space this facility provides for manufacturing projects.</summary>
	public int Workshops => _workshops;

	/// <summary>Gets the number of soldiers this facility can contain for monthly psi-training.</summary>
	public int PsiLaboratories => _psiLabs;

	/// <summary>Gets the facility's sight range.</summary>
	public int SightRange => _sightRange;

	/// <summary>Gets the facility's alien base detection chance.</summary>
	public int SightChance => _sightChance;

	/// <summary>Gets the radar range this facility provides for the detection of UFOs.</summary>
	public int RadarRange => _radarRange;

	/// <summary>Gets the chance of UFOs that come within the facility's radar range being detected.</summary>
	public int RadarChance => _radarChance;

	/// <summary>Gets the defense value of this facility's weaponry against UFO invasions on the base.</summary>
	public int DefenseValue => _defense;

	/// <summary>Gets the hit ratio of this facility's weaponry against UFO invasions on the base.</summary>
	public int HitRatio => _hitRatio;

	/// <summary>Gets the facility's weapon ammo capacity.</summary>
	public int AmmoMax => _ammoMax;

	/// <summary>Gets the facility's weapon rearm rate.</summary>
	public int RearmRate => _rearmRate;

	/// <summary>Gets the facility's weapon ammo spent per shot.</summary>
	public int AmmoNeeded => _ammoNeeded;

	/// <summary>Should unified or vanilla formula be used?</summary>
	public bool UnifiedDamageFormula() => _unifiedDamageFormula;

	/// <summary>Gets the facility's weapon effectiveness against shields.</summary>
	public int ShieldDamageModifier => _shieldDamageModifier;

	/// <summary>Gets the facility's weapon ammo item.</summary>
	public RuleItem AmmoItem => _ammoItem;

	/// <summary>Gets the battlescape map block name for this facility to construct the base defense mission map.</summary>
	public string MapName => _mapName;

	/// <summary>Gets the facility's fire sound.</summary>
	public int FireSound => _fireSound;

	/// <summary>Gets the facility's hit sound.</summary>
	public int HitSound => _hitSound;

	/// <summary>Gets the facility's place sound.</summary>
	public int PlaceSound => _placeSound;

	/// <summary>Gets the facility's list weight.</summary>
	public int ListOrder => _listOrder;

	/// <summary>Returns the amount of soldiers this facility can contain for monthly training.</summary>
	public int TrainingFacilities => _trainingRooms;

	/// <summary>Gets the maximum allowed number of facilities per base.</summary>
	public int MaxAllowedPerBase => _maxAllowedPerBase;

	/// <summary>Gets the facility's mana recovery rate.</summary>
	public int ManaRecoveryPerDay => _manaRecoveryPerDay;

	/// <summary>Gets the facility's health recovery rate.</summary>
	public int HealthRecoveryPerDay => _healthRecoveryPerDay;

	/// <summary>Gets the facility's bonus to wound healing.</summary>
	public float SickBayAbsoluteBonus => _sickBayAbsoluteBonus;

	/// <summary>Gets the facility's bonus to wound healing (as percentage of max hp of the soldier).</summary>
	public float SickBayRelativeBonus => _sickBayRelativeBonus;

	/// <summary>Gets the prison type: 0=alien containment, 1=prison, 2=animal cages, etc.</summary>
	public int PrisonType => _prisonType;

	/// <summary>Gets the action type to perform on right click: 0=default, 1 = prison, 2 = manufacture, 3 = research, 4 = training, 5 = psi training, 6 = soldiers, 7 = sell.</summary>
	public int RightClickActionType => _rightClickActionType;

	/// <summary>Gets the vertical levels for this facility map generation.</summary>
	public List<VerticalLevel> VerticalLevels => _verticalLevels;

	/// <summary>Gets the facility left behind when this one is sold.</summary>
	public List<RuleBaseFacility> LeavesBehindOnSell => _leavesBehindOnSell;

	/// <summary>Gets how long facilities left behind when this one is sold should take to build.</summary>
	public int RemovalTime => _removalTime;

	/// <summary>Gets whether or not this facility can be built over by other ones.</summary>
	public bool CanBeBuiltOver => _canBeBuiltOver;

	/// <summary>Gets whether or not this facility can ONLY be built over another facility (i.e. not standalone).</summary>
	public bool IsUpgradeOnly => _upgradeOnly;

	/// <summary>Check if a given facility <paramref name="fac"/> can be replaced by this facility.</summary>
	public BasePlacementErrors GetCanBuildOverOtherFacility(RuleBaseFacility fac)
	{
		ArgumentNullException.ThrowIfNull(fac);
		if (fac.CanBeBuiltOver == true)
		{
			// the old facility allows unrestricted build-over.
			return BasePlacementErrors.BPE_None;
		}
		else if (_buildOverFacilities.Count == 0)
		{
			// the old facility does not allow unrestricted build-over and we do not have any exception list
			return BasePlacementErrors.BPE_UpgradeDisallowed;
		}
		else if (_buildOverFacilities.Contains(fac))
		{
			// the old facility is on the exception list
			return BasePlacementErrors.BPE_None;
		}
		else
		{
			// we have an exception list, but this facility is not on it.
			return BasePlacementErrors.BPE_UpgradeRequireSpecific;
		}
	}

	/// <summary>Gets which facilities are allowed to be replaced by this building.</summary>
	public List<RuleBaseFacility> BuildOverFacilities => _buildOverFacilities;

	/// <summary>Gets a list of which tiles are used to place items stored in this facility.</summary>
	public List<Position> StorageTiles => _storageTiles;

	/// <summary>Gets the ruleset for the destroyed version of this facility.</summary>
	public RuleBaseFacility DestroyedFacility => _destroyedFacility;
}
