using static Acom.ScriptArg;

namespace Acom.Tests.Engine;

/// <summary>
/// Script engine: the C++ self tests of Script.cpp (dummyTest*) and small scripts whose expected results follow
/// from the C++ semantics (integer arithmetic, blocks, loops, return order, texts, tags, events, error messages).
/// </summary>
[Collection(nameof(OptionsTests))] // swaps Logger.Sink and Options.debug
public sealed class ScriptTests
{
	/// <summary>Bindable test type (C++ DummyClass of the self tests).</summary>
	private sealed class DummyClass
	{

		public int value;
		public readonly ScriptValues<DummyClass> values = new();
		public List<DummyClass> children = [];

		public static void ScriptRegister(ScriptParserBase parser)
		{
			var b = new Bind<DummyClass>(parser);
			b.AddField("getValue", "setValue", "addValue", t => t.value, (t, v) => t.value = v);
			b.AddScriptValue(t => t.values);
			b.AddList("getChild", Bind<DummyClass>.Self, [ScriptParam.Int], ScriptParam.PtrRef<DummyClass>(), t => t.children,
				(DummyClass t, DummyClass? v, ref ScriptArgs a, int off) => v != null && v.value >= a.Int(off));
			b.AddDebugDisplay(t => t == null ? "null" : "Dummy(" + t.value + ")");
		}
	}

	static ScriptTests()
	{
		ScriptBindTable.Add<DummyClass>("DummyClass", DummyClass.ScriptRegister);
	}

	private sealed class Log : IDisposable
	{
		private readonly Action<SeverityLevel, string> _old;
		public readonly List<string> lines = [];

		public Log()
		{
			_old = Logger.Sink;
			Logger.Sink = (level, message) => { lock (lines) lines.Add(message); };
		}

		public void Dispose() => Logger.Sink = _old;

		public bool Has(string s) => lines.Exists(l => l.Contains(s, StringComparison.Ordinal));
	}

	private static ScriptParser MakeParser(ScriptGlobal g, ScriptArgName[] outputs, ScriptArgName[] inputs)
	{
		return new ScriptParser(g, "test", outputs, inputs);
	}

	/// <summary>Parse and run script with outputs `r` (int&amp;) and `s` (int&amp;) and inputs `a`, `b` (int).</summary>
	private static (int r, int s, bool parsed) Run(string code, int r = 0, int s = 0, int a = 0, int b = 0)
	{
		var g = new ScriptGlobal();
		var p = MakeParser(g, [new("r", ScriptParam.IntRef), new("s", ScriptParam.IntRef)], [new("a", ScriptParam.Int), new("b", ScriptParam.Int)]);
		var c = new ScriptContainer();
		c.LoadContainer("unit test", code, p);
		var arg = new ScriptOutputArgs(r, s);
		new ScriptWorker(p.Signature, a, b).Execute(c, arg);
		return (arg.GetFirst(), arg.GetSecond(), c.IsValid);
	}

