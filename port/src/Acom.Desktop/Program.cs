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

// Ported from: src/main.cpp
// Port status: partial (missing: crash dump/error dialog - unhandled exceptions are only logged; startUpdateProcess is left out with oxceUpdateCheck)
// Not ported on purpose: signal/SEH handlers (managed exceptions instead), YAML::setGlobalErrorHandler (Yaml.cs throws),
// the debug self-tests of Collections/fmath (replaced by .NET, see port/tools/port-skip.txt).
// Steps of C++ main() that need the window (creating Game with the title, State::setGamePtr, setState(StartState),
// run) happen in AcomHost, because MonoGame owns the main loop.

using Acom;
using Acom.Desktop;

// Crash handling routines (C++ exceptionLogger -> CrossPlatform::crashDump)
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
	Logger.Fatal($"A fatal error has occurred: {(e.ExceptionObject as Exception)?.ToString() ?? "Unknown exception"}");

var options = HostOptions.Parse(args);
if (!options.Demo)
{
	// Process the command line and load the options before creating the game
	CrossPlatform.SetArgs(options.GameArgs);
	if (!Options.Init())
		return;
}

// Deviation: own project name instead of "OpenXcom <version><git>" (see docs/architecture/csharp-port.md)
string title = $"ACOM {Acom.Version.ACOM_VERSION}";
using (var host = new AcomHost(options, title))
	host.Run();

// Comment this for faster exit.
FileMap.Clear(true, false);
