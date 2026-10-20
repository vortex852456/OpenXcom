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
// Ported from: src/Savegame/Production.h, src/Savegame/Production.cpp
#nullable disable

namespace Acom;

public enum productionProgress_e : uint
{
	PROGRESS_NOT_COMPLETE = 0,
	PROGRESS_COMPLETE = 1,
	PROGRESS_NOT_ENOUGH_MONEY = 2,
	PROGRESS_NOT_ENOUGH_MATERIALS = 3,
	PROGRESS_NOT_ENOUGH_LIVING_SPACE = 4,
	PROGRESS_MAX = 5,
	PROGRESS_CONSTRUCTION = 6,
}

/// <summary>A manufacturing project running in a base.</summary>
public class Production
{
	private readonly RuleManufacture _rules;
	private int _amount;
	private bool _infinite;
	private int _timeSpent;
	private int _engineers;
	private bool _sell;
	private bool _isFallback;
	private SortedDictionary<string, int> _randomProductionInfo = new(StringComparer.Ordinal);

	public Production(RuleManufacture rules, int amount)
	{
		_rules = rules;
		_amount = amount;
		_infinite = false;
		_timeSpent = 0;
		_engineers = 0;
		_sell = false;
		_isFallback = false;
	}

	public int AmountTotal
	{
		get => _amount;
		set => _amount = value;
	}

	public bool InfiniteAmount
	{
		get => _infinite;
		set => _infinite = value;
	}

	public int TimeSpent
	{
		get => _timeSpent;
		set => _timeSpent = value;
	}

	/// <summary>No progress made yet and nobody assigned.</summary>
	public bool IsQueuedOnly => TimeSpent == 0 && AssignedEngineers == 0;

	public int AssignedEngineers
	{
		get => _engineers;
		set => _engineers = value;
	}

	public bool SellItems
	{
		get => _sell;
		set => _sell = value;
	}

	public bool IsFallback => _isFallback;

	public void SetFallback(bool newValue) => _isFallback = newValue;

	private bool HaveEnoughMoneyForOneMoreUnit(SavedGame g)
	{
		return _rules.HaveEnoughMoneyForOneMoreUnit(g.Funds);
	}

	private bool HaveEnoughLivingSpaceForOneMoreUnit(Base b)
	{
		if (_rules.SpawnedPersonType != "")
		{
			// Note: if the production is running then the space we need is already counted by getUsedQuarters
			if (b.AvailableQuarters < b.UsedQuarters)
			{
				return false;
			}
		}
		return true;
	}

	private bool HaveEnoughMaterialsForOneMoreUnit(Base b)
	{
		foreach (var i in _rules.RequiredItems)
		{
			if (b.StorageItems.GetItem(i.Key) < i.Value)
				return false;
		}
		foreach (var i in _rules.RequiredCrafts)
		{
			if (b.GetCraftCountForProduction(i.Key) < i.Value)
				return false;
		}
		return true;
	}

	/// <summary>Adds an item (or the fixed items of a random set) to the base or to a transfer.</summary>
	private static void DeliverItem(Base b, RuleItem item, int qty, int transferTimeItems)
	{
		if (transferTimeItems > 0)
		{
			var t = new Transfer(transferTimeItems);
			t.SetItems(item, qty);
			b.Transfers.Add(t);
		}
		else
		{
			b.StorageItems.AddItem(item, qty);
			if (item.BattleType == BattleType.BT_NONE)
			{
				foreach (var c in b.Crafts)
				{
					c.ReuseItem(item);
				}
			}
		}
	}

