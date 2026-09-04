import os
import ufbx


def export_scene_objs(fbx_path, dest_dir, prefix):
    scene = ufbx.load_file(fbx_path)
    os.makedirs(dest_dir, exist_ok=True)
    written = []
    for i, mesh in enumerate(scene.meshes):
        name = mesh.name or f"mesh_{i}"
        safe = "".join(c if c.isalnum() or c in "-_" else "_" for c in name)
        path = os.path.join(dest_dir, f"{prefix}_{safe}.obj")
        verts = []
        faces = []
        # triangulated indices
        for tri in mesh.faces:
            idxs = []
            for vi in range(tri.index_begin, tri.index_begin + tri.num_indices):
                pi = mesh.vertex_position.indices[vi]
                p = mesh.vertex_position.values[pi]
                verts.append((p.x, p.y, p.z))
                idxs.append(len(verts))
            if len(idxs) >= 3:
                faces.append(tuple(idxs))
        with open(path, "w", encoding="utf-8") as f:
            f.write(f"o {safe}\n")
            for x, y, z in verts:
                f.write(f"v {x:.6f} {y:.6f} {z:.6f}\n")
            for face in faces:
                f.write("f " + " ".join(str(n) for n in face) + "\n")
        written.append((path, len(verts), len(faces), name))
        print(f"wrote {path} verts={len(verts)} faces={len(faces)} name={name!r}")
    print("nodes:")
    for node in scene.nodes:
        print(" ", node.name, "mesh" if node.mesh else "")
    return written


if __name__ == "__main__":
    export_scene_objs(
        r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\rpg\RPG7.fbx",
        r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\rpg\_from_fbx",
        "rpg",
    )
    export_scene_objs(
        r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\_downloads\extracted\Sniper_1A\Sniper_1A\Sniper_1A\Sniper_1A.fbx",
        r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\sniper\_from_fbx",
        "sniperA",
    )
