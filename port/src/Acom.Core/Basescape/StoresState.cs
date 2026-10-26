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
// Ported from: src/Basescape/StoresState.h, src/Basescape/StoresState.cpp
#nullable disable

using static Acom.ItemSort;
using static Acom.SDLKey;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>Item sorting modes.</summary>
public enum ItemSort : uint
{
	ITEM_SORT_NONE = 0,
	ITEM_SORT_NAME_ASC = 1,
	ITEM_SORT_NAME_DESC = 2,
	ITEM_SORT_QUANTITY_ASC = 3,
	ITEM_SORT_QUANTITY_DESC = 4,
	ITEM_SORT_SIZE_ASC = 5,
	ITEM_SORT_SIZE_DESC = 6,
	ITEM_SORT_SPACE_USED_ASC = 7,
	ITEM_SORT_SPACE_USED_DESC = 8,
}

/// <summary>One row of the stores list (C++ struct).</summary>
public class StoredItem
{
	public RuleItem rule;
	public string name;
	public int quantity;
	public double size;
	public double spaceUsed;

	public StoredItem(RuleItem rule, string name, int quantity, double size, double spaceUsed)
	{
		this.rule = rule;
		this.name = name;
		this.quantity = quantity;
		this.size = size;
		this.spaceUsed = spaceUsed;
	}
}

/// <summary>
/// Stores window that displays all
/// the items currently stored in a base.
/// </summary>
public class StoresState : State
{
	private readonly Base _base;

	private readonly TextButton _btnOk;
	private readonly TextEdit _btnQuickSearch;
	private readonly ToggleTextButton _btnGrandTotal;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtItem, _txtQuantity, _txtSize, _txtSpaceUsed;
	private readonly TextList _lstStores;
	private readonly ArrowButton _sortName, _sortQuantity, _sortSize, _sortSpaceUsed;

	private readonly List<StoredItem> _itemList = [];
	private ItemSort _itemOrder;

	/// <summary>
	/// Initializes all the elements in the Stores window.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	public StoresState(Base @base)
	{
		_base = @base;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnQuickSearch = new TextEdit(this, 48, 9, 10, 20);
		_btnOk = new TextButton(148, 16, 164, 176);
		_btnGrandTotal = new ToggleTextButton(148, 16, 8, 176);
		_txtTitle = new Text(310, 17, 5, 8);
		_txtItem = new Text(142, 9, 10, 32);
		_txtQuantity = new Text(54, 9, 152, 32);
		_txtSize = new Text(54, 9, 212, 32);
		_txtSpaceUsed = new Text(54, 9, 248, 32);
		_lstStores = new TextList(288, 128, 8, 40);
		_sortName = new ArrowButton(ArrowShape.ARROW_NONE, 11, 8, 10, 32);
		_sortQuantity = new ArrowButton(ArrowShape.ARROW_NONE, 11, 8, 152, 32);
		_sortSize = new ArrowButton(ArrowShape.ARROW_NONE, 11, 8, 212, 32);
		_sortSpaceUsed = new ArrowButton(ArrowShape.ARROW_NONE, 11, 8, 248, 32);

		// Set palette
		SetInterface("storesInfo");

		Add(_window, "window", "storesInfo");
		Add(_btnQuickSearch, "button", "storesInfo");
		Add(_btnOk, "button", "storesInfo");
		Add(_btnGrandTotal, "button", "storesInfo");
		Add(_txtTitle, "text", "storesInfo");
		Add(_txtItem, "text", "storesInfo");
		Add(_txtQuantity, "text", "storesInfo");
		Add(_txtSize, "text", "storesInfo");
		Add(_txtSpaceUsed, "text", "storesInfo");
		Add(_lstStores, "list", "storesInfo");
		Add(_sortName, "text", "storesInfo");
		Add(_sortQuantity, "text", "storesInfo");
		Add(_sortSize, "text", "storesInfo");
		Add(_sortSpaceUsed, "text", "storesInfo");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "storesInfo");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_btnGrandTotal.Text = Tr("STR_GRAND_TOTAL");
		_btnGrandTotal.OnMouseClick(BtnGrandTotalClick);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_STORES"));

		_txtItem.SetText(Tr("STR_ITEM"));

		_txtQuantity.SetText(Tr("STR_QUANTITY_UC"));
		_txtSize.SetText(Tr("STR_SIZE_UC"));
		_txtSpaceUsed.SetText(Tr("STR_SPACE_USED_UC"));

		_lstStores.SetColumns(4, 162, 40, 50, 34);
		_lstStores.SetSelectable(true);
		_lstStores.SetBackground(_window);
		_lstStores.Margin = 2;
		_lstStores.OnMouseClick(LstStoresClick, SDLMouse.SDL_BUTTON_LEFT);
		_lstStores.OnMouseClick(LstStoresClick, SDLMouse.SDL_BUTTON_MIDDLE);

		_sortName.X = _sortName.X + _txtItem.GetTextWidth() + 4;
		_sortName.OnMouseClick(SortNameClick);

