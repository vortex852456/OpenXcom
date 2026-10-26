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
// Ported from: src/Engine/FlcPlayer.h, src/Engine/FlcPlayer.cpp
// Based on http://www.libsdl.org/projects/flxplay/ (see also https://github.com/aseprite/flic)
// Deviation: playback does not block. C++ play()/delay() run their own event loop, wait with SDL_Delay and flip
// the screen; here Play() and Delay() only start, Update() runs one step per game cycle without waiting,
// HandleEvent() takes the input SDLPolling() read, and the owner (VideoState) blits Surface onto the screen.
// Deviation: frames decode into a palette-indexed surface of their own (at least the screen size, so a video larger
// than the screen is cropped instead of switching the display resolution); reads past the end of a truncated file
// end the video instead of reading foreign memory; palette updates are clamped to 256 entries.
// Deviation: the audio callback continues the output stream after a buffer swap (C++ copies to the start of the
// stream again, overwriting the samples it just wrote).

using System.Runtime.InteropServices;

namespace Acom;

/// <summary>
/// Player for FLI/FLC videos (with the TFTD audio chunk extension).
/// </summary>
public class FlcPlayer
{
	// Taken from: http://www.compuphase.com/flic.htm
	private const ushort FLI_TYPE = 0xAF11;
	private const ushort FLC_TYPE = 0xAF12;

	private const ushort COLOR_256 = 0x04;
	private const ushort FLI_SS2 = 0x07; // or DELTA_FLC
	private const ushort COLOR_64 = 0x0B;
	private const ushort FLI_LC = 0x0C; // or DELTA_FLI
	private const ushort BLACK = 0x0D;
	private const ushort FLI_BRUN = 0x0F; // or BYTE_RUN
	private const ushort FLI_COPY = 0x10;
	private const ushort AUDIO_CHUNK = 0xAAAA; // This is the only exception, it's from TFTD
	private const ushort PREFIX_CHUNK = 0xF100;
	private const ushort FRAME_TYPE = 0xF1FA;

	private const int PACKETS_COUNT = 0x0000; // 0000000000000000
	private const int LAST_PIXEL = 0x8000; // 1000000000000000
	private const int SKIP_LINES = 0xc000; // 1100000000000000
	private const int MASK = SKIP_LINES;

	private enum PlayingState
	{
		PLAYING,
		FINISHED,
		SKIPPED,
	}

	private byte[]? _fileBuf;
	private uint _fileSize;
	private int _videoFrameData;
	private int _chunkData;
	private int _audioFrameData;
	private ushort _frameCount;    /* Frame Counter */
	private uint _headerSize;      /* Fli file size */
	private ushort _headerType;    /* Fli header check */
	private ushort _headerFrames;  /* Number of frames in flic */
	private ushort _headerWidth;   /* Fli width */
	private ushort _headerHeight;  /* Fli height */
	private ushort _headerDepth;   /* Color depth */
	private ushort _headerSpeed;   /* Number of video ticks between frame */
	private uint _videoFrameSize;  /* Frame size in bytes */
	private ushort _videoFrameType;
	private ushort _frameChunks;   /* Number of chunks in frame */
	private uint _chunkSize;       /* Size of chunk */
	private ushort _chunkType;     /* Type of chunk */
	private ushort _delayOverride; /* FRAME_TYPE extension */
	private uint _audioFrameSize;
	private ushort _audioFrameType;

	private System.Action? _frameCallBack;

	private Surface? _mainScreen;
	private Screen? _realScreen;
	private readonly Rgba[] _colors = new Rgba[256];
	private int _screenWidth;
	private int _screenHeight;
	private int _dx, _dy;
	private int _offset;
	private PlayingState _playingState;
	private bool _hasAudio, _useInternalAudio;
	private int _videoDelay;
	private readonly double _volume;

