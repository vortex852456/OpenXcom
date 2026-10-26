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
// Ported from: src/Geoscape/ConfirmLandingState.h, src/Geoscape/ConfirmLandingState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window that allows the player
/// to confirm a Craft landing at its destination.
/// </summary>
public class ConfirmLandingState : State
{
	private readonly Craft _craft;
	private readonly Window _window;
	private readonly Texture _missionTexture, _globeTexture;
	private readonly int _shade;
	private readonly Text _txtMessage, _txtBegin;
	private readonly TextButton _btnYes, _btnNo;
	private readonly Surface _sprite;

	/// <summary>
	/// Initializes all the elements in the Confirm Landing window.
	/// </summary>
	/// <param name="craft">Pointer to the craft to confirm.</param>
	/// <param name="missionTexture">Texture of the landing site.</param>
	/// <param name="globeTexture">Texture of the landing site.</param>
	/// <param name="shade">Shade of the landing site.</param>
	public ConfirmLandingState(Craft craft, Texture missionTexture, Texture globeTexture, int shade)
	{
		_craft = craft;
		_missionTexture = missionTexture;
		_globeTexture = globeTexture;
		_shade = shade;
		_screen = false;

		// Create objects
		_window = new Window(this, 216, 160, 20, 20, WindowPopup.POPUP_BOTH);
		_btnYes = new TextButton(80, 20, 40, 150);
		_btnNo = new TextButton(80, 20, 136, 150);
		_txtMessage = new Text(206, 80, 25, 40);
		_txtBegin = new Text(206, 17, 25, 130);
		_sprite = new Surface(24, 24, 202, 30);

		// Set palette
		SetInterface("confirmLanding");

		Add(_window, "window", "confirmLanding");
		Add(_btnYes, "button", "confirmLanding");
		Add(_btnNo, "button", "confirmLanding");
		Add(_txtMessage, "text", "confirmLanding");
		Add(_txtBegin, "text", "confirmLanding");
		Add(_sprite);

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "confirmLanding");

		_btnYes.Text = Tr("STR_YES");
		_btnYes.OnMouseClick(BtnYesClick);
		_btnYes.OnKeyboardPress(BtnYesClick, Options.keyOk);

		if (_game.IsCtrlPressed())
		{
			_btnNo.Text = Tr("STR_PATROL");
		}
		else
		{
			_btnNo.Text = Tr("STR_NO");
		}
		_btnNo.OnMouseClick(BtnNoClick);
		_btnNo.OnKeyboardPress(BtnNoClick, Options.keyCancel);
		_btnNo.OnKeyboardPress(TogglePatrolButton, SDLKey.SDLK_LCTRL);
		_btnNo.OnKeyboardRelease(TogglePatrolButton, SDLKey.SDLK_LCTRL);
		_btnNo.OnKeyboardPress(TogglePatrolButton, SDLKey.SDLK_RCTRL);
		_btnNo.OnKeyboardRelease(TogglePatrolButton, SDLKey.SDLK_RCTRL);

		_txtMessage.SetBig();
		_txtMessage.Align = TextHAlign.ALIGN_CENTER;
		_txtMessage.SetWordWrap(true);
		_txtMessage.SetText(Tr("STR_CRAFT_READY_TO_LAND_NEAR_DESTINATION")
							 .Arg(_craft.GetName(_game.Language))
							 .Arg(_craft.Destination.GetName(_game.Language)));

		_txtBegin.SetBig();
		_txtBegin.Align = TextHAlign.ALIGN_CENTER;
		_txtBegin.SetText(Unicode.TOK_COLOR_FLIP + (string)Tr("STR_BEGIN_MISSION"));

