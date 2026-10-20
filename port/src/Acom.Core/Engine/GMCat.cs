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
// Ported from: src/Engine/GMCat.h, src/Engine/GMCat.cpp

namespace Acom;

/// <summary>
/// Subclass of CatFile to handle gm.cat files that contain MIDI music streams.
/// </summary>
public class GMCatFile : CatFile
{
	/// <summary>Reads a gm.cat archive.</summary>
	/// <param name="stream">Archive data.</param>
	/// <param name="fileName">Name for diagnostics.</param>
	public GMCatFile(Stream stream, string fileName = "") : base(stream, fileName)
	{
	}

	private static uint read_uint32_le(ReadOnlySpan<byte> p, int o)
	{
		return ((uint)p[o]) + (((uint)p[o + 1]) << 8)
			+ (((uint)p[o + 2]) << 16) + (((uint)p[o + 3]) << 24);
	}

	/// <summary>MIDI sequence (offset into the stream data).</summary>
	private struct Seq
	{
		public uint size;
		public int data;
	}

	/// <summary>MIDI track.</summary>
	private struct Track
	{
		public Seq seq;
		public uint channel;
	}

	/// <summary>MIDI stream.</summary>
	private sealed class GmStream
	{
		public int tempo, nsubs, ntracks;
		public readonly Seq[] subs = new Seq[256];
		public readonly Track[] tracks = new Track[256];
	}

	/// <remarks>
	/// The C++ length counter is unsigned and wraps around when a block size exceeds the
	/// remaining data, which leads to reads past the buffer; this is reported as an error here.
	/// </remarks>
	private static int gmext_read_stream(GmStream p, uint n, ReadOnlySpan<byte> buf, int data)
	{
		if (n-- == 0)
			return -1;
		p.tempo = buf[data++];

		// subsequences
		if (n-- == 0)
			return -1;
		p.nsubs = buf[data++];

		for (int i = 0; i < p.nsubs; ++i)
		{
			if (n < 4)
				return -1;
			uint s = read_uint32_le(buf, data);
			if (s < 4)
				return -1;
			if (s > n)
				return -1;
			p.subs[i].size = s - 4;
			p.subs[i].data = data + 4;
			n -= s;
			data += (int)s;
		}

		// tracks
		if (n-- == 0)
			return -1;
		p.ntracks = buf[data++];

		for (int i = 0; i < p.ntracks; ++i)
		{
			if (n-- < 5)
				return -1;
			p.tracks[i].channel = buf[data++];
			uint s = read_uint32_le(buf, data);
			if (s < 4)
				return -1;
			if (s > n)
				return -1;
			p.tracks[i].seq.size = s - 4;
			p.tracks[i].seq.data = data + 4;
			n -= s;
			data += (int)s;
		}

		return n != 0 ? -1 : 0;
	}

	private static void gmext_write_int16(List<byte> midi, uint n)
	{
		midi.Add((byte)(n >> 8));
		midi.Add((byte)n);
	}

	private static void gmext_write_delta(List<byte> midi, uint delta)
	{
		Span<byte> data = stackalloc byte[4];
		int i = 0;

		delta &= ((1 << 28) - 1);
		do
		{
			data[i++] = (byte)(delta & 0x7F);
			delta >>= 7;
		} while (delta > 0 && i <= 3);

		while (--i != 0)
			midi.Add((byte)(data[i] | 0x80));

		midi.Add(data[0]);
	}

	private static void gmext_write_tempo_ev(List<byte> midi, uint tempo)
	{
		midi.Add(0xFF);
		midi.Add(0x51);
		midi.Add(3);
		tempo = 60000000 / tempo;
		midi.Add((byte)(tempo >> 16));
		midi.Add((byte)(tempo >> 8));
		midi.Add((byte)tempo);
	}

	private static void gmext_write_end_ev(List<byte> midi)
	{
		midi.Add(0xFF);
		midi.Add(0x2F);
		midi.Add(0);
	}

