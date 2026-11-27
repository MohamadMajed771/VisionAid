import io
import json
import os
import tempfile
import traceback
import zipfile

from pathlib import Path
from contextlib import asynccontextmanager

from PIL import Image

from fastapi import FastAPI, UploadFile, File, Form, HTTPException
from fastapi.concurrency import run_in_threadpool
from fastapi.responses import StreamingResponse, Response

from alert_generator import (
    generate_automatic_alert,
    prepare_alert_evidence
)

from vision_analyzer import (
    analyze_image,
    load_detector,
    load_unidepth,
    check_image_quality
)

from whisper_transcriber import (
    load_whisper,
    transcribe_audio
)

from llama32_response_generator import (
    load_model as load_llama,
    generate_answer,
    generate_alert
)

from scrfd_face_detector import (
    load_face_detector,
    mask_faces
)


# ------------------------------------
# Configuration
# ------------------------------------

MAX_IMAGE_SIZE = 10 * 1024 * 1024
MAX_AUDIO_SIZE = 20 * 1024 * 1024

SUPPORTED_LANGUAGES = {"en", "fr"}

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

    # Load SCRFD-2.5G face detector on CPU
    print("\nLoading SCRFD-2.5G face detector...")
    await run_in_threadpool(load_face_detector)

    # Load Whisper
    await run_in_threadpool(load_whisper)

    # Preload Llama 3.2 through Ollama
    await run_in_threadpool(load_llama)

    # Warm up image analysis models
    print("\nWarming up VisionAid image models...")

    warmup_path = None

    try:
        with tempfile.NamedTemporaryFile(
            suffix=".jpg",
            delete=False
        ) as temp_file:
            warmup_path = temp_file.name

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
    version="1.2.0",
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



# ------------------------------------
# Analyze image + optional audio
# RT-DETRv2 + UniDepth + PaddleOCR
# Whisper + Automatic Alerts + Llama 3.2
# Image Quality + Reliability + SCRFD
# ------------------------------------

