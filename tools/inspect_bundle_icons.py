import UnityPy
from pathlib import Path

bundle = Path(r"D:\Schedule I\Schedule I\MoreGuns\Resources\voidanesguns")
env = UnityPy.load(str(bundle))

print("=== Icon-ish Texture2D / Sprite / MonoBehaviour ===")
for obj in env.objects:
    if obj.type.name not in ("Texture2D", "Sprite", "MonoBehaviour"):
        continue
    try:
        data = obj.read()
    except Exception as e:
        continue
    name = getattr(data, "name", None) or getattr(data, "m_Name", "") or ""
    n = name.lower()
    if not any(x in n for x in ("sniper", "smg", "rpg", "icon", "ak47__")):
        continue
    extra = ""
    if obj.type.name == "Texture2D":
        try:
            extra = f" {data.m_Width}x{data.m_Height}"
        except Exception:
            pass
    if obj.type.name == "Sprite":
        try:
            rd = data.m_RD if hasattr(data, "m_RD") else None
            tex = getattr(getattr(rd, "texture", None), "m_PathID", None) if rd else None
            extra = f" tex_pathid={tex}"
        except Exception:
            pass
    print(f"{obj.type.name:14} {name!r}{extra} path_id={obj.path_id}")

print("\n=== Item defs Icon fields ===")
for obj in env.objects:
    if obj.type.name != "MonoBehaviour":
        continue
    try:
        data = obj.read()
        tree = data.read_typetree()
    except Exception:
        continue
    name = (tree.get("m_Name") or tree.get("Name") or "").lower()
    if name not in ("sniper", "smg", "rpg", "sniper_magazine", "smg_magazine", "rpg_magazine", "ak47", "ak47_magazine"):
        # also check ID field
        iid = (tree.get("ID") or "").lower()
        if iid not in ("sniper", "smg", "rpg", "sniper_magazine", "smg_magazine", "rpg_magazine", "ak47"):
            continue
        name = iid or name
    icon = tree.get("Icon")
    print(f"{name}: Icon={icon}")
