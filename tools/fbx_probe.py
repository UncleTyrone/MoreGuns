import ufbx
import traceback

p = r"D:\Schedule I\Schedule I\MoreGuns\UnityAuthoring\Assets\Models\rpg\RPG7.fbx"
try:
    scene = ufbx.load_file(p)
    print("ok scene", scene)
    print("num meshes", len(scene.meshes))
    print("num nodes", len(scene.nodes))
    for node in list(scene.nodes)[:20]:
        print("node", repr(node.name))
except Exception:
    traceback.print_exc()
