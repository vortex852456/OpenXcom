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
// Ported from: src/Savegame/SaveConverter.h, src/Savegame/SaveConverter.cpp
#nullable disable

using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace Acom;

/// <summary>Container for savegame info displayed on listings.</summary>
public class SaveOriginal
{
	public int id;
	public string name = "";
	public string date = "";
	public string time = "";
	public bool tactical;

	public SaveOriginal()
	{
		id = 0;
		tactical = false;
	}
}

/// <summary>
/// Handles conversion operations for original X-COM savegames.
/// See http://ufopaedia.org/index.php?title=Saved_Game_Files
/// </summary>
/// <remarks>
/// Strings in the binary files are read as UTF-8 up to the first NUL byte (C++ passes the raw bytes on as UTF-8 text).
/// Reads past the end of a file throw here (undefined behaviour in C++).
/// </remarks>
public class SaveConverter
{
	private enum TargetType
	{
		TARGET_NONE, TARGET_UFO, TARGET_CRAFT, TARGET_XBASE, TARGET_ABASE, TARGET_CRASH, TARGET_LANDED, TARGET_WAYPOINT, TARGET_TERROR,
		TARGET_PORT = 0x51, TARGET_ISLAND = 0x52, TARGET_SHIP = 0x53, TARGET_ARTEFACT = 0x54,
	}

	private static readonly string[] xcomAltitudes = ["STR_GROUND", "STR_VERY_LOW", "STR_LOW_UC", "STR_HIGH_UC", "STR_VERY_HIGH"];
	private static readonly string[] xcomStatus = ["STR_READY", "STR_OUT", "STR_REPAIRS", "STR_REFUELLING", "STR_REARMING"];

	public const int NUM_SAVES = 10;

	private string _saveName, _savePath;
	private SavedGame _save;
	private Mod _mod;
	private RuleConverter _rules;
	private int _year, _funds;
	private readonly List<Target> _targets = [];
	private readonly List<int> _targetDat = [];
	private readonly List<Soldier> _soldiers = [];
	private readonly List<string> _aliens = [];
	private readonly Dictionary<(int first, int second), AlienMission> _missions = [];