	// State of the non-blocking play loop (C++ keeps it on the stack of play()/waitForNextFrame()/delay()).
	private bool _skipLastFrame;
	private bool _waitingForFrame;
	private int _newTick;
	private bool _delaying;
	private uint _pauseStart, _pauseLength;

	/// <summary>Last frame time; a function-static variable in C++, so it carries over between videos.</summary>
	private static uint s_oldTick;

	/// <summary>Samples waiting to be played (one of the two buffers swapped between decoder and mixer).</summary>
	public class AudioBuffer
	{
		public short[] samples = [];
		public int sampleCount;
		public int sampleBufSize;
		public int currSamplePos;
	}

	/// <summary>Audio state shared with the mixer thread.</summary>
	public class AudioData
	{
		public int sampleRate;
		public AudioBuffer? loadingBuffer;
		public AudioBuffer? playingBuffer;
		public object? sharedLock;
	}

	private readonly AudioData _audioData = new();

	private Game? _game;

	public FlcPlayer()
	{
		_volume = Game.VolumeExponent(Options.musicVolume);
	}

	/// <summary>The palette-indexed surface the frames are decoded into (null when not initialized).</summary>
	public Surface? Surface => _mainScreen;

	/// <summary>
	/// Initialize data structures needed buy the player and read the whole file into memory
	/// </summary>
	/// <param name="filename">Video file name</param>
	/// <param name="frameCallBack">Function to call each video frame</param>
	/// <param name="game">Pointer to the Game instance</param>
	/// <param name="useInternalAudio">Play the audio chunks of the file</param>
	/// <param name="dx">An offset on the x axis for the video to be rendered</param>
	/// <param name="dy">An offset on the y axis for the video to be rendered</param>
	public bool Init(string filename, System.Action? frameCallBack, Game game, bool useInternalAudio, int dx, int dy)
	{
		if (_fileBuf != null)
		{
			Logger.Error("Trying to init a video player that is already initialized");
			return false;
		}
		return Init(FileMap.ReadAll(filename), frameCallBack, game, useInternalAudio, dx, dy);
	}

	/// <summary>
	/// Same as <see cref="Init(string, System.Action, Game, bool, int, int)"/>, with the file already in memory.
	/// </summary>
	public bool Init(byte[] fileData, System.Action? frameCallBack, Game game, bool useInternalAudio, int dx, int dy)
	{
		ArgumentNullException.ThrowIfNull(fileData);
		ArgumentNullException.ThrowIfNull(game);
		if (_fileBuf != null)
		{
			Logger.Error("Trying to init a video player that is already initialized");
			return false;
		}

		_frameCallBack = frameCallBack;
		_realScreen = game.Screen;
		_realScreen.Clear();
		_game = game;
		_useInternalAudio = useInternalAudio;
		_dx = dx;
		_dy = dy;

		_fileSize = 0;
		_frameCount = 0;
		_audioFrameData = 0;
		_hasAudio = false;
		_audioData.loadingBuffer = null;
		_audioData.playingBuffer = null;

		_fileBuf = fileData;
		_fileSize = (uint)fileData.Length;

		_audioFrameData = 128;

		if (_fileSize < 128)
		{
			Logger.Error("Flx file failed header check.");
			return false;
		}

		// Let's read the first 128 bytes
		ReadFileHeader();

		// If it's a FLC or FLI file, it's ok
		if (_headerType == FLI_TYPE || _headerType == FLC_TYPE)
		{
			_screenWidth = _headerWidth;
			_screenHeight = _headerHeight;
			// _screenDepth = 8; (unused)

			Logger.Info($"Playing flx, {_screenWidth}x{_screenHeight}, {_headerFrames} frames");
		}
		else
		{
			Logger.Error("Flx file failed header check.");
			return false;
		}
		// C++ switches the display to the video resolution here when the video is wider than the screen
		// (and the display allows it); the port decodes into a surface large enough for the video instead.
		var screenSurface = _realScreen.Surface;
		_mainScreen = new Surface(Math.Max(screenSurface.Width, _screenWidth), Math.Max(screenSurface.Height, _screenHeight), 0, 0, 1);

		return true;
	}

