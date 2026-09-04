from pathlib import Path
import UnityPy

BUNDLE = Path(r"D:\Schedule I\Schedule I\MoreGuns\Resources\voidanesguns")
env = UnityPy.load(str(BUNDLE))

for obj in env.objects:
    if obj.type.name != "MonoBehaviour":
        continue
    try:
        data = obj.read()
    except Exception:
        continue
    name = getattr(data, "name", None) or getattr(data, "m_Name", "") or ""
    if not name:
        continue
    n = str(name).lower()
    if n not in ("smg", "sniper", "rpg", "smg_magazine", "sniper_magazine", "rpg_magazine", "ak47", "minigun"):
        continue
    print("name=", name, "type=", type(data))
    # try typetree
    try:
        tree = data.read_typetree()
        print("  typetree keys:", [k for k in tree.keys() if "Rank" in k or "Price" in k or k in ("ID", "Name", "m_Name")])
        print("  ID=", tree.get("ID"), "Price=", tree.get("BasePurchasePrice"), "RR=", tree.get("RequiredRank"))
    except Exception as e:
        print("  typetree fail:", e)
    # try raw
    try:
        print("  vars:", [a for a in dir(data) if "Rank" in a or "Price" in a or a in ("ID", "Name")])
    except Exception:
        pass
