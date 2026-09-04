from pathlib import Path
d = Path(r"D:\SteamLibrary\steamapps\common\Schedule I\MelonLoader\Il2CppAssemblies\Assembly-CSharp.dll").read_bytes()
for n in [b"Block_Boss", b"BlockBoss", b"Shot_Caller", b"Street_Rat", b"Kingpin", b"Baron"]:
    print(n.decode(), d.find(n) >= 0)
