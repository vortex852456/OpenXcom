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
// Ported from: src/Basescape/PurchaseState.h, src/Basescape/PurchaseState.cpp
#nullable disable

using System.Globalization;
using static Acom.SDLKey;
using static Acom.TransferType;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Purchase/Hire screen that lets the player buy
/// new items for a base.
/// </summary>
public class PurchaseState : TouchState
{
	private readonly Base _base;
	private readonly CannotReequipState _parent;
	private readonly bool _autoBuyDone;
	private readonly Dictionary<RuleItem, int> _missingItemsMap = [];

	private readonly TextButton _btnOk, _btnCancel;
	private readonly TextEdit _btnQuickSearch;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtFunds, _txtPurchases, _txtCost, _txtQuantity, _txtSpaceUsed;
	private readonly ComboBox _cbxCategory;
	private readonly TextList _lstItems;
	private readonly List<TransferRow> _items = [];
	private readonly List<int> _rows = [];
	private readonly List<string> _cats = [];
	private readonly int _vanillaCategories;
	private int _sel;
	private int _total, _pQty, _cQty;
	private double _iQty;
	private readonly Dictionary<int, int> _iPrisonQty = [];
	private readonly byte _ammoColor;
	private readonly Timer _timerInc, _timerDec;

	/// <summary>Gets the row of the current selection.</summary>
	private TransferRow GetRow() => _items[_rows[_sel]];

	private static string Str(int value) => value.ToString(CultureInfo.InvariantCulture);

	/// <summary>C++ <c>std::map::operator[]</c> read: inserts a zero entry when missing.</summary>
	internal static int MapGet<TKey>(IDictionary<TKey, int> map, TKey key)
	{
		if (!map.TryGetValue(key, out int value))
		{
			map[key] = 0;
			value = 0;
		}
		return value;
	}

	/// <summary>
	/// Formats the "space used" value: used stores, the pending change (if any) and the available stores.
	/// </summary>
	internal static string FormatSpaceUsed(double usedStores, double change, int availableStores)
	{
		string s = LocalizedText.FormatLikeOstream(usedStores);
		if (Math.Abs(change) > 0.05)
		{
			s += "(";
			if (change > 0.05)
				s += "+";
			s += change.ToString("F1", CultureInfo.InvariantCulture) + ")";
		}
		s += ":" + Str(availableStores);
		return s;
	}

	/// <summary>
	/// How many items of the given size still fit into the free store space (both aliens and items).
	/// </summary>
	internal static int MaxByStores(double freeStores, double storesNeededPerItem)
	{
		double maxByStores = (double)int.MaxValue;
		if (!Target.AreSame(storesNeededPerItem, 0.0) && storesNeededPerItem > 0.0)
		{
			maxByStores = (freeStores + 0.05) / storesNeededPerItem;
		}
		return DoubleToInt(maxByStores);
	}

	/// <summary>C++ <c>(int)double</c>; out-of-range values are undefined in C++ (x86: INT_MIN), here clamped to the int range.</summary>
	internal static int DoubleToInt(double value)
	{
		if (value >= int.MaxValue)
			return int.MaxValue;
		if (value <= int.MinValue)
			return int.MinValue;
		return (int)value;
	}

