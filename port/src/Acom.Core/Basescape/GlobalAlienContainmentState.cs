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
// Ported from: src/Basescape/GlobalAlienContainmentState.h, src/Basescape/GlobalAlienContainmentState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Global Alien Containment screen that provides overview
/// of the prisoners in all the bases.
/// </summary>
public class GlobalAlienContainmentState : State
{
	private readonly TextButton _btnOk;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtTotalUsed, _txtTotalInterrogated, _txtPrisoner, _txtPrisonerAmount, _txtPrisonersInterrogated;
	private readonly TextList _lstPrisoners;
	private readonly List<(string item, Base xbase, int prisonType)> _topics = [];
	private readonly bool _openedFromBasescape;

	/// <summary>
	/// Initializes all the elements in the GlobalAlienContainment screen.
	/// </summary>
	public GlobalAlienContainmentState(bool openedFromBasescape)
	{
		_openedFromBasescape = openedFromBasescape;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnOk = new TextButton(304, 16, 8, 176);
		_txtTitle = new Text(310, 17, 5, 8);
		_txtTotalUsed = new Text(150, 9, 10, 24);
		_txtTotalInterrogated = new Text(150, 9, 130, 24);
		_txtPrisoner = new Text(146, 9, 10, 34);
		_txtPrisonerAmount = new Text(60, 9, 156, 34);
		_txtPrisonersInterrogated = new Text(80, 9, 216, 34);
		_lstPrisoners = new TextList(288, 128, 8, 44);

		// Set palette
		SetInterface("globalContainmentMenu");

		Add(_window, "window", "globalContainmentMenu");
		Add(_btnOk, "button", "globalContainmentMenu");
		Add(_txtTitle, "text", "globalContainmentMenu");
		Add(_txtTotalUsed, "text", "globalContainmentMenu");
		Add(_txtTotalInterrogated, "text", "globalContainmentMenu");
		Add(_txtPrisoner, "text", "globalContainmentMenu");
		Add(_txtPrisonerAmount, "text", "globalContainmentMenu");
		Add(_txtPrisonersInterrogated, "text", "globalContainmentMenu");
		Add(_lstPrisoners, "list", "globalContainmentMenu");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "globalContainmentMenu");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_PRISONER_OVERVIEW"));

		_txtPrisoner.SetText(Tr("STR_PRISONER"));

		_txtPrisonerAmount.Align = TextHAlign.ALIGN_CENTER;
		_txtPrisonerAmount.SetText(Tr("STR_PRISONER_AMOUNT"));

		_txtPrisonersInterrogated.Align = TextHAlign.ALIGN_CENTER;
		_txtPrisonersInterrogated.SetText(Tr("STR_PRISONER_INTERROGATED"));

