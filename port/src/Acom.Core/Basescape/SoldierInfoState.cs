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
// Ported from: src/Basescape/SoldierInfoState.h, src/Basescape/SoldierInfoState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)
#pragma warning disable IDE0060 // handler signatures keep the unused Action parameter like C++

/// <summary>
/// Soldier Info screen that shows all the
/// info of a specific soldier.
/// </summary>
public class SoldierInfoState : State
{
	private readonly Base _base;
	private int _soldierId; // C++ size_t
	private readonly bool _forceLimits;
	private readonly bool _readOnly;
	private readonly bool _noTransformations;
	private Soldier _soldier;
	private readonly List<Soldier> _list;
	private readonly Surface _bg;
	private readonly InteractiveSurface _rank;
	private readonly InteractiveSurface _flag;
	private readonly TextButton _btnOk, _btnPrev, _btnNext, _btnArmor, _btnSack, _btnDiary, _btnBonuses, _btnTransformations;
	private readonly Text _txtRank, _txtMissions, _txtKills, _txtCraft, _txtRecovery, _txtPsionic, _txtDead;
	private readonly Text _txtStuns;
	private readonly TextEdit _edtSoldier;
	private readonly Text _txtTimeUnits, _txtStamina, _txtHealth, _txtBravery, _txtReactions, _txtFiring, _txtThrowing, _txtMelee, _txtStrength, _txtPsiStrength, _txtPsiSkill, _txtMana;
	private readonly Text _numTimeUnits, _numStamina, _numHealth, _numBravery, _numReactions, _numFiring, _numThrowing, _numMelee, _numStrength, _numPsiStrength, _numPsiSkill, _numMana;
	private readonly Bar _barTimeUnits, _barStamina, _barHealth, _barBravery, _barReactions, _barFiring, _barThrowing, _barMelee, _barStrength, _barPsiStrength, _barPsiSkill, _barMana;

