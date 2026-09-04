import os

root = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Audio\_downloads\extracted\Prepared_SFX_Library"
for dirpath, _, files in os.walk(root):
    for f in sorted(files):
        print(os.path.relpath(os.path.join(dirpath, f), root))
