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
// Ported from: src/Mod/Armor.h, src/Mod/Armor.cpp (script binding)
#nullable disable

using static Acom.ScriptParam;

namespace Acom;

public partial class Armor
{
	private ScriptGroupContainer _battleUnitScripts;
	private readonly ScriptValues<Armor> _scriptValues = new();

	/// <summary>Get script of this armor (C++ <c>getScript&lt;ModScript::X&gt;()</c>, index from <see cref="ModScript"/>).</summary>
	public ScriptContainerEvents GetScript(int index) => (ScriptContainerEvents)_battleUnitScripts?.Get(index);

	/// <summary>Get all script values.</summary>
	public ScriptValues<Armor> GetScriptValuesRaw() => _scriptValues;

	/// <summary>Load scripts and script values (C++ part of <c>Armor::load</c>).</summary>
	private void LoadScripts(YamlNodeReader reader, ModScript parsers)
	{
		_battleUnitScripts ??= parsers.battleUnitScripts.CreateContainer();
		_battleUnitScripts.Load(_type, reader, parsers.battleUnitScripts);
	}

	/// <summary>Register Armor in script parser.</summary>
	public static void ScriptRegister(ScriptParserBase parser)
	{
		var ar = new Bind<Armor>(parser);

		ar.AddCustomConst("SIDE_FRONT", (int)UnitSide.SIDE_FRONT);
		ar.AddCustomConst("SIDE_LEFT", (int)UnitSide.SIDE_LEFT);
		ar.AddCustomConst("SIDE_RIGHT", (int)UnitSide.SIDE_RIGHT);
		ar.AddCustomConst("SIDE_REAR", (int)UnitSide.SIDE_REAR);
		ar.AddCustomConst("SIDE_UNDER", (int)UnitSide.SIDE_UNDER);

		ar.Add("getType", [Bind<Armor>.Self, TextRef], (ref ScriptArgs a) =>
		{
			var r = a.Ptr<Armor>(0);
			a.SetText(1, r != null ? new ScriptText(r.Type) : ScriptText.empty);
		});

		ar.AddGetPtr("getCorpseGeoscape", r => r.CorpseGeoscape);
		ar.Add("getCorpseBattlescape", [Bind<Armor>.Self, PtrRef<RuleItem>()], (ref ScriptArgs a) =>
		{
			var r = a.Ptr<Armor>(0);
			a.SetPtr(1, r != null && 0 < r.CorpseBattlescape.Count ? r.CorpseBattlescape[0] : null);
		}, "Get corpse for small unit or main corpse of big unit");
		ar.Add("getCorpseBattlescape", [Bind<Armor>.Self, PtrRef<RuleItem>(), Int], (ref ScriptArgs a) =>
		{
			var r = a.Ptr<Armor>(0);
			int part = a.Int(2);
			a.SetPtr(1, r != null && 0 <= part && part < r.CorpseBattlescape.Count ? r.CorpseBattlescape[part] : null);
		}, "Get corpses for big unit");

		ar.AddGetPtr("getRequiredResearch", r => r.RequiredResearch);

		ar.AddGet("getDrawingRoutine", r => r.DrawingRoutine);
		ar.AddGet("getDrawBubbles", r => r.DrawBubbles());
		ar.AddGet("getDeathFrames", r => r.DeathFrames);
		ar.AddGet("getConstantAnimation", r => r.ConstantAnimation);

		ar.AddGet("getVisibilityAtDark", r => r.VisibilityAtDark);
		ar.AddGet("getVisibilityAtDay", r => r.VisibilityAtDay);
		ar.AddGet("getCamouflageAtDark", r => r.CamouflageAtDark);
		ar.AddGet("getCamouflageAtDay", r => r.CamouflageAtDay);
		ar.AddGet("getAntiCamouflageAtDark", r => r.AntiCamouflageAtDark);
		ar.AddGet("getAntiCamouflageAtDay", r => r.AntiCamouflageAtDay);
		ar.AddGet("getHeatVision", r => r.VisibilityThroughSmoke, "getVisibilityThroughSmoke");
		ar.AddGet("getVisibilityThroughFire", r => r.VisibilityThroughFire, "getVisibilityThroughFire");
		ar.AddGet("getPersonalLight", r => r.PersonalLightFriend);
		ar.AddGet("getPersonalLightHostile", r => r.PersonalLightHostile);
		ar.AddGet("getPersonalLightNeutral", r => r.PersonalLightNeutral);
		ar.AddGet("getSize", r => r.Size);

		UnitStatsScript.AddGetStatsScript(ar, r => ref r._stats, "Stats.");

		ar.Add("getArmor", [Bind<Armor>.Self, IntRef, Int], (ref ScriptArgs a) =>
		{
			var r = a.Ptr<Armor>(0);
			int side = a.Int(2);
			a.IntRef(1) = r != null && 0 <= side && side < (int)UnitSide.SIDE_MAX ? r.GetArmor((UnitSide)side) : 0;
		});

		ar.AddField("MoveCost.getBaseTimePercent", r => r._moveCostBase.TimePercent);
		ar.AddField("MoveCost.getBaseEnergyPercent", r => r._moveCostBase.EnergyPercent);
		ar.AddField("MoveCost.getBaseNormalTimePercent", r => r._moveCostBaseNormal.TimePercent);
		ar.AddField("MoveCost.getBaseNormalEnergyPercent", r => r._moveCostBaseNormal.EnergyPercent);
		ar.AddField("MoveCost.getBaseFlyTimePercent", r => r._moveCostBaseFly.TimePercent);
		ar.AddField("MoveCost.getBaseFlyEnergyPercent", r => r._moveCostBaseFly.EnergyPercent);

		ar.AddScriptValue(r => r._scriptValues, false);
		ar.AddDebugDisplay(r => r != null ? ScriptName + "(name: \"" + r.Type + "\")" : "null");
	}
}
