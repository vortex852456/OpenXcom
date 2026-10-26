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
// Ported from: src/Basescape/SoldiersState.h, src/Basescape/SoldiersState.cpp
// Port status: partial (missing: SDL_WarpMouse - the mouse pointer does not follow a soldier moved with the arrows)
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)
#pragma warning disable IDE0060 // handler signatures keep the unused Action parameter like C++

/// <summary>
/// Soldiers screen that lets the player
/// manage all the soldiers in a base.
/// </summary>
public class SoldiersState : State
{
	private readonly TextButton _btnOk, _btnPsiTraining, _btnTraining, _btnMemorial;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtName, _txtRank, _txtCraft;
	private readonly ComboBox _cbxSortBy, _cbxScreenActions;
	private readonly TextList _lstSoldiers;
	private readonly Base _base;
	private readonly List<Soldier> _origSoldierOrder;
	private readonly List<Soldier> _filteredListOfSoldiers = [];
	private readonly List<int> _filteredIndicesOfSoldiers = [];
	private readonly List<SortFunctor> _sortFunctors = [];
	private GetStatFn _dynGetter;
	private readonly List<string> _availableOptions = [];
	private int _mainOffset;

	/// <summary>
	/// Initializes all the elements in the Soldiers screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	public SoldiersState(Base @base)
	{
		_base = @base;
		_origSoldierOrder = new List<Soldier>(_base.Soldiers);
		_dynGetter = null;
		_mainOffset = 0;

		bool isPsiBtnVisible = Options.anytimePsiTraining && _base.AvailablePsiLabs > 0;
		bool isTrnBtnVisible = _base.AvailableTraining > 0;
		var availableTransformations = new List<RuleSoldierTransformation>();
		_game.SavedGame.GetAvailableTransformations(availableTransformations, _game.Mod, _base);
		bool isTransformationAvailable = availableTransformations.Count > 0;

		// if both training buttons would be displayed, or if there are any transformations, switch to combobox
		bool showCombobox = isTransformationAvailable || (isPsiBtnVisible && isTrnBtnVisible) || Options.oxceAlternateCraftEquipmentManagement;
		// 3 buttons or 2 buttons?
		bool showThreeButtons = !showCombobox && (isPsiBtnVisible || isTrnBtnVisible);

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		if (showThreeButtons)
		{
			_btnOk = PreAdd(new TextButton(96, 16, 216, 176));
			_btnMemorial = PreAdd(new TextButton(96, 16, 8, 176));
		}
		else
		{
			_btnOk = PreAdd(new TextButton(148, 16, 164, 176));
			_btnMemorial = PreAdd(new TextButton(148, 16, 8, 176));
		}
		_btnPsiTraining = PreAdd(new TextButton(96, 16, 112, 176));
		_btnTraining = PreAdd(new TextButton(96, 16, 112, 176));
		_cbxScreenActions = PreAdd(new ComboBox(this, 148, 16, 8, 176, true));
		_txtTitle = new Text(168, 17, 16, 8);
		_cbxSortBy = new ComboBox(this, 120, 16, 192, 8, false);
		_txtName = new Text(114, 9, 16, 32);
		_txtRank = new Text(102, 9, 122, 32);
		_txtCraft = new Text(82, 9, 220, 32);
		_lstSoldiers = new TextList(288, 128, 8, 40);

		// Set palette
		SetInterface("soldierList");

		Add(_window, "window", "soldierList");
		Add(_btnOk, "button", "soldierList");
		Add(_txtTitle, "text1", "soldierList");
		Add(_txtName, "text2", "soldierList");
		Add(_txtRank, "text2", "soldierList");
		Add(_txtCraft, "text2", "soldierList");
		Add(_lstSoldiers, "list", "soldierList");
		Add(_cbxSortBy, "button", "soldierList");
		if (showCombobox)
		{
			Add(_cbxScreenActions, "button", "soldierList");
		}
		else
		{
			Add(_btnMemorial, "button", "soldierList");
			Add(_btnPsiTraining, "button", "soldierList");
			Add(_btnTraining, "button", "soldierList");
		}

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "soldierList");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnOk.OnKeyboardPress(BtnInventoryClick, Options.keyBattleInventory);
		_btnOk.OnKeyboardPress(BtnTransformationsOverviewClick, SDLKey.SDLK_t);

