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
// Ported from: src/Engine/Adlib/adlplayer.h, src/Engine/Adlib/adlplayer.cpp

namespace Acom;

/// <summary>
/// X-COM Adlib Player by Volutar: player functions for midi-like adlib music.
/// Drives two emulated YM3812 chips (<see cref="opl"/>); the second one plays a slightly
/// transposed copy of every note for a harmonic (chorus) effect.
/// </summary>
/// <remarks>
/// Like the C code, all player state is global. Pointers into the music data are offsets into
/// <see cref="s_data"/> (-1 = null). Reads outside the data are undefined behaviour in C++; here
/// an opcode read past the end yields 0xFF (end of track) and any other read 0, so broken data
/// stops the track instead of crashing the audio thread. Out-of-range note numbers read 0 from the
/// note tables, out-of-range chorus instruments use spare instrument slots.
/// </remarks>
internal static class AdlPlayer
{
	private static readonly short[] adl_gv_freq_table = // 9 * 12 -- notes frequency
	[
		0x0B5, 0x0C0, 0x0CC, 0x0D8, 0x0E5, 0x0F2, 0x101, 0x110, 0x120, 0x131, 0x143, 0x157,
		0x16B, 0x181, 0x198, 0x1B0, 0x1CA, 0x1E5, 0x202, 0x220, 0x241, 0x263, 0x287, 0x2AE,
		0x16B, 0x181, 0x198, 0x1B0, 0x1CA, 0x1E5, 0x202, 0x220, 0x241, 0x263, 0x287, 0x2AE,
		0x16B, 0x181, 0x198, 0x1B0, 0x1CA, 0x1E5, 0x202, 0x220, 0x241, 0x263, 0x287, 0x2AE,
		0x16B, 0x181, 0x198, 0x1B0, 0x1CA, 0x1E5, 0x202, 0x220, 0x241, 0x263, 0x287, 0x2AE,
		0x16B, 0x181, 0x198, 0x1B0, 0x1CA, 0x1E5, 0x202, 0x220, 0x241, 0x263, 0x287, 0x2AE,
		0x16B, 0x181, 0x198, 0x1B0, 0x1CA, 0x1E5, 0x202, 0x220, 0x241, 0x263, 0x287, 0x2AE,
		0x16B, 0x181, 0x198, 0x1B0, 0x1CA, 0x1E5, 0x202, 0x220, 0x241, 0x263, 0x287, 0x2AE,
		0x16B, 0x181, 0x198, 0x1B0, 0x1CA, 0x1E5, 0x202, 0x220, 0x241, 0x263, 0x287, 0x2AE,
	];

	private static readonly sbyte[] adl_gv_octave_table = // 9 * 12 -- octaves of notes
	[
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
		2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
		3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
		4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
		5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
		6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
		7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
	];

	private static readonly sbyte[] adl_gv_detune_table = // 9 * 12 -- pitch bend scale values depending on note
	[
		3, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5,
		3, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5,
		3, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5,
		3, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5,
		3, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5,
		3, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5,
		3, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5,
		3, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5,
		3, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5,
	];

	private static readonly sbyte[] adl_gv_instr_order = [0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 9];

	private static readonly sbyte[] adl_gv_operators1 = [0, 1, 2, 8, 9, 10, 16, 17, 18, 24, 25, 26];

	private static readonly sbyte[] slot_array =
	[
		 0, 2, 4, 1, 3, 5,-1,-1,
		 6, 8,10, 7, 9,11,-1,-1,
		12,14,16,13,15,17,-1,-1,
		18,20,22,19,21,23,-1,-1
	];

	private struct struc_adlib_channels
	{
		public byte cur_note;
		public byte cur_instrument;
		public byte cur_sample;
		public byte cur_freq;
		public byte hifreq;
		public byte cur_volume;
		public int duration;
#pragma warning disable CS0649 // unused, kept from the C++ struct
		public int pan;
#pragma warning restore CS0649
	}

	private struct struc_instruments
	{
		public byte sample_id;
		public byte prev_cmd;
		public byte volume;
		public short cur_pitchbend;
		public int cur_delay;
		public int cur_address;    // pointer into s_data, -1 = null
		public int start_address;  // pointer into s_data, -1 = null
		public int return_address; // pointer into s_data, -1 = null
	}

