/*
 * fmopl.c -- software implementation of FM sound generator
 * Copyright (C) 1999,2000 Tatsuyuki Satoh , MultiArcadeMachineEmurator development
 * Version 0.37a
 * Modified version for X-COM (increased polyphony) by Volutar
 * C# port: Copyright 2026 ACOM contributors.
 *
 * This version of fmopl.c is a fork of the MAME one, relicensed under the LGPL.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */
// Ported from: src/Engine/Adlib/fmopl.h, src/Engine/Adlib/fmopl.cpp

namespace Acom;

/// <summary>Timer callback of an OPL chip (channel, interval in seconds).</summary>
internal delegate void OPL_TIMERHANDLER(int channel, double interval_Sec);

/// <summary>IRQ callback of an OPL chip.</summary>
internal delegate void OPL_IRQHANDLER(int param, int irq);

/// <summary>Stream update callback of an OPL chip.</summary>
internal delegate void OPL_UPDATEHANDLER(int param, int min_interval_us);

/// <summary>One operator slot of an OPL channel.</summary>
/// <remarks>Rate pointers of the C code are an array plus an offset; the wave table pointer is an offset into the sine table.</remarks>
internal sealed class OPL_SLOT
{
	public int TL;      // total level     :TL << 8
	public int TLL;     // adjusted now TL
	public byte KSR;    // key scale rate  :(shift down bit)
	public int[] AR = Fmopl.RATE_0; // attack rate :&AR_TABLE[AR<<2]
	public int ARo;
	public int[] DR = Fmopl.RATE_0; // decay rate  :&DR_TALBE[DR<<2]
	public int DRo;
	public int SL;      // sustin level    :SL_TALBE[SL]
	public int[] RR = Fmopl.RATE_0; // release rate :&DR_TABLE[RR<<2]
	public int RRo;
	public byte ksl;    // keyscale level  :(shift down bits)
	public byte ksr;    // key scale rate  :kcode>>KSR
	public uint mul;    // multiple        :ML_TABLE[ML]
	public uint Cnt;    // frequency count
	public uint Incr;   // frequency step
	// envelope generator state
	public byte eg_typ; // envelope type flag
	public byte evm;    // envelope phase
	public int evc;     // envelope counter
	public int eve;     // envelope counter end point
	public int evs;     // envelope counter step
	public int evsa;    // envelope step for AR :AR[ksr]
	public int evsd;    // envelope step for DR :DR[ksr]
	public int evsr;    // envelope step for RR :RR[ksr]
	// LFO
	public byte ams;    // ams flag
	public byte vib;    // vibrate flag
	// wave selector
	public int wavetable; // offset into SIN_TABLE
}

/// <summary>One OPL channel (two slots).</summary>
internal sealed class OPL_CH
{
	public readonly OPL_SLOT[] SLOT = [new OPL_SLOT(), new OPL_SLOT()];
	public byte CON;          // connection type
	public byte FB;           // feed back       :(shift down bit)
	public bool connect1Carrier; // slot1 output pointer: true = carrier (outd), false = feedback2
	public readonly int[] op1_out = new int[2]; // slot1 output for selfeedback
	// phase generator state
	public uint block_fnum;   // block+fnum
	public byte kcode;        // key code        : KeyScaleCode
	public uint fc;           // Freq. Increment base
	public uint ksl_base;     // KeyScaleLevel Base step
	public byte keyon;        // key on/off flag
}

/// <summary>OPL chip state.</summary>
internal sealed class FM_OPL
{
	public byte type;          // chip type
	public int clock;          // master clock  (Hz)
	public int rate;           // sampling rate (Hz)
	public double freqbase;    // frequency base
	public double TimerBase;   // Timer base time (==sampling time)
	public byte address;       // address register
	public byte status;        // status flag
	public byte statusmask;    // status mask
	public uint mode;          // Reg.08 : CSM , notesel,etc.
	// Timer
	public readonly int[] T = new int[2];    // timer counter
	public readonly byte[] st = new byte[2]; // timer enable
	// FM channel slots
	public OPL_CH[] P_CH = [];
	public int max_ch;         // maximum channel
	// Rythm sention
	public byte rythm;         // Rythm mode , key flag
	// time tables
	// One element more than in C: rate 15 with key scale 15 reads AR_TABLE[75] / DR_TABLE[75], which in the
	// C struct are the following DR_TABLE[0] / FN_TABLE[0] (both always 0).
	public readonly int[] AR_TABLE = new int[75 + 1];    // attack rate tables
	public readonly int[] DR_TABLE = new int[75 + 1];    // decay rate tables
	public readonly uint[] FN_TABLE = new uint[1024]; // fnumber -> increment counter
	// LFO
	public int ams_table;      // offset into AMS_TABLE
	public int vib_table;      // offset into VIB_TABLE
	public int amsCnt;
	public int amsIncr;
	public int vibCnt;
	public int vibIncr;
	// wave selector enable flag
	public byte wavesel;
	// external event callback handler
	public OPL_TIMERHANDLER? TimerHandler; // TIMER handler
	public int TimerParam;                 // TIMER parameter
	public OPL_IRQHANDLER? IRQHandler;     // IRQ handler
	public int IRQParam;                   // IRQ parameter
	public OPL_UPDATEHANDLER? UpdateHandler; // stream update handler
	public int UpdateParam;                // stream update parameter
}

/// <summary>
/// Software implementation of the YM3812 (OPL2) FM sound generator, as used by the Adlib music player.
/// </summary>
/// <remarks>
/// Like the C code, the emulator keeps the tables and the "current chip" cache in static state.
/// Only the YM3812 is built (no Y8950 DELTA-T/keyboard/IO units, as in the C++ build).
/// </remarks>
internal static class Fmopl
{
	// --- select emulation chips / output ---
	public const int OPL_TYPE_WAVESEL = 0x01;  // waveform select
	public const int OPL_TYPE_ADPCM = 0x02;    // DELTA-T ADPCM unit
	public const int OPL_TYPE_KEYBOARD = 0x04; // keyboard interface
	public const int OPL_TYPE_IO = 0x08;       // I/O port
	public const int OPL_TYPE_YM3526 = 0;
	public const int OPL_TYPE_YM3812 = OPL_TYPE_WAVESEL;
	public const int OPL_TYPE_Y8950 = OPL_TYPE_ADPCM | OPL_TYPE_KEYBOARD | OPL_TYPE_IO;

	private const double PI = 3.14159265358979323846;

