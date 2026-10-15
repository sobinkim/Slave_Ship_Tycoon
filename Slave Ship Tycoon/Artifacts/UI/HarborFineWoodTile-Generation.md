# Harbor Fine Wood Tile Generation

## Deliverable

- Asset: `Assets/SB/UI/Shared/Textures/HarborFineWoodTile.png`
- Built-in `image_gen.imagegen` used; one generation call.
- Output: 1254 × 1254 pixels, fully opaque (minimum alpha 255).
- Original generated file: `C:/Users/sobin/.codex/generated_images/01a13916-6dc4-7412-b5fb-9e40d5e58334/exec-f99b09d2-7ba1-4dfc-84f4-383c628d7caa.png`
- Style reference: `Assets/SB/Art/Sprites/Ships/TransportShip.png`.
- Negative reference: `Assets/SB/UI/Shared/Textures/HarborShipIronRimWoodGrainV3.png`.

## Visual inspection

A dark chestnut surface with narrow horizontal flowing grain and two small oval wood-growth marks. It contains no metal frame or plank seams. Generated strokes remain somewhat tapered and more numerous than requested, but are much finer than the previous large panel facets. Intended display is a fixed repeating tile rather than stretching the entire image over a tall panel. Exact seamlessness is requested in the prompt and should be judged in the consuming UI. This subtask does not modify USS, import metadata, or Unity state.

## Exact final prompt

```text
Use case: stylized-concept
Asset type: A single square 1024x1024 fully opaque seamless repeating wood material tile for a 2D Unity game UI panel. This is a flat material texture, not a UI panel or framed object.
Input images: Image 1 is STYLE REFERENCE ONLY, the game's actual cartoon pirate ship. Borrow its clean hand-drawn cartoon finish and rich warm timber colors. Do not reproduce the ship. Image 2 is a NEGATIVE REFERENCE ONLY showing the old unsuccessful framed UI texture. Specifically avoid its big pointed wedge shapes, broad jagged gouges, and metallic frame. Neither input is an edit target.
Primary request: create a new calm, recognizable dark chestnut timber texture filling the entire square canvas edge to edge. Use dark warm brown base colors around #563B2C to #704A30. It must read immediately as gently flowing wood grain rather than camouflage or scratch damage.
Materials and detail: about 25 to 40 sparse slender, gently curving horizontal grain strokes spread evenly across the square. Controlled hand-drawn cartoon grain lines approximately 6 to 10 pixels wide at source size, varying short to medium lengths, with natural broken ends and mild thickness variation. A couple of tiny incomplete elongated oval grain loops suggest wood growth. Use soft dark umber lines with restrained warm brown companion highlights. Keep these grain marks small and subtle, with calm medium-low contrast so cream UI text stays legible.
Style and composition: flat 2D cartoon surface, clean restrained drawing matching the reference game's ship materials, straight-on orthographic texture. Quiet consistent dark warm timber base without large tonal facets. Natural horizontal flow with modest irregularity. Tile seamlessly at every edge; distribute small details so tiling is unobtrusive.
Constraints: entire canvas opaque wood, no transparent pixels. No frame, outline around the canvas, border, bevel, panel, plank seams, metal, rivets, ornament, text, icons, scene, ship, perspective, vignette, lighting gradients, glossy reflections, photographic noise or realistic pores. Absolutely no broad jagged gouges, pointy wedges, arrow shapes, flames, camouflage patches, large faceted blobs, giant knots or large sweeping bands.
```

