"""Rebuild RPG fire/empty/reload so they sound like a rocket launcher, not a gun."""
import os
import subprocess

FFMPEG = r"C:\Applications\ffmpeg-8.0.1-essentials_build\bin\ffmpeg.exe"
SRC = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Audio\_downloads\rpg"
LAUNCHES = os.path.join(SRC, "launches", "launches")
OUT = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Ripped\ExportedProject\Assets\AudioClip\rpg"
MIRROR = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Audio\Weapons\rpg"
TMP = os.path.join(SRC, "_tmp")
os.makedirs(TMP, exist_ok=True)
os.makedirs(OUT, exist_ok=True)
os.makedirs(MIRROR, exist_ok=True)


def ff(*args: str) -> None:
    subprocess.check_call(
        [FFMPEG, "-y", "-hide_banner", "-loglevel", "error", *args]
    )


def oneshot(src: str, dst: str, duration: float, fade: float = 0.25, gain: str = "1.0") -> None:
    st = max(0.05, duration - fade)
    af = (
        f"atrim=0:{duration},asetpts=PTS-STARTPTS,"
        f"afade=t=out:st={st}:d={fade},volume={gain},"
        f"loudnorm=I=-14:TP=-1.5:LRA=11"
    )
    ff("-i", src, "-ac", "1", "-ar", "44100", "-af", af, "-c:a", "libvorbis", "-q:a", "5", dst)


def mix2(a: str, b: str, dst: str, duration: float, vol_a="1.0", vol_b="0.85") -> None:
    # Trim each, then amix
    ta = os.path.join(TMP, "a.wav")
    tb = os.path.join(TMP, "b.wav")
    ff("-i", a, "-t", str(duration), "-ac", "1", "-ar", "44100", "-af", f"volume={vol_a}", ta)
    ff("-i", b, "-t", str(duration), "-ac", "1", "-ar", "44100", "-af", f"volume={vol_b}", tb)
    af = (
        f"amix=inputs=2:duration=longest:dropout_transition=0,"
        f"atrim=0:{duration},afade=t=out:st={duration-0.3}:d=0.3,"
        f"loudnorm=I=-14:TP=-1.5:LRA=11"
    )
    ff("-i", ta, "-i", tb, "-filter_complex", af, "-ac", "1", "-ar", "44100",
       "-c:a", "libvorbis", "-q:a", "5", dst)


def mix3(a: str, b: str, c: str, dst: str, duration: float) -> None:
    paths = []
    for i, src in enumerate((a, b, c)):
        p = os.path.join(TMP, f"m{i}.wav")
        ff("-i", src, "-t", str(duration), "-ac", "1", "-ar", "44100", p)
        paths.append(p)
    af = (
        f"[0:a][1:a][2:a]amix=inputs=3:duration=longest:dropout_transition=0,"
        f"atrim=0:{duration},afade=t=out:st={max(0.1, duration-0.35)}:d=0.35,"
        f"loudnorm=I=-14:TP=-1.5:LRA=11[a]"
    )
    ff("-i", paths[0], "-i", paths[1], "-i", paths[2],
       "-filter_complex", af, "-map", "[a]", "-ac", "1", "-ar", "44100",
       "-c:a", "libvorbis", "-q:a", "5", dst)


def concat(parts: list[str], dst: str) -> None:
    wavs = []
    for i, src in enumerate(parts):
        w = os.path.join(TMP, f"c{i}.wav")
        # keep each part short
        ff("-i", src, "-ac", "1", "-ar", "44100", w)
        wavs.append(w)
    n = len(wavs)
    inputs = "".join(f"[{i}:a]" for i in range(n))
    filt = f"{inputs}concat=n={n}:v=0:a=1,loudnorm=I=-16:TP=-1.5:LRA=11[a]"
    args = []
    for w in wavs:
        args += ["-i", w]
    ff(*args, "-filter_complex", filt, "-map", "[a]", "-ac", "1", "-ar", "44100",
       "-c:a", "libvorbis", "-q:a", "5", dst)


