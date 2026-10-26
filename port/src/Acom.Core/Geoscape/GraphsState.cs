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
// Ported from: src/Geoscape/GraphsState.h, src/Geoscape/GraphsState.cpp
#nullable disable

using System.Globalization;
using System.Text;

namespace Acom;

#pragma warning disable CA1822 // UI handlers stay instance members like in C++ (State._game is static here)

/// <summary>A toggle of the graphs screen (a region or country, possibly scrolled out of the buttons).</summary>
internal sealed class GraphButInfo
{
	internal string _name;
	internal int _color;
	internal bool _pushed;

	internal GraphButInfo(string name, byte color)
	{
		_name = name;
		_color = color;
		_pushed = false;
	}
}

/// <summary>
/// Graphs screen for displaying graphs of various
/// monthly game data like activity and funding.
/// </summary>
public class GraphsState : State
{
	private readonly InteractiveSurface _bg;
	private readonly InteractiveSurface _btnGeoscape;
	private readonly InteractiveSurface _btnXcomCountry, _btnUfoCountry;
	private readonly InteractiveSurface _btnXcomRegion, _btnUfoRegion;
	private readonly InteractiveSurface _btnIncome, _btnFinance;
	private readonly Text _txtTitle, _txtFactor;
	private readonly TextList _txtMonths, _txtYears;
	private readonly List<Text> _txtScale = [];
	private readonly List<ToggleTextButton> _btnRegions = [], _btnCountries = [], _btnFinances = [];
	private readonly List<GraphButInfo> _regionToggles = [], _countryToggles = [];
	private readonly List<bool> _financeToggles = [];
	private readonly ToggleTextButton _btnRegionTotal, _btnCountryTotal;
	private readonly List<Surface> _alienRegionLines = [], _alienCountryLines = [];
	private readonly List<Surface> _xcomRegionLines = [], _xcomCountryLines = [];
	private readonly List<Surface> _financeLines = [], _incomeLines = [];
	private bool _alien, _income, _country, _finance;
	private const int GRAPH_MAX_BUTTONS = 16;
	//will be only between 0 and size()
	private int _butRegionsOffset, _butCountriesOffset;
	private int _zoom;

