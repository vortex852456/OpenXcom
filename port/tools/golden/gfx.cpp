// Golden values for the SDL_gfx primitives behind Surface::drawLine/drawCircle/drawPolygon/
// drawTexturedPolygon/drawString (src/Engine/Surface.cpp) on an 8-bit surface with colour key 0.
// The palette is SDL's 3-3-2 colour cube, so colour matching (SDL_MapRGBA, the RGBA->8 bit blit of
// the font) maps every index to itself and the result shows the raw pixel output of SDL_gfx.
// Output per case: <op> <arguments...> <fnv(pixels)>; the C# test replays the arguments.
// Build/run: port/tools/golden/generate.sh
#include <cstdio>
#include <cstdint>
#include <cstring>
#include <string>
#include <vector>
#include <SDL.h>
#include <SDL_gfxPrimitives.h>

static const int W = 40, H = 30;
static SDL_Color palette[256];

static uint64_t fnv(const SDL_Surface *s)
{
	uint64_t h = 14695981039346656037ULL;
	for (int y = 0; y < s->h; ++y)
	{
		const uint8_t *row = (const uint8_t *)s->pixels + y * s->pitch;
		for (int x = 0; x < s->w; ++x) { h ^= row[x]; h *= 1099511628211ULL; }
	}
	return h;
}

static SDL_Surface *newSurface(int w, int h)
{
	SDL_Surface *s = SDL_CreateRGBSurface(SDL_SWSURFACE, w, h, 8, 0, 0, 0, 0);
	SDL_SetColors(s, palette, 0, 256);
	SDL_SetColorKey(s, SDL_SRCCOLORKEY, 0);
	return s;
}

// Background: every pixel distinct from its neighbours, with some transparent (0) pixels.
static void fill(SDL_Surface *s)
{
	for (int y = 0; y < s->h; ++y)
		for (int x = 0; x < s->w; ++x)
			((uint8_t *)s->pixels)[y * s->pitch + x] = (uint8_t)((x * 7 + y * 13) % 251);
}

// Palette::getRGBA
static Uint32 rgba(Uint8 color)
{
	return ((Uint32)palette[color].r << 24) | ((Uint32)palette[color].g << 16) | ((Uint32)palette[color].b << 8) | 0xFF;
}

static uint32_t seed = 12345;
static int rnd(int min, int max)
{
	seed = seed * 1103515245u + 12345u;
	return min + (int)((seed >> 8) % (uint32_t)(max - min + 1));
}

static void line(SDL_Surface *s, int x1, int y1, int x2, int y2, int color)
{
	fill(s);
	lineColor(s, x1, y1, x2, y2, rgba(color));
	printf("line %d %d %d %d %d %016llx\n", x1, y1, x2, y2, color, (unsigned long long)fnv(s));
}

static void circle(SDL_Surface *s, int x, int y, int r, int color)
{
	fill(s);
	filledCircleColor(s, x, y, r, rgba(color));
	printf("circle %d %d %d %d %016llx\n", x, y, r, color, (unsigned long long)fnv(s));
}

static void polygon(SDL_Surface *s, const std::vector<Sint16> &vx, const std::vector<Sint16> &vy, int color)
{
	fill(s);
	filledPolygonColor(s, vx.data(), vy.data(), (int)vx.size(), rgba(color));
	printf("polygon %d", (int)vx.size());
	for (size_t i = 0; i < vx.size(); ++i) printf(" %d %d", vx[i], vy[i]);
	printf(" %d %016llx\n", color, (unsigned long long)fnv(s));
}

static void textured(SDL_Surface *s, SDL_Surface *texture, const std::vector<Sint16> &vx, const std::vector<Sint16> &vy, int dx, int dy)
{
	fill(s);
	texturedPolygon(s, vx.data(), vy.data(), (int)vx.size(), texture, dx, dy);
	printf("textured %d", (int)vx.size());
	for (size_t i = 0; i < vx.size(); ++i) printf(" %d %d", vx[i], vy[i]);
	printf(" %d %d %016llx\n", dx, dy, (unsigned long long)fnv(s));
}

static void str(SDL_Surface *s, int x, int y, const std::string &text, int color)
{
	fill(s);
	stringColor(s, x, y, text.c_str(), rgba(color));
	printf("string %d %d %d ", x, y, color);
	if (text.empty()) printf("-");
	for (unsigned char c : text) printf("%02x", c);
	printf(" %016llx\n", (unsigned long long)fnv(s));
}

static void randomPolygon(std::vector<Sint16> &vx, std::vector<Sint16> &vy, int n)
{
	vx.clear();
	vy.clear();
	for (int i = 0; i < n; ++i)
	{
		vx.push_back(rnd(-20, W + 20));
		vy.push_back(rnd(-20, H + 20));
	}
}

