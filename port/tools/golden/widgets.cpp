// Golden values for the Interface widgets: Text layout/drawing with the fonts of bin/common/Language
// (FONT_BIG, FONT_SMALL) and the drawing of TextButton, ToggleTextButton, Window, Frame, Bar,
// ProgressBar, ArrowButton, NumberText and Cursor, compiled from the original sources.
// Only the code paths the cases need are linked; the rest of the game is left unresolved.
// Output per case: <widget> <arguments...> [<metrics...>] <fnv(pixels)>; the C# test replays the arguments.
// Build/run: port/tools/golden/generate.sh
#include <cstdio>
#include <cstdint>
#include <cstring>
#include <cmath>
#include <algorithm>
#include <fstream>
#include <iterator>
#include <map>
#include <sstream>
#include <string>
#include <unordered_map>
#include <vector>
#include <SDL.h>
#include "lodepng.h"
#include "Engine/Yaml.h"
#include "Engine/FileMap.h"
#include "Engine/State.h"
#include "Engine/Surface.h"
#include "Engine/InteractiveSurface.h"
#include "Engine/Language.h"
#include "Engine/Options.h"
#include "Engine/Action.h"
#include "Engine/Timer.h"
#include "Engine/Unicode.h"
#include "Engine/RNG.h"
// The harness fills fonts and reads the processed text directly.
#define private public
#define protected public
#include "Engine/Font.h"
#include "Interface/Text.h"
#include "Interface/TextButton.h"
#include "Interface/ToggleTextButton.h"
#include "Interface/Window.h"
#include "Interface/Frame.h"
#include "Interface/Bar.h"
#include "Interface/ProgressBar.h"
#include "Interface/ArrowButton.h"
#include "Interface/NumberText.h"
#include "Interface/Cursor.h"
#undef private
#undef protected

using namespace OpenXcom;

// The parts of the game the widgets touch (not linked from the original).
namespace OpenXcom
{
namespace Options { bool debugUi = false; int maxFrameSkip = 8; }
Game *State::_game = 0;
static TextDirection g_direction = DIRECTION_LTR;
static TextWrapping g_wrap = WRAP_WORDS;
Language::Language() : _handler(0), _direction(g_direction), _wrap(g_wrap) {}
Language::~Language() {}
TextDirection Language::getTextDirection() const { return _direction; }
TextWrapping Language::getTextWrapping() const { return _wrap; }
}

static uint64_t fnv(Surface &s)
{
	uint64_t h = 14695981039346656037ULL;
	for (int y = 0; y < s.getHeight(); ++y)
		for (int x = 0; x < s.getWidth(); ++x) { h ^= s.getPixel(x, y); h *= 1099511628211ULL; }
	return h;
}

static std::string hex(const std::string &s)
{
	if (s.empty()) return "-";
	std::string r;
	char b[3];
	for (unsigned char c : s) { snprintf(b, sizeof(b), "%02x", c); r += b; }
	return r;
}

/// Loads an 8-bit PNG like Surface::loadImage (lodepng path: the first palette entry with alpha 0 becomes index 0).
static Surface *loadPng(const std::string &path, int cellW, int cellH)
{
	std::ifstream in(path, std::ios::binary);
	std::vector<unsigned char> png((std::istreambuf_iterator<char>(in)), std::istreambuf_iterator<char>());
	std::vector<unsigned char> image;
	unsigned w, h;
	lodepng::State state;
	state.decoder.color_convert = 0;
	if (lodepng::decode(image, w, h, state, png) || lodepng_get_bpp(&state.info_png.color) != 8)
	{
		fprintf(stderr, "cannot load %s\n", path.c_str());
		exit(1);
	}
	int transparent = 0;
	for (size_t c = 0; c < state.info_png.color.palettesize; ++c)
	{
		if (state.info_png.color.palette[c * 4 + 3] == 0) { transparent = (int)c; break; }
	}
	Surface *s = new Surface(w, h);
	for (unsigned y = 0; y < h; ++y)
		for (unsigned x = 0; x < w; ++x)
		{
			int p = image[y * w + x];
			s->setPixel(x, y, p == transparent ? 0 : p);
		}
	return s;
}