def save(name: str, builder) -> None:
    dst = os.path.join(OUT, name)
    builder(dst)
    # keep existing .meta (GUID); only replace audio bytes
    mirror = os.path.join(MIRROR, name)
    import shutil
    shutil.copy2(dst, mirror)
    print("wrote", name, os.path.getsize(dst), "bytes")


# --- FIRE: whoosh + thrust / missile ---
# 1) Real missile launch (CC0 mikeask)
save("rpg_fire1.ogg", lambda d: oneshot(os.path.join(SRC, "missile.wav"), d, 2.0, 0.4, "1.15"))

# 2) Sci-fi rocket launch (Michel Baradari CC-BY 3.0) mixed with tube whoosh for backblast
save(
    "rpg_fire2.ogg",
    lambda d: mix2(
        os.path.join(LAUNCHES, "rlaunch.wav"),
        os.path.join(SRC, "tube_whoosh7.ogg"),
        d,
        1.8,
        "1.1",
        "0.7",
    ),
)

# 3) Firework/rocket hiss + tube whoosh + short boom
save(
    "rpg_fire3.ogg",
    lambda d: mix3(
        os.path.join(SRC, "firework3.ogg"),
        os.path.join(SRC, "tube_whoosh8.ogg"),
        os.path.join(LAUNCHES, "flaunch.wav"),
        d,
        1.7,
    ),
)

# --- EMPTY: hollow metal tube thunk (not a gun click) ---
# Take a sharp metal hit / pipe moment, keep it short
empty_a = os.path.join(TMP, "empty_hit.wav")
ff(
    "-i", os.path.join(SRC, "metal_hit1.ogg"),
    "-ss", "0.05", "-t", "0.35",
    "-ac", "1", "-ar", "44100",
    "-af", "highpass=f=200,volume=1.4",
    empty_a,
)
empty_b = os.path.join(TMP, "empty_tube.wav")
ff(
    "-i", os.path.join(SRC, "tube_whoosh7.ogg"),
    "-t", "0.25",
    "-ac", "1", "-ar", "44100",
    "-af", "volume=0.6",
    empty_b,
)
save(
    "rpg_empty.ogg",
    lambda d: mix2(empty_a, empty_b, d, 0.45, "1.2", "0.5"),
)

# --- RELOAD: load rocket into tube ---
# 1) open/handle tube (metal can lid slice)
open_part = os.path.join(TMP, "reload_open.wav")
ff("-i", os.path.join(SRC, "metal_can.ogg"), "-ss", "0.2", "-t", "0.55",
   "-ac", "1", "-ar", "44100", "-af", "volume=0.9", open_part)
# 2) slide rocket in (plastic/tube scrape)
slide_part = os.path.join(TMP, "reload_slide.wav")
ff("-i", os.path.join(SRC, "plastic_tube.ogg"), "-ss", "0.1", "-t", "0.7",
   "-ac", "1", "-ar", "44100", "-af", "volume=1.1", slide_part)
# 3) seat / whoosh lock
seat_part = os.path.join(TMP, "reload_seat.wav")
ff("-i", os.path.join(SRC, "tube_whoosh8.ogg"), "-t", "0.5",
   "-ac", "1", "-ar", "44100", "-af", "volume=1.0", seat_part)
# 4) final metallic clunk
clunk_part = os.path.join(TMP, "reload_clunk.wav")
ff("-i", os.path.join(SRC, "pipe_drop.ogg"), "-ss", "0.15", "-t", "0.4",
   "-ac", "1", "-ar", "44100", "-af", "volume=1.0", clunk_part)

save(
    "rpg_reload.ogg",
    lambda d: concat([open_part, slide_part, seat_part, clunk_part], d),
)

print("RPG audio bank rebuilt (fire x3, empty, reload). Prefab GUIDs unchanged.")