	/// <summary>Free memory, free love, etc.</summary>
	public void DeInit()
	{
		if (_mainScreen != null && _realScreen != null)
		{
			_mainScreen = null;
		}

		if (_fileBuf != null)
		{
			_fileBuf = null;

			DeInitAudio();
		}
	}

	/// <summary>
	/// Starts decoding and playing the FLI/FLC file. Unlike C++ this returns at once; call <see cref="Update"/>
	/// every game cycle until it returns false.
	/// </summary>
	public void Play(bool skipLastFrame)
	{
		if (_fileBuf == null || _mainScreen == null)
			throw new InvalidOperationException("FlcPlayer.Play called before a successful Init");

		_playingState = PlayingState.PLAYING;
		_skipLastFrame = skipLastFrame;
		_waitingForFrame = false;
		_delaying = false;

		// Vertically center the video
		_dy = (_mainScreen.Height - _headerHeight) / 2;

		_offset = _dy * Pitch + 1 * _dx;

		// Skip file header
		_videoFrameData = 128;
		_audioFrameData = _videoFrameData;
	}

	/// <summary>
	/// Runs the play loop (or a pause started by <see cref="Delay"/>) as far as possible without waiting:
	/// at most one video frame is decoded per call.
	/// </summary>
	/// <returns>True while the video (or pause) is still running.</returns>
	public bool Update()
	{
		if (_delaying)
		{
			if (_playingState != PlayingState.SKIPPED && SDLTimer.SDL_GetTicks() < (_pauseStart + _pauseLength))
				return true;
			_delaying = false;
			return false;
		}

		if (_fileBuf == null || ShouldQuit())
			return false;

		if (!_waitingForFrame)
		{
			if (_frameCallBack != null)
				_frameCallBack();
			else // TODO: support both, in the case the callback is not some audio?
				DecodeAudio(2);

			if (ShouldQuit())
				return false;

			// first half of decodeVideo(): find the next frame and start waiting for it
			if (!FindVideoFrame(out uint delay))
				return false;
			BeginWaitForNextFrame(delay);
			_waitingForFrame = true;
		}

		if (!WaitForNextFrame())
			return true;
		_waitingForFrame = false;

		// second half of decodeVideo()
		// Skip the frame header, we are not interested in the rest
		_chunkData = _videoFrameData + 16;

		_videoFrameData += (int)_videoFrameSize;
		// If this frame is the last one, don't play it
		if (IsEndOfFile(_videoFrameData))
			_playingState = PlayingState.FINISHED;

		if (!ShouldQuit() || !_skipLastFrame)
			PlayVideoFrame();

		return !ShouldQuit();
	}

	/// <summary>Is the video (or pause) still running?</summary>
	public bool IsPlaying => _delaying || (_fileBuf != null && !ShouldQuit());

	/// <summary>
	/// Delay player at the end. Unlike C++ this returns at once; <see cref="Update"/> returns true until
	/// the time is up or the video was skipped.
	/// </summary>
	public void Delay(uint milliseconds)
	{
		_pauseStart = SDLTimer.SDL_GetTicks();
		_pauseLength = milliseconds;
		_delaying = true;
	}

	/// <summary>
	/// Takes the events C++ SDLPolling() reads during playback: a mouse click or key press skips the video.
	/// (Window resizes and quitting are handled by the host and <see cref="Game"/>.)
	/// </summary>
	public void HandleEvent(SDL_Event ev)
	{
		ArgumentNullException.ThrowIfNull(ev);
		switch (ev.type)
		{
			case SDLEventType.SDL_MOUSEBUTTONDOWN:
			case SDLEventType.SDL_KEYDOWN:
				_playingState = PlayingState.SKIPPED;
				break;
			default:
				break;
		}
	}

	private bool ShouldQuit() => _playingState == PlayingState.FINISHED || _playingState == PlayingState.SKIPPED;