	private static readonly uint[] volume =
	[
		100,100,100,100,100, 90,100,100,100,100,100, 90,100,100,100,100,
		100,100, 85,100,100,100,100,100,100,100,100,100, 90,90, 110, 80,
		100,100,100, 90, 70,100,100,100,100,100,100,100,100,100,100,100,
		100,100, 90,100,100,100,100,100,100,120,100,100,100,120,100,127,
		100,100, 90,100,100,100,100,100,100, 95,100,100,100,100,100,100,
		100,100,100,100,100,100,100,115,100,100,100,100,100,100,100,100,
		100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,
		100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,
	];

	/// <summary>Output status.</summary>
	private struct output_status
	{
		public uint delta;
		public uint patch;
		public byte prevcmd;
	}

	private static int gmext_write_sequence(List<byte> midi, GmStream stream, uint channel, Seq seq, ref output_status status, ReadOnlySpan<byte> buf)
	{
		int data = seq.data;
		uint left = seq.size;

		byte cmd = unchecked((byte)-1);

		while (left != 0)
		{
			// read delta
			uint ndelta = 0;

			for (int i = 0; ;)
			{
				byte c = buf[data++];
				left--;
				ndelta += (uint)(c & 0x7F);
				if ((c & 0x80) == 0)
					break;
				if ((++i == 4) || left == 0)
					return -1;
				ndelta <<= 7;
			}

			status.delta += ndelta;

			// read cmd byte
			if (left == 0)
				return -1;

			if ((buf[data] & 0x80) != 0)
			{
				// actual cmd byte
				cmd = buf[data++];
				left--;
				switch (cmd)
				{
				case 0xFF:    // end track
				case 0xFD:    // end subsequence
					return 0;
				case 0xFE:    // insert subsequence
					if (left-- == 0)
						return -1;
					if (buf[data] >= stream.nsubs)
						// invalid subsequence
						return -1;
					if (gmext_write_sequence(midi,
						stream, channel,
						stream.subs[buf[data++]], ref status, buf)
							== -1)
						return -1;
					cmd = 0;
					continue;
				default:
					cmd &= 0xF0;
					break;
				}
			}
			else if (cmd == 0)
				return -1;    // invalid running mode

			if (left-- == 0)
				return -1;
			byte data1 = buf[data++];

			switch (cmd)
			{
			case 0x80:
			case 0x90:
				{
					if (left-- == 0)
						return -1;
					byte data2 = buf[data++];
					if (data2 != 0)
						data2 = (byte)((uint)data2 *
							(channel == 9 ? 80 : PatchVolume(status.patch)) >> 7);
					gmext_write_delta(midi, status.delta);
					midi.Add((byte)(cmd | channel));
					midi.Add(data1);
					midi.Add(data2);
				}
				break;

			case 0xC0:
				if (data1 == 0x7E)
					return 0;     // restart stream
				status.patch = data1;
				if ((data1 == 0x57) || (data1 == 0x3F))
					data1 = 0x3E;
				gmext_write_delta(midi, status.delta);
				midi.Add((byte)(cmd | channel));
				midi.Add(data1);
				break;

			case 0xB0:
				{
					if (left-- == 0)
						return -1;
					byte data2 = buf[data++];
					if (data1 == 0x7E)
						continue;
					if (data1 == 0)
					{
						if (data2 == 0)
							continue;
						gmext_write_delta(midi, status.delta);
						gmext_write_tempo_ev(midi, 2u * data2);
						break;
					}
					if (data1 == 0x5B)
						data2 = 0x1E;
					gmext_write_delta(midi, status.delta);
					midi.Add((byte)(cmd | channel));
					midi.Add(data1);
					midi.Add(data2);
				}
				break;

			case 0xE0:
				{
					if (left-- == 0)
						return -1;
					byte data2 = buf[data++];
					gmext_write_delta(midi, status.delta);
					midi.Add((byte)(cmd | channel));
					midi.Add(data1);
					midi.Add(data2);
				}
				break;

			default:      // unhandled cmd byte
				return -1;
			}

			status.delta = 0;
		}

		return 0;
	}

	/// <summary>Volume of a patch (C++ reads past the table for patches above 0x7F).</summary>
	private static uint PatchVolume(uint patch) => patch < volume.Length ? volume[patch] : 100;

