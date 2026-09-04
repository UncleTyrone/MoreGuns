from pathlib import Path

root = Path(
    r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Ripped\ExportedProject\Assets\resources\weapons"
)
pairs = [
    (root / "sniper" / "Sniper_Stored.prefab.meta", "c00479c8cbf14a699d0ec6042211a8fc"),
    (root / "sniper" / "magazine" / "Sniper_Magazine_Stored.prefab.meta", "03122ab019cc43179009c01247d70d22"),
    (root / "smg" / "SMG_Stored.prefab.meta", "f63e88e24f4f448ab0ef0372c40ebe04"),
    (root / "smg" / "magazine" / "SMG_Magazine_Stored.prefab.meta", "5581bba37c244a5c9dc0b6e881aede1c"),
    (root / "rpg" / "RPG_Stored.prefab.meta", "a3a4618ba16640b9a5e176bad2c69732"),
    (root / "rpg" / "magazine" / "RPG_Magazine_Stored.prefab.meta", "85c6571860f24492a22c3087e63bae21"),
]
tpl = """fileFormatVersion: 2
guid: {guid}
timeCreated: 1788452623
licenseType: Free
PrefabImporter:
  externalObjects: {{}}
  addedObjectFileIDs:
  isPrefabVariant: 0
  variantParentGUID: 00000000000000000000000000000000
  userData:
  assetBundleName: voidanesguns
  assetBundleVariant:
"""
for path, guid in pairs:
    path.write_text(tpl.format(guid=guid), encoding="utf-8")
    print("wrote", path.name, guid)
