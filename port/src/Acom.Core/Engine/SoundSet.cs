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
// Ported from: src/Engine/SoundSet.h, src/Engine/SoundSet.cpp

using System.Buffers.Binary;
using System.Diagnostics;

namespace Acom;

/// <summary>
/// Container of a set of sounds.
/// Used to manage file sets that contain a pack of sounds inside.
/// </summary>
public class SoundSet
{
	private readonly SortedDictionary<int, Sound> _sounds = [];
	private int _sharedSounds;

	/// <summary>Sets up a new empty sound set.</summary>
	public SoundSet()
	{
		_sharedSounds = int.MaxValue;
	}

	/// <summary>Converts a 8Khz sample to 11Khz.</summary>
	/// <param name="oldsound">Original sample buffer.</param>
	/// <param name="newsound">Converted sample buffer.</param>
	/// <returns>Converted buffer size.</returns>
	private static int ConvertSampleRate(ReadOnlySpan<byte> oldsound, Span<byte> newsound)
	{
		const uint step16 = (8000 << 16) / 11025;
		int newsize = 0;
		// The C++ position is 32-bit and wraps around for samples of 64 KiB and more (endless loop, buffer overflow);
		// a 64-bit position gives the same result for shorter samples and converts longer ones correctly.
		for (ulong offset16 = 0; (offset16 >> 16) < (ulong)oldsound.Length && newsize < newsound.Length; offset16 += step16, ++newsize)
		{
			newsound[newsize] = oldsound[(int)(offset16 >> 16)];
		}
		return newsize;
	}

	private static readonly byte[] header =
	[
		(byte)'R', (byte)'I', (byte)'F', (byte)'F', 0x00, 0x00, 0x00, 0x00, (byte)'W', (byte)'A', (byte)'V', (byte)'E',
		(byte)'f', (byte)'m', (byte)'t', (byte)' ', 0x10, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00,
		0x11, 0x2b, 0x00, 0x00, 0x11, 0x2b, 0x00, 0x00, 0x01, 0x00, 0x08, 0x00,
		(byte)'d', (byte)'a', (byte)'t', (byte)'a', 0x00, 0x00, 0x00, 0x00,
	];

	/// <summary>Write out a WAV (8-bit mono 11025 Hz). Resample if needed.</summary>
	/// <param name="sound">Sound data.</param>
	/// <param name="resample">If resampling (8 kHz to 11 kHz) is needed.</param>
	private static byte[] WriteWAV(ReadOnlySpan<byte> sound, bool resample)
	{
		int newsize = sound.Length;
		byte[] data;
		if (resample)
		{
			var newsound = new byte[2 * sound.Length];
			newsize = ConvertSampleRate(sound, newsound);
			data = newsound;
		}
		else
		{
			data = sound.ToArray();
		}
		var dest = new byte[header.Length + newsize];
		header.CopyTo(dest, 0);
		data.AsSpan(0, newsize).CopyTo(dest.AsSpan(header.Length));

		// update the header
		BinaryPrimitives.WriteInt32LittleEndian(dest.AsSpan(4), newsize + 36);  // write WAVE chunk size
		BinaryPrimitives.WriteInt32LittleEndian(dest.AsSpan(40), newsize);      // write data subchunk size
		return dest;
	}

	/// <summary>
	/// Loads the contents of an X-Com CAT file which usually contains
	/// a set of sound files. The CAT starts with an index of the offset
	/// and size of every file contained within. Each file consists of a
	/// filename followed by its contents.
	/// </summary>
	/// <param name="sndFile">The CAT set.</param>
	public void LoadCat(CatFile sndFile)
	{
		ArgumentNullException.ThrowIfNull(sndFile);
		for (int i = 0; i < sndFile.Size; ++i)
		{
			LoadCatByIndex(sndFile, i);
		}
	}

	/// <summary>Returns a particular wave from the sound set.</summary>
	/// <param name="i">Sound number in the set.</param>
	/// <returns>The respective sound, null if there is none.</returns>
	public Sound? GetSound(int i)
	{
		return _sounds.TryGetValue(i, out var sound) ? sound : null;
	}

	/// <summary>Creates and returns a particular wave in the sound set.</summary>
	/// <param name="i">Sound number in the set.</param>
	/// <returns>The new sound.</returns>
	public Sound AddSound(int i)
	{
		Debug.Assert(i >= 0, "Negative indexes are not supported in SoundSet");
		var sound = new Sound();
		_sounds[i] = sound;
		return sound;
	}

