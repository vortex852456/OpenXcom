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
// Ported from: src/Geoscape/UfoTrackerState.h, src/Geoscape/UfoTrackerState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Shows all the detected UFOs, mission sites and discovered alien bases.
/// </summary>
public class UfoTrackerState : State
{
	private readonly TextButton _btnCancel;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtObject, _txtSize, _txtAltitude, _txtHeading, _txtSpeed;
	private readonly TextList _lstObjects;
	private readonly GeoscapeState _state;
	private readonly Globe _globe;
	private readonly List<Target> _objects = [];

	/// <summary>
	/// Initializes all the elements in the UfoTracker window.
	/// </summary>
	/// <param name="state">Pointer to the Geoscape state.</param>
	/// <param name="globe">Pointer to the Geoscape globe.</param>
	public UfoTrackerState(GeoscapeState state, Globe globe)
	{
		_state = state;
		_globe = globe;

		const int WIDTH_OBJECT = 88;
		const int WIDTH_SIZE = 60;
		const int WIDTH_ALTITUDE = 54;
		const int WIDTH_HEADING = 54;
		const int WIDTH_SPEED = 32;

		_screen = false;

		int extraRows = Math.Clamp(Options.oxceInterceptTableSize, 8, 80) - 8;
		int extraHeight = 8 * extraRows;
		int offset = extraHeight / 2;

		// Create objects
		_window = new Window(this, 320, 140 + extraHeight, 0, 30 - offset, WindowPopup.POPUP_HORIZONTAL);
		_btnCancel = new TextButton(288, 16, 16, 146 + offset);
		_txtTitle = new Text(300, 17, 10, 46 - offset);
		int x = 14;
		_txtObject = new Text(WIDTH_OBJECT, 9, x, 70 - offset);
		x += WIDTH_OBJECT;
		_txtSize = new Text(WIDTH_SIZE, 9, x, 70 - offset);
		x += WIDTH_SIZE;
		_txtAltitude = new Text(WIDTH_ALTITUDE, 9, x, 70 - offset);
		x += WIDTH_ALTITUDE;
		_txtHeading = new Text(WIDTH_HEADING, 9, x, 70 - offset);
		x += WIDTH_HEADING;
		_txtSpeed = new Text(WIDTH_SPEED + 16, 17, x - 16, 70 - offset); // 16 pixels overlap for translators
		_lstObjects = new TextList(290, 64 + extraHeight, 12, 78 - offset);

		// Set palette
		SetInterface("ufoTracker");

		Add(_window, "window", "ufoTracker");
		Add(_btnCancel, "button", "ufoTracker");
		Add(_txtTitle, "text1", "ufoTracker");
		Add(_txtObject, "text2", "ufoTracker");
		Add(_txtSize, "text2", "ufoTracker");
		Add(_txtAltitude, "text2", "ufoTracker");
		Add(_txtHeading, "text2", "ufoTracker");
		Add(_txtSpeed, "text2", "ufoTracker");
		Add(_lstObjects, "list", "ufoTracker");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "ufoTracker");

		_btnCancel.Text = Tr("STR_CANCEL");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyGeoUfoTracker);

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetBig();
		_txtTitle.SetText(Tr("STR_UFO_TRACKER"));

		_txtObject.SetText(Tr("STR_NAME_UC"));

		_txtSize.SetText(Tr("STR_SIZE_UC"));

		_txtAltitude.SetText(Tr("STR_ALTITUDE"));

		_txtHeading.SetText(Tr("STR_HEADING"));

		_txtSpeed.Align = TextHAlign.ALIGN_RIGHT;
		_txtSpeed.SetText(Tr("STR_SPEED"));

		_lstObjects.SetColumns(5, WIDTH_OBJECT, WIDTH_SIZE, WIDTH_ALTITUDE, WIDTH_HEADING, WIDTH_SPEED);
		_lstObjects.SetAlign(TextHAlign.ALIGN_RIGHT, 4);
		_lstObjects.SetSelectable(true);
		_lstObjects.SetBackground(_window);
		_lstObjects.Margin = 2;
		_lstObjects.OnMouseClick(LstObjectsLeftClick);
		_lstObjects.OnMouseClick(LstObjectsRightClick, SDLMouse.SDL_BUTTON_RIGHT);
		_lstObjects.OnMouseClick(LstObjectsMiddleClick, SDLMouse.SDL_BUTTON_MIDDLE);

		int row = 0;
		foreach (var site in _game.SavedGame.MissionSites)
		{
			if (!site.Detected)
				continue;

			_objects.Add(site);
			_lstObjects.AddRow(1, site.GetName(_game.Language));
			_lstObjects.SetCellColor(row, 0, _lstObjects.SecondaryColor);
			row++;
		}
		foreach (var ufo in _game.SavedGame.Ufos)
		{
			if (!ufo.Detected || ufo.Status == Ufo.UfoStatus.IGNORE_ME)
				continue;

			string ss1 = Tr(ufo.Rules.Size);
			string altitude = ufo.Altitude == "STR_GROUND" ? "STR_GROUNDED" : ufo.Altitude;
			string ss2 = Tr(altitude);
			string heading = ufo.Status != Ufo.UfoStatus.FLYING ? "STR_NONE_UC" : ufo.Direction;
			string ss3 = Tr(heading);
			string ss4 = Unicode.FormatNumber(ufo.Speed);

			_objects.Add(ufo);
			_lstObjects.AddRow(5, ufo.GetName(_game.Language), ss1, ss2, ss3, ss4);
			if (altitude == "STR_GROUNDED")
			{
				_lstObjects.SetCellColor(row, 2, _lstObjects.SecondaryColor);
			}
			row++;
		}
		foreach (var ab in _game.SavedGame.AlienBases)
		{
			if (!ab.IsDiscovered)
				continue;

			_objects.Add(ab);
			_lstObjects.AddRow(1, ab.GetName(_game.Language));
			row++;
		}
	}

	/// <summary>
	/// Closes the window.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Displays the right popup for a specific target.
	/// </summary>
	/// <param name="target">Pointer to target.</param>
	public void PopupTarget(Target target)
	{
		_game.PopState();
		if (target is Ufo u)
		{
			_game.PushState(new UfoDetectedState(u, _state, false, u.HyperDetected));
		}
		else
		{
			_game.PushState(new TargetInfoState(target, _globe));
		}
	}

	/// <summary>
	/// Displays object detail.
	/// </summary>
	public void LstObjectsLeftClick(Action action)
	{
		Target t = _objects[(int)_lstObjects.SelectedRow];
		if (t != null)
		{
			PopupTarget(t);
		}
	}

	/// <summary>
	/// Centers on the selected object.
	/// </summary>
	public void LstObjectsRightClick(Action action)
	{
		Target t = _objects[(int)_lstObjects.SelectedRow];
		if (t != null)
		{
			_globe.Center(t.Longitude, t.Latitude);
			_game.PopState();
		}
	}

	/// <summary>
	/// Opens the corresponding Ufopaedia article.
	/// </summary>
	public void LstObjectsMiddleClick(Action action)
	{
		Target t = _objects[(int)_lstObjects.SelectedRow];
		if (t != null)
		{
			if (t is Ufo u && u.HyperDetected)
			{
				string articleId = u.Rules.Type;
				Ufopaedia.OpenArticle(_game, articleId);
			}
		}
	}
}
