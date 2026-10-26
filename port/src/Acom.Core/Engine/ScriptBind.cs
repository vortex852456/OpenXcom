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
// Ported from: src/Engine/ScriptBind.h
//
// C++ derives the script signature of a bound function from its C++ parameter types with templates
// (helper::ArgSelector, FuncGroup, BindFunc, BindPropGet, ...). The port describes each parameter with a
// ScriptParam that has the same accepted argument kinds in the same order, and the body is a delegate that
// reads/writes the arguments through ScriptArgs. Names and signatures of registered functions stay identical.

#pragma warning disable CA1000 // static members on generic types mirror C++ templates.
#pragma warning disable CA1711 // C++ names (ArgEnum, RegEnum, ScriptImpl, ...) are kept.
#pragma warning disable CA1715 // C++ template parameter names are kept.
#pragma warning disable CA1720 // script type names (Int, Ptr) are kept.
#pragma warning disable CA1822 // members mirror C++ instance members.
#pragma warning disable CA1815 // value types mirror C++ PODs, equality is not used.
#pragma warning disable CA1034 // nested types mirror C++ nested types.

using System.Runtime.CompilerServices;
using static Acom.ScriptArg;

namespace Acom;

/// <summary>Type of code block.</summary>
public enum BlockEnum
{
	/// <summary>Block that represents whole body of script.</summary>
	BlockMain,
	/// <summary>Normal block.</summary>
	BlockBegin,
	/// <summary>Conditional block.</summary>
	BlockIf,
	/// <summary>Else block of conditional block.</summary>
	BlockElse,
	/// <summary>Iteraion block.</summary>
	BlockLoop,
}

/// <summary>Accessor to the arguments of one executed operation (C++ arguments decoded from the proc vector).</summary>
public ref struct ScriptArgs
{
	private readonly ScriptWorkerBase _w;
	private readonly ScriptOperand[] _ops;
	private readonly ref int _pos;

	public ScriptArgs(ScriptWorkerBase w, ScriptOperand[] ops, ref int pos)
	{
		_w = w;
		_ops = ops;
		_pos = ref pos;
	}

	/// <summary>Worker executing script (C++ <c>ScriptWorkerBase&amp;</c> argument).</summary>
	public readonly ScriptWorkerBase Worker => _w;

	/// <summary>Position of next operation (C++ <c>ProgPos&amp;</c> argument).</summary>
	public readonly ref int Prog => ref _pos;

	/// <summary>Read int (reg or const).</summary>
	public readonly int Int(int i)
	{
		ref var o = ref _ops[i];
		return o.kind == ScriptOperandKind.Reg ? _w.RegI[(int)o.reg] : o.i;
	}

	/// <summary>Int variable.</summary>
	public readonly ref int IntRef(int i) => ref _w.RegI[(int)_ops[i].reg];

	/// <summary>Label argument (instruction index).</summary>
	public readonly int Label(int i) => _ops[i].i;

	/// <summary>Read pointer (reg, const or null).</summary>
	public readonly T? Ptr<T>(int i) where T : class
	{
		ref var o = ref _ops[i];
		return (T?)(o.kind == ScriptOperandKind.Reg ? _w.RegO[(int)o.reg] : o.o);
	}

	/// <summary>Read raw object (reg, const or null).</summary>
	public readonly object? Obj(int i)
	{
		ref var o = ref _ops[i];
		return o.kind == ScriptOperandKind.Reg ? _w.RegO[(int)o.reg] : o.o;
	}

	/// <summary>Object variable (pointers, texts).</summary>
	public readonly ref object? ObjRef(int i) => ref _w.RegO[(int)_ops[i].reg];

	/// <summary>Set pointer variable.</summary>
	public readonly void SetPtr(int i, object? v) => _w.RegO[(int)_ops[i].reg] = v;

	/// <summary>Read text (reg or literal).</summary>
	public readonly ScriptText Text(int i)
	{
		ref var o = ref _ops[i];
		return new ScriptText((string?)(o.kind == ScriptOperandKind.Reg ? _w.RegO[(int)o.reg] : o.o));
	}

	/// <summary>Read text as std::string (null gives "").</summary>
	public readonly string Str(int i) => Text(i).ToString();

	/// <summary>Set text variable.</summary>
	public readonly void SetText(int i, ScriptText t) => _w.RegO[(int)_ops[i].reg] = t.ptr;

	/// <summary>Read value type (reg or const).</summary>
	public readonly T Value<T>(int i) where T : struct
	{
		ref var o = ref _ops[i];
		var v = o.kind == ScriptOperandKind.Reg ? _w.RegO[(int)o.reg] : o.o;
		return v is T t ? t : default;
	}

	/// <summary>Value type variable.</summary>
	public readonly ref T ValueRef<T>(int i) where T : struct => ref ScriptBox.RefValue<T>(ref _w.RegO[(int)_ops[i].reg]);

	/// <summary>Read tag (const, reg or null).</summary>
	public readonly ScriptTag<T> Tag<T>(int i) => new(unchecked((ushort)Int(i)));

	/// <summary>Tag variable.</summary>
	public readonly ref int TagRef(int i) => ref _w.RegI[(int)_ops[i].reg];
}

/// <summary>Body of a bound function that always continues (C++ bound functions returning void).</summary>
public delegate void ScriptAction(ref ScriptArgs a);

/// <summary>Kind of one accepted argument alternative (C++ ArgRegDef, ArgValueDef, ...).</summary>
public enum ScriptArgDefKind : byte
{
	Reg,
	Value,
	Label,
	Text,
	Null,
	Sep,
	Func,
	Raw,
}

/// <summary>C++ parameter type of a bound function.</summary>
public enum ScriptParamKind : byte
{
	/// <summary><c>int</c>, <c>bool</c>, <c>const int&amp;</c></summary>
	Int,
	/// <summary><c>int&amp;</c></summary>
	IntRef,
	/// <summary><c>ScriptText</c>, <c>const std::string&amp;</c></summary>
	Text,
	/// <summary><c>ScriptText&amp;</c></summary>
	TextRef,
	/// <summary><c>const T*</c></summary>
	Ptr,
	/// <summary><c>T*</c></summary>
	PtrE,
	/// <summary><c>const T*&amp;</c></summary>
	PtrRef,
	/// <summary><c>T*&amp;</c></summary>
	PtrERef,
	/// <summary><c>ScriptTag&lt;T&gt;</c></summary>
	Tag,
	/// <summary><c>ScriptTag&lt;T&gt;&amp;</c></summary>
	TagRef,
	/// <summary>Value type <c>T</c> (e.g. Position)</summary>
	Value,
	/// <summary>Value type <c>T&amp;</c></summary>
	ValueRef,
	/// <summary><c>ScriptWorkerBase&amp;</c></summary>
	Context,
	/// <summary><c>ProgPos&amp;</c></summary>
	Prog,
	/// <summary><c>ProgPos</c></summary>
	Label,
	/// <summary><c>ScriptNull</c></summary>
	Null,
	/// <summary><c>ScriptArgSeparator</c></summary>
	Sep,
	/// <summary><c>ScriptFunc</c> (internal of <c>call</c>)</summary>
	Func,
	/// <summary><c>const Uint8*</c> (internal of <c>call</c>)</summary>
	Raw,
}

