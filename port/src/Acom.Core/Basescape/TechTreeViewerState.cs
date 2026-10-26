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
// Ported from: src/Basescape/TechTreeViewerState.h, src/Basescape/TechTreeViewerState.cpp
#nullable disable

using System.Globalization;
using System.Text;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

public enum TTVMode : uint
{
	TTV_NONE = 0,
	TTV_RESEARCH = 1,
	TTV_MANUFACTURING = 2,
	TTV_FACILITIES = 3,
	TTV_ITEMS = 4,
	TTV_CRAFTS = 5,
}

/// <summary>
/// TechTreeViewer screen, where you can browse the Tech Tree.
/// </summary>
/// <remarks>
/// Deviation: the arc/event/mission script lists ("unlocks arcs" etc.) are std::unordered_set in C++ and are listed
/// in hash order there; here they keep the order in which the scripts are found (rule list order).
/// </remarks>
public class TechTreeViewerState : State
{
	private readonly TextButton _btnOk, _btnNew;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtSelectedTopic, _txtProgress, _txtCostIndicator;
	private readonly TextList _lstLeft, _lstRight, _lstFull;
	private readonly byte _purple, _pink, _blue, _white, _gold, _grey;
	private string _selectedTopic = "";
	private TTVMode _selectedFlag;
	private readonly List<(string first, TTVMode second)> _history = [];
	private readonly List<string> _leftTopics = [], _rightTopics = [];
	private readonly List<TTVMode> _leftFlags = [], _rightFlags = [];
	private readonly HashSet<string> _disabledResearch = new(StringComparer.Ordinal);
	private readonly HashSet<string> _alreadyAvailableResearch = new(StringComparer.Ordinal), _alreadyAvailableManufacture = new(StringComparer.Ordinal), _alreadyAvailableFacilities = new(StringComparer.Ordinal), _alreadyAvailableCrafts = new(StringComparer.Ordinal);
	private readonly HashSet<string> _protectedItems = new(StringComparer.Ordinal), _alreadyAvailableItems = new(StringComparer.Ordinal);

	/// <summary>
	/// Initializes all the elements on the UI.
	/// </summary>
	public TechTreeViewerState(RuleResearch r = null, RuleManufacture m = null, RuleBaseFacility f = null, RuleCraft c = null)
	{
		if (r != null)
		{
			_selectedTopic = r.Name;
			_selectedFlag = TTVMode.TTV_RESEARCH;
		}
		else if (m != null)
		{
			_selectedTopic = m.Name;
			_selectedFlag = TTVMode.TTV_MANUFACTURING;
		}
		else if (f != null)
		{
			_selectedTopic = f.Type;
			_selectedFlag = TTVMode.TTV_FACILITIES;
		}
		else if (c != null)
		{
			_selectedTopic = c.Type;
			_selectedFlag = TTVMode.TTV_CRAFTS;
		}

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_txtTitle = new Text(304, 17, 8, 7);
		_txtSelectedTopic = new Text(204, 9, 8, 24);
		_txtProgress = new Text(100, 9, 212, 24);
		_txtCostIndicator = new Text(100, 9, 16, 32); // experimental cost indicator
		_lstLeft = new TextList(132, 128, 8, 40);
		_lstRight = new TextList(132, 128, 164, 40);
		_lstFull = new TextList(288, 128, 8, 40);
		_btnNew = new TextButton(148, 16, 8, 176);
		_btnOk = new TextButton(148, 16, 164, 176);

		// Set palette
		SetInterface("techTreeViewer");

		var ruleInterface = _game.Mod.GetInterface("techTreeViewer");
		_purple = unchecked((byte)ruleInterface.GetElement("list").color);
		_pink = unchecked((byte)ruleInterface.GetElement("list").color2);
		_blue = unchecked((byte)ruleInterface.GetElement("list").border);
		_white = unchecked((byte)ruleInterface.GetElement("listExtended").color);
		_gold = unchecked((byte)ruleInterface.GetElement("listExtended").color2);
		_grey = unchecked((byte)ruleInterface.GetElement("listExtended").border);

		Add(_window, "window", "techTreeViewer");
		Add(_txtTitle, "text", "techTreeViewer");
		Add(_txtSelectedTopic, "text", "techTreeViewer");
		Add(_txtProgress, "text", "techTreeViewer");
		Add(_txtCostIndicator, "text", "techTreeViewer");
		Add(_lstLeft, "list", "techTreeViewer");
		Add(_lstRight, "list", "techTreeViewer");
		Add(_lstFull, "list", "techTreeViewer");
		Add(_btnNew, "button", "techTreeViewer");
		Add(_btnOk, "button", "techTreeViewer");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "techTreeViewer");

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_TECH_TREE_VIEWER"));

		_txtSelectedTopic.SetText(Tr("STR_TOPIC").Arg(""));

		_lstLeft.SetColumns(1, 132);
		_lstLeft.SetSelectable(true);
		_lstLeft.SetBackground(_window);
		_lstLeft.SetWordWrap(true);
		_lstLeft.OnMouseClick(OnSelectLeftTopic);

		_lstRight.SetColumns(1, 132);
		_lstRight.SetSelectable(true);
		_lstRight.SetBackground(_window);
		_lstRight.SetWordWrap(true);
		_lstRight.OnMouseClick(OnSelectRightTopic);

		_lstFull.SetColumns(1, 288);
		_lstFull.SetSelectable(true);
		_lstFull.SetBackground(_window);
		_lstFull.SetWordWrap(true);
		_lstFull.OnMouseClick(OnSelectFullTopic);

		_btnNew.Text = Tr("STR_SELECT_TOPIC");
		_btnNew.OnMouseClick(BtnNewClick);
		_btnNew.OnKeyboardPress(BtnNewClick, Options.keyToggleQuickSearch);

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnOk.OnKeyboardPress(BtnBackClick, SDLKey.SDLK_BACKSPACE);

