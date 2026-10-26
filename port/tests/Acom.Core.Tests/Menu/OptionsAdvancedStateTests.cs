namespace Acom.Tests.Menu;

// Options are global state.
[Collection(nameof(Acom.Tests.Engine.OptionsTests))]
public sealed class OptionsAdvancedStateTests
{
	public OptionsAdvancedStateTests()
	{
		Options.Create();
	}

	private static IEnumerable<OptionInfo> Shown(bool keys) =>
		Options.GetOptionInfo().Where(o => (o.Type == OptionType.OPTION_KEY) == keys && o.Description.Length != 0);

	[Fact]
	public void Settings_are_grouped_by_category_and_owner_in_registration_order()
	{
		var groups = OptionCategories.Build(Options.GetOptionInfo(), keys: false);
		var categories = new (string cat, List<OptionInfo>[] group)[]
		{
			("STR_GENERAL", groups.General), ("STR_GEOSCAPE", groups.Geo), ("STR_BASESCAPE", groups.Base),
			("STR_BATTLESCAPE", groups.Battle), ("STR_AI", groups.AI),
		};
		foreach (var (cat, group) in categories)
		{
			for (int owner = 0; owner < (int)OptionOwner.OPTION_OWNER_MAX; ++owner)
			{
				var expected = Shown(false).Where(o => o.Category == cat && (int)o.Owner == owner).Select(o => o.Id);
				Assert.Equal(expected, group[owner].Select(o => o.Id));
			}
		}
		Assert.All(groups.General.SelectMany(g => g), o => Assert.NotEqual(OptionType.OPTION_KEY, o.Type));
		Assert.Contains(groups.General[(int)OptionOwner.OPTION_OXC], o => o.Id == "FPS");
		Assert.Contains(groups.Battle[(int)OptionOwner.OPTION_OXCE], o => o.Id == "oxceNightVisionColor");
		Assert.Empty(groups.AI.SelectMany(g => g)); // no option uses STR_AI yet (also not in C++)
		Assert.Empty(groups.General[(int)OptionOwner.OPTION_OTHER]);
	}

	[Fact]
	public void Options_without_description_or_known_category_are_not_shown()
	{
		int v = 0;
		var infos = new[]
		{
			new OptionInfo(OptionOwner.OPTION_OXCE, "ai", () => v, x => v = x, 0, "STR_AI_OPT", "STR_AI"),
			new OptionInfo(OptionOwner.OPTION_OXCE, "hidden", () => v, x => v = x, 0, "STR_HIDDEN_OPT", "HIDDEN"),
			new OptionInfo(OptionOwner.OPTION_OXCE, "nodesc", () => v, x => v = x, 0, "", "STR_GENERAL"),
			new OptionInfo(OptionOwner.OPTION_OTHER, "base", () => v, x => v = x, 0, "STR_BASE_OPT", "STR_BASESCAPE"),
		};
		var groups = OptionCategories.Build(infos, keys: false);
		Assert.Equal("ai", Assert.Single(groups.AI[(int)OptionOwner.OPTION_OXCE]).Id);
		Assert.Equal("base", Assert.Single(groups.Base[(int)OptionOwner.OPTION_OTHER]).Id);
		Assert.Empty(groups.General.SelectMany(g => g));
		Assert.Empty(OptionCategories.Build(infos, keys: true).AI.SelectMany(g => g));
	}

	[Fact]
	public void Controls_take_only_keys_and_have_no_ai_group()
	{
		var groups = OptionCategories.Build(Options.GetOptionInfo(), keys: true);
		Assert.All(new[] { groups.General, groups.Geo, groups.Base, groups.Battle }.SelectMany(g => g).SelectMany(g => g),
			o => Assert.Equal(OptionType.OPTION_KEY, o.Type));
		Assert.Empty(groups.AI.SelectMany(g => g));
		int total = new[] { groups.General, groups.Geo, groups.Base, groups.Battle }.SelectMany(g => g).Sum(g => g.Count);
		Assert.Equal(Shown(true).Count(o => o.Category is "STR_GENERAL" or "STR_GEOSCAPE" or "STR_BASESCAPE" or "STR_BATTLESCAPE"), total);
	}

	[Theory]
	[InlineData("FPS", 60, true, 70)]
	[InlineData("FPS", 120, true, 0)] // wraps to min
	[InlineData("FPS", 0, false, 120)] // wraps to max
	[InlineData("FPSInactive", 10, false, 120)]
	[InlineData("oxceInterceptTableSize", 80, true, 8)]
	[InlineData("oxceInterceptTableSize", 8, true, 12)]
	[InlineData("oxceReactionFireThreshold", 0, true, 5)]
	[InlineData("oxceResearchScrollSpeedWithCtrl", 5, false, 50)]
	[InlineData("battleExplosionHeight", 3, true, 0)]
	[InlineData("autosaveSlots", 10, true, 1)]
	[InlineData("oxceShowETAMode", 2, true, 0)]
	[InlineData("someUnlistedInt", 0, true, 0)] // min = max = 0
	public void Integer_settings_step_and_wrap_like_cpp(string id, int value, bool left, int expected)
	{
		Assert.Equal(expected, OptionsAdvancedState.StepIntSetting(id, value, left, isTFTD: false));
	}

	[Theory]
	[InlineData(15, true, false, 1)]
	[InlineData(1, false, false, 15)]
	[InlineData(7, true, true, 9)] // TFTD skips 8
	[InlineData(11, false, true, 9)] // TFTD skips 10
	[InlineData(16, true, true, 2)]
	[InlineData(2, false, true, 16)]
	public void Night_vision_colour_skips_unusable_tftd_colours(int value, bool left, bool tftd, int expected)
	{
		Assert.Equal(expected, OptionsAdvancedState.StepIntSetting("oxceNightVisionColor", value, left, tftd));
	}
}
