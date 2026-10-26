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
// Ported from: src/Basescape/ManufactureInfoState.h, src/Basescape/ManufactureInfoState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Screen that allows changing of Production settings (assigned engineer, units to build).
/// </summary>
public class ManufactureInfoState : State
{
	private readonly Base _base;
	private readonly RuleManufacture _item;
	private Production _production;
	private Window _window;
	private ArrowButton _btnUnitInfinity, _btnUnitMinimum;
	private ArrowButton _btnUnitUp, _btnUnitDown, _btnEngineerUp, _btnEngineerDown;
	private TextButton _btnStop, _btnOk;
	private Text _txtTitle, _txtAvailableEngineer, _txtAvailableSpace, _txtHoursPerUnit, _txtMonthlyProfit;
	private Text _txtAllocatedEngineer, _txtUnitToProduce, _txtUnitUp, _txtUnitDown, _txtEngineerUp, _txtEngineerDown, _txtAllocated, _txtTodo;
	private ToggleTextButton _btnSell;
	private ToggleTextButton _btnFallback;
	private Timer _timerMoreEngineer, _timerMoreUnit, _timerLessEngineer, _timerLessUnit;
	private InteractiveSurface _surfaceEngineers, _surfaceUnits;
	private int _producedItemsValue;

	/// <summary>
	/// Initializes all elements in the Production settings screen (new Production).
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="_item">The RuleManufacture to produce.</param>
#pragma warning disable CA1707 // parameter name as in the C++ header
	public ManufactureInfoState(Base @base, RuleManufacture _item)
#pragma warning restore CA1707
	{
		_base = @base;
		this._item = _item;
		_production = null;
		BuildUi();
	}

	/// <summary>
	/// Initializes all elements in the Production settings screen (modifying Production).
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="production">The Production to modify.</param>
	public ManufactureInfoState(Base @base, Production production)
	{
		_base = @base;
		_item = null;
		_production = production;
		BuildUi();
	}

