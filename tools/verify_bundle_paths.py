import UnityPy

env = UnityPy.load("Resources/voidanesguns")
for path, obj in sorted((env.container or {}).items()):
    if any(x in path.lower() for x in ("sniper", "smg", "rpg", "ak47", "minigun", "ui/")):
        print(path)