	/// <summary>
	/// Initializes all the elements in the Soldier Info screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from. NULL to use the dead soldiers list.</param>
	/// <param name="soldierId">ID of the selected soldier.</param>
	/// <param name="forceLimits">Force storage limits when leaving the screen.</param>
	/// <param name="readOnly">Hide the armor/sack/transformation buttons.</param>
	public SoldierInfoState(Base @base, int soldierId, bool forceLimits = true, bool readOnly = false)
	{
		_base = @base;
		_soldierId = soldierId;
		_forceLimits = forceLimits;
		_readOnly = readOnly;
		_noTransformations = false;
		_soldier = null;

		if (_base == null)
		{
			_list = _game.SavedGame.DeadSoldiers;
			if ((uint)_soldierId >= (uint)_list.Count)
			{
				_soldierId = 0;
			}
			else
			{
				_soldierId = _list.Count - (1 + _soldierId);
			}
		}
		else
		{
			_list = _base.Soldiers;
		}

		// Create objects
		_bg = new Surface(320, 200, 0, 0);
		_rank = new InteractiveSurface(26, 23, 4, 4);
		_flag = new InteractiveSurface(40, 20, 275, 6);
		_btnPrev = new TextButton(28, 14, 0, 33);
		_btnOk = new TextButton(48, 14, 30, 33);
		_btnNext = new TextButton(28, 14, 80, 33);
		_btnArmor = new TextButton(110, 14, 130, 33);
		_btnBonuses = new TextButton(16, 14, 242, 33);
		_btnTransformations = new TextButton(18, 14, 110, 33);
		_edtSoldier = new TextEdit(this, 210, 16, 40, 9);
		_btnSack = new TextButton(60, 14, 260, 33);
		_btnDiary = new TextButton(60, 14, 260, 48);
		_txtRank = new Text(130, 9, 0, 48);
		_txtMissions = new Text(100, 9, 130, 48);
		_txtKills = new Text(100, 9, 200, 48);
		_txtStuns = new Text(60, 9, 260, 48);
		_txtCraft = new Text(130, 9, 0, 56);
		_txtRecovery = new Text(180, 9, 130, 56);
		_txtPsionic = new Text(150, 9, 0, 66);
		_txtDead = new Text(150, 9, 130, 33);

		int yPos = 80;
		int step = 11;
		if (_game.Mod.IsManaFeatureEnabled)
		{
			yPos = 81;
			step = 10;
		}

		_txtTimeUnits = new Text(120, 9, 6, yPos);
		_numTimeUnits = new Text(18, 9, 131, yPos);
		_barTimeUnits = new Bar(170, 7, 150, yPos);
		yPos += step;

		_txtStamina = new Text(120, 9, 6, yPos);
		_numStamina = new Text(18, 9, 131, yPos);
		_barStamina = new Bar(170, 7, 150, yPos);
		yPos += step;

		_txtHealth = new Text(120, 9, 6, yPos);
		_numHealth = new Text(18, 9, 131, yPos);
		_barHealth = new Bar(170, 7, 150, yPos);
		yPos += step;

		_txtBravery = new Text(120, 9, 6, yPos);
		_numBravery = new Text(18, 9, 131, yPos);
		_barBravery = new Bar(170, 7, 150, yPos);
		yPos += step;

		_txtReactions = new Text(120, 9, 6, yPos);
		_numReactions = new Text(18, 9, 131, yPos);
		_barReactions = new Bar(170, 7, 150, yPos);
		yPos += step;

		_txtFiring = new Text(120, 9, 6, yPos);
		_numFiring = new Text(18, 9, 131, yPos);
		_barFiring = new Bar(170, 7, 150, yPos);
		yPos += step;

		_txtThrowing = new Text(120, 9, 6, yPos);
		_numThrowing = new Text(18, 9, 131, yPos);
		_barThrowing = new Bar(170, 7, 150, yPos);
		yPos += step;

		_txtMelee = new Text(120, 9, 6, yPos);
		_numMelee = new Text(18, 9, 131, yPos);
		_barMelee = new Bar(170, 7, 150, yPos);
		yPos += step;

		_txtStrength = new Text(120, 9, 6, yPos);
		_numStrength = new Text(18, 9, 131, yPos);
		_barStrength = new Bar(170, 7, 150, yPos);
		yPos += step;

		if (_game.Mod.IsManaFeatureEnabled)
		{
			_txtMana = new Text(120, 9, 6, yPos);
			_numMana = new Text(18, 9, 131, yPos);
			_barMana = new Bar(170, 7, 150, yPos);
			yPos += step;
		}

		_txtPsiStrength = new Text(120, 9, 6, yPos);
		_numPsiStrength = new Text(18, 9, 131, yPos);
		_barPsiStrength = new Bar(170, 7, 150, yPos);
		yPos += step;

		_txtPsiSkill = new Text(120, 9, 6, yPos);
		_numPsiSkill = new Text(18, 9, 131, yPos);
		_barPsiSkill = new Bar(170, 7, 150, yPos);

		// Set palette
		SetInterface("soldierInfo");

		Add(_bg);
		Add(_rank);
		Add(_flag);
		Add(_btnOk, "button", "soldierInfo");
		Add(_btnPrev, "button", "soldierInfo");
		Add(_btnNext, "button", "soldierInfo");
		Add(_btnArmor, "button", "soldierInfo");
		Add(_btnBonuses, "button", "soldierInfo");
		Add(_btnTransformations, "button", "soldierInfo");
		Add(_edtSoldier, "text1", "soldierInfo");
		Add(_btnSack, "button", "soldierInfo");
		Add(_btnDiary, "button", "soldierInfo");
		Add(_txtRank, "text1", "soldierInfo");
		Add(_txtMissions, "text1", "soldierInfo");
		Add(_txtKills, "text1", "soldierInfo");
		Add(_txtStuns, "text1", "soldierInfo");
		Add(_txtCraft, "text1", "soldierInfo");
		Add(_txtRecovery, "text1", "soldierInfo");
		Add(_txtPsionic, "text2", "soldierInfo");
		Add(_txtDead, "text2", "soldierInfo");

		Add(_txtTimeUnits, "text2", "soldierInfo");
		Add(_numTimeUnits, "numbers", "soldierInfo");
		Add(_barTimeUnits, "barTUs", "soldierInfo");

		Add(_txtStamina, "text2", "soldierInfo");
		Add(_numStamina, "numbers", "soldierInfo");
		Add(_barStamina, "barEnergy", "soldierInfo");

		Add(_txtHealth, "text2", "soldierInfo");
		Add(_numHealth, "numbers", "soldierInfo");
		Add(_barHealth, "barHealth", "soldierInfo");

		Add(_txtBravery, "text2", "soldierInfo");
		Add(_numBravery, "numbers", "soldierInfo");
		Add(_barBravery, "barBravery", "soldierInfo");

		Add(_txtReactions, "text2", "soldierInfo");
		Add(_numReactions, "numbers", "soldierInfo");
		Add(_barReactions, "barReactions", "soldierInfo");

		Add(_txtFiring, "text2", "soldierInfo");
		Add(_numFiring, "numbers", "soldierInfo");
		Add(_barFiring, "barFiring", "soldierInfo");

		Add(_txtThrowing, "text2", "soldierInfo");
		Add(_numThrowing, "numbers", "soldierInfo");
		Add(_barThrowing, "barThrowing", "soldierInfo");

		Add(_txtMelee, "text2", "soldierInfo");
		Add(_numMelee, "numbers", "soldierInfo");
		Add(_barMelee, "barMelee", "soldierInfo");

		Add(_txtStrength, "text2", "soldierInfo");
		Add(_numStrength, "numbers", "soldierInfo");
		Add(_barStrength, "barStrength", "soldierInfo");

		if (_game.Mod.IsManaFeatureEnabled)
		{
			Add(_txtMana, "text2", "soldierInfo");
			Add(_numMana, "numbers", "soldierInfo");
			Add(_barMana, "barMana", "soldierInfo");
		}

		Add(_txtPsiStrength, "text2", "soldierInfo");
		Add(_numPsiStrength, "numbers", "soldierInfo");
		Add(_barPsiStrength, "barPsiStrength", "soldierInfo");

		Add(_txtPsiSkill, "text2", "soldierInfo");
		Add(_numPsiSkill, "numbers", "soldierInfo");
		Add(_barPsiSkill, "barPsiSkill", "soldierInfo");

		CenterAllSurfaces();

		// Set up objects
		_game.Mod.GetSurface("BACK06.SCR").BlitNShade(_bg, 0, 0);

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_btnPrev.Text = "<<";
		if (_base == null)
		{
			_btnPrev.OnMouseClick(BtnNextClick);
			_btnPrev.OnKeyboardPress(BtnNextClick, Options.keyBattlePrevUnit);
		}
		else
		{
			_btnPrev.OnMouseClick(BtnPrevClick);
			_btnPrev.OnKeyboardPress(BtnPrevClick, Options.keyBattlePrevUnit);
		}

		_btnNext.Text = ">>";
		if (_base == null)
		{
			_btnNext.OnMouseClick(BtnPrevClick);
			_btnNext.OnKeyboardPress(BtnPrevClick, Options.keyBattleNextUnit);
		}
		else
		{
			_btnNext.OnMouseClick(BtnNextClick);
			_btnNext.OnKeyboardPress(BtnNextClick, Options.keyBattleNextUnit);
		}

		_btnArmor.Text = Tr("STR_ARMOR");
		_btnArmor.OnMouseClick(BtnArmorClick);
		if (_readOnly)
		{
			_btnArmor.Visible = false;
		}

		_btnBonuses.Text = Tr("STR_BONUSES_BUTTON"); // tiny button, default translation is " "
		_btnBonuses.OnMouseClick(BtnBonusesClick);

		_btnTransformations.Text = Tr("STR_TRANSFORMATIONS_BUTTON");
		_btnTransformations.OnMouseClick(BtnTransformationsClick);

		var availableTransformations = new List<RuleSoldierTransformation>();
		if (_base != null)
		{
			_game.SavedGame.GetAvailableTransformations(availableTransformations, _game.Mod, _base);
		}
		if (availableTransformations.Count == 0)
		{
			_noTransformations = true;
		}

		_edtSoldier.SetBig();
		_edtSoldier.OnChange(EdtSoldierChange);
		_edtSoldier.OnMousePress(EdtSoldierPress);

		// Can't change nationality of dead soldiers
		if (_base != null)
		{
			// Ignore also if flags are used to indicate number of kills
			if (_game.Mod.FlagByKills.Count == 0)
			{
				_flag.OnMouseClick(BtnFlagClick, SDLMouse.SDL_BUTTON_LEFT);
				_flag.OnMouseClick(BtnFlagClick, SDLMouse.SDL_BUTTON_RIGHT);
			}

			_rank.OnMouseClick(BtnRankClick);
		}

		_btnSack.Text = Tr("STR_SACK");
		_btnSack.OnMouseClick(BtnSackClick);

		_btnDiary.Text = Tr("STR_DIARY");
		_btnDiary.OnMouseClick(BtnDiaryClick);
		_btnDiary.Visible = Options.soldierDiaries;

		_txtPsionic.SetText(Tr("STR_IN_PSIONIC_TRAINING"));

		_txtTimeUnits.SetText(Tr("STR_TIME_UNITS"));

		_barTimeUnits.Scale = 1.0;

		_txtStamina.SetText(Tr("STR_STAMINA"));

		_barStamina.Scale = 1.0;

		_txtHealth.SetText(Tr("STR_HEALTH"));

		_barHealth.Scale = 1.0;

		_txtBravery.SetText(Tr("STR_BRAVERY"));

		_barBravery.Scale = 1.0;

		_txtReactions.SetText(Tr("STR_REACTIONS"));

		_barReactions.Scale = 1.0;

		_txtFiring.SetText(Tr("STR_FIRING_ACCURACY"));

		_barFiring.Scale = 1.0;

		_txtThrowing.SetText(Tr("STR_THROWING_ACCURACY"));

		_barThrowing.Scale = 1.0;

		_txtMelee.SetText(Tr("STR_MELEE_ACCURACY"));

		_barMelee.Scale = 1.0;

		_txtStrength.SetText(Tr("STR_STRENGTH"));

		_barStrength.Scale = 1.0;

		if (_game.Mod.IsManaFeatureEnabled)
		{
			_txtMana.SetText(Tr("STR_MANA_POOL"));
			_barMana.Scale = 1.0;
		}

		_txtPsiStrength.SetText(Tr("STR_PSIONIC_STRENGTH"));

		_barPsiStrength.Scale = 1.0;

		_txtPsiSkill.SetText(Tr("STR_PSIONIC_SKILL"));

		_barPsiSkill.Scale = 1.0;
	}

