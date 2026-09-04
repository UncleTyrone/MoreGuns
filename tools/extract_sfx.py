import os
import py7zr

dl = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Audio\_downloads"
ex = os.path.join(dl, "extracted")
os.makedirs(ex, exist_ok=True)

for name in ["Prepared_SFX_Library.7z", "shots.7z"]:
    path = os.path.join(dl, name)
    if not os.path.isfile(path):
        print("missing", name)
        continue
    dest = os.path.join(ex, os.path.splitext(name)[0])
    os.makedirs(dest, exist_ok=True)
    print("extracting", name, "->", dest)
    with py7zr.SevenZipFile(path, mode="r") as z:
        z.extractall(path=dest)
    print("done", name)
