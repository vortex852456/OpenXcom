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
// Ported from: src/Engine/Script.h, src/Engine/Script.cpp
// Tokenizer, built-in operations, parsing of operations and the parser classes.

#pragma warning disable CA1711 // C++ names (ArgEnum, RegEnum, ScriptImpl, ...) are kept.
#pragma warning disable CA1715 // C++ template parameter names are kept.
#pragma warning disable CA1720 // script type names (Int, Ptr) are kept.
#pragma warning disable CA1822 // members mirror C++ instance members.
#pragma warning disable CA1815 // value types mirror C++ PODs, equality is not used.
#pragma warning disable CA1034 // nested types mirror C++ nested types.

using System.Globalization;
using System.Numerics;
using System.Text;
using static Acom.ScriptArg;

namespace Acom;

////////////////////////////////////////////////////////////
//						proc definition
////////////////////////////////////////////////////////////

/// <summary>Built-in operations of the script engine (C++ <c>MACRO_PROC_DEFINITION</c> and helper functions).</summary>
public static class ScriptBuiltin
{
	public static void addShade_h(ref int reg, int var)
	{
		int newShade = (reg & 0xF) + var;
		if (newShade > 0xF)
		{
			// so dark it would flip over to another color - make it black instead
			reg = 0xF;
			return;
		}
		else if (newShade > 0)
		{
			reg = (reg & 0xF0) | newShade;
			return;
		}
		reg &= 0xF0;
		//prevent overflow to 0 or another color - make it white instead
		if (reg == 0 || newShade < 0)
			reg = 0x01;
	}

	public static RetEnum mulAddMod_h(ref int reg, int mul, int add, int mod)
	{
		long a = ((long)reg) * mul + add;
		if (mod != 0)
		{
			reg = unchecked((int)((a % mod + mod) % mod));
			return RetContinue;
		}
		return RetError;
	}

	public static RetEnum mulDiv_h(ref int reg, int mul, int div)
	{
		if (div != 0)
		{
			reg = unchecked((int)((((long)reg) * mul) / div));
			return RetContinue;
		}
		return RetError;
	}

	public static RetEnum wavegen_rect_h(ref int reg, int period, int size, int max)
	{
		if (period <= 0)
			return RetError;
		reg %= period;
		if (reg < 0)
			reg += period;
		if (reg > size)
			reg = 0;
		else
			reg = max;
		return RetContinue;
	}

	public static RetEnum wavegen_saw_h(ref int reg, int period, int size, int max)
	{
		if (period <= 0)
			return RetError;
		reg %= period;
		if (reg < 0)
			reg += period;
		if (reg > size)
			reg = 0;
		else if (reg > max)
			reg = max;
		return RetContinue;
	}

	public static RetEnum wavegen_tri_h(ref int reg, int period, int size, int max)
	{
		if (period <= 0)
			return RetError;
		reg %= period;
		if (reg < 0)
			reg += period;
		if (reg > size)
			reg = 0;
		else
		{
			if (reg > size / 2)
				reg = size - reg;
			if (reg > max)
				reg = max;
		}
		return RetContinue;
	}

	public static RetEnum wavegen_sin_h(ref int reg, int period, int size)
	{
		if (period <= 0)
			return RetError;
		reg = CppDoubleToInt(size * Math.Sin(2.0 * Math.PI * reg / period));
		return RetContinue;
	}

	public static RetEnum wavegen_cos_h(ref int reg, int period, int size)
	{
		if (period <= 0)
			return RetError;
		reg = CppDoubleToInt(size * Math.Cos(2.0 * Math.PI * reg / period));
		return RetContinue;
	}

	public static RetEnum bit_popcount_h(ref int reg)
	{
		reg = BitOperations.PopCount(unchecked((uint)reg));
		return RetContinue;
	}

	/// <summary>C++ <c>(int)double</c> like x86 <c>cvttsd2si</c>: out of range and NaN give INT_MIN.</summary>
	public static int CppDoubleToInt(double d)
	{
		if (double.IsNaN(d) || d >= 2147483648.0 || d <= -2147483649.0)
		{
			return int.MinValue;
		}
		return (int)d;
	}

	/// <summary>C++ <c>int /= int</c>; INT_MIN / -1 traps in C++, the port wraps.</summary>
	private static int Div(int a, int b) => b == -1 ? unchecked(-a) : a / b;

	/// <summary>C++ <c>int %= int</c>; INT_MIN % -1 traps in C++, the port gives 0.</summary>
	private static int Mod(int a, int b) => b == -1 ? 0 : a % b;

	private static readonly ScriptParam C = ScriptParam.Context;
	private static readonly ScriptParam P = ScriptParam.Prog;
	private static readonly ScriptParam L = ScriptParam.Label;
	private static readonly ScriptParam I = ScriptParam.Int;
	private static readonly ScriptParam R = ScriptParam.IntRef;

	/// <summary><c>exit</c> operation.</summary>
	internal static readonly ScriptImpl ExitImpl = (ref ScriptArgs a) => RetEnd;
	/// <summary><c>goto</c> operation.</summary>
	internal static readonly ScriptImpl GotoImpl = (ref ScriptArgs a) =>
	{
		a.Prog = a.Label(2);
		return RetContinue;
	};

	/// <summary>One built-in operation.</summary>
	internal readonly record struct Builtin(string name, string description, ScriptFuncGroup group);

	private static Builtin B(string name, ScriptParam[] p, ScriptImpl impl, string desc) => new(name, desc, new ScriptFuncGroup(p, impl));

	/// <summary>Main list defining all available operation in script engine (C++ <c>MACRO_PROC_DEFINITION</c>).</summary>
	internal static readonly Builtin[] Procs =
	[
		B("exit", [C], ExitImpl, ""),

		B("goto", [C, P, L], GotoImpl, ""),

		B("set", [C, R, I], (ref ScriptArgs a) => { a.IntRef(1) = a.Int(2); return RetContinue; }, "arg1 = arg2"),

		B("clear", [C, R], (ref ScriptArgs a) => { a.IntRef(1) = 0; return RetContinue; }, "arg1 = 0"),

		B("test_le", [P, I, I, L, L], (ref ScriptArgs a) => { a.Prog = (a.Int(1) <= a.Int(2)) ? a.Label(3) : a.Label(4); return RetContinue; }, ""),
		B("test_eq", [P, I, I, L, L], (ref ScriptArgs a) => { a.Prog = (a.Int(1) == a.Int(2)) ? a.Label(3) : a.Label(4); return RetContinue; }, ""),

		B("swap", [R, R], (ref ScriptArgs a) => { (a.IntRef(0), a.IntRef(1)) = (a.IntRef(1), a.IntRef(0)); return RetContinue; }, "Swap value of arg1 and arg2"),
		B("add", [R, I], (ref ScriptArgs a) => { unchecked { a.IntRef(0) += a.Int(1); } return RetContinue; }, "arg1 = arg1 + arg2"),
		B("sub", [R, I], (ref ScriptArgs a) => { unchecked { a.IntRef(0) -= a.Int(1); } return RetContinue; }, "arg1 = arg1 - arg2"),
		B("mul", [R, I], (ref ScriptArgs a) => { unchecked { a.IntRef(0) *= a.Int(1); } return RetContinue; }, "arg1 = arg1 * arg2"),

		B("aggregate", [R, I, I], (ref ScriptArgs a) => { ref int r = ref a.IntRef(0); r = unchecked(r + a.Int(1) * a.Int(2)); return RetContinue; }, "arg1 = arg1 + (arg2 * arg3)"),
		B("offset", [R, I, I], (ref ScriptArgs a) => { ref int r = ref a.IntRef(0); r = unchecked(r * a.Int(1) + a.Int(2)); return RetContinue; }, "arg1 = (arg1 * arg2) + arg3"),
		B("offsetmod", [R, I, I, I], (ref ScriptArgs a) => mulAddMod_h(ref a.IntRef(0), a.Int(1), a.Int(2), a.Int(3)), "arg1 = ((arg1 * arg2) + arg3) % arg4"),

		B("div", [R, I], (ref ScriptArgs a) => { int d = a.Int(1); if (d == 0) return RetError; ref int r = ref a.IntRef(0); r = Div(r, d); return RetContinue; }, "arg1 = arg1 / arg2"),
		B("mod", [R, I], (ref ScriptArgs a) => { int d = a.Int(1); if (d == 0) return RetError; ref int r = ref a.IntRef(0); r = Mod(r, d); return RetContinue; }, "arg1 = arg1 % arg2"),
		B("muldiv", [R, I, I], (ref ScriptArgs a) => mulDiv_h(ref a.IntRef(0), a.Int(1), a.Int(2)), "arg1 = (arg1 * arg2) / arg3"),

		B("shl", [R, I], (ref ScriptArgs a) => { a.IntRef(0) <<= a.Int(1); return RetContinue; }, "Left bit shift of arg1 by arg2"),
		B("shr", [R, I], (ref ScriptArgs a) => { a.IntRef(0) >>= a.Int(1); return RetContinue; }, "Right bit shift of arg1 by arg2"),

		B("bit_and", [R, I], (ref ScriptArgs a) => { ref int r = ref a.IntRef(0); r &= a.Int(1); return RetContinue; }, "Bit And of arg1 and arg2"),
		B("bit_or", [R, I], (ref ScriptArgs a) => { ref int r = ref a.IntRef(0); r |= a.Int(1); return RetContinue; }, "Bit Or of arg1 and arg2"),
		B("bit_xor", [R, I], (ref ScriptArgs a) => { ref int r = ref a.IntRef(0); r ^= a.Int(1); return RetContinue; }, "Bit Xor of arg1 and arg2"),
		B("bit_not", [R], (ref ScriptArgs a) => { ref int r = ref a.IntRef(0); r = ~r; return RetContinue; }, "Bit Not of arg1"),
		B("bit_count", [R], (ref ScriptArgs a) => bit_popcount_h(ref a.IntRef(0)), "Count number of set bits of arg1"),

		B("pow", [R, I], (ref ScriptArgs a) => { ref int r = ref a.IntRef(0); r = CppDoubleToInt(Math.Pow(r, Math.Max(0, a.Int(1)))); return RetContinue; }, "Power of arg1 to arg2"),
		B("sqrt", [R], (ref ScriptArgs a) => { ref int r = ref a.IntRef(0); r = r > 0 ? (int)Math.Sqrt(r) : 0; return RetContinue; }, "Square root of arg1"),

		B("abs", [R], (ref ScriptArgs a) => { ref int r = ref a.IntRef(0); r = r < 0 ? unchecked(-r) : r; return RetContinue; }, "Absolute value of arg1"),
		B("limit", [R, I, I], (ref ScriptArgs a) => { ref int r = ref a.IntRef(0); r = Math.Max(Math.Min(r, a.Int(2)), a.Int(1)); return RetContinue; }, "Correct value in arg1 that is always between arg2 and arg3"),
		B("limit_upper", [R, I], (ref ScriptArgs a) => { ref int r = ref a.IntRef(0); r = Math.Min(r, a.Int(1)); return RetContinue; }, "Correct value in arg1 that is always lesser than arg2"),
		B("limit_lower", [R, I], (ref ScriptArgs a) => { ref int r = ref a.IntRef(0); r = Math.Max(r, a.Int(1)); return RetContinue; }, "Correct value in arg1 that is always greater than arg2"),

		B("wavegen_rect", [R, I, I, I], (ref ScriptArgs a) => wavegen_rect_h(ref a.IntRef(0), a.Int(1), a.Int(2), a.Int(3)), "Square wave function, arg1 - argument and result, arg2 - period, arg3 - length of square, arg4 - height of square"),
		B("wavegen_saw", [R, I, I, I], (ref ScriptArgs a) => wavegen_saw_h(ref a.IntRef(0), a.Int(1), a.Int(2), a.Int(3)), "Saw wave function, arg1 - argument and result, arg2 - period, arg3 - size of saw, arg4 - cap value"),
		B("wavegen_tri", [R, I, I, I], (ref ScriptArgs a) => wavegen_tri_h(ref a.IntRef(0), a.Int(1), a.Int(2), a.Int(3)), "Triangle wave function, arg1 - argument and result, arg2 - period, arg3 - size of triangle, arg4 - cap value"),
		B("wavegen_sin", [R, I, I], (ref ScriptArgs a) => wavegen_sin_h(ref a.IntRef(0), a.Int(1), a.Int(2)), "Sin wave function, arg1 - argument and result, arg2 - period, arg3 - size of amplitude"),
		B("wavegen_cos", [R, I, I], (ref ScriptArgs a) => wavegen_cos_h(ref a.IntRef(0), a.Int(1), a.Int(2)), "Cos wave function, arg1 - argument and result, arg2 - period, arg3 - size of amplitude"),

		B("get_color", [R, I], (ref ScriptArgs a) => { a.IntRef(0) = a.Int(1) >> 4; return RetContinue; }, "Get color part to arg1 of pixel color in arg2"),
		B("set_color", [R, I], (ref ScriptArgs a) => { ref int r = ref a.IntRef(0); r = (r & 0xF) | (a.Int(1) << 4); return RetContinue; }, "Set color part to pixel color in arg1"),
		B("get_shade", [R, I], (ref ScriptArgs a) => { a.IntRef(0) = a.Int(1) & 0xF; return RetContinue; }, "Get shade part to arg1 of pixel color in arg2"),
		B("set_shade", [R, I], (ref ScriptArgs a) => { ref int r = ref a.IntRef(0); r = (r & 0xF0) | (a.Int(1) & 0xF); return RetContinue; }, "Set color part to pixel color in arg1"),
		B("add_shade", [R, I], (ref ScriptArgs a) => { addShade_h(ref a.IntRef(0), a.Int(1)); return RetContinue; }, "Add value of shade to pixel color in arg1"),

		B("call", [ScriptParam.Func, ScriptParam.Raw, C, P], (ref ScriptArgs a) => RetError, ""),
	];

