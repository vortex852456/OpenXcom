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
// Ported from: src/Ufopaedia/Ufopaedia.h, src/Ufopaedia/Ufopaedia.cpp
#nullable disable

namespace Acom;

/// <summary>
/// This static class encapsulates all functions related to Ufopaedia for the game.
/// Main purpose is to open Ufopaedia from Geoscape, navigate between articles
/// and release new articles after successful research.
/// </summary>
public static class Ufopaedia
{
	// This section is meant for articles, that have to be activated,
	// but have no own entry in a list. E.g. Ammunition items.
	// Maybe others as well, that should just not be selectable.
	public const string UFOPAEDIA_NOT_AVAILABLE = "STR_NOT_AVAILABLE";
	public const string UFOPAEDIA_COMMENDATIONS = "STR_COMMENDATIONS_UC";

	/// <summary>Checks, if an article has already been released.</summary>
	/// <param name="save">Pointer to saved game.</param>
	/// <param name="article">Article definition to release.</param>
	/// <returns>true, if the article is available.</returns>
	public static bool IsArticleAvailable(SavedGame save, ArticleDefinition article)
	{
		if (article.disabledBy.Count != 0)
		{
			// Note: yes, check for non-empty vector is necessary
			if (save.IsResearched(article.disabledBy, false))
			{
				return false; // article is disabled, i.e. not available
			}
		}
		return save.IsResearched(article._requires);
	}

	/// <summary>
	/// Gets the index of the selected article_id in the visible list.
	/// If the id is not found, returns -1.
	/// </summary>
	/// <param name="articles">Visible article list.</param>
	/// <param name="article_id">Article id to find.</param>
	/// <returns>Index of the given article id in the internal list, -1 if not found.</returns>
	public static int GetArticleIndex(List<ArticleDefinition> articles, string article_id)
	{
		string UC_ID = article_id + "_UC";
		for (int it = 0; it < articles.Count; ++it)
		{
			if (articles[it].id == article_id)
			{
				return it;
			}
		}
		for (int it = 0; it < articles.Count; ++it)
		{
			if (articles[it].id == UC_ID)
			{
				return it;
			}
		}
		for (int it = 0; it < articles.Count; ++it)
		{
			foreach (var req in articles[it]._requires)
			{
				if (article_id == req)
				{
					return it;
				}
			}
		}
		return ArticleCommonState.invalid;
	}

	/// <summary>Creates a new article state dependent on the given article definition.</summary>
	/// <param name="state">Shared state; its current article is opened.</param>
	/// <returns>Article state object.</returns>
	public static ArticleState CreateArticleState(ArticleCommonState state)
	{
		ArticleDefinition article = state.CurrentArticle;
		switch (article.Type)
		{
			case UfopaediaTypeId.UFOPAEDIA_TYPE_UNIT:
				return new ArticleStateUnit(article as ArticleDefinitionUnit, state);
			case UfopaediaTypeId.UFOPAEDIA_TYPE_SOLDIER:
				return new ArticleStateSoldier(article as ArticleDefinitionSoldier, state);
			case UfopaediaTypeId.UFOPAEDIA_TYPE_CRAFT:
				return new ArticleStateCraft(article as ArticleDefinitionCraft, state);
			case UfopaediaTypeId.UFOPAEDIA_TYPE_CRAFT_WEAPON:
				return new ArticleStateCraftWeapon(article as ArticleDefinitionCraftWeapon, state);
			case UfopaediaTypeId.UFOPAEDIA_TYPE_VEHICLE:
				return new ArticleStateVehicle(article as ArticleDefinitionVehicle, state);
			case UfopaediaTypeId.UFOPAEDIA_TYPE_ITEM:
				return new ArticleStateItem(article as ArticleDefinitionItem, state);
			case UfopaediaTypeId.UFOPAEDIA_TYPE_ARMOR:
				return new ArticleStateArmor(article as ArticleDefinitionArmor, state);
			case UfopaediaTypeId.UFOPAEDIA_TYPE_BASE_FACILITY:
				return new ArticleStateBaseFacility(article as ArticleDefinitionBaseFacility, state);
			case UfopaediaTypeId.UFOPAEDIA_TYPE_TEXT:
				return new ArticleStateText(article as ArticleDefinitionText, state);
			case UfopaediaTypeId.UFOPAEDIA_TYPE_TEXTIMAGE:
				return new ArticleStateTextImage(article as ArticleDefinitionTextImage, state);
			case UfopaediaTypeId.UFOPAEDIA_TYPE_UFO:
				return new ArticleStateUfo(article as ArticleDefinitionUfo, state);
			case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD:
				return new ArticleStateTFTD(article as ArticleDefinitionTFTD, state);
			case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_CRAFT:
				return new ArticleStateTFTDCraft(article as ArticleDefinitionTFTD, state);
			case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_CRAFT_WEAPON:
				return new ArticleStateTFTDCraftWeapon(article as ArticleDefinitionTFTD, state);
			case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_VEHICLE:
				return new ArticleStateTFTDVehicle(article as ArticleDefinitionTFTD, state);
			case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_ITEM:
				return new ArticleStateTFTDItem(article as ArticleDefinitionTFTD, state);
			case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_ARMOR:
				return new ArticleStateTFTDArmor(article as ArticleDefinitionTFTD, state);
			case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_BASE_FACILITY:
				return new ArticleStateTFTDFacility(article as ArticleDefinitionTFTD, state);
			case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_USO:
				return new ArticleStateTFTDUso(article as ArticleDefinitionTFTD, state);
			default:
				throw new InvalidOperationException("Unknown type for article '" + article.id + "'");
		}
	}

