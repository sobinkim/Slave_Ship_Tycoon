# Ship-style UI button generation

## Final assets (V2: tightly fitted silhouette)

Use `Assets/SB/UI/Shared/Textures/HarborShipIronButtonV2.png` and `HarborShipGoldButtonV2.png` (both 2172 × 724 RGBA). The original generation below is superseded because it had 50–70 px transparent top and bottom margins. A single targeted built-in imagegen edit per asset enlarged the plate body to reach the canvas sides; only clipped corner cutouts remain transparent. Palette, cel-shaded planes and plum outline are retained. No scripted image edits were performed.

### Final iron correction prompt

```text
Use case: precise-object-edit. Edit target: the single blank lavender iron UI button plate in the supplied image. Preserve its exact palette, bold dark plum outline, angular clipped corners, flat front view, bright pale angular upper-edge highlight, broad cel-shaded lavender planes, and darker faceted lower bevel. Preserve the large quiet center and illustrated cartoon ship armor finish. Make ONE targeted geometric correction: enlarge the button plate vertically and horizontally so its flat outside top edge reaches the first pixel row, its flat bottom edge reaches the final pixel row, and its left and right flat edges reach the canvas sides. Absolutely ZERO transparent padding above, below, left, or right. Only the tiny triangular cutouts at the four clipped corners remain truly transparent. Edge-to-edge, tightly cropped to the silhouette rectangle. The upper and lower frame must remain narrow around 8 percent of canvas height, not grow into thick bands. Landscape 3:1 canvas. Maintain a single centered blank rectangle, no perspective. No new details, no rivets, no symbols, text, screws, emblems, realistic grain, specular effects, noise, or external shadow. Do not change color, style, or materials; correct only the empty canvas padding and keep alpha in corner cutouts.
```

### Final gold correction prompt

```text
Use case: precise-object-edit. Edit target: the single blank yellow-gold UI button plate in the supplied image. Preserve its exact palette, bold dark plum outline, angular clipped corners, flat front view, bright lemon-cream angular upper-edge highlight, broad cel-shaded saturated gold face, and orange-ochre lower bevel. Preserve the large quiet center and illustrated cartoon ship gold-hardware finish. Make ONE targeted geometric correction: enlarge the button plate vertically and horizontally so its flat outside top edge reaches the first pixel row, its flat bottom edge reaches the final pixel row, and its left and right flat edges reach the canvas sides. Absolutely ZERO transparent padding above, below, left, or right. Only the tiny triangular cutouts at the four clipped corners remain truly transparent. Edge-to-edge, tightly cropped to the silhouette rectangle. The upper and lower frame must remain narrow around 8 percent of canvas height, not grow into thick bands. Landscape 3:1 canvas. Maintain a single centered blank rectangle, no perspective. No new details, no rivets, no symbols, text, screws, emblems, realistic grain, specular effects, noise, or external shadow. Do not change color, style, or materials; correct only the empty canvas padding and keep alpha in corner cutouts.
```

Built-in `image_gen` tool. Generated 2026-10-14. Source images were viewed as style references, not edit targets: `TransportShip.png` and `IroncladDefender.png` under `Assets/SB/Art/Sprites/Ships`.

## Iron

Final asset: `Assets/SB/UI/Shared/Textures/HarborShipIronButton.png` (2172 × 724 RGBA PNG).

Visual review: broad lavender iron planes, pale angular upper-edge highlight, dark plum outline. Quiet middle for dark text. No rivets or noise. Transparent outer corners and margin retained.

Final prompt:

```text
Use case: stylized-concept. Asset type: a single production game UI button background, transparent PNG. Generate a NEW asset matching the actual TransportShip and IroncladDefender sprites just viewed in this conversation. Those two images are STYLE REFERENCES ONLY; do not reproduce a ship. The reference ships have bold dark plum ink outlines, chunky angular shapes, broad painted cel shading, decisive pale highlights, and lavender gray iron armor.

Subject/composition: one front-facing flat rectangular iron armor button plate. Canvas LANDSCAPE 3:1, ideally 1536 by 512. The plate fills almost the entire canvas; only tiny clipped outside corner triangles are transparent. Rectangular silhouette, squared or subtly clipped angular corners, long straight horizontal sides. Deep near-black plum outline, cool muted lavender-gray iron center around #A99AB7, 2 or 3 large calm flat graphic cel-shaded planes, one narrow angular pale highlight along the upper edge, darker plum-lavender faceted lower bevel. A broad light midtone quiet central face that will carry dark game text. Edge decoration stays within 48 to 64 source pixels so this is friendly to UI 9-slicing. Keep the vast horizontal center calm and free of distinct features. All edges are crisply illustrated and substantial enough to stay readable at a small UI size. Match the iron armor shading language of IroncladDefender, with the same confident plum outline and simplified hand-painted finish.

Constraints: a single blank button plate only. Genuine transparent background outside the clipped corners, no cast shadow outside. No text, letters, numbers, icons, symbols, emblems, screws, rivets, bolts, scratches, ornaments, wood, or ship objects. No realistic metal, brushed grain, chrome, photographic lighting, gradients, noise, glossy reflection bands, texture clutter, or 3D rendered look. Metal must feel like illustrated cartoon ship armor, made from clean broad cel-shaded color shapes. Do not make a pill shape.
```

## Gold

Final asset: `Assets/SB/UI/Shared/Textures/HarborShipGoldButton.png` (2172 × 724 RGBA PNG).

Visual review: saturated yellow gold, lemon upper-edge highlight, orange lower bevel and dark plum outline. Simple ship-hardware shading; no photoreal metal texture. Transparent outer corners and margin retained.

Final prompt:

```text
Use case: stylized-concept. Asset type: a single production game UI button background, transparent PNG. Generate a NEW asset matching the actual TransportShip and IroncladDefender sprites just viewed in this conversation. Those two images are STYLE REFERENCES ONLY; do not reproduce a ship. The reference ships have bold dark plum ink outlines, chunky angular shapes, broad painted cel shading, decisive pale highlights, and saturated yellow gold fittings.

Subject/composition: one front-facing flat rectangular golden ship hardware button plate. Canvas LANDSCAPE 3:1, ideally 1536 by 512. The plate fills almost the entire canvas; only tiny clipped outside corner triangles are transparent. Rectangular silhouette, squared or subtly clipped angular corners, long straight horizontal sides. Deep near-black plum outline, saturated yellow-gold center around #EAAF25, 2 or 3 large calm flat graphic cel-shaded planes, a narrow flat bright lemon-cream angular highlight along the upper edge, orange-ochre faceted lower bevel. A broad quiet yellow-gold central face that will carry dark game text. Edge decoration stays within 48 to 64 source pixels so this is friendly to UI 9-slicing. Keep the vast horizontal center calm and free of distinct features. All edges are crisply illustrated and substantial enough to stay readable at a small UI size. Match the golden rails and fittings on TransportShip, with the same confident plum outline and simplified hand-painted finish.

Constraints: a single blank button plate only. Genuine transparent background outside the clipped corners, no cast shadow outside. No text, letters, numbers, icons, symbols, emblems, screws, rivets, bolts, scratches, ornaments, wood, or ship objects. No realistic metal, brushed grain, chrome, photographic lighting, gradients, noise, glossy reflection bands, texture clutter, or 3D rendered look. Metal must feel like illustrated cartoon ship gold hardware, made from clean broad cel-shaded color shapes. Do not make a pill shape.
```

