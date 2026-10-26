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
// Ported from: src/Basescape/MonthlyCostsState.h, src/Basescape/MonthlyCostsState.cpp
// Ported from: src/Basescape/CraftEquipmentState.h, src/Basescape/CraftEquipmentState.cpp
#nullable disable

using System.Globalization;
using static Acom.SDLKey;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Equipment screen that lets the player
/// pick the equipment to carry on a craft.
/// </summary>
public class CraftEquipmentState : TouchState
{
	private readonly TextButton _btnOk, _btnClear, _btnInventory;
	private readonly TextEdit _btnQuickSearch;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtItem, _txtStores, _txtAvailable, _txtUsed, _txtCrew;
	private readonly List<string> _categoryStrings = [];
	private readonly SortedDictionary<string, bool> _usedCategoryStrings = new(StringComparer.Ordinal);
	private readonly ComboBox _cbxFilterBy;
	private readonly TextList _lstEquipment;
	private int _lstScroll;
	private readonly Timer _timerLeft, _timerRight;
	private int _sel;
	private readonly int _craft;
	private readonly Base _base;
	private readonly List<string> _items = [];
	private int _totalItems;
	private double _totalItemStorageSize;
	private readonly byte _ammoColor;
	private bool _reload;
	private bool _returningFromGlobalTemplates;
	private bool _returningFromInventory;
	private bool _firstInit;
	private readonly bool _isNewBattle;

	private static string Str(int value) => value.ToString(CultureInfo.InvariantCulture);

	/// <summary>
	/// Initializes all the elements in the Craft Equipment screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="craft">ID of the selected craft.</param>
	public CraftEquipmentState(Base @base, int craft)
	{
		_lstScroll = 0;
		_sel = 0;
		_craft = craft;
		_base = @base;
		_totalItems = 0;
		_totalItemStorageSize = 0.0;
		_ammoColor = 0;
		_reload = true;
		_returningFromGlobalTemplates = false;
		_returningFromInventory = false;
		_firstInit = true;
		_isNewBattle = false;

		Craft c = _base.Crafts[_craft];
		bool craftHasACrew = c.NumTotalSoldiers > 0;
		_isNewBattle = _game.SavedGame.MonthsPassed == -1;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnQuickSearch = new TextEdit(this, 48, 9, Options.oxceBaseTouchButtons ? 10 : 264, Options.oxceBaseTouchButtons ? 13 : 12);
		_btnOk = new TextButton((craftHasACrew || _isNewBattle) ? 30 : 140, 16, (craftHasACrew || _isNewBattle) ? 274 : 164, 176);
		_btnClear = new TextButton(102, 16, 164, 176);
		_btnInventory = new TextButton(102, 16, 164, 176);
		_txtTitle = new Text(300, 17, 16, 7);
		_txtItem = new Text(144, 9, 16, 32);
		_txtStores = new Text(150, 9, 160, 32);
		_txtAvailable = new Text(110, 9, 16, 24);
		_txtUsed = new Text(110, 9, 130, 24);
		_txtCrew = new Text(71, 9, 244, 24);
		_lstEquipment = new TextList(288, 128, 8, 40);
		_cbxFilterBy = new ComboBox(this, 140, 16, 16, 176, true);

		TouchComponentsCreate(_txtTitle);

		// Set palette
		SetInterface("craftEquipment");

		_ammoColor = (byte)_game.Mod.GetInterface("craftEquipment").GetElement("ammoColor").color;

		Add(_window, "window", "craftEquipment");
		Add(_btnQuickSearch, "button", "craftEquipment");
		Add(_btnOk, "button", "craftEquipment");
		Add(_btnClear, "button", "craftEquipment");
		Add(_btnInventory, "button", "craftEquipment");
		Add(_txtTitle, "text", "craftEquipment");
		Add(_txtItem, "text", "craftEquipment");
		Add(_txtStores, "text", "craftEquipment");
		Add(_txtAvailable, "text", "craftEquipment");
		Add(_txtUsed, "text", "craftEquipment");
		Add(_txtCrew, "text", "craftEquipment");
		Add(_lstEquipment, "list", "craftEquipment");
		Add(_cbxFilterBy, "button", "craftEquipment");

		TouchComponentsAdd("button2", "craftEquipment", _window);

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "craftEquipment");

		TouchComponentsConfigure();

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnOk.OnKeyboardPress(BtnClearClick, Options.keyRemoveEquipmentFromCraft);
		_btnOk.OnKeyboardPress(BtnLoadClick, Options.keyCraftLoadoutLoad);
		_btnOk.OnKeyboardPress(BtnSaveClick, Options.keyCraftLoadoutSave);

