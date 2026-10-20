// Golden reference for the Adlib emulation (src/Engine/Adlib/fmopl.cpp, adlplayer.cpp and the
// AdlibMusic::player loop). Build/run: port/tools/golden/generate.sh
// Output (adlib.txt), replayed by AdlibTests.cs:
//   noise <seed>                     seed of RNG::seedless (rhythm white noise)
//   create <rate>                    OPLCreate(YM3812, 3579545, rate)
//   write <reg> <value> <irq>        OPLWrite address + data port, irq = result of the data write
//   read <port> <value>              OPLRead
//   timer <c> <irq>                  OPLTimerOver
//   render <length> <stripe> <volume> <hex int16 samples>   YM3812UpdateOne on a zeroed buffer
//   destroy
//   song <rate> <musicVolume> <volume> <alwaysLoop> <hex data>   AdlibMusic(volume) + load + play
//   block <len> <fnv64 of the bytes> <playing> <polyphony>    one AdlibMusic::player call on a zeroed buffer
//   fade                             func_fade()
#include <cmath>
#include <cstdio>
#include <cstdint>
#include <map>
#include <vector>
#include "Engine/RNG.h"
#include "Engine/Adlib/fmopl.h"
#include "Engine/Adlib/adlplayer.h"

namespace OpenXcom { namespace RNG { extern RandomState x_seedless; } }
extern FM_OPL* opl[2];

using namespace OpenXcom;

static FILE *f;
static FM_OPL *chip;

static void wr(int r, int v)
{
	OPLWrite(chip, 0, r);
	int irq = OPLWrite(chip, 1, v);
	fprintf(f, "write %d %d %d\n", r, v, irq);
}

static void rd(int a)
{
	fprintf(f, "read %d %d\n", a, OPLRead(chip, a));
}

static void timer(int c)
{
	fprintf(f, "timer %d %d\n", c, OPLTimerOver(chip, c));
}

static void render(int length, int stripe, float volume)
{
	std::vector<INT16> buf(length, 0);
	YM3812UpdateOne(chip, buf.data(), length, stripe, volume);
	fprintf(f, "render %d %d %.9g ", length, stripe, volume);
	for (INT16 s : buf) fprintf(f, "%04x", (unsigned)(uint16_t)s);
	fprintf(f, "\n");
}

static void registers()
{
	const uint64_t seed = 12345;
	RNG::x_seedless = RNG::RandomState(seed);
	fprintf(f, "noise %llu\n", (unsigned long long)seed);
	fprintf(f, "create 22050\n");
	chip = OPLCreate(OPL_TYPE_YM3812, 3579545, 22050);
	rd(0);
	// channel 0: FM with feedback, waveform select enabled
	wr(0x01, 0x20);
	const int ch0[][2] = { {0x20, 0x01}, {0x23, 0x01}, {0x40, 0x10}, {0x43, 0x00}, {0x60, 0xF2}, {0x63, 0xF4},
		{0x80, 0x77}, {0x83, 0x77}, {0xE0, 0x00}, {0xE3, 0x00}, {0xC0, 0x0E}, {0xA0, 0x98}, {0xB0, 0x31} };
	for (auto &w : ch0) wr(w[0], w[1]);
	render(1024, 1, 1.0f);
	// channel 1: additive, AM/vibrato, other waveforms (the cached LFO depth only changes on a chip switch)
	wr(0xBD, 0xC0);
	const int ch1[][2] = { {0x21, 0xC2}, {0x24, 0x41}, {0x41, 0x08}, {0x44, 0x04}, {0x61, 0xA3}, {0x64, 0xC5},
		{0x81, 0x25}, {0x84, 0x36}, {0xE1, 0x01}, {0xE4, 0x02}, {0xC1, 0x07}, {0xA1, 0x41}, {0xB1, 0x2E} };
	for (auto &w : ch1) wr(w[0], w[1]);
	render(1024, 1, 0.5f);
	wr(0xB0, 0x11); // key off channel 0 (release)
	wr(0xE1, 0x03);
	render(2048, 2, 1.0f);
	// channel 2: KSL, envelope type, KSR, highest block, note select
	wr(0x08, 0x40);
	const int ch2[][2] = { {0x22, 0x30}, {0x25, 0x21}, {0x42, 0xC5}, {0x45, 0x80}, {0x62, 0xFF}, {0x65, 0x8F},
		{0x82, 0x0F}, {0x85, 0xF1}, {0xC2, 0x05}, {0xA2, 0xFF}, {0xB2, 0x3F} };
	for (auto &w : ch2) wr(w[0], w[1]);
	render(1024, 1, 1.0f);
	wr(0x01, 0x00); // waveform select off: all slots back to sine
	render(512, 1, 1.0f);
	// rhythm section (channels 6-8)
	wr(0x01, 0x20);
	for (int off = 0x10; off <= 0x15; ++off)
	{
		wr(0x20 + off, 0x01 + (off & 3));
		wr(0x40 + off, 0x04 * (off - 0x10));
		wr(0x60 + off, 0xF8 - (off - 0x10));
		wr(0x80 + off, 0x47 + 0x10 * (off - 0x10));
		wr(0xE0 + off, off & 3);
	}
	const int rh[][2] = { {0xC6, 0x04}, {0xA6, 0x57}, {0xB6, 0x09}, {0xA7, 0x03}, {0xB7, 0x0A}, {0xA8, 0x57}, {0xB8, 0x09}, {0xBD, 0xFF} };
	for (auto &w : rh) wr(w[0], w[1]);
	render(2048, 1, 1.0f);
	wr(0xBD, 0xE5);
	render(1024, 1, 0.25f);
	// status, timers, CSM mode
	wr(0x02, 0xF0);
	wr(0x04, 0x01);
	rd(0);
	timer(0);
	rd(0);
	timer(1);
	wr(0x04, 0x80);
	rd(0);
	rd(1);
	wr(0x08, 0x80);
	wr(0x40, 0x3F);
	timer(0);
	render(512, 1, 1.0f);
	fprintf(f, "destroy\n");
	OPLDestroy(chip);
	chip = 0;
}

