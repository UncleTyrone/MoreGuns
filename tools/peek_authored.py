import importlib.util
import os

spec = importlib.util.spec_from_file_location(
    "r",
    r"D:\Schedule I\Schedule I\MoreGuns\tools\resplit_with_materials.py",
)
r = importlib.util.module_from_spec(spec)
spec.loader.exec_module(r)

base = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\sniper"
for name in [
    "sniper_body_authored.obj",
    "sniper_mag_authored.obj",
    "sniper_upper_authored.obj",
    "sniper_scope_authored.obj",
    "sniper_body.obj",
    "sniper_mag.obj",
]:
    path = os.path.join(base, name)
    if not os.path.isfile(path):
        print("missing", name)
        continue
    verts, _, _, faces, mtllib = r.parse_obj(path)
    with open(path, encoding="utf-8") as f:
        head = [next(f).rstrip() for _ in range(3)]
    print(f"{name}: v={len(verts)} f={len(faces)} mtllib={mtllib}")
    for line in head:
        print(" ", line)