	public productionProgress_e Step(Base b, SavedGame g, Mod m, Language lang)
	{
		if (_isFallback)
		{
			int availableEngineer = b.AvailableEngineers;
			int availableWorkSpace = b.FreeWorkshops;

			if (IsQueuedOnly)
			{
				// start counting the workshop space now
				availableWorkSpace -= _rules.RequiredSpace;
			}
			if (availableEngineer > 0 && availableWorkSpace > 0)
			{
				int change = Math.Min(availableEngineer, availableWorkSpace);
				AssignedEngineers = AssignedEngineers + change;
				b.Engineers = b.Engineers - change;
			}
		}

		int done = AmountProduced;
		_timeSpent += _engineers;

		if (done < AmountProduced)
		{
			int produced;
			if (!InfiniteAmount)
			{
				produced = Math.Min(AmountProduced, _amount) - done; // std::min is required because we don't want to overproduce
			}
			else
			{
				produced = AmountProduced - done;
			}
			int count = 0;
			do
			{
				var ruleCraft = _rules.ProducedCraft;
				if (ruleCraft != null)
				{
					var craft = new Craft(ruleCraft, b, g.GetId(ruleCraft.Type));
					craft.InitFixedWeapons(m);
					craft.Checkup();
					int transferTimeCraft = Math.Max(0, _rules.TransferTimes.Count < 3 ? 0 : _rules.TransferTimes[2]);
					if (transferTimeCraft > 0)
					{
						var t = new Transfer(transferTimeCraft);
						t.Craft = craft;
						b.Transfers.Add(t);
					}
					else
					{
						b.Crafts.Add(craft);
					}
				}
				else
				{
					int transferTimeItems = Math.Max(0, _rules.TransferTimes.Count == 0 ? 0 : _rules.TransferTimes[0]);
					foreach (var i in _rules.ProducedItems)
					{
						if (SellItems)
						{
							long adjustedSellValue = i.Key.GetSellCostAdjusted(b, g);
							adjustedSellValue *= i.Value;
							g.Funds = g.Funds + adjustedSellValue;
						}
						else
						{
							DeliverItem(b, i.Key, i.Value, transferTimeItems);
							if (_rules.RandomProducedItems.Count != 0)
							{
								AddRandomProductionInfo(i.Key.Type, i.Value);
							}
						}
					}
				}
				// Random manufacture
				if (_rules.RandomProducedItems.Count != 0)
				{
					int transferTimeItems = Math.Max(0, _rules.TransferTimes.Count == 0 ? 0 : _rules.TransferTimes[0]);
					int totalWeight = 0;
					foreach (var itemSet in _rules.RandomProducedItems)
					{
						totalWeight += itemSet.first;
					}
					// RNG
					int roll = Rng.Generate(1, totalWeight);
					int runningTotal = 0;
					foreach (var itemSet in _rules.RandomProducedItems)
					{
						runningTotal += itemSet.first;
						if (runningTotal >= roll)
						{
							foreach (var i in itemSet.second)
							{
								DeliverItem(b, i.Key, i.Value, transferTimeItems);
								AddRandomProductionInfo(i.Key.Type, i.Value);
							}
							// break outer loop
							break;
						}
					}
				}
				// Spawn persons (soldiers, engineers, scientists, ...)
				string spawnedPersonType = _rules.SpawnedPersonType;
				if (spawnedPersonType != "")
				{
					int transferTimePersonnel = Math.Max(1, _rules.TransferTimes.Count < 2 ? 24 : _rules.TransferTimes[1]);
					if (spawnedPersonType == "STR_SCIENTIST")
					{
						var t = new Transfer(transferTimePersonnel);
						t.SetScientists(1);
						b.Transfers.Add(t);
					}
					else if (spawnedPersonType == "STR_ENGINEER")
					{
						var t = new Transfer(transferTimePersonnel);
						t.SetEngineers(1);
						b.Transfers.Add(t);
					}
					else
					{
						var rule = m.GetSoldier(spawnedPersonType);
						if (rule != null)
						{
							var t = new Transfer(transferTimePersonnel);
							int nationality = g.SelectSoldierNationalityByLocation(m, rule, b);
							var s = m.GenSoldier(g, rule, nationality);
							var reader = new YamlRootNodeReader(_rules.SpawnedSoldierTemplate, "(spawned soldier template)");
							s.Load(reader.ToBase(), m, g, true); // load from soldier template
							if (_rules.SpawnedPersonName != "")
							{
								s.SetName(lang.GetString(_rules.SpawnedPersonName));
							}
							else
							{
								s.GenName();
							}
							t.Soldier = s;
							b.Transfers.Add(t);
						}
					}
				}
				if (_rules.Points != 0)
				{
					// yes, negative points are allowed too
					g.AddResearchScore(_rules.Points);
				}
				count++;
				if (count < produced)
				{
					// We need to ensure that player has enough cash/item to produce a new unit
					if (!HaveEnoughMoneyForOneMoreUnit(g)) return productionProgress_e.PROGRESS_NOT_ENOUGH_MONEY;
					if (!HaveEnoughMaterialsForOneMoreUnit(b)) return productionProgress_e.PROGRESS_NOT_ENOUGH_MATERIALS;
					StartItem(b, g, m);
				}
			}
			while (count < produced);
		}
		if (AmountProduced >= _amount && !InfiniteAmount) return productionProgress_e.PROGRESS_COMPLETE;
		if (done < AmountProduced)
		{
			// We need to ensure that player has enough cash/item to produce a new unit
			if (!HaveEnoughMoneyForOneMoreUnit(g)) return productionProgress_e.PROGRESS_NOT_ENOUGH_MONEY;
			if (!HaveEnoughLivingSpaceForOneMoreUnit(b)) return productionProgress_e.PROGRESS_NOT_ENOUGH_LIVING_SPACE;
			if (!HaveEnoughMaterialsForOneMoreUnit(b)) return productionProgress_e.PROGRESS_NOT_ENOUGH_MATERIALS;
			StartItem(b, g, m);
		}
		return productionProgress_e.PROGRESS_NOT_COMPLETE;
	}