		_btnClear.Text = Tr("STR_UNLOAD_CRAFT");
		_btnClear.OnMouseClick(BtnClearClick);
		_btnClear.Visible = _isNewBattle;

		_btnInventory.Text = Tr("STR_INVENTORY");
		_btnInventory.OnMouseClick(BtnInventoryClick);
		_btnInventory.Visible = craftHasACrew && !_isNewBattle;
		_btnInventory.OnKeyboardPress(BtnInventoryClick, Options.keyBattleInventory);

		_txtTitle.SetBig();
		if (Options.oxceBaseTouchButtons)
		{
			_txtTitle.Align = TextHAlign.ALIGN_CENTER;
			_txtTitle.SetText(c.GetName(_game.Language));
		}
		else
		{
			_txtTitle.Align = TextHAlign.ALIGN_LEFT;
			_txtTitle.SetText(Tr("STR_EQUIPMENT_FOR_CRAFT").Arg(c.GetName(_game.Language)));
		}

		_txtItem.SetText(Tr("STR_ITEM"));

		_txtStores.SetText(Tr("STR_STORES"));

		_txtAvailable.SetText(Tr("STR_SPACE_AVAILABLE").Arg(c.SpaceAvailable));

		_txtUsed.SetText(Tr("STR_SPACE_USED").Arg(c.SpaceUsed));

		_txtCrew.SetText(Tr("STR_SOLDIERS_UC") + ">" + Unicode.TOK_COLOR_FLIP + Str(c.NumTotalSoldiers));

		// populate sort options
		_categoryStrings.Add("STR_ALL");
		_categoryStrings.Add("STR_EQUIPPED");
		bool hasUnassigned = false;
		foreach (var itemType in _game.Mod.ItemsList)
		{
			RuleItem rule = _game.Mod.GetItem(itemType);
			Unit isVehicle = rule.VehicleUnit;
			int cQty = isVehicle != null ? c.GetVehicleCount(itemType) : c.Items.GetItem(rule);

			if ((isVehicle != null || rule.IsInventoryItem) && rule.CanBeEquippedToCraftInventory() &&
				_game.SavedGame.IsResearched(rule.Requirements) &&
				(_base.StorageItems.GetItem(rule) > 0 || cQty > 0))
			{
				if (rule.Categories.Count == 0)
				{
					hasUnassigned = true;
				}
				else
				{
					foreach (var itemCategoryName in rule.Categories)
					{
						_usedCategoryStrings[itemCategoryName] = true;
					}
				}
			}
		}
		var itemCategories = _game.Mod.ItemCategoriesList;
		foreach (var categoryName in itemCategories)
		{
			if (PurchaseState.MapGetBool(_usedCategoryStrings, categoryName))
			{
				if (!_game.Mod.GetItemCategory(categoryName).IsHidden)
				{
					_categoryStrings.Add(categoryName);
				}
			}
		}
		if (hasUnassigned && itemCategories.Count != 0)
		{
			_categoryStrings.Add("STR_UNASSIGNED");
		}
		_categoryStrings.Add("STR_NOT_EQUIPPED");

		_cbxFilterBy.SetOptions(_categoryStrings, true);
		_cbxFilterBy.Selected = 0;
		_cbxFilterBy.OnChange(CbxFilterByChange);

		_lstEquipment.SetArrowColumn(203, ArrowOrientation.ARROW_HORIZONTAL);
		_lstEquipment.SetColumns(3, 156, 83, 41);
		_lstEquipment.SetSelectable(true);
		_lstEquipment.SetBackground(_window);
		_lstEquipment.Margin = 8;
		_lstEquipment.OnLeftArrowPress(LstEquipmentLeftArrowPress);
		_lstEquipment.OnLeftArrowRelease(LstEquipmentLeftArrowRelease);
		_lstEquipment.OnLeftArrowClick(LstEquipmentLeftArrowClick);
		_lstEquipment.OnRightArrowPress(LstEquipmentRightArrowPress);
		_lstEquipment.OnRightArrowRelease(LstEquipmentRightArrowRelease);
		_lstEquipment.OnRightArrowClick(LstEquipmentRightArrowClick);
		_lstEquipment.OnMousePress(LstEquipmentMousePress);

		_btnQuickSearch.Text = ""; // redraw
		_btnQuickSearch.OnChange(BtnQuickSearchApply);
		_btnQuickSearch.Visible = Options.oxceQuickSearchButton;

		_btnOk.OnKeyboardRelease(BtnQuickSearchToggle, Options.keyToggleQuickSearch);

