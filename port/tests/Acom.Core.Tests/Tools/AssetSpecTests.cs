using System.Text;
using System.Text.Json;
using Acom.AssetSpec;

namespace Acom.Tests.Tools;

public sealed class AssetSpecTests : IDisposable
{
	private const string SecretText = "ORIGINAL-TEXT-MUST-NOT-LEAK";
	private readonly string _data = Directory.CreateTempSubdirectory("acom-spec-").FullName;

	public void Dispose() => Directory.Delete(_data, recursive: true);

	private void Write(string path, byte[] data)
	{
		var full = Path.Combine(_data, path);
		Directory.CreateDirectory(Path.GetDirectoryName(full)!);
		File.WriteAllBytes(full, data);
	}

	/// <summary>PCK with <paramref name="frames"/> frames, each one pixel of <paramref name="color"/> at (1,0).</summary>
	private void WritePck(string path, int frames, byte color)
	{
		var pck = new List<byte>();
		var tab = new List<byte>();
		for (int i = 0; i < frames; ++i)
		{
			tab.AddRange(BitConverter.GetBytes((ushort)pck.Count));
			pck.AddRange([0, 254, 1, color, 255]);
		}
		Write(path + ".PCK", [.. pck]);
		Write(path + ".TAB", [.. tab]);
	}

	private static string RulesDir()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir != null && !File.Exists(Path.Combine(dir.FullName, "port", "Acom.slnx")))
			dir = dir.Parent;
		return Path.Combine(dir!.FullName, "bin", "standard", "xcom1");
	}

	private AssetSpecification Run()
	{
		var spec = new AssetSpecification { Game = "ufo", Rules = "bin/standard/xcom1", HdScale = 4, Files = new DataScanner(_data, 4).ScanAll() };
		Merger.Apply(spec, RulesetIndex.Load(RulesDir()));
		return spec;
	}

	[Fact]
	public void Frames_are_described_and_linked_to_rulesets()
	{
		WritePck("units/bigobs", 3, 0x43); // lower case like a Linux copy
		var spec = Run();
		var bigobs = Assert.Single(spec.Files, f => f.Path == "UNITS/BIGOBS.PCK");
		Assert.Equal((32, 48, 128, 192), (bigobs.Width, bigobs.Height, bigobs.HdWidth, bigobs.HdHeight));
		Assert.Equal(3, bigobs.Frames);
		var frame = bigobs.FrameDetails![0];
		Assert.Equal([1, 0, 1, 1], frame.Bounds!);
		Assert.Equal([4], frame.ColorGroups);
		Assert.Contains(frame.UsedBy, u => u.EndsWith(": bigSprite", StringComparison.Ordinal));
		Assert.Contains(bigobs.Notes, n => n.Contains("fehlen", StringComparison.Ordinal)); // the rulesets reference more frames
	}

	[Fact]
	public void Sound_archive_is_mapped_to_logical_set()
	{
		// GEO.CAT (Windows version): 11 entries with name chunk + WAV header
		var entries = Enumerable.Range(0, 11).Select(_ => Wav(11025, 2205)).ToList();
		Write("SOUND/SAMPLE.CAT", Cat(entries));
		var spec = Run();
		var cat = Assert.Single(spec.Files, f => f.Path == "SOUND/SAMPLE.CAT");
		Assert.Equal(11, cat.SoundDetails!.Count);
		Assert.Equal(("WAV", 11025, 0.2), (cat.SoundDetails[0].Format, cat.SoundDetails[0].SampleRate, cat.SoundDetails[0].Seconds));
		Assert.Contains(cat.SoundDetails.SelectMany(s => s.UsedBy), u => u.Contains("(GEO.CAT #", StringComparison.Ordinal));
	}

	[Fact]
	public void Output_contains_no_texts_or_absolute_paths()
	{
		Write("GEODATA/ENGLISH.DAT", Encoding.ASCII.GetBytes(SecretText));
		Write("GEOGRAPH/BACK01.SCR", new byte[64000]);
		var spec = Run();
		string json = JsonSerializer.Serialize(spec, Merger.JsonOptions);
		string md = MarkdownReport.Render(spec);
		foreach (var output in new[] { json, md })
		{
			Assert.DoesNotContain(SecretText, output, StringComparison.Ordinal);
			Assert.DoesNotContain(_data, output, StringComparison.Ordinal);
		}
		Assert.Equal("text", Assert.Single(spec.Files, f => f.Path == "GEODATA/ENGLISH.DAT").Category);
	}

	private static byte[] Wav(int rate, int samples)
	{
		var w = new List<byte>();
		w.AddRange("RIFF"u8.ToArray());
		w.AddRange(BitConverter.GetBytes(36 + samples));
		w.AddRange("WAVEfmt "u8.ToArray());
		w.AddRange(BitConverter.GetBytes(16));
		w.AddRange(BitConverter.GetBytes((short)1));
		w.AddRange(BitConverter.GetBytes((short)1));
		w.AddRange(BitConverter.GetBytes(rate));
		w.AddRange(BitConverter.GetBytes(rate));
		w.AddRange(BitConverter.GetBytes((short)1));
		w.AddRange(BitConverter.GetBytes((short)8));
		w.AddRange("data"u8.ToArray());
		w.AddRange(BitConverter.GetBytes(samples));
		w.AddRange(new byte[samples]);
		return [.. w];
	}

	private static byte[] Cat(List<byte[]> items)
	{
		var header = new List<byte>();
		var body = new List<byte>();
		foreach (var item in items)
		{
			byte[] entry = [3, (byte)'S', (byte)'N', (byte)'D', .. item];
			header.AddRange(BitConverter.GetBytes(items.Count * 8 + body.Count));
			header.AddRange(BitConverter.GetBytes(entry.Length));
			body.AddRange(entry);
		}
		return [.. header, .. body];
	}
}
