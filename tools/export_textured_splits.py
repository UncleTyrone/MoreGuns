"""Export RPG / sniper FBX meshes to OBJ with UVs + MTL pointing at PNG."""
import os
import ufbx


def attr_vec2(attr, corner_index):
    return attr.values[attr.indices[corner_index]]


def attr_vec3(attr, corner_index):
    return attr.values[attr.indices[corner_index]]


def export_node(node, obj_path, mtl_file, mtl_name):
    mesh = node.mesh
    has_uv = mesh.vertex_uv.exists
    has_n = mesh.vertex_normal.exists

    os.makedirs(os.path.dirname(obj_path), exist_ok=True)
    vt_list = []
    vn_list = []
    face_lines = []

    for fi in range(mesh.num_faces):
        face = mesh.faces[fi]
        bits = []
        for k in range(face.num_indices):
            idx = face.index_begin + k
            vi = mesh.vertex_indices[idx] + 1
            vt_i = ""
            vn_i = ""
            if has_uv:
                uv = attr_vec2(mesh.vertex_uv, idx)
                vt_list.append((float(uv.x), float(uv.y)))
                vt_i = str(len(vt_list))
            if has_n:
                n = attr_vec3(mesh.vertex_normal, idx)
                vn_list.append((float(n.x), float(n.y), float(n.z)))
                vn_i = str(len(vn_list))
            if has_uv and has_n:
                bits.append(f"{vi}/{vt_i}/{vn_i}")
            elif has_uv:
                bits.append(f"{vi}/{vt_i}")
            elif has_n:
                bits.append(f"{vi}//{vn_i}")
            else:
                bits.append(str(vi))
        face_lines.append("f " + " ".join(bits))

    with open(obj_path, "w", encoding="utf-8") as f:
        f.write(f"mtllib {mtl_file}\n")
        f.write(f"o {node.name.replace(' ', '_')}\n")
        for i in range(mesh.num_vertices):
            p = mesh.vertices[i]
            f.write(f"v {p.x:.6f} {p.y:.6f} {p.z:.6f}\n")
        for u, v in vt_list:
            f.write(f"vt {u:.6f} {v:.6f}\n")
        for x, y, z in vn_list:
            f.write(f"vn {x:.6f} {y:.6f} {z:.6f}\n")
        f.write(f"usemtl {mtl_name}\n")
        for line in face_lines:
            f.write(line + "\n")

    print(f"wrote {obj_path} verts={mesh.num_vertices} faces={mesh.num_faces} uvs={len(vt_list)}")


def write_mtl(path, material_name, tex_name):
    with open(path, "w", encoding="utf-8") as f:
        f.write(f"newmtl {material_name}\n")
        f.write("Kd 1.000000 1.000000 1.000000\n")
        f.write("Ks 0.200000 0.200000 0.200000\n")
        f.write("d 1.000000\n")
        f.write("illum 2\n")
        f.write(f"map_Kd {tex_name}\n")


def main():
    dest = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\rpg"
    write_mtl(os.path.join(dest, "rpg.mtl"), "RPG7", "RPG7.png")
    scene = ufbx.load_file(os.path.join(dest, "RPG7.fbx"))
    for node in scene.nodes:
        if not node.mesh:
            continue
        name = node.name.lower()
        if "rocket" in name:
            export_node(node, os.path.join(dest, "rpg_rocket.obj"), "rpg.mtl", "RPG7")
        elif name == "rpg7":
            export_node(node, os.path.join(dest, "rpg_body.obj"), "rpg.mtl", "RPG7")

    sniper_fbx = (
        r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\_downloads"
        r"\extracted\Sniper_1A\Sniper_1A\Sniper_1A\Sniper_1A.fbx"
    )
    sniper_dest = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\sniper"
    write_mtl(os.path.join(sniper_dest, "sniper_parts.mtl"), "SniperPallet", "Sniper_parts.png")
    scene2 = ufbx.load_file(sniper_fbx)
    for node in scene2.nodes:
        if not node.mesh:
            continue
        low = node.name.lower()
        mapping = {
            "magazine": "sniper_mag_authored.obj",
            "body": "sniper_body_authored.obj",
            "upper_body": "sniper_upper_authored.obj",
            "scope": "sniper_scope_authored.obj",
        }
        if low in mapping:
            export_node(
                node,
                os.path.join(sniper_dest, mapping[low]),
                "sniper_parts.mtl",
                "SniperPallet",
            )


if __name__ == "__main__":
    main()