// --- AdlibMusic::player, with the game options as plain variables ---

static int delay = 0, rate = 0;
static std::map<int, int> delayRates;
static int musicVolume;
static bool musicAlwaysLoop;

struct Track
{
	std::vector<unsigned char> data;
	float volume;
};

static double volumeExponent(int volume)
{
	return (exp(log(10.0 + 1.0) * volume / (double)128) - 1.0) / 10.0;
}

// AdlibMusic::play: Music::stop(), then set up the player
static void play(Track *music)
{
	func_mute();
	func_setup_music(music->data.data(), music->data.size());
	func_set_music_volume(127 * music->volume);
}

static void player(Track *music, unsigned char *stream, int len)
{
	if (musicVolume == 0)
		return;
	if (musicAlwaysLoop && !func_is_music_playing())
	{
		play(music);
		return;
	}
	while (len != 0)
	{
		if (!opl[0] || !opl[1])
			return;
		int i = std::min(delay, len);
		if (i)
		{
			float volume = volumeExponent(musicVolume);
			YM3812UpdateOne(opl[0], (INT16*)stream, i / 2, 2, volume);
			YM3812UpdateOne(opl[1], ((INT16*)stream) + 1, i / 2, 2, volume);
			stream += i;
			delay -= i;
			len -= i;
		}
		if (!len)
			return;
		func_play_tick();

		delay = delayRates[rate];
	}
}

static uint64_t fnv(const unsigned char *p, size_t n)
{
	uint64_t h = 14695981039346656037ULL;
	for (size_t i = 0; i < n; ++i) { h ^= p[i]; h *= 1099511628211ULL; }
	return h;
}

static void song(const std::vector<unsigned char> &data, int sampleRate, int volumeOption, float volume, bool alwaysLoop, int blocks, int fadeAt)
{
	musicVolume = volumeOption;
	musicAlwaysLoop = alwaysLoop;
	// AdlibMusic::AdlibMusic
	rate = sampleRate;
	if (!opl[0]) opl[0] = OPLCreate(OPL_TYPE_YM3812, 3579545, rate);
	if (!opl[1]) opl[1] = OPLCreate(OPL_TYPE_YM3812, 3579545, rate);
	if (delayRates.empty())
	{
		delayRates[8000] = 114 * 4;
		delayRates[11025] = 157 * 4;
		delayRates[16000] = 228 * 4;
		delayRates[22050] = 314 * 4;
		delayRates[32000] = 456 * 4;
		delayRates[44100] = 629 * 4;
		delayRates[48000] = 685 * 4;
	}
	Track track = { data, volume };
	fprintf(f, "song %d %d %.9g %d ", sampleRate, volumeOption, volume, alwaysLoop ? 1 : 0);
	for (unsigned char c : data) fprintf(f, "%02x", c);
	fprintf(f, "\n");
	play(&track);
	const int len = 1024;
	std::vector<unsigned char> buf(len);
	for (int b = 0; b < blocks; ++b)
	{
		if (b == fadeAt)
		{
			fprintf(f, "fade\n");
			func_fade();
		}
		std::fill(buf.begin(), buf.end(), 0);
		player(&track, buf.data(), len);
		fprintf(f, "block %d %016llx %d %d\n", len, (unsigned long long)fnv(buf.data(), len), func_is_music_playing() ? 1 : 0, func_get_polyphony());
	}
}

