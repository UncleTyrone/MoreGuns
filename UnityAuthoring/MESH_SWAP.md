# Mesh swap (looks-good checklist)

Do **one gun at a time** (sniper → smg → rpg). Keep prefab components; only change meshes, materials, and icons.

## A. Models

Split meshes (body vs mag) are already in these folders (and copied into the ripped Unity project):

- `Assets/Models/sniper/` — `sniper_body.obj` + `sniper_mag.obj` (or expand `Sniper_parts.fbx` for `Body` / `Magazine`)
- `Assets/Models/smg/` — `smg_body.obj` + `smg_mag.obj`
- `Assets/Models/rpg/` — `rpg_body.obj` + `rpg_rocket.obj` (also children inside `RPG7.fbx`)

**Materials / textures**

- Quaternius sniper/SMG (`sniper_body.obj`, `smg_*.obj`): **no PNG** — solid MTL colors (`Black`, `Metal`, …). After Unity imports them, run **MoreGuns → Assign Materials To Split Guns**.
- RPG: `rpg_body.obj` / `rpg_rocket.obj` use `RPG7.png` (or just expand `RPG7.fbx` — texture is already on those children).
- Stylized sniper: `sniper_*_authored.obj` + `Sniper_parts.png`, or expand `Sniper_parts.fbx`.

Credits: `Assets/Models/CREDITS.md`.

## B. Import

1. Select FBX → **Model**: Scale Factor `1`, enable Read/Write if needed.
2. **Materials**: Extract into `Assets/Models/{id}/Materials/`.
3. **Animation**: off (keep the AK animator).
4. Drop the mesh next to an AK47 in a scene. If it is huge/tiny, try Scale Factor `0.01`, `0.1`, or `100`.

## C. Align to the AK grip

1. Open `{id}_equippable.prefab`.
2. Under `K47` / `Frame`, disable the old `MeshRenderer` (do not strip the root).
3. Parent the new FBX under the **same parent** as `Frame`.
4. Line up **grip** (right hand), **barrel** down-range, **MuzzlePoint** at the tip.
5. Sniper: longer barrel, scale down if it clips the camera. SMG: smaller, closer. RPG: thick tube on the shoulder line.
6. Delete or leave disabled the old AK meshes.

## D. Muzzle and audio

Move `Muzzle Flash` and `MuzzlePoint` to the new barrel tip. Optional: swap clips on `Fire Sound`.

## E. Magazines (plain English)

There are **two different magazine prefabs**. Mixing them up is why reload looks broken.

| Prefab | When you see it |
|--------|-----------------|
| Inside `SMG_Equippable` / `Sniper_Equippable` → child named `Magazine` | Sitting **in the gun** while you hold it |
| `SMG_Magazine_AvatarEquippable` / `Sniper_Magazine_AvatarEquippable` | In your **hand during the reload animation** |

The game does **not** magically know where your new mesh’s mag well is. On reload it parents the hand-mag onto the gun’s `Magazine` transform. If that transform is still in the old AK spot (or scaled wrong), the mag flies to the barrel tip or becomes huge.

### Fix the mag **in the gun** (held view)

1. In the Project window open  
   `Assets/resources/weapons/ak47/AK47_Equippable.prefab`  
   and  
   `Assets/resources/weapons/smg/SMG_Equippable.prefab`  
   (do sniper the same way later).
2. In the Hierarchy, expand until you see a child named **`Magazine`** (there may be more than one — pick the one with a MeshRenderer that looks like a magazine).
3. On the AK, notice: that `Magazine` sits under the receiver, sticking **down**, not at the muzzle.
4. On the SMG, move / rotate / scale **your** `Magazine` until it sits in the SMG’s mag well the same way. Use the Scene view Move/Rotate tools; don’t edit numbers blindly.
5. If you see a **second** `Magazine` that is disabled / looks like the old AK mag (often crazy scale like `10`), leave it **disabled** or delete it after your new one looks right. That leftover is a common cause of “mag at the end of the barrel.”

### Fix the mag **in the hand** (reload animation)

1. Open `Assets/resources/weapons/smg/magazine/SMG_Magazine_AvatarEquippable.prefab`.
2. Select its child `Magazine` mesh.
3. Set **Transform Position** to `(0, 0, 0)`.
4. Adjust **Scale** until the mag looks about the same size as the one seated in the gun (often start around `1`, or `0.1` if the import is huge — pick **one** scale, don’t stack a tiny mesh under a huge parent).
5. Optional: open `SMG_Magazine_Trash.prefab` and make that mag the same size (that’s the empty mag that drops).

### Quick check in Unity (no game needed)

- Prefab Mode on `SMG_Equippable`: mag should already look correct in the gun.
- Prefab Mode on `SMG_Magazine_AvatarEquippable`: mag alone should look normal-sized, not room-sized.

Then: **MoreGuns → Build MoreGuns Bundle**, rebuild/copy the DLL.

### RPG note

Keep the rocket object named exactly **`Magazine`**. The mod hides it when empty and shows it again when you actually have a rocket loaded. Seat that mesh inside the tube.

If Unity says **missing scripts** when saving: wait for scripts to recompile (Console clean), then try again. Never strip root components — only move/disable meshes.

To hide RPG casings without Prefab Mode: menu **MoreGuns → Disable Shell Ejection On RPG**.

## F. Shop icons

Assign new sprites on `{id}.asset` and `{id}_magazine.asset` so Stan does not show an AK thumbnail.

## G. Rebuild

**MoreGuns → Build MoreGuns Bundle**, rebuild the DLL, copy to Mods. In-game check: barrel vs reticle, hand clip, flash at tip, third-person silhouette.

## H. Audio (already assigned)

Each new gun has CC0 clips under `Assets/AudioClip/{id}/` (and mirrored in `UnityAuthoring/Assets/Audio/Weapons/`):

- `{id}_fire1.ogg` … `_fire3.ogg` on **Fire Sound** (random Clips)
- `{id}_empty.ogg` on **Empty Sound**
- `{id}_reload.ogg` on **Reload Sound**

Credits: `UnityAuthoring/Assets/Audio/CREDITS.md`.
