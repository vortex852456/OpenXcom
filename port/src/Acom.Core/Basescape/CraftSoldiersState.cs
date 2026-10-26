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
// Ported from: src/Basescape/CraftSoldiersState.h, src/Basescape/CraftSoldiersState.cpp
#nullable disable

using System.Globalization;
using static Acom.SoldierSortUtilFunctions;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Select Squad screen that lets the player
/// pick the soldiers to assign to a craft.
/// </summary>
public class CraftSoldiersState : TouchState
{
	private readonly TextButton _btnOk;
	private readonly TextButton _btnPreview;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtName, _txtRank, _txtCraft, _txtAvailable, _txtUsed;
	private readonly ComboBox _cbxSortBy;
	private readonly TextList _lstSoldiers;

	private readonly Base _base;
	private readonly int _craft;
	private readonly byte _otherCraftColor;
	private readonly List<Soldier> _origSoldierOrder;
	private readonly List<SortFunctor> _sortFunctors = [];
	private GetStatFn _dynGetter;

	/// <summary>
	/// C++ <c>std::stable_sort</c> with a "less" predicate (merge sort; equal elements keep their order).
	/// </summary>
	internal static void StableSort<T>(List<T> list, Func<T, T, bool> less)
	{
		T[] a = [.. list];
		T[] tmp = new T[a.Length];
		for (int width = 1; width < a.Length; width *= 2)
		{
			for (int lo = 0; lo < a.Length; lo += 2 * width)
			{
				int mid = Math.Min(lo + width, a.Length);
				int hi = Math.Min(lo + 2 * width, a.Length);
				int i = lo, j = mid, k = lo;
				while (i < mid && j < hi)
				{
					// take from the right run only if it is strictly less (stability)
					tmp[k++] = less(a[j], a[i]) ? a[j++] : a[i++];
				}
				while (i < mid) tmp[k++] = a[i++];
				while (j < hi) tmp[k++] = a[j++];
			}
			(a, tmp) = (tmp, a);
		}
		list.Clear();
		list.AddRange(a);
	}

	/// <summary>
	/// Sort order of soldiers by craft: soldiers in a craft first, grouped by craft type, then by craft id.
	/// </summary>
	/// <remarks>
	/// C++ orders craft types by RuleCraft pointer value (allocation order); here the index in the
	/// craft list of the mod (load order) is used, which is deterministic.
	/// </remarks>
	internal static bool CraftLess(Mod mod, Soldier a, Soldier b)
	{
		if (a.Craft != null)
		{
			if (b.Craft != null)
			{
				if (a.Craft.Rules == b.Craft.Rules)
				{
					return a.Craft.Id < b.Craft.Id;
				}
				else
				{
					return mod.CraftsList.IndexOf(a.Craft.Rules.Type) < mod.CraftsList.IndexOf(b.Craft.Rules.Type);
				}
			}
			else
			{
				return true; // a < b
			}
		}
		return false; // b > a
	}

	/// <summary>
	/// Initializes all the elements in the Craft Soldiers screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="craft">ID of the selected craft.</param>
	public CraftSoldiersState(Base @base, int craft)
	{
		_base = @base;
		_craft = craft;
		_otherCraftColor = 0;
		_origSoldierOrder = [.. _base.Soldiers];
		_dynGetter = null;

		bool hidePreview = _game.SavedGame.MonthsPassed == -1;
		Craft c = _base.Crafts[_craft];
		if (c != null && !c.Rules.IsForNewBattle)
		{
			// no battlescape map available
			hidePreview = true;
		}

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnOk = new TextButton(hidePreview ? 148 : 30, 16, hidePreview ? 164 : 274, 176);
		_btnPreview = new TextButton(102, 16, 164, 176);
		_txtTitle = new Text(300, 17, 16, 7);
		_txtName = new Text(114, 9, 16, 32);
		_txtRank = new Text(102, 9, 122, 32);
		_txtCraft = new Text(84, 9, 220, 32);
		_txtAvailable = new Text(110, 9, 16, 24);
		_txtUsed = new Text(110, 9, 122, 24);
		_cbxSortBy = new ComboBox(this, 148, 16, 8, 176, true);
		_lstSoldiers = new TextList(288, 128, 8, 40);

		TouchComponentsCreate(_txtTitle, true);

		// Set palette
		SetInterface("craftSoldiers");

		Add(_window, "window", "craftSoldiers");
		Add(_btnOk, "button", "craftSoldiers");
		Add(_btnPreview, "button", "craftSoldiers");
		Add(_txtTitle, "text", "craftSoldiers");
		Add(_txtName, "text", "craftSoldiers");
		Add(_txtRank, "text", "craftSoldiers");
		Add(_txtCraft, "text", "craftSoldiers");
		Add(_txtAvailable, "text", "craftSoldiers");
		Add(_txtUsed, "text", "craftSoldiers");
		Add(_lstSoldiers, "list", "craftSoldiers");
		Add(_cbxSortBy, "button", "craftSoldiers");

		TouchComponentsAdd("button2", "craftSoldiers", _window);

		_otherCraftColor = (byte)_game.Mod.GetInterface("craftSoldiers").GetElement("otherCraft").color;

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "craftSoldiers");

