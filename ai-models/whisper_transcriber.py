import os
import time
import threading
from pathlib import Path

import torch

# Make PyTorch's bundled CUDA libraries available on Windows.
if os.name == "nt":
    torch_lib_path = Path(torch.__file__).resolve().parent / "lib"
    os.add_dll_directory(str(torch_lib_path))

from faster_whisper import WhisperModel


# ------------------------------------
# Project configuration
# ------------------------------------

BASE_DIR = Path(__file__).resolve().parent

WHISPER_MODEL_PATH = BASE_DIR / "whisper-models"

WHISPER_DEVICE = "cuda"
WHISPER_COMPUTE_TYPE = "float16"

# Cached Whisper model
_whisper_model = None
_whisper_lock = threading.Lock()


# ------------------------------------
# Load Whisper Small
# ------------------------------------

def load_whisper():
    global _whisper_model

    with _whisper_lock:
        if _whisper_model is not None:
            print("Reusing cached Whisper Small.")
            return _whisper_model

        print("\nLoading Whisper Small...")

        start = time.perf_counter()

        _whisper_model = WhisperModel(
            str(WHISPER_MODEL_PATH),
            device=WHISPER_DEVICE,
            compute_type=WHISPER_COMPUTE_TYPE,
            num_workers=1,
            local_files_only=True
        )

        elapsed = time.perf_counter() - start

        print(f"Whisper loading time: {elapsed:.2f} seconds")

        return _whisper_model


# ------------------------------------
# Transcribe audio
# ------------------------------------

def transcribe_audio(audio_path, language=None):
    """
    Convert speech to text.

    language=None enables automatic language detection.

    Examples:
        en = English
        fr = French
        ar = Arabic
    """

    audio_path = Path(audio_path)

    if not audio_path.is_file():
        raise FileNotFoundError(
            f"Audio file not found: {audio_path}"
        )

    model = load_whisper()

    print("\n===================================")
    print("VisionAid Speech Recognition")
    print("===================================")

    start = time.perf_counter()

    segments, info = model.transcribe(
        str(audio_path),
        language=language,
        beam_size=1,
        best_of=1,
        vad_filter=True,
        condition_on_previous_text=False,
        task="transcribe"
    )

    # Transcription is lazy: iterate to perform inference.
    segments = list(segments)

    transcription = " ".join(
        segment.text.strip()
        for segment in segments
    ).strip()

    elapsed = time.perf_counter() - start

    print("Detected language:", info.language)
    print("Language probability:", round(info.language_probability, 3))
    print("Transcription:", transcription)
    print(f"Processing time: {elapsed:.2f} seconds")

    return {
        "transcription": transcription,
        "language": info.language,
        "language_probability": round(
            info.language_probability, 3
        ),
        "processing_time_seconds": round(elapsed, 2)
    }


# ------------------------------------
# Standalone test
# ------------------------------------


# ------------------------------------
# Standalone test
# ------------------------------------

if __name__ == "__main__":
    import json

    load_whisper()

    audio_path = input(
        "\nEnter audio file path: "
    ).strip().strip('"')

    # Automatically detect English, French or Arabic
    result = transcribe_audio(
        audio_path,
        language=None
    )

    print("\n========== WHISPER RESULTS ==========")
    print(json.dumps(result, indent=2, ensure_ascii=False))
    print("=====================================")