	[Fact]
	public void Arithmetic()
	{
		Assert.Equal(21, Run("add r 5; mul r 3; return r s;", r: 2).r);
		Assert.Equal(-7 / 2, Run("set r -7; div r 2; return r s;").r);
		Assert.Equal(-7 % 2, Run("set r -7; mod r 2; return r s;").r);
		Assert.Equal(int.MinValue, Run("set r 0x7FFFFFFF; add r 1; return r s;").r);
		Assert.Equal(1, Run("set r 100; muldiv r 100 10000; return r s;").r);
		Assert.Equal(int.MaxValue / 2, Run("set r 0x7FFFFFFF; muldiv r 50 100; return r s;").r);
		Assert.Equal(1, Run("set r 0x7FFFFFFF; offsetmod r 100 1 100; return r s;").r);
		Assert.Equal(3, Run("set r -7; offsetmod r 1 0 5; return r s;").r);
		Assert.Equal(1 + 4 * 5, Run("set r 1; aggregate r 4 5; return r s;").r);
		Assert.Equal(2 * 4 + 5, Run("set r 2; offset r 4 5; return r s;").r);
		Assert.Equal(8, Run("set r 2; pow r 3; return r s;").r);
		Assert.Equal(1, Run("set r 2; pow r -3; return r s;").r);
		Assert.Equal(int.MinValue, Run("set r 10; pow r 20; return r s;").r); // x86 cvttsd2si
		Assert.Equal(3, Run("set r 15; sqrt r; return r s;").r);
		Assert.Equal(0, Run("set r -15; sqrt r; return r s;").r);
		Assert.Equal(int.MinValue, Run("set r -2147483648; abs r; return r s;").r);
		Assert.Equal(5, Run("set r -5; abs r; return r s;").r);
		Assert.Equal(10, Run("set r 50; limit r 0 10; return r s;").r);
		Assert.Equal(3, Run("set r 0b1011; bit_count r; return r s;").r);
		Assert.Equal(~0x0F, Run("set r 0x0F; bit_not r; return r s;").r);
		Assert.Equal(0x0F0 >> 4, Run("set r 0x0F0; shr r 4; return r s;").r);
		Assert.Equal(1 << 31, Run("set r 1; shl r 31; return r s;").r);
		Assert.Equal(-8 >> 1, Run("set r -8; shr r 1; return r s;").r);
		Assert.Equal(8, Run("set r 0o10; return r s;").r);
		Assert.Equal(-0x10, Run("set r -0x10; return r s;").r);
		Assert.Equal(12, Run("set r 012; return r s;").r);
		Assert.Equal(5, Run("set r +5; return r s;").r);
	}

	[Fact]
	public void ColorAndWaveOperations()
	{
		Assert.Equal(0x3, Run("set r 0x35; get_color r r; return r s;").r);
		Assert.Equal(0x5, Run("set r 0x35; get_shade r r; return r s;").r);
		Assert.Equal(0x75, Run("set r 0x35; set_color r 7; return r s;").r);
		Assert.Equal(0x0F, Run("set r 0x35; add_shade r 20; return r s;").r); // too dark: 0xF (black)
		Assert.Equal(0x37, Run("set r 0x35; add_shade r 2; return r s;").r);
		Assert.Equal(0x30, Run("set r 0x35; add_shade r -5; return r s;").r);
		Assert.Equal(0x01, Run("set r 0x05; add_shade r -10; return r s;").r);
		Assert.Equal(7, Run("set r 13; wavegen_rect r 10 5 7; return r s;").r);
		Assert.Equal(0, Run("set r 17; wavegen_rect r 10 5 7; return r s;").r);
		Assert.Equal(3, Run("set r 13; wavegen_tri r 10 8 7; return r s;").r);
		Assert.Equal(100, Run("set r 25; wavegen_sin r 100 100; return r s;").r);
		Assert.Equal(-100, Run("set r 50; wavegen_cos r 100 100; return r s;").r);
	}

	[Fact]
	public void ErrorStopsExecution()
	{
		using var log = new Log();
		var (r, _, parsed) = Run("set r 10; div r 0; set r 5; return r s;");
		Assert.True(parsed);
		Assert.Equal(10, r);
		Assert.True(log.Has("Invalid script operation"));
	}

	[Fact]
	public void IfElse()
	{
		const string code = "if gt a 5; set r 1; else lt a 0; set r 2; else; set r 3; end; return r s;";
		Assert.Equal(1, Run(code, a: 6).r);
		Assert.Equal(2, Run(code, a: -1).r);
		Assert.Equal(3, Run(code, a: 5).r);
		Assert.Equal(3, Run(code, a: 0).r);

		const string and = "if and gt a 1 lt a 10 neq a 5; set r 1; end; return r s;";
		Assert.Equal(1, Run(and, a: 2).r);
		Assert.Equal(0, Run(and, a: 5).r);
		Assert.Equal(0, Run(and, a: 10).r);

		const string or = "if or eq a 1 eq a 3 ge a 10; set r 1; end; return r s;";
		Assert.Equal(1, Run(or, a: 1).r);
		Assert.Equal(1, Run(or, a: 3).r);
		Assert.Equal(0, Run(or, a: 2).r);
		Assert.Equal(1, Run(or, a: 10).r);
		Assert.Equal(0, Run(or, a: 9).r);
	}

