import os
import ufbx


def export_node_mesh(node, path):
    mesh = node.mesh
    if mesh is None:
        print("no mesh on", node.name)
        return
    # Use triangulated faces via mesh.num_triangles / face_indices
    n_verts = mesh.num_vertices
    print(node.name, "num_vertices", n_verts, "num_faces", mesh.num_faces, "num_triangles", mesh.num_triangles)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        f.write(f"o {node.name.replace(' ', '_')}\n")
        for i in range(n_verts):
            p = mesh.vertices[i]
            f.write(f"v {p.x:.6f} {p.y:.6f} {p.z:.6f}\n")
        for fi in range(mesh.num_faces):
            face = mesh.faces[fi]
            idxs = []
            for k in range(face.num_indices):
                vi = mesh.vertex_indices[face.index_begin + k]
                idxs.append(vi + 1)
            if len(idxs) >= 3:
                f.write("f " + " ".join(str(i) for i in idxs) + "\n")
    print("wrote", path)


scene = ufbx.load_file(r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\rpg\RPG7.fbx")
dest = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\rpg"
for node in scene.nodes:
    if not node.mesh:
        continue
    if "rocket" in node.name.lower():
        export_node_mesh(node, os.path.join(dest, "rpg_rocket.obj"))
    elif node.name == "RPG7":
        export_node_mesh(node, os.path.join(dest, "rpg_body.obj"))

scene2 = ufbx.load_file(
    r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\_downloads\extracted\Sniper_1A\Sniper_1A\Sniper_1A\Sniper_1A.fbx"
)
print("--- sniper A nodes ---")
for node in scene2.nodes:
    print(repr(node.name), "mesh" if node.mesh else "")
