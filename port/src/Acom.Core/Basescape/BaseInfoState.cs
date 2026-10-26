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
// Ported from: src/Basescape/BaseInfoState.h, src/Basescape/BaseInfoState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Base Info screen that shows all the
/// stats of a base from the Basescape.
/// </summary>
public class BaseInfoState : State
{
	private const int MAX_BAR_WIDTH = 140;

	private Base _base;
	private readonly BasescapeState _state;

	private readonly Surface _bg;
	private readonly MiniBaseView _mini;
	private readonly TextButton _btnOk, _btnTransfers, _btnStores, _btnMonthlyCosts;
	private readonly TextEdit _edtBase;

	private readonly Text _txtPersonnel, _txtSoldiers, _txtEngineers, _txtScientists;
	private readonly Text _numSoldiers, _numEngineers, _numScientists;
	private readonly Bar _barSoldiers, _barEngineers, _barScientists;

	private readonly Text _txtSpace, _txtQuarters, _txtStores, _txtLaboratories, _txtWorkshops, _txtContainment, _txtHangars;
	private readonly Text _numQuarters, _numStores, _numLaboratories, _numWorkshops, _numContainment, _numHangars;
	private readonly Bar _barQuarters, _barStores, _barLaboratories, _barWorkshops, _barContainment, _barHangars;

	private readonly Text _txtDefense, _txtShortRange, _txtLongRange;
	private readonly Text _numDefense, _numShortRange, _numLongRange;
	private readonly Bar _barDefense, _barShortRange, _barLongRange;

