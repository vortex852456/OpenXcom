// Golden values for the OXCE script engine (src/Engine/Script.cpp): each case parses one script for a parser
// with outputs `r`, `s` (int&) and inputs `a`, `b` (int), runs it and prints the results and every log line.
// Event scripts ("events" cases) are loaded from YAML like `extended: scripts:` and run around the script.
// Output per case:
//   case <escaped code> <r> <s> <a> <b> [<debug>]
//   [events <escaped yaml>]
//   log <message>              (zero or more)
//   result <parsed 0/1> <r> <s>
// The C# test (Engine/ScriptGoldenTests) replays the cases and compares everything.
// Build/run: port/tools/golden/generate.sh
#include <cstdio>
#include <string>
#include <sstream>
#include <vector>
#include "Engine/Script.h"
#include "Engine/ScriptBind.h"
#include "Engine/Options.h"
#include "Engine/CrossPlatform.h"

using namespace OpenXcom;

static std::vector<std::string> g_log;

namespace OpenXcom
{
namespace Options { bool debug = false; bool verboseLogging = false; }
namespace CrossPlatform
{
void log(int, const std::ostringstream& msg) { g_log.push_back(msg.str()); }
}
}

static std::string escape(const std::string& s)
{
	std::string r;
	for (char c : s)
	{
		if (c == '\\') r += "\\\\";
		else if (c == '\n') r += "\\n";
		else if (c == '\t') r += "\\t";
		else if (c == ' ') r += "\\s";
		else r += c;
	}
	return r.empty() ? "\\e" : r;
}

using Output = ScriptOutputArgs<int&, int&>;

struct TestParser : ScriptParserEvents<Output, int, int>
{
	TestParser(ScriptGlobal* g) : ScriptParserEvents<Output, int, int>(g, "test", "r", "s", "a", "b") { }
};

struct Case
{
	std::string code;
	int r, s, a, b;
	bool debug = false;
	std::string events = "";
};

