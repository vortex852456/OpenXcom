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
// Ported from: src/Basescape/SoldierTransformationListState.h, src/Basescape/SoldierTransformationListState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)
#pragma warning disable IDE0060 // handler signatures keep the unused Action parameter like C++

/// <summary>
/// Transformations Overview screen that lets the player
/// manage all the soldier transformations in a base.
/// </summary>
public class SoldierTransformationListState : State
{
	private readonly Base _base;
	private readonly ComboBox _screenActions;
	private readonly TextButton _btnOK;
	private readonly ToggleTextButton _btnOnlyEligible;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtProject, _txtNumber, _txtSoldierNumber;
	private readonly ComboBox _cbxSoldierType, _cbxSoldierStatus;
	private readonly TextEdit _btnQuickSearch;
	private readonly TextList _lstTransformations;
	private readonly List<int> _transformationIndices = [];
	private readonly List<RuleSoldierTransformation> _availableTransformations = [];

	/// <summary>
	/// Initializes all the elements in the Transformations Overview screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="screenActions">Pointer to the list of transformations in the parent SoldiersState.</param>
	public SoldierTransformationListState(Base @base, ComboBox screenActions)
	{
		_base = @base;
		_screenActions = screenActions;

		// Calculate once
		_game.SavedGame.GetAvailableTransformations(_availableTransformations, _game.Mod, _base);

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_txtTitle = new Text(310, 17, 5, 8);
		_cbxSoldierType = new ComboBox(this, 148, 16, 8, 30, false);
		_cbxSoldierStatus = new ComboBox(this, 148, 16, 164, 30, false);
		_btnQuickSearch = new TextEdit(this, 48, 9, 16, 48);
		_txtProject = new Text(140, 9, 16, 58);
		_txtNumber = new Text(78, 17, 160, 50);
		_txtSoldierNumber = new Text(78, 17, 238, 50);
		_lstTransformations = new TextList(288, 104, 8, 68);
		_btnOnlyEligible = new ToggleTextButton(148, 16, 8, 176);
		_btnOK = new TextButton(148, 16, 164, 176);

		// Set palette
		SetInterface("transformationList");

		Add(_window, "window", "transformationList");
		Add(_txtTitle, "text1", "transformationList");
		Add(_btnQuickSearch, "button", "transformationList");
		Add(_txtProject, "text2", "transformationList");
		Add(_txtNumber, "text2", "transformationList");
		Add(_txtSoldierNumber, "text2", "transformationList");
		Add(_lstTransformations, "list", "transformationList");
		Add(_btnOnlyEligible, "button", "transformationList");
		Add(_btnOK, "button", "transformationList");
		Add(_cbxSoldierType, "button2", "transformationList");
		Add(_cbxSoldierStatus, "button2", "transformationList");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "transformationList");

		_btnOnlyEligible.Text = Tr("STR_SHOW_ONLY_ELIGIBLE");
		_btnOnlyEligible.Pressed = Options.oxceBaseSoldierTransformationShowOnlyEligible;
		_btnOnlyEligible.OnMouseClick(BtnOnlyEligibleClick);

		_btnOK.Text = Tr("STR_OK");
		_btnOK.OnMouseClick(BtnOkClick);
		_btnOK.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnOK.OnKeyboardPress(BtnOkClick, Options.keyOk);

		var availableOptions = new List<string>();
		availableOptions.Add("STR_ALL_SOLDIER_TYPES");
		foreach (var soldierType in _game.Mod.SoldiersList)
		{
			availableOptions.Add(soldierType);
		}

		_cbxSoldierType.SetOptions(availableOptions, true);
		_cbxSoldierType.Selected = 0;
		_cbxSoldierType.OnChange(CbxSoldierTypeChange);

		availableOptions.Clear();
		availableOptions.Add("STR_ALL_SOLDIERS");
		availableOptions.Add("STR_HEALTHY_SOLDIERS");
		availableOptions.Add("STR_WOUNDED_SOLDIERS");
		availableOptions.Add("STR_DEAD_SOLDIERS");

		_cbxSoldierStatus.SetOptions(availableOptions, true);
		_cbxSoldierStatus.Selected = 0;
		_cbxSoldierStatus.OnChange(CbxSoldierStatusChange);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_TRANSFORMATIONS_OVERVIEW"));

		_txtProject.Align = TextHAlign.ALIGN_LEFT;
		_txtProject.SetText(Tr("STR_PROJECT_NAME"));

		_txtNumber.Align = TextHAlign.ALIGN_CENTER;
		_txtNumber.SetWordWrap(true);
		_txtNumber.SetText(Tr("STR_AVAILABLE_MATERIALS"));

		_txtSoldierNumber.Align = TextHAlign.ALIGN_CENTER;
		_txtSoldierNumber.SetWordWrap(true);
		_txtSoldierNumber.SetText(Tr("STR_ELIGIBLE_SOLDIERS"));

		_lstTransformations.SetColumns(3, 178, 10, 78);
		_lstTransformations.SetAlign(TextHAlign.ALIGN_RIGHT, 1);
		_lstTransformations.SetAlign(TextHAlign.ALIGN_RIGHT, 2);
		_lstTransformations.SetSelectable(true);
		_lstTransformations.SetBackground(_window);
		_lstTransformations.Margin = 8;
		_lstTransformations.OnMouseClick(LstTransformationsClick);
		_lstTransformations.OnMouseClick(LstTransformationsClick, SDLMouse.SDL_BUTTON_MIDDLE);

