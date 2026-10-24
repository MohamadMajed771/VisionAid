
import gc
import inspect
import json
import sys
import time
from dataclasses import replace
from pathlib import Path

import numpy as np
import torch
import torchvision.transforms as T
from PIL import Image, ImageDraw


# ============================================================
# 1. PATHS AND SETTINGS
# ============================================================

PROJECT_ROOT = Path(__file__).resolve().parent.parent
AI_MODELS_DIR = PROJECT_ROOT / "ai-models"
OUTPUT_DIR = AI_MODELS_DIR / "benchmark_results_unidepth"

# Official RT-DETR repository and checkpoint
RTDETR_REPO = AI_MODELS_DIR / "rtdetr-source" / "rtdetrv2_pytorch"

RTDETR_CONFIG = (
    RTDETR_REPO
    / "configs"
    / "rtdetrv2"
    / "rtdetrv2_r18vd_120e_coco.yml"
)

RTDETR_CHECKPOINT = (
    AI_MODELS_DIR
    / "rtdetr-v2"
    / "rtdetrv2_r18vd_120e_coco_rerun_48.1.pth"
)

LABELS_CONFIG = AI_MODELS_DIR / "rtdetr-v2" / "config.json"

# UniDepth V2 Small repository and checkpoint
UNIDEPTH_REPO = AI_MODELS_DIR / "unidepth-source"

UNIDEPTH_CONFIG = (
    UNIDEPTH_REPO / "configs" / "config_v2_vits14.json"
)

UNIDEPTH_CHECKPOINT = (
    UNIDEPTH_REPO / "weights" / "unidepth_v2_small.safetensors"
)

CONFIDENCE_THRESHOLD = 0.5

DEVICE = torch.device(
    "cuda:0" if torch.cuda.is_available() else "cpu"
)

DEPTH_PRECISION = (
    torch.float16 if DEVICE.type == "cuda" else torch.float32
)

if DEVICE.type == "cuda":
    torch.backends.cudnn.benchmark = True
    torch.backends.cuda.matmul.allow_tf32 = True
    torch.backends.cudnn.allow_tf32 = True

OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

print(f"Device: {DEVICE}")

if DEVICE.type == "cuda":
    print(f"GPU: {torch.cuda.get_device_name(0)}")
    print(f"CUDA version: {torch.version.cuda}")
    print("UniDepth precision: FP32")
else:
    print("WARNING: CUDA is unavailable; running on CPU.")


def synchronize_gpu():
    """Synchronize CUDA before and after timing inference."""
    if DEVICE.type == "cuda":
        torch.cuda.synchronize()


# ============================================================
# 2. VERIFY REQUIRED FILES
# ============================================================

required_paths = [
    RTDETR_REPO,
    RTDETR_CONFIG,
    RTDETR_CHECKPOINT,
    LABELS_CONFIG,
    UNIDEPTH_REPO,
    UNIDEPTH_CONFIG,
    UNIDEPTH_CHECKPOINT,
]

for path in required_paths:
    if not path.exists():
        raise FileNotFoundError(
            f"Required path not found: {path}"
        )

print("All required model paths exist.")


# ============================================================
# 3. LOAD OBJECT LABELS
# ============================================================

with open(LABELS_CONFIG, "r", encoding="utf-8") as file:
    label_config = json.load(file)

label_map = {
    int(class_id): class_name
    for class_id, class_name in label_config["id2label"].items()
}


# ============================================================
# 4. LOAD RT-DETRv2 ONCE
# ============================================================

sys.path.insert(0, str(RTDETR_REPO))

from src.core import YAMLConfig

print("\nLoading RT-DETRv2...")

detector_load_start = time.perf_counter()

cfg = YAMLConfig(str(RTDETR_CONFIG))

checkpoint = torch.load(
    RTDETR_CHECKPOINT,
    map_location="cpu",
    weights_only=True,
)

