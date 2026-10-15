# Harbor metal rim / iron with wood inner face

- Tool: built-in `image_gen.imagegen` (single precise-object-edit call).
- Saved asset: `Assets/SB/UI/Shared/Textures/HarborShipIronRimWood.png`.
- Canvas: 2172 × 724 px, RGBA, 3:1.
- Alpha bounds (left, top, right exclusive, bottom exclusive): alpha > 0: (0, 0, 2172, 724); alpha > 127: (0, 0, 2172, 721).
- Visual inspection: edge-to-edge clipped-corner silhouette; dark plum outline; lavender gray cel-shaded iron around perimeter; calm dark chestnut center with a few broad angular marks confined to left and right ends. No plank seams or repeated stripes. Source sprites unchanged.
- Original generated output: `C:/Users/sobin/.codex/generated_images/01a138dc-b693-7c93-89b5-2a9fe258923c/exec-6f2bbaf1-6cba-42ef-bf9d-c58c9c9e8ae3.png`.
- No USS, importer metadata, scenes, or Unity state changed in this asset subtask. Import and nine-slice integration are handled by the parent task.

## Inputs

1. EDIT TARGET: `Assets/SB/UI/Shared/Textures/HarborShipIronButtonV2.png`.
2. STYLE / MATERIAL REFERENCE ONLY: `Assets/SB/Art/Sprites/Ships/TransportShip.png`.
3. STYLE / MATERIAL REFERENCE ONLY: `Assets/SB/Art/Sprites/Ships/IroncladDefender.png`.

## Exact final prompt

```text
Use case: precise-object-edit
Asset type: one 3:1 wide, edge-to-edge Unity UI nine-slice button/panel plate on transparent background.
Input images: Image 1 is the EDIT TARGET, the existing lavender iron UI plate. Image 2 (TransportShip) and Image 3 (IroncladDefender) are STYLE AND MATERIAL REFERENCES ONLY from this game's actual ship sprites.
Primary request: change the inner face of Image 1 into quiet DARK CHESTNUT WOOD and keep a THIN CARTOON IRON FRAME around all four sides. The user's requested material structure is METAL OUTSIDE, WOOD INSIDE.
Keep: the existing exact wide 3:1 canvas, clipped-corner silhouette, edge-to-edge outer outline touching every canvas side, bold dark plum outline, flat lavender gray metal bevel colors, angular pale upper highlight and darker lower metal facet. Preserve transparency only in the tiny clipped corner triangles. No empty transparent strips above or below.
Frame: the existing outer lavender-gray iron rim becomes narrow, approximately 8% of the image height on each side. It is a continuous simple iron frame, clean solid cel-shaded facets, matching the ship armor. Keep its inner wood boundary straight and quiet for reliable nine-slice scaling. NO rivets, boltheads, ornaments, tabs or metal bars crossing the center.
Inner face: replace the former lavender metal center with a single calm dark chestnut wood in approximately #603D2B. Entire inner face is opaque and wooden. Center 75% must be flat, clean and undistracting for cream UI text. Only 2 or 3 very short, broad, angular, low-contrast wood marks near the far left and right edges. Warm modest cel-shaded wood, not a plank wall.
Constraints: no plank divisions, no repeated horizontal stripes, no seams, no center grain streaks, no big knots, no noisy texture, no realistic photographic grain, no brushed metal, no realistic reflections, no gradients simulating physical metal, no text, no icon, no watermark, no cast shadow, no background. Keep the image mostly plain within its thin metal frame. Match the reference ships' confident cartoon contours and broad color facets.
```

