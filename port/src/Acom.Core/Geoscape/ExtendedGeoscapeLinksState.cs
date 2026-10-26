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
// Ported from: src/Geoscape/ExtendedGeoscapeLinksState.h, src/Geoscape/ExtendedGeoscapeLinksState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window with additional OXCE Geoscape links.
/// </summary>
public class ExtendedGeoscapeLinksState : State
{
	private readonly TextButton _btnOk;
	private readonly TextButton _btnFunding, _btnTechTree, _btnGlobalResearch, _btnGlobalProduction, _btnUfoTracker, _btnPilotExp, _btnNotes, _btnMusic;
	private readonly TextButton _btnTest;
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly GeoscapeState _parent;

	/// <summary>
	/// Initializes all the elements in the ExtendedGeoscapeLinksState screen.
	/// </summary>
	/// <param name="parent">Pointer to parent Geoscape state.</param>
	public ExtendedGeoscapeLinksState(GeoscapeState parent)
	{
		_parent = parent;
		_screen = false;

		// Create objects
		_window = new Window(this, 256, 180, 32, 10, WindowPopup.POPUP_BOTH);
		_txtTitle = new Text(220, 17, 50, 33);
		if (Options.oxceFatFingerLinks)
		{
			_btnFunding = new TextButton(116, 25, 44, 50);
			_btnTechTree = new TextButton(116, 25, 161, 50);
			_btnGlobalResearch = new TextButton(116, 25, 44, 76);
			_btnGlobalProduction = new TextButton(116, 25, 161, 76);
			_btnUfoTracker = new TextButton(116, 25, 44, 102);
			_btnPilotExp = new TextButton(116, 25, 161, 102);
			_btnNotes = new TextButton(116, 25, 44, 128);
			_btnMusic = new TextButton(116, 25, 161, 128);
			_btnTest = new TextButton(116, 25, 44, 154);
			_btnOk = new TextButton(116, 25, 161, 154);
		}
		else
		{
			_btnFunding = new TextButton(220, 12, 50, 50);
			_btnTechTree = new TextButton(220, 12, 50, 63);
			_btnGlobalResearch = new TextButton(220, 12, 50, 76);
			_btnGlobalProduction = new TextButton(220, 12, 50, 89);
			_btnUfoTracker = new TextButton(220, 12, 50, 102);
			_btnPilotExp = new TextButton(220, 12, 50, 115);
			_btnNotes = new TextButton(220, 12, 50, 128);
			_btnMusic = new TextButton(220, 12, 50, 141);
			_btnTest = new TextButton(220, 12, 50, 154);
			_btnOk = new TextButton(220, 12, 50, 167);
		}

		// Set palette
		SetInterface("oxceLinks");

		Add(_window, "window", "oxceLinks");
		Add(_txtTitle, "text", "oxceLinks");
		Add(_btnOk, "button", "oxceLinks");
		Add(_btnFunding, "button", "oxceLinks");
		Add(_btnTechTree, "button", "oxceLinks");
		Add(_btnGlobalResearch, "button", "oxceLinks");
		Add(_btnGlobalProduction, "button", "oxceLinks");
		Add(_btnUfoTracker, "button", "oxceLinks");
		Add(_btnPilotExp, "button", "oxceLinks");
		Add(_btnNotes, "button", "oxceLinks");
		Add(_btnMusic, "button", "oxceLinks");
		Add(_btnTest, "button", "oxceLinks");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "oxceLinks");

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_EXTENDED_LINKS"));

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_btnFunding.Text = Tr("STR_FUNDING_UC");
		_btnFunding.OnMouseClick(BtnFundingClick);

		string tmp = Tr("STR_TECH_TREE_VIEWER");
		tmp = Unicode.UpperCase(tmp);
		_btnTechTree.Text = tmp;
		_btnTechTree.OnMouseClick(BtnTechTreeClick);

		_btnGlobalResearch.Text = Tr("STR_RESEARCH_OVERVIEW");
		_btnGlobalResearch.OnMouseClick(BtnGlobalResearchClick);

		_btnGlobalProduction.Text = Tr("STR_PRODUCTION_OVERVIEW");
		_btnGlobalProduction.OnMouseClick(BtnGlobalProductionClick);

		tmp = Tr("STR_UFO_TRACKER");
		tmp = Unicode.UpperCase(tmp);
		_btnUfoTracker.Text = tmp;
		_btnUfoTracker.OnMouseClick(BtnUfoTrackerClick);

		tmp = Tr("STR_DAILY_PILOT_EXPERIENCE");
		tmp = Unicode.UpperCase(tmp);
		_btnPilotExp.Text = tmp;
		_btnPilotExp.OnMouseClick(BtnPilotExpClick);

		tmp = Tr("STR_NOTES");
		tmp = Unicode.UpperCase(tmp);
		_btnNotes.Text = tmp;
		_btnNotes.OnMouseClick(BtnNotesClick);

		tmp = Tr("STR_SELECT_MUSIC_TRACK");
		tmp = Unicode.UpperCase(tmp);
		_btnMusic.Text = tmp;
		_btnMusic.OnMouseClick(BtnMusicClick);

		if (Options.debug)
		{
			_btnTest.Text = Tr("STR_TOGGLE_DEBUG_MODE");
		}
		else
		{
			_btnTest.Text = Tr("STR_TEST_SCREEN");
		}
		_btnTest.OnMouseClick(BtnTestClick);
	}

	public void BtnFundingClick(Action action)
	{
		_game.PopState();
		_game.PushState(new FundingState());
	}

	public void BtnTechTreeClick(Action action)
	{
		_game.PopState();
		_parent.BtnTechTreeViewerClick(null);
	}

	public void BtnGlobalResearchClick(Action action)
	{
		_game.PopState();
		_parent.BtnGlobalResearchClick(null);
	}

	public void BtnGlobalProductionClick(Action action)
	{
		_game.PopState();
		_parent.BtnGlobalProductionClick(null);
	}

	public void BtnUfoTrackerClick(Action action)
	{
		_game.PopState();
		_parent.BtnUfoTrackerClick(null);
	}

	public void BtnPilotExpClick(Action action)
	{
		_game.PopState();
		_parent.BtnDogfightExperienceClick(null);
	}

	public void BtnNotesClick(Action action)
	{
		_game.PopState();
		_game.PushState(new NotesState(OptionsOrigin.OPT_GEOSCAPE));
	}

	public void BtnMusicClick(Action action)
	{
		_game.PopState();
		_parent.BtnSelectMusicTrackClick(null);
	}

	public void BtnTestClick(Action action)
	{
		_game.PopState();
		if (Options.debug)
		{
			_parent.BtnDebugClick(null);
		}
		else
		{
			_game.PushState(new TestState());
		}
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}
}
