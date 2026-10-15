# Metal frame and wood grain generation

- Tool: built-in `image_gen.imagegen`, precise-object-edit mode.
- Date: 2026-10-14.
- No source sprites, USS, Unity import metadata or scene state were modified by this asset task.
- Outputs copied non-destructively into `Assets/SB/UI/Shared/Textures/`.
- All generated outputs inspected with `view_image`; all are 2172 × 724 pixels with alpha.

## Outputs

| File | Edit target | Change |
| --- | --- | --- |
| HarborShipIronRimWoodGrain.png | HarborShipIronRimWood.png | Added bold angular woodgrain across the wooden inset |
| HarborShipGoldRimWoodGrain.png | HarborShipGoldRimWood.png | Added bold angular woodgrain across the wooden inset |
| HarborShipIronRimWoodGrainV2.png | HarborShipIronRimWoodGrain.png | Attempted uniform metal rim geometry; retained grain |

The woodgrain edits preserve the metal coloring, bold plum outline, clipped corners and edge-to-edge placement. Intentional warm light/dark angular marks now appear through the middle of the wood inset, avoiding plank divisions and realistic fine noise.

### Geometry limitation for iron V2

A read-only centerline color boundary inspection measured approximate rim widths in source pixels: top 115, bottom 133, left 102, right 102. The generator reduced the lower bevel slightly but did **not** meet the requested exact 95 px width. The integrating agent was informed; exact final visual uniformity requires further layout handling. This record does not claim geometric equality.

## Exact initial iron edit prompt

```text
Use case: precise-object-edit
Asset type: existing 3:1 cartoon pirate game UI button texture, iron frame with wooden inset.
Input image: Image 1 is the EDIT TARGET. Edit only the warm brown WOODEN INSET.
Primary request: make the wood surface visibly read as hand-drawn cartoon wood, instead of an almost blank brown fill. Add 8 to 12 short, chunky, horizontal angular woodgrain notches and gouges distributed across the wood surface, INCLUDING some quiet marks in its central half. Use broad tapered pointed strokes with slightly lighter warm brown beside slightly darker chestnut strokes. The marks should be sparse, deliberate, varied in length and spacing, and clearly readable when this image is used small. Keep low to medium contrast against the brown so cream UI text will remain clear over the center. Retain broad flat cel-shaded coloring consistent with a 2D cartoon ship sprite.
Hard invariants: preserve the existing lavender-gray cartoon iron frame, its exact outer silhouette, shape, color, bevel highlights and dark plum outline. Preserve the overall 3:1 wide aspect ratio and edge-to-edge footprint. Preserve transparent clipped outer corners with actual alpha, do not add canvas padding. Keep the wood's overall warm shaded brown color.
Avoid: no new objects, rivets, symbols, icons or text. No plank seams or long dividing lines. No repeated horizontal stripes. No large knots. No photorealistic grain, brushed metal, realistic reflections, scratches on metal, fine grain noise or added decoration. Do not leave the entire central 75 percent blank; the new few quiet grain marks must be spread through the center as well.
```

## Exact initial gold edit prompt

```text
Use case: precise-object-edit
Asset type: existing 3:1 cartoon pirate game UI button texture, golden metal frame with wooden inset.
Input image: Image 1 is the EDIT TARGET. Edit only the warm brown WOODEN INSET.
Primary request: make the wood surface visibly read as hand-drawn cartoon wood, instead of an almost blank brown fill. Add 8 to 12 short, chunky, horizontal angular woodgrain notches and gouges distributed across the wood surface, INCLUDING some quiet marks in its central half. Use broad tapered pointed strokes with slightly lighter warm brown beside slightly darker chestnut strokes. The marks should be sparse, deliberate, varied in length and spacing, and clearly readable when this image is used small. Keep low to medium contrast against the brown so cream UI text will remain clear over the center. Retain broad flat cel-shaded coloring consistent with a 2D cartoon ship sprite.
Hard invariants: preserve the existing golden cartoon metal frame, its exact outer silhouette, shape, color, bevel highlights and dark plum outline. Preserve the overall 3:1 wide aspect ratio and edge-to-edge footprint. Preserve transparent clipped outer corners with actual alpha, do not add canvas padding. Keep the wood's overall warm shaded brown color.
Avoid: no new objects, rivets, symbols, icons or text. No plank seams or long dividing lines. No repeated horizontal stripes. No large knots. No photorealistic grain, brushed metal, realistic reflections, scratches on metal, fine grain noise or added decoration. Do not leave the entire central 75 percent blank; the new few quiet grain marks must be spread through the center as well.
```

