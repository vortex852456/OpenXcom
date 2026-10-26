// Golden values for Surface::loadImage: the decoding steps of src/Engine/Surface.cpp
// (lodepng first for .png, SDL_image otherwise, FixTransparent) on a plain pixel buffer.
// Output per file: <path> lodepng|sdl <width> <height> <transparent> <fnv(pixels)> <colors> <fnv(palette)>, or <path> error.
// The palette hash covers the colours defined by the file (GIF/BMP/LBM: see gifColors/bmpColors/lbmColors): r,g,b,alpha for lodepng (alpha decides the transparent
// index), r,g,b for SDL_image (the "unused" byte differs between SDL 1.2 and sdl12-compat).
// Build/run: port/tools/golden/generate.sh
#include <cstdio>
#include <cstdint>
#include <cstring>
#include <fstream>
#include <iterator>
#include <string>
#include <algorithm>
#include <vector>
#include <SDL.h>
#include <SDL_image.h>
#include "lodepng.h"

static uint64_t fnv(const uint8_t *data, size_t size)
{
	uint64_t h = 14695981039346656037ULL;
	for (size_t i = 0; i < size; ++i) { h ^= data[i]; h *= 1099511628211ULL; }
	return h;
}

// Colour table size of the first GIF image (sdl12-compat reports 256 palette entries).
static int gifColors(const std::vector<unsigned char> &d)
{
	if (d.size() < 13) return 0;
	int global = (d[10] & 0x80) ? 2 << (d[10] & 7) : 0;
	size_t pos = 13 + 3 * global;
	while (pos < d.size())
	{
		unsigned char c = d[pos++];
		if (c == ',')
			return (pos + 8 < d.size() && (d[pos + 8] & 0x80)) ? 2 << (d[pos + 8] & 7) : global;
		if (c == '!')
		{
			++pos;
			while (pos < d.size() && d[pos] != 0) pos += d[pos] + 1;
			++pos;
		}
	}
	return 0;
}

// Palette entries an SDL_image 1.2 BMP/LBM load defines from the file; the others keep the initial colour of a new
// palette (black in SDL 1.2, white in sdl12-compat) and are left out of the hash.
static int bmpColors(const std::vector<unsigned char> &d, int ncolors)
{
	if (d.size() < 18) return 0;
	uint32_t biSize = d[14] | d[15] << 8 | d[16] << 16 | (uint32_t)d[17] << 24;
	size_t entry = biSize == 12 ? 3 : 4;
	size_t start = 14 + biSize;
	size_t avail = d.size() > start ? (d.size() - start) / entry : 0;
	return (int)std::min<size_t>(avail, ncolors);
}

static int lbmColors(const std::vector<unsigned char> &d, int ncolors)
{
	bool pbm = d.size() >= 12 && memcmp(&d[8], "PBM ", 4) == 0;
	if (!pbm) return ncolors; // ILBM: ncolors = 1 << planes, all defined (the colour map repeats)
	int n = 0;
	for (size_t pos = 12; pos + 8 <= d.size();)
	{
		uint32_t size = (uint32_t)d[pos + 4] << 24 | d[pos + 5] << 16 | d[pos + 6] << 8 | d[pos + 7];
		if (memcmp(&d[pos], "BODY", 4) == 0) break;
		if (memcmp(&d[pos], "CMAP", 4) == 0) n = size / 3;
		pos += 8 + size + (size & 1);
	}
	// PBM counts as one plane: no EHB, the colour map is repeated up to 2 colours
	return n == 0 ? 0 : std::min(std::max(n, 2), ncolors);
}

static bool isPng(const std::string &name)
{
	auto dot = name.rfind('.');
	return dot != std::string::npos && strcasecmp(name.c_str() + dot + 1, "png") == 0;
}

int main(int argc, char **argv)
{
	for (int a = 1; a < argc; ++a)
	{
		std::string filename = argv[a];
		std::ifstream in(filename, std::ios::binary);
		std::vector<unsigned char> png((std::istreambuf_iterator<char>(in)), std::istreambuf_iterator<char>());
		int width = 0, height = 0, transparent = 0, colors = 0;
		bool loaded = false;
		std::vector<uint8_t> pixels;
		SDL_Color palette[256];
		memset(palette, 0, sizeof(palette)); // new 8-bit SDL surface

		if (isPng(filename) && png.size() > 8 + 12 + 12)
		{
			std::vector<unsigned char> image;
			unsigned w, h;
			lodepng::State state;
			state.decoder.color_convert = 0;
			unsigned error = lodepng::decode(image, w, h, state, png);
			if (!error && lodepng_get_bpp(&state.info_png.color) == 8)
			{
				width = w;
				height = h;
				if (state.info_png.color.palettesize) memcpy(palette, state.info_png.color.palette, state.info_png.color.palettesize * 4);
				colors = state.info_png.color.palettesize;
				pixels.assign(image.begin(), image.end());
				for (int c = 0; c < 256; ++c)
				{
					if (palette[c].unused == 0) { transparent = c; break; }
				}
				loaded = true;
			}
		}
		if (!loaded)
		{
			SDL_Surface *surface = IMG_Load_RW(SDL_RWFromMem(png.data(), (int)png.size()), 1);
			if (!surface || surface->format->BitsPerPixel != 8)
			{
				printf("%s error\n", filename.c_str());
				if (surface) SDL_FreeSurface(surface);
				continue;
			}
			width = surface->w;
			height = surface->h;
			memcpy(palette, surface->format->palette->colors, surface->format->palette->ncolors * sizeof(SDL_Color));
			colors = surface->format->palette->ncolors;
			if (png.size() >= 3 && memcmp(png.data(), "GIF", 3) == 0) colors = gifColors(png);
			else if (png.size() >= 2 && memcmp(png.data(), "BM", 2) == 0) colors = bmpColors(png, colors);
			else if (png.size() >= 4 && memcmp(png.data(), "FORM", 4) == 0) colors = lbmColors(png, colors);
			for (int y = 0; y < height; ++y)
			{
				const uint8_t *row = (const uint8_t *)surface->pixels + y * surface->pitch;
				pixels.insert(pixels.end(), row, row + width);
			}
			transparent = surface->format->colorkey;
			SDL_FreeSurface(surface);
		}
		if (transparent != 0)
		{
			for (auto &p : pixels) if (p == transparent) p = 0;
		}
		std::vector<uint8_t> pal;
		for (int c = 0; c < colors; ++c)
		{
			pal.insert(pal.end(), { palette[c].r, palette[c].g, palette[c].b });
			if (loaded) pal.push_back(palette[c].unused);
		}
		printf("%s %s %d %d %d %016llx %d %016llx\n", filename.c_str(), loaded ? "lodepng" : "sdl", width, height, transparent,
			(unsigned long long)fnv(pixels.data(), pixels.size()), colors, (unsigned long long)fnv(pal.data(), pal.size()));
	}
	return 0;
}
