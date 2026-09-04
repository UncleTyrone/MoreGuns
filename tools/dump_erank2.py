from pathlib import Path
import re

path = Path(r"D:\SteamLibrary\steamapps\common\Schedule I\MelonLoader\Il2CppAssemblies\Assembly-CSharp.dll")
data = path.read_bytes()

# Look for Il2Cpp enum field names pattern: often listed together
patterns = [
    b"Street_Rat\x00Hoodlum\x00Peddler\x00Hustler\x00Bagman\x00Enforcer\x00Shot_Caller",
    b"Street_Rat\x00Hoodlum\x00Peddler\x00Hustler\x00Bagman\x00Enforcer",
]
for p in patterns:
    i = data.find(p)
    print("pattern", p[:40], "at", i)
    if i >= 0:
        chunk = data[i:i+200]
        parts = chunk.split(b"\x00")
        print([x.decode("ascii", "replace") for x in parts[:20]])

# Search for Underlord surrounded by other ranks
for m in re.finditer(rb"Underlord\x00[A-Za-z_]{3,20}\x00", data):
    start = max(0, m.start() - 120)
    chunk = data[start:m.end()+80]
    parts = [x.decode("ascii", "replace") for x in chunk.split(b"\x00") if x]
    rankish = [p for p in parts if re.fullmatch(r"[A-Za-z_]{3,20}", p)]
    print("near Underlord:", rankish[-15:])
    break

# Find FullRank / Rank display mapping tables
for name in [b"Kingpin", b"Baron", b"Underlord", b"Shot_Caller"]:
    print(name, data.count(name))
