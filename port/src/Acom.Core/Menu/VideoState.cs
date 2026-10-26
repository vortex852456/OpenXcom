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
// Ported from: src/Menu/VideoState.h, src/Menu/VideoState.cpp
// Port status: partial (missing: Screen::resetDisplay, the port's screen keeps its logical size)
// Deviation: C++ init() plays all videos and the fade-out in a blocking loop of its own (polling events, SDL_Delay,
// flipping the screen). MonoGame owns the main loop here, so init() only starts the first video; think() advances
// the FlcPlayer (one frame per game cycle at most), handle() passes clicks/keys on to it, blit() shows the decoded
// frame, and the fade-out runs over the following cycles. The order of all steps is the C++ one.
// Deviation: a video that fails the header check is skipped (C++ plays it anyway and crashes).
#nullable disable

namespace Acom;

/// <summary>
/// Shows video cinematics.
/// </summary>
public class VideoState : State
{
	private readonly List<string> _videos, _tracks;
	private bool _useUfoAudioSequence;

	private enum Phase
	{
		NotStarted,
		Playing,
		Delaying,
		Fading,
		Done,
	}

	private const int FADE_DELAY = 45;
	private const int FADE_STEPS = 20;

	// Locals of the C++ init(), kept between game cycles.
	private Phase _phase = Phase.NotStarted;
	private bool _wasLetterboxed;
	private int _prevMusicVol, _prevSoundVol;
	private int _dx, _dy;
	private bool _fade;
	private FlcPlayer _flcPlayer;
	private AudioSequence _audioSequence;
	private int _videoIndex;
	private int _audioCounter;
	private Surface _frame;
	private Rgba[] _fadePalette;
	private int _fadeStep;
	private uint _fadeNext;

	/// <summary>
	/// Initializes all the elements in the Intro screen.
	/// </summary>
	public VideoState(List<string> videos, List<string> tracks, bool useUfoAudioSequence)
	{
		_videos = videos;
		_tracks = tracks;
		_useUfoAudioSequence = useUfoAudioSequence;
	}

	private struct SoundInFile(string catFile, int sound, int volume)
	{
		public string catFile = catFile;
		public int sound = sound;
		public int volume = volume;
	}

	// the pure MS-DOS experience
	private static readonly SoundInFile[] introCatOnlySounds =
	[
		new("INTRO.CAT", 0x0, 32),
		new("INTRO.CAT", 0x1, 32),
		new("INTRO.CAT", 0x2, 32),
		new("INTRO.CAT", 0x3, 32),
		new("INTRO.CAT", 0x4, 32),
		new("INTRO.CAT", 0x5, 32),
		new("INTRO.CAT", 0x6, 32),
		new("INTRO.CAT", 0x7, 32),
		new("INTRO.CAT", 0x8, 32),
		new("INTRO.CAT", 0x9, 32),
		new("INTRO.CAT", 0xa, 32),
		new("INTRO.CAT", 0xb, 32),
		new("INTRO.CAT", 0xc, 32),
		new("INTRO.CAT", 0xd, 32),
		new("INTRO.CAT", 0xe, 32),
		new("INTRO.CAT", 0xf, 32),
		new("INTRO.CAT", 0x10, 32),
		new("INTRO.CAT", 0x11, 32),
		new("INTRO.CAT", 0x12, 32),
		new("INTRO.CAT", 0x13, 32),
		new("INTRO.CAT", 0x14, 32),
		new("INTRO.CAT", 0x15, 32),
		new("INTRO.CAT", 0x16, 32),
		new("INTRO.CAT", 0x17, 32),
		new("INTRO.CAT", 0x18, 32),
		new("INTRO.CAT", 0x18, 32),
	];