	/// <summary>
	/// Formats a stat for display.
	/// </summary>
	private static void FormatStat(short current2, short max2, short withArmor2, short initial2, Text number, Bar bar)
	{
		string ss = "";
		if (current2 >= max2)
		{
			ss += Unicode.TOK_COLOR_FLIP;
		}
		ss += withArmor2.ToString(CultureInfo.InvariantCulture);
		number.SetText(ss);
		bar.Max = current2;
		bar.Value = withArmor2;
		bar.Value2 = Math.Min(withArmor2, initial2);
	}

	/// <summary>
	/// Updates soldier stats when
	/// the soldier changes.
	/// </summary>
	public override void Init()
	{
		base.Init();
		if (_list.Count == 0)
		{
			_game.PopState();
			return;
		}
		if ((uint)_soldierId >= (uint)_list.Count)
		{
			_soldierId = 0;
		}
		_soldier = _list[_soldierId];
		_edtSoldier.SetBig();
		_edtSoldier.Text = _soldier.GetName();
		UnitStats initial = _soldier.InitStats;
		UnitStats current = _soldier.CurrentStats;
		UnitStats max = _soldier.Rules.StatCaps;

		bool hasBonus = _soldier.PrepareStatsWithBonuses(_game.Mod); // refresh all bonuses
		UnitStats withArmor = _soldier.StatsWithAllBonuses;
		_btnBonuses.Visible = hasBonus;

		SurfaceSet texture = _game.Mod.GetSurfaceSet("BASEBITS.PCK");
		var frame = texture.GetFrame(_soldier.RankSprite);
		if (frame != null)
		{
			frame.BlitNShade(_rank, 0, 0);
		}

		string flagId = "Flag";
		var mapping = _game.Mod.FlagByKills;
		if (mapping.Count == 0)
		{
			flagId += (_soldier.Nationality + _soldier.Rules.FlagOffset).ToString(CultureInfo.InvariantCulture);
		}
		else
		{
			int index = 0;
			foreach (int item in mapping)
			{
				if (_soldier.Kills <= item)
				{
					break;
				}
				index++;
			}
			flagId += (index + _soldier.Rules.FlagOffset).ToString(CultureInfo.InvariantCulture);
		}
		Surface flagTexture = _game.Mod.GetSurface(flagId, false);
		_flag.Clear();
		if (flagTexture != null)
		{
			flagTexture.BlitNShade(_flag, _flag.Width - flagTexture.Width, 0); // align right
		}

		FormatStat(current.tu, max.tu, withArmor.tu, initial.tu, _numTimeUnits, _barTimeUnits);
		FormatStat(current.stamina, max.stamina, withArmor.stamina, initial.stamina, _numStamina, _barStamina);
		FormatStat(current.health, max.health, withArmor.health, initial.health, _numHealth, _barHealth);
		FormatStat(current.bravery, max.bravery, withArmor.bravery, initial.bravery, _numBravery, _barBravery);
		FormatStat(current.reactions, max.reactions, withArmor.reactions, initial.reactions, _numReactions, _barReactions);
		FormatStat(current.firing, max.firing, withArmor.firing, initial.firing, _numFiring, _barFiring);
		FormatStat(current.throwing, max.throwing, withArmor.throwing, initial.throwing, _numThrowing, _barThrowing);
		FormatStat(current.melee, max.melee, withArmor.melee, initial.melee, _numMelee, _barMelee);
		FormatStat(current.strength, max.strength, withArmor.strength, initial.strength, _numStrength, _barStrength);

		string wsArmor;
		if (_soldier.Armor == _soldier.Rules.DefaultArmor)
		{
			wsArmor = Tr("STR_ARMOR_").Arg(Tr(_soldier.Armor.Type));
		}
		else
		{
			wsArmor = Tr(_soldier.Armor.Type);
		}

		_btnArmor.Text = wsArmor;

		bool showNastyButtons = !_readOnly && _game.SavedGame.MonthsPassed > -1 && !(_soldier.Craft != null && _soldier.Craft.Status == "STR_OUT");

		_btnSack.Visible = showNastyButtons;
		_btnTransformations.Visible = showNastyButtons && !_noTransformations;

		_txtRank.SetText(Tr("STR_RANK_").Arg(Tr(_soldier.RankString)));

		_txtMissions.SetText(Tr("STR_MISSIONS").Arg(_soldier.Missions));

		_txtKills.SetText(Tr("STR_KILLS").Arg(_soldier.Kills));

		_txtStuns.SetText(Tr("STR_STUNS").Arg(_soldier.Stuns));
		_txtStuns.Visible = !Options.soldierDiaries;

		string craft;
		if (_soldier.Craft == null)
		{
			craft = Tr("STR_NONE_UC");
		}
		else
		{
			craft = _soldier.Craft.GetName(_game.Language);
		}
		_txtCraft.SetText(Tr("STR_CRAFT_").Arg(craft));

		BaseSumDailyRecovery recovery = _base != null ? _base.SumRecoveryPerDay : new BaseSumDailyRecovery();
		string GetDaysOrInfinity(int days)
		{
			if (days < 0)
			{
				return "∞";
			}
			else
			{
				return Tr("STR_DAY", days);
			}
		}
		if (_soldier.IsWounded)
		{
			int recoveryTime = _soldier.GetNeededRecoveryTime(recovery);
			_txtRecovery.SetText(Tr("STR_WOUND_RECOVERY").Arg(GetDaysOrInfinity(recoveryTime)));
		}
		else
		{
			_txtRecovery.SetText("");
			if (_soldier.ManaMissing > 0)
			{
				int manaRecoveryTime = _soldier.GetManaRecovery(recovery.ManaRecovery);
				_txtRecovery.SetText(Tr("STR_MANA_RECOVERY").Arg(GetDaysOrInfinity(manaRecoveryTime)));
			}
			if (_soldier.HealthMissing > 0)
			{
				int healthRecoveryTime = _soldier.GetHealthRecovery(recovery.HealthRecovery);
				_txtRecovery.SetText(Tr("STR_HEALTH_RECOVERY").Arg(GetDaysOrInfinity(healthRecoveryTime)));
			}
		}

		_txtPsionic.Visible = _soldier.IsInPsiTraining;

		if (_game.Mod.IsManaFeatureEnabled)
		{
			if (_game.SavedGame.IsManaUnlocked(_game.Mod))
			{
				FormatStat(current.mana, max.mana, withArmor.mana, initial.mana, _numMana, _barMana);

				_txtMana.Visible = true;
				_numMana.Visible = true;
				_barMana.Visible = true;
			}
			else
			{
				_txtMana.Visible = false;
				_numMana.Visible = false;
				_barMana.Visible = false;
			}
		}

		if (current.psiSkill > 0 || (Options.psiStrengthEval && _game.SavedGame.IsResearched(_game.Mod.PsiRequirements)))
		{
			FormatStat(current.psiStrength, max.psiStrength, withArmor.psiStrength, initial.psiStrength, _numPsiStrength, _barPsiStrength);

			_txtPsiStrength.Visible = true;
			_numPsiStrength.Visible = true;
			_barPsiStrength.Visible = true;
		}
		else
		{
			_txtPsiStrength.Visible = false;
			_numPsiStrength.Visible = false;
			_barPsiStrength.Visible = false;
		}

		if (current.psiSkill > 0)
		{
			FormatStat(current.psiSkill, max.psiSkill, withArmor.psiSkill, initial.psiSkill, _numPsiSkill, _barPsiSkill);

			_txtPsiSkill.Visible = true;
			_numPsiSkill.Visible = true;
			_barPsiSkill.Visible = true;
		}
		else
		{
			_txtPsiSkill.Visible = false;
			_numPsiSkill.Visible = false;
			_barPsiSkill.Visible = false;
		}

		// Dead can't talk
		if (_base == null)
		{
			_btnArmor.Visible = false;
			_btnSack.Visible = false;
			_txtCraft.Visible = false;
			_txtDead.Visible = true;
			string status = "STR_MISSING_IN_ACTION";
			if (_soldier.Death != null && _soldier.Death.Cause != null)
			{
				status = "STR_KILLED_IN_ACTION";
			}
			_txtDead.SetText(Tr(status, _soldier.Gender));
		}
		else
		{
			_txtDead.Visible = false;
		}
	}