	// sample layout (struc_sample, 11 bytes used of 24)
	private const int reg20_op1 = 0, reg20_op2 = 1, reg40_op1 = 2, reg40_op2 = 3, reg60_op1 = 4, reg60_op2 = 5;
	private const int reg80_op1 = 6, reg80_op2 = 7, regE0_op1 = 8, regE0_op2 = 9, regC0 = 10;

	/// <summary>Number of instruments the format knows.</summary>
	private const int INSTRUMENTS = 16;

	private static readonly struc_adlib_channels[] adlib_channels = new struc_adlib_channels[12];
	// 16 instruments like C++, plus spare slots for out-of-range chorus instrument numbers (UB in C++)
	private static readonly struc_instruments[] instruments = NewInstruments(256);

	private static int adl_gv_master_music_volume = 127;
	private static int adl_gv_tmp_music_volume = 127;
	private static bool adl_gv_want_fade;
	private static bool adl_gv_music_playing;
	private static int adl_gv_tempo = 120;
	private static int adl_gv_tempo_run = 60;
	private static int adl_gv_tempo_inc = 70;
	private static int adl_gv_samples_addr = -1;
	private static readonly int[] adl_gv_subtracks = NewPointers(128);
	private static uint adl_gv_instruments_count;
	private static uint adl_gv_subtracks_count;
	private static int adl_gv_polyphony_level;
	private static readonly byte[] adl_gv_chorus_instruments = new byte[INSTRUMENTS];
	private static int adl_gv_FORMAT; // 0 = without title, 1=with title
	private static readonly byte[] iFMReg = new byte[256];
	private static readonly byte[] iTweakedFMReg = new byte[256];
	private static readonly byte[] iCurrentTweakedBlock = new byte[12];
	private static readonly byte[] iCurrentFNum = new byte[12];
	private static readonly struc_instruments[][] saved_instruments = [NewInstruments(INSTRUMENTS), NewInstruments(INSTRUMENTS)];

	/// <summary>The music data (C++: the pointer passed to func_setup_music).</summary>
	private static byte[] s_data = [];
	private static int s_length;

	/// <summary>The two emulated chips (created by <see cref="AdlibMusic"/>).</summary>
	internal static readonly FM_OPL?[] opl = [null, null];

	private const int NEWBLOCK_LIMIT = 32;
	private const double FREQ_OFFSET = 128.0; //128.0//96.0

	private static struc_instruments[] NewInstruments(int n)
	{
		var a = new struc_instruments[n];
		for (int i = 0; i < n; ++i)
		{
			a[i].cur_address = a[i].start_address = a[i].return_address = -1;
		}
		return a;
	}

	private static int[] NewPointers(int n)
	{
		var a = new int[n];
		Array.Fill(a, -1);
		return a;
	}

	/// <summary>Reads a data byte; 0 outside the data.</summary>
	private static byte Peek(int p) => p >= 0 && p < s_data.Length ? s_data[p] : (byte)0;

	/// <summary>Reads a 2-byte little endian value from the data.</summary>
	private static ushort peek_u16(int p) => (ushort)(Peek(p) | (Peek(p + 1) << 8));

	private static int FreqTable(int note) => (uint)note < (uint)adl_gv_freq_table.Length ? adl_gv_freq_table[note] : 0;
	private static int OctaveTable(int note) => (uint)note < (uint)adl_gv_octave_table.Length ? adl_gv_octave_table[note] : 0;
	private static int DetuneTable(int note) => (uint)note < (uint)adl_gv_detune_table.Length ? adl_gv_detune_table[note] : 0;

