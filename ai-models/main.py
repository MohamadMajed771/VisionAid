import io
import json
import zipfile

from PIL import Image

from fastapi.responses import StreamingResponse
import os
import tempfile
from fastapi import Form
import traceback

from pathlib import Path
from contextlib import asynccontextmanager

from fastapi import FastAPI, UploadFile, File, HTTPException
from fastapi.concurrency import run_in_threadpool

from vision_analyzer import (
    analyze_image,
    load_detector,
    load_unidepth
)

from whisper_transcriber import (
    load_whisper,
    transcribe_audio
)

from llama32_response_generator import (
    load_model as load_llama,
    generate_answer
)


# ------------------------------------
# Configuration
# ------------------------------------

MAX_IMAGE_SIZE = 10 * 1024 * 1024
MAX_AUDIO_SIZE = 20 * 1024 * 1024

ALLOWED_IMAGE_TYPES = {
    "image/jpeg": ".jpg",
    "image/png": ".png"
}

ALLOWED_AUDIO_TYPES = {
    "audio/wav": ".wav",
    "audio/x-wav": ".wav",
    "audio/wave": ".wav",
    "audio/mpeg": ".mp3",
    "audio/mp3": ".mp3",
    "audio/mp4": ".m4a",
    "audio/x-m4a": ".m4a",
    "audio/aac": ".aac",
    "audio/webm": ".webm",
    "audio/ogg": ".ogg"
}


# ------------------------------------
# Load AI models at startup
# ------------------------------------

@asynccontextmanager
async def lifespan(app: FastAPI):

    print("\n===================================")
    print("Starting VisionAid API...")
    print("Loading AI models...")
    print("===================================")

    # Load RT-DETRv2
    await run_in_threadpool(load_detector)

    # Load UniDepth V2
    await run_in_threadpool(load_unidepth)

    # Load Whisper
    await run_in_threadpool(load_whisper)

    # Preload Llama 3.2 through Ollama
    await run_in_threadpool(load_llama)

    
    # Warm up image analysis models automatically
    print("\nWarming up VisionAid image models...")

    warmup_path = None

    try:
        with tempfile.NamedTemporaryFile(
            suffix=".jpg",
            delete=False
        ) as temp_file:
            warmup_path = temp_file.name

        # Generate a temporary RGB image
        Image.new(
            "RGB",
            (224, 224),
            color=(128, 128, 128)
        ).save(warmup_path)

        await run_in_threadpool(
            analyze_image,
            warmup_path
        )

        print("VisionAid image models warmed up successfully!")

    finally:
        if warmup_path and os.path.exists(warmup_path):
            os.remove(warmup_path)

        # Warm up Llama 3.2
    print("\nWarming up Llama 3.2...")

    llm_result = await run_in_threadpool(
        generate_answer,
        "What is in front of me?",
        [
            {
                "label": "chair",
                "depth": 2.0
            }
        ],
        []
    )

    if llm_result is not None and llm_result.get("answer"):
        print("Llama 3.2 warmed up successfully!")
    else:
        print("WARNING: Llama 3.2 warm-up failed.")

    print("\n===================================")
    print("All VisionAid AI models are ready!")
    print("===================================")

    yield

    print("\nShutting down VisionAid API...")


app = FastAPI(
    title="VisionAid API",
    version="1.1.0",
    lifespan=lifespan
)


# ------------------------------------
# Home
# ------------------------------------

@app.get("/")
def home():
    return {
        "message": "Welcome to VisionAid API!"
    }


# ------------------------------------
# Health check
# ------------------------------------

@app.get("/health")
def health():
    return {
        "status": "ok",
        "service": "VisionAid Backend"
    }


# ------------------------------------
# Analyze image
# RT-DETRv2 + UniDepth + PaddleOCR
# ------------------------------------

@app.post("/analyze-image")
async def analyze_uploaded_image(
    file: UploadFile = File(...)
):

    if file.content_type not in ALLOWED_IMAGE_TYPES:
        raise HTTPException(
            status_code=400,
            detail="Only JPEG and PNG images are allowed."
        )

    image_bytes = await file.read(MAX_IMAGE_SIZE + 1)

    if len(image_bytes) > MAX_IMAGE_SIZE:
        raise HTTPException(
            status_code=413,
            detail="Image exceeds the 10 MB limit."
        )

    if not image_bytes:
        raise HTTPException(
            status_code=400,
            detail="Image file is empty."
        )

    suffix = ALLOWED_IMAGE_TYPES[file.content_type]

    temporary_path = None

    try:

        with tempfile.NamedTemporaryFile(
            delete=False,
            suffix=suffix
        ) as temp_file:

            temp_file.write(image_bytes)
            temporary_path = Path(temp_file.name)

        combined_result, _, _ = await run_in_threadpool(
            analyze_image,
            temporary_path
        )

        return combined_result

    except Exception as e:

        print("Image analysis error:", e)
        traceback.print_exc()

        raise HTTPException(
            status_code=500,
            detail="Image analysis failed."
        )

    finally:

        await file.close()

        if temporary_path is not None:
            try:
                os.remove(temporary_path)
            except FileNotFoundError:
                pass


