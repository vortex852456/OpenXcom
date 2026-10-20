#!/usr/bin/env python3
"""Shows how much of the C++ code base (src/) has been ported to C# (port/src/).

A C# file declares what it ports with a header line
    // Ported from: src/Engine/RNG.h, src/Engine/RNG.cpp
and, while incomplete, a line
    // Port status: partial (missing: ...)

Units listed in port/tools/port-skip.txt are intentionally not ported.

Usage: port-status.py            summary per area
       port-status.py Engine     open/partial units of one area
"""
import fnmatch
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CPP = ROOT / "src"
CS = ROOT / "port" / "src"


def cpp_units():
    """C++ translation units: X.h + X.cpp of one directory count as one unit."""
    units = {}
    for f in sorted(CPP.rglob("*")):
        if f.suffix not in (".cpp", ".h") or "apple" in f.parts:
            continue
        rel = f.relative_to(ROOT).as_posix()
        key = rel.rsplit(".", 1)[0]
        lines = sum(1 for _ in f.open(encoding="utf-8", errors="replace"))
        u = units.setdefault(key, {"files": [], "loc": 0})
        u["files"].append(rel)
        u["loc"] += lines
    return units


def skipped():
    """Units intentionally not ported (port/tools/port-skip.txt): glob -> reason."""
    rules = []
    for line in (ROOT / "port" / "tools" / "port-skip.txt").read_text(encoding="utf-8").splitlines():
        if line.strip() and not line.startswith("#"):
            pattern, _, reason = line.partition("|")
            rules.append((pattern.strip(), reason.strip()))
    return rules


def ported():
    """Maps C++ file path -> 'partial' or 'done'."""
    status = {}
    for f in CS.rglob("*.cs"):
        text = f.read_text(encoding="utf-8", errors="replace")
        m = re.search(r"^// Ported from: (.+)$", text, re.M)
        if not m:
            continue
        state = "partial" if re.search(r"^// Port status: partial", text, re.M) else "done"
        for path in m.group(1).split(","):
            path = path.strip().split(" ")[0]
            if path.startswith("src/"):
                status[path] = state
    return status


def main():
    units = cpp_units()
    status = ported()
    skip = skipped()
    areas = {}
    for key, u in units.items():
        parts = key.split("/")
        area = parts[1] if len(parts) > 2 else "(root)"
        states = {status.get(f, "open") for f in u["files"]}
        state = "done" if states == {"done"} else ("open" if states == {"open"} else "partial")
        if state == "open" and any(fnmatch.fnmatch(key, p) for p, _ in skip):
            state = "skip"
        u["state"] = state
        a = areas.setdefault(area, {"done": 0, "partial": 0, "open": 0, "skip": 0, "loc": 0, "loc_touched": 0, "units": []})
        a[state] += 1
        if state != "skip":
            a["loc"] += u["loc"]
        if state in ("done", "partial"):
            a["loc_touched"] += u["loc"]
        a["units"].append((key, state, u["loc"]))

    if len(sys.argv) > 1:
        area = areas.get(sys.argv[1])
        if not area:
            sys.exit(f"unknown area {sys.argv[1]}; known: {', '.join(sorted(areas))}")
        for key, state, loc in sorted(area["units"], key=lambda x: (x[1] != "partial", x[1] == "done", -x[2])):
            if state not in ("done", "skip"):
                print(f"{state:8} {loc:6}  {key}")
        return

    print(f"{'Bereich':14}{'Units':>6}{'fertig':>8}{'teilw.':>8}{'offen':>7}{'entf.':>7}{'LOC':>9}{'LOC begonnen':>14}")
    total = {"done": 0, "partial": 0, "open": 0, "skip": 0, "loc": 0, "loc_touched": 0}
    for name in sorted(areas):
        a = areas[name]
        n = a["done"] + a["partial"] + a["open"] + a["skip"]
        print(f"{name:14}{n:6}{a['done']:8}{a['partial']:8}{a['open']:7}{a['skip']:7}{a['loc']:9}{a['loc_touched']:14}")
        for k in total:
            total[k] += a[k]
    n = total["done"] + total["partial"] + total["open"] + total["skip"]
    pct = 100.0 * total["loc_touched"] / total["loc"] if total["loc"] else 0
    print(f"{'Gesamt':14}{n:6}{total['done']:8}{total['partial']:8}{total['open']:7}{total['skip']:7}{total['loc']:9}{total['loc_touched']:14}  ({pct:.1f} % begonnen)")
    print("LOC = zu portierende Zeilen (ohne 'entfaellt', siehe port/tools/port-skip.txt)")


if __name__ == "__main__":
    main()
