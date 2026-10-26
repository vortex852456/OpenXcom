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
// Ported from: src/Basescape/SoldierBonusState.h, src/Basescape/SoldierBonusState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)
#pragma warning disable IDE0060 // handler signatures keep the unused Action parameter like C++

/// <summary>
/// SoldierBonus window displays all soldier bonuses.
/// </summary>
public class SoldierBonusState : State
{
	private readonly Base _base;
	private readonly int _soldier; // C++ size_t
	private readonly List<string> _bonuses = [];
	private readonly TextButton _btnCancel;
	private readonly ToggleTextButton _btnSummary;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtType;
	private readonly TextList _lstBonuses;
	private readonly TextList _lstSummary;

	private static string Str(int value) => value.ToString(CultureInfo.InvariantCulture);

	/// <summary>C++ helper::SingleRun: true only on the first call after a reset.</summary>
	private sealed class SingleRun
	{
		private bool _done;

		public bool TryRun()
		{
			if (_done)
			{
				return false;
			}
			_done = true;
			return true;
		}

		public void Reset() => _done = false;
	}

	/// <summary>
	/// Initializes all the elements in the SoldierBonus window.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="soldier">ID of the selected soldier.</param>
	public SoldierBonusState(Base @base, int soldier)
	{
		_base = @base;
		_soldier = soldier;
		_screen = false;

		// Create objects
		_window = new Window(this, 192, 160, 64, 20, WindowPopup.POPUP_BOTH);
		_btnSummary = new ToggleTextButton(84, 16, 73, 156);
		_btnCancel = new TextButton(84, 16, 164, 156);
		_txtTitle = new Text(182, 16, 69, 28);
		_txtType = new Text(90, 9, 80, 52);
		_lstBonuses = new TextList(158, 80, 73, 68);
		_lstSummary = new TextList(158, 80, 73, 68);

		// Set palette
		SetInterface("soldierBonus");

		Add(_window, "window", "soldierBonus");
		Add(_btnSummary, "button", "soldierBonus");
		Add(_btnCancel, "button", "soldierBonus");
		Add(_txtTitle, "text", "soldierBonus");
		Add(_txtType, "text", "soldierBonus");
		Add(_lstBonuses, "list", "soldierBonus");
		Add(_lstSummary, "list", "soldierBonus");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "soldierBonus");

		_btnSummary.Text = Tr("STR_SUMMARY");
		_btnSummary.OnMouseClick(BtnSummaryClick);

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		Soldier s = _base != null ? _base.Soldiers[_soldier] : _game.SavedGame.DeadSoldiers[_soldier];
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_SOLDIER_BONUSES_FOR").Arg(s.GetName()));

		_txtType.SetText(Tr("STR_TYPE"));

		_lstBonuses.SetColumns(1, 150);
		_lstBonuses.SetSelectable(true);
		_lstBonuses.SetBackground(_window);
		_lstBonuses.Margin = 8;

		_bonuses.Clear();
		_lstBonuses.ClearList();
		foreach (var bonusRule in s.GetBonuses(null))
		{
			_bonuses.Add(bonusRule.Name);
			_lstBonuses.AddRow(1, Tr(bonusRule.Name));
		}

		_lstBonuses.OnMouseClick(LstBonusesClick);

		_lstSummary.SetColumns(2, 122, 20);
		_lstSummary.SetAlign(TextHAlign.ALIGN_RIGHT, 1);
		_lstSummary.SetSelectable(true);
		_lstSummary.SetBackground(_window);
		_lstSummary.Margin = 8;
		_lstSummary.Visible = false;

		int visibilityAtDark = 0;
		int visibilityAtDay = 0;
		int psiVision = 0;
		int bonusVisibilityThroughSmoke = 0;
		int bonusVisibilityThroughFire = 0;

		int frontArmor = 0, leftArmor = 0, rightArmor = 0, rearArmor = 0, underArmor = 0;
		var stats = new UnitStats();
		bool timeRecovery = false, energyRecovery = false, moraleRecovery = false, healthRecovery = false, stunRecovery = false, manaRecovery = false;
		foreach (var bonusRule in s.GetBonuses(null))
		{
			frontArmor += bonusRule.FrontArmor;
			leftArmor += bonusRule.LeftSideArmor;
			rightArmor += bonusRule.RightSideArmor;
			rearArmor += bonusRule.RearArmor;
			underArmor += bonusRule.UnderArmor;

			visibilityAtDark += bonusRule.VisibilityAtDark;
			visibilityAtDay += bonusRule.VisibilityAtDay;
			psiVision += bonusRule.PsiVision;
			bonusVisibilityThroughSmoke += bonusRule.VisibilityThroughSmoke;
			bonusVisibilityThroughFire += bonusRule.VisibilityThroughFire;

			stats += bonusRule.Stats;
			timeRecovery = timeRecovery || bonusRule.TimeRecoveryRaw.IsModded;
			energyRecovery = energyRecovery || bonusRule.EnergyRecoveryRaw.IsModded;
			moraleRecovery = moraleRecovery || bonusRule.MoraleRecoveryRaw.IsModded;
			healthRecovery = healthRecovery || bonusRule.HealthRecoveryRaw.IsModded;
			stunRecovery = stunRecovery || bonusRule.StunRegenerationRaw.IsModded;
			manaRecovery = manaRecovery || bonusRule.ManaRecoveryRaw.IsModded;
		}
		if (stats.tu != 0)
			_lstSummary.AddRow(2, Tr("STR_TIME_UNITS"), Str(stats.tu));
		if (stats.stamina != 0)
			_lstSummary.AddRow(2, Tr("STR_STAMINA"), Str(stats.stamina));
		if (stats.health != 0)
			_lstSummary.AddRow(2, Tr("STR_HEALTH"), Str(stats.health));
		if (stats.bravery != 0)
			_lstSummary.AddRow(2, Tr("STR_BRAVERY"), Str(stats.bravery));
		if (stats.reactions != 0)
			_lstSummary.AddRow(2, Tr("STR_REACTIONS"), Str(stats.reactions));
		if (stats.firing != 0)
			_lstSummary.AddRow(2, Tr("STR_FIRING_ACCURACY"), Str(stats.firing));
		if (stats.throwing != 0)
			_lstSummary.AddRow(2, Tr("STR_THROWING_ACCURACY"), Str(stats.throwing));
		if (stats.melee != 0)
			_lstSummary.AddRow(2, Tr("STR_MELEE_ACCURACY"), Str(stats.melee));
		if (stats.strength != 0)
			_lstSummary.AddRow(2, Tr("STR_STRENGTH"), Str(stats.strength));
		if (stats.mana != 0)
			_lstSummary.AddRow(2, Tr("STR_MANA_POOL"), Str(stats.mana));
		if (stats.psiStrength != 0)
			_lstSummary.AddRow(2, Tr("STR_PSIONIC_STRENGTH"), Str(stats.psiStrength));
		if (stats.psiSkill != 0)
			_lstSummary.AddRow(2, Tr("STR_PSIONIC_SKILL"), Str(stats.psiSkill));

