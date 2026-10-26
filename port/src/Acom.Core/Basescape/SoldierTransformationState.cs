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
// Ported from: src/Basescape/SoldierTransformationState.h, src/Basescape/SoldierTransformationState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)
#pragma warning disable IDE0060 // handler signatures keep the unused Action parameter like C++

/// <summary>
/// Screen that allocates a soldier to a transformation project
/// </summary>
public class SoldierTransformationState : State
{
	private readonly RuleSoldierTransformation _transformationRule;
	private readonly Base _base;
	private Soldier _sourceSoldier;
	private readonly List<Soldier> _filteredListOfSoldiers;
	private readonly Window _window;
	private readonly TextEdit _edtSoldier;
	private readonly Text _txtCost, _txtTransferTime, _txtRecoveryTime, _txtRequiredItems, _txtItemNameColumn, _txtUnitRequiredColumn, _txtUnitAvailableColumn;
	private readonly TextList _lstRequiredItems, _lstStatChanges;
	private readonly TextButton _btnCancel, _btnLeftArrow, _btnRightArrow, _btnStart;

	/// <summary>
	/// Initializes all the elements in the Soldier Transformation GUI
	/// </summary>
	/// <param name="transformationRule">Pointer to the transformation ruleset</param>
	/// <param name="base">Pointer to the current base</param>
	/// <param name="selectedSoldier">Pointer to the selected soldier</param>
	/// <param name="filteredListOfSoldiers">Pointer to the list of available soldiers</param>
	public SoldierTransformationState(RuleSoldierTransformation transformationRule, Base @base, Soldier selectedSoldier, List<Soldier> filteredListOfSoldiers)
	{
		_transformationRule = transformationRule;
		_base = @base;
		_sourceSoldier = selectedSoldier;
		_filteredListOfSoldiers = filteredListOfSoldiers;

		_window = new Window(this, 320, 200, 0, 0);
		_btnCancel = new TextButton(148, 16, 8, 176);
		_btnStart = new TextButton(148, 16, 164, 176);

		_btnLeftArrow = new TextButton(16, 16, 8, 8);
		_btnRightArrow = new TextButton(16, 16, 296, 8);
		_edtSoldier = new TextEdit(this, 210, 16, 54, 8);

		_txtCost = new Text(290, 9, 16, 30);
		_txtTransferTime = new Text(290, 9, 16, 40);
		_txtRecoveryTime = new Text(290, 9, 16, 50);

		_txtRequiredItems = new Text(290, 9, 16, 64);
		_txtItemNameColumn = new Text(60, 16, 30, 72);
		_txtUnitRequiredColumn = new Text(60, 16, 155, 72);
		_txtUnitAvailableColumn = new Text(60, 16, 230, 72);
		_lstRequiredItems = new TextList(270, 32, 30, 88);

		_lstStatChanges = new TextList(288, 40, 16, 130);

		// Set palette
		SetInterface("soldierTransformation");

		Add(_window, "window", "soldierTransformation");
		Add(_btnCancel, "button", "soldierTransformation");
		Add(_btnStart, "button", "soldierTransformation");

		Add(_btnLeftArrow, "button", "soldierTransformation");
		Add(_btnRightArrow, "button", "soldierTransformation");
		Add(_edtSoldier, "text", "soldierTransformation");

		Add(_txtCost, "text", "soldierTransformation");
		Add(_txtTransferTime, "text", "soldierTransformation");
		Add(_txtRecoveryTime, "text", "soldierTransformation");

		Add(_txtRequiredItems, "text", "soldierTransformation");
		Add(_txtItemNameColumn, "text", "soldierTransformation");
		Add(_txtUnitRequiredColumn, "text", "soldierTransformation");
		Add(_txtUnitAvailableColumn, "text", "soldierTransformation");
		Add(_lstRequiredItems, "list1", "soldierTransformation");

		Add(_lstStatChanges, "list2", "soldierTransformation");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "soldierTransformation");

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_btnStart.Text = Tr(_transformationRule.Name);
		_btnStart.OnMouseClick(BtnStartClick);
		_btnStart.OnKeyboardPress(BtnStartClick, Options.keyOk);

