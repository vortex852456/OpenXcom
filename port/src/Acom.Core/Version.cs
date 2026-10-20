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
// Ported from: src/version.h

namespace Acom;

/// <summary>Version information.</summary>
public static class Version
{
	/// <summary>ACOM's own version.</summary>
	public const string ACOM_VERSION = "0.1-dev";

	// The OXCE version this port is based on. Mods state their requirements against these values,
	// so they stay unchanged to keep mod compatibility checks meaningful.
	public const string OPENXCOM_VERSION_ENGINE = "Extended";
	public const string OPENXCOM_VERSION_SHORT = "Extended 8.5.8";
	public const string OPENXCOM_VERSION_LONG = "8.5.8.0";
	public static readonly int[] OPENXCOM_VERSION_NUMBER = [8, 5, 8, 0];
	public static readonly int[] MIN_REQUIRED_RULESET_VERSION_NUMBER = [8, 5, 1, 0];
}
