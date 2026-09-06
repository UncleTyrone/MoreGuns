# Changelog

## 1.6.4

### Stability

- Fixed intermittent Fatal CLR `0x80131506` (`MonoMod CompileMethodHook`) when loading a save: Harmony waits for the local player + settle time, then applies patches one class per frame instead of bulk `PatchAll` during Main/save deserialize.

## 1.6.3

### Multiplayer

- Other players can see and hear the guns you’re holding again (avatar equippable sync restored).

### Animations (NPCs and External Player Models)

- AK-47, sniper, SMG, minigun, and RPG use proper two-handed grips (player and NPCs).
- SMG uses a two-hand pistol-style grip so both hands sit on the weapon instead of an empty AK-style forward grip.

### Combat

- Fixed NullReference crashes when equipping SMGs (`PositionAnimationModel`) and when firing ranged weapons.
- Custom-gun bullet tracers are visible again for cops and other players.
