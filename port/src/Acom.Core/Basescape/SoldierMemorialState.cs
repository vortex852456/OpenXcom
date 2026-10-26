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
// Ported from: src/Basescape/SoldierMemorialState.h, src/Basescape/SoldierMemorialState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)
#pragma warning disable IDE0060 // handler signatures keep the unused Action parameter like C++

/// <summary>
/// Screen that shows all the soldiers
/// that have died throughout the game.
/// </summary>
public class SoldierMemorialState : State
{
	private readonly TextButton _btnOk, _btnStatistics;
	private readonly TextEdit _btnQuickSearch;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtName, _txtRank, _txtDate, _txtRecruited, _txtLost;
	private readonly TextList _lstSoldiers;
	private readonly List<int> _indices = [];

	/// <summary>
	/// Initializes all the elements in the Soldier Memorial screen.
	/// </summary>
	public SoldierMemorialState()
	{
		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnQuickSearch = new TextEdit(this, 48, 9, 16, 10);
		_btnOk = new TextButton(148, 16, 164, 176);
		_btnStatistics = new TextButton(148, 16, 8, 176);
		_txtTitle = new Text(310, 17, 5, 8);
		_txtName = new Text(114, 9, 16, 36);
		_txtRank = new Text(102, 9, 130, 36);
		_txtDate = new Text(90, 9, 218, 36);
		_txtRecruited = new Text(150, 9, 16, 24);
		_txtLost = new Text(150, 9, 160, 24);
		_lstSoldiers = new TextList(288, 120, 8, 44);

		// Set palette
		SetInterface("soldierMemorial");

		Add(_window, "window", "soldierMemorial");
		Add(_btnQuickSearch, "button", "soldierMemorial");
		Add(_btnOk, "button", "soldierMemorial");
		Add(_btnStatistics, "button", "soldierMemorial");
		Add(_txtTitle, "text", "soldierMemorial");
		Add(_txtName, "text", "soldierMemorial");
		Add(_txtRank, "text", "soldierMemorial");
		Add(_txtDate, "text", "soldierMemorial");
		Add(_txtRecruited, "text", "soldierMemorial");
		Add(_txtLost, "text", "soldierMemorial");
		Add(_lstSoldiers, "list", "soldierMemorial");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "soldierMemorial");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_btnStatistics.Text = Tr("STR_STATISTICS");
		_btnStatistics.OnMouseClick(BtnStatisticsClick);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_MEMORIAL"));

		_txtName.SetText(Tr("STR_NAME_UC"));

		_txtRank.SetText(Tr("STR_RANK"));

		_txtDate.SetText(Tr("STR_DATE_UC"));

		int lost = _game.SavedGame.DeadSoldiers.Count;
		int recruited = lost;
		foreach (var xbase in _game.SavedGame.Bases)
		{
			recruited += xbase.TotalSoldiers;
		}

		_txtRecruited.SetText(Tr("STR_SOLDIERS_RECRUITED_UC").Arg(recruited));

		_txtLost.SetText(Tr("STR_SOLDIERS_LOST_UC").Arg(lost));

		_lstSoldiers.SetColumns(5, 114, 88, 30, 25, 35);
		_lstSoldiers.SetSelectable(true);
		_lstSoldiers.SetBackground(_window);
		_lstSoldiers.Margin = 8;
		_lstSoldiers.OnMouseClick(LstSoldiersClick);

		_btnQuickSearch.Text = ""; // redraw
		_btnQuickSearch.OnChange(BtnQuickSearchApply);
		_btnQuickSearch.Visible = Options.oxceQuickSearchButton;

		_btnOk.OnKeyboardRelease(BtnQuickSearchToggle, Options.keyToggleQuickSearch);
	}

	/// <summary>
	/// Initializes the screen (fills the list).
	/// </summary>
	public override void Init()
	{
		base.Init();

		FillMemorialList();
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
		_game.Mod.PlayMusic("GMGEO");
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
		FillMemorialList();
	}

	/// <summary>
	/// Shows the Statistics screen.
	/// </summary>
	public void BtnStatisticsClick(Action action)
	{
		_game.PushState(new StatisticsState());
	}

	/// <summary>
	/// Shows the selected soldier's info.
	/// </summary>
	public void LstSoldiersClick(Action action)
	{
		_game.PushState(new SoldierInfoState(null, _indices[(int)_lstSoldiers.SelectedRow]));
	}

	/// <summary>
	/// Fills the list with filtered memorial entries.
	/// </summary>
	private void FillMemorialList()
	{
		string searchString = Unicode.UpperCase(_btnQuickSearch.Text);

		_lstSoldiers.ClearList();
		_indices.Clear();

		int index = -1;
		var deadSoldiers = _game.SavedGame.DeadSoldiers;
		for (int i = deadSoldiers.Count - 1; i >= 0; --i)
		{
			++index;
			Soldier deadSoldier = deadSoldiers[i];

			// quick search
			if (!string.IsNullOrEmpty(searchString))
			{
				string soldierName = Unicode.UpperCase(deadSoldier.GetName());
				if (!soldierName.Contains(searchString, StringComparison.Ordinal))
				{
					continue;
				}
			}

			SoldierDeath death = deadSoldier.Death;

			string saveDay = death.Time.GetDayString(_game.Language);
			string saveMonth = Tr(death.Time.MonthString);
			string saveYear = death.Time.Year.ToString(CultureInfo.InvariantCulture);
			_lstSoldiers.AddRow(5, deadSoldier.GetName(), Tr(deadSoldier.RankString), saveDay, saveMonth, saveYear);
			_indices.Add(index);
		}
	}
}
