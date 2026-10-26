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
// Ported from: src/Geoscape/AllocatePsiTrainingState.h, src/Geoscape/AllocatePsiTrainingState.cpp
#nullable disable

using System.Globalization;
using System.Text;

namespace Acom;

#pragma warning disable CA1822 // UI handlers stay instance members like in C++ (State._game is static here)

/// <summary>
/// Screen shown monthly to allow changing
/// soldiers currently in psi training.
/// </summary>
public class AllocatePsiTrainingState : State
{
	private readonly TextButton _btnOk;
	private readonly ToggleTextButton _btnPlus;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtTraining, _txtName, _txtRemaining;
	private readonly Text _txtPsiStrength, _txtPsiSkill;
	private readonly ComboBox _cbxSortBy;
	private readonly TextList _lstSoldiers;
#pragma warning disable IDE0052 // filled but never read, like in C++
	private readonly List<Soldier> _soldiers = [];
#pragma warning restore IDE0052
	private uint _sel;
	private int _labSpace;
	private readonly Base _base;
	private readonly List<Soldier> _origSoldierOrder;
	private readonly List<SortFunctor> _sortFunctors = [];
	private readonly List<SortFunctor> _sortFunctorsPlus = [];
	private bool _doNotReset;

	/// <summary>
	/// Initializes all the elements in the Psi Training screen.
	/// </summary>
	/// <param name="base">Pointer to the base to handle.</param>
	public AllocatePsiTrainingState(Base @base)
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
		_txtPsiStrength = new Text(80, 20, 124, 32);
		_txtPsiSkill = new Text(80, 20, 188, 32);
		_txtTraining = new Text(48, 20, 270, 32);
		_btnOk = new TextButton(148, 16, 164, 176);
		_lstSoldiers = new TextList(290, 112, 8, 52);
		_cbxSortBy = new ComboBox(this, 148, 16, 8, 176, true);
		_btnPlus = new ToggleTextButton(18, 16, 294, 8);

		// Set palette
		SetInterface("allocatePsi");

