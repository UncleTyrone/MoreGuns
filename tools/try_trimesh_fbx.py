import trimesh
p = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\rpg\RPG7.fbx"
try:
    m = trimesh.load(p, force=None)
    print(type(m), m)
    if hasattr(m, "geometry"):
        print("geoms", list(m.geometry.keys()))
        for k, g in m.geometry.items():
            print(k, g.vertices.shape, g.faces.shape)
    elif hasattr(m, "vertices"):
        print("verts", m.vertices.shape, "faces", m.faces.shape)
except Exception as e:
    print("ERR", type(e).__name__, e)