/// <summary>Registry of types that have script bindings (C++ SFINAE on <c>T::ScriptRegister</c>).</summary>
public static class ScriptBindTable
{
	private static readonly Lock s_lock = new();
	private static readonly Dictionary<Type, (string name, Action<ScriptParserBase> register)> s_types = [];

	static ScriptBindTable()
	{
		ModScript.RegisterBindTypes();
	}

	/// <summary>Add bindable type (C++ class with <c>ScriptName</c> and <c>ScriptRegister</c>).</summary>
	public static void Add<T>(string scriptName, Action<ScriptParserBase> scriptRegister) where T : class
	{
		lock (s_lock)
		{
			s_types[typeof(T)] = (scriptName, scriptRegister);
		}
	}

	/// <summary>Find bindable type.</summary>
	public static bool TryGet(Type t, out string name, out Action<ScriptParserBase> register)
	{
		lock (s_lock)
		{
			if (s_types.TryGetValue(t, out var r))
			{
				name = r.name;
				register = r.register;
				return true;
			}
		}
		name = "";
		register = null!;
		return false;
	}
}

/// <summary>One parameter of a script function or script argument (C++ parameter type + helper::ArgSelector).</summary>
public sealed class ScriptParam
{
	public readonly ScriptParamKind Kind;
	/// <summary>Type for pointers, tags (parent type) and values.</summary>
	public readonly Type? ClrType;
	private readonly TypeInfo _valueMeta;
	private (ScriptArgDefKind kind, ArgEnum type)[]? _alternatives;
	private ArgEnum? _argType;

	private ScriptParam(ScriptParamKind k, Type? t = null, TypeInfo meta = default)
	{
		Kind = k;
		ClrType = t;
		_valueMeta = meta;
	}

	private static readonly ScriptParam s_int = new(ScriptParamKind.Int);
	private static readonly ScriptParam s_intRef = new(ScriptParamKind.IntRef);
	private static readonly ScriptParam s_text = new(ScriptParamKind.Text);
	private static readonly ScriptParam s_textRef = new(ScriptParamKind.TextRef);
	private static readonly ScriptParam s_context = new(ScriptParamKind.Context);
	private static readonly ScriptParam s_prog = new(ScriptParamKind.Prog);
	private static readonly ScriptParam s_label = new(ScriptParamKind.Label);
	private static readonly ScriptParam s_null = new(ScriptParamKind.Null);
	private static readonly ScriptParam s_sep = new(ScriptParamKind.Sep);
	private static readonly ScriptParam s_func = new(ScriptParamKind.Func);
	private static readonly ScriptParam s_raw = new(ScriptParamKind.Raw);

	/// <summary><c>int</c> (also <c>bool</c>, <c>const int&amp;</c>).</summary>
	public static ScriptParam Int => s_int;
	/// <summary><c>int&amp;</c>.</summary>
	public static ScriptParam IntRef => s_intRef;
	/// <summary><c>ScriptText</c> or <c>const std::string&amp;</c>.</summary>
	public static ScriptParam Text => s_text;
	/// <summary><c>ScriptText&amp;</c>.</summary>
	public static ScriptParam TextRef => s_textRef;
	/// <summary><c>ScriptWorkerBase&amp;</c>.</summary>
	public static ScriptParam Context => s_context;
	/// <summary><c>ProgPos&amp;</c>.</summary>
	public static ScriptParam Prog => s_prog;
	/// <summary><c>ProgPos</c>.</summary>
	public static ScriptParam Label => s_label;
	/// <summary><c>ScriptNull</c>.</summary>
	public static ScriptParam Null => s_null;
	/// <summary><c>ScriptArgSeparator</c>.</summary>
	public static ScriptParam Sep => s_sep;
	/// <summary><c>ScriptFunc</c>.</summary>
	public static ScriptParam Func => s_func;
	/// <summary><c>const Uint8*</c>.</summary>
	public static ScriptParam Raw => s_raw;

	private static readonly Lock s_cacheLock = new();
	private static readonly Dictionary<(ScriptParamKind, Type), ScriptParam> s_cache = [];

	private static ScriptParam Cached(ScriptParamKind k, Type t, TypeInfo meta = default)
	{
		lock (s_cacheLock)
		{
			if (!s_cache.TryGetValue((k, t), out var p))
			{
				p = new ScriptParam(k, t, meta);
				s_cache.Add((k, t), p);
			}
			return p;
		}
	}

	/// <summary><c>const T*</c>.</summary>
	public static ScriptParam Ptr<T>() where T : class => Cached(ScriptParamKind.Ptr, typeof(T));
	/// <summary><c>T*</c>.</summary>
	public static ScriptParam PtrE<T>() where T : class => Cached(ScriptParamKind.PtrE, typeof(T));
	/// <summary><c>const T*&amp;</c>.</summary>
	public static ScriptParam PtrRef<T>() where T : class => Cached(ScriptParamKind.PtrRef, typeof(T));
	/// <summary><c>T*&amp;</c>.</summary>
	public static ScriptParam PtrERef<T>() where T : class => Cached(ScriptParamKind.PtrERef, typeof(T));
	/// <summary><c>ScriptTag&lt;T&gt;</c>.</summary>
	public static ScriptParam Tag<T>() => Cached(ScriptParamKind.Tag, typeof(T));
	/// <summary><c>ScriptTag&lt;T&gt;&amp;</c>.</summary>
	public static ScriptParam TagRef<T>() => Cached(ScriptParamKind.TagRef, typeof(T));
	/// <summary>Value type <c>T</c>.</summary>
	public static ScriptParam Value<T>() where T : struct => Cached(ScriptParamKind.Value, typeof(T), ValueMeta<T>());
	/// <summary>Value type <c>T&amp;</c>.</summary>
	public static ScriptParam ValueRef<T>() where T : struct => Cached(ScriptParamKind.ValueRef, typeof(T), ValueMeta<T>());

	/// <summary>Size and alignment of a value type like the C++ POD (e.g. Position: 3 x Sint16).</summary>
	public static TypeInfo ValueMeta<T>() where T : struct
	{
		int size = Unsafe.SizeOf<T>();
		int align = 1;
		while (align < 8 && size % (align * 2) == 0)
		{
			align *= 2;
		}
		return new TypeInfo(size, align);
	}

	/// <summary>Run time type of the C++ base type (pointee for pointers).</summary>
	private ArgEnum BaseType => Kind switch
	{
		ScriptParamKind.Int or ScriptParamKind.IntRef => ArgInt,
		ScriptParamKind.Text or ScriptParamKind.TextRef => ArgText,
		ScriptParamKind.Ptr or ScriptParamKind.PtrE or ScriptParamKind.PtrRef or ScriptParamKind.PtrERef => ArgRegisteType(ClrType!),
		ScriptParamKind.Tag or ScriptParamKind.TagRef => ArgRegisteType(typeof(ScriptTag<>).MakeGenericType(ClrType!)),
		ScriptParamKind.Value or ScriptParamKind.ValueRef => ArgRegisteType(ClrType!),
		ScriptParamKind.Label => ArgLabel,
		ScriptParamKind.Null => ArgNull,
		ScriptParamKind.Sep => ArgSep,
		_ => ArgInvalid,
	};

