# Casual ship artwork replacement — 2026-09-29

Applied the two user-approved attached PNGs to IroncladDefender and HarpoonRaider. Existing asset GUIDs were retained, so fleet icons, prefab sprites and animation sprite keys use the new artwork.

Updated art: IroncladDefender.png, HarpoonRaider.png, HarpoonRaider_Unloaded.png, Projectiles/RaiderHarpoon.png and their import settings. Source images and metadata before replacement are in `QA/ArtBackups/2026-09-29-CasualShips`.

The built-in image generator produced the unloaded hull and separate projectile from the approved HarpoonRaider image. Unloaded prompt: remove only the protruding arrowhead and shaft, preserve launcher, hull, palette, framing and transparent background. Projectile prompt: isolate a right-facing broad mauve barbed harpoon with a short wooden shaft, gold collar, thick plum outline, simple flat shading and transparent background.

## Inspector settings

| Sprite | Pixels per unit | Custom pivot |
| --- | --- | --- |
| IroncladDefender | 1596.8 | (0.5086, 0.4488) |
| HarpoonRaider and unloaded hull | 1290 | (0.47765, 0.52463) |
| RaiderHarpoon projectile | 1700 | (0.5, 0.5) |

HarpoonRaider prefab: ShootPos and Visual/LoadedHarpoon local position is (0.343, -0.103, 0). Existing loaded-spear position curves were offset by (+0.023, -0.043); original loading displacement and event timings remain intact. Loaded spear rotation remains 12 degrees; scale remains 0.72. No gameplay code changed.

## Verification

Rendered the actual prefabs with their attack clip sampled at 0.10 seconds: `QA/CasualShipsPreview.png`. Loaded spear is visible at 0.10 seconds and hidden at 0.20 seconds. All eight Idle/Move/Attack/Dead clips were inspected for valid sprite references and sampled in the Editor. Existing attack events and sinking curves were retained. Unity console reported no errors or compilation failure. This verifies asset connections and sampled animation; a full gameplay cycle was not replayed.
