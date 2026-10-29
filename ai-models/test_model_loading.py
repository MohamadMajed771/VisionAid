
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
