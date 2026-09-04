# MoreGuns Unity authoring

This folder is **not** the game. It is only for rebuilding the embedded `voidanesguns` asset bundle (meshes, audio, icons, prefabs).

**Shipped guns in the bundle:** AK47, MiniGun, SMG, Sniper, RPG (and their magazines / trash / avatar equippables).

**Open this Unity project:** `UnityAuthoring/Assets/Ripped/ExportedProject`

1. [UNITY_SETUP.md](UNITY_SETUP.md) — Unity 2022.3.62, open project, build bundle
2. [MODEL_SOURCES.md](MODEL_SOURCES.md) — mesh sources under `Assets/Models/`
3. [MESH_SWAP.md](MESH_SWAP.md) — how body/mag/muzzle swaps were done
4. [Extracted/PREFAB_LAYOUT.md](Extracted/PREFAB_LAYOUT.md) — prefab hierarchy notes
5. [Assets/Audio/CREDITS.md](Assets/Audio/CREDITS.md) / [Assets/Models/CREDITS.md](Assets/Models/CREDITS.md) — asset credits

Menu items (after the project opens in Unity):

- **MoreGuns → Build MoreGuns Bundle** — writes `Resources/voidanesguns` for the Melon mod to embed

After rebuilding the bundle, recompile `MoreGuns.dll` (`dotnet build -p:Backend=Il2Cpp`) so the new assets ship inside the mod.
