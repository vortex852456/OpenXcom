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
// Ported from: src/Basescape/ManufactureState.h, src/Basescape/ManufactureState.cpp
#nullable disable

using System.Globalization;
using System.Text;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Manufacture screen that lets the player manage
/// all the manufacturing operations of a base.
/// </summary>
public class ManufactureState : State
{
	private readonly Base _base;
	private readonly TextButton _btnNew, _btnOk;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtAvailable, _txtAllocated, _txtSpace, _txtFunds, _txtItem, _txtEngineers, _txtProduced, _txtCost, _txtTimeLeft;
	private readonly TextList _lstManufacture;

	/// <summary>
	/// Initializes all the elements in the Manufacture screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	public ManufactureState(Base @base)
	{
		_base = @base;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnNew = new TextButton(148, 16, 8, 176);
		_btnOk = new TextButton(148, 16, 164, 176);
		_txtTitle = new Text(310, 17, 5, 8);
		_txtAvailable = new Text(150, 9, 8, 24);
		_txtAllocated = new Text(150, 9, 160, 24);
		_txtSpace = new Text(150, 9, 8, 34);
		_txtFunds = new Text(150, 9, 160, 34);
		_txtItem = new Text(80, 9, 10, 52);
		_txtEngineers = new Text(56, 18, 112, 44);
		_txtProduced = new Text(56, 18, 168, 44);
		_txtCost = new Text(44, 27, 222, 44);
		_txtTimeLeft = new Text(60, 27, 260, 44);
		_lstManufacture = new TextList(288, 88, 8, 80);

		// Set palette
		SetInterface("manufactureMenu");

		Add(_window, "window", "manufactureMenu");
		Add(_btnNew, "button", "manufactureMenu");
		Add(_btnOk, "button", "manufactureMenu");
		Add(_txtTitle, "text1", "manufactureMenu");
		Add(_txtAvailable, "text1", "manufactureMenu");
		Add(_txtAllocated, "text1", "manufactureMenu");
		Add(_txtSpace, "text1", "manufactureMenu");
		Add(_txtFunds, "text1", "manufactureMenu");
		Add(_txtItem, "text2", "manufactureMenu");
		Add(_txtEngineers, "text2", "manufactureMenu");
		Add(_txtProduced, "text2", "manufactureMenu");
		Add(_txtCost, "text2", "manufactureMenu");
		Add(_txtTimeLeft, "text2", "manufactureMenu");
		Add(_lstManufacture, "list", "manufactureMenu");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "manufactureMenu");

		_btnNew.Text = Tr("STR_NEW_PRODUCTION");
		_btnNew.OnMouseClick(BtnNewProductionClick);
		_btnNew.OnKeyboardPress(BtnNewProductionClick, Options.keyToggleQuickSearch);
		_btnNew.OnKeyboardPress(OnCurrentGlobalProductionClick, Options.keyGeoGlobalProduction);

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_CURRENT_PRODUCTION"));

		_txtItem.SetText(Tr("STR_ITEM"));

		_txtEngineers.SetText(Tr("STR_ENGINEERS__ALLOCATED"));
		_txtEngineers.SetWordWrap(true);

		_txtProduced.SetText(Tr("STR_UNITS_PRODUCED"));
		_txtProduced.SetWordWrap(true);

		_txtCost.SetText(Tr("STR_COST__PER__UNIT"));
		_txtCost.SetWordWrap(true);

		_txtTimeLeft.SetText(Tr("STR_DAYS_HOURS_LEFT"));
		_txtTimeLeft.SetWordWrap(true);