	[Fact]
	public void Loops()
	{
		Assert.Equal(45, Run("var int x 0; loop var i 10; add x i; end; set r x; return r s;").r);
		Assert.Equal(0 + 1 + 2 + 3, Run("loop var i 10; if eq i 4; break; end; add r i; end; return r s;").r);
		Assert.Equal(1 + 3 + 5 + 7 + 9, Run("loop var i 10; var int t i; mod t 2; if eq t 0; continue; end; add r i; end; return r s;").r);
		Assert.Equal(0, Run("loop var i a; add r 1; end; return r s;", a: -5).r);
		Assert.Equal(6, Run("begin; var int x 6; set r x; end; return r s;").r);
	}

	[Fact]
	public void ReturnOrder()
	{
		Assert.Equal((2, 1), Ret(Run("return s r;", r: 1, s: 2)));
		// C++ quirk kept: `r` is still needed as source of `s` when the loop looks for free registers, so it is never assigned
		Assert.Equal((1, 1), Ret(Run("var int x 7; return x r;", r: 1, s: 2)));
		Assert.Equal((7, 1), Ret(Run("var int x 7; return x s;", r: 1, s: 1)));
		// input registers are not compatible with output variables
		using (var log = new Log())
		{
			Assert.False(Run("return a r;").parsed);
			Assert.True(log.Has("Invalid return argument 'a'"));
		}
		Assert.Equal((3, 4), Ret(Run("return 3 4;", r: 1, s: 2)));
		Assert.Equal((1, 1), Ret(Run("return r r;", r: 1, s: 2)));

		static (int, int) Ret((int r, int s, bool p) x) => (x.r, x.s);
	}

	[Fact]
	public void TextValues()
	{
		Assert.Equal(1, Run("var text t \"abc\"; if eq t \"abc\"; set r 1; end; return r s;").r);
		Assert.Equal(0, Run("var text t \"abc\"; if eq t \"ab\"; set r 1; end; return r s;").r);
		Assert.Equal(1, Run("var text t; var text u; if eq t u; set r 1; end; return r s;").r);
		Assert.Equal(1, Run("var text t; var text u \"\"; if eq t u; set r 1; end; return r s;").r); // `var text t;` clears to "" (not null)
		Assert.Equal(1, Run("var text t \"a\\\"b\"; if eq t \"a\\\"b\"; set r 1; end; return r s;").r);
	}

	[Fact]
	public void ConstDefinition()
	{
		Assert.Equal(42, Run("const int x 42; set r x; return r s;").r);
		using var log = new Log();
		Assert.False(Run("const int x a; set r x; return r s;").parsed);
		Assert.True(log.Has("Invalid value 'a' for const type 'x'"));
	}

