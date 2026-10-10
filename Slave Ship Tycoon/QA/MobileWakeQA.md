# Mobile-style wake refinement — 2026-09-29

## Changes

- ShipWaterWake_Right.png: regenerated using the built-in imagegen skill/tool. Bigger rounded ivory bow crest, simplified mint/teal under-shapes; existing texture GUID/import references retained.
- ShipWakeEffect.cs: two explicitly assigned trailing foam renderers drift backward, expand/flatten and fade over a 0.7-second cycle, half a cycle apart. Movement still comes from the existing MOVE bool; death/stop fades everything out. No runtime object spawning or object searches were added.
- Added an initialization guard so disabling an uninitialized prefab/preview instance does not reset its authored scale to zero.
- Updated six prefabs in Assets/SB/Prefabs/PlayerShips: Test/TestMainShip, Test/TestEscort_A/B/C, IroncladDefender, HarpoonRaider. Added two child foam renderers per wake and increased bow-wave height.
- SampleScene: island opacity 0.6; island local scale Y 0.85 and local Y 4.083. The baseline stays aligned, with lower distant contrast and smaller silhouettes. Sky/sea art, game balance, UI and ship artwork remain unchanged.

## Inspector

- ShipWaterWake transform: base effect size. Y scale: main 1.55; harpoon 1.05; other four prefabs 1.12.
- Fade In/Out Seconds: 0.12 / 0.3.
- Ripple Cycles Per Second: 1.8; Ripple Scale Amount: 0.065; Ripple Opacity Amount: 0.08.
- Trailing Foam: two connected children, not auto-discovered. Foam Cycle Seconds controls rhythm; Foam Travel Local Offset controls direction/distance; Start/End Local Scale controls spreading; Trailing Foam Opacity controls strength.
- Existing prefabs without trailing-foam assignments retain the original single-renderer behavior.

## Play-mode QA

- Used the existing 1080×1920 portrait Game view. Captured a 540×960 before view without the aspect distortion of the previous 1280×720 capture.
- Entered the stage via the actual GameFlowManager.StartStage path. Inspected an initial approach at 0.15× speed, then restored Time.timeScale to 1.
- Observed a natural stage loop at normal speed. Captured when MOVE was true and wake alpha exceeded 0.75, then captured after all wake/foam renderers switched off in combat. Did not force MOVE or override combat stats.
- QA/MobileWakeNormalMoving.png and QA/MobileWakeNormalStopped.png are actual composited play-mode captures, not arranged previews.
- All four live parallax layers passed repeat-pitch, equal-height and alternating-flip checks after scrolling.
- Compilation succeeded; diff whitespace check passed. Final current-session Unity console: 0 errors, 0 warnings. The old MainShip/ShipStatCompo error did not recur in this run; its cause was not changed or claimed fixed.
- This validates the Unity portrait Game view, not physical-device performance/touch input. Main ship remains partly clipped at the left edge by the existing fleet layout; layout was left outside this visual-effect change.
- Editor left playing at normal speed. Temporary observation callback unregisters after capturing the movement and stopped states.

## Backup

Before this revision: QA/ArtBackups/2026-09-29-MobileWake contains the prior scene, wake art, effect script and player prefabs. Restore selectively to avoid losing unrelated subsequent edits.

## Image prompt (built-in, no API CLI)

Edit target: provided ship wake sprite. Redesign as polished cute mobile casual 2D game water effect, MUCH bolder and cleaner, not painterly. Single transparent-background sprite, 3:1 wide. Ship travels RIGHT. At right end a compact rounded ivory bow wave curls upward and splits into two chunky lobes, flowing into a broad low ivory crescent under the hull and 3 separated tapered foam streaks trailing LEFT. Main crest near x80%, with generous transparent padding, effect occupies middle 65% height. Flat warm ivory foam, pale mint undersides and muted teal shadow only 3 solid colors, smooth crisp contours, simple legible at 100 pixels wide. NO dark outlines, NO texture, NO fine speckles, NO long thin straight line, NO ship or ocean or background or text. True transparent alpha, isolated game VFX sprite.

Final asset: Assets/SB/Art/Sprites/Effects/ShipWaterWake_Right.png
