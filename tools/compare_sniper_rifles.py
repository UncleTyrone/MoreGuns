"""Compare hang islands across Quaternius SniperRifle OBJs."""
from __future__ import annotations

import importlib.util
import os
from collections import defaultdict

spec = importlib.util.spec_from_file_location(
    "resplit",
    r"D:\Schedule I\Schedule I\MoreGuns\tools\resplit_with_materials.py",
)
resplit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(resplit)

BASE = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\_downloads\extracted\quaternius\Ultimate Gun Pack - July 2019\OBJ"


def islands(faces):
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
    return groups


def cen(verts, corners):
    xs = ys = zs = 0.0
    for v, _, _ in corners:
        x, y, z = verts[v - 1]
        xs += x
        ys += y
        zs += z
    n = float(len(corners))
    return xs / n, ys / n, zs / n


def main():
    for name in sorted(os.listdir(BASE)):
        if not name.startswith("SniperRifle") or not name.endswith(".obj"):
            continue
        path = os.path.join(BASE, name)
        verts, _, _, faces, _ = resplit.parse_obj(path)
        gs = islands(faces)
        hangs = []
        for gi, g in enumerate(gs):
            cs = [cen(verts, faces[i][1]) for i in g]
            my = sum(c[1] for c in cs) / len(cs)
            mx = sum(c[0] for c in cs) / len(cs)
            if my < -0.05 and 20 < len(g) < 250:
                hangs.append((gi, len(g), round(mx, 2), round(my, 2)))
        print(f"{name}: faces={len(faces)} islands={len(gs)} hangs={hangs}")


if __name__ == "__main__":
    main()
