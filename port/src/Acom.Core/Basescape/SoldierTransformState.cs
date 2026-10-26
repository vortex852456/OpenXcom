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
// Ported from: src/Basescape/SoldierTransformState.h, src/Basescape/SoldierTransformState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)
#pragma warning disable IDE0060 // handler signatures keep the unused Action parameter like C++

/// <summary>Sort order of the transformation list.</summary>
public enum TransformationSort : uint
{
	TRANSFORM_SORT_NONE = 0,
	TRANSFORM_SORT_NAME_ASC = 1,
	TRANSFORM_SORT_NAME_DESC = 2,
}

/// <summary>A transformation in the selection list.</summary>
public class TransformationItem
{
	public string type;
	public string name;

	public TransformationItem(string _type, string _name)
	{
		type = _type;
		name = _name;
	}
}

/// <summary>
/// Soldier Transform window that allows performing a transformation on a soldier.
/// </summary>
public class SoldierTransformState : State
{
	private readonly Base _base;
	private readonly int _soldier; // C++ size_t
	private readonly TextButton _btnCancel;
	private readonly TextEdit _btnQuickSearch;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtType;
	private readonly TextList _lstTransformations;
	private readonly ArrowButton _sortName;
	private readonly List<TransformationItem> _transformations = [];
	private readonly List<int> _indices = [];
	private TransformationSort _transformationOrder;

	/// <summary>C++ compareTransformationName.</summary>
	private static bool CompareTransformationName(TransformationItem a, TransformationItem b) => Unicode.NaturalCompare(a.name, b.name);

	/// <summary>
	/// Initializes all the elements in the Soldier Transform window.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="soldier">ID of the selected soldier.</param>
	public SoldierTransformState(Base @base, int soldier)
	{
		_base = @base;
		_soldier = soldier;
		_screen = false;

		// Create objects
		_window = new Window(this, 192, 160, 64, 20, WindowPopup.POPUP_BOTH);
		_btnQuickSearch = new TextEdit(this, 48, 9, 80, 43);
		_btnCancel = new TextButton(140, 16, 90, 156);
		_txtTitle = new Text(182, 16, 69, 28);
		_txtType = new Text(90, 9, 80, 52);
		_lstTransformations = new TextList(160, 80, 73, 68);
		_sortName = new ArrowButton(ArrowShape.ARROW_NONE, 11, 8, 80, 52);

		// Set palette
		SetInterface("soldierTransform");

		Add(_window, "window", "soldierTransform");
		Add(_btnQuickSearch, "button", "soldierTransform");
		Add(_btnCancel, "button", "soldierTransform");
		Add(_txtTitle, "text", "soldierTransform");
		Add(_txtType, "text", "soldierTransform");
		Add(_lstTransformations, "list", "soldierTransform");
		Add(_sortName, "text", "soldierTransform");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "soldierTransform");

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		Soldier s = _base.Soldiers[_soldier];
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_SELECT_TRANSFORMATION_FOR").Arg(s.GetName()));

		_txtType.SetText(Tr("STR_TYPE"));

		_lstTransformations.SetColumns(1, 153);
		_lstTransformations.SetSelectable(true);
		_lstTransformations.SetBackground(_window);
		_lstTransformations.Margin = 8;

		_sortName.X = _sortName.X + _txtType.GetTextWidth() + 4;
		_sortName.OnMouseClick(SortNameClick);

		var availableTransformations = new List<RuleSoldierTransformation>();
		_game.SavedGame.GetAvailableTransformations(availableTransformations, _game.Mod, _base);

		foreach (var trRule in availableTransformations)
		{
			if (!s.IsEligibleForTransformation(trRule))
				continue;

			_transformations.Add(new TransformationItem(trRule.Name, Tr(trRule.Name)));
		}

		_btnQuickSearch.Text = ""; // redraw
		_btnQuickSearch.OnChange(BtnQuickSearchApply);
		_btnQuickSearch.Visible = Options.oxceQuickSearchButton;

		_btnCancel.OnKeyboardRelease(BtnQuickSearchToggle, Options.keyToggleQuickSearch);

		_transformationOrder = TransformationSort.TRANSFORM_SORT_NONE;
		SortList();

		_lstTransformations.OnMouseClick(LstTransformationClick);
		_lstTransformations.OnMouseClick(LstTransformationClickMiddle, SDLMouse.SDL_BUTTON_MIDDLE);
	}

	/// <summary>
	/// Updates the sorting arrows based on the current setting.
	/// </summary>
	private void UpdateArrows()
	{
		_sortName.SetShape(ArrowShape.ARROW_NONE);
		switch (_transformationOrder)
		{
			case TransformationSort.TRANSFORM_SORT_NAME_ASC:
				_sortName.SetShape(ArrowShape.ARROW_SMALL_UP);
				break;
			case TransformationSort.TRANSFORM_SORT_NAME_DESC:
				_sortName.SetShape(ArrowShape.ARROW_SMALL_DOWN);
				break;
			default:
				break;
		}
	}

	/// <summary>
	/// Sorts the transformations list.
	/// </summary>
	public void SortList()
	{
		UpdateArrows();

		switch (_transformationOrder)
		{
			case TransformationSort.TRANSFORM_SORT_NAME_ASC:
				SoldiersState.StableSort(_transformations, CompareTransformationName);
				break;
			case TransformationSort.TRANSFORM_SORT_NAME_DESC:
				// C++ stable_sort over reverse iterators
				_transformations.Reverse();
				SoldiersState.StableSort(_transformations, CompareTransformationName);
				_transformations.Reverse();
				break;
			default:
				break;
		}

		UpdateList();
	}

	/// <summary>
	/// Updates the transformations list.
	/// </summary>
	public void UpdateList()
	{
		string searchString = Unicode.UpperCase(_btnQuickSearch.Text);

		_lstTransformations.ClearList();
		_indices.Clear();

		int index = -1;
		foreach (var transformationItem in _transformations)
		{
			++index;

			// quick search
			if (!string.IsNullOrEmpty(searchString))
			{
				string trType = Unicode.UpperCase(transformationItem.name);
				if (!trType.Contains(searchString, StringComparison.Ordinal))
				{
					continue;
				}
			}

			_lstTransformations.AddRow(1, transformationItem.name);
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
	/// Opens the SoldierTransformation state.
	/// </summary>
	public void LstTransformationClick(Action action)
	{
		string trType = _transformations[_indices[(int)_lstTransformations.SelectedRow]].type;
		RuleSoldierTransformation transformationRule = _game.Mod.GetSoldierTransformation(trType, false);

		if (transformationRule != null)
		{
			Soldier soldier = _base.Soldiers[_soldier];

			_game.PopState();
			_game.PushState(new SoldierTransformationState(transformationRule, _base, soldier, null));
		}
	}

	/// <summary>
	/// Shows the corresponding Ufopaedia article.
	/// </summary>
	public void LstTransformationClickMiddle(Action action)
	{
		string trType = _transformations[_indices[(int)_lstTransformations.SelectedRow]].type;
		RuleSoldierTransformation transformationRule = _game.Mod.GetSoldierTransformation(trType, false);

		if (transformationRule != null)
		{
			Ufopaedia.OpenArticle(_game, transformationRule.Name);
		}
	}

	/// <summary>
	/// Sorts the transformations by name.
	/// </summary>
	public void SortNameClick(Action action)
	{
		_transformationOrder = _transformationOrder == TransformationSort.TRANSFORM_SORT_NAME_ASC ? TransformationSort.TRANSFORM_SORT_NAME_DESC : TransformationSort.TRANSFORM_SORT_NAME_ASC;
		SortList();
	}
}
