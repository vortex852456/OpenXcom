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
//
// Design of the port (behaviour is 1:1, the representation is not):
// - C++ compiles a script into a byte vector (operation ids + raw argument bytes) and runs it with a
//   computed `switch`. The port compiles into a list of ScriptInstr (delegate + resolved operands);
//   a ProgPos is the index of an instruction instead of a byte offset.
// - Registers are still addressed by the C++ byte offsets (RegEnum) with the same sizes and alignments
//   (64-bit pointers), so the register limit (ScriptMaxReg) behaves the same. Int-like values live in
//   ScriptWorkerBase.RegI, everything else (pointers, texts, Position, ...) in RegO, both indexed by offset.
// - Template types (ScriptParser<Output, Args...>, ScriptWorker<...>, FuncGroup<...>) become runtime
//   descriptions: ScriptParam lists with the same argument types, see ScriptBind.cs.

#pragma warning disable CA1000 // static members on generic types mirror C++ static template members (ScriptTag<T>::type()).
#pragma warning disable CA1711 // C++ names (ArgEnum, RegEnum, ScriptImpl, ...) are kept.
#pragma warning disable CA1715 // C++ template parameter names are kept.
#pragma warning disable CA1720 // script type names (Int, Ptr) are kept.
#pragma warning disable CA1822 // members mirror C++ instance members.
#pragma warning disable CA1815 // value types mirror C++ PODs, equality is not used.
#pragma warning disable CA1034 // nested types mirror C++ nested types.

using System.Runtime.CompilerServices;
using static Acom.ScriptArg;

namespace Acom;

////////////////////////////////////////////////////////////
//					script base types
////////////////////////////////////////////////////////////

/// <summary>Script null type, alias to std nullptr.</summary>
public sealed class ScriptNull
{
	private ScriptNull()
	{
	}
}

/// <summary>Dummy type used to separate function args groups.</summary>
public readonly struct ScriptArgSeparator
{
}

/// <summary>Type marker of label arguments (C++ <c>ProgPos</c>).</summary>
public readonly struct ProgPos
{
	/// <summary>Unknown position of a label.</summary>
	public const int Unknown = -1;
	/// <summary>Index of the first instruction.</summary>
	public const int Start = 0;
}

/// <summary>Script const text, null or a string (C++ zero terminated <c>const char*</c>).</summary>
public readonly struct ScriptText
{
	/// <summary>Text, null for C++ <c>nullptr</c>.</summary>
	public readonly string? ptr;

	public ScriptText(string? p)
	{
		ptr = p;
	}

	/// <summary>Like C++ <c>operator std::string</c>: null gives an empty string.</summary>
	public override string ToString() => ptr ?? "";

	/// <summary>Empty text (not null).</summary>
	public static readonly ScriptText empty = new("");
}

/// <summary>Available regs.</summary>
public enum RegEnum : ushort
{
	RegInvalid = 0xFFFF,

	RegStartPos = 0,
}

/// <summary>Return value from script operation.</summary>
public enum RetEnum : byte
{
	RetContinue = 0,
	RetEnd,
	RetError,

	RetSize,
}

/// <summary>Args special types.</summary>
public enum ArgSpecEnum : ushort
{
	ArgSpecNone = 0x0,
	ArgSpecReg = 0x1,
	ArgSpecVar = 0x2 + ArgSpecReg,
	ArgSpecPtr = 0x4,
	ArgSpecPtrE = 0x8 + ArgSpecPtr,
	ArgSpecSize = 0x10,
}

/// <summary>Args types; values above <see cref="ArgMax"/> are assigned at run time per registered type.</summary>
public enum ArgEnum : ushort
{
	ArgInvalid = ArgSpecEnum.ArgSpecSize * 0,
	ArgPlaceholder,
	ArgUnknowSimple,
	ArgUnknowSegment,
	ArgInvalidMax,

	ArgNull = ArgSpecEnum.ArgSpecSize * 1,
	ArgInt = ArgSpecEnum.ArgSpecSize * 2,
	ArgLabel = ArgSpecEnum.ArgSpecSize * 3,
	ArgText = ArgSpecEnum.ArgSpecSize * 4,
	ArgSep = ArgSpecEnum.ArgSpecSize * 5,

	ArgMax = ArgSpecEnum.ArgSpecSize * 6,
}

/// <summary>Common type meta data.</summary>
public readonly record struct TypeInfo(int size, int alignment)
{
	/// <summary>Is type valid?</summary>
	public bool IsValid => size != 0;

	/// <summary>Get next valid position for type.</summary>
	public int NextRegPos(int prev) => (prev + alignment - 1) & ~(alignment - 1);

	/// <summary>Get total space used by this type with considering alignment.</summary>
	public int NeedRegSpace(int prev) => NextRegPos(prev) + size;

	/// <summary>Default value for pointers (64-bit).</summary>
	public static TypeInfo PtrTypeInfo => new(8, 8);
}

/// <summary>Free functions on <see cref="ArgEnum"/> (C++ <c>ArgBase</c>, <c>ArgSpecAdd</c>, ...).</summary>
public static class ScriptArg
{
	public const int ScriptMaxOut = 9;
	public const int ScriptMaxArg = 16;
	public const int ScriptMaxReg = 64 * 8;

	public const ArgEnum ArgInvalid = ArgEnum.ArgInvalid;
	public const ArgEnum ArgPlaceholder = ArgEnum.ArgPlaceholder;
	public const ArgEnum ArgUnknowSimple = ArgEnum.ArgUnknowSimple;
	public const ArgEnum ArgUnknowSegment = ArgEnum.ArgUnknowSegment;
	public const ArgEnum ArgNull = ArgEnum.ArgNull;
	public const ArgEnum ArgInt = ArgEnum.ArgInt;
	public const ArgEnum ArgLabel = ArgEnum.ArgLabel;
	public const ArgEnum ArgText = ArgEnum.ArgText;
	public const ArgEnum ArgSep = ArgEnum.ArgSep;
	public const ArgEnum ArgMax = ArgEnum.ArgMax;

	public const ArgSpecEnum ArgSpecNone = ArgSpecEnum.ArgSpecNone;
	public const ArgSpecEnum ArgSpecReg = ArgSpecEnum.ArgSpecReg;
	public const ArgSpecEnum ArgSpecVar = ArgSpecEnum.ArgSpecVar;
	public const ArgSpecEnum ArgSpecPtr = ArgSpecEnum.ArgSpecPtr;
	public const ArgSpecEnum ArgSpecPtrE = ArgSpecEnum.ArgSpecPtrE;

	public const RetEnum RetContinue = RetEnum.RetContinue;
	public const RetEnum RetEnd = RetEnum.RetEnd;
	public const RetEnum RetError = RetEnum.RetError;

	public const RegEnum RegInvalid = RegEnum.RegInvalid;

	private const ushort SpecSize = (ushort)ArgSpecEnum.ArgSpecSize;

	/// <summary>Next available value for arg type.</summary>
	public static ArgEnum ArgNext(ArgEnum arg) => (ArgEnum)((ushort)arg + SpecSize);

	/// <summary>Base version of argument type.</summary>
	public static ArgEnum ArgBase(ArgEnum arg) => (ArgEnum)((ushort)arg & ~(SpecSize - 1));

	/// <summary>Specialized version of argument type.</summary>
	public static ArgEnum ArgSpecAdd(ArgEnum arg, ArgSpecEnum spec) => ArgBase(arg) != ArgInvalid ? (ArgEnum)((ushort)arg | (ushort)spec) : arg;

	/// <summary>Remove specialization from argument type.</summary>
	public static ArgEnum ArgSpecRemove(ArgEnum arg, ArgSpecEnum spec) => ArgBase(arg) != ArgInvalid ? (ArgEnum)((ushort)arg & ~(ushort)spec) : arg;

	/// <summary>Test if argument type is register (readonly or writeable).</summary>
	public static bool ArgIsReg(ArgEnum arg) => ArgBase(arg) != ArgInvalid && ((ushort)arg & (ushort)ArgSpecReg) == (ushort)ArgSpecReg;

