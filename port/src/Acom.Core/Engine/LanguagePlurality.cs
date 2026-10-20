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
// Ported from: src/Engine/LanguagePlurality.h, src/Engine/LanguagePlurality.cpp

namespace Acom.Engine;

/// <summary>
/// Finds plural forms for the different languages.
/// Derived classes implement <see cref="GetSuffix"/> according to the specific language's rules.
/// </summary>
public abstract class LanguagePlurality
{
	private static readonly Dictionary<string, Func<LanguagePlurality>> s_factoryFunctions = new(StringComparer.Ordinal)
	{
		["fr"] = () => new ZeroOneSingular(),
		["fr-CA"] = () => new ZeroOneSingular(),
		["hu"] = () => new NoSingular(),
		["tr"] = () => new NoSingular(),
		["cs"] = () => new CzechPlurality(),
		["pl"] = () => new PolishPlurality(),
		["ro"] = () => new RomanianPlurality(),
		["ru"] = () => new CyrillicPlurality(),
		["sk"] = () => new CzechPlurality(),
		["uk"] = () => new CyrillicPlurality(),
		["ja"] = () => new NoSingular(),
		["ko"] = () => new NoSingular(),
		["zh-CN"] = () => new NoSingular(),
		["zh-TW"] = () => new NoSingular(),
		["hr"] = () => new CroatianPlurality(),
	};

	/// <summary>Gets the dictionary key suffix for value of <paramref name="n"/>.</summary>
	public abstract string GetSuffix(uint n);

	/// <summary>Creates a concrete instance for a given language locale.</summary>
	public static LanguagePlurality Create(string language)
	{
		return s_factoryFunctions.TryGetValue(language, out var creator) ? creator() : new OneSingular();
	}

	/// <summary>Singular only for 1 (English, German, ...).</summary>
	private sealed class OneSingular : LanguagePlurality
	{
		public override string GetSuffix(uint n) => n == 1 ? "_one" : "_other";
	}

	/// <summary>Singular for 0 and 1 (French).</summary>
	private sealed class ZeroOneSingular : LanguagePlurality
	{
		public override string GetSuffix(uint n) => n == 0 || n == 1 ? "_one" : "_other";
	}

	/// <summary>No singular form (Hungarian, Turkish, CJK).</summary>
	private sealed class NoSingular : LanguagePlurality
	{
		public override string GetSuffix(uint n) => "_other";
	}

	/// <summary>Russian, Ukrainian.</summary>
	private sealed class CyrillicPlurality : LanguagePlurality
	{
		public override string GetSuffix(uint n)
		{
			if (n % 10 == 1 && n % 100 != 11)
				return "_one";
			if ((n % 10 >= 2 && n % 10 <= 4) && !(n % 100 >= 12 && n % 100 <= 14))
				return "_few";
			if (n % 10 == 0 || (n % 10 >= 5 && n % 10 <= 9) || (n % 100 >= 11 && n % 100 <= 14))
				return "_many";
			return "_other";
		}
	}

	/// <summary>Czech, Slovak.</summary>
	private sealed class CzechPlurality : LanguagePlurality
	{
		public override string GetSuffix(uint n)
		{
			if (n == 1)
				return "_one";
			if (n >= 2 && n <= 4)
				return "_few";
			return "_other";
		}
	}

	/// <summary>Polish.</summary>
	private sealed class PolishPlurality : LanguagePlurality
	{
		public override string GetSuffix(uint n)
		{
			if (n == 1)
				return "_one";
			if ((n % 10 >= 2 && n % 10 <= 4) && !(n % 100 >= 12 && n % 100 <= 14))
				return "_few";
			if ((n % 10 <= 1) || (n % 10 >= 5 && n % 10 <= 9) || (n % 100 >= 12 && n % 100 <= 14))
				return "_many";
			return "_other";
		}
	}

	/// <summary>Romanian.</summary>
	private sealed class RomanianPlurality : LanguagePlurality
	{
		public override string GetSuffix(uint n)
		{
			if (n == 1)
				return "_one";
			if (n == 0 || (n % 100 >= 1 && n % 100 <= 19))
				return "_few";
			return "_other";
		}
	}

	/// <summary>Croatian.</summary>
	private sealed class CroatianPlurality : LanguagePlurality
	{
		public override string GetSuffix(uint n)
		{
			if (n % 10 == 1 && n % 100 != 11)
				return "_one";
			if ((n % 10 >= 2 && n % 10 <= 4) && !(n % 100 >= 12 && n % 100 <= 14))
				return "_few";
			return "_other";
		}
	}
}
