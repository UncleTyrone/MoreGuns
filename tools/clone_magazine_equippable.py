"""Clone AK47_Magazine_Equippable for sniper / smg / rpg and wire magazine.asset Equippable."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(
    r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Ripped\ExportedProject\Assets\resources\weapons"
)
SRC = ROOT / "ak47" / "magazine" / "AK47_Magazine_Equippable.prefab"

META = """fileFormatVersion: 2
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

# Magazine + LOD1 mesh/materials taken from each gun's Magazine_AvatarEquippable
GUNS = [
    {
        "folder": "sniper",
        "name": "Sniper_Magazine_Equippable",
        "guid": "b7e4c1a92f0d4e6ab3c8d1e5f6071829",
        "avatar_guid": "83cf828e81c32124fb9ceeaf02af022c",
        "asset": "sniper_magazine.asset",
        "mesh": "{fileID: -2432090755550338912, guid: 8d8fc57661dcd6744bde702e3f5c0d35, type: 3}",
        "materials": "  - {fileID: -1580535799172915004, guid: 8d8fc57661dcd6744bde702e3f5c0d35, type: 3}\n",
    },
    {
        "folder": "smg",
        "name": "SMG_Magazine_Equippable",
        "guid": "c8f5d2b03e1e4f7bc4d9e2f60718293a",
        "avatar_guid": "cf977e22a25fd2644845c1a55de6b42a",
        "asset": "smg_magazine.asset",
        "mesh": "{fileID: -2432090755550338912, guid: 3cdcd0ad5396aae4bae32b6f3c1eeedf, type: 3}",
        "materials": (
            "  - {fileID: -1391134871863005975, guid: 3cdcd0ad5396aae4bae32b6f3c1eeedf, type: 3}\n"
            "  - {fileID: -1580535799172915004, guid: 3cdcd0ad5396aae4bae32b6f3c1eeedf, type: 3}\n"
        ),
    },
    {
        "folder": "rpg",
        "name": "RPG_Magazine_Equippable",
        "guid": "d9a6e3c14f2f408cd5e0f3718293ab4b",
        "avatar_guid": "11e5963d0dd484d4d92cdc59bd2e8b69",
        "asset": "rpg_magazine.asset",
        "mesh": "{fileID: -6868162317503869489, guid: 286e6759a9cbf344ebc3159f6d2e9815, type: 3}",
        "materials": "  - {fileID: 3924586991699607848, guid: 286e6759a9cbf344ebc3159f6d2e9815, type: 3}\n",
    },
]

AK_MAG_MESH = "{fileID: 4300000, guid: 369d7d9bc9445444c9e21d43ca1d272f, type: 2}"
AK_LOD_MESH = "{fileID: 4300000, guid: 135b73dcff88a414a9b6f6bbb4874dec, type: 2}"
AK_MAT = "  - {fileID: 2100000, guid: d74d20f6f0fc9ce45a93ed18e13cadce, type: 2}\n"
AK_AVATAR = "guid: 62ac05b5eb12e7648acecee4282ec676"
AK_EQUIPPABLE_GUID = "bbc578203c017314583e48dbcf01f661"


def main():
    src = SRC.read_text(encoding="utf-8")
    for gun in GUNS:
        text = src
        text = text.replace("m_Name: AK47_Magazine_Equippable", f"m_Name: {gun['name']}")
        text = text.replace(AK_AVATAR, f"guid: {gun['avatar_guid']}")
        text = text.replace(AK_MAG_MESH, gun["mesh"])
        text = text.replace(AK_LOD_MESH, gun["mesh"])
        text = text.replace(AK_MAT, gun["materials"])

        dest = ROOT / gun["folder"] / "magazine" / f"{gun['name']}.prefab"
        dest.write_text(text, encoding="utf-8")
        dest.with_suffix(".prefab.meta").write_text(META.format(guid=gun["guid"]), encoding="utf-8")

        asset = ROOT / gun["folder"] / "magazine" / gun["asset"]
        asset_text = asset.read_text(encoding="utf-8")
        if AK_EQUIPPABLE_GUID not in asset_text:
            print("WARN: Equippable guid not AK clone in", asset)
        asset.write_text(asset_text.replace(AK_EQUIPPABLE_GUID, gun["guid"]), encoding="utf-8")
        print("wrote", dest.name, "and wired", gun["asset"])


if __name__ == "__main__":
    main()