	// -------------------- preliminary define section ---------------------
	// attack/decay rate time rate
	private const int OPL_ARRATE = 141280;  // RATE 4 =  2826.24ms @ 3.6MHz
	private const int OPL_DRRATE = 1956000; // RATE 4 = 39280.64ms @ 3.6MHz

	private const int FREQ_BITS = 24; // frequency turn

	// counter bits = 20 , octerve 7
	private const int FREQ_RATE = 1 << (FREQ_BITS - 20);
	private const int TL_BITS = FREQ_BITS + 2;

	// final output shift , limit minimum and maximum
	private const int OPL_OUTSB = TL_BITS + 1 - 16; // OPL output final shift 16bit
	private const int OPL_MAXOUT = 0x7fff << OPL_OUTSB;
	private const int OPL_MINOUT = -(0x8000 << OPL_OUTSB);

	// -------------------- quality selection ---------------------

	// sinwave entries
	private const int SIN_ENT = 2048;

	// output level entries (envelope,sinwave)
	// envelope counter lower bits
	private const int ENV_BITS = 16;
	// envelope output entries
	private const int EG_ENT = 4096;

	private const int EG_OFF = (2 * EG_ENT) << ENV_BITS; // OFF
	private const int EG_DED = EG_OFF;
	private const int EG_DST = EG_ENT << ENV_BITS;       // DECAY  START
	private const int EG_AED = EG_DST;
	private const int EG_AST = 0;                        // ATTACK START

	private const double EG_STEP = 96.0 / EG_ENT; // OPL is 0.1875 dB step

	// LFO table entries
	private const int VIB_ENT = 512;
	private const int VIB_SHIFT = 32 - 9;
	private const int AMS_ENT = 512;
	private const int AMS_SHIFT = 32 - 9;

	private const int VIB_RATE = 256;

	// -------------------- local defines , macros ---------------------

	// register number to channel number , slot offset
	private const int SLOT1 = 0;
	private const int SLOT2 = 1;

	// envelope phase
	private const int ENV_MOD_RR = 0x00;
	private const int ENV_MOD_DR = 0x01;
	private const int ENV_MOD_AR = 0x02;

	// -------------------- tables ---------------------
	private static readonly int[] slot_array =
	[
		 0, 2, 4, 1, 3, 5,-1,-1,
		 6, 8,10, 7, 9,11,-1,-1,
		12,14,16,13,15,17,-1,-1,
		18,20,22,19,21,23,-1,-1
	];

	// key scale level
	// table is 3dB/OCT , DV converts this in TL step at 6dB/OCT
	private const double DV = EG_STEP / 2;
	private static readonly uint[] KSL_TABLE = MakeKslTable(
	[
		// OCT 0
		0.000, 0.000, 0.000, 0.000, 0.000, 0.000, 0.000, 0.000,
		0.000, 0.000, 0.000, 0.000, 0.000, 0.000, 0.000, 0.000,
		// OCT 1
		0.000, 0.000, 0.000, 0.000, 0.000, 0.000, 0.000, 0.000,
		0.000, 0.750, 1.125, 1.500, 1.875, 2.250, 2.625, 3.000,
		// OCT 2
		0.000, 0.000, 0.000, 0.000, 0.000, 1.125, 1.875, 2.625,
		3.000, 3.750, 4.125, 4.500, 4.875, 5.250, 5.625, 6.000,
		// OCT 3
		0.000, 0.000, 0.000, 1.875, 3.000, 4.125, 4.875, 5.625,
		6.000, 6.750, 7.125, 7.500, 7.875, 8.250, 8.625, 9.000,
		// OCT 4
		0.000, 0.000, 3.000, 4.875, 6.000, 7.125, 7.875, 8.625,
		9.000, 9.750, 10.125, 10.500, 10.875, 11.250, 11.625, 12.000,
		// OCT 5
		0.000, 3.000, 6.000, 7.875, 9.000, 10.125, 10.875, 11.625,
		12.000, 12.750, 13.125, 13.500, 13.875, 14.250, 14.625, 15.000,
		// OCT 6
		0.000, 6.000, 9.000, 10.875, 12.000, 13.125, 13.875, 14.625,
		15.000, 15.750, 16.125, 16.500, 16.875, 17.250, 17.625, 18.000,
		// OCT 7
		0.000, 9.000, 12.000, 13.875, 15.000, 16.125, 16.875, 17.625,
		18.000, 18.750, 19.125, 19.500, 19.875, 20.250, 20.625, 21.000
	]);

	private static uint[] MakeKslTable(double[] db)
	{
		var t = new uint[db.Length];
		for (int i = 0; i < db.Length; ++i)
			t[i] = (uint)(db[i] / DV);
		return t;
	}

	// sustain level table (3db per step)
	// 0 - 15: 0, 3, 6, 9,12,15,18,21,24,27,30,33,36,39,42,93 (dB)
	private static int SC(int db) => (int)((db * ((3 / EG_STEP) * (1 << ENV_BITS))) + EG_DST);
	private static readonly int[] SL_TABLE =
	[
		SC(0), SC(1), SC(2), SC(3), SC(4), SC(5), SC(6), SC(7),
		SC(8), SC(9), SC(10), SC(11), SC(12), SC(13), SC(14), SC(31)
	];

	private const int TL_MAX = EG_ENT * 2; // limit(tl + ksr + envelope) + sinwave
	// TotalLevel : 48 24 12  6  3 1.5 0.75 (dB)
	// TL_TABLE[ 0      to TL_MAX          ] : plus  section
	// TL_TABLE[ TL_MAX to TL_MAX+TL_MAX-1 ] : minus section
	private static int[] TL_TABLE = [];

	// offsets into TL_TABLE with sinwave output offset
	private static int[] SIN_TABLE = [];

	// LFO table
	private static int[] AMS_TABLE = [];
	private static int[] VIB_TABLE = [];

	// envelope output curve table
	// attack + decay + OFF
	private static readonly int[] ENV_CURVE = new int[2 * EG_ENT + 1];

	// multiple table
	private const uint ML = 2;
	private static readonly uint[] MUL_TABLE =
	[
		// 1/2, 1, 2, 3, 4, 5, 6, 7, 8, 9,10,11,12,13,14,15
		ML / 2, 1 * ML, 2 * ML, 3 * ML, 4 * ML, 5 * ML, 6 * ML, 7 * ML,
		8 * ML, 9 * ML, 10 * ML, 10 * ML, 12 * ML, 12 * ML, 15 * ML, 15 * ML
	];