		_lstPrisoners.SetColumns(3, 146, 60, 80);
		_lstPrisoners.SetSelectable(true);
		_lstPrisoners.SetBackground(_window);
		_lstPrisoners.Margin = 2;
		_lstPrisoners.SetWordWrap(true);
		_lstPrisoners.SetAlign(TextHAlign.ALIGN_CENTER, 1);
		_lstPrisoners.SetAlign(TextHAlign.ALIGN_CENTER, 2);
		_lstPrisoners.OnMouseClick(OnSelectBase, SDLMouse.SDL_BUTTON_LEFT);
		_lstPrisoners.OnMouseClick(OnOpenTechTreeViewer, SDLMouse.SDL_BUTTON_MIDDLE);
	}

	/// <summary>
	/// Updates the prisoner list
	/// after going to other screens.
	/// </summary>
	public override void Init()
	{
		base.Init();
		FillPrisonerList();
	}

	/// <summary>
	/// Fills the list with all prisoners from all bases. Also updates totals.
	/// </summary>
	public void FillPrisonerList()
	{
		_topics.Clear();
		_lstPrisoners.ClearList();

		int totalInterrogated = 0;
		int totalUsed = 0;

		// determine prison types used in the game
		SortedSet<int> prisonTypes = [0];
		bool noTypes = true;
		foreach (var facType in _game.Mod.BaseFacilitiesList)
		{
			var facRule = _game.Mod.GetBaseFacility(facType);
			if (facRule.PrisonType > 0)
			{
				prisonTypes.Add(facRule.PrisonType);
				noTypes = false;
			}
		}

		foreach (var xbase in _game.SavedGame.Bases)
		{
			bool displayed = false;
			int totalBaseCapacity = 0;

			// determine prison types used in the base
			SortedSet<int> occupiedPrisonTypes = [];
			foreach (int prisonType in prisonTypes)
			{
				totalBaseCapacity += xbase.GetAvailableContainment(prisonType);

				foreach (var baseFacility in xbase.Facilities)
				{
					if (baseFacility.Rules.Aliens > 0 && baseFacility.Rules.PrisonType == prisonType)
					{
						int usedSpace = xbase.GetUsedContainment(prisonType);
						if (usedSpace > 0)
						{
							occupiedPrisonTypes.Add(prisonType);
						}
						totalUsed += usedSpace;
						break;
					}
				}
			}

			foreach (int prisonType in occupiedPrisonTypes)
			{
				List<string> researchList = [];
				foreach (var proj in xbase.Research)
				{
					RuleResearch research = proj.Rules;
					RuleItem item = research.NeededItem;
					if (research.IsHoldingNeededItem && item != null && item.IsAlien && item.PrisonType == prisonType)
					{
						researchList.Add(research.Name);
					}
				}

				string baseNameAndPrisonType = xbase.GetName(_game.Language);
				if (!noTypes)
				{
					baseNameAndPrisonType = baseNameAndPrisonType + " - " + (string)TrAlt("STR_PRISON_TYPE", prisonType);
				}
				_lstPrisoners.AddRow(3, baseNameAndPrisonType, "", "");
				_lstPrisoners.SetRowColor(_lstPrisoners.LastRowIndex, _lstPrisoners.SecondaryColor);
				_topics.Add(("", null, 0));
				displayed = true;

				foreach (var itemType in _game.Mod.ItemsList)
				{
					RuleItem rule = _game.Mod.GetItem(itemType, true);
					if (rule.IsAlien && rule.PrisonType == prisonType)
					{
						int qty = xbase.StorageItems.GetItem(rule);
						if (qty > 0)
						{
							string ss = qty.ToString(CultureInfo.InvariantCulture);
							string rqty;
							int researchIt = researchList.IndexOf(itemType);
							if (researchIt != -1)
							{
								rqty = "1";
								researchList.RemoveAt(researchIt);
								totalInterrogated++;
							}
							else
							{
								rqty = "0";
							}

							_lstPrisoners.AddRow(3, Tr(itemType), ss, rqty);
							_topics.Add((itemType, xbase, prisonType));
						}
					}
				}

				foreach (var researchName in researchList)
				{
					_lstPrisoners.AddRow(3, Tr(researchName), "0", "1");
					_topics.Add((researchName, xbase, prisonType));
					totalInterrogated++;
				}
			}

			if (!displayed && totalBaseCapacity > 0)
			{
				_lstPrisoners.AddRow(3, xbase.GetName(_game.Language), "", "");
				_lstPrisoners.SetRowColor(_lstPrisoners.LastRowIndex, _lstPrisoners.SecondaryColor);
				_topics.Add(("", null, 0));

				_lstPrisoners.AddRow(3, Tr("STR_NONE"), "", "");
				_topics.Add(("", xbase, 0));
			}
		}

		_txtTotalUsed.SetText(Tr("STR_TOTAL_IN_PRISON").Arg(totalUsed));
		_txtTotalInterrogated.SetText(Tr("STR_TOTAL_INTERROGATED").Arg(totalInterrogated));
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Goes to the base's containment screen when clicking its prisoner
	/// </summary>
	public void OnSelectBase(Action action)
	{
		var tuple = _topics[(int)_lstPrisoners.SelectedRow];
		Base @base = tuple.xbase;
		int prisonType = tuple.prisonType;

		if (@base != null)
		{
			// close this window
			_game.PopState();

			// close Manage Alien Containment UI (goes back to BaseView)
			if (_openedFromBasescape)
			{
				_game.PopState();
			}

			// open new window
			_game.PushState(new ManageAlienContainmentState(@base, prisonType, OptionsOrigin.OPT_GEOSCAPE));
		}
	}

	/// <summary>
	/// Opens the TechTreeViewer for the corresponding topic.
	/// </summary>
	public void OnOpenTechTreeViewer(Action action)
	{
		var tuple = _topics[(int)_lstPrisoners.SelectedRow];
		RuleResearch selectedTopic = _game.Mod.GetResearch(tuple.item);

		if (selectedTopic != null)
		{
			_game.PushState(new TechTreeViewerState(selectedTopic, null));
		}
	}
}
