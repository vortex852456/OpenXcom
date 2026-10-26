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
// Ported from: src/Basescape/ManageAlienContainmentState.h, src/Basescape/ManageAlienContainmentState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// ManageAlienContainment screen that lets the player manage
/// alien numbers in a particular base.
/// </summary>
public class ManageAlienContainmentState : TouchState
{
	private readonly Base _base;
	private readonly int _prisonType;
	private readonly OptionsOrigin _origin;
	private readonly TextButton _btnOk, _btnSell, _btnCancel, _btnTransfer, _btnCleanup;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtUsed, _txtAvailable, _txtValueOfSales, _txtItem, _txtLiveAliens, _txtDeadAliens, _txtInterrogatedAliens;
	private readonly TextList _lstAliens;
	private readonly Timer _timerInc, _timerDec;
	private readonly List<int> _qtys = [];
	private readonly List<string> _aliens = [];
	private int _sel;
	private int _aliensSold;
	private long _total;
	private bool _doNotReset;
	private readonly bool _threeButtons;

	/// <summary>
	/// Initializes all the elements in the Manage Alien Containment screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="prisonType">Type of the prison to manage.</param>
	/// <param name="origin">Game section that originated this state.</param>
	public ManageAlienContainmentState(Base @base, int prisonType, OptionsOrigin origin)
	{
		_base = @base;
		_prisonType = prisonType;
		_origin = origin;
		_sel = 0;
		_aliensSold = 0;
		_total = 0;
		_doNotReset = false;
		_threeButtons = false;

		_threeButtons = Options.canSellLiveAliens && Options.retainCorpses;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		if (_threeButtons)
		{
			// 3 buttons
			_btnOk = new TextButton(96, 16, 8, 176);
			_btnSell = new TextButton(96, 16, 112, 176);
			_btnCancel = new TextButton(96, 16, 216, 176);
			_btnTransfer = new TextButton(96, 16, 216, 176);
			_btnCleanup = new TextButton(96, 16, 8, 176);
		}
		else
		{
			// 2 buttons
			_btnOk = new TextButton(148, 16, 8, 176);
			_btnSell = new TextButton(148, 16, 8, 176);
			_btnCancel = new TextButton(148, 16, 164, 176);
			_btnTransfer = new TextButton(148, 16, 164, 176);
			_btnCleanup = new TextButton(148, 16, 8, 176);
		}
		_txtTitle = new Text(310, 17, 5, 8);
		_txtAvailable = new Text(190, 9, 10, 24);
		_txtValueOfSales = new Text(190, 9, 10, 32);
		_txtUsed = new Text(110, 9, 136, 24);
		_txtItem = new Text(120, 9, 10, 41);
		_txtLiveAliens = new Text(54, 18, 153, 32);
		_txtDeadAliens = new Text(54, 18, 207, 32);
		_txtInterrogatedAliens = new Text(54, 18, 261, 32);
		_lstAliens = new TextList(286, 112, 8, 53);

		TouchComponentsCreate(_txtTitle);

		// Set palette
		SetInterface("manageContainment");

		Add(_window, "window", "manageContainment");
		Add(_btnSell, "button", "manageContainment");
		Add(_btnOk, "button", "manageContainment");
		Add(_btnCancel, "button", "manageContainment");
		Add(_btnTransfer, "button", "manageContainment");
		Add(_btnCleanup, "button", "manageContainment");
		Add(_txtTitle, "text", "manageContainment");
		Add(_txtAvailable, "text", "manageContainment");
		Add(_txtValueOfSales, "text", "manageContainment");
		Add(_txtUsed, "text", "manageContainment");
		Add(_txtItem, "text", "manageContainment");
		Add(_txtLiveAliens, "text", "manageContainment");
		Add(_txtDeadAliens, "text", "manageContainment");
		Add(_txtInterrogatedAliens, "text", "manageContainment");
		Add(_lstAliens, "list", "manageContainment");

		TouchComponentsAdd("button2", "manageContainment", _window);

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "manageContainment");

		TouchComponentsConfigure();

		_btnOk.Text = TrAlt(_threeButtons ? "STR_KILL_SELECTED" : "STR_REMOVE_SELECTED", _prisonType);
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(OnGlobalAlienContainmentClick, Options.keyGeoGlobalAlienContainment);

