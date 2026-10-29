
from pathlib import Path
import sys

import torch
from PIL import Image, ImageDraw

# --------------------------------------------------
# 1. Paths
# --------------------------------------------------

PROJECT_ROOT = Path(__file__).resolve().parent.parent
AI_MODELS_DIR = PROJECT_ROOT / "ai-models"

RTDETR_REPO = Path(
    r"C:\Users\HP\Desktop\RT-DETR-main\rtdetrv2_pytorch"
)

RTDETR_CONFIG = (
    RTDETR_REPO
    / "configs"
    / "rtdetrv2"
    / "rtdetrv2_r18vd_120e_coco.yml"
)

RTDETR_CHECKPOINT = Path(
    r"C:\Users\HP\Desktop\RT-DETR-main"
    r"\rtdetrv2_r18vd_120e_coco_rerun_48.1.pth"
)

IMAGE_PATH = Path.home() / "Downloads" / "test.jpg"
OUTPUT_PATH = AI_MODELS_DIR / "result.jpg"

CONFIDENCE_THRESHOLD = 0.5
DEVICE = "cuda:0" if torch.cuda.is_available() else "cpu"

# --------------------------------------------------
# 2. Verify paths
# --------------------------------------------------

for name, path in {
    "RT-DETR repository": RTDETR_REPO,
    "Configuration": RTDETR_CONFIG,
    "Checkpoint": RTDETR_CHECKPOINT,
    "Input image": IMAGE_PATH,
}.items():
    if not path.exists():
        raise FileNotFoundError(f"{name} not found: {path}")

# --------------------------------------------------
# 3. Load the original RT-DETRv2 model
# --------------------------------------------------

# The original repository supplies the correct model
# architecture and checkpoint-loading implementation.
sys.path.insert(0, str(RTDETR_REPO))

from src.core import YAMLConfig

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
        "No 'ema' or 'model' weights found in checkpoint."
    )

cfg.model.load_state_dict(state_dict)

# Match the repository's inference/deployment path.
model = cfg.model.deploy().to(DEVICE)
model.eval()

postprocessor = cfg.postprocessor.deploy()

# --------------------------------------------------
# 4. Prepare the image
# --------------------------------------------------

image = Image.open(IMAGE_PATH).convert("RGB")
original_width, original_height = image.size

import torchvision.transforms as T

transform = T.Compose([
    T.Resize((640, 640)),
    T.ToTensor(),
])

image_tensor = transform(image).unsqueeze(0).to(DEVICE)
original_size = torch.tensor(
    [[original_width, original_height]],
    device=DEVICE,
)

# --------------------------------------------------
# 5. Run object detection
# --------------------------------------------------

with torch.inference_mode():
    outputs = model(image_tensor)
    labels, boxes, scores = postprocessor(
        outputs, original_size
    )

labels = labels[0]
boxes = boxes[0]
scores = scores[0]

# --------------------------------------------------
# 6. Draw and print detections
# --------------------------------------------------

draw = ImageDraw.Draw(image)
label_names = cfg.yaml_cfg.get("label_names", None)

for label, box, score in zip(labels, boxes, scores):
    confidence = score.item()

    if confidence < CONFIDENCE_THRESHOLD:
        continue

    class_id = label.item()

    # This checkpoint is trained on COCO's 80 classes.
    # COCO IDs here are zero-based in the model output.
    name = (
        label_names[class_id]
        if label_names and class_id < len(label_names)
        else f"class_{class_id}"
    )

    x1, y1, x2, y2 = box.tolist()

    draw.rectangle(
        (x1, y1, x2, y2),
        outline="red",
        width=3,
    )
    draw.text(
        (x1, max(0, y1 - 15)),
        f"{name}: {confidence:.2f}",
        fill="red",
    )

    print(f"Detected: {name} | Confidence: {confidence:.2f}")

# --------------------------------------------------
# 7. Save the result
# --------------------------------------------------

OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
image.save(OUTPUT_PATH)

print(f"\nDetection completed. Result saved to: {OUTPUT_PATH}")
print(f"Device: {DEVICE}")