		_sortQuantity.X = _sortQuantity.X + _txtQuantity.GetTextWidth() + 4;
		_sortQuantity.OnMouseClick(SortQuantityClick);

		_sortSize.X = _sortSize.X + _txtSize.GetTextWidth() + 4;
		_sortSize.OnMouseClick(SortSizeClick);

		_sortSpaceUsed.X = _sortSpaceUsed.X + _txtSpaceUsed.GetTextWidth() + 4;
		_sortSpaceUsed.OnMouseClick(SortSpaceUsedClick);

		_itemOrder = ITEM_SORT_NONE;

		_btnQuickSearch.Text = ""; // redraw
		_btnQuickSearch.OnChange(BtnQuickSearchApply);
		_btnQuickSearch.Visible = Options.oxceQuickSearchButton;

		_btnOk.OnKeyboardRelease(BtnQuickSearchToggle, Options.keyToggleQuickSearch);
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
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
		InitList();
	}

	/// <summary>
	/// Counts an item: in this base's stores only, or (grand total) everywhere in all bases
	/// (stores, base defenses, craft, worn armor, research, transfers).
	/// </summary>
	internal static int CountItem(SavedGame save, Base @base, RuleItem rule, bool grandTotal)
	{
		int qty = 0;
		if (!grandTotal)
		{
			// items in stores from this base only
			qty += @base.StorageItems.GetItem(rule);
		}
		else
		{
			// items from all bases
			foreach (var xbase in save.Bases)
			{
				// 1. items in base stores
				qty += xbase.StorageItems.GetItem(rule);

				// 1b. items from base defense facilities
				foreach (var facility in xbase.Facilities)
				{
					if (facility.Rules.AmmoMax > 0 && facility.Rules.AmmoItem == rule)
					{
						qty += facility.Ammo;
					}
				}

				// 2. items from craft
				foreach (var craft in xbase.Crafts)
				{
					qty += craft.GetTotalItemCount(rule);
				}

				// 3. armor in use (worn by soldiers)
				foreach (var soldier in xbase.Soldiers)
				{
					if (soldier.Armor.StoreItem == rule)
					{
						qty += 1;
					}
				}

				// 4. items/aliens in research
				foreach (var research in xbase.Research)
				{
					var rrule = research.Rules;
					if (rrule.IsHoldingNeededItem)
					{
						if (rrule.NeededItem != null && rrule.NeededItem == rule)
						{
							qty += 1;
							break;
						}
					}
				}

				// 5. items in transfer
				foreach (var transfer in xbase.Transfers)
				{
					if (transfer.Craft != null)
					{
						// 5a. craft equipment, weapons, vehicles
						qty += transfer.Craft.GetTotalItemCount(rule);
					}
					else if (transfer.Soldier != null)
					{
						// 5c. armor in use (worn by soldiers)
						if (transfer.Soldier.Armor.StoreItem == rule)
						{
							qty += 1;
						}
					}
					else if (transfer.Items == rule)
					{
						// 5b. items in transfer
						qty += transfer.Quantity;
					}
				}
			}
		}
		return qty;
	}

	/// <summary>
	/// Reloads the item list.
	/// </summary>
	private void InitList()
	{
		bool grandTotal = _btnGrandTotal.Pressed;

		string searchString = Unicode.UpperCase(_btnQuickSearch.Text);

		// clear everything
		_itemList.Clear();

		// find relevant items
		foreach (var itemType in _game.Mod.ItemsList)
		{
			// quick search
			if (!string.IsNullOrEmpty(searchString))
			{
				string projectName = Unicode.UpperCase(Tr(itemType));
				if (!projectName.Contains(searchString, StringComparison.Ordinal))
				{
					continue;
				}
			}

			var rule = _game.Mod.GetItem(itemType, true);
			int qty = CountItem(_game.SavedGame, _base, rule, grandTotal);

			if (qty > 0)
			{
				_itemList.Add(new StoredItem(rule, Tr(itemType), qty, rule.Size, qty * rule.Size));
			}
		}

		SortList();
	}

	/// <summary>
	/// Refreshes the item list.
	/// </summary>
	public override void Init()
	{
		base.Init();

		InitList();
	}

	/// <summary>
	/// Includes items from all bases.
	/// </summary>
	public void BtnGrandTotalClick(Action action)
	{
		InitList();
	}

	/// <summary>
	/// Updates the sorting arrows based on the current setting.
	/// </summary>
	private void UpdateArrows()
	{
		_sortName.SetShape(ArrowShape.ARROW_NONE);
		_sortQuantity.SetShape(ArrowShape.ARROW_NONE);
		_sortSize.SetShape(ArrowShape.ARROW_NONE);
		_sortSpaceUsed.SetShape(ArrowShape.ARROW_NONE);
		switch (_itemOrder)
		{
			case ITEM_SORT_NONE:
				break;
			case ITEM_SORT_NAME_ASC:
				_sortName.SetShape(ArrowShape.ARROW_SMALL_UP);
				break;
			case ITEM_SORT_NAME_DESC:
				_sortName.SetShape(ArrowShape.ARROW_SMALL_DOWN);
				break;
			case ITEM_SORT_QUANTITY_ASC:
				_sortQuantity.SetShape(ArrowShape.ARROW_SMALL_UP);
				break;
			case ITEM_SORT_QUANTITY_DESC:
				_sortQuantity.SetShape(ArrowShape.ARROW_SMALL_DOWN);
				break;
			case ITEM_SORT_SIZE_ASC:
				_sortSize.SetShape(ArrowShape.ARROW_SMALL_UP);
				break;
			case ITEM_SORT_SIZE_DESC:
				_sortSize.SetShape(ArrowShape.ARROW_SMALL_DOWN);
				break;
			case ITEM_SORT_SPACE_USED_ASC:
				_sortSpaceUsed.SetShape(ArrowShape.ARROW_SMALL_UP);
				break;
			case ITEM_SORT_SPACE_USED_DESC:
				_sortSpaceUsed.SetShape(ArrowShape.ARROW_SMALL_DOWN);
				break;
			default:
				break;
		}
	}

	/// <summary>
	/// Sorts a stores list like the C++ <c>std::stable_sort</c> calls
	/// (descending orders sort the reversed range, which equals a stable descending sort).
	/// </summary>
	internal static void SortItems(List<StoredItem> items, ItemSort order)
	{
		IComparer<string> nameComparer = Comparer<string>.Create(Unicode.NaturalComparison);
		IEnumerable<StoredItem> sorted = order switch
		{
			ITEM_SORT_NAME_ASC => items.OrderBy(i => i.name, nameComparer),
			ITEM_SORT_NAME_DESC => items.OrderByDescending(i => i.name, nameComparer),
			ITEM_SORT_QUANTITY_ASC => items.OrderBy(i => i.quantity),
			ITEM_SORT_QUANTITY_DESC => items.OrderByDescending(i => i.quantity),
			ITEM_SORT_SIZE_ASC => items.OrderBy(i => i.size),
			ITEM_SORT_SIZE_DESC => items.OrderByDescending(i => i.size),
			ITEM_SORT_SPACE_USED_ASC => items.OrderBy(i => i.spaceUsed),
			ITEM_SORT_SPACE_USED_DESC => items.OrderByDescending(i => i.spaceUsed),
			_ => null,
		};
		if (sorted == null)
			return;
		var list = sorted.ToList(); // LINQ ordering is stable
		items.Clear();
		items.AddRange(list);
	}

	/// <summary>
	/// Sorts the item list.
	/// </summary>
	public void SortList()
	{
		UpdateArrows();

		SortItems(_itemList, _itemOrder);

		UpdateList();
	}

	/// <summary>
	/// Updates the item list with the current list of available items.
	/// </summary>
	public virtual void UpdateList()
	{
		_lstStores.ClearList();

		foreach (var item in _itemList)
		{
			_lstStores.AddRow(4, item.name, item.quantity.ToString(System.Globalization.CultureInfo.InvariantCulture), LocalizedText.FormatLikeOstream(item.size), LocalizedText.FormatLikeOstream(item.spaceUsed));
		}
	}

	/// <summary>
	/// Sorts the items by name.
	/// </summary>
	public void SortNameClick(Action action)
	{
		_itemOrder = _itemOrder == ITEM_SORT_NAME_ASC ? ITEM_SORT_NAME_DESC : ITEM_SORT_NAME_ASC;
		SortList();
	}

	/// <summary>
	/// Sorts the items by quantity.
	/// </summary>
	public void SortQuantityClick(Action action)
	{
		_itemOrder = _itemOrder == ITEM_SORT_QUANTITY_ASC ? ITEM_SORT_QUANTITY_DESC : ITEM_SORT_QUANTITY_ASC;
		SortList();
	}

	/// <summary>
	/// Sorts the items by size.
	/// </summary>
	public void SortSizeClick(Action action)
	{
		_itemOrder = _itemOrder == ITEM_SORT_SIZE_ASC ? ITEM_SORT_SIZE_DESC : ITEM_SORT_SIZE_ASC;
		SortList();
	}

	/// <summary>
	/// Sorts the items by space used.
	/// </summary>
	public void SortSpaceUsedClick(Action action)
	{
		_itemOrder = _itemOrder == ITEM_SORT_SPACE_USED_ASC ? ITEM_SORT_SPACE_USED_DESC : ITEM_SORT_SPACE_USED_ASC;
		SortList();
	}

	/// <summary>
	/// Handles mouse clicks.
	/// </summary>
	public void LstStoresClick(Action action)
	{
		if (_game.IsMiddleClick(action))
		{
			var rule = _itemList[(int)_lstStores.SelectedRow].rule;

			string articleId = rule.UfopediaType;
			Ufopaedia.OpenArticle(_game, articleId);
		}
		else if (_game.IsLeftClick(action))
		{
			var rule = _itemList[(int)_lstStores.SelectedRow].rule;

			_game.PushState(new ItemLocationsState(rule));
		}
	}
}
