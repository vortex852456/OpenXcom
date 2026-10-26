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
// Ported from: src/Basescape/TransferItemsState.h, src/Basescape/TransferItemsState.cpp
#nullable disable

using System.Globalization;
using static Acom.SDLKey;
using static Acom.TransferType;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Transfer screen that lets the player pick
/// what items to transfer between bases.
/// </summary>
public class TransferItemsState : TouchState
{
	private readonly Base _baseFrom, _baseTo;
	private readonly DebriefingState _debriefingState;
	private readonly TextButton _btnOk, _btnCancel;
	private readonly TextEdit _btnQuickSearch;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtQuantity, _txtAmountTransfer, _txtAmountDestination;
	private readonly ComboBox _cbxCategory;
	private readonly TextList _lstItems;
	private readonly List<TransferRow> _items = [];
	private readonly List<int> _rows = [];
	private readonly List<string> _cats = [];
	private readonly int _vanillaCategories;
	private int _sel;
	private int _total, _pQty, _cQty, _aQty;
	private double _iQty;
	private readonly double _distance;
	private readonly byte _ammoColor;
	private readonly Timer _timerInc, _timerDec;
	private TransferSortDirection _previousSort, _currentSort;
	private bool _errorShown;

	/// <summary>Gets the row of the current selection.</summary>
	private TransferRow GetRow() => _items[_rows[_sel]];

	private static string Str(int value) => value.ToString(CultureInfo.InvariantCulture);

	/// <summary>
	/// Initializes all the elements in the Transfer screen.
	/// </summary>
	/// <param name="baseFrom">Pointer to the source base.</param>
	/// <param name="baseTo">Pointer to the destination base.</param>
	/// <param name="debriefingState">Pointer to the debriefing state (transfer recovered items), or null.</param>
	public TransferItemsState(Base baseFrom, Base baseTo, DebriefingState debriefingState)
	{
		_baseFrom = baseFrom;
		_baseTo = baseTo;
		_debriefingState = debriefingState;
		_sel = 0;
		_total = 0;
		_pQty = 0;
		_cQty = 0;
		_aQty = 0;
		_iQty = 0.0;
		_distance = 0.0;
		_ammoColor = 0;
		_previousSort = TransferSortDirection.BY_LIST_ORDER;
		_currentSort = TransferSortDirection.BY_LIST_ORDER;
		_errorShown = false;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnQuickSearch = new TextEdit(this, 48, 9, 10, 13);
		_btnOk = new TextButton(148, 16, 8, 176);
		_btnCancel = new TextButton(148, 16, 164, 176);
		_txtTitle = new Text(310, 17, 5, 8);
		_txtQuantity = new Text(50, 9, 150, 24);
		_txtAmountTransfer = new Text(60, 17, 200, 24);
		_txtAmountDestination = new Text(60, 17, 260, 24);
		_cbxCategory = new ComboBox(this, 120, 16, 10, 24);
		_lstItems = new TextList(287, 128, 8, 44);

		TouchComponentsCreate(_txtTitle);

		// Set palette
		SetInterface("transferMenu");

		_ammoColor = (byte)_game.Mod.GetInterface("transferMenu").GetElement("ammoColor").color;

		Add(_window, "window", "transferMenu");
		Add(_btnQuickSearch, "button", "transferMenu");
		Add(_btnOk, "button", "transferMenu");
		Add(_btnCancel, "button", "transferMenu");
		Add(_txtTitle, "text", "transferMenu");
		Add(_txtQuantity, "text", "transferMenu");
		Add(_txtAmountTransfer, "text", "transferMenu");
		Add(_txtAmountDestination, "text", "transferMenu");
		Add(_lstItems, "list", "transferMenu");
		Add(_cbxCategory, "text", "transferMenu");

		TouchComponentsAdd("button2", "transferMenu", _window);

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "transferMenu");

		TouchComponentsConfigure();

