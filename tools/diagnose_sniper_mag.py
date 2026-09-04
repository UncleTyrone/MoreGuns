"""Re-split Quaternius SniperRifle so mag faces leave the body mesh."""
from __future__ import annotations

import os
import shutil
from collections import defaultdict

# Reuse parsers from resplit_with_materials
import importlib.util

spec = importlib.util.spec_from_file_location(
    "resplit",
    r"D:\Schedule I\Schedule I\MoreGuns\tools\resplit_with_materials.py",
)
resplit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(resplit)


def face_centroid(verts, corners):
    xs = ys = zs = 0.0
    for v, _, _ in corners:
        x, y, z = verts[v - 1]
        xs += x
        ys += y
        zs += z
    n = float(len(corners))
    return xs / n, ys / n, zs / n


def dump_islands(verts, faces):
    # Build connectivity on vertex indices only
    vert_to = defaultdict(list)
    for i, (_, corners) in enumerate(faces):
        for v, _, _ in corners:
            vert_to[v].append(i)
    adj = defaultdict(set)
    for i, (_, corners) in enumerate(faces):
        for v, _, _ in corners:
            for j in vert_to[v]:
                if j != i:
                    adj[i].add(j)
    seen = set()
    groups = []
    for i in range(len(faces)):
        if i in seen:
            continue
        stack = [i]
        seen.add(i)
        g = []
        while stack:
            cur = stack.pop()
            g.append(cur)
            for n in adj[cur]:
                if n not in seen:
                    seen.add(n)
                    stack.append(n)
        groups.append(g)

    print(f"islands={len(groups)} faces={len(faces)}")
    rows = []
    for gi, g in enumerate(groups):
        cs = [face_centroid(verts, faces[i][1]) for i in g]
        ys = [c[1] for c in cs]
        xs = [c[0] for c in cs]
        rows.append((len(g), sum(ys) / len(ys), min(ys), max(ys), sum(xs) / len(xs), min(xs), max(xs), gi))
    rows.sort()
    for r in rows:
        print(
            f"  faces={r[0]:4d} meanY={r[1]:7.3f} y=[{r[2]:7.3f},{r[3]:7.3f}] "
            f"meanX={r[4]:7.3f} x=[{r[5]:7.3f},{r[6]:7.3f}] id={r[7]}"
        )
    return groups


def cut_mag_by_hang(verts, faces):
    """Magazine hangs below receiver, mid-forward of stock, not barrel tip."""
    xs = [v[0] for v in verts]
    ys = [v[1] for v in verts]
    minx, maxx = min(xs), max(xs)
    miny, maxy = min(ys), max(ys)
    # Quaternius sniper: barrel +X. Mag under receiver ~20-45% along length, bottom third.
    x0 = minx + (maxx - minx) * 0.22
    x1 = minx + (maxx - minx) * 0.48
    y_cut = miny + (maxy - miny) * 0.38

    mag, body = [], []
    for i, (mtl, corners) in enumerate(faces):
        cx, cy, _ = face_centroid(verts, corners)
        if x0 <= cx <= x1 and cy <= y_cut:
            mag.append(i)
        else:
            body.append(i)
    return body, mag, (x0, x1, y_cut)


def cut_mag_by_island_plus_hang(verts, faces, groups):
    """Prefer low hanging island; if mag is welded into a big island, fall back to hang-cut."""
    stats = []
    for g in groups:
        cs = [face_centroid(verts, faces[i][1]) for i in g]
        ys = [c[1] for c in cs]
        xs = [c[0] for c in cs]
        stats.append(
            {
                "idxs": g,
                "count": len(g),
                "mean_y": sum(ys) / len(ys),
                "min_y": min(ys),
                "mean_x": sum(xs) / len(xs),
            }
        )
    stats.sort(key=lambda s: s["count"], reverse=True)
    body_island = stats[0]

    # Candidate detached mags: small-ish, clearly below body mean Y
    cands = [
        s
        for s in stats[1:]
        if 20 <= s["count"] <= 120 and s["mean_y"] < body_island["mean_y"] - 0.15
    ]
    cands.sort(key=lambda s: (s["mean_y"], -s["count"]))
    if cands:
        mag = cands[0]
        body = []
        for s in stats:
            if s is mag:
                continue
            body.extend(s["idxs"])
        return body, mag["idxs"], f"island id count={mag['count']} meanY={mag['mean_y']:.3f}"

    # Welded: geometric hang cut
    body, mag, box = cut_mag_by_hang(verts, faces)
    return body, mag, f"hang-cut box={box}"


def main():
    q = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\_downloads\extracted\quaternius\Ultimate Gun Pack - July 2019\OBJ"
    # Try a few snipers and pick the best mag cut
    for name in ["SniperRifle_2.obj", "SniperRifle_1.obj", "SniperRifle_3.obj", "SniperRifle_4.obj"]:
        src = os.path.join(q, name)
        if not os.path.isfile(src):
            continue
        print("\n====", name, "====")
        verts, uvs, norms, faces, mtllib = resplit.parse_obj(src)
        groups = dump_islands(verts, faces)
        body_idxs, mag_idxs, method = cut_mag_by_island_plus_hang(verts, faces, groups)
        print(f"method={method} body_faces={len(body_idxs)} mag_faces={len(mag_idxs)}")
        if len(mag_idxs) < 12:
            print("  skip: mag too small")
            continue
        # Verify body no longer has faces in hang region
        xs = [v[0] for v in verts]
        ys = [v[1] for v in verts]
        minx, maxx = min(xs), max(xs)
        miny, maxy = min(ys), max(ys)
        x0 = minx + (maxx - minx) * 0.22
        x1 = minx + (maxx - minx) * 0.48
        y_cut = miny + (maxy - miny) * 0.38
        leftover = 0
        for i in body_idxs:
            cx, cy, _ = face_centroid(verts, faces[i][1])
            if x0 <= cx <= x1 and cy <= y_cut:
                leftover += 1
        print(f"  leftover hang faces on body={leftover}")


if __name__ == "__main__":
    main()