		if (Options.oxceDisableTechTreeViewer)
		{
			_txtTitle.SetHeight(_txtTitle.Height * 9);
			_txtTitle.SetWordWrap(true);
			_txtTitle.SetText(Tr("STR_THIS_FEATURE_IS_DISABLED_1"));
			_txtSelectedTopic.Visible = false;
			_txtProgress.Visible = false;
			_txtCostIndicator.Visible = false;
			_lstLeft.Visible = false;
			_lstRight.Visible = false;
			_lstFull.Visible = false;
			_btnNew.Visible = false;
			return;
		}

		int discoveredSum = 0;
		// pre-calculate globally
		foreach (var discoveredResearchRule in _game.SavedGame.DiscoveredResearch)
		{
			_alreadyAvailableResearch.Add(discoveredResearchRule.Name);
			discoveredSum += discoveredResearchRule.Cost;
		}
		foreach (var info in _game.SavedGame.ResearchRuleStatusRaw)
		{
			if (info.Value == RuleResearch.RESEARCH_STATUS_DISABLED)
			{
				var rr = _game.Mod.GetResearch(info.Key, false);
				if (rr != null)
				{
					_disabledResearch.Add(rr.Name);
				}
			}
		}

		int totalSum = 0;
		foreach (var resType in _game.Mod.ResearchList)
		{
			RuleResearch resRule = _game.Mod.GetResearch(resType);
			if (resRule != null)
			{
				totalSum += resRule.Cost;
			}
		}

		foreach (var manuf in _game.Mod.ManufactureList)
		{
			RuleManufacture manRule = _game.Mod.GetManufacture(manuf);
			if (_game.SavedGame.IsResearched(manRule.Requirements))
			{
				_alreadyAvailableManufacture.Add(manRule.Name);
			}
		}

		foreach (var facType in _game.Mod.BaseFacilitiesList)
		{
			RuleBaseFacility facRule = _game.Mod.GetBaseFacility(facType);
			if (_game.SavedGame.IsResearched(facRule.Requirements))
			{
				_alreadyAvailableFacilities.Add(facRule.Type);
			}
		}

		foreach (var itemType in _game.Mod.ItemsList)
		{
			RuleItem itemRule = _game.Mod.GetItem(itemType);
			if (itemRule.Requirements.Count != 0 || itemRule.BuyRequirements.Count != 0)
			{
				_protectedItems.Add(itemRule.Type);
				if (_game.SavedGame.IsResearched(itemRule.Requirements) && _game.SavedGame.IsResearched(itemRule.BuyRequirements))
				{
					_alreadyAvailableItems.Add(itemRule.Type);
				}
			}
		}

		foreach (var craftType in _game.Mod.CraftsList)
		{
			RuleCraft craftRule = _game.Mod.GetCraft(craftType);
			if (_game.SavedGame.IsResearched(craftRule.Requirements))
			{
				_alreadyAvailableCrafts.Add(craftRule.Type);
			}
		}

