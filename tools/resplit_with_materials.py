"""Split Quaternius OBJs into body/mag while keeping UVs, normals, and materials."""
from __future__ import annotations

import os
import shutil
from collections import defaultdict


def parse_obj(path: str):
    verts, uvs, norms = [], [], []
    # face: list of (v, vt, vn) 1-based, vt/vn may be None
    # groups by material in order
    faces = []  # (mtl, [(v,vt,vn),...])
    mtllib = None
    cur_mtl = "Default"
    with open(path, "r", encoding="utf-8", errors="ignore") as f:
        for line in f:
            if line.startswith("mtllib "):
                mtllib = line.split(None, 1)[1].strip()
            elif line.startswith("v "):
                verts.append(tuple(float(x) for x in line.split()[1:4]))
            elif line.startswith("vt "):
                parts = line.split()
                uvs.append(tuple(float(x) for x in parts[1:3]))
            elif line.startswith("vn "):
                norms.append(tuple(float(x) for x in line.split()[1:4]))
            elif line.startswith("usemtl "):
                cur_mtl = line.split(None, 1)[1].strip()
            elif line.startswith("f "):
                corners = []
                for tok in line.split()[1:]:
                    bits = tok.split("/")
                    v = int(bits[0])
                    vt = int(bits[1]) if len(bits) > 1 and bits[1] else None
                    vn = int(bits[2]) if len(bits) > 2 and bits[2] else None
                    corners.append((v, vt, vn))
                faces.append((cur_mtl, corners))
    return verts, uvs, norms, faces, mtllib


def face_centroid(verts, corners):
    xs = ys = zs = 0.0
    for v, _, _ in corners:
        x, y, z = verts[v - 1]
        xs += x
        ys += y
        zs += z
    n = float(len(corners))
    return xs / n, ys / n, zs / n


def connected_groups(faces, verts):
    # connect by shared vertex indices
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


def pick_mag(verts, faces, groups):
    stats = []
    for g in groups:
        cs = [face_centroid(verts, faces[i][1]) for i in g]
        ys = [c[1] for c in cs]
        stats.append(
            {
                "idxs": g,
                "count": len(g),
                "mean_y": sum(ys) / len(ys),
                "min_y": min(ys),
            }
        )
    stats.sort(key=lambda s: s["count"], reverse=True)
    body = stats[0]
    # Mag: smaller island hanging lower
    cands = [s for s in stats[1:] if s["count"] >= 8]
    cands.sort(key=lambda s: (s["mean_y"], -s["count"]))
    if not cands:
        return None, None
    mag = cands[0]
    # Prefer the hanging clip: low mean_y and not the huge barrel
    hanging = [s for s in cands if s["mean_y"] < body["mean_y"] - 0.05 and s["count"] <= 120]
    if hanging:
        hanging.sort(key=lambda s: (s["mean_y"], -s["count"]))
        mag = hanging[0]
    body_idxs = []
    for s in stats:
        if s is mag:
            continue
        body_idxs.extend(s["idxs"])
    return body_idxs, mag["idxs"]


def write_obj(path, verts, uvs, norms, face_idxs, faces, mtllib, obj_name):
    used_v, used_vt, used_vn = set(), set(), set()
    selected = [faces[i] for i in face_idxs]
    for _, corners in selected:
        for v, vt, vn in corners:
            used_v.add(v)
            if vt:
                used_vt.add(vt)
            if vn:
                used_vn.add(vn)
    map_v = {old: i for i, old in enumerate(sorted(used_v), 1)}
    map_vt = {old: i for i, old in enumerate(sorted(used_vt), 1)} if used_vt else {}
    map_vn = {old: i for i, old in enumerate(sorted(used_vn), 1)} if used_vn else {}

    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        f.write(f"# split from source with materials\n")
        if mtllib:
            f.write(f"mtllib {mtllib}\n")
        f.write(f"o {obj_name}\n")
        for old in sorted(used_v):
            x, y, z = verts[old - 1]
            f.write(f"v {x:.6f} {y:.6f} {z:.6f}\n")
        for old in sorted(used_vt):
            u, vv = uvs[old - 1]
            f.write(f"vt {u:.6f} {vv:.6f}\n")
        for old in sorted(used_vn):
            x, y, z = norms[old - 1]
            f.write(f"vn {x:.6f} {y:.6f} {z:.6f}\n")
        cur = None
        for mtl, corners in selected:
            if mtl != cur:
                f.write(f"usemtl {mtl}\n")
                cur = mtl
            bits = []
            for v, vt, vn in corners:
                s = str(map_v[v])
                if vt and vn:
                    s += f"/{map_vt[vt]}/{map_vn[vn]}"
                elif vt:
                    s += f"/{map_vt[vt]}"
                elif vn:
                    s += f"//{map_vn[vn]}"
                bits.append(s)
            f.write("f " + " ".join(bits) + "\n")


def split_one(src_obj, dest_dir, prefix, mtl_name=None):
    verts, uvs, norms, faces, mtllib = parse_obj(src_obj)
    groups = connected_groups(faces, verts)
    body_idxs, mag_idxs = pick_mag(verts, faces, groups)
    if body_idxs is None:
        print("FAIL", src_obj)
        return False
    if mtl_name is None:
        mtl_name = mtllib or f"{prefix}.mtl"
    write_obj(
        os.path.join(dest_dir, f"{prefix}_body.obj"),
        verts, uvs, norms, body_idxs, faces, mtl_name, f"{prefix}_body",
    )
    write_obj(
        os.path.join(dest_dir, f"{prefix}_mag.obj"),
        verts, uvs, norms, mag_idxs, faces, mtl_name, f"{prefix}_mag",
    )
    # copy mtl next to objs
    src_mtl = os.path.join(os.path.dirname(src_obj), mtllib or "")
    if mtllib and os.path.isfile(src_mtl):
        shutil.copy2(src_mtl, os.path.join(dest_dir, mtl_name))
    print(
        f"OK {prefix} body={len(body_idxs)} mag={len(mag_idxs)} uvs={len(uvs)} mtllib={mtl_name}"
    )
    return True


def main():
    root = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models"
    q = os.path.join(
        root,
        r"_downloads\extracted\quaternius\Ultimate Gun Pack - July 2019\OBJ",
    )
    split_one(
        os.path.join(q, "SniperRifle_2.obj"),
        os.path.join(root, "sniper"),
        "sniper",
        "sniper.mtl",
    )
    split_one(
        os.path.join(q, "SubmachineGun_3.obj"),
        os.path.join(root, "smg"),
        "smg",
        "smg.mtl",
    )


if __name__ == "__main__":
    main()
