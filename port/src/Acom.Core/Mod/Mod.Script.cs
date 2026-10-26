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
// Ported from: src/Mod/Mod.cpp (ModScriptGlobal, script binding)
#nullable disable

using static Acom.ScriptParam;

namespace Acom;

/// <summary>Script globals of the game: mod names (<c>RuleList</c>), version constant and global script values.</summary>
public sealed class ModScriptGlobal : ScriptGlobal
{
	private int _modCurr;
	private readonly List<(string first, int second)> _modNames = [];
	private readonly ScriptValues<Mod> _scriptValues = new();

	private void LoadRuleList(ref int value, YamlNodeReader reader)
	{
		if (reader)
		{
			var name = reader.ReadVal<string>();
			if (name == Mod.ModNameMasterPublic)
			{
				value = 0;
			}
			else if (name == Mod.ModNameCurrentPublic)
			{
				value = _modCurr;
			}
			else
			{
				foreach (var p in _modNames)
				{
					if (name == p.first)
					{
						value = p.second;
						return;
					}
				}
				value = -1;
			}
		}
	}

	private void SaveRuleList(int value, YamlNodeWriter writer)
	{
		foreach (var p in _modNames)
		{
			if (value == p.second)
			{
				writer.SetValue(p.first);
				return;
			}
		}
	}

	/// <summary>Initialize shared globals like types.</summary>
	public override void InitParserGlobals(ScriptParserBase parser)
	{
		parser.RegisterPointerType<Mod>();
		parser.RegisterPointerType<SavedGame>();
		parser.RegisterPointerType<SavedBattleGame>();
	}

	/// <summary>Prepare for loading data.</summary>
	public override void BeginLoad()
	{
		base.BeginLoad();

		AddTagValueTypeBase("RuleList",
			(ScriptGlobal g, ref int value, YamlNodeReader reader) => ((ModScriptGlobal)g).LoadRuleList(ref value, reader),
			(g, value, writer) => ((ModScriptGlobal)g).SaveRuleList(value, writer));
		AddConst("RuleList." + Mod.ModNameMasterPublic, 0);
		AddConst("RuleList." + Mod.ModNameCurrentPublic, 0);

		var v = new Mod.OxceVersionDate(Mod.OpenXcomVersionGit);
		AddConst("SCRIPT_VERSION_DATE", v.Year * 10000 + v.Month * 100 + v.Day);
	}

	/// <summary>Add mod name and id.</summary>
	public void AddMod(string s, int i)
	{
		var name = "RuleList." + s;
		AddConst(name, i);
		_modNames.Add((s, i));
	}

	/// <summary>Set current mod id.</summary>
	public void SetMod(int i)
	{
		UpdateConst("RuleList." + Mod.ModNameCurrentPublic, i);
		_modCurr = i;
	}

	/// <summary>Get script values.</summary>
	public ScriptValues<Mod> ScriptValues => _scriptValues;
}

public partial class Mod
{
	internal const string ModNameMasterPublic = ModNameMaster;
	internal const string ModNameCurrentPublic = ModNameCurrent;

	private readonly ModScriptGlobal _scriptGlobal = new();
	private ModScript _scriptParsers;

	/// <summary>Get the global script data.</summary>
	public ScriptGlobal ScriptGlobal => _scriptGlobal;

	/// <summary>
	/// Parsers used while loading rulesets (C++ local <c>ModScript parser</c> of <c>loadAll</c>, passed to every rule load).
	/// Created on demand when rules are loaded outside of <see cref="LoadAll()"/> (tests).
	/// </summary>
	internal ModScript ScriptParsers => _scriptParsers ??= new ModScript(_scriptGlobal, this);

	/// <summary>C++ <c>offset&lt;f&gt;</c>: convert mod index to runtime index of a set.</summary>
	private static ScriptAction OffsetScript(Func<Mod, int> f) => (ref ScriptArgs a) =>
	{
		var m = a.Ptr<Mod>(0);
		if (m == null) return; // C++ dereferences null
		ref int b = ref a.IntRef(1);
		int baseMax = f(m);
		if (b >= baseMax)
		{
			b += a.Int(2);
		}
	};

	private static ScriptAction GetRuleScript<T>(Func<Mod, string, T> get) where T : class => (ref ScriptArgs a) =>
	{
		var mod = a.Ptr<Mod>(0);
		a.SetPtr(1, mod != null ? get(mod, a.Str(2)) : null);
	};