@app.post("/analyze")
async def analyze_frame_for_backend(
    image: UploadFile = File(...),
    audio: UploadFile | None = File(None),
    question: str | None = Form(None),
    language: str = Form("en")
):

    temporary_path = None
    temporary_audio_path = None
    transcription_result = None

    try:

        # ------------------------------------
        # Validate selected language
        # ------------------------------------

        language = language.strip().lower()

        if language not in SUPPORTED_LANGUAGES:
            raise HTTPException(
                status_code=400,
                detail="Supported languages are 'en' and 'fr'."
            )

        # ------------------------------------
        # Validate image
        # ------------------------------------

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

        # ------------------------------------
        # Validate optional audio
        # ------------------------------------

        audio_bytes = None
        audio_suffix = None

        if audio is not None:

            if audio.content_type not in ALLOWED_AUDIO_TYPES:
                raise HTTPException(
                    status_code=400,
                    detail=(
                        "Unsupported audio format. "
                        "Use WAV, MP3, M4A, AAC, WebM or OGG."
                    )
                )

            audio_bytes = await audio.read(MAX_AUDIO_SIZE + 1)

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

            audio_suffix = ALLOWED_AUDIO_TYPES[
                audio.content_type
            ]

        suffix = ALLOWED_IMAGE_TYPES[image.content_type]

        # ------------------------------------
        # Save uploaded image temporarily
        # ------------------------------------

        with tempfile.NamedTemporaryFile(
            delete=False,
            suffix=suffix
        ) as temp_file:

            temporary_path = Path(temp_file.name)
            temp_file.write(image_bytes)

        # ------------------------------------
        # Transcribe audio with Whisper
        # ------------------------------------

        if audio_bytes is not None:

            with tempfile.NamedTemporaryFile(
                delete=False,
                suffix=audio_suffix
            ) as temp_audio_file:

                temporary_audio_path = Path(
                    temp_audio_file.name
                )

                temp_audio_file.write(audio_bytes)

            print("\n===================================")
            print(f"Transcribing audio ({language})")
            print("===================================")

            transcription_result = await run_in_threadpool(
                transcribe_audio,
                temporary_audio_path,
                language
            )

            transcribed_question = (
                transcription_result["transcription"].strip()
            )

            # Use the transcribed question
            if transcribed_question:
                question = transcribed_question

            # If no speech was recognized and no written
            # question exists, ask the client to retry
            elif not question or not question.strip():
                raise HTTPException(
                    status_code=422,
                    detail=(
                        "No speech was recognized. "
                        "Please record your question again."
                    )
                )

            print("Whisper transcription:", transcribed_question)

            print(
                "Whisper processing time:",
                transcription_result.get(
                    "processing_time_seconds"
                ),
                "seconds"
            )

        # ------------------------------------
        # Check whether the user asked a question
        # ------------------------------------

        has_question = bool(
            question and question.strip()
        )

        # ------------------------------------
        # Image quality check
        # ------------------------------------

        image_quality = await run_in_threadpool(
            check_image_quality,
            temporary_path
        )

        is_blurry = image_quality["is_blurry"]

        # Discard blurry frames without a question
        if is_blurry and not has_question:

            print("\nBlurry image without a question.")
            print("Discarding frame. Returning HTTP 204.")

            return Response(status_code=204)

        if is_blurry and has_question:

            print("\nBlurry image with a user question.")
            print("Continuing analysis with uncertainty.")

        # ------------------------------------
        # Run object detection, depth and OCR
        # ------------------------------------

        print("\n===================================")
        print("VisionAid Image Analysis")
        print("===================================")

        combined_result, backend_result, annotated_image = (
            await run_in_threadpool(
                analyze_image,
                temporary_path
            )
        )

        # Preserve the final question
        backend_result["question"] = (
            question.strip()
            if question and question.strip()
            else None
        )

        backend_result["answer"] = None
        backend_result["language"] = language

        # ------------------------------------
        # Store Whisper transcription information
        # ------------------------------------

        if transcription_result is not None:

            backend_result["transcription"] = (
                transcription_result["transcription"]
            )

            backend_result["transcriptionLanguage"] = (
                transcription_result["language"]
            )

            backend_result["transcriptionProcessingTime"] = (
                transcription_result["processing_time_seconds"]
            )

        # ------------------------------------
        # Model reliability check
        # ------------------------------------

        reliability = combined_result["reliability"]

        is_unreliable = reliability["is_unreliable"]

        uncertain_result = is_blurry or is_unreliable

        # Discard unreliable frames without a question
        if uncertain_result and not has_question:

            print("\nUnreliable frame without a question.")
            print("Discarding frame. Returning HTTP 204.")

            return Response(status_code=204)

        # ------------------------------------
        # Prepare automatic alerts
        # ------------------------------------

        alert_evidence = prepare_alert_evidence(
            combined_result
        )

        # ------------------------------------
        # Handle uncertain results first
        # ------------------------------------

        if uncertain_result:

            print("\nGenerating uncertainty message.")

            if language == "fr":

                if is_blurry:

                    backend_result["answer"] = (
                        "L'image n'est pas assez nette. "
                        "Je ne peux pas répondre avec certitude. "
                        "Veuillez réessayer."
                    )

                else:

                    backend_result["answer"] = (
                        "Je ne suis pas certain des résultats. "
                        "Veuillez réessayer."
                    )

            else:

                if is_blurry:

                    backend_result["answer"] = (
                        "The image is not clear enough. "
                        "I cannot answer with confidence. "
                        "Please try again."
                    )

                else:

                    backend_result["answer"] = (
                        "I am not confident about the results. "
                        "Please try again."
                    )

            print(backend_result["answer"])

        # ------------------------------------
        # Automatic alerts
        # ------------------------------------

        elif alert_evidence is not None:

            backend_result["question"] = "alert"

            print("\n===================================")
            print("Automatic VisionAid Alert Detected")
            print("===================================")

            # Generate a deterministic fallback alert
            fallback_alert = generate_automatic_alert(
                combined_result,
                language
            )

            backend_result["answer"] = fallback_alert

            obstacle = alert_evidence["nearby_obstacle"]

            # Check whether the obstacle is critically close
            is_critical = (
                obstacle is not None
                and obstacle.get("level") == "critical"
            )

            if is_critical:

                # Avoid waiting for Llama in critical situations
                print("CRITICAL OBSTACLE DETECTED")
                print("Using immediate Python warning.")

            else:

                print(
                    "Generating automatic alert with Llama 3.2..."
                )

                try:

                    alert_result = await run_in_threadpool(
                        generate_alert,
                        alert_evidence["detected_objects"],
                        alert_evidence["important_ocr"],
                        alert_evidence["nearby_obstacle"],
                        language
                    )

                except Exception as error:

                    print("Llama alert generation failed:", error)
                    alert_result = None

                # Use only one alert message
                if alert_result and alert_result.get("answer"):

                    backend_result["answer"] = (
                        alert_result["answer"]
                    )

                    print(
                        "Alert generated successfully by Llama."
                    )

                    print(
                        "LLM alert processing time:",
                        alert_result.get("processing_time_seconds"),
                        "seconds"
                    )

                else:

                    print(
                        "WARNING: Llama alert unavailable. "
                        "Using Python fallback."
                    )

            print("\nFinal automatic alert:")
            print(backend_result["answer"])
            print("===================================")

        # ------------------------------------
        # Generate Llama answer
        # ------------------------------------

        elif question and question.strip():

            print(
                "\nGenerating VisionAid answer with Llama 3.2..."
            )

            llm_question = question.strip()

            # French instructions
            if language == "fr":

                llm_question = (
                    "Réponds uniquement en français. "
                    "Utilise des phrases courtes, claires et naturelles. "
                    "Base ta réponse uniquement sur les objets détectés "
                    "et les résultats OCR fournis. "
                    "N'invente aucun objet ni aucune information. "
                    "Question de l'utilisateur : "
                    + question.strip()
                )

            # English instructions
            else:

                llm_question = (
                    "Answer only in English. "
                    "Use short, clear and natural sentences. "
                    "Base your answer only on the detected objects "
                    "and OCR results provided. "
                    "Do not invent objects or information. "
                    "User question: "
                    + question.strip()
                )

            llm_result = await run_in_threadpool(
                generate_answer,
                llm_question,
                backend_result.get("detectedObjects", []),
                backend_result.get("ocrResults", [])
            )

            if (
                llm_result is not None
                and llm_result.get("answer")
            ):

                backend_result["answer"] = (
                    llm_result["answer"]
                )

                print(
                    "Llama 3.2 answer:",
                    backend_result["answer"]
                )

                # Print LLM performance statistics
                print(
                    "LLM processing time:",
                    llm_result.get("processing_time_seconds"),
                    "seconds"
                )

                print(
                    "Ollama internal duration:",
                    llm_result.get("ollama_total_seconds"),
                    "seconds"
                )

                print(
                    "LLM model loading:",
                    llm_result.get("model_load_seconds"),
                    "seconds"
                )

                print(
                    "LLM prompt processing:",
                    llm_result.get("prompt_processing_seconds"),
                    "seconds"
                )

                print(
                    "LLM generation:",
                    llm_result.get("generation_seconds"),
                    "seconds"
                )

                print(
                    "LLM prompt tokens:",
                    llm_result.get("prompt_tokens")
                )

                print(
                    "LLM generated tokens:",
                    llm_result.get("total_tokens")
                )

            else:

                print(
                    "WARNING: Llama 3.2 returned no usable answer."
                )

        else:

            print("\nNo question and no automatic alert.")

        # ------------------------------------
        # Preserve original uploaded filename
        # ------------------------------------

        backend_result["image"] = (
            (image.filename or "image.jpg")
            .replace("\\", "/")
            .split("/")[-1]
        )

        # ------------------------------------
        # Face privacy masking
        # ------------------------------------

        image_buffer = io.BytesIO()

        # ------------------------------------
        # SCRFD face privacy masking
        # ------------------------------------

        print("\nApplying SCRFD face privacy masking...")

        with Image.open(temporary_path) as source_image:

            original_image = source_image.convert("RGB")

        annotated_image, faces_masked = await run_in_threadpool(
            mask_faces,
            original_image,
            annotated_image
        )

        print(f"Faces masked: {faces_masked}")

        backend_result["faceMasking"] = {
            "model": "SCRFD-2.5G",
            "device": "CPU",
            "facesMasked": faces_masked,
            "method": "solid_black_rectangles"
        }

        annotated_image.save(
            image_buffer,
            format="JPEG",
            quality=90
        )

        # ------------------------------------
        # Create ZIP in memory
        # ------------------------------------

        zip_buffer = io.BytesIO()

        with zipfile.ZipFile(
            zip_buffer,
            mode="w",
            compression=zipfile.ZIP_DEFLATED
        ) as archive:

            # JSON analysis
            archive.writestr(
                "analysis.json",
                json.dumps(
                    backend_result,
                    indent=2,
                    ensure_ascii=False
                )
            )

            # Annotated image
            archive.writestr(
                "annotated.jpg",
                image_buffer.getvalue()
            )

        zip_buffer.seek(0)

        # ------------------------------------
        # Return ZIP to ASP.NET Core
        # ------------------------------------

        return StreamingResponse(
            zip_buffer,
            media_type="application/zip",
            headers={
                "Content-Disposition":
                    'attachment; filename="analysis.zip"'
            }
        )

    except HTTPException:
        raise

    except Exception as e:

        print("Backend analysis error:", e)
        traceback.print_exc()

        raise HTTPException(
            status_code=500,
            detail="Backend image analysis failed."
        )

    finally:

        await image.close()

        if audio is not None:
            await audio.close()

        # Remove temporary image
        if temporary_path is not None:

            try:
                os.remove(temporary_path)

            except FileNotFoundError:
                pass

        # Remove temporary audio
        if temporary_audio_path is not None:

            try:
                os.remove(temporary_audio_path)

            except FileNotFoundError:
                pass




            except FileNotFoundError:
                pass
