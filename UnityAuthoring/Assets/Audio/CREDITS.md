# Weapon audio credits

Clips live under `UnityAuthoring/Assets/Ripped/ExportedProject/Assets/AudioClip/{sniper,smg,rpg}/`
(and mirrored in `UnityAuthoring/Assets/Audio/Weapons/`).

## Sniper / SMG

| Role | Source |
|------|--------|
| Sniper fire 1–2 | Mosin Nagant — [Free Firearm Sound Library](https://opengameart.org/content/the-free-firearm-sound-library) (CC0) |
| Sniper fire 3 | Tikka — same library (CC0) |
| SMG fire 1–3 | PPSh — same library (CC0) |
| Empty | Gun dry-fire click — [BigSoundBank](https://bigsoundbank.com/) (Joseph Sardin, CC0) |
| Reload | Gun loading / mag insert / cock — BigSoundBank (CC0, concatenated) |

## RPG (rocket launcher)

| Role | Source |
|------|--------|
| Fire 1 | [Missile sound](https://opengameart.org/content/missile-sound) (mikeask, CC0) |
| Fire 2 | [rlaunch](https://opengameart.org/content/4-projectile-launches) (Michel Baradari, **CC-BY 3.0**) + metal tube whoosh (BigSoundBank CC0) |
| Fire 3 | Firework hiss + tube whoosh (BigSoundBank CC0) + flaunch (Michel Baradari, **CC-BY 3.0**) |
| Empty | Hollow metal hit + tube whoosh (BigSoundBank CC0) — not a gun click |
| Reload | Metal lid / plastic tube slide / tube whoosh / pipe clunk (BigSoundBank CC0) — rocket-into-tube load |

Credit **Michel Baradari** for RPG fire 2–3 (CC-BY 3.0).

Each weapon prefab already references: `*_fire1`–`*_fire3`, `*_empty`, `*_reload`.
