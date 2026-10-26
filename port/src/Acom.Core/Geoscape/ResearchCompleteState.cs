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
// Ported from: src/Geoscape/ResearchCompleteState.h, src/Geoscape/ResearchCompleteState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window which inform the player that a research project is finished.
/// Allow him to view information about the project (Ufopaedia).
/// </summary>
public class ResearchCompleteState : State
{
	private readonly Window _window;
	private readonly Text _txtTitle, _txtResearch, _txtBase;
	private readonly TextButton _btnReport, _btnOk;
	private readonly RuleResearch _research, _bonus;

	/// <summary>
	/// Initializes all the elements in the EndResearch screen.
	/// </summary>
	/// <param name="newResearch">Pointer to the completed research (or 0, if the ufopedia article shouldn't popup again).</param>
	/// <param name="bonus">Pointer to bonus unlocked research.</param>
	/// <param name="research">Pointer to the research project.</param>
	/// <param name="base">Pointer to the base where the research was completed.</param>
	public ResearchCompleteState(RuleResearch newResearch, RuleResearch bonus, RuleResearch research, Base @base)
	{
		_research = newResearch;
		_bonus = bonus;
		_screen = false;

		// Create objects
		_window = new Window(this, 230, 140, 45, 30, WindowPopup.POPUP_BOTH);
		_btnOk = new TextButton(80, 16, 64, 146);
		_btnReport = new TextButton(80, 16, 176, 146);
		_txtBase = new Text(230, 9, 45, 40);
		_txtTitle = new Text(230, 17, 45, 70);
		_txtResearch = new Text(230, 32, 45, 96);

		// Set palette
		SetInterface("geoResearchComplete");

		Add(_window, "window", "geoResearchComplete");
		Add(_btnOk, "button", "geoResearchComplete");
		Add(_btnReport, "button", "geoResearchComplete");
		Add(_txtBase, "text1", "geoResearchComplete");
		Add(_txtTitle, "text1", "geoResearchComplete");
		Add(_txtResearch, "text2", "geoResearchComplete");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "geoResearchComplete");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnReport.Text = Tr("STR_VIEW_REPORTS");
		_btnReport.OnMouseClick(BtnReportClick);
		_btnReport.OnKeyboardPress(BtnReportClick, Options.keyOk);

		_txtBase.Align = TextHAlign.ALIGN_CENTER;
		_txtBase.SetText(@base.GetName(null));
		_txtBase.Visible = Options.oxceShowBaseNameInPopups;

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_RESEARCH_COMPLETED"));

		_txtResearch.Align = TextHAlign.ALIGN_CENTER;
		_txtResearch.SetBig();
		_txtResearch.SetWordWrap(true);
		if (research != null)
		{
			_txtResearch.SetText(Tr(research.Name));
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
	/// Opens the Ufopaedia to the entry about the Research.
	/// </summary>
	public void BtnReportClick(Action action)
	{
		_game.PopState();
		string name;
		string bonusName;
		if (_bonus != null)
		{
			if (_bonus.Lookup != null)
				bonusName = _bonus.Lookup.Name;
			else
				bonusName = _bonus.Name;
			Ufopaedia.OpenArticle(_game, bonusName);
		}
		if (_research != null)
		{
			if (_research.Lookup != null)
				name = _research.Lookup.Name;
			else
				name = _research.Name;
			Ufopaedia.OpenArticle(_game, name);
		}
	}
}