	/// <summary>C++ <c>ScriptParserBase::getArgType&lt;T&gt;()</c> of the parameter type.</summary>
	public ArgEnum ArgType
	{
		get
		{
			if (_argType is { } t)
			{
				return t;
			}
			var spec = ArgSpecNone;
			if (IsRef) spec |= ArgSpecVar;
			if (Kind is ScriptParamKind.Ptr or ScriptParamKind.PtrRef) spec |= ArgSpecPtr;
			if (Kind is ScriptParamKind.PtrE or ScriptParamKind.PtrERef) spec |= ArgSpecPtrE;
			var r = ArgSpecAdd(BaseType, spec);
			_argType = r;
			return r;
		}
	}

	/// <summary>Is C++ reference.</summary>
	public bool IsRef => Kind is ScriptParamKind.IntRef or ScriptParamKind.TextRef or ScriptParamKind.PtrRef or ScriptParamKind.PtrERef or ScriptParamKind.TagRef or ScriptParamKind.ValueRef;

	/// <summary>Is output (non-const reference).</summary>
	public bool IsOutput => IsRef;

	/// <summary>Is internal argument not visible in script (C++ ArgInternal).</summary>
	public bool IsInternal => Kind is ScriptParamKind.Context or ScriptParamKind.Prog;

	/// <summary>Value is stored as int in registers.</summary>
	public bool IntStorage => Kind is ScriptParamKind.Int or ScriptParamKind.IntRef or ScriptParamKind.Tag or ScriptParamKind.TagRef;

	/// <summary>Convert value to int register storage.</summary>
	internal int ToIntStorage(object value) => value switch
	{
		int i => i,
		IScriptTag t => t.Index,
		_ => Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture),
	};

	/// <summary>Convert int register storage to value.</summary>
	internal object FromIntStorage(int v) => v;

	/// <summary>Size of the decayed C++ type (C++ <c>TypeInfoImpl::metaBase</c>).</summary>
	public TypeInfo MetaBase => Kind switch
	{
		ScriptParamKind.Int or ScriptParamKind.IntRef => new TypeInfo(4, 4),
		ScriptParamKind.Tag or ScriptParamKind.TagRef => new TypeInfo(2, 2),
		ScriptParamKind.Value or ScriptParamKind.ValueRef => _valueMeta,
		ScriptParamKind.Sep => new TypeInfo(1, 1),
		_ => TypeInfo.PtrTypeInfo,
	};

	/// <summary>Accepted argument alternatives in C++ order (C++ <c>helper::ArgSelector</c>).</summary>
	public (ScriptArgDefKind kind, ArgEnum type)[] Alternatives
	{
		get
		{
			if (_alternatives != null)
			{
				return _alternatives;
			}
			var t = ArgType;
			var regType = ArgSpecAdd(t, ArgSpecReg);
			(ScriptArgDefKind, ArgEnum)[] r = Kind switch
			{
				ScriptParamKind.Int => [(ScriptArgDefKind.Value, t), (ScriptArgDefKind.Reg, regType)],
				ScriptParamKind.Text => [(ScriptArgDefKind.Text, ArgText), (ScriptArgDefKind.Reg, regType)],
				ScriptParamKind.Ptr or ScriptParamKind.PtrE => [(ScriptArgDefKind.Reg, regType), (ScriptArgDefKind.Value, t), (ScriptArgDefKind.Null, ArgNull)],
				ScriptParamKind.Tag => [(ScriptArgDefKind.Value, t), (ScriptArgDefKind.Reg, regType), (ScriptArgDefKind.Null, ArgNull)],
				ScriptParamKind.Value => [(ScriptArgDefKind.Reg, regType), (ScriptArgDefKind.Value, t)],
				ScriptParamKind.IntRef or ScriptParamKind.TextRef or ScriptParamKind.PtrRef or ScriptParamKind.PtrERef or ScriptParamKind.TagRef or ScriptParamKind.ValueRef
					=> [(ScriptArgDefKind.Reg, regType)],
				ScriptParamKind.Label => [(ScriptArgDefKind.Label, ArgLabel)],
				ScriptParamKind.Null => [(ScriptArgDefKind.Null, ArgNull)],
				ScriptParamKind.Sep => [(ScriptArgDefKind.Sep, ArgSep)],
				ScriptParamKind.Func => [(ScriptArgDefKind.Func, ArgInvalid)],
				ScriptParamKind.Raw => [(ScriptArgDefKind.Raw, ArgInvalid), (ScriptArgDefKind.Raw, ArgInvalid), (ScriptArgDefKind.Raw, ArgInvalid), (ScriptArgDefKind.Raw, ArgInvalid), (ScriptArgDefKind.Raw, ArgInvalid), (ScriptArgDefKind.Raw, ArgInvalid)],
				_ => [],
			};
			_alternatives = r;
			return r;
		}
	}

	/// <summary>Register pointer type if it has bindings (C++ <c>addTypeImpl</c>).</summary>
	internal void AddTypeImpl(ScriptParserBase parser)
	{
		if (Kind is ScriptParamKind.Ptr or ScriptParamKind.PtrE or ScriptParamKind.PtrRef or ScriptParamKind.PtrERef or ScriptParamKind.Tag or ScriptParamKind.TagRef)
		{
			if (ScriptBindTable.TryGet(ClrType!, out var name, out var register))
			{
				parser.RegisterPointerType(ClrType!, name, register);
			}
		}
	}
}

/// <summary>Name and type of a script argument (C++ <c>helper::ArgName&lt;T&gt;</c>).</summary>
public readonly record struct ScriptArgName(string name, ScriptParam type);

/// <summary>Bound function: parameter list and body (C++ <c>helper::FuncGroup&lt;Func&gt;</c>).</summary>
public sealed class ScriptFuncGroup
{
	public readonly ScriptParam[] Params;
	public readonly ScriptImpl Impl;

	public ScriptFuncGroup(ScriptParam[] p, ScriptImpl impl)
	{
		Params = p;
		Impl = impl;
	}

	public ScriptFuncGroup(ScriptParam[] p, ScriptAction impl)
	{
		Params = p;
		Impl = (ref ScriptArgs a) =>
		{
			impl(ref a);
			return RetContinue;
		};
	}

	/// <summary>Types accepted at each argument position, empty for internal ones.</summary>
	public ArgEnum[][] OverloadType()
	{
		var r = new ArgEnum[Params.Length][];
		for (int i = 0; i < Params.Length; ++i)
		{
			if (Params[i].IsInternal)
			{
				r[i] = [];
			}
			else
			{
				var alt = Params[i].Alternatives;
				r[i] = new ArgEnum[alt.Length];
				for (int j = 0; j < alt.Length; ++j)
				{
					r[i][j] = alt[j].type;
				}
			}
		}
		return r;
	}