	/// <summary>Dummy attack / decay rate ( when rate == 0 ).</summary>
	internal static readonly int[] RATE_0 = new int[16];

	// -------------------- static state ---------------------

	// lock level of common table
	private static int num_lock;

	// work table
	private static FM_OPL? cur_chip; // current chip point
	// current chip state
	private static OPL_CH[] S_CH = [];
	private static OPL_SLOT SLOT7_1 = new(), SLOT7_2 = new(), SLOT8_1 = new(), SLOT8_2 = new();

	private static int outd;
	private static int ams;
	private static int vib;
	private static int ams_table;
	private static int vib_table;
	private static int amsIncr;
	private static int vibIncr;
	private static int feedback2; // connect for SLOT 2

	/// <summary>Test hook: replaces <see cref="Rng.Seedless"/> as source of the rhythm white noise.</summary>
	internal static Rng.RandomState? NoiseState { get; set; }

	// --------------------- subroutines  ---------------------

	private static int Limit(int val, int max, int min)
	{
		if (val > max)
			val = max;
		else if (val < min)
			val = min;

		return val;
	}

	/// status set and IRQ handling
	private static void OPL_STATUS_SET(FM_OPL OPL, int flag)
	{
		// set status flag
		OPL.status |= (byte)flag;
		if ((OPL.status & 0x80) == 0)
		{
			if ((OPL.status & OPL.statusmask) != 0)
			{   // IRQ on
				OPL.status |= 0x80;
				// callback user interrupt handler (IRQ is OFF to ON)
				OPL.IRQHandler?.Invoke(OPL.IRQParam, 1);
			}
		}
	}

	/// status reset and IRQ handling
	private static void OPL_STATUS_RESET(FM_OPL OPL, int flag)
	{
		// reset status flag
		OPL.status &= (byte)~flag;
		if ((OPL.status & 0x80) != 0)
		{
			if ((OPL.status & OPL.statusmask) == 0)
			{
				OPL.status &= 0x7f;
				// callback user interrupt handler (IRQ is ON to OFF)
				OPL.IRQHandler?.Invoke(OPL.IRQParam, 0);
			}
		}
	}

	/// IRQ mask set
	private static void OPL_STATUSMASK_SET(FM_OPL OPL, int flag)
	{
		OPL.statusmask = (byte)flag;
		// IRQ handling check
		OPL_STATUS_SET(OPL, 0);
		OPL_STATUS_RESET(OPL, 0);
	}

	/// ----- key on  -----
	private static void OPL_KEYON(OPL_SLOT SLOT)
	{
		// sin wave restart
		SLOT.Cnt = 0;
		// set attack
		SLOT.evm = ENV_MOD_AR;
		SLOT.evs = SLOT.evsa;
		SLOT.evc = EG_AST;
		SLOT.eve = EG_AED;
	}

	/// ----- key off -----
	private static void OPL_KEYOFF(OPL_SLOT SLOT)
	{
		if (SLOT.evm > ENV_MOD_RR)
		{
			// set envelope counter from envelope output
			SLOT.evm = ENV_MOD_RR;
			if ((SLOT.evc & EG_DST) == 0)
				SLOT.evc = (ENV_CURVE[SLOT.evc >> ENV_BITS] << ENV_BITS) + EG_DST;
			SLOT.eve = EG_DED;
			SLOT.evs = SLOT.evsr;
		}
	}

	/// ---------- calcrate Envelope Generator & Phase Generator ----------
	/// return : envelope output
	private static uint OPL_CALC_SLOT(OPL_SLOT SLOT)
	{
		// calcrate envelope generator
		if ((SLOT.evc += SLOT.evs) >= SLOT.eve)
		{
			switch (SLOT.evm)
			{
			case ENV_MOD_AR: // ATTACK -> DECAY1
				// next DR
				SLOT.evm = ENV_MOD_DR;
				SLOT.evc = EG_DST;
				SLOT.eve = SLOT.SL;
				SLOT.evs = SLOT.evsd;
				break;
			case ENV_MOD_DR: // DECAY -> SL or RR
				SLOT.evc = SLOT.SL;
				SLOT.eve = EG_DED;
				if (SLOT.eg_typ != 0)
				{
					SLOT.evs = 0;
				}
				else
				{
					SLOT.evm = ENV_MOD_RR;
					SLOT.evs = SLOT.evsr;
				}
				break;
			case ENV_MOD_RR: // RR -> OFF
				SLOT.evc = EG_OFF;
				SLOT.eve = EG_OFF + 1;
				SLOT.evs = 0;
				break;
			}
		}
		// calcrate envelope
		return unchecked((uint)(SLOT.TLL + ENV_CURVE[SLOT.evc >> ENV_BITS] + (SLOT.ams != 0 ? ams : 0)));
	}

	/// set algorithm connection
	private static void set_algorythm(OPL_CH CH)
	{
		CH.connect1Carrier = CH.CON != 0;
	}

	/// ---------- frequency counter for operator update ----------
	private static void CALC_FCSLOT(OPL_CH CH, OPL_SLOT SLOT)
	{
		int ksr;

		// frequency step counter
		SLOT.Incr = unchecked(CH.fc * SLOT.mul);
		ksr = CH.kcode >> SLOT.KSR;

		if (SLOT.ksr != ksr)
		{
			SLOT.ksr = (byte)ksr;
			// attack , decay rate recalcration
			SLOT.evsa = SLOT.AR[SLOT.ARo + ksr];
			SLOT.evsd = SLOT.DR[SLOT.DRo + ksr];
			SLOT.evsr = SLOT.RR[SLOT.RRo + ksr];
		}
		SLOT.TLL = (int)(SLOT.TL + (CH.ksl_base >> SLOT.ksl));
	}

	/// set multi,am,vib,EG-TYP,KSR,mul
	private static void set_mul(FM_OPL OPL, int slot, int v)
	{
		OPL_CH CH = OPL.P_CH[slot / 2];
		OPL_SLOT SLOT = CH.SLOT[slot & 1];

		SLOT.mul = MUL_TABLE[v & 0x0f];
		SLOT.KSR = (byte)((v & 0x10) != 0 ? 0 : 2);
		SLOT.eg_typ = (byte)((v & 0x20) >> 5);
		SLOT.vib = (byte)(v & 0x40);
		SLOT.ams = (byte)(v & 0x80);
		CALC_FCSLOT(CH, SLOT);
	}

