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
// Ported from: src/Ufopaedia/StatsForNerdsState.h, src/Ufopaedia/StatsForNerdsState.cpp (script binding)
#nullable disable

using System.Globalization;
using static Acom.ScriptParam;

namespace Acom;

public partial class StatsForNerdsState
{
	/// <summary>Register object in script parser.</summary>
	public static void ScriptRegister(ScriptParserBase parser)
	{
		parser.RegisterPointerType<Mod>();
		parser.RegisterPointerType<SavedGame>();

		var b = new Bind<StatsForNerdsState>(parser);
		var selfE = Bind<StatsForNerdsState>.SelfE;

		b.Add("addIntRow", [selfE, ScriptParam.Text, Int], (ref ScriptArgs a) => { a.Ptr<StatsForNerdsState>(0)?.AddIntegerScript(a.Str(1), a.Int(2)); }, "add new row with first argument as name and second argument as value");
		b.Add("addTextRow", [selfE, ScriptParam.Text, ScriptParam.Text], (ref ScriptArgs a) => { a.Ptr<StatsForNerdsState>(0)?.AddTextScript(a.Str(1), a.Str(2)); }, "add new row with first argument as name and second argument as text value to translate");
		b.Add("addTextFormatRow", [selfE, ScriptParam.Text, ScriptParam.Text, Int], (ref ScriptArgs a) => { a.Ptr<StatsForNerdsState>(0)?.AddTextFormat1Script(a.Str(1), a.Str(2), a.Int(3)); }, "add new row with first argument as name and second argument as text format with one argument");
		b.Add("addTextFormatRow", [selfE, ScriptParam.Text, ScriptParam.Text, Int, Int], (ref ScriptArgs a) => { a.Ptr<StatsForNerdsState>(0)?.AddTextFormat2Script(a.Str(1), a.Str(2), a.Int(3), a.Int(4)); }, "add new row with first argument as name and second argument as text format with two arguments");

		b.AddDebugDisplay(sfn => sfn != null ? ScriptName + "(typeId: " + ((int)sfn.TypeId).ToString(CultureInfo.InvariantCulture) + " topicId: \"" + sfn.TopicId + "\")" : "null");
	}
}