	/// <summary>Parse arguments and push them to current operation.</summary>
	/// <returns>Zero or -1 on error.</returns>
	public int Parse(ParserWriter ph, ReadOnlySpan<ScriptRefData> args)
	{
		int curr = 0;
		foreach (var p in Params)
		{
			if (p.IsInternal)
			{
				ph.PushOperand(new ScriptOperand { kind = ScriptOperandKind.None }, 0);
				continue;
			}
			if (curr == args.Length)
			{
				Logger.Error("Not enough args in operation");
				return -1;
			}
			var t = args[curr];
			bool ok = false;
			foreach (var (kind, type) in p.Alternatives)
			{
				if (ph.TryPushArg(kind, type, t))
				{
					ok = true;
					break;
				}
			}
			if (!ok)
			{
				ph.LogDump(t);
				return -1;
			}
			++curr;
		}
		//we should have used all available tokens.
		if (curr != args.Length)
		{
			Logger.Error("Too many args in operation");
			return -1;
		}
		return 0;
	}
}

/// <summary>Helper structure used by ScriptParser::parse.</summary>
public sealed class ParserWriter
{
	public sealed class Block
	{
		public BlockEnum type;
		public int regStackSizeFrom;
		public RegEnum regIndexUsedFrom;
		public ScriptRefData nextLabel;
		public ScriptRefData finalLabel;
	}

	/// <summary>List of all places in proc vector where we need have same values.</summary>
	private sealed class ReservedCrossRefrenece<TComp>(ArgEnum refType)
	{
		/// <summary>list of places of usage.</summary>
		private readonly List<(ScriptInstr instr, int op, int index)> positions = [];
		/// <summary>list of final values.</summary>
		private readonly List<TComp> values = [];

		public ScriptValueData AddValue(TComp defaultValue)
		{
			var index = values.Count;
			values.Add(defaultValue);
			return new ScriptValueData(refType, 8, index, null);
		}

		public TComp GetValue(ScriptValueData data) => values[data.GetIntOfType(refType)];

		public void SetValue(ScriptValueData data, TComp value) => values[data.GetIntOfType(refType)] = value;

		public void PushPosition(ParserWriter pw, ScriptValueData data, ScriptOperandKind kind)
		{
			var index = data.GetIntOfType(refType);
			var (instr, op) = pw.PushOperand(new ScriptOperand { kind = kind }, 8);
			positions.Add((instr, op, index));
		}

		public void ForEachPosition(Action<ScriptInstr, int, TComp> f)
		{
			foreach (var pos in positions)
			{
				f(pos.instr, pos.op, values[pos.index]);
			}
		}
	}

	/// <summary>Label reference type (C++ <c>ReservedCrossRefrenece&lt;ProgPos&gt;::Ref</c>).</summary>
	private readonly struct LabelRef;
	/// <summary>Text reference type (C++ <c>ReservedCrossRefrenece&lt;ScriptText, ScriptRef&gt;::Ref</c>).</summary>
	private readonly struct TextRef;

	/// <summary>Operations of script.</summary>
	private readonly List<ScriptInstr> _instrs = [];
	/// <summary>Container that will be fill with script data.</summary>
	public readonly ScriptContainerBase container;
	/// <summary>All available data for script.</summary>
	public readonly ScriptParserBase parser;
	/// <summary>List of labels.</summary>
	private readonly ReservedCrossRefrenece<int> refLabels = new(ArgRegisteType<LabelRef>());
	/// <summary>List of texts.</summary>
	private readonly ReservedCrossRefrenece<ScriptRef> refTexts = new(ArgRegisteType<TextRef>());

	/// <summary>Index of used script registers.</summary>
	public RegEnum regIndexUsed;

	/// <summary>Stack of registers limited to code blocks.</summary>
	public readonly List<ScriptRefData> regStack = [];
	/// <summary>Store position of blocks of code like "if" or "while".</summary>
	public readonly List<Block> codeBlocks = [];

	/// <summary>Constructor.</summary>
	/// <param name="regUsed">current used reg space.</param>
	/// <param name="c">container that will be fill with script data.</param>
	/// <param name="d">parser having all meta data.</param>
	public ParserWriter(int regUsed, ScriptContainerBase c, ScriptParserBase d)
	{
		container = c;
		parser = d;
		regIndexUsed = (RegEnum)regUsed;
		PushScopeBlock(BlockEnum.BlockMain);
	}

	/// <summary>Function finalizing parsing of script.</summary>
	public void Relese()
	{
		PushProc(ScriptBuiltin.ExitImpl);
		refLabels.ForEachPosition(
			(instr, op, value) =>
			{
				if (value == ProgPos.Unknown)
				{
					throw new InvalidOperationException("Incorrect label position reference");
				}
				var o = instr.building[op];
				o.i = value;
				instr.building[op] = o;
			}
		);

		refTexts.ForEachPosition(
			(instr, op, value) =>
			{
				var s = value.Source!;
				//check beginning of string
				int begin = value.Begin;
				if (begin == value.End || s[begin] != '"')
				{
					throw new InvalidOperationException("Invalid Text: >>" + value.ToString() + "<<");
				}

				//check end of string
				int end = value.End - 1;
				if (begin == end || s[end] != '"')
				{
					throw new InvalidOperationException("Invalid Text: >>" + value.ToString() + "<<");
				}

				++begin;
				bool escape = false;
				var sb = new System.Text.StringBuilder();
				while (begin != end)
				{
					if (escape == true)
					{
						escape = false;
					}
					else
					{
						if (s[begin] == '\\')
						{
							escape = true;
							++begin;
							continue;
						}
					}
					sb.Append(s[begin]);
					++begin;
				}

				var o = instr.building[op];
				o.o = sb.ToString();
				instr.building[op] = o;
			}
		);

		foreach (var i in _instrs)
		{
			i.ops = [.. i.building];
			i.building.Clear();
		}
		container._proc = [.. _instrs];
		container._parser = parser;
	}

	/// <summary>Returns reference based on name.</summary>
	public ScriptRefData GetReferece(ScriptRef s)
	{
		ScriptRefData? ptr = null;
		foreach (var r in regStack)
		{
			if (r.name == s)
			{
				ptr = r;
				break;
			}
		}
		ptr ??= parser.GetRef(s);
		ptr ??= parser.GetGlobal().GetRef(s);
		return ptr ?? default;
	}

	/// <summary>Get current position in proc vector.</summary>
	public int GetCurrPos() => _instrs.Count;

	/// <summary>Push operand to current operation.</summary>
	internal (ScriptInstr instr, int op) PushOperand(ScriptOperand o, int bytes)
	{
		var instr = _instrs[^1];
		instr.building.Add(o);
		instr.argBytes += bytes;
		return (instr, instr.building.Count - 1);
	}

	/// <summary>Pushing proc operation on proc vector.</summary>
	public ScriptInstr PushProc(ScriptImpl impl)
	{
		var i = new ScriptInstr(impl);
		_instrs.Add(i);
		return i;
	}

