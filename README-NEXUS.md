[b][size=6]More Guns ︻デ═一[/size][/b]

[url=https://discord.gg/XB7ruKtJje][img]https://img.shields.io/badge/Discord-VOID_Community-7289DA?style=for-the-badge&logo=discord&logoColor=white[/img][/url]
[url=https://opensource.org/licenses/MIT][img]https://img.shields.io/badge/LICENSE-MIT-5466b8?style=for-the-badge[/img][/url]
[url=https://www.nexusmods.com/schedule1/mods/2528][img]https://img.shields.io/badge/DOWNLOADS-10,000+-00B81F?style=for-the-badge[/img][/url]

[b][size=5]📋 Overview[/size][/b]
Adds new weapons to Schedule I. Buy guns and magazines from [b]Stan’s[/b] arms dealer shop. Reload with [b]R[/b] (minigun can be limited to Stan — see config).

[size=5]Guns[/size]
[b]AK47[/b] [b]|[/b] Full-auto [b]|[/b] 30 Rounds [b]|[/b] Classic assault rifle
[b]MiniGun[/b] [b]|[/b] Full-auto + windup [b]|[/b] 400 Rounds [b]|[/b] Heavy; optional manual reload
[b]SMG[/b] [b]|[/b] Full-auto [b]|[/b] 30 Rounds [b]|[/b] Compact, high rate of fire
[b]Sniper[/b] [b]|[/b] Semi-auto (~0.65s) [b]|[/b] 5 Rounds [b]|[/b] Long range, high damage
[b]RPG[/b] [b]|[/b] Single-shot [b]|[/b] 1 Round [b]|[/b] Explosive rocket; rocket mesh tracks ammo

[size=5]Magazines / Ammo Items[/size]
[list]
[*]AK47 Magazine (30)[/*]
[*]MiniGun Magazine (400)[/*]
[*]SMG Magazine (30)[/*]
[*]Sniper Magazine (5)[/*]
[*]RPG Rocket (1)[/*]
[/list]

[b][size=5]✨ Features[/size][/b]
[list]
[*][b]Automatic fire[/b] on AK47, MiniGun, and SMG[/*]
[*][b]Custom meshes, sounds, and icons[/b] per weapon[/*]
[*][b]Stan’s shop[/b] listings for every gun and magazine[/*]
[*][b]Reload[/b] with inventory magazines (R); SMG/sniper get a seated mag pop; AK uses its native reload clip; RPG reloads the rocket tube[/*]
[*][b]Two-handed holds[/b] for AK47, sniper, SMG, minigun, and RPG (player and NPCs). SMG uses a compact two-hand pistol grip so both hands sit on the weapon[/*]
[*][b]Co-op sync[/b] — other players see and hear the guns you are holding; bullet tracers show for custom guns[/*]
[*][b]RPG explosions[/b] on hit[/*]
[*][b]Optional crosshair[/b] for MoreGuns weapons[/*]
[*][b]Per-gun config[/b] in UserData/MoreGuns.cfg (damage, price, shop availability, names, etc.)[/*]
[/list]

[size=4][b]📥 Installation[/b][/size]
[list=1]
[*]Install MelonLoader [b]0.7.3[/b][/*]
[*]Copy the matching DLL into your Schedule I [mono]Mods[/mono] folder:[/*]
[/list]
[list]
[*]Il2Cpp (main / Steam build): [mono]MoreGuns.dll[/mono][/*]
[*]Mono (alternate branch): [mono]MoreGunsMono.dll[/mono][/*]
[/list]
  3. Launch the game (the weapon asset bundle is embedded in the DLL)


[b][size=5]⚙️ Configuration[/size][/b]
User settings live in UserData/MoreGuns.cfg:
[code]["MoreGuns-! User Settings"]
"Allow Gun Crosshair" = true
"Allow Minigun Manual Reload" = true[/code]

Each gun has its own category (IDs: ak47, minigun, smg, sniper, rpg). Example for the AK47:
[code]["MoreGuns-ak47 Settings"]
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
"ak47 Magazine Non-Available Reason" = ""[/code]

[b][size=4]Default combat tuning for the newer guns (overridable in the same file):[/size][/b]
SMG | 28 Damage | 30 Rounds | Fast full-auto
Sniper | 140  Damage | 5 Rounds | Slow semi, long range
RPG | 200 Damage | 1 Round | Explosive single shot

[b][size=5]🔄 Compatibility[/size][/b]
[list]
[*]Schedule I [b]Il2Cpp[/b] + MelonLoader [b]0.7.3[/b] (primary)[/*]
[*]Mono alternate build supported via MoreGunsMono.dll[/*]
[*]Works with [b][url=https://www.nexusmods.com/games/schedule1/mods/1202]Police Response Overhaul[/url][/b] for armed police / pickpocket loadouts[/*]
[*]Compatible with most other mods[/*]
[/list]

[b][size=5]🆘 Support[/size][/b]
Having issues? Join our Discord community for support:
[list]
[*][b]Discord[/b]: [url=https://discord.gg/XB7ruKtJje]discord.gg/XB7ruKtJje[/url][/*]
[/list]

[b][size=5]👨‍💻 Credits[/size][/b]
[list]
[*]Created by Voidane[/*]
[/list]

[b][size=5]⚖️ License and Usage[/size][/b]
This mod is released as fair use. Other modders are welcome to:
[list]
[*]Study and learn from the code[/*]
[*]Incorporate portions of the code into their own projects[/*]
[*]Modify and redistribute the code[/*]
[/list]

[b]Requirements:[/b]
[list]
[*]Credit must be given to Voidane as the original creator[/*]
[*]Include a link to the original mod or Discord server when redistributing[/*]
[/list]