	/// <summary>Transpose the other channel to produce the harmonic effect.</summary>
	private static void Transpose(int reg, int val, out int val2, ref int reg3, ref int val3)
	{
		int iChannel = -1;
		int iRegister = reg; // temp
		int iValue = val; // temp
		if ((iRegister >> 4 == 0xA) || (iRegister >> 4 == 0xB)) iChannel = iRegister & 0x0F;

		// Remember the FM state, so that the harmonic effect can access
		// previously assigned register values.
		iFMReg[iRegister] = (byte)iValue;

		if (iChannel >= 0 && iChannel < 12)
		{
			byte iBlock = (byte)((iFMReg[0xB0 + iChannel] >> 2) & 0x07);
			ushort iFNum = (ushort)(((iFMReg[0xB0 + iChannel] & 0x03) << 8) | iFMReg[0xA0 + iChannel]);
			double dbOriginalFreq = 49716.0 * (double)iFNum / Math.Pow(2, 20 - iBlock);

			byte iNewBlock = iBlock;
			ushort iNewFNum;

			// Adjust the frequency and calculate the new FNum
			double calcFNum() => (dbOriginalFreq + (dbOriginalFreq / FREQ_OFFSET)) / (49716.0 / Math.Pow(2.0f, 20 - iNewBlock));
			double dbNewFNum = calcFNum();

			// Make sure it's in range for the OPL chip
			if (dbNewFNum > 1023 - NEWBLOCK_LIMIT)
			{
				// It's too high, so move up one block (octave) and recalculate

				if (iNewBlock > 6)
				{
					// Uh oh, we're already at the highest octave!
					// The best we can do here is to just play the same note out of the second OPL, so at least it shouldn't
					// sound *too* bad (hopefully it will just miss out on the nice harmonic.)
					iNewBlock = iBlock;
					iNewFNum = iFNum;
				}
				else
				{
					iNewBlock++;
					iNewFNum = (ushort)(int)calcFNum();
				}
			}
			else if (dbNewFNum < 0 + NEWBLOCK_LIMIT)
			{
				// It's too low, so move down one block (octave) and recalculate

				if (iNewBlock == 0)
				{
					// Uh oh, we're already at the lowest octave!
					iNewBlock = iBlock;
					iNewFNum = iFNum;
				}
				else
				{
					iNewBlock--;
					iNewFNum = (ushort)(int)calcFNum();
				}
			}
			else
			{
				// Original calculation is within range, use that
				iNewFNum = (ushort)(int)dbNewFNum;
			}

			// Sanity check
			if (iNewFNum > 1023)
			{
				// Uh oh, the new FNum is still out of range! (This shouldn't happen)
				iNewBlock = iBlock;
				iNewFNum = iFNum;
			}

			if ((iRegister >= 0xB0) && (iRegister <= 0xBC))
			{
				// Overwrite the supplied value with the new F-Number and Block.
				iValue = (iValue & ~0x1F) | (iNewBlock << 2) | ((iNewFNum >> 8) & 0x03);

				iCurrentTweakedBlock[iChannel] = iNewBlock; // save it so we don't have to update register 0xB0 later on
				iCurrentFNum[iChannel] = (byte)iNewFNum;

				if (iTweakedFMReg[0xA0 + iChannel] != (iNewFNum & 0xFF))
				{
					// Need to write out low bits
					byte iAdditionalReg = (byte)(0xA0 + iChannel);
					byte iAdditionalValue = (byte)(iNewFNum & 0xFF);
					reg3 = iAdditionalReg;
					val3 = iAdditionalValue;
					iTweakedFMReg[iAdditionalReg] = iAdditionalValue;
				}
			}
			else if ((iRegister >= 0xA0) && (iRegister <= 0xAC))
			{
				// Overwrite the supplied value with the new F-Number.
				iValue = iNewFNum & 0xFF;

				// See if we need to update the block number, which is stored in a different register
				byte iNewB0Value = (byte)((iFMReg[0xB0 + iChannel] & ~0x1F) | (iNewBlock << 2) | ((iNewFNum >> 8) & 0x03));
				if (
					(iNewB0Value & 0x20) != 0 && // but only update if there's a note currently playing (otherwise we can just wait
					(iTweakedFMReg[0xB0 + iChannel] != iNewB0Value)   // until the next noteon and update it then)
				)
				{
					// The note is already playing, so we need to adjust the upper bits too
					byte iAdditionalReg = (byte)(0xB0 + iChannel);
					reg3 = iAdditionalReg;
					val3 = iNewB0Value;
					iTweakedFMReg[iAdditionalReg] = iNewB0Value;
				} // else the note is not playing, the upper bits will be set when the note is next played
			} // if (register 0xB0 or 0xA0)
		} // if (a register we're interested in)

		// Now write to the original register with a possibly modified value
		val2 = iValue;
		iTweakedFMReg[iRegister] = (byte)iValue;
	}