	/// <summary>Try push one argument alternative.</summary>
	internal bool TryPushArg(ScriptArgDefKind kind, ArgEnum type, ScriptRefData t)
	{
		switch (kind)
		{
			case ScriptArgDefKind.Reg:
				return PushRegTry(t, type);
			case ScriptArgDefKind.Value:
				return PushConstTry(t, type);
			case ScriptArgDefKind.Label:
				return PushLabelTry(t);
			case ScriptArgDefKind.Text:
				return PushTextTry(t);
			case ScriptArgDefKind.Null:
				if (t.type == ArgNull)
				{
					PushOperand(new ScriptOperand { kind = ScriptOperandKind.Null }, 0);
					return true;
				}
				return false;
			case ScriptArgDefKind.Sep:
				if (t.type == ArgSep)
				{
					PushOperand(new ScriptOperand { kind = ScriptOperandKind.None }, 0);
					return true;
				}
				return false;
			default:
				return false;
		}
	}

	/// <summary>Try pushing label arg on proc vector. Can't use this to create loop back label.</summary>
	public bool PushLabelTry(ScriptRefData data)
	{
		var temp = data;
		if (!temp.IsValid && temp.name.IsValid)
		{
			return false;
		}
		if (temp.type != ArgLabel)
		{
			return false;
		}

		// Can't use this to create loop back named label.
		if (temp.name.IsValid && refLabels.GetValue(temp.value) != ProgPos.Unknown)
		{
			return false;
		}
		refLabels.PushPosition(this, temp.value, ScriptOperandKind.Label);
		return true;
	}

	/// <summary>Create new label definition for proc vector.</summary>
	public ScriptRefData AddLabel(ScriptRef name = default)
	{
		return new ScriptRefData(name, ArgLabel, refLabels.AddValue(ProgPos.Unknown));
	}

	/// <summary>Setting offset of label on proc vector.</summary>
	public bool SetLabel(ScriptRefData data, int offset)
	{
		var temp = data;
		if (!temp.IsValid && temp.name.IsValid)
		{
			return false;
		}
		if (temp.type != ArgLabel)
		{
			return false;
		}

		if (refLabels.GetValue(temp.value) != ProgPos.Unknown)
		{
			return false;
		}
		refLabels.SetValue(temp.value, offset);
		return true;
	}

	/// <summary>Label value equality (C++ compare of label index).</summary>
	internal static bool SameLabel(ScriptRefData a, ScriptRefData b) => a.value.i == b.value.i && a.value.type == b.value.type;

	/// <summary>Try pushing text literal arg on proc vector.</summary>
	public bool PushTextTry(ScriptRefData data)
	{
		if (data.IsValid && data.type == ArgText)
		{
			refTexts.PushPosition(this, refTexts.AddValue(data.value.GetRef()), ScriptOperandKind.Text);
			return true;
		}
		return false;
	}

	/// <summary>Try pushing data arg on proc vector.</summary>
	public bool PushConstTry(ScriptRefData data, ArgEnum type)
	{
		if (data.IsValid && data.type == type && !ArgIsReg(data.type) && data.value.type == type)
		{
			PushOperand(new ScriptOperand { kind = ScriptOperandKind.Value, i = data.value.i, o = data.value.o }, data.value.size);
			return true;
		}
		return false;
	}

	/// <summary>Try pushing reg arg on proc vector.</summary>
	public bool PushRegTry(ScriptRefData data, ArgEnum type)
	{
		type = ArgSpecAdd(type, ArgSpecReg);
		if (data.IsValid && ArgCompatible(type, data.type, 0) != 0 && data.GetReg() != RegInvalid)
		{
			PushOperand(new ScriptOperand { kind = ScriptOperandKind.Reg, reg = data.GetReg() }, 2);
			return true;
		}
		return false;
	}

	/// <summary>Add new reg arg definition.</summary>
	/// <param name="s">optional name of reg</param>
	/// <param name="type">type of reg</param>
	/// <returns>Reg data</returns>
	public ScriptRefData AddReg(ScriptRef s, ArgEnum type)
	{
		if (s.IsValid && GetReferece(s).IsValid)
		{
			return default;
		}

		type = ArgSpecAdd(type, ArgSpecReg);
		var meta = ScriptParserBase.GetRegMeta(parser, type);
		if (!meta.IsValid)
		{
			return default;
		}
		if (meta.NeedRegSpace((int)regIndexUsed) > ScriptMaxReg)
		{
			return default;
		}
		var data = new ScriptRefData(s, type, ScriptValueData.FromReg((RegEnum)meta.NextRegPos((int)regIndexUsed)));
		if (s.IsValid)
		{
			regStack.Add(data);
		}
		regIndexUsed = (RegEnum)meta.NeedRegSpace((int)regIndexUsed);
		return data;
	}

	/// <summary>Add new local const definition.</summary>
	public ScriptRefData AddConst(ScriptRef s, ArgEnum type, ScriptValueData value)
	{
		if (!s.IsValid)
		{
			return default;
		}

		if (GetReferece(s).IsValid)
		{
			return default;
		}

		if (ArgIsReg(type))
		{
			return default;
		}

		var meta = ScriptParserBase.GetRegMeta(parser, type);
		if (!meta.IsValid)
		{
			return default;
		}

		var data = new ScriptRefData(s, type, value);

		regStack.Add(data);

		return data;
	}

	/// <summary>Add new code scope.</summary>
	public Block PushScopeBlock(BlockEnum type)
	{
		var b = new Block { type = type, regStackSizeFrom = regStack.Count, regIndexUsedFrom = regIndexUsed };
		codeBlocks.Add(b);
		return b;
	}

	/// <summary>Clear values in code scope.</summary>
	public Block ClearScopeBlock()
	{
		var current = codeBlocks[^1];
		regStack.RemoveRange(current.regStackSizeFrom, regStack.Count - current.regStackSizeFrom);
		regIndexUsed = current.regIndexUsedFrom;
		return current;
	}

	/// <summary>Pop code scope.</summary>
	public Block PopScopeBlock()
	{
		if (codeBlocks[^1].type == BlockEnum.BlockMain)
		{
			throw new InvalidOperationException("Invalid stack popup");
		}

		var prev = ClearScopeBlock();
		codeBlocks.RemoveAt(codeBlocks.Count - 1);
		return prev;
	}

	/// <summary>Dump to log error info about ref.</summary>
	public void LogDump(ScriptRefData r)
	{
		if (r.IsValid)
		{
			Logger.Error("Incorrect type of argument '" + r.name.ToString() + "' of type " + ScriptParserBase.DisplayType(parser, r.type));
		}
		else
		{
			Logger.Error("Unknown argument '" + r.name.ToString() + "'");
		}
	}
}

////////////////////////////////////////////////////////////
//						Bind class
////////////////////////////////////////////////////////////

/// <summary>Common part of bindings.</summary>
public class BindBase
{
	public const string functionWithoutDescription = "-";
	public const string functionInvisible = "";

	/// <summary>Tag type to skip adding default operations.</summary>
	public readonly struct ExtensionBinding;

	public readonly ScriptParserBase parser;

	public BindBase(ScriptParserBase p)
	{
		parser = p;
	}

