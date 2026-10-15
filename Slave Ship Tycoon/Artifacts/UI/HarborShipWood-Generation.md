# Harbor ship cartoon wood UI assets

Generated on 2026-10-14 using the built-in `image_gen.imagegen` tool. No CLI/API fallback, bitmap crop, resample, or other manual pixel modification was used. Generated PNG files were copied into the project; original generated files and earlier asset versions were preserved.

## Style references

The following local images were inspected with `view_image` before generation and supplied through `referenced_image_paths` as **style and color references only**, not edit targets:

- `C:/Users/sobin/Slave_Ship_Tycoon/Slave Ship Tycoon/Assets/SB/Art/Sprites/Ships/TransportShip.png`
- `C:/Users/sobin/Slave_Ship_Tycoon/Slave Ship Tycoon/Assets/SB/Art/Sprites/Ships/CrusherShip.png`

The intended match is warm orange/reddish-brown timber, broad cel-shaded facets, deliberate sparse grain strokes, and a near-black plum outline (`#2A071E`).

## Final assets

| Asset | Absolute path | Actual dimensions | Transparency / result |
| --- | --- | --- | --- |
| Wood panel/card tile | `C:/Users/sobin/Slave_Ship_Tycoon/Slave Ship Tycoon/Assets/SB/UI/Shared/Textures/HarborShipWoodTile.png` | 1254 × 1254 | Opaque edge-to-edge square; broad cartoon plank faces and sparse dark grain marks. |
| Final wood button | `C:/Users/sobin/Slave_Ship_Tycoon/Slave Ship Tycoon/Assets/SB/UI/Shared/Textures/HarborShipWoodButtonV2.png` | 2172 × 724 | Wide 3:1 face; top/bottom transparent strips removed by imagegen editing. Tiny clipped corner transparency remains. |

The tool's native output dimensions differed from the approximate sizes requested in the prompts. The outputs were kept at their native dimensions.

The final button's alpha bounds, measured with alpha > 127, are **x 0–2171, y 0–723**. There are **0 fully transparent image rows**. Center alpha at the top row is 240 and at the bottom row is 199, reflecting edge antialiasing rather than empty padding.

## Tile generation

Settings: `transparent_background: false`; the two ship reference paths above were supplied through `referenced_image_paths`.

Selected generated source:

`C:/Users/sobin/.codex/generated_images/01a1383d-16d8-7883-ae1c-fbea20c914ab/exec-b8587022-ddde-4c80-b684-d174f51ae48b.png`

Exact prompt:

```text
Use case: stylized-concept
Asset type: square 1024x1024 opaque seamless wood tile for Unity game UI panels and cards.
Input images: Image 1 and Image 2 are STYLE AND COLOR REFERENCES ONLY showing this game's existing cartoon pirate ships. They are not edit targets. Create a new UI material asset matching their wood hulls, bold clean shapes, and game-sprite finishing.
Primary request: a hand-drawn warm mid/dark chestnut wood surface filling the entire canvas edge to edge, in the SAME clear CARTOON ART STYLE as the reference ships.
Materials/style: warm orange-brown and reddish-brown timber, broad flat cel-shaded facets, 2 or 3 large quiet tonal bands, sparse deliberate short broad grain marks, subtle horizontal plank divisions. Use muted dark plum #2A071E for a few structural plank strokes. Clean confident cartoon finishing, not realistic wood. Entire image is wood, no outer outline or frame.
Composition: tileable square surface, calm low-contrast center for cream UI text, evenly distributed horizontally flowing broad forms. No conspicuous central knot or object. No realistic grain. Average mid/dark chestnut tones close to the ships' shaded hull timber, especially brown approximately #794226, with restrained warmer facets.
Constraints: seamless edge-to-edge fully opaque material tile. No scene, ship, object, icon, text, symbols, metal, rivets, border, frame, large knots, fine swirling grain, tiny brush noise, photographic woodgrain, shiny varnish, photorealism, lighting simulation, perspective, or transparency.
```

## Button generation and refinement

### Initial generation

Settings: `transparent_background: true`; the two ship reference paths above were supplied through `referenced_image_paths`.

Generated source:

`C:/Users/sobin/.codex/generated_images/01a1383d-16d8-7883-ae1c-fbea20c914ab/exec-ed62ba58-184c-4f0d-b295-9df8a67a2f87.png`

The initial result matched the illustrated ship style, but included yellow/gold corner trim and horizontal transparent padding. It was refined using the two edits documented below.

Exact prompt:

```text
Use case: stylized-concept
Asset type: wide LANDSCAPE 3:1 cartoon wooden UI button sprite, approximately 1536x512, transparent only at tiny clipped corners.
Input images: Image 1 and Image 2 are STYLE AND COLOR REFERENCES ONLY showing this game's existing cartoon pirate ships. They are not edit targets. Create a new UI button asset matching the bold plum outlines, warm timber color, broad cel-shading, and clean game-sprite finishing of the references.
Primary request: front-facing flat orthographic WOODEN BUTTON PLATE, a wide rectangular wooden surface entirely filling the canvas except tiny transparent clipped corners. Bold near-black plum outline #2A071E, warm dark reddish-brown wooden flat face with center approximately #794226 for cream text readability.
Style/material: confident clean CARTOON 2D game sprite in exactly the reference ships' simple illustrated style. Broad flat cel-shaded forms. A narrow chunky warm upper edge and simple dark bottom shadow, a tiny flat angled highlight edge. Sparse deliberate short broad grain marks only toward outer edges. Quiet horizontally uniform center. Slightly clipped square corners, never a pill.
9-slice composition: reserve roughly 48 to 64 source pixels at the outer margins at 1536 width. Keep all outline and corner treatment within that band. No isolated objects or knots in the stretchable middle. Face reaches canvas edges, no padding or floating object composition.
Constraints: genuinely transparent tiny outside corners only, no scene/background. No text, symbols, icons, rivets, ornament, large knots, shiny metal, realistic woodgrain, micrograin, fine brush noise, gradients resembling photographs, photo lighting, perspective, deep shadow, or photorealism. Full-color timber like the reference hull, not grayscale.
```

### First edit: simplify the trim

The initial result was inspected with `view_image` before editing. Settings: `transparent_background: true`. Input 1 was the generated button; inputs 2 and 3 were the ship style references listed above.

Generated source, retained as the earlier project asset `HarborShipWoodButton.png`:

`C:/Users/sobin/.codex/generated_images/01a1383d-16d8-7883-ae1c-fbea20c914ab/exec-7a9897e0-2094-40a7-b338-406b8610877d.png`

The yellow/gold corner trim was replaced by a wooden beveled edge, but top/bottom empty strips remained. Its alpha bounds with alpha > 127 were x 0–2171, y 49–664.

Exact prompt:

```text
Use case: precise-object-edit
Asset type: 3:1 wooden cartoon UI button sprite.
Input images: Image 1 is the EDIT TARGET, the generated wooden UI button. Image 2 and 3 are STYLE REFERENCES ONLY, the game's existing pirate ships.
Change only these two issues on Image 1: remove the outside transparent horizontal padding so the button's dark outline touches all four canvas edges except tiny clipped corner triangles; and simplify the thick yellow/gold corner blocks and frame into a THIN WARM BROWN WOODEN beveled edge. No yellow/gold corner pieces or metal. The edge must fit within about 4 percent of image height, dark plum outline, small chestnut-orange upper highlight, simple bottom shadow. Keep the edge flat and chunky cartoon, not shiny.
Keep the wide 3:1 aspect ratio, quiet dark reddish-brown #794226 center, restrained intentional broad wood marks at outer edges, clean bold plum #2A071E outline, reference-matching cartoon cel-shaded drawing. The face must occupy at least 94 percent of total image height. Entire button is one simple wooden plate, not a decorative framed panel. Transparent corners only. No text, symbols, rivets, ornate details, realistic texture, micrograin, or photograph lighting.
```

### Final edit: extend the button to the canvas edges

The earlier project button was inspected with `view_image` before editing. Settings: `transparent_background: true`; `referenced_image_paths` contained only:

`C:/Users/sobin/Slave_Ship_Tycoon/Slave Ship Tycoon/Assets/SB/UI/Shared/Textures/HarborShipWoodButton.png`

Final generated source:

`C:/Users/sobin/.codex/generated_images/01a1383d-16d8-7883-ae1c-fbea20c914ab/exec-5722363f-77a1-45c8-8602-846445a4760a.png`

This edit extended the body vertically and removed all completely transparent horizontal rows. The result was copied to the sibling asset `HarborShipWoodButtonV2.png`; the earlier PNG was preserved.

Exact final prompt:

```text
Use case: precise-object-edit
Asset type: cartoon wooden UI button for a 9-slice background.
Input image: this is the EDIT TARGET. Keep its cartoon art, warm brown wood colors, calm center, dark plum outline, short broad wood marks, and same wide landscape aspect ratio.
Required edit: EXTEND THE WOODEN BUTTON BODY HEIGHT TO FILL THE ENTIRE CANVAS. Move and extend its upper edge outward until the outer dark outline touches the VERY TOP ROW of the image. Move and extend its lower edge outward until the outer dark outline touches the VERY BOTTOM ROW. Extend the wooden face with these edges. Remove ALL transparent horizontal strips above and below. There must be no empty horizontal margin at all. The solid dark outer top edge must run directly along the canvas's top boundary across nearly the full width, and the dark outer bottom edge must run directly along the canvas's bottom boundary across nearly the full width. Both side edges also touch their canvas boundaries. Only tiny clipped corner triangles may be transparent.
Do not retain the initial padded button height. Enlarge the plate body vertically, extending its shape to occupy the whole image. Keep the quiet center and the same simple illustration style. No other content changes. No metal, text, icons, symbols, new ornament, new grain, outside shadow, background, or photographic texture.
```

## Scope

This generation work created/copied PNG assets only. It did not edit USS, UXML, scenes, or metadata. This record was added separately under `Artifacts/UI`.