	/// set ksl & tl
	private static void set_ksl_tl(FM_OPL OPL, int slot, int v)
	{
		OPL_CH CH = OPL.P_CH[slot / 2];
		OPL_SLOT SLOT = CH.SLOT[slot & 1];
		int ksl = v >> 6; // 0 / 1.5 / 3 / 6 db/OCT

		SLOT.ksl = (byte)(ksl != 0 ? 3 - ksl : 31);
		SLOT.TL = (int)((v & 0x3f) * (0.75 / EG_STEP)); // 0.75db step

		if ((OPL.mode & 0x80) == 0)
		{   // not CSM latch total level
			SLOT.TLL = (int)(SLOT.TL + (CH.ksl_base >> SLOT.ksl));
		}
	}

	/// set attack rate & decay rate
	private static void set_ar_dr(FM_OPL OPL, int slot, int v)
	{
		OPL_CH CH = OPL.P_CH[slot / 2];
		OPL_SLOT SLOT = CH.SLOT[slot & 1];
		int ar = v >> 4;
		int dr = v & 0x0f;

		if (ar != 0) { SLOT.AR = OPL.AR_TABLE; SLOT.ARo = ar << 2; } else { SLOT.AR = RATE_0; SLOT.ARo = 0; }
		SLOT.evsa = SLOT.AR[SLOT.ARo + SLOT.ksr];
		if (SLOT.evm == ENV_MOD_AR) SLOT.evs = SLOT.evsa;

		if (dr != 0) { SLOT.DR = OPL.DR_TABLE; SLOT.DRo = dr << 2; } else { SLOT.DR = RATE_0; SLOT.DRo = 0; }
		SLOT.evsd = SLOT.DR[SLOT.DRo + SLOT.ksr];
		if (SLOT.evm == ENV_MOD_DR) SLOT.evs = SLOT.evsd;
	}

	/// set sustain level & release rate
	private static void set_sl_rr(FM_OPL OPL, int slot, int v)
	{
		OPL_CH CH = OPL.P_CH[slot / 2];
		OPL_SLOT SLOT = CH.SLOT[slot & 1];
		int sl = v >> 4;
		int rr = v & 0x0f;

		SLOT.SL = SL_TABLE[sl];
		if (SLOT.evm == ENV_MOD_DR) SLOT.eve = SLOT.SL;
		SLOT.RR = OPL.DR_TABLE;
		SLOT.RRo = rr << 2;
		SLOT.evsr = SLOT.RR[SLOT.RRo + SLOT.ksr];
		if (SLOT.evm == ENV_MOD_RR) SLOT.evs = SLOT.evsr;
	}

	/// operator output calcrator
	private static int OP_OUT(OPL_SLOT slot, uint env, int con)
	{
		uint idx = unchecked((slot.Cnt + (uint)con) / (0x1000000 / SIN_ENT)) & (SIN_ENT - 1);
		return TL_TABLE[SIN_TABLE[slot.wavetable + (int)idx] + (int)env];
	}

	/// ---------- calcrate one of channel ----------
	private static void OPL_CALC_CH(OPL_CH CH)
	{
		uint env_out;
		OPL_SLOT SLOT;

		feedback2 = 0;
		// SLOT 1
		SLOT = CH.SLOT[SLOT1];
		env_out = OPL_CALC_SLOT(SLOT);
		if (env_out < EG_ENT - 1)
		{
			// PG
			if (SLOT.vib != 0) SLOT.Cnt += unchecked(SLOT.Incr * (uint)vib / VIB_RATE);
			else SLOT.Cnt += SLOT.Incr;
			// connection
			if (CH.FB != 0)
			{
				int feedback1 = (CH.op1_out[0] + CH.op1_out[1]) >> CH.FB;
				CH.op1_out[1] = CH.op1_out[0];
				int o = CH.op1_out[0] = OP_OUT(SLOT, env_out, feedback1);
				if (CH.connect1Carrier) outd += o; else feedback2 += o;
			}
			else
			{
				int o = OP_OUT(SLOT, env_out, 0);
				if (CH.connect1Carrier) outd += o; else feedback2 += o;
			}
		}
		else
		{
			CH.op1_out[1] = CH.op1_out[0];
			CH.op1_out[0] = 0;
		}
		// SLOT 2
		SLOT = CH.SLOT[SLOT2];
		env_out = OPL_CALC_SLOT(SLOT);
		if (env_out < EG_ENT - 1)
		{
			// PG
			if (SLOT.vib != 0) SLOT.Cnt += unchecked(SLOT.Incr * (uint)vib / VIB_RATE);
			else SLOT.Cnt += SLOT.Incr;
			// connection
			outd += OP_OUT(SLOT, env_out, feedback2);
		}
	}

