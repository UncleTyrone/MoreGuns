"""Create distinct shop icons for sniper / smg / rpg from gun textures."""
import shutil
import uuid
from pathlib import Path

from PIL import Image

ROOT = Path(r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Ripped\ExportedProject\Assets")
TEX = ROOT / "Texture2D"
SPR = ROOT / "Sprite"
TEMPLATE_TEX_META = (TEX / "AK47__Icon.png.meta").read_text(encoding="utf-8")
TEMPLATE_SPR = (SPR / "AK47__Icon.asset").read_text(encoding="utf-8")
TEMPLATE_SPR_META = (SPR / "AK47__Icon.asset.meta").read_text(encoding="utf-8")

SOURCES = {
    "Sniper": ROOT / "Models" / "sniper" / "Sniper_parts.png",
    "SMG": TEX / "MachineGun_Diffuse.png",
    "RPG": ROOT / "Models" / "rpg" / "RPG7.png",
}

# Distinct accent borders so icons are obviously different even if textures are busy
ACCENTS = {
    "Sniper": (40, 120, 200, 255),
    "SMG": (200, 140, 40, 255),
    "RPG": (180, 50, 50, 255),
}


def new_guid() -> str:
    return uuid.uuid4().hex


def make_icon(src: Path, out: Path, accent, size=512):
    img = Image.open(src).convert("RGBA")
    # Crop non-transparent / non-near-black content if possible
    bbox = img.getbbox()
    if bbox:
        img = img.crop(bbox)
    img.thumbnail((size - 48, size - 48), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    # accent frame
    frame = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    for x in range(size):
        for y in range(size):
            edge = x < 8 or y < 8 or x >= size - 8 or y >= size - 8
            inner = 8 <= x < size - 8 and 8 <= y < size - 8 and (x < 14 or y < 14 or x >= size - 14 or y >= size - 14)
            if edge:
                frame.putpixel((x, y), accent)
            elif inner:
                frame.putpixel((x, y), (accent[0], accent[1], accent[2], 180))
    canvas.alpha_composite(frame)
    ox = (size - img.width) // 2
    oy = (size - img.height) // 2
    canvas.alpha_composite(img, (ox, oy))
    canvas.save(out, "PNG")
    print("wrote", out, canvas.size)


def write_tex_meta(path: Path, guid: str):
    text = TEMPLATE_TEX_META
    # replace first guid line
    lines = text.splitlines()
    for i, line in enumerate(lines):
        if line.startswith("guid:"):
            lines[i] = f"guid: {guid}"
            break
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def write_sprite(name: str, sprite_guid: str, tex_guid: str):
    asset = TEMPLATE_SPR.replace("m_Name: AK47__Icon", f"m_Name: {name}")
    # replace texture guid in sprite asset
    import re

    asset = re.sub(
        r"texture: \{fileID: 2800000, guid: [a-f0-9]+, type: 3\}",
        f"texture: {{fileID: 2800000, guid: {tex_guid}, type: 3}}",
        asset,
    )
    (SPR / f"{name}.asset").write_text(asset, encoding="utf-8")
    meta = TEMPLATE_SPR_META
    lines = meta.splitlines()
    for i, line in enumerate(lines):
        if line.startswith("guid:"):
            lines[i] = f"guid: {sprite_guid}"
            break
    (SPR / f"{name}.asset.meta").write_text("\n".join(lines) + "\n", encoding="utf-8")


def patch_item_icon(asset_path: Path, sprite_guid: str):
    text = asset_path.read_text(encoding="utf-8")
    import re

    new_text, n = re.subn(
        r"Icon: \{fileID: 21300000, guid: [a-f0-9]+, type: 2\}",
        f"Icon: {{fileID: 21300000, guid: {sprite_guid}, type: 2}}",
        text,
        count=1,
    )
    if n != 1:
        raise SystemExit(f"Icon field not patched in {asset_path}")
    asset_path.write_text(new_text, encoding="utf-8")
    print("patched", asset_path.name, "->", sprite_guid)


def main():
    mapping = {}  # gun key -> (gun_sprite_guid, mag_sprite_guid)
    for label, src in SOURCES.items():
        if not src.exists():
            raise SystemExit(f"missing source {src}")
        gun_name = f"{label}__Icon"
        mag_name = f"{label}__Magazine_Icon"
        gun_tex = TEX / f"{gun_name}.png"
        mag_tex = TEX / f"{mag_name}.png"
        make_icon(src, gun_tex, ACCENTS[label])
        # mag: slightly smaller crop / same with different accent thickness feel
        make_icon(src, mag_tex, tuple(min(255, c + 40) if i < 3 else c for i, c in enumerate(ACCENTS[label])))

        gun_tex_guid = new_guid()
        mag_tex_guid = new_guid()
        gun_spr_guid = new_guid()
        mag_spr_guid = new_guid()
        write_tex_meta(Path(str(gun_tex) + ".meta"), gun_tex_guid)
        write_tex_meta(Path(str(mag_tex) + ".meta"), mag_tex_guid)
        write_sprite(gun_name, gun_spr_guid, gun_tex_guid)
        write_sprite(mag_name, mag_spr_guid, mag_tex_guid)
        mapping[label.lower()] = (gun_spr_guid, mag_spr_guid)

    weapons = ROOT / "resources" / "weapons"
    patch_item_icon(weapons / "sniper" / "sniper.asset", mapping["sniper"][0])
    patch_item_icon(weapons / "sniper" / "magazine" / "sniper_magazine.asset", mapping["sniper"][1])
    patch_item_icon(weapons / "smg" / "smg.asset", mapping["smg"][0])
    patch_item_icon(weapons / "smg" / "magazine" / "smg_magazine.asset", mapping["smg"][1])
    patch_item_icon(weapons / "rpg" / "rpg.asset", mapping["rpg"][0])
    patch_item_icon(weapons / "rpg" / "magazine" / "rpg_magazine.asset", mapping["rpg"][1])
    print("done", mapping)


if __name__ == "__main__":
    main()