	/// <summary>Number of shared sound indexes that are accessible for all mods.</summary>
	public int MaxSharedSounds
	{
		get => _sharedSounds;
		set => _sharedSounds = value >= 0 ? value : 0;
	}

	/// <summary>The total amount of sounds currently stored in the set.</summary>
	public int TotalSounds => _sounds.Count;

	/// <summary>
	/// Loads individual contents of a sound CAT file by index.
	/// Each item consists of a name chunk followed by the sound: a WAV file, or raw samples behind a DOS header.
	/// </summary>
	/// <param name="sndFile">The CAT set.</param>
	/// <param name="index">Which index in the cat file do we load?</param>
	/// <param name="tftd">If to expect signed 8bit 11Khz instead of unsigned 6bit 8KHz in the data,
	/// and also under which ID to put the sound.</param>
	public void LoadCatByIndex(CatFile sndFile, int index, bool tftd = false)
	{
		ArgumentNullException.ThrowIfNull(sndFile);
		int set_index = tftd ? TotalSounds : index;
		var target = new Sound(); // in case everything else fails, an empty Sound.
		_sounds[set_index] = target;
		var item = sndFile.GetItem(index).Span;
		if (index < 0 || index >= sndFile.Size)
		{
			Logger.Verbose($"SoundSet::loadCatByIndex({sndFile.FileName}, {index}): got NULL.");
			return;
		}
		// skip "name" (SDL_ReadU8 + seek, both clamped to the item)
		int namesize = item.Length > 0 ? item[0] : 0;
		int start = item.Length > 0 ? Math.Min(1 + namesize, item.Length) : 0;
		// NB: the original code after ce548c29d5742e26a442a44ef2a5fcce3f80dace
		// did not adjust the size for the namesize byte when skipping the name
		// and thus submitted one trailing byte of garbage.
		// The original code before that commit did not adjust the size at all
		// when skipping the name and thus submitted at least one trailing byte of garbage.
		// v1.4 sounds do miss namesize+1 bytes as they are in the catfile -
		// comparing what's in the WAV header to cat item size without name.

		byte[] sound = item[start..].ToArray();
		int size = sound.Length;

		// Skip short data
		if (size < 12)
		{
			Logger.Verbose($"SoundSet::loadCatByIndex({sndFile.FileName}, {index}) size={size} , skipping.");
			return;
		}

		// See if we've got RIFF header here.
		bool wav = sound[0] == 'R' && sound[1] == 'I' && sound[2] == 'F' && sound[3] == 'F'
			&& sound[8] == 'W' && sound[9] == 'A' && sound[10] == 'V' && sound[11] == 'E';

		int samples;
		int samplecount;
		bool do_resample = true;
		if (wav)
		{ // skip WAV header
			if (size < 44)
			{
				// the C++ code reads the header beyond the data here
				Logger.Verbose($"SoundSet::loadCatByIndex({sndFile.FileName}, {index}) size={size} , skipping.");
				return;
			}
			int expected_size = BinaryPrimitives.ReadInt32LittleEndian(sound.AsSpan(0x04)) + 8;
			int delta = size - expected_size;
			// fix the header if we miss some data.
			if (delta < 0)
			{
				BinaryPrimitives.WriteInt32LittleEndian(sound.AsSpan(0x04), BinaryPrimitives.ReadInt32LittleEndian(sound.AsSpan(0x04)) + delta); // WAVE chunk size
				BinaryPrimitives.WriteInt32LittleEndian(sound.AsSpan(0x28), BinaryPrimitives.ReadInt32LittleEndian(sound.AsSpan(0x28)) + delta); // data chunk size
			}
			int samplerate = BinaryPrimitives.ReadInt32LittleEndian(sound.AsSpan(0x18));
			do_resample = samplerate < 11025;
			samples = 44;
			samplecount = size - 44;
		}
		else
		{ // skip DOS header
			// UFO2000 style: samples = sound + 6, samplecount = size - 6
			// OpenXcom style
			samples = 5;
			samplecount = size - 6;

			// scale to 8 bits (UFO) or get rid of signedness (TFTD)
			for (int n = 0; n < samplecount; ++n)
			{
				int sample = sound[samples + n];
				sound[samples + n] = (byte)(tftd ? sample + 128 : sample * 4);
			}
		}

		byte[] dest;
		if (do_resample)
		{
			dest = WriteWAV(sound.AsSpan(samples, samplecount), !tftd);
		}
		else
		{ // nothing to do.
			dest = sound;
		}
		target.Load(new MemoryStream(dest, false));
	}
}