	/// <summary>Test if argument type is variable (writeable register).</summary>
	public static bool ArgIsVar(ArgEnum arg) => ArgBase(arg) != ArgInvalid && ((ushort)arg & (ushort)ArgSpecVar) == (ushort)ArgSpecVar;

	/// <summary>Test if argument type is pointer.</summary>
	public static bool ArgIsPtr(ArgEnum arg) => ArgBase(arg) != ArgInvalid && ((ushort)arg & (ushort)ArgSpecPtr) == (ushort)ArgSpecPtr;

	/// <summary>Test if argument type is editable pointer.</summary>
	public static bool ArgIsPtrE(ArgEnum arg) => ArgBase(arg) != ArgInvalid && ((ushort)arg & (ushort)ArgSpecPtrE) == (ushort)ArgSpecPtrE;

	/// <summary>
	/// Compatibility between operation argument type and variable type. Greater numbers mean bigger compatibility.
	/// </summary>
	/// <param name="argType">Type of operation argument.</param>
	/// <param name="varType">Type of variable/value we try pass to operation.</param>
	/// <param name="overloadSize">Number of alternatives of the argument.</param>
	/// <returns>Zero if incompatible, 255 if both types are same.</returns>
	public static int ArgCompatible(ArgEnum argType, ArgEnum varType, int overloadSize)
	{
		return
			ArgBase(argType) == ArgInvalid ? 0 :
			ArgIsVar(argType) && argType != varType ? 0 :
			ArgBase(argType) != ArgBase(varType) ? 0 :
			ArgIsReg(argType) != ArgIsReg(varType) ? 0 :
			ArgIsPtr(argType) != ArgIsPtr(varType) ? 0 :
			ArgIsPtrE(argType) && !ArgIsPtrE(varType) ? 0 :
				255 - (ArgIsPtrE(argType) != ArgIsPtrE(varType) ? 128 : 0) - (ArgIsVar(argType) != ArgIsVar(varType) ? 64 : 0) - (overloadSize > 8 ? 8 : overloadSize);
	}

	private static readonly Lock s_typeLock = new();
	private static ArgEnum s_curr = ArgMax;
	private static readonly Dictionary<Type, ArgEnum> s_types = [];

	/// <summary>Function returning next unique value for ArgEnum.</summary>
	public static ArgEnum ArgNextUniqueValue()
	{
		lock (s_typeLock)
		{
			ArgEnum old = s_curr;
			s_curr = ArgNext(s_curr);
			return old;
		}
	}

	/// <summary>Function matching some Type to ArgEnum (C++ <c>ArgRegisteType&lt;T&gt;</c>, unique values assigned on first use).</summary>
	public static ArgEnum ArgRegisteType(Type t)
	{
		if (t == typeof(int))
		{
			return ArgInt;
		}
		else if (t == typeof(ScriptNull))
		{
			return ArgNull;
		}
		else if (t == typeof(ScriptText))
		{
			return ArgText;
		}
		else if (t == typeof(ProgPos))
		{
			return ArgLabel;
		}
		else if (t == typeof(ScriptArgSeparator))
		{
			return ArgSep;
		}
		else
		{
			lock (s_typeLock)
			{
				if (!s_types.TryGetValue(t, out var curr))
				{
					curr = s_curr;
					s_curr = ArgNext(s_curr);
					s_types.Add(t, curr);
				}
				return curr;
			}
		}
	}

	/// <summary>Function matching some Type to ArgEnum.</summary>
	public static ArgEnum ArgRegisteType<T>() => ArgRegisteType(typeof(T));
}

////////////////////////////////////////////////////////////
//				containers definitions
////////////////////////////////////////////////////////////

/// <summary>Kind of a compiled operand.</summary>
public enum ScriptOperandKind : byte
{
	/// <summary>No data (internal argument, separator).</summary>
	None,
	/// <summary>Register, <see cref="ScriptOperand.reg"/> is the offset.</summary>
	Reg,
	/// <summary>Constant value in <see cref="ScriptOperand.i"/> / <see cref="ScriptOperand.o"/>.</summary>
	Value,
	/// <summary>Label, <see cref="ScriptOperand.i"/> is the instruction index.</summary>
	Label,
	/// <summary>Text literal, <see cref="ScriptOperand.o"/> is the string.</summary>
	Text,
	/// <summary>Null value.</summary>
	Null,
}

/// <summary>One compiled argument of a script operation (C++ raw bytes after the operation id).</summary>
public struct ScriptOperand
{
	public ScriptOperandKind kind;
	public RegEnum reg;
	public int i;
	public object? o;
}

/// <summary>Implementation of one operation, reads its arguments through <see cref="ScriptArgs"/>.</summary>
public delegate RetEnum ScriptImpl(ref ScriptArgs a);

/// <summary>One compiled script operation.</summary>
public sealed class ScriptInstr
{
	/// <summary>Function executing the operation.</summary>
	public ScriptImpl impl;
	/// <summary>Operands, one per argument position of the function.</summary>
	public ScriptOperand[] ops;
	/// <summary>Raw size in bytes the C++ arguments would use (limits custom functions to 64 bytes).</summary>
	internal int argBytes;

	internal readonly List<ScriptOperand> building = [];

	public ScriptInstr(ScriptImpl i)
	{
		impl = i;
		ops = [];
	}
}

/// <summary>Common base of script execution.</summary>
public class ScriptContainerBase
{
	internal ScriptInstr[]? _proc;
	/// <summary>Parser that compiled this script (gives the argument layout to workers).</summary>
	internal ScriptParserBase? _parser;

	/// <summary>Test if is any script there.</summary>
	public bool IsValid => _proc != null && _proc.Length != 0;

	/// <summary>Get proc data.</summary>
	public ScriptInstr[]? Data => IsValid ? _proc : null;

	/// <summary>Move data from other container (C++ move assignment).</summary>
	internal void MoveFrom(ScriptContainerBase other)
	{
		_proc = other._proc;
		_parser = other._parser;
		other._proc = null;
	}
}

/// <summary>Container that can load itself using a parser (common part of ScriptContainer and ScriptContainerEvents).</summary>
public interface IScriptContainer
{
	/// <summary>Load code from string in YAML node.</summary>
	void LoadContainer(string parentName, YamlNodeReader reader, ScriptParserBase parent);
	/// <summary>Load data from string.</summary>
	void LoadContainer(string parentName, string srcCode, ScriptParserBase parent);
}

/// <summary>Strong typed script.</summary>
public class ScriptContainer : ScriptContainerBase, IScriptContainer
{
	/// <summary>Load code from string in YAML node.</summary>
	public void LoadContainer(string parentName, YamlNodeReader reader, ScriptParserBase parent)
	{
		parent.ParseNode(this, parentName, reader);
	}

	/// <summary>Load data from string.</summary>
	public void LoadContainer(string parentName, string srcCode, ScriptParserBase parent)
	{
		parent.ParseCode(this, parentName, srcCode);
	}
}

/// <summary>Common base of typed script with events.</summary>
public class ScriptContainerEventsBase
{
	internal readonly ScriptContainerBase _current = new();
	internal List<ScriptContainerBase>? _events;
	internal ScriptParserBase? _eventsParser;

	/// <summary>Test if is any script there (always true like C++).</summary>
	public static bool IsValid => true;

	/// <summary>Get proc data.</summary>
	public ScriptInstr[]? Data => _current.Data;

	/// <summary>Get events data (C++ pointer to a list ended by an empty container).</summary>
	public List<ScriptContainerBase>? DataEvents => _events;

	/// <summary>Parser that compiled this script.</summary>
	internal ScriptParserBase? Parser => _eventsParser ?? _current._parser;
}

/// <summary>Strong typed script with events.</summary>
public class ScriptContainerEvents : ScriptContainerEventsBase, IScriptContainer
{
	/// <summary>Load code from string in YAML node.</summary>
	public void LoadContainer(string parentName, YamlNodeReader reader, ScriptParserBase parent)
	{
		((ScriptParserEventsBase)parent).ParseNode(this, parentName, reader);
	}

