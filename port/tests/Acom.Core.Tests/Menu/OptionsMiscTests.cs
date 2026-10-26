namespace Acom.Tests.Menu;

public sealed class OptionsMiscTests
{
	[Theory]
	[InlineData("crt-hyllian", "Crt Hyllian")]
	[InlineData("4xBR_hybrid", "4xBR Hybrid")]
	[InlineData("a", "A")]
	[InlineData("", "")]
	public void Video_ucwords_replaces_separators_and_capitalizes(string input, string expected)
	{
		Assert.Equal(expected, OptionsVideoState.UcWords(input));
	}

	[Theory]
	[InlineData("left shift", "Left Shift")]
	[InlineData("[+]", "[+]")]
	[InlineData("page down", "Page Down")]
	[InlineData("a ", "A ")]
	public void Controls_ucwords_capitalizes_after_spaces(string input, string expected)
	{
		Assert.Equal(expected, OptionsControlsState.UcWords(input));
	}

	[Theory]
	[InlineData(SDLKey.SDLK_a, "a")]
	[InlineData(SDLKey.SDLK_RETURN, "return")]
	[InlineData(SDLKey.SDLK_SEMICOLON, ";")]
	[InlineData(SDLKey.SDLK_KP_PLUS, "[+]")]
	[InlineData(SDLKey.SDLK_LSHIFT, "left shift")]
	[InlineData(SDLKey.SDLK_F12, "f12")]
	[InlineData(SDLKey.SDLK_UNKNOWN, "unknown key")]
	public void Key_names_are_those_of_sdl12(SDLKey key, string expected)
	{
		Assert.Equal(expected, SDLKeyNames.SDL_GetKeyName(key));
	}

	[Theory]
	[InlineData("1024", 1024)]
	[InlineData("", 0)]
	[InlineData("  -12x", -12)]
	[InlineData("abc", 0)]
	[InlineData("99999999999", int.MaxValue)]
	public void Typed_numbers_parse_like_a_stringstream(string text, int expected)
	{
		Assert.Equal(expected, OptionsVideoState.ParseLeadingInt(text));
	}

	[Theory]
	[InlineData(3, 10, 3)]
	[InlineData(12, 10, 9)]
	[InlineData(-1, 10, 9)] // size_t: negative values are huge
	[InlineData(5, 0, 5)] // size - 1 wraps around
	public void Battle_config_indexes_are_clamped_like_size_t(int value, int count, int expected)
	{
		Assert.Equal(expected, NewBattleState.ClampIndex(value, count));
	}

	[Fact]
	public void Data_folder_is_the_path_before_the_file()
	{
		Assert.Equal("/games/ufo/", OptionsFoldersState.DataFolderOf("/games/ufo/GEODATA/BACKPALS.DAT", "GEODATA/BACKPALS.DAT"));
		Assert.Equal("/x/openxcom.png", OptionsFoldersState.DataFolderOf("/x/openxcom.png", "nope"));
	}

	[Fact]
	public void Jasc_colour_lines_read_like_stream_extraction()
	{
		int r = 1, g = 2, b = 3;
		TestState.ReadRgb("252 248 4", ref r, ref g, ref b);
		Assert.Equal((252, 248, 4), (r, g, b));
		TestState.ReadRgb("7 x 9", ref r, ref g, ref b);
		Assert.Equal((7, 0, 4), (r, g, b)); // failed read stores 0, later reads keep their value
		TestState.ReadRgb(null, ref r, ref g, ref b);
		Assert.Equal((0, 0, 4), (r, g, b));
	}
}