		_btnOk.Text = Tr("STR_TRANSFER");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);

		_btnCancel.Text = Tr("STR_CANCEL");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_TRANSFER"));

		_txtQuantity.SetText(Tr("STR_QUANTITY_UC"));

		_txtAmountTransfer.SetText(Tr("STR_AMOUNT_TO_TRANSFER"));
		_txtAmountTransfer.SetWordWrap(true);

		_txtAmountDestination.SetText(Tr("STR_AMOUNT_AT_DESTINATION"));
		_txtAmountDestination.SetWordWrap(true);

		_lstItems.SetArrowColumn(193, ArrowOrientation.ARROW_VERTICAL);
		_lstItems.SetColumns(4, 162, 58, 40, 27);
		_lstItems.SetSelectable(true);
		_lstItems.SetBackground(_window);
		_lstItems.Margin = 2;
		_lstItems.OnLeftArrowPress(LstItemsLeftArrowPress);
		_lstItems.OnLeftArrowRelease(LstItemsLeftArrowRelease);
		_lstItems.OnLeftArrowClick(LstItemsLeftArrowClick);
		_lstItems.OnRightArrowPress(LstItemsRightArrowPress);
		_lstItems.OnRightArrowRelease(LstItemsRightArrowRelease);
		_lstItems.OnRightArrowClick(LstItemsRightArrowClick);
		_lstItems.OnMousePress(LstItemsMousePress);

		_distance = GetDistance(_baseFrom, _baseTo);

		_cats.Add("STR_ALL_ITEMS");
		_cats.Add("STR_ITEMS_AT_DESTINATION");
		if (Options.oxceBaseFilterResearchable)
		{
			_cats.Add("STR_FILTER_RESEARCHED");
			_cats.Add("STR_FILTER_RESEARCHABLE");
		}

		foreach (var soldier in _baseFrom.Soldiers)
		{
			if (_debriefingState != null) break;
			if (soldier.Craft == null)
			{
				AddRow(new TransferRow { type = TRANSFER_SOLDIER, rule = soldier, name = soldier.GetName(true), cost = (int)(5 * _distance), qtySrc = 1, listOrder = -4, totalCost = (int)(5 * _distance) });
			}
		}
		foreach (var craft in _baseFrom.Crafts)
		{
			if (_debriefingState != null) break;
			if (craft.Status != "STR_OUT" || (Options.canTransferCraftsWhileAirborne && craft.Fuel >= craft.GetFuelLimit(_baseTo)))
			{
				AddRow(new TransferRow { type = TRANSFER_CRAFT, rule = craft, name = craft.GetName(_game.Language), cost = (int)(25 * _distance), qtySrc = 1, listOrder = -3, totalCost = (int)(25 * _distance) });
			}
		}
		if (_baseFrom.AvailableScientists > 0 && _debriefingState == null)
		{
			AddRow(new TransferRow { type = TRANSFER_SCIENTIST, rule = null, name = Tr("STR_SCIENTIST"), cost = (int)(5 * _distance), qtySrc = _baseFrom.AvailableScientists, qtyDst = _baseTo.AvailableScientists, listOrder = -2, totalCost = _baseFrom.AvailableScientists * (int)(5 * _distance) });
		}
		if (_baseFrom.AvailableEngineers > 0 && _debriefingState == null)
		{
			AddRow(new TransferRow { type = TRANSFER_ENGINEER, rule = null, name = Tr("STR_ENGINEER"), cost = (int)(5 * _distance), qtySrc = _baseFrom.AvailableEngineers, qtyDst = _baseTo.AvailableEngineers, listOrder = -1, totalCost = _baseFrom.AvailableEngineers * (int)(5 * _distance) });
		}
		foreach (var itemType in _game.Mod.ItemsList)
		{
			RuleItem rule = _game.Mod.GetItem(itemType, true);
			int qty = _baseFrom.StorageItems.GetItem(rule);
			if (_debriefingState != null)
			{
				qty = _debriefingState.GetRecoveredItemCount(rule);
			}
			if (qty > 0)
			{
				AddRow(new TransferRow { type = TRANSFER_ITEM, rule = rule, name = Tr(itemType), cost = (int)(1 * _distance), qtySrc = qty, qtyDst = _baseTo.StorageItems.GetItem(rule), listOrder = rule.ListOrder, size = rule.Size, totalSize = qty * rule.Size, totalCost = qty * (int)(1 * _distance) });
			}
		}

		_vanillaCategories = _cats.Count;
		if (_game.Mod.DisplayCustomCategories > 0)
		{
			bool hasUnassigned = false;

			// first find all relevant item categories
			List<string> tempCats = [];
			foreach (var transferRow in _items)
			{
				if (transferRow.type == TRANSFER_ITEM)
				{
					RuleItem rule = (RuleItem)transferRow.rule;
					if (rule.Categories.Count == 0)
					{
						hasUnassigned = true;
					}
					foreach (var itemCategoryName in rule.Categories)
					{
						if (!tempCats.Contains(itemCategoryName))
						{
							tempCats.Add(itemCategoryName);
						}
					}
				}
			}
			// then use them nicely in order
			if (_game.Mod.DisplayCustomCategories == 1)
			{
				_cats.Clear();
				_cats.Add("STR_ALL_ITEMS");
				_cats.Add("STR_ITEMS_AT_DESTINATION");
				if (Options.oxceBaseFilterResearchable)
				{
					_cats.Add("STR_FILTER_RESEARCHED");
					_cats.Add("STR_FILTER_RESEARCHABLE");
				}
				_vanillaCategories = _cats.Count;
			}
			foreach (var categoryName in _game.Mod.ItemCategoriesList)
			{
				if (tempCats.Contains(categoryName))
				{
					_cats.Add(categoryName);
				}
			}
			if (hasUnassigned)
			{
				_cats.Add("STR_UNASSIGNED");
			}
		}

		_cbxCategory.SetOptions(_cats, true);
		_cbxCategory.OnChange(CbxCategoryChange);
		_cbxCategory.OnKeyboardPress(BtnTransferAllClick, Options.keyTransferAll);

		_btnQuickSearch.Text = ""; // redraw
		_btnQuickSearch.OnChange(BtnQuickSearchApply);
		_btnQuickSearch.Visible = Options.oxceQuickSearchButton;

		_btnOk.OnKeyboardRelease(BtnQuickSearchToggle, Options.keyToggleQuickSearch);

		UpdateList();

		_timerInc = new Timer(250);
		_timerInc.OnTimer((StateHandler)Increase);
		_timerDec = new Timer(250);
		_timerDec.OnTimer((StateHandler)Decrease);
	}

	/// <summary>Adds a row to the item list and registers its category.</summary>
	private void AddRow(TransferRow row)
	{
		_items.Add(row);
		string cat = GetCategory(_items.Count - 1);
		if (!_cats.Contains(cat))
		{
			_cats.Add(cat);
		}
	}

	/// <summary>
	/// Resets stuff when coming back from other screens.
	/// </summary>
	public override void Init()
	{
		base.Init();

		TouchComponentsRefresh();
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
	/// Determines the category a row item belongs in.
	/// </summary>
	private string GetCategory(int sel)
	{
		RuleItem rule;
		switch (_items[sel].type)
		{
			case TRANSFER_SOLDIER:
			case TRANSFER_SCIENTIST:
			case TRANSFER_ENGINEER:
				return "STR_PERSONNEL";
			case TRANSFER_CRAFT:
				return "STR_CRAFT_ARMAMENT";
			case TRANSFER_ITEM:
				rule = (RuleItem)_items[sel].rule;
				if (rule.BattleType == BattleType.BT_CORPSE || rule.IsAlien)
				{
					if (rule.VehicleUnit != null)
						return "STR_PERSONNEL"; // OXCE: critters fighting for us
					if (rule.IsAlien)
						return "STR_PRISONERS"; // OXCE: live aliens
					return "STR_ALIENS";
				}
				if (rule.BattleType == BattleType.BT_NONE)
				{
					if (_game.Mod.IsCraftWeaponStorageItem(rule))
						return "STR_CRAFT_ARMAMENT";
					if (_game.Mod.IsArmorStorageItem(rule))
						return "STR_ARMORS"; // OXCE: armors
					return "STR_COMPONENTS";
				}
				return "STR_EQUIPMENT";
		}
		return "STR_ALL_ITEMS";
	}

	/// <summary>
	/// Determines if a row item belongs to a given category.
	/// </summary>
	private bool BelongsToCategory(int sel, string cat)
	{
		switch (_items[sel].type)
		{
			case TRANSFER_SOLDIER:
			case TRANSFER_SCIENTIST:
			case TRANSFER_ENGINEER:
			case TRANSFER_CRAFT:
				return false;
			case TRANSFER_ITEM:
				RuleItem rule = (RuleItem)_items[sel].rule;
				return rule.BelongsToCategory(cat);
		}
		return false;
	}

	/// <summary>
	/// Quick search toggle.
	/// </summary>
	public void BtnQuickSearchToggle(Action action)
	{
		if (_btnQuickSearch.Visible)
		{
			_btnQuickSearch.Text = "";
			_btnQuickSearch.Visible = false;
			BtnQuickSearchApply(action);
		}
		else
		{
			_btnQuickSearch.Visible = true;
			_btnQuickSearch.SetFocus(true);
		}
	}

	/// <summary>
	/// Quick search.
	/// </summary>
	public void BtnQuickSearchApply(Action action)
	{
		if (!Options.oxceInstantQuickSearch && action != null && action.Details.type == SDLEventType.SDL_KEYDOWN)
		{
			SDLKey sym = action.Details.key.keysym.sym;
			if (sym != SDLK_RETURN && sym != SDLK_KP_ENTER && sym != SDLK_ESCAPE)
				return;
		}
		UpdateList();
	}

	/// <summary>
	/// Filters the current list of items.
	/// </summary>
	public void UpdateList()
	{
		string searchString = Unicode.UpperCase(_btnQuickSearch.Text);

		_lstItems.ClearList();
		_rows.Clear();

		int selCategory = _cbxCategory.Selected;
		string cat = _cats[selCategory];
		bool allItems = (cat == "STR_ALL_ITEMS");
		bool onlyItemsAtDestination = (cat == "STR_ITEMS_AT_DESTINATION");
		bool categoryResearched = (cat == "STR_FILTER_RESEARCHED");
		bool categoryResearchable = (cat == "STR_FILTER_RESEARCHABLE");
		bool categoryUnassigned = (cat == "STR_UNASSIGNED");
		bool specialCategory = allItems || onlyItemsAtDestination;

		if (_previousSort != _currentSort)
		{
			SellState.SortRows(_items, _currentSort);
		}

		for (int i = 0; i < _items.Count; ++i)
		{
			// filter
			if (categoryResearched || categoryResearchable)
			{
				if (_items[i].type == TRANSFER_ITEM)
				{
					RuleItem rule = (RuleItem)_items[i].rule;
					bool isResearchable = _game.SavedGame.IsResearchable(rule, _game.Mod);
					if (categoryResearched && isResearchable) continue;
					if (categoryResearchable && !isResearchable) continue;
				}
				else
				{
					// don't show non-items (e.g. craft, personnel)
					continue;
				}
			}
			else if (selCategory >= _vanillaCategories)
			{
				if (categoryUnassigned && _items[i].type == TRANSFER_ITEM)
				{
					RuleItem rule = (RuleItem)_items[i].rule;
					if (rule.Categories.Count != 0)
					{
						continue;
					}
				}
				else if (!specialCategory && !BelongsToCategory(i, cat))
				{
					continue;
				}
			}
			else
			{
				if (!specialCategory && cat != GetCategory(i))
				{
					continue;
				}
			}

			// "items at destination" filter
			if (onlyItemsAtDestination && _items[i].qtyDst <= 0)
			{
				continue;
			}

			// quick search
			if (!string.IsNullOrEmpty(searchString))
			{
				string projectName = Unicode.UpperCase(_items[i].name);
				if (!projectName.Contains(searchString, StringComparison.Ordinal))
				{
					continue;
				}
			}

			string name = _items[i].name;
			bool ammo = false;
			if (_items[i].type == TRANSFER_ITEM)
			{
				RuleItem rule = (RuleItem)_items[i].rule;
				ammo = (rule.BattleType == BattleType.BT_AMMO || (rule.BattleType == BattleType.BT_NONE && rule.ClipSize > 0));
				if (ammo)
				{
					name = name.Insert(0, "  ");
				}
			}
			_lstItems.AddRow(4, name, Str(_items[i].qtySrc - _items[i].amount), Str(_items[i].amount), Str(_items[i].qtyDst));
			_rows.Add(i);
			if (_items[i].amount > 0)
			{
				_lstItems.SetRowColor(_rows.Count - 1, _lstItems.SecondaryColor);
			}
			else if (ammo)
			{
				_lstItems.SetRowColor(_rows.Count - 1, _ammoColor);
			}
		}
	}

	/// <summary>
	/// Checks the stores of the base whose available space is decreasing (items with negative size can free space).
	/// </summary>
	/// <returns>True if that base would be over its storage limit.</returns>
	internal static bool StoresExceeded(double availableTo, double usedTo, double availableFrom, double usedFrom, double iQty)
	{
		double freeStoresTo = availableTo - usedTo - iQty;
		double freeStoresFrom = availableFrom - usedFrom + iQty;
		return iQty > 0.0 ? freeStoresTo < -0.00001 : freeStoresFrom < -0.00001;
	}

	/// <summary>
	/// Transfers the selected items.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		if (Options.storageLimitsEnforced && !Target.AreSame(_iQty, 0.0))
		{
			// check again (because of items with negative size)
			// But only check the base whose available space is decreasing.
			if (StoresExceeded(_baseTo.AvailableStores, _baseTo.GetUsedStores(), _baseFrom.AvailableStores, _baseFrom.GetUsedStores(), _iQty))
			{
				RuleInterface menuInterface = _game.Mod.GetInterface("transferMenu");
				_game.PushState(new ErrorMessageState(Tr("STR_NOT_ENOUGH_STORE_SPACE"), _palette, (byte)menuInterface.GetElement("errorMessage").color, "BACK13.SCR", menuInterface.GetElement("errorPalette").color));
				return;
			}
		}

		_game.PushState(new TransferConfirmState(_baseTo, this));
	}

	/// <summary>Transfer time (hours) for a given distance between bases.</summary>
	internal static int TransferTime(double distance) => (int)Math.Floor(6 + distance / 10.0);

	/// <summary>
	/// Completes the transfer between bases.
	/// </summary>
	public void CompleteTransfer()
	{
		int time = TransferTime(_distance);
		_game.SavedGame.Funds = _game.SavedGame.Funds - _total;
		foreach (var transferRow in _items)
		{
			if (transferRow.amount > 0)
			{
				Transfer t;
				Craft craft;
				Soldier soldier;
				switch (transferRow.type)
				{
					case TRANSFER_SOLDIER:
						for (int s = 0; s < _baseFrom.Soldiers.Count; ++s)
						{
							soldier = _baseFrom.Soldiers[s];
							if (soldier == transferRow.rule)
							{
								soldier.SetPsiTraining(false);
								if (soldier.IsInTraining)
								{
									soldier.ReturnToTrainingWhenHealed = true;
								}
								soldier.SetTraining(false);
								t = new Transfer(time);
								t.Soldier = soldier;
								_baseTo.Transfers.Add(t);
								_baseFrom.Soldiers.RemoveAt(s);
								break;
							}
						}
						break;
					case TRANSFER_CRAFT:
						craft = (Craft)transferRow.rule;
						// Transfer soldiers inside craft
						for (int s = 0; s < _baseFrom.Soldiers.Count;)
						{
							soldier = _baseFrom.Soldiers[s];
							if (soldier.Craft == craft)
							{
								soldier.SetPsiTraining(false);
								if (soldier.IsInTraining)
								{
									soldier.ReturnToTrainingWhenHealed = true;
								}
								soldier.SetTraining(false);
								if (craft.Status == "STR_OUT")
								{
									_baseTo.Soldiers.Add(soldier);
								}
								else
								{
									t = new Transfer(time);
									t.Soldier = soldier;
									_baseTo.Transfers.Add(t);
								}
								_baseFrom.Soldiers.RemoveAt(s);
							}
							else
							{
								++s;
							}
						}

						// Transfer craft
						_baseFrom.RemoveCraft(craft, false);
						if (craft.Status == "STR_OUT")
						{
							bool returning = (craft.Destination == (Target)craft.Base);
							_baseTo.Crafts.Add(craft);
							craft.SetBase(_baseTo, false);
							if (craft.Fuel <= craft.GetFuelLimit(_baseTo))
							{
								craft.LowFuel = true;
								craft.ReturnToBase();
							}
							else if (returning)
							{
								craft.LowFuel = false;
								craft.ReturnToBase();
							}
						}
						else
						{
							t = new Transfer(time);
							t.Craft = craft;
							_baseTo.Transfers.Add(t);
						}
						break;
					case TRANSFER_SCIENTIST:
						_baseFrom.Scientists = _baseFrom.Scientists - transferRow.amount;
						t = new Transfer(time);
						t.SetScientists(transferRow.amount);
						_baseTo.Transfers.Add(t);
						break;
					case TRANSFER_ENGINEER:
						_baseFrom.Engineers = _baseFrom.Engineers - transferRow.amount;
						t = new Transfer(time);
						t.SetEngineers(transferRow.amount);
						_baseTo.Transfers.Add(t);
						break;
					case TRANSFER_ITEM:
						RuleItem item = (RuleItem)transferRow.rule;
						_baseFrom.StorageItems.RemoveItem(item, transferRow.amount);
						t = new Transfer(time);
						t.SetItems(item, transferRow.amount);
						_baseTo.Transfers.Add(t);
						if (_debriefingState != null)
						{
							// remember the decreased amount for next sell/transfer
							_debriefingState.DecreaseRecoveredItemCount(item, transferRow.amount);
						}
						break;
				}
			}
		}

		if (_debriefingState != null && _debriefingState.TotalRecoveredItemCount <= 0)
		{
			_debriefingState.HideSellTransferButtons();
		}
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
		_game.PopState();
	}

	/// <summary>
	/// Increase all items to max, i.e. transfer everything.
	/// </summary>
	public void BtnTransferAllClick(Action action)
	{
		bool allItemsSelected = true;
		for (int i = 0; i < _lstItems.Texts; ++i)
		{
			if (_items[_rows[i]].type == TRANSFER_ITEM && _items[_rows[i]].amount < _items[_rows[i]].qtySrc)
			{
				allItemsSelected = false;
				break;
			}
		}

		int backup = _sel;
		_errorShown = false;
		for (int i = 0; i < _lstItems.Texts; ++i)
		{
			if (_items[_rows[i]].type == TRANSFER_ITEM)
			{
				_sel = i;
				if (allItemsSelected)
					DecreaseByValue(int.MaxValue);
				else
					IncreaseByValue(int.MaxValue);
				if (_errorShown)
				{
					break; // stop on first error
				}
			}
		}
		_sel = backup;
	}

	/// <summary>
	/// Starts increasing the item.
	/// </summary>
	public void LstItemsLeftArrowPress(Action action)
	{
		_sel = (int)_lstItems.SelectedRow;
		if (_game.IsLeftClick(action, true) && !_timerInc.IsRunning) _timerInc.Start();
	}

	/// <summary>
	/// Stops increasing the item.
	/// </summary>
	public void LstItemsLeftArrowRelease(Action action)
	{
		if (_game.IsLeftClick(action, true))
		{
			_timerInc.Stop();
		}
	}

	/// <summary>
	/// Increases the selected item;
	/// by one on left-click; to max on right-click.
	/// </summary>
	public void LstItemsLeftArrowClick(Action action)
	{
		if (_game.IsRightClick(action, true)) IncreaseByValue(int.MaxValue);
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
			IncreaseByValue(change);
			_timerInc.SetInterval(250);
			_timerDec.SetInterval(250);
		}
	}

	/// <summary>
	/// Starts decreasing the item.
	/// </summary>
	public void LstItemsRightArrowPress(Action action)
	{
		_sel = (int)_lstItems.SelectedRow;
		if (_game.IsLeftClick(action, true) && !_timerDec.IsRunning) _timerDec.Start();
	}

	/// <summary>
	/// Stops decreasing the item.
	/// </summary>
	public void LstItemsRightArrowRelease(Action action)
	{
		if (_game.IsLeftClick(action, true))
		{
			_timerDec.Stop();
		}
	}

	/// <summary>
	/// Decreases the selected item;
	/// by one on left-click; to 0 on right-click.
	/// </summary>
	public void LstItemsRightArrowClick(Action action)
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
		_sel = (int)_lstItems.SelectedRow;
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELUP)
		{
			_timerInc.Stop();
			_timerDec.Stop();
			if (action.AbsoluteXMouse >= _lstItems.ArrowsLeftEdge &&
				action.AbsoluteXMouse <= _lstItems.ArrowsRightEdge)
			{
				IncreaseByValue(Options.changeValueByMouseWheel);
			}
		}
		else if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELDOWN)
		{
			_timerInc.Stop();
			_timerDec.Stop();
			if (action.AbsoluteXMouse >= _lstItems.ArrowsLeftEdge &&
				action.AbsoluteXMouse <= _lstItems.ArrowsRightEdge)
			{
				DecreaseByValue(Options.changeValueByMouseWheel);
			}
		}
		else if (_game.IsRightClick(action, true))
		{
			if (action.AbsoluteXMouse >= _lstItems.ArrowsLeftEdge &&
				action.AbsoluteXMouse <= _lstItems.ArrowsRightEdge)
			{
				return;
			}
			if (GetRow().type == TRANSFER_ITEM)
			{
				if (GetRow().rule is RuleItem rule)
				{
					if (_game.IsCtrlPressed(true))
					{
						_game.PushState(new ItemLocationsState(rule));
					}
					else
					{
						_game.PushState(new ManufactureDependenciesTreeState(rule.Type));
					}
				}
			}
		}
		else if (_game.IsMiddleClick(action, true))
		{
			if (GetRow().type == TRANSFER_ITEM)
			{
				if (GetRow().rule is RuleItem rule)
				{
					string articleId = rule.UfopediaType;
					if (_game.IsCtrlPressed(true))
					{
						Ufopaedia.OpenArticle(_game, articleId);
					}
					else
					{
						RuleResearch selectedTopic = _game.Mod.GetResearch(articleId, false);
						if (selectedTopic != null)
						{
							_game.PushState(new TechTreeViewerState(selectedTopic, null, null, null));
						}
					}
				}
			}
			else if (GetRow().type == TRANSFER_CRAFT)
			{
				if (GetRow().rule is Craft rule)
				{
					string articleId = rule.Rules.Type;
					if (_game.IsCtrlPressed(true))
					{
						Ufopaedia.OpenArticle(_game, articleId);
					}
					else
					{
						_game.PushState(new TechTreeViewerState(null, null, null, rule.Rules));
					}
				}
			}
		}
	}

	/// <summary>
	/// Increases the quantity of the selected item to transfer by one.
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
	/// Increases the quantity of the selected item to transfer by "change".
	/// </summary>
	/// <param name="change">How much we want to add.</param>
	public void IncreaseByValue(int change)
	{
		if (0 >= change || GetRow().qtySrc <= GetRow().amount) return;
		string errorMessage = "";
		RuleItem selItem = null;
		Craft craft = null;
		int storageLimits = Options.storageLimitsEnforced ? 1 : 0;

		switch (GetRow().type)
		{
			case TRANSFER_SOLDIER:
			case TRANSFER_SCIENTIST:
			case TRANSFER_ENGINEER:
				if (_pQty + 1 > _baseTo.AvailableQuarters - _baseTo.UsedQuarters)
				{
					errorMessage = Tr("STR_NO_FREE_ACCOMODATION");
				}
				break;
			case TRANSFER_CRAFT:
				craft = (Craft)GetRow().rule;
				if (_cQty + 1 > _baseTo.AvailableHangars - _baseTo.UsedHangars)
				{
					errorMessage = Tr("STR_NO_FREE_HANGARS_FOR_TRANSFER");
				}
				else if (craft.NumTotalSoldiers > 0 && _pQty + craft.NumTotalSoldiers > _baseTo.AvailableQuarters - _baseTo.UsedQuarters)
				{
					errorMessage = Tr("STR_NO_FREE_ACCOMODATION_CREW");
				}
				else if (Options.storageLimitsEnforced)
				{
					double used = craft.TotalItemStorageSize;
					if (used > 0.0 && _baseTo.StoresOverfull(_iQty + used))
					{
						errorMessage = Tr("STR_NOT_ENOUGH_STORE_SPACE_FOR_CRAFT");
					}
				}
				break;
			case TRANSFER_ITEM:
				selItem = (RuleItem)GetRow().rule;
				if (selItem.Size > 0.0 && _baseTo.StoresOverfull(selItem.Size + _iQty))
				{
					errorMessage = Tr("STR_NOT_ENOUGH_STORE_SPACE");
				}
				if (selItem.IsAlien)
				{
					if (storageLimits * _aQty + 1 > _baseTo.GetAvailableContainment(selItem.PrisonType) - storageLimits * _baseTo.GetUsedContainment(selItem.PrisonType))
					{
						errorMessage = TrAlt("STR_NO_ALIEN_CONTAINMENT_FOR_TRANSFER", selItem.PrisonType);
					}
				}
				break;
		}

		if (errorMessage.Length == 0)
		{
			int freeQuarters = _baseTo.AvailableQuarters - _baseTo.UsedQuarters - _pQty;
			switch (GetRow().type)
			{
				case TRANSFER_SOLDIER:
				case TRANSFER_SCIENTIST:
				case TRANSFER_ENGINEER:
					change = Math.Min(Math.Min(freeQuarters, GetRow().qtySrc - GetRow().amount), change);
					_pQty += change;
					GetRow().amount += change;
					_total += GetRow().cost * change;
					break;
				case TRANSFER_CRAFT:
					_cQty++;
					_pQty += craft.NumTotalSoldiers;
					_iQty += craft.TotalItemStorageSize;
					GetRow().amount++;
					if (!Options.canTransferCraftsWhileAirborne || craft.Status != "STR_OUT")
						_total += GetRow().cost;
					break;
				case TRANSFER_ITEM:
					if (selItem.IsAlien)
					{
						int freeContainment = Options.storageLimitsEnforced ? _baseTo.GetAvailableContainment(selItem.PrisonType) - _baseTo.GetUsedContainment(selItem.PrisonType) - _aQty : int.MaxValue;
						change = Math.Min(Math.Min(freeContainment, GetRow().qtySrc - GetRow().amount), change);
					}
					// both aliens and items
					{
						double storesNeededPerItem = ((RuleItem)GetRow().rule).Size;
						double freeStores = _baseTo.AvailableStores - _baseTo.GetUsedStores() - _iQty;
						change = Math.Min(Math.Min(PurchaseState.MaxByStores(freeStores, storesNeededPerItem), GetRow().qtySrc - GetRow().amount), change);
						_iQty += change * storesNeededPerItem;
					}
					if (selItem.IsAlien)
					{
						_aQty += change;
					}
					GetRow().amount += change;
					_total += GetRow().cost * change;
					break;
			}
			UpdateItemStrings();
		}
		else
		{
			_timerInc.Stop();
			RuleInterface menuInterface = _game.Mod.GetInterface("transferMenu");
			_game.PushState(new ErrorMessageState(errorMessage, _palette, (byte)menuInterface.GetElement("errorMessage").color, "BACK13.SCR", menuInterface.GetElement("errorPalette").color));
			_errorShown = true;
		}
	}

	/// <summary>
	/// Decreases the quantity of the selected item to transfer by one.
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
	/// Decreases the quantity of the selected item to transfer by "change".
	/// </summary>
	/// <param name="change">How much we want to remove.</param>
	public void DecreaseByValue(int change)
	{
		if (0 >= change || 0 >= GetRow().amount) return;
		Craft craft = null;
		change = Math.Min(GetRow().amount, change);

		switch (GetRow().type)
		{
			case TRANSFER_SOLDIER:
			case TRANSFER_SCIENTIST:
			case TRANSFER_ENGINEER:
				_pQty -= change;
				break;
			case TRANSFER_CRAFT:
				craft = (Craft)GetRow().rule;
				_cQty--;
				_pQty -= craft.NumTotalSoldiers;
				_iQty -= craft.TotalItemStorageSize;
				break;
			case TRANSFER_ITEM:
				RuleItem selItem = (RuleItem)GetRow().rule;
				_iQty -= selItem.Size * change;
				if (selItem.IsAlien)
				{
					_aQty -= change;
				}
				break;
		}
		GetRow().amount -= change;
		if (!Options.canTransferCraftsWhileAirborne || null == craft || craft.Status != "STR_OUT")
			_total -= GetRow().cost * change;
		UpdateItemStrings();
	}

	/// <summary>
	/// Updates the quantity-strings of the selected item.
	/// </summary>
	public void UpdateItemStrings()
	{
		_lstItems.SetCellText(_sel, 1, Str(GetRow().qtySrc - GetRow().amount));
		_lstItems.SetCellText(_sel, 2, Str(GetRow().amount));

		if (GetRow().amount > 0)
		{
			_lstItems.SetRowColor(_sel, _lstItems.SecondaryColor);
		}
		else
		{
			_lstItems.SetRowColor(_sel, _lstItems.Color);
			if (GetRow().type == TRANSFER_ITEM)
			{
				RuleItem rule = (RuleItem)GetRow().rule;
				if (rule.BattleType == BattleType.BT_AMMO || (rule.BattleType == BattleType.BT_NONE && rule.ClipSize > 0))
				{
					_lstItems.SetRowColor(_sel, _ammoColor);
				}
			}
		}
	}

	/// <summary>
	/// Gets the total cost of the current transfer.
	/// </summary>
	public int Total => _total;

	/// <summary>
	/// Gets the shortest distance between the two bases.
	/// </summary>
	internal static double GetDistance(Target from, Target to)
	{
		double[] x = new double[3], y = new double[3], z = new double[3];
		double r = 51.2;
		Target @base = from;
		for (int i = 0; i < 2; ++i)
		{
			x[i] = r * Math.Cos(@base.Latitude) * Math.Cos(@base.Longitude);
			y[i] = r * Math.Cos(@base.Latitude) * Math.Sin(@base.Longitude);
			z[i] = r * -Math.Sin(@base.Latitude);
			@base = to;
		}
		x[2] = x[1] - x[0];
		y[2] = y[1] - y[0];
		z[2] = z[1] - z[0];
		return Math.Sqrt(x[2] * x[2] + y[2] * y[2] + z[2] * z[2]);
	}

	/// <summary>
	/// Updates the production list to match the category filter.
	/// </summary>
	public void CbxCategoryChange(Action action)
	{
		_previousSort = _currentSort;

		if (_game.IsCtrlPressed(true))
		{
			_currentSort = _game.IsShiftPressed(true) ? TransferSortDirection.BY_UNIT_SIZE : TransferSortDirection.BY_TOTAL_SIZE;
		}
		else if (_game.IsAltPressed(true))
		{
			_currentSort = _game.IsShiftPressed(true) ? TransferSortDirection.BY_UNIT_COST : TransferSortDirection.BY_TOTAL_COST;
		}
		else
		{
			_currentSort = TransferSortDirection.BY_LIST_ORDER;
		}

		UpdateList();
	}
}
