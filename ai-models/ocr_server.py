
from contextlib import asynccontextmanager
from pathlib import Path
import tempfile
import os

from fastapi import FastAPI, File, UploadFile, HTTPException
from fastapi.concurrency import run_in_threadpool

from paddleocr_recognizer import load_ocr, recognize_text


@asynccontextmanager
async def lifespan(app: FastAPI):
    print("Starting VisionAid PaddleOCR GPU server...")
    await run_in_threadpool(load_ocr)
    print("PaddleOCR GPU model ready!")
    yield
    print("Stopping PaddleOCR server...")


app = FastAPI(
    title="VisionAid OCR GPU Server",
    lifespan=lifespan
)


@app.get("/health")
def health():
    return {
        "status": "running",
        "service": "PaddleOCR",
        "device": "gpu:0"
    }


@app.post("/recognize-text")
async def recognize_text_endpoint(file: UploadFile = File(...)):
    suffix = Path(file.filename or "").suffix.lower()

    if suffix not in {".jpg", ".jpeg", ".png", ".webp"}:
        raise HTTPException(
            status_code=400,
            detail="Unsupported image format"
        )

    temp_path = None

    try:
        with tempfile.NamedTemporaryFile(
            delete=False,
            suffix=suffix
        ) as temp_file:
            temp_path = temp_file.name
            while chunk := await file.read(1024 * 1024):
                temp_file.write(chunk)

        result = await run_in_threadpool(
            recognize_text,
            temp_path
        )

        return result

    finally:
        await file.close()
        if temp_path and os.path.exists(temp_path):
            os.remove(temp_path)
