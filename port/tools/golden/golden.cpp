// Generates golden reference values from the original C++ sources.
// The C# tests compare the port against these files bit by bit.
// Build/run: port/tools/golden/generate.sh
#include <cstdio>
#include <cinttypes>
#include <string>
#include <vector>
#include "Engine/RNG.h"
#include "Engine/LanguagePlurality.h"
#include "Engine/ShaderDraw.h"

using namespace OpenXcom;

static void rng(FILE *f)
{
	const uint64_t seeds[] = { 1ULL, 42ULL, 0xDEADBEEFULL, 0x0123456789ABCDEFULL, 0xFFFFFFFFFFFFFFFFULL };
	for (uint64_t seed : seeds)
	{
		fprintf(f, "seed %" PRIu64 "\n", seed);
		RNG::RandomState s(seed);
		fprintf(f, "next");
		for (int i = 0; i < 16; ++i) fprintf(f, " %" PRIu64, s.next());
		fprintf(f, "\n");
		const int ranges[][2] = { {0, 99}, {1, 6}, {-50, 50}, {-1000000, -999990}, {0, 0}, {0, 2147483646} };
		for (auto& r : ranges)
		{
			fprintf(f, "generate %d %d", r[0], r[1]);
			for (int i = 0; i < 16; ++i) fprintf(f, " %d", s.generate(r[0], r[1]));
			fprintf(f, "\n");
		}
		fprintf(f, "percent");
		for (int i = 0; i < 16; ++i) fprintf(f, " %d", s.percent(37) ? 1 : 0);
		fprintf(f, "\n");
		RNG::RandomState sub = s.subSequence();
		fprintf(f, "subseq %" PRIu64 " %" PRIu64 "\n", sub.getSeed(), s.getSeed());

		RNG::setSeed(seed);
		fprintf(f, "double");
		for (int i = 0; i < 8; ++i) fprintf(f, " %.17g", RNG::generate(-2.5, 7.25));
		fprintf(f, "\n");
		std::vector<int> v;
		for (int i = 0; i < 12; ++i) v.push_back(i);
		RNG::shuffle(v);
		fprintf(f, "shuffle");
		for (int x : v) fprintf(f, " %d", x);
		fprintf(f, "\nend %" PRIu64 "\n", RNG::getSeed());
	}
}

static void plurality(FILE *f)
{
	const char *langs[] = { "en-US", "de", "fr", "fr-CA", "hu", "tr", "cs", "pl", "ro", "ru", "sk", "uk", "ja", "ko", "zh-CN", "zh-TW", "hr" };
	for (const char *lang : langs)
	{
		LanguagePlurality *p = LanguagePlurality::create(lang);
		fprintf(f, "%s", lang);
		for (unsigned n = 0; n <= 230; ++n) fprintf(f, " %s", p->getSuffix(n) + 1);
		const unsigned big[] = { 1000, 1001, 1011, 1021, 1112, 4294967295u };
		for (unsigned n : big) fprintf(f, " %s", p->getSuffix(n) + 1);
		fprintf(f, "\n");
		delete p;
	}
}

static void shade(FILE *f)
{
	for (int sh = 0; sh < 16; ++sh)
	{
		fprintf(f, "standard %d ", sh);
		for (int src = 0; src < 256; ++src)
		{
			Uint8 dest = 0xAB;
			helper::StandardShade::func(dest, (Uint8)src, sh);
			fprintf(f, "%02x", dest);
		}
		fprintf(f, "\n");
	}
	const int baseColors[] = { 1, 5, 16 }; // blitNShade newBaseColor argument (1-based colour group)
	for (int base : baseColors)
	{
		const int newColor = (base - 1) << 4;
		for (int sh = 0; sh < 16; ++sh)
		{
			fprintf(f, "replace %d %d ", base, sh);
			for (int src = 0; src < 256; ++src)
			{
				Uint8 dest = 0xAB;
				helper::ColorReplace::func(dest, (Uint8)src, sh, newColor);
				fprintf(f, "%02x", dest);
			}
			fprintf(f, "\n");
		}
	}
}

int main(int argc, char **argv)
{
	if (argc != 2) { fprintf(stderr, "usage: golden <outdir>\n"); return 1; }
	std::string dir = argv[1];
	FILE *f;
	f = fopen((dir + "/rng.txt").c_str(), "w"); rng(f); fclose(f);
	f = fopen((dir + "/plurality.txt").c_str(), "w"); plurality(f); fclose(f);
	f = fopen((dir + "/shade.txt").c_str(), "w"); shade(f); fclose(f);
	return 0;
}