	/// <summary>Load data from string.</summary>
	public void LoadContainer(string parentName, string srcCode, ScriptParserBase parent)
	{
		((ScriptParserEventsBase)parent).ParseCode(this, parentName, srcCode);
	}
}

////////////////////////////////////////////////////////////
//					worker definition
////////////////////////////////////////////////////////////

/// <summary>Script output and input arguments (C++ <c>ScriptOutputArgs&lt;OutputArgs...&gt;</c>).</summary>
public sealed class ScriptOutputArgs
{
	/// <summary>Values (int for int/bool/tags, objects otherwise).</summary>
	public readonly object?[] data;

	/// <summary>Constructor.</summary>
	public ScriptOutputArgs(params object?[] args)
	{
		data = args;
	}

	/// <summary>Getter for first element.</summary>
	public int GetFirst() => (int)data[0]!;
	/// <summary>Getter for second element.</summary>
	public int GetSecond() => (int)data[1]!;
	/// <summary>Getter for third element.</summary>
	public int GetThird() => (int)data[2]!;
	/// <summary>Get int element.</summary>
	public int GetInt(int i) => (int)data[i]!;
}

/// <summary>Class execute scripts and store its data.</summary>
public class ScriptWorkerBase
{
	private string log_buffer = "";
	/// <summary>Int-like registers by byte offset.</summary>
	internal readonly int[] RegI = new int[ScriptMaxReg];
	/// <summary>Other registers by byte offset.</summary>
	internal readonly object?[] RegO = new object?[ScriptMaxReg];

	/// <summary>Layout of the script arguments.</summary>
	private protected ScriptSignature? _signature;

	/// <summary>Update values in script.</summary>
	private protected void UpdateBase(ScriptSignature sig, object?[] args)
	{
		_signature = sig;
		Array.Clear(RegI);
		Array.Clear(RegO);
		if (args.Length != sig.inputs.Length)
		{
			throw new ArgumentException("Wrong number of script arguments");
		}
		for (int i = 0; i < args.Length; ++i)
		{
			SetReg(sig.inputs[i], sig.inputOffsets[i], args[i]);
		}
	}

	private void SetReg(ScriptParam p, int offset, object? value)
	{
		if (p.IntStorage)
		{
			RegI[offset] = value switch
			{
				null => 0,
				bool b => b ? 1 : 0,
				_ => p.ToIntStorage(value),
			};
		}
		else
		{
			RegO[offset] = value;
		}
	}

	private object? GetReg(ScriptParam p, int offset)
	{
		return p.IntStorage ? p.FromIntStorage(RegI[offset]) : RegO[offset];
	}

	/// <summary>Set all output values.</summary>
	private protected void Set(ScriptOutputArgs arg)
	{
		var sig = _signature!;
		for (int i = 0; i < sig.outputs.Length; ++i)
		{
			SetReg(sig.outputs[i], sig.outputOffsets[i], arg.data[i]);
		}
	}

	/// <summary>Get writable output values.</summary>
	private protected void Get(ScriptOutputArgs arg)
	{
		var sig = _signature!;
		for (int i = 0; i < sig.outputs.Length; ++i)
		{
			if (sig.outputs[i].IsOutput)
			{
				arg.data[i] = GetReg(sig.outputs[i], sig.outputOffsets[i]);
			}
		}
	}

	/// <summary>Reset readonly output values.</summary>
	private protected void Reset(ScriptOutputArgs arg)
	{
		var sig = _signature!;
		for (int i = 0; i < sig.outputs.Length; ++i)
		{
			if (!sig.outputs[i].IsOutput)
			{
				SetReg(sig.outputs[i], sig.outputOffsets[i], arg.data[i]);
			}
		}
	}

	/// <summary>Call script.</summary>
	private protected void ExecuteBase(ScriptInstr[]? proc)
	{
		if (proc != null)
		{
			ScriptExe(this, proc);
		}
	}

	/// <summary>Get int value from reg.</summary>
	public ref int RefInt(int off) => ref RegI[off];

	/// <summary>Get object value from reg.</summary>
	public ref object? RefObj(int off) => ref RegO[off];

	private static readonly Lock s_logLock = new();
	private const int log_buffer_limit_max = 500;
	private static int log_buffer_limit_count;
	private static int s_bugCount;

	/// <summary>Add text to log buffer.</summary>
	public void log_buffer_add(Func<string> func)
	{
		if (log_buffer_limit_count > log_buffer_limit_max)
		{
			return;
		}
		if (log_buffer.Length != 0)
		{
			log_buffer += " ";
		}
		log_buffer += func();
	}

	/// <summary>Flush buffer to log file.</summary>
	public void log_buffer_flush()
	{
		lock (s_logLock)
		{
			if (++log_buffer_limit_count < log_buffer_limit_max)
			{
				Logger.Debug("Script debug log: " + log_buffer);
				log_buffer = "";
			}
			else if (log_buffer_limit_count == log_buffer_limit_max)
			{
				Logger.Debug("Script debug log limit reach");
				log_buffer = "";
			}
		}
	}

	/// <summary>Core function in script engine used to executing scripts.</summary>
	/// <param name="data">Worker with registers.</param>
	/// <param name="proc">Array storing operation of script.</param>
	internal static void ScriptExe(ScriptWorkerBase data, ScriptInstr[] proc)
	{
		int curr = ProgPos.Start;
		RetEnum ret;
		while (true)
		{
			var op = proc[curr];
			++curr;
			var args = new ScriptArgs(data, op.ops, ref curr);
			ret = op.impl(ref args);
			if (ret != RetContinue)
			{
				break;
			}
		}

		switch (ret)
		{
			case RetEnum.RetEnd:
				// normal end of script execution
				return;

			default:
				lock (s_logLock)
				{
					if (++s_bugCount < 100)
					{
						Logger.Error("Invalid script operation");
					}
				}
				return;
		}
	}
}

/// <summary>Layout of the output and input registers of a script type.</summary>
public sealed class ScriptSignature
{
	public readonly ScriptParam[] outputs;
	public readonly ScriptParam[] inputs;
	public readonly int[] outputOffsets;
	public readonly int[] inputOffsets;

	public ScriptSignature(ScriptParam[] o, ScriptParam[] i)
	{
		outputs = o;
		inputs = i;
		outputOffsets = new int[o.Length];
		inputOffsets = new int[i.Length];
		int off = 0;
		for (int k = 0; k < o.Length; ++k)
		{
			outputOffsets[k] = o[k].MetaBase.NextRegPos(off);
			off = o[k].MetaBase.NeedRegSpace(off);
		}
		for (int k = 0; k < i.Length; ++k)
		{
			inputOffsets[k] = i[k].MetaBase.NextRegPos(off);
			off = i[k].MetaBase.NeedRegSpace(off);
		}
	}
}

/// <summary>Strong typed script executor.</summary>
public sealed class ScriptWorker : ScriptWorkerBase
{
	/// <summary>Constructor, <paramref name="args"/> are the script input arguments.</summary>
	public ScriptWorker(ScriptSignature sig, params object?[] args)
	{
		UpdateBase(sig, args);
	}

	/// <summary>Execute standard script.</summary>
	public void Execute(ScriptContainerBase c, ScriptOutputArgs arg)
	{
		Set(arg);
		ExecuteBase(c.Data);
		Get(arg);
	}

	/// <summary>Execute standard script with global events.</summary>
	public void Execute(ScriptContainerEventsBase c, ScriptOutputArgs arg)
	{
		Set(arg);
		var ptr = c.DataEvents;
		int i = 0;
		if (ptr != null)
		{
			while (i < ptr.Count && ptr[i].IsValid)
			{
				Reset(arg);
				ExecuteBase(ptr[i].Data);
				++i;
			}
			++i;
		}
		Reset(arg);
		ExecuteBase(c.Data);
		if (ptr != null)
		{
			while (i < ptr.Count && ptr[i].IsValid)
			{
				Reset(arg);
				ExecuteBase(ptr[i].Data);
				++i;
			}
		}
		Get(arg);
	}
}

