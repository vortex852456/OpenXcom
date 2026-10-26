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
// Ported from: src/Ufopaedia/ArticleStateVehicle.h, src/Ufopaedia/ArticleStateVehicle.cpp
#nullable disable

using System.Globalization;

namespace Acom;

/// <summary>ArticleStateVehicle has a caption, text and a stats block.</summary>
public class ArticleStateVehicle : ArticleState
{
	protected Text _txtTitle;
	protected Text _txtInfo;
	protected TextList _lstStats;

	public ArticleStateVehicle(ArticleDefinitionVehicle article_defs, ArticleCommonState state) : base(article_defs.id, state)
	{
		ArticleDefinitionVehicle defs = article_defs;
		RuleItem item = _game.Mod.GetItem(defs.id, true);
		Unit unit = item.VehicleUnit;
		if (unit == null)
		{
			throw new InvalidOperationException("ArticleStateVehicle: Item " + defs.id + " is missing a vehicle unit definition!");
		}
		Armor armor = unit.Armor;

		// add screen elements
		_txtTitle = new Text(310, 17, 5, 23);
		_txtInfo = new Text(300, 150, 10, 122);
		_lstStats = new TextList(300, 89, 10, 48);

		// Set palette
		if (defs.customPalette)
		{
			SetCustomPalette(_game.Mod.GetSurface(defs.image_id).Palette, Mod.UFOPAEDIA_CURSOR);
		}
		else
		{
			SetStandardPalette("PAL_UFOPAEDIA");
		}

		InitLayout();

		// add other elements
		Add(_txtTitle);
		Add(_txtInfo);
		Add(_lstStats);

		// Set up objects
		if (!string.IsNullOrEmpty(defs.image_id))
		{
			_game.Mod.GetSurface(defs.image_id).BlitNShade(_bg, 0, 0);
		}
		else
		{
			_game.Mod.GetSurface("BACK10.SCR").BlitNShade(_bg, 0, 0);
		}
		_btnOk.SetColor(Acom.Palette.BlockOffset(5));
		_btnPrev.SetColor(Acom.Palette.BlockOffset(5));
		_btnNext.SetColor(Acom.Palette.BlockOffset(5));

		_txtTitle.SetColor((byte)(Acom.Palette.BlockOffset(15) + 4));
		_txtTitle.SetBig();
		_txtTitle.SetText(Tr(defs.GetTitleForPage(_state.current_page)));

		_txtInfo.SetColor((byte)(Acom.Palette.BlockOffset(15) - 1));
		_txtInfo.SetSecondaryColor((byte)(Acom.Palette.BlockOffset(15) + 4));
		_txtInfo.SetWordWrap(true);
		_txtInfo.SetScrollable(true);
		_txtInfo.SetText(Tr(defs.GetTextForPage(_state.current_page)));

		_lstStats.SetColor((byte)(Acom.Palette.BlockOffset(15) + 4));
		_lstStats.SetColumns(2, 175, 145);
		_lstStats.SetDot(true);

		_lstStats.AddRow(2, Tr("STR_TIME_UNITS"), unit.Stats.tu.ToString(CultureInfo.InvariantCulture));

		_lstStats.AddRow(2, Tr("STR_HEALTH"), unit.Stats.health.ToString(CultureInfo.InvariantCulture));

		_lstStats.AddRow(2, Tr("STR_FRONT_ARMOR"), armor.FrontArmor.ToString(CultureInfo.InvariantCulture));

		_lstStats.AddRow(2, Tr("STR_LEFT_ARMOR"), armor.LeftSideArmor.ToString(CultureInfo.InvariantCulture));

		_lstStats.AddRow(2, Tr("STR_RIGHT_ARMOR"), armor.RightSideArmor.ToString(CultureInfo.InvariantCulture));

		_lstStats.AddRow(2, Tr("STR_REAR_ARMOR"), armor.RearArmor.ToString(CultureInfo.InvariantCulture));

		_lstStats.AddRow(2, Tr("STR_UNDER_ARMOR"), armor.UnderArmor.ToString(CultureInfo.InvariantCulture));

		_lstStats.AddRow(2, Tr("STR_WEAPON"), Tr(defs.weapon));

		if (item.VehicleClipAmmo != null)
		{
			RuleItem ammo = item.VehicleClipAmmo;

			string ss8;
			if (item.IgnoreAmmoPower)
			{
				ss8 = item.Power.ToString(CultureInfo.InvariantCulture);
			}
			else
			{
				ss8 = ammo.Power.ToString(CultureInfo.InvariantCulture);
			}
			_lstStats.AddRow(2, Tr("STR_WEAPON_POWER"), ss8);

			_lstStats.AddRow(2, Tr("STR_AMMUNITION"), Tr(ammo.Name));

			_lstStats.AddRow(2, Tr("STR_ROUNDS"), item.VehicleClipSize.ToString(CultureInfo.InvariantCulture));

			_txtInfo.Y = 138;
		}
		else
		{
			_lstStats.AddRow(2, Tr("STR_WEAPON_POWER"), item.Power.ToString(CultureInfo.InvariantCulture));
		}

		CenterAllSurfaces();
	}
}
