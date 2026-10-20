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
// Ported from: src/Savegame/SavedGame.h, src/Savegame/SavedGame.cpp
// Port status: partial (missing: script hooks)
#nullable disable

using System.Globalization;
using System.Text;

namespace Acom;

/// <summary>Enumerator containing all the possible game difficulties.</summary>
public enum GameDifficulty
{
	DIFF_BEGINNER = 0,
	DIFF_EXPERIENCED = 1,
	DIFF_VETERAN = 2,
	DIFF_GENIUS = 3,
	DIFF_SUPERHUMAN = 4,
}

/// <summary>Enumerator for the various save types.</summary>
public enum SaveType : uint
{
	SAVE_DEFAULT = 0,
	SAVE_INSTA = 1,
	SAVE_QUICK = 2,
	SAVE_AUTO_GEOSCAPE = 3,
	SAVE_AUTO_BATTLESCAPE = 4,
	SAVE_IRONMAN = 5,
	SAVE_IRONMAN_END = 6,
}

/// <summary>Enumerator for the current game ending.</summary>
public enum GameEnding : uint
{
	END_NONE = 0,
	END_WIN = 1,
	END_LOSE = 2,
}

/// <summary>Container for savegame info displayed on listings.</summary>
public class SaveInfo
{
	public string fileName = "";
	public string displayName = "";
	/// <summary>File modification time (C time_t: seconds since 1970-01-01 UTC).</summary>
	public long timestamp;
	public string isoDate = "";
	public string isoTime = "";
	public string details = "";
	public List<string> mods = [];
	public bool reserved;
}

/// <summary>
/// The game data that gets written to disk when the game is saved.
/// A saved game holds all the variable info in a game like funds,
/// game time, current bases and contents, world activities, score, etc.
/// </summary>
/// <remarks>
/// C++ keeps the discovered research sorted by pointer address (an arbitrary but fixed order) for binary searches;
/// the port sorts by the (unique) research name instead. Only the order of <see cref="DiscoveredResearch"/> and of the
/// lists from <see cref="GetNewlyAvailableResearchProjects"/> can differ from the original.
/// </remarks>
public class SavedGame
{
	public Country debugCountry;
	public Region debugRegion;
	public int debugType;
	public int debugZone;
	public int debugArea;

	/// <summary>Name of class used in script.</summary>
	public const string ScriptName = "GeoscapeGame";

	public const int MAX_EQUIPMENT_LAYOUT_TEMPLATES = 50;
	public const int MAX_CRAFT_LOADOUT_TEMPLATES = 10;

	public const string AUTOSAVE_GEOSCAPE = "_autogeo_.asav";
	public const string AUTOSAVE_BATTLESCAPE = "_autobattle_.asav";
	public const string QUICKSAVE = "_quick_.asav";

	private string _name = "";
	private GameDifficulty _difficulty;
	private GameEnding _end;
	private bool _ironman;
	private GameTime _time;
	private List<string> _userNotes = [];
	private List<string> _geoscapeDebugLog = [];
	private List<int> _researchScores = [];
	private List<long> _funds = [], _maintenance = [], _incomes = [], _expenditures = [];
	private double _globeLon, _globeLat;
	private int _globeZoom;
	private SortedDictionary<string, int> _ids = new(StringComparer.Ordinal);
	private readonly List<Country> _countries = [];
	private readonly List<Region> _regions = [];
	private readonly List<Base> _bases = [];
	private readonly List<Ufo> _ufos = [];
	private readonly List<Waypoint> _waypoints = [];
	private readonly List<MissionSite> _missionSites = [];
	private readonly List<AlienBase> _alienBases = [];
	private readonly AlienStrategy _alienStrategy;
	private SavedBattleGame _battleGame;
	private readonly List<RuleResearch> _discovered = [];
	private readonly List<ResearchDiaryEntry> _researchDiary = [];
	private SortedDictionary<string, int> _generatedEvents = new(StringComparer.Ordinal);
	private SortedDictionary<string, int> _ufopediaRuleStatus = new(StringComparer.Ordinal);
	private SortedDictionary<string, int> _manufactureRuleStatus = new(StringComparer.Ordinal);
	private SortedDictionary<string, int> _researchRuleStatus = new(StringComparer.Ordinal);
	private SortedDictionary<string, int> _monthlyPurchaseLimitLog = new(StringComparer.Ordinal);
	private SortedDictionary<string, bool> _hiddenPurchaseItemsMap = new(StringComparer.Ordinal);
	private SortedDictionary<string, List<List<int>>> _customRuleCraftDeployments = new(StringComparer.Ordinal);
	private Base _previewBase;
	private readonly List<AlienMission> _activeMissions = [];
	private readonly List<GeoscapeEvent> _geoscapeEvents = [];
	private bool _debug, _warned;
	private bool _togglePersonalLight, _toggleNightVision;
	private int _toggleBrightness;
	private int _monthsPassed;
	private int _daysPassed;
	private int _vehiclesLost, _craftLostDogfight, _craftLostMission;
	private string _graphRegionToggles = "";
	private string _graphCountryToggles = "";
	private string _graphFinanceToggles = "";
	private readonly List<RuleResearch> _poppedResearch = [];
	private readonly List<Soldier> _deadSoldiers = [];
	private int _selectedBase;
	private string _lastselectedArmor; //contains the last selected armor
	private readonly string[] _globalEquipmentLayoutName = new string[MAX_EQUIPMENT_LAYOUT_TEMPLATES];
	private readonly string[] _globalEquipmentLayoutArmor = new string[MAX_EQUIPMENT_LAYOUT_TEMPLATES];
	private readonly List<EquipmentLayoutItem>[] _globalEquipmentLayout = new List<EquipmentLayoutItem>[MAX_EQUIPMENT_LAYOUT_TEMPLATES];
	private readonly string[] _globalCraftLoadoutName = new string[MAX_CRAFT_LOADOUT_TEMPLATES];
	private readonly ItemContainer[] _globalCraftLoadout = new ItemContainer[MAX_CRAFT_LOADOUT_TEMPLATES];
	private readonly List<MissionStatistics> _missionStatistics = [];
	private readonly SortedSet<int> _ignoredUfos = [];
	private readonly HashSet<RuleItem> _autosales = [];
	private bool _disableSoldierEquipment;
	private bool _alienContainmentChecked;

	// C++ std::less<const RuleResearch*> replacement (see class remarks); null sorts first like a null pointer.
	private static int ResearchCompare(RuleResearch a, RuleResearch b)
	{
		if (ReferenceEquals(a, b))
			return 0;
		if (a == null)
			return -1;
		if (b == null)
			return 1;
		return string.CompareOrdinal(a.Name, b.Name);
	}

	private static bool ResearchLess(RuleResearch a, RuleResearch b) => ResearchCompare(a, b) < 0;

	private static void SortResearchVector(List<RuleResearch> vec) => vec.Sort(ResearchCompare);

	private static bool HaveResearchVector(List<RuleResearch> vec, RuleResearch res)
	{
		// std::lower_bound
		int first = 0, count = vec.Count;
		while (count > 0)
		{
			int step = count / 2;
			int it = first + step;
			if (ResearchLess(vec[it], res))
			{
				first = it + 1;
				count -= step + 1;
			}
			else
			{
				count = step;
			}
		}
		return first != vec.Count && vec[first] == res;
	}

	private static bool HaveResearchVector(List<RuleResearch> vec, string res)
	{
		return vec.Exists(r => r.Name == res);
	}

	/// <summary>Initializes a brand new saved game according to the specified difficulty.</summary>
	public SavedGame()
	{
		_difficulty = GameDifficulty.DIFF_BEGINNER;
		_end = GameEnding.END_NONE;
		_ironman = false;
		_globeLon = 0.0;
		_globeLat = 0.0;
		_globeZoom = 0;
		_battleGame = null;
		_previewBase = null;
		_debug = false;
		_warned = false;
		_togglePersonalLight = true;
		_toggleNightVision = false;
		_toggleBrightness = 0;
		_monthsPassed = -1;
		_daysPassed = 0;
		_vehiclesLost = 0;
		_craftLostDogfight = 0;
		_craftLostMission = 0;
		_selectedBase = 0;
		_disableSoldierEquipment = false;
		_alienContainmentChecked = false;

		Array.Fill(_globalEquipmentLayoutName, "");
		Array.Fill(_globalEquipmentLayoutArmor, "");
		Array.Fill(_globalCraftLoadoutName, "");
		for (int j = 0; j < MAX_EQUIPMENT_LAYOUT_TEMPLATES; ++j)
		{
			_globalEquipmentLayout[j] = [];
		}

		_time = new GameTime(6, 1, 1, 1999, 12, 0, 0);
		_alienStrategy = new AlienStrategy();
		_funds.Add(0);
		_maintenance.Add(0);
		_researchScores.Add(0);
		_incomes.Add(0);
		_expenditures.Add(0);
		_lastselectedArmor = "STR_NONE_UC";

		for (int j = 0; j < MAX_CRAFT_LOADOUT_TEMPLATES; ++j)
		{
			_globalCraftLoadout[j] = new ItemContainer();
		}
	}

	/// <summary>Removes version number from a mod name, if any.</summary>
	/// <param name="name">Mod id from a savegame.</param>
	/// <returns>Sanitized mod name.</returns>
	public static string SanitizeModName(string name)
	{
		ArgumentNullException.ThrowIfNull(name);
		int versionInfoBreakPoint = name.IndexOf(" ver: ", StringComparison.Ordinal);
		if (versionInfoBreakPoint == -1)
		{
			return name;
		}
		else
		{
			return name[..versionInfoBreakPoint];
		}
	}

	internal static bool IsCurrentGameType(SaveInfo saveInfo, string curMaster)
	{
		bool matchMasterMod = false;
		if (saveInfo.mods.Count == 0)
		{
			// if no mods listed in the savegame, this is an old-style
			// savegame.  assume "xcom1" as the game type.
			matchMasterMod = curMaster == "xcom1";
		}
		else
		{
			foreach (var modName in saveInfo.mods)
			{
				string name = SanitizeModName(modName);
				if (name == curMaster)
				{
					matchMasterMod = true;
					break;
				}
			}
		}

		if (!matchMasterMod)
		{
			Logger.Debug($"skipping save from inactive master: {saveInfo.fileName}");
		}

		return matchMasterMod;
	}

	/// <summary>Gets all the info of the saves found in the user folder.</summary>
	/// <param name="lang">Loaded language.</param>
	/// <param name="autoquick">Include autosaves and quicksaves.</param>
	/// <returns>List of saves info.</returns>
	public static List<SaveInfo> GetList(Language lang, bool autoquick)
	{
		var info = new List<SaveInfo>();
		string curMaster = Options.GetActiveMaster();
		var saves = CrossPlatform.GetFolderContents(Options.GetMasterUserFolder(), "sav");

		if (autoquick)
		{
			var asaves = CrossPlatform.GetFolderContents(Options.GetMasterUserFolder(), "asav");
			saves.InsertRange(0, asaves);
		}
		foreach (var tuple in saves)
		{
			string filename = tuple.Name;
			try
			{
				SaveInfo saveInfo = GetSaveInfo(filename, lang);
				if (!IsCurrentGameType(saveInfo, curMaster))
				{
					continue;
				}
				info.Add(saveInfo);
			}
			catch (Exception e) when (e is InvalidOperationException or YamlReadException)
			{
				Logger.Error($"{filename}: {e.Message}");
				continue;
			}
		}

		return info;
	}

	/// <summary>Gets the info of a specific save file.</summary>
	/// <param name="file">Save filename.</param>
	/// <param name="lang">Loaded language.</param>
	private static SaveInfo GetSaveInfo(string file, Language lang)
	{
		return ReadSaveInfo(Options.GetMasterUserFolder() + file, file, lang);
	}

