"""Compare thickness of hanging islands — mags are thick, guards are thin."""
import importlib.util
from collections import defaultdict

spec = importlib.util.spec_from_file_location(
    "resplit",
    r"D:\Schedule I\Schedule I\MoreGuns\tools\resplit_with_materials.py",
)
resplit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(resplit)

SRC = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\_downloads\extracted\quaternius\Ultimate Gun Pack - July 2019\OBJ\SniperRifle_2.obj"


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


verts, _, _, faces, _ = resplit.parse_obj(SRC)
groups = islands(faces)
for gi, g in enumerate(groups):
    used = set()
    for i in g:
        for v, _, _ in faces[i][1]:
            used.add(v)
    zs = [verts[v - 1][2] for v in used]
    ys = [verts[v - 1][1] for v in used]
    xs = [verts[v - 1][0] for v in used]
    print(
        f"island {gi}: n={len(g):3d} "
        f"x=[{min(xs):7.3f},{max(xs):7.3f}] "
        f"y=[{min(ys):7.3f},{max(ys):7.3f}] "
        f"z=[{min(zs):7.3f},{max(zs):7.3f}] widthZ={max(zs)-min(zs):.3f}"
    )
