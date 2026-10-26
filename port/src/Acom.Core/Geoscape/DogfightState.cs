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
// Ported from: src/Geoscape/DogfightState.h, src/Geoscape/DogfightState.cpp
#nullable disable

using System.Globalization;
using System.Runtime.CompilerServices;

namespace Acom;

public enum ColorNames : uint
{
	CRAFT_MIN = 0,
	CRAFT_MAX = 1,
	RADAR_MIN = 2,
	RADAR_MAX = 3,
	DAMAGE_MIN = 4,
	DAMAGE_MAX = 5,
	BLOB_MIN = 6,
	RANGE_METER = 7,
	DISABLED_WEAPON = 8,
	DISABLED_AMMO = 9,
	DISABLED_RANGE = 10,
	SHIELD_MIN = 11,
	SHIELD_MAX = 12,
}

/// <summary>
/// Shows a dogfight (interception) between a
/// player craft and an UFO.
/// </summary>
public class DogfightState : State
{
	public const int STANDOFF_DIST = 560;
	public const int AGGRESSIVE_DIST = 64;

	private const int CRAFT_MIN = (int)ColorNames.CRAFT_MIN;
	private const int CRAFT_MAX = (int)ColorNames.CRAFT_MAX;
	private const int RADAR_MIN = (int)ColorNames.RADAR_MIN;
	private const int RADAR_MAX = (int)ColorNames.RADAR_MAX;
	private const int DAMAGE_MIN = (int)ColorNames.DAMAGE_MIN;
	private const int DAMAGE_MAX = (int)ColorNames.DAMAGE_MAX;
	private const int BLOB_MIN = (int)ColorNames.BLOB_MIN;
	private const int RANGE_METER = (int)ColorNames.RANGE_METER;
	private const int DISABLED_WEAPON = (int)ColorNames.DISABLED_WEAPON;
	private const int DISABLED_AMMO = (int)ColorNames.DISABLED_AMMO;
	private const int DISABLED_RANGE = (int)ColorNames.DISABLED_RANGE;
	private const int SHIELD_MIN = (int)ColorNames.SHIELD_MIN;
	private const int SHIELD_MAX = (int)ColorNames.SHIELD_MAX;

	private const int WeaponMax = RuleCraft.WeaponMax;

	private readonly GeoscapeState _state;
	private readonly Timer _craftDamageAnimTimer;
	private readonly Surface _window, _battle, _damage, _craftSprite, _craftShield;
	private readonly Surface[] _range = new Surface[WeaponMax];
	private readonly InteractiveSurface _btnMinimize, _preview;
	private readonly InteractiveSurface[] _weapon = new InteractiveSurface[WeaponMax];
	private readonly ImageButton _btnStandoff, _btnCautious, _btnStandard, _btnAggressive, _btnDisengage, _btnUfo;
	private readonly StrongBox<ImageButton> _mode = new();
	private readonly InteractiveSurface _btnMinimizedIcon;
	private readonly Text[] _txtAmmo = new Text[WeaponMax];
	private readonly Text _txtDistance, _txtStatus, _txtInterceptionNumber;
	private readonly Text _txtOceanIndicator;
	private readonly Craft _craft;
	private readonly Ufo _ufo;
	private readonly bool _ufoIsAttacking, _missileCraft;
	private bool _missileImpact;
	private readonly bool _disableDisengage, _disableStandoff, _disableCautious, _disableStandard, _disableAggressive;
	private bool _craftIsDefenseless, _selfDestructPressed;
	private int _timeout, _currentDist, _targetDist;
	private readonly int[] _weaponFireInterval = new int[WeaponMax], _weaponFireCountdown = new int[WeaponMax];
	private bool _end, _endUfoHandled, _endCraftHandled, _ufoBreakingOff, _destroyUfo, _destroyCraft;
	private readonly bool[] _weaponEnabled = new bool[WeaponMax];
	private bool _minimized, _endDogfight, _animatingHit, _waitForPoly, _waitForAltitude;
	private readonly List<CraftWeaponProjectile> _projectiles = [];
	private readonly int _ufoSize;
	private int _ufoBlobSize, _craftHeight, _currentCraftDamageColor, _interceptionNumber;
	private int _interceptionsCount;
	private int _x, _y, _minimizedIconX, _minimizedIconY;
	private readonly int _weaponNum;
	private readonly int _pilotAccuracyBonus, _pilotDodgeBonus, _pilotApproachSpeedModifier, _craftAccelerationBonus;
	private bool _firedAtLeastOnce, _experienceAwarded;
	private bool _delayedRecolorDone;
	/// <summary>craft min/max, radar min/max, damage min/max, shield min/max</summary>
	private readonly int[] _colors = new int[13];
	private readonly bool[] _tractorLockedOn = new bool[WeaponMax];

	/// <summary>UFO blobs graphics ...</summary>
	private static readonly int[,,] _ufoBlobs =
	{
			/*0 STR_VERY_SMALL */
		{
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 1, 2, 3, 2, 1, 0, 0, 0, 0},
			{0, 0, 0, 0, 1, 3, 5, 3, 1, 0, 0, 0, 0},
			{0, 0, 0, 0, 1, 2, 3, 2, 1, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
		},
			/*1 STR_SMALL */
		{
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 1, 2, 2, 2, 1, 0, 0, 0, 0},
			{0, 0, 0, 1, 2, 3, 4, 3, 2, 1, 0, 0, 0},
			{0, 0, 0, 1, 2, 4, 5, 4, 2, 1, 0, 0, 0},
			{0, 0, 0, 1, 2, 3, 4, 3, 2, 1, 0, 0, 0},
			{0, 0, 0, 0, 1, 2, 2, 2, 1, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
		},
			/*2 STR_MEDIUM_UC */
		{
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0},
			{0, 0, 0, 1, 1, 2, 2, 2, 1, 1, 0, 0, 0},
			{0, 0, 0, 1, 2, 3, 3, 3, 2, 1, 0, 0, 0},
			{0, 0, 1, 2, 3, 4, 5, 4, 3, 2, 1, 0, 0},
			{0, 0, 1, 2, 3, 5, 5, 5, 3, 2, 1, 0, 0},
			{0, 0, 1, 2, 3, 4, 5, 4, 3, 2, 1, 0, 0},
			{0, 0, 0, 1, 2, 3, 3, 3, 2, 1, 0, 0, 0},
			{0, 0, 0, 1, 1, 2, 2, 2, 1, 1, 0, 0, 0},
			{0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
		},
			/*3 STR_LARGE */
		{
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0},
			{0, 0, 0, 1, 1, 2, 2, 2, 1, 1, 0, 0, 0},
			{0, 0, 1, 2, 2, 3, 3, 3, 2, 2, 1, 0, 0},
			{0, 0, 1, 2, 3, 4, 4, 4, 3, 2, 1, 0, 0},
			{0, 1, 2, 3, 4, 5, 5, 5, 4, 3, 2, 1, 0},
			{0, 1, 2, 3, 4, 5, 5, 5, 4, 3, 2, 1, 0},
			{0, 1, 2, 3, 4, 5, 5, 5, 4, 3, 2, 1, 0},
			{0, 0, 1, 2, 3, 4, 4, 4, 3, 2, 1, 0, 0},
			{0, 0, 1, 2, 2, 3, 3, 3, 2, 2, 1, 0, 0},
			{0, 0, 0, 1, 1, 2, 2, 2, 1, 1, 0, 0, 0},
			{0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
		},
			/*4 STR_VERY_LARGE */
		{
			{0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0},
			{0, 0, 0, 1, 1, 2, 2, 2, 1, 1, 0, 0, 0},
			{0, 0, 1, 2, 2, 3, 3, 3, 2, 2, 1, 0, 0},
			{0, 1, 2, 3, 3, 4, 4, 4, 3, 3, 2, 1, 0},
			{0, 1, 2, 3, 4, 5, 5, 5, 4, 3, 2, 1, 0},
			{1, 2, 3, 4, 5, 5, 5, 5, 5, 4, 3, 2, 1},
			{1, 2, 3, 4, 5, 5, 5, 5, 5, 4, 3, 2, 1},
			{1, 2, 3, 4, 5, 5, 5, 5, 5, 4, 3, 2, 1},
			{0, 1, 2, 3, 4, 5, 5, 5, 4, 3, 2, 1, 0},
			{0, 1, 2, 3, 3, 4, 4, 4, 3, 3, 2, 1, 0},
			{0, 0, 1, 2, 2, 3, 3, 3, 2, 2, 1, 0, 0},
			{0, 0, 0, 1, 1, 2, 2, 2, 1, 1, 0, 0, 0},
			{0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0}
		},
			/*5 STR_HUGE */
		{
			{0, 0, 0, 1, 1, 2, 2, 2, 1, 1, 0, 0, 0},
			{0, 0, 1, 2, 2, 3, 3, 3, 2, 2, 1, 0, 0},
			{0, 1, 2, 3, 3, 4, 4, 4, 3, 3, 2, 1, 0},
			{1, 2, 3, 4, 4, 5, 5, 5, 4, 4, 3, 2, 1},
			{1, 2, 3, 4, 5, 5, 5, 5, 5, 4, 3, 2, 1},
			{2, 3, 4, 5, 5, 5, 5, 5, 5, 5, 4, 3, 2},
			{2, 3, 4, 5, 5, 5, 5, 5, 5, 5, 4, 3, 2},
			{2, 3, 4, 5, 5, 5, 5, 5, 5, 5, 4, 3, 2},
			{1, 2, 3, 4, 5, 5, 5, 5, 5, 4, 3, 2, 1},
			{1, 2, 3, 4, 4, 5, 5, 5, 4, 4, 3, 2, 1},
			{0, 1, 2, 3, 3, 4, 4, 4, 3, 3, 2, 1, 0},
			{0, 0, 1, 2, 2, 3, 3, 3, 2, 2, 1, 0, 0},
			{0, 0, 0, 1, 1, 2, 2, 2, 1, 1, 0, 0, 0}
		},
			/*6 STR_VERY_HUGE :p */
		{
			{0, 0, 0, 2, 2, 3, 3, 3, 2, 2, 0, 0, 0},
			{0, 0, 2, 3, 3, 4, 4, 4, 3, 3, 2, 0, 0},
			{0, 2, 3, 4, 4, 5, 5, 5, 4, 4, 3, 2, 0},
			{2, 3, 4, 5, 5, 5, 5, 5, 5, 5, 4, 3, 2},
			{2, 3, 4, 5, 5, 5, 5, 5, 5, 5, 4, 3, 2},
			{3, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 4, 3},
			{3, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 4, 3},
			{3, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 4, 3},
			{2, 3, 4, 5, 5, 5, 5, 5, 5, 5, 4, 3, 2},
			{2, 3, 4, 5, 5, 5, 5, 5, 5, 5, 4, 3, 2},
			{0, 2, 3, 4, 4, 5, 5, 5, 4, 4, 3, 2, 0},
			{0, 0, 2, 3, 3, 4, 4, 4, 3, 3, 2, 0, 0},
			{0, 0, 0, 2, 2, 3, 3, 3, 2, 2, 0, 0, 0}
		},
			/*7 STR_ENOURMOUS */
		{
			{0, 0, 0, 3, 3, 4, 4, 4, 3, 3, 0, 0, 0},
			{0, 0, 3, 4, 4, 5, 5, 5, 4, 4, 3, 0, 0},
			{0, 3, 4, 5, 5, 5, 5, 5, 5, 5, 4, 3, 0},
			{3, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 4, 3},
			{3, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 4, 3},
			{4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 4},
			{4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 4},
			{4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 4},
			{3, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 4, 3},
			{3, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 4, 3},
			{0, 3, 4, 5, 5, 5, 5, 5, 5, 5, 4, 3, 0},
			{0, 0, 3, 4, 4, 5, 5, 5, 4, 4, 3, 0, 0},
			{0, 0, 0, 3, 3, 4, 4, 4, 3, 3, 0, 0, 0}
		}
	};
	
