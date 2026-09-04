<div align="center">

# More Guns ︻デ═一

[![Discord](https://img.shields.io/badge/Discord-VOID_Community-7289DA?style=for-the-badge&logo=discord&logoColor=white)](https://discord.gg/XB7ruKtJje)
[![License](https://img.shields.io/badge/LICENSE-MIT-5466b8?style=for-the-badge)](https://opensource.org/licenses/MIT)
[![Downloads](https://img.shields.io/badge/DOWNLOADS-10,000+-00B81F?style=for-the-badge)](https://www.nexusmods.com/schedule1/mods/904)

[![Patreon](https://img.shields.io/badge/Patreon-Support_Me-FF424D?style=for-the-badge&logo=patreon&logoColor=white)](https://www.patreon.com/c/Voidane)

</div>

<div align="center">

### [⬇️ Download from Nexus Mods](https://www.nexusmods.com/schedule1/mods/904)

</div>

## 📋 Overview

Adds new weapons to Schedule I. Buy guns and magazines from **Stan’s** arms dealer shop. Reload with **R** (minigun can be limited to Stan — see config).

### Guns

| Gun | Fire | Mag | Notes |
|-----|------|-----|--------|
| **AK47** | Full-auto | 30 | Classic assault rifle |
| **MiniGun** | Full-auto + windup | 400 | Heavy; optional manual reload |
| **SMG** | Full-auto | 30 | Compact, high rate of fire |
| **Sniper** | Semi-auto (~0.65s) | 5 | Long range, high damage |
| **RPG** | Single-shot | 1 | Explosive rocket; rocket mesh tracks ammo |

### Magazines / ammo items

- AK47 Magazine (30)
- MiniGun Magazine (400)
- SMG Magazine (30)
- Sniper Magazine (5)
- RPG Rocket (1)

## ✨ Features

- **Automatic fire** on AK47, MiniGun, and SMG
- **Custom meshes, sounds, and icons** per weapon
- **Stan’s shop** listings for every gun and magazine
- **Reload** with inventory magazines (R); SMG/sniper get a seated mag pop animation; AK uses its native reload clip; RPG reloads the rocket tube
- **RPG explosions** on hit
- **Optional crosshair** for MoreGuns weapons
- **Per-gun config** in `UserData/MoreGuns.cfg` (damage, price, shop availability, names, etc.)

## 📥 Installation

1. Install MelonLoader **0.7.3**
2. Copy the matching DLL into your Schedule I `Mods` folder:
   - Il2Cpp (main / Steam build): `MoreGuns.dll`
   - Mono (alternate branch): `MoreGunsMono.dll`
3. Launch the game (the weapon asset bundle is embedded in the DLL)

## 🛠️ Building

```
.\build.bat                   # Il2Cpp + Mono
.\build.bat nopause           # same, no pause (CI / scripts)
dotnet build                  # Il2Cpp -> bin\Il2Cpp\net6.0\MoreGuns.dll
dotnet build -c Mono          # Mono   -> bin\Mono\net6.0\MoreGunsMono.dll
dotnet build -t:BuildBoth     # both
```

Override install paths with `-p:GamePath="..."` (Il2Cpp + MelonLoader) and `-p:MonoGamePath="..."` (Alternate/Mono). Defaults look for `D:\Schedule I\Builds\Public` and `D:\Schedule I\Builds\Alternate`.

To change meshes/audio and rebuild the embedded bundle, see `UnityAuthoring/README.md`.

## ⚙️ Configuration

User settings live in `UserData/MoreGuns.cfg`:

```
["MoreGuns-! User Settings"]
"Allow Gun Crosshair" = true
"Allow Minigun Manual Reload" = true
```

Each gun has its own category (IDs: `ak47`, `minigun`, `smg`, `sniper`, `rpg`). Example for the AK:

```
["MoreGuns-ak47 Settings"]
"ak47 Damage" = 70.0
"ak47 Impact Force" = 200.0
"ak47 Min Aim FOV Reduction" = 10.0
"ak47 Max Aim FOV Reduction" = 10.0
"ak47 Accuracy Change Duration" = 0.5
"ak47 Magazine Size" = 30
"ak47 Display Name" = "AK47"
"ak47 Display Description" = "AK47 assault rifle A true American classic."
"ak47 Legal Status" = "Legal"
"ak47 Required Rank" = { Rank = "Underlord", Tier = 3 }
"ak47 Mag Display Name" = "AK47 Magazine"
"ak47 Mag Display Description" = "30-round magazine for the ak47 assault rifle."
"ak47 Mag Legal Status" = "Legal"
"ak47 Mag Required Rank" = { Rank = "Underlord", Tier = 3 }
"ak47 Price" = 15000.0
"ak47 Name" = "AK47"
"ak47 Shop Availability" = true
"ak47 Non-Available Reason" = ""
"ak47 Magazine Price" = 1000.0
"ak47 Magazine Name" = "AK47 Magazine"
"ak47 Magazine Shop Availability" = true
"ak47 Magazine Non-Available Reason" = ""
```

Default combat tuning for the newer guns (overridable in the same file):

| ID | Damage | Mag size | Style |
|----|--------|----------|--------|
| `smg` | 28 | 30 | Fast full-auto |
| `sniper` | 140 | 5 | Slow semi, long range |
| `rpg` | 200 | 1 | Explosive single shot |

## 🔄 Compatibility

- Schedule I **Il2Cpp** + MelonLoader **0.7.3** (primary)
- Mono alternate build supported via `MoreGunsMono.dll`
- Compatible with most other mods

## 🆘 Support

Having issues? Join our Discord community for support:

- **Discord**: [discord.gg/XB7ruKtJje](https://discord.gg/XB7ruKtJje)

## 👨‍💻 Credits

- Created by Voidane

## ⚖️ License and Usage

This mod is released as fair use. Other modders are welcome to:

- Study and learn from the code
- Incorporate portions of the code into their own projects
- Modify and redistribute the code

**Requirements:**

- Credit must be given to Voidane as the original creator
- Include a link to the original mod or Discord server when redistributing

---