	/// <summary>Register all useful function used by script.</summary>
	public static void ScriptRegister(ScriptParserBase parser)
	{
		parser.RegisterPointerType<Unit>();
		parser.RegisterPointerType<RuleItem>();
		parser.RegisterPointerType<Armor>();
		parser.RegisterPointerType<RuleSkill>();
		parser.RegisterPointerType<RuleResearch>();
		parser.RegisterPointerType<RuleSoldier>();
		parser.RegisterPointerType<RuleInventory>();

		var mod = new Bind<Mod>(parser);
		ScriptParam[] offsetSig = [Bind<Mod>.Self, IntRef, Int];

		mod.Add("getSoundOffsetBattle", offsetSig, OffsetScript(m => m._soundOffsetBattle), "convert mod sound index in first argument to runtime index in given set, second argument is mod id");
		mod.Add("getSoundOffsetGeo", offsetSig, OffsetScript(m => m._soundOffsetGeo), "convert mod sound index in first argument to runtime index in given set, second argument is mod id");
		mod.Add("getSpriteOffsetBasebits", offsetSig, OffsetScript(m => m._surfaceOffsetBasebits), "convert mod surface index in first argument to runtime index in given set, second argument is mod id");
		mod.Add("getSpriteOffsetBigobs", offsetSig, OffsetScript(m => m._surfaceOffsetBigobs), "convert mod surface index in first argument to runtime index in given set, second argument is mod id");
		mod.Add("getSpriteOffsetFloorob", offsetSig, OffsetScript(m => m._surfaceOffsetFloorob), "convert mod surface index in first argument to runtime index in given set, second argument is mod id");
		mod.Add("getSpriteOffsetHandob", offsetSig, OffsetScript(m => m._surfaceOffsetHandob), "convert mod surface index in first argument to runtime index in given set, second argument is mod id");
		mod.Add("getSpriteOffsetHit", offsetSig, OffsetScript(m => m._surfaceOffsetHit), "convert mod surface index in first argument to runtime index in given set, second argument is mod id");
		mod.Add("getSpriteOffsetSmoke", offsetSig, OffsetScript(m => m._surfaceOffsetSmoke), "convert mod surface index in first argument to runtime index in given set, second argument is mod id");
		mod.AddGet("getMaxDarknessToSeeUnits", m => m.MaxDarknessToSeeUnits);
		mod.AddGet("getMaxViewDistance", m => m.MaxViewDistance);
		mod.Add("getSmokeReduction", [Bind<Mod>.Self, IntRef], (ref ScriptArgs a) =>
		{
			var m = a.Ptr<Mod>(0);
			if (m == null) return; // C++ dereferences null
			// initial smoke "density" of a smoke grenade is around 15 per tile
			// we do density/3 to get the decay of visibility
			// so in fresh smoke we should only have 4 tiles of visibility
			// this is traced in voxel space, with smoke affecting visibility every step of the way

			// 3  - coefficient of calculation (see above).
			// 20 - maximum view distance in vanilla Xcom.
			// Even if MaxViewDistance will be increased via ruleset, smoke will keep effect.
			ref int smoke = ref a.IntRef(1);
			smoke = smoke * m.MaxViewDistance / (3 * 20);
		});

		mod.Add("getRuleUnit", [Bind<Mod>.Self, PtrRef<Unit>(), ScriptParam.Text], GetRuleScript((m, s) => m.GetUnit(s)));
		mod.Add("getRuleItem", [Bind<Mod>.Self, PtrRef<RuleItem>(), ScriptParam.Text], GetRuleScript((m, s) => m.GetItem(s)));
		mod.Add("getRuleArmor", [Bind<Mod>.Self, PtrRef<Armor>(), ScriptParam.Text], GetRuleScript((m, s) => m.GetArmor(s)));
		mod.Add("getRuleSkill", [Bind<Mod>.Self, PtrRef<RuleSkill>(), ScriptParam.Text], GetRuleScript((m, s) => m.GetSkill(s)));
		mod.Add("getRuleResearch", [Bind<Mod>.Self, PtrRef<RuleResearch>(), ScriptParam.Text], GetRuleScript((m, s) => m.GetResearch(s)));
		mod.Add("getRuleSoldier", [Bind<Mod>.Self, PtrRef<RuleSoldier>(), ScriptParam.Text], GetRuleScript((m, s) => m.GetSoldier(s)));
		mod.Add("getRuleInventory", [Bind<Mod>.Self, PtrRef<RuleInventory>(), ScriptParam.Text], GetRuleScript((m, s) => m.GetInventory(s)));
		mod.AddGetPtr("getRuleInventoryRightHand", m => m.InventoryRightHand);
		mod.AddGetPtr("getRuleInventoryLeftHand", m => m.InventoryLeftHand);
		mod.AddGetPtr("getRuleInventoryBackpack", m => m.InventoryBackpack);
		mod.AddGetPtr("getRuleInventoryBelt", m => m.InventoryBelt);
		mod.AddGetPtr("getRuleInventoryGround", m => m.InventoryGround);

		mod.AddScriptValue(m => m._scriptGlobal.ScriptValues);
	}
}
