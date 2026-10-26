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
// Ported from: src/Basescape/SellState.h, src/Basescape/SellState.cpp
#nullable disable

using System.Globalization;
using static Acom.SDLKey;
using static Acom.TransferType;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Sell/Sack screen that lets the player sell
/// any items in a particular base.
/// </summary>
public class SellState : TouchState
{
	private readonly Base _base;
	private readonly DebriefingState _debriefingState;
	private TextButton _btnOk, _btnCancel, _btnTransfer;
	private TextEdit _btnQuickSearch;
	private Window _window;
	private Text _txtTitle, _txtSales, _txtFunds, _txtQuantity, _txtSell, _txtValue, _txtSpaceUsed;
	private ComboBox _cbxCategory;
	private TextList _lstItems;
	private readonly List<TransferRow> _items = [];
	private readonly List<int> _rows = [];
	private readonly List<string> _cats = [];
	private int _vanillaCategories;
	private int _sel;
	private long _total;
	private double _spaceChange;
	private readonly Timer _timerInc, _timerDec;
	private byte _ammoColor;
	private readonly OptionsOrigin _origin;
	private bool _reset;
	private bool _sellAllButOne;
	private bool _delayedInitDone;
	private TransferSortDirection _previousSort, _currentSort;

	/// <summary>Gets the row of the current selection.</summary>
	private TransferRow GetRow() => _items[_rows[_sel]];

	private static string Str(int value) => value.ToString(CultureInfo.InvariantCulture);

