from pathlib import Path
from PIL import Image
folder = Path(r"C:\Users\sobin\Slave_Ship_Tycoon\Slave Ship Tycoon\QA\InterceptorShip\ParticleUpdate-2026-10-14")
paths = sorted(folder.glob("frame-*.png"))[::2]
frames = [Image.open(p).convert("RGB").resize((480, 600), Image.Resampling.LANCZOS) for p in paths]
durations = [34] * len(frames)
durations[0] = 200
durations[-1] = 450
frames[0].save(folder / "interception-preview.gif", save_all=True, append_images=frames[1:], duration=durations, loop=0, optimize=True)
print(f"{len(frames)} captured frames -> interception-preview.gif")
