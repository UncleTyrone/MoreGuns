"""Build per-weapon OGG banks and patch equippable prefab audio GUIDs."""
from __future__ import annotations

import hashlib
import os
import shutil
import subprocess
import uuid

FFMPEG = r"C:\Applications\ffmpeg-8.0.1-essentials_build\bin\ffmpeg.exe"
ROOT = r"D:\Schedule I\Schedule I\MoreGuns"
PREPARED = os.path.join(
    ROOT,
    r"UnityAuthoring\Assets\Audio\_downloads\extracted\Prepared_SFX_Library\Prepared SFX Library",
)
DL = os.path.join(ROOT, r"UnityAuthoring\Assets\Audio\_downloads")
OUT = os.path.join(
    ROOT, r"UnityAuthoring\Assets\Ripped\ExportedProject\Assets\AudioClip"
)
WEAPONS_DIR = os.path.join(
    ROOT, r"UnityAuthoring\Assets\Ripped\ExportedProject\Assets\resources\weapons"
)

# Stable GUIDs derived from relative path so re-runs stay consistent.
def guid_for(rel: str) -> str:
    h = hashlib.md5(("moreguns-audio:" + rel.replace("\\", "/")).encode()).hexdigest()
    return h


def run_ffmpeg(args: list[str]) -> None:
    cmd = [FFMPEG, "-y", "-hide_banner", "-loglevel", "error"] + args
    subprocess.check_call(cmd)


def wav_to_ogg(src: str, dst: str, loudnorm: bool = True) -> None:
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    filt = "loudnorm=I=-16:TP=-1.5:LRA=11" if loudnorm else "anull"
    run_ffmpeg(
        [
            "-i",
            src,
            "-ac",
            "1",
            "-ar",
            "44100",
            "-af",
            filt,
            "-c:a",
            "libvorbis",
            "-q:a",
            "5",
            dst,
        ]
    )


def concat_ogg(sources: list[str], dst: str) -> None:
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    # Re-encode concat for reliability
    args = []
    for s in sources:
        args += ["-i", s]
    n = len(sources)
    inputs = "".join(f"[{i}:a]" for i in range(n))
    filt = f"{inputs}concat=n={n}:v=0:a=1[a]"
    run_ffmpeg(
        args
        + [
            "-filter_complex",
            filt,
            "-map",
            "[a]",
            "-ac",
            "1",
            "-ar",
            "44100",
            "-c:a",
            "libvorbis",
            "-q:a",
            "5",
            dst,
        ]
    )