int main()
{
	// SDL_DitherColors for 8 bpp (3-3-2)
	for (int i = 0; i < 256; ++i)
	{
		int r = i & 0xe0; r |= r >> 3 | r >> 6;
		int g = (i << 3) & 0xe0; g |= g >> 3 | g >> 6;
		int b = i & 0x3; b |= b << 2; b |= b << 4;
		palette[i].r = r; palette[i].g = g; palette[i].b = b; palette[i].unused = 0;
	}
	printf("size %d %d\n", W, H);
	SDL_Surface *s = newSurface(W, H);

	// Lines: special cases, clipping, then random ones.
	line(s, 5, 5, 5, 5, 200);
	line(s, 3, 7, 30, 7, 201);
	line(s, 30, 8, 3, 8, 202);
	line(s, 9, 2, 9, 25, 203);
	line(s, 10, 25, 10, 2, 204);
	line(s, -10, 3, 50, 3, 205);
	line(s, 4, -10, 4, 50, 206);
	line(s, -100, -50, 150, 90, 207);
	line(s, 39, 0, 0, 29, 208);
	line(s, 0, 0, 39, 29, 209);
	line(s, -1000, 15, 1000, 16, 210);
	line(s, 20, -1000, 21, 1000, 211);
	line(s, -30, -30, -5, -1, 212);
	line(s, 50, 40, 60, 10, 213);
	for (int i = 0; i < 400; ++i)
		line(s, rnd(-40, 80), rnd(-30, 60), rnd(-40, 80), rnd(-30, 60), rnd(1, 255));
	for (int i = 0; i < 100; ++i)
		line(s, rnd(0, W - 1), rnd(0, H - 1), rnd(0, W - 1), rnd(0, H - 1), rnd(1, 255));

	// Filled circles.
	circle(s, 20, 15, 0, 100);
	circle(s, 20, 15, -1, 101);
	circle(s, 20, 15, 1, 102);
	circle(s, 20, 15, 2, 103);
	circle(s, 20, 15, 10, 104);
	circle(s, 20, 15, 40, 105);
	circle(s, -5, -5, 8, 106);
	circle(s, 60, 15, 19, 107);
	circle(s, 60, 15, 20, 108);
	circle(s, 0, 0, 0, 109);
	for (int i = 0; i < 150; ++i)
		circle(s, rnd(-20, 60), rnd(-20, 50), rnd(0, 30), rnd(1, 255));

	// Filled polygons.
	std::vector<Sint16> vx, vy;
	polygon(s, { 5, 30, 15 }, { 5, 8, 25 }, 50);
	polygon(s, { 5, 30, 30, 5 }, { 5, 5, 20, 20 }, 51);
	polygon(s, { 5, 30, 15 }, { 10, 10, 10 }, 52);
	polygon(s, { 5, 30 }, { 10, 20 }, 53);
	polygon(s, { -10, 50, 20 }, { -10, 0, 45 }, 54);
	polygon(s, { 2, 20, 38, 20 }, { 15, 2, 15, 28 }, 55);
	polygon(s, { 5, 35, 5, 35 }, { 5, 25, 25, 5 }, 56); // self-intersecting
	for (int i = 0; i < 150; ++i)
	{
		randomPolygon(vx, vy, rnd(3, 8));
		polygon(s, vx, vy, rnd(1, 255));
	}

	// Textured polygons: 7x5 texture with transparent pixels.
	SDL_Surface *texture = newSurface(7, 5);
	for (int y = 0; y < 5; ++y)
		for (int x = 0; x < 7; ++x)
			((uint8_t *)texture->pixels)[y * texture->pitch + x] = (uint8_t)((x + y) % 4 == 0 ? 0 : 100 + x * 10 + y);
	printf("texture 7 5");
	for (int y = 0; y < 5; ++y)
		for (int x = 0; x < 7; ++x)
			printf(" %d", ((uint8_t *)texture->pixels)[y * texture->pitch + x]);
	printf("\n");
	textured(s, texture, { 5, 30, 15 }, { 5, 8, 25 }, 0, 0);
	textured(s, texture, { 5, 30, 30, 5 }, { 5, 5, 20, 20 }, 3, -2);
	textured(s, texture, { -10, 50, 20 }, { -10, 0, 45 }, -17, 23);
	textured(s, texture, { 50, 60, 55 }, { 5, 5, 10 }, 0, 0); // outside
	textured(s, texture, { 5, 30, 15 }, { 40, 45, 50 }, 0, 0); // below
	for (int i = 0; i < 100; ++i)
	{
		randomPolygon(vx, vy, rnd(3, 6));
		textured(s, texture, vx, vy, rnd(-20, 20), rnd(-20, 20));
	}

	// Strings with the built-in 8x8 font.
	str(s, 0, 0, "Hello", 60);
	str(s, 3, 11, "AZaz09!~", 61);
	str(s, -4, -3, "clip", 62);
	str(s, 30, 25, "edge", 63);
	str(s, 0, 0, "", 64);
	str(s, 2, 2, "\x01\x7f\x80\xc4\xe9\xff", 65);
	for (int i = 0; i < 20; ++i)
	{
		std::string text;
		int n = rnd(1, 6);
		for (int c = 0; c < n; ++c) text += (char)rnd(1, 255);
		str(s, rnd(-10, W), rnd(-10, H), text, rnd(1, 255));
	}

	SDL_FreeSurface(texture);
	SDL_FreeSurface(s);
	return 0;
}
