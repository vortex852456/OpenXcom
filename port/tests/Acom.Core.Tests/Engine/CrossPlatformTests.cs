namespace Acom.Tests.Engine;

[Collection(nameof(OptionsTests))] // changes the global log file and reporting level
public sealed class CrossPlatformTests : IDisposable
{
	private readonly string _folder = Path.Combine(Path.GetTempPath(), "acom-cp-" + Guid.NewGuid().ToString("N"));

	public CrossPlatformTests() => Directory.CreateDirectory(_folder);

	public void Dispose() => Directory.Delete(_folder, true);

	[Fact]
	public void Log_buffers_until_the_file_is_set_then_filters_by_level()
	{
		var oldLevel = Logger.ReportingLevel;
		string oldFile = CrossPlatform.GetLogFileName();
		string log = Path.Combine(_folder, "openxcom.log");
		File.WriteAllText(log, "old run\n");
		try
		{
			Logger.ReportingLevel = SeverityLevel.LOG_UNCENSORED;
			CrossPlatform.Log(SeverityLevel.LOG_INFO, "buffered info");
			CrossPlatform.Log(SeverityLevel.LOG_VERBOSE, "buffered verbose");

			// like Options::init: set the level, then the file (which deletes the old log)
			Logger.ReportingLevel = SeverityLevel.LOG_INFO;
			CrossPlatform.SetLogFileName(log);
			Assert.False(File.Exists(log));
			CrossPlatform.Log(SeverityLevel.LOG_WARNING, "current");

			string text = File.ReadAllText(log);
			Assert.DoesNotContain("old run", text, StringComparison.Ordinal);
			Assert.Contains("]\t[INFO]\tbuffered info\n", text, StringComparison.Ordinal);
			Assert.DoesNotContain("buffered verbose", text, StringComparison.Ordinal);
			Assert.Matches(@"^\[\d\d-\d\d-\d{4}_\d\d-\d\d-\d\d\]\t\[WARN\]\tcurrent$", text.Split('\n')[^2]);
			Assert.True(text.IndexOf("buffered info", StringComparison.Ordinal) < text.IndexOf("current", StringComparison.Ordinal));
		}
		finally
		{
			CrossPlatform.SetLogFileName(oldFile);
			Logger.ReportingLevel = oldLevel;
		}
	}

	[Fact]
	public void Path_and_time_helpers()
	{
		Assert.Equal("a/b/", CrossPlatform.ConvertPath("a/b"));
		Assert.Equal("a/b/", CrossPlatform.ConvertPath("a/b/"));
		Assert.Equal("", CrossPlatform.ConvertPath(""));

		var local = new DateTime(1999, 5, 21, 14, 5, 59, DateTimeKind.Local);
		var (date, time) = CrossPlatform.TimeToString(new DateTimeOffset(local).ToUnixTimeSeconds());
		Assert.Equal("1999-05-21", date);
		Assert.Equal("14:05", time);

		Assert.Equal(0, CrossPlatform.GetDateModified(Path.Combine(_folder, "missing")));
	}

	[Fact]
	public void Binary_files_round_trip()
	{
		string file = Path.Combine(_folder, "data.bin");
		byte[] data = [0, 1, 0xFF, (byte)'\n', 0x80];
		Assert.True(CrossPlatform.WriteFile(file, data.AsSpan()));
		Assert.Equal(data, CrossPlatform.ReadFileRaw(file));
		using var stream = CrossPlatform.ReadFile(file);
		Assert.Equal(data.Length, stream.Length);
		Assert.Throws<IOException>(() => CrossPlatform.ReadFileRaw(Path.Combine(_folder, "missing")));
		Assert.False(CrossPlatform.WriteFile(Path.Combine(_folder, "no", "such", "dir"), data.AsSpan()));
	}

	[Fact]
	public void Palette_files_are_written_through_CrossPlatform()
	{
		var palette = new Palette();
		palette.LoadDat(new MemoryStream([1, 2, 3, 63, 0, 10]), 2);
		string pal = Path.Combine(_folder, "p.pal");
		palette.SavePal(pal);
		using (var expected = new MemoryStream())
		{
			palette.SavePal(expected);
			Assert.Equal(expected.ToArray(), File.ReadAllBytes(pal));
		}
		string jasc = Path.Combine(_folder, "p.jasc.pal");
		palette.SavePalJasc(jasc);
		Assert.Equal("JASC-PAL\n0100\n256\n4 8 12\n252 0 40\n", File.ReadAllText(jasc));
		string mod = Path.Combine(_folder, "p.rul");
		palette.SavePalMod(mod, "PAL_X", "PAL_Y");
		Assert.StartsWith("customPalettes:\n  - type: PAL_X\n    target: PAL_Y\n    palette:\n      0: [4,8,12]\n", File.ReadAllText(mod), StringComparison.Ordinal);
	}
}