		if (_filteredListOfSoldiers != null && _filteredListOfSoldiers.Count > 1)
		{
			_btnLeftArrow.Text = "<<";
			_btnLeftArrow.OnMouseClick(BtnLeftArrowClick);
			_btnLeftArrow.OnKeyboardPress(BtnLeftArrowClick, Options.keyGeoLeft);

			_btnRightArrow.Text = ">>";
			_btnRightArrow.OnMouseClick(BtnRightArrowClick);
			_btnRightArrow.OnKeyboardPress(BtnRightArrowClick, Options.keyGeoRight);
		}
		else
		{
			_btnLeftArrow.Visible = false;
			_btnRightArrow.Visible = false;
		}

		_edtSoldier.SetBig();
		_edtSoldier.SetAlign(TextHAlign.ALIGN_CENTER);

		_txtRequiredItems.SetText(Tr("STR_SPECIAL_MATERIALS_REQUIRED"));
		_txtRequiredItems.Align = TextHAlign.ALIGN_CENTER;

		_txtItemNameColumn.SetText(Tr("STR_ITEM_REQUIRED"));
		_txtItemNameColumn.SetWordWrap(true);

		_txtUnitRequiredColumn.SetText(Tr("STR_UNITS_REQUIRED"));
		_txtUnitRequiredColumn.SetWordWrap(true);

		_txtUnitAvailableColumn.SetText(Tr("STR_UNITS_AVAILABLE"));
		_txtUnitAvailableColumn.SetWordWrap(true);

		_lstRequiredItems.SetColumns(3, 140, 75, 55);

		if (_game.Mod.IsManaFeatureEnabled)
		{
			_lstStatChanges.SetColumns(14, 72, 18, 18, 18, 18, 18, 18, 18, 18, 18, 18, 18, 18, 0);
		}
		else
		{
			_lstStatChanges.SetColumns(13, 90, 18, 18, 18, 18, 18, 18, 18, 18, 18, 18, 18, 0);
		}
		_lstStatChanges.SetAlign(TextHAlign.ALIGN_RIGHT);
		_lstStatChanges.SetAlign(TextHAlign.ALIGN_LEFT, 0);