		_timerLeft = new Timer(250);
		_timerLeft.OnTimer((StateHandler)MoveLeft);
		_timerRight = new Timer(250);
		_timerRight.OnTimer((StateHandler)MoveRight);
	}

	/// <summary>
	/// Filters the equipment list by the selected criterion.
	/// </summary>
	public void CbxFilterByChange(Action action)
	{
		InitList();
	}

	/// <summary>
	/// Resets the savegame when coming back from the inventory.
	/// </summary>
	public override void Init()
	{
		base.Init();

		_game.SavedGame.SetBattleGame(null);

		Craft c = _base.Crafts[_craft];
		c.SetInBattlescape(false);

		// don't reload after closing error popups
		if (_reload)
		{
			if (Options.oxceAlternateCraftEquipmentManagement && !_isNewBattle)
			{
				// skip when returning from craft equipment template load/save
				if (!_returningFromGlobalTemplates)
				{
					c.CalculateTotalSoldierEquipment();
				}
				if (_returningFromInventory)
				{
					// now that we're back from the inventory screen, we need to remove all the excess base gear
					for (_sel = 0; _sel != _items.Count; ++_sel)
					{
						int excessQty = c.Items.GetItem(_items[_sel]) - (c.ExtraItems.GetItem(_items[_sel]) + c.SoldierItems.GetItem(_items[_sel]));
						MoveLeftByValue(excessQty);
					}
				}
			}
			InitList();
		}
		_reload = true;
		_returningFromGlobalTemplates = false;
		_returningFromInventory = false;
		_firstInit = false;

		TouchComponentsRefresh();
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
	/// Formats the craft quantity column: with alternate craft equipment management, items reserved
	/// by soldiers are shown separately ("reserved/+extra" or "quantity/missing").
	/// </summary>
	internal static string FormatCraftQuantity(int cQty, int reserved, bool isVehicle, bool alternateManagement)
	{
		if (alternateManagement)
		{
			if (isVehicle)
				return Str(cQty);
			else if (cQty - reserved > 0)
				return Str(reserved) + "/+" + Str(cQty - reserved);
			else if (cQty - reserved == 0)
				return Str(cQty);
			else
				return Str(cQty) + "/" + Str(cQty - reserved);
		}
		return Str(cQty);
	}

	/// <summary>
	/// Shows the equipment in a list filtered by selected criterion.
	/// </summary>
	private void InitList()
	{
		string searchString = Unicode.UpperCase(_btnQuickSearch.Text);

		int selIdx = _cbxFilterBy.Selected;
		if (selIdx == -1)
		{
			return;
		}
		string selectedCategory = _categoryStrings[selIdx];
		bool categoryFilterEnabled = (selectedCategory != "STR_ALL");
		bool categoryUnassigned = (selectedCategory == "STR_UNASSIGNED");
		bool categoryEquipped = (selectedCategory == "STR_EQUIPPED");
		bool categoryNotEquipped = (selectedCategory == "STR_NOT_EQUIPPED");
		bool shareAmmoCategories = _game.Mod.ShareAmmoCategories;

		Craft c = _base.Crafts[_craft];

		// reset
		_totalItems = 0;
		_totalItemStorageSize = 0.0;
		_items.Clear();
		_lstEquipment.ClearList();

		int row = 0;
		foreach (var itemType in _game.Mod.ItemsList)
		{
			RuleItem rule = _game.Mod.GetItem(itemType);

			Unit isVehicle = rule.VehicleUnit;
			int cQty = 0;
			if (isVehicle != null)
			{
				cQty = c.GetVehicleCount(itemType);
			}
			else
			{
				cQty = c.Items.GetItem(rule);
				_totalItems += cQty;
				_totalItemStorageSize += cQty * rule.Size;
			}

			int bQty = _base.StorageItems.GetItem(rule);
			int reserved = 0;
			if (Options.oxceAlternateCraftEquipmentManagement && !_isNewBattle)
			{
				reserved = c.SoldierItems.GetItem(rule);
			}
			if ((isVehicle != null || rule.IsInventoryItem) && rule.CanBeEquippedToCraftInventory() &&
				(bQty > 0 || cQty > 0 || reserved > 0))
			{
				// check research requirements
				if (!_game.SavedGame.IsResearched(rule.Requirements))
				{
					continue;
				}

				// filter by category
				if (categoryFilterEnabled)
				{
					if (categoryUnassigned)
					{
						if (rule.Categories.Count != 0)
						{
							continue;
						}
					}
					else if (categoryEquipped)
					{
						if (!(cQty > 0))
						{
							continue;
						}
					}
					else if (categoryNotEquipped)
					{
						if (cQty > 0)
						{
							continue;
						}
					}
					else
					{
						bool isOK = rule.BelongsToCategory(selectedCategory);
						if (shareAmmoCategories && !isOK && rule.BattleType == BattleType.BT_FIREARM)
						{
							foreach (var ammoRule in rule.PrimaryCompatibleAmmo)
							{
								if (_base.StorageItems.GetItem(ammoRule) > 0 || c.Items.GetItem(ammoRule) > 0)
								{
									if (ammoRule.IsInventoryItem && ammoRule.CanBeEquippedToCraftInventory() && _game.SavedGame.IsResearched(ammoRule.Requirements))
									{
										isOK = ammoRule.BelongsToCategory(selectedCategory);
										if (isOK) break;
									}
								}
							}
						}
						if (!isOK) continue;
					}
				}

				// quick search
				if (!string.IsNullOrEmpty(searchString))
				{
					string projectName = Unicode.UpperCase(Tr(itemType));
					if (!projectName.Contains(searchString, StringComparison.Ordinal))
					{
						continue;
					}
				}

				_items.Add(itemType);
				string ss, ss2;
				if (Options.oxceAlternateCraftEquipmentManagement && !_isNewBattle)
				{
					// doing this once (on opening the screen) is enough
					// and just to make sure, we must skip this when returning from craft equipment template load/save
					if (_firstInit && isVehicle == null && cQty < reserved && !_returningFromGlobalTemplates)
					{
						// try to automatically add more items (if possible)
						int itemsToAdd = Math.Min(bQty, reserved - cQty);
						if (itemsToAdd > 0)
						{
							_base.StorageItems.RemoveItem(rule, itemsToAdd);
							bQty -= itemsToAdd;
							c.Items.AddItem(rule, itemsToAdd);
							cQty += itemsToAdd;
							_totalItems += itemsToAdd;
							_totalItemStorageSize += itemsToAdd * rule.Size;
						}
					}
					ss2 = FormatCraftQuantity(cQty, reserved, isVehicle != null, true);
				}
				else
				{
					ss2 = Str(cQty);
				}
				if (!_isNewBattle)
				{
					ss = Str(bQty);
				}
				else
				{
					ss = "-";
				}

				string s = Tr(itemType);
				if (rule.BattleType == BattleType.BT_AMMO)
				{
					s = s.Insert(0, "  ");
				}
				_lstEquipment.AddRow(3, s, ss, ss2);

				byte color;
				if (cQty == 0)
				{
					if (rule.BattleType == BattleType.BT_AMMO)
					{
						color = _ammoColor;
					}
					else
					{
						color = _lstEquipment.Color;
					}
				}
				else
				{
					color = _lstEquipment.SecondaryColor;
				}
				_lstEquipment.SetRowColor(row, color);

				++row;
			}
		}

		_lstEquipment.Draw();
		if (_lstScroll > 0)
		{
			_lstEquipment.ScrollTo(_lstScroll);
			_lstScroll = 0;
		}
	}

	/// <summary>
	/// Runs the arrow timers.
	/// </summary>
	public override void Think()
	{
		base.Think();

		_timerLeft.Think(this, null);
		_timerRight.Think(this, null);
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Starts moving the item to the base.
	/// </summary>
	public void LstEquipmentLeftArrowPress(Action action)
	{
		_sel = (int)_lstEquipment.SelectedRow;
		if (_game.IsLeftClick(action, true) && !_timerLeft.IsRunning) _timerLeft.Start();
	}

	/// <summary>
	/// Stops moving the item to the base.
	/// </summary>
	public void LstEquipmentLeftArrowRelease(Action action)
	{
		if (_game.IsLeftClick(action, true))
		{
			_timerLeft.Stop();
		}
	}

	/// <summary>
	/// Moves all the items to the base on right-click.
	/// </summary>
	public void LstEquipmentLeftArrowClick(Action action)
	{
		if (_game.IsRightClick(action, true)) MoveLeftByValue(int.MaxValue);
		if (_game.IsLeftClick(action, true))
		{
			MoveLeftByValue(_game.ScrollStep);
			_timerRight.SetInterval(250);
			_timerLeft.SetInterval(250);
		}
	}

	/// <summary>
	/// Starts moving the item to the craft.
	/// </summary>
	public void LstEquipmentRightArrowPress(Action action)
	{
		_sel = (int)_lstEquipment.SelectedRow;
		if (_game.IsLeftClick(action, true) && !_timerRight.IsRunning) _timerRight.Start();
	}

	/// <summary>
	/// Stops moving the item to the craft.
	/// </summary>
	public void LstEquipmentRightArrowRelease(Action action)
	{
		if (_game.IsLeftClick(action, true))
		{
			_timerRight.Stop();
		}
	}

	/// <summary>
	/// Moves all the items (as much as possible) to the craft on right-click.
	/// </summary>
	public void LstEquipmentRightArrowClick(Action action)
	{
		if (_game.IsRightClick(action, true)) MoveRightByValue(int.MaxValue);
		if (_game.IsLeftClick(action, true))
		{
			MoveRightByValue(_game.ScrollStep);
			_timerRight.SetInterval(250);
			_timerLeft.SetInterval(250);
		}
	}

	/// <summary>
	/// Handles the mouse-wheels on the arrow-buttons.
	/// </summary>
	public void LstEquipmentMousePress(Action action)
	{
		_sel = (int)_lstEquipment.SelectedRow;
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELUP)
		{
			_timerRight.Stop();
			_timerLeft.Stop();
			if (action.AbsoluteXMouse >= _lstEquipment.ArrowsLeftEdge &&
				action.AbsoluteXMouse <= _lstEquipment.ArrowsRightEdge)
			{
				MoveRightByValue(Options.changeValueByMouseWheel);
			}
		}
		else if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELDOWN)
		{
			_timerRight.Stop();
			_timerLeft.Stop();
			if (action.AbsoluteXMouse >= _lstEquipment.ArrowsLeftEdge &&
				action.AbsoluteXMouse <= _lstEquipment.ArrowsRightEdge)
			{
				MoveLeftByValue(Options.changeValueByMouseWheel);
			}
		}
		else if (_game.IsMiddleClick(action, true))
		{
			_lstScroll = _lstEquipment.Scroll;
			RuleItem rule = _game.Mod.GetItem(_items[_sel]);
			string articleId = rule.UfopediaType;
			Ufopaedia.OpenArticle(_game, articleId);
		}
	}

	/// <summary>
	/// Updates the displayed quantities of the
	/// selected item on the list.
	/// </summary>
	private void UpdateQuantity()
	{
		Craft c = _base.Crafts[_craft];
		RuleItem item = _game.Mod.GetItem(_items[_sel], true);
		int cQty = 0;
		if (item.VehicleUnit != null)
		{
			cQty = c.GetVehicleCount(_items[_sel]);
		}
		else
		{
			cQty = c.Items.GetItem(item);
		}
		string ss, ss2;
		if (!_isNewBattle)
		{
			ss = Str(_base.StorageItems.GetItem(item));
		}
		else
		{
			ss = "-";
		}
		if (Options.oxceAlternateCraftEquipmentManagement && !_isNewBattle)
		{
			int reserved = c.SoldierItems.GetItem(item);
			ss2 = FormatCraftQuantity(cQty, reserved, item.VehicleUnit != null, true);
		}
		else
		{
			ss2 = Str(cQty);
		}

		byte color;
		if (cQty == 0)
		{
			if (item.BattleType == BattleType.BT_AMMO)
			{
				color = _ammoColor;
			}
			else
			{
				color = _lstEquipment.Color;
			}
		}
		else
		{
			color = _lstEquipment.SecondaryColor;
		}
		_lstEquipment.SetRowColor(_sel, color);
		_lstEquipment.SetCellText(_sel, 1, ss);
		_lstEquipment.SetCellText(_sel, 2, ss2);

		_txtAvailable.SetText(Tr("STR_SPACE_AVAILABLE").Arg(c.SpaceAvailable));
		_txtUsed.SetText(Tr("STR_SPACE_USED").Arg(c.SpaceUsed));
	}

	/// <summary>
	/// Moves the selected item to the base.
	/// </summary>
	public void MoveLeft()
	{
		_timerLeft.SetInterval(50);
		_timerRight.SetInterval(50);
		MoveLeftByValue(_game.ScrollStep);
	}

	/// <summary>
	/// Moves the given number of items (selected) to the base.
	/// </summary>
	/// <param name="change">Item difference.</param>
	public void MoveLeftByValue(int change)
	{
		Craft c = _base.Crafts[_craft];
		RuleItem item = _game.Mod.GetItem(_items[_sel], true);
		int cQty = 0;
		if (item.VehicleUnit != null) cQty = c.GetVehicleCount(_items[_sel]);
		else cQty = c.Items.GetItem(item);
		if (change <= 0 || cQty <= 0) return;
		if (Options.oxceAlternateCraftEquipmentManagement && !_isNewBattle)
		{
			int reserved = c.SoldierItems.GetItem(item);
			if (cQty - reserved > 0)
			{
				change = Math.Min(cQty - reserved, change);
			}
			else
			{
				//change = 0;
				return;
			}
		}
		else
		{
			change = Math.Min(cQty, change);
		}
		// Convert vehicle to item
		if (item.VehicleUnit != null)
		{
			if (item.VehicleClipAmmo != null)
			{
				// Calculate how much ammo needs to be added to the base.
				RuleItem ammo = item.VehicleClipAmmo;
				int ammoPerVehicle = item.VehicleClipsLoaded;

				// Put the vehicles and their ammo back as separate items.
				if (!_isNewBattle)
				{
					_base.StorageItems.AddItem(item, change);
					_base.StorageItems.AddItem(ammo, ammoPerVehicle * change);
				}
				// now delete the vehicles from the craft.
				Base.DeleteIf(c.Vehicles, change, v => v.Rules == item);
			}
			else
			{
				if (!_isNewBattle)
				{
					_base.StorageItems.AddItem(item, change);
				}
				Base.DeleteIf(c.Vehicles, change, v => v.Rules == item);
			}
		}
		else
		{
			c.Items.RemoveItem(item, change);
			_totalItems -= change;
			_totalItemStorageSize -= change * item.Size;
			if (!_isNewBattle)
			{
				_base.StorageItems.AddItem(item, change);
			}
		}
		UpdateQuantity();
	}

	/// <summary>
	/// Moves the selected item to the craft.
	/// </summary>
	public void MoveRight()
	{
		_timerLeft.SetInterval(50);
		_timerRight.SetInterval(50);
		MoveRightByValue(_game.ScrollStep);
	}

	/// <summary>
	/// How many items fit into the remaining craft storage space (C++ floor of the free space by item size).
	/// </summary>
	internal static int MaxByCraftStorage(double maxStorageSpace, double totalItemStorageSize, double itemSize, int change)
	{
		if (totalItemStorageSize + (change * itemSize) > maxStorageSpace + 0.05)
		{
			if (itemSize > 0.0)
			{
				change = (int)Math.Floor((maxStorageSpace + 0.05 - totalItemStorageSize) / itemSize);
			}
			if (change < 0)
			{
				// if the player is already over the maximum (e.g. after a mod update), don't go into some ridiculous minus values
				change = 0;
			}
		}
		return change;
	}

	/// <summary>Shows an equipment error and stops reloading the list.</summary>
	private void ShowError(string msg)
	{
		_game.PushState(new ErrorMessageState(msg, _palette, (byte)_game.Mod.GetInterface("craftEquipment").GetElement("errorMessage").color, "BACK04.SCR", _game.Mod.GetInterface("craftEquipment").GetElement("errorPalette").color));
		_reload = false;
	}

	/// <summary>
	/// Moves the given number of items (selected) to the craft.
	/// </summary>
	/// <param name="change">Item difference.</param>
	/// <param name="suppressErrors">Suppress error messages?</param>
	public void MoveRightByValue(int change, bool suppressErrors = false)
	{
		Craft c = _base.Crafts[_craft];
		RuleItem item = _game.Mod.GetItem(_items[_sel], true);
		int bqty = _base.StorageItems.GetItem(item);
		if (_isNewBattle)
		{
			if (change == int.MaxValue)
			{
				change = 10;
			}
			bqty = change;
		}
		if (0 >= change || 0 >= bqty) return;
		change = Math.Min(bqty, change);
		// Do we need to convert item to vehicle?
		if (item.VehicleUnit != null)
		{
			int size = item.VehicleUnit.Armor.TotalSize;
			// Check if there's enough room
			int room = c.ValidateAddingVehicles(size);
			if (room > 0)
			{
				change = Math.Min(room, change);
				if (item.VehicleClipAmmo != null)
				{
					// And now let's see if we can add the total number of vehicles.
					RuleItem ammo = item.VehicleClipAmmo;
					int ammoPerVehicle = item.VehicleClipsLoaded;

					int baseQty = _base.StorageItems.GetItem(ammo) / ammoPerVehicle;
					if (_isNewBattle)
						baseQty = change;
					int canBeAdded = Math.Min(change, baseQty);
					if (canBeAdded > 0)
					{
						for (int i = 0; i < canBeAdded; ++i)
						{
							if (!_isNewBattle)
							{
								_base.StorageItems.RemoveItem(ammo, ammoPerVehicle);
								_base.StorageItems.RemoveItem(item);
							}
							c.Vehicles.Add(new Vehicle(item, item.VehicleClipSize, size));
							c.ResetCustomDeployment(); // adding a vehicle into a craft invalidates a custom craft deployment
						}
					}
					else
					{
						if (!suppressErrors)
						{
							// So we haven't managed to increase the count of vehicles because of the ammo
							_timerRight.Stop();
							ShowError(Tr("STR_NOT_ENOUGH_AMMO_TO_ARM_HWP").Arg(ammoPerVehicle).Arg(Tr(ammo.Type)));
						}
					}
				}
				else
				{
					for (int i = 0; i < change; ++i)
					{
						c.Vehicles.Add(new Vehicle(item, item.VehicleClipSize, size));
						c.ResetCustomDeployment(); // adding a vehicle into a craft invalidates a custom craft deployment
						if (!_isNewBattle)
						{
							_base.StorageItems.RemoveItem(item);
						}
					}
				}
			}
		}
		else
		{
			if (_totalItems + change > c.MaxItemsClamped)
			{
				if (!suppressErrors)
				{
					_timerRight.Stop();
					ShowError(Tr("STR_NO_MORE_EQUIPMENT_ALLOWED", c.MaxItemsClamped));
				}
				change = c.MaxItemsClamped - _totalItems;
				if (change < 0)
				{
					// if the player is already over the maximum (e.g. after a mod update), don't go into some ridiculous minus values
					change = 0;
				}
			}
			if (_totalItemStorageSize + (change * item.Size) > c.MaxStorageSpaceClamped + 0.05)
			{
				change = MaxByCraftStorage(c.MaxStorageSpaceClamped, _totalItemStorageSize, item.Size, change);
				if (!suppressErrors)
				{
					_timerRight.Stop();
					ShowError(Tr("STR_NO_MORE_EQUIPMENT_ALLOWED_BY_SIZE").Arg(c.MaxStorageSpaceClamped));
				}
			}
			c.Items.AddItem(item, change);
			_totalItems += change;
			_totalItemStorageSize += change * item.Size;
			if (!_isNewBattle)
			{
				_base.StorageItems.RemoveItem(item, change);
			}
		}
		UpdateQuantity();
	}

	/// <summary>
	/// Empties the contents of the craft, moving all of the items back to the base.
	/// </summary>
	public void BtnClearClick(Action action)
	{
		for (_sel = 0; _sel != _items.Count; ++_sel)
		{
			MoveLeftByValue(int.MaxValue);
		}

		// in New Battle, clear also stuff that is not displayed on the GUI (for whatever reason)
		if (_isNewBattle)
		{
			Craft c = _base.Crafts[_craft];
			c.Items.Clear();
		}
	}

	/// <summary>
	/// Displays the inventory screen for the soldiers
	/// inside the craft.
	/// </summary>
	public void BtnInventoryClick(Action action)
	{
		Craft craft = _base.Crafts[_craft];
		if (craft.NumTotalSoldiers > 0)
		{
			if (Options.oxceAlternateCraftEquipmentManagement && !_isNewBattle)
			{
				// This is a bit tricky... here's what we're doing:
				// * Remember the extra craft items (i.e. items that are on the craft, but not equipped by soldiers)
				// * Move all equipment from the base into the craft.
				// * Run the inventory screen.
				// * Remove excess items from the craft when CraftEquipmentState::init() is called after leaving the inventory screen.
				// (After this, the craft should have all the updated soldier equipment, and the same extra items as before.)

				// Note: the current implementation assumes no limit to the number or size of items a craft can hold.
				//       If the craft has limited space, then we just won't have all the base items available on the inventory screen.

				var extras = craft.ExtraItems;
				extras.Clear();

				// temporarily ignore craft limits
				int maxItemsBackup = craft.CraftStats.maxItems;
				double maxStorageSpaceBackup = craft.CraftStats.maxStorageSpace;
				craft.MaxItemsRaw = 999999;
				craft.MaxStorageSpaceRaw = 99999.0;

				for (_sel = 0; _sel != _items.Count; ++_sel)
				{
					RuleItem rule = _game.Mod.GetItem(_items[_sel], true);
					if (craft.Items.GetItem(rule) > 0)
					{
						extras.AddItem(rule, craft.Items.GetItem(rule) - craft.SoldierItems.GetItem(rule));
					}
					if (rule.VehicleUnit == null && rule.CanBeEquippedBeforeBaseDefense())
					{
						MoveRightByValue(int.MaxValue, true);
					}
				}
				// restore limits
				craft.MaxItemsRaw = maxItemsBackup;
				craft.MaxStorageSpaceRaw = maxStorageSpaceBackup;
			}

			SavedBattleGame bgame = new SavedBattleGame(_game.Mod, _game.Language);
			_game.SavedGame.SetBattleGame(bgame);

			if (_game.IsCtrlPressed(true) && _game.IsAltPressed(true))
			{
				_game.SavedGame.DisableSoldierEquipment = true;
			}
			BattlescapeGenerator bgen = new BattlescapeGenerator(_game);
			bgen.RunInventory(craft);

			_game.Screen.Clear();
			_game.PushState(new InventoryState(false, null, _base));
			_returningFromInventory = true;
		}
	}

	/// <summary>
	/// Saves the craft equipment visible on the screen into a global loadout template.
	/// </summary>
	public void SaveGlobalLoadout(int index)
	{
		// clear the template
		ItemContainer tmpl = _game.SavedGame.GetGlobalCraftLoadout(index);
		tmpl.Clear();

		Craft c = _base.Crafts[_craft];
		// save only what is visible on the screen (can be DIFFERENT than what's really in the craft for various reasons)
		foreach (var itemType in _items)
		{
			RuleItem item = _game.Mod.GetItem(itemType, true);
			int cQty = 0;
			if (item.VehicleUnit != null)
			{
				cQty = c.GetVehicleCount(itemType);
			}
			else
			{
				cQty = c.Items.GetItem(item);
			}
			if (cQty > 0)
			{
				tmpl.AddItem(item, cQty);
			}
		}
	}

	/// <summary>
	/// Applies a global loadout template to the craft and reports what is missing.
	/// </summary>
	public void LoadGlobalLoadout(int index, bool onlyAddItems = false)
	{
		// temporarily turn off alternate craft equipment management to allow removing all items from the craft
		bool backup = Options.oxceAlternateCraftEquipmentManagement;
		Options.oxceAlternateCraftEquipmentManagement = false;

		// reset filters and reload the full equipment list
		_btnQuickSearch.Text = "";
		_cbxFilterBy.Selected = 0;
		InitList();

		Craft c = _base.Crafts[_craft];

		ItemContainer craftItemsBackup = new ItemContainer();
		List<Vehicle> craftVehiclesBackup = [];
		if (onlyAddItems)
		{
			// remember for later, make copies
			foreach (var pair in c.Items.Contents)
			{
				craftItemsBackup.AddItem(pair.Key, pair.Value);
			}
			craftVehiclesBackup.AddRange(c.Vehicles);
		}
		else
		{
			// first move everything visible back to base
			for (_sel = 0; _sel != _items.Count; ++_sel)
			{
				MoveLeftByValue(int.MaxValue);
			}
		}

		// now start applying the template (consider ONLY items visible on the GUI)
		ItemContainer tmpl = _game.SavedGame.GetGlobalCraftLoadout(index);
		for (_sel = 0; _sel != _items.Count; ++_sel)
		{
			RuleItem item = _game.Mod.GetItem(_items[_sel], true);
			int tQty = tmpl.GetItem(item);
			MoveRightByValue(tQty, true);
		}

		// lastly check and report what's missing
		string craftName = c.GetName(_game.Language);
		List<ReequipStat> missingItems = [];
		foreach (var templateItem in tmpl.Contents)
		{
			RuleItem item = templateItem.Key;
			if (item != null)
			{
				int tQty = templateItem.Value;
				int cQty = 0;
				if (item.VehicleUnit != null)
				{
					// Note: we will also report HWPs as missing:
					// - if there is not enough ammo to arm them
					// - if there is not enough cargo space in the craft
					cQty = c.GetVehicleCount(item.Type);
					if (onlyAddItems)
					{
						int total = 0;
						foreach (var vehicle in craftVehiclesBackup)
						{
							if (vehicle.Rules == item)
							{
								total++;
							}
						}
						cQty -= total; // i.e. only count newly added vehicles
					}
				}
				else
				{
					cQty = c.Items.GetItem(item);
					if (onlyAddItems)
					{
						cQty -= craftItemsBackup.GetItem(item); // i.e. only count newly added items
					}
				}
				int missing = tQty - cQty;
				if (missing > 0)
				{
					missingItems.Add(new ReequipStat { item = item.Type, qty = missing, craft = craftName, listOrder = item.ListOrder });
				}
			}
		}

		if (missingItems.Count != 0)
		{
			// C++ std::sort; a stable sort gives the same result for distinct list orders
			CraftSoldiersState.StableSort(missingItems, (a, b) => a.listOrder < b.listOrder);
			_game.PushState(new CannotReequipState(missingItems, _base));
		}

		// turn back the original setting
		Options.oxceAlternateCraftEquipmentManagement = backup;
	}

	/// <summary>
	/// Opens the CraftEquipmentLoadState screen.
	/// </summary>
	public void BtnLoadClick(Action action)
	{
		//if (!_isNewBattle)
		{
			_game.PushState(new CraftEquipmentLoadState(this));
			_returningFromGlobalTemplates = true;
		}
	}

	/// <summary>
	/// Opens the CraftEquipmentSaveState screen.
	/// </summary>
	public void BtnSaveClick(Action action)
	{
		if (!_isNewBattle)
		{
			_game.PushState(new CraftEquipmentSaveState(this));
			_returningFromGlobalTemplates = true;
		}
	}
}
