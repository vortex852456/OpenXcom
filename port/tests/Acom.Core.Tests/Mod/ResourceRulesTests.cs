#pragma warning disable CA1716 // namespace mirrors the src/Mod area like the other test folders
namespace Acom.Tests.Mod;

/// <summary>Small resource rules: SoundDefinition, RuleMusic, RuleVideo, CustomPalettes, ExtraSprites, ExtraSounds, RuleConverter.</summary>
public class ResourceRulesTests
{
	private static YamlNodeReader Node(string yaml) => new YamlRootNodeReader(yaml, "test").ToBase();

	private static IReadOnlyList<YamlNodeReader> Xcom1(string file, string section)
	{
		var path = Path.Combine(Engine.LanguageTests.RepoRoot(), "bin", "standard", "xcom1", file);
		return new YamlRootNodeReader(File.ReadAllText(path), path)[section].Children();
	}

	[Fact]
	public void SoundDefinition_expands_ranges_before_single_sounds()
	{
		var def = new SoundDefinition("TEST");
		def.Load(Node("soundRanges: [[3, 5], [9, 9]]\nsounds: [1, 2]\nfile: TEST.CAT"));
		Assert.Equal([3, 4, 5, 9, 1, 2], def.SoundList);
		Assert.Equal("TEST.CAT", def.CATFile);
	}

	[Fact]
	public void RuleMusic_defaults_and_xcom1_tracks()
	{
		var music = new RuleMusic("GMTEST");
		Assert.Equal("GMTEST", music.Name);
		Assert.Equal(int.MaxValue, music.CatPos);
		Assert.Equal(0.76f, music.Normalization);

		var first = Xcom1("music.rul", "musics")[0];
		var track = new RuleMusic(first["type"].ReadVal<string>());
		track.Load(first);
		Assert.Equal("GMDEFEND", track.Name);
		Assert.Equal(3, track.CatPos);
	}

	[Fact]
	public void RuleVideo_reads_xcom1_cutscenes()
	{
		var videos = Xcom1("cutscenes.rul", "cutscenes").ToDictionary(n => n["type"].ReadVal<string>(), n => n, StringComparer.Ordinal);

		var intro = new RuleVideo("intro");
		intro.Load(videos["intro"]);
		Assert.True(intro.UseUfoAudioSequence());
		Assert.Equal(["UFOINTRO/UFOINT.FLI"], intro.Videos);
		Assert.False(intro.WinGame);

		var win = new RuleVideo("winGame");
		win.Load(videos["winGame"]);
		Assert.True(win.WinGame);
		Assert.Equal("GMWIN", win.SlideshowHeader.musicId);
		Assert.Equal(30, win.SlideshowHeader.transitionSeconds);
		var slide = win.Slides[1];
		Assert.Equal(("UFOINTRO/PICT2.LBM", "STR_VICTORY_2"), (slide.imagePath, slide.caption));
		Assert.Equal((88, 136, 232, 64, 249), (slide.x, slide.y, slide.w, slide.h, slide.color));
		Assert.Equal(TextHAlign.ALIGN_LEFT, slide.align);

		Assert.True(new RuleVideo("loseGame").LoseGame); // backwards-compatibility by name
		var defaults = new RuleVideo("x");
		defaults.Load(Node("slideshow:\n  slides:\n    - {}"));
		Assert.Equal(30, defaults.SlideshowHeader.transitionSeconds);
		Assert.Equal((Screen.ORIGINAL_WIDTH, Screen.ORIGINAL_HEIGHT, int.MaxValue), (defaults.Slides[0].w, defaults.Slides[0].h, defaults.Slides[0].color));
	}

	[Fact]
	public void CustomPalettes_reads_colors_by_index()
	{
		var palette = new CustomPalettes("PAL_TEST");
		palette.Load(Node("target: PAL_GEOSCAPE\nfile: Palettes/test.pal\npalette:\n  5: [1, 2, 3]\n  1: [4, 5, 6]"));
		Assert.Equal("PAL_GEOSCAPE", palette.Target);
		Assert.Equal("Palettes/test.pal", palette.File);
		Assert.Equal([1, 5], palette.Palette.Keys);
		Assert.Equal(new Position(1, 2, 3), palette.Palette[5]);
	}

	[Fact]
	public void ExtraSprites_reads_xcom1_definitions()
	{
		var defs = Xcom1("extraSprites.rul", "extraSprites");
		var touch = new ExtraSprites();
		touch.Load(defs[0], null!);
		Assert.Equal("Touch", touch.Type);
		Assert.Equal((64, 120, 32, 24), (touch.Width, touch.Height, touch.SubX, touch.SubY));
		Assert.False(touch.SingleImage);
		Assert.Equal("Resources/Extended/Touch/Touch_UFOEU.png", touch.Sprites[0]);

		var single = new ExtraSprites();
		single.Load(Node("typeSingle: BACK.SCR\nfileSingle: back.png"), null!);
		Assert.Equal("BACK.SCR", single.Type);
		Assert.True(single.SingleImage);
		Assert.Equal("back.png", single.Sprites[0]);
		Assert.Equal((320, 200), (single.Width, single.Height));

		Assert.True(ExtraSprites.IsImageFile("a/b.Png"));
		Assert.False(ExtraSprites.IsImageFile("a/b.txt"));
	}

	[Fact]
	public void ExtraSounds_reads_files_by_index()
	{
		var sounds = new ExtraSounds();
		var owner = new ModData { name = "test" };
		sounds.Load(Node("type: BATTLE.CAT\nfiles:\n  10: a.ogg\n  2: dir/"), owner);
		Assert.Equal("BATTLE.CAT", sounds.Type);
		Assert.Equal([2, 10], sounds.Sounds.Keys);
		Assert.Same(owner, sounds.ModOwner);
	}

	[Fact]
	public void Folder_contents_load_in_natural_order()
	{
		List<string> files = ["10.png", "2.png", "B.png", "a.png", "1.png", "a01.png", "a1.png"];
		files.Sort(Unicode.NaturalComparison);
		Assert.Equal(["1.png", "2.png", "10.png", "a.png", "a01.png", "a1.png", "B.png"], files);
	}

	[Fact]
	public void RuleConverter_reads_xcom1_ids()
	{
		var path = Path.Combine(Engine.LanguageTests.RepoRoot(), "bin", "standard", "xcom1", "converter.rul");
		var converter = new RuleConverter();
		converter.Load(new YamlRootNodeReader(File.ReadAllText(path), path)["converter"]);
		Assert.Equal(0x16, converter.GetOffset("BASE.DAT_FACILITIES"));
		Assert.Equal(0, converter.GetOffset("MISSING"));
		Assert.NotEmpty(converter.Items);
		Assert.NotEmpty(converter.Ufopaedia);
	}
}
