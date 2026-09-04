"""Dump GameObject / Prefab hierarchies from the MoreGuns bundle."""
import json
import os

import UnityPy
from UnityPy.enums import ClassIDType

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
BUNDLE = os.path.join(ROOT, "Resources", "voidanesguns")
OUT = os.path.join(ROOT, "UnityAuthoring", "Extracted", "prefab_layout.json")


def deref(ptr):
    if ptr is None:
        return None
    try:
        return ptr.read()
    except Exception:
        return None


def go_name(obj):
    data = deref(obj) if hasattr(obj, "read") else obj
    if data is None:
        return "?"
    return getattr(data, "name", None) or getattr(data, "m_Name", None) or "?"


def dump_gameobject(data, depth=0):
    name = getattr(data, "name", "") or getattr(data, "m_Name", "")
    node = {"name": name, "components": [], "children": []}
    for cptr in getattr(data, "m_Components", []) or []:
        comp = deref(cptr)
        if comp is None:
            node["components"].append({"type": "unreadable"})
            continue
        ctype = type(comp).__name__
        info = {"type": ctype}
        if hasattr(comp, "m_Script"):
            script = deref(comp.m_Script)
            if script is not None:
                info["script"] = getattr(script, "name", None) or getattr(script, "m_Name", None)
        if ctype == "MeshFilter":
            mesh = deref(getattr(comp, "m_Mesh", None))
            if mesh is not None:
                info["mesh"] = getattr(mesh, "name", None) or getattr(mesh, "m_Name", None)
        node["components"].append(info)

    transform = None
    for cptr in getattr(data, "m_Components", []) or []:
        comp = deref(cptr)
        if comp is not None and type(comp).__name__ == "Transform":
            transform = comp
            break
    if transform is not None:
        for child in getattr(transform, "m_Children", []) or []:
            child_t = deref(child)
            if child_t is None:
                continue
            child_go = deref(getattr(child_t, "m_GameObject", None))
            if child_go is not None:
                node["children"].append(dump_gameobject(child_go, depth + 1))
    return node


def main():
    env = UnityPy.load(BUNDLE)
    trees = {}
    for obj in env.objects:
        if obj.type != ClassIDType.GameObject:
            continue
        try:
            data = obj.read()
        except Exception:
            continue
        name = getattr(data, "name", "") or getattr(data, "m_Name", "")
        if name in (
            "AK47_Equippable",
            "AK47_Magazine_Trash",
            "AK47_Magazine_AvatarEquippable",
            "AK47",
            "MiniGun_Equippable",
        ):
            trees[name] = dump_gameobject(data)

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as f:
        json.dump(trees, f, indent=2)
    print(f"Wrote {OUT}")
    for name, tree in trees.items():
        print(name, "children=", len(tree.get("children", [])), "comps=", [c.get("type") for c in tree.get("components", [])])


if __name__ == "__main__":
    main()
