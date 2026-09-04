import re
from pathlib import Path

path = Path(r"D:\SteamLibrary\steamapps\common\Schedule I\MelonLoader\Il2CppAssemblies\Assembly-CSharp.dll")
data = path.read_bytes()
names = {x.decode("ascii") for x in re.findall(rb"[\x20-\x7e]{3,40}", data)}
want = [
    "Street_Rat", "Hoodlum", "Peddler", "Hustler", "Bagman", "Enforcer",
    "Shot_Caller", "Underlord", "Boss", "Kingpin", "Baron", "Captain",
    "Lieutenant", "Cartel_Boss", "Dealer", "Crewman", "Soldier",
]
for w in want:
    print(f"{w}: {'YES' if w in names else 'no'}")

idx = data.find(b"Street_Rat")
print("Street_Rat at", idx)
print(data[idx : idx + 300])

# Also search Il2CppScheduleOne.Core
core = Path(r"D:\SteamLibrary\steamapps\common\Schedule I\MelonLoader\Il2CppAssemblies\Il2CppScheduleOne.Core.dll")
if core.exists():
    cdata = core.read_bytes()
    idx2 = cdata.find(b"Street_Rat")
    print("core Street_Rat at", idx2)
    if idx2 >= 0:
        print(cdata[idx2 : idx2 + 300])