	/// <summary>Projectile blobs.</summary>
	private static readonly int[,,] _projectileBlobs =
	{
			/*0 STR_STINGRAY_MISSILE ?*/
		{
			{0, 1, 0},
			{1, 9, 1},
			{1, 4, 1},
			{0, 3, 0},
			{0, 2, 0},
			{0, 1, 0}
		},
			/*1 STR_AVALANCHE_MISSILE ?*/
		{
			{1, 2, 1},
			{2, 9, 2},
			{2, 5, 2},
			{1, 3, 1},
			{0, 2, 0},
			{0, 1, 0}
		},
			/*2 STR_CANNON_ROUND ?*/
		{
			{0, 0, 0},
			{0, 7, 0},
			{0, 2, 0},
			{0, 1, 0},
			{0, 0, 0},
			{0, 0, 0}
		},
			/*3 STR_FUSION_BALL ?*/
		{
			{2, 4, 2},
			{4, 9, 4},
			{2, 4, 2},
			{0, 0, 0},
			{0, 0, 0},
			{0, 0, 0}
		}
	};

	private static void DrawRect(Surface s, int x, int y, int w, int h, int color) =>
		s.DrawRect((short)x, (short)y, (short)w, (short)h, unchecked((byte)color));

	/// <summary>
	/// Initializes all the elements in the Dogfight window.
	/// </summary>
	/// <param name="state">Pointer to the Geoscape.</param>
	/// <param name="craft">Pointer to the craft intercepting.</param>
	/// <param name="ufo">Pointer to the UFO being intercepted.</param>
	/// <param name="ufoIsAttacking">Is UFO the aggressor?</param>
	public DogfightState(GeoscapeState state, Craft craft, Ufo ufo, bool ufoIsAttacking = false)
	{
		_state = state;
		_craft = craft;
		_ufo = ufo;
		_ufoIsAttacking = ufoIsAttacking;
		_missileCraft = craft.Rules.IsMissile;
		_missileImpact = false;
		_disableDisengage = false;
		_disableStandoff = false;
		_disableCautious = false;
		_disableStandard = false;
		_disableAggressive = false;
		_craftIsDefenseless = false;
		_selfDestructPressed = false;
		_timeout = 50;
		_currentDist = 640;
		_targetDist = 560;
		_end = false;
		_endUfoHandled = false;
		_endCraftHandled = false;
		_ufoBreakingOff = false;
		_destroyUfo = false;
		_destroyCraft = false;
		_minimized = false;
		_endDogfight = false;
		_animatingHit = false;
		_waitForPoly = false;
		_waitForAltitude = false;
		_ufoSize = 0;
		_ufoBlobSize = 0;
		_craftHeight = 0;
		_currentCraftDamageColor = 0;
		_interceptionNumber = 0;
		_interceptionsCount = 0;
		_x = 0;
		_y = 0;
		_minimizedIconX = 0;
		_minimizedIconY = 0;
		_firedAtLeastOnce = false;
		_experienceAwarded = false;
		_delayedRecolorDone = false;

		_screen = false;
		_craft.SetInDogfight(true);
		_weaponNum = _craft.Rules.Weapons;
		if (_weaponNum > WeaponMax)
			_weaponNum = WeaponMax;

		for (int i = 0; i < _weaponNum; ++i)
		{
			_weaponEnabled[i] = true;
			_weaponFireCountdown[i] = 0;
			_tractorLockedOn[i] = false;

			CraftWeapon w = _craft.Weapons[i];
			if (w != null)
			{
				_weaponEnabled[i] = !w.IsDisabled;
			}
		}

		// pilot modifiers
		List<Soldier> pilots = _craft.GetPilotList(false, _game.Mod); // refresh soldier bonuses

		_pilotAccuracyBonus = _craft.GetPilotAccuracyBonus(pilots, _game.Mod);
		_pilotDodgeBonus = _craft.GetPilotDodgeBonus(pilots, _game.Mod);
		_pilotApproachSpeedModifier = _craft.GetPilotApproachSpeedModifier(pilots, _game.Mod);

		_craftAccelerationBonus = 2; // vanilla
		if (pilots.Count != 0)
		{
			_craftAccelerationBonus = Math.Min(4, (_craft.CraftStats.accel / 3) + 1);
		}

		// HK options
		if (_ufoIsAttacking)
		{
			_disableStandoff = true;
			_disableStandard = true;
			_disableAggressive = false;
			if (_ufo.CraftStats.speedMax >= _craft.CraftStats.speedMax)
			{
				_disableDisengage = true;
			}
			if (_weaponNum == 0)
			{
				_disableCautious = true;
			}
			// make sure the HK attacks its primary target first!
			{
				if (_ufo.Destination is Craft target)
				{
					if (_craft != target)
					{
						// push secondary targets a tiny bit away from the HK
						_currentDist += 16;
					}
					else
					{
						// approach primary target at maximum approach speed
						_pilotApproachSpeedModifier = 4;
					}
				}
			}
		}
		// Missile options
		if (_missileCraft)
		{
			_disableStandoff = true;
			_disableStandard = true;
			_disableAggressive = false;
			_disableDisengage = true;
			_disableCautious = true;

			// approach UFO at maximum approach speed
			_pilotApproachSpeedModifier = 4;
		}

		// Create objects
		_window = new Surface(160, 96, _x, _y);
		_battle = new Surface(77, 74, _x + 3, _y + 3);
		for (int i = 0; i < _weaponNum; ++i)
		{
			int w_off = i % 2 != 0 ? 64 : 4;
			int r_off = i % 2 != 0 ? 43 : 19;
			int y_off = 52 - (i / 2) * 28;

			_weapon[i] = new InteractiveSurface(15, 17, _x + w_off, _y + y_off);
			_range[i] = new Surface(21, 74, _x + r_off, _y + 3);
			_txtAmmo[i] = new Text(16, 9, _x + w_off, _y + y_off + 18);
		}
		_craftSprite = new Surface(22, 25, _x + 93, _y + 40);
		_damage = new Surface(22, 25, _x + 93, _y + 40);
		_craftShield = new Surface(22, 25, _x + 93, _y + 40);

		_btnMinimize = new InteractiveSurface(12, 12, _x, _y);
		_preview = new InteractiveSurface(160, 96, _x, _y);
		_btnStandoff = new ImageButton(36, 15, _x + 83, _y + 4);
		_btnCautious = new ImageButton(36, 15, _x + 120, _y + 4);
		_btnStandard = new ImageButton(36, 15, _x + 83, _y + 20);
		_btnAggressive = new ImageButton(36, 15, _x + 120, _y + 20);
		_btnDisengage = new ImageButton(36, 15, _x + 120, _y + 36);
		_btnUfo = new ImageButton(36, 17, _x + 120, _y + 52);
		_txtDistance = new Text(40, 9, _x + 116, _y + 72);
		_txtOceanIndicator = new Text(16, 9, _x + 150, _y + 72);
		_txtStatus = new Text(154, 9, _x + 4, _y + 85);
		_btnMinimizedIcon = new InteractiveSurface(32, 20, _minimizedIconX, _minimizedIconY);
		_txtInterceptionNumber = new Text(16, 9, _minimizedIconX + 18, _minimizedIconY + 6);

		_mode.Value = (_ufoIsAttacking || _missileCraft) ? _btnAggressive : _btnStandoff;
		_craftDamageAnimTimer = new Timer(500);

		MoveWindow();

		// Set palette
		SetInterface("dogfight");

		Add(_window);
		Add(_battle);
		for (int i = 0; i < _weaponNum; ++i)
		{
			Add(_weapon[i]);
			Add(_range[i]);
		}
		Add(_craftSprite);
		Add(_damage);
		Add(_craftShield);
		Add(_btnMinimize);
		Add(_btnStandoff, "standoffButton", "dogfight", _window);
		Add(_btnCautious, "cautiousButton", "dogfight", _window);
		Add(_btnStandard, "standardButton", "dogfight", _window);
		Add(_btnAggressive, "aggressiveButton", "dogfight", _window);
		Add(_btnDisengage, "disengageButton", "dogfight", _window);
		Add(_btnUfo, "ufoButton", "dogfight", _window);
		for (int i = 0; i < _weaponNum; ++i)
		{
			Add(_txtAmmo[i], "numbers", "dogfight", _window);
		}
		Add(_txtDistance, "distance", "dogfight", _window);
		Add(_txtOceanIndicator, "oceanIndicator", "dogfight", _window);
		Add(_preview);
		Add(_txtStatus, "text", "dogfight", _window);
		Add(_btnMinimizedIcon);
		Add(_txtInterceptionNumber, "minimizedNumber", "dogfight");

		_btnStandoff.Invalidate(false);
		_btnCautious.Invalidate(false);
		_btnStandard.Invalidate(false);
		_btnAggressive.Invalidate(false);
		_btnDisengage.Invalidate(false);
		_btnUfo.Invalidate(false);

		// Set up objects
		RuleInterface dogfightInterface = _game.Mod.GetInterface("dogfight");

		SurfaceCrop crop = _game.Mod.GetSurface("INTERWIN.DAT").GetCrop();
		crop.X = 0;
		crop.Y = 0;
		crop.Crop.x = 0;
		crop.Crop.y = 0;
		crop.Crop.w = (ushort)_window.Width;
		crop.Crop.h = (ushort)_window.Height;
		_window.DrawRect(ref crop.Crop, 15);
		crop.Blit(_window);

		if (_ufoIsAttacking || _missileCraft)
		{
			if (_disableStandoff)
			{
				DrawRect(_window, _btnStandoff.X + 2, _btnStandoff.Y + 2, _btnStandoff.Width - 4, _btnStandoff.Height - 4,
					dogfightInterface.GetElement("standoffButton").color + 4);
			}
			if (_disableCautious)
			{
				DrawRect(_window, _btnCautious.X + 2, _btnCautious.Y + 2, _btnCautious.Width - 4, _btnCautious.Height - 4,
					dogfightInterface.GetElement("cautiousButton").color + 4);
			}
			if (_disableStandard)
			{
				DrawRect(_window, _btnStandard.X + 2, _btnStandard.Y + 2, _btnStandard.Width - 4, _btnStandard.Height - 4,
					dogfightInterface.GetElement("standardButton").color + 4);
			}
			if (_disableAggressive)
			{
				DrawRect(_window, _btnAggressive.X + 2, _btnAggressive.Y + 2, _btnAggressive.Width - 4, _btnAggressive.Height - 4,
					dogfightInterface.GetElement("aggressiveButton").color + 4);
			}
			if (_disableDisengage)
			{
				DrawRect(_window, _btnDisengage.X + 2, _btnDisengage.Y + 2, _btnDisengage.Width - 4, _btnDisengage.Height - 4,
					dogfightInterface.GetElement("disengageButton").color + 4);
			}
		}
		if (_ufoIsAttacking)
		{
			int offset = dogfightInterface.GetElement("minimizeButtonDummy").TFTDMode ? 1 : 0;
			DrawRect(_window, _btnMinimize.X + 1 + offset, _btnMinimize.Y + 1, _btnMinimize.Width - 2 - offset, _btnMinimize.Height - 2,
				dogfightInterface.GetElement("minimizeButtonDummy").color + 4);
		}

		_preview.DrawRect(ref crop.Crop, 15);
		crop.Crop.y = (short)dogfightInterface.GetElement("previewTop").y;
		crop.Crop.h = (ushort)dogfightInterface.GetElement("previewTop").h;
		crop.Blit(_preview);
		crop.Y = _window.Height - dogfightInterface.GetElement("previewBot").h;
		crop.Crop.y = (short)dogfightInterface.GetElement("previewBot").y;
		crop.Crop.h = (ushort)dogfightInterface.GetElement("previewBot").h;
		crop.Blit(_preview);

		if (string.IsNullOrEmpty(ufo.Rules.ModSprite))
		{
			crop.Crop.y = (short)(dogfightInterface.GetElement("previewMid").y + dogfightInterface.GetElement("previewMid").h * _ufo.Rules.Sprite);
			crop.Crop.h = (ushort)dogfightInterface.GetElement("previewMid").h;
		}
		else
		{
			crop = _game.Mod.GetSurface(ufo.Rules.ModSprite).GetCrop();
		}
		crop.X = dogfightInterface.GetElement("previewTop").x;
		crop.Y = dogfightInterface.GetElement("previewTop").h;
		crop.Blit(_preview);

		_preview.Visible = false;
		_preview.OnMouseClick(PreviewClick);

		_btnMinimize.OnMouseClick(BtnMinimizeClick);
		_btnMinimize.Visible = !_ufoIsAttacking;

		_btnStandoff.Copy(_window);
		_btnStandoff.SetGroup(_mode);
		_btnStandoff.OnMousePress(BtnStandoffPress);
		_btnStandoff.OnMousePress(BtnStandoffRightPress, SDLMouse.SDL_BUTTON_RIGHT);
		_btnStandoff.Visible = !_disableStandoff;

		_btnCautious.Copy(_window);
		_btnCautious.SetGroup(_mode);
		_btnCautious.OnMousePress(BtnCautiousPress);
		_btnCautious.OnMousePress(BtnCautiousRightPress, SDLMouse.SDL_BUTTON_RIGHT);
		_btnCautious.Visible = !_disableCautious;

		_btnStandard.Copy(_window);
		_btnStandard.SetGroup(_mode);
		_btnStandard.OnMousePress(BtnStandardPress);
		_btnStandard.OnMousePress(BtnStandardRightPress, SDLMouse.SDL_BUTTON_RIGHT);
		_btnStandard.Visible = !_disableStandard;

		_btnAggressive.Copy(_window);
		_btnAggressive.SetGroup(_mode);
		_btnAggressive.OnMousePress(BtnAggressivePress);
		_btnAggressive.OnMousePress(BtnAggressiveRightPress, SDLMouse.SDL_BUTTON_RIGHT);
		_btnAggressive.Visible = !_disableAggressive;
		if (_ufoIsAttacking || _missileCraft)
		{
			BtnAggressivePress(null);
		}

		_btnDisengage.Copy(_window);
		_btnDisengage.OnMousePress(BtnDisengagePress);
		_btnDisengage.OnMousePress(BtnDisengageRightPress, SDLMouse.SDL_BUTTON_RIGHT);
		_btnDisengage.SetGroup(_mode);
		_btnDisengage.Visible = !_disableDisengage;

		_btnUfo.Copy(_window);
		_btnUfo.OnMouseClick(BtnUfoClick);

		_txtDistance.SetText("640");
		UpdateOceanIndicator();

		if (_ufoIsAttacking)
			_txtStatus.SetText(Tr("STR_AGGRESSIVE_ATTACK"));
		else
			_txtStatus.SetText(Tr("STR_STANDOFF"));

		SurfaceSet set = _game.Mod.GetSurfaceSet("INTICON.PCK");

		// Create the minimized dogfight icon.
		Surface frame = set.GetFrame(_craft.SkinSprite);
		frame.BlitNShade(_btnMinimizedIcon, 0, 0);
		_btnMinimizedIcon.OnMouseClick(BtnMinimizedIconClick);
		_btnMinimizedIcon.Visible = false;

		// Draw correct number on the minimized dogfight icon.
		if (_craft.InterceptionOrder == 0)
		{
			int maxInterceptionOrder = 0;
			foreach (var xbase in _game.SavedGame.Bases)
			{
				foreach (var xcraft in xbase.Crafts)
				{
					if (xcraft.InterceptionOrder > maxInterceptionOrder)
					{
						maxInterceptionOrder = xcraft.InterceptionOrder;
					}
				}
			}
			_craft.InterceptionOrder = ++maxInterceptionOrder;
		}
		_txtInterceptionNumber.SetText(_craft.InterceptionOrder.ToString(CultureInfo.InvariantCulture));
		_txtInterceptionNumber.Visible = false;

		// define the colors to be used
		_colors[CRAFT_MIN] = dogfightInterface.GetElement("craftRange").color;
		_colors[CRAFT_MAX] = dogfightInterface.GetElement("craftRange").color2;
		_colors[RADAR_MIN] = dogfightInterface.GetElement("radarRange").color;
		_colors[RADAR_MAX] = dogfightInterface.GetElement("radarRange").color2;
		_colors[DAMAGE_MIN] = dogfightInterface.GetElement("damageRange").color;
		_colors[DAMAGE_MAX] = dogfightInterface.GetElement("damageRange").color2;
		_colors[BLOB_MIN] = dogfightInterface.GetElement("radarDetail").color;
		_colors[RANGE_METER] = dogfightInterface.GetElement("radarDetail").color2;
		_colors[DISABLED_WEAPON] = dogfightInterface.GetElement("disabledWeapon").color;
		_colors[DISABLED_RANGE] = dogfightInterface.GetElement("disabledWeapon").color2;
		_colors[DISABLED_AMMO] = dogfightInterface.GetElement("disabledAmmo").color;
		_colors[SHIELD_MIN] = dogfightInterface.GetElement("shieldRange").color;
		_colors[SHIELD_MAX] = dogfightInterface.GetElement("shieldRange").color2;

		for (int i = 0; i < _weaponNum; ++i)
		{
			CraftWeapon w = _craft.Weapons[i];

			// Slot empty or no sprite, skip!
			if (w == null || w.Rules.Sprite < 0)
				continue;

			InteractiveSurface weapon = _weapon[i];
			Surface range = _range[i];
			Text ammo = _txtAmmo[i];
			int x1, x2;
			int x_off = 2 * (i / 2 + 1);
			if (i % 2 == 0)
			{
				x1 = x_off;
				x2 = 0;
			}
			else
			{
				x1 = 0;
				x2 = 20 - x_off;
			}

			// Draw icon
			frame = set.GetFrame(w.Rules.Sprite + 5);
			frame.BlitNShade(weapon, 0, 0);

			// Just an equipment, it doesn't have any ammo (weapon) or range (tractor beam), skip!
			if (w.Rules.AmmoMax == 0 && w.Rules.TractorBeamPower == 0)
				continue;

			// Used for weapon toggling.
			// Only relevant for weapons and tractor beams, not for equipment.
			_weapon[i].OnMouseClick(WeaponClick);

			// Draw ammo.
			// Only relevant for weapons, not for tractor beams.
			if (w.Rules.AmmoMax > 0)
			{
				ammo.SetText(w.Ammo.ToString(CultureInfo.InvariantCulture));
			}

			// Draw range (1 km = 1 pixel)
			// Only relevant for weapons and tractor beams.
			byte color = unchecked((byte)_colors[RANGE_METER]);
			range.Lock();

			int rangeY = range.Height - w.Rules.Range;
			int connectY = weapon.Height / 2 + weapon.Y - range.Y;
			for (int x = x1; x <= x1 + 20 - x_off; x += 2)
			{
				range.SetPixel(x, rangeY, color);
			}

			int minY = 0, maxY = 0;
			if (rangeY < connectY)
			{
				minY = rangeY;
				maxY = connectY;
			}
			else if (rangeY > connectY)
			{
				minY = connectY;
				maxY = rangeY;
			}
			for (int y = minY; y <= maxY; ++y)
			{
				range.SetPixel(x1 + x2, y, color);
			}
			for (int x = x2; x <= x2 + x_off; ++x)
			{
				range.SetPixel(x, connectY, color);
			}
			range.Unlock();
		}

		for (int i = 0; i < _weaponNum; ++i)
		{
			if (_craft.Weapons[i] == null)
			{
				_weapon[i].Visible = false;
				_range[i].Visible = false;
				_txtAmmo[i].Visible = false;
			}
		}

		// Draw damage indicator.
		frame = set.GetFrame(_craft.SkinSprite + 11);
		frame.BlitNShade(_craftSprite, 0, 0);

		_craftDamageAnimTimer.OnTimer((StateHandler)AnimateCraftDamage);

		// don't set these variables if the ufo is already engaged in a dogfight
		if (_ufo.EscapeCountdown == 0)
		{
			_ufo.FireCountdown = 0;
			int escapeCountdown = _ufo.Rules.BreakOffTime + Rng.Generate(0, _ufo.Rules.BreakOffTime) - 30 * _game.SavedGame.DifficultyCoefficient;
			{
				int diff = (int)_game.SavedGame.Difficulty;
				var custom = _game.Mod.UfoEscapeCountdownCoefficients;
				if (custom.Count > diff)
				{
					escapeCountdown = _ufo.Rules.BreakOffTime + Rng.Generate(0, _ufo.Rules.BreakOffTime);
					escapeCountdown = escapeCountdown * custom[diff] / 100;
				}
			}
			_ufo.EscapeCountdown = Math.Max(1, escapeCountdown);
		}

		for (int i = 0; i < _weaponNum; ++i)
		{
			if (_craft.Weapons[i] != null)
			{
				if (!_ufoIsAttacking)
				{
					_weaponFireInterval[i] = _craft.Weapons[i].Rules.StandardReload;
				}
				else
				{
					_weaponFireInterval[i] = _craft.Weapons[i].Rules.AggressiveReload;
				}
			}
		}

		// Set UFO size - going to be moved to Ufo class to implement simultaneous dogfights.
		_ufoBlobSize = _ufo.Rules.BlobSize;
		_ufoSize = Math.Min(_ufoBlobSize, 4); // yes, maximum supported is 4, not a typo

		// Get crafts height. Used for damage indication.
		for (int y = 0; y < _craftSprite.Height; ++y)
		{
			for (int x = 0; x < _craftSprite.Width; ++x)
			{
				byte pixelColor = _craftSprite.GetPixel(x, y);
				if (pixelColor >= _colors[CRAFT_MIN] && pixelColor < _colors[CRAFT_MAX])
				{
					++_craftHeight;
					break;
				}
			}
		}

		DrawCraftDamage();
		DrawCraftShield();

		// Set this as the interception handling UFO shield recharge if no other is doing it
		if (_ufo.ShieldRechargeHandle == 0)
		{
			_ufo.ShieldRechargeHandle = _interceptionNumber;
		}
	}

