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
// Ported from: src/Engine/Logger.h

namespace Acom;

/// <summary>Severity levels of information logged by the game.</summary>
public enum SeverityLevel
{
	/// <summary>Something horrible has happened and the game is going to die!</summary>
	LOG_FATAL,
	/// <summary>Something bad happened but we can still move on.</summary>
	LOG_ERROR,
	/// <summary>Something weird happened, nothing special but it's good to know.</summary>
	LOG_WARNING,
	/// <summary>Useful information for users/developers to help debug and figure stuff out.</summary>
	LOG_INFO,
	/// <summary>Purely test stuff to help developers implement, not really relevant to users.</summary>
	LOG_DEBUG,
	/// <summary>Extra details that even developers won't really need 90% of the time.</summary>
	LOG_VERBOSE,
	/// <summary>Makes sure everything makes it into log buffer until there's a logfile set up.</summary>
	LOG_UNCENSORED,
}

/// <summary>
/// A basic logging and debugging class. C++ <c>Log(LOG_WARNING) &lt;&lt; "x"</c>
/// becomes <c>Logger.Log(SeverityLevel.LOG_WARNING, "x")</c> or <c>Logger.Warning("x")</c>.
/// </summary>
public static class Logger
{
	private static readonly string[] s_levelNames = ["FATAL", "ERROR", "WARN", "INFO", "DEBUG", "VERB", "ALL"];

	/// <summary>Messages above this level are dropped.</summary>
	public static SeverityLevel ReportingLevel { get; set; } = SeverityLevel.LOG_UNCENSORED;

	/// <summary>
	/// Receives every reported message. Default: <see cref="CrossPlatform.Log"/> (standard error and the log file,
	/// like the C++ Logger destructor); tests can replace it to capture messages.
	/// </summary>
	public static Action<SeverityLevel, string> Sink { get; set; } = CrossPlatform.Log;

	/// <summary>Display name of a level.</summary>
	public static string ToString(SeverityLevel level) => s_levelNames[(int)level];

	/// <summary>Logs a message if the level is reported.</summary>
	public static void Log(SeverityLevel level, string message)
	{
		if (level <= ReportingLevel)
			Sink(level, message);
	}

	public static void Fatal(string message) => Log(SeverityLevel.LOG_FATAL, message);

	public static void Error(string message) => Log(SeverityLevel.LOG_ERROR, message);

	public static void Warning(string message) => Log(SeverityLevel.LOG_WARNING, message);

	public static void Info(string message) => Log(SeverityLevel.LOG_INFO, message);

	public static void Debug(string message) => Log(SeverityLevel.LOG_DEBUG, message);

	public static void Verbose(string message) => Log(SeverityLevel.LOG_VERBOSE, message);
}
