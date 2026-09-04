"""
Rebuild sniper_body / sniper_mag from already-exported authored OBJs.

Body = Body + Upper_Body + Scope (no magazine)
Mag  = Magazine
"""
from __future__ import annotations

import os
import shutil

DEST = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\sniper"
UNITY = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Ripped\ExportedProject\Assets\Models\sniper"
MTL = "sniper_parts.mtl"


def parse_obj_geom(path: str):
    verts, uvs, norms, faces = [], [], [], []
    with open(path, "r", encoding="utf-8", errors="ignore") as f:
        for line in f:
            if line.startswith("v "):
                verts.append(tuple(float(x) for x in line.split()[1:4]))
            elif line.startswith("vt "):
                parts = line.split()
                uvs.append((float(parts[1]), float(parts[2])))
            elif line.startswith("vn "):
                norms.append(tuple(float(x) for x in line.split()[1:4]))
            elif line.startswith("f "):
                corners = []
                for tok in line.split()[1:]:
                    bits = tok.split("/")
                    v = int(bits[0])
                    vt = int(bits[1]) if len(bits) > 1 and bits[1] else None
                    vn = int(bits[2]) if len(bits) > 2 and bits[2] else None
                    corners.append((v, vt, vn))
                faces.append(corners)
    return verts, uvs, norms, faces


def append_mesh(dst_v, dst_vt, dst_vn, dst_f, src_path):
    verts, uvs, norms, faces = parse_obj_geom(src_path)
    v_off = len(dst_v)
    vt_off = len(dst_vt)
    vn_off = len(dst_vn)
    dst_v.extend(verts)
    dst_vt.extend(uvs)
    dst_vn.extend(norms)
    for corners in faces:
        remapped = []
        for v, vt, vn in corners:
            remapped.append(
                (
                    v + v_off,
                    (vt + vt_off) if vt is not None else None,
                    (vn + vn_off) if vn is not None else None,
                )
            )
        dst_f.append(remapped)
    print(f"  + {os.path.basename(src_path)}: v={len(verts)} f={len(faces)}")


def write_obj(path, verts, uvs, norms, faces, object_name):
    with open(path, "w", encoding="utf-8") as f:
        f.write(f"mtllib {MTL}\n")
        f.write(f"o {object_name}\n")
        for x, y, z in verts:
            f.write(f"v {x:.6f} {y:.6f} {z:.6f}\n")
        for u, v in uvs:
            f.write(f"vt {u:.6f} {v:.6f}\n")
        for x, y, z in norms:
            f.write(f"vn {x:.6f} {y:.6f} {z:.6f}\n")
        f.write("usemtl Sniper_parts\n")
        for corners in faces:
            bits = []
            for vi, vti, vni in corners:
                if vti is not None and vni is not None:
                    bits.append(f"{vi}/{vti}/{vni}")
                elif vti is not None:
                    bits.append(f"{vi}/{vti}")
                elif vni is not None:
                    bits.append(f"{vi}//{vni}")
                else:
                    bits.append(str(vi))
            f.write("f " + " ".join(bits) + "\n")
    print(f"wrote {path} v={len(verts)} f={len(faces)}")


def main():
    # Restore Quaternius full rifle for reference (unsplit)
    q_src = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\_downloads\extracted\quaternius\Ultimate Gun Pack - July 2019\OBJ\SniperRifle_2.obj"
    q_mtl = os.path.join(os.path.dirname(q_src), "SniperRifle_2.mtl")
    if os.path.isfile(q_src):
        shutil.copy2(q_src, os.path.join(DEST, "sniper_full_quaternius.obj"))
        if os.path.isfile(q_mtl):
            shutil.copy2(q_mtl, os.path.join(DEST, "sniper_full_quaternius.mtl"))
        print("Saved unsplit Quaternius as sniper_full_quaternius.obj")

    body_parts = [
        os.path.join(DEST, "sniper_body_authored.obj"),
        os.path.join(DEST, "sniper_upper_authored.obj"),
        os.path.join(DEST, "sniper_scope_authored.obj"),
    ]
    mag_src = os.path.join(DEST, "sniper_mag_authored.obj")
    for p in body_parts + [mag_src]:
        if not os.path.isfile(p):
            raise SystemExit(f"missing {p}")

    bv, bu, bn, bf = [], [], [], []
    print("Building sniper_body from authored parts (no magazine):")
    for p in body_parts:
        append_mesh(bv, bu, bn, bf, p)

    mv, mu, mn, mf = [], [], [], []
    print("Building sniper_mag from authored Magazine:")
    append_mesh(mv, mu, mn, mf, mag_src)

    # Ensure MTL exists
    mtl_path = os.path.join(DEST, MTL)
    if not os.path.isfile(mtl_path):
        with open(mtl_path, "w", encoding="utf-8") as f:
            f.write("newmtl Sniper_parts\nKd 1 1 1\nmap_Kd Sniper_parts.png\n")

    write_obj(os.path.join(DEST, "sniper_body.obj"), bv, bu, bn, bf, "sniper_body")
    write_obj(os.path.join(DEST, "sniper_mag.obj"), mv, mu, mn, mf, "sniper_mag")

    os.makedirs(UNITY, exist_ok=True)
    for name in (
        "sniper_body.obj",
        "sniper_mag.obj",
        MTL,
        "Sniper_parts.png",
    ):
        src = os.path.join(DEST, name)
        if os.path.isfile(src):
            shutil.copy2(src, os.path.join(UNITY, name))

    print("Done. Trigger guard is intact on body; magazine is only in sniper_mag.obj.")


if __name__ == "__main__":
    main()