	private int Pitch => _mainScreen!.PhysicalWidth;

	private void ReadFileHeader()
	{
		var buf = _fileBuf!;
		ReadU32(out _headerSize, buf, 0);
		ReadU16(out _headerType, buf, 4);
		ReadU16(out _headerFrames, buf, 6);
		ReadU16(out _headerWidth, buf, 8);
		ReadU16(out _headerHeight, buf, 10);
		ReadU16(out _headerDepth, buf, 12);
		ReadU16(out _headerSpeed, buf, 16);
	}

	private bool IsValidFrame(int frameHeader, out uint frameSize, out ushort frameType)
	{
		if (frameHeader < 0 || frameHeader + 6 > _fileSize)
		{
			// C++ reads past the end of the buffer here
			frameSize = 0;
			frameType = 0;
			return false;
		}
		ReadU32(out frameSize, _fileBuf!, frameHeader);
		ReadU16(out frameType, _fileBuf!, frameHeader + 4);

		return frameType == FRAME_TYPE || frameType == AUDIO_CHUNK || frameType == PREFIX_CHUNK;
	}

	private void DecodeAudio(int frames)
	{
		int audioFramesFound = 0;

		while (audioFramesFound < frames && !IsEndOfFile(_audioFrameData))
		{
			if (!IsValidFrame(_audioFrameData, out _audioFrameSize, out _audioFrameType))
			{
				_playingState = PlayingState.FINISHED;
				break;
			}

			switch (_audioFrameType)
			{
				case FRAME_TYPE:
				case PREFIX_CHUNK:
					_audioFrameData += (int)_audioFrameSize;
					break;
				case AUDIO_CHUNK:
					ReadU16(out ushort sampleRate, _fileBuf!, _audioFrameData + 8);

					_chunkData = _audioFrameData + 16;

					PlayAudioFrame(sampleRate);

					_audioFrameData += (int)_audioFrameSize + 16;

					++audioFramesFound;

					break;
			}
		}
	}

	/// <summary>Finds the next video frame (the part of C++ decodeVideo() before waitForNextFrame()).</summary>
	/// <returns>False if the file has no further valid frame (the video is finished).</returns>
	private bool FindVideoFrame(out uint delay)
	{
		while (true)
		{
			if (!IsValidFrame(_videoFrameData, out _videoFrameSize, out _videoFrameType))
			{
				_playingState = PlayingState.FINISHED;
				delay = 0;
				return false;
			}

			switch (_videoFrameType)
			{
				case FRAME_TYPE:
					ReadU16(out _frameChunks, _fileBuf!, _videoFrameData + 6);
					ReadU16(out _delayOverride, _fileBuf!, _videoFrameData + 8);

					if (_headerType == FLI_TYPE)
					{
						delay = _delayOverride > 0 ? _delayOverride : (uint)(_headerSpeed * (1000.0 / 70.0));
					}
					else if (_useInternalAudio && _frameCallBack == null) // this means TFTD videos are playing
					{
						delay = (uint)_videoDelay;
					}
					else
					{
						delay = _headerSpeed;
					}
					return true;
				case AUDIO_CHUNK:
					_videoFrameData += (int)_videoFrameSize + 16;
					break;
				case PREFIX_CHUNK:
					// Just skip it
					_videoFrameData += (int)_videoFrameSize;
					break;
			}
		}
	}

	private void PlayVideoFrame()
	{
		++_frameCount;
		int chunkCount = _frameChunks;

		for (int i = 0; i < chunkCount; ++i)
		{
			ReadU32(out _chunkSize, _fileBuf!, _chunkData);
			ReadU16(out _chunkType, _fileBuf!, _chunkData + 4);

			switch (_chunkType)
			{
				case COLOR_256:
					Color256();
					break;
				case FLI_SS2:
					FliSS2();
					break;
				case COLOR_64:
					Color64();
					break;
				case FLI_LC:
					FliLC();
					break;
				case BLACK:
					Black();
					break;
				case FLI_BRUN:
					FliBRun();
					break;
				case FLI_COPY:
					FliCopy();
					break;
				case 18:
					break;
				default:
					Logger.Warning($"Ieek an non implemented chunk type:{_chunkType}");
					break;
			}

			_chunkData += (int)_chunkSize;
		}

		// C++ blits the frame onto the screen and flips it here; the owner of the player blits Surface instead.
	}

