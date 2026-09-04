from pathlib import Path
import UnityPy

BUNDLE = Path(r"D:\Schedule I\Schedule I\MoreGuns\Resources\voidanesguns")
env = UnityPy.load(str(BUNDLE))

for obj in env.objects:
    if obj.type.name != "MonoBehaviour":
        continue
    try:
        data = obj.read()
        tree = data.read_typetree()
    except Exception as e:
        continue
    name = tree.get("m_Name")
    iid = tree.get("ID")
    if iid in ("smg", "sniper", "rpg", "smg_magazine", "sniper_magazine", "rpg_magazine", "ak47") or (
        isinstance(name, str) and name.lower() in ("smg", "sniper", "rpg", "ak47")
    ):
        print("---")
        print("keys sample:", list(tree.keys())[:40])
        print("m_Name=", name, "ID=", iid)
        print("BasePurchasePrice=", tree.get("BasePurchasePrice"))
        print("RequiredRank=", tree.get("RequiredRank"))
        print("RequiresLevelToPurchase=", tree.get("RequiresLevelToPurchase"))