	/// <summary>Writes a register of both chips (the second one with the harmonic transposition).</summary>
	internal static void adlib_reg(int i, int v)
	{
		if (opl[0] == null) return;
		int v2, i3, v3 = 0;
		i3 = -1;
		Transpose(i, v, out v2, ref i3, ref v3);

		Fmopl.OPLWrite(opl[0]!, 0, i);
		Fmopl.OPLWrite(opl[0]!, 1, v);
		Fmopl.OPLWrite(opl[1]!, 0, i);
		if (i >= 0x20 && i <= 0x3f) //no tremolo/vibrato
			v2 = (v2 & 0x3F);
		if (i >= 0xE0 && i <= 0xFC)
		{
			if ((slot_array[i & 0x1f] & 1) == 1) //wave form
				v2 = v2 & 0x02;
		}
		Fmopl.OPLWrite(opl[1]!, 1, v2);
		if (i3 != -1)
		{
			Fmopl.OPLWrite(opl[1]!, 0, i3);
			Fmopl.OPLWrite(opl[1]!, 1, v3);
		}
	}

	/// <summary>Initialises adlib parameters.</summary>
	private static void adlib_init()
	{
		for (int i = 1; i < 0xf5; ++i) adlib_reg(i, 0);
		adlib_reg(0x04, 0x60); //set both timers
		adlib_reg(0x04, 0x80); //reset timers
		adlib_reg(0x01, 0x20); //bit5 defines any wave form
		adlib_reg(0xa8, 0x01); //last channel freq -??
		adlib_reg(0x08, 0x40); //note mode
		adlib_reg(0xbd, 0xC0); // vibrato/AM depth
	}

	/// <summary>Sets voice amplitude for particular adlib channel.</summary>
	private static void adlib_set_amplitude(int channel, int value)
	{
		adlib_reg(0x43 + adl_gv_operators1[channel], ~(value >> 1) & 0x3f);
	}

	/// <summary>Clear channel notes and samples.</summary>
	private static void clear_channels()
	{
		for (int i = 0; i < 12; ++i)
		{
			adlib_channels[i].cur_sample = 0xff;
			adlib_channels[i].cur_note = 0;
		}
	}

	/// <summary>Resets (clears) adlib channels.</summary>
	private static void adlib_reset_channels()
	{
		clear_channels();
		for (int i = 0; i < 12; ++i)
		{
			adlib_reg(0xB0 + i, 0);
			adlib_set_amplitude(i, 0);
		}
	}

	/// <summary>Returns note frequency with pitch wheel value applied.</summary>
	private static int get_pitched_freq_instr(int note, int instrument)
	{
		int pitch = instruments[instrument].cur_pitchbend;
		if (pitch == 0)
			return FreqTable(note);
		else if (pitch > 0)
			return FreqTable(note) + DetuneTable(note) * pitch;
		else
			return FreqTable(note) + DetuneTable(note - 1) * pitch;
	}

	/// <summary>
	/// Sets pitch for particular instrument, apply for adlib channels with same instrument.
	/// !!! probably should also apply pitch for CHORUS instrument !!!
	/// </summary>
	private static void adlib_set_instrument_pitch(int instrument, int pitch)
	{
		instruments[instrument].cur_pitchbend = (short)pitch;
		for (int i = 0; i < 12; ++i) //search through active adlib channels
		{
			int note = adlib_channels[i].cur_note;
			if (note != 0 && adlib_channels[i].cur_instrument == instrument)
			{
				int freq = get_pitched_freq_instr(note, instrument);
				adlib_channels[i].cur_freq = (byte)freq;
				adlib_reg(0xA0 + i, freq & 0xff);
				int hf = ((freq >> 8) & 0x03) | (OctaveTable(note) << 2);
				adlib_channels[i].hifreq = (byte)hf;
				adlib_reg(0xB0 + i, hf | 0x20);
			}
		}
	}