	/// <summary>
	/// Takes care of any events from the core game engine.
	/// </summary>
	public override void Handle(Action action)
	{
		base.Handle(action);

		if (Options.oxceThumbButtons && action.Details.type == SDLEventType.SDL_MOUSEBUTTONDOWN)
		{
			if (action.Details.button.button == SDLMouse.SDL_BUTTON_X1)
			{
				BtnNextClick(action);
			}
			else if (action.Details.button.button == SDLMouse.SDL_BUTTON_X2)
			{
				BtnPrevClick(action);
			}
		}
	}

	/// <summary>
	/// Disables the soldier input.
	/// </summary>
	public void EdtSoldierPress(Action action)
	{
		if (_base == null)
		{
			_edtSoldier.SetFocus(false);
		}
	}

	/// <summary>
	/// Set the soldier Id.
	/// </summary>
	public void SetSoldierId(int soldier)
	{
		_soldierId = soldier;
	}

	/// <summary>
	/// Changes the soldier's name.
	/// </summary>
	public void EdtSoldierChange(Action action)
	{
		_soldier.SetName(_edtSoldier.Text);
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
		if (_game.SavedGame.MonthsPassed > -1 && Options.storageLimitsEnforced && _base != null && _base.StoresOverfull())
		{
			if (_forceLimits)
			{
				// Note: we could sell a currently opened craft here and crash the game
				_game.PushState(new SellState(_base, null));
			}
			RuleInterface soldierInfo = _game.Mod.GetInterface("soldierInfo");
			_game.PushState(new ErrorMessageState(Tr("STR_STORAGE_EXCEEDED").Arg(_base.GetName(_game.Language)), _palette, (byte)soldierInfo.GetElement("errorMessage").color, "BACK01.SCR", soldierInfo.GetElement("errorPalette").color));
		}
	}

