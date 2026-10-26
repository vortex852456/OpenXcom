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
// Ported from: src/Basescape/PlaceFacilityState.h, src/Basescape/PlaceFacilityState.cpp
#nullable disable

using System.Globalization;
using System.Text;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window shown when the player tries to
/// build a facility.
/// </summary>
public class PlaceFacilityState : State
{
	protected Base _base;
	protected RuleBaseFacility _rule;
	protected BaseFacility _origFac;
	protected BaseView _view;
	protected TextButton _btnCancel;
	protected Window _window;
	protected Text _txtFacility;
	protected Text _txtCost;
	protected Text _numCost;
	protected Text _numResources;
	protected Text _txtTime;
	protected Text _numTime;
	protected Text _txtMaintenance;
	protected Text _numMaintenance;

	/// <summary>
	/// Initializes all the elements in the Place Facility window.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="rule">Pointer to the facility ruleset to build.</param>
	/// <param name="origFac">Facility being moved (null: build a new one).</param>
	public PlaceFacilityState(Base @base, RuleBaseFacility rule, BaseFacility origFac = null)
	{
		_base = @base;
		_rule = rule;
		_origFac = origFac;
		_screen = false;

		// Create objects
		_window = new Window(this, 128, 160, 192, 40);
		_view = new BaseView(192, 192, 0, 8);
		_btnCancel = new TextButton(112, 16, 200, 176);
		_txtFacility = new Text(110, 9, 202, 50);
		_txtCost = new Text(110, 9, 202, 62);
		_numCost = new Text(110, 17, 202, 70);
		_numResources = new Text(110, 25, 202, 87);
		int resourceTextOffset = 9 * Math.Min(3, (_origFac == null ? _rule.BuildCostItems.Count : 0));
		_txtTime = new Text(110, 9, 202, 90 + resourceTextOffset);
		_numTime = new Text(110, 17, 202, 98 + resourceTextOffset);
		_txtMaintenance = new Text(110, 9, 202, 118 + resourceTextOffset);
		_numMaintenance = new Text(110, 17, 202, 126 + resourceTextOffset);

		// Set palette
		SetInterface("placeFacility");

		Add(_window, "window", "placeFacility");
		Add(_view, "baseView", "basescape");
		Add(_btnCancel, "button", "placeFacility");
		Add(_txtFacility, "text", "placeFacility");
		Add(_txtCost, "text", "placeFacility");
		Add(_numCost, "numbers", "placeFacility");
		Add(_numResources, "numbers", "placeFacility");
		Add(_txtTime, "text", "placeFacility");
		Add(_numTime, "numbers", "placeFacility");
		Add(_txtMaintenance, "text", "placeFacility");
		Add(_numMaintenance, "numbers", "placeFacility");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "placeFacility");

		var itf = _game.Mod.GetInterface("basescape").GetElementOptional("trafficLights");
		if (itf != null)
		{
			_view.SetOtherColors((byte)itf.color, (byte)itf.color2, (byte)itf.border, !itf.TFTDMode);
		}
		_view.SetTexture(_game.Mod.GetSurfaceSet("BASEBITS.PCK"));
		_view.SetBase(_base);
		_view.SetSelectable(rule.SizeX, rule.SizeY);
		_view.OnMouseClick(ViewClick);

		_btnCancel.Text = Tr("STR_CANCEL");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_txtFacility.SetText(Tr(_rule.Type));

		_txtCost.SetText(Tr("STR_COST_UC"));

		_numCost.SetBig();
		_numCost.SetText(Unicode.FormatFunding(_origFac != null ? 0 : _rule.BuildCost));

		if (_origFac == null && _rule.BuildCostItems.Count != 0)
		{
			var ss = new StringBuilder();

			// Currently, the text box will only fit three lines of items.
			// But I'm going to add everything to the list anyway.
			foreach (var item in _rule.BuildCostItems)
			{
				// Note: `item` is of the form (item name, (cost number, refund number))
				ss.Append(ResourceLine(Tr(item.Key), item.Value.first)).Append('\n');
			}
			_numResources.SetText(ss.ToString());
		}

		_txtTime.SetText(Tr("STR_CONSTRUCTION_TIME_UC"));