	/// <summary>Gets unused adlib channel and checks if old sample is the same.</summary>
	private static int adlib_get_unused_channel(int sample_id, ref bool same_sample)
	{
		int maxchan = 0, maxdur = 0, i;

		for (i = 0; i < 12; ++i)
			++adlib_channels[i].duration;

		for (i = 0; i < 12; ++i) //12/9
		{
			if (adlib_channels[i].duration > maxdur)
			{
				maxdur = adlib_channels[i].duration;
				maxchan = i;
			}
			if (adlib_channels[i].cur_note == 0) //empty channel
			{
				maxchan = i;
				break;
			}
		}

		if (adlib_channels[maxchan].cur_sample == sample_id)
			same_sample = true;
		else
			adlib_channels[maxchan].cur_sample = (byte)sample_id;
		adlib_channels[maxchan].duration = 0;
		return maxchan;
	}

	/// <summary>Starts playing note of particular instrument.</summary>
	private static void adlib_play_note(int note, int volume, int instrument)
	{
		int cur_sample;
		int sample_id = instruments[instrument].sample_id;
		int channel;
		int ampl;
		int op1;
		bool same_sample = false;
		// (the C++ code has a disabled percussion branch for instrument 9 here)
		{
			cur_sample = adl_gv_samples_addr + sample_id * 24;
		}
		note--;
		if (volume == 0) //stop note
		{
			for (int i = 0; i < 12; ++i)
			{
				if (adlib_channels[i].cur_note == note &&
					adlib_channels[i].cur_instrument == instrument)
				{
					adlib_channels[i].cur_note = 0; //clear channel
					adlib_reg(0xB0 + i, adlib_channels[i].hifreq); //mute note
				}
			}
			return;
		}
		if (volume > 127) volume = 127;
		channel = adlib_get_unused_channel(sample_id, ref same_sample);
		adlib_channels[channel].cur_volume = (byte)volume;
		adlib_channels[channel].cur_note = (byte)note;
		adlib_channels[channel].cur_instrument = (byte)instrument;
		op1 = adl_gv_operators1[channel];
		if (!same_sample)
		{
			adlib_reg(0x20 + op1, Peek(cur_sample + reg20_op1)); //amp mod / vibrato / eg type
			adlib_reg(0x23 + op1, Peek(cur_sample + reg20_op2)); //key scaling / multiple
			ampl = Peek(cur_sample + reg40_op1);
			adlib_reg(0x40 + op1, ((~ampl) & 0x3f) | (ampl & 0xc0)); // amplitude op1
		}

		adlib_reg(0xB0 + channel, adlib_channels[channel].hifreq);  // reinit note
		adlib_reg(0x43 + op1, (~((adl_gv_tmp_music_volume * volume) >> 8)) & 0x3f); //amplitude op2

		if (!same_sample)
		{
			adlib_reg(0x60 + op1, Peek(cur_sample + reg60_op1)); //attack rate/decay rate
			adlib_reg(0x63 + op1, Peek(cur_sample + reg60_op2));
			adlib_reg(0x80 + op1, Peek(cur_sample + reg80_op1)); //sustain level / release rate
			adlib_reg(0x83 + op1, Peek(cur_sample + reg80_op2));
			adlib_reg(0xE0 + op1, Peek(cur_sample + regE0_op1)); //wave select
			adlib_reg(0xE3 + op1, Peek(cur_sample + regE0_op2));
			adlib_reg(0xC0 + channel, Peek(cur_sample + regC0) ^ 0x01); //feedback strength / connection type
		}

		int freq = get_pitched_freq_instr(note, instrument);
		adlib_channels[channel].cur_freq = (byte)freq;
		adlib_reg(0xA0 + channel, freq & 0xff);
		int hf = (freq >> 8) | (OctaveTable(note) << 2);
		adlib_channels[channel].hifreq = (byte)hf;
		adlib_reg(0xB0 + channel, hf | 0x20); //reinit note
	}

	/// <summary>Reads running-length value from data stream.</summary>
	private static int get_numseq(ref int mus_ptr)
	{
		byte c;
		int v = 0;
		do
		{
			c = Peek(mus_ptr); // 0 past the end ends the value
			v = unchecked((v << 7) + (c & 0x7f));
			mus_ptr++;
		} while ((c & 0x80) != 0);
		return v;
	}

	/// <summary>Reads an opcode; 0xFF (end of track) past the end of the data.</summary>
	private static byte ReadOpcode(ref int p)
	{
		byte c = p >= 0 && p < s_data.Length ? s_data[p] : (byte)0xff;
		p++;
		return c;
	}

