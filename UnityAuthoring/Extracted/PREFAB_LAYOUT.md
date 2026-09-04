# Extracted MoreGuns bundle layout

Source: `Resources/voidanesguns` (UnityFS, Unity 2022.3.32f1 header).
Full object list: `index.json`. Hierarchy dump: `prefab_layout.json`.
Textures / meshes / audio were exported next to this folder.

## Required paths (WeaponBase.LoadGun)

For ID `ak47` the bundle contains:

| Path | Asset |
|------|--------|
| `assets/resources/weapons/ak47/ak47_equippable.prefab` | First-person `Equippable_RangedWeapon` |
| `assets/resources/weapons/ak47/ak47.asset` | `IntegerItemDefinition` |
| `assets/resources/weapons/ak47/magazine/ak47_magazine.asset` | Mag `IntegerItemDefinition` |
| `assets/resources/weapons/ak47/magazine/ak47_magazine_trash.prefab` | `TrashItem` |
| `assets/resources/weapons/ak47/magazine/ak47_magazine_avatarequippable.prefab` | Avatar mag |
| `assets/resources/avatar/equippables/ak47.prefab` | Third-person `AvatarEquippable` |

Same six paths exist for `minigun`. New guns must use the same pattern with IDs `sniper`, `smg`, `rpg`.

## AK47_Equippable (first person)

Root: `MonoBehaviour` (`Equippable_RangedWeapon`) + Transform.

Important children (from dump):

- Visual group `AK47` / `K47`
  - `Frame` (mesh) → `Barrel`, `Grip`, `Trigger`
  - `Magazine` (+ LOD)
- `MuzzlePoint` — aim / ray origin; keep this aligned with the barrel
- `Muzzle Flash` (`Flash Red`, `Flash Orange`) — move to the new muzzle
- `Shell Ejjection Particles`, `Smoke Particles`
- `Fire Sound`, `Reload Sound`, `Empty Sound`
- `PlayAnimation` on the animated visual (`AK47 Idle` / `Aiming` / `Fire` / `Reload`)

**Mesh swap:** replace `Frame` / `Barrel` / `Grip` meshes (or hide them and parent a new FBX under `K47`). Do not remove the root `Equippable_RangedWeapon`.

## AK47_Magazine_Trash

Root: Rigidbody + several MonoBehaviours including `TrashItem`. Children are mag meshes + collider. After a mesh swap, resize the collider to the new mag.

## Third-person `ak47` avatar prefab

`AvatarEquippable` + gun visual. Use the same rotation as first-person so other players see a matching silhouette.

## Icons / textures

Exported under `Extracted/textures/`:

- `AK47__Icon_*.png`, `AK47__Magazine_Icon_*.png`
- `AK47_Texture`, `AK47_Normal`, `AK47_Roughness`
- MiniGun equivalents
- UI: `Reload Message`, `Windup Indicator`
