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
// Ported from: src/Menu/OptionsAudioState.h, src/Menu/OptionsAudioState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>Screen that lets the user configure various Audio options.</summary>
public class OptionsAudioState : OptionsBaseState
{
	/* MUS_NONE, MUS_CMD, MUS_WAV, MUS_MOD, MUS_MID, MUS_OGG, MUS_MP3, MUS_MP3_MAD, MUS_FLAC, MUS_MODPLUG */
	private static readonly string[] musFormats = ["Adlib", "?", "WAV", "MOD", "MIDI", "OGG", "MP3", "MP3", "FLAC", "MOD"];
	private static readonly string[] sndFormats = ["?", "1.4", "1.0"];

	private readonly Text _txtMusicVolume, _txtSoundVolume, _txtUiVolume;
	private readonly Slider _slrMusicVolume, _slrSoundVolume, _slrUiVolume;
	private readonly Text _txtMusicFormat, _txtCurrentMusic, _txtSoundFormat, _txtCurrentSound, _txtVideoFormat;
	private readonly ComboBox _cbxMusicFormat, _cbxSoundFormat, _cbxVideoFormat;
	private readonly Text _txtOptions;
	private readonly ToggleTextButton _btnBackgroundMute;

	/// <summary>Initializes all the elements in the Audio Options screen.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	public OptionsAudioState(OptionsOrigin origin) : base(origin)
	{
		SetCategory(_btnAudio);

		// Create object
		_txtMusicVolume = new Text(114, 9, 94, 8);
		_slrMusicVolume = new Slider(104, 16, 94, 18);

		_txtSoundVolume = new Text(114, 9, 94, 40);
		_slrSoundVolume = new Slider(104, 16, 94, 50);

		_txtUiVolume = new Text(114, 9, 94, 72);
		_slrUiVolume = new Slider(104, 16, 94, 82);

		_txtMusicFormat = new Text(114, 9, 206, 40);
		_cbxMusicFormat = new ComboBox(this, 104, 16, 206, 50);
		_txtCurrentMusic = new Text(114, 9, 206, 68);

		_txtSoundFormat = new Text(114, 9, 206, 82);
		_cbxSoundFormat = new ComboBox(this, 104, 16, 206, 92);
		_txtCurrentSound = new Text(114, 9, 206, 110);

		_txtVideoFormat = new Text(114, 9, 206, 8);
		_cbxVideoFormat = new ComboBox(this, 104, 16, 206, 18);

		_txtOptions = new Text(114, 9, 94, 104);
		_btnBackgroundMute = new ToggleTextButton(104, 16, 94, 114);

		Add(_txtMusicVolume, "text", "audioMenu");
		Add(_slrMusicVolume, "button", "audioMenu");

		Add(_txtSoundVolume, "text", "audioMenu");
		Add(_slrSoundVolume, "button", "audioMenu");

		Add(_txtUiVolume, "text", "audioMenu");
		Add(_slrUiVolume, "button", "audioMenu");

		Add(_txtVideoFormat, "text", "audioMenu");
		Add(_txtMusicFormat, "text", "audioMenu");
		Add(_txtCurrentMusic, "text", "audioMenu");
		Add(_txtSoundFormat, "text", "audioMenu");
		Add(_txtCurrentSound, "text", "audioMenu");

		Add(_cbxSoundFormat, "button", "audioMenu");
		Add(_cbxMusicFormat, "button", "audioMenu");
		Add(_cbxVideoFormat, "button", "audioMenu");

		Add(_txtOptions, "text", "audioMenu");
		Add(_btnBackgroundMute, "button", "audioMenu");

		CenterAllSurfaces();

		// Set up object
		_txtMusicVolume.SetText(Tr("STR_MUSIC_VOLUME"));

		_slrMusicVolume.SetRange(0, Mixer.SDL_MIX_MAXVOLUME);
		_slrMusicVolume.Value = Options.musicVolume;
		_slrMusicVolume.OnChange(SlrMusicVolumeChange);
		_slrMusicVolume.Tooltip = "STR_MUSIC_VOLUME_DESC";
		_slrMusicVolume.OnMouseIn(TxtTooltipIn);
		_slrMusicVolume.OnMouseOut(TxtTooltipOut);

		_txtSoundVolume.SetText(Tr("STR_SFX_VOLUME"));

		_slrSoundVolume.SetRange(0, Mixer.SDL_MIX_MAXVOLUME);
		_slrSoundVolume.Value = Options.soundVolume;
		_slrSoundVolume.OnChange(SlrSoundVolumeChange);
		_slrSoundVolume.OnMouseRelease(SlrSoundVolumeRelease);
		_slrSoundVolume.Tooltip = "STR_SFX_VOLUME_DESC";
		_slrSoundVolume.OnMouseIn(TxtTooltipIn);
		_slrSoundVolume.OnMouseOut(TxtTooltipOut);

		_txtUiVolume.SetText(Tr("STR_UI_VOLUME"));

		_slrUiVolume.SetRange(0, Mixer.SDL_MIX_MAXVOLUME);
		_slrUiVolume.Value = Options.uiVolume;
		_slrUiVolume.OnChange(SlrUiVolumeChange);
		_slrUiVolume.OnMouseRelease(SlrUiVolumeRelease);
		_slrUiVolume.Tooltip = "STR_UI_VOLUME_DESC";
		_slrUiVolume.OnMouseIn(TxtTooltipIn);
		_slrUiVolume.OnMouseOut(TxtTooltipOut);

		/* MUSIC_AUTO, MUSIC_FLAC, MUSIC_OGG, MUSIC_MP3, MUSIC_MOD, MUSIC_WAV, MUSIC_ADLIB, MUSIC_GM, MUSIC_MIDI */
		var musicText = new List<string> { Tr("STR_PREFERRED_FORMAT_AUTO"), "FLAC", "OGG", "MP3", "MOD", "WAV", "Adlib", "GM", "MIDI" };
		var soundText = new List<string> { Tr("STR_PREFERRED_FORMAT_AUTO"), "1.4", "1.0" };
		var videoText = new List<string> { Tr("STR_PREFERRED_VIDEO_ANIMATION"), Tr("STR_PREFERRED_VIDEO_SLIDESHOW") };

		_txtMusicFormat.SetText(Tr("STR_PREFERRED_MUSIC_FORMAT"));

		_cbxMusicFormat.SetOptions(musicText);
		_cbxMusicFormat.Selected = (int)Options.preferredMusic;
		_cbxMusicFormat.Tooltip = "STR_PREFERRED_MUSIC_FORMAT_DESC";
		_cbxMusicFormat.OnChange(CbxMusicFormatChange);
		_cbxMusicFormat.OnMouseIn(TxtTooltipIn);
		_cbxMusicFormat.OnMouseOut(TxtTooltipOut);

		string curMusic = musFormats[(int)Mixer.GetMusicType(null)]; // this is confusing
		// since  both GM (GM.CAT) and MIDI (*.mid) are the same from SDL_mixer POV.
		_txtCurrentMusic.SetText(Tr("STR_CURRENT_FORMAT").Arg(curMusic));

		_txtSoundFormat.SetText(Tr("STR_PREFERRED_SFX_FORMAT"));

		_cbxSoundFormat.SetOptions(soundText);
		_cbxSoundFormat.Selected = (int)Options.preferredSound;
		_cbxSoundFormat.Tooltip = "STR_PREFERRED_SFX_FORMAT_DESC";
		_cbxSoundFormat.OnChange(CbxSoundFormatChange);
		_cbxSoundFormat.OnMouseIn(TxtTooltipIn);
		_cbxSoundFormat.OnMouseOut(TxtTooltipOut);

		string curSound = sndFormats[(int)Options.currentSound];
		_txtCurrentSound.SetText(Tr("STR_CURRENT_FORMAT").Arg(curSound));

		_txtVideoFormat.SetText(Tr("STR_PREFERRED_VIDEO_FORMAT"));

		_cbxVideoFormat.SetOptions(videoText);
		_cbxVideoFormat.Selected = (int)Options.preferredVideo;
		_cbxVideoFormat.Tooltip = "STR_PREFERRED_VIDEO_FORMAT_DESC";
		_cbxVideoFormat.OnChange(CbxVideoFormatChange);
		_cbxVideoFormat.OnMouseIn(TxtTooltipIn);
		_cbxVideoFormat.OnMouseOut(TxtTooltipOut);

		// These options require a restart, so don't enable them in-game
		_txtMusicFormat.Visible = _origin == OptionsOrigin.OPT_MENU;
		_cbxMusicFormat.Visible = _origin == OptionsOrigin.OPT_MENU;
		_txtCurrentMusic.Visible = _origin == OptionsOrigin.OPT_MENU;

		// These options only apply to UFO
		_txtSoundFormat.Visible = _origin == OptionsOrigin.OPT_MENU && _game.Mod.SoundDefinitions.Count == 0;
		_cbxSoundFormat.Visible = _origin == OptionsOrigin.OPT_MENU && _game.Mod.SoundDefinitions.Count == 0;
		_txtCurrentSound.Visible = _origin == OptionsOrigin.OPT_MENU && _game.Mod.SoundDefinitions.Count == 0;

		_txtOptions.SetText(Tr("STR_SOUND_OPTIONS"));

		_btnBackgroundMute.Text = Tr("STR_BACKGROUND_MUTE");
		_btnBackgroundMute.Pressed = Options.backgroundMute;
		_btnBackgroundMute.OnMouseClick(BtnBackgroundMuteClick);
		_btnBackgroundMute.Tooltip = "STR_BACKGROUND_MUTE_DESC";
		_btnBackgroundMute.OnMouseIn(TxtTooltipIn);
		_btnBackgroundMute.OnMouseOut(TxtTooltipOut);
	}

