# Subtle sea shader — 2026-09-29 (removed)

Status: removed from SampleScene on 2026-09-29 at the user's request. The six sea and foreground-wave renderer material slots now use Unity's built-in Sprites-Default material. The shader and its two materials are retained in `QA/ArtBackups/2026-09-29-SeaShader/RemovedAssets/` for recovery.

Added Assets/SB/Art/Shaders/CasualSea.shader and two materials under Assets/SB/Art/Materials/Sea. SampleScene assigns CasualSea to the three SeaLayerRoot sprites, and CasualSeaForeground to the three WaveLayerRoot sprites. Existing source images, alpha fades, tile flips and scroll settings remain the inputs.

The sea's painted shapes drift by a few texture pixels and the existing bright patches vary slightly in brightness. Edge attenuation pins the full-texture tile borders and horizon. Highlight phase uses world position to remain continuous between mirrored tiles. Foreground waves share the highlight timing with lower strength and zero UV distortion, retaining their silhouette.

## Material controls

- Flow Speed: 0.55, shared between both materials. Zero freezes the shader animation.
- Pattern Drift (UV X / Y): sea (0.0025, 0.004); foreground (0, 0). Controls texture displacement, not object motion.
- Soft Highlight Strength: sea 0.045; foreground 0.018. Zero removes highlight variation.
- Wave Frequency: (1.1, 4.3), controls spacing in world units.
- Tile Edge Protection: 0.04, fades displacement near full-texture borders.
- Highlight Brightness Threshold / Mask Softness: 0.32 / 0.22. Restricts highlight variation to lighter painted shapes.

Use full-texture sprites, as in the current parallax assets; this shader does not support atlas-packed UV rectangles. Sprite tint, renderer alpha and Flip X use Unity's built-in sprite helpers. The currently active quality setting renders with GraphicsSettings.currentRenderPipeline == null, despite a pipeline asset in GraphicsSettings; the shader therefore uses the working built-in sprite path. No render-pipeline setting was changed.

## Verification

- Confirmed all six scene material references saved on disk and loaded in Play Mode.
- Rendered actual portrait gameplay: Assets/QA/CasualSeaShaderRuntime.png.
- Shader compiler messages: none. Final current Unity console: zero errors and warnings.
- With cloned background geometry fixed, rendered frames three seconds apart: 126043 of 432000 pixels changed by more than 3 combined RGB byte values; mean per-channel delta 1.294/255. This confirms time-driven motion independent of parallax movement, with low overall contrast change.
- Inspected a mirrored tile join. The magenta strip at the bottom of the dedicated QA seam image is the diagnostic camera extending below all background geometry, not a gap between tiles.
- Uses a single texture sample and a single rendering pass; no extra camera, noise texture or frame update component. Physical mobile-device performance was not measured.

Scene backup before this task: QA/ArtBackups/2026-09-29-SeaShader/SampleScene.unity. Restore selectively if later scene edits exist. To disable only this visual treatment, reassign Sprites-Default to the six sea/foreground renderers.
