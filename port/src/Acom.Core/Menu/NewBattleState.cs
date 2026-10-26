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
// Ported from: src/Menu/NewBattleState.h, src/Menu/NewBattleState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

public enum NewBattleSelectType
{
	MISSION = 0,
	TERRAIN = 1,
	ALIENRACE = 2,
	GLOBETEXTURE = 3,
}

/// <summary>New Battle that displays a list of options to configure a new standalone mission.</summary>
public class NewBattleState : State
{
	private const int TFTD_DEPLOYMENTS = 22;

	private readonly Window _window;
	private readonly Frame _frameLeft, _frameRight;
	private readonly Text _txtTitle, _txtMapOptions, _txtAlienOptions;
	private readonly Text _txtMission, _txtCraft, _txtDarkness, _txtTerrain, _txtDifficulty, _txtAlienRace, _txtAlienTech, _txtDepth;
	private readonly ComboBox _cbxMission, _cbxCraft, _cbxTerrain, _cbxDifficulty, _cbxAlienRace;
	private readonly Slider _slrDarkness, _slrAlienTech, _slrDepth;
	private readonly TextButton _btnOk, _btnCancel, _btnEquip, _btnRandom;
	private readonly TextButton _btnMission, _btnTerrain, _btnAlienRace;
	private readonly Text _txtGlobeTexture;
	private readonly TextButton _btnGlobeTexture;
	private readonly TextButton _btnGlobeTextureToggle;
	private readonly ToggleTextButton _btnUfoLanded;
	private readonly TextList _lstSelect;
	private readonly TextEdit _btnQuickSearch;
	/// <summary>Visibility of all surfaces while the advanced selector is open (C++ map keyed by pointer: order does not matter).</summary>
	private readonly Dictionary<Surface, bool> _surfaceBackup = [];
	private readonly List<string> _missionTypes = [], _terrainTypes = [], _crafts = [];
	private List<string> _alienRaces = [];
	private readonly List<string> _globeTextures = [];
	private readonly List<int> _globeTextureIDs = [];
	private Craft _craft;
	private NewBattleSelectType _selectType;
	private bool _isRightClick;
	private bool _depthVisible, _globeTextureVisible;
	private int _selectedGlobeTexture;
	private readonly List<int> _filtered = [];

	/// <summary>Initializes all the elements in the New Battle window.</summary>
	public NewBattleState()
	{
		_craft = null;
		_selectType = NewBattleSelectType.MISSION;
		_isRightClick = false;
		_depthVisible = false;
		_globeTextureVisible = false;
		_selectedGlobeTexture = 0;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0, WindowPopup.POPUP_BOTH);
		_btnQuickSearch = new TextEdit(this, 48, 9, 264, 183);
		_txtTitle = new Text(304, 17, 8, 9);

		_txtMapOptions = new Text(148, 9, 8, 68);
		_frameLeft = new Frame(148, 96, 8, 78);
		_txtAlienOptions = new Text(148, 9, 164, 68);
		_frameRight = new Frame(148, 96, 164, 78);

		_btnUfoLanded = new ToggleTextButton(100, 16, 212, 8);

		_txtMission = new Text(100, 9, 8, 30);
		_cbxMission = new ComboBox(this, 214, 16, 98, 26);
		_btnMission = new TextButton(16, 16, 81, 26);

		_txtCraft = new Text(100, 9, 8, 50);
		_cbxCraft = new ComboBox(this, 106, 16, 98, 46);
		_btnEquip = new TextButton(106, 16, 206, 46);

		_txtDarkness = new Text(120, 9, 22, 83);
		_slrDarkness = new Slider(120, 16, 22, 93);

		_txtTerrain = new Text(120, 9, 22, 113);
		_cbxTerrain = new ComboBox(this, 120, 16, 22 + 9, 123);
		_btnTerrain = new TextButton(16, 16, 5 + 9, 123);

		_txtDepth = new Text(120, 9, 22, 143);
		_slrDepth = new Slider(120, 16, 22, 153);

		_txtGlobeTexture = new Text(120, 9, 22, 143);
		_btnGlobeTexture = new TextButton(120, 16, 22, 153);
		_btnGlobeTextureToggle = new TextButton(16, 16, 145, 153);

		_txtDifficulty = new Text(120, 9, 178, 83);
		_cbxDifficulty = new ComboBox(this, 120, 16, 178, 93);

		_txtAlienRace = new Text(120, 9, 178, 113);
		_cbxAlienRace = new ComboBox(this, 120, 16, 178 + 9, 123);
		_btnAlienRace = new TextButton(16, 16, 161 + 9, 123);

		_txtAlienTech = new Text(120, 9, 178, 143);
		_slrAlienTech = new Slider(120, 16, 178, 153);

		_btnOk = new TextButton(100, 16, 8, 176);
		_btnCancel = new TextButton(100, 16, 110, 176);
		_btnRandom = new TextButton(100, 16, 212, 176);

		_lstSelect = new TextList(288, 144, 8, 28);

		// Set palette
		SetInterface("newBattleMenu");