	private void PlayAudioFrame(ushort sampleRate)
	{
		/* TFTD audio header (10 bytes)
		* Uint16 unknown1 - always 0
		* Uint16 sampleRate
		* Uint16 unknown2 - always 1 (Channels? bytes per sample?)
		* Uint16 unknown3 - always 10 (No idea)
		* Uint16 unknown4 - always 0
		* Uint8[] unsigned 1-byte 1-channel PCM data of length _chunkSize_ (so the total chunk is _chunkSize_ + 6-byte flc header + 10 byte audio header */

		if (_useInternalAudio)
		{
			if (!_hasAudio)
			{
				_audioData.sampleRate = sampleRate;
				_hasAudio = true;
				InitAudio(Mixer.AUDIO_S16SYS, 1);
			}
			else
			{
				/* Cannot change sample rate mid-video */
				System.Diagnostics.Debug.Assert(sampleRate == _audioData.sampleRate);
			}

			lock (_audioData.sharedLock!)
			{
				AudioBuffer loadingBuff = _audioData.loadingBuffer!;
				System.Diagnostics.Debug.Assert(loadingBuff.currSamplePos == 0);
				int newSize = ((int)_audioFrameSize + loadingBuff.sampleCount) * 2;
				if (newSize > loadingBuff.sampleBufSize)
				{
					/* If the sample count has changed, we need to reallocate (Handles initial state
					* of '0' sample count too, as realloc(NULL, size) == malloc(size) */
					Array.Resize(ref loadingBuff.samples, newSize / 2);
					loadingBuff.sampleBufSize = newSize;
				}

				var buf = _fileBuf!;
				for (int i = 0; i < _audioFrameSize; i++)
				{
					loadingBuff.samples[loadingBuff.sampleCount + i] = (short)((float)(buf[_chunkData + i] - 128) * 240 * _volume);
				}
				loadingBuff.sampleCount += (int)_audioFrameSize;
			}
		}
		else
		{
			_audioData.sampleRate = sampleRate; // this is used to keep the framerate correct
		}
	}

	/// <summary>Updates the palette of the frame surface and the screen (C++ SDL_SetColors + Screen::setPalette).</summary>
	private void SetColors(int firstcolor, int ncolors)
	{
		ncolors = Math.Min(ncolors, 256 - firstcolor);
		var colors = _colors.AsSpan(0, ncolors);
		_mainScreen!.SetPalette(colors, firstcolor, ncolors);
		_realScreen!.SetPalette(colors, firstcolor, ncolors);
	}

	private void Color256()
	{
		var buf = _fileBuf!;
		int pSrc;
		ushort numColorPackets;
		ushort numColors = 0;
		byte numColorsSkip;

		pSrc = _chunkData + 6;
		ReadU16(out numColorPackets, buf, pSrc);
		pSrc += 2;

		while (numColorPackets-- != 0)
		{
			numColorsSkip = (byte)(buf[pSrc++] + numColors);
			numColors = buf[pSrc++];
			if (numColors == 0)
			{
				numColors = 256;
			}

			for (int i = 0; i < numColors; ++i)
			{
				byte r = buf[pSrc++];
				byte g = buf[pSrc++];
				byte b = buf[pSrc++];
				_colors[i] = new Rgba(r, g, b);
			}

			SetColors(numColorsSkip, numColors);

			if (numColorPackets >= 1)
			{
				++numColors;
			}
		}
	}