	/// <summary>
	/// Returns true if this is a hunter-killer dogfight. Otherwise returns false.
	/// </summary>
	public bool IsUfoAttacking => _ufoIsAttacking;

	/// <summary>
	/// Runs the higher level dogfight functionality.
	/// </summary>
	public override void Think()
	{
		if (!_delayedRecolorDone)
		{
			// can't be done in the constructor (recoloring the ammo text doesn't work)
			for (int i = 0; i < _weaponNum; ++i)
			{
				if (_craft.Weapons[i] != null && !_weaponEnabled[i])
				{
					Recolor(i, _weaponEnabled[i]);
				}
			}
			_delayedRecolorDone = true;

			// Note: init() is never called for DogfightState, so we'll do it here instead
			{
				var sounds = _game.Mod.StartDogfightSounds;
				int soundId = sounds.Count == 0 ? Mod.NO_SOUND : sounds[Rng.Generate(0, sounds.Count - 1)];
				if (soundId != Mod.NO_SOUND)
				{
					var customSound = _game.Mod.GetSound("GEO.CAT", soundId);
					customSound.Play();
				}
			}
		}
		if (!_endDogfight)
		{
			Update();
			_craftDamageAnimTimer.Think(this, null);
		}
		if (!_ufoIsAttacking || _ufo.Status == Ufo.UfoStatus.LANDED)
		{
			if (!_craft.IsInDogfight || _craft.Destination != _ufo || _ufo.Status == Ufo.UfoStatus.LANDED)
			{
				EndDogfight();
			}
		}
	}

	/// <summary>
	/// Animates interceptor damage by changing the color and redrawing the image.
	/// </summary>
	public void AnimateCraftDamage()
	{
		if (_minimized)
		{
			return;
		}
		--_currentCraftDamageColor;
		if (_currentCraftDamageColor < _colors[DAMAGE_MIN])
		{
			_currentCraftDamageColor = _colors[DAMAGE_MAX];
		}
		DrawCraftDamage();
	}