	private static readonly byte[] midi_file_signature = [(byte)'M', (byte)'T', (byte)'h', (byte)'d', 0, 0, 0, 6];
	private static readonly byte[] midi_track_header = [(byte)'M', (byte)'T', (byte)'r', (byte)'k', 0, 0, 0, 11];
	private static readonly byte[] midi_track_init = [/* 0, 0xB0, */ 0x78, 0, 0, 0x79, 0, 0, 0x7B, 0];

	private static int gmext_write_midi(GmStream stream, List<byte> midi, ReadOnlySpan<byte> buf)
	{
		// write MIDI file header
		for (int i = 0; i < 8; ++i)
			midi.Add(midi_file_signature[i]);
		gmext_write_int16(midi, 1);
		gmext_write_int16(midi, (uint)(stream.ntracks + 1));
		gmext_write_int16(midi, 24);

		// write global tempo track
		for (int i = 0; i < 8; ++i)
			midi.Add(midi_track_header[i]);
		gmext_write_delta(midi, 0);
		gmext_write_tempo_ev(midi, (uint)stream.tempo);
		gmext_write_delta(midi, 0);
		gmext_write_end_ev(midi);

		// write tracks
		for (int j = 0; j < stream.ntracks; ++j)
		{
			// header
			for (int i = 0; i < 4; ++i)
				midi.Add(midi_track_header[i]);

			int loffset = midi.Count;
			for (int i = 0; i < 4; ++i)
				midi.Add(0);

			// initial data
			midi.Add(0);
			midi.Add((byte)(0xB0 | stream.tracks[j].channel));
			for (int i = 0; i < 8; ++i)
				midi.Add(midi_track_init[i]);

			// body
			var status = new output_status { delta = 0, patch = 0, prevcmd = 0 };
			if (gmext_write_sequence(midi, stream,
					stream.tracks[j].channel,
					stream.tracks[j].seq, ref status, buf) == -1)
				return -1;

			// end of track
			gmext_write_delta(midi, status.delta);
			gmext_write_end_ev(midi);

			// rewrite track length
			int length = midi.Count - loffset - 4;
			midi[loffset] = (byte)(length >> 24);
			midi[loffset + 1] = (byte)(length >> 16);
			midi[loffset + 2] = (byte)(length >> 8);
			midi[loffset + 3] = (byte)length;
		}

		return 0;
	}

	/// <summary>Converts a gm.cat item (name chunk + stream) into a standard MIDI file.</summary>
	/// <param name="item">The raw item.</param>
	/// <param name="midi">The MIDI file (also on a write error, then incomplete).</param>
	/// <returns>0 = ok, -1 = error reading the stream, -2 = error writing the MIDI file.</returns>
	internal static int ConvertToMidi(ReadOnlySpan<byte> item, out byte[] midi)
	{
		// stream info
		var stream = new GmStream();
		midi = [];
		int nameskip = (item.Length > 0 ? item[0] : 0) + 1;
		int size = item.Length;
		if (size < nameskip) // the unsigned C++ size wraps around here
			return -1;
		if (gmext_read_stream(stream, (uint)(size - nameskip), item, nameskip) == -1)
			return -1;

		var list = new List<byte>(65536);
		int result = gmext_write_midi(stream, list, item) == -1 ? -2 : 0;
		midi = [.. list];
		return result;
	}

	/// <summary>Loads a MIDI object into memory.</summary>
	/// <param name="i">Music number to load.</param>
	/// <returns>The loaded music (empty on a stream error).</returns>
	/// <exception cref="InvalidOperationException">The MIDI file cannot be played (e.g. no SoundFont).</exception>
	public Music LoadMIDI(uint i)
	{
		var music = new Music();
		var item = GetItem((int)Math.Min(i, int.MaxValue));

		if (i >= (uint)Size)
			throw new InvalidOperationException("Can't get GMcat object");

		int result = ConvertToMidi(item.Span, out byte[] midi);
		if (result == -1)
		{
			Logger.Error($"GMCatFile::loadMIDI({FileName}, {i}): Error reading MIDI stream");
			return music;
		}

		if (result == -2)
		{
			Logger.Error($"GMCatFile::loadMIDI({FileName}, {i}): Error writing MIDI stream");
		}
		else
		{
			music.Load(new MemoryStream(midi, false));
		}
		Logger.Verbose($"GMCatFile::loadMIDI({FileName}, {i}): loaded ok.");
		return music;
	}
}