/// <summary>Strong typed blit script executor.</summary>
public sealed class ScriptWorkerBlit : ScriptWorkerBase
{
	/// <summary>Current script set in worker.</summary>
	private ScriptInstr[]? _proc;
	private List<ScriptContainerBase>? _events;

	/// <summary>Update data from container script.</summary>
	public void Update(ScriptContainerBase c, params object?[] args)
	{
		Clear();
		if (c.IsValid)
		{
			_proc = c.Data;
			_events = null;
			UpdateBase(c._parser!.Signature, args);
		}
	}

	/// <summary>Update data from container script.</summary>
	public void Update(ScriptContainerEventsBase c, params object?[] args)
	{
		Clear();
		var parser = c.Parser;
		if (parser != null)
		{
			_proc = c.Data;
			_events = c.DataEvents;
			UpdateBase(parser.Signature, args);
		}
	}

	/// <summary>Is a script set (C++ <c>_proc</c> not null).</summary>
	public bool HaveScript => _proc != null;

	/// <summary>Programmable blitting using script.</summary>
	public void ExecuteBlit(Surface src, Surface dest, int x, int y, int shade)
	{
		ExecuteBlit(src, dest, x, y, shade, new GraphSubset(dest.Width, dest.Height));
	}

	/// <summary>Programmable blitting using script.</summary>
	/// <param name="src">source surface.</param>
	/// <param name="dest">destination surface.</param>
	/// <param name="x">x offset of source surface.</param>
	/// <param name="y">y offset of source surface.</param>
	/// <param name="shade">shade used when there is no script.</param>
	/// <param name="mask">destination area.</param>
	public void ExecuteBlit(Surface src, Surface dest, int x, int y, int shade, GraphSubset mask)
	{
		if (_proc != null)
		{
			int sx0 = Math.Max(0, mask.beg_x - x), sy0 = Math.Max(0, mask.beg_y - y);
			int sx1 = Math.Min(src.Width, mask.end_x - x), sy1 = Math.Min(src.Height, mask.end_y - y);
			if (sx0 < sx1 && sy0 < sy1)
			{
				src.BlitRect(dest, x, y, sx0, sy0, sx1, sy1, new BlitOp(this));
			}
		}
		else
		{
			src.BlitNShade(dest, x, y, shade, mask);
		}
	}

	/// <summary>One pixel of <see cref="ExecuteBlit(Surface, Surface, int, int, int, GraphSubset)"/>.</summary>
	internal void BlitPixel(ref byte destStuff, byte srcStuff)
	{
		if (srcStuff != 0)
		{
			var arg = new ScriptOutputArgs((int)srcStuff, (int)destStuff);
			Set(arg);
			if (_events != null)
			{
				var ptr = _events;
				int i = 0;
				while (i < ptr.Count && ptr[i].IsValid)
				{
					Reset(arg);
					ScriptExeOpt(ptr[i].Data);
					++i;
				}
				++i;

				Reset(arg);
				ScriptExeOpt(_proc);

				while (i < ptr.Count && ptr[i].IsValid)
				{
					Reset(arg);
					ScriptExeOpt(ptr[i].Data);
					++i;
				}
			}
			else
			{
				ScriptExeOpt(_proc);
			}
			Get(arg);
			int first = arg.GetFirst();
			if (first != 0) destStuff = unchecked((byte)first);
		}
	}

	private void ScriptExeOpt(ScriptInstr[]? proc)
	{
		if (proc != null)
		{
			ScriptExe(this, proc);
		}
	}

	private readonly struct BlitOp(ScriptWorkerBlit worker) : Surface.IPixelOp
	{
		public void Apply(ref byte dest, byte src) => worker.BlitPixel(ref dest, src);
	}

	/// <summary>Clear all worker data.</summary>
	public void Clear()
	{
		_proc = null;
		_events = null;
	}
}

////////////////////////////////////////////////////////////
//					objects ranges
////////////////////////////////////////////////////////////

/// <summary>Symbol in script (C++ range of chars).</summary>
public readonly struct ScriptRef : IEquatable<ScriptRef>
{
	private readonly string? _s;
	private readonly int _begin;
	private readonly int _end;

	/// <summary>Constructor from whole string.</summary>
	public ScriptRef(string s)
	{
		_s = s;
		_begin = 0;
		_end = s.Length;
	}

	/// <summary>Constructor from range of string.</summary>
	public ScriptRef(string? s, int b, int e)
	{
		_s = s;
		_begin = b;
		_end = e;
	}

	/// <summary>Source string.</summary>
	internal string? Source => _s;
	/// <summary>Beginning of range in source.</summary>
	public int Begin => _begin;
	/// <summary>End of range in source.</summary>
	public int End => _end;

	/// <summary>Size of string range.</summary>
	public int Size => _end - _begin;

	/// <summary>Bool operator.</summary>
	public bool IsValid => _begin != _end;

	/// <summary>Get char of range.</summary>
	public char this[int i] => _s![_begin + i];

	/// <summary>Find first occurrence of character in string range, -1 if not found.</summary>
	public int Find(char c)
	{
		for (int i = _begin; i < _end; ++i)
		{
			if (_s![i] == c)
			{
				return i - _begin;
			}
		}
		return -1;
	}

	/// <summary>Return sub range of current range.</summary>
	public ScriptRef Substr(int p, int s = int.MaxValue)
	{
		int totalSize = Size;
		if (p >= totalSize)
		{
			return default;
		}

		int b = _begin + p;
		if (s > totalSize - p)
		{
			return new ScriptRef(_s, b, _end);
		}
		else
		{
			return new ScriptRef(_s, b, b + s);
		}
	}

	public ScriptRef Head(int p) => Substr(0, p);

	public ScriptRef Tail(int p) => Substr(p);

	public ScriptRef HeadFromEnd(int p)
	{
		int totalSize = Size;
		if (p >= totalSize)
		{
			return this;
		}

		return Substr(totalSize - p);
	}

	public ScriptRef TailFromEnd(int p)
	{
		int totalSize = Size;
		if (p >= totalSize)
		{
			return default;
		}

		return Substr(0, totalSize - p);
	}

	/// <summary>Create string based on current range.</summary>
	public override string ToString() => IsValid ? _s!.Substring(_begin, Size) : "";

	/// <summary>Span of the chars.</summary>
	public ReadOnlySpan<char> AsSpan() => IsValid ? _s.AsSpan(_begin, Size) : [];

	/// <summary>Create ref based on string.</summary>
	public static ScriptRef TempFrom(string s) => new(s);

	/// <summary>Compare two ranges (size first, then content like memcmp).</summary>
	public static int Compare(ScriptRef a, ScriptRef b)
	{
		int size_a = a.Size;
		int size_b = b.Size;

		if (size_a == size_b)
		{
			return a.AsSpan().SequenceCompareTo(b.AsSpan());
		}
		else if (size_a < size_b)
		{
			return -1;
		}
		else
		{
			return 1;
		}
	}

	/// <summary>Compare range with string (size first, then content).</summary>
	public static int Compare(ScriptRef a, ReadOnlySpan<char> b)
	{
		int size_a = a.Size;
		int size_b = b.Length;

		if (size_a == size_b)
		{
			return a.AsSpan().SequenceCompareTo(b);
		}
		else if (size_a < size_b)
		{
			return -1;
		}
		else
		{
			return 1;
		}
	}

	public bool Equals(ScriptRef other) => Compare(this, other) == 0;
	public override bool Equals(object? obj) => obj is ScriptRef r && Equals(r);
	public override int GetHashCode() => string.GetHashCode(AsSpan(), StringComparison.Ordinal);

	public static bool operator ==(ScriptRef a, ScriptRef b) => Compare(a, b) == 0;
	public static bool operator !=(ScriptRef a, ScriptRef b) => Compare(a, b) != 0;
	public static bool operator <(ScriptRef a, ScriptRef b) => Compare(a, b) < 0;
	public static bool operator >(ScriptRef a, ScriptRef b) => Compare(a, b) > 0;
	public static bool operator <=(ScriptRef a, ScriptRef b) => Compare(a, b) <= 0;
	public static bool operator >=(ScriptRef a, ScriptRef b) => Compare(a, b) >= 0;
}