	private void FliSS2()
	{
		var buf = _fileBuf!;
		var pixels = _mainScreen!.Pixels;
		int pitch = Pitch;
		int pSrc, pDst, pTmpDst;
		sbyte countData;
		byte columSkip, fill1, fill2;
		ushort lines;
		short count;
		bool setLastByte = false;
		byte lastByte = 0;

		pSrc = _chunkData + 6;
		pDst = _offset;
		ReadU16(out lines, buf, pSrc);

		pSrc += 2;

		while (lines-- != 0)
		{
			ReadS16(out count, buf, pSrc);
			pSrc += 2;

			if ((count & MASK) == SKIP_LINES)
			{
				pDst += (-count) * pitch;
				++lines;
				continue;
			}

			else if ((count & MASK) == LAST_PIXEL)
			{
				setLastByte = true;
				lastByte = (byte)(count & 0x00FF);
				ReadS16(out count, buf, pSrc);
				pSrc += 2;
			}

			if ((count & MASK) == PACKETS_COUNT)
			{
				pTmpDst = pDst;
				while (count-- != 0)
				{
					columSkip = buf[pSrc++];
					pTmpDst += columSkip;
					countData = (sbyte)buf[pSrc++];

					if (countData > 0)
					{
						buf.AsSpan(pSrc, 2 * countData).CopyTo(pixels[pTmpDst..]);
						pTmpDst += 2 * countData;
						pSrc += 2 * countData;
					}
					else
					{
						if (countData < 0)
						{
							countData = (sbyte)-countData;

							fill1 = buf[pSrc++];
							fill2 = buf[pSrc++];
							while (countData-- != 0)
							{
								pixels[pTmpDst++] = fill1;
								pixels[pTmpDst++] = fill2;
							}
						}
					}
				}

				if (setLastByte)
				{
					setLastByte = false;
					pixels[pDst + pitch - 1] = lastByte;
				}
				pDst += pitch;
			}
		}
	}

	private void FliBRun()
	{
		var buf = _fileBuf!;
		var pixelData = _mainScreen!.Pixels;
		int pitch = Pitch;
		int pSrc, pDst, pTmpDst;
		byte fill;
		sbyte countData;
		int heightCount;

		heightCount = _headerHeight;
		pSrc = _chunkData + 6; // Skip chunk header
		pDst = _offset;

		while (heightCount-- != 0)
		{
			pTmpDst = pDst;
			++pSrc; // Read and skip the packet count value

			int pixels = 0;
			while (pixels != _headerWidth)
			{
				countData = (sbyte)buf[pSrc++];
				if (countData > 0)
				{
					fill = buf[pSrc++];

					pixelData.Slice(pTmpDst, countData).Fill(fill);
					pTmpDst += countData;
					pixels += countData;
				}
				else
				{
					if (countData < 0)
					{
						countData = (sbyte)-countData;

						buf.AsSpan(pSrc, countData).CopyTo(pixelData[pTmpDst..]);
						pTmpDst += countData;
						pSrc += countData;
						pixels += countData;
					}
				}
			}
			pDst += pitch;
		}
	}

	private void FliLC()
	{
		var buf = _fileBuf!;
		var pixels = _mainScreen!.Pixels;
		int pitch = Pitch;
		int pSrc, pDst, pTmpDst;
		sbyte countData;
		byte countSkip;
		byte fill;
		ushort lines, tmp;
		int packetsCount;

		pSrc = _chunkData + 6;
		pDst = _offset;

		ReadU16(out tmp, buf, pSrc);
		pSrc += 2;
		pDst += tmp * pitch;
		ReadU16(out lines, buf, pSrc);
		pSrc += 2;

		while (lines-- != 0)
		{
			pTmpDst = pDst;
			packetsCount = buf[pSrc++];

			while (packetsCount-- != 0)
			{
				countSkip = buf[pSrc++];
				pTmpDst += countSkip;
				countData = (sbyte)buf[pSrc++];
				if (countData > 0)
				{
					while (countData-- != 0)
					{
						pixels[pTmpDst++] = buf[pSrc++];
					}
				}
				else
				{
					if (countData < 0)
					{
						countData = (sbyte)-countData;

						fill = buf[pSrc++];
						while (countData-- != 0)
						{
							pixels[pTmpDst++] = fill;
						}
					}
				}
			}
			pDst += pitch;
		}
	}