	private static readonly SoundInFile[] sample3CatOnlySounds =
	[
		new("SAMPLE3.CAT", 24, 32), // machine gun
		new("SAMPLE3.CAT", 5, 32),   // plasma rifle
		new("SAMPLE3.CAT", 23, 32), // rifle
		new("SAMPLE3.CAT", 6, 32), // some kind of death noise, urgh?
		new("SAMPLE3.CAT", 9, 64), // mutdie
		new("SAMPLE3.CAT", 7, 64), // dying alien
		new("SAMPLE3.CAT", 27, 64), // another dying alien
		new("SAMPLE3.CAT", 4, 32), // ??? ship flying? alien screech?
		new("SAMPLE3.CAT", 0x8, 32), // fscream
		new("SAMPLE3.CAT", 11, 32), // alarm
		new("SAMPLE3.CAT", 4, 32), // gun spinning up?
		new("INTRO.CAT", 0xb, 32),  // reload; this one's not even in sample3
		new("SAMPLE3.CAT", 19, 48),  // whoosh
		new("INTRO.CAT", 0xd, 32),  // feet, also not in sample3
		new("SAMPLE3.CAT", 2, 32),  // low pulsating hum
		new("SAMPLE3.CAT", 30, 32), // energise
		new("SAMPLE3.CAT", 21, 32), // hatch
		new("SAMPLE3.CAT", 0, 64), // phizz -- no equivalent in sample3.cat?
		new("SAMPLE3.CAT", 13, 32), // warning
		new("SAMPLE3.CAT", 14, 32), // detected
		new("SAMPLE3.CAT", 19, 64), // UFO flyby whoosh?
		new("SAMPLE3.CAT", 3, 32), // growl
		new("SAMPLE3.CAT", 15, 128), // voice
		new("SAMPLE3.CAT", 12, 32), // beep 1
		new("SAMPLE3.CAT", 18, 32), // takeoff
		new("SAMPLE3.CAT", 20, 32),  // another takeoff/landing sound?? if it exists?
	];

	// an attempt at a mix of (subjectively) the best sounds from the two versions
	// difficult because we can't find a definitive map from old sequence numbers to SAMPLE3.CAT indexes
	// probably only the Steam version of the game comes with both INTRO.CAT and SAMPLE3.CAT
	private static readonly SoundInFile[] hybridIntroSounds =
	[
		new("SAMPLE3.CAT", 24, 32), // machine gun
		new("SAMPLE3.CAT", 5, 32),   // plasma rifle
		new("SAMPLE3.CAT", 23, 32), // rifle
		new("INTRO.CAT", 3, 32), // some kind of death noise, urgh?
		new("INTRO.CAT", 0x4, 64), // mutdie
		new("INTRO.CAT", 0x5, 64), // dying alien
		new("INTRO.CAT", 0x6, 64), // another dying alien
		new("INTRO.CAT", 0x7, 32), // ??? ship flying? alien screech?
		new("SAMPLE3.CAT", 0x8, 32), // fscream
		new("SAMPLE3.CAT", 11, 32), // alarm
		new("SAMPLE3.CAT", 4, 32), // gun spinning up?
		new("INTRO.CAT", 0xb, 32),  // reload; this one's not even in sample3
		new("SAMPLE3.CAT", 19, 48),  // whoosh
		new("INTRO.CAT", 0xd, 32),  // feet, also not in sample3
		new("INTRO.CAT", 0xe, 32),  // low pulsating hum
		new("SAMPLE3.CAT", 30, 32), // energise
		new("SAMPLE3.CAT", 21, 32), // hatch
		new("INTRO.CAT", 0x11, 64), // phizz
		new("SAMPLE3.CAT", 13, 32), // warning
		new("SAMPLE3.CAT", 14, 32), // detected
		new("SAMPLE3.CAT", 19, 64), // UFO flyby whoosh?
		new("INTRO.CAT", 0x15, 32), // growl
		new("SAMPLE3.CAT", 15, 128), // voice
		new("SAMPLE3.CAT", 12, 32), // beep 1
		new("SAMPLE3.CAT", 18, 32), // takeoff
		new("SAMPLE3.CAT", 20, 32),  // another takeoff/landing sound?? if it exists?
	];

	// sample3: 18 is takeoff, 20 is landing; 19 is flyby whoosh sound, not sure for which craft

	private static readonly SoundInFile[][] introSounds =
	[
		hybridIntroSounds,
		introCatOnlySounds,
		sample3CatOnlySounds,
	];

	private readonly record struct IntroSoundEffect(int frameNumber, int sound);

