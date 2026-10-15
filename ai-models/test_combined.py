
import gc
import inspect
from dataclasses import replace
from pathlib import Path

import numpy as np
import torch
import depth_pro

from PIL import Image, ImageDraw
from transformers import (
    RTDetrImageProcessor,
    RTDetrV2ForObjectDetection
)


# ------------------------------------
# 1. Configuration
# ------------------------------------

device = torch.device(
    "cuda" if torch.cuda.is_available() else "cpu"
)

image_path = Path(
    r"C:\Users\96170\Downloads\test.jpg"
)

model_path = "ai-models/rtdetr-v2"
depth_checkpoint = "ai-models/depth-pro/depth_pro.pt"
output_path = "ai-models/combined_result.jpg"

print("Using device:", device)


# ------------------------------------
# 2. Load RT-DETRv2
# ------------------------------------

print("Loading RT-DETRv2...")

processor = RTDetrImageProcessor.from_pretrained(
    model_path
)

detector = RTDetrV2ForObjectDetection.from_pretrained(
    model_path
).to(device)

detector.eval()

# Save class names before releasing the detector
label_map = detector.config.id2label

print("RT-DETRv2 loaded successfully!")


# ------------------------------------
# 3. Detect objects
# ------------------------------------

image = Image.open(image_path).convert("RGB")

inputs = processor(
    images=image,
    return_tensors="pt"
).to(device)

with torch.inference_mode():
    outputs = detector(**inputs)

results = processor.post_process_object_detection(
    outputs,
    threshold=0.5,
    target_sizes=torch.tensor([
        (image.height, image.width)
    ])
)[0]

# Move detection results to CPU
results = {
    key: value.cpu()
    for key, value in results.items()
}

print("Object detection completed!")
print("Objects detected:", len(results["scores"]))


# ------------------------------------
# 4. Free GPU memory
# ------------------------------------

del detector
del inputs
del outputs

gc.collect()

if device.type == "cuda":
    torch.cuda.empty_cache()

print("Detector memory released!")


# ------------------------------------
# 5. Load Apple Depth Pro
# ------------------------------------

print("Loading Depth Pro...")

default_config = inspect.signature(
    depth_pro.create_model_and_transforms
).parameters["config"].default

config = replace(
    default_config,
    checkpoint_uri=depth_checkpoint
)

depth_model, transform = (
    depth_pro.create_model_and_transforms(
        config=config,
        device=device
    )
)

depth_model.eval()

print("Depth Pro loaded successfully!")


# ------------------------------------
# 6. Estimate depth
# ------------------------------------

print("Estimating distances...")

depth_image, _, f_px = depth_pro.load_rgb(
    str(image_path)
)

depth_input = transform(depth_image).to(device)

with torch.inference_mode():
    prediction = depth_model.infer(
        depth_input,
        f_px=f_px
    )

depth_map = (
    prediction["depth"]
    .detach()
    .cpu()
    .numpy()
)

print("Depth estimation completed!")


# ------------------------------------
# 7. Combine detections and distances
# ------------------------------------

draw = ImageDraw.Draw(image)

image_width, image_height = image.size
depth_height, depth_width = depth_map.shape

print("\nDetected objects and estimated distances:")
print("-----------------------------------------")

for score, label, box in zip(
    results["scores"],
    results["labels"],
    results["boxes"]
):

    x1, y1, x2, y2 = box.tolist()

    # Center of the detected object
    center_x = (x1 + x2) / 2
    center_y = (y1 + y2) / 2

    # Convert image coordinates to depth coordinates
    depth_x = int(
        center_x * depth_width / image_width
    )

    depth_y = int(
        center_y * depth_height / image_height
    )

    # Prevent out-of-range coordinates
    depth_x = max(
        0, min(depth_x, depth_width - 1)
    )

    depth_y = max(
        0, min(depth_y, depth_height - 1)
    )

    # Estimated distance at the center pixel
    distance = float(
        depth_map[depth_y, depth_x]
    )

    object_name = label_map[int(label)]
    confidence = float(score)

    # Prepare label
    if np.isfinite(distance) and distance > 0:
        text = f"{object_name}: {distance:.2f} m"
        distance_display = f"{distance:.2f} m"
    else:
        text = f"{object_name}: distance unknown"
        distance_display = "unknown"

    print(
        f"{object_name} | "
        f"Confidence: {confidence:.2f} | "
        f"Estimated distance: {distance_display}"
    )

    # Draw bounding box
    draw.rectangle(
        [x1, y1, x2, y2],
        outline="red",
        width=3
    )

    # Draw label
    draw.text(
        (x1, max(0, y1 - 18)),
        text,
        fill="red",
        stroke_width=1,
        stroke_fill="white"
    )


# ------------------------------------
# 8. Save combined image
# ------------------------------------

image.save(output_path)

print("-----------------------------------------")
print("Combined detection completed!")
print("Result saved to:", output_path)