	/// <summary>Add function with own name (C++ <c>addCustomFunc&lt;X&gt;</c>).</summary>
	public void AddCustomFunc(string name, ScriptParam[] sig, ScriptImpl impl, string description = functionWithoutDescription)
	{
		parser.AddParser(name, description, new ScriptFuncGroup(sig, impl));
	}

	/// <summary>Add function with own name (C++ <c>addCustomFunc&lt;X&gt;</c>), body always continues.</summary>
	public void AddCustomFunc(string name, ScriptParam[] sig, ScriptAction impl, string description = functionWithoutDescription)
	{
		parser.AddParser(name, description, new ScriptFuncGroup(sig, impl));
	}

	public void AddCustomConst(string name, int i)
	{
		parser.AddConst(name, i);
	}

	/// <summary>Add const pointer (C++ <c>addCustomPtr&lt;const T&gt;</c>).</summary>
	public void AddCustomPtr<T>(string name, T? p) where T : class
	{
		parser.AddConst(name, ScriptValueData.FromPtr(p));
	}

	/// <summary>Add editable pointer (C++ <c>addCustomPtr&lt;T&gt;</c>).</summary>
	public void AddCustomPtrE<T>(string name, T? p) where T : class
	{
		parser.AddConst(name, ScriptValueData.FromPtr(p, true));
	}
}

/// <summary>Bindings of a pointer type.</summary>
public sealed class Bind<T> : BindBase where T : class
{
	public readonly string prefix;

	/// <summary>Self as const pointer (C++ <c>const T*</c>).</summary>
	public static ScriptParam Self => ScriptParam.Ptr<T>();
	/// <summary>Self as editable pointer (C++ <c>T*</c>).</summary>
	public static ScriptParam SelfE => ScriptParam.PtrE<T>();

	private static string ScriptNameOf()
	{
		if (!ScriptBindTable.TryGet(typeof(T), out var name, out _))
		{
			throw new InvalidOperationException("Type without script name: " + typeof(T).Name);
		}
		return name;
	}

	public Bind(ScriptParserBase p) : this(p, ScriptNameOf())
	{
	}

	public Bind(ScriptParserBase p, ExtensionBinding e) : this(p, ScriptNameOf(), e)
	{
	}

	public Bind(ScriptParserBase p, string r) : base(p)
	{
		prefix = r;
		parser.AddParser("set", functionInvisible, BindHelper.SetPtrE<T>());
		parser.AddParser("set", functionInvisible, BindHelper.SetPtr<T>());
		parser.AddParser("swap", functionInvisible, BindHelper.SwapPtrE<T>());
		parser.AddParser("swap", functionInvisible, BindHelper.SwapPtr<T>());
		parser.AddParser("clear", functionInvisible, BindHelper.ClearPtrE<T>());
		parser.AddParser("clear", functionInvisible, BindHelper.ClearPtr<T>());
		parser.AddParser("test_eq", functionInvisible, BindHelper.EqPtr<T>());
	}

#pragma warning disable IDE0060 // C++ tag type selects the constructor
	public Bind(ScriptParserBase p, string r, ExtensionBinding e) : base(p)
#pragma warning restore IDE0060
	{
		prefix = r;
		// no default operations defined!
	}

	public string GetName(string s) => prefix + "." + s;

	/// <summary>Add bound function <c>prefix.name</c> (C++ <c>add&lt;&amp;func&gt;</c>, <c>addFunc&lt;X&gt;</c>).</summary>
	public void Add(string func, ScriptParam[] sig, ScriptAction impl, string description = functionWithoutDescription)
	{
		AddCustomFunc(GetName(func), sig, impl, description);
	}

	/// <summary>Add bound function <c>prefix.name</c> with own return code.</summary>
	public void AddFunc(string func, ScriptParam[] sig, ScriptImpl impl, string description = functionWithoutDescription)
	{
		AddCustomFunc(GetName(func), sig, impl, description);
	}

	/// <summary>Getter <c>R f() const</c> (C++ <c>add&lt;&amp;T::f&gt;</c>): <c>(const T*, int&amp;)</c>, null gives 0.</summary>
	public void AddGet(string func, Func<T, int> f, string description = functionWithoutDescription)
	{
		Add(func, [Self, ScriptParam.IntRef], (ref ScriptArgs a) =>
		{
			var t = a.Ptr<T>(0);
			a.IntRef(1) = t != null ? f(t) : 0;
		}, description);
	}

	/// <summary>Getter <c>bool f() const</c>.</summary>
	public void AddGet(string func, Func<T, bool> f, string description = functionWithoutDescription)
	{
		AddGet(func, t => f(t) ? 1 : 0, description);
	}

	/// <summary>Getter <c>R f()</c> without const (C++ <c>add&lt;&amp;T::f&gt;</c>): <c>(T*, int&amp;)</c>.</summary>
	public void AddGetE(string func, Func<T, int> f, string description = functionWithoutDescription)
	{
		Add(func, [SelfE, ScriptParam.IntRef], (ref ScriptArgs a) =>
		{
			var t = a.Ptr<T>(0);
			a.IntRef(1) = t != null ? f(t) : 0;
		}, description);
	}

	/// <summary>Getter <c>const P* f() const</c>: <c>(const T*, const P*&amp;)</c>.</summary>
	public void AddGetPtr<P>(string func, Func<T, P?> f, string description = functionWithoutDescription) where P : class
	{
		Add(func, [Self, ScriptParam.PtrRef<P>()], (ref ScriptArgs a) =>
		{
			var t = a.Ptr<T>(0);
			a.SetPtr(1, t != null ? f(t) : null);
		}, description);
	}

	/// <summary>Getter <c>P* f()</c>: <c>(T*, P*&amp;)</c>.</summary>
	public void AddGetPtrE<P>(string func, Func<T, P?> f, string description = functionWithoutDescription) where P : class
	{
		Add(func, [SelfE, ScriptParam.PtrERef<P>()], (ref ScriptArgs a) =>
		{
			var t = a.Ptr<T>(0);
			a.SetPtr(1, t != null ? f(t) : null);
		}, description);
	}

	/// <summary>C++ <c>addField&lt;MemPtr...&gt;(get)</c>.</summary>
	public void AddField(string getFuncName, Func<T, int> getter)
	{
		AddGet(getFuncName, getter, "Get field of " + prefix);
	}

	/// <summary>C++ <c>addField&lt;MemPtr...&gt;(get, set)</c>.</summary>
	public void AddField(string getFuncName, string setFuncName, Func<T, int> getter, Action<T, int> setter)
	{
		AddField(getFuncName, getter);
		Add(setFuncName, [SelfE, ScriptParam.Int], (ref ScriptArgs a) =>
		{
			var t = a.Ptr<T>(0);
			if (t != null) setter(t, a.Int(1));
		}, "Set field of " + prefix);
	}

	/// <summary>C++ <c>addField&lt;MemPtr...&gt;(get, set, add)</c>.</summary>
	public void AddField(string getFuncName, string setFuncName, string addFuncName, Func<T, int> getter, Action<T, int> setter)
	{
		AddField(getFuncName, setFuncName, getter, setter);
		Add(addFuncName, [SelfE, ScriptParam.Int], (ref ScriptArgs a) =>
		{
			var t = a.Ptr<T>(0);
			if (t != null) setter(t, unchecked(getter(t) + a.Int(1)));
		}, "Add to field of " + prefix);
	}