////////////////////////////////////////////////////////////
//					parser definitions
////////////////////////////////////////////////////////////

/// <summary>Struct storing script type data.</summary>
public struct ScriptTypeData
{
	public ScriptRef name;
	public ArgEnum type;
	public TypeInfo meta;

	public ScriptTypeData(ScriptRef n, ArgEnum t, TypeInfo m)
	{
		name = n;
		type = t;
		meta = m;
	}
}

/// <summary>Struct storing value used by script (C++ raw memory + type).</summary>
public struct ScriptValueData : IEquatable<ScriptValueData>
{
	public ArgEnum type;
	public byte size;
	/// <summary>Int-like value.</summary>
	public int i;
	/// <summary>Object value (pointers, texts, ...).</summary>
	public object? o;

	public ScriptValueData(ArgEnum t, byte s, int iv, object? ov)
	{
		type = t;
		size = s;
		i = iv;
		o = ov;
	}

	/// <summary>Value of type int.</summary>
	public static implicit operator ScriptValueData(int v) => new(ArgInt, 4, v, null);

	/// <summary>Value of type RegEnum.</summary>
	public static ScriptValueData FromReg(RegEnum r) => new(ArgRegisteType<RegEnum>(), 2, (int)r, null);

	/// <summary>Value of type ScriptRef (text tokens).</summary>
	public static ScriptValueData FromRef(ScriptRef r) => new(ArgRegisteType<ScriptRef>(), 16, 0, r);

	/// <summary>Value of type pointer to <typeparamref name="T"/> (const pointer unless <paramref name="editable"/>).</summary>
	public static ScriptValueData FromPtr<T>(T? p, bool editable = false) where T : class
		=> new(ArgSpecAdd(ArgRegisteType<T>(), editable ? ArgSpecPtrE : ArgSpecPtr), 8, 0, p);

	/// <summary>Test if value have have selected type.</summary>
	public readonly bool IsValueType(ArgEnum t) => type == t;

	/// <summary>Test if value is register.</summary>
	public readonly bool IsRegValue => type == ArgRegisteType<RegEnum>();

	/// <summary>Get int value.</summary>
	public readonly int GetInt()
	{
		if (type != ArgInt)
		{
			throw new InvalidOperationException("Invalid cast of value");
		}
		return i;
	}

	/// <summary>Get register value.</summary>
	public readonly RegEnum GetReg()
	{
		if (!IsRegValue)
		{
			throw new InvalidOperationException("Invalid cast of value");
		}
		return (RegEnum)i;
	}

	/// <summary>Get ScriptRef value.</summary>
	public readonly ScriptRef GetRef()
	{
		if (type != ArgRegisteType<ScriptRef>())
		{
			throw new InvalidOperationException("Invalid cast of value");
		}
		return (ScriptRef)o!;
	}

	/// <summary>Get value with type check.</summary>
	public readonly int GetIntOfType(ArgEnum t)
	{
		if (type != t)
		{
			throw new InvalidOperationException("Invalid cast of value");
		}
		return i;
	}

	public readonly bool Equals(ScriptValueData other) => type == other.type && i == other.i && Equals(o, other.o);
	public override readonly bool Equals(object? obj) => obj is ScriptValueData d && Equals(d);
	public override readonly int GetHashCode() => HashCode.Combine(type, i);
	public static bool operator ==(ScriptValueData a, ScriptValueData b) => a.Equals(b);
	public static bool operator !=(ScriptValueData a, ScriptValueData b) => !a.Equals(b);
}

/// <summary>Struct used to store named definition used by script.</summary>
public struct ScriptRefData
{
	public ScriptRef name;
	public ArgEnum type;
	public ScriptValueData value;

	/// <summary>Constructor.</summary>
	public ScriptRefData(ScriptRef n, ArgEnum t)
	{
		name = n;
		type = t;
		value = default;
	}

	/// <summary>Constructor.</summary>
	public ScriptRefData(ScriptRef n, ArgEnum t, ScriptValueData v)
	{
		name = n;
		type = t;
		value = v;
	}

	/// <summary>Get true if this valid reference.</summary>
	public readonly bool IsValid => type != ArgInvalid;

	/// <summary>Is the value a register.</summary>
	public readonly bool IsRegValue => value.IsRegValue;

	/// <summary>Get register.</summary>
	public readonly RegEnum GetReg() => value.GetReg();

	/// <summary>Get register if value is a register or default value otherwise.</summary>
	public readonly RegEnum GetRegOrDefault(RegEnum def) => value.IsRegValue ? (RegEnum)value.i : def;
}

/// <summary>Function deciding how well arguments match an overload, 0 if not.</summary>
public delegate int ScriptOverloadFunc(ScriptProcData spd, ReadOnlySpan<ScriptRefData> args);

/// <summary>Function parsing arguments and writing operation.</summary>
public delegate bool ScriptParserFunc(ScriptProcData spd, ParserWriter ph, ReadOnlySpan<ScriptRefData> args);

/// <summary>Struct storing available operation to scripts.</summary>
public sealed class ScriptProcData
{
	public ScriptRef name;
	public ScriptRef description;

	public ScriptOverloadFunc overload = null!;
	/// <summary>For each argument position the accepted types, empty for internal arguments.</summary>
	public ArgEnum[][] overloadArg = [];

	public ScriptParserFunc parser = null!;
	/// <summary>Custom function (C++ <c>parserArg</c>/<c>parserGet</c>), null for built-in operations.</summary>
	public ScriptFuncGroup? parserArg;

	/// <summary>Have overload argument list (C++ <c>operator bool</c> of the range).</summary>
	public bool HaveOverloadArg => overloadArg.Length != 0;

	public bool Invoke(ParserWriter ph, ReadOnlySpan<ScriptRefData> args) => parser(this, ph, args);
}

/// <summary>Range of functions with same name (C++ <c>ScriptRange&lt;ScriptProcData&gt;</c>).</summary>
public readonly struct ScriptProcRange
{
	private readonly List<ScriptProcData>? _list;
	private readonly int _begin;
	private readonly int _end;

	public ScriptProcRange(List<ScriptProcData> l, int b, int e)
	{
		_list = l;
		_begin = b;
		_end = e;
	}

	public int Count => _end - _begin;
	public bool IsValid => _begin != _end;
	public ScriptProcData this[int i] => _list![_begin + i];
	public ScriptProcData First => _list![_begin];

	public IEnumerable<ScriptProcData> All()
	{
		for (int i = _begin; i < _end; ++i)
		{
			yield return _list![i];
		}
	}
}

/// <summary>Untyped access to tag index.</summary>
public interface IScriptTag
{
	/// <summary>Index of tag.</summary>
	int Index { get; }
}

/// <summary>Strong typed tag.</summary>
public readonly struct ScriptTag<T> : IEquatable<ScriptTag<T>>, IScriptTag
{
	/// <summary>Index that identify value in ScriptValues.</summary>
	public readonly ushort index;

	public ScriptTag(ushort i)
	{
		index = i;
	}

	/// <summary>Get value.</summary>
	public int Get() => index;
	int IScriptTag.Index => index;
	/// <summary>Test if tag have valid value.</summary>
	public bool IsValid => index != 0;

	public bool Equals(ScriptTag<T> other) => index == other.index;
	public override bool Equals(object? obj) => obj is ScriptTag<T> t && Equals(t);
	public override int GetHashCode() => index;
	public static bool operator ==(ScriptTag<T> a, ScriptTag<T> b) => a.index == b.index;
	public static bool operator !=(ScriptTag<T> a, ScriptTag<T> b) => a.index != b.index;

	/// <summary>Get run time value for type.</summary>
	public static ArgEnum Type() => ArgRegisteType<ScriptTag<T>>();
	/// <summary>Test if value can be used.</summary>
	public static bool IsValidIndex(int i) => i != 0 && i <= Limit();
	/// <summary>Fake constructor.</summary>
	public static ScriptTag<T> Make(int i) => new((ushort)i);
	/// <summary>Max supported value.</summary>
	public static int Limit() => ushort.MaxValue;
	/// <summary>Null value.</summary>
	public static ScriptTag<T> GetNullTag() => Make(0);
}