	private void Color64()
	{
		var buf = _fileBuf!;
		int pSrc;
		ushort NumColors, NumColorPackets;
		byte NumColorsSkip;

		pSrc = _chunkData + 6;
		ReadU16(out NumColorPackets, buf, pSrc);
		pSrc += 2;

		while (NumColorPackets-- != 0)
		{
			NumColorsSkip = buf[pSrc++];
			NumColors = buf[pSrc++];

			if (NumColors == 0)
			{
				NumColors = 256;
			}

			for (int i = 0; i < NumColors; ++i)
			{
				byte r = (byte)(buf[pSrc++] << 2);
				byte g = (byte)(buf[pSrc++] << 2);
				byte b = (byte)(buf[pSrc++] << 2);
				_colors[i] = new Rgba(r, g, b);
			}

			SetColors(NumColorsSkip, NumColors);
		}
	}

	private void FliCopy()
	{
		var buf = _fileBuf!;
		var pixels = _mainScreen!.Pixels;
		int pitch = Pitch;
		int pSrc, pDst;
		int Lines = _screenHeight;
		pSrc = _chunkData + 6;
		pDst = _offset;

		while (Lines-- != 0)
		{
			buf.AsSpan(pSrc, _screenWidth).CopyTo(pixels[pDst..]);
			pSrc += _screenWidth;
			pDst += pitch;
		}
	}

	private void Black()
	{
		var pixels = _mainScreen!.Pixels;
		int pitch = Pitch;
		int pDst;
		int Lines = _screenHeight;
		pDst = _offset;

		while (Lines-- > 0)
		{
			// C++ clears _screenHeight bytes per line (not the width); kept as in the original
			pixels.Slice(pDst, _screenHeight).Clear();
			pDst += pitch;
		}
	}

	/// <summary>Music hook of the mixer: plays the decoded samples (runs on the audio thread).</summary>
	private static void AudioCallback(object? userData, Span<byte> stream, int len)
	{
		AudioData audio = (AudioData)userData!;

		AudioBuffer playBuff = audio.playingBuffer!;

		int pos = 0;
		while (len > 0)
		{
			if (playBuff.sampleCount > 0)
			{
				int bytesToCopy = Math.Min(len, playBuff.sampleCount * 2);
				MemoryMarshal.AsBytes(playBuff.samples.AsSpan(playBuff.currSamplePos)).Slice(0, bytesToCopy).CopyTo(stream[pos..]);
				pos += bytesToCopy;

				playBuff.currSamplePos += bytesToCopy / 2;
				playBuff.sampleCount -= bytesToCopy / 2;
				len -= bytesToCopy;

				System.Diagnostics.Debug.Assert(playBuff.sampleCount >= 0);
			}

			if (len > 0)
			{
				/* Need to swap buffers */
				playBuff.currSamplePos = 0;
				lock (audio.sharedLock!)
				{
					AudioBuffer tempBuff = playBuff;
					audio.playingBuffer = playBuff = audio.loadingBuffer!;
					audio.loadingBuffer = tempBuff;
				}

				if (playBuff.sampleCount == 0)
					break;
			}
		}
	}

