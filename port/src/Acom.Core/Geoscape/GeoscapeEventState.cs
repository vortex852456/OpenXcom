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
// Ported from: src/Geoscape/GeoscapeEventState.h, src/Geoscape/GeoscapeEventState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Displays info about a custom Geoscape event.
/// </summary>
public class GeoscapeEventState : State
{
	private readonly Window _window;
	private readonly Text _txtTitle, _txtMessage;
	private readonly Text _txtItem, _txtQuantity;
	private readonly TextButton _btnOk;
	private readonly ToggleTextButton _btnItemsArriving;
	private readonly TextList _lstTransfers;

	private string _researchName = "";
	private string _bonusResearchName = "";
	private readonly RuleEvent _eventRule;

	/// <summary>
	/// Initializes all the elements in the Geoscape Event window.
	/// </summary>
	/// <param name="eventRule">Game event.</param>
	public GeoscapeEventState(RuleEvent eventRule)
	{
		_eventRule = eventRule;
		_screen = false;

		// Create objects
		_window = new Window(this, 256, 176, 32, 12, WindowPopup.POPUP_BOTH);
		_txtTitle = new Text(236, 32, 42, 23);
		_txtMessage = new Text(236, 96, 42, 58);
		_btnOk = new TextButton(108, 18, 48, 158);
		_btnItemsArriving = new ToggleTextButton(108, 18, 164, 158);
		_txtItem = new Text(114, 9, 44, 58);
		_txtQuantity = new Text(94, 9, 182, 58);
		_lstTransfers = new TextList(216, 80, 42, 69);

		// Set palette
		SetInterface("geoscapeEvent");

		Add(_window, "window", "geoscapeEvent");
		Add(_txtTitle, "text1", "geoscapeEvent");
		Add(_txtMessage, "text2", "geoscapeEvent");
		Add(_btnOk, "button", "geoscapeEvent");
		Add(_btnItemsArriving, "button", "geoscapeEvent");
		Add(_txtItem, "text2", "geoscapeEvent");
		Add(_txtQuantity, "text2", "geoscapeEvent");
		Add(_lstTransfers, "list", "geoscapeEvent");

		CenterAllSurfaces();

		// Set up objects
		_window.SetBackground(_game.Mod.GetSurface(_eventRule.Background));

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetBig();
		_txtTitle.SetWordWrap(true);
		_txtTitle.SetText(Tr(_eventRule.Name));

		_txtMessage.VerticalAlign = TextVAlign.ALIGN_TOP;
		_txtMessage.SetWordWrap(true);
		_txtMessage.SetText(Tr(_eventRule.Description));
		if (_eventRule.AlignBottom())
		{
			_txtMessage.VerticalAlign = TextVAlign.ALIGN_BOTTOM;
		}
		_txtMessage.SetScrollable(true);

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_btnItemsArriving.Text = Tr("STR_ITEMS_ARRIVING");
		_btnItemsArriving.OnMouseClick(BtnItemsArrivingClick);

		_txtItem.SetText(Tr("STR_ITEM"));

		_txtQuantity.SetText(Tr("STR_QUANTITY_UC"));

		_lstTransfers.SetColumns(2, 155, 41);
		_lstTransfers.SetSelectable(true);
		_lstTransfers.SetBackground(_window);
		_lstTransfers.Margin = 2;

		EventLogic();

		_txtItem.Visible = false;
		_txtQuantity.Visible = false;
		_lstTransfers.Visible = false;
		if (_eventRule.Invert)
		{
			_btnItemsArriving.Text = Tr("STR_SUMMARY");
		}
		else if (_lstTransfers.Texts == 0 || !Options.oxceGeoscapeEventsInstantDelivery)
		{
			_btnOk.X = (_btnOk.X + _btnItemsArriving.X) / 2;
			_btnItemsArriving.Visible = false;
		}
	}

