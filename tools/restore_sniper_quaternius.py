"""Restore Quaternius SniperRifle_2 as sniper_body/sniper_mag with original MTL colors."""
from __future__ import annotations

import importlib.util
import os
import shutil
from collections import defaultdict

spec = importlib.util.spec_from_file_location(
    "resplit",
    r"D:\Schedule I\Schedule I\MoreGuns\tools\resplit_with_materials.py",
)
resplit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(resplit)

SRC = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\_downloads\extracted\quaternius\Ultimate Gun Pack - July 2019\OBJ\SniperRifle_2.obj"
SRC_MTL = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\_downloads\extracted\quaternius\Ultimate Gun Pack - July 2019\OBJ\SniperRifle_2.mtl"
DEST = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\sniper"
UNITY = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Ripped\ExportedProject\Assets\Models\sniper"
MTL_NAME = "sniper.mtl"


def centroid(verts, corners):
    xs = ys = zs = 0.0
    for v, _, _ in corners:
        x, y, z = verts[v - 1]
        xs += x
        ys += y
        zs += z
    n = float(len(corners))
    return xs / n, ys / n, zs / n


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

    # Trigger guard: 58-face under-receiver hang — keep on body
    trigger = None
    mag = None
    for g in groups:
        cs = [centroid(verts, faces[i][1]) for i in g]
        mean_y = sum(c[1] for c in cs) / len(cs)
        mean_x = sum(c[0] for c in cs) / len(cs)
        if 50 <= len(g) <= 70 and mean_y < -0.1 and 0.3 < mean_x < 0.55:
            trigger = g
        # Forward hanging block (not the guard)
        if 80 <= len(g) <= 130 and 1.2 < mean_x < 2.0:
            mag = g

    if trigger is None:
        raise SystemExit("trigger guard island not found")
    if mag is None:
        raise SystemExit("forward mag candidate not found")

    mag_set = set(mag)
    body = [i for i in range(len(faces)) if i not in mag_set]
    print(
        f"body={len(body)} mag={len(mag_set)} "
        f"guard_on_body={sum(1 for i in trigger if i not in mag_set)}/{len(trigger)}"
    )

    shutil.copy2(SRC_MTL, os.path.join(DEST, MTL_NAME))
    resplit.write_obj(
        os.path.join(DEST, "sniper_body.obj"),
        verts, uvs, norms, body, faces, MTL_NAME, "sniper_body",
    )
    resplit.write_obj(
        os.path.join(DEST, "sniper_mag.obj"),
        verts, uvs, norms, sorted(mag), faces, MTL_NAME, "sniper_mag",
    )

    os.makedirs(UNITY, exist_ok=True)
    for name in ("sniper_body.obj", "sniper_mag.obj", MTL_NAME):
        shutil.copy2(os.path.join(DEST, name), os.path.join(UNITY, name))
    print("Restored Quaternius sniper_body/sniper_mag + sniper.mtl")


if __name__ == "__main__":
    main()
