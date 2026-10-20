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
// Ported from: src/Engine/Unicode.h, src/Engine/Unicode.cpp
// Port status: partial (text tokens and character classes; C# strings are UTF-16, so most of the UTF-8 helpers are not needed)

namespace Acom;

/// <summary>Special characters used in game texts.</summary>
public static class Unicode
{
	/// <summary>Special tag for custom text format.</summary>
	public const char TOK_CUSTOM_FORMAT = (char)27;

	/// <summary>Line break and change to small font.</summary>
	public const char TOK_NL_SMALL = (char)2;

	/// <summary>Alternate between primary and secondary color.</summary>
	public const char TOK_COLOR_FLIP = (char)1;

	/// <summary>Non-breaking space.</summary>
	public const char TOK_NBSP = (char)0xA0;

	public static bool IsLinebreak(char c) => c == '\n' || c == TOK_NL_SMALL;

	public static bool IsSpace(char c) => c == ' ' || c == TOK_NBSP;

	/// <summary>Checks if a character is a word separator (where text can be wrapped).</summary>
	public static bool IsSeparator(char c) => c == '-' || c == '/';

	public static bool IsPrintable(char c) => c > 32 && c != TOK_NBSP;
}
