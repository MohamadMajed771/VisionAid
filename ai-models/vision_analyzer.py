
import gc
import threading
import time
import sys
import json

from pathlib import Path

import numpy as np
import torch

from safetensors.torch import load_file
from PIL import Image, ImageDraw
from transformers import (
    RTDetrImageProcessor,
    RTDetrV2ForObjectDetection
)

# Communicate with the separate PaddleOCR GPU server.
import requests

OCR_SERVER_URL = "http://127.0.0.1:8001/recognize-text"


# ------------------------------------
# 1. Project configuration
# ------------------------------------

BASE_DIR = Path(__file__).resolve().parent

RTDETR_PATH = BASE_DIR / "rtdetr-v2"

UNIDEPTH_REPO = BASE_DIR / "unidepth-source"

UNIDEPTH_CONFIG = (
    UNIDEPTH_REPO / "configs" / "config_v2_vits14.json"
)

DEPTH_CHECKPOINT = (
    UNIDEPTH_REPO / "weights" / "unidepth_v2_small.safetensors"
)

GPU_DEVICE = torch.device(
    "cuda" if torch.cuda.is_available() else "cpu"
)

# Run RT-DETRv2 on the NVIDIA GPU when CUDA is available.
DETECTOR_DEVICE = torch.device(
    "cuda:0" if torch.cuda.is_available() else "cpu"
)

# Experimental GPU cache cleanup.
CLEAR_UNUSED_GPU_CACHE = True

DETECTION_THRESHOLD = 0.5


# ------------------------------------
# 2. Cached AI models
# ------------------------------------

_depth_model = None

_detector = None
_detector_processor = None

# Prevent simultaneous analyses from competing
# for GPU memory or shared model resources.
_analysis_lock = threading.Lock()


# ------------------------------------
# 3. GPU utility functions
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
# 4. Load RT-DETRv2
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
# 5. Load UniDepth V2 Small
# ------------------------------------

def load_unidepth():
    """
    Load UniDepth V2 Small only once.

    Keep the model in GPU memory for reuse.
    """

    global _depth_model

    if _depth_model is not None:
        print("Reusing cached UniDepth V2 Small.")
        return _depth_model, None, 0.0

    print("\nLoading UniDepth V2 Small for the first time...")

    start = time.perf_counter()

    # Import UniDepth from the local repository.
    if str(UNIDEPTH_REPO) not in sys.path:
        sys.path.insert(0, str(UNIDEPTH_REPO))

    from unidepth.models import UniDepthV2

    # Read model configuration.
    with open(UNIDEPTH_CONFIG, "r", encoding="utf-8") as f:
        config = json.load(f)

    # Create model.
    model = UniDepthV2(config)

    # Load pretrained weights.
    weights = load_file(
        str(DEPTH_CHECKPOINT),
        device="cpu"
    )

    model.load_state_dict(weights, strict=True)

    # Move model to GPU if available.
    model = model.to(GPU_DEVICE).eval()

    synchronize_gpu()

    _depth_model = model

    elapsed = time.perf_counter() - start

    print(
        f"UniDepth V2 Small loading time: "
        f"{elapsed:.2f} seconds"
    )

    print("\nGPU memory after loading UniDepth:")
    print_gpu_memory()

    return model, None, elapsed


# ------------------------------------
# 6. Analyze image
# ------------------------------------