AUDIO_META = """fileFormatVersion: 2
guid: {guid}
AudioImporter:
  serializedVersion: 7
  externalObjects: {{}}
  defaultSettings:
    serializedVersion: 2
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 1
    quality: 0.7
    conversionMode: 0
    preloadAudioData: 1
  platformSettingOverrides: {{}}
  forceToMono: 1
  normalize: 1
  loadInBackground: 0
  ambisonic: 0
  3D: 1
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def write_meta(ogg_path: str) -> str:
    rel = os.path.relpath(ogg_path, OUT).replace("\\", "/")
    g = guid_for(rel)
    meta = ogg_path + ".meta"
    with open(meta, "w", encoding="utf-8", newline="\n") as f:
        f.write(AUDIO_META.format(guid=g))
    return g


def p(*parts: str) -> str:
    return os.path.join(PREPARED, *parts)


BANKS = {
    "sniper": {
        "fire": [
            p("Mosin Nagant", "M_21P.wav"),
            p("Mosin Nagant", "M_26P.wav"),
            p("Tikka", "W_24P.wav"),
        ],
        "empty": os.path.join(DL, "dryfire.ogg"),
        "reload_parts": [
            os.path.join(DL, "gun_cock.ogg"),
            os.path.join(DL, "gun_loading.ogg"),
            os.path.join(DL, "mag_insert.ogg"),
        ],
    },
    "smg": {
        "fire": [
            p("PPSh", "P_16P.wav"),
            p("PPSh", "P_18P.wav"),
            p("PPSh", "P_22P.wav"),
        ],
        "empty": os.path.join(DL, "dryfire.ogg"),
        "reload_parts": [
            os.path.join(DL, "gun_loading.ogg"),
            os.path.join(DL, "mag_insert.ogg"),
            os.path.join(DL, "gun_cock.ogg"),
        ],
    },
    "rpg": {
        "fire": [
            p("Mossberg", "N_26P.wav"),
            p("Model 12", "K_17P.wav"),
            os.path.join(DL, "explosion2.ogg"),
        ],
        "empty": os.path.join(DL, "dryfire.ogg"),
        "reload_parts": [
            os.path.join(DL, "gun_loading.ogg"),
            os.path.join(DL, "mag_insert.ogg"),
        ],
    },
}


def build_bank(weapon: str) -> dict[str, str]:
    """Returns map role -> guid."""
    cfg = BANKS[weapon]
    dest_dir = os.path.join(OUT, weapon)
    os.makedirs(dest_dir, exist_ok=True)
    guids: dict[str, str] = {}

    for i, src in enumerate(cfg["fire"], start=1):
        dst = os.path.join(dest_dir, f"{weapon}_fire{i}.ogg")
        if src.lower().endswith(".ogg"):
            # normalize length/volume
            run_ffmpeg(
                [
                    "-i",
                    src,
                    "-t",
                    "2.5",
                    "-ac",
                    "1",
                    "-ar",
                    "44100",
                    "-af",
                    "loudnorm=I=-16:TP=-1.5:LRA=11",
                    "-c:a",
                    "libvorbis",
                    "-q:a",
                    "5",
                    dst,
                ]
            )
        else:
            wav_to_ogg(src, dst)
        guids[f"fire{i}"] = write_meta(dst)

    empty_dst = os.path.join(dest_dir, f"{weapon}_empty.ogg")
    run_ffmpeg(
        [
            "-i",
            cfg["empty"],
            "-ac",
            "1",
            "-ar",
            "44100",
            "-af",
            "loudnorm=I=-18:TP=-1.5:LRA=11",
            "-c:a",
            "libvorbis",
            "-q:a",
            "5",
            empty_dst,
        ]
    )
    guids["empty"] = write_meta(empty_dst)

    # Build reload: convert parts to temp oggs then concat
    tmp_parts = []
    for i, part in enumerate(cfg["reload_parts"]):
        tmp = os.path.join(dest_dir, f"_tmp_reload_{i}.ogg")
        if part.lower().endswith(".wav"):
            wav_to_ogg(part, tmp, loudnorm=False)
        else:
            run_ffmpeg(
                [
                    "-i",
                    part,
                    "-ac",
                    "1",
                    "-ar",
                    "44100",
                    "-c:a",
                    "libvorbis",
                    "-q:a",
                    "5",
                    tmp,
                ]
            )
        tmp_parts.append(tmp)
    reload_dst = os.path.join(dest_dir, f"{weapon}_reload.ogg")
    concat_ogg(tmp_parts, reload_dst)
    for t in tmp_parts:
        os.remove(t)
    guids["reload"] = write_meta(reload_dst)
    print(weapon, guids)
    return guids


def patch_prefab(weapon: str, guids: dict[str, str]) -> None:
    path = os.path.join(WEAPONS_DIR, weapon, f"{weapon}_equippable.prefab")
    text = open(path, encoding="utf-8").read()

    # Replace Fire Sound Clips list: find "m_Name: Fire Sound" then later Clips block belonging to that controller.
    # Safer: replace the shared AK fire/empty/reload GUIDs that still appear on these clones
    # with weapon-specific ones by rewriting the three Clips/m_audioClip sections near sound names.

    # AK defaults still on clones:
    ak_fire = "aff58ead1f7828a46832f8b26d22eac1"
    ak_empty = "39a11e6a839282b43ab7c4e11cad48d2"
    ak_reload = "e14c13b46c336c5468d12500dc930683"

    # Fire Sound currently has a single Clips entry — expand to 3.
    old_fire_clips = f"""  Clips:
  - {{fileID: 8300000, guid: {ak_fire}, type: 3}}"""
    new_fire_clips = f"""  Clips:
  - {{fileID: 8300000, guid: {guids['fire1']}, type: 3}}
  - {{fileID: 8300000, guid: {guids['fire2']}, type: 3}}
  - {{fileID: 8300000, guid: {guids['fire3']}, type: 3}}"""

    # Only replace first occurrence after Fire Sound — Fire Sound's Clips come before Empty's.
    # Structure in file: Fire Sound controller Clips, then Empty Clips, then Reload m_audioClip.
    if old_fire_clips not in text:
        # already patched or different — try replacing any single fire guid list near fire1
        raise SystemExit(f"{weapon}: expected AK fire Clips block not found")

    text = text.replace(old_fire_clips, new_fire_clips, 1)
    text = text.replace(
        f"guid: {ak_empty}, type: 3",
        f"guid: {guids['empty']}, type: 3",
        1,
    )
    text = text.replace(
        f"guid: {ak_reload}, type: 3",
        f"guid: {guids['reload']}, type: 3",
        1,
    )

    open(path, "w", encoding="utf-8", newline="\n").write(text)
    print("patched", path)


def main():
    credits = []
    for weapon in ("sniper", "smg", "rpg"):
        guids = build_bank(weapon)
        patch_prefab(weapon, guids)
        credits.append(weapon)
    print("done", credits)


if __name__ == "__main__":
    main()
