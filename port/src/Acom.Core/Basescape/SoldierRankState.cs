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
// Ported from: src/Basescape/SoldierRankState.h, src/Basescape/SoldierRankState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)
#pragma warning disable IDE0060 // handler signatures keep the unused Action parameter like C++

/// <summary>A rank in the manual promotion list.</summary>
public class RankItem
{
	public SoldierRank rank;
	public string name;
	public int openings;
	public bool promotionAllowed;

	public RankItem(SoldierRank _rank, string _name, int _openings, bool _promotionAllowed)
	{
		rank = _rank;
		name = _name;
		openings = _openings;
		promotionAllowed = _promotionAllowed;
	}
}

/// <summary>
/// Select Rank window that allows manual promotion/demotion of a soldier.
/// </summary>
public class SoldierRankState : State
{
	private readonly Base _base;
	private readonly int _soldierId; // C++ size_t
	private readonly TextButton _btnCancel;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtRank, _txtOpening;
	private readonly TextList _lstRanks;
	private readonly List<RankItem> _ranks = [];

	/// <summary>
	/// Initializes all the elements in the Soldier Rank window.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="soldier">ID of the selected soldier.</param>
	public SoldierRankState(Base @base, int soldier)
	{
		_base = @base;
		_soldierId = soldier;
		_screen = false;

		// Create objects
		_window = new Window(this, 192, 160, 64, 20, WindowPopup.POPUP_BOTH);
		_btnCancel = new TextButton(140, 16, 90, 156);
		_txtTitle = new Text(182, 17, 69, 28);
		_txtRank = new Text(90, 9, 80, 52);
		_txtOpening = new Text(70, 9, 190, 52);
		_lstRanks = new TextList(160, 80, 73, 68);

		// Set palette
		SetInterface("soldierRank");

		Add(_window, "window", "soldierRank");
		Add(_btnCancel, "button", "soldierRank");
		Add(_txtTitle, "text", "soldierRank");
		Add(_txtRank, "text", "soldierRank");
		Add(_txtOpening, "text", "soldierRank");
		Add(_lstRanks, "list", "soldierRank");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "soldierRank");

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		Soldier s = _base.Soldiers[_soldierId];
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_PROMOTE_SOLDIER").Arg(Tr(s.RankString)).Arg(s.GetName()));

		_txtRank.SetText(Tr("STR_RANK_HEADER"));

		_txtOpening.SetText(Tr("STR_OPENINGS_HEADER"));

		_lstRanks.SetColumns(2, 132, 21);
		_lstRanks.SetSelectable(true);
		_lstRanks.SetBackground(_window);
		_lstRanks.Margin = 8;

		var openings = new PromotionOpenings(_game.SavedGame.AllActiveSoldiers, _game.Mod);

		// a copy is unavoidable here because me may want to modify this set.
		var rankStringsCopy = new List<string>(s.Rules.RankStrings);

		// if the rank strings is empty and the soldier is allowed promotion, build a default set of rank strings.
		// ideally this would be encoded in the data someplace instead of relying on this behavior for empty rank strings.
		if (rankStringsCopy.Count == 0 && s.Rules.AllowPromotion)
		{
			rankStringsCopy = ["STR_ROOKIE", "STR_SQUADDIE", "STR_SERGEANT", "STR_CAPTAIN", "STR_COLONEL", "STR_COMMANDER"];
		}

		// build the promotion list.
		foreach (var rank in new[] { SoldierRank.RANK_ROOKIE, SoldierRank.RANK_SQUADDIE, SoldierRank.RANK_SERGEANT, SoldierRank.RANK_CAPTAIN, SoldierRank.RANK_COLONEL, SoldierRank.RANK_COMMANDER })
		{
			if ((int)rank < rankStringsCopy.Count)
			{
				_ranks.Add(new RankItem(rank, rankStringsCopy[(int)rank], openings[rank], openings.IsManualPromotionPossible(s, rank)));
			}
		}

		foreach (var rankItem in _ranks)
		{
			string quantityText = rankItem.openings >= 0 ? rankItem.openings.ToString(CultureInfo.InvariantCulture) : "-";

			_lstRanks.AddRow(2, Tr(rankItem.name), quantityText);

			if (!rankItem.promotionAllowed)
			{
				// disabled
				_lstRanks.SetCellColor(_lstRanks.LastRowIndex, 0, _lstRanks.SecondaryColor);
			}
		}

		_lstRanks.OnMouseClick(LstRankClick);
		_lstRanks.OnMouseClick(LstRankClickMiddle, SDLMouse.SDL_BUTTON_MIDDLE);
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Promotes/demotes the soldier if possible.
	/// </summary>
	public void LstRankClick(Action action)
	{
		RankItem selectedRank = _ranks[(int)_lstRanks.SelectedRow];
		if (selectedRank.promotionAllowed)
		{
			Soldier soldier = _base.Soldiers[_soldierId];
			soldier.Rank = selectedRank.rank;

			_game.PopState();
		}
	}

	/// <summary>
	/// Opens the corresponding Ufopaedia article.
	/// </summary>
	public void LstRankClickMiddle(Action action)
	{
		string articleId = _ranks[(int)_lstRanks.SelectedRow].name;
		Ufopaedia.OpenArticle(_game, articleId);
	}
}