def analyze_image(image_path):
    """
    Analyze an image using three AI models:

    1. RT-DETRv2: Object detection
    2. UniDepth V2: Distance estimation
    3. PaddleOCR: Text recognition

    Models are reused from memory.

    Return three results:
    1. Original JSON response
    2. ASP.NET Core compatible JSON response
    3. Annotated PIL image
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
    print("OCR device: GPU (separate OCR server)")
    # ------------------------------------
    # 7. Read image
    # ------------------------------------

    with Image.open(image_path) as source_image:
        image = source_image.convert("RGB")

    print(f"Image size: {image.width} x {image.height}")

    # ------------------------------------
    # 8. Load or reuse RT-DETRv2
    # ------------------------------------

    detector, processor, detector_load_time = load_detector()

    # ------------------------------------
    # 9. Detect objects
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
        threshold=DETECTION_THRESHOLD,
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

    del inputs, outputs
    gc.collect()

    # ------------------------------------
    # 10. Load or reuse UniDepth
    # ------------------------------------

    depth_model, _, depth_load_time = load_unidepth()

    # ------------------------------------
    # 11. Estimate depth
    # ------------------------------------

    print("\nEstimating depth...")

    depth_start = time.perf_counter()

    # Convert RGB image to tensor.
    depth_input = torch.from_numpy(
        np.array(image).copy()
    ).permute(2, 0, 1).contiguous().to(GPU_DEVICE)

    if GPU_DEVICE.type == "cuda":
        torch.cuda.reset_peak_memory_stats()

    # UniDepth V2 Small inference in FP32.
    with torch.inference_mode():
        prediction = depth_model.infer(depth_input)

    synchronize_gpu()

    depth_inference_time = (
        time.perf_counter() - depth_start
    )

    print(
        f"UniDepth V2 Small inference time: "
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
        .squeeze()
        .detach()
        .float()
        .cpu()
        .numpy()
    )

    del depth_input, prediction

    # ------------------------------------
    # 12. Release unused GPU cache
    # ------------------------------------

    # UniDepth remains loaded in GPU memory.
    # Only unused cached allocations are released.

    clear_unused_gpu_cache()

    # ------------------------------------
    # 13. Combine objects and distances
    # ------------------------------------

    print("\nCombining objects and estimated distances...")

    depth_height, depth_width = depth_map.shape

    objects = []
    annotated_image = image.copy()
    draw = ImageDraw.Draw(annotated_image)

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

        object_data = {
            "name": detector.config.id2label[int(label)],
            "confidence": round(float(score), 2),
            "estimated_distance_m": distance,
            "bounding_box": [
                round(x1, 2),
                round(y1, 2),
                round(x2, 2),
                round(y2, 2)
            ]
        }

        objects.append(object_data)

        draw.rectangle(
            [x1, y1, x2, y2],
            outline="red",
            width=3
        )

        draw.text(
            (x1, max(0, y1 - 18)),
            f"{object_data['name']}: {object_data['confidence']:.2f}",
            fill="red"
        )

        print(
            f"Object: {object_data['name']} | "
            f"Confidence: {object_data['confidence']} | "
            f"Distance: {object_data['estimated_distance_m']} m"
        )

    # ------------------------------------
    # 14. PaddleOCR text recognition
    # ------------------------------------

    print("\n===================================")
    print("PaddleOCR Text Recognition")
    print("===================================")

    ocr_start = time.perf_counter()

    # Send the image to the separate PaddleOCR GPU server.
    # PaddleOCR stays loaded in the OCR worker process.

    try:
        with open(image_path, "rb") as image_file:
            response = requests.post(
                OCR_SERVER_URL,
                files={
                    "file": (image_path.name, image_file)
                },
                timeout=60
            )

        response.raise_for_status()
        ocr_result = response.json()

    except requests.RequestException as e:
        raise RuntimeError(
            f"PaddleOCR GPU server request failed: {e}"
        ) from e

    ocr_time = time.perf_counter() - ocr_start

    texts = ocr_result["texts"]

    print(
        f"PaddleOCR inference time: "
        f"{ocr_time:.2f} seconds"
    )

    print("Texts detected:", len(texts))

    for text_data in texts:
        print(
            f"Text: {text_data['text']} | "
            f"Confidence: {text_data['confidence']}"
        )

    # ------------------------------------
    # 15. GPU memory measurements
    # ------------------------------------

    print("\n===================================")
    print("VisionAid GPU Memory Usage")
    print("===================================")

    print_gpu_memory()

    # ------------------------------------
    # 16. Performance results
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
        f"UniDepth loading:    "
        f"{depth_load_time:.2f} seconds"
    )

    print(
        f"UniDepth inference:  "
        f"{depth_inference_time:.2f} seconds"
    )

    print(
        f"PaddleOCR inference: "
        f"{ocr_time:.2f} seconds"
    )

    print("-----------------------------------")

    print(
        f"TOTAL PROCESSING TIME: "
        f"{total_time:.2f} seconds"
    )

    print("===================================")

    # ------------------------------------
    # 17. Combined JSON response
    # ------------------------------------

    combined_result = {
        "objects_detected": len(objects),
        "objects": objects,
        "texts_detected": len(texts),
        "texts": texts
    }
    backend_result = {
    "image": image_path.name,
    "imageWidth": image.width,
    "imageHeight": image.height,
    "depthModel": "UniDepth V2 Small",
    "depthPrecision": "torch.float32",
    "detectedObjects": [
        {
            "label": obj["name"],
            "confidence": obj["confidence"],
            "depth": obj["estimated_distance_m"],
            "depthUnit": (
                "meters"
                if obj["estimated_distance_m"] is not None
                else None
            ),
            "x1": obj["bounding_box"][0],
            "y1": obj["bounding_box"][1],
            "x2": obj["bounding_box"][2],
            "y2": obj["bounding_box"][3]
        }
        for obj in objects
    ],
    "ocrResults": [
        {
            "extractedText": item["text"],
            "confidence": item["confidence"],
            "boundingBox": item.get("bounding_box")
        }
        for item in texts
    ],
    "question": None,
    "answer": None
}

    print("\nCombined analysis completed successfully!")

    return combined_result, backend_result, annotated_image