		InitTransformationData();
	}

	/// <summary>
	/// Creates a string for the soldier stats table
	/// </summary>
	internal static string FormatStat(int stat, bool plus, bool hide)
	{
		if (hide) return "\x01?\x01"; //Unicode.TOK_COLOR_FLIP
		if (stat == 0) return "";

		string ss = Unicode.TOK_COLOR_FLIP.ToString();
		if (plus && stat > 0) ss += '+';
		ss += stat.ToString(CultureInfo.InvariantCulture) + Unicode.TOK_COLOR_FLIP;
		return ss;
	}

	/// <summary>Stat columns of the table, in display order (mana only with the mana feature).</summary>
	private enum StatCol { Tu, Stamina, Health, Bravery, Reactions, Firing, Throwing, Melee, Strength, Mana, PsiStrength, PsiSkill }

	private static short GetStat(UnitStats s, StatCol c) => c switch
	{
		StatCol.Tu => s.tu,
		StatCol.Stamina => s.stamina,
		StatCol.Health => s.health,
		StatCol.Bravery => s.bravery,
		StatCol.Reactions => s.reactions,
		StatCol.Firing => s.firing,
		StatCol.Throwing => s.throwing,
		StatCol.Melee => s.melee,
		StatCol.Strength => s.strength,
		StatCol.Mana => s.mana,
		StatCol.PsiStrength => s.psiStrength,
		_ => s.psiSkill,
	};

	/// <summary>
	/// Sets all the values for the data fields on the screen
	/// </summary>
	public void InitTransformationData()
	{
		_edtSoldier.Text = _sourceSoldier.GetName();

		_lstRequiredItems.ClearList();
		_lstStatChanges.ClearList();

		bool transformationPossible = _game.SavedGame.Funds >= _transformationRule.Cost;

		if (_base.AvailableQuarters <= _base.UsedQuarters &&
			(_transformationRule.IsCreatingClone ||
			(_transformationRule.IsAllowingDeadSoldiers && _sourceSoldier.Death != null)))
		{
			transformationPossible = false;
		}

		_txtCost.SetText(Tr("STR_COST_").Arg(Unicode.FormatFunding(_transformationRule.Cost)));

		int transferTime = 0;
		if (!Mod.IsEmptyRuleName(_transformationRule.ProducedItem))
		{
			transferTime = _transformationRule.TransferTime > 0 ? _transformationRule.TransferTime : 1;
		}
		else if (_transformationRule.TransferTime > 0 || _transformationRule.IsCreatingClone || _sourceSoldier.Death != null)
		{
			transferTime = _transformationRule.TransferTime > 0 ? _transformationRule.TransferTime : 24;
		}
		_txtTransferTime.SetText(Tr("STR_TRANSFER_TIME").Arg(Tr("STR_HOUR", transferTime)));
		_txtRecoveryTime.SetText(Tr("STR_RECOVERY_TIME").Arg(Tr("STR_DAY", _transformationRule.RecoveryTime)));

		int row = 0;
		foreach (var requiredItem in _transformationRule.RequiredItems)
		{
			string s1 = requiredItem.Value.ToString(CultureInfo.InvariantCulture), s2 = "";
			RuleItem rule = _game.Mod.GetItem(requiredItem.Key);
			if (rule != null)
			{
				s2 = _base.StorageItems.GetItem(rule).ToString(CultureInfo.InvariantCulture);
				transformationPossible &= (_base.StorageItems.GetItem(rule) >= requiredItem.Value);
			}

			_lstRequiredItems.AddRow(3, Tr(requiredItem.Key), s1, s2);
			_lstRequiredItems.SetCellColor(row, 1, _lstRequiredItems.SecondaryColor);
			_lstRequiredItems.SetCellColor(row, 2, _lstRequiredItems.SecondaryColor);
			row++;
		}

		_btnStart.Visible = transformationPossible;

		if (!Mod.IsEmptyRuleName(_transformationRule.ProducedItem))
		{
			_txtRecoveryTime.Visible = false;

			// no need to show the rest, the soldier will be removed from existence after this project
			return;
		}

		UnitStats currentStats = _sourceSoldier.CurrentStats;
		UnitStats changedStatsMin = _sourceSoldier.CalculateStatChanges(_game.Mod, _transformationRule, _sourceSoldier, 1, _sourceSoldier.Rules);
		UnitStats changedStatsMax = _sourceSoldier.CalculateStatChanges(_game.Mod, _transformationRule, _sourceSoldier, 2, _sourceSoldier.Rules);
		var bonusStats = new UnitStats();
		RuleSoldierBonus bonusRule = _game.Mod.GetSoldierBonus(_transformationRule.SoldierBonusType, false);
		if (bonusRule != null)
		{
			bonusStats += bonusRule.Stats;
		}

		bool showPsiSkill = currentStats.psiSkill > 0;
		bool showPsiStrength = showPsiSkill || (Options.psiStrengthEval && _game.SavedGame.IsResearched(_game.Mod.PsiRequirements));

		UnitStats rerollFlags = _transformationRule.RerollStats;

		var randomFlags = new UnitStats();
		bool twoRows = _transformationRule.ShowMinMax;
		if (!twoRows)
		{
			randomFlags += UnitStats.IsRandom(_transformationRule.FlatMin, _transformationRule.FlatMax);
			randomFlags += UnitStats.IsRandom(_transformationRule.PercentMin, _transformationRule.PercentMax);
			randomFlags += UnitStats.IsRandom(_transformationRule.PercentGainedMin, _transformationRule.PercentGainedMax);
		}

		bool manaEnabled = _game.Mod.IsManaFeatureEnabled;
		bool showMana = manaEnabled && _game.SavedGame.IsManaUnlocked(_game.Mod);
		var cols = new List<StatCol> { StatCol.Tu, StatCol.Stamina, StatCol.Health, StatCol.Bravery, StatCol.Reactions, StatCol.Firing, StatCol.Throwing, StatCol.Melee, StatCol.Strength };
		if (manaEnabled)
		{
			cols.Add(StatCol.Mana);
		}
		cols.Add(StatCol.PsiStrength);
		cols.Add(StatCol.PsiSkill);
		int numCols = cols.Count + 2; // 14 with mana, 13 without

		// hidden stats (unknown mana / psi)
		bool Unknown(StatCol c) => c switch
		{
			StatCol.Mana => !showMana,
			StatCol.PsiStrength => !showPsiStrength,
			StatCol.PsiSkill => !showPsiSkill,
			_ => false,
		};
		void AddStatRow(string label, Func<StatCol, string> cell)
		{
			var texts = new string[numCols];
			texts[0] = label;
			for (int i = 0; i < cols.Count; ++i)
			{
				texts[i + 1] = cell(cols[i]);
			}
			texts[numCols - 1] = "";
			_lstStatChanges.AddRow(numCols, texts);
		}

		AddStatRow("", c => c switch
		{
			StatCol.Tu => Tr("STR_TIME_UNITS_ABBREVIATION"),
			StatCol.Stamina => Tr("STR_STAMINA_ABBREVIATION"),
			StatCol.Health => Tr("STR_HEALTH_ABBREVIATION"),
			StatCol.Bravery => Tr("STR_BRAVERY_ABBREVIATION"),
			StatCol.Reactions => Tr("STR_REACTIONS_ABBREVIATION"),
			StatCol.Firing => Tr("STR_FIRING_ACCURACY_ABBREVIATION"),
			StatCol.Throwing => Tr("STR_THROWING_ACCURACY_ABBREVIATION"),
			StatCol.Melee => Tr("STR_MELEE_ACCURACY_ABBREVIATION"),
			StatCol.Strength => Tr("STR_STRENGTH_ABBREVIATION"),
			StatCol.Mana => Tr("STR_MANA_ABBREVIATION"),
			StatCol.PsiStrength => Tr("STR_PSIONIC_STRENGTH_ABBREVIATION"),
			_ => Tr("STR_PSIONIC_SKILL_ABBREVIATION"),
		});
		AddStatRow(Tr("STR_CURRENT_STATS"), c => FormatStat(GetStat(currentStats, c), false, Unknown(c)));
		AddStatRow(Tr(twoRows ? "STR_CHANGES_MIN" : "STR_CHANGES"),
			c => FormatStat(GetStat(changedStatsMin, c), true, Unknown(c) || GetStat(rerollFlags, c) != 0 || GetStat(randomFlags, c) != 0));
		if (twoRows)
		{
			// C++ labels this row "STR_CHANGES" when the mana feature is off
			AddStatRow(Tr(manaEnabled ? "STR_CHANGES_MAX" : "STR_CHANGES"),
				c => FormatStat(GetStat(changedStatsMax, c), true, Unknown(c) || GetStat(rerollFlags, c) != 0));
		}
		if (bonusRule != null)
		{
			AddStatRow(Tr("STR_BONUS_STATS"), c => FormatStat(GetStat(bonusStats, c), true, false));
		}
	}

	/// <summary>
	/// Returns to previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Performs the selected transformation on the selected soldier
	/// </summary>
	public void BtnStartClick(Action action)
	{
		// Pay upfront, no refunds
		_game.SavedGame.Funds = _game.SavedGame.Funds - _transformationRule.Cost;

		foreach (var requiredItem in _transformationRule.RequiredItems)
		{
			RuleItem rule = _game.Mod.GetItem(requiredItem.Key);
			if (rule != null)
			{
				_base.StorageItems.RemoveItem(rule, requiredItem.Value);
			}
		}

		// Generate an event (side effect)
		string choice = _transformationRule.ChooseEvent();
		if (!string.IsNullOrEmpty(choice))
		{
			RuleEvent eventToSpawn = _game.Mod.GetEvent(choice, false);
			if (eventToSpawn != null)
			{
				_game.SavedGame.SpawnEvent(eventToSpawn);
			}
		}

		// Here we go
		if (!Mod.IsEmptyRuleName(_transformationRule.ProducedItem))
		{
			Retire();
		}
		else
		{
			PerformTransformation();
		}

		_game.PopState();
	}

	/// <summary>
	/// Transforms (or clones/resurrects) the selected soldier.
	/// </summary>
	public void PerformTransformation()
	{
		Soldier destinationSoldier = null;

		if (_transformationRule.IsCreatingClone)
		{
			int newId = _game.SavedGame.GetId("STR_SOLDIER");
			RuleSoldier newSoldierType = _game.Mod.GetSoldier(_sourceSoldier.Rules.Type);
			if (!Mod.IsEmptyRuleName(_transformationRule.ProducedSoldierType))
			{
				newSoldierType = _game.Mod.GetSoldier(_transformationRule.ProducedSoldierType);
			}
			destinationSoldier = new Soldier(
				newSoldierType,
				newSoldierType.DefaultArmor,
				_sourceSoldier.Nationality, // try to preserve nationality if possible
				newId);

			// copy stuff that is not influenced by transformation ruleset
			destinationSoldier.Gender = _sourceSoldier.Gender;
			destinationSoldier.Look = _sourceSoldier.Look;
			destinationSoldier.LookVariant = _sourceSoldier.LookVariant;

			// preserve nationality only for soldiers of the same type
			if (_sourceSoldier.Rules == newSoldierType)
			{
				destinationSoldier.Nationality = _sourceSoldier.Nationality;
			}

			// allow easy name handling when cloning (e.g. John Doe III => John Doe IV)
			destinationSoldier.SetName(_edtSoldier.Text);
		}
		else
		{
			destinationSoldier = _sourceSoldier;

			// some players want to rename soldiers too
			destinationSoldier.SetName(_edtSoldier.Text);

			if (_sourceSoldier.Death != null)
			{
				// true resurrect = remove from Memorial Wall
				_game.SavedGame.DeadSoldiers.Remove(_sourceSoldier);
			}
			else if (_transformationRule.TransferTime > 0)
			{
				// transfer time on a live soldier already at the base (doesn't make much sense, but we need to handle it anyway)
				_base.Soldiers.Remove(_sourceSoldier);
			}
		}

		if (_transformationRule.TransferTime > 0 || _transformationRule.IsCreatingClone || _sourceSoldier.Death != null)
		{
			// handle training (transfer rules)
			destinationSoldier.SetPsiTraining(false);
			if (destinationSoldier.IsInTraining)
			{
				destinationSoldier.ReturnToTrainingWhenHealed = true;
			}
			destinationSoldier.SetTraining(false);

			int transferTime = _transformationRule.TransferTime > 0 ? _transformationRule.TransferTime : 24;
			var transfer = new Transfer(transferTime);
			transfer.Soldier = destinationSoldier;
			_base.Transfers.Add(transfer);
		}

		destinationSoldier.Transform(_game.Mod, _transformationRule, _sourceSoldier, _base);
	}

	/// <summary>
	/// Removes the soldier from existence and produces the item.
	/// </summary>
	public void Retire()
	{
		// remove the soldier from existence
		{
			if (_sourceSoldier.Death != null)
			{
				// I wonder if anyone will ever use THIS option
				_game.SavedGame.DeadSoldiers.Remove(_sourceSoldier);
			}
			else
			{
				_base.Soldiers.Remove(_sourceSoldier);
			}
		}

		// create the item
		{
			int transferTime = _transformationRule.TransferTime > 0 ? _transformationRule.TransferTime : 1;
			var transfer = new Transfer(transferTime);
			transfer.SetItems(_game.Mod.GetItem(_transformationRule.ProducedItem, true), 1);
			_base.Transfers.Add(transfer);
		}
	}

	/// <summary>
	/// Handles pressing the Left arrow button
	/// </summary>
	public void BtnLeftArrowClick(Action action)
	{
		int iter = _filteredListOfSoldiers.IndexOf(_sourceSoldier);
		if (iter == 0)
		{
			iter = _filteredListOfSoldiers.Count;
		}
		// C++ find() returns end() when not found: --end() is the last element
		if (iter == -1)
		{
			iter = _filteredListOfSoldiers.Count;
		}

		--iter;
		_sourceSoldier = _filteredListOfSoldiers[iter];
		InitTransformationData();
	}

	/// <summary>
	/// Handles pressing the Right arrow button
	/// </summary>
	public void BtnRightArrowClick(Action action)
	{
		int iter = _filteredListOfSoldiers.IndexOf(_sourceSoldier);
		if (iter == _filteredListOfSoldiers.Count - 1)
		{
			iter = 0;
		}
		else
		{
			// not found (C++ undefined behaviour): -1 + 1 starts at the first soldier
			++iter;
		}

		_sourceSoldier = _filteredListOfSoldiers[iter];
		InitTransformationData();
	}
}