	/// <summary>C++ <c>_randomProductionInfo[type] += qty</c>.</summary>
	private void AddRandomProductionInfo(string type, int qty)
	{
		_randomProductionInfo.TryGetValue(type, out int current);
		_randomProductionInfo[type] = current + qty;
	}

	public int AmountProduced
	{
		get
		{
			if (_rules.ManufactureTime > 0)
				return _timeSpent / _rules.ManufactureTime;
			else
				return _amount;
		}
	}

	public RuleManufacture Rules => _rules;

	public void StartItem(Base b, SavedGame g, Mod m)
	{
		g.Funds = g.Funds - _rules.ManufactureCost;
		foreach (var i in _rules.RequiredItems)
		{
			b.StorageItems.RemoveItem(i.Key, i.Value);
		}
		foreach (var i in _rules.RequiredCrafts)
		{
			// Find suitable craft
			var crafts = b.Crafts;
			for (int c = 0; c < crafts.Count; ++c)
			{
				if (crafts[c].Rules == i.Key)
				{
					b.RemoveCraft(crafts[c], true);
					break;
				}
			}
		}
	}

	public void RefundItem(Base b, SavedGame g, Mod m)
	{
		g.Funds = g.Funds + _rules.ManufactureCost;
		foreach (var pair in _rules.RequiredItems)
		{
			b.StorageItems.AddItem(pair.Key, pair.Value);
		}
		//for (const auto& pair : _rules->getRequiredCrafts())
		//{
		//	// not supported
		//}
	}

	public void Save(YamlNodeWriter writer)
	{
		writer.SetAsMap();
		writer.Write("item", Rules.Name);
		writer.Write("assigned", AssignedEngineers);
		writer.Write("spent", TimeSpent);
		writer.Write("amount", AmountTotal);
		writer.Write("infinite", InfiniteAmount);
		if (SellItems)
			writer.Write("sell", SellItems);
		if (_isFallback)
			writer.Write("isFallback", _isFallback);
		if (_rules.RandomProducedItems.Count != 0)
			writer.Write("randomProductionInfo", _randomProductionInfo);
	}

	public void Load(YamlNodeReader reader)
	{
		AssignedEngineers = reader["assigned"].ReadVal(AssignedEngineers);
		TimeSpent = reader["spent"].ReadVal(TimeSpent);
		AmountTotal = reader["amount"].ReadVal(AmountTotal);
		InfiniteAmount = reader["infinite"].ReadVal(InfiniteAmount);
		SellItems = reader["sell"].ReadVal(SellItems);
		reader.TryRead("isFallback", ref _isFallback);
		if (_rules.RandomProducedItems.Count != 0)
		{
			_randomProductionInfo = new SortedDictionary<string, int>(reader["randomProductionInfo"].ReadVal(_randomProductionInfo), StringComparer.Ordinal);
		}
		// backwards compatibility
		if (AmountTotal == int.MaxValue)
		{
			AmountTotal = 999;
			InfiniteAmount = true;
			SellItems = true;
		}
	}

	public SortedDictionary<string, int> RandomProductionInfo => _randomProductionInfo;
}
