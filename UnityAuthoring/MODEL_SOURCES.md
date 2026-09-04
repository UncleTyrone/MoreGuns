# Models (already in the repo)

CC0 meshes are in `Assets/Models/{sniper,smg,rpg}/`. The same files are copied into the Unity project at `Assets/Ripped/ExportedProject/Assets/Models/`.

**Use the split files** (body vs mag / rocket):

| Gun | Body | Mag / rocket |
|-----|------|----------------|
| Sniper (Quaternius) | `sniper_body.obj` | `sniper_mag.obj` |
| Sniper (stylized, named parts) | `Sniper_parts.fbx` → `Body` | same FBX → `Magazine` |
| SMG | `smg_body.obj` | `smg_mag.obj` |
| RPG | `rpg_body.obj` (or `RPG7.fbx` → `RPG7`) | `rpg_rocket.obj` (or FBX → `RPG7 Rocket`) |

Whole-gun FBXs (`SniperRifle.fbx`, `SMG.fbx`, `RPG7.fbx`) are still there if you want a single mesh.

Credits: [Assets/Models/CREDITS.md](Assets/Models/CREDITS.md). Next step: [MESH_SWAP.md](MESH_SWAP.md).
