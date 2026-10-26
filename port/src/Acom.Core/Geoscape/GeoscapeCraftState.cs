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
// Ported from: src/Geoscape/GeoscapeCraftState.h, src/Geoscape/GeoscapeCraftState.cpp
#nullable disable

using System.Text;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Craft window that displays info about
/// a specific craft out on the Geoscape.
/// </summary>
public class GeoscapeCraftState : State
{
	private readonly Craft _craft;
	private readonly Globe _globe;
	private readonly Waypoint _waypoint;

	private readonly TextButton _btnBase, _btnTarget, _btnPatrol, _btnCancel;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtStatus, _txtBase, _txtSpeed, _txtMaxSpeed, _txtAltitude, _txtFuel;
	private readonly Text _txtSoldier, _txtHWP, _txtShield, _txtDamage;
	private readonly Text[] _txtWeaponName = new Text[RuleCraft.WeaponMax], _txtWeaponAmmo = new Text[RuleCraft.WeaponMax];
	private readonly Text _txtRedirect;
	private readonly Text _txtETA;

	private readonly int _weaponNum;

	/// <summary>
	/// Initializes all the elements in the Geoscape Craft window.
	/// </summary>
	/// <param name="craft">Pointer to the craft to display.</param>
	/// <param name="globe">Pointer to the Geoscape globe.</param>
	/// <param name="waypoint">Pointer to the last UFO position (if redirecting the craft).</param>
	/// <param name="useCustomSound">Play a custom sound when opened?</param>
	public GeoscapeCraftState(Craft craft, Globe globe, Waypoint waypoint, bool useCustomSound)
	{
		_craft = craft;
		_globe = globe;
		_waypoint = waypoint;
		_screen = false;

		_weaponNum = _craft.Rules.Weapons;
		if (_weaponNum > RuleCraft.WeaponMax)
			_weaponNum = RuleCraft.WeaponMax;

		const int offset_upper = -8;
		const int offset_lower = 120;

		if (useCustomSound)
		{
			int soundId = craft.Rules.SelectSound;
			if (soundId != Mod.NO_SOUND)
			{
				_customSound = _game.Mod.GetSound("GEO.CAT", soundId);
			}
		}

		// Create objects
		_window = new Window(this, 240, 192, 4, 4, WindowPopup.POPUP_BOTH);
		_txtTitle = new Text(210, 17, 32, offset_upper + 20);
		_txtStatus = new Text(210, 17, 32, offset_upper + 36);
		_txtBase = new Text(210, 9, 32, offset_upper + 52);
		_txtSpeed = new Text(210, 9, 32, offset_upper + 60);
		_txtMaxSpeed = new Text(210, 9, 32, offset_upper + 68);
		_txtAltitude = new Text(210, 9, 32, offset_upper + 76);
		_txtSoldier = new Text(80, 9, 164, offset_upper + 60);
		_txtHWP = new Text(80, 9, 164, offset_upper + 68);
		_txtFuel = new Text(130, 9, 32, offset_upper + 84);
		_txtDamage = new Text(80, 9, 164, offset_upper + 84);
		_txtShield = new Text(80, 9, 164, offset_upper + 76);

		for (int i = 0; i < _weaponNum; ++i)
		{
			_txtWeaponName[i] = new Text(130, 9, 32, offset_upper + 92 + 8 * i);
			_txtWeaponAmmo[i] = new Text(80, 9, 164, offset_upper + 92 + 8 * i);
		}

		_txtRedirect = new Text(230, 17, 13, offset_lower + 0);
		_txtETA = new Text(230, 9, 13, offset_lower + 4);

		_btnBase = new TextButton(192, 12, 32, offset_lower + 14);
		_btnTarget = new TextButton(192, 12, 32, offset_lower + 28);
		_btnPatrol = new TextButton(192, 12, 32, offset_lower + 42);
		_btnCancel = new TextButton(192, 12, 32, offset_lower + 56);

		// Set palette
		SetInterface("geoCraft");

		Add(_window, "window", "geoCraft");
		Add(_btnBase, "button", "geoCraft");
		Add(_btnTarget, "button", "geoCraft");
		Add(_btnPatrol, "button", "geoCraft");
		Add(_btnCancel, "button", "geoCraft");
		Add(_txtTitle, "text1", "geoCraft");
		Add(_txtStatus, "text1", "geoCraft");
		Add(_txtBase, "text3", "geoCraft");
		Add(_txtSpeed, "text3", "geoCraft");
		Add(_txtMaxSpeed, "text3", "geoCraft");
		Add(_txtAltitude, "text3", "geoCraft");
		Add(_txtFuel, "text3", "geoCraft");
		Add(_txtDamage, "text3", "geoCraft");
		Add(_txtShield, "text3", "geoCraft");
		for (int i = 0; i < _weaponNum; ++i)
		{
			Add(_txtWeaponName[i], "text3", "geoCraft");
			Add(_txtWeaponAmmo[i], "text3", "geoCraft");
		}
		Add(_txtRedirect, "text3", "geoCraft");
		Add(_txtETA, "text3", "geoCraft");
		Add(_txtSoldier, "text3", "geoCraft");
		Add(_txtHWP, "text3", "geoCraft");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "geoCraft");