	/// <summary>Number of C++ versions of an operation (product of alternatives).</summary>
	internal static int Ver(ScriptFuncGroup g)
	{
		int v = 1;
		foreach (var p in g.Params)
		{
			if (!p.IsInternal)
			{
				v *= p.Alternatives.Length;
			}
		}
		return v;
	}

	internal static readonly ScriptFuncGroup test_eq_null = new([P, ScriptParam.Null, ScriptParam.Null, L, L], (ref ScriptArgs a) =>
	{
		a.Prog = a.Label(3);
		return RetContinue;
	});

	internal static readonly ScriptFuncGroup debug_impl_int = new([C, I], (ref ScriptArgs a) =>
	{
		int i = a.Int(1);
		a.Worker.log_buffer_add(() => i.ToString(CultureInfo.InvariantCulture));
		return RetContinue;
	});

	internal static readonly ScriptFuncGroup debug_impl_text = new([C, ScriptParam.Text], (ref ScriptArgs a) =>
	{
		var p = a.Text(1);
		a.Worker.log_buffer_add(() => p.ToString());
		return RetContinue;
	});

	internal static readonly ScriptFuncGroup debug_flush = new([C, P], (ref ScriptArgs a) =>
	{
		a.Worker.log_buffer_flush();
		return RetContinue;
	});

	internal static readonly ScriptFuncGroup set_text = new([C, ScriptParam.TextRef, ScriptParam.Text], (ref ScriptArgs a) =>
	{
		a.SetText(1, a.Text(2));
		return RetContinue;
	});

	internal static readonly ScriptFuncGroup clear_text = new([C, ScriptParam.TextRef], (ref ScriptArgs a) =>
	{
		a.SetText(1, ScriptText.empty);
		return RetContinue;
	});

	internal static readonly ScriptFuncGroup test_eq_text = new([P, ScriptParam.Text, ScriptParam.Text, L, L], (ref ScriptArgs a) =>
	{
		var x = a.Text(1);
		var y = a.Text(2);
		if (x.ptr == null && y.ptr == null)
		{
			a.Prog = a.Label(3);
		}
		else if (x.ptr == null || y.ptr == null)
		{
			a.Prog = a.Label(4);
		}
		else if (string.Equals(x.ptr, y.ptr, StringComparison.Ordinal))
		{
			a.Prog = a.Label(3);
		}
		else
		{
			a.Prog = a.Label(4);
		}
		return RetContinue;
	});
}

////////////////////////////////////////////////////////////
//				ParserWriter helpers
////////////////////////////////////////////////////////////

/// <summary>Token type.</summary>
public enum TokenEnum
{
	TokenNone,
	TokenInvalid,
	TokenColon,
	TokenSemicolon,
	TokenSymbol,
	TokenNumber,
	TokenText,
}

/// <summary>Struct represents position of token in input string.</summary>
public readonly struct SelectedToken
{
	public readonly ScriptRef range;
	/// <summary>type of this token.</summary>
	private readonly TokenEnum _type;
	/// <summary>line where token start.</summary>
	private readonly int _linePos;

	/// <summary>Constructor from range.</summary>
	public SelectedToken(TokenEnum type, ScriptRef r, int linePos)
	{
		range = r;
		_type = type;
		_linePos = linePos;
	}

	/// <summary>Get token type.</summary>
	public TokenEnum Type => _type;

	/// <summary>Get token line postion in script.</summary>
	public int LinePos => _linePos;

	public bool IsValid => range.IsValid;

	public override string ToString() => range.ToString();

	/// <summary>Convert token to script ref.</summary>
	public ScriptRefData Parse(ParserWriter ph)
	{
		if (Type == TokenEnum.TokenNumber)
		{
			var str = range.AsSpan();
			if (str.Length > 0 && str[0] == '+')
				str = str[1..];
			if (C4FromChars(str, out int val))
				return new ScriptRefData(range, ArgInt, val);
		}
		else if (Type == TokenEnum.TokenSymbol)
		{
			var r = ph.GetReferece(range);
			if (r.IsValid)
				return r;

			var type = ArgUnknowSimple;

			foreach (var c in range.AsSpan())
			{
				if (c == '.')
				{
					// look like `abc.def`
					type = (ArgEnum)Math.Max((int)type, (int)ArgUnknowSegment);
				}
			}

			return new ScriptRefData(range, type);
		}
		else if (Type == TokenEnum.TokenText)
		{
			return new ScriptRefData(range, ArgText, ScriptValueData.FromRef(range));
		}
		return new ScriptRefData(range, ArgInvalid);
	}

	/// <summary>c4core <c>from_chars(csubstr, int*)</c>: decimal, <c>0x</c>, <c>0b</c>, <c>0o</c>, optional <c>-</c>, wraps on overflow.</summary>
	internal static bool C4FromChars(ReadOnlySpan<char> str, out int v)
	{
		v = 0;
		if (str.Length == 0)
			return false;

		int sign = 1;
		int start = 0;
		if (str[0] == '-')
		{
			if (str.Length == ++start)
				return false;
			sign = -1;
		}

		bool parsed_ok;
		if (str[start] != '0')
		{
			parsed_ok = ReadBase(str[start..], 10, out v);
		}
		else if (str.Length > start + 1)
		{
			char pfx = str[start + 1];
			if (pfx is 'x' or 'X')
				parsed_ok = str.Length > start + 2 && ReadBase(str[(start + 2)..], 16, out v);
			else if (pfx is 'b' or 'B')
				parsed_ok = str.Length > start + 2 && ReadBase(str[(start + 2)..], 2, out v);
			else if (pfx is 'o' or 'O')
				parsed_ok = str.Length > start + 2 && ReadBase(str[(start + 2)..], 8, out v);
			else
				parsed_ok = ReadBase(str[(start + 1)..], 10, out v);
		}
		else
		{
			parsed_ok = ReadBase(str[start..], 10, out v);
		}
		if (parsed_ok)
			v = unchecked(v * sign);
		return parsed_ok;
	}

	private static bool ReadBase(ReadOnlySpan<char> s, int b, out int v)
	{
		v = 0;
		foreach (var c in s)
		{
			int cv;
			if (c >= '0' && c <= '9')
				cv = c - '0';
			else if (b == 16 && c >= 'a' && c <= 'f')
				cv = 10 + (c - 'a');
			else if (b == 16 && c >= 'A' && c <= 'F')
				cv = 10 + (c - 'A');
			else
				return false;
			if (cv >= b)
				return false;
			v = unchecked(v * b + cv);
		}
		return true;
	}
}

/// <summary>Script source that is split into tokens.</summary>
public sealed class ScriptRefTokens
{
	private readonly string _s;
	private int _begin;
	private readonly int _end;
	/// <summary>Current line position.</summary>
	private int _linePos = 1;

	public ScriptRefTokens(string s)
	{
		_s = s;
		_begin = 0;
		_end = s.Length;
	}

	/// <summary>Current position.</summary>
	public int Begin => _begin;
	/// <summary>End position.</summary>
	public int End => _end;
	/// <summary>Source.</summary>
	public string Source => _s;
	/// <summary>Have any chars left.</summary>
	public bool IsValid => _begin != _end;

	//groups of different types of ASCII characters
	private const byte CC_none = 0x1;
	private const byte CC_spec = 0x2;
	private const byte CC_digit = 0x4;
	private const byte CC_digitHex = 0x8;
	private const byte CC_charRest = 0x10;
	private const byte CC_digitSign = 0x20;
	private const byte CC_digitPrefix = 0x40;
	private const byte CC_quote = 0x80;

	private static readonly byte[] charDecoder = CreateDecoder();

	private static byte[] CreateDecoder()
	{
		var r = new byte[256];
		for (int i = 0; i < 256; ++i)
		{
			if (i == '#' || i == ' ' || i == '\r' || i == '\n' || i == '\t') r[i] |= CC_none;
			if (i == ':' || i == ';') r[i] |= CC_spec;

			if (i == '+' || i == '-') r[i] |= CC_digitSign;
			if (i >= '0' && i <= '9') r[i] |= CC_digit;
			if (i >= 'A' && i <= 'F') r[i] |= CC_digitHex;
			if (i >= 'a' && i <= 'f') r[i] |= CC_digitHex;

			if (i == 'x' || i == 'X') r[i] |= CC_digitPrefix;
			if (i == 'b' || i == 'B') r[i] |= CC_digitPrefix;
			if (i == 'o' || i == 'O') r[i] |= CC_digitPrefix;

			if (i >= 'A' && i <= 'Z') r[i] |= CC_charRest;
			if (i >= 'a' && i <= 'z') r[i] |= CC_charRest;
			if (i == '_' || i == '.') r[i] |= CC_charRest;

			if (i == '"') r[i] |= CC_quote;
		}
		return r;
	}

	private readonly struct NextSymbol(char c, byte decode)
	{
		public readonly char c = c;
		public readonly byte decode = decode;

		/// <summary>Is valid symbol.</summary>
		public bool IsValid => c != 0;

		/// <summary>Check type of symbol.</summary>
		public bool Is(byte t) => (decode & t) != 0;

		/// <summary>Is this symbol starting next token?</summary>
		public bool IsStartOfNextToken => c == 0 || Is(CC_spec | CC_none);
	}

	private NextSymbol PeekCharacter()
	{
		if (_begin != _end)
		{
			var c = _s[_begin];
			// C++ works on bytes, chars outside of 0..255 have no class
			return new NextSymbol(c, c < 256 ? charDecoder[c] : (byte)0);
		}
		else
		{
			return new NextSymbol('\0', 0);
		}
	}

	private NextSymbol ReadCharacter()
	{
		var curr = PeekCharacter();
		//it will stop on `\0` character
		if (curr.IsValid)
		{
			if (_s[_begin] == '\n') ++_linePos;
			++_begin;
		}
		return curr;
	}

	private void BackCharacter()
	{
		--_begin;
		if (_s[_begin] == '\n') --_linePos;
	}

	/// <summary>Function extracting token from range.</summary>
	/// <param name="excepted">what token type we expecting now</param>
	/// <returns>extracted token</returns>
	public SelectedToken GetNextToken(TokenEnum excepted = TokenEnum.TokenNone)
	{
		//end of sequence, quit.
		if (_begin == _end)
		{
			return default;
		}

		//find first no whitespace character.
		if (PeekCharacter().Is(CC_none))
		{
			while (true)
			{
				var next = ReadCharacter();
				if (!next.IsValid)
				{
					break;
				}
				if (next.c == '#')
				{
					while (true)
					{
						var comment = ReadCharacter();
						if (!comment.IsValid || comment.c == '\n')
						{
							break;
						}
					}
					continue;
				}
				else if (next.Is(CC_none))
				{
					continue;
				}
				else
				{
					//not empty character, put it back
					BackCharacter();
					break;
				}
			}
			if (!PeekCharacter().IsValid)
			{
				return default;
			}
		}

		//start of new token of unknown type
		var type = TokenEnum.TokenInvalid;
		var begin = _begin;
		var first = ReadCharacter();

		//text like `"abcdef"`
		if (first.Is(CC_quote))
		{
			type = TokenEnum.TokenText;
			while (true)
			{
				var next = ReadCharacter();
				if (!next.IsValid)
				{
					break;
				}
				if (next.c == first.c)
				{
					break;
				}
				else if (next.c == '\\')
				{
					var escapedChar = ReadCharacter();
					if (escapedChar.c == first.c)
					{
						continue;
					}
					else if (escapedChar.c == '\\')
					{
						continue;
					}
					else
					{
						type = TokenEnum.TokenInvalid;
						break;
					}
				}
				else if (next.c == '\n')
				{
					type = TokenEnum.TokenInvalid;
					break;
				}
				else
				{
					//eat all other chars
					continue;
				}
			}
			if (!PeekCharacter().IsStartOfNextToken)
			{
				type = TokenEnum.TokenInvalid;
			}

		}
		//special symbol like `;` or `:`
		else if (first.Is(CC_spec))
		{
			if (first.c == ':')
			{
				type = excepted == TokenEnum.TokenColon ? TokenEnum.TokenColon : TokenEnum.TokenInvalid;
			}
			else if (first.c == ';')
			{
				//semicolon wait for his turn, returning empty token
				if (excepted != TokenEnum.TokenSemicolon)
				{
					BackCharacter();
					type = TokenEnum.TokenNone;
				}
				else
				{
					type = TokenEnum.TokenSemicolon;
				}
			}
			else
			{
				type = TokenEnum.TokenInvalid;
			}
		}
		//number like `0x1234` or `5432` or `+232`
		else if (first.Is(CC_digitSign | CC_digit))
		{
			var firstDigit = first;
			//sign
			if (firstDigit.Is(CC_digitSign))
			{
				firstDigit = ReadCharacter();
			}
			if (firstDigit.Is(CC_digit))
			{
				var prefix = PeekCharacter();
				var havePrefix = firstDigit.c == '0' && prefix.Is(CC_digitPrefix);
				var hex = havePrefix && (prefix.c == 'x' || prefix.c == 'X');

				if (havePrefix)
				{
					//eat `x` or `o` or `b`
					ReadCharacter();
				}
				else
				{
					//at least we have already one digit
					type = TokenEnum.TokenNumber;
				}

				byte serachClass = hex ? (byte)(CC_digitHex | CC_digit) : CC_digit;

				while (true)
				{
					var next = ReadCharacter();
					if (!next.IsValid)
					{
						break;
					}
					//end of symbol
					if (next.IsStartOfNextToken)
					{
						BackCharacter();
						break;
					}
					else if (next.Is(serachClass))
					{
						type = TokenEnum.TokenNumber;
					}
					else
					{
						type = TokenEnum.TokenInvalid;
						break;
					}
				}
			}
		}
		//symbol like `abcd` or `p12345`
		else if (first.Is(CC_charRest))
		{
			type = TokenEnum.TokenSymbol;
			while (true)
			{
				var next = ReadCharacter();
				if (!next.IsValid)
				{
					break;
				}
				//end of symbol
				if (next.IsStartOfNextToken)
				{
					BackCharacter();
					break;
				}
				else if (!next.Is(CC_charRest | CC_digit))
				{
					type = TokenEnum.TokenInvalid;
					break;
				}
			}

		}
		var end = _begin;
		return new SelectedToken(type, new ScriptRef(_s, begin, end), _linePos);
	}
}

