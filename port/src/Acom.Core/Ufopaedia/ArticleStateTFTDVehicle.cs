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
// Ported from: src/Ufopaedia/ArticleStateTFTDVehicle.h, src/Ufopaedia/ArticleStateTFTDVehicle.cpp
#nullable disable

using System.Globalization;

namespace Acom;

/// <summary>TFTD-style article for vehicles (HWPs).</summary>
public class ArticleStateTFTDVehicle : ArticleStateTFTD
{
	protected TextList _lstStats;
	protected TextList _lstStats2;

	public ArticleStateTFTDVehicle(ArticleDefinitionTFTD defs, ArticleCommonState state) : base(defs, state)
	{
		_txtInfo.SetHeight(72);

		RuleItem item = _game.Mod.GetItem(defs.id, true);
		Unit unit = item.VehicleUnit;
		if (unit == null)
		{
			throw new InvalidOperationException("ArticleStateTFTDVehicle: Item " + defs.id + " is missing a vehicle unit definition!");
		}
		Armor armor = unit.Armor;

		_lstStats = new TextList(150, 65, 168, 106);
		Add(_lstStats);

		_lstStats.SetColor(_listColor1);
		_lstStats.SetColumns(2, 100, 50);
		_lstStats.SetDot(true);

		_lstStats2 = new TextList(195, 33, 25, 166);
		Add(_lstStats2);

		_lstStats2.SetColor(_listColor1);
		_lstStats2.SetColumns(2, 65, 130);
		_lstStats2.SetDot(true);

		_lstStats.AddRow(2, Tr("STR_TIME_UNITS"), unit.Stats.tu.ToString(CultureInfo.InvariantCulture));

		_lstStats.AddRow(2, Tr("STR_HEALTH"), unit.Stats.health.ToString(CultureInfo.InvariantCulture));

		_lstStats.AddRow(2, Tr("STR_FRONT_ARMOR"), armor.FrontArmor.ToString(CultureInfo.InvariantCulture));

		_lstStats.AddRow(2, Tr("STR_LEFT_ARMOR"), armor.LeftSideArmor.ToString(CultureInfo.InvariantCulture));

		_lstStats.AddRow(2, Tr("STR_RIGHT_ARMOR"), armor.RightSideArmor.ToString(CultureInfo.InvariantCulture));

		_lstStats.AddRow(2, Tr("STR_REAR_ARMOR"), armor.RearArmor.ToString(CultureInfo.InvariantCulture));

		_lstStats.AddRow(2, Tr("STR_UNDER_ARMOR"), armor.UnderArmor.ToString(CultureInfo.InvariantCulture));

		_lstStats2.AddRow(2, Tr("STR_WEAPON"), Tr(defs.weapon));

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
			_lstStats2.AddRow(2, Tr("STR_WEAPON_POWER"), ss8);

			_lstStats2.AddRow(2, Tr("STR_AMMUNITION"), Tr(ammo.Name));

			_lstStats2.AddRow(2, Tr("STR_ROUNDS"), item.VehicleClipSize.ToString(CultureInfo.InvariantCulture));
		}
		else
		{
			_lstStats2.AddRow(2, Tr("STR_WEAPON_POWER"), item.Power.ToString(CultureInfo.InvariantCulture));
		}

		for (int i = 0; i != _lstStats.Texts; ++i)
		{
			_lstStats.SetCellColor(i, 1, _listColor2);
		}
		for (int i = 0; i != _lstStats2.Texts; ++i)
		{
			_lstStats2.SetCellColor(i, 1, _listColor2);
		}

		CenterAllSurfaces();
	}
}
