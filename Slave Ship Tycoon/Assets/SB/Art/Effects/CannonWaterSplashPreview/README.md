# Cannon water splash preview

Water splash VFX, now assigned to NormalProjectile's Water Splash Effect Prefab. The folder/prefab name retains Preview to preserve the existing asset identity.

Open `CannonWaterSplash_Preview.prefab` and preview its Particle Systems.
Normal projectile water impacts (lucky misses and shots whose target was lost) spawn this prefab.
It uses the existing TimedPoolEffect (0.6 seconds) and PoolingManager lifecycle.

## Inspector controls

- WaterColumn: Start Size X/Y controls width/height (1 / 0.72); Start Lifetime (0.4 s) controls all eight frames. Texture Sheet Animation is 4 x 2, one cycle. Local Y 0.235 aligns the surface contact.
- Droplets and SurfaceRipple are inactive. The sheet supplies the single small drop and thin ripple, keeping this effect restrained.
- Renderers: Sorting Layer `Ship`; WaterColumn order 10, Droplets 11, SurfaceRipple -2. `Default` renders behind the project's `BackGround` layer regardless of its numeric order, so it must not be used for these effects.
- Material: Second Row Offset corrects the generated second-row waterline by 88 / 1024 UV. This is an asset-specific correction, not a gameplay positioning rule.
- Root TimedPoolEffect lifetime must exceed all child delays plus lifetimes.

The texture has genuine alpha; no chroma key or additive black-background removal is used.

## Current simple style (2026-09-29)

The earlier blue plume and subsequent rounded cream plume were replaced by a low two-flick splash, one small drop, and a thin expanding ripple. Muted sea-green with ivory accents matches the casual wake. TimedPoolEffect is 0.6 seconds, longer than the 0.4-second sheet animation. Original identity and NormalProjectile reference are retained.

Built-in imagegen edit prompt:

Simplify dramatically. A modest quick splash when a small cannonball misses in a simple 2D mobile game. Eight sequential frames in a 4x2 equal grid, 1536x1024 transparent PNG. Only two short pointed water flicks and one small detached teardrop at peak. Peak height 90 pixels above contact baseline in each 384x512 cell, width 160 pixels; modest ellipse 180 pixels wide. Mostly muted desaturated sea-green with one thin ivory highlight. Sequence: tiny surface dent, two short flicks, peak low flicks with one drop, falling, flat ripple, expanding thin ripple, broken arcs, nearly empty. Plain flat cel cartoon, two solid colors, minimal detail, actual transparent alpha. Remove glow, halos, gradients, texture, petal shapes and extra droplets; no background, text or grid lines.

Final texture: Assets/SB/Art/Effects/CannonWaterSplashPreview/WaterSplashSheet.png.
QA/MobileMissSplashSimpleStages.png shows five actual Unity particle stages. QA/MobileMissSplashSimpleRuntime.png captures a natural combat water impact. These supersede the larger-plume QA/MobileMissSplashStages.png and MobileMissSplashRuntime.png. Original assets are backed up under QA/ArtBackups/2026-09-29-MobileMissSplash.
The existing Sprite Editor package does not expose the skill's required slicing-capability API. This prototype therefore uses Particle System grid animation, without modifying sprite metadata or installing packages.

## Art provenance

Created with built-in imagegen, using ShipWaterWake_Right.png as the style reference.
Earlier blue-plume edit prompt (historical; superseded by Current simple style above):

Edit this sprite sheet into a clean production TRANSPARENT PNG RGBA asset. Remove ALL black background and ALL diffuse cyan glow/halos between and around the splashes. Genuine fully transparent empty pixels, not black, not a checkerboard drawing. Keep ONLY the sharp opaque painted cyan water shapes, white foam, isolated droplets. Zero bloom, zero haze. Preserve exactly eight frames in a 4 columns x 2 rows grid at 1536x1024. Additionally align every water-contact center at local pixel (192,440) within its own 384x512 cell. Scale no frames; only shift artwork within each cell as needed. Keep visible splashes separated from each cell's edges by at least 15 pixels. Keep hand painted water shapes and the eight stages of rising and falling intact. No text no borders no labels. Background MUST be actual alpha=0 transparency.

## Verification and remaining work

Unity isolated preview rendering checked the visible peak and transparency; particle lifetime and repeated simulation were checked. See LuckMiss.md for combat rules, tuning and verification.