		_btnPsiTraining.Text = Tr("STR_PSI_TRAINING");
		_btnPsiTraining.OnMouseClick(BtnPsiTrainingClick);
		_btnPsiTraining.Visible = isPsiBtnVisible;

		_btnTraining.Text = Tr("STR_TRAINING");
		_btnTraining.OnMouseClick(BtnTrainingClick);
		_btnTraining.Visible = isTrnBtnVisible;

		_btnMemorial.Text = Tr("STR_MEMORIAL");
		_btnMemorial.OnMouseClick(BtnMemorialClick);

		_availableOptions.Clear();
		if (showCombobox)
		{
			_btnMemorial.Visible = false;
			_btnPsiTraining.Visible = false;
			_btnTraining.Visible = false;

			_availableOptions.Add("STR_SOLDIER_INFO");
			_availableOptions.Add("STR_MEMORIAL");
			_availableOptions.Add("STR_INVENTORY");

			if (isPsiBtnVisible)
				_availableOptions.Add("STR_PSI_TRAINING");

			if (isTrnBtnVisible)
				_availableOptions.Add("STR_TRAINING");

			if (isTransformationAvailable)
			{
				_mainOffset = _availableOptions.Count;
				_availableOptions.Add("STR_TRANSFORMATIONS_OVERVIEW");
			}

			bool refreshDeadSoldierStats = false;
			foreach (var transformationRule in availableTransformations)
			{
				_availableOptions.Add(transformationRule.Name);
				if (transformationRule.IsAllowingDeadSoldiers)
				{
					refreshDeadSoldierStats = true;
				}
			}
			if (refreshDeadSoldierStats)
			{
				foreach (var deadMan in _game.SavedGame.DeadSoldiers)
				{
					deadMan.PrepareStatsWithBonuses(_game.Mod); // refresh stats for sorting
				}
			}

			_cbxScreenActions.SetOptions(_availableOptions, true);
			_cbxScreenActions.Selected = 0;
			_cbxScreenActions.OnChange(CbxScreenActionsChange);
		}

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_LEFT;
		_txtTitle.SetText(Tr("STR_SOLDIER_LIST"));

		_txtName.SetText(Tr("STR_NAME_UC"));

		_txtRank.SetText(Tr("STR_RANK"));

		_txtCraft.SetText(Tr("STR_CRAFT"));

		// populate sort options
		var sortOptions = new List<string>();
		sortOptions.Add(Tr("STR_ORIGINAL_ORDER"));
		_sortFunctors.Add(null);

		void PushIn(string strId, GetStatFn functor)
		{
			sortOptions.Add(Tr(strId));
			_sortFunctors.Add(new SortFunctor(_game, functor));
		}

		PushIn("STR_ID", SoldierSortUtilFunctions.IdStat);
		PushIn("STR_NAME_UC", SoldierSortUtilFunctions.NameStat);
		PushIn("STR_CRAFT", SoldierSortUtilFunctions.CraftIdStat);
		PushIn("STR_SOLDIER_TYPE", SoldierSortUtilFunctions.TypeStat);
		PushIn("STR_RANK", SoldierSortUtilFunctions.RankStat);
		PushIn("STR_IDLE_DAYS", SoldierSortUtilFunctions.IdleDaysStat);
		PushIn("STR_MISSIONS2", SoldierSortUtilFunctions.MissionsStat);
		PushIn("STR_KILLS2", SoldierSortUtilFunctions.KillsStat);
		PushIn("STR_WOUND_RECOVERY2", SoldierSortUtilFunctions.WoundRecoveryStat);
		if (_game.Mod.IsManaFeatureEnabled && !_game.Mod.ReplenishManaAfterMission)
		{
			PushIn("STR_MANA_MISSING", SoldierSortUtilFunctions.ManaMissingStat);
		}
		PushIn("STR_TIME_UNITS", SoldierSortUtilFunctions.TuStat);
		PushIn("STR_STAMINA", SoldierSortUtilFunctions.StaminaStat);
		PushIn("STR_HEALTH", SoldierSortUtilFunctions.HealthStat);
		PushIn("STR_BRAVERY", SoldierSortUtilFunctions.BraveryStat);
		PushIn("STR_REACTIONS", SoldierSortUtilFunctions.ReactionsStat);
		PushIn("STR_FIRING_ACCURACY", SoldierSortUtilFunctions.FiringStat);
		PushIn("STR_THROWING_ACCURACY", SoldierSortUtilFunctions.ThrowingStat);
		PushIn("STR_MELEE_ACCURACY", SoldierSortUtilFunctions.MeleeStat);
		PushIn("STR_STRENGTH", SoldierSortUtilFunctions.StrengthStat);
		if (_game.Mod.IsManaFeatureEnabled)
		{
			// "unlock" is checked later
			PushIn("STR_MANA_POOL", SoldierSortUtilFunctions.ManaStat);
		}
		PushIn("STR_PSIONIC_STRENGTH", SoldierSortUtilFunctions.PsiStrengthStat);
		PushIn("STR_PSIONIC_SKILL", SoldierSortUtilFunctions.PsiSkillStat);