	private static readonly IntroSoundEffect[] introSoundTrack =
	[
		new(0, 0x200), // inserting this to keep the code simple
		new(149, 0x11),
		new(173, 0x0C),
		new(183, 0x0E),
		new(205, 0x15),
		new(211, 0x201),
		new(211, 0x407),
		new(223, 0x7),
		new(250, 0x1),
		new(253, 0x1),
		new(255, 0x1),
		new(257, 0x1),
		new(260, 0x1),
		new(261, 0x3),
		new(262, 0x1),
		new(264, 0x1),
		new(268, 0x1),
		new(270, 0x1),
		new(272, 0x5),
		new(272, 0x1),
		new(274, 0x1),
		new(278, 0x1),
		new(280, 0x1),
		new(282, 0x8),
		new(282, 0x1),
		new(284, 0x1),
		new(286, 0x1),
		new(288, 0x1),
		new(290, 0x1),
		new(292, 0x6),
		new(292, 0x1),
		new(296, 0x1),
		new(298, 0x1),
		new(300, 0x1),
		new(302, 0x1),
		new(304, 0x1),
		new(306, 0x1),
		new(308, 0x1),
		new(310, 0x1),
		new(312, 0x1),
		new(378, 0x202),
		new(378, 0x9), // alarm
		new(386, 0x9),
		new(393, 0x9),
		new(399, 0x17), // bleeps
		new(433, 0x17),
		new(463, 0x12), // warning
		new(477, 0x12),
		new(487, 0x13), // ufo detected
		new(495, 0x16), // voice
		new(501, 0x16),
		new(512, 0xd),  // feet -- not in original
		new(514, 0xd),  // feet -- not in original
		new(522, 0x0B), // rifle grab
		new(523, 0xd),  // feet -- not in original
		new(525, 0xd),  // feet -- not in original
		new(534, 0x18),
		new(535, 0x405),
		new(560, 0x407),
		new(577, 0x14),
		new(582, 0x405),
		// new(582, 0x18), // landing! correcting to landing sound!
		new(582, 0x19),
		new(613, 0x407),
		new(615, 0x10),
		new(635, 0x14),
		new(638, 0x14),
		new(639, 0x14),
		new(644, 0x2),
		new(646, 0x2),
		new(648, 0x2),
		new(650, 0x2),
		new(652, 0x2),
		new(654, 0x2),
		new(656, 0x2),
		new(658, 0x2),
		new(660, 0x2),
		new(662, 0x2),
		new(664, 0x2),
		new(666, 0x2),
		new(668, 0x401),
		new(681, 0x406),
		new(687, 0x402),
		new(689, 0x407),
		new(694, 0x0A),
		new(711, 0x407),
		new(711, 0x0),
		new(714, 0x0),
		new(716, 0x4),
		new(717, 0x0),
		new(720, 0x0),
		new(723, 0x0),
		new(726, 0x5),
		new(726, 0x0),
		new(729, 0x0),
		new(732, 0x0),
		new(735, 0x0),
		new(738, 0x0),
		new(741, 0x0),
		new(742, 0x6),
		new(744, 0x0),
		new(747, 0x0),
		new(750, 0x0),
		new(753, 0x0),
		new(756, 0x0),
		new(759, 0x0),
		new(762, 0x0),
		new(765, 0x0),
		new(768, 0x0),
		new(771, 0x0),
		new(774, 0x0),
		new(777, 0x0),
		new(780, 0x0),
		new(783, 0x0),
		new(786, 0x0),
		new(790, 0x15),
		new(790, 0x15),
		new(807, 0x2),
		new(810, 0x2),
		new(812, 0x2),
		new(814, 0x2),
		new(816, 0x0),
		new(819, 0x0),
		new(822, 0x0),
		new(824, 0x40A),
		new(824, 0x5),
		new(827, 0x6),
		new(835, 0x0F),
		new(841, 0x0F),
		new(845, 0x0F),
		new(855, 0x407),
		new(879, 0x0C),
		new(65535, 0x0FFFF),
	];

	/// <summary>Plays the music, sounds and speed changes of the UFO intro at their frame numbers.</summary>
	private sealed class AudioSequence(Mod mod, FlcPlayer flcPlayer)
	{
		private readonly Mod _mod = mod;
		private Music _m;
		private Sound _s;
		private int _trackPosition;
		private readonly FlcPlayer _flcPlayer = flcPlayer;

