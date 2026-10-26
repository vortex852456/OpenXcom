using GameMod = Acom.Mod;

namespace Acom.Tests.Menu;

/// <summary>Saves a new SavedGame like SaveGameState (backup file, then move) and lists/loads it like ListLoadState/LoadGameState.</summary>
[Collection("OptionsTests")]
public sealed class SaveRoundTripTests : IDisposable
{
	private readonly string _dir = Directory.CreateTempSubdirectory("acom-saves-").FullName + "/";

	public SaveRoundTripTests()
	{
		// the "UFO" resources of the master come from the user folder (see ModRulesetTests)
		Directory.CreateDirectory(_dir + "UFO/GEODATA");
		File.WriteAllBytes(_dir + "UFO/GEODATA/WORLD.DAT", []);
		File.WriteAllText(_dir + "UFO/README.txt", "stand-in");
		Options.SetFoldersForTest(Path.Combine(Engine.LanguageTests.RepoRoot(), "bin") + "/", _dir, _dir);
		CrossPlatform.SetArgs([]);
		Options.Create();
		Options.ResetDefault(includeMods: false);
		Options.mods = [new ModToggle("xcom1", true)];
		Options.UpdateMods();
		Options.language = "en-US";
		Options.wordwrap = TextWrapping.WRAP_AUTO;
		Directory.CreateDirectory(Options.GetMasterUserFolder());
	}

	public void Dispose()
	{
		FileMap.Clear(clearOnly: true, embeddedOnly: false);
		CrossPlatform.SetArgs([]);
		Directory.Delete(_dir, recursive: true);
	}

	private static Language English()
	{
		var lang = new Language();
		lang.LoadStrings(new Dictionary<string, string>
		{
			["STR_DATE_FIRST"] = "{0}st",
			["STR_DATE_SECOND"] = "{0}nd",
			["STR_DATE_THIRD"] = "{0}rd",
			["STR_DATE_FOURTH"] = "{0}th",
			["STR_JAN"] = "January",
			["STR_MAY"] = "May",
			["STR_GEOSCAPE"] = "Geoscape",
			["STR_QUICK_SAVE_SLOT"] = "[Quicksave]",
		});
		return lang;
	}

	/// <summary>The file part of SaveGameState::think.</summary>
	private static void SaveLikeSaveGameState(SavedGame save, string filename, GameMod mod)
	{
		string backup = filename + ".bak";
		save.Save(backup, mod);
		Assert.True(CrossPlatform.MoveFile(Options.GetMasterUserFolder() + backup, Options.GetMasterUserFolder() + filename));
	}

	[Fact]
	public void New_game_survives_save_list_and_load()
	{
		var mod = new GameMod(createConverter: false);
		var lang = English();

		var save = new SavedGame();
		save.Name = "Round trip";
		save.Time = new GameTime(6, 21, 5, 1999, 14, 5, 0);
		save.Difficulty = GameDifficulty.DIFF_VETERAN;
		save.UserNotes.Add("note one");
		SaveLikeSaveGameState(save, CrossPlatform.SanitizeFilename(save.Name) + ".sav", mod);
		save.Name = SavedGame.QUICKSAVE;
		SaveLikeSaveGameState(save, SavedGame.QUICKSAVE, mod);

		Assert.False(File.Exists(Options.GetMasterUserFolder() + "Round trip.sav.bak"));

		// ListLoadState shows auto/quick saves, ListSaveState does not
		var all = SavedGame.GetList(lang, true);
		Assert.Equal(2, all.Count);
		ListGamesState.SortSaves(all, SaveSort.SORT_NAME_ASC);
		Assert.Equal(["[Quicksave]", "Round trip"], all.Select(s => s.displayName));
		Assert.True(all[0].reserved);
		Assert.Equal(["xcom1 ver: " + Options.GetActiveMasterInfo().Version], all[1].mods);

		var manual = SavedGame.GetList(lang, false);
		Assert.Equal("Round trip.sav", Assert.Single(manual).fileName);

		var loaded = new SavedGame();
		loaded.Load(manual[0].fileName, mod, lang);
		Assert.Equal("Round trip", loaded.Name);
		Assert.Equal(GameDifficulty.DIFF_VETERAN, loaded.Difficulty);
		Assert.Equal((1999, 5, 21, 14, 5), (loaded.Time.Year, loaded.Time.Month, loaded.Time.Day, loaded.Time.Hour, loaded.Time.Minute));
		Assert.Equal(["note one"], loaded.UserNotes);
		Assert.Equal(GameEnding.END_NONE, loaded.Ending);

		// DeleteGameState
		Assert.True(CrossPlatform.DeleteFile(Options.GetMasterUserFolder() + manual[0].fileName));
		Assert.False(CrossPlatform.DeleteFile(Options.GetMasterUserFolder() + manual[0].fileName));
		Assert.Empty(SavedGame.GetList(lang, false));
	}
}
