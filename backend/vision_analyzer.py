
import gc
import inspect
import threading
import time

from contextlib import nullcontext
from dataclasses import replace
from pathlib import Path

import depth_pro
import numpy as np
import torch

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

GPU_DEVICE = torch.device(
    "cuda" if torch.cuda.is_available() else "cpu"
)

# Keep RT-DETRv2 on CPU to avoid GPU memory conflicts.
DETECTOR_DEVICE = torch.device("cpu")

# Use FP16 for Depth Pro when CUDA is available.
USE_DEPTH_FP16 = True

# Experimental GPU cache cleanup.
# Set to False if performance becomes worse.
CLEAR_UNUSED_GPU_CACHE = True


# ------------------------------------
# Cached AI models
# ------------------------------------

_depth_model = None
_depth_transform = None

_detector = None
_detector_processor = None

# Prevent simultaneous image analyses from
# competing for GPU memory.
_analysis_lock = threading.Lock()


# ------------------------------------
# GPU utility functions
# ------------------------------------

def synchronize_gpu():
    if GPU_DEVICE.type == "cuda":
        torch.cuda.synchronize()


def print_gpu_memory():
    """
    Display GPU memory usage inside the
    running VisionAid backend process.
    """

    if GPU_DEVICE.type != "cuda":
        print("CUDA is unavailable. Running on CPU.")
        return

    synchronize_gpu()

    allocated = torch.cuda.memory_allocated() / 1024**2
    reserved = torch.cuda.memory_reserved() / 1024**2

    free_memory, total_memory = torch.cuda.mem_get_info()

    free_memory /= 1024**2
    total_memory /= 1024**2

    print(f"GPU allocated memory: {allocated:.0f} MiB")
    print(f"GPU reserved memory:  {reserved:.0f} MiB")
    print(f"GPU free memory:      {free_memory:.0f} MiB")
    print(f"GPU total memory:     {total_memory:.0f} MiB")


def clear_unused_gpu_cache():
    """
    Release unused cached GPU allocations.

    This does not unload the cached AI models.
    """

    if GPU_DEVICE.type != "cuda":
        return

    if not CLEAR_UNUSED_GPU_CACHE:
        return

    print("\nClearing unused GPU cache...")

    synchronize_gpu()
    torch.cuda.empty_cache()

    print("GPU memory after cache cleanup:")
    print_gpu_memory()


# ------------------------------------
# Load RT-DETRv2
# ------------------------------------

def load_detector():
    """
    Load RT-DETRv2 only once.

    Subsequent calls reuse the cached detector.
    """

    global _detector, _detector_processor

    if _detector is not None:
        print("Reusing cached RT-DETRv2.")
        return _detector, _detector_processor, 0.0

    print("\nLoading RT-DETRv2 for the first time...")

    start = time.perf_counter()

    processor = RTDetrImageProcessor.from_pretrained(
        str(RTDETR_PATH)
    )

    detector = RTDetrV2ForObjectDetection.from_pretrained(
        str(RTDETR_PATH)
    ).to(DETECTOR_DEVICE)

    detector.eval()

    _detector_processor = processor
    _detector = detector

    elapsed = time.perf_counter() - start

    print(
        f"RT-DETRv2 loading time: "
        f"{elapsed:.2f} seconds"
    )

    return detector, processor, elapsed


# ------------------------------------
# Load Apple Depth Pro
# ------------------------------------

def load_depth_pro():
    """
    Load Apple Depth Pro only once.

    Keep the model in GPU memory for reuse.
    """

    global _depth_model, _depth_transform

    if _depth_model is not None:
        print("Reusing cached Apple Depth Pro.")
        return _depth_model, _depth_transform, 0.0

    print("\nLoading Apple Depth Pro for the first time...")

    start = time.perf_counter()

    default_config = inspect.signature(
        depth_pro.create_model_and_transforms
    ).parameters["config"].default

    config = replace(
        default_config,
        checkpoint_uri=str(DEPTH_CHECKPOINT)
    )

    use_fp16 = (
        USE_DEPTH_FP16
        and GPU_DEVICE.type == "cuda"
    )

    precision = (
        torch.float16
        if use_fp16
        else torch.float32
    )

    model, transform = depth_pro.create_model_and_transforms(
        config=config,
        device=GPU_DEVICE,
        precision=precision
    )

    model.eval()

    synchronize_gpu()

    _depth_model = model
    _depth_transform = transform

    elapsed = time.perf_counter() - start

    print(
        f"Depth Pro loading time: "
        f"{elapsed:.2f} seconds"
    )

    print("\nGPU memory after loading Depth Pro:")
    print_gpu_memory()

    return model, transform, elapsed


