"""Wire sniper/smg/rpg item defs, equippable refs, Other anims, and Play strings."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(
    r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Ripped\ExportedProject\Assets\resources"
)

AK_AVATAR = "guid: a4c6fc57eb57c794ba87017515d457e0"
AK_MAG_ASSET = "guid: 2bb4ab854cc2cd143b6417f5c9a02afd"
AK_TRASH = "guid: de4110e95363b234094816e9697e181e"
AK_GUN_EQUIPPABLE = "guid: 11acfd0ef03f9ee4c8b375790fdf6fd3"

IDLE = "guid: f26caf8c33bfb9a4394e0f7f25f6ae23"
AIM = "guid: c6306f5b0571e7648aef5b85e2c9aeac"
FIRE = "guid: 4cd9c30b2d87f524c9888a233a39279c"
RELOAD = "guid: 7869b3c069f84a8479bbbd8e7e3feb0b"

ANIM_BLOCK = f"""  m_Animation: {{fileID: 7400000, {IDLE}, type: 2}}
  m_Animations:
  - {{fileID: 7400000, {IDLE}, type: 2}}
  - {{fileID: 7400000, {AIM}, type: 2}}
  - {{fileID: 7400000, {FIRE}, type: 2}}
  - {{fileID: 7400000, {RELOAD}, type: 2}}
"""

GUNS = [
    {
        "id": "sniper",
        "title": "Sniper",
        "desc": "Bolt-action sniper rifle.",
        "mag_title": "Sniper Magazine",
        "mag_desc": "Magazine for the sniper rifle.",
        "equippable": "95fb5a4eaad508c4b8ee84e0009b3059",
        "avatar": "639e7e6906e433343ab6ad53e4cbd7f9",
        "mag_asset": "dbc62ae1e8680434b96ff99170f294bd",
        "trash": "fcef9afa8682393419536c74591c1e58",
        "folder": "sniper",
        "prefab": "Sniper_Equippable.prefab",
        "avatar_prefab": "Sniper.prefab",
        "gun_asset": "sniper.asset",
        "mag_file": "magazine/sniper_magazine.asset",
    },
    {
        "id": "smg",
        "title": "SMG",
        "desc": "Compact submachine gun.",
        "mag_title": "SMG Magazine",
        "mag_desc": "Magazine for the SMG.",
        "equippable": "bec9faff3663f8a4aa71e4ef9eaec378",
        "avatar": "7c93320f58a9b474f8e50e8837e82f1d",
        "mag_asset": "ea315c71844575e4780dc5f94d45d622",
        "trash": "cc6b645547722144bab54c1e2198c571",
        "folder": "smg",
        "prefab": "SMG_Equippable.prefab",
        "avatar_prefab": "SMG.prefab",
        "gun_asset": "smg.asset",
        "mag_file": "magazine/smg_magazine.asset",
    },
    {
        "id": "rpg",
        "title": "RPG",
        "desc": "Shoulder-fired rocket launcher.",
        "mag_title": "RPG Rocket",
        "mag_desc": "Rocket for the RPG.",
        "equippable": "39cddab4f255520499788ba7d5bb228e",
        "avatar": "6e6249a23df24eb4a8ee5efa33e82b26",
        "mag_asset": "be310a4f2d5022c42a45bd64c9b3c32e",
        "trash": "87b740ee3549bf648b103287857636bc",
        "folder": "rpg",
        "prefab": "RPG_Equippable.prefab",
        "avatar_prefab": "RPG.prefab",
        "gun_asset": "rpg.asset",
        "mag_file": "magazine/rpg_magazine.asset",
        "rpg": True,
    },
]


def replace_anim_lists(text: str) -> str:
    # Replace first-person / avatar Animation clip lists that currently hold Other_Fire
    # (and maybe Other Reload) with the full Other set.
    import re

    pattern = re.compile(
        r"  m_Animation: \{fileID: 7400000, guid: 4cd9c30b2d87f524c9888a233a39279c, type: 2\}\n"
        r"  m_Animations:\n"
        r"(?:  - \{fileID: 7400000, guid: [a-f0-9]+, type: 2\}\n)+",
        re.MULTILINE,
    )
    new, n = pattern.subn(ANIM_BLOCK, text, count=1)
    if n == 0:
        raise SystemExit("Animation clip list not found")
    return new


def patch_gun_asset(path: Path, gun: dict) -> None:
    text = path.read_text(encoding="utf-8")
    text = text.replace("  Name: AK47\n", f"  Name: {gun['title']}\n", 1)
    text = text.replace(
        "  Description: AK47 assault rifle A true American classic.\n",
        f"  Description: {gun['desc']}\n",
        1,
    )
    text = text.replace("  ID: ak47\n", f"  ID: {gun['id']}\n", 1)
    text = text.replace(AK_GUN_EQUIPPABLE, f"guid: {gun['equippable']}")
    path.write_text(text, encoding="utf-8")


def patch_mag_asset(path: Path, gun: dict) -> None:
    text = path.read_text(encoding="utf-8")
    text = text.replace("  Name: AK47 Magazine\n", f"  Name: {gun['mag_title']}\n", 1)
    text = text.replace(
        "  Description: 30-round magazine for the ak47 assault rifle.\n",
        f"  Description: {gun['mag_desc']}\n",
        1,
    )
    text = text.replace("  ID: ak47mag\n", f"  ID: {gun['id']}mag\n", 1)
    if gun.get("rpg"):
        text = text.replace("  DefaultValue: 30\n", "  DefaultValue: 1\n", 1)
    path.write_text(text, encoding="utf-8")


def patch_equippable(path: Path, gun: dict) -> None:
    text = path.read_text(encoding="utf-8")
    text = text.replace(AK_AVATAR, f"guid: {gun['avatar']}")
    text = text.replace(AK_MAG_ASSET, f"guid: {gun['mag_asset']}")
    text = text.replace(AK_TRASH, f"guid: {gun['trash']}")
    text = text.replace("m_StringArgument: AK47 Fire", "m_StringArgument: Other_Fire")
    text = text.replace("m_StringArgument: AK47 Reload", "m_StringArgument: Other Reload")
    text = replace_anim_lists(text)
    if gun.get("rpg"):
        text = text.replace("  MagazineSize: 30\n", "  MagazineSize: 1\n", 1)
    path.write_text(text, encoding="utf-8")


def patch_avatar(path: Path) -> None:
    text = path.read_text(encoding="utf-8")
    text = replace_anim_lists(text)
    path.write_text(text, encoding="utf-8")


def main() -> None:
    for gun in GUNS:
        weapons = ROOT / "weapons" / gun["folder"]
        patch_gun_asset(weapons / gun["gun_asset"], gun)
        patch_mag_asset(weapons / gun["mag_file"], gun)
        patch_equippable(weapons / gun["prefab"], gun)
        patch_avatar(ROOT / "avatar" / "equippables" / gun["avatar_prefab"])
        print("wired", gun["id"])


if __name__ == "__main__":
    main()
