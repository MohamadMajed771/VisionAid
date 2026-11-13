
import time
import threading
from pathlib import Path

import cv2
import numpy as np
import onnxruntime as ort
from PIL import Image, ImageDraw


# ------------------------------------
# Configuration
# ------------------------------------

BASE_DIR = Path(__file__).resolve().parent
MODEL_PATH = Path(__file__).resolve().parent / "face-models" / "scrfd_2.5g.onnx"

CONFIDENCE_THRESHOLD = 0.5
NMS_THRESHOLD = 0.4
INPUT_SIZE = 640
MASK_EXPANSION = 0.25

_session = None
_input_name = None
_session_lock = threading.Lock()


# ------------------------------------
# Load SCRFD once (CPU only)
# ------------------------------------

def load_face_detector():
    global _session, _input_name

    with _session_lock:
        if _session is not None:
            return _session

        if not MODEL_PATH.is_file():
            raise FileNotFoundError(
                f"SCRFD model not found: {MODEL_PATH}"
            )

        print("\nLoading SCRFD-2.5G face detector (CPU)...")
        start = time.perf_counter()

        session = ort.InferenceSession(
            str(MODEL_PATH),
            providers=["CPUExecutionProvider"]
        )

        input_name = session.get_inputs()[0].name

        # Warm up SCRFD with a dummy image.
        dummy = np.zeros(
            (1, 3, INPUT_SIZE, INPUT_SIZE),
            dtype=np.float32
        )
        session.run(None, {input_name: dummy})

        _input_name = input_name
        _session = session

        print(
            f"SCRFD loaded and warmed up in "
            f"{time.perf_counter() - start:.2f} seconds."
        )
        return _session


# ------------------------------------
# Image preprocessing
# ------------------------------------

def preprocess(image):
    height, width = image.shape[:2]

    scale = min(INPUT_SIZE / width, INPUT_SIZE / height)
    new_width = int(width * scale)
    new_height = int(height * scale)

    resized = cv2.resize(image, (new_width, new_height))

    canvas = np.zeros(
        (INPUT_SIZE, INPUT_SIZE, 3),
        dtype=np.uint8
    )
    canvas[:new_height, :new_width] = resized

    blob = cv2.dnn.blobFromImage(
        canvas,
        scalefactor=1.0 / 128.0,
        size=(INPUT_SIZE, INPUT_SIZE),
        mean=(127.5, 127.5, 127.5),
        swapRB=True
    )

    return blob, scale


# ------------------------------------
# Decode face detections
# ------------------------------------

def decode_predictions(outputs, scale, width, height):
    boxes = []
    scores = []

    for index, stride in enumerate([8, 16, 32]):
        score_output = outputs[index].reshape(-1)
        bbox_output = outputs[index + 3].reshape(-1, 4)

        feature_size = INPUT_SIZE // stride

        anchor_centers = np.stack(
            np.mgrid[:feature_size, :feature_size][::-1],
            axis=-1
        ).reshape(-1, 2)

        anchor_centers = (
            np.repeat(anchor_centers, 2, axis=0) * stride
        )

        valid_indices = np.where(
            score_output >= CONFIDENCE_THRESHOLD
        )[0]

        for idx in valid_indices:
            center_x, center_y = anchor_centers[idx]
            left, top, right, bottom = bbox_output[idx] * stride

            x1 = max(
                0, min(width - 1, int((center_x - left) / scale))
            )
            y1 = max(
                0, min(height - 1, int((center_y - top) / scale))
            )
            x2 = max(
                0, min(width, int((center_x + right) / scale))
            )
            y2 = max(
                0, min(height, int((center_y + bottom) / scale))
            )

            if x2 > x1 and y2 > y1:
                boxes.append([x1, y1, x2 - x1, y2 - y1])
                scores.append(float(score_output[idx]))

    if not boxes:
        return []

    indices = cv2.dnn.NMSBoxes(
        boxes,
        scores,
        CONFIDENCE_THRESHOLD,
        NMS_THRESHOLD
    )

    return [
        (boxes[idx], scores[idx])
        for idx in np.array(indices).flatten()
    ]


# ------------------------------------
# Detect faces from original image
# ------------------------------------

def detect_faces(image):
    session = load_face_detector()

    # PIL RGB -> OpenCV BGR
    image_bgr = cv2.cvtColor(
        np.asarray(image.convert("RGB")),
        cv2.COLOR_RGB2BGR
    )

    height, width = image_bgr.shape[:2]
    blob, scale = preprocess(image_bgr)

    start = time.perf_counter()

    outputs = session.run(
        None,
        {_input_name: blob}
    )

    detections = decode_predictions(
        outputs, scale, width, height
    )

    print(
        f"SCRFD detected {len(detections)} face(s) "
        f"in {time.perf_counter() - start:.3f} seconds."
    )

    return detections


# ------------------------------------
# Apply solid-black face masks
# ------------------------------------

def mask_faces(original_image, annotated_image):
    if original_image.size != annotated_image.size:
        raise ValueError(
            "Original and annotated image sizes do not match."
        )

    detections = detect_faces(original_image)

    masked_image = annotated_image.convert("RGB").copy()
    draw = ImageDraw.Draw(masked_image)

    for box, confidence in detections:
        x, y, w, h = box

        padding_x = w * MASK_EXPANSION
        padding_y = h * MASK_EXPANSION

        x1 = max(0, int(x - padding_x))
        y1 = max(0, int(y - padding_y))
        x2 = min(masked_image.width, int(x + w + padding_x))
        y2 = min(masked_image.height, int(y + h + padding_y))

        draw.rectangle(
            [x1, y1, x2, y2],
            fill="black"
        )

    return masked_image, len(detections)
