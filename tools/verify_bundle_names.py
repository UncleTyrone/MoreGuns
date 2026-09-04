import UnityPy

env = UnityPy.load("Resources/voidanesguns")
names = set()
for obj in env.objects:
    try:
        data = obj.read()
    except Exception:
        continue
    name = getattr(data, "name", None) or getattr(data, "m_Name", None)
    if name:
        names.add(name)

needles = ("sniper", "smg", "rpg", "ak47", "minigun")
for name in sorted(n for n in names if any(x in n.lower() for x in needles)):
    print(name)