@app.post("/analyze")
async def analyze_frame_for_backend(
    image: UploadFile = File(...),
    question: str | None = Form(None)
):
    # Validate image format
    if image.content_type not in ALLOWED_IMAGE_TYPES:
        raise HTTPException(
            status_code=400,
            detail="Only JPEG and PNG images are allowed."
        )

    image_bytes = await image.read(MAX_IMAGE_SIZE + 1)

    if len(image_bytes) > MAX_IMAGE_SIZE:
        raise HTTPException(
            status_code=413,
            detail="Image exceeds the 10 MB limit."
        )

    if not image_bytes:
        raise HTTPException(
            status_code=400,
            detail="Image file is empty."
        )

    suffix = ALLOWED_IMAGE_TYPES[image.content_type]
    temporary_path = None

    try:
        # Save uploaded image temporarily
        with tempfile.NamedTemporaryFile(
            delete=False,
            suffix=suffix
        ) as temp_file:
            temp_file.write(image_bytes)
            temporary_path = Path(temp_file.name)

        # Run object detection, depth estimation, OCR
        _, backend_result, annotated_image = (
            await run_in_threadpool(
                analyze_image,
                temporary_path
            )
        )

        # Add the user's question to the analysis JSON
        # Add the user's question to the analysis JSON
        backend_result["question"] = question
        backend_result["answer"] = None

        # Generate an answer only when a question is provided
        if question and question.strip():

            print("\nGenerating VisionAid answer with Llama 3.2...")

            llm_result = await run_in_threadpool(
                generate_answer,
                question.strip(),
                backend_result.get("detectedObjects", []),
                backend_result.get("ocrResults", [])
            )

            if llm_result is not None and llm_result.get("answer"):
                backend_result["answer"] = llm_result["answer"]

                print("Llama 3.2 answer:", backend_result["answer"])
                print(
                    "LLM processing time:",
                    llm_result["processing_time_seconds"],
                    "seconds"
                )
                print(
                    "Ollama internal duration:",
                    llm_result["ollama_total_seconds"],
                    "seconds"
                )
                print(
                    "LLM model loading:",
                    llm_result["model_load_seconds"],
                    "seconds"
                )

                print(
                    "LLM prompt processing:",
                    llm_result["prompt_processing_seconds"],
                    "seconds"
                )

                print(
                    "LLM generation:",
                    llm_result["generation_seconds"],
                    "seconds"
                )

                print(
                    "LLM prompt tokens:",
                    llm_result["prompt_tokens"]
                )

                print(
                    "LLM generated tokens:",
                    llm_result["total_tokens"]
                )
            else:
                print("WARNING: Llama 3.2 returned no usable answer.")

        # Preserve the original uploaded filename
        backend_result["image"] = (
            (image.filename or "image.jpg")
            .replace("\\", "/")
            .split("/")[-1]
        )

        # Create annotated image in memory
        image_buffer = io.BytesIO()
        annotated_image.save(
            image_buffer,
            format="JPEG",
            quality=90
        )

        # Create ZIP in memory
        zip_buffer = io.BytesIO()

        with zipfile.ZipFile(
            zip_buffer,
            mode="w",
            compression=zipfile.ZIP_DEFLATED
        ) as archive:

            archive.writestr(
                "analysis.json",
                json.dumps(
                    backend_result,
                    indent=2,
                    ensure_ascii=False
                )
            )

            archive.writestr(
                "annotated.jpg",
                image_buffer.getvalue()
            )

        zip_buffer.seek(0)

        # Return ZIP to ASP.NET Core
        return StreamingResponse(
            zip_buffer,
            media_type="application/zip",
            headers={
                "Content-Disposition":
                    'attachment; filename="analysis.zip"'
            }
        )

    except Exception as e:
        print("Backend analysis error:", e)
        traceback.print_exc()

        raise HTTPException(
            status_code=500,
            detail="Backend image analysis failed."
        )

    finally:
        await image.close()

        if temporary_path is not None:
            try:
                os.remove(temporary_path)
            except FileNotFoundError:
                pass


# ------------------------------------
# Transcribe audio
# Whisper Small
# ------------------------------------

@app.post("/transcribe-audio")
async def transcribe_uploaded_audio(
    file: UploadFile = File(...)
):

    # Validate audio format
    if file.content_type not in ALLOWED_AUDIO_TYPES:
        raise HTTPException(
            status_code=400,
            detail=(
                "Unsupported audio format. "
                "Use WAV, MP3, M4A, AAC, WebM or OGG."
            )
        )

    # Limit audio size to 20 MB
    audio_bytes = await file.read(MAX_AUDIO_SIZE + 1)

    if len(audio_bytes) > MAX_AUDIO_SIZE:
        raise HTTPException(
            status_code=413,
            detail="Audio exceeds the 20 MB limit."
        )

    if not audio_bytes:
        raise HTTPException(
            status_code=400,
            detail="Audio file is empty."
        )

    suffix = ALLOWED_AUDIO_TYPES[file.content_type]

    temporary_path = None

    try:

        # Save audio temporarily
        with tempfile.NamedTemporaryFile(
            delete=False,
            suffix=suffix
        ) as temp_file:

            temp_file.write(audio_bytes)
            temporary_path = Path(temp_file.name)

        print("\n===================================")
        print("Received audio for transcription")
        print("===================================")

        # Run Whisper in a worker thread
        result = await run_in_threadpool(
            transcribe_audio,
            temporary_path
        )

        question = result["transcription"]

        return {
            **result,
            "question": question
        }

    except Exception as e:

        print("Audio transcription error:", e)

        raise HTTPException(
            status_code=500,
            detail="Audio transcription failed."
        )

    finally:

        await file.close()

        # Delete temporary audio
        if temporary_path is not None:
            try:
                os.remove(temporary_path)
            except FileNotFoundError:
                pass
