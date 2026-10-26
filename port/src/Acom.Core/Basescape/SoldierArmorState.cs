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
// Ported from: src/Basescape/SoldierArmorState.h, src/Basescape/SoldierArmorState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)
#pragma warning disable IDE0060 // handler signatures keep the unused Action parameter like C++

/// <summary>Where the armor screen was opened from.</summary>
public enum SoldierArmorOrigin : uint
{
	SA_GEOSCAPE = 0,
	SA_BATTLESCAPE = 1,
}

/// <summary>Sort order of the armor list.</summary>
public enum ArmorSort : uint
{
	ARMOR_SORT_NONE = 0,
	ARMOR_SORT_NAME_ASC = 1,
	ARMOR_SORT_NAME_DESC = 2,
}

/// <summary>An armor in the selection list.</summary>
public class ArmorItem
{
	public string type;
	public string name;
	public string quantity;

	public ArmorItem(string _type, string _name, string _quantity)
	{
		type = _type;
		name = _name;
		quantity = _quantity;
	}
}

/// <summary>
/// Select Armor window that allows changing
/// of the armor equipped on a soldier.
/// </summary>
public class SoldierArmorState : State
{
	private readonly Base _base;
	private readonly int _soldier; // C++ size_t
	private readonly SoldierArmorOrigin _origin;
	private readonly TextButton _btnCancel;
	private readonly TextEdit _btnQuickSearch;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtType, _txtQuantity;
	private readonly TextList _lstArmor;
	private readonly ArrowButton _sortName;
	private readonly List<ArmorItem> _armors = [];
	private readonly List<int> _indices = [];
	private ArmorSort _armorOrder;

	/// <summary>C++ compareArmorName.</summary>
	private static bool CompareArmorName(ArmorItem a, ArmorItem b) => Unicode.NaturalCompare(a.name, b.name);

	/// <summary>
	/// Initializes all the elements in the Soldier Armor window.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="soldier">ID of the selected soldier.</param>
	/// <param name="origin">Where the screen was opened from.</param>
	public SoldierArmorState(Base @base, int soldier, SoldierArmorOrigin origin)
	{
		_base = @base;
		_soldier = soldier;
		_origin = origin;
		_screen = false;

		// Create objects
		_window = new Window(this, 192, 160, 64, 20, WindowPopup.POPUP_BOTH);
		_btnQuickSearch = new TextEdit(this, 48, 9, 80, 43);
		_btnCancel = new TextButton(140, 16, 90, 156);
		_txtTitle = new Text(182, 16, 69, 28);
		_txtType = new Text(90, 9, 80, 52);
		_txtQuantity = new Text(70, 9, 190, 52);
		_lstArmor = new TextList(160, 80, 73, 68);
		_sortName = new ArrowButton(ArrowShape.ARROW_NONE, 11, 8, 80, 52);

		// Set palette
		if (_origin == SoldierArmorOrigin.SA_BATTLESCAPE)
		{
			SetStandardPalette("PAL_BATTLESCAPE");
		}
		else
		{
			SetInterface("soldierArmor");
		}

		Add(_window, "window", "soldierArmor");
		Add(_btnQuickSearch, "button", "soldierArmor");
		Add(_btnCancel, "button", "soldierArmor");
		Add(_txtTitle, "text", "soldierArmor");
		Add(_txtType, "text", "soldierArmor");
		Add(_txtQuantity, "text", "soldierArmor");
		Add(_lstArmor, "list", "soldierArmor");
		Add(_sortName, "text", "soldierArmor");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "soldierArmor");

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		Soldier s = _base.Soldiers[_soldier];
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_SELECT_ARMOR_FOR_SOLDIER").Arg(s.GetName()));

		_txtType.SetText(Tr("STR_TYPE"));

		_txtQuantity.SetText(Tr("STR_QUANTITY_UC"));

		_lstArmor.SetColumns(2, 132, 21);
		_lstArmor.SetSelectable(true);
		_lstArmor.SetBackground(_window);
		_lstArmor.Margin = 8;

		_sortName.X = _sortName.X + _txtType.GetTextWidth() + 4;
		_sortName.OnMouseClick(SortNameClick);

		{
			// refresh soldier's _bonusCache, needed below in Armor::getCanBeUsedBy()
			//s.GetBonuses(_game.Mod);
		}

		foreach (var a in _game.Mod.ArmorsForSoldiers)
		{
			if (a.RequiredResearch != null && !_game.SavedGame.IsResearched(a.RequiredResearch))
				continue;
			if (!a.GetCanBeUsedBy(s))
				continue;
			if (a.HasInfiniteSupply)
			{
				_armors.Add(new ArmorItem(a.Type, Tr(a.Type), ""));
			}
			else if (_base.StorageItems.GetItem(a.StoreItem) > 0 || a.StoreItem == s.Armor.StoreItem)
			{
				string ss;
				if (_game.SavedGame.MonthsPassed > -1)
				{
					ss = _base.StorageItems.GetItem(a.StoreItem).ToString(CultureInfo.InvariantCulture);
				}
				else
				{
					ss = "-";
				}
				_armors.Add(new ArmorItem(a.Type, Tr(a.Type), ss));
			}
		}