	[Fact]
	public void ParseErrors()
	{
		using (var log = new Log())
		{
			Assert.False(Run("add r 5;").parsed);
			Assert.True(log.Has("Error in parsing script 'test' for 'unit test': script need to end with return statement"));
		}
		using (var log = new Log())
		{
			Assert.False(Run("if eq r 1; return r s;").parsed);
			Assert.True(log.Has("script have missed 'end;'"));
		}
		using (var log = new Log())
		{
			Assert.False(Run("add q 5; return r s;").parsed);
			Assert.True(log.Has("Incorrect type of argument 'q' of type []"));
			Assert.True(log.Has("Error in matching arguments for operator 'add'"));
			Assert.True(log.Has("invalid operation in line: 'add q 5;' (at 1)"));
		}
		using (var log = new Log())
		{
			Assert.False(Run("add r 0x; return r s;").parsed);
			Assert.True(log.Has("invalid argument '0x' in line: 'add r 0x;' (at 1)"));
		}
		using (var log = new Log())
		{
			Assert.False(Run("return r s; add r 1;").parsed);
			Assert.True(log.Has("unreachable code after return in line: 'add r 1;'"));
		}
		using (var log = new Log())
		{
			Assert.False(Run("add r 1; var int x; return r s;").parsed);
			Assert.True(log.Has("invalid variable definition after other operations"));
		}
		using (var log = new Log())
		{
			Assert.False(Run("set a 1; return r s;").parsed);
			Assert.True(log.Has("Incorrect type of argument 'a' of type [int]") || log.Has("Can't match overload for operator 'set'"));
		}
		using (var log = new Log())
		{
			Assert.False(Run("return r;").parsed);
			Assert.True(log.Has("Invalid length of returns arguments"));
		}
		using (var log = new Log())
		{
			Assert.False(Run("foo r; return r s;").parsed);
			Assert.True(log.Has("Invalid operation 'foo'"));
		}
		using (var log = new Log())
		{
			Assert.False(Run("r.foo 1; return r s;").parsed);
			Assert.True(log.Has("Unsupported type for variable 'r'"));
		}
		using (var log = new Log())
		{
			Assert.False(Run("end; return r s;").parsed);
			Assert.True(log.Has("Unexpected 'end'"));
		}
		using (var log = new Log())
		{
			Assert.False(Run("break; return r s;").parsed);
			Assert.True(log.Has("Operation 'break' outside 'loop'"));
		}
	}

	[Fact]
	public void RegisterLimit()
	{
		// 512 bytes of registers: 2 outputs + 2 inputs use 16 bytes, 124 more ints fit
		var vars = string.Concat(Enumerable.Range(0, 124).Select(i => "var int v" + i + "; "));
		Assert.True(Run(vars + "return r s;").parsed);
		using var log = new Log();
		Assert.False(Run(vars + "var int last; return r s;").parsed);
		Assert.True(log.Has("Invalid type for variable 'last'"));
	}

	[Fact]
	public void DebugLog()
	{
		var old = Options.debug;
		try
		{
			Options.debug = true;
			using var log = new Log();
			Run("debug_log \"value\" a 5; return r s;", a: 3);
			Assert.True(log.Has("Script debug log: value 3 5"));
		}
		finally
		{
			Options.debug = old;
		}
	}

	[Fact]
	public void PointerTypeTagsAndLists()
	{
		var g = new ScriptGlobal();
		var p = MakeParser(g, [new("r", ScriptParam.IntRef)], [new("obj", ScriptParam.PtrE<DummyClass>()), new("cobj", ScriptParam.Ptr<DummyClass>())]);

		var root = new YamlRootNodeReader("tags:\n  DummyClass:\n    FOO: int\n    BAR: int\n", "t");
		g.Load(root.ToBase());

		var d = new DummyClass { value = 5 };
		var tagFoo = g.GetTag<DummyClass>("Tag.FOO");
		Assert.Equal(1, tagFoo.Get());
		Assert.Equal(2, g.GetTag<DummyClass>("Tag.BAR").Get());

		int run(string code, DummyClass? cobj = null)
		{
			var c = new ScriptContainer();
			c.LoadContainer("unit test", code, p);
			Assert.True(c.IsValid);
			var arg = new ScriptOutputArgs(0);
			new ScriptWorker(p.Signature, d, cobj).Execute(c, arg);
			return arg.GetFirst();
		}

		Assert.Equal(5, run("obj.getValue r; return r;"));
		Assert.Equal(0, run("cobj.getValue r; return r;"));
		Assert.Equal(7, run("obj.addValue 2; obj.getValue r; return r;"));
		Assert.Equal(7, d.value);

		run("obj.setTag Tag.FOO 11; return r;");
		Assert.Equal(11, d.values.Get(tagFoo));
		Assert.Equal(11, run("obj.getTag r Tag.FOO; return r;"));
		Assert.Equal(1, run("var ptr DummyClass x; set x obj; if eq x obj; set r 1; end; return r;"));
		Assert.Equal(1, run("if eq cobj null; set r 1; end; return r;"));

		d.children = [new DummyClass { value = 1 }, new DummyClass { value = 4 }, new DummyClass { value = 9 }];
		Assert.Equal(4 + 9, run("loop var c obj.getChild.list 3; var int t; c.getValue t; add r t; end; return r;"));

		// editable pointer required for setters
		using var log = new Log();
		var bad = new ScriptContainer();
		bad.LoadContainer("unit test", "cobj.setValue 1; return r;", p);
		Assert.False(bad.IsValid);
		Assert.True(log.Has("Can't match overload for operator 'DummyClass.setValue' for:"));

		// tag values save and load
		var w = new YamlRootNodeWriter();
		w.SetAsMap();
		d.values.Save(w, g);
		Assert.Contains("FOO: 11", w.Emit(), StringComparison.Ordinal);
		var d2 = new DummyClass();
		d2.values.Load(w.ToReader(), g);
		Assert.Equal(11, d2.values.Get(tagFoo));
	}

