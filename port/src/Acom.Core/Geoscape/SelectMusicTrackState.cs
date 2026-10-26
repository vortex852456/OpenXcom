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
// Ported from: src/Geoscape/SelectMusicTrackState.h, src/Geoscape/SelectMusicTrackState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

public enum SelectMusicTrackOrigin : uint
{
	SMT_GEOSCAPE = 0,
	SMT_BATTLESCAPE = 1,
}

/// <summary>
/// Select Music Track window that allows changing
/// of the currently played music track.
/// </summary>
public class SelectMusicTrackState : State
{
	private readonly SelectMusicTrackOrigin _origin;
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly TextButton _btnCancel;
	private readonly TextList _lstTracks;
	private readonly List<Music> _tracks = [];
	private readonly List<string> _trackNames = [];

	/// <summary>
	/// Initializes all the elements in the Select Music Track window.
	/// </summary>
	/// <param name="origin">Game section that originated this state.</param>
	public SelectMusicTrackState(SelectMusicTrackOrigin origin)
	{
		_origin = origin;
		_screen = false;

		int x;
		if (_origin == SelectMusicTrackOrigin.SMT_BATTLESCAPE)
		{
			x = 52;
		}
		else
		{
			x = 20;
		}

		// Create objects
		_window = new Window(this, 216, 160, x, 20, WindowPopup.POPUP_BOTH);
		_txtTitle = new Text(206, 17, x + 5, 32);
		_btnCancel = new TextButton(140, 16, x + 38, 156);
		_lstTracks = new TextList(180, 96, x + 13, 52);

		// Set palette
		SetInterface("selectMusicTrack", false, _game.SavedGame != null ? _game.SavedGame.SavedBattle : null);

		Add(_window, "window", "selectMusicTrack");
		Add(_txtTitle, "text", "selectMusicTrack");
		Add(_btnCancel, "button", "selectMusicTrack");
		Add(_lstTracks, "list", "selectMusicTrack");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "selectMusicTrack");

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetBig();
		_txtTitle.SetText(Tr("STR_SELECT_MUSIC_TRACK"));

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_lstTracks.SetColumns(1, 172);
		_lstTracks.SetSelectable(true);
		_lstTracks.SetBackground(_window);
		_lstTracks.Margin = 8;
		_lstTracks.SetAlign(TextHAlign.ALIGN_CENTER);
		_lstTracks.OnMouseClick(LstTrackClick);

		string search = _origin == SelectMusicTrackOrigin.SMT_BATTLESCAPE ? "GMTAC" : "GMGEO";
		string currentName = _game.Mod.CurrentMusicTrack;
		int currentTrackIndex = -1;
		foreach (var pair in _game.Mod.MusicTrackList)
		{
			if (pair.Key == currentName || pair.Key.Contains(search, StringComparison.Ordinal))
			{
				_lstTracks.AddRow(1, Tr(pair.Key));
				_tracks.Add(pair.Value);
				_trackNames.Add(pair.Key);
				if (pair.Key == currentName)
				{
					currentTrackIndex = _lstTracks.LastRowIndex;
					_lstTracks.SetRowColor(currentTrackIndex, _lstTracks.SecondaryColor);
				}
			}
		}

		// switch to battlescape theme if called from battlestate
		if (_origin == SelectMusicTrackOrigin.SMT_BATTLESCAPE)
		{
			ApplyBattlescapeTheme("selectMusicTrack");
			if (currentTrackIndex > -1)
			{
				Element element = _game.Mod.GetInterface("battlescape").GetElement("optionLists");
				_lstTracks.SetRowColor(currentTrackIndex, unchecked((byte)element.color2));
			}
		}
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Starts playing the selected track and returns to the previous screen.
	/// </summary>
	public void LstTrackClick(Action action)
	{
		Music selected = _tracks[(int)_lstTracks.SelectedRow];
		selected.Play();
		_game.Mod.CurrentMusicTrack = _trackNames[(int)_lstTracks.SelectedRow];
		_game.PopState();
	}
}