	/// <summary>
	/// Draws interceptor damage according to percentage of HP's left.
	/// </summary>
	public void DrawCraftDamage()
	{
		if (_craft.DamagePercentage != 0)
		{
			if (!_craftDamageAnimTimer.IsRunning)
			{
				_craftDamageAnimTimer.Start();
				if (_currentCraftDamageColor < _colors[DAMAGE_MIN])
				{
					_currentCraftDamageColor = _colors[DAMAGE_MIN];
				}
			}
			int damagePercentage = _craft.DamagePercentage;
			int rowsToColor = (int)Math.Floor((double)_craftHeight * (double)(damagePercentage / 100.0));
			if (rowsToColor == 0)
			{
				return;
			}
			int rowsColored = 0;
			bool rowColored = false;
			for (int y = 0; y < _damage.Height; ++y)
			{
				rowColored = false;
				for (int x = 0; x < _damage.Width; ++x)
				{
					int craftPixel = _craftSprite.GetPixel(x, y);
					if (craftPixel != 0)
					{
						_damage.SetPixel(x, y, unchecked((byte)_currentCraftDamageColor));
						rowColored = true;
					}
				}
				if (rowColored)
				{
					++rowsColored;
				}
				if (rowsColored == rowsToColor)
				{
					break;
				}
			}
		}
	}

	/// <summary>
	/// Draws the craft's shield over it's sprite according to the shield remaining
	/// </summary>
	public void DrawCraftShield()
	{
		if (_craft.ShieldCapacity == 0)
			return;

		int maxRow = _craftHeight - ((_craftHeight * _craft.Shield) / _craft.ShieldCapacity);
		bool startColoring = false;
		for (int y = _craftShield.Height; y >= 0; --y)
		{
			for (int x = 0; x < _craftShield.Width; ++x)
			{
				int craftPixel = _craftSprite.GetPixel(x, y);
				if (!startColoring && craftPixel != 0)
				{
					maxRow += (y - _craftHeight + 1);
					startColoring = true;
				}
				if (y < maxRow && craftPixel != 0)
				{
					_craftShield.SetPixel(x, y, 0);
				}
				if (y >= maxRow && craftPixel != 0)
				{
					byte shieldColor = unchecked((byte)Math.Min(_colors[SHIELD_MAX], _colors[SHIELD_MIN] + (craftPixel - _colors[CRAFT_MIN])));
					_craftShield.SetPixel(x, y, shieldColor);
				}
			}
		}
	}

	/// <summary>
	/// Animates the window with a palette effect.
	/// </summary>
	public void Animate()
	{
		// Animate radar waves and other stuff.
		for (int x = 0; x < _window.Width; ++x)
		{
			for (int y = 0; y < _window.Height; ++y)
			{
				byte radarPixelColor = _window.GetPixel(x, y);
				if (radarPixelColor >= _colors[RADAR_MIN] && radarPixelColor < _colors[RADAR_MAX])
				{
					++radarPixelColor;
					if (radarPixelColor >= _colors[RADAR_MAX])
					{
						radarPixelColor = unchecked((byte)_colors[RADAR_MIN]);
					}
					_window.SetPixel(x, y, radarPixelColor);
				}
			}
		}

		_battle.Clear();

		// Draw UFO.
		if (!_ufo.IsDestroyed)
		{
			DrawUfo();
		}

		// Draw projectiles.
		foreach (var cwp in _projectiles)
		{
			DrawProjectile(cwp);
		}

		// Clears text after a while
		if (_timeout == 0)
		{
			_txtStatus.SetText("");
		}
		else
		{
			_timeout--;
		}

		// Animate UFO hit.
		bool lastHitAnimFrame = false;
		if (_animatingHit && _ufo.HitFrame > 0)
		{
			_ufo.HitFrame = _ufo.HitFrame - 1;
			if (_ufo.HitFrame == 0)
			{
				_animatingHit = false;
				lastHitAnimFrame = true;
			}
		}

		// Animate UFO crash landing.
		if (_ufo.IsCrashed && _ufo.HitFrame == 0 && !lastHitAnimFrame)
		{
			--_ufoBlobSize;
		}
	}

