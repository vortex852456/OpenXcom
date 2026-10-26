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
// Ported from: src/Ufopaedia/StatsForNerdsState.h, src/Ufopaedia/StatsForNerdsState.cpp
#nullable disable

using System.Globalization;
using System.Text;

namespace Acom;

/// <summary>
/// A screen, where you can see the (almost) raw ruleset corresponding to the given Ufopedia article.
/// </summary>
/// <remarks>
/// The C++ std::ostringstream that is passed around becomes a StringBuilder; the value formatting helpers
/// (<see cref="FormatFloat"/>, <see cref="FormatDouble"/>) reproduce the default ostream formatting.
/// </remarks>
#pragma warning disable CA1822 // instance members in C++ (State._game is static here)
#pragma warning disable CA1859 // generic collection parameters mirror the C++ const std::vector& parameters
public partial class StatsForNerdsState : State
{
	private Window _window;
	private Text _txtTitle;
	private ComboBox _cbxRelatedStuff;
	private Text _txtArticle;
	private TextButton _btnPrev, _btnNext;
	private TextList _lstRawData;
	private ToggleTextButton _btnIncludeDebug, _btnIncludeIds, _btnIncludeDefaults;
	private TextButton _btnOk;
	private TextButton _btnPreview;
	private byte _purple, _pink, _blue, _white, _gold;
	private UfopaediaTypeId _typeId;
	private string _topicId;
	private bool _mainArticle;
	private ArticleCommonState _state;
	private readonly List<string> _filterOptions = [];
	private bool _showDebug, _showIds, _showDefaults;
	private int _counter;
	private bool _indent;

	/// <summary>Name of class used in script.</summary>
	public const string ScriptName = "StatsForNerds";

	public static readonly SortedDictionary<string, string> translationMap = new(StringComparer.Ordinal)
	{
		{ "flatOne", "" }, // no translation
		{ "flatHundred", "" }, // no translation
		{ "strength", "STR_STRENGTH" },
		{ "psi", "STR_PSI_SKILL_AND_PSI_STRENGTH" }, // new, STR_PSIONIC_SKILL * STR_PSIONIC_STRENGTH
		{ "psiSkill", "STR_PSIONIC_SKILL" },
		{ "psiStrength", "STR_PSIONIC_STRENGTH" },
		{ "throwing", "STR_THROWING_ACCURACY" },
		{ "bravery", "STR_BRAVERY" },
		{ "firing", "STR_FIRING_ACCURACY" },
		{ "health", "STR_HEALTH" },
		{ "mana", "STR_MANA_POOL" },
		{ "tu", "STR_TIME_UNITS" },
		{ "reactions", "STR_REACTIONS" },
		{ "stamina", "STR_STAMINA" },
		{ "melee", "STR_MELEE_ACCURACY" },
		{ "strengthMelee", "STR_STRENGTH_AND_MELEE_ACCURACY" }, // new, STR_STRENGTH * STR_MELEE_ACCURACY
		{ "strengthThrowing", "STR_STRENGTH_AND_THROWING_ACCURACY" }, // new, STR_STRENGTH * STR_THROWING_ACCURACY
		{ "firingReactions", "STR_FIRING_ACCURACY_AND_REACTIONS" }, // new, STR_FIRING_ACCURACY * STR_REACTIONS

		{ "rank", "STR_RANK" },
		{ "fatalWounds", "STR_FATAL_WOUNDS" },

		{ "healthCurrent", "STR_HEALTH_CURRENT" }, // new, current HP (i.e. not max HP)
		{ "manaCurrent", "STR_MANA_CURRENT" },
		{ "tuCurrent", "STR_TIME_UNITS_CURRENT" }, // new
		{ "energyCurrent", "STR_ENERGY" },
		{ "moraleCurrent", "STR_MORALE" },
		{ "stunCurrent", "STR_STUN_LEVEL_CURRENT" }, // new

		{ "healthNormalized", "STR_HEALTH_NORMALIZED" }, // new, current HP normalized to [0, 1] interval
		{ "manaNormalized", "STR_MANA_NORMALIZED" },
		{ "tuNormalized", "STR_TIME_UNITS_NORMALIZED" }, // new
		{ "energyNormalized", "STR_ENERGY_NORMALIZED" }, // new
		{ "moraleNormalized", "STR_MORALE_NORMALIZED" }, // new
		{ "stunNormalized", "STR_STUN_LEVEL_NORMALIZED" }, // new

		{ "energyRegen", "STR_ENERGY_REGENERATION" }, // new, special stat returning vanilla energy regen

		{ "tuScaled", "STR_TIME_UNITS_SCALED" },
		{ "staminaScaled", "STR_STAMINA_SCALED" },
		{ "healthScaled", "STR_HEALTH_SCALED" },
		{ "braveryScaled", "STR_BRAVERY_SCALED" },
		{ "reactionsScaled", "STR_REACTIONS_SCALED" },
		{ "firingScaled", "STR_FIRING_ACCURACY_SCALED" },
		{ "throwingScaled", "STR_THROWING_ACCURACY_SCALED" },
		{ "strengthScaled", "STR_STRENGTH_SCALED" },
		{ "psiStrengthScaled", "STR_PSIONIC_STRENGTH_SCALED" },
		{ "psiSkillScaled", "STR_PSIONIC_SKILL_SCALED" },
		{ "meleeScaled", "STR_MELEE_ACCURACY_SCALED" },
		{ "manaScaled", "STR_MANA_POOL_SCALED" },
		{ "psiScaled", "STR_PSI_SKILL_AND_PSI_STRENGTH_SCALED" },
		{ "strengthMeleeScaled", "STR_STRENGTH_AND_MELEE_ACCURACY_SCALED" },
		{ "strengthThrowingScaled", "STR_STRENGTH_AND_THROWING_ACCURACY_SCALED" },
		{ "firingReactionsScaled", "STR_FIRING_ACCURACY_AND_REACTIONS_SCALED" },
	};

	public static readonly SortedDictionary<string, string> shortTranslationMap = new(StringComparer.Ordinal)
	{
		{ "flatOne", "" }, // no translation
		{ "flatHundred", "" }, // no translation
		{ "strength", "STR_STRENGTH_ABBREVIATION" },
		{ "psi", "STR_PSI_SKILL_AND_PSI_STRENGTH_ABBREVIATION" }, // new, STR_PSIONIC_SKILL * STR_PSIONIC_STRENGTH
		{ "psiSkill", "STR_PSIONIC_SKILL_ABBREVIATION" },
		{ "psiStrength", "STR_PSIONIC_STRENGTH_ABBREVIATION" },
		{ "throwing", "STR_THROWING_ACCURACY_ABBREVIATION" },
		{ "bravery", "STR_BRAVERY_ABBREVIATION" },
		{ "firing", "STR_FIRING_ACCURACY_ABBREVIATION" },
		{ "health", "STR_HEALTH_ABBREVIATION" },
		{ "mana", "STR_MANA_ABBREVIATION" },
		{ "tu", "STR_TIME_UNITS_ABBREVIATION" },
		{ "reactions", "STR_REACTIONS_ABBREVIATION" },
		{ "stamina", "STR_STAMINA_ABBREVIATION" },
		{ "melee", "STR_MELEE_ACCURACY_ABBREVIATION" },
		{ "strengthMelee", "STR_STRENGTH_AND_MELEE_ACCURACY_ABBREVIATION" }, // new, STR_STRENGTH * STR_MELEE_ACCURACY
		{ "strengthThrowing", "STR_STRENGTH_AND_THROWING_ACCURACY_ABBREVIATION" }, // new, STR_STRENGTH * STR_THROWING_ACCURACY
		{ "firingReactions", "STR_FIRING_ACCURACY_AND_REACTIONS_ABBREVIATION" }, // new, STR_FIRING_ACCURACY * STR_REACTIONS

		{ "rank", "STR_RANK" },
		{ "fatalWounds", "STR_FATAL_WOUNDS" },

		{ "healthCurrent", "STR_HEALTH_CURRENT_ABBREVIATION" }, // new, current HP (i.e. not max HP)
		{ "manaCurrent", "STR_MANA_CURRENT_ABBREVIATION" },
		{ "tuCurrent", "STR_TIME_UNITS_CURRENT_ABBREVIATION" }, // new
		{ "energyCurrent", "STR_ENERGY" },
		{ "moraleCurrent", "STR_MORALE" },
		{ "stunCurrent", "STR_STUN_LEVEL_CURRENT_ABBREVIATION" }, // new

		{ "healthNormalized", "STR_HEALTH_NORMALIZED_ABBREVIATION" }, // new, current HP normalized to [0, 1] interval
		{ "manaNormalized", "STR_MANA_NORMALIZED_ABBREVIATION" },
		{ "tuNormalized", "STR_TIME_UNITS_NORMALIZED_ABBREVIATION" }, // new
		{ "energyNormalized", "STR_ENERGY_NORMALIZED_ABBREVIATION" }, // new
		{ "moraleNormalized", "STR_MORALE_NORMALIZED_ABBREVIATION" }, // new
		{ "stunNormalized", "STR_STUN_LEVEL_NORMALIZED_ABBREVIATION" }, // new

		{ "energyRegen", "STR_ENERGY_REGENERATION_ABBREVIATION" }, // new, special stat returning vanilla energy regen

		{ "tuScaled", "STR_TIME_UNITS_SCALED_ABBREVIATION" },
		{ "staminaScaled", "STR_STAMINA_SCALED_ABBREVIATION" },
		{ "healthScaled", "STR_HEALTH_SCALED_ABBREVIATION" },
		{ "braveryScaled", "STR_BRAVERY_SCALED_ABBREVIATION" },
		{ "reactionsScaled", "STR_REACTIONS_SCALED_ABBREVIATION" },
		{ "firingScaled", "STR_FIRING_ACCURACY_SCALED_ABBREVIATION" },
		{ "throwingScaled", "STR_THROWING_ACCURACY_SCALED_ABBREVIATION" },
		{ "strengthScaled", "STR_STRENGTH_SCALED_ABBREVIATION" },
		{ "psiStrengthScaled", "STR_PSIONIC_STRENGTH_SCALED_ABBREVIATION" },
		{ "psiSkillScaled", "STR_PSIONIC_SKILL_SCALED_ABBREVIATION" },
		{ "meleeScaled", "STR_MELEE_ACCURACY_SCALED_ABBREVIATION" },
		{ "manaScaled", "STR_MANA_POOL_SCALED_ABBREVIATION" },
		{ "psiScaled", "STR_PSI_SKILL_AND_PSI_STRENGTH_SCALED_ABBREVIATION" },
		{ "strengthMeleeScaled", "STR_STRENGTH_AND_MELEE_ACCURACY_SCALED_ABBREVIATION" },
		{ "strengthThrowingScaled", "STR_STRENGTH_AND_THROWING_ACCURACY_SCALED_ABBREVIATION" },
		{ "firingReactionsScaled", "STR_FIRING_ACCURACY_AND_REACTIONS_SCALED_ABBREVIATION" },
	};