	/// ---------- calcrate rythm block ----------
	private const double WHITE_NOISE_db = 6.0;
	private static void OPL_CALC_RH(OPL_CH[] CH)
	{
		uint env_tam, env_sd, env_top, env_hh;
		int noise = NoiseState != null ? NoiseState.Generate(0, 1) : Rng.Seedless(0, 1);
		int whitenoise = (int)(noise * (WHITE_NOISE_db / EG_STEP));
		int tone8;

		OPL_SLOT SLOT;
		int env_out;

		// BD : same as FM serial mode and output level is large
		feedback2 = 0;
		// SLOT 1
		SLOT = CH[6].SLOT[SLOT1];
		env_out = (int)OPL_CALC_SLOT(SLOT);
		if (env_out < EG_ENT - 1)
		{
			// PG
			if (SLOT.vib != 0) SLOT.Cnt += unchecked(SLOT.Incr * (uint)vib / VIB_RATE);
			else SLOT.Cnt += SLOT.Incr;
			// connection
			if (CH[6].FB != 0)
			{
				int feedback1 = (CH[6].op1_out[0] + CH[6].op1_out[1]) >> CH[6].FB;
				CH[6].op1_out[1] = CH[6].op1_out[0];
				feedback2 = CH[6].op1_out[0] = OP_OUT(SLOT, (uint)env_out, feedback1);
			}
			else
			{
				feedback2 = OP_OUT(SLOT, (uint)env_out, 0);
			}
		}
		else
		{
			feedback2 = 0;
			CH[6].op1_out[1] = CH[6].op1_out[0];
			CH[6].op1_out[0] = 0;
		}
		// SLOT 2
		SLOT = CH[6].SLOT[SLOT2];
		env_out = (int)OPL_CALC_SLOT(SLOT);
		if (env_out < EG_ENT - 1)
		{
			// PG
			if (SLOT.vib != 0) SLOT.Cnt += unchecked(SLOT.Incr * (uint)vib / VIB_RATE);
			else SLOT.Cnt += SLOT.Incr;
			// connection
			outd += OP_OUT(SLOT, (uint)env_out, feedback2) * 2;
		}

		// SD  (17) = mul14[fnum7] + white noise
		// TAM (15) = mul15[fnum8]
		// TOP (18) = fnum6(mul18[fnum8]+whitenoise)
		// HH  (14) = fnum7(mul18[fnum8]+whitenoise) + white noise
		unchecked
		{
			env_sd = OPL_CALC_SLOT(SLOT7_2) + (uint)whitenoise;
			env_tam = OPL_CALC_SLOT(SLOT8_1);
			env_top = OPL_CALC_SLOT(SLOT8_2);
			env_hh = OPL_CALC_SLOT(SLOT7_1) + (uint)whitenoise;

			// PG
			if (SLOT7_1.vib != 0) SLOT7_1.Cnt += 2 * SLOT7_1.Incr * (uint)vib / VIB_RATE;
			else SLOT7_1.Cnt += 2 * SLOT7_1.Incr;
			if (SLOT7_2.vib != 0) SLOT7_2.Cnt += CH[7].fc * 8 * (uint)vib / VIB_RATE;
			else SLOT7_2.Cnt += CH[7].fc * 8;
			if (SLOT8_1.vib != 0) SLOT8_1.Cnt += SLOT8_1.Incr * (uint)vib / VIB_RATE;
			else SLOT8_1.Cnt += SLOT8_1.Incr;
			if (SLOT8_2.vib != 0) SLOT8_2.Cnt += CH[8].fc * 48 * (uint)vib / VIB_RATE;
			else SLOT8_2.Cnt += CH[8].fc * 48;
		}

		tone8 = OP_OUT(SLOT8_2, (uint)whitenoise, 0);

		// SD
		if (env_sd < EG_ENT - 1)
			outd += OP_OUT(SLOT7_1, env_sd, 0) * 8;
		// TAM
		if (env_tam < EG_ENT - 1)
			outd += OP_OUT(SLOT8_1, env_tam, 0) * 2;
		// TOP-CY
		if (env_top < EG_ENT - 1)
			outd += OP_OUT(SLOT7_2, env_top, tone8) * 2;
		// HH
		if (env_hh < EG_ENT - 1)
			outd += OP_OUT(SLOT7_2, env_hh, tone8) * 2;
	}

	/// ----------- initialize time tables -----------
	private static void init_timetables(FM_OPL OPL, int ARRATE, int DRRATE)
	{
		int i;
		double rate;

		// make attack rate & decay rate tables
		for (i = 0; i < 4; i++) OPL.AR_TABLE[i] = OPL.DR_TABLE[i] = 0;
		for (i = 4; i <= 60; i++)
		{
			rate = OPL.freqbase;                        // frequency rate
			if (i < 60) rate *= 1.0 + (i & 3) * 0.25;   // b0-1 : x1 , x1.25 , x1.5 , x1.75
			rate *= 1 << ((i >> 2) - 1);                // b2-5 : shift bit
			rate *= (double)(EG_ENT << ENV_BITS);
			OPL.AR_TABLE[i] = (int)(rate / ARRATE);
			OPL.DR_TABLE[i] = (int)(rate / DRRATE);
		}
		for (i = 60; i < 75; i++)
		{
			OPL.AR_TABLE[i] = EG_AED - 1;
			OPL.DR_TABLE[i] = OPL.DR_TABLE[60];
		}
	}

	/// ---------- generic table initialize ----------
	private static int OPLOpenTable()
	{
		int s, t;
		double rate;
		int i, j;
		double pom;

		// allocate dynamic tables
		TL_TABLE = new int[TL_MAX * 2];
		SIN_TABLE = new int[SIN_ENT * 4];
		AMS_TABLE = new int[AMS_ENT * 2];
		VIB_TABLE = new int[VIB_ENT * 2];
		// make total level table
		for (t = 0; t < EG_ENT - 1; t++)
		{
			rate = ((1 << TL_BITS) - 1) / Math.Pow(10, EG_STEP * t / 20); // dB -> voltage
			TL_TABLE[t] = (int)rate;
			TL_TABLE[TL_MAX + t] = -TL_TABLE[t];
		}
		// fill volume off area
		for (t = EG_ENT - 1; t < TL_MAX; t++)
		{
			TL_TABLE[t] = TL_TABLE[TL_MAX + t] = 0;
		}

		// make sinwave table (total level offset)
		// degree 0 = degree 180                   = off
		SIN_TABLE[0] = SIN_TABLE[SIN_ENT / 2] = EG_ENT - 1;
		for (s = 1; s <= SIN_ENT / 4; s++)
		{
			pom = Math.Sin(2 * PI * s / SIN_ENT); // sin
			pom = 20 * Math.Log10(1 / pom);        // decibel
			j = (int)(pom / EG_STEP);              // TL_TABLE steps

			// degree 0   -  90    , degree 180 -  90 : plus section
			SIN_TABLE[s] = SIN_TABLE[SIN_ENT / 2 - s] = j;
			// degree 180 - 270    , degree 360 - 270 : minus section
			SIN_TABLE[SIN_ENT / 2 + s] = SIN_TABLE[SIN_ENT - s] = TL_MAX + j;
		}
		for (s = 0; s < SIN_ENT; s++)
		{
			SIN_TABLE[SIN_ENT * 1 + s] = s < (SIN_ENT / 2) ? SIN_TABLE[s] : EG_ENT;
			SIN_TABLE[SIN_ENT * 2 + s] = SIN_TABLE[s % (SIN_ENT / 2)];
			SIN_TABLE[SIN_ENT * 3 + s] = ((s / (SIN_ENT / 4)) & 1) != 0 ? EG_ENT : SIN_TABLE[SIN_ENT * 2 + s];
		}

		// envelope counter -> envelope output table
		for (i = 0; i < EG_ENT; i++)
		{
			// ATTACK curve
			pom = Math.Pow((double)(EG_ENT - 1 - i) / EG_ENT, 8) * EG_ENT;
			ENV_CURVE[i] = (int)pom;
			// DECAY ,RELEASE curve
			ENV_CURVE[(EG_DST >> ENV_BITS) + i] = i;
		}
		// off
		ENV_CURVE[EG_OFF >> ENV_BITS] = EG_ENT - 1;
		// make LFO ams table
		for (i = 0; i < AMS_ENT; i++)
		{
			pom = (1.0 + Math.Sin(2 * PI * i / AMS_ENT)) / 2; // sin
			AMS_TABLE[i] = (int)((1.0 / EG_STEP) * pom);           // 1dB
			AMS_TABLE[AMS_ENT + i] = (int)((4.8 / EG_STEP) * pom); // 4.8dB
		}
		// make LFO vibrate table
		for (i = 0; i < VIB_ENT; i++)
		{
			// 100cent = 1seminote = 6% ??
			pom = (double)VIB_RATE * 0.06 * Math.Sin(2 * PI * i / VIB_ENT); // +-100sect step
			VIB_TABLE[i] = (int)(VIB_RATE + (pom * 0.07));           // +- 7cent
			VIB_TABLE[VIB_ENT + i] = (int)(VIB_RATE + (pom * 0.14)); // +-14cent
		}
		return 1;
	}