	/// <summary>
	/// Builds screen User Interface.
	/// </summary>
	private void BuildUi()
	{
		_screen = false;

		_window = new Window(this, 320, 160, 0, 20, WindowPopup.POPUP_BOTH);
		_txtTitle = new Text(320, 17, 0, 30);
		_btnOk = new TextButton(136, 16, 168, 155);
		_btnStop = new TextButton(136, 16, 16, 155);
		_btnSell = new ToggleTextButton(60, 16, 244, 61);
		_btnFallback = new ToggleTextButton(72, 16, 168, 61);
		_txtAvailableEngineer = new Text(160, 9, 16, 50);
		_txtAvailableSpace = new Text(160, 9, 16, 60);
		_txtHoursPerUnit = new Text(150, 9, 16, 70);
		_txtMonthlyProfit = new Text(160, 9, 168, 50);
		_txtAllocatedEngineer = new Text(112, 32, 16, 80);
		_txtUnitToProduce = new Text(112, 48, 168, 64);
		_txtEngineerUp = new Text(90, 9, 40, 118);
		_txtEngineerDown = new Text(90, 9, 40, 138);
		_txtUnitUp = new Text(90, 9, 192, 118);
		_txtUnitDown = new Text(90, 9, 192, 138);
		_btnEngineerUp = new ArrowButton(ArrowShape.ARROW_BIG_UP, 13, 14, 132, 114);
		_btnEngineerDown = new ArrowButton(ArrowShape.ARROW_BIG_DOWN, 13, 14, 132, 136);
		_btnUnitInfinity = new ArrowButton(ArrowShape.ARROW_BIG_UP, 13, 14, 300, 114);
		_btnUnitMinimum = new ArrowButton(ArrowShape.ARROW_BIG_DOWN, 13, 14, 300, 136);
		_btnUnitUp = new ArrowButton(ArrowShape.ARROW_BIG_UP, 13, 14, 284, 114);
		_btnUnitDown = new ArrowButton(ArrowShape.ARROW_BIG_DOWN, 13, 14, 284, 136);
		_txtAllocated = new Text(40, 16, 128, 88);
		_txtTodo = new Text(40, 16, 280, 88);

		_surfaceEngineers = new InteractiveSurface(160, 150, 0, 25);
		_surfaceEngineers.OnMouseClick(HandleWheelEngineer, 0);

		_surfaceUnits = new InteractiveSurface(160, 150, 160, 25);
		_surfaceUnits.OnMouseClick(HandleWheelUnit, 0);

		// Set palette
		SetInterface("manufactureInfo");

		Add(_surfaceEngineers);
		Add(_surfaceUnits);
		Add(_window, "window", "manufactureInfo");
		Add(_txtTitle, "text", "manufactureInfo");
		Add(_txtAvailableEngineer, "text", "manufactureInfo");
		Add(_txtAvailableSpace, "text", "manufactureInfo");
		Add(_txtHoursPerUnit, "text", "manufactureInfo");
		Add(_txtMonthlyProfit, "text", "manufactureInfo");
		Add(_txtAllocatedEngineer, "text", "manufactureInfo");
		Add(_txtAllocated, "text", "manufactureInfo");
		Add(_txtUnitToProduce, "text", "manufactureInfo");
		Add(_txtTodo, "text", "manufactureInfo");
		Add(_txtEngineerUp, "text", "manufactureInfo");
		Add(_txtEngineerDown, "text", "manufactureInfo");
		Add(_btnEngineerUp, "button1", "manufactureInfo");
		Add(_btnEngineerDown, "button1", "manufactureInfo");
		Add(_txtUnitUp, "text", "manufactureInfo");
		Add(_txtUnitDown, "text", "manufactureInfo");
		Add(_btnUnitInfinity, "button1", "manufactureInfo");
		Add(_btnUnitMinimum, "button1", "manufactureInfo");
		Add(_btnUnitUp, "button1", "manufactureInfo");
		Add(_btnUnitDown, "button1", "manufactureInfo");
		Add(_btnOk, "button2", "manufactureInfo");
		Add(_btnStop, "button2", "manufactureInfo");
		Add(_btnSell, "button1", "manufactureInfo");
		Add(_btnFallback, "button1", "manufactureInfo");

		CenterAllSurfaces();

		SetWindowBackground(_window, "manufactureInfo");

		_txtTitle.SetText(Tr(_item != null ? _item.Name : _production.Rules.Name));
		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;

		_txtAllocatedEngineer.SetText(Tr("STR_ENGINEERS__ALLOCATED"));
		_txtAllocatedEngineer.SetBig();
		_txtAllocatedEngineer.SetWordWrap(true);
		_txtAllocatedEngineer.VerticalAlign = TextVAlign.ALIGN_BOTTOM;

		_txtAllocated.SetBig();

		_txtTodo.SetBig();

		_txtUnitToProduce.SetText(Tr("STR_UNITS_TO_PRODUCE"));
		_txtUnitToProduce.SetBig();
		_txtUnitToProduce.SetWordWrap(true);
		_txtUnitToProduce.VerticalAlign = TextVAlign.ALIGN_BOTTOM;

		_txtEngineerUp.SetText(Tr("STR_INCREASE_UC"));

		_txtEngineerDown.SetText(Tr("STR_DECREASE_UC"));

		_btnEngineerUp.OnMousePress(MoreEngineerPress);
		_btnEngineerUp.OnMouseRelease(MoreEngineerRelease);
		_btnEngineerUp.OnMouseClick(MoreEngineerClick, 0);

		_btnEngineerDown.OnMousePress(LessEngineerPress);
		_btnEngineerDown.OnMouseRelease(LessEngineerRelease);
		_btnEngineerDown.OnMouseClick(LessEngineerClick, 0);

		_btnUnitUp.OnMousePress(MoreUnitPress);
		_btnUnitUp.OnMouseRelease(MoreUnitRelease);
		_btnUnitUp.OnMouseClick(MoreUnitClick, 0);

		_btnUnitDown.OnMousePress(LessUnitPress);
		_btnUnitDown.OnMouseRelease(LessUnitRelease);
		_btnUnitDown.OnMouseClick(LessUnitClick, 0);

		_txtUnitUp.SetText(Tr("STR_INCREASE_UC"));

		_txtUnitDown.SetText(Tr("STR_DECREASE_UC"));

		_btnSell.Text = Tr("STR_SELL_PRODUCTION");
		_btnSell.OnMouseClick(BtnSellClick, 0);

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		if (_item == null && _production != null)
		{
			if (_production.Rules.Refund)
			{
				_btnStop.Text = Tr("STR_REFUND_PRODUCTION");
			}
			else
			{
				_btnStop.Text = Tr("STR_STOP_PRODUCTION");
			}
		}
		else
		{
			_btnStop.Text = Tr("STR_CANCEL_UC");
		}
		_btnStop.OnMouseClick(BtnStopClick);
		if (_production == null)
		{
			_production = new Production(_item, 1);
			_base.AddProduction(_production);
		}
		_btnSell.Pressed = _production.SellItems;
		_btnSell.Visible = _production.Rules.CanAutoSell();
		InitProfitInfo();
		SetAssignedEngineer();

		_btnFallback.Text = Tr("STR_FALLBACK_PRODUCTION");
		_btnFallback.Pressed = _production.IsFallback;
		_btnFallback.Visible = Options.oxceBaseManufactureFallbackButton;

		if (Options.oxceBaseManufactureInfinityButton)
		{
			if (_production.Rules.ProducedCraft != null)
			{
				_btnUnitInfinity.Visible = false;
				_btnUnitMinimum.Visible = false;
			}
			else
			{
				_btnUnitInfinity.OnMouseClick(InfinityUnitClick, 0);
				_btnUnitMinimum.OnMouseClick(MinimumUnitClick, 0);
			}
		}
		else
		{
			_btnUnitInfinity.Visible = false;
			_btnUnitMinimum.Visible = false;
		}

		_txtHoursPerUnit.SetText(Tr("STR_HOURS_PER_UNIT").Arg(_production.Rules.ManufactureTime));

		_timerMoreEngineer = new Timer(250);
		_timerLessEngineer = new Timer(250);
		_timerMoreUnit = new Timer(250);
		_timerLessUnit = new Timer(250);
		_timerMoreEngineer.OnTimer((StateHandler)OnMoreEngineer);
		_timerLessEngineer.OnTimer((StateHandler)OnLessEngineer);
		_timerMoreUnit.OnTimer((StateHandler)OnMoreUnit);
		_timerLessUnit.OnTimer((StateHandler)OnLessUnit);
	}