/// Font::load for one entry of Font.dat (the file is parsed by hand: indentation 2 = font, 4 = font key, 6 = image, 8 = image key, 10 = chars).
static Font *loadFont(const std::string &dir, const std::string &id)
{
	std::ifstream in(dir + "/Language/Font.dat");
	std::string line;
	bool inFont = false;
	int width = 0, height = 0, spacing = 0;
	struct Img { std::string file, chars; int w = -1, h = -1, sp = -9999; };
	std::vector<Img> images;
	while (std::getline(in, line))
	{
		size_t ind = line.find_first_not_of(' ');
		if (ind == std::string::npos) continue;
		std::string t = line.substr(ind);
		if (ind == 2) { inFont = t == "- id: " + id; continue; }
		if (!inFont) continue;
		auto val = [&t]() { return t.substr(t.find(':') + 2); };
		if (ind == 4 && t.rfind("width:", 0) == 0) width = std::stoi(val());
		else if (ind == 4 && t.rfind("height:", 0) == 0) height = std::stoi(val());
		else if (ind == 4 && t.rfind("spacing:", 0) == 0) spacing = std::stoi(val());
		else if (ind == 6 && t.rfind("- file:", 0) == 0) { images.push_back(Img()); images.back().file = val(); }
		else if (ind == 8 && t.rfind("width:", 0) == 0) images.back().w = std::stoi(val());
		else if (ind == 8 && t.rfind("height:", 0) == 0) images.back().h = std::stoi(val());
		else if (ind == 8 && t.rfind("spacing:", 0) == 0) images.back().sp = std::stoi(val());
		else if (ind == 10) images.back().chars = t + "\n"; // folded block scalar keeps the final line break
	}
	Font *font = new Font();
	for (const Img &i : images)
	{
		FontImage image;
		image.width = i.w != -1 ? i.w : width;
		image.height = i.h != -1 ? i.h : height;
		image.spacing = i.sp != -9999 ? i.sp : spacing;
		image.surface = loadPng(dir + "/Language/" + i.file, image.width, image.height);
		font->_images.push_back(image);
		font->init(font->_images.size() - 1, Unicode::convUtf8ToUtf32(i.chars));
	}
	return font;
}

static uint32_t seed = 2024;
static int rnd(int min, int max)
{
	seed = seed * 1103515245u + 12345u;
	return min + (int)((seed >> 8) % (uint32_t)(max - min + 1));
}

static Font *big, *small;

struct TextCase
{
	bool bigFont; int w, h; bool wrap, indent, ignoreSep; TextWrapping mode; TextDirection dir;
	int align, valign, color, color2; bool invert, contrast, scroll; int wheel; bool debug; std::string text;
};

