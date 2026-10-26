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
// Ported from: src/Basescape/CraftArmorState.h, src/Basescape/CraftArmorState.cpp
// Port status: partial (missing: SDL_WarpMouse after reordering, the host cannot warp the pointer yet)
#nullable disable

using System.Globalization;
using static Acom.SoldierSortUtilFunctions;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Select Armor screen that lets the player
/// pick armor for the soldiers on the craft.
/// </summary>
public class CraftArmorState : TouchState
{
	private readonly TextButton _btnOk;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtName, _txtCraft, _txtArmor;
	private readonly ComboBox _cbxSortBy;
	private readonly TextList _lstSoldiers;

	private readonly Base _base;
	private readonly int _craft;
	private int _savedScrollPosition;
	private readonly List<Soldier> _origSoldierOrder;
	private readonly List<SortFunctor> _sortFunctors = [];
	private GetStatFn _dynGetter;

	/// <summary>
	/// Initializes all the elements in the Craft Armor screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="craft">ID of the selected craft.</param>
	public CraftArmorState(Base @base, int craft)
	{
		_base = @base;
		_craft = craft;
		_savedScrollPosition = 0;
		_origSoldierOrder = [.. _base.Soldiers];
		_dynGetter = null;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnOk = new TextButton(148, 16, 164, 176);
		_txtTitle = new Text(300, 17, 16, 7);
		_txtName = new Text(114, 9, 16, 32);
		_txtCraft = new Text(76, 9, 122, 32);
		_txtArmor = new Text(100, 9, 192, 32);
		_lstSoldiers = new TextList(288, 128, 8, 40);
		_cbxSortBy = new ComboBox(this, 148, 16, 8, 176, true);

		TouchComponentsCreate(_txtTitle, true);

		// Set palette
		SetInterface("craftArmor");

		Add(_window, "window", "craftArmor");
		Add(_btnOk, "button", "craftArmor");
		Add(_txtTitle, "text", "craftArmor");
		Add(_txtName, "text", "craftArmor");
		Add(_txtCraft, "text", "craftArmor");
		Add(_txtArmor, "text", "craftArmor");
		Add(_lstSoldiers, "list", "craftArmor");
		Add(_cbxSortBy, "button", "craftArmor");

		TouchComponentsAdd("button2", "craftArmor", _window);

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "craftArmor");

		TouchComponentsConfigure();

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnOk.OnKeyboardPress(BtnDeequipAllArmorClick, Options.keyRemoveArmorFromAllCrafts);
		_btnOk.OnKeyboardPress(BtnDeequipCraftArmorClick, Options.keyRemoveArmorFromCraft);

		_txtTitle.SetBig();
		_txtTitle.SetText(Tr("STR_SELECT_ARMOR"));

		_txtName.SetText(Tr("STR_NAME_UC"));

		_txtCraft.SetText(Tr("STR_CRAFT"));

		_txtArmor.SetText(Tr("STR_ARMOR"));

		// populate sort options
		List<string> sortOptions = [];
		sortOptions.Add(Tr("STR_ORIGINAL_ORDER"));
		_sortFunctors.Add(null);

		void PUSH_IN(string strId, GetStatFn functor)
		{
			sortOptions.Add(Tr(strId));
			_sortFunctors.Add(new SortFunctor(_game, functor));
		}

		PUSH_IN("STR_ID", IdStat);
		PUSH_IN("STR_NAME_UC", NameStat);
		PUSH_IN("STR_CRAFT", CraftIdStat);
		PUSH_IN("STR_SOLDIER_TYPE", TypeStat);
		PUSH_IN("STR_RANK", RankStat);
		PUSH_IN("STR_IDLE_DAYS", IdleDaysStat);
		PUSH_IN("STR_MISSIONS2", MissionsStat);
		PUSH_IN("STR_KILLS2", KillsStat);
		PUSH_IN("STR_WOUND_RECOVERY2", WoundRecoveryStat);
		if (_game.Mod.IsManaFeatureEnabled && !_game.Mod.ReplenishManaAfterMission)
		{
			PUSH_IN("STR_MANA_MISSING", ManaMissingStat);
		}
		PUSH_IN("STR_TIME_UNITS", TuStat);
		PUSH_IN("STR_STAMINA", StaminaStat);
		PUSH_IN("STR_HEALTH", HealthStat);
		PUSH_IN("STR_BRAVERY", BraveryStat);
		PUSH_IN("STR_REACTIONS", ReactionsStat);
		PUSH_IN("STR_FIRING_ACCURACY", FiringStat);
		PUSH_IN("STR_THROWING_ACCURACY", ThrowingStat);
		PUSH_IN("STR_MELEE_ACCURACY", MeleeStat);
		PUSH_IN("STR_STRENGTH", StrengthStat);
		if (_game.Mod.IsManaFeatureEnabled)
		{
			// "unlock" is checked later
			PUSH_IN("STR_MANA_POOL", ManaStat);
		}
		PUSH_IN("STR_PSIONIC_STRENGTH", PsiStrengthStat);
		PUSH_IN("STR_PSIONIC_SKILL", PsiSkillStat);

