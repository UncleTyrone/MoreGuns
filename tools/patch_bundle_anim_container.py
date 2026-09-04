import shutil
from pathlib import Path
import UnityPy

src = Path(r"D:\Schedule I\Schedule I\MoreGuns\Resources\voidanesguns")
env = UnityPy.load(str(src))
ab_obj = next(o for o in env.objects if o.type.name == "AssetBundle")
tree = ab_obj.read_typetree()
container = list(tree.get("m_Container") or [])
existing = {item[0].lower() for item in container}

def add(path, path_id):
    global container, existing
    if path.lower() in existing:
        return False
    container.append((path, {"preloadIndex": 0, "preloadSize": 1, "asset": {"m_FileID": 0, "m_PathID": path_id}}))
    existing.add(path.lower())
    return True

for obj in env.objects:
    if obj.type.name == "AnimatorController":
        name = obj.read().m_Name
        add(f"assets/animatorcontroller/{name.lower()}.controller", obj.path_id)
    elif obj.type.name == "AnimationClip":
        name = obj.read().m_Name
        add(f"assets/animationclip/{name}.anim", obj.path_id)

tree["m_Container"] = container
ab_obj.save_typetree(tree)

# Try compressed save options
data = None
for kwargs in (
    {"packer": "lz4"},
    {"packer": "lz4hc"},
    {"packer": "lzma"},
    {},
):
    try:
        data = env.file.save(**kwargs) if kwargs else env.file.save()
        print("save ok", kwargs, "size", len(data))
        if len(data) < 8_000_000:
            break
    except Exception as e:
        print("save fail", kwargs, e)

src.write_bytes(data)
print("final", src, src.stat().st_size)