		_lstManufacture.SetColumns(5, 114, 16, 52, 56, 48);
		_lstManufacture.SetAlign(TextHAlign.ALIGN_RIGHT);
		_lstManufacture.SetAlign(TextHAlign.ALIGN_LEFT, 0);
		_lstManufacture.SetSelectable(true);
		_lstManufacture.SetBackground(_window);
		_lstManufacture.Margin = 2;
		_lstManufacture.SetWordWrap(true);
		_lstManufacture.OnMouseClick(LstManufactureClickLeft, SDLMouse.SDL_BUTTON_LEFT);
		_lstManufacture.OnMouseClick(LstManufactureClickMiddle, SDLMouse.SDL_BUTTON_MIDDLE);
		_lstManufacture.OnMousePress(LstManufactureMousePress);
	}

	/// <summary>
	/// Updates the production list
	/// after going to other screens.
	/// </summary>
	public override void Init()
	{
		base.Init();
		FillProductionList(0);

		if (Options.oxceManufactureScrollSpeed > 0 || Options.oxceManufactureScrollSpeedWithCtrl > 0)
		{
			// 140 +/- 20
			_lstManufacture.SetNoScrollArea(_txtAllocated.X - 40, _txtAllocated.X);
		}
		else
		{
			_lstManufacture.SetNoScrollArea(0, 0);
		}
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Opens the Global Production UI.
	/// </summary>
	public void OnCurrentGlobalProductionClick(Action action)
	{
		_game.PushState(new GlobalManufactureState(true));
	}

	/// <summary>
	/// Opens the screen with the list of possible productions.
	/// </summary>
	public void BtnNewProductionClick(Action action)
	{
		_game.PushState(new NewManufactureListState(_base));
	}

	/// <summary>
	/// Fills the list of base productions.
	/// </summary>
	public void FillProductionList(int scrl)
	{
		_lstManufacture.ClearList();
		foreach (var prod in _base.Productions)
		{
			string s1 = prod.AssignedEngineers.ToString(CultureInfo.InvariantCulture);
			StringBuilder s2 = new StringBuilder();
			s2.Append(prod.AmountProduced.ToString(CultureInfo.InvariantCulture)).Append('/');
			if (prod.InfiniteAmount) s2.Append('∞');
			else s2.Append(prod.AmountTotal.ToString(CultureInfo.InvariantCulture));
			if (prod.SellItems) s2.Append(" $");
			string s3 = Unicode.FormatFunding(prod.Rules.ManufactureCost);
			string s4;
			if (prod.InfiniteAmount)
			{
				s4 = "∞";
			}
			else if (prod.AssignedEngineers > 0)
			{
				int timeLeft = prod.AmountTotal * prod.Rules.ManufactureTime - prod.TimeSpent;
				int numEffectiveEngineers = prod.AssignedEngineers;
				// ensure we round up since it takes an entire hour to manufacture any part of that hour's capacity
				int hoursLeft = (timeLeft + numEffectiveEngineers - 1) / numEffectiveEngineers;
				int daysLeft = hoursLeft / 24;
				int hours = hoursLeft % 24;
				s4 = daysLeft.ToString(CultureInfo.InvariantCulture) + "/" + hours.ToString(CultureInfo.InvariantCulture);
			}
			else
			{
				s4 = "-";
			}
			_lstManufacture.AddRow(5, Tr(prod.Rules.Name), s1, s2.ToString(), s3, s4);
		}
		_txtAvailable.SetText(Tr("STR_ENGINEERS_AVAILABLE").Arg(_base.AvailableEngineers));
		_txtAllocated.SetText(Tr("STR_ENGINEERS_ALLOCATED").Arg(_base.AllocatedEngineers));
		_txtSpace.SetText(Tr("STR_WORKSHOP_SPACE_AVAILABLE").Arg(_base.FreeWorkshops));
		_txtFunds.SetText(Tr("STR_CURRENT_FUNDS").Arg(Unicode.FormatFunding(_game.SavedGame.Funds)));

		if (scrl != 0)
			_lstManufacture.ScrollTo(scrl);
	}

	/// <summary>
	/// Opens the screen displaying production settings.
	/// </summary>
	private void LstManufactureClickLeft(Action action)
	{
		var productions = _base.Productions;
		_game.PushState(new ManufactureInfoState(_base, productions[(int)_lstManufacture.SelectedRow]));
	}

	/// <summary>
	/// Opens the TechTreeViewer for the corresponding topic.
	/// </summary>
	private void LstManufactureClickMiddle(Action action)
	{
		var productions = _base.Productions;
		RuleManufacture selectedTopic = productions[(int)_lstManufacture.SelectedRow].Rules;
		if (_game.IsCtrlPressed())
		{
			string articleId = selectedTopic.Name;
			Ufopaedia.OpenArticle(_game, articleId);
		}
		else
		{
			_game.PushState(new TechTreeViewerState(null, selectedTopic));
		}
	}

	/// <summary>
	/// Handles the mouse-wheels.
	/// </summary>
	private void LstManufactureMousePress(Action action)
	{
		if (!_lstManufacture.IsInsideNoScrollArea((int)action.AbsoluteXMouse))
		{
			return;
		}

		int change = Options.oxceManufactureScrollSpeed;
		if (_game.IsCtrlPressed())
			change = Options.oxceManufactureScrollSpeedWithCtrl;

		if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELUP)
		{
			Production selectedProject = _base.Productions[(int)_lstManufacture.SelectedRow];
			int availableWorkSpace = _base.FreeWorkshops;
			int timeLeft = selectedProject.AmountTotal * selectedProject.Rules.ManufactureTime - selectedProject.TimeSpent;
			if (selectedProject.IsQueuedOnly)
			{
				// start counting the workshop space now
				availableWorkSpace -= selectedProject.Rules.RequiredSpace;
			}
			change = Math.Min(change, _base.AvailableEngineers);
			change = Math.Min(change, availableWorkSpace);
			if (!selectedProject.InfiniteAmount)
				change = Math.Min(change, timeLeft - selectedProject.AssignedEngineers);

			if (change > 0)
			{
				selectedProject.AssignedEngineers = selectedProject.AssignedEngineers + change;
				_base.Engineers = _base.Engineers - change;
				FillProductionList(_lstManufacture.Scroll);
			}
		}
		else if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELDOWN)
		{
			Production selectedProject = _base.Productions[(int)_lstManufacture.SelectedRow];
			change = Math.Min(change, selectedProject.AssignedEngineers);
			if (change > 0)
			{
				selectedProject.AssignedEngineers = selectedProject.AssignedEngineers - change;
				_base.Engineers = _base.Engineers + change;
				FillProductionList(_lstManufacture.Scroll);
			}
		}
	}
}
