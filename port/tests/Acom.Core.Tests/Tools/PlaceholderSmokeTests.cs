/*
 * Copyright 2010-2026 OpenXcom Developers.
 * C# port: Copyright 2026 ACOM contributors.
 *
 * This file is part of ACOM, a C# port of OpenXcom Extended.
 *
 * ACOM is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * ACOM is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Acom.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Reflection;
using System.Security.Cryptography;
using Acom.Placeholder;

namespace Acom.Tests.Tools;

/// <summary>
/// End-to-end smoke test: generates the placeholder data set, loads the xcom1 master mod with resources through
/// the real start-up path and plays the first days of a new campaign headless (fake clock, no audio device).
/// Changes Options, FileMap, the clock and the static game pointer.
/// </summary>
[Collection(nameof(Acom.Tests.Engine.OptionsTests))]
public sealed class PlaceholderSmokeTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "acom-placeholder-" + Guid.NewGuid().ToString("N")) + "/";
	private readonly Func<uint> _oldClock = SDLTimer.Source;
	private uint _now = 1000;

	public void Dispose()
	{
		SDLTimer.Source = _oldClock;
		Acom.Timer.ResetSlowTick();
		StartState.loading = LoadingPhase.LOADING_STARTED;
		State.SetGamePtr(null);
		if (Directory.Exists(_root))
			Directory.Delete(_root, true);
	}

	private static string RepoRoot() => RepoPaths.FindRoot() ?? throw new InvalidOperationException("repository not found");

	private static GeneratorReport Generate(string outDir)
	{
		string root = RepoRoot();
		var spec = AssetList.Load(Path.Combine(root, "content", "spec", "ufo", "assets.json"));
		return new PlaceholderGenerator(spec, Rules.Load(Path.Combine(root, "bin", "standard", "xcom1")), null).Generate(outDir);
	}

	/// <summary>Folders of the master mod that hold data derived from the original game; left out so only placeholder data is used.</summary>
	private static readonly string[] s_derivedFolders = ["MAPS", "ROUTES", "TERRAIN", "UFOGRAPH", "UNITS"];

	/// <summary>
	/// Data folder for the engine: the common resources and the xcom1 rulesets of the repository, without the
	/// mod's own map, terrain and sprite files (so every battlescape file comes from the placeholder set).
	/// </summary>
	private string MakeDataFolder()
	{
		string bin = Path.Combine(RepoRoot(), "bin");
		string data = _root + "data/";
		Directory.CreateDirectory(data + "standard");
		Directory.CreateSymbolicLink(data + "common", Path.Combine(bin, "common"));
		string src = Path.Combine(bin, "standard", "xcom1");
		foreach (var file in Directory.EnumerateFiles(src, "*", SearchOption.AllDirectories))
		{
			string rel = Path.GetRelativePath(src, file);
			if (s_derivedFolders.Contains(rel.Split(Path.DirectorySeparatorChar)[0], StringComparer.OrdinalIgnoreCase))
				continue;
			string dest = Path.Combine(data, "standard", "xcom1", rel);
			Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
			File.Copy(file, dest);
		}
		return data;
	}

	[Fact]
	public void Generator_is_deterministic()
	{
		Generate(_root + "a");
		Generate(_root + "b");
		var files = Directory.GetFiles(_root + "a", "*", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(_root + "a", f)).Order(StringComparer.Ordinal).ToList();
		Assert.True(files.Count > 500);
		foreach (var file in files)
			Assert.Equal(SHA256.HashData(File.ReadAllBytes(_root + "a/" + file)), SHA256.HashData(File.ReadAllBytes(_root + "b/" + file)));
	}

	/// <summary>One iteration of the host loop with 20 ms of fake time.</summary>
	private void Frame(Game game)
	{
		_now += 20;
		game.RunFrame();
	}

	private static T Field<T>(object obj, string name) =>
		(T)obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(obj)!;

	/// <summary>Clicks the middle of a surface through the event queue (window coordinates like the host sends them).</summary>
	private void Click(Game game, Surface surface, int? atX = null, int? atY = null)
	{
		int x = (int)((atX ?? surface.X + surface.Width / 2) * game.Screen.XScale) + game.Screen.CursorLeftBlackBand;
		int y = (int)((atY ?? surface.Y + surface.Height / 2) * game.Screen.YScale) + game.Screen.CursorTopBlackBand;
		game.PushEvent(SDL_Event.MouseMotion(x, y));
		game.PushEvent(SDL_Event.MouseButtonDown(SDLMouse.SDL_BUTTON_LEFT, x, y));
		Frame(game);
		game.PushEvent(SDL_Event.MouseButtonUp(SDLMouse.SDL_BUTTON_LEFT, x, y));
		Frame(game);
	}

	private T WaitFor<T>(Game game, int maxFrames = 200) where T : State
	{
		for (int i = 0; i < maxFrames; ++i)
		{
			if (game.States.Count > 0 && game.States[^1] is T t)
			{
				// let window popup animations finish before clicking
				for (int j = 0; j < 30; ++j)
					Frame(game);
				return t;
			}
			Frame(game);
		}
		throw new InvalidOperationException($"{typeof(T).Name} not reached, top state: {(game.States.Count > 0 ? game.States[^1].GetType().Name : "none")}");
	}

	/// <summary>Generates the data, points the folders at it and runs the start-up screen until the main menu.</summary>
	private async Task<Game> StartGame()
	{
		Generate(_root + "user/UFO");
		SDLTimer.Source = () => _now;
		Acom.Timer.ResetSlowTick();
		Options.Create();
		Options.ResetDefault(includeMods: true);
		// the placeholder data goes in the user folder, which is searched first for the master's "UFO" resources
		Options.SetFoldersForTest(MakeDataFolder(), _root + "user/", _root + "user/");
		Options.playIntro = false;
		FileMap.Clear(false, false);

		var game = new Game(new Screen(320, 200));
		Assert.True(Options.mute); // headless: no audio device
		var start = new StartState();
		game.SetState(start);
		game.Update();
		await start.LoadingTask.WaitAsync(TimeSpan.FromMinutes(2), TestContext.Current.CancellationToken);
		Assert.True(StartState.loading == LoadingPhase.LOADING_SUCCESSFUL, StartState.error);
		Assert.Equal("xcom1", Options.GetActiveMaster());
		return game;
	}

	[Fact]
	public async Task New_campaign_runs_on_placeholder_data()
	{
		var game = await StartGame();
		var menu = WaitFor<MainMenuState>(game);
		Click(game, Field<TextButton>(menu, "_btnNewGame"));
		var newGame = WaitFor<NewGameState>(game);
		Click(game, Field<TextButton>(newGame, "_btnOk"));

		// the first base: rotate the globe to a land point (a country label) and click it
		var build = WaitFor<BuildNewBaseState>(game);
		var globe = Field<Globe>(build, "_globe");
		var usa = game.Mod!.GetCountry("STR_USA");
		globe.Center(usa.LabelLongitude, usa.LabelLatitude);
		Frame(game);
		Assert.True(globe.InsideLand(usa.LabelLongitude, usa.LabelLatitude));
		short gx = 0, gy = 0;
		globe.PolarToCart(usa.LabelLongitude, usa.LabelLatitude, ref gx, ref gy);
		Click(game, globe, gx, gy);
		var name = WaitFor<BaseNameState>(game);
		foreach (char c in "Alpha")
			game.PushEvent(SDL_Event.KeyDown(SDLKey.SDLK_a + (char.ToLowerInvariant(c) - 'a'), SDLMod.KMOD_NONE, c));
		Frame(game);
		game.PushEvent(SDL_Event.KeyDown(SDLKey.SDLK_RETURN));
		Frame(game);

		var geo = WaitFor<GeoscapeState>(game);
		var save = game.SavedGame!;
		Assert.Single(save.Bases);
		Assert.Equal("Alpha", save.Bases[0].GetName(game.Language));
		Assert.True(game.Render());

		// let the geoscape run at 1 day per tick past the first month end, closing popups as a player would
		// (popups reset the speed to 5 seconds, so it is selected again)
		const int days = 35;
		var popups = new List<string>();
		Click(game, Field<TextButton>(geo, "_btn1Day"));
		for (int i = 0; i < 5000 && save.DaysPassed < days; ++i)
		{
			if (game.States[^1] is not GeoscapeState)
			{
				popups.Add(game.States[^1].GetType().Name);
				game.PopState();
				Frame(game);
				Click(game, Field<TextButton>(geo, "_btn1Day"));
			}
			Frame(game);
		}
		Assert.True(save.DaysPassed >= days, $"time stuck at {save.Time.Day}.{save.Time.Month}, popups: {string.Join(", ", popups)}");
		Assert.Contains(nameof(MonthlyReportState), popups);

		// save and load the campaign
		save.Save("placeholder.sav", game.Mod);
		var loaded = new SavedGame();
		loaded.Load("placeholder.sav", game.Mod, game.Language);
		Assert.Equal(save.DaysPassed, loaded.DaysPassed);
		Assert.Equal(save.Funds, loaded.Funds);
		Assert.Equal(save.Bases[0].Facilities.Count, loaded.Bases[0].Facilities.Count);
		Assert.True(game.Render());
		Assert.Contains(game.Screen.Surface.Pixels.ToArray(), p => p != 0);
	}

	[Fact]
	public async Task New_battles_generate_on_placeholder_data()
	{
		var game = await StartGame();
		WaitFor<MainMenuState>(game);
		var failures = new List<string>();
		int battles = 0;
		int missions = game.Mod!.DeploymentsList.Count;
		for (int m = 0; m < missions; ++m)
		{
			for (int t = 0; ; ++t)
			{
				game.SetState(new MainMenuState());
				var state = new NewBattleState();
				game.PushState(state);
				Frame(game);
				var cbxMission = Field<ComboBox>(state, "_cbxMission");
				var missionTypes = Field<List<string>>(state, "_missionTypes");
				if (m >= missionTypes.Count)
					break;
				cbxMission.Selected = m;
				state.CbxMissionChange(null!);
				var terrains = Field<List<string>>(state, "_terrainTypes");
				if (t >= terrains.Count)
					break;
				Field<ComboBox>(state, "_cbxTerrain").Selected = t;
				state.CbxTerrainChange(null!);
				string what = $"{missionTypes[m]} / {terrains[t]}";
				try
				{
					state.BtnOkClick(null!);
					if (game.States[^1] is not BriefingState)
						failures.Add(what + ": no briefing");
					var battle = game.SavedGame!.SavedBattle!;
					if (battle.MapSizeX * battle.MapSizeY == 0 || battle.Units.Count == 0)
						failures.Add(what + ": empty battle");
					++battles;
				}
				catch (Exception e)
				{
					failures.Add($"{what}: {e.GetType().Name}: {e.Message} @ {string.Join(" | ", (e.StackTrace ?? "").Split('\n').Take(8).Select(l => l.Trim()))}");
				}
			}
		}
		Assert.Empty(failures);
		Assert.True(battles > 40, $"only {battles} battles generated");
	}

	/// <summary>Generates one battle through the New Battle screen (up to the briefing).</summary>
	private static void GenerateBattle(Game game, string missionType, int terrain = 0)
	{
		game.SetState(new MainMenuState());
		var state = new NewBattleState();
		game.PushState(state);
		game.RunFrame();
		var missionTypes = Field<List<string>>(state, "_missionTypes");
		Field<ComboBox>(state, "_cbxMission").Selected = missionTypes.IndexOf(missionType);
		state.CbxMissionChange(null!);
		Field<ComboBox>(state, "_cbxTerrain").Selected = terrain;
		state.CbxTerrainChange(null!);
		state.BtnOkClick(null!);
		Assert.IsType<BriefingState>(game.States[^1]);
	}

	/// <summary>Leaves the briefing through its OK button.</summary>
	private static BattlescapeState EnterBattle(Game game)
	{
		Assert.IsType<BriefingState>(game.States[^1]).BtnOkClick(null!);
		Assert.True(game.States[^1] is InventoryState, "no inventory after briefing");
		return Assert.IsType<BattlescapeState>(game.States[^3]);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	public async Task Map_cursor_is_drawn_on_upper_levels(int level)
	{
		var game = await StartGame();
		WaitFor<MainMenuState>(game);
		GenerateBattle(game, "STR_TERROR_MISSION");
		var bs = EnterBattle(game);
		var battle = game.SavedGame!.SavedBattle!;
		Assert.True(battle.MapSizeZ > level);
		var map = bs.Map;
		var unit = battle.Units.First(u => u.Faction == UnitFaction.FACTION_PLAYER);
		var pos = new Position(unit.Position.x, unit.Position.y, level);
		map.Camera.CenterOnPosition(pos, false);
		map.Camera.ViewLevel = level;
		var screen = new Position();
		map.Camera.ConvertMapToScreen(pos, ref screen);
		screen += map.Camera.MapOffset;
		map.SetSelectorPosition(screen.x + 16, screen.y + 12);
		byte[] Render(CursorType type)
		{
			map.SetCursorType(type);
			map.Invalidate();
			map.Draw();
			var px = new byte[map.Width * map.Height];
			for (int y = 0; y < map.Height; ++y)
				for (int x = 0; x < map.Width; ++x)
					px[y * map.Width + x] = map.GetPixel(x, y);
			return px;
		}
		Assert.NotEqual(Render(CursorType.CT_NONE), Render(CursorType.CT_NORMAL));
	}

	[Theory]
	[InlineData("STR_SMALL_SCOUT")]
	[InlineData("STR_TERROR_MISSION")]
	[InlineData("STR_BASE_DEFENSE")]
	public async Task Battle_turns_run_on_placeholder_data(string missionType)
	{
		var game = await StartGame();
		WaitFor<MainMenuState>(game);
		GenerateBattle(game, missionType);
		var bs = EnterBattle(game);
		var battle = game.SavedGame!.SavedBattle!;
		var screens = new List<string>();
		int turn = battle.Turn, moves = 0, shots = 0, plannedTurn = -1;
		var plan = new Queue<System.Action>();
		bool finished = false;
		for (int i = 0; i < 30000 && battle.Turn < turn + 3 && !finished; ++i)
		{
			var top = game.States[^1];
			switch (top)
			{
				case DebriefingState:
					// all aliens down (a small scout has a single one) - the battle is over before turn + 3
					finished = true;
					continue;
				case InventoryState inventory:
					inventory.BtnOkClick(null!);
					break;
				case NextTurnState next:
					next.Close();
					break;
				case BattlescapeState when battle.Side == UnitFaction.FACTION_PLAYER && !bs.BattleGame.IsBusy && i % 5 == 0:
					if (plannedTurn != battle.Turn)
					{
						plannedTurn = battle.Turn;
						PlanPlayerTurn(battle, bs.BattleGame, plan, () => ++moves, () => ++shots);
					}
					if (plan.Count > 0)
						plan.Dequeue()();
					else
						bs.BtnEndTurnClick(null!);
					break;
				case BattlescapeState:
					break;
				default:
					screens.Add(top.GetType().Name);
					game.PopState();
					break;
			}
			Frame(game);
		}
		Assert.True(moves > 0, "no soldier moved");
		Assert.True(finished || battle.Turn >= turn + 3, $"battle stuck in turn {battle.Turn}, side {battle.Side}, screens: {string.Join(", ", screens)}");
	}

	/// <summary>
	/// Player input for one turn, as the battlescape UI would send it: select up to four soldiers, walk each a few
	/// tiles and take a snap shot at a visible enemy.
	/// </summary>
	private static void PlanPlayerTurn(SavedBattleGame battle, BattlescapeGame bg, Queue<System.Action> plan, System.Action moved, System.Action shot)
	{
		var soldiers = battle.Units.Where(u => u.Faction == UnitFaction.FACTION_PLAYER && !u.IsOut).Take(4).ToList();
		foreach (var unit in soldiers)
		{
			plan.Enqueue(() => bg.PrimaryAction(unit.Position)); // select
			plan.Enqueue(() =>
			{
				if (unit.IsOut || battle.SelectedUnit != unit)
					return;
				int dx = (battle.Turn + unit.Id) % 2 == 0 ? 3 : -3;
				var dest = unit.Position + new Position(dx, 2, 0);
				if (battle.GetTile(dest) != null)
				{
					bg.PrimaryAction(dest);
					moved();
				}
			});
			plan.Enqueue(() =>
			{
				var target = unit.IsOut ? null : unit.VisibleUnits.FirstOrDefault(u => u.Faction == UnitFaction.FACTION_HOSTILE && !u.IsOut);
				var weapon = unit.IsOut ? null : unit.GetMainHandWeapon();
				if (target == null || weapon == null || battle.SelectedUnit != unit)
					return;
				var action = bg.CurrentAction;
				action.actor = unit;
				action.weapon = weapon;
				action.type = BattleActionType.BA_SNAPSHOT;
				action.UpdateTU();
				if (!action.HaveTU())
					return;
				action.targeting = true;
				bg.PrimaryAction(target.Position);
				shot();
			});
		}
	}
}
