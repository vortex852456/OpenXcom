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
// Ported from: src/Basescape/ManufactureDependenciesTreeState.h, src/Basescape/ManufactureDependenciesTreeState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window which displays manufacture dependencies tree.
/// </summary>
public class ManufactureDependenciesTreeState : State
{
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly TextList _lstTopics;
	private readonly TextButton _btnOk, _btnShowAll;
	private readonly string _selectedItem;
	private bool _showAll;

	/// <summary>
	/// Initializes all the elements on the UI.
	/// </summary>
	public ManufactureDependenciesTreeState(string selectedItem)
	{
		_selectedItem = selectedItem;
		_showAll = false;

		_screen = false;

		_window = new Window(this, 222, 144, 49, 32);
		_txtTitle = new Text(182, 9, 53, 42);
		_lstTopics = new TextList(198, 96, 53, 54);
		_btnShowAll = new TextButton(100, 16, 57, 153);
		_btnOk = new TextButton(100, 16, 163, 153);

		// Set palette
		SetInterface("dependencyTree");

		Add(_window, "window", "dependencyTree");
		Add(_txtTitle, "text", "dependencyTree");
		Add(_btnShowAll, "button", "dependencyTree");
		Add(_btnOk, "button", "dependencyTree");
		Add(_lstTopics, "list", "dependencyTree");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "dependencyTree");

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_TOPIC").Arg(Tr(_selectedItem)));

		_btnShowAll.Text = Tr("STR_SHOW_ALL");
		_btnShowAll.OnMouseClick(BtnShowAllClick);

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_lstTopics.SetColumns(1, 182);
		_lstTopics.SetBackground(_window);
		_lstTopics.Margin = 0;
		_lstTopics.SetAlign(TextHAlign.ALIGN_CENTER);

