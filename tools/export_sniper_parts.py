import os
import ufbx

def export_node_mesh(node, path):
    mesh = node.mesh
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        f.write(f"o {node.name.replace(' ', '_')}\n")
        for i in range(mesh.num_vertices):
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
    print("wrote", path, "verts", mesh.num_vertices)


dest = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\sniper"
scene = ufbx.load_file(
    r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\_downloads\extracted\Sniper_1A\Sniper_1A\Sniper_1A\Sniper_1A.fbx"
)
for node in scene.nodes:
    if not node.mesh:
        continue
    name = node.name.lower()
    if name == "magazine":
        export_node_mesh(node, os.path.join(dest, "sniper_mag_authored.obj"))
    elif name == "body":
        export_node_mesh(node, os.path.join(dest, "sniper_body_authored.obj"))