	/// <summary>Set UPSaved index and open the new state.</summary>
	/// <param name="game">Pointer to actual game.</param>
	/// <param name="article">Article definition of the article to open.</param>
	public static void OpenArticle(Game game, ArticleDefinition article)
	{
		var state = CreateCommonArticleState(game.SavedGame, game.Mod);
		state.current_index = GetArticleIndex(state.articleList, article.id);
		if (state.current_index != ArticleCommonState.invalid)
		{
			game.PushState(CreateArticleState(state));
		}
	}

	/// <summary>Checks if selected article_id is available -> if yes, open it.</summary>
	/// <param name="game">Pointer to actual game.</param>
	/// <param name="article_id">Article id to find.</param>
	public static void OpenArticle(Game game, string article_id)
	{
		var state = CreateCommonArticleState(game.SavedGame, game.Mod);
		state.current_index = GetArticleIndex(state.articleList, article_id);
		if (state.current_index != ArticleCommonState.invalid)
		{
			game.PushState(CreateArticleState(state));
		}
	}

	/// <summary>Checks if selected article_id is available -> if yes, open its (raw) details.</summary>
	/// <param name="game">Pointer to actual game.</param>
	/// <param name="article_id">Article id to find.</param>
	public static void OpenArticleDetail(Game game, string article_id)
	{
		var state = CreateCommonArticleState(game.SavedGame, game.Mod);
		state.current_index = GetArticleIndex(state.articleList, article_id);
		if (state.current_index != ArticleCommonState.invalid)
		{
			game.PushState(new StatsForNerdsState(state, false, false, false));
		}
	}

	/// <summary>Open Ufopaedia start state, presenting the section selection buttons.</summary>
	/// <param name="game">Pointer to actual game.</param>
	public static void Open(Game game)
	{
		game.PushState(new UfopaediaStartState());
	}

	/// <summary>Open the next article in the list. Loops to the first.</summary>
	/// <param name="game">Pointer to actual game.</param>
	/// <param name="state">Shared state of the current article.</param>
	public static void Next(Game game, ArticleCommonState state)
	{
		if (state.IsCurrentArticleHidden) return;
		state.NextArticlePage();
		game.PopState();
		game.PushState(CreateArticleState(state));
	}

	/// <summary>Open the next article detail (Stats for Nerds) in the list. Loops to the first.</summary>
	/// <param name="game">Pointer to actual game.</param>
	/// <param name="state">Shared state of the current article.</param>
	public static void NextDetail(Game game, ArticleCommonState state, bool debug, bool ids, bool defaults)
	{
		if (state.IsCurrentArticleHidden) return;
		state.NextArticle();
		game.PopState();
		game.PushState(new StatsForNerdsState(state, debug, ids, defaults));
	}

