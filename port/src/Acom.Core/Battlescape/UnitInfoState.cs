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
// Ported from: src/Battlescape/UnitInfoState.h, src/Battlescape/UnitInfoState.cpp
// Port status: partial (missing: Screen::resetDisplay)
#nullable disable

using System.Globalization;
using static Acom.UnitSide;

namespace Acom;

/// <summary>
/// Unit Info screen that shows all the
/// info of a specific unit.
/// </summary>
public class UnitInfoState : State
{
	private readonly SavedBattleGame _battleGame;
	private BattleUnit _unit;
	private readonly BattlescapeState _parent;
	private readonly bool _fromInventory, _mindProbe;
	private readonly Surface _bg;
	private readonly InteractiveSurface _exit;
	private readonly Text _txtName;
	private readonly Text _txtTimeUnits, _txtEnergy, _txtHealth, _txtFatalWounds, _txtBravery, _txtMorale, _txtReactions, _txtFiring, _txtThrowing, _txtMelee, _txtStrength;
	private readonly Text _txtPsiStrength, _txtPsiSkill, _txtMana;
	private readonly Text _numTimeUnits, _numEnergy, _numHealth, _numFatalWounds, _numBravery, _numMorale, _numReactions, _numFiring, _numThrowing, _numMelee, _numStrength;
	private readonly Text _numPsiStrength, _numPsiSkill, _numMana;
	private readonly Text _numMaxHealth;
	private readonly Bar _barTimeUnits, _barEnergy, _barHealth, _barFatalWounds, _barBravery, _barMorale, _barReactions, _barFiring, _barThrowing, _barMelee, _barStrength;
	private readonly Bar _barPsiStrength, _barPsiSkill, _barMana;
	private readonly Text _txtFrontArmor, _txtLeftArmor, _txtRightArmor, _txtRearArmor, _txtUnderArmor;
	private readonly Text _numFrontArmor, _numLeftArmor, _numRightArmor, _numRearArmor, _numUnderArmor;
	private readonly Bar _barFrontArmor, _barLeftArmor, _barRightArmor, _barRearArmor, _barUnderArmor;
	private readonly TextButton _btnPrev, _btnNext;

