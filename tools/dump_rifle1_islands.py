"""Dump all SniperRifle_1 islands with bounds."""
from __future__ import annotations

import importlib.util
from collections import Counter, defaultdict

spec = importlib.util.spec_from_file_location(
    "resplit",
    r"D:\Schedule I\Schedule I\MoreGuns\tools\resplit_with_materials.py",
)
resplit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(resplit)

SRC = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\_downloads\extracted\quaternius\Ultimate Gun Pack - July 2019\OBJ\SniperRifle_1.obj"


def cen(verts, corners):
    xs = ys = zs = 0.0
    for v, _, _ in corners:
        x, y, z = verts[v - 1]
        xs += x
        ys += y
        zs += z
    n = float(len(corners))
    return xs / n, ys / n, zs / n


def bounds(verts, idxs, faces):
    xs, ys, zs = [], [], []
    for i in idxs:
        for v, _, _ in faces[i][1]:
            x, y, z = verts[v - 1]
            xs.append(x)
            ys.append(y)
            zs.append(z)
    return min(xs), max(xs), min(ys), max(ys), min(zs), max(zs)


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


def main():
    verts, _, _, faces, _ = resplit.parse_obj(SRC)
    groups = islands(faces)
    for gi, g in enumerate(sorted(groups, key=lambda x: -len(x))):
        xmin, xmax, ymin, ymax, zmin, zmax = bounds(verts, g, faces)
        cs = [cen(verts, faces[i][1]) for i in g]
        print(
            f"isl n={len(g):3d} "
            f"x=[{xmin:7.3f},{xmax:7.3f}] y=[{ymin:7.3f},{ymax:7.3f}] z=[{zmin:6.3f},{zmax:6.3f}] "
            f"mean=({sum(c[0] for c in cs)/len(cs):6.3f},{sum(c[1] for c in cs)/len(cs):6.3f}) "
            f"mats={dict(Counter(faces[i][0] for i in g))}"
        )


if __name__ == "__main__":
    main()
