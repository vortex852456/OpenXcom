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

// Ported from: src/main.cpp, crash handling of src/Engine/CrossPlatform.cpp (crashDump, showError)
// Not ported on purpose: signal/SEH handlers (managed exceptions instead), YAML::setGlobalErrorHandler (Yaml.cs throws),
// the debug self-tests of Collections/fmath (replaced by .NET, see port/tools/port-skip.txt), startUpdateProcess
// (belongs to the OXCE update check, which the port leaves out: docs/architecture/csharp-port.md).
// Steps of C++ main() that need the window (creating Game with the title, State::setGamePtr, setState(StartState),
// run) happen in AcomHost, because MonoGame owns the main loop.

using Acom;
using Acom.Desktop;

// Crash handling routines (C++ exceptionLogger -> CrossPlatform::crashDump), for exceptions of other threads
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
	if (e.ExceptionObject is Exception ex)
		CrossPlatform.CrashDump(ex, "");
	else
		CrossPlatform.CrashDump(null, "Unknown exception");
};

// C++ shows MessageBoxW on Windows; the host uses SDL's message box there
CrossPlatform.ErrorDialogHandler = SdlInput.ShowErrorMessageBox;
CrossPlatform.GetErrorDialog();

try
{
	var options = HostOptions.Parse(args);
	if (!options.Demo)
	{
		// Process the command line and load the options before creating the game
		CrossPlatform.SetArgs(options.GameArgs);
		if (!Options.Init())
			return 0;
	}

	// Deviation: own project name instead of "OpenXcom <version><git>" (see docs/architecture/csharp-port.md)
	string title = $"ACOM {Acom.Version.ACOM_VERSION}";
	using (var host = new AcomHost(options, title))
		host.Run();

	// Comment this for faster exit.
	FileMap.Clear(true, false);
	return 0;
}
#pragma warning disable CA1031 // the crash handler catches everything, like std::set_terminate
catch (Exception e)
#pragma warning restore CA1031
{
	CrossPlatform.CrashDump(e, "");
	return 1; // EXIT_FAILURE
}
