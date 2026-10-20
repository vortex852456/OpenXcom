// Golden values for Surface::loadImage: the decoding steps of src/Engine/Surface.cpp
// (lodepng first for .png, SDL_image otherwise, FixTransparent) on a plain pixel buffer.
// Output per file: <path> lodepng|sdl <width> <height> <transparent> <fnv(pixels)> <colors> <fnv(palette)>, or <path> error.
// The palette hash covers the colours defined by the file: r,g,b,alpha for lodepng (alpha decides the transparent
// index), r,g,b for SDL_image (the "unused" byte differs between SDL 1.2 and sdl12-compat).
// Build/run: port/tools/golden/generate.sh
#include <cstdio>
#include <cstdint>
#include <cstring>
#include <fstream>
#include <iterator>
#include <string>
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
			colors = png.size() >= 3 && memcmp(png.data(), "GIF", 3) == 0 ? gifColors(png) : surface->format->palette->ncolors;
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