	/// <summary>MAIN FUNCTION - instantly stops music.</summary>
	public static void func_mute()
	{
		adl_gv_polyphony_level = 0;
		adl_gv_music_playing = false;
		adlib_reset_channels();
	}

	/// <summary>Decrease volume until 0 with each call, and then stops music.</summary>
	private static void fade_volume_if_need()
	{
		if (!adl_gv_want_fade) return;
		if (--adl_gv_tmp_music_volume == 0)
		{
			func_mute();
			adl_gv_want_fade = false;
			adl_gv_tmp_music_volume = adl_gv_master_music_volume;
			return;
		}
		for (int i = 0; i < 12; ++i)
		{
			adlib_set_amplitude(i, (adlib_channels[i].cur_volume * adl_gv_tmp_music_volume) >> 7);
		}
	}

	/// <summary>Checks if there are unused adlib channels.</summary>
	private static bool free_channel_available()
	{
		for (int i = 0; i < 12; ++i)
		{
			if (adlib_channels[i].cur_note == 0)
				return true;
		}
		return false;
	}

	/// <summary>Main decode function - reads data from stream and decodes it accordingly.</summary>
	private static int decode_op(int instrument, ref bool another_loop)
	{
		ref struc_instruments instr1 = ref instruments[instrument];
		int music_ptr = instr1.cur_address;
		byte opcode, arg1, arg2;
		int delay = 0;

		do
		{
			opcode = ReadOpcode(ref music_ptr);
			if (opcode == 0xfe) //call for subtrack
			{
				arg1 = Peek(music_ptr++);
				instr1.return_address = music_ptr;
				music_ptr = arg1 < adl_gv_subtracks.Length ? adl_gv_subtracks[arg1] : -1;
			}
			else if (opcode == 0xfd) //return from subtrack
			{
				if (instr1.return_address == -1)
				{
					// DOUBLE RETURN - ERROR
				}
				else
				{
					music_ptr = instr1.return_address;
					instr1.return_address = -1;
				}
			}
			else if (opcode == 0xff) //finishing track
			{
				adl_gv_music_playing = false;
				delay = 0;
				break;
			}
			else if (opcode >= 0x80) //opcode
			{
				instr1.prev_cmd = opcode;
				opcode = Peek(music_ptr++); //read "repeated" opcode which goes to arg
			}

			if (opcode < 0x80) //can be not a opcode, just the repeated command
			{
				arg1 = opcode;
				opcode = instr1.prev_cmd;
				switch (opcode & 0xf0)
				{
				case 0x80: //note off
					arg2 = Peek(music_ptr++);
					adlib_play_note(arg1, 0, instrument);
					--adl_gv_polyphony_level;
					if (adl_gv_chorus_instruments[instrument] != 0)
					{
						adlib_play_note(arg1, 0, adl_gv_chorus_instruments[instrument]);
						--adl_gv_polyphony_level;
					}
					break;
				case 0x90: //note on
					arg2 = Peek(music_ptr++);
					if (arg2 == 0) //volume==0 - note off
					{
						adlib_play_note(arg1, 0, instrument);
						--adl_gv_polyphony_level;
						if (adl_gv_chorus_instruments[instrument] != 0)
						{
							adlib_play_note(arg1, 0, adl_gv_chorus_instruments[instrument]);
							--adl_gv_polyphony_level;
						}
					}
					else
					{
						int vol = (arg2 * instr1.volume) >> 7;
						if (adl_gv_chorus_instruments[instrument] != 0)
						{
							if (free_channel_available())
							{
								ref struc_instruments instr2 = ref instruments[adl_gv_chorus_instruments[instrument]];
								instr2.sample_id = instr1.sample_id;
								instr2.cur_pitchbend = (short)(instr1.cur_pitchbend - 1);
								adlib_play_note(arg1, vol, adl_gv_chorus_instruments[instrument]);
							}
							++adl_gv_polyphony_level; //increase it nevertheless, because it SHOULD play
						}
						adlib_play_note(arg1, vol, instrument);
						++adl_gv_polyphony_level;
					}
					break;
				case 0xB0: //set controller
					arg2 = Peek(music_ptr++);
					if (arg1 == 0 && arg2 != 0) //tempo change
						adl_gv_tempo = (int)(arg2 * 0.8);
					else if (arg1 == 7) //channel volume change
					{
						instr1.volume = arg2;
					}
					else if (arg1 == 0x7e) //setting up chorus (slave) instrument
						adl_gv_chorus_instruments[instrument] = (byte)(arg2 - 1);
					else if (arg1 == 0x7f) //clearing chorus (slave) instrument
						adl_gv_chorus_instruments[instrument] = 0;
					break;
				case 0xC0: //set sample
					if (arg1 == 0x7E)
					{
						another_loop = true; // LOOP
					}
					else
					{
						instr1.sample_id = arg1;
					}
					break;
				case 0xE0: //pitch bend
					instr1.cur_pitchbend = (short)(arg1 - 16); // no need???
					adlib_set_instrument_pitch(instrument, arg1 - 16);
					if (adl_gv_chorus_instruments[instrument] != 0)
						adlib_set_instrument_pitch(adl_gv_chorus_instruments[instrument], arg1 - 17);
					break;
				}
			}
			delay = get_numseq(ref music_ptr);
		} while (delay == 0);
		instr1.cur_address = music_ptr;
		return delay;
	}