	/// <summary>
	/// Initializes all the elements in the Sell/Sack screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="debriefingState">Pointer to the debriefing state (sell recovered items), or null.</param>
	/// <param name="origin">Game section that originated this state.</param>
	public SellState(Base @base, DebriefingState debriefingState, OptionsOrigin origin = OptionsOrigin.OPT_GEOSCAPE)
	{
		_base = @base;
		_debriefingState = debriefingState;
		_sel = 0;
		_total = 0;
		_spaceChange = 0;
		_origin = origin;
		_reset = false;
		_sellAllButOne = false;
		_delayedInitDone = false;
		_previousSort = TransferSortDirection.BY_LIST_ORDER;
		_currentSort = TransferSortDirection.BY_LIST_ORDER;

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
	/// Delayed constructor functionality.
	/// </summary>
	public void DelayedInit()
	{
		if (_delayedInitDone)
		{
			return;
		}
		_delayedInitDone = true;

		bool overfull = _debriefingState == null && Options.storageLimitsEnforced && _base.StoresOverfull();
		bool overfullCritical = overfull && _base.StoresOverfullCritical();

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnQuickSearch = new TextEdit(this, 48, 9, 10, 13);
		_btnOk = new TextButton(148, 16, 8, 176);
		_btnCancel = new TextButton(148, 16, 164, 176);
		_btnTransfer = new TextButton(148, 16, 164, 176);
		_txtTitle = new Text(310, 17, 5, 8);
		_txtSales = new Text(150, 9, 10, 24);
		_txtFunds = new Text(150, 9, 160, 24);
		_txtSpaceUsed = new Text(150, 9, 160, 34);
		_txtQuantity = new Text(54, 9, 136, 44);
		_txtSell = new Text(96, 9, 190, 44);
		_txtValue = new Text(40, 9, 270, 44);
		_cbxCategory = new ComboBox(this, 120, 16, 10, 36);
		_lstItems = new TextList(287, 120, 8, 54);

		TouchComponentsCreate(_txtTitle);

		// Set palette
		SetInterface("sellMenu");

		_ammoColor = (byte)_game.Mod.GetInterface("sellMenu").GetElement("ammoColor").color;

		Add(_window, "window", "sellMenu");
		Add(_btnQuickSearch, "button", "sellMenu");
		Add(_btnOk, "button", "sellMenu");
		Add(_btnCancel, "button", "sellMenu");
		Add(_btnTransfer, "button", "sellMenu");
		Add(_txtTitle, "text", "sellMenu");
		Add(_txtSales, "text", "sellMenu");
		Add(_txtFunds, "text", "sellMenu");
		Add(_txtSpaceUsed, "text", "sellMenu");
		Add(_txtQuantity, "text", "sellMenu");
		Add(_txtSell, "text", "sellMenu");
		Add(_txtValue, "text", "sellMenu");
		Add(_lstItems, "list", "sellMenu");
		Add(_cbxCategory, "text", "sellMenu");

		TouchComponentsAdd("button2", "sellMenu", _window);

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "sellMenu");

		TouchComponentsConfigure();

		_btnOk.Text = Tr("STR_SELL_SACK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);

		_btnCancel.Text = Tr("STR_CANCEL");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_btnTransfer.Text = Tr("STR_GO_TO_TRANSFERS");
		_btnTransfer.OnMouseClick(BtnTransferClick);

		_btnCancel.Visible = !overfull;
		_btnOk.Visible = !overfull;
		_btnTransfer.Visible = overfull;

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_SELL_ITEMS_SACK_PERSONNEL"));

		_txtFunds.SetText(Tr("STR_FUNDS").Arg(Unicode.FormatFunding(_game.SavedGame.Funds)));

		_txtSpaceUsed.Visible = Options.storageLimitsEnforced;

		string ss = LocalizedText.FormatLikeOstream(_base.GetUsedStores()) + ":" + Str(_base.AvailableStores);
		_txtSpaceUsed.SetText(ss);
		_txtSpaceUsed.SetText(Tr("STR_SPACE_USED").Arg(ss));

		_txtQuantity.SetText(Tr("STR_QUANTITY_UC"));

		_txtSell.SetText(Tr("STR_SELL_SACK"));

		_txtValue.SetText(Tr("STR_VALUE"));

		_lstItems.SetArrowColumn(182, ArrowOrientation.ARROW_VERTICAL);
		_lstItems.SetColumns(4, 156, 54, 24, 53);
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

		_cats.Add("STR_ALL_ITEMS");
		_cats.Add("STR_FILTER_HIDDEN");
		if (Options.oxceBaseFilterResearchable)
		{
			_cats.Add("STR_FILTER_RESEARCHED");
			_cats.Add("STR_FILTER_RESEARCHABLE");
		}

		foreach (var soldier in _base.Soldiers)
		{
			if (_debriefingState != null) break;
			if (soldier.Craft == null)
			{
				AddRow(new TransferRow { type = TRANSFER_SOLDIER, rule = soldier, name = soldier.GetName(true), cost = 0, qtySrc = 1, listOrder = -4 });
			}
		}
		foreach (var craft in _base.Crafts)
		{
			if (_debriefingState != null) break;
			if (craft.Status != "STR_OUT")
			{
				AddRow(new TransferRow { type = TRANSFER_CRAFT, rule = craft, name = craft.GetName(_game.Language), cost = craft.Rules.SellCost, qtySrc = 1, listOrder = -3, totalCost = craft.Rules.SellCost });
			}
		}
		if (_base.AvailableScientists > 0 && _debriefingState == null)
		{
			AddRow(new TransferRow { type = TRANSFER_SCIENTIST, rule = null, name = Tr("STR_SCIENTIST"), cost = 0, qtySrc = _base.AvailableScientists, listOrder = -2 });
		}
		if (_base.AvailableEngineers > 0 && _debriefingState == null)
		{
			AddRow(new TransferRow { type = TRANSFER_ENGINEER, rule = null, name = Tr("STR_ENGINEER"), cost = 0, qtySrc = _base.AvailableEngineers, listOrder = -1 });
		}
		foreach (var itemType in _game.Mod.ItemsList)
		{
			RuleItem rule = _game.Mod.GetItem(itemType, true);
			int qty = 0;
			if (_debriefingState != null)
			{
				qty = _debriefingState.GetRecoveredItemCount(rule);
			}
			else
			{
				qty = _base.StorageItems.GetItem(rule);
				if (Options.storageLimitsEnforced && (_origin == OptionsOrigin.OPT_BATTLESCAPE || overfullCritical))
				{
					foreach (var transfer in _base.Transfers)
					{
						if (transfer.Items == rule)
						{
							qty += transfer.Quantity;
						}
						else if (transfer.Craft != null)
						{
							qty += overfullCritical ? transfer.Craft.GetTotalItemCount(rule) : transfer.Craft.Items.GetItem(rule);
						}
					}
					foreach (var craft in _base.Crafts)
					{
						qty += overfullCritical ? craft.GetTotalItemCount(rule) : craft.Items.GetItem(rule);
					}
				}
			}
			if (qty > 0 && (Options.canSellLiveAliens || !rule.IsAlien))
			{
				int cost = rule.GetSellCostAdjusted(_base, _game.SavedGame);
				TransferRow row = new() { type = TRANSFER_ITEM, rule = rule, name = Tr(itemType), cost = cost, qtySrc = qty, listOrder = rule.ListOrder, size = rule.Size, totalSize = qty * rule.Size, totalCost = (long)qty * rule.GetSellCostAdjusted(_base, _game.SavedGame) };
				if ((_debriefingState != null) && (_game.SavedGame.GetAutosell(rule)))
				{
					row.amount = qty;
					_total += row.cost * qty;
					_spaceChange -= qty * rule.Size;
				}
				AddRow(row);
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
				_cats.Add("STR_FILTER_HIDDEN");
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

		_txtSales.SetText(Tr("STR_VALUE_OF_SALES").Arg(Unicode.FormatFunding(_total)));

		_cbxCategory.SetOptions(_cats, true);
		_cbxCategory.OnChange(CbxCategoryChange);
		_cbxCategory.OnKeyboardPress(BtnSellAllClick, Options.keySellAll);
		_cbxCategory.OnKeyboardPress(BtnSellAllButOneClick, Options.keySellAllButOne);

		_btnQuickSearch.Text = ""; // redraw
		_btnQuickSearch.OnChange(BtnQuickSearchApply);
		_btnQuickSearch.Visible = Options.oxceQuickSearchButton;

		// OK button is not always visible, so bind it here
		_cbxCategory.OnKeyboardRelease(BtnQuickSearchToggle, Options.keyToggleQuickSearch);

		UpdateList();
	}

	/// <summary>
	/// Resets stuff when coming back from other screens.
	/// </summary>
	public override void Init()
	{
		DelayedInit();

		base.Init();

		if (_reset)
		{
			_game.PopState();
			_game.PushState(new SellState(_base, _debriefingState, _origin));
		}

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
	/// Determines if a row item is supposed to be hidden.
	/// </summary>
	private bool IsHidden(int sel)
	{
		string itemName = "";

		switch (_items[sel].type)
		{
			case TRANSFER_SOLDIER:
			case TRANSFER_SCIENTIST:
			case TRANSFER_ENGINEER:
				return false;
			case TRANSFER_CRAFT:
				return false;
			case TRANSFER_ITEM:
				if (_items[sel].rule is RuleItem rule)
				{
					itemName = rule.Type;
				}
				if (itemName.Length != 0)
				{
					var hiddenMap = _game.SavedGame.HiddenPurchaseItems;
					if (hiddenMap.TryGetValue(itemName, out bool hidden))
					{
						return hidden;
					}
					else
					{
						// not found = not hidden
						return false;
					}
				}
				break;
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
	/// Sorts transfer rows like the C++ <c>std::stable_sort</c> calls of Sell/Transfer screens.
	/// </summary>
	internal static void SortRows(List<TransferRow> items, TransferSortDirection sort)
	{
		IEnumerable<TransferRow> sorted = sort switch
		{
			TransferSortDirection.BY_TOTAL_COST => items.OrderByDescending(r => r.totalCost),
			TransferSortDirection.BY_UNIT_COST => items.OrderByDescending(r => r.cost),
			TransferSortDirection.BY_TOTAL_SIZE => items.OrderByDescending(r => r.totalSize),
			TransferSortDirection.BY_UNIT_SIZE => items.OrderByDescending(r => r.size),
			_ => items.OrderBy(r => r.listOrder),
		};
		var list = sorted.ToList(); // LINQ ordering is stable
		items.Clear();
		items.AddRange(list);
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
		string selectedCategory = _cats[selCategory];
		bool categoryFilterEnabled = (selectedCategory != "STR_ALL_ITEMS");
		bool categoryUnassigned = (selectedCategory == "STR_UNASSIGNED");
		bool categoryHidden = (selectedCategory == "STR_FILTER_HIDDEN");
		bool categoryResearched = (selectedCategory == "STR_FILTER_RESEARCHED");
		bool categoryResearchable = (selectedCategory == "STR_FILTER_RESEARCHABLE");

		if (_previousSort != _currentSort)
		{
			SortRows(_items, _currentSort);
		}

		for (int i = 0; i < _items.Count; ++i)
		{
			// filter
			if (categoryHidden)
			{
				bool hidden = IsHidden(i);
				if (!hidden)
				{
					continue;
				}
			}
			else if (categoryResearched || categoryResearchable)
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
				else if (categoryFilterEnabled && !BelongsToCategory(i, selectedCategory))
				{
					continue;
				}
			}
			else
			{
				if (categoryFilterEnabled && selectedCategory != GetCategory(i))
				{
					continue;
				}
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
			long adjustedCost = _items[i].cost;
			_lstItems.AddRow(4, name, Str(_items[i].qtySrc - _items[i].amount), Str(_items[i].amount), Unicode.FormatFunding(adjustedCost));
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
	/// Removes up to <paramref name="toRemove"/> items from a container.
	/// </summary>
	/// <returns>How many items still need to be removed.</returns>
	internal static int CleanUpContainer(ItemContainer container, RuleItem rule, int toRemove)
	{
		int curr = container.GetItem(rule);
		if (curr >= toRemove)
		{
			container.RemoveItem(rule, toRemove);
			return 0;
		}
		else
		{
			container.RemoveItem(rule, int.MaxValue);
			return toRemove - curr;
		}
	}

	/// <summary>
	/// Removes up to <paramref name="toRemove"/> items from the weapons and vehicles of a craft;
	/// the rest of a dismantled weapon/vehicle goes to the base stores.
	/// </summary>
	/// <returns>How many items still need to be removed.</returns>
	internal static int CleanUpCraft(Base @base, Craft craft2, RuleItem rule, int toRemove)
	{
		(int ToRemove, int ToSave, RuleItem rule) tryRemove(int curr, RuleItem i)
		{
			if (i == rule)
			{
				int r = Math.Min(toRemove, curr);
				toRemove -= r;
				curr -= r;
				return (r, curr, i);
			}
			else
			{
				return (0, curr, i);
			}
		}
		void tryStore((int ToRemove, int ToSave, RuleItem rule) s)
		{
			if (s.ToSave > 0)
			{
				@base.StorageItems.AddItem(s.rule, s.ToSave);
			}
		}

		var weapons = craft2.Weapons;
		for (int wi = 0; wi < weapons.Count; ++wi)
		{
			var w = weapons[wi];
			if (w != null)
			{
				var wr = w.Rules;

				var launcher = tryRemove(1, wr.LauncherItem);
				var clip = tryRemove(w.ClipsLoaded, wr.ClipItem);
				if (launcher.ToRemove != 0 || clip.ToRemove != 0)
				{
					tryStore(launcher);
					tryStore(clip);

					weapons[wi] = null;
				}
			}
		}

		craft2.Vehicles.RemoveAll(v =>
		{
			var clipType = v.Rules.VehicleClipAmmo;

			var launcher = tryRemove(1, v.Rules);
			var clip = tryRemove(v.Rules.VehicleClipsLoaded, clipType);

			if (launcher.ToRemove != 0 || clip.ToRemove != 0)
			{
				tryStore(launcher);
				tryStore(clip);

				return true;
			}
			else
			{
				return false;
			}
		});

		return toRemove;
	}

	/// <summary>
	/// Sells the selected items.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.SavedGame.Funds = _game.SavedGame.Funds + _total;

		foreach (var transferRow in _items)
		{
			if (transferRow.amount > 0)
			{
				switch (transferRow.type)
				{
					case TRANSFER_SOLDIER:
						{
							Soldier tmpSoldier = (Soldier)transferRow.rule;
							for (int s = 0; s < _base.Soldiers.Count; ++s)
							{
								if (_base.Soldiers[s] == tmpSoldier)
								{
									if (tmpSoldier.Armor.StoreItem != null)
									{
										_base.StorageItems.AddItem(tmpSoldier.Armor.StoreItem);
									}
									_base.Soldiers.RemoveAt(s);
									break;
								}
							}
						}
						break;
					case TRANSFER_CRAFT:
						{
							Craft tmpCraft = (Craft)transferRow.rule;
							_base.RemoveCraft(tmpCraft, true);
						}
						break;
					case TRANSFER_SCIENTIST:
						_base.Scientists = _base.Scientists - transferRow.amount;
						break;
					case TRANSFER_ENGINEER:
						_base.Engineers = _base.Engineers - transferRow.amount;
						break;
					case TRANSFER_ITEM:
						RuleItem item = (RuleItem)transferRow.rule;
						{
							// remove all of said items from base
							int toRemove = CleanUpContainer(_base.StorageItems, item, transferRow.amount);

							// if we still need to remove any, remove them from the crafts first, and keep a running tally
							foreach (var craft in _base.Crafts)
							{
								if (toRemove <= 0) break; // loop finished
								toRemove = CleanUpContainer(craft.Items, item, toRemove);
								if (toRemove > 0)
								{
									toRemove = CleanUpCraft(_base, craft, item, toRemove);
								}
							}

							// if there are STILL any left to remove, take them from the transfers, and if necessary, delete it.
							var transfers = _base.Transfers;
							for (int ti = 0; ti < transfers.Count && toRemove != 0;)
							{
								var transfer = transfers[ti];
								if (transfer.Items == item)
								{
									if (transfer.Quantity <= toRemove)
									{
										toRemove -= transfer.Quantity;
										transfers.RemoveAt(ti);
									}
									else
									{
										transfer.SetItems(transfer.Items, transfer.Quantity - toRemove);
										toRemove = 0;
									}
								}
								else
								{
									if (transfer.Craft != null)
									{
										toRemove = CleanUpContainer(transfer.Craft.Items, item, toRemove);
										if (toRemove > 0)
										{
											toRemove = CleanUpCraft(_base, transfer.Craft, item, toRemove);
										}
									}
									++ti;
								}
							}
						}

						// Note: this only updates a helper map, it doesn't affect real item recovery (that has already happened and all items are already in the base)
						if (_debriefingState != null)
						{
							// remember the decreased amount for next sell/transfer
							_debriefingState.DecreaseRecoveredItemCount(item, transferRow.amount);

							// set autosell status if we sold all of the item
							_game.SavedGame.SetAutosell(item, (transferRow.qtySrc == transferRow.amount));
						}

						break;
				}
			}
			else
			{
				if (_debriefingState != null && transferRow.type == TRANSFER_ITEM)
				{
					// disable autosell since we haven't sold any of the item.
					_game.SavedGame.SetAutosell((RuleItem)transferRow.rule, false);
				}
			}
		}
		if (_debriefingState != null && _debriefingState.TotalRecoveredItemCount <= 0)
		{
			_debriefingState.HideSellTransferButtons();
		}
		_game.PopState();
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
		_reset = true;
		_game.PushState(new TransferBaseState(_base, null));
	}

	/// <summary>
	/// Increase all items to max, i.e. sell everything.
	/// </summary>
	public void BtnSellAllClick(Action action)
	{
		bool allItemsSelected = true;
		for (int i = 0; i < _lstItems.Texts; ++i)
		{
			if (_items[_rows[i]].qtySrc > _items[_rows[i]].amount)
			{
				allItemsSelected = false;
				break;
			}
		}
		int dir = allItemsSelected ? -1 : 1;

		int backup = _sel;
		for (int i = 0; i < _lstItems.Texts; ++i)
		{
			_sel = i;
			ChangeByValue(int.MaxValue, dir);
		}
		_sel = backup;
	}

	/// <summary>
	/// Increase all items to max - 1, i.e. sell everything but one.
	/// </summary>
	public void BtnSellAllButOneClick(Action action)
	{
		_sellAllButOne = true;
		BtnSellAllClick(null);
		_sellAllButOne = false;
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
	/// by one on left-click, to max on right-click.
	/// </summary>
	public void LstItemsLeftArrowClick(Action action)
	{
		if (_game.IsRightClick(action, true)) ChangeByValue(int.MaxValue, 1);
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
			ChangeByValue(change, 1);
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
	/// by one on left-click, to 0 on right-click.
	/// </summary>
	public void LstItemsRightArrowClick(Action action)
	{
		if (_game.IsRightClick(action, true)) ChangeByValue(int.MaxValue, -1);
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
			ChangeByValue(change, -1);
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
				ChangeByValue(Options.changeValueByMouseWheel, 1);
			}
		}
		else if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELDOWN)
		{
			_timerInc.Stop();
			_timerDec.Stop();
			if (action.AbsoluteXMouse >= _lstItems.ArrowsLeftEdge &&
				action.AbsoluteXMouse <= _lstItems.ArrowsRightEdge)
			{
				ChangeByValue(Options.changeValueByMouseWheel, -1);
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
						if (_game.IsShiftPressed(true))
						{
							if (rule.Type.Length != 0)
							{
								bool categoryHidden = (_cats[_cbxCategory.Selected] == "STR_FILTER_HIDDEN");

								var hiddenMap = _game.SavedGame.HiddenPurchaseItems;
								bool hidden = false;
								if (hiddenMap.ContainsKey(rule.Type))
								{
									// found => unhide it when in "Hidden" view; mark it as hidden otherwise
									hidden = !categoryHidden;
								}
								else
								{
									// not found = not hidden yet => mark it as hidden
									hidden = true;
								}
								_game.SavedGame.SetHiddenPurchaseItemsStatus(rule.Type, hidden);

								if (categoryHidden)
								{
									// update screen
									int scrollPos = _lstItems.Scroll;
									UpdateList();
									_lstItems.ScrollTo(scrollPos);
								}
								else
								{
									// no screen update, at least play a sound
									_game.Mod.GetSound("GEO.CAT", Mod.UFO_EXPLODE).Play();
								}
							}
						}
						else
						{
							_game.PushState(new ItemLocationsState(rule));
						}
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
	/// Increases the quantity of the selected item to sell by one.
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
		ChangeByValue(change, 1);
	}

	/// <summary>
	/// Increases or decreases the quantity of the selected item to sell.
	/// </summary>
	/// <param name="change">How much we want to add or remove.</param>
	/// <param name="dir">Direction to change, +1 to increase or -1 to decrease.</param>
	public void ChangeByValue(int change, int dir)
	{
		if (dir > 0 && GetRow().type == TRANSFER_ITEM)
		{
			RuleItem tmpItem = (RuleItem)GetRow().rule;
			if (tmpItem.SellActionMessage.Length != 0 && !_game.IsShiftPressed(true))
			{
				_timerInc.Stop();
				_timerDec.Stop();
				RuleInterface menuInterface = _game.Mod.GetInterface("buyMenu");
				_game.PushState(new ErrorMessageState(
					Tr(tmpItem.SellActionMessage),
					_palette,
					(byte)menuInterface.GetElement("errorMessage").color,
					"BACK13.SCR",
					menuInterface.GetElement("errorPalette").color)
				);

				return;
			}
		}

		if (dir > 0)
		{
			if (0 >= change || GetRow().qtySrc <= GetRow().amount) return;
			change = Math.Min(GetRow().qtySrc - GetRow().amount, change);
			if (_sellAllButOne && change > 0)
			{
				--change;
			}
		}
		else
		{
			if (0 >= change || 0 >= GetRow().amount) return;
			change = Math.Min(GetRow().amount, change);
		}
		GetRow().amount += dir * change;
		_total += dir * GetRow().cost * change;

		// Calculate the change in storage space.
		switch (GetRow().type)
		{
			case TRANSFER_SOLDIER:
				{
					Soldier soldier = (Soldier)GetRow().rule;
					if (soldier.Armor.StoreItem != null)
					{
						_spaceChange += dir * soldier.Armor.StoreItem.Size;
					}
				}
				break;
			case TRANSFER_CRAFT:
				// Note: in OXCE, there is no storage space change, everything on the craft is already included in the base storage space calculations
				break;
			case TRANSFER_ITEM:
				{
					RuleItem item = (RuleItem)GetRow().rule;
					_spaceChange -= dir * change * item.Size;
				}
				break;
			default:
				//TRANSFER_SCIENTIST and TRANSFER_ENGINEER do not own anything that takes storage
				break;
		}

		UpdateItemStrings();
	}

	/// <summary>
	/// Decreases the quantity of the selected item to sell by one.
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
		ChangeByValue(change, -1);
	}

	/// <summary>
	/// Updates the quantity-strings of the selected item.
	/// </summary>
	public void UpdateItemStrings()
	{
		_lstItems.SetCellText(_sel, 2, Str(GetRow().amount));
		_lstItems.SetCellText(_sel, 1, Str(GetRow().qtySrc - GetRow().amount));
		_txtSales.SetText(Tr("STR_VALUE_OF_SALES").Arg(Unicode.FormatFunding(_total)));

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

		_txtSpaceUsed.SetText(Tr("STR_SPACE_USED").Arg(PurchaseState.FormatSpaceUsed(_base.GetUsedStores(), _spaceChange, _base.AvailableStores)));
		if (_debriefingState == null && Options.storageLimitsEnforced)
		{
			_btnOk.Visible = !_base.StoresOverfull(_spaceChange);
		}
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
