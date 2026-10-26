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
// Ported from: src/Savegame/SavedBattleGame.h, src/Savegame/SavedBattleGame.cpp (script binding)
#nullable disable

using System.Globalization;
using static Acom.ScriptParam;

namespace Acom;

public partial class SavedBattleGame
{
	private readonly ScriptValues<SavedBattleGame> _scriptValues = new();

	/// <summary>Get script values.</summary>
	public ScriptValues<SavedBattleGame> GetScriptValuesRaw() => _scriptValues;

	/// <summary>C++ <c>flashMessageVariadicScriptImpl</c> / <c>flashLongMessageVariadicScriptImpl</c>.</summary>
	private static ScriptAction FlashMessageScript(int argCount, bool longMessage) => (ref ScriptArgs a) =>
	{
		var sbg = a.Ptr<SavedBattleGame>(0);
		if (sbg == null || sbg.BattleState == null)
		{
			return;
		}
		var lang = State.GamePtr.Language;
		LocalizedText translated = lang.GetString(a.Str(1));
		for (int i = 0; i < argCount; ++i)
		{
			translated = translated.Arg(a.Int(2 + i));
		}
		if (longMessage)
		{
			sbg.BattleState.WarningLongRaw(translated.ToString());
		}
		else
		{
			sbg.BattleState.WarningRaw(translated.ToString());
		}
	};

	private static bool FilterUnitScript(BattleUnit unit) => unit != null && !unit.IsIgnored && unit.Status != UnitStatus.STATUS_DEAD;

