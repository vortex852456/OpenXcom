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
// Ported from: src/Ufopaedia/ArticleState.h, src/Ufopaedia/ArticleState.cpp
#nullable disable

namespace Acom;

/// <summary>Current state of ufopedia (shared between the article states, C++ std::shared_ptr).</summary>
public class ArticleCommonState
{
	/// <summary>Invalid index.</summary>
	public const int invalid = -1;

	/// <summary>Current selected article index (for previous/next navigation).</summary>
	public int current_index = invalid;

	/// <summary>Current sub page of article.</summary>
	public int current_page;

	/// <summary>List of all available articles.</summary>
	public List<ArticleDefinition> articleList = [];
	public List<bool> articleStatusList = [];

	/// <summary>Get current Article definition for current index position.</summary>
	public ArticleDefinition CurrentArticle => articleList[current_index];

	public bool IsCurrentArticleHidden => articleStatusList[current_index];

	// Note: the C++ indexes are size_t; the unsigned casts keep the wrap-around comparisons (e.g. with invalid).

	/// <summary>Change index position to next article.</summary>
	public void NextArticle()
	{
		if ((uint)current_index >= (uint)(articleList.Count - 1))
		{
			// goto first
			current_index = 0;
		}
		else
		{
			current_index++;
		}
		if (IsCurrentArticleHidden)
		{
			NextArticle();
		}
	}

	/// <summary>
	/// Change page to next in article or move to next index position.
	/// Each article can have multiple pages, if we are on last page we will move to first page of next article.
	/// </summary>
	public void NextArticlePage()
	{
		if (!HasNextArticlePage)
		{
			// goto to first page of next article
			NextArticle();
			current_page = 0;
		}
		else
		{
			current_page++;
		}
	}

	public bool HasNextArticlePage => !((uint)current_page >= (uint)(CurrentArticle.NumberOfPages - 1));

	/// <summary>Change index position to previous article.</summary>
	public void PrevArticle()
	{
		if (current_index == 0 || (uint)current_index > (uint)(articleList.Count - 1))
		{
			// goto last
			current_index = articleList.Count - 1;
		}
		else
		{
			current_index--;
		}
		if (IsCurrentArticleHidden)
		{
			PrevArticle();
		}
	}

	/// <summary>
	/// Change page to previous in article or move to previous index position.
	/// Each article can have multiple pages, if we are on first page we will move to last page of previous article.
	/// </summary>
	public void PrevArticlePage()
	{
		if (!HasPrevArticlePage)
		{
			// goto last page of previous article
			PrevArticle();
			current_page = CurrentArticle.NumberOfPages - 1;
		}
		else
		{
			current_page--;
		}
	}

	public bool HasPrevArticlePage => !(current_page == 0 || (uint)current_page > (uint)(CurrentArticle.NumberOfPages - 1));
}

/// <summary>
/// UfopaediaArticle is the base class for all articles of various types.
/// It encapsulates the basic characteristics.
/// </summary>
public class ArticleState : State
{
	/// <summary>The article id.</summary>
	protected string _id;

	/// <summary>Screen elements common to all articles!</summary>
	protected Surface _bg;
	protected TextButton _btnOk;
	protected TextButton _btnPrev;
	protected TextButton _btnNext;
	protected TextButton _btnInfo;

	/// <summary>Shared state.</summary>
	protected ArticleCommonState _state;

	/// <summary>Constructor (protected, so it can only be instantiated by derived classes).</summary>
	/// <param name="article_id">The article id of this article state instance.</param>
	/// <param name="state">Shared ufopedia state.</param>
	protected ArticleState(string article_id, ArticleCommonState state)
	{
		_id = article_id;

		// init background and navigation elements
		_bg = new Surface(320, 200, 0, 0);
		_btnOk = new TextButton(30, 14, 5, 5);
		_btnPrev = new TextButton(30, 14, 40, 5);
		_btnNext = new TextButton(30, 14, 75, 5);
		_btnInfo = new TextButton(40, 14, 110, 5);

		_state = state;

		// remember this article as seen/normal
		int ruleStatus = _game.SavedGame.GetUfopediaRuleStatus(_id);
		if (ruleStatus != ArticleDefinition.PEDIA_STATUS_HIDDEN)
		{
			_game.SavedGame.SetUfopediaRuleStatus(_id, ArticleDefinition.PEDIA_STATUS_NORMAL);
		}
	}

