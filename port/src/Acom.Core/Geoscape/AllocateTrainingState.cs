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
// Ported from: src/Geoscape/AllocateTrainingState.h, src/Geoscape/AllocateTrainingState.cpp
// Port status: partial (missing: SDL_WarpMouse – the mouse pointer does not follow a soldier moved with the arrows)
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // UI handlers stay instance members like in C++ (State._game is static here)

/// <summary>
/// Screen shown monthly to allow changing
/// soldiers currently in martial training.
/// </summary>
public class AllocateTrainingState : State
{
	private readonly TextButton _btnOk;
	private readonly ToggleTextButton _btnPlus;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtTraining, _txtName, _txtRemaining;
	private readonly Text _txtTu, _txtStamina, _txtHealth, _txtFiring, _txtThrowing, _txtMelee, _txtStrength;
	private readonly ComboBox _cbxSortBy;
	private readonly TextList _lstSoldiers;
	private uint _sel;
	private int _space;
	private readonly Base _base;
	private readonly List<Soldier> _origSoldierOrder;
	private readonly List<SortFunctor> _sortFunctors = [];
	private readonly List<SortFunctor> _sortFunctorsPlus = [];
	private bool _doNotReset;

	/// <summary>
	/// Initializes all the elements in the Martial Training screen.
	/// </summary>
	/// <param name="base">Pointer to the base to handle.</param>
	public AllocateTrainingState(Base @base)
	{
		_sel = 0;
		_base = @base;
		_origSoldierOrder = [.. _base.Soldiers];
		_doNotReset = false;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_txtTitle = new Text(300, 17, 10, 8);
		_txtRemaining = new Text(300, 10, 10, 24);
		_txtName = new Text(64, 10, 10, 40);
		_txtTraining = new Text(48, 20, 270, 32);
		_btnOk = new TextButton(148, 16, 164, 176);
		_lstSoldiers = new TextList(290, 112, 8, 52);
		_txtTu = new Text(18, 10, 120, 40);
		_txtStamina = new Text(18, 10, 138, 40);
		_txtHealth = new Text(18, 10, 156, 40);
		_txtFiring = new Text(18, 10, 174, 40);
		_txtThrowing = new Text(18, 10, 192, 40);
		_txtMelee = new Text(18, 10, 210, 40);
		_txtStrength = new Text(18, 10, 228, 40);
		_cbxSortBy = new ComboBox(this, 148, 16, 8, 176, true);
		_btnPlus = new ToggleTextButton(18, 16, 294, 8);

		// Set palette
		SetInterface("allocateMartial");

		Add(_window, "window", "allocateMartial");
		Add(_btnOk, "button", "allocateMartial");
		Add(_txtName, "text", "allocateMartial");
		Add(_txtTitle, "text", "allocateMartial");
		Add(_txtRemaining, "text", "allocateMartial");
		Add(_txtTraining, "text", "allocateMartial");
		Add(_lstSoldiers, "list", "allocateMartial");
		Add(_txtTu, "text", "allocateMartial");
		Add(_txtStamina, "text", "allocateMartial");
		Add(_txtHealth, "text", "allocateMartial");
		Add(_txtFiring, "text", "allocateMartial");
		Add(_txtThrowing, "text", "allocateMartial");
		Add(_txtMelee, "text", "allocateMartial");
		Add(_txtStrength, "text", "allocateMartial");
		Add(_cbxSortBy, "button", "allocateMartial");
		Add(_btnPlus, "button", "allocateMartial");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "allocateMartial");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnOk.OnKeyboardPress(BtnDeassignAllSoldiersClick, Options.keyRemoveSoldiersFromTraining);
		_btnOk.OnKeyboardPress(BtnAssignAllSoldiersClick, Options.keyAddSoldiersToTraining);

		_btnPlus.Text = "+";
		_btnPlus.Pressed = false;
		if (_game.Mod.SoldierBonusList.Count == 0)
		{
			// no soldier bonuses in the mod = button not needed
			_btnPlus.Visible = false;
		}
		else
		{
			_btnPlus.OnMouseClick(BtnPlusClick, 0);
		}

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_PHYSICAL_TRAINING"));

		_space = @base.AvailableTraining - @base.UsedTraining;
		_txtRemaining.SetText(Tr("STR_REMAINING_TRAINING_FACILITY_CAPACITY").Arg(_space));

