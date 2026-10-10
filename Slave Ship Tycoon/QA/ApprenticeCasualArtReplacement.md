# ApprenticeGunship casual artwork — 2026-09-29

Redrew the currently used oversized-cannon ship to match the approved IroncladDefender and HarpoonRaider: plum outlines, mauve metal, crimson flag, cream sail, amber trim and large simple shaded shapes.

## Applied assets

- `Assets/SB/Art/Sprites/Ships/Animations/ApprenticeGunship/ApprenticeGunship_Idle_Sheet.png`: 4 frames.
- `ApprenticeGunship_Move_Sheet.png` in the same directory: 4 frames.
- `ApprenticeGunship_Attack_Sheet.png`: 6 frames including muzzle flash and recoil.
- `ApprenticeGunship_Dead_Sheet.png`: 6 progressively submerged frames.
- `Assets/SB/Data/PlayerFleet/EscortShips/Escort_A.asset` through `Escort_E.asset`: icon references changed to the new Idle_00 sprite.

Existing sprite GUIDs, frame names, 640x640 rectangles, centered pivots, 800 pixels per unit, clip frame timings and animation events are retained. Generated images were exported at the existing sheet dimensions: 1280x1280 for Idle/Move, 1920x1280 for Attack/Dead. No runtime code or controller changes. All player and enemy prefabs already sharing these sheets display the new art.

Built-in image generation used the old sheets for frame layout and motion, and the approved HarpoonRaider for palette/detail level. Prompt: redraw every original frame in clean thick burgundy outlines and simple broad mauve/amber/cream/crimson color shapes; preserve ship identity, frame registration, original grid, firing/recoil sequence and progressive sinking; transparent background, no labels or fine painted texture. Move/Attack/Dead additionally referenced the new Idle sheet for consistent design. Background-removal trial variants were not applied; Unity rendering confirmed the original generated alpha works correctly.

## Verification and recovery

All four clips have valid sprite references, with original AttackStart, AttackEnd and Dead event times. Sampled Idle, Move, firing frame and mid-sinking frame were rendered using the real prefab in an isolated Editor preview scene. Console reported no errors or compile failure. Full interactive battle was not replayed.

Preview: `QA/CasualFleetThreeShipsPreview.png`; state comparison: `QA/ApprenticeCasualAnimationPreview.png` (Idle/Move above, Attack/Dead below).

Original four sheets, metadata and changed fleet data are backed up in `QA/ArtBackups/2026-09-29-ApprenticeGunship`. No manual Inspector wiring is needed. Frame artwork is edited in the sheets, animation timing in the existing clips, and display size through existing prefab Visual transforms / sprite PPU.
