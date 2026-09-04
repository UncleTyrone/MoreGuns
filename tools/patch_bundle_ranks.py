"""Patch RequiredRank + BasePurchasePrice on new-gun item defs inside voidanesguns."""
from pathlib import Path
import UnityPy

BUNDLE = Path(r"D:\Schedule I\Schedule I\MoreGuns\Resources\voidanesguns")

PATCHES = {
    "smg": {"BasePurchasePrice": 8500.0, "Rank": 6, "Tier": 3},
    "smg_magazine": {"BasePurchasePrice": 600.0, "Rank": 6, "Tier": 3},
    "sniper": {"BasePurchasePrice": 22000.0, "Rank": 7, "Tier": 3},
    "sniper_magazine": {"BasePurchasePrice": 1500.0, "Rank": 7, "Tier": 3},
    "rpg": {"BasePurchasePrice": 65000.0, "Rank": 10, "Tier": 1},
    "rpg_magazine": {"BasePurchasePrice": 8000.0, "Rank": 10, "Tier": 1},
}

env = UnityPy.load(str(BUNDLE))
patched = 0
for obj in env.objects:
    if obj.type.name != "MonoBehaviour":
        continue
    try:
        data = obj.read()
    except Exception:
        continue
    name = (getattr(data, "name", None) or getattr(data, "m_Name", "") or "").lower()
    if name not in PATCHES:
        continue
    p = PATCHES[name]
    data.BasePurchasePrice = float(p["BasePurchasePrice"])
    rr = data.RequiredRank
    # RequiredRank may be dict-like or object with Rank/Tier
    if hasattr(rr, "Rank"):
        rr.Rank = int(p["Rank"])
        rr.Tier = int(p["Tier"])
    elif isinstance(rr, dict):
        rr["Rank"] = int(p["Rank"])
        rr["Tier"] = int(p["Tier"])
        data.RequiredRank = rr
    else:
        raise SystemExit(f"Unknown RequiredRank type for {name}: {type(rr)} {rr}")
    if hasattr(data, "RequiresLevelToPurchase"):
        data.RequiresLevelToPurchase = 1
    data.save()
    patched += 1
    print(f"patched {name}: price={data.BasePurchasePrice} rank={rr.Rank if hasattr(rr,'Rank') else rr} tier={rr.Tier if hasattr(rr,'Tier') else rr.get('Tier')}")

if patched != len(PATCHES):
    raise SystemExit(f"expected {len(PATCHES)} patches, got {patched}")

with open(BUNDLE, "wb") as f:
    f.write(env.file.save())
print(f"wrote {BUNDLE} ({BUNDLE.stat().st_size} bytes)")