		_txtName.SetText(Tr("STR_NAME"));
		_txtTu.SetText(Tr("STR_TIME_UNITS_ABBREVIATION"));
		_txtStamina.SetText(Tr("STR_STAMINA_ABBREVIATION"));
		_txtHealth.SetText(Tr("STR_HEALTH_ABBREVIATION"));
		_txtFiring.SetText(Tr("STR_FIRING_ACCURACY_ABBREVIATION"));
		_txtThrowing.SetText(Tr("STR_THROWING_ACCURACY_ABBREVIATION"));
		_txtMelee.SetText(Tr("STR_MELEE_ACCURACY_ABBREVIATION"));
		_txtStrength.SetText(Tr("STR_STRENGTH_ABBREVIATION"));
		_txtTraining.SetText(Tr("STR_IN_TRAINING"));

		_cbxSortBy.SetOptions(AllocateTrainingSort.PopulateSortOptions(this, _sortFunctors, _sortFunctorsPlus));
		_cbxSortBy.Selected = 0;
		_cbxSortBy.OnChange(CbxSortByChange);
		_cbxSortBy.SetText(Tr("STR_SORT_BY"));

		_lstSoldiers.SetArrowColumn(238, ArrowOrientation.ARROW_VERTICAL);
		_lstSoldiers.SetColumns(9, 110, 18, 18, 18, 18, 18, 18, 42, 40);
		_lstSoldiers.SetSelectable(true);
		_lstSoldiers.SetBackground(_window);
		_lstSoldiers.Margin = 2;
		_lstSoldiers.OnLeftArrowClick(LstItemsLeftArrowClick);
		_lstSoldiers.OnRightArrowClick(LstItemsRightArrowClick);
		_lstSoldiers.OnMouseClick(LstSoldiersClick);
		_lstSoldiers.OnMouseClick(LstSoldiersClick, SDLMouse.SDL_BUTTON_RIGHT);
		_lstSoldiers.OnMousePress(LstSoldiersMousePress);
	}

	/// <summary>
	/// Sorts the soldiers list by the selected criterion
	/// </summary>
	public void CbxSortByChange(Action action)
	{
		int selIdx = _cbxSortBy.Selected;
		if (selIdx == -1)
		{
			return;
		}

		SortFunctor compFunc = _btnPlus.Pressed ? _sortFunctorsPlus[selIdx] : _sortFunctors[selIdx];
		AllocateTrainingSort.Sort(_base.Soldiers, _origSoldierOrder, compFunc, selIdx, _game.IsShiftPressed());

		int originalScrollPos = _lstSoldiers.Scroll;
		InitList(originalScrollPos);
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		// Note: statString updates not necessary
		_game.PopState();
	}

	/// <summary>
	/// Updates the soldier stats. Sorts the list if applicable.
	/// </summary>
	public void BtnPlusClick(Action action)
	{
		int selIdx = _cbxSortBy.Selected;
		if (selIdx == -1)
		{
			int originalScrollPos = _lstSoldiers.Scroll;
			InitList(originalScrollPos);
		}
		else
		{
			CbxSortByChange(action);
		}
	}

	/// <summary>
	/// The soldier info could maybe change (armor? something else?)
	/// after going into other screens.
	/// </summary>
	public override void Init()
	{
		base.Init();

		// coming back from SoldierInfoState
		if (_doNotReset)
		{
			_doNotReset = false;
			return;
		}

		_base.PrepareSoldierStatsWithBonuses(); // refresh stats for sorting
		InitList(0);
	}

	/// <summary>
	/// Shows the soldiers in a list at specified offset/scroll.
	/// </summary>
	private void InitList(int scrl)
	{
		int row = 0;
		_lstSoldiers.ClearList();
		foreach (var soldier in _base.Soldiers)
		{
			UnitStats stats = _btnPlus.Pressed ? soldier.StatsWithSoldierBonusesOnly : soldier.CurrentStats;

			string tu = stats.tu.ToString(CultureInfo.InvariantCulture);
			string stamina = stats.stamina.ToString(CultureInfo.InvariantCulture);
			string health = stats.health.ToString(CultureInfo.InvariantCulture);
			string firing = stats.firing.ToString(CultureInfo.InvariantCulture);
			string throwing = stats.throwing.ToString(CultureInfo.InvariantCulture);
			string melee = stats.melee.ToString(CultureInfo.InvariantCulture);
			string strength = stats.strength.ToString(CultureInfo.InvariantCulture);

			bool isDone = soldier.IsFullyTrained;
			bool isWounded = soldier.IsWounded;
			bool isTraining = soldier.IsInTraining;
			bool isQueued = !isTraining && soldier.ReturnToTrainingWhenHealed;

			string status;
			if (isDone)
				status = Tr("STR_NO_DONE");
			else if (isQueued)
				status = Tr("STR_NO_QUEUED");
			else if (isWounded)
				status = Tr("STR_NO_WOUNDED");
			else if (isTraining)
				status = Tr("STR_YES");
			else
				status = Tr("STR_NO");

			_lstSoldiers.AddRow(9,
				soldier.GetName(true),
				tu,
				stamina,
				health,
				firing,
				throwing,
				melee,
				strength,
				status);
			_lstSoldiers.SetRowColor(row, isTraining ? _lstSoldiers.SecondaryColor : _lstSoldiers.Color);
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
		if (AllocateTrainingSort.MoveUp(_base.Soldiers, row, max, _lstSoldiers, action))
		{
			_lstSoldiers.ScrollUp(false);
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
		if (AllocateTrainingSort.MoveDown(_base.Soldiers, row, max, _lstSoldiers, action))
		{
			_lstSoldiers.ScrollDown(false);
		}
		InitList(_lstSoldiers.Scroll);
	}

	/// <summary>
	/// Assigns / removes a soldier from Martial Training.
	/// </summary>
	public void LstSoldiersClick(Action action)
	{
		double mx = action.AbsoluteXMouse;
		if (mx >= _lstSoldiers.ArrowsLeftEdge && mx < _lstSoldiers.ArrowsRightEdge)
		{
			return;
		}

		_sel = _lstSoldiers.SelectedRow;
		int sel = (int)_sel;
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
		{
			var soldier = _base.Soldiers[sel];

			// can't put fully trained soldiers back into training
			if (soldier.IsFullyTrained) return;

			// wounded soldiers can be queued/dequeued
			if (soldier.IsWounded)
			{
				if (soldier.ReturnToTrainingWhenHealed)
				{
					_lstSoldiers.SetCellText(sel, 8, Tr("STR_NO_WOUNDED"));
					soldier.ReturnToTrainingWhenHealed = false;
				}
				else
				{
					_lstSoldiers.SetCellText(sel, 8, Tr("STR_NO_QUEUED"));
					soldier.ReturnToTrainingWhenHealed = true;
				}
				return;
			}

			// healthy soldiers can be assigned/deassigned
			if (!soldier.IsInTraining)
			{
				if (_base.UsedTraining < _base.AvailableTraining)
				{
					_lstSoldiers.SetCellText(sel, 8, Tr("STR_YES"));
					_lstSoldiers.SetRowColor(sel, _lstSoldiers.SecondaryColor);
					_space--;
					_txtRemaining.SetText(Tr("STR_REMAINING_TRAINING_FACILITY_CAPACITY").Arg(_space));
					soldier.SetTraining(true);
					soldier.ReturnToTrainingWhenHealed = false;
				}
			}
			else
			{
				_lstSoldiers.SetCellText(sel, 8, Tr("STR_NO"));
				_lstSoldiers.SetRowColor(sel, _lstSoldiers.Color);
				_space++;
				_txtRemaining.SetText(Tr("STR_REMAINING_TRAINING_FACILITY_CAPACITY").Arg(_space));
				soldier.SetTraining(false);
				soldier.ReturnToTrainingWhenHealed = false;
			}
		}
		else if (action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT)
		{
			_doNotReset = true;
			_game.PushState(new SoldierInfoState(_base, sel, true, true));
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
	/// Removes all soldiers from Martial Training.
	/// </summary>
	public void BtnDeassignAllSoldiersClick(Action action)
	{
		int row = 0;
		foreach (var soldier in _base.Soldiers)
		{
			soldier.SetTraining(false);
			soldier.ReturnToTrainingWhenHealed = false;

			string status;
			if (soldier.IsFullyTrained)
				status = Tr("STR_NO_DONE");
			else if (soldier.IsWounded)
				status = Tr("STR_NO_WOUNDED");
			else
				status = Tr("STR_NO");

			_lstSoldiers.SetCellText(row, 8, Tr(status)); // C++ translates the translated text again
			_lstSoldiers.SetRowColor(row, _lstSoldiers.Color);
			row++;
		}
		_space = _base.AvailableTraining - _base.UsedTraining;
		_txtRemaining.SetText(Tr("STR_REMAINING_TRAINING_FACILITY_CAPACITY").Arg(_space));
	}

	/// <summary>
	/// Adds all soldiers to Martial Training.
	/// </summary>
	public void BtnAssignAllSoldiersClick(Action action)
	{
		int row = 0;
		foreach (var soldier in _base.Soldiers)
		{
			if (soldier.IsFullyTrained)
			{
				// can't put fully trained soldiers back into training
			}
			else if (soldier.IsWounded)
			{
				// wounded soldiers can be queued
				if (!soldier.ReturnToTrainingWhenHealed)
				{
					_lstSoldiers.SetCellText(row, 8, Tr("STR_NO_QUEUED"));
					soldier.ReturnToTrainingWhenHealed = true;
				}
			}
			else if (_space > 0 && !soldier.IsInTraining)
			{
				// healthy soldiers can be assigned
				_lstSoldiers.SetCellText(row, 8, Tr("STR_YES"));
				_lstSoldiers.SetRowColor(row, _lstSoldiers.SecondaryColor);
				_space--;
				soldier.SetTraining(true);
				soldier.ReturnToTrainingWhenHealed = false;
			}
			row++;
		}
		_txtRemaining.SetText(Tr("STR_REMAINING_TRAINING_FACILITY_CAPACITY").Arg(_space));
	}
}

/// <summary>
/// Logic shared by the martial and psi training allocation screens (identical in both C++ files).
/// </summary>
internal static class AllocateTrainingSort
{
	/// <summary>Builds the sort options and functors (the PUSH_IN macros of the C++ constructors).</summary>
	internal static List<string> PopulateSortOptions(State state, List<SortFunctor> sortFunctors, List<SortFunctor> sortFunctorsPlus)
	{
		Game game = State.GamePtr;
		var sortOptions = new List<string>();
		sortOptions.Add(state.Tr("STR_ORIGINAL_ORDER"));
		sortFunctors.Add(null);
		sortFunctorsPlus.Add(null);

		void pushIn(string strId, GetStatFn functor, GetStatFn functorPlus)
		{
			sortOptions.Add(state.Tr(strId));
			sortFunctors.Add(new SortFunctor(game, functor));
			sortFunctorsPlus.Add(new SortFunctor(game, functorPlus));
		}

		pushIn("STR_ID", SoldierSortUtilFunctions.IdStat, SoldierSortUtilFunctions.IdStat);
		pushIn("STR_NAME_UC", SoldierSortUtilFunctions.NameStat, SoldierSortUtilFunctions.NameStat);
		pushIn("STR_SOLDIER_TYPE", SoldierSortUtilFunctions.TypeStat, SoldierSortUtilFunctions.TypeStat);
		pushIn("STR_RANK", SoldierSortUtilFunctions.RankStat, SoldierSortUtilFunctions.RankStat);
		pushIn("STR_IDLE_DAYS", SoldierSortUtilFunctions.IdleDaysStat, SoldierSortUtilFunctions.IdleDaysStat);
		pushIn("STR_MISSIONS2", SoldierSortUtilFunctions.MissionsStat, SoldierSortUtilFunctions.MissionsStat);
		pushIn("STR_KILLS2", SoldierSortUtilFunctions.KillsStat, SoldierSortUtilFunctions.KillsStat);
		pushIn("STR_WOUND_RECOVERY2", SoldierSortUtilFunctions.WoundRecoveryStat, SoldierSortUtilFunctions.WoundRecoveryStat);
		if (game.Mod.IsManaFeatureEnabled && !game.Mod.ReplenishManaAfterMission)
		{
			pushIn("STR_MANA_MISSING", SoldierSortUtilFunctions.ManaMissingStat, SoldierSortUtilFunctions.ManaMissingStat);
		}

		pushIn("STR_TIME_UNITS", SoldierSortUtilFunctions.TuStatBase, SoldierSortUtilFunctions.TuStatPlus);
		pushIn("STR_STAMINA", SoldierSortUtilFunctions.StaminaStatBase, SoldierSortUtilFunctions.StaminaStatPlus);
		pushIn("STR_HEALTH", SoldierSortUtilFunctions.HealthStatBase, SoldierSortUtilFunctions.HealthStatPlus);
		pushIn("STR_BRAVERY", SoldierSortUtilFunctions.BraveryStatBase, SoldierSortUtilFunctions.BraveryStatPlus);
		pushIn("STR_REACTIONS", SoldierSortUtilFunctions.ReactionsStatBase, SoldierSortUtilFunctions.ReactionsStatPlus);
		pushIn("STR_FIRING_ACCURACY", SoldierSortUtilFunctions.FiringStatBase, SoldierSortUtilFunctions.FiringStatPlus);
		pushIn("STR_THROWING_ACCURACY", SoldierSortUtilFunctions.ThrowingStatBase, SoldierSortUtilFunctions.ThrowingStatPlus);
		pushIn("STR_MELEE_ACCURACY", SoldierSortUtilFunctions.MeleeStatBase, SoldierSortUtilFunctions.MeleeStatPlus);
		pushIn("STR_STRENGTH", SoldierSortUtilFunctions.StrengthStatBase, SoldierSortUtilFunctions.StrengthStatPlus);
		if (game.Mod.IsManaFeatureEnabled)
		{
			// "unlock" is checked later
			pushIn("STR_MANA_POOL", SoldierSortUtilFunctions.ManaStatBase, SoldierSortUtilFunctions.ManaStatPlus);
		}
		pushIn("STR_PSIONIC_STRENGTH", SoldierSortUtilFunctions.PsiStrengthStatBase, SoldierSortUtilFunctions.PsiStrengthStatPlus);
		pushIn("STR_PSIONIC_SKILL", SoldierSortUtilFunctions.PsiSkillStatBase, SoldierSortUtilFunctions.PsiSkillStatPlus);
		return sortOptions;
	}

	/// <summary>std::stable_sort with a "less" predicate.</summary>
	internal static void StableSort(List<Soldier> list, Func<Soldier, Soldier, bool> less)
	{
		var comparer = Comparer<Soldier>.Create((a, b) => less(a, b) ? -1 : less(b, a) ? 1 : 0);
		var sorted = list.OrderBy(s => s, comparer).ToList(); // OrderBy is stable
		list.Clear();
		list.AddRange(sorted);
	}

	/// <summary>
	/// Sorts the soldiers by the selected criterion (cbxSortByChange): null functor = original order.
	/// </summary>
	internal static void Sort(List<Soldier> soldiers, List<Soldier> origSoldierOrder, SortFunctor compFunc, int selIdx, bool reverse)
	{
		if (compFunc != null)
		{
			if (selIdx == 2)
			{
				StableSort(soldiers, static (a, b) => Unicode.NaturalCompare(a.GetName(), b.GetName()));
			}
			else
			{
				StableSort(soldiers, compFunc.Invoke);
			}
			if (reverse)
			{
				soldiers.Reverse();
			}
		}
		else
		{
			// restore original ordering, ignoring (of course) those
			// soldiers that have been sacked since this state started
			foreach (var origSoldier in origSoldierOrder)
			{
				int soldierIt = soldiers.IndexOf(origSoldier);
				if (soldierIt != -1)
				{
					Soldier s = soldiers[soldierIt];
					soldiers.RemoveAt(soldierIt);
					soldiers.Add(s);
				}
			}
		}
	}

	/// <summary>
	/// SDL_WarpMouse: would move the mouse pointer to the given window position (truncated to Uint16 like SDL).
	/// Port status: the host cannot move the system pointer yet, so this does nothing.
	/// </summary>
#pragma warning disable IDE0060 // parameters kept for the call sites of the C++ code
	private static void WarpMouse(int x, int y)
	{
	}
#pragma warning restore IDE0060

	/// <summary>Moves a soldier up (moveSoldierUp without the list refresh); returns true if the list must scroll up.</summary>
	internal static bool MoveUp(List<Soldier> soldiers, uint row, bool max, TextList list, Action action)
	{
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
			if (row != (uint)list.Scroll)
			{
				WarpMouse(action.LeftBlackBand + action.XMouse, action.TopBlackBand + action.YMouse - (ushort)(8 * action.YScale));
			}
			else
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Moves a soldier down (moveSoldierDown without the list refresh); returns true if the list must scroll down.</summary>
	internal static bool MoveDown(List<Soldier> soldiers, uint row, bool max, TextList list, Action action)
	{
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
			if (row != (uint)(list.VisibleRows - 1 + list.Scroll))
			{
				WarpMouse(action.LeftBlackBand + action.XMouse, action.TopBlackBand + action.YMouse + (ushort)(8 * action.YScale));
			}
			else
			{
				return true;
			}
		}
		return false;
	}
}