		if (Options.oxceDisableProductionDependencyTree)
		{
			_txtTitle.SetHeight(_txtTitle.Height * 11);
			_txtTitle.SetWordWrap(true);
			_txtTitle.SetText(Tr("STR_THIS_FEATURE_IS_DISABLED_3"));
			_btnShowAll.Visible = false;
			_lstTopics.Visible = false;
			return;
		}
	}

	/// <summary>
	/// Initializes the screen (fills the list).
	/// </summary>
	public override void Init()
	{
		base.Init();

		if (!Options.oxceDisableProductionDependencyTree)
		{
			InitList();
		}
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Shows spoilers.
	/// </summary>
	public void BtnShowAllClick(Action action)
	{
		_showAll = true;
		_btnOk.SetWidth(_btnOk.X - _btnShowAll.X + _btnOk.Width);
		_btnOk.X = _btnShowAll.X;
		_btnShowAll.Visible = false;
		InitList();
	}

	/// <summary>
	/// Builds the dependency map (item -> list of manufacture projects that need this item).
	/// </summary>
	internal static Dictionary<string, List<string>> BuildDependencies(Mod mod)
	{
		Dictionary<string, List<string>> deps = new(StringComparer.Ordinal);

		foreach (var manufName in mod.ManufactureList)
		{
			RuleManufacture rule = mod.GetManufacture(manufName);
			foreach (var pair in rule.RequiredItems)
			{
				GetOrAdd(deps, pair.Key.Type).Add(manufName);
			}
		}
		return deps;
	}

	/// <summary>std::unordered_map::operator[] (inserts an empty list for a missing key).</summary>
	private static List<string> GetOrAdd(Dictionary<string, List<string>> deps, string key)
	{
		if (!deps.TryGetValue(key, out var list))
		{
			list = [];
			deps[key] = list;
		}
		return list;
	}

	/// <summary>
	/// Adds the rows of one search level and collects the next level.
	/// </summary>
	private void AddLevel(List<string> level, List<string> nextLevel, Dictionary<string, List<string>> deps, HashSet<string> alreadyVisited, ref int row)
	{
		foreach (var name in level)
		{
			if (_showAll || _game.SavedGame.IsResearched(_game.Mod.GetManufacture(name).Requirements))
			{
				_lstTopics.AddRow(1, Tr(name));
			}
			else
			{
				_lstTopics.AddRow(1, "***");
			}
			++row;

			foreach (var goDeeper in GetOrAdd(deps, name))
			{
				if (!alreadyVisited.Contains(goDeeper))
				{
					nextLevel.Add(goDeeper);
					alreadyVisited.Add(goDeeper);
				}
			}
		}
	}

	/// <summary>
	/// Ends the current level; returns true (after adding the end marker) when the next level is empty.
	/// </summary>
	private bool EndLevel(List<string> nextLevel, string nextTitle, ref int row)
	{
		_lstTopics.AddRow(1, "");
		++row;
		if (nextLevel.Count == 0)
		{
			_lstTopics.AddRow(1, Tr("STR_END_OF_SEARCH"));
			_lstTopics.SetRowColor(row, _lstTopics.SecondaryColor);
			++row;
			return true;
		}

		_lstTopics.AddRow(1, Tr(nextTitle));
		_lstTopics.SetRowColor(row, _lstTopics.SecondaryColor);
		++row;
		return false;
	}

	/// <summary>
	/// Shows the tree.
	/// </summary>
	private void InitList()
	{
		_lstTopics.ClearList();

		// dependency map (item -> vector of items that needs this item)
		var deps = BuildDependencies(_game.Mod);

		// breadth-first tree search
		int row = 0;
		List<string> firstLevel = [.. GetOrAdd(deps, _selectedItem)];
		List<string> secondLevel = [];
		List<string> thirdLevel = [];
		List<string> fourthLevel = [];
		List<string> fifthLevel = [];

		HashSet<string> alreadyVisited = new(StringComparer.Ordinal);
		alreadyVisited.Add(_selectedItem);
		foreach (var name in firstLevel)
		{
			alreadyVisited.Add(name);
		}

		List<RuleBaseFacility> facilitiesLevel = [];
		foreach (var facilityType in _game.Mod.BaseFacilitiesList)
		{
			RuleBaseFacility facilityRule = _game.Mod.GetBaseFacility(facilityType);
			foreach (var itemRequired in facilityRule.BuildCostItems)
			{
				if (itemRequired.Key == _selectedItem)
				{
					facilitiesLevel.Add(facilityRule);
					break;
				}
			}
		}

		if (firstLevel.Count == 0 && facilitiesLevel.Count == 0)
		{
			_lstTopics.AddRow(1, Tr("STR_NO_DEPENDENCIES"));
			_lstTopics.SetRowColor(row, _lstTopics.SecondaryColor);
			++row;
			return;
		}

		// first level
		_lstTopics.AddRow(1, Tr("STR_DIRECT_DEPENDENCIES"));
		_lstTopics.SetRowColor(row, _lstTopics.SecondaryColor);
		++row;

		// first list all the dependent base facilities
		foreach (var fac in facilitiesLevel)
		{
			if (_showAll || _game.SavedGame.IsResearched(fac.Requirements))
			{
				_lstTopics.AddRow(1, Tr(fac.Type));
			}
			else
			{
				_lstTopics.AddRow(1, "***");
			}
			++row;
		}

		AddLevel(firstLevel, secondLevel, deps, alreadyVisited, ref row);
		if (EndLevel(secondLevel, "STR_LEVEL_2_DEPENDENCIES", ref row))
			return;

		// second level
		AddLevel(secondLevel, thirdLevel, deps, alreadyVisited, ref row);
		if (EndLevel(thirdLevel, "STR_LEVEL_3_DEPENDENCIES", ref row))
			return;

		// third level
		AddLevel(thirdLevel, fourthLevel, deps, alreadyVisited, ref row);
		if (EndLevel(fourthLevel, "STR_LEVEL_4_DEPENDENCIES", ref row))
			return;

		// fourth level
		AddLevel(fourthLevel, fifthLevel, deps, alreadyVisited, ref row);
		if (EndLevel(fifthLevel, "STR_MORE_DEPENDENCIES", ref row))
			return;
	}
}