static void text(const TextCase &c)
{
	g_direction = c.dir;
	g_wrap = c.mode;
	Options::debugUi = c.debug;
	Language lang;
	Text t(c.w, c.h, 0, 0);
	t.initText(big, small, &lang);
	if (c.bigFont) t.setBig();
	t.setWordWrap(c.wrap, c.indent, c.ignoreSep);
	t.setAlign((TextHAlign)c.align);
	t.setVerticalAlign((TextVAlign)c.valign);
	t.setColor(c.color);
	t.setSecondaryColor(c.color2);
	t.setInvert(c.invert);
	t.setHighContrast(c.contrast);
	t.setScrollable(c.scroll);
	t.setText(c.text);
	for (int i = 0; i < std::abs(c.wheel); ++i)
	{
		SDL_Event ev;
		memset(&ev, 0, sizeof(ev));
		ev.type = SDL_MOUSEBUTTONDOWN;
		ev.button.button = c.wheel > 0 ? SDL_BUTTON_WHEELDOWN : SDL_BUTTON_WHEELUP;
		Action action(&ev, 1.0, 1.0, 0, 0);
		t.mousePress(&action, 0);
	}
	t.draw();
	printf("text %c %d %d %d %d %d %c %c %d %d %d %d %d %d %d %d %d %s", c.bigFont ? 'B' : 'S', c.w, c.h, c.wrap, c.indent, c.ignoreSep,
		c.mode == WRAP_LETTERS ? 'L' : 'W', c.dir == DIRECTION_RTL ? 'R' : 'L', c.align, c.valign, c.color, c.color2,
		c.invert, c.contrast, c.scroll, c.wheel, c.debug, hex(c.text).c_str());
	printf(" %c %d", t.getFont() == big ? 'B' : 'S', t.getNumLines());
	std::string widths, heights;
	for (size_t i = 0; i < t._lineWidth.size(); ++i)
	{
		widths += (i ? "," : "") + std::to_string(t._lineWidth[i]);
		heights += (i ? "," : "") + std::to_string(t._lineHeight[i]);
	}
	printf(" %s %s %d %d", widths.empty() ? "-" : widths.c_str(), heights.empty() ? "-" : heights.c_str(), t.getTextWidth(), t.getTextHeight());
	std::string processed;
	for (UCode u : t._processedText)
	{
		char b[16];
		snprintf(b, sizeof(b), "%s%x", processed.empty() ? "" : ".", (unsigned)u);
		processed += b;
	}
	printf(" %s %016llx\n", processed.empty() ? "-" : processed.c_str(), (unsigned long long)fnv(t));
	Options::debugUi = false;
}

static void textButton(int w, int h, int color, bool contrast, bool geo, bool pressed, bool toggle, int invertColor, const std::string &label)
{
	Language lang;
	if (toggle)
	{
		ToggleTextButton b(w, h, 0, 0);
		b.initText(big, small, &lang);
		b.setColor(color);
		b.setHighContrast(contrast);
		b.setGeoscapeButton(geo);
		if (invertColor >= 0) b.setInvertColor(invertColor);
		b.setPressed(pressed);
		b.setText(label);
		b.draw();
		printf("toggle %d %d %d %d %d %d %d %s %016llx\n", w, h, color, contrast, geo, pressed, invertColor, hex(label).c_str(), (unsigned long long)fnv(b));
	}
	else
	{
		TextButton b(w, h, 0, 0);
		TextButton *group = 0;
		b.initText(big, small, &lang);
		b.setColor(color);
		b.setHighContrast(contrast);
		b.setGeoscapeButton(geo);
		if (pressed) { group = &b; b.setGroup(&group); }
		b.setText(label);
		b.draw();
		printf("button %d %d %d %d %d %d %s %016llx\n", w, h, color, contrast, geo, pressed, hex(label).c_str(), (unsigned long long)fnv(b));
	}
}

static Surface *background;

static void window(int w, int h, int x, int y, int popup, int steps, int color, bool contrast, bool thin, int inner, bool bg, int dx, int dy)
{
	Window win(0, w, h, x, y, (WindowPopup)popup);
	for (int i = 0; i < steps; ++i) win.popup();
	win.setColor(color);
	win.setHighContrast(contrast);
	if (thin) win.setThinBorder();
	win.setInnerColor(inner);
	if (bg) win.setBackground(background);
	if (dx != -9999) win.setDX(dx);
	if (dy != -9999) win.setDY(dy);
	win.draw();
	printf("window %d %d %d %d %d %d %d %d %d %d %d %d %d %016llx\n", w, h, x, y, popup, steps, color, contrast, thin, inner, bg, dx, dy, (unsigned long long)fnv(win));
}

static void frame(int w, int h, int color, int bg, bool contrast, int thickness)
{
	Frame f(w, h, 0, 0);
	f.setColor(color);
	f.setSecondaryColor(bg);
	f.setHighContrast(contrast);
	f.setThickness(thickness);
	f.draw();
	printf("frame %d %d %d %d %d %d %016llx\n", w, h, color, bg, contrast, thickness, (unsigned long long)fnv(f));
}