	/// <summary>Body of C++ getSaveInfo for a full path (separate from the user folder lookup for tests).</summary>
	internal static SaveInfo ReadSaveInfo(string fullname, string file, Language lang)
	{
		ArgumentNullException.ThrowIfNull(lang);
		var reader = new YamlRootNodeReader(ReadYamlSaveHeader(fullname), fullname).ToBase();
		var save = new SaveInfo();

		save.fileName = file;

		if (save.fileName == QUICKSAVE)
		{
			save.displayName = lang.GetString("STR_QUICK_SAVE_SLOT");
			save.reserved = true;
		}
		else if (save.fileName == AUTOSAVE_GEOSCAPE)
		{
			save.displayName = lang.GetString("STR_AUTO_SAVE_GEOSCAPE_SLOT");
			save.reserved = true;
		}
		else if (save.fileName.Contains(AUTOSAVE_GEOSCAPE, StringComparison.Ordinal))
		{
			var time = new GameTime(6, 1, 1, 1999, 12, 0, 0);
			if (reader["time"])
				time.Load(reader["time"]);
			save.displayName = lang.GetString("STR_AUTO_SAVE_GEOSCAPE_SLOT_WITH_NUMBER").Arg(time.GetDayString(lang));
			save.reserved = true;
		}
		else if (save.fileName == AUTOSAVE_BATTLESCAPE)
		{
			save.displayName = lang.GetString("STR_AUTO_SAVE_BATTLESCAPE_SLOT");
			save.reserved = true;
		}
		else if (save.fileName.Contains(AUTOSAVE_BATTLESCAPE, StringComparison.Ordinal))
		{
			int turn = reader["turn"].ReadVal(0);
			save.displayName = lang.GetString("STR_AUTO_SAVE_BATTLESCAPE_SLOT_WITH_NUMBER").Arg(turn);
			save.reserved = true;
		}
		else
		{
			if (!reader.TryRead("name", ref save.displayName))
				save.displayName = CrossPlatform.NoExt(file);
			save.reserved = false;
		}

		save.timestamp = GetDateModified(fullname);
		var str = TimeToString(save.timestamp);
		save.isoDate = str.first;
		save.isoTime = str.second;
		reader.TryRead("mods", ref save.mods);

		var details = new StringBuilder();
		if (reader["turn"])
		{
			details.Append(lang.GetString("STR_BATTLESCAPE")).Append(": ").Append(lang.GetString(reader["mission"].ReadVal<string>())).Append(", ");
			details.Append(lang.GetString("STR_TURN").Arg(reader["turn"].ReadVal<int>()));
		}
		else
		{
			var time = new GameTime(6, 1, 1, 1999, 12, 0, 0);
			time.Load(reader["time"]);
			details.Append(lang.GetString("STR_GEOSCAPE")).Append(": ");
			details.Append(time.GetDayString(lang)).Append(' ').Append(lang.GetString(time.MonthString)).Append(' ').Append(time.Year.ToString(CultureInfo.InvariantCulture)).Append(", ");
			details.Append(time.Hour.ToString(CultureInfo.InvariantCulture)).Append(':').Append(time.Minute.ToString("D2", CultureInfo.InvariantCulture));
		}
		if (reader["ironman"].ReadVal(false))
		{
			details.Append(" (").Append(lang.GetString("STR_IRONMAN")).Append(')');
		}
		save.details = details.ToString();

		return save;
	}

	/// <summary>
	/// Reads a save file up to the first "\n---" document separator and returns the header document
	/// (C++ CrossPlatform::getYamlSaveHeaderRaw plus the cut in YamlRootNodeReader). Empty if there is no separator.
	/// </summary>
	internal static string ReadYamlSaveHeader(string filename)
	{
		ReadOnlySpan<byte> separator = "\n---"u8;
		try
		{
			using var stream = File.OpenRead(filename);
			var data = new MemoryStream();
			var chunk = new byte[4096];
			int found = -1;
			while (true)
			{
				int actuallyRead = stream.Read(chunk, 0, chunk.Length);
				if (actuallyRead == 0)
				{
					break;
				}
				int searchFrom = Math.Max(0, (int)data.Length - (separator.Length - 1));
				data.Write(chunk, 0, actuallyRead);
				int pos = data.GetBuffer().AsSpan(searchFrom, (int)data.Length - searchFrom).IndexOf(separator);
				if (pos >= 0)
				{
					found = searchFrom + pos;
					break;
				}
			}
			// C++: str.find("\n---") + 1, i.e. npos + 1 == 0 without separator
			int length = found + 1;
			return Encoding.UTF8.GetString(data.GetBuffer(), 0, length);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			string err = $"Failed to read {filename}: {e.Message}";
			Logger.Error(err);
			throw new InvalidOperationException(err, e);
		}
	}

	/// <summary>C++ CrossPlatform::getDateModified: modification time of a file as time_t, 0 if it can't be read.</summary>
	private static long GetDateModified(string path)
	{
		if (!File.Exists(path))
			return 0;
		return new DateTimeOffset(File.GetLastWriteTimeUtc(path)).ToUnixTimeSeconds();
	}