	/// <summary>Updates the music volume.</summary>
	public void SlrMusicVolumeChange(Action action)
	{
		Options.musicVolume = _slrMusicVolume.Value;
		_game.SetVolume(Options.soundVolume, Options.musicVolume, Options.uiVolume);
	}

	/// <summary>Updates the sound volume with the slider.</summary>
	public void SlrSoundVolumeChange(Action action)
	{
		Options.soundVolume = _slrSoundVolume.Value;
		_game.SetVolume(Options.soundVolume, Options.musicVolume, Options.uiVolume);
	}

	/// <summary>Plays a game sound for volume preview.</summary>
	public void SlrSoundVolumeRelease(Action action)
	{
		_game.Mod.GetSound("GEO.CAT", Acom.Mod.UFO_FIRE).Play();
	}

	/// <summary>Updates the UI volume with the slider.</summary>
	public void SlrUiVolumeChange(Action action)
	{
		Options.uiVolume = _slrUiVolume.Value;
		_game.SetVolume(Options.soundVolume, Options.musicVolume, Options.uiVolume);
	}

	/// <summary>Plays a UI sound for volume preview.</summary>
	public void SlrUiVolumeRelease(Action action)
	{
		TextButton.soundPress.Play(SDLMixer.Mix_GroupAvailable(0));
	}

	/// <summary>Changes the Music Format option.</summary>
	public void CbxMusicFormatChange(Action action)
	{
		Options.preferredMusic = (MusicFormat)_cbxMusicFormat.Selected;
		Options.reload = true;
	}

	/// <summary>Changes the Sound Format option.</summary>
	public void CbxSoundFormatChange(Action action)
	{
		Options.preferredSound = (SoundFormat)_cbxSoundFormat.Selected;
		Options.reload = true;
	}

	/// <summary>Changes the Video Format option.</summary>
	public void CbxVideoFormatChange(Action action)
	{
		Options.preferredVideo = (VideoFormat)_cbxVideoFormat.Selected;
	}

	/// <summary>Updates the Background Mute option.</summary>
	public void BtnBackgroundMuteClick(Action action)
	{
		Options.backgroundMute = _btnBackgroundMute.Pressed;
	}
}