	/// <summary>
	/// Initializes all the elements in the Purchase/Hire screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="parent">Pointer to the "cannot reequip" state (missing items), or null.</param>
	public PurchaseState(Base @base, CannotReequipState parent = null)
	{
		_base = @base;
		_parent = parent;
		_sel = 0;
		_total = 0;
		_pQty = 0;
		_cQty = 0;
		_iQty = 0.0;
		_ammoColor = 0;

		_autoBuyDone = false;
		if (_parent != null)
		{
			foreach (var i in _parent.MissingItems)
			{
				if (i.qty > 0)
				{
					var rule = _game.Mod.GetItem(i.item);
					if (rule != null)
					{
						_missingItemsMap[rule] = i.qty;
					}
				}
			}
		}

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnQuickSearch = new TextEdit(this, 48, 9, 10, 13);
		_btnOk = new TextButton(148, 16, 8, 176);
		_btnCancel = new TextButton(148, 16, 164, 176);
		_txtTitle = new Text(310, 17, 5, 8);
		_txtFunds = new Text(150, 9, 10, 24);
		_txtPurchases = new Text(150, 9, 160, 24);
		_txtSpaceUsed = new Text(150, 9, 160, 34);
		_txtCost = new Text(102, 9, 152, 44);
		_txtQuantity = new Text(60, 9, 256, 44);
		_cbxCategory = new ComboBox(this, 120, 16, 10, 36);
		_lstItems = new TextList(287, 120, 8, 54);

		TouchComponentsCreate(_txtTitle);

		// Set palette
		SetInterface("buyMenu");

		_ammoColor = (byte)_game.Mod.GetInterface("buyMenu").GetElement("ammoColor").color;

		Add(_window, "window", "buyMenu");
		Add(_btnQuickSearch, "button", "buyMenu");
		Add(_btnOk, "button", "buyMenu");
		Add(_btnCancel, "button", "buyMenu");
		Add(_txtTitle, "text", "buyMenu");
		Add(_txtFunds, "text", "buyMenu");
		Add(_txtPurchases, "text", "buyMenu");
		Add(_txtSpaceUsed, "text", "buyMenu");
		Add(_txtCost, "text", "buyMenu");
		Add(_txtQuantity, "text", "buyMenu");
		Add(_lstItems, "list", "buyMenu");
		Add(_cbxCategory, "text", "buyMenu");

		TouchComponentsAdd("button2", "buyMenu", _window);

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "buyMenu");