	/// <summary>
	/// Initializes all the elements in the Graphs screen.
	/// </summary>
	public GraphsState()
	{
		_butRegionsOffset = 0;
		_butCountriesOffset = 0;
		_zoom = 100;

		// Create object
		_bg = new InteractiveSurface(320, 200, 0, 0);
		_bg.OnMousePress(ShiftButtons, SDLMouse.SDL_BUTTON_WHEELUP);
		_bg.OnMousePress(ShiftButtons, SDLMouse.SDL_BUTTON_WHEELDOWN);
		_btnUfoRegion = new InteractiveSurface(32, 24, 96, 0);
		_btnUfoCountry = new InteractiveSurface(32, 24, 128, 0);
		_btnXcomRegion = new InteractiveSurface(32, 24, 160, 0);
		_btnXcomCountry = new InteractiveSurface(32, 24, 192, 0);
		_btnIncome = new InteractiveSurface(32, 24, 224, 0);
		_btnFinance = new InteractiveSurface(32, 24, 256, 0);
		_btnGeoscape = new InteractiveSurface(32, 24, 288, 0);
		_txtTitle = new Text(230, 16, 90, 28);
		_txtFactor = new Text(38, 11, 96, 28);
		_txtMonths = new TextList(205, 8, 115, 183);
		_txtYears = new TextList(200, 8, 121, 191);

		// Set palette
		SetInterface("graphs");

		//add all our element
		Add(_bg);
		Add(_btnUfoRegion);
		Add(_btnUfoCountry);
		Add(_btnXcomRegion);
		Add(_btnXcomCountry);
		Add(_btnIncome);
		Add(_btnFinance);
		Add(_btnGeoscape);
		Add(_txtMonths, "scale", "graphs");
		Add(_txtYears, "scale", "graphs");
		Add(_txtTitle, "text", "graphs");
		Add(_txtFactor, "text", "graphs");
		for (int scaleText = 0; scaleText != 10; ++scaleText)
		{
			_txtScale.Add(new Text(42, 16, 80, 171 - (scaleText * 14)));
			Add(_txtScale[scaleText], "scale", "graphs");
		}
		byte regionTotalColor = unchecked((byte)_game.Mod.GetInterface("graphs").GetElement("regionTotal").color);
		byte countryTotalColor = unchecked((byte)_game.Mod.GetInterface("graphs").GetElement("countryTotal").color);

		//create buttons (sooooo many buttons)
		int offset = 0;
		foreach (var region in _game.SavedGame.Regions)
		{
			// always save in toggles all the region
			byte color = unchecked((byte)(13 + 8 * (offset % GRAPH_MAX_BUTTONS)));
			_regionToggles.Add(new GraphButInfo(Tr(region.Rules.Type), color));
			// initially add the GRAPH_MAX_BUTTONS having the first regions information
			if (offset < GRAPH_MAX_BUTTONS)
			{
				_btnRegions.Add(new ToggleTextButton(88, 11, 0, offset * 11));
				_btnRegions[offset].Text = Tr(region.Rules.Type);
				_btnRegions[offset].SetInvertColor(color);
				_btnRegions[offset].OnMousePress(BtnRegionListClick);
				Add(_btnRegions[offset], "button", "graphs");
			}
			_alienRegionLines.Add(new Surface(320, 200, 0, 0));
			Add(_alienRegionLines[offset]);
			_xcomRegionLines.Add(new Surface(320, 200, 0, 0));
			Add(_xcomRegionLines[offset]);

			++offset;
		}

		if (_regionToggles.Count < GRAPH_MAX_BUTTONS)
			_btnRegionTotal = new ToggleTextButton(88, 11, 0, _regionToggles.Count * 11);
		else
			_btnRegionTotal = new ToggleTextButton(88, 11, 0, GRAPH_MAX_BUTTONS * 11);
		_regionToggles.Add(new GraphButInfo(Tr("STR_TOTAL_UC"), regionTotalColor));
		_btnRegionTotal.OnMousePress(BtnRegionListClick);
		_btnRegionTotal.SetInvertColor(regionTotalColor);
		_btnRegionTotal.Text = Tr("STR_TOTAL_UC");
		_alienRegionLines.Add(new Surface(320, 200, 0, 0));
		Add(_alienRegionLines[offset]);
		_xcomRegionLines.Add(new Surface(320, 200, 0, 0));
		Add(_xcomRegionLines[offset]);
		Add(_btnRegionTotal, "button", "graphs");

		offset = 0;
		foreach (var country in _game.SavedGame.Countries)
		{
			// always save in toggles all the countries
			byte color = unchecked((byte)(13 + 8 * (offset % GRAPH_MAX_BUTTONS)));
			_countryToggles.Add(new GraphButInfo(Tr(country.Rules.Type), color));
			// initially add the GRAPH_MAX_BUTTONS having the first countries information
			if (offset < GRAPH_MAX_BUTTONS)
			{
				_btnCountries.Add(new ToggleTextButton(88, 11, 0, offset * 11));
				_btnCountries[offset].SetInvertColor(color);
				_btnCountries[offset].Text = Tr(country.Rules.Type);
				_btnCountries[offset].OnMousePress(BtnCountryListClick);
				Add(_btnCountries[offset], "button", "graphs");
			}
			_alienCountryLines.Add(new Surface(320, 200, 0, 0));
			Add(_alienCountryLines[offset]);
			_xcomCountryLines.Add(new Surface(320, 200, 0, 0));
			Add(_xcomCountryLines[offset]);
			_incomeLines.Add(new Surface(320, 200, 0, 0));
			Add(_incomeLines[offset]);

			++offset;
		}

		if (_countryToggles.Count < GRAPH_MAX_BUTTONS)
			_btnCountryTotal = new ToggleTextButton(88, 11, 0, _countryToggles.Count * 11);
		else
			_btnCountryTotal = new ToggleTextButton(88, 11, 0, GRAPH_MAX_BUTTONS * 11);
		_countryToggles.Add(new GraphButInfo(Tr("STR_TOTAL_UC"), countryTotalColor));
		_btnCountryTotal.OnMousePress(BtnCountryListClick);
		_btnCountryTotal.SetInvertColor(countryTotalColor);
		_btnCountryTotal.Text = Tr("STR_TOTAL_UC");
		_alienCountryLines.Add(new Surface(320, 200, 0, 0));
		Add(_alienCountryLines[offset]);
		_xcomCountryLines.Add(new Surface(320, 200, 0, 0));
		Add(_xcomCountryLines[offset]);
		_incomeLines.Add(new Surface(320, 200, 0, 0));
		Add(_incomeLines[offset]);
		Add(_btnCountryTotal, "button", "graphs");

		for (int iter = 0; iter != 5; ++iter)
		{
			offset = iter;
			_btnFinances.Add(new ToggleTextButton(88, 11, 0, offset * 11));
			_financeToggles.Add(false);
			_btnFinances[offset].SetInvertColor(unchecked((byte)(13 + (8 * offset))));
			_btnFinances[offset].OnMousePress(BtnFinanceListClick);
			Add(_btnFinances[offset], "button", "graphs");
			_financeLines.Add(new Surface(320, 200, 0, 0));
			Add(_financeLines[offset]);
		}

		_btnFinances[0].Text = Tr("STR_INCOME");
		_btnFinances[1].Text = Tr("STR_EXPENDITURE");
		_btnFinances[2].Text = Tr("STR_MAINTENANCE");
		_btnFinances[3].Text = Tr("STR_BALANCE");
		_btnFinances[4].Text = Tr("STR_SCORE");

		// load back the button state
		string graphRegionToggles = _game.SavedGame.GraphRegionToggles ?? "";
		string graphCountryToggles = _game.SavedGame.GraphCountryToggles ?? "";
		string graphFinanceToggles = _game.SavedGame.GraphFinanceToggles ?? "";
		graphRegionToggles = graphRegionToggles.PadRight(_regionToggles.Count, '0');
		graphCountryToggles = graphCountryToggles.PadRight(_countryToggles.Count, '0');
		graphFinanceToggles = graphFinanceToggles.PadRight(_financeToggles.Count, '0');
		for (int i = 0; i < _regionToggles.Count; ++i)
		{
			_regionToggles[i]._pushed = '0' != graphRegionToggles[i];
			if (_regionToggles.Count - 1 == i)
				_btnRegionTotal.Pressed = _regionToggles[i]._pushed;
			else if (i < GRAPH_MAX_BUTTONS)
				_btnRegions[i].Pressed = _regionToggles[i]._pushed;
		}
		for (int i = 0; i < _countryToggles.Count; ++i)
		{
			_countryToggles[i]._pushed = '0' != graphCountryToggles[i];
			if (_countryToggles.Count - 1 == i)
				_btnCountryTotal.Pressed = _countryToggles[i]._pushed;
			else if (i < GRAPH_MAX_BUTTONS)
				_btnCountries[i].Pressed = _countryToggles[i]._pushed;
		}
		for (int i = 0; i < _financeToggles.Count; ++i)
		{
			_financeToggles[i] = '0' != graphFinanceToggles[i];
			_btnFinances[i].Pressed = _financeToggles[i];
		}
		byte gridColor = unchecked((byte)_game.Mod.GetInterface("graphs").GetElement("graph").color);
		// set up the grid
		_bg.DrawRect(125, 49, 188, 127, gridColor);

		for (int grid = 0; grid != 5; ++grid)
		{
			for (int y = 50 + grid; y <= 163 + grid; y += 14)
			{
				for (int x = 126 + grid; x <= 297 + grid; x += 17)
				{
					byte color = unchecked((byte)(gridColor + grid + 1));
					if (grid == 4)
					{
						color = 0;
					}
					_bg.DrawRect((short)x, (short)y, (short)(16 - (grid * 2)), (short)(13 - (grid * 2)), color);
				}
			}
		}

		//set up the horizontal measurement unit
		string[] months = ["STR_JAN", "STR_FEB", "STR_MAR", "STR_APR", "STR_MAY", "STR_JUN", "STR_JUL", "STR_AUG", "STR_SEP", "STR_OCT", "STR_NOV", "STR_DEC"];
		int month = _game.SavedGame.Time.Month;
		// i know using textlist for this is ugly and brutal, but YOU try getting this damn text to line up.
		// also, there's nothing wrong with being ugly or brutal, you should learn tolerance.
		_txtMonths.SetColumns(12, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17);
		_txtMonths.AddRow(12, " ", " ", " ", " ", " ", " ", " ", " ", " ", " ", " ", " ");
		_txtYears.SetColumns(6, 34, 34, 34, 34, 34, 34);
		_txtYears.AddRow(6, " ", " ", " ", " ", " ", " ");

		for (int iter = 0; iter != 12; ++iter)
		{
			if (month > 11)
			{
				month = 0;
				_txtYears.SetCellText(0, iter / 2, _game.SavedGame.Time.Year.ToString(CultureInfo.InvariantCulture));
				if (iter > 2)
				{
					_txtYears.SetCellText(0, 0, (_game.SavedGame.Time.Year - 1).ToString(CultureInfo.InvariantCulture));
				}
			}
			_txtMonths.SetCellText(0, iter, Tr(months[month]));
			++month;
		}

		// set up the vertical measurement unit
		foreach (var scaleText in _txtScale)
		{
			scaleText.Align = TextHAlign.ALIGN_RIGHT;
		}
		BtnUfoRegionClick(null);

		// Set up object
		if (_game.Mod.GetSurface("GRAPH.BDY", false) != null)
		{
			_game.Mod.GetSurface("GRAPH.BDY").BlitNShade(_bg, 0, 0);
		}
		else
		{
			_game.Mod.GetSurface("GRAPHS.SPK").BlitNShade(_bg, 0, 0);
		}

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;

		_txtFactor.SetText(Tr("STR_FINANCE_THOUSANDS"));

		// Set up buttons
		_btnUfoRegion.OnMousePress(BtnUfoRegionClick);
		_btnUfoCountry.OnMousePress(BtnUfoCountryClick);
		_btnXcomRegion.OnMousePress(BtnXcomRegionClick);
		_btnXcomCountry.OnMousePress(BtnXcomCountryClick);
		_btnIncome.OnMousePress(BtnIncomeClick);
		_btnFinance.OnMousePress(BtnFinanceClick);
		_btnGeoscape.OnMousePress(BtnGeoscapeClick);
		_btnGeoscape.OnKeyboardPress(BtnGeoscapeClick, Options.keyCancel);
		_btnGeoscape.OnKeyboardPress(BtnGeoscapeClick, Options.keyGeoGraphs);
		_btnGeoscape.OnKeyboardPress(BtnZoomInClick, Options.keyGraphsZoomIn);
		_btnGeoscape.OnKeyboardPress(BtnZoomOutClick, Options.keyGraphsZoomOut);

		CenterAllSurfaces();
	}

