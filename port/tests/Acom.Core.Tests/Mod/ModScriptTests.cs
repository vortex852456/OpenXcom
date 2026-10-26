using System.IO.Compression;
using System.Text;
using GameMod = Acom.Mod;

namespace Acom.Tests.Mod;

/// <summary>
/// ModScript: all parsers with all bindings of the game. The metadata dump (`debug` + `verboseLogging`) of every parser
/// is compared with the dump of the C++ game (Golden/script-metadata.txt.gz), which lists every type, register, constant
/// and every function with its argument types, in the same order.
/// </summary>
[Collection("OptionsTests")] // changes Options.debug, Options.verboseLogging and Logger.Sink
public sealed class ModScriptTests
{
	private static List<string> CaptureMetadata()
	{
		var oldSink = Logger.Sink;
		var oldDebug = Options.debug;
		var oldVerbose = Options.verboseLogging;
		var oldLevel = Logger.ReportingLevel;
		var log = new List<string>();
		try
		{
			Options.debug = true;
			Options.verboseLogging = true;
			Logger.ReportingLevel = SeverityLevel.LOG_VERBOSE;
			ScriptParserBase.ResetPrintOp();
			Logger.Sink = (level, message) =>
			{
				if (level == SeverityLevel.LOG_DEBUG)
				{
					lock (log) log.Add(message);
				}
			};
			var mod = new GameMod(createConverter: false);
			_ = mod.ScriptParsers;
		}
		finally
		{
			Logger.Sink = oldSink;
			Options.debug = oldDebug;
			Options.verboseLogging = oldVerbose;
			Logger.ReportingLevel = oldLevel;
		}
		var lines = new List<string>();
		foreach (var m in log)
		{
			var parts = m.Split('\n');
			lines.Add("@@ " + parts[0]);
			lines.AddRange(parts.Skip(1));
		}
		return lines;
	}

	private static List<string> ReadGolden()
	{
		using var file = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Golden", "script-metadata.txt.gz"));
		using var gz = new GZipStream(file, CompressionMode.Decompress);
		using var reader = new StreamReader(gz, Encoding.UTF8);
		var lines = new List<string>();
		string? line;
		while ((line = reader.ReadLine()) != null)
		{
			lines.Add(line);
		}
		return lines;
	}

	[Fact]
	public void Metadata_of_all_parsers_matches_cpp()
	{
		var expected = ReadGolden();
		var actual = CaptureMetadata();
		// the C++ log record ends with "\n", the log file adds the line break of the next record
		while (actual.Count > 0 && actual[^1].Length == 0) actual.RemoveAt(actual.Count - 1);
		while (expected.Count > 0 && expected[^1].Length == 0) expected.RemoveAt(expected.Count - 1);

		var diffs = new List<string>();
		int n = Math.Max(expected.Count, actual.Count);
		for (int i = 0, j = 0; i < expected.Count || j < actual.Count;)
		{
			var e = i < expected.Count ? expected[i] : "<end>";
			var a = j < actual.Count ? actual[j] : "<end>";
			if (e != a)
			{
				diffs.Add($"line {i + 1}:\n  C++: {e}\n  C#:  {a}");
				if (diffs.Count > 30) break;
			}
			++i;
			++j;
		}
		Assert.True(diffs.Count == 0, $"{diffs.Count} differences (C++ {expected.Count} lines, C# {actual.Count} lines)\n" + string.Join("\n", diffs));
	}

	[Fact]
	public void Default_scripts_compile_without_errors()
	{
		var oldSink = Logger.Sink;
		var errors = new List<string>();
		try
		{
			Logger.Sink = (level, message) => { if (level <= SeverityLevel.LOG_WARNING) lock (errors) errors.Add(message); };
			var mod = new GameMod(createConverter: false);
			var parsers = mod.ScriptParsers;
			var empty = new YamlRootNodeReader("{}", "test").ToBase();
			foreach (var group in new ScriptGroup[] { parsers.battleUnitScripts, parsers.battleItemScripts, parsers.bonusStatsScripts, parsers.skillScripts, parsers.countryScripts, parsers.ufoScripts, parsers.craftScripts, parsers.soldierBonusScripts })
			{
				var c = group.CreateContainer();
				c.Load("test", empty, group);
			}
		}
		finally
		{
			Logger.Sink = oldSink;
		}
		Assert.Empty(errors);
	}
}
