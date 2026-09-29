# Mist parallax and ship wake — 2026-09-29

## Applied assets

- Replaced Sky, Island, Sea and ForegroundWave PNGs (_01–_03) in Assets/SB/Art/Sprites/Background/Parallax. Existing GUIDs and sprite dimensions (1656×316, 100 PPU) preserved.
- Added MistMoon.png and BackgroundRoot/MistMoon in SampleScene. The moon is fixed and does not repeat with sky tiles.
- Replaced Assets/SB/Art/Sprites/Effects/ShipWaterWake_Right.png with low ivory foam and muted teal wake artwork.
- Updated ShipWakeEffect settings on PlayerShips/HarpoonRaider, IroncladDefender and Test/TestMainShip, TestEscort_A/B/C prefabs. Original attachment positions and animation references retained.
- Updated Assets/SB/Scripts/Visual/ParallaxLayer.cs and Assets/SB/Animations/Scenes/SampleScene.unity.

Artwork generated with the imagegen skill from the approved lavender/teal concept. Four layer families use simplified silhouettes and broad color shapes; existing ship artwork is unchanged by this background task.

## Inspector controls

- ParallaxLayer: Scroll Speed retains the existing layer speeds. Sprite Width is the repeat pitch, set to 16.52 for a 16.56-wide tile (four source pixels of overlap).
- Alternate Mirrored Tiles: enabled on these four layers; initial tiles alternate normal/flipped. Recycled tiles take the opposite flip of the rightmost tile, including when there are three tiles.
- Each layer starts at local x 5.4, with subsequent tiles separated by the repeat pitch. Keep tile scale at one and each row at the same local y.
- Local y: Sky 4.65, Island 4.32, Sea 1.5, Wave 1.25. BackgroundRoot's existing transform is retained.
- MistMoon local position: (8.6, 4.95, 0). Change its transform for placement/size.
- ShipWakeEffect: Foam Color (1,1,1,0.9), Ripple Cycles Per Second 1.4, Ripple Scale Amount 0.018, Ripple Opacity Amount 0.06. Existing MOVE parameter and fade times retained.

## Verification and limits

- Unity compilation passed.
- All four layers passed 12 recycle operations each: repeat pitch, equal height and alternating flips checked after each operation.
- Rendered actual Unity assets in an isolated preview scene: MistSeaAndWakePreview.png and MistParallaxSeamPreview.png. Ships are arranged for comparison, not the runtime formation. Mirroring produces symmetrical scenery at tile joins; overlap reduces raster gaps but is not a unique continuous landscape.
- Three ship wake components passed movement-on / stopped-off checks after their Awake initialization in a preview scene.
- Full battle validation is not clean: console contains `MainShip needs a ShipStatCompo` from MainShip.StatUpdate during stage spawning, outside the background/wake changes. No claim of an error-free full battle run is made.
- Earlier test-harness Animator Hash 0 warnings came from invoking LateUpdate before Awake; the corrected initialized tests passed. No new console entries appeared during final renders/recycle tests.

## Recovery

Original background/wake textures and metadata, scene, ParallaxLayer script and affected prefabs were copied to QA/ArtBackups/2026-09-29-MistParallax before this task's changes. Restore selectively through Unity; do not overwrite unrelated subsequent edits.