# ------------------------------------
# Analyze image
# ------------------------------------

def analyze_image(image_path):
    """
    Analyze an image using RT-DETRv2 and Depth Pro.

    Both models are reused from memory.
    """

    with _analysis_lock:
        return _analyze_image_locked(image_path)


def _analyze_image_locked(image_path):

    start_time = time.perf_counter()
    image_path = Path(image_path)

    print("\n===================================")
    print("VisionAid Image Analysis")
    print("===================================")

    print("Depth device:", GPU_DEVICE)
    print("Detector device:", DETECTOR_DEVICE)

    # ------------------------------------
    # 1. Read image
    # ------------------------------------

    with Image.open(image_path) as source_image:
        image = source_image.convert("RGB")

    # ------------------------------------
    # 2. Load or reuse RT-DETRv2
    # ------------------------------------

    detector, processor, detector_load_time = load_detector()

    # ------------------------------------
    # 3. Detect objects
    # ------------------------------------

    print("\nDetecting objects...")

    detection_start = time.perf_counter()

    inputs = processor(
        images=image,
        return_tensors="pt"
    ).to(DETECTOR_DEVICE)

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

    del inputs, outputs
    gc.collect()

    # ------------------------------------
    # 4. Load or reuse Depth Pro
    # ------------------------------------

    depth_model, transform, depth_load_time = load_depth_pro()

    # ------------------------------------
    # 5. Estimate depth
    # ------------------------------------

    print("\nEstimating depth...")

    depth_start = time.perf_counter()

    depth_image, _, f_px = depth_pro.load_rgb(
        str(image_path)
    )

    depth_input = transform(depth_image).to(
        GPU_DEVICE
    )

    use_autocast = (
        USE_DEPTH_FP16
        and GPU_DEVICE.type == "cuda"
    )

    precision_context = (
        torch.autocast(
            device_type="cuda",
            dtype=torch.float16
        )
        if use_autocast
        else nullcontext()
    )

    if GPU_DEVICE.type == "cuda":
        torch.cuda.reset_peak_memory_stats()

    with torch.inference_mode():
        with precision_context:
            prediction = depth_model.infer(
                depth_input,
                f_px=f_px
            )

    synchronize_gpu()

    depth_inference_time = (
        time.perf_counter() - depth_start
    )

    print(
        f"Depth Pro inference time: "
        f"{depth_inference_time:.2f} seconds"
    )

    if GPU_DEVICE.type == "cuda":
        peak_memory = (
            torch.cuda.max_memory_allocated()
            / 1024**2
        )

        print(
            f"Peak GPU memory during inference: "
            f"{peak_memory:.0f} MiB"
        )

    depth_map = (
        prediction["depth"]
        .detach()
        .float()
        .cpu()
        .numpy()
    )

    del depth_input, prediction

    # ------------------------------------
    # 6. Release unused GPU cache
    # ------------------------------------

    # Depth Pro remains loaded in GPU memory.
    # Only unused cached allocations are released.

    clear_unused_gpu_cache()

    # ------------------------------------
    # 7. Combine objects and distances
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
            0,
            min(depth_x, depth_width - 1)
        )

        depth_y = max(
            0,
            min(depth_y, depth_height - 1)
        )

        distance = float(
            depth_map[depth_y, depth_x]
        )

        if (
            not np.isfinite(distance)
            or distance <= 0
        ):
            distance = None
        else:
            distance = round(distance, 2)

        objects.append({
            "name": detector.config.id2label[int(label)],
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
    # 8. GPU memory measurements
    # ------------------------------------

    print("\n===================================")
    print("VisionAid GPU Memory Usage")
    print("===================================")

    print_gpu_memory()

    # ------------------------------------
    # 9. Performance results
    # ------------------------------------

    total_time = (
        time.perf_counter() - start_time
    )

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