	[Fact]
	public void EventsRunAroundScript()
	{
		var g = new ScriptGlobal();
		var p = new ScriptParserEvents(g, "eventTest", [new("r", ScriptParam.IntRef), new("s", ScriptParam.Int)], []);
		g.PushParser("group", p);
		var root = new YamlRootNodeReader(
			"scripts:\n" +
			"  eventTest:\n" +
			"    - offset: -1\n" +
			"      code: 'mul r 10; add r s; return r;'\n" +
			"    - offset: 2\n" +
			"      code: 'add r 1000; return r;'\n" +
			"    - offset: 1\n" +
			"      code: 'mul r 2; return r;'\n", "t");
		g.Load(root.ToBase());

		var c = new ScriptContainerEvents();
		c.LoadContainer("unit test", "add r 5; return r;", p);
		g.EndLoad();

		var arg = new ScriptOutputArgs(1, 3);
		new ScriptWorker(p.Signature).Execute(c, arg);
		Assert.Equal(((1 * 10 + 3) + 5) * 2 + 1000, arg.GetFirst());
	}

	// C++ dummyTestScriptRefTokens
	[Fact]
	public void Tokens()
	{
		var srt = new ScriptRefTokens("aaaa bb");
		var next = srt.GetNextToken();
		Assert.True(next.range == new ScriptRef("aaaa") && next.Type == TokenEnum.TokenSymbol);
		next = srt.GetNextToken();
		Assert.True(next.range == new ScriptRef("bb") && next.Type == TokenEnum.TokenSymbol);
		Assert.Equal(TokenEnum.TokenNone, srt.GetNextToken().Type);

		var g = new ScriptGlobal();
		var p = MakeParser(g, [], []);
		var help = new ParserWriter(0, new ScriptContainerBase(), p);
		srt = new ScriptRefTokens("0x10 1234 0b100 0o10 0x0f 0xAb");
		foreach (var (text, value) in new[] { ("0x10", 0x10), ("1234", 1234), ("0b100", 4), ("0o10", 8), ("0x0f", 15), ("0xAb", 0xAB) })
		{
			next = srt.GetNextToken();
			Assert.True(next.range == new ScriptRef(text) && next.Type == TokenEnum.TokenNumber);
			var r = next.Parse(help);
			Assert.Equal(ArgInt, r.type);
			Assert.Equal(value, r.value.GetInt());
		}
		Assert.Equal(TokenEnum.TokenNone, srt.GetNextToken().Type);

		static TokenEnum getType(string s, TokenEnum next = TokenEnum.TokenNone) => new ScriptRefTokens(s).GetNextToken(next).Type;

		Assert.Equal(TokenEnum.TokenInvalid, getType(":"));
		Assert.Equal(TokenEnum.TokenColon, getType(":", TokenEnum.TokenColon));
		Assert.Equal(TokenEnum.TokenNone, getType(";"));
		Assert.Equal(TokenEnum.TokenSemicolon, getType(";", TokenEnum.TokenSemicolon));
		Assert.Equal(TokenEnum.TokenText, getType("\"aaa\""));
		Assert.Equal(TokenEnum.TokenNumber, getType("0x1"));
		Assert.Equal(TokenEnum.TokenNone, getType(""));
		Assert.Equal(TokenEnum.TokenNone, getType(" "));
		Assert.Equal(TokenEnum.TokenNone, getType("#aaaaa"));
		Assert.Equal(TokenEnum.TokenNone, getType("  #  1235"));
		Assert.Equal(TokenEnum.TokenNumber, getType("  #  \n1235"));
		Assert.Equal(TokenEnum.TokenSymbol, getType(" a"));
		Assert.Equal(TokenEnum.TokenSymbol, getType(" \na"));
		Assert.Equal(TokenEnum.TokenSymbol, getType("a111"));
		Assert.Equal(TokenEnum.TokenInvalid, getType("0x"));
		Assert.Equal(TokenEnum.TokenInvalid, getType("0xk"));
	}