		_btnSell.Text = TrAlt("STR_SELL_SELECTED", _prisonType);
		_btnSell.OnMouseClick(BtnSellClick);

		_btnCancel.Text = Tr("STR_CANCEL");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_btnTransfer.Text = Tr("STR_GO_TO_TRANSFERS");
		_btnTransfer.OnMouseClick(BtnTransferClick);

		_btnCleanup.Text = Tr("STR_PRISON_CLEANUP");
		_btnCleanup.OnMouseClick(BtnCleanupClick);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(TrAlt("STR_MANAGE_CONTAINMENT", _prisonType));

		_txtItem.SetText(TrAlt("STR_ALIEN", _prisonType));

		_txtLiveAliens.SetText(TrAlt("STR_LIVE_ALIENS", _prisonType));
		_txtLiveAliens.SetWordWrap(true);
		_txtLiveAliens.VerticalAlign = TextVAlign.ALIGN_BOTTOM;

		_txtDeadAliens.SetText(TrAlt("STR_DEAD_ALIENS", _prisonType));
		_txtDeadAliens.SetWordWrap(true);
		_txtDeadAliens.VerticalAlign = TextVAlign.ALIGN_BOTTOM;

		_txtInterrogatedAliens.SetText(TrAlt("STR_UNDER_INTERROGATION", _prisonType));
		_txtInterrogatedAliens.SetWordWrap(true);
		_txtInterrogatedAliens.VerticalAlign = TextVAlign.ALIGN_BOTTOM;

		_lstAliens.SetArrowColumn(184, ArrowOrientation.ARROW_HORIZONTAL);
		if (Options.canSellLiveAliens)
		{
			_lstAliens.SetColumns(5, 120, 40, 64, 46, 46);
		}
		else
		{
			_lstAliens.SetColumns(5, 150, 10, 64, 46, 46);
		}
		_lstAliens.SetSelectable(true);
		_lstAliens.SetBackground(_window);
		_lstAliens.Margin = 2;
		_lstAliens.OnLeftArrowPress(LstItemsLeftArrowPress);
		_lstAliens.OnLeftArrowRelease(LstItemsLeftArrowRelease);
		_lstAliens.OnLeftArrowClick(LstItemsLeftArrowClick);
		_lstAliens.OnRightArrowPress(LstItemsRightArrowPress);
		_lstAliens.OnRightArrowRelease(LstItemsRightArrowRelease);
		_lstAliens.OnRightArrowClick(LstItemsRightArrowClick);
		_lstAliens.OnMousePress(LstItemsMousePress);

