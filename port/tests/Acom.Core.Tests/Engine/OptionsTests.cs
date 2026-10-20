using Acom.Engine;

namespace Acom.Tests.Engine;

// Options are global state: run these tests one after another.
[Collection(nameof(OptionsTests))]
public sealed class OptionsTests : IDisposable
{
	private readonly string _dir = Directory.CreateTempSubdirectory("acom-options-").FullName + "/";

	public OptionsTests()
	{
		Options.SetFoldersForTest(_dir, _dir, _dir);
		CrossPlatform.SetArgs([]);
		Options.Create();
		Options.ResetDefault(includeMods: false);
	}

	public void Dispose()
	{
		CrossPlatform.SetArgs([]);
		Directory.Delete(_dir, recursive: true);
	}

	[Fact]
	public void Create_registers_every_option_once()
	{
		var ids = Options.GetOptionInfo().Select(o => o.Id).ToList();
		Assert.Equal(338, ids.Count);
		Assert.Equal(ids.Count, ids.Distinct().Count());
	}

	[Fact]
	public void Defaults_match_cpp_create()
	{
		Assert.Equal(640, Options.displayWidth);
		Assert.Equal(400, Options.displayHeight);
		Assert.Equal(84, Options.soundVolume); // 2*(MIX_MAX_VOLUME/3)
		Assert.Equal(SDLKey.SDLK_RETURN, Options.keyOk);
		Assert.Equal(KeyboardType.KEYBOARD_ON, Options.keyboardMode);
		Assert.Equal(ScrollType.SCROLL_AUTO, Options.battleEdgeScroll);
		Assert.Equal(SDLMouse.SDL_BUTTON_MIDDLE, Options.geoDragScrollButton);
		Assert.Equal(SDL_GrabMode.SDL_GRAB_OFF, Options.captureMouse);
		Assert.Equal((int)SeverityLevel.LOG_WARNING, Options.oxceModValidationLevel);
		Assert.False(Options.oxceUFONeverDestroyed);
	}

	[Fact]
	public void Save_and_load_round_trip_and_keep_unknown_options()
	{
		File.WriteAllText(_dir + "options.cfg", "options:\n  someOtherForkOption: 7\n");
		Options.displayWidth = 1280;
		Options.language = "de";
		Options.keyCancel = SDLKey.SDLK_q;
		Options.mods = [new ModToggle("xcom1", true), new ModToggle("someMod", false)];
		Assert.True(Options.Save());

		string text = File.ReadAllText(_dir + "options.cfg");
		Assert.Contains("someOtherForkOption: 7", text, StringComparison.Ordinal);
		Assert.Contains("displayWidth: 1280", text, StringComparison.Ordinal);

		Options.ResetDefault(includeMods: false);
		Assert.Equal(640, Options.displayWidth);
		Assert.True(Options.Load());
		Assert.Equal(1280, Options.displayWidth);
		Assert.Equal("de", Options.language);
		Assert.Equal(SDLKey.SDLK_q, Options.keyCancel);
		Assert.Equal(["xcom1", "someMod"], Options.mods.Select(m => m.Id));
		Assert.False(Options.mods[1].Active);
	}

	[Fact]
	public void Missing_entries_reset_to_default_and_modifier_keys_are_rejected()
	{
		Options.displayWidth = 999;
		File.WriteAllText(_dir + "options.cfg", "options:\n  keyOk: 304\nmods:\n  - id: xcom1\n    active: true\n");
		Assert.True(Options.Load());
		Assert.Equal(640, Options.displayWidth);
		Assert.Equal(SDLKey.SDLK_UNKNOWN, Options.keyOk); // left shift can't be bound
	}

	[Fact]
	public void Command_line_overrides_config()
	{
		File.WriteAllText(_dir + "options.cfg", "options:\n  displayWidth: 800\n  fullscreen: false\n");
		CrossPlatform.SetArgs(["-displayWidth", "1024x", "--FullScreen", "true", "-continue", "-language", "fr"]);
		var load = typeof(Options).GetMethod("LoadArgs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
		load.Invoke(null, null);
		Options.UpdateOptions();
		Assert.Equal(1024, Options.displayWidth); // stringstream reads the leading number
		Assert.True(Options.fullscreen);
		Assert.Equal("fr", Options.language);
		Assert.True(Options.GetLoadLastSave());
	}

	[Fact]
	public void Display_settings_can_be_switched_back()
	{
		Options.displayWidth = 1920;
		Options.BackupDisplay();
		Options.newDisplayWidth = 800;
		Options.SwitchDisplay();
		Assert.Equal(800, Options.displayWidth);
		Options.SwitchDisplay();
		Assert.Equal(1920, Options.displayWidth);
	}
}

[Collection(nameof(OptionsTests))]
public sealed class OptionsGoldenTests : IDisposable
{
	private readonly string _dir = Directory.CreateTempSubdirectory("acom-options-golden-").FullName + "/";

	public void Dispose() => Directory.Delete(_dir, recursive: true);

	/// <summary>
	/// Golden/options-defaults.cfg was written by the original C++ game on first start (Linux build).
	/// Every option and every default value must match.
	/// </summary>
	[Fact]
	public void Default_options_match_cpp_options_cfg()
	{
		Options.SetFoldersForTest(_dir, _dir, _dir);
		Options.Create();
		Options.ResetDefault(includeMods: false);
		Options.mods = [];
		Assert.True(Options.Save(reset: true));

		var expected = new YamlRootNodeReader(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Golden", "options-defaults.cfg")), "cpp")["options"];
		var actual = YamlRootNodeReader.FromFile(_dir + "options.cfg")["options"];
		var expectedMap = expected.Children().ToDictionary(c => c.Key, c => c.Val);
		var actualMap = actual.Children().ToDictionary(c => c.Key, c => c.Val);
		Assert.Equal(expectedMap.Keys.Order(StringComparer.Ordinal), actualMap.Keys.Order(StringComparer.Ordinal));
		var differences = expectedMap.Where(e => actualMap[e.Key] != e.Value).Select(e => $"{e.Key}: C++ '{e.Value}' vs C# '{actualMap[e.Key]}'").ToList();
		Assert.Empty(differences);
		// same order as the C++ save (sorted by id)
		Assert.Equal(expected.Children().Select(c => c.Key), actual.Children().Select(c => c.Key));
	}
}
