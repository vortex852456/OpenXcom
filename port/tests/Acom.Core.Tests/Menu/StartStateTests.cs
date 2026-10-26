namespace Acom.Tests.Menu;

// Changes Options, FileMap and the static game pointer.
[Collection(nameof(Acom.Tests.Engine.OptionsTests))]
public sealed class StartStateTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "acom-start-" + Guid.NewGuid().ToString("N")) + "/";

	public StartStateTests()
	{
		Directory.CreateDirectory(_root + "data");
		Directory.CreateDirectory(_root + "user");
		Options.SetFoldersForTest(_root + "data/", _root + "user/", _root + "user/");
	}

	public void Dispose()
	{
		StartState.loading = LoadingPhase.LOADING_STARTED;
		Directory.Delete(_root, true);
	}

	private static Acom.Action Key() => new(SDL_Event.KeyDown(SDLKey.SDLK_a), 1, 1, 0, 0);

	[Fact]
	public async Task Failed_load_shows_the_error_and_quits_on_a_key()
	{
		var game = new Game(new Screen(320, 200));
		var state = new StartState();
		game.SetState(state);
		Assert.False(game.Cursor.Visible);
		game.Update(); // init: starts loading
		Assert.NotNull(state.LoadingTask);
		await state.LoadingTask.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
		Assert.Equal(LoadingPhase.LOADING_FAILED, StartState.loading);
		Assert.Contains("no master mod", StartState.error, StringComparison.Ordinal);

		game.Update(); // think: prints the error
		Assert.Equal(LoadingPhase.LOADING_DONE, StartState.loading);
		Assert.True(game.Render());
		Assert.Contains(game.Screen.Surface.Pixels.ToArray(), p => p != 0);
		Assert.False(game.IsQuitting);

		game.PushEvent(SDL_Event.KeyDown(SDLKey.SDLK_a));
		game.Update();
		Assert.True(game.IsQuitting);
	}

	[Fact]
	public void Keys_do_nothing_while_loading()
	{
		_ = new Game(new Screen(320, 200));
		var state = new StartState();
		state.Handle(Key());
		Assert.Equal(LoadingPhase.LOADING_STARTED, StartState.loading);
		Assert.False(State.GamePtr!.IsQuitting);
	}

	[Fact]
	public void Successful_load_goes_to_the_main_menu()
	{
		var game = new Game(new Screen(320, 200));
		var state = new StartState();
		game.SetState(state);
		StartState.loading = LoadingPhase.LOADING_SUCCESSFUL;
		state.Think();
		Assert.IsType<GoToMainMenuState>(game.States[^1]);
		Assert.Single(game.States);
		Assert.True(game.Cursor.Visible);
	}
}