	/// <summary>
	/// Initializes all the elements in the Base Info screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="state">Pointer to the Basescape state.</param>
	public BaseInfoState(Base @base, BasescapeState state)
	{
		_base = @base;
		_state = state;

		// Create objects
		_bg = new Surface(320, 200, 0, 0);
		_mini = new MiniBaseView(128, 16, 182, 8);
		_btnOk = new TextButton(30, 14, 10, 180);
		_btnTransfers = new TextButton(80, 14, 46, 180);
		_btnStores = new TextButton(80, 14, 132, 180);
		_btnMonthlyCosts = new TextButton(92, 14, 218, 180);
		_edtBase = new TextEdit(this, 127, 16, 8, 8);

		_txtPersonnel = new Text(300, 9, 8, 30);
		_txtSoldiers = new Text(114, 9, 8, 41);
		_numSoldiers = new Text(40, 9, 126, 41);
		_barSoldiers = new Bar(150, 5, 166, 43);
		_txtEngineers = new Text(114, 9, 8, 51);
		_numEngineers = new Text(40, 9, 126, 51);
		_barEngineers = new Bar(150, 5, 166, 53);
		_txtScientists = new Text(114, 9, 8, 61);
		_numScientists = new Text(40, 9, 126, 61);
		_barScientists = new Bar(150, 5, 166, 63);

		_txtSpace = new Text(300, 9, 8, 72);
		_txtQuarters = new Text(114, 9, 8, 83);
		_numQuarters = new Text(40, 9, 126, 83);
		_barQuarters = new Bar(150, 5, 166, 85);
		_txtStores = new Text(114, 9, 8, 93);
		_numStores = new Text(40, 9, 126, 93);
		_barStores = new Bar(150, 5, 166, 95);
		_txtLaboratories = new Text(114, 9, 8, 103);
		_numLaboratories = new Text(40, 9, 126, 103);
		_barLaboratories = new Bar(150, 5, 166, 105);
		_txtWorkshops = new Text(114, 9, 8, 113);
		_numWorkshops = new Text(40, 9, 126, 113);
		_barWorkshops = new Bar(150, 5, 166, 115);
		if (Options.storageLimitsEnforced)
		{
			_txtContainment = new Text(114, 9, 8, 123);
			_numContainment = new Text(40, 9, 126, 123);
			_barContainment = new Bar(150, 5, 166, 125);
		}
		_txtHangars = new Text(114, 9, 8, Options.storageLimitsEnforced ? 133 : 123);
		_numHangars = new Text(40, 9, 126, Options.storageLimitsEnforced ? 133 : 123);
		_barHangars = new Bar(150, 5, 166, Options.storageLimitsEnforced ? 135 : 125);

		_txtDefense = new Text(114, 9, 8, Options.storageLimitsEnforced ? 147 : 138);
		_numDefense = new Text(40, 9, 126, Options.storageLimitsEnforced ? 147 : 138);
		_barDefense = new Bar(150, 5, 166, Options.storageLimitsEnforced ? 149 : 140);
		_txtShortRange = new Text(114, 9, 8, Options.storageLimitsEnforced ? 157 : 153);
		_numShortRange = new Text(40, 9, 126, Options.storageLimitsEnforced ? 157 : 153);
		_barShortRange = new Bar(150, 5, 166, Options.storageLimitsEnforced ? 159 : 155);
		_txtLongRange = new Text(114, 9, 8, Options.storageLimitsEnforced ? 167 : 163);
		_numLongRange = new Text(40, 9, 126, Options.storageLimitsEnforced ? 167 : 163);
		_barLongRange = new Bar(150, 5, 166, Options.storageLimitsEnforced ? 169 : 165);

		// Set palette
		SetInterface("baseInfo");

		Add(_bg);
		Add(_mini, "miniBase", "basescape");
		Add(_btnOk, "button", "baseInfo");
		Add(_btnTransfers, "button", "baseInfo");
		Add(_btnStores, "button", "baseInfo");
		Add(_btnMonthlyCosts, "button", "baseInfo");
		Add(_edtBase, "text1", "baseInfo");

		Add(_txtPersonnel, "text1", "baseInfo");
		Add(_txtSoldiers, "text2", "baseInfo");
		Add(_numSoldiers, "numbers", "baseInfo");
		Add(_barSoldiers, "personnelBars", "baseInfo");
		Add(_txtEngineers, "text2", "baseInfo");
		Add(_numEngineers, "numbers", "baseInfo");
		Add(_barEngineers, "personnelBars", "baseInfo");
		Add(_txtScientists, "text2", "baseInfo");
		Add(_numScientists, "numbers", "baseInfo");
		Add(_barScientists, "personnelBars", "baseInfo");

		Add(_txtSpace, "text1", "baseInfo");
		Add(_txtQuarters, "text2", "baseInfo");
		Add(_numQuarters, "numbers", "baseInfo");
		Add(_barQuarters, "facilityBars", "baseInfo");
		Add(_txtStores, "text2", "baseInfo");
		Add(_numStores, "numbers", "baseInfo");
		Add(_barStores, "facilityBars", "baseInfo");
		Add(_txtLaboratories, "text2", "baseInfo");
		Add(_numLaboratories, "numbers", "baseInfo");
		Add(_barLaboratories, "facilityBars", "baseInfo");
		Add(_txtWorkshops, "text2", "baseInfo");
		Add(_numWorkshops, "numbers", "baseInfo");
		Add(_barWorkshops, "facilityBars", "baseInfo");
		if (Options.storageLimitsEnforced)
		{
			Add(_txtContainment, "text2", "baseInfo");
			Add(_numContainment, "numbers", "baseInfo");
			Add(_barContainment, "facilityBars", "baseInfo");
		}
		Add(_txtHangars, "text2", "baseInfo");
		Add(_numHangars, "numbers", "baseInfo");
		Add(_barHangars, "facilityBars", "baseInfo");

		Add(_txtDefense, "text2", "baseInfo");
		Add(_numDefense, "numbers", "baseInfo");
		Add(_barDefense, "defenceBar", "baseInfo");
		Add(_txtShortRange, "text2", "baseInfo");
		Add(_numShortRange, "numbers", "baseInfo");
		Add(_barShortRange, "detectionBars", "baseInfo");
		Add(_txtLongRange, "text2", "baseInfo");
		Add(_numLongRange, "numbers", "baseInfo");
		Add(_barLongRange, "detectionBars", "baseInfo");

		CenterAllSurfaces();

		// Set up objects
		string ss = "";
		if (Options.storageLimitsEnforced)
		{
			ss += "ALT";
		}
		ss += "BACK07.SCR";
		_game.Mod.GetSurface(ss).BlitNShade(_bg, 0, 0);

		_mini.SetTexture(_game.Mod.GetSurfaceSet("BASEBITS.PCK"));
		_mini.SetBases(_game.SavedGame.Bases);
		for (int i = 0; i < _game.SavedGame.Bases.Count; ++i)
		{
			if (_game.SavedGame.Bases[i] == _base)
			{
				_mini.SetSelectedBase(i);
				break;
			}
		}
		_mini.OnMouseClick(MiniClick);
		_mini.OnKeyboardPress(HandleKeyPress);

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_btnTransfers.Text = Tr("STR_TRANSFERS_UC");
		_btnTransfers.OnMouseClick(BtnTransfersClick);

		_btnStores.Text = Tr("STR_STORES_UC");
		_btnStores.OnMouseClick(BtnStoresClick);

		_btnMonthlyCosts.Text = Tr("STR_MONTHLY_COSTS");
		_btnMonthlyCosts.OnMouseClick(BtnMonthlyCostsClick);

		_edtBase.SetBig();
		_edtBase.OnChange(EdtBaseChange);

		_txtPersonnel.SetText(Tr("STR_PERSONNEL_AVAILABLE_PERSONNEL_TOTAL"));

		_txtSoldiers.SetText(Tr("STR_SOLDIERS"));

		_barSoldiers.Scale = 1.0;

		_txtEngineers.SetText(Tr("STR_ENGINEERS"));

		_barEngineers.Scale = 1.0;

		_txtScientists.SetText(Tr("STR_SCIENTISTS"));

		_barScientists.Scale = 1.0;


		_txtSpace.SetText(Tr("STR_SPACE_USED_SPACE_AVAILABLE"));

		_txtQuarters.SetText(Tr("STR_LIVING_QUARTERS_PLURAL"));

		_barQuarters.Scale = 0.5;

		_txtStores.SetText(Tr("STR_STORES"));

		_barStores.Scale = 0.5;

		_txtLaboratories.SetText(Tr("STR_LABORATORIES"));

		_barLaboratories.Scale = 0.5;

		_txtWorkshops.SetText(Tr("STR_WORK_SHOPS"));

		_barWorkshops.Scale = 0.5;

		if (Options.storageLimitsEnforced)
		{
			_txtContainment.SetText(Tr("STR_ALIEN_CONTAINMENT"));

			_barContainment.Scale = 0.5;
		}

		_txtHangars.SetText(Tr("STR_HANGARS"));

		_barHangars.Scale = 18.0;


		_txtDefense.SetText(Tr("STR_DEFENSE_STRENGTH"));

		_barDefense.Scale = 0.125;

		_txtShortRange.SetText(Tr("STR_SHORT_RANGE_DETECTION"));

		_barShortRange.Scale = 25.0;

		_txtLongRange.SetText(Tr("STR_LONG_RANGE_DETECTION"));

		_barLongRange.Scale = 25.0;
	}

