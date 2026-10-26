#pragma warning disable CA1716 // namespace mirrors the C++ folder Ufopaedia
using System.Reflection;
using GameMod = Acom.Mod;
using Pedia = Acom.Ufopaedia;

namespace Acom.Tests.Ufopaedia;

/// <summary>
/// Article availability, visible list order and prev/next navigation (Ufopaedia.cpp, ArticleState.cpp),
/// with a small in-memory ruleset and a fake research state.
/// </summary>
public class UfopaediaTests
{
	private const string Rules = """
		ufopaedia:
		  - id: STR_FIRST
		    type_id: 10
		    section: STR_XCOM_CRAFT_ARMAMENT
		  - id: STR_LOCKED
		    type_id: 10
		    section: STR_XCOM_CRAFT_ARMAMENT
		    requires: [STR_RES_B]
		  - id: STR_PISTOL
		    type_id: 10
		    section: STR_WEAPONS_AND_EQUIPMENT
		    requires: [STR_RES_A]
		  - id: STR_PISTOL_CLIP
		    type_id: 10
		    section: STR_NOT_AVAILABLE
		  - id: STR_OBSOLETE
		    type_id: 10
		    section: STR_WEAPONS_AND_EQUIPMENT
		    disabledBy: [STR_RES_A]
		  - id: STR_ALIEN_UC
		    type_id: 10
		    section: STR_ALIEN_LIFE_FORMS
		    requires: [STR_RES_A, STR_ALIEN_AUTOPSY]
		  - id: STR_MEDAL
		    type_id: 10
		    section: STR_COMMENDATIONS_UC
		    hiddenCommendation: true
		  - id: STR_LAST
		    type_id: 10
		    section: STR_WEAPONS_AND_EQUIPMENT
		    pages:
		      - title: STR_LAST
		        text: STR_LAST_PAGE1
		      - title: STR_LAST
		        text: STR_LAST_PAGE2
		""";

	private static GameMod CreateMod()
	{
		var mod = new GameMod(createConverter: false);
		mod._modData.Add(new ModData { name = "xcom1", offset = 0, size = 1000 });
		mod._modCurrent = mod._modData[0];
		mod.LoadFile(new YamlRootNodeReader(Rules, "test.rul"));
		return mod;
	}