	/// <summary>Initializes all the elements in the Unit Info screen.</summary>
	/// <param name="unit">Pointer to the selected unit.</param>
	/// <param name="parent">Pointer to parent Battlescape.</param>
	/// <param name="fromInventory">Is player coming from the inventory?</param>
	/// <param name="mindProbe">Is player using a Mind Probe?</param>
	public UnitInfoState(BattleUnit unit, BattlescapeState parent, bool fromInventory, bool mindProbe)
	{
		_unit = unit;
		_parent = parent;
		_fromInventory = fromInventory;
		_mindProbe = mindProbe;
		if (Options.maximizeInfoScreens)
		{
			Options.baseXResolution = Screen.ORIGINAL_WIDTH;
			Options.baseYResolution = Screen.ORIGINAL_HEIGHT;
			// missing: _game->getScreen()->resetDisplay(false);
		}
		_battleGame = _game.SavedGame.SavedBattle;

		// Create objects
		_bg = new Surface(320, 200, 0, 0);
		_exit = new InteractiveSurface(320, 180, 0, 20);
		_txtName = new Text(288, 17, 16, 4);

		int yPos = 38;
		int step = 9;
		if (_game.Mod.IsManaFeatureEnabled)
		{
			yPos = 30;
		}

		_txtTimeUnits = new Text(140, 9, 8, yPos);
		_numTimeUnits = new Text(18, 9, 150, yPos);
		_barTimeUnits = new Bar(150, 5, 170, yPos + 1);
		yPos += step;

		_txtEnergy = new Text(140, 9, 8, yPos);
		_numEnergy = new Text(18, 9, 150, yPos);
		_barEnergy = new Bar(150, 5, 170, yPos + 1);
		yPos += step;

		_txtHealth = new Text(140, 9, 8, yPos);
		_numHealth = new Text(18, 9, 150, yPos);
		_barHealth = new Bar(150, 5, 170, yPos + 1);
		{
			int numMaxHealthPosX = _game.Mod.GetInterface("stats").GetElement("numMaxHealth").x;
			_numMaxHealth = new Text(40, 9, numMaxHealthPosX, yPos);
		}
		yPos += step;

		_txtFatalWounds = new Text(140, 9, 8, yPos);
		_numFatalWounds = new Text(18, 9, 150, yPos);
		_barFatalWounds = new Bar(150, 5, 170, yPos + 1);
		yPos += step;

		_txtBravery = new Text(140, 9, 8, yPos);
		_numBravery = new Text(18, 9, 150, yPos);
		_barBravery = new Bar(150, 5, 170, yPos + 1);
		yPos += step;

		_txtMorale = new Text(140, 9, 8, yPos);
		_numMorale = new Text(18, 9, 150, yPos);
		_barMorale = new Bar(150, 5, 170, yPos + 1);
		yPos += step;

		_txtReactions = new Text(140, 9, 8, yPos);
		_numReactions = new Text(18, 9, 150, yPos);
		_barReactions = new Bar(150, 5, 170, yPos + 1);
		yPos += step;

		_txtFiring = new Text(140, 9, 8, yPos);
		_numFiring = new Text(18, 9, 150, yPos);
		_barFiring = new Bar(150, 5, 170, yPos + 1);
		yPos += step;

		_txtThrowing = new Text(140, 9, 8, yPos);
		_numThrowing = new Text(18, 9, 150, yPos);
		_barThrowing = new Bar(150, 5, 170, yPos + 1);
		yPos += step;

		_txtMelee = new Text(140, 9, 8, yPos);
		_numMelee = new Text(18, 9, 150, yPos);
		_barMelee = new Bar(150, 5, 170, yPos + 1);
		yPos += step;

		_txtStrength = new Text(140, 9, 8, yPos);
		_numStrength = new Text(18, 9, 150, yPos);
		_barStrength = new Bar(150, 5, 170, yPos + 1);
		yPos += step;

		if (_game.Mod.IsManaFeatureEnabled)
		{
			_txtMana = new Text(140, 9, 8, yPos);
			_numMana = new Text(18, 9, 150, yPos);
			_barMana = new Bar(150, 5, 170, yPos + 1);
			yPos += step;
		}

		_txtPsiStrength = new Text(140, 9, 8, yPos);
		_numPsiStrength = new Text(18, 9, 150, yPos);
		_barPsiStrength = new Bar(150, 5, 170, yPos + 1);
		yPos += step;

		_txtPsiSkill = new Text(140, 9, 8, yPos);
		_numPsiSkill = new Text(18, 9, 150, yPos);
		_barPsiSkill = new Bar(150, 5, 170, yPos + 1);
		yPos += step;

		_txtFrontArmor = new Text(140, 9, 8, yPos);
		_numFrontArmor = new Text(18, 9, 150, yPos);
		_barFrontArmor = new Bar(150, 5, 170, yPos + 1);
		yPos += step;

		_txtLeftArmor = new Text(140, 9, 8, yPos);
		_numLeftArmor = new Text(18, 9, 150, yPos);
		_barLeftArmor = new Bar(150, 5, 170, yPos + 1);
		yPos += step;

		_txtRightArmor = new Text(140, 9, 8, yPos);
		_numRightArmor = new Text(18, 9, 150, yPos);
		_barRightArmor = new Bar(150, 5, 170, yPos + 1);
		yPos += step;

		_txtRearArmor = new Text(140, 9, 8, yPos);
		_numRearArmor = new Text(18, 9, 150, yPos);
		_barRearArmor = new Bar(150, 5, 170, yPos + 1);
		yPos += step;

		_txtUnderArmor = new Text(140, 9, 8, yPos);
		_numUnderArmor = new Text(18, 9, 150, yPos);
		_barUnderArmor = new Bar(150, 5, 170, yPos + 1);

		if (!_mindProbe)
		{
			_btnPrev = new TextButton(14, 18, 2, 2);
			_btnNext = new TextButton(14, 18, 304, 2);
		}

		// Set palette
		SetStandardPalette("PAL_BATTLESCAPE");

		Add(_bg);
		Add(_exit);
		Add(_txtName, "textName", "stats", null);

		Add(_txtTimeUnits);
		Add(_numTimeUnits);
		Add(_barTimeUnits, "barTUs", "stats", null);

		Add(_txtEnergy);
		Add(_numEnergy);
		Add(_barEnergy, "barEnergy", "stats", null);

		Add(_txtHealth);
		Add(_numHealth);
		Add(_barHealth, "barHealth", "stats", null);
		Add(_numMaxHealth, "numMaxHealth", "stats", null);

		Add(_txtFatalWounds);
		Add(_numFatalWounds);
		Add(_barFatalWounds, "barWounds", "stats", null);

		Add(_txtBravery);
		Add(_numBravery);
		Add(_barBravery, "barBravery", "stats", null);

		Add(_txtMorale);
		Add(_numMorale);
		Add(_barMorale, "barMorale", "stats", null);

		Add(_txtReactions);
		Add(_numReactions);
		Add(_barReactions, "barReactions", "stats", null);

		Add(_txtFiring);
		Add(_numFiring);
		Add(_barFiring, "barFiring", "stats", null);

		Add(_txtThrowing);
		Add(_numThrowing);
		Add(_barThrowing, "barThrowing", "stats", null);

		Add(_txtMelee);
		Add(_numMelee);
		Add(_barMelee, "barMelee", "stats", null);

		Add(_txtStrength);
		Add(_numStrength);
		Add(_barStrength, "barStrength", "stats", null);

		if (_game.Mod.IsManaFeatureEnabled)
		{
			Add(_txtMana);
			Add(_numMana);
			Add(_barMana, "barMana", "stats", null);
		}

		Add(_txtPsiStrength);
		Add(_numPsiStrength);
		Add(_barPsiStrength, "barPsiStrength", "stats", null);

		Add(_txtPsiSkill);
		Add(_numPsiSkill);
		Add(_barPsiSkill, "barPsiSkill", "stats", null);

		Add(_txtFrontArmor);
		Add(_numFrontArmor);
		Add(_barFrontArmor, "barFrontArmor", "stats", null);

		Add(_txtLeftArmor);
		Add(_numLeftArmor);
		Add(_barLeftArmor, "barLeftArmor", "stats", null);

		Add(_txtRightArmor);
		Add(_numRightArmor);
		Add(_barRightArmor, "barRightArmor", "stats", null);

		Add(_txtRearArmor);
		Add(_numRearArmor);
		Add(_barRearArmor, "barRearArmor", "stats", null);

		Add(_txtUnderArmor);
		Add(_numUnderArmor);
		Add(_barUnderArmor, "barUnderArmor", "stats", null);

		if (!_mindProbe)
		{
			Add(_btnPrev, "button", "stats");
			Add(_btnNext, "button", "stats");
		}

		CenterAllSurfaces();

		// Set up objects
		_game.Mod.GetSurface("UNIBORD.PCK").BlitNShade(_bg, 0, 0);

		_exit.OnMouseClick(ExitClick);
		_exit.OnKeyboardPress(ExitClick, Options.keyCancel);
		_exit.OnKeyboardPress(ExitClick, Options.keyBattleStats);

		byte color = (byte)_game.Mod.GetInterface("stats").GetElement("text").color;
		byte color2 = (byte)_game.Mod.GetInterface("stats").GetElement("text").color2;

		_txtName.Align = TextHAlign.ALIGN_CENTER;
		_txtName.SetBig();
		_txtName.SetHighContrast(true);

		_txtTimeUnits.SetColor(color);
		_txtTimeUnits.SetHighContrast(true);
		_txtTimeUnits.SetText(Tr("STR_TIME_UNITS"));

		_numTimeUnits.SetColor(color2);
		_numTimeUnits.SetHighContrast(true);

		_barTimeUnits.Scale = 1.0;

		_txtEnergy.SetColor(color);
		_txtEnergy.SetHighContrast(true);
		_txtEnergy.SetText(Tr("STR_ENERGY"));

		_numEnergy.SetColor(color2);
		_numEnergy.SetHighContrast(true);

		_barEnergy.Scale = 1.0;

		_txtHealth.SetColor(color);
		_txtHealth.SetHighContrast(true);
		_txtHealth.SetText(Tr("STR_HEALTH"));

		_numHealth.SetColor(color2);
		_numHealth.SetHighContrast(true);

		_numMaxHealth.SetHighContrast(true);
		_numMaxHealth.Align = TextHAlign.ALIGN_RIGHT;

		_barHealth.Scale = 1.0;

		_txtFatalWounds.SetColor(color);
		_txtFatalWounds.SetHighContrast(true);
		_txtFatalWounds.SetText(Tr("STR_FATAL_WOUNDS"));

		_numFatalWounds.SetColor(color2);
		_numFatalWounds.SetHighContrast(true);

		_barFatalWounds.Scale = 1.0;

		_txtBravery.SetColor(color);
		_txtBravery.SetHighContrast(true);
		_txtBravery.SetText(Tr("STR_BRAVERY"));

		_numBravery.SetColor(color2);
		_numBravery.SetHighContrast(true);

		_barBravery.Scale = 1.0;

		_txtMorale.SetColor(color);
		_txtMorale.SetHighContrast(true);
		_txtMorale.SetText(Tr("STR_MORALE"));

		_numMorale.SetColor(color2);
		_numMorale.SetHighContrast(true);

		_barMorale.Scale = 1.0;

		_txtReactions.SetColor(color);
		_txtReactions.SetHighContrast(true);
		_txtReactions.SetText(Tr("STR_REACTIONS"));

		_numReactions.SetColor(color2);
		_numReactions.SetHighContrast(true);

		_barReactions.Scale = 1.0;

		_txtFiring.SetColor(color);
		_txtFiring.SetHighContrast(true);
		_txtFiring.SetText(Tr("STR_FIRING_ACCURACY"));

		_numFiring.SetColor(color2);
		_numFiring.SetHighContrast(true);

		_barFiring.Scale = 1.0;

		_txtThrowing.SetColor(color);
		_txtThrowing.SetHighContrast(true);
		_txtThrowing.SetText(Tr("STR_THROWING_ACCURACY"));

		_numThrowing.SetColor(color2);
		_numThrowing.SetHighContrast(true);

		_barThrowing.Scale = 1.0;

		_txtMelee.SetColor(color);
		_txtMelee.SetHighContrast(true);
		_txtMelee.SetText(Tr("STR_MELEE_ACCURACY"));

		_numMelee.SetColor(color2);
		_numMelee.SetHighContrast(true);

		_barMelee.Scale = 1.0;

		_txtStrength.SetColor(color);
		_txtStrength.SetHighContrast(true);
		_txtStrength.SetText(Tr("STR_STRENGTH"));

		_numStrength.SetColor(color2);
		_numStrength.SetHighContrast(true);

		_barStrength.Scale = 1.0;

		if (_game.Mod.IsManaFeatureEnabled)
		{
			_txtMana.SetColor(color);
			_txtMana.SetHighContrast(true);
			_txtMana.SetText(Tr("STR_MANA"));

			_numMana.SetColor(color2);
			_numMana.SetHighContrast(true);

			_barMana.Scale = 1.0;
		}

		_txtPsiStrength.SetColor(color);
		_txtPsiStrength.SetHighContrast(true);
		_txtPsiStrength.SetText(Tr("STR_PSIONIC_STRENGTH"));

		_numPsiStrength.SetColor(color2);
		_numPsiStrength.SetHighContrast(true);

		_barPsiStrength.Scale = 1.0;

		_txtPsiSkill.SetColor(color);
		_txtPsiSkill.SetHighContrast(true);
		_txtPsiSkill.SetText(Tr("STR_PSIONIC_SKILL"));

		_numPsiSkill.SetColor(color2);
		_numPsiSkill.SetHighContrast(true);

		_barPsiSkill.Scale = 1.0;

		_txtFrontArmor.SetColor(color);
		_txtFrontArmor.SetHighContrast(true);
		_txtFrontArmor.SetText(Tr("STR_FRONT_ARMOR_UC"));

		_numFrontArmor.SetColor(color2);
		_numFrontArmor.SetHighContrast(true);

		_barFrontArmor.Scale = 1.0;

		_txtLeftArmor.SetColor(color);
		_txtLeftArmor.SetHighContrast(true);
		_txtLeftArmor.SetText(Tr("STR_LEFT_ARMOR_UC"));

		_numLeftArmor.SetColor(color2);
		_numLeftArmor.SetHighContrast(true);

		_barLeftArmor.Scale = 1.0;

		_txtRightArmor.SetColor(color);
		_txtRightArmor.SetHighContrast(true);
		_txtRightArmor.SetText(Tr("STR_RIGHT_ARMOR_UC"));

		_numRightArmor.SetColor(color2);
		_numRightArmor.SetHighContrast(true);

		_barRightArmor.Scale = 1.0;

		_txtRearArmor.SetColor(color);
		_txtRearArmor.SetHighContrast(true);
		_txtRearArmor.SetText(Tr("STR_REAR_ARMOR_UC"));

		_numRearArmor.SetColor(color2);
		_numRearArmor.SetHighContrast(true);

		_barRearArmor.Scale = 1.0;

		_txtUnderArmor.SetColor(color);
		_txtUnderArmor.SetHighContrast(true);
		_txtUnderArmor.SetText(Tr("STR_UNDER_ARMOR_UC"));

		_numUnderArmor.SetColor(color2);
		_numUnderArmor.SetHighContrast(true);

		_barUnderArmor.Scale = 1.0;

		if (!_mindProbe)
		{
			_btnPrev.Text = "<<";
			_btnPrev.OnMouseClick(BtnPrevClick);
			_btnPrev.OnKeyboardPress(BtnPrevClick, Options.keyBattlePrevUnit);
			_btnNext.Text = ">>";
			_btnNext.OnMouseClick(BtnNextClick);
			_btnNext.OnKeyboardPress(BtnNextClick, Options.keyBattleNextUnit);
		}

	}