	/// <summary>
	/// Updates all the elements in the dogfight, including ufo movement,
	/// weapons fire, projectile movement, ufo escape conditions,
	/// craft and ufo destruction conditions, and retaliation mission generation, as applicable.
	/// </summary>
	public void Update()
	{
		bool finalRun = false;
		// Check if craft is not low on fuel when window minimized, and
		// Check if crafts destination hasn't been changed when window minimized.
		if (!_ufoIsAttacking)
		{
			Ufo u = _craft.Destination as Ufo;
			if (u != _ufo || !_craft.IsInDogfight || _craft.LowFuel || (_minimized && _ufo.IsCrashed))
			{
				EndDogfight();
				return;
			}
		}

		if (!_minimized)
		{
			Animate();
			if (!_ufo.IsCrashed && !_ufo.IsDestroyed && !_craft.IsDestroyed && !_ufo.InterceptionProcessed)
			{
				_ufo.InterceptionProcessed = true;
				int escapeCounter = _ufo.EscapeCountdown;
				if (_ufoIsAttacking)
				{
					if (_disableDisengage && _ufo.SoftlockShotCounter >= _ufo.Rules.SoftlockThreshold)
					{
						escapeCounter = 1; // game is in softlock, stop being a hunter-killer and disengage!
					}
					else if (_ufo.Damage > _ufo.CraftStats.damageMax / 3 && _ufo.HuntBehavior != 1)
					{
						// TODO: rethink: unhardcode run away thresholds?
						if (_craft.Damage > _craft.DamageMax / 2)
						{
							escapeCounter = 999; // it's gonna be tight, continue shooting...
						}
						else
						{
							escapeCounter = 1; // we're badly hurt and xcom isn't, abort immediately!
						}
					}
					else
					{
						escapeCounter = 999; // we're still ok, continue shooting...
					}
				}

				if (escapeCounter > 0)
				{
					escapeCounter--;
					_ufo.EscapeCountdown = escapeCounter;
					// Check if UFO is breaking off.
					if (escapeCounter == 0)
					{
						_ufo.Speed = _ufo.CraftStats.speedMax;
						if (_ufoIsAttacking && _ufo.IsHunterKiller)
						{
							// stop being a hunter-killer and run away!
							_ufo.ResetOriginalDestination(_craft);
							_ufo.SetHunterKiller(false);
						}
					}
				}
				if (_ufo.FireCountdown > 0)
				{
					_ufo.FireCountdown = _ufo.FireCountdown - 1;
				}
			}
		}
		// Crappy craft is chasing UFO.
		int speedMinusTractors = Math.Max(0, _ufo.Speed - _ufo.TractorBeamSlowdown);
		if (speedMinusTractors > _craft.CraftStats.speedMax)
		{
			if (!_ufoIsAttacking || !_ufo.IsHunterKiller)
			{
				_ufoBreakingOff = true;
				finalRun = true;
				SetStatus("STR_UFO_OUTRUNNING_INTERCEPTOR");
			}
		}
		else
		{
			_ufoBreakingOff = false;
		}

		bool projectileInFlight = false;
		if (!_minimized)
		{
			int distanceChange = 0;

			// Update distance
			if (!_ufoBreakingOff)
			{
				if (_currentDist < _targetDist && !_ufo.IsCrashed && !_craft.IsDestroyed)
				{
					distanceChange = 2 * _craftAccelerationBonus; // disengage speed
					if (_currentDist + distanceChange > _targetDist)
					{
						distanceChange = _targetDist - _currentDist;
					}
				}
				else if (_currentDist > _targetDist && !_ufo.IsCrashed && !_craft.IsDestroyed)
				{
					distanceChange = -1 * _pilotApproachSpeedModifier; // engage speed
				}

				// don't let the interceptor mystically push or pull its fired projectiles
				foreach (var cwp in _projectiles)
				{
					if (cwp.GlobalType != CraftWeaponProjectileGlobalType.CWPGT_BEAM && cwp.Direction == (int)Directions.D_UP)
					{
						cwp.Position = cwp.Position + distanceChange;
					}
				}
			}
			else
			{
				distanceChange = 4; // ufo breaking off speed

				// UFOs can try to outrun our missiles, don't adjust projectile positions here
				// If UFOs ever fire anything but beams, those positions need to be adjust here though.
			}

			_currentDist += distanceChange;

			if (_game.Mod.ShowDogfightDistanceInKm)
			{
				_txtDistance.SetText(Tr("STR_KILOMETERS").Arg(_currentDist / 8));
			}
			else
			{
				_txtDistance.SetText(_currentDist.ToString(CultureInfo.InvariantCulture));
			}

			// Check and recharge craft shields
			// Check if the UFO's shields are being handled by an interception window
			if (_ufo.ShieldRechargeHandle == 0)
			{
				_ufo.ShieldRechargeHandle = _interceptionNumber;
			}

			// UFO shields
			if ((_ufo.Shield != 0) && (_interceptionNumber == _ufo.ShieldRechargeHandle))
			{
				int total = _ufo.CraftStats.shieldRecharge / 100;
				if (Rng.Percent(_ufo.CraftStats.shieldRecharge % 100))
					total++;
				_ufo.Shield = _ufo.Shield + total;
			}

			// Player craft shields
			if (_craft.Shield != 0)
			{
				int total = _craft.CraftStats.shieldRecharge / 100;
				if (Rng.Percent(_craft.CraftStats.shieldRecharge % 100))
					total++;
				if (total != 0)
				{
					_craft.Shield = _craft.Shield + total;
					DrawCraftShield();
				}
			}

			// Is the craft itself a projectile?
			if (_missileCraft && !_missileImpact && !_craft.IsDestroyed)
			{
				if (_currentDist <= AGGRESSIVE_DIST) // Note: hard-coded distance, ok
				{
					_missileImpact = true;

					// Missile self-destruct
					_craft.Damage = _craft.CraftStats.damageMax;
					DrawCraftDamage();

					// Handle UFO shields
					int damage = _craft.Rules.MissilePower(); // Note: no randomness :(
					int shieldDamage = 0;
					if (_ufo.Shield != 0)
					{
						shieldDamage = damage; // Note: no shield-effectiveness factor
						// scale down by bleed-through factor
						// Note: C++ multiplies by shieldBleedThrough without dividing by 100 here (kept as is)
						damage = Math.Max(0, shieldDamage - _ufo.Shield) * _ufo.CraftStats.shieldBleedThrough;
						_ufo.Shield = _ufo.Shield - shieldDamage;
					}
					damage = Math.Max(0, damage - _ufo.CraftStats.armor);
					_ufo.SetDamage(_ufo.Damage + damage, _game.Mod);
					_state.HandleDogfightExperience(); // called after setDamage
					if (_ufo.IsCrashed)
					{
						_ufo.ShotDownByCraftId = _craft.UniqueId;
						_ufo.Speed = 0;
						_ufo.Destination = null;
						// if the ufo got destroyed here, these no longer apply
						_ufoBreakingOff = false;
						finalRun = false;
						_end = false;
					}
					if (_ufo.HitFrame == 0)
					{
						_animatingHit = true;
						_ufo.HitFrame = 3;
					}
					// How hard was the ufo hit?
					if (_ufo.Shield != 0)
					{
						SetStatus("STR_UFO_SHIELD_HIT");
					}
					else
					{
						if (damage == 0)
						{
							if (shieldDamage == 0)
								SetStatus("STR_UFO_HIT_NO_DAMAGE");
							else
								SetStatus("STR_UFO_SHIELD_DOWN");
						}
						else
						{
							if (damage < _ufo.CraftStats.damageMax / 2 * _game.Mod.UfoGlancingHitThreshold / 100)
								SetStatus("STR_UFO_HIT_GLANCING");
							else
								SetStatus("STR_UFO_HIT");
						}
					}
					_game.Mod.GetSound("GEO.CAT", Mod.UFO_HIT).Play();
				}
			}

			// Move projectiles and check for hits.
			foreach (var p in _projectiles)
			{
				p.Move();
				// Projectiles fired by interceptor.
				if (p.Direction == (int)Directions.D_UP)
				{
					// Projectile reached the UFO - determine if it's been hit.
					if (((p.Position >= _currentDist) || (p.GlobalType == CraftWeaponProjectileGlobalType.CWPGT_BEAM && p.ToBeRemoved())) && !_ufo.IsCrashed && !p.Missed)
					{
						// UFO hit.
						int chanceToHit = CraftChanceToHit(p.Accuracy, _ufoSize, _ufo.CraftStats.avoidBonus, _craft.CraftStats.hitBonus, _pilotAccuracyBonus);
						if (Rng.Percent(chanceToHit))
						{
							// Formula delivered by Volutar, altered by Extended version.
							int power = p.Damage * (_craft.CraftStats.powerBonus + 100) / 100;

							int damage = 0;
							if (p.DamageItem != null)
							{
								// unified damage formula
								damage = p.DamageItem.DamageType.GetRandomDamage(power);
							}
							else
							{
								// vanilla dmg formula: 50-100%
								damage = Rng.Generate(power / 2, power);
							}
							// Handle UFO shields
							int shieldDamage = 0;
							if (_ufo.Shield != 0)
							{
								shieldDamage = damage * p.ShieldDamageModifier / 100;
								if (p.ShieldDamageModifier == 0)
								{
									damage = 0;
								}
								else
								{
									// scale down by bleed-through factor and scale up by shield-effectiveness factor
									damage = Math.Max(0, shieldDamage - _ufo.Shield) * _ufo.CraftStats.shieldBleedThrough / p.ShieldDamageModifier;
								}
								_ufo.Shield = _ufo.Shield - shieldDamage;
							}

							damage = Math.Max(0, damage - _ufo.CraftStats.armor);
							_ufo.SetDamage(_ufo.Damage + damage, _game.Mod);
							_state.HandleDogfightExperience(); // called after setDamage
							if (_ufo.IsCrashed)
							{
								_ufo.ShotDownByCraftId = _craft.UniqueId;
								_ufo.Speed = 0;
								_ufo.Destination = null;
								// if the ufo got destroyed here, these no longer apply
								_ufoBreakingOff = false;
								finalRun = false;
								_end = false;
							}
							if (_ufo.HitFrame == 0)
							{
								_animatingHit = true;
								_ufo.HitFrame = 3;
							}

							if (Options.oxceShowUFOCombatStats)
							{
								var ss = new System.Text.StringBuilder();
								if (_ufo.Shield > 0)
								{
									ss.Append(CultureInfo.InvariantCulture, $"SH: {_ufo.Shield} ({_ufo.ShieldPercentage}%)");
								}
								else
								{
									ss.Append(CultureInfo.InvariantCulture, $"HP: {(_ufo.CraftStats.damageMax / (_ufo.CanCrash() ? 2 : 1)) - _ufo.Damage} ({(_ufo.CanCrash() ? 50 : 100) - _ufo.DamagePercentage}%)");
								}
								ss.Append(CultureInfo.InvariantCulture, $" HIT: {(shieldDamage > 0 ? shieldDamage : damage)}");
								SetStatus(ss.ToString());
							}
							else
							{
								// How hard was the ufo hit?
								if (_ufo.Shield != 0)
								{
									SetStatus("STR_UFO_SHIELD_HIT");
								}
								else
								{
									if (damage == 0)
									{
										if (shieldDamage == 0)
										{
											SetStatus("STR_UFO_HIT_NO_DAMAGE");
										}
										else
										{
											SetStatus("STR_UFO_SHIELD_DOWN");
										}
									}
									else
									{
										if (damage < _ufo.CraftStats.damageMax / 2 * _game.Mod.UfoGlancingHitThreshold / 100)
										{
											SetStatus("STR_UFO_HIT_GLANCING");
										}
										else
										{
											SetStatus("STR_UFO_HIT");
										}
									}
								}
							}

							_game.Mod.GetSound("GEO.CAT", Mod.UFO_HIT).Play();
							p.Remove();
						}
						// Missed.
						else
						{
							if (p.GlobalType == CraftWeaponProjectileGlobalType.CWPGT_BEAM)
							{
								p.Remove();
							}
							else
							{
								p.Missed = true;
							}
						}
					}
					// Check if projectile passed it's maximum range.
					if (p.GlobalType == CraftWeaponProjectileGlobalType.CWPGT_MISSILE && p.Position / 8 >= p.Range)
					{
						p.Remove();
					}
					else if (!_ufo.IsCrashed)
					{
						projectileInFlight = true;
					}
				}
				// Projectiles fired by UFO.
				else if (p.Direction == (int)Directions.D_DOWN)
				{
					if (p.GlobalType == CraftWeaponProjectileGlobalType.CWPGT_MISSILE || (p.GlobalType == CraftWeaponProjectileGlobalType.CWPGT_BEAM && p.ToBeRemoved()))
					{
						int chancetoHit = UfoChanceToHit(p.Accuracy, _craft.CraftStats.avoidBonus, _ufo.CraftStats.hitBonus, _pilotDodgeBonus,
							_ufoIsAttacking && _mode.Value == _btnCautious);
						if (Rng.Percent(chancetoHit) || _selfDestructPressed)
						{
							// Formula delivered by Volutar, altered by Extended version.
							int power = p.Damage * (_ufo.CraftStats.powerBonus + 100) / 100;
							int damage = Rng.Generate(0, power);

							if (_craft.Shield != 0)
							{
								int shieldBleedThroughDamage = Math.Max(0, damage - _craft.Shield) * _craft.CraftStats.shieldBleedThrough / 100;
								_craft.Shield = _craft.Shield - damage;
								damage = shieldBleedThroughDamage;
								DrawCraftShield();
								SetStatus("STR_INTERCEPTOR_SHIELD_HIT");
							}

							damage = Math.Max(0, damage - _craft.CraftStats.armor);

							// if a totally crappy HK is attacking a completely defenseless craft, avoid endless fight
							if (_selfDestructPressed)
							{
								damage = _craft.CraftStats.damageMax;
							}

							if (damage != 0)
							{
								_craft.Damage = _craft.Damage + damage;
								DrawCraftDamage();
								if (_missileCraft)
									SetStatus("STR_MISSILE_DAMAGED");
								else
									SetStatus("STR_INTERCEPTOR_DAMAGED");
								_game.Mod.GetSound("GEO.CAT", Mod.INTERCEPTOR_HIT).Play(); //10
								if (_mode.Value == _btnCautious && _craft.DamagePercentage >= 50 && !_ufoIsAttacking)
								{
									_targetDist = STANDOFF_DIST;
								}
							}
						}
						p.Remove();
					}
				}
			}

			// Remove projectiles that hit or missed their target.
			_projectiles.RemoveAll(cwp => cwp.ToBeRemoved() == true || (cwp.Missed == true && cwp.Position <= 0));

			// Check if the situation is hopeless for the craft
			if (_disableDisengage && !_craftIsDefenseless && !_missileCraft)
			{
				if (_projectiles.Count == 0)
				{
					bool hasNoAmmo = true;
					foreach (var cw in _craft.Weapons)
					{
						if (cw != null && cw.Ammo > 0)
						{
							hasNoAmmo = false;
							break;
						}
					}
					// no projectiles in the air and no ammo left
					if (hasNoAmmo)
					{
						_craftIsDefenseless = true;

						// self-destruct button
						int offset = _game.Mod.GetInterface("dogfight").GetElement("minimizeButtonDummy").TFTDMode ? 1 : 0;
						DrawRect(_btnMinimize, 1 + offset, 1, _btnMinimize.Width - 2 - offset, _btnMinimize.Height - 2, _colors[DAMAGE_MAX]);
						_btnMinimize.Visible = true;
					}
				}
			}

			// Handle weapons and craft distance.
			for (int i = 0; i < _weaponNum; ++i)
			{
				CraftWeapon w = _craft.Weapons[i];
				if (w == null || _missileCraft)
				{
					continue;
				}
				int wTimer = _weaponFireCountdown[i];

				// Handle weapon firing
				if (wTimer == 0 && _currentDist <= w.Rules.Range * 8 && w.Ammo > 0 && _mode.Value != _btnStandoff
					&& _mode.Value != _btnDisengage && !_ufo.IsCrashed && !_craft.IsDestroyed)
				{
					if (_weaponEnabled[i])
					{
						FireWeapon(i);
						projectileInFlight = true;
					}
				}
				else if (wTimer > 0)
				{
					--_weaponFireCountdown[i];
				}

				// Handle craft tractor beams
				if (w.Rules.TractorBeamPower != 0)
				{
					if (_currentDist <= w.Rules.Range * 8 && _mode.Value != _btnStandoff
						&& _mode.Value != _btnDisengage && !_ufo.IsCrashed && !_craft.IsDestroyed
						&& _weaponEnabled[i])
					{
						if (!_tractorLockedOn[i])
						{
							_tractorLockedOn[i] = true;
							int tractorBeamSlowdown = _ufo.TractorBeamSlowdown;
							tractorBeamSlowdown += w.Rules.TractorBeamPower * _game.Mod.GetUfoTractorBeamSizeModifier(_ufoSize) / 100;
							_ufo.TractorBeamSlowdown = tractorBeamSlowdown;
							SetStatus("STR_TRACTOR_BEAM_ENGAGED");
						}
					}
					else
					{
						if (_tractorLockedOn[i])
						{
							_tractorLockedOn[i] = false;
							int tractorBeamSlowdown = _ufo.TractorBeamSlowdown;
							tractorBeamSlowdown -= w.Rules.TractorBeamPower * _game.Mod.GetUfoTractorBeamSizeModifier(_ufoSize) / 100;
							_ufo.TractorBeamSlowdown = tractorBeamSlowdown;
							SetStatus("STR_TRACTOR_BEAM_DISENGAGED");
						}
					}
				}

				if (w.Ammo == 0 && !projectileInFlight && !_craft.IsDestroyed)
				{
					// Handle craft distance according to option set by user and available ammo.
					if (_mode.Value == _btnCautious && !_ufoIsAttacking)
					{
						MinimumDistance();
					}
					else if (_mode.Value == _btnStandard)
					{
						MaximumDistance();
					}
				}
			}

			// Handle UFO firing.
			if (_currentDist <= _ufo.Rules.WeaponRange * 8 && !_ufo.IsCrashed && !_craft.IsDestroyed)
			{
				if (_ufo.ShootingAt == 0)
				{
					_ufo.ShootingAt = _interceptionNumber;
				}
				if (_ufo.ShootingAt == _interceptionNumber)
				{
					if (_ufo.FireCountdown == 0)
					{
						UfoFireWeapon();
					}
				}
			}
			else if (_ufo.ShootingAt == _interceptionNumber)
			{
				_ufo.ShootingAt = 0;
			}
		}

		// Check when battle is over.
		if (_end == true && (((_currentDist > 640 || _minimized) && (_mode.Value == _btnDisengage || _ufoBreakingOff == true)) || (_timeout == 0 && (_ufo.IsCrashed || _craft.IsDestroyed))))
		{
			if (_ufoBreakingOff)
			{
				_ufo.Move();
				// TODO: rethink: give hunter-killers opportunity to escape?
				if (!_ufoIsAttacking)
				{
					_craft.Destination = _ufo;
				}
			}
			if (!_destroyCraft && (_destroyUfo || _mode.Value == _btnDisengage))
			{
				// keep original target if attacked by a HK (and didn't disengage manually)
				bool keepOriginalTarget = _ufoIsAttacking && _craft.Destination != _ufo;
				if (!keepOriginalTarget || _mode.Value == _btnDisengage)
				{
					_craft.ReturnToBase();
				}

				// Need to give the craft at least one step advantage over the hunter-killer (to be able to escape)
				if (_ufoIsAttacking)
				{
					bool returnedToBase = _craft.Think();
					if (returnedToBase)
					{
						_game.SavedGame.StopHuntingXcomCraft(_craft); // hiding in the base is good enough, obviously
					}
				}
			}
			if (_ufo.IsCrashed)
			{
				foreach (var follower in _ufo.CraftFollowers)
				{
					if (follower.NumTotalUnits == 0 || !follower.Rules.AllowLanding)
					{
						follower.ReturnToBase();
					}
				}
			}
			EndDogfight();
		}

		if (_currentDist > 640 && _ufoBreakingOff)
		{
			finalRun = true;
		}

		if (!_end)
		{
			if (_endCraftHandled)
			{
				finalRun = true;
			}
			else if (_craft.IsDestroyed)
			{
				// End dogfight if craft is destroyed.
				if (_missileCraft)
				{
					if (_missileImpact)
					{
						// Empty by design
						// Let's keep showing how much damage was done to the UFO (and/or shields)
						// No need to say the missile impacted/self-destructed, it's obvious
					}
					else
					{
						SetStatus("STR_MISSILE_DESTROYED");
					}
				}
				else
				{
					SetStatus("STR_INTERCEPTOR_DESTROYED");
				}
				// Note: crew evacuation for HK attacks was moved to GeoscapeState
				_timeout += 30;
				_game.Mod.GetSound("GEO.CAT", Mod.INTERCEPTOR_EXPLODE).Play();
				finalRun = true;
				_destroyCraft = true;
				_endCraftHandled = true;
				_ufo.ShootingAt = 0;
			}

			if (_endUfoHandled)
			{
				finalRun = true;
			}
			else if (_ufo.IsCrashed)
			{
				// End dogfight if UFO is crashed or destroyed.
				_endUfoHandled = true;

				if (_ufo.ShotDownByCraftId == _craft.UniqueId)
				{
					AlienRace race = _game.Mod.GetAlienRace(_ufo.AlienRace);
					AlienMission mission = _ufo.Mission;
					mission.UfoShotDown(_ufo);
					// Check for retaliation trigger.
					int retaliationOdds = mission.Rules.RetaliationOdds;
					if (retaliationOdds == -1)
					{
						retaliationOdds = 100 - (4 * (24 - _game.SavedGame.DifficultyCoefficient) - race.RetaliationAggression);
						{
							int diff = (int)_game.SavedGame.Difficulty;
							var custom = _game.Mod.RetaliationTriggerOdds;
							if (custom.Count > diff)
							{
								retaliationOdds = custom[diff] + race.RetaliationAggression;
							}
						}
					}
					// Have mercy on beginners
					if (_game.SavedGame.MonthsPassed < Mod.DIFFICULTY_BASED_RETAL_DELAY[(int)_game.SavedGame.Difficulty])
					{
						retaliationOdds = 0;
					}

					if (Rng.Percent(retaliationOdds))
					{
						// Spawn retaliation mission.
						string targetRegion;
						int retaliationUfoMissionRegionOdds = 50 - 6 * _game.SavedGame.DifficultyCoefficient;
						{
							int diff = (int)_game.SavedGame.Difficulty;
							var custom = _game.Mod.RetaliationBaseRegionOdds;
							if (custom.Count > diff)
							{
								retaliationUfoMissionRegionOdds = 100 - custom[diff];
							}
						}
						if (Rng.Percent(retaliationUfoMissionRegionOdds))
						{
							// Attack on UFO's mission region
							targetRegion = _ufo.Mission.Region;
						}
						else
						{
							// Try to find and attack the originating base.
							targetRegion = _game.SavedGame.LocateRegion(_craft.Base).Rules.Type;
							// TODO: If the base is removed, the mission is canceled.
						}
						// Difference from original: No retaliation until final UFO lands (Original: Is spawned).
						if (_game.SavedGame.FindAlienMission(targetRegion, MissionObjective.OBJECTIVE_RETALIATION, race) == null)
						{
							var retalWeights = race.RetaliationMissionWeights(_game.SavedGame.MonthsPassed);
							string retalMission = retalWeights != null ? retalWeights.Choose() : "";
							RuleAlienMission rule = _game.Mod.GetAlienMission(retalMission, false);
							if (rule == null)
							{
								rule = _game.Mod.GetRandomMission(MissionObjective.OBJECTIVE_RETALIATION, _game.SavedGame.MonthsPassed);
							}

							if (rule != null)
							{
								var newMission = new AlienMission(rule);
								newMission.Id = _game.SavedGame.GetId("ALIEN_MISSIONS");
								newMission.SetRegion(targetRegion, _game.Mod);
								newMission.Race = _ufo.AlienRace;
								newMission.Start(_game, _state.Globe, newMission.Rules.GetWave(0).spawnTimer); // fixed delay for first scout
								_game.SavedGame.AlienMissions.Add(newMission);
							}
						}
					}
				}

				if (_ufo.IsDestroyed)
				{
					if (_ufo.ShotDownByCraftId == _craft.UniqueId)
					{
						AddActivityXcom(_ufo.Rules.Score * 2);
						SetStatus("STR_UFO_DESTROYED");
						_game.Mod.GetSound("GEO.CAT", Mod.UFO_EXPLODE).Play(); //11
					}
					_destroyUfo = true;
				}
				else
				{
					if (_ufo.ShotDownByCraftId == _craft.UniqueId)
					{
						SetStatus("STR_UFO_CRASH_LANDS");
						_game.Mod.GetSound("GEO.CAT", Mod.UFO_CRASH).Play(); //10
						AddActivityXcom(_ufo.Rules.Score);
					}
					bool survived = true;
					bool fakeUnderwaterTexture = _state.Globe.InsideFakeUnderwaterTexture(_ufo.Longitude, _ufo.Latitude);
					if (!_state.Globe.InsideLand(_ufo.Longitude, _ufo.Latitude))
					{
						survived = false; // destroyed on real water
					}
					else if (fakeUnderwaterTexture)
					{
						if (Rng.Percent(_ufo.Rules.SplashdownSurvivalChance))
						{
							SetStatus("STR_UFO_SURVIVED_SPLASHDOWN");
						}
						else
						{
							survived = false; // destroyed on fake water
							SetStatus("STR_UFO_DESTROYED_BY_SPLASHDOWN");
						}
					}
					if (!survived)
					{
						_ufo.Status = Ufo.UfoStatus.DESTROYED;
						_destroyUfo = true;
					}
					else
					{
						_ufo.SecondsRemaining = Rng.Generate(24, 96) * 3600;
						_ufo.Altitude = "STR_GROUND";
						if (_ufo.CrashId == 0)
						{
							_ufo.CrashId = _game.SavedGame.GetId("STR_CRASH_SITE");
							if (_ufo.IsHunterKiller)
							{
								// stop being a hunter-killer
								_ufo.ResetOriginalDestination(_craft);
								_ufo.SetHunterKiller(false);
							}
						}
					}
				}
				_timeout += 30;
				if (_ufo.ShotDownByCraftId != _craft.UniqueId)
				{
					_timeout += 50;
					_ufo.HitFrame = 3;
				}
				finalRun = true;

				if (_ufo.Status == Ufo.UfoStatus.LANDED)
				{
					_timeout += 30;
					finalRun = true;
					_ufo.ShootingAt = 0;
				}
			}
			else if (_ufo.CraftStats.speedMax - _ufo.TractorBeamSlowdown == 0) // UFO brought down by tractor beam
			{
				_endUfoHandled = true;

				bool survived = true;
				if (!_state.Globe.InsideLand(_ufo.Longitude, _ufo.Latitude))
				{
					survived = false; // destroyed on real water
				}
				else
				{
					bool fakeUnderwaterTexture = _state.Globe.InsideFakeUnderwaterTexture(_ufo.Longitude, _ufo.Latitude);
					if (fakeUnderwaterTexture && !Rng.Percent(_ufo.Rules.SplashdownSurvivalChance))
					{
						survived = false; // destroyed on fake water
					}
				}
				if (_ufo.Rules.IsUnmanned)
				{
					survived = false; // unmanned UFOs (drones, missiles, etc.) can't be forced to land
				}
				if (!survived) // Brought it down over water (and didn't survive splashdown)
				{
					finalRun = true;
					_ufo.SetDamage(_ufo.CraftStats.damageMax, _game.Mod);
					_state.HandleDogfightExperience(); // called after setDamage
					_ufo.ShotDownByCraftId = _craft.UniqueId;
					_ufo.Speed = 0;
					_ufo.Status = Ufo.UfoStatus.DESTROYED;
					_destroyUfo = true;
					AddActivityXcom(_ufo.Rules.Score);
				}
				else // Brought it down over land (or survived splashdown)
				{
					finalRun = true;
					_ufo.SecondsRemaining = Rng.Generate(30, 120) * 60;
					_ufo.ShootingAt = 0;
					_ufo.Status = Ufo.UfoStatus.LANDED;
					_ufo.Altitude = "STR_GROUND";
					_ufo.Speed = 0;
					_ufo.TractorBeamSlowdown = 0;
					if (_ufo.LandId == 0)
					{
						_ufo.LandId = _game.SavedGame.GetId("STR_LANDING_SITE");
					}
				}
			}
		}

		if (!projectileInFlight && finalRun)
		{
			_end = true;
		}
	}