		TouchComponentsConfigure();

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);

		_btnCancel.Text = Tr("STR_CANCEL");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_PURCHASE_HIRE_PERSONNEL"));

		_txtFunds.SetText(Tr("STR_CURRENT_FUNDS").Arg(Unicode.FormatFunding(_game.SavedGame.Funds)));

		_txtPurchases.SetText(Tr("STR_COST_OF_PURCHASES").Arg(Unicode.FormatFunding(_total)));

		_txtSpaceUsed.Visible = Options.storageLimitsEnforced;
		string ss = LocalizedText.FormatLikeOstream(_base.GetUsedStores()) + ":" + Str(_base.AvailableStores);
		_txtSpaceUsed.SetText(Tr("STR_SPACE_USED").Arg(ss));

		_txtCost.SetText(Tr("STR_COST_PER_UNIT_UC"));

		_txtQuantity.SetText(Tr("STR_QUANTITY_UC"));

		_lstItems.SetArrowColumn(227, ArrowOrientation.ARROW_VERTICAL);
		_lstItems.SetColumns(4, 150, 55, 50, 32);
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
		_cats.Add("STR_FILTER_EQUIPPED");
		if (_missingItemsMap.Count != 0)
		{
			_cats.Add("STR_FILTER_MISSING");
		}

		RuleBaseFacilityFunctions providedBaseFunc = _base.GetProvidedBaseFunc(default);

		// setup the filter methods to be used in various situations (craft, soldiers, items).
		bool necessaryBaseFunctionsPresent(RuleBaseFacilityFunctions requires) => (~providedBaseFunc & requires).None();
		static bool requiredCountryAllied(string requiresBuyCountry)
		{
			if (!string.IsNullOrEmpty(requiresBuyCountry))
			{
				foreach (var country in _game.SavedGame.Countries)
				{
					if (country.Pact && country.Rules.Type == requiresBuyCountry)
					{
						return false;
					}
				}
			}
			return true;
		}

		foreach (var soldierType in _game.Mod.SoldiersList)
		{
			RuleSoldier rule = _game.Mod.GetSoldier(soldierType);
			if (rule.BuyCost != 0 && _game.SavedGame.IsResearched(rule.Requirements) && necessaryBaseFunctionsPresent(rule.RequiresBuyBaseFunc) && requiredCountryAllied(rule.RequiresBuyCountry))
			{
				AddRow(new TransferRow { type = TRANSFER_SOLDIER, rule = rule, name = Tr(rule.Type), cost = rule.BuyCost, qtySrc = _base.GetSoldierCountAndSalary(rule.Type).first, listOrder = -4 });
			}
		}
		if ((string.IsNullOrEmpty(_game.Mod.HireScientistsUnlockResearch) || _game.SavedGame.IsResearched(_game.Mod.HireScientistsUnlockResearch, true))
			&& (~providedBaseFunc & _game.Mod.HireScientistsRequiresBaseFunc).None())
		{
			AddRow(new TransferRow { type = TRANSFER_SCIENTIST, rule = null, name = Tr("STR_SCIENTIST"), cost = _game.Mod.HireScientistCost, qtySrc = _base.TotalScientists, listOrder = -3 });
		}
		if ((string.IsNullOrEmpty(_game.Mod.HireEngineersUnlockResearch) || _game.SavedGame.IsResearched(_game.Mod.HireEngineersUnlockResearch, true))
			&& (~providedBaseFunc & _game.Mod.HireEngineersRequiresBaseFunc).None())
		{
			AddRow(new TransferRow { type = TRANSFER_ENGINEER, rule = null, name = Tr("STR_ENGINEER"), cost = _game.Mod.HireEngineerCost, qtySrc = _base.TotalEngineers, listOrder = -2 });
		}
		foreach (var craftType in _game.Mod.CraftsList)
		{
			RuleCraft rule = _game.Mod.GetCraft(craftType);
			if (rule.BuyCost != 0 && _game.SavedGame.IsResearched(rule.Requirements) && necessaryBaseFunctionsPresent(rule.RequiresBuyBaseFunc) && requiredCountryAllied(rule.RequiresBuyCountry))
			{
				AddRow(new TransferRow { type = TRANSFER_CRAFT, rule = rule, name = Tr(rule.Type), cost = rule.BuyCost, qtySrc = _base.GetCraftCount(rule), listOrder = -1 });
			}
		}
		foreach (var itemType in _game.Mod.ItemsList)
		{
			RuleItem rule = _game.Mod.GetItem(itemType);
			if (rule.BuyCost != 0 && _game.SavedGame.IsResearched(rule.Requirements) && _game.SavedGame.IsResearched(rule.BuyRequirements) && necessaryBaseFunctionsPresent(rule.RequiresBuyBaseFunc) && requiredCountryAllied(rule.RequiresBuyCountry))
			{
				AddRow(new TransferRow { type = TRANSFER_ITEM, rule = rule, name = Tr(rule.Type), cost = rule.GetBuyCostAdjusted(_base, _game.SavedGame), qtySrc = _base.StorageItems.GetItem(rule), listOrder = rule.ListOrder });
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
				_cats.Add("STR_FILTER_EQUIPPED");
				if (_missingItemsMap.Count != 0)
				{
					_cats.Add("STR_FILTER_MISSING");
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
		if (_missingItemsMap.Count != 0)
		{
			_cbxCategory.Selected = 3; // STR_FILTER_MISSING
		}
		_cbxCategory.OnChange(CbxCategoryChange);

		_btnQuickSearch.Text = ""; // redraw
		_btnQuickSearch.OnChange(BtnQuickSearchApply);
		_btnQuickSearch.Visible = Options.oxceQuickSearchButton;

		_btnOk.OnKeyboardRelease(BtnQuickSearchToggle, Options.keyToggleQuickSearch);

		UpdateList();

		_autoBuyDone = true;
		if (_missingItemsMap.Count != 0)
		{
			_txtPurchases.SetText(Tr("STR_COST_OF_PURCHASES").Arg(Unicode.FormatFunding(_total)));
			_txtSpaceUsed.SetText(Tr("STR_SPACE_USED").Arg(FormatSpaceUsed(_base.GetUsedStores(), _iQty, _base.AvailableStores)));
		}

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
			case TRANSFER_ITEM:
				if (_items[sel].type == TRANSFER_CRAFT)
				{
					if (_items[sel].rule is RuleCraft rule)
					{
						itemName = rule.Type;
					}
				}
				else
				{
					if (_items[sel].rule is RuleItem rule)
					{
						itemName = rule.Type;
					}
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
	/// Determines if a row item corresponds to equipped items.
	/// </summary>
	private bool IsEquipped(int sel)
	{
		switch (_items[sel].type)
		{
			case TRANSFER_SOLDIER:
			case TRANSFER_SCIENTIST:
			case TRANSFER_ENGINEER:
			case TRANSFER_CRAFT:
				return false;
			case TRANSFER_ITEM:
				if (_items[sel].rule is RuleItem rule)
				{
					// iterate all craft, also craft which are currently not at the base
					foreach (var xcraft in _base.Crafts)
					{
						if (xcraft.Items.GetItem(rule) > 0)
							return true;
					}
				}
				break;
		}

		return false;
	}

	/// <summary>
	/// Determines if a row item is in the map of missing items.
	/// </summary>
	/// <returns>Number of missing items that can be bought. -1 if not missing.</returns>
	private int GetMissingQty(int sel)
	{
		switch (_items[sel].type)
		{
			case TRANSFER_SOLDIER:
			case TRANSFER_SCIENTIST:
			case TRANSFER_ENGINEER:
			case TRANSFER_CRAFT:
				return -1;
			case TRANSFER_ITEM:
				if (_items[sel].rule is RuleItem rule)
				{
					if (_missingItemsMap.TryGetValue(rule, out int missing))
					{
						if (rule.MonthlyBuyLimit > 0)
						{
							var itemPurchaseLimitLog = _game.SavedGame.MonthlyPurchaseLimitLog;
							int maxByLimit = Math.Max(0, rule.MonthlyBuyLimit - MapGet(itemPurchaseLimitLog, rule.Type));
							return Math.Min(maxByLimit, missing);
						}

						return missing;
					}
					else
					{
						// not found = not missing
						return -1;
					}
				}
				break;
		}

		return -1;
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
		string selectedCategory = _cats[selCategory];
		bool categoryFilterEnabled = (selectedCategory != "STR_ALL_ITEMS");
		bool categoryUnassigned = (selectedCategory == "STR_UNASSIGNED");
		bool categoryHidden = (selectedCategory == "STR_FILTER_HIDDEN");
		bool categoryEquipped = (selectedCategory == "STR_FILTER_EQUIPPED");
		bool categoryMissing = (selectedCategory == "STR_FILTER_MISSING");

		for (int i = 0; i < _items.Count; ++i)
		{
			// filter
			if (categoryMissing)
			{
				int missingQty = GetMissingQty(i);
				if (missingQty > -1)
				{
					if (!_autoBuyDone)
					{
						RuleItem rule = (RuleItem)_items[i].rule;
						if (rule.IsAlien)
						{
							// don't buy automatically
						}
						else
						{
							_items[i].amount += missingQty; // buy automatically

							_iQty += missingQty * rule.Size; // update total size
							_total += missingQty * _items[i].cost;  // update total cost
						}
					}
				}
				else
				{
					continue;
				}
			}
			else if (categoryEquipped)
			{
				// Note: showing also hidden items (if they are equipped)
				if (!IsEquipped(i))
				{
					continue;
				}
			}
			else
			{
				bool hidden = IsHidden(i);
				if (categoryHidden)
				{
					if (!hidden)
					{
						continue;
					}
				}
				else if (hidden)
				{
					continue;
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
			_lstItems.AddRow(4, name, Unicode.FormatFunding(_items[i].cost), Str(_items[i].qtySrc), Str(_items[i].amount));
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
	/// Purchases the selected items.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		if (_missingItemsMap.Count != 0)
		{
			string errorMessage = "";
			if (_total > _game.SavedGame.Funds)
			{
				errorMessage = Tr("STR_NOT_ENOUGH_MONEY");
			}
			if (_base.StoresOverfull(_iQty))
			{
				errorMessage = Tr("STR_NOT_ENOUGH_STORE_SPACE");
			}
			if (errorMessage.Length != 0)
			{
				RuleInterface menuInterface = _game.Mod.GetInterface("buyMenu");
				_game.PushState(new ErrorMessageState(errorMessage, _palette, (byte)menuInterface.GetElement("errorMessage").color, "BACK13.SCR", menuInterface.GetElement("errorPalette").color));
				return;
			}
		}

		_game.SavedGame.Funds = _game.SavedGame.Funds - _total;
		foreach (var transferRow in _items)
		{
			if (transferRow.amount > 0)
			{
				Transfer t;
				switch (transferRow.type)
				{
					case TRANSFER_SOLDIER:
						for (int s = 0; s < transferRow.amount; s++)
						{
							RuleSoldier rule = (RuleSoldier)transferRow.rule;
							if (rule.MonthlyBuyLimit > 0)
							{
								// remember the hire for the limit check
								var soldierHireLimitLog = _game.SavedGame.MonthlyPurchaseLimitLog;
								soldierHireLimitLog[rule.Type] = MapGet(soldierHireLimitLog, rule.Type) + 1;
							}
							int time = rule.TransferTime;
							if (time == 0)
								time = _game.Mod.PersonnelTime;
							t = new Transfer(time);
							int nationality = _game.SavedGame.SelectSoldierNationalityByLocation(_game.Mod, rule, _base);
							Soldier soldier = _game.Mod.GenSoldier(_game.SavedGame, rule, nationality);
							if (!string.IsNullOrEmpty(rule.SpawnedSoldierTemplate))
							{
								var reader = new YamlRootNodeReader(rule.SpawnedSoldierTemplate, "(spawned soldier template)");
								int nationalityOrig = soldier.Nationality;
								soldier.Load(reader.ToBase(), _game.Mod, _game.SavedGame, true); // load from soldier template
								if (soldier.Nationality != nationalityOrig)
								{
									soldier.GenName();
								}
							}
							t.Soldier = soldier;
							_base.Transfers.Add(t);
						}
						break;
					case TRANSFER_SCIENTIST:
						t = new Transfer(_game.Mod.PersonnelTime);
						t.SetScientists(transferRow.amount);
						_base.Transfers.Add(t);
						break;
					case TRANSFER_ENGINEER:
						t = new Transfer(_game.Mod.PersonnelTime);
						t.SetEngineers(transferRow.amount);
						_base.Transfers.Add(t);
						break;
					case TRANSFER_CRAFT:
						for (int c = 0; c < transferRow.amount; c++)
						{
							RuleCraft rule = (RuleCraft)transferRow.rule;
							if (rule.MonthlyBuyLimit > 0)
							{
								// remember the purchase for the limit check
								var craftPurchaseLimitLog = _game.SavedGame.MonthlyPurchaseLimitLog;
								craftPurchaseLimitLog[rule.Type] = MapGet(craftPurchaseLimitLog, rule.Type) + 1;
							}
							t = new Transfer(rule.TransferTime);
							Craft craft = new Craft(rule, _base, _game.SavedGame.GetId(rule.Type));
							craft.InitFixedWeapons(_game.Mod);
							craft.Status = "STR_REFUELLING";
							t.Craft = craft;
							_base.Transfers.Add(t);
						}
						break;
					case TRANSFER_ITEM:
						{
							RuleItem rule = (RuleItem)transferRow.rule;
							if (rule.MonthlyBuyLimit > 0)
							{
								// remember the purchase for the limit check
								var itemPurchaseLimitLog = _game.SavedGame.MonthlyPurchaseLimitLog;
								itemPurchaseLimitLog[rule.Type] = MapGet(itemPurchaseLimitLog, rule.Type) + transferRow.amount;
							}
							t = new Transfer(rule.TransferTime);
							t.SetItems(rule, transferRow.amount);
							_base.Transfers.Add(t);
							if (_parent != null && _missingItemsMap.Count != 0 && _missingItemsMap.ContainsKey(rule))
							{
								// remember the decreased amount for next buy
								_parent.DecreaseMissingItemCount(rule, transferRow.amount);
							}
						}
						break;
				}
			}
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
	/// Increases the item by one on left-click,
	/// to max on right-click.
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
	/// Decreases the item by one on left-click,
	/// to 0 on right-click.
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
		else if (_game.IsMiddleClick(action, true))
		{
			if (_game.IsCtrlPressed())
			{
				// C++ casts any rule to RuleItem* here and reads its type; all row rules have a type
				string ruleType = RuleType(GetRow().rule);
				if (ruleType != null)
				{
					RuleResearch selectedTopic = _game.Mod.GetResearch(ruleType);
					if (selectedTopic != null)
					{
						_game.PushState(new TechTreeViewerState(selectedTopic, null, null, null));
					}
				}
			}
			else if (GetRow().type == TRANSFER_ITEM)
			{
				if (GetRow().rule is RuleItem rule)
				{
					string articleId = rule.UfopediaType;
					Ufopaedia.OpenArticle(_game, articleId);
				}
			}
			else if (GetRow().type == TRANSFER_CRAFT)
			{
				if (GetRow().rule is RuleCraft rule)
				{
					string articleId = rule.Type;
					Ufopaedia.OpenArticle(_game, articleId);
				}
			}
		}
		else if (_game.IsRightClick(action, true))
		{
			if (action.AbsoluteXMouse >= _lstItems.ArrowsLeftEdge &&
				action.AbsoluteXMouse <= _lstItems.ArrowsRightEdge)
			{
				return;
			}
			string itemName = "";
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
						itemName = rule.Type;
					}
				}
			}
			else if (GetRow().type == TRANSFER_CRAFT)
			{
				if (GetRow().rule is RuleCraft rule)
				{
					itemName = rule.Type;
				}
			}
			if (itemName.Length != 0)
			{
				var hiddenMap = _game.SavedGame.HiddenPurchaseItems;
				if (hiddenMap.TryGetValue(itemName, out bool hidden))
				{
					// found => flip it
					_game.SavedGame.SetHiddenPurchaseItemsStatus(itemName, !hidden);
				}
				else
				{
					// not found = not hidden yet => hide it
					_game.SavedGame.SetHiddenPurchaseItemsStatus(itemName, true);
				}

				// update screen
				int scrollPos = _lstItems.Scroll;
				UpdateList();
				_lstItems.ScrollTo(scrollPos);
			}
		}
	}

	/// <summary>Type name of a row rule (item, craft or soldier), null for personnel rows.</summary>
	internal static string RuleType(object rule) => rule switch
	{
		RuleItem r => r.Type,
		RuleCraft r => r.Type,
		RuleSoldier r => r.Type,
		_ => null,
	};

	/// <summary>
	/// Increases the quantity of the selected item to buy by one.
	/// </summary>
	public void Increase()
	{
		_timerDec.SetInterval(50);
		_timerInc.SetInterval(50);
		IncreaseByValue(_game.ScrollStep);
	}

	/// <summary>
	/// Increases the quantity of the selected item to buy by "change".
	/// </summary>
	/// <param name="change">How much we want to add.</param>
	public void IncreaseByValue(int change)
	{
		if (0 >= change) return;
		string errorMessage = "";

		if (_total + GetRow().cost > _game.SavedGame.Funds)
		{
			errorMessage = Tr("STR_NOT_ENOUGH_MONEY");
		}
		else
		{
			RuleItem rule;
			RuleSoldier ruleS;
			RuleCraft ruleC;
			switch (GetRow().type)
			{
				case TRANSFER_SOLDIER:
				case TRANSFER_SCIENTIST:
				case TRANSFER_ENGINEER:
					if (GetRow().type == TRANSFER_SOLDIER)
					{
						ruleS = (RuleSoldier)GetRow().rule;
						if (ruleS.MonthlyBuyLimit > 0)
						{
							var soldierHireLimitLog = _game.SavedGame.MonthlyPurchaseLimitLog;
							int alreadyBought = MapGet(soldierHireLimitLog, ruleS.Type);
							int maxByLimit = Math.Max(0, ruleS.MonthlyBuyLimit - alreadyBought - GetRow().amount);
							if (maxByLimit <= 0)
							{
								errorMessage = Tr(ruleS.MonthlyBuyLimitMessage.Length != 0 ? ruleS.MonthlyBuyLimitMessage : "STR_MONTHLY_SOLDIER_HIRING_LIMIT_EXCEEDED")
									.Arg(ruleS.MonthlyBuyLimit)                 // {0} limit
									.Arg(alreadyBought)                         // {1} already bought
									.Arg(ruleS.MonthlyBuyLimit - alreadyBought) // {2} remaining
									.Arg(Tr(ruleS.Type));                       // {3} soldier type
							}
						}
					}
					// fall-through
					if (_pQty + 1 > _base.AvailableQuarters - _base.UsedQuarters)
					{
						errorMessage = Tr("STR_NOT_ENOUGH_LIVING_SPACE");
					}
					break;
				case TRANSFER_CRAFT:
					ruleC = (RuleCraft)GetRow().rule;
					if (_cQty + 1 > _base.AvailableHangars - _base.UsedHangars)
					{
						errorMessage = Tr("STR_NO_FREE_HANGARS_FOR_PURCHASE");
					}
					else if (ruleC.MonthlyBuyLimit > 0)
					{
						var craftPurchaseLimitLog = _game.SavedGame.MonthlyPurchaseLimitLog;
						int alreadyBought = MapGet(craftPurchaseLimitLog, ruleC.Type);
						int maxByLimit = Math.Max(0, ruleC.MonthlyBuyLimit - alreadyBought - GetRow().amount);
						if (maxByLimit <= 0)
						{
							errorMessage = Tr(ruleC.MonthlyBuyLimitMessage.Length != 0 ? ruleC.MonthlyBuyLimitMessage : "STR_MONTHLY_CRAFT_PURCHASE_LIMIT_EXCEEDED")
								.Arg(ruleC.MonthlyBuyLimit)                 // {0} limit
								.Arg(alreadyBought)                         // {1} already bought
								.Arg(ruleC.MonthlyBuyLimit - alreadyBought) // {2} remaining
								.Arg(Tr(ruleC.Type));                       // {3} craft type
						}
					}
					break;
				case TRANSFER_ITEM:
					rule = (RuleItem)GetRow().rule;
					if (_base.StoresOverfull(_iQty + rule.Size))
					{
						errorMessage = Tr("STR_NOT_ENOUGH_STORE_SPACE");
					}
					else if (rule.IsAlien)
					{
						int p = rule.PrisonType;
						if (MapGet(_iPrisonQty, p) + 1 > _base.GetAvailableContainment(p) - _base.GetUsedContainment(p))
						{
							errorMessage = TrAlt("STR_NOT_ENOUGH_PRISON_SPACE", p);
						}
					}
					else if (rule.MonthlyBuyLimit > 0)
					{
						var itemPurchaseLimitLog = _game.SavedGame.MonthlyPurchaseLimitLog;
						int alreadyBought = MapGet(itemPurchaseLimitLog, rule.Type);
						int maxByLimit = Math.Max(0, rule.MonthlyBuyLimit - alreadyBought - GetRow().amount);
						if (maxByLimit <= 0)
						{
							errorMessage = Tr(rule.MonthlyBuyLimitMessage.Length != 0 ? rule.MonthlyBuyLimitMessage : "STR_MONTHLY_ITEM_PURCHASE_LIMIT_EXCEEDED")
								.Arg(rule.MonthlyBuyLimit)                 // {0} limit
								.Arg(alreadyBought)                        // {1} already bought
								.Arg(rule.MonthlyBuyLimit - alreadyBought) // {2} remaining
								.Arg(Tr(rule.Type));                       // {3} item type
						}
					}
					break;
			}
		}

		if (errorMessage.Length == 0)
		{
			// C++: int maxByMoney = (funds - _total) / cost; (int64 division truncated to int)
			int maxByMoney = (int)((_game.SavedGame.Funds - _total) / GetRow().cost);
			if (maxByMoney >= 0)
				change = Math.Min(maxByMoney, change);
			switch (GetRow().type)
			{
				case TRANSFER_SOLDIER:
				case TRANSFER_SCIENTIST:
				case TRANSFER_ENGINEER:
					if (GetRow().type == TRANSFER_SOLDIER)
					{
						RuleSoldier ruleS = (RuleSoldier)GetRow().rule;
						if (ruleS.MonthlyBuyLimit > 0)
						{
							var soldierHireLimitLog = _game.SavedGame.MonthlyPurchaseLimitLog;
							int maxByLimit = Math.Max(0, ruleS.MonthlyBuyLimit - MapGet(soldierHireLimitLog, ruleS.Type) - GetRow().amount);
							change = Math.Min(maxByLimit, change);
						}
					}
					// fall-through
					{
						int maxByQuarters = _base.AvailableQuarters - _base.UsedQuarters - _pQty;
						change = Math.Min(maxByQuarters, change);
						_pQty += change;
					}
					break;
				case TRANSFER_CRAFT:
					{
						RuleCraft ruleC = (RuleCraft)GetRow().rule;
						if (ruleC.MonthlyBuyLimit > 0)
						{
							var craftPurchaseLimitLog = _game.SavedGame.MonthlyPurchaseLimitLog;
							int maxByLimit = Math.Max(0, ruleC.MonthlyBuyLimit - MapGet(craftPurchaseLimitLog, ruleC.Type) - GetRow().amount);
							change = Math.Min(maxByLimit, change);
						}
						int maxByHangars = _base.AvailableHangars - _base.UsedHangars - _cQty;
						change = Math.Min(maxByHangars, change);
						_cQty += change;
					}
					break;
				case TRANSFER_ITEM:
					{
						RuleItem rule = (RuleItem)GetRow().rule;
						if (rule.MonthlyBuyLimit > 0)
						{
							var itemPurchaseLimitLog = _game.SavedGame.MonthlyPurchaseLimitLog;
							int maxByLimit = Math.Max(0, rule.MonthlyBuyLimit - MapGet(itemPurchaseLimitLog, rule.Type) - GetRow().amount);
							change = Math.Min(maxByLimit, change);
						}
						int p = rule.PrisonType;
						if (rule.IsAlien)
						{
							int maxByPrisons = _base.GetAvailableContainment(p) - _base.GetUsedContainment(p) - MapGet(_iPrisonQty, p);
							change = Math.Min(maxByPrisons, change);
						}
						// both aliens and items
						{
							double storesNeededPerItem = rule.Size;
							double freeStores = _base.AvailableStores - _base.GetUsedStores() - _iQty;
							change = Math.Min(MaxByStores(freeStores, storesNeededPerItem), change);
							_iQty += change * storesNeededPerItem;
						}
						if (rule.IsAlien)
						{
							_iPrisonQty[p] = MapGet(_iPrisonQty, p) + change;
						}
					}
					break;
			}
			GetRow().amount += change;
			_total += GetRow().cost * change;
			UpdateItemStrings();
		}
		else
		{
			_timerInc.Stop();
			RuleInterface menuInterface = _game.Mod.GetInterface("buyMenu");
			_game.PushState(new ErrorMessageState(
				errorMessage,
				_palette,
				(byte)menuInterface.GetElement("errorMessage").color,
				"BACK13.SCR",
				menuInterface.GetElement("errorPalette").color,
				(byte)menuInterface.GetElement("errorMessage").color2
			));
		}
	}

	/// <summary>
	/// Decreases the quantity of the selected item to buy by one.
	/// </summary>
	public void Decrease()
	{
		_timerInc.SetInterval(50);
		_timerDec.SetInterval(50);
		DecreaseByValue(_game.ScrollStep);
	}

	/// <summary>
	/// Decreases the quantity of the selected item to buy by "change".
	/// </summary>
	/// <param name="change">How much we want to remove.</param>
	public void DecreaseByValue(int change)
	{
		if (0 >= change || 0 >= GetRow().amount) return;
		change = Math.Min(GetRow().amount, change);

		RuleItem rule;
		switch (GetRow().type)
		{
			case TRANSFER_SOLDIER:
			case TRANSFER_SCIENTIST:
			case TRANSFER_ENGINEER:
				_pQty -= change;
				break;
			case TRANSFER_CRAFT:
				_cQty -= change;
				break;
			case TRANSFER_ITEM:
				rule = (RuleItem)GetRow().rule;
				_iQty -= rule.Size * change;
				if (rule.IsAlien)
				{
					_iPrisonQty[rule.PrisonType] = MapGet(_iPrisonQty, rule.PrisonType) - change;
				}
				break;
		}
		GetRow().amount -= change;
		_total -= GetRow().cost * change;
		UpdateItemStrings();
	}

	/// <summary>
	/// Updates the quantity-strings of the selected item.
	/// </summary>
	public void UpdateItemStrings()
	{
		_txtPurchases.SetText(Tr("STR_COST_OF_PURCHASES").Arg(Unicode.FormatFunding(_total)));
		_lstItems.SetCellText(_sel, 3, Str(GetRow().amount));
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
		_txtSpaceUsed.SetText(Tr("STR_SPACE_USED").Arg(FormatSpaceUsed(_base.GetUsedStores(), _iQty, _base.AvailableStores)));
	}

	/// <summary>
	/// Updates the production list to match the category filter.
	/// </summary>
	public void CbxCategoryChange(Action action)
	{
		UpdateList();
	}
}
