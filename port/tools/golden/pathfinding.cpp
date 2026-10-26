// Reference values for the Battlescape pathfinding helpers (PathfindingOpenSet tie-breaking, std::sort with
// MinNodeCosts, direction vectors, Particle screen positions, Explosion frames), computed with the original C++ code.
// The output is embedded in port/tests/Acom.Core.Tests/Battlescape/*Tests.cs. Build and run from the repo root:
//   g++ -std=c++17 -O1 -Isrc -Ilibs/rapidyaml $(pkg-config --cflags sdl) port/tools/golden/pathfinding.cpp \
//     src/Battlescape/PathfindingNode.cpp src/Battlescape/PathfindingOpenSet.cpp src/Battlescape/Particle.cpp \
//     src/Battlescape/Explosion.cpp src/Engine/RNG.cpp -o /tmp/pathfinding && /tmp/pathfinding
// The order of equal elements depends on the standard library (values here: GCC 13 libstdc++).
#include <cstdio>
#include <cstdint>
#include <vector>
#include <algorithm>
#include "Battlescape/PathfindingNode.h"
#include "Battlescape/PathfindingOpenSet.h"
#include "Battlescape/Pathfinding.h"
#include "Battlescape/Particle.h"
#include "Battlescape/Explosion.h"
using namespace OpenXcom;

static uint32_t s = 12345;
static int rnd() { s = s * 1103515245u + 12345u; return (s >> 16) & 0x7fff; }

int main()
{
	// A: open set with ties and re-pushed (discarded) entries
	{
		s = 12345;
		const int N = 60;
		std::vector<PathfindingNode> nodes;
		for (int i = 0; i < N; ++i) nodes.push_back(PathfindingNode(Position(i % 10, i / 10, 0)));
		PathfindingOpenSet set;
		printf("A:");
		for (int step = 0; step < 300; ++step)
		{
			int r = rnd() % 3;
			if (r < 2 || set.empty())
			{
				int k = rnd() % N;
				int t = rnd() % 6;
				int tx = rnd() % 10; // separate statements: argument evaluation order is unspecified
				int ty = rnd() % 6;
				Position target(tx, ty, 0);
				nodes[k].connect({t, 0}, nullptr, 0, target);
				set.push(&nodes[k]);
			}
			else
			{
				PathfindingNode* n = set.pop();
				printf(" %d", (int)(n - &nodes[0]));
			}
		}
		printf(" |");
		while (!set.empty())
		{
			PathfindingNode* n = set.pop();
			printf(" %d", (int)(n - &nodes[0]));
		}
		printf("\n");
	}
	// B: std::sort with MinNodeCosts (partial order on time/energy)
	for (int variant = 0; variant < 3; ++variant)
	{
		s = 777 + variant;
		int N = variant == 0 ? 200 : variant == 1 ? 50 : 12;
		std::vector<PathfindingNode> nodes;
		for (int i = 0; i < N; ++i) nodes.push_back(PathfindingNode(Position(i % 100, i / 100, 0)));
		for (int i = 0; i < N; ++i)
		{
			int t = variant == 1 ? 3 : rnd() % 8;
			int e = variant == 1 ? 3 : rnd() % 8;
			nodes[i].connect({t, e}, nullptr, 0);
		}
		std::vector<PathfindingNode*> v;
		for (auto& n : nodes) v.push_back(&n);
		std::sort(v.begin(), v.end(), MinNodeCosts());
		printf("B%d:", variant);
		for (auto* n : v) printf(" %d", (int)(n - &nodes[0]));
		printf("\n");
	}
	// C: direction vectors
	printf("C:");
	for (int d = 0; d < 10; ++d)
	{
		Position p;
		Pathfinding::directionToVector(d, &p);
		printf(" (%d,%d,%d)->%d", p.x, p.y, p.z, Pathfinding::vectorToDirection(p));
	}
	printf("\n");
	// D: particle screen position
	{
		Position voxels[] = { Position(37, 21, 50), Position(15, 15, 23), Position(0, 0, 0), Position(100, 3, 71) };
		Position offsets[] = { Position(0, 0, 0), Position(300, -200, 100), Position(-384, 384, -384), Position(4000, 100, 6000) };
		printf("D:");
		for (int i = 0; i < 4; ++i)
		{
			Particle p(voxels[i], offsets[i], Position(), Position(), 128, 5, (Uint8)(32 + i * 4), 2);
			Position t = p.updateScreenPosition();
			printf(" [%d %d %d|%d %d %d %d %d]", t.x, t.y, t.z, p.getOffsetX(), p.getOffsetY(), p.getLayerZ(), p.getTileZ(), (int)p.getOpacity());
		}
		printf("\n");
	}
	// E: explosion frames
	{
		printf("E:");
		Explosion ex[] = { Explosion(Position(1,2,3), 0, 2), Explosion(Position(), 5, 0, true), Explosion(Position(), 3, 1, false, true), Explosion(Position(), 0, 0, false, false, 3) };
		for (auto& e : ex)
		{
			printf(" [");
			do { printf("%d,", e.getCurrentFrame()); } while (e.animate());
			printf("%d]", e.getCurrentFrame());
		}
		printf("\n");
	}
	return 0;
}