	/// <summary>
	/// Caches static data for monthly profit calculations
	/// </summary>
	private void InitProfitInfo()
	{
		RuleManufacture manuf = _production.Rules;

		_producedItemsValue = 0;
		var ruleCraft = manuf.ProducedCraft;
		if (ruleCraft != null)
		{
			_producedItemsValue += ruleCraft.SellCost;
		}
		else
		{
			foreach (var pair in manuf.ProducedItems)
			{
				long adjustedSellValue = pair.Key.GetSellCostAdjusted(_base, _game.SavedGame);
				adjustedSellValue *= pair.Value;
				_producedItemsValue = unchecked((int)(_producedItemsValue + adjustedSellValue));
			}
		}
	}

	/// <summary>
	/// Calculates the monthly change in funds due to the job.
	/// </summary>
	/// <remarks>
	/// note that this function calculates only the change in funds, not the change
	/// in net worth.  after discussion in the forums, it was decided that focusing
	/// only on visible changes in funds was clearer and more valuable to the player
	/// than trying to take used materials and maintenance costs into account.
	/// </remarks>
	private int GetMonthlyNetFunds()
	{
		return CalcMonthlyNetFunds(_production.Rules, _btnSell.Pressed ? _producedItemsValue : 0, _production.AssignedEngineers,
			_production.InfiniteAmount, _production.AmountTotal, _production.AmountProduced);
	}

	/// <summary>
	/// The calculation of getMonthlyNetFunds.
	/// </summary>
	internal static int CalcMonthlyNetFunds(RuleManufacture item, int saleValue, int numEngineers, bool infiniteAmount, int amountTotal, int amountProduced)
	{
		// does not take into account leap years, but a game is unlikely to take long enough for that to matter
		const int AVG_HOURS_PER_MONTH = (365 * 24) / 12;

		int manHoursPerMonth = AVG_HOURS_PER_MONTH * numEngineers;
		if (!infiniteAmount)
		{
			// scale down to actual number of man hours required if the job will
			// take less than one month
			int manHoursRemaining = item.ManufactureTime * (amountTotal - amountProduced);
			manHoursPerMonth = Math.Min(manHoursPerMonth, manHoursRemaining);
		}
		float itemsPerMonth = (float)manHoursPerMonth / (float)item.ManufactureTime;

		return (int)((saleValue - item.ManufactureCost) * itemsPerMonth);
	}