	/// <summary>Open the previous article in the list. Loops to the last.</summary>
	/// <param name="game">Pointer to actual game.</param>
	/// <param name="state">Shared state of the current article.</param>
	public static void Prev(Game game, ArticleCommonState state)
	{
		if (state.IsCurrentArticleHidden) return;
		state.PrevArticlePage();
		game.PopState();
		game.PushState(CreateArticleState(state));
	}

	/// <summary>Open the previous article detail (Stats for Nerds) in the list. Loops to the last.</summary>
	/// <param name="game">Pointer to actual game.</param>
	/// <param name="state">Shared state of the current article.</param>
	public static void PrevDetail(Game game, ArticleCommonState state, bool debug, bool ids, bool defaults)
	{
		if (state.IsCurrentArticleHidden) return;
		state.PrevArticle();
		game.PopState();
		game.PushState(new StatsForNerdsState(state, debug, ids, defaults));
	}

	/// <summary>Fill an ArticleList with the currently visible ArticleIds of the given section.</summary>
	/// <param name="save">Pointer to saved game.</param>
	/// <param name="rule">Pointer to mod.</param>
	/// <param name="section">Article section to find, e.g. "XCOM Crafts &amp; Armaments", "Alien Lifeforms", etc.</param>
	/// <param name="data">Article definition list object to fill data in.</param>
	public static void List(SavedGame save, Mod rule, string section, List<ArticleDefinition> data)
	{
		var state = CreateCommonArticleState(save, rule);
		foreach (var a in state.articleList)
		{
			if (a.section == section)
			{
				data.Add(a);
			}
		}
	}

	/// <summary>Check if this is an invisible commendation article.</summary>
	/// <param name="save">Pointer to saved game.</param>
	/// <param name="article">Article to check.</param>
	public static bool IsCommendationArticleInvisible(SavedGame save, ArticleDefinition article)
	{
		// show hidden Commendations entries if:
		if (article.hiddenCommendation)
		{
			// 1. debug mode is on
			if (save.DebugMode)
			{
				return false;
			}

			// 2. or if the article was opened already
			if (save.GetUfopediaRuleStatus(article.id) != ArticleDefinition.PEDIA_STATUS_NEW)
			{
				return false;
			}

			// 3. or if the medal was awarded at least once
			if (IsAwardedCommendation(save, article))
			{
				return false;
			}

			return true;
		}

		return false;
	}

	/// <summary>Check if the article corresponds to an awarded commendation.</summary>
	/// <param name="save">Pointer to saved game.</param>
	/// <param name="article">Article to check.</param>
	public static bool IsAwardedCommendation(SavedGame save, ArticleDefinition article)
	{
		if (article.section == UFOPAEDIA_COMMENDATIONS)
		{
			// 1. check living soldiers
			foreach (var xbase in save.Bases)
			{
				foreach (var soldier in xbase.Soldiers)
				{
					foreach (var comm in soldier.Diary.SoldierCommendations)
					{
						if (comm.Type == article.MainTitle)
						{
							return true;
						}
					}
				}
			}

			// 2. check dead soldiers
			for (int deadManIt = save.DeadSoldiers.Count - 1; deadManIt >= 0; --deadManIt)
			{
				foreach (var comm in save.DeadSoldiers[deadManIt].Diary.SoldierCommendations)
				{
					if (comm.Type == article.MainTitle)
					{
						return true;
					}
				}
			}
		}

		return false;
	}

	/// <summary>Return an ArticleList with all the currently visible ArticleIds.</summary>
	/// <param name="save">Pointer to saved game.</param>
	/// <param name="rule">Pointer to mod.</param>
	/// <returns>List of visible ArticleDefinitions.</returns>
	public static ArticleCommonState CreateCommonArticleState(SavedGame save, Mod rule)
	{
		var shared = new ArticleCommonState();
		foreach (var articleName in rule.UfopaediaList)
		{
			ArticleDefinition article = rule.GetUfopaediaArticle(articleName);
			if (IsArticleAvailable(save, article) && article.section != UFOPAEDIA_NOT_AVAILABLE && !IsCommendationArticleInvisible(save, article))
			{
				shared.articleList.Add(article);
				shared.articleStatusList.Add(save.GetUfopediaRuleStatus(articleName) == ArticleDefinition.PEDIA_STATUS_HIDDEN);
			}
		}
		return shared;
	}
}
