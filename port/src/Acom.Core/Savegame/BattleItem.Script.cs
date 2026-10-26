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
// Ported from: src/Savegame/BattleItem.h, src/Savegame/BattleItem.cpp (script binding)
#nullable disable

using System.Globalization;
using static Acom.ScriptParam;

namespace Acom;

public partial class BattleItem
{
	private readonly ScriptValues<BattleItem> _scriptValues = new();

	/// <summary>Get script values.</summary>
	public ScriptValues<BattleItem> GetScriptValuesRaw() => _scriptValues;

	private static string DebugDisplayScript(BattleItem bt)
	{
		if (bt != null)
		{
			var rule = bt.Rules;
			string s = ScriptName + "(type: \"" + rule.Type + "\" id: " + bt.Id.ToString(CultureInfo.InvariantCulture);

			int clipSize = rule.ClipSize;
			if (clipSize > 0)
			{
				s += " ammo: " + bt.AmmoQuantity.ToString(CultureInfo.InvariantCulture) + "/" + clipSize.ToString(CultureInfo.InvariantCulture);
			}
			s += ")";
			return s;
		}
		else
		{
			return "null";
		}
	}

	private static ScriptAction GetAmmoForSlotScript(bool fixedSlot) => (ref ScriptArgs a) =>
	{
		var weapon = a.Ptr<BattleItem>(0);
		int slot = fixedSlot ? 0 : a.Int(2);
		a.SetPtr(1, weapon != null && slot >= 0 && slot < RuleItem.AmmoSlotMax ? weapon.GetAmmoForSlot(slot) : null);
	};