	/// <summary>
	/// Stores the button toggles in the saved game (the C++ destructor).
	/// </summary>
	public override void Destroy()
	{
		var graphRegionToggles = new StringBuilder();
		var graphCountryToggles = new StringBuilder();
		var graphFinanceToggles = new StringBuilder();
		for (int i = 0; i < _regionToggles.Count; ++i)
		{
			graphRegionToggles.Append(_regionToggles[i]._pushed ? '1' : '0');
		}
		for (int i = 0; i < _countryToggles.Count; ++i)
		{
			graphCountryToggles.Append(_countryToggles[i]._pushed ? '1' : '0');
		}
		for (int i = 0; i < _financeToggles.Count; ++i)
		{
			graphFinanceToggles.Append(_financeToggles[i] ? '1' : '0');
		}
		_game.SavedGame.GraphRegionToggles = graphRegionToggles.ToString();
		_game.SavedGame.GraphCountryToggles = graphCountryToggles.ToString();
		_game.SavedGame.GraphFinanceToggles = graphFinanceToggles.ToString();
		base.Destroy();
	}

	/// <summary>
	/// Zooms in.
	/// </summary>
	public void BtnZoomInClick(Action action)
	{
		_zoom = _zoom * 2 / 3;
		if (_zoom < 5) _zoom = 5;

		DrawLines();
	}

