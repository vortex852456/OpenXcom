#!/bin/sh
# Regenerates golden reference files from the original C++ code (needs g++, SDL 1.2, SDL_image 1.2, SDL_gfx 1.2 and SDL_mixer 1.2 dev packages).
set -e
here=$(cd "$(dirname "$0")" && pwd)
root=$(cd "$here/../../.." && pwd)
out="$root/port/tests/Acom.Core.Tests/Golden"
tmp=$(mktemp -d)
g++ -std=c++17 -O1 -I"$root/src" $(pkg-config --cflags sdl) \
	"$here/golden.cpp" "$root/src/Engine/RNG.cpp" "$root/src/Engine/LanguagePlurality.cpp" "$root/src/Engine/FastLineClip.cpp" \
	-o "$tmp/golden"
mkdir -p "$out"
"$tmp/golden" "$out"
# Adlib emulation (fmopl/adlplayer) driven by register writes and by the AdlibMusic::player loop
g++ -std=c++17 -O1 -I"$root/src" "$here/adlib.cpp" "$root/src/Engine/Adlib/fmopl.cpp" "$root/src/Engine/Adlib/adlplayer.cpp" \
	"$root/src/Engine/RNG.cpp" -o "$tmp/adlib"
"$tmp/adlib" "$out/adlib.txt"
# SDL_mixer 1.2 channel/music mixing behind Engine/Mixer.cs (needs the SDL_mixer 1.2 dev package; runs with the dummy audio driver)
g++ -std=c++17 -O1 "$here/mixer.cpp" $(pkg-config --cflags --libs SDL_mixer sdl) -o "$tmp/mixer"
"$tmp/mixer" "$out/mixer.txt"
# Surface::loadImage (lodepng + SDL_image) on the repo images and the synthetic test images
g++ -std=c++17 -O1 -I"$root/src" "$here/images.cpp" "$root/src/lodepng.cpp" \
	$(pkg-config --cflags --libs SDL_image sdl) -o "$tmp/images"
images="port/tests/Acom.Core.Tests/Images"
python3 "$here/make_test_images.py" "$root/$images"
(cd "$root" && find bin "$images" -type f \( -iname '*.png' -o -iname '*.gif' \) | LC_ALL=C sort | xargs "$tmp/images") > "$out/images.txt"
# SDL_gfx primitives behind Surface::drawLine/drawCircle/drawPolygon/drawTexturedPolygon/drawString (needs SDL_gfx 1.2 dev package)
g++ -std=c++17 -O1 "$here/gfx.cpp" $(pkg-config --cflags --libs SDL_gfx sdl) -o "$tmp/gfx"
"$tmp/gfx" > "$out/gfx.txt"
# Interface widgets (Text layout and drawing with the bin/common fonts, buttons, windows, bars, ...):
# compiled from the original sources; game parts the cases never reach stay unresolved (calling one would crash the run)
widgets=""
for f in Interface/Text Interface/TextButton Interface/ToggleTextButton Interface/Window Interface/Frame Interface/Bar \
	Interface/ArrowButton Interface/ImageButton Interface/NumberText Interface/ProgressBar Interface/Cursor \
	Engine/Surface Engine/InteractiveSurface Engine/Font Engine/Unicode Engine/Timer Engine/Action Engine/Palette Engine/RNG; do
	widgets="$widgets $root/src/$f.cpp"
done
g++ -std=c++17 -O1 -DNDEBUG -I"$root/src" -I"$root/libs/rapidyaml" $(pkg-config --cflags sdl) "$here/widgets.cpp" $widgets "$root/src/lodepng.cpp" \
	$(pkg-config --libs SDL_gfx SDL_image SDL_mixer sdl) -Wl,--unresolved-symbols=ignore-all -o "$tmp/widgets"
"$tmp/widgets" "$root/bin/common" > "$out/widgets.txt"
rm -rf "$tmp"
echo "Golden files written to $out"
echo "options-defaults.cfg: build the C++ game (see docs/architecture/build.md), then run"
echo "  xvfb-run -a build/bin/openxcom -user <tmp>/user -cfg <tmp>/cfg   (stop it after a few seconds)"
echo "and copy <tmp>/cfg/options.cfg to $out/options-defaults.cfg"