if "ema" in checkpoint:
    state_dict = checkpoint["ema"]["module"]
elif "model" in checkpoint:
    state_dict = checkpoint["model"]
else:
    raise KeyError(
        "RT-DETRv2 checkpoint has no 'ema' or 'model' weights."
    )

cfg.model.load_state_dict(state_dict)

detector = cfg.model.deploy().to(DEVICE)
detector.eval()

postprocessor = cfg.postprocessor.deploy()

del checkpoint, state_dict
gc.collect()

synchronize_gpu()

detector_load_seconds = (
    time.perf_counter() - detector_load_start
)

print(
    f"RT-DETRv2 loaded in {detector_load_seconds:.3f} seconds."
)



# ============================================================
# 5. LOAD UNIDEPTH V2 SMALL ONCE
# ============================================================

from safetensors.torch import load_file

sys.path.insert(0, str(UNIDEPTH_REPO))

from unidepth.models import UniDepthV2

print("\nLoading UniDepth V2 Small...")

depth_load_start = time.perf_counter()

# Read model configuration
with open(UNIDEPTH_CONFIG, "r", encoding="utf-8") as file:
    depth_config = json.load(file)

# Create model
depth_model = UniDepthV2(depth_config)

# Load downloaded checkpoint
depth_weights = load_file(
    str(UNIDEPTH_CHECKPOINT),
    device="cpu"
)

load_result = depth_model.load_state_dict(
    depth_weights,
    strict=True
)

del depth_weights
gc.collect()

# Move model to GPU
depth_model = depth_model.to(DEVICE)
depth_model.eval()

actual_dtype = next(depth_model.parameters()).dtype
actual_device = next(depth_model.parameters()).device

synchronize_gpu()

depth_load_seconds = time.perf_counter() - depth_load_start

print(
    f"UniDepth V2 Small loaded in {depth_load_seconds:.3f} seconds."
)
print(f"UniDepth dtype: {actual_dtype}")
print(f"UniDepth device: {actual_device}")

print("\nBoth models are loaded and ready.")



# ============================================================
# 6. IMAGE TRANSFORM
# ============================================================

detector_transform = T.Compose([
    T.Resize((640, 640)),
    T.ToTensor(),
])


# ============================================================
# 7. ESTIMATE OBJECT DEPTH
# ============================================================

def estimate_object_depth(box, depth_array, image_size):
    """
    Estimate object depth using the median positive depth
    in the central region of the detection bounding box.

    This does not independently verify metric accuracy.
    """
    x1, y1, x2, y2 = box

    image_width, image_height = image_size
    depth_height, depth_width = depth_array.shape

    left = max(
        0,
        min(
            depth_width - 1,
            int(np.floor(x1 * depth_width / image_width)),
        ),
    )

    right = max(
        left + 1,
        min(
            depth_width,
            int(np.ceil(x2 * depth_width / image_width)),
        ),
    )

    top = max(
        0,
        min(
            depth_height - 1,
            int(np.floor(y1 * depth_height / image_height)),
        ),
    )

    bottom = max(
        top + 1,
        min(
            depth_height,
            int(np.ceil(y2 * depth_height / image_height)),
        ),
    )

    region = depth_array[top:bottom, left:right]

    if region.size == 0:
        return None

    height, width = region.shape

    inner = region[
        height // 4 : max(
            height // 4 + 1,
            (3 * height) // 4,
        ),
        width // 4 : max(
            width // 4 + 1,
            (3 * width) // 4,
        ),
    ]

    valid = inner[
        np.isfinite(inner) & (inner > 0)
    ]

    if valid.size == 0:
        return None

    return float(np.median(valid))


# ============================================================
# 8. WARM UP BOTH MODELS
# ============================================================