/// <summary>ScriptRef that is glue from independent parts. First empty ref mean end of list.</summary>
public struct ScriptRefCompound
{
	public ScriptRef p0, p1, p2, p3;

	/// <summary>Constructor from one ref.</summary>
	public ScriptRefCompound(ScriptRef r)
	{
		p0 = r;
	}

	private readonly ScriptRef Get(int i) => i switch { 0 => p0, 1 => p1, 2 => p2, _ => p3 };

	private void Set(int i, ScriptRef r)
	{
		switch (i)
		{
			case 0: p0 = r; break;
			case 1: p1 = r; break;
			case 2: p2 = r; break;
			default: p3 = r; break;
		}
	}

	public bool TryPopBack()
	{
		int prev = -1;
		for (int i = 0; i < 4; ++i)
		{
			if (Get(i).IsValid)
			{
				prev = i;
			}
			else
			{
				break;
			}
		}
		if (prev >= 0) Set(prev, default);
		return prev >= 0;
	}

	public bool TryPushBack(ScriptRef n)
	{
		int prev = -1;
		for (int i = 0; i < 4; ++i)
		{
			if (Get(i).IsValid)
			{
				continue;
			}
			else
			{
				prev = i;
				break;
			}
		}
		if (prev >= 0) Set(prev, n);
		return prev >= 0;
	}

	public void Clear()
	{
		p0 = p1 = p2 = p3 = default;
	}

	public readonly bool HaveParts => p1.IsValid;

	public readonly int SizeParts
	{
		get
		{
			int s = 0;
			for (int i = 0; i < 4 && Get(i).IsValid; ++i) s += 1;
			return s;
		}
	}

	public readonly int Size
	{
		get
		{
			int s = 0;
			for (int i = 0; i < 4 && Get(i).IsValid; ++i) s += Get(i).Size;
			return s;
		}
	}

	public readonly ScriptRef Last
	{
		get
		{
			ScriptRef l = default;
			for (int i = 0; i < 4 && Get(i).IsValid; ++i) l = Get(i);
			return l;
		}
	}

	public override readonly string ToString()
	{
		var sb = new StringBuilder();
		for (int i = 0; i < 4 && Get(i).IsValid; ++i) sb.Append(Get(i).AsSpan());
		return sb.ToString();
	}

	public readonly bool IsValid => p0.IsValid;

	/// <summary>Name as one string.</summary>
	public readonly string Joined => ToString();
}

/// <summary>Operation name split to optional argument and function.</summary>
public struct ScriptRefOperation
{
	public ScriptProcRange procList;
	public ScriptRefCompound procName;

	public ScriptRefData argRef;
	public ScriptRef argName;

	/// <summary>Check if whole object is correct.</summary>
	public readonly bool IsValid =>
		(procName.IsValid && procList.IsValid) && // have function name and have related overload set
		(!argName.IsValid || (argRef.IsValid && procName.HaveParts)); // have optional argument embedded in original operation name

	public readonly bool HaveProc => procName.IsValid;

	public readonly bool HaveArg => argName.IsValid;
}

/// <summary>Fixed size list of arguments.</summary>
public sealed class ScriptArgList
{
	private int argsLength;
	private readonly ScriptRefData[] args = new ScriptRefData[ScriptMaxArg];

	/// <summary>Add one arg to list.</summary>
	public bool TryPushBack(ScriptRefData d)
	{
		if (argsLength < args.Length)
		{
			args[argsLength++] = d;
			return true;
		}
		else
		{
			return false;
		}
	}

	/// <summary>Add arg range to list.</summary>
	public bool TryPushBack(ReadOnlySpan<ScriptRefData> l)
	{
		if (l.Length + argsLength <= args.Length)
		{
			// copy first, `l` can be part of this list
			var temp = l.ToArray();
			foreach (var d in temp)
			{
				args[argsLength++] = d;
			}
			return true;
		}
		else
		{
			return false;
		}
	}

	public int Size => argsLength;

	public ReadOnlySpan<ScriptRefData> Span => args.AsSpan(0, argsLength);
}

////////////////////////////////////////////////////////////
//					Parser helper functions
////////////////////////////////////////////////////////////

/// <summary>Helper functions of parsing (C++ anonymous namespace of Script.cpp).</summary>
internal static class ScriptParseHelper
{
	public static readonly ScriptRef[] KnowNamesPrefix = [new("ModList"), new("Tag")];