		_timerInc = new Timer(250);
		_timerInc.OnTimer((StateHandler)Increase);
		_timerDec = new Timer(250);
		_timerDec.OnTimer((StateHandler)Decrease);
	}

	/// <summary>
	/// Resets stuff when coming back from other screens.
	/// </summary>
	public override void Init()
	{
		base.Init();

		// coming back from TechTreeViewer
		if (_doNotReset)
		{
			_doNotReset = false;
			return;
		}

		ResetListAndTotals();

		TouchComponentsRefresh();
	}

	/// <summary>
	/// Resets the list and the totals, updates button visibility.
	/// </summary>
	public void ResetListAndTotals()
	{
		_qtys.Clear();
		_aliens.Clear();
		_sel = 0;
		_aliensSold = 0;
		_total = 0;

		_lstAliens.ClearList();

		List<string> researchList = [];
		foreach (var proj in _base.Research)
		{
			RuleResearch research = proj.Rules;
			RuleItem item = research.NeededItem;
			if (research.IsHoldingNeededItem && item != null && item.IsAlien && item.PrisonType == _prisonType)
			{
				researchList.Add(research.Name);
			}
		}

		foreach (var itemType in _game.Mod.ItemsList)
		{
			RuleItem rule = _game.Mod.GetItem(itemType, true);

			int qty = _base.StorageItems.GetItem(rule);
			if (qty > 0 && rule.IsAlien && rule.PrisonType == _prisonType)
			{
				_qtys.Add(0);
				_aliens.Add(itemType);
				string ss = qty.ToString(CultureInfo.InvariantCulture);
				string rqty;
				int researchIt = researchList.IndexOf(itemType);
				if (researchIt != -1)
				{
					rqty = "1";
					researchList.RemoveAt(researchIt);
				}
				else
				{
					rqty = "0";
				}

				string formattedCost = "";
				if (Options.canSellLiveAliens)
				{
					long adjustedCost = rule.GetSellCostAdjusted(_base, _game.SavedGame);
					formattedCost = Unicode.FormatFunding(adjustedCost / 1000) + "K";
				}

				_lstAliens.AddRow(5, Tr(itemType), formattedCost, ss, "0", rqty);
			}
		}

		foreach (var researchName in researchList)
		{
			_aliens.Add(researchName);
			_qtys.Add(0);
			_lstAliens.AddRow(5, Tr(researchName), Options.canSellLiveAliens ? "-" : "", "0", "0", "1");
			_lstAliens.SetRowColor(_qtys.Count - 1, _lstAliens.SecondaryColor);
		}

		// update totals
		int availableContainment = _base.GetAvailableContainment(_prisonType);
		int usedContainment = _base.GetUsedContainment(_prisonType);
		int freeContainment = availableContainment - usedContainment;
		{
			_txtAvailable.SetText(Tr("STR_SPACE_AVAILABLE").Arg(freeContainment));

			_txtUsed.SetText(Tr("STR_SPACE_USED").Arg(usedContainment));

			if (Options.canSellLiveAliens)
			{
				_txtValueOfSales.SetText(Tr("STR_VALUE_OF_SALES").Arg(Unicode.FormatFunding(_total)));
			}
		}

		// update buttons
		{
			bool overCrowded = false;
			if (availableContainment == 0 || Options.storageLimitsEnforced)
			{
				overCrowded = (freeContainment < 0);
			}

			_btnCancel.Visible = !overCrowded;
			_btnOk.Visible = !overCrowded;
			_btnSell.Visible = !overCrowded && _threeButtons;
			_btnTransfer.Visible = overCrowded;

			int usedContainmentExternal = _base.GetUsedContainment(_prisonType, true);
			bool needToCleanup = overCrowded && usedContainmentExternal > 0 && availableContainment < usedContainmentExternal;

			_btnCleanup.Visible = needToCleanup;
		}
	}

	/// <summary>
	/// Runs the arrow timers.
	/// </summary>
	public override void Think()
	{
		base.Think();

		_timerInc.Think(this, null);
		_timerDec.Think(this, null);
	}

	/// <summary>
	/// Deals with the selected aliens.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		bool sell = Options.canSellLiveAliens && !Options.retainCorpses; // in all other cases, it's kill, not sell
		DealWithSelectedAliens(sell);
	}

	/// <summary>
	/// Opens the Global Alien Containment UI.
	/// </summary>
	public void OnGlobalAlienContainmentClick(Action action)
	{
		_game.PushState(new GlobalAlienContainmentState(true));
	}

	/// <summary>
	/// Deals with the selected aliens.
	/// </summary>
	public void BtnSellClick(Action action)
	{
		DealWithSelectedAliens(true); // this one is always sell
	}

	/// <summary>
	/// Deals with the selected aliens.
	/// </summary>
	private void DealWithSelectedAliens(bool sell)
	{
		for (int i = 0; i < _qtys.Count; ++i)
		{
			if (_qtys[i] > 0)
			{
				// remove the aliens
				_base.StorageItems.RemoveItem(_aliens[i], _qtys[i]);

				if (sell)
				{
					long adjustedCost = _game.Mod.GetItem(_aliens[i], true).GetSellCostAdjusted(_base, _game.SavedGame);
					adjustedCost = adjustedCost * _qtys[i];
					_game.SavedGame.Funds = _game.SavedGame.Funds + adjustedCost;
				}
				else
				{
					// add the corpses
					Unit ruleUnit = _game.Mod.GetUnit(_aliens[i], false);
					if (ruleUnit != null)
					{
						var ruleCorpse = ruleUnit.Armor.CorpseGeoscape;
						if (ruleCorpse != null && ruleCorpse.IsRecoverable && ruleCorpse.IsCorpseRecoverable)
						{
							_base.StorageItems.AddItem(ruleCorpse, _qtys[i]);
						}
					}
				}
			}
		}
		_game.PopState();

		if (Options.storageLimitsEnforced && _base.StoresOverfull())
		{
			if (_origin == OptionsOrigin.OPT_BATTLESCAPE)
			{
				// not used anymore, because multiple prison types could spam this screen; it will pop up in Geoscape anyway
			}
			else
			{
				_game.PushState(new SellState(_base, null, _origin));
				_game.PushState(new ErrorMessageState(Tr("STR_STORAGE_EXCEEDED").Arg(_base.GetName(null)), _palette, unchecked((byte)_game.Mod.GetInterface("manageContainment").GetElement("errorMessage").color), "BACK13.SCR", _game.Mod.GetInterface("manageContainment").GetElement("errorPalette").color));
			}
		}
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Opens the Transfer UI and gives the player an option to transfer stuff instead of selling it.
	/// Returns back to this screen when finished.
	/// </summary>
	public void BtnTransferClick(Action action)
	{
		_game.PushState(new TransferBaseState(_base, null));
	}

	/// <summary>
	/// Cancels all prisoner interrogations. Cancels all incoming prisoner transfers.
	/// Allows the player to sell interrogated prisoners in case the prisons were destroyed (e.g. during a base defense).
	/// Reloads the screen when finished.
	/// </summary>
	public void BtnCleanupClick(Action action)
	{
		// cleanup
		_base.CleanupPrisons(_prisonType);

		// reset
		ResetListAndTotals();
	}

	/// <summary>
	/// Starts increasing the alien count.
	/// </summary>
	public void LstItemsRightArrowPress(Action action)
	{
		_sel = (int)_lstAliens.SelectedRow;
		if (_game.IsLeftClick(action, true) && !_timerInc.IsRunning) _timerInc.Start();
	}

	/// <summary>
	/// Stops increasing the alien count.
	/// </summary>
	public void LstItemsRightArrowRelease(Action action)
	{
		if (_game.IsLeftClick(action, true))
		{
			_timerInc.Stop();
		}
	}

	/// <summary>
	/// Increases the selected alien count;
	/// by one on left-click, to max on right-click.
	/// </summary>
	public void LstItemsRightArrowClick(Action action)
	{
		if (_game.IsRightClick(action, true)) IncreaseByValue(int.MaxValue);
		if (_game.IsLeftClick(action, true))
		{
			IncreaseByValue(_game.ScrollStep);
			_timerInc.SetInterval(250);
			_timerDec.SetInterval(250);
		}
	}

	/// <summary>
	/// Starts decreasing the alien count.
	/// </summary>
	public void LstItemsLeftArrowPress(Action action)
	{
		_sel = (int)_lstAliens.SelectedRow;
		if (_game.IsLeftClick(action, true) && !_timerDec.IsRunning) _timerDec.Start();
	}

	/// <summary>
	/// Stops decreasing the alien count.
	/// </summary>
	public void LstItemsLeftArrowRelease(Action action)
	{
		if (_game.IsLeftClick(action, true))
		{
			_timerDec.Stop();
		}
	}

	/// <summary>
	/// Decreases the selected alien count;
	/// by one on left-click, to 0 on right-click.
	/// </summary>
	public void LstItemsLeftArrowClick(Action action)
	{
		if (_game.IsRightClick(action, true)) DecreaseByValue(int.MaxValue);
		if (_game.IsLeftClick(action, true))
		{
			int change = _game.ScrollStep;
			if (_game.IsCtrlPressed())
			{
				change *= 10;
			}
			if (_game.IsShiftPressed())
			{
				change *= 10;
			}
			DecreaseByValue(change);
			_timerInc.SetInterval(250);
			_timerDec.SetInterval(250);
		}
	}

	/// <summary>
	/// Handles the mouse-wheels on the arrow-buttons.
	/// </summary>
	public void LstItemsMousePress(Action action)
	{
		_sel = (int)_lstAliens.SelectedRow;
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELUP)
		{
			_timerInc.Stop();
			_timerDec.Stop();
			if (action.AbsoluteXMouse >= _lstAliens.ArrowsLeftEdge &&
				action.AbsoluteXMouse <= _lstAliens.ArrowsRightEdge)
			{
				IncreaseByValue(Options.changeValueByMouseWheel);
			}
		}
		else if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELDOWN)
		{
			_timerInc.Stop();
			_timerDec.Stop();
			if (action.AbsoluteXMouse >= _lstAliens.ArrowsLeftEdge &&
				action.AbsoluteXMouse <= _lstAliens.ArrowsRightEdge)
			{
				DecreaseByValue(Options.changeValueByMouseWheel);
			}
		}
		else if (_game.IsMiddleClick(action, true))
		{
			RuleResearch selectedTopic = _game.Mod.GetResearch(_aliens[_sel]);
			if (selectedTopic != null)
			{
				_doNotReset = true;
				_game.PushState(new TechTreeViewerState(selectedTopic, null));
			}
		}
	}

	/// <summary>
	/// Gets the quantity of the currently selected alien on the base.
	/// </summary>
	/// <returns>Quantity of selected alien on the base.</returns>
	private int GetQuantity()
	{
		return _base.StorageItems.GetItem(_aliens[_sel]);
	}

	/// <summary>
	/// Increases the quantity of the selected alien to exterminate by one.
	/// </summary>
	public void Increase()
	{
		_timerDec.SetInterval(50);
		_timerInc.SetInterval(50);
		int change = _game.ScrollStep;
		if (_game.IsCtrlPressed())
		{
			change *= 10;
		}
		if (_game.IsShiftPressed())
		{
			change *= 10;
		}
		IncreaseByValue(change);
	}

	/// <summary>
	/// Increases the quantity of the selected alien to exterminate by "change".
	/// </summary>
	/// <param name="change">How much we want to add.</param>
	public void IncreaseByValue(int change)
	{
		int qty = GetQuantity() - _qtys[_sel];
		if (change <= 0 || qty <= 0) return;

		change = Math.Min(qty, change);
		_qtys[_sel] += change;
		_aliensSold += change;
		UpdateStrings();
	}

	/// <summary>
	/// Decreases the quantity of the selected alien to exterminate by one.
	/// </summary>
	public void Decrease()
	{
		_timerInc.SetInterval(50);
		_timerDec.SetInterval(50);
		int change = _game.ScrollStep;
		if (_game.IsCtrlPressed())
		{
			change *= 10;
		}
		if (_game.IsShiftPressed())
		{
			change *= 10;
		}
		DecreaseByValue(change);
	}

	/// <summary>
	/// Decreases the quantity of the selected alien to exterminate by "change".
	/// </summary>
	/// <param name="change">How much we want to remove.</param>
	public void DecreaseByValue(int change)
	{
		if (change <= 0 || _qtys[_sel] <= 0) return;
		change = Math.Min(_qtys[_sel], change);
		_qtys[_sel] -= change;
		_aliensSold -= change;
		UpdateStrings();
	}

	/// <summary>
	/// Updates the quantity-strings of the selected alien.
	/// </summary>
	public void UpdateStrings()
	{
		int qty = GetQuantity() - _qtys[_sel];
		string ss = qty.ToString(CultureInfo.InvariantCulture);
		string ss2 = _qtys[_sel].ToString(CultureInfo.InvariantCulture);

		_lstAliens.SetRowColor(_sel, (qty == 0) ? _lstAliens.SecondaryColor : _lstAliens.Color);
		_lstAliens.SetCellText(_sel, 2, ss);
		_lstAliens.SetCellText(_sel, 3, ss2);

		int aliens = _base.GetUsedContainment(_prisonType) - _aliensSold;
		int availableContainment = _base.GetAvailableContainment(_prisonType);
		int spaces = availableContainment - aliens;
		if (availableContainment == 0 || Options.storageLimitsEnforced)
		{
			_btnOk.Visible = spaces >= 0;
			_btnSell.Visible = spaces >= 0 && _threeButtons;
		}
		_txtAvailable.SetText(Tr("STR_SPACE_AVAILABLE").Arg(spaces));
		_txtUsed.SetText(Tr("STR_SPACE_USED").Arg(aliens));

		if (Options.canSellLiveAliens)
		{
			// we could probably keep track of _total with each change (only adding deltas), but I am lazy today
			_total = 0;
			for (int i = 0; i < _qtys.Count; ++i)
			{
				if (_qtys[i] > 0)
				{
					long adjustedCost = _game.Mod.GetItem(_aliens[i]).GetSellCostAdjusted(_base, _game.SavedGame);
					adjustedCost *= _qtys[i];
					_total += adjustedCost;
				}
			}
			_txtValueOfSales.SetText(Tr("STR_VALUE_OF_SALES").Arg(Unicode.FormatFunding(_total)));
		}
	}
}