		public void Run()
		{
			while (_flcPlayer.FrameCount >= introSoundTrack[_trackPosition].frameNumber)
			{
				int command = introSoundTrack[_trackPosition].sound;

				if ((command & 0x200) != 0)
				{
					switch (command)
					{
						case 0x200:
							Logger.Debug("Playing gmintro1");
							_m = _mod.GetMusic("GMINTRO1");
							_m.Play(1);
							break;
						case 0x201:
							Logger.Debug("Playing gmintro2");
							_m = _mod.GetMusic("GMINTRO2");
							_m.Play(1);
							break;
						case 0x202:
							Logger.Debug("Playing gmintro3");
							_m = _mod.GetMusic("GMINTRO3");
							_m.Play(1);
							//Mix_HookMusicFinished(_FlcPlayer::stop);
							break;
					}
				}
				else if ((command & 0x400) != 0)
				{
					int newSpeed = command & 0xff;
					_flcPlayer.SetHeaderSpeed(newSpeed);
					Logger.Debug($"Frame delay now: {newSpeed}");
				}
				else if (command <= 0x19)
				{
					foreach (var sounds in introSounds) // try hybrid sound set, then intro.cat or sample3.cat alone
					{
						SoundInFile sf = sounds[command];
						int channel = _trackPosition % 4; // use at most four channels to play sound effects
						double ratio = (double)Options.soundVolume / Mixer.MIX_MAX_VOLUME;
						Logger.Debug($"playing: {sf.catFile}:{sf.sound} for index {command}");
						_s = _mod.GetSound(sf.catFile, sf.sound/*, false*/);
						if (_s != null)
						{
							_s.Play(channel);
							Mixer.Volume(channel, (int)(sf.volume * ratio));
							break;
						}
						else Logger.Debug($"Couldn't play {sf.catFile}:{sf.sound}");
					}
				}
				++_trackPosition;
			}
		}
	}

	/// <summary>
	/// Plays the video (starts it; see the deviation note at the top).
	/// </summary>
	public override void Init()
	{
		base.Init();
		if (_phase != Phase.NotStarted)
			return;

		_wasLetterboxed = CutsceneState.InitDisplay();

		bool ufoIntroSoundFileDosExists = false;
		bool ufoIntroSoundFileWinExists = false;
		_prevMusicVol = Options.musicVolume;
		_prevSoundVol = Options.soundVolume;
		if (_useUfoAudioSequence)
		{
			var soundDir = FileMap.GetVFolderContents("SOUND");
			ufoIntroSoundFileDosExists = soundDir.Contains("intro.cat");
			ufoIntroSoundFileWinExists = soundDir.Contains("sample3.cat");

			if (!ufoIntroSoundFileDosExists && !ufoIntroSoundFileWinExists)
			{
				_useUfoAudioSequence = false;
			}
			else
			{
				// ensure user can hear both music and sound effects for the
				// vanilla intro sequence
				Options.musicVolume = Options.soundVolume = Math.Max(_prevMusicVol, _prevSoundVol);
				_game.SetVolume(Options.soundVolume, Options.musicVolume, -1);
			}
		}
		_game.Cursor.Visible = false;

		_dx = (Options.baseXResolution - Screen.ORIGINAL_WIDTH) / 2;
		_dy = (Options.baseYResolution - Screen.ORIGINAL_HEIGHT) / 2;

		// We can only do a fade out in 8bpp, otherwise instantly end it
		_fade = true; // the port's screen is always palette-indexed

		_flcPlayer = null;
		_audioCounter = 0;
		_videoIndex = 0;
		NextVideo();
	}

	/// <summary>Starts the next existing video of the list (one pass of the C++ loop), or ends the list.</summary>
	private void NextVideo()
	{
		while (_videoIndex < _videos.Count)
		{
			string videoFileName = _videos[_videoIndex++];
			bool useInternalAudio = true;
			if (_tracks.Count != 0 && _tracks.Count > _audioCounter && _game.Mod.GetMusic(_tracks[_audioCounter]) != null)
			{
				_game.Mod.GetMusic(_tracks[_audioCounter]).Play(0);
				useInternalAudio = false;
			}
			_audioCounter++;

			if (!FileMap.FileExists(videoFileName))
			{
				continue;
			}

			if (_flcPlayer == null)
			{
				_flcPlayer = new FlcPlayer();
			}

			if (_useUfoAudioSequence)
			{
				_audioSequence = new AudioSequence(_game.Mod, _flcPlayer);
			}

			if (!_flcPlayer.Init(videoFileName, _useUfoAudioSequence ? _audioSequence.Run : null, _game, useInternalAudio, _dx, _dy))
			{
				_flcPlayer.DeInit();
				_audioSequence = null;
				continue;
			}
			_frame = _flcPlayer.Surface;
			_flcPlayer.Play(_useUfoAudioSequence);
			_phase = Phase.Playing;
			return;
		}
		EndVideos();
	}