static void bar(int w, int h, int color, int color2, int border, double scale, double max, double value, double value2, bool top)
{
	Bar b(w, h, 0, 0);
	b.setColor(color);
	b.setSecondaryColor(color2);
	if (border) b.setBorderColor(border);
	b.setScale(scale);
	b.setMax(max);
	b.setValue(value);
	b.setValue2(value2);
	b.setSecondValueOnTop(top);
	b.draw();
	printf("bar %d %d %d %d %d %.17g %.17g %.17g %.17g %d %016llx\n", w, h, color, color2, border, scale, max, value, value2, top, (unsigned long long)fnv(b));
}

static void progress(int w, int h, int color, int border, int value)
{
	ProgressBar p(w, h, 0, 0);
	p.setColor(color);
	p.setBorderColor(border);
	p.setValue(value);
	p.draw();
	printf("progress %d %d %d %d %d %016llx\n", w, h, color, border, value, (unsigned long long)fnv(p));
}

static void arrow(int shape, int w, int h, int color)
{
	ArrowButton a((ArrowShape)shape, w, h, 0, 0);
	a.setColor(color);
	a.draw();
	printf("arrow %d %d %d %d %016llx\n", shape, w, h, color, (unsigned long long)fnv(a));
}

static void number(int w, int h, unsigned value, int color, bool bordered)
{
	NumberText n(w, h, 0, 0);
	n.setValue(value);
	n.setColor(color);
	n.setBordered(bordered);
	n.draw();
	printf("number %d %d %u %d %d %016llx\n", w, h, value, color, bordered, (unsigned long long)fnv(n));
}

static void cursor(int w, int h, int color)
{
	// SDL_gfx draws the lines: with the 3-3-2 colour cube palette every index maps to itself (see gfx.cpp)
	SDL_Color palette[256];
	for (int i = 0; i < 256; ++i)
	{
		int r = i & 0xe0; r |= r >> 3 | r >> 6;
		int g = (i << 3) & 0xe0; g |= g >> 3 | g >> 6;
		int b = i & 0x3; b |= b << 2; b |= b << 4;
		palette[i].r = r; palette[i].g = g; palette[i].b = b; palette[i].unused = 0;
	}
	Cursor c(w, h, 0, 0);
	c.setPalette(palette);
	c.setColor(color);
	c.draw();
	printf("cursor %d %d %d %016llx\n", w, h, color, (unsigned long long)fnv(c));
}