	private static string Str(int value) => value.ToString(CultureInfo.InvariantCulture);

	/// <summary>Updates unit info which can change after going into other screens.</summary>
	public override void Init()
	{
		base.Init();
		_numTimeUnits.SetText(Str(_unit.TimeUnits));
		_barTimeUnits.Max = _unit.BaseStats.tu;
		_barTimeUnits.Value = _unit.TimeUnits;

		string ss = "";
		// aliens have their rank in their "name", soldiers don't
		if (_unit.Type == "SOLDIER")
		{
			ss += Tr(_unit.RankString);
			ss += " ";
		}
		ss += _unit.GetName(_game.Language, BattlescapeGame._debugPlay);
		_txtName.SetBig();
		_txtName.SetText(ss);

		_numEnergy.SetText(Str(_unit.Energy));
		_barEnergy.Max = _unit.BaseStats.stamina;
		_barEnergy.Value = _unit.Energy;

		_numHealth.SetText(Str(_unit.Health));
		_barHealth.Max = _unit.BaseStats.health;
		_barHealth.Value = _unit.Health;
		_barHealth.Value2 = _unit.Stunlevel;

		_numMaxHealth.SetText("");
		if (_unit.BaseStats.health >= 147)
		{
			Element numMaxHealthElement = _game.Mod.GetInterface("stats").GetElement("numMaxHealth");
			if ((numMaxHealthElement.custom & 1) != 0 || _unit.Health != _unit.BaseStats.health)
			{
				ss = "";
				if ((numMaxHealthElement.custom & 2) != 0)
				{
					ss += "/";
				}
				ss += Str(_unit.BaseStats.health);
				_numMaxHealth.SetText(ss);
			}
		}

		_numFatalWounds.SetText(Str(_unit.FatalWounds));
		_barFatalWounds.Max = _unit.FatalWounds;
		_barFatalWounds.Value = _unit.FatalWounds;

		_numBravery.SetText(Str(_unit.BaseStats.bravery));
		_barBravery.Max = _unit.BaseStats.bravery;
		_barBravery.Value = _unit.BaseStats.bravery;

		_numMorale.SetText(Str(_unit.Morale));
		_barMorale.Max = 100;
		_barMorale.Value = _unit.Morale;

		_numReactions.SetText(Str(_unit.BaseStats.reactions));
		_barReactions.Max = _unit.BaseStats.reactions;
		_barReactions.Value = _unit.BaseStats.reactions;

		// more info: http://ufopaedia.org/index.php?title=Accuracy_formula#Stat_Screen_Accuracy
		int healthModifier = 75 + ((25 * _unit.Health) / _unit.BaseStats.health);

		_numFiring.SetText(Str((_unit.BaseStats.firing * healthModifier) / 100));
		_barFiring.Max = _unit.BaseStats.firing;
		_barFiring.Value = (_unit.BaseStats.firing * healthModifier) / 100;

		_numThrowing.SetText(Str((_unit.BaseStats.throwing * healthModifier) / 100));
		_barThrowing.Max = _unit.BaseStats.throwing;
		_barThrowing.Value = (_unit.BaseStats.throwing * healthModifier) / 100;

		_numMelee.SetText(Str((_unit.BaseStats.melee * healthModifier) / 100));
		_barMelee.Max = _unit.BaseStats.melee;
		_barMelee.Value = (_unit.BaseStats.melee * healthModifier) / 100;
		// end of healthModifier usage

		_numStrength.SetText(Str(_unit.BaseStats.strength));
		_barStrength.Max = _unit.BaseStats.strength;
		_barStrength.Value = _unit.BaseStats.strength;

		if (_game.Mod.IsManaFeatureEnabled)
		{
			if (_game.SavedGame.IsManaUnlocked(_game.Mod))
			{
				_numMana.SetText(Str(_unit.Mana));
				_barMana.Max = _unit.BaseStats.mana;
				_barMana.Value = _unit.Mana;

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

		int psiSkillWithoutAnyBonuses = _unit.BaseStats.psiSkill;
		if (_unit.GeoscapeSoldier != null)
		{
			psiSkillWithoutAnyBonuses = _unit.GeoscapeSoldier.CurrentStats.psiSkill;
		}
		if (psiSkillWithoutAnyBonuses > 0 || (Options.psiStrengthEval && _game.SavedGame.IsResearched(_game.Mod.PsiRequirements)))
		{
			_numPsiStrength.SetText(Str(_unit.BaseStats.psiStrength));
			_barPsiStrength.Max = _unit.BaseStats.psiStrength;
			_barPsiStrength.Value = _unit.BaseStats.psiStrength;

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

		if (psiSkillWithoutAnyBonuses > 0)
		{
			_numPsiSkill.SetText(Str(_unit.BaseStats.psiSkill));
			_barPsiSkill.Max = _unit.BaseStats.psiSkill;
			_barPsiSkill.Value = _unit.BaseStats.psiSkill;

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

		_numFrontArmor.SetText(Str(_unit.GetArmor(SIDE_FRONT)));
		_barFrontArmor.Max = _unit.GetMaxArmor(SIDE_FRONT);
		_barFrontArmor.Value = _unit.GetArmor(SIDE_FRONT);

		_numLeftArmor.SetText(Str(_unit.GetArmor(SIDE_LEFT)));
		_barLeftArmor.Max = _unit.GetMaxArmor(SIDE_LEFT);
		_barLeftArmor.Value = _unit.GetArmor(SIDE_LEFT);

		_numRightArmor.SetText(Str(_unit.GetArmor(SIDE_RIGHT)));
		_barRightArmor.Max = _unit.GetMaxArmor(SIDE_RIGHT);
		_barRightArmor.Value = _unit.GetArmor(SIDE_RIGHT);

		_numRearArmor.SetText(Str(_unit.GetArmor(SIDE_REAR)));
		_barRearArmor.Max = _unit.GetMaxArmor(SIDE_REAR);
		_barRearArmor.Value = _unit.GetArmor(SIDE_REAR);

		_numUnderArmor.SetText(Str(_unit.GetArmor(SIDE_UNDER)));
		_barUnderArmor.Max = _unit.GetMaxArmor(SIDE_UNDER);
		_barUnderArmor.Value = _unit.GetArmor(SIDE_UNDER);
	}

	/// <summary>Closes the window on right-click.</summary>
	public override void Handle(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		base.Handle(action);
		if (action.Details.type == SDLEventType.SDL_MOUSEBUTTONDOWN)
		{
			if (_game.IsRightClick(action))
			{
				ExitClick(action);
				return;
			}
			if (Options.oxceThumbButtons)
			{
				if (action.Details.button.button == SDLMouse.SDL_BUTTON_X1)
				{
					if (!_mindProbe) BtnNextClick(action);
				}
				else if (action.Details.button.button == SDLMouse.SDL_BUTTON_X2)
				{
					if (!_mindProbe) BtnPrevClick(action);
				}
			}
		}
	}

	/// <summary>Selects the previous unit.</summary>
	public void BtnPrevClick(Action action)
	{
		if (_parent != null)
		{ // so we are here from a Battlescape Game
			_parent.SelectPreviousPlayerUnit(false, false, _fromInventory);
		}
		else
		{ // so we are here from the Craft Equipment screen
			_battleGame.SelectPreviousPlayerUnit(false, false, true);
		}
		_unit = _battleGame.SelectedUnit;
		if (_unit != null)
		{
			Init();
		}
		else
		{
			ExitClick(action);
		}
	}

	/// <summary>Selects the next unit.</summary>
	public void BtnNextClick(Action action)
	{
		if (_parent != null)
		{ // so we are here from a Battlescape Game
			_parent.SelectNextPlayerUnit(false, false, _fromInventory);
		}
		else
		{ // so we are here from the Craft Equipment screen
			_battleGame.SelectNextPlayerUnit(false, false, true);
		}
		_unit = _battleGame.SelectedUnit;
		if (_unit != null)
		{
			Init();
		}
		else
		{
			ExitClick(action);
		}
	}

	/// <summary>Exits the screen.</summary>
	public void ExitClick(Action action)
	{
		if (!_fromInventory && Options.maximizeInfoScreens)
		{
			Screen.UpdateScale(Options.battlescapeScale, ref Options.baseXBattlescape, ref Options.baseYBattlescape, true);
			// missing: _game->getScreen()->resetDisplay(false);
		}
		_game.PopState();
	}
}