	/// <summary>
	/// Goes to the previous soldier.
	/// </summary>
	public void BtnPrevClick(Action action)
	{
		if (_soldierId == 0)
			_soldierId = _list.Count - 1;
		else
			_soldierId--;
		Init();
	}

	/// <summary>
	/// Goes to the next soldier.
	/// </summary>
	public void BtnNextClick(Action action)
	{
		_soldierId++;
		if ((uint)_soldierId >= (uint)_list.Count)
			_soldierId = 0;
		Init();
	}

	/// <summary>
	/// Shows the Select Armor window.
	/// </summary>
	public void BtnArmorClick(Action action)
	{
		if (_soldier.Craft == null || (_soldier.Craft != null && _soldier.Craft.Status != "STR_OUT"))
		{
			_game.PushState(new SoldierArmorState(_base, _soldierId, SoldierArmorOrigin.SA_GEOSCAPE));
		}
	}

	/// <summary>
	/// Shows the SoldierBonus window.
	/// </summary>
	public void BtnBonusesClick(Action action)
	{
		_game.PushState(new SoldierBonusState(_base, _soldierId));
	}

	/// <summary>
	/// Shows the SoldierTransform window.
	/// </summary>
	public void BtnTransformationsClick(Action action)
	{
		_game.PushState(new SoldierTransformState(_base, _soldierId));
	}