int main(int argc, char **argv)
{
	if (argc != 2) { fprintf(stderr, "usage: widgets <bin/common>\n"); return 1; }
	big = loadFont(argv[1], "FONT_BIG");
	small = loadFont(argv[1], "FONT_SMALL");

	const std::vector<std::string> texts = {
		"Hello world",
		"The quick brown fox jumps over the lazy dog",
		"  indented text that wraps around several lines of the box",
		"multi\nline\ntext",
		"small\x02line follows here and wraps maybe",
		"colour \x01" "flip\x01 back \x01open",
		"custom \x1b" "C\x45" "colour \x1b" "cSsecond \x1b" "cPprimary \x1b" "xyignored",
		"trailing format \x1b" "C",
		"trailing \x1b",
		"separators-are/handled-like-spaces-sometimes",
		"nbsp\xc2\xa0kept\xc2\xa0together\xc2\xa0in\xc2\xa0one\xc2\xa0word and more",
		"tab\tstop\tand more words after the tab",
		"Ünïcödé Ελληνικά Русский текст",
		"あいうえおかきくけこ、さしすせそ。",
		"emoji 😀 outside the BMP",
		"Ends with a dot.",
		"Supercalifragilisticexpialidocious",
		"",
		"a b c d e f g h i j k l m n o p q r s t u v w x y z",
		"word-\nbreak-after separator / slash",
		"   ",
		"x",
	};

	// Text: fixed configurations, then random ones (letter wrapping needs a width above the widest glyph).
	for (const std::string &s : texts)
	{
		for (int b = 0; b < 2; ++b)
			for (int wrap = 0; wrap < 2; ++wrap)
				text({ b == 1, 100, 40, wrap == 1, false, false, WRAP_WORDS, DIRECTION_LTR, 0, 0, 16, 32, false, false, false, 0, false, s });
	}
	for (int i = 0; i < 1500; ++i)
	{
		TextCase c;
		c.bigFont = rnd(0, 2) == 0;
		c.mode = rnd(0, 3) == 0 ? WRAP_LETTERS : WRAP_WORDS;
		c.w = rnd(c.mode == WRAP_LETTERS ? 24 : 4, 160);
		c.h = rnd(4, 80);
		c.wrap = rnd(0, 3) != 0;
		c.indent = rnd(0, 2) == 0;
		c.ignoreSep = rnd(0, 2) == 0;
		c.dir = rnd(0, 4) == 0 ? DIRECTION_RTL : DIRECTION_LTR;
		c.align = rnd(0, 2);
		c.valign = rnd(0, 2);
		c.color = rnd(0, 255);
		c.color2 = rnd(0, 255);
		c.invert = rnd(0, 3) == 0;
		c.contrast = rnd(0, 3) == 0;
		c.scroll = rnd(0, 2) == 0;
		c.wheel = c.scroll ? rnd(-3, 6) : 0;
		c.debug = rnd(0, 9) == 0;
		c.text = texts[rnd(0, (int)texts.size() - 1)];
		text(c);
	}

	// Buttons
	const std::vector<std::string> labels = { "OK", "CANCEL", "A much longer label that wraps", "" };
	for (int i = 0; i < 80; ++i)
	{
		bool toggle = rnd(0, 1) == 1;
		textButton(rnd(4, 120), rnd(4, 30), rnd(0, 250), rnd(0, 3) == 0, rnd(0, 3) == 0, rnd(0, 1) == 1, toggle, toggle && rnd(0, 1) ? rnd(0, 250) : -1, labels[rnd(0, (int)labels.size() - 1)]);
	}

	// Windows: background pattern, popup animation steps (the last step would need a state)
	background = new Surface(320, 200);
	for (int y = 0; y < 200; ++y)
		for (int x = 0; x < 320; ++x)
			background->setPixel(x, y, (x * 3 + y * 5) % 200 + (x % 7 == 0 ? 0 : 1));
	for (int i = 0; i < 120; ++i)
	{
		int popup = rnd(0, 3);
		window(rnd(1, 120), rnd(1, 90), rnd(-10, 200), rnd(-10, 110), popup, popup ? rnd(0, 15) : 0, rnd(0, 250), rnd(0, 3) == 0, rnd(0, 3) == 0,
			rnd(0, 2) ? 0 : rnd(1, 255), rnd(0, 2) != 0, rnd(0, 2) ? -9999 : rnd(-50, 50), rnd(0, 2) ? -9999 : rnd(-50, 50));
	}

	for (int i = 0; i < 80; ++i)
		frame(rnd(1, 60), rnd(1, 40), rnd(0, 255), rnd(0, 255), rnd(0, 3) == 0, rnd(0, 12));

	for (int i = 0; i < 80; ++i)
		bar(rnd(1, 120), rnd(1, 10), rnd(0, 255), rnd(0, 255), rnd(0, 2) ? 0 : rnd(1, 255), rnd(0, 400) / 100.0, rnd(0, 150), rnd(-20, 160) * 0.7, rnd(-20, 160) * 0.3, rnd(0, 1) == 1);

	for (int i = 0; i < 50; ++i)
		progress(rnd(1, 120), rnd(1, 12), rnd(0, 255), rnd(0, 2) ? 0 : rnd(1, 255), rnd(-10, 130));

	for (int shape = 0; shape <= 6; ++shape)
		for (int i = 0; i < 6; ++i)
			arrow(shape, rnd(1, 20), rnd(1, 20), rnd(0, 250));

	for (int i = 0; i < 40; ++i)
		number(rnd(1, 50), rnd(1, 12), rnd(0, 3) == 0 ? 4294967295u : (unsigned)rnd(0, 99999), rnd(0, 250), rnd(0, 1) == 1);

	for (int i = 0; i < 20; ++i)
		cursor(rnd(1, 16), rnd(1, 16), rnd(0, 250));
	return 0;
}