		_cbxSortBy.SetOptions(sortOptions);
		_cbxSortBy.Selected = 0;
		_cbxSortBy.OnChange(CbxSortByChange);
		_cbxSortBy.SetText(Tr("STR_SORT_BY"));

		_lstSoldiers.SetColumns(3, 106, 70, 104);
		_lstSoldiers.SetAlign(TextHAlign.ALIGN_RIGHT, 3);
		_lstSoldiers.SetSelectable(true);
		_lstSoldiers.SetBackground(_window);
		_lstSoldiers.Margin = 8;
		_lstSoldiers.OnLeftArrowClick(LstItemsLeftArrowClick);
		_lstSoldiers.OnRightArrowClick(LstItemsRightArrowClick);
		_lstSoldiers.OnMouseClick(LstSoldiersClick, 0);
		_lstSoldiers.OnMousePress(LstSoldiersMousePress);
	}

	/// <summary>
	/// Sorts the soldiers list by the selected criterion.
	/// </summary>
	public void CbxSortByChange(Action action)
	{
		bool ctrlPressed = _game.IsCtrlPressed(true);
		int selIdx = _cbxSortBy.Selected;
		if (selIdx == -1)
		{
			return;
		}

		SortFunctor compFunc = _sortFunctors[selIdx];
		_dynGetter = null;
		if (compFunc != null)
		{
			if (selIdx != 2 && selIdx != 3)
			{
				_dynGetter = compFunc.Getter;
			}

			// if CTRL is pressed, we only want to show the dynamic column, without actual sorting
			if (!ctrlPressed)
			{
				if (selIdx == 2)
				{
					CraftSoldiersState.StableSort(_base.Soldiers, (a, b) => Unicode.NaturalCompare(a.GetName(), b.GetName()));
				}
				else if (selIdx == 3)
				{
					var mod = _game.Mod;
					CraftSoldiersState.StableSort(_base.Soldiers, (a, b) => CraftSoldiersState.CraftLess(mod, a, b));
				}
				else
				{
					CraftSoldiersState.StableSort(_base.Soldiers, compFunc.Invoke);
				}
				if (_game.IsShiftPressed(true))
				{
					_base.Soldiers.Reverse();
				}
			}
		}
		else
		{
			// restore original ordering, ignoring (of course) those
			// soldiers that have been sacked since this state started
			foreach (var origSoldier in _origSoldierOrder)
			{
				int soldierIt = _base.Soldiers.IndexOf(origSoldier);
				if (soldierIt != -1)
				{
					Soldier s = _base.Soldiers[soldierIt];
					_base.Soldiers.RemoveAt(soldierIt);
					_base.Soldiers.Add(s);
				}
			}
		}

		InitList(_lstSoldiers.Scroll);
	}

	/// <summary>
	/// The soldier armors can change
	/// after going into other screens.
	/// </summary>
	public override void Init()
	{
		base.Init();
		_base.PrepareSoldierStatsWithBonuses(); // refresh stats for sorting
		InitList(_savedScrollPosition);

		int row = 0;
		foreach (var soldier in _base.Soldiers)
		{
			_lstSoldiers.SetCellText(row, 2, Tr(soldier.Armor.Type));
			row++;
		}

		TouchComponentsRefresh();
	}

	/// <summary>
	/// Shows the soldiers in a list at specified offset/scroll.
	/// </summary>
	private void InitList(int scrl)
	{
		byte otherCraftColor = (byte)_game.Mod.GetInterface("craftArmor").GetElement("otherCraft").color;
		int row = 0;
		_lstSoldiers.ClearList();

		if (_dynGetter != null)
		{
			_lstSoldiers.SetArrowColumn(160, ArrowOrientation.ARROW_VERTICAL);
			_lstSoldiers.SetColumns(4, 106, 70, 88, 16);
		}
		else
		{
			_lstSoldiers.SetArrowColumn(-1, ArrowOrientation.ARROW_VERTICAL);
			_lstSoldiers.SetColumns(3, 106, 70, 104);
		}

		Craft c = _base.Crafts[_craft];
		BaseSumDailyRecovery recovery = _base.SumRecoveryPerDay;
		foreach (var soldier in _base.Soldiers)
		{
			if (_dynGetter != null)
			{
				// call corresponding getter
				int dynStat = _dynGetter(_game, soldier);
				_lstSoldiers.AddRow(4, soldier.GetName(true), soldier.GetCraftString(_game.Language, recovery), Tr(soldier.Armor.Type), dynStat.ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				_lstSoldiers.AddRow(3, soldier.GetName(true), soldier.GetCraftString(_game.Language, recovery), Tr(soldier.Armor.Type));
			}

			byte color;
			if (soldier.Craft == c)
			{
				color = _lstSoldiers.SecondaryColor;
			}
			else if (soldier.Craft != null)
			{
				color = otherCraftColor;
			}
			else
			{
				color = _lstSoldiers.Color;
			}
			_lstSoldiers.SetRowColor(row, color);
			row++;
		}
		if (scrl != 0)
			_lstSoldiers.ScrollTo(scrl);
		_lstSoldiers.Draw();
	}

	/// <summary>
	/// Reorders a soldier up.
	/// </summary>
	public void LstItemsLeftArrowClick(Action action)
	{
		uint row = _lstSoldiers.SelectedRow;
		if (row > 0)
		{
			if (_game.IsLeftClick(action, true))
			{
				MoveSoldierUp(action, row);
			}
			else if (_game.IsRightClick(action, true))
			{
				MoveSoldierUp(action, row, true);
			}
		}
		_cbxSortBy.SetText(Tr("STR_SORT_BY"));
		_cbxSortBy.Selected = -1;
	}

	/// <summary>
	/// Moves a soldier up on the list.
	/// </summary>
	/// <param name="action">Pointer to an action.</param>
	/// <param name="row">Selected soldier row.</param>
	/// <param name="max">Move the soldier to the top?</param>
	public void MoveSoldierUp(Action action, uint row, bool max = false)
	{
		var soldiers = _base.Soldiers;
		int r = (int)row;
		Soldier s = soldiers[r];
		if (max)
		{
			soldiers.RemoveAt(r);
			soldiers.Insert(0, s);
		}
		else
		{
			soldiers[r] = soldiers[r - 1];
			soldiers[r - 1] = s;
			if (r != _lstSoldiers.Scroll)
			{
				// C++: SDL_WarpMouse(x, y - 8 * yScale); missing, the host cannot warp the pointer yet.
			}
			else
			{
				_lstSoldiers.ScrollUp(false);
			}
		}
		InitList(_lstSoldiers.Scroll);
	}

	/// <summary>
	/// Reorders a soldier down.
	/// </summary>
	public void LstItemsRightArrowClick(Action action)
	{
		uint row = _lstSoldiers.SelectedRow;
		int numSoldiers = _base.Soldiers.Count;
		if (0 < numSoldiers && row < numSoldiers - 1)
		{
			if (_game.IsLeftClick(action, true))
			{
				MoveSoldierDown(action, row);
			}
			else if (_game.IsRightClick(action, true))
			{
				MoveSoldierDown(action, row, true);
			}
		}
		_cbxSortBy.SetText(Tr("STR_SORT_BY"));
		_cbxSortBy.Selected = -1;
	}

	/// <summary>
	/// Moves a soldier down on the list.
	/// </summary>
	/// <param name="action">Pointer to an action.</param>
	/// <param name="row">Selected soldier row.</param>
	/// <param name="max">Move the soldier to the bottom?</param>
	public void MoveSoldierDown(Action action, uint row, bool max = false)
	{
		var soldiers = _base.Soldiers;
		int r = (int)row;
		Soldier s = soldiers[r];
		if (max)
		{
			soldiers.RemoveAt(r);
			soldiers.Add(s);
		}
		else
		{
			soldiers[r] = soldiers[r + 1];
			soldiers[r + 1] = s;
			if (r != _lstSoldiers.VisibleRows - 1 + _lstSoldiers.Scroll)
			{
				// C++: SDL_WarpMouse(x, y + 8 * yScale); missing, the host cannot warp the pointer yet.
			}
			else
			{
				_lstSoldiers.ScrollDown(false);
			}
		}
		InitList(_lstSoldiers.Scroll);
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>Shows an error (soldierInfo interface colors).</summary>
	private void ShowError(string message)
	{
		_game.PushState(new ErrorMessageState(Tr(message), _palette, (byte)_game.Mod.GetInterface("soldierInfo").GetElement("errorMessage").color, "BACK01.SCR", _game.Mod.GetInterface("soldierInfo").GetElement("errorPalette").color));
	}

	/// <summary>
	/// Shows the Select Armor window.
	/// </summary>
	public void LstSoldiersClick(Action action)
	{
		double mx = action.AbsoluteXMouse;
		if (mx >= _lstSoldiers.ArrowsLeftEdge && mx < _lstSoldiers.ArrowsRightEdge)
		{
			return;
		}

		int selRow = (int)_lstSoldiers.SelectedRow;
		Soldier s = _base.Soldiers[selRow];
		if (!(s.Craft != null && s.Craft.Status == "STR_OUT"))
		{
			if (_game.IsLeftClick(action, true))
			{
				if (_game.IsCtrlPressed(true))
				{
					Craft c = _base.Crafts[_craft];
					if (s.Craft == c)
					{
						s.SetCraftAndMoveEquipment(null, _base, _game.SavedGame.MonthsPassed == -1);
						_lstSoldiers.SetCellText(selRow, 1, Tr("STR_NONE_UC"));
						_lstSoldiers.SetRowColor(selRow, _lstSoldiers.Color);
					}
					else if (s.HasFullHealth)
					{
						int space = c.SpaceAvailable;
						CraftPlacementErrors err = c.ValidateAddingSoldier(space, s);
						if (err == CraftPlacementErrors.CPE_None)
						{
							s.SetCraftAndMoveEquipment(c, _base, _game.SavedGame.MonthsPassed == -1, true);
							_lstSoldiers.SetCellText(selRow, 1, c.GetName(_game.Language));
							_lstSoldiers.SetRowColor(selRow, _lstSoldiers.SecondaryColor);
						}
						else if (err == CraftPlacementErrors.CPE_SoldierGroupNotAllowed)
						{
							ShowError("STR_SOLDIER_GROUP_NOT_ALLOWED");
						}
						else if (err == CraftPlacementErrors.CPE_SoldierGroupNotSame)
						{
							ShowError("STR_SOLDIER_GROUP_NOT_SAME");
						}
						else if (err == CraftPlacementErrors.CPE_ArmorGroupNotAllowed)
						{
							ShowError("STR_ARMOR_GROUP_NOT_ALLOWED");
						}
						else if (space > 0)
						{
							ShowError("STR_NOT_ENOUGH_CRAFT_SPACE");
						}
					}
				}
				else
				{
					_savedScrollPosition = _lstSoldiers.Scroll;
					_game.PushState(new SoldierArmorState(_base, selRow, SoldierArmorOrigin.SA_GEOSCAPE));
				}
			}
			else if (_game.IsRightClick(action, true))
			{
				SavedGame save = _game.SavedGame;
				Armor a = _game.IsCtrlPressed(true) ? s.Rules.DefaultArmor : _game.Mod.GetArmor(save.LastSelectedArmor);
				bool armorUnlocked = true;
				if (a != null && a.RequiredResearch != null && !_game.SavedGame.IsResearched(a.RequiredResearch))
				{
					armorUnlocked = false;
				}
				if (armorUnlocked && a != null)
				{
					Craft craft = s.Craft;
					if (craft != null && !craft.ValidateArmorChange(s.Armor.Size, a.Size))
					{
						armorUnlocked = false; // armor not valid due to craft constraints
						ShowError("STR_NOT_ENOUGH_CRAFT_SPACE");
					}
				}
				if (armorUnlocked && a != null && a.GetCanBeUsedBy(s))
				{
					if (save.MonthsPassed != -1)
					{
						if (a.StoreItem == null ||
							a.StoreItem == s.Armor.StoreItem ||
							_base.StorageItems.GetItem(a.StoreItem) > 0)
						{
							if (s.Armor.StoreItem != null)
							{
								_base.StorageItems.AddItem(s.Armor.StoreItem);
							}
							if (a.StoreItem != null)
							{
								_base.StorageItems.RemoveItem(a.StoreItem);
							}

							s.SetArmor(a, true);
							s.PrepareStatsWithBonuses(_game.Mod); // refresh stats for sorting
							_lstSoldiers.SetCellText(selRow, 2, Tr(a.Type));
						}
					}
					else
					{
						s.SetArmor(a, true);
						s.PrepareStatsWithBonuses(_game.Mod); // refresh stats for sorting
						_lstSoldiers.SetCellText(selRow, 2, Tr(a.Type));
					}
				}
			}
			else if (_game.IsMiddleClick(action, true))
			{
				string articleId = s.Armor.UfopediaType;
				Ufopaedia.OpenArticle(_game, articleId);
			}
		}
	}

	/// <summary>
	/// Handles the mouse-wheels on the arrow-buttons.
	/// </summary>
	public void LstSoldiersMousePress(Action action)
	{
		if (Options.changeValueByMouseWheel == 0)
			return;
		uint row = _lstSoldiers.SelectedRow;
		int numSoldiers = _base.Soldiers.Count;
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELUP &&
			row > 0)
		{
			if (action.AbsoluteXMouse >= _lstSoldiers.ArrowsLeftEdge &&
				action.AbsoluteXMouse <= _lstSoldiers.ArrowsRightEdge)
			{
				MoveSoldierUp(action, row);
			}
		}
		else if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELDOWN &&
			0 < numSoldiers && row < numSoldiers - 1)
		{
			if (action.AbsoluteXMouse >= _lstSoldiers.ArrowsLeftEdge &&
				action.AbsoluteXMouse <= _lstSoldiers.ArrowsRightEdge)
			{
				MoveSoldierDown(action, row);
			}
		}
	}

	/// <summary>
	/// Replaces the armor of a soldier by the default armor of its type, if the stores allow it.
	/// </summary>
	private void DeequipArmor(Soldier soldier, int row)
	{
		Armor a = soldier.Rules.DefaultArmor;

		if (soldier.Craft != null && !soldier.Craft.ValidateArmorChange(soldier.Armor.Size, a.Size))
		{
			// silently ignore
			return;
		}
		if (a.StoreItem == null || _base.StorageItems.GetItem(a.StoreItem) > 0)
		{
			if (soldier.Armor.StoreItem != null)
			{
				_base.StorageItems.AddItem(soldier.Armor.StoreItem);
			}
			if (a.StoreItem != null)
			{
				_base.StorageItems.RemoveItem(a.StoreItem);
			}

			soldier.SetArmor(a, true);
			soldier.PrepareStatsWithBonuses(_game.Mod); // refresh stats for sorting
			_lstSoldiers.SetCellText(row, 2, Tr(a.Type));
		}
	}

	/// <summary>
	/// De-equip armor from all soldiers located in the base (i.e. not out on a mission).
	/// </summary>
	public void BtnDeequipAllArmorClick(Action action)
	{
		int row = 0;
		foreach (var soldier in _base.Soldiers)
		{
			if (!(soldier.Craft != null && soldier.Craft.Status == "STR_OUT"))
			{
				DeequipArmor(soldier, row);
			}
			row++;
		}
	}

	/// <summary>
	/// De-equip armor of all soldiers on the current craft, and also all soldiers not assigned to any craft.
	/// </summary>
	public void BtnDeequipCraftArmorClick(Action action)
	{
		Craft c = _base.Crafts[_craft];
		int row = 0;
		foreach (var s in _base.Soldiers)
		{
			if (s.Craft == c || s.Craft == null)
			{
				DeequipArmor(s, row);
			}
			row++;
		}
	}
}
