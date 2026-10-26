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
// Ported from: src/Ufopaedia/ArticleStateTFTDArmor.h, src/Ufopaedia/ArticleStateTFTDArmor.cpp
#nullable disable

using System.Globalization;

namespace Acom;

/// <summary>TFTD-style article for armors.</summary>
public class ArticleStateTFTDArmor : ArticleStateTFTD
{
	protected int _row;
	protected TextList _lstInfo;

	public ArticleStateTFTDArmor(ArticleDefinitionTFTD defs, ArticleCommonState state) : base(defs, state)
	{
		_row = 0;
		_txtInfo.SetHeight(72);

		_btnInfo.Visible = _game.Mod.ShowPediaInfoButton;

		Armor armor = _game.Mod.GetArmor(defs.id, true);

		_lstInfo = new TextList(150, 64, 168, 110);
		Add(_lstInfo);

		_lstInfo.SetColor(_listColor1);
		_lstInfo.SetColumns(2, 125, 25);
		_lstInfo.SetDot(true);

		// Add armor values
		AddStat("STR_FRONT_ARMOR", armor.FrontArmor);
		AddStat("STR_LEFT_ARMOR", armor.LeftSideArmor);
		AddStat("STR_RIGHT_ARMOR", armor.RightSideArmor);
		AddStat("STR_REAR_ARMOR", armor.RearArmor);
		AddStat("STR_UNDER_ARMOR", armor.UnderArmor);

		_lstInfo.AddRow(0);
		++_row;

		if (Options.oxcePediaSortResistances == 0)
		{
			// Add damage modifiers
			for (int i = 0; i < (int)ItemDamageType.DAMAGE_TYPES; ++i)
			{
				ItemDamageType dt = (ItemDamageType)i;
				int percentage = (int)Round(armor.GetDamageModifier(dt) * 100.0f);
				string damage = GetDamageTypeText(dt);
				if (percentage != 100 && damage != "STR_UNKNOWN")
				{
					AddModifier(damage, Unicode.FormatPercentage(percentage));
				}
			}
		}
		else
		{
			// Add resistances
			int counter = 0;
			bool first = true;
			for (int i = 0; i < (int)ItemDamageType.DAMAGE_TYPES; ++i)
			{
				ItemDamageType dt = (ItemDamageType)i;
				int percentage = (int)Round(armor.GetDamageModifier(dt) * 100.0f);
				string damage = GetDamageTypeText(dt);
				if (percentage < 100 && damage != "STR_UNKNOWN")
				{
					AddModifier(damage, Unicode.FormatPercentage(percentage));
					counter++;
				}
			}
			if (Options.oxcePediaSortResistances >= 2)
			{
				// Add standard damage (100%)
				for (int i = 0; i < (int)ItemDamageType.DAMAGE_TYPES; ++i)
				{
					ItemDamageType dt = (ItemDamageType)i;
					int percentage = (int)Round(armor.GetDamageModifier(dt) * 100.0f);
					string damage = GetDamageTypeText(dt);
					if (percentage == 100 && damage != "STR_UNKNOWN")
					{
						if (counter > 0 && first)
						{
							first = false;
							counter = 0;
							_lstInfo.AddRow(0);
							++_row;
						}
						AddModifier(damage, Unicode.FormatPercentage(percentage));
						counter++;
					}
				}
			}
			// Add vulnerabilities
			first = true;
			for (int i = 0; i < (int)ItemDamageType.DAMAGE_TYPES; ++i)
			{
				ItemDamageType dt = (ItemDamageType)i;
				int percentage = (int)Round(armor.GetDamageModifier(dt) * 100.0f);
				string damage = GetDamageTypeText(dt);
				if (percentage > 100 && damage != "STR_UNKNOWN")
				{
					if (counter > 0 && first)
					{
						first = false;
						counter = 0;
						_lstInfo.AddRow(0);
						++_row;
					}
					AddModifier(damage, Unicode.FormatPercentage(percentage));
				}
			}
		}

		_lstInfo.AddRow(0);
		++_row;

		// Add unit stats
		AddStat("STR_TIME_UNITS", armor.Stats.tu, true);
		AddStat("STR_STAMINA", armor.Stats.stamina, true);
		AddStat("STR_HEALTH", armor.Stats.health, true);
		AddStat("STR_BRAVERY", armor.Stats.bravery, true);
		AddStat("STR_REACTIONS", armor.Stats.reactions, true);
		AddStat("STR_FIRING_ACCURACY", armor.Stats.firing, true);
		AddStat("STR_THROWING_ACCURACY", armor.Stats.throwing, true);
		AddStat("STR_MELEE_ACCURACY", armor.Stats.melee, true);
		AddStat("STR_STRENGTH", armor.Stats.strength, true);
		AddStat("STR_MANA_POOL", armor.Stats.mana, true);
		AddStat("STR_PSIONIC_STRENGTH", armor.Stats.psiStrength, true);
		AddStat("STR_PSIONIC_SKILL", armor.Stats.psiSkill, true);

		CenterAllSurfaces();
	}

	/// <summary>C++ fmath.h Round(float).</summary>
	private static float Round(float x) => x < 0.0f ? MathF.Ceiling(x - 0.5f) : MathF.Floor(x + 0.5f);

	protected void AddStat(string label, int stat, bool plus = false)
	{
		if (stat != 0)
		{
			string ss = "";
			if (plus && stat > 0)
				ss += "+";
			ss += stat.ToString(CultureInfo.InvariantCulture);
			_lstInfo.AddRow(2, Tr(label), ss);
			_lstInfo.SetCellColor(_row, 1, _listColor2);
			++_row;
		}
	}

	protected void AddModifier(string label, string stat)
	{
		string translation = Tr(label);
		if (System.Text.Encoding.UTF8.GetByteCount(translation) > 2) // filter out unused OXCE damage types (C++ length() counts bytes)
		{
			_lstInfo.AddRow(2, Tr(label), stat);
			_lstInfo.SetCellColor(_row, 1, _listColor2);
			++_row;
		}
	}
}