	/// <summary>C++ <c>addScriptTag&lt;ScriptValues&lt;X&gt;&gt;()</c>.</summary>
	public void AddScriptTag<X>() where X : class
	{
		BindHelper.AddScriptTag<X>(parser);
	}

	/// <summary>C++ <c>addScriptTag()</c>.</summary>
	public void AddScriptTag() => AddScriptTag<T>();

	/// <summary>C++ <c>addScriptValue&lt;canEdit, MemPtr...&gt;()</c>.</summary>
	public void AddScriptValue<X>(Func<T, ScriptValues<X>?> values, bool canEdit = true) where X : class
	{
		AddScriptTag<X>();
		Add("getTag", [Self, ScriptParam.IntRef, ScriptParam.Tag<X>()], (ref ScriptArgs a) =>
		{
			var t = a.Ptr<T>(0);
			a.IntRef(1) = t != null ? values(t)?.Get(a.Tag<X>(2)) ?? 0 : 0;
		}, "Get tag of " + prefix);
		if (canEdit)
		{
			Add("setTag", [SelfE, ScriptParam.Tag<X>(), ScriptParam.Int], (ref ScriptArgs a) =>
			{
				var t = a.Ptr<T>(0);
				if (t != null) values(t)?.Set(a.Tag<X>(1), a.Int(2));
			}, "Set tag of " + prefix);
		}
	}

	/// <summary>C++ <c>addDebugDisplay&lt;X&gt;()</c>.</summary>
	public void AddDebugDisplay(Func<T?, string> f)
	{
		AddCustomFunc("debug_impl", [ScriptParam.Context, Self], (ref ScriptArgs a) =>
		{
			var t = a.Ptr<T>(1);
			a.Worker.log_buffer_add(() => f(t));
		}, functionInvisible);
	}

	/// <summary>C++ <c>addFreeFunction&lt;X&gt;(func)</c>: name without prefix.</summary>
	public void AddFreeFunction(string func, ScriptParam[] sig, ScriptAction impl, string description = functionWithoutDescription)
	{
		AddCustomFunc(func, sig, impl, description);
	}

	/// <summary>C++ <c>addFake&lt;X&gt;(get)</c>: <c>(T*, int&amp;)</c>.</summary>
	public void AddFake(string get, int x)
	{
		Add(get, [SelfE, ScriptParam.IntRef], (ref ScriptArgs a) =>
		{
			var t = a.Ptr<T>(0);
			a.IntRef(1) = t != null ? x : 0;
		}, "Get int field of " + prefix);
	}

	/// <summary>C++ <c>addPair&lt;P, X, Y&gt;(get)</c>: editable and const getter of same pointer.</summary>
	public void AddPair<P>(string get, Func<T, P?> f) where P : class
	{
		AddGetPtrE(get, f);
		AddGetPtr(get, f);
	}

	/// <summary>C++ <c>addRules&lt;P, Y&gt;(get)</c>.</summary>
	public void AddRules<P>(string get, Func<T, P?> f) where P : class
	{
		AddGetPtr(get, f);
	}

	/// <summary>
	/// C++ <c>addList&lt;Func, MemPtr...&gt;(name)</c>: <c>name.init</c> and <c>name.list</c> used by <c>loop</c>.
	/// </summary>
	/// <param name="name">Name of function.</param>
	/// <param name="self">Type of first argument of filter function (<c>T*</c> or <c>const T*</c>).</param>
	/// <param name="filterArgs">Extra arguments of filter function.</param>
	/// <param name="result">Type of result (<c>V*&amp;</c>).</param>
	/// <param name="list">Getter of list.</param>
	/// <param name="filter">Filter, return true when element is accepted (arguments start at <paramref name="argOffset"/>).</param>
	/// <param name="description">Description.</param>
	public void AddList<V>(string name, ScriptParam self, ScriptParam[] filterArgs, ScriptParam result, Func<T, IReadOnlyList<V?>> list, ListFilter<T, V> filter, string description = functionWithoutDescription) where V : class
	{
		int n = filterArgs.Length;
		ScriptParam[] initSig = [self, .. filterArgs, ScriptParam.Sep, ScriptParam.IntRef, ScriptParam.IntRef];
		ScriptParam[] loopSig = [self, .. filterArgs, ScriptParam.Sep, ScriptParam.IntRef, ScriptParam.IntRef, ScriptParam.Sep, result];
		AddCustomFunc(GetName(name) + ".init", initSig, (ref ScriptArgs a) =>
		{
			var t = a.Ptr<T>(0);
			ref int curr = ref a.IntRef(n + 2);
			ref int limit = ref a.IntRef(n + 3);
			if (t != null)
			{
				var obj = list(t);
				curr = 0;
				limit = obj.Count;
				foreach (var u in obj)
				{
					if (filter(t, u, ref a, 1))
					{
						break;
					}
					++curr;
				}
			}
			else
			{
				curr = 0;
				limit = 0;
			}
		}, functionInvisible);
		AddCustomFunc(GetName(name) + ".list", loopSig, (ref ScriptArgs a) =>
		{
			var t = a.Ptr<T>(0);
			ref int curr = ref a.IntRef(n + 2);
			ref int limit = ref a.IntRef(n + 3);
			if (t != null)
			{
				var obj = list(t);
				int l = obj.Count;
				if ((uint)curr < (uint)l)
				{
					a.SetPtr(n + 5, obj[curr]);
				}
				else
				{
					a.SetPtr(n + 5, null);
				}
				++curr;
				for (; (uint)curr < (uint)l; ++curr)
				{
					if (filter(t, obj[curr], ref a, 1))
					{
						break;
					}
				}
			}
			else
			{
				a.SetPtr(n + 5, null);
				curr = 0;
				limit = 0;
			}
		}, description);
	}
}

/// <summary>Filter of <see cref="Bind{T}.AddList"/>; extra arguments are read from <paramref name="a"/> starting at <paramref name="argOffset"/>.</summary>
public delegate bool ListFilter<T, V>(T t, V? v, ref ScriptArgs a, int argOffset);

/// <summary>Bindings of a value type.</summary>
public sealed class BindValue<T> : BindBase where T : struct
{
	public readonly string prefix;

	public static ScriptParam Self => ScriptParam.Value<T>();
	public static ScriptParam SelfRef => ScriptParam.ValueRef<T>();

	public BindValue(ScriptParserBase p, string r) : base(p)
	{
		prefix = r;
		parser.AddParser("set", functionInvisible, new ScriptFuncGroup([SelfRef, Self], (ref ScriptArgs a) => { a.ValueRef<T>(0) = a.Value<T>(1); }));
		parser.AddParser("swap", functionInvisible, new ScriptFuncGroup([SelfRef, SelfRef], (ref ScriptArgs a) => { (a.ValueRef<T>(0), a.ValueRef<T>(1)) = (a.ValueRef<T>(1), a.ValueRef<T>(0)); }));
		parser.AddParser("clear", functionInvisible, new ScriptFuncGroup([SelfRef], (ref ScriptArgs a) => { a.ValueRef<T>(0) = default; }));

		parser.AddParser("test_eq", functionInvisible, new ScriptFuncGroup([ScriptParam.Prog, Self, Self, ScriptParam.Label, ScriptParam.Label], (ref ScriptArgs a) =>
		{
			a.Prog = EqualityComparer<T>.Default.Equals(a.Value<T>(1), a.Value<T>(2)) ? a.Label(3) : a.Label(4);
		}));
	}

