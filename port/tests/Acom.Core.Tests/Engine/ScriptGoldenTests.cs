using System.Text;

namespace Acom.Tests.Engine;

/// <summary>
/// Script engine against the compiled C++ Script.cpp (Golden/script.txt from tools/golden/script.cpp):
/// same parse result, same outputs and the same log lines for every case.
/// </summary>
[Collection(nameof(OptionsTests))] // swaps Logger.Sink and Options.debug
public sealed class ScriptGoldenTests
{
	private static string Unescape(string s)
	{
		if (s == "\\e")
		{
			return "";
		}
		var sb = new StringBuilder();
		for (int i = 0; i < s.Length; ++i)
		{
			if (s[i] == '\\' && i + 1 < s.Length)
			{
				++i;
				sb.Append(s[i] switch { 'n' => '\n', 't' => '\t', 's' => ' ', _ => s[i] });
			}
			else
			{
				sb.Append(s[i]);
			}
		}
		return sb.ToString();
	}

	private sealed record Case(string code, int r, int s, int a, int b, bool debug, string? events, List<string> log, bool parsed, int outR, int outS);

	private static List<Case> ReadCases()
	{
		var cases = new List<Case>();
		Case? curr = null;
		foreach (var line in Golden.ReadLines("script.txt"))
		{
			var f = Golden.Fields(line);
			switch (f[0])
			{
				case "case":
					curr = new Case(Unescape(f[1]), int.Parse(f[2]), int.Parse(f[3]), int.Parse(f[4]), int.Parse(f[5]), f[6] == "1", null, [], false, 0, 0);
					break;
				case "events":
					curr = curr! with { events = Unescape(f[1]) };
					break;
				case "log":
					curr!.log.Add(Unescape(f[1]));
					break;
				case "result":
					cases.Add(curr! with { parsed = f[1] == "1", outR = int.Parse(f[2]), outS = int.Parse(f[3]) });
					curr = null;
					break;
			}
		}
		return cases;
	}

	[Fact]
	public void MatchesCpp()
	{
		var cases = ReadCases();
		Assert.True(cases.Count > 100);
		var oldSink = Logger.Sink;
		var oldDebug = Options.debug;
		var log = new List<string>();
		try
		{
			Logger.Sink = (level, message) => { lock (log) log.Add(message); };
			foreach (var c in cases)
			{
				Options.debug = c.debug;
				log.Clear();
				var g = new ScriptGlobal();
				var p = new ScriptParserEvents(g, "test", [new("r", ScriptParam.IntRef), new("s", ScriptParam.IntRef)], [new("a", ScriptParam.Int), new("b", ScriptParam.Int)]);
				g.PushParser("group", p);
				if (c.events != null)
				{
					g.Load(new YamlRootNodeReader(c.events, "events").ToBase());
				}
				var container = new ScriptContainerEvents();
				bool parsed;
				try
				{
					container.LoadContainer("golden", c.code, p);
					parsed = container.Data != null;
				}
				catch (InvalidOperationException e)
				{
					log.Add("exception: " + e.Message);
					parsed = false;
				}
				g.EndLoad();
				var arg = new ScriptOutputArgs(c.r, c.s);
				new ScriptWorker(p.Signature, c.a, c.b).Execute(container, arg);

				var id = c.code.Replace("\n", "\\n", StringComparison.Ordinal);
				Assert.True(c.log.SequenceEqual(log), id + "\nC++:\n" + string.Join("\n", c.log) + "\nC#:\n" + string.Join("\n", log));
				Assert.True(c.parsed == parsed, id);
				Assert.True((c.outR, c.outS) == (arg.GetFirst(), arg.GetSecond()), id + $" C++ {c.outR} {c.outS}, C# {arg.GetFirst()} {arg.GetSecond()}");
			}
		}
		finally
		{
			Logger.Sink = oldSink;
			Options.debug = oldDebug;
		}
	}
}
