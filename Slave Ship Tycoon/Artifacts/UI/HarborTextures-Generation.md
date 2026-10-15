# Harbor UI textures

Generated with the built-in imagegen tool on 2026-10-14. The initial source images are 1254 x 1254; the newer metal button source is 2172 x 724. Source PNGs remain unedited. Unity imports each at a maximum size of 1024, uncompressed, with bilinear filtering and no mipmaps.

## Project assets

- `Assets/SB/UI/Shared/Textures/HarborWoodGrain.png`: neutral grayscale wood grain, repeated at fixed scales on panel backdrops, cards, and ordinary buttons. Repeat wrapping.
- `Assets/SB/UI/Shared/Textures/HarborBrassFace.png`: original square brass source, preserved as an earlier variant.
- `Assets/SB/UI/Shared/Textures/HarborMetalButtonV2.png`: wide 3:1 metallic face with horizontal reflection and thin bevel, used for boss-entry, upgrade, and multi-recruitment buttons. Clamp wrapping.
- `Assets/SB/UI/Shared/Styles/HarborMaterials.uss`: material selectors, palette tint, wood tile size, and narrow metal slicing.
- `Assets/SB/UI/Shared/Styles/HarborTheme.uss`: imports the material stylesheet; existing palette tokens continue to control colors.

## Adjustment

- `--texture-wood-size` controls the panel/card grain size (512 x 512 UI units); `--texture-control-size` controls ordinary button grain size (384 x 384). These keep the source proportions instead of fitting the grain to every rectangle.
- `--color-texture-wood`, `--color-texture-raised`, and `--color-texture-metal` in HarborTheme.uss control texture tint and opacity. Current alpha values are 0.6, 0.65, and 0.78. Metal hover, pressed, and disabled tint tokens are exposed next to them.
- The new metal face uses 12-pixel source slices at a `0.5px` slice scale. Thin edges remain consistent across button sizes, while the horizontally uniform center reflection follows the button shape.
- Ordinary and navigation buttons use wood. Boss-entry, upgrade, and multi-recruitment buttons use the stronger metal material. Disabled upgrade buttons retain a darkened texture.
- Transparent gameplay overlays, screen fades, and child ship/currency/skill icons are not texture targets.

## Final generation prompts

### Wood grain

```text
Use case: stylized-concept
Asset type: production raster texture for Unity UI panel backgrounds, 1024 x 1024 pixels square.
Primary request: edge-to-edge seamless flat horizontal hand-painted wood grain, a neutral grayscale multiplication tint mask for a casual pirate ship game interface.
Composition: the entire opaque square is the wood surface. Broad continuous horizontally flowing grain, medium readable grain detail, small painterly strokes. Flat 2D orthographic material swatch, evenly distributed.
Color palette: NEAR-WHITE NEUTRAL GRAYSCALE ONLY, all values approximately #D0D0D0 through #FFFFFF. No brown, no colored pigments. Uniform average brightness from edge to edge for later tinting in UI.
Style: restrained hand-painted cartoon wood, understated irregular grain suitable behind legible interface text.
Constraints: seamless on every edge. No physical directional lighting, no photorealism. No large knots, no deep cracks, no planks or seams, no frames, no vignette, no outside shadows, no large light/dark masses. No text, emblems, objects, borders, transparency, or scene. Fully opaque square texture.
```

### Brass button face

```text
Use case: stylized-concept
Asset type: production raster texture for a Unity UI 9-slice button face, 1024 x 1024 pixels square.
Primary request: edge-to-edge flat hand-painted aged brass surface, rendered as a NEAR-WHITE NEUTRAL GRAYSCALE multiplication tint mask, with fine brushed metal and tiny subtle hammered marks.
Composition: the entire opaque square is the button surface. Center 80 percent is low contrast and uniform for interface text legibility. Only in the outermost 24 to 32 pixels show a shallow light bevel on the top edge and a thin darker lower rim, suitable for 9-slicing. Square corners or very tiny corner rounding.
Color palette: neutral grayscale only, values approximately #C0C0C0 through #FFFFFF. No gold, brown, yellow, or colored pigments. Bright uniform center for later UI tinting.
Style: restrained painterly cartoon metal appropriate to a casual pirate ship game, small soft surface marks, no harsh speckles.
Constraints: flat 2D orthographic texture, not a photograph or real physical object. No text, emblems, ornament, rivets, objects, external background, outer drop shadow, vignette, transparency, or perspective. Fully opaque full square. Do not draw a button floating in a scene.
```