	// ---- value formatting helpers (std::ostream << value) ----

	/// <summary>Formats an integer like <c>std::ostream &lt;&lt; int</c>.</summary>
	internal static string FormatInt(long value) => value.ToString(CultureInfo.InvariantCulture);

	/// <summary>Formats a float like <c>std::ostream &lt;&lt; float</c> (default precision 6, "%g").</summary>
	internal static string FormatFloat(float value) => LocalizedText.FormatLikeOstream(value);

	/// <summary>Formats a double like <c>std::ostream &lt;&lt; double</c> (default precision 6, "%g").</summary>
	internal static string FormatDouble(double value) => LocalizedText.FormatLikeOstream(value);

	/// <summary>C++ fmath.h AreSame(float, float).</summary>
	internal static bool AreSame(float l, float r) => MathF.Abs(l - r) <= 1.1920929e-07f /* FLT_EPSILON */ * MathF.Max(1.0f, MathF.Max(MathF.Abs(l), MathF.Abs(r)));

	/// <summary>C++ fmath.h Round(double).</summary>
	private static double Round(double x) => x < 0.0 ? Math.Ceiling(x - 0.5) : Math.Floor(x + 0.5);

	/// <summary>C++ fmath.h AreSame(double, double).</summary>
	internal static bool AreSame(double l, double r) => Math.Abs(l - r) <= 2.2204460492503131e-16 /* DBL_EPSILON */ * Math.Max(1.0, Math.Max(Math.Abs(l), Math.Abs(r)));

	/// <summary>Initializes all the elements on the UI.</summary>
	public StatsForNerdsState(ArticleCommonState state, bool debug, bool ids, bool defaults)
	{
		_state = state;
		_counter = 0;
		_indent = false;
		ArticleDefinition article = _state.CurrentArticle;
		_typeId = article.Type;
		_topicId = article.id;
		_mainArticle = true;
		BuildUI(debug, ids, defaults);
	}

	/// <summary>Initializes all the elements on the UI.</summary>
	public StatsForNerdsState(UfopaediaTypeId typeId, string topicId, bool debug, bool ids, bool defaults)
	{
		_counter = 0;
		_indent = false;
		_typeId = typeId;
		_topicId = topicId;
		_mainArticle = false;
		BuildUI(debug, ids, defaults);
	}

	/// <summary>Builds the UI.</summary>
	private void BuildUI(bool debug, bool ids, bool defaults)
	{
		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_txtTitle = new Text(304, 17, 8, 7);
		_cbxRelatedStuff = new ComboBox(this, 148, 16, 164, 7);
		_txtArticle = new Text(230, 9, 8, 24);
		_btnPrev = new TextButton(33, 14, 242, 24);
		_btnNext = new TextButton(33, 14, 279, 24);
		_lstRawData = new TextList(287, 128, 8, 40);
		_btnIncludeDebug = new ToggleTextButton(70, 16, 8, 176);
		_btnIncludeIds = new ToggleTextButton(70, 16, 86, 176);
		_btnIncludeDefaults = new ToggleTextButton(70, 16, 164, 176);
		_btnOk = new TextButton(70, 16, 242, 176);
		_btnPreview = new TextButton(102, 16, 210, 7);

		// Set palette
		SetInterface("statsForNerds");

		_purple = (byte)_game.Mod.GetInterface("statsForNerds").GetElement("list").color;
		_pink = (byte)_game.Mod.GetInterface("statsForNerds").GetElement("list").color2;
		_blue = (byte)_game.Mod.GetInterface("statsForNerds").GetElement("list").border;
		_white = (byte)_game.Mod.GetInterface("statsForNerds").GetElement("listExtended").color;
		_gold = (byte)_game.Mod.GetInterface("statsForNerds").GetElement("listExtended").color2;

		Add(_window, "window", "statsForNerds");
		Add(_txtTitle, "text", "statsForNerds");
		Add(_txtArticle, "text", "statsForNerds");
		Add(_lstRawData, "list", "statsForNerds");
		Add(_btnIncludeDebug, "button", "statsForNerds");
		Add(_btnIncludeIds, "button", "statsForNerds");
		Add(_btnIncludeDefaults, "button", "statsForNerds");
		Add(_btnOk, "button", "statsForNerds");
		Add(_btnPreview, "button", "statsForNerds");
		Add(_btnPrev, "button", "statsForNerds");
		Add(_btnNext, "button", "statsForNerds");
		Add(_cbxRelatedStuff, "comboBox", "statsForNerds");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "statsForNerds");

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_STATS_FOR_NERDS"));

		_txtArticle.SetText(Tr("STR_ARTICLE").Arg(Tr(_topicId)));

		_lstRawData.SetColumns(2, 110, 177);
		_lstRawData.SetSelectable(true);
		_lstRawData.SetBackground(_window);
		_lstRawData.SetWordWrap(true);

		_btnIncludeDebug.Text = Tr("STR_INCLUDE_DEBUG");
		_btnIncludeDebug.Pressed = debug;
		_btnIncludeDebug.OnMouseClick(BtnRefreshClick);

		_btnIncludeIds.Text = Tr("STR_INCLUDE_IDS");
		_btnIncludeIds.Pressed = ids;
		_btnIncludeIds.OnMouseClick(BtnRefreshClick);

		_btnIncludeDefaults.Text = Tr("STR_INCLUDE_DEFAULTS");
		_btnIncludeDefaults.Pressed = defaults;
		_btnIncludeDefaults.OnMouseClick(BtnRefreshClick);

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_btnOk.OnKeyboardPress(BtnScrollUpClick, Options.keyGeoUp);
		_btnOk.OnKeyboardPress(BtnScrollDownClick, Options.keyGeoDown);

		_btnPreview.Text = Tr("STR_CRAFT_DEPLOYMENT_PREVIEW");
		_btnPreview.OnMouseClick(BtnPreviewClick);
		_btnPreview.Visible = false;

		_btnPrev.Text = "<<";
		_btnPrev.OnMouseClick(BtnPrevClick);
		_btnPrev.OnKeyboardPress(BtnPrevClick, Options.keyGeoLeft);

		_btnNext.Text = ">>";
		_btnNext.OnMouseClick(BtnNextClick);
		_btnNext.OnKeyboardPress(BtnNextClick, Options.keyGeoRight);

		if (Options.oxceDisableStatsForNerds)
		{
			_txtTitle.SetHeight(_txtTitle.Height * 9);
			_txtTitle.SetWordWrap(true);
			_txtTitle.SetText(Tr("STR_THIS_FEATURE_IS_DISABLED_2"));
			_txtArticle.Visible = false;
			_lstRawData.Visible = false;
			_btnIncludeDebug.Visible = false;
			_btnIncludeIds.Visible = false;
			_btnIncludeDefaults.Visible = false;
			_btnPrev.Visible = false;
			_btnNext.Visible = false;
			_cbxRelatedStuff.Visible = false;
			return;
		}

		if (!_mainArticle)
		{
			_btnPrev.Visible = false;
			_btnNext.Visible = false;
		}

