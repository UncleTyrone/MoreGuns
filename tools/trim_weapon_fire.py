"""Re-trim weapon fire OGGs to short one-shots (keep paths/GUIDs)."""
import os
import subprocess

FFMPEG = r"C:\Applications\ffmpeg-8.0.1-essentials_build\bin\ffmpeg.exe"
PREPARED = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Audio\_downloads\extracted\Prepared_SFX_Library\Prepared SFX Library"
DL = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Audio\_downloads"
OUT = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Ripped\ExportedProject\Assets\AudioClip"


def convert(src: str, dst: str, duration: float = 1.2) -> None:
    fade_start = max(0.05, duration - 0.35)
    af = f"atrim=0:{duration},afade=t=out:st={fade_start}:d=0.35,loudnorm=I=-14:TP=-1.5:LRA=11"
    subprocess.check_call(
        [
            FFMPEG,
            "-y",
            "-hide_banner",
            "-loglevel",
            "error",
            "-i",
            src,
            "-ac",
            "1",
            "-ar",
            "44100",
            "-af",
            af,
            "-c:a",
            "libvorbis",
            "-q:a",
            "5",
            dst,
        ]
    )
    print("trimmed", dst)


def p(*parts):
    return os.path.join(PREPARED, *parts)


jobs = [
    ("sniper/sniper_fire1.ogg", p("Mosin Nagant", "M_21P.wav"), 1.3),
    ("sniper/sniper_fire2.ogg", p("Mosin Nagant", "M_26P.wav"), 1.3),
    ("sniper/sniper_fire3.ogg", p("Tikka", "W_24P.wav"), 1.3),
    ("smg/smg_fire1.ogg", p("PPSh", "P_16P.wav"), 0.55),
    ("smg/smg_fire2.ogg", p("PPSh", "P_18P.wav"), 0.55),
    ("smg/smg_fire3.ogg", p("PPSh", "P_22P.wav"), 0.55),
    ("rpg/rpg_fire1.ogg", p("Mossberg", "N_26P.wav"), 1.5),
    ("rpg/rpg_fire2.ogg", p("Model 12", "K_17P.wav"), 1.5),
    ("rpg/rpg_fire3.ogg", os.path.join(DL, "cannon.ogg"), 1.8),
]

for rel, src, dur in jobs:
    convert(src, os.path.join(OUT, rel.replace("/", os.sep)), dur)