		_btnBase.Text = Tr("STR_RETURN_TO_BASE");
		_btnBase.OnMouseClick(BtnBaseClick);

		_btnTarget.Text = Tr("STR_SELECT_NEW_TARGET");
		_btnTarget.OnMouseClick(BtnTargetClick);

		_btnPatrol.Text = _craft.Rules.CanAutoPatrol() ? Tr("STR_AUTO_PATROL") : Tr("STR_PATROL");
		_btnPatrol.OnMouseClick(BtnPatrolClick);

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTitle.SetText(_craft.GetName(_game.Language));

		_txtStatus.SetWordWrap(true);
		string status;
		if (_waypoint != null)
		{
			status = Tr("STR_INTERCEPTING_UFO").Arg(_waypoint.Id);
		}
		else if (_craft.LowFuel)
		{
			status = Tr("STR_LOW_FUEL_RETURNING_TO_BASE");
		}
		else if (_craft.MissionComplete)
		{
			status = Tr("STR_MISSION_COMPLETE_RETURNING_TO_BASE");
		}
		else if (_craft.Destination == null)
		{
			status = Tr("STR_PATROLLING");
		}
		else if (_craft.Destination == (Target)_craft.Base)
		{
			status = Tr("STR_RETURNING_TO_BASE");
		}
		else
		{
			if (_craft.Destination is Ufo u)
			{
				if (_craft.IsInDogfight)
				{
					status = Tr("STR_TAILING_UFO");
				}
				else if (u.Status == Ufo.UfoStatus.FLYING)
				{
					status = Tr("STR_INTERCEPTING_UFO").Arg(u.Id);
				}
				else
				{
					status = Tr("STR_DESTINATION_UC_").Arg(u.GetName(_game.Language));
				}
			}
			else
			{
				status = Tr("STR_DESTINATION_UC_").Arg(_craft.Destination.GetName(_game.Language));
			}
		}
		_txtStatus.SetText(Tr("STR_STATUS_").Arg(status));

		_txtBase.SetText(Tr("STR_BASE_UC").Arg(_craft.Base.GetName(null)));

		{
			int speed = _craft.Speed;
			if (_craft.IsInDogfight)
			{
				if (_craft.Destination is Ufo ufo)
				{
					speed = ufo.Speed;
				}
			}
			_txtSpeed.SetText(Tr("STR_SPEED_").Arg(Unicode.FormatNumber(speed)));
		}

		_txtMaxSpeed.SetText(Tr("STR_MAXIMUM_SPEED_UC").Arg(Unicode.FormatNumber(_craft.CraftStats.speedMax)));

		string altitude = _craft.Altitude;
		if (_craft.Rules.IsWaterOnly && !_globe.InsideLand(_craft.Longitude, _craft.Latitude))
		{
			altitude = "STR_AIRBORNE";
		}
		_txtAltitude.SetText(Tr("STR_ALTITUDE_").Arg(Tr(altitude)));

		_txtFuel.SetText(Tr("STR_FUEL").Arg(Unicode.FormatPercentage(_craft.FuelPercentage)));

		_txtDamage.SetText(Tr("STR_DAMAGE_UC_").Arg(Unicode.FormatPercentage(_craft.DamagePercentage)));

		_txtShield.SetText(Tr("STR_SHIELD").Arg(Unicode.FormatPercentage(_craft.ShieldPercentage)));
		_txtShield.Visible = _craft.ShieldCapacity != 0;