	// Helper functions (little-endian loads like C++ load<T>)
	private static ushort LoadU16(byte[] data, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset));
	private static short LoadS16(byte[] data, int offset) => BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(offset));
	private static int LoadInt(byte[] data, int offset) => BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset));
	private static byte LoadU8(byte[] data, int offset) => data[offset];
	private static sbyte LoadChar(byte[] data, int offset) => unchecked((sbyte)data[offset]);

	private static string LoadString(byte[] data, int offset)
	{
		int end = Array.IndexOf(data, (byte)0, offset);
		if (end < 0)
			end = data.Length;
		return Encoding.UTF8.GetString(data, offset, end - offset);
	}

	/// <summary>C++ Xcom2Rad (fmath.h).</summary>
	private static double Xcom2Rad(int deg) => deg * 0.125 * Math.PI / 180.0;

	/// <summary>Loads a binary file of the save folder.</summary>
	private byte[] BinaryBuffer(string filename) => CrossPlatform.ReadFileRaw(_savePath + "/" + filename);

	/// <summary>Creates a new converter for the given save folder.</summary>
	/// <param name="save">Number of the save folder GAME_#.</param>
	/// <param name="mod">Mod to associate with this save.</param>
	public SaveConverter(int save, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		_save = null;
		_mod = mod;
		_rules = mod.Converter;
		_year = 0;
		_funds = 0;
		string ssFolder = "GAME_" + save.ToString(CultureInfo.InvariantCulture);
		string ssPath = Options.GetMasterUserFolder() + ssFolder;
		_saveName = ssFolder;
		_savePath = ssPath;
		ssPath += "/SAVEINFO.DAT";
		if (!CrossPlatform.FileExists(ssPath))
		{
			throw new InvalidOperationException(_saveName + " is not a valid save folder");
		}
	}

	/// <summary>Gets all the info of the saves found in the user folder.</summary>
	/// <param name="lang">Loaded language.</param>
	/// <param name="info">Returned list of saves info.</param>
	public static void GetList(Language lang, SaveOriginal[] info)
	{
		ArgumentNullException.ThrowIfNull(lang);
		ArgumentNullException.ThrowIfNull(info);
		for (int i = 0; i < NUM_SAVES; ++i)
		{
			SaveOriginal save = info[i];
			save.id = 0;

			int id = i + 1;
			string datname = Options.GetMasterUserFolder() + "GAME_" + id.ToString(CultureInfo.InvariantCulture) + "/SAVEINFO.DAT";
			if (!CrossPlatform.FileExists(datname))
			{
				continue;
			}
			byte[] data = CrossPlatform.ReadFileRaw(datname);

			string name = LoadString(data, 0x02);
			int year = LoadU16(data, 0x1C);
			int month = LoadU16(data, 0x1E);
			int day = LoadU16(data, 0x20);
			int hour = LoadU16(data, 0x22);
			int minute = LoadU16(data, 0x24);
			bool tactical = LoadChar(data, 0x26) != 0;

			var time = new GameTime(0, day, month + 1, year, hour, minute, 0);

			string ssDate = time.GetDayString(lang) + "  " + lang.GetString(time.MonthString) + "  " + time.Year.ToString(CultureInfo.InvariantCulture);
			string ssTime = time.Hour.ToString(CultureInfo.InvariantCulture) + ":" + time.Minute.ToString("00", CultureInfo.InvariantCulture);

			save.id = id;
			save.name = name;
			save.date = ssDate;
			save.time = ssTime;
			save.tactical = tactical;
		}
	}

	/// <summary>Converts an original X-COM save into an OpenXcom save.</summary>
	/// <returns>New OpenXcom save.</returns>
	public SavedGame LoadOriginal()
	{
		_save = new SavedGame();

		// Load globe data
		_save.Incomes.Clear();
		for (int i = 0; i < _rules.Countries.Count; ++i)
		{
			var country = new Country(_mod.GetCountry(_rules.Countries[i], true));
			country.ActivityAlien.Clear();
			country.ActivityXcom.Clear();
			country.Funding.Clear();
			_save.Countries.Add(country);
		}
		for (int i = 0; i < _rules.Regions.Count; ++i)
		{
			var region = new Region(_mod.GetRegion(_rules.Regions[i], true));
			region.ActivityAlien.Clear();
			region.ActivityXcom.Clear();
			_save.Regions.Add(region);
		}
		LoadDatXcom();
		LoadDatAlien();
		LoadDatDiplom();
		LoadDatLease();

		// Load graph data
		_save.Expenditures.Clear();
		_save.Maintenances.Clear();
		_save.FundsList.Clear();
		_save.ResearchScores.Clear();
		LoadDatLIGlob();
		LoadDatUIGlob();
		LoadDatIGlob();

		// Load alien data
		LoadDatZonal();
		LoadDatActs();
		LoadDatMissions();

		// Load player data
		LoadDatLoc();
		LoadDatBase();
		LoadDatAStore();
		LoadDatCraft();
		LoadDatSoldier();
		LoadDatTransfer();
		LoadDatResearch();
		LoadDatUp();
		LoadDatProject();
		LoadDatBProd();
		LoadDatXBases();

		return _save;
	}

	/// <summary>
	/// Corrects vectors of graph data.
	/// Original X-COM uses months as array indexes,
	/// while OpenXcom stores month data in a linear fashion.
	/// </summary>
	/// <param name="vector">Vector of graph data.</param>
	/// <param name="month">Current month.</param>
	/// <param name="year">Has game gone longer than a year?</param>
	internal static void GraphVector<T>(List<T> vector, int month, bool year)
	{
		if (year)
		{
			var newVector = new List<T>();
			int i = month;
			do
			{
				newVector.Add(vector[i]);
				i = (i + 1) % vector.Count;
			}
			while (i != month);
			vector.Clear();
			vector.AddRange(newVector);
		}
		else
		{
			// std::vector::resize
			if (month < vector.Count)
				vector.RemoveRange(month, vector.Count - month);
			else
				while (vector.Count < month)
					vector.Add(default);
		}
	}

	/// <summary>Loads the IGLOB.DAT file. Contains game date, time and difficulty.</summary>
	private void LoadDatIGlob()
	{
		byte[] data = BinaryBuffer("IGLOB.DAT");

		int month = LoadInt(data, 0x00) + 1;
		int weekday = LoadInt(data, 0x04) + 1;
		int day = LoadInt(data, 0x08);
		int hour = LoadInt(data, 0x0C);
		int minute = LoadInt(data, 0x10);
		int second = LoadInt(data, 0x14);
		_save.Time = new GameTime(weekday, day, month, _year, hour, minute, second);

		// account for difficulty bug
		if (data.Length > 0x3C)
		{
			int coefficient = LoadInt(data, 0x3C);
			for (int i = (int)GameDifficulty.DIFF_BEGINNER; i <= (int)GameDifficulty.DIFF_SUPERHUMAN; ++i)
			{
				if (coefficient == Mod.DIFFICULTY_COEFFICIENT[i])
				{
					_save.Difficulty = (GameDifficulty)i;
					break;
				}
			}
		}

		// Fix up the months
		int startYear = _mod.StartingTime.Year;
		int monthsPassed = month + (_year - startYear) * 12;
		for (int i = 0; i < monthsPassed; ++i)
		{
			_save.AddMonth();
		}
		GraphVector(_save.Incomes, month, _year != startYear);
		GraphVector(_save.Expenditures, month, _year != startYear);
		GraphVector(_save.Maintenances, month, _year != startYear);
		GraphVector(_save.FundsList, month, _year != startYear);
		GraphVector(_save.ResearchScores, month, _year != startYear);
		for (int i = 0; i < _rules.Countries.Count; ++i)
		{
			Country country = _save.Countries[i];
			GraphVector(country.ActivityAlien, month, _year != startYear);
			GraphVector(country.ActivityXcom, month, _year != startYear);
			GraphVector(country.Funding, month, _year != startYear);
		}
		for (int i = 0; i < _rules.Regions.Count; ++i)
		{
			Region region = _save.Regions[i];
			GraphVector(region.ActivityAlien, month, _year != startYear);
			GraphVector(region.ActivityXcom, month, _year != startYear);
		}
		_save.FundsList[^1] = _funds;
	}

	/// <summary>Loads the LIGLOB.DAT file. Contains financial data.</summary>
	private void LoadDatLIGlob()
	{
		byte[] data = BinaryBuffer("LIGLOB.DAT");

		const int MONTHS = 12;
		for (int i = 0; i < MONTHS; ++i)
		{
			int expenditure = LoadInt(data, 0x04 + i * sizeof(int));
			int maintenance = LoadInt(data, 0x34 + i * sizeof(int));
			int balance = LoadInt(data, 0x64 + i * sizeof(int));
			_save.Expenditures.Add(expenditure);
			_save.Maintenances.Add(maintenance);
			_save.FundsList.Add(balance);
		}

		_funds = LoadInt(data, 0);
	}

	/// <summary>Loads the UIGLOB.DAT file. Contains Geoscape number IDs and scores.</summary>
	private void LoadDatUIGlob()
	{
		byte[] data = BinaryBuffer("UIGLOB.DAT");

		var ids = new SortedDictionary<string, int>(StringComparer.Ordinal);
		for (int i = 0; i < _rules.Markers.Count; ++i)
		{
			ids[_rules.Markers[i]] = LoadU16(data, i * sizeof(ushort));
		}
		int ufoId = ids.GetValueOrDefault("STR_UFO");
		ids["STR_UFO"] = ufoId; // C++ operator[] inserts missing keys
		ids["STR_CRASH_SITE"] = ids["STR_LANDING_SITE"] = ufoId;

		_year = LoadU16(data, 0x16);

		const int MONTHS = 12;
		for (int i = 0; i < MONTHS; ++i)
		{
			int score = LoadS16(data, 0x18 + i * sizeof(short));
			_save.ResearchScores.Add(score);
		}

		// Loads the SITE.DAT file (TFTD only).
		string s = _savePath + "/" + "SITE.DAT";
		if (CrossPlatform.FileExists(s))
		{
			byte[] sitedata = BinaryBuffer("SITE.DAT");
			int generatedArtifactSiteMissions = LoadU16(sitedata, 0x24);
			if (generatedArtifactSiteMissions > 0)
			{
				_save.AlienStrategy.AddMissionRun("artifacts", generatedArtifactSiteMissions);

				int spawnedArtifactSites = generatedArtifactSiteMissions;
				sbyte siteTypeToBeSpawned = LoadChar(sitedata, 0x26);
				if (siteTypeToBeSpawned == 'T')
				{
					// before the first hour of the month, the mission was generated already, but the site has not spawned yet
					spawnedArtifactSites--;
				}
				else
				{
					// after the first hour of the month
					// or not an artifact site type
				}
				ids["STR_ARTIFACT_SITE"] = spawnedArtifactSites + 1; // OXC stores the ID of the next site, thus +1
			}
		}

		_save.AllIds = ids;
	}

	/// <summary>Loads the LEASE.DAT file. Contains globe camera settings.</summary>
	private void LoadDatLease()
	{
		byte[] data = BinaryBuffer("LEASE.DAT");

		double lat = -Xcom2Rad(LoadS16(data, 0x00));
		double lon = -Xcom2Rad(LoadS16(data, 0x06));
		_save.GlobeLongitude = lon;
		_save.GlobeLatitude = lat;

		int zoom = LoadS16(data, 0x0C);
		int[] DISTANCE = [90, 120, 180, 360, 450, 720];
		for (int i = 0; i < 6; ++i)
		{
			if (zoom == DISTANCE[i])
			{
				_save.GlobeZoom = i;
				break;
			}
		}
	}

	/// <summary>Loads the XCOM.DAT file. Contains X-COM graph info.</summary>
	private void LoadDatXcom()
	{
		byte[] data = BinaryBuffer("XCOM.DAT");

		int ENTRIES = _rules.Countries.Count + _rules.Regions.Count;
		const int MONTHS = 12;
		for (int i = 0; i < ENTRIES * MONTHS; ++i)
		{
			int score = LoadInt(data, i * sizeof(int));
			int j = i % ENTRIES;
			// country
			if (j < _rules.Countries.Count)
			{
				_save.Countries[j].ActivityXcom.Add(score);
			}
			// region
			else
			{
				j -= _rules.Countries.Count;
				_save.Regions[j].ActivityXcom.Add(score);
			}
		}
	}

	/// <summary>Loads the ALIEN.DAT file. Contains Alien graph info.</summary>
	private void LoadDatAlien()
	{
		byte[] data = BinaryBuffer("ALIEN.DAT");

		int ENTRIES = _rules.Countries.Count + _rules.Regions.Count;
		const int MONTHS = 12;
		for (int i = 0; i < ENTRIES * MONTHS; ++i)
		{
			int score = LoadInt(data, i * sizeof(int));
			int j = i % ENTRIES;
			// country
			if (j < _rules.Countries.Count)
			{
				_save.Countries[j].ActivityAlien.Add(score);
			}
			// region
			else
			{
				j -= _rules.Countries.Count;
				_save.Regions[j].ActivityAlien.Add(score);
			}
		}
	}

	/// <summary>Loads the DIPLOM.DAT file. Contains country status.</summary>
	private void LoadDatDiplom()
	{
		byte[] data = BinaryBuffer("DIPLOM.DAT");

		const int MONTHS = 12;
		var income = new List<long>();
		for (int i = 0; i < MONTHS; ++i)
		{
			income.Add(0);
		}

		const int ENTRY_SIZE = 36;
		for (int i = 0; i < _rules.Countries.Count; ++i)
		{
			int cdata = i * ENTRY_SIZE;
			Country country = _save.Countries[i];

			int satisfaction = LoadS16(data, cdata + 0x02);
			for (int j = 0; j < MONTHS; ++j)
			{
				int funding = LoadS16(data, cdata + 0x04 + j * sizeof(short));
				funding *= 1000;
				income[j] += funding;
				country.Funding.Add(funding);
			}
			bool pact = satisfaction == 0;
			bool newPact = LoadS16(data, cdata + 0x1E) != 0;

			if (pact)
				country.SetPact();
			if (newPact)
				country.SetNewPact();
		}
		_save.Incomes.Clear();
		_save.Incomes.AddRange(income);
	}

	/// <summary>Loads the ZONAL.DAT file. Contains alien region chances.</summary>
	private void LoadDatZonal()
	{
		byte[] data = BinaryBuffer("ZONAL.DAT");

		var chances = new SortedDictionary<string, int>(StringComparer.Ordinal);
		const int REGIONS = 12;
		for (int i = 0; i < REGIONS; ++i)
		{
			chances[_rules.Regions[i]] = LoadU8(data, i);
		}
		var writer = new YamlRootNodeWriter();
		writer.SetAsMap();
		writer.Write("regions", chances);
		_save.AlienStrategy.Load(writer.ToReader(), _mod);
	}

	/// <summary>Loads the ACTS.DAT file. Contains alien mission chances.</summary>
	private void LoadDatActs()
	{
		byte[] data = BinaryBuffer("ACTS.DAT");

		var chances = new SortedDictionary<string, SortedDictionary<string, int>>(StringComparer.Ordinal);
		const int REGIONS = 12;
		const int MISSIONS = 7;
		for (int i = 0; i < REGIONS * MISSIONS; ++i)
		{
			int mission = i % MISSIONS;
			int region = i / MISSIONS;

			string regionName = _rules.Regions[region];
			if (!chances.TryGetValue(regionName, out var missions))
			{
				missions = new SortedDictionary<string, int>(StringComparer.Ordinal);
				chances[regionName] = missions;
			}
			missions[_rules.Missions[mission]] = LoadU8(data, i);
		}

		var writer = new YamlRootNodeWriter();
		writer.SetAsMap();
		if (chances.Count != 0)
		{
			var possibleMisions = writer["possibleMissions"];
			possibleMisions.SetAsSeq();
			foreach (var keyMapPair in chances)
			{
				var regionAndMissions = possibleMisions.Write();
				regionAndMissions.SetAsMap();
				regionAndMissions.Write("region", keyMapPair.Key);
				regionAndMissions.Write("missions", keyMapPair.Value);
			}
		}
		_save.AlienStrategy.Load(writer.ToReader(), _mod);
	}

	/// <summary>Loads the MISSIONS.DAT file. Contains ongoing alien missions.</summary>
	private void LoadDatMissions()
	{
		byte[] data = BinaryBuffer("MISSIONS.DAT");

		const int REGIONS = 12;
		const int MISSIONS = 7;
		const int ENTRY_SIZE = 8;
		for (int i = 0; i < REGIONS * MISSIONS; ++i)
		{
			int mdata = i * ENTRY_SIZE;
			int wave = LoadU16(data, mdata + 0x00);
			if (wave != 0xFFFF)
			{
				int ufoCounter = LoadU16(data, mdata + 0x02);
				int spawn = LoadU16(data, mdata + 0x04);
				int race = LoadU16(data, mdata + 0x06);
				int mission = i % MISSIONS;
				int region = i / MISSIONS;

				var writer = new YamlRootNodeWriter();
				writer.SetAsMap();
				var m = new AlienMission(_mod.GetAlienMission(_rules.Missions[mission], true));
				writer.Write("region", _rules.Regions[region]);
				writer.Write("race", _rules.Crews[race]);
				writer.Write("nextWave", wave);
				writer.Write("nextUfoCounter", ufoCounter);
				writer.Write("spawnCountdown", spawn * 30);
				writer.Write("uniqueID", _save.GetId("ALIEN_MISSIONS"));
				if (m.Rules.Objective == MissionObjective.OBJECTIVE_SITE)
				{
					int missionZone = 3; // pick a city for terror missions
					RuleRegion rule = _mod.GetRegion(_rules.Regions[region], true);
					if (rule.MissionZones.Count <= 3)
					{
						// try to account for TFTD's artefacts and such
						missionZone = 0;
					}
					writer.Write("missionSiteZone", Rng.Generate(0, rule.MissionZones[missionZone].areas.Count - 1));
				}
				m.Load(writer.ToReader(), _save, _mod);
				_save.AlienMissions.Add(m);
				_missions[(mission, region)] = m;
			}
		}
	}

	/// <summary>Loads the LOC.DAT file. Contains globe markers.</summary>
	private void LoadDatLoc()
	{
		byte[] data = BinaryBuffer("LOC.DAT");

		const int ENTRIES = 50;
		int ENTRY_SIZE = data.Length / ENTRIES;
		for (int i = 0; i < ENTRIES; ++i)
		{
			int tdata = i * ENTRY_SIZE;
			var type = (TargetType)LoadU8(data, tdata);

			int dat = LoadU8(data, tdata + 0x01);
			double lon = Xcom2Rad(LoadS16(data, tdata + 0x02));
			double lat = Xcom2Rad(LoadS16(data, tdata + 0x04));
			int timer = LoadS16(data, tdata + 0x06);
			int id = LoadS16(data, tdata + 0x0A);
			int visibility = LoadInt(data, tdata + 0x10);
			bool detected = (visibility & 1) == 0;

			Target target = null;
			Ufo ufo;
			Craft craft;
			Base xbase;
			AlienBase abase;
			Waypoint waypoint;
			MissionSite mission = null;
			switch (type)
			{
				case TargetType.TARGET_NONE:
					target = null;
					break;
				case TargetType.TARGET_UFO:
				case TargetType.TARGET_CRASH:
				case TargetType.TARGET_LANDED:
					ufo = new Ufo(_mod.GetUfo(_rules.Ufos[0], true), 0);
					ufo.Id = id;
					ufo.CrashId = id;
					ufo.LandId = id;
					ufo.SecondsRemaining = timer;
					ufo.Detected = detected;
					target = ufo;
					break;
				case TargetType.TARGET_CRAFT:
					craft = new Craft(_mod.GetCraft(_rules.Crafts[0], true), null, id);
					target = craft;
					break;
				case TargetType.TARGET_XBASE:
					xbase = new Base(_mod);
					target = xbase;
					break;
				case TargetType.TARGET_ABASE:
					abase = new AlienBase(_mod.GetDeployment("STR_ALIEN_BASE_ASSAULT", true), 0);
					abase.Id = id;
					abase.AlienRace = _rules.Crews[dat];
					abase.SetDiscovered(detected);
					_save.AlienBases.Add(abase);
					target = abase;
					break;
				case TargetType.TARGET_WAYPOINT:
					waypoint = new Waypoint();
					waypoint.Id = id;
					_save.Waypoints.Add(waypoint);
					target = waypoint;
					break;
				case TargetType.TARGET_TERROR:
					mission = new MissionSite(_mod.GetAlienMission("STR_ALIEN_TERROR", true), _mod.GetDeployment("STR_TERROR_MISSION", true), null);
					break;
				case TargetType.TARGET_PORT:
					mission = new MissionSite(_mod.GetAlienMission("STR_ALIEN_SURFACE_ATTACK", true), _mod.GetDeployment("STR_PORT_TERROR", true), null);
					break;
				case TargetType.TARGET_ISLAND:
					mission = new MissionSite(_mod.GetAlienMission("STR_ALIEN_SURFACE_ATTACK", true), _mod.GetDeployment("STR_ISLAND_TERROR", true), null);
					break;
				case TargetType.TARGET_SHIP:
					mission = new MissionSite(_mod.GetAlienMission("STR_ALIEN_SHIP_ATTACK", true), _mod.GetDeployment("STR_CARGO_SHIP_P1", true), null);
					break;
				case TargetType.TARGET_ARTEFACT:
					mission = new MissionSite(_mod.GetAlienMission("STR_ALIEN_ARTIFACT", true), _mod.GetDeployment("STR_ARTIFACT_SITE_P1", true), null);
					break;
			}
			if (mission != null)
			{
				mission.Id = id;
				mission.AlienRace = _rules.Crews[dat];
				mission.SecondsRemaining = timer * 3600;
				mission.Detected = detected;
				_save.MissionSites.Add(mission);
				target = mission;
			}
			if (target != null)
			{
				target.Longitude = lon;
				target.Latitude = lat;
			}
			_targets.Add(target);
			_targetDat.Add(dat);
		}
	}

	/// <summary>Loads the BASE.DAT file. Contains X-COM base contents.</summary>
	private void LoadDatBase()
	{
		byte[] data = BinaryBuffer("BASE.DAT");

		const int BASES = 8;
		const int BASE_SIZE = 6;
		const int FACILITIES = BASE_SIZE * BASE_SIZE;
		int ENTRY_SIZE = data.Length / BASES;
		var bases = new Base[BASES];
		for (int i = 0; i < _targets.Count; ++i)
		{
			if (_targets[i] is Base @base)
			{
				int j = _targetDat[i];
				int bdata = j * ENTRY_SIZE;
				string name = LoadString(data, bdata);
				// facilities
				for (int k = 0; k < FACILITIES; ++k)
				{
					int facilityType = LoadU8(data, bdata + _rules.GetOffset("BASE.DAT_FACILITIES") + k);
					if (facilityType < _rules.Facilities.Count)
					{
						var facility = new BaseFacility(_mod.GetBaseFacility(_rules.Facilities[facilityType], true), @base);
						int x = k % BASE_SIZE;
						int y = k / BASE_SIZE;
						int days = LoadU8(data, bdata + _rules.GetOffset("BASE.DAT_FACILITIES") + FACILITIES + k);
						facility.X = x;
						facility.Y = y;
						facility.BuildTime = days;
						@base.Facilities.Add(facility);
					}
				}
				int engineers = LoadU8(data, bdata + _rules.GetOffset("BASE.DAT_ENGINEERS"));
				int scientists = LoadU8(data, bdata + _rules.GetOffset("BASE.DAT_SCIENTISTS"));
				// items
				for (int k = 0; k < _rules.Items.Count; ++k)
				{
					int qty = LoadU16(data, bdata + _rules.GetOffset("BASE.DAT_ITEMS") + k * 2);
					if (qty != 0 && !string.IsNullOrEmpty(_rules.Items[k]))
					{
						RuleItem rule = _mod.GetItem(_rules.Items[k], true);
						@base.StorageItems.AddItem(rule, qty);
					}
				}
				@base.Engineers = engineers;
				@base.Scientists = scientists;
				@base.SetName(name);
				bases[j] = @base;
			}
		}

		foreach (var b in bases)
		{
			if (b != null)
			{
				_save.Bases.Add(b);
			}
		}
	}

	/// <summary>Loads the ASTORE.DAT file. Contains X-COM alien containment.</summary>
	private void LoadDatAStore()
	{
		byte[] data = BinaryBuffer("ASTORE.DAT");

		const int ENTRY_SIZE = 12;
		int ENTRIES = data.Length / ENTRY_SIZE;
		for (int i = 0; i < ENTRIES; ++i)
		{
			int adata = i * ENTRY_SIZE;
			int race = LoadU8(data, adata + 0x00);
			string liveAlien = "";
			if (race != 0)
			{
				int rank = LoadU8(data, adata + 0x01);
				int @base = LoadU8(data, adata + 0x02);
				liveAlien = _rules.AlienRaces[race];
				liveAlien += _rules.AlienRanks[rank];
				if (@base != 0xFF)
				{
					var b = _targets[@base] as Base;
					RuleItem rule = _mod.GetItem(liveAlien, true);
					b.StorageItems.AddItem(rule);
				}
			}
			_aliens.Add(liveAlien);
		}
	}

	/// <summary>Loads the TRANSFER.DAT file. Contains transfers to X-COM bases.</summary>
	private void LoadDatTransfer()
	{
		byte[] data = BinaryBuffer("TRANSFER.DAT");

		const int ENTRY_SIZE = 8;
		int ENTRIES = data.Length / ENTRY_SIZE;
		for (int i = 0; i < ENTRIES; ++i)
		{
			int tdata = i * ENTRY_SIZE;
			int qty = LoadU8(data, tdata + 0x06);
			if (qty != 0)
			{
				int baseSrc = LoadU8(data, tdata + 0x00);
				int baseDest = LoadU8(data, tdata + 0x01);
				var b = _targets[baseDest] as Base;
				int hours = LoadU8(data, tdata + 0x02);
				var type = (TransferType)LoadU8(data, tdata + 0x03);
				int dat = LoadU8(data, tdata + 0x04);

				var transfer = new Transfer(hours);
				switch (type)
				{
					case TransferType.TRANSFER_CRAFT:
						if (baseSrc == 255)
						{
							string newCraft = _rules.Crafts[dat];
							transfer.Craft = new Craft(_mod.GetCraft(newCraft, true), b, _save.GetId(newCraft));
						}
						else
						{
							transfer.Craft = _targets[dat] as Craft;
						}
						break;
					case TransferType.TRANSFER_SOLDIER:
						transfer.Soldier = _soldiers[dat];
						break;
					case TransferType.TRANSFER_SCIENTIST:
						transfer.SetScientists(qty);
						break;
					case TransferType.TRANSFER_ENGINEER:
						transfer.SetEngineers(qty);
						break;
					default:
						if (type == TransferType.TRANSFER_ITEM)
							transfer.SetItems(_mod.GetItem(_rules.Items[dat], true), qty);
						else
							transfer.SetItems(_mod.GetItem(_aliens[dat], true));
						break;
				}

				b.Transfers.Add(transfer);
			}
		}
	}

	/// <summary>Loads the CRAFT.DAT file. Contains X-COM craft and Alien UFOs.</summary>
	private void LoadDatCraft()
	{
		byte[] data = BinaryBuffer("CRAFT.DAT");

		int ENTRY_SIZE = data.Length / _targets.Count;
		for (int i = 0; i < _targets.Count; ++i)
		{
			int j = _targetDat[i];
			int cdata = j * ENTRY_SIZE;
			int type = LoadU8(data, cdata);
			if (type != 0xFF)
			{
				var writer = new YamlRootNodeWriter();
				writer.SetAsMap();
				if (_targets[i] is Craft craft)
				{
					craft.ChangeRules(_mod.GetCraft(_rules.Crafts[type], true));

					int lweapon = LoadU8(data, cdata + _rules.GetOffset("CRAFT.DAT_LEFT_WEAPON"));
					int lammo = LoadU16(data, cdata + _rules.GetOffset("CRAFT.DAT_LEFT_AMMO"));
					if (lweapon != 0xFF)
					{
						var cw = new CraftWeapon(_mod.GetCraftWeapon(_rules.CraftWeapons[lweapon], true), lammo);
						craft.Weapons[0] = cw;
					}
					int flight = LoadU8(data, cdata + _rules.GetOffset("CRAFT.DAT_FLIGHT"));
					int rweapon = LoadU8(data, cdata + _rules.GetOffset("CRAFT.DAT_RIGHT_WEAPON"));
					int rammo = LoadU8(data, cdata + _rules.GetOffset("CRAFT.DAT_RIGHT_AMMO"));
					if (rweapon != 0xFF)
					{
						var cw = new CraftWeapon(_mod.GetCraftWeapon(_rules.CraftWeapons[rweapon], true), rammo);
						craft.Weapons[1] = cw;
					}

					writer.Write("damage", (int)LoadU16(data, cdata + _rules.GetOffset("CRAFT.DAT_DAMAGE")));
					writer.Write("speed", (int)LoadU16(data, cdata + _rules.GetOffset("CRAFT.DAT_SPEED")));
					int dest = LoadU16(data, cdata + _rules.GetOffset("CRAFT.DAT_DESTINATION"));
					writer.Write("fuel", (int)LoadU16(data, cdata + _rules.GetOffset("CRAFT.DAT_FUEL")));
					int @base = LoadU16(data, cdata + _rules.GetOffset("CRAFT.DAT_BASE"));
					writer.Write("status", xcomStatus[LoadU16(data, cdata + _rules.GetOffset("CRAFT.DAT_STATUS"))]);

					// vehicles
					const int VEHICLES = 5;
					for (int k = 0; k < VEHICLES; ++k)
					{
						int qty = LoadU8(data, cdata + _rules.GetOffset("CRAFT.DAT_ITEMS") + k);
						for (int v = 0; v < qty; ++v)
						{
							RuleItem rule = _mod.GetItem(_rules.Items[k + 10], true);
							craft.Vehicles.Add(new Vehicle(rule, rule.VehicleClipSize, 4));
						}
					}
					// items
					const int ITEMS = 50;
					for (int k = VEHICLES; k < VEHICLES + ITEMS; ++k)
					{
						int qty = LoadU8(data, cdata + _rules.GetOffset("CRAFT.DAT_ITEMS") + k);
						if (qty != 0 && !string.IsNullOrEmpty(_rules.Items[k + 10]))
						{
							RuleItem rule = _mod.GetItem(_rules.Items[k + 10], true);
							craft.Items.AddItem(rule, qty);
						}
					}

					int state = LoadInt(data, cdata + _rules.GetOffset("CRAFT.DAT_STATE"));
					writer.Write("lowFuel", (state & (1 << 1)) != 0);

					craft.Load(writer.ToReader(), _mod, _save);

					if (flight != 0 && dest != 0xFFFF)
					{
						Target t = _targets[dest];
						craft.Destination = t;
					}
					if (@base != 0xFFFF)
					{
						var b = _targets[@base] as Base;
						craft.SetBase(b, false);
						b.Crafts.Add(craft);
					}
				}
				if (_targets[i] is Ufo ufo)
				{
					ufo.ChangeRules(_mod.GetUfo(_rules.Ufos[type - 5], true));
					writer.Write("damage", (int)LoadU16(data, cdata + _rules.GetOffset("CRAFT.DAT_DAMAGE")));
					writer.Write("altitude", xcomAltitudes[LoadU16(data, cdata + _rules.GetOffset("CRAFT.DAT_ALTITUDE"))]);
					writer.Write("speed", (int)LoadU16(data, cdata + _rules.GetOffset("CRAFT.DAT_SPEED")));
					var destWriter = writer["dest"];
					destWriter.SetAsMap();
					destWriter.Write("lon", Xcom2Rad(LoadS16(data, cdata + _rules.GetOffset("CRAFT.DAT_DEST_LON"))));
					destWriter.Write("lat", Xcom2Rad(LoadS16(data, cdata + _rules.GetOffset("CRAFT.DAT_DEST_LAT"))));

					int mission = LoadU16(data, cdata + _rules.GetOffset("CRAFT.DAT_MISSION"));
					int region = LoadU16(data, cdata + _rules.GetOffset("CRAFT.DAT_REGION"));
					string trajectory = "";
					_missions.TryGetValue((mission, region), out AlienMission m);
					if (m == null)
					{
						var missionWriter = new YamlRootNodeWriter();
						missionWriter.SetAsMap();
						m = new AlienMission(_mod.GetAlienMission(_rules.Missions[mission], true));
						missionWriter.Write("region", _rules.Regions[region]);
						missionWriter.Write("race", _rules.Crews[LoadU16(data, cdata + _rules.GetOffset("CRAFT.DAT_RACE"))]);
						missionWriter.Write("nextWave", 1);
						missionWriter.Write("nextUfoCounter", 0);
						missionWriter.Write("spawnCountdown", 1000);
						missionWriter.Write("uniqueID", _save.GetId("ALIEN_MISSIONS"));
						m.Load(missionWriter.ToReader(), _save, _mod);
						_save.AlienMissions.Add(m);
						_missions[(mission, region)] = m;
						if (mission == 6)
						{
							trajectory += UfoTrajectory.RETALIATION_ASSAULT_RUN;
						}
					}
					writer.Write("mission", m.Id);
					m.IncreaseLiveUfos();
					if (trajectory.Length == 0)
					{
						trajectory += "P" + LoadU16(data, cdata + _rules.GetOffset("CRAFT.DAT_TRAJECTORY")).ToString(CultureInfo.InvariantCulture);
					}
					writer.Write("trajectory", trajectory);
					writer.Write("trajectoryPoint", (int)LoadU16(data, cdata + _rules.GetOffset("CRAFT.DAT_TRAJECTORY_POINT")));
					int state = LoadInt(data, cdata + _rules.GetOffset("CRAFT.DAT_STATE"));
					writer.Write("hyperDetected", (state & (1 << 6)) != 0);

					ufo.Load(writer.ToReader(), _mod, _save);
					ufo.Speed = ufo.Speed;
					if (ufo.Status == Ufo.UfoStatus.CRASHED)
					{
						ufo.SecondsRemaining = ufo.SecondsRemaining * 3600;
					}
					else if (ufo.Status == Ufo.UfoStatus.LANDED)
					{
						ufo.SecondsRemaining = ufo.SecondsRemaining * 5;
					}
					else
					{
						ufo.SecondsRemaining = 0;
					}

					_save.Ufos.Add(ufo);
				}
			}
		}
	}

	/// <summary>Loads the SOLDIER.DAT file. Contains X-COM soldiers.</summary>
	private void LoadDatSoldier()
	{
		byte[] data = BinaryBuffer("SOLDIER.DAT");

		const int SOLDIERS = 250;
		int ENTRY_SIZE = data.Length / SOLDIERS;
		for (int i = 0; i < SOLDIERS; ++i)
		{
			int sdata = i * ENTRY_SIZE;
			int rank = LoadU16(data, sdata + _rules.GetOffset("SOLDIER.DAT_RANK"));
			if (rank != 0xFFFF)
			{
				var writer = new YamlRootNodeWriter();
				writer.SetAsMap();
				int @base = LoadU16(data, sdata + _rules.GetOffset("SOLDIER.DAT_BASE"));
				int craft = LoadU16(data, sdata + _rules.GetOffset("SOLDIER.DAT_CRAFT"));
				writer.Write("missions", (int)LoadS16(data, sdata + _rules.GetOffset("SOLDIER.DAT_MISSIONS")));
				writer.Write("kills", (int)LoadS16(data, sdata + _rules.GetOffset("SOLDIER.DAT_KILLS")));
				writer.Write("recovery", (int)LoadS16(data, sdata + _rules.GetOffset("SOLDIER.DAT_RECOVERY")));
				writer.Write("name", LoadString(data, sdata + _rules.GetOffset("SOLDIER.DAT_NAME")));
				writer.Write("rank", rank);

				var initial = new UnitStats();
				initial.tu = LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_INITIAL_TU"));
				initial.health = LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_INITIAL_HE"));
				initial.stamina = LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_INITIAL_STA"));
				initial.reactions = LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_INITIAL_RE"));
				initial.strength = LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_INITIAL_STR"));
				initial.firing = LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_INITIAL_FA"));
				initial.throwing = LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_INITIAL_TA"));
				initial.melee = LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_INITIAL_ME"));
				initial.psiStrength = LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_INITIAL_PST"));
				initial.psiSkill = LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_INITIAL_PSK"));
				initial.bravery = (short)(110 - (10 * LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_INITIAL_BR"))));
				writer.Write("initialStats", initial);

				var current = new UnitStats();
				current.tu = LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_IMPROVED_TU"));
				current.health = LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_IMPROVED_HE"));
				current.stamina = LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_IMPROVED_STA"));
				current.reactions = LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_IMPROVED_RE"));
				current.strength = LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_IMPROVED_STR"));
				current.firing = LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_IMPROVED_FA"));
				current.throwing = LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_IMPROVED_TA"));
				current.melee = LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_IMPROVED_ME"));
				current.psiStrength = 0;
				current.psiSkill = 0;
				current.bravery = (short)(10 * LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_IMPROVED_BR")));
				current += initial;
				writer.Write("currentStats", current);

				int armor = LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_ARMOR"));
				List<string> armors = _rules.Armor;
				if (armor >= 0 && armor < armors.Count)
				{
					writer.Write("armor", armors[armor]);
				}
				else
				{
					throw new InvalidOperationException("Invalid armor index. Modded saves are not supported.");
				}
				writer.Write("improvement", (int)LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_PSI")));
				writer.Write("psiTraining", LoadChar(data, sdata + _rules.GetOffset("SOLDIER.DAT_PSILAB")) != 0);
				writer.Write("gender", (int)LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_GENDER")));
				writer.Write("look", (int)LoadU8(data, sdata + _rules.GetOffset("SOLDIER.DAT_LOOK")));
				writer.Write("id", _save.GetId("STR_SOLDIER"));

				var soldier = new Soldier(_mod.GetSoldier(_mod.SoldiersList[0], true), null, 0 /*nationality*/);
				soldier.Load(writer.ToReader(), _mod, _save);
				if (@base != 0xFFFF)
				{
					var b = _targets[@base] as Base;
					b.Soldiers.Add(soldier);
				}
				if (craft != 0xFFFF)
				{
					var c = _targets[craft] as Craft;
					soldier.SetCraft(c);
				}
				_soldiers.Add(soldier);
			}
			else
			{
				_soldiers.Add(null);
			}
		}
	}

	/// <summary>Loads the RESEARCH.DAT file. Contains X-COM research progress.</summary>
	private void LoadDatResearch()
	{
		byte[] data = BinaryBuffer("RESEARCH.DAT");

		int ENTRY_SIZE = data.Length / _rules.Research.Count;
		for (int i = 0; i < _rules.Research.Count; ++i)
		{
			int rdata = i * ENTRY_SIZE;
			if (!string.IsNullOrEmpty(_rules.Research[i]))
			{
				RuleResearch research = _mod.GetResearch(_rules.Research[i]);
				if (research != null && research.Cost != 0)
				{
					bool discovered = LoadU8(data, rdata + 0x0A) != 0;
					bool popped = LoadU8(data, rdata + 0x12) != 0;
					if (discovered)
					{
						_save.AddFinishedResearch(research, _mod, null, false);
					}
					else if (popped)
					{
						_save.AddPoppedResearch(research);
					}
				}
			}
		}
	}

	/// <summary>Loads the UP.DAT file. Contains X-COM Ufopaedia progress.</summary>
	private void LoadDatUp()
	{
		byte[] data = BinaryBuffer("UP.DAT");

		int ENTRY_SIZE = data.Length / _rules.Ufopaedia.Count;
		for (int i = 0; i < _rules.Ufopaedia.Count; ++i)
		{
			int rdata = i * ENTRY_SIZE;
			ArticleDefinition article = _mod.GetUfopaediaArticle(_rules.Ufopaedia[i]);
			if (article != null && article.section != Ufopaedia.UFOPAEDIA_NOT_AVAILABLE)
			{
				bool discovered = LoadU8(data, rdata + 0x08) == 2;
				if (discovered)
				{
					foreach (var req in article._requires)
					{
						RuleResearch research = _mod.GetResearch(req);
						if (research != null && research.Cost == 0)
						{
							_save.AddFinishedResearch(research, _mod, null, false);
						}
					}
				}
			}
		}
	}

	/// <summary>Loads the PROJECT.DAT file. Contains X-COM research projects.</summary>
	private void LoadDatProject()
	{
		byte[] data = BinaryBuffer("PROJECT.DAT");

		int ENTRIES = _rules.Research.Count;
		// days (Uint16) | scientists (Uint8)
		int ENTRY_SIZE = ENTRIES * (sizeof(ushort) + sizeof(byte));
		for (int i = 0; i < _save.Bases.Count; ++i)
		{
			Base @base = _save.Bases[i];
			int pdata = i * ENTRY_SIZE;
			int arrRemaining = pdata;
			int arrScientists = arrRemaining + ENTRIES * sizeof(ushort);
			for (int j = 0; j < _rules.Research.Count; ++j)
			{
				int remaining = LoadU16(data, arrRemaining + j * sizeof(ushort));
				int scientists = LoadU8(data, arrScientists + j);
				if (remaining != 0 && !string.IsNullOrEmpty(_rules.Research[j]))
				{
					RuleResearch research = _mod.GetResearch(_rules.Research[j]);
					if (research != null && research.Cost != 0)
					{
						var project = new ResearchProject(research, research.Cost);
						project.Assigned = scientists;
						project.Spent = project.Cost - remaining;
						@base.AddResearch(project);
						@base.Scientists = @base.Scientists - scientists;
					}
				}
			}
		}
	}

	/// <summary>Loads the BPROD.DAT file. Contains X-COM manufacture projects.</summary>
	private void LoadDatBProd()
	{
		byte[] data = BinaryBuffer("BPROD.DAT");

		int ENTRIES = _rules.Manufacture.Count;
		// hours (int) | engineers (Uint16) | quantity (Uint16)| produced (Uint16)
		int ENTRY_SIZE = ENTRIES * (sizeof(int) + 3 * sizeof(ushort));
		for (int i = 0; i < _save.Bases.Count; ++i)
		{
			Base @base = _save.Bases[i];
			int pdata = i * ENTRY_SIZE;
			int arrRemaining = pdata;
			int arrEngineers = arrRemaining + ENTRIES * sizeof(int);
			int arrTotal = arrEngineers + ENTRIES * sizeof(ushort);
			int arrProduced = arrTotal + ENTRIES * sizeof(ushort);
			for (int j = 0; j < _rules.Manufacture.Count; ++j)
			{
				int remaining = LoadInt(data, arrRemaining + j * sizeof(int));
				int engineers = LoadU16(data, arrEngineers + j * sizeof(ushort));
				int total = LoadU16(data, arrTotal + j * sizeof(ushort));
				int produced = LoadU16(data, arrProduced + j * sizeof(ushort));
				if (remaining != 0 && !string.IsNullOrEmpty(_rules.Manufacture[j]))
				{
					RuleManufacture manufacture = _mod.GetManufacture(_rules.Manufacture[j]);
					if (manufacture != null)
					{
						var project = new Production(manufacture, total);
						project.AssignedEngineers = engineers;
						project.TimeSpent = produced * manufacture.ManufactureTime + manufacture.ManufactureTime - remaining;
						@base.AddProduction(project);
						@base.Engineers = @base.Engineers - engineers;
					}
				}
			}
		}
	}

	/// <summary>Loads the XBASES.DAT file. Contains targeted X-COM bases.</summary>
	private void LoadDatXBases()
	{
		byte[] data = BinaryBuffer("XBASES.DAT");
		const int REGIONS = 12;
		for (int i = 0; i < REGIONS; ++i)
		{
			int bdata = i * 4;
			bool detected = LoadU16(data, bdata + 0x00) != 0;
			if (detected)
			{
				int loc = LoadU16(data, bdata + 0x02);
				if (_targets[loc] is Base @base)
				{
					@base.SetRetaliationTarget(true);
				}
			}
		}
	}
}