		_numTime.SetBig();
		_numTime.SetText(Tr("STR_DAY", _origFac != null ? 0 : _rule.BuildTime));

		_txtMaintenance.SetText(Tr("STR_MAINTENANCE_UC"));

		_numMaintenance.SetBig();
		_numMaintenance.SetText(Unicode.FormatFunding(_rule.MonthlyCost));
	}

	/// <summary>
	/// One line of the build cost item list: the name is cut to fit (19 code points, less for big numbers).
	/// </summary>
	internal static string ResourceLine(string name, int cost)
	{
		int max = 19;
		if (cost > 9) --max;
		if (cost > 99) --max;
		if (name.EnumerateRunes().Count() > max) // Unicode::codePointLengthUTF8
		{
			// Unicode::codePointSubstrUTF8(name, 0, max)
			var sub = new StringBuilder();
			int count = 0;
			foreach (var rune in name.EnumerateRunes())
			{
				if (count++ == max)
					break;
				sub.Append(rune.ToString());
			}
			name = sub.ToString();
		}
		return string.Create(CultureInfo.InvariantCulture, $"{name}: {cost}");
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Processes clicking on facilities.
	/// </summary>
	public void ViewClick(Action action)
	{
		if (_origFac != null)
		{
			// EXPERIMENTAL!: just moving an existing facility
			if (_view.GridX == _origFac.X && _view.GridY == _origFac.Y)
			{
				// unchanged location -> no message, no cost.
				_game.PopState();
			}
			else if (_view.GetPlacementError(_rule, _origFac) != BasePlacementErrors.BPE_None)
			{
				_game.PushState(new ErrorMessageState(Tr("STR_CANNOT_BUILD_HERE"), _palette, (byte)_game.Mod.GetInterface("placeFacility").GetElement("errorMessage").color, "BACK01.SCR", _game.Mod.GetInterface("placeFacility").GetElement("errorPalette").color));
			}
			else
			{
				_origFac.X = _view.GridX;
				_origFac.Y = _view.GridY;
				if (Options.allowBuildingQueue)
				{
					// first reset (maybe the moved facility is not queued anymore)
					if (Math.Abs(_origFac.BuildTime) > _rule.BuildTime) _origFac.BuildTime = _rule.BuildTime;
					// if it is still in the queue though, recalc
					if (_origFac.BuildTime > 0 && _view.IsQueuedBuilding(_rule)) _origFac.BuildTime = int.MaxValue;
					_view.ReCalcQueuedBuildings();
				}
				_game.PopState();
			}
		}
		else
		{
			// Pre-calculate refunds
			BaseAreaSubset areaToBuildOverTemp = new BaseAreaSubset(_rule.SizeX, _rule.SizeY).Offset(_view.GridX, _view.GridY);
			int refundValueTemp = 0;
			var refundItemsTemp = new Dictionary<string, int>(StringComparer.Ordinal);
			for (int i = _base.Facilities.Count - 1; i >= 0; --i)
			{
				BaseFacility checkFacilityTemp = _base.Facilities[i];
				if ((bool)BaseAreaSubset.Intersection(areaToBuildOverTemp, checkFacilityTemp.Placement))
				{
					var itemCostTemp = checkFacilityTemp.Rules.BuildCostItems;
					if (checkFacilityTemp.BuildTime > checkFacilityTemp.Rules.BuildTime)
					{
						refundValueTemp = checkFacilityTemp.Rules.BuildCost;
						foreach (var itemTemp in itemCostTemp)
							refundItemsTemp[itemTemp.Key] = refundItemsTemp.GetValueOrDefault(itemTemp.Key) + itemTemp.Value.first;
					}
					else
					{
						refundValueTemp = checkFacilityTemp.Rules.RefundValue;
						foreach (var itemTemp in itemCostTemp)
							refundItemsTemp[itemTemp.Key] = refundItemsTemp.GetValueOrDefault(itemTemp.Key) + itemTemp.Value.second;
					}
					if (checkFacilityTemp.Ammo > 0)
					{
						string ammoType = checkFacilityTemp.Rules.AmmoItem.Type;
						refundItemsTemp[ammoType] = refundItemsTemp.GetValueOrDefault(ammoType) + checkFacilityTemp.Ammo;
					}
				}
			}

			// placing a brand new facility
			BasePlacementErrors placementErrorCode = _view.GetPlacementError(_rule);
			if (placementErrorCode != BasePlacementErrors.BPE_None)
			{
				byte errorColor1 = (byte)_game.Mod.GetInterface("placeFacility").GetElement("errorMessage").color;
				int errorColor2 = _game.Mod.GetInterface("placeFacility").GetElement("errorPalette").color;
				var (msg, bg) = PlacementErrorMessage(placementErrorCode);
				_game.PushState(new ErrorMessageState(Tr(msg), _palette, errorColor1, bg, errorColor2));
			}
			else if (_game.SavedGame.Funds < (_rule.BuildCost - refundValueTemp))
			{
				_game.PopState();
				_game.PushState(new ErrorMessageState(Tr("STR_NOT_ENOUGH_MONEY"), _palette, (byte)_game.Mod.GetInterface("placeFacility").GetElement("errorMessage").color, "BACK01.SCR", _game.Mod.GetInterface("placeFacility").GetElement("errorPalette").color));
			}
			else
			{
				foreach (var item in _rule.BuildCostItems)
				{
					// C++ map operator[] inserts missing keys with 0
					int needed = (item.Value.first - refundItemsTemp.GetValueOrDefault(item.Key)) - _base.StorageItems.GetItem(item.Key);
					if (needed > 0)
					{
						_game.PopState();
						_game.PushState(new ErrorMessageState(Tr("STR_NOT_ENOUGH_ITEMS").Arg(Tr(item.Key)).Arg(needed), _palette, (byte)_game.Mod.GetInterface("placeFacility").GetElement("errorMessage").color, "BACK01.SCR", _game.Mod.GetInterface("placeFacility").GetElement("errorPalette").color));
						return;
					}
				}
				// Remove any facilities we're building over
				double reducedBuildTime = 0.0;
				bool buildingOver = false;
				BaseAreaSubset areaToBuildOver = new BaseAreaSubset(_rule.SizeX, _rule.SizeY).Offset(_view.GridX, _view.GridY);
				for (int i = _base.Facilities.Count - 1; i >= 0; --i)
				{
					BaseFacility checkFacility = _base.Facilities[i];
					if ((bool)BaseAreaSubset.Intersection(areaToBuildOver, checkFacility.Placement))
					{
						// Get a refund from the facility we're building over
						var itemCost = checkFacility.Rules.BuildCostItems;

						if (checkFacility.BuildTime > checkFacility.Rules.BuildTime)
						{
							// Give full refund if this is a (not yet started) queued build.
							_game.SavedGame.Funds = _game.SavedGame.Funds + checkFacility.Rules.BuildCost;
							foreach (var item in itemCost)
							{
								_base.StorageItems.AddItem(_game.Mod.GetItem(item.Key, true), item.Value.first);
							}
						}
						else
						{
							// Give partial refund if this is a started build or a completed facility.
							_game.SavedGame.Funds = _game.SavedGame.Funds + checkFacility.Rules.RefundValue;
							foreach (var item in itemCost)
							{
								_base.StorageItems.AddItem(_game.Mod.GetItem(item.Key, true), item.Value.second);
							}

							// Reduce the build time of the new facility
							double oldSizeSquared = (checkFacility.Rules.SizeX * checkFacility.Rules.SizeY);
							double newSizeSquared = (_rule.SizeX * _rule.SizeY);
							reducedBuildTime += (checkFacility.Rules.BuildTime - checkFacility.BuildTime) * oldSizeSquared / newSizeSquared;

							// This only counts as building over something if it wasn't in construction
							if (checkFacility.BuildTime == 0)
								buildingOver = true;
						}
						if (checkFacility.Ammo > 0)
						{
							// Full refund of loaded ammo
							_base.StorageItems.AddItem(checkFacility.Rules.AmmoItem, checkFacility.Ammo);
							checkFacility.Ammo = 0;
						}

						// Remove the facility from the base
						_base.Facilities.RemoveAt(i);
					}

				}

				var fac = new BaseFacility(_rule, _base);
				fac.X = _view.GridX;
				fac.Y = _view.GridY;
				fac.BuildTime = _rule.BuildTime;
				if (buildingOver)
				{
					fac.IfHadPreviousFacility = true;
					reducedBuildTime = reducedBuildTime * _game.Mod.BuildTimeReductionScaling / 100.0;
					int reducedBuildTimeRounded = (int)Math.Round(reducedBuildTime, MidpointRounding.AwayFromZero);
					fac.BuildTime = Math.Max(1, fac.BuildTime - reducedBuildTimeRounded);
				}
				_base.Facilities.Add(fac);
				if (fac.Rules.PlaceSound != Mod.NO_SOUND)
				{
					_game.Mod.GetSound("GEO.CAT", fac.Rules.PlaceSound).Play();
				}
				if (Options.allowBuildingQueue)
				{
					if (_view.IsQueuedBuilding(_rule)) fac.BuildTime = int.MaxValue;
					_view.ReCalcQueuedBuildings();
				}
				_view.SetBase(_base);
				_game.SavedGame.Funds = _game.SavedGame.Funds - _rule.BuildCost;
				foreach (var item in _rule.BuildCostItems)
				{
					_base.StorageItems.RemoveItem(item.Key, item.Value.first);
				}
				if (!_game.IsShiftPressed())
				{
					_game.PopState();
				}
			}
		}
	}

	/// <summary>Error message and background for a placement error (the switch of C++ viewClick).</summary>
	internal static (string msg, string bg) PlacementErrorMessage(BasePlacementErrors placementErrorCode) => placementErrorCode switch
	{
		BasePlacementErrors.BPE_Used_Stores => ("STR_FACILITY_IN_USE_STORAGE", "BACK01.SCR"),
		BasePlacementErrors.BPE_Used_Quarters => ("STR_FACILITY_IN_USE_QUARTERS", "BACK01.SCR"),
		BasePlacementErrors.BPE_Used_Laboratories => ("STR_FACILITY_IN_USE_LABORATORIES", "BACK01.SCR"),
		BasePlacementErrors.BPE_Used_Workshops => ("STR_FACILITY_IN_USE_WORKSHOPS", "BACK01.SCR"),
		BasePlacementErrors.BPE_Used_Hangars => ("STR_FACILITY_IN_USE_HANGARS", "BACK01.SCR"),
		BasePlacementErrors.BPE_Used_PsiLabs => ("STR_FACILITY_IN_USE_PSI_LABS", "BACK01.SCR"),
		BasePlacementErrors.BPE_Used_Gyms => ("STR_FACILITY_IN_USE_GYMS", "BACK01.SCR"),
		BasePlacementErrors.BPE_Used_AlienContainment => ("STR_FACILITY_IN_USE_PRISONS", "BACK01.SCR"),
		BasePlacementErrors.BPE_NotConnected => ("STR_CANNOT_BUILD_HERE", "BACK01.SCR"),
		BasePlacementErrors.BPE_Used => ("STR_FACILITY_IN_USE", "BACK01.SCR"),
		BasePlacementErrors.BPE_Upgrading => ("STR_CANNOT_UPGRADE_FACILITY_ALREADY_UPGRADING", "BACK01.SCR"),
		BasePlacementErrors.BPE_UpgradeSizeMismatch => ("STR_CANNOT_UPGRADE_FACILITY_WRONG_SIZE", "BACK13.SCR"),
		BasePlacementErrors.BPE_UpgradeRequireSpecific => ("STR_CANNOT_UPGRADE_FACILITY_WRONG_TYPE", "BACK13.SCR"),
		BasePlacementErrors.BPE_UpgradeDisallowed => ("STR_CANNOT_UPGRADE_FACILITY_DISALLOWED", "BACK13.SCR"),
		BasePlacementErrors.BPE_Queue => ("STR_CANNOT_BUILD_QUEUE_OFF", "BACK13.SCR"),
		BasePlacementErrors.BPE_ForbiddenByOther => ("STR_FACILITY_FORBIDDEN_BY_OTHER", "BACK01.SCR"),
		BasePlacementErrors.BPE_ForbiddenByThis => ("STR_FACILITY_OTHER_FORBIDDEN_BY_THIS", "BACK01.SCR"),
		BasePlacementErrors.BPE_UpgradeOnly => ("STR_CANNOT_BUILD_UPGRADE_ONLY", "BACK13.SCR"),
		_ => ("STR_CANNOT_BUILD_HERE", "BACK01.SCR"),
	};
}