	/// <summary>C++ CrossPlatform::timeToString: local date and time in ISO 8601 ("%Y-%m-%d", "%H:%M").</summary>
	private static (string first, string second) TimeToString(long time)
	{
		var local = DateTimeOffset.FromUnixTimeSeconds(time).ToLocalTime();
		return (local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), local.ToString("HH:mm", CultureInfo.InvariantCulture));
	}

	/// <summary>Reads a string-keyed map and keeps the byte-wise key order of C++ std::map.</summary>
	private static void TryReadMap<TValue>(YamlNodeReader reader, string key, ref SortedDictionary<string, TValue> map)
	{
		SortedDictionary<string, TValue> read = null;
		if (reader.TryRead(key, ref read))
			map = new SortedDictionary<string, TValue>(read, StringComparer.Ordinal);
	}

	/// <summary>Loads a saved game's contents from a YAML file.</summary>
	/// <remarks>Assumes the saved game is blank.</remarks>
	/// <param name="filename">YAML filename.</param>
	/// <param name="mod">Mod for the saved game.</param>
	/// <param name="lang">Loaded language.</param>
	public void Load(string filename, Mod mod, Language lang)
	{
		ArgumentNullException.ThrowIfNull(mod);
		string filepath = Options.GetMasterUserFolder() + filename;
		var documents = YamlRootNodeReader.FromFile(filepath);

		// Get brief save info
		var header = documents[0];
		_time.Load(header["time"]);
		header.ReadNode("name", ref _name, filename);
		header.TryRead("ironman", ref _ironman);

		// Get full save data
		var reader = documents[1].UseIndex();
		reader.TryRead("difficulty", ref _difficulty);
		reader.TryRead("end", ref _end);
		if (reader["rng"] && (_ironman || !Options.newSeedOnLoad))
			Rng.Seed = reader["rng"].ReadVal<ulong>();
		reader.TryRead("monthsPassed", ref _monthsPassed);
		reader.TryRead("daysPassed", ref _daysPassed);
		reader.TryRead("vehiclesLost", ref _vehiclesLost);
		reader.TryRead("craftLostDogfight", ref _craftLostDogfight);
		reader.TryRead("craftLostMission", ref _craftLostMission);
		reader.TryRead("graphRegionToggles", ref _graphRegionToggles);
		reader.TryRead("graphCountryToggles", ref _graphCountryToggles);
		reader.TryRead("graphFinanceToggles", ref _graphFinanceToggles);
		reader.TryRead("funds", ref _funds);
		reader.TryRead("maintenance", ref _maintenance);
		reader.TryRead("userNotes", ref _userNotes);
		reader.TryRead("geoscapeDebugLog", ref _geoscapeDebugLog);
		reader.TryRead("researchScores", ref _researchScores);
		reader.TryRead("incomes", ref _incomes);
		reader.TryRead("expenditures", ref _expenditures);
		reader.TryRead("warned", ref _warned);
		reader.TryRead("togglePersonalLight", ref _togglePersonalLight);
		reader.TryRead("toggleNightVision", ref _toggleNightVision);
		reader.TryRead("toggleBrightness", ref _toggleBrightness);
		reader.TryRead("globeLon", ref _globeLon);
		reader.TryRead("globeLat", ref _globeLat);
		reader.TryRead("globeZoom", ref _globeZoom);
		TryReadMap(reader, "ids", ref _ids);

		foreach (var country in reader["countries"].Children())
		{
			string type = country["type"].ReadVal<string>();
			if (mod.GetCountry(type) != null)
			{
				var c = new Country(mod.GetCountry(type), false);
				c.Load(country);
				_countries.Add(c);
			}
			else
			{
				Logger.Error($"Failed to load country {type}");
			}
		}

		foreach (var region in reader["regions"].Children())
		{
			string type = region["type"].ReadVal<string>();
			if (mod.GetRegion(type) != null)
			{
				var r = new Region(mod.GetRegion(type));
				r.Load(region);
				_regions.Add(r);
			}
			else
			{
				Logger.Error($"Failed to load region {type}");
			}
		}

		// Alien bases must be loaded before alien missions
		foreach (var alienBase in reader["alienBases"].Children())
		{
			string deployment = alienBase["deployment"].ReadVal("STR_ALIEN_BASE_ASSAULT");
			if (mod.GetDeployment(deployment) != null)
			{
				var b = new AlienBase(mod.GetDeployment(deployment), 0);
				b.Load(alienBase);
				_alienBases.Add(b);
			}
			else
			{
				Logger.Error($"Failed to load deployment for alien base {deployment}");
			}
		}

		// Missions must be loaded before UFOs.
		foreach (var alienMission in reader["alienMissions"].Children())
		{
			string missionType = alienMission["type"].ReadVal<string>();
			if (mod.GetAlienMission(missionType) != null)
			{
				RuleAlienMission mRule = mod.GetAlienMission(missionType);
				var mission = new AlienMission(mRule);
				mission.Load(alienMission, this, mod);
				_activeMissions.Add(mission);
			}
			else
			{
				Logger.Error($"Failed to load mission {missionType}");
			}
		}

		foreach (var ufo in reader["ufos"].Children())
		{
			string type = ufo["type"].ReadVal<string>();
			if (mod.GetUfo(type) != null)
			{
				var u = new Ufo(mod.GetUfo(type), 0);
				u.Load(ufo, mod, this);
				_ufos.Add(u);
			}
			else
			{
				Logger.Error($"Failed to load UFO {type}");
			}
		}

		foreach (var geoEvent in reader["geoscapeEvents"].Children())
		{
			string eventName = geoEvent["name"].ReadVal<string>();
			if (mod.GetEvent(eventName) != null)
			{
				RuleEvent eventRule = mod.GetEvent(eventName);
				var @event = new GeoscapeEvent(eventRule);
				@event.Load(geoEvent);
				_geoscapeEvents.Add(@event);
			}
			else
			{
				Logger.Error($"Failed to load geoscape event {eventName}");
			}
		}

		foreach (var waypoint in reader["waypoints"].Children())
		{
			var w = new Waypoint();
			w.Load(waypoint);
			_waypoints.Add(w);
		}

		// Backwards compatibility
		foreach (var terrorSite in reader["terrorSites"].Children())
		{
			string type = "STR_ALIEN_TERROR";
			string deployment = "STR_TERROR_MISSION";
			if (mod.GetAlienMission(type) != null && mod.GetDeployment(deployment) != null)
			{
				var m = new MissionSite(mod.GetAlienMission(type), mod.GetDeployment(deployment), null);
				m.Load(terrorSite);
				_missionSites.Add(m);
			}
			else
			{
				Logger.Error($"Failed to load mission {type} deployment {deployment}");
			}
		}

		foreach (var missionSite in reader["missionSites"].Children())
		{
			string type = missionSite["type"].ReadVal<string>();
			string deployment = missionSite["deployment"].ReadVal("STR_TERROR_MISSION");
			string alienWeaponDeploy = missionSite["missionCustomDeploy"].ReadVal("");
			if (mod.GetAlienMission(type) != null && mod.GetDeployment(deployment) != null)
			{
				var m = new MissionSite(mod.GetAlienMission(type), mod.GetDeployment(deployment), mod.GetDeployment(alienWeaponDeploy));
				m.Load(missionSite);
				_missionSites.Add(m);
				// link with UFO
				if (m.UfoUniqueId > 0)
				{
					Ufo ufo = null;
					foreach (var u in _ufos)
					{
						if (u.UniqueId == m.UfoUniqueId)
						{
							ufo = u;
							break;
						}
					}
					if (ufo != null)
					{
						m.Ufo = ufo;
					}
				}
			}
			else
			{
				Logger.Error($"Failed to load mission {type} deployment {deployment}");
			}
		}

		// Discovered Techs Should be loaded before Bases (e.g. for PSI evaluation)
		foreach (var discovery in reader["discovered"].Children())
		{
			string research = discovery.ReadVal<string>();
			if (mod.GetResearch(research) is RuleResearch researchRule)
			{
				_discovered.Add(researchRule);
			}
			else
			{
				Logger.Error($"Failed to load research {research}");
			}
		}
		SortResearchVector(_discovered);

		// Research Diary
		{
			string name = "";
			foreach (var researchDiaryEntryReader in reader["researchDiary"].Children())
			{
				researchDiaryEntryReader.ReadNode("name", ref name);
				// only valid topics are loaded
				if (mod.GetResearch(name, false) is RuleResearch research)
				{
					var entry = new ResearchDiaryEntry(research);
					entry.Load(researchDiaryEntryReader, mod);
					_researchDiary.Add(entry);
				}
			}
		}

		TryReadMap(reader, "generatedEvents", ref _generatedEvents);
		LoadUfopediaRuleStatus(reader["ufopediaRuleStatus"]);
		TryReadMap(reader, "manufactureRuleStatus", ref _manufactureRuleStatus);
		TryReadMap(reader, "researchRuleStatus", ref _researchRuleStatus);
		TryReadMap(reader, "monthlyPurchaseLimitLog", ref _monthlyPurchaseLimitLog);
		TryReadMap(reader, "hiddenPurchaseItems", ref _hiddenPurchaseItemsMap);
		TryReadMap(reader, "customRuleCraftDeployments", ref _customRuleCraftDeployments);

		foreach (var @base in reader["bases"].Children())
		{
			var b = new Base(mod);
			b.Load(@base, this, false);
			_bases.Add(b);
		}

		// Finish loading crafts after bases (more specifically after all crafts) are loaded, because of references between crafts (i.e. friendly escorts)
		for (int i = 0; i < _bases.Count; ++i)
			_bases[i].FinishLoading(reader["bases"][i], this);

		// Finish loading UFOs after all craft and all other UFOs are loaded
		foreach (var ufoReader in reader["ufos"].Children())
		{
			int uniqueUfoId = ufoReader["uniqueId"].ReadVal(0);
			if (uniqueUfoId > 0)
			{
				Ufo ufo = null;
				foreach (var u in _ufos)
				{
					if (u.UniqueId == uniqueUfoId)
					{
						ufo = u;
						break;
					}
				}
				ufo?.FinishLoading(ufoReader, this);
			}
		}

		foreach (var popped in reader["poppedResearch"].Children())
		{
			string id = popped.ReadVal<string>();
			if (mod.GetResearch(id) != null)
			{
				_poppedResearch.Add(mod.GetResearch(id));
			}
			else
			{
				Logger.Error($"Failed to load popped research {id}");
			}
		}
		_alienStrategy.Load(reader["alienStrategy"], mod);

		foreach (var weHardlyKnewYe in reader["deadSoldiers"].Children())
		{
			string type = weHardlyKnewYe["type"].ReadVal(mod.SoldiersList[0]);
			if (mod.GetSoldier(type) != null)
			{
				var soldier = new Soldier(mod.GetSoldier(type), null, 0 /*nationality*/);
				soldier.Load(weHardlyKnewYe, mod, this);
				_deadSoldiers.Add(soldier);
			}
			else
			{
				Logger.Error($"Failed to load dead soldier {type}");
			}
		}

		LoadTemplates(reader, mod);

		foreach (var missionStats in reader["missionStatistics"].Children())
		{
			var ms = new MissionStatistics();
			ms.Load(missionStats);
			_missionStatistics.Add(ms);
		}

		foreach (var autoSale in reader["autoSales"].Children())
		{
			string itype = autoSale.ReadVal<string>();
			if (mod.GetItem(itype) != null)
			{
				_autosales.Add(mod.GetItem(itype));
			}
		}

		if (reader["battleGame"] is { IsValid: true } battle)
		{
			_battleGame = new SavedBattleGame(mod, lang);
			_battleGame.Load(battle, mod, this);
		}

		// Script hook not ported: _scriptValues.load(reader, mod->getScriptGlobal())
	}

	/// <summary>Loads the global equipment layouts and craft loadouts.</summary>
	public void LoadTemplates(YamlNodeReader reader, Mod mod)
	{
		for (int j = 0; j < Options.oxceMaxEquipmentLayoutTemplates; ++j)
		{
			string suffix = j.ToString(CultureInfo.InvariantCulture);
			foreach (var layout in reader["globalEquipmentLayout" + suffix].Children())
			{
				try
				{
					_globalEquipmentLayout[j].Add(new EquipmentLayoutItem(layout, mod));
				}
				catch (InvalidOperationException ex)
				{
					Logger.Error($"Error loading Layout: {ex.Message}");
				}
			}
			if (reader["globalEquipmentLayoutName" + suffix] is { IsValid: true } layoutName)
				_globalEquipmentLayoutName[j] = layoutName.ReadVal<string>();
			if (reader["globalEquipmentLayoutArmor" + suffix] is { IsValid: true } layoutArmor)
				_globalEquipmentLayoutArmor[j] = layoutArmor.ReadVal<string>();
		}

		for (int j = 0; j < MAX_CRAFT_LOADOUT_TEMPLATES; ++j)
		{
			string suffix = j.ToString(CultureInfo.InvariantCulture);
			if (reader["globalCraftLoadout" + suffix] is { IsValid: true } loadout)
				_globalCraftLoadout[j].Load(loadout, mod);
			if (reader["globalCraftLoadoutName" + suffix] is { IsValid: true } loadoutName)
				_globalCraftLoadoutName[j] = loadoutName.ReadVal<string>();
		}
	}

	/// <summary>Loads the ufopedia rule status map from a node.</summary>
	public void LoadUfopediaRuleStatus(YamlNodeReader reader)
	{
		SortedDictionary<string, int> read = null;
		if (reader.TryReadVal(ref read))
			_ufopediaRuleStatus = new SortedDictionary<string, int>(read, StringComparer.Ordinal);
	}

	private static void SaveVector<T>(YamlNodeWriter writer, List<T> vector, string key, Action<T, YamlNodeWriter> save)
	{
		if (vector.Count == 0)
			return;
		YamlNodeWriter sequenceWriter = writer[key];
		sequenceWriter.SetAsSeq();
		foreach (T item in vector)
			save(item, sequenceWriter.Write());
	}

	/// <summary>Saves a saved game's contents to a YAML file.</summary>
	/// <param name="filename">YAML filename.</param>
	/// <param name="mod">Mod for the saved game (only used by script hooks in C++).</param>
	public void Save(string filename, Mod mod)
	{
		var headerWriter = new YamlRootNodeWriter();
		headerWriter.SetAsMap();
		// Saves the brief game info used in the saves list

		headerWriter.Write("name", _name);
		headerWriter.Write("version", Version.OPENXCOM_VERSION_SHORT);
		headerWriter.Write("engine", Version.OPENXCOM_VERSION_ENGINE);
		// C++ writes OPENXCOM_VERSION_GIT (the build's git description); the port writes its own version
		headerWriter.Write("build", "ACOM " + Version.ACOM_VERSION);
		_time.Save(headerWriter["time"]);
		if (_battleGame != null)
		{
			headerWriter.Write("mission", _battleGame.MissionType);
			headerWriter.Write("target", _battleGame.MissionTarget);
			headerWriter.Write("craftOrBase", _battleGame.MissionCraftOrBase).SetAsQuotedAndEscaped();
			headerWriter.Write("turn", _battleGame.Turn);
		}

		// only save mods that work with the current master
		var modsList = new List<string>();
		foreach (var modInfo in Options.GetActiveMods())
			modsList.Add(modInfo.Id + " ver: " + modInfo.Version);
		headerWriter.Write("mods", modsList);

		if (_ironman)
			headerWriter.Write("ironman", _ironman);

		// Saves the full game data to the save
		var writer = new YamlRootNodeWriter();
		writer.SetAsMap();
		writer.Write("difficulty", _difficulty);
		writer.Write("end", _end);
		writer.Write("monthsPassed", _monthsPassed);
		writer.Write("daysPassed", _daysPassed);
		writer.Write("vehiclesLost", _vehiclesLost);
		writer.Write("craftLostDogfight", _craftLostDogfight);
		writer.Write("craftLostMission", _craftLostMission);
		writer.Write("graphRegionToggles", _graphRegionToggles);
		writer.Write("graphCountryToggles", _graphCountryToggles);
		writer.Write("graphFinanceToggles", _graphFinanceToggles);
		writer.Write("rng", Rng.Seed);
		writer.Write("funds", _funds);
		writer.Write("maintenance", _maintenance);
		writer.Write("userNotes", _userNotes);
		if (Options.oxceGeoscapeDebugLogMaxEntries > 0 && _geoscapeDebugLog.Count > 0)
		{
			var geoDebugLog = writer["geoscapeDebugLog"];
			geoDebugLog.SetAsSeq();
			int lastEntriesToWrite = Math.Min(_geoscapeDebugLog.Count, Options.oxceGeoscapeDebugLogMaxEntries);
			for (int j = _geoscapeDebugLog.Count - lastEntriesToWrite; j < _geoscapeDebugLog.Count; ++j)
				geoDebugLog.Write(_geoscapeDebugLog[j]);
		}

		writer.Write("researchScores", _researchScores);
		writer.Write("incomes", _incomes);
		writer.Write("expenditures", _expenditures);
		writer.Write("warned", _warned);
		writer.Write("togglePersonalLight", _togglePersonalLight);
		writer.Write("toggleNightVision", _toggleNightVision);
		writer.Write("toggleBrightness", _toggleBrightness);
		writer.Write("globeLon", _globeLon);
		writer.Write("globeLat", _globeLat);
		writer.Write("globeZoom", _globeZoom);
		writer.Write("ids", _ids);

		SaveVector(writer, _countries, "countries", (c, w) => c.Save(w));
		SaveVector(writer, _regions, "regions", (r, w) => r.Save(w));
		SaveVector(writer, _bases, "bases", (b, w) => b.Save(w));
		SaveVector(writer, _waypoints, "waypoints", (wp, w) => wp.Save(w));
		SaveVector(writer, _missionSites, "missionSites", (m, w) => m.Save(w));
		// Alien bases must be saved before alien missions.
		SaveVector(writer, _alienBases, "alienBases", (ab, w) => ab.Save(w));
		// Missions must be saved before UFOs, but after alien bases.
		SaveVector(writer, _activeMissions, "alienMissions", (am, w) => am.Save(w));
		// UFOs must be after missions
		bool newBattle = MonthsPassed == -1;
		SaveVector(writer, _ufos, "ufos", (u, w) => u.Save(w, newBattle));
		SaveVector(writer, _geoscapeEvents, "geoscapeEvents", (ge, w) => ge.Save(w));
		if (_discovered.Count != 0)
		{
			var discoveredWriter = writer["discovered"];
			discoveredWriter.SetAsSeq();
			{
				var discoveredCopy = new List<RuleResearch>(_discovered);
				discoveredCopy.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
				foreach (var research in discoveredCopy)
				{
					discoveredWriter.Write(research.Name);
				}
			}
		}
		SaveVector(writer, _researchDiary, "researchDiary", (rde, w) => rde.Save(w));
		writer.Write("poppedResearch", _poppedResearch, (w, r) => w.Write(r.Name));
		writer.Write("generatedEvents", _generatedEvents);
		writer.Write("ufopediaRuleStatus", _ufopediaRuleStatus);
		writer.Write("manufactureRuleStatus", _manufactureRuleStatus);
		writer.Write("researchRuleStatus", _researchRuleStatus);
		writer.Write("monthlyPurchaseLimitLog", _monthlyPurchaseLimitLog);
		writer.Write("hiddenPurchaseItems", _hiddenPurchaseItemsMap);
		writer.Write("customRuleCraftDeployments", _customRuleCraftDeployments);
		_alienStrategy.Save(writer["alienStrategy"]);

		SaveVector(writer, _deadSoldiers, "deadSoldiers", (s, w) => s.Save(w));
		for (int j = 0; j < Options.oxceMaxEquipmentLayoutTemplates; ++j)
		{
			string suffix = j.ToString(CultureInfo.InvariantCulture);
			if (_globalEquipmentLayout[j].Count != 0)
				SaveVector(writer, _globalEquipmentLayout[j], "globalEquipmentLayout" + suffix, (e, w) => e.Save(w));
			if (_globalEquipmentLayoutName[j].Length != 0)
				writer.Write("globalEquipmentLayoutName" + suffix, _globalEquipmentLayoutName[j]);
			if (_globalEquipmentLayoutArmor[j].Length != 0)
				writer.Write("globalEquipmentLayoutArmor" + suffix, _globalEquipmentLayoutArmor[j]);
		}
		for (int j = 0; j < MAX_CRAFT_LOADOUT_TEMPLATES; ++j)
		{
			string suffix = j.ToString(CultureInfo.InvariantCulture);
			if (_globalCraftLoadout[j].Contents.Count != 0)
				_globalCraftLoadout[j].Save(writer["globalCraftLoadout" + suffix]);
			if (_globalCraftLoadoutName[j].Length != 0)
				writer.Write("globalCraftLoadoutName" + suffix, _globalCraftLoadoutName[j]);
		}
		if (Options.soldierDiaries)
			SaveVector(writer, _missionStatistics, "missionStatistics", (ms, w) => ms.Save(w));

		if (_autosales.Count != 0)
		{
			var autoSales = writer["autoSales"];
			autoSales.SetAsSeq();
			{
				var autosalesVector = new List<RuleItem>(_autosales);
				autosalesVector.Sort((a, b) => string.CompareOrdinal(a.Type, b.Type));
				foreach (var sale in autosalesVector)
				{
					autoSales.Write(sale.Type);
				}
			}
		}
		// snapshot of the user options (just for debugging purposes)
		var optionsWriter = writer["options"];
		optionsWriter.SetAsMap();
		foreach (var optionInfo in Options.GetOptionInfo())
			optionInfo.Save(optionsWriter);

		_battleGame?.Save(writer["battleGame"]);
		// Script hook not ported: _scriptValues.save(writer.toBase(), mod->getScriptGlobal())

		// concatenate header + separator + body
		// per yaml standard, "bare documents" in a yaml "stream" can be separated by either a "document end" or "directives end" marker line
		string headerString = headerWriter.Emit();
		const string directivesEndMarker = "---\n";
		string bodyString = writer.Emit();
		string finalString = headerString + directivesEndMarker + bodyString;

		string filepath = Options.GetMasterUserFolder() + filename;
		if (!CrossPlatform.WriteFile(filepath, finalString))
		{
			throw new InvalidOperationException("Failed to save " + filepath);
		}
	}

	/// <summary>The game's name shown in Save screens.</summary>
	public string Name
	{
		get => _name;
		set => _name = value;
	}

	/// <summary>The game's difficulty level.</summary>
	public GameDifficulty Difficulty
	{
		get => _difficulty;
		set => _difficulty = value;
	}

	/// <summary>The game's difficulty coefficient based on the current level.</summary>
	public int DifficultyCoefficient => Mod.DIFFICULTY_COEFFICIENT[Math.Min((int)_difficulty, 4)];

	/// <summary>The game's sell price coefficient based on the current difficulty level.</summary>
	public int SellPriceCoefficient => Mod.SELL_PRICE_COEFFICIENT[Math.Min((int)_difficulty, 4)];

	/// <summary>The game's buy price coefficient based on the current difficulty level.</summary>
	public int BuyPriceCoefficient => Mod.BUY_PRICE_COEFFICIENT[Math.Min((int)_difficulty, 4)];

	/// <summary>The game's current ending.</summary>
	public GameEnding Ending
	{
		get => _end;
		set => _end = value;
	}

	/// <summary>Returns if the game is set to ironman mode. Ironman games cannot be manually saved.</summary>
	public bool IsIronman => _ironman;

	/// <summary>Changes if the game is set to ironman mode. Ironman games cannot be manually saved.</summary>
	public void SetIronman(bool ironman)
	{
		_ironman = ironman;
	}

	/// <summary>The player's current funds; setting it books the difference as income or expenditure.</summary>
	public long Funds
	{
		get => _funds[^1];
		set
		{
			if (_funds[^1] > value)
			{
				_expenditures[^1] += _funds[^1] - value;
			}
			else
			{
				_incomes[^1] += value - _funds[^1];
			}
			_funds[^1] = value;
		}
	}

	/// <summary>The player's funds for the last 12 months.</summary>
	public List<long> FundsList => _funds;

	/// <summary>The current longitude of the Geoscape globe.</summary>
	public double GlobeLongitude
	{
		get => _globeLon;
		set => _globeLon = value;
	}

	/// <summary>The current latitude of the Geoscape globe.</summary>
	public double GlobeLatitude
	{
		get => _globeLat;
		set => _globeLat = value;
	}

	/// <summary>The current zoom level of the Geoscape globe.</summary>
	public int GlobeZoom
	{
		get => _globeZoom;
		set => _globeZoom = value;
	}

	/// <summary>Gives the player his monthly funds, taking in account all maintenance and profit costs.</summary>
	public void MonthlyFunding()
	{
		int countryFunding = CountryFunding;
		int baseMaintenance = BaseMaintenance;
		_funds[^1] += countryFunding - baseMaintenance;
		_funds.Add(_funds[^1]);
		_maintenance[^1] = baseMaintenance;
		_maintenance.Add(0);
		_incomes.Add(countryFunding);
		_expenditures.Add(baseMaintenance);
		_researchScores.Add(0);

		if (_incomes.Count > 12)
			_incomes.RemoveAt(0);
		if (_expenditures.Count > 12)
			_expenditures.RemoveAt(0);
		if (_researchScores.Count > 12)
			_researchScores.RemoveAt(0);
		if (_funds.Count > 12)
			_funds.RemoveAt(0);
		if (_maintenance.Count > 12)
			_maintenance.RemoveAt(0);
	}

	/// <summary>The current time of the game; setting it stores a copy.</summary>
	public GameTime Time
	{
		get => _time;
		set => _time = new GameTime(value);
	}

	/// <summary>Returns the latest ID for the specified object and increases it.</summary>
	/// <param name="name">Object name.</param>
	/// <returns>Latest ID number.</returns>
	public int GetId(string name)
	{
		if (_ids.TryGetValue(name, out int id))
		{
			_ids[name] = id + 1;
			return id;
		}
		else
		{
			_ids[name] = 1;
			id = _ids[name];
			_ids[name] = id + 1;
			return id;
		}
	}

	/// <summary>Returns the last used ID for the specified object.</summary>
	/// <param name="name">Object name.</param>
	/// <returns>Last used ID number.</returns>
	public int GetLastId(string name)
	{
		if (_ids.TryGetValue(name, out int id))
		{
			return Math.Max(1, id - 1);
		}
		else
		{
			return 0;
		}
	}

	/// <summary>Increase a custom counter.</summary>
	/// <param name="name">Counter name.</param>
	public void IncreaseCustomCounter(string name)
	{
		if (!string.IsNullOrEmpty(name))
		{
			if (_ids.TryGetValue(name, out int value))
			{
				_ids[name] = value + 1;
			}
			else
			{
				_ids[name] = 2; // not a typo
			}
		}
	}

	/// <summary>Decrease a custom counter.</summary>
	/// <param name="name">Counter name.</param>
	public void DecreaseCustomCounter(string name)
	{
		if (!string.IsNullOrEmpty(name))
		{
			if (_ids.TryGetValue(name, out int value))
			{
				// don't go below "zero" (which is saved as one)
				if (value > 1)
				{
					_ids[name] = value - 1;
				}
			}
			else
			{
				_ids[name] = 1; // not a typo
			}
		}
	}

	/// <summary>The list of unique object IDs; setting it stores a copy.</summary>
	public SortedDictionary<string, int> AllIds
	{
		get => _ids;
		set => _ids = new SortedDictionary<string, int>(value, StringComparer.Ordinal);
	}

	/// <summary>The list of countries in the game world.</summary>
	public List<Country> Countries => _countries;

	/// <summary>Adds up the monthly funding of all the countries.</summary>
	public int CountryFunding
	{
		get
		{
			int total = 0;
			foreach (var country in _countries)
			{
				total += country.Funding[^1];
			}
			return total;
		}
	}

	/// <summary>The list of world regions.</summary>
	public List<Region> Regions => _regions;

	/// <summary>The list of player bases.</summary>
	public List<Base> Bases => _bases;

	/// <summary>The last selected player base (the first one if the selection is out of range).</summary>
	public Base SelectedBase
	{
		get
		{
			// in case a base was destroyed or something...
			if ((uint)_selectedBase < (uint)_bases.Count)
			{
				return _bases[_selectedBase];
			}
			else
			{
				return _bases[0];
			}
		}
	}

	/// <summary>Sets the last selected player base.</summary>
	/// <param name="base">Number of the base.</param>
	public void SetSelectedBase(int @base)
	{
		_selectedBase = @base;
	}

	/// <summary>Adds up the monthly maintenance of all the bases.</summary>
	public int BaseMaintenance
	{
		get
		{
			int total = 0;
			foreach (var xbase in _bases)
			{
				total += xbase.MonthlyMaintenace;
			}
			return total;
		}
	}

	/// <summary>The list of alien UFOs.</summary>
	public List<Ufo> Ufos => _ufos;

	/// <summary>The list of craft waypoints.</summary>
	public List<Waypoint> Waypoints => _waypoints;

	/// <summary>The list of mission sites.</summary>
	public List<MissionSite> MissionSites => _missionSites;

	/// <summary>The current battle game.</summary>
	public SavedBattleGame SavedBattle => _battleGame;

	/// <summary>Set battleGame object (replaces the old one).</summary>
	public void SetBattleGame(SavedBattleGame battleGame)
	{
		_battleGame = battleGame;
	}

	/// <summary>Sets the status of a ufopedia rule.</summary>
	public void SetUfopediaRuleStatus(string ufopediaRule, int newStatus)
	{
		_ufopediaRuleStatus[ufopediaRule] = newStatus;
	}

	/// <summary>Sets the status of a manufacture rule.</summary>
	public void SetManufactureRuleStatus(string manufactureRule, int newStatus)
	{
		_manufactureRuleStatus[manufactureRule] = newStatus;
	}

	/// <summary>Sets the status of a research rule.</summary>
	public void SetResearchRuleStatus(string researchRule, int newStatus)
	{
		_researchRuleStatus[researchRule] = newStatus;
	}

	/// <summary>Sets the hidden status of a purchase item.</summary>
	public void SetHiddenPurchaseItemsStatus(string itemName, bool hidden)
	{
		_hiddenPurchaseItemsMap[itemName] = hidden;
	}

	/// <summary>The map of hidden items.</summary>
	public SortedDictionary<string, bool> HiddenPurchaseItems => _hiddenPurchaseItemsMap;

	/// <summary>Selects a "getOneFree" topic for the given research rule.</summary>
	/// <param name="research">The given research rule.</param>
	/// <returns>The selected getOneFree topic; null, if nothing was selected.</returns>
	public RuleResearch SelectGetOneFree(RuleResearch research)
	{
		ArgumentNullException.ThrowIfNull(research);
		if (research.GetOneFree.Count != 0 || research.GetOneFreeProtected.Count != 0)
		{
			var possibilities = new List<RuleResearch>();
			foreach (var free in research.GetOneFree)
			{
				if (IsResearchRuleStatusDisabled(free.Name))
				{
					continue; // skip disabled topics
				}
				if (!IsResearched(free, false))
				{
					possibilities.Add(free);
				}
			}
			foreach (var pair in research.GetOneFreeProtected)
			{
				if (IsResearched(pair.first, false))
				{
					foreach (var res in pair.second)
					{
						if (IsResearchRuleStatusDisabled(res.Name))
						{
							continue; // skip disabled topics
						}
						if (!IsResearched(res, false))
						{
							possibilities.Add(res);
						}
					}
				}
			}
			if (possibilities.Count != 0)
			{
				int pick = 0;
				if (!research.SequentialGetOneFree())
				{
					pick = Rng.Generate(0, possibilities.Count - 1);
				}
				var ret = possibilities[pick];
				return ret;
			}
		}
		return null;
	}

	/// <summary>Checks for and removes a research project from the "already discovered" list.</summary>
	public void RemoveDiscoveredResearch(RuleResearch research)
	{
		_discovered.Remove(research);
	}

	/// <summary>Make all research discovered (used in New Battle).</summary>
	public void MakeAllResearchDiscovered(Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		foreach (var pair in mod.ResearchMap)
		{
			_discovered.Add(pair.Value);
		}
		SortResearchVector(_discovered);
	}

	/// <summary>Add a ResearchProject to the list of already discovered ResearchProject.</summary>
	/// <param name="research">The newly found ResearchProject.</param>
	/// <param name="mod">The game Mod.</param>
	/// <param name="base">The base, in which the project was finished.</param>
	/// <param name="score">Should the score be awarded or not?</param>
	public void AddFinishedResearch(RuleResearch research, Mod mod, Base @base, bool score = true)
	{
		ArgumentNullException.ThrowIfNull(research);
		if (IsResearchRuleStatusDisabled(research.Name))
		{
			return;
		}

		// Not really a queue in C++ terminology (we don't need or want pop_front())
		var queue = new List<RuleResearch>();
		queue.Add(research);

		int currentQueueIndex = 0;
		while (queue.Count > currentQueueIndex)
		{
			RuleResearch currentQueueItem = queue[currentQueueIndex];

			// 1. Find out and remember if the currentQueueItem has any undiscovered non-disabled "protected unlocks" or "getOneFree"
			bool hasUndiscoveredProtectedUnlocks = HasUndiscoveredProtectedUnlock(currentQueueItem);
			bool hasAnyUndiscoveredGetOneFrees = HasUndiscoveredGetOneFree(currentQueueItem, false);

			// 2. If the currentQueueItem was *not* already discovered before, add it to discovered research
			bool checkRelatedZeroCostTopics = true;
			if (!IsResearched(currentQueueItem, false))
			{
				if (!research.IsRepeatable)
				{
					_discovered.Add(currentQueueItem);
					SortResearchVector(_discovered);
				}

				if (currentQueueItem != research)
				{
					var entry = new ResearchDiaryEntry(currentQueueItem);
					entry.SetDate(_time);
					entry.source.type = DiscoverySourceType.FREE_AFTER;
					entry.source.research = research;
					entry.source.name = research.Name;
					AddResearchDiaryEntry(entry);
				}

				if (!hasUndiscoveredProtectedUnlocks && !hasAnyUndiscoveredGetOneFrees)
				{
					// If the currentQueueItem can't tell you anything anymore, remove it from popped research
					// Note: this is for optimisation purposes only, functionally it is *not* required...
					// ... removing it prematurely leads to bugs, maybe we should not do it at all?
					RemovePoppedResearch(currentQueueItem);
				}
				if (score)
				{
					AddResearchScore(currentQueueItem.Points);
				}
				// process "disables"
				foreach (var dis in currentQueueItem.Disabled)
				{
					RemoveDiscoveredResearch(dis); // unresearch
					SetResearchRuleStatus(dis.Name, RuleResearch.RESEARCH_STATUS_DISABLED); // mark as permanently disabled
				}
			}
			else
			{
				// If the currentQueueItem *was* already discovered before, check if it has any undiscovered "protected unlocks".
				// If not, all zero-cost topics have already been processed before (during the first discovery)
				// and we can basically terminate here (i.e. skip step 3.).
				if (!hasUndiscoveredProtectedUnlocks)
				{
					checkRelatedZeroCostTopics = false;
				}
			}

			// process "re-enables": https://openxcom.org/forum/index.php?topic=12071.0
			foreach (var ree in currentQueueItem.Reenabled)
			{
				if (IsResearchRuleStatusDisabled(ree.Name))
				{
					SetResearchRuleStatus(ree.Name, RuleResearch.RESEARCH_STATUS_NEW); // reset status
				}
			}

			// 3. If currentQueueItem is completed for the *first* time, or if it has any undiscovered "protected unlocks",
			// process all related zero-cost topics
			if (checkRelatedZeroCostTopics)
			{
				// 3a. Gather all available research projects
				var availableResearch = new List<RuleResearch>();
				if (@base != null)
				{
					// Note: even if two different but related projects are finished in two different bases at the same time,
					// the algorithm is robust enough to treat them *sequentially* (i.e. as if one was researched first and the other second),
					// thus calling this method for *one* base only is enough
					GetAvailableResearchProjects(availableResearch, mod, @base);
				}
				else
				{
					// Used in vanilla save converter only
					GetAvailableResearchProjects(availableResearch, mod, null);
				}

				// 3b. Iterate through all available projects and add zero-cost projects to the processing queue
				foreach (var projectToTest in availableResearch)
				{
					// We are only interested in zero-cost projects!
					if (projectToTest.Cost == 0)
					{
						// We are only interested in *new* projects (i.e. not processed or scheduled for processing yet)
						bool isAlreadyInTheQueue = false;
						foreach (var res in queue)
						{
							if (res.Name == projectToTest.Name)
							{
								isAlreadyInTheQueue = true;
								break;
							}
						}

						if (!isAlreadyInTheQueue)
						{
							if (projectToTest.Requirements.Count == 0)
							{
								// no additional checks for "unprotected" topics
								queue.Add(projectToTest);
							}
							else
							{
								// for "protected" topics, we need to check if the currentQueueItem can unlock it or not
								foreach (var unl in currentQueueItem.Unlocked)
								{
									if (projectToTest == unl)
									{
										queue.Add(projectToTest);
										break;
									}
								}
							}
						}
					}
				}
			}

			// 4. process remaining items in the queue
			++currentQueueIndex;
		}
	}

	/// <summary>Add a new record to the research diary.</summary>
	public void AddResearchDiaryEntry(ResearchDiaryEntry entry)
	{
		_researchDiary.Add(entry);
	}

	/// <summary>Gets the research diary.</summary>
	public List<ResearchDiaryEntry> ResearchDiary => _researchDiary;

	/// <summary>The list of already discovered ResearchProject.</summary>
	public List<RuleResearch> DiscoveredResearch => _discovered;

	/// <summary>Does this item correspond to at least one research topic that can be researched now or in the future?</summary>
	public bool IsResearchable(RuleItem item, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		foreach (var pair in mod.ResearchMap)
		{
			if (pair.Value.NeedItem() && pair.Value.NeededItem == item)
			{
				// This research topic is "permanently" disabled, ignore it!
				if (IsResearchRuleStatusDisabled(pair.Key))
				{
					continue;
				}

				if (IsResearched(pair.Value, false))
				{
					if (HasUndiscoveredGetOneFree(pair.Value, false))
					{
						// This research topic still has some more undiscovered non-disabled "getOneFree" topics, keep it!
						return true;
					}
					else if (HasUndiscoveredProtectedUnlock(pair.Value))
					{
						// This research topic still has one or more undiscovered non-disabled "protected unlocks", keep it!
						return true;
					}
					else
					{
						// This topic can't give you anything else anymore, ignore it!
						continue;
					}
				}
				else
				{
					// This research topic is not yet researched, keep it!
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>C++ <c>(~provided &amp; required).any()</c> on base function bitsets.</summary>
	private static bool AnyMissingBaseFunc(System.Collections.BitArray provided, System.Collections.BitArray required)
	{
		for (int i = 0; i < required.Length; ++i)
		{
			if (required[i] && !(i < provided.Length && provided[i]))
				return true;
		}
		return false;
	}

	/// <summary>Get the list of RuleResearch which can be researched in a Base.</summary>
	/// <param name="projects">The list of ResearchProject which are available.</param>
	/// <param name="mod">The game Mod.</param>
	/// <param name="base">A pointer to a Base.</param>
	/// <param name="considerDebugMode">Should debug mode be considered or not.</param>
	public void GetAvailableResearchProjects(List<RuleResearch> projects, Mod mod, Base @base, bool considerDebugMode = false)
	{
		ArgumentNullException.ThrowIfNull(projects);
		ArgumentNullException.ThrowIfNull(mod);
		// This list is used for topics that can be researched even if *not all* dependencies have been discovered yet (e.g. STR_ALIEN_ORIGINS)
		// Note: all requirements of such topics *have to* be discovered though! This will be handled elsewhere.
		var unlocked = new List<RuleResearch>();
		foreach (var research in _discovered)
		{
			foreach (var unl in research.Unlocked)
			{
				unlocked.Add(unl);
			}
			SortResearchVector(unlocked);
		}

		// Create a list of research topics available for research in the given base
		foreach (var pair in mod.ResearchMap)
		{
			// This research topic is permanently disabled, ignore it!
			if (IsResearchRuleStatusDisabled(pair.Key))
			{
				continue;
			}

			RuleResearch research = pair.Value;

			if ((considerDebugMode && _debug) || HaveResearchVector(unlocked, research))
			{
				// Empty, these research topics are on the "unlocked list", *don't* check the dependencies!
			}
			else
			{
				// These items are not on the "unlocked list", we must check if "dependencies" are satisfied!
				if (!IsResearched(research.Dependencies, considerDebugMode))
				{
					continue;
				}
			}

			// Check if "requires" are satisfied
			// IMPORTANT: research topics with "requires" will NEVER be directly visible to the player anyway
			//   - there is an additional filter in NewResearchListState::fillProjectList(), see comments there for more info
			//   - there is an additional filter in NewPossibleResearchState::NewPossibleResearchState()
			//   - we do this check for other functionality using this method, namely SavedGame::addFinishedResearch()
			//     - Note: when called from there, parameter considerDebugMode = false
			if (!IsResearched(research.Requirements, considerDebugMode))
			{
				continue;
			}

			// Remove the already researched topics from the list *UNLESS* they can still give you something more
			if (IsResearched(research.Name, false))
			{
				if (HasUndiscoveredGetOneFree(research, true))
				{
					// This research topic still has some more undiscovered non-disabled and *AVAILABLE* "getOneFree" topics, keep it!
				}
				else if (HasUndiscoveredProtectedUnlock(research))
				{
					// This research topic still has one or more undiscovered non-disabled "protected unlocks", keep it!
				}
				else
				{
					// This topic can't give you anything else anymore, ignore it!
					continue;
				}
			}

			if (@base != null)
			{
				// Check if this topic is already being researched in the given base
				bool found = false;
				foreach (var ongoing in @base.Research)
				{
					if (ongoing.Rules == research)
					{
						found = true;
						break;
					}
				}
				if (found)
				{
					continue;
				}

				// Check for needed item in the given base
				if (research.NeedItem() && @base.StorageItems.GetItem(research.NeededItem) == 0)
				{
					continue;
				}

				// Check for required buildings/functions in the given base
				if (AnyMissingBaseFunc(@base.GetProvidedBaseFunc(default), research.RequireBaseFunc))
				{
					continue;
				}
			}
			else
			{
				// Used in vanilla save converter only
				if (research.NeedItem() && research.Cost == 0)
				{
					continue;
				}
			}

			// Hallelujah, all checks passed, add the research topic to the list
			projects.Add(research);
		}
	}

	/// <summary>Get the list of newly available research projects once a ResearchProject has been completed.</summary>
	/// <param name="before">The list of available RuleResearch before completing new research.</param>
	/// <param name="after">The list of available RuleResearch after completing new research.</param>
	/// <param name="diff">The list of newly available RuleResearch after completing new research (after - before).</param>
#pragma warning disable CA1822 // instance method in C++, kept for callers
	public void GetNewlyAvailableResearchProjects(List<RuleResearch> before, List<RuleResearch> after, List<RuleResearch> diff)
#pragma warning restore CA1822
	{
		ArgumentNullException.ThrowIfNull(before);
		ArgumentNullException.ThrowIfNull(after);
		ArgumentNullException.ThrowIfNull(diff);
		// History lesson:
		// Completely rewritten the original recursive algorithm, because it was inefficient, unreadable and wrong
		// a/ inefficient: it could call SavedGame::getAvailableResearchProjects() way too many times
		// b/ unreadable: because of recursion
		// c/ wrong: could end in an endless loop! in two different ways! (not in vanilla, but in mods)

		// Note:
		// We could move the sorting of "before" vector right after its creation to optimize a little bit more.
		// But sorting a short list is negligible compared to other operations we had to do to get to this point.
		// So I decided to leave it here, so that it's 100% clear what's going on.
		SortResearchVector(before);
		SortResearchVector(after);
		// std::set_difference(after, before) through std::inserter(diff, diff.begin())
		var result = new List<RuleResearch>();
		int i = 0, j = 0;
		while (i < after.Count)
		{
			if (j == before.Count)
			{
				result.AddRange(after.GetRange(i, after.Count - i));
				break;
			}
			if (ResearchLess(after[i], before[j]))
			{
				result.Add(after[i]);
				++i;
			}
			else
			{
				if (!ResearchLess(before[j], after[i]))
				{
					++i;
				}
				++j;
			}
		}
		diff.InsertRange(0, result);
	}

	/// <summary>Get the list of RuleManufacture which can be manufacture in a Base.</summary>
	/// <param name="productions">The list of Productions which are available.</param>
	/// <param name="mod">The Game Mod.</param>
	/// <param name="base">A pointer to a Base.</param>
	/// <param name="filter">Filter for facility requirements.</param>
	public void GetAvailableProductions(List<RuleManufacture> productions, Mod mod, Base @base, ManufacturingFilterType filter = ManufacturingFilterType.MANU_FILTER_DEFAULT)
	{
		ArgumentNullException.ThrowIfNull(productions);
		ArgumentNullException.ThrowIfNull(mod);
		ArgumentNullException.ThrowIfNull(@base);
		var baseProductions = @base.Productions;
		var baseFunc = @base.GetProvidedBaseFunc(default);

		foreach (var manuf in mod.ManufactureList)
		{
			RuleManufacture m = mod.GetManufacture(manuf);
			if (!IsResearched(m.Requirements))
			{
				continue;
			}
			bool found = false;
			foreach (var ongoing in baseProductions)
			{
				if (ongoing.Rules == m)
				{
					found = true;
					break;
				}
			}
			if (found)
			{
				continue;
			}
			if (AnyMissingBaseFunc(baseFunc, m.RequireBaseFunc))
			{
				if (filter != ManufacturingFilterType.MANU_FILTER_FACILITY_REQUIRED)
					continue;
			}
			else
			{
				if (filter == ManufacturingFilterType.MANU_FILTER_FACILITY_REQUIRED)
					continue;
			}

			productions.Add(m);
		}
	}

	/// <summary>
	/// Get the list of newly available manufacture projects once a ResearchProject has been completed.
	/// This function check for fake ResearchProject.
	/// </summary>
	/// <param name="dependables">The list of RuleManufacture which are now available.</param>
	/// <param name="research">The RuleResearch which has just been discovered.</param>
	/// <param name="mod">The Game Mod.</param>
	/// <param name="base">A pointer to a Base (unused).</param>
	public void GetDependableManufacture(List<RuleManufacture> dependables, RuleResearch research, Mod mod, Base @base)
	{
		ArgumentNullException.ThrowIfNull(dependables);
		ArgumentNullException.ThrowIfNull(mod);
		foreach (var manuf in mod.ManufactureList)
		{
			// don't show previously unlocked (and seen!) manufacturing topics
			if (_manufactureRuleStatus.TryGetValue(manuf, out int status))
			{
				if (status != RuleManufacture.MANU_STATUS_NEW)
					continue;
			}

			RuleManufacture m = mod.GetManufacture(manuf);
			var reqs = m.Requirements;
			if (IsResearched(reqs) && reqs.Contains(research))
			{
				dependables.Add(m);
			}
		}
	}

	/// <summary>Get the list of RuleSoldierTransformation which can occur at a base.</summary>
	/// <param name="transformations">The list of Transformations which are available.</param>
	/// <param name="mod">The Game Mod.</param>
	/// <param name="base">A pointer to a Base.</param>
	public void GetAvailableTransformations(List<RuleSoldierTransformation> transformations, Mod mod, Base @base)
	{
		ArgumentNullException.ThrowIfNull(transformations);
		ArgumentNullException.ThrowIfNull(mod);
		ArgumentNullException.ThrowIfNull(@base);
		var list = mod.SoldierTransformationList;
		if (list.Count == 0)
		{
			return;
		}

		var baseFunc = @base.GetProvidedBaseFunc(default);

		foreach (var transformType in list)
		{
			RuleSoldierTransformation m = mod.GetSoldierTransformation(transformType);
			if (!IsResearched(m.RequiredResearch))
			{
				continue;
			}
			if (AnyMissingBaseFunc(baseFunc, m.RequiredBaseFuncs))
			{
				continue;
			}

			transformations.Add(m);
		}
	}

	/// <summary>Get the list of newly available items to purchase once a ResearchProject has been completed.</summary>
	/// <param name="dependables">The list of RuleItem which are now available.</param>
	/// <param name="research">The RuleResearch which has just been discovered.</param>
	/// <param name="mod">The Game Mod.</param>
	public void GetDependablePurchase(List<RuleItem> dependables, RuleResearch research, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(dependables);
		ArgumentNullException.ThrowIfNull(mod);
		foreach (var itemType in mod.ItemsList)
		{
			RuleItem item = mod.GetItem(itemType);
			if (item.BuyCost != 0)
			{
				var reqs = item.Requirements;
				bool found = reqs.Contains(research);
				var reqsBuy = item.BuyRequirements;
				bool foundBuy = reqsBuy.Contains(research);
				if (found || foundBuy)
				{
					if (IsResearched(item.BuyRequirements) && IsResearched(item.Requirements))
					{
						dependables.Add(item);
					}
				}
			}
		}
	}

	/// <summary>Get the list of newly available craft to purchase/rent once a ResearchProject has been completed.</summary>
	/// <param name="dependables">The list of RuleCraft which are now available.</param>
	/// <param name="research">The RuleResearch which has just been discovered.</param>
	/// <param name="mod">The Game Mod.</param>
	public void GetDependableCraft(List<RuleCraft> dependables, RuleResearch research, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(dependables);
		ArgumentNullException.ThrowIfNull(research);
		ArgumentNullException.ThrowIfNull(mod);
		foreach (var craftType in mod.CraftsList)
		{
			RuleCraft craftItem = mod.GetCraft(craftType);
			if (craftItem.BuyCost != 0)
			{
				var reqs = craftItem.Requirements;
				if (reqs.Contains(research.Name))
				{
					if (IsResearched(craftItem.Requirements))
					{
						dependables.Add(craftItem);
					}
				}
			}
		}
	}

	/// <summary>Get the list of newly available facilities to build once a ResearchProject has been completed.</summary>
	/// <param name="dependables">The list of RuleBaseFacility which are now available.</param>
	/// <param name="research">The RuleResearch which has just been discovered.</param>
	/// <param name="mod">The Game Mod.</param>
	public void GetDependableFacilities(List<RuleBaseFacility> dependables, RuleResearch research, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(dependables);
		ArgumentNullException.ThrowIfNull(research);
		ArgumentNullException.ThrowIfNull(mod);
		foreach (var facType in mod.BaseFacilitiesList)
		{
			RuleBaseFacility facilityItem = mod.GetBaseFacility(facType);
			var reqs = facilityItem.Requirements;
			if (reqs.Contains(research.Name))
			{
				if (IsResearched(facilityItem.Requirements))
				{
					dependables.Add(facilityItem);
				}
			}
		}
	}

	/// <summary>Gets the status of a ufopedia rule (0=new, 1=normal); like std::map::operator[] it adds a missing entry.</summary>
	public int GetUfopediaRuleStatus(string ufopediaRule)
	{
		if (!_ufopediaRuleStatus.TryGetValue(ufopediaRule, out int status))
			_ufopediaRuleStatus[ufopediaRule] = status = 0;
		return status;
	}

	/// <summary>Gets the log of monthly purchase limits.</summary>
	public SortedDictionary<string, int> MonthlyPurchaseLimitLog => _monthlyPurchaseLimitLog;

	/// <summary>Gets the list of player-defined save-specific RuleCraft '_deployment' overrides.</summary>
	public SortedDictionary<string, List<List<int>>> CustomRuleCraftDeployments => _customRuleCraftDeployments;

	/// <summary>The preview base.</summary>
	public Base PreviewBase
	{
		get => _previewBase;
		set => _previewBase = value;
	}

	/// <summary>Gets the status of a manufacture rule (0=new, 1=normal, 2=hidden); like std::map::operator[] it adds a missing entry.</summary>
	public int GetManufactureRuleStatus(string manufactureRule)
	{
		if (!_manufactureRuleStatus.TryGetValue(manufactureRule, out int status))
			_manufactureRuleStatus[manufactureRule] = status = 0;
		return status;
	}

	/// <summary>Gets all the research rule status info.</summary>
	public SortedDictionary<string, int> ResearchRuleStatusRaw => _researchRuleStatus;

	/// <summary>Gets the status of a research rule (0=new, 1=normal, 2=disabled, 3=hidden).</summary>
	public int GetResearchRuleStatus(string researchRule)
	{
		if (_researchRuleStatus.TryGetValue(researchRule, out int status))
		{
			return status;
		}
		return RuleResearch.RESEARCH_STATUS_NEW; // no status = new
	}

	/// <summary>Is the research permanently disabled?</summary>
	public bool IsResearchRuleStatusDisabled(string researchRule)
	{
		if (_researchRuleStatus.TryGetValue(researchRule, out int status))
		{
			if (status == RuleResearch.RESEARCH_STATUS_DISABLED)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Returns if a research still has undiscovered non-disabled "getOneFree".</summary>
	/// <param name="r">Research to check.</param>
	/// <param name="checkOnlyAvailableTopics">Check only available topics (=topics with discovered prerequisite) or all topics?</param>
	public bool HasUndiscoveredGetOneFree(RuleResearch r, bool checkOnlyAvailableTopics)
	{
		ArgumentNullException.ThrowIfNull(r);
		if (!IsResearched(r.GetOneFree, false, true))
		{
			return true; // found something undiscovered (and NOT disabled) already, no need to search further
		}
		else
		{
			// search through getOneFreeProtected topics too
			foreach (var pair in r.GetOneFreeProtected)
			{
				if (checkOnlyAvailableTopics && !IsResearched(pair.first, false))
				{
					// skip this group, its prerequisite has not been discovered yet
				}
				else
				{
					if (!IsResearched(pair.second, false, true))
					{
						return true; // found something undiscovered (and NOT disabled) already, no need to search further
					}
				}
			}
		}
		return false;
	}

	/// <summary>Returns if a research still has undiscovered non-disabled "protected unlocks".</summary>
	public bool HasUndiscoveredProtectedUnlock(RuleResearch r)
	{
		ArgumentNullException.ThrowIfNull(r);
		// Note: checking for not yet discovered unlocks protected by "requires" (which also implies cost = 0)
		foreach (var unlock in r.Unlocked)
		{
			if (IsResearchRuleStatusDisabled(unlock.Name))
			{
				// ignore all disabled topics (as if they didn't exist)
				continue;
			}
			if (unlock.Requirements.Count != 0)
			{
				if (!IsResearched(unlock, false))
				{
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>Returns if a certain research topic has been completed.</summary>
	/// <param name="research">Research ID.</param>
	/// <param name="considerDebugMode">Should debug mode be considered or not.</param>
	public bool IsResearched(string research, bool considerDebugMode = true)
	{
		//if (research.empty())
		//	return true;
		if (considerDebugMode && _debug)
			return true;

		return HaveResearchVector(_discovered, research);
	}

	/// <summary>Returns if a certain research topic has been completed.</summary>
	public bool IsResearched(RuleResearch research, bool considerDebugMode = true)
	{
		//if (research.empty())
		//	return true;
		if (considerDebugMode && _debug)
			return true;

		return HaveResearchVector(_discovered, research);
	}

	/// <summary>Returns if a certain list of research topics has been completed.</summary>
	public bool IsResearched(List<string> research, bool considerDebugMode = true)
	{
		ArgumentNullException.ThrowIfNull(research);
		if (research.Count == 0)
			return true;
		if (considerDebugMode && _debug)
			return true;

		foreach (var res in research)
		{
			if (!HaveResearchVector(_discovered, res))
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>Returns if a certain list of research topics has been completed.</summary>
	/// <param name="research">List of research IDs.</param>
	/// <param name="considerDebugMode">Should debug mode be considered or not.</param>
	/// <param name="skipDisabled">Should permanently disabled topics be considered or not.</param>
	public bool IsResearched(List<RuleResearch> research, bool considerDebugMode = true, bool skipDisabled = false)
	{
		ArgumentNullException.ThrowIfNull(research);
		if (research.Count == 0)
			return true;
		if (considerDebugMode && _debug)
			return true;

		foreach (var res in research)
		{
			if (skipDisabled)
			{
				// ignore all disabled topics (as if they didn't exist)
				if (IsResearchRuleStatusDisabled(res.Name))
				{
					continue;
				}
			}
			if (!HaveResearchVector(_discovered, res))
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Returns if a certain item has been obtained, i.e. is present in the base stores or on a craft.
	/// Items in transfer, worn by soldiers, etc. are ignored!!
	/// </summary>
	public bool IsItemObtained(string itemType, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		RuleItem item = mod.GetItem(itemType);
		if (item != null)
		{
			foreach (var xbase in _bases)
			{
				if (xbase.StorageItems.GetItem(item) > 0)
					return true;

				foreach (var xcraft in xbase.Crafts)
				{
					if (xcraft.Items.GetItem(item) > 0)
						return true;
				}
			}
		}
		return false;
	}

	/// <summary>Returns if a certain facility has been built in any base.</summary>
	public bool IsFacilityBuilt(string facilityType)
	{
		foreach (var xbase in _bases)
		{
			foreach (var fac in xbase.Facilities)
			{
				if (fac.BuildTime == 0 && fac.Rules.Type == facilityType)
				{
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>Returns if a certain soldier type has been hired (and arrived already) in any base.</summary>
	public bool IsSoldierTypeHired(string soldierType)
	{
		foreach (var xbase in _bases)
		{
			foreach (var soldier in xbase.Soldiers)
			{
				if (soldier.Rules.Type == soldierType)
				{
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>Returns the Soldier given it's unique ID.</summary>
	public Soldier GetSoldier(int id)
	{
		foreach (var xbase in _bases)
		{
			foreach (var soldier in xbase.Soldiers)
			{
				if (soldier.Id == id)
				{
					return soldier;
				}
			}
		}
		foreach (var soldier in _deadSoldiers)
		{
			if (soldier.Id == id)
			{
				return soldier;
			}
		}
		return null;
	}

	/// <summary>Handles the higher promotions (not the rookie-squaddie ones).</summary>
	/// <param name="participants">A list of soldiers that were actually present at the battle.</param>
	/// <param name="mod">The Game Mod.</param>
	/// <returns>Whether or not some promotions happened - to show the promotions screen.</returns>
	public bool HandlePromotions(List<Soldier> participants, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		int soldiersPromoted = 0;
		Soldier highestRanked = null;
		List<Soldier> soldiers = AllActiveSoldiers;
		var rankCounts = new RankCount(soldiers);

		int totalSoldiers = rankCounts.TotalSoldiers;

		if (rankCounts[SoldierRank.RANK_COMMANDER] == 0 && totalSoldiers >= mod.GetSoldiersPerRank(SoldierRank.RANK_COMMANDER))
		{
			highestRanked = InspectSoldiers(soldiers, participants, (int)SoldierRank.RANK_COLONEL);
			if (highestRanked != null)
			{
				// only promote one colonel to commander
				highestRanked.PromoteRank();
				soldiersPromoted++;
				rankCounts[SoldierRank.RANK_COMMANDER]++;
				rankCounts[SoldierRank.RANK_COLONEL]--;
			}
		}

		foreach (SoldierRank rank in (ReadOnlySpan<SoldierRank>)[SoldierRank.RANK_COLONEL, SoldierRank.RANK_CAPTAIN, SoldierRank.RANK_SERGEANT])
		{
			while ((totalSoldiers / mod.GetSoldiersPerRank(rank)) > rankCounts[rank])
			{
				highestRanked = InspectSoldiers(soldiers, participants, (int)rank - 1);
				if (highestRanked != null)
				{
					highestRanked.PromoteRank();
					soldiersPromoted++;
					rankCounts[rank]++;
					rankCounts[(SoldierRank)((int)rank - 1)]--;
				}
				else
				{
					break;
				}
			}
		}

		return soldiersPromoted > 0;
	}

	/// <summary>Checks how many soldiers of a rank exist and which one has the highest score.</summary>
	/// <param name="soldiers">Full list of live soldiers.</param>
	/// <param name="participants">List of participants on this mission.</param>
	/// <param name="rank">Rank to inspect.</param>
	/// <returns>The highest ranked soldier.</returns>
	public Soldier InspectSoldiers(List<Soldier> soldiers, List<Soldier> participants, int rank)
	{
		ArgumentNullException.ThrowIfNull(soldiers);
		ArgumentNullException.ThrowIfNull(participants);
		int highestScore = 0;
		Soldier highestRanked = null;
		foreach (var soldier in soldiers)
		{
			var rankStrings = soldier.Rules.RankStrings;
			bool rankIsMatching = (int)soldier.Rank == rank;
			if (rankStrings.Count != 0)
			{
				// if rank is matching, but there are no more higher ranks defined for this soldier type, skip this soldier
				if (rankIsMatching && (rank >= rankStrings.Count - 1))
				{
					rankIsMatching = false;
				}
			}
			if (rankIsMatching)
			{
				int score = GetSoldierScore(soldier);
				if (score > highestScore && (!Options.fieldPromotions || participants.Contains(soldier)))
				{
					highestScore = score;
					highestRanked = soldier;
				}
			}
		}
		return highestRanked;
	}

	/// <summary>Gets the (approximate) number of idle days since the soldier's last mission.</summary>
	public int GetSoldierIdleDays(Soldier soldier)
	{
		ArgumentNullException.ThrowIfNull(soldier);
		int lastMissionId = -1;
		int idleDays = 999;

		if (soldier.Diary.MissionIdList.Count != 0)
		{
			lastMissionId = soldier.Diary.MissionIdList[^1];
		}

		if (lastMissionId == -1)
			return idleDays;

		foreach (var missionInfo in _missionStatistics)
		{
			if (missionInfo.id == lastMissionId)
			{
				idleDays = 0;
				idleDays += (_time.Year - missionInfo.time.Year) * 365;
				idleDays += (_time.Month - missionInfo.time.Month) * 30;
				idleDays += (_time.Day - missionInfo.time.Day) * 1;
				break;
			}
		}

		if (idleDays > 999)
			idleDays = 999;

		return idleDays;
	}

	/// <summary>Evaluate the score of a soldier based on all of his stats, missions and kills.</summary>
#pragma warning disable CA1822 // instance method in C++, kept for callers
	public int GetSoldierScore(Soldier soldier)
#pragma warning restore CA1822
	{
		ArgumentNullException.ThrowIfNull(soldier);
		UnitStats s = soldier.CurrentStats;
		int v1 = 2 * s.health + 2 * s.stamina + 4 * s.reactions + 4 * s.bravery;
		int v2 = v1 + 3 * (s.tu + 2 * s.firing);
		int v3 = v2 + s.melee + s.throwing + s.strength;
		if (s.psiSkill > 0) v3 += s.psiStrength + 2 * s.psiSkill;
		return v3 + 10 * (soldier.Missions + soldier.Kills);
	}

	/// <summary>Returns the list of alien bases.</summary>
	public List<AlienBase> AlienBases => _alienBases;

	/// <summary>Toggles debug mode.</summary>
	public void SetDebugMode()
	{
		_debug = !_debug;
	}

	/// <summary>Gets the current debug mode.</summary>
	public bool DebugMode => _debug;

	/// <summary>The list of monthly maintenance costs.</summary>
	public List<long> Maintenances => _maintenance;

	/// <summary>Adds to this month's research score.</summary>
	public void AddResearchScore(int score)
	{
		_researchScores[^1] += score;
	}

	/// <summary>The list of research scores.</summary>
	public List<int> ResearchScores => _researchScores;

	/// <summary>The list of incomes.</summary>
	public List<long> Incomes => _incomes;

	/// <summary>The list of expenditures.</summary>
	public List<long> Expenditures => _expenditures;

	/// <summary>If the player has been warned about poor performance.</summary>
	public bool Warned
	{
		get => _warned;
		set => _warned = value;
	}

	/// <summary>Personal light toggle.</summary>
	public bool TogglePersonalLight
	{
		get => _togglePersonalLight;
		set => _togglePersonalLight = value;
	}

	/// <summary>Night vision toggle.</summary>
	public bool ToggleNightVision
	{
		get => _toggleNightVision;
		set => _toggleNightVision = value;
	}

	/// <summary>Brightness toggle.</summary>
	public int ToggleBrightness
	{
		get => _toggleBrightness;
		set => _toggleBrightness = value;
	}

	/// <summary>Full access to the alien strategy data.</summary>
	public AlienStrategy AlienStrategy => _alienStrategy;

	/// <summary>Full access to the current alien missions.</summary>
	public List<AlienMission> AlienMissions => _activeMissions;

	/// <summary>Find a mission type in the active alien missions.</summary>
	/// <param name="region">The region string ID.</param>
	/// <param name="objective">The active mission objective.</param>
	/// <param name="race">The alien race used for more specific search (by type instead of objective). Optional.</param>
	/// <returns>The mission, or null if no mission matched.</returns>
	public AlienMission FindAlienMission(string region, MissionObjective objective, AlienRace race = null)
	{
		if (race != null)
		{
			var retalWeights = race.RetaliationMissionWeights(_monthsPassed);
			if (retalWeights != null)
			{
				var retalNames = retalWeights.Names;
				if (retalNames.Count != 0)
				{
					// if we made it this far, search by type and region
					foreach (var missionType in retalNames)
					{
						foreach (var mission in _activeMissions)
						{
							if (mission.Rules.Type == missionType && mission.Region == region)
							{
								return mission;
							}
						}
					}
					return null;
				}
			}
		}

		foreach (var mission in _activeMissions)
		{
			if (mission.Rules.Objective == objective && mission.Region == region)
			{
				return mission;
			}
		}
		return null;
	}

	/// <summary>Full access to the current geoscape events.</summary>
	public List<GeoscapeEvent> GeoscapeEvents => _geoscapeEvents;

	/// <summary>Find the region containing this location.</summary>
	/// <returns>The region, or null.</returns>
	public Region LocateRegion(double lon, double lat)
	{
		Region found = null;
		foreach (var region in _regions)
		{
			if (region.Rules.InsideRegion(lon, lat))
			{
				found = region;
				break;
			}
		}
		if (found != null)
		{
			return found;
		}
		Logger.Error(string.Create(CultureInfo.InvariantCulture, $"Failed to find a region at location [{lon:G6}, {lat:G6}]."));
		return null;
	}

	/// <summary>Find the region containing this target.</summary>
	public Region LocateRegion(Target target)
	{
		ArgumentNullException.ThrowIfNull(target);
		return LocateRegion(target.Longitude, target.Latitude);
	}

	/// <summary>Find the country containing this location.</summary>
	/// <returns>The country, or null.</returns>
	public Country LocateCountry(double lon, double lat)
	{
		Country found = null;
		foreach (var country in _countries)
		{
			if (country.Rules.InsideCountry(lon, lat))
			{
				found = country;
				break;
			}
		}
		if (found != null)
		{
			return found;
		}
		//Log(LOG_DEBUG) << "Failed to find a country at location [" << lon << ", " << lat << "].";
		return null;
	}

	/// <summary>Find the country containing this target.</summary>
	public Country LocateCountry(Target target)
	{
		ArgumentNullException.ThrowIfNull(target);
		return LocateCountry(target.Longitude, target.Latitude);
	}

	/// <summary>Select a soldier nationality based on mod rules and location on the globe.</summary>
	public int SelectSoldierNationalityByLocation(Mod mod, RuleSoldier rule, Target target)
	{
		ArgumentNullException.ThrowIfNull(mod);
		ArgumentNullException.ThrowIfNull(rule);
		if (target == null)
		{
			return -1;
		}

		if (mod.HireByCountryOdds > 0 && Rng.Percent(mod.HireByCountryOdds))
		{
			Country country = LocateCountry(target);
			if (country != null)
			{
				int nationality = 0;
				foreach (var namepool in rule.Names)
				{
					// we assume there is only one such name pool (or none), thus we stop searching on the first hit
					if (country.Rules.Type == namepool.Country)
					{
						return nationality;
					}
					++nationality;
				}
			}
		}

		if (mod.HireByRegionOdds > 0 && Rng.Percent(mod.HireByRegionOdds))
		{
			Region region = LocateRegion(target);
			if (region != null)
			{
				// build a new name pool collection, filtered by the region
				var filteredNames = new List<(SoldierNamePool first, int second)>();
				int totalFilteredNamePoolWeight = 0;
				int nationality = 0;
				foreach (var namepool in rule.Names)
				{
					if (region.Rules.Type == namepool.Region)
					{
						filteredNames.Add((namepool, nationality));
						totalFilteredNamePoolWeight += namepool.GlobalWeight;
					}
					++nationality;
				}

				// the modder wants to have regional name pools, but didn't provide any, sigh
				if (totalFilteredNamePoolWeight < 1)
				{
					return -1; // let's just ignore the modder's unfulfillable wish
				}

				// select the nationality from the filtered pool, by weight
				int tmp = Rng.Generate(1, totalFilteredNamePoolWeight);
				foreach (var namepoolPair in filteredNames)
				{
					if (tmp <= namepoolPair.first.GlobalWeight)
					{
						return namepoolPair.second;
					}
					tmp -= namepoolPair.first.GlobalWeight;
				}
			}
		}

		return -1;
	}

	/// <summary>The month counter.</summary>
	public int MonthsPassed => _monthsPassed;

	/// <summary>The day counter.</summary>
	public int DaysPassed => _daysPassed;

	public void IncreaseDaysPassed()
	{
		_daysPassed++;
	}

	/// <summary>The vehicles lost counter.</summary>
	public int VehiclesLost => _vehiclesLost;

	public void IncreaseVehiclesLost()
	{
		_vehiclesLost++;
	}

	/// <summary>The craft lost in dogfight counter.</summary>
	public int CraftLostDogfight => _craftLostDogfight;

	public void IncreaseCraftLostDogfight()
	{
		_craftLostDogfight++;
	}

	/// <summary>The craft lost in mission counter.</summary>
	public int CraftLostMission => _craftLostMission;

	public void IncreaseCraftLostMission()
	{
		_craftLostMission++;
	}

	/// <summary>The GraphRegionToggles.</summary>
	public string GraphRegionToggles
	{
		get => _graphRegionToggles;
		set => _graphRegionToggles = value;
	}

	/// <summary>The GraphCountryToggles.</summary>
	public string GraphCountryToggles
	{
		get => _graphCountryToggles;
		set => _graphCountryToggles = value;
	}

	/// <summary>The GraphFinanceToggles.</summary>
	public string GraphFinanceToggles
	{
		get => _graphFinanceToggles;
		set => _graphFinanceToggles = value;
	}

	/// <summary>Increment the month counter.</summary>
	public void AddMonth()
	{
		++_monthsPassed;

		_monthlyPurchaseLimitLog.Clear();
	}

	/// <summary>Marks a research topic as having already come up as "we can now research".</summary>
	public void AddPoppedResearch(RuleResearch research)
	{
		if (!WasResearchPopped(research))
			_poppedResearch.Add(research);
	}

	/// <summary>Checks if an unresearched topic has previously been popped up.</summary>
	public bool WasResearchPopped(RuleResearch research)
	{
		return _poppedResearch.Contains(research);
	}

	/// <summary>Checks for and removes a research project from the "has been popped up" array.</summary>
	public void RemovePoppedResearch(RuleResearch research)
	{
		_poppedResearch.Remove(research);
	}

	/// <summary>Remembers that this event has been generated.</summary>
	public void AddGeneratedEvent(RuleEvent @event)
	{
		ArgumentNullException.ThrowIfNull(@event);
		_generatedEvents.TryGetValue(@event.Name, out int count);
		_generatedEvents[@event.Name] = count + 1;
	}

	/// <summary>Checks if an event has been generated previously.</summary>
	public bool WasEventGenerated(string eventName)
	{
		return _generatedEvents.ContainsKey(eventName);
	}

	/// <summary>The list of dead soldiers.</summary>
	public List<Soldier> DeadSoldiers => _deadSoldiers;

	/// <summary>Calculates and returns a list of all active soldiers.</summary>
	public List<Soldier> AllActiveSoldiers
	{
		get
		{
			var soldiers = new List<Soldier>();
			foreach (var xbase in _bases)
			{
				soldiers.AddRange(xbase.Soldiers);

				foreach (var transfer in xbase.Transfers)
				{
					if (transfer.Type == TransferType.TRANSFER_SOLDIER)
					{
						soldiers.Add(transfer.Soldier);
					}
				}
			}

			return soldiers;
		}
	}

	/// <summary>The last selected armor type string.</summary>
	public string LastSelectedArmor
	{
		get => _lastselectedArmor;
		set => _lastselectedArmor = value;
	}

	/// <summary>Returns the name of a global equipment layout at specified index.</summary>
	public string GetGlobalEquipmentLayoutName(int index)
	{
		return _globalEquipmentLayoutName[index];
	}

	/// <summary>Sets the name of a global equipment layout at specified index.</summary>
	public void SetGlobalEquipmentLayoutName(int index, string name)
	{
		_globalEquipmentLayoutName[index] = name;
	}

	/// <summary>Returns the armor type of a global equipment layout at specified index.</summary>
	public string GetGlobalEquipmentLayoutArmor(int index)
	{
		return _globalEquipmentLayoutArmor[index];
	}

	/// <summary>Sets the armor type of a global equipment layout at specified index.</summary>
	public void SetGlobalEquipmentLayoutArmor(int index, string armorType)
	{
		_globalEquipmentLayoutArmor[index] = armorType;
	}

	/// <summary>Returns the global equipment layout at specified index.</summary>
	public List<EquipmentLayoutItem> GetGlobalEquipmentLayout(int index)
	{
		return _globalEquipmentLayout[index];
	}

	/// <summary>Returns the name of a global craft loadout at specified index.</summary>
	public string GetGlobalCraftLoadoutName(int index)
	{
		return _globalCraftLoadoutName[index];
	}

	/// <summary>Sets the name of a global craft loadout at specified index.</summary>
	public void SetGlobalCraftLoadoutName(int index, string name)
	{
		_globalCraftLoadoutName[index] = name;
	}

	/// <summary>Returns the global craft loadout at specified index.</summary>
	public ItemContainer GetGlobalCraftLoadout(int index)
	{
		return _globalCraftLoadout[index];
	}

	/// <summary>The list of mission statistics.</summary>
	public List<MissionStatistics> MissionStatistics => _missionStatistics;

	/// <summary>Adds a UFO to the ignore list.</summary>
	public void AddUfoToIgnoreList(int ufoId)
	{
		if (ufoId != 0)
		{
			_ignoredUfos.Add(ufoId);
		}
	}

	/// <summary>Checks if a UFO is on the ignore list.</summary>
	public bool IsUfoOnIgnoreList(int ufoId)
	{
		return _ignoredUfos.Contains(ufoId);
	}

	/// <summary>Registers a soldier's death in the memorial.</summary>
	/// <param name="resetArmor">Bury the soldier in the default armor.</param>
	/// <param name="soldier">The dead soldier.</param>
	/// <param name="cause">Cause of death, null if missing in action.</param>
	/// <returns>
	/// C++ returns the iterator after the erased element of the base's soldier list: here its index
	/// (the removed soldier's former index), or -1 if the soldier was in no base (C++: the end of the last base's list).
	/// </returns>
	public int KillSoldier(bool resetArmor, Soldier soldier, BattleUnitKills cause = null)
	{
		ArgumentNullException.ThrowIfNull(soldier);
		if (resetArmor)
		{
			// OXCE: soldiers are buried in their default armor (...nicer stats in the Memorial GUI; no free armor if resurrected)
			soldier.SetArmor(soldier.Rules.DefaultArmor);
			soldier.ReplacedArmor = null;
			soldier.TransformedArmor = null;
		}
		else
		{
			// IMPORTANT: don't change the geoscape armor during the ongoing battle!
			// battlescape armor would reset to geoscape armor after save and reload
		}

		foreach (var xbase in _bases)
		{
			var soldiers = xbase.Soldiers;
			for (int i = 0; i < soldiers.Count; ++i)
			{
				if (soldiers[i] == soldier)
				{
					// SoldierDeath takes the time by value in C++
					soldier.Die(new SoldierDeath(new GameTime(_time), cause));
					_deadSoldiers.Add(soldier);
					soldiers.RemoveAt(i);
					return i;
				}
			}
		}
		return -1;
	}

	/// <summary>Enables/disables autosell for an item type.</summary>
	public void SetAutosell(RuleItem itype, bool enabled)
	{
		if (enabled)
		{
			_autosales.Add(itype);
		}
		else
		{
			_autosales.Remove(itype);
		}
	}

	/// <summary>Get autosell state for an item type.</summary>
	public bool GetAutosell(RuleItem arg0)
	{
		if (!Options.oxceAutoSell)
		{
			return false;
		}
		return _autosales.Contains(arg0);
	}

	/// <summary>Removes all soldiers from a given craft.</summary>
	public void RemoveAllSoldiersFromXcomCraft(Craft craft)
	{
		foreach (var xbase in _bases)
		{
			foreach (var soldier in xbase.Soldiers)
			{
				if (soldier.Craft == craft)
				{
					soldier.SetCraft(null);
				}
			}
		}
	}

	/// <summary>Stop hunting the given xcom craft.</summary>
	public void StopHuntingXcomCraft(Craft target)
	{
		foreach (var ufo in _ufos)
		{
			ufo.ResetOriginalDestination(target);
		}
	}

	/// <summary>Stop hunting all xcom craft from a given xcom base.</summary>
	public void StopHuntingXcomCrafts(Base @base)
	{
		ArgumentNullException.ThrowIfNull(@base);
		foreach (var xcraft in @base.Crafts)
		{
			foreach (var ufo in _ufos)
			{
				ufo.ResetOriginalDestination(xcraft);
			}
		}
	}

	/// <summary>Should all xcom soldiers have completely empty starting inventory when doing base equipment?</summary>
	public bool DisableSoldierEquipment
	{
		get => _disableSoldierEquipment;
		set => _disableSoldierEquipment = value;
	}

	/// <summary>Is alien containment check finished?</summary>
	public bool AlienContainmentChecked
	{
		get => _alienContainmentChecked;
		set => _alienContainmentChecked = value;
	}

	/// <summary>Is the mana feature already unlocked?</summary>
	public bool IsManaUnlocked(Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		string researchName = mod.ManaUnlockResearch;
		if (Mod.IsEmptyRuleName(researchName) || IsResearched(researchName))
		{
			return true;
		}
		return false;
	}

	/// <summary>Gets the current score based on research score and xcom/alien activity in regions.</summary>
	public int GetCurrentScore(int monthsPassed)
	{
		int invertedEntry = _funds.Count - 1;
		int scoreTotal = _researchScores[invertedEntry];
		if (monthsPassed > 1)
			scoreTotal += 400;
		foreach (var region in _regions)
		{
			scoreTotal += region.ActivityXcom[invertedEntry] - region.ActivityAlien[invertedEntry];
		}
		return scoreTotal;
	}

	/// <summary>Clear links for the given alien base. Use this before deleting the alien base.</summary>
	public void ClearLinksForAlienBase(AlienBase alienBase, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(alienBase);
		ArgumentNullException.ThrowIfNull(mod);
		// Take care to remove supply missions for this base.
		foreach (var am in _activeMissions)
		{
			if (am.AlienBase == alienBase)
			{
				am.AlienBase = null;

				// if this is an Earth-based operation, losing the base means mission cannot continue anymore
				if (am.Rules.OperationType != AlienMissionOperationType.AMOT_SPACE)
				{
					am.SetInterrupted(true);
				}
			}
		}
		// If there was a pact with this base, cancel it?
		if (mod.AllowCountriesToCancelAlienPact && !string.IsNullOrEmpty(alienBase.PactCountry))
		{
			foreach (var country in _countries)
			{
				if (country.Rules.Type == alienBase.PactCountry)
				{
					country.SetCancelPact();
					break;
				}
			}
		}
	}

	/// <summary>Delete the given retaliation mission (and its UFOs).</summary>
	public void DeleteRetaliationMission(AlienMission am, Base @base)
	{
		for (int i = 0; i < _ufos.Count;)
		{
			Ufo ufo = _ufos[i];
			if (ufo.Mission == am)
			{
				ufo.Destroy();
				_ufos.RemoveAt(i);
			}
			else
			{
				++i;
			}
		}
		_activeMissions.Remove(am);
		if (@base != null)
		{
			@base.RetaliationMission = null;
		}
	}

	/// <summary>Spawn a Geoscape event from the event rules.</summary>
	/// <returns>True if successful.</returns>
	public bool SpawnEvent(RuleEvent eventRules)
	{
		if (eventRules == null)
		{
			return false;
		}

		var newEvent = new GeoscapeEvent(eventRules);
		int minutes = (eventRules.Timer + Rng.Generate(0, eventRules.TimerRandom)) / 30 * 30;
		if (minutes < 60) minutes = 60; // just in case
		newEvent.SpawnCountdown = minutes;
		_geoscapeEvents.Add(newEvent);

		// remember that it has been generated
		AddGeneratedEvent(eventRules);

		if (Options.oxceGeoscapeDebugLogMaxEntries > 0)
		{
			_geoscapeDebugLog.Add(string.Create(CultureInfo.InvariantCulture,
				$"gameTime: {_time.FullString} eventSpawn: {newEvent.Rules.Name} days/hours: {minutes / 60 / 24}/{minutes / 60 % 24}"));
		}

		return true;
	}

	/// <summary>Checks if an instant Geoscape event can be spawned.</summary>
	public bool CanSpawnInstantEvent(RuleEvent eventRules)
	{
		if (eventRules == null)
		{
			return false;
		}

		bool interrupted = false;
		if (!string.IsNullOrEmpty(eventRules.InterruptResearch))
		{
			if (IsResearched(eventRules.InterruptResearch, false))
			{
				interrupted = true;
			}
		}

		if (!interrupted)
		{
			AddGeneratedEvent(eventRules);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Handles research unlocked by successful/failed missions and despawned mission sites.
	/// 1. Adds the research topic to finished research list. Silently.
	/// 2. Adds also getOneFree bonus and possible lookup(s). Also silently.
	/// 3. Handles alien mission interruption.
	/// </summary>
	public bool HandleResearchUnlockedByMissions(RuleResearch research, Mod mod, AlienDeployment deployment)
	{
		if (research == null)
		{
			return false;
		}
		ArgumentNullException.ThrowIfNull(mod);
		if (_bases.Count == 0)
		{
			return false; // all bases lost, game over
		}
		Base @base = _bases[0];

		void addResearchDiaryEntryForMission(RuleResearch discoveredResearch, DiscoverySourceType sourceType, AlienDeployment sourceMission, RuleResearch sourceResearch)
		{
			if (!IsResearched(discoveredResearch, false) && !IsResearchRuleStatusDisabled(discoveredResearch.Name))
			{
				var entry = new ResearchDiaryEntry(discoveredResearch);
				entry.SetDate(_time);
				entry.source.type = sourceType;
				if (sourceType == DiscoverySourceType.MISSION)
				{
					entry.source.mission = sourceMission;
					entry.source.name = sourceMission.Type;
				}
				else // sourceType == DiscoverySourceType::FREE_FROM
				{
					entry.source.research = sourceResearch;
					entry.source.name = sourceResearch.Name;
				}
				AddResearchDiaryEntry(entry);
			}
		}

		var researchVec = new List<RuleResearch>();
		researchVec.Add(research);
		addResearchDiaryEntryForMission(research, DiscoverySourceType.MISSION, deployment, null);
		AddFinishedResearch(research, mod, @base, true);
		if (research.Lookup != null)
		{
			researchVec.Add(research.Lookup);
			addResearchDiaryEntryForMission(researchVec[^1], DiscoverySourceType.MISSION, deployment, null);
			AddFinishedResearch(researchVec[^1], mod, @base, true);
		}

		if (SelectGetOneFree(research) is RuleResearch bonus)
		{
			researchVec.Add(bonus);
			addResearchDiaryEntryForMission(bonus, DiscoverySourceType.FREE_FROM, null, research);
			AddFinishedResearch(bonus, mod, @base, true);
			if (bonus.Lookup != null)
			{
				researchVec.Add(bonus.Lookup);
				addResearchDiaryEntryForMission(researchVec[^1], DiscoverySourceType.FREE_FROM, null, research);
				AddFinishedResearch(researchVec[^1], mod, @base, true);
			}
		}

		// check and interrupt alien missions if necessary (based on unlocked research)
		foreach (var am in _activeMissions)
		{
			string interruptResearchName = am.Rules.InterruptResearch;
			if (!string.IsNullOrEmpty(interruptResearchName))
			{
				var interruptResearch = mod.GetResearch(interruptResearchName, true);
				if (researchVec.Contains(interruptResearch))
				{
					am.SetInterrupted(true);
				}
			}
		}

		return true;
	}

	/// <summary>Handles research side effects for primary research sources.</summary>
	public void HandlePrimaryResearchSideEffects(List<RuleResearch> topicsToCheck, Mod mod, Base @base)
	{
		ArgumentNullException.ThrowIfNull(topicsToCheck);
		ArgumentNullException.ThrowIfNull(mod);
		foreach (var myResearchRule in topicsToCheck)
		{
			// 3j. now iterate through all the bases and remove this project from their labs (unless it can still yield more stuff!)
			foreach (Base otherBase in _bases)
			{
				var research = otherBase.Research;
				for (int i = 0; i < research.Count; ++i)
				{
					ResearchProject otherProject = research[i];
					if (myResearchRule == otherProject.Rules)
					{
						if (HasUndiscoveredGetOneFree(myResearchRule, true))
						{
							// This research topic still has some more undiscovered non-disabled and *AVAILABLE* "getOneFree" topics, keep it!
						}
						else if (HasUndiscoveredProtectedUnlock(myResearchRule))
						{
							// This research topic still has one or more undiscovered non-disabled "protected unlocks", keep it!
						}
						else
						{
							// This topic can't give you anything else anymore, remove it!
							otherBase.RemoveResearch(otherProject);
							break;
						}
					}
				}
			}
			// 3k. handle spawned items
			RuleItem spawnedItem = mod.GetItem(myResearchRule.SpawnedItem);
			if (spawnedItem != null)
			{
				var t = new Transfer(1);
				t.SetItems(spawnedItem, Math.Max(1, myResearchRule.SpawnedItemCount));
				@base.Transfers.Add(t);
			}
			foreach (var spawnedItemName2 in myResearchRule.SpawnedItemList)
			{
				RuleItem spawnedItem2 = mod.GetItem(spawnedItemName2);
				if (spawnedItem2 != null)
				{
					var t = new Transfer(1);
					t.SetItems(spawnedItem2);
					@base.Transfers.Add(t);
				}
			}
			// 3l. handle spawned events
			RuleEvent spawnedEventRule = mod.GetEvent(myResearchRule.SpawnedEvent);
			SpawnEvent(spawnedEventRule);
			// 3m. handle counters
			foreach (var inc in myResearchRule.IncreaseCounter)
			{
				IncreaseCustomCounter(inc);
			}
			foreach (var dec in myResearchRule.DecreaseCounter)
			{
				DecreaseCustomCounter(dec);
			}
		}
	}

	/// <summary>Gets the list of user notes.</summary>
	public List<string> UserNotes => _userNotes;

	/// <summary>Gets the list of geoscape debug log entries.</summary>
	public List<string> GeoscapeDebugLog => _geoscapeDebugLog;
}