static void sample(std::vector<unsigned char> &d, std::initializer_list<int> regs)
{
	for (int r : regs) d.push_back(r);
	for (size_t i = regs.size(); i < 24; ++i) d.push_back(0);
}

static void block(std::vector<unsigned char> &d, std::initializer_list<int> head, std::initializer_list<int> body)
{
	size_t len = 2 + head.size() + body.size(); // the 16-bit length counts itself and the header
	d.push_back(len & 0xff);
	d.push_back(len >> 8);
	for (int c : head) d.push_back(c);
	for (int c : body) d.push_back(c);
}

static void songs()
{
	// new format (with title): name, tempo, samples, subtracks, instruments with their number
	std::vector<unsigned char> s1 = { 4, 'T', 'E', 'S', 'T', 120, 3 };
	sample(s1, { 0x01, 0x01, 0x8F, 0x00, 0xF1, 0xF3, 0x53, 0x74, 0x00, 0x00, 0x0B });
	sample(s1, { 0x21, 0xA2, 0x52, 0x00, 0xC3, 0xA4, 0x24, 0x45, 0x01, 0x02, 0x06 });
	sample(s1, { 0xE1, 0x61, 0x3A, 0x00, 0xF6, 0xF5, 0x17, 0x16, 0x03, 0x01, 0x01 });
	s1.push_back(1); // subtracks
	block(s1, { 0, 0 }, { 0x02, 0x90, 0x43, 0x60, 0x03, 0x80, 0x43, 0x00, 0x00, 0xFD });
	s1.push_back(2); // instruments
	block(s1, { 0, 0, 0 }, { 0x01, 0xC0, 0x00, 0x00, 0xB0, 0x07, 0x7F, 0x00, 0xE0, 0x10, 0x00, 0x90, 0x3C, 0x64, 0x03,
		0x3E, 0x50, 0x03, 0x80, 0x3C, 0x00, 0x00, 0x3E, 0x00, 0x02, 0xE0, 0x14, 0x00, 0x90, 0x40, 0x70, 0x04,
		0xFE, 0x00, 0x01, 0x80, 0x40, 0x00, 0x01, 0xE0, 0x0C, 0x00, 0x90, 0x37, 0x60, 0x02, 0x80, 0x37, 0x00, 0x05, 0xFF });
	block(s1, { 0, 0, 1 }, { 0x02, 0xC1, 0x01, 0x00, 0xB1, 0x7E, 0x03, 0x00, 0xB1, 0x00, 0x64, 0x00, 0xB1, 0x07, 0x60, 0x00,
		0x91, 0x30, 0x7F, 0x06, 0x81, 0x30, 0x00, 0x02, 0xE1, 0x11, 0x00, 0x91, 0x34, 0x7F, 0x06, 0x91, 0x34, 0x00, 0x01,
		0xB1, 0x7F, 0x00, 0x00, 0x91, 0x37, 0x50, 0x05, 0x81, 0x37, 0x00, 0x00, 0xC1, 0x7E, 0x01 });
	song(s1, 11025, 100, 0.8f, false, 64, -1);

	// old format (no title): the first byte is the tempo (> 56); the track ends, loops by option and fades out
	std::vector<unsigned char> s2 = { 100, 1 };
	sample(s2, { 0xE1, 0x61, 0x3A, 0x00, 0xF6, 0xF5, 0x17, 0x16, 0x03, 0x01, 0x01 });
	s2.push_back(0); // subtracks
	s2.push_back(1); // instruments
	block(s2, { 0, 0 }, { 0x00, 0xC0, 0x00, 0x00, 0x90, 0x48, 0x7F, 0x02, 0x90, 0x4C, 0x7F, 0x02, 0x90, 0x4F, 0x7F, 0x04,
		0x80, 0x48, 0x00, 0x00, 0x80, 0x4C, 0x00, 0x00, 0x80, 0x4F, 0x00, 0x01, 0xFF });
	song(s2, 11025, 128, 0.25f, true, 48, 20);
}

int main(int argc, char **argv)
{
	if (argc != 2) { fprintf(stderr, "usage: adlib <outfile>\n"); return 1; }
	f = fopen(argv[1], "w");
	registers();
	songs();
	fclose(f);
	return 0;
}
