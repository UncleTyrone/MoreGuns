"""Find faces on island 1 (main stock/receiver) that look like a box magazine."""
from __future__ import annotations

import importlib.util
from collections import Counter, defaultdict

spec = importlib.util.spec_from_file_location(
    "resplit",
    r"D:\Schedule I\Schedule I\MoreGuns\tools\resplit_with_materials.py",
)
resplit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(resplit)

SRC = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\_downloads\extracted\quaternius\Ultimate Gun Pack - July 2019\OBJ\SniperRifle_2.obj"


def centroid(verts, corners):
    xs = ys = zs = 0.0
    for v, _, _ in corners:
        x, y, z = verts[v - 1]
        xs += x
        ys += y
        zs += z
    n = float(len(corners))
    return xs / n, ys / n, zs / n


def face_bounds(verts, corners):
    xs, ys, zs = [], [], []
    for v, _, _ in corners:
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
    verts, uvs, norms, faces, _ = resplit.parse_obj(SRC)
    groups = islands(faces)
    # island 1 is the big stock one (178 faces)
    island1 = max(groups, key=lambda g: len(g) if abs(sum(centroid(verts, faces[i][1])[0] for i in g)/len(g)) < 2 else 0)
    # pick by size ~178
    for g in groups:
        if len(g) == 178:
            island1 = g
            break

    # Candidate mag faces: mid-gun X, hanging below receiver (y < -0.05), not the far stock butt (x > -0.5)
    cands = []
    for i in island1:
        cx, cy, cz = centroid(verts, faces[i][1])
        xmin, xmax, ymin, ymax, zmin, zmax = face_bounds(verts, faces[i][1])
        if cy < -0.05 and xmin > -0.2 and xmax < 1.2 and ymin < -0.08:
            cands.append((i, cx, cy, cz, faces[i][0], ymin, ymax, zmax - zmin))

    print(f"island1 size={len(island1)} candidate mag-like faces={len(cands)}")
    for i, cx, cy, cz, mat, ymin, ymax, zw in sorted(cands, key=lambda t: t[2])[:40]:
        print(f"  f{i:4d} mat={mat:12s} c=({cx:6.3f},{cy:6.3f},{cz:6.3f}) y=[{ymin:6.3f},{ymax:6.3f}] zw={zw:.3f}")

    # Also list ALL islands with mean_y < 0
    print("\nAll islands meanY<0.05:")
    for gi, g in enumerate(groups):
        cs = [centroid(verts, faces[i][1]) for i in g]
        my = sum(c[1] for c in cs) / len(cs)
        mx = sum(c[0] for c in cs) / len(cs)
        if my < 0.05:
            ys = [c[1] for c in cs]
            zs = []
            for i in g:
                for v, _, _ in faces[i][1]:
                    zs.append(verts[v - 1][2])
            print(
                f"  island {gi}: n={len(g):3d} meanX={mx:6.3f} meanY={my:6.3f} "
                f"y=[{min(ys):.3f},{max(ys):.3f}] zW={max(zs)-min(zs):.3f} mats={dict(Counter(faces[i][0] for i in g))}"
            )


if __name__ == "__main__":
    main()