	public string GetName(string s) => prefix + "." + s;

	/// <summary>Add bound function <c>prefix.name</c>.</summary>
	public void Add(string func, ScriptParam[] sig, ScriptAction impl, string description = functionWithoutDescription)
	{
		AddCustomFunc(GetName(func), sig, impl, description);
	}

	/// <summary>C++ <c>addField&lt;MemPtr&gt;(get, set, add)</c> of value type.</summary>
	public void AddField(string getFuncName, string setFuncName, string addFuncName, Func<T, int> getter, ValueSetter<T> setter)
	{
		Add(getFuncName, [Self, ScriptParam.IntRef], (ref ScriptArgs a) => { a.IntRef(1) = getter(a.Value<T>(0)); }, "Get field of " + prefix);
		Add(setFuncName, [SelfRef, ScriptParam.Int], (ref ScriptArgs a) => { setter(ref a.ValueRef<T>(0), a.Int(1)); }, "Set field of " + prefix);
		Add(addFuncName, [SelfRef, ScriptParam.Int], (ref ScriptArgs a) =>
		{
			ref T t = ref a.ValueRef<T>(0);
			setter(ref t, unchecked(getter(t) + a.Int(1)));
		}, "Add to field of " + prefix);
	}

	/// <summary>C++ <c>addDebugValueDisplay&lt;X&gt;()</c>.</summary>
	public void AddDebugValueDisplay(Func<T, string> f)
	{
		AddCustomFunc("debug_impl", [ScriptParam.Context, Self], (ref ScriptArgs a) =>
		{
			var t = a.Value<T>(1);
			a.Worker.log_buffer_add(() => f(t));
		}, functionInvisible);
	}

	/// <summary>C++ <c>addFreeFunction&lt;X&gt;(func)</c>.</summary>
	public void AddFreeFunction(string func, ScriptParam[] sig, ScriptAction impl, string description = functionWithoutDescription)
	{
		AddCustomFunc(func, sig, impl, description);
	}

	/// <summary>C++ <c>addConstructor&lt;Args...&gt;()</c>: <c>set</c> with constructor arguments.</summary>
	public void AddConstructor(ScriptParam[] args, ScriptAction impl, string description = functionWithoutDescription)
	{
		AddCustomFunc("set", [SelfRef, .. args], impl, description);
	}
}

/// <summary>Setter of a field of a value type.</summary>
public delegate void ValueSetter<T>(ref T t, int v);

/// <summary>Default operations of bound types (C++ <c>helper::BindSet</c>, <c>BindSwap</c>, <c>BindClear</c>, <c>BindEq</c>).</summary>
internal static class BindHelper
{
	public static ScriptFuncGroup SetPtrE<T>() where T : class
		=> new([ScriptParam.PtrERef<T>(), ScriptParam.PtrE<T>()], (ref ScriptArgs a) => { a.SetPtr(0, a.Obj(1)); });
	public static ScriptFuncGroup SetPtr<T>() where T : class
		=> new([ScriptParam.PtrRef<T>(), ScriptParam.Ptr<T>()], (ref ScriptArgs a) => { a.SetPtr(0, a.Obj(1)); });
	public static ScriptFuncGroup SwapPtrE<T>() where T : class
		=> new([ScriptParam.PtrERef<T>(), ScriptParam.PtrERef<T>()], (ref ScriptArgs a) => { (a.ObjRef(0), a.ObjRef(1)) = (a.ObjRef(1), a.ObjRef(0)); });
	public static ScriptFuncGroup SwapPtr<T>() where T : class
		=> new([ScriptParam.PtrRef<T>(), ScriptParam.PtrRef<T>()], (ref ScriptArgs a) => { (a.ObjRef(0), a.ObjRef(1)) = (a.ObjRef(1), a.ObjRef(0)); });
	public static ScriptFuncGroup ClearPtrE<T>() where T : class
		=> new([ScriptParam.PtrERef<T>()], (ref ScriptArgs a) => { a.SetPtr(0, null); });
	public static ScriptFuncGroup ClearPtr<T>() where T : class
		=> new([ScriptParam.PtrRef<T>()], (ref ScriptArgs a) => { a.SetPtr(0, null); });
	public static ScriptFuncGroup EqPtr<T>() where T : class
		=> new([ScriptParam.Prog, ScriptParam.Ptr<T>(), ScriptParam.Ptr<T>(), ScriptParam.Label, ScriptParam.Label], (ref ScriptArgs a) =>
		{
			a.Prog = ReferenceEquals(a.Obj(1), a.Obj(2)) ? a.Label(3) : a.Label(4);
		});

	/// <summary>C++ <c>Bind::addScriptTag&lt;ScriptValues&lt;X&gt;&gt;()</c>.</summary>
	public static void AddScriptTag<X>(ScriptParserBase parser) where X : class
	{
		if (!ScriptBindTable.TryGet(typeof(X), out var parentName, out _))
		{
			throw new InvalidOperationException("Errow with adding script tag to unknown type");
		}
		parser.GetGlobal().AddTagType<X>(parentName);
		var tagType = ScriptTag<X>.Type();
		if (!parser.HaveTypeBase(tagType))
		{
			var conf = parser.GetType(ScriptParam.PtrE<X>().ArgType);
			if (conf == null)
			{
				throw new InvalidOperationException("Errow with adding script tag to unknown type");
			}
			parser.AddTypeBase(conf.Value.name.ToString() + ".Tag", tagType, new TypeInfo(2, 2));
			parser.AddParser("set", BindBase.functionInvisible, new ScriptFuncGroup([ScriptParam.TagRef<X>(), ScriptParam.Tag<X>()], (ref ScriptArgs a) => { a.TagRef(0) = a.Int(1); }));
			parser.AddParser("swap", BindBase.functionInvisible, new ScriptFuncGroup([ScriptParam.TagRef<X>(), ScriptParam.TagRef<X>()], (ref ScriptArgs a) => { (a.TagRef(0), a.TagRef(1)) = (a.TagRef(1), a.TagRef(0)); }));
			parser.AddParser("clear", BindBase.functionInvisible, new ScriptFuncGroup([ScriptParam.TagRef<X>()], (ref ScriptArgs a) => { a.TagRef(0) = 0; }));
			parser.AddParser("test_eq", BindBase.functionInvisible, new ScriptFuncGroup([ScriptParam.Prog, ScriptParam.Tag<X>(), ScriptParam.Tag<X>(), ScriptParam.Label, ScriptParam.Label], (ref ScriptArgs a) =>
			{
				a.Prog = a.Int(1) == a.Int(2) ? a.Label(3) : a.Label(4);
			}));
		}
	}
}