	/// <summary>
	/// Zooms out.
	/// </summary>
	public void BtnZoomOutClick(Action action)
	{
		_zoom = _zoom * 3 / 2;
		if (_zoom > 100) _zoom = 100;

		DrawLines();
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnGeoscapeClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Switches to the UFO Region Activity screen.
	/// </summary>
	public void BtnUfoRegionClick(Action action)
	{
		_alien = true;
		_income = false;
		_country = false;
		_finance = false;
		ResetScreen();
		DrawLines();
		foreach (var toggleButton in _btnRegions)
		{
			toggleButton.Visible = true;
		}
		_btnRegionTotal.Visible = true;
		_txtTitle.SetBig();
		_txtTitle.SetText(Tr("STR_UFO_ACTIVITY_IN_AREAS"));
	}

	/// <summary>
	/// Switches to the UFO Country activity screen.
	/// </summary>
	public void BtnUfoCountryClick(Action action)
	{
		_alien = true;
		_income = false;
		_country = true;
		_finance = false;
		ResetScreen();
		DrawLines();
		foreach (var toggleButton in _btnCountries)
		{
			toggleButton.Visible = true;
		}
		_btnCountryTotal.Visible = true;
		_txtTitle.SetBig();
		_txtTitle.SetText(Tr("STR_UFO_ACTIVITY_IN_COUNTRIES"));
	}

	/// <summary>
	/// Switches to the XCom Region activity screen.
	/// </summary>
	public void BtnXcomRegionClick(Action action)
	{
		_alien = false;
		_income = false;
		_country = false;
		_finance = false;
		ResetScreen();
		DrawLines();
		foreach (var toggleButton in _btnRegions)
		{
			toggleButton.Visible = true;
		}
		_btnRegionTotal.Visible = true;
		_txtTitle.SetBig();
		_txtTitle.SetText(Tr("STR_XCOM_ACTIVITY_IN_AREAS"));
	}

	/// <summary>
	/// Switches to the XCom Country activity screen.
	/// </summary>
	public void BtnXcomCountryClick(Action action)
	{
		_alien = false;
		_income = false;
		_country = true;
		_finance = false;
		ResetScreen();
		DrawLines();
		foreach (var toggleButton in _btnCountries)
		{
			toggleButton.Visible = true;
		}
		_btnCountryTotal.Visible = true;
		_txtTitle.SetBig();
		_txtTitle.SetText(Tr("STR_XCOM_ACTIVITY_IN_COUNTRIES"));
	}

	/// <summary>
	/// Switches to the Income screen.
	/// </summary>
	public void BtnIncomeClick(Action action)
	{
		_alien = false;
		_income = true;
		_country = true;
		_finance = false;
		ResetScreen();
		DrawLines();
		_txtFactor.Visible = true;
		foreach (var toggleButton in _btnCountries)
		{
			toggleButton.Visible = true;
		}
		_btnCountryTotal.Visible = true;
		_txtTitle.SetBig();
		_txtTitle.SetText(Tr("STR_INCOME"));
	}

	/// <summary>
	/// Switches to the Finances screen.
	/// </summary>
	public void BtnFinanceClick(Action action)
	{
		_alien = false;
		_income = false;
		_country = false;
		_finance = true;
		ResetScreen();
		DrawLines();

		foreach (var toggleButton in _btnFinances)
		{
			toggleButton.Visible = true;
		}
		_txtTitle.SetBig();
		_txtTitle.SetText(Tr("STR_FINANCE"));
	}

	/// <summary>
	/// Handles a click on a region button.
	/// </summary>
	public void BtnRegionListClick(Action action)
	{
		int number = 0;
		var button = action.Sender as ToggleTextButton;

		if (action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT)
		{
			for (int i = 0; i < _btnRegions.Count; ++i)
			{
				if (button != _btnRegions[i])
				{
					_btnRegions[i].Pressed = button.Pressed;
				}
			}
			_btnRegionTotal.Pressed = button.Pressed;
			foreach (var gbi in _regionToggles)
			{
				gbi._pushed = button.Pressed;
			}
		}
		else
		{
			if (button == _btnRegionTotal)
			{
				number = _regionToggles.Count - 1;
			}
			else
			{
				for (int i = 0; i < _btnRegions.Count; ++i)
				{
					if (button == _btnRegions[i])
					{
						number = i + _butRegionsOffset;
						break;
					}
				}
			}

			_regionToggles[number]._pushed = button.Pressed;
		}

		DrawLines();
	}

	/// <summary>
	/// Handles a click on a country button.
	/// </summary>
	public void BtnCountryListClick(Action action)
	{
		int number = 0;
		var button = action.Sender as ToggleTextButton;

		if (action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT)
		{
			for (int i = 0; i < _btnCountries.Count; ++i)
			{
				if (button != _btnCountries[i])
				{
					_btnCountries[i].Pressed = button.Pressed;
				}
			}
			_btnCountryTotal.Pressed = button.Pressed;
			foreach (var gbi in _countryToggles)
			{
				gbi._pushed = button.Pressed;
			}
		}
		else
		{
			if (button == _btnCountryTotal)
			{
				number = _countryToggles.Count - 1;
			}
			else
			{
				for (int i = 0; i < _btnCountries.Count; ++i)
				{
					if (button == _btnCountries[i])
					{
						number = i + _butCountriesOffset;
						break;
					}
				}
			}

			_countryToggles[number]._pushed = button.Pressed;
		}

		DrawLines();
	}

	/// <summary>
	/// handles a click on a finances button.
	/// </summary>
	public void BtnFinanceListClick(Action action)
	{
		int number = 0;
		var button = action.Sender as ToggleTextButton;

		for (int i = 0; i < _btnFinances.Count; ++i)
		{
			if (button == _btnFinances[i])
			{
				number = i;
				break;
			}
		}

		_financeLines[number].Visible = !_financeToggles[number];
		_financeToggles[number] = button.Pressed;

		DrawLines();
	}

	/// <summary>
	/// remove all elements from view
	/// </summary>
	public void ResetScreen()
	{
		foreach (var surface in _alienRegionLines)
		{
			surface.Visible = false;
		}
		foreach (var surface in _alienCountryLines)
		{
			surface.Visible = false;
		}
		foreach (var surface in _xcomRegionLines)
		{
			surface.Visible = false;
		}
		foreach (var surface in _xcomCountryLines)
		{
			surface.Visible = false;
		}
		foreach (var surface in _incomeLines)
		{
			surface.Visible = false;
		}
		foreach (var surface in _financeLines)
		{
			surface.Visible = false;
		}

		foreach (var ttb in _btnRegions)
		{
			ttb.Visible = false;
		}
		foreach (var ttb in _btnCountries)
		{
			ttb.Visible = false;
		}
		foreach (var ttb in _btnFinances)
		{
			ttb.Visible = false;
		}

		_btnRegionTotal.Visible = false;
		_btnCountryTotal.Visible = false;
		_txtFactor.Visible = false;
	}

	/// <summary>
	/// updates the text on the vertical scale
	/// </summary>
	/// <param name="lowerLimit">minimum value</param>
	/// <param name="upperLimit">maximum value</param>
	public void UpdateScale(double lowerLimit, double upperLimit)
	{
		double increment = ((upperLimit - lowerLimit) / 9);
		double text = lowerLimit;
		for (int i = 0; i < 10; ++i)
		{
			_txtScale[i].SetText(Unicode.FormatNumber((int)text));
			text += increment;
		}
	}

	/// <summary>
	/// instead of having all our line drawing in one giant ridiculous routine, just use the one we need.
	/// </summary>
	public void DrawLines()
	{
		if (!_country && !_finance)
		{
			DrawRegionLines();
		}
		else if (!_finance)
		{
			DrawCountryLines();
		}
		else
		{
			DrawFinanceLines();
		}
	}

	/// <summary>C++ drawLine with Sint16 coordinates.</summary>
	private static void DrawLine(Surface s, int x1, int y1, int x2, int y2, int color) =>
		s.DrawLine((short)x1, (short)y1, (short)x2, (short)y2, unchecked((byte)color));

	/// <summary>
	/// Rounds the graph limits to the grid: the C++ scale fitting shared by the three line drawers (before the zoom).
	/// </summary>
	internal static void FitScale(int check, ref int lowerLimit, ref int upperLimit)
	{
		double range = upperLimit - lowerLimit;
		double low = lowerLimit;
		int grids = 9; // cells in grid
		while (range > check * grids)
		{
			check *= 2;
		}

		lowerLimit = 0;
		upperLimit = check * grids;

		if (low < 0)
		{
			while (low < lowerLimit)
			{
				lowerLimit -= check;
				upperLimit -= check;
			}
		}
	}

	/// <summary>
	/// Sets up the screens and draws the lines for country button
	/// to toggle on and off
	/// </summary>
	public void DrawCountryLines()
	{
		var countries = _game.SavedGame.Countries;
		//calculate the totals, and set up our upward maximum
		int upperLimit = 0;
		int lowerLimit = 0;
		int[] totals = new int[12];
		for (int entry = 0; entry != _game.SavedGame.FundsList.Count; ++entry)
		{
			int total = 0;
			if (_alien)
			{
				for (int iter = 0; iter != countries.Count; ++iter)
				{
					total += countries[iter].ActivityAlien[entry];
					if (countries[iter].ActivityAlien[entry] > upperLimit && _countryToggles[iter]._pushed)
					{
						upperLimit = countries[iter].ActivityAlien[entry];
					}
				}
			}
			else if (_income)
			{
				for (int iter = 0; iter != countries.Count; ++iter)
				{
					total += countries[iter].Funding[entry] / 1000;
					if (countries[iter].Funding[entry] / 1000 > upperLimit && _countryToggles[iter]._pushed)
					{
						upperLimit = countries[iter].Funding[entry] / 1000;
					}
				}
			}
			else
			{
				for (int iter = 0; iter != countries.Count; ++iter)
				{
					total += countries[iter].ActivityXcom[entry];
					if (countries[iter].ActivityXcom[entry] > upperLimit && _countryToggles[iter]._pushed)
					{
						upperLimit = countries[iter].ActivityXcom[entry];
					}
					if (countries[iter].ActivityXcom[entry] < lowerLimit && _countryToggles[iter]._pushed)
					{
						lowerLimit = countries[iter].ActivityXcom[entry];
					}
				}
			}
			if (_countryToggles[^1]._pushed && total > upperLimit)
				upperLimit = total;
			if (_countryToggles[^1]._pushed && total < lowerLimit)
				lowerLimit = total;
		}

		//adjust the scale to fit the upward maximum
		FitScale(_income ? 50 : 10, ref lowerLimit, ref upperLimit);

		// custom zoom
		lowerLimit = (lowerLimit * _zoom) / 100;
		upperLimit = (upperLimit * _zoom) / 100;

		double range = upperLimit - lowerLimit;
		double units = range / 126;

		// draw country line
		for (int entry = 0; entry != countries.Count; ++entry)
		{
			Country country = countries[entry];
			_alienCountryLines[entry].Clear();
			_xcomCountryLines[entry].Clear();
			_incomeLines[entry].Clear();
			var newLineVector = new List<short>();
			int reduction = 0;
			for (int iter = 0; iter != 12; ++iter)
			{
				int x = 312 - (iter * 17);
				int y = (int)(175 - (-lowerLimit / units));
				if (_alien)
				{
					if (iter < country.ActivityAlien.Count)
					{
						reduction = (int)(country.ActivityAlien[country.ActivityAlien.Count - (1 + iter)] / units);
						y -= reduction;
						totals[iter] += country.ActivityAlien[country.ActivityAlien.Count - (1 + iter)];
					}
				}
				else if (_income)
				{
					if (iter < country.Funding.Count)
					{
						reduction = (int)((country.Funding[country.Funding.Count - (1 + iter)] / 1000) / units);
						y -= reduction;
						totals[iter] += country.Funding[country.Funding.Count - (1 + iter)] / 1000;
					}
				}
				else
				{
					if (iter < country.ActivityXcom.Count)
					{
						reduction = (int)(country.ActivityXcom[country.ActivityXcom.Count - (1 + iter)] / units);
						y -= reduction;
						totals[iter] += country.ActivityXcom[country.ActivityXcom.Count - (1 + iter)];
					}
				}

				if (y >= 180) y = 180;
				if (y <= 45) y = 45;
				newLineVector.Add((short)y);

				if (newLineVector.Count > 1 && _alien)
					DrawLine(_alienCountryLines[entry], x, y, x + 17, newLineVector[^2], _countryToggles[entry]._color + 4);
				else if (newLineVector.Count > 1 && _income)
					DrawLine(_incomeLines[entry], x, y, x + 17, newLineVector[^2], _countryToggles[entry]._color + 4);
				else if (newLineVector.Count > 1)
					DrawLine(_xcomCountryLines[entry], x, y, x + 17, newLineVector[^2], _countryToggles[entry]._color + 4);
			}
			if (_alien)
				_alienCountryLines[entry].Visible = _countryToggles[entry]._pushed;
			else if (_income)
				_incomeLines[entry].Visible = _countryToggles[entry]._pushed;
			else
				_xcomCountryLines[entry].Visible = _countryToggles[entry]._pushed;
		}
		if (_alien)
			_alienCountryLines[^1].Clear();
		else if (_income)
			_incomeLines[^1].Clear();
		else
			_xcomCountryLines[^1].Clear();

		// set up the "total" line
		{
			var newLineVector = new List<short>();
			byte color = unchecked((byte)_game.Mod.GetInterface("graphs").GetElement("countryTotal").color2);
			for (int iter = 0; iter != 12; ++iter)
			{
				int x = 312 - (iter * 17);
				int y = (int)(175 - (-lowerLimit / units));
				//if (totals[iter] > 0)
				{
					int reduction = (int)(totals[iter] / units);
					y -= reduction;
				}

				if (y >= 180) y = 180;
				if (y <= 45) y = 45;
				newLineVector.Add((short)y);

				if (newLineVector.Count > 1)
				{
					if (_alien)
						DrawLine(_alienCountryLines[^1], x, y, x + 17, newLineVector[^2], color);
					else if (_income)
						DrawLine(_incomeLines[^1], x, y, x + 17, newLineVector[^2], color);
					else
						DrawLine(_xcomCountryLines[^1], x, y, x + 17, newLineVector[^2], color);
				}
			}
		}
		if (_alien)
			_alienCountryLines[^1].Visible = _countryToggles[^1]._pushed;
		else if (_income)
			_incomeLines[^1].Visible = _countryToggles[^1]._pushed;
		else
			_xcomCountryLines[^1].Visible = _countryToggles[^1]._pushed;
		UpdateScale(lowerLimit, upperLimit);
		_txtFactor.Visible = _income;
	}

	/// <summary>
	/// Sets up the screens and draws the lines for region button
	/// to toggle on and off
	/// </summary>
	public void DrawRegionLines()
	{
		var regions = _game.SavedGame.Regions;
		//calculate the totals, and set up our upward maximum
		int upperLimit = 0;
		int lowerLimit = 0;
		int[] totals = new int[12];
		for (int entry = 0; entry != _game.SavedGame.FundsList.Count; ++entry)
		{
			int total = 0;
			if (_alien)
			{
				for (int iter = 0; iter != regions.Count; ++iter)
				{
					total += regions[iter].ActivityAlien[entry];
					if (regions[iter].ActivityAlien[entry] > upperLimit && _regionToggles[iter]._pushed)
					{
						upperLimit = regions[iter].ActivityAlien[entry];
					}
					if (regions[iter].ActivityAlien[entry] < lowerLimit && _regionToggles[iter]._pushed)
					{
						lowerLimit = regions[iter].ActivityAlien[entry];
					}
				}
			}
			else
			{
				for (int iter = 0; iter != regions.Count; ++iter)
				{
					total += regions[iter].ActivityXcom[entry];
					if (regions[iter].ActivityXcom[entry] > upperLimit && _regionToggles[iter]._pushed)
					{
						upperLimit = regions[iter].ActivityXcom[entry];
					}
					if (regions[iter].ActivityXcom[entry] < lowerLimit && _regionToggles[iter]._pushed)
					{
						lowerLimit = regions[iter].ActivityXcom[entry];
					}
				}
			}
			if (_regionToggles[^1]._pushed && total > upperLimit)
				upperLimit = total;
			if (_regionToggles[^1]._pushed && total < lowerLimit)
				lowerLimit = total;
		}

		//adjust the scale to fit the upward maximum
		FitScale(10, ref lowerLimit, ref upperLimit);

		// custom zoom
		lowerLimit = (lowerLimit * _zoom) / 100;
		upperLimit = (upperLimit * _zoom) / 100;

		double range = upperLimit - lowerLimit;
		double units = range / 126;

		// draw region lines
		for (int entry = 0; entry != regions.Count; ++entry)
		{
			Region region = regions[entry];
			_alienRegionLines[entry].Clear();
			_xcomRegionLines[entry].Clear();
			var newLineVector = new List<short>();
			int reduction = 0;
			for (int iter = 0; iter != 12; ++iter)
			{
				int x = 312 - (iter * 17);
				int y = (int)(175 - (-lowerLimit / units));
				if (_alien)
				{
					if (iter < region.ActivityAlien.Count)
					{
						reduction = (int)(region.ActivityAlien[region.ActivityAlien.Count - (1 + iter)] / units);
						y -= reduction;
						totals[iter] += region.ActivityAlien[region.ActivityAlien.Count - (1 + iter)];
					}
				}
				else
				{
					if (iter < region.ActivityXcom.Count)
					{
						reduction = (int)(region.ActivityXcom[region.ActivityXcom.Count - (1 + iter)] / units);
						y -= reduction;
						totals[iter] += region.ActivityXcom[region.ActivityXcom.Count - (1 + iter)];
					}
				}

				if (y >= 180) y = 180;
				if (y <= 45) y = 45;
				newLineVector.Add((short)y);

				if (newLineVector.Count > 1 && _alien)
					DrawLine(_alienRegionLines[entry], x, y, x + 17, newLineVector[^2], _regionToggles[entry]._color + 4);
				else if (newLineVector.Count > 1)
					DrawLine(_xcomRegionLines[entry], x, y, x + 17, newLineVector[^2], _regionToggles[entry]._color + 4);
			}

			if (_alien)
				_alienRegionLines[entry].Visible = _regionToggles[entry]._pushed;
			else
				_xcomRegionLines[entry].Visible = _regionToggles[entry]._pushed;
		}

		// set up the "total" line
		if (_alien)
			_alienRegionLines[^1].Clear();
		else
			_xcomRegionLines[^1].Clear();

		{
			byte color = unchecked((byte)_game.Mod.GetInterface("graphs").GetElement("regionTotal").color2);
			var newLineVector = new List<short>();
			for (int iter = 0; iter != 12; ++iter)
			{
				int x = 312 - (iter * 17);
				int y = (int)(175 - (-lowerLimit / units));
				//if (totals[iter] > 0)
				{
					int reduction = (int)(totals[iter] / units);
					y -= reduction;
				}

				if (y >= 180) y = 180;
				if (y <= 45) y = 45;
				newLineVector.Add((short)y);

				if (newLineVector.Count > 1)
				{
					if (_alien)
						DrawLine(_alienRegionLines[^1], x, y, x + 17, newLineVector[^2], color);
					else
						DrawLine(_xcomRegionLines[^1], x, y, x + 17, newLineVector[^2], color);
				}
			}
		}
		if (_alien)
			_alienRegionLines[^1].Visible = _regionToggles[^1]._pushed;
		else
			_xcomRegionLines[^1].Visible = _regionToggles[^1]._pushed;
		UpdateScale(lowerLimit, upperLimit);
		_txtFactor.Visible = false;
	}

	/// <summary>
	/// Sets up the screens and draws the lines for the finance button
	/// to toggle on and off
	/// </summary>
	public void DrawFinanceLines()
	{
		var save = _game.SavedGame;
		//set up array
		int upperLimit = 0;
		int lowerLimit = 0;
		long[] incomeTotals = new long[12];
		long[] balanceTotals = new long[12];
		long[] expendTotals = new long[12];
		long[] maintTotals = new long[12];
		int[] scoreTotals = new int[12];
		maintTotals[0] = save.BaseMaintenance / 1000;

		// start filling those arrays with score value
		// determine which is the highest one being displayed, so we can adjust the scale
		for (int entry = 0; entry != save.FundsList.Count; ++entry)
		{
			int invertedEntry = save.FundsList.Count - (1 + entry);
			maintTotals[entry] += save.Maintenances[invertedEntry] / 1000;
			balanceTotals[entry] = save.FundsList[invertedEntry] / 1000;
			scoreTotals[entry] = save.ResearchScores[invertedEntry];

			foreach (var region in save.Regions)
			{
				scoreTotals[entry] += region.ActivityXcom[invertedEntry] - region.ActivityAlien[invertedEntry];
			}

			if (_financeToggles[2])
			{
				if (maintTotals[entry] > upperLimit)
				{
					upperLimit = unchecked((int)maintTotals[entry]);
				}
				if (maintTotals[entry] < lowerLimit)
				{
					lowerLimit = unchecked((int)maintTotals[entry]);
				}
			}
			if (_financeToggles[3])
			{
				if (balanceTotals[entry] > upperLimit)
				{
					upperLimit = unchecked((int)balanceTotals[entry]);
				}
				if (balanceTotals[entry] < lowerLimit)
				{
					lowerLimit = unchecked((int)balanceTotals[entry]);
				}
			}
			if (_financeToggles[4])
			{
				if (scoreTotals[entry] > upperLimit)
				{
					upperLimit = scoreTotals[entry];
				}
				if (scoreTotals[entry] < lowerLimit)
				{
					lowerLimit = scoreTotals[entry];
				}
			}
		}

		for (int entry = 0; entry != save.Expenditures.Count; ++entry)
		{
			expendTotals[entry] = save.Expenditures[save.Expenditures.Count - (entry + 1)] / 1000;
			incomeTotals[entry] = save.Incomes[save.Incomes.Count - (entry + 1)] / 1000;

			if (_financeToggles[0] && incomeTotals[entry] > upperLimit)
			{
				upperLimit = unchecked((int)incomeTotals[entry]);
			}
			if (_financeToggles[1] && expendTotals[entry] > upperLimit)
			{
				upperLimit = unchecked((int)expendTotals[entry]);
			}
		}

		FitScale(250, ref lowerLimit, ref upperLimit);

		//toggle screen
		for (int button = 0; button != 5; ++button)
		{
			_financeLines[button].Visible = _financeToggles[button];
			_financeLines[button].Clear();
		}
		double range = upperLimit - lowerLimit;
		//figure out how many units to the pixel, then plot the points for the graph and connect the dots.
		double units = range / 126;
		for (int button = 0; button != 5; ++button)
		{
			var newLineVector = new List<short>();
			for (int iter = 0; iter != 12; ++iter)
			{
				int x = 312 - (iter * 17);
				int y = (int)(175 - (-lowerLimit / units));
				int reduction = 0;
				switch (button)
				{
					case 0:
						reduction = (int)(incomeTotals[iter] / units);
						break;
					case 1:
						reduction = (int)(expendTotals[iter] / units);
						break;
					case 2:
						reduction = (int)(maintTotals[iter] / units);
						break;
					case 3:
						reduction = (int)(balanceTotals[iter] / units);
						break;
					case 4:
						reduction = (int)(scoreTotals[iter] / units);
						break;
				}
				y -= reduction;
				newLineVector.Add((short)y);
				int offset = button % 2 != 0 ? 8 : 0;
				if (newLineVector.Count > 1)
					DrawLine(_financeLines[button], x, y, x + 17, newLineVector[^2], Acom.Palette.BlockOffset((byte)((button / 2) + 1)) + offset);
			}
		}
		UpdateScale(lowerLimit, upperLimit);
		_txtFactor.Visible = true;
	}

	/// <summary>
	/// 'Shift' the buttons to display only GRAPH_MAX_BUTTONS - reset their state from toggle
	/// </summary>
	public void ShiftButtons(Action action)
	{
		// only if active 'screen' is other than finance
		if (_finance)
			return;
		// select the data's we'll processing - regions or countries
		if (_country)
		{
			// too few countries? - return
			if (_countryToggles.Count <= GRAPH_MAX_BUTTONS)
				return;
			else if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELUP)
				ScrollButtons(_countryToggles, _btnCountries, ref _butCountriesOffset, -1);
			else if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELDOWN)
				ScrollButtons(_countryToggles, _btnCountries, ref _butCountriesOffset, 1);
		}
		else
		{
			// too few regions? - return
			if (_regionToggles.Count <= GRAPH_MAX_BUTTONS)
				return;
			else if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELUP)
				ScrollButtons(_regionToggles, _btnRegions, ref _butRegionsOffset, -1);
			else if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELDOWN)
				ScrollButtons(_regionToggles, _btnRegions, ref _butRegionsOffset, 1);
		}
	}

	/// <summary>Scrolls a button list.</summary>
	private static void ScrollButtons(List<GraphButInfo> toggles, List<ToggleTextButton> buttons, ref int offset, int step)
	{
		if (step + offset < 0 || offset + step + GRAPH_MAX_BUTTONS >= toggles.Count)
			return;
		// set the next offset - cheaper to do it from starter
		offset += step;
		int i = 0;
		for (int itert = offset; itert != toggles.Count && i < GRAPH_MAX_BUTTONS; ++itert, ++i)
		{
			UpdateButton(toggles[itert], buttons[i]);
		}
	}

	/// <summary>Repaints a button from its toggle.</summary>
	private static void UpdateButton(GraphButInfo from, ToggleTextButton to)
	{
		to.Text = from._name;
		to.SetInvertColor(unchecked((byte)from._color));
		to.Pressed = from._pushed;
	}
}
