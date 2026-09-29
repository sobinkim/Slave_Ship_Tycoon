# Cannon water splash preview

Water splash VFX, now assigned to NormalProjectile's Water Splash Effect Prefab. The folder/prefab name retains Preview to preserve the existing asset identity.

Open `CannonWaterSplash_Preview.prefab` and preview its Particle Systems.
Normal projectile water impacts (lucky misses and shots whose target was lost) spawn this prefab.
It uses the existing TimedPoolEffect (0.85 seconds) and PoolingManager lifecycle.

## Inspector controls

- WaterColumn: Start Size X/Y controls width/height; Start Lifetime (0.56 s) controls all eight frames. Texture Sheet Animation is 4 x 2, one cycle.
- Droplets: Emission burst is 6; velocity, gravity and lifetime control the falling droplets.
- SurfaceRipple: start delay 0.2 s, lifetime 0.52 s; Size over Lifetime controls expansion.
- Renderers: Sorting Layer `Ship`; WaterColumn order 10, Droplets 11, SurfaceRipple -2. `Default` renders behind the project's `BackGround` layer regardless of its numeric order, so it must not be used for these effects.
- Material: Second Row Offset corrects the generated second-row waterline by 50 / 1024 UV. This is an asset-specific correction, not a gameplay positioning rule.
- Root TimedPoolEffect lifetime must exceed all child delays plus lifetimes.

The texture has genuine alpha; no chroma key or additive black-background removal is used.
The existing Sprite Editor package does not expose the skill's required slicing-capability API. This prototype therefore uses Particle System grid animation, without modifying sprite metadata or installing packages.

## Art provenance

Created with built-in imagegen, using ShipWaterWake_Right.png as the style reference.
Final edit prompt:

Edit this sprite sheet into a clean production TRANSPARENT PNG RGBA asset. Remove ALL black background and ALL diffuse cyan glow/halos between and around the splashes. Genuine fully transparent empty pixels, not black, not a checkerboard drawing. Keep ONLY the sharp opaque painted cyan water shapes, white foam, isolated droplets. Zero bloom, zero haze. Preserve exactly eight frames in a 4 columns x 2 rows grid at 1536x1024. Additionally align every water-contact center at local pixel (192,440) within its own 384x512 cell. Scale no frames; only shift artwork within each cell as needed. Keep visible splashes separated from each cell's edges by at least 15 pixels. Keep hand painted water shapes and the eight stages of rising and falling intact. No text no borders no labels. Background MUST be actual alpha=0 transparency.

## Verification and remaining work

Unity isolated preview rendering checked the visible peak and transparency; particle lifetime and repeated simulation were checked. See LuckMiss.md for combat rules, tuning and verification.