		_txtProgress.Align = TextHAlign.ALIGN_RIGHT;
		_txtProgress.SetText(Tr("STR_RESEARCH_PROGRESS").Arg(discoveredSum * 100 / totalSum));
	}

	/// <summary>
	/// Initializes the screen (fills the lists).
	/// </summary>
	public override void Init()
	{
		base.Init();

		if (!Options.oxceDisableTechTreeViewer)
		{
			InitLists();
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
	/// Navigates to the previous topic from the browsing history.
	/// </summary>
	public void BtnBackClick(Action action)
	{
		if (_history.Count != 0)
		{
			_selectedFlag = _history[^1].second;
			_selectedTopic = _history[^1].first;
			_history.RemoveAt(_history.Count - 1);
			InitLists();
		}
	}

	/// <summary>
	/// Opens the Select Topic screen.
	/// </summary>
	public void BtnNewClick(Action action)
	{
		_game.PushState(new TechTreeSelectState(this));
	}

	/// <summary>
	/// Builds the experimental cost indicator ("#" = 100, "=" = 20, "-" = 5, "." = 1 for costs below 10).
	/// </summary>
	internal static string GetCostIndicator(int ruleCost)
	{
		StringBuilder ss = new StringBuilder();
		int cost = ruleCost;
		(int first, string second)[] symbol_values = [(100, "#"), (20, "="), (5, "-")];

		foreach (var sym in symbol_values)
		{
			while (cost >= sym.first)
			{
				cost -= sym.first;
				ss.Append(sym.second);
			}
		}
		if (ruleCost < 10)
		{
			while (cost >= 1)
			{
				cost -= 1;
				ss.Append('.');
			}
		}
		return ss.ToString();
	}

	/// <summary>
	/// Adds a row to a list together with its topic and flag (the C++ code repeats this inline).
	/// </summary>
	/// <param name="list">The list to add the row to.</param>
	/// <param name="topics">Topics of the list rows.</param>
	/// <param name="flags">Flags of the list rows.</param>
	/// <param name="row">Current row index (incremented).</param>
	/// <param name="text">Row text.</param>
	/// <param name="color">Row color, or null to keep the default color.</param>
	/// <param name="topic">Topic of the row.</param>
	/// <param name="flag">Topic type of the row.</param>
	private static void AddRow(TextList list, List<string> topics, List<TTVMode> flags, ref int row, string text, byte? color, string topic = "-", TTVMode flag = TTVMode.TTV_NONE)
	{
		list.AddRow(1, text);
		if (color.HasValue)
		{
			list.SetRowColor(row, color.Value);
		}
		topics.Add(topic);
		flags.Add(flag);
		++row;
	}

	private void AddLeft(ref int row, string text, byte? color, string topic = "-", TTVMode flag = TTVMode.TTV_NONE)
		=> AddRow(_lstLeft, _leftTopics, _leftFlags, ref row, text, color, topic, flag);

	private void AddRight(ref int row, string text, byte? color, string topic = "-", TTVMode flag = TTVMode.TTV_NONE)
		=> AddRow(_lstRight, _rightTopics, _rightFlags, ref row, text, color, topic, flag);

	/// <summary>The item list (_lstFull) uses the left topics/flags as in C++.</summary>
	private void AddFull(ref int row, string text, byte? color, string topic = "-", TTVMode flag = TTVMode.TTV_NONE)
		=> AddRow(_lstFull, _leftTopics, _leftFlags, ref row, text, color, topic, flag);

	/// <summary>Adds a list of research topics as indented rows.</summary>
	private void AddLeftResearch(ref int row, IEnumerable<string> topics)
	{
		foreach (var res in topics)
		{
			AddLeft(ref row, "  " + Tr(res), GetResearchColor(res), res, TTVMode.TTV_RESEARCH);
		}
	}

	/// <summary>Adds a list of research topics as indented rows.</summary>
	private void AddRightResearch(ref int row, IEnumerable<string> topics)
	{
		foreach (var res in topics)
		{
			AddRight(ref row, "  " + Tr(res), GetResearchColor(res), res, TTVMode.TTV_RESEARCH);
		}
	}

	/// <summary>Adds a header and the base function names as indented rows.</summary>
	private void AddServices(bool left, ref int row, string header, RuleBaseFacilityFunctions funcs, byte color)
	{
		if (left)
		{
			AddLeft(ref row, Tr(header), _blue);
			foreach (var serviceType in _game.Mod.GetBaseFunctionNames(funcs))
			{
				AddLeft(ref row, "  " + Tr(serviceType), color);
			}
		}
		else
		{
			AddRight(ref row, Tr(header), _blue);
			foreach (var serviceType in _game.Mod.GetBaseFunctionNames(funcs))
			{
				AddRight(ref row, "  " + Tr(serviceType), color);
			}
		}
	}

	/// <summary>"  name: amount".</summary>
	private string NameAndAmount(string type, int amount) => "  " + Tr(type) + ": " + amount.ToString(CultureInfo.InvariantCulture);

	/// <summary>"  hours (Xd/Yh)" for the engineer hours.</summary>
	private string FormatManufactureTime(int manufactureTime)
	{
		int days = manufactureTime / 24;
		int hours = manufactureTime % 24;
		StringBuilder txt = new StringBuilder();
		txt.Append("  ");
		txt.Append(manufactureTime.ToString(CultureInfo.InvariantCulture));
		txt.Append(" (");
		if (days > 0)
		{
			txt.Append((string)Tr("STR_DAY_SHORT").Arg(days));
		}
		if (hours > 0)
		{
			if (days > 0)
			{
				txt.Append('/');
			}
			txt.Append((string)Tr("STR_HOUR_SHORT").Arg(hours));
		}
		txt.Append(')');
		return txt.ToString();
	}

	/// <summary>
	/// Shows the filtered topics.
	/// </summary>
	private void InitLists()
	{
		// Set topic name
		{
			string ss = Tr(_selectedTopic);
			if (_selectedFlag == TTVMode.TTV_MANUFACTURING)
			{
				ss += Tr("STR_M_FLAG");
			}
			else if (_selectedFlag == TTVMode.TTV_FACILITIES)
			{
				ss += Tr("STR_F_FLAG");
			}
			else if (_selectedFlag == TTVMode.TTV_ITEMS)
			{
				ss += Tr("STR_I_FLAG");
			}
			else if (_selectedFlag == TTVMode.TTV_CRAFTS)
			{
				ss += Tr("STR_C_FLAG");
			}
			_txtSelectedTopic.SetText(Tr("STR_TOPIC").Arg(ss));
			_txtCostIndicator.SetText("");
		}

		// reset
		_leftTopics.Clear();
		_rightTopics.Clear();
		_leftFlags.Clear();
		_rightFlags.Clear();
		_lstLeft.ClearList();
		_lstRight.ClearList();
		_lstFull.ClearList();
		_lstLeft.Visible = true;
		_lstRight.Visible = true;
		_lstFull.Visible = false;

		if (_selectedFlag == TTVMode.TTV_NONE)
		{
			return;
		}
		else if (_selectedFlag == TTVMode.TTV_RESEARCH)
		{
			InitResearchLists();
		}
		else if (_selectedFlag == TTVMode.TTV_MANUFACTURING)
		{
			InitManufactureLists();
		}
		else if (_selectedFlag == TTVMode.TTV_FACILITIES)
		{
			InitFacilityLists();
		}
		else if (_selectedFlag == TTVMode.TTV_ITEMS)
		{
			InitItemLists();
		}
		else if (_selectedFlag == TTVMode.TTV_CRAFTS)
		{
			InitCraftLists();
		}
	}

	/// <summary>The TTV_RESEARCH branch of initLists.</summary>
	private void InitResearchLists()
	{
		int row = 0;
		RuleResearch rule = _game.Mod.GetResearch(_selectedTopic);
		if (rule == null)
			return;

		// Cost indicator
		_txtCostIndicator.SetText(GetCostIndicator(rule.Cost));

		List<string> researchList = _game.Mod.ResearchList;
		List<string> manufactureList = _game.Mod.ManufactureList;

		// 0. common pre-calculation
		List<RuleResearch> reqs = rule.Requirements;
		List<RuleResearch> deps = rule.Dependencies;
		List<string> unlockedBy = [];
		List<string> disabledBy = [];
		List<string> reenabledBy = [];
		List<string> getForFreeFrom = [];
		List<string> lookupOf = [];
		List<string> requiredByResearch = [];
		List<string> requiredByManufacture = [];
		List<string> requiredByFacilities = [];
		List<string> requiredByItems = [];
		List<string> requiredByTransformations = [];
		List<string> requiredByCrafts = [];
		List<string> leadsTo = [];
		List<RuleResearch> unlocks = rule.Unlocked;
		List<RuleResearch> disables = rule.Disabled;
		List<RuleResearch> reenables = rule.Reenabled;
		List<RuleResearch> free = rule.GetOneFree;
		var freeProtected = rule.GetOneFreeProtected;

		foreach (var j in manufactureList)
		{
			RuleManufacture temp = _game.Mod.GetManufacture(j);
			foreach (var i in temp.Requirements)
			{
				if (i == rule)
				{
					requiredByManufacture.Add(j);
				}
			}
		}

		foreach (var f in _game.Mod.BaseFacilitiesList)
		{
			RuleBaseFacility temp = _game.Mod.GetBaseFacility(f);
			foreach (var i in temp.Requirements)
			{
				if (i == rule.Name)
				{
					requiredByFacilities.Add(f);
				}
			}
		}

		foreach (var item in _game.Mod.ItemsList)
		{
			RuleItem temp = _game.Mod.GetItem(item);
			foreach (var i in temp.Requirements)
			{
				if (i == rule)
				{
					requiredByItems.Add(item);
				}
			}
			foreach (var i in temp.BuyRequirements)
			{
				if (i == rule)
				{
					requiredByItems.Add(item);
				}
			}
		}

		foreach (var transf in _game.Mod.SoldierTransformationList)
		{
			RuleSoldierTransformation temp = _game.Mod.GetSoldierTransformation(transf);
			foreach (var i in temp.RequiredResearch)
			{
				if (i == rule.Name)
				{
					requiredByTransformations.Add(transf);
				}
			}
		}

		foreach (var c in _game.Mod.CraftsList)
		{
			RuleCraft temp = _game.Mod.GetCraft(c);
			foreach (var i in temp.Requirements)
			{
				if (i == rule.Name)
				{
					requiredByCrafts.Add(c);
				}
			}
		}

		foreach (var j in researchList)
		{
			RuleResearch temp = _game.Mod.GetResearch(j);
			foreach (var i in temp.Unlocked)
			{
				if (i == rule)
				{
					unlockedBy.Add(j);
				}
			}
			foreach (var i in temp.Disabled)
			{
				if (i == rule)
				{
					disabledBy.Add(j);
				}
			}
			foreach (var i in temp.Reenabled)
			{
				if (i == rule)
				{
					reenabledBy.Add(j);
				}
			}
			foreach (var i in temp.GetOneFree)
			{
				if (i == rule)
				{
					getForFreeFrom.Add(j);
				}
			}
			foreach (var itMap in temp.GetOneFreeProtected)
			{
				foreach (var i in itMap.second)
				{
					if (i == rule)
					{
						getForFreeFrom.Add(j);
					}
				}
			}
			if (temp.Lookup != null)
			{
				if (temp.Lookup == rule)
				{
					lookupOf.Add(j);
				}
			}
			foreach (var i in temp.Requirements)
			{
				if (i == rule)
				{
					requiredByResearch.Add(j);
				}
			}
			foreach (var i in temp.Dependencies)
			{
				if (i == rule)
				{
					leadsTo.Add(j);
				}
			}
		}

		// 1. item required
		if (rule.NeedItem())
		{
			if (rule.DestroyItem())
			{
				AddLeft(ref row, Tr("STR_ITEM_DESTROYED"), _blue);
			}
			else
			{
				AddLeft(ref row, Tr("STR_ITEM_REQUIRED"), _blue);
			}
			if (rule.NeededItem != null)
			{
				string itemName = "  " + Tr(rule.NeededItem.Type);
				AddLeft(ref row, itemName, GetResearchColor(rule.NeededItem.Type), rule.NeededItem.Type, TTVMode.TTV_ITEMS);
			}
			else
			{
				AddLeft(ref row, "  -", _white);
			}
		}

		// 1b. requires services (from base facilities)
		if (rule.RequireBaseFunc.Any())
		{
			AddServices(true, ref row, "STR_SERVICES_REQUIRED", rule.RequireBaseFunc, _gold);
		}

		// 2. requires
		if (reqs.Count > 0)
		{
			AddLeft(ref row, Tr("STR_REQUIRES"), _blue);
			AddLeftResearch(ref row, reqs.Select(i => i.Name));
		}

		// 3. depends on
		if (deps.Count > 0)
		{
			AddLeft(ref row, Tr("STR_DEPENDS_ON"), _blue);
			foreach (var i in deps)
			{
				if (unlockedBy.Contains(i.Name))
				{
					// if the same item is also in the "Unlocked by" section, skip it
					continue;
				}
				AddLeft(ref row, "  " + Tr(i.Name), GetResearchColor(i.Name), i.Name, TTVMode.TTV_RESEARCH);
			}
		}

		// 4a. unlocked by
		if (unlockedBy.Count > 0)
		{
			AddLeft(ref row, Tr("STR_UNLOCKED_BY"), _blue);
			AddLeftResearch(ref row, unlockedBy);
		}

		// 4b. disabled by
		if (disabledBy.Count > 0)
		{
			AddLeft(ref row, Tr("STR_DISABLED_BY"), _blue);
			AddLeftResearch(ref row, disabledBy);
		}

		// 4c. reenabled by
		if (reenabledBy.Count > 0)
		{
			AddLeft(ref row, Tr("STR_REENABLED_BY"), _blue);
			AddLeftResearch(ref row, reenabledBy);
		}

		// 5. get for free from
		if (getForFreeFrom.Count > 0)
		{
			AddLeft(ref row, Tr("STR_GET_FOR_FREE_FROM"), _blue);
			AddLeftResearch(ref row, getForFreeFrom);
		}

		// is lookup of
		if (lookupOf.Count > 0)
		{
			AddLeft(ref row, Tr("STR_IS_LOOKUP_OF"), _blue);
			AddLeftResearch(ref row, lookupOf);
		}

		row = 0;

		// lookup link
		if (rule.Lookup != null)
		{
			AddRight(ref row, Tr("STR_LOOKUP"), _blue);
			AddRight(ref row, "  " + Tr(rule.Lookup.Name), GetResearchColor(rule.Lookup.Name), rule.Lookup.Name, TTVMode.TTV_RESEARCH);
		}

		// spawned item
		if (!Mod.IsEmptyRuleName(rule.SpawnedItem) || rule.SpawnedItemList.Count != 0)
		{
			AddRight(ref row, Tr("STR_SPAWNED_ITEMS"), _blue);
		}
		if (!Mod.IsEmptyRuleName(rule.SpawnedItem))
		{
			string name = "  " + Tr(rule.SpawnedItem);
			if (rule.SpawnedItemCount > 1)
			{
				name += " x";
				name += rule.SpawnedItemCount.ToString(CultureInfo.InvariantCulture);
			}
			AddRight(ref row, name, _white, rule.SpawnedItem, TTVMode.TTV_ITEMS);
		}
		foreach (var sil in rule.SpawnedItemList)
		{
			AddRight(ref row, "  " + Tr(sil), _white, sil, TTVMode.TTV_ITEMS);
		}

		// spawned event
		if (!Mod.IsEmptyRuleName(rule.SpawnedEvent))
		{
			AddRight(ref row, Tr("STR_SPAWNED_EVENT"), _blue);
			AddRight(ref row, "  " + Tr(rule.SpawnedEvent), _white);
		}

		// 6. required by
		if (requiredByResearch.Count > 0 || requiredByManufacture.Count > 0 || requiredByFacilities.Count > 0 || requiredByItems.Count > 0 || requiredByCrafts.Count > 0)
		{
			AddRight(ref row, Tr("STR_REQUIRED_BY"), _blue);
		}

		// 6a. required by research
		AddRightResearch(ref row, requiredByResearch);

		// 6b. required by manufacture
		foreach (var manuf in requiredByManufacture)
		{
			AddRight(ref row, "  " + Tr(manuf) + Tr("STR_M_FLAG"), !IsDiscoveredManufacture(manuf) ? _pink : null, manuf, TTVMode.TTV_MANUFACTURING);
		}

		// 6c. required by facilities
		foreach (var facType in requiredByFacilities)
		{
			AddRight(ref row, "  " + Tr(facType) + Tr("STR_F_FLAG"), !IsDiscoveredFacility(facType) ? _pink : null, facType, TTVMode.TTV_FACILITIES);
		}

		// 6d. required by items
		foreach (var itemType in requiredByItems)
		{
			AddRight(ref row, "  " + Tr(itemType) + Tr("STR_I_FLAG"), !IsProtectedAndDiscoveredItem(itemType) ? _pink : null, itemType, TTVMode.TTV_ITEMS);
		}

		// 6e. required by crafts
		foreach (var craftType in requiredByCrafts)
		{
			AddRight(ref row, "  " + Tr(craftType) + Tr("STR_C_FLAG"), !IsDiscoveredCraft(craftType) ? _pink : null, craftType, TTVMode.TTV_CRAFTS);
		}

		// 6f. required by transformations
		if (requiredByTransformations.Count > 0)
		{
			AddRight(ref row, Tr("STR_REQUIRED_BY_TRANSFORMATIONS"), _blue);
			foreach (var transformationType in requiredByTransformations)
			{
				AddRight(ref row, "  " + Tr(transformationType), _white);
			}
		}

		// 7. leads to
		if (leadsTo.Count > 0)
		{
			AddRight(ref row, Tr("STR_LEADS_TO"), _blue);
			foreach (var res in leadsTo)
			{
				RuleResearch iTemp = _game.Mod.GetResearch(res);
				if (unlocks.Contains(iTemp))
				{
					// if the same topic is also in the "Unlocks" section, skip it
					continue;
				}
				AddRight(ref row, "  " + Tr(res), GetResearchColor(res), res, TTVMode.TTV_RESEARCH);
			}
		}

		// 8a. unlocks
		if (unlocks.Count > 0)
		{
			AddRight(ref row, Tr("STR_UNLOCKS"), _blue);
			AddRightResearch(ref row, unlocks.Select(i => i.Name));
		}

		// 8b. disables
		if (disables.Count > 0)
		{
			AddRight(ref row, Tr("STR_DISABLES"), _blue);
			AddRightResearch(ref row, disables.Select(i => i.Name));
		}

		// 8c. reenables
		if (reenables.Count > 0)
		{
			AddRight(ref row, Tr("STR_REENABLES"), _blue);
			AddRightResearch(ref row, reenables.Select(i => i.Name));
		}

		// 9. gives one for free
		if (free.Count > 0 || freeProtected.Count > 0)
		{
			int remaining = 0;
			int total = 0;
			foreach (var i in free)
			{
				if (GetResearchColor(i.Name) == _pink)
				{
					++remaining;
				}
				++total;
			}
			foreach (var itMap in freeProtected)
			{
				foreach (var i in itMap.second)
				{
					if (GetResearchColor(i.Name) == _pink)
					{
						++remaining;
					}
					++total;
				}
			}
			string ssFree;
			if (rule.SequentialGetOneFree())
			{
				ssFree = Tr("STR_GIVES_ONE_FOR_FREE_SEQ");
			}
			else
			{
				ssFree = Tr("STR_GIVES_ONE_FOR_FREE");
			}
			ssFree += " " + remaining.ToString(CultureInfo.InvariantCulture) + "/" + total.ToString(CultureInfo.InvariantCulture);
			AddRight(ref row, ssFree, _blue);
			AddRightResearch(ref row, free.Select(i => i.Name));
			foreach (var itMap in freeProtected)
			{
				string name2 = " " + Tr(itMap.first.Name) + ":";
				AddRight(ref row, name2, GetAltResearchColor(itMap.first.Name), itMap.first.Name, TTVMode.TTV_RESEARCH);
				AddRightResearch(ref row, itMap.second.Select(i => i.Name));
			}
		}

		// 10. unlocks/disables alien missions, game arcs or geoscape events
		HashSet<string> unlocksArcs = new(StringComparer.Ordinal), disablesArcs = new(StringComparer.Ordinal);
		HashSet<string> unlocksEvents = new(StringComparer.Ordinal), disablesEvents = new(StringComparer.Ordinal);
		HashSet<string> unlocksMissions = new(StringComparer.Ordinal), disablesMissions = new(StringComparer.Ordinal);
		bool affectsGameProgression = false;

		foreach (var arcScriptId in _game.Mod.ArcScriptList)
		{
			var arcScript = _game.Mod.GetArcScript(arcScriptId, false);
			if (arcScript != null)
			{
				foreach (var trigger in arcScript.ResearchTriggers)
				{
					if (trigger.Key == _selectedTopic)
					{
						if (trigger.Value)
							unlocksArcs.Add(arcScriptId);
						else
							disablesArcs.Add(arcScriptId);
					}
				}
			}
		}
		foreach (var eventScriptId in _game.Mod.EventScriptList)
		{
			var eventScript = _game.Mod.GetEventScript(eventScriptId, false);
			if (eventScript != null)
			{
				foreach (var trigger in eventScript.ResearchTriggers)
				{
					if (trigger.Key == _selectedTopic)
					{
						if (eventScript.AffectsGameProgression) affectsGameProgression = true; // remember for later
						if (trigger.Value)
							unlocksEvents.Add(eventScriptId);
						else
							disablesEvents.Add(eventScriptId);
					}
				}
			}
		}
		foreach (var missionScriptId in _game.Mod.MissionScriptList)
		{
			var missionScript = _game.Mod.GetMissionScript(missionScriptId, false);
			if (missionScript != null)
			{
				foreach (var trigger in missionScript.ResearchTriggers)
				{
					if (trigger.Key == _selectedTopic)
					{
						if (trigger.Value)
							unlocksMissions.Add(missionScriptId);
						else
							disablesMissions.Add(missionScriptId);
					}
				}
			}
		}
		bool showDetails = Options.oxceAlwaysTechTreeDetails || Options.IsPasswordCorrect();
		if (showDetails)
		{
			void addGameProgressionEntry(HashSet<string> list, string label, ref int row)
			{
				if (list.Count != 0)
				{
					AddRight(ref row, Tr(label), _blue);
					foreach (var i in list)
					{
						AddRight(ref row, "  " + Tr(i), _white);
					}
				}
			}

			addGameProgressionEntry(unlocksArcs, "STR_UNLOCKS_ARCS", ref row);
			addGameProgressionEntry(disablesArcs, "STR_DISABLES_ARCS", ref row);

			addGameProgressionEntry(unlocksMissions, "STR_UNLOCKS_MISSIONS", ref row);
			addGameProgressionEntry(disablesMissions, "STR_DISABLES_MISSIONS", ref row);

			addGameProgressionEntry(unlocksEvents, "STR_UNLOCKS_EVENTS", ref row);
			addGameProgressionEntry(disablesEvents, "STR_DISABLES_EVENTS", ref row);
		}
		else
		{
			int showDisclaimer = 0;
			if (unlocksMissions.Count != 0 || disablesMissions.Count != 0 || unlocksArcs.Count != 0 || disablesArcs.Count != 0)
			{
				showDisclaimer = 1; // STR_AFFECTS_GAME_PROGRESSION
			}
			else if (unlocksEvents.Count != 0 || disablesEvents.Count != 0)
			{
				if (affectsGameProgression)
				{
					showDisclaimer = 1; // STR_AFFECTS_GAME_PROGRESSION
				}
				else if (Options.IsPasswordCorrect())
				{
					showDisclaimer = 2; // .
				}
			}
			if (showDisclaimer > 0)
			{
				AddRight(ref row, Tr("STR_AFFECTS_GAME_PROGRESSION"), showDisclaimer == 1 ? _gold : _white);
			}
		}
	}

	/// <summary>The TTV_MANUFACTURING branch of initLists.</summary>
	private void InitManufactureLists()
	{
		int row = 0;
		RuleManufacture rule = _game.Mod.GetManufacture(_selectedTopic);
		if (rule == null)
			return;

		// 1. requires
		List<RuleResearch> reqs = rule.Requirements;
		if (reqs.Count > 0)
		{
			AddLeft(ref row, Tr("STR_RESEARCH_REQUIRED"), _blue);
			AddLeftResearch(ref row, reqs.Select(i => i.Name));
		}

		// 2. requires services (from base facilities)
		if (rule.RequireBaseFunc.Any())
		{
			AddServices(true, ref row, "STR_SERVICES_REQUIRED", rule.RequireBaseFunc, _gold);
		}

		// 3. inputs
		var craftInputs = rule.RequiredCrafts;
		var inputs = rule.RequiredItems;
		if (inputs.Count > 0 || craftInputs.Count > 0)
		{
			AddLeft(ref row, Tr("STR_MATERIALS_REQUIRED"), _blue);
			foreach (var i in craftInputs)
			{
				AddLeft(ref row, NameAndAmount(i.Key.Type, i.Value), _white);
			}
			foreach (var i in inputs)
			{
				AddLeft(ref row, NameAndAmount(i.Key.Type, i.Value), _white, i.Key.Type, TTVMode.TTV_ITEMS);
			}
		}

		// empty line
		AddLeft(ref row, "", null);

		// cost per unit
		if (rule.ManufactureCost > 0)
		{
			AddLeft(ref row, Tr("STR_TTV_COST_PER_UNIT"), _blue);
			AddLeft(ref row, "  " + Unicode.FormatFunding(rule.ManufactureCost), _white);
		}
		// engineer hours
		if (rule.ManufactureTime > 0)
		{
			AddLeft(ref row, Tr("STR_TTV_ENGINEER_HOURS"), _blue);
			AddLeft(ref row, FormatManufactureTime(rule.ManufactureTime), _white);
		}
		// work space required
		if (rule.RequiredSpace > 0)
		{
			AddLeft(ref row, Tr("STR_TTV_WORK_SPACE_REQUIRED"), _blue);
			AddLeft(ref row, "  " + rule.RequiredSpace.ToString(CultureInfo.InvariantCulture), _white);
		}

		row = 0;

		// 4. outputs
		var outputs = rule.ProducedItems;
		if (outputs.Count > 0 || rule.ProducedCraft != null)
		{
			AddRight(ref row, Tr("STR_ITEMS_PRODUCED"), _blue);
			if (rule.ProducedCraft != null)
			{
				AddRight(ref row, "  " + Tr(rule.ProducedCraft.Type) + ": 1", _white);
			}
			foreach (var i in outputs)
			{
				AddRight(ref row, NameAndAmount(i.Key.Type, i.Value), _white, i.Key.Type, TTVMode.TTV_ITEMS);
			}
		}

		// 4b. random outputs
		var randomOutputs = rule.RandomProducedItems;
		if (randomOutputs.Count > 0)
		{
			AddRight(ref row, Tr("STR_RANDOM_PRODUCTION_DISCLAIMER"), _blue);
			int total = 0;
			foreach (var n in randomOutputs)
			{
				total += n.first;
			}
			foreach (var randomOutput in randomOutputs)
			{
				string chance = " " + (randomOutput.first * 100 / total).ToString(CultureInfo.InvariantCulture) + "%";
				AddRight(ref row, chance, _gold);
				foreach (var i in randomOutput.second)
				{
					AddRight(ref row, NameAndAmount(i.Key.Type, i.Value), _white, i.Key.Type, TTVMode.TTV_ITEMS);
				}
			}
		}

		// 5. person joining
		if (rule.SpawnedPersonType != "")
		{
			AddRight(ref row, Tr("STR_PERSON_RECRUITED"), _blue);

			// person joining
			string name = "  ";
			if (!string.IsNullOrEmpty(rule.SpawnedSoldierTemplate))
			{
				name += "*";
			}
			name += Tr(rule.SpawnedPersonName != "" ? rule.SpawnedPersonName : rule.SpawnedPersonType);
			AddRight(ref row, name, _white);
		}
	}

	/// <summary>The TTV_FACILITIES branch of initLists.</summary>
	private void InitFacilityLists()
	{
		int row = 0;
		RuleBaseFacility rule = _game.Mod.GetBaseFacility(_selectedTopic);
		if (rule == null)
			return;

		// 1. requires
		List<string> reqs = rule.Requirements;
		if (reqs.Count > 0)
		{
			AddLeft(ref row, Tr("STR_RESEARCH_REQUIRED"), _blue);
			AddLeftResearch(ref row, reqs);
		}

		// 2. requires services (from other base facilities)
		if (rule.RequireBaseFunc.Any())
		{
			AddServices(true, ref row, "STR_SERVICES_REQUIRED", rule.RequireBaseFunc, _gold);
		}

		row = 0;

		// 3. provides services
		if (rule.ProvidedBaseFunc.Any())
		{
			AddServices(false, ref row, "STR_SERVICES_PROVIDED", rule.ProvidedBaseFunc, _gold);
		}

		// 4. forbids services
		if (rule.ForbiddenBaseFunc.Any())
		{
			AddServices(false, ref row, "STR_SERVICES_FORBIDDEN", rule.ForbiddenBaseFunc, _white);
		}
	}

	/// <summary>The TTV_ITEMS branch of initLists.</summary>
	private void InitItemLists()
	{
		_lstLeft.Visible = false;
		_lstRight.Visible = false;
		_lstFull.Visible = true;

		int row = 0;
		RuleItem rule = _game.Mod.GetItem(_selectedTopic);
		if (rule == null)
			return;

		// 1. requires to use/equip
		List<RuleResearch> reqs = rule.Requirements;
		if (reqs.Count > 0)
		{
			AddFull(ref row, Tr("STR_RESEARCH_REQUIRED_USE"), _blue);
			foreach (var i in reqs)
			{
				AddFull(ref row, "  " + Tr(i.Name), GetResearchColor(i.Name), i.Name, TTVMode.TTV_RESEARCH);
			}
		}

		// 2. requires to buy
		List<RuleResearch> reqsBuy = rule.BuyRequirements;
		if (reqsBuy.Count > 0)
		{
			AddFull(ref row, Tr("STR_RESEARCH_REQUIRED_BUY"), _blue);
			foreach (var i in reqsBuy)
			{
				AddFull(ref row, "  " + Tr(i.Name), GetResearchColor(i.Name), i.Name, TTVMode.TTV_RESEARCH);
			}
		}

		// 3. services (from base facilities) required to buy
		if (rule.RequiresBuyBaseFunc.Any())
		{
			List<string> servicesBuy = _game.Mod.GetBaseFunctionNames(rule.RequiresBuyBaseFunc);
			AddFull(ref row, Tr("STR_SERVICES_REQUIRED_BUY"), _blue);
			foreach (var i in servicesBuy)
			{
				AddFull(ref row, "  " + Tr(i), _gold);
			}
		}

		// 4. produced by
		List<string> producedBy = [];
		foreach (var j in _game.Mod.ManufactureList)
		{
			RuleManufacture temp = _game.Mod.GetManufacture(j);
			bool found = false;
			foreach (var i in temp.ProducedItems)
			{
				if (i.Key == rule)
				{
					producedBy.Add(j);
					found = true;
					break;
				}
			}
			if (!found)
			{
				foreach (var itMap in temp.RandomProducedItems)
				{
					foreach (var i in itMap.second)
					{
						if (i.Key == rule)
						{
							producedBy.Add(j);
							found = true;
							break;
						}
					}
					if (found) break;
				}
			}
		}
		if (producedBy.Count > 0)
		{
			AddFull(ref row, Tr("STR_PRODUCED_BY"), _blue);
			foreach (var manuf in producedBy)
			{
				AddFull(ref row, "  " + Tr(manuf) + Tr("STR_M_FLAG"), !IsDiscoveredManufacture(manuf) ? _pink : null, manuf, TTVMode.TTV_MANUFACTURING);
			}
		}

		// 5. spawned by
		List<string> spawnedBy = [];
		foreach (var j in _game.Mod.ResearchList)
		{
			RuleResearch temp = _game.Mod.GetResearch(j);
			if (temp.SpawnedItem == rule.Type)
			{
				spawnedBy.Add(j);
			}
			else
			{
				foreach (var sil in temp.SpawnedItemList)
				{
					if (sil == rule.Type)
					{
						spawnedBy.Add(j);
						break;
					}
				}
			}
		}
		if (spawnedBy.Count > 0)
		{
			AddFull(ref row, Tr("STR_SPAWNED_BY"), _blue);
			foreach (var res in spawnedBy)
			{
				AddFull(ref row, "  " + Tr(res), GetResearchColor(res), res, TTVMode.TTV_RESEARCH);
			}
		}

		// empty line
		AddFull(ref row, "", null);

		// cost to buy
		if (rule.BuyCost > 0)
		{
			AddFull(ref row, Tr("STR_TTV_COST_PER_UNIT"), _blue);
			AddFull(ref row, "  " + Unicode.FormatFunding(rule.BuyCost), _white);
		}
	}

	/// <summary>The TTV_CRAFTS branch of initLists.</summary>
	private void InitCraftLists()
	{
		int row = 0;
		RuleCraft rule = _game.Mod.GetCraft(_selectedTopic);
		if (rule == null)
			return;

		// 1. requires
		List<string> reqs = rule.Requirements;
		if (reqs.Count > 0)
		{
			AddLeft(ref row, Tr("STR_RESEARCH_REQUIRED"), _blue);
			AddLeftResearch(ref row, reqs);
		}

		// 2. services (from base facilities) required to buy
		if (rule.RequiresBuyBaseFunc.Any())
		{
			AddServices(true, ref row, "STR_SERVICES_REQUIRED_BUY", rule.RequiresBuyBaseFunc, _gold);
		}

		// 3. produced by
		List<string> producedBy = [];
		foreach (var j in _game.Mod.ManufactureList)
		{
			RuleManufacture temp = _game.Mod.GetManufacture(j);
			if (temp.ProducedCraft == rule)
			{
				producedBy.Add(j);
				break;
			}
		}
		if (producedBy.Count > 0)
		{
			AddLeft(ref row, Tr("STR_PRODUCED_BY"), _blue);
			foreach (var manuf in producedBy)
			{
				AddLeft(ref row, "  " + Tr(manuf) + Tr("STR_M_FLAG"), !IsDiscoveredManufacture(manuf) ? _pink : null, manuf, TTVMode.TTV_MANUFACTURING);
			}
		}

		// empty line
		AddLeft(ref row, "", null);

		// cost to buy
		if (rule.BuyCost > 0)
		{
			AddLeft(ref row, Tr("STR_TTV_COST_PER_UNIT"), _blue);
			AddLeft(ref row, "  " + Unicode.FormatFunding(rule.BuyCost), _white);
		}
	}

	/// <summary>
	/// Selects the topic.
	/// </summary>
	private void OnSelectLeftTopic(Action action)
	{
		int index = (int)_lstLeft.SelectedRow;
		if (_leftFlags[index] > TTVMode.TTV_NONE)
		{
			_history.Add((_selectedTopic, _selectedFlag));
			_selectedFlag = _leftFlags[index];
			_selectedTopic = _leftTopics[index];
			InitLists();
		}
	}

	/// <summary>
	/// Selects the topic.
	/// </summary>
	private void OnSelectRightTopic(Action action)
	{
		int index = (int)_lstRight.SelectedRow;
		if (_rightFlags[index] > TTVMode.TTV_NONE)
		{
			_history.Add((_selectedTopic, _selectedFlag));
			_selectedFlag = _rightFlags[index];
			_selectedTopic = _rightTopics[index];
			InitLists();
		}
	}

	/// <summary>
	/// Selects the topic.
	/// </summary>
	private void OnSelectFullTopic(Action action)
	{
		int index = (int)_lstFull.SelectedRow;
		if (_leftFlags[index] > TTVMode.TTV_NONE)
		{
			_history.Add((_selectedTopic, _selectedFlag));
			_selectedFlag = _leftFlags[index];
			_selectedTopic = _leftTopics[index];
			InitLists();
		}
	}

	/// <summary>
	/// Changes the selected topic.
	/// </summary>
	public void SetSelectedTopic(string selectedTopic, TTVMode topicType)
	{
		_history.Add((_selectedTopic, _selectedFlag));
		_selectedTopic = selectedTopic;
		_selectedFlag = topicType;
	}

	/// <summary>
	/// Gets the color coding for the given research topic.
	/// </summary>
	public byte GetResearchColor(string topic)
	{
		if (_disabledResearch.Contains(topic))
		{
			return _grey; // disabled
		}
		if (!_alreadyAvailableResearch.Contains(topic))
		{
			return _pink; // not discovered
		}
		return _purple; // discovered
	}

	/// <summary>
	/// Gets the alternative color coding for the given research topic.
	/// </summary>
	public byte GetAltResearchColor(string topic)
	{
		if (_disabledResearch.Contains(topic))
		{
			return _grey; // disabled
		}
		if (!_alreadyAvailableResearch.Contains(topic))
		{
			return _gold; // not discovered
		}
		return _white; // discovered
	}

	/// <summary>
	/// Is given research topic discovered/available?
	/// </summary>
	public bool IsDiscoveredResearch(string topic) => _alreadyAvailableResearch.Contains(topic);

	/// <summary>
	/// Is given manufacture topic discovered/available?
	/// </summary>
	public bool IsDiscoveredManufacture(string topic) => _alreadyAvailableManufacture.Contains(topic);

	/// <summary>
	/// Is given base facility discovered/available?
	/// </summary>
	public bool IsDiscoveredFacility(string topic) => _alreadyAvailableFacilities.Contains(topic);

	/// <summary>
	/// Is given item protected by any research?
	/// </summary>
	public bool IsProtectedItem(string topic) => _protectedItems.Contains(topic);

	/// <summary>
	/// Is given protected item discovered/available for both purchase and usage/equipment?
	/// </summary>
	public bool IsProtectedAndDiscoveredItem(string topic) => _alreadyAvailableItems.Contains(topic);

	/// <summary>
	/// Is given craft discovered/available for both purchase and usage/equipment?
	/// </summary>
	public bool IsDiscoveredCraft(string topic) => _alreadyAvailableCrafts.Contains(topic);
}