		SurfaceSet sprites = _game.Mod.GetSurfaceSet("DayNightIndicator", false);
		if (sprites != null)
		{
			if (_shade <= 0)
			{
				// day (0)
				sprites.GetFrame(0).BlitNShade(_sprite, 0, 0);
			}
			else if (_shade > _game.Mod.MaxDarknessToSeeUnits)
			{
				// night (10-15); note: this is configurable in the ruleset (in OXCE only)
				sprites.GetFrame(1).BlitNShade(_sprite, 0, 0);
			}
			else
			{
				// dusk/dawn (1-9)
				sprites.GetFrame(2).BlitNShade(_sprite, 0, 0);
			}
		}
	}

	/// <summary>
	/// Make sure we aren't returning to base.
	/// </summary>
	public override void Init()
	{
		base.Init();
		Base b = _craft.Destination as Base;
		if (b == _craft.Base)
			_game.PopState();
	}

	/// <summary>
	/// Checks the starting condition.
	/// </summary>
	private string CheckStartingCondition()
	{
		if (_craft.AreBannedArmorsOnboard())
		{
			return Tr("STR_ARMOR_NOT_ALLOWED_ONBOARD");
		}

		Ufo u = _craft.Destination as Ufo;
		MissionSite m = _craft.Destination as MissionSite;
		AlienBase b = _craft.Destination as AlienBase;

		AlienDeployment ruleDeploy = null;
		if (u != null)
		{
			string ufoMissionName = u.Rules.Type;
			if (_missionTexture != null && _missionTexture.IsFakeUnderwater)
			{
				ufoMissionName = u.Rules.Type + "_UNDERWATER";
			}
			ruleDeploy = _game.Mod.GetDeployment(ufoMissionName);
		}
		else if (m != null)
		{
			ruleDeploy = _game.Mod.GetDeployment(m.Deployment.Type);
		}
		else if (b != null)
		{
			AlienRace race = _game.Mod.GetAlienRace(b.AlienRace);
			ruleDeploy = _game.Mod.GetDeployment(race.BaseCustomMission);
			if (ruleDeploy == null) ruleDeploy = _game.Mod.GetDeployment(b.Deployment.Type);
		}
		else
		{
			// irrelevant for this check
			return "";
		}

		if (ruleDeploy == null)
		{
			// just in case
			return "";
		}

		RuleStartingCondition rule = _game.Mod.GetStartingCondition(ruleDeploy.StartingCondition);
		if (rule == null && _missionTexture != null)
		{
			rule = _game.Mod.GetStartingCondition(_missionTexture.StartingCondition);
		}
		if (rule != null)
		{
			if (rule.RequiresCommanderOnboard() && !_craft.IsCommanderOnboard)
			{
				if (u == null || u.Status == Ufo.UfoStatus.LANDED || u.Status == Ufo.UfoStatus.CRASHED)
				{
					return Tr("STR_STARTING_CONDITION_COMMANDER");
				}
			}
			if (!rule.IsCraftPermitted(_craft.Rules.Type))
			{
				return Tr("STR_STARTING_CONDITION_CRAFT"); // simple message without details/argument
			}
			if (!_craft.AreOnlyPermittedSoldierTypesOnboard(rule))
			{
				return Tr("STR_STARTING_CONDITION_SOLDIER_TYPE"); // simple message without details/argument
			}
			if (!_craft.AreRequiredItemsOnboard(rule.RequiredItems))
			{
				return Tr("STR_STARTING_CONDITION_ITEM"); // simple message without details/argument
			}
			else
			{
				if (rule.DestroyRequiredItems)
				{
					_craft.DestroyRequiredItems(rule.RequiredItems);
				}
			}
		}
		return "";
	}

	/// <summary>
	/// Enters the mission.
	/// </summary>
	public void BtnYesClick(Action action)
	{
		string message = CheckStartingCondition();
		if (message.Length != 0)
		{
			_craft.ReturnToBase();
			_game.PopState();
			_game.PushState(new CraftErrorState(null, message));
			return;
		}

		_game.PopState();
		Ufo u = _craft.Destination as Ufo;
		MissionSite m = _craft.Destination as MissionSite;
		AlienBase b = _craft.Destination as AlienBase;

		SavedBattleGame bgame = new SavedBattleGame(_game.Mod, _game.Language);
		_game.SavedGame.SetBattleGame(bgame);
		BattlescapeGenerator bgen = new BattlescapeGenerator(_game);
		bgen.SetWorldTexture(_missionTexture, _globeTexture);
		bgen.SetWorldShade(_shade);
		bgen.SetCraft(_craft);
		if (u != null)
		{
			if (u.Status == Ufo.UfoStatus.CRASHED)
				bgame.MissionType = "STR_UFO_CRASH_RECOVERY";
			else
				bgame.MissionType = "STR_UFO_GROUND_ASSAULT";
			bgen.SetUfo(u);
			AlienDeployment customWeaponDeploy = _game.Mod.GetDeployment(u.CraftStats.craftCustomDeploy);
			if (_missionTexture != null && _missionTexture.IsFakeUnderwater)
			{
				string ufoUnderwaterMissionName = u.Rules.Type + "_UNDERWATER";
				AlienDeployment ufoUnderwaterMission = _game.Mod.GetDeployment(ufoUnderwaterMissionName, true);
				bgen.SetAlienCustomDeploy(customWeaponDeploy, ufoUnderwaterMission);
			}
			else
			{
				bgen.SetAlienCustomDeploy(customWeaponDeploy);
			}
			bgen.SetAlienRace(u.AlienRace);
		}
		else if (m != null)
		{
			bgame.MissionType = m.Deployment.Type;
			bgen.SetMissionSite(m);
			bgen.SetAlienCustomDeploy(m.MissionCustomDeploy);
			bgen.SetAlienRace(m.AlienRace);
		}
		else if (b != null)
		{
			AlienRace race = _game.Mod.GetAlienRace(b.AlienRace);
			bgame.MissionType = b.Deployment.Type;
			bgen.SetAlienBase(b);
			bgen.SetAlienRace(b.AlienRace);
			bgen.SetAlienCustomDeploy(_game.Mod.GetDeployment(race.BaseCustomDeploy), _game.Mod.GetDeployment(race.BaseCustomMission));
			bgen.SetWorldTexture(null, _globeTexture);
		}
		else
		{
			throw new InvalidOperationException("No mission available!");
		}
		bgen.Run();
		_game.PushState(new BriefingState(_craft, null));
	}

	/// <summary>
	/// Returns the craft to base and closes the window.
	/// </summary>
	public void BtnNoClick(Action action)
	{
		if (_game.IsCtrlPressed())
		{
			_craft.Destination = null;
		}
		else
		{
			_craft.ReturnToBase();
		}
		_game.PopState();
	}

	/// <summary>
	/// Toggles No/Patrol button.
	/// </summary>
	public void TogglePatrolButton(Action action)
	{
		if (_game.IsCtrlPressed())
		{
			_btnNo.Text = Tr("STR_PATROL");
		}
		else
		{
			_btnNo.Text = Tr("STR_NO");
		}
	}
}