	/// <summary>
	/// Adds xcom activity to the first country and the first region containing the UFO
	/// (the loops repeated in C++ update()).
	/// </summary>
	private void AddActivityXcom(int score)
	{
		foreach (var country in _game.SavedGame.Countries)
		{
			if (country.Rules.InsideCountry(_ufo.Longitude, _ufo.Latitude))
			{
				country.AddActivityXcom(score);
				break;
			}
		}
		foreach (var region in _game.SavedGame.Regions)
		{
			if (region.Rules.InsideRegion(_ufo.Longitude, _ufo.Latitude))
			{
				region.AddActivityXcom(score);
				break;
			}
		}
	}

	/// <summary>Chance of an interceptor projectile to hit the UFO (vanilla xcom formula plus OXCE bonuses).</summary>
	internal static int CraftChanceToHit(int accuracy, int ufoSize, int ufoAvoidBonus, int craftHitBonus, int pilotAccuracyBonus)
	{
		int chanceToHit = (accuracy * (100 + 300 / (5 - ufoSize)) + 100) / 200; // vanilla xcom
		chanceToHit -= ufoAvoidBonus;
		chanceToHit += craftHitBonus;
		chanceToHit += pilotAccuracyBonus;
		return chanceToHit;
	}

	/// <summary>Chance of an UFO projectile to hit the interceptor.</summary>
	internal static int UfoChanceToHit(int accuracy, int craftAvoidBonus, int ufoHitBonus, int pilotDodgeBonus, bool evasive)
	{
		int chancetoHit = accuracy; // vanilla xcom
		chancetoHit -= craftAvoidBonus;
		chancetoHit += ufoHitBonus;
		chancetoHit -= pilotDodgeBonus;
		// evasive maneuvers
		if (evasive)
		{
			// HK's chance to hit is halved, but craft's reload time is doubled too
			chancetoHit = chancetoHit / 2;
		}
		return chancetoHit;
	}

	/// <summary>
	/// Fires a shot from the first weapon
	/// equipped on the craft.
	/// </summary>
	public void FireWeapon(int i)
	{
		CraftWeapon w1 = _craft.Weapons[i];
		if (w1.SetAmmo(w1.Ammo - 1))
		{
			_weaponFireCountdown[i] = _weaponFireInterval[i];

			_txtAmmo[i].SetText(w1.Ammo.ToString(CultureInfo.InvariantCulture));

			CraftWeaponProjectile p = w1.Fire();
			p.Direction = (int)Directions.D_UP;
			p.HorizontalPosition = (i % 2 != 0 ? CraftWeaponProjectile.HP_RIGHT : CraftWeaponProjectile.HP_LEFT) * (1 + 2 * (i / 2));
			_projectiles.Add(p);

			_game.Mod.GetSound("GEO.CAT", w1.Rules.Sound).Play();
			_firedAtLeastOnce = true;
		}
	}

	/// <summary>
	/// Each time a UFO will try to fire it's cannons
	/// a calculation is made. There's only 10% chance
	/// that it will actually fire.
	/// </summary>
	public void UfoFireWeapon()
	{
		int fireCountdown = Math.Max(1, (_ufo.Rules.WeaponReload - 2 * _game.SavedGame.DifficultyCoefficient));
		{
			int diff = (int)_game.SavedGame.Difficulty;
			var custom = _game.Mod.UfoFiringRateCoefficients;
			if (custom.Count > diff)
			{
				fireCountdown = Math.Max(1, _ufo.Rules.WeaponReload * custom[diff] / 100);
			}
		}
		_ufo.FireCountdown = Rng.Generate(0, fireCountdown) + fireCountdown;

		if (_txtStatus.GetText() == "")
		{
			SetStatus("STR_UFO_RETURN_FIRE");
		}
		var p = new CraftWeaponProjectile(null);
		p.Type = CraftWeaponProjectileType.CWPT_PLASMA_BEAM;
		p.Accuracy = 60;
		p.Damage = _ufo.Rules.WeaponPower;
		p.Direction = (int)Directions.D_DOWN;
		p.HorizontalPosition = CraftWeaponProjectile.HP_CENTER;
		p.Position = _currentDist - (_ufo.Rules.Radius / 2);
		_projectiles.Add(p);
		if (_ufoIsAttacking && _disableDisengage)
		{
			_ufo.IncreaseSoftlockShotCounter();
		}

		if (_ufo.Rules.FireSound == -1)
		{
			_game.Mod.GetSound("GEO.CAT", Mod.UFO_FIRE).Play();
		}
		else
		{
			_game.Mod.GetSound("GEO.CAT", _ufo.Rules.FireSound).Play();
		}
	}