	private static void OPLCloseTable()
	{
		TL_TABLE = [];
		SIN_TABLE = [];
		AMS_TABLE = [];
		VIB_TABLE = [];
	}

	/// CSM Key Control
	private static void CSMKeyControl(OPL_CH CH)
	{
		OPL_SLOT slot1 = CH.SLOT[SLOT1];
		OPL_SLOT slot2 = CH.SLOT[SLOT2];
		// all key off
		OPL_KEYOFF(slot1);
		OPL_KEYOFF(slot2);
		// total level latch
		slot1.TLL = (int)(slot1.TL + (CH.ksl_base >> slot1.ksl));
		slot1.TLL = (int)(slot1.TL + (CH.ksl_base >> slot1.ksl));
		// key on
		CH.op1_out[0] = CH.op1_out[1] = 0;
		OPL_KEYON(slot1);
		OPL_KEYON(slot2);
	}

	/// ---------- opl initialize ----------
	private static void OPL_initalize(FM_OPL OPL)
	{
		int fn;

		// frequency base
		OPL.freqbase = (OPL.rate != 0) ? ((double)OPL.clock / OPL.rate) / 72 : 0;
		// Timer base time
		OPL.TimerBase = 1.0 / ((double)OPL.clock / 72.0);
		// make time tables
		init_timetables(OPL, OPL_ARRATE, OPL_DRRATE);
		// make fnumber -> increment counter table
		for (fn = 0; fn < 1024; fn++)
		{
			OPL.FN_TABLE[fn] = (uint)(OPL.freqbase * fn * FREQ_RATE * (1 << 7) / 2);
		}
		// LFO freq.table
		OPL.amsIncr = OPL.rate != 0 ? (int)((double)AMS_ENT * (1 << AMS_SHIFT) / OPL.rate * 3.7 * ((double)OPL.clock / 3600000)) : 0;
		OPL.vibIncr = OPL.rate != 0 ? (int)((double)VIB_ENT * (1 << VIB_SHIFT) / OPL.rate * 6.4 * ((double)OPL.clock / 3600000)) : 0;
	}