		_filterOptions.Clear();
		_filterOptions.Add("STR_UNKNOWN");
		_cbxRelatedStuff.SetOptions(_filterOptions, true);
		_cbxRelatedStuff.OnChange(CbxAmmoSelect);
		_cbxRelatedStuff.Visible = _filterOptions.Count > 1;
	}

	/// <summary>Initializes the screen (fills the data).</summary>
	public override void Init()
	{
		base.Init();

		if (!Options.oxceDisableStatsForNerds)
		{
			InitLists();

			_btnPreview.Visible = false;
			if (_typeId == UfopaediaTypeId.UFOPAEDIA_TYPE_CRAFT || _typeId == UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_CRAFT)
			{
				if (_game.SavedGame == null)
				{
					// how did we even get here?
					return;
				}
				if (_game.SavedGame.SavedBattle != null)
				{
					// there is already a battle going on, don't start another one
					return;
				}
				if (!_game.ContainsUfopaediaStartState())
				{
					// let's ignore any random M-click articles too
					// and allow the preview only if the ufopedia was opened from the main geoscape menu
					return;
				}
				RuleCraft craftRule = _game.Mod.GetCraft(_topicId);
				if (craftRule.IsForNewBattle)
				{
					var data = _game.SavedGame.CustomRuleCraftDeployments;
					// update the label to indicate presence of a saved deployment
					if (data.ContainsKey(craftRule.Type))
						_btnPreview.Text = Tr("STR_CRAFT_DEPLOYMENT_PREVIEW_SAVED");
					else
						_btnPreview.Text = Tr("STR_CRAFT_DEPLOYMENT_PREVIEW");

					_txtTitle.Align = TextHAlign.ALIGN_LEFT;
					_btnPreview.Visible = true;
				}
			}
		}
	}

	/// <summary>Get ufopaedia type Id.</summary>
	public UfopaediaTypeId TypeId => _typeId;

	/// <summary>Get current topic Id.</summary>
	public string TopicId => _topicId;

	/// <summary>Opens the details for the selected ammo item.</summary>
	public void CbxAmmoSelect(Action action)
	{
		int selIdx = _cbxRelatedStuff.Selected;
		if (selIdx > 0)
		{
			if (_typeId == UfopaediaTypeId.UFOPAEDIA_TYPE_ITEM || _typeId == UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_ITEM)
			{
				// perform same checks as in ArticleStateItem.cpp
				string ammoId = _filterOptions[selIdx];
				ArticleDefinition ammo_article = _game.Mod.GetUfopaediaArticle(ammoId, true);
				if (Ufopaedia.IsArticleAvailable(_game.SavedGame, ammo_article))
				{
					RuleItem ammo_rule = _game.Mod.GetItem(ammoId, true);
					_game.PushState(new StatsForNerdsState(UfopaediaTypeId.UFOPAEDIA_TYPE_ITEM, ammo_rule.Type, _btnIncludeDebug.Pressed, _btnIncludeIds.Pressed, _btnIncludeDefaults.Pressed));
				}
			}
			else if (_typeId == UfopaediaTypeId.UFOPAEDIA_TYPE_ARMOR || _typeId == UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_ARMOR)
			{
				// perform similar checks as above, but don't crash if article is not found
				string builtInItemId = _filterOptions[selIdx];
				ArticleDefinition builtInItem_article = _game.Mod.GetUfopaediaArticle(builtInItemId, false);
				if (builtInItem_article != null && Ufopaedia.IsArticleAvailable(_game.SavedGame, builtInItem_article))
				{
					RuleItem builtInItem_rule = _game.Mod.GetItem(builtInItemId, true);
					_game.PushState(new StatsForNerdsState(UfopaediaTypeId.UFOPAEDIA_TYPE_ITEM, builtInItem_rule.Type, _btnIncludeDebug.Pressed, _btnIncludeIds.Pressed, _btnIncludeDefaults.Pressed));
				}
			}
		}
	}

	/// <summary>Refresh the displayed data including/excluding the raw IDs.</summary>
	public void BtnRefreshClick(Action action)
	{
		InitLists();
	}

	/// <summary>Returns to the previous screen.</summary>
	public void BtnOkClick(Action action)
	{
		if (_game.IsCtrlPressed())
		{
			Logger.Info(_txtArticle.GetText());
			for (int row = 0; row < _lstRawData.Texts; ++row)
			{
				Logger.Info($"{_lstRawData.GetCellText(row, 0)}\t\t{_lstRawData.GetCellText(row, 1)}");
			}
			return;
		}
		_game.PopState();
	}

	/// <summary>Shows the battlescape preview.</summary>
	public void BtnPreviewClick(Action action)
	{
		Mod mod = _game.Mod;
		SavedGame save = _game.SavedGame;
		Base @base = save.PreviewBase;
		if (@base == null)
		{
			// create a new base
			// this base is temporary (i.e. not saved in the save file), but we also don't have to create a new one each time
			@base = new Base(mod);
			save.PreviewBase = @base;
			@base.SetName("Preview Base");

			// create some 1x1 soldiers (as many as needed for the biggest craft type)
			RuleSoldier soldierRule = null;
			Armor defaultArmor = null;
			foreach (var soldierType in mod.SoldiersList)
			{
				soldierRule = mod.GetSoldier(soldierType);
				defaultArmor = soldierRule.DefaultArmor;
				if (defaultArmor.Size == 1)
				{
					break;
				}
			}
			int biggest = 0;
			foreach (var craftType in mod.CraftsList)
			{
				RuleCraft cRule = mod.GetCraft(craftType);
				if (cRule.MaxUnitsLimit > biggest)
				{
					biggest = cRule.MaxUnitsLimit;
				}
			}
			for (int i = 0; i < biggest; ++i)
			{
				// we use NEGATIVE soldier IDs to make sure there is not even a theoretical chance of modifying real geoscape soldiers during the preview
				int newId = -(i + 1);
				Soldier soldier = new Soldier(soldierRule, defaultArmor, 0 /*nationality*/, newId);
				@base.Soldiers.Add(soldier);
				soldier.SetName("Position" + FormatInt(newId));
			}
		}

		// now clean up from the previous preview
		{
			foreach (var soldier in @base.Soldiers)
			{
				soldier.SetCraft(null);
			}
			@base.Crafts.Clear();
		}

		// and finally create the craft we need
		RuleCraft craftRule = mod.GetCraft(_topicId);
		Craft c = new Craft(craftRule, @base, RuleCraft.DUMMY_CRAFT_ID); // a negative integer
		@base.Crafts.Add(c);
		c.SetName(Tr(craftRule.Type));
		int max = craftRule.MaxUnitsLimit;
		foreach (var soldier in @base.Soldiers)
		{
			soldier.SetCraft(c);
			max--;
			if (max <= 0) break;
		}

		SavedBattleGame bgame = new SavedBattleGame(_game.Mod, _game.Language, true);
		_game.SavedGame.SetBattleGame(bgame);
		BattlescapeGenerator bgen = new BattlescapeGenerator(_game);
		bgame.MissionType = c.Rules.CustomPreviewType;
		bgame.CraftForPreview = c;
		bgen.SetCraft(c);
		bgen.Run();

		// needed for preview of craft deployment tiles
		bgame.SetCraftPos(bgen.CraftPos);
		bgame.SetCraftZ(bgen.CraftZ);
		bgame.CalculateCraftTiles();

		_game.PushState(new BriefingState(c, null));
	}

	/// <summary>Shows the previous available article.</summary>
	public void BtnPrevClick(Action action)
	{
		Ufopaedia.PrevDetail(_game, _state, _btnIncludeDebug.Pressed, _btnIncludeIds.Pressed, _btnIncludeDefaults.Pressed);
	}

	/// <summary>Shows the next available article. Loops to the first.</summary>
	public void BtnNextClick(Action action)
	{
		Ufopaedia.NextDetail(_game, _state, _btnIncludeDebug.Pressed, _btnIncludeIds.Pressed, _btnIncludeDefaults.Pressed);
	}

	/// <summary>Scrolls the main table up by one row.</summary>
	public void BtnScrollUpClick(Action action)
	{
		_lstRawData.ScrollUp(false);
	}

	/// <summary>Scrolls the main table down by one row.</summary>
	public void BtnScrollDownClick(Action action)
	{
		_lstRawData.ScrollDown(false);
	}

	/// <summary>Shows the "raw" data.</summary>
	private void InitLists()
	{
		_showDebug = _btnIncludeDebug.Pressed;
		_showIds = _btnIncludeIds.Pressed;
		_showDefaults = _btnIncludeDefaults.Pressed;
		_counter = 0;
		_indent = false;

		switch (_typeId)
		{
		case UfopaediaTypeId.UFOPAEDIA_TYPE_ITEM:
		case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_ITEM:
			InitItemList();
			break;
		case UfopaediaTypeId.UFOPAEDIA_TYPE_ARMOR:
		case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_ARMOR:
			InitArmorList();
			break;
		case UfopaediaTypeId.UFOPAEDIA_TYPE_BASE_FACILITY:
		case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_BASE_FACILITY:
			InitFacilityList();
			break;
		case UfopaediaTypeId.UFOPAEDIA_TYPE_CRAFT:
		case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_CRAFT:
			InitCraftList();
			break;
		case UfopaediaTypeId.UFOPAEDIA_TYPE_UFO:
		case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_USO:
			InitUfoList();
			break;
		case UfopaediaTypeId.UFOPAEDIA_TYPE_CRAFT_WEAPON:
		case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_CRAFT_WEAPON:
			InitCraftWeaponList();
			break;
		case UfopaediaTypeId.UFOPAEDIA_TYPE_SOLDIER:
			InitSoldierList();
			break;
		case UfopaediaTypeId.UFOPAEDIA_TYPE_UNIT:
			InitUnitList();
			break;
		case UfopaediaTypeId.UFOPAEDIA_TYPE_UNKNOWN:
			InitSoldierBonusList();
			break; // C++ falls through into "default: break;"
		default:
			break;
		}
	}

	/// <summary>Resets the string stream.</summary>
	private static void ResetStream(StringBuilder ss)
	{
		ss.Clear();
	}

	/// <summary>Adds a translatable item to the string stream, optionally with string ID.</summary>
	private void AddTranslation(StringBuilder ss, string id)
	{
		ss.Append(Tr(id));
		if (_showIds)
		{
			ss.Append(" [").Append(id).Append(']');
		}
	}

	/// <summary>Translates a property name.</summary>
	private string Trp(string propertyName)
	{
		if (_showDebug)
		{
			if (_indent)
			{
				string indentation = " ";
				string code = propertyName;
				return indentation + code;
			}
			else
			{
				return propertyName;
			}
		}
		else
		{
			if (_indent)
			{
				string indentation = " ";
				string translation = Tr(propertyName);
				return indentation + translation;
			}
			else
			{
				return Tr(propertyName);
			}
		}
	}

	/// <summary>Adds a section name to the table.</summary>
	private void AddSection(string name, string desc, byte color, bool forceShow = false)
	{
		if (_showDefaults || forceShow)
		{
			_lstRawData.AddRow(2, name, desc);
			_lstRawData.SetRowColor(_lstRawData.LastRowIndex, color);
		}

		// reset counter
		_counter = 0;
	}

	/// <summary>Adds a group heading to the table.</summary>
	private void AddHeading(string propertyName, string moreDetail = "", bool addDifficulty = false)
	{
		if (string.IsNullOrEmpty(moreDetail))
		{
			_lstRawData.AddRow(2, Trp(propertyName), "");
		}
		else
		{
			var ss2 = new StringBuilder();
			if (addDifficulty)
			{
				string diff;
				switch (_game.SavedGame.Difficulty)
				{
					case GameDifficulty.DIFF_SUPERHUMAN: diff = Tr("STR_5_SUPERHUMAN"); break;
					case GameDifficulty.DIFF_GENIUS: diff = Tr("STR_4_GENIUS"); break;
					case GameDifficulty.DIFF_VETERAN: diff = Tr("STR_3_VETERAN"); break;
					case GameDifficulty.DIFF_EXPERIENCED: diff = Tr("STR_2_EXPERIENCED"); break;
					default: diff = Tr("STR_1_BEGINNER"); break;
				}
				ss2.Append(Tr(moreDetail).Arg(diff));
			}
			else
			{
				AddTranslation(ss2, moreDetail);
			}
			_lstRawData.AddRow(2, Trp(propertyName), ss2.ToString());
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _white);
		}
		_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 0, _blue);

		// reset counter
		_counter = 0;
		_indent = true;
	}

	/// <summary>Ends a group with a heading, potentially removing the heading... if the group turned out to be empty.</summary>
	private void EndHeading()
	{
		if (_counter == 0)
		{
			_lstRawData.RemoveLastRow();
		}
		_indent = false;
	}

	/// <summary>Adds a vector of generic types.</summary>
	private void AddVectorOfGeneric<T>(StringBuilder ss, IReadOnlyCollection<T> vec, string propertyName, Func<T, string> callback, bool translate = true)
	{
		if (vec.Count == 0 && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		int i = 0;
		ss.Append('{');
		foreach (var item in vec)
		{
			if (i > 0)
			{
				ss.Append(", ");
			}
			if (translate)
			{
				AddTranslation(ss, callback(item));
			}
			else
			{
				ss.Append(callback(item));
			}
			i++;
		}
		ss.Append('}');
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (vec.Count != 0)
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds a single string value to the table.</summary>
	private void AddSingleString(StringBuilder ss, string id, string propertyName, string defaultId = "", bool translate = true)
	{
		if (id == defaultId && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		if (translate)
		{
			AddTranslation(ss, id);
		}
		else
		{
			ss.Append(id);
		}
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (id != defaultId)
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds a vector of strings to the table.</summary>
	private void AddVectorOfStrings(StringBuilder ss, IReadOnlyCollection<string> vec, string propertyName, bool translate = true)
	{
		AddVectorOfGeneric(ss, vec, propertyName, s => s, translate);
	}

	/// <summary>Adds a vector of RuleResearch names to the table.</summary>
	private void AddVectorOfResearch(StringBuilder ss, IReadOnlyCollection<RuleResearch> vec, string propertyName)
	{
		AddVectorOfGeneric(ss, vec, propertyName, item => item.Name);
	}

	// Script API (C++ addIntegerScript/addTextScript/addTextFormat1Script/addTextFormat2Script), see StatsForNerdsState.Script.cs.

	internal void AddIntegerScript(string propertyName, int value)
	{
		_lstRawData.AddRow(2, Trp(propertyName), FormatInt(value));
		_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		++_counter;
	}

	internal void AddTextScript(string propertyName, string value)
	{
		string text = _showDebug ? value : (string)Tr(value);
		_lstRawData.AddRow(2, Trp(propertyName), text);
		_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		++_counter;
	}

	internal void AddTextFormat1Script(string propertyName, string format, int value1)
	{
		string text = Tr(format).Arg(value1);
		_lstRawData.AddRow(2, Trp(propertyName), text);
		_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		++_counter;
	}

	internal void AddTextFormat2Script(string propertyName, string format, int value1, int value2)
	{
		string text = Tr(format).Arg(value1).Arg(value2);
		_lstRawData.AddRow(2, Trp(propertyName), text);
		_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		++_counter;
	}

	/// <summary>Adds a single boolean value to the table.</summary>
	private void AddBoolean(StringBuilder ss, bool value, string propertyName, bool defaultvalue = false)
	{
		if (value == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		if (value)
		{
			ss.Append(Tr("STR_TRUE"));
		}
		else
		{
			ss.Append(Tr("STR_FALSE"));
		}
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (value != defaultvalue)
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds a single floating point number to the table.</summary>
	private void AddFloat(StringBuilder ss, float value, string propertyName, float defaultvalue = 0.0f)
	{
		if (AreSame(value, defaultvalue) && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		ss.Append(FormatFloat(value));
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (!AreSame(value, defaultvalue))
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds a single floating point number (formatted as percentage) to the table.</summary>
	private void AddFloatAsPercentage(StringBuilder ss, float value, string propertyName, float defaultvalue = 0.0f)
	{
		if (AreSame(value, defaultvalue) && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		ss.Append(FormatFloat(value * 100.0f)).Append('%');
		if (_showIds)
		{
			ss.Append(" [").Append(FormatFloat(value)).Append(']');
		}
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (!AreSame(value, defaultvalue))
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds a single floating point (double precision) number to the table.</summary>
	private void AddDouble(StringBuilder ss, double value, string propertyName, double defaultvalue = 0.0)
	{
		if (AreSame(value, defaultvalue) && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		ss.Append(FormatDouble(value));
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (!AreSame(value, defaultvalue))
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds a single integer number to the table.</summary>
	private void AddInteger(StringBuilder ss, int value, string propertyName, int defaultvalue = 0, bool formatAsMoney = false, string specialTranslation = "", int specialvalue = -1)
	{
		if (value == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		if (value == specialvalue && !string.IsNullOrEmpty(specialTranslation))
		{
			ss.Append(Tr(specialTranslation));
			if (_showIds)
			{
				ss.Append(" [").Append(FormatInt(specialvalue)).Append(']');
			}
		}
		else
		{
			if (formatAsMoney)
			{
				ss.Append(Unicode.FormatFunding(value));
			}
			else
			{
				ss.Append(FormatInt(value));
			}
		}
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (value != defaultvalue)
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds all script tags of a rule to the table.</summary>
	private void AddScriptTags<T>(StringBuilder ss, ScriptValues<T> values)
	{
		var tagValues = values.GetValuesRaw();
		ArgEnum index = ScriptTag<T>.Type();
		var tagNames = _game.Mod.ScriptGlobal.TagNames[index];
		for (int i = 0; i < tagValues.Count; ++i)
		{
			var nameAsString = tagNames.values[i].name.ToString().Substring(4);
			AddIntegerScriptTag(ss, tagValues[i], nameAsString);
		}
	}

	/// <summary>Adds a script tag to the table.</summary>
	private void AddIntegerScriptTag(StringBuilder ss, int value, string propertyName, int defaultvalue = 0)
	{
		if (value == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		ss.Append(Trp(propertyName));
		ss.Append(": ");
		ss.Append(FormatInt(value));
		_lstRawData.SetFlooding(true);
		_lstRawData.AddRow(1, ss.ToString());
		_lstRawData.SetFlooding(false);
		++_counter;
		if (value != defaultvalue)
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 0, _pink);
		}
	}

	/// <summary>Adds a single integer number (representing a percentage) to the table.</summary>
	private void AddIntegerPercent(StringBuilder ss, int value, string propertyName, int defaultvalue = 0)
	{
		if (value == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		ss.Append(FormatInt(value));
		// -1 is not a percentage, usually means "take value from somewhere else"
		if (value != -1)
		{
			ss.Append('%');
		}
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (value != defaultvalue)
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds a single integer number (representing a distance in nautical miles) to the table.</summary>
	private void AddIntegerNauticalMiles(StringBuilder ss, int value, string propertyName, int defaultvalue = 0)
	{
		if (value == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		ss.Append(Tr("STR_NAUTICAL_MILES").Arg(value));
		ss.Append(" = ");
		ss.Append(Tr("STR_KILOMETERS").Arg(value * 1852 / 1000));
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (value != defaultvalue)
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds a single integer number (representing a speed in knots = nautical miles per hour) to the table.</summary>
	private void AddIntegerKnots(StringBuilder ss, int value, string propertyName, int defaultvalue = 0)
	{
		if (value == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		ss.Append(Tr("STR_KNOTS").Arg(value));
		ss.Append(" = ");
		ss.Append(Tr("STR_KILOMETERS_PER_HOUR").Arg(value * 1852 / 1000));
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (value != defaultvalue)
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds a single integer number (representing a distance in kilometers) to the table.</summary>
	private void AddIntegerKm(StringBuilder ss, int value, string propertyName, int defaultvalue = 0)
	{
		if (value == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		ss.Append(Tr("STR_KILOMETERS").Arg(value));
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (value != defaultvalue)
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds one or two integer numbers (representing a duration in seconds) to the table.</summary>
	private void AddIntegerSeconds(StringBuilder ss, int value, string propertyName, int defaultvalue = 0, int value2 = -1)
	{
		if (value == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		if (value2 == -1)
		{
			ss.Append(Tr("STR_SECONDS_LONG").Arg(value));
		}
		else
		{
			ss.Append(Tr("STR_SECONDS_LONG").Arg(FormatInt(value) + "-" + FormatInt(value2)));
		}
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (value != defaultvalue)
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds a vector of integer numbers to the table.</summary>
	private void AddVectorOfIntegers(StringBuilder ss, IReadOnlyCollection<int> vec, string propertyName)
	{
		if (vec.Count == 0 && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		int i = 0;
		ss.Append('{');
		foreach (var item in vec)
		{
			if (i > 0)
			{
				ss.Append(", ");
			}
			ss.Append(FormatInt(item));
			i++;
		}
		ss.Append('}');
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (vec.Count != 0)
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds a map of integer/integer numbers to the table.</summary>
	private void AddMapOfIntegers(StringBuilder ss, SortedDictionary<int, int> map, string propertyName)
	{
		if (map.Count == 0 && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		int i = 0;
		ss.Append('{');
		foreach (var item in map)
		{
			if (i > 0)
			{
				ss.Append(", ");
			}
			ss.Append(FormatInt(item.Key)).Append(':').Append(FormatInt(item.Value));
			i++;
		}
		ss.Append('}');
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (map.Count != 0)
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds an enum value as " [n]" (C++ streams the underlying integer).</summary>
	private void AddEnumId(StringBuilder ss, long value)
	{
		if (_showIds)
		{
			ss.Append(" [").Append(FormatInt(value)).Append(']');
		}
	}

	/// <summary>Adds the row of an enum-like value and highlights it when it differs from the default.</summary>
	private void AddEnumRow(StringBuilder ss, string propertyName, bool isDefault)
	{
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (!isDefault)
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds a BattleType to the table.</summary>
	private void AddBattleType(StringBuilder ss, BattleType value, string propertyName, BattleType defaultvalue = BattleType.BT_NONE)
	{
		if (value == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		switch (value)
		{
			case BattleType.BT_NONE: ss.Append(Tr("BT_NONE")); break;
			case BattleType.BT_FIREARM: ss.Append(Tr("BT_FIREARM")); break;
			case BattleType.BT_AMMO: ss.Append(Tr("BT_AMMO")); break;
			case BattleType.BT_MELEE: ss.Append(Tr("BT_MELEE")); break;
			case BattleType.BT_GRENADE: ss.Append(Tr("BT_GRENADE")); break;
			case BattleType.BT_PROXIMITYGRENADE: ss.Append(Tr("BT_PROXIMITYGRENADE")); break;
			case BattleType.BT_MEDIKIT: ss.Append(Tr("BT_MEDIKIT")); break;
			case BattleType.BT_SCANNER: ss.Append(Tr("BT_SCANNER")); break;
			case BattleType.BT_MINDPROBE: ss.Append(Tr("BT_MINDPROBE")); break;
			case BattleType.BT_PSIAMP: ss.Append(Tr("BT_PSIAMP")); break;
			case BattleType.BT_FLARE: ss.Append(Tr("BT_FLARE")); break;
			case BattleType.BT_CORPSE: ss.Append(Tr("BT_CORPSE")); break;
			default: ss.Append(Tr("STR_UNKNOWN")); break;
		}
		AddEnumId(ss, (long)value);
		AddEnumRow(ss, propertyName, value == defaultvalue);
	}

	/// <summary>Adds a DamageType to the table.</summary>
	private void AddDamageType(StringBuilder ss, ItemDamageType value, string propertyName, ItemDamageType defaultvalue = ItemDamageType.DT_NONE)
	{
		if (value == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		switch (value)
		{
			case ItemDamageType.DT_NONE: ss.Append(Tr("STR_DAMAGE_NONE")); break;
			case ItemDamageType.DT_AP: ss.Append(Tr("STR_DAMAGE_ARMOR_PIERCING")); break;
			case ItemDamageType.DT_IN: ss.Append(Tr("STR_DAMAGE_INCENDIARY")); break;
			case ItemDamageType.DT_HE: ss.Append(Tr("STR_DAMAGE_HIGH_EXPLOSIVE")); break;
			case ItemDamageType.DT_LASER: ss.Append(Tr("STR_DAMAGE_LASER_BEAM")); break;
			case ItemDamageType.DT_PLASMA: ss.Append(Tr("STR_DAMAGE_PLASMA_BEAM")); break;
			case ItemDamageType.DT_STUN: ss.Append(Tr("STR_DAMAGE_STUN")); break;
			case ItemDamageType.DT_MELEE: ss.Append(Tr("STR_DAMAGE_MELEE")); break;
			case ItemDamageType.DT_ACID: ss.Append(Tr("STR_DAMAGE_ACID")); break;
			case ItemDamageType.DT_SMOKE: ss.Append(Tr("STR_DAMAGE_SMOKE")); break;
			case ItemDamageType.DT_10: ss.Append(Tr("STR_DAMAGE_10")); break;
			case ItemDamageType.DT_11: ss.Append(Tr("STR_DAMAGE_11")); break;
			case ItemDamageType.DT_12: ss.Append(Tr("STR_DAMAGE_12")); break;
			case ItemDamageType.DT_13: ss.Append(Tr("STR_DAMAGE_13")); break;
			case ItemDamageType.DT_14: ss.Append(Tr("STR_DAMAGE_14")); break;
			case ItemDamageType.DT_15: ss.Append(Tr("STR_DAMAGE_15")); break;
			case ItemDamageType.DT_16: ss.Append(Tr("STR_DAMAGE_16")); break;
			case ItemDamageType.DT_17: ss.Append(Tr("STR_DAMAGE_17")); break;
			case ItemDamageType.DT_18: ss.Append(Tr("STR_DAMAGE_18")); break;
			case ItemDamageType.DT_19: ss.Append(Tr("STR_DAMAGE_19")); break;
			default: ss.Append(Tr("STR_UNKNOWN")); break;
		}
		AddEnumId(ss, (long)value);
		AddEnumRow(ss, propertyName, value == defaultvalue);
	}

	/// <summary>Adds a DamageRandomType to the table.</summary>
	private void AddDamageRandomType(StringBuilder ss, ItemDamageRandomType value, string propertyName, ItemDamageRandomType defaultvalue = ItemDamageRandomType.DRT_DEFAULT)
	{
		if (value == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		switch (value)
		{
			case ItemDamageRandomType.DRT_DEFAULT: ss.Append(Tr("DRT_DEFAULT")); break;
			case ItemDamageRandomType.DRT_UFO: ss.Append(Tr("DRT_UFO")); break;
			case ItemDamageRandomType.DRT_TFTD: ss.Append(Tr("DRT_TFTD")); break;
			case ItemDamageRandomType.DRT_FLAT: ss.Append(Tr("DRT_FLAT")); break;
			case ItemDamageRandomType.DRT_FIRE: ss.Append(Tr("DRT_FIRE")); break;
			case ItemDamageRandomType.DRT_NONE: ss.Append(Tr("DRT_NONE")); break;
			case ItemDamageRandomType.DRT_UFO_WITH_TWO_DICE: ss.Append(Tr("DRT_UFO_WITH_TWO_DICE")); break;
			case ItemDamageRandomType.DRT_EASY: ss.Append(Tr("DRT_EASY")); break;
			case ItemDamageRandomType.DRT_STANDARD: ss.Append(Tr("DRT_STANDARD")); break;
			case ItemDamageRandomType.DRT_EXPLOSION: ss.Append(Tr("DRT_EXPLOSION")); break;
			default: ss.Append(Tr("STR_UNKNOWN")); break;
		}
		AddEnumId(ss, (long)value);
		AddEnumRow(ss, propertyName, value == defaultvalue);
	}

	/// <summary>Adds a BattleFuseType to the table.</summary>
	private void AddBattleFuseType(StringBuilder ss, BattleFuseType value, string propertyName, BattleFuseType defaultvalue = BattleFuseType.BFT_NONE)
	{
		if (value == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		switch (value)
		{
			case BattleFuseType.BFT_NONE: ss.Append(Tr("BFT_NONE")); break;
			case BattleFuseType.BFT_INSTANT: ss.Append(Tr("BFT_INSTANT")); break;
			case BattleFuseType.BFT_SET: ss.Append(Tr("BFT_SET")); break;
			default:
			{
				if (value >= BattleFuseType.BFT_FIX_MIN && value < BattleFuseType.BFT_FIX_MAX)
				{
					ss.Append(Tr("BFT_FIXED"));
				}
				else
				{
					ss.Append(Tr("STR_UNKNOWN"));
				}
				break;
			}
		}
		AddEnumId(ss, (long)value);
		AddEnumRow(ss, propertyName, value == defaultvalue);
	}

	/// <summary>Adds a single RuleItemUseCost to the table.</summary>
	private void AddRuleItemUseCostBasic(StringBuilder ss, RuleItemUseCost value, string propertyName, int defaultvalue = 0)
	{
		if (value.Time == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		ss.Append(FormatInt(value.Time));
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (value.Time != defaultvalue)
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds a number to the string stream, optionally formatted as boolean.</summary>
	private void AddBoolOrInteger(StringBuilder ss, int value, bool formatAsBoolean)
	{
		if (formatAsBoolean)
		{
			if (value == 1)
				ss.Append(Tr("STR_TRUE"));
			else if (value == 0)
				ss.Append(Tr("STR_FALSE"));
			else
				ss.Append(FormatInt(value));
		}
		else
		{
			ss.Append(FormatInt(value));
		}
	}

	/// <summary>Adds a percentage sign to the string stream, unless the value is flat (1).</summary>
	private static void AddPercentageSignOrNothing(StringBuilder ss, int value, bool smartFormat)
	{
		if (smartFormat)
		{
			// 1 means flat: true
			if (value != 1)
			{
				ss.Append('%');
			}
		}
	}

	/// <summary>Adds a full RuleItemUseCost to the table (C++ template over short/bool; values are given as ints).</summary>
	private void AddRuleItemUseCostFull(StringBuilder ss, int[] value, string propertyName, int[] defaultvalue = null, bool smartFormat = false, RuleItemUseFlat formatBy = null)
	{
		defaultvalue ??= new int[6];
		formatBy ??= new RuleItemUseFlat();
		bool isDefault = false;
		if (value[0] == defaultvalue[0] &&
			value[1] == defaultvalue[1] &&
			value[2] == defaultvalue[2] &&
			value[3] == defaultvalue[3] &&
			value[4] == defaultvalue[4] &&
			value[5] == defaultvalue[5])
		{
			isDefault = true;
		}
		if (isDefault && !_showDefaults)
		{
			return;
		}
		bool isFlatAttribute = propertyName.StartsWith("flat", StringComparison.Ordinal);
		ResetStream(ss);
		bool isFirst = true;
		// always show non-zero TUs, even if it's a default value
		if (value[0] != 0 || _showDefaults)
		{
			ss.Append(Tr("STR_COST_TIME")).Append(": ");
			AddBoolOrInteger(ss, value[0], isFlatAttribute);
			AddPercentageSignOrNothing(ss, formatBy.Time ? 1 : 0, smartFormat);
			isFirst = false;
		}
		if (value[1] != defaultvalue[1] || _showDefaults)
		{
			if (!isFirst) ss.Append(", ");
			ss.Append(Tr("STR_COST_ENERGY")).Append(": ");
			AddBoolOrInteger(ss, value[1], isFlatAttribute);
			AddPercentageSignOrNothing(ss, formatBy.Energy ? 1 : 0, smartFormat);
			isFirst = false;
		}
		if (value[2] != defaultvalue[2] || _showDefaults)
		{
			if (!isFirst) ss.Append(", ");
			ss.Append(Tr("STR_COST_MORALE")).Append(": ");
			AddBoolOrInteger(ss, value[2], isFlatAttribute);
			AddPercentageSignOrNothing(ss, formatBy.Morale ? 1 : 0, smartFormat);
			isFirst = false;
		}
		if (value[3] != defaultvalue[3] || _showDefaults)
		{
			if (!isFirst) ss.Append(", ");
			ss.Append(Tr("STR_COST_HEALTH")).Append(": ");
			AddBoolOrInteger(ss, value[3], isFlatAttribute);
			AddPercentageSignOrNothing(ss, formatBy.Health ? 1 : 0, smartFormat);
			isFirst = false;
		}
		if (value[4] != defaultvalue[4] || _showDefaults)
		{
			if (!isFirst) ss.Append(", ");
			ss.Append(Tr("STR_COST_STUN")).Append(": ");
			AddBoolOrInteger(ss, value[4], isFlatAttribute);
			AddPercentageSignOrNothing(ss, formatBy.Stun ? 1 : 0, smartFormat);
			isFirst = false;
		}
		if (value[5] != defaultvalue[5] || _showDefaults)
		{
			if (!isFirst) ss.Append(", ");
			ss.Append(Tr("STR_COST_MANA")).Append(": ");
			AddBoolOrInteger(ss, value[5], isFlatAttribute);
			AddPercentageSignOrNothing(ss, formatBy.Mana ? 1 : 0, smartFormat);
			isFirst = false;
		}
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (!isDefault)
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Values of a RuleItemUseCost as ints (Time, Energy, Morale, Health, Stun, Mana).</summary>
	private static int[] CostValues(RuleItemUseRuleBase<short> v) => [v.Time, v.Energy, v.Morale, v.Health, v.Stun, v.Mana];

	/// <summary>Values of a RuleItemUseFlat as ints (Time, Energy, Morale, Health, Stun, Mana).</summary>
	private static int[] FlatValues(RuleItemUseRuleBase<bool> v) => [v.Time ? 1 : 0, v.Energy ? 1 : 0, v.Morale ? 1 : 0, v.Health ? 1 : 0, v.Stun ? 1 : 0, v.Mana ? 1 : 0];

	/// <summary>Adds a BattleMediKitType to the table.</summary>
	private void AddBattleMediKitType(StringBuilder ss, BattleMediKitType value, string propertyName, BattleMediKitType defaultvalue = BattleMediKitType.BMT_NORMAL)
	{
		if (value == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		switch (value)
		{
			case BattleMediKitType.BMT_NORMAL: ss.Append(Tr("BMT_NORMAL")); break;
			case BattleMediKitType.BMT_HEAL: ss.Append(Tr("BMT_HEAL")); break;
			case BattleMediKitType.BMT_STIMULANT: ss.Append(Tr("BMT_STIMULANT")); break;
			case BattleMediKitType.BMT_PAINKILLER: ss.Append(Tr("BMT_PAINKILLER")); break;
			default: ss.Append(Tr("STR_UNKNOWN")); break;
		}
		AddEnumId(ss, (long)value);
		AddEnumRow(ss, propertyName, value == defaultvalue);
	}

	/// <summary>Adds medikit target types to the table.</summary>
	private void AddMediKitTargets(StringBuilder ss, RuleItem value, string propertyName, int defaultvalue)
	{
		if (value.MedikitTargetMatrixRaw == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		ss.Append('{');
		// FIXME: make translatable one day, when some better default names are suggested
		if (value.AllowTargetFriendGround)
			ss.Append("friend down").Append(", ");
		if (value.AllowTargetFriendStanding)
			ss.Append("friend up").Append(", ");
		if (value.AllowTargetHostileGround)
			ss.Append("hostile down").Append(", ");
		if (value.AllowTargetHostileStanding)
			ss.Append("hostile up").Append(", ");
		if (value.AllowTargetNeutralGround)
			ss.Append("neutral down").Append(", ");
		if (value.AllowTargetNeutralStanding)
			ss.Append("neutral up").Append(", ");
		ss.Append('}');
		if (_showIds)
		{
			ss.Append(" [").Append(FormatInt(value.MedikitTargetMatrixRaw)).Append(']');
		}
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (value.MedikitTargetMatrixRaw != defaultvalue)
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds item target types to the table.</summary>
	private void AddItemTargets(StringBuilder ss, RuleItem value, string propertyName, int defaultvalue)
	{
		if (value.TargetMatrixRaw == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		ss.Append('{');
		// FIXME: make translatable one day, when some better default names are suggested
		if (value.IsTargetAllowed(UnitFaction.FACTION_PLAYER, UnitFaction.FACTION_PLAYER))
			ss.Append("friend").Append(", ");
		if (value.IsTargetAllowed(UnitFaction.FACTION_HOSTILE, UnitFaction.FACTION_PLAYER))
			ss.Append("hostile").Append(", ");
		if (value.IsTargetAllowed(UnitFaction.FACTION_NEUTRAL, UnitFaction.FACTION_PLAYER))
			ss.Append("neutral").Append(", ");
		ss.Append('}');
		if (_showIds)
		{
			ss.Append(" [").Append(FormatInt(value.TargetMatrixRaw)).Append(']');
		}
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (value.TargetMatrixRaw != defaultvalue)
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds a ExperienceTrainingMode to the table.</summary>
	private void AddExperienceTrainingMode(StringBuilder ss, ExperienceTrainingMode value, string propertyName, ExperienceTrainingMode defaultvalue = ExperienceTrainingMode.ETM_DEFAULT)
	{
		if (value == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		switch (value)
		{
			case ExperienceTrainingMode.ETM_DEFAULT: ss.Append(Tr("ETM_DEFAULT")); break;
			case ExperienceTrainingMode.ETM_MELEE_100: ss.Append(Tr("ETM_MELEE_100")); break;
			case ExperienceTrainingMode.ETM_MELEE_50: ss.Append(Tr("ETM_MELEE_50")); break;
			case ExperienceTrainingMode.ETM_MELEE_33: ss.Append(Tr("ETM_MELEE_33")); break;
			case ExperienceTrainingMode.ETM_FIRING_100: ss.Append(Tr("ETM_FIRING_100")); break;
			case ExperienceTrainingMode.ETM_FIRING_50: ss.Append(Tr("ETM_FIRING_50")); break;
			case ExperienceTrainingMode.ETM_FIRING_33: ss.Append(Tr("ETM_FIRING_33")); break;
			case ExperienceTrainingMode.ETM_THROWING_100: ss.Append(Tr("ETM_THROWING_100")); break;
			case ExperienceTrainingMode.ETM_THROWING_50: ss.Append(Tr("ETM_THROWING_50")); break;
			case ExperienceTrainingMode.ETM_THROWING_33: ss.Append(Tr("ETM_THROWING_33")); break;
			case ExperienceTrainingMode.ETM_FIRING_AND_THROWING: ss.Append(Tr("ETM_FIRING_AND_THROWING")); break;
			case ExperienceTrainingMode.ETM_FIRING_OR_THROWING: ss.Append(Tr("ETM_FIRING_OR_THROWING")); break;
			case ExperienceTrainingMode.ETM_REACTIONS: ss.Append(Tr("ETM_REACTIONS")); break;
			case ExperienceTrainingMode.ETM_REACTIONS_AND_MELEE: ss.Append(Tr("ETM_REACTIONS_AND_MELEE")); break;
			case ExperienceTrainingMode.ETM_REACTIONS_AND_FIRING: ss.Append(Tr("ETM_REACTIONS_AND_FIRING")); break;
			case ExperienceTrainingMode.ETM_REACTIONS_AND_THROWING: ss.Append(Tr("ETM_REACTIONS_AND_THROWING")); break;
			case ExperienceTrainingMode.ETM_REACTIONS_OR_MELEE: ss.Append(Tr("ETM_REACTIONS_OR_MELEE")); break;
			case ExperienceTrainingMode.ETM_REACTIONS_OR_FIRING: ss.Append(Tr("ETM_REACTIONS_OR_FIRING")); break;
			case ExperienceTrainingMode.ETM_REACTIONS_OR_THROWING: ss.Append(Tr("ETM_REACTIONS_OR_THROWING")); break;
			case ExperienceTrainingMode.ETM_BRAVERY: ss.Append(Tr("ETM_BRAVERY")); break;
			case ExperienceTrainingMode.ETM_BRAVERY_2X: ss.Append(Tr("ETM_BRAVERY_2X")); break;
			case ExperienceTrainingMode.ETM_BRAVERY_AND_REACTIONS: ss.Append(Tr("ETM_BRAVERY_AND_REACTIONS")); break;
			case ExperienceTrainingMode.ETM_BRAVERY_OR_REACTIONS: ss.Append(Tr("ETM_BRAVERY_OR_REACTIONS")); break;
			case ExperienceTrainingMode.ETM_BRAVERY_OR_REACTIONS_2X: ss.Append(Tr("ETM_BRAVERY_OR_REACTIONS_2X")); break;
			case ExperienceTrainingMode.ETM_PSI_STRENGTH: ss.Append(Tr("ETM_PSI_STRENGTH")); break;
			case ExperienceTrainingMode.ETM_PSI_STRENGTH_2X: ss.Append(Tr("ETM_PSI_STRENGTH_2X")); break;
			case ExperienceTrainingMode.ETM_PSI_SKILL: ss.Append(Tr("ETM_PSI_SKILL")); break;
			case ExperienceTrainingMode.ETM_PSI_SKILL_2X: ss.Append(Tr("ETM_PSI_SKILL_2X")); break;
			case ExperienceTrainingMode.ETM_PSI_STRENGTH_AND_SKILL: ss.Append(Tr("ETM_PSI_STRENGTH_AND_SKILL")); break;
			case ExperienceTrainingMode.ETM_PSI_STRENGTH_AND_SKILL_2X: ss.Append(Tr("ETM_PSI_STRENGTH_AND_SKILL_2X")); break;
			case ExperienceTrainingMode.ETM_PSI_STRENGTH_OR_SKILL: ss.Append(Tr("ETM_PSI_STRENGTH_OR_SKILL")); break;
			case ExperienceTrainingMode.ETM_PSI_STRENGTH_OR_SKILL_2X: ss.Append(Tr("ETM_PSI_STRENGTH_OR_SKILL_2X")); break;
			case ExperienceTrainingMode.ETM_NOTHING: ss.Append(Tr("ETM_NOTHING")); break;
			default: ss.Append(Tr("STR_UNKNOWN")); break;
		}
		AddEnumId(ss, (long)value);
		AddEnumRow(ss, propertyName, value == defaultvalue);
	}

	/// <summary>
	/// Formats the bonus terms of a RuleStatBonus (shared by Stats for Nerds and the item article).
	/// </summary>
	/// <param name="value">The bonus.</param>
	/// <param name="getStatName">Returns the text for a stat name (already translated).</param>
	/// <param name="flatOneFallsThrough">C++ ArticleStateItem misses an "else" after "flatOne", so the term also gets the generic suffix there.</param>
	internal static string FormatRuleStatBonus(RuleStatBonus value, Func<string, string> getStatName, bool flatOneFallsThrough)
	{
		var ss = new StringBuilder();
		bool isFirst = true;
		foreach (var item in value.BonusRaw)
		{
			int power = 0;
			foreach (float number in item.second)
			{
				++power;
				if (!AreSame(number, 0.0f))
				{
					float numberAbs = number;
					if (!isFirst)
					{
						if (number > 0.0f)
						{
							ss.Append(" + ");
						}
						else
						{
							ss.Append(" - ");
							numberAbs = Math.Abs(number);
						}
					}
					bool generic = true;
					if (item.first == "flatOne")
					{
						ss.Append(FormatFloat(numberAbs * 1));
						generic = flatOneFallsThrough;
					}
					if (generic)
					{
						if (item.first == "flatHundred")
						{
							ss.Append(FormatDouble(numberAbs * Math.Pow(100, power)));
						}
						else
						{
							if (!AreSame(numberAbs, 1.0f))
							{
								ss.Append(FormatFloat(numberAbs)).Append('*');
							}
							ss.Append(getStatName(item.first));
							if (power > 1)
							{
								ss.Append('^').Append(FormatInt(power));
							}
						}
					}
					isFirst = false;
				}
			}
		}
		return ss.ToString();
	}

	/// <summary>Adds a RuleStatBonus info to the table.</summary>
	private void AddRuleStatBonus(StringBuilder ss, RuleStatBonus value, string propertyName)
	{
		if (!value.IsModded && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		ss.Append(FormatRuleStatBonus(value, stat =>
		{
			var t = new StringBuilder();
			AddTranslation(t, translationMap[stat]);
			return t.ToString();
		}, false));
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (value.IsModded)
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds a ArmorMoveCost info to the table.</summary>
	private void AddRuleArmorMoveCost(StringBuilder ss, ArmorMoveCost value, string propertyName, ArmorMoveCost defaultvalue = default)
	{
		if (value == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		ss.Append(Tr("STR_COST_TIME")).Append(": ");
		AddBoolOrInteger(ss, value.TimePercent, false);
		ss.Append('%');
		ss.Append(", ");
		ss.Append(Tr("STR_COST_ENERGY")).Append(": ");
		AddBoolOrInteger(ss, value.EnergyPercent, false);
		ss.Append('%');
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (value != defaultvalue)
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds a sprite resource path to the table.</summary>
	private void AddSpriteResourcePath(StringBuilder ss, Mod mod, string resourceSetName, int resourceId)
	{
		if (mod.ExtraSprites.TryGetValue(resourceSetName, out var i))
		{
			foreach (var extraSprite in i)
			{
				// strip mod offset from the in-game ID
				int originalSpriteId = resourceId - (extraSprite.ModOwner.offset);

				var mapOfSprites = extraSprite.Sprites;
				if (mapOfSprites.TryGetValue(originalSpriteId, out var individualSprite))
				{
					string numbers = "  " + FormatInt(originalSpriteId) + " + " + FormatInt(extraSprite.ModOwner.offset);

					ResetStream(ss);
					ss.Append(individualSprite);

					_lstRawData.AddRow(2, numbers, ss.ToString());
					++_counter;
					_lstRawData.SetRowColor(_lstRawData.LastRowIndex, _gold);
					return;
				}
			}
		}
	}

	/// <summary>Adds sound resource paths to the table.</summary>
	private void AddSoundVectorResourcePaths(StringBuilder ss, Mod mod, string resourceSetName, IReadOnlyCollection<int> resourceIds)
	{
		if (resourceIds.Count == 0)
		{
			return;
		}

		// go through all extra sounds from all mods
		foreach (var resourceSet in mod.ExtraSounds)
		{
			// only check the relevant ones (e.g. "BATTLE.CAT")
			if (resourceSet.first == resourceSetName)
			{
				foreach (int resourceId in resourceIds)
				{
					// strip mod offset from the in-game ID
					int originalSoundId = resourceId - (resourceSet.second.ModOwner.offset);

					var mapOfSounds = resourceSet.second.Sounds;
					if (mapOfSounds.TryGetValue(originalSoundId, out var individualSound))
					{
						string numbers = "  " + FormatInt(originalSoundId) + " + " + FormatInt(resourceSet.second.ModOwner.offset);

						ResetStream(ss);
						ss.Append(individualSound);

						_lstRawData.AddRow(2, numbers, ss.ToString());
						++_counter;
						_lstRawData.SetRowColor(_lstRawData.LastRowIndex, _gold);
					}
				}
			}
		}
	}

	// ---- helpers used by the generic "Rule" adders (C++ templates requiring getType()/getName()/getId()) ----

	/// <summary>C++ <c>rule->getType()</c> for the rule classes shown in Stats for Nerds.</summary>
	private static string RuleType(object rule) => rule switch
	{
		RuleItem r => r.Type,
		Armor r => r.Type,
		Unit r => r.Type,
		RuleBaseFacility r => r.Type,
		RuleCraft r => r.Type,
		RuleSoldier r => r.Type,
		RuleCommendations r => r.Type,
		_ => throw new InvalidOperationException("StatsForNerdsState: unsupported rule type " + rule.GetType().Name),
	};

	/// <summary>C++ <c>rule->getName()</c> for the rule classes shown in Stats for Nerds.</summary>
	private static string RuleName(object rule) => rule switch
	{
		RuleResearch r => r.Name,
		RuleSoldierBonus r => r.Name,
		_ => throw new InvalidOperationException("StatsForNerdsState: unsupported rule type " + rule.GetType().Name),
	};

	/// <summary>C++ <c>rule->getId()</c> for the rule classes shown in Stats for Nerds.</summary>
	private static string RuleId(object rule) => rule switch
	{
		RuleInventory r => r.Id,
		_ => throw new InvalidOperationException("StatsForNerdsState: unsupported rule type " + rule.GetType().Name),
	};

	/// <summary>Adds generic Rule (requires <c>getType()</c>).</summary>
	private void AddRule(StringBuilder ss, object rule, string propertyName)
	{
		AddSingleString(ss, rule != null ? RuleType(rule) : "", propertyName);
	}

	/// <summary>Adds generic Rule (requires <c>getId()</c>).</summary>
	private void AddRuleId(StringBuilder ss, object rule, string propertyName)
	{
		AddSingleString(ss, rule != null ? RuleId(rule) : "", propertyName);
	}

	/// <summary>Adds generic Rule (requires <c>getName()</c>).</summary>
	private void AddRuleNamed(StringBuilder ss, object rule, string propertyName)
	{
		AddSingleString(ss, rule != null ? RuleName(rule) : "", propertyName);
	}

	/// <summary>Adds a vector of generic Rules (requires <c>getType()</c>).</summary>
	private void AddVectorOfRules(StringBuilder ss, IReadOnlyCollection<object> vec, string propertyName)
	{
		AddVectorOfGeneric(ss, vec, propertyName, RuleType);
	}

	/// <summary>Adds a vector of generic Rules (requires <c>getId()</c>).</summary>
	private void AddVectorOfRulesId(StringBuilder ss, IReadOnlyCollection<object> vec, string propertyName)
	{
		AddVectorOfGeneric(ss, vec, propertyName, RuleId);
	}

	/// <summary>Adds a vector of generic Rules (requires <c>getName()</c>).</summary>
	private void AddVectorOfRulesNamed(StringBuilder ss, IReadOnlyCollection<object> vec, string propertyName)
	{
		AddVectorOfGeneric(ss, vec, propertyName, RuleName);
	}

	/// <summary>Adds a full RuleItemUseCost to the table.</summary>
	private void AddRuleItemUseCostFull(StringBuilder ss, RuleItemUseCost value, string propertyName, RuleItemUseCost defaultvalue = null, bool smartFormat = false, RuleItemUseFlat formatBy = null)
	{
		AddRuleItemUseCostFull(ss, CostValues(value), propertyName, CostValues(defaultvalue ?? new RuleItemUseCost()), smartFormat, formatBy);
	}

	/// <summary>Adds a full RuleItemUseFlat to the table.</summary>
	private void AddRuleItemUseCostFull(StringBuilder ss, RuleItemUseFlat value, string propertyName, RuleItemUseFlat defaultvalue = null, bool smartFormat = false, RuleItemUseFlat formatBy = null)
	{
		AddRuleItemUseCostFull(ss, FlatValues(value), propertyName, FlatValues(defaultvalue ?? new RuleItemUseFlat()), smartFormat, formatBy);
	}

	/// <summary>Adds a unit stat to the string stream, formatted with a label.</summary>
	private void AddUnitStatFormatted(StringBuilder ss, int value, string label, ref bool isFirst)
	{
		if (value != 0 || _showDefaults)
		{
			if (!isFirst) ss.Append(", ");
			ss.Append(Tr(label)).Append(':').Append(FormatInt(value));
			isFirst = false;
		}
	}

	/// <summary>Adds a UnitStats info to the table.</summary>
	private void AddUnitStatBonus(StringBuilder ss, UnitStats value, string propertyName)
	{
		bool isDefault = value.tu == 0 && value.stamina == 0 && value.health == 0 && value.strength == 0
			&& value.reactions == 0 && value.firing == 0 && value.melee == 0 && value.throwing == 0
			&& value.psiSkill == 0 && value.psiStrength == 0 && value.bravery == 0 && value.mana == 0;

		if (isDefault && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		bool isFirst = true;
		AddUnitStatFormatted(ss, value.tu, "STR_TIME_UNITS_ABBREVIATION", ref isFirst);
		AddUnitStatFormatted(ss, value.stamina, "STR_STAMINA_ABBREVIATION", ref isFirst);
		AddUnitStatFormatted(ss, value.health, "STR_HEALTH_ABBREVIATION", ref isFirst);
		AddUnitStatFormatted(ss, value.strength, "STR_STRENGTH_ABBREVIATION", ref isFirst);
		AddUnitStatFormatted(ss, value.reactions, "STR_REACTIONS_ABBREVIATION", ref isFirst);
		AddUnitStatFormatted(ss, value.firing, "STR_FIRING_ACCURACY_ABBREVIATION", ref isFirst);
		AddUnitStatFormatted(ss, value.melee, "STR_MELEE_ACCURACY_ABBREVIATION", ref isFirst);
		AddUnitStatFormatted(ss, value.throwing, "STR_THROWING_ACCURACY_ABBREVIATION", ref isFirst);
		AddUnitStatFormatted(ss, value.mana, "STR_MANA_ABBREVIATION", ref isFirst);
		AddUnitStatFormatted(ss, value.psiStrength, "STR_PSIONIC_STRENGTH_ABBREVIATION", ref isFirst);
		AddUnitStatFormatted(ss, value.psiSkill, "STR_PSIONIC_SKILL_ABBREVIATION", ref isFirst);
		AddUnitStatFormatted(ss, value.bravery, "STR_BRAVERY_ABBREVIATION", ref isFirst);
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (!isDefault)
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds armor damage modifiers to the table.</summary>
	private void AddArmorDamageModifiers(StringBuilder ss, IReadOnlyCollection<float> vec, string propertyName)
	{
		bool isDefault = true;
		foreach (var item in vec)
		{
			if (!AreSame(item, 1.0f))
			{
				isDefault = false;
			}
		}
		if (isDefault && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		int index = 0;
		bool isFirst = true;
		ss.Append('{');
		foreach (var item in vec)
		{
			if (!AreSame(item, 1.0f) || _showDefaults)
			{
				if (!isFirst)
				{
					ss.Append(", ");
				}
				switch (index)
				{
					case 0: ss.Append(Tr("STR_DAMAGE_NONE")); break;
					case 1: ss.Append(Tr("STR_DAMAGE_ARMOR_PIERCING")); break;
					case 2: ss.Append(Tr("STR_DAMAGE_INCENDIARY")); break;
					case 3: ss.Append(Tr("STR_DAMAGE_HIGH_EXPLOSIVE")); break;
					case 4: ss.Append(Tr("STR_DAMAGE_LASER_BEAM")); break;
					case 5: ss.Append(Tr("STR_DAMAGE_PLASMA_BEAM")); break;
					case 6: ss.Append(Tr("STR_DAMAGE_STUN")); break;
					case 7: ss.Append(Tr("STR_DAMAGE_MELEE")); break;
					case 8: ss.Append(Tr("STR_DAMAGE_ACID")); break;
					case 9: ss.Append(Tr("STR_DAMAGE_SMOKE")); break;
					case 10: ss.Append(Tr("STR_DAMAGE_10")); break;
					case 11: ss.Append(Tr("STR_DAMAGE_11")); break;
					case 12: ss.Append(Tr("STR_DAMAGE_12")); break;
					case 13: ss.Append(Tr("STR_DAMAGE_13")); break;
					case 14: ss.Append(Tr("STR_DAMAGE_14")); break;
					case 15: ss.Append(Tr("STR_DAMAGE_15")); break;
					case 16: ss.Append(Tr("STR_DAMAGE_16")); break;
					case 17: ss.Append(Tr("STR_DAMAGE_17")); break;
					case 18: ss.Append(Tr("STR_DAMAGE_18")); break;
					case 19: ss.Append(Tr("STR_DAMAGE_19")); break;
					default: ss.Append(Tr("STR_UNKNOWN")); break;
				}
				ss.Append(": ").Append(FormatFloat(item * 100)).Append('%');
				isFirst = false;
			}
			index++;
		}
		ss.Append('}');
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (!isDefault)
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds a SpecialAbility to the table.</summary>
	private void AddSpecialAbility(StringBuilder ss, SpecialAbility value, string propertyName, SpecialAbility defaultvalue = SpecialAbility.SPECAB_NONE)
	{
		if (value == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		switch (value)
		{
		case SpecialAbility.SPECAB_NONE: ss.Append(Tr("SPECAB_NONE")); break;
		case SpecialAbility.SPECAB_EXPLODEONDEATH: ss.Append(Tr("SPECAB_EXPLODEONDEATH")); break;
		case SpecialAbility.SPECAB_BURNFLOOR: ss.Append(Tr("SPECAB_BURNFLOOR")); break;
		case SpecialAbility.SPECAB_BURN_AND_EXPLODE: ss.Append(Tr("SPECAB_BURN_AND_EXPLODE")); break;
		default: ss.Append(Tr("STR_UNKNOWN")); break;
		}
		AddEnumId(ss, (long)value);
		AddEnumRow(ss, propertyName, value == defaultvalue);
	}

	/// <summary>Adds a MovementType to the table.</summary>
	private void AddMovementType(StringBuilder ss, MovementType value, string propertyName, MovementType defaultvalue = MovementType.MT_WALK)
	{
		if (value == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		switch (value)
		{
			case MovementType.MT_WALK: ss.Append(Tr("MT_WALK")); break;
			case MovementType.MT_FLY: ss.Append(Tr("MT_FLY")); break;
			case MovementType.MT_SLIDE: ss.Append(Tr("MT_SLIDE")); break;
			case MovementType.MT_FLOAT: ss.Append(Tr("MT_FLOAT")); break;
			case MovementType.MT_SINK: ss.Append(Tr("MT_SINK")); break;
			default: ss.Append(Tr("STR_UNKNOWN")); break;
		}
		AddEnumId(ss, (long)value);
		AddEnumRow(ss, propertyName, value == defaultvalue);
	}

	/// <summary>Adds a ForcedTorso to the table.</summary>
	private void AddForcedTorso(StringBuilder ss, ForcedTorso value, string propertyName, ForcedTorso defaultvalue = ForcedTorso.TORSO_USE_GENDER)
	{
		if (value == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		switch (value)
		{
			case ForcedTorso.TORSO_USE_GENDER: ss.Append(Tr("TORSO_USE_GENDER")); break;
			case ForcedTorso.TORSO_ALWAYS_MALE: ss.Append(Tr("TORSO_ALWAYS_MALE")); break;
			case ForcedTorso.TORSO_ALWAYS_FEMALE: ss.Append(Tr("TORSO_ALWAYS_FEMALE")); break;
			default: ss.Append(Tr("STR_UNKNOWN")); break;
		}
		AddEnumId(ss, (long)value);
		AddEnumRow(ss, propertyName, value == defaultvalue);
	}

	/// <summary>Adds a DrawingRoutine to the table.</summary>
	private void AddDrawingRoutine(StringBuilder ss, int value, string propertyName, int defaultvalue = 0)
	{
		if (value == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		switch (value)
		{
			case 0: ss.Append(Tr("DRAWING_ROUTINE_SOLDIER_SECTOID")); break;
			case 1: ss.Append(Tr("DRAWING_ROUTINE_FLOATER")); break;
			case 2: ss.Append(Tr("DRAWING_ROUTINE_HWP")); break;
			case 3: ss.Append(Tr("DRAWING_ROUTINE_CYBERDISC")); break;
			case 4: ss.Append(Tr("DRAWING_ROUTINE_CIVILIAN_ETHEREAL")); break;
			case 5: ss.Append(Tr("DRAWING_ROUTINE_SECTOPOD_REAPER")); break;
			case 6: ss.Append(Tr("DRAWING_ROUTINE_SNAKEMAN")); break;
			case 7: ss.Append(Tr("DRAWING_ROUTINE_CHRYSSALID")); break;
			case 8: ss.Append(Tr("DRAWING_ROUTINE_SILACOID")); break;
			case 9: ss.Append(Tr("DRAWING_ROUTINE_CELATID")); break;
			case 10: ss.Append(Tr("DRAWING_ROUTINE_MUTON")); break;
			case 11: ss.Append(Tr("DRAWING_ROUTINE_SWS")); break;
			case 12: ss.Append(Tr("DRAWING_ROUTINE_HALLUCINOID")); break;
			case 13: ss.Append(Tr("DRAWING_ROUTINE_AQUANAUTS")); break;
			case 14: ss.Append(Tr("DRAWING_ROUTINE_CALCINITE_AND_MORE")); break;
			case 15: ss.Append(Tr("DRAWING_ROUTINE_AQUATOID")); break;
			case 16: ss.Append(Tr("DRAWING_ROUTINE_BIO_DRONE")); break;
			case 17: ss.Append(Tr("DRAWING_ROUTINE_TFTD_CIVILIAN_A")); break;
			case 18: ss.Append(Tr("DRAWING_ROUTINE_TFTD_CIVILIAN_B")); break;
			case 19: ss.Append(Tr("DRAWING_ROUTINE_TENTACULAT")); break;
			case 20: ss.Append(Tr("DRAWING_ROUTINE_TRISCENE")); break;
			case 21: ss.Append(Tr("DRAWING_ROUTINE_XARQUID")); break;
			case 22: ss.Append(Tr("DRAWING_ROUTINE_INVERTED_CYBERDISC")); break;
			default: ss.Append(Tr("STR_UNKNOWN")); break;
		}
		AddEnumId(ss, value);
		AddEnumRow(ss, propertyName, value == defaultvalue);
	}

	/// <summary>Adds a vector of positions to the table.</summary>
	private void AddVectorOfPositions(StringBuilder ss, IReadOnlyCollection<Position> vec, string propertyName)
	{
		if (vec.Count == 0 && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		int i = 0;
		ss.Append('{');
		foreach (var item in vec)
		{
			if (i > 0)
			{
				ss.Append(", ");
			}
			ss.Append('(').Append(FormatInt(item.x)).Append(',').Append(FormatInt(item.y)).Append(',').Append(FormatInt(item.z)).Append(')');
			i++;
		}
		ss.Append('}');
		_lstRawData.AddRow(2, Trp(propertyName), ss.ToString());
		++_counter;
		if (vec.Count != 0)
		{
			_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
		}
	}

	/// <summary>Adds a build cost item to the table.</summary>
	private void AddBuildCostItem(StringBuilder ss, KeyValuePair<string, (int first, int second)> costItem)
	{
		ResetStream(ss);
		AddTranslation(ss, costItem.Key);
		ss.Append(": ").Append(Tr("STR_COST_BUILD")).Append(": ");
		ss.Append(FormatInt(costItem.Value.first));
		ss.Append(", ").Append(Tr("STR_COST_REFUND")).Append(": ");
		ss.Append(FormatInt(costItem.Value.second));
		_lstRawData.AddRow(2, "", ss.ToString());
		++_counter;
		_lstRawData.SetCellColor(_lstRawData.LastRowIndex, 1, _pink);
	}

	/// <summary>Adds a right click action type to the table.</summary>
	private void AddRightClickActionType(StringBuilder ss, int value, string propertyName, int defaultvalue = 0)
	{
		if (value == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		switch (value)
		{
		case 0: ss.Append(Tr("BFRCT_DEFAULT")); break;
		case 1: ss.Append(Tr("BFRCT_PRISON")); break;
		case 2: ss.Append(Tr("BFRCT_ENGINEERING")); break;
		case 3: ss.Append(Tr("BFRCT_RESEARCH")); break;
		case 4: ss.Append(Tr("BFRCT_GYM")); break;
		case 5: ss.Append(Tr("BFRCT_PSI_LABS")); break;
		case 6: ss.Append(Tr("BFRCT_BARRACKS")); break;
		case 7: ss.Append(Tr("BFRCT_GREY_MARKET")); break;
		default: ss.Append(Tr("BFRCT_GEOSCAPE")); break;
		}
		AddEnumId(ss, value);
		AddEnumRow(ss, propertyName, value == defaultvalue);
	}

	/// <summary>Adds a hunt mode to the table.</summary>
	private void AddHuntMode(StringBuilder ss, int value, string propertyName, int defaultvalue = 0)
	{
		if (value == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		switch (value)
		{
		case 0: ss.Append(Tr("HM_INTERCEPTORS")); break;
		case 1: ss.Append(Tr("HM_TRANSPORTS")); break;
		case 2: ss.Append(Tr("HM_RANDOM")); break;
		default: ss.Append(Tr("STR_UNKNOWN")); break;
		}
		AddEnumId(ss, value);
		AddEnumRow(ss, propertyName, value == defaultvalue);
	}

	/// <summary>Adds a hunt behavior to the table.</summary>
	private void AddHuntBehavior(StringBuilder ss, int value, string propertyName, int defaultvalue = 0)
	{
		if (value == defaultvalue && !_showDefaults)
		{
			return;
		}
		ResetStream(ss);
		switch (value)
		{
		case 0: ss.Append(Tr("HB_FLEE")); break;
		case 1: ss.Append(Tr("HB_KAMIKAZE")); break;
		case 2: ss.Append(Tr("HB_RANDOM")); break;
		default: ss.Append(Tr("STR_UNKNOWN")); break;
		}
		AddEnumId(ss, value);
		AddEnumRow(ss, propertyName, value == defaultvalue);
	}
}
#pragma warning restore CA1859
#pragma warning restore CA1822
