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
// Ported from: src/Ufopaedia/ArticleStateArmor.h, src/Ufopaedia/ArticleStateArmor.cpp
#nullable disable

using System.Globalization;

namespace Acom;

/// <summary>ArticleStateArmor has a caption, text, preview image and a stats block.</summary>
public class ArticleStateArmor : ArticleState
{
	protected int _row;
	protected Text _txtTitle;
	protected TextList _lstInfo;
	protected Text _txtInfo;
	protected byte _buttonColor;
	protected byte _textColor;
	protected byte _textColor2;
	protected byte _listColor1;
	protected byte _listColor2;

	public ArticleStateArmor(ArticleDefinitionArmor article_defs, ArticleCommonState state) : base(article_defs.id, state)
	{
		ArticleDefinitionArmor defs = article_defs;
		_row = 0;
		Armor armor = _game.Mod.GetArmor(defs.id, true);

		// add screen elements
		_txtTitle = new Text(300, 17, 5, 24);

		// Set palette
		Surface customArmorSprite = string.IsNullOrEmpty(defs.image_id) ? null : _game.Mod.GetSurface(defs.image_id, true);
		if (defs.customPalette && customArmorSprite != null)
		{
			SetCustomPalette(customArmorSprite.Palette, Mod.BATTLESCAPE_CURSOR);
		}
		else
		{
			SetStandardPalette("PAL_BATTLEPEDIA");
		}

		RuleInterface itf = _game.Mod.GetInterface("articleArmor");
		_buttonColor = (byte)itf.GetElement("button").color;
		_textColor = (byte)itf.GetElement("text").color;
		_textColor2 = (byte)itf.GetElement("text").color2;
		_listColor1 = (byte)itf.GetElement("list").color;
		_listColor2 = (byte)itf.GetElement("list").color2;

		InitLayout();

		// add other elements
		Add(_txtTitle);

		// Set up objects
		_btnOk.SetColor(_buttonColor);
		_btnPrev.SetColor(_buttonColor);
		_btnNext.SetColor(_buttonColor);
		_btnInfo.SetColor(_buttonColor);
		_btnInfo.Visible = _game.Mod.ShowPediaInfoButton;

		_txtTitle.SetColor(_textColor);
		_txtTitle.SetBig();
		_txtTitle.SetText(Tr(defs.GetTitleForPage(_state.current_page)));

		// optional background image
		if (!defs.customPalette)
		{
			string bgImageName = itf.GetBackgroundImage(_game.Mod, _game.SavedGame);
			if (!string.IsNullOrEmpty(bgImageName))
			{
				_game.Mod.GetSurface(bgImageName).BlitNShade(_bg, 0, 0);
			}
		}

		if (customArmorSprite != null)
		{
			// blit on the background, so that text and button are always visible
			customArmorSprite.BlitNShade(_bg, 0, 0);
		}
		else if (armor.HasLayersDefinition)
		{
			// dummy default soldier (M0)
			Soldier s = new Soldier(_game.Mod.GetSoldier(_game.Mod.SoldiersList[0], true), armor, 0 /*nationality*/, 0 /*id*/);
			s.Gender = SoldierGender.GENDER_MALE;
			s.Look = SoldierLook.LOOK_BLONDE;
			s.LookVariant = 0;

			foreach (var layer in s.GetArmorLayers())
			{
				Surface surf = _game.Mod.GetSurface(layer, true);
				surf.BlitNShade(_bg, 0, 0);
			}
		}
		else
		{
			string look = armor.SpriteInventory;
			look += "M0.SPK";
			if (_game.Mod.GetSurface(look, false) == null)
			{
				look = armor.SpriteInventory + ".SPK";
			}
			if (_game.Mod.GetSurface(look, false) == null)
			{
				look = armor.SpriteInventory;
			}
			_game.Mod.GetSurface(look, true).BlitNShade(_bg, 0, 0);
		}

		_lstInfo = new TextList(150, 96, 150, 46);
		Add(_lstInfo);

		_lstInfo.SetColor(_listColor1);
		_lstInfo.SetColumns(2, 125, 25);
		_lstInfo.SetDot(true);

		_txtInfo = new Text(300, 48, 8, 150);
		Add(_txtInfo);

		_txtInfo.SetColor(_textColor);
		_txtInfo.SetSecondaryColor(_textColor2);
		_txtInfo.SetWordWrap(true);
		_txtInfo.SetScrollable(true);
		_txtInfo.SetText(Tr(defs.GetTextForPage(_state.current_page)));

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