	// C++ dummyTestScriptStringRef
	[Fact]
	public void StringRef()
	{
		static ScriptRef R(string s) => new(s);
		Assert.True(R("foo") == R("foo").Substr(0));
		Assert.True(R("oo") == R("foo").Substr(1));
		Assert.True(R("o") == R("foo").Substr(2));
		Assert.True(R("") == R("foo").Substr(3));
		Assert.True(R("") == R("foo").Substr(4));
		Assert.True(R("") == R("foo1234").Substr(3, 0));
		Assert.True(R("1") == R("foo1234").Substr(3, 1));
		Assert.True(R("1234") == R("foo1234").Substr(3, 4));
		Assert.True(R("1234") == R("foo1234").Substr(3, 5));
		Assert.True(R("") == R("12345").Head(0));
		Assert.True(R("123") == R("12345").Head(3));
		Assert.True(R("12345") == R("12345").Head(6));
		Assert.True(R("2345") == R("12345").Tail(1));
		Assert.True(R("") == R("12345").Tail(6));
		Assert.True(R("") == R("12345").HeadFromEnd(0));
		Assert.True(R("45") == R("12345").HeadFromEnd(2));
		Assert.True(R("12345") == R("12345").HeadFromEnd(6));
		Assert.True(R("12345") == R("12345").TailFromEnd(0));
		Assert.True(R("123") == R("12345").TailFromEnd(2));
		Assert.True(R("") == R("12345").TailFromEnd(6));
	}

	// C++ dummyTestScriptRefCompound
	[Fact]
	public void RefCompound()
	{
		var t = new ScriptRefCompound();
		Assert.Equal("", t.ToString());
		Assert.True(t.TryPushBack(new ScriptRef("f1")));
		Assert.True(t.TryPushBack(new ScriptRef("f2")));
		Assert.True(t.TryPushBack(new ScriptRef("f3")));
		Assert.True(t.TryPushBack(new ScriptRef("f4")));
		Assert.False(t.TryPushBack(new ScriptRef("f5")));
		Assert.Equal("f1f2f3f4", t.ToString());
		Assert.True(t.TryPopBack());
		Assert.Equal("f1f2f3", t.ToString());
		Assert.True(t.TryPopBack());
		Assert.True(t.TryPopBack());
		Assert.True(t.TryPopBack());
		Assert.False(t.TryPopBack());
		Assert.True(t.TryPushBack(new ScriptRef("f6")));
		Assert.Equal("f6", t.ToString());
	}

	// C++ dummyTestScriptArgList
	[Fact]
	public void ArgList()
	{
		var list1 = new ScriptArgList();
		var list2 = new ScriptArgList();
		var list3 = new ScriptArgList();
		var arg_a = new ScriptRefData(new ScriptRef("a"), ArgInvalid);
		var arg_b = new ScriptRefData(new ScriptRef("b"), ArgInvalid);
		Assert.True(list1.TryPushBack(arg_a));
		Assert.True(list1.TryPushBack(arg_b));
		Assert.True(list2.TryPushBack(list1.Span));
		Assert.True(list2.TryPushBack(list1.Span));
		Assert.Equal(4, list2.Size);
		Assert.True(list3.TryPushBack(list2.Span));
		Assert.True(list3.TryPushBack(list2.Span));
		Assert.True(list3.TryPushBack(list3.Span));
		Assert.Equal(16, list3.Size);
		Assert.False(list3.TryPushBack(list3.Span));
		Assert.False(list3.TryPushBack(arg_a));
		Assert.Equal(16, list3.Size);
	}