	/// <summary>Fake research state: marks the given topics as discovered.</summary>
	private static SavedGame CreateSave(params string[] discovered)
	{
		var save = new SavedGame();
		var list = (List<RuleResearch>)typeof(SavedGame).GetField("_discovered", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(save)!;
		int order = 0;
		foreach (var name in discovered)
			list.Add(new RuleResearch(name, ++order));
		return save;
	}

	private static void SetDebugMode(SavedGame save, bool debug) =>
		typeof(SavedGame).GetField("_debug", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(save, debug);

	private static List<string> Ids(IEnumerable<ArticleDefinition> articles) => articles.Select(a => a.id).ToList();

	[Fact]
	public void Article_availability_follows_requires_and_disabledBy()
	{
		var mod = CreateMod();
		var none = CreateSave();
		var resA = CreateSave("STR_RES_A");

		Assert.True(Pedia.IsArticleAvailable(none, mod.GetUfopaediaArticle("STR_FIRST")));
		Assert.False(Pedia.IsArticleAvailable(none, mod.GetUfopaediaArticle("STR_PISTOL")));
		Assert.True(Pedia.IsArticleAvailable(resA, mod.GetUfopaediaArticle("STR_PISTOL")));

		// all requirements must be researched
		Assert.False(Pedia.IsArticleAvailable(resA, mod.GetUfopaediaArticle("STR_ALIEN_UC")));
		Assert.True(Pedia.IsArticleAvailable(CreateSave("STR_RES_A", "STR_ALIEN_AUTOPSY"), mod.GetUfopaediaArticle("STR_ALIEN_UC")));

		// disabledBy wins once researched; debug mode is ignored for it
		Assert.True(Pedia.IsArticleAvailable(none, mod.GetUfopaediaArticle("STR_OBSOLETE")));
		Assert.False(Pedia.IsArticleAvailable(resA, mod.GetUfopaediaArticle("STR_OBSOLETE")));
		var debug = CreateSave();
		SetDebugMode(debug, true);
		Assert.True(Pedia.IsArticleAvailable(debug, mod.GetUfopaediaArticle("STR_OBSOLETE")));
		Assert.True(Pedia.IsArticleAvailable(debug, mod.GetUfopaediaArticle("STR_LOCKED")));
	}

	[Fact]
	public void Visible_list_keeps_the_mod_order_and_drops_unavailable_entries()
	{
		var mod = CreateMod();
		var state = Pedia.CreateCommonArticleState(CreateSave("STR_RES_A"), mod);

		// STR_LOCKED: not researched, STR_PISTOL_CLIP: not available section, STR_OBSOLETE: disabled,
		// STR_ALIEN_UC: autopsy missing, STR_MEDAL: hidden commendation never awarded
		Assert.Equal(["STR_FIRST", "STR_PISTOL", "STR_LAST"], Ids(state.articleList));
		Assert.Equal([false, false, false], state.articleStatusList);
		Assert.Equal(ArticleCommonState.invalid, state.current_index);
		Assert.Equal(0, state.current_page);
	}

	[Fact]
	public void Hidden_commendations_show_up_in_debug_mode_or_once_opened()
	{
		var mod = CreateMod();
		var medal = mod.GetUfopaediaArticle("STR_MEDAL");
		var save = CreateSave();

		Assert.True(Pedia.IsCommendationArticleInvisible(save, medal));
		Assert.False(Pedia.IsAwardedCommendation(save, medal));

		save.SetUfopediaRuleStatus("STR_MEDAL", ArticleDefinition.PEDIA_STATUS_NORMAL);
		Assert.False(Pedia.IsCommendationArticleInvisible(save, medal));
		Assert.Contains("STR_MEDAL", Ids(Pedia.CreateCommonArticleState(save, mod).articleList));

		var debug = CreateSave();
		SetDebugMode(debug, true);
		Assert.False(Pedia.IsCommendationArticleInvisible(debug, medal));

		// not a hidden commendation
		Assert.False(Pedia.IsCommendationArticleInvisible(save, mod.GetUfopaediaArticle("STR_FIRST")));
	}

	[Fact]
	public void Hidden_articles_are_listed_but_flagged()
	{
		var mod = CreateMod();
		var save = CreateSave("STR_RES_A");
		save.SetUfopediaRuleStatus("STR_PISTOL", ArticleDefinition.PEDIA_STATUS_HIDDEN);

		var state = Pedia.CreateCommonArticleState(save, mod);
		Assert.Equal(["STR_FIRST", "STR_PISTOL", "STR_LAST"], Ids(state.articleList));
		Assert.Equal([false, true, false], state.articleStatusList);
	}

	[Fact]
	public void Section_list_filters_the_visible_articles()
	{
		var mod = CreateMod();
		var data = new List<ArticleDefinition>();
		Pedia.List(CreateSave("STR_RES_A", "STR_ALIEN_AUTOPSY"), mod, "STR_WEAPONS_AND_EQUIPMENT", data);
		Assert.Equal(["STR_PISTOL", "STR_LAST"], Ids(data));

		data.Clear();
		Pedia.List(CreateSave("STR_RES_A", "STR_ALIEN_AUTOPSY"), mod, "STR_ALIEN_LIFE_FORMS", data);
		Assert.Equal(["STR_ALIEN_UC"], Ids(data));
	}

	[Fact]
	public void Article_index_matches_id_then_uc_id_then_requirement()
	{
		var mod = CreateMod();
		var articles = Pedia.CreateCommonArticleState(CreateSave("STR_RES_A", "STR_ALIEN_AUTOPSY"), mod).articleList;
		Assert.Equal(["STR_FIRST", "STR_PISTOL", "STR_ALIEN_UC", "STR_LAST"], Ids(articles));

		Assert.Equal(1, Pedia.GetArticleIndex(articles, "STR_PISTOL"));
		Assert.Equal(2, Pedia.GetArticleIndex(articles, "STR_ALIEN")); // + "_UC"
		Assert.Equal(1, Pedia.GetArticleIndex(articles, "STR_RES_A")); // first article requiring it
		Assert.Equal(2, Pedia.GetArticleIndex(articles, "STR_ALIEN_AUTOPSY"));
		Assert.Equal(ArticleCommonState.invalid, Pedia.GetArticleIndex(articles, "STR_UNKNOWN"));
	}

	private static ArticleCommonState Navigation(params bool[] hidden)
	{
		var state = new ArticleCommonState();
		for (int i = 0; i < hidden.Length; ++i)
		{
			var article = new ArticleDefinitionText();
			article.Load(new YamlRootNodeReader(i == 1
				? "id: A1\ntype_id: 10\npages:\n  - title: A1\n  - title: A1\n  - title: A1\n"
				: $"id: A{i}\ntype_id: 10\n", "test").ToBase(), (i + 1) * 100);
			state.articleList.Add(article);
			state.articleStatusList.Add(hidden[i]);
		}
		return state;
	}

	[Fact]
	public void Next_and_previous_wrap_around_and_skip_hidden_articles()
	{
		var state = Navigation(false, false, true, false);
		state.current_index = 3;
		state.NextArticle();
		Assert.Equal(0, state.current_index); // wraps
		state.NextArticle();
		Assert.Equal(1, state.current_index);
		state.NextArticle();
		Assert.Equal(3, state.current_index); // skips the hidden one

		state.PrevArticle();
		Assert.Equal(1, state.current_index);
		state.current_index = 0;
		state.PrevArticle();
		Assert.Equal(3, state.current_index); // wraps to the last

		// an invalid index (size_t -1) goes to the last article
		state.current_index = ArticleCommonState.invalid;
		state.PrevArticle();
		Assert.Equal(3, state.current_index);
		state.current_index = ArticleCommonState.invalid;
		state.NextArticle();
		Assert.Equal(0, state.current_index);
	}

	[Fact]
	public void Pages_are_walked_before_moving_to_the_next_article()
	{
		var state = Navigation(false, false, false);
		state.current_index = 0;
		Assert.False(state.HasNextArticlePage);
		Assert.False(state.HasPrevArticlePage);

		state.NextArticlePage();
		Assert.Equal((1, 0), (state.current_index, state.current_page));
		Assert.True(state.HasNextArticlePage);
		state.NextArticlePage();
		state.NextArticlePage();
		Assert.Equal((1, 2), (state.current_index, state.current_page));
		Assert.False(state.HasNextArticlePage);
		state.NextArticlePage();
		Assert.Equal((2, 0), (state.current_index, state.current_page));

		// back: the previous article starts at its last page
		state.PrevArticlePage();
		Assert.Equal((1, 2), (state.current_index, state.current_page));
		state.PrevArticlePage();
		Assert.Equal((1, 1), (state.current_index, state.current_page));
		Assert.True(state.HasPrevArticlePage);
	}
}