	/// <summary>"used:total" as shown in the number columns.</summary>
	internal static string Ratio(int a, int b) => string.Create(CultureInfo.InvariantCulture, $"{a}:{b}");

	/// <summary>
	/// Sets a bar to value/max; with <see cref="Options.oxceBaseInfoScaleEnabled"/>, bars that would get wider
	/// than MAX_BAR_WIDTH are scaled down to it (the repeated block of C++ init()).
	/// </summary>
	/// <param name="resetScale">Also set the bar's scale to 1 when scaling down (facility bars).</param>
	internal static void SetBar(Bar bar, int value, int max, bool resetScale)
	{
		if (!Options.oxceBaseInfoScaleEnabled || max * bar.Scale < MAX_BAR_WIDTH)
		{
			bar.Max = max;
			bar.Value = value;
		}
		else
		{
			bar.Max = MAX_BAR_WIDTH;
			bar.Value = value * MAX_BAR_WIDTH / max;
			if (resetScale)
			{
				bar.Scale = 1.0;
			}
		}
	}

	/// <summary>Used stores as shown (C++ <c>(int)floor(getUsedStores() + 0.05)</c>).</summary>
	internal static int UsedStoresShown(Base @base) => (int)Math.Floor(@base.GetUsedStores() + 0.05);