def warm_up_models():
    
    print("Warming up UniDepth V2 Small...")

    # Dummy RGB image: 3 channels, height 480, width 640
    dummy_depth_input = torch.zeros(
        (3, 480, 640),
        device=DEVICE,
        dtype=torch.uint8,
    )

    with torch.inference_mode():
        _ = depth_model.infer(dummy_depth_input)

    synchronize_gpu()

    del dummy_depth_input
    gc.collect()


    print("Warm-up complete.\n")


# ============================================================
# 9. PROCESS ONE IMAGE
# ============================================================

def process_image(image_path):
    image_path = Path(image_path).expanduser()

    if not image_path.is_file():
        print(f"Image not found: {image_path}\n")
        return

    total_start = time.perf_counter()

    # Load image
    with Image.open(image_path) as source:
        image = source.convert("RGB")

    original_width, original_height = image.size

    # --------------------------------------------------------
    # RT-DETRv2 preprocessing
    # --------------------------------------------------------

    preprocess_start = time.perf_counter()

    image_tensor = (
        detector_transform(image)
        .unsqueeze(0)
        .to(DEVICE)
    )

    original_size = torch.tensor(
        [[original_width, original_height]],
        device=DEVICE,
    )

    detector_preprocess_seconds = (
        time.perf_counter() - preprocess_start
    )

    # --------------------------------------------------------
    # RT-DETRv2 inference
    # --------------------------------------------------------

    synchronize_gpu()
    detector_start = time.perf_counter()

    with torch.inference_mode():
        outputs = detector(image_tensor)

        labels, boxes, scores = postprocessor(
            outputs,
            original_size,
        )

    synchronize_gpu()

    detector_seconds = (
        time.perf_counter() - detector_start
    )

    labels = labels[0].detach().cpu()
    boxes = boxes[0].detach().cpu()
    scores = scores[0].detach().cpu()

    del outputs, image_tensor, original_size

    detections = []

    for label, box, score in zip(labels, boxes, scores):
        confidence = float(score.item())

        if confidence < CONFIDENCE_THRESHOLD:
            continue

        class_id = int(label.item())

        x1, y1, x2, y2 = [
            float(value) for value in box.tolist()
        ]

        detections.append({
            "name": label_map.get(
                class_id,
                f"class_{class_id}",
            ),
            "class_id": class_id,
            "confidence": confidence,
            "box": (x1, y1, x2, y2),
        })

    # --------------------------------------------------------
    # UniDepth V2 preprocessing
    # --------------------------------------------------------

    depth_preprocess_start = time.perf_counter()

    depth_input = torch.from_numpy(
    np.array(image)
).permute(2, 0, 1).contiguous().to(DEVICE)

    depth_preprocess_seconds = (
        time.perf_counter() - depth_preprocess_start
    )

    
    # --------------------------------------------------------
    # UniDepth V2 preprocessing
    # --------------------------------------------------------

    depth_preprocess_start = time.perf_counter()

    # Convert the original RGB image to a tensor
    depth_input = torch.from_numpy(
        np.array(image)
    ).permute(2, 0, 1).contiguous().to(DEVICE)

    synchronize_gpu()

    depth_preprocess_seconds = (
        time.perf_counter() - depth_preprocess_start
    )

    # --------------------------------------------------------
    # UniDepth V2 inference
    # --------------------------------------------------------

    synchronize_gpu()
    depth_start = time.perf_counter()

    with torch.inference_mode():
        prediction = depth_model.infer(depth_input)

    synchronize_gpu()

    depth_seconds = time.perf_counter() - depth_start

    # Convert depth prediction to a 2D NumPy array
    depth_map = (
        prediction["depth"]
        .detach()
        .float()
        .cpu()
        .numpy()
    )

    depth_map = np.squeeze(depth_map)

    if depth_map.ndim != 2:
        raise ValueError(
            f"Expected 2D depth map, got {depth_map.shape}"
        )


    # --------------------------------------------------------
    # Draw detections and prepare JSON
    # --------------------------------------------------------

    draw = ImageDraw.Draw(image)
    objects = []

    for item in detections:
        x1, y1, x2, y2 = item["box"]

        depth_estimate = estimate_object_depth(
            item["box"],
            depth_map,
            (original_width, original_height),
        )

        draw.rectangle(
            (x1, y1, x2, y2),
            outline="red",
            width=3,
        )

        draw.text(
            (x1, max(0, y1 - 18)),
            f'{item["name"]}: {item["confidence"]:.2f}',
            fill="red",
        )

        objects.append({
            "name": item["name"],
            "class_id": item["class_id"],
            "confidence": round(
                item["confidence"], 4
            ),
            "bounding_box": {
                "x1": round(x1, 2),
                "y1": round(y1, 2),
                "x2": round(x2, 2),
                "y2": round(y2, 2),
            },
            "depth_estimate": (
                round(depth_estimate, 3)
                if depth_estimate is not None
                else None
            ),
            "depth_unit": None,
        })

    output_image = (
        OUTPUT_DIR / f"{image_path.stem}_result.jpg"
    )

    output_json = (
        OUTPUT_DIR / f"{image_path.stem}_result.json"
    )

    result = {
        "image": image_path.name,
        "image_width": original_width,
        "image_height": original_height,
        "detection_model": "RT-DETRv2-R18",
        "depth_model": "UniDepth V2 Small",
        "depth_precision": str(actual_dtype),
        "objects": objects,
        "timings": {
            "rtdetr_load_seconds": round(
                detector_load_seconds, 3
            ),
            "unidepth_load_seconds": round(
                depth_load_seconds, 3
            ),
            "rtdetr_preprocessing_seconds": round(
                detector_preprocess_seconds, 3
            ),
            "rtdetr_inference_seconds": round(
                detector_seconds, 3
            ),
            "depth_preprocessing_seconds": round(
                depth_preprocess_seconds, 3
            ),
            "depth_inference_seconds": round(
                depth_seconds, 3
            ),
        },
    }

    with open(output_json, "w", encoding="utf-8") as file:
        json.dump(
            result,
            file,
            indent=2,
            ensure_ascii=False,
        )

    image.save(output_image)

    total_seconds = time.perf_counter() - total_start

    result["timings"]["total_processing_seconds"] = round(
        total_seconds, 3
    )

    # Update JSON with total processing time
    with open(output_json, "w", encoding="utf-8") as file:
        json.dump(
            result,
            file,
            indent=2,
            ensure_ascii=False,
        )

    print(f"\nImage: {image_path.name}")
    print(f"Detections: {len(objects)}")
    print(
        f"RT-DETRv2 preprocessing: "
        f"{detector_preprocess_seconds:.3f} s"
    )
    print(
        f"RT-DETRv2 inference:     "
        f"{detector_seconds:.3f} s"
    )
    print(
        f"UniDepth preprocessing: "
        f"{depth_preprocess_seconds:.3f} s"
    )
    print(
        f"UniDepth inference:     "
        f"{depth_seconds:.3f} s"
    )
    print(
        f"Total processing:        "
        f"{total_seconds:.3f} s"
    )
    print(f"Annotated image: {output_image}")
    print(f"JSON: {output_json}\n")

    del depth_input, prediction, depth_map
    gc.collect()


# ============================================================
# 10. INTERACTIVE BENCHMARK LOOP
# ============================================================

try:
    warm_up_models()

    while True:
        try:
            image_input = input(
                'Enter full image path (or "q" to quit): '
            ).strip().strip('"')

            if image_input.lower() in {
                "q", "quit", "exit"
            }:
                break

            if not image_input:
                continue

            process_image(image_input)

        except (KeyboardInterrupt, EOFError):
            print()
            break

        except Exception as error:
            print(
                f"Could not process image: {error}\n"
            )

finally:
    del depth_model, detector
    gc.collect()

    if DEVICE.type == "cuda":
        torch.cuda.empty_cache()

print("Benchmark stopped; models unloaded.")