## Exact iron V2 frame correction prompt

```text
Use case: precise-object-edit
Asset type: cartoon UI iron frame with wooden center, 2172 by 724 pixels, 3:1.
Input image 1 is the edit target.
Single change: CLEAN UP THE FRAME GEOMETRY so the visible metal band has the SAME WIDTH on ALL FOUR SIDES. The top, bottom, left and right metal rim must each measure about 95 pixels at this resolution, measured inward perpendicular to the outer edge. Make straight parallel inner/outer edges with clipped/chamfered corners. Currently the bottom metal bevel is thicker than the upper band: shrink that bottom band to the SAME approximately 95-pixel thickness as the others. The wood inset must expand downward into the released space. Keep the outer frame at the canvas edges, with no padding except tiny transparent clipped corner areas. The finished wooden rectangle inside should be horizontally and vertically centered with evenly inset edges.
Keep everything else: exact cool lavender-gray cartoon metal palette, bold dark plum outline, cel-shaded angular highlight style, warm brown wooden inset, the new deliberate angular woodgrain gouges spread across the entire inset including center. Preserve their low-medium contrast and overall amount, do not erase the wood texture. Preserve actual alpha, 3:1 aspect, overall silhouette and current illustration style. No new ornament, rivets, text, object, realistic material or noise. One single consistent rim width, clean rectangular construction.
```

## Generated source files

- Iron grain: `C:/Users/sobin/.codex/generated_images/01a138e7-2354-7f72-8b04-00d433d0a12b/exec-c386927b-d54f-4573-9ec8-1ffbd5b3f5a3.png`
- Gold grain: `C:/Users/sobin/.codex/generated_images/01a138e7-2354-7f72-8b04-00d433d0a12b/exec-b15ccd2e-ed99-4db0-bc53-bbb2e51649e4.png`
- Iron V2: `C:/Users/sobin/.codex/generated_images/01a138e7-2354-7f72-8b04-00d433d0a12b/exec-0b4b50c6-c8d3-403c-8d59-9aa6726c70a5.png`


## Iron V3 palette transfer on uniform gold geometry

- Final iron output: `Assets/SB/UI/Shared/Textures/HarborShipIronRimWoodGrainV3.png` (2172 × 724 RGBA).
- Edit target: `Assets/SB/UI/Shared/Textures/HarborShipGoldRimWoodGrainV2.png`.
- Color-only reference: `Assets/SB/UI/Shared/Textures/HarborShipIronRimWoodGrainV2.png`.
- Generated source: `C:/Users/sobin/.codex/generated_images/01a138e7-2354-7f72-8b04-00d433d0a12b/exec-3eadc4a2-e282-42ba-beed-2f603847a599.png`.
- Visual inspection: the gold rim is replaced by clean cel-shaded lavender-gray iron. The target gold V2 geometry and woodgrain are retained.
- Read-only centerline wood color boundary approximation: top 123 px, bottom 124 px, left 126 px, right 124 px. This is substantially more uniform than iron V2; color transitions and outlines account for a few pixels of variation.

### Exact iron V3 prompt

```text
Use case: precise-object-edit
Asset type: 3:1 cartoon ship game UI texture, metal rim with wood inset.
Input images: IMAGE 1 is the EDIT TARGET, a golden metal frame with wood inside. IMAGE 2 is COLOR REFERENCE ONLY for cool lavender iron colors; do not copy its frame geometry.
Single request: RECOLOR ONLY THE GOLD METAL RIM OF IMAGE 1 into the lavender-gray IRON palette of Image 2. Map bright gold highlights into very pale lavender/ivory highlights; map medium yellow/gold into muted lavender-gray iron; map orange/darkgold lower facets into darker plum-gray iron. The dark plum exterior outline remains unchanged. No yellow or gold remains in the metal. Keep crisp flat cel-shaded angular facets.
HARD INVARIANTS: Keep Image 1's exact frame geometry, thickness, edge positions, corner cuts, silhouette and 3:1 canvas. Its top/bottom/left/right metal bands are already the intended clean equal construction: do NOT reshape or inflate any side, especially the bottom. Recolor the existing facet regions in place. Preserve the Image 1 wooden inset completely: exact warm brown color, body dimensions, inset edge, woodgrain placements, amount and shapes. Preserve true transparent alpha at clipped corners and edge-to-edge placement with no new padding. No new drawing, texture, rivets, decorations, text or objects. Do not borrow Image 2's wood or geometry. This is a palette replacement of only the metal pixels on Image 1.
```