	/// <summary>
	/// Sets the craft to the minimum distance
	/// required to fire a weapon.
	/// </summary>
	public void MinimumDistance()
	{
		int max = 0;
		foreach (var cw in _craft.Weapons)
		{
			if (cw == null)
				continue;
			if (cw.Rules.Range > max && cw.Ammo > 0)
			{
				max = cw.Rules.Range;
			}
		}
		if (max == 0)
		{
			_targetDist = STANDOFF_DIST;
		}
		else
		{
			_targetDist = max * 8;
		}
	}

	/// <summary>
	/// Sets the craft to the maximum distance
	/// required to fire a weapon.
	/// </summary>
	public void MaximumDistance()
	{
		int min = 1000;
		foreach (var cw in _craft.Weapons)
		{
			if (cw == null)
				continue;
			if (cw.Rules.Range < min && cw.Ammo > 0)
			{
				min = cw.Rules.Range;
			}
		}
		if (_ufoIsAttacking)
		{
			// If the UFO is actively hunting us, consider its weapon range too
			if (_ufo.Rules.WeaponRange > 0 && _ufo.Rules.WeaponRange < min)
			{
				min = _ufo.Rules.WeaponRange;
			}
		}
		if (min == 1000)
		{
			_targetDist = STANDOFF_DIST;
		}
		else
		{
			_targetDist = min * 8;
		}
	}

	/// <summary>
	/// Sets the craft to the distance relevant for aggressive attack.
	/// </summary>
	public void AggressiveDistance()
	{
		MaximumDistance();
		if (_targetDist > AGGRESSIVE_DIST)
		{
			_targetDist = AGGRESSIVE_DIST;
		}
	}

	/// <summary>
	/// Updates the status text and restarts
	/// the text timeout counter.
	/// </summary>
	/// <param name="status">New status text.</param>
	public void SetStatus(string status)
	{
		_txtStatus.SetText(Tr(status));
		_timeout = 50;
	}

	/// <summary>
	/// Minimizes the dogfight window.
	/// </summary>
	public void BtnMinimizeClick(Action action)
	{
		if (_craftIsDefenseless)
		{
			_selfDestructPressed = !_selfDestructPressed;
			if (_selfDestructPressed)
				SetStatus("STR_SELF_DESTRUCT_ACTIVATED");
			else
				SetStatus("STR_SELF_DESTRUCT_CANCELLED");
			int offset = _game.Mod.GetInterface("dogfight").GetElement("minimizeButtonDummy").TFTDMode ? 1 : 0;
			int color = _selfDestructPressed ? DAMAGE_MIN : DAMAGE_MAX;
			DrawRect(_btnMinimize, 1 + offset, 1, _btnMinimize.Width - 2 - offset, _btnMinimize.Height - 2, _colors[color]);
			return;
		}

		if (!_ufo.IsCrashed && !_craft.IsDestroyed && !_ufoBreakingOff)
		{
			if (_currentDist >= STANDOFF_DIST)
			{
				SetMinimized(true);
				_ufo.ShieldRechargeHandle = 0;
			}
			else
			{
				SetStatus("STR_MINIMISE_AT_STANDOFF_RANGE_ONLY");
			}
		}
	}

	/// <summary>
	/// Switches to Standoff mode (maximum range).
	/// </summary>
	public void BtnStandoffPress(Action action)
	{
		if (!_ufo.IsCrashed && !_craft.IsDestroyed && !_ufoBreakingOff)
		{
			_end = false;
			SetStatus("STR_STANDOFF");
			_targetDist = STANDOFF_DIST;
		}
	}

	public void BtnStandoffRightPress(Action action)
	{
		_state.HandleDogfightMultiAction(0);
	}

	public void BtnStandoffSimulateLeftPress(Action action)
	{
		if (_btnStandoff.Visible)
		{
			_btnStandoff.MousePress(action, this);
		}
	}

	/// <summary>
	/// Switches to Cautious mode (maximum weapon range).
	/// </summary>
	public void BtnCautiousPress(Action action)
	{
		if (!_ufo.IsCrashed && !_craft.IsDestroyed && !_ufoBreakingOff)
		{
			_end = false;
			if (!_ufoIsAttacking)
			{
				SetStatus("STR_CAUTIOUS_ATTACK");
				for (int i = 0; i < _weaponNum; ++i)
				{
					CraftWeapon w = _craft.Weapons[i];
					if (w != null)
					{
						_weaponFireInterval[i] = w.Rules.CautiousReload;
					}
				}
				MinimumDistance();
			}
			else
			{
				SetStatus("STR_EVASIVE_MANEUVERS");
				for (int i = 0; i < _weaponNum; ++i)
				{
					CraftWeapon w = _craft.Weapons[i];
					if (w != null)
					{
						// double the craft's reload time to balance halving the HK's chance to hit
						_weaponFireInterval[i] = w.Rules.AggressiveReload * 2;
					}
				}
				// same distance as aggressive (by design)
				AggressiveDistance();
			}
		}
	}

	public void BtnCautiousRightPress(Action action)
	{
		_state.HandleDogfightMultiAction(1);
	}

	public void BtnCautiousSimulateLeftPress(Action action)
	{
		if (_btnCautious.Visible)
		{
			_btnCautious.MousePress(action, this);
		}
	}

	/// <summary>
	/// Switches to Standard mode (minimum weapon range).
	/// </summary>
	public void BtnStandardPress(Action action)
	{
		if (!_ufo.IsCrashed && !_craft.IsDestroyed && !_ufoBreakingOff)
		{
			_end = false;
			SetStatus("STR_STANDARD_ATTACK");
			for (int i = 0; i < _weaponNum; ++i)
			{
				CraftWeapon w = _craft.Weapons[i];
				if (w != null)
				{
					_weaponFireInterval[i] = w.Rules.StandardReload;
				}
			}
			MaximumDistance();
		}
	}

	public void BtnStandardRightPress(Action action)
	{
		_state.HandleDogfightMultiAction(2);
	}

	public void BtnStandardSimulateLeftPress(Action action)
	{
		if (_btnStandard.Visible)
		{
			_btnStandard.MousePress(action, this);
		}
	}

	/// <summary>
	/// Switches to Aggressive mode (minimum range).
	/// </summary>
	public void BtnAggressivePress(Action action)
	{
		if (!_ufo.IsCrashed && !_craft.IsDestroyed && !_ufoBreakingOff)
		{
			_end = false;
			SetStatus("STR_AGGRESSIVE_ATTACK");
			for (int i = 0; i < _weaponNum; ++i)
			{
				CraftWeapon w = _craft.Weapons[i];
				if (w != null)
				{
					_weaponFireInterval[i] = w.Rules.AggressiveReload;
				}
			}
			AggressiveDistance();
		}
	}

	public void BtnAggressiveRightPress(Action action)
	{
		_state.HandleDogfightMultiAction(3);
	}

	public void BtnAggressiveSimulateLeftPress(Action action)
	{
		if (_btnAggressive.Visible)
		{
			_btnAggressive.MousePress(action, this);
		}
	}

	/// <summary>
	/// Disengages from the UFO.
	/// </summary>
	public void BtnDisengagePress(Action action)
	{
		if (!_ufo.IsCrashed && !_craft.IsDestroyed && !_ufoBreakingOff)
		{
			_end = true;
			SetStatus("STR_DISENGAGING");
			_targetDist = 800;
		}
	}

	public void BtnDisengageRightPress(Action action)
	{
		_state.HandleDogfightMultiAction(4);
	}

	public void BtnDisengageSimulateLeftPress(Action action)
	{
		if (_btnDisengage.Visible)
		{
			_btnDisengage.MousePress(action, this);
		}
	}

	/// <summary>
	/// Shows a front view of the UFO.
	/// </summary>
	public void BtnUfoClick(Action action)
	{
		_preview.Visible = true;
		// Disable all other buttons to prevent misclicks
		_btnStandoff.Visible = false;
		_btnCautious.Visible = false;
		_btnStandard.Visible = false;
		_btnAggressive.Visible = false;
		_btnDisengage.Visible = false;
		_btnUfo.Visible = false;
		_btnMinimize.Visible = false;
		for (int i = 0; i < _weaponNum; ++i)
		{
			_weapon[i].Visible = false;
		}
	}

	/// <summary>
	/// Hides the front view of the UFO.
	/// </summary>
	public void PreviewClick(Action action)
	{
		_preview.Visible = false;
		// Reenable all other buttons to prevent misclicks
		_btnStandoff.Visible = !_disableStandoff;
		_btnCautious.Visible = !_disableCautious;
		_btnStandard.Visible = !_disableStandard;
		_btnAggressive.Visible = !_disableAggressive;
		_btnDisengage.Visible = !_disableDisengage;
		_btnUfo.Visible = true;
		_btnMinimize.Visible = !_ufoIsAttacking || _craftIsDefenseless;
		for (int i = 0; i < _weaponNum; ++i)
		{
			_weapon[i].Visible = true;
		}
	}

	/// <summary>
	/// Draws the UFO blob on the radar screen.
	/// Currently works only for original sized blobs
	/// 13 x 13 pixels.
	/// </summary>
	public void DrawUfo()
	{
		if (_ufoBlobSize < 0 || _ufo.IsDestroyed)
		{
			return;
		}
		int currentUfoXposition = _battle.Width / 2 - 6;
		int currentUfoYposition = _battle.Height - (_currentDist / 8) - 6;
		for (int y = 0; y < 13; ++y)
		{
			for (int x = 0; x < 13; ++x)
			{
				byte pixelOffset = (byte)_ufoBlobs[_ufoBlobSize + _ufo.HitFrame, y, x];
				if (pixelOffset == 0)
				{
					continue;
				}
				else
				{
					if (_ufo.IsCrashed || _ufo.HitFrame > 0)
					{
						pixelOffset = unchecked((byte)(pixelOffset * 2));
					}

					byte radarPixelColor = _window.GetPixel(currentUfoXposition + x + 3, currentUfoYposition + y + 3); // + 3 cause of the window frame
					byte color = unchecked((byte)(radarPixelColor - pixelOffset));
					if (color < _colors[BLOB_MIN])
					{
						color = unchecked((byte)_colors[BLOB_MIN]);
					}

					if (_ufo.Shield != 0 && _ufo.CraftStats.shieldCapacity != 0)
					{
						byte shieldColor = unchecked((byte)(
							_colors[SHIELD_MAX]
							- ((_colors[SHIELD_MAX] - _colors[SHIELD_MIN]) * _ufo.Shield / _ufo.CraftStats.shieldCapacity)
							+ (color - _colors[BLOB_MIN])));
						if (shieldColor < _colors[SHIELD_MIN])
						{
							shieldColor = unchecked((byte)_colors[SHIELD_MIN]);
						}
						if (shieldColor <= _colors[SHIELD_MAX])
						{
							color = shieldColor;
						}
					}

					_battle.SetPixel(currentUfoXposition + x, currentUfoYposition + y, color);
				}
			}
		}
	}

