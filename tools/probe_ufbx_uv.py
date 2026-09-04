import ufbx
mesh = None
scene = ufbx.load_file(r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\rpg\RPG7.fbx")
for n in scene.nodes:
    if n.mesh and n.name == "RPG7":
        mesh = n.mesh
        break
print("uv exists", mesh.vertex_uv.exists)
print("type", type(mesh.vertex_uv))
print("dir sample", [x for x in dir(mesh.vertex_uv) if not x.startswith("_")])
vu = mesh.vertex_uv
print("values", type(vu.values), len(vu.values) if hasattr(vu.values,'__len__') else vu.values)
print("indices", type(vu.indices), len(vu.indices) if hasattr(vu.indices,'__len__') else None)
# try first face corner
face = mesh.faces[0]
idx = face.index_begin
print("vertex_indices", mesh.vertex_indices[idx])
print("uv indices[idx]", vu.indices[idx])
print("uv values[that]", vu.values[vu.indices[idx]])
# maybe .values[i] returns vec2 with .x .y
uv = vu.values[vu.indices[idx]]
print(uv, getattr(uv,'x',None), getattr(uv,'y',None))