	/// <summary>Register BattleItem in script parser.</summary>
	public static void ScriptRegister(ScriptParserBase parser)
	{
		parser.RegisterPointerType<Mod>();
		parser.RegisterPointerType<Tile>();
		parser.RegisterPointerType<RuleItem>();
		parser.RegisterPointerType<BattleUnit>();

		var bi = new Bind<BattleItem>(parser);
		var self = Bind<BattleItem>.Self;
		var selfE = Bind<BattleItem>.SelfE;

		bi.AddRules<RuleItem>("getRuleItem", b => b.Rules);
		bi.AddPair<BattleUnit>("getBattleUnit", b => b.Unit);
		bi.Add("getAmmoItem", [selfE, PtrERef<BattleItem>()], GetAmmoForSlotScript(true));
		bi.Add("getAmmoItem", [self, PtrRef<BattleItem>()], GetAmmoForSlotScript(true));
		bi.Add("getAmmoForSlot", [selfE, PtrERef<BattleItem>(), Int], GetAmmoForSlotScript(false));
		bi.Add("getAmmoForSlot", [self, PtrRef<BattleItem>(), Int], GetAmmoForSlotScript(false));
		bi.Add("getAmmoForAction", [selfE, PtrERef<BattleItem>(), Int], (ref ScriptArgs a) =>
		{
			var weapon = a.Ptr<BattleItem>(0);
			a.SetPtr(1, weapon?.GetAmmoForAction((BattleActionType)a.Int(2)));
		});
		bi.Add("getAmmoForAction", [self, PtrRef<BattleItem>(), Int], (ref ScriptArgs a) =>
		{
			var weapon = a.Ptr<BattleItem>(0);
			a.SetPtr(1, weapon?.GetAmmoForActionConst((BattleActionType)a.Int(2)));
		});

		bi.Add("getSlot", [self, PtrRef<RuleInventory>()], (ref ScriptArgs a) =>
		{
			var weapon = a.Ptr<BattleItem>(0);
			a.SetPtr(1, weapon?.Slot);
		});
		bi.Add("getMoveToCost", [self, IntRef, Ptr<RuleInventory>()], (ref ScriptArgs a) =>
		{
			var weapon = a.Ptr<BattleItem>(0);
			var inv = a.Ptr<RuleInventory>(2);
			a.IntRef(1) = weapon != null && weapon.Slot != null && inv != null ? weapon.GetMoveToCost(inv) : 0;
		}, "cost of moving item from slot in first arg to slot from last arg");
		bi.AddField("InventoryMoveCost.getBaseTimePercent", "InventoryMoveCost.setBaseTimePercent", b => b._inventoryMoveCostPercent, (b, v) => b._inventoryMoveCostPercent = v);

		bi.AddPair<BattleUnit>("getPreviousOwner", b => b.PreviousOwner);
		bi.AddPair<BattleUnit>("getOwner", b => b.Owner);
		bi.AddGet("getId", b => b.Id);
		bi.AddGet("getGlow", b => b.Glow);
		bi.AddGet("getTotalWeight", b => b.TotalWeight);
		bi.AddGet("isAmmo", b => b.IsAmmo);
		bi.AddGet("isSpecialWeapon", b => b.IsSpecialWeapon);

		bi.AddGet("getAmmoQuantityMax", b => b.Rules.ClipSize);
		bi.AddGet("getAmmoQuantity", b => b.AmmoQuantity);
		bi.Add("setAmmoQuantity", [selfE, Int], (ref ScriptArgs a) =>
		{
			var bt = a.Ptr<BattleItem>(0);
			if (bt != null) bt.AmmoQuantity = ScriptClamp(a.Int(1), 1, bt.Rules.ClipSize);
		});

		bi.AddGet("getFuseTimer", b => b.FuseTimer);
		bi.Add("getFuseTimerDefault", [self, IntRef], (ref ScriptArgs a) =>
		{
			var bt = a.Ptr<BattleItem>(0);
			a.IntRef(1) = bt != null ? bt.Rules.FuseTimerDefault : -1;
		}, "get default fuse timer");
		bi.Add("setFuseTimer", [selfE, Int], (ref ScriptArgs a) =>
		{
			var bt = a.Ptr<BattleItem>(0);
			if (bt != null) bt.FuseTimer = ScriptClamp(a.Int(1), -1, 100);
		}, "set item fuse timer, -1 mean disable it");

		bi.AddGet("isFuseEnabled", b => b.IsFuseEnabled, "check if fuse is triggered (like throw or proxy unit)");
		bi.Add("setFuseEnabled", [selfE, Int], (ref ScriptArgs a) =>
		{
			var bt = a.Ptr<BattleItem>(0);
			bt?.SetFuseEnabled(a.Int(1) != 0);
		}, "force set or unset fuse trigger state");

		bi.AddGet("getHealQuantity", b => b.HealQuantity);
		bi.Add("setHealQuantity", [selfE, Int], (ref ScriptArgs a) =>
		{
			var bt = a.Ptr<BattleItem>(0);
			if (bt != null) bt.HealQuantity = ScriptClamp(a.Int(1), 0, bt.Rules.HealQuantity);
		});

		bi.AddGet("getPainKillerQuantity", b => b.PainKillerQuantity);
		bi.Add("setPainKillerQuantity", [selfE, Int], (ref ScriptArgs a) =>
		{
			var bt = a.Ptr<BattleItem>(0);
			if (bt != null) bt.PainKillerQuantity = ScriptClamp(a.Int(1), 0, bt.Rules.PainKillerQuantity);
		});

		bi.AddGet("getStimulantQuantity", b => b.StimulantQuantity);
		bi.Add("setStimulantQuantity", [selfE, Int], (ref ScriptArgs a) =>
		{
			var bt = a.Ptr<BattleItem>(0);
			if (bt != null) bt.StimulantQuantity = ScriptClamp(a.Int(1), 0, bt.Rules.StimulantQuantity);
		});

		bi.Add("getActionCost.getTimeUnits", [self, IntRef, Ptr<BattleUnit>(), Int], (ref ScriptArgs a) =>
		{
			var bt = a.Ptr<BattleItem>(0);
			var bu = a.Ptr<BattleUnit>(2);
			var bat = (BattleActionType)a.Int(3);
			a.IntRef(1) = bt != null && bu != null ? bu.GetActionTUs(bat, bt).Time : -1;
		});

		bi.AddScriptValue(b => b._rules.GetScriptValuesRaw(), false);
		bi.AddScriptValue(b => b._scriptValues);
		bi.AddDebugDisplay(DebugDisplayScript);

		bi.AddCustomConst("BA_AUTOSHOT", (int)BattleActionType.BA_AUTOSHOT);
		bi.AddCustomConst("BA_SNAPSHOT", (int)BattleActionType.BA_SNAPSHOT);
		bi.AddCustomConst("BA_AIMEDSHOT", (int)BattleActionType.BA_AIMEDSHOT);
		bi.AddCustomConst("BA_LAUNCH", (int)BattleActionType.BA_LAUNCH);
		bi.AddCustomConst("BA_HIT", (int)BattleActionType.BA_HIT);
		bi.AddCustomConst("BA_USE", (int)BattleActionType.BA_USE);
		bi.AddCustomConst("BA_THROW", (int)BattleActionType.BA_THROW);
		bi.AddCustomConst("BA_MINDCONTROL", (int)BattleActionType.BA_MINDCONTROL);
		bi.AddCustomConst("BA_PANIC", (int)BattleActionType.BA_PANIC);
		bi.AddCustomConst("BA_PRIME", (int)BattleActionType.BA_PRIME);
		bi.AddCustomConst("BA_UNPRIME", (int)BattleActionType.BA_UNPRIME);
		bi.AddCustomConst("BA_NONE", (int)BattleActionType.BA_NONE);
		bi.AddCustomConst("BA_TRIGGER_TIMED_GRENADE", (int)BattleActionType.BA_TRIGGER_TIMED_GRENADE);
		bi.AddCustomConst("BA_TRIGGER_PROXY_GRENADE", (int)BattleActionType.BA_TRIGGER_PROXY_GRENADE);
	}

	/// <summary>OXCE <c>Clamp</c>: <c>std::min(std::max(val, min), max)</c> (no exception when min &gt; max).</summary>
	internal static int ScriptClamp(int val, int min, int max) => Math.Min(Math.Max(val, min), max);

	/// <summary>Init all required data in script using object data.</summary>
	public static void ScriptFill(ScriptWorkerBlit w, BattleItem item, SavedBattleGame save, int part, int anim_frame, int shade)
	{
		ArgumentNullException.ThrowIfNull(w);
		w.Clear();
		if (item != null)
		{
			var scr = item.Rules.GetScriptEvents(ModScript.RecolorItemSprite);
			if (scr != null)
			{
				w.Update(scr, item, save, part, anim_frame, shade);
			}
			else
			{
				BattleUnit.ScriptFill(w, item.Unit, save, part, anim_frame, shade, 0);
			}
		}
	}
}
