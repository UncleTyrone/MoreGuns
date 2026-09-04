# Unity setup (you do this once)

This machine already has **Unity 2022.3.62f3** at `C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe`. Use that editor (same 2022.3.62 line as the game).

## 1. Install the editor

1. Open **Unity Hub** (`C:\Program Files\Unity Hub\Unity Hub.exe`).
2. **Installs → Install Editor**.
3. **Archive** / official releases → **2022.3.62f1** or **2022.3.62f2**.
4. Modules: **Windows Build Support (IL2CPP)** is optional for this authoring project. A Personal license is enough.

## 2. Import ripped AK47 prefabs

AssetRipper GUI is already downloaded at `tools/AssetRipper/AssetRipper.GUI.Free.exe`.

1. Run that exe.
2. **File → Open** → `Resources/voidanesguns`.
3. **Export** to `UnityAuthoring/Assets/Ripped`.
4. In Hub: **Open** → folder `UnityAuthoring` (this repo). If Hub asks to upgrade, stay on 2022.3.62.

**Already done on this machine:** AssetRipper exported to `Assets/Ripped/ExportedProject`, Unity 2022.3.62f3 duplicated sniper/smg/rpg from AK47, and rebuilt `Resources/voidanesguns`.

Open **that** project in Hub (not the empty `UnityAuthoring` root):

`UnityAuthoring/Assets/Ripped/ExportedProject`

## 3. Rebuild after mesh / audio changes

SMG, Sniper, and RPG are already authored in this project and shipped in `Resources/voidanesguns`.

1. Edit prefabs under `Assets/resources/weapons/{smg,sniper,rpg,...}` as needed.
2. Menu **MoreGuns → Build MoreGuns Bundle**.
3. Rebuild `MoreGuns.dll` (`dotnet build -p:Backend=Il2Cpp`) so the embedded bundle updates.

## 4. Mesh swap reference

How the body/mag/muzzle swaps were done: [MESH_SWAP.md](MESH_SWAP.md). Source FBXs live under `Assets/Models/`.