		_btnQuickSearch.Text = ""; // redraw
		_btnQuickSearch.OnChange(BtnQuickSearchApply);
		_btnQuickSearch.Visible = Options.oxceQuickSearchButton;

		_btnOK.OnKeyboardRelease(BtnQuickSearchToggle, Options.keyToggleQuickSearch);

		InitList();
	}

	/// <summary>
	/// Inits the list.
	/// </summary>
	public void InitList()
	{
		_lstTransformations.ClearList();
		_transformationIndices.Clear();

		string searchString = Unicode.UpperCase(_btnQuickSearch.Text);

		int currentIndex = -1;
		ItemContainer itemContainer = _base.StorageItems;
		foreach (var transformationRule in _availableTransformations)
		{
			++currentIndex;

			// quick search
			if (!string.IsNullOrEmpty(searchString))
			{
				string transformationName = Unicode.UpperCase(Tr(transformationRule.Name));
				if (!transformationName.Contains(searchString, StringComparison.Ordinal))
				{
					continue;
				}
			}

			if (_cbxSoldierType.Selected > 0 &&
				!transformationRule.AllowedSoldierTypes.Contains(_game.Mod.SoldiersList[_cbxSoldierType.Selected - 1]))
			{
				continue;
			}

			switch (_cbxSoldierStatus.Selected)
			{
				case 1:
					if (!transformationRule.IsAllowingAliveSoldiers) continue;
					break;
				case 2:
					if (!transformationRule.IsAllowingWoundedSoldiers) continue;
					break;
				case 3:
					if (!transformationRule.IsAllowingDeadSoldiers) continue;
					break;
				default:
					break;
			}

			string col1;
			string col2;

			// supplies calculation
			int projectsPossible = 10; // max
			if (transformationRule.Cost > 0)
			{
				int byFunds = (int)(_game.SavedGame.Funds / transformationRule.Cost);
				projectsPossible = Math.Min(projectsPossible, byFunds);
			}
			foreach (var item in transformationRule.RequiredItems)
			{
				RuleItem itemRule = _game.Mod.GetItem(item.Key);
				projectsPossible = Math.Min(projectsPossible, itemContainer.GetItem(itemRule) / item.Value);
			}
			if (projectsPossible <= 0)
			{
				col1 = "-";
			}
			else
			{
				if (projectsPossible < 10)
				{
					col1 = projectsPossible.ToString(CultureInfo.InvariantCulture);
				}
				else
				{
					col1 = "+";
				}
			}

			int eligibleSoldiers = 0;
			foreach (var soldier in _base.Soldiers)
			{
				if (soldier.Craft != null && soldier.Craft.Status == "STR_OUT")
				{
					// soldiers outside of the base are not eligible
					continue;
				}
				if (soldier.IsEligibleForTransformation(transformationRule))
				{
					++eligibleSoldiers;
				}
			}
			foreach (var deadMan in _game.SavedGame.DeadSoldiers)
			{
				if (deadMan.IsEligibleForTransformation(transformationRule))
				{
					++eligibleSoldiers;
				}
			}

			if (eligibleSoldiers <= 0)
			{
				if (_btnOnlyEligible.Pressed) continue;

				col2 = "-";
			}
			else
			{
				if (eligibleSoldiers < 10)
				{
					col2 = eligibleSoldiers.ToString(CultureInfo.InvariantCulture);
				}
				else
				{
					col2 = "+";
				}
			}

			_lstTransformations.AddRow(3, Tr(transformationRule.Name), col1, col2);
			_transformationIndices.Add(currentIndex);
		}
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
		InitList();
	}

	/// <summary>
	/// Filters the list of transformations by soldier type.
	/// </summary>
	public void CbxSoldierTypeChange(Action action)
	{
		InitList();
	}

	/// <summary>
	/// Filters the list of transformations by soldier status.
	/// </summary>
	public void CbxSoldierStatusChange(Action action)
	{
		InitList();
	}

	/// <summary>
	/// Toggles showing transformations with eligible soldiers and all transformations.
	/// </summary>
	public void BtnOnlyEligibleClick(Action action)
	{
		Options.oxceBaseSoldierTransformationShowOnlyEligible = _btnOnlyEligible.Pressed;

		InitList();
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_screenActions.Selected = 0;
		_game.PopState();
	}

	/// <summary>
	/// Selects a transformation (L-click) or shows info about a transformation (M-click).
	/// </summary>
	public void LstTransformationsClick(Action action)
	{
		int transformationIndex = _transformationIndices[(int)_lstTransformations.SelectedRow];

		if (action.Details.button.button == SDLMouse.SDL_BUTTON_MIDDLE)
		{
			string articleId = _availableTransformations[transformationIndex].Name;
			Ufopaedia.OpenArticle(_game, articleId);
			return;
		}

		_screenActions.Selected = _screenActions.Selected + transformationIndex + 1;
		_game.PopState();
	}
}
