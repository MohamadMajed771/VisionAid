
from pathlib import Path
import threading
import time

import os

os.environ["FLAGS_use_mkldnn"] = "0"

from paddleocr import PaddleOCR

BASE_DIR = Path(__file__).resolve().parent
OCR_MODELS_DIR = BASE_DIR / "paddleocr-models"

_ocr_model = None
_ocr_load_lock = threading.Lock()


def load_ocr():
    """Load PaddleOCR once and reuse the model."""

    global _ocr_model

    with _ocr_load_lock:
        if _ocr_model is not None:
            print("Reusing cached PaddleOCR.")
            return _ocr_model

        print("\nLoading PaddleOCR...")
        start = time.perf_counter()

        detection_dir = OCR_MODELS_DIR / "detection"
        recognition_dir = OCR_MODELS_DIR / "recognition"

        for model_dir in (detection_dir, recognition_dir):
            if not (model_dir / "inference.yml").is_file():
                raise FileNotFoundError(
                    f"OCR model is missing inference.yml: {model_dir}"
                )

        _ocr_model = PaddleOCR(
            text_detection_model_name="PP-OCRv5_mobile_det",
            text_recognition_model_name="en_PP-OCRv5_mobile_rec",
            text_detection_model_dir=str(detection_dir),
            text_recognition_model_dir=str(recognition_dir),
            use_doc_orientation_classify=False,
            use_doc_unwarping=False,
            use_textline_orientation=False,
            enable_mkldnn=False,
            device="cpu"
        )

        elapsed = time.perf_counter() - start
        print(f"PaddleOCR loading time: {elapsed:.2f}s")

        return _ocr_model


def recognize_text(image_path):
    """Return recognized text, confidence and bounding boxes."""

    ocr = load_ocr()
    results = ocr.predict(str(image_path))

    texts = []

    for result in results:
        data = result.json
        data = data.get("res", data)

        recognized = data.get("rec_texts", [])
        scores = data.get("rec_scores", [])
        boxes = data.get("rec_boxes", [])

        for text, score, box in zip(recognized, scores, boxes):
            texts.append({
                "text": str(text),
                "confidence": round(float(score), 3),
                "bounding_box": [
                    int(value) for value in box
                ]
            })

    return {
        "texts_detected": len(texts),
        "texts": texts
    }

if __name__ == "__main__":
    import json

    image_path = input("Enter image path: ").strip().strip('"')

    if not Path(image_path).is_file():
        print("Error: Image not found:", image_path)
    else:
        result = recognize_text(image_path)

        print("\n========== OCR RESULTS ==========")
        print(json.dumps(result, indent=2))
        print("=================================")