int main(int argc, char** argv)
{
	FILE* f = argc > 1 ? fopen(argv[1], "w") : stdout;
	std::vector<Case> cases =
	{
		// arithmetic, wrap around, integer division
		{ "add r 5; mul r 3; return r s;", 2, 0, 0, 0 },
		{ "set r -7; div r 2; set s -7; mod s 2; return r s;", 0, 0, 0, 0 },
		{ "set r 0x7FFFFFFF; add r 1; set s -2147483648; sub s 1; return r s;", 0, 0, 0, 0 },
		{ "set r 0x7FFFFFFF; mul r 3; set s 65536; mul s 65536; return r s;", 0, 0, 0, 0 },
		{ "muldiv r 100 10000; set s 0x7FFFFFFF; muldiv s 50 100; return r s;", 100, 0, 0, 0 },
		{ "offsetmod r 100 1 100; set s -7; offsetmod s 1 0 5; return r s;", 0x7FFFFFFF, 0, 0, 0 },
		{ "aggregate r 4 5; offset s 4 5; return r s;", 1, 2, 0, 0 },
		{ "pow r 3; pow s -3; return r s;", 2, 2, 0, 0 },
		{ "pow r 20; set s -3; pow s 3; return r s;", 10, 0, 0, 0 },
		{ "sqrt r; sqrt s; return r s;", 15, -15, 0, 0 },
		{ "abs r; abs s; return r s;", -2147483648, -5, 0, 0 },
		{ "limit r 0 10; limit_upper s 3; return r s;", 50, 7, 0, 0 },
		{ "limit_lower r 3; limit s 10 0; return r s;", 1, 5, 0, 0 },
		{ "bit_count r; bit_not s; return r s;", 0b1011, 0x0F, 0, 0 },
		{ "bit_and r 0x0F; bit_or s 0xF0; return r s;", 0x3C, 0x0F, 0, 0 },
		{ "bit_xor r 0xFF; bit_count s; return r s;", 0x0F, -1, 0, 0 },
		{ "shl r 31; shr s 1; return r s;", 1, -8, 0, 0 },
		{ "shl r 33; shr s 35; return r s;", 1, -64, 0, 0 },
		{ "set r 0o10; set s -0x10; return r s;", 0, 0, 0, 0 },
		{ "set r 012; set s +5; return r s;", 0, 0, 0, 0 },
		{ "set r 0b102; return r s;", 0, 0, 0, 0 },
		{ "set r 99999999999; set s -0x80000000; return r s;", 0, 0, 0, 0 },
		{ "swap r s; return r s;", 1, 2, 0, 0 },
		{ "clear r; set s a; add s b; return r s;", 9, 9, 3, 4 },
		// colors and waves
		{ "get_color r r; get_shade s s; return r s;", 0x35, 0x35, 0, 0 },
		{ "set_color r 7; set_shade s 0x1A; return r s;", 0x35, 0x35, 0, 0 },
		{ "add_shade r 20; add_shade s 2; return r s;", 0x35, 0x35, 0, 0 },
		{ "add_shade r -5; add_shade s -10; return r s;", 0x35, 0x05, 0, 0 },
		{ "add_shade r -10; add_shade s 0; return r s;", 0x00, 0x00, 0, 0 },
		{ "wavegen_rect r 10 5 7; wavegen_rect s 10 5 7; return r s;", 13, 17, 0, 0 },
		{ "wavegen_saw r 10 8 5; wavegen_saw s 10 5 9; return r s;", -3, 17, 0, 0 },
		{ "wavegen_tri r 10 8 7; wavegen_tri s 10 8 2; return r s;", 13, 16, 0, 0 },
		{ "wavegen_sin r 100 100; wavegen_cos s 100 100; return r s;", 25, 50, 0, 0 },
		{ "wavegen_sin r 360 1000; wavegen_cos s 7 33; return r s;", 30, 3, 0, 0 },
		{ "wavegen_rect r 0 5 7; return r s;", 13, 0, 0, 0 },
		// runtime errors stop the script
		{ "set r 10; div r 0; set r 5; return r s;", 0, 0, 0, 0 },
		{ "set r 10; mod r a; set r 5; return r s;", 0, 0, 0, 0 },
		{ "muldiv r 1 0; set r 5; return r s;", 3, 0, 0, 0 },
		// blocks
		{ "if gt a 5; set r 1; else lt a 0; set r 2; else; set r 3; end; return r s;", 0, 0, 6, 0 },
		{ "if gt a 5; set r 1; else lt a 0; set r 2; else; set r 3; end; return r s;", 0, 0, -1, 0 },
		{ "if gt a 5; set r 1; else lt a 0; set r 2; else; set r 3; end; return r s;", 0, 0, 5, 0 },
		{ "if and gt a 1 lt a 10 neq a 5; set r 1; end; return r s;", 0, 0, 2, 0 },
		{ "if and gt a 1 lt a 10 neq a 5; set r 1; end; return r s;", 0, 0, 5, 0 },
		{ "if or eq a 1 eq a 3 ge a 10; set r 1; end; return r s;", 0, 0, 3, 0 },
		{ "if or eq a 1 eq a 3 ge a 10; set r 1; end; return r s;", 0, 0, 9, 0 },
		{ "if le a b; set r 1; end; if ge a b; set s 1; end; return r s;", 0, 0, 4, 4 },
		{ "if eq a 1; set r 1; else eq a 2; set r 2; end; return r s;", 0, 0, 2, 0 },
		{ "begin; var int x 6; set r x; end; begin; var int x 7; set s x; end; return r s;", 0, 0, 0, 0 },
		// loops
		{ "var int x 0; loop var i 10; add x i; end; set r x; return r s;", 0, 0, 0, 0 },
		{ "loop var i 10; if eq i 4; break; end; add r i; end; return r s;", 0, 0, 0, 0 },
		{ "loop var i 10; var int t i; mod t 2; if eq t 0; continue; end; add r i; end; return r s;", 0, 0, 0, 0 },
		{ "loop var i a; add r 1; end; return r s;", 0, 0, -5, 0 },
		{ "loop var i a; loop var j b; add r 1; end; end; return r s;", 0, 0, 3, 4 },
		// variables, consts, texts
		{ "var int x; var int y 5; set r x; set s y; return r s;", 9, 9, 0, 0 },
		{ "const int x 42; set r x; return r s;", 0, 0, 0, 0 },
		{ "const int x a; set r x; return r s;", 0, 0, 0, 0 },
		{ "var text t \"abc\"; if eq t \"abc\"; set r 1; end; if eq t \"ab\"; set s 1; end; return r s;", 0, 0, 0, 0 },
		{ "var text t; var text u \"\"; if eq t u; set r 1; end; return r s;", 0, 0, 0, 0 },
		{ "var text t \"a\\\"b\\\\\"; if eq t \"a\\\"b\\\\\"; set r 1; end; return r s;", 0, 0, 0, 0 },
		{ "var text t \"abc; return r s;", 0, 0, 0, 0 },
		{ "var text t \"a\\b\"; return r s;", 0, 0, 0, 0 },
		{ "if eq null null; set r 1; end; return r s;", 0, 0, 0, 0 },
		// return order
		{ "return s r;", 1, 2, 0, 0 },
		{ "var int x 7; return x r;", 1, 2, 0, 0 },
		{ "var int x 7; return x s;", 1, 1, 0, 0 },
		{ "return 3 4;", 1, 2, 0, 0 },
		{ "return r r;", 1, 2, 0, 0 },
		{ "return a r;", 1, 2, 0, 0 },
		{ "return r;", 1, 2, 0, 0 },
		// parse errors
		{ "add r 5;", 0, 0, 0, 0 },
		{ "if eq r 1; return r s;", 0, 0, 0, 0 },
		{ "add q 5; return r s;", 0, 0, 0, 0 },
		{ "add r 0x; return r s;", 0, 0, 0, 0 },
		{ "return r s; add r 1;", 0, 0, 0, 0 },
		{ "add r 1; var int x; return r s;", 0, 0, 0, 0 },
		{ "set a 1; return r s;", 0, 0, 0, 0 },
		{ "foo r; return r s;", 0, 0, 0, 0 },
		{ "r.foo 1; return r s;", 0, 0, 0, 0 },
		{ "x.foo 1; return r s;", 0, 0, 0, 0 },
		{ "end; return r s;", 0, 0, 0, 0 },
		{ "else; return r s;", 0, 0, 0, 0 },
		{ "break; return r s;", 0, 0, 0, 0 },
		{ "continue; return r s;", 0, 0, 0, 0 },
		{ "add r; return r s;", 0, 0, 0, 0 },
		{ "add r 1 2; return r s;", 0, 0, 0, 0 },
		{ "add r 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16; return r s;", 0, 0, 0, 0 },
		{ "add r 1 return r s;", 0, 0, 0, 0 },
		{ "lab: add r 1; return r s;", 0, 0, 0, 0 },
		{ "lab: var int x; return r s;", 0, 0, 0, 0 },
		{ "var int; return r s;", 0, 0, 0, 0 },
		{ "var foo x; return r s;", 0, 0, 0, 0 },
		{ "var int int; return r s;", 0, 0, 0, 0 },
		{ "var int add; return r s;", 0, 0, 0, 0 },
		{ "var int r; return r s;", 0, 0, 0, 0 },
		{ "var label x; return r s;", 0, 0, 0, 0 },
		{ "loop i 10; end; return r s;", 0, 0, 0, 0 },
		{ "loop var i; end; return r s;", 0, 0, 0, 0 },
		{ "loop var i r.foo.list; end; return r s;", 0, 0, 0, 0 },
		{ "if foo r 1; end; return r s;", 0, 0, 0, 0 },
		{ "if eq r; end; return r s;", 0, 0, 0, 0 },
		{ "if eq r \"a\"; end; return r s;", 0, 0, 0, 0 },
		{ "debug_assert r; return r s;", 0, 0, 0, 0 },
		{ "call r; return r s;", 0, 0, 0, 0 },
		{ "goto r; return r s;", 0, 0, 0, 0 },
		{ "exit; return r s;", 0, 0, 0, 0 },
		{ "begin r; end; return r s;", 0, 0, 0, 0 },
		{ "", 0, 0, 0, 0 },
		{ "   # only comment", 0, 0, 0, 0 },
		{ "# comment\nadd r 1; # other\n\tadd r 2;\nreturn r s; # end", 0, 0, 0, 0 },
		{ "add r 1;\nadd r x;\nreturn r s;", 0, 0, 0, 0 },
		{ "add r 1:2; return r s;", 0, 0, 0, 0 },
		{ "add r a.b; return r s;", 0, 0, 0, 0 },
		{ "add r 1a; return r s;", 0, 0, 0, 0 },
		{ "add r \"a\"b; return r s;", 0, 0, 0, 0 },
		// debug log
		{ "debug_log \"value\" a 5; debug_log; return r s;", 0, 0, 3, 0, true },
		{ "debug_log \"value\" a; return r s;", 0, 0, 3, 0, false },
		{ "debug_log r.x; return r s;", 0, 0, 3, 0, true },
		// events
		{ "add r 5; return r s;", 1, 3, 0, 0, false,
			"scripts:\n  test:\n    - offset: -1\n      code: 'mul r 10; add r s; return r s;'\n    - offset: 2\n      code: 'add r 1000; return r s;'\n    - offset: 1\n      code: 'mul r 2; return r s;'\n" },
		{ "add r 5; return r s;", 1, 3, 0, 0, false,
			"scripts:\n  test:\n    - new: A\n      offset: 1\n      code: 'add r 1; return r s;'\n    - update: A\n      offset: 2\n      code: 'add r 2; return r s;'\n    - new: B\n      offset: 3\n      code: 'mul r 3; return r s;'\n    - delete: B\n    - delete: C\n    - ignore: D\n      offset: 4\n      code: 'mul r 4; return r s;'\n" },
		{ "add r 5; return r s;", 1, 3, 0, 0, false,
			"scripts:\n  test:\n    - offset: 0\n      code: 'add r 1; return r s;'\n    - offset: 1\n      code: 'add r; return r s;'\n    - offset: 100\n      code: 'add r 3; return r s;'\n" },
	};

	for (auto& c : cases)
	{
		Options::debug = c.debug;
		g_log.clear();
		ScriptGlobal g;
		TestParser p(&g);
		g.pushParser("group", &p);
		fprintf(f, "case %s %d %d %d %d %d\n", escape(c.code).c_str(), c.r, c.s, c.a, c.b, c.debug ? 1 : 0);
		if (!c.events.empty())
		{
			fprintf(f, "events %s\n", escape(c.events).c_str());
			RawData raw((void*)c.events.data(), c.events.size(), +[](void*){});
			YAML::YamlRootNodeReader reader(raw, "events");
			g.load(reader);
		}
		TestParser::Container container;
		bool parsed = true;
		try
		{
			container.loadContainer("golden", c.code, p);
			parsed = (bool)container.data();
		}
		catch (Exception& e)
		{
			g_log.push_back(std::string("exception: ") + e.what());
			parsed = false;
		}
		g.endLoad();
		TestParser::Output arg{ c.r, c.s };
		TestParser::Worker worker{ c.a, c.b };
		worker.execute(container, arg);
		for (auto& l : g_log)
		{
			fprintf(f, "log %s\n", escape(l).c_str());
		}
		fprintf(f, "result %d %d %d\n", parsed ? 1 : 0, arg.getFirst(), arg.getSecond());
	}
	if (f != stdout) fclose(f);
	return 0;
}