////////////////////////////////////////////////////////////
//					tags & global data
////////////////////////////////////////////////////////////

/// <summary>Global data shared by all scripts.</summary>
public class ScriptGlobal
{
	/// <summary>Function loading tag value.</summary>
	protected internal delegate void LoadFunc(ScriptGlobal global, ref int value, YamlNodeReader reader);
	/// <summary>Function saving tag value.</summary>
	protected internal delegate void SaveFunc(ScriptGlobal global, int value, YamlNodeWriter writer);

	protected internal struct TagValueType
	{
		public ScriptRef name;
		public LoadFunc? load;
		public SaveFunc? save;
	}

	protected internal struct TagValueData
	{
		public ScriptRef name;
		public int valueType;
	}

	protected internal sealed class TagData
	{
		public ScriptRef name;
		public int limit;
		public Func<int, ScriptValueData> crate = null!;
		public List<TagValueData> values = [];
	}

	protected void AddTagValueTypeBase(string name, LoadFunc loadFunc, SaveFunc saveFunc)
	{
		_tagValueTypes.Add(new TagValueType { name = AddNameRef(name), load = loadFunc, save = saveFunc });
	}

	private string _currFile = "";
	private readonly List<List<ScriptContainerBase>> _events = [];
	private readonly SortedDictionary<string, ScriptParserBase> _parserNames = new(StringComparer.Ordinal);
	private readonly Dictionary<string, ScriptParserEventsBase> _parserEvents = new(StringComparer.Ordinal);
	private readonly SortedDictionary<ArgEnum, TagData> _tagNames = [];
	private readonly List<TagValueType> _tagValueTypes = [];
	private readonly List<ScriptRefData> _refList = [];

	/// <summary>Get tag value.</summary>
	internal int GetTag(ArgEnum type, ScriptRef s)
	{
		if (_tagNames.TryGetValue(type, out var data))
		{
			for (int i = 0; i < data.values.Count; ++i)
			{
				var name = data.values[i].name;
				if (name == s)
				{
					return i + 1;
				}
			}
		}
		return 0;
	}

	/// <summary>Get data of tag value.</summary>
	internal TagValueData GetTagValueData(ArgEnum type, int i)
	{
		if (_tagNames.TryGetValue(type, out var data))
		{
			if (i != 0 && i <= data.values.Count)
			{
				return data.values[i - 1];
			}
		}
		return default;
	}

	/// <summary>Get tag value type data.</summary>
	internal TagValueType GetTagValueTypeData(int valueType)
	{
		if (valueType >= 0 && valueType < _tagValueTypes.Count)
		{
			return _tagValueTypes[valueType];
		}
		return default;
	}

	/// <summary>Get tag value type id.</summary>
	internal int GetTagValueTypeId(ScriptRef s)
	{
		for (int i = 0; i < _tagValueTypes.Count; ++i)
		{
			if (_tagValueTypes[i].name == s)
			{
				return i;
			}
		}
		return -1;
	}

	/// <summary>Add new tag name.</summary>
	internal int AddTag(ArgEnum type, ScriptRef s, int valueType)
	{
		if (!_tagNames.TryGetValue(type, out var data))
		{
			throw new InvalidOperationException("Unknown tag type");
		}
		var tag = GetTag(type, s);
		if (tag == 0)
		{
			// is tag used for other tag type?
			if (ScriptSort.FindSortHelper(_refList, s) >= 0)
			{
				return 0;
			}
			// is tag type valid?
			if (valueType < 0 || valueType >= _tagValueTypes.Count)
			{
				return 0;
			}
			// test to prevent warp of index value
			if (data.values.Count < data.limit)
			{
				data.values.Add(new TagValueData { name = s, valueType = valueType });
				ScriptSort.AddSortHelper(_refList, new ScriptRefData(s, type, data.crate(data.values.Count)));
				return data.values.Count;
			}
			return 0;
		}
		else
		{
			return tag;
		}
	}

	/// <summary>Add new name ref.</summary>
	private static ScriptRef AddNameRef(string s) => new(s);

	/// <summary>Default constructor.</summary>
	public ScriptGlobal()
	{
		AddTagValueTypeBase(
			"int",
			(ScriptGlobal s, ref int value, YamlNodeReader reader) =>
			{
				if (reader)
				{
					value = reader.ReadVal<int>();
				}
			},
			(s, value, writer) =>
			{
				writer.SetValue(value);
			}
		);
	}

	/// <summary>Store parser.</summary>
	public void PushParser(string groupName, ScriptParserBase parser)
	{
		if (parser is ScriptParserEventsBase events)
		{
			parser.LogScriptMetadata(true, groupName);
			_parserNames.TryAdd(parser.Name, parser);
			_parserEvents.TryAdd(parser.Name, events);
		}
		else
		{
			parser.LogScriptMetadata(false, groupName);
			_parserNames.TryAdd(parser.Name, parser);
		}
	}

	/// <summary>Add new const value.</summary>
	public void AddConst(string name, ScriptValueData i)
	{
		foreach (var p in _parserNames)
		{
			p.Value.AddConst(name, i);
		}
	}

	/// <summary>Update const value.</summary>
	public void UpdateConst(string name, ScriptValueData i)
	{
		foreach (var p in _parserNames)
		{
			p.Value.UpdateConst(name, i);
		}
	}

	/// <summary>Get global ref data.</summary>
	public ScriptRefData? GetRef(ScriptRef name, ScriptRef postfix = default)
	{
		int i = postfix.IsValid
			? ScriptSort.FindSortHelper(_refList, string.Concat(name.AsSpan(), postfix.AsSpan()))
			: ScriptSort.FindSortHelper(_refList, name);
		return i >= 0 ? _refList[i] : null;
	}

	/// <summary>Get all tag names.</summary>
	protected internal SortedDictionary<ArgEnum, TagData> TagNames => _tagNames;

	/// <summary>Get tag based on it name.</summary>
	public ScriptTag<T> GetTag<T>(ScriptRef s) => ScriptTag<T>.Make(GetTag(ScriptTag<T>.Type(), s));

	/// <summary>Get tag based on it name.</summary>
	public ScriptTag<T> GetTag<T>(string s) => GetTag<T>(ScriptRef.TempFrom(s));

	/// <summary>Add new tag name.</summary>
	public ScriptTag<T> AddTag<T>(string s, string valueTypeName)
	{
		return ScriptTag<T>.Make(AddTag(ScriptTag<T>.Type(), AddNameRef(s), GetTagValueTypeId(ScriptRef.TempFrom(valueTypeName))));
	}

	/// <summary>Add new type of tag.</summary>
	/// <param name="parentScriptName">C++ <c>Tag::Parent::ScriptName</c>.</param>
	public void AddTagType<T>(string parentScriptName)
	{
		if (!_tagNames.ContainsKey(ScriptTag<T>.Type()))
		{
			_tagNames.Add(
				ScriptTag<T>.Type(),
				new TagData
				{
					name = new ScriptRef(parentScriptName),
					limit = ScriptTag<T>.Limit(),
					crate = i => new ScriptValueData(ScriptTag<T>.Type(), 2, (ushort)i, null),
				}
			);
		}
	}

	/// <summary>Get current file that is loaded.</summary>
	public string CurrentFile => _currFile;

	/// <summary>Initialize shared globals like types.</summary>
	public virtual void InitParserGlobals(ScriptParserBase parser)
	{
	}