		Add(_window, "window", "newBattleMenu");
		Add(_btnQuickSearch, "button1", "newBattleMenu");
		Add(_txtTitle, "heading", "newBattleMenu");
		Add(_txtMapOptions, "heading", "newBattleMenu");
		Add(_frameLeft, "frames", "newBattleMenu");
		Add(_txtAlienOptions, "heading", "newBattleMenu");
		Add(_frameRight, "frames", "newBattleMenu");

		Add(_btnUfoLanded, "button1", "newBattleMenu");

		Add(_txtMission, "text", "newBattleMenu");
		Add(_txtCraft, "text", "newBattleMenu");
		Add(_btnEquip, "button1", "newBattleMenu");

		Add(_txtDarkness, "text", "newBattleMenu");
		Add(_slrDarkness, "button1", "newBattleMenu");
		Add(_txtDepth, "text", "newBattleMenu");
		Add(_slrDepth, "button1", "newBattleMenu");
		Add(_txtGlobeTexture, "text", "newBattleMenu");
		Add(_btnGlobeTexture, "button1", "newBattleMenu");
		Add(_btnGlobeTextureToggle, "button1", "newBattleMenu");
		Add(_txtTerrain, "text", "newBattleMenu");
		Add(_txtDifficulty, "text", "newBattleMenu");
		Add(_txtAlienRace, "text", "newBattleMenu");
		Add(_txtAlienTech, "text", "newBattleMenu");
		Add(_slrAlienTech, "button1", "newBattleMenu");

		Add(_btnOk, "button2", "newBattleMenu");
		Add(_btnCancel, "button2", "newBattleMenu");
		Add(_btnRandom, "button2", "newBattleMenu");

		Add(_btnMission, "button1", "newBattleMenu");
		Add(_btnTerrain, "button1", "newBattleMenu");
		Add(_btnAlienRace, "button1", "newBattleMenu");

		Add(_lstSelect, "list", "newBattleMenu");

