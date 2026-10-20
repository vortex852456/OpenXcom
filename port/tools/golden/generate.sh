#!/bin/sh
# Regenerates golden reference files from the original C++ code (needs g++, SDL 1.2, SDL_image 1.2 and SDL_gfx 1.2 dev packages).
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
# Surface::loadImage (lodepng + SDL_image) on the repo images and the synthetic test images
g++ -std=c++17 -O1 -I"$root/src" "$here/images.cpp" "$root/src/lodepng.cpp" \
	$(pkg-config --cflags --libs SDL_image sdl) -o "$tmp/images"
images="port/tests/Acom.Core.Tests/Images"
python3 "$here/make_test_images.py" "$root/$images"
(cd "$root" && find bin "$images" -type f \( -iname '*.png' -o -iname '*.gif' \) | LC_ALL=C sort | xargs "$tmp/images") > "$out/images.txt"
# SDL_gfx primitives behind Surface::drawLine/drawCircle/drawPolygon/drawTexturedPolygon/drawString (needs SDL_gfx 1.2 dev package)
g++ -std=c++17 -O1 "$here/gfx.cpp" $(pkg-config --cflags --libs SDL_gfx sdl) -o "$tmp/gfx"
"$tmp/gfx" > "$out/gfx.txt"
rm -rf "$tmp"
echo "Golden files written to $out"
echo "options-defaults.cfg: build the C++ game (see docs/architecture/build.md), then run"
echo "  xvfb-run -a build/bin/openxcom -user <tmp>/user -cfg <tmp>/cfg   (stop it after a few seconds)"
echo "and copy <tmp>/cfg/options.cfg to $out/options-defaults.cfg"