	/// <summary>Prepare for loading data.</summary>
	public virtual void BeginLoad()
	{
	}

	/// <summary>Prepare for loading file.</summary>
	public virtual void FileLoad(string path)
	{
		_currFile = path;
	}

	/// <summary>Finishing loading data.</summary>
	public virtual void EndLoad()
	{
		foreach (var p in _parserEvents)
		{
			_events.Add(p.Value.ReleseEvents());
		}
		_parserNames.Clear();
		_parserEvents.Clear();
		_currFile = "After-load validation";
	}

	/// <summary>Load global data from YAML.</summary>
	public void Load(YamlNodeReader reader)
	{
		var t = reader["tags"];
		if (t)
		{
			foreach (var p in _tagNames)
			{
				var nodeName = p.Value.name.ToString();
				var tags = t[nodeName];
				if (tags && tags.IsMap)
				{
					foreach (var tag in tags.Children())
					{
						var type = tag.ReadVal<string>();
						var name = tag.ReadKey<string>();
						var invalidType = _tagValueTypes.Count;
						var valueType = invalidType;
						for (int typei = 0; typei < invalidType; ++typei)
						{
							if (ScriptRef.TempFrom(type) == _tagValueTypes[typei].name)
							{
								valueType = typei;
								break;
							}
						}
						if (valueType != invalidType)
						{
							var namePrefix = "Tag." + name;
							var r = GetRef(ScriptRef.TempFrom(namePrefix));
							if (r != null && r.Value.type != p.Key)
							{
								Logger.Error("Script variable '" + name + "' already used in '" + _tagNames[r.Value.type].name.ToString() + "'.");
								continue;
							}
							var tagOffset = GetTag(p.Key, ScriptRef.TempFrom(namePrefix));
							if (tagOffset != 0)
							{
								var data = GetTagValueData(p.Key, tagOffset);
								if (valueType != data.valueType)
								{
									Logger.Error("Script variable '" + name + "' have wrong type '" + _tagValueTypes[valueType].name.ToString() + "' instead of '" + _tagValueTypes[data.valueType].name.ToString() + "' in '" + nodeName + "'.");
								}
								continue;
							}
							tagOffset = AddTag(p.Key, AddNameRef(namePrefix), valueType);
							if (tagOffset == 0)
							{
								Logger.Error("Script variable '" + name + "' exceeds limit of " + p.Value.limit + " available variables in '" + nodeName + "'.");
								continue;
							}
						}
						else
						{
							Logger.Error("Invalid type def '" + type + "' for script variable '" + name + "' in '" + nodeName + "'.");
						}
					}
				}
			}
		}
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

				if (!_parserEvents.TryGetValue(key, out var ev))
				{
					throw new InvalidOperationException("Unknown '" + key + "' node in 'scripts' at line " + p.LocationInFile.Line);
				}

				ev.LoadEvents(p);
			}
		}
	}
}

/// <summary>Collection of values for script usage.</summary>
public class ScriptValuesBase
{
	/// <summary>Vector with all available values for script.</summary>
	private List<int> values = [];

	/// <summary>Get all values.</summary>
	protected List<int> Values => values;

	/// <summary>Copy values from other (C++ copy assignment).</summary>
	protected void CopyFrom(ScriptValuesBase other)
	{
		values = [.. other.values];
	}

	/// <summary>Set value.</summary>
	protected void SetBase(int t, int i)
	{
		if (t != 0)
		{
			if (t > values.Count)
			{
				while (values.Count < t)
				{
					values.Add(0);
				}
			}
			values[t - 1] = i;
		}
	}

	/// <summary>Get value.</summary>
	protected int GetBase(int t)
	{
		if (t != 0 && t <= values.Count)
		{
			return values[t - 1];
		}
		return 0;
	}

	/// <summary>Load values from yaml file.</summary>
	protected void LoadBase(YamlNodeReader reader, ScriptGlobal shared, ArgEnum type, string nodeName)
	{
		var tags = reader[nodeName];
		if (tags)
		{
			if (tags.IsMap)
			{
				foreach (var tag in tags.Children())
				{
					int i = shared.GetTag(type, ScriptRef.TempFrom("Tag." + tag.ReadKey<string>()));
					if (i != 0)
					{
						var temp = 0;
						var data = shared.GetTagValueData(type, i);
						shared.GetTagValueTypeData(data.valueType).load!(shared, ref temp, tag);
						SetBase(i, temp);
					}
					else
					{
						Logger.Error("Error in tags: '" + tag.ReadKey<string>() + "' unknown tag name not defined in current file");
					}
				}
			}
		}
	}

	/// <summary>Save values to yaml file.</summary>
	protected void SaveBase(YamlNodeWriter writer, ScriptGlobal shared, ArgEnum type, string nodeName)
	{
		bool hasTags = false; // We have to know whether the object has any tags before creating a "tags" child node in the yaml
		for (int i = values.Count; i > 0 && !hasTags; --i) // It's usually the last one
			if (GetBase(i) != 0)
				hasTags = true;
		if (!hasTags)
			return;
		YamlNodeWriter tags = writer[nodeName];
		tags.SetAsMap();
		for (int i = 1; i <= values.Count; ++i)
		{
			int v = GetBase(i);
			if (v != 0)
			{
				var data = shared.GetTagValueData(type, i);
				string tagName = data.name.Substr(data.name.Find('.') + 1).ToString();
				YamlNodeWriter temp = tags[tagName];
				shared.GetTagValueTypeData(data.valueType).save!(shared, v, temp);
			}
		}
	}
}

/// <summary>Strong typed collection of values for script.</summary>
public sealed class ScriptValues<T> : ScriptValuesBase
{
	/// <summary>Load values from yaml file.</summary>
	public void Load(YamlNodeReader reader, ScriptGlobal shared, string nodeName = "tags")
	{
		LoadBase(reader, shared, ScriptTag<T>.Type(), nodeName);
	}

	/// <summary>Save values to yaml file.</summary>
	public void Save(YamlNodeWriter writer, ScriptGlobal shared, string nodeName = "tags")
	{
		SaveBase(writer, shared, ScriptTag<T>.Type(), nodeName);
	}

	/// <summary>Get value.</summary>
	public int Get(ScriptTag<T> t) => GetBase(t.Get());

	/// <summary>Set value.</summary>
	public void Set(ScriptTag<T> t, int i) => SetBase(t.Get(), i);

	/// <summary>Get all values.</summary>
	public IReadOnlyList<int> GetValuesRaw() => Values;

	/// <summary>Copy of values (C++ copy of value member).</summary>
	public ScriptValues<T> Clone()
	{
		var r = new ScriptValues<T>();
		r.CopyFrom(this);
		return r;
	}

	/// <summary>Assign values from other (C++ copy assignment).</summary>
	public void Assign(ScriptValues<T> other) => CopyFrom(other);
}

/// <summary>Helpers keeping script name lists sorted (C++ <c>addSortHelper</c>, <c>findSortHelper</c>, ...): by size first, then content.</summary>
internal static class ScriptSort
{
	private static ScriptRef NameOf<R>(R r) => r switch
	{
		ScriptRefData d => d.name,
		ScriptTypeData t => t.name,
		ScriptProcData p => p.name,
		_ => throw new ArgumentException("unknown type"),
	};

	/// <summary>Add new value to sorted vector by name (after equal names).</summary>
	public static void AddSortHelper<R>(List<R> vec, R value)
	{
		var name = NameOf(value);
		int lo = 0, hi = vec.Count;
		while (lo < hi)
		{
			int mid = (lo + hi) >> 1;
			if (ScriptRef.Compare(NameOf(vec[mid]), name) <= 0)
				lo = mid + 1;
			else
				hi = mid;
		}
		vec.Insert(lo, value);
	}

