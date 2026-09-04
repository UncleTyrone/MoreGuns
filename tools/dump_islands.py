"""Print connected-island stats for an OBJ so mag/body picks can be checked."""
import os
import sys

sys.path.insert(0, r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models")
from _split_obj import connected_face_groups, face_centroid, load_obj


def dump(path):
    verts, faces, _ = load_obj(path)
    groups = connected_face_groups(faces)
    print(f"\n=== {os.path.basename(path)} islands={len(groups)} faces={len(faces)} ===")
    rows = []
    for gi, g in enumerate(groups):
        gfaces = [faces[i] for i in g]
        cs = [face_centroid(verts, fa) for fa in gfaces]
        ys = [c[1] for c in cs]
        xs = [c[0] for c in cs]
        zs = [c[2] for c in cs]
        rows.append(
            (
                len(gfaces),
                sum(ys) / len(ys),
                min(ys),
                max(ys),
                sum(xs) / len(xs),
                min(xs),
                max(xs),
                gi,
            )
        )
    rows.sort()
    print("faces  meanY   minY    maxY    meanX   minX    maxX    id")
    for r in rows:
        print(f"{r[0]:5d}  {r[1]:6.3f}  {r[2]:6.3f}  {r[3]:6.3f}  {r[4]:6.3f}  {r[5]:6.3f}  {r[6]:6.3f}  {r[7]}")


q = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\_downloads\extracted\quaternius\Ultimate Gun Pack - July 2019\OBJ"
for name in ["SniperRifle_2.obj", "SubmachineGun_3.obj", "SubmachineGun_1.obj"]:
    dump(os.path.join(q, name))
