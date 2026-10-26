// Golden reference for the TileEngine trajectory helpers (calculateLineHelper / calculateParabolaHelper).
// The helpers live in an anonymous namespace of src/Battlescape/TileEngine.cpp; generate.sh extracts them
// verbatim into TRAJECTORY_HELPERS (a temporary file) so they compile without the rest of the battlescape.
// Output lines:
//   line ox oy oz tx ty tz : P x y z | D x y z ...   (P = posFunc step, D = driftFunc side step)
//   para ox oy oz tx ty tz curvature dx dy dz : x y z ... (posFunc points, capped at 400)
#include <cstdio>
#include <cstdlib>
#include <cmath>
#include <utility>
#include "Battlescape/Position.h"
#include "fmath.h"

namespace OpenXcom
{
namespace
{
#include TRAJECTORY_HELPERS
}
}

using namespace OpenXcom;

static void line(Position o, Position t)
{
	printf("line %d %d %d %d %d %d :", o.x, o.y, o.z, t.x, t.y, t.z);
	int n = 0;
	calculateLineHelper(o, t,
		[&](Position p) { printf(" P %d %d %d", p.x, p.y, p.z); return ++n >= 2000; },
		[&](Position p) { printf(" D %d %d %d", p.x, p.y, p.z); return false; });
	printf("\n");
}

static void para(Position o, Position t, double curvature, Position d)
{
	printf("para %d %d %d %d %d %d %.17g %d %d %d :", o.x, o.y, o.z, t.x, t.y, t.z, curvature, d.x, d.y, d.z);
	int n = 0;
	calculateParabolaHelper(o, t, curvature, d,
		[&](Position p) { printf(" %d %d %d", p.x, p.y, p.z); return ++n >= 400; });
	printf("\n");
}

int main()
{
	const Position origins[] = { {8, 8, 12}, {40, 24, 36}, {100, 3, 70}, {0, 0, 0} };
	const Position targets[] = {
		{8, 8, 12}, {120, 8, 12}, {8, 120, 12}, {8, 8, 80}, {72, 40, 12}, {9, 200, 30}, {200, 199, 1},
		{0, 0, 0}, {255, 13, 47}, {33, 70, 2}, {-20, 15, 12}, {5, -40, 90}, {150, 150, 150}, {41, 23, 35},
	};
	for (const Position& o : origins)
		for (const Position& t : targets)
			line(o, t);

	const double curvatures[] = { 1.0, 1.73, 2.5, 3.0 };
	const Position deltas[] = { {0, 0, 0}, {3, -2, 1}, {-7, 4, -5} };
	const Position pOrigins[] = { {8, 8, 36}, {40, 24, 60}, {120, 70, 13} };
	const Position pTargets[] = { {8, 8, 36}, {120, 8, 12}, {8, 120, 2}, {72, 40, 30}, {200, 199, 1}, {41, 25, 12}, {130, 75, 80} };
	for (const Position& o : pOrigins)
		for (const Position& t : pTargets)
			for (double c : curvatures)
				for (const Position& d : deltas)
					para(o, t, c, d);
	return 0;
}