	/// <summary>Get bound of value, upper or lower.</summary>
	public static int BoundSortHelper<R>(List<R> vec, int begin, int end, ReadOnlySpan<char> name, bool upper)
	{
		int limit = upper ? 1 : 0;
		int lo = begin, hi = end;
		while (lo < hi)
		{
			int mid = (lo + hi) >> 1;
			if (ScriptRef.Compare(NameOf(vec[mid]), name) < limit)
				lo = mid + 1;
			else
				hi = mid;
		}
		return lo;
	}

	/// <summary>Helper function finding data by name, index or -1.</summary>
	public static int FindSortHelper<R>(List<R> vec, ReadOnlySpan<char> name)
	{
		var f = BoundSortHelper(vec, 0, vec.Count, name, false);
		if (f != vec.Count)
		{
			// check upper bound, if is different than lower, its mean we have hit
			if (f != BoundSortHelper(vec, f, f + 1, name, true))
			{
				return f;
			}
		}
		return -1;
	}

	/// <summary>Helper function finding data by name, index or -1.</summary>
	public static int FindSortHelper<R>(List<R> vec, ScriptRef name) => FindSortHelper(vec, name.AsSpan());

	/// <summary>Join name parts (C++ <c>ScriptRange&lt;ScriptRef&gt;</c> of a compound name).</summary>
	public static string Join(ReadOnlySpan<ScriptRef> parts)
	{
		int total = 0;
		foreach (var p in parts)
		{
			if (!p.IsValid) break;
			total += p.Size;
		}
		return string.Create(total, parts.ToArray(), (span, arr) =>
		{
			int k = 0;
			foreach (var p in arr)
			{
				if (!p.IsValid) break;
				p.AsSpan().CopyTo(span[k..]);
				k += p.Size;
			}
		});
	}
}

/// <summary>Unsafe helpers for value registers.</summary>
internal static class ScriptBox
{
	/// <summary>Get reference to boxed struct in register, creating a default one if empty.</summary>
	public static ref T RefValue<T>(ref object? slot) where T : struct
	{
		if (slot is not T)
		{
			slot = default(T);
		}
		return ref Unsafe.Unbox<T>(slot);
	}
}

/// <summary>
/// libstdc++ <c>std::sort</c> (introsort + final insertion sort), so that elements comparing equal end up in the same
/// (unstable) order as in the C++ game.
/// </summary>
internal static class StdSort
{
	private const int S_threshold = 16;

	/// <summary>Sort with strict weak ordering <paramref name="less"/>.</summary>
	public static void Sort<T>(IList<T> v, Func<T, T, bool> less)
	{
		int first = 0, last = v.Count;
		if (first != last)
		{
			IntrosortLoop(v, first, last, Lg(last - first) * 2, less);
			FinalInsertionSort(v, first, last, less);
		}
	}

	private static int Lg(int n)
	{
		int r = 0;
		while ((n >>= 1) != 0) ++r;
		return r;
	}

	private static void Swap<T>(IList<T> v, int a, int b) => (v[a], v[b]) = (v[b], v[a]);

	private static void IntrosortLoop<T>(IList<T> v, int first, int last, int depthLimit, Func<T, T, bool> less)
	{
		while (last - first > S_threshold)
		{
			if (depthLimit == 0)
			{
				HeapSelect(v, first, last, last, less);
				SortHeap(v, first, last, less);
				return;
			}
			--depthLimit;
			int cut = UnguardedPartitionPivot(v, first, last, less);
			IntrosortLoop(v, cut, last, depthLimit, less);
			last = cut;
		}
	}

	private static void MoveMedianToFirst<T>(IList<T> v, int result, int a, int b, int c, Func<T, T, bool> less)
	{
		if (less(v[a], v[b]))
		{
			if (less(v[b], v[c])) Swap(v, result, b);
			else if (less(v[a], v[c])) Swap(v, result, c);
			else Swap(v, result, a);
		}
		else if (less(v[a], v[c])) Swap(v, result, a);
		else if (less(v[b], v[c])) Swap(v, result, c);
		else Swap(v, result, b);
	}

	private static int UnguardedPartition<T>(IList<T> v, int first, int last, int pivot, Func<T, T, bool> less)
	{
		while (true)
		{
			while (less(v[first], v[pivot])) ++first;
			--last;
			while (less(v[pivot], v[last])) --last;
			if (!(first < last)) return first;
			Swap(v, first, last);
			++first;
		}
	}

	private static int UnguardedPartitionPivot<T>(IList<T> v, int first, int last, Func<T, T, bool> less)
	{
		int mid = first + (last - first) / 2;
		MoveMedianToFirst(v, first, first + 1, mid, last - 1, less);
		return UnguardedPartition(v, first + 1, last, first, less);
	}

	private static void UnguardedLinearInsert<T>(IList<T> v, int last, Func<T, T, bool> less)
	{
		T val = v[last];
		int next = last - 1;
		while (less(val, v[next]))
		{
			v[last] = v[next];
			last = next;
			--next;
		}
		v[last] = val;
	}

	private static void InsertionSort<T>(IList<T> v, int first, int last, Func<T, T, bool> less)
	{
		if (first == last) return;
		for (int i = first + 1; i != last; ++i)
		{
			if (less(v[i], v[first]))
			{
				T val = v[i];
				for (int k = i; k > first; --k) v[k] = v[k - 1];
				v[first] = val;
			}
			else
			{
				UnguardedLinearInsert(v, i, less);
			}
		}
	}

	private static void FinalInsertionSort<T>(IList<T> v, int first, int last, Func<T, T, bool> less)
	{
		if (last - first > S_threshold)
		{
			InsertionSort(v, first, first + S_threshold, less);
			for (int i = first + S_threshold; i != last; ++i) UnguardedLinearInsert(v, i, less);
		}
		else
		{
			InsertionSort(v, first, last, less);
		}
	}

	private static void PushHeap<T>(IList<T> v, int first, int holeIndex, int topIndex, T value, Func<T, T, bool> less)
	{
		int parent = (holeIndex - 1) / 2;
		while (holeIndex > topIndex && less(v[first + parent], value))
		{
			v[first + holeIndex] = v[first + parent];
			holeIndex = parent;
			parent = (holeIndex - 1) / 2;
		}
		v[first + holeIndex] = value;
	}

	private static void AdjustHeap<T>(IList<T> v, int first, int holeIndex, int len, T value, Func<T, T, bool> less)
	{
		int topIndex = holeIndex;
		int secondChild = holeIndex;
		while (secondChild < (len - 1) / 2)
		{
			secondChild = 2 * (secondChild + 1);
			if (less(v[first + secondChild], v[first + (secondChild - 1)])) secondChild--;
			v[first + holeIndex] = v[first + secondChild];
			holeIndex = secondChild;
		}
		if ((len & 1) == 0 && secondChild == (len - 2) / 2)
		{
			secondChild = 2 * (secondChild + 1);
			v[first + holeIndex] = v[first + (secondChild - 1)];
			holeIndex = secondChild - 1;
		}
		PushHeap(v, first, holeIndex, topIndex, value, less);
	}

	private static void MakeHeap<T>(IList<T> v, int first, int last, Func<T, T, bool> less)
	{
		if (last - first < 2) return;
		int len = last - first;
		int parent = (len - 2) / 2;
		while (true)
		{
			T value = v[first + parent];
			AdjustHeap(v, first, parent, len, value, less);
			if (parent == 0) return;
			parent--;
		}
	}

	private static void PopHeap<T>(IList<T> v, int first, int last, int result, Func<T, T, bool> less)
	{
		T value = v[result];
		v[result] = v[first];
		AdjustHeap(v, first, 0, last - first, value, less);
	}

	private static void HeapSelect<T>(IList<T> v, int first, int middle, int last, Func<T, T, bool> less)
	{
		MakeHeap(v, first, middle, less);
		for (int i = middle; i < last; ++i)
		{
			if (less(v[i], v[first])) PopHeap(v, first, middle, i, less);
		}
	}

	private static void SortHeap<T>(IList<T> v, int first, int last, Func<T, T, bool> less)
	{
		while (last - first > 1)
		{
			--last;
			PopHeap(v, first, last, last, less);
		}
	}
}