	/// <summary>
	/// Helper performing event logic.
	/// </summary>
	private void EventLogic()
	{
		if (_eventRule.AdhocMissionScriptTags.Count != 0)
		{
			var geo = _game.GeoscapeState;
			geo.DetermineAlienMissions(false, _eventRule);
		}

		SavedGame save = _game.SavedGame;
		Base hq = save.Bases[0];
		Mod mod = _game.Mod;
		RuleEvent rule = _eventRule;

		RuleRegion regionRule = null;
		City city = null;
		if (rule.RegionList.Count != 0)
		{
			int pickRegion = Rng.Generate(0, rule.RegionList.Count - 1);
			var regionName = rule.RegionList[pickRegion];
			regionRule = _game.Mod.GetRegion(regionName, true);
			string place = Tr(regionName);

			if (rule.IsCitySpecific)
			{
				int cities = regionRule.Cities.Count;
				if (cities > 0)
				{
					int pickCity = Rng.Generate(0, cities - 1);
					city = regionRule.Cities[pickCity];
					place = city.GetName(_game.Language);
				}
			}

			string titlePlus = Tr(rule.Name).Arg(place);
			_txtTitle.SetText(titlePlus);

			string messagePlus = Tr(rule.Description).Arg(place);
			_txtMessage.SetText(messagePlus);
		}

		// even if the event isn't city-specific, we'll still pick one city randomly to represent the region (and maybe even a country)
		if (regionRule != null)
		{
			if (!rule.IsCitySpecific)
			{
				int cities = regionRule.Cities.Count;
				if (cities > 0)
				{
					int pickCity = Rng.Generate(0, cities - 1);
					city = regionRule.Cities[pickCity];
				}
			}
		}

		// 1. give/take score points
		if (regionRule != null)
		{
			foreach (var region in _game.SavedGame.Regions)
			{
				if (region.Rules == regionRule)
				{
					region.AddActivityXcom(rule.Points);
					break;
				}
			}
		}
		else
		{
			save.AddResearchScore(rule.Points);
		}

		// 2. give/take funds
		save.Funds = save.Funds + rule.Funds;

		// 3. spawn/transfer persons (soldiers, engineers, scientists, ...)
		string spawnedPersonType = rule.SpawnedPersonType;
		if (rule.SpawnedPersons > 0 && !string.IsNullOrEmpty(spawnedPersonType))
		{
			if (spawnedPersonType == "STR_SCIENTIST")
			{
				Transfer t = new Transfer(24);
				t.SetScientists(rule.SpawnedPersons);
				hq.Transfers.Add(t);
			}
			else if (spawnedPersonType == "STR_ENGINEER")
			{
				Transfer t = new Transfer(24);
				t.SetEngineers(rule.SpawnedPersons);
				hq.Transfers.Add(t);
			}
			else
			{
				RuleSoldier ruleSoldier = mod.GetSoldier(spawnedPersonType);
				if (ruleSoldier != null)
				{
					for (int i = 0; i < rule.SpawnedPersons; ++i)
					{
						Transfer t = new Transfer(24);
						int nationality = _game.SavedGame.SelectSoldierNationalityByLocation(_game.Mod, ruleSoldier, city);
						Soldier s = mod.GenSoldier(save, ruleSoldier, nationality);
						YamlRootNodeReader reader = new YamlRootNodeReader(rule.SpawnedSoldierTemplate, "(spawned soldier template)");
						s.Load(reader.ToBase(), mod, save, true); // load from soldier template
						if (!string.IsNullOrEmpty(rule.SpawnedPersonName))
						{
							s.SetName(Tr(rule.SpawnedPersonName));
						}
						else
						{
							s.GenName();
						}
						t.Soldier = s;
						hq.Transfers.Add(t);
					}
				}
			}
		}

		// 3. spawn/transfer multiple soldiers into the HQ
		{
			// C++ std::map keyed by pointer (address order); the port keeps first-insertion order
			Dictionary<RuleSoldier, int> soldiersToTransfer = [];
			foreach (var pair in rule.EveryMultiSoldierList)
			{
				RuleSoldier soldierRule = mod.GetSoldier(pair.Key, true);
				if (soldierRule != null)
				{
					soldiersToTransfer[soldierRule] = soldiersToTransfer.GetValueOrDefault(soldierRule) + pair.Value;
				}
			}
			if (rule.RandomMultiSoldierList.Count != 0)
			{
				int pickSoldier = Rng.Generate(0, rule.RandomMultiSoldierList.Count - 1);
				var sublist = rule.RandomMultiSoldierList[pickSoldier];
				foreach (var pair in sublist)
				{
					RuleSoldier soldierRule = mod.GetSoldier(pair.Key, true);
					if (soldierRule != null)
					{
						soldiersToTransfer[soldierRule] = soldiersToTransfer.GetValueOrDefault(soldierRule) + pair.Value;
					}
				}
			}
			foreach (var ts in soldiersToTransfer)
			{
				for (int i = 0; i < ts.Value; ++i)
				{
					Transfer t = new Transfer(24);
					int nationality = _game.SavedGame.SelectSoldierNationalityByLocation(_game.Mod, ts.Key, city);
					Soldier s = mod.GenSoldier(save, ts.Key, nationality);
					YamlRootNodeReader reader = new YamlRootNodeReader(rule.SpawnedSoldierTemplate, "(spawned soldier template)");
					s.Load(reader.ToBase(), mod, save, true); // load from soldier template
					{
						// reset what may have been loaded
						s.GenName();
					}
					t.Soldier = s;
					hq.Transfers.Add(t);
				}
			}
		}

		// 3. spawn/transfer item into the HQ
		SortedDictionary<string, int> itemsToTransfer = new(StringComparer.Ordinal);

		foreach (var pair in rule.EveryMultiItemList)
		{
			RuleItem itemRule = mod.GetItem(pair.Key, true);
			if (itemRule != null)
			{
				itemsToTransfer[itemRule.Type] = itemsToTransfer.GetValueOrDefault(itemRule.Type) + pair.Value;
			}
		}

		foreach (var itemName in rule.EveryItemList)
		{
			RuleItem itemRule = mod.GetItem(itemName, true);
			if (itemRule != null)
			{
				itemsToTransfer[itemRule.Type] = itemsToTransfer.GetValueOrDefault(itemRule.Type) + 1;
			}
		}

		if (rule.RandomItemList.Count != 0)
		{
			int pickItem = Rng.Generate(0, rule.RandomItemList.Count - 1);
			RuleItem randomItem = mod.GetItem(rule.RandomItemList[pickItem], true);
			if (randomItem != null)
			{
				itemsToTransfer[randomItem.Type] = itemsToTransfer.GetValueOrDefault(randomItem.Type) + 1;
			}
		}

		if (rule.RandomMultiItemList.Count != 0)
		{
			int pickItem = Rng.Generate(0, rule.RandomMultiItemList.Count - 1);
			var sublist = rule.RandomMultiItemList[pickItem];
			foreach (var pair in sublist)
			{
				RuleItem itemRule = mod.GetItem(pair.Key, true);
				if (itemRule != null)
				{
					itemsToTransfer[itemRule.Type] = itemsToTransfer.GetValueOrDefault(itemRule.Type) + pair.Value;
				}
			}
		}

		if (!rule.WeightedItemList.Empty())
		{
			RuleItem randomItem = mod.GetItem(rule.WeightedItemList.Choose(), true);
			if (randomItem != null)
			{
				itemsToTransfer[randomItem.Type] = itemsToTransfer.GetValueOrDefault(randomItem.Type) + 1;
			}
		}

		foreach (var ti in itemsToTransfer)
		{
			int quantity = ti.Value; // C++ modifies the map entry in place; it is not read again
			if (rule.Invert)
			{
				RuleItem r = mod.GetItem(ti.Key, true);
				int removed = 0;
				foreach (var xbase in save.Bases)
				{
					int bQty = xbase.StorageItems.GetItem(r);
					if (bQty > 0)
					{
						int toRemove = Math.Min(bQty, quantity);
						xbase.StorageItems.RemoveItem(r, toRemove);
						quantity -= toRemove;
						removed += toRemove;
					}
					if (quantity <= 0) break; // already removed enough
				}
				if (quantity > 0)
				{
					foreach (var xbase in save.Bases)
					{
						foreach (var xcraft in xbase.Crafts)
						{
							int cQty = xcraft.Items.GetItem(r);
							if (cQty > 0 && xcraft.Status != "STR_OUT")
							{
								int toRemove = Math.Min(cQty, quantity);
								xcraft.Items.RemoveItem(r, toRemove);
								quantity -= toRemove;
								removed += toRemove;
							}
							if (quantity <= 0) break; // already removed enough
						}
						if (quantity <= 0) break; // already removed enough
					}
				}
				_lstTransfers.AddRow(2, Tr(ti.Key), (-removed).ToString(CultureInfo.InvariantCulture));
			}
			else if (Options.oxceGeoscapeEventsInstantDelivery)
			{
				hq.StorageItems.AddItem(mod.GetItem(ti.Key, true), quantity);
				_lstTransfers.AddRow(2, Tr(ti.Key), quantity.ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				Transfer t = new Transfer(1);
				t.SetItems(mod.GetItem(ti.Key, true), quantity);
				hq.Transfers.Add(t);
			}
		}

		// 3b. spawn craft into the HQ
		RuleCraft craftRule = mod.GetCraft(rule.SpawnedCraftType, true);
		if (craftRule != null)
		{
			Craft craft = new Craft(craftRule, hq, save.GetId(craftRule.Type));
			craft.InitFixedWeapons(mod);
			if (Options.oxceGeoscapeEventsInstantDelivery)
			{
				// same as manufacture
				craft.Checkup();
				hq.Crafts.Add(craft);
			}
			else
			{
				// same as buy
				craft.Status = "STR_REFUELLING";
				Transfer t = new Transfer(1);
				t.Craft = craft;
				hq.Transfers.Add(t);
			}
		}

		// 4. give bonus research
		List<RuleResearch> possibilities = [];
		foreach (var rRule in rule.ResearchList)
		{
			if (!save.IsResearched(rRule, false) || save.HasUndiscoveredGetOneFree(rRule, true))
			{
				possibilities.Add(rRule);
			}
		}

		List<RuleResearch> topicsToCheck = [];
		if (possibilities.Count != 0)
		{
			int pickResearch = Rng.Generate(0, possibilities.Count - 1);
			RuleResearch eventResearch = possibilities[pickResearch];

			bool alreadyResearched = save.IsResearched(eventResearch.Lookup ?? eventResearch, false); // we have seen the pedia article already, don't show it again

			void addResearchDiaryEntryForEvent(RuleResearch discoveredResearch, DiscoverySourceType sourceType, RuleEvent sourceEvent, RuleResearch sourceResearch)
			{
				if (!save.IsResearched(discoveredResearch, false) && !save.IsResearchRuleStatusDisabled(discoveredResearch.Name))
				{
					ResearchDiaryEntry entry = new ResearchDiaryEntry(discoveredResearch);
					entry.SetDate(save.Time);
					entry.source.type = sourceType;
					if (sourceType == DiscoverySourceType.EVENT)
					{
						entry.source.@event = sourceEvent;
						entry.source.name = sourceEvent.Name;
					}
					else // sourceType == DiscoverySourceType::FREE_FROM
					{
						entry.source.research = sourceResearch;
						entry.source.name = sourceResearch.Name;
					}
					save.AddResearchDiaryEntry(entry);
				}
			}

			addResearchDiaryEntryForEvent(eventResearch, DiscoverySourceType.EVENT, rule, null);
			save.AddFinishedResearch(eventResearch, mod, hq, true);
			topicsToCheck.Add(eventResearch);
			_researchName = alreadyResearched ? "" : eventResearch.Name;

			RuleResearch lookupResearch = eventResearch.Lookup;
			if (lookupResearch != null)
			{
				addResearchDiaryEntryForEvent(lookupResearch, DiscoverySourceType.EVENT, rule, null);
				save.AddFinishedResearch(lookupResearch, mod, hq, true);
				_researchName = alreadyResearched ? "" : lookupResearch.Name;
			}

			RuleResearch bonus = save.SelectGetOneFree(eventResearch);
			if (bonus != null)
			{
				addResearchDiaryEntryForEvent(bonus, DiscoverySourceType.FREE_FROM, null, eventResearch);
				save.AddFinishedResearch(bonus, mod, hq, true);
				topicsToCheck.Add(bonus);
				_bonusResearchName = bonus.Name;

				RuleResearch bonusLookup = bonus.Lookup;
				if (bonusLookup != null)
				{
					addResearchDiaryEntryForEvent(bonusLookup, DiscoverySourceType.FREE_FROM, null, eventResearch);
					save.AddFinishedResearch(bonusLookup, mod, hq, true);
					_bonusResearchName = bonusLookup.Name;
				}
			}
		}

		// Side effects:
		// 1. remove obsolete research projects from all bases
		// 2. handle items spawned by research
		// 3. handle events spawned by research
		save.HandlePrimaryResearchSideEffects(topicsToCheck, mod, hq);

		if (Options.oxceGeoscapeDebugLogMaxEntries > 0)
		{
			save.GeoscapeDebugLog.Add($"gameTime: {save.Time.FullString} eventPopup: {rule.Name}");
		}
	}

	/// <summary>
	/// Initializes the state.
	/// </summary>
	public override void Init()
	{
		base.Init();

		if (!string.IsNullOrEmpty(_eventRule.Music))
		{
			_game.Mod.PlayMusic(_eventRule.Music);
		}
	}

	/// <summary>
	/// Closes the window and shows a pedia article if needed.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();

		if (!string.IsNullOrEmpty(_eventRule.Cutscene))
		{
			_game.PushState(new CutsceneState(_eventRule.Cutscene));

			if (_game.SavedGame.Ending == GameEnding.END_NONE)
			{
				RuleVideo videoRule = _game.Mod.GetVideo(_eventRule.Cutscene, true);
				if (videoRule.WinGame) _game.SavedGame.Ending = GameEnding.END_WIN;
				if (videoRule.LoseGame) _game.SavedGame.Ending = GameEnding.END_LOSE;
			}
		}

		if (_game.SavedGame.Ending == GameEnding.END_NONE)
		{
			Base @base = _game.SavedGame.Bases[0];
			if (_game.SavedGame.MonthsPassed > -1 && Options.storageLimitsEnforced && @base != null && @base.StoresOverfull())
			{
				_game.PushState(new SellState(@base, null));
				RuleInterface debriefing = _game.Mod.GetInterface("debriefing");
				_game.PushState(new ErrorMessageState(Tr("STR_STORAGE_EXCEEDED").Arg(@base.GetName(null)), _palette, unchecked((byte)debriefing.GetElement("errorMessage").color), "BACK01.SCR", debriefing.GetElement("errorPalette").color));
			}
		}

		if (_bonusResearchName.Length != 0)
		{
			Ufopaedia.OpenArticle(_game, _bonusResearchName);
		}

		if (_researchName.Length != 0)
		{
			Ufopaedia.OpenArticle(_game, _researchName);
		}
	}

	/// <summary>
	/// Toggles the view between the description and the ItemsArriving list.
	/// </summary>
	public void BtnItemsArrivingClick(Action action)
	{
		if (_btnItemsArriving.Pressed)
		{
			_txtMessage.Visible = false;
			_txtItem.Visible = true;
			_txtQuantity.Visible = true;
			_lstTransfers.Visible = true;
		}
		else
		{
			_txtItem.Visible = false;
			_txtQuantity.Visible = false;
			_lstTransfers.Visible = false;
			_txtMessage.Visible = true;
		}
	}
}
