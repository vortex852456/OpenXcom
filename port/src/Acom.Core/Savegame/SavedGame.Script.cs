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
// Ported from: src/Savegame/SavedGame.h, src/Savegame/SavedGame.cpp (script binding)
#nullable disable

using System.Globalization;
using static Acom.ScriptParam;

namespace Acom;

public partial class SavedGame
{
	private readonly ScriptValues<SavedGame> _scriptValues = new();

	/// <summary>Get script values.</summary>
	public ScriptValues<SavedGame> GetScriptValuesRaw() => _scriptValues;

	/// <summary>C++ <c>getDaysPastEpochScript</c>: <c>std::mktime</c> of the game time in local standard time (tm_isdst 0), in days.</summary>
	private static int DaysPastEpoch(GameTime p)
	{
		var local = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)
			.AddYears(p.Year - 1970).AddMonths(p.Month - 1).AddDays(p.Day - 1)
			.AddHours(p.Hour).AddMinutes(p.Minute).AddSeconds(p.Second);
		long seconds = (long)(local - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)).TotalSeconds;
		seconds -= (long)TimeZoneInfo.Local.BaseUtcOffset.TotalSeconds;
		return (int)(seconds / (60 * 60 * 24));
	}

	private static string DebugDisplayTime(GameTime p)
	{
		if (p != null)
		{
			static string zeroPrefix(int i) => i < 10 ? "0" + i.ToString(CultureInfo.InvariantCulture) : i.ToString(CultureInfo.InvariantCulture);

			return "Time(\"" + p.Year.ToString(CultureInfo.InvariantCulture) + "-" + zeroPrefix(p.Month) + "-" + zeroPrefix(p.Day) + " "
				+ zeroPrefix(p.Hour) + ":" + zeroPrefix(p.Minute) + ":" + zeroPrefix(p.Second) + "\")";
		}
		else
		{
			return "null";
		}
	}

	/// <summary>Register SavedGame in script parser.</summary>
	public static void ScriptRegister(ScriptParserBase parser)
	{
		parser.RegisterPointerType<Country>();

		{
			const string name = "RandomState";
			parser.RegisterRawPointerType<Rng.RandomState>(name);
			var rs = new Bind<Rng.RandomState>(parser, name);
			var self = Bind<Rng.RandomState>.SelfE;

			rs.Add("randomChance", [self, IntRef], (ref ScriptArgs a) =>
			{
				var r = a.Ptr<Rng.RandomState>(0);
				ref int val = ref a.IntRef(1);
				val = r != null ? (r.Generate(0, 99) < val ? 1 : 0) : 0;
			}, "Change value from range 0-100 to 0-1 based on probability");
			rs.Add("randomRange", [self, IntRef, Int, Int], (ref ScriptArgs a) =>
			{
				var r = a.Ptr<Rng.RandomState>(0);
				int min = a.Int(2), max = a.Int(3);
				a.IntRef(1) = r != null && max >= min ? r.Generate(min, max) : 0;
			}, "Return random value from defined range");
			rs.Add("randomRangeSymmetric", [self, IntRef, Int], (ref ScriptArgs a) =>
			{
				var r = a.Ptr<Rng.RandomState>(0);
				int max = a.Int(2);
				a.IntRef(1) = r != null && max >= 0 ? r.Generate(-max, max) : 0;
			}, "Return random value from negative to positive of given max value");

			rs.AddDebugDisplay(p => p != null ? "RandomState(seed: \"" + p.Seed.ToString(CultureInfo.InvariantCulture) + "\")" : "null");
		}

		{
			const string name = "Time";
			parser.RegisterRawPointerType<GameTime>(name);
			var t = new Bind<GameTime>(parser, name);

			t.AddGet("getSecond", p => p.Second);
			t.AddGet("getMinute", p => p.Minute);
			t.AddGet("getHour", p => p.Hour);
			t.AddGet("getDay", p => p.Day);
			t.AddGet("getMonth", p => p.Month);
			t.AddGet("getYear", p => p.Year);
			t.AddGet("getDaysPastEpoch", DaysPastEpoch, "Days past 1970-01-01");
			t.AddGet("getSecondsPastMidnight", p => p.Second + 60 * p.Minute + 60 * 60 * p.Hour, "Seconds past 00:00");

			t.AddDebugDisplay(DebugDisplayTime);
		}

		var sgg = new Bind<SavedGame>(parser);

		sgg.AddGetPtr("getTime", sg => sg.Time, "Get global time that is Greenwich Mean Time");
		sgg.Add("getRandomState", [Bind<SavedGame>.SelfE, PtrERef<Rng.RandomState>()], (ref ScriptArgs a) =>
		{
			var sg = a.Ptr<SavedGame>(0);
			a.SetPtr(1, sg != null ? Rng.GlobalRandomState : null);
		});

		sgg.AddGet("difficultyLevel", sg => (int)sg.Difficulty, "Get difficulty level");
		sgg.AddGet("getMonthsPassed", sg => sg.MonthsPassed, "Number of months passed from start");
		sgg.AddGet("getDaysPassed", sg => sg.DaysPassed, "Number of days passed from start");

		sgg.AddList<Country>("getCountries", Bind<SavedGame>.Self, [], PtrRef<Country>(), sg => sg._countries, (SavedGame sg, Country c, ref ScriptArgs a, int off) => true);
		sgg.AddList<Country>("getCountries", Bind<SavedGame>.SelfE, [], PtrERef<Country>(), sg => sg._countries, (SavedGame sg, Country c, ref ScriptArgs a, int off) => true);

		sgg.Add("isResearched", [Bind<SavedGame>.Self, IntRef, Ptr<RuleResearch>()], (ref ScriptArgs a) =>
		{
			var sg = a.Ptr<SavedGame>(0);
			a.IntRef(1) = sg != null && sg.IsResearched(a.Ptr<RuleResearch>(2), false) ? 1 : 0;
		});

		sgg.AddScriptValue(sg => sg._scriptValues);

		sgg.AddDebugDisplay(p => p != null ? ScriptName + "(fileName: \"" + p.Name + "\" time: " + DebugDisplayTime(p.Time) + ")" : "null");
	}
}