		_btnQuickSearch.Text = ""; // redraw
		_btnQuickSearch.OnChange(BtnQuickSearchApply);
		_btnQuickSearch.Visible = Options.oxceQuickSearchButton;

		_btnCancel.OnKeyboardRelease(BtnQuickSearchToggle, Options.keyToggleQuickSearch);

		_armorOrder = ArmorSort.ARMOR_SORT_NONE;
		SortList();

		_lstArmor.OnMouseClick(LstArmorClick);
		_lstArmor.OnMouseClick(LstArmorClickMiddle, SDLMouse.SDL_BUTTON_MIDDLE);

		// switch to battlescape theme if called from inventory
		if (_origin == SoldierArmorOrigin.SA_BATTLESCAPE)
		{
			ApplyBattlescapeTheme("soldierArmor");
		}
	}

	/// <summary>
	/// Updates the sorting arrows based
	/// on the current setting.
	/// </summary>
	private void UpdateArrows()
	{
		_sortName.SetShape(ArrowShape.ARROW_NONE);
		switch (_armorOrder)
		{
			case ArmorSort.ARMOR_SORT_NAME_ASC:
				_sortName.SetShape(ArrowShape.ARROW_SMALL_UP);
				break;
			case ArmorSort.ARMOR_SORT_NAME_DESC:
				_sortName.SetShape(ArrowShape.ARROW_SMALL_DOWN);
				break;
			default:
				break;
		}
	}

	/// <summary>
	/// Sorts the armor list.
	/// </summary>
	public void SortList()
	{
		UpdateArrows();

		switch (_armorOrder)
		{
			case ArmorSort.ARMOR_SORT_NAME_ASC:
				SoldiersState.StableSort(_armors, CompareArmorName);
				break;
			case ArmorSort.ARMOR_SORT_NAME_DESC:
				// C++ stable_sort over reverse iterators
				_armors.Reverse();
				SoldiersState.StableSort(_armors, CompareArmorName);
				_armors.Reverse();
				break;
			default:
				break;
		}

		UpdateList();
	}

	/// <summary>
	/// Updates the armor list with the current list
	/// of available armors.
	/// </summary>
	public void UpdateList()
	{
		string searchString = Unicode.UpperCase(_btnQuickSearch.Text);

		_lstArmor.ClearList();
		_indices.Clear();

		int index = -1;
		foreach (var armorItem in _armors)
		{
			++index;

			// quick search
			if (!string.IsNullOrEmpty(searchString))
			{
				string armorName = Unicode.UpperCase(armorItem.name);
				if (!armorName.Contains(searchString, StringComparison.Ordinal))
				{
					continue;
				}
			}

			_lstArmor.AddRow(2, armorItem.name, armorItem.quantity);
			_indices.Add(index);
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
			if (sym != SDLKey.SDLK_RETURN && sym != SDLKey.SDLK_KP_ENTER && sym != SDLKey.SDLK_ESCAPE)
				return;
		}
		UpdateList();
	}

	/// <summary>
	/// Equips the armor on the soldier and returns to the previous screen.
	/// </summary>
	public void LstArmorClick(Action action)
	{
		Soldier soldier = _base.Soldiers[_soldier];
		Armor prev = soldier.Armor;
		Armor next = _game.Mod.GetArmor(_armors[_indices[(int)_lstArmor.SelectedRow]].type);
		Craft craft = soldier.Craft;
		if (craft != null)
		{
			if (!craft.ValidateArmorChange(prev.Size, next.Size))
			{
				RuleInterface soldierInfo = _game.Mod.GetInterface("soldierInfo");
				_game.PushState(new ErrorMessageState(Tr("STR_NOT_ENOUGH_CRAFT_SPACE"), _palette, (byte)soldierInfo.GetElement("errorMessage").color, "BACK01.SCR", soldierInfo.GetElement("errorPalette").color));
				return;
			}
		}
		if (_game.SavedGame.MonthsPassed != -1)
		{
			if (prev.StoreItem != null)
			{
				_base.StorageItems.AddItem(prev.StoreItem);
			}
			if (next.StoreItem != null)
			{
				_base.StorageItems.RemoveItem(next.StoreItem);
			}
		}
		soldier.SetArmor(next, true);
		_game.SavedGame.LastSelectedArmor = next.Type;

		_game.PopState();
	}

	/// <summary>
	/// Shows corresponding Ufopaedia article.
	/// </summary>
	public void LstArmorClickMiddle(Action action)
	{
		Armor armor = _game.Mod.GetArmor(_armors[_indices[(int)_lstArmor.SelectedRow]].type, true);
		string articleId = armor.UfopediaType;
		Ufopaedia.OpenArticle(_game, articleId);
	}

	/// <summary>
	/// Sorts the armors by name.
	/// </summary>
	public void SortNameClick(Action action)
	{
		_armorOrder = _armorOrder == ArmorSort.ARMOR_SORT_NAME_ASC ? ArmorSort.ARMOR_SORT_NAME_DESC : ArmorSort.ARMOR_SORT_NAME_ASC;
		SortList();
	}
}
