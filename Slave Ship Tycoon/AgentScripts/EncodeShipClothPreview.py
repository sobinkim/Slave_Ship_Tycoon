from pathlib import Path
from PIL import Image, ImageChops
import json
folder = Path(r"C:/Users/sobin/Slave_Ship_Tycoon/Slave Ship Tycoon/Artifacts/Ships/ClothFlutter")
paths = sorted(folder.glob("Frame_*.png"))
frames = [Image.open(path).convert("RGB") for path in paths]
palette = frames[0].quantize(colors=256)
encoded = [frame.quantize(palette=palette, dither=Image.Dither.NONE) for frame in frames]
encoded[0].save(folder / "ShipClothFlutter.gif", save_all=True, append_images=encoded[1:], duration=70, loop=0, optimize=False, disposal=1)
difference = ImageChops.difference(frames[0], frames[6])
report=[]
for i in range(11):
    col, row = i % 4, i // 4
    cell=(col*384,row*384,(col+1)*384,(row+1)*384)
    hull=(col*384,row*384+272,(col+1)*384,(row+1)*384-10)
    report.append({"index":i,"motion_pixel_bounds":difference.crop(cell).getbbox(),"lower_hull_changed":difference.crop(hull).getbbox() is not None})
(folder / "PreviewPixelComparison.json").write_text(json.dumps(report,indent=2),encoding="utf-8")
print(json.dumps({"gif_bytes":(folder/"ShipClothFlutter.gif").stat().st_size,"frames":len(frames),"comparison":report},ensure_ascii=False))

