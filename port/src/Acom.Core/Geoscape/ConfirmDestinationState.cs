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
// Ported from: src/Geoscape/ConfirmDestinationState.h, src/Geoscape/ConfirmDestinationState.cpp
#nullable disable

using System.Text;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window that allows the player
/// to confirm a Craft's new destination.
/// </summary>
public class ConfirmDestinationState : State
{
	private readonly List<Craft> _crafts;
	private readonly Target _target;
	private readonly Window _window;
	private readonly Text _txtTarget;
	private readonly Text _txtETA;
	private readonly TextButton _btnOk, _btnTransfer, _btnCancel;
	private readonly ToggleTextButton _btnFollowWingLeader;

	/// <summary>
	/// Initializes all the elements in the Confirm Destination window.
	/// </summary>
	/// <param name="crafts">Pointers to the crafts to retarget.</param>
	/// <param name="target">Pointer to the selected target (NULL if it's just a point on the globe).</param>
	public ConfirmDestinationState(List<Craft> crafts, Target target)
	{
		_crafts = [.. crafts]; // passed by value in C++
		_target = target;

		Waypoint w = _target as Waypoint;
		_screen = false;

		Base @base = _target as Base;
		bool transferAvailable = false; // no transfer allowed for wings
		if (_crafts.Count == 1)
		{
			transferAvailable = Options.canTransferCraftsWhileAirborne && @base != null && @base != _crafts[0].Base && _crafts[0].ArePilotsOnboard(_game.Mod);
		}
		int btnOkX = transferAvailable ? 29 : 68;
		int btnCancelX = transferAvailable ? 177 : 138;

		// Create objects
		_window = new Window(this, 244, 72, 6, 64);
		_btnOk = new TextButton(50, 12, btnOkX, 104);
		_btnTransfer = new TextButton(82, 12, 87, 104);
		_btnCancel = new TextButton(50, 12, btnCancelX, 104);
		_btnFollowWingLeader = new ToggleTextButton(170, 16, 43, 138);
		_txtTarget = new Text(232, 32, 12, 72);
		_txtETA = new Text(232, 9, 12, 120);

		// Set palette
		SetInterface("confirmDestination", w != null && w.Id == 0);

		Add(_window, "window", "confirmDestination");
		Add(_btnOk, "button", "confirmDestination");
		Add(_btnCancel, "button", "confirmDestination");
		Add(_btnTransfer, "button", "confirmDestination");
		Add(_btnFollowWingLeader, "button", "confirmDestination");
		Add(_txtTarget, "text", "confirmDestination");
		Add(_txtETA, "text", "confirmDestination");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "confirmDestination");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);

		_btnTransfer.Text = Tr("STR_TRANSFER_UC");
		_btnTransfer.OnMouseClick(BtnTransferClick);
		_btnTransfer.Visible = transferAvailable;

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_btnFollowWingLeader.Text = Tr("STR_FOLLOW_WING_LEADER_QUESTION");
		_btnFollowWingLeader.Visible = false;
		if (_crafts.Count > 1)
		{
			_btnFollowWingLeader.Visible = true;
			if (_target is Ufo u && u.Status == Ufo.UfoStatus.FLYING)
			{
				_btnFollowWingLeader.Pressed = false; // everybody go for the UFO as quickly as possible
			}
			else
			{
				_btnFollowWingLeader.Pressed = true; // follow the wing leader please
			}
		}

		_txtTarget.SetBig();
		_txtTarget.Align = TextHAlign.ALIGN_CENTER;
		_txtTarget.VerticalAlign = TextVAlign.ALIGN_MIDDLE;
		_txtTarget.SetWordWrap(true);
		if (w != null && w.Id == 0)
		{
			_txtTarget.SetText(Tr("STR_TARGET").Arg(Tr("STR_WAY_POINT")));
		}
		else
		{
			_txtTarget.SetText(Tr("STR_TARGET").Arg(_target.GetName(_game.Language)));
		}

		// ETA display
		if (Options.oxceShowETAMode > 0 && _target != null)
		{
			if (Options.oxceShowETAMode == 1 && _target is MovingTarget mt && mt.Speed > 0)
			{
				// don't show ETA for moving targets (i.e. UFOs and crafts)
			}
			else
			{
				int speed = _crafts[0].CraftStats.speedMax;
				int distance = Craft.XcomDistance(_crafts[0].GetDistance(_target));
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

	/// <summary>
	/// Checks the starting condition.
	/// </summary>
	private string CheckStartingCondition()
	{
		// Check all selected craft
		foreach (var xcraft in _crafts)
		{
			if (xcraft.AreBannedArmorsOnboard())
			{
				return Tr("STR_ARMOR_NOT_ALLOWED_ONBOARD");
			}
		}

		Ufo u = _target as Ufo;
		MissionSite m = _target as MissionSite;
		AlienBase b = _target as AlienBase;

		AlienDeployment ruleDeploy = null;
		if (u != null)
		{
			ruleDeploy = _game.Mod.GetDeployment(u.Rules.Type); // no need to check for fake underwater UFOs here
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
			// for example just a waypoint
			return "";
		}

		if (ruleDeploy == null)
		{
			// e.g. UFOs without alien deployment :(
			return "";
		}

		RuleStartingCondition rule = _game.Mod.GetStartingCondition(ruleDeploy.StartingCondition);
		if (rule == null)
		{
			// rule doesn't exist (mod upgrades?)
			return "";
		}

		if (rule.RequiresCommanderOnboard() && !_crafts[0].IsCommanderOnboard)
		{
			if (u == null || u.Status == Ufo.UfoStatus.LANDED || u.Status == Ufo.UfoStatus.CRASHED)
			{
				return Tr("STR_STARTING_CONDITION_COMMANDER");
			}
		}

		// Only check first selected craft
		// The other crafts will follow the first selected craft so they will not land at the mission site
		// This check is only performed once per wing
		// Checking if _crafts.size() != 1 will disallow the escorting scenario

		// check required item(s)
		var requiredItems = rule.RequiredItems;
		if (!_crafts[0].AreRequiredItemsOnboard(requiredItems))
		{
			StringBuilder ss2 = new StringBuilder();
			int i2 = 0;
			foreach (var pair in requiredItems)
			{
				if (i2 > 0)
					ss2.Append(", ");
				ss2.Append((string)Tr(pair.Key)).Append(": ").Append(pair.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
				i2++;
			}
			string argument2 = ss2.ToString();
			return Tr("STR_STARTING_CONDITION_ITEM").Arg(argument2);
		}

		// check permitted soldiers
		if (!_crafts[0].AreOnlyPermittedSoldierTypesOnboard(rule))
		{
			var soldierList = rule.ForbiddenSoldierTypes.Count == 0 ? rule.AllowedSoldierTypes : rule.ForbiddenSoldierTypes;
			string soldierMessageCode = rule.ForbiddenSoldierTypes.Count == 0 ? "STR_STARTING_CONDITION_SOLDIER_TYPE_ALLOWED" : "STR_STARTING_CONDITION_SOLDIER_TYPE_FORBIDDEN";
			StringBuilder ssS = new StringBuilder();
			int iS = 0;
			foreach (var soldierType in soldierList)
			{
				RuleSoldier soldierTypeRule = _game.Mod.GetSoldier(soldierType, false);
				if (soldierTypeRule != null && _game.SavedGame.IsResearched(soldierTypeRule.Requirements))
				{
					if (iS > 0)
						ssS.Append(", ");
					ssS.Append((string)Tr(soldierType));
					iS++;
				}
			}
			string soldierArgument = ssS.ToString();
			if (soldierArgument.Length == 0)
			{
				// no suitable soldier type yet?
				soldierArgument = Tr("STR_UNKNOWN");
			}
			return Tr(soldierMessageCode).Arg(soldierArgument);
		}

		if (rule.IsCraftPermitted(_crafts[0].Rules.Type))
		{
			// craft is permitted
			return "";
		}

		// craft is not permitted (= either forbidden or not allowed)
		var list = rule.ForbiddenCraft.Count == 0 ? rule.AllowedCraft : rule.ForbiddenCraft;
		string messageCode = rule.ForbiddenCraft.Count == 0 ? "STR_STARTING_CONDITION_CRAFT_ALLOWED" : "STR_STARTING_CONDITION_CRAFT_FORBIDDEN";
		StringBuilder ss = new StringBuilder();
		int i = 0;
		foreach (var articleName in list)
		{
			ArticleDefinition article = _game.Mod.GetUfopaediaArticle(articleName, false);
			if (article != null && Ufopaedia.IsArticleAvailable(_game.SavedGame, article))
			{
				if (i > 0)
					ss.Append(", ");
				ss.Append((string)Tr(articleName));
				i++;
			}
		}
		string argument = ss.ToString();
		if (argument.Length == 0)
		{
			// no suitable craft yet
			argument = Tr("STR_UNKNOWN");
		}
		return Tr(messageCode).Arg(argument);
	}

	/// <summary>
	/// Confirms the selected target for the craft.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		string message = CheckStartingCondition();
		if (message.Length != 0)
		{
			_game.PopState();
			_game.PopState();
			_game.PushState(new CraftErrorState(null, message));
			return;
		}

		foreach (var craft in _crafts)
		{
			if (craft.AreTooManyItemsOnboard())
			{
				_game.PopState();
				_game.PopState();
				string message2 = Tr("STR_TOO_MANY_ITEMS_ONBOARD");
				_game.PushState(new CraftErrorState(null, message2));
				return;
			}
		}

		foreach (var craft in _crafts)
		{
			if (!craft.ArePilotsOnboard(_game.Mod))
			{
				_game.PopState();
				_game.PopState();
				_game.PushState(new CraftNotEnoughPilotsState(craft));
				return;
			}
		}

		Waypoint w = _target as Waypoint;
		if (w != null && w.Id == 0)
		{
			w.Id = _game.SavedGame.GetId("STR_WAY_POINT");
			_game.SavedGame.Waypoints.Add(w);
		}

		// first selected _craft (first shift-clicked craft) is wing leader; the other crafts follow the wing leader
		if (_crafts[0] == _target)
		{
			//setting itself as target works fine but it should be saying "patrolling" instead
			_crafts[0].Destination = null;
		}
		else
		{
			_crafts[0].Destination = _target;
			if (_crafts[0].IsTakingOff)
			{
				if (_crafts[0].Rules.TakeoffSoundRaw.Count != 0)
				{
					_game.Mod.GetSound("GEO.CAT", _crafts[0].Rules.TakeoffSound).Play();
				}
			}
		}

		foreach (var craft in _crafts)
		{
			if (craft != _crafts[0])
			{
				if (_btnFollowWingLeader.Pressed)
				{
					craft.Destination = _crafts[0]; // follow the wing leader
				}
				else
				{
					craft.Destination = _target; // go for the same target as the wing leader
				}
			}
			if (craft.Rules.CanAutoPatrol())
			{
				// cancel auto-patrol
				craft.IsAutoPatrolling = false;
			}
			craft.Status = "STR_OUT";
		}

		_game.PopState();
		_game.PopState();
	}

	/// <summary>
	/// Handles clicking the transfer button.
	/// Performs a transfer of a craft to a targeted base if possible, otherwise pops an error message.
	/// </summary>
	public void BtnTransferClick(Action action)
	{
		string errorMessage = "";

		Base targetBase = _target as Base;
		if ((targetBase.AvailableHangars - targetBase.UsedHangars) <= 0) // don't know how you'd get less than 0 available hangars, but want to handle that just in case
		{
			errorMessage = Tr("STR_NO_FREE_HANGARS_FOR_TRANSFER");
		}
		else if (_crafts[0].NumTotalSoldiers > targetBase.AvailableQuarters - targetBase.UsedQuarters)
		{
			errorMessage = Tr("STR_NO_FREE_ACCOMODATION_CREW");
		}
		else if (Options.storageLimitsEnforced && targetBase.StoresOverfull(_crafts[0].TotalItemStorageSize))
		{
			errorMessage = Tr("STR_NOT_ENOUGH_STORE_SPACE_FOR_CRAFT");
		}
		else if (_crafts[0].Fuel < _crafts[0].GetFuelLimit(targetBase))
		{
			errorMessage = Tr("STR_NOT_ENOUGH_FUEL_TO_REACH_TARGET");
		}

		// clicking transfer will start the craft moving or make us need to pick a new destination
		// either way, we need to get rid of this confirming the destination state
		_game.PopState();

		if (errorMessage.Length == 0)
		{
			// Transfer soldiers inside craft
			Base currentBase = _crafts[0].Base;
			var soldiers = currentBase.Soldiers;
			for (int soldierIt = 0; soldierIt < soldiers.Count;)
			{
				Soldier soldier = soldiers[soldierIt];
				if (soldier.Craft == _crafts[0])
				{
					soldier.SetPsiTraining(false);
					if (soldier.IsInTraining)
					{
						soldier.ReturnToTrainingWhenHealed = true;
					}
					soldier.SetTraining(false);
					targetBase.Soldiers.Add(soldier);
					soldiers.RemoveAt(soldierIt);
				}
				else
				{
					++soldierIt;
				}
			}

			// Transfer craft
			currentBase.RemoveCraft(_crafts[0], false);
			targetBase.Crafts.Add(_crafts[0]);
			_crafts[0].SetBase(targetBase, false);
			_crafts[0].ReturnToBase();
			_crafts[0].Status = "STR_OUT";
			if (_crafts[0].Fuel <= _crafts[0].GetFuelLimit(targetBase))
			{
				_crafts[0].LowFuel = true;
			}

			// pop the selecting the destination state
			_game.PopState();
		}
		else
		{
			RuleInterface menuInterface = _game.Mod.GetInterface("errorMessages");
			_game.PushState(new ErrorMessageState(errorMessage, _palette, unchecked((byte)menuInterface.GetElement("geoscapeColor").color), "BACK13.SCR", menuInterface.GetElement("geoscapePalette").color));
		}
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		if (_target is Waypoint w && w.Id == 0)
		{
			w.Destroy(); // C++: delete w;
		}
		_game.PopState();
	}
}
