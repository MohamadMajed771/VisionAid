
import os
import tempfile

from pathlib import Path
from contextlib import asynccontextmanager

from fastapi import FastAPI, UploadFile, File, HTTPException
from fastapi.concurrency import run_in_threadpool

from backend.vision_analyzer import (
    analyze_image,
    load_detector,
    load_unidepth
)


# Load AI models automatically when FastAPI starts
@asynccontextmanager
async def lifespan(app: FastAPI):
    print("\n===================================")
    print("Starting VisionAid API...")
    print("Loading AI models...")
    print("===================================")

    # Run blocking model loading in a worker thread
    await run_in_threadpool(load_detector)
    await run_in_threadpool(load_unidepth)

    print("\n===================================")
    print("VisionAid AI models are ready!")
    print("===================================")

    yield

    print("\nShutting down VisionAid API...")


app = FastAPI(
    title="VisionAid API",
    version="1.0.0",
    lifespan=lifespan
)


@app.get("/")
def home():
    return {
        "message": "Welcome to VisionAid API!"
    }


@app.get("/health")
def health():
    return {
        "status": "ok",
        "service": "VisionAid Backend"
    }


@app.post("/analyze-image")
async def analyze_uploaded_image(
    file: UploadFile = File(...)
):
    # Validate image type
    allowed_types = [
        "image/jpeg",
        "image/png"
    ]

    if file.content_type not in allowed_types:
        raise HTTPException(
            status_code=400,
            detail="Only JPEG and PNG images are allowed."
        )

    # Limit uploaded image size to 10 MB
    image_bytes = await file.read(10 * 1024 * 1024 + 1)

    if len(image_bytes) > 10 * 1024 * 1024:
        raise HTTPException(
            status_code=413,
            detail="Image exceeds the 10 MB limit."
        )

    suffix = (
        ".png"
        if file.content_type == "image/png"
        else ".jpg"
    )

    temporary_path = None

    try:
        # Save image temporarily
        with tempfile.NamedTemporaryFile(
            delete=False,
            suffix=suffix
        ) as temp_file:
            temp_file.write(image_bytes)
            temporary_path = Path(temp_file.name)

        # Run AI models
        result = await run_in_threadpool(
            analyze_image,
            temporary_path
        )

        return result

    except Exception as e:
        print("Image analysis error:", e)

        raise HTTPException(
            status_code=500,
            detail="Image analysis failed."
        )

    finally:
        # Delete temporary image
        if temporary_path is not None:
            try:
                os.remove(temporary_path)
            except FileNotFoundError:
                pass
