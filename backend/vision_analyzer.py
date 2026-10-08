
import gc
import inspect
import time
from contextlib import nullcontext
from dataclasses import replace
from pathlib import Path

import numpy as np
import torch
import depth_pro

from PIL import Image
from transformers import (
    RTDetrImageProcessor,
    RTDetrV2ForObjectDetection
)


# ------------------------------------
# Project configuration
# ------------------------------------

BASE_DIR = Path(__file__).resolve().parent.parent

RTDETR_PATH = BASE_DIR / "ai-models" / "rtdetr-v2"

DEPTH_CHECKPOINT = (
    BASE_DIR / "ai-models" / "depth-pro" / "depth_pro.pt"
)

DEVICE = torch.device(
    "cuda" if torch.cuda.is_available() else "cpu"
)

# Experimental: use FP16 autocast for Depth Pro on CUDA.
# Change to False to return to normal FP32 inference.
USE_DEPTH_FP16 = True


def synchronize_gpu():
    """Wait for GPU operations to finish before timing."""
    if DEVICE.type == "cuda":
        torch.cuda.synchronize()


def release_gpu_memory():
    """Release unused GPU cache."""
    gc.collect()

    if DEVICE.type == "cuda":
        torch.cuda.empty_cache()


def analyze_image(image_path):
    """
    Detect objects and estimate their distances.
    Returns a dictionary suitable for a JSON API.
    """

    start_time = time.perf_counter()
    image_path = Path(image_path)

    print("\n===================================")
    print("VisionAid Image Analysis")
    print("Device:", DEVICE)
    print("Depth FP16 enabled:", USE_DEPTH_FP16)
    print("===================================")

    # ------------------------------------
    # 1. Load image
    # ------------------------------------

    with Image.open(image_path) as source_image:
        image = source_image.convert("RGB")

    # ------------------------------------
    # 2. Load RT-DETRv2
    # ------------------------------------

    print("\nLoading RT-DETRv2...")

    detector_load_start = time.perf_counter()

    processor = RTDetrImageProcessor.from_pretrained(
        str(RTDETR_PATH)
    )

    detector = RTDetrV2ForObjectDetection.from_pretrained(
        str(RTDETR_PATH)
    ).to(DEVICE)

    detector.eval()
    label_map = detector.config.id2label

    synchronize_gpu()

    detector_load_time = (
        time.perf_counter() - detector_load_start
    )

    print(
        f"RT-DETRv2 loading time: "
        f"{detector_load_time:.2f} seconds"
    )

    # ------------------------------------
    # 3. Detect objects
    # ------------------------------------

    print("\nDetecting objects...")

    detection_start = time.perf_counter()

    inputs = processor(
        images=image,
        return_tensors="pt"
    ).to(DEVICE)

    with torch.inference_mode():
        outputs = detector(**inputs)

    results = processor.post_process_object_detection(
        outputs,
        threshold=0.5,
        target_sizes=torch.tensor([
            (image.height, image.width)
        ])
    )[0]

    results = {
        key: value.cpu()
        for key, value in results.items()
    }

    synchronize_gpu()

    detection_time = (
        time.perf_counter() - detection_start
    )

    print(
        f"RT-DETRv2 detection time: "
        f"{detection_time:.2f} seconds"
    )

    print(
        "Objects detected:",
        len(results["scores"])
    )

    # ------------------------------------
    # 4. Release RT-DETRv2 GPU memory
    # ------------------------------------

    del detector, inputs, outputs
    release_gpu_memory()

    # ------------------------------------
    # 5. Load Apple Depth Pro
    # ------------------------------------

    print("\nLoading Apple Depth Pro...")

    depth_load_start = time.perf_counter()

    default_config = inspect.signature(
        depth_pro.create_model_and_transforms
    ).parameters["config"].default

    config = replace(
        default_config,
        checkpoint_uri=str(DEPTH_CHECKPOINT)
    )

    depth_model, transform = (
        depth_pro.create_model_and_transforms(
            config=config,
            device=DEVICE
        )
    )

    depth_model.eval()

    synchronize_gpu()

    depth_load_time = (
        time.perf_counter() - depth_load_start
    )

    print(
        f"Depth Pro loading time: "
        f"{depth_load_time:.2f} seconds"
    )

    # ------------------------------------
    # 6. Estimate depth using FP16 autocast
    # ------------------------------------

    print("\nEstimating depth...")

    depth_inference_start = time.perf_counter()

    depth_image, _, f_px = depth_pro.load_rgb(
        str(image_path)
    )

    depth_input = transform(depth_image).to(DEVICE)

    use_autocast = (
        USE_DEPTH_FP16 and DEVICE.type == "cuda"
    )

    if use_autocast:
        precision_context = torch.autocast(
            device_type="cuda",
            dtype=torch.float16
        )
    else:
        precision_context = nullcontext()

    with torch.inference_mode():
        with precision_context:
            prediction = depth_model.infer(
                depth_input,
                f_px=f_px
            )

    # Convert depth output to FP32 before NumPy.
    depth_map = (
        prediction["depth"]
        .detach()
        .float()
        .cpu()
        .numpy()
    )

    synchronize_gpu()

    depth_inference_time = (
        time.perf_counter() - depth_inference_start
    )

    print(
        f"Depth Pro inference time: "
        f"{depth_inference_time:.2f} seconds"
    )

    # ------------------------------------
    # 7. Release Depth Pro GPU memory
    # ------------------------------------

    del depth_model, depth_input, prediction
    release_gpu_memory()

    # ------------------------------------
    # 8. Combine detections and distances
    # ------------------------------------

    depth_height, depth_width = depth_map.shape

    objects = []

    for score, label, box in zip(
        results["scores"],
        results["labels"],
        results["boxes"]
    ):

        x1, y1, x2, y2 = box.tolist()

        center_x = (x1 + x2) / 2
        center_y = (y1 + y2) / 2

        depth_x = int(
            center_x * depth_width / image.width
        )

        depth_y = int(
            center_y * depth_height / image.height
        )

        depth_x = max(
            0, min(depth_x, depth_width - 1)
        )

        depth_y = max(
            0, min(depth_y, depth_height - 1)
        )

        distance = float(
            depth_map[depth_y, depth_x]
        )

        if not np.isfinite(distance) or distance <= 0:
            distance = None
        else:
            distance = round(distance, 2)

        objects.append({
            "name": label_map[int(label)],
            "confidence": round(float(score), 2),
            "estimated_distance_m": distance,
            "bounding_box": [
                round(x1, 2),
                round(y1, 2),
                round(x2, 2),
                round(y2, 2)
            ]
        })

    # ------------------------------------
    # 9. Display performance results
    # ------------------------------------

    synchronize_gpu()

    total_time = time.perf_counter() - start_time

    print("\n===================================")
    print("VisionAid Performance Results")
    print("===================================")

    print(
        f"RT-DETRv2 loading:   "
        f"{detector_load_time:.2f} seconds"
    )

    print(
        f"RT-DETRv2 detection: "
        f"{detection_time:.2f} seconds"
    )

    print(
        f"Depth Pro loading:   "
        f"{depth_load_time:.2f} seconds"
    )

    print(
        f"Depth Pro inference: "
        f"{depth_inference_time:.2f} seconds"
    )

    print("-----------------------------------")

    print(
        f"TOTAL PROCESSING TIME: "
        f"{total_time:.2f} seconds"
    )

    print("===================================")

    return {
        "objects_detected": len(objects),
        "objects": objects
    }
