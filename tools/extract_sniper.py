import os
import zipfile

root = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\_downloads"
out = os.path.join(root, "extracted", "Sniper_1A")
os.makedirs(out, exist_ok=True)
with zipfile.ZipFile(os.path.join(root, "Sniper_1A.zip")) as z:
    z.extractall(out)
print("extracted to", out)
for dirpath, _, files in os.walk(out):
    for f in files:
        print(os.path.join(dirpath, f), os.path.getsize(os.path.join(dirpath, f)))