# ------------------------------------
# Transcribe audio
# Whisper Small
# ------------------------------------

@app.post("/transcribe-audio")
async def transcribe_uploaded_audio(
    file: UploadFile = File(...),
    language: str = Form("en")
):

    if file.content_type not in ALLOWED_AUDIO_TYPES:
        raise HTTPException(
            status_code=400,
            detail=(
                "Unsupported audio format. "
                "Use WAV, MP3, M4A, AAC, WebM or OGG."
            )
        )

    # Validate language
    language = language.strip().lower()

    if language not in {"en", "fr", "ar"}:
        raise HTTPException(
            status_code=400,
            detail="Supported languages are English, French and Arabic."
        )

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
        with tempfile.NamedTemporaryFile(
            delete=False,
            suffix=suffix
        ) as temp_file:
            temp_file.write(audio_bytes)
            temporary_path = Path(temp_file.name)

        print("\n===================================")
        print(f"Received audio for transcription ({language})")
        print("===================================")

        result = await run_in_threadpool(
            transcribe_audio,
            temporary_path,
            language
        )

        question = result["transcription"]

        return {
            **result,
            "question": question
        }

    except Exception as e:
        print("Audio transcription error:", e)
        traceback.print_exc()

        raise HTTPException(
            status_code=500,
            detail="Audio transcription failed."
        )

    finally:
        await file.close()

        if temporary_path is not None:
            try:
                os.remove(temporary_path)
            except FileNotFoundError:
                pass