	/// <summary>
	/// Shows the Sack Soldier window.
	/// </summary>
	public void BtnSackClick(Action action)
	{
		_game.PushState(new SackSoldierState(_base, _soldierId));
	}

	/// <summary>
	/// Shows the Diary Soldier window.
	/// </summary>
	public void BtnDiaryClick(Action action)
	{
		_game.PushState(new SoldierDiaryOverviewState(_base, _soldierId, this));
	}

	/// <summary>
	/// Changes soldier's nationality.
	/// </summary>
	public void BtnFlagClick(Action action)
	{
		int temp = _soldier.Nationality;
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
		{
			temp += 1;
		}
		else if (action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT)
		{
			temp += -1;
		}

		var names = _soldier.Rules.Names;
		if (names.Count != 0)
		{
			int max = names.Count;
			if (temp > max - 1)
			{
				temp = 0;
			}
			else if (temp < 0)
			{
				temp = max - 1;
			}
		}
		else
		{
			temp = 0;
		}

		_soldier.Nationality = temp;
		Init();
	}

	/// <summary>
	/// Shows the Manual Promotion window.
	/// </summary>
	public void BtnRankClick(Action action)
	{
		if (Options.oxceManualPromotions)
		{
			_game.PushState(new SoldierRankState(_base, _soldierId));
		}
	}
}