	/// <summary>Return the article id.</summary>
	public string Id => _id;

	/// <summary>Converts damage type to string.</summary>
	protected static string GetDamageTypeText(ItemDamageType dt)
	{
		string type;
		switch (dt)
		{
		case ItemDamageType.DT_NONE:
			type = "STR_DAMAGE_NONE";
			break;
		case ItemDamageType.DT_AP:
			type = "STR_DAMAGE_ARMOR_PIERCING";
			break;
		case ItemDamageType.DT_IN:
			type = "STR_DAMAGE_INCENDIARY";
			break;
		case ItemDamageType.DT_HE:
			type = "STR_DAMAGE_HIGH_EXPLOSIVE";
			break;
		case ItemDamageType.DT_LASER:
			type = "STR_DAMAGE_LASER_BEAM";
			break;
		case ItemDamageType.DT_PLASMA:
			type = "STR_DAMAGE_PLASMA_BEAM";
			break;
		case ItemDamageType.DT_STUN:
			type = "STR_DAMAGE_STUN";
			break;
		case ItemDamageType.DT_MELEE:
			type = "STR_DAMAGE_MELEE";
			break;
		case ItemDamageType.DT_ACID:
			type = "STR_DAMAGE_ACID";
			break;
		case ItemDamageType.DT_SMOKE:
			type = "STR_DAMAGE_SMOKE";
			break;
		case ItemDamageType.DT_10:
			type = "STR_DAMAGE_10";
			break;
		case ItemDamageType.DT_11:
			type = "STR_DAMAGE_11";
			break;
		case ItemDamageType.DT_12:
			type = "STR_DAMAGE_12";
			break;
		case ItemDamageType.DT_13:
			type = "STR_DAMAGE_13";
			break;
		case ItemDamageType.DT_14:
			type = "STR_DAMAGE_14";
			break;
		case ItemDamageType.DT_15:
			type = "STR_DAMAGE_15";
			break;
		case ItemDamageType.DT_16:
			type = "STR_DAMAGE_16";
			break;
		case ItemDamageType.DT_17:
			type = "STR_DAMAGE_17";
			break;
		case ItemDamageType.DT_18:
			type = "STR_DAMAGE_18";
			break;
		case ItemDamageType.DT_19:
			type = "STR_DAMAGE_19";
			break;
		default:
			type = "STR_UNKNOWN";
			break;
		}
		return type;
	}

	/// <summary>Set captions and click handlers for the common control elements.</summary>
	protected void InitLayout()
	{
		Add(_bg);
		Add(_btnOk);
		Add(_btnPrev);
		Add(_btnNext);
		Add(_btnInfo);

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnOk.OnKeyboardPress(BtnResetMusicClick, Options.keySelectMusicTrack);
		_btnPrev.Text = "<<";
		_btnPrev.OnMouseClick(BtnPrevClick);
		_btnPrev.OnKeyboardPress(BtnPrevClick, Options.keyGeoLeft);
		_btnNext.Text = ">>";
		_btnNext.OnMouseClick(BtnNextClick);
		_btnNext.OnKeyboardPress(BtnNextClick, Options.keyGeoRight);
		_btnInfo.Text = Tr("STR_INFO_UFOPEDIA");
		_btnInfo.OnMouseClick(BtnInfoClick);
		_btnInfo.OnKeyboardPress(BtnInfoClick, Options.keyGeoUfopedia);
		_btnInfo.Visible = false;
	}

#pragma warning disable CA1822 // instance handlers in C++ (State._game is static here)
	/// <summary>Returns to the previous screen.</summary>
	protected void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>Resets the music to a random geoscape music.</summary>
	protected void BtnResetMusicClick(Action action)
	{
		// reset that pesky interception music!
		_game.Mod.PlayMusic("GMGEO");
	}
#pragma warning restore CA1822

	/// <summary>Shows the previous available article.</summary>
	protected void BtnPrevClick(Action action)
	{
		Ufopaedia.Prev(_game, _state);
	}

	/// <summary>Shows the next available article. Loops to the first.</summary>
	protected void BtnNextClick(Action action)
	{
		Ufopaedia.Next(_game, _state);
	}

	/// <summary>Shows the detailed (raw) information about the current topic.</summary>
	protected void BtnInfoClick(Action action)
	{
		Ufopaedia.OpenArticleDetail(_game, _id);
	}
}
