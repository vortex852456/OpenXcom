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
// C# strings are UTF-16, so the UTF-8 helpers map as follows: convUtf8ToUtf32/convUtf32ToUtf8/convWcToMb/convMbToWc and
// getUtf8Locale are not needed (text is already decoded; Font/Text work on chars), isValidUTF8 is done by the decoders
// (Encoding.UTF8, ZipArchive entry names), codePointLengthUTF8/codePointSubstrUTF8 count UTF-16 code points,
// replace is string.Replace. caseCompare/caseFind have no callers in OXCE and are not ported.

namespace Acom;

/// <summary>Special characters used in game texts.</summary>
public static class Unicode
{
	/// <summary>Currency symbol used by formatFunding, set by the ruleset constant "extendedCurrencySymbol".</summary>
#pragma warning disable CA2211 // Global set by Mod.LoadConstants, as in the C++ code.
	public static string OXCE_CURRENCY_SYMBOL = "$";
#pragma warning restore CA2211

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

	/// <summary>
	/// Compares two strings in natural order (ignoring case, runs of digits compared by value), like
	/// <c>naturalCompare</c> does on Windows (<c>StrCmpLogicalW</c>). The C++ fallback on other systems is a
	/// locale-dependent compare; the port uses the Windows order everywhere so that numbered files
	/// ("2.png" before "10.png") load in the same order on all systems.
	/// </summary>
	/// <returns>Negative if <paramref name="a"/> comes first, positive if <paramref name="b"/> does, 0 if equal.</returns>
	public static int NaturalComparison(string a, string b)
	{
		int i = 0, j = 0;
		while (i < a.Length && j < b.Length)
		{
			if (char.IsAsciiDigit(a[i]) && char.IsAsciiDigit(b[j]))
			{
				int si = i, sj = j;
				while (si < a.Length && a[si] == '0')
					++si;
				while (sj < b.Length && b[sj] == '0')
					++sj;
				int ei = si, ej = sj;
				while (ei < a.Length && char.IsAsciiDigit(a[ei]))
					++ei;
				while (ej < b.Length && char.IsAsciiDigit(b[ej]))
					++ej;
				if (ei - si != ej - sj)
					return ei - si < ej - sj ? -1 : 1;
				int c = string.CompareOrdinal(a, si, b, sj, ei - si);
				if (c != 0)
					return c;
				i = ei;
				j = ej;
				continue;
			}
			int d = char.ToUpperInvariant(a[i]).CompareTo(char.ToUpperInvariant(b[j]));
			if (d != 0)
				return d;
			++i;
			++j;
		}
		int r = (a.Length - i).CompareTo(b.Length - j);
		if (r != 0)
			return r;
		// equal in natural order (e.g. "a01" and "a1"): keep a deterministic total order
		r = string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
		return r != 0 ? r : string.CompareOrdinal(a, b);
	}

	/// <summary>Compares two strings in natural order: true if <paramref name="a"/> comes before <paramref name="b"/> (C++ naturalCompare).</summary>
	public static bool NaturalCompare(string a, string b) => NaturalComparison(a, b) < 0;

	/// <summary>Converts a string to upper case (C++ upperCase works in place; here a new string is returned).</summary>
	public static string UpperCase(string s) => string.IsNullOrEmpty(s) ? s : s.ToUpperInvariant();

	/// <summary>Converts a string to lower case (C++ lowerCase works in place; here a new string is returned).</summary>
	public static string LowerCase(string s) => string.IsNullOrEmpty(s) ? s : s.ToLowerInvariant();

	/// <summary>Number of Unicode code points (C++ codePointLengthUTF8); a lone surrogate counts as one.</summary>
	public static int CodePointLength(string str)
	{
		ArgumentNullException.ThrowIfNull(str);
		int size = 0;
		for (int i = 0; i < str.Length; ++i)
		{
			if (char.IsHighSurrogate(str[i]) && i + 1 < str.Length && char.IsLowSurrogate(str[i + 1]))
				++i;
			++size;
		}
		return size;
	}

	/// <summary>Substring by code points (C++ codePointSubstrUTF8); <paramref name="count"/> may run past the end.</summary>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="pos"/> is past the end (C++ substr throws out_of_range).</exception>
	public static string CodePointSubstr(string str, long pos, long count = long.MaxValue)
	{
		ArgumentNullException.ThrowIfNull(str);
		int begin = -1;
		int i = 0;
		for (long curr = 0; ; ++curr)
		{
			if (begin < 0 && curr == pos)
				begin = i;
			if (begin >= 0 && count-- == 0)
				break;
			if (i >= str.Length)
				break;
			if (char.IsHighSurrogate(str[i]) && i + 1 < str.Length && char.IsLowSurrogate(str[i + 1]))
				++i;
			++i;
		}
		if (begin < 0)
			throw new ArgumentOutOfRangeException(nameof(pos), "Invalid code point position for substr");
		return str[begin..i];
	}

	/// <summary>Takes an integer value and formats it as number with separators (spacing the thousands).</summary>
	/// <param name="value">The value.</param>
	/// <param name="currency">Currency symbol (prefix if one byte in UTF-8, otherwise suffix).</param>
	public static string FormatNumber(long value, string currency = "")
	{
		const string thousands_sep = "\u00A0"; // TOK_NBSP

		bool negative = value < 0;
		var s = new System.Text.StringBuilder(unchecked((ulong)(negative ? -value : value)).ToString(System.Globalization.CultureInfo.InvariantCulture));
		int spacer = s.Length - 3;
		while (spacer > 0 && spacer < s.Length)
		{
			s.Insert(spacer, thousands_sep);
			spacer -= 3;
		}
		if (!string.IsNullOrEmpty(currency))
		{
			if (System.Text.Encoding.UTF8.GetByteCount(currency) > 1) // Note: size in bytes, not characters!
				s.Append(currency);
			else
				s.Insert(0, currency);
		}
		if (negative)
			s.Insert(0, '-');
		return s.ToString();
	}

	/// <summary>Takes an integer value and formats it as currency, spacing the thousands and adding a $ sign to the front.</summary>
	public static string FormatFunding(long funds) => FormatNumber(funds, OXCE_CURRENCY_SYMBOL);

	/// <summary>Takes an integer value and formats it as percentage, adding a % sign.</summary>
	public static string FormatPercentage(int value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "%";
}
