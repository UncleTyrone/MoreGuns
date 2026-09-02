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
**Info** Adds in new guns into the game, as of now it is in beta and ive been fixing a lot of the bugs. Guns will start be released after all important errors are resolved

**Guns** 
AK47
Minigun

**Bullets**
7.62x39mm
7.62x51mm

**Magazines**
AK47 Magazine (30 Rounds)
MiniGun Magazine (400 Rounds)

## ✨ Features
- Guns Automatics: Some guns can be fired automatically
- Gun Customization: Custom animations, sounds, and meshes for guns
- Store: All weapons can be bought at stans weapons shop!

## 📥 Installation
1. Install MelonLoader 0.7.3
2. Copy the matching DLL into your Schedule I Mods folder:
   - Il2Cpp (main branch): `MoreGuns.dll`
   - Mono (alternate branch): `MoreGunsMono.dll`
3. Launch the game

## 🛠️ Building
```
.\build.bat                   # Il2Cpp + Mono
.\build.bat nopause           # same, no pause (CI / scripts)
dotnet build                  # Il2Cpp -> bin\Il2Cpp\net6.0\MoreGuns.dll
dotnet build -c Mono          # Mono   -> bin\Mono\net6.0\MoreGunsMono.dll
dotnet build -t:BuildBoth     # both
```
Override install paths with `-p:GamePath="..."` (Il2Cpp + MelonLoader) and `-p:MonoGamePath="..."` (Alternate/Mono). Defaults look for `D:\Schedule I\Builds\Public` and `D:\Schedule I\Builds\Alternate`.

## ⚙️ Configuration
- User settings live in `UserData/MoreGuns.cfg`
```
["MoreGuns-! User Settings"]
"Allow Gun Crosshair" = true
"Allow Minigun Manual Reload" = true
```
- Each gun will have its own configuration like the ak47
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

## 🔄 Compatibility
- Works with Schedule I (Il2Cpp) and MelonLoader 0.7.3
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