	/// <summary>
	/// Refreshes profit values.
	/// </summary>
	private void BtnSellClick(Action action)
	{
		SetAssignedEngineer();
	}

	/// <summary>
	/// Stops this Production. Returns to the previous screen.
	/// </summary>
	private void BtnStopClick(Action action)
	{
		if (_item == null && _production != null && _production.Rules.Refund)
		{
			_production.RefundItem(_base, _game.SavedGame, _game.Mod);
		}
		_base.RemoveProduction(_production);
		ExitState();
	}

	/// <summary>
	/// Starts this Production (if new). Returns to the previous screen.
	/// </summary>
	private void BtnOkClick(Action action)
	{
		if (_item != null)
		{
			_production.StartItem(_base, _game.SavedGame, _game.Mod);
		}
		_production.SellItems = _btnSell.Pressed;
		if (_btnFallback.Pressed)
		{
			foreach (var p in _base.Productions)
			{
				p.SetFallback(false);
			}
			_production.SetFallback(true);
		}
		ExitState();
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	private void ExitState()
	{
		_game.PopState();
		if (_item != null)
		{
			_game.PopState();
		}
	}

	/// <summary>
	/// Updates display of assigned/available engineer/workshop space.
	/// </summary>
	private void SetAssignedEngineer()
	{
		_txtAvailableEngineer.SetText(Tr("STR_ENGINEERS_AVAILABLE_UC").Arg(_base.AvailableEngineers));
		_txtAvailableSpace.SetText(Tr("STR_WORKSHOP_SPACE_AVAILABLE_UC").Arg(_base.FreeWorkshops));
		string s3 = ">" + Unicode.TOK_COLOR_FLIP + _production.AssignedEngineers.ToString(CultureInfo.InvariantCulture);
		_txtAllocated.SetText(s3);
		string s4 = ">" + Unicode.TOK_COLOR_FLIP;
		if (_production.InfiniteAmount) s4 += "∞";
		else s4 += _production.AmountTotal.ToString(CultureInfo.InvariantCulture);
		_txtTodo.SetText(s4);
		_txtMonthlyProfit.SetText(Tr("STR_MONTHLY_PROFIT").Arg(Unicode.FormatFunding(GetMonthlyNetFunds())));
	}

	/// <summary>
	/// Adds given number of engineers to the project if possible.
	/// </summary>
	/// <param name="change">How much we want to add.</param>
	private void MoreEngineer(int change)
	{
		if (change <= 0) return;
		int availableEngineer = _base.AvailableEngineers;
		int availableWorkSpace = _base.FreeWorkshops;
		int timeLeft = _production.AmountTotal * _production.Rules.ManufactureTime - _production.TimeSpent;
		if (_production.InfiniteAmount)
			timeLeft = int.MaxValue;

		if (_production.IsQueuedOnly)
		{
			// start counting the workshop space now
			availableWorkSpace -= _production.Rules.RequiredSpace;
		}
		if (availableEngineer > 0 && availableWorkSpace > 0 && timeLeft > _production.AssignedEngineers)
		{
			change = Math.Min(Math.Min(Math.Min(availableEngineer, availableWorkSpace), change), timeLeft - _production.AssignedEngineers);

			_production.AssignedEngineers = _production.AssignedEngineers + change;
			_base.Engineers = _base.Engineers - change;
			SetAssignedEngineer();
		}
		else if (availableWorkSpace <= 0 && availableEngineer > 0 && _production.IsQueuedOnly && _production.Rules.RequiredSpace > 0)
		{
			_timerMoreEngineer.Stop();
			_game.PushState(new ErrorMessageState(
				Tr("STR_NOT_ENOUGH_WORK_SPACE"),
				_palette,
				unchecked((byte)_game.Mod.GetInterface("basescape").GetElement("errorMessage").color),
				"BACK17.SCR",
				_game.Mod.GetInterface("basescape").GetElement("errorPalette").color)
			);
		}
	}

	/// <summary>
	/// Starts the timerMoreEngineer.
	/// </summary>
	private void MoreEngineerPress(Action action)
	{
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT) _timerMoreEngineer.Start();
	}

	/// <summary>
	/// Stops the timerMoreEngineer.
	/// </summary>
	private void MoreEngineerRelease(Action action)
	{
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
		{
			_timerMoreEngineer.SetInterval(250);
			_timerMoreEngineer.Stop();
		}
	}