	// C++ dummyTestScriptOverload
	[Fact]
	public void Overload()
	{
		var data_a = new ScriptProcData { overload = ScriptParseHelper.OverloadCustomProc, overloadArg = new ScriptFuncGroup([ScriptParam.Context, ScriptParam.Int, ScriptParam.IntRef], (ref ScriptArgs a) => { }).OverloadType() };
		var data_b = new ScriptProcData { overload = ScriptParseHelper.OverloadCustomProc, overloadArg = new ScriptFuncGroup([ScriptParam.Int, ScriptParam.IntRef], (ref ScriptArgs a) => { }).OverloadType() };

		var arg_any = ArgInvalid;
		var arg_int = ArgInt;
		var arg_int_var = ArgSpecAdd(ArgInt, ArgSpecVar);

		static int test(ScriptProcData a, params ArgEnum[] r) => ScriptParseHelper.OverloadCustomProc(a, r.Select(t => new ScriptRefData(default, t)).ToArray());

		Assert.Equal(3, data_a.overloadArg.Length);
		Assert.Equal(2, ScriptParseHelper.GetOverloadArgSize(data_a.overloadArg));
		Assert.Equal(2, ScriptParseHelper.GetOverloadArgSize(data_b.overloadArg));
		Assert.Equal(0, test(data_a));
		Assert.Equal(0, test(data_a, arg_any));
		Assert.Equal(255, test(data_a, arg_any, arg_any));
		Assert.Equal(255 - 1, test(data_a, arg_int, arg_any));
		Assert.Equal(0, test(data_a, arg_int, arg_int));
		Assert.Equal(255 - 1, test(data_a, arg_int, arg_int_var));
		Assert.Equal(255, test(data_a, arg_any, arg_int_var));
		Assert.Equal(0, test(data_a, arg_any, arg_int_var, arg_any));
		Assert.Equal(255 - 64 - 1, test(data_a, arg_int_var, arg_any));
		Assert.Equal(255 - 64 - 1, test(data_a, arg_int_var, arg_int_var));

		Assert.Equal([arg_int, ArgSpecAdd(ArgInt, ArgSpecReg)], ScriptParseHelper.GetOverloadArgType(data_a.overloadArg, 0));
		Assert.Equal([arg_int_var], ScriptParseHelper.GetOverloadArgType(data_a.overloadArg, 1));
		Assert.Empty(ScriptParseHelper.GetOverloadArgType(data_a.overloadArg, 2));
	}