	/// <summary>Called when a video (and its delay) has ended.</summary>
	private void EndVideo()
	{
		_audioSequence = null;
		_flcPlayer.DeInit();

		if (_flcPlayer.WasSkipped())
		{
			_fade = false;
			EndVideos();
			return;
		}
		NextVideo();
	}

	/// <summary>All videos are done: fades out the music and starts fading out the screen.</summary>
	private void EndVideos()
	{
		_flcPlayer = null;

		// fade out!
		if (_fade)
		{
			Mixer.FadeOutChannel(-1, FADE_DELAY * FADE_STEPS);
			// SDL_Mixer has trouble with native midi and volume on windows,
			// which is the most likely use case, so f@%# it.
			if (Mixer.GetMusicType(null) != MusicType.MUS_MID)
			{
				Mixer.FadeOutMusic(FADE_DELAY * FADE_STEPS);
				AdlPlayer.func_fade();
			}
			else
			{
				Mixer.HaltMusic();
			}
		}
		else
		{
			Mixer.HaltChannel(-1);
			Mixer.HaltMusic();
		}

		if (_fade)
		{
			_fadePalette = _game.Screen.Palette.ToArray();
			_fadeStep = FADE_STEPS;
			_fadeNext = SDLTimer.SDL_GetTicks();
			_phase = Phase.Fading;
			FadeStep();
		}
		else
		{
			Finish();
		}
	}

	/// <summary>One step of the C++ fade loop (palette, flip, then SDL_Delay(FADE_DELAY) until the next step).</summary>
	private void FadeStep()
	{
		if (SDLTimer.SDL_GetTicks() < _fadeNext)
			return;
		if (_fadeStep <= 0)
		{
			Finish();
			return;
		}
		int i = _fadeStep--;
		var pal2 = new Rgba[256];
		for (int color = 0; color < 256; ++color)
		{
			var pal = _fadePalette[color];
			pal2[color] = new Rgba((byte)((pal.R * i) / 20), (byte)((pal.G * i) / 20), (byte)((pal.B * i) / 20), pal.A);
		}
		_game.Screen.SetPalette(pal2, 0, 256);
		_fadeNext = SDLTimer.SDL_GetTicks() + FADE_DELAY;
	}

	/// <summary>Clears the screen, restores audio and display and pops the state (the end of the C++ init()).</summary>
	private void Finish()
	{
		_phase = Phase.Done;
		_frame = null;
		_game.Screen.Clear();

		if (_useUfoAudioSequence)
		{
			Options.musicVolume = _prevMusicVol;
			Options.soundVolume = _prevSoundVol;
			_game.SetVolume(Options.soundVolume, Options.musicVolume, Options.uiVolume);
		}

		Sound.Stop();
		Music.Stop();

		_game.Cursor.Visible = true;
		CutsceneState.ResetDisplay(_wasLetterboxed);
		_game.PopState();
	}

	/// <summary>Advances the video, the pause after it or the fade-out.</summary>
	public override void Think()
	{
		base.Think();
		switch (_phase)
		{
			case Phase.Playing:
				if (!_flcPlayer.Update())
				{
					if (_useUfoAudioSequence)
					{
						_flcPlayer.Delay(10000);
						_phase = Phase.Delaying;
						if (!_flcPlayer.Update())
							EndVideo();
					}
					else
					{
						EndVideo();
					}
				}
				break;
			case Phase.Delaying:
				if (!_flcPlayer.Update())
					EndVideo();
				break;
			case Phase.Fading:
				FadeStep();
				break;
			default:
				break;
		}
	}

	/// <summary>Passes clicks and key presses on to the player (they skip the video) or ends the fade-out.</summary>
	public override void Handle(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		base.Handle(action);
		switch (_phase)
		{
			case Phase.Playing:
			case Phase.Delaying:
				_flcPlayer.HandleEvent(action.Details);
				break;
			case Phase.Fading:
				if (action.Details.type == SDLEventType.SDL_KEYDOWN)
					Finish();
				break;
			default:
				break;
		}
	}

	/// <summary>Shows the current video frame.</summary>
	public override void Blit()
	{
		base.Blit();
		_frame?.Blit(_game.Screen.Surface);
	}
}
