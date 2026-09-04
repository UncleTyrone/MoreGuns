"""Inspect island 3 (upper receiver) for downward mag protrusion; also dump island 0."""
import importlib.util
from collections import defaultdict

spec = importlib.util.spec_from_file_location(
    "resplit",
    r"D:\Schedule I\Schedule I\MoreGuns\tools\resplit_with_materials.py",
)
resplit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(resplit)

SRC = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\_downloads\extracted\quaternius\Ultimate Gun Pack - July 2019\OBJ\SniperRifle_2.obj"
OUT = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\sniper\_debug_islands"


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


import os

verts, uvs, norms, faces, mtllib = resplit.parse_obj(SRC)
groups = islands(faces)
os.makedirs(OUT, exist_ok=True)

# Export every island as its own OBJ for visual ID in Unity
for gi, g in enumerate(groups):
    cs = [centroid(verts, faces[i][1]) for i in g]
    mean_y = sum(c[1] for c in cs) / len(cs)
    mean_x = sum(c[0] for c in cs) / len(cs)
    min_y = min(c[1] for c in cs)
    path = os.path.join(OUT, f"island_{gi:02d}_n{len(g)}_x{mean_x:.2f}_y{mean_y:.2f}.obj")
    resplit.write_obj(path, verts, uvs, norms, g, faces, "sniper.mtl", f"island_{gi}")
    print(f"wrote {os.path.basename(path)} minY={min_y:.3f}")

# Also restore full unsplit body as sniper_body NOW (reassemble)
full = list(range(len(faces)))
dest = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\sniper"
unity = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Ripped\ExportedProject\Assets\Models\sniper"
resplit.write_obj(os.path.join(dest, "sniper_body.obj"), verts, uvs, norms, full, faces, "sniper.mtl", "sniper_body")
# clear wrong mag - write empty placeholder note by copying island that looks most mag-like
# For now copy island 0 as candidate "forward mag/bipod" AND keep trigger as separate
print("Restored FULL sniper_body.obj (complete rifle, trigger guard included).")
import shutil
shutil.copy2(os.path.join(dest, "sniper_body.obj"), os.path.join(unity, "sniper_body.obj"))
shutil.copy2(os.path.join(dest, "sniper.mtl"), os.path.join(unity, "sniper.mtl"))
# Copy debug islands into Unity project too
ud = os.path.join(unity, "_debug_islands")
os.makedirs(ud, exist_ok=True)
for f in os.listdir(OUT):
    shutil.copy2(os.path.join(OUT, f), os.path.join(ud, f))
print("Debug islands in Assets/Models/sniper/_debug_islands — drop into scene to see which is the mag.")