	/// ---------- write a OPL registers ----------
	private static void OPLWriteReg(FM_OPL OPL, int r, int v)
	{
		OPL_CH CH;
		int slot;
		int block_fnum;

		switch (r & 0xe0)
		{
		case 0x00: // 00-1f:control
			switch (r & 0x1f)
			{
			case 0x01:
				// wave selector enable
				if ((OPL.type & OPL_TYPE_WAVESEL) != 0)
				{
					OPL.wavesel = (byte)(v & 0x20);
					if (OPL.wavesel == 0)
					{
						// preset compatible mode
						int c;
						for (c = 0; c < OPL.max_ch; c++)
						{
							OPL.P_CH[c].SLOT[SLOT1].wavetable = 0;
							OPL.P_CH[c].SLOT[SLOT2].wavetable = 0;
						}
					}
				}
				return;
			case 0x02: // Timer 1
				OPL.T[0] = (256 - v) * 4;
				break;
			case 0x03: // Timer 2
				OPL.T[1] = (256 - v) * 16;
				return;
			case 0x04: // IRQ clear / mask and Timer enable
				if ((v & 0x80) != 0)
				{   // IRQ flag clear
					OPL_STATUS_RESET(OPL, 0x7f);
				}
				else
				{   // set IRQ mask ,timer enable
					byte st1 = (byte)(v & 1);
					byte st2 = (byte)((v >> 1) & 1);
					// IRQRST,T1MSK,t2MSK,EOSMSK,BRMSK,x,ST2,ST1
					OPL_STATUS_RESET(OPL, v & 0x78);
					OPL_STATUSMASK_SET(OPL, ((~v) & 0x78) | 0x01);
					// timer 2
					if (OPL.st[1] != st2)
					{
						double interval = st2 != 0 ? (double)OPL.T[1] * OPL.TimerBase : 0.0;
						OPL.st[1] = st2;
						OPL.TimerHandler?.Invoke(OPL.TimerParam + 1, interval);
					}
					// timer 1
					if (OPL.st[0] != st1)
					{
						double interval = st1 != 0 ? (double)OPL.T[0] * OPL.TimerBase : 0.0;
						OPL.st[0] = st1;
						OPL.TimerHandler?.Invoke(OPL.TimerParam + 0, interval);
					}
				}
				return;
			}
			break;
		case 0x20: // am,vib,ksr,eg type,mul
			slot = slot_array[r & 0x1f];
			if (slot == -1) return;
			set_mul(OPL, slot, v);
			return;
		case 0x40:
			slot = slot_array[r & 0x1f];
			if (slot == -1) return;
			set_ksl_tl(OPL, slot, v);
			return;
		case 0x60:
			slot = slot_array[r & 0x1f];
			if (slot == -1) return;
			set_ar_dr(OPL, slot, v);
			return;
		case 0x80:
			slot = slot_array[r & 0x1f];
			if (slot == -1) return;
			set_sl_rr(OPL, slot, v);
			return;
		case 0xa0:
			switch (r)
			{
			case 0xbd:
				// amsep,vibdep,r,bd,sd,tom,tc,hh
				{
					byte rkey = (byte)(OPL.rythm ^ v);
					OPL.ams_table = (v & 0x80) != 0 ? AMS_ENT : 0;
					OPL.vib_table = (v & 0x40) != 0 ? VIB_ENT : 0;
					OPL.rythm = (byte)(v & 0x3f);
					if ((OPL.rythm & 0x20) != 0)
					{
						// BD key on/off
						if ((rkey & 0x10) != 0)
						{
							if ((v & 0x10) != 0)
							{
								OPL.P_CH[6].op1_out[0] = OPL.P_CH[6].op1_out[1] = 0;
								OPL_KEYON(OPL.P_CH[6].SLOT[SLOT1]);
								OPL_KEYON(OPL.P_CH[6].SLOT[SLOT2]);
							}
							else
							{
								OPL_KEYOFF(OPL.P_CH[6].SLOT[SLOT1]);
								OPL_KEYOFF(OPL.P_CH[6].SLOT[SLOT2]);
							}
						}
						// SD key on/off
						if ((rkey & 0x08) != 0)
						{
							if ((v & 0x08) != 0) OPL_KEYON(OPL.P_CH[7].SLOT[SLOT2]);
							else OPL_KEYOFF(OPL.P_CH[7].SLOT[SLOT2]);
						}
						// TAM key on/off
						if ((rkey & 0x04) != 0)
						{
							if ((v & 0x04) != 0) OPL_KEYON(OPL.P_CH[8].SLOT[SLOT1]);
							else OPL_KEYOFF(OPL.P_CH[8].SLOT[SLOT1]);
						}
						// TOP-CY key on/off
						if ((rkey & 0x02) != 0)
						{
							if ((v & 0x02) != 0) OPL_KEYON(OPL.P_CH[8].SLOT[SLOT2]);
							else OPL_KEYOFF(OPL.P_CH[8].SLOT[SLOT2]);
						}
						// HH key on/off
						if ((rkey & 0x01) != 0)
						{
							if ((v & 0x01) != 0) OPL_KEYON(OPL.P_CH[7].SLOT[SLOT1]);
							else OPL_KEYOFF(OPL.P_CH[7].SLOT[SLOT1]);
						}
					}
				}
				return;
			}
			// keyon,block,fnum
			if ((r & 0x0f) > 11) return; //8
			CH = OPL.P_CH[r & 0x0f];
			if ((r & 0x10) == 0)
			{   // a0-a8
				block_fnum = (int)(CH.block_fnum & 0x1f00) | v;
			}
			else
			{   // b0-b8
				int keyon = (v >> 5) & 1;
				block_fnum = ((v & 0x1f) << 8) | (int)(CH.block_fnum & 0xff);
				if (CH.keyon != keyon)
				{
					if ((CH.keyon = (byte)keyon) != 0)
					{
						CH.op1_out[0] = CH.op1_out[1] = 0;
						OPL_KEYON(CH.SLOT[SLOT1]);
						OPL_KEYON(CH.SLOT[SLOT2]);
					}
					else
					{
						OPL_KEYOFF(CH.SLOT[SLOT1]);
						OPL_KEYOFF(CH.SLOT[SLOT2]);
					}
				}
			}
			// update
			if ((int)CH.block_fnum != block_fnum)
			{
				int blockRv = 7 - (block_fnum >> 10);
				int fnum = block_fnum & 0x3ff;
				CH.block_fnum = (uint)block_fnum;

				CH.ksl_base = KSL_TABLE[block_fnum >> 6];
				CH.fc = OPL.FN_TABLE[fnum] >> blockRv;
				CH.kcode = (byte)(CH.block_fnum >> 9);
				if ((OPL.mode & 0x40) != 0 && (CH.block_fnum & 0x100) != 0) CH.kcode |= 1;
				CALC_FCSLOT(CH, CH.SLOT[SLOT1]);
				CALC_FCSLOT(CH, CH.SLOT[SLOT2]);
			}
			return;
		case 0xc0:
			// FB,C
			if ((r & 0x0f) > 11) return; //8
			CH = OPL.P_CH[r & 0x0f];
			{
				int feedback = (v >> 1) & 7;
				CH.FB = (byte)(feedback != 0 ? (8 + 1) - feedback : 0);
				CH.CON = (byte)(v & 1);
				set_algorythm(CH);
			}
			return;
		case 0xe0: // wave type
			slot = slot_array[r & 0x1f];
			if (slot == -1) return;
			CH = OPL.P_CH[slot / 2];
			if (OPL.wavesel != 0)
			{
				CH.SLOT[slot & 1].wavetable = (v & 0x03) * SIN_ENT;
			}
			return;
		}
	}

	/// lock/unlock for common table
	private static int OPL_LockTable()
	{
		num_lock++;
		if (num_lock > 1) return 0;
		// first time
		cur_chip = null;
		// allocate total level table (128kb space)
		if (OPLOpenTable() == 0)
		{
			num_lock--;
			return -1;
		}
		return 0;
	}

	private static void OPL_UnLockTable()
	{
		if (num_lock != 0) num_lock--;
		if (num_lock != 0) return;
		// last time
		cur_chip = null;
		OPLCloseTable();
	}

	/*******************************************************************************/
	/*		YM3812 local section                                                   */
	/*******************************************************************************/

	/// <summary>Renders samples of one chip.</summary>
	/// <param name="OPL">The chip.</param>
	/// <param name="buffer">Output; sample n goes to <c>buffer[n * stripe]</c>.</param>
	/// <param name="length">Number of buffer elements to cover (samples * stripe).</param>
	/// <param name="stripe">Distance between two output samples (2 = one channel of interleaved stereo).</param>
	/// <param name="volume">Output volume factor.</param>
	public static void YM3812UpdateOne(FM_OPL OPL, Span<short> buffer, int length, int stripe, float volume)
	{
		ArgumentNullException.ThrowIfNull(OPL);
		int i;
		int data;
		Span<short> buf = buffer;
		uint amsCnt = unchecked((uint)OPL.amsCnt);
		uint vibCnt = unchecked((uint)OPL.vibCnt);
		byte rythm = (byte)(OPL.rythm & 0x20);
		int R_CH;

		if (OPL != cur_chip)
		{
			cur_chip = OPL;
			// channel pointers
			S_CH = OPL.P_CH;
			// rythm slot
			SLOT7_1 = S_CH[7].SLOT[SLOT1];
			SLOT7_2 = S_CH[7].SLOT[SLOT2];
			SLOT8_1 = S_CH[8].SLOT[SLOT1];
			SLOT8_2 = S_CH[8].SLOT[SLOT2];
			// LFO state
			amsIncr = OPL.amsIncr;
			vibIncr = OPL.vibIncr;
			ams_table = OPL.ams_table;
			vib_table = OPL.vib_table;
		}
		R_CH = rythm != 0 ? 6 : 9; // E_CH = &S_CH[9]
		for (i = 0; i < length; i += stripe)
		{
			//            channel A         channel B         channel C
			// LFO
			unchecked
			{
				ams = AMS_TABLE[ams_table + (int)((amsCnt += (uint)amsIncr) >> AMS_SHIFT)];
				vib = VIB_TABLE[vib_table + (int)((vibCnt += (uint)vibIncr) >> VIB_SHIFT)];
			}
			outd = 0;
			// FM part
			for (int CH = 0; CH < R_CH; CH++)
				OPL_CALC_CH(S_CH[CH]);
			// Rythm part
			if (rythm != 0)
				OPL_CALC_RH(S_CH);
			outd = (int)(outd * volume);
			// limit check
			data = Limit(outd, OPL_MAXOUT, OPL_MINOUT);
			// store to sound buffer
			buf[i] = (short)(data >> OPL_OUTSB);
		}

		OPL.amsCnt = unchecked((int)amsCnt);
		OPL.vibCnt = unchecked((int)vibCnt);
	}