	/// <summary>
	/// The player can change the selected base.
	/// </summary>
	public override void Init()
	{
		base.Init();
		_edtBase.Text = _base.GetName(null);

		_numSoldiers.SetText(Ratio(_base.GetAvailableSoldiers(), _base.TotalSoldiers));
		SetBar(_barSoldiers, _base.GetAvailableSoldiers(), _base.TotalSoldiers, false);

		_numEngineers.SetText(Ratio(_base.AvailableEngineers, _base.TotalEngineers));
		SetBar(_barEngineers, _base.AvailableEngineers, _base.TotalEngineers, false);

		_numScientists.SetText(Ratio(_base.AvailableScientists, _base.TotalScientists));
		SetBar(_barScientists, _base.AvailableScientists, _base.TotalScientists, false);


		_numQuarters.SetText(Ratio(_base.UsedQuarters, _base.AvailableQuarters));
		SetBar(_barQuarters, _base.UsedQuarters, _base.AvailableQuarters, true);

		_numStores.SetText(Ratio(UsedStoresShown(_base), _base.AvailableStores));
		SetBar(_barStores, UsedStoresShown(_base), _base.AvailableStores, true);

		_numLaboratories.SetText(Ratio(_base.UsedLaboratories, _base.AvailableLaboratories));
		SetBar(_barLaboratories, _base.UsedLaboratories, _base.AvailableLaboratories, true);

		_numWorkshops.SetText(Ratio(_base.UsedWorkshops, _base.AvailableWorkshops));
		SetBar(_barWorkshops, _base.UsedWorkshops, _base.AvailableWorkshops, true);

		if (Options.storageLimitsEnforced)
		{
			_numContainment.SetText(Ratio(_base.GetUsedContainment(0), _base.GetAvailableContainment(0)));

			_barContainment.Max = _base.GetAvailableContainment(0);
			_barContainment.Value = _base.GetUsedContainment(0);
		}

		_numHangars.SetText(Ratio(_base.UsedHangars, _base.AvailableHangars));

		_barHangars.Max = _base.AvailableHangars;
		_barHangars.Value = _base.UsedHangars;


		_numDefense.SetText(_base.DefenseValue.ToString(CultureInfo.InvariantCulture));

		_barDefense.Max = _base.DefenseValue;
		_barDefense.Value = _base.DefenseValue;
		if (Options.oxceBaseInfoDefenseScaleMultiplier != 100)
		{
			_barDefense.Scale = 0.125 * Options.oxceBaseInfoDefenseScaleMultiplier / 100.0;
		}

		int shortRangeDetection = _base.ShortRangeDetection;
		_numShortRange.SetText(shortRangeDetection.ToString(CultureInfo.InvariantCulture));

		_barShortRange.Max = shortRangeDetection;
		_barShortRange.Value = shortRangeDetection;

		int longRangeDetection = _base.LongRangeDetection;
		_numLongRange.SetText(longRangeDetection.ToString(CultureInfo.InvariantCulture));

		_barLongRange.Max = longRangeDetection;
		_barLongRange.Value = longRangeDetection;
	}

	/// <summary>Changes the base name.</summary>
	public void EdtBaseChange(Action action)
	{
		_base.SetName(_edtBase.Text);
	}

	/// <summary>Selects a new base to display.</summary>
	public void MiniClick(Action action)
	{
		int @base = _mini.HoveredBase;
		if (@base < _game.SavedGame.Bases.Count)
		{
			_mini.SetSelectedBase(@base);
			_base = _game.SavedGame.Bases[@base];
			_state.SetBase(_base);
			Init();
		}
	}

	/// <summary>Selects a new base to display.</summary>
	public void HandleKeyPress(Action action)
	{
		if (action.Details.type == SDLEventType.SDL_KEYDOWN)
		{
			SDLKey[] baseKeys = [
				Options.keyBaseSelect1,
				Options.keyBaseSelect2,
				Options.keyBaseSelect3,
				Options.keyBaseSelect4,
				Options.keyBaseSelect5,
				Options.keyBaseSelect6,
				Options.keyBaseSelect7,
				Options.keyBaseSelect8
			];
			SDLKey key = action.Details.key.keysym.sym;
			// C++ reads past the key array with more than 8 bases
			for (int i = 0; i < _game.SavedGame.Bases.Count && i < baseKeys.Length; ++i)
			{
				if (key == baseKeys[i])
				{
					_mini.SetSelectedBase(i);
					_base = _game.SavedGame.Bases[i];
					_state.SetBase(_base);
					Init();
					break;
				}
			}
		}
	}

	/// <summary>Returns to the previous screen.</summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>Goes to the Transfers window.</summary>
	public void BtnTransfersClick(Action action)
	{
		_game.PushState(new TransfersState(_base));
	}

	/// <summary>Goes to the Stores screen.</summary>
	public void BtnStoresClick(Action action)
	{
		_game.PushState(new StoresState(_base));
	}

	/// <summary>Goes to the Monthly Costs screen.</summary>
	public void BtnMonthlyCostsClick(Action action)
	{
		_game.PushState(new MonthlyCostsState(_base));
	}
}