	public static bool IsKnowNamePrefix(ScriptRef name)
	{
		foreach (var r in KnowNamesPrefix)
		{
			if (r == name)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Test for validity of arguments.</summary>
	public static bool ValidOverloadProc(ArgEnum[][] overload)
	{
		foreach (var p in overload)
		{
			foreach (var pp in p)
			{
				if (pp == ArgInvalid)
				{
					return false;
				}
			}
		}
		return true;
	}

	public static string DisplayArgs(ScriptParserBase spb, ReadOnlySpan<ScriptRefData> range)
	{
		var result = new StringBuilder();
		foreach (var p in range)
		{
			AppendType(spb, result, p.type);
		}
		if (result.Length != 0)
		{
			result.Length -= 1;
		}
		return result.ToString();
	}

	private static void AppendType(ScriptParserBase spb, StringBuilder result, ArgEnum type)
	{
		if (type != ArgInvalid)
		{
			result.Append('[');
			result.Append(spb.GetTypePrefix(type));
			result.Append(spb.GetTypeName(type).ToString());
			result.Append("] ");
		}
	}

	/// <summary>Display arguments.</summary>
	public static string DisplayOverloadProc(ScriptParserBase spb, ArgEnum[][] overload)
	{
		var result = new StringBuilder();
		foreach (var o in overload)
		{
			AppendType(spb, result, o.Length != 0 ? o[0] : ArgInvalid);
		}
		if (result.Length != 0)
		{
			result.Length -= 1;
		}
		return result.ToString();
	}

	/// <summary>Accept all arguments.</summary>
	public static int OverloadBuildinProc(ScriptProcData spd, ReadOnlySpan<ScriptRefData> args) => 1;

	/// <summary>Reject all arguments.</summary>
	public static int OverloadInvalidProc(ScriptProcData spd, ReadOnlySpan<ScriptRefData> args) => 0;

	/// <summary>Verify arguments.</summary>
	public static int OverloadCustomProc(ScriptProcData spd, ReadOnlySpan<ScriptRefData> args)
	{
		var tempSorce = 255;
		int curr = 0;
		foreach (var currOver in spd.overloadArg)
		{
			if (curr == args.Length)
			{
				return 0;
			}
			var size = currOver.Length;
			if (size != 0)
			{
				if (ArgBase(args[curr].type) != ArgInvalid)
				{
					int oneArgTempScore = 0;
					foreach (var o in currOver)
					{
						oneArgTempScore = Math.Max(oneArgTempScore, ArgCompatible(o, args[curr].type, size - 1));
					}
					tempSorce = Math.Min(tempSorce, oneArgTempScore);
				}
				++curr;
			}
		}
		if (curr != args.Length)
		{
			return 0;
		}
		return tempSorce;
	}

	/// <summary>Return public argument number of given function.</summary>
	public static int GetOverloadArgSize(ArgEnum[][]? over)
	{
		int argSize = 0;
		if (over == null) return 0;
		foreach (var currOver in over)
		{
			if (currOver.Length != 0)
			{
				argSize += 1;
			}
		}

		return argSize;
	}

	/// <summary>Return type of public argument of given function.</summary>
	public static ArgEnum[] GetOverloadArgType(ArgEnum[][]? over, int argPos)
	{
		if (over == null) return [];
		foreach (var currOver in over)
		{
			if (currOver.Length != 0)
			{
				if (argPos == 0)
				{
					return currOver;
				}
				--argPos;
			}
		}

		return [];
	}

	/// <summary>Return tail type list of public arguments of given function.</summary>
	public static ArgEnum[][] GetOverloadArgTypeTail(ArgEnum[][] over, int argPos)
	{
		for (int i = 0; i < over.Length; ++i)
		{
			if (over[i].Length != 0)
			{
				if (argPos == 0)
				{
					return over[i..];
				}
				--argPos;
			}
		}

		return [];
	}

	public static (int score, ScriptProcData? proc) FindBestOverloadProc(ScriptProcRange proc, ReadOnlySpan<ScriptRefData> args)
	{
		int bestSorce = 0;
		ScriptProcData? bestValue = null;
		for (int i = 0; i < proc.Count; ++i)
		{
			var p = proc[i];
			int tempSorce = p.overload(p, args);
			if (tempSorce != 0)
			{
				if (tempSorce == bestSorce)
				{
					bestValue = null;
				}
				else if (tempSorce > bestSorce)
				{
					bestSorce = tempSorce;
					bestValue = p;
				}
			}
		}

		return (bestSorce, bestValue);
	}

	public static ScriptRefOperation FindOperationAndArg(ParserWriter ph, ScriptRef op)
	{
		var result = new ScriptRefOperation
		{
			procName = new ScriptRefCompound(op),
			procList = ph.parser.GetProc(op),
		};
		if (!result.IsValid)
		{
			var first_dot = op.Find('.');
			if (first_dot == -1)
			{
				return result;
			}

			result.argName = op.Head(first_dot);
			result.argRef = ph.GetReferece(result.argName);
			if (!result.argRef.IsValid)
			{
				var origArgName = result.argName;

				++first_dot; //skip '.'
				var second_dot = op.Tail(first_dot).Find('.');
				if (second_dot == -1)
				{
					return result;
				}
				second_dot += first_dot;
				result.argName = op.Head(second_dot);
				result.argRef = ph.GetReferece(result.argName);
				if (!result.argRef.IsValid)
				{
					// restore initial name for error propose, but only if is unknown. Other wise typo should be in next part
					if (IsKnowNamePrefix(origArgName) == false)
					{
						result.argName = origArgName;
					}
					return result;
				}
				first_dot = second_dot;
			}

			var name = ph.parser.GetTypeName(result.argRef.type);
			if (result.argRef.type < ArgMax || !name.IsValid)
			{
				return result;
			}

			result.procName = new ScriptRefCompound { p0 = name, p1 = op.Tail(first_dot) };
			result.procList = ph.parser.GetProc(result.procName);
		}

		return result;
	}

	public static ScriptRefOperation ReplaceOperation(ParserWriter ph, ScriptRefOperation op, ScriptRef from, ScriptRef to)
	{
		var result = op;

		bool correct = false;
		if (result.procName.Size != 0)
		{
			var last = result.procName.Last;
			var lastHead = last.HeadFromEnd(from.Size);
			var lastTail = last.TailFromEnd(from.Size);
			if (lastHead == from)
			{
				correct = true;
				correct &= result.procName.TryPopBack();
				if (lastTail.IsValid)
				{
					correct &= result.procName.TryPushBack(lastTail);
				}
				correct &= result.procName.TryPushBack(to);
				result.procList = ph.parser.GetProc(result.procName);
				correct &= result.procList.IsValid;
			}
		}

		if (correct)
		{
			return result;
		}
		else
		{
			return default;
		}
	}

	public static void LogErrorOnOperationArg(ScriptRefOperation op)
	{
		if (op.IsValid)
		{
			return;
		}

		if (op.HaveArg)
		{
			if (op.argRef.IsValid)
			{
				if (op.procName.HaveParts)
				{
					Logger.Error("Unknown operation name '" + op.procName.ToString() + "' for variable '" + op.argName.ToString() + "'");
				}
				else
				{
					Logger.Error("Unsupported type for variable '" + op.argName.ToString() + "'");
				}
			}
			else
			{
				Logger.Error("Unknown variable name '" + op.argName.ToString() + "'");
			}
		}
	}

	////////////////////////////////////////////////////////////
	//			Pushing operation on proc vector
	////////////////////////////////////////////////////////////

	private static void LogExpected(ParserWriter ph, ScriptProcRange proc)
	{
		Logger.Error("Expected:");
		foreach (var p in proc.All())
		{
			if (p.parserArg != null && p.HaveOverloadArg)
			{
				Logger.Error("  " + DisplayOverloadProc(ph.parser, p.overloadArg));
			}
		}
	}

	/// <summary>Helper choosing correct overload function to call.</summary>
	public static bool ParseOverloadProc(ParserWriter ph, ScriptProcRange proc, ReadOnlySpan<ScriptRefData> args)
	{
		if (!proc.IsValid)
		{
			return false;
		}
		if (args.Length > ScriptMaxArg)
		{
			return false;
		}

		var (bestSorce, bestValue) = FindBestOverloadProc(proc, args);

		if (bestSorce != 0)
		{
			if (bestValue != null)
			{
				if (bestValue.Invoke(ph, args) == false)
				{
					Logger.Error("Error in matching arguments for operator '" + proc.First.name.ToString() + "'");
					return false;
				}
				else
				{
					return true;
				}
			}
			else
			{
				Logger.Error("Conflicting overloads for operator '" + proc.First.name.ToString() + "' for:");
				Logger.Error("  " + DisplayArgs(ph.parser, args));
				LogExpected(ph, proc);
				return false;
			}
		}
		else
		{
			Logger.Error("Can't match overload for operator '" + proc.First.name.ToString() + "' for:");
			Logger.Error("  " + DisplayArgs(ph.parser, args));
			LogExpected(ph, proc);
			return false;
		}
	}

	/// <summary>Helper used to parse line for build in function.</summary>
	public static ScriptParserFunc ParseBuildinProc(ScriptFuncGroup group)
	{
		return (ScriptProcData spd, ParserWriter ph, ReadOnlySpan<ScriptRefData> args) =>
		{
			ph.PushProc(group.Impl);
			int ver = group.Parse(ph, args);
			return ver >= 0;
		};
	}

	/// <summary>Helper used to parse line for custom functions.</summary>
	public static bool ParseCustomProc(ScriptProcData spd, ParserWriter ph, ReadOnlySpan<ScriptRefData> args)
	{
		var group = spd.parserArg!;
		var instr = ph.PushProc(group.Impl);

		var argType = group.Parse(ph, args);

		if (argType < 0)
		{
			return false;
		}

		// C++ copies the arguments into a raw block of at most 64 bytes.
		return instr.argBytes <= 64;
	}

	////////////////////////////////////////////////////////////
	//		Custom parsers of operation arguments
	////////////////////////////////////////////////////////////

	public static readonly ScriptRef[] ConditionNames =
	[
		new("eq"), new("neq"),
		new("le"), new("gt"),
		new("ge"), new("lt"),
	];

	public static readonly ScriptRef[] ConditionSpecNames =
	[
		new("or"),
		new("and"),
	];

	/// <summary>Helper used of condition operations.</summary>
	public static bool ParseConditionImpl(ParserWriter ph, ScriptRefData truePos, ScriptRefData falsePos, ReadOnlySpan<ScriptRefData> args)
	{
		if (args.Length != 3)
		{
			Logger.Error("Invalid length of condition arguments");
			return false;
		}

		ScriptRefData[] conditionArgs =
		[
			args[1],
			args[2],
			truePos, //success
			falsePos, //failure
		];

		bool equalFunc = false;
		int i = 0;
		for (; i < ConditionNames.Length; ++i)
		{
			if (args[0].name == ConditionNames[i])
			{
				if (i < 2) equalFunc = true;
				if ((i & 1) != 0) (conditionArgs[2], conditionArgs[3]) = (conditionArgs[3], conditionArgs[2]); //negate condition result
				if (i >= 4) (conditionArgs[0], conditionArgs[1]) = (conditionArgs[1], conditionArgs[0]); //swap condition args
				break;
			}
		}
		if (i == ConditionNames.Length)
		{
			Logger.Error("Unknown condition: '" + args[0].name.ToString() + "'");
			return false;
		}

		var proc = ph.parser.GetProc(new ScriptRef(equalFunc ? "test_eq" : "test_le"));
		if (ParseOverloadProc(ph, proc, conditionArgs) == false)
		{
			Logger.Error("Unsupported operator: '" + args[0].name.ToString() + "'");
			return false;
		}

		return true;
	}

	/// <summary>Parse <c>or</c> or <c>and</c> conditions.</summary>
	public static bool ParseFullConditionImpl(ParserWriter ph, ScriptRefData falsePos, ReadOnlySpan<ScriptRefData> args)
	{
		if (args.Length <= 1)
		{
			Logger.Error("Invalid length of condition arguments");
			return false;
		}

		// each operation can fail, we can't prevent
		var correct = true;

		var truePos = ph.AddLabel();
		var orFunc = args[0].name == ConditionSpecNames[0];
		var andFunc = args[0].name == ConditionSpecNames[1];
		if (orFunc || andFunc)
		{
			args = args[1..];
			for (; args.Length > 3; args = args[3..])
			{
				var temp = ph.AddLabel();
				if (orFunc)
				{
					correct &= ParseConditionImpl(ph, truePos, temp, args[..3]);
				}
				else
				{
					correct &= ParseConditionImpl(ph, temp, falsePos, args[..3]);
				}
				correct &= ph.SetLabel(temp, ph.GetCurrPos());
			}
		}
		correct &= ParseConditionImpl(ph, truePos, falsePos, args);

		correct &= ph.SetLabel(truePos, ph.GetCurrPos());
		return correct;
	}

	/// <summary>Helper used of variable operation declaration.</summary>
	public static bool ParseVariableImpl(ParserWriter ph, ScriptRefData reg, ScriptRefData val = default)
	{
		if (!ArgIsReg(reg.type))
		{
			Logger.Error("Invalid register");
			return false;
		}

		if (val.IsValid)
		{
			ScriptRefData[] setArgs =
			[
				reg,
				val,
			];
			var proc = ph.parser.GetProc(new ScriptRef("set"));
			return ParseOverloadProc(ph, proc, setArgs);
		}
		else
		{
			ScriptRefData[] setArgs =
			[
				reg,
			];
			var proc = ph.parser.GetProc(new ScriptRef("clear"));
			return ParseOverloadProc(ph, proc, setArgs);
		}
	}

	/// <summary>Parser of <c>if</c> operation.</summary>
	public static bool ParseIf(ScriptProcData spd, ParserWriter ph, ReadOnlySpan<ScriptRefData> args)
	{
		var block = ph.PushScopeBlock(BlockEnum.BlockIf);
		block.nextLabel = ph.AddLabel();
		block.finalLabel = ph.AddLabel();

		return ParseFullConditionImpl(ph, block.nextLabel, args);
	}

	/// <summary>Parser of <c>else</c> operation.</summary>
	public static bool ParseElse(ScriptProcData spd, ParserWriter ph, ReadOnlySpan<ScriptRefData> args)
	{
		if (ph.codeBlocks[^1].type != BlockEnum.BlockIf)
		{
			Logger.Error("Unexpected 'else'");
			return false;
		}

		// each operation can fail, we can't prevent
		var correct = true;

		var block = ph.ClearScopeBlock();

		ph.PushProc(ScriptBuiltin.GotoImpl);
		ph.PushOperand(new ScriptOperand { kind = ScriptOperandKind.None }, 0); // ScriptWorkerBase&
		ph.PushOperand(new ScriptOperand { kind = ScriptOperandKind.None }, 0); // ProgPos&
		correct &= ph.PushLabelTry(block.finalLabel);

		correct &= ph.SetLabel(block.nextLabel, ph.GetCurrPos());
		if (args.Length == 0)
		{
			block.nextLabel = block.finalLabel;
			block.type = BlockEnum.BlockElse;
		}
		else
		{
			block.nextLabel = ph.AddLabel();
			correct &= ParseFullConditionImpl(ph, block.nextLabel, args);
		}

		if (correct)
		{
			return true;
		}
		else
		{
			Logger.Error("Error in processing 'else'");
			return false;
		}
	}

	/// <summary>Parser of <c>begin</c> operation.</summary>
	public static bool ParseBegin(ScriptProcData spd, ParserWriter ph, ReadOnlySpan<ScriptRefData> args)
	{
		if (args.Length != 0)
		{
			Logger.Error("Unexpected symbols after 'begin'");
			return false;
		}

		ph.PushScopeBlock(BlockEnum.BlockBegin);

		return true;
	}

	/// <summary>Push <c>goto label</c>.</summary>
	private static bool PushGoto(ParserWriter ph, ScriptRefData label)
	{
		ph.PushProc(ScriptBuiltin.GotoImpl);
		ph.PushOperand(new ScriptOperand { kind = ScriptOperandKind.None }, 0); // ScriptWorkerBase&
		ph.PushOperand(new ScriptOperand { kind = ScriptOperandKind.None }, 0); // ProgPos&
		return ph.PushLabelTry(label);
	}

	/// <summary>Parser of <c>loop</c> operation.</summary>
	public static bool ParseLoop(ScriptProcData spd, ParserWriter ph, ReadOnlySpan<ScriptRefData> args)
	{
		if (args.Length < 3)
		{
			Logger.Error("Missing symbols after 'loop'");
			return false;
		}
		if (args[0].name != new ScriptRef("var"))
		{
			Logger.Error("After 'loop' should be 'var'");
			return false;
		}

		// each operation can fail, we can't prevent
		var correct = true;

		// we support simple `loop var x 100;` or complex like `loop var x obj.getInv.list "BIG_GUN";`
		var functionPostfix = new ScriptRef(".list");
		var functionName = args[2].name;
		var functionArgSep = ph.GetReferece(new ScriptRef("__"));
		var functionArgPh = ph.GetReferece(new ScriptRef("_"));

		if (functionName.HeadFromEnd(functionPostfix.Size) == functionPostfix && !IsKnowNamePrefix(functionName.TailFromEnd(functionPostfix.Size)))
		{
			var loop = ph.PushScopeBlock(BlockEnum.BlockLoop);
			loop.nextLabel = ph.AddLabel();
			loop.finalLabel = ph.AddLabel();

			var loopArgs = new ScriptArgList();

			(ScriptProcData? proc, ArgEnum[][] types) getProcAndRegTypes(ParserWriter ph, ScriptRefOperation proc, int placeHolders)
			{
				var temp = new ScriptArgList();
				temp.TryPushBack(loopArgs.Span);
				int org = temp.Size;
				for (int i = 0; i < placeHolders; ++i)
				{
					if (!temp.TryPushBack(functionArgPh))
					{
						return (null, []);
					}
				}

				var bestOverload = FindBestOverloadProc(proc.procList, temp.Span).proc;
				if (bestOverload == null)
				{
					Logger.Error("Conflicting overloads for operator '" + proc.procList.First.name.ToString() + "' for:");
					Logger.Error("  " + DisplayArgs(ph.parser, temp.Span));
					LogExpected(ph, proc.procList);
					return (null, []);
				}

				return (bestOverload, GetOverloadArgTypeTail(bestOverload.overloadArg, org));
			}

			ScriptRefData parseReg(ParserWriter ph, ScriptRef name, ArgEnum[] types)
			{
				if (types.Length != 1)
				{
					return default;
				}

				var c = true;
				var r = ph.AddReg(name, ArgSpecAdd(types[0], ArgSpecVar));
				c &= r.IsValid;
				c &= loopArgs.TryPushBack(r);
				c &= ParseVariableImpl(ph, r);
				if (c)
				{
					return r;
				}
				else
				{
					return default;
				}
			}

			// now we known that parameter look like `obj.foo.list` but not like `Tag.list`
			var loopFunction = FindOperationAndArg(ph, functionName);
			var initFunction = ReplaceOperation(ph, loopFunction, functionPostfix, new ScriptRef(".init"));

			if (!loopFunction.IsValid)
			{
				LogErrorOnOperationArg(loopFunction);
				Logger.Error("Unsupported function '" + functionName.ToString() + "' for 'loop'");
				return false;
			}

			if (!initFunction.IsValid)
			{
				Logger.Error("Unsupported function '" + functionName.ToString() + "' for 'loop'");
				return false;
			}

			correct &= loopArgs.TryPushBack(loopFunction.argRef);
			correct &= loopArgs.TryPushBack(args[3..]);
			correct &= loopArgs.TryPushBack(functionArgSep);

			// init part of loop, try parse arg types of control registers
			var (initBestProc, initBestOverload) = getProcAndRegTypes(ph, initFunction, 2);
			if (!correct || GetOverloadArgSize(initBestOverload) != 2)
			{
				Logger.Error("Error in processing init of 'loop'");
				return false;
			}
			var curr = parseReg(ph, default, GetOverloadArgType(initBestOverload, 0));
			var limit = parseReg(ph, default, GetOverloadArgType(initBestOverload, 1));
			correct &= curr.IsValid;
			correct &= limit.IsValid;
			correct &= ParseCustomProc(initBestProc!, ph, loopArgs.Span);

			// check part of loop, break if control register are equal
			correct &= ph.SetLabel(loop.nextLabel, ph.GetCurrPos());
			ScriptRefData[] breakCond =
			[
				new ScriptRefData(new ScriptRef("lt"), ArgInvalid),
				curr,
				limit,
			];
			correct &= ParseFullConditionImpl(ph, loop.finalLabel, breakCond);

			// increment part and getting current element of loop
			correct &= loopArgs.TryPushBack(functionArgSep);
			var (loopBestProc, loopBestOverload) = getProcAndRegTypes(ph, loopFunction, 1);
			if (!correct || GetOverloadArgSize(loopBestOverload) != 1)
			{
				Logger.Error("Error in processing step of 'loop'");
				return false;
			}

			var v = parseReg(ph, args[1].name, GetOverloadArgType(loopBestOverload, 0));
			correct &= v.IsValid;
			correct &= ParseCustomProc(loopBestProc!, ph, loopArgs.Span);
		}
		else
		{
			var loop = ph.PushScopeBlock(BlockEnum.BlockLoop);
			loop.nextLabel = ph.AddLabel();
			loop.finalLabel = ph.AddLabel();

			var limit = ph.AddReg(default, ArgSpecAdd(ArgInt, ArgSpecVar));
			var curr = ph.AddReg(default, ArgSpecAdd(ArgInt, ArgSpecVar));
			var v = ph.AddReg(args[1].name, ArgSpecAdd(ArgInt, ArgSpecVar));

			correct &= limit.IsValid;
			correct &= curr.IsValid;
			correct &= v.IsValid;

			correct &= ParseVariableImpl(ph, limit, args[2]);
			correct &= ParseVariableImpl(ph, curr);

			correct &= ph.SetLabel(loop.nextLabel, ph.GetCurrPos());

			ScriptRefData[] breakCond =
			[
				new ScriptRefData(new ScriptRef("lt"), ArgInvalid),
				curr,
				limit,
			];
			correct &= ParseFullConditionImpl(ph, loop.finalLabel, breakCond);

			correct &= ParseVariableImpl(ph, v, curr);

			ScriptRefData[] addArgs =
			[
				curr,
				new ScriptRefData(default, ArgInt, 1),
			];
			correct &= ParseOverloadProc(ph, ph.parser.GetProc(new ScriptRef("add")), addArgs);
		}

		if (correct)
		{
			return true;
		}
		else
		{
			Logger.Error("Error in processing 'loop'");
			return false;
		}
	}

	/// <summary>Get first outer scope of given type.</summary>
	private static ParserWriter.Block? GetTopBlockOfType(ParserWriter ph, BlockEnum type)
	{
		for (int i = ph.codeBlocks.Count - 1; i >= 0; --i)
		{
			if (ph.codeBlocks[i].type == type)
			{
				return ph.codeBlocks[i];
			}
		}
		return null;
	}

	/// <summary>Parser of <c>break</c> operation.</summary>
	public static bool ParseBreak(ScriptProcData spd, ParserWriter ph, ReadOnlySpan<ScriptRefData> args)
	{
		if (args.Length != 0)
		{
			Logger.Error("Unexpected symbols after 'break'");
			return false;
		}

		var loopBlock = GetTopBlockOfType(ph, BlockEnum.BlockLoop);
		if (loopBlock == null)
		{
			Logger.Error("Operation 'break' outside 'loop'");
			return false;
		}

		// each operation can fail, we can't prevent
		var correct = true;

		correct &= PushGoto(ph, loopBlock.finalLabel);

		if (correct)
		{
			return true;
		}
		else
		{
			Logger.Error("Error in processing 'break'");
			return false;
		}
	}

	/// <summary>Parser of <c>continue</c> operation.</summary>
	public static bool ParseContinue(ScriptProcData spd, ParserWriter ph, ReadOnlySpan<ScriptRefData> args)
	{
		if (args.Length != 0)
		{
			Logger.Error("Unexpected symbols after 'continue'");
			return false;
		}

		var loopBlock = GetTopBlockOfType(ph, BlockEnum.BlockLoop);
		if (loopBlock == null)
		{
			Logger.Error("Operation 'continue' outside 'loop'");
			return false;
		}

		// each operation can fail, we can't prevent
		var correct = true;

		correct &= PushGoto(ph, loopBlock.nextLabel);

		if (correct)
		{
			return true;
		}
		else
		{
			Logger.Error("Error in processing 'continue'");
			return false;
		}
	}

	/// <summary>Parser of <c>end</c> operation.</summary>
	public static bool ParseEnd(ScriptProcData spd, ParserWriter ph, ReadOnlySpan<ScriptRefData> args)
	{
		if (ph.codeBlocks[^1].type == BlockEnum.BlockMain)
		{
			Logger.Error("Unexpected 'end'");
			return false;
		}
		if (args.Length != 0)
		{
			Logger.Error("Unexpected symbols after 'end'");
			return false;
		}

		// each operation can fail, we can't prevent
		var correct = true;

		var block = ph.PopScopeBlock();

		switch (block.type)
		{
			case BlockEnum.BlockIf:
			case BlockEnum.BlockElse:
				if (!ParserWriter.SameLabel(block.nextLabel, block.finalLabel))
				{
					correct &= ph.SetLabel(block.nextLabel, ph.GetCurrPos());
				}
				correct &= ph.SetLabel(block.finalLabel, ph.GetCurrPos());
				break;

			case BlockEnum.BlockBegin:
				// nothing
				break;

			case BlockEnum.BlockLoop:
				correct &= PushGoto(ph, block.nextLabel);

				correct &= ph.SetLabel(block.finalLabel, ph.GetCurrPos());
				break;

			default:
				throw new InvalidOperationException("Unsuported block type");
		}

		if (correct)
		{
			return true;
		}
		else
		{
			Logger.Error("Error in processing 'end'");
			return false;
		}
	}

	/// <summary>Parser of <c>var</c> operation that define local variables.</summary>
	public static bool ParseVar(ScriptProcData spd, ParserWriter ph, ReadOnlySpan<ScriptRefData> args)
	{
		var spec = ArgSpecVar;
		if (args.Length != 0)
		{
			if (args[0].name == new ScriptRef("ptr"))
			{
				spec |= ArgSpecPtr;
				args = args[1..];
			}
			else if (args[0].name == new ScriptRef("ptre"))
			{
				spec |= ArgSpecPtrE;
				args = args[1..];
			}
		}
		var size = args.Length;
		if (size < 2 || 3 < size)
		{
			Logger.Error("Invalid length of 'var' definition");
			return false;
		}

		// adding new custom variables of type selected type.
		var type_curr = ph.parser.GetType(args[0].name);
		if (type_curr == null)
		{
			Logger.Error("Invalid type '" + args[0].name.ToString() + "'");
			return false;
		}

		if (type_curr.Value.meta.size == 0 && (spec & ArgSpecPtr) == 0)
		{
			Logger.Error("Can't create variable of type '" + args[0].name.ToString() + "', require 'ptr'");
			return false;
		}

		args = args[1..];
		if (args[0].type != ArgUnknowSimple || !args[0].name.IsValid)
		{
			Logger.Error("Invalid variable name '" + args[0].name.ToString() + "'");
			return false;
		}
		if (ph.parser.GetType(args[0].name) != null)
		{
			Logger.Error("Invalid variable name '" + args[0].name.ToString() + "' same as existing type");
			return false;
		}
		if (ph.parser.GetProc(args[0].name).IsValid)
		{
			Logger.Error("Invalid variable name '" + args[0].name.ToString() + "' same as existing function");
			return false;
		}

		var reg = ph.AddReg(args[0].name, ArgSpecAdd(type_curr.Value.type, spec));
		if (!reg.IsValid)
		{
			Logger.Error("Invalid type for variable '" + args[0].name.ToString() + "'");
			return false;
		}

		// each operation can fail, we can't prevent
		var correct = true;

		if (size == 2)
		{
			correct &= ParseVariableImpl(ph, reg);
		}
		else
		{
			correct &= ParseVariableImpl(ph, reg, args[1]);
		}

		if (correct)
		{
			return true;
		}
		else
		{
			Logger.Error("Error in processing 'var'");
			return false;
		}
	}

	/// <summary>Parser of <c>const</c> operation that define local variables.</summary>
	public static bool ParseConst(ScriptProcData spd, ParserWriter ph, ReadOnlySpan<ScriptRefData> args)
	{
		var spec = ArgSpecNone;
		if (args.Length != 0)
		{
			if (args[0].name == new ScriptRef("ptr"))
			{
				spec |= ArgSpecPtr;
				args = args[1..];
			}
			else if (args[0].name == new ScriptRef("ptre"))
			{
				spec |= ArgSpecPtrE;
				args = args[1..];
			}
		}
		var size = args.Length;
		if (3 != size)
		{
			Logger.Error("Invalid length of 'const' definition");
			return false;
		}

		// adding new custom variables of type selected type.
		var type_curr = ph.parser.GetType(args[0].name);
		if (type_curr == null)
		{
			Logger.Error("Invalid type '" + args[0].name.ToString() + "'");
			return false;
		}

		if (type_curr.Value.meta.size == 0 && (spec & ArgSpecPtr) == 0)
		{
			Logger.Error("Can't create const of type '" + args[0].name.ToString() + "', require 'ptr'");
			return false;
		}

		args = args[1..];
		if (args[0].type != ArgUnknowSimple || !args[0].name.IsValid)
		{
			Logger.Error("Invalid const name '" + args[0].name.ToString() + "'");
			return false;
		}
		if (ph.parser.GetType(args[0].name) != null)
		{
			Logger.Error("Invalid variable name '" + args[0].name.ToString() + "' same as existing type");
			return false;
		}
		if (ph.parser.GetProc(args[0].name).IsValid)
		{
			Logger.Error("Invalid variable name '" + args[0].name.ToString() + "' same as existing function");
			return false;
		}

		var type = ArgSpecAdd(type_curr.Value.type, spec);

		if (type != args[1].type)
		{
			Logger.Error("Invalid value '" + args[1].name.ToString() + "' for const type '" + args[0].name.ToString() + "'");
			return false;
		}

		var reg = ph.AddConst(args[0].name, type, args[1].value);
		if (!reg.IsValid)
		{
			Logger.Error("Invalid type for const '" + args[0].name.ToString() + "'");
			return false;
		}

		return true;
	}

	/// <summary>Parse return statement.</summary>
	public static bool ParseReturn(ScriptProcData spd, ParserWriter ph, ReadOnlySpan<ScriptRefData> args)
	{
		var size = args.Length;
		var returnSize = ph.parser.HaveEmptyReturn ? 0 : ph.parser.ParamSize;
		if (returnSize != size)
		{
			Logger.Error("Invalid length of returns arguments");
			return false;
		}

		var outputRegsData = new ScriptRefData[ScriptMaxOut];
		var currValueIndex = new RegEnum[ScriptMaxOut];
		var newValueIndex = new RegEnum[ScriptMaxOut];

		for (int i = 0; i < size; ++i)
		{
			outputRegsData[i] = ph.parser.GetParamData(i)!.Value;
			if (args[i].IsRegValue && ArgCompatible(outputRegsData[i].type, args[i].type, 1) == 0)
			{
				Logger.Error("Invalid return argument '" + args[i].name.ToString() + "'");
				return false;
			}
			currValueIndex[i] = outputRegsData[i].GetReg();
			newValueIndex[i] = args[i].GetRegOrDefault(RegInvalid);
			if (currValueIndex[i] == newValueIndex[i])
			{
				currValueIndex[i] = RegInvalid;
			}
		}

		// matching return arguments to return register,
		// sometimes current value in one register is needed in another.
		// we need find order of assignments that will not lose any value.
		var any_changed = true;
		var all_free = false;
		while (!all_free && any_changed)
		{
			all_free = true;
			any_changed = false;
			for (int i = 0; i < size; ++i)
			{
				if (currValueIndex[i] == RegInvalid)
				{
					continue;
				}
				var free = true;
				for (int j = 0; j < size; ++j)
				{
					if (i != j && currValueIndex[i] == newValueIndex[j])
					{
						free = false;
						break;
					}
				}
				if (free)
				{
					any_changed = true;
					currValueIndex[i] = RegInvalid;
					ScriptRefData[] temp = [outputRegsData[i], args[i]];

					var proc = ph.parser.GetProc(new ScriptRef("set"));
					if (!ParseOverloadProc(ph, proc, temp))
					{
						Logger.Error("Invalid return argument '" + args[i].name.ToString() + "'");
						return false;
					}
				}
				else
				{
					all_free = false;
				}
			}
		}

		if (!all_free)
		{
			// now we can have only cases where return register have circular dependencies:
			// e.g. A need B, B need C and C need A.
			// using swap we can fix circular dependencies.
			for (int i = 0; i < size; ++i)
			{
				if (currValueIndex[i] == RegInvalid)
				{
					continue;
				}

				for (int j = 0; j < size; ++j)
				{
					if (i != j && newValueIndex[i] == currValueIndex[j])
					{
						ScriptRefData[] temp = [outputRegsData[i], outputRegsData[j]];

						var proc = ph.parser.GetProc(new ScriptRef("swap"));
						if (!ParseOverloadProc(ph, proc, temp))
						{
							return false;
						}

						// now value from 'i' is in 'j'
						currValueIndex[j] = currValueIndex[i];
						currValueIndex[i] = RegInvalid;
						break;
					}
				}
			}
		}

		ph.PushProc(ScriptBuiltin.ExitImpl);
		ph.PushOperand(new ScriptOperand { kind = ScriptOperandKind.None }, 0);
		return true;
	}

	/// <summary>Parse <c>debug_log</c> operator.</summary>
	public static bool ParseDebugLog(ScriptProcData spd, ParserWriter ph, ReadOnlySpan<ScriptRefData> args)
	{
		if (!Options.debug)
		{
			return true;
		}

		for (int i = 0; i < args.Length; ++i)
		{
			var proc = ph.parser.GetProc(new ScriptRef("debug_impl"));
			if (!ParseOverloadProc(ph, proc, args.Slice(i, 1)))
			{
				Logger.Error("Invalid debug argument '" + args[i].name.ToString() + "'");
				return false;
			}
		}

		var flush = ph.parser.GetProc(new ScriptRef("debug_flush"));
		return flush.Count == 1 && flush.First.Invoke(ph, []);
	}

	/// <summary>Dummy operation.</summary>
	public static bool ParseDummy(ScriptProcData spd, ParserWriter ph, ReadOnlySpan<ScriptRefData> args)
	{
		Logger.Error("Reserved operation for future use");
		return false;
	}
}

////////////////////////////////////////////////////////////
//				ScriptParserBase class
////////////////////////////////////////////////////////////

/// <summary>Common base of script parser.</summary>
public class ScriptParserBase
{
	private readonly ScriptGlobal _shared;
	private bool _emptyReturn;
	private int _regUsedSpace;
	private byte _regOutSize;
	private readonly ScriptRef[] _regOutName = new ScriptRef[ScriptMaxOut];
	private readonly string _name;
	private string _defaultScript = "";
	private string _description = "";
	private readonly List<ScriptTypeData> _typeList = [];
	private readonly List<ScriptProcData> _procList = [];
	private readonly List<ScriptRefData> _refList = [];
	private ScriptSignature? _signature;
	private readonly List<ScriptParam> _outputs = [];
	private readonly List<ScriptParam> _inputs = [];

	/// <summary>Layout of output and input arguments.</summary>
	public ScriptSignature Signature => _signature ??= new ScriptSignature([.. _outputs], [.. _inputs]);

	/// <summary>Add registers for script arguments (C++ <c>addRegImpl</c>).</summary>
	protected void AddRegImpl(bool writable, ScriptArgName[] names)
	{
		foreach (var n in names)
		{
			n.type.AddTypeImpl(this);
			AddScriptReg(n.name, n.type.ArgType, writable, n.type.IsOutput);
			(writable ? _outputs : _inputs).Add(n.type);
			_signature = null;
		}
	}

	/// <summary>Default constructor.</summary>
	protected ScriptParserBase(ScriptGlobal shared, string name)
	{
		_shared = shared;
		_emptyReturn = false;
		_regUsedSpace = (int)RegEnum.RegStartPos;
		_regOutSize = 0;
		_name = name;

		//--------------------------------------------------
		//					op_data init
		//--------------------------------------------------
		foreach (var b in ScriptBuiltin.Procs)
		{
			AddParserBase(b.name, b.description, null, b.group.OverloadType(), ScriptParseHelper.ParseBuildinProc(b.group), null);
		}

		void buildin(string s, ScriptParserFunc func)
		{
			AddParserBase(s, "", ScriptParseHelper.OverloadBuildinProc, [], func, null);
		}

		buildin("if", ScriptParseHelper.ParseIf);
		buildin("else", ScriptParseHelper.ParseElse);
		buildin("end", ScriptParseHelper.ParseEnd);
		buildin("var", ScriptParseHelper.ParseVar);
		buildin("const", ScriptParseHelper.ParseConst);
		buildin("debug_log", ScriptParseHelper.ParseDebugLog);
		buildin("debug_assert", ScriptParseHelper.ParseDummy);
		buildin("loop", ScriptParseHelper.ParseLoop);
		buildin("break", ScriptParseHelper.ParseBreak);
		buildin("continue", ScriptParseHelper.ParseContinue);
		buildin("return", ScriptParseHelper.ParseReturn);
		buildin("begin", ScriptParseHelper.ParseBegin);

		AddParser("test_eq", "", ScriptBuiltin.test_eq_null);
		AddParser("debug_impl", "", ScriptBuiltin.debug_impl_int);
		AddParser("debug_impl", "", ScriptBuiltin.debug_impl_text);
		AddParser("debug_flush", "", ScriptBuiltin.debug_flush);

		AddParser("set", "", ScriptBuiltin.set_text);
		AddParser("clear", "", ScriptBuiltin.clear_text);
		AddParser("test_eq", "", ScriptBuiltin.test_eq_text);

		AddTypeBase("int", ArgInt, new TypeInfo(4, 4));
		AddTypeBase("text", ArgText, new TypeInfo(8, 8));
		AddTypeBase("__", ArgSep, new TypeInfo(1, 1));

		var labelName = AddNameRef("label");
		var nullName = AddNameRef("null");
		var phName = AddNameRef("_");
		var seperatorName = AddNameRef("__");
		var varName = AddNameRef("var");
		var constName = AddNameRef("const");

		ScriptSort.AddSortHelper(_typeList, new ScriptTypeData(labelName, ArgLabel, default));
		ScriptSort.AddSortHelper(_typeList, new ScriptTypeData(nullName, ArgNull, default));
		ScriptSort.AddSortHelper(_refList, new ScriptRefData(nullName, ArgNull));
		ScriptSort.AddSortHelper(_refList, new ScriptRefData(phName, ArgPlaceholder));
		ScriptSort.AddSortHelper(_refList, new ScriptRefData(seperatorName, ArgSep));
		ScriptSort.AddSortHelper(_refList, new ScriptRefData(varName, ArgInvalid));
		ScriptSort.AddSortHelper(_refList, new ScriptRefData(constName, ArgInvalid));

		_shared.InitParserGlobals(this);
	}

	/// <summary>Calculate space used by reg of that type.</summary>
	internal static TypeInfo GetRegMeta(ScriptParserBase parser, ArgEnum type)
	{
		var t = parser.GetType(type);
		if (t == null)
		{
			return default;
		}
		else
		{
			return ArgIsPtr(type) ? TypeInfo.PtrTypeInfo : t.Value.meta;
		}
	}

	internal static string DisplayType(ScriptParserBase spb, ArgEnum type)
	{
		return "[" + spb.GetTypePrefix(type) + spb.GetTypeName(type).ToString() + "]";
	}

	/// <summary>Test if that name is already in use.</summary>
	public bool HaveNameRef(string s)
	{
		var r = new ScriptRef(s);
		if (ScriptSort.FindSortHelper(_refList, r) >= 0)
		{
			return true;
		}
		if (ScriptSort.FindSortHelper(_procList, r) >= 0)
		{
			return true;
		}
		if (ScriptSort.FindSortHelper(_typeList, r) >= 0)
		{
			return true;
		}
		foreach (var c in ScriptParseHelper.ConditionNames)
		{
			if (r == c)
			{
				return true;
			}
		}
		foreach (var c in ScriptParseHelper.ConditionSpecNames)
		{
			if (r == c)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Store new name reference for future use.</summary>
	public static ScriptRef AddNameRef(string s) => new(s);

	/// <summary>Add new function parsing arguments of script operation.</summary>
	internal void AddParserBase(string s, string description, ScriptOverloadFunc? overload, ArgEnum[][] overloadArg, ScriptParserFunc? parser, ScriptFuncGroup? arg)
	{
		if (HaveNameRef(s))
		{
			var procs = GetProc(new ScriptRef(s));
			if (!procs.IsValid)
			{
				throw new InvalidOperationException("Function name '" + s + "' already used");
			}
		}
		parser ??= ScriptParseHelper.ParseCustomProc;
		overload ??= ScriptParseHelper.ValidOverloadProc(overloadArg) ? ScriptParseHelper.OverloadCustomProc : ScriptParseHelper.OverloadInvalidProc;
		ScriptSort.AddSortHelper(_procList, new ScriptProcData
		{
			name = AddNameRef(s),
			description = AddNameRef(description),
			overload = overload,
			overloadArg = overloadArg,
			parser = parser,
			parserArg = arg,
		});
	}

	/// <summary>Add line parsing function (C++ <c>addParser&lt;T&gt;</c>).</summary>
	public void AddParser(string s, string description, ScriptFuncGroup group)
	{
		AddParserBase(s, description, null, group.OverloadType(), null, group);
	}

	/// <summary>Add new type to parser.</summary>
	internal void AddTypeBase(string s, ArgEnum type, TypeInfo meta)
	{
		if (HaveNameRef(s))
		{
			throw new InvalidOperationException("Type name '" + s + "' already used");
		}

		ScriptSort.AddSortHelper(_typeList, new ScriptTypeData(AddNameRef(s), ArgBase(type), meta));
	}

	/// <summary>Test if type is already used.</summary>
	public bool HaveTypeBase(ArgEnum type)
	{
		type = ArgBase(type);
		foreach (var v in _typeList)
		{
			if (v.type == type)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Set default script for type.</summary>
	protected void SetDefault(string s) => _defaultScript = s;
	/// <summary>Set description for script.</summary>
	protected void SetDescription(string s) => _description = s;
	/// <summary>Set mode where return does not accept any value.</summary>
	protected void SetEmptyReturn() => _emptyReturn = true;

	/// <summary>Set name for custom script parameter.</summary>
	/// <param name="s">name for custom parameter.</param>
	/// <param name="type">type of custom parameter.</param>
	/// <param name="writableReg">is this reg writable.</param>
	/// <param name="outputReg">is this reg used for script output.</param>
	public void AddScriptReg(string s, ArgEnum type, bool writableReg, bool outputReg)
	{
		if (writableReg || outputReg)
		{
			if (outputReg && _regOutSize >= ScriptMaxOut)
			{
				throw new InvalidOperationException("Custom output reg limit reach for: '" + s + "'");
			}
			type = ArgSpecAdd(type, ArgSpecVar);
		}
		else
		{
			type = ArgSpecAdd(ArgSpecRemove(type, ArgSpecVar), ArgSpecReg);
		}
		var t = GetType(type);
		if (t == null)
		{
			throw new InvalidOperationException("Invalid type for reg: '" + s + "'");
		}
		var meta = GetRegMeta(this, type);
		if (!meta.IsValid)
		{
			throw new InvalidOperationException("Invalid use of type '" + t.Value.name.ToString() + "' for reg: '" + s + "'");
		}
		if (meta.NeedRegSpace(_regUsedSpace) <= ScriptMaxReg)
		{
			if (HaveNameRef(s))
			{
				throw new InvalidOperationException("Reg name '" + s + "' already used");
			}

			var name = AddNameRef(s);
			if (outputReg)
			{
				_regOutName[_regOutSize++] = name;
			}
			var old = meta.NextRegPos(_regUsedSpace);
			_regUsedSpace = meta.NeedRegSpace(_regUsedSpace);
			ScriptSort.AddSortHelper(_refList, new ScriptRefData(name, type, ScriptValueData.FromReg((RegEnum)old)));
		}
		else
		{
			throw new InvalidOperationException("Custom reg limit reach for: '" + s + "'");
		}
	}

	/// <summary>Add const value to script.</summary>
	public void AddConst(string s, ScriptValueData i)
	{
		if (HaveNameRef(s))
		{
			throw new InvalidOperationException("Const name '" + s + "' already used");
		}

		ScriptSort.AddSortHelper(_refList, new ScriptRefData(AddNameRef(s), i.type, i));
	}

	/// <summary>Update const value in script.</summary>
	public void UpdateConst(string s, ScriptValueData i)
	{
		int f = ScriptSort.FindSortHelper(_refList, new ScriptRef(s));
		if (f < 0)
		{
			throw new InvalidOperationException("Unknown const with name '" + s + "' to update");
		}
		var d = _refList[f];
		if (d.type != i.type)
		{
			throw new InvalidOperationException("Incompatible const with name '" + s + "' to update");
		}
		d.value = i;
		_refList[f] = d;
	}

	/// <summary>Test if type was already added.</summary>
	public bool HaveType(ScriptParam p) => HaveTypeBase(p.ArgType);

	/// <summary>Register pointer type in parser (C++ <c>registerPointerType&lt;P&gt;</c>).</summary>
	public void RegisterPointerType<P>() where P : class, IScriptBind
	{
		RegisterPointerType(typeof(P), P.ScriptName, P.ScriptRegister);
	}

	/// <summary>Register pointer type in parser.</summary>
	internal void RegisterPointerType(Type t, string name, Action<ScriptParserBase> register)
	{
		var type = ArgRegisteType(t);
		if (!HaveTypeBase(type))
		{
			AddTypeBase(name, type, default);
			register(this);
		}
	}

	/// <summary>Register pointer type with name in parser but without any automatic registrations.</summary>
	public void RegisterRawPointerType<P>(string s) where P : class
	{
		var type = ArgRegisteType<P>();
		if (!HaveTypeBase(type))
		{
			AddTypeBase(s, type, default);
		}
	}

	/// <summary>Register value type with name in parser but without any automatic registrations.</summary>
	public void RegisterRawValueType<P>(string s) where P : struct
	{
		var type = ArgRegisteType<P>();
		if (!HaveTypeBase(type))
		{
			AddTypeBase(s, type, ScriptParam.ValueMeta<P>());
		}
	}

	/// <summary>Get name of script.</summary>
	public string Name => _name;
	/// <summary>Get default script.</summary>
	public string Default => _defaultScript;

	/// <summary>Get number of parameters.</summary>
	public int ParamSize => _regOutSize;
	/// <summary>Get parameter data.</summary>
	public ScriptRefData? GetParamData(int i) => GetRef(_regOutName[i]);

	/// <summary>Get name of type.</summary>
	public ScriptRef GetTypeName(ArgEnum type)
	{
		var p = GetType(type);
		if (p != null)
		{
			return p.Value.name;
		}
		else
		{
			return default;
		}
	}

	/// <summary>Get full name of type with e.g. <c>var</c> or <c>ptr</c>.</summary>
	public string GetTypePrefix(ArgEnum type)
	{
		string prefix = "";
		if (ArgIsVar(type))
		{
			prefix = "var ";
		}
		if (ArgIsPtr(type))
		{
			if (ArgIsPtrE(type))
			{
				prefix += "ptre ";
			}
			else
			{
				prefix += "ptr ";
			}
		}
		return prefix;
	}

	/// <summary>Get type data.</summary>
	public ScriptTypeData? GetType(ArgEnum type)
	{
		ArgEnum b = ArgBase(type);
		foreach (var t in _typeList)
		{
			if (t.type == b)
			{
				return t;
			}
		}
		return null;
	}

	/// <summary>Get type data with name.</summary>
	public ScriptTypeData? GetType(ScriptRef name)
	{
		int i = ScriptSort.FindSortHelper(_typeList, name);
		return i >= 0 ? _typeList[i] : null;
	}

	/// <summary>Get function data with name.</summary>
	public ScriptProcRange GetProc(ScriptRef name) => GetProc(name.AsSpan());

	/// <summary>Get function data with name equal to joined parts.</summary>
	public ScriptProcRange GetProc(ScriptRefCompound name) => GetProc(name.Joined.AsSpan());

	private ScriptProcRange GetProc(ReadOnlySpan<char> name)
	{
		var lower = ScriptSort.BoundSortHelper(_procList, 0, _procList.Count, name, false);
		var upper = ScriptSort.BoundSortHelper(_procList, lower, _procList.Count, name, true);

		return new ScriptProcRange(_procList, lower, upper);
	}

	/// <summary>Get arguments data with name.</summary>
	public ScriptRefData? GetRef(ScriptRef name)
	{
		int i = ScriptSort.FindSortHelper(_refList, name);
		return i >= 0 ? _refList[i] : null;
	}

	/// <summary>Get script shared data.</summary>
	public ScriptGlobal GetGlobal() => _shared;

	/// <summary>Get true if return does not accept any arguments.</summary>
	public bool HaveEmptyReturn => _emptyReturn;

	/// <summary>Parse string and write script to ScriptBase.</summary>
	/// <param name="destScript">struct where final script is write to</param>
	/// <param name="parentName">name of owner of script</param>
	/// <param name="srcCode">string with script</param>
	/// <returns>true if string have valid script</returns>
	public bool ParseBase(ScriptContainerBase destScript, string parentName, string srcCode)
	{
		var tempScript = new ScriptContainerBase();
		string err = "Error in parsing script '" + _name + "' for '" + parentName + "': ";
		var help = new ParserWriter(
			_regUsedSpace,
			tempScript,
			this
		);

		bool haveLastReturn = false;
		bool haveCodeNormal = false;
		var range = new ScriptRefTokens(srcCode);
		if (!range.IsValid)
		{
			return false;
		}

		var src = range.Source;
		while (true)
		{
			SelectedToken op = range.GetNextToken();
			if (!op.IsValid)
			{
				if (help.codeBlocks.Count > 1)
				{
					Logger.Error(err + "script have missed 'end;'");
					return false;
				}
				if (!haveLastReturn)
				{
					Logger.Error(err + "script need to end with return statement");
					return false;
				}
				help.Relese();
				destScript.MoveFrom(tempScript);
				return true;
			}

			var line_begin = op.range.Begin;
			SelectedToken label = default;
			var args = new SelectedToken[ScriptMaxArg];
			args[0] = range.GetNextToken(TokenEnum.TokenColon);
			if (args[0].Type == TokenEnum.TokenColon)
			{
				(op, label) = (label, op);
				op = range.GetNextToken();
				args[0] = range.GetNextToken();
			}

			ScriptRefOperation op_curr = ScriptParseHelper.FindOperationAndArg(help, op.range);
			if (!op_curr.IsValid)
			{
				ScriptParseHelper.LogErrorOnOperationArg(op_curr);
				Logger.Error("Invalid operation '" + op.ToString() + "'");
			}

			// change form of `Reg.Function` to `Type.Function Reg`.
			if (op_curr.HaveArg)
			{
				// we already loaded op_curr = "Reg.Function", args[0] = "X"
				// then switch it to op_curr = "Type.Function", args[0] = "Reg", args[1] = "X"
				args[1] = args[0];
				args[0] = new SelectedToken(TokenEnum.TokenSymbol, op_curr.argName, op.LinePos);
			}

			for (int i = op_curr.HaveArg ? 2 : 1; i < ScriptMaxArg; ++i)
				args[i] = range.GetNextToken();
			SelectedToken f = range.GetNextToken(TokenEnum.TokenSemicolon);

			//validation
			bool valid = true;
			valid &= label.Type == TokenEnum.TokenSymbol || label.Type == TokenEnum.TokenNone;
			valid &= op.Type == TokenEnum.TokenSymbol;
			for (int i = 0; i < ScriptMaxArg; ++i)
				valid &= args[i].Type != TokenEnum.TokenInvalid;
			valid &= f.Type == TokenEnum.TokenSemicolon;

			if (!valid)
			{
				var line_end = range.Begin;
				if (f.Type != TokenEnum.TokenSemicolon)
				{
					// fixing `line_end` position.
					while (line_end != range.End && src[line_end] != ';')
						++line_end;
					if (line_end != range.End)
						++line_end;
				}

				for (int i = 0; i < ScriptMaxArg; ++i)
				{
					if (args[i].Type == TokenEnum.TokenInvalid)
					{
						Logger.Error(err + "invalid argument '" + args[i].ToString() + "' in line: '" + src[line_begin..line_end] + "' (at " + op.LinePos + ")");
						return false;
					}
				}

				Logger.Error(err + "invalid line: '" + src[line_begin..line_end] + "' (at " + op.LinePos + ")");
				return false;
			}

			var line = new ScriptRef(src, line_begin, range.Begin);

			// test validity of operation positions
			var isReturn = op.range == new ScriptRef("return");
			var isVarDef = (op.range == new ScriptRef("var")) || (op.range == new ScriptRef("const"));
			var isBegin = (op.range == new ScriptRef("if")) || (op.range == new ScriptRef("else")) || (op.range == new ScriptRef("begin")) || (op.range == new ScriptRef("loop"));
			var isEnd = (op.range == new ScriptRef("end")) || (op.range == new ScriptRef("else")); // `else;` is begin and end of scope
			var isBreak = op.range == new ScriptRef("continue") || op.range == new ScriptRef("break");

			if (haveLastReturn && !isEnd)
			{
				Logger.Error(err + "unreachable code after return in line: '" + line.ToString() + "' (at " + op.LinePos + ")");
				return false;
			}
			if (haveCodeNormal && isVarDef)
			{
				Logger.Error(err + "invalid variable definition after other operations in line: '" + line.ToString() + "' (at " + op.LinePos + ")");
				return false;
			}
			if (label.IsValid && isVarDef)
			{
				Logger.Error(err + "label can't be before variable definition in line: '" + line.ToString() + "' (at " + op.LinePos + ")");
				return false;
			}

			haveLastReturn = isReturn || isBreak;
			haveCodeNormal = !(isVarDef || isBegin); // we can have `var` only on begining of new scope

			// matching args from operation definition with args available in string
			var argData = new ScriptArgList();
			foreach (var t in args)
			{
				if (t.Type == TokenEnum.TokenNone)
				{
					break;
				}

				if (!argData.TryPushBack(t.Parse(help)))
				{
					Logger.Error(err + "too many arguments in line: '" + line.ToString() + "' (at " + op.LinePos + ")");
					return false;
				}
			}

			if (label.IsValid && !help.SetLabel(label.Parse(help), help.GetCurrPos()))
			{
				Logger.Error(err + "invalid label '" + label.ToString() + "' in line: '" + line.ToString() + "' (at " + op.LinePos + ")");
				return false;
			}

			// create normal proc call
			if (ScriptParseHelper.ParseOverloadProc(help, op_curr.procList, argData.Span) == false)
			{
				Logger.Error(err + "invalid operation in line: '" + line.ToString() + "' (at " + op.LinePos + ")");
				return false;
			}
		}
	}

	/// <summary>Parse node and return new script.</summary>
	public void ParseNode(ScriptContainerBase container, string parentName, YamlNodeReader reader)
	{
		var scripts = reader["scripts"];
		if (scripts)
		{
			var curr = scripts[Name];
			if (curr)
			{
				if (false == ParseBase(container, parentName, curr.ReadVal<string>()))
				{
					Logger.Error("    for node with code at line " + reader.LocationInFile.Line + " in " + GetGlobal().CurrentFile);
					Logger.Error(""); // dummy line to separate similar errors
				}
			}
		}
		if (!container.IsValid && Default.Length != 0)
		{
			if (false == ParseBase(container, parentName, Default))
			{
				Logger.Error(""); // dummy line to separate similar errors
			}
		}
		container._parser ??= this;
	}

	/// <summary>Parse string and return new script.</summary>
	public void ParseCode(ScriptContainerBase container, string parentName, string srcCode)
	{
		if (srcCode.Length != 0)
		{
			if (false == ParseBase(container, parentName, srcCode))
			{
				Logger.Error("    for code in " + GetGlobal().CurrentFile);
				Logger.Error(""); // dummy line to separate similar errors
			}
		}
		if (!container.IsValid && Default.Length != 0)
		{
			if (false == ParseBase(container, parentName, Default))
			{
				Logger.Error(""); // dummy line to separate similar errors
			}
		}
		container._parser ??= this;
	}

	private static readonly Lock s_printOpLock = new();
	private static bool s_printOp = true;

	/// <summary>Print all metadata.</summary>
	public void LogScriptMetadata(bool haveEvents, string groupName)
	{
		if (Options.debug && Options.verboseLogging)
		{
			string argType(ArgEnum type) => GetTypeName(type).ToString();
			static string hex(int v) => v == 0 ? "0" : "0x" + v.ToString("x", CultureInfo.InvariantCulture);

			const int tabSize = 8;
			bool printOp;
			lock (s_printOpLock)
			{
				printOp = s_printOp;
				s_printOp = false;
			}
			if (printOp)
			{
				int offset = 0;
				var opLog = new StringBuilder();
				opLog.Append("Available built-in script operations:\n");
				foreach (var b in ScriptBuiltin.Procs)
				{
					var ver = ScriptBuiltin.Ver(b.group);
					var ot = b.group.OverloadType();
					if (ScriptParseHelper.ValidOverloadProc(ot) && b.description.Length != 0)
					{
						opLog.Append("Op:   ").Append(b.name.PadRight(tabSize * 2))
							.Append("OpId: ").Append(hex(offset).PadRight(tabSize / 2)).Append(" .. ").Append(hex(offset + ver - 1).PadRight(tabSize))
							.Append("Args: ").Append(ScriptParseHelper.DisplayOverloadProc(this, ot).PadRight(tabSize * 5))
							.Append("Desc: ").Append(b.description)
							.Append('\n');
					}
					offset += ver;
				}
				opLog.Append("Total size: ").Append(hex(offset)).Append('\n');
				Logger.Debug(opLog.ToString());
			}

			var refLog = new StringBuilder();
			refLog.Append("Script info for:  '").Append(_name).Append("'  in group:  '").Append(groupName).Append("'\n");
			refLog.Append('\n');
			if (haveEvents)
			{
				refLog.Append("Have global events\n");
				refLog.Append('\n');
			}
			if (_description.Length != 0)
			{
				refLog.Append("Description:\n");
				refLog.Append(_description).Append('\n');
				refLog.Append('\n');
			}
			if (_defaultScript.Length != 0)
			{
				refLog.Append("Script default implementation:\n");
				refLog.Append(_defaultScript).Append('\n');
				refLog.Append('\n');
			}
			if (_regOutSize > 0)
			{
				refLog.Append("Script return values:\n");
				for (int i = 0; i < _regOutSize; ++i)
				{
					var r = GetRef(_regOutName[i]);
					if (r != null)
					{
						refLog.Append("Name: ").Append(r.Value.name.ToString().PadRight(40)).Append(GetTypePrefix(r.Value.type).PadRight(9)).Append(' ').Append(argType(r.Value.type).PadRight(9)).Append('\n');
					}
				}
				if (_emptyReturn)
				{
					refLog.Append("In this script 'return' statement is empty, script returning values are edited directly\n");
				}
				refLog.Append('\n');
			}
			refLog.Append("Script data:\n");
			var temp = _refList.ToList();
			temp.Sort((a, b) => string.CompareOrdinal(a.name.ToString(), b.name.ToString()));
			foreach (var r in temp)
			{
				if ((!ArgIsReg(r.type) && !ArgIsPtr(r.type) && Logger.ReportingLevel != SeverityLevel.LOG_VERBOSE) || ArgBase(r.type) == ArgInvalid)
				{
					continue;
				}
				if (ArgBase(r.type) == ArgInt && !ArgIsReg(r.type))
				{
					refLog.Append("Name: ").Append(r.name.ToString().PadRight(40)).Append(GetTypePrefix(r.type).PadRight(9)).Append(' ').Append(argType(r.type).PadRight(9)).Append(' ').Append(hex(r.value.GetInt())).Append('\n');
				}
				else
				{
					refLog.Append("Name: ").Append(r.name.ToString().PadRight(40)).Append(GetTypePrefix(r.type).PadRight(9)).Append(' ').Append(argType(r.type).PadRight(9)).Append('\n');
				}
			}
			if (Logger.ReportingLevel != SeverityLevel.LOG_VERBOSE)
			{
				refLog.Append("To see const values and custom operations use 'verboseLogging'\n");
			}
			else
			{
				var tmp = _procList.ToList();
				tmp.Sort((a, b) => string.CompareOrdinal(a.name.ToString(), b.name.ToString()));

				refLog.Append('\n');
				refLog.Append("Script operations:\n");
				foreach (var p in tmp)
				{
					if (p.parserArg != null && p.HaveOverloadArg && p.description != new ScriptRef(BindBase.functionInvisible))
					{
						const int tabStop = 4; // alignment of next part
						const int minSpace = 2; // min space to next part

						var name = p.name.ToString();
						var nameTab = Math.Max((name.Length + minSpace + tabStop - 1) & -tabStop, 40);

						var a = ScriptParseHelper.DisplayOverloadProc(this, p.overloadArg);
						var argsTab = Math.Max((a.Length + minSpace + tabStop - 1) & -tabStop, 48);

						refLog.Append("Name: ").Append(name.PadRight(nameTab)).Append("Args: ").Append(a.PadRight(argsTab))
							.Append(p.description != new ScriptRef(BindBase.functionWithoutDescription) ? "Desc: " + p.description.ToString() + "\n" : "\n");
					}
				}
			}
			Logger.Debug(refLog.ToString());
		}
	}
}

/// <summary>Strong typed parser (C++ <c>ScriptParser&lt;ScriptOutputArgs&lt;OutputArgs...&gt;, Args...&gt;</c>).</summary>
public class ScriptParser : ScriptParserBase
{
	/// <summary>Constructor.</summary>
	public ScriptParser(ScriptGlobal shared, string name, ScriptArgName[] argOutputNames, ScriptArgName[] argNames) : base(shared, name)
	{
		AddRegImpl(true, argOutputNames);
		AddRegImpl(false, argNames);
	}

	/// <summary>Create container used by this parser.</summary>
	public virtual IScriptContainer CreateContainer() => new ScriptContainer();
}

/// <summary>Common base for strong typed event parser.</summary>
public class ScriptParserEventsBase : ScriptParserBase
{
	private const int EventsMax = 256;
	private const int OffsetScale = 100;
	private const int OffsetMax = 100 * OffsetScale;

	private sealed class EventData
	{
		public int offset;
		public string name = "";
		public ScriptContainerBase script = new();
	}

	/// <summary>Final list of events.</summary>
	private List<ScriptContainerBase> _events = new(EventsMax);
	/// <summary>Meta data of events.</summary>
	private readonly List<EventData> _eventsData = [];

	/// <summary>Constructor.</summary>
	public ScriptParserEventsBase(ScriptGlobal shared, string name) : base(shared, name)
	{
		_eventsData.Add(new EventData { offset = 0 });
	}

	/// <summary>Parse node and return new script.</summary>
	public void ParseNode(ScriptContainerEventsBase container, string type, YamlNodeReader reader)
	{
		ParseNode(container._current, type, reader);
		container._events = GetEvents();
		container._eventsParser = this;
	}

	/// <summary>Parse string and return new script.</summary>
	public void ParseCode(ScriptContainerEventsBase container, string type, string srcCode)
	{
		ParseCode(container._current, type, srcCode);
		container._events = GetEvents();
		container._eventsParser = this;
	}

	/// <summary>Load global data from YAML.</summary>
	public void LoadEvents(YamlNodeReader scripts)
	{
		// helper functions to get position in data vector
		int findPos(string n) => _eventsData.FindIndex(p => p.name == n);

		static string getLineFromNode(YamlNodeReader n) => n.LocationInFile.Line.ToString(CultureInfo.InvariantCulture);
		static string getDescriptionNode((string name, YamlNodeReader node, bool have) nn) => "'" + nn.name + "' at line " + getLineFromNode(nn.node);
		static string getNameFromNode((string name, YamlNodeReader node, bool have) nn)
		{
			var name = nn.node.ReadVal<string>();
			if (name.Length == 0)
			{
				throw new InvalidOperationException("Invalid name for " + getDescriptionNode(nn));
			}
			return name;
		}
		static (string, YamlNodeReader, bool) getNode(YamlNodeReader i, string nodeName)
		{
			var n = i[nodeName];
			return (nodeName, n, n.IsValid);
		}

		if (scripts)
		{
			foreach (var i in scripts.Children())
			{
				var deleteNode = getNode(i, "delete");
				var newNode = getNode(i, "new");
				var overrideNode = getNode(i, "override");
				var updateNode = getNode(i, "update");
				var ignoreNode = getNode(i, "ignore");

				// default name for case when use don't define node with name
				var name = "";

				{
					// check for duplicates
					(string, YamlNodeReader, bool)? last = null;
					foreach (var p in new[] { deleteNode, newNode, updateNode, overrideNode, ignoreNode })
					{
						if (p.Item3)
						{
							if (last != null)
							{
								throw new InvalidOperationException("Conflict of " + getDescriptionNode(last.Value) + " and " + getDescriptionNode(p));
							}
							else
							{
								last = p;
								name = getNameFromNode(p);
							}
						}
					}
				}

				if (deleteNode.Item3)
				{
					var it = findPos(name);
					if (it >= 0)
					{
						_eventsData.RemoveAt(it);
					}
					else
					{
						Logger.Warning("Unknown script name '" + name + "' for " + getDescriptionNode(deleteNode));
						Logger.Warning("    in " + GetGlobal().CurrentFile);
						Logger.Warning(""); // dummy line to separate similar errors
					}
				}
				else
				{
					var scp = new ScriptContainerBase();

					int offset = (int)(i["offset"].ReadVal<double>(0) * OffsetScale);
					if (offset == 0 || offset >= OffsetMax || offset <= -OffsetMax)
					{
						//TODO: make it a exception
						Logger.Error("Invalid offset for '" + Name + "' equal: '" + i["offset"].ReadVal<string>("") + "'");
						Logger.Error("    for node at line " + getLineFromNode(i["offset"]) + " in " + GetGlobal().CurrentFile);
						Logger.Error(""); // dummy line to separate similar errors
						continue;
					}

					{
						var nameWithPrefix = name.Length != 0 ? "Global:" + name : "Global off: " + i["offset"].ReadVal<string>();
						if (false == ParseBase(scp, nameWithPrefix, i["code"].ReadVal<string>("")))
						{
							Logger.Error("    for node with code at line " + getLineFromNode(i["code"]) + " in " + GetGlobal().CurrentFile);
							Logger.Error(""); // dummy line to separate similar errors
							continue;
						}
					}

					if (updateNode.Item3)
					{
						var it = findPos(name);
						if (it >= 0)
						{
							_eventsData[it].offset = offset;
							_eventsData[it].script = scp;
						}
						else
						{
							Logger.Warning("Unknown script name '" + name + "' for " + getDescriptionNode(updateNode));
							Logger.Warning("    in " + GetGlobal().CurrentFile);
							Logger.Warning(""); // dummy line to separate similar errors
						}
					}
					else if (overrideNode.Item3)
					{
						var it = findPos(name);
						if (it >= 0)
						{
							_eventsData[it].offset = offset;
							_eventsData[it].script = scp;
						}
						else
						{
							throw new InvalidOperationException("Unknown script name '" + name + "' for " + getDescriptionNode(overrideNode));
						}
					}
					else if (ignoreNode.Item3)
					{
						// nothing to see there...
					}
					else
					{
						if (newNode.Item3)
						{
							var it = findPos(name);
							if (it >= 0)
							{
								throw new InvalidOperationException("Script script name '" + name + "' already used for " + getDescriptionNode(newNode));
							}
						}
						_eventsData.Add(new EventData { name = name, offset = offset, script = scp });
					}
				}
			}
		}
	}

	/// <summary>Get events.</summary>
	public List<ScriptContainerBase> GetEvents() => _events;

	/// <summary>Release event data.</summary>
	public List<ScriptContainerBase> ReleseEvents()
	{
		// C++ std::sort, the port uses a stable sort
		var sorted = _eventsData.OrderBy(e => e.offset).ToList();
		_eventsData.Clear();
		_eventsData.AddRange(sorted);
		foreach (var e in _eventsData)
		{
			var reservedSpaceForZero = e.offset < 0;
			if (_events.Count + (reservedSpaceForZero ? 2 : 1) < EventsMax)
			{
				var c = new ScriptContainerBase();
				c.MoveFrom(e.script);
				_events.Add(c);
			}
			else
			{
				Logger.Error("Error in script parser '" + Name + "': global script limit reach");
				if (reservedSpaceForZero)
				{
					_events.Add(new ScriptContainerBase());
				}
				break;
			}
		}
		_events.Add(new ScriptContainerBase());
		var r = _events;
		_events = new List<ScriptContainerBase>(EventsMax);
		return r;
	}

	/// <summary>Create container used by this parser.</summary>
	public virtual IScriptContainer CreateContainer() => new ScriptContainerEvents();
}

/// <summary>Strong typed event parser (C++ <c>ScriptParserEvents&lt;ScriptOutputArgs&lt;OutputArgs...&gt;, Args...&gt;</c>).</summary>
public class ScriptParserEvents : ScriptParserEventsBase
{
	/// <summary>Constructor.</summary>
	public ScriptParserEvents(ScriptGlobal shared, string name, ScriptArgName[] argOutputNames, ScriptArgName[] argNames) : base(shared, name)
	{
		AddRegImpl(true, argOutputNames);
		AddRegImpl(false, argNames);
	}
}

////////////////////////////////////////////////////////////
//					script groups
////////////////////////////////////////////////////////////

/// <summary>Group of named parsers (C++ <c>ScriptGroup&lt;Master, Parsers...&gt;</c>).</summary>
public class ScriptGroup
{
	private readonly HashSet<string> _allParserNames = new(StringComparer.Ordinal);
	private readonly List<ScriptParserBase> _parsers = [];

	/// <summary>Parsers in group order.</summary>
	public IReadOnlyList<ScriptParserBase> Parsers => _parsers;

	/// <summary>Add parsers (C++ constructor of ScriptGroup).</summary>
	protected void Init(ScriptGlobal shared, string groupName, params ScriptParserBase[] parsers)
	{
		foreach (var p in parsers)
		{
			shared.PushParser(groupName, p);
		}
		foreach (var p in parsers)
		{
			_allParserNames.Add(p.Name);
			_parsers.Add(p);
		}
	}

	/// <summary>Check if exists parser for given name.</summary>
	public bool IsKnowParserName(string view) => _allParserNames.Contains(view);

	/// <summary>Create containers for all parsers.</summary>
	public ScriptGroupContainer CreateContainer()
	{
		var r = new IScriptContainer[_parsers.Count];
		for (int i = 0; i < r.Length; ++i)
		{
			r[i] = _parsers[i] is ScriptParserEventsBase ? new ScriptContainerEvents() : new ScriptContainer();
		}
		return new ScriptGroupContainer(r);
	}
}

/// <summary>Containers of one script group (C++ <c>ScriptGroupContainer</c>).</summary>
public sealed class ScriptGroupContainer
{
	private readonly IScriptContainer[] _containers;

	internal ScriptGroupContainer(IScriptContainer[] c)
	{
		_containers = c;
	}

	/// <summary>Get container by parser index.</summary>
	public IScriptContainer Get(int i) => _containers[i];

	/// <summary>Load scripts.</summary>
	public void Load(string type, YamlNodeReader reader, ScriptGroup parsers)
	{
		var scripts = reader["scripts"];
		if (scripts)
		{
			if (scripts.HasNullVal == false && scripts.IsMap == false)
			{
				throw new InvalidOperationException("Wrong type of 'scripts' node at line " + scripts.LocationInFile.Line);
			}

			foreach (var p in scripts.Children())
			{
				var key = p.Key;
				if (key.Length > 0 && key[^1] == '#')
				{
					continue;
				}

				if (parsers.IsKnowParserName(key) == false)
				{
					throw new InvalidOperationException("Unknown '" + key + "' node in 'scripts' at line " + p.LocationInFile.Line);
				}
			}
		}

		for (int i = 0; i < _containers.Length; ++i)
		{
			_containers[i].LoadContainer(type, reader, parsers.Parsers[i]); //TODO: some scripts need called even if "scripts" is not present
		}
	}
}