	/// <summary>Register SavedBattleGame in script parser.</summary>
	public static void ScriptRegister(ScriptParserBase parser)
	{
		parser.RegisterPointerType<SavedGame>();
		parser.RegisterPointerType<Tile>();

		var sbg = new Bind<SavedBattleGame>(parser);
		var self = Bind<SavedBattleGame>.Self;
		var selfE = Bind<SavedBattleGame>.SelfE;

		sbg.AddGet("getTurn", s => s.Turn, "Current turn, 0 - before battle, 1 - first turn, each stage reset this value.");
		sbg.AddGet("getAnimFrame", s => s.AnimFrame);
		sbg.AddGet("getSize.getX", s => s.MapSizeX, "Get size in x direction");
		sbg.AddGet("getSize.getY", s => s.MapSizeY, "Get size in y direction");
		sbg.AddGet("getSize.getZ", s => s.MapSizeZ, "Get size in z direction");
		sbg.Add("getTile", [self, PtrRef<Tile>(), Int, Int, Int], (ref ScriptArgs a) =>
		{
			var s = a.Ptr<SavedBattleGame>(0);
			a.SetPtr(1, s != null ? s.GetTile(new Position(a.Int(2), a.Int(3), a.Int(4))) : null);
		}, "Get tile on position x, y, z");
		sbg.Add("getTile", [selfE, PtrERef<Tile>(), Int, Int, Int], (ref ScriptArgs a) =>
		{
			var s = a.Ptr<SavedBattleGame>(0);
			a.SetPtr(1, s != null ? s.GetTile(new Position(a.Int(2), a.Int(3), a.Int(4))) : null);
		}, "Get tile on position x, y, z");
		sbg.AddList<BattleUnit>("getUnits", selfE, [], PtrERef<BattleUnit>(), s => s._units,
			(SavedBattleGame s, BattleUnit unit, ref ScriptArgs a, int off) => FilterUnitScript(unit), "Get list of all units");
		sbg.AddList<BattleUnit>("getUnits.byFaction", selfE, [Int], PtrERef<BattleUnit>(), s => s._units,
			(SavedBattleGame s, BattleUnit unit, ref ScriptArgs a, int off) => FilterUnitScript(unit) && (int)unit.Faction == a.Int(off), "Get list of units from faction");
		sbg.AddList<BattleItem>("getItems", selfE, [], PtrERef<BattleItem>(), s => s._items,
			(SavedBattleGame s, BattleItem item, ref ScriptArgs a, int off) => item != null && !item.IsOwnerIgnored, "Get list of all items");

		sbg.AddGet("getAlienItemLevel", s => s.AlienItemLevel);
		sbg.Add("setAlienItemLevel", [selfE, Int], (ref ScriptArgs a) =>
		{
			var s = a.Ptr<SavedBattleGame>(0);
			if (s != null)
			{
				s.AlienItemLevel = BattleItem.ScriptClamp(a.Int(1), 0, s.Mod.AlienItemLevels.Count);
			}
		});
		sbg.AddGet("getReinforcementsItemLevel", s => s.ReinforcementsItemLevel);
		sbg.Add("setReinforcementsItemLevel", [selfE, Int], (ref ScriptArgs a) =>
		{
			var s = a.Ptr<SavedBattleGame>(0);
			if (s != null)
			{
				s.ReinforcementsItemLevel = BattleItem.ScriptClamp(a.Int(1), 0, s.Mod.AlienItemLevels.Count);
			}
		});

		sbg.AddPair<SavedGame>("getGeoscapeGame", s => s.GeoscapeSave);

		for (int n = 0; n <= 4; ++n)
		{
			ScriptParam[] sig = [selfE, ScriptParam.Text, .. Enumerable.Repeat(Int, n)];
			sbg.Add("flashMessage", sig, FlashMessageScript(n, false));
		}
		for (int n = 0; n <= 4; ++n)
		{
			ScriptParam[] sig = [selfE, ScriptParam.Text, .. Enumerable.Repeat(Int, n)];
			sbg.Add("flashLongMessage", sig, FlashMessageScript(n, true));
		}

		sbg.Add("randomChance", [selfE, IntRef], (ref ScriptArgs a) =>
		{
			var s = a.Ptr<SavedBattleGame>(0);
			ref int val = ref a.IntRef(1);
			val = s != null ? (Rng.Percent(val) ? 1 : 0) : 0;
		}, "first argument is percent in range 0 - 100, then return in that argument random 1 or 0 based on percent");
		sbg.Add("randomRange", [selfE, IntRef, Int, Int], (ref ScriptArgs a) =>
		{
			var s = a.Ptr<SavedBattleGame>(0);
			int min = a.Int(2), max = a.Int(3);
			a.IntRef(1) = s != null && max >= min ? Rng.Generate(min, max) : 0;
		}, "set in first argument random value from range given in two last arguments");

		sbg.AddGet("getTurnSide", s => (int)s.Side, "Return the faction whose turn it is.");
		sbg.AddGet("getDepth", s => s.Depth, "Return the depth of the battlescape.");
		sbg.AddGet("getGlobalShade", s => s.GlobalShade, "Return the global shade of the battlescape.");

		sbg.AddCustomConst("FACTION_PLAYER", (int)UnitFaction.FACTION_PLAYER);
		sbg.AddCustomConst("FACTION_HOSTILE", (int)UnitFaction.FACTION_HOSTILE);
		sbg.AddCustomConst("FACTION_NEUTRAL", (int)UnitFaction.FACTION_NEUTRAL);

		sbg.Add("tryConcealUnit", [selfE, PtrE<BattleUnit>(), IntRef], (ref ScriptArgs a) =>
		{
			var s = a.Ptr<SavedBattleGame>(0);
			var bu = a.Ptr<BattleUnit>(1);
			a.IntRef(2) = s != null && bu != null && s.TileEngine.TryConcealUnit(bu) ? 1 : 0;
		});

		sbg.AddGet("difficultyLevel", s => (int)s.GeoscapeSave.Difficulty);

		sbg.AddScriptValue(s => s._scriptValues);

		sbg.AddDebugDisplay(p => p != null
			? "BattleGame(missionType: \"" + p.MissionType + "\" missionTarget: \"" + p.MissionTarget + "\" turn: " + p.Turn.ToString(CultureInfo.InvariantCulture) + ")"
			: "null");

		sbg.AddCustomConst("DIFF_BEGINNER", (int)GameDifficulty.DIFF_BEGINNER);
		sbg.AddCustomConst("DIFF_EXPERIENCED", (int)GameDifficulty.DIFF_EXPERIENCED);
		sbg.AddCustomConst("DIFF_VETERAN", (int)GameDifficulty.DIFF_VETERAN);
		sbg.AddCustomConst("DIFF_GENIUS", (int)GameDifficulty.DIFF_GENIUS);
		sbg.AddCustomConst("DIFF_SUPERHUMAN", (int)GameDifficulty.DIFF_SUPERHUMAN);
	}

	/// <summary>Register useful function used by graphic scripts.</summary>
	public static void ScriptRegisterUnitAnimations(ScriptParserBase parser)
	{
		var sbg = new Bind<SavedBattleGame>(parser, new BindBase.ExtensionBinding());

		sbg.AddGet("isAltPressed", s => s.IsAltPressed(true));
		sbg.AddGet("isCtrlPressed", s => s.IsCtrlPressed(true));
		sbg.AddGet("isShiftPressed", s => s.IsShiftPressed(true));
		sbg.AddField("getDebugVisionMode", s => s._toggleBrightnessTemp);
		sbg.AddField("isNightVisionEnabled", s => s._toggleNightVisionTemp ? 1 : 0);
		sbg.AddField("isPersonalLightEnabled", s => s._togglePersonalLightTemp ? 1 : 0);
		sbg.AddField("getNightVisionColor", s => s._toggleNightVisionColorTemp);
		sbg.AddGetPtr("getSelectedUnit", s => s.SelectedUnit);
	}
}