	// C++ dummyTestScriptFunctionParser
	[Fact]
	public void FunctionParser()
	{
		var g = new ScriptGlobal();
		var f = MakeParser(g, [], []);
		f.RegisterPointerType<DummyClass>();
		var help = new ParserWriter(0, new ScriptContainerBase(), f);
		Assert.True(help.AddReg(new ScriptRef("foo"), ScriptParam.PtrERef<DummyClass>().ArgType).IsValid);
		Assert.True(help.AddReg(new ScriptRef("bar.a"), ScriptParam.PtrERef<DummyClass>().ArgType).IsValid);
		Assert.True(help.AddReg(new ScriptRef("Tag.foo"), ScriptParam.PtrERef<DummyClass>().ArgType).IsValid);

		Assert.True(f.GetProc(new ScriptRef("DummyClass.getValue")).IsValid);
		Assert.True(f.GetProc(new ScriptRefCompound { p0 = new("DummyClass"), p1 = new("."), p2 = new("get"), p3 = new("Value") }).IsValid);

		var r = ScriptParseHelper.FindOperationAndArg(help, new ScriptRef("if"));
		Assert.True(r.IsValid && !r.HaveArg && r.HaveProc);
		r = ScriptParseHelper.FindOperationAndArg(help, new ScriptRef("foo.getValue"));
		Assert.True(r.IsValid && r.HaveArg && r.argName == new ScriptRef("foo"));
		r = ScriptParseHelper.FindOperationAndArg(help, new ScriptRef("bar.a.getValue"));
		Assert.True(r.IsValid && r.HaveArg && r.argName == new ScriptRef("bar.a"));
		r = ScriptParseHelper.FindOperationAndArg(help, new ScriptRef("Tag.foo.getValue"));
		Assert.True(r.IsValid && r.argName == new ScriptRef("Tag.foo"));
		r = ScriptParseHelper.FindOperationAndArg(help, new ScriptRef("bar.a2.getValue"));
		Assert.True(!r.IsValid && r.HaveArg && r.argName == new ScriptRef("bar"));
		r = ScriptParseHelper.FindOperationAndArg(help, new ScriptRef("Tag.foo2.getValue"));
		Assert.True(!r.IsValid && r.HaveArg && r.argName == new ScriptRef("Tag.foo2"));

		r = ScriptParseHelper.FindOperationAndArg(help, new ScriptRef("Tag.foo.getValue"));
		Assert.True(ScriptParseHelper.ReplaceOperation(help, r, new ScriptRef("getValue"), new ScriptRef("setValue")).IsValid);
		Assert.True(ScriptParseHelper.ReplaceOperation(help, r, new ScriptRef("get"), new ScriptRef("add")).IsValid == false); // "getValue" does not end with "get"
		Assert.True(ScriptParseHelper.ReplaceOperation(help, r, new ScriptRef("Value"), new ScriptRef("Value")).IsValid);
		Assert.False(ScriptParseHelper.ReplaceOperation(help, r, new ScriptRef("Value"), new ScriptRef("Foo")).IsValid);
	}

	// C++ dummyTestScriptLowerBound
	[Fact]
	public void SortHelpers()
	{
		var test = new List<ScriptTypeData>();
		foreach (var s in new[] { "b", "bb", "bbb", "bbbb", "c", "cc", "ccc", "a", "aa", "aaa", "aaaa", "aaab", "aaaba", "aaaaa", "abcde", "abcdf" })
		{
			ScriptSort.AddSortHelper(test, new ScriptTypeData(new ScriptRef(s), ArgInvalid, default));
		}
		// sorted by size, then content
		Assert.Equal(["a", "b", "c", "aa", "bb", "cc", "aaa", "bbb", "ccc", "aaaa", "aaab", "bbbb", "aaaaa", "aaaba", "abcde", "abcdf"], test.Select(t => t.name.ToString()));
		Assert.True(ScriptSort.FindSortHelper(test, "aa") >= 0);
		Assert.True(ScriptSort.FindSortHelper(test, "www") < 0);
		Assert.Equal(ScriptSort.FindSortHelper(test, "abcde"), ScriptSort.FindSortHelper(test, ScriptSort.Join([new("ab"), new("cde")])));
	}

	// C++ dummyTestFunctions
	[Fact]
	public void HelperFunctions()
	{
		static int mulDiv(int reg, int mul, int div)
		{
			ScriptBuiltin.mulDiv_h(ref reg, mul, div);
			return reg;
		}
		Assert.Equal(1, mulDiv(1, 100, 100));
		Assert.Equal(1, mulDiv(2, 50, 100));
		Assert.Equal(int.MaxValue, mulDiv(int.MaxValue, 100, 100));
		Assert.Equal(int.MaxValue / 2, mulDiv(int.MaxValue, 50, 100));

		static int mulAddMod(int reg, int mul, int add, int div)
		{
			ScriptBuiltin.mulAddMod_h(ref reg, mul, add, div);
			return reg;
		}
		Assert.Equal(1, mulAddMod(100, 100, 1, 100));
		Assert.Equal(1, mulAddMod(int.MaxValue, 100, 1, 100));
	}
}
