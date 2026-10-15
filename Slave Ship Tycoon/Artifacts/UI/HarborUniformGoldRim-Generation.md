# Uniform Gold Rim Wood Button

- Tool: built-in `image_gen` targeted edit, one call.
- Input: `Assets/SB/UI/Shared/Textures/HarborShipGoldRimWoodGrain.png`
- Final: `Assets/SB/UI/Shared/Textures/HarborShipGoldRimWoodGrainV2.png`
- Generated source: `C:/Users/sobin/.codex/generated_images/01a138e9-bd8b-7c52-88ad-7b061187e6a8/exec-a4346db9-fc25-4207-bae9-3de683295f02.png`
- Visual inspection: the top, bottom, left, and right bands now use one consistent inner inset. The lower bevel is reduced and the upper/side bands are expanded. Broad angular wood grain, gold palette, clipped corners, and plum contours remain. Outer edge touches the canvas with transparency only at clipped corners.
- No USS, importer metadata, or Unity state changes were made by this generation task.

## Final prompt

```text
Use case: precise-object-edit
Asset type: wide 3:1 cartoon game UI button sprite, 2172 by 724.
Input image: Image 1 is the EDIT TARGET. Preserve its existing golden material colors, dark plum outlines, warm dark wood interior, and broad angular wood grain marks.
Primary request: change ONLY the geometry of the gold metal frame so all four straight sides have the exact SAME substantial thickness. At this canvas size the visible gold band must be 95 pixels wide at the top, bottom, left, and right, measured perpendicular from the outer plum outline to the inner plum outline. The INNER wooden rectangle is therefore inset exactly 95 pixels from every side, about 1982 by 534 pixels. The bottom band is currently too thick; shrink it. Make the top and side bands agree with this single uniform inset. Keep the four clipped corners neat and consistent.
Style: clean flat cartoon cel shading matching the supplied image, pale lemon bevel highlight, gold middle plane, ochre shadow, narrow dark plum outline. The bevel highlights and shadows must fit WITHIN the uniform 95 pixel frame; do not use a larger bottom bevel to fake depth. Avoid depth perspective. Front-facing orthographic symmetrical rectangle.
Constraints: preserve the exact wide 3:1 composition and wood texture palette, the clearly visible sparse broad wood grain pattern, original gold hues, and clean stylized finish. Do not redesign the wood grain. Straight horizontal and vertical inner edges, tidy mitred corner facets. Fill the whole image edge to edge with the button: outer plum outline touches all four canvas boundaries, no transparent strips or padding, transparency only at tiny clipped outer corners.
No text, icons, labels, rivets, realistic metal, brushed texture, noise, glossy gradients, shadows outside the button, extra objects, or scenery.
```

