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
// Ported from: src/Engine/LocalizedText.h, src/Engine/LocalizedText.cpp

using System.Globalization;

namespace Acom;

/// <summary>
/// A string that is already translated. Allows argument substitution in the translated strings:
/// <c>tr("STR_X").Arg(5).Arg("abc")</c> replaces <c>{0}</c>, then <c>{1}</c>, ...
/// </summary>
/// <remarks>
/// Immutable, unlike the C++ class; <see cref="Arg(string)"/> follows the C++ non-const overload
/// (used on temporaries): the argument counter advances even if the marker is missing.
/// </remarks>
public sealed class LocalizedText : IEquatable<LocalizedText>
{
	private readonly string _text;
	private readonly int _nextArg;

	/// <summary>Creates from an existing string; the first argument replaces <c>{0}</c>.</summary>
	public LocalizedText(string text) : this(text, 0)
	{
	}

	private LocalizedText(string text, int nextArg)
	{
		_text = text ?? string.Empty;
		_nextArg = nextArg;
	}

	/// <summary>The empty string.</summary>
	public static LocalizedText Empty { get; } = new(string.Empty, 1);

	/// <summary>Replaces all occurrences of the next argument placeholder with <paramref name="val"/>.</summary>
	public LocalizedText Arg(string val)
	{
		ArgumentNullException.ThrowIfNull(val);
		string marker = "{" + _nextArg.ToString(CultureInfo.InvariantCulture) + "}";
		return new LocalizedText(_text.Replace(marker, val, StringComparison.Ordinal), _nextArg + 1);
	}

	public LocalizedText Arg(LocalizedText val) => Arg(val?._text ?? string.Empty);

	public LocalizedText Arg(int val) => Arg(val.ToString(CultureInfo.InvariantCulture));

	public LocalizedText Arg(long val) => Arg(val.ToString(CultureInfo.InvariantCulture));

	public LocalizedText Arg(uint val) => Arg(val.ToString(CultureInfo.InvariantCulture));

	/// <summary>Floating point values are formatted like a default C++ ostream (6 significant digits).</summary>
	public LocalizedText Arg(double val) => Arg(FormatLikeOstream(val));

	public LocalizedText Arg(float val) => Arg(FormatLikeOstream(val));

	/// <summary>Formats like <c>std::ostream &lt;&lt; double</c> (printf "%g").</summary>
	internal static string FormatLikeOstream(double val)
	{
		if (double.IsNaN(val))
			return "nan";
		if (double.IsInfinity(val))
			return val > 0 ? "inf" : "-inf";
		return val.ToString("G6", CultureInfo.InvariantCulture).Replace("E", "e", StringComparison.Ordinal);
	}

	public override string ToString() => _text;

	public static implicit operator string(LocalizedText text) => text?._text ?? string.Empty;

	public bool Equals(LocalizedText? other) => other is not null && other._text == _text;

	public override bool Equals(object? obj) => obj is LocalizedText other && Equals(other);

	public override int GetHashCode() => _text.GetHashCode(StringComparison.Ordinal);
}