	private void InitAudio(ushort format, byte channels)
	{
		_videoDelay = 1000 / (_audioData.sampleRate / (int)_audioFrameSize);
		if (_useInternalAudio)
		{
			if (!Options.mute)
			{
				if (Mixer.OpenAudio(_audioData.sampleRate, format, channels, (int)_audioFrameSize * 2) != 0)
				{
					Logger.Error(Mixer.GetError());
					Logger.Warning("Failed to init cutscene audio");
					Options.mute = true;
				}
			}

			/* Start runnable */
			_audioData.sharedLock = new object();

			_audioData.loadingBuffer = new AudioBuffer
			{
				currSamplePos = 0,
				sampleCount = 0,
				samples = new short[_audioFrameSize],
				sampleBufSize = (int)_audioFrameSize * 2,
			};

			_audioData.playingBuffer = new AudioBuffer
			{
				currSamplePos = 0,
				sampleCount = 0,
				samples = new short[_audioFrameSize],
				sampleBufSize = (int)_audioFrameSize * 2,
			};

			if (!Options.mute)
			{
				Mixer.HookMusic(AudioCallback, _audioData);
			}
		}
	}

	private void DeInitAudio()
	{
		if (_game != null)
		{
			if (!Options.mute)
			{
				Mixer.HookMusic(null, null);
				Mixer.CloseAudio();
				_game.InitAudio();
			}
		}
		else
		{
			_audioData.sharedLock = null;
		}

		_audioData.loadingBuffer = null;
		_audioData.playingBuffer = null;
	}

	/// <summary>Stop FLC Player</summary>
	public void Stop()
	{
		_playingState = PlayingState.FINISHED;
	}

	private bool IsEndOfFile(int pos) => pos == (int)_fileSize; // should be Sint64, but let's assume the videos won't be 2gb

	/// <summary>Number of video frames played so far.</summary>
	public int FrameCount => _frameCount;

	public void SetHeaderSpeed(int speed)
	{
		_headerSpeed = (ushort)speed;
	}

	public bool WasSkipped() => _playingState == PlayingState.SKIPPED;

	/// <summary>Starts waiting for the next frame (the first part of C++ waitForNextFrame()).</summary>
	private void BeginWaitForNextFrame(uint delay)
	{
		int currentTick = (int)SDLTimer.SDL_GetTicks();
		if (s_oldTick == 0)
		{
			s_oldTick = (uint)currentTick;
			_newTick = (int)s_oldTick;
		}
		else
			_newTick = (int)(s_oldTick + delay);
	}

	/// <summary>
	/// Waits for the next frame without blocking (the loop of C++ waitForNextFrame()): while it is too early,
	/// decodes audio ahead (as C++ does while waiting) and returns false.
	/// </summary>
	private bool WaitForNextFrame()
	{
		int currentTick = (int)SDLTimer.SDL_GetTicks();
		if (currentTick < _newTick)
		{
			if (_hasAudio)
			{
				while ((_newTick - currentTick) > 10 && !IsEndOfFile(_audioFrameData))
				{
					DecodeAudio(1);
					currentTick = (int)SDLTimer.SDL_GetTicks();
				}
			}
			return false;
		}
		s_oldTick = SDLTimer.SDL_GetTicks();
		return true;
	}

	/// <summary>Resets the frame clock shared by all players (for tests that replace <see cref="SDLTimer.Source"/>).</summary>
	internal static void ResetFrameClock() => s_oldTick = 0;

	private static void ReadU16(out ushort dst, byte[] src, int pos) => dst = (ushort)((src[pos + 1] << 8) | src[pos]);

	private static void ReadU32(out uint dst, byte[] src, int pos) => dst = (uint)((src[pos + 3] << 24) | (src[pos + 2] << 16) | (src[pos + 1] << 8) | src[pos]);

	/// <remarks>
	/// As in C++, both bytes are read as signed chars, so a low byte of 0x80 or more sign-extends over the high
	/// byte (e.g. bytes 80 00 give -128, not 128).
	/// </remarks>
#pragma warning disable CS0675 // the sign extension is the point (faithful to C++)
	private static void ReadS16(out short dst, byte[] src, int pos) => dst = (short)(((sbyte)src[pos + 1] << 8) | (sbyte)src[pos]);
#pragma warning restore CS0675
}