	/// <summary>
	/// Draws projectiles on the radar screen.
	/// Depending on what type of projectile it is, it's
	/// shape will be different. Currently works for
	/// original sized blobs 3 x 6 pixels.
	/// </summary>
	public void DrawProjectile(CraftWeaponProjectile p)
	{
		int xPos = _battle.Width / 2 + p.HorizontalPosition;
		// Draw missiles.
		if (p.GlobalType == CraftWeaponProjectileGlobalType.CWPGT_MISSILE)
		{
			xPos -= 1;
			int yPos = _battle.Height - p.Position / 8;
			for (int x = 0; x < 3; ++x)
			{
				for (int y = 0; y < 6; ++y)
				{
					int pixelOffset = _projectileBlobs[(int)p.Type, y, x];
					if (pixelOffset == 0)
					{
						continue;
					}
					else
					{
						byte radarPixelColor = _window.GetPixel(xPos + x + 3, yPos + y + 3); // + 3 cause of the window frame
						byte color = unchecked((byte)(radarPixelColor - pixelOffset));
						if (color < _colors[BLOB_MIN])
						{
							color = unchecked((byte)_colors[BLOB_MIN]);
						}
						_battle.SetPixel(xPos + x, yPos + y, color);
					}
				}
			}
		}
		// Draw beams.
		else if (p.GlobalType == CraftWeaponProjectileGlobalType.CWPGT_BEAM)
		{
			int yStart = _battle.Height - 2;
			int yEnd = _battle.Height - (_currentDist / 8);
			byte pixelOffset = unchecked((byte)p.State);
			for (int y = yStart; y > yEnd; --y)
			{
				byte radarPixelColor = _window.GetPixel(xPos + 3, y + 3);

				int beamPower = 0;
				if (p.Type == CraftWeaponProjectileType.CWPT_PLASMA_BEAM)
				{
					beamPower = _ufo.Rules.WeaponPower / _game.Mod.UfoBeamWidthParameter;
				}

				for (int x = 0; x <= Math.Min(beamPower, 3); x++)
				{
					byte color = unchecked((byte)(radarPixelColor - pixelOffset - beamPower + 2 * x));
					if (color < _colors[BLOB_MIN])
					{
						color = unchecked((byte)_colors[BLOB_MIN]);
					}
					if (color > radarPixelColor)
					{
						color = radarPixelColor;
					}
					_battle.SetPixel(xPos + x, y, color);
					_battle.SetPixel(xPos - x, y, color);
				}
			}
		}
	}

	/// <summary>
	/// Toggles usage of weapons.
	/// </summary>
	public void WeaponClick(Action action)
	{
		for (int i = 0; i < _weaponNum; ++i)
		{
			if (action.Sender == _weapon[i])
			{
				_weaponEnabled[i] = !_weaponEnabled[i];
				Recolor(i, _weaponEnabled[i]);

				if (Options.oxceRememberDisabledCraftWeapons)
				{
					CraftWeapon w = _craft.Weapons[i];
					if (w != null)
					{
						w.SetDisabled(!_weaponEnabled[i]);
					}
				}
				return;
			}
		}
	}

	/// <summary>
	/// Changes colors of weapon icons, range indicators and ammo texts base on current weapon state.
	/// </summary>
	/// <param name="weaponNo">number of weapon for which colors must be changed.</param>
	/// <param name="currentState">state of weapon (enabled = true, disabled = false).</param>
	public void Recolor(int weaponNo, bool currentState)
	{
		InteractiveSurface weapon = _weapon[weaponNo];
		Text ammo = _txtAmmo[weaponNo];
		Surface range = _range[weaponNo];

		if (currentState)
		{
			weapon.Offset(-_colors[DISABLED_WEAPON]);
			ammo.Offset(-_colors[DISABLED_AMMO]);
			range.Offset(-_colors[DISABLED_RANGE]);
		}
		else
		{
			weapon.Offset(_colors[DISABLED_WEAPON]);
			ammo.Offset(_colors[DISABLED_AMMO]);
			range.Offset(_colors[DISABLED_RANGE]);
		}
	}

	/// <summary>
	/// Returns true if state is minimized. Otherwise returns false.
	/// </summary>
	public bool IsMinimized => _minimized;

	private void UpdateOceanIndicator()
	{
		// ocean (or cosmetic ocean texture)
		bool oceanTexture = !_state.Globe.InsideLand(_ufo.Longitude, _ufo.Latitude);
		// fake ocean texture
		bool fakeUnderwaterTexture = _state.Globe.InsideFakeUnderwaterTexture(_ufo.Longitude, _ufo.Latitude);
		int survivalChance = _ufo.Rules.SplashdownSurvivalChance;

		if (oceanTexture)
		{
			_txtOceanIndicator.SetText(Tr("STR_OCEAN_INDICATOR")); // ! ufo lost
		}
		else if (fakeUnderwaterTexture)
		{
			if (survivalChance >= 100)
				_txtOceanIndicator.SetText("");
			else if (survivalChance > 0)
				_txtOceanIndicator.SetText(Tr("STR_OCEAN_INDICATOR_RNG")); // ? ufo maybe lost, maybe not
			else
				_txtOceanIndicator.SetText(Tr("STR_OCEAN_INDICATOR")); // ! ufo lost
		}
		else
		{
			_txtOceanIndicator.SetText("");
		}
	}

	/// <summary>
	/// Sets the state to minimized/maximized status.
	/// </summary>
	/// <param name="minimized">Is the dogfight minimized?</param>
	public void SetMinimized(bool minimized)
	{
		if (!minimized)
		{
			UpdateOceanIndicator();
		}

		// set these to the same as the incoming minimized state
		_minimized = minimized;
		_btnMinimizedIcon.Visible = minimized;
		_txtInterceptionNumber.Visible = minimized;

		// set these to the opposite of the incoming minimized state
		_window.Visible = !minimized;
		_btnStandoff.Visible = !minimized;
		_btnCautious.Visible = !minimized;
		_btnStandard.Visible = !minimized;
		_btnAggressive.Visible = !minimized;
		_btnDisengage.Visible = !minimized;
		_btnUfo.Visible = !minimized;
		_btnMinimize.Visible = !minimized;
		_battle.Visible = !minimized;
		for (int i = 0; i < _weaponNum; ++i)
		{
			_weapon[i].Visible = !minimized;
			_range[i].Visible = !minimized;
			_txtAmmo[i].Visible = !minimized;
		}
		_craftSprite.Visible = !minimized;
		_damage.Visible = !minimized;
		_craftShield.Visible = !minimized;
		_txtDistance.Visible = !minimized;
		_txtOceanIndicator.Visible = !minimized;
		_txtStatus.Visible = !minimized;

		// set to false regardless
		_preview.Visible = false;
	}

	/// <summary>
	/// Maximizes the interception window.
	/// </summary>
	public void BtnMinimizedIconClick(Action action)
	{
		if (_craft.Rules.IsWaterOnly && _ufo.AltitudeInt > _craft.Rules.MaxAltitude)
		{
			_state.Popup(new DogfightErrorState(_craft, Tr("STR_UNABLE_TO_ENGAGE_DEPTH")));
			WaitForAltitude = true;
		}
		else if (_craft.Rules.IsWaterOnly && !_state.Globe.InsideLand(_craft.Longitude, _craft.Latitude))
		{
			_state.Popup(new DogfightErrorState(_craft, Tr("STR_UNABLE_TO_ENGAGE_AIRBORNE")));
			WaitForPoly = true;
		}
		else
		{
			SetMinimized(false);
		}
	}

	/// <summary>
	/// Interception number. Used to draw proper number when window minimized.
	/// </summary>
	public int InterceptionNumber { get => _interceptionNumber; set => _interceptionNumber = value; }

	/// <summary>
	/// Sets interceptions count. Used to properly position the window.
	/// </summary>
	/// <param name="count">Amount of interception windows.</param>
	public void SetInterceptionsCount(int count)
	{
		_interceptionsCount = count;
		CalculateWindowPosition();
		MoveWindow();
	}

	/// <summary>
	/// Calculates dogfight window position according to
	/// number of active interceptions.
	/// </summary>
	public void CalculateWindowPosition()
	{
		_minimizedIconX = 5;
		_minimizedIconY = (5 * _interceptionNumber) + (16 * (_interceptionNumber - 1));

		if (_interceptionsCount == 1)
		{
			_x = 80;
			_y = 52;
		}
		else if (_interceptionsCount == 2)
		{
			if (_interceptionNumber == 1)
			{
				_x = 80;
				_y = 0;
			}
			else // 2
			{
				_x = 80;
				_y = 200 - _window.Height;//96;
			}
		}
		else if (_interceptionsCount == 3)
		{
			if (_interceptionNumber == 1)
			{
				_x = 80;
				_y = 0;
			}
			else if (_interceptionNumber == 2)
			{
				_x = 0;
				_y = 200 - _window.Height;//96;
			}
			else // 3
			{
				_x = 320 - _window.Width;//160;
				_y = 200 - _window.Height;//96;
			}
		}
		else
		{
			if (_interceptionNumber == 1)
			{
				_x = 0;
				_y = 0;
			}
			else if (_interceptionNumber == 2)
			{
				_x = 320 - _window.Width;//160;
				_y = 0;
			}
			else if (_interceptionNumber == 3)
			{
				_x = 0;
				_y = 200 - _window.Height;//96;
			}
			else // 4
			{
				_x = 320 - _window.Width;//160;
				_y = 200 - _window.Height;//96;
			}
		}
		_x += _game.Screen.DX;
		_y += _game.Screen.DY;
	}

	/// <summary>
	/// Relocates all dogfight window elements to
	/// calculated position. This is used when multiple
	/// interceptions are running.
	/// </summary>
	public void MoveWindow()
	{
		int x = _window.X - _x;
		int y = _window.Y - _y;
		foreach (var surface in _surfaces)
		{
			surface.X = surface.X - x;
			surface.Y = surface.Y - y;
		}
		_btnMinimizedIcon.X = _minimizedIconX; _btnMinimizedIcon.Y = _minimizedIconY;
		_txtInterceptionNumber.X = _minimizedIconX + 18; _txtInterceptionNumber.Y = _minimizedIconY + 6;
	}

	/// <summary>
	/// Checks whether the dogfight should end.
	/// </summary>
	/// <returns>Returns true if the dogfight should end, otherwise returns false.</returns>
	public bool DogfightEnded() => _endDogfight;

	/// <summary>
	/// Returns the UFO associated to this dogfight.
	/// </summary>
	public Ufo Ufo => _ufo;

	/// <summary>
	/// Returns the craft associated to this dogfight.
	/// </summary>
	public Craft Craft => _craft;

	/// <summary>
	/// Ends the dogfight.
	/// </summary>
	private void EndDogfight()
	{
		if (_endDogfight)
			return;
		if (_craft != null)
		{
			_craft.SetInDogfight(false);
			_craft.InterceptionOrder = 0;
		}
		// set the ufo as "free" for the next engagement (as applicable)
		if (_ufo != null)
		{
			_ufo.InterceptionProcessed = false;
			_ufo.ShieldRechargeHandle = 0;
		}
		_endDogfight = true;
	}

	/// <summary>Waits until the UFO reaches a polygon.</summary>
	public bool WaitForPoly { get => _waitForPoly; set => _waitForPoly = value; }

	/// <summary>Waits until the UFO reaches the right altitude.</summary>
	public bool WaitForAltitude { get => _waitForAltitude; set => _waitForAltitude = value; }

	/// <summary>
	/// Award experience to the pilots.
	/// </summary>
	public void AwardExperienceToPilots()
	{
		if (_firedAtLeastOnce && !_experienceAwarded && _craft != null && _ufo != null && (_ufo.IsCrashed || _ufo.IsDestroyed))
		{
			bool psiStrengthEval = (Options.psiStrengthEval && _game.SavedGame.IsResearched(_game.Mod.PsiRequirements));
			foreach (var pilot in _craft.GetPilotList(false, null)) // refresh already done in the constructor
			{
				if (pilot.CurrentStats.firing < pilot.Rules.StatCaps.firing)
				{
					if (Rng.Percent(pilot.Rules.DogfightExperience.firing))
					{
						pilot.CurrentStatsEditable.firing++;
						pilot.DailyDogfightExperienceCache.firing++;
					}
				}
				if (pilot.CurrentStats.reactions < pilot.Rules.StatCaps.reactions)
				{
					if (Rng.Percent(pilot.Rules.DogfightExperience.reactions))
					{
						pilot.CurrentStatsEditable.reactions++;
						pilot.DailyDogfightExperienceCache.reactions++;
					}
				}
				if (pilot.CurrentStats.bravery < pilot.Rules.StatCaps.bravery)
				{
					if (Rng.Percent(pilot.Rules.DogfightExperience.bravery))
					{
						pilot.CurrentStatsEditable.bravery += 10; // increase by 10 to keep OCD at bay
						pilot.DailyDogfightExperienceCache.bravery += 10;
					}
				}
				pilot.CalcStatString(_game.Mod.StatStrings, psiStrengthEval);
			}
			_experienceAwarded = true;
		}
	}
}