		TouchComponentsConfigure();

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnOk.OnKeyboardPress(BtnDeassignAllSoldiersClick, Options.keyRemoveSoldiersFromAllCrafts);
		_btnOk.OnKeyboardPress(BtnDeassignCraftSoldiersClick, Options.keyRemoveSoldiersFromCraft);

		_btnPreview.Text = Tr("STR_CRAFT_DEPLOYMENT_PREVIEW");
		_btnPreview.Visible = !hidePreview;
		_btnPreview.OnMouseClick(BtnPreviewClick);

		_txtTitle.SetBig();
		if (Options.oxceBaseTouchButtons)
		{
			_txtTitle.Align = TextHAlign.ALIGN_CENTER;
			_txtTitle.SetText(c.GetName(_game.Language));
		}
		else
		{
			_txtTitle.Align = TextHAlign.ALIGN_LEFT;
			_txtTitle.SetText(Tr("STR_SELECT_SQUAD_FOR_CRAFT").Arg(c.GetName(_game.Language)));
		}

		_txtName.SetText(Tr("STR_NAME_UC"));

		_txtRank.SetText(Tr("STR_RANK"));

		_txtCraft.SetText(Tr("STR_CRAFT"));

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

		_lstSoldiers.SetArrowColumn(188, ArrowOrientation.ARROW_VERTICAL);
		_lstSoldiers.SetColumns(3, 106, 98, 76);
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
					StableSort(_base.Soldiers, (a, b) => Unicode.NaturalCompare(a.GetName(), b.GetName()));
				}
				else if (selIdx == 3)
				{
					var mod = _game.Mod;
					StableSort(_base.Soldiers, (a, b) => CraftLess(mod, a, b));
				}
				else
				{
					StableSort(_base.Soldiers, compFunc.Invoke);
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

		int originalScrollPos = _lstSoldiers.Scroll;
		InitList(originalScrollPos);
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Shows the battlescape preview.
	/// </summary>
	public void BtnPreviewClick(Action action)
	{
		Craft c = _base.Crafts[_craft];
		if (c.SpaceUsed <= 0)
		{
			// at least one unit must be onboard
			return;
		}

		SavedBattleGame bgame = new SavedBattleGame(_game.Mod, _game.Language, true);
		_game.SavedGame.SetBattleGame(bgame);
		BattlescapeGenerator bgen = new BattlescapeGenerator(_game);
		bgame.MissionType = c.Rules.CustomPreviewType;
		bgame.CraftForPreview = c;
		bgen.SetCraft(c);
		bgen.Run();

		// needed for preview of craft deployment tiles
		bgame.SetCraftPos(bgen.CraftPos);
		bgame.SetCraftZ(bgen.CraftZ);
		bgame.CalculateCraftTiles();

		_game.PushState(new BriefingState(c, null));
	}

	/// <summary>
	/// Shows the soldiers in a list at specified offset/scroll.
	/// </summary>
	private void InitList(int scrl)
	{
		int row = 0;
		_lstSoldiers.ClearList();

		if (_dynGetter != null)
		{
			_lstSoldiers.SetColumns(4, 106, 98, 60, 16);
		}
		else
		{
			_lstSoldiers.SetColumns(3, 106, 98, 76);
		}

		Craft c = _base.Crafts[_craft];
		BaseSumDailyRecovery recovery = _base.SumRecoveryPerDay;
		foreach (var soldier in _base.Soldiers)
		{
			if (_dynGetter != null)
			{
				// call corresponding getter
				int dynStat = _dynGetter(_game, soldier);
				_lstSoldiers.AddRow(4, soldier.GetName(true, 19), Tr(soldier.RankString), soldier.GetCraftString(_game.Language, recovery), dynStat.ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				_lstSoldiers.AddRow(3, soldier.GetName(true, 19), Tr(soldier.RankString), soldier.GetCraftString(_game.Language, recovery));
			}

			byte color;
			if (soldier.Craft == c)
			{
				color = _lstSoldiers.SecondaryColor;
			}
			else if (soldier.Craft != null)
			{
				color = _otherCraftColor;
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

		_txtAvailable.SetText(Tr("STR_SPACE_AVAILABLE").Arg(c.SpaceAvailable));
		_txtUsed.SetText(Tr("STR_SPACE_USED").Arg(c.SpaceUsed));
	}

	/// <summary>
	/// Shows the soldiers in a list.
	/// </summary>
	public override void Init()
	{
		base.Init();
		_base.PrepareSoldierStatsWithBonuses(); // refresh stats for sorting
		InitList(_lstSoldiers.Scroll);

		// update the label to indicate presence of a saved craft deployment
		Craft c = _base.Crafts[_craft];
		if (c.HasCustomDeployment)
			_btnPreview.Text = Tr("STR_CRAFT_DEPLOYMENT_PREVIEW_SAVED");
		else
			_btnPreview.Text = Tr("STR_CRAFT_DEPLOYMENT_PREVIEW");

		TouchComponentsRefresh();
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
				SDLMouse.SDL_WarpMouse((ushort)(action.LeftBlackBand + action.XMouse), (ushort)(action.TopBlackBand + action.YMouse - (ushort)(8 * action.YScale)));
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
				SDLMouse.SDL_WarpMouse((ushort)(action.LeftBlackBand + action.XMouse), (ushort)(action.TopBlackBand + action.YMouse + (ushort)(8 * action.YScale)));
			}
			else
			{
				_lstSoldiers.ScrollDown(false);
			}
		}
		InitList(_lstSoldiers.Scroll);
	}

	/// <summary>Shows a craft placement error (soldierInfo interface colors).</summary>
	private void ShowError(string message)
	{
		_game.PushState(new ErrorMessageState(Tr(message), _palette, (byte)_game.Mod.GetInterface("soldierInfo").GetElement("errorMessage").color, "BACK01.SCR", _game.Mod.GetInterface("soldierInfo").GetElement("errorPalette").color));
	}

	/// <summary>
	/// Shows the selected soldier's info.
	/// </summary>
	public void LstSoldiersClick(Action action)
	{
		double mx = action.AbsoluteXMouse;
		if (mx >= _lstSoldiers.ArrowsLeftEdge && mx < _lstSoldiers.ArrowsRightEdge)
		{
			return;
		}
		int row = (int)_lstSoldiers.SelectedRow;
		if (_game.IsLeftClick(action, true))
		{
			Craft c = _base.Crafts[_craft];
			Soldier s = _base.Soldiers[(int)_lstSoldiers.SelectedRow];
			if (s.Craft == c)
			{
				s.SetCraftAndMoveEquipment(null, _base, _game.SavedGame.MonthsPassed == -1);
				_lstSoldiers.SetCellText(row, 2, Tr("STR_NONE_UC"));
				_lstSoldiers.SetRowColor(row, _lstSoldiers.Color);
			}
			else if (s.Craft != null && s.Craft.Status == "STR_OUT")
			{
				// nothing
			}
			else if (s.HasFullHealth)
			{
				int space = c.SpaceAvailable;
				CraftPlacementErrors err = c.ValidateAddingSoldier(space, s);
				if (err == CraftPlacementErrors.CPE_None)
				{
					s.SetCraftAndMoveEquipment(c, _base, _game.SavedGame.MonthsPassed == -1, true);
					_lstSoldiers.SetCellText(row, 2, c.GetName(_game.Language));
					_lstSoldiers.SetRowColor(row, _lstSoldiers.SecondaryColor);

					// update the label to indicate absence of a saved craft deployment
					_btnPreview.Text = Tr("STR_CRAFT_DEPLOYMENT_PREVIEW");
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

			_txtAvailable.SetText(Tr("STR_SPACE_AVAILABLE").Arg(c.SpaceAvailable));
			_txtUsed.SetText(Tr("STR_SPACE_USED").Arg(c.SpaceUsed));
		}
		else if (_game.IsRightClick(action, true))
		{
			_game.PushState(new SoldierInfoState(_base, row, false));
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
	/// De-assign all soldiers from all craft located in the base (i.e. not out on a mission).
	/// </summary>
	public void BtnDeassignAllSoldiersClick(Action action)
	{
		int row = 0;
		foreach (var soldier in _base.Soldiers)
		{
			if (soldier.Craft != null && soldier.Craft.Status != "STR_OUT")
			{
				soldier.SetCraftAndMoveEquipment(null, _base, _game.SavedGame.MonthsPassed == -1);
				_lstSoldiers.SetCellText(row, 2, Tr("STR_NONE_UC"));
				_lstSoldiers.SetRowColor(row, _lstSoldiers.Color);
			}
			row++;
		}

		Craft c = _base.Crafts[_craft];
		_txtAvailable.SetText(Tr("STR_SPACE_AVAILABLE").Arg(c.SpaceAvailable));
		_txtUsed.SetText(Tr("STR_SPACE_USED").Arg(c.SpaceUsed));
	}

	/// <summary>
	/// De-assign all soldiers from the current craft.
	/// </summary>
	public void BtnDeassignCraftSoldiersClick(Action action)
	{
		Craft c = _base.Crafts[_craft];
		int row = 0;
		foreach (var soldier in _base.Soldiers)
		{
			if (soldier.Craft == c)
			{
				soldier.SetCraftAndMoveEquipment(null, _base, _game.SavedGame.MonthsPassed == -1);
				_lstSoldiers.SetCellText(row, 2, Tr("STR_NONE_UC"));
				_lstSoldiers.SetRowColor(row, _lstSoldiers.Color);
			}
			row++;
		}

		_txtAvailable.SetText(Tr("STR_SPACE_AVAILABLE").Arg(c.SpaceAvailable));
		_txtUsed.SetText(Tr("STR_SPACE_USED").Arg(c.SpaceUsed));
	}
}