	/// <summary>Resets one chip.</summary>
	public static void OPLResetChip(FM_OPL OPL)
	{
		ArgumentNullException.ThrowIfNull(OPL);
		int c, s;
		int i;

		// reset chip
		OPL.mode = 0; // normal mode
		OPL_STATUS_RESET(OPL, 0x7f);
		// reset with register write
		OPLWriteReg(OPL, 0x01, 0); // wabesel disable
		OPLWriteReg(OPL, 0x02, 0); // Timer1
		OPLWriteReg(OPL, 0x03, 0); // Timer2
		OPLWriteReg(OPL, 0x04, 0); // IRQ mask clear
		for (i = 0xff; i >= 0x20; i--) OPLWriteReg(OPL, i, 0);
		// reset OPerator parameter
		for (c = 0; c < OPL.max_ch; c++)
		{
			OPL_CH CH = OPL.P_CH[c];
			for (s = 0; s < 2; s++)
			{
				// wave table
				CH.SLOT[s].wavetable = 0;
				CH.SLOT[s].evc = EG_OFF;
				CH.SLOT[s].eve = EG_OFF + 1;
				CH.SLOT[s].evs = 0;
			}
		}
	}

	/// <summary>Creates one virtual YM3812.</summary>
	/// <param name="type">Chip type (<see cref="OPL_TYPE_YM3812"/>).</param>
	/// <param name="clock">Master clock in Hz.</param>
	/// <param name="rate">Sampling rate in Hz.</param>
	public static FM_OPL? OPLCreate(int type, int clock, int rate)
	{
		int max_ch = 12; // normally 9 channels

		if (OPL_LockTable() == -1) return null;
		// allocate OPL state space
		var OPL = new FM_OPL();
		OPL.P_CH = new OPL_CH[max_ch];
		for (int c = 0; c < max_ch; ++c)
			OPL.P_CH[c] = new OPL_CH();
		// set channel state pointer
		OPL.type = (byte)type;
		OPL.clock = clock;
		OPL.rate = rate;
		OPL.max_ch = max_ch;
		// init global tables
		OPL_initalize(OPL);
		// reset chip
		OPLResetChip(OPL);
		return OPL;
	}

	/// <summary>Destroys one virtual YM3812.</summary>
	public static void OPLDestroy(FM_OPL? OPL)
	{
		if (OPL == null)
		{
			return;
		}
		OPL_UnLockTable();
	}

	// ----------  Option handlers ----------

	public static void OPLSetTimerHandler(FM_OPL OPL, OPL_TIMERHANDLER? TimerHandler, int channelOffset)
	{
		ArgumentNullException.ThrowIfNull(OPL);
		OPL.TimerHandler = TimerHandler;
		OPL.TimerParam = channelOffset;
	}

	public static void OPLSetIRQHandler(FM_OPL OPL, OPL_IRQHANDLER? IRQHandler, int param)
	{
		ArgumentNullException.ThrowIfNull(OPL);
		OPL.IRQHandler = IRQHandler;
		OPL.IRQParam = param;
	}

	public static void OPLSetUpdateHandler(FM_OPL OPL, OPL_UPDATEHANDLER? UpdateHandler, int param)
	{
		ArgumentNullException.ThrowIfNull(OPL);
		OPL.UpdateHandler = UpdateHandler;
		OPL.UpdateParam = param;
	}

	/// <summary>YM3812 I/O interface: writes the address (even <paramref name="a"/>) or data port.</summary>
	/// <returns>The IRQ flag.</returns>
	public static int OPLWrite(FM_OPL OPL, int a, int v)
	{
		ArgumentNullException.ThrowIfNull(OPL);
		if ((a & 1) == 0)
		{   // address port
			OPL.address = (byte)(v & 0xff);
		}
		else
		{   // data port
			OPL.UpdateHandler?.Invoke(OPL.UpdateParam, 0);
			OPLWriteReg(OPL, OPL.address, v);
		}
		return OPL.status >> 7;
	}

	/// <summary>Reads the status (even <paramref name="a"/>) or data port.</summary>
	public static byte OPLRead(FM_OPL OPL, int a)
	{
		ArgumentNullException.ThrowIfNull(OPL);
		if ((a & 1) == 0)
		{   // status port
			return (byte)(OPL.status & (OPL.statusmask | 0x80));
		}
		// data port: the keyboard / I/O handlers only exist on the Y8950, which is not built
		return 0;
	}

	/// <summary>Signals that timer <paramref name="c"/> (0 = A, 1 = B) has expired.</summary>
	public static int OPLTimerOver(FM_OPL OPL, int c)
	{
		ArgumentNullException.ThrowIfNull(OPL);
		if (c != 0)
		{   // Timer B
			OPL_STATUS_SET(OPL, 0x20);
		}
		else
		{   // Timer A
			OPL_STATUS_SET(OPL, 0x40);
			// CSM mode key,TL control
			if ((OPL.mode & 0x80) != 0)
			{   // CSM mode total level latch and auto key on
				int ch;
				OPL.UpdateHandler?.Invoke(OPL.UpdateParam, 0);
				for (ch = 0; ch < 9; ch++)
					CSMKeyControl(OPL.P_CH[ch]);
			}
		}
		// reload timer
		OPL.TimerHandler?.Invoke(OPL.TimerParam + c, (double)OPL.T[c] * OPL.TimerBase);
		return OPL.status >> 7;
	}
}
