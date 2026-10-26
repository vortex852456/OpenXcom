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
// Ported from: src/Basescape/GlobalManufactureState.h, src/Basescape/GlobalManufactureState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Global Manufacture screen that provides overview
/// of the ongoing manufacturing operations in all the bases.
/// </summary>
public class GlobalManufactureState : State
{
	private readonly TextButton _btnOk;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtAvailable, _txtAllocated, _txtSpace, _txtFunds, _txtItem, _txtEngineers, _txtProduced, _txtCost, _txtTimeLeft;
	private readonly TextList _lstManufacture;
	private readonly List<Base> _bases = [];
	private readonly List<RuleManufacture> _topics = [];
	private readonly bool _openedFromBasescape;

	/// <summary>
	/// Initializes all the elements in the GlobalManufacture screen.
	/// </summary>
	public GlobalManufactureState(bool openedFromBasescape)
	{
		_openedFromBasescape = openedFromBasescape;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnOk = new TextButton(304, 16, 8, 176);
		_txtTitle = new Text(310, 17, 5, 8);
		_txtAvailable = new Text(150, 9, 8, 24);
		_txtAllocated = new Text(150, 9, 160, 24);
		_txtSpace = new Text(150, 9, 8, 34);
		_txtFunds = new Text(150, 9, 160, 34);
		_txtItem = new Text(80, 9, 10, 52);
		_txtEngineers = new Text(56, 18, 112, 44);
		_txtProduced = new Text(56, 18, 168, 44);
		_txtCost = new Text(44, 27, 222, 44);
		_txtTimeLeft = new Text(60, 27, 260, 44);
		_lstManufacture = new TextList(288, 88, 8, 80);

		// Set palette
		SetInterface("globalManufactureMenu");

		Add(_window, "window", "globalManufactureMenu");
		Add(_btnOk, "button", "globalManufactureMenu");
		Add(_txtTitle, "text1", "globalManufactureMenu");
		Add(_txtAvailable, "text1", "globalManufactureMenu");
		Add(_txtAllocated, "text1", "globalManufactureMenu");
		Add(_txtSpace, "text1", "globalManufactureMenu");
		Add(_txtFunds, "text1", "globalManufactureMenu");
		Add(_txtItem, "text2", "globalManufactureMenu");
		Add(_txtEngineers, "text2", "globalManufactureMenu");
		Add(_txtProduced, "text2", "globalManufactureMenu");
		Add(_txtCost, "text2", "globalManufactureMenu");
		Add(_txtTimeLeft, "text2", "globalManufactureMenu");
		Add(_lstManufacture, "list", "globalManufactureMenu");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "globalManufactureMenu");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_PRODUCTION_OVERVIEW"));

		_txtFunds.SetText(Tr("STR_CURRENT_FUNDS").Arg(Unicode.FormatFunding(_game.SavedGame.Funds)));

		_txtItem.SetText(Tr("STR_ITEM"));

		_txtEngineers.SetText(Tr("STR_ENGINEERS__ALLOCATED"));
		_txtEngineers.SetWordWrap(true);

		_txtProduced.SetText(Tr("STR_UNITS_PRODUCED"));
		_txtProduced.SetWordWrap(true);

		_txtCost.SetText(Tr("STR_COST__PER__UNIT"));
		_txtCost.SetWordWrap(true);

		_txtTimeLeft.SetText(Tr("STR_DAYS_HOURS_LEFT"));
		_txtTimeLeft.SetWordWrap(true);

		_lstManufacture.SetColumns(5, 114, 16, 52, 56, 48);
		_lstManufacture.SetAlign(TextHAlign.ALIGN_RIGHT);
		_lstManufacture.SetAlign(TextHAlign.ALIGN_LEFT, 0);
		_lstManufacture.SetSelectable(true);
		_lstManufacture.SetBackground(_window);
		_lstManufacture.Margin = 2;
		_lstManufacture.SetWordWrap(true);
		_lstManufacture.OnMouseClick(OnSelectBase, SDLMouse.SDL_BUTTON_LEFT);
		_lstManufacture.OnMouseClick(OnOpenTechTreeViewer, SDLMouse.SDL_BUTTON_MIDDLE);
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Goes to the base's manufacture screen when clicking its project
	/// </summary>
	public void OnSelectBase(Action action)
	{
		Base @base = _bases[(int)_lstManufacture.SelectedRow];

		if (@base != null)
		{
			// close this window
			_game.PopState();

			// close Manufacture UI (goes back to BaseView)
			if (_openedFromBasescape)
			{
				_game.PopState();
			}

			// open new window
			_game.PushState(new ManufactureState(@base));
		}
	}

	/// <summary>
	/// Opens the TechTreeViewer for the corresponding topic.
	/// </summary>
	public void OnOpenTechTreeViewer(Action action)
	{
		RuleManufacture selectedTopic = _topics[(int)_lstManufacture.SelectedRow];

		if (selectedTopic != null)
		{
			if (_game.IsCtrlPressed())
			{
				string articleId = selectedTopic.Name;
				Ufopaedia.OpenArticle(_game, articleId);
			}
			else
			{
				_game.PushState(new TechTreeViewerState(null, selectedTopic));
			}
		}
	}

	/// <summary>
	/// Updates the production list
	/// after going to other screens.
	/// </summary>
	public override void Init()
	{
		base.Init();
		FillProductionList();
	}

	/// <summary>
	/// Fills the list with Productions from all bases. Also updates total count of available workshop space and available/allocated engineers.
	/// </summary>
	public void FillProductionList()
	{
		_bases.Clear();
		_topics.Clear();
		_lstManufacture.ClearList();

		int availableEngineers = 0;
		int allocatedEngineers = 0;
		int freeWorkshops = 0;

		foreach (Base xbase in _game.SavedGame.Bases)
		{
			var baseProductions = xbase.Productions;
			if (baseProductions.Count != 0 || xbase.Engineers > 0)
			{
				string baseName = xbase.GetName(_game.Language);
				_lstManufacture.AddRow(3, baseName, "", "");
				_lstManufacture.SetRowColor(_lstManufacture.LastRowIndex, _lstManufacture.SecondaryColor);

				// dummy
				_bases.Add(null);
				_topics.Add(null);
			}
			foreach (var prod in baseProductions)
			{
				var (s1, s2, s3, s4) = ManufactureState.FormatProduction(prod);
				_lstManufacture.AddRow(5, Tr(prod.Rules.Name), s1, s2, s3, s4);

				_bases.Add(xbase);
				_topics.Add(prod.Rules);
			}
			if (baseProductions.Count == 0 && xbase.Engineers > 0)
			{
				_lstManufacture.AddRow(5, Tr("STR_NONE"), "", "", "", "");

				_bases.Add(xbase);
				_topics.Add(null);
			}

			availableEngineers += xbase.AvailableEngineers;
			allocatedEngineers += xbase.AllocatedEngineers;
			freeWorkshops += xbase.FreeWorkshops;
		}

		_txtAvailable.SetText(Tr("STR_ENGINEERS_AVAILABLE").Arg(availableEngineers));
		_txtAllocated.SetText(Tr("STR_ENGINEERS_ALLOCATED").Arg(allocatedEngineers));
		_txtSpace.SetText(Tr("STR_WORKSHOP_SPACE_AVAILABLE").Arg(freeWorkshops));
	}
}
