namespace Acom.Tests.Geoscape;

/// <summary>Graphs screen: scale fitting and the toggle round trip through the saved game.</summary>
[Collection(nameof(Acom.Tests.Engine.GameTests))]
public sealed class GraphsStateTests : IDisposable
{
	private const string Rules = """
		interfaces:
		  - type: graphs
		    palette: PAL_GEOSCAPE
		    elements:
		      - { id: button, color: 1 }
		      - { id: text, color: 2 }
		      - { id: scale, color: 3 }
		      - { id: graph, color: 4 }
		      - { id: regionTotal, color: 5, color2: 6 }
		      - { id: countryTotal, color: 7, color2: 8 }
		""";

	private readonly GeoscapeTestGame _env = new(Rules);

	public GraphsStateTests()
	{
		_env.Mod._surfaces["GRAPHS.SPK"] = new Surface(320, 200);
		_env.Save.Countries[0].Funding.Add(100000);
	}

	public void Dispose() => _env.Dispose();

	[Theory]
	[InlineData(10, 0, 0, 0, 90)]
	[InlineData(10, 0, 91, 0, 180)]
	[InlineData(10, -5, 50, -10, 80)]
	[InlineData(10, -95, 0, -100, 80)] // range 95 doubles check to 20, then shifts by 20 until low is covered
	[InlineData(250, 0, 3000, 0, 4500)]
	public void Scale_fits_the_grid(int check, int lower, int upper, int expectedLower, int expectedUpper)
	{
		GraphsState.FitScale(check, ref lower, ref upper);
		Assert.Equal((expectedLower, expectedUpper), (lower, upper));
	}

	[Fact]
	public void Toggles_are_loaded_and_stored_in_the_saved_game()
	{
		_env.Save.GraphRegionToggles = "1";
		_env.Save.GraphFinanceToggles = "01";
		var state = new GraphsState();
		state.BtnFinanceClick(null);
		state.BtnIncomeClick(null);
		state.BtnZoomInClick(null);
		state.Destroy();
		// missing toggles are padded with '0' (regions: STR_TEST_REGION + total)
		Assert.Equal("10", _env.Save.GraphRegionToggles);
		Assert.Equal("00", _env.Save.GraphCountryToggles);
		Assert.Equal("01000", _env.Save.GraphFinanceToggles);
	}
}