		var gap = new SingleRun();


		gap.Reset();
		if (frontArmor != 0)
		{
			if (gap.TryRun()) _lstSummary.AddRow(1, "");
			_lstSummary.AddRow(2, Tr("STR_FRONT_ARMOR_UC"), Str(frontArmor));
		}
		if (leftArmor != 0)
		{
			if (gap.TryRun()) _lstSummary.AddRow(1, "");
			_lstSummary.AddRow(2, Tr("STR_LEFT_ARMOR_UC"), Str(leftArmor));
		}
		if (rightArmor != 0)
		{
			if (gap.TryRun()) _lstSummary.AddRow(1, "");
			_lstSummary.AddRow(2, Tr("STR_RIGHT_ARMOR_UC"), Str(rightArmor));
		}
		if (rearArmor != 0)
		{
			if (gap.TryRun()) _lstSummary.AddRow(1, "");
			_lstSummary.AddRow(2, Tr("STR_REAR_ARMOR_UC"), Str(rearArmor));
		}
		if (underArmor != 0)
		{
			if (gap.TryRun()) _lstSummary.AddRow(1, "");
			_lstSummary.AddRow(2, Tr("STR_UNDER_ARMOR_UC"), Str(underArmor));
		}

		gap.Reset();
		if (visibilityAtDark != 0)
		{
			if (gap.TryRun()) _lstSummary.AddRow(1, "");
			_lstSummary.AddRow(2, Tr("visibilityAtDark"), Str(visibilityAtDark));
		}
		if (visibilityAtDay != 0)
		{
			if (gap.TryRun()) _lstSummary.AddRow(1, "");
			_lstSummary.AddRow(2, Tr("visibilityAtDay"), Str(visibilityAtDay));
		}
		if (psiVision != 0)
		{
			if (gap.TryRun()) _lstSummary.AddRow(1, "");
			_lstSummary.AddRow(2, Tr("psiVision"), Str(psiVision));
		}
		if (bonusVisibilityThroughSmoke != 0)
		{
			if (gap.TryRun()) _lstSummary.AddRow(1, "");
			_lstSummary.AddRow(2, Tr("heatVision"), Str(bonusVisibilityThroughSmoke));
		}
		if (bonusVisibilityThroughFire != 0)
		{
			if (gap.TryRun()) _lstSummary.AddRow(1, "");
			_lstSummary.AddRow(2, Tr("visibilityThroughFire"), Str(bonusVisibilityThroughFire));
		}

		if (timeRecovery || energyRecovery || moraleRecovery || healthRecovery || stunRecovery || manaRecovery)
		{
			_lstSummary.AddRow(1, "");
			_lstSummary.AddRow(1, Tr("recovery"));
			if (timeRecovery)
				_lstSummary.AddRow(1, Tr("time"));
			if (energyRecovery)
				_lstSummary.AddRow(1, Tr("energy"));
			if (moraleRecovery)
				_lstSummary.AddRow(1, Tr("morale"));
			if (healthRecovery)
				_lstSummary.AddRow(1, Tr("health"));
			if (stunRecovery)
				_lstSummary.AddRow(1, Tr("stun"));
			if (manaRecovery)
				_lstSummary.AddRow(1, Tr("mana"));
		}
	}

	/// <summary>
	/// Toggles Summary view.
	/// </summary>
	public void BtnSummaryClick(Action action)
	{
		_lstBonuses.Visible = !_btnSummary.Pressed;
		_lstSummary.Visible = _btnSummary.Pressed;
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Shows corresponding Stats for Nerds article.
	/// </summary>
	public void LstBonusesClick(Action action)
	{
		string articleId = _bonuses[(int)_lstBonuses.SelectedRow];
		_game.PushState(new StatsForNerdsState(UfopaediaTypeId.UFOPAEDIA_TYPE_UNKNOWN, articleId, false, false, false));
	}
}