		_cbxSortBy.SetOptions(sortOptions);
		_cbxSortBy.Selected = 0;
		_cbxSortBy.OnChange(CbxSortByChange);
		_cbxSortBy.SetText(Tr("STR_SORT_BY"));

		//_lstSoldiers.SetArrowColumn(188, ArrowOrientation.ARROW_VERTICAL);
		_lstSoldiers.SetColumns(3, 106, 98, 76);
		_lstSoldiers.SetAlign(TextHAlign.ALIGN_RIGHT, 3);
		_lstSoldiers.SetSelectable(true);
		_lstSoldiers.SetBackground(_window);
		_lstSoldiers.Margin = 8;
		_lstSoldiers.OnLeftArrowClick(LstItemsLeftArrowClick);
		_lstSoldiers.OnRightArrowClick(LstItemsRightArrowClick);
		_lstSoldiers.OnMouseClick(LstSoldiersClick);
		_lstSoldiers.OnMouseClick(LstSoldiersClick, SDLMouse.SDL_BUTTON_RIGHT);
		_lstSoldiers.OnMousePress(LstSoldiersMousePress);
	}

	/// <summary>
	/// std::stable_sort with a C++ "less than" predicate.
	/// </summary>
	internal static void StableSort<T>(List<T> list, Func<T, T, bool> less)
	{
		var sorted = list.OrderBy(x => x, Comparer<T>.Create((a, b) => less(a, b) ? -1 : less(b, a) ? 1 : 0)).ToList();
		list.Clear();
		list.AddRange(sorted);
	}

	/// <summary>
	/// Stand-in for the C++ comparison of RuleCraft pointers (allocation order): the position in the craft list.
	/// </summary>
	internal static int CraftRuleOrder(RuleCraft rule) => _game.Mod.CraftsList.IndexOf(rule.Type);

	/// <summary>
	/// Sorts the soldiers list by the selected criterion
	/// </summary>
	public void CbxSortByChange(Action action)
	{
		bool ctrlPressed = _game.IsCtrlPressed();
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
					StableSort(_base.Soldiers, (a, b) =>
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
									return CraftRuleOrder(a.Craft.Rules) < CraftRuleOrder(b.Craft.Rules);
								}
							}
							else
							{
								return true; // a < b
							}
						}
						return false; // b > a
					});
				}
				else
				{
					StableSort(_base.Soldiers, compFunc.Invoke);
				}
				if (_game.IsShiftPressed())
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
	/// Updates the soldiers list
	/// after going to other screens.
	/// </summary>
	public override void Init()
	{
		base.Init();

		// resets the savegame when coming back from the inventory
		_game.SavedGame.SetBattleGame(null);
		_base.SetInBattlescape(false);

		_base.PrepareSoldierStatsWithBonuses(); // refresh stats for sorting
		InitList(_lstSoldiers.Scroll);
	}

	/// <summary>
	/// Shows the soldiers in a list at specified offset/scroll.
	/// </summary>
	private void InitList(int scrl)
	{
		_lstSoldiers.ClearList();

		_filteredListOfSoldiers.Clear();
		_filteredIndicesOfSoldiers.Clear();

		string selAction = "STR_SOLDIER_INFO";
		if (_availableOptions.Count != 0)
		{
			selAction = _availableOptions[_cbxScreenActions.Selected];
		}

		int offset = 0;
		if (selAction == "STR_SOLDIER_INFO")
		{
			_lstSoldiers.SetArrowColumn(188, ArrowOrientation.ARROW_VERTICAL);

			// all soldiers in the base
			_filteredListOfSoldiers.AddRange(_base.Soldiers);
		}
		else
		{
			offset = 20;
			_lstSoldiers.SetArrowColumn(-1, ArrowOrientation.ARROW_VERTICAL);

			// filtered list of soldiers eligible for transformation
			RuleSoldierTransformation transformationRule = _game.Mod.GetSoldierTransformation(selAction);
			if (transformationRule != null)
			{
				int idx = -1;
				foreach (var soldier in _base.Soldiers)
				{
					idx++;
					if (soldier.Craft != null && soldier.Craft.Status == "STR_OUT")
					{
						// soldiers outside of the base are not eligible
						continue;
					}
					if (soldier.IsEligibleForTransformation(transformationRule))
					{
						_filteredListOfSoldiers.Add(soldier);
						_filteredIndicesOfSoldiers.Add(idx);
					}
				}
				foreach (var deadMan in _game.SavedGame.DeadSoldiers)
				{
					if (deadMan.IsEligibleForTransformation(transformationRule))
					{
						_filteredListOfSoldiers.Add(deadMan);
						_filteredIndicesOfSoldiers.Add(-1); // invalid
					}
				}
			}
		}

		if (_dynGetter != null)
		{
			_lstSoldiers.SetColumns(4, 106, 98 - offset, 60 + offset, 16);
		}
		else
		{
			_lstSoldiers.SetColumns(3, 106, 98 - offset, 76 + offset);
		}
		_txtCraft.X = _txtRank.X + 98 - offset;

		BaseSumDailyRecovery recovery = _base.SumRecoveryPerDay;
		int row = 0;
		foreach (var soldier in _filteredListOfSoldiers)
		{
			string craftString = soldier.GetCraftString(_game.Language, recovery);

			if (_dynGetter != null)
			{
				// call corresponding getter
				int dynStat = _dynGetter(_game, soldier);
				_lstSoldiers.AddRow(4, soldier.GetName(true), Tr(soldier.RankString), craftString, dynStat.ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				_lstSoldiers.AddRow(3, soldier.GetName(true), Tr(soldier.RankString), craftString);
			}

			if (soldier.Craft == null)
			{
				_lstSoldiers.SetRowColor(row, _lstSoldiers.SecondaryColor);
			}
			if (soldier.Death != null)
			{
				_lstSoldiers.SetRowColor(row, _txtCraft.Color);
			}
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
			if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
			{
				MoveSoldierUp(action, row);
			}
			else if (action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT)
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
		Soldier s = soldiers[(int)row];
		if (max)
		{
			soldiers.RemoveAt((int)row);
			soldiers.Insert(0, s);
		}
		else
		{
			soldiers[(int)row] = soldiers[(int)row - 1];
			soldiers[(int)row - 1] = s;
			if (row != (uint)_lstSoldiers.Scroll)
			{
				WarpMouse(action.LeftBlackBand + action.XMouse, action.TopBlackBand + action.YMouse - (ushort)(8 * action.YScale));
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
			if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
			{
				MoveSoldierDown(action, row);
			}
			else if (action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT)
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
		Soldier s = soldiers[(int)row];
		if (max)
		{
			soldiers.RemoveAt((int)row);
			soldiers.Add(s);
		}
		else
		{
			soldiers[(int)row] = soldiers[(int)row + 1];
			soldiers[(int)row + 1] = s;
			if (row != (uint)(_lstSoldiers.VisibleRows - 1 + _lstSoldiers.Scroll))
			{
				WarpMouse(action.LeftBlackBand + action.XMouse, action.TopBlackBand + action.YMouse + (ushort)(8 * action.YScale));
			}
			else
			{
				_lstSoldiers.ScrollDown(false);
			}
		}
		InitList(_lstSoldiers.Scroll);
	}

	/// <summary>
	/// SDL_WarpMouse: would move the mouse pointer to the given window position.
	/// Port status: the host cannot move the system pointer yet, so this does nothing.
	/// </summary>
	internal static void WarpMouse(int x, int y)
	{
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Opens the Psionic Training screen.
	/// </summary>
	public void BtnPsiTrainingClick(Action action)
	{
		_game.PushState(new AllocatePsiTrainingState(_base));
	}

	/// <summary>
	/// Opens the Martial Training screen.
	/// </summary>
	public void BtnTrainingClick(Action action)
	{
		_game.PushState(new AllocateTrainingState(_base));
	}

	/// <summary>
	/// Opens the Memorial screen.
	/// </summary>
	public void BtnMemorialClick(Action action)
	{
		_game.PushState(new SoldierMemorialState());
	}

	/// <summary>
	/// Opens the Transformations Overview screen.
	/// </summary>
	public void BtnTransformationsOverviewClick(Action action)
	{
		if (_mainOffset > 0)
		{
			// needed in SoldierTransformationListState.LstTransformationsClick()
			_cbxScreenActions.Selected = _mainOffset;

			_game.PushState(new SoldierTransformationListState(_base, _cbxScreenActions));
		}
	}

	/// <summary>
	/// Opens the selected screen from the combo box
	/// </summary>
	public void CbxScreenActionsChange(Action action)
	{
		string selAction = _availableOptions[_cbxScreenActions.Selected];

		if (selAction == "STR_MEMORIAL")
		{
			_cbxScreenActions.Selected = 0;
			_game.PushState(new SoldierMemorialState());
		}
		else if (selAction == "STR_INVENTORY")
		{
			_cbxScreenActions.Selected = 0;
			BtnInventoryClick(null);
		}
		else if (selAction == "STR_PSI_TRAINING")
		{
			_cbxScreenActions.Selected = 0;
			_game.PushState(new AllocatePsiTrainingState(_base));
		}
		else if (selAction == "STR_TRAINING")
		{
			_cbxScreenActions.Selected = 0;
			_game.PushState(new AllocateTrainingState(_base));
		}
		else if (selAction == "STR_TRANSFORMATIONS_OVERVIEW")
		{
			_game.PushState(new SoldierTransformationListState(_base, _cbxScreenActions));
		}
		else
		{
			// "STR_SOLDIER_INFO" or any available soldier transformation
			InitList(0);
		}
	}

	/// <summary>
	/// Displays the inventory screen for the soldiers inside the base.
	/// </summary>
	public void BtnInventoryClick(Action action)
	{
		if (_base.GetAvailableSoldiers(true, true) > 0)
		{
			var bgame = new SavedBattleGame(_game.Mod, _game.Language);
			_game.SavedGame.SetBattleGame(bgame);
			bgame.MissionType = "STR_BASE_DEFENSE";

			if (_game.IsCtrlPressed() && _game.IsAltPressed())
			{
				_game.SavedGame.DisableSoldierEquipment = true;
			}
			var bgen = new BattlescapeGenerator(_game);
			bgen.SetBase(_base);
			bgen.RunInventory(null);

			// pre-select the soldier under the mouse cursor (if possible)
			if (_availableOptions.Count == 0 || _cbxScreenActions.Selected == 0)
			{
				uint idx = _lstSoldiers.SelectedRow;
				if (idx < (uint)_base.Soldiers.Count)
				{
					int soldierId = _base.Soldiers[(int)idx].Id;
					foreach (var unit in bgame.Units)
					{
						if (unit.Id == soldierId)
						{
							bgame.SelectedUnit = unit;
							break;
						}
					}
				}
			}

			_game.Screen.Clear();
			_game.PushState(new InventoryState(false, null, _base, true));
		}
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

		string selAction = "STR_SOLDIER_INFO";
		if (_availableOptions.Count != 0)
		{
			selAction = _availableOptions[_cbxScreenActions.Selected];
		}
		if (selAction == "STR_SOLDIER_INFO")
		{
			if (action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT)
			{
				BtnInventoryClick(null);
			}
			else
			{
				_game.PushState(new SoldierInfoState(_base, unchecked((int)_lstSoldiers.SelectedRow)));
			}
		}
		else if (action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT)
		{
			uint idx = _lstSoldiers.SelectedRow;
			if (idx < (uint)_filteredIndicesOfSoldiers.Count)
			{
				int soldierId = _filteredIndicesOfSoldiers[(int)idx];
				if (soldierId > -1)
				{
					_game.PushState(new SoldierInfoState(_base, soldierId));
				}
			}
		}
		else
		{
			RuleSoldierTransformation transformationRule = _game.Mod.GetSoldierTransformation(selAction);
			if (transformationRule != null)
			{
				_game.PushState(new SoldierTransformationState(
					transformationRule,
					_base,
					_filteredListOfSoldiers[(int)_lstSoldiers.SelectedRow],
					_filteredListOfSoldiers));
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
}