	/// <summary>
	/// Allocates all engineers.
	/// </summary>
	private void MoreEngineerClick(Action action)
	{
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT) MoreEngineer(int.MaxValue);
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT) MoreEngineer(1);
	}

	/// <summary>
	/// Removes the given number of engineers from the project if possible.
	/// </summary>
	/// <param name="change">How much we want to subtract.</param>
	private void LessEngineer(int change)
	{
		if (change <= 0) return;
		int assigned = _production.AssignedEngineers;
		if (assigned > 0)
		{
			change = Math.Min(assigned, change);
			_production.AssignedEngineers = assigned - change;
			_base.Engineers = _base.Engineers + change;
			SetAssignedEngineer();
		}
	}

	/// <summary>
	/// Starts the timerLessEngineer.
	/// </summary>
	private void LessEngineerPress(Action action)
	{
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT) _timerLessEngineer.Start();
	}

	/// <summary>
	/// Stops the timerLessEngineer.
	/// </summary>
	private void LessEngineerRelease(Action action)
	{
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
		{
			_timerLessEngineer.SetInterval(250);
			_timerLessEngineer.Stop();
		}
	}

	/// <summary>
	/// Removes engineers from the production.
	/// </summary>
	private void LessEngineerClick(Action action)
	{
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT) LessEngineer(int.MaxValue);
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT) LessEngineer(1);
	}

	/// <summary>
	/// Adds given number of units to produce to the project if possible.
	/// </summary>
	/// <param name="change">How much we want to add.</param>
	private void MoreUnit(int change)
	{
		if (change <= 0) return;
		if (_production.Rules.ProducedCraft != null && _base.AvailableHangars - _base.UsedHangars <= 0)
		{
			_timerMoreUnit.Stop();
			_game.PushState(new ErrorMessageState(Tr("STR_NO_FREE_HANGARS_FOR_CRAFT_PRODUCTION"), _palette, unchecked((byte)_game.Mod.GetInterface("basescape").GetElement("errorMessage").color), "BACK17.SCR", _game.Mod.GetInterface("basescape").GetElement("errorPalette").color));
		}
		else
		{
			int units = _production.AmountTotal;
			if (units == 1 && change > 1)
			{
				--change; // e.g. jump from 1 to 10, not to 11
			}
			change = Math.Min(int.MaxValue - units, change);
			if (_production.Rules.ProducedCraft != null)
				change = Math.Min(_base.AvailableHangars - _base.UsedHangars, change);
			_production.AmountTotal = units + change;
			SetAssignedEngineer();
		}
	}

	/// <summary>
	/// Starts the timerMoreUnit.
	/// </summary>
	private void MoreUnitPress(Action action)
	{
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT && _production.AmountTotal < int.MaxValue)
			_timerMoreUnit.Start();
	}

	/// <summary>
	/// Stops the timerMoreUnit.
	/// </summary>
	private void MoreUnitRelease(Action action)
	{
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
		{
			_timerMoreUnit.SetInterval(250);
			_timerMoreUnit.Stop();
		}
	}

	/// <summary>
	/// Increases the "units to produce", in the case of a right-click, to infinite, and 1 on left-click.
	/// </summary>
	private void MoreUnitClick(Action action)
	{
		if (_production.InfiniteAmount) return; // We can't increase over infinite :)
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT)
		{
			if (_production.Rules.ProducedCraft != null)
			{
				MoreUnit(int.MaxValue);
			}
			else
			{
				_production.InfiniteAmount = true;
				SetAssignedEngineer();
			}
		}
		else if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
		{
			MoreUnit(1);
		}
	}

	/// <summary>
	/// Increases the "units to produce" to infinite ("right-click button" on mobile platforms).
	/// </summary>
	private void InfinityUnitClick(Action action)
	{
		if (_production.InfiniteAmount) return; // We can't increase over infinite :)

		if (_production.Rules.ProducedCraft != null)
		{
			// nothing
		}
		else
		{
			_production.InfiniteAmount = true;
			SetAssignedEngineer();
		}
	}

	/// <summary>
	/// Removes the given number of units to produce from the project if possible.
	/// </summary>
	/// <param name="change">How much we want to subtract.</param>
	private void LessUnit(int change)
	{
		if (change <= 0) return;
		int units = _production.AmountTotal;
		change = Math.Min(units - (_production.AmountProduced + 1), change);
		_production.AmountTotal = units - change;
		SetAssignedEngineer();
	}

	/// <summary>
	/// Starts the timerLessUnit.
	/// </summary>
	private void LessUnitPress(Action action)
	{
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT) _timerLessUnit.Start();
	}

	/// <summary>
	/// Stops the timerLessUnit.
	/// </summary>
	private void LessUnitRelease(Action action)
	{
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
		{
			_timerLessUnit.SetInterval(250);
			_timerLessUnit.Stop();
		}
	}

	/// <summary>
	/// Decreases the units to produce.
	/// </summary>
	private void LessUnitClick(Action action)
	{
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT
		|| action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
		{
			bool wasInfinite = _production.InfiniteAmount;
			_production.InfiniteAmount = false;
			if (action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT
			|| _production.AmountTotal <= _production.AmountProduced)
			{ // So the produced item number is increased over the planned, OR it was simply a right-click
				_production.AmountTotal = _production.AmountProduced + 1;
				SetAssignedEngineer();
			}
			if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
			{
				if (wasInfinite)
				{
					// when infinite amount is decreased by 1, set the amount to maximum possible (capped at 999) considering current funds and store supplies
					int productionPossible = 999;
					var manufRule = _production.Rules;
					if (manufRule.ManufactureCost > 0)
					{
						long byFunds = _game.SavedGame.Funds / manufRule.ManufactureCost;
						if (byFunds < 1000L)
						{
							int byFundsInt = (int)byFunds;
							productionPossible = Math.Min(productionPossible, byFundsInt);
						}
					}
					foreach (var item in manufRule.RequiredItems)
					{
						productionPossible = Math.Min(productionPossible, _base.StorageItems.GetItem(item.Key) / item.Value);
					}
					productionPossible = Math.Max(0, productionPossible);

					int newTotal = _production.AmountProduced + productionPossible;
					if (_item == null)
					{
						newTotal += 1; // +1 for the item being produced currently
					}
					_production.AmountTotal = newTotal + 1; // +1 because of lessUnit(1) call below
				}
				LessUnit(1);
			}
		}
	}

	/// <summary>
	/// Decreases the units to produce ("right-click button" on mobile platforms).
	/// </summary>
	private void MinimumUnitClick(Action action)
	{
		_production.InfiniteAmount = false;
		_production.AmountTotal = _production.AmountProduced + 1;
		SetAssignedEngineer();
	}

	/// <summary>
	/// Assigns one more engineer (if possible).
	/// </summary>
	private void OnMoreEngineer()
	{
		_timerMoreEngineer.SetInterval(50);
		MoreEngineer(1);
	}

	/// <summary>
	/// Removes one engineer (if possible).
	/// </summary>
	private void OnLessEngineer()
	{
		_timerLessEngineer.SetInterval(50);
		LessEngineer(1);
	}

	/// <summary>
	/// Increases or decreases the Engineers according the mouse-wheel used.
	/// </summary>
	private void HandleWheelEngineer(Action action)
	{
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELUP) MoreEngineer(Options.changeValueByMouseWheel);
		else if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELDOWN) LessEngineer(Options.changeValueByMouseWheel);
	}

	/// <summary>
	/// Builds one more unit.
	/// </summary>
	private void OnMoreUnit()
	{
		_timerMoreUnit.SetInterval(50);
		MoreUnit(1);
	}

	/// <summary>
	/// Builds one less unit( if possible).
	/// </summary>
	private void OnLessUnit()
	{
		_timerLessUnit.SetInterval(50);
		LessUnit(1);
	}

	/// <summary>
	/// Increases or decreases the Units to produce according the mouse-wheel used.
	/// </summary>
	private void HandleWheelUnit(Action action)
	{
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELUP) MoreUnit(Options.changeValueByMouseWheel);
		else if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELDOWN) LessUnit(Options.changeValueByMouseWheel);
	}

	/// <summary>
	/// Runs state functionality every cycle (used to update the timer).
	/// </summary>
	public override void Think()
	{
		base.Think();
		_timerMoreEngineer.Think(this, null);
		_timerLessEngineer.Think(this, null);
		_timerMoreUnit.Think(this, null);
		_timerLessUnit.Think(this, null);
	}
}