		Add(_cbxTerrain, "button1", "newBattleMenu");
		Add(_cbxAlienRace, "button1", "newBattleMenu");
		Add(_cbxDifficulty, "button1", "newBattleMenu");
		Add(_cbxCraft, "button1", "newBattleMenu");
		Add(_cbxMission, "button1", "newBattleMenu");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "newBattleMenu");

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetBig();
		_txtTitle.SetText(Tr("STR_MISSION_GENERATOR"));

		_txtMapOptions.SetText(Tr("STR_MAP_OPTIONS"));

		_frameLeft.SetThickness(3);

		_txtAlienOptions.SetText(Tr("STR_ALIEN_OPTIONS"));

		_frameRight.SetThickness(3);

		_btnUfoLanded.Text = Tr("STR_LANDED");
		_btnUfoLanded.Visible = Options.oxceCrashedOrLanded > 0;
		_btnUfoLanded.Pressed = Options.oxceCrashedOrLanded > 1;
		_txtTitle.Align = _btnUfoLanded.Visible ? TextHAlign.ALIGN_LEFT : TextHAlign.ALIGN_CENTER;

		_txtMission.SetText(Tr("STR_MISSION"));

		_txtCraft.SetText(Tr("STR_CRAFT"));

		_txtDarkness.SetText(Tr("STR_MAP_DARKNESS"));

		_txtDepth.SetText(Tr("STR_MAP_DEPTH"));

		_txtTerrain.SetText(Tr("STR_MAP_TERRAIN"));

		_txtGlobeTexture.SetText(Tr("STR_GLOBE_TEXTURE"));
		_txtGlobeTexture.Visible = false;

		_txtDifficulty.SetText(Tr("STR_DIFFICULTY"));

		_txtAlienRace.SetText(Tr("STR_ALIEN_RACE"));

		_txtAlienTech.SetText(Tr("STR_ALIEN_TECH_LEVEL"));

		var mod = _game.Mod;
		if (Options.debug)
		{
			_missionTypes.AddRange(mod.DeploymentsList);
		}
		else
		{
			foreach (var deploymentName in mod.DeploymentsList)
			{
				var depl = mod.GetDeployment(deploymentName);
				if (depl != null && !depl.IsHidden)
				{
					_missionTypes.Add(deploymentName);
				}
			}
		}
		// ignore, hardcoded
		_missionTypes.Remove(RuleCraft.DEFAULT_CRAFT_DEPLOYMENT_PREVIEW);
		_cbxMission.SetOptions(_missionTypes, true);
		_cbxMission.OnChange(CbxMissionChange);

		foreach (var craftType in mod.CraftsList)
		{
			RuleCraft rule = mod.GetCraft(craftType);
			if (rule.IsForNewBattle)
			{
				_crafts.Add(craftType);
			}
		}
		_cbxCraft.SetOptions(_crafts, true);
		_cbxCraft.OnChange(CbxCraftChange);

		_slrDarkness.SetRange(0, 15);

		_slrDepth.SetRange(1, 3);

		_cbxTerrain.OnChange(CbxTerrainChange);

		foreach (var pair in mod.Globe.TexturesRaw)
		{
			if (pair.Key >= 0 && !pair.Value.IsCosmeticOcean)
			{
				_globeTextures.Add("GLOBE_TEXTURE_" + pair.Key.ToString(System.Globalization.CultureInfo.InvariantCulture));
				_globeTextureIDs.Add(pair.Key);
			}
		}

		_btnGlobeTexture.Text = Tr(_globeTextures[_selectedGlobeTexture]);
		_btnGlobeTexture.OnMouseClick(BtnGlobeTextureChange);
		_btnGlobeTexture.OnMouseClick(BtnGlobeTextureChange, SDLMouse.SDL_BUTTON_RIGHT);
		_btnGlobeTexture.Visible = false;

		_btnGlobeTextureToggle.Text = "*";
		_btnGlobeTextureToggle.OnMouseClick(BtnGlobeTextureToggle);
		_btnGlobeTextureToggle.Visible = false;

		var difficulty = new List<string>
		{
			Tr("STR_1_BEGINNER"),
			Tr("STR_2_EXPERIENCED"),
			Tr("STR_3_VETERAN"),
			Tr("STR_4_GENIUS"),
			Tr("STR_5_SUPERHUMAN"),
		};
		_cbxDifficulty.SetOptions(difficulty);

		_slrAlienTech.SetRange(0, mod.AlienItemLevels.Count - 1);
		if (mod.AlienItemLevels.Count <= 1)
		{
			_slrAlienTech.Visible = false;
			_txtAlienTech.Visible = false;
		}

		_btnEquip.Text = Tr("STR_EQUIP_CRAFT");
		_btnEquip.OnMouseClick(BtnEquipClick);

		_btnRandom.Text = Tr("STR_RANDOMIZE");
		_btnRandom.OnMouseClick(BtnRandomClick);

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);

		_btnCancel.Text = Tr("STR_CANCEL");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		Load();

		// -------------------------------

		bool showExtraButtons = _missionTypes.Count > TFTD_DEPLOYMENTS;
		if (!showExtraButtons)
		{
			_cbxTerrain.X = _txtTerrain.X;
			_cbxAlienRace.X = _txtAlienRace.X;
		}

		_btnMission.Text = "...";
		_btnMission.OnMouseClick(BtnMissionChange);
		_btnMission.OnMouseClick(BtnMissionChange, SDLMouse.SDL_BUTTON_RIGHT);
		_btnMission.Visible = showExtraButtons;

		_btnTerrain.Text = "...";
		_btnTerrain.OnMouseClick(BtnTerrainChange);
		_btnTerrain.OnMouseClick(BtnTerrainChange, SDLMouse.SDL_BUTTON_RIGHT);
		_btnTerrain.Visible = showExtraButtons;

		_btnAlienRace.Text = "...";
		_btnAlienRace.OnMouseClick(BtnAlienRaceChange);
		_btnAlienRace.OnMouseClick(BtnAlienRaceChange, SDLMouse.SDL_BUTTON_RIGHT);
		_btnAlienRace.Visible = showExtraButtons;

		_lstSelect.SetColumns(1, 280);
		_lstSelect.SetBackground(_window);
		_lstSelect.SetAlign(TextHAlign.ALIGN_CENTER);
		_lstSelect.Margin = 8;
		_lstSelect.SetSelectable(true);
		_lstSelect.OnMouseClick(LstSelectClick);
		_lstSelect.OnMouseClick(LstSelectClick, SDLMouse.SDL_BUTTON_RIGHT);
		_lstSelect.OnMouseClick(LstSelectClick, SDLMouse.SDL_BUTTON_MIDDLE);
		_lstSelect.Visible = false;

		_btnQuickSearch.Text = ""; // redraw
		_btnQuickSearch.OnChange(BtnQuickSearchApply);
		_btnQuickSearch.Visible = false;

		_btnCancel.OnKeyboardRelease(BtnQuickSearchToggle, Options.keyToggleQuickSearch);
	}

	/// <summary>Handle key shortcuts.</summary>
	public override void Handle(Action action)
	{
		base.Handle(action);

		if (action.Details.type == SDLEventType.SDL_KEYDOWN)
		{
			// F11 - show/hide "UFO crashed" toggle button
			if (action.Details.key.keysym.sym == SDLKey.SDLK_F11)
			{
				_btnUfoLanded.Visible = !_btnUfoLanded.Visible;
				_txtTitle.Align = _btnUfoLanded.Visible ? TextHAlign.ALIGN_LEFT : TextHAlign.ALIGN_CENTER;
			}
		}
	}

	/// <summary>Resets the menu music and savegame when coming back from the battlescape.</summary>
	public override void Init()
	{
		base.Init();

		if (_craft == null)
		{
			Load();
		}
	}

	/// <summary>C++ std::min(value, size - 1) on size_t: a count of 0 wraps around and leaves the value unchanged.</summary>
	internal static int ClampIndex(int value, int count) => (int)Math.Min((uint)value, (uint)(count - 1));

	/// <summary>Loads new battle data from a YAML file.</summary>
	/// <param name="filename">YAML filename.</param>
	public void Load(string filename = "battle")
	{
		var mod = _game.Mod;
		string s = Options.GetMasterUserFolder() + filename + ".cfg";
		if (!CrossPlatform.FileExists(s))
		{
			InitSave();
		}
		else
		{
			try
			{
				var cfgReader = YamlRootNodeReader.FromFile(s);
				_cbxMission.Selected = ClampIndex(cfgReader["mission"].ReadVal(0), _missionTypes.Count);
				CbxMissionChange(null);
				_cbxCraft.Selected = ClampIndex(cfgReader["craft"].ReadVal(0), _crafts.Count);
				_slrDarkness.Value = cfgReader["darkness"].ReadVal(0);
				_cbxTerrain.Selected = ClampIndex(cfgReader["terrain"].ReadVal(0), _terrainTypes.Count);
				CbxTerrainChange(null);
				{
					_selectedGlobeTexture = ClampIndex(cfgReader["globeTexture"].ReadVal(0), _globeTextures.Count);
					_btnGlobeTexture.Text = Tr(_globeTextures[_selectedGlobeTexture]);
				}
				_cbxAlienRace.Selected = ClampIndex(cfgReader["alienRace"].ReadVal(0), _alienRaces.Count);
				_cbxDifficulty.Selected = cfgReader["difficulty"].ReadVal(0);
				_slrAlienTech.Value = cfgReader["alienTech"].ReadVal(0);

				if (cfgReader["base"])
				{
					var save = new SavedGame();

					var @base = new Base(mod);
					@base.Load(cfgReader["base"], save, false);
					save.Bases.Add(@base);

					// Add research
					save.MakeAllResearchDiscovered(mod);

					// Generate items
					@base.StorageItems.Clear();
					foreach (var itemType in mod.ItemsList)
					{
						RuleItem rule = mod.GetItem(itemType);
						if (rule.BattleType != BattleType.BT_CORPSE && rule.IsRecoverable)
						{
							@base.StorageItems.AddItem(rule, 1);
						}
					}

					// Fix invalid contents
					if (@base.Crafts.Count == 0)
					{
						string craftType = _crafts[_cbxCraft.Selected];
						_craft = new Craft(mod.GetCraft(craftType), @base, save.GetId(craftType));
						@base.Crafts.Add(_craft);
					}
					else
					{
						_craft = @base.Crafts[0];
					}

					_game.SavedGame = save;
				}
				else
				{
					InitSave();
				}
			}
			catch (YamlReadException e)
			{
				Logger.Warning(e.Message);
				InitSave();
			}
		}

		var starterBaseReader = new YamlRootNodeReader(mod.DefaultStartingBase, "(starting base template)");
		var globalTemplates = starterBaseReader["globalTemplates"];
		if (globalTemplates)
		{
			_game.SavedGame.LoadTemplates(globalTemplates, mod);
		}
		var ufopediaRuleStatus = starterBaseReader["ufopediaRuleStatus"];
		if (ufopediaRuleStatus)
		{
			_game.SavedGame.LoadUfopediaRuleStatus(ufopediaRuleStatus);
		}
	}

	/// <summary>Saves new battle data to a YAML file.</summary>
	/// <param name="filename">YAML filename.</param>
	public void Save(string filename = "battle")
	{
		var writer = new YamlRootNodeWriter();
		writer.SetAsMap();
		writer.Write("mission", _cbxMission.Selected);
		writer.Write("craft", _cbxCraft.Selected);
		writer.Write("darkness", _slrDarkness.Value);
		writer.Write("terrain", _cbxTerrain.Selected);
		writer.Write("globeTexture", _selectedGlobeTexture);
		writer.Write("alienRace", _cbxAlienRace.Selected);
		writer.Write("difficulty", _cbxDifficulty.Selected);
		writer.Write("alienTech", _slrAlienTech.Value);
		_game.SavedGame.Bases[0].Save(writer["base"]);

		string filepath = Options.GetMasterUserFolder() + filename + ".cfg";
		if (!CrossPlatform.WriteFile(filepath, writer.Emit()))
		{
			Logger.Warning($"Failed to save {filepath}");
			return;
		}
	}

	/// <summary>Initializes a new savegame with everything available.</summary>
	public void InitSave()
	{
		var mod = _game.Mod;
		var save = new SavedGame();
		var @base = new Base(mod);
		var startingBaseReader = new YamlRootNodeReader(mod.DefaultStartingBase, "(starting base template)");
		@base.Load(startingBaseReader.ToBase(), save, true, true);
		save.Bases.Add(@base);

		// Kill everything we don't want in this base
		@base.Soldiers.Clear();
		@base.Crafts.Clear();
		@base.StorageItems.Clear();

		_craft = new Craft(mod.GetCraft(_crafts[_cbxCraft.Selected]), @base, 1);
		@base.Crafts.Add(_craft);

		// Generate soldiers
		bool psiStrengthEval = Options.psiStrengthEval && save.IsResearched(mod.PsiRequirements);
		for (int i = 0; i < 30; ++i)
		{
			int randomType = Rng.Generate(0, mod.SoldiersList.Count - 1);
			RuleSoldier ruleSoldier = mod.GetSoldier(mod.SoldiersList[randomType], true);
			int nationality = save.SelectSoldierNationalityByLocation(mod, ruleSoldier, null); // -1
			Soldier soldier = mod.GenSoldier(save, ruleSoldier, nationality);

			for (int n = 0; n < 5; ++n)
			{
				if (Rng.Percent(70))
					continue;
				soldier.PromoteRank();

				ref UnitStats stats = ref soldier.CurrentStatsEditable;
				stats.tu = (short)(stats.tu + Rng.Generate(0, 5));
				stats.stamina = (short)(stats.stamina + Rng.Generate(0, 5));
				stats.health = (short)(stats.health + Rng.Generate(0, 5));
				stats.bravery = (short)(stats.bravery + Rng.Generate(0, 5));
				stats.reactions = (short)(stats.reactions + Rng.Generate(0, 5));
				stats.firing = (short)(stats.firing + Rng.Generate(0, 5));
				stats.throwing = (short)(stats.throwing + Rng.Generate(0, 5));
				stats.strength = (short)(stats.strength + Rng.Generate(0, 5));
				stats.mana = (short)(stats.mana + Rng.Generate(0, 5));
				stats.psiStrength = (short)(stats.psiStrength + Rng.Generate(0, 5));
				stats.melee = (short)(stats.melee + Rng.Generate(0, 5));
				stats.psiSkill = (short)(stats.psiSkill + Rng.Generate(0, 20));
			}
			{
				ref UnitStats stats = ref soldier.CurrentStatsEditable;
				stats.bravery = (short)((int)Math.Ceiling(stats.bravery / 10.0) * 10); // keep it a multiple of 10
			}

			// update again, could have been changed since soldier creation
			soldier.CalcStatString(mod.StatStrings, psiStrengthEval);

			@base.Soldiers.Add(soldier);

			int space = _craft.SpaceAvailable;
			if (_craft.ValidateAddingSoldier(space, soldier) == CraftPlacementErrors.CPE_None)
			{
				soldier.SetCraft(_craft);
			}
		}

		// Generate items
		foreach (var itemType in mod.ItemsList)
		{
			RuleItem rule = mod.GetItem(itemType);
			if (rule.BattleType != BattleType.BT_CORPSE && rule.IsRecoverable)
			{
				int howMany = rule.BattleType == BattleType.BT_AMMO ? 2 : 1;
				@base.StorageItems.AddItem(rule, howMany);
				if (rule.BattleType != BattleType.BT_NONE && rule.IsInventoryItem)
				{
					_craft.Items.AddItem(rule, howMany);
				}
			}
		}

		// Add research
		save.MakeAllResearchDiscovered(mod);

		_game.SavedGame = save;
		CbxMissionChange(null);
	}

	/// <summary>Starts the battle.</summary>
	public void BtnOkClick(Action action)
	{
		var mod = _game.Mod;
		if (_craft != null)
		{
			// just in case somebody manually edited battle.cfg
			_craft.ResetCustomDeployment();
		}
		Save();
		if (_missionTypes[_cbxMission.Selected] != "STR_BASE_DEFENSE" && _craft.NumTotalUnits == 0)
		{
			return;
		}

		var bgame = new SavedBattleGame(mod, _game.Language);
		_game.SavedGame.SetBattleGame(bgame);
		bgame.MissionType = _missionTypes[_cbxMission.Selected];
		var bgen = new BattlescapeGenerator(_game);
		Base @base = null;

		bgen.SetTerrain(mod.GetTerrain(_terrainTypes[_cbxTerrain.Selected]));

		if (_globeTextureVisible)
		{
			int textureId = _globeTextureIDs[_selectedGlobeTexture];
			var globeTexture = mod.Globe.GetTexture(textureId);
			bgen.SetWorldTexture(null, globeTexture);
		}

		// base defense
		if (_missionTypes[_cbxMission.Selected] == "STR_BASE_DEFENSE")
		{
			@base = _craft.Base;
			bgen.SetBase(@base);
			_craft = null;
		}
		// alien base
		else if (mod.GetDeployment(bgame.MissionType).IsAlienBase)
		{
			var b = new AlienBase(mod.GetDeployment(bgame.MissionType), -1);
			b.Id = 1;
			b.AlienRace = _alienRaces[_cbxAlienRace.Selected];
			_craft.Destination = b;
			bgen.SetAlienBase(b);
			_game.SavedGame.AlienBases.Add(b);
		}
		// ufo assault
		else if (_craft != null && mod.GetUfo(_missionTypes[_cbxMission.Selected]) != null)
		{
			var u = new Ufo(mod.GetUfo(_missionTypes[_cbxMission.Selected]), 1);
			u.Id = 1;
			_craft.Destination = u;
			bgen.SetUfo(u);
			// either ground assault or ufo crash
			bool ufoLanded = _btnUfoLanded.Visible ? _btnUfoLanded.Pressed : Rng.Generate(0, 1) == 1;
			if (ufoLanded)
			{
				u.Status = Ufo.UfoStatus.LANDED;
				bgame.MissionType = "STR_UFO_GROUND_ASSAULT";
			}
			else
			{
				u.Status = Ufo.UfoStatus.CRASHED;
				bgame.MissionType = "STR_UFO_CRASH_RECOVERY";
			}
			_game.SavedGame.Ufos.Add(u);
		}
		// mission site
		else
		{
			AlienDeployment deployment = mod.GetDeployment(bgame.MissionType);
			RuleAlienMission mission = mod.GetAlienMission(mod.AlienMissionList[0]); // doesn't matter
			var m = new MissionSite(mission, deployment, null);
			m.Id = 1;
			m.AlienRace = _alienRaces[_cbxAlienRace.Selected];
			_craft.Destination = m;
			bgen.SetMissionSite(m);
			_game.SavedGame.MissionSites.Add(m);
		}

		if (_craft != null)
		{
			_craft.Speed = 0;
			bgen.SetCraft(_craft);
		}

		_game.SavedGame.Difficulty = (GameDifficulty)_cbxDifficulty.Selected;

		bgen.SetWorldShade(_slrDarkness.Value);
		bgen.SetAlienRace(_alienRaces[_cbxAlienRace.Selected]);
		bgen.SetAlienItemlevel(_slrAlienTech.Value);
		bgame.Depth = _slrDepth.Value;

		bgen.Run();

		_game.PopState();
		_game.PopState();
		_game.PushState(new BriefingState(_craft, @base));
		_craft = null;
	}

	/// <summary>Returns to the previous screen.</summary>
	public void BtnCancelClick(Action action)
	{
		if (_surfaceBackup.Count != 0)
		{
			Cleanup();
			return;
		}

		Save();
		_game.SavedGame = null;
		_game.PopState();
	}

	/// <summary>Randomize the state.</summary>
	public void BtnRandomClick(Action action)
	{
		InitSave();

		_cbxMission.Selected = Rng.Generate(0, _missionTypes.Count - 1);
		CbxMissionChange(null);
		_cbxCraft.Selected = Rng.Generate(0, _crafts.Count - 1);
		CbxCraftChange(null);
		_slrDarkness.Value = Rng.Generate(0, 15);
		_cbxTerrain.Selected = Rng.Generate(0, _terrainTypes.Count - 1);
		CbxTerrainChange(null);
		_cbxAlienRace.Selected = Rng.Generate(0, _alienRaces.Count - 1);
		_cbxDifficulty.Selected = Rng.Generate(0, 4);
		_slrAlienTech.Value = Rng.Generate(0, _game.Mod.AlienItemLevels.Count - 1);
	}

	/// <summary>Shows the Craft Info screen.</summary>
	public void BtnEquipClick(Action action)
	{
		_game.PushState(new CraftInfoState(_game.SavedGame.Bases[0], 0));
	}

	/// <summary>Updates Map Options based on the current Mission type.</summary>
	public void CbxMissionChange(Action action)
	{
		var mod = _game.Mod;
		AlienDeployment ruleDeploy = mod.GetDeployment(_missionTypes[_cbxMission.Selected]);
		var terrains = new SortedSet<string>(StringComparer.Ordinal);

		// Get terrains associated with this mission
		List<string> deployTerrains, globeTerrains;
		deployTerrains = ruleDeploy.Terrains;
		if (deployTerrains.Count == 0)
		{
			globeTerrains = mod.Globe.GetTerrains("");
		}
		else
		{
			globeTerrains = mod.Globe.GetTerrains(ruleDeploy.Type);
		}
		foreach (var terrain in deployTerrains)
		{
			terrains.Add(terrain);
		}
		foreach (var terrain in globeTerrains)
		{
			terrains.Add(terrain);
		}
		_terrainTypes.Clear();
		var terrainStrings = new List<string>();
		foreach (var terrain in terrains)
		{
			_terrainTypes.Add(terrain);
			terrainStrings.Add("MAP_" + terrain);
		}

		// Hide controls that don't apply to mission
		_txtDarkness.Visible = ruleDeploy.Shade == -1;
		_slrDarkness.Visible = ruleDeploy.Shade == -1;
		_txtTerrain.Visible = _terrainTypes.Count > 1;
		_cbxTerrain.Visible = _terrainTypes.Count > 1;
		_cbxTerrain.SetOptions(terrainStrings, true);
		_cbxTerrain.Selected = 0;
		_btnTerrain.Visible = _missionTypes.Count > TFTD_DEPLOYMENTS && _terrainTypes.Count > 1;
		CbxTerrainChange(null);
	}

	/// <summary>Updates craft accordingly.</summary>
	public void CbxCraftChange(Action action)
	{
		_craft.ChangeRules(_game.Mod.GetCraft(_crafts[_cbxCraft.Selected]));

		int count = 0;
		var tmpCraft = new Craft(_craft.Rules, _craft.Base, 0);

		// temporarily re-assign all soldiers to a dummy craft
		foreach (var soldier in _craft.Base.Soldiers)
		{
			if (soldier.Craft == _craft)
			{
				soldier.SetCraft(tmpCraft);
				count++;
			}
		}
		// try assigning all soldiers back while validating constraints
		foreach (var soldier in _craft.Base.Soldiers)
		{
			if (count <= 0)
			{
				break;
			}
			if (soldier.Craft == tmpCraft)
			{
				count--;
				int space = _craft.SpaceAvailable;
				if (_craft.ValidateAddingSoldier(space, soldier) == CraftPlacementErrors.CPE_None)
				{
					soldier.SetCraft(_craft);
				}
				else
				{
					soldier.SetCraft(null);
				}
			}
		}

		// FIXME? HWPs can still violate the constraints (e.g. when switching from Avenger to Lightning)
	}

	/// <summary>Updates the depth slider accordingly when terrain selection changes.</summary>
	public void CbxTerrainChange(Action action)
	{
		var mod = _game.Mod;
		AlienDeployment ruleDeploy = mod.GetDeployment(_missionTypes[_cbxMission.Selected]);
		int minDepth = 0;
		int maxDepth = 0;
		if (ruleDeploy.MaxDepth > 0 || mod.GetTerrain(_terrainTypes[_cbxTerrain.Selected]).MaxDepth > 0 ||
			(ruleDeploy.Terrains.Count != 0 && mod.GetTerrain(ruleDeploy.Terrains[0]).MaxDepth > 0))
		{
			minDepth = 1;
			maxDepth = 3;
		}
		_depthVisible = minDepth != maxDepth;
		_txtDepth.Visible = _depthVisible;
		_slrDepth.Visible = _depthVisible;
		_slrDepth.SetRange(minDepth, maxDepth);
		_slrDepth.Value = minDepth;

		{
			int found = ruleDeploy.HasTextureBasedScript(mod);
			if (found == -1)
			{
				// there is no map script on the alien deployment at all, perform the check on the terrain
				var ruleTerrain = mod.GetTerrain(_terrainTypes[_cbxTerrain.Selected]);
				found = ruleTerrain.HasTextureBasedScript(mod);
			}
			_globeTextureVisible = found == 1;

			_txtGlobeTexture.Visible = _globeTextureVisible;
			_btnGlobeTexture.Visible = _globeTextureVisible;

			// if both are visible
			if (_globeTextureVisible && _depthVisible)
			{
				// hide the depth slider
				_txtDepth.Visible = false;
				_slrDepth.Visible = false;
				// and allow toggling between the texture selector and depth slider
				_btnGlobeTextureToggle.Visible = true;
			}
			else
			{
				// not needed
				_btnGlobeTextureToggle.Visible = false;
			}
		}

		// Get races "supported" by this mission
		_alienRaces = [.. mod.AlienRacesList];
		int maxAlienRank = ruleDeploy.MaxAlienRank;
		for (int i = 0; i < _alienRaces.Count;)
		{
			string alienRace = _alienRaces[i];
			if (alienRace.Contains("_UNDERWATER", StringComparison.Ordinal))
			{
				_alienRaces.RemoveAt(i);
			}
			else
			{
				string raceName = (minDepth != maxDepth) ? alienRace + "_UNDERWATER" : alienRace;
				var raceRules = mod.GetAlienRace(raceName);
				if (raceRules == null || maxAlienRank >= raceRules.Members)
				{
					// not enough members or race doesn't exist
					_alienRaces.RemoveAt(i);
				}
				else
				{
					++i;
				}
			}
		}
		_cbxAlienRace.SetOptions(_alienRaces, true);
		if ((uint)_cbxAlienRace.Selected >= (uint)_alienRaces.Count)
		{
			_cbxAlienRace.Selected = 0;
		}
	}

	/// <summary>Shows the advanced mission selector.</summary>
	public void BtnMissionChange(Action action)
	{
		FillList(NewBattleSelectType.MISSION, _game.IsRightClick(action));
	}

	/// <summary>Shows the advanced terrain selector.</summary>
	public void BtnTerrainChange(Action action)
	{
		FillList(NewBattleSelectType.TERRAIN, _game.IsRightClick(action));
	}

	/// <summary>Shows the advanced globe texture selector.</summary>
	public void BtnGlobeTextureChange(Action action)
	{
		FillList(NewBattleSelectType.GLOBETEXTURE, _game.IsRightClick(action));
	}

	/// <summary>Toggles between the globe texture selector and depth slider.</summary>
	public void BtnGlobeTextureToggle(Action action)
	{
		_txtDepth.Visible = !_txtDepth.Visible;
		_slrDepth.Visible = !_slrDepth.Visible;

		_txtGlobeTexture.Visible = !_txtGlobeTexture.Visible;
		_btnGlobeTexture.Visible = !_btnGlobeTexture.Visible;
	}

	/// <summary>Shows the advanced alien race selector.</summary>
	public void BtnAlienRaceChange(Action action)
	{
		FillList(NewBattleSelectType.ALIENRACE, _game.IsRightClick(action));
	}

	/// <summary>Fills the advanced selector with data.</summary>
	private void FillList(NewBattleSelectType selectType, bool isRightClick)
	{
		_selectType = selectType;
		_isRightClick = isRightClick;

		bool firstRun = false;
		if (_surfaceBackup.Count == 0)
		{
			firstRun = true;
			foreach (var surface in _surfaces)
			{
				_surfaceBackup[surface] = surface.Visible;
				surface.Visible = false;
			}
			_window.Visible = true;
			_txtTitle.Visible = true;
			_btnCancel.Visible = true;
			_btnRandom.Visible = false;
			_lstSelect.Visible = true;
			_btnQuickSearch.Visible = Options.oxceQuickSearchButton;
		}

		string searchString = Unicode.UpperCase(_btnQuickSearch.Text);

		int counter = 0;
		void fill(List<string> list, bool prefix, int scroll)
		{
			foreach (var m in list)
			{
				if (searchString.Length != 0)
				{
					string itemName;
					if (_isRightClick) { itemName = m; } else { itemName = Tr(prefix ? "MAP_" + m : m); }
					itemName = Unicode.UpperCase(itemName);
					if (!itemName.Contains(searchString, StringComparison.Ordinal))
					{
						counter++;
						continue;
					}
				}
				_filtered.Add(counter);
				counter++;
				_lstSelect.AddRow(1, _isRightClick ? m : Tr(prefix ? "MAP_" + m : m));
			}
			if (firstRun && _lstSelect.IsScrollbarVisible) _lstSelect.ScrollTo(scroll);
		}

		_filtered.Clear();
		_lstSelect.ClearList();
		if (_selectType == NewBattleSelectType.MISSION)
		{
			fill(_missionTypes, false, _cbxMission.Selected);
		}
		else if (_selectType == NewBattleSelectType.TERRAIN)
		{
			fill(_terrainTypes, true, _cbxTerrain.Selected);
		}
		else if (_selectType == NewBattleSelectType.GLOBETEXTURE)
		{
			fill(_globeTextures, false, _selectedGlobeTexture);
		}
		else if (_selectType == NewBattleSelectType.ALIENRACE)
		{
			fill(_alienRaces, false, _cbxAlienRace.Selected);
		}
	}

	/// <summary>
	/// L-click: Selects an item from the advanced selector and closes the selector.
	/// R-click: Displays the code of the selected item.
	/// M-click: Displays the translation of the selected item.
	/// </summary>
	public void LstSelectClick(Action action)
	{
		int selected = (int)_lstSelect.SelectedRow;

		// quick toggle
		if (_game.IsRightClick(action) || _game.IsMiddleClick(action))
		{
			var list =
				_selectType == NewBattleSelectType.MISSION ? _missionTypes :
				_selectType == NewBattleSelectType.TERRAIN ? _terrainTypes :
				_selectType == NewBattleSelectType.GLOBETEXTURE ? _globeTextures : _alienRaces;
			string s = list[_filtered[selected]];
			if (_game.IsMiddleClick(action))
			{
				s = Tr((_selectType == NewBattleSelectType.TERRAIN) ? "MAP_" + s : s);
			}
			_lstSelect.SetCellText(selected, 0, s);
			return;
		}

		// restore the GUI
		Cleanup();

		// select the new value
		if (_selectType == NewBattleSelectType.MISSION)
		{
			_cbxMission.Selected = _filtered[selected];
			CbxMissionChange(null);
		}
		else if (_selectType == NewBattleSelectType.TERRAIN)
		{
			_cbxTerrain.Selected = _filtered[selected];
			CbxTerrainChange(null);
		}
		else if (_selectType == NewBattleSelectType.GLOBETEXTURE)
		{
			_selectedGlobeTexture = _filtered[selected];
			_btnGlobeTexture.Text = Tr(_globeTextures[_selectedGlobeTexture]);
		}
		else if (_selectType == NewBattleSelectType.ALIENRACE)
		{
			_cbxAlienRace.Selected = _filtered[selected];
		}
	}

	/// <summary>Restores the GUI after closing the advanced selector.</summary>
	private void Cleanup()
	{
		_btnQuickSearch.Text = "";
		_btnQuickSearch.Visible = false;

		foreach (var surfacePair in _surfaceBackup)
		{
			surfacePair.Key.Visible = surfacePair.Value;
		}
		_surfaceBackup.Clear();
	}

	/// <summary>Quick search toggle.</summary>
	public void BtnQuickSearchToggle(Action action)
	{
		if (_surfaceBackup.Count == 0)
		{
			return;
		}

		if (_btnQuickSearch.Visible)
		{
			_btnQuickSearch.Text = "";
			_btnQuickSearch.Visible = false;
			BtnQuickSearchApply(action);
		}
		else
		{
			_btnQuickSearch.Visible = true;
			_btnQuickSearch.SetFocus(true);
		}
	}

	/// <summary>Quick search.</summary>
	public void BtnQuickSearchApply(Action action)
	{
		if (!Options.oxceInstantQuickSearch && action != null && action.Details.type == SDLEventType.SDL_KEYDOWN)
		{
			SDLKey sym = action.Details.key.keysym.sym;
			if (sym != SDLKey.SDLK_RETURN && sym != SDLKey.SDLK_KP_ENTER && sym != SDLKey.SDLK_ESCAPE)
				return;
		}
		FillList(_selectType, _isRightClick);
	}
}