		for (int i = 0; i < _weaponNum; ++i)
		{
			string wName = _craft.Rules.GetWeaponSlotString(i);
			if (string.IsNullOrEmpty(wName))
			{
				_txtWeaponName[i].Visible = false;
				_txtWeaponAmmo[i].Visible = false;
				continue;
			}
			CraftWeapon w1 = _craft.Weapons[i];
			if (w1 != null)
			{
				_txtWeaponName[i].SetText(Tr(wName).Arg(Tr(w1.Rules.Type)));
				if (w1.Rules.AmmoMax != 0)
					_txtWeaponAmmo[i].SetText(Tr("STR_ROUNDS_").Arg(w1.Ammo));
				else
					_txtWeaponAmmo[i].Visible = false;
			}
			else
			{
				_txtWeaponName[i].SetText(Tr(wName).Arg(Tr("STR_NONE_UC")));
				_txtWeaponAmmo[i].Visible = false;
			}
		}

		_txtRedirect.SetBig();
		_txtRedirect.Align = TextHAlign.ALIGN_CENTER;
		_txtRedirect.SetText(Tr("STR_REDIRECT_CRAFT"));

		_txtSoldier.SetText($"{(string)Tr("STR_SOLDIERS_UC")}>{Unicode.TOK_COLOR_FLIP}{_craft.NumTotalSoldiers}");

		_txtHWP.SetText($"{(string)Tr("STR_HWPS")}>{Unicode.TOK_COLOR_FLIP}{_craft.NumTotalVehicles}");

		if (_waypoint == null)
		{
			_txtRedirect.Visible = false;

			// ETA display
			if (Options.oxceShowETAMode > 0 && _craft.Destination != null)
			{
				if (Options.oxceShowETAMode == 1 && _craft.Destination is MovingTarget mt && mt.Speed > 0)
				{
					// don't show ETA for moving targets (i.e. UFOs and crafts)
				}
				else
				{
					int speed = _craft.CraftStats.speedMax;
					int distance = Craft.XcomDistance(_craft.GetDistance(_craft.Destination));
					int etaInHoursHelper = (distance + (speed / 2)) / speed;
					int days = etaInHoursHelper / 24;
					int hours = etaInHoursHelper % 24;
					StringBuilder ssStatus = new StringBuilder();
					if (days > 0) ssStatus.Append((string)Tr("STR_DAY_SHORT").Arg(days));
					if (hours > 0 || days == 0)
					{
						if (days > 0) ssStatus.Append('/');
						ssStatus.Append((string)Tr("STR_HOUR_SHORT").Arg(hours));
					}
					_txtETA.Align = TextHAlign.ALIGN_CENTER;
					_txtETA.SetText(Tr("STR_ETA").Arg(ssStatus.ToString()));
				}
			}
		}
		else
		{
			_btnCancel.Text = Tr("STR_GO_TO_LAST_KNOWN_UFO_POSITION");
		}

		if (_craft.LowFuel || _craft.MissionComplete)
		{
			_btnBase.Visible = false;
			_btnTarget.Visible = false;
			_btnPatrol.Visible = false;
		}

		if (_craft.Rules.MaxUnitsLimit == 0)
			_txtSoldier.Visible = false;
		if (_craft.Rules.MaxVehiclesAndLargeSoldiersLimit == 0)
			_txtHWP.Visible = false;
	}

	/// <summary>
	/// Returns the craft back to its base.
	/// </summary>
	public void BtnBaseClick(Action action)
	{
		_game.PopState();
		_craft.ReturnToBase();
		_waypoint?.Destroy(); // C++: delete _waypoint;
		if (_craft.Rules.CanAutoPatrol())
		{
			// cancel auto-patrol
			_craft.IsAutoPatrolling = false;
		}
	}

	/// <summary>
	/// Changes the craft's target.
	/// </summary>
	public void BtnTargetClick(Action action)
	{
		_game.PopState();
		_game.PushState(new SelectDestinationState([_craft], _globe));
		_waypoint?.Destroy(); // C++: delete _waypoint;
	}

	/// <summary>
	/// Sets the craft to patrol the current location.
	/// </summary>
	public void BtnPatrolClick(Action action)
	{
		_game.PopState();
		_craft.Destination = null;
		_waypoint?.Destroy(); // C++: delete _waypoint;
		if (_craft.Rules.CanAutoPatrol())
		{
			// start auto-patrol
			_craft.LatitudeAuto = _craft.Latitude;
			_craft.LongitudeAuto = _craft.Longitude;
			_craft.IsAutoPatrolling = true;
		}
	}

	/// <summary>
	/// Closes the window.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		// Go to the last known UFO position
		if (_waypoint != null)
		{
			_waypoint.Id = _game.SavedGame.GetId("STR_WAY_POINT");
			_game.SavedGame.Waypoints.Add(_waypoint);
			_craft.Destination = _waypoint;
		}
		// Cancel
		_game.PopState();
	}
}
