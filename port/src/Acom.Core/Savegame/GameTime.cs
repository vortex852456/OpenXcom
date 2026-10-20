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
// Ported from: src/Savegame/GameTime.h, src/Savegame/GameTime.cpp
#nullable disable

using System.Globalization;

namespace Acom;

/// <summary>Enumerator for time periods.</summary>
public enum TimeTrigger : uint
{
	TIME_5SEC = 0,
	TIME_10MIN = 1,
	TIME_30MIN = 2,
	TIME_1HOUR = 3,
	TIME_1DAY = 4,
	TIME_1MONTH = 5,
}

/// <summary>
/// Stores the current ingame time/date according to GMT.
/// Takes care of managing and representing each component,
/// as well as common time operations.
/// </summary>
/// <remarks>A value object in C++: use the copy constructor where C++ copies it.</remarks>
public class GameTime
{
	private static readonly string[] s_weekdays = ["STR_SUNDAY", "STR_MONDAY", "STR_TUESDAY", "STR_WEDNESDAY", "STR_THURSDAY", "STR_FRIDAY", "STR_SATURDAY"];
	private static readonly string[] s_months = ["STR_JAN", "STR_FEB", "STR_MAR", "STR_APR", "STR_MAY", "STR_JUN", "STR_JUL", "STR_AUG", "STR_SEP", "STR_OCT", "STR_NOV", "STR_DEC"];

	private int _second, _minute, _hour, _weekday, _day, _month, _year;

	/// <summary>Initializes a new ingame time with a certain starting point.</summary>
	public GameTime(int weekday, int day, int month, int year, int hour, int minute, int second)
	{
		_second = second;
		_minute = minute;
		_hour = hour;
		_weekday = weekday;
		_day = day;
		_month = month;
		_year = year;
	}

	/// <summary>Copy constructor (C++ copies GameTime by value).</summary>
	public GameTime(GameTime other)
	{
		ArgumentNullException.ThrowIfNull(other);
		_second = other._second;
		_minute = other._minute;
		_hour = other._hour;
		_weekday = other._weekday;
		_day = other._day;
		_month = other._month;
		_year = other._year;
	}

	/// <summary>Loads the time from a YAML file.</summary>
	public void Load(YamlNodeReader reader)
	{
		reader.TryRead("second", ref _second);
		reader.TryRead("minute", ref _minute);
		reader.TryRead("hour", ref _hour);
		reader.TryRead("weekday", ref _weekday);
		reader.TryRead("day", ref _day);
		reader.TryRead("month", ref _month);
		reader.TryRead("year", ref _year);
	}

	/// <summary>Saves the time to a YAML file.</summary>
	public void Save(YamlNodeWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.SetAsMap();
		writer.SetFlowStyle();
		writer.Write("second", _second);
		writer.Write("minute", _minute);
		writer.Write("hour", _hour);
		writer.Write("weekday", _weekday);
		writer.Write("day", _day);
		writer.Write("month", _month);
		writer.Write("year", _year);
	}

	private int[] MonthDays()
	{
		int[] monthDays = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
		// Leap year
		if ((_year % 4 == 0) && !(_year % 100 == 0 && _year % 400 != 0))
			monthDays[1]++;
		return monthDays;
	}

	/// <summary>Checks if it is the last day of the month.</summary>
	public bool IsLastDayOfMonth => _day == MonthDays()[_month - 1];

	/// <summary>
	/// Advances the ingame time by 5 seconds, automatically correcting
	/// the other components when necessary and sending out a trigger when
	/// a certain time span has elapsed for time-dependent events.
	/// </summary>
	/// <returns>Time span trigger.</returns>
	public TimeTrigger Advance()
	{
		TimeTrigger trigger = TimeTrigger.TIME_5SEC;
		int[] monthDays = MonthDays();

		_second += 5;

		if (_second >= 60)
		{
			_minute++;
			_second = 0;
			if (_minute % 10 == 0)
			{
				trigger = TimeTrigger.TIME_10MIN;
			}
			if (_minute % 30 == 0)
			{
				trigger = TimeTrigger.TIME_30MIN;
			}
		}
		if (_minute >= 60)
		{
			_hour++;
			_minute = 0;
			trigger = TimeTrigger.TIME_1HOUR;
		}
		if (_hour >= 24)
		{
			_day++;
			_weekday++;
			_hour = 0;
			trigger = TimeTrigger.TIME_1DAY;
		}
		if (_weekday > 7)
		{
			_weekday = 1;
		}
		if (_day > monthDays[_month - 1])
		{
			_day = 1;
			_month++;
			trigger = TimeTrigger.TIME_1MONTH;
		}
		if (_month > 12)
		{
			_month = 1;
			_year++;
		}

		return trigger;
	}

	/// <summary>Current ingame second (0-59).</summary>
	public int Second => _second;

	/// <summary>Current ingame minute (0-59).</summary>
	public int Minute => _minute;

	/// <summary>Current ingame hour (0-23).</summary>
	public int Hour => _hour;

	/// <summary>Current ingame weekday (1-7), starts on Sunday.</summary>
	public int Weekday => _weekday;

	/// <summary>Localizable-string representation of the current ingame weekday.</summary>
	public string WeekdayString => s_weekdays[_weekday - 1];

	/// <summary>Current ingame day (1-31).</summary>
	public int Day => _day;

	/// <summary>Returns the localized representation of the current ingame day with the cardinal operator.</summary>
	public string GetDayString(Language lang)
	{
		ArgumentNullException.ThrowIfNull(lang);
		string s = _day switch
		{
			1 or 21 or 31 => "STR_DATE_FIRST",
			2 or 22 => "STR_DATE_SECOND",
			3 or 23 => "STR_DATE_THIRD",
			_ => "STR_DATE_FOURTH",
		};
		return lang.GetString(s).Arg(_day);
	}

	/// <summary>Current ingame month (1-12).</summary>
	public int Month => _month;

	/// <summary>Localizable-string representation of the current ingame month.</summary>
	public string MonthString => s_months[_month - 1];

	/// <summary>Current ingame year.</summary>
	public int Year => _year;

	/// <summary>String version of the ingame date and time ("1999-01-01 12:00:00").</summary>
	public string FullString => string.Create(CultureInfo.InvariantCulture, $"{_year}-{_month:D2}-{_day:D2} {_hour:D2}:{_minute:D2}:{_second:D2}");

	/// <summary>
	/// Current position of the daylight emitted on the globe according to the current ingame time,
	/// so the value is 0 when the light starts at 0 longitude (6h) and 1 when the light ends at 0 longitude (18h).
	/// </summary>
	public double Daylight => (double)((((((_hour + 18) % 24) * 60) + _minute) * 60) + _second) / (60 * 60 * 24);
}