## Validation

Both generated sources were visually inspected. Unity imported both textures at 1024 x 1024 and loaded the new stylesheet. The first stylesheet import warned that `-unity-slice-scale` required a length unit; this was repaired to `0.25px`. Final Editor checks report `StyleSheet.importedWithErrors = false` and `StyleSheet.importedWithWarnings = false`.

At the start of the texture task, Unity already reported `InterceptorAttackCompo.cs(72,35)` error CS0019 (`MyShipData == null`), which blocked Play Mode. On the user's subsequent error-fix request, the invalid null comparison against the `MyShipData` struct was removed while preserving the target Ship null check. Unity compilation succeeded, Play Mode entry succeeded, and the textured HUD, upgrade buttons, and bottom navigation were visually checked at 540 x 960. Screenshot: `Assets/Artifacts/UI/Texture-Applied.png`. The Editor was returned to Edit Mode afterward. The existing `MerchantLedger needs an icon.` runtime message remains separate from the repaired compile error.

Unity references: [background images](https://docs.unity3d.com/6000.0/Documentation/Manual/UIB-styling-ui-backgrounds.html), [image import settings](https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-image-import-settings.html).

## Texture refinement after user feedback

The user reported a squeezed texture appearance and an artificial look. Texture coverage was reduced to large panel backdrops, opacity was lowered, and square-button stretching/slicing was replaced with fixed-scale centered sampling. The source PNGs were preserved. Actual runtime checks confirmed brass tint alpha 0.18, 1280 x 1280 background size, zero slicing, no image on the secondary navigation button or upgrade row, and wood tint alpha approximately 0.28 on the large backdrop. Final preview: `Assets/Artifacts/UI/Texture-Refined.png`. Style import errors and warnings were both false, and the Editor was restored to Edit Mode after the check.

## Restored textures and stronger metal buttons

The user clarified that textures should remain on UI elements and be refined, with some buttons more metallic. The preceding coverage reduction was superseded: wood was restored to ordinary buttons/cards at fixed grain scales, and selected action buttons received a new wide metal image with cleaner reflection and a thin rim. No source PNG was overwritten. Final runtime checks confirmed `HarborMetalButtonV2` on boss and upgrade buttons, `HarborWoodGrain` on ordinary navigation and upgrade rows, metal alpha 0.78, row alpha approximately 0.65, and 12-pixel metal slicing. Actual 540 x 960 Game view preview: `Assets/Artifacts/UI/Texture-Metallic.png`. Style import errors and warnings were both false. The Editor was returned to Edit Mode afterward. Multi-recruitment shares the metal selector but was not separately opened in this check.

### Final generation prompt: metal button v2

```text
Use case: stylized-concept
Asset type: production raster background for a Unity UI 9-slice metallic button face.
Primary request: create ONE wide horizontal LANDSCAPE 3:1 rectangular UI button face, approximately 1536 x 512 pixels, an opaque grayscale tint multiplier for a casual hand-painted pirate game interface.
Composition: the metallic surface completely fills the entire wide rectangular canvas edge to edge. No surrounding background. Calm readable center area. Horizontally uniform diffuse lighting allows stretching the center without distorting specific marks.
Material: clean hand-painted stylized brass metal, using NEUTRAL GRAYSCALE ONLY because amber color will be applied in Unity. Medium-strong clearly readable metallic finish: a broad soft horizontal reflection in the upper third, a narrower bright reflection strip, restrained fine horizontal brushed grain, and a slightly darker lower edge.
Color palette: neutral near-white grays, central surface approximately #C4C4C4 through #F4F4F4. No yellow, gold, brown, or colored pigments. The reflection strip may approach white; the thin lower edge may be darker.
Edges: extremely shallow straight beveled rim confined to the outermost 12 to 16 pixels on all four sides, prepared for 9-slice. Tiny rounded or square corners. Keep the rim thin and understated.
Style: polished stylized 2D game UI material swatch, understated hand-painted character. Smooth central text area, clean legible surface.
Constraints: opaque full-canvas wide rectangular texture. No thick frame, chunky hammered dents, pronounced scratches, ornate design, rivets, studs, engraved patterns, symbols, text, external background, drop shadow, photography, perspective, floating button object, or decorative objects.
```
