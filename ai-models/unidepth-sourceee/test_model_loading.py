
import json
import torch
from safetensors.torch import load_file
from unidepth.models import UniDepthV2

print("Loading UniDepth V2 Small configuration...")

with open("configs/config_v2_vits14.json", "r") as f:
    config = json.load(f)

print("Creating UniDepth V2 Small model...")
model = UniDepthV2(config)

print("Loading local pretrained weights...")
weights = load_file(
    "weights/unidepth_v2_small.safetensors",
    device="cpu"
)

result = model.load_state_dict(weights, strict=False)

print("Missing keys:", len(result.missing_keys))
print("Unexpected keys:", len(result.unexpected_keys))

print("Moving model to GPU...")
model = model.to("cuda")
model.eval()

print("UniDepth V2 Small loaded successfully!")
print("Device:", next(model.parameters()).device)
print(
    "GPU memory allocated:",
    round(torch.cuda.memory_allocated() / 1024**2, 2),
    "MB"
)

import time
import numpy as np
from PIL import Image

image_path = r"C:\Users\96170\Downloads\OneMeter.jpeg"

image = Image.open(image_path).convert("RGB")
image_array = np.array(image)

# Convert RGB image to PyTorch tensor
rgb = torch.from_numpy(image_array).permute(2, 0, 1)
rgb = rgb.to("cuda")

print("\nRunning UniDepth V2 inference...")

with torch.inference_mode():
    # Warm up GPU
    _ = model.infer(rgb)

    torch.cuda.synchronize()
    start = time.perf_counter()

    predictions = model.infer(rgb)

    torch.cuda.synchronize()
    elapsed = time.perf_counter() - start

depth = predictions["depth"]

print("Depth shape:", depth.shape)
print("Minimum depth:", round(depth.min().item(), 3), "m")
print("Maximum depth:", round(depth.max().item(), 3), "m")
print("Center depth:", round(depth.flatten()[depth.numel() // 2].item(), 3), "m")
print("UniDepth V2 inference time:", round(elapsed, 3), "seconds")
