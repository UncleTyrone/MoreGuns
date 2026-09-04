"""List and extract assets from Resources/voidanesguns."""
import json
import os
import sys

import UnityPy
from UnityPy.enums import ClassIDType

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
BUNDLE = os.path.join(ROOT, "Resources", "voidanesguns")
OUT = os.path.join(ROOT, "UnityAuthoring", "Extracted")


def main():
    env = UnityPy.load(BUNDLE)
    os.makedirs(OUT, exist_ok=True)
    index = []
    for obj in env.objects:
        try:
            data = obj.read()
        except Exception as ex:
            index.append({"type": str(obj.type), "error": str(ex)})
            continue

        name = getattr(data, "name", "") or getattr(data, "m_Name", "") or ""
        path_id = obj.path_id
        typ = str(obj.type)
        entry = {"type": typ, "name": name, "path_id": path_id}

        if obj.type == ClassIDType.GameObject:
            try:
                comps = []
                for c in data.m_Components:
                    try:
                        comps.append(str(c.type) if hasattr(c, "type") else str(c))
                    except Exception:
                        comps.append("?")
                entry["components"] = comps
            except Exception:
                pass

        index.append(entry)

        safe = "".join(ch if ch.isalnum() or ch in "-_." else "_" for ch in (name or f"unnamed_{path_id}"))
        if obj.type in (ClassIDType.Texture2D, ClassIDType.Sprite):
            try:
                img = data.image
                dest = os.path.join(OUT, "textures", f"{safe}_{path_id}.png")
                os.makedirs(os.path.dirname(dest), exist_ok=True)
                img.save(dest)
            except Exception as ex:
                entry["export_error"] = str(ex)
        elif obj.type == ClassIDType.TextAsset:
            try:
                dest = os.path.join(OUT, "text", f"{safe}_{path_id}.txt")
                os.makedirs(os.path.dirname(dest), exist_ok=True)
                raw = data.script if hasattr(data, "script") else data.m_Script
                with open(dest, "wb") as f:
                    f.write(raw if isinstance(raw, bytes) else str(raw).encode("utf-8", "replace"))
            except Exception as ex:
                entry["export_error"] = str(ex)
        elif obj.type == ClassIDType.Mesh:
            try:
                dest = os.path.join(OUT, "meshes", f"{safe}_{path_id}.obj")
                os.makedirs(os.path.dirname(dest), exist_ok=True)
                with open(dest, "wt", encoding="utf-8") as f:
                    f.write(data.export())
            except Exception as ex:
                entry["export_error"] = str(ex)
        elif obj.type == ClassIDType.AudioClip:
            try:
                dest_dir = os.path.join(OUT, "audio")
                os.makedirs(dest_dir, exist_ok=True)
                samples = data.samples
                if isinstance(samples, dict):
                    for fname, blob in samples.items():
                        with open(os.path.join(dest_dir, fname), "wb") as f:
                            f.write(blob)
                elif samples:
                    with open(os.path.join(dest_dir, f"{safe}_{path_id}.wav"), "wb") as f:
                        f.write(samples if isinstance(samples, bytes) else bytes(samples))
            except Exception as ex:
                entry["export_error"] = str(ex)

    dest = os.path.join(OUT, "index.json")
    with open(dest, "w", encoding="utf-8") as f:
        json.dump(index, f, indent=2)
    print(f"Wrote {len(index)} objects to {dest}")
    names = sorted({e.get("name") for e in index if e.get("name")})
    print("Named assets:")
    for n in names:
        print(f"  {n}")


if __name__ == "__main__":
    sys.exit(main())