		Add(_window, "window", "allocatePsi");
		Add(_btnOk, "button", "allocatePsi");
		Add(_txtName, "text", "allocatePsi");
		Add(_txtTitle, "text", "allocatePsi");
		Add(_txtRemaining, "text", "allocatePsi");
		Add(_txtPsiStrength, "text", "allocatePsi");
		Add(_txtPsiSkill, "text", "allocatePsi");
		Add(_txtTraining, "text", "allocatePsi");
		Add(_lstSoldiers, "list", "allocatePsi");
		Add(_cbxSortBy, "button", "allocatePsi");
		Add(_btnPlus, "button", "allocatePsi");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "allocatePsi");

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
		_txtTitle.SetText(Tr("STR_PSIONIC_TRAINING"));

		_labSpace = @base.AvailablePsiLabs - @base.UsedPsiLabs;
		_txtRemaining.SetText(Tr("STR_REMAINING_PSI_LAB_CAPACITY").Arg(_labSpace));

		_txtName.SetText(Tr("STR_NAME"));

		_txtPsiStrength.SetText(Tr("STR_PSIONIC__STRENGTH"));

		_txtPsiSkill.SetText(Tr("STR_PSIONIC_SKILL_IMPROVEMENT"));

		_txtTraining.SetText(Tr("STR_IN_TRAINING"));

		_cbxSortBy.SetOptions(AllocateTrainingSort.PopulateSortOptions(this, _sortFunctors, _sortFunctorsPlus));
		_cbxSortBy.Selected = 0;
		_cbxSortBy.OnChange(CbxSortByChange);
		_cbxSortBy.SetText(Tr("STR_SORT_BY"));

		_lstSoldiers.SetArrowColumn(238, ArrowOrientation.ARROW_VERTICAL);
		_lstSoldiers.SetColumns(4, 114, 80, 66, 40);
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
		// Note: statString updates are needed only because of the potential "psiTraining" attribute change
		bool psiStrengthEval = (Options.psiStrengthEval && _game.SavedGame.IsResearched(_game.Mod.PsiRequirements));
		foreach (var soldier in _base.Soldiers)
		{
			soldier.CalcStatString(_game.Mod.StatStrings, psiStrengthEval);
		}
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
	/// Updates the soldiers list
	/// after going to other screens.
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

			var ssStr = new StringBuilder();
			var ssSkl = new StringBuilder();
			_soldiers.Add(soldier);
			if (soldier.CurrentStats.psiSkill > 0 || (Options.psiStrengthEval && _game.SavedGame.IsResearched(_game.Mod.PsiRequirements)))
			{
				ssStr.Append("   ").Append(stats.psiStrength.ToString(CultureInfo.InvariantCulture));
				if (Options.allowPsiStrengthImprovement) ssStr.Append("/+").Append(soldier.PsiStrImprovement.ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				ssStr.Append((string)Tr("STR_UNKNOWN"));
			}
			if (soldier.CurrentStats.psiSkill > 0)
			{
				ssSkl.Append(stats.psiSkill.ToString(CultureInfo.InvariantCulture)).Append("/+").Append(soldier.Improvement.ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				ssSkl.Append("0/+0");
			}
			if (soldier.Rules.TrainingStatCaps.psiSkill <= 0)
			{
				_lstSoldiers.AddRow(4, soldier.GetName(true), ssStr.ToString(), ssSkl.ToString(), Tr("STR_NO_WOUNDED"));
				_lstSoldiers.SetRowColor(row, _lstSoldiers.Color);
				if (soldier.IsInPsiTraining)
				{
					_labSpace++;
					soldier.SetPsiTraining(false);
				}
			}
			else if (soldier.IsFullyPsiTrained)
			{
				_lstSoldiers.AddRow(4, soldier.GetName(true), ssStr.ToString(), ssSkl.ToString(), Tr("STR_NO_DONE"));
				_lstSoldiers.SetRowColor(row, _lstSoldiers.Color);
				if (soldier.IsInPsiTraining)
				{
					_labSpace++;
					soldier.SetPsiTraining(false);
				}
			}
			else if (soldier.IsInPsiTraining)
			{
				_lstSoldiers.AddRow(4, soldier.GetName(true), ssStr.ToString(), ssSkl.ToString(), Tr("STR_YES"));
				_lstSoldiers.SetRowColor(row, _lstSoldiers.SecondaryColor);
			}
			else
			{
				_lstSoldiers.AddRow(4, soldier.GetName(true), ssStr.ToString(), ssSkl.ToString(), Tr("STR_NO"));
				_lstSoldiers.SetRowColor(row, _lstSoldiers.Color);
			}
			row++;
		}
		_txtRemaining.SetText(Tr("STR_REMAINING_PSI_LAB_CAPACITY").Arg(_labSpace));
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
	/// Assigns / removes a soldier from Psi Training.
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
			var s = _base.Soldiers[sel];
			if (s.Rules.TrainingStatCaps.psiSkill <= 0)
			{
				// noop
			}
			else if (s.IsFullyPsiTrained)
			{
				// can't put fully psi-trained soldiers back into psi training
			}
			else if (!s.IsInPsiTraining)
			{
				if (_base.UsedPsiLabs < _base.AvailablePsiLabs)
				{
					_lstSoldiers.SetCellText(sel, 3, Tr("STR_YES"));
					_lstSoldiers.SetRowColor(sel, _lstSoldiers.SecondaryColor);
					_labSpace--;
					_txtRemaining.SetText(Tr("STR_REMAINING_PSI_LAB_CAPACITY").Arg(_labSpace));
					s.SetPsiTraining(true);
				}
			}
			else
			{
				_lstSoldiers.SetCellText(sel, 3, Tr("STR_NO"));
				_lstSoldiers.SetRowColor(sel, _lstSoldiers.Color);
				_labSpace++;
				_txtRemaining.SetText(Tr("STR_REMAINING_PSI_LAB_CAPACITY").Arg(_labSpace));
				s.SetPsiTraining(false);
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
	/// Removes all soldiers from Psi Training.
	/// </summary>
	public void BtnDeassignAllSoldiersClick(Action action)
	{
		int row = 0;
		foreach (var s in _base.Soldiers)
		{
			s.SetPsiTraining(false);
			if (s.Rules.TrainingStatCaps.psiSkill <= 0)
			{
				_lstSoldiers.SetCellText(row, 3, Tr("STR_NO_WOUNDED"));
			}
			else if (s.IsFullyPsiTrained)
			{
				_lstSoldiers.SetCellText(row, 3, Tr("STR_NO_DONE"));
			}
			else
			{
				_lstSoldiers.SetCellText(row, 3, Tr("STR_NO"));
			}
			_lstSoldiers.SetRowColor(row, _lstSoldiers.Color);
			row++;
		}
		_labSpace = _base.AvailablePsiLabs - _base.UsedPsiLabs;
		_txtRemaining.SetText(Tr("STR_REMAINING_PSI_LAB_CAPACITY").Arg(_labSpace));
	}

	/// <summary>
	/// Adds all soldiers to Psi Training.
	/// </summary>
	public void BtnAssignAllSoldiersClick(Action action)
	{
		int row = 0;
		foreach (var s in _base.Soldiers)
		{
			if (s.Rules.TrainingStatCaps.psiSkill <= 0)
			{
				// noop
			}
			else if (s.IsFullyPsiTrained)
			{
				// can't put fully psi-trained soldiers back into psi training
			}
			else if (_labSpace > 0 && !s.IsInPsiTraining)
			{
				_lstSoldiers.SetCellText(row, 3, Tr("STR_YES"));
				_lstSoldiers.SetRowColor(row, _lstSoldiers.SecondaryColor);
				_labSpace--;
				s.SetPsiTraining(true);
			}
			row++;
		}
		_txtRemaining.SetText(Tr("STR_REMAINING_PSI_LAB_CAPACITY").Arg(_labSpace));
	}
}