	/// <summary>Music initialization function, sets up subblock array, tempo, instrument addresses and samples.</summary>
	private static void init_music_data(int music_ptr, int length)
	{
		uint i, to_add, j;
		int start = music_ptr;
		for (i = 0; i < INSTRUMENTS; ++i)
		{
			instruments[i].start_address = -1;
		}
		adl_gv_subtracks_count = 0;

		i = Peek(music_ptr);
		if (i > 56) adl_gv_FORMAT = 0; //switch to old
		else adl_gv_FORMAT = 1;
		if (adl_gv_FORMAT == 1) music_ptr += Peek(music_ptr) + 1; //skip name
		adl_gv_tempo = Peek(music_ptr++);
		adl_gv_samples_addr = music_ptr + 1; //samples
		music_ptr += (Peek(music_ptr) * 24) + 1; //moving to next section - subtracks
		adl_gv_subtracks_count = Peek(music_ptr++);
		for (i = 0; i < adl_gv_subtracks_count; ++i)
		{
			to_add = peek_u16(music_ptr); //reading 16bit length
			if (i < adl_gv_subtracks.Length) // C++ writes past the array for more than 128 subtracks
				adl_gv_subtracks[i] = music_ptr + 4; //store subtrack pointers
			music_ptr += (int)to_add;
		}
		adl_gv_instruments_count = Peek(music_ptr++);
		for (i = 0; i < adl_gv_instruments_count; ++i)
		{
			to_add = peek_u16(music_ptr); //reading 16bit length
			if (adl_gv_FORMAT == 1)
			{
				j = Peek(music_ptr + 4);
				if (j > 15) j = 15;
				instruments[j].start_address = music_ptr + 5;
			}
			else
			if (adl_gv_FORMAT == 0)
			{
				j = i;
				if (j < INSTRUMENTS) // C++ writes past the array for more than 16 instruments
					instruments[j].start_address = music_ptr + 4; //old format, without title
			}
			music_ptr += (int)to_add;
			if (music_ptr - start >= length)
			{
				// DATA ERROR: track has gone past the end of the data
				break;
			}
		}
	}

	/// <summary>Initialize music tracks, prepare for playing.</summary>
	private static void init_music()
	{
		for (int i = 0; i < INSTRUMENTS; ++i)
		{
			instruments[i].cur_pitchbend = 0;
			adl_gv_chorus_instruments[i] = 0;
			if (instruments[i].start_address != -1)
			{
				instruments[i].cur_address = instruments[i].start_address;
				instruments[i].cur_delay = get_numseq(ref instruments[i].cur_address);
			}
			else
			{
				instruments[i].cur_address = -1;
				instruments[i].cur_delay = 0;
			}
		}
	}

	/// <summary>Save music state.</summary>
	public static void func_save_music_state(int i)
	{
		Array.Copy(instruments, saved_instruments[i], INSTRUMENTS);
	}

	/// <summary>Load music state.</summary>
	public static void func_load_music_state(int i)
	{
		adlib_reset_channels();
		Array.Copy(saved_instruments[i], instruments, INSTRUMENTS);
	}

	/// <summary>MAIN FUNCTION - runs 1 tick of music according tempo.</summary>
	public static void func_play_tick()
	{
		bool another_loop;

		if (!adl_gv_music_playing) return;
		fade_volume_if_need();
		adl_gv_tempo_run -= adl_gv_tempo;
		if (adl_gv_tempo_run > 0) return;
		adl_gv_tempo_run += adl_gv_tempo_inc;

		do
		{
			another_loop = false;
			for (int i = 0; i < INSTRUMENTS; ++i)
			{
				int instr = adl_gv_instr_order[i];
				if (instruments[instr].cur_address == -1) continue;
				if (instruments[instr].cur_delay == 0)
				{
					instruments[instr].cur_delay = decode_op(instr, ref another_loop);
					if (!adl_gv_music_playing) break;
				}
				--instruments[instr].cur_delay;
			}
			if (!another_loop && adl_gv_music_playing) break;
			init_music();
			clear_channels();
		} while (another_loop);
	}

	/// <summary>MAIN FUNCTION - setup music for playing.</summary>
	/// <param name="music_ptr">The music data (kept by reference while playing).</param>
	/// <param name="length">Length of the data.</param>
	public static void func_setup_music(byte[] music_ptr, int length)
	{
		adl_gv_music_playing = false;
		func_mute();
		adl_gv_polyphony_level = 0;
		adl_gv_want_fade = false;
		adl_gv_tmp_music_volume = adl_gv_master_music_volume;
		s_data = music_ptr ?? [];
		s_length = length;
		init_music_data(0, s_length);
		init_music();
		adlib_init();
		adlib_reset_channels();
		adl_gv_tempo = (int)(adl_gv_tempo * 0.4);
		adl_gv_tempo_run = adl_gv_tempo;
		adl_gv_music_playing = true;
	}

	/// <summary>MAIN FUNCTION - initialize fade procedure.</summary>
	public static void func_fade()
	{
		if (adl_gv_tmp_music_volume == 0)
		{
			func_mute();
		}
		else
		{
			adl_gv_want_fade = true;
		}
	}

	/// <summary>MAIN FUNCTION - check if music finished.</summary>
	public static bool func_is_music_playing() => adl_gv_music_playing;

	public static void func_set_music_tempo(int value)
	{
		adl_gv_tempo_inc = value;
	}

	public static void func_set_music_volume(int value)
	{
		adl_gv_master_music_volume = value;
		adl_gv_tmp_music_volume = adl_gv_master_music_volume;

		for (int i = 0; i < 12; ++i)
		{
			adlib_set_amplitude(i, (adlib_channels[i].cur_volume * adl_gv_tmp_music_volume) >> 7);
		}
	}

	public static int func_get_polyphony() => adl_gv_polyphony_level;

	/// <summary>Destroys both chips (C++ ~AdlibMusic); the next <see cref="AdlibMusic"/> creates new ones.</summary>
	internal static void ResetChips()
	{
		for (int i = 0; i < opl.Length; ++i)
		{
			Fmopl.OPLDestroy(opl[i]);
			opl[i] = null;
		}
	}

	/// <summary>Tests: destroys the chips and puts all player state back to its initial values (a fresh process in C++).</summary>
	internal static void ResetForTest()
	{
		ResetChips();
		Array.Clear(adlib_channels);
		var fresh = NewInstruments(1);
		Array.Fill(instruments, fresh[0]);
		Array.Fill(saved_instruments[0], fresh[0]);
		Array.Fill(saved_instruments[1], fresh[0]);
		adl_gv_master_music_volume = 127;
		adl_gv_tmp_music_volume = 127;
		adl_gv_want_fade = false;
		adl_gv_music_playing = false;
		adl_gv_tempo = 120;
		adl_gv_tempo_run = 60;
		adl_gv_tempo_inc = 70;
		adl_gv_samples_addr = -1;
		Array.Fill(adl_gv_subtracks, -1);
		adl_gv_instruments_count = 0;
		adl_gv_subtracks_count = 0;
		adl_gv_polyphony_level = 0;
		Array.Clear(adl_gv_chorus_instruments);
		adl_gv_FORMAT = 0;
		Array.Clear(iFMReg);
		Array.Clear(iTweakedFMReg);
		Array.Clear(iCurrentTweakedBlock);
		Array.Clear(iCurrentFNum);
		s_data = [];
		s_length = 0;
	}
}
